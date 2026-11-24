using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics;
using TargetWrapper = Kingmaker.Utility.TargetWrapper;
using UnityEngine;
using ModContext = WotrClassMod.Common.ModContext;

namespace ShadowSorcerer
{
    // 메아리 한 발의 「보이는 출발점」(비저장). 활성 무대가 메아리 실행 컨텍스트별로 보관한다.
    // 본체(Caster)는 소유자 대조용: 투사체의 발사자가 이 유닛일 때만 출발점을 옮긴다. Slot = 그림자 자리 번호(1부터), Shadow = 그 자리에 드러난 시각 전용 그림자 유닛.
    public sealed class EchoLaunchAnchor
    {
        public readonly UnitEntityData Caster;
        public readonly int Slot;
        public readonly UnitEntityData Shadow;
        // 이 메아리의 능력 실행 과정(H11 이 첫 Tick 에 적는다 — 흩어짐이 주문 종료를 기다릴 때 읽는다). null = 아직 돌지 않았다.
        public AbilityExecutionProcess Process;
        // 발사가 실패해 주문이 돌지 않는 메아리(EchoStage.AfterFire 가 적는다).
        public bool Dud;
         
        public readonly DeliveryKind Delivery;
        public EchoLaunchAnchor(UnitEntityData caster, int slot, UnitEntityData shadow, DeliveryKind delivery)
        {
            if ((object)caster == null) throw new ArgumentNullException("caster");
            if ((object)shadow == null) throw new ArgumentNullException("shadow");
            Caster = caster; Slot = slot; Shadow = shadow; Delivery = delivery;
        }
    }

     
    // 계약: 여기의 어떤 실패도 메아리 판정을 막지 않는다 — 공개 진입점은 던지지 않고, 실패는 오류 줄 1회(ReportOnce) 뒤 「본체 기준(연출 없음)」으로 떨어진다.
    //       반대로 여기는 메아리 규칙(RuleCastSpell·AbilityData·대기열 상태)에 쓰지 않는다.
    public static class EchoVisuals
    {
        // 오류 보고 위치 이름(주입 사이트 아님 — ErrorLedger.Sites 는 결함 주입 대상 목록이고 연출에는 주입하지 않는다).
        public const string Site = "echo-visual";

        // 메아리 실행 범위(H11). 게임 메인 스레드 하나.
        public static readonly EchoLaunchScope Scope = new EchoLaunchScope();

        // 점검 전용 관측(점검 러너가 돌 때만 쌓는다): 옮긴 투사체와 옮긴 좌표 · 옮긴 FX 앵커 수.
        public static readonly List<Tuple<Projectile, Vector3>> CheckMovedLaunches = new List<Tuple<Projectile, Vector3>>();
        public static int MovedLaunches { get; private set; }
        public static int MovedFxAnchors { get; private set; }

        public static long Stamp() { return Time.frameCount; }

        public static EchoLaunchAnchor AnchorOf(MechanicsContext ctx)
        {
            return EchoStage.AnchorOf(ctx); // 정당: 메아리가 아니거나 연출이 묶이지 않은 컨텍스트
        }

        // EchoQueue.Fire 가 Rulebook.Trigger 직전에 부른다. 던지지 않는다.
        // 이 작업의 index 번째 그림자가 드러나 있으면 그 메아리의 컨텍스트에 출발점을 묶는다. 드러나 있지 않으면(연출 없음 — 사유는 등장 때 남았다) 묶지 않는다 → 본체에서 나간다.
        public static void Bind(MechanicsContext echoContext, EchoJob job, int index)
        {
            try
            {
                if (echoContext == null) throw new ArgumentNullException("echoContext");
                if (job == null) throw new ArgumentNullException("job");
                LogPatchOwnersOnce();
                var lineOrCone = LineOrConeOf(job);
                // 2차 독립 검토 중요-1: 규칙 발행 직전에 드러난 그림자가 본체 자신의 원뿔·선 밖인지 다시 본다 — 아니면 무대를 끈다(그림자 전부 게임 밖). 그러면 아래 ShadowFor 가 null.
                EchoStage.ClearForFire(job, lineOrCone);
                var shadow = EchoStage.ShadowFor(job, index);
                if ((object)shadow == null) return; // 정당한 무동작: 이 메아리에는 그림자가 없다(등장하지 않았다 · 꺼졌다 · 방금 자리 판정으로 껐다)
                var anchor = new EchoLaunchAnchor(job.Caster, index, shadow, DeliveryOf(job));
                EchoStage.NoteAnchor(job, echoContext, anchor);
            }
            catch (Exception e)
            {
                ShadowErrors.ReportOnce(Site, "bind-" + e.GetType().Name, "echo visual not bound (" + e.GetType().Name + ": " + e.Message + ") - this echo launches from the caster as before");
            }
        }

        // 메아리 주문(그 작업의 AbilityData 블루프린트)이 원뿔·선 투사체로 전달되는가: 컴포넌트마다 AbilityDeliverProjectile 이고 Type ≠ Simple 인가를 순수 AnyLineOrCone 에 넘긴다.
        //   null = 블루프린트·컴포넌트 목록을 읽을 수 없거나 빈 칸이 있다(「아니다」로 넘기지 않는다 — H12 가 ReportUnknown 으로 엔진 값을 둔다). 던지지 않는다.
        internal static bool? LineOrConeOf(EchoJob job)
        {
            var data = job == null ? null : job.Data;
            var bp = data == null ? null : data.Blueprint;
            var comps = bp == null ? null : bp.ComponentsArray;
            if (comps == null) return null;
            var parts = new bool?[comps.Length];
            for (int k = 0; k < comps.Length; k++)
            {
                var c = comps[k];
                if (c == null) { parts[k] = null; continue; }
                var p = c as AbilityDeliverProjectile;
                parts[k] = p != null && p.Type != AbilityProjectileType.Simple;
            }
            return EchoLaunchRule.AnyLineOrCone(parts);
        }

         
        //   빈 칸(Unknown)을 순수 FoldDelivery 에 넘긴다. 블루프린트·컴포넌트 목록을 읽을 수 없으면 Unknown(H12 가 ReportUnknown 으로 엔진 값을 둔다). 던지지 않는다.
        internal static DeliveryKind DeliveryOf(EchoJob job)
        {
            var data = job == null ? null : job.Data;
            var bp = data == null ? null : data.Blueprint;
            var comps = bp == null ? null : bp.ComponentsArray;
            if (comps == null) return DeliveryKind.Unknown;
            var parts = new DeliveryKind?[comps.Length];
            for (int k = 0; k < comps.Length; k++)
            {
                var c = comps[k];
                if (c == null) { parts[k] = DeliveryKind.Unknown; continue; }
                var p = c as AbilityDeliverProjectile;
                if (p != null) { parts[k] = p.Type == AbilityProjectileType.Simple ? DeliveryKind.Simple : DeliveryKind.LineOrCone; continue; }
                if (c is AbilityDeliverChain) { parts[k] = DeliveryKind.Chain; continue; }
                parts[k] = c is AbilityDeliverEffect ? DeliveryKind.Other : (DeliveryKind?)null; // 정당: 전달 컴포넌트가 아니다(세지 않는다)
            }
            return EchoLaunchRule.FoldDelivery(parts);
        }

        // 전달 형식이 「옮겨도 된다」 목록에 없어 본체에서 내보낸 메아리 투사체(규칙 KeepDelivery — S15 의 전제 확인용). 점검 러너가 돌 때만 투사체를 쌓는다.
        public static int KeptByDelivery { get; private set; }
        public static readonly List<Projectile> CheckKeptLaunches = new List<Projectile>();
        internal static void NoteKeptByDelivery(Projectile p)
        {
            KeptByDelivery++;



        }

        // 이 앵커의 그림자가 지금 어떤 상태인가(H12·H13 의 판정 입력 — 순수 EchoLaunchRule 이 고른다).
        //   Gone = 유닛이 사라지는 중 · Shown = 묶음의 그 자리에 드러나 있고 게임 안 · Hidden = 그 밖(흩어져 숨겨졌다 / 묶음이 지워졌거나 다시 만들어졌다).
        // 독립 검토 F6: 묶인 뒤에 숨겨진 그림자(게임 밖 — 뷰가 꺼져 있다)에서 투사체·FX 가 나가지 않게 한다. 숨겨졌으면 엔진 값(본체) 그대로다.
        internal static ShadowUse UseOf(EchoLaunchAnchor a)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (ShadowDoubles.Going(a.Shadow)) return ShadowUse.Gone;
            return ShadowDoubles.IsShown(a.Caster, a.Slot, a.Shadow) ? ShadowUse.Shown : ShadowUse.Hidden;
        }

        // H12 가 부른다(순수 규칙이 Move 를 고른 뒤에만): 이 앵커의 투사체 출발 좌표 = 그림자의 그 뼈대(투사체 블루프린트의 SourceBone — 대부분 왼손바닥).
        // 뼈대가 없으면 ShadowCast.LaunchPoint 가 오류 줄 1회 + 그림자 위치 + 높이 다이얼로 대체한다.
        internal static Vector3 LaunchPoint(EchoLaunchAnchor a, string sourceBone)
        {
            return ShadowCast.LaunchPoint(a.Shadow, sourceBone);
        }

        // H13 이 부른다(순수 규칙이 Move 를 고른 뒤에만): 시전자 앵커 주문 FX 가 붙을 곳 = 그림자 유닛.
        internal static TargetWrapper FxAnchor(EchoLaunchAnchor a)
        {
            return new TargetWrapper(a.Shadow);
        }

        // 그림자가 사라진 뒤에 온 발사·FX(순수 규칙의 ReportGone): 오류 줄 1회, 엔진 값 그대로.
        internal static void ReportGone(bool projectile)
        {
            if (projectile) ShadowErrors.ReportOnce(Site, "launch-shadow-gone", "echo projectile launched after its shadow unit was destroyed - origin left on the caster");
            else ShadowErrors.ReportOnce(Site, "fx-shadow-gone", "echo spell FX resolved after its shadow unit was destroyed - anchor left on the caster");
        }

        // H11 이 매 Tick 부른다: 닫히지 않고 샌 실행 범위를 버린 일이 새로 있었으면 한 번 보고한다(독립 검토 F7 — 버린 수를 아무도 읽지 않았다).
        internal static void ReportScopeLeaks()
        {
            int n = Scope.TakeNewLeaks();
            if (n > 0) ShadowErrors.ReportOnce(Site, "scope-leak", "echo launch scope: " + n + " frame(s) left open by an earlier Unity frame were dropped (a Tick finalizer did not run?) - dropped total " + Scope.LeakedDropped);
        }

        internal static void NoteMovedLaunch(Projectile p, Vector3 at)
        {
            MovedLaunches++;



        }

        internal static void NoteMovedFxAnchor() { MovedFxAnchors++; }

        // ---- 다른 모드와의 공존 기록(정찰 미확인 9) ----
        // 2026-11-17 오프라인 조사(설치본 Mods 폴더 62 dll, dnlib 로 [HarmonyPatch] 인자와 문자열 직독):
        //   AbilityExecutionProcess.Tick = DarkCodex Patch_ResourcefulCaster Postfix 1건(원본을 건너뛰지 않는다) · ProjectileController.CreateView = 없음 · AbilitySpawnFx.ResolveAnchor = 없음.
        // 런타임에는 첫 메아리 때 실제 패치 목록을 한 번 읽어 남긴다: 우리 것 / 남의 Postfix·Finalizer(정보 줄) / 남의 Prefix·Transpiler(원본을 건너뛰거나 고칠 수 있다 → 오류 줄).
        static bool patchOwnersLogged;

        static void LogPatchOwnersOnce()
        {
            if (patchOwnersLogged) return;
            patchOwnersLogged = true;
            foreach (var m in PatchedMethods())
            {
                var info = Harmony.GetPatchInfo(m);
                string name = m.DeclaringType.Name + "." + m.Name;
                if (info == null) { ShadowErrors.Report(Site, "no Harmony patch info on " + name + " - the echo visual patch is not applied"); continue; }
                var mine = ModContext.HarmonyId;
                var friendly = info.Postfixes.Concat(info.Finalizers).Where(p => p.owner != mine).Select(p => p.owner).Distinct().ToArray();
                var risky = info.Prefixes.Concat(info.Transpilers).Where(p => p.owner != mine).Select(p => p.owner).Distinct().ToArray();
                bool ours = info.Prefixes.Concat(info.Postfixes).Concat(info.Finalizers).Any(p => p.owner == mine);
                if (!ours) ShadowErrors.Report(Site, "our patch is missing on " + name + " - the echo visual patch is not applied");
                ModContext.Log("ShadowSorcerer: echo visual patch owners on " + name + ": ours=" + ours + ", other postfix/finalizer=[" + string.Join(",", friendly) + "], other prefix/transpiler=[" + string.Join(",", risky) + "]");
                if (risky.Length > 0) ShadowErrors.Report(Site, "another mod patches " + name + " with a prefix/transpiler (" + string.Join(",", risky) + ") - the echo launch origin may not apply (unverified combination)");
            }
        }

        static IEnumerable<MethodBase> PatchedMethods()
        {
            yield return Require(AccessTools.Method(typeof(AbilityExecutionProcess), "Tick", new Type[0]), "AbilityExecutionProcess.Tick");
            yield return Require(AccessTools.Method(typeof(ProjectileController), "CreateView", new[] { typeof(Projectile), typeof(Vector3?) }), "ProjectileController.CreateView");
            yield return Require(AccessTools.Method(typeof(AbilitySpawnFx), "ResolveAnchor", new[] { typeof(AbilitySpawnFxAnchor), typeof(Kingmaker.UnitLogic.Abilities.AbilityExecutionContext), typeof(TargetWrapper) }), "AbilitySpawnFx.ResolveAnchor");
        }

        static MethodBase Require(MethodBase m, string name)
        {
            if (m == null) throw new MissingMethodException("ShadowSorcerer: " + name + " not found");
            return m;
        }
    }
}
