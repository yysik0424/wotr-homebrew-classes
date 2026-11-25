using HarmonyLib;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Newtonsoft.Json;

namespace ShadowSorcerer
{
    // Kept for saves written before the flat daily-slot penalty was retired.
    public class UnitPartShadowSlotMigration : UnitPart
    {
        [JsonProperty] public bool Applied;
    }

    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.SpellSlotCost), MethodType.Getter)]
    static class ShadowSlotCostPatch
    {
        static void Postfix(AbilityData __instance, ref int __result)
        {
            // The native getter inherits the parent's cost for a variant or conversion.
            // The same getter drives both availability and SpendFromSpellbook's debit loop.
            if (__instance.ConvertedFrom != null || __instance.SourceItem != null || __instance.StickyTouch != null ||
                __instance.Blueprint.Type != AbilityType.Spell || __instance.Spellbook?.Blueprint.AssetGuid != SlotsFeature.SorcererBook ||
                __instance.SpellLevel <= 0 || !SlotsFeature.Active(__instance.Caster.Unit)) return;
            try
            {
                ShadowErrors.MaybeFault(ErrorLedger.SiteSpellsPerDay);
                __result = ShadowSlotCost.WithShadow(__result);
            }
            catch (System.Exception error) { ShadowErrors.ReportException(ErrorLedger.SiteSpellsPerDay, error); }
        }
    }
}
