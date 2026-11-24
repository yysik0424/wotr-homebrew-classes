using System;

namespace ChimeraMonk
{
     
    //  표지는 시전자 자신의 새 라운드에 뗀다(Bulwark Rebuke.cs:128-134 선례). 순수 Decide 함수는 생산 소비자가 없어 지웠다(독립 검토 I-1).
    public static class VenomPlan
    {
        public static int Rounds { get { return ChimeraRules.Dials.VenomRounds; } }           // 설계 §7 6라운드
        public static Dice Die { get { return ChimeraRules.Dials.VenomDie; } }                 // 설계 §7 건강 1d2
        public const string Stat = "Constitution";                                             // 설계 §7 건강(엔진 StatType 이름)
        public const string Save = "Fortitude";                                                // 설계 §7 인내(엔진 SavingThrowType 이름)
        public static int DcMutagenBonus { get { return ChimeraRules.Dials.VenomMutagenDc; } } // 설계 §7 변이약 DC +2
    }
}
