using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;

namespace Spiritsteel
{
    [HarmonyPatch(typeof(BlueprintAbilityResource), nameof(BlueprintAbilityResource.GetMaxAmount))]
    internal static class RageRounds
    {
        // Substitute the attribute before the engine applies its permanent-stat rules, caps and feat bonuses.
        // The shared barbarian blueprint is also used by other characters in the same party.
        static StatType DailyStat(StatType original, BlueprintAbilityResource resource, UnitDescriptor owner)
        {
            return resource == Blueprints.RageRounds && owner.Progression.IsArchetype(Blueprints.Archetype)
                ? StatType.Wisdom : original;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var amount = typeof(BlueprintAbilityResource).GetField("m_MaxAmount", AccessTools.all).FieldType;
            var stat = amount.GetField("ResourceBonusStat", AccessTools.all);
            var select = AccessTools.Method(typeof(RageRounds), nameof(DailyStat));
            bool found = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!instruction.LoadsField(stat)) continue;
                found = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call, select);
            }
            if (!found) throw new InvalidOperationException("Rage rounds: resource attribute lookup missing.");
        }
    }
}
