using System;
using System.Security.Cryptography;
using System.Text;

namespace Falconer
{
    internal static class Rules
    {
        internal const string Ranger = "cda0615668a6df14eb36ba19ee881af6";
        internal const string Eagle = "406c1e1af5400ac4881e330502ccbd9e";
        internal const string Endurance = "54ee847996c25cd4ba8773d7b8555174";
        internal const string ImprovedEvasion = "ce96af454a6137d47b9c6a1e02e66803";
        internal const int ImprovedEvasionLevel = 16;
        internal const string Camouflage = "ff1b5aa8dcc7d7d4d9aa85e1cb3f9e88";
        internal const string Terrain = "a6ea422d7308c0d428a541562faedefd";
        internal const string TerrainRank = "c4de22ecac494c91b3d2f077ec162bbd";
        internal const string TrueStrike = "2c38da66e5a599347ac95b3294acbe00";
        internal const string Charge = "c78506dd0e14f7c45a599990e4e65038";
        internal const int DiveBonus = 2, ShotBonus = 2, DrivingBonus = 2, EyeBonus = 20;
        internal static int Strikes(int level) => level >= 15 ? 3 : level >= 8 ? 2 : 1;
        internal static int Die(int level) => 2 + Strikes(level) * 2;
        internal static int MissingBonus(int requested, int existing) => Math.Max(0, requested - existing);
        internal static int EyeRounds(int wisdom) => Math.Max(1, wisdom);
        internal static float Scale(int level) => level <= 10 ? 1f + Math.Max(0, level - 1) * .3f / 9 : 1.3f + Math.Min(10, level - 10) * .03f;
        internal static string Id(string key)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("WotrClassMod.Falconer.v1/" + key)), 0, 16).Replace("-", "").ToLowerInvariant();
        }
    }
}
