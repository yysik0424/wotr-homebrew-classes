using WotrClassMod.Common;
namespace Spiritsteel
{
    public static class ReflectionLookups
    {
        static ReflectionLookup Field(string type, string name)
        {
            return new ReflectionLookup("Assembly-CSharp", type, name, LookupKind.Field, LookupMode.Walk);
        }
        public static readonly ReflectionLookup[] All = {
            Field("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DisplayName"),
            Field("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Description"),
            Field("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            Field("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            Field("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            Field("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            Field("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            Field("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features"),
            Field("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures"),
            Field("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            Field("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel", "m_CharacterClass"),
            Field("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_EnchantName"),
            Field("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Description"),
            Field("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Prefix"),
            Field("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Suffix"),
            Field("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_Enchantments"),
            Field("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_SecondWeapon"),
            Field("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_VisualParameters"),
            Field("Kingmaker.Blueprints.Items.Shields.BlueprintItemShield", "m_WeaponComponent"),
            Field("Kingmaker.Blueprints.Items.Shields.BlueprintItemShield", "m_VisualParameters"),
            Field("Kingmaker.Blueprints.Items.BlueprintItem", "m_DisplayNameText"),
            Field("Kingmaker.Blueprints.Items.BlueprintItem", "m_DescriptionText"),
            Field("Kingmaker.Blueprints.Items.BlueprintItem", "m_Cost"),
            Field("Kingmaker.Blueprints.Items.BlueprintItem", "m_IsNotable"),
            Field("Kingmaker.RuleSystem.Rules.RuleCalculateWeaponStats", "m_SizeShift")
        };
    }
}
