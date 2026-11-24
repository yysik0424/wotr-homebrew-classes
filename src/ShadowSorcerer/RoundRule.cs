namespace ShadowSorcerer
{
     
     
    //   같은 회차 · 둘 다 턴제: 라운드 번호 비교. 그 밖(실시간 · 모드 전환 · 전투 밖): 시각 창(RoundSeconds).
    //   전투 밖은 실시간 규칙(판정 J6 (a)). 결측 → Unknown. Commit 은 메아리를 올릴 때만(호출자).
    public struct RoundStamp
    {
        public bool Has;
        public long Epoch;
        public bool TurnBased;
        public int Round;
        public double Time;
        public override string ToString() { return Has ? "epoch " + Epoch + (TurnBased ? " TB round " + Round : " RT") + " t=" + Time : "none"; }
    }

    public sealed class RoundFacts
    {
        public long? CombatEpoch;       // RoundRule.OutOfCombatEpoch = 전투 밖 표지(결측이 아니다)
        public bool? TurnBased;
        public int? RoundNumber;        // 턴제일 때만 필요
        public double? GameTime;
        public RoundStamp Last;
    }

    public static class RoundRule
    {
         
        public static readonly long OutOfCombatEpoch = -1;





        public static GateResult Core(RoundFacts r, double roundSeconds)
        {
            if (r == null) return GateResult.Unknown("no round facts");
            if (!r.CombatEpoch.HasValue || !r.TurnBased.HasValue || !r.GameTime.HasValue) return GateResult.Unknown("round facts unread");
            if (r.TurnBased.Value && !r.RoundNumber.HasValue) return GateResult.Unknown("turn-based round number unread");
            var last = r.Last;
            if (!last.Has || last.Epoch != r.CombatEpoch.Value) return new GateResult(Decision.Echo, null, 0);
            if (r.TurnBased.Value && last.TurnBased && r.CombatEpoch.Value != OutOfCombatEpoch)
                return r.RoundNumber.Value != last.Round ? new GateResult(Decision.Echo, null, 0) : GateResult.Skip(GateReasons.RoundUsed);
            return r.GameTime.Value - last.Time < roundSeconds ? GateResult.Skip(GateReasons.RoundUsed) : new GateResult(Decision.Echo, null, 0);
        }

        public static RoundStamp Commit(RoundFacts r)
        {
            if (r == null || !r.CombatEpoch.HasValue || !r.TurnBased.HasValue || !r.GameTime.HasValue) throw new System.InvalidOperationException("ShadowSorcerer: RoundRule.Commit with unread facts");
            return new RoundStamp { Has = true, Epoch = r.CombatEpoch.Value, TurnBased = r.TurnBased.Value, Round = r.RoundNumber ?? -1, Time = r.GameTime.Value };
        }
    }
}
