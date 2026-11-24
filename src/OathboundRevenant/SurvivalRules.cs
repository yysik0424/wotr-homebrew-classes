using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
     
    // 전부 생산 부품(SurvivalParts.cs)이 직접 부른다 — wiring: 시험이 IL 로 호출을 확인한다(장식용 순수 함수 금지, 검토 M4).

    public static class SurvivalPlan
    {
        // 출처: 설계 §6 무덤의 갑주 DR 5/선 · §8-c 강철 뼈 25% · 망령의 걸음 +10 ft(값은 원장 Dials — 여기는 행 이름과 연결만).
        public static int GraveArmorDr { get { return RevenantRules.Dials.GraveArmorDr; } }
        public static readonly string GraveArmorBypass = "Good";
        public static int IronBonesPercent { get { return RevenantRules.Dials.IronBonesPercent; } }
        public static int GhostStrideSpeed { get { return RevenantRules.Dials.GhostStrideSpeedFeet; } }
        public static readonly string GhostStrideImmunity = "DifficultTerrain";
        public static readonly string GhostStrideDescriptor = "UntypedStackable";
    }

    public struct UnyieldingDecision
    {
        public readonly bool Save, Burst;
        public readonly int SetDamage;
        public UnyieldingDecision(bool save, int setDamage, bool burst) { Save = save; SetDamage = setDamage; Burst = burst; }
    }

    public static class UnyieldingRule
    {
        // 피해 뒤 누적 피해 ≥ 최대 HP(= HP 0 이하) ∧ 자원 ≥ 1 이면 버팀: 누적 피해 = 최대 HP − 1(본편 UnitPartDeathPrevention @0031~0070 과 같은 식), 마지막 숨 보유 시 폭발.
        public static UnyieldingDecision Decide(int damageAfter, int maxHp, int resourceLeft, bool hasLastBreath)
        {
            if (maxHp < 1) throw new ArgumentOutOfRangeException("maxHp", maxHp, "OathboundRevenant: max HP must be >= 1");
            if (damageAfter < maxHp || resourceLeft < RevenantRules.Dials.UsesPerActivation) return new UnyieldingDecision(false, damageAfter, false);
            return new UnyieldingDecision(true, maxHp - RevenantRules.Dials.UnyieldingHpLeft, hasLastBreath);
        }
    }

    public enum RiposteVerdict { Attack, Refuse, Missing }

    public struct RiposteDecision
    {
        public readonly RiposteVerdict Verdict;
        public readonly string MissingInput;
        public RiposteDecision(RiposteVerdict v, string missing) { Verdict = v; MissingInput = missing; }
    }

    public static class RiposteRule
    {
        // 결측(소유자·무기 — 맨손도 엔진의 빈 손 무기로 있음 — ·자원 블루프린트) = Missing(Report) · 정당한 거절 = 주 무기가 원거리·자원 0·공격자 사망·사거리 밖·이미 이 공격에 반격 · 그 밖 Attack.
        public static RiposteDecision Decide(bool ownerPresent, bool weaponPresent, bool resourceBlueprintPresent, int left, bool attackerAlive, bool inReach, bool alreadyThisAttack, bool weaponIsMelee)
        {
            if (!ownerPresent) return new RiposteDecision(RiposteVerdict.Missing, "owner");
            if (!weaponPresent) return new RiposteDecision(RiposteVerdict.Missing, "weapon");
            if (!resourceBlueprintPresent) return new RiposteDecision(RiposteVerdict.Missing, "resource");
            if (!weaponIsMelee || alreadyThisAttack || left < RevenantRules.Dials.UsesPerActivation || !attackerAlive || !inReach) return new RiposteDecision(RiposteVerdict.Refuse, null);
            return new RiposteDecision(RiposteVerdict.Attack, null);
        }
    }

    // 「같은 공격 사건에 한 번」 원장(검토 M1): 키 = (반격하는 소유자, 공격자, 게임 시각). 소유자마다 마지막 사건 하나만 기억한다(두 망자가 같은 공격자에게 각자 반격 가능).
    public sealed class RiposteOnceLedger<TUnit> where TUnit : class
    {
        sealed class LastAttack
        {
            public readonly WeakReference<TUnit> Owner;
            public readonly WeakReference<TUnit> Attacker;
            public TimeSpan At;

            public LastAttack(TUnit owner, TUnit attacker, TimeSpan at)
            {
                Owner = new WeakReference<TUnit>(owner);
                Attacker = new WeakReference<TUnit>(attacker);
                At = at;
            }
        }

        // Only units that actually riposte have entries. Weak handles on both sides work on the game's Mono.
        readonly List<LastAttack> last = new List<LastAttack>();

        LastAttack Find(TUnit owner)
        {
            for (int i = last.Count - 1; i >= 0; i--)
            {
                var entry = last[i];
                if (!entry.Owner.TryGetTarget(out var recordedOwner) || !entry.Attacker.TryGetTarget(out _))
                    last.RemoveAt(i);
                else if (ReferenceEquals(recordedOwner, owner))
                    return entry;
            }
            return null;
        }

        public bool Already(TUnit owner, TUnit attacker, TimeSpan now)
        {
            if (owner == null || attacker == null) throw new ArgumentNullException(owner == null ? "owner" : "attacker");
            lock (last)
            {
                var entry = Find(owner);
                return entry != null && entry.At == now && entry.Attacker.TryGetTarget(out var previous) && ReferenceEquals(previous, attacker);
            }
        }

        public void Record(TUnit owner, TUnit attacker, TimeSpan now)
        {
            if (owner == null || attacker == null) throw new ArgumentNullException(owner == null ? "owner" : "attacker");
            lock (last)
            {
                var entry = Find(owner);
                if (entry == null)
                    last.Add(new LastAttack(owner, attacker, now));
                else
                {
                    entry.Attacker.SetTarget(attacker);
                    entry.At = now;
                }
            }
        }
    }

    public struct ShieldSplit
    {
        public readonly int Ally, Paladin;
        public ShieldSplit(int ally, int paladin) { Ally = ally; Paladin = paladin; }
    }

    public static class OathShieldRule
    {
        // 근접 무기 피해만(명중 굴림 ∧ 무기 ∧ 근접). 그 밖은 정당한 무동작.
        public static bool Applies(bool hasAttackRoll, bool hasWeapon, bool weaponIsMelee) { return hasAttackRoll && hasWeapon && weaponIsMelee; }

        // 실제로 받은 피해 = 피해 반영 뒤 누적 피해 − 반영 전(엔진 RuleDealDamage.OnTrigger IL_0232~025A: Damage += 임시 HP 가 흡수하고 남은 값 — Result 는 임시 HP 전 값, 검토 I1).
        public static int TakenAfterTemp(int damageBefore, int damageAfter)
        {
            if (damageBefore < 0 || damageAfter < 0) throw new ArgumentOutOfRangeException(damageBefore < 0 ? "damageBefore" : "damageAfter", "OathboundRevenant: accumulated damage must be >= 0");
            return Math.Max(0, damageAfter - damageBefore);
        }

        // 실제로 받은 피해 중 min(피해, 방패량)을 팔라딘이 대신 받는다.
        public static ShieldSplit Split(int incoming, int shield)
        {
            if (incoming < 0 || shield < 0) throw new ArgumentOutOfRangeException(incoming < 0 ? "incoming" : "shield", "OathboundRevenant: damage split inputs must be >= 0");
            int p = Math.Min(incoming, shield);
            return new ShieldSplit(incoming - p, p);
        }

        // 아군의 누적 피해에서 옮긴 양을 되돌린다(0 미만 금지).
        public static int Restore(int currentDamage, int moved)
        {
            if (moved < 0) throw new ArgumentOutOfRangeException("moved", moved, "OathboundRevenant: moved damage must be >= 0");
            return Math.Max(0, currentDamage - moved);
        }
    }

    // 「진짜 무기 처치」 판정(판정 기록 R2 — 소유자: 접촉·광선·주문 전달 무기 제외). 죽음의 안수 등 AbilityDeliverTouch 는 명중 굴림에 접촉 무기(TouchItem → TouchType, Category Touch · AttackType Touch)를 싣는다
    // (blueprints.zip Weapons/Types/Special/TouchType.jbp) → ar.Weapon != null 만으로는 무기 처치가 아니다. 광선 = Category Ray / AttackType RangedTouch.
    public static class SoulHarvestGate
    {
        // 출처: 엔진 WeaponCategory.Touch(61)·Ray(62), AttackType Touch·RangedTouch(Kingmaker.RuleSystem.AttackType) — 이름으로 비교한다.
        public static readonly IReadOnlyList<string> ExcludedCategories = new[] { "Touch", "Ray" };
        public static readonly IReadOnlyList<string> ExcludedAttackTypes = new[] { "Touch", "RangedTouch" };

        public static bool RealWeaponKill(bool hasAttackRoll, bool hasWeapon, string weaponCategory, string attackType)
        {
            if (!hasAttackRoll || !hasWeapon) return false;
            if (string.IsNullOrEmpty(weaponCategory) || string.IsNullOrEmpty(attackType)) throw new ArgumentException("OathboundRevenant: soul harvest gate needs the weapon category and attack type of a present weapon");
            return !ExcludedCategories.Contains(weaponCategory) && !ExcludedAttackTypes.Contains(attackType);
        }
    }

    public enum SoulHarvestVerdict { Heal, Pending, Ignore, Expired, None }

    // 영혼 수확(판정 기록 C1 — 「무기로 실제로 죽은 적만」): 무기 피해로 HP 0 이하가 된 적을 보류로 적고, 그 유닛의 생명 상태가 「죽음」으로 바뀌면 회복.
    // 죽음이 아닌 상태로 바뀌거나(기절 등) 창이 지나면 보류를 버린다. 엔진은 HP 0 이하 → 사망을 다음 틱(UnitLifeController)에 처리하므로 피해 사건 안의 생존 판정은 항상 「살아 있음」이다(검토 C1).
    public sealed class SoulHarvestLedger<TKey> where TKey : class
    {
        sealed class Pending
        {
            public WeakReference<TKey> Target;
            public TimeSpan Until;
        }
        readonly List<Pending> pending = new List<Pending>();
        Pending Find(TKey target)
        {
            foreach (var entry in pending)
                if (entry.Target.TryGetTarget(out var live) && EqualityComparer<TKey>.Default.Equals(live, target)) return entry;
            return null;
        }
        public int PendingCount { get { return pending.Count; } }
        public bool IsPending(TKey target) { return target != null && Find(target) != null; }

        public SoulHarvestVerdict OnLethalHit(TKey target, bool isEnemy, bool byWeapon, bool deadNow, TimeSpan now, TimeSpan window)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (window <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("window", window, "OathboundRevenant: soul harvest window must be > 0");
            Sweep(now);
            if (!isEnemy || !byWeapon) return SoulHarvestVerdict.Ignore;
            var existing = Find(target);
            if (deadNow) { pending.Remove(existing); return SoulHarvestVerdict.Heal; }
            if (existing == null) pending.Add(new Pending { Target = new WeakReference<TKey>(target), Until = now + window });
            else existing.Until = now + window;
            return SoulHarvestVerdict.Pending;
        }

        public SoulHarvestVerdict OnLifeStateChanged(TKey unit, bool isDeadNow, TimeSpan now)
        {
            var entry = unit == null ? null : Find(unit);
            if (entry == null) return SoulHarvestVerdict.None;
            pending.Remove(entry);
            if (now > entry.Until || !isDeadNow) return SoulHarvestVerdict.Expired;
            return SoulHarvestVerdict.Heal;
        }

        // 라운드 틱 정리(검토 R2 선택 5): 사라진(파괴·게임 밖) 유닛의 보류를 버린다. 반환 = 버린 수.
        public int Prune(TimeSpan now, Func<TKey, bool> gone)
        {
            if (gone == null) throw new ArgumentNullException("gone");
            return pending.RemoveAll(e => !e.Target.TryGetTarget(out var target) || gone(target) || now > e.Until);
        }

        public int Sweep(TimeSpan now)
        {
            return pending.RemoveAll(e => now > e.Until || !e.Target.TryGetTarget(out _));
        }
    }
}
