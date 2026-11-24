using System.Reflection;
using HarmonyLib;
using UnityModManager = UnityModManagerNet.UnityModManager;
using WotrClassMod.Common;



namespace HellRider
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "HellRider", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, Ids.CavalierClass);

            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            return true;
        }
    }
}
