using System;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Utility;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ShadowSorcerer
{
     
    // OnEventDidTrigger: 사실 → 순수 EchoGate.Decide → Echo 면 기본 주문의 독립 데이터 생성(Capture) → 범위 고정(FreezeAtEnqueue) → RoundLedger.Commit → EchoQueue.Enqueue.
    public class ShadowEchoComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCastSpell>
    {
        public void OnEventAboutToTrigger(RuleCastSpell evt) { } // 정당한 무동작: 결과(Success·Params)가 정해진 뒤만 본다

        // 2026-11-17 메아리 연출: 특성이 켜져 있는 동안 본체의 그림자 유닛 묶음을 미리 만들어 숨겨 둔다(ShadowDoubles — 던지지 않고 규칙에 쓰지 않는다).
        protected override void OnTurnOn() { ShadowDoubles.Watch(Owner); }
        protected override void OnTurnOff() { ShadowDoubles.Unwatch(Owner); }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            try
            {
                ShadowErrors.MaybeFault(ErrorLedger.SiteEchoHandler);
                var caster = Owner;
                var facts = Facts(evt, caster);
                var g = EchoGate.Decide(facts);
                if (g.Decision == Decision.Skip) { ShadowErrors.CountSkip(g.Reason ?? "Skip"); return; }
                if (g.Decision == Decision.Unknown) { ShadowErrors.Report(ErrorLedger.SiteEchoHandler, "echo gate unknown for " + evt.Spell.Blueprint.name + ": " + g.Reason); return; }
                Enqueue(evt, caster, facts, g.Count);
            }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteEchoHandler, e); }
        }

        internal static CastFacts Facts(RuleCastSpell evt, UnitEntityData caster)
        {
            var f = new CastFacts();
            if (evt == null || evt.Spell == null || caster == null) return f; // 전부 null → Unknown
            var data = evt.Spell;
            f.IsDuplicate = evt.IsDuplicateSpellApplied || EchoRegistry.IsEcho(evt);
            f.Success = evt.Success;
            f.Origin = EchoRegistry.OriginOf(evt);
            var sticky = data.StickyTouch;                       // 전달 단계면 원래(충전) 주문 데이터
            var source = sticky ?? data;                         // 적대·아이템·형식 사실은 원래 주문에서
            f.HasSourceItem = data.SourceItem != null || data.SourceItemForTouch != null || source.SourceItem != null;
            f.IsSpellType = source.Blueprint.Type == AbilityType.Spell;
            var sb = data.Spellbook;
            f.SpellbookKnown = true;
            f.SpellbookGuid = sb == null || sb.Blueprint == null ? null : sb.Blueprint.AssetGuid.ToString();
            f.IsStickyCastStage = data.Blueprint.StickyTouch != null;
            f.IsStickyDelivery = sticky != null;
            var bp = source.Blueprint;
            f.Hostile = new HostileFacts { SpellGuid = bp.AssetGuid.ToString(), CanTargetEnemies = bp.CanTargetEnemies, CanTargetFriends = bp.CanTargetFriends, EffectOnEnemyHarmful = bp.EffectOnEnemy == AbilityEffectOnUnit.Harmful, IsCantrip = bp.IsCantrip, IsSummoning = (bp.SpellDescriptor & SpellDescriptor.Summoning) != 0 };
            f.Target = TargetFactsOf(data, evt.SpellTarget, caster, true);
            f.SorcererLevel = caster.Progression.GetClassLevel(Archetype.SorcererClass);
            f.Round = RoundLedger.Facts(caster);
            return f;
        }

        // 자기 앵커(Owner — 개인 범위 주문은 시전자 자신이 대상) 는 지점처럼 본다: 적대 대상 검사를 하지 않는다(시전자는 자기의 적이 아니다).
        internal static TargetFacts TargetFactsOf(AbilityData data, TargetWrapper t, UnitEntityData caster, bool atCast)
        {
            var f = new TargetFacts();
            if (t == null) return f;
            bool owner = data.TargetAnchor == AbilityTargetAnchor.Owner;
            f.IsPoint = !t.IsUnit || owner;
            if (!f.IsPoint)
            {
                var u = t.Unit;
                // 검토 I2: 파괴·지역 밖 유닛은 없는 것과 같다(UnitUseAbility.OnAction IL_0035~005B 도 IsInGame 거짓이면 시전하지 않는다) → TargetInvalid 소멸(기록됨).
                f.UnitPresent = u != null && !u.Destroyed && u.IsInGame;
                if (!f.UnitPresent.Value) return f;
                f.IsDead = u.Descriptor.State.IsDead;
                if (atCast) f.IsEnemyOfCaster = caster.IsEnemy(u);
            }
            f.CanTarget = data.CanTarget(t);
            return f;
        }

        // UnitUseAbility pays the original after the rule returns. Touch delivery already paid at charge time.
        // Keep the slot tier separately from the base spell the echo casts without metamagic.
        internal static AbilityData ExtraSlot(AbilityData spell, AbilityData commandOriginal, bool spendsOriginal)
        {
            var charge = spell.StickyTouch;
            var original = charge ?? commandOriginal ?? spell;
            int reserved = charge == null && spendsOriginal ? original.SpellSlotCost : 0;
            var source = original;
            while (source.ConvertedFrom != null) source = source.ConvertedFrom;
            var book = source.Spellbook;
            if (book == null || !book.Blueprint.Spontaneous || book.Blueprint.AssetGuid.ToString() != Guids.Game.SorcererSpellbook) return null;
            int level = book.GetSpellLevel(source);
            if (level <= 0 || book.GetSpontaneousSlots(level) <= reserved) return null;
            return new AbilityData(source.Blueprint, book, level);
        }

        static void Enqueue(RuleCastSpell evt, UnitEntityData caster, CastFacts facts, int count)
        {
            var data = evt.Spell;
            if (evt.Result == null) { ShadowErrors.Report(ErrorLedger.SiteEchoHandler, "successful cast " + data.Blueprint.name + " has no execution process - no echo"); return; }
            AbilityData parameterSource;
            var clean = ParamsCopy.Capture(data, out parameterSource);
            var ff = new FreezeFacts { IsAoe = data.IsAOE, Anchor = (int)data.TargetAnchor, UsesTargetsAround = data.AbilityTargetsAround != null, IsUnitWrapper = evt.SpellTarget.IsUnit };
            var kind = TargetRule.FreezeAtEnqueue(ff);
            TargetWrapper wrapper;
            Vector3? center = null;
            switch (kind)
            {
                case FreezeKind.KeepWrapper: wrapper = evt.SpellTarget; break;
                case FreezeKind.PointWrapper: wrapper = new TargetWrapper(evt.SpellTarget.Point, evt.SpellTarget.IsOrientationSpecified ? evt.SpellTarget.Orientation : (float?)null, null); break;
                case FreezeKind.KeepWrapperFrozenCenter:
                    wrapper = evt.SpellTarget;
                    center = evt.SpellTarget.IsUnit ? evt.SpellTarget.Unit.EyePosition : evt.SpellTarget.Point; // AbilityTargetsAround.Select IL_0014~002A 와 같은 중심
                    break;
                default:
                    ShadowErrors.Report(ErrorLedger.SiteEchoHandler, "unsupported area shape for " + data.Blueprint.name + " (anchor " + data.TargetAnchor + ", TargetsAround " + ff.UsesTargetsAround + ") - no echo (J17)");
                    return;
            }
            AbilityData commandOriginal;
            evt.TryGetCustomData(EchoRegistry.OriginalAbilityKey, out commandOriginal);
            var slot = ExtraSlot(data, commandOriginal, evt.ShouldSpendResource);
            if (slot == null) { ShadowErrors.CountSkip(DudReasons.NoEchoSlot); return; }
            RoundLedger.Commit(caster, facts.Round);
            EchoQueue.Enqueue(new EchoJob(caster, clean, wrapper, parameterSource, slot, kind == FreezeKind.KeepWrapperFrozenCenter, center, evt.Result, count));
            ShadowDoubles.Watch(caster); // 연출 전용(던지지 않는다): 불러온 직후처럼 특성 켜짐 이벤트를 놓친 본체도 첫 메아리에서 묶음을 갖게 한다
        }

        // 메아리 특성에 붙이기(BuildAll: Archetype.Build 뒤, Register 앞). 본편 컴포넌트 복제 없음.
        public static void Attach()
        {
            if (Archetype.EchoFeature == null) throw new InvalidOperationException("ShadowSorcerer: ShadowEchoComponent.Attach before Archetype.Build");
            AddComponents(SlotsFeature.Buff, new ShadowEchoComponent());
        }
    }
}
