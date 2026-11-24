using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
    // 블루프린트 조립 도우미(본편 부품만, Bulwark Rebuke.cs:384-425 의 모양). 수치는 인자로만 받는다(원장 값 — dial: single block).
    internal static class Parts
    {
        // 새 객체의 null 컬렉션·참조 필드를 빈 값으로(직렬화본은 [] / 빈 참조 — 엔진이 null 을 순회하다 죽지 않게). Bulwark Rebuke.FillEmpty 와 같음.
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

        internal static ActionList List(params GameAction[] a) { return new ActionList { Actions = a }; }
        internal static T Act<T>(T a) where T : GameAction { FillEmpty(a); return a; }
        internal static T Cond<T>(T c) where T : Condition { FillEmpty(c); return c; }
        internal static T Comp<T>(T c) where T : BlueprintComponent { FillEmpty(c); return c; }

        internal static Conditional If(Condition c, ActionList t, ActionList f) { return If(new[] { c }, t, f); }
        internal static Conditional If(Condition[] cs, ActionList t, ActionList f)
        {
            return Act(new Conditional { ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = cs }, IfTrue = t, IfFalse = f });
        }

        internal static ContextValue CvZero() { return new ContextValue { ValueType = ContextValueType.Simple }; }
        internal static ContextValue Cv(int v) { return new ContextValue { ValueType = ContextValueType.Simple, Value = v }; }
        internal static ContextValue CvRank(AbilityRankType rank) { return new ContextValue { ValueType = ContextValueType.Rank, ValueRank = rank }; }
        internal static ContextDurationValue NoDuration() { return new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = CvZero(), BonusValue = CvZero() }; }

        internal static DiceType Die(int sides)
        {
            DiceType d;
            if (!Enum.TryParse("D" + sides, out d)) throw new ArgumentException("OathboundRevenant: no engine dice type for d" + sides);
            return d;
        }

        internal static DamageEnergyType Energy(string name)
        {
            DamageEnergyType e;
            if (!Enum.TryParse(name, out e)) throw new ArgumentException("OathboundRevenant: no engine energy type " + name);
            return e;
        }

        internal static ContextActionApplyBuff ApplyBuff(string buff, bool permanent, ContextDurationValue duration, bool asChild, bool toCaster = false)
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

        internal static ContextConditionHasFact HasFact(string fact, bool not)
        {
            var c = Cond(new ContextConditionHasFact { Not = not });
            SetField(c, "m_Fact", Ref<BlueprintUnitFactReference>(fact));
            return c;
        }

        internal static ContextConditionCasterHasFact CasterHasFact(string fact, bool not)
        {
            var c = Cond(new ContextConditionCasterHasFact { Not = not });
            SetField(c, "m_Fact", Ref<BlueprintUnitFactReference>(fact));
            return c;
        }

        // 피해 1건: energy 형식, (rank 또는 고정 수)d sides, IgnoreCritical.
        internal static ContextActionDealDamage DealEnergy(string energy, ContextValue diceCount, int sides, bool ignoreCritical)
        {
            var a = Act(new ContextActionDealDamage
            {
                DamageType = new DamageTypeDescription { Type = DamageType.Energy, Energy = Energy(energy) },
                Duration = NoDuration(),
                Value = new ContextDiceValue { DiceType = Die(sides), DiceCountValue = diceCount, BonusValue = CvZero() },
                Half = false,
                HalfIfSaved = false,
                IgnoreCritical = ignoreCritical,
            });
            SetField(a, "m_Type", Enum.Parse(FindField(typeof(ContextActionDealDamage), "m_Type").FieldType, "Damage"));
            return a;
        }

        // 극성 분기(설계 §8-b): 대상이 음에너지 친화 ? 친화 갈래(신성) : 기본 갈래(음에너지). 주사위 = 같은 식(같은 ContextValue 모양 두 벌).
        internal static Conditional Polarity(DamageBranch b, Func<ContextValue> diceCount, Action<ContextActionDealDamage> tweak = null)
        {
            var aff = DealEnergy(b.AffinityEnergy, diceCount(), b.Sides, b.IgnoreCritical);
            var plain = DealEnergy(b.PlainEnergy, diceCount(), b.Sides, b.IgnoreCritical);
            if (tweak != null) { tweak(aff); tweak(plain); }
            // FX(2026-11-15 승인, FxTable): 신성 갈래(음에너지 친화 대상)에만 악 응징 FX 1회 — 대상 위. 피해 [0] 위치 유지(Strike.VerifyPolarity 가 [1] 을 표와 대조).
            return If(HasFact(Guids.Game.NegativeEnergyAffinity, false), List(aff, OathFx.SpawnAction(FxTable.Divine + b.Key)), List(plain));
        }

        internal static ContextRankConfig RankClassLevel(AbilityRankType type, ContextRankProgression progression)
        {
            var c = Comp(new ContextRankConfig());
            SetField(c, "m_Type", type);
            SetField(c, "m_BaseValueType", ContextRankBaseValueType.ClassLevel);
            SetField(c, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(Guids.Game.PaladinClass) });
            SetField(c, "m_ExceptClasses", false);
            SetField(c, "m_Progression", progression);
            SetField(c, "m_UseMin", false);
            SetField(c, "m_UseMax", false);
            return c;
        }

        // 사용자 진행 표(레벨 i → table[i-1]) — CustomProgressionItem{BaseValue = 레벨, ProgressionValue = 값}.
        internal static ContextRankConfig RankCustom(AbilityRankType type, int[] table)
        {
            var c = RankClassLevel(type, ContextRankProgression.Custom);
            var itemType = FindField(typeof(ContextRankConfig), "m_CustomProgression").FieldType.GetElementType();
            var items = Array.CreateInstance(itemType, table.Length);
            for (int i = 0; i < table.Length; i++)
            {
                var it = Activator.CreateInstance(itemType);
                SetField(it, "BaseValue", i + RevenantRules.Levels.MinLevel);
                SetField(it, "ProgressionValue", table[i]);
                items.SetValue(it, i);
            }
            SetField(c, "m_CustomProgression", items);
            return c;
        }

        internal static BlueprintBuff NewBuff(string name, string guid, string nameKey, string descKey, bool hidden)
        {
            return Create<BlueprintBuff>(name, guid, b =>
            {
                SetField(b, "m_DisplayName", Strings.L(nameKey));
                SetField(b, "m_Description", Strings.L(descKey));
                Tooltips.NoShortDescription(b);   // 2026-11-16: LocalizedString 필드 null 금지(Tooltips 머리말)
                b.Stacking = StackingType.Replace;
                var flagsType = FindField(typeof(BlueprintBuff), "m_Flags").FieldType;
                SetField(b, "m_Flags", hidden ? Enum.Parse(flagsType, "HiddenInUi") : Enum.ToObject(flagsType, 0));
                b.IsClassFeature = true;
                // 2026-11-17 결함(GameLogFull: SmiteLinkComponent.HandleBuffDidRemoved → Buff.Remove → 자식 Buff.OnRemove @00B0 NRE):
                // 엔진 Buff.OnRemove IL_0095~00B7 은 입자 주인(m_ParticleEffectOwner)이 있으면 Blueprint.FxOnRemove.Load 를 null 검사 없이 부른다.
                // 본편 jbp 의 빈 FxOnRemove 는 읽을 때 빈 링크가 되지만 코드로 만든 버프는 null 이다 → 빈 링크를 넣는다(선례 PackWarden Aspects.cs:87, ChaosWarrior BuildKit.cs:107).
                // FillEmpty 는 PrefabLink 를 채우지 않는다(BlueprintReferenceBase·배열·List 만). 전수 되읽기 = BuffFx.Require(BuildAll).
                b.FxOnRemove = new PrefabLink();
                FillEmpty(b);
            });
        }

        internal static BlueprintFeature NewFeature(string name, string guid, string nameKey, string descKey, bool hidden)
        {
            return Create<BlueprintFeature>(name, guid, f =>
            {
                SetField(f, "m_DisplayName", Strings.L(nameKey));
                SetField(f, "m_Description", Strings.L(descKey));
                Tooltips.NoShortDescription(f);   // 2026-11-16: LocalizedString 필드 null 금지(Tooltips 머리말)
                f.IsClassFeature = true;
                f.HideInUI = hidden;
                FillEmpty(f);
            });
        }
    }

    // 빌드 끝 전수 되읽기(2026-11-17): 우리가 Create 로 만든 모든 버프의 FxOnRemove 가 null 이 아니어야 한다(Parts.NewBuff 주석의 엔진 NRE).
    // 생성 경로 = Parts.NewBuff · DivineBond 유대 버프(본편 불꽃 유대 버프 필드 복사). 하나라도 null 이면 던진다 → 아키타입을 등록하지 않는다(BuildAll 실패 계약).
    public static class BuffFx
    {
        public static int Checked { get; private set; }

        public static void Require()
        {
            var buffs = OwnBlueprints.OfType<BlueprintBuff>().ToList();
            if (buffs.Count == 0) throw new InvalidOperationException("OathboundRevenant: BuffFx.Require found no buffs we created (called before the builders?)");
            var bad = buffs.Where(b => b.FxOnRemove == null).Select(b => b.name).ToList();
            if (bad.Count != 0) throw new InvalidOperationException("OathboundRevenant: buff(s) with a null FxOnRemove (engine Buff.OnRemove IL_00B0 would NRE on removal): " + string.Join(", ", bad.ToArray()));
            Checked = buffs.Count;
        }
    }
}
