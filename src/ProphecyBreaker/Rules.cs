using System;
using System.Security.Cryptography;
using System.Text;

namespace ProphecyBreaker
{
    internal static class Rules
    {
        // Four inscriptions and two preparations/shared casts per tier, per the confirmed design.
        internal const int Capacity = 4, Slots = 2, HighestTier = 9;
        // Native ArcanistSpellSlots unlocks the first tier at 1 and the remaining tiers at 4, 6, ..., 18.
        internal static int MaxTier(int oracleLevel) => oracleLevel < 1 ? 0 : Math.Min(HighestTier, Math.Max(1, oracleLevel / 2));
        internal static string Id(string key)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("WotrClassMod.ProphecyBreaker.v1/" + key)), 0, 16).Replace("-", "").ToLowerInvariant();
        }
    }
}
