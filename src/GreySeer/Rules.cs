using System;

namespace GreySeer
{
    // One designation can purchase protection once and authorize at most one attack.
    internal sealed class StrikePayment
    {
        readonly bool guarded;
        bool protectedCast;
        bool finished;

        internal StrikePayment(bool guarded) { this.guarded = guarded; }

        internal bool Protect(int slot, Func<int, bool> spend)
        {
            if (!guarded || finished) return false;
            if (protectedCast) return true;
            protectedCast = spend(Rules.ProtectedCost(slot));
            if (!protectedCast) finished = true;
            return protectedCast;
        }

        internal bool Finish(bool canStrike, Func<int, bool> spend)
        {
            if (finished) return false;
            finished = true;
            return canStrike && (protectedCast || spend(Rules.StrikeCost));
        }
    }

    // Owner-approved progression and reservoir bands, 2026-11-23.
    internal static class Rules
    {
        internal static readonly int[] ExchangeLevels = { 1, 5, 9, 13, 17 };
        internal const int StrikeCost = 1;

        internal static int ProtectedCost(int slotLevel)
        {
            if (slotLevel < 0 || slotLevel > 9)
                throw new ArgumentOutOfRangeException("slotLevel", slotLevel, "Grey Seer expects an arcanist slot from 0 to 9.");
            return slotLevel < 4 ? StrikeCost : slotLevel < 7 ? 2 : 3;
        }
    }
}
