using WotrClassMod.Common;

namespace BulwarkMage
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: 시험(CommonTests)이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField(Archetype·RuntimeReadback 의 GetF) — 기반 형식까지 올라간다. 선언 형식은 dnlib 2026-11-14 직독, 시험이 Assembly-CSharp.dll 에서 다시 찾는다.
    // 되읽기 행(ForeignTolerance.Rows)의 필드 이름은 리터럴이 아니라 행 데이터다 → 이 원장 대신 시험 `jbp: readback rows resolve` 가 jbp 형식 + dnlib 로 잠근다.
    // 리플렉션이 아닌 직접 호출(컴파일 잠금): RuleCalculateArcaneSpellFailureChance..ctor(UnitEntityData) · .Result/.IgnoreArmor/.IgnoreShield/.Armor/.Shield(dnlib 2026-11-14),
    //   UnitProficiency.Contains(ArmorProficiencyGroup), Spellbook.CalcSlotsLimit(int, SpellSlotType)/GetSpellsPerDay(int)/OppositionSchools/SpecialLists.
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            // Archetype.Build (m_ParentClass) · Archetype.Register (m_Archetypes — 등록 예외, 계획 Global Constraints)
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // Proficiency.Build — 우리 특성의 이름·설명(선언 형식 BlueprintUnitFact, Walk)
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DisplayName"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Description"),
            // 2026-11-17 툴팁 `<null>` 수리: 우리 특성·선택·버프·토글·능력의 짧은 설명(빈 키) · 타워실드 복제의 짧은 설명 복사(선언 형식 BlueprintUnitFact, Walk)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            // RuntimeReadback 판독기 — LevelEntry 의 특성 참조(참조를 풀지 않고 GUID 만 읽는다)
            F("Kingmaker.Blueprints.Classes.LevelEntry", "m_Features"),
            // T3 TowerShield 복제(설계 §4, 정찰 §3 M3): 표시 문구·아이콘·버프 표지 복사 + 참조 4곳(m_NewFact · m_Feature · m_CheckedFacts · m_Buff)을 복제본으로. 선언 형식 dnlib 2026-11-14.
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_DisplayName"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Flags"),
            F("Kingmaker.Designers.Mechanics.Buffs.HasShieldFeatureUnlock", "m_NewFact"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Feature"),
            F("Kingmaker.Designers.Mechanics.Facts.FactsChangeTrigger", "m_CheckedFacts"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff", "m_Buff"),
            // T5 Rebuke.Build / Describe(만들기·되읽기 — 선언 형식 dnlib 2026-11-14 직독, Step 0 members.txt).
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_UseMax"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Min"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "BaseValue"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevel"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevelStartPlusDivStep"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByStat"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "ResourceBonusStat"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextSpendResource", "m_Resource"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_ActivateWithUnitCommand"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.Designers.Mechanics.Buffs.HasShieldFeatureUnlock", "FilterByShieldProficiencyGroup"),
            F("Kingmaker.Designers.Mechanics.Buffs.HasShieldFeatureUnlock", "m_ShiledProficiencyGroupEntries"),
            F("Kingmaker.Designers.Mechanics.Buffs.HasShieldFeatureUnlock", "FilterByBlueprintShieldTypes"),
            F("Kingmaker.Designers.Mechanics.Buffs.HasShieldFeatureUnlock", "m_DisableWhenHasArmor"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCastSpell", "m_Spell"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_BaseValueType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Class"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_ExceptClasses"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Progression"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_CustomProgression"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMin"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMax"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig/CustomProgressionItem", "BaseValue"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig/CustomProgressionItem", "ProgressionValue"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass", "m_CharacterClass"),
            // T6 BondedShield.Build / VerifyNow(선언 형식 dnlib 2026-11-14 직독: BlueprintAbility 의 m_AutoUseIsForbidden·m_IsFullRoundAction, BlueprintUnitFact 의 이름·설명·아이콘, AbilityResourceLogic·BuffEnchantArmor 비공개 필드).
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_DisplayName"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_Description"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_Icon"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_AutoUseIsForbidden"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_IsFullRoundAction"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_IsSpendResource"),
             
            //   A2b 우리 강화 마법부여 = 본편 ArmorEnhancementBonus{n} 필드 복사(BlueprintItemEnchantment 선언, BlueprintArmorEnchantment 에서 Walk). BuffEnchantArmor 필드 3개는 쓰지 않게 되어 뺐다.
            F("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact", "m_Fact"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_EnchantName"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Description"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Prefix"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Suffix"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_EnchantmentCost"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_IdentifyDC"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_AllowNonContextActions"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_HiddenInUI"),
        };
    }
}
