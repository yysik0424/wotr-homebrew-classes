using WotrClassMod.Common;

namespace PhantomBlade
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: 시험이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // 전부 Walk: Archetype/StandBlueprints 의 SetField·GetF 는 BlueprintUtil.FindField 로 기반 형식까지 올라간다. 선언 형식은 dnlib 확인 2026-11-07.
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            // Archetype
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // StandBlueprints.Names (BlueprintUnitFact 의 private 필드; Feature·Buff·ActivatableAbility·Ability 에 부른다)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DisplayName"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            // 토글·능력·액션
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_IsSpendResource"),
            // 주먹 무기(m_VisualParameters 는 BlueprintItemEquipmentHand, m_DisplayNameText/m_DescriptionText 는 BlueprintItem 에 선언)
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_Type"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_Size"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_OverrideDamageDice"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_DamageDice"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_OverrideDamageType"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_DamageType"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_DisplayNameText"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_DescriptionText"),
            // 2026-11-17 툴팁 `<null>`·예외 수리(MakeFist): 나머지 BlueprintItem 문구 칸 3개 = 빈 키(BlueprintItem 에 선언, dnlib 2026-11-17)
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_FlavorText"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_NonIdentifiedNameText"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_NonIdentifiedDescriptionText"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_VisualParameters"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_Enchantments"),
            // 속성
            F("Kingmaker.UnitLogic.Mechanics.Properties.ClassLevelGetter", "m_Class"),
            F("Kingmaker.UnitLogic.Mechanics.Properties.ClassLevelGetter", "m_Archetype"),
            F("Kingmaker.UnitLogic.Mechanics.Properties.CustomPropertyGetter", "m_Property"),
            F("Kingmaker.UnitLogic.Mechanics.Properties.PropertySettings", "m_LimitType"),
            F("Kingmaker.UnitLogic.Mechanics.Properties.PropertySettings", "m_Min"),
            // 자원(Amount 는 비공개 중첩 구조체)
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Icon"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "BaseValue"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevel"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByStat"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_Class"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_Archetypes"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_ClassDiv"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_ArchetypesDiv"),
            // 특성 컴포넌트
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource"),
            F("Kingmaker.UnitLogic.FactLogic.IncreaseResourceAmountBySharedValue", "m_Resource"),
            F("Kingmaker.UnitLogic.Mechanics.ContextValue", "m_CustomProperty"),
            // 러시 트리거 조건(StandBlueprints.BuildRushTriggers)
            F("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact", "m_Fact"),
            F("Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasFacts", "m_Facts"),
            // 귀신참(StandBlueprints.BuildGhostSlash, Task 6c). 이름·선언 형식은 dnlib 확인 2026-11-09(AbilityDeliverProjectile·ContextRankConfig 필드 목록).
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Projectiles"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Weapon"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_BaseValueType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_CustomProperty"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Progression"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMin"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMax"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_ItemsTypes"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_ArmorTypes"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_FeatureList"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_CustomProgression"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_AdditionalArchetypes"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Class"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_CustomPropertyList"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage", "m_Type"),
             
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Faction"),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Brain"),
            // 결함 2026-11-14(파티 인벤토리 누수): 령 복제의 시작 인벤토리를 빈 새 배열로(SpiritBlueprints.BuildClone).
            F("Kingmaker.Blueprints.BlueprintUnit", "m_StartingInventory"),
            F("Kingmaker.UnitLogic.FactLogic.AddMechanicsFeature", "m_Feature"),
            // 령 반투명 FX 버프(2026-11-14): UI 숨김 깃발(BlueprintBuff 의 private Flags m_Flags, dnlib 2026-11-14 형식 덤프 — 공개 설정자 없음, IsHiddenInUI 는 get 만).
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Flags"),
            // 점검 S12 translucent-fx: 버프가 띄운 FX 손잡이(Buff.TrySpawnParticleEffect IL 0113 stfld). GetField 직접 호출 = Exact.
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Buffs.Buff", "m_ParticleEffect", LookupKind.Field, LookupMode.Exact),
             
            // 점검 S12-spirit-visual no-save-footprint 도 같은 이름으로 엔진 판정을 직접 부른다(이 한 줄이 두 곳의 원장).
            new ReflectionLookup(A, "Kingmaker.EntitySystem.Persistence.JsonUtility.SceneEntitiesStateConverter", "ShouldSerialize", LookupKind.Method, LookupMode.Exact),
             
            // OnEnable IL 0000 에서 더하고 OnDisable IL 0000 에서 뺀다(정찰 §2(c), dnlib 2026-11-09) → 령 에이전트가 없고 대조군(주인) 에이전트는 있어야 한다. GetField 직접 호출 = Exact.
            new ReflectionLookup(A, "Kingmaker.View.UnitMovementAgentBase", "AllAgents", LookupKind.StaticField, LookupMode.Exact),
             
            new ReflectionLookup(A, "Kingmaker.View.UnitMovementAgentBase", "m_Speed", LookupKind.Field, LookupMode.Exact),
            // 유령 디졸브(같은 날 판정 1, SpiritGhostLook.Attach): private 필드(공개 게터 get_DissolveController 는 internal). GetField 직접 호출 = Exact.
            new ReflectionLookup(A, "Kingmaker.Visual.MaterialEffects.StandardMaterialController", "m_DissolveController", LookupKind.Field, LookupMode.Exact),
            // 점검 S12 ghost-dissolve: 컨트롤러가 다루는 재질 목록(internal 속성 Materials). GetProperty 직접 호출 = Exact.
            new ReflectionLookup(A, "Kingmaker.Visual.MaterialEffects.StandardMaterialController", "Materials", LookupKind.Property, LookupMode.Exact),
            // 2026-11-17 K11 하체 흐림 불일치 진단(SpiritGhostLook.MismatchDiagnosis): 컨트롤러 렌더러 목록·재캐시 깃발(private, dnlib 2026-11-17 PrepareForAnimation IL_0014·001B). GetField 직접 호출 = Exact.
            new ReflectionLookup(A, "Kingmaker.Visual.MaterialEffects.StandardMaterialController", "m_Renderers", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.Visual.MaterialEffects.StandardMaterialController", "m_RenderersUpdated", LookupKind.Field, LookupMode.Exact),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
        };
    }
}
