using System;
using HarmonyLib;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;
using UnityEngine;

namespace ShadowSorcerer
{
    // 그림자 메아리 연출의 출발점 옮기기(소유자 결정 2026-11-16 밤: 범위 주문도 「보이는 것만」 옮긴다 — 판정은 본체 기준 그대로).
     
    //   - 메아리 연출이 묶인 컨텍스트(EchoVisuals.Bind — 그 메아리의 그림자가 드러나 있을 때만 묶인다)에서만 값을 바꾼다. 그 밖(본 시전·다른 유닛·다른 모드의 시전)은 판독 뒤 그대로 돌아간다.
    //   - 원래 메서드를 건너뛰지 않는다(void). 예외 = 보고 + 엔진 값 그대로.
    //   - 규칙에 쓰지 않는다: 투사체의 Launcher(발사자 = 본체)와 주문 대상·컨텍스트는 그대로 둔다 → 명중 굴림·범위 선택·턴 진행(TurnController 가 Launcher 로 본다)이 지금과 같다.

    // H11: AbilityExecutionProcess.Tick() Prefix + Finalizer — 메아리 실행 범위.
    // 근거: 투사체는 전달 반복자가 도는 Tick 안에서 발사된다(ProcessRoutine IL_020E·0257 → AbilityDeliverProjectile.Deliver → ProjectileController.Launch → CreateView).
    //   CreateView 에는 컨텍스트가 없으므로 「지금 도는 과정」을 여기서 알려 준다. 범위는 Finalizer 가 닫는다(Tick 본문의 예외·조기 종료 포함).
    //   메아리가 아닌 과정은 범위가 비어 있으면 표 조회 1회 뒤 아무것도 하지 않는다.
    [HarmonyPatch(typeof(AbilityExecutionProcess), nameof(AbilityExecutionProcess.Tick))]
    static class EchoExecScopePatch
    {
        static void Prefix(AbilityExecutionProcess __instance, out ScopeToken __state)
        {
            __state = default(ScopeToken);
            try
            {
                var anchor = EchoVisuals.AnchorOf(__instance.Context);
                if (anchor != null && anchor.Process == null) anchor.Process = __instance; // 연출 쪽 기록(흩어짐이 이 주문의 종료를 기다린다)
                __state = EchoVisuals.Scope.Enter(__instance, anchor, EchoVisuals.Stamp());
                EchoVisuals.ReportScopeLeaks(); // Enter 가 샌 범위를 버렸으면 한 번 보고(정수 비교 1회)
            }
            catch (Exception e) { ShadowErrors.ReportException(EchoVisuals.Site, e); }
        }

        static Exception Finalizer(Exception __exception, ScopeToken __state)
        {
            try { EchoVisuals.Scope.Exit(__state); }
            catch (Exception e) { ShadowErrors.ReportException(EchoVisuals.Site, e); }
            return __exception;
        }
    }

    // H12: ProjectileController.CreateView(Projectile, Vector3? customLaunchPosition) Prefix — 메아리 투사체의 출발 좌표.
    // 근거 CreateView IL_0169~017B: customLaunchPosition 에 값이 있으면 발사자 뼈대 계산을 건너뛰고 그 좌표가 출발점이 된다(본편 조종 투사체가 쓰는 경로).
    //   모든 Launch 오버로드(6)가 이 메서드를 지난다(정찰 §2.1). 판정 = 순수 EchoLaunchRule.Projectile:
    //   범위가 이 프레임의 메아리 과정 · 엔진이 좌표를 따로 정하지 않음 · 발사자가 그 메아리의 본체
     
    //     도착 시각을 바꾸고, 도착 시각은 연쇄의 다음 대상·효과 시각·다음 메아리의 대상 재확인에 쓰인다. EchoLaunchRule.MovableDeliveries 주석) · 지형을 따라가는 투사체가 아님 · 그 그림자가 지금 드러나 있음
    //   — 하나라도 아니면 손대지 않는다. 그래서 지금은 메아리 투사체가 모두 본체에서 나간다(그림자의 시전 동작·손 FX·시전자 앵커 FX 는 그대로).
    //   범위가 다른 프레임 것이면(닫히지 않고 샌 범위) 한 번 보고하고 손대지 않는다.
    //   지형을 따라가는 투사체(BlueprintProjectile.FollowTerrain): 엔진 Projectile.Tick IL_0146~0162 가 현재 위치로 UnitMovementAgentBase.Move(벽 충돌)를 부른다
    //   → 출발 좌표가 판정에 들어가므로 옮기지 않는다(독립 검토 F9). 숨겨진 그림자(게임 밖)에서는 내보내지 않는다(독립 검토 F6).
    [HarmonyPatch(typeof(ProjectileController), "CreateView", new[] { typeof(Projectile), typeof(Vector3?) })]
    static class EchoLaunchOriginPatch
    {
        static void Prefix(Projectile __0, ref Vector3? __1)
        {
            try
            {
                object boxed;
                var scope = EchoVisuals.Scope.Current(EchoVisuals.Stamp(), out boxed);
                if (scope == ScopeState.Empty || scope == ScopeState.Masked) return; // 정당한 무동작: 메아리 실행 중이 아니다
                var anchor = boxed as EchoLaunchAnchor;
                bool? mine = null;
                if (anchor != null && __0 != null && __0.Launcher != null && __0.Launcher.IsUnit) mine = ReferenceEquals(__0.Launcher.Unit, anchor.Caster);
                var bp = __0 == null ? null : __0.Blueprint;
                bool? followsTerrain = null; // null = 블루프린트를 읽을 수 없다(순수 규칙이 ReportUnknown 으로 돌려준다 — false 로 넘기지 않는다)
                if (bp != null) followsTerrain = bp.FollowTerrain;
                var use = anchor == null ? ShadowUse.Hidden : EchoVisuals.UseOf(anchor); // 앵커 없음 = 범위가 Anchored 가 아니다(규칙이 use 를 읽기 전에 Keep / ReportStale 로 끝난다)
                 
                var delivery = anchor == null ? DeliveryKind.Unknown : anchor.Delivery;
                var choice = EchoLaunchRule.Projectile(scope, __1.HasValue, mine, followsTerrain, delivery, use);
                switch (choice)
                {
                    case LaunchChoice.Keep:
                        return; // 정당한 무동작: 이 메아리의 본체가 쏜 것이 아니다 · 엔진이 좌표를 정했다 · 지형을 따라가는 투사체다 · 그림자가 이미 숨겨졌다
                    case LaunchChoice.KeepDelivery:
                        EchoVisuals.NoteKeptByDelivery(__0); // 정당한 무동작(센다): 전달 형식이 옮겨도 되는 목록에 없다 — 본체에서 나간다
                        return;
                    case LaunchChoice.ReportStale:
                        ShadowErrors.ReportOnce(EchoVisuals.Site, "stale-scope", "echo launch scope from another frame is still open - projectile origin left untouched");
                        return;
                    case LaunchChoice.ReportUnknown:
                        if (delivery == DeliveryKind.Unknown) ShadowErrors.ReportOnce(EchoVisuals.Site, "launch-delivery-unknown", "cannot tell how the echo spell delivers (its blueprint components are unreadable) - projectile origin left untouched");
                        else ShadowErrors.ReportOnce(EchoVisuals.Site, "launch-no-blueprint", "echo projectile has no blueprint (cannot tell whether it follows terrain) - projectile origin left untouched");
                        return;
                    case LaunchChoice.ReportGone:
                        EchoVisuals.ReportGone(true); // 그림자가 사라졌다 → 본체에서 나간다
                        return;
                    case LaunchChoice.Move:
                        break;
                    default:
                        throw new InvalidOperationException("unknown launch choice " + choice);
                }
                Vector3 at = EchoVisuals.LaunchPoint(anchor, bp.SourceBone);
                __1 = at;
                EchoVisuals.NoteMovedLaunch(__0, at);
            }
            catch (Exception e) { ShadowErrors.ReportException(EchoVisuals.Site, e); }
        }
    }

    // H13: AbilitySpawnFx.ResolveAnchor(anchor, context, selectedTarget) Postfix — 앵커가 시전자인 주문 FX 를 그림자 쪽으로.
    // 근거 ResolveAnchor IL_001A~0049: Caster(0) → context.MaybeCaster 래퍼. 호출처 = Spawn IL_006A · PositionFx IL_0011·0039 뿐.
    //   컨텍스트가 인자로 오므로 범위가 필요 없다(지연 생성·생성 뒤 위치 잡기처럼 Tick 밖에서 불려도 같은 판정). 대상 앵커(ClickedTarget·SelectedTarget)는 손대지 않는다.
    [HarmonyPatch(typeof(AbilitySpawnFx), "ResolveAnchor", new[] { typeof(AbilitySpawnFxAnchor), typeof(AbilityExecutionContext), typeof(TargetWrapper) })]
    static class EchoFxAnchorPatch
    {
        static void Postfix(AbilitySpawnFxAnchor __0, AbilityExecutionContext __1, ref TargetWrapper __result)
        {
            try
            {
                if (__0 != AbilitySpawnFxAnchor.Caster) return; // 정당한 무동작: 시전자 앵커가 아니다
                var anchor = EchoVisuals.AnchorOf(__1);
                var use = anchor == null ? ShadowUse.Hidden : EchoVisuals.UseOf(anchor); // 앵커 없음 = 규칙이 use 를 읽기 전에 Keep 으로 끝난다
                var choice = EchoLaunchRule.FxAnchor(true, __result != null, anchor != null, use);
                if (choice == FxAnchorChoice.Keep) return; // 정당한 무동작: 메아리 연출이 묶이지 않았다 · 엔진이 앵커를 못 냈다 · 그림자가 이미 숨겨졌다(엔진 앵커 = 본체 그대로)
                if (choice == FxAnchorChoice.ReportGone) { EchoVisuals.ReportGone(false); return; } // 그림자가 사라졌다 → 엔진 앵커 그대로
                __result = EchoVisuals.FxAnchor(anchor);
                EchoVisuals.NoteMovedFxAnchor();
            }
            catch (Exception e) { ShadowErrors.ReportException(EchoVisuals.Site, e); }
        }
    }
}
