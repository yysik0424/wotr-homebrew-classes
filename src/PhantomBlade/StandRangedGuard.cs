using System;
using System.Runtime.CompilerServices;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using WotrClassMod.Common;

namespace PhantomBlade
{
    // 스탠드 원거리 방어(설계 §4.4: 6레벨 +2, 14레벨 +4). StandBuff 컴포넌트라 토글을 끄면 효과도 없다(설계 §4.2, 컨트롤러 판정 1).
     
    //   일반 광선 주문은 RuleAttackWithWeapon 없이 RuleAttackRoll 을 직접 만든다(AbilityDeliverProjectile/<Deliver>d__36 IL 0500~0538, 무기 = RayItem).
    //   무기 공격은 RuleAttackWithWeapon.OnTrigger 가 자기 RuleAttackRoll 을 만들어 발동한다(IL 0025 newobj, 00B1 Trigger) → 둘 다 이 훅을 지난다.
    //   RuleCalculateAC 는 RuleAttackRoll.OnTrigger 안에서만 만들어진다(본 AC 00E8 또는 018A 중 하나 + 치명타 확인 0397; 그 밖은 AI 추정 PowerAttackWatcher).
    //   계산 규칙 하나마다 AddModifier 1회 → 같은 AC 계산에 두 번 더해지지 않는다(본편과 같이 치명타 확인 AC 에도 붙는다).
    //   원거리 판정 = rule.Weapon.Blueprint.IsRanged (= 무기 형식 AttackType Ranged(2)|RangedTouch(3), BlueprintWeaponType.get_IsRanged IL; RayType 은 RangedTouch).
    //   AboutToTrigger(RuleAttackRoll): 원거리면 {Roll, Initiator, 보너스} 보관
    //   AboutToTrigger(RuleCalculateAC): 보관한 Initiator == rule.Initiator 이면 rule.AddModifier(보너스, Fact, Descriptor) (본편 IL 0029~0043)
    //   DidTrigger(RuleAttackRoll): 그 Roll 의 보관을 지운다. 규칙 단계마다 SaveExecute 가 예외를 잡으므로(Rulebook.SaveExecute IL) OnTrigger 가 던져도 Did 는 돈다
    //   (단 SaveExecute 가 다시 던지는 예외 형식이나 규칙이 TriggerEvent 밖에서 끊기면 Did 가 빠질 수 있다 → DropFinished).
    // 보관 칸은 사실(fact) 키의 ConditionalWeakTable(본편은 ComponentRuntime.m_Data([JsonProperty]) — 모드 형식이 세이브에 들어가지 않게).
    // 중첩(원거리 굴림 처리 중 같은 대상에 또 원거리 굴림)은 Outer 사슬로 되살린다 — 안쪽 Did 가 바깥 보관을 지우지 않게.
    // Did 가 빠진 찌꺼기(이미 끝난 굴림의 보관)는 던지지 않고 기록 후 걷어낸다(DropFinished, fix round 1 ③).
    public class StandRangedGuard : UnitFactComponentDelegate, ITargetRulebookHandler<RuleAttackRoll>, ITargetRulebookHandler<RuleCalculateAC>
    {
        sealed class Pending
        {
            public RuleAttackRoll Roll;
            public UnitEntityData Initiator;
            public int Bonus;
            public Pending Outer;
        }

        static readonly ConditionalWeakTable<EntityFact, Pending> pending = new ConditionalWeakTable<EntityFact, Pending>();

        // 설계 §4.4 「유형 없음 보너스」: 이전 ACBonusAgainstAttacks 배선과 같은 누적 서술자.
        const ModifierDescriptor Descriptor = ModifierDescriptor.UntypedStackable;

        public void OnEventAboutToTrigger(RuleAttackRoll rule)
        {
            // 생성자가 Weapon.Blueprint 를 역참조하므로(RuleAttackRoll..ctor IL 0049~005A / 002A~003B) 정상 경로에선 null 이 아니다 — 아니면 던진다.
            var weapon = rule.Weapon;
            if (weapon == null) throw new InvalidOperationException("PhantomBlade: StandRangedGuard got a RuleAttackRoll without a weapon");
            // 정당한 무동작: 근접·접촉 공격(본편 Check 의 AgainstRangedOnly 분기 — false 면 보관하지 않는다).
            if (!weapon.Blueprint.IsRanged) return;
            int bonus = StandRules.RangedAcBonus(StandOwner.Level(Owner, "StandRangedGuard", StandRules.StandLevel));
            // 정당한 무동작: 6레벨 미만은 보너스 0(StandRules.RangedAcBonus).
            if (bonus == 0) return;
            Pending outer = DropFinished("attack roll");
            pending.Remove(Fact);
            pending.Add(Fact, new Pending { Roll = rule, Initiator = rule.Initiator, Bonus = bonus, Outer = outer });
        }

        public void OnEventDidTrigger(RuleAttackRoll rule)
        {
            Pending top;
            // 정당한 무동작: 보관한 원거리 굴림이 없다(근접·보너스 0).
            if (!pending.TryGetValue(Fact, out top)) return;
            if (!ReferenceEquals(top.Roll, rule))
            {
                for (var p = top.Outer; p != null; p = p.Outer)
                    if (ReferenceEquals(p.Roll, rule))
                    {
                        // 안쪽 굴림의 Did 가 빠졌다. 던지면 보관이 남아 이후 매번 던진다 → 기록하고 이 굴림까지 통째로 걷어낸다(fix round 1 ③).
                        ModContext.Log("StandRangedGuard: attack roll finished with an inner ranged roll still stashed; dropping the stale stash (" + (Owner == null ? "?" : Owner.CharacterName) + ")");
                        pending.Remove(Fact);
                        if (p.Outer != null) pending.Add(Fact, p.Outer);
                        return;
                    }
                // 정당한 무동작: 이 굴림은 보관하지 않았다(근접·보너스 0) — 바깥 원거리 굴림의 보관은 그대로 둔다.
                return;
            }
            pending.Remove(Fact);
            if (top.Outer != null) pending.Add(Fact, top.Outer);
        }

        public void OnEventAboutToTrigger(RuleCalculateAC rule)
        {
            Pending p = DropFinished("AC calculation");
            // 정당한 무동작: 보관한 원거리 공격이 없거나 다른 공격자의 AC 계산(본편 IL 000B~0029 와 같은 조건).
            if (p == null || p.Initiator != rule.Initiator) return;
            rule.AddModifier(p.Bonus, Fact, Descriptor);
        }

        // 이미 끝난 굴림(IsTriggered: RulebookEvent.OnDidTrigger IL 0002 에서 선다 = 우리 Did 단계 뒤)의 보관은 Did 가 빠진 찌꺼기다.
        // 남겨 두면 다음 근접 AC 에 보너스가 잘못 붙고, 던지면 찌꺼기가 영구히 남아 이 주인의 공격마다 던진다 → UMM 로그에 남기고 걷어낸다
        // (엔진 한 번의 사고가 방어를 영구히 망가뜨리지 않게, fix round 1 ③). 반환 = 정리 뒤 맨 위(없으면 null).
        Pending DropFinished(string where)
        {
            Pending top;
            if (!pending.TryGetValue(Fact, out top)) return null;
            var live = top;
            int dropped = 0;
            while (live != null && live.Roll.IsTriggered) { live = live.Outer; dropped++; }
            if (dropped == 0) return top;
            ModContext.Log("StandRangedGuard: dropped " + dropped + " stale ranged stash(es) at " + where + " (DidTrigger(RuleAttackRoll) never ran) (" + (Owner == null ? "?" : Owner.CharacterName) + ")");
            pending.Remove(Fact);
            if (live != null) pending.Add(Fact, live);
            return live;
        }

        // 정당한 무동작: 본편도 비어 있다(ACBonusAgainstAttacks.OnEventDidTrigger(RuleCalculateAC) IL 0000 ret).
        public void OnEventDidTrigger(RuleCalculateAC rule) { }
    }
}
