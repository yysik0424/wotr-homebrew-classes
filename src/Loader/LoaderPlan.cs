using System;

namespace HomebrewClasses.Loader
{
    internal sealed class ModuleInfo
    {
        internal readonly string Id, Display, Version, StatusSource;
        internal readonly Dependency[] Requires;

        internal ModuleInfo(string id, string display, string version, string statusSource, Dependency[] requires)
        {
            Id = id; Display = display; Version = version; StatusSource = statusSource;
            Requires = requires ?? throw new ArgumentNullException(nameof(requires));
        }
    }

    internal sealed class Dependency
    {
        internal readonly string Id, Display;
        internal Dependency(string id, string display) { Id = id; Display = display; }
    }

    internal struct ModProbe
    {
        internal bool Found, Enabled, Active, HasLoadError;
    }

    internal enum ClassDecision { Load, Disabled, SkippedDependency, BlockedDuplicate }

    internal static class LoaderPlan
    {
        internal static ClassDecision Decide(ModuleInfo module, bool enabled, Func<string, ModProbe> probe,
            bool standaloneCopyInstalled, out string ko, out string en)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (probe == null) throw new ArgumentNullException(nameof(probe));
            ko = en = null;
            if (!enabled) return ClassDecision.Disabled;
            if (standaloneCopyInstalled) return ClassDecision.BlockedDuplicate;
            foreach (var dependency in module.Requires)
            {
                var state = probe(dependency.Id);
                if (!state.Found) { ko = "미설치"; en = "not installed"; }
                else if (!state.Enabled) { ko = "꺼짐"; en = "disabled"; }
                else if (state.HasLoadError) { ko = "로드 오류"; en = "failed to load"; }
                else if (!state.Active) { ko = "아직 로드되지 않음"; en = "not loaded yet"; }
                else continue;
                ko = dependency.Display + " (" + ko + ")";
                en = dependency.Display + " (" + en + ")";
                return ClassDecision.SkippedDependency;
            }
            return ClassDecision.Load;
        }
    }
}
