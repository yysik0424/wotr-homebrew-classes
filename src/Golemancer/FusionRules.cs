using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Golemancer
{
     
     
    //   FusionInvariant      — Active 불변식 4판정 Ok / StopPending / FormLost / Violation(§3-4, C-01·R2-I01·R2-I03).
    //   ControlRemoval       — 제어 버프 제거 경로 분류 Swap / End / Ignore(§3-3, R2·R2-I03).
    //   FormRemoval          — 형태 버프 외부 제거 분류(§4-4, R3).
    //   AbsorberGate         — 흡수 계산 관문(§1-8, R2-M01).
     
    //   FusionDamageCore     — 확정 원장(Fix round 2: 훅에서 상한·우리 몫 귀속·재사용 상한 걷기·보상 대상 선택·영수증 보존, 가짜 세계로 실행 시험).
    //   FusionTauntAfterEnd  — 종료 뒤 도발 기대(§4-2, R4).
    //   GolemFusionSequence  — 시작·제거 원시 동작 순서(원장 ⑤·⑥, 가짜 세계 시험 `exec: fusion wiring order`).
    //   FusionPetIdentity    — 합체 골렘 신원(원장 ②, `exec: pet identity`).

    // P 축(§2-3): 파트 없음 / 코어 상태.
    public enum PartAxis { None, Idle, Active, Ending, AwaitRestore }

    // T 축(§2-3): off(!IsOn·!IsStarted) · requested(IsOn·!IsStarted) · started(IsOn·IsStarted) · offStarted(!IsOn·IsStarted).
    public enum ToggleAxis { Off, Requested, Started, OffStarted }

    // 분류 결과. Active·CoreBusy 는 파트가 있는 경우(표 밖 — 불변식·코어가 맡는다).
    public enum FusionPhase { NoFusion, PendingStart, RejectedStart, Starting, StartRejectedNoBuff, Orphan, Active, CoreBusy }

    // Fusion.Check 의 단계(§1-2·§1-4 ④): 평상(토글 제한) / 시작 사건 안 재검증.
    public enum CheckPhase { Steady, Starting }

    // 관측 1회(§2-4). 어댑터가 모든 필드를 채운다 — 판정 함수는 누락(음수 개수·null 키)을 던진다(C1).
    public sealed class FusionObs
    {
        public bool IsPreview;
        public bool HasFeature = true;          // 합체 특성 사실이 있는가(Fix round 1 I03 — 합체 중 특성·토글 소실은 정상 빈 상태가 아니다)
        public bool HasPart;
        public FusionState State;               // HasPart 일 때만 의미
        public bool SwapPending, SelfFormOp, FormLostPending, StopRequestedIsControl;
        public int ToggleCount = -1;
        public bool IsOn, IsStarted, AppliedIsControl, AppliedIsNull;
        public bool PendingActivateCommand;
        public int ControlCount = -1;
        public bool ControlActive, ControlSuppressed;
        public int FormCount = -1;
        public bool FormActive, FormSuppressed;
        public string FormKey;                  // FormCount == 1 일 때만(아니면 null)
        public bool ActivePolymorphPresent, ActivePolymorphIsOurs, BodyPolymorphed;
        public int StatCount = -1;
        public string StatKey;                  // StatCount == 1 일 때만
        public bool StatHasPetSet;
        public bool? GolemHidden;               // 파트 Active/Ending 일 때만 관측(그 밖 null)
        public TauntSource? Taunt;              // 파트 Active 일 때만 관측(그 밖 null)
        public string ExpectedKey;              // 파트 Active 일 때만(AppliedKey → BuffKey, I5)

        public PartAxis P
        {
            get
            {
                if (!HasPart) return PartAxis.None;
                switch (State)
                {
                    case FusionState.Idle: return PartAxis.Idle;
                    case FusionState.Active: return PartAxis.Active;
                    case FusionState.Ending: return PartAxis.Ending;
                    case FusionState.AwaitRestore: return PartAxis.AwaitRestore;
                }
                throw new ArgumentException("FusionObs: undefined fusion state " + (int)State);
            }
        }

        public ToggleAxis T { get { return IsOn ? (IsStarted ? ToggleAxis.Started : ToggleAxis.Requested) : (IsStarted ? ToggleAxis.OffStarted : ToggleAxis.Off); } }

        // 개수 필드가 채워졌는지(-1 = 관측 안 됨). 판정 함수 첫머리가 부른다(C2).
        public void RequireObserved()
        {
            if (ToggleCount < 0 || ControlCount < 0 || FormCount < 0 || StatCount < 0) throw new InvalidOperationException("FusionObs: counts not observed (toggle " + ToggleCount + ", control " + ControlCount + ", form " + FormCount + ", stat " + StatCount + ")");
            if (AppliedIsControl && AppliedIsNull) throw new InvalidOperationException("FusionObs: AppliedBuff is both null and the control buff");
        }

        public override string ToString()
        {
            return "{P=" + P + " T=" + T + " cmd=" + (PendingActivateCommand ? 1 : 0) + " c" + ControlCount + (AppliedIsControl ? "/ap" : AppliedIsNull ? "/null" : "/other") + (ControlActive ? "" : "/inactive") + (ControlSuppressed ? "/supp" : "")
                + " f" + FormCount + (FormActive ? "" : "/inactive") + (FormSuppressed ? "/supp" : "") + "(" + (FormKey ?? "-") + ") s" + StatCount + "(" + (StatKey ?? "-") + (StatHasPetSet ? ",pet" : "") + ")"
                + " poly=" + (ActivePolymorphPresent ? (ActivePolymorphIsOurs ? "ours" : "other") : "none") + (BodyPolymorphed ? "/body" : "")
                + " swap=" + SwapPending + " lost=" + FormLostPending + " stop=" + StopRequestedIsControl + " hidden=" + (GolemHidden.HasValue ? GolemHidden.Value.ToString() : "?") + " taunt=" + (Taunt.HasValue ? Taunt.Value.ToString() : "?") + " key=" + (ExpectedKey ?? "-") + "}";
        }
    }

    public sealed class PhaseResult
    {
        public readonly FusionPhase Phase;
        public readonly int Row;                // §2-3 표의 행 번호(1~11). 0 = 표 밖(Active·CoreBusy)
        public PhaseResult(FusionPhase phase, int row) { Phase = phase; Row = row; }
        public override string ToString() { return Phase + (Row > 0 ? " (row " + Row + ")" : ""); }
    }

    public static class StartPhase
    {
        // §2-3. 미리보기는 호출자가 먼저 거른다(§2-1) — 여기 오면 배선 오류 → throw.
        public static PhaseResult Classify(FusionObs o, bool inStartHandler)
        {
            if (o == null) throw new ArgumentNullException("o");
            if (o.IsPreview) throw new InvalidOperationException("StartPhase.Classify on a preview unit (the preview guard must come first)");
            o.RequireObserved();
            var p = o.P;
            if (p == PartAxis.Active) return new PhaseResult(FusionPhase.Active, 0);
            if (p == PartAxis.Ending || p == PartAxis.AwaitRestore) return new PhaseResult(FusionPhase.CoreBusy, 0);
            // P ∈ {없음, Idle}. IsOn 은 산출물이 아니다.
            // 행 12(Fix round 1 I03): 토글 사실 ≠ 1(특성·토글 소실·중복) — 산출물이 남았거나 토글이 겹치면 고아, 아무것도 없으면 정상 빈 상태.
            if (o.ToggleCount != 1 || !o.HasFeature)
            {
                bool any = o.ControlCount > 0 || o.FormCount > 0 || o.StatCount > 0 || o.IsStarted;
                return any || o.ToggleCount > 1 ? new PhaseResult(FusionPhase.Orphan, 12) : new PhaseResult(FusionPhase.NoFusion, 1);
            }
            if (o.FormCount > 0 || o.StatCount > 0) return new PhaseResult(FusionPhase.Orphan, 8);
            switch (o.T)
            {
                case ToggleAxis.OffStarted: return new PhaseResult(FusionPhase.Orphan, 10);
                case ToggleAxis.Off:
                    return o.ControlCount > 0 ? new PhaseResult(FusionPhase.Orphan, 9) : new PhaseResult(FusionPhase.NoFusion, 1);
                case ToggleAxis.Requested:
                    if (o.ControlCount > 0) return new PhaseResult(FusionPhase.Orphan, 9);
                    return o.PendingActivateCommand ? new PhaseResult(FusionPhase.PendingStart, 2) : new PhaseResult(FusionPhase.RejectedStart, 3);
                case ToggleAxis.Started:
                    if (o.ControlCount == 1 && o.AppliedIsControl) return inStartHandler ? new PhaseResult(FusionPhase.Starting, 4) : new PhaseResult(FusionPhase.Orphan, 6);
                    if (o.ControlCount == 0 && o.AppliedIsNull) return inStartHandler ? new PhaseResult(FusionPhase.StartRejectedNoBuff, 5) : new PhaseResult(FusionPhase.Orphan, 7);
                    return new PhaseResult(FusionPhase.Orphan, 11);
            }
            throw new ArgumentException("StartPhase.Classify: undefined toggle axis " + o.T);
        }

         
        public static bool IsPendingActivate(bool isActivateCommand, bool abilityIsOurToggle, bool isFinished, bool isActed)
        {
            return isActivateCommand && abilityIsOurToggle && !isFinished;
        }

        // §6-1 canStart(파트 없음·Idle): 정상 빈 상태 → true, Starting → 시작 사건 안 재검증에서만 true, 고아·제어 없는 시작 → null(Unknown). 파트 있는 단계는 코어 관문이 답한다 → throw.
        public static bool? CanStart(FusionPhase phase, CheckPhase check)
        {
            switch (phase)
            {
                case FusionPhase.NoFusion:
                case FusionPhase.PendingStart:
                case FusionPhase.RejectedStart:
                    return true;
                case FusionPhase.Starting:
                    if (check != CheckPhase.Starting) throw new InvalidOperationException("StartPhase.CanStart: Starting outside the start handler");
                    return true;
                case FusionPhase.StartRejectedNoBuff:
                case FusionPhase.Orphan:
                    return null;
                case FusionPhase.Active:
                case FusionPhase.CoreBusy:
                    throw new InvalidOperationException("StartPhase.CanStart: phase " + phase + " has a part — ask the core gate");
            }
            throw new ArgumentException("StartPhase.CanStart: undefined phase " + (int)phase);
        }
    }

    public enum InvariantVerdict { Ok, StopPending, FormLost, Violation }

    public static class FusionInvariant
    {
        // §2-4 formOk. 기대 키가 없으면 판정 불가 → throw(I5).
        public static bool FormOk(FusionObs o)
        {
            if (o == null) throw new ArgumentNullException("o");
            if (string.IsNullOrEmpty(o.ExpectedKey)) throw new InvalidOperationException("FusionInvariant.FormOk: expected key not observed");
            return o.FormCount == 1 && o.FormActive && !o.FormSuppressed && o.ActivePolymorphPresent && o.ActivePolymorphIsOurs && o.BodyPolymorphed && o.FormKey == o.ExpectedKey;
        }

        // §3-4 (P = Active). EndTime 은 입력이 아니다(R2-I03).
        public static InvariantVerdict Check(FusionObs o)
        {
            if (o == null) throw new ArgumentNullException("o");
            o.RequireObserved();
            if (o.P != PartAxis.Active) throw new InvalidOperationException("FusionInvariant.Check: part is " + o.P + ", expected Active");
            if (o.ToggleCount != 1 || !o.HasFeature) return InvariantVerdict.Violation; // 합체 중 특성·토글 소실(I03) — 종료한다
            if (!o.GolemHidden.HasValue || !o.Taunt.HasValue) throw new InvalidOperationException("FusionInvariant.Check: golem hidden / taunt source not observed");
            bool controlOk = !o.SwapPending && o.IsOn && o.IsStarted && o.AppliedIsControl && o.ControlCount == 1 && o.ControlActive && !o.ControlSuppressed;
            bool stopPending = !o.SwapPending && o.StopRequestedIsControl && !o.IsOn && !o.IsStarted && o.AppliedIsNull && o.ControlCount == 1;
            if (!controlOk && !stopPending) return InvariantVerdict.Violation;
            if (o.FormLostPending || !FormOk(o)) return InvariantVerdict.FormLost;
            bool restOk = o.StatCount == 1 && o.StatKey == o.ExpectedKey && !o.StatHasPetSet && o.GolemHidden.Value && o.Taunt.Value == TauntSource.Master;
            if (!restOk) return InvariantVerdict.Violation;
            return controlOk ? InvariantVerdict.Ok : InvariantVerdict.StopPending;
        }
    }

    public enum ControlRemovalKind { Swap, End, Ignore }

    public static class ControlRemoval
    {
        // §3-3: 레벨업 교체(ReapplyBuff — Stop 없음) 만 Swap. 그 밖의 Active 제거는 End(정상 끄기·외부 제거·사망). 다른 상태 = Ignore(우리 종료 중·Idle).
        public static ControlRemovalKind Classify(FusionState state, bool isStarted, bool appliedIsNull, bool appliedIsThis, bool stopRecorded)
        {
            if (!Enum.IsDefined(typeof(FusionState), state)) throw new ArgumentException("ControlRemoval.Classify: undefined state " + (int)state);
            if (appliedIsNull && appliedIsThis) throw new ArgumentException("ControlRemoval.Classify: AppliedBuff cannot be both null and this buff");
            if (state != FusionState.Active) return ControlRemovalKind.Ignore;
            if (isStarted && appliedIsNull && !stopRecorded) return ControlRemovalKind.Swap;
            return ControlRemovalKind.End;
        }
    }

    public enum FormRemovalKind { ShamanDead, DurationOver, Ignore }

    public static class FormRemoval
    {
        // §4-4: 우리 교체·종료(selfFormOp) 가 아닌 Active 중 제거 = 외부 → 종료(주인 사망이면 ShamanDead). 코어 사건 종류는 늘리지 않는다.
        public static FormRemovalKind Classify(FusionState state, bool selfFormOp, bool ownerDead)
        {
            if (!Enum.IsDefined(typeof(FusionState), state)) throw new ArgumentException("FormRemoval.Classify: undefined state " + (int)state);
            if (state != FusionState.Active || selfFormOp) return FormRemovalKind.Ignore;
            return ownerDead ? FormRemovalKind.ShamanDead : FormRemovalKind.DurationOver;
        }
    }

    public enum AbsorberVerdict { Skip, ErrorObservationFailed, ErrorNotDesignated, ErrorOrphan, SkipWarnFormLost, PassToGate }

    public sealed class AbsorberDecision
    {
        public readonly AbsorberVerdict Verdict;
        public readonly bool DuplicateError;    // 흡수기 사실 2개 이상, 이 사실이 첫 사실 → ERROR 를 남기고 판정은 그대로(분배 1회 유지)
        public AbsorberDecision(AbsorberVerdict v, bool dup) { Verdict = v; DuplicateError = dup; }
        public override string ToString() { return Verdict + (DuplicateError ? "+ErrorDuplicate" : ""); }
    }

    public static class AbsorberGate
    {
        // §1-8 표. phase·state 는 미리보기·관측 실패일 때만 null 이 될 수 있다.
        public static AbsorberDecision DecideCalculate(bool isPreview, int featureFactCount, bool isFirstFact, FusionPhase? phase, FusionState? state, bool formOk, bool formLostPending, bool observationFailed)
        {
            if (isPreview) return new AbsorberDecision(AbsorberVerdict.Skip, false);
            if (observationFailed) return new AbsorberDecision(AbsorberVerdict.ErrorObservationFailed, false);
            if (featureFactCount < 1) throw new ArgumentOutOfRangeException("featureFactCount", "AbsorberGate: absorber runs without any fusion feature fact (" + featureFactCount + ")");
            if (featureFactCount > 1 && !isFirstFact) return new AbsorberDecision(AbsorberVerdict.ErrorNotDesignated, false);
            if (featureFactCount == 1 && !isFirstFact) throw new ArgumentException("AbsorberGate: the only fusion feature fact is not the first fact");
            bool dup = featureFactCount > 1;
            if (!phase.HasValue || !state.HasValue) throw new ArgumentNullException(phase.HasValue ? "state" : "phase", "AbsorberGate: phase/state missing without an observation failure");
            switch (phase.Value)
            {
                case FusionPhase.NoFusion:
                case FusionPhase.PendingStart:
                case FusionPhase.RejectedStart:
                    return new AbsorberDecision(AbsorberVerdict.Skip, dup);
                case FusionPhase.Orphan:
                    return new AbsorberDecision(AbsorberVerdict.ErrorOrphan, dup);
                case FusionPhase.Active:
                    if (state.Value != FusionState.Active) throw new ArgumentException("AbsorberGate: phase Active with state " + state.Value);
                    return new AbsorberDecision(formLostPending || !formOk ? AbsorberVerdict.SkipWarnFormLost : AbsorberVerdict.PassToGate, dup);
                case FusionPhase.CoreBusy:
                    if (state.Value != FusionState.Ending && state.Value != FusionState.AwaitRestore) throw new ArgumentException("AbsorberGate: phase CoreBusy with state " + state.Value);
                    return new AbsorberDecision(AbsorberVerdict.PassToGate, dup); // WSS 관문이 WARN 과 함께 거부(기존 동작)
                case FusionPhase.Starting:
                case FusionPhase.StartRejectedNoBuff:
                    throw new InvalidOperationException("AbsorberGate: phase " + phase.Value + " exists only inside the start handler");
            }
            throw new ArgumentException("AbsorberGate: undefined phase " + (int)phase.Value);
        }
    }

    public static class AbsorberGateObs
    {
        // 관측 1개 → 관문 입력(Fix round 1 — opus X12: 사건 밖 분류 inStartHandler=false 를 순수 쪽에서 고정). o == null = 관측 실패.
        public static AbsorberDecision Decide(bool isPreview, int featureFactCount, bool isFirstFact, FusionObs o)
        {
            if (isPreview) return AbsorberGate.DecideCalculate(true, featureFactCount, isFirstFact, null, null, false, false, false);
            if (o == null) return AbsorberGate.DecideCalculate(false, featureFactCount, isFirstFact, null, null, false, false, true);
            var ph = StartPhase.Classify(o, false);
            bool formOk = ph.Phase == FusionPhase.Active && FusionInvariant.FormOk(o);
            return AbsorberGate.DecideCalculate(false, featureFactCount, isFirstFact, ph.Phase, o.State, formOk, o.FormLostPending, false);
        }
    }

    public enum AbsorbCommitKind { NoRecord, NoDeduction, Unverifiable, Commit, Rebase, CompensateMaster }

     
    // 단위: 모든 양은 **난이도 배율 전**(RuleDealDamage.OnTrigger @01C8 이전 FinalValue 합)이다. 골렘 HP 는 파티 피해 배율로 늘거나 줄지 않는다(PhantomBlade F2 선례).
    //   그래서 배율 ≠ 1 이면 「본체 + 골렘」은 합체하지 않았을 때의 피해와 다르다(의도된 설계, README 알려진 한계).
    // 우리 몫(Fix round 2, NI1): 덩어리마다 우리 상한 c 를 걸 **직전**의 원천 상한 m0(남이 먼저 건 상한, 없으면 null)를 기록한다.
    //   지금 값 v = max(0, 감산 전 − 감소), 지금 원천 상한 M(엔진 SetMaximum 은 Min 만 남긴다 BaseDamage.SetMaximum @0014-002D)으로
    //   E = min(v, m0, (M < c ? M : ∞)), 우리 몫 = max(0, E − c). 앞선 남의 상한은 m0 로 빠지고, 뒤에 온 더 낮은 상한은 M < c 로 보인다.
    //   M 이 없거나 c 보다 크면(우리 상한이 원천에서 사라짐 — SetMaximum 으로는 생길 수 없다) 귀속 불가 → throw → Unverifiable(ERROR, HP 쓰기 없음).
    //   우리 상한은 전역 DidTrigger 훅(RulebookEventBus.OnEventDidTrigger @0078-007E → RulebookEventHooksManager.OnAfterEventDidTrigger)에서 건다
    //   — 그 규칙의 모든 구독자(전역 @001C·대상 @004D·개시자 @0073) 뒤라, 남의 처리기 상한은 모두 m0 에 들어간다.
     
    //   (경계 x == c 포함 — c == 0 이면 남이 0 으로 만든 피해가 골렘에 청구된다, 검토 T5-r3)
    //   → 우리 몫이 그만큼 크게 잡혀 골렘이 실제 몫보다 더 맞는다(검출 불가, 다른 모드의 흡수가 같은 유닛에 우리 뒤에 걸릴 때만). 시험 `known limit` 이 값을 고정한다.
    public static class AbsorbCommit
    {
        public static int OurDeduction(int valueNow, int? max0, int cap, int? maxNow)
        {
            if (valueNow < 0 || cap < 0) throw new ArgumentOutOfRangeException("AbsorbCommit.OurDeduction: negative value / cap");
            if (!maxNow.HasValue) throw new InvalidOperationException("AbsorbCommit.OurDeduction: our cap " + cap + " is gone from the damage source (cannot attribute)");
            if (maxNow.Value > cap) throw new InvalidOperationException("AbsorbCommit.OurDeduction: source maximum " + maxNow.Value + " above our cap " + cap + " (SetMaximum only lowers — the maximum was written outside our knowledge, cannot attribute)");
            long e = valueNow;
            if (max0.HasValue) e = Math.Min(e, max0.Value);
            if (maxNow.Value < cap) e = Math.Min(e, maxNow.Value); // 뒤에 온 더 낮은 상한(관측 가능) — 그 몫은 남의 것
            return (int)Math.Max(0, e - cap);
        }

        // 상태(Active/Ending/Idle)는 입력이 아니다(R2-I02). deducted = 확정 시점 관측 공제(null = 관측·귀속 불가), planned = 계산 때 정한 흡수량.
        //  관측 불가 → Unverifiable(추측 HP 쓰기 금지, ERROR) · 공제 0 → NoDeduction · 파트 교체·골렘 불가 → CompensateMaster(ERROR)
        //  · 공제 == 계획 ∧ 골렘 HP 그대로 → Commit(코어 Deal) · 그 밖 → Rebase(지금 HP 기준으로 다시 나눔, 정상 경로).
        public static AbsorbCommitKind Decide(bool hasRecord, int? deducted, int planned, bool partSame, bool petResolvable, bool petHpUnchanged)
        {
            if (!hasRecord) return AbsorbCommitKind.NoRecord;
            if (!deducted.HasValue) return AbsorbCommitKind.Unverifiable;
            if (deducted.Value < 0 || planned < 0) throw new ArgumentOutOfRangeException("AbsorbCommit.Decide: negative amount");
            if (deducted.Value == 0) return AbsorbCommitKind.NoDeduction;
            if (!partSame || !petResolvable) return AbsorbCommitKind.CompensateMaster;
            if (deducted.Value == planned && petHpUnchanged) return AbsorbCommitKind.Commit;
            return AbsorbCommitKind.Rebase;
        }

        // Rebase: 지금 골렘 HP 로 다시 나눈다 — 골렘 몫 = min(공제, 지금 HP), 나머지는 본체에 되돌린다.
        public static int RebaseShare(int deducted, int petHpNow)
        {
            if (deducted < 0 || petHpNow < 0) throw new ArgumentOutOfRangeException("AbsorbCommit.RebaseShare: negative amount");
            return Math.Min(deducted, petHpNow);
        }

        // 본체에 되돌릴 양(난이도 전) = 공제 − 골렘이 실제로 잃은 HP(관측). 모르면 null(추측해 쓰지 않는다).
        public static int? Compensation(int deducted, int? golemReceived)
        {
            if (deducted < 0) throw new ArgumentOutOfRangeException("deducted");
            if (!golemReceived.HasValue) return null;
            if (golemReceived.Value < 0 || golemReceived.Value > deducted) throw new InvalidOperationException("AbsorbCommit: golem received " + golemReceived.Value + " of a " + deducted + " deduction — cannot conserve by compensation");
            return deducted - golemReceived.Value;
        }

        // 보상 쓰기의 양 = floor(pre × min(1, 배율)) — 합체 안 했을 때보다 본체가 더 다치지 않는다(엔진은 Direct 를 배율에서 뺀다 @0102-010E,
        //   1 이하 배율을 무시하면 더 다친다). 배율 > 1 이면 난이도 전 양 그대로(덜 다친다, INFO 에 적는다).
        public static int CompensationWrite(int preDifficulty, float difficultyModifier)
        {
            if (preDifficulty < 0) throw new ArgumentOutOfRangeException("preDifficulty");
            if (float.IsNaN(difficultyModifier) || difficultyModifier < 0f) throw new ArgumentOutOfRangeException("difficultyModifier");
            return (int)Math.Floor(preDifficulty * Math.Min(1.0, difficultyModifier));
        }

        // 보상 영수증 검증(nm1·R2-I02): 관측 임시 HP 소비 + 관측 HP 감소 + MinHPAfterDamage 보류 == 정책 쓰기 양(CompensationWrite).
        public static bool ReceiptOk(CompensationReceipt r, int pre, float difficultyModifier)
        {
            if (r == null || !r.Observed) return false;
            if (r.TempConsumed < 0 || r.HpLost < 0 || r.MinHpKept < 0) return false;
            int want = CompensationWrite(pre, difficultyModifier);
            return r.Planned == want && r.TempConsumed + r.HpLost + r.MinHpKept == want;
        }

        // 보존(난이도 전, 서로 따로 관측한 세 양): 우리 상한이 깎은 양 == 골렘 HP 가 잃은 양 + 본체로 되돌린 양,
        //   그리고 되돌림이 있으면 그 쓰기가 영수증으로 확인됐다. 영수증 없는 되돌림(쓰기 0 포함)은 보존이 아니다.
        public static bool Conserved(int deductedObserved, int golemLostObserved, int backPre, bool backReceiptOk)
        {
            if (deductedObserved < 0 || golemLostObserved < 0 || backPre < 0) throw new ArgumentOutOfRangeException("AbsorbCommit.Conserved: negative amount");
            if (backPre > 0 && !backReceiptOk) return false;
            return golemLostObserved + backPre == deductedObserved;
        }

        // 정산이 끝난 우리 상한을 원천에서 걷는 결정(재사용 대비). 지금 == c → 걸기 직전 값 m0 로 되돌린다.
        //   c 보다 낮으면 → 뒤에 온 남의 더 낮은 상한(합체 안 했어도 남는다) → 그대로 둔다. null → 이미 없다. 더 높으면 → SetMaximum 으로 생길 수 없다 → 이상.
        //   (알려진 한계와 같은 뿌리: c 보다 높은 뒤 상한은 Min 에 묻혀 사라졌으므로 되살리지 못한다.)
        public static RestoreKind RestoreFor(int? maxNow, int cap)
        {
            if (!maxNow.HasValue) return RestoreKind.Gone;
            if (maxNow.Value == cap) return RestoreKind.Restore;
            if (maxNow.Value < cap) return RestoreKind.ForeignLower;
            return RestoreKind.Anomaly;
        }
    }

    public enum RestoreKind { Restore, ForeignLower, Gone, Anomaly }

    // 보상 쓰기 영수증(관측): 정책 쓰기 양, 실제 임시 HP 소비(전후 값), 실제 HP 감소(Damage 전후), MinHPAfterDamage 로 보류한 양.
    public sealed class CompensationReceipt
    {
        public bool Observed;
        public int Planned, TempConsumed, HpLost, MinHpKept;
        public override string ToString() { return Observed ? "planned " + Planned + " = temp " + TempConsumed + " + hp " + HpLost + " + minhp " + MinHpKept : "unobserved"; }
    }

    // 보상 대상 유닛의 원시 동작(게임 = 한 줄씩). 순서·양·관측은 순수 CompensationRun 이 정한다.
    public interface ICompensationPrims
    {
        int TempHp();               // 임시 HP 지금 값
        int ConsumeTemp(int amount); // 임시 HP 가 먼저 받는다 → 남은 양(엔진 @0254 와 같은 TemporaryHitPoints.HandleDamage)
        int HpLeft();
        int Damage();
        void SetDamage(int value);
        void LifeTick();            // 생명 판정(UnitLifeController.ForceTickOnUnit)
    }

    public static class CompensationRun
    {
        // 본체 보상: 정책 양 → 임시 HP → 원래 규칙의 MinHPAfterDamage(HPLeft − MinHP 로 자름, nm2 알려진 한계: 엔진은 Result 를 먼저 자른다) → Damage 쓰기 → 생명 판정 → 전후 관측.
        public static CompensationReceipt Apply(ICompensationPrims p, int pre, float modifier, int? minHp)
        {
            if (p == null) throw new ArgumentNullException("p");
            int want = AbsorbCommit.CompensationWrite(pre, modifier);
            int t0 = p.TempHp(), d0 = p.Damage();
            int afterTemp = want > 0 ? p.ConsumeTemp(want) : 0;
            if (afterTemp < 0 || afterTemp > want) throw new InvalidOperationException("CompensationRun: temp HP returned " + afterTemp + " of " + want);
            int write = afterTemp;
            if (minHp.HasValue) write = Math.Max(0, Math.Min(write, p.HpLeft() - minHp.Value));
            if (write > 0) { p.SetDamage(d0 + write); p.LifeTick(); }
            return new CompensationReceipt { Observed = true, Planned = want, TempConsumed = t0 - p.TempHp(), HpLost = p.Damage() - d0, MinHpKept = afterTemp - write };
        }
    }

    // 덩어리 관측(게임 → 순수): 원천(참조), 지금 값 v = max(0, 감산 전 − 감소), 지금 FinalValue, 지금 원천 상한.
    public sealed class ChunkNow
    {
        public object Source;
        public int Value, Final;
        public int? Max;
        // FinalValue = max(0, min(Max ?? ∞, v)) (DamageValue.get_FinalValue @0002-0036). 어긋나면 관측 오류.
        public void Validate()
        {
            if (Source == null) throw new InvalidOperationException("damage chunk without a source");
            if (Value < 0 || Final < 0) throw new InvalidOperationException("negative damage chunk");
            int want = Max.HasValue ? Math.Max(0, Math.Min(Max.Value, Value)) : Value;
            if (Final != want) throw new InvalidOperationException("damage chunk final " + Final + " != min(max " + (Max.HasValue ? Max.Value.ToString() : "-") + ", value " + Value + ")");
        }
    }

    // 우리 상한 기록: 원천, 걸기 직전 원천 상한, 우리 상한, 정산 여부(정산 뒤에도 원천에 남는다 → 걷을 대상).
    public sealed class ChunkMark
    {
        public object Source;
        public int? Max0;
        public int Cap;
        public bool Settled;
        public object Rule;
    }

    public sealed class DamageRecord
    {
        public object Master;           // 기록을 연 유닛(= 그 규칙의 대상, 흡수기는 대상 처리기)
        public object Part;             // 합체 파트(참조)
        public string PetId;
        public AbsorbCalc Calc;         // 훅이 상한을 걸 때 정한다(null = 상한 없음)
        public bool Capped;             // 훅이 이 규칙을 처리했다(재계산이면 다시 걸지 않는다)
        public readonly List<ChunkMark> Marks = new List<ChunkMark>();
        public override string ToString() { return (Capped ? "capped" : "open") + " marks " + Marks.Count + (Calc == null ? "" : " planned " + Calc.Absorbed); }
    }

    // 확정 원장의 게임 경계(관측·적용만). 순수 FusionDamageCore 가 순서·판정·대상 선택을 정한다. 게임 구현은 규칙 객체를 들지 않는다(보상 대상 = 기록의 주인만 닿는다).
    public interface IDamageCoreWorld
    {
        void EnsureSubscribed();
        AbsorbCalc AbsorbCalc(object part, int incoming);
        void SetMaximum(object source, int cap);
        int? ReadMaximum(object source);
        void RestoreMaximum(object source, int? value);
        object CurrentPartOf(object master);          // 지금 그 주인의 합체 파트(없으면 null)
        string ResolvePetId(object part);             // 그 파트의 골렘 ID(못 찾으면 throw)
        int PetHp(object part);
        int PetDamage(object part);
        void CommitCore(DamageRecord r);
        void CommitRebased(DamageRecord r, int petHpNow, int share);
        ICompensationPrims PrimsOf(object unit);
        void Info(object unit, string m); void Warn(object unit, string m); void Error(object unit, string m);
    }

    public sealed class SettleResult
    {
        public AbsorbCommitKind Kind;
        public int? Deducted, GolemReceived, Compensated;
        public bool Conserved;
        public object CompensatedUnit;
        public override string ToString() { return Kind + " deducted " + (Deducted.HasValue ? Deducted.Value.ToString() : "?") + " golem " + (GolemReceived.HasValue ? GolemReceived.Value.ToString() : "?") + " back " + (Compensated.HasValue ? Compensated.Value.ToString() : "?") + (Conserved ? "" : " NOT CONSERVED"); }
    }

    // 확정 원장 핵심(순수, 참조 동일성). 수명: 흡수기 관문(OpenPending) → 계산 훅(OnCalculateHook: 남은 예전 상한을 걷고, 이 규칙에 상한을 한 번 건다)
    //   → 같은 RuleDealDamage 전역 DidTrigger(OnDidTrigger: 정산) → 같은 규칙의 훅(OnDealHook: 정산한 상한을 걷는다). Tick 경계를 넘긴 기록은 Sweep(ERROR + 정산 + 걷기).
    // 재사용(같은 BaseDamage 를 새 규칙이 다시 씀 — 엔진은 같은 규칙 객체의 두 번째 Trigger 를 거부한다 Rulebook.TriggerEvent IL_0000-0022):
    //   결정 = 정산이 끝난 우리 상한은 원천에서 걷는다(걸기 전 값으로). 새 계산은 우리 상한이 없는 값에서 시작한다. DamageValue.FinalValue 는 원천 상한을 매번 다시 읽는다(@0002-0036).
    public sealed class FusionDamageCore<TRule> where TRule : class
    {
        readonly PendingLedger<TRule, DamageRecord> open = new PendingLedger<TRule, DamageRecord>();
        // DamageRecord owns the original mark while the rule is open. A missing result chunk may
        // return in another rule after settlement. Only its cap and weak handles belong in this index.
        sealed class Cap
        {
            readonly WeakReference original, rule;
            readonly int? previous;
            readonly int limit;
            internal bool Settled;

            internal Cap(ChunkMark mark)
            {
                original = new WeakReference(mark);
                rule = new WeakReference(mark.Rule);
                previous = mark.Max0;
                limit = mark.Cap;
            }

            internal ChunkMark Read(object source)
            {
                return original.Target as ChunkMark ?? new ChunkMark { Source = source, Rule = rule.Target, Max0 = previous, Cap = limit, Settled = Settled };
            }
        }
        readonly WotrClassMod.Common.WeakKeyTable<object, Cap> marks = new WotrClassMod.Common.WeakKeyTable<object, Cap>();
        public int OpenCount { get { return open.Count; } }
        public bool IsOpen(TRule r) { return open.Contains(r); }
        public ChunkMark MarkOf(object source) { Cap cap; return source != null && marks.TryGetValue(source, out cap) ? cap.Read(source) : null; }

        void SettleMarks(DamageRecord rec)
        {
            foreach (var m in rec.Marks)
            {
                m.Settled = true;
                Cap cap;
                if (marks.TryGetValue(m.Source, out cap) && ReferenceEquals(cap.Read(m.Source), m)) cap.Settled = true;
            }
        }

        // 흡수기 관문 통과(같은 규칙의 재계산이면 열린 기록 유지 → false).
        public bool OpenPending(TRule rule, object master, object part, string petId, IDamageCoreWorld w)
        {
            if (rule == null || master == null || part == null || w == null) throw new ArgumentNullException("OpenPending inputs");
            if (open.Contains(rule)) return false;
            w.EnsureSubscribed();
            open.Open(rule, new DamageRecord { Master = master, Part = part, PetId = petId });
            return true;
        }

        // 계산 훅(그 RuleCalculateDamage 의 모든 구독자 뒤). target = 규칙 대상, read = 지금 덩어리(원천 상한을 다시 읽는다).
        public void OnCalculateHook(TRule rule, object target, Func<IList<ChunkNow>> read, IDamageCoreWorld w)
        {
            if (rule == null || target == null || read == null || w == null) throw new ArgumentNullException("OnCalculateHook inputs");
            if (!Relevant(open.Peek(rule) != null, read)) return; // 정당한 무동작: 기록도 우리 상한도 없는 계산(모든 피해 규칙이 지나간다)
            var chunks = Read(read);
            int restored = LiftSettled(chunks, target, w);
            if (restored > 0) { w.Warn(target, "damage source reused with an old fusion cap still on it — cap lifted before this damage (" + restored + " chunk(s))"); chunks = Read(read); }
            var rec = open.Peek(rule);
            if (rec == null) return; // 정당한 무동작: 흡수기 관문을 지나지 않은 계산
            if (rec.Capped) return; // 같은 규칙의 재계산: 상한은 원천에 남아 있다(다시 걸지 않는다) — 정산이 지금 값으로 공제를 다시 관측한다
            rec.Capped = true;
            if (!ReferenceEquals(target, rec.Master)) { w.Error(rec.Master, "absorber record's rule hit another target — no cap"); return; }
            foreach (var c in chunks)
            {
                var m = MarkOf(c.Source);
                if (m != null && !m.Settled && !ReferenceEquals(m.Rule, rule)) { w.Error(rec.Master, "damage source shared with another rule's unsettled fusion cap — not absorbing this damage"); return; }
            }
            int incoming = 0;
            foreach (var c in chunks) incoming += c.Final;
            var calc = w.AbsorbCalc(rec.Part, incoming);
            if (calc == null) throw new InvalidOperationException("FusionDamageCore: AbsorbCalc returned null");
            if (calc.Incoming != incoming) throw new InvalidOperationException("FusionDamageCore: AbsorbCalc incoming " + calc.Incoming + " != " + incoming);
            rec.Calc = calc;
            if (!calc.Allowed || calc.Absorbed <= 0) return; // 관문 거부(WARN 은 코어) 또는 흡수 0 — 상한 없음
            int absorb = calc.Absorbed;
            if (absorb > incoming) throw new InvalidOperationException("FusionDamageCore: absorb " + absorb + " > incoming " + incoming);
            var share = new int[chunks.Count];
            int given = 0;
            for (int i = 0; i < chunks.Count; i++) { share[i] = (int)((long)absorb * chunks[i].Final / incoming); given += share[i]; }
            for (int i = 0; given < absorb && i < chunks.Count; i++) if (share[i] < chunks[i].Final) { share[i]++; given++; }
            if (given != absorb) throw new InvalidOperationException("FusionDamageCore: could not distribute " + absorb + " over " + incoming);
            for (int i = 0; i < chunks.Count; i++)
            {
                if (share[i] <= 0) continue;
                var mk = new ChunkMark { Source = chunks[i].Source, Max0 = chunks[i].Max, Cap = chunks[i].Final - share[i], Rule = rule };
                rec.Marks.Add(mk); // 걸기 전에 기록(걸다 실패해도 정산이 원천을 다시 읽는다)
                marks.Remove(mk.Source);
                marks.Add(mk.Source, new Cap(mk));
                w.SetMaximum(mk.Source, mk.Cap);
            }
            int after = 0;
            foreach (var c in Read(read)) after += c.Final;
            if (after != incoming - absorb) w.Error(rec.Master, "cap readback: " + incoming + " - " + absorb + " expected " + (incoming - absorb) + ", got " + after + " — settled from the observed deduction");
        }

        // 같은 RuleDealDamage 의 전역 DidTrigger. ruleTarget = 엔진이 피해를 쓴 유닛, modifier/minHp = 그 규칙의 값(보상 쓰기 정책).
        public SettleResult OnDidTrigger(TRule rule, bool isFake, object ruleTarget, Func<IList<ChunkNow>> read, float modifier, int? minHp, IDamageCoreWorld w)
        {
            if (rule == null || read == null || w == null) throw new ArgumentNullException("OnDidTrigger inputs");
            var rec = open.Take(rule); // 정확히 한 번(먼저 꺼낸다)
            if (rec == null) return null; // 정당한 무동작: 흡수기 관문을 지나지 않은 규칙
            if (isFake)
            {
                // 흡수기는 미리보기에서 기록을 열지 않는다 → 여기 온 것은 배선 위반. HP 를 건드리지 않고, 건 상한은 정산된 것으로 걷는다.
                SettleMarks(rec);
                w.Error(rec.Master, "absorber record on a preview (IsFake) rule — no HP is written");
                return new SettleResult { Kind = AbsorbCommitKind.Unverifiable };
            }
            return Settle(rec, ruleTarget, read, modifier, minHp, w);
        }

        // 같은 RuleDealDamage 의 훅(모든 구독자 뒤): 정산한 우리 상한을 원천에서 걷는다.
        public int OnDealHook(object target, Func<IList<ChunkNow>> read, IDamageCoreWorld w)
        {
            if (read == null || w == null) throw new ArgumentNullException("OnDealHook inputs");
            if (!Relevant(false, read)) return 0; // 정당한 무동작: 우리 상한이 없는 피해
            return LiftSettled(Read(read), target, w);
        }

        // Tick 경계: 열린 기록 = 수명 위반. ERROR 뒤 지금 관측으로 정산하고 상한을 걷는다.
        public List<SettleResult> Sweep(Func<TRule, object> targetOf, Func<TRule, Func<IList<ChunkNow>>> readOf, Func<TRule, float> modifierOf, Func<TRule, int?> minHpOf, IDamageCoreWorld w)
        {
            var res = new List<SettleResult>();
            foreach (var kv in open.TakeAll())
            {
                var rule = kv.Key; var rec = kv.Value;
                w.Error(rec.Master, "absorber record outlived its rule (no DidTrigger before the next tick) — settling from the observed damage now");
                object target = null; float mod = float.NaN; int? minHp = null;
                try { target = targetOf(rule); mod = modifierOf(rule); minHp = minHpOf(rule); } catch (Exception ex) { w.Error(rec.Master, "sweep: rule not readable (" + ex.GetType().Name + ")"); }
                var read = readOf(rule);
                res.Add(Settle(rec, target, read, mod, minHp, w));
                try { LiftSettled(Read(read), rec.Master, w); } catch (Exception ex) { w.Error(rec.Master, "sweep: cap lift failed (" + ex.GetType().Name + ": " + ex.Message + ")"); }
            }
            return res;
        }

        // 관련 = 이 규칙에 기록이 있거나, 덩어리 원천 중 하나에 우리 상한 기록이 있다. 기록이 있는데 목록이 없으면 관측 불가(throw).
        bool Relevant(bool hasRecord, Func<IList<ChunkNow>> read)
        {
            var raw = read();
            if (raw == null) { if (hasRecord) throw new InvalidOperationException("damage list not readable"); return false; }
            if (hasRecord) return true;
            foreach (var c in raw) if (c != null && MarkOf(c.Source) != null) return true;
            return false;
        }

        static IList<ChunkNow> Read(Func<IList<ChunkNow>> read)
        {
            var l = read();
            if (l == null) throw new InvalidOperationException("damage list not readable");
            foreach (var c in l) { if (c == null) throw new InvalidOperationException("null damage chunk"); c.Validate(); }
            return l;
        }

        int LiftSettled(IList<ChunkNow> chunks, object unit, IDamageCoreWorld w)
        {
            int n = 0;
            foreach (var c in chunks)
            {
                var m = MarkOf(c.Source);
                if (m == null || !m.Settled) continue;
                marks.Remove(c.Source);
                var k = AbsorbCommit.RestoreFor(c.Max, m.Cap);
                switch (k)
                {
                    case RestoreKind.Restore:
                        var to = m.Max0;
                        w.RestoreMaximum(c.Source, to);
                        var back = w.ReadMaximum(c.Source);
                        if (back != to) w.Error(unit, "fusion cap lift readback " + (back.HasValue ? back.Value.ToString() : "null") + " != " + (to.HasValue ? to.Value.ToString() : "null"));
                        n++;
                        break;
                    case RestoreKind.ForeignLower: break; // 정당한 무동작: 남의 더 낮은 뒤 상한(합체 안 했어도 남는다)
                    case RestoreKind.Gone: w.Warn(unit, "fusion cap already gone from a settled damage source"); break;
                    case RestoreKind.Anomaly: w.Error(unit, "damage source maximum " + c.Max.Value + " above our settled cap " + m.Cap + " — not lifted"); break;
                    default: throw new InvalidOperationException("undefined restore kind " + k);
                }
            }
            return n;
        }

        SettleResult Settle(DamageRecord rec, object ruleTarget, Func<IList<ChunkNow>> read, float modifier, int? minHp, IDamageCoreWorld w)
        {
            var r = new SettleResult();
            var unit = rec.Master;
            SettleMarks(rec);
            if (!ReferenceEquals(ruleTarget, rec.Master)) { w.Error(unit, "absorb settle: the rule's target is not the recorded master — no HP is written (unverifiable)"); r.Kind = AbsorbCommitKind.Unverifiable; return r; }
            if (!rec.Capped) w.Error(unit, "absorb settle: the cap hook never ran for this rule (hook unregistered?) — nothing was absorbed");
            if (rec.Calc == null) { r.Kind = AbsorbCommitKind.NoDeduction; r.Deducted = 0; r.GolemReceived = 0; r.Compensated = 0; r.Conserved = rec.Marks.Count == 0; return r; }
            int? deducted = null;
            try { deducted = Deduction(rec.Marks, Read(read)); }
            catch (Exception ex) { w.Error(unit, "absorb settle: our deduction is not attributable (" + ex.GetType().Name + ": " + ex.Message + ") — no HP is written (unverifiable)"); }
            r.Deducted = deducted;
            if (deducted.HasValue && (float.IsNaN(modifier) || modifier < 0f)) { w.Error(unit, "absorb settle: difficulty modifier unreadable — no HP is written (unverifiable)"); deducted = null; }

            int planned = rec.Calc.Absorbed;
            object partNow = null; bool resolvable = false; int hpNow = -1;
            try { partNow = w.CurrentPartOf(rec.Master); } catch (Exception ex) { w.Error(unit, "absorb settle: fusion part not readable (" + ex.GetType().Name + ")"); }
            bool partSame = partNow != null && ReferenceEquals(partNow, rec.Part);
            if (partSame && deducted.HasValue)
            {
                try { resolvable = w.ResolvePetId(rec.Part) == rec.PetId; } catch (Exception ex) { w.Error(unit, "absorb settle: golem " + rec.PetId + " not resolvable (" + ex.GetType().Name + ": " + ex.Message + ")"); }
                if (resolvable) hpNow = w.PetHp(rec.Part);
            }
            r.Kind = AbsorbCommit.Decide(true, deducted, planned, partSame, resolvable, resolvable && hpNow == rec.Calc.PetHp);
            switch (r.Kind)
            {
                case AbsorbCommitKind.Unverifiable:
                    r.Conserved = false;
                    return r;
                case AbsorbCommitKind.NoDeduction:
                    r.GolemReceived = 0; r.Compensated = 0; r.Conserved = true;
                    if (planned > 0) w.Warn(unit, "absorb settle: planned " + planned + " but our cap removed nothing (a lower cap or reduction came after ours) — golem untouched");
                    return r;
                case AbsorbCommitKind.CompensateMaster:
                    w.Error(unit, "absorb settle: golem commit impossible (part same " + partSame + ", golem resolvable " + resolvable + ") — returning " + deducted.Value + " to the master");
                    r.GolemReceived = 0;
                    return Back(w, r, rec.Master, deducted.Value, deducted.Value, modifier, minHp);
                case AbsorbCommitKind.Commit:
                case AbsorbCommitKind.Rebase:
                {
                    int before = w.PetDamage(rec.Part);
                    int share = r.Kind == AbsorbCommitKind.Commit ? planned : AbsorbCommit.RebaseShare(deducted.Value, hpNow);
                    if (r.Kind == AbsorbCommitKind.Rebase) w.Info(unit, "absorb settle: rebased (golem HP " + rec.Calc.PetHp + " -> " + hpNow + ", deducted " + deducted.Value + " vs planned " + planned + ") — golem takes " + share);
                    Exception commitError = null;
                    try { if (r.Kind == AbsorbCommitKind.Commit) w.CommitCore(rec); else w.CommitRebased(rec, hpNow, share); }
                    catch (Exception ex) { commitError = ex; }
                    int? received = null;
                    try { received = w.PetDamage(rec.Part) - before; } catch (Exception ex) { w.Error(unit, "absorb settle: golem damage not observable after commit (" + ex.GetType().Name + ") — no HP is written (unverifiable)"); }
                    r.GolemReceived = received;
                    if (commitError != null) w.Error(unit, "absorb settle: golem commit threw " + commitError.GetType().Name + ": " + commitError.Message + " (golem received " + (received.HasValue ? received.Value.ToString() : "unknown") + ")");
                    if (!received.HasValue) { r.Conserved = false; return r; }
                    if (received.Value < 0 || received.Value > deducted.Value) { w.Error(unit, "absorb settle: golem lost " + received.Value + " HP of a " + deducted.Value + " deduction — no HP is written to the master"); r.Conserved = false; return r; }
                    var back = AbsorbCommit.Compensation(deducted.Value, received);
                    if (back.Value > 0 && commitError == null && r.Kind == AbsorbCommitKind.Commit) w.Error(unit, "absorb settle: core commit landed " + received.Value + " of " + deducted.Value + " — returning the rest to the master");
                    return Back(w, r, rec.Master, deducted.Value, back.Value, modifier, minHp);
                }
            }
            throw new InvalidOperationException("FusionDamageCore: undefined verdict " + r.Kind);
        }

        static int Deduction(List<ChunkMark> ms, IList<ChunkNow> now)
        {
            int d = 0;
            foreach (var m in ms)
            {
                ChunkNow hit = null;
                foreach (var c in now) if (ReferenceEquals(c.Source, m.Source)) { if (hit != null) throw new InvalidOperationException("a capped damage source appears twice in the rule's damage list"); hit = c; }
                if (hit == null) throw new InvalidOperationException("a capped damage chunk is missing from the rule's damage list");
                d += AbsorbCommit.OurDeduction(hit.Value, m.Max0, m.Cap, hit.Max);
            }
            return d;
        }

        // 보상 대상 = 기록의 주인(규칙 개시자 아님 — 순수 결정, opus N3g).
        static SettleResult Back(IDamageCoreWorld w, SettleResult r, object master, int deducted, int back, float modifier, int? minHp)
        {
            r.Compensated = back;
            bool ok = true;
            if (back > 0)
            {
                CompensationReceipt rc = null;
                try { rc = CompensationRun.Apply(w.PrimsOf(master), back, modifier, minHp); r.CompensatedUnit = master; }
                catch (Exception ex) { w.Error(master, "absorb settle: compensation threw " + ex.GetType().Name + ": " + ex.Message); }
                ok = AbsorbCommit.ReceiptOk(rc, back, modifier);
                if (!ok) w.Error(master, "absorb settle: compensation write not confirmed (" + (rc == null ? "no receipt" : rc.ToString()) + ", wanted " + AbsorbCommit.CompensationWrite(back, modifier) + ")");
                else w.Info(master, "compensated master " + back + " (pre-difficulty) x" + modifier + ": " + rc);
            }
            r.Conserved = AbsorbCommit.Conserved(deducted, r.GolemReceived.Value, back, ok);
            if (!r.Conserved) w.Error(master, "absorb settle: conservation broken (pre-difficulty): deducted " + deducted + ", golem " + r.GolemReceived.Value + ", back " + back + (ok ? "" : " (unconfirmed write)"));
            return r;
        }
    }

    // 키별 발생 횟수(nm3) — 매번 센다.
    public sealed class RepeatCounter
    {
        readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        public int Total { get; private set; }
        public int Hit(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            int n; counts.TryGetValue(key, out n); n++; counts[key] = n; Total++;
            return n;
        }
        public int Count(string key) { int n; return key != null && counts.TryGetValue(key, out n) ? n : 0; }
    }

    // 반복 ERROR 가시성(nm3): 같은 내용은 1·2·4·8… 번째에 로그(개수 포함), 매번 센다.
    public static class RepeatLog
    {
        public static bool ShouldLog(int count) { if (count < 1) throw new ArgumentOutOfRangeException("count"); return (count & (count - 1)) == 0; }
        public static string Text(string what, int count) { return count == 1 ? what : what + " (repeated " + count + " times)"; }
    }

    // 확정 원장 구독 확인 시점(2026-11-15 성능 수리, 알려진 문제 F3 — 예전에는 Fusion.Tick 이 매 프레임 리플렉션으로 확인했다).
    //   확인한다: ① 생성 직후(모드 적재 뒤 첫 게임 틱 — 예전 첫 구독과 같은 시점) ② NotInGame(로딩·지역 없음) 뒤 첫 틱 — 저장 불러오기·지역 이동은 LoadingProcess 를 지난다
    //   ③ 지역 상태 참조가 마지막 확인 때와 다를 때(AreaDidLoad 뒤 첫 틱 — 로딩 틱을 못 봐도 잡는다) ④ 지역 상태가 null(정체를 모름 = 매 틱 확인, C1) ⑤ 확인이 실패한 뒤(Verified 를 적지 않으니 계속 Due).
    //   기록을 열 때(FusionDamageCore.OpenPending)는 이 일정과 무관하게 매번 확인한다(기존 동작).
    //   사이 구간의 해지 경로 없음 = 시험 `perf: engine removes a global rulebook subscription …`·`perf: only the ledger's own …` (엔진 해지는 EventBus.Unsubscribe(object) 뿐, 처리기 참조는 원장 비공개).
    public sealed class LedgerVerifySchedule
    {
        bool due = true;      // ① 모드 적재
        object verifiedArea;  // 마지막으로 확인을 끝낸 지역 상태(참조 동일성)

        public bool Due(object areaState) { return due || areaState == null || !ReferenceEquals(areaState, verifiedArea); }
        public void NotInGame() { due = true; verifiedArea = null; } // 옛 지역 상태를 붙잡지 않는다
        public void Verified(object areaState) { due = false; verifiedArea = areaState; }
    }

    // 보류 기록 원장(순수, 참조 동일성 키). 수명 = 그 규칙의 Calculate(기록) ~ 같은 규칙의 DidTrigger(소비). 그 사이에 Tick 경계를 넘긴 기록은 위반(SweepAll → ERROR + 정산).
    public sealed class PendingLedger<TKey, TRec> where TKey : class where TRec : class
    {
        readonly List<KeyValuePair<TKey, TRec>> open = new List<KeyValuePair<TKey, TRec>>();
        public int Count { get { return open.Count; } }
        int Find(TKey k) { for (int i = 0; i < open.Count; i++) if (ReferenceEquals(open[i].Key, k)) return i; return -1; }
        public bool Contains(TKey k) { if (k == null) throw new ArgumentNullException("k"); return Find(k) >= 0; }
        public TRec Peek(TKey k) { if (k == null) throw new ArgumentNullException("k"); int i = Find(k); return i < 0 ? null : open[i].Value; }
        public void Open(TKey k, TRec r)
        {
            if (k == null || r == null) throw new ArgumentNullException(k == null ? "k" : "r");
            if (Find(k) >= 0) throw new InvalidOperationException("PendingLedger: a record for this rule is already open");
            open.Add(new KeyValuePair<TKey, TRec>(k, r));
        }
        // 정확히 한 번: 꺼낸 기록은 원장에서 빠진다.
        public TRec Take(TKey k)
        {
            if (k == null) throw new ArgumentNullException("k");
            int i = Find(k);
            if (i < 0) return null; // 정당한 무동작 신호: 이 규칙은 계산 단계를 밟지 않았다
            var r = open[i].Value;
            open.RemoveAt(i);
            return r;
        }
        public List<KeyValuePair<TKey, TRec>> TakeAll() { var all = open.ToList(); open.Clear(); return all; }
    }

    // 깊이 카운터(selfFormOp — 우리 형태 교체·종료 중). using 블록으로만 늘고 준다(예외에도 0 으로 돌아온다).
    public sealed class DepthCounter
    {
        int depth;
        public int Depth { get { return depth; } }
        public bool Active { get { return depth > 0; } }
        public IDisposable Enter() { depth++; return new Exit(this); }
        sealed class Exit : IDisposable
        {
            DepthCounter c;
            public Exit(DepthCounter c) { this.c = c; }
            public void Dispose() { if (c == null) return; if (c.depth <= 0) throw new InvalidOperationException("DepthCounter: exit without enter"); c.depth--; c = null; }
        }
    }

    // 파트의 런타임 기록(저장 안 함) — 처리기는 판정·기록을 여기 맡기고 결과만 따른다(Fix round 1: 게임 쪽 분기를 오프라인에서 실행 시험).
    public sealed class FusionRuntimeFlags
    {
        public bool SwapPending { get; private set; }
        public bool FormLostPending { get; private set; }
        public object StopRequested { get; private set; }
        public readonly DepthCounter SelfFormOp = new DepthCounter();

        // 제어 제거 분류 결과를 반영: Swap 이면 교체 대기. 반환 = 코어에 종료(DurationOver)를 넣어야 하는가.
        public bool OnControlRemoved(ControlRemovalKind k)
        {
            if (!Enum.IsDefined(typeof(ControlRemovalKind), k)) throw new ArgumentException("FusionRuntimeFlags: undefined removal kind " + (int)k);
            if (k == ControlRemovalKind.Swap) { SwapPending = true; return false; }
            return k == ControlRemovalKind.End;
        }

        // 우리 제어 버프가 붙음 → 교체 완료(교체 중일 때만 의미). 반환 = 교체를 끝냈는가.
        public bool OnControlAdded(bool isOurControlOnOwner)
        {
            if (!isOurControlOnOwner || !SwapPending) return false;
            SwapPending = false;
            return true;
        }

        // WillStop(R2-I03): Active 일 때만 기록. 반환 = 기록했는가(applied 가 null 이면 기록하지 않고 false — 호출자 ERROR).
        public bool OnWillStop(bool isOurToggleOnOwner, FusionState state, object applied)
        {
            if (!isOurToggleOnOwner || state != FusionState.Active) return false;
            StopRequested = applied;
            return applied != null;
        }

        // 변신 해제(R2-I01): 주인 + 우리 형태 + 우리 조작 아님 + Active 일 때만 기록.
        public bool OnPolymorphDeactivated(bool isOwner, bool isOurForm, FusionState state)
        {
            if (!isOwner || !isOurForm || SelfFormOp.Active || state != FusionState.Active) return false;
            FormLostPending = true;
            return true;
        }

        public bool StopRequestedIs(object onlyControl) { return StopRequested != null && onlyControl != null && ReferenceEquals(StopRequested, onlyControl); }
        public void ConsumeFormLost() { FormLostPending = false; }
        public void Clear() { SwapPending = false; FormLostPending = false; StopRequested = null; }
    }

    // 지금 상태(처리기용): 런타임이 있으면 관문 상태(실행 중 저장 필드는 이전 값), 없으면 저장값.
    public static class PartState
    {
        public static FusionState Current(FusionState? runtimeState, int savedState)
        {
            if (runtimeState.HasValue) return runtimeState.Value;
            if (!Enum.IsDefined(typeof(FusionState), (FusionState)savedState)) throw new ArgumentException("PartState: undefined saved state " + savedState);
            return (FusionState)savedState;
        }
    }

    // Tick 실행기(순수, §2-3·§3-4): 관측 → 분류 → 불변식 → 원시 동작. 게임 쪽 GameTickWorld 는 원시 동작만.
    public interface IFusionTickWorld
    {
        GolemFusionBlock CheckSteady();
        void TurnToggleOff();               // 토글이 있으면 끈다(IsOn false, IsStarted 면 Stop)
        void CleanupOrphan();               // 제어·형태 Remove, 수치 회수, 토글 끔 + 되읽기(실패 = throw)
        void ConsumeFormLost();
        void DeliverEnd();                  // 코어 DurationOver
        void CountStopPending();
        void Warn(string m); void Error(string m);
    }

    public enum TickOutcome { Idle, Pending, RejectedOff, OrphanCleaned, CoreBusy, Ok, StopPending, FormLostEnded, ViolationEnded }

    // The game's EntityFactsManager.Add/Remove maintain the canonical List, including buff,
    // ability and feature facts. Mono's list version also changes on same-count replacement.
    // Cache only completed negative scans; the UnitPart can change independently of this list.
    internal sealed class FusionAbsentFacts<T>
    {
        sealed class Stamp
        {
            public readonly WeakReference Facts = new WeakReference(null);
            public int Version;
        }
        readonly Func<List<T>, int> readVersion = MakeVersionReader();
        readonly List<Stamp> roster = new List<Stamp>();
        object area;

        static Func<List<T>, int> MakeVersionReader()
        {
            var field = typeof(List<T>).GetField("_version", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(int)) return null;
            try
            {
                var list = Expression.Parameter(typeof(List<T>), "list");
                return Expression.Lambda<Func<List<T>, int>>(Expression.Field(list, field), list).Compile();
            }
            catch (MemberAccessException) { return null; }
            catch (System.Security.SecurityException) { return null; }
            catch (NotSupportedException) { return null; }
        }

        public void SetRoster(object currentArea, int count)
        {
            if (!ReferenceEquals(area, currentArea))
            {
                roster.Clear();
                area = currentArea;
            }
            if (area == null || readVersion == null) return;
            if (count < roster.Count) roster.RemoveRange(count, roster.Count - count);
            while (roster.Count < count) roster.Add(new Stamp());
        }

        public bool KnownAbsent(int slot, List<T> facts, out int version)
        {
            version = 0;
            if (area == null || readVersion == null) return false;
            version = readVersion(facts);
            var entry = roster[slot];
            return entry.Version == version && ReferenceEquals(entry.Facts.Target, facts);
        }

        public void Remember(int slot, List<T> facts, int checkedVersion)
        {
            if (area == null || readVersion == null) return;
            var entry = roster[slot];
            entry.Facts.Target = facts;
            entry.Version = checkedVersion;
        }
    }

    public static class FusionTick
    {
         
        public static bool IsTarget(bool hasFeature, bool hasPart, int toggles, int controls, int forms, int stats)
        {
            if (toggles < 0 || controls < 0 || forms < 0 || stats < 0) throw new ArgumentOutOfRangeException("FusionTick.IsTarget: negative count");
            return hasFeature || hasPart || toggles > 0 || controls > 0 || forms > 0 || stats > 0;
        }

        // 매 프레임 셈(2026-11-15 perf — Fusion.IsFusionTickTarget 이 캐릭터마다 ToList 4~5 개를 만들던 것을 대체). 할당 없음(List<T>.Enumerator 는 구조체).
        // 값·예외 = 옛 source.Where(match).ToList().Count 와 같다: source null → ArgumentNullException, match null → ArgumentNullException,
        //   술어 예외는 그대로, 순회 중 목록 변경 → InvalidOperationException(같은 List<T>.Enumerator). 시험 `perf: count` 가 옛 모양과 경우마다 대조한다.
        // 일찍 끝내지 않는다(전부 센다): 일찍 끝내면 뒤쪽 원소의 결함(null 사실 등)이 던지지 않고 지나간다 — 던지던 입력이 조용해진다(C1).
        public static int CountMatching<T>(List<T> source, Func<T, bool> match)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (match == null) throw new ArgumentNullException("match");
            int n = 0;
            foreach (var x in source) if (match(x)) n++;
            return n;
        }

        // 합체 수치 특성 셈: 한 번 순회 — 우리 수치 특성(isOwned)인 사실마다 isFusion.
        // 옛 모양 = facts.Where(isOwned).ToList().Where(isFusion).ToList().Count(두 번 순회). 값은 같고, 던지는 입력도 같다.
         
        //   시험 `perf: count` 가 이 경우를 이름 붙여 고정한다(둘 다 던짐 — 던지던 것이 조용해지지는 않는다).
        public static int CountFusionStats<T>(List<T> facts, Func<T, bool> isOwned, Func<T, bool> isFusion)
        {
            if (facts == null) throw new ArgumentNullException("facts");
            if (isOwned == null) throw new ArgumentNullException("isOwned");
            if (isFusion == null) throw new ArgumentNullException("isFusion");
            int n = 0;
            foreach (var f in facts) if (isOwned(f) && isFusion(f)) n++;
            return n;
        }

        // 고아 정리 되읽기(m3): 형태·제어·수치 0, 모든 토글이 꺼져 있고 시작돼 있지 않다.
        public static bool CleanupDone(int forms, int controls, int stats, int togglesOn, int togglesStarted)
        {
            if (forms < 0 || controls < 0 || stats < 0 || togglesOn < 0 || togglesStarted < 0) throw new ArgumentOutOfRangeException("FusionTick.CleanupDone: negative count");
            return forms == 0 && controls == 0 && stats == 0 && togglesOn == 0 && togglesStarted == 0;
        }

        public static TickOutcome Run(FusionObs obs, IFusionTickWorld w)
        {
            if (obs == null || w == null) throw new ArgumentNullException(obs == null ? "obs" : "w");
            var ph = StartPhase.Classify(obs, false);
            switch (ph.Phase)
            {
                case FusionPhase.NoFusion: return TickOutcome.Idle;
                case FusionPhase.PendingStart: return TickOutcome.Pending;
                case FusionPhase.RejectedStart:
                {
                    var why = w.CheckSteady();
                    w.TurnToggleOff();
                    w.Warn("시작 거부: " + why + " — toggle turned off (no pending activate command, row 3)");
                    return TickOutcome.RejectedOff;
                }
                case FusionPhase.Orphan:
                    w.Error("orphan fusion artefacts " + ph + " " + obs + " — cleaning up");
                    w.CleanupOrphan();
                    return TickOutcome.OrphanCleaned;
                case FusionPhase.CoreBusy: return TickOutcome.CoreBusy;
                case FusionPhase.Active: break;
                default: throw new InvalidOperationException("FusionTick: phase " + ph + " outside the start handler");
            }
            switch (FusionInvariant.Check(obs))
            {
                case InvariantVerdict.Ok: return TickOutcome.Ok;
                case InvariantVerdict.StopPending: w.CountStopPending(); return TickOutcome.StopPending;
                case InvariantVerdict.FormLost:
                    w.Warn("형태 억제·해제 — 합체 종료 " + obs);
                    w.ConsumeFormLost();
                    w.DeliverEnd();
                    return TickOutcome.FormLostEnded;
                case InvariantVerdict.Violation:
                    w.Error("Active invariant violated " + obs + " — ending the fusion");
                    w.DeliverEnd();
                    w.TurnToggleOff();
                    return TickOutcome.ViolationEnded;
            }
            throw new InvalidOperationException("FusionTick: undefined invariant verdict");
        }
    }

    // 시작기 실행기(순수, §1-4): 사건 안 분류 → 거부·불일치 처리 → 파트 생성 → 재검증 → 시작.
    public interface IFusionStartWorld
    {
        void EnsurePart();                  // 파트 생성(여기서만)
        GolemFusionBlock CheckStarting();
        void Begin();                       // part.BeginWith(golem, control context)
        void SetIsOnFalse();
        void RemoveAppliedControl();        // 동기 → 엔진이 토글을 끈다
        void TurnOff();                     // 불일치 정리: 제어가 있으면 Remove, 토글 끔
        void Info(string m); void Warn(string m); void Error(string m);
    }

    public enum StartOutcome { RejectedNoBuff, Mismatch, RejectedByCheck, Started }

    public static class FusionStart
    {
        public static StartOutcome Run(FusionObs obs, IFusionStartWorld w)
        {
            if (obs == null || w == null) throw new ArgumentNullException(obs == null ? "obs" : "w");
            var ph = StartPhase.Classify(obs, true);
            if (ph.Phase == FusionPhase.StartRejectedNoBuff)
            {
                w.Warn("시작 거부: 제어 버프 추가 실패 " + obs);
                w.SetIsOnFalse();
                return StartOutcome.RejectedNoBuff;
            }
            if (ph.Phase != FusionPhase.Starting)
            {
                w.Error("시작 사건 상태 불일치 " + ph + " " + obs);
                w.TurnOff();
                return StartOutcome.Mismatch;
            }
            w.EnsurePart();
            if (!obs.HasPart) w.Info("part created by the start handler");
            var block = w.CheckStarting();
            if (block != GolemFusionBlock.None)
            {
                w.Warn("시작 거부: " + block);
                w.RemoveAppliedControl();
                return StartOutcome.RejectedByCheck;
            }
            w.Begin();
            return StartOutcome.Started;
        }
    }

    // Fusion.Check 의 입력 조합(§6-1) — 관측 → FusionCheck.Decide 인자. 게임 쪽은 관측값만 넘긴다.
    public static class FusionCheckInputs
    {
        // 남의 변신: 활성 변신이 있고 우리 형태가 아니다. 관측 실패는 호출자가 null 로 넘긴다.
        public static bool MasterPolymorphedByOther(FusionObs o) { if (o == null) throw new ArgumentNullException("o"); return o.ActivePolymorphPresent && !o.ActivePolymorphIsOurs; }

        // canStart: 파트가 Idle 이 아니면 코어 관문(gateCanStart), Idle·없음이면 산출물 분류(고아 → null) ∧ (파트 있으면 관문).
        public static bool? CanStart(FusionObs o, CheckPhase phase, bool? gateCanStart)
        {
            if (o == null) throw new ArgumentNullException("o");
            if (o.HasPart && o.State != FusionState.Idle) return gateCanStart;
            var c = StartPhase.CanStart(StartPhase.Classify(o, phase == CheckPhase.Starting).Phase, phase);
            if (!c.HasValue) return null;
            if (!o.HasPart) return c.Value;
            return gateCanStart.HasValue ? (bool?)(c.Value && gateCanStart.Value) : null;
        }

        public static GolemFusionBlock Decide(FusionObs o, CheckPhase phase, LifecycleState life, bool broken, bool? golemOk, bool? savePending, bool? gateCanStart)
        {
            if (o == null) throw new ArgumentNullException("o");
            var d = FusionCheck.Decide(life, broken, golemOk, savePending, MasterPolymorphedByOther(o), FusionGate.CanFuse(true, false, CanStart(o, phase, gateCanStart)));
            // nm4: 특성(= 흡수기·L10 숙주)이 없으면 행 12 는 NoFusion 이라 시작이 허용됐다 → 이름 붙인 사유로 막는다(없음은 통과가 아니다).
            if (d == GolemFusionBlock.None && !o.HasFeature) return GolemFusionBlock.FeatureMissing;
            return d;
        }
    }

    public static class FusionTauntAfterEnd
    {
        // §4-2(R4): 종료 뒤 골렘 생사 → 기대 원천. 죽었으면 없음, 살았으면 Idle 기대(생명주기 상태 반영), 골렘 미관측은 판정 불가 → throw.
        public static TauntSource Expected(PetLife life, LifecycleState lifecycle)
        {
            if (!Enum.IsDefined(typeof(PetLife), life)) throw new ArgumentException("FusionTauntAfterEnd: undefined pet life " + (int)life);
            switch (life)
            {
                case PetLife.Dead: return TauntSource.None;
                case PetLife.Alive: return TauntPlan.Expected(FusionState.Idle, lifecycle);
            }
            throw new InvalidOperationException("FusionTauntAfterEnd: golem not observed after the fusion ended (life " + life + ") — taunt cannot be reconciled");
        }
    }

    // 원장 ⑤·⑥: 시작(AddFusionBuff)·제거(RemoveFusionBuffs) 안의 원시 동작 순서. 게임 쪽 GameGolemWorld 가 원시 동작을 구현한다.
    public interface IGolemFusionPrims
    {
        void AssertControlIsApplied();      // 제어 1 == AppliedBuff
        void DetachGolemTaunt();            // Taunt.Reconcile(master, golem, None)
        string ReadbackGolemKey();          // 골렘 OurBuffs 키 정확히 1 == 기대 키 → 그 키
        void AddForm(string key);           // 형태 AddBuff(null → throw)
        void ReadbackForm(string key);      // formOk + 크기·슬램·도발 == Master
        void AddStat(string key);           // Materials.AddStat(master, Fusion, key)
        void ClearRuntime();                // swapPending·formLostPending·stopRequestedControl 비움
        int RemoveForms();                  // 형태 전부 Remove(selfFormOp) → 뗀 수
        int RemoveControls();               // 제어 전부 Remove → 뗀 수
        int RemoveStats();                  // 합체 수치 사실 전부 회수 → 뗀 수
    }

    public static class GolemFusionSequence
    {
        // 시작: 제어 단언 → 골렘 도발 떼기(두 원천 방지 — 형태의 m_Facts 가 본체에 도발을 붙인다) → 키 되읽기 → 형태 → 형태 되읽기 → 수치 → 런타임 초기화.
        public static void Begin(IGolemFusionPrims p)
        {
            if (p == null) throw new ArgumentNullException("p");
            p.AssertControlIsApplied();
            p.DetachGolemTaunt();
            string key = p.ReadbackGolemKey();
            if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("GolemFusionSequence.Begin: golem material key readback empty");
            p.AddForm(key);
            p.ReadbackForm(key);
            p.AddStat(key);
            p.ClearRuntime();
        }

        // 제거: 형태(본체 도발이 함께 빠진다) → 제어 → 수치. 반환 = 뗀 수 합.
        public static int RemoveAll(IGolemFusionPrims p)
        {
            if (p == null) throw new ArgumentNullException("p");
            int n = p.RemoveForms();
            n += p.RemoveControls();
            n += p.RemoveStats();
            return n;
        }
    }

    public static class FusionPetIdentity
    {
        // 원장 ②: 합체 골렘 = 저장된 PetId 와 같은 ID 의 우리 골렘. 다르면 throw(같은 블루프린트의 새 펫에 HP 를 쓰지 않는다 — WSS Fusion.cs:291-295).
        public static void Require(string petId, string observedId, bool observedIsOurs)
        {
            if (string.IsNullOrEmpty(petId)) throw new InvalidOperationException("Golemancer: fusion part has no PetId");
            if (observedId == null) throw new InvalidOperationException("Golemancer: fused golem " + petId + " not found (no companion)");
            if (!observedIsOurs) throw new InvalidOperationException("Golemancer: companion " + observedId + " is not our golem (fused golem " + petId + ")");
            if (observedId != petId) throw new InvalidOperationException("Golemancer: fused golem " + petId + " not found (current golem " + observedId + ")");
        }
    }
}
