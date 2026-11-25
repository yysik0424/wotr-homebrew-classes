namespace Spiritsteel
{
    internal static class Rules
    {
        public const int TripLevel = 6;
        public static bool FlexibleSlot(int level) { return level >= 4 && level <= 20 && level != TripLevel && level % 2 == 0; }
    }
}
