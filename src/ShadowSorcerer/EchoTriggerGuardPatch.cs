using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Mechanics;
using WotrClassMod.Common;

namespace ShadowSorcerer
{
    // H5: 「시전 시」 반응·「다음 주문」 소비 가드. 모든 대상의 첫 인자는 이벤트다.
    // 메아리 규칙/컨텍스트/생성 중이면 GuardHit(행의 S14 단계 이름) 후 return false(그 컴포넌트 본문을 건너뜀 — 부작용은 원래 시전에서만 1번).
    // 해석 실패 = Report + BuildFailed(그 행만 빠진다 — PatchAll 전체를 깨지 않는다). 본문 예외 = Report + return true(본편 동작 유지).
    [HarmonyPatch]
    static class EchoTriggerGuardPatch
    {
        static readonly Dictionary<MethodBase, GuardRow> rows = new Dictionary<MethodBase, GuardRow>();
        public static int Resolved { get { return rows.Count; } }

        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var r in TriggerGuardPlan.Targets)
            {
                MethodBase m = null;
                try
                {
                    var t = AccessTools.TypeByName(r.TypeFullName);
                    var a = AccessTools.TypeByName(r.ArgTypeFullName);
                    if (t != null && a != null) m = t.GetMethod(r.Method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new[] { a }, null);
                }
                catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteTriggerGuard, e); }
                if (m == null)
                {
                    ModContext.BuildFailed = true;
                    ShadowErrors.Report(ErrorLedger.SiteTriggerGuard, "trigger guard target not found: " + r.TargetLine);
                    continue;
                }
                rows[m] = r;
                yield return m;
            }
        }

        static bool Prefix(MethodBase __originalMethod, object __0)
        {
            try
            {
                GuardRow r;
                if (!rows.TryGetValue(__originalMethod, out r)) { ShadowErrors.ReportOnce(ErrorLedger.SiteTriggerGuard, __originalMethod.Name, "trigger guard called for an unknown method " + __originalMethod.DeclaringType + "." + __originalMethod.Name); return true; }
                var cast = __0 as RuleCastSpell;
                bool? ruleEcho = cast == null ? (bool?)null : EchoRegistry.IsEcho(cast);
                MechanicsContext ctx = cast != null ? cast.Context : (__0 as RuleApplySpell)?.Context;
                bool? ctxEcho = ctx == null ? (bool?)null : EchoRegistry.IsEcho(ctx);
                bool unread;
                var calc = __0 as RuleCalculateAbilityParams;
                bool echoCalculation = EchoAbilityIdentity.IsEchoCalculation(calc);
                bool run = TriggerGuardPlan.RunOriginal(r.Kind, ruleEcho, ctxEcho, echoCalculation, out unread);
                if (unread) ShadowErrors.ReportOnce(ErrorLedger.SiteTriggerGuard, r.Step + "|unread", "trigger guard " + r.Step + " could not read its " + r.Kind + " fact (argument " + (__0 == null ? "null" : __0.GetType().Name) + ") - not blocked");
                if (run) return true;
                ShadowErrors.MaybeFault(ErrorLedger.SiteTriggerGuard);
                ShadowErrors.GuardHit(r.Step);
                return false;
            }
            catch (Exception e)
            {
                ShadowErrors.ReportException(ErrorLedger.SiteTriggerGuard, e);
                return true;
            }
        }
    }
}
