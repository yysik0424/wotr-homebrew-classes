using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
     
    // 극성 분기(설계 §8-b): Conditional(대상 HasFact NEA) ? DealDamage(Divine) : DealDamage(NegativeEnergy) — 두 갈래 주사위 같음.
    public sealed class DamageBranch
    {
        public readonly string Key;
        public readonly string DiceSource;   // "SmiteNegative"(CRC 사용자 표) · "DeathBlade"(고정) · "LohDice"(CRC 클래스 레벨 Div2) · "ChannelLike"(½ 레벨)
        public readonly int Sides;
        public readonly string PlainEnergy;  // 친화 없는 대상
        public readonly string AffinityEnergy; // 음에너지 친화 대상(§8-b 신성 전환)
        public readonly bool IgnoreCritical;  
        public DamageBranch(string key, string dice, int sides, string plain, string affinity, bool ignoreCritical) { Key = key; DiceSource = dice; Sides = sides; PlainEnergy = plain; AffinityEnergy = affinity; IgnoreCritical = ignoreCritical; }
    }

    public sealed class StrikeTrigger
    {
        public readonly string Key;          // 채우는 특성 키(레벨 표 키 또는 재주 Id)
        public readonly string Kind;         // "Weapon" = AddInitiatorAttackWithWeaponTrigger · "AbilityUse" = AddAbilityUseTrigger(스마이트 시전 뒤)
        public readonly bool OnlyHit;
        public readonly string Range;        // "Any" | "Melee"
        public readonly bool RequiresSmiteTarget;
        public readonly string RoundMarker;  // RoundOnceAction 표지 키 | null
        public StrikeTrigger(string key, string kind, bool onlyHit, string range, bool smite, string marker) { Key = key; Kind = kind; OnlyHit = onlyHit; Range = range; RequiresSmiteTarget = smite; RoundMarker = marker; }
    }

    public static class StrikePlan
    {
        public const string Negative = "NegativeEnergy";
        public const string Divine = "Divine";

        static readonly IReadOnlyList<DamageBranch> branches = new[]
        {
             
            new DamageBranch("SmiteNegative", "SmiteNegative", RevenantRules.Dials.SmiteDie, Negative, Divine, true),
            // 출처: 설계 §6 죽음의 칼날 +1d6(근접) + §8-b.
            new DamageBranch("DeathBlade", "DeathBlade", RevenantRules.Dials.SmiteDie, Negative, Divine, true),
            // 출처: 설계 §6 죽음의 안수(안수 주사위 ½ 레벨 d6) + P3 신성 전환(무기 아님 — 치명 없음).
            new DamageBranch("DeathsTouch", "LohDice", RevenantRules.Dials.SmiteDie, Negative, Divine, false),
            // 출처: 판정 P4 마지막 숨 = 채널과 같음(½ 레벨 d6) + §8-b.
            new DamageBranch("LastBreath", "ChannelLike", RevenantRules.Dials.SmiteDie, Negative, Divine, false),
        };

        public static IReadOnlyList<DamageBranch> AllBranches { get { return branches; } }

        public static DamageBranch DamageBranches(string key)
        {
            var b = branches.FirstOrDefault(x => x.Key == key);
            if (b == null) throw new ArgumentException("OathboundRevenant: no damage branch for " + key);
            return b;
        }

        // 스마이트 음에너지 사용자 CRC 표(레벨 1..20 → 주사위 수) — 원장 함수로 만든다(계획 「SmiteNegativeProgression」).
        public static int[] SmiteNegativeProgression()
        {
            var a = new int[RevenantRules.Levels.MaxLevel];
            for (int l = RevenantRules.Levels.MinLevel; l <= RevenantRules.Levels.MaxLevel; l++) a[l - RevenantRules.Levels.MinLevel] = RevenantRules.SmiteNegativeDice(l);
            return a;
        }

         
        public static readonly IReadOnlyList<StrikeTrigger> Triggers = new[]
        {
            new StrikeTrigger("OathRegeneration", "Weapon", true, "Any", true, "OathRegenSpent"),
            new StrikeTrigger("SmiteNegative", "Weapon", true, "Any", true, null),
            new StrikeTrigger("GraveyardDread", "AbilityUse", false, "Any", true, null),
            new StrikeTrigger("LifeDrain", "Weapon", true, "Any", true, "LifeDrainSpent"),
            new StrikeTrigger("DeathBlade", "Weapon", true, "Melee", false, null),
        };

        public static StrikeTrigger Trigger(string key)
        {
            var t = Triggers.FirstOrDefault(x => x.Key == key);
            if (t == null) throw new ArgumentException("OathboundRevenant: no strike trigger for " + key);
            return t;
        }
    }
}
