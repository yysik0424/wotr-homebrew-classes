using WotrClassMod.Common;

namespace WeaponSpiritShaman
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: 시험(CommonTests)이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField(Archetype 의 GetF) — 기반 형식까지 올라간다. 선언 형식은 블라스트 메이거스 원장과 같은 멤버(dnlib 2026-11-07),
    // 시험이 Assembly-CSharp.dll 에서 다시 찾는다.
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            // Archetype.Build (m_ParentClass) · Archetype.Register (m_Archetypes — 등록 예외, 설계 §3 보충)
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
             
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Progression"),
            F("Kingmaker.Blueprints.Classes.BlueprintProgression", "m_Classes"),
            F("Kingmaker.Blueprints.Classes.LevelEntry", "m_Features"),
            F("Kingmaker.Blueprints.BlueprintUnit/UnitBody", "m_PrimaryHand"),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Portrait"),
            // SpiritPortrait (2026-11-17, dnlib 직독): PortraitData 의 그림 링크 4개(SerializeField 비공개 SpriteLink). get_PetEyePortrait @0001 이 m_PetEyeImage 를 읽는다.
            F("Kingmaker.Blueprints.PortraitData", "m_PortraitImage"),
            F("Kingmaker.Blueprints.PortraitData", "m_HalfLengthImage"),
            F("Kingmaker.Blueprints.PortraitData", "m_FullLengthImage"),
            F("Kingmaker.Blueprints.PortraitData", "m_PetEyeImage"),
            F("Kingmaker.UnitLogic.FactLogic.LockEquipmentSlot", "m_SlotType"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DisplayName"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Description"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DescriptionShort"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Icon"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_Enchantments"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_Cost"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_VisualParameters"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_SecondWeapon"),
            F("Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters", "m_WeaponModel"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet", "m_Pet"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet", "m_LevelRank"),
            F("Kingmaker.UnitLogic.FactLogic.AddPet", "m_LevelContextValue"),
            F("Kingmaker.UnitLogic.FactLogic.AddParametrizedFeatures", "m_Features"),
            F("Kingmaker.UnitLogic.FactLogic.AddParametrizedFeatures/FeatureData", "m_Feature"),
            F("Kingmaker.UnitLogic.FactLogic.AddParametrizedFeatures/FeatureData", "ParamWeaponCategory"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnApply", "m_Feature"),
            // 성장 특기(소유자 결정 2026-11-18) — SpiritBlueprints.BuildTierFeatures (dnlib 2026-11-18 직독: 셋 다 비공개 직렬화 필드).
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel", "m_Class"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel", "m_Feature"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel", "m_AdditionalClasses"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel", "m_Archetypes"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureToPet", "m_PetType"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureToPet", "m_Feature"),
            F("Kingmaker.DLC.DlcCondition", "m_DlcReward"),
            F("Kingmaker.DLC.DlcCondition", "m_HideInstead"),
            F("Kingmaker.Blueprints.Classes.SelectionEntry", "m_ParametrizedFeature"),
            // Checks\WssTestUnit — 펫 레벨(무리 조련사 PackTestUnit 과 같은 멤버, 같은 방식): GetField/GetMethod 직접 = Exact, Data 는 기반 형식으로 올라가며 찾는다.
            new ReflectionLookup(A, "Kingmaker.UnitLogic.FactLogic.AddPet", "RankToLevelAnimalCompanion", LookupKind.StaticField, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.FactLogic.AddPet", "GetPetLevel", LookupKind.Method, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.FactLogic.AddPet", "TryLevelUpPet", LookupKind.Method, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.FactLogic.AddPet", "Data", LookupKind.Property, LookupMode.Walk),
             
            // 근거(dnlib 2026-11-09): SaveManager.<SerializeAndSaveThread>b__0 @0017 ldsfld DefaultJsonSettings::DefaultSettings → @001C JsonSerializer.Create.
            new ReflectionLookup(A, "Kingmaker.EntitySystem.Persistence.JsonUtility.DefaultJsonSettings", "DefaultSettings", LookupKind.StaticField, LookupMode.Exact),
             
            // 근거(dnlib 2026-11-09): RestController.ApplyRestInterval @0014 · CampCoroutine MoveNext @0127 이 부른다, @0039 ResurrectOnRest 깃발 · @0055 UnitDescriptor.Resurrect.
            new ReflectionLookup(A, "Kingmaker.Controllers.Rest.RestController", "ResurrectDeadUnits", LookupKind.Method, LookupMode.Exact),
            // 2026-11-18 주인 상실 수리 — MirrorPart.cs SpiritMirrorComponent.MasterLinkOf: 펫 파트의 주인 연결(비공개 [JsonProperty] 인스턴스 필드, typeof(UnitPartPet).GetField 직접 = Exact).
            // 근거(dnlib 2026-11-18): UnitPartPet.get_Master @0001 ldflda m_MasterRef · Clear @0001 initobj m_MasterRef · Initialize @0007 stfld m_MasterRef.
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Parts.UnitPartPet", "m_MasterRef", LookupKind.Field, LookupMode.Exact),
             
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Max"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_UseMax"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Min"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Icon"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "BaseValue"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevel"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByStat"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_Class"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_Archetypes"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevelStartPlusDivStep"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_ClassDiv"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_ArchetypesDiv"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "StartingLevel"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "StartingIncrease"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "LevelStep"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "PerStepIncrease"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "MinClassLevelIncrease"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "OtherClassesModifier"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_IsSpendResource"),
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
        };
    }
}
