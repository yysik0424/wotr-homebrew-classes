using System;
namespace Spiritsteel
{
    internal static class Rules
    {
        public const int MinimumRounds = 2, MaximumRounds = 5, TripLevel = 6;
        public static int Rounds(int wisdom) { return Math.Max(MinimumRounds, Math.Min(MaximumRounds, 1 + wisdom)); }
        public static bool FlexibleSlot(int level) { return level >= 4 && level <= 20 && level != TripLevel && level % 2 == 0; }
    }
}
