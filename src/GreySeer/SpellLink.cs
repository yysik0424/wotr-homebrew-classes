using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using TargetWrapper = Kingmaker.Utility.TargetWrapper;
using WotrClassMod.Common;

namespace GreySeer
{
    public class WeaponRequirement : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return Weapons.StrikeHand(caster) != null; }
        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "equipment"); }
    }

    // Unit-owned, transient. A reload discards queued target/command references.
    public class SwordLinkPart : UnitPart
    {
        [NonSerialized] internal UnitEntityData Target;
        [NonSerialized] internal UnitUseAbility Command;
        [NonSerialized] internal StrikePayment Payment;
        [NonSerialized] internal int SlotLevel;
        [NonSerialized] internal bool StrikeFirst;
        [NonSerialized] internal bool OpeningAttempted;
    }

    // A quickened spell can start while another spell's projectiles are still in flight.
    public class PendingSwordStrikes : UnitPart
    {
        [NonSerialized] internal readonly Dictionary<AbilityExecutionContext, SwordLinkPart> Casts =
            new Dictionary<AbilityExecutionContext, SwordLinkPart>();
        [NonSerialized] internal UnitCommand TouchCommand;
        [NonSerialized] internal SwordLinkPart TouchLink;
    }

    public class DesignateTarget : AbilityApplyEffect
    {
        public bool Guarded;
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.Caster;
            var foe = target.Unit;
            if (foe == null || foe.State.IsDead || !caster.IsEnemy(foe) || Weapons.StrikeHand(caster) == null) return;
            var link = caster.Ensure<SwordLinkPart>();
            if (link.Command != null) return;
            link.Target = foe;
            link.Payment = new StrikePayment(Guarded);
        }
    }

    public class SpellLink : UnitFactComponentDelegate, IUnitCommandStartHandler, IUnitCommandEndHandler, IUnitTryCastingDefensivelyHandler, IUnitCombatHandler, IActivatableAbilityStartHandler,
        IInitiatorRulebookHandler<RuleCastSpell>, IAbilityExecutionProcessHandler
    {
        public void HandleActivatableAbilityStart(ActivatableAbility ability)
        {
            if (ability.Owner != Owner.Descriptor) return;
            var mode = ability.Blueprint;
            if (mode != Blueprints.AutoStrike && mode != Blueprints.AutoGuarded) return;
            var other = mode == Blueprints.AutoStrike ? Blueprints.AutoGuarded : Blueprints.AutoStrike;
            foreach (var toggle in Owner.ActivatableAbilities.Enumerable)
                if (toggle.Blueprint == other && toggle.IsOn) toggle.IsOn = false;
        }

        bool? AutoGuardedMode()
        {
            foreach (var toggle in Owner.ActivatableAbilities.Enumerable)
            {
                if (!toggle.IsOn || !toggle.IsStarted) continue;
                if (toggle.Blueprint == Blueprints.AutoStrike) return false;
                if (toggle.Blueprint == Blueprints.AutoGuarded) return true;
            }
            return null;
        }

        bool CanStrike(UnitEntityData target, HandSlot sword)
        {
            return sword != null && target != null && target.IsInGame && !target.State.IsDead &&
                Owner.IsEnemy(target) && !UnitCommand.CommandTargetUntargetable(Owner, target) &&
                Owner.View != null && target.View != null && Owner.IsReach(target, sword);
        }

        UnitEntityData AutoTarget(UnitEntityData spellTarget, HandSlot sword)
        {
            if (CanStrike(spellTarget, sword)) return spellTarget;
            if (!Owner.IsInCombat) return null;
            // Only opponents engaged with this caster. Never draw an idle unit into a buff cast.
            var combat = Owner.CombatState;
            return combat.EngagedUnits.Concat(combat.EngagedBy)
                .Where(foe => foe.IsInCombat && Owner.Memory.ContainsVisible(foe) && CanStrike(foe, sword))
                .OrderBy(foe => Owner.SqrDistanceTo(foe))
                .ThenBy(foe => foe.UniqueId, StringComparer.Ordinal).FirstOrDefault();
        }

        public void HandleUnitCommandDidStart(UnitCommand command)
        {
            var cast = command as UnitUseAbility;
            if (cast == null || cast.Executor != Owner || cast.Cutscene) return;
            // The engine automatically queues the delivery of a charged touch spell.
            // It belongs to the original spell and must never buy a second strike.
            if (ReferenceEquals(cast, Owner.Get<PendingSwordStrikes>()?.TouchCommand) || cast.Ability?.StickyTouch != null) return;
            var link = Owner.Get<SwordLinkPart>();
            if (link?.Command != null) return;
            // OnStart may substitute the displayed ability. OnAction spends the original.
            var spell = (AbilityData)Blueprints.OriginalAbility.GetValue(cast) ?? cast.Ability;
            int? slot = SlotOfOwnSpell(Owner, spell);
            if (!slot.HasValue) return;
            var sword = Weapons.StrikeHand(Owner);
            var first = Owner.ActivatableAbilities.GetFact(Blueprints.BladeFirst);
            bool strikeFirst = first != null && first.IsOn && first.IsStarted;
            if (link?.Payment != null)
            {
                // A manual designation owns this cast, including its choice of guarded price.
                if (sword == null || link.Target == null || !link.Target.IsInGame || link.Target.State.IsDead || !Owner.IsEnemy(link.Target))
                {
                    Owner.Remove<SwordLinkPart>();
                    return;
                }
            }
            else
            {
                if (sword == null) return;
                var guarded = AutoGuardedMode();
                if (!guarded.HasValue && !strikeFirst) return;
                var target = AutoTarget(cast.TargetUnit, sword);
                if (target == null) return;
                link = Owner.Ensure<SwordLinkPart>();
                link.Target = target;
                link.Payment = new StrikePayment(guarded ?? false);
            }
            if (slot.Value < 0 || slot.Value > 9)
            {
                WotrClassMod.Common.ModContext.LogError("Grey Seer: no link cost defined for slot level " + slot.Value);
                Owner.Remove<SwordLinkPart>();
                return;
            }
            link.SlotLevel = slot.Value;
            link.Command = cast;
            link.StrikeFirst = strikeFirst;
        }

        internal static int? SlotOfOwnSpell(UnitEntityData owner, AbilityData spell)
        {
            if (spell == null || spell.Caster != owner.Descriptor || !spell.Blueprint.IsSpell || spell.SourceItem != null || spell.SourceItemForTouch != null) return null;
            if (spell.Spellbook == null)
            {
                // The action bar also exposes cantrips granted as facts. Match the granted
                // ability instance, since another class or an item can grant the same blueprint.
                if (!spell.Blueprint.IsCantrip || spell.Fact == null) return null;
                var feature = owner.GetFact(Blueprints.Cantrips);
                if (feature == null) return null;
                return feature.SelectComponentsWithRuntime<AddFacts>()
                    .Any(c => c.GetData<AddFactsData>().AppliedFacts.Contains(spell.Fact)) ? (int?)0 : null;
            }
            if (spell.Spellbook.Owner != owner.Descriptor || spell.Spellbook.Blueprint != Blueprints.Arcanist.Spellbook) return null;
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
            if (Weapons.StrikeHand(Owner) == null) { Owner.Remove<SwordLinkPart>(); return; }
            if (link.Payment.Protect(link.SlotLevel, TryPay)) forceSuccess = true;
        }

        public void HandleUnitCommandDidEnd(UnitCommand command)
        {
            if (command.Executor != Owner) return;
            var pending = Owner.Get<PendingSwordStrikes>();
            if (pending != null && ReferenceEquals(command, pending.TouchCommand))
            {
                var delivery = pending.TouchLink;
                pending.TouchCommand = null;
                pending.TouchLink = null;
                if (command.Result == UnitCommand.ResultType.Success)
                    AfterCommand(command as UnitUseAbility, delivery);
                PrunePending();
                return;
            }
            var link = Owner.Get<SwordLinkPart>();
            if (link == null || !ReferenceEquals(command, link.Command)) return;
            var cast = link.Command;
            Owner.Remove<SwordLinkPart>(); // Clear before nested attack events can run.
            if (!link.StrikeFirst && cast.Result == UnitCommand.ResultType.Success && cast.ExecutionProcess != null)
                AfterCommand(cast, link);
        }

        void AfterCommand(UnitUseAbility cast, SwordLinkPart link)
        {
            if (cast == null) { Strike(link); return; } // Native weapon delivery (e.g. a multiclass magus).
            var process = cast.ExecutionProcess;
            if (process == null) return;
            if (process.IsEnded) AfterEffects(process.Context, link);
            else Owner.Ensure<PendingSwordStrikes>().Casts.Add(process.Context, link);
        }

        void AfterEffects(AbilityExecutionContext context, SwordLinkPart link)
        {
            if (context.Ability.Blueprint.StickyTouch != null)
            {
                var touch = Owner.Get<UnitPartTouch>();
                if (touch?.AutoCastCommand != null && !touch.AutoCastCommand.IsFinished &&
                    ReferenceEquals(touch.Ability?.Data.StickyTouch, context.Ability))
                {
                    var pending = Owner.Ensure<PendingSwordStrikes>();
                    pending.TouchCommand = touch.AutoCastCommand;
                    pending.TouchLink = link;
                    return;
                }
            }
            Strike(link);
        }

        void PrunePending()
        {
            var pending = Owner.Get<PendingSwordStrikes>();
            if (pending != null && pending.Casts.Count == 0 && pending.TouchCommand == null) Owner.Remove<PendingSwordStrikes>();
        }

        public void HandleExecutionProcessStart(AbilityExecutionContext context) { }
        public void HandleExecutionProcessEnd(AbilityExecutionContext context)
        {
            if (context.MaybeCaster != Owner) return;
            var pending = Owner.Get<PendingSwordStrikes>();
            if (pending == null || !pending.Casts.TryGetValue(context, out var link)) return;
            pending.Casts.Remove(context);
            AfterEffects(context, link);
            PrunePending();
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
            var link = Owner.Get<SwordLinkPart>();
            if (link?.Command == null || !link.StrikeFirst || link.OpeningAttempted ||
                evt.IsCutscene || evt.IsDuplicateSpellApplied || !ReferenceEquals(evt.Spell, link.Command.Ability) ||
                !ReferenceEquals(evt.SpellTarget, link.Command.Target)) return;
            link.OpeningAttempted = true;
            Strike(link);
        }
        public void OnEventDidTrigger(RuleCastSpell evt) { }

        void Strike(SwordLinkPart link)
        {
            if (Owner.State.IsDead || !Owner.State.CanAct) return;
            var hand = Weapons.StrikeHand(Owner);
            if (!link.Payment.Finish(CanStrike(link.Target, hand), TryPay)) return;
            var attack = new RuleAttackWithWeapon(Owner, link.Target, hand.MaybeWeapon, 0) {
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
        public void HandleUnitLeaveCombat(UnitEntityData unit) { if (unit == Owner) Clear(); }
        protected override void OnTurnOff() { Clear(); }
        void Clear()
        {
            Owner.Remove<SwordLinkPart>();
            Owner.Remove<PendingSwordStrikes>();
        }
    }
}
