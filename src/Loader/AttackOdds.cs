using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

namespace HomebrewClasses.Loader
{
    // A snapshot of readable attack conditions. Hover must never resolve an attack or spend a reroll/image.
    internal sealed class AttackOdds
    {
        internal int Armor, AlternateArmor, TargetBonus;
        internal bool FlatFooted, AlwaysChance, Conditional, SeparateMiss;
        internal int ConcealmentMiss, ConcealmentRerolls, Images;
        internal int OnSuccess, OnFailure, FixedDie;
        internal int RangeAdjustment, AlternateRangeAdjustment;
        readonly List<DieAdjustment> conditionalDice = new List<DieAdjustment>();
        sealed class DieAdjustment
        {
            internal int Signed, Threshold, ActivationPercent;
            internal bool OnlyFailed, OnlySucceeded;
            internal ModifyD20.RollConditionType Condition;

            internal bool Applies(int die, bool success)
            {
                if (OnlyFailed) return !success;
                if (OnlySucceeded) return success;
                switch (Condition)
                {
                    case ModifyD20.RollConditionType.ShouldBeMoreThan: return die > Threshold;
                    case ModifyD20.RollConditionType.ShouldBeMoreOrEqualThan: return die >= Threshold;
                    case ModifyD20.RollConditionType.ShouldBeLessThan: return die < Threshold;
                    case ModifyD20.RollConditionType.ShouldBeLessOrEqualThan: return die <= Threshold;
                    case ModifyD20.RollConditionType.Equal: return die == Threshold;
                    default: return true;
                }
            }
        }
        static readonly System.Reflection.FieldInfo coverageField = AccessTools.Field(typeof(RerollConcealment), "m_WeaponCoverage");

        static readonly System.Reflection.FieldInfo missTypeField = AccessTools.Field(typeof(SetAttackerMissChance), "m_Type");
        static bool OtherAttackRule(BlueprintComponent c) =>
            c is IRulebookHandler<RuleAttackRoll> || c is IRulebookHandler<RuleRollD20> ||
            c is IRulebookHandler<RuleCalculateAC> || c is IRulebookHandler<RuleCalculateAttackBonus> ||
            c is IRulebookHandler<RuleConcealmentCheck> || c is IRulebookHandler<RuleCheckTargetFlatFooted>;

        internal static AttackOdds Read(UnitEntityData actor, UnitEntityData target, AttackType type, bool weaponAttack)
        {
            var result = new AttackOdds();
            bool melee = type.IsMelee();
            var flat = new RuleCheckTargetFlatFooted(actor, target);
            var attackMiss = new MissChance();
            var concealCheck = new MissChance();
            foreach (var fact in target.Facts.List.Where(f => f.Active))
            foreach (var component in fact.Blueprint.ComponentsArray)
            {
                if (component is FlatFootedIgnore ignore)
                {
                    if (ignore.Type == FlatFootedIgnoreType.UncannyDodge) flat.IgnoreVisibility = true;
                    if (ignore.Type == FlatFootedIgnoreType.UncannyDodge || ignore.Type == FlatFootedIgnoreType.GreaterBlindFight ||
                        (ignore.Type == FlatFootedIgnoreType.BlindFight && melee) ||
                        (ignore.Type == FlatFootedIgnoreType.ImprovedBlindFight && actor.DistanceTo(target) <= 30.Feet().Meters))
                        flat.IgnoreConcealment = true;
                }
                else if (component is SetAttackerMissChance miss)
                {
                    if (missTypeField == null) { result.Conditional = true; continue; }
                    var applies = type == AttackType.Touch ? SetAttackerMissChance.Type.Touch :
                        type == AttackType.RangedTouch ? SetAttackerMissChance.Type.RangedTouch :
                        melee ? SetAttackerMissChance.Type.Melee : SetAttackerMissChance.Type.Ranged;
                    if (((SetAttackerMissChance.Type)missTypeField.GetValue(miss) & applies) == 0) continue;
                    if (miss.Conditions != null && !miss.Conditions.HasConditions &&
                        miss.Value?.ValueType == ContextValueType.Simple) attackMiss.Set(miss.Value.Value);
                    else result.Conditional = true;
                }
                else if (component is ModifyAttackerMissChance mod)
                {
                    if (mod.Value?.ValueType != ContextValueType.Simple) { result.Conditional = true; continue; }
                    attackMiss.Modify(mod.Value.Value);
                    concealCheck.Modify(mod.Value.Value);
                }
                else if (OtherAttackRule(component)) result.Conditional = true;
            }
            // This native method only writes the result on this private query. Do not dispatch RuleAttackRoll.
            flat.OnTrigger(null);
            result.FlatFooted = flat.IsFlatFooted;
            var ac = target.Stats.AC;
            result.Armor = type.IsTouch() ? (flat.IsFlatFooted ? ac.FlatFootedTouch : ac.Touch) :
                (flat.IsFlatFooted ? ac.FlatFooted : ac.ModifiedValue);
            if (target.State.Prone.Active || (target.View && target.View.IsGetUp)) result.Armor += melee ? -4 : 4;
            if (melee && target.State.IsHelpless) result.Armor -= 4;
            if (target.State.HasCondition(UnitCondition.Blindness)) result.Armor -= 2;

            int flatDifference = type.IsTouch() ? ac.FlatFootedTouch - ac.Touch : ac.FlatFooted - ac.ModifiedValue;
            result.AlternateArmor = result.Armor + (flat.IsFlatFooted ? -flatDifference : flatDifference);
            var conceal = UnitPartConcealment.Calculate(actor, target, true);
            var fallback = actor.GetFirstWeapon();
            if (fallback != null && fallback.Blueprint.AttackType.IsMelee() != melee) result.Conditional = true;
            int nativeConcealment = conceal == Concealment.Total ? 50 : conceal == Concealment.Partial ? 20 : 0;
            concealCheck.Set(nativeConcealment);
            // Native attack resolution chooses the larger independent miss chance before the concealment check.
            result.SeparateMiss = attackMiss.Calculate() > nativeConcealment;
            result.ConcealmentMiss = result.SeparateMiss ? attackMiss.Calculate() :
                nativeConcealment > 0 ? Math.Max(0, concealCheck.Calculate()) : 0;
            result.AlwaysChance = actor.State.Features.AlwaysChance;
            if (!actor.Descriptor.IsImmuneToVisualEffects)
                result.Images = target.Get<UnitPartMirrorImage>()?.MechanicsImages?.Count ?? 0;
            if (UnitPartConcealment.Calculate(target, actor, false) == Concealment.Total ||
                (actor.State.Features.Blinking && !target.Descriptor.IsSeeInvisibility)) result.TargetBonus += 2;
            if (melee && target.CombatState.IsFlanked) result.TargetBonus += 2;
            bool precise = false;
            var distanceBonuses = new List<DistanceAttackBonus>();
            foreach (var fact in actor.Facts.List.Where(f => f.Active))
            foreach (var component in fact.Blueprint.ComponentsArray)
            {
                if (component is ModifyD20 roll)
                {
                    if (!roll.AgainstAlignment || actor.Alignment.ValueRaw.HasComponent(roll.Alignment) ||
                        (roll.TargetAlignment && target.Alignment.ValueRaw.HasComponent(roll.Alignment))) result.ReadRoll(roll);
                }
                else if (component is PreciseShot) precise = true;
                else if (component is AttackBonusAgainstFactOwner marked)
                {
                    if (target.Descriptor.HasFact(marked.CheckedFact) == marked.Not) continue;
                    if (marked.Bonus?.ValueType == ContextValueType.Simple && marked.Descriptor == ModifierDescriptor.None)
                        result.TargetBonus += marked.AttackBonus * fact.GetRank() + marked.Bonus.Value;
                    else result.Conditional = true;
                }
                else if (component is OutflankAttackBonus outflank)
                {
                    if (melee && target.CombatState.IsFlanked && (actor.State.Features.SoloTactics ||
                        target.CombatState.EngagedBy.Any(u => u != actor && u.Descriptor.HasFact(outflank.OutflankFact))))
                        result.TargetBonus += outflank.AttackBonus * fact.GetRank();
                }
                else if (component is DistanceAttackBonus distance && !melee) distanceBonuses.Add(distance);
                else if (component is RerollConcealment reroll && !result.SeparateMiss && result.ConcealmentMiss > 0)
                {
                    if (coverageField == null) { result.Conditional = true; continue; }
                    var coverage = (RerollConcealment.WeaponCoverage)coverageField.GetValue(reroll);
                    // Native ShouldReroll allows a filtered reroll when no RuleAttackWithWeapon encloses a ray.
                    if (!weaponAttack || coverage == RerollConcealment.WeaponCoverage.All ||
                        (melee ? coverage == RerollConcealment.WeaponCoverage.Melee : coverage == RerollConcealment.WeaponCoverage.Ranged))
                        result.ConcealmentRerolls++;
                }
                else if (OtherAttackRule(component)) result.Conditional = true;
            }
            if (!melee && !precise && (target.CombatState.EngagedBy.Any(u => u != actor && !actor.IsEnemy(u)) ||
                target.CombatState.EngagedUnits.Any(u => u != actor && !actor.IsEnemy(u)))) result.TargetBonus -= 4;
            var enemies = actor.Get<UnitPartFavoredEnemy>();
            if (enemies != null)
            {
                int favored = 0;
                foreach (var entry in enemies.Entries)
                    if (entry.Bonus > favored && entry.CheckedFeatures.Any(f => target.Descriptor.HasFact(f))) favored = entry.Bonus;
                result.TargetBonus += favored;
            }
            if (distanceBonuses.Count == 1)
            {
                var d = distanceBonuses[0];
                if (actor.DistanceTo(target) <= d.Range.Meters + actor.Corpulence + target.Corpulence) result.RangeAdjustment = d.AttackBonus;
                else result.AlternateRangeAdjustment = d.AttackBonus;
                result.TargetBonus += result.RangeAdjustment;
            }
            else if (distanceBonuses.Count > 1) result.Conditional = true;
            return result;
        }

        internal void ReadRoll(ModifyD20 r)
        {
            if ((r.Rule & RuleType.AttackRoll) == 0) return;
            if (r.AddBonus || (r.WithChance && r.Chance?.ValueType != ContextValueType.Simple) ||
                r.ActionsToTriggerBeforeRoll?.HasActions == true || r.RollsAmount < 0)
            { Conditional = true; return; }
            if (r.Replace)
            {
                if (!r.WithChance && r.RollCondition == ModifyD20.RollConditionType.None && r.RollResult != null && r.RollResult.ValueType == ContextValueType.Simple &&
                    r.RollResult.Value >= 1 && r.RollResult.Value <= 20 && !r.RerollOnlyIfFailed && !r.RerollOnlyIfSuccess && FixedDie == 0)
                    FixedDie = r.RollResult.Value;
                else Conditional = true;
                return;
            }
            int signed = r.TakeBest ? r.RollsAmount : -r.RollsAmount;
            if (r.WithChance || r.RollCondition != ModifyD20.RollConditionType.None)
            {
                if (r.RollCondition != ModifyD20.RollConditionType.None && !r.RerollOnlyIfFailed && !r.RerollOnlyIfSuccess &&
                    r.ValueToCompareRoll?.ValueType != ContextValueType.Simple) { Conditional = true; return; }
                conditionalDice.Add(new DieAdjustment { Signed = signed, Threshold = r.ValueToCompareRoll?.Value ?? 0,
                    ActivationPercent = r.WithChance ? Math.Max(0, Math.Min(100, r.Chance.Value)) : 100,
                    OnlyFailed = r.RerollOnlyIfFailed, OnlySucceeded = r.RerollOnlyIfSuccess, Condition = r.RollCondition });
                return;
            }
            if (!r.RerollOnlyIfFailed) OnSuccess += signed;
            if (r.RerollOnlyIfFailed || !r.RerollOnlyIfSuccess) OnFailure += signed;
        }

        // Condition on the initial d20. Native AddReroll sums signed amounts before taking the best or worst.
        internal static double Chance(int bonus, int armor, bool alwaysChance, int onSuccess, int onFailure,
            int fixedDie, int concealmentMiss, int concealmentRerolls, int images)
        {
            int need = (int)Math.Max(alwaysChance ? 1L : 2L, Math.Min(20L, (long)armor - bonus));
            double p = (21 - need) / 20.0;
            double hit = fixedDie > 0 ? (fixedDie >= need ? 1 : 0) :
                p * Math.Pow(p, Math.Max(0L, -(long)onSuccess)) +
                (1 - p) * (1 - Math.Pow(1 - p, Math.Max(0, onFailure)));
            return 100 * Math.Min(1, Math.Max(0, hit)) * (1 - Math.Pow(concealmentMiss / 100.0, 1.0 + concealmentRerolls)) / (1.0 + images);
        }

        double PercentAt(int bonus, int armor)
        {
            if (conditionalDice.Count == 0 || FixedDie != 0)
                return Chance(bonus, armor, AlwaysChance, OnSuccess, OnFailure, FixedDie, ConcealmentMiss, ConcealmentRerolls, Images);
            int need = (int)Math.Max(AlwaysChance ? 1L : 2L, Math.Min(20L, (long)armor - bonus));
            double p = (21 - need) / 20.0;
            double total = 0;
            for (int die = 1; die <= 20; die++)
            {
                bool hit = die >= need;
                var weights = new Dictionary<int, double> { { hit ? OnSuccess : OnFailure, 1 } };
                foreach (var effect in conditionalDice)
                {
                    if (!effect.Applies(die, hit) || effect.ActivationPercent == 0) continue;
                    var next = new Dictionary<int, double>();
                    double activate = effect.ActivationPercent / 100.0;
                    foreach (var row in weights)
                    {
                        if (activate < 1) AddWeight(next, row.Key, row.Value * (1 - activate));
                        AddWeight(next, row.Key + effect.Signed, row.Value * activate);
                    }
                    // Arbitrary mod stacks with many independent outcomes can exceed an interactive preview budget.
                    if (next.Count > 256) { Conditional = true; return double.NaN; }
                    weights = next;
                }
                foreach (var row in weights)
                    total += row.Value * (hit ? Math.Pow(p, Math.Max(0L, -(long)row.Key)) :
                        1 - Math.Pow(1 - p, Math.Max(0, row.Key)));
            }
            return total * 5 * (1 - Math.Pow(ConcealmentMiss / 100.0, 1.0 + ConcealmentRerolls)) / (1.0 + Images);
        }

        static void AddWeight(Dictionary<int, double> weights, int count, double amount)
        {
            weights.TryGetValue(count, out var old);
            weights[count] = old + amount;
        }

        internal string At(int adjustedBonus, int armor) => Format(PercentAt(adjustedBonus, armor));

        internal string BandAt(int bonus)
        {
            double current = PercentAt(bonus, Armor);
            int other = bonus - RangeAdjustment + AlternateRangeAdjustment;
            double low = PercentAt(Math.Min(bonus, other), Math.Max(Armor, AlternateArmor));
            double high = PercentAt(Math.Max(bonus, other), Math.Min(Armor, AlternateArmor));
            if (Format(low) == Format(high)) return Format(current);
            if (Math.Abs((current - low) - (high - current)) < 0.000001)
                return Format(current) + " (±" + Format(high - current) + "p)";
            return Format(current) + " (" + Format(low) + "–" + Format(high) + ")";
        }

        internal static string Format(double value)
        {
            if (double.IsNaN(value)) return "?";
            if (value > 0 && value < 0.05) return "<0.1%";
            if (value >= 99.95 && value < 100) return ">99.9%";
            return value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        internal string Details(bool ko)
        {
            var items = new List<string>();
            if (FlatFooted) items.Add(ko ? "무방비" : "flat-footed");
            if (ConcealmentMiss > 0) items.Add((SeparateMiss ? (ko ? "빗나갈 확률 " : "miss chance ") : (ko ? "은폐 " : "concealment ")) + ConcealmentMiss + "%" +
                (ConcealmentRerolls == 0 ? "" : (ko ? " 재굴림 ×" : " reroll ×") + ConcealmentRerolls));
            if (Images > 0) items.Add((ko ? "분신 " : "images ") + Images);
            if (FixedDie > 0) items.Add((ko ? "고정 d20 " : "fixed d20 ") + FixedDie);
            if (OnSuccess != 0 || OnFailure != 0 || conditionalDice.Count != 0) items.Add(ko ? "명중 재굴림" : "attack reroll");
            if (Conditional) items.Add(ko ? "조건부 보정 미확정" : "conditional modifiers pending");
            return items.Count == 0 ? "" : " · " + string.Join(" · ", items);
        }
    }
}
