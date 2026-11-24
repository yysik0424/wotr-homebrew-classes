using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using UnityEngine;

namespace OathboundRevenant
{
    // 자체 C# 부품 3(계획 Global Constraints 「자체 C# 부품 목록」 — 본편 대안 부재 근거는 부록 A-c·#4·#11·#12):
    //   SmiteTargetCondition · RoundOnceAction · SmiteLinkComponent. 본문은 전부 Guard.Run(사이트, 문맥 키, …) — 예외를 엔진에 넘기지 않고 계수한다.

    internal static class SmiteLookup
    {
        // 본편 SmiteEvilBuff(시전자 = caster)가 target 에 있는가. 블루프린트 결측은 호출자가 Missing 으로 본다.
        internal static BlueprintBuff SmiteBuff() { return ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Guids.Game.SmiteEvilBuff); }

        internal static Buff SmiteFrom(UnitEntityData target, BlueprintBuff smite, UnitEntityData caster)
        {
            return target.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint == smite && b.Context != null && b.Context.MaybeCaster == caster);
        }
    }

     
    // 결측(시전자·대상·블루프린트) = Report("smite-target", 입력) 후 거짓(계수됨). 주입·예외 = Guard 가 계수, 거짓.
    public class SmiteTargetCondition : ContextCondition
    {
        protected override string GetConditionCaption() { return "Target is the caster's smite target (OathboundRevenant)"; }

        protected override bool CheckCondition()
        {
            bool result = false;
            var ctx = Context;
            bool ok = Guard.Run(ErrorLedger.SiteSmiteTarget, ctx, () =>
            {
                var caster = ctx == null ? null : ctx.MaybeCaster;
                var target = Target == null ? null : Target.Unit;
                var smite = SmiteLookup.SmiteBuff();
                var d = SmiteTargetRule.Decide(caster != null, target != null, smite != null, caster != null && target != null && smite != null && SmiteLookup.SmiteFrom(target, smite, caster) != null);
                if (d.Verdict == SmiteTargetVerdict.Missing) { RevenantErrors.Report(ErrorLedger.SiteSmiteTarget, "missing " + d.MissingInput); return; }
                result = d.Verdict == SmiteTargetVerdict.Yes;
            }, RevenantErrors.Sink);
            return ok && result;
        }
    }

     
    // 표지 있음 → 정당한 거절(계수 없음) · 표지 부착 실패 → Report + 본문 미실행 · 부착 성공 → 내부 행동을 원소별 직접 실행(ActionList.Run 미사용).
    // 표지 해제 = 시전자 새 라운드(그 특성의 AddFactContextActions.NewRound — Bulwark Rebuke.cs:128-135 선례).
    public class RoundOnceAction : ContextAction
    {
        public BlueprintBuffReference m_Marker;
        public GameAction[] Actions;

        public override string GetCaption() { return "Once per caster round (OathboundRevenant)"; }

        public override void RunAction()
        {
            var ctx = Context;
            // 실패(주입·예외)는 Guard 가 계수하고 FailureLedger 에 남긴다 — 여기서 더 할 일은 없지만 반환을 버리지 않는다(wiring: guards consumed).
            if (!Guard.Run(ErrorLedger.SiteRoundOnce, ctx, () =>
            {
                var caster = ctx == null ? null : ctx.MaybeCaster;
                if (caster == null) throw new InvalidOperationException("no caster in context");
                var marker = m_Marker == null ? null : m_Marker.Get();
                if (marker == null) throw new InvalidOperationException("round marker blueprint missing");
                if (Actions == null) throw new InvalidOperationException("no inner actions");
                bool present = caster.Buffs.GetBuff(marker) != null;
                bool applied = false;
                if (!present) applied = caster.Descriptor.AddBuff(marker, ctx, null) != null;
                var v = RoundOnceRule.Decide(present, applied);
                if (v == RoundOnceVerdict.Skip) return; // 정당한 거절: 이 라운드에 이미 발동
                if (v == RoundOnceVerdict.MarkerFailed) { RevenantErrors.Report(ErrorLedger.SiteRoundOnce, "marker " + marker.name + " not applied (AddBuff returned null)"); return; }
                foreach (var a in Actions) a.RunAction();
            }, RevenantErrors.Sink)) return;
        }
    }

    // 낙인 버프(SmiteMark, 소유자 = 스마이트 대상)의 수명 부품(#4): 우리 시전자의 SmiteEvilBuff 가 떨어지는 순간 낙인을 뗀다(자식 흔들림·감속·치유 불가가 함께 떨어짐),
    // 소유자가 죽으면 그 순간 스마이트가 살아 있었고 시전자가 죽음의 낙인을 가졌을 때만 30 ft 안 다른 악한 적에게 스마이트를 무료로 다시 건다.
    public class SmiteLinkComponent : UnitBuffComponentDelegate, IUnitBuffHandler, IUnitLifeStateChanged
    {
        // 한 낙인당 이전 1회(사망 경로 두 개 — 버프 제거 · 생명 상태 — 중 먼저 온 것만).
        [NonSerialized] bool transferred;

        protected override void OnActivate()
        {
            // 부착 시 스마이트가 이미 없으면 제거(정당한 무동작 아님 — 연동이 깨진 상태로 남지 않게).
            // 검토 M7: OnActivate 안에서 Buff.Remove() 를 부르면 부착 처리 도중 재진입 → 종료 시각을 지금으로 당겨 다음 버프 틱에 떼게 한다(Buff.RemoveAfterDelay IL_0000~0035 = m_EndTime 설정 + UpdateNextEvent).
            var caster = Context == null ? null : Context.MaybeCaster;
            var smite = SmiteLookup.SmiteBuff();
            if (caster == null || smite == null) { RevenantErrors.Report(ErrorLedger.SiteSmiteTarget, "smite mark without " + (caster == null ? "caster" : "SmiteEvilBuff blueprint")); Buff.RemoveAfterDelay(TimeSpan.Zero); return; }
            if (SmiteLookup.SmiteFrom(Owner, smite, caster) == null) Buff.RemoveAfterDelay(TimeSpan.Zero);
        }

        public void HandleBuffDidAdded(Buff buff) { } // 정당한 무동작: 부착은 볼 필요 없다

        public void HandleBuffDidRemoved(Buff buff)
        {
            if (buff == null || buff.Owner != Owner || buff == Buff) return; // 정당한 무동작: 다른 유닛·자기 자신
            var caster = Context == null ? null : Context.MaybeCaster;
            var smite = SmiteLookup.SmiteBuff();
            var v = SmiteLinkRule.OnBuffRemoved(smite != null && buff.Blueprint == smite, caster != null && buff.Context != null && buff.Context.MaybeCaster == caster);
            if (v == SmiteLinkOnRemove.Ignore) return;
            // 스마이트가 소유자 사망 때문에 떨어졌다면(소유자 이미 죽음) 그 순간까지 활성이었다 → 사망 처리 먼저.
            if (Owner.State.IsDead) OnDeath(true);
            if (Buff.Active) Buff.Remove();
        }

        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        {
            if (unit != Owner || !Owner.State.IsDead) return; // 정당한 무동작: 다른 유닛·아직 살아 있음
            var caster = Context == null ? null : Context.MaybeCaster;
            var smite = SmiteLookup.SmiteBuff();
            OnDeath(caster != null && smite != null && SmiteLookup.SmiteFrom(Owner, smite, caster) != null);
        }

        void OnDeath(bool smiteActive)
        {
            if (transferred) return;
            transferred = true;
            var caster = Context == null ? null : Context.MaybeCaster;
            var mark = Strike.MarkOfDeathTalent;
            bool hasMark = caster != null && mark != null && caster.Descriptor.HasFact(mark);
            if (SmiteLinkRule.OnOwnerDeath(smiteActive, hasMark) != SmiteLinkOnDeath.Transfer) return;
            var ctx = Context;
            if (!Guard.Run(ErrorLedger.SiteSmiteTransfer, ctx, () =>
            {
                if (FailureLedger.Failed(ctx) != null) return; // 실패 전파: 같은 문맥의 앞 부품이 실패했다 — 추가 오류 없이 멈춘다
                var ability = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(Guids.Game.SmiteEvilAbility);
                if (ability == null) throw new InvalidOperationException("SmiteEvilAbility blueprint missing");
                var units = Game.Instance.State.Units.Where(u => u != null && u != Owner && !u.Destroyed && u.IsInGame).ToList();
                var cand = units.Select(u => new TransferCandidate(!u.State.IsDead, u.IsEnemy(caster), Kingmaker.Enums.AlignmentExtension.HasComponent(u.Alignment.ValueVisible, Kingmaker.Enums.AlignmentComponent.Evil), false, EdgeFeet(Owner, u))).ToList();
                int i = SmiteTransferRule.Pick(cand, RevenantRules.Dials.MarkRadiusFeet);
                if (i < 0) return; // 정당한 무동작: 30 ft 안 악한 적 없음
                var rule = Rulebook.Trigger(new RuleCastSpell(new AbilityData(ability, caster.Descriptor), new TargetWrapper(units[i])));
                if (!rule.Success) RevenantErrors.Report(ErrorLedger.SiteSmiteTransfer, "free smite cast on " + units[i].CharacterName + " did not start (RuleCastSpell.Success false)");
            }, RevenantErrors.Sink)) return; // 실패는 계수됨 — 이전 없음
        }

        // 가장자리 거리(ft) = (중심 2D 거리 − 두 체구) / 0.3048 — RevenantTestUnit.PlaceAtEdge 와 같은 식(계획 #8).
        static float EdgeFeet(UnitEntityData a, UnitEntityData b)
        {
            float center = Vector2.Distance(new Vector2(a.Position.x, a.Position.z), new Vector2(b.Position.x, b.Position.z));
            return Math.Max(0f, (center - a.Corpulence - b.Corpulence) / RevenantRules.Dials.MetersPerFoot);
        }
    }
}
