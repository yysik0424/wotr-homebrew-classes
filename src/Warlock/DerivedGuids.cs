using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Warlock
{
    // [순수] 범주별 파생 GUID(계획 Global Constraints 「파생 96」, WSS src\WeaponSpiritShaman\DerivedGuids.cs 와 같은 방식).
    // GUID = MD5(Guids.Derive.Namespace + "|" + kind + "|" + key) 의 32자 소문자 hex. 이름공간은 한 곳 — 커밋 뒤 변경 금지(세이브 호환).
    // 시험 `exec: derived guids` 가 96 개수·유일·고정 3개 스냅샷(파이썬 독립 계산)으로 잠근다.
    public static class DerivedGuids
    {
        // kind: blast(그룹 블라스트, key "<Group>/Close|Medium" 12) · double(연속 사격, "<Group>" 6) · summon(과열 소환 변형, "<Group>/<1..9>" 54) · ward(가호, "<Group>/<1..4>" 24).
        public static readonly string[] Kinds = { "blast", "double", "summon", "ward" };

        static readonly string[] RangeNames = { "Close", "Medium" }; // 설계 §4.4 Range=Close · §5 L6 Medium

        static int TierCount(string kind) { return kind == "summon" ? WarlockRules.Dials.SummonTiers : WarlockRules.Levels.WardLevels.Length; }

        static bool IsGroupName(string s)
        {
            foreach (var g in PatronGroups.AllGroups) if (g.ToString() == s) return true;
            return false;
        }

        // key 모양을 kind 별로 검사한다(대소문자·앞자리 0·범위 밖·구분자는 던진다 — 오타가 조용히 새 GUID 를 만들지 않게).
        static void RequireKey(string kind, string key)
        {
            if (key.IndexOf('|') >= 0) throw new ArgumentException("DerivedGuids: key contains the separator", "key");
            int slash = key.IndexOf('/');
            string group = slash < 0 ? key : key.Substring(0, slash), rest = slash < 0 ? null : key.Substring(slash + 1);
            if (!IsGroupName(group)) throw new ArgumentException("DerivedGuids: unknown group in key '" + key + "'", "key");
            if (kind == "double")
            {
                if (rest != null) throw new ArgumentException("DerivedGuids: double key must be '<Group>' (got '" + key + "')", "key");
                return;
            }
            if (rest == null) throw new ArgumentException("DerivedGuids: " + kind + " key must be '<Group>/<x>' (got '" + key + "')", "key");
            if (kind == "blast")
            {
                if (Array.IndexOf(RangeNames, rest) < 0) throw new ArgumentException("DerivedGuids: blast range must be Close or Medium (got '" + key + "')", "key");
                return;
            }
            int n = TierCount(kind);
            bool ok = false;
            for (int t = 1; t <= n; t++) if (rest == t.ToString()) ok = true;
            if (!ok) throw new ArgumentException("DerivedGuids: " + kind + " tier must be 1.." + n + " (got '" + key + "')", "key");
        }

        public static string For(string kind, string key)
        {
            if (string.IsNullOrEmpty(kind)) throw new ArgumentException("DerivedGuids: kind is empty");
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("DerivedGuids: key is empty");
            if (Array.IndexOf(Kinds, kind) < 0) throw new ArgumentException("DerivedGuids: unknown kind '" + kind + "'");
            RequireKey(kind, key);
            using (var md5 = MD5.Create())
            {
                var h = md5.ComputeHash(Encoding.UTF8.GetBytes(Guids.Derive.Namespace + "|" + kind + "|" + key));
                var sb = new StringBuilder(32);
                foreach (var b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string Blast(Group g, bool medium) { return For("blast", g + "/" + (medium ? RangeNames[1] : RangeNames[0])); }
        public static string Double(Group g) { return For("double", g.ToString()); }
        public static string Summon(Group g, int tier) { return For("summon", g + "/" + tier); }
        public static string Ward(Group g, int tier) { return For("ward", g + "/" + tier); }

        // 전체 (kind, key) 96 — 순서 = kind 순 → 그룹 순 → 범위/단계 순.
        public static Tuple<string, string>[] All()
        {
            var list = new List<Tuple<string, string>>();
            foreach (var g in PatronGroups.AllGroups) foreach (var r in RangeNames) list.Add(Tuple.Create("blast", g + "/" + r));
            foreach (var g in PatronGroups.AllGroups) list.Add(Tuple.Create("double", g.ToString()));
            foreach (var g in PatronGroups.AllGroups) for (int t = 1; t <= TierCount("summon"); t++) list.Add(Tuple.Create("summon", g + "/" + t));
            foreach (var g in PatronGroups.AllGroups) for (int t = 1; t <= TierCount("ward"); t++) list.Add(Tuple.Create("ward", g + "/" + t));
            return list.ToArray();
        }
    }
}
