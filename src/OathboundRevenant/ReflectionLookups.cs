using WotrClassMod.Common;

namespace OathboundRevenant
{
    // 이 모드 코드가 리플렉션(SetField/GetF 등)으로 찾는 게임 멤버의 원장(형식은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴을 더하면 여기에도 더한다: 시험(CommonTests)이 소스를 읽어 표에 없는 리터럴을 실패시키고, 쓰이지 않는 항목도 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField — 기반 형식까지 올라간다. 선언 형식은 시험이 Assembly-CSharp.dll 에서 다시 찾는다(dnlib 2026-11-14).
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            // T1 Archetype.Build (m_ParentClass) · Archetype.Register (m_Archetypes — 등록 예외)
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // T3 Archetype.Build 되읽기: 대체 주문서 없음(주문서 형식 멤버를 직접 부르지 않으려고 필드로 읽음 — wiring: no spellbook dependency).
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ReplaceSpellbook"),
            // T1 UndeadBody.Build — 특성 이름·설명(선언 형식 BlueprintUnitFact, Walk) · AddFacts.m_Facts
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DisplayName"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Description"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            // T3 Hands.Build (ReplaceCasterLevelOfAbility.m_Spell) · Talents.Build (선행·선택 목록) — 선언 형식 dnlib 2026-11-14.
            F("Kingmaker.Designers.Mechanics.Facts.ReplaceCasterLevelOfAbility", "m_Spell"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel", "m_CharacterClass"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature", "m_Feature"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features"),
            // T4 Parts·Strike — 본편 부품 조립(선언 형식 dnlib 2026-11-14).
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact", "m_Fact"),
            F("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact", "m_Fact"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_BaseValueType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Class"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_ExceptClasses"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Progression"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMin"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMax"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_CustomProgression"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig/CustomProgressionItem", "BaseValue"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig/CustomProgressionItem", "ProgressionValue"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Flags"),
            // Icons.Apply · DivineBond(2026-11-15): 본편 원본·우리 블루프린트의 아이콘(선언 BlueprintUnitFact · 자원은 BlueprintAbilityResource)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Icon"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_DisplayName"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Description"),
            F("Kingmaker.UnitLogic.FactLogic.AddAbilityUseTrigger", "m_Ability"),
            // T5 Channel (영역 대상·사용자 속성 CRC·DC·자원 소비).
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround", "m_Radius"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround", "m_TargetType"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround", "m_IncludeDead"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround", "m_Condition"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_CustomProperty"), F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_StepLevel"), // R2: 마지막 숨 = 채널 주사위 × 배수(MultiplyByModifier)
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass", "m_CharacterClass"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_IsSpendResource"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_DisplayName"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_Description"),
            // T6 Auras·Fear·Survival 자원(본편 오라 사슬·하루 자원).
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect", "m_TargetType"),
            F("Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect", "m_AreaEffect"),
            F("Kingmaker.Designers.Mechanics.Facts.AuraFeatureComponent", "m_Buff"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_UseMax"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "BaseValue"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByStat"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource"),
            // T8 DivineBond · Hands(P21).
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureToPet", "m_PetType"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureToPet", "m_Feature"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureIfHasFact", "m_CheckedFact"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureIfHasFact", "m_Feature"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_EnchantName"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Description"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_EnchantmentCost"),
            // 2026-11-16 툴팁 `<null>` 결함: 문구 없음 명시(Tooltips.NoShortDescription — 선언 형식 BlueprintUnitFact, Walk · DivineBond 인챈트 접두/접미).
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Prefix"),
            F("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment", "m_Suffix"),
            F("Kingmaker.UnitLogic.FactLogic.AddBondProperty", "m_Enchant"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_DisplayName"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Description"),
            F("Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact", "m_CheckedFacts"),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
        };
    }
}
