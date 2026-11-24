using WotrClassMod.Common;
namespace Deathdealer
{
    public static class ReflectionLookups
    {
        static ReflectionLookup F(string t,string f) => new ReflectionLookup("Assembly-CSharp",t,f,LookupKind.Field,LookupMode.Walk);
        public static readonly ReflectionLookup[] All = {
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_DisplayName"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_DescriptionShort"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_Icon"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype","m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass","m_Archetypes"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts","m_Facts"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility","m_Buff"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource","m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource","m_Icon"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount","BaseValue"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources","m_Resource"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility","m_Parent"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic","m_RequiredResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic","m_IsSpendResource"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass","m_CharacterClass"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityVariants","m_Variants")
        };
    }
}
