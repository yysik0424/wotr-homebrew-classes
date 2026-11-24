using System;
using System.Security.Cryptography;
using System.Text;

namespace Spiritsteel
{
    internal static class BoundRules
    {
        internal static readonly string[] Enhancements = {
            "d42fc23b92c640846ac137dc26e000d4", "eb2faccc4c9487d43b3575d7e77ff3f5",
            "80bb8a737579e35498177e1e3c75899b", "783d7d496da6ac44f9511011fc5f1979",
            "bdba267e951851449af552aa9f9e3992"
        };

        internal static int EnhancementRank(int level)
        {
            return level < 1 ? 0 : Math.Min(5, 1 + (level - 1) / 4);
        }

        internal static string ShieldSource(string category)
        {
            switch (category)
            {
                case "SpikedLightShield": return "a85d51d0fb905f940b951eec60388bac";
                case "SpikedHeavyShield": return "f4cef3ba1a15b0f4fa7fd66b602ff32b";
                default: return null;
            }
        }

        internal static string Id(string kind, string category)
        {
            using (var md5 = MD5.Create())
                return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes("Spiritsteel.Bond." + kind + "." + category))).ToString("N");
        }
    }
}
