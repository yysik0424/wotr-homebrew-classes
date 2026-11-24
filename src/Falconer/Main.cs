using System.Reflection;
using HarmonyLib;
using UnityModManager = UnityModManagerNet.UnityModManager;
using WotrClassMod.Common;



namespace Falconer
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "Falconer", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, Rules.Ranger);
            modEntry.OnUpdate += (entry, dt) => Flight.Tick(dt);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            return true;
        }
    }
}
