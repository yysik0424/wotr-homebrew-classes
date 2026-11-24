using WotrClassMod.Common;

namespace ChimeraMonk
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: 시험(CommonTests)이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField(Archetype.GetF) — 기반 형식까지 올라간다.
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            // Archetype.Build (m_ParentClass) · Archetype.Register (m_Archetypes — 등록 예외, 계획 Global Constraints)
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // T3 Fab.Feature/Selection — 표시 문구(선언 형식 BlueprintUnitFact, Walk) · 선택 목록(BlueprintFeatureSelection) · 선행 조건 참조(Mutations.Build, Readback).
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DisplayName"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Description"),
            // 2026-11-17 툴팁 `<null>` 수리: Fab.Feature/Selection/Buff/Ability 의 짧은 설명(빈 키, 선언 형식 BlueprintUnitFact)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel", "m_CharacterClass"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature", "m_Feature"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoFeature", "m_Feature"),
            // T4 Mutagen.Build / Readback.MutagenDiff / Fab(Buff·Ability·RankConfig·ApplyBuff·RemoveBuff) — 선언 형식 = Bulwark 원장과 같은 직독값(dnlib 2026-11-14).
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_UseMax"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Min"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "BaseValue"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByStat"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "ResourceBonusStat"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Flags"),
            // Icons.Apply(2026-11-15): 본편 원본·우리 블루프린트의 아이콘(선언 BlueprintUnitFact · 자원은 BlueprintAbilityResource)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Icon"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_DisplayName"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Description"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_DisplayName"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_Description"),
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
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_IsSpendResource"),
            // T5 NaturalWeapons.Build — 추가 사지 무기 참조(dnlib 2026-11-14: AddAdditionalLimb.m_Weapon BlueprintItemWeaponReference).
            F("Kingmaker.UnitLogic.FactLogic.AddAdditionalLimb", "m_Weapon"),
            // 점검 S9 tail-limb-exactly-1 · retired-extra-tail-inert(2026-11-17): 엔진이 종류별 상한 때문에 밀어 둔 추가 사지(dnlib 2026-11-17: UnitBody.m_UnusedAdditionalLimbs Dictionary<int, LimbRecord>, 비공개).
            F("Kingmaker.Items.UnitBody", "m_UnusedAdditionalLimbs"),
            // T6 BodyMutations.Build / Readback.BodyDiff(dnlib 2026-11-14 직독 — 비공개 필드).
            F("Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects", "m_CheckedBuffList"),
            F("Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects", "m_ExtraEffectBuff"),
            F("Kingmaker.UnitLogic.FactLogic.AddMechanicsFeature", "m_Feature"),
            F("Kingmaker.UnitLogic.FactLogic.AreaEffectImmunity", "m_CasterType"),
            F("Kingmaker.UnitLogic.FactLogic.AreaEffectImmunity", "m_SpecificAreaEffects"),
            F("Kingmaker.UnitLogic.FactLogic.AreaEffectImmunity", "m_AreaEffects"),
            // T7 Breath / Venom / Aspects / Readback(dnlib 2026-11-14 직독 — 비공개 필드).
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_Icon"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Projectiles"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Length"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass", "m_CharacterClass"),
            F("Kingmaker.UnitLogic.FactLogic.IncreaseResourceAmount", "m_Resource"),
            F("Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasFacts", "m_Facts"),
            F("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact", "m_Fact"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow", "m_ConditionalDCIncrease"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel", "m_Class"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel", "m_AdditionalClasses"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel", "m_Feature"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
        };
    }
}
