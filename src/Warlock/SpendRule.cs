using System;

namespace Warlock
{
     
    public struct SpendObs
    {
        public bool HasAbilityContext; // Context.SourceAbilityContext?.Ability != null
        public bool AbilityIsReserved; // 그 실행이 UnitPartWarlockAuto.Reserved 와 같은 참조
        public bool AbilityIsObsolete; // 그 실행이 UnitPartWarlockAuto.Obsolete 에 있음(시간 초과로 폐기된 자동 실행)
        public bool PartExists; // 시전자에게 UnitPartWarlockAuto 가 있음
    }

    public enum SpendKind { MatchingReservation, ObsoleteReservation, KnownManual, MissingContext }

    // 차감 액션(T5 ContextActionWarlockSpendHeat)이 적용할 것 — 순서: 랭크 쓰기 → (ConsumeReservation 이면 Reserved = null) → (DropObsolete 이면 Obsolete 에서 뺌) →
    // (OpenApproval 이면 ApprovedOpen = 이 실행, ApprovedAt = 지금) → LastDecision = (이 실행 문맥, Approved) → Info 가 있으면 INFO 1줄.
    public sealed class SpendPlan
    {
        public readonly SpendKind Kind;
        public readonly int NewRank;
        public readonly bool ConsumeReservation, DropObsolete, OpenApproval, Approved;
        public readonly string Info;
        public SpendPlan(SpendKind kind, int newRank, bool consume, bool drop, bool open, bool approved, string info)
        {
            Kind = kind; NewRank = newRank; ConsumeReservation = consume; DropObsolete = drop; OpenApproval = open; Approved = approved; Info = info;
        }
        public override string ToString() { return Kind + " rank=" + NewRank + " consume=" + ConsumeReservation + " drop=" + DropObsolete + " open=" + OpenApproval + " approved=" + Approved + " info=" + (Info ?? "-"); }
    }

    // 소환 확인 결과(SpendRule.AfterSpawn): Failed 면 NewRank 를 쓰고 승인 기록을 닫고 Error 를 1줄 남긴다.
    public sealed class SpawnCheck
    {
        public readonly bool Failed;
        public readonly int NewRank;
        public readonly string Error;
        public SpawnCheck(bool failed, int newRank, string error) { Failed = failed; NewRank = newRank; Error = error; }
        public override string ToString() { return (Failed ? "Failed" : "Ok") + " rank=" + NewRank + (Error == null ? "" : " error"); }
    }

    // 소환 확인 관측(리뷰 T6 I-1). RankNow = 지금 과열 랭크(WarlockHeat.Rank), Kind·Tier = 이 실행의 SpendDecision 기록, DecisionBound = 그 기록이 이 실행 문맥·능력 것.
    public sealed class ConfirmObs
    {
        public bool DecisionBound, ApprovalOpenIsThis, ApprovalOpenIsOther;
        public SpendKind Kind;
        public int Tier, RankNow;
        public string Unit;
    }

    // 소환 확인 계획: Failed 면 NewRank 를 쓰고, CloseApproval 이면 승인 기록을 닫고, ErrorIsError 면 Error 를 ERROR 로 1줄. 소환됨 = 전부 false·Error null.
    public sealed class ConfirmPlan
    {
        public readonly bool Failed, CloseApproval, ErrorIsError;
        public readonly int NewRank;
        public readonly string Error;
        public ConfirmPlan(bool failed, int newRank, bool closeApproval, bool errorIsError, string error) { Failed = failed; NewRank = newRank; CloseApproval = closeApproval; ErrorIsError = errorIsError; Error = error; }
        public override string ToString() { return (Failed ? "Failed" : "Ok") + " rank=" + NewRank + " close=" + CloseApproval + " error=" + (Error == null ? "-" : (ErrorIsError ? "ERROR" : "debug")); }
    }

     
    public static class SpendRule
    {
        // 분류 → 적용 계획(계획 T5 「ContextActionWarlockSpendHeat」 문단). MissingContext = 판정 불가 → 던진다(C1, R2-F04). 승인 = SpendRule.Approved(분류).
        public static SpendPlan Plan(SpendObs o, int rank, int tier, string unit)
        {
            if (string.IsNullOrEmpty(unit)) throw new ArgumentException("Warlock: SpendRule.Plan needs a unit key");
            var kind = Classify(o);
            if (kind == SpendKind.MissingContext) throw new InvalidOperationException("Warlock: " + unit + " heat spend (tier " + tier + ") has no ability context or no auto part (MissingContext)");
            int newRank = HeatRule.Spend(rank, tier, kind);
            bool approved = Approved(kind);
            switch (kind)
            {
                case SpendKind.MatchingReservation: return new SpendPlan(kind, newRank, true, false, true, approved, "Warlock: " + unit + " auto-summon reservation consumed (tier " + tier + ")");
                case SpendKind.KnownManual: return new SpendPlan(kind, newRank, false, false, true, approved, null);
                case SpendKind.ObsoleteReservation: return new SpendPlan(kind, newRank, false, true, false, approved, "Warlock: " + unit + " obsolete auto-summon execution ignored (tier " + tier + ", no deduction, no summon)");
                default: throw new ArgumentOutOfRangeException("o", kind, "Warlock: unknown spend kind");
            }
        }

        public static SpendKind Classify(SpendObs o)
        {
            // 판정 불가: 능력 컨텍스트 또는 상태 부품이 없다 → 게임 쪽은 throw(C1).
            if (!o.HasAbilityContext || !o.PartExists) return SpendKind.MissingContext;
            if (o.AbilityIsReserved && o.AbilityIsObsolete) throw new InvalidOperationException("Warlock: an execution is both the live reservation and obsolete (state corrupted)");
            if (o.AbilityIsReserved) return SpendKind.MatchingReservation;
            if (o.AbilityIsObsolete) return SpendKind.ObsoleteReservation;
            return SpendKind.KnownManual;
        }

        // Conditional{ConditionWarlockApproved} 게이트를 통과하는 분류(비우기·소환 실행). MissingContext 는 여기까지 오면 안 된다(그 전에 throw).
        // 소환 사건이 승인 기록을 닫는가(T5 WarlockSummonComponent): 열린 승인이 있고 그것이 이 소환을 낸 실행과 같은 참조일 때만. 아니면 「승인 기록 없는 소환」 ERROR.
        public static bool ClosesApproval(object approvedOpen, object executed)
        {
            if (executed == null) throw new ArgumentNullException("executed");
            return approvedOpen != null && ReferenceEquals(approvedOpen, executed);
        }

        // 게이트(ConditionWarlockApproved)의 실패 계약(리뷰 T5 I-2): 판정이 던지면 승인 아님(false) + 실패를 정확히 1번 보고. 던지지 않으면 판정 그대로.
        // 「던지면 통과」 = 차감 없는 소환이므로 여기서 한 번만 정하고 게임 쪽은 이것만 부른다(시험 `exec: gate guard`).
        public static bool GuardedApproval(Func<bool> decide, Action<Exception> onFailure)
        {
            if (decide == null) throw new ArgumentNullException("decide");
            if (onFailure == null) throw new ArgumentNullException("onFailure");
            try { return decide(); }
            catch (Exception ex) { onFailure(ex); return false; }
        }

        // 소환 확인(리뷰 T5 m-4): 승인된 실행이 게이트 안에서 소환까지 갔는가. 소환 사건(WarlockSummonComponent)이 승인 기록을 닫으므로,
        // 게이트 IfTrue 의 마지막 액션 시점에 승인 기록이 여전히 이 실행이면 소환이 실패한 것이다 → 쓴 과열 복원(상한 9) + 승인 기록 닫기 + ERROR 1줄.
        // spent = 이 실행이 낸 과열(수동 = 단계, 예약 = 9 — 예약이 9 를 0 으로 바꿨다). 열린 승인이 다른 실행이면 판정 불가 → 던진다.
        public static SpawnCheck AfterSpawn(SpendKind kind, int rankNow, int tier, bool approvalOpenIsThis, bool approvalOpenIsOther, string unit)
        {
            if (string.IsNullOrEmpty(unit)) throw new ArgumentException("Warlock: SpendRule.AfterSpawn needs a unit key");
            if (tier < 1 || tier > WarlockRules.Dials.SummonTiers) throw new ArgumentOutOfRangeException("tier", tier, "Warlock: summon tier must be 1..9");
            if (rankNow < 0 || rankNow > WarlockRules.Dials.HeatMax) throw new ArgumentOutOfRangeException("rankNow", rankNow, "Warlock: heat rank must be 0..9");
            if (approvalOpenIsThis && approvalOpenIsOther) throw new ArgumentException("Warlock: approval cannot be this and another execution at once");
            if (kind != SpendKind.KnownManual && kind != SpendKind.MatchingReservation) throw new InvalidOperationException("Warlock: AfterSpawn for a non-approved spend " + kind + " (the gate must not run the summon)");
            if (!approvalOpenIsThis)
            {
                if (approvalOpenIsOther) throw new InvalidOperationException("Warlock: " + unit + " approval record belongs to another execution after the tier " + tier + " summon (state corrupted)");
                return new SpawnCheck(false, rankNow, null); // 정당한 무동작: 소환 사건이 승인을 닫았다 = 소환됨
            }
            int spent = kind == SpendKind.KnownManual ? tier : WarlockRules.Dials.HeatMax;
            int restored = Math.Min(WarlockRules.Dials.HeatMax, rankNow + spent);
            return new SpawnCheck(true, restored, "Warlock: " + unit + " heat summon tier " + tier + " was approved but nothing was summoned — heat restored to " + restored + ", approval closed");
        }

        // 소환 확인 액션의 관측 → 적용 전부(리뷰 T6 I-1): 게임 쪽 ContextActionWarlockConfirmSummon 은 관측을 평평하게 넣고 이 계획만 그대로 적용한다
        // (Failed 면 Write(NewRank) → CloseApproval 이면 ApprovedOpen = null → Emit(ErrorIsError, Error)). 결정 기록이 이 실행 것이 아니면 판정 불가 → 던진다.
        public static ConfirmPlan Confirm(ConfirmObs o)
        {
            if (o == null) throw new ArgumentNullException("o");
            if (!o.DecisionBound) throw new InvalidOperationException("Warlock: " + (o.Unit ?? "?") + " confirm summon without this execution's spend decision");
            var c = AfterSpawn(o.Kind, o.RankNow, o.Tier, o.ApprovalOpenIsThis, o.ApprovalOpenIsOther, o.Unit);
            return new ConfirmPlan(c.Failed, c.NewRank, c.Failed, c.Failed, c.Error);
        }

        public static bool Approved(SpendKind kind)
        {
            switch (kind)
            {
                case SpendKind.MatchingReservation: return true;
                case SpendKind.KnownManual: return true;
                case SpendKind.ObsoleteReservation: return false;
                case SpendKind.MissingContext: throw new InvalidOperationException("Warlock: Approved(MissingContext) — the spend action must throw before the gate");
                default: throw new ArgumentOutOfRangeException("kind", kind, "Warlock: unknown spend kind");
            }
        }
    }
}
