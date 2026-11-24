using WotrClassMod.Common;
namespace DreamThief
{
    public static class ReflectionLookups
    {
        static ReflectionLookup F(string t, string f) => new ReflectionLookup("Assembly-CSharp", t, f, LookupKind.Field, LookupMode.Walk);
        const string Fact = "Kingmaker.Blueprints.Facts.BlueprintUnitFact";
        const string Area = "Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect";
        const string Sel = "Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection";
        const string Toggle = "Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility";
        const string Pre = "Kingmaker.Blueprints.Classes.Prerequisites.";
        public static readonly ReflectionLookup[] All = {
            F(Fact,"m_DisplayName"),F(Fact,"m_Description"),F(Fact,"m_DescriptionShort"),F(Fact,"m_Icon"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype","m_ParentClass"),F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass","m_Archetypes"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts","m_Facts"),F("Kingmaker.Blueprints.BlueprintAbilityResource","m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource","m_Icon"),F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources","m_Resource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic","m_RequiredResource"),F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic","m_IsSpendResource"),
            F(Sel,"m_Features"),F(Sel,"m_AllFeatures"),F(Pre+"PrerequisiteFeature","m_Feature"),F(Pre+"PrerequisiteNoFeature","m_Feature"),
            F(Pre+"PrerequisiteNoArchetype","m_Archetype"),F(Pre+"PrerequisiteNoArchetype","m_CharacterClass"),F(Pre+"PrerequisiteClassLevel","m_CharacterClass"),
            F(Area,"m_TargetType"),F("Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff","m_Buff"),
            F("Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect","m_AreaEffect"),F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff","m_Buff"),
            F(Toggle,"m_Buff"),F(Toggle,"m_ActivateWithUnitCommand"),
            new ReflectionLookup("Assembly-CSharp","Kingmaker.UI.MVVM._VM.CharGen.Phases.Class.CharGenClassSelectorItemVM","LevelUpController",LookupKind.Field,LookupMode.Exact)
        };
    }
}
