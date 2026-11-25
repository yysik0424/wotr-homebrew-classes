namespace ShadowSorcerer
{
    public static class ShadowSlotCost
    {
        public static int WithShadow(int ordinaryCost)
        {
            return ordinaryCost > 0 ? checked(ordinaryCost + ShadowRules.ExtraSlotWhileActive) : ordinaryCost;
        }
    }
}
