using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // [순수] 고통 겹 규칙(설계 §8·§10-b·§11 · 개편 설계 §6-b·§12-4, 개편 계획 2026-11-16 Task O7 · 소유자 결정 2026-11-17 「계수 버프 하나로 재구성」).
    // 「고통 총 겹」 = 숨김 계수 버프의 순위(단일 원장 — CounterTotal). 명중마다 AddFor 겹, 총 겹이 혼란 문턱 이상이 되는 순간 1회 내성 + 소멸, 정신 면역이면 문턱에서 멈춤(내성·소멸 없음).
    // 벌점은 계수의 자식인 벌점 버프 순위 = PenaltyRankFor(총 겹) = min(총 겹, PainPenaltyCap) — 총 겹에서 파생될 뿐 따로 세지 않는다.
    public struct PainStep
    {
        public readonly int NewTotal;
        public readonly bool Save, Clear;
        public PainStep(int newTotal, bool save, bool clear) { NewTotal = newTotal; Save = save; Clear = clear; }
        public override string ToString() { return "{" + NewTotal + "," + (Save ? "T" : "F") + "," + (Clear ? "T" : "F") + "}"; }
    }

    public static class PainRule
    {
        // 개편 설계 §6-b 「벌점 상한 ≤ 혼란 문턱을 코드가 검사한다」: 생산 값으로 형식 초기화 때 1회 — 어긋나면 이 형식을 처음 쓰는 곳(빌드 · 첫 명중)에서 던진다.
        static PainRule() { ValidateCaps(ChaosRules.Dials.PainPenaltyCap, ChaosRules.Dials.PainConfusionThreshold); }

        public static void ValidateCaps(int penaltyCap, int threshold)
        {
            if (penaltyCap < 1 || penaltyCap > threshold)
                throw new InvalidOperationException("ChaosWarrior: pain penalty cap " + penaltyCap + " must be 1.." + threshold + " (the confusion threshold)");
        }

        static int Cap { get { return ChaosRules.Dials.PainPenaltyCap; } }
        static int Threshold { get { return ChaosRules.Dials.PainConfusionThreshold; } }

        // 고통 합산식(개편 설계 §12-4 · 계획 원장 C — 한 곳): 명중당 겹 = PainStacksPerHit + PainAmplifyExtra × 증폭 순위 + PainResonanceExtra × 공명 순위 + (신경독침 무기면 PainNeuroStingExtra).
        // 순위 < 0 → 던짐. 증폭 + 공명 순위 > MaxDraws(분노 한 번에 뽑을 수 있는 최대) → 던짐(표식 순위가 뽑기 수를 넘는 상태는 계약 위반이다).
        public static int AddFor(int amplifyRank, int resonanceRank, bool neuroSting)
        {
            if (amplifyRank < 0) throw new ArgumentOutOfRangeException("amplifyRank", amplifyRank, "ChaosWarrior: pain amplify rank must be >= 0");
            if (resonanceRank < 0) throw new ArgumentOutOfRangeException("resonanceRank", resonanceRank, "ChaosWarrior: pain resonance rank must be >= 0");
            if (amplifyRank + resonanceRank > ChaosRules.MaxDraws)
                throw new ArgumentOutOfRangeException("amplifyRank", amplifyRank, "ChaosWarrior: pain amplify " + amplifyRank + " + resonance " + resonanceRank + " ranks exceed the max draws " + ChaosRules.MaxDraws);
            return ChaosRules.Dials.PainStacksPerHit + ChaosRules.Dials.PainAmplifyExtra * amplifyRank + ChaosRules.Dials.PainResonanceExtra * resonanceRank + (neuroSting ? ChaosRules.Dials.PainNeuroStingExtra : 0);
        }

        // 식의 최대값(파생): 증폭·공명 순위의 합이 MaxDraws 이하이므로 더 큰 쪽 계수 × MaxDraws + 신경독침.
        public static int MaxAddPerHit
        {
            get { return ChaosRules.Dials.PainStacksPerHit + Math.Max(ChaosRules.Dials.PainAmplifyExtra, ChaosRules.Dials.PainResonanceExtra) * ChaosRules.MaxDraws + ChaosRules.Dials.PainNeuroStingExtra; }
        }

        // oldTotal ∉ 0..문턱 → 던짐 · add ∉ 1..MaxAddPerHit → 던짐. 면역 → {min(합, 문턱), F, F} · 합 ≥ 문턱 → {0, T, T}(한 번에 문턱을 건너뛰어도 1회) · 그 밖 {합, F, F}.
        public static PainStep Next(int oldTotal, int add, bool mindImmune)
        {
            if (oldTotal < 0 || oldTotal > Threshold) throw new ArgumentOutOfRangeException("oldTotal", oldTotal, "ChaosWarrior: pain total must be 0.." + Threshold);
            if (add < 1 || add > MaxAddPerHit) throw new ArgumentOutOfRangeException("add", add, "ChaosWarrior: pain add must be 1.." + MaxAddPerHit);
            int sum = oldTotal + add;
            if (mindImmune) return new PainStep(Math.Min(sum, Threshold), false, false);
            if (sum >= Threshold) return new PainStep(0, true, true);
            return new PainStep(sum, false, false);
        }

        // 계수 버프의 Ranks = 혼란 문턱(총 겹 0..문턱을 한 버프에 담는다 — 면역 대상이 문턱에서 멈춘 값까지).
        public static int CounterRanks { get { return Threshold; } }

        // 총 겹 → 벌점 버프 순위 = min(총, 상한). 총 ∉ 0..문턱 → 던짐. 0 = 벌점 버프 없음.
        public static int PenaltyRankFor(int total)
        {
            if (total < 0 || total > Threshold) throw new ArgumentOutOfRangeException("total", total, "ChaosWarrior: pain total must be 0.." + Threshold);
            return Math.Min(total, Cap);
        }

        // 붙어 있는 계수 버프의 순위 → 총 겹. 순위 ∉ 1..문턱 → 던짐(계수 버프는 있으면 1 이상이다 — 0 겹이면 지운다).
        public static int CounterTotal(int counterRank)
        {
            if (counterRank < 1 || counterRank > Threshold) throw new InvalidOperationException("ChaosWarrior: pain counter rank " + counterRank + " outside 1.." + Threshold);
            return counterRank;
        }

        // 계수 이전 판의 세이브(계수 없이 벌점 버프 + 개발판의 숨김 초과 버프)에서 총 겹을 되살린다 — PainCore 가 계수를 새로 만들어 옮길 때만 쓴다(옛 짝 검사 그대로).
        // penaltyRank ∉ 1..상한 → 던짐 · overflowRank(있으면) ∉ 1..PainLegacyOverflowRanks → 던짐 · 초과가 있는데 벌점이 상한이 아님 → 던짐(pain pair mismatch).
        public static int LegacyTotal(int penaltyRank, int? overflowRank)
        {
            if (penaltyRank < 1 || penaltyRank > Cap) throw new InvalidOperationException("ChaosWarrior: pain penalty rank " + penaltyRank + " outside 1.." + Cap);
            if (overflowRank == null) return penaltyRank;
            int legacyCap = ChaosRules.Dials.PainLegacyOverflowRanks;
            if (overflowRank.Value < 1 || overflowRank.Value > legacyCap) throw new InvalidOperationException("ChaosWarrior: legacy pain overflow rank " + overflowRank.Value + " outside 1.." + legacyCap);
            if (penaltyRank != Cap) throw new InvalidOperationException("ChaosWarrior: pain pair mismatch — legacy overflow rank " + overflowRank.Value + " with penalty rank " + penaltyRank + " (an overflow needs the penalty at its cap " + Cap + ")");
            return penaltyRank + overflowRank.Value;
        }

        public static bool AoeApplies(int level)
        {
            ChaosRules.RequireLevel(level);
            return level >= ChaosRules.Levels.PainAoeLevel;
        }
    }

    // 정신 효과 면역 판정 두 경로(부록 A-d): 버프 면역(RuleCanApplyBuff 거부) 또는 주문 면역(UnitPartSpellResistance.IsImmune). 둘 중 하나라도면 면역.
    public static class MindImmunity
    {
        public static bool Decide(bool probeCanApply, bool spellImmune) { return !probeCanApply || spellImmune; }
    }

    // 시전자별 고통 인스턴스 찾기(Stacking Stack → 시전자마다 따로, 부록 A-d). 0개 → null, 1개 → 그것, 같은 시전자 2개 이상 → 던짐(엔진 상태가 계약과 어긋남, C1).
    public static class PainLedger
    {
        public static T Find<T>(IEnumerable<T> instances, Func<T, object> casterOf, object caster) where T : class
        {
            if (instances == null) throw new ArgumentNullException("instances");
            if (casterOf == null) throw new ArgumentNullException("casterOf");
            if (caster == null) throw new ArgumentNullException("caster");
            var mine = instances.Where(i => i != null && Equals(casterOf(i), caster)).ToList();
            if (mine.Count > 1) throw new InvalidOperationException("ChaosWarrior: " + mine.Count + " pain instances from the same caster on one target (expected at most 1)");
            return mine.Count == 1 ? mine[0] : null;
        }
    }
}
