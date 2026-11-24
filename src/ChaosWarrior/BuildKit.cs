using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
    // 빌더 공용 도우미(게임 형식). 빌더 IL 에 숫자 리터럴이 남지 않게(시험 `dial: core literals`) 엔진 열거값은 이름 문자열 → E<T>, 배열은 목록 → ToArray 로 만든다.
    internal static class BuildKit
    {
        // 엔진 열거 이름 → 값. 이름이 없으면(게임 버전 차이·오타) ArgumentException — 조용히 기본값으로 가지 않는다.
        internal static T E<T>(string name) where T : struct
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("ChaosWarrior: empty enum name for " + typeof(T).Name);
            if (!Enum.IsDefined(typeof(T), name)) throw new ArgumentException("ChaosWarrior: " + typeof(T).Name + " has no member '" + name + "'");
            return (T)Enum.Parse(typeof(T), name);
        }

        // 엔진 플래그 열거: 이름 목록 → 비트 합. 빈 목록·없는 이름은 던진다(조용히 0 이 되지 않는다).
        internal static T Flags<T>(IEnumerable<string> names) where T : struct
        {
            if (names == null) throw new ArgumentNullException("names");
            int v = 0, n = 0;
            foreach (var name in names) { v |= Convert.ToInt32(E<T>(name)); n++; }
            if (n == 0) throw new ArgumentException("ChaosWarrior: empty flag name list for " + typeof(T).Name);
            return (T)Enum.ToObject(typeof(T), v);
        }

        // 이름 목록이 비어 있어도 되는 플래그(= 그 속성을 주지 않는 단계): 빈 목록 → 0(이름 없는 값), 그 밖엔 Flags 와 같다. null 은 던진다.
        internal static T FlagsOrNone<T>(IEnumerable<string> names) where T : struct
        {
            if (names == null) throw new ArgumentNullException("names");
            return names.Any() ? Flags<T>(names) : default(T);
        }

        // 플래그 값 → 켜진 비트의 이름 목록(되읽기용). 이름 붙은 비트로 설명되지 않는 비트가 남으면 "?<값>" 을 넣어 사양과 반드시 달라지게 한다.
        internal static List<string> FlagNames<T>(T value) where T : struct
        {
            int v = Convert.ToInt32(value), seen = 0;
            var l = new List<string>();
            foreach (var name in Enum.GetNames(typeof(T)))
            {
                int bit = Convert.ToInt32(Enum.Parse(typeof(T), name));
                if (bit != 0 && (v & bit) == bit) { l.Add(name); seen |= bit; }
            }
            if ((v & ~seen) != 0) l.Add("?" + (v & ~seen));
            return l;
        }

        internal static void FillEmpty(object o) { BuildReflect.FillEmpty(o); }

        internal static T Comp<T>(T c) where T : BlueprintComponent { FillEmpty(c); return c; }
        internal static T Act<T>(T a) where T : GameAction { FillEmpty(a); return a; }
        internal static T Cond<T>(T c) where T : Condition { FillEmpty(c); return c; }

        internal static ActionList Actions(IEnumerable<GameAction> a) { return new ActionList { Actions = a.ToArray() }; }

        internal static ContextValue Cv(int v) { return new ContextValue { ValueType = ContextValueType.Simple, Value = v }; }
        internal static ContextValue CvRank(string rankType) { return new ContextValue { ValueType = E<ContextValueType>("Rank"), ValueRank = E<AbilityRankType>(rankType) }; }

        // 2026-11-17 결함 수리(툴팁 짧은 설명 `<null>`): 엔진 생성자는 LocalizedString 칸을 채우지 않는다(BlueprintUtil 「LocalizedString 필드」 주석). 짧은 설명 = 빈 키(NoText) —
        // BlueprintUnitFact.get_DescriptionShort 는 빈 값이면 설명(m_Description)으로 대신하고, null 이면 "<null>" 을 그대로 돌려준다. 숨긴 칸 규칙 = 점검 InGameAudit.HiddenShortDescriptionField.
        internal static void Names(BlueprintScriptableObject bp, string nameKey, string descKey)
        {
            SetField(bp, "m_DisplayName", Strings.L(nameKey));
            SetField(bp, "m_Description", Strings.L(descKey));
            SetField(bp, "m_DescriptionShort", NoText());
        }

        internal static BlueprintFeature Feature(string name, string guid, string nameKey, string descKey, bool hidden)
        {
            return Create<BlueprintFeature>(name, guid, f =>
            {
                Names(f, nameKey, descKey);
                f.Ranks = 1; // 특성 한 번(순위 없는 특성)
                f.IsClassFeature = true;
                f.HideInUI = hidden;
                f.Groups = new FeatureGroup[0];
                f.ReapplyOnLevelUp = false;
                FillEmpty(f);
            });
        }

        internal static BlueprintBuff Buff(string name, string guid, string nameKey, string descKey, bool hidden, string stacking)
        {
            return Create<BlueprintBuff>(name, guid, b =>
            {
                Names(b, nameKey, descKey);
                b.Stacking = E<StackingType>(stacking);
                var flagsType = FindField(typeof(BlueprintBuff), "m_Flags").FieldType;
                SetField(b, "m_Flags", hidden ? Enum.Parse(flagsType, "HiddenInUi") : Enum.ToObject(flagsType, 0));
                b.IsClassFeature = true;
                FillEmpty(b);
                // 2026-11-16 결함: FillEmpty 는 PrefabLink 를 채우지 않는다. 엔진 Buff.OnRemove IL_00A2~00B7 은 FxOnStart 로 입자를 띄운 버프를 지울 때
                // Blueprint.FxOnRemove.Load 를 null 검사 없이 부른다 → NRE → 분노가 묶은 변이 버프를 다 지우지 못하고 남겼다(GameLogFull 23건).
                // 본편 jbp 는 null 이어도 읽을 때 빈 링크가 된다(FxOnStart 있는 본편 버프 1,344 중 1,296 이 jbp 상 null 인데 정상 제거). 우리는 빈 링크를 직접 넣는다.
                if (b.FxOnStart == null) b.FxOnStart = new Kingmaker.ResourceLinks.PrefabLink();
                if (b.FxOnRemove == null) b.FxOnRemove = new Kingmaker.ResourceLinks.PrefabLink();
            });
        }

        // ContextRankConfig{m_Type = rankType, ClassLevel(WarpriestClass), Custom(rows)}. rows = ChaosRules.ProgressionRows(...) 그대로.
        internal static ContextRankConfig RankConfig(string rankType, string baseValueType, string progression, (int BaseValue, int ProgressionValue)[] rows)
        {
            var c = Comp(new ContextRankConfig());
            SetField(c, "m_Type", E<AbilityRankType>(rankType));
            SetField(c, "m_BaseValueType", E<ContextRankBaseValueType>(baseValueType));
            var cls = new List<BlueprintCharacterClassReference> { Ref<BlueprintCharacterClassReference>(Guids.Game.WarpriestClass) };
            SetField(c, "m_Class", cls.ToArray());
            SetField(c, "m_ExceptClasses", false);
            SetField(c, "m_Progression", E<ContextRankProgression>(progression));
            var itemType = FindField(typeof(ContextRankConfig), "m_CustomProgression").FieldType.GetElementType();
            var items = Array.CreateInstance(itemType, rows.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                var it = Activator.CreateInstance(itemType);
                SetField(it, "BaseValue", rows[i].BaseValue);
                SetField(it, "ProgressionValue", rows[i].ProgressionValue);
                items.SetValue(it, i);
            }
            SetField(c, "m_CustomProgression", items);
            SetField(c, "m_UseMin", false);
            SetField(c, "m_UseMax", false);
            return c;
        }

        // ContextRankConfig 되읽기: (m_Type, m_Class GUID 목록, 행). 빌더가 생산 사양과 대조한다.
        internal static string DescribeRank(ContextRankConfig c)
        {
            var cls = (BlueprintCharacterClassReference[])GetF(c, "m_Class");
            var rows = (Array)GetF(c, "m_CustomProgression");
            var parts = new List<string>();
            foreach (var it in rows) parts.Add(GetF(it, "BaseValue") + ">" + GetF(it, "ProgressionValue"));
            return GetF(c, "m_Type") + "|" + GetF(c, "m_BaseValueType") + "|" + GetF(c, "m_Progression") + "|" + string.Join(",", cls.Select(r => r.Guid.ToString()).ToArray()) + "|" + string.Join(",", parts.ToArray());
        }

        internal static string DescribeRows(string rankType, string baseValueType, string progression, (int BaseValue, int ProgressionValue)[] rows)
        {
            return rankType + "|" + baseValueType + "|" + progression + "|" + Guids.Game.WarpriestClass + "|" + string.Join(",", rows.Select(r => r.BaseValue + ">" + r.ProgressionValue).ToArray());
        }

        internal static object GetF(object o, string name) { return BuildReflect.GetF(o, name); }
    }

    // 리플렉션 배관(규칙·수치 없음 — `dial: core literals` 검사 대상 밖, BindingFlags 상수가 IL 에 남는다).
    internal static class BuildReflect
    {
        static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        // 본편 부품의 얕은 복제(복제본만 우리 블루프린트에 붙인다 — 원본 OwnerBlueprint 를 바꾸지 않게, BlueprintUtil.AddComponents 주석). 이름은 AddComponents 가 새로 단다.
        internal static BlueprintComponent CloneComponent(BlueprintComponent c)
        {
            if (c == null) throw new ArgumentNullException("c");
            if (MemberwiseCloneMethod == null) throw new MissingMethodException("ChaosWarrior: object.MemberwiseClone not found");
            return (BlueprintComponent)MemberwiseCloneMethod.Invoke(c, null);
        }

        internal static object CloneObject(object o)
        {
            if (o == null) throw new ArgumentNullException("o");
            if (MemberwiseCloneMethod == null) throw new MissingMethodException("ChaosWarrior: object.MemberwiseClone not found");
            return MemberwiseCloneMethod.Invoke(o, null);
        }

        internal static Type FieldType(Type t, string name)
        {
            var f = FindField(t, name);
            if (f == null) throw new MissingFieldException("ChaosWarrior: " + t.Name + "." + name + " not found");
            return f.FieldType;
        }

        // 새 객체의 null 컬렉션·참조 필드를 빈 값으로(직렬화본은 [] / 빈 참조 — 엔진이 null 을 순회하다 죽지 않게). src\BulwarkMage\Rebuke.cs FillEmpty 와 같은 규칙.
        internal static void FillEmpty(object o)
        {
            for (var t = o.GetType(); t != null && t != typeof(object); t = t.BaseType)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.IsInitOnly || f.GetValue(o) != null) continue;
                    var ft = f.FieldType;
                    if (ft.IsArray) f.SetValue(o, Array.CreateInstance(ft.GetElementType(), 0));
                    else if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>)) f.SetValue(o, Activator.CreateInstance(ft));
                    else if (typeof(BlueprintReferenceBase).IsAssignableFrom(ft) && !ft.IsAbstract) f.SetValue(o, Activator.CreateInstance(ft));
                }
        }

        internal static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("ChaosWarrior: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
