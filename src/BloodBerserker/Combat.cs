using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Newtonsoft.Json;
using TurnBased.Controllers;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using WotrClassMod.Common;
using ModContext = WotrClassMod.Common.ModContext;

namespace BloodBerserker
{
    public class BloodrageRequired : BlueprintComponent, IAbilityCasterRestriction
    {
        internal static bool Active(UnitEntityData u) { return u != null && u.Buffs.HasFact(Blueprints.Rage); }
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return Active(caster); }
        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "needsRage"); }
    }

    public class ChainTarget : BlueprintComponent, IAbilityTargetRestriction
    {
        public bool Extended;
        internal static float Gap(UnitEntityData caster, UnitEntityData foe)
        {
            return caster.DistanceTo(foe) - caster.Corpulence - foe.Corpulence;
        }
        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
        {
            var foe = target.Unit;
            return foe != null && !foe.State.IsDead && caster.IsEnemy(foe) &&
                (!Extended || Gap(caster, foe) <= new Feet(Rules.ChainFeet).Meters || caster.HPLeft > Rules.BloodPrice);
        }
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
        {
            return Strings.Resolve(ModContext.StringPrefix + "needsLife");
        }
    }

    // Pay at launch, including a missed roll. Recheck after a queued command or a target move.
    public class ChainFlight : AbilityDeliverProjectile
    {
        public bool Extended;
        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var u = context.Caster;
            var foe = target.Unit;
            if (!BloodrageRequired.Active(u) || foe == null || foe.State.IsDead || !u.IsEnemy(foe)) yield break;
            float gap = ChainTarget.Gap(u, foe);
            if (gap > new Feet(Extended ? Rules.ExtendedFeet : Rules.ChainFeet).Meters) yield break;
            if (Extended && gap > new Feet(Rules.ChainFeet).Meters)
            {
                if (u.HPLeft <= Rules.BloodPrice) yield break;
                u.Damage += Rules.BloodPrice;
            }
            using (var flight = base.Deliver(context, target))
                while (flight.MoveNext()) yield return flight.Current;
        }
    }

    public class ChainImpact : AbilityApplyEffect
    {
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var u = context.Caster;
            var foe = target.Unit;
            if (!BloodrageRequired.Active(u) || foe == null || foe.State.IsDead || !u.IsEnemy(foe)) return;
            bool pull = Rules.PullTarget((int)u.State.Size, (int)foe.State.Size);
            if (pull) context.TriggerRule(new RuleCombatManeuver(u, foe, CombatManeuver.Pull, null));
            else
            {
                var delta = foe.Position - u.Position;
                float gap = delta.magnitude - u.Corpulence - foe.Corpulence;
                if (gap > 0) u.Ensure<UnitPartForceMove>().Push(delta.normalized, gap, false);
            }
            BloodTether.Attach(u, foe, pull ? foe : u);
            // The marker lives on this berserker alone. Its saved caster reference identifies the marked foe.
            u.Buffs.GetBuff(Blueprints.Knot)?.Remove();
            TimeSpan? window = CombatController.IsInTurnBasedCombat() ? (TimeSpan?)null : new Rounds(1).Seconds;
            u.Buffs.AddBuff(Blueprints.Knot, foe, window);
            ChargeSpell.Discharge(u, target);
        }
    }

    // A turn-based round's clock can tick before the owner gets to spend their next action.
    public class KnotWindow : UnitBuffComponentDelegate<KnotWindow.WindowData>, ITurnBasedModeHandler,
        ITurnBasedUnitTurnEndedHandler, ITurnBasedModeEnabledHandler, IUnitCombatHandler
    {
        public class WindowData { [JsonProperty] public bool NextTurnStarted; }
        public void HandleTurnStarted(UnitEntityData unit) { if (unit == Owner) Data.NextTurnStarted = true; }
        public void HandleUnitTurnEnded(UnitEntityData unit) { if (unit == Owner && Data.NextTurnStarted) Buff.Remove(); }
        public void UpdateMandatoryTurnBasedModeState() { }
        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            if (!enabled && Buff.TimeLeft > new Rounds(1).Seconds) Buff.SetDuration(new Rounds(1).Seconds);
        }
        public void HandleUnitLeaveCombat(UnitEntityData unit) { if (unit == Owner) Buff.Remove(); }
        public void HandleUnitJoinCombat(UnitEntityData unit) { }
        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck roll) { }
    }

    public class KnotCombat : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>, IUnitBuffHandler
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            var mark = Owner.Buffs.GetBuff(Blueprints.Knot);
            if (mark == null || mark.Context.MaybeCaster != evt.Target || evt.Weapon == null || !evt.Weapon.Blueprint.IsMelee ||
                evt.AttackRoll == null || !evt.AttackRoll.IsHit || !BloodrageRequired.Active(Owner)) return;
            mark.Remove();
            Owner.Buffs.GetBuff(Blueprints.Ward)?.Remove();
            Owner.Buffs.AddBuff(Blueprints.Ward, Owner, new Rounds(1).Seconds);
        }

        public void HandleBuffDidAdded(Buff buff) { }
        public void HandleBuffDidRemoved(Buff buff)
        {
            if (buff.Owner == Owner && buff.Blueprint == Blueprints.Rage) Clear();
        }
        protected override void OnTurnOff() { Clear(); }
        void Clear()
        {
            Owner.Buffs.GetBuff(Blueprints.Knot)?.Remove();
            Owner.Buffs.GetBuff(Blueprints.Ward)?.Remove();
            Owner.Buffs.GetBuff(Blueprints.ShockBuff)?.Remove();
            Owner.Buffs.GetBuff(Blueprints.VampBuff)?.Remove();
        }
    }

    public class BloodWeapon : UnitFactComponentDelegate, IInitiatorRulebookHandler<RulePrepareDamage>
    {
        internal static bool Eligible(ItemEntityWeapon weapon)
        {
            return weapon != null && weapon.Blueprint.IsMelee && !weapon.Blueprint.IsNatural && !weapon.Blueprint.IsUnarmed;
        }

        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            var hit = evt.ParentRule.AttackRoll;
            var weapon = evt.DamageBundle.Weapon;
            var physical = evt.DamageBundle.WeaponDamage as PhysicalDamage;
            if (hit == null || !hit.IsHit || !Eligible(weapon) || physical == null || hit.Weapon != weapon ||
                (weapon != Owner.Body.PrimaryHand.MaybeWeapon && weapon != Owner.Body.SecondaryHand.MaybeWeapon) ||
                !BloodrageRequired.Active(Owner)) return;
            var blood = DamageForHit(physical, hit, Owner.Progression.GetClassLevel(Blueprints.Bloodrager));
            blood.SourceFact = Fact;
            evt.Add(blood);
        }

        internal static PhysicalDamage DamageForHit(PhysicalDamage weapon, RuleAttackRoll hit, int level)
        {
            bool critical = hit.IsCriticalConfirmed && !hit.ImmuneToCriticalHit && !hit.FortificationNegatesCriticalHit;
            int dice = Rules.WeaponDice(level, critical);
            return new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(dice, DiceType.D6)), 0, weapon.Form);
        }
        public void OnEventDidTrigger(RulePrepareDamage evt) { }
    }
}
