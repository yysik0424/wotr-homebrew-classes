using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UMM = UnityModManagerNet.UnityModManager;

namespace HomebrewClasses.Loader
{
    internal enum LoadState { NotLoaded, Loaded, LoadFailed }

    internal sealed class ClassHost
    {
        internal readonly ModuleInfo Module;
        internal readonly ClassDecision Decision;
        readonly AssemblyNameResolver resolver;
        readonly Action<string> logError;
        readonly HashSet<string> tickErrors = new HashSet<string>(StringComparer.Ordinal);
        readonly string decisionKo, decisionEn;
        UMM.ModEntry proxy;
        ClassStatus status;
        string failure;

        internal LoadState State { get; private set; }
        internal BundleClassHooks Hooks { get; private set; }

        internal ClassHost(ModuleInfo module, ClassDecision decision, string ko, string en,
                           AssemblyNameResolver resolver, Action<string> logError)
        {
            Module = module; Decision = decision; decisionKo = ko; decisionEn = en;
            this.resolver = resolver; this.logError = logError;
        }

        internal LoadState Load(string bundleDir)
        {
            if (State != LoadState.NotLoaded || Decision != ClassDecision.Load)
                throw new InvalidOperationException("Class cannot be loaded in this state: " + Module.Id);
            bool invoked = false;
            try
            {
                string path = Path.Combine(bundleDir, "classes", Module.Id + ".dll");
                var assembly = Assembly.Load(File.ReadAllBytes(path));
                resolver.Register(Module.Id, assembly);
                status = new ClassStatus(assembly, Module.StatusSource);
                var hooks = new BundleClassHooks(assembly, Module);
                var main = assembly.GetType(Module.Id + ".Main", true);
                var load = main.GetMethod("Load", BindingFlags.Public | BindingFlags.Static, null,
                                          new[] { typeof(UMM.ModEntry) }, null);
                if (load == null || load.ReturnType != typeof(bool)) throw new MissingMethodException(main.FullName, "Load");
                var info = new UMM.ModInfo { Id = Module.Id, DisplayName = Module.Display, Version = Module.Version,
                    AssemblyName = Module.Id + ".dll", EntryMethod = Module.Id + ".Main.Load" };
                proxy = new UMM.ModEntry(info, bundleDir);
                invoked = true;
                hooks.Enable();
                if (!(bool)load.Invoke(null, new object[] { proxy })) throw new InvalidOperationException("Main.Load returned false");
                Hooks = hooks;
                State = LoadState.Loaded;
            }
            catch (Exception e)
            {
                State = LoadState.LoadFailed;
                if (proxy != null) proxy.OnUpdate = null;
                var cause = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                failure = cause.GetType().Name + ": " + cause.Message;
                logError(Module.Id + ": " + cause);
                if (invoked)
                {
                    try { new Harmony(Module.Id).UnpatchAll(Module.Id); }
                    catch (Exception cleanup) { logError(Module.Id + " patch cleanup: " + cleanup); }
                }
            }
            return State;
        }

        internal void Tick(float dt)
        {
            if (State != LoadState.Loaded || proxy.OnUpdate == null) return;
            try
            {
                var build = status.Read(out _, out _);
                if (build != BuildState.Skipped && build != BuildState.Failed) proxy.OnUpdate(proxy, dt);
            }
            catch (Exception e)
            {
                string key = e.GetType().FullName + ": " + e.Message;
                if (tickErrors.Add(key)) logError(Module.Id + ": " + e);
            }
        }

        internal string Describe()
        {
            switch (Decision)
            {
                case ClassDecision.Disabled: return LoaderText.Off;
                case ClassDecision.BlockedDuplicate: return LoaderText.Blocked;
                case ClassDecision.SkippedDependency: return LoaderText.Skipped(decisionKo, decisionEn);
            }
            if (State == LoadState.LoadFailed) return LoaderText.Failed(failure);
            if (State == LoadState.NotLoaded) return LoaderText.Pending;
            return status.Describe();
        }
    }
}
