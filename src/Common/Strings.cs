using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Localization;

namespace WotrClassMod.Common
{
    // 모드 문자열(한/영)을 LocalizedString 키로 내보내고, GetText 조회 때 돌려준다(LocalizationPatches.cs).
    // 키 접두 = ModContext.StringPrefix ("<모드 id>."). 표는 모드가 Configure 로 넘긴다(키는 접두 없이).
    public static class Strings
    {
        static IReadOnlyDictionary<string, (string ko, string en)> table;
        static string koreanProbeClassGuid;

        // Main.Load 에서 ModContext.Init 다음에 한 번. 두 번 부르거나 빈 값이면 예외.
        public static void Configure(IReadOnlyDictionary<string, (string ko, string en)> modTable, string probeClassGuid)
        {
            if (table != null) throw new InvalidOperationException(ModContext.Id + ": Strings.Configure called twice");
            if (modTable == null) throw new ArgumentNullException("modTable");
            if (modTable.Count == 0) throw new ArgumentException(ModContext.Id + ": Strings.Configure: empty string table", "modTable");
            if (string.IsNullOrEmpty(probeClassGuid)) throw new ArgumentException(ModContext.Id + ": Strings.Configure: Korean probe class GUID is empty", "probeClassGuid");
            // 접두 원천 확인: Init 전이면 여기서 ModContext 가 예외를 던진다.
            string unused = ModContext.StringPrefix;
            table = modTable;
            koreanProbeClassGuid = probeClassGuid;
            if (BundleHooks.Enabled) PrepareBundleKeys();
        }

        static IReadOnlyDictionary<string, (string ko, string en)> Table
        {
            get
            {
                if (table == null) throw new InvalidOperationException((ModContext.IsInitialized ? ModContext.Id : "(ModContext not initialized)") + ": Strings used before Strings.Configure");
                return table;
            }
        }

        // 언어 판정. 이 게임(2.7.0x) 의 Locale 열거형에는 koKR 이 없다(어셈블리 확인: enGB ruRU deDE frFR zhCN Sound esES ptBR itIT).
        // 한국어 모드들은 영어 팩 문자열을 한국어로 덮어쓴다. 그래서 (1) 열거형 이름이 koKR 이면 한국어(미래 대비),
        // (2) 아니면 Configure 로 받은 본편 클래스(헌터 등)의 이름에 한글이 있는지로 현재 팩이 한국어인지 본다. 특정 모드 존재에는 의존하지 않는다.
        // 조회 시점에 판정한다. 한국어(true)만 캐시한다: 한국어 모드가 영어 팩을 나중에 덮어쓸 수 있으므로 false 를 캐시하면
        // 그 뒤로 영원히 영어가 된다(최종 리뷰 M6). 로케일이 바뀌면 InvalidateLanguage 로 비운다.
        static bool koreanCached;

        public static void InvalidateLanguage() { koreanCached = false; }

        static bool IsKorean()
        {
            if (koreanCached) return true;
            bool ko = DetectKorean();
            if (ko) koreanCached = true;
            return ko;
        }

        static bool DetectKorean()
        {
            if (LocalizationManager.CurrentLocale.ToString() == "koKR") return true;
            // 본편 클래스 키는 우리 접두(ModContext.StringPrefix)가 아니므로 GetText 접두 패치가 통과시킨다(재귀 없음).
            var probe = BlueprintUtil.Get<BlueprintCharacterClass>(koreanProbeClassGuid);
            string name = probe.Name;
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException(ModContext.Id + ": cannot detect UI language, probe class " + koreanProbeClassGuid + " name is empty");
            foreach (char c in name)
                if (c >= '가' && c <= '힣') return true;
            return false;
        }

        // 번들에서는 이미 접두가 확인된 전체 키를 재사용한다. 번역 본문은 원래 표에서 매번 읽어 갱신을 반영한다.
        static ConcurrentDictionary<string, string> bundleKeys;

        internal static void PrepareBundleKeys()
        {
            var keys = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in Table.Keys) keys.TryAdd(ModContext.StringPrefix + key, key);
            bundleKeys = keys;
        }

        public static bool IsOurKey(string key)
        {
            if (table == null) throw new InvalidOperationException((ModContext.IsInitialized ? ModContext.Id : "(ModContext not initialized)") + ": Strings.IsOurKey before Strings.Configure");
            return key != null && key.StartsWith(ModContext.StringPrefix, StringComparison.Ordinal);
        }

        // GetText 접두 패치가 부른다. 없는 키는 예외(빈 문자열로 조용히 넘기지 않는다).
        public static string Resolve(string fullKey)
        {
            string key;
            if (bundleKeys != null && fullKey != null && bundleKeys.TryGetValue(fullKey, out key))
                return Lookup(key, fullKey);
            if (!IsOurKey(fullKey)) throw new ArgumentException(ModContext.Id + ": Strings.Resolve on a key without our prefix '" + fullKey + "'", "fullKey");
            key = fullKey.Substring(ModContext.StringPrefix.Length);
            string value = Lookup(key, fullKey);
            if (bundleKeys != null) bundleKeys.TryAdd(fullKey, key);
            return value;
        }

        static string Lookup(string key, string fullKey)
        {
            (string ko, string en) entry;
            if (!Table.TryGetValue(key, out entry))
                throw new KeyNotFoundException(ModContext.Id + ": unknown string key '" + fullKey + "'");
            return IsKorean() ? entry.ko : entry.en;
        }

        public static LocalizedString L(string key)
        {
            if (!Table.ContainsKey(key))
                throw new KeyNotFoundException(ModContext.Id + ": missing string key '" + key + "'");
            string fullKey = ModContext.StringPrefix + key;
            if (bundleKeys != null) bundleKeys.TryAdd(fullKey, key);
            return new LocalizedString { Key = fullKey };
        }
    }
}
