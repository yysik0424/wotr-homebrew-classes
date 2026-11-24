using WotrClassMod.Common;
namespace SorrowInquisitor
{
    public static class ReflectionLookups
    {
        static ReflectionLookup F(string type, string field) { return new ReflectionLookup("Assembly-CSharp", type, field, LookupKind.Field, LookupMode.Walk); }
        const string Fact = "Kingmaker.Blueprints.Facts.BlueprintUnitFact";
        public static readonly ReflectionLookup[] All = {
            F("Kingmaker.UnitLogic.Buffs.Buff", "m_Context"),
            F("Kingmaker.UnitLogic.Parts.UnitPartGrappleTarget", "m_Buff"),
            F("Kingmaker.UnitLogic.Mechanics.MechanicsContext", "m_Ranks"),
            F("Kingmaker.UnitLogic.Mechanics.MechanicsContext", "m_SharedValues"),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.EntitySystem.EntityFactComponentDelegate`2", "Runtime", LookupKind.Property, LookupMode.Exact),
            F(Fact, "m_DisplayName"), F(Fact, "m_Description"), F(Fact, "m_DescriptionShort"), F(Fact, "m_Icon"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.UnitLogic.Buffs.Buff", "NextTickTime", LookupKind.Property, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.UnitLogic.Buffs.Buff", "TickTime", LookupKind.Property, LookupMode.Exact)
        };
    }
}
