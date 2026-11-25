using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using static WotrClassMod.Common.BlueprintUtil;

namespace BlastMagus
{
    internal static class PoolCompatibility
    {
        // DarkCodex blueprints.txt, LimitlessArcanePool.
        const string LimitlessArcanePool = "80648fab6145492b8978c7d9cf86abc6";

        internal static void AllowUpgrade(BlueprintFeature upgrade, BlueprintFeature native, BlueprintFeature pool)
        {
            var prerequisites = upgrade.ComponentsArray.OfType<PrerequisiteFeature>().Where(p => p.Group == Prerequisite.GroupType.Any).ToArray();
            if (prerequisites.Any(p => p.Feature == pool)) return;
            var original = prerequisites.Single(p => p.Feature == native);
            var alt = new PrerequisiteFeature {name = "$BlastMagusPool$" + upgrade.AssetGuid,
                Group = original.Group, HideInUI = original.HideInUI, CheckInProgression = original.CheckInProgression};
            SetField(alt, "m_Feature", Ref<BlueprintFeatureReference>(pool.AssetGuid.ToString()));
            alt.OwnerBlueprint = upgrade;
            upgrade.ComponentsArray = upgrade.ComponentsArray.Concat(new BlueprintComponent[]{alt}).ToArray();
            pool.IsPrerequisiteFor.Add(Ref<BlueprintFeatureReference>(upgrade.AssetGuid.ToString()));
        }

        // Read the actual native arcana after DarkCodex applies its configured cost rules.
        internal static void SyncAfterBlueprints()
        {
            try { SyncBlastCost(); }
            catch (Exception e) { WotrClassMod.Common.ModContext.LogError("Blast Strike resource compatibility: " + e); }
        }

        internal static void SyncBlastCost()
        {
            if (BlastStrike.Ability == null) return;
            var native = Get<BlueprintAbility>(Guids.Game.ArcaneAccuracyAbility).GetComponent<AbilityResourceLogic>();
            var strike = BlastStrike.Ability.GetComponent<AbilityResourceLogic>();
            var id = BlueprintGuid.Parse(LimitlessArcanePool);
            if (native?.ResourceCostDecreasingFacts?.Any(f => f.Guid == id) != true) return;
            if (strike == null || strike.RequiredResource != native.RequiredResource)
                throw new InvalidOperationException("Blast Strike and Arcane Accuracy must use the same arcane pool");
            if (strike.ResourceCostDecreasingFacts == null) strike.ResourceCostDecreasingFacts = new List<BlueprintUnitFactReference>();
            if (!strike.ResourceCostDecreasingFacts.Any(f => f.Guid == id))
                strike.ResourceCostDecreasingFacts.Add(Ref<BlueprintUnitFactReference>(LimitlessArcanePool));
        }
    }
}
