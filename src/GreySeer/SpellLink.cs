using System;
using System.Linq;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using TargetWrapper = Kingmaker.Utility.TargetWrapper;
using WotrClassMod.Common;

namespace GreySeer
{
    public class WeaponRequirement : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return Weapons.SwordHand(caster) != null; }
        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "equipment"); }
    }

    // Unit-owned, transient. A reload discards queued target/command references.
    public class SwordLinkPart : UnitPart
    {
        [NonSerialized] internal UnitEntityData Target;
        [NonSerialized] internal UnitUseAbility Command;
        [NonSerialized] internal StrikePayment Payment;
        [NonSerialized] internal int SlotLevel;
    }

    public class DesignateTarget : AbilityApplyEffect
    {
        public bool Guarded;
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.Caster;
            var foe = target.Unit;
            if (foe == null || foe.State.IsDead || !caster.IsEnemy(foe) || Weapons.SwordHand(caster) == null) return;
            var link = caster.Ensure<SwordLinkPart>();
            if (link.Command != null) return;
            link.Target = foe;
            link.Payment = new StrikePayment(Guarded);
        }
    }

    public class SpellLink : UnitFactComponentDelegate, IUnitCommandStartHandler, IUnitCommandEndHandler, IUnitTryCastingDefensivelyHandler, IUnitCombatHandler
    {
        public void HandleUnitCommandDidStart(UnitCommand command)
        {
            var cast = command as UnitUseAbility;
            if (cast == null || cast.Executor != Owner || cast.Cutscene) return;
            var link = Owner.Get<SwordLinkPart>();
            if (link == null || link.Command != null) return;
            // OnStart may substitute the displayed ability. OnAction spends the original.
            var spell = (AbilityData)Blueprints.OriginalAbility.GetValue(cast) ?? cast.Ability;
            int? slot = SlotOfOwnSpell(spell);
            if (!slot.HasValue) return;
            if (link.Target == null || !link.Target.IsInGame || link.Target.State.IsDead || !Owner.IsEnemy(link.Target) || Weapons.SwordHand(Owner) == null)
            {
                Owner.Remove<SwordLinkPart>();
                return;
            }
            if (slot.Value < 0 || slot.Value > 9)
            {
                WotrClassMod.Common.ModContext.LogError("Grey Seer: no link cost defined for slot level " + slot.Value);
                Owner.Remove<SwordLinkPart>();
                return;
            }
            link.SlotLevel = slot.Value;
            link.Command = cast;
        }

        int? SlotOfOwnSpell(AbilityData spell)
        {
            if (!spell.Blueprint.IsSpell || spell.SourceItem != null || spell.SourceItemForTouch != null) return null;
            if (spell.Spellbook == null)
            {
                // The action bar also exposes cantrips granted as facts. Match the granted
                // ability instance, since another class or an item can grant the same blueprint.
                if (!spell.Blueprint.IsCantrip || spell.Fact == null) return null;
                var feature = Owner.GetFact(Blueprints.Cantrips);
                if (feature == null) return null;
                return feature.SelectComponentsWithRuntime<AddFacts>()
                    .Any(c => c.GetData<AddFactsData>().AppliedFacts.Contains(spell.Fact)) ? (int?)0 : null;
            }
            if (spell.Spellbook.Owner != Owner.Descriptor || spell.Spellbook.Blueprint != Blueprints.Arcanist.Spellbook) return null;
            var slot = spell;
            while (slot.ConvertedFrom != null) slot = slot.ConvertedFrom;
            if (slot.Spellbook != spell.Spellbook || slot.SourceItem != null) return null;
            // Same effective level (including metamagic) as Spellbook.SpendInternal.
            return slot.Spellbook.GetSpellLevel(slot);
        }

        public void HandleUnitTryCastingDefensively(AbilityData ability, ref bool forceSuccess)
        {
            var link = Owner.Get<SwordLinkPart>();
            if (link == null || link.Command == null || !ReferenceEquals(link.Command.Ability, ability)) return;
            if (Weapons.SwordHand(Owner) == null) { Owner.Remove<SwordLinkPart>(); return; }
            if (link.Payment.Protect(link.SlotLevel, TryPay)) forceSuccess = true;
        }

        public void HandleUnitCommandDidEnd(UnitCommand command)
        {
            if (command.Executor != Owner) return;
            var link = Owner.Get<SwordLinkPart>();
            if (link == null || !ReferenceEquals(command, link.Command)) return;
            var cast = link.Command;
            var target = link.Target;
            var payment = link.Payment;
            Owner.Remove<SwordLinkPart>(); // Clear before nested attack events can run.
            if (cast.Result != UnitCommand.ResultType.Success || cast.ExecutionProcess == null ||
                Owner.State.IsDead || !Owner.State.CanAct || target == null || target.State.IsDead ||
                !target.IsInGame || !Owner.IsEnemy(target)) return;
            var hand = Weapons.SwordHand(Owner);
            bool inReach = hand != null && Owner.View != null && target.View != null && Owner.IsReach(target, hand);
            if (!payment.Finish(inReach, TryPay)) return;
            var attack = new RuleAttackWithWeapon(Owner, target, hand.MaybeWeapon, 0) {
                ExtraAttack = true, AttackNumber = 1, AttacksCount = 1
            };
            Rulebook.Trigger(attack);
        }

        bool TryPay(int amount)
        {
            var resources = Owner.Descriptor.Resources;
            if (resources.GetResourceAmount(Blueprints.Reservoir) < amount) return false;
            resources.Spend(Blueprints.Reservoir, amount);
            return true;
        }

        public void HandleUnitJoinCombat(UnitEntityData unit) { }
        public void HandleUnitLeaveCombat(UnitEntityData unit) { if (unit == Owner) Owner.Remove<SwordLinkPart>(); }
        protected override void OnTurnOff() { Owner.Remove<SwordLinkPart>(); }
    }
}
