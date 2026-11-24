using System.Collections.Generic;

namespace BulwarkMage
{
    // [순수] 중장법사의 레벨·다이얼 상수 — 한 곳(C3). 게임 형식 없음(modinfo testSources). 시험 `dial: single block` 이
    // 생산 코드의 수치 정적 필드가 이 두 블록(Levels·Dials) 밖에 없음을 고정하고, 각 필드 위의 「// 출처:」 줄을 본다.
     
    // T1 이 쓰는 것은 RemovedBonusFeatLevels·MinLevel·MaxLevel 뿐이다. 나머지는 T3~T7 이 쓴다(지금은 값과 출처만 고정).
    public static class BulwarkRules
    {
        public static class Levels
        {
            // 출처: 정찰 §8(WizardProgression 02f30498… LevelEntries 직독 — 1·5·10·15·20에 WizardFeatSelection 8c3102c2…), 설계 §0-7·§5.4 「보너스 특기 5개 전부 제거」
            public static readonly IReadOnlyList<int> RemovedBonusFeatLevels = new[] { 1, 5, 10, 15, 20 };
             
            public static readonly IReadOnlyList<int> RebukeLearnLevels = new[] { 1, 5, 10, 15, 20 };
            // 출처: TowerShieldSpecialistArchetype a599da9a… AddFeatures 직독(정찰 §3, 설계 §5.2) — 폭발 차단 2/6/10/14/18
            public static readonly IReadOnlyList<int> BurstBarrierLevels = new[] { 2, 6, 10, 14, 18 };
            // 출처: 설계 §5.2 레벨표(TSS 타워실드 훈련 3/7/11/15, ⚑M1)
            public static readonly IReadOnlyList<int> TowerTrainingLevels = new[] { 3, 7, 11, 15 };
             
            public static readonly IReadOnlyList<int> ArmorTrainingLevels = new[] { 3, 7, 11, 15 };
            // 출처: TSS a599da9a… AddFeatures 직독(설계 §5.2) — 타워실드 페널티 무효 5
            public static readonly int NegatePenaltyLevel = 5;
            // 출처: TSS a599da9a… AddFeatures 직독(설계 §5.2) — 접촉 방패 9
            public static readonly int TouchShieldLevel = 9;
            // 출처: TSS a599da9a… AddFeatures 직독(설계 §5.2) — 완전 엄폐 13
            public static readonly int TotalCoverLevel = 13;
            // 출처: TSS a599da9a… AddFeatures 직독(설계 §5.2) — 타워실드 회피 16
            public static readonly int EvasionLevel = 16;
            // 출처: TSS a599da9a… AddFeatures 직독(설계 §5.2) — 향상된 타워실드 회피 20
            public static readonly int ImprovedEvasionLevel = 20;
             
            public static readonly int IronConcentrationLevel = 5;
            // 출처: 설계 §5.3(3) 작성자 제안 = 기본값 Q6(5·10·15·20 에 +1)
            public static readonly IReadOnlyList<int> BondEnhancementLevels = new[] { 5, 10, 15, 20 };
            // 출처: 엔진 캐릭터 레벨 범위(XPTable 20행 — TestUnit.cs:45 의 1..20 검사와 같은 값)
            public static readonly int MinLevel = 1;
            // 출처: 엔진 캐릭터 레벨 범위(XPTable 20행 — TestUnit.cs:45 의 1..20 검사와 같은 값)
            public static readonly int MaxLevel = 20;
        }

        public static class Dials
        {
             
            public static readonly int RebukesPerRound = 1;
             
            public static readonly int RebukeDailyBase = 3;
             
            public static readonly int RebukeDailyMin = 1;
            // 출처: 본편 ItemBondResource fbc6de6f… m_MaxAmount.BaseValue 1(정찰 §6)
            public static readonly int BondRecallsPerDay = 1;
             
            public static readonly int IronForcedDC = -1000;
             
            // 출처: 설계 §R3.2 E1·E2·E3 「⌈L/2⌉d6 / ⌈L/2⌉d4」(위저드 레벨 2당 주사위 1, 올림)
            public static readonly int RebukeLevelsPerDie = 2;
            // 출처: 설계 §R3.2 E3 「비틀거림 1라운드」
            public static readonly int RebukeStaggerRounds = 1;
            // 출처: 설계 §R3.2 E4 「10피트 밀어냄」
            public static readonly int RebukePushFeet = 10;
             
            public static readonly int EngineMicrometersPerFoot = 304800;
            // 출처: 설계 §R3.2 E5 「1 + ⌊L/5⌋ 라운드」의 1
            public static readonly int RebukeSlowBaseRounds = 1;
            // 출처: 설계 §R3.2 E5 「1 + ⌊L/5⌋ 라운드」의 5(위저드 레벨 5당 1라운드, 내림)
            public static readonly int RebukeSlowLevelsPerRound = 5;
            // 출처: 설계 §R3.2 E6 「멍해짐 1라운드」
            public static readonly int RebukeDazeRounds = 1;
            // 출처: 엔진 RuleCalculateAbilityParams @023C~0240 DC = 10 + 주문 레벨 + 능력치 보너스(설계 §R3.2 공통 부품)
            public static readonly int SaveDcBase = 10;
            // 출처: 엔진 ContextCalculateAbilityParamsBasedOnClass.Calculate @00AD~00B9 주문 레벨 = GetClassLevel / 2(정수 나눗셈, 설계 §R3.2)
            public static readonly int SaveDcLevelDivisor = 2;
             
            // 출처: 엔진 ContextSpendResource.RunAction @0043 ldc.i4.1 — ContextValueSpendure = false 면 정확히 1 을 쓴다(T5 Step 0). 자원 조건의 필요량도 이 값(같은 1).
            public static readonly int RebukeUsesPerFiring = 1;
            // 출처: 본편 DivinationSchoolForetellAidToggleAbility 3cb265f5… WeightInGroup 1 + ActivatableAbility.TryStart @007D~011A 그룹 무게 합 > GetGroupSize(기본 1) 이면 시작 안 함 → 그룹에서 하나만 켜짐
            public static readonly int RebukeToggleWeight = 1;
             
            // (A2b 판정 2026-11-14: BuffEnchantArmor 를 쓰지 않게 되어 고정 색인 다이얼 BondScalingStartingMod·BondScalingModifier 는 지웠다 — 캐릭터 레벨 21 한계도 함께 사라짐.)
            // 출처: blueprints.zip ArmorEnhancementBonus1~4.jbp AdvanceArmorStats.ArmorCheckPenaltyShift 1(우리 BondEnchant{n} 은 그 컴포넌트의 복사본) + 엔진 AdvanceArmorStats.OnEventAboutToTrigger @0011~0016(마법부여마다 따로 AddBonus) — 층마다 ACP +1(시험·점검 오라클용, 보고 미결 항목 A6)
            public static readonly int BondAcpShiftPerEnchantment = 1;
        }
    }
}
