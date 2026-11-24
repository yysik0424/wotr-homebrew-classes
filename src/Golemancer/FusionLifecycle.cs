using System;

namespace Golemancer
{
     
    // 종료 절차 = 단계 BuffRemoved → Unhidden → HpApplied(이 순서). 기계가 효과기에 한 단계씩 실행을 시키고, 효과기는 결과를
    // 사건(StepDone / StepFailed / UnhideNotReturned)으로 되돌린다(동기·비동기 모두 가능). Execute 가 던지면 그 단계 StepFailed.
    // SetHidden(false) 는 주인이 IsInGame·플레이어 진영이 아니면 펫을 켜지 않는다(@0007~0022) → UnhideNotReturned.
    // 재시도 상한 없음(임의값 금지, C3) — AwaitRestore 는 AreaDidLoad 마다 남은 단계를 다시 시도하고 실패마다 오류 로그.

    public enum FusionState { Idle, Active, Ending, AwaitRestore }

    [Flags]
    public enum FusionStep { None = 0, BuffRemoved = 1, Unhidden = 2, HpApplied = 4, All = 7 }

    public enum FusionEventKind
    {
        Start, Damage,
        DurationOver, ExternalUnhide, AreaUnloading, PreSave, PetDowned, ShamanDead, // 종료 묶음 6
        AreaDidLoad, StepDone, StepFailed, UnhideNotReturned,
    }

    public sealed class FusionEvent
    {
        public readonly FusionEventKind Kind;
        public readonly FusionStep Step;     // StepDone·StepFailed 만
        public readonly int PetHp, Damage;   // Damage 만
        FusionEvent(FusionEventKind k, FusionStep s, int hp, int dmg) { Kind = k; Step = s; PetHp = hp; Damage = dmg; }
        public static FusionEvent Of(FusionEventKind k)
        {
            if (k == FusionEventKind.Damage || k == FusionEventKind.StepDone || k == FusionEventKind.StepFailed) throw new ArgumentException("FusionEvent: " + k + " needs arguments");
            return new FusionEvent(k, FusionStep.None, 0, 0);
        }
        public static FusionEvent Hit(int petHp, int damage) { return new FusionEvent(FusionEventKind.Damage, FusionStep.None, petHp, damage); }
        public static FusionEvent Done(FusionStep s) { return new FusionEvent(FusionEventKind.StepDone, Single(s), 0, 0); }
        public static FusionEvent Failed(FusionStep s) { return new FusionEvent(FusionEventKind.StepFailed, Single(s), 0, 0); }
        static FusionStep Single(FusionStep s)
        {
            if (s != FusionStep.BuffRemoved && s != FusionStep.Unhidden && s != FusionStep.HpApplied) throw new ArgumentException("FusionEvent: step must be exactly one step (got " + s + ")");
            return s;
        }
        public override string ToString() { return Kind + (Step != FusionStep.None ? "(" + Step + ")" : "") + (Kind == FusionEventKind.Damage ? "(" + PetHp + "," + Damage + ")" : ""); }
    }

    public enum FusionOutcome { Transition, Ignored, Refused, Absorbed }
    public enum FusionRefusal { None, Ending, AwaitRestore }

    public sealed class HpSplitResult
    {
        public readonly int PetHp, ShamanDamage;
        public readonly bool PetDowned;
        public HpSplitResult(int petHp, int shamanDamage, bool petDowned) { PetHp = petHp; ShamanDamage = shamanDamage; PetDowned = petDowned; }
    }

    public sealed class FusionResult
    {
        public FusionOutcome Outcome;
        public FusionRefusal Refusal;
        public HpSplitResult Split;   // Active + Damage 만
    }

    public interface IFusionEffects
    {
        void Begin();                 // 숨김 + 버프(Idle → Active)
        void Execute(FusionStep step); // 종료 단계 하나 — 결과는 FusionMachine.Handle(StepDone/StepFailed/UnhideNotReturned) 로
        void Log(string level, string message);
    }

    public sealed class FusionMachine
    {
        readonly IFusionEffects fx;
        public FusionState State { get; private set; }
        public bool HiddenByUs { get; private set; }
        public FusionStep PendingSteps { get; private set; }
        FusionStep inFlight = FusionStep.None; // 런타임 전용: Execute 를 불렀고 아직 결과가 오지 않은 단계
        bool touched;                          // 런타임 전용: Handle·Loaded 가 한 번이라도 돌았는가(Loaded 는 생성 직후 1회만)

        FusionMachine(IFusionEffects fx, FusionState s, bool hidden, FusionStep pending)
        {
            if (fx == null) throw new ArgumentNullException("fx");
            Validate(s, hidden, pending);
            this.fx = fx; State = s; HiddenByUs = hidden; PendingSteps = pending;
        }

        // 저장 값의 일관성: Idle ⇒ 숨김 없음·남은 단계 0, Active ⇒ 숨김·남은 0, Ending/AwaitRestore ⇒ 남은 ≠ 0. 어긋나면 손상 → throw.
        static void Validate(FusionState s, bool hidden, FusionStep pending)
        {
            if (!Enum.IsDefined(typeof(FusionState), s)) throw new ArgumentException("FusionMachine: undefined state " + (int)s);
            if ((pending & ~FusionStep.All) != 0) throw new ArgumentException("FusionMachine: undefined step bits " + (int)pending);
            bool ok = s == FusionState.Idle ? !hidden && pending == FusionStep.None
                : s == FusionState.Active ? hidden && pending == FusionStep.None
                : pending != FusionStep.None;
            if (!ok) throw new InvalidOperationException("FusionMachine: inconsistent saved state " + s + " hidden=" + hidden + " pending=" + pending);
        }

        public static FusionMachine New(IFusionEffects fx) { return new FusionMachine(fx, FusionState.Idle, false, FusionStep.None); }

        // 저장값(또는 같은 세션의 상태)으로 만든다 — 효과 없음. 로드면 효과기 연결 뒤 Loaded() 를 1회 부른다.
        public static FusionMachine At(IFusionEffects fx, FusionState s, bool hiddenByUs, FusionStep pending) { return new FusionMachine(fx, s, hiddenByUs, pending); }

        // 로드 시 1회(생성 직후, 다른 사건 전): Idle → Idle · Active → Ending(+WARN, 남은 = 전부, 즉시 첫 단계 실행 — Ending 은 다른 사건을 무시하므로
        // 여기서 시작해야 진행된다) · Ending → AwaitRestore(남은 단계 그대로, 다음 AreaDidLoad 에서 재시도) · AwaitRestore → AwaitRestore.
        public void Loaded()
        {
            if (touched) throw new InvalidOperationException("FusionMachine: Loaded must be the first call, exactly once");
            touched = true;
            switch (State)
            {
                case FusionState.Active:
                    fx.Log("WARN", "fusion was Active in the save (pre-save end failed) — ending now");
                    BeginEnding();
                    break;
                case FusionState.Ending:
                    State = FusionState.AwaitRestore;
                    fx.Log("WARN", "fusion was Ending in the save — awaiting restore, pending " + PendingSteps);
                    break;
            }
        }

        public static HpSplitResult HpSplit(int petHp, int damage)
        {
            if (petHp < 0) throw new ArgumentOutOfRangeException("petHp");
            if (damage < 0) throw new ArgumentOutOfRangeException("damage");
            if (damage < petHp) return new HpSplitResult(petHp - damage, 0, false);
            return new HpSplitResult(0, damage - petHp, true);
        }

        static bool IsEndGroup(FusionEventKind k)
        {
            return k == FusionEventKind.DurationOver || k == FusionEventKind.ExternalUnhide || k == FusionEventKind.AreaUnloading
                || k == FusionEventKind.PreSave || k == FusionEventKind.PetDowned || k == FusionEventKind.ShamanDead;
        }

        static FusionResult R(FusionOutcome o) { return new FusionResult { Outcome = o }; }

        public FusionResult Handle(FusionEvent e)
        {
            if (e == null) throw new ArgumentNullException("e");
            touched = true;
            switch (State)
            {
                case FusionState.Idle:
                    if (e.Kind == FusionEventKind.Start)
                    {
                        State = FusionState.Active;
                        HiddenByUs = true;
                        fx.Begin();
                        return R(FusionOutcome.Transition);
                    }
                    if (IsStepReport(e.Kind)) throw new InvalidOperationException("FusionMachine: " + e + " while Idle");
                    return R(FusionOutcome.Ignored);

                case FusionState.Active:
                    if (e.Kind == FusionEventKind.Start) throw new InvalidOperationException("FusionMachine: Start while Active (double start)");
                    if (IsStepReport(e.Kind)) throw new InvalidOperationException("FusionMachine: " + e + " while Active");
                    if (e.Kind == FusionEventKind.Damage)
                    {
                        var split = HpSplit(e.PetHp, e.Damage);
                        if (split.PetDowned) Handle(FusionEvent.Of(FusionEventKind.PetDowned));
                        return new FusionResult { Outcome = split.PetDowned ? FusionOutcome.Transition : FusionOutcome.Absorbed, Split = split };
                    }
                    // 종료 묶음 6 · AreaDidLoad → Ending
                    BeginEnding();
                    return R(FusionOutcome.Transition);

                case FusionState.Ending:
                    switch (e.Kind)
                    {
                        case FusionEventKind.Start: return new FusionResult { Outcome = FusionOutcome.Refused, Refusal = FusionRefusal.Ending };
                        case FusionEventKind.StepDone: return StepDone(e.Step);
                        case FusionEventKind.StepFailed:
                            Require(e.Step);
                            inFlight = FusionStep.None;
                            State = FusionState.AwaitRestore;
                            fx.Log("ERROR", "fusion end step " + e.Step + " failed — awaiting restore, pending " + PendingSteps);
                            return R(FusionOutcome.Transition);
                        case FusionEventKind.UnhideNotReturned:
                            Require(FusionStep.Unhidden);
                            inFlight = FusionStep.None;
                            State = FusionState.AwaitRestore;
                            fx.Log("ERROR", "fusion end: spirit did not return on unhide — awaiting restore");
                            return R(FusionOutcome.Transition);
                        default:
                            fx.Log("INFO", "fusion Ending ignores " + e);
                            return R(FusionOutcome.Ignored);
                    }

                case FusionState.AwaitRestore:
                    switch (e.Kind)
                    {
                        case FusionEventKind.Start: return new FusionResult { Outcome = FusionOutcome.Refused, Refusal = FusionRefusal.AwaitRestore };
                        case FusionEventKind.StepDone: return StepDone(e.Step);
                        case FusionEventKind.StepFailed:
                            Require(e.Step);
                            inFlight = FusionStep.None;
                            fx.Log("ERROR", "fusion restore step " + e.Step + " failed again — pending " + PendingSteps);
                            return R(FusionOutcome.Ignored);
                        case FusionEventKind.UnhideNotReturned:
                            Require(FusionStep.Unhidden);
                            inFlight = FusionStep.None;
                            fx.Log("ERROR", "fusion restore: spirit still did not return — pending " + PendingSteps);
                            return R(FusionOutcome.Ignored);
                        case FusionEventKind.ExternalUnhide:
                            // 외부(에튜드 등)가 펫을 되살렸다 → Unhidden 완료 처리 후 남은 단계 실행.
                            if ((PendingSteps & FusionStep.Unhidden) != 0)
                            {
                                PendingSteps &= ~FusionStep.Unhidden;
                                if (inFlight == FusionStep.Unhidden) inFlight = FusionStep.None;
                            }
                            if (PendingSteps == FusionStep.None) { ToIdle(); return R(FusionOutcome.Transition); }
                            if (inFlight == FusionStep.None) ExecuteNext();
                            return R(FusionOutcome.Transition);
                        case FusionEventKind.AreaDidLoad:
                            if (inFlight != FusionStep.None) { fx.Log("WARN", "fusion restore retry skipped: " + inFlight + " in flight (its result never arrived)"); return R(FusionOutcome.Ignored); }
                            fx.Log("INFO", "fusion restore retry on area load, pending " + PendingSteps);
                            ExecuteNext();
                            return R(FusionOutcome.Transition);
                        default:
                            return R(FusionOutcome.Ignored);
                    }
            }
            throw new InvalidOperationException("FusionMachine: undefined state " + State);
        }

        static bool IsStepReport(FusionEventKind k) { return k == FusionEventKind.StepDone || k == FusionEventKind.StepFailed || k == FusionEventKind.UnhideNotReturned; }

        // 보고된 단계는 남은 단계여야 하고, 실행 중인 단계가 있으면 바로 그 단계여야 한다(순서가 틀린 보고를 조용히 완료 처리하지 않음).
        void Require(FusionStep s)
        {
            if ((PendingSteps & s) == 0) throw new InvalidOperationException("FusionMachine: report for step " + s + " which is not pending (" + PendingSteps + ")");
            if (inFlight != FusionStep.None && inFlight != s) throw new InvalidOperationException("FusionMachine: report for step " + s + " while " + inFlight + " is in flight");
        }

        // 재진입 차단: 상태를 먼저 Ending 으로 세운 뒤 단계를 실행한다 — 실행 중 동기로 들어오는 종료 묶음 사건은 Ending 칸(무시)으로 간다.
        void BeginEnding()
        {
            State = FusionState.Ending;
            PendingSteps = FusionStep.All;
            ExecuteNext();
        }

        FusionResult StepDone(FusionStep s)
        {
            Require(s);
            PendingSteps &= ~s;
            if (inFlight == s) inFlight = FusionStep.None;
            if (PendingSteps == FusionStep.None) { ToIdle(); return R(FusionOutcome.Transition); }
            if (inFlight == FusionStep.None) ExecuteNext();
            return R(FusionOutcome.Transition);
        }

        void ToIdle()
        {
            State = FusionState.Idle;
            HiddenByUs = false;
            inFlight = FusionStep.None;
        }

        static FusionStep First(FusionStep pending)
        {
            foreach (var s in new[] { FusionStep.BuffRemoved, FusionStep.Unhidden, FusionStep.HpApplied })
                if ((pending & s) != 0) return s;
            return FusionStep.None;
        }

        void ExecuteNext()
        {
            var s = First(PendingSteps);
            if (s == FusionStep.None) return;
            inFlight = s;
            try { fx.Execute(s); }
            catch (Exception ex)
            {
                fx.Log("ERROR", "fusion step " + s + " threw: " + ex.Message);
                if (inFlight == s && (PendingSteps & s) != 0) Handle(FusionEvent.Failed(s));
            }
        }
    }
}
