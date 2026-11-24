using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.View;
using Kingmaker.UnitLogic.Mechanics;
using UnityEngine;
using ModContext = WotrClassMod.Common.ModContext;

namespace ShadowSorcerer
{
    [HarmonyLib.HarmonyPatch(typeof(Kingmaker.Visual.Animation.Kingmaker.UnitAnimationManager), "Tick")]
    static class MirrorCombatStance
    {
        static void Prefix(Kingmaker.Visual.Animation.Kingmaker.UnitAnimationManager __instance)
            => EchoStage.RefreshCombatStance(__instance);
    }

    // 그림자 메아리 연출의 단계 집행(소유자 결정 2026-11-16 밤 2·4, 정찰 §5·§7-3). 결정은 순수 EchoStageRule / ShadowPlacement 가 하고 여기는 집행만 한다.
    //   등장(원래 주문이 끝난 것을 대기열이 처음 본 틱) → 메아리마다 시전 동작(발사 CastLead 전) → 작업이 끝나면 쏜 주문이 끝나기를 기다렸다가 유지 시간 뒤 함께 숨김.
    // 계약: 공개 진입점은 던지지 않는다. 실패 = 오류 줄 1회 + 그 작업의 연출을 끈다(그림자를 숨긴다) — 메아리는 본체 기준으로 그대로 나간다.
     
    // 정리: 드러난 동안 매 틱 「계속 보여도 되는가」를 다시 본다 — 본체 사망·기절·변신·은신·지역 이탈, 컷신·대화, 전투 종료, 그림자 유닛 소실이면 즉시 숨긴다.
    //       무산 경로(EchoDudLog 의 사유 전부)는 작업 종료로 온다: 남은 그림자는 시전 없이 유지 시간 뒤 숨는다. 중단(Abort)·예외는 즉시 숨긴다. 지역을 다시 불러오면 전부 버린다.
    public static class EchoStage
    {
        sealed class Show
        {
            public EchoJob Job;
            public UnitEntityData Caster;
            public UnitEntityData[] Units;
            public Vector3[] Ground;
            public bool[] StanceDeferred;     // 드러낸 프레임에 애니메이션 관리자가 없어 시전 동작으로 미룬 자세(그림자 자리별)
            public int LastCue;               // Windup 또는 Late 로 처리한 마지막 메아리 번호(EchoStageRule.Cue)
            public readonly HashSet<int> Late = new HashSet<int>(); // 시전 동작이 늦어 연출을 생략한 메아리 번호 — 그 메아리는 묶지 않는다(본체에서 나간다)
            public bool StartedInCombat;
            public double? ClosingAt, QuietAt;
            public bool Off;
            public readonly Dictionary<MechanicsContext, EchoLaunchAnchor> Anchors = new Dictionary<MechanicsContext, EchoLaunchAnchor>();
        }

        static readonly StageIndex<EchoJob, Show> byJob = new StageIndex<EchoJob, Show>();
        static readonly List<Show> active = new List<Show>();

        // UnitAnimationController recalculates this flag every tick. Mirror only currently displayed doubles.
        internal static void RefreshCombatStance(Kingmaker.Visual.Animation.Kingmaker.UnitAnimationManager manager)
        {
            foreach (var show in active)
            {
                if (show.Off) continue;
                foreach (var shadow in show.Units)
                    if (ReferenceEquals(shadow?.View?.AnimationManager, manager))
                    {
                        manager.IsInCombat = show.Caster.IsInCombat;
                        return;
                    }
            }
        }

        // 점검·눈 점검용 계수.
        public static int Appeared { get; private set; }
        public static int Windups { get; private set; }
        public static int Hidden { get; private set; }



        static readonly Dictionary<string, int> skips = new Dictionary<string, int>();




        // 대기열이 작업을 한 걸음 보기 직전에 부른다(EchoQueueCore.Step → EchoQueue.StepHost). 던지지 않는다. 돌려주는 값이 없다 — 대기열 결정에 들어가지 않는다.
        //   등장 = 원래 주문이 끝난 것을 처음 본 틱. 시전 동작 = 발사 예정(EchoQueueCore.DueAt — 대기열 시계 그대로) − 동작 선행.
        //   발사 틱까지 동작을 시작하지 못했으면(Late) 그 메아리의 연출만 생략한다 — 사유 WindupLate 정보 줄, 그 메아리는 본체에서 나간다(ShadowFor 가 null).
        public static void BeforeNext(EchoJob j, double now, bool? originalEnded)
        {
            Show show = null;
            try
            {
                if (j == null) throw new ArgumentNullException("j");
                var st = j.State;
                if (st.Finished) return;
                if (!byJob.TryGetValue(j, out show))
                {
                    // 등장은 한 번: 원래 주문이 끝났는데 대기열이 아직 끝난 시각을 적지 않은 틱(= 처음 본 틱).
                    if (originalEnded != true || st.EndedAt.HasValue) return;
                    TryAppear(j);
                    return;
                }
                if (show.Off) return;
                int index = st.Fired + st.Dropped + 1;
                if (index > show.Units.Length) return;
                var cue = EchoStageRule.Cue(EchoQueueCore.DueAt(st, ShadowRules.EchoIntervalSeconds), now, index, show.LastCue);
                if (cue == StageCue.Windup)
                {
                    show.LastCue = index;
                    Windup(show, index);
                }
                else if (cue == StageCue.Late)
                {
                    show.LastCue = index;
                    show.Late.Add(index);
                    Skip(show.Caster, StageReasons.WindupLate);
                }
            }
            catch (Exception e)
            {
                ShadowErrors.ReportOnce(EchoVisuals.Site, "stage-" + e.GetType().Name, "echo visual stage failed (" + e.GetType().Name + ": " + e.Message + ") - shadows hidden, the echo goes on from the caster");
                if (show != null) TurnOff(show, null);
            }
        }

        // 방금 쏜(쏘려던) 메아리의 결과. Ok = 그 그림자의 손 FX. Ok 가 아니면 그 메아리의 주문은 돌지 않는다 → 흩어짐이 그 주문을 기다리지 않는다. 던지지 않는다.
        public static void AfterFire(EchoJob j, int index, FireOutcome outcome)
        {
            try
            {
                Show show;
                if (j == null || !byJob.TryGetValue(j, out show) || show.Off) return; // 정당한 무동작: 연출 없음
                if (outcome == FireOutcome.Ok)
                {
                    // 발사 성공: 그 그림자의 손에 시전 FX(본체가 시전할 때와 같은 프리팹 — 던지지 않는다). 시전 동작이 늦어 연출을 생략한 메아리(Late)는 손 FX 도 없다.
                    if (index >= 1 && index <= show.Units.Length && !show.Late.Contains(index)) ShadowCast.HandFx(show.Units[index - 1], j.Data);
                    return;
                }
                foreach (var a in show.Anchors.Values) if (a.Slot == index) a.Dud = true;
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "after-fire-" + e.GetType().Name, "echo visual stage: after-fire failed (" + e.GetType().Name + ": " + e.Message + ")"); }
        }

        // 작업이 대기열에서 빠질 때(정상 종료·소멸·중단). immediate = 중단·예외 → 즉시 숨김. 던지지 않는다.
        public static void JobFinished(EchoJob j, double now, bool immediate)
        {
            try
            {
                Show show;
                if (j == null || !byJob.Take(j, out show) || show.Off) return; // 정당한 무동작: 연출 없음(기록은 Take 가 이미 뺐다)
                if (immediate) { TurnOff(show, null); return; }
                if (!show.ClosingAt.HasValue) show.ClosingAt = now;
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "finished-" + e.GetType().Name, "echo visual stage: job-finished failed (" + e.GetType().Name + ": " + e.Message + ")"); }
        }

        // 매 틱(EchoQueue.Tick 맨 앞 — 대기 작업이 없어도 돈다: 흩어짐은 작업이 끝난 뒤에 온다). 던지지 않는다.
        public static void Tick(double now)
        {
            foreach (var show in active.ToArray())
            {
                try
                {
                    if (show.Off) { active.Remove(show); continue; }
                    string reason;
                    var verdict = EchoStageRule.Decide(ReadFacts(show.Caster, show.Units), out reason);
                    if (verdict == StageVerdict.Unknown) { ShadowErrors.ReportOnce(EchoVisuals.Site, "stage-unknown-" + reason, "echo visual stage: cannot read " + reason + " - shadows hidden"); TurnOff(show, null); continue; }
                    if (verdict == StageVerdict.Skip) { TurnOff(show, reason == StageReasons.NoShadowUnits ? StageReasons.ShadowLost : reason); continue; } // 드러난 뒤에 그림자가 없어진 것은 「소실」
                    if (show.StartedInCombat && !Game.Instance.Player.IsInCombat) { TurnOff(show, StageReasons.CombatEnded); continue; }
                    // 자리 유지: 시각 전용 유닛이라 스스로 움직이지 않지만 엔진이 높이·위치를 고칠 수 있다(귀검사 령도 매 프레임 다시 놓는다).
                    for (int k = 0; k < show.Units.Length; k++)
                        if (show.Units[k].Position != show.Ground[k]) show.Units[k].Position = show.Ground[k];
                    if (!show.ClosingAt.HasValue) continue;
                    bool pending = false;
                    foreach (var a in show.Anchors.Values) if (!a.Dud && (a.Process == null || !a.Process.IsEnded)) pending = true;
                    if (EchoStageRule.Close(show.ClosingAt.Value, ref show.QuietAt, now, pending) == CloseStep.Hide) TurnOff(show, null);
                }
                catch (Exception e)
                {
                    ShadowErrors.ReportOnce(EchoVisuals.Site, "stage-tick-" + e.GetType().Name, "echo visual stage tick failed (" + e.GetType().Name + ": " + e.Message + ") - shadows hidden");
                    TurnOff(show, null);
                }
            }
        }

        // 지역을 다시 불러왔을 때(EchoQueue.OnAreaLoad). 그림자 유닛은 지역과 함께 사라진다 — 기록만 버린다. 던지지 않는다.
        public static void DropAll()
        {
            foreach (var show in active) { show.Off = true; show.Anchors.Clear(); }
            active.Clear();
            byJob.Clear();
        }

        // EchoVisuals.Bind 가 묻는다: 이 작업의 index 번째 메아리를 「쏠」 그림자. null = 이 메아리에는 연출이 없다(등장하지 않았거나 꺼졌다 — 사유는 그때 남았다).
        internal static UnitEntityData ShadowFor(EchoJob j, int index)
        {
            Show show;
            if (j == null || !byJob.TryGetValue(j, out show) || show.Off || index < 1 || index > show.Units.Length || show.Late.Contains(index)) return null;
            var u = show.Units[index - 1];
            return ShadowDoubles.Going(u) ? null : u;
        }

        internal static void NoteAnchor(EchoJob j, MechanicsContext ctx, EchoLaunchAnchor anchor)
        {
            Show show;
            if (!byJob.TryGetValue(j, out show) || show.Off)
                throw new InvalidOperationException("cannot bind an echo anchor after its stage closed");
            show.Anchors.Add(ctx, anchor);
        }

        internal static EchoLaunchAnchor AnchorOf(MechanicsContext ctx)
        {
            if (ctx == null) return null;
            foreach (var show in active)
                if (show.Anchors.TryGetValue(ctx, out var anchor)) return anchor;
            return null;
        }

        // EchoVisuals.Bind 가 메아리 규칙 발행(Rulebook.Trigger) 직전에 묻는다(2차 독립 검토 중요-1): 드러나 있는 그림자가 지금 본체·대상 자리 기준으로 본체 자신의 원뿔·선 밖인가.
        //   등장 뒤 본체나 대상이 움직였으면 「뒤」가 아닐 수 있다 → 그러면 이 무대를 지금 끈다(전부 숨김 = 게임 밖 — 엔진 유닛 열거에서 빠진다). false = 껐다(이 메아리는 본체에서 나간다).
        //   무대가 없거나 이미 꺼졌으면 true(지킬 그림자가 없다). 판독 실패는 Allows 가 거짓 → 끈다(조용히 통과시키지 않는다).
        //   원뿔·선이 아니라고 읽힌 메아리(false)는 true — 그 메아리의 대상 수집은 그림자 자리와 무관하다(연쇄·튕김·주변 범위는 앞뒤로 닫히지 않는다 — README 남는 노출). 판독 불가(null)는 원뿔·선처럼 본다.
        internal static bool ClearForFire(EchoJob j, bool? lineOrCone)
        {
            Show show;
            if (j == null || !byJob.TryGetValue(j, out show) || show.Off) return true;
            if (lineOrCone == false) return true;
            var c = show.Caster.Position;
            var t = j.Wrapper.Point;
            for (int k = 0; k < show.Units.Length; k++)
            {
                var u = show.Units[k];
                if (!ShadowDoubles.IsShown(show.Caster, k + 1, u)) continue; // 정당: 숨긴 그림자는 게임 밖이다(유닛 열거에 없다)
                if (ShadowClearance.Allows(ClearanceOf(u, u.Position, c, t), lineOrCone)) continue;
                TurnOff(show, StageReasons.InLineOfFire);
                return false;
            }
            return true;
        }

        // 그림자 하나의 자리 판정. 몸집은 그 유닛에서 되읽는다(엔진 선 판정이 쓰는 UnitEntityData.Corpulence — 뷰가 있으면 원뿔이 쓰는 뷰 값과 같다).
        static ClearanceVerdict ClearanceOf(UnitEntityData shadow, Vector3 at, Vector3 caster, Vector3 target)
        {
            return ShadowClearance.Check(caster.x, caster.z, target.x, target.z, at.x, at.z, shadow.Corpulence, ShadowVisualLimits.ClearanceMarginMeters);
        }

        static StageFacts ReadFacts(UnitEntityData caster, UnitEntityData[] expected)
        {
            var f = new StageFacts();
            // 그림자: 묶음의 k 번 유닛이 쓸 수 있고(expected 가 있으면) 드러낼 때의 그 유닛이다(묶음이 다시 만들어졌으면 다른 객체 → 아니오).
            bool ready = !ShadowDoubles.Going(caster) && ShadowDoubles.Count(caster) >= expected.Length;
            for (int k = 0; ready && k < expected.Length; k++)
            {
                var u = ShadowDoubles.Get(caster, k + 1);
                if ((object)u == null || ((object)expected[k] != null && !ReferenceEquals(u, expected[k]))) ready = false;
            }
            f.ShadowsReady = ready ? Tri.Yes : Tri.No;
            f.CasterInGame = !ShadowDoubles.Going(caster) && caster.IsInGame ? Tri.Yes : Tri.No;
            var st = caster.Descriptor == null ? null : caster.Descriptor.State;
            f.CasterDead = st == null ? Tri.Unknown : st.IsDead ? Tri.Yes : Tri.No;
            f.CasterConscious = st == null ? Tri.Unknown : st.IsConscious ? Tri.Yes : Tri.No;
            f.CasterInStealth = st == null ? Tri.Unknown : st.IsInStealth ? Tri.Yes : Tri.No;
            var body = caster.Body;
            f.CasterPolymorphed = body == null ? Tri.Unknown : body.IsPolymorphed ? Tri.Yes : Tri.No;
            var game = Game.Instance;
            var cutscene = game == null ? null : game.CutsceneLock;
            if (cutscene == null) f.ModeAllows = Tri.Unknown;
            else
            {
                var mode = game.CurrentMode;
                f.ModeAllows = !cutscene.Active && (mode == GameModeType.Default || mode == GameModeType.Pause) ? Tri.Yes : Tri.No;
            }
            return f;
        }

        static bool TryAppear(EchoJob j)
        {
            var caster = j.Caster;
            int n = j.State.Total;
            string reason;
            var verdict = EchoStageRule.Decide(ReadFacts(caster, new UnitEntityData[n]), out reason);
            if (verdict == StageVerdict.Unknown)
            {
                ShadowErrors.ReportOnce(EchoVisuals.Site, "stage-unknown-" + reason, "echo visual stage: cannot read " + reason + " - no shadow shown, the echo goes on from the caster");
                return false;
            }
            if (verdict == StageVerdict.Skip) { Skip(caster, reason); return false; }
            // 한 본체에 한 무대: 앞 작업의 그림자가 아직 흩어지는 중이면 지금 숨기고 새 작업이 쓴다(같은 유닛들이다).
            foreach (var old in active.ToArray()) if (ReferenceEquals(old.Caster, caster)) TurnOff(old, null);

            var show = new Show { Job = j, Caster = caster, Units = new UnitEntityData[n], Ground = new Vector3[n], StanceDeferred = new bool[n], StartedInCombat = Game.Instance.Player.IsInCombat };
            var c = caster.Position;
            var t = j.Wrapper.Point;
            var casterPoint = new Vec3d(c.x, c.y, c.z);
            var targetPoint = new Vec3d(t.x, t.y, t.z);
            float[] facing = new float[n];
            var lineOrCone = EchoVisuals.LineOrConeOf(j);
            for (int k = 0; k < n; k++)
            {
                show.Units[k] = ShadowDoubles.Get(caster, k + 1);
                if ((object)show.Units[k] == null) throw new InvalidOperationException("shadow unit " + (k + 1) + " vanished between the readiness check and the appearance");
                var place = ShadowPlacement.Place(casterPoint, targetPoint, caster.Orientation, k + 1);
                show.Ground[k] = OnNavmesh(c, new Vector3((float)place.Point.X, (float)place.Point.Y, (float)place.Point.Z));
                facing[k] = (float)place.FacingDegrees;
                // 2차 독립 검토 중요-1: 길 안으로 당겨진 자리가 본체 자신의 메아리 원뿔·선에 들 수 있다(벽이 뒤에 있으면 본체 곁으로 당겨진다) → 그 자리면 드러내지 않는다.
                //   원뿔·선이 아니라고 읽힌 주문(false)은 이 판정이 지키는 것이 없다(그 메아리의 대상 수집이 그림자 자리와 무관) — 판독 불가(null)는 원뿔·선처럼 다룬다.
                if (lineOrCone != false && !ShadowClearance.Allows(ClearanceOf(show.Units[k], show.Ground[k], c, t), lineOrCone))
                {
                    // A wall can push one side into the line of fire. Try the opposite shoulder for a single shadow.
                    if (n != 1 || place.Source != FacingSource.Target) { Skip(caster, StageReasons.SlotNotClear); return false; }
                    var opposite = ShadowPlacement.Place(casterPoint, targetPoint, caster.Orientation, 2);
                    var other = OnNavmesh(c, new Vector3((float)opposite.Point.X, (float)opposite.Point.Y, (float)opposite.Point.Z));
                    if (!ShadowClearance.Allows(ClearanceOf(show.Units[k], other, c, t), lineOrCone)) { Skip(caster, StageReasons.SlotNotClear); return false; }
                    show.Ground[k] = other;
                }
            }
            int shown = 0;
            try
            {
                for (int k = 0; k < n; k++)
                {
                    // 먼저 게임 안으로 되돌리고(숨긴 동안은 게임 밖 — ShadowDoubles 머리 주석) 그다음 자리에 놓는다: 위치 쓰기는 게임 안 유닛에만 한다.
                    // 같은 호출 안이라 엔진 틱이 끼지 않는다 — 옛 자리에서 게임 안인 프레임은 없다.
                    ShadowDoubles.SetHidden(caster, k + 1, false);
                    shown++;
                    show.Units[k].Position = show.Ground[k];
                    show.Units[k].Orientation = facing[k];
                    show.StanceDeferred[k] = ShadowCast.Prepare(show.Units[k], caster); // 전투 자세(던지지 않는다)
                }
            }
            catch
            {
                // 드러내던 중에 던진 자리(shown 번째 — 반쯤 드러났을 수 있다)까지 다시 숨긴다. 이미 숨겨져 있으면 무동작(NextHidden).
                for (int k = 0; k < Math.Min(n, shown + 1); k++) ShadowDoubles.SetHidden(caster, k + 1, true);
                throw;
            }
            byJob.Add(j, show);
            active.Add(show);
            Appeared++;
            return true;
        }

        // 자리를 길 안으로: 본체에서 자리 쪽으로 걸을 수 있는 데까지(엔진 ObstacleAnalyzer.TraceAlongNavmesh — 벽 속·절벽 밖에 서지 않게). 높이도 그 결과를 쓴다.
        // 결과가 수가 아니면(엔진이 못 냈다) 본체 자리에 겹쳐 세운다 — 오류 줄 1회(조용히 엉뚱한 곳에 세우지 않는다).
        internal static Vector3 OnNavmesh(Vector3 from, Vector3 wanted)
        {
            // The native trace only projects its start when it is off the mesh; cast the ray from that projected point.
            var origin = ObstacleAnalyzer.TraceAlongNavmesh(from, from);
            var p = ObstacleAnalyzer.TraceAlongNavmesh(origin, wanted);
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z))
            {
                ShadowErrors.ReportOnce(EchoVisuals.Site, "navmesh-nan", "echo visual stage: navmesh trace returned a non-finite point - shadow placed on the caster's position");
                return from;
            }
            return p;
        }

        // 시전 동작: 대상 쪽을 보고 그 주문의 시전 동작을 시작한다(ShadowCast.Play — 던지지 않는다. 손 FX 는 발사가 성공한 순간 AfterFire 에서).
        static void Windup(Show show, int index)
        {
            var u = show.Units[index - 1];
            if (ShadowDoubles.Going(u)) return; // 다음 Tick 이 ShadowLost 로 끈다
            var target = show.Job.Wrapper.Point;
            u.ForceLookAt(target);
            ShadowCast.Play(u, show.Job.Data, target, ShadowCastRule.StanceOwner(show.StanceDeferred[index - 1], show.Caster));
            Windups++;
        }

        static void Skip(UnitEntityData caster, string reason)
        {
            int n;
            skips[reason] = skips.TryGetValue(reason, out n) ? n + 1 : 1;
            ModContext.Log("ShadowSorcerer: echo visual not shown for " + caster.CharacterName + " (" + reason + ") - the echo itself is unaffected");
        }

        // 그림자를 숨기고 무대를 끈다. reason = 드러난 뒤 꺼진 사유(StageReasons) 또는 null(정상 흩어짐·교체·실패 처리). 던지지 않는다.
        static void TurnOff(Show show, string reason)
        {
            show.Off = true;
            show.Anchors.Clear();
            active.Remove(show);
            for (int k = 0; k < show.Units.Length; k++)
            {
                // 묶음이 이미 지워졌거나 다시 만들어졌으면 이 자리의 숨김 장부가 없다(새 유닛은 숨겨진 채 만들어진다) → 할 일이 없다.
                // 자리 대조는 묶음 색인으로 한다(IsSlot) — 뷰가 없어진 유닛도 숨긴다(Get 은 뷰 없는 유닛을 「쓸 수 없음」으로 돌려줘서 숨기지 못했다, 독립 검토 F8).
                try { if (ShadowDoubles.IsSlot(show.Caster, k + 1, show.Units[k])) ShadowDoubles.SetHidden(show.Caster, k + 1, true); }
                catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "hide-" + e.GetType().Name, "echo visual stage: hiding a shadow failed (" + e.GetType().Name + ": " + e.Message + ")"); }
            }
            Hidden++;
            if (reason != null) Skip(show.Caster, reason);
        }
    }
}
