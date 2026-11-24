using WotrClassMod.Common;

namespace Falconer
{
    public static class ReflectionLookups
    {
        static ReflectionLookup F(string type, string field) => new ReflectionLookup("Assembly-CSharp", type, field, LookupKind.Field, LookupMode.Walk);
        public static readonly ReflectionLookup[] All =
        {
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DisplayName"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_IsSpendResource"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Icon"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "BaseValue"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevelStartPlusDivStep"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_ClassDiv"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_ArchetypesDiv"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "StartingLevel"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "StartingIncrease"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "LevelStep"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "PerStepIncrease"),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Faction"),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Brain"),
            F("Kingmaker.UnitLogic.FactLogic.AddMechanicsFeature", "m_Feature"),
        };
    }
}
