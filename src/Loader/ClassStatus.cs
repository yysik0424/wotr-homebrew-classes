using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HomebrewClasses.Loader
{
    internal enum BuildState { Pending, Built, Failed, Skipped }

    internal sealed class ClassStatus
    {
        readonly Func<bool> attempted, failed;
        readonly Func<string> skipKo, skipEn;
        readonly Func<Tuple<bool?, string, string>> readElements;

        internal ClassStatus(Assembly assembly, string source)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            string typeName;
            switch (source)
            {
                case "common": typeName = "WotrClassMod.Common.ModContext"; break;
                case "packwarden": typeName = "PackWarden.Main"; break;
                default: throw new ArgumentException("Unknown build status source: " + source);
            }
            var type = assembly.GetType(typeName, true);
            attempted = Bind<bool>(type, "BuildAttempted");
            failed = Bind<bool>(type, "BuildFailed");
            skipKo = Bind<string>(type, "BuildSkipReasonKo");
            skipEn = Bind<string>(type, "BuildSkipReasonEn");
            if (assembly.GetName().Name == "BlastMagus")
            {
                var stage = assembly.GetType("BlastMagus.KeeStage", true);
                readElements = Bind<Tuple<bool?, string, string>>(stage, "ElementStatus");
            }
        }

        // Bind once so forwarding an update only reads the class's flags.
        static Func<T> Bind<T>(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            var property = type.GetProperty(name, flags);
            if (property != null && property.PropertyType == typeof(T))
                return (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), property.GetGetMethod());
            var field = type.GetField(name, flags);
            if (field == null || field.FieldType != typeof(T)) throw new MissingMemberException(type.FullName, name);
            return Expression.Lambda<Func<T>>(Expression.Field(null, field)).Compile();
        }

        internal static BuildState FromFlags(bool attempted, bool failed, bool skipped)
        {
            if (skipped && failed) throw new InvalidOperationException("Build is both skipped and failed.");
            if (skipped) return BuildState.Skipped;
            if (!attempted) return BuildState.Pending;
            return failed ? BuildState.Failed : BuildState.Built;
        }

        internal BuildState Read(out string ko, out string en)
        {
            ko = skipKo(); en = skipEn();
            if ((ko == null) != (en == null)) throw new InvalidOperationException("Incomplete build status translation.");
            var result = FromFlags(attempted(), failed(), ko != null);
            if (result != BuildState.Built || readElements == null) return result;
            var elements = readElements();
            bool? ready = elements.Item1;
            ko = elements.Item2;
            en = elements.Item3;
            if (!ready.HasValue) return BuildState.Pending;
            return ready.Value ? BuildState.Built : BuildState.Failed;
        }

        internal string Describe()
        {
            var state = Read(out var ko, out var en);
            string detail = ko == null ? null : ko + " / " + en;
            switch (state)
            {
                case BuildState.Pending: return detail == null ? LoaderText.Pending : LoaderText.Pending + " · " + detail;
                case BuildState.Built: return detail == null ? LoaderText.Loaded : LoaderText.Loaded + " · " + detail;
                case BuildState.Skipped: return LoaderText.Skipped(ko, en);
                case BuildState.Failed: return LoaderText.Failed(detail ?? "블루프린트 생성 오류 / Blueprint build error");
                default: throw new InvalidOperationException("Unknown build state.");
            }
        }
    }
}
