using WotrClassMod.Common;

namespace Warlock
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: 시험(CommonTests)이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField(Archetype 의 GetF) — 기반 형식까지 올라간다. 선언 형식은 WSS·Spellfist 원장과 같은 멤버, 시험이 Assembly-CSharp.dll 에서 다시 찾는다.
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            // T2 Archetype.Build (m_ParentClass · m_ReplaceSpellbook) · Archetype.Register (m_Archetypes — 등록 예외 ①)
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ReplaceSpellbook"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // T2 Archetype.Build — PrerequisiteNoArchetype{WitchClass, AccursedWitchArchetype}(Spellfist 원장과 같은 멤버)
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoArchetype", "m_CharacterClass"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoArchetype", "m_Archetype"),
            // T3 KitBuild.Names — BlueprintUnitFact 의 private 필드(BlastMagus 원장과 같은 멤버)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DisplayName"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            // T3 KitBuild.Rank — ContextRankConfig 필드 전부 private(정찰 §16-T3)
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_BaseValueType"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Progression"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_StartLevel"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_StepLevel"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Stat"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMin"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_UseMax"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Class"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_AdditionalArchetypes"),
            // T3 Blasts.BuildRay
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Projectiles"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile", "m_Weapon"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage", "m_Type"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass", "m_CharacterClass"),
            // T3 Summons.Build(부모 변형 0) · LevelFeatures.Build(AugmentSummoning AddFacts)
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityVariants", "m_Variants"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            // T5 Summons.BuildVariant — 소환 액션·버프 액션의 private 필드(정찰 §16-T5)
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster", "m_Blueprint"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster", "m_SummonPool"),
            F("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff", "m_Buff"),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
        };
    }
}
