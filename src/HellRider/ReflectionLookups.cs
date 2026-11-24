using WotrClassMod.Common;
namespace HellRider
{
    public static class ReflectionLookups
    {
        static ReflectionLookup F(string type, string field) => new ReflectionLookup("Assembly-CSharp",type,field,LookupKind.Field,LookupMode.Walk);
        public static readonly ReflectionLookup[] All = {
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount","BaseValue"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount","IncreasedByStat"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount","ResourceBonusStat"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection","m_AllFeatures"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass","m_Archetypes"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig","m_BaseValueType"),
            F("Kingmaker.Blueprints.BlueprintUnit","m_Brain"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff","m_Buff"),
            F("Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff","m_Buff"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_DescriptionShort"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_DisplayName"),
            F("Kingmaker.Blueprints.BlueprintUnit","m_Faction"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts","m_Facts"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureToPet","m_Feature"),
            F("Kingmaker.UnitLogic.FactLogic.AddMechanicsFeature","m_Feature"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection","m_Features"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_Icon"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic","m_IsSpendResource"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource","m_MaxAmount"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility","m_Parent"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype","m_ParentClass"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet","m_Pet"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureToPet","m_PetType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig","m_Progression"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic","m_RequiredResource"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources","m_Resource"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect","m_SkipTickRoundAfterSpawnRealtime"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect","m_TickRoundAfterSpawn"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityVariants","m_Variants"),
        };
    }
}
