using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
     
    // 행 = (소서러 레벨, 주문 레벨, 주문 guid, 우리 특성 guid, 이름 키). ShadowRules.BonusSpellLevels·BonusSpells 와 Guids.BonusFeatures 에서 파생(C4).
    public sealed class BonusRow
    {
        public readonly int SorcererLevel, SpellLevel;
        public readonly string SpellGuid, FeatureGuid, NameKey;
        public BonusRow(int sorcererLevel, int spellLevel, string spellGuid, string featureGuid, string nameKey) { SorcererLevel = sorcererLevel; SpellLevel = spellLevel; SpellGuid = spellGuid; FeatureGuid = featureGuid; NameKey = nameKey; }
        public override string ToString() { return "L" + SorcererLevel + " spell level " + SpellLevel + " " + SpellGuid; }
    }

    // 선검사 판독기가 돌려주는 주문 사실: 존재 여부 · 소서러 주문 목록(WizardSpellList) 레벨(없으면 null).
    public struct SpellFacts
    {
        public bool Exists;
        public int? ListLevel;
    }

    public static class BonusSpellPlan
    {
        public static IReadOnlyList<BonusRow> Rows { get { return BuildRows(ShadowRules.BonusSpellLevels, ShadowRules.BonusSpells, Guids.BonusFeatures); } }

        public static IReadOnlyList<BonusRow> BuildRows(IReadOnlyList<(int sorcererLevel, int spellLevel)> levels, IReadOnlyList<string> spells, IReadOnlyList<string> features)
        {
            if (levels == null || spells == null || features == null) throw new ArgumentNullException("levels/spells/features");
            if (levels.Count != spells.Count || levels.Count != features.Count) throw new InvalidOperationException("ShadowSorcerer: bonus spell tables differ in length (" + levels.Count + " / " + spells.Count + " / " + features.Count + ")");
            return Enumerable.Range(0, levels.Count).Select(i => new BonusRow(levels[i].sorcererLevel, levels[i].spellLevel, spells[i], features[i], "bonus." + levels[i].spellLevel)).ToArray();
        }

        // 9행 전부 먼저 본다(만들기 전에): 존재 · 목록 레벨 == 계획 레벨 · 주문 중복. 문제 목록(빈 목록 = 문제 없음).
        public static List<string> Prevalidate(IReadOnlyList<BonusRow> rows, Func<string, SpellFacts> resolver)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (resolver == null) throw new ArgumentNullException("resolver");
            var problems = new List<string>();
            if (rows.Count == 0) problems.Add("no bonus spell rows");
            foreach (var g in rows.GroupBy(r => r.SpellGuid).Where(g => g.Count() > 1)) problems.Add("spell " + g.Key + " listed " + g.Count() + " times");
            foreach (var r in rows)
            {
                var f = resolver(r.SpellGuid);
                if (!f.Exists) { problems.Add(r + ": spell blueprint missing"); continue; }
                if (!f.ListLevel.HasValue) problems.Add(r + ": not on the sorcerer spell list");
                else if (f.ListLevel.Value != r.SpellLevel) problems.Add(r + ": on the sorcerer spell list at level " + f.ListLevel.Value + ", plan says " + r.SpellLevel);
            }
            return problems;
        }

        // 만들 행. 문제가 하나라도 있으면 빈 목록(일부만 만들지 않는다).
        public static IReadOnlyList<BonusRow> PlanCreates(IReadOnlyList<BonusRow> rows, IReadOnlyList<string> problems)
        {
            if (rows == null || problems == null) throw new ArgumentNullException("rows/problems");
            return problems.Count > 0 ? new BonusRow[0] : rows.ToArray();
        }
    }
}
