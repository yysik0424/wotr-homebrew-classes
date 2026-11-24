using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
     
    // static readonly(const 아님): const 는 빌더 IL 에 리터럴로 접혀 `wiring: … uses plan` 의 「수치 ldc 없음」 단언이 무의미해진다.
    // 생산 코드가 소비하지 않는 함수·다이얼은 두지 않는다(독립 검토 I-1, 2026-11-14 — 엔진이 계산하는 DC·공격 횟수 등은 점검 시나리오의 리터럴 신탁이 맡는다).
    // 형태(부분 변환) 성장 수치는 여기 두지 않는다 — CRC 마다 다르므로 T7 AspectPlan(생산 입력)이 본편 값을 행으로 갖는다.
    public struct Dice
    {
        public readonly int Count, Sides;
        public Dice(int count, int sides)
        {
            if (count < 1 || sides < 2) throw new ArgumentOutOfRangeException("count", "ChimeraMonk: dice " + count + "d" + sides + " is not a die");
            Count = count; Sides = sides;
        }
        public override string ToString() { return Count + "d" + Sides; }
        public override bool Equals(object o) { return o is Dice && ((Dice)o).Count == Count && ((Dice)o).Sides == Sides; }
        public override int GetHashCode() { return Count * 100 + Sides; }
    }

    public enum MutationKind { Limb, Passive, Ability, Toggle, Selection }

    public sealed class MutationSpec
    {
        public readonly string Id;
        public readonly int GateLevel;   // 0 = 조건 없음, 아니면 몽크 레벨 조건
        public readonly string Requires; // null 또는 선행 변이 Id
        public readonly MutationKind Kind;
        public MutationSpec(string id, int gateLevel, string requires, MutationKind kind) { Id = id; GateLevel = gateLevel; Requires = requires; Kind = kind; }
    }

    // 부분 변환 단계 한 줄(소유자 결정 2026-11-16 4번): 단계 번호 → 변이 Id · 선행 변이 · 최소 몽크 레벨. ChimeraRules.AspectStages 가 유일한 표.
    public sealed class AspectStageSpec
    {
        public readonly int Number, GateLevel;
        public readonly string Id, Requires; // Requires = 앞 단계의 변이 Id(첫 단계는 null)
        public AspectStageSpec(int number, string id, string requires, int gateLevel) { Number = number; Id = id; Requires = requires; GateLevel = gateLevel; }
    }

    public static class ChimeraRules
    {
        public static class Levels
        {
            public static readonly int MinLevel = 1;  // 설계 §3 몽크 1~20 (MonkProgression.jbp LevelEntries 1..20)
            public static readonly int MaxLevel = 20; // 설계 §3
            public static readonly int[] MutationLevels = { 2, 4, 6, 8, 10, 12, 14, 16, 18 }; // 설계 §6 변이 선택 9번
            public static readonly int[] RemovedBonusFeatLevels = { 2, 6, 10 };            // 설계 §6 [판정] 제거 = 몽크 보너스 특기
            public static readonly int[] KeptBonusFeatLevels = { 1, 14, 18 };              // 설계 §6 유지 = 몽크 보너스 특기
            public static readonly int[] FastMovementRemovedLevels = { 3 };                 // 판정(소유자 결정 2026-11-16: 명경지수·빠른 이동 제거): 본편 MonkProgression.jbp 에서 MonkFastMovementUnlock 이 있는 레벨 전부(3레벨 한 번)
            public static readonly int[] StillMindRemovedLevels = { 4 };                    // 판정(소유자 결정 2026-11-16: 명경지수·빠른 이동 제거): 본편 MonkProgression.jbp 에서 StillMind 가 있는 레벨 전부(4레벨 한 번)
            public static readonly int GateLevel = 10;                                      // 설계 §7 [판정] PrerequisiteClassLevel(Monk, 10)
            public static readonly int[] LimbStepLevels = { 1, 8, 16 };                    // 판정(소유자 결정 2026-11-16, 설계 「2026-11-16 소유자 결정」 1번): 구간 1–3/4–7/8–11/12–15/16–19/20 의 기본 단계 0,0,1,1,2,2 — 인접 구간이 같은 주사위인 것은 의도(옛 설계 §6 의 4·12·20 단계 폐기)
            public static readonly int[] ScalesStepLevels = { 5, 10, 15, 20 };             // 설계 §7 비늘 5·10·15·20레벨 +1
            public static readonly int[] MutagenTierLevels = { 1, 10, 16 };                // 설계 §8 +4 / 10레벨 +6 / 16레벨 +8
            public static readonly int[] AspectStageGateLevels = { 0, 8, 14 };             // 판정(소유자 결정 2026-11-16): 부분 변환 최대 3 · 단계별 최소 몽크 레벨 I = 조건 없음, II = 8, III = 14 (원소 수 = 단계 수)
        }

        public static class Dials
        {
            public static readonly int MutagenDailyBase = 3;          // 설계 §8 [판정] 하루 3 + 지혜
            public static readonly int MutagenDailyMin = 1;           // 설계 §8 [작성자] max(1, …)
            public static readonly int[] MutagenAlchemical = { 4, 6, 8 };   // 설계 §8 연금술 +4/+6/+8
            public static readonly int[] MutagenNaturalArmor = { 2, 4, 6 }; // 설계 §8 자연 갑옷 +2/+4/+6
            public static readonly int ScalesBase = 2;                // 설계 §7 비늘 고를 때 +2
            public static readonly int ScalesMax = 6;                 // 설계 §7·§11 [판정] 최대 +6
            public static readonly int ScalesMutagenBonus = 1;        // 설계 §7 비늘 강화 +1
            public static readonly int BreathDailyBase = 3;           // 설계 §7 브레스 하루 3회
            public static readonly int BreathLevelsPerDie = 2;        // 설계 §7 몽크 레벨 2당 1d6
            public static readonly int BreathConeFeet = 30;           // 설계 §7 30 ft 원뿔
            public static readonly Dice BreathDie = new Dice(1, 6);   // 설계 §7 몽크 레벨 2당 1d6(주사위 면 — 개수는 BreathDiceCount)
            public static readonly int VenomRounds = 6;               // 설계 §7 독 6라운드
            public static readonly Dice VenomDie = new Dice(1, 2);    // 설계 §7 건강 1d2
            public static readonly int VenomMutagenDc = 2;            // 설계 §7 독 강화 DC +2
            public static readonly int LegsSpeedFeet = 10;            // 설계 §7 추가 다리 속도 +10 ft
            public static readonly int LegsMutagenSpeedFeet = 10;     // 설계 §7 추가 다리 강화 속도 +10 ft
            public static readonly int LegsManeuverDefense = 4;       // 설계 §7 넘어뜨리기·밀치기 방어 +4
            public static readonly int EyesPerception = 2;            // 설계 §7 추가 눈 지각 +2
            public static readonly int EyesMutagenPerception = 2;     // 설계 §7 추가 눈 강화 지각 +2
            public static readonly int EarsPerception = 2;            // 설계 §7 추가 귀 지각 +2
            public static readonly int EarsMutagenPerception = 2;     // 판정 D5(2026-11-14) 추가 귀 강화 = 지각 +2(정신 집중 +2 폐기)
            public static readonly int SensesFeet = 30;               // 설계 §7 추가 감각 맹목감지 30 ft
            public static readonly int SensesMutagenFeet = 60;        // 설계 §7 추가 감각 강화 60 ft
            public static readonly int HeartHp = 2;                   // 설계 §7 추가 심장 체력 +2
            public static readonly int HeartMutagenFastHealing = 2;   // 설계 §7 추가 심장 강화 빠른 치유 2
            public static readonly int WingsMutagenSpeedFeet = 10;    // 설계 §7 날개 강화 이동 +10 ft
            public static readonly int ArmMutagenAttack = 1;          // 설계 §7 추가 팔 강화 명중 +1
            public static readonly int MutationPicks = 9;             // 설계 §6 [판정] 변이 9번
            public static readonly int MutationOptions = 18;          // 설계 §7 변이 16종 + 판정(소유자 결정 2026-11-16) 뿔 · 부분 변환 II · 부분 변환 III − 판정(소유자 결정 2026-11-17) 추가 꼬리 삭제
            public static readonly int BreathSurgeBase = 1;           // 판정 B1(2026-11-14) 분출 자원 최대 1
            public static readonly int LungSurgeBonus = 1;            // 판정 B1 추가 폐 = 분출 자원 +1
        }

        // 자연 무기 사다리 1d4→1d6→1d8→1d10. 기본 = 칸 LimbTier(0..2 → 1d4..1d8), 변이약 중 = 한 칸 위(1d6..1d10). 1d2·1d3·2d6·2d8 은 쓰이지 않는다.
        public static readonly Dice[] LimbLadder = { new Dice(1, 4), new Dice(1, 6), new Dice(1, 8), new Dice(1, 10) }; // 판정(소유자 결정 2026-11-16): 본편 기본 자연 무기(발톱 1d4)에서 시작, 최대 1d8, 변이약 중 최대 1d10

        static void RequireLevel(int monkLevel)
        {
            if (monkLevel < Levels.MinLevel || monkLevel > Levels.MaxLevel) throw new ArgumentOutOfRangeException("monkLevel", monkLevel, "ChimeraMonk: monk level must be " + Levels.MinLevel + ".." + Levels.MaxLevel);
        }

        // 사다리 칸(0 = 사다리의 첫 주사위). 1..20 밖 = 던짐(C1 — 0 레벨을 첫 칸으로 조용히 바꾸지 않는다).
        public static int LimbTier(int monkLevel)
        {
            RequireLevel(monkLevel);
            return Levels.LimbStepLevels.Count(s => s <= monkLevel) - 1;
        }

        public static Dice LimbDice(int monkLevel, bool mutagen)
        {
            int at = LimbTier(monkLevel) + (mutagen ? 1 : 0);
            // C2: 구간 수와 사다리 길이가 어긋나면(구간을 더하고 사다리를 안 늘림) 던진다 — 꼭대기 칸으로 조용히 깎지 않는다.
            if (at < 0 || at >= LimbLadder.Length) throw new InvalidOperationException("ChimeraMonk: limb ladder has " + LimbLadder.Length + " step(s), level " + monkLevel + (mutagen ? " mutated" : "") + " needs step " + at);
            return LimbLadder[at];
        }

        public static int ScalesValue(int monkLevel, bool mutagen)
        {
            RequireLevel(monkLevel);
            return Math.Min(Dials.ScalesMax, Dials.ScalesBase + Levels.ScalesStepLevels.Count(s => s <= monkLevel)) + (mutagen ? Dials.ScalesMutagenBonus : 0);
        }

        // 부분 변환 단계 표(소유자 결정 2026-11-16 4번): 단계 n 의 변이 Id = "Aspect"(n = 1) / "Aspect" + n, 선행 = 앞 단계, 최소 몽크 레벨 = Levels.AspectStageGateLevels[n − 1].
        //  II·III 을 따로 적지 않는다 — 변이 행(Mutations)·GUID(Guids.Mutation.<Id>, Guids.AspectForm<n>)·선택지(MutationsPlan.AspectStageForms)·아이콘이 전부 이 표에서 나온다(C4).
        static string AspectStageId(int number) { return number == 1 ? "Aspect" : "Aspect" + number; }
        public static readonly IReadOnlyList<AspectStageSpec> AspectStages = Enumerable.Range(1, Levels.AspectStageGateLevels.Length) // 판정(소유자 결정 2026-11-16): 단계 수·레벨 = Levels.AspectStageGateLevels
            .Select(n => new AspectStageSpec(n, AspectStageId(n), n == 1 ? null : AspectStageId(n - 1), Levels.AspectStageGateLevels[n - 1])).ToArray();

        // 설계 §7 변이 표(순서 = 설계 표) + 2026-11-16 소유자 결정 − 2026-11-17 소유자 결정(추가 꼬리 삭제). 게이트 10 = 브레스·날개·추가 팔·추가 감각. 선행 변이를 요구하는 것은 부분 변환 II·III 뿐이다.
        // 뿔(Horn) = 소유자 결정 2번: 조건 없음, 들이받기 자연 공격 1회 — 기본 자연 무기(꼬리·물기·발톱) 바로 뒤.
        // 부분 변환 I·II·III = 소유자 결정 4번: AspectStages 표의 행을 비늘 뒤(옛 부분 변환 자리)에 차례로 넣는다.
        public static readonly IReadOnlyList<MutationSpec> Mutations = BuildMutations(); // 설계 §7 변이 표 + 판정(소유자 결정 2026-11-16 · 2026-11-17)

        // 폐지된 변이의 Id(선택지 = Mutations 에는 없다). 블루프린트만 같은 GUID 로 남긴다 — 효과 부품 없음 · 숨김(MutationsPlan.RetiredRows → Mutations.BuildRetired).
        //  ExtraTail: 엔진이 꼬리 종류 추가 사지를 캐릭터당 1개로 제한해(UnitBody.AddAdditionalLimb IL_0012~0031 → MarkWorstLimbItemAsUnused, 본편 BlueprintPersonageLimits Tail 1) 꼬리 공격이 늘지 않았다.
        public static readonly IReadOnlyList<string> RetiredMutations = new[] { "ExtraTail" }; // 판정(소유자 결정 2026-11-17: 추가 꼬리 삭제, 세이브 호환으로 블루프린트만 남김)

        static IReadOnlyList<MutationSpec> BuildMutations()
        {
            var l = new List<MutationSpec>
            {
                new MutationSpec("Tail", 0, null, MutationKind.Limb),
                new MutationSpec("Bite", 0, null, MutationKind.Limb),
                new MutationSpec("Claw", 0, null, MutationKind.Limb),
                new MutationSpec("Horn", 0, null, MutationKind.Limb),
                new MutationSpec("Venom", 0, null, MutationKind.Passive),
                new MutationSpec("Scales", 0, null, MutationKind.Passive),
            };
            l.AddRange(AspectStages.Select(s => new MutationSpec(s.Id, s.GateLevel, s.Requires, MutationKind.Selection)));
            l.AddRange(new[]
            {
                new MutationSpec("Wings", Levels.GateLevel, null, MutationKind.Passive),
                new MutationSpec("Breath", Levels.GateLevel, null, MutationKind.Selection),
                new MutationSpec("ExtraArm", Levels.GateLevel, null, MutationKind.Limb),
                new MutationSpec("ExtraLegs", 0, null, MutationKind.Passive),
                new MutationSpec("ExtraEyes", 0, null, MutationKind.Passive),
                new MutationSpec("ExtraEars", 0, null, MutationKind.Passive),
                new MutationSpec("ExtraSenses", Levels.GateLevel, null, MutationKind.Passive),
                new MutationSpec("ExtraHeart", 0, null, MutationKind.Passive),
                new MutationSpec("ExtraLung", 0, null, MutationKind.Passive),
            });
            return l.ToArray();
        }
    }
}
