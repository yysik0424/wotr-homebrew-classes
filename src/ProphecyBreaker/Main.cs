using System.Reflection;
using HarmonyLib;
using UnityModManager = UnityModManagerNet.UnityModManager;
using WotrClassMod.Common;



namespace ProphecyBreaker
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "ProphecyBreaker", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, Ids.OracleClass);

            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            return true;
        }
    }
}
