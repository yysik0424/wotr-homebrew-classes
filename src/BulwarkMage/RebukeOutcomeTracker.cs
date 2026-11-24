using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Parts;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
    // A3(2026-11-14 아침) 결과 관측기 — ContextActionRebukeEffect 가 효과 행동을 돌리는 동안만 여는 「창」으로 엔진 규칙을 센다. 전역 구독자 1개(첫 OnUpdate 에 EventBus.Subscribe, Main.Load),
    // 창이 없으면 아무것도 하지 않는다. 저장 상태 없음: 창·대기 목록은 메모리에만 있고, 세이브 사이에 남는 것은 없다(로드 직후 대기 중이던 E7 판정은 사라진다 — README).
    //   동기 효과(E1~E6): 창 = 효과 행동이 도는 동안(같은 호출 안). 피해 = 시전자 → 공격자 RuleDealDamage 의 Result 합, 버프 = 공격자 버프 인스턴스·끝 시각 전후 비교,
    //     밀기 = 공격자 UnitPartForceMove.Chunks 개수 전후 비교.
    //   E7: 본편 ContextActionCastSpell → RuleCastSpell(동기) → AbilityData.Cast → AbilityExecutionController.Execute 가 프로세스를 목록에 넣을 뿐(@000F~001C) → 다음 틱에 실행.
    //     그래서 창에서는 RuleCastSpell(시작 여부 = Result 프로세스)만 보고, 판정은 대기 목록으로 미룬다: 그 실행 동안 시전자 → 공격자 RuleDispelMagic 의 수·Success 를 세고,
    //     IAbilityExecutionProcessHandler.HandleExecutionProcessEnd(그 문맥)(AbilityExecutionProcess.ProcessRoutine @03B8~03C4)에서 RebukeOutcome.Decide 로 마무리한다.
    //     끝 사건 없이 끝난 프로세스(IsEnded 인데 대기 중)는 다음 창을 열 때 ERROR 로 정리한다(조용히 쌓이지 않는다).
    public sealed class RebukeOutcomeTracker : IGlobalRulebookHandler<RuleDealDamage>, IGlobalRulebookHandler<RuleCastSpell>, IGlobalRulebookHandler<RuleDispelMagic>, IAbilityExecutionProcessHandler
    {
        public sealed class Window
        {
            public readonly string EffectId;
            public readonly RebukeSpec Spec;
            public readonly UnitEntityData Caster, Attacker;
            public readonly RebukeObservation Obs = new RebukeObservation();
            internal BlueprintBuff Buff;
            internal Dictionary<Buff, TimeSpan> BuffsBefore;
            internal int ChunksBefore = -1;
            internal AbilityExecutionContext CastContext;
            internal AbilityExecutionProcess Process;
            internal Window(string effectId, RebukeSpec spec, UnitEntityData caster, UnitEntityData attacker) { EffectId = effectId; Spec = spec; Caster = caster; Attacker = attacker; }
        }

        static readonly RebukeOutcomeTracker instance = new RebukeOutcomeTracker();
        static readonly List<Window> open = new List<Window>();
        static readonly List<Window> pending = new List<Window>();
        static bool subscribeTried;

        public static bool Subscribed { get; private set; }




        // Main.Load 가 OnUpdate 에 붙인다: 첫 프레임에 한 번 구독(실패 = ERROR 1번, 다시 시도하지 않는다 — 매 프레임 반복 방지). 구독이 없으면 효과 없음 기록이 하나도 안 나온다 → S9 단계가 FAIL.
        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            if (subscribeTried) return;
            subscribeTried = true;
            try
            {
                EventBus.Subscribe(instance);
                Subscribed = EventBus.IsGloballySubscribed(instance);
                if (!Subscribed) BulwarkErrors.Report(ErrorLedger.SiteRebukeOutcome, "outcome tracker subscription did not register (EventBus.IsGloballySubscribed false) - no-effect rebukes will not be logged");
            }
            catch (Exception e) { BulwarkErrors.ReportException(ErrorLedger.SiteRebukeOutcome, e); }
        }

        // 창 열기(관측 행동 try 안에서 — 예외는 그쪽 catch 가 센다). 창은 끝에 넣는다(앞에서 던지면 창이 남지 않는다).
        public static Window Begin(string effectId, UnitEntityData caster, UnitEntityData attacker)
        {
            if (caster == null || attacker == null) throw new ArgumentNullException(caster == null ? "caster" : "attacker");
            if (!Subscribed) throw new InvalidOperationException("outcome tracker is not subscribed (the first OnUpdate has not run or subscribing failed)");
            SweepEnded();
            var w = new Window(effectId, RebukeTable.Get(effectId), caster, attacker);
            if (w.Spec.BuffGuid != null)
            {
                w.Buff = Get<BlueprintBuff>(w.Spec.BuffGuid);
                w.BuffsBefore = attacker.Buffs.Enumerable.Where(b => ReferenceEquals(b.Blueprint, w.Buff)).ToDictionary(b => b, b => b.EndTime);
            }
            if (w.Spec.PushFeet != null) w.ChunksBefore = Chunks(attacker);
            open.Add(w);
            return w;
        }

        static int Chunks(UnitEntityData u)
        {
            var part = u.Get<UnitPartForceMove>();
            return part == null ? 0 : part.Chunks.Count();
        }

        // 창 닫기(관측 행동의 finally). 판정 = RebukeOutcome.Decide. 예외는 여기서 센다(효과는 이미 돌았다).
        public static void End(Window w)
        {
            try
            {
                int at = open.LastIndexOf(w);
                if (at < 0) { BulwarkErrors.Report(ErrorLedger.SiteRebukeOutcome, "rebuke " + w.EffectId + " outcome window was not open"); return; }
                if (at != open.Count - 1) BulwarkErrors.Report(ErrorLedger.SiteRebukeOutcome, "rebuke " + w.EffectId + " outcome window closed out of order (" + (open.Count - 1 - at) + " newer window(s) still open)");
                open.RemoveAt(at);
                if (w.Buff != null)
                {
                    var after = w.Attacker.Buffs.Enumerable.Where(b => ReferenceEquals(b.Blueprint, w.Buff)).ToList();
                    TimeSpan before;
                    w.Obs.BuffApplied = after.Any(b => !w.BuffsBefore.TryGetValue(b, out before) || b.EndTime > before);
                }
                if (w.ChunksBefore >= 0) w.Obs.PushStarted = Chunks(w.Attacker) > w.ChunksBefore;
                var v = RebukeOutcome.Decide(w.EffectId, w.Obs);
                if (v.Kind == RebukeOutcomeKind.Pending) { pending.Add(w); return; }
                Conclude(w, v);
            }
            catch (Exception e) { BulwarkErrors.ReportException(ErrorLedger.SiteRebukeOutcome, e); }
        }

        static void Conclude(Window w, RebukeVerdict v)
        {
            if (v.Kind == RebukeOutcomeKind.NoEffect) RebukeDudLog.NoEffect(ErrorLedger.SiteRebukeOutcome, w.EffectId, v.Reason.Value, w.Caster, w.Attacker, Describe(w.Obs));
            else if (v.Kind == RebukeOutcomeKind.Unknown) BulwarkErrors.Report(ErrorLedger.SiteRebukeOutcome, "rebuke " + w.EffectId + " outcome cannot be judged: " + v.Why + " (" + Describe(w.Obs) + ")");
            else if (v.Kind != RebukeOutcomeKind.Effect) BulwarkErrors.Report(ErrorLedger.SiteRebukeOutcome, "rebuke " + w.EffectId + " unexpected outcome " + v);
        }

        static string Describe(RebukeObservation o)
        {
            return "damage rules " + o.DamageRules + " dealt " + o.DamageDealt + ", buff applied " + o.BuffApplied + ", push " + (o.PushStarted.HasValue ? o.PushStarted.Value.ToString() : "n/a") + ", cast " + o.CastRuleSeen + "/" + o.CastStarted + ", dispels " + o.DispelSuccesses + "/" + o.DispelRules + (o.DispelFinished ? " finished" : "");
        }

        // 끝 사건 없이 끝난 대기(프로세스 IsEnded 또는 프로세스 없음) = ERROR 로 정리.
        static void SweepEnded()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                if (p.Process != null && !p.Process.IsEnded) continue;
                pending.RemoveAt(i);
                BulwarkErrors.Report(ErrorLedger.SiteRebukeOutcome, "rebuke " + p.EffectId + " cast ended without the ability-execution end event - outcome not judged (" + Describe(p.Obs) + ")");
            }
        }

        Window Top { get { return open.Count == 0 ? null : open[open.Count - 1]; } }

        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            var w = Top;
            if (w == null || evt == null || !ReferenceEquals(evt.Initiator, w.Caster) || !ReferenceEquals(evt.Target, w.Attacker)) return;
            w.Obs.DamageRules++;
            w.Obs.DamageDealt += evt.Result; // 음수면 Decide 가 던진다(조용히 0 으로 고치지 않는다)
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt) { }
        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            var w = Top;
            if (w == null || evt == null || w.Spec.CastSpellGuid == null || !ReferenceEquals(evt.Initiator, w.Caster)) return;
            if (evt.Spell == null || evt.Spell.Blueprint == null || evt.Spell.Blueprint.AssetGuid.ToString() != w.Spec.CastSpellGuid) return;
            if (ReferenceEquals(evt.SpellTarget, null) || !ReferenceEquals(evt.SpellTarget.Unit, w.Attacker)) return;
            w.Obs.CastRuleSeen = true;
            w.Process = evt.Result;
            w.Obs.CastStarted = evt.Result != null;
            w.CastContext = evt.Context;
        }

        public void OnEventAboutToTrigger(RuleDispelMagic evt) { }
        public void OnEventDidTrigger(RuleDispelMagic evt)
        {
            if (evt == null) return;
            foreach (var w in pending)
            {
                if (!ReferenceEquals(evt.Initiator, w.Caster) || !ReferenceEquals(evt.Target, w.Attacker)) continue;
                w.Obs.DispelRules++;
                if (evt.Success) w.Obs.DispelSuccesses++;
                return;
            }
        }

        public void HandleExecutionProcessStart(AbilityExecutionContext context) { } // 정당한 무동작: 끝만 본다
        public void HandleExecutionProcessEnd(AbilityExecutionContext context)
        {
            int at = context == null ? -1 : pending.FindIndex(p => ReferenceEquals(p.CastContext, context));
            if (at < 0) return;
            var w = pending[at];
            pending.RemoveAt(at);
            try
            {
                BulwarkErrors.MaybeFault(ErrorLedger.SiteRebukeDispelEnd);
                w.Obs.DispelFinished = true;
                Conclude(w, RebukeOutcome.Decide(w.EffectId, w.Obs));
            }
            catch (Exception e) { BulwarkErrors.ReportException(ErrorLedger.SiteRebukeDispelEnd, e); }
        }
    }
}
