using System;

namespace BloodBerserker
{
    // Owner-approved combat values, 2026-11-23. Distances are in feet.
    internal static class Rules
    {
        internal const int ChainFeet = 30, ExtendedFeet = 60, BloodPrice = 5;
        internal const int ReachLevel = 7, ConductionLevel = 10, CriticalLevel = 13;
        internal static readonly int[] ReductionLevels = { 7, 10, 13, 16, 19 };

        internal static int WeaponDice(int level, bool critical)
        {
            if (level < 1) throw new ArgumentOutOfRangeException("level");
            int dice = level >= 19 ? 4 : level >= CriticalLevel ? 3 : level >= ReachLevel ? 2 : 1;
            return critical && level >= CriticalLevel ? 2 * dice : dice;
        }

        internal static bool PullTarget(int casterSize, int targetSize) { return targetSize <= casterSize; }
    }
}
