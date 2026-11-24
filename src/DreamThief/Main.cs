using System.Reflection;
using HarmonyLib;
using UnityModManager = UnityModManagerNet.UnityModManager;
using WotrClassMod.Common;




namespace DreamThief
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "DreamThief", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, GameAssets.Rogue);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            return true;
        }
    }
}
