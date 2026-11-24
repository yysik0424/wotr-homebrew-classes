using System;

namespace BulwarkMage
{
    // 판정: 버프 없음(보조손에 타워실드도 없음 — 정당한 무동작) · 맞음 · 랭크를 맞춤 · 판정 불가(C1 — 「맞음」으로 새지 않는다, 호출자가 Report).
    public enum TrainingRankVerdict { NoBuff, InSync, SetRank, Unknown }

    public sealed class TrainingRankPlan
    {
        public readonly TrainingRankVerdict Verdict;
        public readonly int Target;    // InSync·SetRank 의 목표 랭크(그 밖 0)
        public readonly string Problem; // Unknown 일 때만
        public TrainingRankPlan(TrainingRankVerdict verdict, int target, string problem) { Verdict = verdict; Target = target; Problem = problem; }
    }

    // [순수] 타워실드 훈련 버프 랭크 맞춤(2026-11-18, 3차 게임 안 점검 S15 L10 ACP −5 / 기대 −4). 게임 형식 없음(modinfo testSources) — 시험 `exec: training rank rule`.
    // 목표 랭크 = 엔진이 버프를 「새로 얻을 때」 만드는 랭크(dnlib 2026-11-18):
    //   문 HasShieldFeatureUnlock.AddFact @0026 이 버프를 1랭크로 붙이고 → FactsChangeTrigger.HandleUnitGainFact @007E~008F 가 반복 행동을 돌린다
    //   → ContextActionRepeatedActions.RunAction @000C~0027 은 Value.Calculate(문맥) 번(i < count, 음수면 0번) ApplyBuff
    //   → BuffCollection.PrepareFactForAttach Stacking.Rank @025F = 있는 버프에 AddRank(1) → Buff.AddRank @0000~0019 는 Ranks 를 넘으면 무동작.
    //   그래서 목표 = min(Ranks, 1 + max(0, 반복 횟수)). 반복 횟수는 호출자가 우리 복제 반복 행동의 Value 를 엔진으로 계산해 넘긴다(본편 BonusValue·step −1 → 특성 랭크 − 1).
    // 레벨업에서 이 값이 깨지는 까닭: Feature.AddRank @0029~0042 의 Deactivate → Activate 가 문으로 버프를 지우고 1랭크로 다시 붙이는데, 그때 특성은 TurnOn 전이라
    //   (EntityFact.Activate: 컴포넌트 Activate 뒤 TurnOn @0190, 구독 = EntityFact.TurnOn @009A) FactsChangeTrigger 가 획득 사건을 받지 못한다.
    public static class TrainingRankRule
    {
        public static int GainRank(int repeats, int buffRanks)
        {
            if (buffRanks < 1) throw new ArgumentOutOfRangeException("buffRanks", buffRanks, "BulwarkMage: training buff Ranks must be >= 1");
            return Math.Min(buffRanks, 1 + Math.Max(0, repeats));
        }

        // repeats = 반복 횟수(null = 읽지 못함), buffRank = 지금 버프 랭크(null = 버프 없음), offHand = 보조손 방패가 타워실드인지(버프가 없을 때만 본다).
        public static TrainingRankPlan Decide(int? repeats, int buffRanks, int? buffRank, ShieldTowerKind offHand)
        {
            if (buffRanks < 1) throw new ArgumentOutOfRangeException("buffRanks", buffRanks, "BulwarkMage: training buff Ranks must be >= 1");
            if (!buffRank.HasValue)
            {
                if (offHand == ShieldTowerKind.NotTower) return new TrainingRankPlan(TrainingRankVerdict.NoBuff, 0, null);
                if (offHand == ShieldTowerKind.Tower) return new TrainingRankPlan(TrainingRankVerdict.Unknown, 0, "a tower shield is in the off hand but the training buff is absent");
                return new TrainingRankPlan(TrainingRankVerdict.Unknown, 0, "the training buff is absent and the off-hand shield type is unreadable");
            }
            if (!repeats.HasValue) return new TrainingRankPlan(TrainingRankVerdict.Unknown, 0, "the training buff is present but the repeat count is unreadable");
            if (buffRank.Value < 1) return new TrainingRankPlan(TrainingRankVerdict.Unknown, 0, "the training buff has rank " + buffRank.Value);
            int target = GainRank(repeats.Value, buffRanks);
            return new TrainingRankPlan(buffRank.Value == target ? TrainingRankVerdict.InSync : TrainingRankVerdict.SetRank, target, null);
        }
    }
}
