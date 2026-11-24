using System;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;

namespace Deathdealer
{
    public sealed class ReactionState : OldStyleUnitPart
    {
        [JsonProperty] public bool Spent;
        [NonSerialized] internal object LastAction;
    }
    public sealed class MovementEpisode : OldStyleUnitPart
    {
        [NonSerialized] internal object Current;
        [NonSerialized] internal bool Moving;
    }
    public sealed class ReactionTraining : UnitFactComponentDelegate, IUnitNewCombatRoundHandler, IUnitCombatHandler
    {
        void Reset(UnitEntityData unit)
        {
            if (unit == Owner && Owner.Get<ReactionState>() is ReactionState s) { s.Spent = false; s.LastAction = null; }
        }
        public void HandleNewCombatRound(UnitEntityData unit) => Reset(unit);
        public void HandleUnitJoinCombat(UnitEntityData unit) => Reset(unit);
        public void HandleUnitLeaveCombat(UnitEntityData unit) => Reset(unit);
    }

    internal enum Response { Attack, Movement, Casting }
    internal static class Reactions
    {
        [ThreadStatic] internal static RuleAttackWithWeapon Strike;
        [ThreadStatic] internal static RuleCombatManeuver Maneuver;
        [ThreadStatic] internal static UnitUseAbility InterruptedCast;
        internal static bool Active => Strike != null || Maneuver != null;
        internal static bool IsWeapon(ItemEntityWeapon weapon)
            => weapon != null && weapon.Blueprint.Category != WeaponCategory.Ray && weapon.Blueprint.Category != WeaponCategory.Touch;
        internal static bool Studied(UnitEntityData owner, UnitEntityData enemy)
            => Blueprints.Studied != null && enemy != null && enemy.Buffs.Enumerable.Any(b=>b.Blueprint==Blueprints.Studied && b.Context?.MaybeCaster==owner);
        static ItemEntityWeapon Weapon(UnitEntityData owner,UnitEntityData enemy,Response kind)
        {
            if (owner == null || !owner.IsInCombat || !owner.CombatState.CanActInCombat || !owner.State.CanAct || owner.State.IsDead || enemy.State.IsDead ||
                !owner.IsEnemy(enemy) || !owner.Buffs.HasFact(Blueprints.Modes[(int)kind]) ||
                !owner.CombatState.CanAttackOfOpportunity || owner.Get<ReactionState>()?.Spent == true) return null;
            var hand = owner.GetThreatHandMelee();
            var range = hand == null ? null : owner.GetThreatRange(hand);
            return range.HasValue && owner.IsAttackOfOpportunityReach(enemy,range.Value) ? hand.MaybeWeapon : null;
        }
        internal static void Respond(UnitEntityData enemy,Response kind,object action)
        {
            if (Active || Blueprints.Archetype == null || enemy == null || !enemy.IsInCombat || !enemy.Buffs.HasFact(Blueprints.Studied)) return;
            var owners = enemy.Buffs.Enumerable.Where(b=>b.Blueprint==Blueprints.Studied)
                .Select(b=>b.Context?.MaybeCaster).Where(u=>u!=null).Distinct().ToArray();
            foreach (var owner in owners)
            {
                var weapon = Weapon(owner,enemy,kind);
                if (weapon == null) continue;
                var state = owner.Ensure<ReactionState>();
                if (action != null && ReferenceEquals(state.LastAction,action)) continue;
                state.Spent = true; state.LastAction = action;
                owner.CombatState.AttackOfOpportunityCount--;
                try
                {
                    if (kind == Response.Movement)
                    {
                        Maneuver = new RuleCombatManeuver(owner,enemy,CombatManeuver.Trip,null);
                        if (Rulebook.Trigger(Maneuver).Success) { enemy.Commands.InterruptMove(); enemy.View?.MovementAgent?.Stop(); }
                    }
                    else
                    {
                        InterruptedCast = kind == Response.Casting ? (UnitUseAbility)action : null;
                        Strike = new RuleAttackWithWeapon(owner,enemy,weapon,0) {
                            IsAttackOfOpportunity = true, SkipAnimation = true, AutoCriticalThreat = kind == Response.Casting
                        };
                        Rulebook.Trigger(Strike);
                        PlaySwing(owner, enemy, weapon);
                    }
                }
                finally { Strike = null; Maneuver = null; InterruptedCast = null; }
            }
        }
        static void PlaySwing(UnitEntityData owner, UnitEntityData target, ItemEntityWeapon weapon)
        {
            // Preserve immediate damage and concentration. This is a view-only handle with no hit callback.
            try
            {
                var manager = owner.View?.AnimationManager;
                var hand = owner.GetThreatHandMelee();
                if (manager == null || hand?.MaybeWeapon != weapon) return;
                var attack = new AttackHandInfo(hand, 0);
                attack.CreateAnimationHandleForAttack(Array.Empty<AttackHandInfo>(), owner.GetSaddledUnit() != null);
                var handle = attack.AnimationHandle;
                if (handle == null) return; // The native factory logs the missing animation.
                handle.AttackTargetDistance = (owner.Position - target.Position).magnitude;
                manager.Execute(handle);
            }
            catch (Exception ex) { WotrClassMod.Common.ModContext.Entry.Logger.Warning("Deathdealer: reaction swing unavailable: " + ex.Message); }
        }

        internal static bool Supersedes(UnitEntityData owner,UnitEntityData enemy,Response kind,object action)
        {
            if (action == null || !Studied(owner,enemy)) return false;
            var s = owner.Get<ReactionState>();
            return ReferenceEquals(s?.LastAction,action) || Weapon(owner,enemy,kind)!=null;
        }
    }

    public sealed class ResearchEffects : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IInitiatorRulebookHandler<RuleAttackRoll>,
        IInitiatorRulebookHandler<RuleCalculateWeaponStats>
    {
        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (Reactions.IsWeapon(evt.Weapon) && Reactions.Studied(Owner,evt.Target))
                evt.AddModifier(Rules.ResearchBonus,Fact,ModifierDescriptor.UntypedStackable);
        }
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (Reactions.IsWeapon(evt.Weapon) && Reactions.Studied(Owner,evt.Target)) evt.AutoCriticalConfirmation = true;
        }
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (Reactions.IsWeapon(evt.Weapon) && evt.AttackWithWeapon != null && Owner.Descriptor.HasFact(Blueprints.Mastery) &&
                Reactions.Studied(Owner,evt.AttackWithWeapon.Target))
                evt.AdditionalCriticalMultiplier.Add(new Modifier(1,Fact,ModifierDescriptor.UntypedStackable));
        }
        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
        public void OnEventDidTrigger(RuleAttackRoll evt) { }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
    }

    [HarmonyPatch(typeof(RuleAttackWithWeaponResolve),nameof(RuleAttackWithWeaponResolve.OnTrigger))]
    static class AfterWeaponAttack
    {
        static void Postfix(RuleAttackWithWeaponResolve __instance)
        {
            if (Blueprints.Archetype == null || Reactions.Active || !Reactions.IsWeapon(__instance.AttackWithWeapon?.Weapon)) return;
            var enemy = __instance.Initiator;
            Reactions.Respond(enemy,Response.Attack,enemy.Commands.Standard);
        }
    }
    [HarmonyPatch(typeof(UnitMoveController),"TickUnit")]
    static class BeforeMovement
    {
        static void Prefix(UnitEntityData unit)
        {
            if (Blueprints.Archetype == null) return;
            var episode = unit.Get<MovementEpisode>();
            if (!unit.IsInCombat || !unit.State.CanMove || unit.View?.MovementAgent?.IsReallyMoving != true ||
                unit.Get<UnitPartForceMove>()?.Chunks.Any()==true)
            {
                if (episode != null) episode.Moving = false;
                return;
            }
            if (Game.Instance.TimeController.DeltaTime <= 0 || !unit.Buffs.HasFact(Blueprints.Studied)) return;
            if (episode == null) episode = unit.Ensure<MovementEpisode>();
            if (!episode.Moving) { episode.Moving = true; episode.Current = new object(); }
            Reactions.Respond(unit,Response.Movement,episode.Current);
        }
    }
    [HarmonyPatch(typeof(UnitMoveController),"OnUnitEndedMoving")]
    static class EndMovement
    {
        static void Postfix(UnitEntityData unit) { var e = unit.Get<MovementEpisode>(); if (e != null) e.Moving = false; }
    }
    [HarmonyPatch(typeof(UnitUseAbility),"OnStart")]
    static class BeforeSpell
    {
        static void Postfix(UnitUseAbility __instance)
        {
            if (__instance.Ability.Blueprint.Type==AbilityType.Spell && __instance.Ability.SourceItem==null)
                Reactions.Respond(__instance.Executor,Response.Casting,__instance);
        }
    }
    // Native concentration is normally queued for the next controller tick. Resolve this reaction's
    // damage once, before even a fast cast can deliver its effect.
    [HarmonyPatch(typeof(UnitConcentrationController),"OnEventAboutToTrigger")]
    static class ConcentrationQueue
    {
        static bool Prefix(RuleDealDamage evt) => !CastDamage.Applies(evt);
    }
    [HarmonyPatch(typeof(RuleDealDamage),nameof(RuleDealDamage.OnTrigger))]
    static class CastDamage
    {
        internal static bool Applies(RuleDealDamage evt) => Reactions.InterruptedCast != null &&
            evt.Target == Reactions.InterruptedCast.Executor && Reactions.Strike != null &&
            evt.Initiator == Reactions.Strike.Initiator && evt.AttackRoll != null && evt.AttackRoll == Reactions.Strike.AttackRoll;
        static void Postfix(RuleDealDamage __instance)
        {
            if (Applies(__instance) && __instance.Result > 0) Reactions.InterruptedCast.MakeConcentrationCheck(__instance);
        }
    }
    [HarmonyPatch(typeof(UnitCombatState),nameof(UnitCombatState.AttackOfOpportunity))]
    static class DuplicateOpportunity
    {
        static bool Prefix(UnitCombatState __instance,UnitEntityData target,bool simulate,bool disengaging,bool tricksterAttack)
        {
            if (simulate || tricksterAttack || Blueprints.Archetype == null) return true;
            if (disengaging) return !Reactions.Supersedes(__instance.Unit,target,Response.Movement,target.Get<MovementEpisode>()?.Current);
            var command = target.Commands.Standard;
            if (command is UnitUseAbility cast && cast.Ability.Blueprint.Type==AbilityType.Spell && cast.Ability.SourceItem==null)
                return !Reactions.Supersedes(__instance.Unit,target,Response.Casting,command);
            if (command is UnitAttack) return !Reactions.Supersedes(__instance.Unit,target,Response.Attack,command);
            return true;
        }
    }
    [HarmonyPatch(typeof(RuleCalculateAttackBonusWithoutTarget),nameof(RuleCalculateAttackBonusWithoutTarget.OnTrigger))]
    static class ReactionAttackStat
    {
        static void Prefix(RuleCalculateAttackBonusWithoutTarget __instance)
        {
            if (Reactions.Strike==null || __instance.WeaponStats?.AttackWithWeapon != Reactions.Strike) return;
            var stats = __instance.Initiator.Stats;
            if (stats.Intelligence.Bonus > stats.GetStat<ModifiableValueAttributeStat>(__instance.AttackBonusStat).Bonus)
                __instance.AttackBonusStat = StatType.Intelligence;
        }
    }
    [HarmonyPatch(typeof(RuleCalculateBaseCMB),nameof(RuleCalculateBaseCMB.OnTrigger))]
    static class ReactionTripStat
    {
        static void Prefix(RuleCalculateBaseCMB __instance)
        {
            if (Reactions.Maneuver == null || __instance.Initiator != Reactions.Maneuver.Initiator ||
                !ReferenceEquals(Rulebook.CurrentContext.EventStack.OfType<RuleCalculateCMB>().LastOrDefault()?.Base,__instance)) return;
            var stats = __instance.Initiator.Stats;
            if (stats.Intelligence.Bonus > stats.GetStat<ModifiableValueAttributeStat>(__instance.ReplaceStrength ?? StatType.Strength).Bonus)
                __instance.ReplaceStrength = StatType.Intelligence;
        }
    }
}
