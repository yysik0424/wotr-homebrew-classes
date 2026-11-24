using System;
using System.Collections.Generic;

namespace Golemancer
{
     
    // 게임 쪽(T3 GolemMasterComponent)이 원시 사건마다 그 순간의 관측(Obs)과 함께 Handle 을 부르고, 돌아온 명령(Cmd)을 순서대로 실행한다.
    //   ① 어느 상태든 Obs.Conflict → Conflict(종결, ERROR 1회, 이후 Ignore).
    //      어느 상태든 Bound.Id != null && Obs.GolemId != null && Obs.GolemId != Bound.Id → Detached(WARN — 옛 골렘 파괴 여부는 아직 모름). Bind/Apply 분기보다 먼저 검사.
    //      Rebind 는 Detached 에서만(그 외 throw), oldDestroyed == true 일 때만 받는다(false → Ignore + ERROR).
    //   ② Unbound: 결속 경계 사건에서 검증 통과 → Bind(id) + Apply(id, level, key, taunt = Expected(fusion, Ready)) → BoundPending. 우리 것이 아닌 펫 → Conflict.
    //   ③ BoundPending(Bind 직후·Apply 실패·저장 ID 로드): 같은 ID + 검증 → Apply(Bound.Id) 만(Bind 없음). ApplyOk → Ready, ApplyFailed → 유지(ERROR).
    //      AreaDidLoad 뒤에도 Unbound/BoundPending 이면 ERROR 1회(영구 미준비 경계) + 계속 Wait.
    //   ④ Ready: 골렘 미관측 → Detached(WARN); 관측 == 적용 기록 → Ignore; 다르면 Stale(예약만).
    //   ⑤ Stale: 안정 경계 사건(PetLevelUp/Poll/AreaDidLoad/RestSucceeded)에서 펫 레벨 == 주인 레벨 && 재질 키 있음 → Apply. ApplyOk → Ready, ApplyFailed → 유지(ERROR).
    //   ⑥ Detached: 일반 사건 Ignore, GolemDestroyed(옛 ID) Ignore, Rebind(newId, true) → Bound = newId, Applied 비움 → BoundPending.
    //   ⑦ (T3 fix 1, 원장 판정 I1) 골렘 사망 관측: Apply 를 낼 자리(②③⑤)에서 골렘이 죽어 있으면 Wait("golem dead — waits for rest")(Bind·Apply 없음, AreaDidLoad 미준비 ERROR 없음 — 부서짐은 정상 상태),
    //      Ready 에서 사망 관측 → Stale(예약) — 휴식 되살림 뒤 RestSucceeded(⑤ 경계)가 Apply 로 도발까지 다시 붙인다(계획 ④). 죽은 펫에 Taunt.Attach 는 엔진이 거부한다(BuffCollection.PrepareFactForAttach @0011-0039).
    //      재결속 여부는 순수 함수 DecideRebind(상태, BoundId, 새 ID, 우리 것, 옛 골렘 파괴) — 어댑터(T3)가 **모든 관측 Handle 뒤** 부르고 Rebind 면 Handle(Rebind(newId, true)).
    //      옛 골렘 파괴 판정은 어댑터의 관측(Game.Instance.State.Units)이 입력 — 코어는 GolemDestroyed 사건을 기록하지 않는다(판정에 쓰지 않는 상태를 두지 않음, 검토 T2 Minor 3).
    // Apply 트랜잭션(R3-02) = Execute(cmd, 효과기): GolemOf → Materials → Taunt(기대 = 명령 인자) → Readback → ApplyOk, 어느 단계든 throw → ApplyFailed.
    public enum LifecycleState { Unbound, BoundPending, Ready, Stale, Detached, Conflict }

    public enum LifeEventKind { Activated, AddPet, PetLevelUp, MasterLevelUp, AreaDidLoad, Poll, RestSucceeded, GolemDestroyed, Rebind, ApplyOk, ApplyFailed }

    public enum CmdKind { Wait, Ignore, Bind, Apply, Report }

    public enum ReportLevel { Warn, Error }

    // 재결속 판정(컨트롤러 판정 fix 1): Detached 밖 = NotApplicable · Detached 에서 같은 ID 또는 우리 것 아님 = Ignore · 옛 골렘 생존 = ErrorOldAlive · 그 외 = Rebind.
    public enum RebindDecision { NotApplicable, Ignore, ErrorOldAlive, Rebind }

    // 관측한 펫의 생사(T3 fix 1). None = 펫 칸이 비어 관측할 펫이 없음(GolemId == null 일 때만), Alive/Dead = 펫이 있을 때만. 기본값 없음 — Obs 생성자가 짝을 검사한다.
    public enum PetLife { None, Alive, Dead, Stowed }

    // 사건 순간의 관측(게임 쪽이 채운다). GolemId == null = 펫 칸에 펫 없음. MaterialKey = Materials.CurrentOfOrNull(0·2개면 null).
    // Fusion = 그 순간의 합체 상태(도발 기대 계산 입력 — T3 는 FusionState.Idle 고정, T5 가 실제 값). GolemLife = 펫 생사(펫 없음 = None, 있음 = Alive/Dead — 짝이 어긋나면 throw).
    public sealed class Obs
    {
        public readonly string GolemId;
        public readonly bool GolemIsOurs;
        public readonly int GolemLevel, MasterLevel;
        public readonly string MaterialKey;
        public readonly bool Conflict;
        public readonly FusionState Fusion;
        public readonly PetLife GolemLife;
        public Obs(string golemId, bool golemIsOurs, int golemLevel, int masterLevel, string materialKey, bool conflict, FusionState fusion, PetLife golemLife)
        {
            if (!Enum.IsDefined(typeof(PetLife), golemLife)) throw new ArgumentException("Obs: undefined pet life " + (int)golemLife);
            if ((golemId == null) != (golemLife == PetLife.None)) throw new ArgumentException("Obs: pet life " + golemLife + " with golem id " + (golemId ?? "null") + " (None exactly when no pet is observed)");
            if (golemId != null && golemId.Length == 0) throw new ArgumentException("Obs: empty golem id (use null for 'no pet')");
            if (materialKey != null && materialKey.Length == 0) throw new ArgumentException("Obs: empty material key (use null for 'not exactly one material')");
            if (golemLevel < 0 || masterLevel < 0) throw new ArgumentOutOfRangeException("Obs: negative level (pet " + golemLevel + ", master " + masterLevel + ")");
            if (!Enum.IsDefined(typeof(FusionState), fusion)) throw new ArgumentException("Obs: undefined fusion state " + (int)fusion);
            GolemId = golemId; GolemIsOurs = golemIsOurs; GolemLevel = golemLevel; MasterLevel = masterLevel; MaterialKey = materialKey; Conflict = conflict; Fusion = fusion; GolemLife = golemLife;
        }
        public override string ToString() { return "{" + (GolemId ?? "null") + (GolemIsOurs ? "" : "!ours") + (GolemLife == PetLife.Dead ? ",dead" : "") + "," + GolemLevel + "," + MasterLevel + "," + (MaterialKey ?? "null") + (Conflict ? ",conflict" : "") + "," + Fusion + "}"; }
    }

    public sealed class LifeEvent
    {
        public readonly LifeEventKind Kind;
        public readonly string Id;          // GolemDestroyed·Rebind·ApplyOk
        public readonly int Level;          // ApplyOk
        public readonly string Key;         // ApplyOk
        public readonly bool OldDestroyed;  // Rebind
        public readonly string Reason;      // ApplyFailed
        LifeEvent(LifeEventKind k, string id, int level, string key, bool oldDestroyed, string reason) { Kind = k; Id = id; Level = level; Key = key; OldDestroyed = oldDestroyed; Reason = reason; }

        public static bool IsObservation(LifeEventKind k)
        {
            return k == LifeEventKind.Activated || k == LifeEventKind.AddPet || k == LifeEventKind.PetLevelUp || k == LifeEventKind.MasterLevelUp
                || k == LifeEventKind.AreaDidLoad || k == LifeEventKind.Poll || k == LifeEventKind.RestSucceeded;
        }

        // 관측 사건 7종(인자 없음 — 관측은 Handle 의 Obs).
        public static LifeEvent Of(LifeEventKind k)
        {
            if (!IsObservation(k)) throw new ArgumentException("LifeEvent.Of: " + k + " has its own factory");
            return new LifeEvent(k, null, 0, null, false, null);
        }
        public static LifeEvent Destroyed(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("LifeEvent.Destroyed: empty id");
            return new LifeEvent(LifeEventKind.GolemDestroyed, id, 0, null, false, null);
        }
        public static LifeEvent Rebind(string newId, bool oldDestroyed)
        {
            if (string.IsNullOrEmpty(newId)) throw new ArgumentException("LifeEvent.Rebind: empty id");
            return new LifeEvent(LifeEventKind.Rebind, newId, 0, null, oldDestroyed, null);
        }
        public static LifeEvent ApplyOk(string id, int level, string key)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(key)) throw new ArgumentException("LifeEvent.ApplyOk: empty id/key");
            return new LifeEvent(LifeEventKind.ApplyOk, id, level, key, false, null);
        }
        public static LifeEvent ApplyFailed(string reason)
        {
            if (string.IsNullOrEmpty(reason)) throw new ArgumentException("LifeEvent.ApplyFailed: empty reason");
            return new LifeEvent(LifeEventKind.ApplyFailed, null, 0, null, false, reason);
        }
        public override string ToString() { return Kind + (Id != null ? "(" + Id + (Kind == LifeEventKind.Rebind ? "," + OldDestroyed : Kind == LifeEventKind.ApplyOk ? "," + Level + "," + Key : "") + ")" : Reason != null ? "(" + Reason + ")" : ""); }
    }

    public sealed class Cmd
    {
        public readonly CmdKind Kind;
        public readonly string Reason;        // Wait
        public readonly string Id;            // Bind·Apply
        public readonly int Level;            // Apply
        public readonly string Key;           // Apply
        public readonly TauntSource Taunt;    // Apply — 적용 뒤 상태(Ready) 기준 기대, 코어가 계산(R3-02)
        public readonly ReportLevel Severity; // Report
        public readonly string Text;          // Report
        Cmd(CmdKind k, string reason, string id, int level, string key, TauntSource taunt, ReportLevel sev, string text)
        {
            Kind = k; Reason = reason; Id = id; Level = level; Key = key; Taunt = taunt; Severity = sev; Text = text;
        }
        internal static Cmd Wait(string reason) { return new Cmd(CmdKind.Wait, reason, null, 0, null, TauntSource.None, ReportLevel.Warn, null); }
        internal static readonly Cmd IgnoreCmd = new Cmd(CmdKind.Ignore, null, null, 0, null, TauntSource.None, ReportLevel.Warn, null);
        internal static Cmd Bind(string id) { return new Cmd(CmdKind.Bind, null, id, 0, null, TauntSource.None, ReportLevel.Warn, null); }
        internal static Cmd Apply(string id, int level, string key, TauntSource taunt) { return new Cmd(CmdKind.Apply, null, id, level, key, taunt, ReportLevel.Warn, null); }
        internal static Cmd Report(ReportLevel sev, string text) { return new Cmd(CmdKind.Report, null, null, 0, null, TauntSource.None, sev, text); }
        public override string ToString()
        {
            switch (Kind)
            {
                case CmdKind.Wait: return "Wait(" + Reason + ")";
                case CmdKind.Bind: return "Bind(" + Id + ")";
                case CmdKind.Apply: return "Apply(" + Id + "," + Level + "," + Key + ",taunt " + Taunt + ")";
                case CmdKind.Report: return Severity.ToString().ToUpperInvariant() + "(" + Text + ")";
                default: return Kind.ToString();
            }
        }
    }

    public sealed class AppliedRecord
    {
        public readonly string Id, Key;
        public readonly int Level;
        public AppliedRecord(string id, int level, string key) { Id = id; Level = level; Key = key; }
    }

    // Apply 트랜잭션의 게임 쪽 단계(T3 GolemMaster 가 구현 — TGolem = 게임 유닛 형식). 어느 단계든 실패는 throw.
    public interface IApplyEffector<TGolem>
    {
        TGolem GolemOf(string id);                                             // ① 주인 펫 중 이 ID 의 골렘(없으면 throw)
        void Materials(TGolem golem, int level, string key);                  // ② 재질 버프·수치 특성 교정(T4 Materials.Reconcile; T3 는 「정당한 무동작」 이음매)
        void Taunt(TGolem golem, TauntSource expected);                       // ③ 도발 교정 — 기대 = 인자(현재 Life.State 로 다시 계산하지 않는다)
        void Readback(TGolem golem, int level, string key, TauntSource expected); // ④ 되읽기(어긋나면 throw)
    }

    public sealed class GolemLifecycle
    {
        public LifecycleState State { get; private set; }
        public string BoundId { get; private set; }          // Bound{Id} — null = 미결속
        public AppliedRecord Applied { get; private set; }    // Applied{Id, Level, Key} — null = 적용 기록 없음
        public bool NotReadyReported { get; private set; }    // AreaDidLoad 영구 미준비 ERROR 를 이미 냈는가(1회)
        public Cmd InFlight { get; private set; }             // 발행했고 결과(ApplyOk/ApplyFailed)가 아직 오지 않은 Apply

        GolemLifecycle(LifecycleState s, string bound) { State = s; BoundId = bound; }

        public static GolemLifecycle New() { return new GolemLifecycle(LifecycleState.Unbound, null); }

        // 저장된 GolemId 로 복원: null → Unbound, 아니면 BoundPending(같은 ID 검증 뒤 Apply — 최초 결속과 같은 규칙, R3-01).
        public static GolemLifecycle Restore(string savedGolemId)
        {
            if (savedGolemId != null && savedGolemId.Length == 0) throw new ArgumentException("GolemLifecycle.Restore: empty saved id (use null for 'never bound')");
            return savedGolemId == null ? New() : new GolemLifecycle(LifecycleState.BoundPending, savedGolemId);
        }

        // 어댑터가 관측 Handle 뒤마다 부른다. newId = 그 관측의 펫 ID(펫이 관측될 때만 부른다 — null·빈 ID 는 throw).
        public static RebindDecision DecideRebind(LifecycleState state, string boundId, string newId, bool newIsOurs, bool oldDestroyed)
        {
            if (!Enum.IsDefined(typeof(LifecycleState), state)) throw new ArgumentException("GolemLifecycle.DecideRebind: undefined state " + (int)state);
            if (string.IsNullOrEmpty(newId)) throw new ArgumentException("GolemLifecycle.DecideRebind: new pet id missing (call only when a pet is observed)");
            if (boundId != null && boundId.Length == 0) throw new ArgumentException("GolemLifecycle.DecideRebind: empty bound id");
            if (state != LifecycleState.Detached) return RebindDecision.NotApplicable;
            if (boundId == null) throw new ArgumentException("GolemLifecycle.DecideRebind: Detached without a bound id (impossible state)");
            if (newId == boundId || !newIsOurs) return RebindDecision.Ignore;
            if (!oldDestroyed) return RebindDecision.ErrorOldAlive;
            return RebindDecision.Rebind;
        }

        // 옛 골렘 파괴 판정(T3 fix 1, 원장 판정 I3): 두 유닛 풀(Game.Instance.State.Units·ExtraUnits)의 (UniqueId, Destroyed) 에서 oldId 를 찾는다.
        //   어느 풀이든 살아 있는(Destroyed=false) 항목이 있으면 false(생존 — 우선: 신원 불일치 ERROR 경로로 간다), 있는 항목이 모두 Destroyed 면 true.
        //   두 풀 어디에도 없으면 true(명시 결정): 파괴가 끝난 개체는 풀에서 빠진다 — EntityDestructionController.Tick @007E SceneEntitiesState.RemoveEntityData
        //   → EntityPool.HandleRemoved @001B List.Remove(dnlib 2026-11-09). 빈 ID·null 풀·null ID 항목은 throw.
        public static bool OldGolemDestroyedIn(IEnumerable<KeyValuePair<string, bool>> units, IEnumerable<KeyValuePair<string, bool>> extraUnits, string oldId)
        {
            if (units == null) throw new ArgumentNullException("units");
            if (extraUnits == null) throw new ArgumentNullException("extraUnits");
            if (string.IsNullOrEmpty(oldId)) throw new ArgumentException("GolemLifecycle.OldGolemDestroyedIn: old golem id missing");
            foreach (var pool in new[] { units, extraUnits })
                foreach (var kv in pool)
                {
                    if (kv.Key == null) throw new ArgumentException("GolemLifecycle.OldGolemDestroyedIn: unit without UniqueId in a pool");
                    if (kv.Key != oldId) continue;
                    if (!kv.Value) return false;
                }
            return true; // 있는 항목이 모두 Destroyed, 또는 두 풀 어디에도 없음(위 근거)
        }

        // Tick 폴링 대상(Ready·Detached·Conflict 는 비용 0).
        public bool NeedsPoll { get { return State == LifecycleState.Unbound || State == LifecycleState.BoundPending || State == LifecycleState.Stale; } }

        static readonly HashSet<LifeEventKind> BindBoundary = new HashSet<LifeEventKind> { LifeEventKind.Activated, LifeEventKind.AddPet, LifeEventKind.PetLevelUp, LifeEventKind.AreaDidLoad, LifeEventKind.Poll };
        static readonly HashSet<LifeEventKind> StaleBoundary = new HashSet<LifeEventKind> { LifeEventKind.PetLevelUp, LifeEventKind.Poll, LifeEventKind.AreaDidLoad, LifeEventKind.RestSucceeded };

        static Cmd[] One(Cmd c) { return new[] { c }; }

        public Cmd[] Handle(LifeEvent e, Obs obs)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (LifeEvent.IsObservation(e.Kind))
            {
                if (obs == null) throw new ArgumentNullException("obs", "GolemLifecycle.Handle: " + e.Kind + " needs the observation of that moment");
                return Observe(e.Kind, obs);
            }
            if (obs != null) throw new ArgumentException("GolemLifecycle.Handle: " + e.Kind + " takes no observation (pass null)");
            switch (e.Kind)
            {
                case LifeEventKind.GolemDestroyed:
                    return One(Cmd.IgnoreCmd); // 「정당한 무동작」: 신원 변화는 다음 관측(규칙 ①·④)과 DecideRebind 가 다룬다
                case LifeEventKind.Rebind: return Rebind(e);
                case LifeEventKind.ApplyOk: return ApplyOk(e);
                case LifeEventKind.ApplyFailed: return ApplyFailed(e);
            }
            throw new ArgumentException("GolemLifecycle.Handle: undefined event " + (int)e.Kind);
        }

        // 검증(②③): 펫 있음 · 우리 것 · 펫 레벨 == 주인 레벨 ≥ 1 · 재질 키 정확히 1. 통과면 null, 아니면 대기 사유.
        public const string DeadWait = "golem dead — waits for rest";
        public const string StowedWait = "golem stowed — waits for call";

        static string WhyNotReady(Obs o)
        {
            if (o.GolemId == null) return "no golem in the pet slot";
            if (!o.GolemIsOurs) return "pet is not our golem";
            if (o.GolemLife == PetLife.Dead) return DeadWait;
            if (o.GolemLife == PetLife.Stowed) return StowedWait;
            if (o.MasterLevel < 1) return "master level " + o.MasterLevel + " < 1";
            if (o.GolemLevel != o.MasterLevel) return "pet level " + o.GolemLevel + " != master level " + o.MasterLevel;
            if (o.MaterialKey == null) return "material key unknown (not exactly one material)";
            return null;
        }

        Cmd[] Observe(LifeEventKind k, Obs o)
        {
            if (InFlight != null) return One(Cmd.Wait("apply in flight (" + InFlight + ") — " + k + " not evaluated"));
            if (State == LifecycleState.Conflict) return One(Cmd.IgnoreCmd);
            if (o.Conflict)
            {
                var from = State;
                State = LifecycleState.Conflict;
                return One(Cmd.Report(ReportLevel.Error, "golem lifecycle: companion conflict observed on " + k + " in " + from + " — golem management stops (terminal)"));
            }
            if (BoundId != null && o.GolemId != null && o.GolemId != BoundId)
            {
                if (State == LifecycleState.Detached) return One(Cmd.IgnoreCmd);
                var from = State;
                State = LifecycleState.Detached;
                return One(Cmd.Report(ReportLevel.Warn, "golem lifecycle: observed pet " + o.GolemId + " differs from the bound golem " + BoundId + " (" + from + " -> Detached; old golem fate unknown yet)"));
            }
            switch (State)
            {
                case LifecycleState.Unbound:
                {
                    if (o.GolemId != null && !o.GolemIsOurs)
                    {
                        State = LifecycleState.Conflict;
                        return One(Cmd.Report(ReportLevel.Error, "golem lifecycle: pet slot holds " + o.GolemId + " which is not our golem — conflict (terminal)"));
                    }
                    if (!BindBoundary.Contains(k)) return One(Cmd.Wait(k + " is not a bind boundary"));
                    var why = WhyNotReady(o);
                    if (why != null) return NotYet(k, why);
                    BoundId = o.GolemId;
                    State = LifecycleState.BoundPending;
                    // Bind 가 먼저(선례 WSS Fusion.cs:218 — ID 선기록), 도발 기대 = 적용 뒤 상태 Ready 기준(R3-02).
                    var ap = Cmd.Apply(o.GolemId, o.GolemLevel, o.MaterialKey, TauntPlan.Expected(o.Fusion, LifecycleState.Ready));
                    InFlight = ap;
                    return new[] { Cmd.Bind(o.GolemId), ap };
                }
                case LifecycleState.BoundPending:
                {
                    if (o.GolemId != null && !o.GolemIsOurs)
                    {
                        State = LifecycleState.Conflict;
                        return One(Cmd.Report(ReportLevel.Error, "golem lifecycle: bound golem " + BoundId + " is observed as not ours — conflict (terminal)"));
                    }
                    if (!BindBoundary.Contains(k)) return One(Cmd.Wait(k + " is not an apply boundary"));
                    var why = WhyNotReady(o);
                    if (why != null) return NotYet(k, why);
                    // 여기 오면 o.GolemId == BoundId(다른 ID 는 위에서 Detached). Bind 없이 같은 ID 재시도만.
                    var ap = Cmd.Apply(BoundId, o.GolemLevel, o.MaterialKey, TauntPlan.Expected(o.Fusion, LifecycleState.Ready));
                    InFlight = ap;
                    return One(ap);
                }
                case LifecycleState.Ready:
                {
                    if (o.GolemId == null)
                    {
                        State = LifecycleState.Detached;
                        return One(Cmd.Report(ReportLevel.Warn, "golem lifecycle: bound golem " + BoundId + " no longer observed on " + k + " (Ready -> Detached)"));
                    }
                    if (!o.GolemIsOurs)
                    {
                        State = LifecycleState.Conflict;
                        return One(Cmd.Report(ReportLevel.Error, "golem lifecycle: bound golem " + BoundId + " is observed as not ours — conflict (terminal)"));
                    }
                    if (o.GolemLife == PetLife.Dead)
                    {
                        State = LifecycleState.Stale; // ⑦ 사망 관측 → 예약만(Apply 는 되살아난 뒤 안정 경계에서)
                        return One(Cmd.Wait(DeadWait + " (Ready -> Stale on " + k + ")"));
                    }
                    if (o.GolemLevel == o.MasterLevel && o.GolemLevel == Applied.Level && o.MaterialKey == Applied.Key) return One(Cmd.IgnoreCmd);
                    State = LifecycleState.Stale; // 예약만 — 적용은 안정 경계(⑤)에서(R2-02 ④)
                    return One(Cmd.Wait("stale on " + k + ": observed " + o + " vs applied L" + Applied.Level + " " + Applied.Key + " — waiting for a stable boundary"));
                }
                case LifecycleState.Stale:
                {
                    if (o.GolemId == null)
                    {
                        // 적용 대상이 관측되지 않는데 Apply 를 내지 않는다(Ready 와 같은 규칙 — 계획 ⑤ 는 이 경우를 따로 적지 않음, 보고 §5).
                        State = LifecycleState.Detached;
                        return One(Cmd.Report(ReportLevel.Warn, "golem lifecycle: bound golem " + BoundId + " no longer observed on " + k + " (Stale -> Detached)"));
                    }
                    if (!o.GolemIsOurs)
                    {
                        State = LifecycleState.Conflict;
                        return One(Cmd.Report(ReportLevel.Error, "golem lifecycle: bound golem " + BoundId + " is observed as not ours — conflict (terminal)"));
                    }
                    if (!StaleBoundary.Contains(k)) return One(Cmd.Wait(k + " is not a stable boundary"));
                    if (o.GolemLife == PetLife.Dead) return One(Cmd.Wait(DeadWait));
                    if (o.GolemLife == PetLife.Stowed) return One(Cmd.Wait(StowedWait));
                    if (o.MasterLevel < 1 || o.GolemLevel != o.MasterLevel) return One(Cmd.Wait("pet level " + o.GolemLevel + " != master level " + o.MasterLevel));
                    if (o.MaterialKey == null) return One(Cmd.Wait("material key unknown (not exactly one material)"));
                    var ap = Cmd.Apply(BoundId, o.GolemLevel, o.MaterialKey, TauntPlan.Expected(o.Fusion, LifecycleState.Ready));
                    InFlight = ap;
                    return One(ap);
                }
                case LifecycleState.Detached:
                    return One(Cmd.IgnoreCmd);
            }
            throw new InvalidOperationException("GolemLifecycle: undefined state " + (int)State);
        }

        // 대기. AreaDidLoad 뒤에도 미결속/결속 대기이면 영구 미준비 경계 — ERROR 1회(이후 Wait 만).
        Cmd[] NotYet(LifeEventKind k, string why)
        {
            // 부서진(죽은) 골렘은 정상 상태 — 휴식까지 기다린다. 영구 미준비 ERROR 를 내지 않는다(⑦).
            if (why == DeadWait || why == StowedWait) return One(Cmd.Wait(why));
            if (k == LifeEventKind.AreaDidLoad && (State == LifecycleState.Unbound || State == LifecycleState.BoundPending) && !NotReadyReported)
            {
                NotReadyReported = true;
                return new[] { Cmd.Wait(why), Cmd.Report(ReportLevel.Error, "golem lifecycle: still " + State + " after area load (" + why + ") — golem is not ready; keeps waiting (reported once)") };
            }
            return One(Cmd.Wait(why));
        }

        Cmd[] Rebind(LifeEvent e)
        {
            if (State != LifecycleState.Detached) throw new InvalidOperationException("GolemLifecycle: Rebind(" + e.Id + ") is only valid in Detached (state " + State + ")");
            if (e.Id == BoundId) throw new ArgumentException("GolemLifecycle: Rebind to the already bound id " + e.Id);
            if (!e.OldDestroyed)
                return new[] { Cmd.IgnoreCmd, Cmd.Report(ReportLevel.Error, "golem lifecycle: rebind to " + e.Id + " refused — the old golem " + BoundId + " is still alive (identity mismatch)") };
            BoundId = e.Id;
            Applied = null;
            State = LifecycleState.BoundPending;
            return One(Cmd.Wait("rebound to " + e.Id + " — verify and apply on the next event"));
        }

        Cmd[] ApplyOk(LifeEvent e)
        {
            var ap = InFlight;
            if (ap == null) throw new InvalidOperationException("GolemLifecycle: " + e + " with no Apply in flight");
            if (ap.Id != e.Id || ap.Level != e.Level || ap.Key != e.Key) throw new InvalidOperationException("GolemLifecycle: " + e + " does not match the Apply in flight " + ap);
            if (State != LifecycleState.BoundPending && State != LifecycleState.Stale) throw new InvalidOperationException("GolemLifecycle: " + e + " in state " + State);
            InFlight = null;
            Applied = new AppliedRecord(e.Id, e.Level, e.Key);
            State = LifecycleState.Ready;
            return One(Cmd.IgnoreCmd);
        }

        Cmd[] ApplyFailed(LifeEvent e)
        {
            var ap = InFlight;
            if (ap == null) throw new InvalidOperationException("GolemLifecycle: " + e + " with no Apply in flight");
            InFlight = null; // 상태 유지(BoundPending/Stale) — 다음 폴링 경계에서 재시도
            return One(Cmd.Report(ReportLevel.Error, "golem lifecycle: " + ap + " failed in " + State + ": " + e.Reason + " — retry on the next poll"));
        }

        // Apply 트랜잭션(R3-02): 단계 순서 고정, 도발 기대 = 명령 인자(현재 상태로 다시 계산하지 않는다), 전부 성공해야 ApplyOk.
        public Cmd[] Execute<TGolem>(Cmd apply, IApplyEffector<TGolem> fx)
        {
            if (apply == null) throw new ArgumentNullException("apply");
            if (fx == null) throw new ArgumentNullException("fx");
            if (apply.Kind != CmdKind.Apply) throw new ArgumentException("GolemLifecycle.Execute: " + apply + " is not an Apply");
            if (!ReferenceEquals(apply, InFlight)) throw new InvalidOperationException("GolemLifecycle.Execute: " + apply + " is not the Apply in flight (" + (InFlight == null ? "none" : InFlight.ToString()) + ")");
            LifeEvent result;
            try
            {
                var golem = fx.GolemOf(apply.Id);
                fx.Materials(golem, apply.Level, apply.Key);
                fx.Taunt(golem, apply.Taunt);
                fx.Readback(golem, apply.Level, apply.Key, apply.Taunt);
                result = LifeEvent.ApplyOk(apply.Id, apply.Level, apply.Key);
            }
            catch (Exception ex)
            {
                // 실패는 ApplyFailed 사건으로(ERROR 보고 + 상태 유지 + 재시도) — 삼키지 않고 원인 형식·메시지를 보고문에 싣는다.
                result = LifeEvent.ApplyFailed(ex.GetType().Name + ": " + ex.Message);
            }
            return Handle(result, null);
        }
    }
}
