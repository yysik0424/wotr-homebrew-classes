using WotrClassMod.Common;
namespace BloodBerserker
{
    public static class ReflectionLookups
    {
        static ReflectionLookup F(string type, string field) { return new ReflectionLookup("Assembly-CSharp", type, field, LookupKind.Field, LookupMode.Walk); }
        const string Fact = "Kingmaker.Blueprints.Facts.BlueprintUnitFact";
        public static readonly ReflectionLookup[] All = {
            F("Kingmaker.Blueprints.BlueprintProjectile", "m_Trajectory"),
            F(Fact, "m_DisplayName"), F(Fact, "m_Description"), F(Fact, "m_DescriptionShort"), F(Fact, "m_Icon"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Projectiles"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Weapon"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_BaseValueType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Class"),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.Controllers.AbilityExecutionProcess", "DoApplyEffect", LookupKind.Method, LookupMode.Exact)
        };
    }
}
