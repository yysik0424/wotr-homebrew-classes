using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    public sealed class ResourceRow
    {
        public readonly string Ability, Resource, GrantFeature;
        public readonly int SpendAmount;
        public readonly bool IsSpend;
        public ResourceRow(string ability, string resource, string grant, int amount, bool spend) { Ability = ability; Resource = resource; GrantFeature = grant; SpendAmount = amount; IsSpend = spend; }
    }

    public static class ResourcePlan
    {
        static IEnumerable<ResourceRow> MutagenRows()
        {
            return MutagenPlan.Stats.Select(s => new ResourceRow(MutagenPlan.AbilityGuid(s), Guids.MutagenResource, Guids.MutagenFeature, 1, true));
        }

        // T7 브레스 행(판정 B1): 원소마다 기본(BreathResource) · 분출(BreathSurgeResource), 부여 특성 = 그 원소 특성(두 자원 모두 AddAbilityResources).
        static IEnumerable<ResourceRow> LaterRows()
        {
            foreach (var e in MutationsPlan.BreathElements)
            {
                yield return new ResourceRow((string)typeof(Guids.BreathAbility).GetField(e.Key).GetRawConstantValue(), Guids.BreathResource, e.Guid, 1, true);
                yield return new ResourceRow((string)typeof(Guids.BreathSurgeAbility).GetField(e.Key).GetRawConstantValue(), Guids.BreathSurgeResource, e.Guid, 1, true);
            }
        }

        public static IReadOnlyList<ResourceRow> Rows { get { return MutagenRows().Concat(LaterRows()).ToArray(); } }

        // 오류 목록(빈 목록 = 정상): 소비 플래그 꺼짐 · 소비량 1 아님 · 부여 특성 없음 · 같은 능력 두 번.
        public static List<string> Validate(IReadOnlyList<ResourceRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            var e = new List<string>();
            if (rows.Count == 0) e.Add("no resource rows");
            foreach (var r in rows)
            {
                if (!r.IsSpend) e.Add(r.Ability + ": IsSpend false");
                if (r.SpendAmount != 1) e.Add(r.Ability + ": SpendAmount " + r.SpendAmount);
                if (string.IsNullOrEmpty(r.Resource)) e.Add(r.Ability + ": no resource");
                if (string.IsNullOrEmpty(r.GrantFeature)) e.Add(r.Ability + ": resource " + r.Resource + " has no granting feature");
            }
            foreach (var g in rows.GroupBy(r => r.Ability).Where(g => g.Count() > 1)) e.Add(g.Key + ": listed " + g.Count() + " times");
            return e;
        }
    }
}
