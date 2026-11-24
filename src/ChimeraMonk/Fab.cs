using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
    // 블루프린트 만들기 도우미(게임 쪽). Bulwark Rebuke.cs 의 FeatureFlags·FillEmpty·Comp 와 같은 규칙:
    // 새 객체의 null 배열·리스트·참조 필드는 빈 값으로(직렬화본처럼 — 엔진이 null 을 순회하다 죽지 않게), 이미 값이 있는 필드는 건드리지 않는다.
    internal static class Fab
    {
        // ShortTextNote(2026-11-17 결함 수리, 툴팁 짧은 설명 `<null>`): 엔진 생성자는 LocalizedString 칸을 채우지 않는다(BlueprintUtil 「LocalizedString 필드」 주석).
        // Feature·Selection·Buff·Ability 는 짧은 설명(m_DescriptionShort)을 빈 키(BlueprintUtil.NoText)로 넣는다 — BlueprintUnitFact.get_DescriptionShort 는 빈 값이면 설명으로 대신하고
        // null 이면 "<null>" 을 그대로 돌려준다. 이 네 칸 = 점검 InGameAudit.HiddenShortDescriptionField 규칙(시험 `tooltip:` 이 IL 로 잠근다).
        internal static void FeatureFlags(BlueprintFeature f, bool hidden)
        {
            f.Ranks = 1;
            f.IsClassFeature = true;
            f.HideInUI = hidden;
            f.Groups = new FeatureGroup[0];
            f.ReapplyOnLevelUp = false;
        }

        internal static BlueprintFeature Feature(string name, string guid, string key, bool hidden, Action<BlueprintFeature> more = null)
        {
            return Create<BlueprintFeature>(name, guid, f =>
            {
                SetField(f, "m_DisplayName", Strings.L(key + ".name"));
                SetField(f, "m_Description", Strings.L(key + ".desc"));
                SetField(f, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 "<null>"(아래 ShortTextNote)
                FeatureFlags(f, hidden);
                if (more != null) more(f);
                FillEmpty(f);
            });
        }

        internal static BlueprintFeatureSelection Selection(string name, string guid, string key, IEnumerable<string> options)
        {
            var refs = options.Select(Ref<BlueprintFeatureReference>).ToArray();
            if (refs.Length == 0) throw new InvalidOperationException("ChimeraMonk: selection " + name + " without options");
            return Create<BlueprintFeatureSelection>(name, guid, s =>
            {
                SetField(s, "m_DisplayName", Strings.L(key + ".name"));
                SetField(s, "m_Description", Strings.L(key + ".desc"));
                SetField(s, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(ShortTextNote)
                FeatureFlags(s, false);
                s.Mode = SelectionMode.Default;
                s.Group = FeatureGroup.None;
                s.Group2 = FeatureGroup.None;
                s.IgnorePrerequisites = false;
                s.Obligatory = false;
                SetField(s, "m_AllFeatures", refs);
                SetField(s, "m_Features", new BlueprintFeatureReference[0]);
                FillEmpty(s);
            });
        }

        internal static T Comp<T>(T c) where T : BlueprintComponent { FillEmpty(c); return c; }
        internal static T Act<T>(T a) where T : GameAction { FillEmpty(a); return a; }
        internal static T Cond<T>(T c) where T : Condition { FillEmpty(c); return c; }
        internal static ActionList List(params GameAction[] a) { return new ActionList { Actions = a }; }
        internal static ContextValue Cv(int v) { return new ContextValue { ValueType = ContextValueType.Simple, Value = v }; }
        internal static ContextValue CvRank(AbilityRankType t) { return new ContextValue { ValueType = ContextValueType.Rank, ValueRank = t }; }
        internal static ContextDurationValue NoDuration() { return new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = Cv(0), BonusValue = Cv(0) }; }
        internal static ContextDurationValue RoundsOfRank(AbilityRankType t) { return new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = Cv(0), BonusValue = CvRank(t) }; }
        internal static ContextDurationValue Rounds(int n) { return new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = Cv(0), BonusValue = Cv(n) }; }

        internal static ContextActionApplyBuff ApplyBuff(string buff, bool permanent, ContextDurationValue duration, bool asChild, bool toCaster)
        {
            var a = Act(new ContextActionApplyBuff { Permanent = permanent, UseDurationSeconds = false, DurationValue = duration, IsFromSpell = false, IsNotDispelable = false, ToCaster = toCaster, AsChild = asChild, SameDuration = false });
            SetField(a, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return a;
        }

        internal static ContextActionRemoveBuff RemoveBuff(string buff, bool toCaster)
        {
            var a = Act(new ContextActionRemoveBuff { ToCaster = toCaster, RemoveRank = false, OnlyFromCaster = false });
            SetField(a, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return a;
        }

        internal static BlueprintBuff Buff(string name, string guid, string key, bool hidden)
        {
            return Create<BlueprintBuff>(name, guid, b =>
            {
                SetField(b, "m_DisplayName", Strings.L(key + ".name"));
                SetField(b, "m_Description", Strings.L(key + ".desc"));
                SetField(b, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(ShortTextNote)
                b.Stacking = StackingType.Replace;
                var flagsType = FindField(typeof(BlueprintBuff), "m_Flags").FieldType;
                SetField(b, "m_Flags", hidden ? Enum.Parse(flagsType, "HiddenInUi") : Enum.ToObject(flagsType, 0));
                b.IsClassFeature = true;
                FillEmpty(b);
                EmptyFxOnRemove(b); // 2026-11-17(Buff.OnRemove @00B0 NRE): 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — FillEmpty 는 PrefabLink 를 채우지 않는다)
            });
        }

        // ContextRankConfig{ClassLevel, [MonkClass], 진행}. 진행 = Custom(20행 표) 또는 Div2/AsIs 등 이름.
        internal static ContextRankConfig RankConfig(AbilityRankType type, ContextRankProgression progression, int[] customTable)
        {
            var c = Comp(new ContextRankConfig());
            SetField(c, "m_Type", type);
            SetField(c, "m_BaseValueType", ContextRankBaseValueType.ClassLevel);
            SetField(c, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(Guids.Game.MonkClass) });
            SetField(c, "m_ExceptClasses", false);
            SetField(c, "m_Progression", progression);
            if (progression == ContextRankProgression.Custom)
            {
                if (customTable == null || customTable.Length != ChimeraRules.Levels.MaxLevel) throw new ArgumentException("ChimeraMonk: custom rank table must have " + ChimeraRules.Levels.MaxLevel + " rows");
                var itemType = FindField(typeof(ContextRankConfig), "m_CustomProgression").FieldType.GetElementType();
                var items = Array.CreateInstance(itemType, customTable.Length);
                for (int i = 0; i < customTable.Length; i++) { var it = Activator.CreateInstance(itemType); SetField(it, "BaseValue", i + 1); SetField(it, "ProgressionValue", customTable[i]); items.SetValue(it, i); }
                SetField(c, "m_CustomProgression", items);
            }
            else if (customTable != null) throw new ArgumentException("ChimeraMonk: custom table given for a non-custom rank progression");
            SetField(c, "m_UseMin", false);
            SetField(c, "m_UseMax", false);
            return c;
        }

        // 엔진 열거값 이름 → 값(순수 계획 표의 문자열). 없는 이름 = 던짐(게임 버전 차이를 조용히 기본값으로 넘기지 않는다).
        internal static T EnumOf<T>(string name) where T : struct
        {
            T v;
            if (string.IsNullOrEmpty(name) || !Enum.TryParse(name, false, out v) || !Enum.IsDefined(typeof(T), v)) throw new InvalidOperationException("ChimeraMonk: '" + name + "' is not a " + typeof(T).Name + " in this game version");
            return v;
        }

        internal static BlueprintAbility Ability(string name, string guid, string key, Action<BlueprintAbility> more)
        {
            // FillEmpty 를 쓰지 않는다(Bulwark BondedShield.cs:91 — 제한 캐시 m_Cached* 를 빈 배열로 채우면 시전 제한이 안 보인다).
            return Create<BlueprintAbility>(name, guid, a =>
            {
                SetField(a, "m_DisplayName", Strings.L(key + ".name"));
                SetField(a, "m_Description", Strings.L(key + ".desc"));
                SetField(a, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(ShortTextNote)
                a.LocalizedDuration = new LocalizedString();
                a.LocalizedSavingThrow = new LocalizedString();
                a.ResourceAssetIds = new string[0];
                more(a);
            });
        }

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

        internal static object GetF(object o, string name) { return Archetype.GetF(o, name); }
    }
}
