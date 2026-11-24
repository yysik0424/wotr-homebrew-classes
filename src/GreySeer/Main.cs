using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityModManager = UnityModManagerNet.UnityModManager;
using WotrClassMod.Common;




namespace GreySeer
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "GreySeer", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, Guids.Arcanist);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            return true;
        }
    }
}
