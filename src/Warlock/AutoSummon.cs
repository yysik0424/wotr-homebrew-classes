using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using UnityModManagerNet;

namespace Warlock
{
     
    // 순서: 사전조건(예약 없음) → 과열 0 → 예약 기록 → RuleCastSpell 무료 발동(정찰 §2 (a), 실행은 다음 틱 큐) → 결과 = HeatRule.AfterReserveCast(성공 여부) 효과 적용.
    // 완료는 변형의 차감 액션이 예약을 참조로 소비할 때(ContextActionWarlockSpendHeat). 즉시 실패 = 예약 해제 + 9 복원 + ERROR(진행 원장 O2/M28 — 효과는 순수 코어가 정한다).
    public static class AutoSummon
    {
        // The ability instance belongs to exactly one automatic cast. Late or repeated deliveries
        // stay recognizable even after its reservation closes. Prune dead handles at each new cast.
        static readonly List<WeakReference> automatic = new List<WeakReference>();
        static readonly List<WeakReference> watching = new List<WeakReference>();

        internal static bool IsRetired(AbilityData ability, AbilityData reserved)
        {
            if (ReferenceEquals(ability, null) || ReferenceEquals(ability, reserved)) return false;
            foreach (var reference in automatic) if (ReferenceEquals(reference.Target, ability)) return true;
            return false;
        }

        static void MarkAutomatic(AbilityData ability)
        {
            automatic.RemoveAll(reference => !reference.IsAlive);
            automatic.Add(new WeakReference(ability));
        }

        internal static void Watch(UnitPartWarlockAuto part)
        {
            foreach (var reference in watching) if (ReferenceEquals(reference.Target, part)) return;
            watching.Add(new WeakReference(part));
        }

        internal static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            for (int i = watching.Count - 1; i >= 0; i--)
            {
                var part = watching[i].Target as UnitPartWarlockAuto;
                if (part == null || part.IsDisposed || !part.RefundDue) { watching.RemoveAt(i); continue; }
                try { part.RecoverIfAbandoned(); }
                catch (Exception ex) { HeatLog.Failed("auto-summon refund", part.Owner, ex, "auto-refund|" + part.Owner.UniqueId); }
            }
        }

        public static void Reserve(UnitEntityData caster, int tier)
        {
            if (caster == null) throw new ArgumentNullException("caster");
            var part = caster.Ensure<UnitPartWarlockAuto>();
            if (part.RefundDue || part.Reserved != null) throw new InvalidOperationException("Warlock: AutoSummon.Reserve while a reservation is pending");
            AbilityData data = null;
            bool started = false;
            bool success;
            Watch(part);
            part.Submitting = true;
            part.RefundDue = true;
            try
            {
                var group = Kit.GroupOf(caster);
                tier = Summons.HighestAvailableTier(group, tier);
                data = new AbilityData(Summons.Variant(group, tier), caster.Descriptor);
                var request = new RuleCastSpell(data, new TargetWrapper(caster));
                part.ReservedAt = HeatClock.Now();
                MarkAutomatic(data);
                part.Cast = request;
                started = true;
                part.Reserved = data;
                part.ReservedTier = tier;
                WarlockHeat.Write(caster, 0);
                var result = Rulebook.Trigger(request);
                success = result.Success && result.Result != null;
            }
            catch
            {
                // Before the debit, the hit has reached the threshold even when preparation throws.
                if (!started) part.Refund();
                else Apply(caster, part, data, HeatRule.AfterReserveCast(false, tier, caster.UniqueId));
                throw;
            }
            finally { part.Submitting = false; }
            Apply(caster, part, data, HeatRule.AfterReserveCast(success, tier, caster.UniqueId));
        }

        static void Apply(UnitEntityData caster, UnitPartWarlockAuto part, AbilityData data, HeatEffect[] effects)
        {
            foreach (var e in effects)
            {
                switch (e.Kind)
                {
                    case HeatEffectKind.ReleaseReservation:
                        if (!part.Owes(data)) return; // A nested delivery already completed, refunded, or moved on.
                        break;
                    case HeatEffectKind.WriteRank: part.Refund(); break;
                    case HeatEffectKind.Error: HeatLog.Emit(true, e.OnceKey, e.Text); break;
                    default: throw new ArgumentOutOfRangeException("effects", e.Kind, "Warlock: unexpected auto-summon effect");
                }
            }
        }
    }
}
