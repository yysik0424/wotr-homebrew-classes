using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Commands;
using WotrClassMod.Common;

namespace Spiritsteel
{
    // The game drops the disengaging flag when it queues a UnitAttackOfOpportunity. Mark that
    // exact command, preserving cancellation, delayed execution and independent attacks on the same target.
    [HarmonyPatch(typeof(UnitCombatState), nameof(UnitCombatState.AttackOfOpportunity))]
    internal static class WeaponFeatMovementCommand
    {
        private static readonly WeakKeyTable<UnitAttackOfOpportunity, object> Movement = new WeakKeyTable<UnitAttackOfOpportunity, object>();
        private static readonly object Marker = new object();

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            int argument = original.GetParameters().Single(p => p.Name == "disengaging" && p.ParameterType == typeof(bool)).Position + 1;
            var constructor = AccessTools.Constructor(typeof(UnitAttackOfOpportunity), new[] { typeof(UnitEntityData), typeof(bool), typeof(bool) });
            var tag = AccessTools.Method(typeof(WeaponFeatMovementCommand), nameof(Tag));
            int matches = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Newobj && Equals(instruction.operand, constructor))
                {
                    matches++;
                    yield return new CodeInstruction(OpCodes.Ldarg, (short)argument);
                    yield return new CodeInstruction(OpCodes.Call, tag);
                }
            }
            if (matches != 1) throw new InvalidOperationException("Spiritsteel: expected one AoO command constructor, found " + matches);
        }

        private static UnitAttackOfOpportunity Tag(UnitAttackOfOpportunity command, bool disengaging)
        {
            if (disengaging) Movement.Add(command, Marker);
            return command;
        }

        internal static bool IsMovement(UnitAttackOfOpportunity command)
        {
            object ignored;
            return Movement.TryGetValue(command, out ignored);
        }
    }

    [HarmonyPatch(typeof(UnitAttackOfOpportunity), "OnAction")]
    internal static class WeaponFeatMovementAction
    {
        [ThreadStatic] internal static UnitAttackOfOpportunity Current;

        private static void Prefix(UnitAttackOfOpportunity __instance, out UnitAttackOfOpportunity __state)
        {
            __state = Current;
            Current = WeaponFeatMovementCommand.IsMovement(__instance) ? __instance : null;
        }

        private static void Finalizer(UnitAttackOfOpportunity __state) { Current = __state; }
    }

    public class WeaponFeatMobility : WeaponEnchantmentLogic, ITargetRulebookHandler<RuleAttackWithWeapon>, ITargetRulebookHandler<RuleCalculateAC>
    {
        private static readonly RulebookEvent.CustomDataKey MovementAttack = new RulebookEvent.CustomDataKey("Spiritsteel.MovementAttack");

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            var command = WeaponFeatMovementAction.Current;
            if (command == null || !evt.IsAttackOfOpportunity || command.Executor != evt.Initiator || command.Target != evt.Target) return;
            evt.SetCustomData(MovementAttack, true);
            WeaponFeatMovementAction.Current = null;
        }

        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            var stack = Rulebook.CurrentContext.EventStack;
            var attack = stack.OfType<RuleAttackWithWeapon>().LastOrDefault();
            bool movement;
            if (attack == null || attack.Initiator != evt.Initiator || attack.Target != evt.Target ||
                attack.AttackRoll == null || attack.AttackRoll != stack.OfType<RuleAttackRoll>().LastOrDefault() ||
                !attack.TryGetCustomData(MovementAttack, out movement) || !movement ||
                WeaponFeats.HasNative(evt.Target, WeaponFeatKind.Mobility)) return;
            evt.AddModifier(4, Fact, ModifierDescriptor.Dodge);
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
    }
}
