using System;
using System.Linq;

namespace Warlock
{
    // [순수] 레벨 자리·다이얼(계획 Global Constraints 「상수는 이름·출처와 한 곳에」 — 이 파일의 Levels·Dials 두 블록이 유일한 원장, 시험 `dial: single block`).
    // 규칙(블라스트 주사위·보너스·가호 단계·최대 소환 단계). 게임 형식 없음 → 콘솔 시험이 직접 실행한다(modinfo testSources).
    public static class WarlockRules
    {
        public const int MinLevel = 1; // 위치 직업 레벨 하한(설계 §5 레벨표 L1)
        public const int MaxLevel = 20; // 위치 직업 레벨 상한(설계 §5 레벨표 L20)

         
        // (규칙은 레벨 숫자가 아니라 마커 사실 보유를 읽는다 — 이 숫자는 Archetype.AddAt 에서만 쓴다, 계획 Global Constraints).
        public static class Levels
        {
            public static readonly int[] HexLevels = { 1, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 }; // 설계 §0-13·§4.10 — WitchProgression 의 WitchHexSelection 자리 11(jbp 시험 `jbp: removed pairs` 가 대조)
            public static readonly int[] WardLevels = { 2, 8, 14, 18 }; // 설계 §5 그룹 가호 5/10/20/면역 자리
            public const int AugmentLevel = 4; // 설계 §5 L4 소환 강화
            public const int ReachLevel = 6; // 설계 §5 L6 사거리 Medium
            public const int DoubleLevel = 10;  
            public const int HeatAccelLevel = 12; // 설계 §5 L12 과열 가속
            public const int ExtendLevel = 16; // 설계 §5 L16 소환 지속 2배
            public const int CapstoneLevel = 20; // 설계 §5 L20 대사자
        }

        // Dials — 출처: 설계 §4.4(주사위·보너스), §4.5(−2·0.2 = ScorchingRay cdb106d5… DelayBetweenProjectiles), §4.7(최대 9·+1·+2),
        // §4.8(가호 5/10/20·별 AC 1/2/3·SR 10+레벨), §4.6(지속 = 위치 레벨 라운드, L16 ×2), §4.3 매핑표
        public static class Dials
        {
            public const int HeatMax = 9;  
            public const int HeatOnHit = 1;  
            public const int HeatOnConfirmedCrit = 2; // 설계 §4.9 L12 과열 가속 — 치명 확정 명중 +2
            public const int HeatCombatEndDivisor = 2; // 소유자 결정 2026-11-16: 전투가 끝나면 과열 = 과열 ÷ 2(내림) — 「절반」의 유일한 정의(HeatRule.AfterCombat 만 읽는다)
            public const int HeatAfterRest = 0; // 설계 §6-6 「휴식 직후 0」 — 소유자 결정 2026-11-16 에서도 그대로(휴식을 마치면 0)
            public const int DoublePenalty = -2; // 설계 §4.5 연속 사격 줄기당 명중 −2(작성자 다이얼)
            public const float DoubleDelaySeconds = 0.2f; // 설계 §4.5 = 본편 ScorchingRay cdb106d5… DelayBetweenProjectiles
            public const int BlastDieSides = 6; // 설계 §4.4 블라스트 1d6 단위
            public const int BlastStartLevel = 1; // 설계 §4.4 ContextRankConfig StartLevel 1
            public const int BlastStepLevel = 2; // 설계 §4.4 「2레벨마다 1d6」 StepLevel 2
            public static readonly int[] WardValues = { 5, 10, 20 }; // 설계 §4.8 가호 1~3단 에너지 저항(4단 = 면역)
            public static readonly int[] StarAc = { 1, 2, 3 }; // 설계 §4.8 별 그룹 가호 1~3단 편향 AC
            public const int StarSrBase = 10; // 설계 §4.8 별 그룹 4단 주문 저항 = 10 + 위치 레벨
            public const int SummonRoundsPerLevel = 1; // 설계 §4.6 지속 = 위치 레벨 라운드
            public const int ExtendMultiplier = 2; // 설계 §4.9 L16 소환 지속 2배
            public const int SummonTiers = 9; // 설계 §4.6 소환 단계 I~IX
             
            public const float AutoPendingTimeoutSeconds = 6f;  
            public const int IxAugmentBonus = 4;  
        }

        static void RequireLevel(int level, string what)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException("level", level, "Warlock: " + what + " level must be " + MinLevel + ".." + MaxLevel);
        }

        // 블라스트 주사위 수 = 1 + (L − StartLevel) / StepLevel (L1 1 · L3 2 · L5 3 · L19 10 · L20 10). 엔진 ContextRankConfig StartPlusDivStep 과 같은 식(설계 §4.4).
        public static int BlastDice(int level)
        {
            RequireLevel(level, "BlastDice");
            return 1 + (level - Dials.BlastStartLevel) / Dials.BlastStepLevel;
        }

        // 블라스트 고정 보너스 = 매력 수정치 / 2, C# 0 방향 절삭 = 엔진 ContextRankConfig Div2(BM src\BlastMagus\BlastRules.cs:64-65).
        // 설계 §4.4 「내림」과 음수 홀수에서만 다르다(−3 → −1) — 「설계와 달리 정한 것」 ①.
        public static int BlastBonus(int chaMod)
        {
            return chaMod / 2;
        }

        // 그룹 가호 단계 0..4 = 레벨 이하인 WardLevels 자리 수(L1 0 · L2 1 · L8 2 · L14 3 · L18 4).
        public static int WardTier(int level)
        {
            RequireLevel(level, "WardTier");
            return Levels.WardLevels.Count(l => l <= level);
        }

        // 과열 소환 최대 단계 = min(SummonTiers, 최고 주문 레벨)(설계 §4.6). 최고 주문 레벨 0 이하는 판정할 수 없다 → 던진다(게임 쪽은 판정 불가를 null 로 따로 다룬다).
        public static int MaxTierAt(int maxSpellLevel)
        {
            if (maxSpellLevel <= 0) throw new ArgumentOutOfRangeException("maxSpellLevel", maxSpellLevel, "Warlock: MaxTierAt needs a max spell level >= 1");
            return Math.Min(Dials.SummonTiers, maxSpellLevel);
        }
    }
}
