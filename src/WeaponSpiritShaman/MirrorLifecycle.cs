using System;
using System.Collections.Generic;

namespace WeaponSpiritShaman
{
     
    // 사실 활성(Body·SetMaster 전, 정찰 r1 §2.1)에 계산하지 않기 위한 문지기: 주인 id 와 몸 준비가 함께 관측되는 첫 사건에서 Ready + 재계산 1회.
    // 2026-11-18 주인 상실 수리(소유자 결정): 확정 상실 → 정리 상태(Detached) + 경고 1회 + 이후 사건 무시(센다), 다시 연결되면 Ready 로 돌아간다.

    public enum CoreKind { FactActivated, MasterLinked, PostLoad, AreaDidLoad, Recompute }

    // 펫의 주인 연결을 엔진에서 읽은 결과(LifecycleAdapter.ClassifyLink 가 정한다). 「주인 없음」을 null 하나로 뭉치지 않는다(C1):
    //  Linked     = 주인 개체가 해석됨.
    //  NotLinked  = 펫 파트(UnitPartPet) 없음 — 연결 전(스폰 중 SetMaster 전) 또는 RemoveMaster(@001E Remove<UnitPartPet>, 엔진에서 유일한 파트 제거처) 뒤.
    //  Cleared    = 펫 파트는 있는데 연결 id 가 빔 — UnitPartPet.Clear(@0001 initobj m_MasterRef). 호출처 = SetPetAsEx @0019(DetachPet 에튜드) · RemovePet @006C 뿐.
    //  Unresolved = 연결 id 는 있는데 개체를 못 찾음(EntityRef.get_Entity → Proxy 없음/해체) — 확정 상실이 아니다(일시적으로 못 읽음일 수 있다).
    public enum MasterLink { Linked, NotLinked, Cleared, Unresolved }

    // 사건이 싣는 그 순간의 관측값. SyncDone = ProgressionSync 첫 프레임 동기화 완료(T3) — 게임 쪽이 채운다.
    public sealed class Observation
    {
        public readonly MasterLink Link;
        public readonly string MasterId; // Linked 일 때만 non-null
        public readonly bool BodyReady, SyncDone;
        // 저장된 「이 펫은 연결된 적 있음」(UnitPartSpiritMirror.WasLinked) — 로드 직후 파트 없음(NotLinked)을 확정 상실로 읽을 근거(아래 Handle 규칙 2).
        public readonly bool SavedLinked;
        public Observation(MasterLink link, string masterId, bool bodyReady, bool syncDone, bool savedLinked)
        {
            LinkContract.Check(link, masterId, "Observation");
            Link = link; MasterId = masterId; BodyReady = bodyReady; SyncDone = syncDone; SavedLinked = savedLinked;
        }
    }

    // 관측 계약 한 곳(Observation·Snapshot 이 같이 쓴다, C4): Linked ⇔ 주인 id 있음. 정의되지 않은 값 = throw.
    public static class LinkContract
    {
        public static void Check(MasterLink link, string masterId, string who)
        {
            if (!Enum.IsDefined(typeof(MasterLink), link)) throw new ArgumentException(who + ": undefined MasterLink " + (int)link);
            if ((link == MasterLink.Linked) != (masterId != null))
                throw new ArgumentException(who + ": link " + link + (masterId == null ? " without a master id" : " must not carry a master id (" + masterId + ")"));
        }
    }

    public sealed class CoreEvent
    {
        public readonly CoreKind Kind;
        public readonly Observation Obs;
        public CoreEvent(CoreKind kind, Observation obs)
        {
            if (obs == null) throw new ArgumentNullException("obs", "CoreEvent: every event carries an observation (R2-01)");
            Kind = kind; Obs = obs;
        }
    }

    // 한 사건 처리가 남기는 로그 1줄(게임 쪽이 TakeNotice 로 꺼내 남긴다). Level = "WARN" | "INFO".
    public sealed class LifeNotice
    {
        public readonly string Level, Text;
        public LifeNotice(string level, string text) { Level = level; Text = text; }
    }

    public enum LifeState { Pending, Ready, Detached }

    public sealed class MirrorLifecycle
    {
        public LifeState State { get; private set; }
        public string MasterId { get; private set; }
        public bool BodyReady { get; private set; }
        // Pending 중 무시한 사건 수(사유별) — S5 `mirror-ready` INFO 용.
        public readonly Dictionary<string, int> IgnoredByReason = new Dictionary<string, int>();
        public int Recomputes { get; private set; }
        // 정리 상태(Detached)에서 무시한 사건 수 — 이번 회차(전이 사건 자체는 세지 않는다). 재부착 때 INFO 1줄로 남기고 0 으로.
        public int IgnoredWhileDetached { get; private set; }
        // 확정 상실 횟수(이 생명주기, 즉 펫 1마리 · 로드 1회 동안).
        public int Detaches { get; private set; }
        // Ready 중 주인 연결을 못 읽어(Unresolved) 재계산하지 않은 사건 수 — 이번 회차. 다시 풀리면 INFO 1줄로 남기고 0 으로.
        public int UnresolvedIgnored { get; private set; }
        public string LastMasterId { get; private set; } // 마지막으로 해석된 주인(경고 문구용)
        bool warnedLost, warnedUnresolved;
        LifeNotice notice;

        public MirrorLifecycle() { State = LifeState.Pending; }

        // 마지막 Handle 이 남긴 통지를 꺼낸다(한 번만). 없으면 null.
        // 이 생명주기에서 Ready 에 든 적 있음(= 주인 해석 + 몸 준비 + 동기화가 함께 관측됨). 저장 근거 WasLinked 의 출처.
        public bool ReachedReady { get; private set; }

        // 저장할 연결 근거 = 저장값 || 이번에 Ready 에 든 적 있음. 한 번 참이면 지우지 않는다(상실 뒤에도 「연결된 적 있음」은 사실).
        public static bool NextWasLinked(bool saved, MirrorLifecycle life)
        {
            if (life == null) throw new ArgumentNullException("life");
            return saved || life.ReachedReady;
        }

        public LifeNotice TakeNotice() { var n = notice; notice = null; return n; }

        // 사건 하나 처리 → 재계산 여부. 규칙(계획 T2 + 2026-11-18):
        //  1. 먼저 관측값 갱신(모든 사건).
        //  2. Pending 의 AreaDidLoad 인데 연결이 Unresolved, 또는 NotLinked 이면서 저장된 연결 근거(SavedLinked) 없음 → throw(손상, 이전 규칙 유지).
        //     NotLinked + SavedLinked → 저장된 확정 상실(연결된 적 있는 펫에서 UnitPartPet 이 사라지는 곳 = RemoveMaster @001E 뿐) → Detached(규칙 3 과 같은 경고 계약).
        //  3. 확정 상실(NotLinked·Cleared)이 Ready 에서, 또는 Cleared 가 Pending 에서 관측되면 → Detached(첫 회 WARN, 이후 회차 INFO). 재계산 안 함.
        //     (Pending 의 NotLinked 는 연결 전이라 정상 — 아래 5 의 무시 사유.)
        //  4. Ready: Linked → 재계산(멱등, MirrorRun). Unresolved → 재계산 안 함 · 센다 · 펫당 WARN 1회(상실로 읽지 않는다).
        //  5. Pending: Linked && 몸 준비 → (동기화 미완이면 throw) Ready + 재계산 1회; 아니면 무시하고 사유를 센다.
        //  6. Detached: Linked && 몸 준비 → 재부착(동기화 미완이면 throw) Ready + 재계산 + INFO 1줄(무시 건수); 아니면 무시하고 센다.
        public bool Handle(CoreEvent e)
        {
            if (e == null) throw new ArgumentNullException("e");
            var link = e.Obs.Link;
            MasterId = e.Obs.MasterId;
            BodyReady = e.Obs.BodyReady;
            if (link == MasterLink.Linked) LastMasterId = MasterId;
            if (e.Kind == CoreKind.AreaDidLoad && State == LifeState.Pending && (link == MasterLink.Unresolved || (link == MasterLink.NotLinked && !e.Obs.SavedLinked)))
                throw new InvalidOperationException("MirrorLifecycle: area loaded but the spirit has no master (link " + link + ", saved link evidence " + e.Obs.SavedLinked + ", state " + State + ")");
            if (e.Kind == CoreKind.AreaDidLoad && State == LifeState.Pending && link == MasterLink.NotLinked)
            {
                Detach(link, e.Kind, "saved loss: linked before, no UnitPartPet after load");
                return false;
            }
            switch (State)
            {
                case LifeState.Ready:
                    if (link == MasterLink.Linked)
                    {
                        if (UnresolvedIgnored > 0)
                        {
                            notice = new LifeNotice("INFO", "master link of the spirit resolved again (master " + MasterId + ") after " + UnresolvedIgnored + " ignored event(s); mirror recomputes");
                            UnresolvedIgnored = 0;
                        }
                        Recomputes++;
                        return true;
                    }
                    if (link == MasterLink.Unresolved)
                    {
                        UnresolvedIgnored++;
                        if (!warnedUnresolved)
                        {
                            warnedUnresolved = true;
                            notice = new LifeNotice("WARN", "master link of the spirit is Unresolved (id set, master " + (LastMasterId ?? "?") + " not found) at event " + e.Kind
                                + "; not treated as lost: mirror skips recomputes and counts them until the link resolves (warned once per spirit)");
                        }
                        return false;
                    }
                    Detach(link, e.Kind, null);
                    return false;
                case LifeState.Pending:
                    if (link == MasterLink.Cleared) { Detach(link, e.Kind, null); return false; }
                    if (link == MasterLink.Linked && BodyReady)
                    {
                        RequireSync(e, "Ready reached");
                        State = LifeState.Ready;
                        ReachedReady = true;
                        Recomputes++;
                        return true;
                    }
                    string reason = link == MasterLink.Linked ? "body-not-ready" : link == MasterLink.Unresolved ? "master-unresolved" : (BodyReady ? "no-master" : "no-master-no-body");
                    int n;
                    IgnoredByReason.TryGetValue(reason, out n);
                    IgnoredByReason[reason] = n + 1;
                    return false;
                case LifeState.Detached:
                    if (link == MasterLink.Linked && BodyReady)
                    {
                        RequireSync(e, "re-attach");
                        notice = new LifeNotice("INFO", "spirit re-attached to master " + MasterId + " at event " + e.Kind + " after " + IgnoredWhileDetached + " ignored event(s) (loss #" + Detaches + "); mirror recomputes");
                        IgnoredWhileDetached = 0;
                        State = LifeState.Ready;
                        ReachedReady = true;
                        Recomputes++;
                        return true;
                    }
                    IgnoredWhileDetached++;
                    return false;
                default:
                    throw new InvalidOperationException("MirrorLifecycle: undefined state " + (int)State);
            }
        }

        void RequireSync(CoreEvent e, string what)
        {
            if (!e.Obs.SyncDone) throw new InvalidOperationException("MirrorLifecycle: " + what + " before ProgressionSync finished (pet must not be used unsynced)");
        }

        void Detach(MasterLink link, CoreKind kind, string why)
        {
            Detaches++;
            IgnoredWhileDetached = 0;
            UnresolvedIgnored = 0;
            string text = "spirit lost its master (link " + link + ", last master " + (LastMasterId ?? "none") + ", event " + kind + ", state " + State + ", loss #" + Detaches + (why == null ? "" : ", " + why)
                + "); mirror stops and ignores later events (counted) until the spirit is re-attached";
            notice = new LifeNotice(warnedLost ? "INFO" : "WARN", text + (warnedLost ? "" : " (warned once per spirit)"));
            warnedLost = true;
            State = LifeState.Detached;
        }
    }
}
