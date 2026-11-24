using System;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using WotrClassMod.Common;

namespace BulwarkMage
{
     
    // 예외 계약: 엔진의 ActionList.Run / ConditionsChecker 는 요소의 예외를 삼키고 계속하므로(ConditionsChecker.Check @0092~00BC) 예외는 우리 쪽에서 잡아 BulwarkErrors 로 센다 —
    // 본문 try, 맨 앞 MaybeFault(사이트), catch 는 ReportException 뒤 안전한 결과(행동: 아무것도 안 함 / 조건: 거짓 = 발동 안 함). 엔진으로 다시 던지지 않는다.
    // 불발 사유 형식 DudReason 은 순수 RebukeOutcome.cs 에 있다(시험 exe 가 효과 없음 판정과 함께 실행한다).

    // 불발·효과 없음 기록(전투 기록 + 모드 로그 DUD — 오류가 아니다). 사용 처리(하루 −1·라운드 표지)는 기록 앞의 행동이 이미 끝냈다 — 여기서는 자원을 보지도 바꾸지도 않는다.
    public static class RebukeDudLog
    {
        // 사유 → 문구 키(접두 없이). 키가 없는 사유 = 던짐(조용한 빈 문구 금지).
        public static string ReasonKey(DudReason r)
        {
            switch (r)
            {
                case DudReason.AttackerDown: return "dud.down";
                case DudReason.NoDamage: return "dud.nodamage";
                case DudReason.SavedOrImmune: return "dud.saved";
                case DudReason.NoDamageSaved: return "dud.nodamagesaved";
                case DudReason.NotPushed: return "dud.notpushed";
                case DudReason.NothingToDispel: return "dud.nothingtodispel";
                case DudReason.DispelFailed: return "dud.dispelfailed";
                case DudReason.DispelNotCast: return "dud.dispelnotcast";
                case DudReason.Untargetable: return "dud.untargetable";
            }
            throw new ArgumentOutOfRangeException("r", r, "BulwarkMage: dud reason without a text key");
        }

        public static string Line(string effectId, DudReason reason)
        {
            string effect = Strings.Resolve(ModContext.StringPrefix + effectId.ToLowerInvariant() + ".name");
            string text = Strings.Resolve(ModContext.StringPrefix + ReasonKey(reason));
            return string.Format(Strings.Resolve(ModContext.StringPrefix + "log.dud"), effect, text);
        }

        // A3: 살아 있는 공격자에게 효과 없음 — DUD +1 · 전투 기록 1줄(받는 처리기가 없으면 모드 로그).
        public static void NoEffect(string site, string effectId, DudReason reason, UnitEntityData caster, UnitEntityData attacker, string detail)
        {
            if (string.IsNullOrEmpty(effectId)) throw new ArgumentException("BulwarkMage: no-effect log without an effect id", "effectId");
            string line = Line(effectId, reason);
            BulwarkErrors.Dud(site, effectId + " " + reason + " owner " + (caster == null ? "<none>" : caster.CharacterName) + " attacker " + (attacker == null ? "<none>" : attacker.CharacterName) + (detail == null ? "" : " (" + detail + ")"));
            bool delivered = EventBus.RaiseEvent<ILogMessageUIHandler>(h => h.HandleLogMessage(line), true);
            if (!delivered) ModContext.Log("BulwarkMage: rebuke no effect (combat log not available): " + line);
        }
    }

    // 불발 기록: 전투 기록 「방패 반격 불발: <효과> — <사유>」 + 모드 로그(DUD, 오류 아님) + 점검 관측(불발 시점 자원). 하루 사용은 이 행동 앞의 ContextSpendResource 가 이미 썼다.
    // 사유 = 공격자 쓰러짐(T5) · 해제 반격 대상 불가(A4 — ContextConditionRebukeCanTarget 거짓 분기).
    public class ContextActionLogRebukeDud : ContextAction
    {
        public DudReason Reason;
        public string EffectId;

        public override string GetCaption() { return "BulwarkMage: log rebuke dud (" + EffectId + ", " + Reason + ")"; }

        public override void RunAction()
        {
            try
            {
                BulwarkErrors.MaybeFault(ErrorLedger.SiteDudAction);
                if (string.IsNullOrEmpty(EffectId)) throw new InvalidOperationException("ContextActionLogRebukeDud without an EffectId");
                var context = Context;
                UnitEntityData caster = context == null ? null : context.MaybeCaster;
                if (caster == null) { BulwarkErrors.Report(ErrorLedger.SiteDudAction, "dud for " + EffectId + " without a caster in the context (nothing logged)"); return; }
                var res = Rebuke.Resource;
                if (res == null) { BulwarkErrors.Report(ErrorLedger.SiteDudAction, "dud for " + EffectId + " before the rebuke resource was built"); return; }
                BulwarkErrors.Probe.RecordDud(caster.UniqueId, caster.Resources.GetResourceAmount(res));
                var tw = Target;
                UnitEntityData attacker = ReferenceEquals(tw, null) ? null : tw.Unit;
                string line = RebukeDudLog.Line(EffectId, Reason);
                BulwarkErrors.Dud(ErrorLedger.SiteDudAction, EffectId + " " + Reason + " owner " + caster.CharacterName + " attacker " + (attacker == null ? "<none>" : attacker.CharacterName));
                // 본편 GameLog.RunAction 과 같은 경로(귀검사 StandAbsorb 선례): ILogMessageUIHandler → 전투 기록. 받는 처리기가 없으면 모드 로그로.
                bool delivered = EventBus.RaiseEvent<ILogMessageUIHandler>(h => h.HandleLogMessage(line), true);
                if (!delivered) ModContext.Log("BulwarkMage: rebuke dud (combat log not available): " + line);
            }
            catch (Exception e)
            {
                BulwarkErrors.ReportException(ErrorLedger.SiteDudAction, e);
            }
        }
    }

    // A3(2026-11-14 아침): 효과 행동 묶음을 감싸 그 결과를 관측한다(RebukeOutcomeTracker 창 → 순수 RebukeOutcome.Decide). 효과 없음 = RebukeDudLog.NoEffect.
    // 관측 실패는 효과를 막지 않는다: 관측 시작이 실패해도 Actions 는 돌고(ERROR 1), 끝 판정이 실패해도 ERROR 1 — 사용 처리는 이 행동 앞에서 이미 끝났다(바뀌지 않음).
    // 해제 반격(E7)은 본편 시전이 다음 프레임에 실행되므로 판정이 미뤄진다(Pending → 능력 실행 끝 사건에서 RebukeOutcomeTracker 가 마무리).
    public class ContextActionRebukeEffect : ContextAction
    {
        public string EffectId;
        public ActionList Actions;

        public override string GetCaption() { return "BulwarkMage: rebuke effect " + EffectId + " (outcome observed)"; }

        public override void RunAction()
        {
            RebukeOutcomeTracker.Window w = null;
            try
            {
                BulwarkErrors.MaybeFault(ErrorLedger.SiteRebukeOutcome);
                if (string.IsNullOrEmpty(EffectId)) throw new InvalidOperationException("ContextActionRebukeEffect without an EffectId");
                var context = Context;
                UnitEntityData caster = context == null ? null : context.MaybeCaster;
                var tw = Target;
                UnitEntityData attacker = ReferenceEquals(tw, null) ? null : tw.Unit;
                if (caster == null || attacker == null) BulwarkErrors.Report(ErrorLedger.SiteRebukeOutcome, "rebuke " + EffectId + " outcome cannot be observed (caster " + (caster == null ? "missing" : "ok") + ", attacker " + (attacker == null ? "missing" : "ok") + ") - the effect still runs, no outcome log");
                else w = RebukeOutcomeTracker.Begin(EffectId, caster, attacker);
            }
            catch (Exception e)
            {
                BulwarkErrors.ReportException(ErrorLedger.SiteRebukeOutcome, e);
                w = null;
            }
            try
            {
                if (Actions == null) BulwarkErrors.Report(ErrorLedger.SiteRebukeOutcome, "rebuke " + EffectId + " has no effect actions");
                else Actions.Run();
            }
            finally
            {
                if (w != null) RebukeOutcomeTracker.End(w);
            }
        }
    }

    // 「시전자에게 그 자원이 Amount 이상 있는가」(본편에 자원 조건이 없다 — 설계 §R4.3). 판정 불가(시전자·자원 없음)는 거짓 = 발동 안 함 + Report(C1: 「없음」이 「발동」으로 새지 않는다).
    public class ContextConditionCasterHasResource : ContextCondition
    {
        public BlueprintAbilityResourceReference m_Resource;
        public int Amount;

        protected override string GetConditionCaption() { return "BulwarkMage: caster has rebuke resource >= " + Amount; }

        protected override bool CheckCondition()
        {
            try
            {
                BulwarkErrors.MaybeFault(ErrorLedger.SiteResourceCondition);
                var context = Context;
                UnitEntityData caster = context == null ? null : context.MaybeCaster;
                if (caster == null) { BulwarkErrors.Report(ErrorLedger.SiteResourceCondition, "no caster in the rebuke context - the rebuke does not fire (resource cannot be judged)"); return false; }
                var res = m_Resource == null ? null : m_Resource.Get();
                if (res == null) { BulwarkErrors.Report(ErrorLedger.SiteResourceCondition, "resource reference does not resolve - the rebuke does not fire"); return false; }
                if (Amount < 1) { BulwarkErrors.Report(ErrorLedger.SiteResourceCondition, "Amount " + Amount + " < 1 - the rebuke does not fire"); return false; }
                bool ok = caster.Resources.HasEnoughResource(res, Amount);
                if (ok) BulwarkErrors.Probe.RecordCheck(caster.UniqueId, caster.Resources.GetResourceAmount(res));
                return ok;
            }
            catch (Exception e)
            {
                BulwarkErrors.ReportException(ErrorLedger.SiteResourceCondition, e);
                return false;
            }
        }
    }

    // A4(2026-11-14 아침): 해제 반격의 대상 사전 차단. 본편 ContextActionCastSpell.RunAction 과 같은 계산 — @0067~0083 new AbilityData(주문, 시전자.Descriptor),
    // @015C~0162 AbilityData.CanTarget(Target) — 을 시전 앞에서 해, 거짓이면 시전하지 않는다(본편 「… is not valid target for …」 PFLog 오류 @0184~01A5 가 나지 않는다).
    // 거짓 분기 = 불발 기록(Untargetable): 사용(하루 −1·라운드 표지)은 이미 끝났다. 판정 불가(시전자·대상·주문 없음, 예외)도 거짓 + ERROR(시전하지 않는다 — C1).
    public class ContextConditionRebukeCanTarget : ContextCondition
    {
        public BlueprintAbilityReference m_Spell;

        protected override string GetConditionCaption() { return "BulwarkMage: rebuke spell can target the attacker"; }

        protected override bool CheckCondition()
        {
            try
            {
                BulwarkErrors.MaybeFault(ErrorLedger.SiteRebukeCanTarget);
                var context = Context;
                UnitEntityData caster = context == null ? null : context.MaybeCaster;
                if (caster == null) { BulwarkErrors.Report(ErrorLedger.SiteRebukeCanTarget, "no caster in the rebuke context - the spell is not cast"); return false; }
                var target = Target;
                if (ReferenceEquals(target, null) || target.Unit == null) { BulwarkErrors.Report(ErrorLedger.SiteRebukeCanTarget, "no attacker unit as the target - the spell is not cast"); return false; }
                var spell = m_Spell == null ? null : m_Spell.Get();
                if (spell == null) { BulwarkErrors.Report(ErrorLedger.SiteRebukeCanTarget, "spell reference does not resolve - the spell is not cast"); return false; }
                return new AbilityData(spell, caster.Descriptor).CanTarget(target);
            }
            catch (Exception e)
            {
                BulwarkErrors.ReportException(ErrorLedger.SiteRebukeCanTarget, e);
                return false;
            }
        }
    }
}
