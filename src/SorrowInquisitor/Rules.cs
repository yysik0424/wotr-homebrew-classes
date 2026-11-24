using System;
namespace SorrowInquisitor
{
    public enum Rite { Gather, Standard, Move, Swift, Blade, Vow, Discard }
    internal static class Rules
    {
        // Design 9, 2026-11-23. Times use the game's clock.
        internal const int RangeFeet = 30, StorageSeconds = 60, ReleaseSeconds = 18, RoundSeconds = 6;
        internal static int Slots(int level) => Math.Min(5, 1 + level / 5);
        internal static int Limit(int level, Rite rite)
        {
            if (rite == Rite.Gather || rite == Rite.Discard) return Slots(level);
            if (rite == Rite.Blade) return level < 5 ? 0 : level < 11 ? 1 : level < 16 ? 2 : 3;
            return level < 6 ? 1 : level < 11 ? 2 : level < 16 ? 3 : 4;
        }
        internal static int Dice(int level, int applied) => level >= 16 ? 2 * applied : 0;
        internal static int Daily(int wisdom) => Math.Max(1, 3 + wisdom);
        internal static int DC(int level, int wisdom, bool original) => 10 + level / 2 + wisdom + (level >= 7 && original ? 2 : 0);
    }
}
