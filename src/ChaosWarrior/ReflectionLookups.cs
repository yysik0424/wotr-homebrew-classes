using WotrClassMod.Common;

namespace ChaosWarrior
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 소스에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더한다: CommonTests 가 표에 없는 리터럴·쓰이지 않는 항목을 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField — 기반 형식까지 올라간다. 선언 형식은 시험이 Assembly-CSharp.dll 에서 다시 찾는다.
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            // Archetype.Build (m_ParentClass) · Archetype.Register (m_Archetypes — 등록 예외)
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // BuildKit: 표시 문구(선언 BlueprintUnitFact) · 버프 표지 · ContextRankConfig 필드 · CustomProgressionItem
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DisplayName"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Description"),
            // 2026-11-17 툴팁 `<null>` 수리: BuildKit.Names 의 짧은 설명(빈 키)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Flags"),
            // Icons.Apply(2026-11-15): 본편 원본·우리 블루프린트의 아이콘(선언 BlueprintUnitFact · 자원은 BlueprintAbilityResource)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Icon"),
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
            // Rage.Build: 자원 Amount · 활성 능력 · 자원 논리 · 특성 부품
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_UseMax"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Min"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevel"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevelStartPlusDivStep"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "StartingLevel"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "StartingIncrease"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "LevelStep"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "PerStepIncrease"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "MinClassLevelIncrease"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_ClassDiv"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByStat"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "ResourceBonusStat"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_ActivateWithUnitCommand"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic", "m_FreeBlueprint"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeaturesFromList", "m_Features"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeaturesFromList", "m_RestrictIfNot"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource"),
            // Mutations.StoredFacts(ChildFacts·IsStoredBy 가 쓴다 — 개편 O3 에서 ChildrenOf 대체): 부모 버프의 저장 사실(비공개, 공개 열거 API 없음 — T5 Step 0)
            F("Kingmaker.UnitLogic.Buffs.Buff", "m_StoredFacts"),
            // T6 Mutations.BuildEffects: 추가 팔 무기 · 오라 참조 · 오라 버프(m_Buff 이름은 위 활성 능력과 같은 항목) · 크기 변경 종류·피해 종류(m_Type 이름은 위 ContextRankConfig 와 같은 항목)
            F("Kingmaker.UnitLogic.FactLogic.AddAdditionalLimb", "m_Weapon"),
            F("Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect", "m_AreaEffect"),
            // 개편 O4 NaturalWeapons: 1d4 원본 복제본의 주사위 재정의(쓰기) · 복제본 표시 문구 칸의 null 검사(읽기 — 선언 BlueprintItem)
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_OverrideDamageDice"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_DamageDice"),
            F("Kingmaker.Blueprints.Items.BlueprintItem", "m_DisplayNameText"),
            F("Kingmaker.Blueprints.Items.BlueprintItem", "m_DescriptionText"),
            F("Kingmaker.Blueprints.Items.BlueprintItem", "m_FlavorText"),
            F("Kingmaker.Blueprints.Items.BlueprintItem", "m_NonIdentifiedNameText"),
            F("Kingmaker.Blueprints.Items.BlueprintItem", "m_NonIdentifiedDescriptionText"),
            // 개편 O5 NaturalWeapons.LimbCapNotes: 엔진의 추가 사지 범주 상한 표(읽기 — 문서 전사와의 차이를 정보 줄로)
            F("Kingmaker.Dungeon.Blueprints.BlueprintPersonageLimits", "m_DopLimbMaxCount"),
            F("Kingmaker.Dungeon.Blueprints.BlueprintPersonageLimits/WeaponSlotCount", "Slot"),
            F("Kingmaker.Dungeon.Blueprints.BlueprintPersonageLimits/WeaponSlotCount", "Count"),
            // 독립 검토 R2-F1 GameMutationWorld.HumanLimbs: 사람 쪽 사지 목록(비공개 — 공개 AdditionalLimbs 는 변신 중 변신 쪽 목록을 준다)
            F("Kingmaker.Items.UnitBody", "m_AdditionalLimbs"),
            // 독립 검토 R2-F3 NaturalWeapons.RuntimeCacheKeys: 복제 되읽기에서 건너뛰는 엔진 런타임 캐시 칸(존재 확인만 — 값은 읽지 않는다)
            F("Kingmaker.Blueprints.Items.BlueprintItem", "m_CachedEnchantments"),
            F("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipment", "m_CachedUseWhileCanComponents"),
            F("Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters", "m_CachedBeltModel"),
            F("Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters", "m_CachedSheathModel"),
            F("Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters", "m_CachedEquipLinksUpToDate"),
            F("Kingmaker.Localization.LocalizedString", "m_ShouldProcess"),
            // 개편 O8 Quicken.Build · VerifyNow: 신속 시전 권리 버프의 AutoMetamagic 비공개 칸(허용 종류 = 비공개 중첩 열거 · 주문서 참조 · 포함/제외 직업 — 쓰기·되읽기)
            F("Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", "m_AllowedAbilities"),
            F("Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", "m_Spellbook"),
            F("Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", "m_IncludeClasses"),
            F("Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", "m_ExcludeClasses"),
            new ReflectionLookup(A, "Kingmaker.ElementsSystem.Condition", "LastResult", LookupKind.Property, LookupMode.Exact),
            // 독립 검토 R3-F2 CloneRuntimeCaches: 참조 래퍼의 블루프린트 캐시 · 자원 링크의 적재 손잡이(자동 속성 — 뒷받침 칸 이름은 속성에서 만든다, 존재 확인만)
            new ReflectionLookup(A, "Kingmaker.Blueprints.BlueprintReferenceBase", "Cached", LookupKind.Property, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.ResourceLinks.WeakResourceLink`1", "m_Handle", LookupKind.Property, LookupMode.Exact),
            // 독립 검토 R3-F1 점검 Scenarios.DispelAll: 해제 주문 행동의 주문 레벨 상한(비공개 ContextValue — null 이면 TryDispelBuff @000D 에서 NRE, 0 단순 값 = 제한 없음)
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic", "m_MaxSpellLevel"),
        };
    }
}
