using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components;
using UnityEngine;

namespace PhantomBlade
{
     
    // 엔진 사실(Assembly-CSharp IL, 시험 CheckGhostSlashVeil 이 매 실행 다시 본다):
    //   - 발사: ProjectileController.AddProjectile 이 IProjectileLaunchedHandler 를 올린다(전역 이벤트) — Harmony 없이 투사체 객체를 받는 지점.
    //   - 날아가는 모습 = Projectile.View(GameObjectsPool.Claim 으로 가져온 풀 객체). 명중 효과 = BlueprintProjectile.ProjectileHit.HitFx 를 HitPlayer.PlayProjectileHit →
    //     PlayHits → FxHelper.SpawnFxOnUnit 이 따로 만든다(View 를 읽지 않는다) → View 의 렌더러를 꺼도 명중 효과는 그대로 나온다. 빗나감 FX(MissFx)도 같은 경로.
    //     시전 순간 손 위치의 CastFx(CreateView 가 FxHelper.SpawnFxOnPoint 로 따로 만듦)도 View 가 아니라서 그대로 보인다(이번 범위 밖 — 보고 항목).
    //   - 굴림: AbilityDeliverProjectile 은 Launch(→ 발사 이벤트) 뒤에 굴리고 Projectile.AttackRoll 을 단다(<Deliver>d__36 Launch < set_AttackRoll) → 발사 이벤트 안에서는
    //     「귀신참이 쏜 것인가」를 아직 알 수 없다. 그래서 2단계: 발사 때는 후보(이 주인 ∧ 귀신참이 쓰는 투사체 블루프린트)에만 가림 컴포넌트를 붙이고,
    //     그 컴포넌트가 같은 프레임의 첫 Update/LateUpdate 에서 굴림의 원인 능력으로 판정한다(같은 블루프린트를 본편 쇠약 광선도 쓴다).
    //   - 풀링: 끝난 투사체의 View 는 GameObjectsPool.Release(SetActive(false) + 대기열)로 돌아가 다른 능력의 투사체로 다시 쓰인다. Claim 은 렌더러를 건드리지 않는다
    //     → 꺼 둔 렌더러는 「우리가」 되켜야 한다. 복원 지점 = 가림 컴포넌트의 OnDisable(Release 의 SetActive(false)·객체 파괴 모두 여기로 온다).
    // 공유 자산 무수정: 블루프린트·프리팹·재질(sharedMaterial)은 건드리지 않는다 — 풀에서 나온 그 인스턴스의 Renderer.enabled 만 끄고 되켠다.
    // 실패는 눈에 드러나게: 판정 불가·렌더러 없음·예외 → SpiritVisualErrors.ReportOnce 후 투사체는 보이는 채로 둔다(게임 진행을 막지 않는다).
    // 끄기 = SpiritRules.Dials.HideGhostSlashProjectileFlight = false 한 줄.

    // StandBuff 컴포넌트(모든 귀검사가 공유 — 인스턴스 필드 금지). 전역 발사 이벤트라 게임 안 모든 투사체가 오고, 발사자 = Owner 인 것만 본다.
    public class GhostSlashProjectileVeil : UnitFactComponentDelegate, IProjectileLaunchedHandler, IAreaHandler
    {
         
        //   숨긴 수·복원한 수의 단순 비교는 쓰지 않는다: 명중 뒤 15초(LifetimeParticlesAfterHit) 동안은 View 가 살아 있어 정상적으로도 둘이 다르다.
        protected override void OnTurnOff() { SweepOrphans("the stand was turned off"); }
        // 정당한 무동작: 내리는 중에는 투사체 View 가 아직 파괴되는 중이다(ProjectileController.Clear) — 로드가 끝난 뒤에 본다.
        public void OnAreaBeginUnloading() { }
        public void OnAreaDidLoad() { SweepOrphans("an area was loaded"); }

        void SweepOrphans(string when)
        {
            string who = "?";
            try
            {
                var owner = Owner;
                if ((object)owner != null) who = owner.CharacterName;
                GhostSlashVeilLedger.SweepOrphans(when, who);
            }
            catch (Exception ex)
            {
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeilLeftover, who + "|sweep-failed", "ghost slash veil leftover sweep failed (" + when + ", " + who + "): " + ex.Message);
            }
        }

        public void HandleProjectileLaunched(Projectile projectile)
        {
            UnitEntityData owner = null;
            try
            {
                owner = Owner;
                if ((object)owner == null) throw new InvalidOperationException("no Owner in the component event context");
                if (projectile == null) throw new ArgumentNullException("projectile");
                var blueprint = projectile.Blueprint;
                if (blueprint == null) throw new InvalidOperationException("launched projectile has no blueprint");
                bool ours = GhostSlashVeilLedger.IsVeilBlueprint(blueprint);
                 
                //   가드가 실패해도 아래 정상 흐름은 계속된다(GuardLaunchedView 가 스스로 잡아 보고). 전역 구독자(GhostSlashVeilGlobalGuard)가 먼저 불렸으면 여기서는 Skip 이다.
                if (ours) GhostSlashVeilLedger.GuardLaunchedView(projectile);
                // 발사자: TargetWrapper 가 없으면 읽을 수 없다(Unknown). 있으면 그 유닛이 이 주인인가(지점에서 쏜 것은 Unit == null → 아님).
                var launcher = projectile.Launcher;
                var isOwner = launcher == null ? SpiritRules.Tri.Unknown : (ReferenceEquals(launcher.Unit, owner) ? SpiritRules.Tri.Yes : SpiritRules.Tri.No);
                var candidate = SpiritRules.VeilCandidateFor(SpiritRules.Dials.HideGhostSlashProjectileFlight, ours, isOwner);
                if (candidate == SpiritRules.VeilCandidate.No) return; // 정당한 무동작: 다이얼 꺼짐·다른 블루프린트·남의 투사체
                if (candidate == SpiritRules.VeilCandidate.Unknown)
                {
                    GhostSlashFlightVeil.CountUnknown();
                    SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeil, owner.CharacterName + "|launcher", "a " + blueprint.name + " projectile was launched without a readable launcher — cannot tell whether it is "
                        + owner.CharacterName + "'s ghost slash; it stays visible (counted every time, reported once)");
                    return;
                }
                if (candidate != SpiritRules.VeilCandidate.Candidate) throw new InvalidOperationException("unknown veil candidate " + candidate);
                var view = projectile.View;
                if (view == null) throw new InvalidOperationException("the projectile has no View object (nothing to hide)");
                view.AddComponent<GhostSlashFlightVeil>().Init(projectile, owner.CharacterName);
            }
            catch (Exception ex)
            {
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeil, ((object)owner == null ? "?" : owner.CharacterName) + "|launch-failed",
                    "ghost slash projectile veil failed at launch for " + ((object)owner == null ? "?" : owner.CharacterName) + " — the projectile stays visible: " + ex.Message);
            }
        }
    }

    // 풀에서 나온 투사체 View 객체에 붙는 가림 컴포넌트(한 번 쓰고 스스로 떨어진다). 상태는 이 컴포넌트뿐 — 세이브 없음.
    //   첫 Update/LateUpdate: 판정(Judge). GhostSlash → 켜져 있던 렌더러를 끄고 목록을 기억(Hide) → 그 뒤 매 프레임 다시 켜진 것이 있으면 다시 끈다(Enforce — 엔진의 RayView 등이 켤 수 있다).
    //   Other → 아무것도 안 하고 떨어진다. Unknown → 보고 후 떨어진다(보이는 채로).
    //   OnDisable(풀 반납·파괴·컴포넌트 제거): 우리가 끈 렌더러를 전부 되켠다(RestoreNow) — 다시 쓰이는 View 가 꺼진 채 남지 않게.
    public class GhostSlashFlightVeil : MonoBehaviour
    {
        Projectile projectile;
        string who;
        bool judged, restored;
        List<Renderer> hidden; // 우리가 끈 렌더러(끄기 전에 켜져 있던 것만). null = 끈 것 없음.

        static int hiddenCount, restoredCount, otherCount, unknownCount, reDisabledCount;
        // 점검·눈 점검용 읽기 전용(프로세스 누적): 숨긴 투사체 수, 복원한 수(둘은 끝나면 같아야 한다), 귀신참이 아니라 그대로 둔 수, 판정 불가 수, 비행 중 다시 켜져서 다시 끈 횟수.
        public static int HiddenCount { get { return hiddenCount; } }
        public static int RestoredCount { get { return restoredCount; } }
        public static int OtherCount { get { return otherCount; } }
        public static int UnknownCount { get { return unknownCount; } }
        public static int ReDisabledCount { get { return reDisabledCount; } }
        internal static void CountUnknown() { unknownCount++; }

        public void Init(Projectile p, string ownerName)
        {
            if (p == null) throw new ArgumentNullException("p");
            if (projectile != null) throw new InvalidOperationException("PhantomBlade: GhostSlashFlightVeil.Init called twice");
            projectile = p;
            who = ownerName ?? "?";
        }

        void Update() { Step(); }
        void LateUpdate() { Step(); }

        void Step()
        {
            if (restored) return; // 정당한 무동작: 이미 끝났다(떨어지는 중)
            try
            {
                if (!judged)
                {
                    judged = true;
                    Judge();
                }
                else if (hidden != null) Enforce();
            }
            catch (Exception e)
            {
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeil, who + "|failed", "ghost slash projectile veil failed for " + who + " — the projectile is shown again: " + e.Message);
                RestoreNow();
            }
        }

        // 이 투사체가 귀신참이 쏜 것인가(3 상태, 순수 SpiritRules.ProjectileOriginFor). 근거 = 엔진이 발사 직후 단 명중 굴림의 원인 능력
        //   (굴림의 Reason.Context.SourceAbility — StandAttackStatScope.IsGhostSlash 와 같은 읽기). 굴림이 없거나 원인 능력을 못 읽으면 Unknown: 보고 후 보이는 채로.
        void Judge()
        {
            if (projectile == null) throw new InvalidOperationException("Judge before Init");
            var ability = StandBlueprints.GhostSlashAbility;
            if (ability == null) throw new InvalidOperationException("StandBlueprints.GhostSlashAbility is null (blueprints not built)");
            var roll = projectile.AttackRoll;
            var reason = roll == null ? null : roll.Reason;
            var context = reason == null ? null : reason.Context;
            var source = context == null ? null : context.SourceAbility;
            var origin = SpiritRules.ProjectileOriginFor(roll != null, source != null, source != null && ReferenceEquals(source, ability));
            switch (origin)
            {
                case SpiritRules.ProjectileOrigin.GhostSlash:
                    Hide();
                    return;
                case SpiritRules.ProjectileOrigin.Other:
                    otherCount++; // 정당: 같은 블루프린트의 다른 능력(본편 쇠약 광선 등) — 그대로 보인다
                    RestoreNow();
                    return;
                case SpiritRules.ProjectileOrigin.Unknown:
                    unknownCount++;
                    SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeil, who + "|unknown", "cannot tell whether a projectile launched by " + who + " is a ghost slash ("
                        + (roll == null ? "no attack roll on the projectile" : "the roll has no source ability") + ") — it stays visible (counted every time, reported once)");
                    RestoreNow();
                    return;
                default: throw new InvalidOperationException("unknown projectile origin " + origin);
            }
        }

        // 이 View 인스턴스 아래의 켜진 렌더러를 끈다(입자·궤적·선·메시 — 모두 Renderer). 끈 것만 기억한다(원래 꺼져 있던 것은 복원 때도 건드리지 않는다).
        void Hide()
        {
            var all = GetComponentsInChildren<Renderer>(true);
            var mine = new List<Renderer>();
            foreach (var r in all)
                if (r != null && r.enabled) mine.Add(r);
            // 끄기 전에 장부에 적는다(가드가 「모드가 끈 것」을 알아보는 유일한 근거). 자리가 없으면 숨기지 않는다 — 기록 없이 끈 렌더러는 흔적이 남아도 되켤 수 없다.
            if (mine.Count > 0 && !GhostSlashVeilLedger.TryRecord(mine, this))
            {
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeil, who + "|ledger-full", "ghost slash projectile of " + who + " is not hidden: the record of switched-off renderers is full ("
                    + GhostSlashVeilLedger.Outstanding + " of " + SpiritRules.Dials.VeilLedgerCapacity + ") — earlier projectiles were probably never restored");
                RestoreNow();
                return;
            }
            hidden = mine; // 아래에서 던져도 RestoreNow 가 이 목록으로 되켜고 장부에서 지운다
            foreach (var r in mine) r.enabled = false;
            int still = all.Count(r => r != null && r.enabled);
            var readback = SpiritRules.VeilReadbackFor(all.Length, mine.Count, still);
            if (readback != SpiritRules.VeilReadback.Hidden)
            {
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeil, who + "|" + readback, "ghost slash projectile of " + who + " could not be hidden (" + readback + ": " + all.Length + " renderer(s), "
                    + mine.Count + " switched off, " + still + " still on) — it is shown again");
                RestoreNow();
                return;
            }
            hiddenCount++;
        }

        // 비행 중 엔진 컴포넌트가 렌더러를 다시 켰으면 다시 끈다(횟수는 센다 — 눈 점검에서 깜빡임의 단서).
        void Enforce()
        {
            foreach (var r in hidden)
            {
                if (r == null || !r.enabled) continue;
                r.enabled = false;
                reDisabledCount++;
            }
        }

        // 우리가 끈 렌더러를 되켜고 이 컴포넌트를 뗀다. 여러 번 불려도 한 번만 한다.
        void RestoreNow()
        {
            if (restored) return;
            restored = true;
            var mine = hidden;
            hidden = null;
            if (mine != null)
            {
                foreach (var r in mine)
                {
                    if (r == null) continue; // 정당: 그 렌더러가 이미 파괴됨(되켤 것이 없다)
                    r.enabled = true;
                }
                GhostSlashVeilLedger.Release(mine);
                if (mine.Count > 0) restoredCount++;
            }
            Destroy(this);
        }

        // 가드용: 지금 이 컴포넌트가 렌더러를 숨기는 중인가.
        internal bool IsHiding { get { return !restored && hidden != null; } }

        // 가드(GhostSlashVeilLedger.GuardLaunchedView)가 새로 쓰이는 View 에서 이전 사용의 컴포넌트를 발견했을 때 부른다.
        //   true = 아직 복원되지 않은 채 남아 있었다(흔적 — 지금 복원했다). false = 이미 복원을 마치고 떨어지는 중이었다(Destroy 는 프레임 끝에 처리된다 — 정당).
        internal bool ForceRestoreIfActive()
        {
            if (restored) return false;
            RestoreNow();
            return true;
        }

        void OnDisable() { RestoreNow(); }
    }

     
    //   엔진 근거(IL, 시험이 잠금): EventBus.Subscribe(object) 는 ISubscriptionProxy 가 아닌 일반 객체도 IGlobalSubscriber 면 EventBus.GlobalSubscribers 에 넣는다.
    //   그 목록은 정적 생성자에서 한 번 만들고 다시 대입·비우지 않는다 → 지역 이동·세이브 로드 뒤에도 구독이 남는다(다시 등록할 곳 없음).
    //   선례: 이 저장소 BulwarkMage 의 RebukeOutcomeTracker(일반 객체 EventBus.Subscribe + IsGloballySubscribed 되읽기).
    // 하는 일은 가드뿐이다 — 판정·숨김은 StandBuff 컴포넌트(GhostSlashProjectileVeil) 그대로. 한 투사체에 두 번 돌지 않는다(GuardLaunchedView 의 투사체당 1회 장치 공용).
    public sealed class GhostSlashVeilGlobalGuard : IProjectileLaunchedHandler
    {
        static GhostSlashVeilGlobalGuard instance;

        // 점검·눈 점검용: 등록돼 있는가(엔진에 되물음).
        public static bool Registered { get { return instance != null && EventBus.IsGloballySubscribed(instance); } }

        // Main.Load 가 한 번 부른다. 실패(예외·등록 되읽기 거짓·두 번 호출)는 로드 때 오류 줄로 남기고 던지지 않는다(게임·모드 로드는 계속 —
        //   그 경우 가드는 스탠드를 켠 귀검사가 있을 때만 돈다).
        public static void Register()
        {
            try
            {
                if (instance != null) throw new InvalidOperationException("Register called twice");
                var guard = new GhostSlashVeilGlobalGuard();
                EventBus.Subscribe(guard);
                if (!EventBus.IsGloballySubscribed(guard)) throw new InvalidOperationException("EventBus.IsGloballySubscribed is false right after EventBus.Subscribe");
                instance = guard;
                WotrClassMod.Common.ModContext.Log("PhantomBlade: ghost slash veil leftover guard registered (global projectile-launch subscriber)");
            }
            catch (Exception ex)
            {
                WotrClassMod.Common.ModContext.LogError("PhantomBlade: the ghost slash veil leftover guard could not be registered at load — it will only run while a Phantom Blade's stand is on: " + ex);
            }
        }

        public void HandleProjectileLaunched(Projectile projectile) { GhostSlashVeilLedger.GuardLaunched(projectile); }
    }

     
    //   장부 = 렌더러 인스턴스 ID → (렌더러, 그것을 끈 가림 컴포넌트). Hide 가 끄기 전에 적고(TryRecord), RestoreNow 가 되켠 뒤 지운다(Release) → 정상이면 비어 있다.
    //   상한 = SpiritRules.Dials.VeilLedgerCapacity: 넘으면 TryRecord 가 거절하고 Hide 는 숨기지 않는다(기록이 무한히 쌓이지 않는다). 파괴된 렌더러의 기록은 Prune 이 지운다.
    //   가드가 켜는 것은 「장부에 있는데 주인 없이 꺼져 있는」 렌더러뿐이다(순수 SpiritRules.VeilLeftoverFor). 장부에 없는 꺼진 렌더러 = 프리팹이 원래 꺼 둔 것일 수 있다 → 절대 켜지 않는다.
    // 부르는 곳 둘: 모드 로드 때 등록한 전역 구독자(GhostSlashVeilGlobalGuard — 스탠드와 무관)와 StandBuff 컴포넌트(GhostSlashProjectileVeil — 전역 등록이 실패했을 때의 보루).
    public static class GhostSlashVeilLedger
    {
        sealed class Entry { public Renderer R; public GhostSlashFlightVeil Owner; }

        static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
        // 투사체마다 한 번만 검사한다: 발사 이벤트는 스탠드를 켠 귀검사마다 온다 — 두 번째 처리기가 첫 처리기가 방금 붙인 가림 컴포넌트를 흔적으로 오인하면 안 된다.
        static readonly WotrClassMod.Common.WeakKeyTable<Projectile, object> guarded = new WotrClassMod.Common.WeakKeyTable<Projectile, object>();
        static int leftoverCount;

        // 점검·눈 점검용 읽기 전용: 지금 장부에 남은 기록 수(정상이면 날아가는 중인 것뿐), 흔적을 발견한 횟수(프로세스 누적 — 0 이어야 한다).
        public static int Outstanding { get { return entries.Count; } }
        public static int LeftoverCount { get { return leftoverCount; } }

        // 파괴된 렌더러의 기록을 지운다(Unity 객체는 파괴되면 == null).
        static void Prune()
        {
            var dead = entries.Where(kv => kv.Value.R == null).Select(kv => kv.Key).ToList();
            foreach (int id in dead) entries.Remove(id);
        }

        // 끄기 전에 적는다. 자리가 없으면 false(아무것도 적지 않는다 — 호출자는 숨기지 않는다).
        internal static bool TryRecord(List<Renderer> toHide, GhostSlashFlightVeil owner)
        {
            if (toHide == null) throw new ArgumentNullException("toHide");
            if (owner == null) throw new ArgumentNullException("owner");
            Prune();
            int fresh = toHide.Count(r => !entries.ContainsKey(r.GetInstanceID()));
            if (!SpiritRules.VeilLedgerHasRoom(entries.Count, fresh, SpiritRules.Dials.VeilLedgerCapacity)) return false;
            foreach (var r in toHide) entries[r.GetInstanceID()] = new Entry { R = r, Owner = owner };
            return true;
        }

        // 되켠 렌더러의 기록을 지운다(파괴된 것은 Prune 몫).
        internal static void Release(List<Renderer> restoredRenderers)
        {
            if (restoredRenderers == null) throw new ArgumentNullException("restoredRenderers");
            foreach (var r in restoredRenderers)
                if (r != null) entries.Remove(r.GetInstanceID());
        }

        // 그 기록을 끈 가림 컴포넌트가 아직 정당하게 숨기는 중인가: 컴포넌트가 살아 있고, 복원 전이고, 그 View 가 활성(풀에 반납되지 않음).
        static bool OwnerActive(GhostSlashFlightVeil owner)
        {
            return owner != null && owner.IsHiding && owner.gameObject.activeInHierarchy;
        }

        // 렌더러들을 훑어 흔적을 되켠다. viewJustClaimed = 이 렌더러들은 방금 새 투사체가 가져간 View 의 것이다(이전 사용은 끝났다 → 어떤 기록도 「숨기는 중」일 수 없다).
        //   돌려주는 값 = 되켠 수. notOurs = 꺼져 있지만 장부에 없어 손대지 않은 수(보고 문구용 — 프리팹이 원래 꺼 둔 것일 수 있다).
        static int Sweep(IEnumerable<Renderer> renderers, bool viewJustClaimed, out int notOurs)
        {
            int reenabled = 0;
            notOurs = 0;
            foreach (var r in renderers)
            {
                if (r == null) continue; // 정당: 파괴된 렌더러
                Entry e;
                bool idFound = entries.TryGetValue(r.GetInstanceID(), out e);
                // 인스턴스 ID 가 같아도 기록된 렌더러가 이 객체가 아니면 「장부에 없음」(ID 재사용 대비 — 원래 꺼 둔 렌더러를 켜지 않는다). 그 낡은 기록은 지운다.
                bool inLedger = SpiritRules.VeilLedgerHit(idFound, idFound && ReferenceEquals(e.R, r));
                if (idFound && !inLedger) entries.Remove(r.GetInstanceID());
                bool ownerActive = inLedger && !viewJustClaimed && OwnerActive(e.Owner);
                bool on = r.enabled;
                switch (SpiritRules.VeilLeftoverFor(on, inLedger, ownerActive))
                {
                    case SpiritRules.VeilLeftover.None:
                        if (!on && !inLedger) notOurs++;
                        break;
                    case SpiritRules.VeilLeftover.DropStaleEntry:
                        entries.Remove(r.GetInstanceID());
                        break;
                    case SpiritRules.VeilLeftover.ReenableOrphan:
                        r.enabled = true;
                        entries.Remove(r.GetInstanceID());
                        reenabled++;
                        break;
                    default: throw new InvalidOperationException("unknown veil leftover decision");
                }
            }
            return reenabled;
        }

        // 이 블루프린트가 귀신참이 쓰는 투사체(= 숨김·가드 대상)인가. 판정 한 곳(C4): 살아 있는 귀신참 능력의 AbilityDeliverProjectile.Projectiles — GUID 리터럴을 다시 쓰지 않는다.
        //   블루프린트가 아직 안 만들어졌으면 판정할 수 없다 → 던진다(호출자가 보고).
        internal static bool IsVeilBlueprint(Kingmaker.Blueprints.BlueprintProjectile blueprint)
        {
            if (blueprint == null) throw new ArgumentNullException("blueprint");
            var ability = StandBlueprints.GhostSlashAbility;
            if (ability == null) throw new InvalidOperationException("StandBlueprints.GhostSlashAbility is null (blueprints not built)");
            var deliver = ability.GetComponent<AbilityDeliverProjectile>();
            if (deliver == null) throw new InvalidOperationException("ghost slash ability has no AbilityDeliverProjectile");
            foreach (var p in deliver.Projectiles) if (ReferenceEquals(p, blueprint)) return true;
            return false;
        }

        // 전역 구독자(GhostSlashVeilGlobalGuard)의 입구: 우리 블루프린트의 투사체면 가드. 판정 실패(블루프린트 없음 등)는 가드 실패로 1회 보고하고 삼킨다.
        internal static void GuardLaunched(Projectile projectile)
        {
            try
            {
                if (projectile == null) throw new ArgumentNullException("projectile");
                var blueprint = projectile.Blueprint;
                if (blueprint == null) throw new InvalidOperationException("launched projectile has no blueprint");
                if (!IsVeilBlueprint(blueprint)) return; // 정당한 무동작: 다른 투사체
            }
            catch (Exception ex)
            {
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeilLeftover, "global|judge-failed", "ghost slash veil leftover guard could not judge a launched projectile (it is launched as usual): " + ex.Message);
                return;
            }
            GuardLaunchedView(projectile);
        }

        // 쇠약 광선 투사체가 발사될 때마다(발사자·판정과 무관) 그 View 를 검사한다. 흔적 = 복원되지 않은 가림 컴포넌트, 또는 장부에 있는 꺼진 렌더러.
        //   있으면 되켜고 오류 1회(SiteProjectileVeilLeftover — 숨김 실패 SiteProjectileVeil 과 다른 이름). 가드 자신의 실패도 보고하고 삼킨다(정상 흐름을 막지 않는다).
        internal static void GuardLaunchedView(Projectile projectile)
        {
            string name = "?";
            try
            {
                if (projectile == null) throw new ArgumentNullException("projectile");
                object seen;
                // 정당한 무동작(Skip): 다른 처리기(전역 구독자·다른 귀검사)가 이 투사체를 이미 검사했다 — 그 뒤에 붙었을 수 있는 새 가림 컴포넌트를 흔적으로 보지 않는다.
                if (SpiritRules.VeilGuardStepFor(guarded.TryGetValue(projectile, out seen)) == SpiritRules.VeilGuardStep.Skip) return;
                guarded.Add(projectile, new object());
                if (projectile.Blueprint != null) name = projectile.Blueprint.name;
                var view = projectile.View;
                if (view == null) return; // 정당한 무동작: View 가 없으면 검사할 것이 없다(엔진 AddProjectile 이 그 사실을 이미 오류로 남긴다)
                int unrestored = 0;
                foreach (var old in view.GetComponents<GhostSlashFlightVeil>())
                    if (old != null && old.ForceRestoreIfActive()) unrestored++;
                Prune();
                int notOurs;
                int reenabled = Sweep(view.GetComponentsInChildren<Renderer>(true), true, out notOurs);
                if (!SpiritRules.VeilTraceFound(unrestored, reenabled)) return; // 정당한 무동작: 깨끗하다
                leftoverCount++;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeilLeftover, name + "|launch", "a reused " + name + " projectile view was left hidden by an earlier ghost slash: " + unrestored
                    + " veil component(s) not restored, " + reenabled + " renderer(s) switched back on now; " + notOurs + " switched-off renderer(s) not recorded by this mod were left as they are (counted every time, reported once)");
            }
            catch (Exception ex)
            {
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeilLeftover, name + "|guard-failed", "ghost slash veil leftover guard failed for a " + name + " projectile (it is launched as usual): " + ex.Message);
            }
        }

        // 스탠드 끔·지역 로드 뒤: 장부 전체에서 주인 없이 꺼진 채 남은 렌더러를 되켜고 1회 보고한다. 날아가는 중인 투사체(주인이 숨기는 중)는 건드리지 않는다.
        internal static void SweepOrphans(string when, string who)
        {
            Prune();
            int notOurs;
            int reenabled = Sweep(entries.Values.Select(e => e.R).ToList(), false, out notOurs);
            if (reenabled == 0) return; // 정당한 무동작: 남은 흔적 없음
            leftoverCount++;
            SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteProjectileVeilLeftover, "orphans", reenabled + " renderer(s) hidden by a ghost slash were still off with no veil hiding them when " + when + " (" + who
                + ") — switched back on now (counted every time, reported once)");
        }
    }
}
