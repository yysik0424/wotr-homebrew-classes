using System;
using System.Collections.Generic;
using System.Threading;

namespace HomebrewClasses.Loader
{
    internal sealed class BundleRoutes
    {
        sealed class Node
        {
            internal readonly Dictionary<char, Node> Children = new Dictionary<char, Node>();
            internal string Id;
            internal volatile BundleClassHooks Hooks;
            internal int BuildStarted;
        }

        readonly Node root = new Node();
        readonly List<Node> ordered = new List<Node>();
        readonly Dictionary<string, Node> byId = new Dictionary<string, Node>(StringComparer.Ordinal);
        readonly Action<string> logError;

        internal BundleRoutes(IEnumerable<string> ids, Action<string> logError)
        {
            this.logError = logError;
            foreach (string id in ids)
            {
                var node = root;
                foreach (char c in id + ".")
                {
                    if (!node.Children.TryGetValue(c, out var next))
                        node.Children.Add(c, next = new Node());
                    node = next;
                }
                node.Id = id;
                ordered.Add(node);
                byId.Add(id, node);
            }
        }

        internal void Activate(string id, BundleClassHooks hooks)
        {
            if (hooks == null) throw new ArgumentNullException(nameof(hooks));
            var node = byId[id];
            if (node.Hooks != null) throw new InvalidOperationException("Class hooks already active: " + id);
            node.Hooks = hooks;
        }

        // Match the exact, case-sensitive class ID and dot without splitting keys or visiting other classes.
        // The class resolves the complete key against its live table, including entries added during its build.
        internal bool TryResolve(string key, ref string result)
        {
            var node = root;
            if (key == null) return false;
            for (int i = 0; i < key.Length; i++)
            {
                if (!node.Children.TryGetValue(key[i], out node)) return false;
                if (node.Id == null) continue;
                var hooks = node.Hooks;
                if (hooks == null) return false;
                result = hooks.Resolve(key);
                return true;
            }
            return false;
        }

        internal void Build()
        {
            foreach (var node in ordered)
            {
                var hooks = node.Hooks;
                if (hooks == null || hooks.Build == null || Interlocked.Exchange(ref node.BuildStarted, 1) != 0) continue;
                try { hooks.Build(); }
                catch (Exception e) { logError(node.Id + " blueprint initialization: " + e); }
            }
        }

        internal void InvalidateLanguage()
        {
            foreach (var node in ordered)
            {
                var hooks = node.Hooks;
                if (hooks == null) continue;
                try { hooks.InvalidateLanguage(); }
                catch (Exception e) { logError(node.Id + " language cache: " + e); }
            }
        }
    }
}
