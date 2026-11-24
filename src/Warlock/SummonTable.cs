using System;
using System.Linq;
using G = Warlock.Guids.Game;

namespace Warlock
{
    // 소환 표 한 칸. Substitute = 설계 표의 [대체](그룹에 맞는 생물이 없어 본편 SM 생물(+ 템플릿)로 채운 칸). TemplateBuffGuid null = 템플릿 없음.
    public sealed class Cell
    {
        public readonly Group Group;
        public readonly int Tier;
        public readonly string UnitGuid;
        public readonly int Cr; // 설계 §4.6 표의 CR(units.txt / jbp Experience.CR — 정찰 §16-T1). 판정에 쓰지 않는 기록값.
        public readonly bool Substitute;
        public readonly string TemplateBuffGuid;
        public readonly bool Dlc5;
        public readonly string Note; // 설계 표 칸의 글(한 줄)
         
        public readonly bool IxAugment;
        public Cell(Group group, int tier, string unitGuid, int cr, bool substitute, string templateBuffGuid, bool dlc5, string note, bool ixAugment)
        {
            Group = group; Tier = tier; UnitGuid = unitGuid; Cr = cr; Substitute = substitute; TemplateBuffGuid = templateBuffGuid; Dlc5 = dlc5; Note = note; IxAugment = ixAugment;
        }
    }

     
    // 유닛 GUID = Guids.Game(정찰 §16-T1 jbp 되읽기, 42종 전부 진영 Summoned — 시험 `jbp: summon units 54 exist + faction Summoned`).
    public static class SummonTable
    {
        // Verify 의 기대 개수. 출처 = 설계 §4.6 표(칸을 센 값) = 진행 원장 사전 점검 T1 행(15 = 12 + 요정 3, 템플릿 12, DLC5 1).
        const int ExpectedCells = 54; // 6 그룹 × 9 단계
        const int ExpectedSubstitutes = 15; // 지옥 I·II·IV, 심연 I·II·IV, 요정 II·III·IV, 죽음 I·II, 별 I·II·III·IV
        const int ExpectedTemplateCells = 12; // [대체] 중 요정 3칸을 뺀 것
        const int ExpectedSubstitutesWithoutTemplate = 3; // 요정 II·III·IV(물 원소 그대로)
        const int ExpectedDlc5Cells = 1; // 심연 III SummonedAbrikandiluFrozen
         
        const int ExpectedIxBoostCells = 3;

        static Cell C(Group g, int tier, string unit, int cr, string note) { return new Cell(g, tier, unit, cr, false, null, false, note, false); }
        static Cell S(Group g, int tier, string unit, int cr, string template, string note) { return new Cell(g, tier, unit, cr, true, template, false, note, false); }
        // IX 보정 칸(항목 9) — 대체 칸이 아닌 일반 칸이다.
        static Cell X(Group g, int tier, string unit, int cr, string note) { return new Cell(g, tier, unit, cr, false, null, false, note, true); }

        const string WaterAsIs = "물 원소 그대로(설계 표에 템플릿 없음)";

        public static readonly Cell[] Cells =
        {
            S(Group.Hell, 1, G.DogSummoned, 1, G.SummonMonsterFiendishBuffI, "[대체] 개 + 악 템플릿 I"),
            S(Group.Hell, 2, G.WolfSummon, 1, G.SummonMonsterFiendishBuffI, "[대체] 늑대 + 악 I"),
            C(Group.Hell, 3, G.HellhoundSummoned, 3, "헬하운드 CR3"),
            S(Group.Hell, 4, G.DireWolfSummon, 4, G.SummonMonsterFiendishBuffII, "[대체] 다이어울프 + 악 II"),
            C(Group.Hell, 5, G.ManticoreSummoned, 5, "맨티코어 CR5(LE 괴수)"),
            C(Group.Hell, 6, G.SummonedFireElementalHuge, 7, "거대 화염 원소 CR7"),
            C(Group.Hell, 7, G.SummonedFireElementalGreater, 9, "상급 화염 원소 CR9"),
            C(Group.Hell, 8, G.SummonedFireElementalElder, 11, "장로 화염 원소 CR11"),
            X(Group.Hell, 9, G.SummonedFireElementalElder, 11, "장로 화염 원소 CR11(VIII 반복) + 두 번째 소환 강화(항목 9)"),

            S(Group.Abyss, 1, G.DogSummoned, 1, G.SummonMonsterFiendishBuffI, "[대체] 개 + 악 템플릿 I"),
            S(Group.Abyss, 2, G.WolfSummon, 1, G.SummonMonsterFiendishBuffI, "[대체] 늑대 + 악 I"),
            new Cell(Group.Abyss, 3, G.SummonedAbrikandiluFrozen, 3, false, null, true, "아브리칸딜루 CR3(DLC5 유닛, 설계 결정대로 유지 — DlcCondition 없음)", false),
            S(Group.Abyss, 4, G.DireWolfSummon, 4, G.SummonMonsterFiendishBuffII, "[대체] 다이어울프 + 악 II"),
            C(Group.Abyss, 5, G.BabauSummon, 6, "바바우 CR6"),
            C(Group.Abyss, 6, G.VrockSummon, 9, "브록 CR9"),
            C(Group.Abyss, 7, G.GibrilethSummon, 11, "기브릴레스 CR11"),
            C(Group.Abyss, 8, G.CR12_NabasuAdvancedSummoned, 12, "나바수 CR12"),
            C(Group.Abyss, 9, G.OolioddrooSummon, 13, "울리오드루 CR13"),

            C(Group.Fey, 1, G.MiteSummoned, 1, "마이트 CR1"),
            S(Group.Fey, 2, G.SummonedWaterElementalSmall, 1, null, "[대체] 소형 물 원소 CR1 — " + WaterAsIs),
            S(Group.Fey, 3, G.SummonedWaterElementalMedium, 3, null, "[대체] 중형 물 원소 CR3 — " + WaterAsIs),
            S(Group.Fey, 4, G.SummonedWaterElementalMedium, 3, null, "[대체] 중형 물 원소 CR3 — " + WaterAsIs),
            C(Group.Fey, 5, G.RedcapSummoned, 6, "레드캡 CR6"),
            C(Group.Fey, 6, G.WillOWispSummon, 6, "윌오위습 CR6(이형)"),
            C(Group.Fey, 7, G.BogeymanSummoned, 10, "보기맨 CR10"),
            C(Group.Fey, 8, G.NereidSummoned, 10, "네레이드 CR10"),
            C(Group.Fey, 9, G.WildHunt_ArcherSummon, 13, "와일드 헌트 궁수 CR13"),

            C(Group.Storm, 1, G.SummonedAirElementalSmall, 1, "소형 공기 원소 CR1"),
            C(Group.Storm, 2, G.SummonedWaterElementalSmall, 1, "소형 물 원소 CR1(같은 CR 반복)"),
            C(Group.Storm, 3, G.SummonedAirElementalMedium, 3, "중형 공기 원소 CR3"),
            C(Group.Storm, 4, G.SummonedAirElementalLarge, 5, "대형 공기 원소 CR5"),
            C(Group.Storm, 5, G.SummonedWaterElementalLarge, 5, "대형 물 원소 CR5"),
            C(Group.Storm, 6, G.SummonedAirElementalHuge, 7, "거대 공기 원소 CR7"),
            C(Group.Storm, 7, G.SummonedAirElementalGreater, 9, "상급 공기 원소 CR9"),
            C(Group.Storm, 8, G.SummonedAirElementalElder, 11, "장로 공기 원소 CR11"),
            X(Group.Storm, 9, G.SummonedWaterElementalElder, 11, "장로 물 원소 CR11(같은 CR 반복) + 두 번째 소환 강화(항목 9)"),

            S(Group.Death, 1, G.DogSummoned, 1, G.SummonMonsterFiendishBuffI, "[대체] 개 + 악 템플릿 I"),
            S(Group.Death, 2, G.WolfSummon, 1, G.SummonMonsterFiendishBuffI, "[대체] 늑대 + 악 I"),
            C(Group.Death, 3, G.SummonedSkeletonChampion, 3, "해골 용사 CR3"),
            C(Group.Death, 4, G.SummonedSkeletonAbomination, 3, "해골 흉물 CR3"),
            C(Group.Death, 5, G.SpectreSummoned, 7, "스펙터 CR7"),
            C(Group.Death, 6, G.FallenSummon, 8, "폴른 CR8(유령형, 언데드 확인 — 정찰 §10)"),
            C(Group.Death, 7, G.CR9_ShadowGreaterAdvancedSummoned, 9, "상급 그림자 CR9"),
            C(Group.Death, 8, G.CR11_GraveknightSummoned, 11, "그레이브나이트 CR11"),
            X(Group.Death, 9, G.CR11_GraveknightSummoned, 11, "그레이브나이트 CR11(VIII 반복) + 두 번째 소환 강화(항목 9)"),

            S(Group.Star, 1, G.DogSummoned, 1, G.SummonMonsterCelestialBuffI, "[대체] 개 + 선 템플릿 I"),
            S(Group.Star, 2, G.WolfSummon, 1, G.SummonMonsterCelestialBuffI, "[대체] 늑대 + 선 I"),
            S(Group.Star, 3, G.MonitorLizardSummoned, 2, G.SummonMonsterCelestialBuffI, "[대체] 왕도마뱀 + 선 I"),
            S(Group.Star, 4, G.DireWolfSummon, 4, G.SummonMonsterCelestialBuffII, "[대체] 다이어울프 + 선 II"),
            C(Group.Star, 5, G.AzataBralaniSummoned, 6, "브랄라니 CR6"),
            C(Group.Star, 6, G.CR7_AzataLillendSummoned, 7, "릴렌드 CR7"),
            C(Group.Star, 7, G.MovanicDevaSummoned, 10, "모바닉 데바 CR10"),
            C(Group.Star, 8, G.MonadicDevaSummoned, 12, "모나딕 데바 CR12"),
            C(Group.Star, 9, G.AzataGhaelSummoned, 13, "가엘 CR13"),
        };

        // 단계 → 본편 SM 단일 능력(아이콘 원천 — 작성자 결정, 정찰 §16-T5) · AfterSpawn 연출 버프(본편 SM 단일 능력 AfterSpawn 첫 ApplyBuff — 정찰 §16-T1 표 = jbp).
        // 시험 `jbp: summon monster single abilities shape` 가 아홉 능력의 jbp m_Icon·첫 ApplyBuff 와 대조한다.
        static readonly string[] SmSingle =
        {
            G.SummonMonsterISingle, G.SummonMonsterIISingle, G.SummonMonsterIIISingle, G.SummonMonsterIVSingle, G.SummonMonsterVSingle,
            G.SummonMonsterVISingle, G.SummonMonsterVIISingle, G.SummonMonsterVIIISingle, G.SummonMonsterIXSingle,
        };
        static readonly string[] SmFx =
        {
            G.SummonedCreatureSpawnMonsterI_III, G.SummonedCreatureSpawnMonsterI_III, G.SummonedCreatureSpawnMonsterI_III,
            G.SummonedCreatureSpawnMonsterIV_VI, G.SummonedCreatureSpawnMonsterIV_VI, G.SummonedCreatureSpawnMonsterIV_VI,
            G.SummonedCreatureSpawnMonsterVI_IX, G.SummonedCreatureSpawnMonsterVI_IX, G.SummonedCreatureSpawnMonsterVI_IX,
        };

        static void RequireTier(int tier)
        {
            if (tier < 1 || tier > WarlockRules.Dials.SummonTiers) throw new ArgumentOutOfRangeException("tier", tier, "Warlock: summon tier must be 1.." + WarlockRules.Dials.SummonTiers);
        }

        public static string IconAbility(int tier) { RequireTier(tier); return SmSingle[tier - 1]; }
        public static string FxBuff(int tier) { RequireTier(tier); return SmFx[tier - 1]; }

        public static Cell Of(Group g, int tier)
        {
            var hit = Cells.Where(c => c.Group == g && c.Tier == tier).ToArray();
            if (hit.Length != 1) throw new ArgumentOutOfRangeException("tier", g + "/" + tier, "Warlock: summon table has " + hit.Length + " cell(s) for " + g + " tier " + tier + " (expected 1)");
            return hit[0];
        }

        // 표 불변식. 어긋나면 던진다(게임 쪽 Summons.Build 가 빌드 첫머리에 부른다 — 조용히 빠진 칸이 소환 실패로 흐르지 않게).
        public static Cell[] Available(Func<string, bool> unitExists)
        {
            if (unitExists == null) throw new ArgumentNullException(nameof(unitExists));
            var cells = Cells.Where(c => {
                if (unitExists(c.UnitGuid)) return true;
                if (!c.Dlc5) throw new InvalidOperationException("Warlock: required summon unit missing " + c.UnitGuid);
                return false;
            }).ToArray();
            foreach (var group in PatronGroups.AllGroups)
                if (!cells.Any(c => c.Group == group)) throw new InvalidOperationException("Warlock: no summons available for " + group);
            return cells;
        }

        public static void Verify()
        {
            Action<bool, string> need = (ok, what) => { if (!ok) throw new InvalidOperationException("Warlock: SummonTable.Verify: " + what); };
            need(Cells.Length == ExpectedCells, "cell count " + Cells.Length + " != " + ExpectedCells);
            foreach (var g in PatronGroups.AllGroups)
                for (int t = 1; t <= WarlockRules.Dials.SummonTiers; t++)
                    need(Cells.Count(c => c.Group == g && c.Tier == t) == 1, "(" + g + ", " + t + ") is not exactly one cell");
            need(Cells.All(c => c.UnitGuid != null && c.UnitGuid.Length == 32 && c.UnitGuid.All(ch => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'))), "a unit GUID is not 32 lowercase hex");
            need(Cells.Count(c => c.Substitute) == ExpectedSubstitutes, "substitute cells " + Cells.Count(c => c.Substitute) + " != " + ExpectedSubstitutes);
            need(Cells.Count(c => c.TemplateBuffGuid != null) == ExpectedTemplateCells, "template cells " + Cells.Count(c => c.TemplateBuffGuid != null) + " != " + ExpectedTemplateCells);
            need(Cells.Where(c => c.TemplateBuffGuid != null).All(c => c.Substitute), "a template sits on a non-substitute cell");
            need(Cells.Count(c => c.Substitute && c.TemplateBuffGuid == null) == ExpectedSubstitutesWithoutTemplate, "substitute cells without template != " + ExpectedSubstitutesWithoutTemplate);
            need(Cells.Count(c => c.Dlc5) == ExpectedDlc5Cells, "Dlc5 cells " + Cells.Count(c => c.Dlc5) + " != " + ExpectedDlc5Cells);
            need(Cells.Count(c => c.IxAugment) == ExpectedIxBoostCells, "IX augment cells " + Cells.Count(c => c.IxAugment) + " != " + ExpectedIxBoostCells);
            need(Cells.Where(c => c.IxAugment).All(c => c.Tier == WarlockRules.Dials.SummonTiers && !c.Substitute && c.TemplateBuffGuid == null), "an IX augment cell is not a plain tier " + WarlockRules.Dials.SummonTiers + " cell");
            need(SmSingle.Length == WarlockRules.Dials.SummonTiers && SmFx.Length == WarlockRules.Dials.SummonTiers && SmSingle.Distinct().Count() == SmSingle.Length, "SM single / fx tables must have one distinct row per tier");
        }
    }
}
