using WotrClassMod.Common;
namespace ProphecyBreaker
{
    public static class ReflectionLookups
    {
        static ReflectionLookup F(string type, string field) => new ReflectionLookup("Assembly-CSharp",type,field,LookupKind.Field,LookupMode.Walk);
        public static readonly ReflectionLookup[] All = {
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype","m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype","m_ReplaceSpellbook"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass","m_Archetypes"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook","m_CharacterClass"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook","m_SpellList"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook","m_SpellsPerDay"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook","m_SpellSlots"),
            F("Kingmaker.Designers.Mechanics.Facts.AddSpellbook","m_Spellbook"),
            F("Kingmaker.Designers.Mechanics.Facts.AddSpellbook","m_CasterLevel"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig","m_BaseValueType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig","m_Class"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_DescriptionShort"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_DisplayName"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact","m_Icon"),
        };
    }
}
