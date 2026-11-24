using System;
using HarmonyLib;
using Kingmaker.Controllers;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

namespace ShadowSorcerer
{
     
    // 근거: OnAction IL_019B~01F9 가 new RuleCastSpell(Ability, Target) 후 동기 Rulebook.Trigger(부록 A-1d). 범위 = 그 명령의 AbilityData.
    // 실패해도 원래 실행은 막지 않는다(Prefix 는 void) — 범위가 열리지 않으면 그 시전은 출처 None(메아리 없음) + 보고.
    [HarmonyPatch(typeof(UnitUseAbility), "OnAction")]
    static class OriginScopePatch
    {
        static void Prefix(UnitUseAbility __instance, out bool __state)
        {
            __state = false;
            try
            {
                ShadowErrors.MaybeFault(ErrorLedger.SiteOriginMarker);
                var data = __instance.Ability;
                if (data == null) { ShadowErrors.Report(ErrorLedger.SiteOriginMarker, "UnitUseAbility.OnAction without ability data - scope not opened"); return; }
                EchoRegistry.Claims.Open(OriginKind.Command, data);
                __state = true;
            }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteOriginMarker, e); }
        }

        static Exception Finalizer(Exception __exception, bool __state)
        {
            try { EchoRegistry.Claims.CloseIfOpened(__state); }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteOriginMarker, e); }
            return __exception;
        }
    }

    // H2 (J15 승인): RuleCastSpell..ctor(AbilityData, TargetWrapper) Prefix(진입 claim) + Postfix(표식).
    // 생성자 Prefix 는 본문 IL_0000 앞 = CreateExecutionContext(IL_0023) → CalculateParams 앞 → 같은 데이터의 중첩 생성보다 먼저 claim(r1 2.3, r2 B).
    // RuleCastSpell(Ability, TargetWrapper) 는 IL_0008 에서 이 생성자를 부른다(무기 명중 전달 TouchSpellsController IL_0069) → 여기 하나로 덮인다.
    [HarmonyPatch(typeof(RuleCastSpell), MethodType.Constructor, new[] { typeof(AbilityData), typeof(TargetWrapper) })]
    static class OriginClaimPatch
    {
        static void Prefix(AbilityData __0, out ClaimToken __state)
        {
            __state = null;
            try
            {
                ShadowErrors.MaybeFault(ErrorLedger.SiteOriginMarker);
                __state = EchoRegistry.Claims.EnterCtor(__0);
            }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteOriginMarker, e); }
        }

        static void Postfix(RuleCastSpell __instance, ClaimToken __state)
        {
            try { EchoRegistry.Claims.CompleteCtor(__state, __instance, (rule, kind) => EchoRegistry.MarkOrigin((RuleCastSpell)rule, kind)); }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteOriginMarker, e); }
        }
    }

    // H3: TouchSpellsController.OnEventDidTrigger(RuleAttackWithWeapon) Prefix + Finalizer — 무기·자연 무기 명중 접촉 전달(판정 1.3).
    // 근거 IL_0000~006E: 공격자의 UnitPartTouch 가 있고 명중이면 new RuleCastSpell(part.Ability, 대상) 후 Trigger. 범위 데이터 = part.Ability.Data(같은 참조).
    [HarmonyPatch(typeof(TouchSpellsController), nameof(TouchSpellsController.OnEventDidTrigger), new[] { typeof(RuleAttackWithWeapon) })]
    static class TouchDeliveryScopePatch
    {
        static void Prefix(RuleAttackWithWeapon __0, out bool __state)
        {
            __state = false;
            try
            {
                ShadowErrors.MaybeFault(ErrorLedger.SiteOriginMarker);
                var part = __0 == null || __0.Initiator == null ? null : __0.Initiator.Get<UnitPartTouch>();
                if (part == null || part.Ability == null) return; // 정당한 무동작: 전하가 없다(본편도 IL_0012 에서 그냥 돌아간다)
                if (part.Ability.Data == null) { ShadowErrors.Report(ErrorLedger.SiteOriginMarker, "touch charge without ability data - delivery scope not opened"); return; }
                EchoRegistry.Claims.Open(OriginKind.TouchWeaponDelivery, part.Ability.Data);
                __state = true;
            }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteOriginMarker, e); }
        }

        static Exception Finalizer(Exception __exception, bool __state)
        {
            try { EchoRegistry.Claims.CloseIfOpened(__state); }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteOriginMarker, e); }
            return __exception;
        }
    }

    // H6 (J17 승인): AbilityTargetsAround.Select(AbilityExecutionContext, TargetWrapper) Prefix — 지점을 지정할 수 없는 범위 주문(Owner/Unit 앵커)의 중심 고정.
    // 근거 Select IL_0014~002F: 중심 = 인자 래퍼가 유닛이면 Unit.EyePosition, 아니면 Point. 이 메서드의 범위 선택에만 인자를 바꾼다(주문 대상 래퍼 자체는 그대로).
    // 메아리 컨텍스트가 아니면 무동작. 태그는 있는데 중심이 없으면 보고 + 원래 값. 예외 = 보고 + 원래 값.
    [HarmonyPatch(typeof(AbilityTargetsAround), nameof(AbilityTargetsAround.Select))]
    static class FrozenCenterPatch
    {
        static void Prefix(AbilityExecutionContext __0, ref TargetWrapper __1)
        {
            try
            {
                var tag = EchoRegistry.TagOf(__0);
                var choice = FrozenCenterRule.Choose(tag != null, tag != null && tag.NeedsFrozenCenter, tag != null && tag.FrozenCenter.HasValue);
                if (choice == CenterChoice.KeepLive) return; // 메아리 고정 중심 대상이 아니면 무동작(주입도 메아리에서만 소비된다)
                ShadowErrors.MaybeFault(ErrorLedger.SiteFrozenCenter);
                if (choice == CenterChoice.MissingFrozen) { ShadowErrors.Report(ErrorLedger.SiteFrozenCenter, "echo needs a frozen centre but has none - using the live centre"); return; }
                var centre = FrozenCenterRule.Pick(choice, __1.Point, tag.FrozenCenter.Value);
                __1 = new TargetWrapper(centre, null, null);



            }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteFrozenCenter, e); }
        }
    }
}
