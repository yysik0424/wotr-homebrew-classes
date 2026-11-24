using System;

namespace Warlock
{
    public enum HeatCostVerdict { Ok, NotEnoughHeat, AboveMaxSpellLevel, Unknown }

    // [순수] 과열 소환 변형의 사용 제한(T5 WarlockHeatCost : IAbilityRestriction 이 이것만 따른다 — Ok 만 true, Unknown 은 false + ERROR 1회).
    public static class HeatCostRule
    {
        // maxSpellLevel(계획 T1 시그니처 이름) = 실제로는 최고 소환 단계 max tier = min(9, 최고 주문 레벨)(WarlockRules.MaxTierAt, T5 Summons.MaxTier) 또는 null = 판정 불가(주문서를 못 찾음).
        // tier ≤ 9 이므로 tier > min(9, 최고 주문 레벨) ⇔ tier > 최고 주문 레벨(진행 원장 T1→T5 행). 순서 Unknown → AboveMax → NotEnough → Ok.
        // 판정 → 사용 가능 여부(T5 WarlockHeatCost 의 반환값). Ok 만 true — Unknown(판정 불가)·NotEnoughHeat·AboveMaxSpellLevel 은 false.
        public static bool Allows(HeatCostVerdict v)
        {
            switch (v)
            {
                case HeatCostVerdict.Ok: return true;
                case HeatCostVerdict.NotEnoughHeat: case HeatCostVerdict.AboveMaxSpellLevel: case HeatCostVerdict.Unknown: return false;
                default: throw new ArgumentOutOfRangeException("v", v, "Warlock: unknown heat cost verdict");
            }
        }

        public static HeatCostVerdict Decide(int rank, int tier, int? maxSpellLevel)
        {
            if (rank < 0 || rank > WarlockRules.Dials.HeatMax) throw new ArgumentOutOfRangeException("rank", rank, "Warlock: heat rank must be 0.." + WarlockRules.Dials.HeatMax);
            if (tier < 1 || tier > WarlockRules.Dials.SummonTiers) throw new ArgumentOutOfRangeException("tier", tier, "Warlock: summon tier must be 1.." + WarlockRules.Dials.SummonTiers);
            if (maxSpellLevel.HasValue && (maxSpellLevel.Value < 1 || maxSpellLevel.Value > WarlockRules.Dials.SummonTiers))
                throw new ArgumentOutOfRangeException("maxSpellLevel", maxSpellLevel, "Warlock: maxSpellLevel (= max tier) must be 1.." + WarlockRules.Dials.SummonTiers + " or null (undeterminable)");
            if (!maxSpellLevel.HasValue) return HeatCostVerdict.Unknown;
            if (tier > maxSpellLevel.Value) return HeatCostVerdict.AboveMaxSpellLevel;
            if (rank < tier) return HeatCostVerdict.NotEnoughHeat;
            return HeatCostVerdict.Ok;
        }
    }
}
