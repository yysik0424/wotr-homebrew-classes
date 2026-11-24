using System;
using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Mechanics;

namespace ShadowSorcerer
{
     
    // 두 패치 모두 메아리 표식(EchoMarker)이 있는 컨텍스트에서만 값을 바꾼다 — 본편 그림자 주문의 본 시전, 다른 직업·다른 모드의 시전, 메아리가 아닌 시전은 판독 뒤 그대로 돌아간다.
    // 판정 불가(표식 모양이 깨진 목록)는 한 번 보고하고 엔진 값 그대로 둔다. 예외 = 보고 + 엔진 값 그대로. 원래 메서드는 건너뛰지 않는다(void).

    // H7: ShadowSpellLogicExtension.TryApplyShadowDamageFactor(RulebookEvent, UnitEntityData, int) Postfix — 메아리 피해 × EchoDamagePercent.
    // 근거: 엔진 그림자 피해 감산의 유일한 호출처가 RuleCalculateDamage.CalculateDamageValue IL_0311 이고, 이 public 오버로드가 Reason.Context 로 컨텍스트를 정한다(IL_0000~0021).
    //   불신 성공이면 __result 에 이미 그림자 계수가 곱해져 있다 → 실패 50% / 성공 25%. 컨텍스트 = 피해 규칙의 Reason.Context(버프·장판의 자식 컨텍스트 포함 — 표식은 자식에 복사된다).
    [HarmonyPatch(typeof(ShadowSpellLogicExtension), nameof(ShadowSpellLogicExtension.TryApplyShadowDamageFactor), new[] { typeof(RulebookEvent), typeof(UnitEntityData), typeof(int) })]
    static class EchoDamagePatch
    {
        static void Postfix(RulebookEvent __0, ref int __result)
        {
            try
            {
                var ctx = __0.Reason.Context;
                if (ctx == null) return; // 정당한 무동작: 컨텍스트 없는 피해(무기 공격 등) — 메아리 효과는 항상 컨텍스트를 이유로 갖는다(엔진 그림자 계수와 같은 전제)
                var mark = EchoMarker.Read(ctx);
                if (mark == EchoMark.NotEcho) return; // 정당한 무동작: 메아리가 아니다
                if (mark == EchoMark.Unknown)
                {
                    ShadowErrors.ReportOnce(ErrorLedger.SiteShadowFactor, "echo-mark-unknown-damage", "shadow disbelief list has an unreadable echo mark - echo damage scaling skipped (engine damage kept)");
                    return;
                }
                __result = EchoStrengthRule.Damage(__result, mark);
            }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteShadowFactor, e); }
        }
    }

    // H8: MechanicsContext.TryAffectByShadow(UnitEntityData, bool checkChance) Prefix — 메아리 컨텍스트는 확률을 굴리지 않는다.
    // 근거: checkChance 가 참인 호출처는 ShadowSpellLogicExtension.SkipBecauseOfShadow(버프·능력치 피해·흡수·치유·전투 기동 7 규칙)와 AreaEffectEntityData.UpdateUnit(장판 진입) 둘뿐이고,
    //   checkChance 가 거짓이면 엔진이 불신 성공에 거짓을 돌려준다(IL_0127~013C) → 불신 성공 시 그 효과는 적용되지 않는다. 불신 실패는 그대로 참(온전히 적용).
    [HarmonyPatch(typeof(MechanicsContext), nameof(MechanicsContext.TryAffectByShadow))]
    static class EchoNoChancePatch
    {
        static void Prefix(MechanicsContext __instance, ref bool __1)
        {
            try
            {
                if (!__1) return; // 정당한 무동작: 엔진이 확률 없이 묻는 호출(피해 계수·그림자 소환수)
                var mark = EchoMarker.Read(__instance);
                if (mark == EchoMark.NotEcho) return; // 정당한 무동작: 메아리가 아니다
                if (mark == EchoMark.Unknown)
                {
                    ShadowErrors.ReportOnce(ErrorLedger.SiteShadowFactor, "echo-mark-unknown-chance", "shadow disbelief list has an unreadable echo mark - engine chance roll kept");
                    return;
                }
                __1 = EchoStrengthRule.CheckChance(__1, mark);
            }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteShadowFactor, e); }
        }
    }
}
