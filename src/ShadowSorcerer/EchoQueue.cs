using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ShadowSorcerer
{
    // 메아리 작업 하나(비저장). 선택한 주문과 시전 시 대상을 보관한다. 재시전 데이터와 접촉 충전의 기준 주문은 모두 독립 객체다.
    public sealed class EchoJob
    {
        public readonly UnitEntityData Caster;
        public readonly AbilityData Data;
        public readonly TargetWrapper Wrapper;
        public readonly AbilityData ParameterSource;
        public readonly bool NeedsFrozenCenter;
        public readonly Vector3? FrozenCenter;
        public readonly AbilityExecutionProcess Original;
        public readonly EchoJobState State;

        public EchoJob(UnitEntityData caster, AbilityData data, TargetWrapper wrapper, AbilityData parameterSource, bool needsCenter, Vector3? center, AbilityExecutionProcess original, int count)
        {
            if (caster == null || data == null || wrapper == null || parameterSource == null || original == null) throw new ArgumentNullException("ShadowSorcerer: echo job with a missing part");
            if (needsCenter && !center.HasValue) throw new ArgumentException("ShadowSorcerer: echo job needs a frozen centre but has none");
            Caster = caster; Data = data; Wrapper = wrapper; ParameterSource = parameterSource; NeedsFrozenCenter = needsCenter; FrozenCenter = center; Original = original;
            State = new EchoJobState(Now(), count);
        }

         
        public static double Now() { return Game.Instance.TimeController.GameTime.TotalSeconds; }
    }

     
    public static class EchoQueue
    {
        static readonly List<EchoJob> jobs = new List<EchoJob>();
        static AreaHook areaHook;




        public static int Fired { get; private set; }








        public static void Enqueue(EchoJob j)
        {
            if (j == null) throw new ArgumentNullException("j");
            jobs.Add(j);
        }

        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            if (!Game.HasInstance || Game.Instance.Player == null || Game.Instance.TimeController == null) return; // 정당한 무동작: 게임 준비 전
            EnsureHooks();
            double now = EchoJob.Now();
            // 연출 무대(그림자 유지·흩어짐)는 대기 작업이 없어도 돈다 — 던지지 않고 대기열 상태에 쓰지 않는다.
            EchoStage.Tick(now);
            if (jobs.Count == 0) return;
            foreach (var j in jobs.ToArray())
            {
                try
                {
                    ShadowErrors.MaybeFault(ErrorLedger.SiteEchoQueue);
                    Step(j, now);
                }
                catch (Exception e)
                {
                    j.State.Aborted = true;
                    ShadowErrors.ReportException(ErrorLedger.SiteEchoQueue, e);
                }
                if (j.State.Finished)
                {
                    jobs.Remove(j);
                    EchoStage.JobFinished(j, now, j.State.Aborted); // 연출 전용: 남은 그림자를 흩는다(중단이면 즉시)
                }
            }
        }

         
        static void Step(EchoJob j, double now) { EchoQueueCore.Step(j.State, now, new StepHost(j)); }

        sealed class StepHost : IEchoStepHost
        {
            readonly EchoJob j;
            public StepHost(EchoJob job) { j = job; }
            public bool? OriginalEnded() { return j.Original.IsEnded; }
            public FireOutcome Fire() { return EchoQueue.Fire(j); }
            public void Report(QueueStep s)
            {
                if (s.Action == QueueAction.Drop) EchoDudLog.Log(j.Caster, s.Reason, s.Count);
                else if (s.Action == QueueAction.Abort) ShadowErrors.Report(ErrorLedger.SiteEchoQueue, "echo job aborted (" + s.Reason + ") for " + j.Data.Blueprint.name + ", " + s.Count + " echo(es) not fired");
                else throw new InvalidOperationException("ShadowSorcerer: queue report of " + s.Action);
            }
            // 연출 전용(던지지 않는다 — 던져도 코어가 잡는다): 등장·시전 동작은 대기열의 발사 시계에 맞춘다(EchoStageRule.Cue).
            public void StageBeforeNext(double now, bool? originalEnded) { EchoStage.BeforeNext(j, now, originalEnded); }
            public void StageAfterFire(int index, FireOutcome outcome) { EchoStage.AfterFire(j, index, outcome); } // 실패한 메아리는 흩어짐이 기다리지 않는다
            public void StageFailed(Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "stage-hook-" + e.GetType().Name, "echo visual stage hook threw (" + e.GetType().Name + ": " + e.Message + ") - the echo queue went on unchanged"); }
        }

        static FireOutcome Fire(EchoJob j)
        {
            var caster = j.Caster;
            if (caster.Destroyed || !caster.IsInGame || caster.Descriptor.State.IsDead) return FireOutcome.CasterDown;
            var t = TargetRule.AtFire(ShadowEchoComponent.TargetFactsOf(j.Data, j.Wrapper, caster, false));
            if (t.Decision == Decision.Unknown) { ShadowErrors.Report(ErrorLedger.SiteEchoQueue, "echo target unknown: " + t.Reason); return FireOutcome.TargetUnknown; }
            if (t.Decision == Decision.Skip) return t.Reason == TargetReasons.TargetDead ? FireOutcome.TargetDead : FireOutcome.TargetInvalid;
            RuleCastSpell rule;
            using (EchoConstructionScope.Enter())
            {
                rule = new RuleCastSpell(j.Data, j.Wrapper) { IsDuplicateSpellApplied = true };
            }
            var tag = new EchoTag(j.NeedsFrozenCenter, j.FrozenCenter, j.State.Fired + j.State.Dropped + 1);
            EchoRegistry.Mark(rule, tag);
            EchoRegistry.Mark(rule.Context, tag);
            ParamsCopy.Apply(rule.Context, j.ParameterSource);
            // 연출(보이는 출발점)만 묶는다 — 던지지 않고 규칙에 쓰지 않는다. 묶이지 않으면 이 메아리는 본체에서 나간다(EchoVisuals.Bind).
            EchoVisuals.Bind(rule.Context, j, tag.Index);
            Rulebook.Trigger(rule);
            Fired++;



            if (!rule.Success) return FireOutcome.EchoCastFailed;
            if (rule.Result == null) { ShadowErrors.Report(ErrorLedger.SiteEchoQueue, "successful echo of " + j.Data.Blueprint.name + " has no execution process"); return FireOutcome.ResultMissing; }
            ApplyShadowFactor(rule);
            return FireOutcome.Ok;
        }

         
        // 효과는 AbilityExecutionController 의 다음 Tick 에서 실행된다(Execute 는 과정을 만들기만 한다 — Execute IL_000E~0022, Tick IL_00E5~00EC) → 효과보다 앞선다.
        // 계수 뒤에 메아리 표식 항목(EchoMarker.Mark — 2026-11-16 메아리 세기)을 같은 자리에서 넣는다. 같은 근거로 효과보다 앞선다.
        // 실패 = 그 메아리의 실행을 취소(RuleCastSpell.CancelAbilityExecution — 효과 0, 그림자 없는 전액 효과가 새지 않게)한 뒤 다시 던져 대기열이 보고 + 작업 중단.
        static void ApplyShadowFactor(RuleCastSpell rule)
        {
            try
            {
                ShadowErrors.MaybeFault(ErrorLedger.SiteShadowFactor);
                int engine = rule.Context.ShadowFactorPercents;
                int f = ShadowFactorRule.Decide(engine);
                rule.Context.ShadowFactorPercents = f;
                if (rule.Context.ShadowFactorPercents != f || !rule.Context.IsShadow) throw new InvalidOperationException("ShadowSorcerer: shadow factor readback " + rule.Context.ShadowFactorPercents + ", expected " + f);
                // 메아리 세기(소유자 결정 2026-11-16): 표식 항목을 넣어야 H7(피해 50%)·H8(불신 성공 시 확률 없음)이 이 컨텍스트와 그 자식을 메아리로 읽는다.
                // 실패하면 아래 catch 가 실행을 취소한다 — 표식 없는 메아리가 옛 세기(전액 / 절반)로 새지 않는다.
                EchoMarker.Mark(rule.Context);
            }
            catch
            {
                rule.CancelAbilityExecution();
                throw;
            }
        }

        static void EnsureHooks()
        {
            RoundLedger.EnsureSubscribed();
            if (areaHook != null) return;
            areaHook = new AreaHook();
            EventBus.Subscribe(areaHook);
            if (!EventBus.IsGloballySubscribed(areaHook)) ShadowErrors.Report(ErrorLedger.SiteEchoQueue, "area-load subscription did not register - pending echoes will not be dropped on reload");
        }

         
        public static void OnAreaLoad()
        {
            ReloadSweep.Run(jobs.ToArray(), ReloadJob, ReportReload, FinishReload);
        }

        static void ReloadJob(EchoJob j)
        {
            if (j.State.Finished) return;
            var s = EchoQueueCore.Reloaded(j.State);
            if (s.Count > 0) EchoDudLog.Log(j.Caster, s.Reason, s.Count);
        }

        static void ReportReload(Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteEchoQueue, e); }

        static void FinishReload()
        {
            jobs.Clear();
            RoundLedger.Clear();
            EchoStage.DropAll(); // 연출 전용: 그림자 유닛은 지역과 함께 사라진다 — 무대 기록만 버린다
        }

        sealed class AreaHook : IAreaHandler
        {
            public void OnAreaBeginUnloading() { } // 정당한 무동작: 로드 완료만 본다
            public void OnAreaDidLoad()
            {
                try { OnAreaLoad(); }
                catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteEchoQueue, e); }
            }
        }
    }
}
