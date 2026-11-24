using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock
{
    // 레벨 마커 사실의 보유(게임 쪽이 HasFact 로 읽어 넣는다 — 규칙은 레벨 숫자를 읽지 않는다, 계획 Global Constraints).
    public struct KitMarkers
    {
        public bool Reach; // Reach6Feature 보유 → 블라스트·연속 사격 Medium
        public bool Double; // Double10Feature 보유 → 연속 사격
        public int WardTier; // 가호 마커 단계 0..4(Ward1~4Feature 중 보유한 최고 단계)
    }

    public sealed class KitPlan
    {
        public readonly string[] Add, Remove;
        public KitPlan(string[] add, string[] remove) { Add = add; Remove = remove; }
    }

     
    public static class KitRule
    {
        public static string GroupFeature(Group g)
        {
            switch (g)
            {
                case Group.Hell: return Guids.GroupHell;
                case Group.Abyss: return Guids.GroupAbyss;
                case Group.Fey: return Guids.GroupFey;
                case Group.Storm: return Guids.GroupStorm;
                case Group.Death: return Guids.GroupDeath;
                case Group.Star: return Guids.GroupStar;
                default: throw new ArgumentOutOfRangeException("g", g, "Warlock: unknown group");
            }
        }

        public static string SummonParent(Group g)
        {
            switch (g)
            {
                case Group.Hell: return Guids.SummonParentHell;
                case Group.Abyss: return Guids.SummonParentAbyss;
                case Group.Fey: return Guids.SummonParentFey;
                case Group.Storm: return Guids.SummonParentStorm;
                case Group.Death: return Guids.SummonParentDeath;
                case Group.Star: return Guids.SummonParentStar;
                default: throw new ArgumentOutOfRangeException("g", g, "Warlock: unknown group");
            }
        }

        // 기대 집합 = 그룹 특성 + 블라스트 정확히 1(Reach ? Medium : Close) + (Double ? 연속 사격) + 소환 부모 + (WardTier ≥ 1 ? 그 단계 가호 정확히 1).
        public static string[] Expected(Group g, KitMarkers m)
        {
            if (m.WardTier < 0 || m.WardTier > WarlockRules.Levels.WardLevels.Length)
                throw new ArgumentOutOfRangeException("m", m.WardTier, "Warlock: KitMarkers.WardTier must be 0.." + WarlockRules.Levels.WardLevels.Length);
            var list = new List<string> { GroupFeature(g), DerivedGuids.Blast(g, m.Reach) };
            if (m.Double) list.Add(DerivedGuids.Double(g));
            list.Add(SummonParent(g));
            if (m.WardTier >= 1) list.Add(DerivedGuids.Ward(g, m.WardTier));
            return list.ToArray();
        }

        // 그룹 6 + 블라스트 12 + 연속 사격 6 + 가호 24 = 48(progress.md 사전 점검 T1→T3 행).
        public static readonly string[] AllKitGuids = PatronGroups.AllGroups.SelectMany(g =>
            new[] { GroupFeature(g), DerivedGuids.Blast(g, false), DerivedGuids.Blast(g, true), DerivedGuids.Double(g) }
                .Concat(Enumerable.Range(1, WarlockRules.Levels.WardLevels.Length).Select(t => DerivedGuids.Ward(g, t)))).ToArray();

         
        public static readonly string[] PactOwnedGuids = AllKitGuids.Concat(PatronGroups.AllGroups.Select(SummonParent)).ToArray();

        static void RequireDomain(string[] guids, string what)
        {
            if (guids == null) throw new ArgumentNullException(what);
            if (guids.Any(x => x == null)) throw new ArgumentException("Warlock: " + what + " has a null entry", what);
            if (guids.Distinct().Count() != guids.Length) throw new ArgumentException("Warlock: " + what + " has duplicates", what);
            var outside = guids.Where(x => Array.IndexOf(PactOwnedGuids, x) < 0).ToArray();
            if (outside.Length > 0) throw new ArgumentException("Warlock: " + what + " has GUIDs outside the pact domain: " + string.Join(", ", outside), what);
        }

        // owned = 유닛이 가진 사실 중 PactOwnedGuids 안의 것(게임 쪽이 거른다 — 밖의 것이 오면 호출 결함이라 던진다).
        public static KitPlan Reconcile(string[] expected, string[] owned)
        {
            RequireDomain(expected, "expected");
            RequireDomain(owned, "owned");
            return new KitPlan(expected.Where(x => !owned.Contains(x)).ToArray(), owned.Where(x => !expected.Contains(x)).ToArray());
        }

        public static KitPlan RecallAll(string[] owned)
        {
            RequireDomain(owned, "owned");
            return new KitPlan(new string[0], owned.ToArray());
        }
    }
}
