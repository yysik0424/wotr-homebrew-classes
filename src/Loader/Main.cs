using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UMM = UnityModManagerNet.UnityModManager;

namespace HomebrewClasses.Loader
{
    public static class Main
    {
        public static bool Load(UMM.ModEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            var settings = UMM.ModSettings.Load<LoaderSettings>(entry);
            if (settings == null)
            {
                entry.Logger.Error(LoaderText.InvalidSettings);
                return false;
            }
            var ids = ModuleLedger.Modules.Select(m => m.Id).ToArray();
            var routes = BundlePatches.Install(ids, message => entry.Logger.Error(message));
            var resolver = new AssemblyNameResolver(ids);
            resolver.Install();
            var classes = new List<ClassHost>(ModuleLedger.Modules.Length);
            bool hasDuplicate = false;
            foreach (var module in ModuleLedger.Modules)
            {
                bool duplicate = UMM.FindMod(module.Id) != null;
                hasDuplicate |= duplicate;
                var decision = LoaderPlan.Decide(module, settings.IsEnabled(module.Id), Probe, duplicate, out var ko, out var en);
                var host = new ClassHost(module, decision, ko, en, resolver, message => entry.Logger.Error(message));
                if (decision == ClassDecision.Load && host.Load(entry.Path) == LoadState.Loaded)
                    routes.Activate(module.Id, host.Hooks);
                classes.Add(host);
                entry.Logger.Log(module.Id + " — " + host.Describe());
            }
            entry.OnUpdate = (e, dt) => { foreach (var host in classes) host.Tick(dt); };
            bool hasModMenu = UMM.FindMod("ModMenu") != null;
            entry.OnGUI = e =>
            {
                if (hasDuplicate) GUILayout.Label(LoaderText.DuplicateWarning);
                GUILayout.Label(LoaderText.Restart);
                if (hasModMenu) GUILayout.Label(LoaderText.LegacySave);
                foreach (var host in classes)
                {
                    string id = host.Module.Id;
                    bool before = settings.IsEnabled(id);
                    GUILayout.BeginHorizontal();
                    bool after = GUILayout.Toggle(before, host.Module.Display);
                    if (after != before) settings.SetEnabled(id, after);
                    GUILayout.Label(host.Describe());
                    GUILayout.EndHorizontal();
                }
            };
            entry.OnSaveGUI = e => settings.Save(e);
            return true;
        }

        static ModProbe Probe(string id)
        {
            var entry = UMM.FindMod(id);
            return entry == null ? default(ModProbe) : new ModProbe
                { Found = true, Enabled = entry.Enabled, Active = entry.Active, HasLoadError = entry.ErrorOnLoading };
        }
    }
}
