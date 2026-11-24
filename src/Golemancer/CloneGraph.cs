using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Golemancer
{
     
    // 게임 쪽 SpiritBlueprints.Build 끝과 점검 S22 가 실제 블루프린트 그래프(노드 수집은 게임 쪽 Isolation)로 같은 함수를 부른다.
    // 규칙: 원본 그래프(복사 원본 5종 + 기준 무기 59 + 두 번째 머리 4 의 블루프린트·컴포넌트·배열·LevelEntry·ClassWithLevel·Body·시각 객체)와
    //   우리 그래프의 참조 교집합(ReferenceEquals)에 든 노드 종류 = AllowedSharedKinds 와 정확히 같아야 한다.
    //   - 허용 밖 종류가 교집합에 있으면 위반(본편 객체를 우리 블루프린트가 품음 → 우리 쪽 쓰기·OnEnable 이 본편을 바꾼다).
    //   - 허용 종류가 교집합에 하나도 없으면 위반(허용 목록이 낡았거나 수집기가 그 노드를 안 모음 — 조용한 통과 방지).
    //   - 우리 노드끼리 같은 인스턴스를 두 소유자가 나눠 가지면(허용 종류 제외) 위반(OwnerBlueprint 등이 마지막 소유자로 덮인다).
    public sealed class GraphNode
    {
        public readonly object Ref;
        public readonly string Kind;
        public readonly string Owner;
        public GraphNode(object reference, string kind, string owner)
        {
            if (reference == null) throw new ArgumentNullException("reference", "CloneGraph: node " + kind + " of " + owner + " is null (collector must skip or throw, not add null)");
            if (reference is string || reference.GetType().IsValueType) throw new ArgumentException("CloneGraph: node " + kind + " of " + owner + " is a " + reference.GetType().Name + " (identity is meaningless for strings/value types)");
            if (string.IsNullOrEmpty(kind)) throw new ArgumentException("CloneGraph: empty node kind");
            if (string.IsNullOrEmpty(owner)) throw new ArgumentException("CloneGraph: empty node owner");
            Ref = reference; Kind = kind; Owner = owner;
        }
    }

    public sealed class CloneCheck
    {
        public string[] SharedKinds;   // 교집합에서 관측한 종류(정렬, 중복 없음)
        public string[] Violations;    // 비면 통과
        public int OriginalCount, CloneCount;
    }

    public static class CloneGraph
    {
        // 허용 공유(읽기만) 노드 종류 — 계획 T3 표 「공유(읽기만)」 열 중 수집하는 것: 영병 유닛의 Visual(변경 안 함).
        // 시험 `exec: clone isolation model` 이 이 배열을 리터럴과 대조한다(허용 목록을 넓히면 FAIL).
        public static readonly string[] AllowedSharedKinds = { "unit.visual" };

        sealed class RefEq : IEqualityComparer<object>
        {
            public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
            public int GetHashCode(object o) { return RuntimeHelpers.GetHashCode(o); }
        }

        public static CloneCheck Compare(IEnumerable<GraphNode> original, IEnumerable<GraphNode> clones)
        {
            if (original == null) throw new ArgumentNullException("original");
            if (clones == null) throw new ArgumentNullException("clones");
            var orig = original.ToList();
            var ours = clones.ToList();
            if (orig.Count == 0) throw new ArgumentException("CloneGraph: original graph is empty (collector found nothing)");
            if (ours.Count == 0) throw new ArgumentException("CloneGraph: clone graph is empty (collector found nothing)");
            var allowed = new HashSet<string>(AllowedSharedKinds, StringComparer.Ordinal);
            var violations = new List<string>();
            var shared = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var n in SharedNodes(orig, ours))
            {
                shared.Add(n.Kind);
                if (!allowed.Contains(n.Kind)) violations.Add("shared with base game: " + n.Kind + " of " + n.Owner);
            }
            ours = ours.Where(n => IdentityRelevant(n.Ref)).ToList();
            foreach (var k in AllowedSharedKinds)
                if (!shared.Contains(k)) violations.Add("allowed shared kind " + k + " was not observed shared (stale allowance or collector misses it)");
            // 우리 노드끼리의 재사용(허용 종류 제외): 같은 인스턴스가 서로 다른 소유자에 두 번 이상.
            foreach (var g in ours.Where(n => !allowed.Contains(n.Kind)).GroupBy(n => n.Ref, new RefEq()))
            {
                var owners = g.Select(n => n.Owner).Distinct(StringComparer.Ordinal).ToList();
                if (owners.Count > 1) violations.Add("one " + g.First().Kind + " instance reused by our blueprints " + string.Join(", ", owners.Take(4)) + (owners.Count > 4 ? ", ..." : ""));
            }
            return new CloneCheck { SharedKinds = shared.ToArray(), Violations = violations.ToArray(), OriginalCount = orig.Count, CloneCount = ours.Count };
        }

        // 정체 비교에서 빼는 값(이름 붙인 일반 규칙, 리뷰 T3 C1): 길이 0 배열.
        //   이유: 길이 0 배열은 원소가 없어 공유해도 쓸 곳이 없다(배열 길이는 바뀌지 않고, 다른 배열로 바꾸려면 그 배열을 가진 객체의 필드에 써야 한다 —
        //   그 쓰기는 소유 객체 쪽 규칙(WriteScan·참조 분리)이 본다). 그런데 엔진·런타임은 길이 0 배열을 싱글턴으로 나눠 쓴다:
        //   SimpleBlueprint..ctor @0001~0006 m_ValidationStatus = Array.Empty<string>()(mscorlib EmptyArray<T>.Value, 정적 인스턴스 하나) → 모든 블루프린트가 같은 string[0].
        //   이를 공유로 세면 게임 안 Verify 가 항상 던진다(리뷰 C1). 길이 0 이 아닌 배열·모든 List 는 그대로 비교한다(List 는 비어 있어도 Add 로 쓸 수 있다).
        public static bool IdentityRelevant(object v)
        {
            var a = v as Array;
            return a == null || a.Length != 0;
        }

        // 원본 그래프와 참조가 같은 우리 노드(정체 비교 대상만 — IdentityRelevant).
        public static List<GraphNode> SharedNodes(IEnumerable<GraphNode> original, IEnumerable<GraphNode> clones)
        {
            if (original == null) throw new ArgumentNullException("original");
            if (clones == null) throw new ArgumentNullException("clones");
            var origSet = new HashSet<object>(original.Where(n => IdentityRelevant(n.Ref)).Select(n => n.Ref), new RefEq());
            return clones.Where(n => IdentityRelevant(n.Ref) && origSet.Contains(n.Ref)).ToList();
        }

        // 객체의 배열·List<T> 인스턴스 필드 값마다 노드(종류 = kind + "." + 필드 이름). 기반 형식까지 올라간다. 게임 쪽 Isolation 수집기가 쓰고,
        // 시험이 Assembly-CSharp 의 실제 형식 인스턴스에 그대로 실행한다(게임 형식 없음 — 리플렉션만).
         
        // .NET Framework RuntimeType 멤버 캐시는 GC 에 수거되고 다시 채울 때 앞선 이름 조회 순서를 따른다 — ConditionsChecker 에서 GetField("Conditions") + GC 뒤
        // Conditions,Operation 재현). 필드 순서를 쓰는 곳(EnchShapeReader.SerializedFields·CloneKit.CopyInto·ContainerNodes)은 전부 이것을 거친다.
        public static FieldInfo[] FieldsInDeclarationOrder(Type t, BindingFlags flags)
        {
            if (t == null) throw new ArgumentNullException("t");
            if ((flags & BindingFlags.DeclaredOnly) == 0) throw new ArgumentException("CloneGraph.FieldsInDeclarationOrder: DeclaredOnly required (tokens order fields within one type only)");
            return t.GetFields(flags).OrderBy(f => f.MetadataToken).ToArray();
        }

        public static List<GraphNode> ContainerNodes(object o, string kind, string owner)
        {
            if (o == null) throw new ArgumentNullException("o");
            var l = new List<GraphNode>();
            for (var t = o.GetType(); t != null && t != typeof(object); t = t.BaseType)
                foreach (var f in FieldsInDeclarationOrder(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var v = f.GetValue(o);
                    if (v == null || v is string) continue;
                    var vt = v.GetType();
                    if (v is Array || (vt.IsGenericType && vt.GetGenericTypeDefinition() == typeof(List<>)))
                        l.Add(new GraphNode(v, kind + "." + f.Name, owner));
                }
            return l;
        }

        public static CloneCheck Verify(IEnumerable<GraphNode> original, IEnumerable<GraphNode> clones)
        {
            var r = Compare(original, clones);
            if (r.Violations.Length > 0)
                throw new InvalidOperationException("CloneGraph: " + r.Violations.Length + " isolation violation(s): " + string.Join(" | ", r.Violations.Take(12)) + (r.Violations.Length > 12 ? " | ..." : ""));
            return r;
        }

        // Build 전후 본편 값 되읽기 비교. 값이 참조 형식이면(문자열 제외) 참조 동일성, 아니면 Equals. 키 집합이 다르면 그것도 차이.
        public static string[] SnapshotDiff(IList<KeyValuePair<string, object>> before, IList<KeyValuePair<string, object>> after)
        {
            if (before == null) throw new ArgumentNullException("before");
            if (after == null) throw new ArgumentNullException("after");
            if (before.Count == 0) throw new ArgumentException("CloneGraph: empty snapshot (nothing was read)");
            var dupB = before.GroupBy(kv => kv.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            var dupA = after.GroupBy(kv => kv.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            if (dupB.Length > 0 || dupA.Length > 0) throw new ArgumentException("CloneGraph: duplicate snapshot keys " + string.Join(",", dupB.Concat(dupA)));
            var a = after.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
            var diffs = new List<string>();
            foreach (var kv in before)
            {
                object v;
                if (!a.TryGetValue(kv.Key, out v)) { diffs.Add(kv.Key + ": missing after"); continue; }
                if (!Same(kv.Value, v)) diffs.Add(kv.Key + ": " + Show(kv.Value) + " -> " + Show(v));
            }
            var bKeys = new HashSet<string>(before.Select(kv => kv.Key), StringComparer.Ordinal);
            foreach (var kv in after) if (!bKeys.Contains(kv.Key)) diffs.Add(kv.Key + ": new key after");
            return diffs.ToArray();
        }

        static bool Same(object x, object y)
        {
            if (x == null || y == null) return x == null && y == null;
            if (x is string || x.GetType().IsValueType) return x.Equals(y);
            return ReferenceEquals(x, y);
        }

        static string Show(object o) { return o == null ? "null" : (o is string || o.GetType().IsValueType) ? o.ToString() : o.GetType().Name + "#" + RuntimeHelpers.GetHashCode(o); }
    }

    // 깊은 복사 규칙 표(게임 쪽 CloneKit 이 쓴다, 시험이 Assembly-CSharp.dll 과 대조). 게임 형식 없음 — 이름만.
    public static class CloneRules
    {
        // 값 복사에서 건너뛰는 필드(선언 형식 전체 이름, 필드 이름). 이유:
        //  - 정체: SimpleBlueprint.name·AssetGuid(새 이름·GUID 는 Create 가 준다).
        //  - 원소 등록부: SimpleBlueprint.m_AllElements(BlueprintScriptableObject.get_ElementsArray @0000~0018 이 게으르게 만들어 저장, OnEnable @0024~003F 이 원소 Owner 를 바꾼다 → 공유하면 본편 원소의 Owner 를 우리 것으로 바꾼다).
        //    CloneKit 은 원본의 이 목록이 비어 있지 않으면 던진다(원소 복사는 하지 않는다).
        //  - 검증 상태: SimpleBlueprint.m_ValidationStatus([NonSerialized]).
        //  - 컴포넌트 배열: BlueprintScriptableObject.Components(호출자가 새 컴포넌트로 채운다 — 공유하면 OnEnable @0011~0015 가 본편 컴포넌트의 OwnerBlueprint 를 바꾼다).
        //  - 원형 연결: BlueprintScriptableObject.m_PrototypeId·m_Overrides, BlueprintComponent.m_PrototypeLink(런타임 생성물은 원형이 없다).
        //  - 컴포넌트 소유자·이름: BlueprintComponent.<OwnerBlueprint>k__BackingField·name(AddComponents 가 새로 단다).
        //  - 캐시(지연 계산 결과 — 복사하면 원본의 결과를 우리 것으로 읽는다): 아래 m_Cached*·…Cache·<Prototype>k__BackingField(WeaponVisualParameters, OnEnableWithLibrary @0023~0034 가 우리 객체에 다시 단다).
        public static readonly string[][] SkipFields =
        {
            new[] { "Kingmaker.Blueprints.SimpleBlueprint", "name" },
            new[] { "Kingmaker.Blueprints.SimpleBlueprint", "AssetGuid" },
            new[] { "Kingmaker.Blueprints.SimpleBlueprint", "m_AllElements" },
            new[] { "Kingmaker.Blueprints.SimpleBlueprint", "m_ValidationStatus" },
            new[] { "Kingmaker.Blueprints.BlueprintScriptableObject", "Components" },
            new[] { "Kingmaker.Blueprints.BlueprintScriptableObject", "m_PrototypeId" },
            new[] { "Kingmaker.Blueprints.BlueprintScriptableObject", "m_Overrides" },
            new[] { "Kingmaker.Blueprints.BlueprintComponent", "<OwnerBlueprint>k__BackingField" },
            new[] { "Kingmaker.Blueprints.BlueprintComponent", "name" },
            new[] { "Kingmaker.Blueprints.BlueprintComponent", "m_PrototypeLink" },
            new[] { "Kingmaker.Blueprints.Items.BlueprintItem", "m_CachedEnchantments" },
            new[] { "Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipment", "m_CachedUseWhileCanComponents" },
            new[] { "Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DescriptionModifiersCache" },
            new[] { "Kingmaker.Blueprints.BlueprintUnit", "m_CachedTags" },
            new[] { "Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters", "m_CachedBeltModel" },
            new[] { "Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters", "m_CachedSheathModel" },
            new[] { "Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters", "m_CachedEquipLinksUpToDate" },
            new[] { "Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters", "<Prototype>k__BackingField" },
        };

        // 이름에 이 표식(대소문자 무시)이 든 인스턴스 필드는 반드시 SkipFields 에 있어야 한다(모르는 캐시를 조용히 복사하지 않는다 — CloneKit 이 런타임에, 시험이 dll 로 확인).
        public const string CacheMarker = "cache";

        // CloneKit 이 값 복사하는 형식(시험이 위 규칙을 이 형식들의 필드 사슬에 적용해 본다).
        public static readonly string[] CopiedTypes =
        {
            "Kingmaker.Blueprints.Classes.BlueprintCharacterClass",
            "Kingmaker.Blueprints.Classes.BlueprintProgression",
            "Kingmaker.Blueprints.BlueprintUnit",
            "Kingmaker.Blueprints.BlueprintUnit/UnitBody",
            "Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon",
            "Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters",
            "Kingmaker.Blueprints.Classes.AddClassLevels",
            "Kingmaker.UnitLogic.FactLogic.AddFacts",
            "Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteIsPet",
            "Kingmaker.UnitLogic.FactLogic.AllowDyingCondition",
            "Kingmaker.UnitLogic.FactLogic.AddResurrectOnRest",
            "Kingmaker.DLC.DlcCondition",
            "Kingmaker.Blueprints.Classes.SelectionEntry",
        };

        public static bool Skip(string declaringType, string field)
        {
            return SkipFields.Any(s => s[0] == declaringType && s[1] == field);
        }
    }
}
