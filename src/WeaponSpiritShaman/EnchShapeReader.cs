using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WeaponSpiritShaman
{
     
    // 리플렉션으로 읽는다. 게임 형식을 컴파일 참조하지 않는다(형식 이름 문자열만) → 시험이 같은 함수를 Assembly-CSharp 의 실제 형식 인스턴스로 실행한다
    // (tests\WeaponSpiritShamanTests.cs `exec: shape reader on real game types` — jbp 725 를 실제 형식으로 다시 지어 T2 jbp 추출기 ShapeOf 와 대조).
    //
    // 필드 대응(EnchantInspector.cs 머리 주석의 EnchShape 정의 = jbp 키 기준을 살아 있는 객체에 옮긴 것):
    //   jbp 객체의 키          ↔ 직렬화 필드 = SerializedFields(형식) — 블루프린트 직렬화기 Kingmaker.Blueprints.JsonSystem.FieldsContractResolver 규칙.
    //   jbp "$type"           ↔ 값의 실행 형식 ≠ 선언 형식(필드 형식·배열/List 원소 형식) — 블루프린트 직렬화기 TypeNameHandling.Auto(Json..cctor @0011 ldc.i4.4).
    //   ComponentTypes       = 컴포넌트 실행 형식의 Name(jbp "$type" 쉼표 뒤 이름).
    //   InnerTypes           = 컴포넌트 안(자신 제외) 「$type 이 붙을」 중첩 객체마다 실행 형식 Name.
    //   TrueFlags            = 어느 깊이든 이름이 SpiritRules.WielderFlagNames 인 bool 필드 중 값이 true 인 것의 이름.
    //   NonSimpleValueTypes  = 어느 깊이든 이름이 "ValueType" 인 필드 중 값이 null 도 "Simple" 도 아닌 것의 문자열(열거형 이름 — jbp 는 StringEnumConverter 로 이름을 쓴다).
    //   잎(안으로 들어가지 않음): primitive·enum·string·decimal, mscorlib 의 비컬렉션 형식, UnityEngine.Object 파생(에셋), Unity 값 형식(아래 표),
    //     블루프린트 참조(BlueprintReferenceBase 파생 — jbp 는 "!bp_<guid>" 문자열), 블루프린트 자체(SimpleBlueprint 파생 — 같은 문자열).
    public static class EnchShapeReader
    {
        // 잎으로 보는 기반 형식(전체 이름). 참조 객체의 Cached(블루프린트 본체)로 들어가면 다른 블루프린트의 컴포넌트·플래그를 섞어 읽는다 — 그래서 잎.
        public const string ReferenceBaseType = "Kingmaker.Blueprints.BlueprintReferenceBase";
        public const string SimpleBlueprintType = "Kingmaker.Blueprints.SimpleBlueprint";
        // Unity 형식은 이름공간 + 짧은 이름으로 적는다(「A.B」 모양 리터럴은 문자열 키 시험이 StringsTable 키로 읽는다).
        public const string UnityNamespace = "UnityEngine";
        public const string UnityObjectName = "Object";
        // FieldsContractResolver.GetUnitySerializedFields 의 람다 b__1_0 (public || [SerializeField] || [SerializeReference]) — UnityEngine 의 두 특성.
        public static readonly string[] SerializeAttributeNames = { "SerializeField", "SerializeReference" };
        // FieldsContractResolver.IsSerializableType @002E~0109 의 Unity 값 형식(직렬화 가능, 잎).
        public static readonly string[] UnityValueTypeNames = { "AnimationCurve", "Rect", "Vector2", "Vector3", "Vector4", "Vector2Int", "Vector3Int", "Color", "Color32", "Gradient", "Bounds" };
        public const string ValueTypeFieldName = "ValueType";

        static bool BaseNamed(Type t, string full)
        {
            for (; t != null; t = t.BaseType) if (t.FullName == full) return true;
            return false;
        }

        static bool IsList(Type t) { return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>); }

        static bool IsUnityObject(Type t)
        {
            for (; t != null; t = t.BaseType) if (t.Namespace == UnityNamespace && t.Name == UnityObjectName) return true;
            return false;
        }

        static bool IsUnityValueType(Type t) { return t.Namespace == UnityNamespace && UnityValueTypeNames.Contains(t.Name); }

        public static bool IsLeafType(Type t)
        {
            if (t == null) throw new ArgumentNullException("t");
            if (t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal)) return true;
            if (IsUnityObject(t) || IsUnityValueType(t)) return true;
            if (BaseNamed(t, ReferenceBaseType) || BaseNamed(t, SimpleBlueprintType)) return true;
            if (t.IsArray || IsList(t)) return false;
            return t.Assembly == typeof(int).Assembly; // mscorlib 의 그 밖 형식(object 상자 값·Guid 등) — 블루프린트 데이터 그래프가 아니다
        }

        // FieldsContractResolver.IsSerializableType(type, allowArrays) @0000~0164 그대로.
        static bool IsSerializableType(Type t, bool allowArrays)
        {
            if (t.IsPrimitive || t.IsEnum || t == typeof(string)) return true;
            if (IsUnityObject(t)) return true;
            if (IsUnityValueType(t)) return true;
            if (t.IsArray) return allowArrays && IsSerializableType(t.GetElementType(), false);
            if (IsList(t)) return allowArrays && IsSerializableType(t.GetGenericArguments()[0], false);
            return (t.Attributes & TypeAttributes.Serializable) != 0 && t.Assembly != typeof(int).Assembly;
        }

        static bool HasUnitySerializeAttribute(FieldInfo f)
        {
            return f.GetCustomAttributesData().Any(a => a.AttributeType.Namespace == UnityNamespace && SerializeAttributeNames.Contains(a.AttributeType.Name));
        }

        // 직렬화 필드 = FieldsContractResolver.GetUnitySerializedFields(type) @0000~00A7: 기반 형식 먼저(재귀), 그다음 이 형식의 DeclaredOnly 인스턴스 필드(54 = DeclaredOnly|Instance|Public|NonPublic) 중
        // (public || [SerializeField] || [SerializeReference]) && ![NonSerialized] && IsSerializableType(필드 형식, true).
        public static FieldInfo[] SerializedFields(Type t)
        {
            if (t == null) throw new ArgumentNullException("t");
            var chain = new List<Type>();
            for (var c = t; c != null; c = c.BaseType) chain.Insert(0, c);
            const BindingFlags F = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
             
            return chain.SelectMany(c => CloneGraph.FieldsInDeclarationOrder(c, F).Where(f =>
                    (f.IsPublic || HasUnitySerializeAttribute(f))
                    && !f.IsNotSerialized
                    && IsSerializableType(f.FieldType, true)))
                .ToArray();
        }

        sealed class RefEq : IEqualityComparer<object>
        {
            public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
            public int GetHashCode(object o) { return RuntimeHelpers.GetHashCode(o); }
        }

        sealed class Acc
        {
            public readonly List<string> Inner = new List<string>(), Flags = new List<string>(), Values = new List<string>();
            public readonly HashSet<object> OnPath = new HashSet<object>(new RefEq());
        }

        public static EnchShape Read(string guid, string name, IList<object> components)
        {
            if (string.IsNullOrEmpty(guid)) throw new ArgumentException("EnchShapeReader: guid is empty");
            if (components == null) throw new ArgumentNullException("components", "EnchShapeReader: enchantment " + guid + " has no component list (empty = no components, not null)");
            var acc = new Acc();
            var types = new List<string>();
            var facts = new List<string>();
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c == null) throw new InvalidOperationException("EnchShapeReader: enchantment " + guid + " (" + name + ") component " + i + " is null");
                types.Add(c.GetType().Name);
                Visit(c, acc);
                facts.AddRange(ComponentFacts.Of(i, c.GetType().Name, new ObjectFields(c, guid)));
            }
            return new EnchShape { Guid = guid, Name = name, ComponentTypes = types.ToArray(), InnerTypes = acc.Inner.ToArray(), TrueFlags = acc.Flags.ToArray(), NonSimpleValueTypes = acc.Values.ToArray(), ComponentFacts = facts.ToArray() };
        }

        // ComponentFacts 의 필드 읽기 — 살아 있는 컴포넌트 객체(직렬화 필드 = SerializedFields, 이름으로 찾는다). 없는 필드·형식이 다른 값은 던진다(게임 판 바뀜 = 시끄럽게).
        // 참조 = BlueprintReferenceBase 의 직렬화 필드 "guid"(시험 `exec: shape reader rules` 가 SerializedFields(BlueprintReferenceBase) == guid 를 잠근다).
        sealed class ObjectFields : ComponentFacts.IFieldSource
        {
            readonly object o; readonly string owner;
            public ObjectFields(object o, string owner) { this.o = o; this.owner = owner; }

            object Value(string field)
            {
                var f = SerializedFields(o.GetType()).Where(x => x.Name == field).ToArray();
                if (f.Length != 1) throw new InvalidOperationException("EnchShapeReader: " + owner + " " + o.GetType().Name + " has " + f.Length + " serialized field(s) named " + field);
                return f[0].GetValue(o);
            }

            public bool Bool(string field)
            {
                var v = Value(field);
                if (!(v is bool)) throw new InvalidOperationException("EnchShapeReader: " + owner + " " + o.GetType().Name + "." + field + " is not a bool");
                return (bool)v;
            }

            public string EnumName(string field)
            {
                var v = Value(field);
                if (v == null || !v.GetType().IsEnum) throw new InvalidOperationException("EnchShapeReader: " + owner + " " + o.GetType().Name + "." + field + " is not an enum");
                return v.ToString();
            }

            public string RefGuid(string field)
            {
                var v = Value(field);
                if (v == null) return null;
                if (!BaseNamed(v.GetType(), ReferenceBaseType)) throw new InvalidOperationException("EnchShapeReader: " + owner + " " + o.GetType().Name + "." + field + " is not a blueprint reference");
                var g = SerializedFields(v.GetType()).Where(x => x.Name == ReferenceGuidField).ToArray();
                if (g.Length != 1) throw new InvalidOperationException("EnchShapeReader: blueprint reference " + v.GetType().Name + " has " + g.Length + " '" + ReferenceGuidField + "' field(s)");
                return g[0].GetValue(v) as string;
            }

            public string[] ActionTypes(string actionListField)
            {
                var list = Value(actionListField);
                if (list == null) return new string[0];
                var a = SerializedFields(list.GetType()).Where(x => x.Name == ActionListActionsField).ToArray();
                if (a.Length != 1) throw new InvalidOperationException("EnchShapeReader: " + owner + " " + actionListField + " (" + list.GetType().Name + ") has no '" + ActionListActionsField + "' field");
                var arr = a[0].GetValue(list) as IEnumerable;
                if (arr == null) return new string[0];
                var r = new List<string>();
                foreach (var x in arr)
                {
                    if (x == null) throw new InvalidOperationException("EnchShapeReader: " + owner + " " + actionListField + " has a null action");
                    r.Add(x.GetType().Name);
                }
                return r.ToArray();
            }
        }

        public const string ReferenceGuidField = "guid";
        public const string ActionListActionsField = "Actions";

        // 객체 하나의 직렬화 필드를 훑는다(그 객체 자신의 형식 이름은 부르는 쪽이 이미 기록했다).
        static void Visit(object o, Acc acc)
        {
            bool tracked = !o.GetType().IsValueType;
            if (tracked && !acc.OnPath.Add(o)) throw new InvalidOperationException("EnchShapeReader: reference cycle through " + o.GetType().FullName);
            foreach (var f in SerializedFields(o.GetType()))
            {
                var v = f.GetValue(o);
                if (v is bool && (bool)v && SpiritRules.WielderFlagNames.Contains(f.Name)) acc.Flags.Add(f.Name);
                if (f.Name == ValueTypeFieldName && v != null)
                {
                    string s = Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
                    if (s != SpiritRules.SimpleValueType) acc.Values.Add(s);
                }
                Walk(v, f.FieldType, acc);
            }
            if (tracked) acc.OnPath.Remove(o);
        }

        static void Walk(object v, Type declared, Acc acc)
        {
            if (v == null) return;
            var t = v.GetType();
            if (IsLeafType(t)) return;
            if (t.IsArray || IsList(t))
            {
                // Auto: 원소는 컬렉션 원소 형식과 비교한다(Newtonsoft ShouldWriteType — 컬렉션 계약의 CollectionItemType).
                var et = t.IsArray ? t.GetElementType() : t.GetGenericArguments()[0];
                foreach (var x in (IEnumerable)v) Walk(x, et, acc);
                return;
            }
            var d = Nullable.GetUnderlyingType(declared) ?? declared;
            if (t != d) acc.Inner.Add(t.Name);
            Visit(v, acc);
        }
    }
}
