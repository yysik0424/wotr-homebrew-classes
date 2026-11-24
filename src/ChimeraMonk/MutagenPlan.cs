using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    // 게임 형식 없음: 기술자·능력치·순위 형식은 엔진 열거값 이름 문자열(빌더가 Enum.Parse, 없으면 던짐).
    public enum StatKind { Strength, Dexterity, Wisdom }

    public sealed class StatBonusRow
    {
        public readonly string Descriptor, Stat, RankType;
        public StatBonusRow(string descriptor, string stat, string rankType) { Descriptor = descriptor; Stat = stat; RankType = rankType; }
    }

    public sealed class RankRow
    {
        public readonly string RankType;
        public readonly int[] Table; // [L-1] = 값, L = 1..20 (Custom 진행 20행)
        public RankRow(string rankType, int[] table) { RankType = rankType; Table = table; }
    }

    public sealed class MutagenBuffSpec
    {
        public readonly StatKind Stat;
        public readonly string BuffGuid, AbilityGuid;
        public readonly IReadOnlyList<StatBonusRow> Bonuses;
        public readonly IReadOnlyList<RankRow> Ranks;
        public MutagenBuffSpec(StatKind stat, string buff, string ability, IReadOnlyList<StatBonusRow> bonuses, IReadOnlyList<RankRow> ranks) { Stat = stat; BuffGuid = buff; AbilityGuid = ability; Bonuses = bonuses; Ranks = ranks; }
    }

    public static class MutagenPlan
    {
        // 엔진 이름(Kingmaker.Enums.ModifierDescriptor / StatType / AbilityRankType). 설계 §8: 연금술(능력치) + 자연 갑옷(AC), 정신 페널티 없음.
        public const string DescriptorAlchemical = "Alchemical";
        public const string DescriptorNaturalArmor = "NaturalArmor";
        public const string StatAc = "AC";
        public const string RankAlchemical = "StatBonus";
        public const string RankNaturalArmor = "ProjectilesCount";

        public static readonly StatKind[] Stats = { StatKind.Strength, StatKind.Dexterity, StatKind.Wisdom }; // 설계 §8 힘·민첩·지혜 택1

        // 20행 Custom 표: [L-1] = values[#{t ∈ tierLevels : t ≤ L} − 1]. 빈 입력·길이 불일치·1레벨 계단 없음 = 던짐.
        public static int[] Progression(int[] values, int[] tierLevels)
        {
            if (values == null || tierLevels == null || values.Length == 0 || values.Length != tierLevels.Length) throw new ArgumentException("ChimeraMonk: mutagen progression needs one value per tier level");
            if (tierLevels[0] != ChimeraRules.Levels.MinLevel) throw new ArgumentException("ChimeraMonk: mutagen tier levels must start at level " + ChimeraRules.Levels.MinLevel);
            var t = new int[ChimeraRules.Levels.MaxLevel];
            for (int l = ChimeraRules.Levels.MinLevel; l <= ChimeraRules.Levels.MaxLevel; l++) t[l - 1] = values[tierLevels.Count(x => x <= l) - 1];
            return t;
        }

        public static string BuffGuid(StatKind s) { return s == StatKind.Strength ? Guids.MutagenBuffStrength : s == StatKind.Dexterity ? Guids.MutagenBuffDexterity : Guids.MutagenBuffWisdom; }
        public static string AbilityGuid(StatKind s) { return s == StatKind.Strength ? Guids.MutagenAbilityStrength : s == StatKind.Dexterity ? Guids.MutagenAbilityDexterity : Guids.MutagenAbilityWisdom; }

        public static MutagenBuffSpec Buff(StatKind stat)
        {
            if (!Stats.Contains(stat)) throw new ArgumentOutOfRangeException("stat", stat, "ChimeraMonk: mutagen stat must be Strength, Dexterity or Wisdom");
            var bonuses = new[]
            {
                new StatBonusRow(DescriptorAlchemical, stat.ToString(), RankAlchemical),
                new StatBonusRow(DescriptorNaturalArmor, StatAc, RankNaturalArmor),
            };
            var ranks = new[]
            {
                new RankRow(RankAlchemical, Progression(ChimeraRules.Dials.MutagenAlchemical, ChimeraRules.Levels.MutagenTierLevels)),
                new RankRow(RankNaturalArmor, Progression(ChimeraRules.Dials.MutagenNaturalArmor, ChimeraRules.Levels.MutagenTierLevels)),
            };
            return new MutagenBuffSpec(stat, BuffGuid(stat), AbilityGuid(stat), bonuses, ranks);
        }
    }
}
