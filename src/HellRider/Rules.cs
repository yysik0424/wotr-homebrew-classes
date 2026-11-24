using System;
using System.Security.Cryptography;
using System.Text;

namespace HellRider
{
    internal static class Rules
    {
        internal static int Devils(int level) => level >= 20 ? 3 : level >= 11 ? 2 : 1;
        internal static int DevilDice(int level) => level / 3;
        internal static int BreathDice(int level) => level / 2;
        internal static int RayUses(int charisma) => 1 + Math.Max(0, charisma) / 2;
        internal static int SaveDC(int level, int modifier) => 10 + level / 2 + modifier;
        internal static string Id(string key)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("WotrClassMod.HellRider.v1/" + key)), 0, 16).Replace("-", "").ToLowerInvariant();
        }
    }
}
