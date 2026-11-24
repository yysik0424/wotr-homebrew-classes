using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
     
    // NoSpirit(계획 T6) = 주술사에게 영병 유닛이 없음(생성 전·해산) — 「죽음」과 섞지 않는 별도 사유(C1).
    public enum ActivateBlock { None, PetDead, PetHidden, TooFar, NoUses, NoSpirit }

    // 판정 시점(2026-11-16 수리). Cast = 시전 가능 판정(엔진이 매 프레임 묻는 시전 제한) — 남은 횟수를 본다.
    // Effect = 실행 시점 재판정(능력 효과가 돌 때) — 횟수를 보지 않는다: 엔진은 효과보다 먼저 비용을 치른다
    // (UnitUseAbility.OnAction @01F9 RuleCastSpell 발동 = 실행 과정을 줄에 세우기만 함 → @0223 AbilityData.Spend, 효과는 다음 AbilityExecutionController.Tick —
     
    // 강신·융합·스피릿 배러지 세 능력이 이 한 인자로 두 시점을 가른다(판정식은 하나 — CanActivate).
    public enum CheckMoment { Cast, Effect }

    public sealed class ActivateCheck
    {
        public readonly bool Ok;
        public readonly ActivateBlock Reason;
        public ActivateCheck(bool ok, ActivateBlock reason) { Ok = ok; Reason = reason; }
    }

    public sealed class CopyResult
    {
        public string[] Copy;     // 주술사 무기에 얹을 GUID (비친 집합 순서: 강화 → 특수 → 고유)
        public string[] Skipped;  // 주술사 무기에 이미 있어 건너뜀
    }

    // 엔진 자원 식 매개변수(PossessionRules.ResourceFormula). 이름 = BlueprintAbilityResource.Amount 필드 이름.
    public sealed class StartPlusDivStep
    {
        public readonly int StartingLevel, StartingIncrease, LevelStep, PerStepIncrease;
        public StartPlusDivStep(int startingLevel, int startingIncrease, int levelStep, int perStepIncrease)
        { StartingLevel = startingLevel; StartingIncrease = startingIncrease; LevelStep = levelStep; PerStepIncrease = perStepIncrease; }
    }

    public static class PossessionRules
    {
        public static PossessionTier[] Tiers { get { return SpiritRules.Dials.PossessionTiers; } }

        // 그 주술사 레벨의 하루 횟수 = 레벨 이하 마지막 단계의 횟수, 첫 단계 전이면 0.
        public static int MaxUses(int shamanLevel)
        {
            SpiritRules.Budget(shamanLevel); // 범위 검사(1..20, 밖이면 throw)
            var t = Tiers.Where(x => x.Level <= shamanLevel).OrderBy(x => x.Level).LastOrDefault();
            return t == null ? 0 : t.UsesPerDay;
        }

        // 단계 번호(1..Tiers.Length) → 지속 라운드.
        public static int Rounds(int tier)
        {
            if (tier < 1 || tier > Tiers.Length) throw new ArgumentOutOfRangeException("tier", tier, "PossessionRules: tier must be 1.." + Tiers.Length);
            return Tiers[tier - 1].Rounds;
        }

        // distanceFeet = null 은 「모름」(영병이 죽었거나 숨어 거리를 잴 수 없음) — 거리를 읽어야 하는 지점까지 오면 던진다(가깝다로 읽지 않는다).
        public static ActivateCheck CanActivate(bool petAlive, bool petHidden, double? distanceFeet, int usesLeft) { return CanActivate(CheckMoment.Cast, petAlive, petHidden, distanceFeet, usesLeft); }

        // moment == Effect 면 마지막 줄(횟수)만 건너뛴다. 나머지 조건(죽음·숨음·거리)은 두 시점 모두 본다. usesLeft 는 두 시점 모두 0 이상이어야 한다(음수 = 관측 오류).
        public static ActivateCheck CanActivate(CheckMoment moment, bool petAlive, bool petHidden, double? distanceFeet, int usesLeft)
        {
            if (!Enum.IsDefined(typeof(CheckMoment), moment)) throw new ArgumentException("PossessionRules: undefined check moment " + (int)moment);
            if (distanceFeet.HasValue && (double.IsNaN(distanceFeet.Value) || distanceFeet.Value < 0)) throw new ArgumentOutOfRangeException("distanceFeet", "PossessionRules: distance must be >= 0 (unknown distance is not 'near')");
            if (usesLeft < 0) throw new ArgumentOutOfRangeException("usesLeft");
            if (!petAlive) return new ActivateCheck(false, ActivateBlock.PetDead);
            if (petHidden) return new ActivateCheck(false, ActivateBlock.PetHidden);
            if (!distanceFeet.HasValue) throw new ArgumentNullException("distanceFeet", "PossessionRules: distance unknown for a live, visible spirit");
            if (distanceFeet.Value > SpiritRules.Dials.PossessionRangeFeet) return new ActivateCheck(false, ActivateBlock.TooFar);
            if (moment == CheckMoment.Cast && usesLeft == 0) return new ActivateCheck(false, ActivateBlock.NoUses);
            return new ActivateCheck(true, ActivateBlock.None);
        }

        // 게임 쪽 관측 → 판정(계획 T6 시전 제한). 영병 유닛이 없으면 NoSpirit, 있으면 CanActivate 그대로.
        public static ActivateCheck Observe(bool spiritPresent, bool spiritAlive, bool spiritHidden, double? distanceFeet, int usesLeft) { return Observe(CheckMoment.Cast, spiritPresent, spiritAlive, spiritHidden, distanceFeet, usesLeft); }

        // 시점을 받는 판정(게임 쪽 세 능력이 전부 이것을 부른다 — 시험 `wiring: moment`).
        public static ActivateCheck Observe(CheckMoment moment, bool spiritPresent, bool spiritAlive, bool spiritHidden, double? distanceFeet, int usesLeft)
        {
            if (!Enum.IsDefined(typeof(CheckMoment), moment)) throw new ArgumentException("PossessionRules: undefined check moment " + (int)moment);
            if (usesLeft < 0) throw new ArgumentOutOfRangeException("usesLeft");
            if (!spiritPresent) return new ActivateCheck(false, ActivateBlock.NoSpirit);
            return CanActivate(moment, spiritAlive, spiritHidden, distanceFeet, usesLeft);
        }

        // 그 주술사 레벨의 강신 단계(1..Tiers.Length), 첫 단계 전이면 0. MaxUses 와 같은 「레벨 이하 마지막 단계」 규칙. 표는 레벨 오름차순이어야 한다.
        public static int TierAt(int shamanLevel)
        {
            SpiritRules.Budget(shamanLevel); // 범위 검사
            int t = 0;
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (i > 0 && Tiers[i].Level <= Tiers[i - 1].Level) throw new InvalidOperationException("PossessionRules: Dials.PossessionTiers levels are not strictly ascending");
                if (Tiers[i].Level <= shamanLevel) t = i + 1;
            }
            return t;
        }

        // 단계 → 효과 버프 번호(0 기준). 효과 버프는 I·II·III 셋(설계 §4.7 표: IV 의 추가 효과는 융합 선택 — 강신 효과는 III 과 같다).
        // effectBuffCount = 게임 쪽이 만든 효과 버프 수. 단계가 그보다 크면 마지막 효과 버프(지속은 Rounds(tier) 로 따로 정한다).
        public static int EffectBuffIndex(int tier, int effectBuffCount)
        {
            if (tier < 1 || tier > Tiers.Length) throw new ArgumentOutOfRangeException("tier", tier, "PossessionRules: tier must be 1.." + Tiers.Length);
            if (effectBuffCount < 1 || effectBuffCount > Tiers.Length) throw new ArgumentOutOfRangeException("effectBuffCount", effectBuffCount, "PossessionRules: effect buff count must be 1.." + Tiers.Length);
            return Math.Min(tier, effectBuffCount) - 1;
        }

        // 주술사 쪽 대상(설계 §4.7 「얹는 곳 = 주술사 주무기(비었으면 맨손 무기)」). 보조 무기로 넘어가지 않는다. 주 손에 무기 아닌 것 = NonWeapon(얹지 않음, 거울 T4 m2 와 같은 규칙).
        public static TargetKind ShamanTarget(HandContent primary)
        {
            switch (primary)
            {
                case HandContent.Weapon: return TargetKind.Primary;
                case HandContent.Empty: return TargetKind.EmptyHand;
                case HandContent.NonWeapon: return TargetKind.NonWeapon;
                default: throw new ArgumentException("PossessionRules.ShamanTarget: undefined HandContent " + (int)primary);
            }
        }

        // 자원 최대치의 엔진 식 매개변수(BlueprintAbilityResource.Amount.IncreasedByLevelStartPlusDivStep — GetMaxAmount @02CD~0343:
        // 주술사 레벨 L ≥ StartingLevel 이면 StartingIncrease + PerStepIncrease·(L − StartingLevel)/LevelStep). Dials 표에서 파생한다 — 표가 등차가 아니면 던진다.
        // 시험 `exec: possession rules` 가 이 매개변수로 엔진 식을 L = 1..20 실행해 MaxUses(L) 과 같음을 본다.
        public static StartPlusDivStep ResourceFormula()
        {
            if (Tiers.Length < 2) throw new InvalidOperationException("PossessionRules: need at least 2 tiers to derive a level step");
            int step = Tiers[1].Level - Tiers[0].Level, per = Tiers[1].UsesPerDay - Tiers[0].UsesPerDay;
            for (int i = 1; i < Tiers.Length; i++)
                if (Tiers[i].Level - Tiers[i - 1].Level != step || Tiers[i].UsesPerDay - Tiers[i - 1].UsesPerDay != per)
                    throw new InvalidOperationException("PossessionRules: Dials.PossessionTiers is not arithmetic (row " + i + ") — the engine StartPlusDivStep formula cannot express it");
            if (step <= 0 || per < 0 || Tiers[0].UsesPerDay < 1) throw new InvalidOperationException("PossessionRules: tier steps must be positive");
            return new StartPlusDivStep(Tiers[0].Level, Tiers[0].UsesPerDay, step, per);
        }

        // 복사 대상 = 영병 활성 무기에 비친 집합(계획 결과, 고유 GhostTouch 포함 — 설계 §4.7). 주술사 무기에 이미 있는 GUID 는 건너뜀.
        public static CopyResult CopySet(AbsorbPlan plan, string[] shamanWeaponGuids)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (shamanWeaponGuids == null) throw new ArgumentNullException("shamanWeaponGuids");
            AbsorbPlanner.CheckInvariants(plan);
            var have = new HashSet<string>(shamanWeaponGuids);
            var copy = new List<string>();
            var skip = new List<string>();
            foreach (var g in plan.AllGuids()) (have.Contains(g) ? skip : copy).Add(g);
            return new CopyResult { Copy = copy.ToArray(), Skipped = skip.ToArray() };
        }
    }
}
