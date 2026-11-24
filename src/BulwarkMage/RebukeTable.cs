using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
     
    // 게임 열거형(DamageEnergyType·DiceType·SavingThrowType)은 쓰지 않고 이 파일의 닫힌 열거형으로 둔다. T5 가 게임 값으로 옮길 때 모르는 값은 던진다.
    // 수치 상수는 BulwarkRules.Dials 에 이름·출처와 함께 한 곳(C3). 이 표는 그 상수로 함수를 만든다. 시험 `exec: rebuke table vs transcribed copy` 가
    // 시험 파일의 독립 전사본(설계 §R3.2 표에서 손으로 옮김)과 7행 × 열(1..20 레벨 값 포함)을 칸 단위로 대조한다.
    // 결측 규칙(C1): 해당 없는 칸은 null(주사위 없는 효과의 DiceCount·Die·Energy, 버프 없는 효과의 BuffRounds·BuffGuid, 밀기 없는 효과의 PushFeet) — 0 으로 채우지 않는다.
    public enum RebukeEnergy { Force, Electricity, Cold }
    public enum RebukeDie { D4, D6 }
    // 내성: 없음 · 반사 절반(피해) · 인내 성공 시 버프만 무효 · 의지 성공 시 무효 · 해제 판정(E7 — 내성이 아니라 시전자 레벨 판정, 설계 §R3.2).
    public enum RebukeSave { None, ReflexHalf, FortNegatesBuff, WillNegates, DispelCheck }

    public sealed class RebukeSpec
    {
        public string Id;            // E1..E7
        public string NameKey;       // StringsTable 키(T5 가 문구를 더한다)
        public string DescKey;
        public RebukeEnergy? Energy;
        public RebukeDie? Die;
        public Func<int, int> DiceCount;   // 위저드 레벨 → 주사위 개수(주사위 없는 효과는 null)
        public RebukeSave Save;
        public string BuffGuid;            // 공격자에게 거는 본편 버프(없으면 null)
        public Func<int, int> BuffRounds;  // 위저드 레벨 → 버프 라운드(버프 없는 효과는 null)
        public int? PushFeet;              // 밀기 거리(밀기 없는 효과는 null)
        public int? PushEngineDistance;    // 밀기 거리의 엔진 단위(미터, 정수 — ContextActionPush.Distance 에 넣는 값). PushFeet 와 함께만 있다
        public string CastSpellGuid;       // E7 = DispelMagicTarget(없으면 null)
    }

    public static class RebukeTable
    {
        static int RequireLevel(int wizardLevel)
        {
            if (wizardLevel < BulwarkRules.Levels.MinLevel || wizardLevel > BulwarkRules.Levels.MaxLevel)
                throw new ArgumentOutOfRangeException("wizardLevel", wizardLevel, "BulwarkMage: rebuke values are defined for wizard levels " + BulwarkRules.Levels.MinLevel + ".." + BulwarkRules.Levels.MaxLevel);
            return wizardLevel;
        }

        // ⌈L / RebukeLevelsPerDie⌉ (설계 §R3.2 「위저드 레벨 2당 1d6(올림)」).
        public static int DiceCount(int wizardLevel)
        {
            int l = RequireLevel(wizardLevel), per = BulwarkRules.Dials.RebukeLevelsPerDie;
            return (l + per - 1) / per;
        }

        // RebukeSlowBaseRounds + ⌊L / RebukeSlowLevelsPerRound⌋ (설계 §R3.2 E5).
        public static int SlowRounds(int wizardLevel)
        {
            return BulwarkRules.Dials.RebukeSlowBaseRounds + RequireLevel(wizardLevel) / BulwarkRules.Dials.RebukeSlowLevelsPerRound;
        }

        // 피트 → 엔진 거리(미터)를 가장 가까운 정수로(ContextActionPush.Distance 는 정수 ContextValue — @00E6 Calculate → conv.r4): 10피트 = 3.048 → 3(9.84피트, README 한계). 음수 = 던짐.
        public static int FeetToEngineDistance(int feet)
        {
            if (feet < 0) throw new ArgumentOutOfRangeException("feet", feet, "BulwarkMage: a push distance cannot be negative");
            long um = (long)feet * BulwarkRules.Dials.EngineMicrometersPerFoot;
            return (int)((um + 500000) / 1000000); // 1 m = 1 000 000 µm, 반올림
        }

        static Func<int, int> Constant(int rounds) { return l => { RequireLevel(l); return rounds; }; }

        public static readonly IReadOnlyList<RebukeSpec> Effects = Build();

        static RebukeSpec[] Build()
        {
            var d = new[]
            {
                new RebukeSpec { Id = "E1", Energy = RebukeEnergy.Force, Die = RebukeDie.D6, DiceCount = DiceCount, Save = RebukeSave.None },
                new RebukeSpec { Id = "E2", Energy = RebukeEnergy.Electricity, Die = RebukeDie.D6, DiceCount = DiceCount, Save = RebukeSave.ReflexHalf },
                new RebukeSpec { Id = "E3", Energy = RebukeEnergy.Cold, Die = RebukeDie.D4, DiceCount = DiceCount, Save = RebukeSave.FortNegatesBuff, BuffGuid = Guids.Game.Staggered, BuffRounds = Constant(BulwarkRules.Dials.RebukeStaggerRounds) },
                new RebukeSpec { Id = "E4", Save = RebukeSave.None, PushFeet = BulwarkRules.Dials.RebukePushFeet, PushEngineDistance = FeetToEngineDistance(BulwarkRules.Dials.RebukePushFeet) },
                new RebukeSpec { Id = "E5", Save = RebukeSave.WillNegates, BuffGuid = Guids.Game.SlowBuff, BuffRounds = SlowRounds },
                new RebukeSpec { Id = "E6", Save = RebukeSave.WillNegates, BuffGuid = Guids.Game.DazeBuff, BuffRounds = Constant(BulwarkRules.Dials.RebukeDazeRounds) },
                new RebukeSpec { Id = "E7", Save = RebukeSave.DispelCheck, CastSpellGuid = Guids.Game.DispelMagicTarget },
            };
            foreach (var s in d)
            {
                s.NameKey = s.Id.ToLowerInvariant() + ".name";
                s.DescKey = s.Id.ToLowerInvariant() + ".desc";
                Validate(s);
            }
            if (d.Select(x => x.Id).Distinct().Count() != d.Length) throw new InvalidOperationException("BulwarkMage: duplicate rebuke id");
            return d;
        }

        // 한 효과의 칸 묶음이 모순 없이 채워졌는지(C2): 피해 효과 = 에너지·주사위·개수 셋 다, 버프 효과 = GUID·라운드 둘 다, 아무 효과도 없는 행 금지.
        public static void Validate(RebukeSpec s)
        {
            if (s == null) throw new ArgumentNullException("s");
            if ((s.Energy == null) != (s.Die == null) || (s.Energy == null) != (s.DiceCount == null)) throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " has a partial damage definition");
            if ((s.BuffGuid == null) != (s.BuffRounds == null)) throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " has a buff without rounds or rounds without a buff");
            if ((s.PushFeet == null) != (s.PushEngineDistance == null)) throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " has a push distance in feet without its engine distance (or the reverse)");
            if (s.Energy == null && s.BuffGuid == null && s.PushFeet == null && s.CastSpellGuid == null) throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " does nothing");
        }

        public static RebukeSpec Get(string id)
        {
            var hit = Effects.Where(e => e.Id == id).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("BulwarkMage: rebuke effect '" + id + "' is in RebukeTable " + hit.Length + " time(s)");
            return hit[0];
        }

        // 내성 DC = SaveDcBase + ⌊위저드 레벨 / SaveDcLevelDivisor⌋ + 지능 수정치(설계 §R3.2 — 엔진 계산 부품의 결과를 점검 S9 가 절대값으로 대조할 기대값).
        public static int ExpectedDc(int wizardLevel, int intModifier)
        {
            return BulwarkRules.Dials.SaveDcBase + RequireLevel(wizardLevel) / BulwarkRules.Dials.SaveDcLevelDivisor + intModifier;
        }

         
        public static int DailyMax(int intModifier)
        {
            return Math.Max(BulwarkRules.Dials.RebukeDailyMin, BulwarkRules.Dials.RebukeDailyBase + intModifier);
        }

        // ContextRankConfig{m_Progression = Custom} 의 표(1..20 레벨 각각 한 행: baseValue = 레벨, progressionValue = f(레벨)). 진행 열거형 의미에 기대지 않는다(계획 T2).
        public static (int baseValue, int progressionValue)[] CustomProgression(Func<int, int> f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var rows = new List<(int, int)>();
            for (int l = BulwarkRules.Levels.MinLevel; l <= BulwarkRules.Levels.MaxLevel; l++) rows.Add((l, f(l)));
            return rows.ToArray();
        }
    }
}
