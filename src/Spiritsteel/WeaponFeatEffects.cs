using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace Spiritsteel
{
    public class WeaponFeatAttack : WeaponEnchantmentLogic,
        IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
        IInitiatorRulebookHandler<RuleAttackRoll>, IInitiatorRulebookHandler<RuleCalculateAttackBonus>
    {
        public WeaponFeatKind Kind;
        public int Value;

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if ((Kind == WeaponFeatKind.Focus || Kind == WeaponFeatKind.GreaterFocus) && evt.Weapon == Owner &&
                !WeaponFeats.HasNative(evt.Initiator, Kind, Owner.Blueprint.Category))
                evt.AddModifier(1, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if ((Kind != WeaponFeatKind.Specialization && Kind != WeaponFeatKind.GreaterSpecialization) || evt.Weapon != Owner) return;
            int nativeBonus = WeaponFeats.HasNative(evt.Initiator, Kind, Owner.Blueprint.Category) ? 2 : 0;
            int bonus = WeaponFeatRow.AdditionalBonus(Value, nativeBonus);
            if (bonus > 0) evt.AddDamageModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (Kind == WeaponFeatKind.Critical && evt.Weapon == Owner && !WeaponFeats.HasNative(evt.Initiator, Kind))
                evt.CriticalConfirmationBonus += 4;
        }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (Kind == WeaponFeatKind.Precise && evt.Weapon == Owner)
                evt.IgnoreRangedPenalty = true;
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
        public void OnEventDidTrigger(RuleAttackRoll evt) { }
        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
    }

    public class WeaponFeatGiantSlayer : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleCalculateAttackBonus>
    {
        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (evt.Weapon == Owner && evt.Target.State.Size >= Size.Large)
                evt.AddModifier(2, Fact, ModifierDescriptor.Insight);
        }
        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
    }

    public class WeaponFeatReflexEvasion : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleSavingThrow>
    {
        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            if (evt.StatType == StatType.SaveReflex) evt.Evasion = true;
        }
        public void OnEventDidTrigger(RuleSavingThrow evt) { }
    }

    public class WeaponFeatJudgment : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RulePrepareDamage>
    {
        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (evt.DamageBundle.Weapon == Owner && evt.DamageBundle.WeaponDamage != null &&
                evt.Target.Descriptor.Alignment.ValueRaw.HasComponent(AlignmentComponent.Evil))
                evt.Add(new EnergyDamage(new DiceFormula(2, DiceType.D6), 0, DamageEnergyType.Holy));
        }
        public void OnEventDidTrigger(RulePrepareDamage evt) { }
    }

    public class WeaponFeatPenetration : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleCalculateDamage>
    {
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (evt.DamageBundle.Weapon == Owner && evt.DamageBundle.WeaponDamage is PhysicalDamage physical)
                physical.ReductionPenalty.Add(new Kingmaker.RuleSystem.Rules.Modifier(10, Fact, ModifierDescriptor.Competence));
        }
        public void OnEventDidTrigger(RuleCalculateDamage evt) { }
    }

    public class WeaponFeatBaneDamage : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RulePrepareDamage>
    {
        public BlueprintFeatureReference TargetType;

        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (evt.DamageBundle.Weapon == Owner && evt.DamageBundle.WeaponDamage is PhysicalDamage &&
                evt.Target.Descriptor.HasFact(TargetType.Get()))
                evt.Add(new ForceDamage(new ModifiableDiceFormula(new DiceFormula(2, DiceType.D6)), 0));
        }

        public void OnEventDidTrigger(RulePrepareDamage evt) { }
    }

    public class WeaponFeatAttackAgainstCondition : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleCalculateAttackBonus>
    {
        public UnitCondition Condition;
        public UnitCondition? Alternative;

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (evt.Weapon == Owner && (evt.Target.State.HasCondition(Condition) ||
                (Alternative.HasValue && evt.Target.State.HasCondition(Alternative.Value))))
                evt.AddModifier(2, Fact, ModifierDescriptor.Insight);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
    }

    public class WeaponFeatHunter : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleCalculateAttackBonus>
    {
        public BlueprintFeatureReference TargetType;

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (evt.Weapon == Owner && evt.Target.Descriptor.HasFact(TargetType.Get()))
                evt.AddModifier(2, Fact, ModifierDescriptor.Insight);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
    }

    public class WeaponFeatTacticalAttack : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleCalculateAttackBonus>
    {
        public WeaponFeatKind Kind;

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (evt.Weapon != Owner) return;
            // Match this calculation to its own roll. A triggered secondary attack may be on the same target.
            var roll = Rulebook.CurrentContext?.EventStack.OfType<RuleAttackRoll>()
                .FirstOrDefault(r => ReferenceEquals(r.AttackBonusRule, evt));
            var attack = roll?.RuleAttackWithWeapon;
            if (attack != null && ((Kind == WeaponFeatKind.Charging && attack.IsCharge) ||
                (Kind == WeaponFeatKind.Opportunist && attack.IsAttackOfOpportunity)))
                evt.AddModifier(2, Fact, ModifierDescriptor.Insight);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
    }

    // AddItemModifierUnique identifies each bonus by the enchantment runtime. Learning or losing the
    // original character feat while wielding the weapon changes only this runtime's contribution.
    public class WeaponFeatWearerStat : WeaponEnchantmentLogic, IUnitGainFactHandler, IUnitLostFactHandler
    {
        public WeaponFeatKind Kind;
        public StatType Stat;
        public int Value;

        protected override void OnTurnOn()
        {
            var wielder = Owner.Wielder;
            if (wielder != null) Update(!WeaponFeats.HasNative(wielder.Unit, Kind));
        }

        protected override void OnTurnOff() { Update(false); }

        public void HandleUnitGainFact(EntityFact fact)
        {
            if (fact.Blueprint == WeaponFeats.Native[Kind] && fact.Owner == Owner.Wielder?.Unit) Update(false);
        }

        public void HandleUnitLostFact(EntityFact fact)
        {
            if (fact.Blueprint == WeaponFeats.Native[Kind] && fact.Owner == Owner.Wielder?.Unit)
                // IUnitLostFactHandler runs from OnFactWillDetach, while the departing fact
                // is still in the collection. Keep any independently granted second copy.
                Update(!Owner.Wielder.Unit.Facts.List.Any(other => other != fact && other.Blueprint == fact.Blueprint));
        }

        private void Update(bool apply)
        {
            var stat = Owner.Wielder?.Stats.GetStat(Stat);
            if (stat == null) return;
            stat.RemoveModifiersFrom(Runtime);
            if (apply)
                stat.AddItemModifierUnique(Value, Runtime, Owner,
                    Kind == WeaponFeatKind.Shield ? ModifierDescriptor.ShieldFocus : ModifierDescriptor.UntypedStackable);
        }
    }

    public class WeaponFeatTrip : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleCalculateCMB>
    {
        private static readonly RulebookEvent.CustomDataKey WeaponKey = new RulebookEvent.CustomDataKey("Spiritsteel.TripWeapon");

        internal static void Mark(RuleCombatManeuver maneuver, ItemEntityWeapon weapon) { maneuver.SetCustomData(WeaponKey, weapon); }

        public void OnEventAboutToTrigger(RuleCalculateCMB evt)
        {
            if (evt.Type != CombatManeuver.Trip || WeaponFeats.HasNative(evt.Initiator, WeaponFeatKind.Trip)) return;
            var maneuver = Rulebook.CurrentContext.EventStack.OfType<RuleCombatManeuver>().LastOrDefault();
            if (maneuver == null || maneuver.Initiator != evt.Initiator || maneuver.Target != evt.Target) return;
            ItemEntityWeapon weapon;
            if (!maneuver.TryGetCustomData(WeaponKey, out weapon))
            {
                weapon = maneuver.AttackRule?.Weapon;
                // Standalone Trip creates an attack bonus rule for the empty hand. Only the actual
                // basic maneuver may use the wielder's primary weapon here (a spell can also trip).
                if (weapon == evt.Initiator.Body.EmptyHandWeapon && maneuver.Reason?.Context?.SourceAbility == WeaponFeats.BasicTrip)
                    weapon = evt.Initiator.Body.PrimaryHand.MaybeWeapon;
            }
            if (weapon != null && (weapon == Owner || weapon == Owner.Second))
                evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateCMB evt) { }
    }
}
