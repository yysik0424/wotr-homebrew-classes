using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;

namespace HomebrewClasses.Loader
{
    internal static class HitEstimate
    {
        // WotR ordinary attack roll: a natural one misses and a natural twenty hits.
        internal static int Percent(int attack, int armor)
        {
            return 5 * Math.Min(19, Math.Max(1, 21 + attack - armor));
        }

        internal static string Band(int current, int low, int high)
        {
            if (low == high) return current + "%";
            if (current - low == high - current) return current + "% (±" + (high - current) + "%p)";
            return current + "% (" + low + "–" + high + "%)";
        }

        internal static string Touch(AbilityData ability, UnitEntityData target, AttackType type, bool ko)
        {
            if (type != AttackType.Touch && type != AttackType.RangedTouch) return null;
            var caster = ability.Caster.Unit;
            if (!ActionChecks.Revealed(caster, Kingmaker.Inspect.UnitInfoPart.Offence)) return null;
            var delivery = ability.Blueprint.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility ?? ability.Blueprint;
            var projectile = delivery.GetComponent<AbilityDeliverProjectile>();
            bool ranged = type == AttackType.RangedTouch;
            var stat = projectile != null && projectile.ReplaceAttackRollBonusStat ? projectile.AttackRollBonusStat :
                ranged ? StatType.Dexterity : StatType.Strength;
            var attribute = caster.Stats.GetStat(stat) as ModifiableValueAttributeStat;
            if (attribute == null) return null;
            var facts = caster.Facts.List.Where(f => f.Active);
            int bonus = caster.Stats.BaseAttackBonus.ModifiedValue + caster.Stats.AdditionalAttackBonus.ModifiedValue + attribute.Bonus;
            if (ranged && (target.CombatState.EngagedBy.Any(u => u != caster && !caster.IsEnemy(u)) ||
                target.CombatState.EngagedUnits.Any(u => u != caster && !caster.IsEnemy(u))) &&
                !facts.Any(f => f.Blueprint.GetComponent<PreciseShot>() != null)) bonus -= 4;
            if (!ranged && target.CombatState.IsFlanked) bonus += 2;
            int ac = target.Stats.AC.Touch;
            int flat = target.Stats.AC.FlatFootedTouch;
            int chance = Percent(bonus, ac);
            int lowBonus = bonus, highBonus = bonus;
            string variable = null;
            if (ranged)
            {
                // DistanceAttackBonus checks center distance against range plus both units' corpulence.
                // A single component can be shown without guessing stacking rules of multiple mods.
                var distances = facts.SelectMany(f => f.Blueprint.GetComponents<DistanceAttackBonus>()).Take(2).ToArray();
                if (distances.Length == 1 && distances[0].AttackBonus != 0)
                {
                    var d = distances[0];
                    var within = caster.DistanceTo(target) <= d.Range.Meters + caster.Corpulence + target.Corpulence;
                    int inside = Percent(bonus + d.AttackBonus, ac);
                    chance = within ? inside : chance;
                    variable = (ko ? "거리 조건 안/밖 " : "inside/outside range ") + inside + "%/" + Percent(bonus, ac) + "%";
                    lowBonus += Math.Min(0, d.AttackBonus);
                    highBonus += Math.Max(0, d.AttackBonus);
                    if (within) bonus += d.AttackBonus;
                }
            }
            int flatChance = Percent(bonus, flat);
            string band = Band(chance, Percent(lowBonus, Math.Max(ac, flat)), Percent(highBonus, Math.Min(ac, flat)));
            string text = (ko ? "기본 접촉 명중 추정 " : "estimated base touch hit ") + band;
            if (flatChance != chance) text += (ko ? " · 무방비 시 " : " · if flat-footed ") + flatChance + "%";
            if (variable != null) text += " · " + variable;
            return text;
        }
    }
}
