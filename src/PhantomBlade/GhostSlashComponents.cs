using System;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Mechanics.ContextData;

namespace PhantomBlade
{
    // 귀신참(설계 §13.2, Task 6c)의 게임 쪽 연결부 3개. 셋 다 「레벨 = StandOwner.Level(…, GhostSlash.FirstLevel) → 순수 GhostSlash 함수」만 한다(산술 없음, 시험이 IL 로 고정).
    // 레벨을 모르거나(아키타입 아님) 5 미만이면 StandOwner.Level 이 던진다 — 능력은 5레벨 특성에서만 오므로 그 경우는 배선 결함이다(C1: 조용히 0발·0 비용으로 가지 않는다).

    // 투사체 수(BlueprintUnitProperty 게터). 능력의 ContextRankConfig(CustomProperty, AsIs)가 이 값을 랭크 Default 로 읽고,
    // AbilityDeliverProjectile(UseMaxProjectilesCount)이 m_Projectiles 에서 그만큼 Take 한다(<Deliver>d__35 IL 01E4~0214).
    public class GhostSlashProjectilesGetter : PropertyValueGetter
    {
        protected override int GetBaseValue(UnitEntityData unit)
        {
            return GhostSlash.Projectiles(StandOwner.Level(unit, "ghost slash projectiles", GhostSlash.FirstLevel));
        }
    }

    // 시전 비용(귀신참 전용 하루 횟수 — 소유자 결정 2026-11-16, 설계 §14 ③. 종전: 흡수 풀 점수). 선례: 본편 AbilityMagusSpellRecallCostCalculator(Classes/Magus/SpellRecall/MagusSpellRecall.jbp 1bd76e00…:
    // AbilityResourceLogic.CostIsCustom=true + 이 인터페이스의 컴포넌트). AbilityResourceLogic.CalculateCost IL 0000~0045 가 GetComponent<IAbilityResourceCostCalculator> 로 부르고,
    // IsAbilityRestrictionPassed IL 000B~002A 가 HasEnoughResource(자원, 이 값) 으로 시전 가능 여부를, Spend IL 008F 가 소모량을 정한다.
     
    public class GhostSlashCost : BlueprintComponent, IAbilityResourceCostCalculator
    {
        public int Calculate(AbilityData ability)
        {
            if (ability == null) throw new ArgumentNullException("ability");
            var caster = ability.Caster;
            if (caster == null) throw new InvalidOperationException("PhantomBlade: ghost slash cost without a caster (AbilityData.Caster is null)");
            int level = StandOwner.Level(caster.Unit, "ghost slash cost", GhostSlash.FirstLevel);
             
            StandOwner.RequireGhostSlashUses(caster.Unit, "ghost slash cost");
            return GhostSlash.Cost(level);
        }
    }

    // 투사체 한 발의 피해. ContextActionDealDamage 의 주사위 종류(DiceType)는 고정 필드라 레벨마다 바뀌는 러시 주사위를 한 액션으로 못 쓴다 →
    // StandRules.RushDiceByLevel 행마다 본편 ContextActionDealDamage 하나씩(ByDiceRow[i], 빌드 때 행 i 의 주사위로 만들고 되읽음)을 두고, 현재 레벨의 행만 실행한다.
    // 피해·치명타·피해 규칙은 본편 그대로(같은 MechanicsContext·ContextAttackData 안에서 ActionList.Run — 본편 Conditional 과 같은 호출).
    public class ContextActionGhostSlashDamage : ContextAction
    {
        public ActionList[] ByDiceRow;

        public override string GetCaption() { return "Phantom Blade ghost slash damage (rush dice by level + Charisma, force)"; }

        public override void RunAction()
        {
            var context = Context;
            if (context == null) throw new InvalidOperationException("PhantomBlade: ghost slash damage without a MechanicsContext");
            UnitEntityData caster = context.MaybeCaster;
            if (caster == null) throw new InvalidOperationException("PhantomBlade: ghost slash damage has no caster (Context.MaybeCaster is null)");
            var lists = ByDiceRow;
            var rows = StandRules.RushDiceByLevel;
            if (lists == null || lists.Length != rows.Count) throw new InvalidOperationException("PhantomBlade: ghost slash damage has " + (lists == null ? "no" : lists.Length.ToString()) + " dice variants, StandRules.RushDiceByLevel has " + rows.Count);
            int idx = GhostSlash.DiceRowIndex(StandOwner.Level(caster, "ghost slash damage", GhostSlash.FirstLevel));
            var list = lists[idx];
            var deal = list == null || list.Actions == null || list.Actions.Length != 1 ? null : list.Actions[0] as ContextActionDealDamage;
            if (deal == null) throw new InvalidOperationException("PhantomBlade: ghost slash damage variant " + idx + " is not exactly one ContextActionDealDamage");
            // 실행 직전 재확인(C1): 고른 변형의 주사위 == 표 행. 빌드 되읽기와 같은 조건 — 표나 변형이 어긋나면 엉뚱한 주사위로 치지 않고 던진다.
            var row = rows[idx];
            var v = deal.Value;
            if (v == null || v.DiceCountValue == null || v.DiceCountValue.ValueType != ContextValueType.Simple || v.DiceCountValue.Value != row.Count || (int)v.DiceType != row.Size)
                throw new InvalidOperationException("PhantomBlade: ghost slash damage variant " + idx + " dice do not match StandRules.RushDiceByLevel row " + row.Count + "d" + row.Size);
             
            // <Deliver>d__36 IL 0447~0453). 굴림은 AbilityExecutionProcess.ApplyEffect IL 000E 가 연 ContextAttackData 에 있다(귀신참은 NeedAttackRoll).
            var atk = ContextData<ContextAttackData>.Current;
            if (atk == null || atk.AttackRoll == null) throw new InvalidOperationException("PhantomBlade: ghost slash damage outside the ray's attack (no ContextAttackData.AttackRoll)");
            StandAttackStatScope.RequireApplied(atk.AttackRoll, "ghost slash");
            list.Run();
        }
    }
}
