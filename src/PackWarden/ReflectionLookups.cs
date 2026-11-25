namespace PackWarden
{
    // 모드가 리플렉션(GetField/GetMethod/GetProperty/Set/GetF/SetField)으로 찾는 모든 게임 멤버의 단일 원장.
    // 게임 타입을 참조하지 않는다(문자열만) → tests\PackRulesTests.cs 가 dnlib 으로 Assembly-CSharp.dll 과 대조한다(오프라인 잠금).
    // 코드에 새 이름 리터럴을 더하면 여기에도 더해야 한다: 시험이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // Walk = 코드가 기반 형식까지 올라가며 찾는다(BlueprintUtil.FindField/SetField/Archetype.Fld). Exact = 질의한 바로 그 형식에 선언돼 있어야 한다(GetField 직접 호출, 기반 형식은 찾지 않음).
    public enum LookupKind { Field, StaticField, Method, Property }
    public enum LookupMode { Walk, Exact }

    public sealed class ReflectionLookup
    {
        public readonly string Assembly;   // "Assembly-CSharp" 또는 "mscorlib"(= 실제 리플렉션으로 확인)
        public readonly string TypeFullName;
        public readonly string Member;
        public readonly LookupKind Kind;
        public readonly LookupMode Mode;
        public ReflectionLookup(string assembly, string type, string member, LookupKind kind, LookupMode mode)
        { Assembly = assembly; TypeFullName = type; Member = member; Kind = kind; Mode = mode; }
    }

    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m, LookupMode mode) { return new ReflectionLookup(A, type, m, LookupKind.Field, mode); }

        // 선언 형식은 dnlib 확인 2026-11-07 (Assembly-CSharp.dll): m_DisplayName/m_Description/m_DescriptionShort/m_Icon 은 BlueprintUnitFact 의 private 필드다.
        public static readonly ReflectionLookup[] All =
        {
            // Archetype.Set/GetF (Walk: 파생 형식에서 올라가 찾는다)
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_DisplayName", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Description", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_DescriptionShort", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Icon", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_Difficulty", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes", LookupMode.Walk),
            // BlueprintUtil.SetField 호출처 (Walk)
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnApply", "m_Feature", LookupMode.Walk),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts", LookupMode.Walk),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff", LookupMode.Walk),
            F("Kingmaker.Designers.Mechanics.Facts.RemoveFeatureOnApply", "m_Feature", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.AddClassLevels", "m_CharacterClass", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.AddClassLevels", "m_Archetypes", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.AddClassLevels", "m_SelectSpells", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.AddClassLevels", "m_MemorizeSpells", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.SelectionEntry", "m_Selection", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.SelectionEntry", "m_Features", LookupMode.Walk),
            // 그림자 무리 T2(2026-11-14 dnlib: 둘 다 private [SerializeField]) — AuxUnits.BuildClone 이 복제본 두뇌를, ShadowBrains.Build 가 그림자 두뇌 행동 목록을 쓴다
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Brain", LookupMode.Walk),
            F("Kingmaker.AI.Blueprints.BlueprintBrain", "m_Actions", LookupMode.Walk),
            // 2026-11-16 보조 본성 특성(AuxUnits.BuildNature): 크기 변경 형식·크기(SetField = Walk, 되읽기 GetField = Exact), 기회 공격 면역 조건(SetField = Walk)
            F("Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize", "m_Type", LookupMode.Walk),
            F("Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize", "Size", LookupMode.Walk),
            F("Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize", "m_Type", LookupMode.Exact),
            F("Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize", "Size", LookupMode.Exact),
            F("Kingmaker.UnitLogic.FactLogic.AddCondition", "Condition", LookupMode.Walk),
            // 2026-11-16 무리 강화(PackEmpower.cs): 자원·자원 소모·자원 부여·레벨 전제조건·시전자 레벨 계산(SetField = Walk), 자원 상한(비공개 중첩 구조체 Amount — FindField = Walk)
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Icon", LookupMode.Walk),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount", LookupMode.Walk),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "BaseValue", LookupMode.Walk),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevel", LookupMode.Walk),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByStat", LookupMode.Walk),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass", "m_CharacterClass", LookupMode.Walk),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource", LookupMode.Walk),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_IsSpendResource", LookupMode.Walk),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel", "m_CharacterClass", LookupMode.Walk),
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_DisplayName", LookupMode.Walk),
            // PackCall.Names(BlueprintUnitFact f ...) 는 BlueprintFeature / BlueprintBuff / BlueprintActivatableAbility 에 부른다 (Walk)
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DisplayName", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Description", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DescriptionShort", LookupMode.Walk),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Icon", LookupMode.Walk),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_DisplayName", LookupMode.Walk),
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_DisplayName", LookupMode.Walk),
            // 질의한 형식에 직접 선언된 것만 찾는 GetField/GetMethod/GetProperty (Exact)
            F("Kingmaker.Blueprints.BlueprintComponent", "m_Flags", LookupMode.Exact),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_AddFacts", LookupMode.Exact),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Brain", LookupMode.Exact),   // AuxUnits.BrainField(원본 참조 객체 불변 단언)
            F("Kingmaker.Blueprints.JsonSystem.BlueprintsCache", "m_LoadedBlueprints", LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.FactLogic.AddPet", "RankToLevelAnimalCompanion", LookupKind.StaticField, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.FactLogic.AddPet", "GetPetLevel", LookupKind.Method, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.FactLogic.AddPet", "TryLevelUpPet", LookupKind.Method, LookupMode.Exact),
            // TestUnit.DataProperty() 가 AddPet 부터 기반 형식으로 올라가며 찾는다
            new ReflectionLookup(A, "Kingmaker.UnitLogic.FactLogic.AddPet", "Data", LookupKind.Property, LookupMode.Walk),
            new ReflectionLookup("mscorlib", "System.Object", "MemberwiseClone", LookupKind.Method, LookupMode.Exact),
            // 2026-11-16 무리 강화 수치 대조(PackEmpower.SpecOf): 원본 랭크 설정의 필드(typeof(ContextRankConfig).GetField = Exact)와, 순수 식을 대조할 엔진 메서드(GetMethod = Exact)
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_BaseValueType", LookupMode.Exact),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_AffectedByIntensifiedMetamagic", LookupMode.Exact),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Progression", LookupMode.Exact),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_StartLevel", LookupMode.Exact),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_StepLevel", LookupMode.Exact),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMin", LookupMode.Exact),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Min", LookupMode.Exact),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMax", LookupMode.Exact),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Max", LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "ApplyProgression", LookupKind.Method, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "ApplyMinMax", LookupKind.Method, LookupMode.Exact),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
        };
    }
}
