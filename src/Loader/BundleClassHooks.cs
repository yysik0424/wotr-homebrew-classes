using System;
using System.Reflection;

namespace HomebrewClasses.Loader
{
    internal sealed class BundleClassHooks
    {
        internal readonly Action Enable, Build, InvalidateLanguage;
        internal readonly Func<string, string> Resolve;

        internal BundleClassHooks(Assembly assembly, ModuleInfo module)
        {
            var type = assembly.GetType(module.StatusSource == "packwarden"
                ? "PackWarden.BundleHooks" : "WotrClassMod.Common.BundleHooks", true);
            Enable = (Action)Delegate.CreateDelegate(typeof(Action), type, "Enable");
            InvalidateLanguage = (Action)Delegate.CreateDelegate(typeof(Action), type, "InvalidateLanguage");
            Resolve = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), type, "Resolve");
            // Spellfist builds after Brawler's postfix at low priority. ChimeraMonk builds in LoadAllJson.
            if (module.Id != "Spellfist" && module.Id != "ChimeraMonk")
                Build = (Action)Delegate.CreateDelegate(typeof(Action), type, "Build");
        }
    }
}
