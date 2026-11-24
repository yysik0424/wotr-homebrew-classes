using System;

namespace Warlock
{
    public enum SummonDurationKind { Rounds, UntilCombatEnd }

    public struct SummonDuration
    {
        public readonly SummonDurationKind Kind;
        readonly int rounds;
        public SummonDuration(SummonDurationKind kind, int rounds) { Kind = kind; this.rounds = rounds; }
        // Kind == Rounds 일 때만 뜻이 있다 — UntilCombatEnd 에서 읽으면 던진다(0 이 「즉시 소멸」로 흘러가지 않게).
        public int Rounds
        {
            get
            {
                if (Kind != SummonDurationKind.Rounds) throw new InvalidOperationException("Warlock: SummonDuration.Rounds read on " + Kind);
                return rounds;
            }
        }
    }

    // 소환 직후 지속 조정(T5 WarlockSummonComponent 가 RuleSummonUnit.OnEventDidTrigger 에서 따른다). None = 액션의 기본 지속(위치 레벨 라운드) 그대로(정당한 무동작) ·
    // ExtendRounds = SummonedUnitBuff.IncreaseDuration(ExtraRounds 라운드) · Permanent = MakePermanent + 대사자 표식(전투 종료 처리기가 제거).
    public enum SummonShapeKind { None, ExtendRounds, Permanent }

    public struct SummonShape
    {
        public readonly SummonShapeKind Kind;
        public readonly int ExtraRounds;
        public SummonShape(SummonShapeKind kind, int extra) { Kind = kind; ExtraRounds = extra; }
        public override string ToString() { return Kind + ":" + ExtraRounds; }
    }

     
    public enum SummonPhase { Active, Dismissing, Gone, Invalid }

     
    public static class SummonRule
    {
        static void RequireLevel(int level)
        {
            if (level < WarlockRules.MinLevel || level > WarlockRules.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "Warlock: level must be " + WarlockRules.MinLevel + ".." + WarlockRules.MaxLevel);
        }

        // 설계 §4.6 지속 = 위치 레벨 라운드(× SummonRoundsPerLevel), L16 ×2(Extend16Feature), L20 대사자(Capstone20Feature)는 9단계만 전투 종료까지.
        public static SummonDuration Duration(int level, int tier, bool hasExtend, bool hasCapstone)
        {
            RequireLevel(level);
            if (tier < 1 || tier > WarlockRules.Dials.SummonTiers) throw new ArgumentOutOfRangeException("tier", tier, "Warlock: summon tier must be 1.." + WarlockRules.Dials.SummonTiers);
            if (hasCapstone && tier == WarlockRules.Dials.SummonTiers) return new SummonDuration(SummonDurationKind.UntilCombatEnd, 0);
            return new SummonDuration(SummonDurationKind.Rounds, level * WarlockRules.Dials.SummonRoundsPerLevel * (hasExtend ? WarlockRules.Dials.ExtendMultiplier : 1));
        }

        // 소환 액션이 이미 주는 기본 지속(level × SummonRoundsPerLevel) 위에 RuleSummonUnit.OnEventDidTrigger 에서 IncreaseDuration 할 추가 라운드.
        public static int ExtraRounds(int level, bool hasExtend)
        {
            RequireLevel(level);
            int full = level * WarlockRules.Dials.SummonRoundsPerLevel * (hasExtend ? WarlockRules.Dials.ExtendMultiplier : 1);
            return full - level * WarlockRules.Dials.SummonRoundsPerLevel;
        }

        // Duration → 조정 명령. 액션의 기본 지속 = level × SummonRoundsPerLevel(KitSpec.SummonRoundsRank) 이므로 Rounds 는 그 차이만 더한다.
        public static SummonShape Shape(int level, int tier, bool hasExtend, bool hasCapstone)
        {
            var d = Duration(level, tier, hasExtend, hasCapstone);
            if (d.Kind == SummonDurationKind.UntilCombatEnd) return new SummonShape(SummonShapeKind.Permanent, 0);
            int extra = ExtraRounds(level, hasExtend);
            if (d.Rounds != level * WarlockRules.Dials.SummonRoundsPerLevel + extra) throw new InvalidOperationException("Warlock: summon duration " + d.Rounds + " != base + extra (" + level + " + " + extra + ")");
            return extra == 0 ? new SummonShape(SummonShapeKind.None, 0) : new SummonShape(SummonShapeKind.ExtendRounds, extra);
        }

        // 최고 소환 단계(T5 Summons.MaxTier): 주문서의 최고 주문 레벨 → min(9, 그 값). 주문서 없음(null)·0 이하 = 판정 불가 → null(HeatCostRule Unknown · HeatRule HoldAtMax 가 ERROR 로 드러낸다).
        public static int? MaxTier(int? maxSpellLevel)
        {
            if (!maxSpellLevel.HasValue || maxSpellLevel.Value <= 0) return null;
            return WarlockRules.MaxTierAt(maxSpellLevel.Value);
        }

        // 파괴됐거나 풀에 없음 → Gone · SummonedUnitBuff 있음 → Active(DismemberedUnitBuff 도 있으면 모순 → Invalid) · Dismembered 만 → Dismissing(정상 소멸 중) ·
        // 둘 다 없는데 풀에 있음 → Invalid(게임 쪽 ERROR 1회).
        public static SummonPhase Phase(bool hasSummonedBuff, bool hasDismembered, bool destroyed, bool inPool)
        {
            if (destroyed || !inPool) return SummonPhase.Gone;
            if (hasSummonedBuff) return hasDismembered ? SummonPhase.Invalid : SummonPhase.Active;
            if (hasDismembered) return SummonPhase.Dismissing;
            return SummonPhase.Invalid;
        }

        // 항목 9 IX 강화의 「건강」 몫을 받을 능력치(소유자 결정 2026-11-17: 언데드는 매력 +4).
        // 엔진 사실(Assembly-CSharp, dnlib 2026-11-17):
        //   ModifiableValueAttributeStat.AddModifier @0001~0009 — get_Enabled(= !Disabled, @0001~000C) 가 거짓이면 수정치를 넣지 않고 돌려준다 → 비활성 건강 +4 는 버려진다.
        //   UndeadHealth.OnTurnOn @000B~0011 HitPoints.BaseStat = Charisma(HitPointsBaseStat 1) · @0021~0026 Constitution.RetainDisabled.
        //   ModifiableValueHitPoints.CalculateBonus @0008 switch — Constitution(0) @001B~002D 건강 보너스 × HD · Charisma(1) @002E~0040 매력 보너스 × HD · Size(2) @0041~0057 크기 보너스.
        // 판정 기준 = 「건강 능력치가 비활성인가」(종족 이름이 아니다). 건강 활성 → 건강. 비활성 ∧ 체력 기준이 매력 → 매력(언데드 HP 경로).
        // 비활성인데 체력 기준이 매력이 아님(예: 구조물 — 크기 기준)·판독 불가(null) → 던진다(매력 +4 가 체력에 닿지 않는 경우를 조용히 넘기지 않는다).
        public const string IxHealthStatConstitution = "Constitution";
        public const string IxHealthStatCharisma = "Charisma";
        public static string IxHealthStat(bool? constitutionEnabled, string hitPointsBaseStat)
        {
            if (constitutionEnabled == null) throw new InvalidOperationException("Warlock: IX boost cannot tell whether the summon's Constitution is enabled (stat unreadable)");
            if (constitutionEnabled.Value) return IxHealthStatConstitution;
            if (hitPointsBaseStat == null) throw new InvalidOperationException("Warlock: IX boost: Constitution is disabled and the hit point base stat is unreadable");
            if (hitPointsBaseStat == IxHealthStatCharisma) return IxHealthStatCharisma;
            throw new InvalidOperationException("Warlock: IX boost: Constitution is disabled but hit points are based on " + hitPointsBaseStat + ", not Charisma (not modelled — a Charisma bonus would not reach its hit points)");
        }
    }
}
