using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using CountableFlag = Kingmaker.Utility.CountableFlag;
using Kingmaker.View;
using UnityEngine;
using WotrClassMod.Common;

namespace PhantomBlade
{
     
    // 주인 참조는 Unity 컴포넌트(메모리)에만 있다 — 세이브에 들어가지 않는다.
    // 매 프레임(LateUpdate = 같은 프레임 엔진 갱신 뒤):
    //   1. 유효성 — 판정은 순수 SpiritRules.FollowerCheck. 령이 사라지는 중이면 멈춤, 주인만 사라졌으면 StandSpirit.RemoveOrphan.
    //   2. 위치 = SpiritRules.FollowTarget(주인 위치·방향, 다이얼). EntityDataBase.set_Position IL 0022~0038 이 transform.position 에 직접 대입(높이 포함).
    //      2026-11-16 귀신참 돌진: 돌진 중에는 그 위치 대신 DashPlace(순수 SpiritRules.DashStep)가 정한 위치·방향을 같은 자리에서 대입한다(설계 §14.6).
     
    //   3. 충돌체·이동 에이전트 다시 끔(정찰 2(c): 엔진이 다시 켜는지 미확인 → 매 프레임 재적용).
    //   4. 전투 자세 동작 = 주인 전투 여부(UnitAnimationManager.IsInCombat 공개 get/set, UnitEntityData.get_IsInCombat IL 0000~0015).
     
     
    //      (SpiritWalkSpeedPatch 전처리 → BeforeEngineAnimationTick), 여기 LateUpdate 의 CopyWalkSpeed 는 엔진 결과 판정만 한다(이유는 SpiritWalkSpeedPatch 머리말).
    //   8. 유령 디졸브(같은 날 판정 1): SpiritGhostLook.Tick(처음엔 붙이고 그 뒤엔 남아 있는지 확인). 7·8 의 판정 실패는 SpiritVisualErrors 에 세고 한 번 보고.
    // 숨김 기전 = 령의 UnitMechanicFeatures.Hidden(CountableFlag) 을 바뀔 때 한 번 Retain / Release(본편 UnitPartInPit.SetHidden IL 0000~0049,
    //   CommandMarkHiddenSpan.OnTurnOn 0023 / OnTurnOff 0020 과 같은 방식). 엔진 EntityVisibilityForPlayerController.Tick IL 0000~0043 이 매 틱 모든 유닛에
    //   Update → IsVisible(IL 000B~0018: Hidden 이면 false) → view.SetVisible(값, false)(Update IL 002D~003B) 를 부르므로, 우리가 SetVisible 을 직접 부르면
    //   다음 틱에 엔진이 되돌린다(SetVisible IL 0028~0034 값이 같으면 무동작 → 깜빡임). Hidden 깃발은 엔진의 그 판정 입력 자체라 다툼이 없다.
    //   GameObject 는 끄지 않는다(SetActive 금지 — 이 컴포넌트가 멈춘다). 령은 세이브 제외 유닛이라 깃발이 세이브에 남지 않고, 붙잡은 것은 OnDestroy 가 놓는다.
    // 실패는 시끄럽게: 어떤 단계든 던지면 오류 로그 1회 + 이 컴포넌트를 끈다(매 프레임 반복 로그 금지). 령은 그 자리에 멈춰 눈에도 드러난다.
    public class SpiritFollower : MonoBehaviour
    {
        UnitEntityData owner;
        UnitEntityData spirit;
        UnitEntityView view;
        // 이 따라가기가 령의 Hidden 깃발을 붙잡고 있는가(Retain 1회 ↔ Release 1회). 시험이 이 이름을 IL 로 대조한다.
        bool hiddenByUs;
         
        // (생성 중 = 대기, 등록되면 「정확히 하나」 단언 1번). 시험이 이 이름을 IL 로 대조한다.
        bool registrationConfirmed;
        // 공격 방향 유지 끝(Time.unscaledTime). -∞ = 한 번도 설정 안 함(정당한 초기값, SpiritRules.FacingHeld 가 「유지 아님」으로 판정).
        float holdFacingUntil = float.NegativeInfinity;
         
        SpiritGhostLook ghost;
        // 걷기 동작 복사(판정 2): 지난 프레임에 쓴 값(NaN = 안 씀), 엔진 0 연속 시작, 마지막 판정, 결과별 프레임 수(점검 S12 walk-anim 이 읽는다).
        double walkLastWritten = double.NaN;
        double walkZeroSince = double.NegativeInfinity;
        SpiritRules.WalkCopy lastWalk = SpiritRules.WalkCopy.NoPreviousWrite;
        readonly int[] walkCounts = new int[Enum.GetValues(typeof(SpiritRules.WalkCopy)).Length];
        int walkReportedMask; // 결과 종류별 「이미 보고함」(같은 오류 메시지를 매 프레임 만들지 않게 — 검토 m4)
        bool walkFailed;      // 걷기 복사 자체가 던졌음(한 번 보고 후 이 령에서는 멈춤 — 위치·숨김은 계속, 검토 m4)
        string walkKey;       // 보고 키(Init 에서 한 번 만듦)
        // 2026-11-17 쓰기 지점 이동: 엔진 동작 틱 전처리가 쓸지(지난 LateUpdate 에 령이 보였음), 후처리가 적은 「엔진이 읽은 뒤의 m_Speed」(NaN = 아직 없음), 전처리 쓰기 횟수.
        bool walkShownForEngine;
        double walkEngineSaw = double.NaN;
        int engineTickWrites;
        // 령 유닛 → 따라가기(엔진 동작 틱 전·후처리가 찾는다). OnDestroy가 이 인스턴스의 등록을 제거한다. 세는 수 = 등록된 따라가기 수(0 이면 전·후처리가 표를 보지도 않는다).
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UnitEntityData, SpiritFollower> followerOf = new System.Runtime.CompilerServices.ConditionalWeakTable<UnitEntityData, SpiritFollower>();
        static int registeredFollowers;
        bool registeredForWalk;
        static int patchCheck; // 0 = 아직 확인 안 함, 1 = 걸려 있음, -1 = 안 걸림(매번 던짐 → CopyWalkSpeed catch 가 한 번 보고)

        // ---- 귀신참 돌진(소유자 결정 2026-11-16, 설계 §14.6). 상태는 이 Unity 컴포넌트의 필드뿐 — 세이브에 없다(령은 세이브 제외 유닛, 컴포넌트는 직렬화되지 않음). ----
        //   령·따라가기가 지워지면(스탠드 끔·주인 사망·지역 이동·령 파괴) 상태도 함께 사라지고, 새 령은 Idle 로 시작한다. 판정·위치 계산은 순수 SpiritRules.DashStep.
        SpiritRules.DashPhase dashPhase = SpiritRules.DashPhase.Idle;
        double dashElapsed;          // 돌진 시작 뒤 흐른 시간(초). 일시 정지 중에는 더하지 않는다(DashPlace).
        int dashSwings;              // 낼 휘두름 수 = 귀신참 발 수
        int dashSwingsDone;          // 재생된 휘두름 수
        Vector3 dashStart;           // 돌진을 시작한 순간의 령 위치
        UnitEntityData dashTarget;   // 대상(참조만 — 규칙·명령을 만들지 않는다)
        bool dashSawCombat;          // 돌진 중 주인이 전투에 들어간 적이 있는가(전투 이탈 판정용)
        int dashStarted, dashFinished, dashAborted, dashTimedOut; // 점검·눈 점검용 누계(이 령)
        // 잔상(소유자 결정 2026-11-16 밤, 설계 §14.8): 가는 길의 잔상 지점(BeginDash 가 계획, null = 돌진 없음)과 그중 이미 띄운 수. FX 는 GhostSlashFx 가 띄운다.
        Vector3[] dashTrailOut;
        int dashTrailOutDone;

        // 걷기 동작 속도 원천 = MovementAgent.Speed = protected 필드 m_Speed(get_Speed IL 0000~0006). 엔진이 쓰는 곳은 TickMovement·Stop 뿐(dnlib 참조 역추적 2026-11-14).
        //   2026-11-17 정정: 에이전트가 꺼져 있어도 Stop 은 돈다 — 명령 없는 령에 UnitCommandController.TickOnUnit 이 매 틱 StopMoving → Stop(m_Speed = 0).
        //   그래서 쓰기는 엔진 동작 틱 직전(SpiritWalkSpeedPatch). GetField 직접 호출 = Exact(선언 형식 UnitMovementAgentBase).
        static readonly System.Reflection.FieldInfo AgentSpeedField = typeof(UnitMovementAgentBase).GetField("m_Speed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        // 매 프레임 상자 없는 쓰기(검토 m4): 위 FieldInfo 로 한 번 만든 DynamicMethod 설정자. 만들 수 없으면 null → CopyWalkSpeed 가 던진다(→ 한 번 보고).
        static readonly Action<UnitMovementAgentBase, float> SetAgentSpeed = BuildSpeedSetter();

        static string speedSetterProblem; // 설정자를 못 만든 이유(CopyWalkSpeed 가 던질 때 메시지에 넣는다)

        static Action<UnitMovementAgentBase, float> BuildSpeedSetter()
        {
            if (AgentSpeedField == null) { speedSetterProblem = "UnitMovementAgentBase.m_Speed not found"; return null; }
            if (AgentSpeedField.FieldType != typeof(float)) { speedSetterProblem = "UnitMovementAgentBase.m_Speed is " + AgentSpeedField.FieldType + ", expected float"; return null; }
            try
            {
            var dm = new System.Reflection.Emit.DynamicMethod("PhantomBladeSetAgentSpeed", null, new[] { typeof(UnitMovementAgentBase), typeof(float) }, typeof(UnitMovementAgentBase), true);
            var il = dm.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            il.Emit(System.Reflection.Emit.OpCodes.Stfld, AgentSpeedField);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Action<UnitMovementAgentBase, float>)dm.CreateDelegate(typeof(Action<UnitMovementAgentBase, float>));
            }
            catch (Exception e) { speedSetterProblem = "m_Speed setter could not be built: " + e.Message; return null; } // 정적 초기화가 따라가기 전체를 죽이지 않게 이유를 남기고 null
        }






        public float FacingHoldUntil { get { return holdFacingUntil; } }


































        public void Init(UnitEntityData spiritOwner)
        {
            if (spiritOwner == null) throw new ArgumentNullException("spiritOwner");
            if (owner != null) throw new InvalidOperationException("PhantomBlade: SpiritFollower.Init called twice (owner " + owner + ", new " + spiritOwner + ")");
            var v = GetComponent<UnitEntityView>();
            if (v == null) throw new InvalidOperationException("PhantomBlade: SpiritFollower is not on a UnitEntityView");
            var s = v.EntityData;
            if ((object)s == null) throw new InvalidOperationException("PhantomBlade: SpiritFollower view has no EntityData");
            if (ReferenceEquals(s, spiritOwner)) throw new InvalidOperationException("PhantomBlade: SpiritFollower attached to the owner's own view");
            owner = spiritOwner;
            view = v;
            spirit = s;
            ghost = new SpiritGhostLook(v, s.CharacterName);
            walkKey = s.CharacterName + "#" + v.GetInstanceID();
            // 엔진 동작 틱 전·후처리용 등록. 같은 령에 살아 있는 다른 따라가기가 이미 있으면 어느 쪽이 쓰는지 모호하다 → 던진다(C2).
            SpiritFollower other;
            if (followerOf.TryGetValue(s, out other))
            {
                if (other != null && !ReferenceEquals(other, this))
                {
                    if (ReferenceEquals(other.view, v) && other.enabled)
                        throw new InvalidOperationException("PhantomBlade: two live followers on the same spirit view");
                    other.Retire();
                }
                followerOf.Remove(s); // 정당: 파괴된 옛 따라가기의 남은 칸
            }
            followerOf.Add(s, this);
            registeredForWalk = true;
            registeredFollowers++;
        }

        // ---- 엔진 동작 틱 전·후처리(SpiritWalkSpeedPatch). 우리 령이 아니면 무동작(정당). 여기서 던지면 엔진 틱이 깨지므로 잡아서 걷기 복사만 멈추고 한 번 보고한다. ----
        internal static void BeforeEngineAnimationTick(UnitEntityData unit)
        {
            if (registeredFollowers == 0 || (object)unit == null) return; // 정당: 령 없음
            SpiritFollower f;
            if (!followerOf.TryGetValue(unit, out f) || f == null) return; // 정당: 령이 아닌 유닛(또는 이미 파괴된 따라가기)
            f.WriteWalkSpeedForEngine();
        }

        internal static void AfterEngineAnimationTick(UnitEntityData unit)
        {
            if (registeredFollowers == 0 || (object)unit == null) return;
            SpiritFollower f;
            if (!followerOf.TryGetValue(unit, out f) || f == null) return;
            f.RecordEngineSaw();
        }

        // 쓰기(전처리): 지난 LateUpdate 에 령이 보였고 걷기 복사가 멈추지 않았을 때만. 주인 에이전트 Speed(이번 틱 UnitMoveController 가 갱신한 값) → 령 m_Speed.
        void WriteWalkSpeedForEngine()
        {
            if (walkFailed || !enabled || !walkShownForEngine) return; // 정당: 숨김·중단·꺼짐
            try
            {
                var spiritAgent = view == null ? null : view.MovementAgent;
                if (spiritAgent == null) throw new InvalidOperationException("spirit view has no MovementAgent at the engine animation tick");
                var ov = owner.View;
                var oa = ov == null ? null : ov.MovementAgent;
                if (oa == null) throw new InvalidOperationException("owner " + owner.CharacterName + " has " + (ov == null ? "no view" : "no MovementAgent") + " at the engine animation tick");
                if (SetAgentSpeed == null) throw new InvalidOperationException(speedSetterProblem ?? "m_Speed setter missing");
                float speed = oa.Speed;
                SetAgentSpeed(spiritAgent, speed);
                walkLastWritten = speed;
                walkEngineSaw = double.NaN; // 이번 틱 후처리가 채운다
                engineTickWrites++;
            }
            catch (Exception e)
            {
                walkFailed = true;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteWalkCopy, walkKey + "|failed", "spirit " + spirit.CharacterName + " walk write at the engine animation tick stopped (position/hide continue): " + e.Message);
            }
        }

        // 후처리: 엔진 동작 틱이 끝난 뒤의 령 m_Speed(= 엔진이 읽은 값. TickOnUnit 은 m_Speed 를 쓰지 않는다 — 시험 잠금).
        void RecordEngineSaw()
        {
            if (walkFailed || double.IsNaN(walkLastWritten)) return; // 정당: 이번에 쓰지 않았다
            var spiritAgent = view == null ? null : view.MovementAgent;
            walkEngineSaw = spiritAgent == null ? double.NaN : spiritAgent.Speed; // 없음 = NaN → LateUpdate 판정이 던진다(C1)
        }

         
        public void HoldFacingUntil(float unscaledTime)
        {
            if (float.IsNaN(unscaledTime) || float.IsInfinity(unscaledTime)) throw new ArgumentException("PhantomBlade: HoldFacingUntil needs a finite time (got " + unscaledTime + ")", "unscaledTime");
            holdFacingUntil = unscaledTime;
        }

        // 귀신참 시전 때 StandSpiritAnimator.TryStartDash 가 부른다: 령이 대상 앞으로 돌진해 swings 번 휘두르고 돌아온다(연출만).
        //   겹침 정책: 돌진 중에 새 돌진이 오면 새 것이 대신한다(지금 위치에서 다시 출발, 남은 휘두름은 버림 — 한 줄 기록).
        //   돌진 중의 다른 휘두름 요청(러시 주먹 등)은 버리지 않는다 — 종전대로 즉시 재생되고(StandSpiritAnimator.Swing), 위치는 돌진이 정한다.
        public void BeginDash(UnitEntityData target, int swings)
        {
            if ((object)target == null) throw new ArgumentNullException("target");
            if ((object)owner == null || (object)spirit == null) throw new InvalidOperationException("PhantomBlade: SpiritFollower.BeginDash before Init");
            if (ReferenceEquals(target, owner) || ReferenceEquals(target, spirit)) throw new InvalidOperationException("PhantomBlade: SpiritFollower.BeginDash at the owner or the spirit itself");
            SpiritRules.RequireDashSwings(swings);
            if (dashPhase != SpiritRules.DashPhase.Idle)
            {
                dashAborted++;
                ModContext.Log("PhantomBlade: spirit of " + owner.CharacterName + " restarts its ghost slash dash (a new cast replaced one in " + dashPhase + " with " + dashSwingsDone + "/" + dashSwings + " swings done)");
            }
            dashTarget = target;
            dashSwings = swings;
            dashSwingsDone = 0;
            dashElapsed = 0;
            dashStart = spirit.Position;
            // 가는 길 잔상 지점: 출발점 → 대상 앞(순수 SpiritRules.DashDestination — DashStep 과 같은 함수·같은 다이얼) 사이. 계획 실패는 보고 후 「잔상 없음」(돌진은 시작한다).
            dashTrailOut = GhostSlashFx.PlanOutbound(dashStart, target.Position, target.Corpulence);
            dashTrailOutDone = 0;
            dashSawCombat = owner.IsInCombat;
            dashPhase = SpiritRules.DashPhase.Dashing;
            dashStarted++;
        }

        void ResetDash()
        {
            dashPhase = SpiritRules.DashPhase.Idle;
            dashTarget = null;
            dashSwings = 0;
            dashSwingsDone = 0;
            dashElapsed = 0;
            dashSawCombat = false;
            dashTrailOut = null;
            dashTrailOutDone = 0;
        }

        // 이번 프레임의 령 위치(와 방향)를 정한다. 대기면 따라가기 위치 그대로(종전 동작). 돌진 중이면 순수 SpiritRules.DashStep 의 결과.
        //   여기서는 계산만 한다 — 위치·방향 대입은 LateUpdate 한 곳(set_Position 직접 대입: 길찾기·이동 에이전트·밀어내기를 쓰지 않는다, 에이전트·충돌체는 매 프레임 꺼 둠).
        //   시계 = 실시간(Time.unscaledDeltaTime)을 「일시 정지가 아닐 때만」 더한 값: 일시 정지 중에는 멈추고, 턴제에서도(게임 시간 배율과 무관하게) 진행한다.
        //   종료 보장: ① 정상 — 휘두름을 다 내고 머묾이 끝나면 Idle ② 상한(DashCapSeconds) 초과 → 강제 복귀 + 보고 ③ 대상 사라짐·게임 밖·주인 전투 이탈 → 즉시 Idle(한 줄 기록)
        //     ④ 휘두름 실패 → 즉시 Idle(애니메이터가 보고) ⑤ 이 메서드의 예외 → 보고 후 Idle ⑥ 령·따라가기가 지워짐(스탠드 끔·주인 사망·지역 이동) → 상태 소멸.
        Vector3 DashPlace(Vector3 follow, float ownerFacing, ref float face)
        {
            if (dashPhase == SpiritRules.DashPhase.Idle) return follow; // 정당: 돌진 없음
            try
            {
                var target = dashTarget;
                if ((object)target == null) throw new InvalidOperationException("dash in " + dashPhase + " without a target");
                bool inCombat = owner.IsInCombat;
                bool leftCombat = dashSawCombat && !inCombat;
                if (inCombat) dashSawCombat = true;
                var abort = SpiritRules.DashAbortFor(StandSpirit.Going(target), target.IsInGame ? SpiritRules.Tri.Yes : SpiritRules.Tri.No, leftCombat);
                if (abort != SpiritRules.DashAbort.None)
                {
                    dashAborted++;
                    ModContext.Log("PhantomBlade: spirit of " + owner.CharacterName + " ends its ghost slash dash early (" + abort + ", " + dashSwingsDone + "/" + dashSwings + " swings done) and returns");
                    ResetDash();
                    return follow;
                }
                var game = Kingmaker.Game.Instance;
                if (game == null) throw new InvalidOperationException("Game.Instance is null (cannot tell whether the game is paused)");
                if (!game.IsPaused) dashElapsed += Time.unscaledDeltaTime;
                Vector3 tp = target.Position;
                var r = SpiritRules.DashStep(dashPhase, dashElapsed, dashSwings, dashSwingsDone,
                    ((double)dashStart.x, (double)dashStart.y, (double)dashStart.z), ((double)follow.x, (double)follow.y, (double)follow.z), ownerFacing,
                    ((double)tp.x, (double)tp.y, (double)tp.z), target.Corpulence,
                    SpiritRules.Dials.DashSeconds, SpiritRules.Dials.DashSwingSeconds, SpiritRules.Dials.DashMarginMeters, SpiritRules.Dials.SpiritUpMeters, SpiritRules.Dials.DashTimeoutSlackSeconds);
                if (r.TimedOut)
                {
                    dashTimedOut++;
                    SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteDash, walkKey + "|timeout", "spirit " + spirit.CharacterName + " ghost slash dash did not finish within its cap (" + dashElapsed.ToString("0.00") + " s, "
                        + dashSwingsDone + "/" + dashSwings + " swings played) — forced back behind its owner (counted every time, reported once)");
                    ResetDash();
                    return follow;
                }
                // 잔상 — 가는 길(소유자 결정 2026-11-16 밤): 령이 지나친 지점마다 차례로 하나씩. 지나친 수 = 순수 SpiritRules.DashTrailDue(돌진 중 = 진행률, 도착 뒤 = 전부).
                //   령이 숨겨져 있으면(hiddenByUs — 주인 은신·투명·본체 없음) 띄우지 않는다(GhostSlashFx 가 SpiritRules.DashTrailFor 로 판정). FX 실패는 GhostSlashFx 가 보고하고 돌진은 계속된다.
                if (dashTrailOut != null && dashTrailOutDone < dashTrailOut.Length)
                {
                    int due = SpiritRules.DashTrailDue(dashTrailOut.Length, r.Phase == SpiritRules.DashPhase.Dashing ? dashElapsed / SpiritRules.Dials.DashSeconds : 1.0);
                    if (due > dashTrailOutDone)
                    {
                        GhostSlashFx.Trail(owner, dashTrailOut, dashTrailOutDone, due - dashTrailOutDone, hiddenByUs);
                        dashTrailOutDone = due;
                    }
                }
                if (r.FireSwing)
                {
                    // true = 재생됨, false = 이번 프레임엔 재생 안 됨(같은 프레임 버스트 상한·령 부재가 설명됨 → 다음 프레임 재시도, 상한이 끝을 보장), null = 실패(애니메이터가 보고함).
                    bool? played = StandSpiritAnimator.DashSwing(owner, target);
                    if (!played.HasValue)
                    {
                        dashAborted++;
                        ResetDash();
                        return follow;
                    }
                    if (played.Value)
                    {
                        dashSwingsDone++;
                        // 타격 효과(소유자 결정 2026-11-16 밤): 휘두름이 재생된 그 프레임에 대상 유닛에 하나(발 수만큼 휘두르니 발 수만큼).
                        //   대상이 이미 쓰러졌거나 사라졌으면·령이 숨겨져 있으면 띄우지 않는다 — 판정은 GhostSlashFx.Hit 안의 순수 SpiritRules.DashHitFxFor(명시적 분기).
                        GhostSlashFx.Hit(owner, target, hiddenByUs);
                    }
                }
                if (r.Phase == SpiritRules.DashPhase.Idle)
                {
                    dashFinished++;
                    // 잔상 — 오는 길: 복귀는 이 프레임에 순간이므로 머물던 자리(지금의 령 위치) → 따라가기 위치 사이에 한꺼번에 띄운다. 정상 종료에서만
                    //   (중단·상한 초과·실패 복귀는 이미 한 줄 기록되는 비정상 끝 — 연출을 더하지 않는다).
                    if (SpiritRules.Dials.DashTrailOnReturn)
                    {
                        var back = GhostSlashFx.PlanTrail(spirit.Position, follow);
                        GhostSlashFx.Trail(owner, back, 0, back.Length, hiddenByUs);
                    }
                    ResetDash();
                    return follow;
                }
                dashPhase = r.Phase;
                face = (float)r.FacingDeg;
                return new Vector3((float)r.X, (float)r.Y, (float)r.Z);
            }
            catch (Exception e)
            {
                dashAborted++;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteDash, walkKey + "|failed", "spirit " + ((object)spirit == null ? "?" : spirit.CharacterName) + " ghost slash dash aborted (follow continues): " + e.Message);
                ResetDash();
                return follow;
            }
        }

        void LateUpdate()
        {
            try
            {
                if ((object)owner == null || (object)spirit == null || view == null) throw new InvalidOperationException("LateUpdate before Init");
                // A pooled or detached view must stop moving the previous spirit.
                if (!ReferenceEquals(view.EntityData, spirit) || !ReferenceEquals(spirit.View, view))
                {
                    var currentView = spirit.View;
                    Retire();
                    if (!StandSpirit.Going(spirit) && currentView != null && ReferenceEquals(currentView.EntityData, spirit) &&
                        ReferenceEquals(StandSpirit.Get(owner), spirit)) StandSpirit.Follower(spirit);
                    return;
                }

                // 1. 유효성
                bool spiritGone = StandSpirit.Going(spirit);
                bool ownerGone = StandSpirit.Going(owner);
                var step = SpiritRules.FollowerCheck(spiritGone, ownerGone);
                if (step == SpiritRules.FollowerStep.Stop)
                {
                    ModContext.Log("PhantomBlade: SpiritFollower stops (spirit is being destroyed) for " + owner.CharacterName);
                    Destroy(this);
                    return;
                }
                if (step == SpiritRules.FollowerStep.RemoveOrphan)
                {
                    ModContext.Log("PhantomBlade: SpiritFollower owner is gone (destroyed/disposed) — removing its spirit");
                    StandSpirit.RemoveOrphan(spirit);
                    Destroy(this);
                    return;
                }
                if (step != SpiritRules.FollowerStep.Run) throw new InvalidOperationException("unknown follower step " + step);

                 
                if (!registrationConfirmed) registrationConfirmed = StandSpirit.ConfirmRegistration(owner, spirit);

                // 2. 위치·방향
                Vector3 op = owner.Position;
                float facing = owner.Orientation;
                var t = SpiritRules.FollowTarget(op.x, op.y, op.z, facing, SpiritRules.Dials.SpiritBehindMeters, SpiritRules.Dials.SpiritUpMeters);
                // 2b. 귀신참 돌진(2026-11-16): 대기면 따라가기 위치·주인 방향 그대로, 돌진 중이면 순수 SpiritRules.DashStep 이 정한 위치·방향. 대입은 아래 한 곳.
                float face = facing;
                Vector3 place = DashPlace(new Vector3((float)t.x, (float)t.y, (float)t.z), facing, ref face);
                spirit.Position = place;
                if (!SpiritRules.FacingHeld(Time.unscaledTime, holdFacingUntil))
                    spirit.Orientation = face;

                // 3. 충돌체·이동 에이전트(정찰 2(c))
                view.ActivateColliders(false);
                var agent = view.MovementAgent;
                if (agent == null) throw new InvalidOperationException("spirit view has no MovementAgent");
                agent.enabled = false;

                // 4. 전투 자세 동작
                var anim = view.AnimationManager;
                if (anim == null) throw new InvalidOperationException("spirit view has no AnimationManager");
                anim.IsInCombat = owner.IsInCombat;

                // 5·6. 숨김. 네 입력을 전부 먼저 읽고(Unknown 이면 순수 함수가 이름과 함께 던짐), 두 규칙 결과를 따로 받은 뒤 전이를 정한다.
                var stealthed = OwnerStealthed(owner);
                var invisible = OwnerInvisible(owner);
                var viewActive = OwnerViewActive(owner);
                var inGame = OwnerInGame(owner);
                bool stealthRuleShows = SpiritRules.ShouldShow(stealthed, invisible);
                bool ownerPresent = SpiritRules.OwnerPresent(viewActive, inGame);
                var change = SpiritRules.VisibilityStep(stealthRuleShows, ownerPresent, hiddenByUs);
                ApplyVisibility(change, stealthed, invisible, viewActive, inGame);

                // 7. 걷기 동작(판정 2): 주인 이동 속도를 꺼진 령 에이전트에 복사. 령이 보일 때만(숨었으면 동작이 안 보이고, 본체가 없으면 읽을 속도도 없다 — 정당한 건너뜀).
                CopyWalkSpeed(agent, stealthRuleShows && ownerPresent);
                // 8. 유령 디졸브(판정 1). 실패는 SpiritGhostLook 이 따로 보고하고 따라가기는 계속된다.
                ghost.Tick();
            }
            catch (Exception e)
            {
                ModContext.LogError("PhantomBlade: SpiritFollower failed for " + ((object)owner == null ? "?" : owner.CharacterName) + " — follower disabled: " + e);
                enabled = false;
            }
        }

        // 판정 2(2026-11-14): 걷기 동작 속도 = UnitAnimationController.TickOnUnit IL 01B9~01E1 = View.MovementAgent.Speed × GetSpeedAnimationCoeff.
        //   령 에이전트는 꺼 두고(길막 없음) 속도 필드만 주인 값으로 채운다. (2026-11-14 원설계는 Harmony 없이 여기서 썼으나 다음 틱 Stop 이 덮었다 — 아래 재설계.)
        //   판정은 「엔진 결과」로(검토 I2): 새로 쓰기 전에 지난 프레임에 쓴 값이 지금 에이전트에 그대로 있는지, 엔진이 쓴 UnitAnimationManager.Speed 가 0 인지를
        //   SpiritRules.WalkEngineStep 에 넘긴다(턴제 억제·자기 공격 동작·유예는 그 함수가 가른다). 오류 종류마다 한 번만 메시지를 만들어 보고(검토 m4).
        //   이 메서드가 던지면(Unknown 입력·설정자 없음 등) 한 번 보고하고 걷기 복사만 멈춘다 — 위치·숨김은 계속(검토 m4).
        // 2026-11-17 재설계: 쓰기는 엔진 동작 틱 직전(WriteWalkSpeedForEngine). 여기서는 판정만 — 「지난 쓰기」 = 전처리가 쓴 값, 「엔진이 본 값」 = 후처리가 적은 값.
        //   LateUpdate 시점의 m_Speed 를 보지 않는 이유: 다음 틱 UnitCommandController 가 매번 0 으로 만드는 것은 정상 경로이고(SpiritWalkSpeedPatch 머리말 ①),
        //   엔진이 읽는 순간의 값만이 동작을 정한다. 패치가 안 걸렸으면 첫 판정에서 던진다 → catch 가 오류 줄 1번 + 걷기 복사만 멈춤.
        void CopyWalkSpeed(UnitMovementAgentBase spiritAgent, bool spiritShown)
        {
            if (walkFailed) return;
            try
            {
                if (!spiritShown) { walkLastWritten = double.NaN; walkEngineSaw = double.NaN; walkZeroSince = double.NegativeInfinity; walkShownForEngine = false; return; } // 숨었거나 본체가 없음 = 정당한 건너뜀(다음 판정은 「안 씀」부터)
                if (patchCheck == 0) patchCheck = SpiritWalkSpeedPatch.IsApplied() ? 1 : -1;
                if (patchCheck < 0) throw new InvalidOperationException("UnitAnimationController.TickOnUnit prefix/postfix (SpiritWalkSpeedPatch) is not applied under harmony id " + ModContext.HarmonyId + " — the walk speed cannot reach the engine");
                // 1. 전처리가 쓴 값의 엔진 결과(쓴 적 없으면 NoPreviousWrite — before 는 판정에 안 쓰이지만 유한값이어야 하므로 지금 값)
                float before = double.IsNaN(walkLastWritten) ? spiritAgent.Speed : (float)walkEngineSaw;
                var anim = view.AnimationManager;
                var st = spirit.State;
                float engineAnim = anim == null ? float.NaN : anim.Speed;
                var tb = TurnBased.Controllers.CombatController.IsInTurnBasedCombat() ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
                var canMove = st == null ? SpiritRules.Tri.Unknown : (st.CanMove ? SpiritRules.Tri.Yes : SpiritRules.Tri.No);
                var busy = anim == null ? SpiritRules.Tri.Unknown : (anim.IsPreventingMovement ? SpiritRules.Tri.Yes : SpiritRules.Tri.No);
                // 유예 시계 = 게임 시각(TimeController.GameTime — 일시정지 중엔 안 감, StandRushState 선례). 실시간을 쓰면 일시정지 2 s 가 거짓 EngineSawZero 를 낸다(재검토 n1).
                var game = Kingmaker.Game.Instance;
                if (game == null || game.TimeController == null) throw new InvalidOperationException("Game.Instance.TimeController is missing (no game clock for the walk grace)");
                double gameNow = game.TimeController.GameTime.TotalSeconds;
                var step = SpiritRules.WalkEngineStep(walkLastWritten, before, engineAnim, tb, canMove, busy, gameNow, walkZeroSince, SpiritRules.Dials.EngineSpeedGraceSeconds);
                lastWalk = step.result;
                walkZeroSince = step.zeroSince;
                walkCounts[(int)step.result]++;
                int bit = 1 << (int)step.result;
                if (SpiritRules.WalkCopyIsError(step.result) && (walkReportedMask & bit) == 0)
                {
                    walkReportedMask |= bit;
                    SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteWalkCopy, walkKey + "|" + step.result, "spirit " + spirit.CharacterName + " walk " + step.result + ": last written " + walkLastWritten
                        + ", agent speed the engine read " + before + ", engine animation speed " + engineAnim + ", turn-based " + tb + ", CanMove " + canMove + ", own animation prevents movement " + busy + " (counted every frame, reported once)");
                }
                // 2. 다음 엔진 동작 틱 전처리가 쓰도록 허락(쓰기 자체는 WriteWalkSpeedForEngine)
                walkShownForEngine = true;
            }
            catch (Exception e)
            {
                walkFailed = true;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteWalkCopy, walkKey + "|failed", "spirit " + spirit.CharacterName + " walk copy stopped (position/hide continue): " + e.Message);
            }
        }
        void ApplyVisibility(SpiritRules.VisibilityChange change, SpiritRules.Tri stealthed, SpiritRules.Tri invisible, SpiritRules.Tri viewActive, SpiritRules.Tri inGame)
        {
            switch (change)
            {
                case SpiritRules.VisibilityChange.None: return;
                case SpiritRules.VisibilityChange.Hide:
                {
                    var flag = HiddenFlag();
                    flag.Retain();
                    hiddenByUs = true;
                    if (!flag) throw new InvalidOperationException("Hidden.Retain() did not raise the flag");
                    ModContext.Log("PhantomBlade: spirit of " + owner.CharacterName + " hidden (owner stealthed " + stealthed + ", invisible " + invisible + ", view active " + viewActive + ", in game " + inGame + ")");
                    return;
                }
                case SpiritRules.VisibilityChange.Show:
                    HiddenFlag().Release();
                    hiddenByUs = false;
                    ModContext.Log("PhantomBlade: spirit of " + owner.CharacterName + " shown again");
                    return;
                default: throw new InvalidOperationException("unknown visibility change " + change);
            }
        }

        // 령의 Hidden 깃발(UnitState.Features 는 UnitMechanicFeatures, Hidden 은 공개 필드 — 형식 덤프 2026-11-09).
        CountableFlag HiddenFlag()
        {
            var st = spirit.State;
            if (st == null || st.Features == null || st.Features.Hidden == null) throw new InvalidOperationException("spirit has no State.Features.Hidden");
            return st.Features.Hidden;
        }

        internal bool BelongsTo(UnitEntityData wantedOwner, UnitEntityData wantedSpirit)
            => enabled && ReferenceEquals(owner, wantedOwner) && ReferenceEquals(spirit, wantedSpirit);

        internal void Retire()
        {
            enabled = false;
            ReleaseBinding();
            Destroy(this);
        }

        void OnDestroy() { ReleaseBinding(); }

        void ReleaseBinding()
        {
            if (ghost != null) ghost.ReleaseParticleTints();
            if (registeredForWalk)
            {
                registeredForWalk = false;
                registeredFollowers--;
                SpiritFollower cur;
                if ((object)spirit != null && followerOf.TryGetValue(spirit, out cur) && ReferenceEquals(cur, this)) followerOf.Remove(spirit);
            }
            if (!hiddenByUs) return; // 정당한 무동작: 붙잡은 깃발이 없다
            hiddenByUs = false;
            // 령이 사라지는 중이면 깃발도 유닛과 함께 버려진다(세이브 제외 유닛) — 놓을 대상이 없다.
            if ((object)spirit == null || StandSpirit.Going(spirit)) return;
            HiddenFlag().Release();
            ModContext.Log("PhantomBlade: SpiritFollower destroyed while hiding the spirit — Hidden released");
        }

        // ---- 네 입력(Tri). 상태 객체가 없으면 Unknown → 순수 함수가 이름과 함께 던진다(C1). ----

        // 은신 = UnitState.IsInStealth(엔진 판정 EntityVisibilityForPlayerController.IsVisible IL 006F, 본편 Familiar.Update IL 0056 — 주인 은신이면 숨는 선례).
        static SpiritRules.Tri OwnerStealthed(UnitEntityData o)
        {
            var st = o.State;
            if (st == null) return SpiritRules.Tri.Unknown;
            return st.IsInStealth ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }

        // 투명 = State.HasCondition(UnitCondition.Invisible)(BuffInvisibility.OnTurnOn IL 0022 AddCondition(7) / OnTurnOff 0031 RemoveCondition,
        //   본편 Familiar.Update IL 003B~0041 이 같은 질문으로 숨음).
        static SpiritRules.Tri OwnerInvisible(UnitEntityData o)
        {
            var st = o.State;
            if (st == null) return SpiritRules.Tri.Unknown;
            return st.HasCondition(UnitCondition.Invisible) ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }

        // 뷰 활성 = UnitEntityData.IsViewActive(GetViewActive(true) IL 0000~002B: 게임 안 ∧ 잠들지 않음 등). 엔진 계산값이라 결측 없음.
        //   IL 상으로는 callvirt EntityDataBase::get_IsViewActive — UnitEntityData 가 재정의(virtual, newslot=False)하므로 실행 때 재정의로 간다.
        static SpiritRules.Tri OwnerViewActive(UnitEntityData o)
        {
            return o.IsViewActive ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }

        static SpiritRules.Tri OwnerInGame(UnitEntityData o)
        {
            return o.IsInGame ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }
    }
}
