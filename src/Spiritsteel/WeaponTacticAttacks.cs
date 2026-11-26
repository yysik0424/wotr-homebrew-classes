using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using TurnBased.Controllers;
using Kingmaker.Blueprints.Items.Ecnchantments;

namespace Spiritsteel
{
    public sealed class WeaponBlindFight : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleConcealmentCheck>
    {
        public void OnEventAboutToTrigger(RuleConcealmentCheck evt) { }
        public void OnEventDidTrigger(RuleConcealmentCheck evt)
        {
            if (evt.Success || WeaponFeats.HasNative(evt.Initiator, WeaponFeatKind.SightlessStrike)) return;
            var attack = Rulebook.CurrentContext?.LastEvent<RuleAttackWithWeapon>();
            if (attack?.Weapon == Owner && attack.Target == evt.Target && Owner.Blueprint.IsMelee && evt.Roll.RollHistory.Count <= 1)
                evt.Roll.Reroll(Fact, true);
        }
    }

    public sealed class WeaponCloseShot : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleAttackRoll>
    {
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (evt.Weapon == Owner && Owner.Blueprint.IsRanged) evt.DoNotProvokeAttacksOfOpportunity = true;
        }
        public void OnEventDidTrigger(RuleAttackRoll evt) { }
    }

    public sealed class WeaponCriticalDisarm : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            var roll = evt.AttackRoll;
            if (evt.Weapon != Owner || !Owner.Blueprint.IsMelee || roll == null || !roll.IsHit || !roll.IsCriticalConfirmed ||
                roll.FortificationNegatesCriticalHit || roll.ImmuneToCriticalHit || evt.Target.State.IsDead ||
                evt.Initiator.Buffs.HasFact(WeaponTactics.DisarmUsed)) return;
            var body = evt.Target.Body;
            if (!Armed(body.PrimaryHand.MaybeWeapon) && !Armed(body.SecondaryHand.MaybeWeapon)) return;
            WeaponTactics.MarkRound(evt.Initiator, WeaponTactics.DisarmUsed);
            Rulebook.Trigger(new RuleCombatManeuver(evt.Initiator, evt.Target, CombatManeuver.Disarm, null));
        }
        static bool Armed(ItemEntityWeapon weapon) => weapon != null && !weapon.Blueprint.IsNatural && !weapon.Blueprint.IsUnarmed;
    }

    // Keep the extra opportunity on the actual threat hand. Counting it in the general stat
    // would also let other weapons or abilities spend it. Debit only after Owlcat accepts the attack.
    [HarmonyPatch]
    internal static class SpiritVigil
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(UnitCombatState), "CanAttackOfOpportunity");
            yield return AccessTools.Method(typeof(UnitCombatState), "AttackOfOpportunity");
        }
        internal static bool CanSpend(UnitCombatState state)
        {
            if (WeaponTactics.VigilUsed == null) return false;
            var unit = state.Unit;
            var weapon = unit.GetThreatHand()?.Weapon;
            return WeaponFeats.Inscribed(weapon, WeaponFeatKind.Vigil) && weapon.Blueprint.IsMelee &&
                !unit.Buffs.HasFact(WeaponTactics.VigilUsed);
        }
        static int Available(UnitCombatState state) => state.AttackOfOpportunityCount + (CanSpend(state) ? 1 : 0);
        static void Debit(UnitCombatState state, int remaining)
        {
            if (CanSpend(state)) WeaponTactics.MarkRound(state.Unit, WeaponTactics.VigilUsed);
            else state.AttackOfOpportunityCount = remaining;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var get = AccessTools.PropertyGetter(typeof(UnitCombatState), "AttackOfOpportunityCount");
            var set = AccessTools.PropertySetter(typeof(UnitCombatState), "AttackOfOpportunityCount");
            int reads = 0, writes = 0;
            foreach (var ins in instructions)
            {
                if (ins.Calls(get)) { reads++; ins.opcode = OpCodes.Call; ins.operand = AccessTools.Method(typeof(SpiritVigil), nameof(Available)); }
                else if (ins.Calls(set)) { writes++; ins.opcode = OpCodes.Call; ins.operand = AccessTools.Method(typeof(SpiritVigil), nameof(Debit)); }
                yield return ins;
            }
            bool spends = original.Name == "AttackOfOpportunity";
            if (reads != (spends ? 2 : 1) || writes != (spends ? 1 : 0)) throw new InvalidOperationException("Unexpected opportunity budget in " + original);
        }
    }

    [HarmonyPatch(typeof(RuleCheckCastingDefensively), nameof(RuleCheckCastingDefensively.OnTrigger))]
    internal static class SpiritDisrupt
    {
        static readonly Func<UnitEntityData, Kingmaker.Items.Slots.WeaponSlot, bool> Threatens =
            AccessTools.MethodDelegate<Func<UnitEntityData, Kingmaker.Items.Slots.WeaponSlot, bool>>(AccessTools.Method(typeof(UnitHelper), "IsThreatHandMelee"));
        static int Difficulty(int dc, RuleCheckCastingDefensively evt)
        {
            var caster = evt.Initiator;
            if (WeaponTactics.NativeDisrupt == null || caster.Buffs.HasFact(WeaponTactics.NativeDisrupt)) return dc;
            foreach (var enemy in caster.CombatState.EngagedBy)
            {
                if (!enemy.IsEnemy(caster)) continue;
                var body = enemy.Body;
                foreach (var hand in new[] { body.PrimaryHand, body.SecondaryHand })
                {
                    if (WeaponFeats.Inscribed(hand.MaybeWeapon, WeaponFeatKind.Disrupt) && Threatens(enemy, hand) &&
                        enemy.IsReach(caster, hand)) return dc + 4;
                }
            }
            return dc;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var set = AccessTools.PropertySetter(typeof(RuleCheckCastingDefensively), "DC");
            int matches = 0;
            foreach (var ins in instructions)
            {
                if (ins.Calls(set))
                {
                    matches++;
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(ins);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpiritDisrupt), nameof(Difficulty)));
                }
                yield return ins;
            }
            if (matches != 1) throw new InvalidOperationException("Missing defensive casting DC");
        }
    }

    // The attack command uses this radius for a planned hand. Engagement and attacks of
    // opportunity use UnitHelper.IsReach and the weapon's ordinary AttackRange.
    [HarmonyPatch(typeof(UnitAttack), nameof(UnitAttack.GetApproachRadius), new[] { typeof(ItemEntityWeapon), typeof(UnitEntityData), typeof(UnitEntityData) })]
    internal static class SpiritLunge
    {
        static void Postfix(ItemEntityWeapon __0, UnitEntityData __1, ref float __result)
        {
            if (WeaponTactics.LungeStance == null || __0 == null || !__0.Blueprint.IsMelee || !__1.Buffs.HasFact(WeaponTactics.LungeStance) ||
                __1.Buffs.HasFact(WeaponTactics.NativeLunge) || !WeaponTactics.Wields(__0, __1, WeaponFeatKind.Lunge)) return;
            if (CombatController.IsInTurnBasedCombat())
            {
                var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
                if (turn == null || (turn.Rider != __1 && turn.Mount != __1)) return;
            }
            __result += new Feet(5).Meters;
        }
    }
}
