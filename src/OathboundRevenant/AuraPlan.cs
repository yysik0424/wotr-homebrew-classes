using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
     
    public sealed class AuraRow
    {
        public readonly string Key;            // 레벨 표 키 또는 재주 Id
        public readonly int EdgeRadiusFeet;
        public readonly string Target;         // "Enemy" | "Ally"
        public readonly string Effect;         // "FearPenaltyHalfHeal" · "FearImmunity" · "OathShield"
        public readonly int FearPenalty;       // FearPenaltyHalfHeal 만
        public readonly string NegatedByFact;  // 이 재주를 가진 시전자면 오라 무효(재평가 — #9) | null
        public readonly bool CheckEveryRound;
        public readonly string OwnerTask;
        public AuraRow(string key, int r, string target, string effect, int penalty, string negatedBy, bool everyRound, string owner) { Key = key; EdgeRadiusFeet = r; Target = target; Effect = effect; FearPenalty = penalty; NegatedByFact = negatedBy; CheckEveryRound = everyRound; OwnerTask = owner; }
        public float SizeFeet { get { return AuraGeometry.SizeFor(EdgeRadiusFeet, RevenantRules.Dials.MediumCorpulenceFt); } }
    }

    public static class AuraPlan
    {
         
        public static readonly IReadOnlyList<AuraRow> Rows = new[]
        {
            new AuraRow("DeathAura", RevenantRules.Dials.DeathAuraRadiusFeet, "Enemy", "FearPenaltyHalfHeal", RevenantRules.Dials.DeathAuraFearPenalty, "DenseFear", true, "T6"),
            new AuraRow("DenseFear", RevenantRules.Dials.DenseFearRadiusFeet, "Enemy", "FearPenaltyHalfHeal", RevenantRules.Dials.DenseFearPenalty, null, true, "T6"),
            new AuraRow("DeathsResolve", RevenantRules.Dials.AdjacentFeet, "Ally", "FearImmunity", 0, null, false, "T6"),
            new AuraRow("OathShield", RevenantRules.Dials.AdjacentFeet, "Ally", "OathShield", 0, null, false, "T7"),
        };

        public static AuraRow Row(string key)
        {
            var r = Rows.FirstOrDefault(x => x.Key == key);
            if (r == null) throw new ArgumentException("OathboundRevenant: no aura row " + key);
            return r;
        }
    }

     
    public sealed class FearRow
    {
        public readonly string Key, Range, Targets;
        public readonly int Daily, RadiusFeet;
        public FearRow(string key, int daily, string range, string targets, int radius) { Key = key; Daily = daily; Range = range; Targets = targets; RadiusFeet = radius; }
    }

    public static class FearPlan
    {
        // 출처: 설계 §6 공포의 응시(하루 3회, 적 하나) + P9 Close · §8-c 사자의 외침(하루 1회, 30 ft).
        public static readonly IReadOnlyList<FearRow> Rows = new[]
        {
            new FearRow("TerribleGaze", RevenantRules.Dials.GazeDaily, "Close", "Single", 0),
            new FearRow("WailOfTheDead", RevenantRules.Dials.WailDaily, "Personal", "EnemiesAround", RevenantRules.Dials.WailRadiusFeet),
        };
        public static readonly string OuterSave = "Will";
        // 출처: 검토 I3(2026-11-14) — 공포 능력·공포의 칼날 내성은 공포 기술자를 단다(죽음의 오라 −4/−6·적의 공포 내성 보너스가 SavingThrowBonusAgainstDescriptor 로 걸리게).
        // 값 = 본편 CauseFear·Scare 의 SpellDescriptorComponent 와 같은 마스크 MindAffecting | Fear | Emotion | NegativeEmotion(m_IntValue 70368744177840, blueprints.zip 2026-11-14 — 검토 R2 3).
        public static readonly IReadOnlyList<string> Descriptors = new[] { "MindAffecting", "Fear", "Emotion", "NegativeEmotion" };
        public static int DurationDiceCount { get { return RevenantRules.Dials.FearDurationDiceCount; } }
        public static int DurationDiceSides { get { return RevenantRules.Dials.FearDurationDiceSides; } }

        public static FearRow Row(string key)
        {
            var r = Rows.FirstOrDefault(x => x.Key == key);
            if (r == null) throw new ArgumentException("OathboundRevenant: no fear row " + key);
            return r;
        }
    }
}
