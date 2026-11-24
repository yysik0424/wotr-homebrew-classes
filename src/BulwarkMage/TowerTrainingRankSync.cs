using System;
using System.Linq;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics.Actions;
using WotrClassMod.Common;

namespace BulwarkMage
{
     
    // 우리 타워실드 훈련 복제(TowerShield.Training)에 붙는다. 저장 상태 없음.
    // 원인(dnlib 2026-11-18, 머리말 = TrainingRankRule): 레벨업의 Feature.AddRank @0029~0042 가 특성을 껐다 켜면 문 HasShieldFeatureUnlock 이 버프를 지우고 1랭크로 다시 붙이는데,
    //   그때 특성은 TurnOn 전이라 FactsChangeTrigger 가 획득 사건을 못 받아 반복 적용이 돌지 않는다. ReapplyFeaturesOnLevelUp @002E~0043 은 ReapplyOnLevelUp 거짓이면 Context.Recalculate 만 한다.
    // 사건: IUnitLevelUpHandler.HandleUnitAfterLevelUp — LevelUpController.ApplyLevelup 이 ApplyLevelUpActions(@0091, 특성 부여) 뒤 @00A3 에서 낸다(전역 구독, IGlobalSubscriber 상속).
    // 하는 일: 목표 랭크 = TrainingRankRule.GainRank(우리 복제 반복 행동의 Value 를 이 특성 문맥으로 계산한 반복 횟수, 버프 Ranks) — 엔진이 버프를 새로 얻을 때와 같은 값.
    //   다르면 Buff.SetRank(목표, true)(엔진: 껐다 켜기 → ArmorCheckPenaltyIncrease.OnTurnOn 이 방패 RecalculateStats) + 되읽기, 그 뒤 보조손 방패 ArmorComponent.RecalculateStats 를 직접 한 번 더.
    // 예외 계약: 엔진으로 던지지 않는다(catch → BulwarkErrors.ReportException). 판정 불가·입력 결측은 BulwarkErrors.Report(ERROR 줄 1) — 「맞음」으로 지나가지 않는다(C1).
    // 한계(README): 레벨업 사건 밖에서 훈련 랭크가 바뀌는 경로(외부 모드의 특성 직접 부여 등)는 보지 않는다 — 방패를 뺐다 다시 들면 엔진 경로로 맞는다.
    public class TowerTrainingRankSync : UnitFactComponentDelegate, IUnitLevelUpHandler
    {
        public const string Where = "training-rank";

        public void HandleUnitBeforeLevelUp(UnitEntityData unit) { } // 정당한 무동작: 레벨업 완료(After)만 본다

        public void HandleUnitAfterLevelUp(UnitEntityData unit, LevelUpController controller)
        {
            if (unit == null || !ReferenceEquals(unit, Owner)) return; // 다른 유닛의 레벨업 = 정당한 무동작(전역 사건)
            Sync();
        }

        internal void Sync()
        {
            try
            {
                var owner = Owner;
                if (owner == null) { BulwarkErrors.Report(Where, "training rank component without an owner - nothing synced"); return; }
                var buffBp = TowerShield.TrainingBuff;
                var trainingBp = TowerShield.Training;
                if (buffBp == null || trainingBp == null) { BulwarkErrors.Report(Where, owner.CharacterName + ": tower shield training blueprints are not built - nothing synced"); return; }
                if (Fact == null || !ReferenceEquals(Fact.Blueprint, trainingBp)) { BulwarkErrors.Report(Where, owner.CharacterName + ": training rank component sits on " + (Fact == null ? "no fact" : Fact.Blueprint.name) + ", expected " + trainingBp.name); return; }
                var fct = trainingBp.ComponentsArray.OfType<FactsChangeTrigger>().SingleOrDefault();
                var acts = fct == null || fct.OnFactGainedActions == null ? null : fct.OnFactGainedActions.Actions;
                var rep = acts != null && acts.Length == 1 ? acts[0] as ContextActionRepeatedActions : null;
                if (rep == null || rep.Value == null) { BulwarkErrors.Report(Where, owner.CharacterName + ": training clone has no single repeated gained action - nothing synced"); return; }

                // 반복 횟수 = 엔진이 획득 때 쓰는 그 값(RunAction @000C: Value.Calculate(문맥)). 특성 랭크가 방금 바뀌었으니 문맥 랭크를 다시 계산한다(엔진 ReapplyFeaturesOnLevelUp 과 같은 호출).
                var ctx = Fact.MaybeContext;
                int? repeats = null;
                if (ctx != null) { ctx.Recalculate(); repeats = rep.Value.Calculate(ctx); }
                var buff = owner.Buffs.GetBuff(buffBp);
                int? rank = buff == null ? (int?)null : buff.GetRank();
                var off = owner.Body.SecondaryHand == null ? null : owner.Body.SecondaryHand.MaybeShield;
                var kind = off == null ? ShieldTowerKind.NotTower : BondOffHandEnchant.TowerOf(off);
                var plan = TrainingRankRule.Decide(repeats, buffBp.Ranks, rank, kind);
                if (plan.Verdict == TrainingRankVerdict.Unknown) { BulwarkErrors.Report(Where, owner.CharacterName + ": " + plan.Problem + (ctx == null ? " (training fact has no context)" : "")); return; }
                if (plan.Verdict == TrainingRankVerdict.NoBuff) return; // 보조손에 타워실드 없음 — 맞출 버프가 없다(방패를 들면 엔진이 새로 붙인다)

                if (plan.Verdict == TrainingRankVerdict.SetRank)
                {
                    int before = buff.GetRank();
                    buff.SetRank(plan.Target, true);
                    int after = buff.GetRank();
                    if (after != plan.Target) { BulwarkErrors.Report(Where, owner.CharacterName + ": training buff rank " + before + " -> SetRank(" + plan.Target + ") read back " + after); return; }
                    ModContext.Log("BulwarkMage: " + owner.CharacterName + ": tower shield training buff rank " + before + " -> " + after + " after level-up");
                }
                if (off == null) { BulwarkErrors.Report(Where, owner.CharacterName + ": training buff present but no shield in the off hand to recalculate"); return; }
                var armor = off.ArmorComponent;
                if (armor == null) { BulwarkErrors.Report(Where, owner.CharacterName + ": off-hand shield " + off.Blueprint.name + " has no armor component - cannot recalculate"); return; }
                armor.RecalculateStats();
            }
            catch (Exception e)
            {
                BulwarkErrors.ReportException(Where, e);
            }
        }
    }
}
