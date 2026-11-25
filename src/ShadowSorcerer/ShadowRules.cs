using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
     
    // const 금지(계획): 소비하는 규칙의 IL 이 ldsfld 로 이 필드를 읽어야 시험 `wiring: rule entry loads ShadowRules` 가 소비를 확인할 수 있다(const 는 리터럴로 접혀 사라진다).
    // 시험 `exec: shadow rules vs transcribed` 가 tests\ShadowSorcererTests.cs 의 손 전사본과 칸 단위로 대조한다.
    public static class ShadowRules
    {
        // 소서러 클래스 레벨 범위(AddAt 범위 검사·되읽기 경로 레벨). 출처: 본편 클래스 최대 레벨 20(SorcererProgression LevelEntries 1~20, jbp 2026-11-14).
        public static readonly int MinLevel = 1;
        public static readonly int MaxLevel = 20;

        // 분신 수(= 메아리 횟수) 단계표 (레벨, 분신 수). 1~20레벨에 분신 하나를 유지한다.
        public static readonly IReadOnlyList<(int level, int count)> EchoCountLevels = new[] { (1, 1) };

        // 아키타입이 지우는 혈통 특기 선택 레벨. 출처: 설계 §5 + SorcererProgression.jbp L7/L13/L19 = SorcererFeatSelection 3a60f0c0…(T1 Step 0 직독).
        public static readonly IReadOnlyList<int> RemovedFeatSelectionLevels = new[] { 7, 13, 19 };

        // 메아리 그림자 계수(%). 본편 MechanicsContext.ShadowFactorPercents 한 필드가 피해 배율(½)과 버프·디버프 확률(50%)을 함께 낸다.
         
        // 2026-11-16 소유자 결정 뒤: 메아리에서는 이 계수가 「불신 성공 시 피해 추가 배율」로만 쓰인다 — 버프·디버프 확률은 굴리지 않는다(H8 EchoNoChancePatch, 불신 성공 = 무효).
        public static readonly int DisbeliefFactorPercent = 50;

        // 메아리 피해 배율(%). 소유자 결정 2026-11-16: 메아리의 피해는 불신 여부와 무관하게 원래의 50%.
        // 불신 성공 시에는 위 그림자 계수가 한 번 더 곱해져 25%(= 50 × 50 / 100) — 25 는 따로 정의하지 않는다(EchoStrengthRule 이 이 필드만 읽는다).
        public static readonly int EchoDamagePercent = 50;

         
        public static readonly IReadOnlyList<(int sorcererLevel, int spellLevel)> BonusSpellLevels =
            Enumerable.Range(1, 9).Select(sl => (2 * sl + 1, sl)).ToArray();

         
        public static readonly IReadOnlyList<string> BonusSpells = new[]
        {
            "f001c73999fb5a543a199f890108d936", // 1 Vanish
            "3e4ab69ada402d145a5e0ad3ad4b8564", // 2 Mirror Image
            "903092f6488f9ce45a80943923576ab3", // 3 Displacement
            "caac251ca7601324bbe000372a0a1005", // 4 Shadow Conjuration
            "237427308e48c3341b3d532b9d3a001f", // 5 Shadow Evocation
            "1f2e6019ece86d64baa5effa15e81ecc", // 6 Phantasmal Putrefaction
            "08255ea4cdd812341af93f9cd113acb9", // 7 Greater Shadow Conjuration
            "3c4a2d4181482e84d9cd752ef8edc3b6", // 8 Greater Shadow Evocation
            "70e12191790f69a439ea0132c75f83aa", // 9 Shades
        };

         
        public static readonly float EchoIntervalSeconds = 0.5f;

         
        public static readonly float EchoWaitGameSeconds = 12f;

        // 한 라운드(실시간 창). 출처: 본편 UnitPartTouch.IsCastedInThisRound IL_0000~002D(1.Rounds().Seconds = 6).
        public static readonly double RoundSeconds = 6.0;

        // 분신 수. 단계표에서 level 이하의 가장 높은 단계. 표 밖(1 미만·20 초과)은 0(호출자 EchoGate 가 0 을 Unknown 으로 본다 — C1).
        public static int EchoCount(int sorcererLevel) { return EchoCountCore(sorcererLevel, EchoCountLevels, MinLevel, MaxLevel); }

        public static int EchoCountCore(int sorcererLevel, IReadOnlyList<(int level, int count)> table, int minLevel, int maxLevel)
        {
            if (table == null || table.Count == 0) throw new InvalidOperationException("ShadowSorcerer: echo count table is empty");
            if (sorcererLevel < minLevel || sorcererLevel > maxLevel) return 0;
            int n = 0;
            for (int i = 0; i < table.Count; i++)
            {
                var entry = table[i];
                if (entry.level <= sorcererLevel) n = entry.count;
            }
            return n;
        }

        // BuildAll 첫 줄. 어긋나면 던진다(빌드 실패 → ModContext.BuildFailed).
        public static void Validate() { ValidateCore(EchoCountLevels, EchoIntervalSeconds, EchoWaitGameSeconds, BonusSpells, BonusSpellLevels); }

        public static void ValidateCore(IReadOnlyList<(int level, int count)> echoLevels, float interval, float wait, IReadOnlyList<string> bonus, IReadOnlyList<(int sorcererLevel, int spellLevel)> bonusLevels)
        {
            if (echoLevels == null || echoLevels.Count == 0) throw new InvalidOperationException("ShadowSorcerer: ShadowRules.EchoCountLevels is empty");
            for (int i = 1; i < echoLevels.Count; i++)
                if (echoLevels[i].level <= echoLevels[i - 1].level || echoLevels[i].count <= echoLevels[i - 1].count)
                    throw new InvalidOperationException("ShadowSorcerer: ShadowRules.EchoCountLevels is not strictly increasing at row " + i);
            if (bonus == null || bonus.Count != 9) throw new InvalidOperationException("ShadowSorcerer: ShadowRules.BonusSpells has " + (bonus == null ? 0 : bonus.Count) + " rows, expected 9");
            if (bonusLevels == null || bonusLevels.Count != 9) throw new InvalidOperationException("ShadowSorcerer: ShadowRules.BonusSpellLevels has " + (bonusLevels == null ? 0 : bonusLevels.Count) + " rows, expected 9");
            if (!(interval > 0)) throw new InvalidOperationException("ShadowSorcerer: ShadowRules.EchoIntervalSeconds " + interval + " must be > 0");
            int maxEchoes = echoLevels.Max(t => t.count);
            if (interval * maxEchoes >= wait) throw new InvalidOperationException("ShadowSorcerer: ShadowRules interval " + interval + " x " + maxEchoes + " >= wait limit " + wait + " (the last echo could never fire)");
        }
    }
}
