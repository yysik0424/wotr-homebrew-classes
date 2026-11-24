using System;

namespace ShadowSorcerer
{
     
    //   F == 0 → DisbeliefFactorPercent(50) · F > 0(그림자 전환 주문) → max(1, F × 50 / 100) (예: 그림자 환기 20 → 10, 60 → 30) · F < 0 → 던짐(엔진이 음수를 넣을 수 없다 = 계약 위반).
    public static class ShadowFactorRule
    {
        public static int Decide(int engineFactor) { return Core(engineFactor, ShadowRules.DisbeliefFactorPercent); }

        public static int Core(int engineFactor, int disbeliefPercent)
        {
            if (engineFactor < 0) throw new ArgumentOutOfRangeException("engineFactor", engineFactor, "ShadowSorcerer: negative engine shadow factor");
            if (engineFactor == 0) return disbeliefPercent;
            int v = engineFactor * disbeliefPercent / 100;
            return v < 1 ? 1 : v;
        }
    }
}
