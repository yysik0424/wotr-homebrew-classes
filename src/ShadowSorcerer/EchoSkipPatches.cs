using System;
using HarmonyLib;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace ShadowSorcerer
{
     
    // 엔진은 이 두 액션에 그림자 판정을 걸지 않는다(RunAction 본문에 그림자 호출 0) → 메아리 컨텍스트에서 그 대상이 그 메아리의 불신에 성공했을 때만 원래 동작을 건너뛴다.
    // 판단은 순수 EchoSkipRule. 메아리 표식이 없으면 불신을 읽지도 굴리지도 않고 원래 동작(본편·다른 직업·다른 모드의 즉사·해제는 그대로).

    // 우리가 엔진에 불신을 굴리게 하는 동안의 범위. 점검용 DiceControl 이 「이 내성은 불신 내성」임을 안다(엔진 쪽 그림자 검사는 RuleCalculateDamage · RuleCanApplyBuff 안에서만 일어난다).
    public static class EchoDisbeliefScope
    {
        [ThreadStatic] static int depth;




        // 엔진 TryAffectByShadow(target, checkChance: false): 그 대상의 항목이 있으면 다시 굴리지 않고(IL_0018~0037), 없으면 의지 내성을 한 번 굴려 목록에 넣는다(IL_003C~00D7).
        // 참 = 영향 받음(불신 실패), 거짓 = 불신 성공.
        public static bool Roll(MechanicsContext ctx, UnitEntityData unit)
        {
            depth++;
            try { return ctx.TryAffectByShadow(unit, false); }
            finally { depth--; }
        }
    }

    static class EchoSkip
    {
        // 참 = 원래 동작 실행, 거짓 = 건너뜀. unitRequired: 그 액션이 유닛 대상 없이는 뜻이 없는가(즉사 = 참, 해제 = 거짓 — 지점·장판 해제는 무변경).
        internal static bool RunOriginal(bool unitRequired, string what)
        {
            var data = ContextData<MechanicsContext.Data>.Current;
            var ctx = data == null ? null : data.Context;
            if (ctx == null)
            {
                // 판정 불가: 표식을 읽을 컨텍스트가 없다. 「메아리 아님」으로 읽지 않고 한 번 보고한 뒤 원래 동작.
                ShadowErrors.ReportOnce(ErrorLedger.SiteShadowFactor, "echo-skip-no-context-" + what, what + " action ran without a mechanics context - echo disbelief not judged (original action kept)");
                return true;
            }
            var mark = EchoMarker.Read(ctx);
            var unit = data.CurrentTarget == null ? null : data.CurrentTarget.Unit;
            // 불신 상태는 메아리일 때만 읽는다. 메아리가 아니면 규칙이 상태를 보지 않는다(Unreadable 은 「읽지 않았다」는 자리 채움).
            var state = mark == EchoMark.Echo ? StateOf(ctx, unit, unitRequired) : EchoDisbelief.Unreadable;
            var action = EchoSkipRule.Decide(mark, state);
            if (action == EchoSkipAction.RollFirst) action = EchoSkipRule.AfterRoll(mark, EchoDisbeliefScope.Roll(ctx, unit));
            switch (action)
            {
                case EchoSkipAction.Keep: return true;
                case EchoSkipAction.Skip: return false;
                case EchoSkipAction.KeepAndReport:
                    ShadowErrors.ReportOnce(ErrorLedger.SiteShadowFactor, "echo-skip-unjudged-" + what + "-" + mark + "-" + state, what + " action in an echo context could not be judged (mark " + mark + ", disbelief " + state + ") - original action kept");
                    return true;
                default: throw new InvalidOperationException("ShadowSorcerer: echo skip action " + action + " left unresolved");
            }
        }

        static EchoDisbelief StateOf(MechanicsContext ctx, UnitEntityData unit, bool unitRequired)
        {
            if (unit == null) return unitRequired ? EchoDisbelief.Unreadable : EchoDisbelief.NoUnitTarget;
            if (!ctx.IsShadow) return EchoDisbelief.Unreadable; // 표식은 있는데 그림자 계수가 없다 — 엔진이 불신을 굴릴 수 없다(TryAffectByShadow IL_000E 가 거짓을 돌려준다)
            return EchoSkipRule.FromEntry(EchoMarker.DisbeliefOf(ctx, unit));
        }
    }

    // H9: ContextActionKill.RunAction() Prefix. 예외 = 보고 + 원래 동작.
    [HarmonyPatch(typeof(ContextActionKill), nameof(ContextActionKill.RunAction))]
    static class EchoKillSkipPatch
    {
        static bool Prefix()
        {
            try { return EchoSkip.RunOriginal(true, "kill"); }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteShadowFactor, e); return true; }
        }
    }

    // H10: ContextActionDispelMagic.RunAction() Prefix. 유닛 대상이 없는 해제(지점·장판 — RunAction IL_0026~00B4)는 무변경. 예외 = 보고 + 원래 동작.
    [HarmonyPatch(typeof(ContextActionDispelMagic), nameof(ContextActionDispelMagic.RunAction))]
    static class EchoDispelSkipPatch
    {
        static bool Prefix()
        {
            try { return EchoSkip.RunOriginal(false, "dispel"); }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteShadowFactor, e); return true; }
        }
    }
}
