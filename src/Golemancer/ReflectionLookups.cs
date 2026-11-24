using WotrClassMod.Common;

namespace Golemancer
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: 시험(CommonTests)이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField(Archetype 의 GetF) — 기반 형식까지 올라간다. 선언 형식은 dnlib 2026-11-09 직독, 시험이 Assembly-CSharp.dll 에서 다시 찾는다.
    //
    // 리플렉션이 아닌 직접 호출 기록(T1 Step 0, 컨트롤러 판정 Q2 — 원장 All 에 넣지 않는다: 이름 리터럴이 없어 CommonTests 의 「원장 이름 → 소스 사용」 검사가 실패하므로.
    // 형식 서명이 바뀌면 컴파일이 실패한다 = 컴파일 잠금). dnlib 2026-11-09, Assembly-CSharp.dll:
    //   LevelUpState..ctor(UnitEntityData unit, LevelUpState.CharBuildMode mode, bool isPregen) — 유일한 생성자, unit null 이면 @0035~0042 throw.
    //     CharBuildMode: LevelUp=0, CharGen=1, Respec=2, Mythic=3, SetName=4.
    //   FeatureSelectionState..ctor(FeatureSelectionState parent, FeatureSource source, IFeatureSelection selection, int index, int selectionLevel) — 유일한 생성자.
    //   FeatureSource = 구조체, 생성자 (BlueprintProgression) / (BlueprintCharacterClass) / (BlueprintRace).
    //   BlueprintFeatureSelection.CanSelect(UnitDescriptor unit, LevelUpState state, FeatureSelectionState selectionState, IFeatureSelectionItem item) — public virtual, 이 형식의 유일한 오버로드.
    //     item null → false @0000~0005 · MeetsPrerequisites @006E · 선택지가 아닌 특성의 랭크 검사 GetRank ≥ Ranks → false @0083~009B · Mode 검사 @009D~00B9.
    //   IFeatureSelectionItem 구현 = BlueprintFeature(자기 자신이 항목) · FeatureUIData.
    //   UnitProgressionData.IsArchetype(BlueprintArchetype) — 인자 null 이면 @0000~0004 조용히 false(게이트는 null 을 넘기지 않는다, CognatogenGate.cs).
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            new ReflectionLookup("mscorlib", "System.Collections.Generic.List`1", "_version", LookupKind.Field, LookupMode.Exact),
            // Checks\TextScenario.cs (S20, 2026-11-17): LocalizedString 의 실제 조회 키(비공개 인스턴스 메서드 — Shared 사슬 끝의 m_Key, dnlib 2026-11-17 @0x0000~0x0046). LoadString 이 이 키로 팩을 조회한다.
            new ReflectionLookup(A, "Kingmaker.Localization.LocalizedString", "GetActualKey", LookupKind.Method, LookupMode.Exact),
            // Checks\Scenarios.cs IdentifyForeignCopy (S11·S16, 2026-11-17): 다른 모드(PrestigePlus)의 후처리가 붙은 BlueprintsCache.Load(BlueprintGuid) 의 Harmony 패치 정보.
            new ReflectionLookup(A, "Kingmaker.Blueprints.JsonSystem.BlueprintsCache", "Load", LookupKind.Method, LookupMode.Exact),
            // Archetype.Build (m_ParentClass) · Archetype.Register (m_Archetypes — 등록 예외, 계획 Global Constraints)
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // OppositionSchools.Build · BombHalf.Build — 우리 특성의 이름·설명·아이콘(선언 형식 BlueprintUnitFact, Walk)
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DisplayName"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Description"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Icon"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features"),
            F("Kingmaker.UnitLogic.FactLogic.AddOppositionSchool", "m_CharacterClass"),
            // BombHalf.Build — IncreaseResourceAmountBySharedValue{m_Resource, Value=ContextValue{CasterCustomProperty, m_CustomProperty}}(정찰 §3 @0018·@002E·@004A)
            F("Kingmaker.UnitLogic.FactLogic.IncreaseResourceAmountBySharedValue", "m_Resource"),
            F("Kingmaker.UnitLogic.Mechanics.ContextValue", "m_CustomProperty"),
            // T3 PetClass.Build — 우리 진행표 m_Classes, 직업 m_Progression(m_Archetypes 는 위 BlueprintCharacterClass 줄과 같다)
            F("Kingmaker.Blueprints.Classes.BlueprintProgression", "m_Classes"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Progression"),
            // T3 GolemUnit.Build — 유닛 m_Faction·m_Brain, Body.m_AdditionalLimbs(m_AddFacts·AddClassLevels.m_CharacterClass 는 공용 CommonLookups 에 있다)
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Faction"),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Brain"),
            F("Kingmaker.Blueprints.BlueprintUnit/UnitBody", "m_AdditionalLimbs"),
            // T3 Taunt.Build — 효과 버프·영역·영역 버프
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DescriptionShort"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Flags"),
            F("Kingmaker.Designers.Mechanics.Facts.AttackBonusAgainstFactOwner", "m_CheckedFact"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect", "m_TargetType"),
            F("Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect", "m_AreaEffect"),
            // T3 GolemMaster.Build — 레벨 속성 ClassLevelGetter.m_Class, AddPet 필드(정찰 §4.1)
            F("Kingmaker.UnitLogic.Mechanics.Properties.ClassLevelGetter", "m_Class"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet", "m_Pet"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet", "m_UseContextValueLevel"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet", "m_LevelContextValue"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet", "m_ForceAutoLevelup"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet", "m_DestroyPetOnDeactivate"),
            // T3 GolemIsolation — 원본 스냅샷(LevelEntry 특성 참조)
            F("Kingmaker.Blueprints.Classes.LevelEntry", "m_Features"),
            // T4 Materials.Build — 슬램 무기(정찰 §20), 수치 특성 ContextRankConfig, 재질 버프 Polymorph, 재질 특성 RemoveFeatureOnApply, 불로 수리, 돌 감속 오라
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_VisualParameters"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_DamageDice"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_OverrideDamageDice"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_BaseValueType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Class"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Progression"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_CustomProgression"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_Prefab"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_MainHand"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_OffHand"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_AdditionalLimbs"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_SecondaryAdditionalLimbs"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_KeepSlots"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_Facts"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_EnterTransition"),
            F("Kingmaker.UnitLogic.Buffs.Polymorph", "m_ExitTransition"),
            F("Kingmaker.Designers.Mechanics.Facts.RemoveFeatureOnApply", "m_Feature"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff", "m_Buff"),
            // T4 AbilityClone.Repoint — 복제본 DC 컴포넌트의 직업(정찰 §6·§20)
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass", "m_CharacterClass"),
             
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_ActivateWithUnitCommand"),
            // T7 Repair.Build — 응급 수리 변형 6(AbilityVariants.m_Variants)과 변형의 부모(BlueprintAbility.m_Parent). dnlib 2026-11-14.
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityVariants", "m_Variants"),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_Parent"),
            // 2026-11-16 추가 슬램 사지 버프(SlamLimbs.Build — AddAdditionalLimb.m_Weapon) · 골렘 폭주 학파 효과(Overdrive.Dice — AdditionalDiceOnAttack.m_DamageEntries 빈 목록). dnlib 2026-11-16.
            F("Kingmaker.UnitLogic.FactLogic.AddAdditionalLimb", "m_Weapon"),
            F("Kingmaker.UnitLogic.Mechanics.Components.AdditionalDiceOnAttack", "m_DamageEntries"),
            // T5 점검 S12 — 추출물 시전 가능 판정(비공개 게터, IL @017E-01E5). 억제 고정물의 SuppressBuffs.m_Buffs·m_ExcludedBuffs 는 FindField(잠금 정규식 밖, T4 D11 선례 — 없으면 이름 붙여 던진다).
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Abilities.AbilityData", "CanBeCastByCaster", LookupKind.Property, LookupMode.Exact),
            // T5 Fix round 2 확정 원장 — 정산한 상한 걷기(BaseDamage.MaximumValue 비공개 설정자 @0000-0007), 훅 등록 관측(RulebookEventBus.Hooks 비공개 정적 → m_DidTriggerHooks). dnlib 2026-11-12.
            new ReflectionLookup(A, "Kingmaker.RuleSystem.Rules.Damage.BaseDamage", "MaximumValue", LookupKind.Property, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.PubSubSystem.RulebookEventBus", "Hooks", LookupKind.StaticField, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.PubSubSystem.RulebookEventHooksManager", "m_DidTriggerHooks", LookupKind.Field, LookupMode.Exact),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
            // Fusion.EnginePartsOf(2026-11-17 결함 수리) — OnPreSave 파트 집합 단언의 원본 목록(GetAll<EntityPart>() 는 UpdateCache @0014~001B 로 null). dnlib 2026-11-17.
            new ReflectionLookup(A, "Kingmaker.EntitySystem.EntityPartsManager", "m_Parts", LookupKind.Field, LookupMode.Exact),
        };
    }
}
