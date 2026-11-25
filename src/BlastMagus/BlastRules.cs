using System;

namespace BlastMagus
{
    // 속성 선택 (설계 §4.2). 값마다 원소 모델 표(ElementTable)에 정확히 1행(시험 `elements: enum ↔ rows`, ElementTable.Validate).
     
     
    public enum Element { Fire, Cold, Electric, Air, Earth, Water, Telekinetic, Gravity, Wood, Negative, Positive }

    // 게임 타입 의존 없음. 블라스트 메이거스 수치 규칙의 단일 원장 (설계: docs/2026-11-06-blast-magus-design.md)
    public static class BlastRules
    {
        // 설계 §4.4: 블라스트 강타(Blast Strike) 4레벨 해금
        public const int BlastStrikeLevel = 4;
        // 설계 §4.4: 블라스트 강타 1회당 소모 자원 1
        public const int BlastStrikeCost = 1;
        // 설계 §4.4: 블라스트 강타 치명타 배수 x2
        public const int BlastStrikeCritMultiplier = 2;
        // 설계 §4.5: 복합 블라스트 20레벨
        public const int CompositeLevel = 20;
        public static bool StrikeCanCrit(int magusLevel)
        {
            if (magusLevel < 1) throw new System.ArgumentOutOfRangeException("magusLevel");
            return magusLevel < CompositeLevel;
        }
        // 설계 §4.5: 복합 블라스트 번(Burn) 비용 감소량
        public const int CompositeBurnReduction = 2;
        // 설계 §5 레벨표: 에너자이즈 주사위가 늘어나는 레벨. EnergizeDice 와 Archetype 레벨표가 모두 이 배열을 읽는다(C4).
        public static readonly int[] EnergizeGrowthLevels = { 7, 13, 19 };

        static void Require(bool ok, string name, int value)
        {
            if (!ok) throw new ArgumentOutOfRangeException(name, value, name + " out of range");
        }

        // 설계 §4.3/§5: 블라스트 랭크 = (레벨+1)/2 를 1..10 으로 (1→1, 2→1, 3→2, 19→10, 20→10)
        public static int BlastRank(int level)
        {
            Require(level >= 1, "level", level);
            return Math.Min(10, (level + 1) / 2);
        }

        // 설계 §5: 랭크가 오르는 레벨 = 홀수 1..19 (레벨표 생성용)
        public static bool RankLevel(int level)
        {
            Require(level >= 1, "level", level);
            return level % 2 == 1 && level <= 19;
        }

        // 설계 §4.2/§5: 에너자이즈 주사위 수 = 1 + (레벨 이하인 성장 레벨 개수) → 1~6: 1, 7~12: 2, 13~18: 3, 19+: 4
        public static int EnergizeDice(int level)
        {
            Require(level >= 1, "level", level);
            int dice = 1;
            foreach (int g in EnergizeGrowthLevels) if (level >= g) dice++;
            return dice;
        }

        // 설계 §4.3: 「폭발은 기본 원소의 에너지 유형을 유지한 채 복합 블라스트의 주사위(랭크 2배)를 싣는다」.
        // 본편 BlueFlame·Thunderstorm 은 DamageDice 랭크를 MultiplyByModifier step 2 로 계산한다(정찰 §3). Ice 는 본편에서 step 1 이지만
        // 설계가 폭발에는 원소 무관 ×2 로 정했으므로 여기서는 셋 다 2 를 쓴다(본편 Ice 원거리 블라스트와 다르다 — 의도된 차이).
        public const int CompositeDiceMultiplier = 2;

        // 설계 §4.3: 폭발 주사위 수(d6) = 랭크, 복합(20레벨) 보유 시 랭크 × CompositeDiceMultiplier. 랭크 < 1 은 예외.
        public static int StrikeDice(int rank, bool hasComposite)
        {
            Require(rank >= 1, "rank", rank);
            return hasComposite ? rank * CompositeDiceMultiplier : rank;
        }

        // 설계 §4.3/§11: 블라스트 보너스 = 지능 수정치 / 2. 본편 ApplyProgression 의 Div2(v/2)와 같은 C# 정수 나눗셈(0 방향 절삭): -1→0, -3→-1.
        public static int BlastBonus(int intMod) { return intMod / 2; }

         
        // 물리 = 폭발 주사위 수 + 지능(본편 물리 블라스트 ContextCalculateSharedValue = DamageDice + DamageBonus AsIs — EarthBlastAbility.jbp 직독). 주사위 < 1·정의 밖 종류는 예외.
        public static int StrikeBonus(DamageKind kind, int strikeDice, int intMod)
        {
            Require(strikeDice >= 1, "strikeDice", strikeDice);
            switch (kind)
            {
                case DamageKind.Energy: return BlastBonus(intMod);
                case DamageKind.Physical: return strikeDice + intMod;
                default: throw new ArgumentOutOfRangeException("kind", kind, "BlastMagus: StrikeBonus damage kind undefined");
            }
        }

        // 설계 §4.3-3: 20레벨 같은 원소 복합 이름. 원장 = 원소 모델 표(ElementTable 행의 CompositeName, C4). 행이 없는 값은 ElementTable.Row 가 ArgumentOutOfRangeException.
        public static string CompositeOf(Element e) { return ElementTable.Row(e).CompositeName; }
    }
}
