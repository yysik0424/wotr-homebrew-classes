using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Golemancer
{
    // [순수] 골렘술사 규칙·판정 다이얼의 단일 원장(C3). 게임 형식 없음 → 콘솔 시험이 직접 실행한다(modinfo testSources).
    // T1 최소분 = MinLevel·MaxLevel·Dials·BombDecrease. T2 가 LevelTable·Hp·SlamTier 등 비다이얼 멤버를 더한다(계획 Global Constraints, 원장 판정 T1→T2).
    // 정수는 static readonly(const 는 IL 에 접혀 읽는 곳을 시험이 볼 수 없다 — 컨트롤러 판정).
    // Dials 밖의 수치 필드는 구조 상수 허용 목록(MinLevel·MaxLevel·OppositionSchoolPicks·BaseStrength·LevelTable·SlamRollCandidates·SlamDieCandidates)만 — 시험 `dial: single block`.
    public static class GolemRules
    {
        // 출처: 연금술사 레벨 범위 1~20(설계 §4.1 연금술사 기반, src\Common\Checks\TestUnit.cs:44 와 같은 범위).
        public static readonly int MinLevel = 1;
        public static readonly int MaxLevel = 20;
         
        public static readonly int OppositionSchoolPicks = 3;

        public static class Dials
        {
             
            public static readonly int TauntRadiusFeet = 10;
            public static readonly int TauntAttackPenalty = -2;
            // 출처: 설계 §4.5·§12 Q1 — 재질 선택 레벨·슬램 단계 경계(작성자 기본값 5·10·15·20)
            public static readonly ReadOnlyCollection<int> MaterialLevels = Array.AsReadOnly(new[] { 5, 10, 15 });
            public static readonly ReadOnlyCollection<int> SlamTierLevels = Array.AsReadOnly(new[] { 1, 5, 10, 15, 20 });
             
            // 키 = 엔진 Kingmaker.Enums.Size 의 이름(이 파일은 게임 형식을 못 쓴다 — 쓰는 쪽이 Size.ToString() 으로 찾고, 없으면 KeyNotFoundException).
            public static readonly IReadOnlyDictionary<string, int> SizeHpBonus = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>
            {
                { "Medium", 20 },
                { "Large", 30 },
                { "Huge", 40 },
            });
            // 출처: 본편 StoneGolemSlow 341cd9a1… AbilityTargetsAround 10피트(정찰 §7)
            public static readonly int SlowAuraRadiusFeet = 10;
            // 출처: 설계 §4.1·§4.7 「10레벨부터 골렘을 입는다」(StringsTable Archetype.Description), 계획 T5 Archetype.AddAt(10, 합체 특성)
            public static readonly int FusionLevel = 10;
             
            public static readonly int RepairPercentPerSlotLevel = 10;
            // 출처: 본편 AlchemistSpellLevels bf4a0a03… Levels[20].Count = [0,5,5,5,5,5,5] — 추출물 슬롯 레벨 1..6(시험 `jbp: alchemist max extract level`)
            public static readonly int MaxExtractLevel = 6;
            // 출처: 설계 §4.7 「인접 필요」 — PF1 인접 = 5피트 칸(CRB 8장 전술 이동). 판정 = 두 유닛 중심 거리 − 두 Corpulence ≤ 5피트(엔진 접촉 사거리도 Corpulence 합에 더한다: AbilityData.GetApproachDistance @023C~0253)
            public static readonly int AdjacentFeet = 5;
            // 출처: 설계 §4.8·§4.9 「20레벨: 면역 공유」 — owner approval 2026-11-14(과부하 제외)
            public static readonly int CapstoneLevel = 20;
             
            //   초 = 엔진 Rounds.Seconds(값 × 6초). 게임 동작 값이 아니라 FX 표시 시간이다.
            public static readonly int RepairFxLifetimeRounds = 1;
            // 출처: 소유자 결정 2026-11-16 「골렘 슬램 횟수」 — 골렘의 한 라운드 슬램 횟수 표(유일한 원장, C4). 행 = (구간 시작 레벨, 평소, 폭주): 1~4 = 1 / 폭주 없음(null) ·
            //   5~10 = 1 / 2 · 11~15 = 2 / 3 · 16~20 = 2 / 4. 평소 = 폭주와 무관하게 항상(자원 소모 없음), 폭주 = 「골렘 폭주」(연금술사 5레벨 = 폭주 값이 처음 있는 행) 중.
            //   불변식(SlamRules.TableProblems — 어긋나면 형식 초기화가 던진다): 첫 행 = MinLevel, 구간 시작 오름차순, 평소 ≥ GolemHandSlams, 평소·폭주 단조 증가, 폭주 ≥ 평소 + 1, 폭주는 한번 생기면 끝까지.
            public static readonly ReadOnlyCollection<SlamCountRow> SlamCounts = Array.AsReadOnly(new[]
            {
                new SlamCountRow(1, 1, null),
                new SlamCountRow(5, 1, 2),
                new SlamCountRow(11, 2, 3),
                new SlamCountRow(16, 2, 4),
            });
            // 출처: 설계 §4.4 「골렘 슬램 1회」 = 재질 버프(Polymorph) 주 손 1개(Materials.BuildBuff m_MainHand, m_OffHand null) — 표 값에서 이것을 뺀 만큼이 추가 사지(소유자 결정 2026-11-16 의 파생 기준).
            public static readonly int GolemHandSlams = 1;
            // 출처: 설계 §4.7 「합체 = 슬램 2회」 = 형태 버프(Polymorph) 주 손 + 보조 손(Fusion.BuildForm m_MainHand·m_OffHand) — 소유자 결정 2026-11-16: 합체 중 횟수 = max(이 값, 표 값).
            public static readonly int FusionHandSlams = 2;
            // 출처: 소유자 결정 2026-11-16 「골렘 폭주 지속 = 소모한 추출물의 주문 레벨 × 2 라운드」(1레벨 2라운드 … 6레벨 12라운드) — OverdriveRules.DurationRounds.
            public static readonly int OverdriveRoundsPerExtractLevel = 2;
            // 출처: 소유자 결정 2026-11-16 「골렘 폭주 학파 효과」 — 소모한 추출물의 학파 → 지속 동안의 추가 효과(유일한 원장 — 버프·문구·시험이 여기서 파생, C4). 학파 없음은 효과 없음(행 없음).
            //   변환 = 힘 +4 · 소환 = 빠른 치유 5 · 방호 = AC +4 · 예지 = 명중 +2, 치명타 확정 +4 · 환영 = 은폐 20% · 사령 = 슬램 음에너지 +1d6 · 환혹 = 슬램 명중 시 동요 1라운드 · 방출 = 슬램 전기 +2d6.
            //   보너스 종류(본편 선례, 정찰 §6): 힘 = 강화(황소의 힘) · AC = 갑옷(마법 갑옷) · 명중 = 통찰 · 은폐 = 흐려짐(BlurBuff). 불변식 = OverdriveRules.EffectProblems(여덟 학파 정확히 한 번씩 — 어긋나면 형식 초기화가 던진다).
            public static readonly ReadOnlyCollection<SchoolEffectRow> OverdriveSchoolEffects = Array.AsReadOnly(new[]
            {
                new SchoolEffectRow("Transmutation", SchoolEffectKind.Strength, 4, 0),
                new SchoolEffectRow("Conjuration", SchoolEffectKind.FastHealing, 5, 0),
                new SchoolEffectRow("Abjuration", SchoolEffectKind.ArmorClass, 4, 0),
                new SchoolEffectRow("Divination", SchoolEffectKind.AttackAndCritConfirm, 2, 4),
                new SchoolEffectRow("Illusion", SchoolEffectKind.Concealment, 20, 0),
                new SchoolEffectRow("Necromancy", SchoolEffectKind.SlamNegativeDice, 1, 6),
                new SchoolEffectRow("Enchantment", SchoolEffectKind.SlamShaken, 1, 0),
                new SchoolEffectRow("Evocation", SchoolEffectKind.SlamElectricityDice, 2, 6),
            });
        }

        // 출처: 설계 §4.2 — 폭탄 기본항 절반 P = max(0, ceil(B/2))
        // B = 연금술사 레벨 + 지능 BonusWithoutTemp(엔진 기본항, 정찰 §3 @0198). 엔진 최종 = max(0, B + bonus) 에서 bonus −= P 이므로 B − P = floor(B/2) (B ≥ 0).
        // 음수 B(1레벨 지능 6 등)는 바닥 0 — 본편과 같은 바닥(설계 §4.2 「음수 B」).
        public static int BombDecrease(int B)
        {
            return Math.Max(0, (int)Math.Ceiling(B / 2.0));
        }

        // ---------------- T2: 골렘 레벨표·HP·슬램·비율 적용 ----------------

        // 출처: 정찰 §15 공격안, 판정 2026-11-09 (설계 §12 Q1) — 레벨 1~20 의 자연 방어 / DR / 슬램 / 힘 기준값(비율 1.00, 재질 비율 적용 전). 정찰 §15 표가 원장.
        public static readonly ReadOnlyCollection<LevelRow> LevelTable = Array.AsReadOnly(new[]
        {
            new LevelRow(1, 6, 5, 2, 6, 18),
            new LevelRow(2, 7, 5, 2, 6, 19),
            new LevelRow(3, 8, 5, 2, 6, 20),
            new LevelRow(4, 9, 6, 2, 6, 21),
            new LevelRow(5, 11, 8, 2, 8, 23),
            new LevelRow(6, 12, 8, 2, 8, 24),
            new LevelRow(7, 13, 9, 2, 8, 25),
            new LevelRow(8, 14, 10, 2, 8, 26),
            new LevelRow(9, 15, 10, 2, 8, 27),
            new LevelRow(10, 17, 12, 2, 10, 29),
            new LevelRow(11, 18, 12, 2, 10, 30),
            new LevelRow(12, 19, 13, 2, 10, 31),
            new LevelRow(13, 20, 14, 2, 10, 32),
            new LevelRow(14, 21, 14, 2, 10, 33),
            new LevelRow(15, 24, 15, 3, 10, 36),
            new LevelRow(16, 25, 15, 3, 10, 37),
            new LevelRow(17, 26, 15, 3, 10, 38),
            new LevelRow(18, 27, 15, 3, 10, 39),
            new LevelRow(19, 28, 15, 3, 10, 40),
            new LevelRow(20, 30, 15, 4, 8, 42),
        });

        // 출처: 본편 CR6_GolemWood "Strength": 18(blueprints.zip Units/Monsters/Golem Wood/CR6_GolemWood.jbp, 2026-11-09 직독; 정찰 §15 기준값 L1) — 펫 유닛 복제 원형의 힘.
        // 수치 특성의 힘 보정 StrengthDelta = 목표 힘 − 이 값(시험 `jbp: catalog units` 가 원형 유닛 힘과 대조).
        public static readonly int BaseStrength = 18;

         
        public static readonly ReadOnlyCollection<int> SlamRollCandidates = Array.AsReadOnly(new[] { 1, 2, 3, 4, 5, 6 });
        public static readonly ReadOnlyCollection<int> SlamDieCandidates = Array.AsReadOnly(new[] { 6, 8, 10 });

         
        public const string BaseSize = "Medium";

        static void RequireLevel(int level, string what)
        {
            if (level < MinLevel || level > MaxLevel)
                throw new ArgumentOutOfRangeException("level", "GolemRules." + what + ": level " + level + " outside " + MinLevel + ".." + MaxLevel);
        }

        public static LevelRow Level(int level)
        {
            RequireLevel(level, "Level");
            var row = LevelTable[level - MinLevel];
            if (row.Level != level) throw new InvalidOperationException("GolemRules.LevelTable row " + (level - MinLevel) + " is level " + row.Level + ", expected " + level);
            return row;
        }

        static int SizeBonus(string size)
        {
            if (size == null) throw new ArgumentNullException("size");
            int v;
            if (!Dials.SizeHpBonus.TryGetValue(size, out v)) throw new ArgumentException("GolemRules: size '" + size + "' has no HP bonus (known: " + string.Join(",", Dials.SizeHpBonus.Keys) + ")");
            return v;
        }

        // 엔진 몬스터 HP 식(정찰 §4.2 ApplyHitPoints d10 평균 5.5/HD → 5n + ceil(n/2)) + 크기 보너스. 설계 §12 Q1 「L1 26 → L5 58(대형) → L10 85 → L15 113 → L20 140」.
        public static int Hp(int level, string size)
        {
            RequireLevel(level, "Hp");
            return 5 * level + (level + 1) / 2 + SizeBonus(size);
        }

        // 수치 특성이 더하는 HP = 크기 보너스 − 원형(중형) 보너스: 대형 +10 · 초대형 +20(원장 T4 행 「SizeHpDelta」).
        public static int SizeHpDelta(string size)
        {
            return SizeBonus(size) - SizeBonus(BaseSize);
        }

        // 슬램 단계 1..5 = Dials.SlamTierLevels 중 level 이하인 경계 수.
        public static int SlamTier(int level)
        {
            RequireLevel(level, "SlamTier");
            int tier = Dials.SlamTierLevels.Count(b => b <= level);
            if (tier < 1) throw new InvalidOperationException("GolemRules.SlamTier: no tier boundary <= " + level + " (Dials.SlamTierLevels must start at MinLevel)");
            return tier;
        }

         
        public static Dice SlamDice(int rolls, int die, decimal ratio)
        {
            if (rolls < 1) throw new ArgumentOutOfRangeException("rolls", "GolemRules.SlamDice: rolls " + rolls + " < 1");
            if (die < 2) throw new ArgumentOutOfRangeException("die", "GolemRules.SlamDice: die " + die + " < 2");
            if (ratio <= 0m) throw new ArgumentOutOfRangeException("ratio", "GolemRules.SlamDice: ratio " + ratio + " <= 0");
            decimal target = new Dice(rolls, die).Average * ratio;
            Dice best = null;
            decimal bestErr = 0m;
            foreach (int r in SlamRollCandidates)
                foreach (int d in SlamDieCandidates)
                {
                    var c = new Dice(r, d);
                    decimal err = Math.Abs(c.Average - target);
                    if (best == null || err < bestErr || (err == bestErr && (r < best.Rolls || (r == best.Rolls && d < best.Die))))
                    {
                        best = c;
                        bestErr = err;
                    }
                }
            if (best == null) throw new InvalidOperationException("GolemRules.SlamDice: no candidates");
            return best;
        }

        // 비율 적용(설계 §4.5 「우리 레벨표 × 비율」). 반올림 모드는 인자로 명시(계획 Global Constraints: 수치 = AwayFromZero).
        public static int Scale(int v, decimal ratio, MidpointRounding mode)
        {
            if (v < 0) throw new ArgumentOutOfRangeException("v", "GolemRules.Scale: value " + v + " < 0");
            if (ratio <= 0m) throw new ArgumentOutOfRangeException("ratio", "GolemRules.Scale: ratio " + ratio + " <= 0");
            if (!Enum.IsDefined(typeof(MidpointRounding), mode)) throw new ArgumentException("GolemRules.Scale: undefined rounding mode " + (int)mode);
            return (int)Math.Round(v * ratio, mode);
        }

        // 능력치 수정치 round((str − 10) / 2). 합체 힘 보너스 = 이 값(정찰 §15 「수정치만큼」, 설계 §12 Q1: 황금 L10 힘 33 → +12).
        public static int StrMod(int str, MidpointRounding mode)
        {
            if (str < 1) throw new ArgumentOutOfRangeException("str", "GolemRules.StrMod: score " + str + " < 1");
            if (!Enum.IsDefined(typeof(MidpointRounding), mode)) throw new ArgumentException("GolemRules.StrMod: undefined rounding mode " + (int)mode);
            return (int)Math.Round((str - 10) / 2m, mode);
        }
    }

    // 레벨표 한 행(비율 1.00 기준값).
    public sealed class LevelRow
    {
        public readonly int Level, NaturalArmor, Dr, SlamRolls, SlamDie, Strength;
        public LevelRow(int level, int naturalArmor, int dr, int slamRolls, int slamDie, int strength)
        {
            Level = level; NaturalArmor = naturalArmor; Dr = dr; SlamRolls = slamRolls; SlamDie = slamDie; Strength = strength;
        }
        public override string ToString() { return "L" + Level + " " + NaturalArmor + "/" + Dr + "/" + SlamRolls + "d" + SlamDie + "/" + Strength; }
    }

    // 주사위 rolls d die. 평균 = rolls × (die + 1) / 2.
    public sealed class Dice
    {
        public readonly int Rolls, Die;
        public Dice(int rolls, int die)
        {
            if (rolls < 1) throw new ArgumentOutOfRangeException("rolls");
            if (die < 2) throw new ArgumentOutOfRangeException("die");
            Rolls = rolls; Die = die;
        }
        public decimal Average { get { return Rolls * (Die + 1) / 2m; } }
        public override string ToString() { return Rolls + "d" + Die; }
    }
}
