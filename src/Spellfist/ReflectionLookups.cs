using WotrClassMod.Common;

namespace Spellfist
{
    // 이 모드 코드가 리플렉션으로 찾는 게임 멤버의 원장(형식·공용 몫은 src\Common\ReflectionLookupTypes.cs). 게임 타입을 참조하지 않는다(문자열만).
    // 코드에 새 이름 리터럴(GetField/GetMethod/GetProperty/Set/GetF/SetField)을 더하면 여기에도 더해야 한다: 시험이 소스를 읽어 표에 없는 리터럴을 실패시킨다.
    // (BrawlerHook.Resolve 의 AccessTools.Method(..., "Postfix") 는 게임 멤버가 아니라 이 DLL 에 컴파일된
    // 공용 형식의 메서드라 이 원장(Assembly-CSharp 대조) 대상이 아니다 — tests\SpellfistTests.cs CheckHook 이 dnlib 으로 이 DLL 에서 존재를 대조한다.)
    // 전부 Walk: Archetype/SchoolLists/Spellbook 의 SetField·GetF 는 BlueprintUtil.FindField 로 기반 형식까지 올라간다. 선언 형식은 dnlib 확인 2026-11-07.
    public static class ReflectionLookups
    {
        const string A = "Assembly-CSharp";
        static ReflectionLookup F(string type, string m) { return new ReflectionLookup(A, type, m, LookupKind.Field, LookupMode.Walk); }

        public static readonly ReflectionLookup[] All =
        {
            // Archetype
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ParentClass"),
            F("Kingmaker.Blueprints.Classes.BlueprintArchetype", "m_ReplaceSpellbook"),
            F("Kingmaker.Blueprints.Classes.BlueprintCharacterClass", "m_Archetypes"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoArchetype", "m_CharacterClass"),
            F("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoArchetype", "m_Archetype"),
            // SchoolLists (SpellLevelList 비공개 필드 — 정찰 §0-2; BlueprintSpellList.m_MaxLevel 캐시)
            F("Kingmaker.Blueprints.Classes.Spells.SpellLevelList", "m_Spells"),
            F("Kingmaker.Blueprints.Classes.Spells.SpellLevelList", "m_SpellsFiltered"),
            // 첫 프레임 재동기화(SchoolLists.Resync): get_Spells 가 m_Spells 를 감싼 프록시를 처음 접근 때 저장한다(SpellLevelList.get_Spells IL_0000~0018, dnlib 2026-11-17) → 비운다.
            F("Kingmaker.Blueprints.Classes.Spells.SpellLevelList", "m_SpellsProxy"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList", "m_MaxLevel"),
            // Spellbook 복제(BlueprintSpellbook 비공개 참조 필드)
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook", "m_SpellsPerDay"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook", "m_SpellsKnown"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook", "m_SpellSlots"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook", "m_SpellList"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook", "m_MythicSpellList"),
            F("Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook", "m_CharacterClass"),
            // 학파 특성(LearnSpellList)
            F("Kingmaker.UnitLogic.FactLogic.LearnSpellList", "m_CharacterClass"),
            F("Kingmaker.UnitLogic.FactLogic.LearnSpellList", "m_Archetype"),
            F("Kingmaker.UnitLogic.FactLogic.LearnSpellList", "m_SpellList"),
            // 학파 선택
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_AllFeatures"),
            F("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", "m_Features"),
            // Spellbook.Names (BlueprintUnitFact 의 private 필드; Feature·Selection 에 부른다)
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DisplayName"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Description"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_DescriptionShort"),
            // 점검 S13 d8-branch: 얼음 유도탄·번개 창 복제의 조건 사실 참조(비공개 직렬화 필드, Fact 게터는 해석된 블루프린트를 돌려줘 GUID 대조에 쓰지 않는다 — dnlib 2026-11-17)
            F("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact", "m_Fact"),
            // Reservoir (AddAbilityResources / IncreaseResourcesByClass 의 직렬화 참조 필드; get_* 는 null 안전 — dnlib IL 확인 2026-11-07)
            F("Kingmaker.Designers.Mechanics.Facts.AddAbilityResources", "m_Resource"),
            F("Kingmaker.UnitLogic.FactLogic.IncreaseResourcesByClass", "m_Resource"),
            F("Kingmaker.UnitLogic.FactLogic.IncreaseResourcesByClass", "m_CharacterClass"),
            F("Kingmaker.UnitLogic.FactLogic.IncreaseResourcesByClass", "m_Archetype"),
            // 자원 형태 검사(Reservoir.CheckResourceShape): BlueprintAbilityResource.m_MaxAmount 와 Amount 의 비공개 클래스 배열
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_MaxAmount"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "BaseValue"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByStat"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_Class"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "m_ClassDiv"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource/Amount", "IncreasedByLevelStartPlusDivStep"),
            F("Kingmaker.Blueprints.BlueprintAbilityResource", "m_UseMax"),
            // Exploits(복제 체인): ContextRankConfig 비공개 필드(dnlib 2026-11-08: 전부 ContextRankConfig 선언), AbilityResourceLogic 자원, SimpleBlueprint 의 Element 목록(protected).
            // object.MemberwiseClone 은 공용 원장(CommonLookups) 항목을 그대로 쓴다 — 같은 (형식, 멤버)를 두 원장에 두면 시험이 실패한다.
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Class"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "Archetype"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_AdditionalArchetypes"),
            F("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig", "m_Stat"),
            F("Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "m_RequiredResource"),
            F("Kingmaker.Blueprints.SimpleBlueprint", "m_AllElements"),
            // Quicken(dnlib 2026-11-08): 토글→버프 참조, 숨김 플래그(BlueprintBuff/Flags.HiddenInUi), 특성 AddFacts, 아이콘, AutoMetamagic 허용 종류(기본 SpellOnly 되읽기).
            F("Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility", "m_Buff"),
            F("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff", "m_Flags"),
            F("Kingmaker.UnitLogic.FactLogic.AddFacts", "m_Facts"),
            F("Kingmaker.Blueprints.Facts.BlueprintUnitFact", "m_Icon"),
            F("Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", "m_AllowedAbilities"),
            // 핫픽스 3(dnlib 2026-11-08): ctor 가 채우지 않는 클래스 필터 배열(private BlueprintCharacterClassReference[]) — 빈 배열을 명시적으로 넣고 되읽는다.
            F("Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", "m_IncludeClasses"),
            F("Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", "m_ExcludeClasses"),
            // FistSpellstrike(dnlib 2026-11-08): AddMagusMechanicPart.m_Feature(private, 열거 AddMagusMechanicPart/Feature — private 중첩), UnitPartMagus.m_Spellbook(private Kingmaker.UnitLogic.Spellbook, 비직렬화).
            F("Kingmaker.UnitLogic.FactLogic.AddMagusMechanicPart", "m_Feature"),
            F("Kingmaker.UnitLogic.Parts.UnitPartMagus", "m_Spellbook"),
            // RulebookRegistry.IsGloballySubscribed — 규칙 처리기 구독 판정(EventBus.IsGloballySubscribed 는 IGlobalRulebookHandler<T> 만 구현한 객체에 항상 거짓, 2026-11-15 골렘술사 로그 결함).
            // 비공개 m_Listeners(GetField 직접 = Exact) · SubscribersList<>.List(닫힌 SubscribersList<> 형식에 직접 GetField = Exact). 필드 형식 잠금 = 시험 CheckRulebookSubscriptionProbes. dnlib 2026-11-15.
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.RulebookSubscriptionManager`1", "m_Listeners", LookupKind.Field, LookupMode.Exact),
            new ReflectionLookup("Assembly-CSharp", "Kingmaker.PubSubSystem.SubscribersList`1", "List", LookupKind.Field, LookupMode.Exact),
        };
    }
}
