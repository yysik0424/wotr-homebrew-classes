using System;
using System.Collections.Generic;

namespace Golemancer
{
     
    //  - FusionGate: FusionMachine(T2 전이표 48 + Loaded 4, 코어 수정 없음) 앞의 관문.
    //      (a) 로드 대기(D2): 저장값이 Idle 이 아니면 Loaded() 를 첫 AreaDidLoad 까지 미룬다 — OnPostLoad 시점엔 영병이 아직 PostLoad 전일 수 있어
    //          효과를 실행하지 않는다(SceneEntitiesState.PostLoad @0159 개체별, 인벤토리 @01F6; SetHidden @000B get_Master). 대기 중 사건은 코어에 넣지 않고 WARN.
    //          대기 중 PreSave 는 저장값 그대로(WARN). 대기 중 흡수는 거부(WARN, 주술사가 피해 전부).
    //      (b) 자기 유발 차단(D9): 우리 Begin/Execute 실행 중(깊이 카운터 + try/finally — 동기 보고 안에서 Execute 가 중첩된다) 들어온 영병 IsInGame 사건
    //          (ExternalUnhide)은 코어에 넣지 않고 INFO. bool 이면 안쪽 finally 가 바깥 실행 중에 차단을 푼다.
    //      (c) 흡수는 State == Active 이고 대기 아님일 때만(I4 d). 시작은 State == Idle 이고 대기 아님일 때만(I2).
    //      (d) Begin 부분 실패(I1): 코어 사건을 새로 만들지 않는다 — ERROR 로그 뒤 종료 묶음 사건 StartFailureEnd 를 넣어 종료 단계(되읽기 포함)가 붙은 것을 정리한다.
    //  - FusionEffects: IFusionEffects 의 순수 구현 — 단계마다 IFusionWorld 의 원시 동작을 정해진 순서로 부르고 결과 사건을 관문에 되돌린다.
    //  - FusionRuntime: 관문 + 효과기 + 세계를 묶고, 흡수 계산(Calc)·확정(Deal)을 정한다(D4: HP 풀 = 숨긴 영병 자신의 HP, 흡수 100%).
    public interface IFusionWorld
    {
        void AddFusionBuff();            // 융합 버프를 붙인다(실패면 throw). 강신 복사는 버프 켜짐에서 PossessionBuffComponent 가 한다.
        int RemoveFusionBuffs();         // 붙은 융합 버프를 모두 뗀다 → 뗀 수
        int FusionBuffCount();
        int PossessionRecordCount();     // 주술사 강신 원장 기록 수 + 주술사 아이템 위 우리 강신 인스턴스 수
        void EndPossessionRecords();     // 강신 종료 규칙(Possession.BuffDeactivated — 기록 대조 후 제거)
        void SetPetHidden(bool hidden);  // UnitPartPet.SetHidden — 영병을 못 찾으면 throw
        bool PetInGame();
        int PetHpLeft();
        void DamagePet(int amount);      // 숨긴 영병 HP 에서 뺀다(m_Damage, 엔진이 저장)
        void SettleLifeState();          // UnitLifeController.ForceTickOnUnit(영병) — 엔진 생명 상태 판정
        bool PetDead();
        string PetLifeState();
        void Log(string level, string message);
    }

    public enum GateDrop { None, LoadPending, SelfTriggered, NotActive }

    public sealed class GateResult
    {
        public readonly FusionResult Result; // Dropped != None 이면 null
        public readonly GateDrop Dropped;
        public GateResult(FusionResult r, GateDrop d) { Result = r; Dropped = d; }
    }

     
    public enum FusionBlock { None, Possession, PossessionActive, NotIdle, Unknown }

    // 엔진이 매 프레임 묻는 질의(IAbilityCasterRestriction — ActionBarVM IsEnable 람다·UnitBrain.GetAvailableAutoUseAbility·UnitUseSpellsOnRest 의 능력 순회에 catch 없음,
     
    public static class PolledQuery
    {
        public static bool Passes(bool? verdict) { return verdict == true; }
        public static bool Passes(FusionBlock b) { return b == FusionBlock.None; }
    }

    public sealed class UnknownOnce
    {
        readonly HashSet<string> seen = new HashSet<string>();

         
        // 강신(PossessionGate, "possession check")과 융합(Fusion.Check, "fusion: …")은 관측 이름이 달라 키가 겹치지 않는다.
        public static string KeyOf(string unitId, string what, Exception ex)
        {
            if (unitId == null) throw new ArgumentNullException("unitId");
            if (string.IsNullOrEmpty(what)) throw new ArgumentException("UnknownOnce.KeyOf: observation name is empty");
            if (ex == null) throw new ArgumentNullException("ex");
            return unitId + "|" + what + "|" + ex.GetType().FullName + "|" + ex.Message;
        }
        // 이 키의 첫 판정 불가면 true(기록할 것), 이후 false.
        public bool First(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            return seen.Add(key);
        }
    }

    public sealed class FusionGate
    {
        // Begin 부분 실패 때 넣는 종료 묶음 사건(I1). 종료 묶음 6 은 T2 전이표에서 Active 칸이 모두 같다(→ Ending) — 어느 것이든 같은 칸이고, 이름은 로그 판독용.
        public const FusionEventKind StartFailureEnd = FusionEventKind.DurationOver;

        readonly IFusionEffects inner;
        readonly FusionMachine m;
        bool loadPending;
        int selfDepth;

        // 코어에 넘기는 효과기: 우리 실행 구간을 깊이 카운터로 센다(중첩 안전).
        sealed class Counting : IFusionEffects
        {
            readonly FusionGate g;
            public Counting(FusionGate g) { this.g = g; }
            public void Begin() { g.selfDepth++; try { g.inner.Begin(); } finally { g.selfDepth--; } }
            public void Execute(FusionStep step) { g.selfDepth++; try { g.inner.Execute(step); } finally { g.selfDepth--; } }
            public void Log(string level, string message) { g.inner.Log(level, message); }
        }

        FusionGate(IFusionEffects fx, FusionState s, bool hiddenByUs, FusionStep pending)
        {
            if (fx == null) throw new ArgumentNullException("fx");
            inner = fx;
            m = FusionMachine.At(new Counting(this), s, hiddenByUs, pending); // 저장값 손상 → 여기서 throw
            if (s == FusionState.Idle) m.Loaded(); // Idle → Idle, 효과 없음
            else
            {
                loadPending = true;
                fx.Log("WARN", "fusion state " + s + " (pending " + pending + ") — load reconciliation deferred to the first area load");
            }
        }

        // 저장값(새 파트면 기본값 Idle/false/None)으로 만든다.
        public static FusionGate Restore(IFusionEffects fx, FusionState s, bool hiddenByUs, FusionStep pending) { return new FusionGate(fx, s, hiddenByUs, pending); }

        public FusionState State { get { return m.State; } }
        public bool HiddenByUs { get { return m.HiddenByUs; } }
        public FusionStep PendingSteps { get { return m.PendingSteps; } }
        public bool LoadPending { get { return loadPending; } }
        public int SelfDepth { get { return selfDepth; } }

        public bool CanStart { get { return !loadPending && m.State == FusionState.Idle; } }
        public bool AbsorbAllowed { get { return !loadPending && m.State == FusionState.Active; } }

        // 시전 제한(D6·I2): 일반 강신 판정 통과 + 일반 강신 버프 없음 + 융합 Idle(대기 아님). 셋 중 하나라도 판정 불가(null)면 Unknown(차단, I-1).
        public static FusionBlock CanFuse(bool? possessionOk, bool? possessionBuffActive, bool? fusionCanStart)
        {
            if (!possessionOk.HasValue || !possessionBuffActive.HasValue || !fusionCanStart.HasValue) return FusionBlock.Unknown;
            if (!possessionOk.Value) return FusionBlock.Possession;
            if (possessionBuffActive.Value) return FusionBlock.PossessionActive;
            if (!fusionCanStart.Value) return FusionBlock.NotIdle;
            return FusionBlock.None;
        }

        static bool IsReport(FusionEventKind k) { return k == FusionEventKind.StepDone || k == FusionEventKind.StepFailed || k == FusionEventKind.UnhideNotReturned; }

        // 게임 사건(종료 묶음 · Damage). Start/AreaDidLoad/PreSave/단계 보고는 전용 메서드로.
        public GateResult Deliver(FusionEvent e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (e.Kind == FusionEventKind.Start || e.Kind == FusionEventKind.AreaDidLoad || e.Kind == FusionEventKind.PreSave || IsReport(e.Kind))
                throw new ArgumentException("FusionGate.Deliver: " + e.Kind + " has its own entry point");
            if (loadPending)
            {
                inner.Log("WARN", "fusion event " + e + " before load reconciliation — not delivered (the first area load reconciles from the world)");
                return new GateResult(null, GateDrop.LoadPending);
            }
            if (selfDepth > 0 && e.Kind == FusionEventKind.ExternalUnhide)
            {
                inner.Log("INFO", "fusion: spirit in-game change during our own step — not an external unhide");
                return new GateResult(null, GateDrop.SelfTriggered);
            }
            return new GateResult(m.Handle(e), GateDrop.None);
        }

        public FusionResult Start()
        {
            if (loadPending) throw new InvalidOperationException("FusionGate.Start while load reconciliation is pending (the caster restriction must block this)");
            var before = m.State;
            try { return m.Handle(FusionEvent.Of(FusionEventKind.Start)); }
            catch (Exception ex)
            {
                if (before != FusionState.Idle || m.State != FusionState.Active) throw; // Begin 실패가 아닌 예외(예: 이중 시작)는 그대로
                inner.Log("ERROR", "fusion start failed partway (" + ex.GetType().Name + ": " + ex.Message + ") — ending with " + StartFailureEnd + " to clean up");
                 
                try { m.Handle(FusionEvent.Of(StartFailureEnd)); }
                catch (Exception cleanup) { throw new AggregateException("Golemancer: fusion start failed (" + ex.Message + ") and its cleanup threw (" + cleanup.Message + "); state " + m.State, ex, cleanup); }
                throw new InvalidOperationException("Golemancer: fusion start failed (" + ex.Message + "); cleanup ran, state now " + m.State + " pending " + m.PendingSteps, ex);
            }
        }

        // 첫 AreaDidLoad: 대기 중이면 Loaded() 를 먼저(코어 계약: 생성 뒤 첫 호출), 이어 AreaDidLoad.
        public FusionResult AreaDidLoad()
        {
            if (loadPending)
            {
                loadPending = false;
                inner.Log("INFO", "fusion load reconciliation on the first area load: saved state " + m.State + " pending " + m.PendingSteps);
                m.Loaded();
            }
            return m.Handle(FusionEvent.Of(FusionEventKind.AreaDidLoad));
        }

        public GateResult PreSave()
        {
            if (loadPending)
            {
                inner.Log("WARN", "fusion pre-save while load reconciliation is pending — the saved state " + m.State + " is kept");
                return new GateResult(null, GateDrop.LoadPending);
            }
            return new GateResult(m.Handle(FusionEvent.Of(FusionEventKind.PreSave)), GateDrop.None);
        }

        // 효과기의 단계 결과 보고(동기). 대기 중에는 단계가 실행될 수 없다 → throw.
        public FusionResult Report(FusionEvent e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (!IsReport(e.Kind)) throw new ArgumentException("FusionGate.Report: " + e.Kind + " is not a step report");
            if (loadPending) throw new InvalidOperationException("FusionGate.Report(" + e + ") while load reconciliation is pending");
            return m.Handle(e);
        }

        // 흡수량(Calc). 허용 아니면 0 + WARN. 허용이면 HpSplit 의 영병 몫(= min(incoming, petHp)).
        public int Absorb(int petHp, int incoming)
        {
            if (!AbsorbAllowed)
            {
                inner.Log("WARN", "fusion absorb refused (" + (loadPending ? "load reconciliation pending" : "state " + m.State) + ") — the shaman takes the full " + incoming);
                return 0;
            }
            var s = FusionMachine.HpSplit(petHp, incoming);
            return petHp - s.PetHp;
        }

         
        public static bool SameIdentitySet(IList<object> before, IList<object> after)
        {
            if (before == null || after == null) throw new ArgumentNullException(before == null ? "before" : "after");
            if (before.Count != after.Count) return false;
            foreach (var x in before)
            {
                bool found = false;
                foreach (var y in after) if (ReferenceEquals(x, y)) { found = true; break; }
                if (!found) return false;
            }
            return true;
        }

        // HpApplied 되읽기(I3). 판정 원천 = 엔진 ForceTickOnUnit(HP ≤ 0 일 때 효과기가 부른다): Dead·Unconscious·Conscious(Ferocity @01BD~01CE, Infallible @0119~012C)
        // 모두 엔진 판정이므로 받아들인다. 엔진 규칙상 불가능한 조합(HP > 0 인데 Dead)만 실패.
        public static bool HpVerdict(int hpLeftAfter, bool deadAfter) { return hpLeftAfter > 0 ? !deadAfter : true; }
    }

    // IFusionEffects 의 순수 구현. 단계 순서는 코어(BuffRemoved → Unhidden → HpApplied)가, 단계 안 원시 동작 순서는 여기서 정한다.
    public sealed class FusionEffects : IFusionEffects
    {
        readonly IFusionWorld w;
        FusionGate gate;

        public FusionEffects(IFusionWorld world)
        {
            if (world == null) throw new ArgumentNullException("world");
            w = world;
        }

        public void Attach(FusionGate g)
        {
            if (g == null) throw new ArgumentNullException("g");
            if (gate != null) throw new InvalidOperationException("FusionEffects.Attach called twice");
            gate = g;
        }

        FusionGate G()
        {
            if (gate == null) throw new InvalidOperationException("FusionEffects used before Attach");
            return gate;
        }

        // Idle → Active: 버프 먼저(강신 복사가 보이는 영병 무기를 읽는다), 그 뒤 숨김 + 되읽기.
        public void Begin()
        {
            w.AddFusionBuff();
            w.SetPetHidden(true);
            if (w.PetInGame()) throw new InvalidOperationException("Golemancer: weapon spirit still in game after SetHidden(true)");
        }

        public void Execute(FusionStep step)
        {
            var g = G();
            switch (step)
            {
                case FusionStep.BuffRemoved:
                {
                    int removed = w.RemoveFusionBuffs();
                    // 외부 제거(DurationOver)는 버프 Deactivate 전에 온다(Remove @003B → @0041) → 강신 기록이 남아 있으면 종료 규칙을 직접 돌린다(m1).
                    if (w.PossessionRecordCount() > 0) w.EndPossessionRecords();
                    int buffs = w.FusionBuffCount(), recs = w.PossessionRecordCount();
                    if (buffs == 0 && recs == 0) { g.Report(FusionEvent.Done(step)); return; }
                    w.Log("ERROR", "fusion end: BuffRemoved readback failed (removed " + removed + ", fusion buffs left " + buffs + ", possession records/instances left " + recs + ")");
                    g.Report(FusionEvent.Failed(step));
                    return;
                }
                case FusionStep.Unhidden:
                    w.SetPetHidden(false);
                    g.Report(w.PetInGame() ? FusionEvent.Done(step) : FusionEvent.Of(FusionEventKind.UnhideNotReturned));
                    return;
                case FusionStep.HpApplied:
                {
                    int hp = w.PetHpLeft();
                    if (hp <= 0) w.SettleLifeState();
                    int after = w.PetHpLeft();
                    bool dead = w.PetDead();
                    bool ok = FusionGate.HpVerdict(after, dead);
                    w.Log(ok ? "INFO" : "ERROR", "fusion end: spirit HP " + after + ", life state " + w.PetLifeState() + (hp <= 0 ? " (engine life tick)" : ""));
                    g.Report(ok ? FusionEvent.Done(step) : FusionEvent.Failed(step));
                    return;
                }
                default: throw new ArgumentException("FusionEffects.Execute: not a single step: " + step);
            }
        }

        public void Log(string level, string message) { w.Log(level, message); }
    }

    public sealed class AbsorbCalc
    {
        public readonly bool Allowed;
        public readonly int PetHp, Incoming, Absorbed;
        public AbsorbCalc(bool allowed, int petHp, int incoming, int absorbed) { Allowed = allowed; PetHp = petHp; Incoming = incoming; Absorbed = absorbed; }
    }

    public sealed class FusionRuntime
    {
        public readonly FusionGate Gate;
        readonly IFusionWorld w;

        FusionRuntime(IFusionWorld world, FusionState s, bool hiddenByUs, FusionStep pending)
        {
            if (world == null) throw new ArgumentNullException("world");
            w = world;
            var fx = new FusionEffects(world);
            Gate = FusionGate.Restore(fx, s, hiddenByUs, pending);
            fx.Attach(Gate);
        }

        public static FusionRuntime Restore(IFusionWorld world, FusionState s, bool hiddenByUs, FusionStep pending) { return new FusionRuntime(world, s, hiddenByUs, pending); }

        // 계산 단계: 허용이면 숨긴 영병 HP 를 읽어 흡수량, 아니면 거부(관문이 WARN, 영병을 읽지 않음).
        public AbsorbCalc Calc(int incoming)
        {
            if (incoming < 0) throw new ArgumentOutOfRangeException("incoming");
            if (!Gate.AbsorbAllowed) return new AbsorbCalc(false, -1, incoming, Gate.Absorb(0, incoming));
            int hp = w.PetHpLeft();
            if (hp < 0) throw new InvalidOperationException("Golemancer: hidden weapon spirit HP " + hp + " < 0 while fusion is Active (PetDowned should have ended it)");
            return new AbsorbCalc(true, hp, incoming, Gate.Absorb(hp, incoming));
        }

        // 확정 단계(!IsFake): 영병 HP 반영 → 코어 Damage(넘치면 PetDowned → 종료 단계, HpApplied 가 반영된 HP 를 본다).
        // Calc 와 Deal 사이에 Active 를 벗어났으면 영병 HP 만 반영하고 코어엔 넣지 않는다(D8, WARN).
        public GateResult Deal(AbsorbCalc c)
        {
            if (c == null) throw new ArgumentNullException("c");
            if (!c.Allowed) throw new InvalidOperationException("FusionRuntime.Deal for a refused absorb calculation");
            var split = FusionMachine.HpSplit(c.PetHp, c.Incoming);
            if (c.PetHp - split.PetHp != c.Absorbed) throw new InvalidOperationException("Golemancer: fusion absorb " + c.Absorbed + " != HpSplit(" + c.PetHp + "," + c.Incoming + ") pet share " + (c.PetHp - split.PetHp));
            int now = w.PetHpLeft();
            if (now != c.PetHp) throw new InvalidOperationException("Golemancer: hidden weapon spirit HP changed between calculation (" + c.PetHp + ") and damage (" + now + ")");
            if (c.Absorbed > 0) w.DamagePet(c.Absorbed);
            if (!Gate.AbsorbAllowed)
            {
                w.Log("WARN", "fusion left Active between damage calculation and damage (" + Gate.State + ") — " + c.Absorbed + " applied to the spirit HP only");
                return new GateResult(null, GateDrop.NotActive);
            }
            return Gate.Deliver(FusionEvent.Hit(c.PetHp, c.Incoming));
        }
    }
}
