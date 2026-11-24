namespace DreamThief
{
    // Source assets from the installed blueprints.zip, read 2026-11-24.
    internal static class GameAssets
    {
        internal const string Rogue = "299aa766dee3cbf4790da4efb8c72484";
        internal const string Progression = "b57b2a75a5abcaf47a01cf84672b50e9";
        internal const string TrapFinding = "dbb6b3bffe6db3547b31c3711653838e";
        internal const string DangerSense = "0bcbe9e450b0e7b428f08f66c53c5136";
        internal const string TalentSelection = "c074a5d615200494b8f2a9c845799d93";
        internal const string Desna = "2c0a3b9971327ba4d9d85354d16998c1";
        internal const string KnifeProficiency = "7818ba3db79ac064e88fa14a2478b24b";
        internal const string StandardKnife = "d19662d357d752447a801951b7bec798";
        internal const string Sleeping = "d36e15046cc86c0418c69b8e13f29602";
        internal const string RestlessBuff = "1cf03f5321c4450498b1813f1a72ac82";
        internal const string NightmareBuff = "5a4758c555724c649fd0cbfbb89855bf";
        internal const string LullAuraBuff = "0d874199b71149c3ac55f0f581241dc9";
        internal const string LullAuraArea = "4f2e9162438d4eb1ac1388fd6b61640c";
        internal const string LullAuraEffect = "436ef95d5ec0461184ac0fdd63aa5ca6";
    }

    internal sealed class Talent
    {
        internal readonly string Key, SourceId;
        internal readonly int Level, Cost, SpellLevel;
        internal Talent(string key, string sourceId, int level, int cost, int spellLevel = -1)
        {
            Key = key; SourceId = sourceId; Level = level; Cost = cost; SpellLevel = spellLevel;
        }
    }

    internal static class DreamTalents
    {
        static Talent Spell(string key, string guid, int spellLevel)
        {
            return new Talent(key, guid, DreamRules.SpellTalentLevel(spellLevel),
                DreamRules.SpellCost(spellLevel), spellLevel);
        }

        internal static readonly Talent[] All = {
            Spell("Lullaby", "877297946a6b70744a188eb15dd2daab", 0),
            Spell("Sleep", "bb7ecad2d3d2c8247a38f44855c99061", 1),
            Spell("DeepSlumber", "7658b74f626c56a49939d9c20580885e", 3),
            Spell("Haze", "40ec382849b60504d88946df46a10f2d", 3),
            Spell("Cloak", "7f71a70d822af94458dc1a235507e972", 6),
            Spell("Hypnotism", "88367310478c10b47903463c5d0152b0", 2),
            Spell("Rainbow", "4b8265132f9c8174f87ce7fa6d0fe47b", 4),
            Spell("Killer", "6717dbaef00c0eb4897a1c908a75dfe5", 4),
            Spell("Web", "12fb4a4c22549c74d949e2916a2f0b6a", 5),
            Spell("Putrefaction", "1f2e6019ece86d64baa5effa15e81ecc", 6),
            Spell("Weird", "870af83be6572594d84d276d7fc583e0", 9),
            new Talent("Slumber", "630ea63a63457ff4f9de059c578c930a", 2, 1),
            new Talent("Restless", "a69fb167bb41c6f45a19c81ed4e3c0d9", 10, 1),
            new Talent("Nightmare", "b16935b1b1f24c698da568e29450a9f2", 16, 3),
            new Talent("Aura", "0d874199b71149c3ac55f0f581241dc9", 8, 0),
        };
    }
}
