using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    //   [시전자 표지(VenomSpent) 없음 → 대상에 VenomBuff(6라운드) + 시전자에 VenomSpent(영구)]. 표지는 독 특성의 AddFactContextActions.NewRound/Deactivated 가 뗀다(Bulwark Rebuke.cs:128-134).
    // VenomBuff: 매 라운드 인내 내성(DC = 특성/버프의 ContextCalculateAbilityParamsBasedOnClass{Monk, Wisdom} = 10 + ½ 몽크 레벨 + 지혜),
    //   변이약 중이면 +2(ContextActionSavingThrow.m_ConditionalDCIncrease — 계산 규칙에 닿는 DC 보너스, Step 0), 실패 시 건강 1d2 능력치 피해.
    public static class Venom
    {
        public static BlueprintBuff Buff, Spent;

        public static void Build()
        {
            if (Buff != null) throw new InvalidOperationException("ChimeraMonk: Venom.Build() called twice");
            var feature = Mutations.Get("Venom");
            Spent = Fab.Buff("ChimeraVenomSpent", Guids.VenomSpent, "venom.spent", true);
            Buff = Fab.Buff("ChimeraVenomBuff", Guids.VenomBuff, "venom.buff", false);

            var anyMutagen = new ConditionsChecker { Operation = Operation.Or, Conditions = Mutagen.Buffs.Select(b => (Condition)CasterHas(b.AssetGuid.ToString(), false)).ToArray() };
            var save = Fab.Act(new ContextActionSavingThrow { Type = Fab.EnumOf<SavingThrowType>(VenomPlan.Save), FromBuff = true, HasCustomDC = false });
            SetField(save, "m_ConditionalDCIncrease", new[] { new ContextActionSavingThrow.ConditionalDCIncrease { Condition = anyMutagen, Value = Fab.Cv(VenomPlan.DcMutagenBonus) } });
            var damage = Fab.Act(new ContextActionDealDamage { AbilityType = Fab.EnumOf<StatType>(VenomPlan.Stat), Drain = false, Duration = Fab.NoDuration(),
                Value = new ContextDiceValue { DiceType = Fab.EnumOf<DiceType>("D" + VenomPlan.Die.Sides), DiceCountValue = Fab.Cv(VenomPlan.Die.Count), BonusValue = Fab.Cv(0) } });
            // ContextActionDealDamage.Type 은 비공개 중첩 열거 → 필드 형식에서 이름으로(없는 이름이면 Enum.Parse 가 던짐).
            SetField(damage, "m_Type", Enum.Parse(FindField(typeof(ContextActionDealDamage), "m_Type").FieldType, "AbilityDamage"));
            save.Actions = Fab.List(Fab.Act(new ContextActionConditionalSaved { Succeed = Fab.List(), Failed = Fab.List(damage) }));
            var rounds = Fab.Comp(new AddFactContextActions { Activated = Fab.List(), Deactivated = Fab.List(), NewRound = Fab.List(save), Dispose = Fab.List() });
            AddComponents(Buff, rounds, Params(), Fab.Comp(new SpellDescriptorComponent { Descriptor = new SpellDescriptorWrapper(Fab.EnumOf<SpellDescriptor>("Poison")) }));

            var apply = Fab.ApplyBuff(Guids.VenomBuff, false, Fab.Rounds(VenomPlan.Rounds), false, false);
            var mark = Fab.ApplyBuff(Guids.VenomSpent, true, Fab.NoDuration(), false, true);
            var gate = Fab.Act(new Conditional { ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { CasterHas(Guids.VenomSpent, true) } }, IfTrue = Fab.List(apply, mark), IfFalse = Fab.List() });
            var trigger = Fab.Comp(new AddInitiatorAttackWithWeaponTrigger { OnlyHit = true, AllNaturalAndUnarmed = true, ActionsOnInitiator = false, Action = Fab.List(gate) });
            var clear = Fab.Comp(new AddFactContextActions { Activated = Fab.List(), Deactivated = Fab.List(Fab.RemoveBuff(Guids.VenomSpent, false)), NewRound = Fab.List(Fab.RemoveBuff(Guids.VenomSpent, false)), Dispose = Fab.List() });
            AddComponents(feature, trigger, clear, Params());

            var diff = Readback.VenomDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: venom readback: " + string.Join(" | ", diff));
        }

        static ContextConditionCasterHasFact CasterHas(string fact, bool not)
        {
            var c = Fab.Cond(new ContextConditionCasterHasFact { Not = not });
            SetField(c, "m_Fact", Ref<BlueprintUnitFactReference>(fact));
            return c;
        }

        static ContextCalculateAbilityParamsBasedOnClass Params()
        {
            var p = Fab.Comp(new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Wisdom, UseKineticistMainStat = false });
            SetField(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.MonkClass));
            return p;
        }
    }
}
