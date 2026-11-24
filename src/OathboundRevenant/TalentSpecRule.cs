using System;

namespace OathboundRevenant
{
     
    public sealed class PrereqSpec
    {
        public readonly int ClassLevel;            // PrerequisiteClassLevel{PaladinClass, ClassLevel}
        public readonly string RequiresFeatureKey; // PrerequisiteFeature 대상 재주 Id(없으면 null)
        public readonly int Ranks;                 // 재주 특성 Ranks(각 1회 — 설계 §6 「각 1회」)
        public PrereqSpec(int classLevel, string requires, int ranks) { ClassLevel = classLevel; RequiresFeatureKey = requires; Ranks = ranks; }
    }

    public static class TalentSpecRule
    {
        public static PrereqSpec SpecFor(TalentSpec row)
        {
            if (row == null) throw new ArgumentNullException("row");
            if (string.IsNullOrEmpty(row.Id)) throw new ArgumentException("OathboundRevenant: talent row without an Id");
            if (row.GateLevel < RevenantRules.Levels.MinLevel || row.GateLevel > RevenantRules.Levels.MaxLevel) throw new ArgumentOutOfRangeException("row", row.GateLevel, "OathboundRevenant: talent " + row.Id + " gate level out of range");
            if (Array.IndexOf(RevenantRules.Levels.TalentLevels, row.GateLevel) < 0) throw new ArgumentException("OathboundRevenant: talent " + row.Id + " gate " + row.GateLevel + " is not a talent slot level");
            if (row.Requires != null && TalentPlanIndex(row.Requires) < 0) throw new ArgumentException("OathboundRevenant: talent " + row.Id + " requires unknown talent " + row.Requires);
            return new PrereqSpec(row.GateLevel, row.Requires, 1);
        }

        static int TalentPlanIndex(string id)
        {
            for (int i = 0; i < TalentPlan.Rows.Count; i++) if (TalentPlan.Rows[i].Id == id) return i;
            return -1;
        }
    }
}
