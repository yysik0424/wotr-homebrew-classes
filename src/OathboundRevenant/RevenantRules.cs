using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
    // [순수] 수치 원장(계획 Global Constraints 「설계 수치」 — 단일 원장 C3·C4). 게임 형식 없음(modinfo testSources).
    // 규칙(dial: single block): 모드 생산 코드의 수치 정적 필드는 Levels·Dials 안에만, 전부 static readonly(const 금지 — IL 로 접혀 소비 추적이 끊긴다),
    // 선언 바로 위 줄 = 「// 출처:」. 시험이 독립 전사본과 칸 단위로 대조한다.
    public static class RevenantRules
    {
        public static class Levels
        {
            // 출처: 설계 §5 레벨 표 1~20(팔라딘 진행표, blueprints.zip PaladinProgression LevelEntries 1..20).
            public static readonly int MinLevel = 1;
            // 출처: 설계 §5 레벨 표 1~20.
            public static readonly int MaxLevel = 20;
            // 출처: 설계 §5·§8-b 「1레벨 언데드 몸(본편 UndeadType 그대로)」.
            public static readonly int UndeadBodyLevel = 1;
            // 출처: 설계 §6 「죽음 기사의 재주 3·6·9·12·15·18」.
            public static readonly int[] TalentLevels = { 3, 6, 9, 12, 15, 18 };
            // 출처: 설계 §5 제거 「3·6·9·12·15·18 자비」(본편 PaladinProgression SelectionMercy 레벨, 부록 A-g).
            public static readonly int[] MercyRemovedLevels = { 3, 6, 9, 12, 15, 18 };
            // 출처: 설계 §5 「2 안수 특성(대체 = 아군 전용)」.
            public static readonly int HandsReplaceLevel = 2;
            // 출처: 본편 HolyChampion 레벨 20(blueprints.zip PaladinProgression Level 20, 부록 A-d 극대화 목록).
            public static readonly int HandsMaximizeLevel = 20;
            // 출처: 설계 §6 「4레벨 음에너지 채널」.
            public static readonly int ChannelLevel = 4;
            // 출처: 설계 §5·§6 「스마이트 음에너지 1d6, 7·13·19 +1d6」.
            public static readonly int[] SmiteDiceStepLevels = { 1, 7, 13, 19 };
            // 출처: 본편 PaladinProgression 5레벨 PaladinDivineBondSelection(부록 A-g) + 설계 §8-d.
            public static readonly int DivineBondLevel = 5;
        }

        public static class Dials
        {
            // 출처: 설계 §5·§6 「최대 4d6」.
            public static readonly int SmiteDiceMax = 4;
            // 출처: 설계 §6 「+1d6」(면 6).
            public static readonly int SmiteDie = 6;
            // 출처: 설계 §6 「죽음의 칼날 +1d6」.
            public static readonly int DeathBladeDice = 1;
            // 출처: 설계 §6 「음에너지 채널 안수 2회」.
            public static readonly int ChannelCostLoh = 2;
            // 출처: 설계 §6 「30 ft」.
            public static readonly int ChannelRadiusFeet = 30;
            // 출처: 설계 §6 「강화 채널 +2d6」.
            public static readonly int EmpoweredChannelDice = 2;
            // 출처: 설계 §6 「죽음의 안수 = 안수를 적에게」(안수 1회).
            public static readonly int DeathsTouchCostLoh = 1;
            // 출처: 본편 LayOnHandsOthers AbilityResourceLogic Amount 1(blueprints.zip, 부록 A-d).
            public static readonly int HandsCostLoh = 1;
            // 출처: 설계 §6 「죽음의 오라 10 ft」.
            public static readonly int DeathAuraRadiusFeet = 10;
            // 출처: 설계 §6 「공포 내성 −4」.
            public static readonly int DeathAuraFearPenalty = -4;
            // 출처: 설계 §6 「짙은 공포 오라 20 ft」.
            public static readonly int DenseFearRadiusFeet = 20;
            // 출처: 설계 §6 「공포 내성 −6」.
            public static readonly int DenseFearPenalty = -6;
            // 출처: 판정 P8 기본값 「인접 = 가장자리 5 ft」(계획 판정 필요 P8).
            public static readonly int AdjacentFeet = 5;
            // 출처: 설계 §6 「받는 치유 절반」 → 본편 HealResistance{Resistance} 곱셈 경로(부록 A-f, RuleHealDamage.OnTrigger IL_0075~0090).
            public static readonly float HealResistance = 0.5f;
            // 출처: 설계 §6 「공포의 응시 하루 3회」.
            public static readonly int GazeDaily = 3;
            // 출처: 설계 §8-c 「사자의 외침 하루 1회」.
            public static readonly int WailDaily = 1;
            // 출처: 설계 §8-c 「30 ft 적 공포」.
            public static readonly int WailRadiusFeet = 30;
            // 출처: 설계 §6 「언데드 지배 하루 1회」.
            public static readonly int CommandDaily = 1;
            // 출처: 설계 §6 「꺼지지 않는 맹세 하루 1회」.
            public static readonly int UnyieldingDaily = 1;
            // 출처: 설계 §6 「HP 1로 버팀」.
            public static readonly int UnyieldingHpLeft = 1;
            // 출처: 설계 §8-c 「원한의 반격 하루 3회」.
            public static readonly int RiposteDaily = 3;
            // 출처: 설계 §6 「무덤의 갑주 DR 5/선」.
            public static readonly int GraveArmorDr = 5;
            // 출처: 설계 §8-c 「강철 뼈 치명타·급소 피해 25% 무효」.
            public static readonly int IronBonesPercent = 25;
            // 출처: 설계 §8-c 「망령의 걸음 이동 속도 +10 ft」.
            public static readonly int GhostStrideSpeedFeet = 10;
            // 출처: 설계 §8-c 「맹세의 방패 근접 피해 중 5」.
            public static readonly int OathShieldAmount = 5;
            // 출처: 설계 §8-c 「죽음의 낙인 30 ft 안 다른 악한 적」.
            public static readonly int MarkRadiusFeet = 30;
            // 출처: 설계 §6 「생명 흡수 음성 레벨 1」.
            public static readonly int LifeDrainLevels = 1;
             
            public static readonly int LifeDrainDurationMinutes = 1;
             
            public static readonly int FearDurationDiceCount = 1;
            // 출처: 판정 P2 기본값 1d4 라운드(면 4).
            public static readonly int FearDurationDiceSides = 4;
             
            public static readonly int LastBreathRadiusFeet = 30;
            // 출처: 판정 기록 M2·R2 「채널의 2배(강화 채널 포함)」 — 마지막 숨 주사위 = 2 × 채널 주사위(ChannelDiceGetter 값).
            public static readonly int LastBreathChannelMultiplier = 2;
             
            public static readonly int SoulHarvestWindowRounds = 1;
            // 출처: 엔진 RuleCalculateAbilityParams / ContextCalculateAbilityParamsBasedOnClass = 10 + 레벨/2 + 능력 수정치(Bulwark 근거).
            public static readonly int SaveDcBase = 10;
            // 출처: 같은 식의 레벨 나눗수 2.
            public static readonly int SaveDcLevelDivisor = 2;
            // 출처: 설계 §6 「½ 팔라딘 레벨 d6」(채널·안수 주사위의 레벨 나눗수).
            public static readonly int HalfLevelDivisor = 2;
            // 출처: 설계 §6 「3·6·9·12·15·18 (6번)」.
            public static readonly int TalentPicks = 6;
            // 출처: 설계 §6 표 10 + §8-c 표 10 = 20종.
            public static readonly int TalentOptions = 20;
            // 출처: Bulwark src\BulwarkMage\TowerShield.cs:57-62 복제 깊이 6(ContextRankConfig 배열 → 참조 객체 → 필드 여유).
            public static readonly int CloneDepth = 6;
            // 출처: 계획 Global Constraints 「자원 배선」 행 — 응시·외침·지배·반격·맹세는 사용 1회당 자원 1.
            public static readonly int UsesPerActivation = 1;
            // 출처: blueprints.zip Spells/Level2/CommandUndead.jbp SpellListComponent 3개(부록 A-h) — 복제 때 지우는 개수의 되읽기 기준.
            public static readonly int CommandUndeadSpellLists = 3;
            // 출처: 설계 §8-d + 부록 A-n — 죽음의 기운 비용 = 본편 Necrotic m_EnchantmentCost 1.
            public static readonly int NecroticCost = 1;
            // 출처: 판정 P16 — 흡혈 비용 1(본편 흡혈 계열 KissOfVampire·BloodthirstVampiricTouch 비용).
            public static readonly int VampiricCost = 1;
            // 출처: 본편 GhostTouch m_EnchantmentCost 1(blueprints.zip, 부록 A-n).
            public static readonly int GhostTouchCost = 1;
            // 출처: 판정 P18 — 공포의 칼날 비용 2.
            public static readonly int DreadBladeCost = 2;
            // 출처: 판정 P16 — 흡혈 회복 = ½ 팔라딘 레벨(본편 BladeboundLifeDrinker 회복량 선례, CRC Div2).
            public static readonly int VampiricHealLevelDivisor = 2;
            // 출처: PF 능력치 수정치 = floor((점수 − 10) / 2)(엔진 ModifiableValueAttributeStat.Bonus) — 말 HP 기대값 계산(MountHpRule).
            public static readonly int AbilityScoreBaseline = 10;
            // 출처: 같은 식의 나눗수 2.
            public static readonly int AbilityModifierDivisor = 2;
            // 출처: 본편 LayOnHandsOthers 행동 트리 실측 CloneKit 깊이 16(Conditional 3겹 → ContextActionApplyBuff.DurationValue.DiceCountValue.m_CustomProperty 참조 → BlueprintGuid → Guid, blueprints.zip 2026-11-15, 시험 `exec: copy component depths`) + 여유 1(시험 CloneDepthMargin) = 17. 이전 16 은 「≈10단」 추정이라 여유 0. 넘으면 CloneKit/GraphWalk 가 던진다.
            public static readonly int ActionCloneDepth = 17;
            // 출처: 설계 §6 「추적의 맹세 이동 속도 절반」 + 소유자 결정 2026-11-17 「현재 속도의 절반」 → 본편 AddCondition{Slowed}(엔진 UnitEntityData.CalculateSpeedModifier IL_0093~00AE: Slowed → 배수 × 0.5 를 현재 속도에 곱함 — 엔진 상수와 같아야 한다, 시험 `engine: pursuit slowed halves current speed` 가 IL 과 대조).
            public static readonly float PursuitSpeedMultiplier = 0.5f;
            // 출처: 1 ft = 0.3048 m(엔진 Kingmaker.Utility.Feet 환산 — 가장자리 거리 계산).
            public static readonly float MetersPerFoot = 0.3048f;
            // 출처: 엔진 UnitEntityData.get_Corpulence IL_000D~0012 — 시각 개체가 없을 때 기본 체구 0.5 m(판정 P12 「Medium 고정」의 값, T6 Step 0). 본편 AuraOfCourageArea Size 13 대조는 어긋남(원장 「판정 필요」 P22).
            public static readonly float MediumCorpulenceMeters = 0.5f;
            // 출처: 위 값의 피트 환산(MediumCorpulenceMeters / MetersPerFoot — 선언 순서상 두 값 뒤에 둔다).
            public static readonly float MediumCorpulenceFt = MediumCorpulenceMeters / MetersPerFoot;
            // 출처: 계획 Global Constraints 「살아 있는 적」 비생물 형식 집합(Traits/Races/Common/*Type.jbp 중 언데드·구성체). 값 = Guids.Game 상수 이름.
            public static readonly IReadOnlyList<string> NonLivingTypeFacts = new[] { "UndeadType", "ConstructType" };
            // 출처: 계획 Global Constraints 「단일 원장 상수의 소비 잠금」 ② 대상 필드 이름 목록(게임 수치 저장 지점).
            public static readonly IReadOnlyList<string> GameNumberFields = new[] { "Value", "Bonus", "Size", "Amount", "BaseValue", "Resistance", "m_Radius", "m_Value", "DiceCountValue", "BonusValue", "Level", "Feet" };
        }

        static void RequireLevel(int level)
        {
            if (level < Levels.MinLevel || level > Levels.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "OathboundRevenant: paladin level must be " + Levels.MinLevel + ".." + Levels.MaxLevel);
        }

        // 스마이트 음에너지 주사위 수 = #{s ∈ SmiteDiceStepLevels : s ≤ L}(설계 §6, 최대 SmiteDiceMax).
        public static int SmiteNegativeDice(int paladinLevel)
        {
            RequireLevel(paladinLevel);
            return Levels.SmiteDiceStepLevels.Count(s => s <= paladinLevel);
        }

        // 음에너지 채널 주사위 수 = L / 2 (+ 강화 2). 4레벨 미만은 채널이 없다 → 던진다(판정 불가, C1).
        public static int ChannelDice(int paladinLevel, bool empowered)
        {
            RequireLevel(paladinLevel);
            if (paladinLevel < Levels.ChannelLevel) throw new ArgumentOutOfRangeException("paladinLevel", paladinLevel, "OathboundRevenant: negative channel needs paladin level " + Levels.ChannelLevel + "+");
            return paladinLevel / Dials.HalfLevelDivisor + (empowered ? Dials.EmpoweredChannelDice : 0);
        }

        // 안수 주사위 수 = L / 2. 2레벨 미만은 안수가 없다 → 던진다.
        public static int LohDice(int paladinLevel)
        {
            RequireLevel(paladinLevel);
            if (paladinLevel < Levels.HandsReplaceLevel) throw new ArgumentOutOfRangeException("paladinLevel", paladinLevel, "OathboundRevenant: lay on hands needs paladin level " + Levels.HandsReplaceLevel + "+");
            return paladinLevel / Dials.HalfLevelDivisor;
        }





        // 마지막 숨 주사위 수 = 2 × 채널 주사위(판정 기록 M2·R2 「채널의 2배, 강화 채널 포함」) — 18레벨 18d6, 강화 채널 보유 22d6. 채널이 없는 레벨은 ChannelDice 가 던진다.
        public static int LastBreathDice(int paladinLevel, bool empowered)
        {
            return Dials.LastBreathChannelMultiplier * ChannelDice(paladinLevel, empowered);
        }
        // 맹세의 재생 회복량 = 팔라딘 레벨(설계 §6).
        public static int RegenAmount(int paladinLevel)
        {
            RequireLevel(paladinLevel);
            return paladinLevel;
        }
    }
}
