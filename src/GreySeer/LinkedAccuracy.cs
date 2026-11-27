using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Utility;
using Newtonsoft.Json;
using TurnBased.Controllers;

namespace GreySeer
{
    // The buff is on the struck enemy. Its native context retains the originating seer on reload.
    public sealed class OpeningWindow : UnitBuffComponentDelegate<OpeningWindow.WindowData>,
        ITurnBasedModeHandler, ITurnBasedUnitTurnEndedHandler, ITurnBasedModeEnabledHandler, IUnitCombatHandler
    {
        public class WindowData { [JsonProperty] public bool NextTurnStarted; }
        internal static TimeSpan RealTimeWindow => new Rounds(1).Seconds;
        public void HandleTurnStarted(UnitEntityData unit)
        {
            if (unit == Buff.Context.MaybeCaster) Data.NextTurnStarted = true;
        }
        public void HandleUnitTurnEnded(UnitEntityData unit)
        {
            if (unit == Buff.Context.MaybeCaster && Data.NextTurnStarted) Buff.Remove();
        }
        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            if (enabled) Buff.MakePermanent();
            else Buff.SetDuration(RealTimeWindow);
        }
        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            if (unit == Owner || unit == Buff.Context.MaybeCaster) Buff.Remove();
        }
        public void UpdateMandatoryTurnBasedModeState() { }
        public void HandleUnitJoinCombat(UnitEntityData unit) { }
        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck roll) { }
    }

    public sealed class LinkedAccuracy : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackRoll>,
        IGlobalRulebookHandler<RuleSavingThrow>, IInitiatorRulebookHandler<RuleDealDamage>, IUnitBuffHandler
    {
        bool OwnSpell(AbilityData spell)
        {
            if (spell == null || spell.Caster != Owner.Descriptor) return false;
            return SpellLink.SlotOfOwnSpell(Owner, spell.StickyTouch ?? spell).HasValue;
        }

        static AbilityData Source(RulebookEvent evt) =>
            evt.Reason?.Ability ?? evt.Reason?.Context?.SourceAbilityContext?.Ability;

        Buff Find(UnitEntityData target, BlueprintBuff opening) =>
            target.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint == opening && b.Context?.MaybeCaster == Owner);

        void Grant(UnitEntityData target, BlueprintBuff opening)
        {
            if (target == null || target.State.IsDead || !Owner.IsEnemy(target)) return;
            Find(target, opening)?.Remove();
            TimeSpan? duration = CombatController.IsInTurnBasedCombat() ? (TimeSpan?)null : OpeningWindow.RealTimeWindow;
            target.Buffs.AddBuff(opening, Owner, duration);
        }

        int Consume(UnitEntityData target, BlueprintBuff opening)
        {
            if (target == null || !Owner.IsEnemy(target)) return 0;
            var mark = Find(target, opening);
            if (mark == null) return 0;
            int bonus = Rules.LinkBonus(Owner.Progression.GetClassLevel(Blueprints.Arcanist));
            mark.Remove();
            return bonus;
        }

        BlueprintBuff AttackOpening(RuleAttackRoll evt)
        {
            if (evt.IsFake || evt.Target == null || !Owner.IsEnemy(evt.Target)) return null;
            var hand = Weapons.StrikeHand(Owner);
            if (hand == null) return null;
            if (evt.Weapon != null && evt.Weapon == hand.MaybeWeapon) return Blueprints.NextBlade;
            return OwnSpell(Source(evt)) ? Blueprints.NextSpell : null;
        }

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            var opening = AttackOpening(evt);
            if (opening == null) return;
            int bonus = Consume(evt.Target, opening);
            if (bonus != 0) evt.AddModifierCopy(new Modifier(bonus, Fact, ModifierDescriptor.UntypedStackable));
        }
        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (evt.IsFake || !evt.IsHit) return;
            var opening = AttackOpening(evt);
            if (opening != null) Grant(evt.Target, opening == Blueprints.NextBlade ? Blueprints.NextSpell : Blueprints.NextBlade);
        }

        // Saving throws are initiated by the defender. Attribute the spell before touching its roll.
        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            if (!OwnSpell(Source(evt)) || Weapons.StrikeHand(Owner) == null) return;
            int bonus = Consume(evt.Initiator, Blueprints.NextSpell);
            if (bonus != 0) evt.AddModifier(-bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
            if (OwnSpell(Source(evt)) && !evt.IsPassed && Weapons.StrikeHand(Owner) != null)
                Grant(evt.Initiator, Blueprints.NextBlade);
        }

        // Automatically hitting spells have no attack roll. Require an actual effect on this enemy.
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (!evt.IsFake && evt.Result > 0 && evt.DamageBundle.Weapon == null && evt.SourceArea == null &&
                OwnSpell(Source(evt)) && Weapons.StrikeHand(Owner) != null)
                Grant(evt.Target, Blueprints.NextBlade);
        }
        public void HandleBuffDidAdded(Buff buff)
        {
            if (buff.Blueprint.Harmful && OwnSpell(buff.Context?.SourceAbilityContext?.Ability) && Weapons.StrikeHand(Owner) != null)
                Grant(buff.Owner, Blueprints.NextBlade);
        }
        public void HandleBuffDidRemoved(Buff buff) { }
    }
}
