using System;
using System.Linq;

namespace Golemancer
{
    // [순수] 재질 × 레벨 → 골렘 수치 목표(설계 §4.5 「우리 레벨표(절대치) × 같은 단계 상대 비율」, 비율 축 = DR·자연 방어·슬램·힘, HD·HP 비율 없음, 클램프 없음).
    // 펫 수치 특성과 합체 수치 특성이 이 하나를 쓴다(계획 C4 ⑤). 반올림 = GolemRules.Scale/StrMod(…, AwayFromZero).
    public sealed class Target
    {
        public readonly int NaturalArmor, Dr, StrengthDelta, FusionStrBonus, SlamRolls, SlamDie, SizeHpDelta;
        public Target(int naturalArmor, int dr, int strengthDelta, int fusionStrBonus, int slamRolls, int slamDie, int sizeHpDelta)
        {
            NaturalArmor = naturalArmor; Dr = dr; StrengthDelta = strengthDelta; FusionStrBonus = fusionStrBonus; SlamRolls = slamRolls; SlamDie = slamDie; SizeHpDelta = sizeHpDelta;
        }
    }

    // ContextRankConfig Custom 진행표의 축(T4 수치 특성 = NaturalArmor·Dr·StrengthDelta, T5 합체 = FusionStrBonus).
    public enum StatAxis { NaturalArmor, Dr, StrengthDelta, FusionStrBonus }

    public sealed class ProgressionEntry
    {
        public readonly int Level, Value;
        public ProgressionEntry(int level, int value) { Level = level; Value = value; }
    }

    public static class StatRecipe
    {
        public static Target Target(Material m, int level)
        {
            if (m == null) throw new ArgumentNullException("m");
            MaterialCatalog.RequireLegal(m, level); // 단계 밖 레벨 → throw(도메인 선검사, 계획 T2 Progression 주석)
            var row = GolemRules.Level(level);
            var ratio = MaterialCatalog.RatioOf(m);
            int str = GolemRules.Scale(row.Strength, ratio.Strength, MidpointRounding.AwayFromZero);
            var slam = GolemRules.SlamDice(row.SlamRolls, row.SlamDie, ratio.Slam);
            return new Target(
                GolemRules.Scale(row.NaturalArmor, ratio.NaturalArmor, MidpointRounding.AwayFromZero),
                GolemRules.Scale(row.Dr, ratio.Dr, MidpointRounding.AwayFromZero),
                str - GolemRules.BaseStrength,
                GolemRules.StrMod(str, MidpointRounding.AwayFromZero),
                slam.Rolls, slam.Die,
                GolemRules.SizeHpDelta(m.Size));
        }

        public static int Value(Target t, StatAxis axis)
        {
            if (t == null) throw new ArgumentNullException("t");
            switch (axis)
            {
                case StatAxis.NaturalArmor: return t.NaturalArmor;
                case StatAxis.Dr: return t.Dr;
                case StatAxis.StrengthDelta: return t.StrengthDelta;
                case StatAxis.FusionStrBonus: return t.FusionStrBonus;
            }
            throw new ArgumentException("StatRecipe.Value: undefined axis " + (int)axis);
        }

         
        // 도메인 밖 입력은 엉뚱한 첫·마지막 항목을 집는다 → 적용 측(T4 Materials.Reconcile)이 LegalLevels 를 먼저 검사한다.
        public static ProgressionEntry[] Progression(Material m, StatAxis axis)
        {
            if (m == null) throw new ArgumentNullException("m");
            if (!Enum.IsDefined(typeof(StatAxis), axis)) throw new ArgumentException("StatRecipe.Progression: undefined axis " + (int)axis);
            return MaterialCatalog.LegalLevels(m).Select(l => new ProgressionEntry(l, Value(Target(m, l), axis))).ToArray();
        }

        // 버프 키(재질, 슬램 단계)의 슬램 주사위 = 그 단계 합법 레벨들의 Target 슬램(T4 슬램 무기 13 의 m_DamageDice 원천). 레벨표가 단계 안에서 슬램을 바꾸면 하나로 못 정한다 → throw.
        public static Dice SlamOf(Material m, int slamTier)
        {
            if (m == null) throw new ArgumentNullException("m");
            var levels = MaterialCatalog.LegalLevels(m).Where(l => GolemRules.SlamTier(l) == slamTier).ToArray();
            if (levels.Length == 0) throw new ArgumentOutOfRangeException("slamTier", "StatRecipe.SlamOf: " + m.Id + " has no legal level in slam tier " + slamTier);
            var dice = levels.Select(l => Target(m, l)).Select(t => t.SlamRolls + "d" + t.SlamDie).Distinct().ToArray();
            if (dice.Length != 1) throw new InvalidOperationException("StatRecipe.SlamOf: " + m.Id + " tier " + slamTier + " slam differs across levels (" + string.Join(",", dice) + ")");
            var t0 = Target(m, levels[0]);
            return new Dice(t0.SlamRolls, t0.SlamDie);
        }

        // 버프 키 = (재질, 슬램 단계): 같은 키 안에서는 Polymorph 버프(모델·크기·슬램)가 같다.
        public static string BuffKey(Material m, int level)
        {
            if (m == null) throw new ArgumentNullException("m");
            MaterialCatalog.RequireLegal(m, level);
            return MaterialCatalog.BuffKeyOf(m, GolemRules.SlamTier(level));
        }

         
        public static string StatKey(Material m, int level)
        {
            if (m == null) throw new ArgumentNullException("m");
            MaterialCatalog.RequireLegal(m, level);
            return m.Id + "@L" + level;
        }
    }
}
