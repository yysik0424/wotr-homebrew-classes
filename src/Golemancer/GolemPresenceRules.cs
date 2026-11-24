namespace Golemancer
{
    public static class GolemPresenceRules
    {
        public static bool KeepLegacyPet(bool combat, bool mounted, bool fusion, bool overdrive, bool needsRepair)
        {
            return combat || mounted || fusion || overdrive || needsRepair;
        }

        public static bool AllowsCombat(bool call, bool combat)
        {
            return call || !combat;
        }
    }
}
