using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;

namespace HomebrewClasses.Loader
{
    internal static class BundlePatches
    {
        internal const string Owner = "HomebrewClasses";
        static volatile BundleRoutes active;

        internal static BundleRoutes Install(IEnumerable<string> ids, Action<string> logError)
        {
            if (active != null) throw new InvalidOperationException("Bundle hooks already installed.");
            var routes = new BundleRoutes(ids, logError);
            var harmony = new Harmony(Owner);
            try
            {
                foreach (var type in new[] { typeof(BlueprintInit), typeof(LocaleChanged), typeof(GetText) })
                {
                    var patched = harmony.CreateClassProcessor(type).Patch();
                    if (patched == null || patched.Count != 1)
                        throw new InvalidOperationException("Expected a single patch target for " + type.Name);
                }
                active = routes;
                return routes;
            }
            catch
            {
                harmony.UnpatchAll(Owner);
                throw;
            }
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init), new Type[0])]
        static class BlueprintInit
        {
            [HarmonyPostfix]
            static void Postfix() { active?.Build(); }
        }

        [HarmonyPatch(typeof(LocalizationManager), "OnLocaleChanged", new Type[0])]
        static class LocaleChanged
        {
            [HarmonyPostfix]
            static void Postfix() { active?.InvalidateLanguage(); }
        }

        [HarmonyPatch(typeof(LocalizationPack), nameof(LocalizationPack.GetText), typeof(string), typeof(bool))]
        static class GetText
        {
            [HarmonyPrefix]
            static bool Prefix(string key, ref string __result)
            {
                var routes = active;
                return routes == null || !routes.TryResolve(key, ref __result);
            }
        }
    }
}
