using System;
using System.Collections.Generic;
using System.Reflection;

namespace HomebrewClasses.Loader
{
    internal sealed class AssemblyNameResolver
    {
        readonly HashSet<string> allowed;
        readonly Dictionary<string, Assembly> loaded = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        internal AssemblyNameResolver(IEnumerable<string> ids)
        {
            allowed = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        }

        internal void Install() { AppDomain.CurrentDomain.AssemblyResolve += Resolve; }

        internal void Register(string id, Assembly assembly)
        {
            if (!allowed.Contains(id) || !string.Equals(id, assembly.GetName().Name, StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected class assembly: " + id + " / " + assembly.FullName);
            lock (loaded) loaded.Add(id, assembly);
        }

        internal Assembly Resolve(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            if (!allowed.Contains(name)) return null;
            lock (loaded) return loaded.TryGetValue(name, out var value) ? value : null;
        }
    }
}
