using System.Collections.Generic;

namespace OathboundRevenant
{
     
    // Archetype.Build 가 이 값을 그대로 Bp 에 쓰고 되읽는다. 시험 jbp: spellless precedent 가 본편 jbp 와 칸 단위로 대조.
    public static class ArchetypeFlags
    {
        // 출처: 본편 WarriorOfTheHolyLightArchetype RemoveSpellbook true(blueprints.zip 2026-11-14).
        public static readonly bool RemoveSpellbook = true;
        // 출처: 같은 블루프린트 ChangeCasterType true.
        public static readonly bool ChangeCasterType = true;
        // 출처: 같은 블루프린트 IsArcaneCaster false.
        public static readonly bool IsArcaneCaster = false;
        // 출처: 같은 블루프린트 IsDivineCaster false.
        public static readonly bool IsDivineCaster = false;
        // 출처: 같은 블루프린트 m_ReplaceSpellbook null(대체 주문서 없음).
        public static readonly string ReplaceSpellbookGuid = null;
    }

     
    public static class HandsPlan
    {
        public static readonly string SourceGuid = Guids.Game.LayOnHandsOthers;
        // 바꿀 능력 필드(이름, 값) — 원본은 Enemies/Friends/Self 모두 true(blueprints.zip 2026-11-14).
        public static readonly IReadOnlyList<KeyValuePair<string, bool>> FieldChanges = new[]
        {
            new KeyValuePair<string, bool>("CanTargetEnemies", false),
            new KeyValuePair<string, bool>("CanTargetFriends", true),
            new KeyValuePair<string, bool>("CanTargetSelf", false),
        };
        // 원본 컴포넌트 형식 순서(유지 — 그대로 복제, blueprints.zip LayOnHandsOthers 2026-11-14). 원본이 이 순서가 아니면 빌드 실패(본편 변경 검출).
        public static readonly IReadOnlyList<string> KeptComponents = new[]
        {
            "AbilityResourceLogic", "AbilityEffectRunAction", "AbilitySpawnFx", "ContextRankConfig", "AbilityUseOnRest", "AbilityDeliverTouch",
            "AbilityCasterAlignment", "SpellDescriptorComponent", "TacticalCombatResurrection", "AbilityTargetNotSelf", "ContextCalculateSharedValue", "ContextCalculateSharedValue",
        };
        // 추가 컴포넌트(뒤에 덧붙임): 아군만(AbilityTargetIsAlly{Not = false}).
        // + 판정 P21(T8): 음에너지 친화 아군(내 언데드 말 포함) 대상 불가 = AbilityTargetHasFact{NEA, Inverted}.
        public static readonly IReadOnlyList<string> AddedComponents = new[] { "AbilityTargetIsAlly", "AbilityTargetHasFact" };
        // 극대화 명세: 20레벨(HolyChampion 과 같은 진행표 항목) — AutoMetamagic{Maximize, Abilities = [복제본]}(본편 HolyChampion 의 AutoMetamagic 복제, 목록만 바꿈).
        public static readonly string MaximizeMetamagic = "Maximize";
        // 망자의 손 특성 묶음: AddFacts[LayonHandsFact, 복제본] + ReplaceCasterLevelOfAbility{복제본, PaladinClass}.
        public static readonly IReadOnlyList<string> FeatureFacts = new[] { Guids.Game.LayonHandsFact, Guids.RevenantLayOnHands };
    }
}
