using WotrClassMod.Common;

namespace ShadowSorcerer
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: CommonTests 가 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // Walk = BlueprintUtil.SetField/FindField — 기반 형식까지 올라간다. 시험이 Assembly-CSharp.dll 에서 다시 찾는다.
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Abilities.AbilityData", "ForceTargetOwner", LookupKind.Property, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Abilities.AbilityData", "ParamSpellSlot", LookupKind.Property, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Abilities.AbilityData", "ParamSpellbook", LookupKind.Property, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Abilities.AbilityData", "ParamSpellLevel", LookupKind.Property, LookupMode.Exact),
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Abilities.AbilityData", "ParamUnitFact", LookupKind.Property, LookupMode.Exact),
            // Archetype.Build (m_ParentClass) · Archetype.Register (m_Archetypes — 등록 예외)
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            // 우리 특성의 이름·설명(선언 형식 BlueprintUnitFact, Walk)
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DisplayName"),
            // Icons.Apply(2026-11-15): 본편 원본·우리 블루프린트의 아이콘(선언 BlueprintUnitFact)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_Description"),
            // RuntimeReadback 판독기 — LevelEntry 의 특성 참조
            F("Kingmaker.Blueprints.Classes.LevelEntry", "m_Features"),
            // T2 BonusSpells.Build — AddKnownSpell 직렬화 필드(선언 형식 AddKnownSpell, dnlib 2026-11-14)
            F("Kingmaker.UnitLogic.FactLogic.AddKnownSpell", "m_CharacterClass"),
            F("Kingmaker.UnitLogic.FactLogic.AddKnownSpell", "m_Spell"),
            F("Kingmaker.UnitLogic.FactLogic.AddKnownSpell", "m_Archetype"),
            // EchoMarker(2026-11-16 메아리 세기) — 엔진 불신 목록(표식 항목을 넣고 읽는다). 선언 형식 MechanicsContext, 형식 잠금 = EchoMarker.FindListField. dnlib 2026-11-16.
            F("Kingmaker.UnitLogic.Mechanics.MechanicsContext", "m_ShadowDisbelieveData"),
            // T4 점검 S6 special-list-spell-echoes — Spellbook.AddSpecial(int, BlueprintSpellList, BlueprintAbility) private(정찰 §1).
            new ReflectionLookup(A, "Kingmaker.UnitLogic.Spellbook", "AddSpecial", LookupKind.Method, LookupMode.Exact),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
            // 2026-11-17 메아리 연출 — ShadowUnitBlueprint(시각 전용 그림자 유닛): 껍데기 특성의 짧은 설명(선언 BlueprintUnitFact) · AddMechanicsFeature 의 종류 · 유닛 사본의 진영·두뇌·시작 인벤토리(BlueprintUtil.SetField / FindField = Walk).
            F("Kingmaker.Blueprints.Classes.BlueprintFeature", "m_DescriptionShort"),
            F("Kingmaker.UnitLogic.FactLogic.AddMechanicsFeature", "m_Feature"),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Faction"),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_Brain"),
            F("Kingmaker.Blueprints.BlueprintUnit", "m_StartingInventory"),
            // ShadowDoubles.Create — 엔진의 저장 판정 함수(private static, GetMethod 직접 = Exact). 서명·본문 잠금 = 시험 exec: shadow unit engine.
            new ReflectionLookup(A, "Kingmaker.EntitySystem.Persistence.JsonUtility.SceneEntitiesStateConverter", "ShouldSerialize", LookupKind.Method, LookupMode.Exact),
        };
    }
}
