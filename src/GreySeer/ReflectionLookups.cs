using WotrClassMod.Common;
namespace GreySeer
{
    public static class ReflectionLookups
    {
        static ReflectionLookup F(string type, string field) { return new ReflectionLookup("Assembly-CSharp", type, field, LookupKind.Field, LookupMode.Walk); }
        const string Fact = "Kingmaker.Blueprints.Facts.BlueprintUnitFact";
        public static readonly ReflectionLookup[] All = {
            F(Fact, "m_DisplayName"), F(Fact, "m_Description"), F(Fact, "m_DescriptionShort"), F(Fact, "m_Icon"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.UnitLogic.Commands.UnitUseAbility", "m_AbilityOriginal", LookupKind.Field, LookupMode.Exact)
        };
    }
}
