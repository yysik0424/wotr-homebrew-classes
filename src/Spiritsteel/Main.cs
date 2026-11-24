using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using WotrClassMod.Common;




namespace Spiritsteel
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry entry)
        {
            ModContext.Init(entry, "Spiritsteel", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, Guids.Barbarian);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            return true;
        }
    }
}
