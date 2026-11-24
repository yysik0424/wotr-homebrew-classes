using System.Collections.Generic;

namespace OathboundRevenant
{
    // [순수] 죽음 기사의 재주 원장(계획 「재주 원장」 — 생산 입력, 시험이 계획 표의 독립 전사본과 대조). 설계 §6 표 10 + §8-c 표 10.
    public sealed class TalentSpec
    {
        public readonly string Id;
        public readonly int GateLevel;
        public readonly string Requires;   // 선행 재주 Id(없으면 null)
        public readonly string OwnerTask;
        public TalentSpec(string id, int gate, string requires, string owner) { Id = id; GateLevel = gate; Requires = requires; OwnerTask = owner; }
    }

    public static class TalentPlan
    {
        // 출처: 계획 「재주 원장」 표 1~20행(설계 §6·§8-c). 게이트 레벨 = 그 재주를 고를 수 있는 가장 낮은 슬롯 레벨.
        public static readonly IReadOnlyList<TalentSpec> Rows = new[]
        {
            new TalentSpec("PursuitOath", RevenantRules.Levels.TalentLevels[0], null, "T4"),
            new TalentSpec("TerribleGaze", RevenantRules.Levels.TalentLevels[0], null, "T6"),
            new TalentSpec("DeathsTouch", RevenantRules.Levels.TalentLevels[0], null, "T5"),
            new TalentSpec("GhostStride", RevenantRules.Levels.TalentLevels[0], null, "T7"),
            new TalentSpec("DeathsResolve", RevenantRules.Levels.TalentLevels[0], null, "T6"),
            new TalentSpec("DenseFear", RevenantRules.Levels.TalentLevels[1], null, "T6"),
            new TalentSpec("GraveArmor", RevenantRules.Levels.TalentLevels[1], null, "T7"),
            new TalentSpec("OathShield", RevenantRules.Levels.TalentLevels[1], null, "T7"),
            new TalentSpec("SoulHarvest", RevenantRules.Levels.TalentLevels[1], null, "T7"),
            new TalentSpec("LifeDrain", RevenantRules.Levels.TalentLevels[2], null, "T4"),
            new TalentSpec("CommandUndead", RevenantRules.Levels.TalentLevels[2], null, "T7"),
            new TalentSpec("WailOfTheDead", RevenantRules.Levels.TalentLevels[2], null, "T6"),
            new TalentSpec("CursedStrike", RevenantRules.Levels.TalentLevels[2], null, "T4"),
            new TalentSpec("IronBones", RevenantRules.Levels.TalentLevels[2], null, "T7"),
            new TalentSpec("DeathBlade", RevenantRules.Levels.TalentLevels[3], null, "T4"),
            new TalentSpec("EmpoweredChannel", RevenantRules.Levels.TalentLevels[3], null, "T5"),
            new TalentSpec("VengefulRiposte", RevenantRules.Levels.TalentLevels[3], null, "T7"),
            new TalentSpec("MarkOfDeath", RevenantRules.Levels.TalentLevels[3], null, "T4"),
            new TalentSpec("UnyieldingOath", RevenantRules.Levels.TalentLevels[4], null, "T7"),
            new TalentSpec("LastBreath", RevenantRules.Levels.TalentLevels[5], "UnyieldingOath", "T7"),
        };
    }
}
