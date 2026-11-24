using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace Spiritsteel
{
    internal static class SpiritTeamwork
    {
        private static readonly RulebookEvent.CustomDataKey OutflankKey = new RulebookEvent.CustomDataKey("Spiritsteel.Outflank");

        private static bool Active(UnitEntityData owner)
        {
            return owner != null && Blueprints.Tactics != null && Blueprints.Spirit != null &&
                owner.Descriptor.HasFact(Blueprints.Tactics) && owner.Buffs.GetBuff(Blueprints.Spirit) != null &&
                BoundWeapons.IsEquipped(owner);
        }

        private static bool SharesTarget(UnitEntityData owner, UnitEntityData target)
        {
            return target != null && !target.State.Features.CannotBeFlanked && owner.IsEnemy(target) && Active(owner);
        }

        private static bool IsBoundMelee(UnitEntityData owner, ItemEntityWeapon weapon)
        {
            return weapon != null && weapon.Blueprint.IsMelee && BoundWeapons.IsBound(owner, weapon);
        }

        internal static bool Outflank(bool ordinaryFlank, RuleCalculateAttackBonus attack)
        {
            if (ordinaryFlank) return true;
            if (!IsBoundMelee(attack.Initiator, attack.Weapon) || !SharesTarget(attack.Initiator, attack.Target)) return false;
            // The ordinary Outflank component supplies the feat's +2. The core calculation supplies
            // its ordinary +2 exactly once even if another feature also marks this roll flanked.
            attack.SetCustomData(OutflankKey, true);
            return true;
        }

        internal static bool BaseFlank(RuleCalculateAttackBonus attack)
        {
            bool marked;
            return attack.TargetIsFlanked || (attack.TryGetCustomData(OutflankKey, out marked) && marked);
        }

        internal static bool Precise(bool ordinaryFlank, RulePrepareDamage attack)
        {
            return ordinaryFlank || (IsBoundMelee(attack.Initiator, attack.DamageBundle.Weapon) &&
                SharesTarget(attack.Initiator, attack.Target));
        }

        internal static bool Tandem(bool ordinaryFlank, RuleCombatManeuver maneuver)
        {
            if (ordinaryFlank) return true;
            var owner = maneuver.Initiator;
            var target = maneuver.Target;
            if (!SharesTarget(owner, target)) return false;
            return Threatens(owner, target, owner.Body.PrimaryHand) || Threatens(owner, target, owner.Body.SecondaryHand);
        }

        private static bool Threatens(UnitEntityData owner, UnitEntityData target, WeaponSlot hand)
        {
            return hand.HasWeapon && IsBoundMelee(owner, hand.Weapon) &&
                (!hand.Weapon.Blueprint.IsUnarmed || owner.State.Features.ImprovedUnarmedStrike) && owner.IsReach(target, hand);
        }

        // Nearby-teamwork loops enumerate the caster as well. Accept that single entry as the
        // spirit only for the owner, preserving each original radius, ally check, ceiling and early exit.
        internal static bool AdjacentAlly(UnitEntityData candidate, UnitDescriptor owner)
        {
            return candidate != owner || (owner != null && Active(owner.Unit));
        }

        internal static void ShieldedConcentration(RuleCalculateAbilityParams evt, int realAllies, UnitEntityData owner)
        {
            int bonus = evt.Initiator == owner && Active(owner) ? Math.Max(4, realAllies) : realAllies;
            evt.AddBonusConcentration(bonus);
        }

        private static MethodInfo Helper(string name) { return AccessTools.Method(typeof(SpiritTeamwork), name); }

        // Three concrete native gates consult IsFlanked. Preserve the native answer and pass the
        // result plus the specific rule to the appropriate, per-feat extension.
        internal static IEnumerable<CodeInstruction> FlankGate(IEnumerable<CodeInstruction> input, string helper)
        {
            var code = input.ToList();
            var getter = AccessTools.PropertyGetter(typeof(UnitCombatState), nameof(UnitCombatState.IsFlanked));
            var gate = code.Single(i => i.Calls(getter));
            int pos = code.IndexOf(gate) + 1;
            code.Insert(pos, new CodeInstruction(OpCodes.Ldarg_1));
            code.Insert(pos + 1, new CodeInstruction(OpCodes.Call, Helper(helper)));
            return code;
        }

        internal static IEnumerable<CodeInstruction> BaseGate(IEnumerable<CodeInstruction> input)
        {
            var code = input.ToList();
            var gate = code.Single(i => i.Calls(AccessTools.PropertyGetter(typeof(RuleCalculateAttackBonus), nameof(RuleCalculateAttackBonus.TargetIsFlanked))));
            gate.opcode = OpCodes.Call;
            gate.operand = Helper(nameof(BaseFlank));
            return code;
        }

        internal static IEnumerable<CodeInstruction> NeighbourGate(IEnumerable<CodeInstruction> input, bool validateCaster = false)
        {
            var code = input.ToList();
            var inequality = AccessTools.Method(typeof(UnitEntityData), "op_Inequality", new[] { typeof(UnitEntityData), typeof(UnitDescriptor) });
            var checks = code.Where(i => i.Calls(inequality)).ToArray();
            if (checks.Length != (validateCaster ? 2 : 1))
                throw new InvalidOperationException("Spiritsteel: changed ally checks in native teamwork feat");
            // Allied Spellcaster's first comparison rejects a spell from a different caster.
            var neighbour = checks[checks.Length - 1];
            neighbour.opcode = OpCodes.Call;
            neighbour.operand = Helper(nameof(AdjacentAlly));
            return code;
        }

        internal static IEnumerable<CodeInstruction> ConcentrationGate(IEnumerable<CodeInstruction> input)
        {
            var code = input.ToList();
            var end = code.Single(i => i.Calls(AccessTools.Method(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.AddBonusConcentration))));
            var owner = code.Select(i => i.operand as MethodInfo).First(m => m?.Name == "get_Owner" && m.ReturnType == typeof(UnitEntityData));
            int pos = code.IndexOf(end);
            code.Insert(pos, new CodeInstruction(OpCodes.Ldarg_0));
            code.Insert(pos + 1, new CodeInstruction(OpCodes.Call, owner));
            end.opcode = OpCodes.Call;
            end.operand = Helper(nameof(ShieldedConcentration));
            return code;
        }
    }

    [HarmonyPatch(typeof(OutflankAttackBonus), nameof(OutflankAttackBonus.OnEventAboutToTrigger))]
    internal static class SpiritOutflank
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.FlankGate(instructions, nameof(SpiritTeamwork.Outflank)); }
    }

    [HarmonyPatch(typeof(RuleCalculateAttackBonus), nameof(RuleCalculateAttackBonus.OnTrigger))]
    internal static class SpiritOutflankBase
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.BaseGate(instructions); }
    }

    [HarmonyPatch(typeof(PreciseStrike), nameof(PreciseStrike.OnEventAboutToTrigger))]
    internal static class SpiritPreciseStrike
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.FlankGate(instructions, nameof(SpiritTeamwork.Precise)); }
    }

    [HarmonyPatch(typeof(ModifyD20), "CheckTandemTrip")]
    internal static class SpiritTandemTrip
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.FlankGate(instructions, nameof(SpiritTeamwork.Tandem)); }
    }

    [HarmonyPatch(typeof(ShakeItOff), nameof(ShakeItOff.OnEventAboutToTrigger))]
    internal static class SpiritShakeItOff
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.NeighbourGate(instructions); }
    }

    [HarmonyPatch(typeof(BackToBack), nameof(BackToBack.OnEventAboutToTrigger))]
    internal static class SpiritBackToBack
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.NeighbourGate(instructions); }
    }

    [HarmonyPatch(typeof(CoordinatedDefense), nameof(CoordinatedDefense.OnEventAboutToTrigger))]
    internal static class SpiritCoordinatedDefense
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.NeighbourGate(instructions); }
    }

    [HarmonyPatch(typeof(CoordinatedManeuvers), nameof(CoordinatedManeuvers.OnEventAboutToTrigger))]
    internal static class SpiritCoordinatedManeuvers
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.NeighbourGate(instructions); }
    }

    [HarmonyPatch(typeof(AlliedSpellcaster), nameof(AlliedSpellcaster.OnEventAboutToTrigger), new[] { typeof(RuleCalculateAbilityParams) })]
    internal static class SpiritAlliedConcentration
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.NeighbourGate(instructions, true); }
    }

    [HarmonyPatch(typeof(AlliedSpellcaster), nameof(AlliedSpellcaster.OnEventAboutToTrigger), new[] { typeof(RuleSpellResistanceCheck) })]
    internal static class SpiritAlliedPenetration
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.NeighbourGate(instructions, true); }
    }

    [HarmonyPatch(typeof(ShieldedCaster), nameof(ShieldedCaster.OnEventAboutToTrigger))]
    internal static class SpiritShieldedCaster
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return SpiritTeamwork.ConcentrationGate(instructions); }
    }
}
