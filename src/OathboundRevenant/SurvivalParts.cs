using System;
using System.Linq;
using WotrClassMod.Common;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace OathboundRevenant
{
    // 자체 C# 부품 4(계획 「자체 C# 부품 목록」 + 검토 C1): UnyieldingOathComponent · RiposteAction · OathShieldComponent · SoulHarvestComponent. 본문은 Guard.Run.

    // 꺼지지 않는 맹세: 소유자가 받은 피해 뒤 HP 0 이하 ∧ 하루 자원 ≥ 1 → 누적 피해 = 최대 HP − 1 + 자원 1 + (마지막 숨) 폭발 무료 시전.
    // 본편 UnitPartDeathPrevention.OnEventDidTrigger(RuleDealDamage) @0031~0070 과 같은 식(그 파트 + 1/일 자원 연결 본편 부품 없음 — 부록 A-i).
    public class UnyieldingOathComponent : UnitFactComponentDelegate, ITargetRulebookHandler<RuleDealDamage>
    {
        public void OnEventAboutToTrigger(RuleDealDamage evt) { } // 정당한 무동작: 판정은 피해 반영 뒤

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt == null || evt.IsFake) return; // 정당한 무동작: 미리보기
            if (!Guard.Run(ErrorLedger.SiteUnyielding, Fact, () =>
            {
                if (Owner.State.IsDead) return; // 정당한 무동작: 이미 죽었다(살릴 대상이 아님)
                var res = Survival.UnyieldingResource;
                if (res == null) throw new InvalidOperationException("unyielding resource blueprint missing");
                int left = Owner.Descriptor.Resources.GetResourceAmount(res);
                var lb = Talents.Get("LastBreath");
                var d = UnyieldingRule.Decide(Owner.Damage, Owner.MaxHP, left, Owner.Descriptor.HasFact(lb));
                if (!d.Save) return;
                Owner.Damage = d.SetDamage;
                Owner.Descriptor.Resources.Spend(res, RevenantRules.Dials.UsesPerActivation);
                if (d.Burst)
                {
                    if (Survival.LastBreathAbility == null) throw new InvalidOperationException("last breath ability missing");
                    var rule = Rulebook.Trigger(new RuleCastSpell(new AbilityData(Survival.LastBreathAbility, Owner.Descriptor), new TargetWrapper(Owner)));
                    if (!rule.Success) RevenantErrors.Report(ErrorLedger.SiteUnyielding, "last breath burst did not start");
                }
            }, RevenantErrors.Sink)) return; // 실패는 계수됨 — 생존 처리 없음
        }
    }

    // 원한의 반격: 자원 확인 → 반격 규칙 직접 발동 → 발동·해결됐을 때만 소비(본편 자원 조건 없음, ContextActionMeleeAttack 실패는 ActionList.Run 에 삼켜짐 — #11).
    // 같은 공격 사건(같은 소유자·같은 공격자·같은 게임 시각)에서 두 번째 호출은 거절(이중 발동 방지, 검토 M1 — 키에 소유자 포함).
    // 주 무기가 원거리면 정당한 거절(근접 반격만), 맨손은 엔진의 빈 손 무기로 반격한다(결측 아님).
    public class RiposteAction : ContextAction
    {
        static readonly RiposteOnceLedger<UnitEntityData> once = new RiposteOnceLedger<UnitEntityData>();

        public override string GetCaption() { return "Vengeful riposte (OathboundRevenant)"; }

        public override void RunAction()
        {
            var ctx = Context;
            if (!Guard.Run(ErrorLedger.SiteRiposte, ctx, () =>
            {
                var owner = ctx == null ? null : ctx.MaybeCaster;
                var attacker = Target == null ? null : Target.Unit;
                var weapon = owner == null ? null : (owner.Body.PrimaryHand.MaybeWeapon ?? owner.Body.EmptyHandWeapon);
                var res = Survival.RiposteResource;
                if (attacker == null) throw new InvalidOperationException("no attacker in context");
                var now = Game.Instance.TimeController.GameTime;
                bool already = owner != null && once.Already(owner, attacker, now);
                int left = owner == null || res == null ? 0 : owner.Descriptor.Resources.GetResourceAmount(res);
                bool melee = weapon != null && weapon.Blueprint.IsMelee;
                bool inReach = owner != null && weapon != null && owner.DistanceTo(attacker) <= weapon.AttackRange.Meters + owner.Corpulence + attacker.Corpulence;
                var d = RiposteRule.Decide(owner != null, weapon != null, res != null, left, !attacker.State.IsDead, inReach, already, melee);
                if (d.Verdict == RiposteVerdict.Missing) { RevenantErrors.Report(ErrorLedger.SiteRiposte, "missing " + d.MissingInput); return; }
                if (d.Verdict == RiposteVerdict.Refuse) return; // 정당한 거절
                var rule = Rulebook.Trigger(new RuleAttackWithWeapon(owner, attacker, weapon, 0) { SkipAnimation = true });
                if (rule.AttackRoll == null) { RevenantErrors.Report(ErrorLedger.SiteRiposte, "riposte attack did not resolve"); return; }
                once.Record(owner, attacker, now);
                owner.Descriptor.Resources.Spend(res, RevenantRules.Dials.UsesPerActivation);
            }, RevenantErrors.Sink)) return; // 실패는 계수됨 — 자원 불변
        }
    }

    // 맹세의 방패(아군 오라 효과 버프): 아군이 「실제로 받은」(임시 HP 흡수 뒤) 근접 무기 피해 중 min(피해, 5)를 되돌리고 같은 양을 오라 주인에게 무형식 피해 1건으로(본편 피해 분담 = 동물 동료·변신뿐 — 부록 A-j).
    // 받은 양 = 피해 반영 뒤 누적 피해 − AboutToTrigger 때 기록한 누적 피해(검토 I1 — evt.Result 는 임시 HP 전 값). 오라 주인 자신에게 붙은 효과는 무동작(자기에게 다시 넘기면 무한 반복).
    public class OathShieldComponent : UnitBuffComponentDelegate, ITargetRulebookHandler<RuleDealDamage>
    {
        // 피해 반영 전 누적 피해(사건당 하나) — 참조 형식으로 담는다(dial: 숫자 정적 필드 금지 규칙에 걸리지 않게, 값은 사건 수명 동안만).
        sealed class DamageSnapshot { public readonly int Damage; public DamageSnapshot(int d) { Damage = d; } }
        static readonly RulebookEvent.CustomDataKey before = new RulebookEvent.CustomDataKey("OathboundRevenant.BeforeDamage");

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt == null || evt.IsFake) return; // 정당한 무동작: 미리보기
            DamageSnapshot existing;
            if (!evt.TryGetCustomData(before, out existing)) evt.SetCustomData(before, new DamageSnapshot(Owner.Damage)); // 같은 피해에 방패가 둘이어도 반영 전 값은 하나
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt == null || evt.IsFake) return; // 정당한 무동작
            var ar = evt.AttackRoll;
            if (!OathShieldRule.Applies(ar != null, ar != null && ar.Weapon != null, ar != null && ar.Weapon != null && ar.Weapon.Blueprint.IsMelee)) return; // 정당한 무동작: 근접 무기 피해만
            var ctx = Context;
            if (!Guard.Run(ErrorLedger.SiteOathShield, ctx, () =>
            {
                var paladin = ctx == null ? null : ctx.MaybeCaster;
                if (paladin == null) { RevenantErrors.Report(ErrorLedger.SiteOathShield, "aura owner (caster) missing"); return; }
                if (paladin == Owner || paladin.State.IsDead) return; // 정당한 무동작: 자기 자신 · 주인이 쓰러짐
                DamageSnapshot b;
                if (!evt.TryGetCustomData(before, out b)) { RevenantErrors.Report(ErrorLedger.SiteOathShield, "no pre-damage snapshot for this hit (AboutToTrigger not seen)"); return; }
                int taken = OathShieldRule.TakenAfterTemp(b.Damage, Owner.Damage);
                var s = OathShieldRule.Split(taken, RevenantRules.Dials.OathShieldAmount);
                if (s.Paladin == 0) return; // 정당한 무동작: 임시 HP 가 다 막았다
                Owner.Damage = OathShieldRule.Restore(Owner.Damage, s.Paladin);
                Rulebook.Trigger(new RuleDealDamage(paladin, paladin, new DirectDamage(DiceFormula.Zero, s.Paladin)));
            }, RevenantErrors.Sink)) return; // 실패는 계수됨 — 아군 피해 그대로
        }
    }

    // 영혼 수확(판정 기록 C1 — 무기로 실제로 죽인 적만): 소유자의 무기 피해로 HP 0 이하가 된 적을 보류로 적고(SoulHarvestLedger),
    // 그 유닛의 생명 상태가 「죽음」으로 바뀔 때 팔라딘 레벨만큼 회복. 죽지 않으면(기절·생존) 보류를 버린다. 본편 AddInitiatorAttackWithWeaponTrigger{ReduceHPToZero}+¬Alive 는
    // 사망 처리(UnitLifeController 다음 틱) 전에 평가되어 영원히 거짓이었다(검토 C1).
    public class SoulHarvestComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleDealDamage>, IUnitLifeStateChanged, ITickEachRound
    {
        static readonly WeakKeyTable<UnitEntityData, SoulHarvestLedger<UnitEntityData>> ledgers = new WeakKeyTable<UnitEntityData, SoulHarvestLedger<UnitEntityData>>();

        static TimeSpan Window { get { return new Rounds(RevenantRules.Dials.SoulHarvestWindowRounds).Seconds; } }

        public void OnEventAboutToTrigger(RuleDealDamage evt) { } // 정당한 무동작: 판정은 피해 반영 뒤

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt == null || evt.IsFake) return; // 정당한 무동작: 미리보기
            var target = evt.Target;
            if (target == null || target == Owner || target.HPLeft > 0) return; // 정당한 무동작: 치명 피해 아님
            if (!Guard.Run(ErrorLedger.SiteSoulHarvest, Fact, () =>
            {
                var ar = evt.AttackRoll;
                var wbp = ar == null || ar.Weapon == null ? null : ar.Weapon.Blueprint;
                // 관문 두 개(적 · 진짜 무기)는 인자 자리에서 바로 계산한다 — wiring 시험이 호출 순서·직접 사용을 잠근다(R2 4).
                var v = ledgers.GetValue(Owner, _ => new SoulHarvestLedger<UnitEntityData>())
                    .OnLethalHit(target, target.IsEnemy(Owner), SoulHarvestGate.RealWeaponKill(ar != null, wbp != null, wbp == null ? null : wbp.Category.ToString(), wbp == null ? null : wbp.AttackType.ToString()), target.State.IsDead, Game.Instance.TimeController.GameTime, Window);
                if (v == SoulHarvestVerdict.Heal) Heal();
            }, RevenantErrors.Sink)) return; // 실패는 계수됨 — 보류·회복 없음
        }

        // 라운드 틱: 파괴·게임 밖 유닛과 창이 지난 보류를 버린다(선택 5).
        public void OnNewRound()
        {
            SoulHarvestLedger<UnitEntityData> l;
            if (!ledgers.TryGetValue(Owner, out l) || l.PendingCount == 0) return; // 정당한 무동작: 보류 없음
            if (!Guard.Run(ErrorLedger.SiteSoulHarvest, Fact, () => { l.Prune(Game.Instance.TimeController.GameTime, u => u.Destroyed || !u.IsInGame); }, RevenantErrors.Sink)) return; // 실패는 계수됨
        }

        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        {
            SoulHarvestLedger<UnitEntityData> l;
            if (unit == null || unit == Owner || !ledgers.TryGetValue(Owner, out l) || !l.IsPending(unit)) return; // 정당한 무동작: 보류 중인 대상이 아님(전역 사건)
            if (!Guard.Run(ErrorLedger.SiteSoulHarvest, Fact, () =>
            {
                if (l.OnLifeStateChanged(unit, unit.State.IsDead, Game.Instance.TimeController.GameTime) == SoulHarvestVerdict.Heal) Heal();
            }, RevenantErrors.Sink)) return; // 실패는 계수됨 — 회복 없음
        }

        void Heal()
        {
            var paladin = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Guids.Game.PaladinClass);
            if (paladin == null) throw new InvalidOperationException("PaladinClass blueprint missing");
            int level = Owner.Progression.GetClassLevel(paladin);
            if (level < RevenantRules.Levels.MinLevel) throw new InvalidOperationException("soul harvest owner has paladin level " + level);
            Rulebook.Trigger(new RuleHealDamage(Owner, Owner, level));
            OathFx.SpawnOnUnit("SoulHarvest", Owner);   // 회복 뒤 연출(실패는 경고만 — 회복은 이미 끝)
        }
    }
}
