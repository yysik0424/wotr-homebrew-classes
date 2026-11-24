using System;

namespace Spellfist
{
    // 설계 §4.2: 8학파 (범용 제외)
    public enum School { Abjuration, Conjuration, Divination, Enchantment, Evocation, Illusion, Necromancy, Transmutation }

    // 게임 타입 의존 없음. 주문 격투가 수치 규칙의 단일 원장 (설계: docs/2026-11-07-spellfist-design.md)
    public static class SpellfistRules
    {
        // 설계 §4.2: 학파 선택 개수 3
        public const int SchoolPicks = 3;
        // 설계 §4.4: 신속화 캡/비용의 주문 레벨 상한 9
        public const int MaxSpellLevel = 9;
        // 설계 §4.5: 비전 비법(Exploit) 획득 레벨
        public static readonly int[] ExploitLevels = { 3, 7, 11, 15, 19 };
        // 설계 §11-9/§11-10: 주먹 주문강타 2레벨 해금 (기습 공격 없음)
        public const int FistSpellstrikeLevel = 2;
        // 설계 §11-11: 저장고 기본 3
        public const int ReservoirBase = 3;
        // 설계 §11-13(2026-11-16 소유자 결정, §11-11 의 2배를 되돌림): 저장고 = 3 + 1 x 레벨 + 지능 수정치
        public const int ReservoirLevelMultiplier = 1;
        // 설계 §4.5: 상위 비법 11레벨
        public const int GreaterExploitsLevel = 11;

         
        // (WizardSpellList ba0401fd 의 각 주문 jbp SpellComponent.School, blueprints.zip 2026-11-07 직독).
         
        public static readonly int[,] SchoolCounts =
        {
            { 1, 4, 3, 5, 4, 4, 2, 2, 2, 2 },   // Abjuration 29
            { 2, 5, 7, 4, 5, 5, 4, 4, 4, 3 },   // Conjuration 43
            { 0, 1, 2, 1, 1, 1, 1, 1, 1, 1 },   // Divination 10 (캔트립 0)
            { 1, 2, 2, 4, 3, 5, 3, 4, 2, 5 },   // Enchantment 31
            { 3, 5, 4, 5, 5, 3, 5, 4, 4, 3 },   // Evocation 41
            { 0, 2, 4, 1, 4, 2, 1, 2, 2, 2 },   // Illusion 20 (캔트립 0)
            { 2, 3, 8, 3, 7, 3, 5, 6, 5, 2 },   // Necromancy 44
            { 1, 7, 7, 7, 6, 8, 13, 7, 4, 2 },  // Transmutation 62
        };
        // 같은 표의 Universalist 행(버리는 주문 — 몇 개를 버리는지도 원장으로 고정).
        public static readonly int[] UniversalistCounts = { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        // WizardSpellList.SpellsByLevel 길이 = 11(0..10, 10레벨은 신화용 빈 칸; jbp 직독 2026-11-07). 우리 목록 9개도 같은 길이로 만든다.
        public const int SpellListLevels = 11;

        // 설계 §4.7 교환표의 RemoveFeatures (레벨, 브롤러 모드 특성 GUID). 출처: 정찰 §8-1 표(모드 IL 정적 필드 대입 직독). Archetype.Build 가 진행표에서 되읽는다.
        public static readonly (int level, string guid)[] RemovedFeatures =
        {
            (3, Guids.Game.ManeuverTraining1), (7, Guids.Game.ManeuverTraining2), (11, Guids.Game.ManeuverTraining3),
            (15, Guids.Game.ManeuverTraining4), (19, Guids.Game.ManeuverTraining5),
            (4, Guids.Game.Knockout), (16, Guids.Game.AwesomeBlow), (20, Guids.Game.AwesomeBlowImproved),
        };

        // 병행 금지 대상 = 브롤러 모드의 기존 아키타입 8종(정찰 §8-2; GUID 출처는 Guids.Game 주석). Archetype.Build 의 PrerequisiteNoArchetype, Register 의 집합 대조.
        public static readonly string[] BrawlerModArchetypes =
        {
            Guids.Game.ArchetypeWildChild, Guids.Game.ArchetypeVenomfist, Guids.Game.ArchetypeSnakebiteStriker, Guids.Game.ArchetypeSteelBreaker,
            Guids.Game.ArchetypeTurfer, Guids.Game.ArchetypeBeastWrestler, Guids.Game.ArchetypeExemplar, Guids.Game.ArchetypeMutagenicMauler,
        };

        static void Require(bool ok, string name, int value)
        {
            if (!ok) throw new ArgumentOutOfRangeException(name, value, name + " out of range");
        }

        // 설계 §11-13: 최대 저장고 = ReservoirBase + ReservoirLevelMultiplier*레벨 + 지능 수정치 (음수 수정치도 그대로 더함, 엔진 IncreaseResourcesByClass 와 동일)
        public static int ReservoirMax(int brawlerLevel, int intMod)
        {
            Require(brawlerLevel >= 1, "brawlerLevel", brawlerLevel);
            return ReservoirBase + ReservoirLevelMultiplier * brawlerLevel + intMod;
        }

        // 설계 §4.4: 신속화 비용 = max(1, 주문 레벨) (0레벨도 1)
        public static int QuickenCost(int spellLevel)
        {
            Require(spellLevel >= 0 && spellLevel <= MaxSpellLevel, "spellLevel", spellLevel);
            return Math.Max(1, spellLevel);
        }

        // 설계 §4.4: 신속화 가능 최대 주문 레벨 = min(저장고 잔량, 9). 0 이면 캡 버프 없음
        public static int QuickenCap(int pool)
        {
            Require(pool >= 0, "pool", pool);
            return Math.Min(pool, MaxSpellLevel);
        }

        // 시험·시나리오 기대값 전용. 게임 코드는 이 함수를 쓰지 않고 ArcanistSpellsPerDayTable 을 읽는다 (정찰 §1-3 요약; 설계 §11-2).
        public static int SpellLevelAt(int casterLevel)
        {
            Require(casterLevel >= 1, "casterLevel", casterLevel);
            if (casterLevel >= 18) return MaxSpellLevel;
            if (casterLevel <= 3) return 1;
            return casterLevel / 2;
        }
    }
}
