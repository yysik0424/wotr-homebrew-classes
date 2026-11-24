using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityModManager = UnityModManagerNet.UnityModManager;
using WotrClassMod.Common;




namespace SorrowInquisitor
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "SorrowInquisitor", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, Guids.Inquisitor);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            return true;
        }
    }
}
