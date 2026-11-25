using WotrClassMod.Common;

namespace BlastMagus
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: 시험이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField(Archetype·BlastBlueprints 의 GetF) — 기반 형식까지 올라간다. Exact = GetField 직접 호출(BlastPartGuard).
     
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature", "m_Feature"),
            // Archetype
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // BlastBlueprints.Names (BlueprintUnitFact 의 private 필드; Feature·Selection 에 부른다)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DisplayName"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
             
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_AllowNonContextActions"),
            // 원소 선택지
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features"),
            // 0 자원(Amount 는 비공개 중첩 구조체)
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_Max"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_UseMax"),
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
            F("Kingmaker.UnitLogic.FactLogic.AttackStatReplacement", "m_WeaponTypes"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureIfHasFact", "m_CheckedFact"),
            F("Kingmaker.Designers.Mechanics.Facts.AddFeatureIfHasFact", "m_Feature"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistBurnModifier", "m_AppliableTo"),
            // AddKineticistPart (정찰 §4 필드표)
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_Class"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_MaxBurn"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_MaxBurnPerRound"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_GatherPowerAbility"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_GatherPowerIncreaseFeature"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_GatherPowerBuff1"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_GatherPowerBuff2"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_GatherPowerBuff3"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_Blasts"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_BladeActivatedBuff"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_QuiverBuff"),
            F("Kingmaker.UnitLogic.Class.Kineticist.AddKineticistPart", "m_CanGatherPowerWithShieldBuff"),
            // Energize 토글
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            // 폭발(BlastStrike.Build)
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff", "m_Buff"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_IsSpendResource"),
            F("Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasFacts", "m_Facts"),
             
            F("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility", "m_Parent"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityVariants", "m_Variants"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityShowIfCasterHasFact", "m_UnitFact"),
            F("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "m_Type"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Weapon"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_BaseValueType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Feature"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_FeatureList"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_CustomProperty"),
            // BlastPartGuard.SettingsOf (typeof(UnitPartKineticist).GetField 직접 호출 = Exact; m_Settings 는 UnitPartKineticist 에 선언)
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Class.Kineticist.UnitPartKineticist", "m_Settings", LookupKind.Field, LookupMode.Exact),
             
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Flags", LookupKind.Field, LookupMode.Exact),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
        };
    }
}
