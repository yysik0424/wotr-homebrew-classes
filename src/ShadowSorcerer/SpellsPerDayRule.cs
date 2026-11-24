namespace ShadowSorcerer
{
     
    // 엔진 근거(Spellbook.GetSpellsPerDay, dnlib 2026-11-14):
    //   IL_0087~00A7 능력치 Min(ModifiedValue, 영구값) < 10 + 주문 레벨 → 표를 보기 전에 0.
    //   IL_00A8~00C5 표 GetCount(시전자 레벨, 주문 레벨) == −1 → 0(보너스도 없음).
    //   IL_00C6~0137 그 밖 = 표 + UnitPartExtraSpellsPerDay + 능력치 보너스.
    // 그래서 기본 슬롯만 최대 2칸 줄이면 능력치 보너스 칸은 그대로다(설계 §7). 0 아래로 내리지 않는다 = 표 값보다 많이 빼지 않는다.
    public struct SlotFacts
    {
        public int SpellLevel;
        public bool? AbilityTooLow;   // null = 능력치를 읽지 못함
        public int? TableValue;       // null = 표를 읽지 못함
        public int EngineResult;      // 원래(본편 + 앞선 Postfix) 결과
    }

    public enum SlotsUnknown { None, MissingAbility, MissingTable, NegativeAfterAdjust }

    public struct SlotsDecision
    {
        public int Value;
        public SlotsUnknown Unknown;  // None 이 아니면 호출자가 ReportOnce
        public override string ToString() { return Value + (Unknown == SlotsUnknown.None ? "" : " (" + Unknown + ")"); }
    }

    public static class SpellsPerDayRule
    {
        // 생산 진입점: ShadowRules 를 ldsfld 로 읽어 넘긴다(시험 wiring: rule entry loads ShadowRules).
        public static bool IsLegacySurplus(int remaining, int currentCap, int previousCap)
        {
            return remaining > currentCap && remaining <= previousCap;
        }

        public static SlotsDecision Decide(SlotFacts f) { return Core(f, ShadowRules.SpellsPerDayDelta, ShadowRules.SpellsPerDayMinLevel, ShadowRules.SpellsPerDayMaxLevel); }

        public static SlotsDecision Core(SlotFacts f, int delta, int minLevel, int maxLevel)
        {
            var d = new SlotsDecision { Value = f.EngineResult, Unknown = SlotsUnknown.None };
            if (f.SpellLevel < minLevel || f.SpellLevel > maxLevel) return d;            // 범위 밖(캔트립 0 등) → 그대로
            if (!f.AbilityTooLow.HasValue) { d.Unknown = SlotsUnknown.MissingAbility; return d; }
            if (f.AbilityTooLow.Value) return d;                                         // 엔진이 이미 0(정상)
            if (!f.TableValue.HasValue) { d.Unknown = SlotsUnknown.MissingTable; return d; }
            if (f.TableValue.Value < 0) return d;                                        // 열리지 않은 레벨(표 −1 → 엔진 0)
            int cut = -delta < f.TableValue.Value ? -delta : f.TableValue.Value;          // min(−delta, 표)
            int v = f.EngineResult - cut;
            if (v < 0) { d.Value = 0; d.Unknown = SlotsUnknown.NegativeAfterAdjust; return d; }
            d.Value = v;
            return d;
        }
    }
}
