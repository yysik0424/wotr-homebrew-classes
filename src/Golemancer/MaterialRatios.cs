using System;
using System.Collections.Generic;
using System.Linq;

namespace Golemancer
{
    // [순수] 재질 비율(설계 §4.5 「상대 비율 = 유닛 값 ÷ 같은 단계 유닛 평균, 소수 둘째 자리 반올림, 클램프 없음」). 게임 형식 없음.
    // 입력 = 본편 유닛 수치(T4 는 게임 안에서 10개 유닛을 읽어 넣고, 결과가 MaterialCatalog.ExpectedRatios 와 다르면 빌드 실패 — 계획 C4 ②).
     
    // 단계 = 0(L1, 유닛 1) · 1..Dials.MaterialLevels.Count(각 유닛 3). 행 수가 다르거나 빠진 단계가 있으면 throw(결측을 평균에 섞지 않는다).
    public sealed class UnitRow
    {
        public readonly string Material;
        public readonly int Stage, NaturalArmor, Dr, Strength;
        public readonly decimal SlamAverage;   // 슬램 「공격 1회분」 평균(설계 §4.5: 황동 = 팔치온 + 화염 부가)
        public UnitRow(string material, int stage, int naturalArmor, int dr, decimal slamAverage, int strength)
        {
            if (string.IsNullOrEmpty(material)) throw new ArgumentException("UnitRow: empty material id");
            if (stage < 0 || stage > GolemRules.Dials.MaterialLevels.Count) throw new ArgumentOutOfRangeException("stage", "UnitRow " + material + ": stage " + stage + " outside 0.." + GolemRules.Dials.MaterialLevels.Count);
            if (naturalArmor <= 0 || dr <= 0 || slamAverage <= 0m || strength <= 0) throw new ArgumentOutOfRangeException("UnitRow " + material + ": every value must be > 0 (NA " + naturalArmor + ", DR " + dr + ", slam " + slamAverage + ", Str " + strength + ")");
            Material = material; Stage = stage; NaturalArmor = naturalArmor; Dr = dr; SlamAverage = slamAverage; Strength = strength;
        }
    }

    public sealed class Ratio
    {
        public readonly string Material;
        public readonly decimal NaturalArmor, Dr, Slam, Strength;
        public Ratio(string material, decimal naturalArmor, decimal dr, decimal slam, decimal strength)
        {
            if (string.IsNullOrEmpty(material)) throw new ArgumentException("Ratio: empty material id");
            if (naturalArmor <= 0m || dr <= 0m || slam <= 0m || strength <= 0m) throw new ArgumentOutOfRangeException("Ratio " + material + ": every ratio must be > 0");
            Material = material; NaturalArmor = naturalArmor; Dr = dr; Slam = slam; Strength = strength;
        }
    }

    public static class MaterialRatios
    {
        // 출력 = 입력 순서 그대로 행마다 1개.
        public static Ratio[] Compute(UnitRow[] rows, MidpointRounding mode)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (!Enum.IsDefined(typeof(MidpointRounding), mode)) throw new ArgumentException("MaterialRatios.Compute: undefined rounding mode " + (int)mode);
            if (rows.Any(r => r == null)) throw new ArgumentException("MaterialRatios.Compute: null row");
            var dup = rows.GroupBy(r => r.Material).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            if (dup.Length > 0) throw new ArgumentException("MaterialRatios.Compute: duplicate material " + string.Join(",", dup));
            int stages = GolemRules.Dials.MaterialLevels.Count + 1;
            for (int s = 0; s < stages; s++)
            {
                int want = s == 0 ? 1 : 3; // 설계 §4.5 단계표: L1 = 나무 1, L5·L10·L15 = 각 3
                int n = rows.Count(r => r.Stage == s);
                if (n != want) throw new ArgumentException("MaterialRatios.Compute: stage " + s + " has " + n + " row(s), expected " + want);
            }
            var outp = new List<Ratio>();
            foreach (var r in rows)
            {
                var peers = rows.Where(x => x.Stage == r.Stage).ToArray();
                outp.Add(new Ratio(r.Material,
                    Of(r.NaturalArmor, peers.Sum(x => (decimal)x.NaturalArmor), peers.Length, mode),
                    Of(r.Dr, peers.Sum(x => (decimal)x.Dr), peers.Length, mode),
                    Of(r.SlamAverage, peers.Sum(x => x.SlamAverage), peers.Length, mode),
                    Of(r.Strength, peers.Sum(x => (decimal)x.Strength), peers.Length, mode)));
            }
            return outp.ToArray();
        }

        // v ÷ (sum / n) = v × n ÷ sum — 정확한 십진 계산(1.125 가 1.1249… 로 흐려지지 않게).
        static decimal Of(decimal v, decimal sum, int n, MidpointRounding mode)
        {
            return Math.Round(v * n / sum, 2, mode);
        }
    }
}
