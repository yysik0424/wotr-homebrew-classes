using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // [순수] 수치 원장 — 이 모드의 모든 설계 수치·엔진 도출값은 여기 한 곳(C3·C4). 생산 소비처(빌더·핵심)는 이 이름만 읽는다.
    // 값은 전부 static readonly(const 는 소비처 IL 에 접혀 출처 추적이 끊긴다 — 시험 `dial: static fields`·`dial: core literals` 가 고정). 줄마다 바로 위에 「// 출처:」.
    public static class ChaosRules
    {
        public static class Levels
        {
            // 출처: 엔진 클래스 레벨 범위(TestUnit.Create 1..20, src\Common\Checks\TestUnit.cs:45)
            public static readonly int MinLevel = 1;
            // 출처: 엔진 클래스 레벨 범위(TestUnit.Create 1..20, src\Common\Checks\TestUnit.cs:45)
            public static readonly int MaxLevel = 20;
            // 출처: 설계 §5 1레벨 축복 두 번째 칸 제거 + 판정 2026-11-14(신 영역 안 1개)
            public static readonly int BlessingLevel = 1;
            // 출처: 설계 §5 2레벨 열정 기반·치유 선택·신속 시전 제거
            public static readonly int FervorBaseLevel = 2;
            // 출처: 설계 §5 열정 순위 5·8·11·14·17·20 제거
            public static readonly int[] FervorRankLevels = { 5, 8, 11, 14, 17, 20 };
            // 출처: 설계 §10-b 4레벨 에너지 방출 선택 제거
            public static readonly int ChannelLevel = 4;
            // 출처: 설계 §5 성스러운 갑옷 7·10·13·16·19 제거
            public static readonly int[] SacredArmorLevels = { 7, 10, 13, 16, 19 };
            // 출처: 설계 §6 분노 단계 1(기본)·11(상급)·20(최상급)
            public static readonly int[] RageTierLevels = { 1, 11, 20 };
            // 출처: 소유자 결정 2026-11-16: 몽크 기 일격 구조(3·7·10), 10레벨은 혼돈·악, 16레벨 단계 없음 — 분노 중 자연 무기의 마법 / 냉철·은 / 혼돈·악(속성 행은 RageStrikeSpec.TierRows, 같은 순서)
            public static readonly int[] RageStrikeLevels = { 3, 7, 10 };
            // 출처: 설계 §7 「5레벨마다」 = 5·10·15·20 [작성자]
            public static readonly int[] ScaleStepLevels = { 5, 10, 15, 20 };
            // 출처: 설계 §8 10레벨부터 고통 광역
            public static readonly int PainAoeLevel = 10;
        }

        public static class Alignment
        {
            // 출처: 설계 §3 성향 CN·CE [판정] = 엔진 AlignmentMaskType ChaoticNeutral 32 | ChaoticEvil 256 (Assembly-CSharp 열거값, 부록 A-e)
            public static readonly int Mask = 288;
        }

        public static class Dials
        {
            // 출처: 설계 §6 힘·체력 +4 / 11레벨 +6 / 20레벨 +8 + 판정 2026-11-14(설계 수치)
            public static readonly int[] RageStrCon = { 4, 6, 8 };
            // 출처: 설계 §6·§10-b 의지 +2 / +3 / +4
            public static readonly int[] RageWill = { 2, 3, 4 };
            // 출처: 설계 §6 AC −2
            public static readonly int RageAc = -2;
            // 출처: StandartRageActivateableAbility WeightInGroup 1(zip) — 분노 그룹 무게(숫자는 원장에만, RageSpec 은 이름 행)
            public static readonly int RageWeightInGroup = 1;
            // 출처: 설계 §6 하루 ½ 워프리스트 레벨(내림) + 지혜
            public static readonly int RageLevelDivisor = 2;
            // 출처: 개편 설계 §3 32종(구성 12 · 10 · 5 · 5) — 표가 자라는 동안의 값은 그 커밋의 표 행 수(계획 2026-11-16 원장 C: 16 → O5 27 → O6 32)
            public static readonly int MutationOptions = 32;
            // 출처: 개편 설계 §3 구성 12 · 10 · 5 · 5 [소유자] — 표가 자라는 동안의 값은 그 커밋의 표 행 수(계획 2026-11-16 원장 C: O1 1 → O5 12). 자연 공격
            public static readonly int NaturalAttackMutations = 12;
            // 출처: 개편 설계 §3 구성 12 · 10 · 5 · 5 [소유자] — 이로운 10(추가 팔은 자연 공격으로 옮겼다, 개편 설계 §3-1)
            public static readonly int BeneficialMutations = 10;
            // 출처: 개편 설계 §3 구성 12 · 10 · 5 · 5 [소유자] — 해로운 5
            public static readonly int HarmfulMutations = 5;
            // 출처: 개편 설계 §3 구성 12 · 10 · 5 · 5 [소유자] — 양면(계획 2026-11-16 원장 C: O1 0 → O6 5, 표가 자라는 대로)
            public static readonly int TwoSidedMutations = 5;
            // 출처: 개편 설계 §3-1 「전부 1d6」 · §12-4(촉수·갈고리발톱은 1d4 복제 + 주사위만 1d6) — 자연 공격 주사위 개수
            public static readonly int NaturalAttackDiceCount = 1;
            // 출처: 개편 설계 §3-1 「전부 1d6」 · §12-4(촉수·갈고리발톱은 1d4 복제 + 주사위만 1d6) — 자연 공격 주사위 면
            public static readonly int NaturalAttackDiceSides = 6;
            // 출처: 엔진 AddAdditionalLimb.OnTurnOn @0006~0029 — 사실(표식 인스턴스) 하나에 사지 하나(정찰 2026-11-16 §1.2 · §2.1). 자연 공격 변이 한 번 = 사지 1개(개편 설계 §3-1 「자연 공격 1회」)
            public static readonly int LimbsPerNaturalAttack = 1;
            // 출처: 엔진 BlueprintPersonageLimits.GetWeaponSlotCount @003C~003D — 표에 없는 범주는 −1 을 돌려준다(= 상한 없음). UnitBody.AddAdditionalLimb @0018~001D 가 음수면 상한 검사를 건너뛴다(IL 직독 2026-11-17)
            public static readonly int LimbCapNone = -1;
            // 출처: 엔진 RuleCalculateAttackBonusWithoutTarget.OnTrigger @0154 ldc.i4.s −5(보조 자연 공격의 명중 페널티 — @0148~0152 ReduceSecondaryNaturalAttackPenalty(다중 공격)면 @0158 −2, IL 직독 2026-11-17). 설명문 전용(규칙 판정에는 쓰지 않는다 — 엔진이 정한다)
            public static readonly int SecondaryNaturalAttackPenalty = -5;
            // 출처: 설계 §7 거대화 크기 +1 단계
            public static readonly int EnlargeSizeDelta = 1;
            // 출처: 설계 §7 거대화 힘 +2
            public static readonly int EnlargeStr = 2;
            // 출처: 설계 §7 굳은 살갗 DR 2/— (5레벨마다 +1)
            public static readonly int ToughHideBase = 2;
            // 출처: 설계 §7 재생 빠른 치유 2 (5레벨마다 +1)
            public static readonly int RegenBase = 2;
            // 출처: 설계 §7 신속 속도 +10 ft
            public static readonly int SwiftSpeedFeet = 10;
            // 출처: 설계 §7 신속 반사 +2
            public static readonly int SwiftReflex = 2;
            // 출처: 설계 §7 가시 피부 1d6(주사위 개수)
            public static readonly int ThornsDiceCount = 1;
            // 출처: 설계 §7 가시 피부 1d6(주사위 면)
            public static readonly int ThornsDiceSides = 6;
            // 출처: 설계 §7 혼돈의 피 모든 내성 +2
            public static readonly int ChaosBloodSaves = 2;
            // 출처: 설계 §7 원소 껍질 저항 10
            public static readonly int ElementResist = 10;
            // ---- 개편 O7: 고통 합산식(계획 2026-11-16 원장 C — 한 곳 PainRule.AddFor). 명중당 겹 = PainStacksPerHit + PainAmplifyExtra × 증폭 순위 + PainResonanceExtra × 공명 순위 + (신경독침 무기면 PainNeuroStingExtra). ----
            // 출처: 설계 §8 명중마다 +1겹
            public static readonly int PainStacksPerHit = 1;
            // 출처: 개편 설계 §12-4 합산식 「증폭 추가 겹 1」(기존 「+2겹(기본 +1 대신)」 = 1 + 1, 원장 C — PainAmplifyStacks 2 대체)
            public static readonly int PainAmplifyExtra = 1;
            // 출처: 개편 설계 §3-4 고통 공명 「명중 시 고통 +1겹 추가」 · §12-4 합산식(원장 C)
            public static readonly int PainResonanceExtra = 1;
            // 출처: 개편 설계 §3-1 #12 신경독침 「+2겹(기본 +1 대신)」 = 1 + 1 · §12-4 합산식 「신경독침 공격이면 1」(원장 C)
            public static readonly int PainNeuroStingExtra = 1;
            // 출처: 설계 §7 늘어난 사지 간격 +5 ft
            public static readonly int LongLimbsReachFeet = 5;
            // 출처: 설계 §7 비틀린 다리 속도 −10 ft
            public static readonly int TwistedLegsSpeedFeet = -10;
            // 출처: 설계 §7 흐린 눈 명중 −2
            public static readonly int BlurredEyesAttack = -2;
            // 출처: 설계 §7 드러난 신경 AC −2
            public static readonly int ExposedNervesAc = -2;
            // 출처: 설계 §7 터지는 핏줄 매 라운드 1 피해
            public static readonly int BurstingVeinsDamage = 1;
            // 출처: 설계 §7 들끓는 정신 의지 −2
            public static readonly int SeethingMindWill = -2;
            // ---- 개편 O6: 양면 변이 5종(계획 2026-11-16 원장 C). 전부 순위형 표식의 부품 값 — 엔진이 순위를 곱한다(정찰 §1.2). ----
            // 출처: 개편 설계 §3-4 피의 갈증 근접 피해 +2
            public static readonly int BloodthirstDamage = 2;
            // 출처: 개편 설계 §3-4 피의 갈증 AC −2
            public static readonly int BloodthirstAc = -2;
            // 피의 갈증 부품의 곱수: 피해 = Value × AttackBonus × 순위(AttackBonus 는 이름과 달리 명중 보너스가 아니다). 1 = 값 그대로.
            // 출처: 엔진 WeaponAttackTypeDamageBonus.OnEventAboutToTrigger @0000~001C(IL 직독 2026-11-17) · 본편 CrushingChargeFeature 의 같은 부품 AttackBonus 1(blueprints.zip 직독 2026-11-17) — 시험 `exec: two-sided engine`
            public static readonly int WeaponDamageBonusMultiplier = 1;
            // 출처: 개편 설계 §3-4 비대한 근육 힘 +4
            public static readonly int BloatedMuscleStr = 4;
            // 출처: 개편 설계 §3-4 비대한 근육 민첩 −4
            public static readonly int BloatedMuscleDex = -4;
            // 출처: 개편 설계 §3-4 돌 껍질 자연 방어 +3
            public static readonly int StoneShellNaturalArmor = 3;
            // 출처: 개편 설계 §3-4 돌 껍질 속도 −10 ft
            public static readonly int StoneShellSpeedFeet = -10;
            // 출처: 개편 설계 §12-3 [소유자] 과민한 신경 반사 내성 +4(§3-4 의 우선권 +4 폐기)
            public static readonly int RawNervesReflex = 4;
            // 출처: 개편 설계 §12-3 [소유자] 과민한 신경 회피 AC +2
            public static readonly int RawNervesDodgeAc = 2;
            // 출처: 개편 설계 §12-3 [소유자] 과민한 신경 의지 내성 −2
            public static readonly int RawNervesWill = -2;
            // 출처: 개편 설계 §3-4 고통 공명 자신의 모든 내성 −1(고통 +1겹 쪽은 O7 의 PainResonanceExtra)
            public static readonly int PainResonanceSaves = -1;
            // 출처: 설계 §8 겹당 명중·내성·기술 −1
            public static readonly int PainPerStack = -1;
            // 출처: 설계 §8 마지막 겹 뒤 3라운드
            public static readonly int PainDurationRounds = 3;
            // 출처: 개편 설계 §6-b [소유자 2026-11-16] 벌점은 5겹까지(1~5겹 = −1~−5, 6~8겹 = −5 그대로) — 원장 C. 벌점 버프의 Ranks.
            public static readonly int PainPenaltyCap = 5;
            // 출처: 개편 설계 §6-b [소유자 2026-11-16] 8겹 이상이 되는 순간 혼란 내성 · 정신 면역은 8겹에서 멈춤 — 원장 C(PainThreshold 5 대체). 고통 계수 버프의 Ranks(총 겹 0..이 값, 소유자 2026-11-17 「계수 버프 하나로 재구성」).
            public static readonly int PainConfusionThreshold = 8;
            // 출처: 커밋 bbfb86d Pain.OverflowRanks = 8 − 5 — 계수 이전 개발판의 숨김 초과 버프(Guids.PainOverflowBuff) Ranks. 옛 세이브의 그 인스턴스를 읽어 계수로 옮길 때만 쓴다(PainRule.LegacyTotal). 지금 다이얼이 바뀌어도 옛 모양은 그대로다.
            public static readonly int PainLegacyOverflowRanks = 3;
            // 출처: 설계 §8 의지 DC 10 + ½ 워프리스트 레벨 + 지혜
            public static readonly int PainDcBase = 10;
            // 출처: 설계 §8 DC 의 ½ 워프리스트 레벨
            public static readonly int PainDcLevelDivisor = 2;
            // 출처: 설계 §8 혼란 1d4라운드(주사위 개수)
            public static readonly int ConfusionDiceCount = 1;
            // 출처: 설계 §8 혼란 1d4라운드(주사위 면)
            public static readonly int ConfusionDiceSides = 4;
            // 출처: 설계 §8 광역 반경 15 ft
            public static readonly int PainAoeFeet = 15;
             
            public static readonly int HorrorRadiusFeet = 5;
             
            public static readonly string[] ShellElements = { "Fire", "Cold", "Acid", "Electricity" };
            // 출처: 엔진 AutoMetamagic.ShouldApplyTo @0160~016C(IL 직독 2026-11-17, md5 앞 12 748b3a143b60) — MaxSpellLevel ≤ 0 이면 상한 검사를 건너뛴다(= 모든 주문 레벨, 정찰 2026-11-16 §5.1). 신속 시전 상한 표에 이 값 이하가 섞이면 던진다(계획 「조용한 가정 닫기」 A15)
            public static readonly int QuickenEngineNoCap = 0;
        }

        // 하루 분노 자원의 엔진 모양(열정 자원 WarpriestFervorResource 와 같은 IncreasedByLevelStartPlusDivStep + 하한). 부록 A-b, GetMaxAmount @02CD~0343 · @0363~037C.
        public static class RageResourceSpec
        {
            // 출처: 엔진 WarpriestFervorResource m_MaxAmount.StartingLevel 2(zip) — ½ 레벨과 같은 값을 내는 시작점(시험 exec: resource from spec)
            public static readonly int StartingLevel = 2;
            // 출처: 엔진 WarpriestFervorResource m_MaxAmount.StartingIncrease 1(zip)
            public static readonly int StartingIncrease = 1;
            // 출처: 엔진 WarpriestFervorResource m_MaxAmount.LevelStep 2(zip)
            public static readonly int LevelStep = 2;
            // 출처: 엔진 WarpriestFervorResource m_MaxAmount.PerStepIncrease 1(zip)
            public static readonly int PerStepIncrease = 1;
            // 출처: 설계 §6 하루 max(1, …) 의 하한 1 [작성자] → BlueprintAbilityResource.m_Min(GetMaxAmount @0363~037C Math.Max(m_Min, …))
            public static readonly int Min = 1;
        }

        public struct CountBand
        {
            public readonly int MinLevel, MaxLevel, MinCount, MaxCount;
            public CountBand(int minLevel, int maxLevel, int minCount, int maxCount) { MinLevel = minLevel; MaxLevel = maxLevel; MinCount = minCount; MaxCount = maxCount; }
            public override string ToString() { return "L" + MinLevel + "-" + MaxLevel + ":" + MinCount + "~" + MaxCount; }
        }

        // 출처: 개편 설계 §4 개수 표 [소유자: 최대 8 / 작성자: 구간별 값] · §12 머리(1~3 · 2~5 · 3~6 · 4~8)
        public static readonly CountBand[] CountBands = { new CountBand(1, 5, 1, 3), new CountBand(6, 10, 2, 5), new CountBand(11, 15, 3, 6), new CountBand(16, 20, 4, 8) };

        // 출처: 개수 표의 최대값 — 순위형 표식의 Ranks 와 고통 합산식 최대값이 여기서 나온다(정찰 2026-11-16 §7.2). 파생 속성(숫자를 따로 적지 않는다).
        public static int MaxDraws { get { return CountBands.Max(b => b.MaxCount); } }

        // 분노 중 신속 시전의 주문 레벨 상한 한 구간(개편 계획 2026-11-16 Task O8): 워프리스트 레벨 MinLevel..MaxLevel 에서 신속으로 나가는 주문의 최고 레벨 = MaxSpellLevel(엔진 AutoMetamagic.MaxSpellLevel).
        public struct QuickenBand
        {
            public readonly int MinLevel, MaxLevel, MaxSpellLevel;
            public QuickenBand(int minLevel, int maxLevel, int maxSpellLevel) { MinLevel = minLevel; MaxLevel = maxLevel; MaxSpellLevel = maxSpellLevel; }
            public override string ToString() { return "L" + MinLevel + "-" + MaxLevel + ":" + MaxSpellLevel; }
        }

        // 출처: 개편 설계 §6 표 [소유자 2026-11-16: 배우는 것보다 한 단계 늦게] · §12-2(표 그대로, 판정 기준 = 슬롯 레벨 — 엔진 AbilityData.SpellLevel = 주문서 레벨 + 메타매직 비용) — 계획 2026-11-16 원장 C. 배우는 레벨 1/4/7/10/13/16 = WarpriestSpellSlotsTable c73a394e…(시험 `jbp: quicken caps one step behind slots`)
        public static readonly QuickenBand[] QuickenCaps = { new QuickenBand(1, 6, 1), new QuickenBand(7, 9, 2), new QuickenBand(10, 12, 3), new QuickenBand(13, 15, 4), new QuickenBand(16, 18, 5), new QuickenBand(19, 20, 6) };

        // 그 워프리스트 레벨의 신속 시전 상한. 레벨 1..20 밖이면 ArgumentOutOfRangeException, 그 레벨이 정확히 한 구간에 있지 않거나 상한이 엔진의 「상한 없음」(QuickenEngineNoCap 이하)이면 던진다.
        public static int QuickenCapFor(int level) { return QuickenCapIn(QuickenCaps, level); }

        // 표를 인자로 받는 같은 판정(시험이 겹침·빈틈 가짜 표로 밟는다 — 생산 경로와 같은 본문).
        internal static int QuickenCapIn(IReadOnlyList<QuickenBand> table, int level)
        {
            if (table == null) throw new ArgumentNullException("table");
            RequireLevel(level);
            var hit = table.Where(b => b.MinLevel <= level && level <= b.MaxLevel).ToArray();
            if (hit.Length != 1) throw new InvalidOperationException("ChaosWarrior: level " + level + " is in " + hit.Length + " quicken cap bands (need exactly 1) [" + string.Join(", ", table.Select(b => b.ToString()).ToArray()) + "]");
            if (hit[0].MaxSpellLevel <= Dials.QuickenEngineNoCap) throw new InvalidOperationException("ChaosWarrior: quicken cap band " + hit[0] + " has MaxSpellLevel " + hit[0].MaxSpellLevel + " (the engine reads <= " + Dials.QuickenEngineNoCap + " as 'no cap' — every spell level would open)");
            return hit[0].MaxSpellLevel;
        }

        // 표 전체(빌드 시작 때 QuickenSpec.Validate 가 부른다): 1..20 의 모든 레벨이 정확히 한 구간 · 상한 전부 QuickenEngineNoCap 초과 · 구간 안 최소 ≤ 최대 · 상한이 레벨을 따라 줄지 않는다. 어기면 던진다.
        internal static void ValidateQuickenCaps(IReadOnlyList<QuickenBand> table)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (table.Count == 0) throw new InvalidOperationException("ChaosWarrior: quicken cap table is empty");
            foreach (var b in table) if (b.MinLevel > b.MaxLevel) throw new InvalidOperationException("ChaosWarrior: quicken cap band " + b + " has MinLevel > MaxLevel");
            int prev = Dials.QuickenEngineNoCap;
            for (int l = Levels.MinLevel; l <= Levels.MaxLevel; l++)
            {
                int cap = QuickenCapIn(table, l);
                if (cap < prev) throw new InvalidOperationException("ChaosWarrior: quicken cap drops from " + prev + " to " + cap + " at level " + l);
                prev = cap;
            }
            foreach (var b in table)
                if (b.MinLevel < Levels.MinLevel || b.MaxLevel > Levels.MaxLevel) throw new InvalidOperationException("ChaosWarrior: quicken cap band " + b + " reaches outside levels " + Levels.MinLevel + ".." + Levels.MaxLevel);
        }

        // 변이 분류(개편 설계 §3: 자연 공격 12 · 이로운 10 · 해로운 5 · 양면 5).
        public enum MutationKind { NaturalAttack, Beneficial, Harmful, TwoSided }

        public struct MutationSpec
        {
            public readonly string Id;
            public readonly MutationKind Kind;
            public MutationSpec(string id, MutationKind kind) { Id = id; Kind = kind; }
        }

        // 출처: 개편 설계 §3(분류) · 설계 §7 16종 · 개편 설계 §3-1 자연 공격 12종(계획 2026-11-16 원장 A #16~26) · §3-4·§12-3 양면 5종(원장 A #27~31). 순서 고정 — 색인 0~15 불변(색인이 CheckHooks.ForcePicks·Guids.Mark* 순서, 계획 2026-11-16 Global Constraints 「색인 불변」), 새 변이는 뒤에 붙인다(16~26 · 27~31). 추가 팔은 자연 공격(개편 설계 §3-1).
        public static readonly MutationSpec[] Mutations =
        {
            new MutationSpec("Enlarge", MutationKind.Beneficial), new MutationSpec("ToughHide", MutationKind.Beneficial), new MutationSpec("Regeneration", MutationKind.Beneficial), new MutationSpec("Swiftness", MutationKind.Beneficial),
            new MutationSpec("ThornSkin", MutationKind.Beneficial), new MutationSpec("ChaosBlood", MutationKind.Beneficial), new MutationSpec("ElementalShell", MutationKind.Beneficial), new MutationSpec("PainAmplify", MutationKind.Beneficial),
            new MutationSpec("ExtraArm", MutationKind.NaturalAttack), new MutationSpec("HorrificForm", MutationKind.Beneficial), new MutationSpec("LongLimbs", MutationKind.Beneficial),
            new MutationSpec("TwistedLegs", MutationKind.Harmful), new MutationSpec("BlurredEyes", MutationKind.Harmful), new MutationSpec("ExposedNerves", MutationKind.Harmful), new MutationSpec("BurstingVeins", MutationKind.Harmful),
            new MutationSpec("SeethingMind", MutationKind.Harmful),
            new MutationSpec("Fangs", MutationKind.NaturalAttack), new MutationSpec("Claws", MutationKind.NaturalAttack), new MutationSpec("Horns", MutationKind.NaturalAttack), new MutationSpec("Tail", MutationKind.NaturalAttack),
            new MutationSpec("Tentacle", MutationKind.NaturalAttack), new MutationSpec("Stinger", MutationKind.NaturalAttack), new MutationSpec("Wings", MutationKind.NaturalAttack), new MutationSpec("Hooves", MutationKind.NaturalAttack),
            new MutationSpec("Talons", MutationKind.NaturalAttack), new MutationSpec("Pincer", MutationKind.NaturalAttack), new MutationSpec("NeuroStinger", MutationKind.NaturalAttack),
            new MutationSpec("Bloodthirst", MutationKind.TwoSided), new MutationSpec("BloatedMuscle", MutationKind.TwoSided), new MutationSpec("StoneShell", MutationKind.TwoSided),
            new MutationSpec("RawNerves", MutationKind.TwoSided), new MutationSpec("PainResonance", MutationKind.TwoSided),
        };

        // 엔진의 추가 사지 범주 상한(개편 설계 §12-1). 「판정에는 쓰지 않는다」 — 판정은 실행 때 엔진 값(BlueprintPersonageLimits.GetWeaponSlotCount, 어댑터 GameMutationWorld.LimbCap)으로 한다.
        // 이 표는 시험 대조 · 설명문 · README 용 전사본이고, 빌드 때 엔진 값과 다르면 정보 줄을 남긴다(NaturalWeapons.Build — 다른 모드가 상한을 바꾼 경우. 판정은 엔진 값을 따르므로 오류가 아니다).
        public struct LimbCapRow
        {
            public readonly string Category;
            public readonly int Cap;
            public LimbCapRow(string category, int cap) { Category = category; Cap = cap; }
        }

        // 출처: 엔진 BlueprintPersonageLimits 2a609e8ce9844f98ae11097bef7be8bb m_DopLimbMaxCount(blueprints.zip Root/SystemMechanics/BlueprintPersonageLimits.jbp 직독 2026-11-17, 정찰 2026-11-16 §2.1) — Bite 1 · Claw 2(집게도 Claw 범주) · Gore 1 · Tail 1
        public static readonly LimbCapRow[] LimbCapsExpected = { new LimbCapRow("Bite", 1), new LimbCapRow("Claw", 2), new LimbCapRow("Gore", 1), new LimbCapRow("Tail", 1) };

        // 전사 표의 값(없는 범주 = Dials.LimbCapNone). 시험 · 빌드 때의 정보 줄 비교용 — 다시 뽑기 판정은 이 함수를 부르지 않는다(시험 `wiring: limb cap judged by engine`).
        public static int LimbCapExpected(string category)
        {
            if (string.IsNullOrEmpty(category)) throw new ArgumentException("ChaosWarrior: empty limb category", "category");
            var hit = LimbCapsExpected.Where(r => r.Category == category).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("ChaosWarrior: limb category '" + category + "' is on " + hit.Length + " expected-cap rows");
            return hit.Length == 1 ? hit[0].Cap : Dials.LimbCapNone;
        }

        // 분류별 개수(표에서 센다).
        public static int KindCount(MutationKind k) { return Mutations.Count(m => m.Kind == k); }

        // 분류별 상수(Dials) ↔ 표. 표의 종류별 수 == 상수, 표 길이 == MutationOptions. 어긋나면 던진다(MutationSpecs.Validate 가 빌드 시작 때 부른다).
        public static void ValidateKinds()
        {
            var want = new[]
            {
                Tuple.Create(MutationKind.NaturalAttack, Dials.NaturalAttackMutations), Tuple.Create(MutationKind.Beneficial, Dials.BeneficialMutations),
                Tuple.Create(MutationKind.Harmful, Dials.HarmfulMutations), Tuple.Create(MutationKind.TwoSided, Dials.TwoSidedMutations),
            };
            var kinds = (MutationKind[])Enum.GetValues(typeof(MutationKind));
            if (kinds.Length != want.Length || kinds.Any(k => want.Count(x => x.Item1 == k) != 1))
                throw new InvalidOperationException("ChaosWarrior: MutationKind has " + kinds.Length + " members but the kind-count ledger covers " + want.Length);
            foreach (var x in want)
                if (KindCount(x.Item1) != x.Item2)
                    throw new InvalidOperationException("ChaosWarrior: mutation table has " + KindCount(x.Item1) + " " + x.Item1 + " rows, Dials says " + x.Item2);
            if (Mutations.Length != Dials.MutationOptions)
                throw new InvalidOperationException("ChaosWarrior: mutation table has " + Mutations.Length + " rows, expected " + Dials.MutationOptions);
            if (Mutations.Select(m => m.Id).Distinct().Count() != Mutations.Length)
                throw new InvalidOperationException("ChaosWarrior: mutation table has duplicate ids");
        }

        // ---------------- 함수(레벨 1..20 밖 → ArgumentOutOfRangeException) ----------------

        internal static void RequireLevel(int level)
        {
            if (level < Levels.MinLevel || level > Levels.MaxLevel)
                throw new ArgumentOutOfRangeException("level", level, "ChaosWarrior: warpriest level must be " + Levels.MinLevel + ".." + Levels.MaxLevel);
        }

        // 분노 단계 색인: RageTierLevels 중 L 이하인 마지막 것.
        public static int RageTierIndex(int level)
        {
            RequireLevel(level);
            int idx = -1;
            for (int i = 0; i < Levels.RageTierLevels.Length; i++) if (Levels.RageTierLevels[i] <= level) idx = i;
            if (idx < 0) throw new InvalidOperationException("ChaosWarrior: no rage tier reaches level " + level + " (RageTierLevels must start at the minimum level)");
            return idx;
        }

        public static int RageStrCon(int level) { return Dials.RageStrCon[RageTierIndex(level)]; }
        public static int RageWill(int level) { return Dials.RageWill[RageTierIndex(level)]; }

        // 설계 식: max(1, ½ 워프리스트 레벨(내림) + 지혜 수정치).
        public static int RageDailyMax(int level, int wisMod)
        {
            RequireLevel(level);
            return Math.Max(RageResourceSpec.Min, level / Dials.RageLevelDivisor + wisMod);
        }

        // 엔진 식(BlueprintAbilityResource.GetMaxAmount @02CD~0343 IncreasedByLevelStartPlusDivStep — PerStepIncrease × (L − StartingLevel) / LevelStep 순서 그대로 — 그리고 @0363~037C Math.Max(m_Min, …)).
        // 지혜는 IncreasedByStat(@017D~019E BonusWithoutTemp)로 더해진다. 시험이 RageDailyMax 와 180칸 대조한다.
        public static int ResourceFromSpec(int level, int wisMod)
        {
            RequireLevel(level);
            int byLevel = level < RageResourceSpec.StartingLevel ? 0
                : RageResourceSpec.StartingIncrease + RageResourceSpec.PerStepIncrease * (level - RageResourceSpec.StartingLevel) / RageResourceSpec.LevelStep;
            return Math.Max(RageResourceSpec.Min, byLevel + wisMod);
        }

        public static CountBand BandFor(int level)
        {
            RequireLevel(level);
            var hit = CountBands.Where(b => b.MinLevel <= level && level <= b.MaxLevel).ToArray();
            if (hit.Length != 1) throw new InvalidOperationException("ChaosWarrior: level " + level + " is in " + hit.Length + " count bands (need exactly 1)");
            return hit[0];
        }

        // 「5레벨마다 +1」: base + (ScaleStepLevels 중 L 이하 개수).
        public static int ScaledByFive(int baseValue, int level)
        {
            RequireLevel(level);
            return baseValue + Levels.ScaleStepLevels.Count(s => s <= level);
        }

        public static int PainDc(int level, int wisMod)
        {
            RequireLevel(level);
            return Dials.PainDcBase + level / Dials.PainDcLevelDivisor + wisMod;
        }

         
        public static (int BaseValue, int ProgressionValue)[] ProgressionRows(Func<int, int> f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var rows = new List<(int, int)>();
            for (int l = Levels.MinLevel; l <= Levels.MaxLevel; l++) rows.Add((l, f(l)));
            return rows.ToArray();
        }
    }
}
