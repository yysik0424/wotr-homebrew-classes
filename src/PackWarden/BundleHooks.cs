using System;

namespace PackWarden
{
    // 이 타입은 각 직업 DLL에 들어간다. 통합 적재기가 Main.Load 전에 해당 DLL만 전환한다.
    public static class BundleHooks
    {
        internal static bool Enabled { get; private set; }

        public static void Enable()
        {
            if (Main.Entry != null) throw new InvalidOperationException("Bundle hooks must be enabled before Main.Load.");
            Enabled = true;
            Strings.PrepareBundleKeys();
        }

        public static void Build() { BlueprintsCacheInitPatch.Postfix(); }
        public static void InvalidateLanguage() { Strings.InvalidateLanguage(); }
        public static string Resolve(string fullKey) { return Strings.Resolve(fullKey); }
    }
}
