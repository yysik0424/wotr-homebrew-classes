using System;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.ContextData;

namespace PhantomBlade
{
    // 스탠드 러시 타격(설계 §4.3, 정찰 §2 대안 B). 본편 ContextActionAttackWithWeapon 의 IL 을 따르되 대상은 Context.MainTarget 이 아니라
    // ContextAction.Target(트리거가 넘긴 공격 대상)이다 — MainTarget 은 비면 버프 주인(본체)을 돌려준다(정찰 §2 IL).
    // 규칙 필드 설정은 본편 ContextActionMeleeAttack.RunAttackRule(IL 000A~0067)과 같은 순서·의미: Reason=Context, ExtraAttack, AttackNumber/AttacksCount.
    // 재귀: ExtraAttack=true 인 이 타격은 NotExtraAttack=true 트리거(StandBuff·FullAttackBuff 둘 다)를 통과하지 못한다
    // (AbstractWeaponTrigger.IsSuitable(RuleAttackWithWeapon) IL 03BB~03CB: NotExtraAttack && rule.ExtraAttack → false).
    // Task 6d(귀신 접촉, 설계 §13.3): 트리거는 빗나감에도 온다(OnlyHit=false). 명중 여부는 엔진이 행동 동안 연 ContextAttackData(본체 굴림)에서 읽고
    // (AbstractWeaponTrigger.RunActions IL 0021~0037), 순수 PlanStrike 가 「10 미만 = 명중에만 얹힘 / 10 이상 = 공격마다 스탠드 자기 접촉 굴림」을 정한다.
    // 접촉 타격(plan.Touch)은 StandTouchStrike(독립 RuleAttackRoll + 피해 규칙 — RuleAttackWithWeapon 이 없어 본체 무기 명중 트리거가 오지 않는다).
    public class ContextActionStandStrike : ContextAction
    {
        // 0 = StandRules.StrikesPerHit(전력 공격 라운드?) — 평상 1타, 전력 공격 FullAttackStrikesPerHit(Task 6b). 양수면 그 횟수. 음수는 잘못된 설정이라 실패.
        public int StrikesOverride;

        public override string GetCaption()
        {
            return "Phantom Blade stand strike (" + (StrikesOverride > 0 ? StrikesOverride.ToString() : "by level") + ")";
        }

        public override void RunAction()
        {
            if (StrikesOverride < 0) throw new InvalidOperationException("PhantomBlade: ContextActionStandStrike.StrikesOverride is negative (" + StrikesOverride + ")");
            var context = Context;
            if (context == null) throw new InvalidOperationException("PhantomBlade: stand strike without a MechanicsContext");
            UnitEntityData caster = context.MaybeCaster;
            if (caster == null) throw new InvalidOperationException("PhantomBlade: stand strike has no caster (Context.MaybeCaster is null)");
            var tw = Target;
            UnitEntityData target = ReferenceEquals(tw, null) ? null : tw.Unit;
            if (target == null) throw new InvalidOperationException("PhantomBlade: stand strike has no target unit (ContextAction.Target.Unit is null)");
            if (ReferenceEquals(target, caster)) throw new InvalidOperationException("PhantomBlade: stand strike target is the caster itself (" + caster.CharacterName + ")");
            if (Archetype.Bp == null) throw new InvalidOperationException("PhantomBlade: stand strike before Archetype.Build()");
            // 아키타입·레벨 도출은 StandOwner.Level 한 곳(C4). 아키타입 아님·레벨 < RushLevel 이면 거기서 던진다(트리거는 Rush3 보유로 걸러진다).
            int level = StandOwner.Level(caster, "stand strike", StandRules.RushLevel);
             
            // 그 라운드 첫 RushStrikes(레벨) 번의 명중이 각각 칠 수 있다(StandRules.MayStrike). 전력 공격 버프 중에는 제한이 풀린다(설계 §4.3 「스탠드 전력 공격」) →
            // 세지 않고 명중마다 친다. 버프 판정은 ContextConditionCasterHasFact 와 같은 HasFact(IL 0035~0040).
            var fullAttack = StandBlueprints.FullAttackBuff;
            if (fullAttack == null) throw new InvalidOperationException("PhantomBlade: stand strike before StandBlueprints.Build() (FullAttackBuff is null)");
            bool fullAttackRound = caster.Descriptor.HasFact(fullAttack);
            // Task 6d: 이번 본체 공격의 명중 여부(빗나감도 여기 온다 — 트리거 OnlyHit=false). 결측은 던진다(「빗나감」으로 조용히 읽지 않음).
            bool ownerHit = OwnerAttackHit(caster);
            // 계획 = 순수 StandRules.PlanStrike(타수·주사위·라운드 상태·접촉 여부). 주사위 = 타격 시점의 현재 전사 레벨(본편 몽크 맨손 표). 레벨을 모르면 위 StandOwner.Level 이 이미 던졌다.
            var plan = StandRushState.TryConsume(caster, level, fullAttackRound, ownerHit);
            if (!plan.Allowed) return; // legitimate no-op: this round's rush strikes are used up, or a miss below TouchLevel (design §13.1, §13.3)
            int n = StrikesOverride > 0 ? StrikesOverride : plan.Strikes;
            var bp = StandBlueprints.FistFor(plan.DiceCount, plan.DieSize);
            for (int i = 0; i < n; i++)
            {
                // 임시 무기: 소유자·슬롯 없음(본편 ContextActionAttackWithWeapon 과 같은 방식, 정찰 §2·§3).
                var weapon = new ItemEntityWeapon(bp, null);
                if (plan.Touch)
                {
                    // 10레벨 이상: 스탠드 자기 근접 접촉 굴림(BAB + 매력 vs 접촉 AC) → 명중이면 같은 주사위 + 매력 역장 피해.
                    StandTouchStrike.Strike(context, caster, target, weapon, plan.DiceCount, plan.DieSize);
                }
                else
                {
                    var rule = new RuleAttackWithWeapon(caster, target, weapon, 0)
                    {
                        Reason = context,
                        ExtraAttack = true,
                        AttackNumber = i,
                        AttacksCount = n,
                    };
                    // 기준값: 피해 보너스 능력치 = 매력 ×1 (RuleCalculateWeaponStats.OnTrigger IL 030E~0384, 배율 기본 1.0; 1.5·0.5 경로는 무기 소유자 없음으로 안 탐).
                    // 단 능력치 교체 컴포넌트(AnyWeaponDamageStatReplacement·DamageGrace·WeaponTypeDamageStatReplacement)가 OnEventAboutToTrigger 에서
                    // 더 높은 능력치로 다시 덮을 수 있다 — 다른 모든 공격과 같은 엔진 규칙이라 허용(컨트롤러 판정, 전투 로그에 보임).
                    rule.WeaponStats.OverrideDamageBonusStat(StatType.Charisma);
                    context.TriggerRule(rule);
                }
            }
        }

        // 트리거가 행동을 부르는 동안 엔진이 연 본체 공격 굴림의 명중 여부(AbstractWeaponTrigger.RunActions IL 0021~0037: TriggerBeforeAttack 가 아니면
        // ContextData<ContextAttackData>.Request().Setup(evt.AttackRoll, …) 를 열고 행동을 돌린다). 없거나 굴림이 비었거나 다른 유닛의 굴림이면 배선 오류 → 던진다.
        static bool OwnerAttackHit(UnitEntityData caster)
        {
            var data = ContextData<ContextAttackData>.Current;
            if (data == null) throw new InvalidOperationException("PhantomBlade: stand strike outside a weapon trigger (no ContextAttackData; the rush action must run from AddInitiatorAttackWithWeaponTrigger after the roll)");
            var roll = data.AttackRoll;
            if (roll == null) throw new InvalidOperationException("PhantomBlade: stand strike: ContextAttackData has no AttackRoll");
            if (!ReferenceEquals(roll.Initiator, caster)) throw new InvalidOperationException("PhantomBlade: stand strike: the attack roll in context belongs to " + (roll.Initiator == null ? "nobody" : roll.Initiator.CharacterName) + ", not the stand owner " + caster.CharacterName);
            return roll.IsHit;
        }
    }
}
