using System;
using System.Text;
using System.Security.Cryptography;

namespace Deathdealer
{
    internal static class Rules
    {
        // Native Slayer progression and spell blueprints, installed archive 2026-11-24.
        internal const string Slayer = "c75e0971973957d4dbad24bc7957e4fb", Studied = "45548967b714e254aa83f23354f174b0";
        internal static readonly int[] ExchangeLevels = { 2, 6, 10, 14, 19 };
        internal static readonly string[] Exchanges = {
            "04430ad24988baa4daa0bcd4f1c7d118", "43d1b15873e926848be2abf0ea3ad9a8", "913b9cf25c9536949b43a2651b7ffb66",
            "385260ca07d5f1b4e907ba22a02944fc", "25e009b7e53f86141adee3a1213af5af"
        };
        internal static readonly string[] Spells = {
            "2c38da66e5a599347ac95b3294acbe00", "3e4ab69ada402d145a5e0ad3ad4b8564", "486eaff58293f6441a5c2759c4872f98",
            "c66e86905f7606c4eaa5c774f0357b2b", "07b608fab304f894880898dc0764e6e5", "e15e5e7045fda2244b98c8f010adfe31",
            "da1b292d91ba37948893cdbe9ea89e28", "7ef49f184922063499b8f1346fb7f521", "1f01a098d737ec6419aedc4e7ad61fdd"
        };
        internal const int GenieTier = 5, ResearchBonus = 2;
        internal static int UnlockLevel(int tier)
        {
            if (tier < 1 || tier > Spells.Length) throw new ArgumentOutOfRangeException(nameof(tier));
            return tier * 2 - 1;
        }
        internal static string Id(string key)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("WotrClassMod.Deathdealer.v1/" + key)), 0, 16).Replace("-", "").ToLowerInvariant();
        }
    }
}
