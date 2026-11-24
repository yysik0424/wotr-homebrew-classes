using System;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics;

namespace PhantomBlade
{
     
    // 부르는 곳 = ContextActionStandStrike.RunAction 의 plan.Touch 갈래 하나(순수 StandRules.PlanStrike 가 TouchLevel 이상에서만 Touch 를 세운다).
    //
    // 왜 RuleAttackWithWeapon 이 아니라 독립 RuleAttackRoll + 피해 규칙인가(본체 명중 효과를 피하려고):
    //   - 무기 명중 트리거(AddInitiatorAttackWithWeaponTrigger — 다른 버프·특기의 「무기로 명중하면 …」)는 RuleAttackWithWeapon 처리기다
    //     (OnEventDidTrigger(RuleAttackWithWeapon) → AbstractWeaponTrigger.DidTrigger). 독립 RuleAttackRoll 은 그 규칙을 만들지 않으므로 거기 오지 않는다.
    //   - 본체의 공격 횟수·명령을 쓰지 않는다: UnitAttack 명령·SpendAction 없음(시험 고정).
    //   - 피해 규칙에 굴림을 달되(전투 로그·치명 표시) 암습·정밀 피해는 끈다: RulePrepareDamage.OnTrigger IL 0006·004C·005A 가 「굴림이 암습 ∧ ¬DisableSneakDamage」
    //     일 때만 암습을 더하고, RuleCalculateDamage.OnTrigger IL 0006 이 DisablePrecisionDamage 를 본다 → 본체의 암습·정밀이 스탠드에 얹히지 않는다.
    //   - 피해 묶음에 무기를 달지 않는다(DamageBundle(BaseDamage[])) — 무기 피해 조건 트리거(AbstractWeaponTrigger.IsSuitable(RuleDealDamage) 의 WeaponDamage)와 무관.
    //   남는 것(정당, 보고서 기록): 「공격 굴림」 자체를 보는 처리기(AddInitiatorAttackRollTrigger 등, 본편 110 블루프린트)는 이 굴림도 본다 — 귀신참 광선·접촉 주문과 같은 엔진 규칙.
    //
    // 명중 = BAB + 매력 vs 접촉 AC:
    //   - 공격 보너스 벌점 0(RuleAttackRoll(…, weapon, 0)) — 반복 공격 벌점 없이 BAB 전체.
    //   - 능력치 = 매력: 이 굴림 자신의 공격 보너스 계산에서만 StandAttackStatScope 가 ContextData<AttackBonusStatReplacement>.Setup(Charisma) 를 연다 —
     
    //     계산까지 매력이 됐다. 그 밖의 일반 보정(크기·버프 등)은 엔진 규칙대로 붙는다.
    //   - 접촉: 생성자가 무기의 공격 유형(주먹 = ClawType 근접)을 복사한 뒤(.ctor IL 003B~0040) AttackType = Touch 로 덮는다 → OnTrigger 가
     
    //     AC 없는 빗나감은 빗나감, 그 밖의 AC 결측·접촉 아닌 AC 는 던짐).
    // 피해 = 주먹 주사위(plan — 러시 원장 RushDiceByLevel) + 매력 수정치, 역장(주먹 블루프린트의 DamageType — 빌드 되읽기로 Force 고정).
    // 치명 = 지금 스탠드 주먹과 같은 규칙: 확정이면 BaseDamage.CriticalModifier = 굴림의 WeaponStats.CriticalMultiplier(RuleAttackWithWeapon.CreateDamage IL 004E~008C, 주먹 ×2).
    // 거울상: 무기 해결 규칙처럼 굴림 뒤 ConsumeMirrorImageIfNecessary(RuleAttackWithWeaponResolve.OnTrigger IL 0001~0006).
    internal static class StandTouchStrike
    {
        internal static void Strike(MechanicsContext context, UnitEntityData caster, UnitEntityData target, ItemEntityWeapon weapon, int diceCount, int dieSize)
        {
            if (context == null) throw new InvalidOperationException("PhantomBlade: stand touch without a MechanicsContext");
            if (caster == null || target == null) throw new InvalidOperationException("PhantomBlade: stand touch without caster or target");
            if (weapon == null || weapon.Blueprint == null) throw new InvalidOperationException("PhantomBlade: stand touch without a fist weapon");
            if (diceCount <= 0 || dieSize <= 0) throw new ArgumentOutOfRangeException("diceCount", diceCount + "d" + dieSize, "PhantomBlade: stand touch dice must be positive");
            var damageType = weapon.Blueprint.DamageType;
            if (damageType == null || damageType.Type != DamageType.Force) throw new InvalidOperationException("PhantomBlade: stand fist " + weapon.Blueprint.name + " damage type is not Force");

            var roll = new RuleAttackRoll(caster, target, weapon, 0)
            {
                AttackType = AttackType.Touch,
                Reason = context,
                DoNotProvokeAttacksOfOpportunity = true,
            };
             
            // StandAttackStatScope 가 이 굴림 자신의 공격 보너스 계산에서만 매력 문맥을 열고 닫는다. 굴린 뒤 실제로 매력이 쓰였는지 되읽는다(없으면 던짐).
            StandAttackStatScope.MarkStandTouch(roll);
            context.TriggerRule(roll);
            StandAttackStatScope.RequireApplied(roll, "stand touch");
             
            // 그 밖의 AC 결측·접촉 아닌 AC 는 던진다. 엔진 값을 그대로 넘긴다(시험이 IL 로 순서 잠금).
            var ac = roll.ACRule;
            bool hit = StandRules.TouchRollHit((StandRules.RollResult)(int)roll.Result, roll.IsHit, roll.AutoHit, roll.AutoMiss, ac != null, ac != null && ac.AttackType == AttackType.Touch);
            roll.ConsumeMirrorImageIfNecessary();
            if (!hit) return; // legitimate no-op: the stand's own touch attack missed (incl. concealment / auto-miss — no damage, the strike loop goes on)

            var damage = damageType.CreateDamage(new DiceFormula(diceCount, (DiceType)dieSize), caster.Stats.Charisma.Bonus);
            if (roll.IsCriticalConfirmed)
            {
                var stats = roll.WeaponStats;
                if (stats == null) throw new InvalidOperationException("PhantomBlade: stand touch crit without WeaponStats");
                damage.CriticalModifier = stats.CriticalMultiplier;
            }
            var deal = new RuleDealDamage(caster, target, new DamageBundle(damage))
            {
                AttackRoll = roll,
                DisableSneakDamage = true,
                DisablePrecisionDamage = true,
                Reason = context,
            };
            context.TriggerRule(deal);
        }
    }
}
