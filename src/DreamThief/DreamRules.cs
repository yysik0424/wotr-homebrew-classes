using System;

namespace DreamThief
{
    // Owner-approved Dream Thief design, 2026-11-15, sections 5, 6 and 6-2.
    internal static class DreamRules
    {
        internal const int ThrowFeet = 30;
        internal const int ReachLevel = 9;
        internal const int ExtendedFeet = 50;
        internal const int ShroudLevel = 3;
        internal const int DreamwalkLevel = 6;
        internal const int BurstLevel = 12;
        internal const int MoveLevel = 15;
        internal const int ReserveLevel = 18;
        internal const int ReserveBonus = 3;
        internal const int ImmunityRounds = 10;
        internal const int ShroudRounds = 10;
        internal const int SleepDieSides = 4;
        internal const int AuraFeet = 30;
        internal const int NightmareFeet = 10;

        internal static int Starlight(int rogueLevel, int dexterityModifier, int charismaModifier)
        {
            int basePool = Math.Max(1, rogueLevel / 2 + dexterityModifier + charismaModifier);
            return basePool + (rogueLevel >= ReserveLevel ? ReserveBonus : 0);
        }

        internal static int DifficultyClass(int rogueLevel, int charismaModifier)
        {
            return 10 + rogueLevel * 3 / 4 + charismaModifier;
        }

        internal static int SpellTalentLevel(int spellLevel)
        {
            if (spellLevel < 0 || spellLevel > 9) throw new ArgumentOutOfRangeException("spellLevel");
            return Math.Max(2, spellLevel * 2);
        }

        internal static int SpellCost(int spellLevel)
        {
            if (spellLevel < 0 || spellLevel > 9) throw new ArgumentOutOfRangeException("spellLevel");
            return spellLevel <= 3 ? 1 : spellLevel <= 6 ? 2 : 3;
        }

        internal static int SleepHitDice(int rogueLevel, bool deep) { return rogueLevel + (deep ? 4 : 0); }
        internal static int WalkFeet(int rogueLevel) { return rogueLevel >= ReachLevel ? ExtendedFeet : ThrowFeet; }
        internal static int AuraPenalty(int rogueLevel) { return rogueLevel >= BurstLevel ? -4 : -2; }
        internal static int BurstDice(int level, int multiplier, bool confirmedCritical)
        {
            return level >= BurstLevel && confirmedCritical ? Math.Max(0, multiplier - 1) : 0;
        }
    }
}
