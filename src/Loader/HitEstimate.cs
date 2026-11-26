using System;
using System.Linq;
using Kingmaker.UnitLogic;
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

        internal static string Ability(AbilityData ability, UnitEntityData target, AttackType type, bool ko)
        {
            var caster = ability.Caster.Unit;
            if (!ActionChecks.Revealed(caster, Kingmaker.Inspect.UnitInfoPart.Offence)) return null;
            var delivery = ability.Blueprint.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility ?? ability.Blueprint;
            var projectile = delivery.GetComponent<AbilityDeliverProjectile>();
            bool touch = type == AttackType.Touch || type == AttackType.RangedTouch;
            bool ranged = type == AttackType.RangedTouch || type == AttackType.Ranged;
            var stat = projectile != null && projectile.ReplaceAttackRollBonusStat ? (caster.State.Features.DomainsByIntelligenceFeature ? StatType.Intelligence : projectile.AttackRollBonusStat) :
                projectile?.Weapon != null && projectile.Weapon.AttackBonusStat != StatType.Unknown ? projectile.Weapon.AttackBonusStat :
                ranged ? StatType.Dexterity : StatType.Strength;
            var attribute = caster.Stats.GetStat(stat) as ModifiableValueAttributeStat;
            if (attribute == null) return null;
            var odds = AttackOdds.Read(caster, target, type, projectile?.IsHandOfTheApprentice == true);
            var spellWeapon = projectile?.Weapon;
            int focusBonus = 0;
            if (spellWeapon != null)
                foreach (var fact in caster.Facts.List.Where(f => f.Active))
                foreach (var component in fact.Blueprint.ComponentsArray)
                {
                    if (component is WeaponFocus focus && focus.WeaponType == spellWeapon.Type)
                        focusBonus += focus.AttackBonus * fact.GetRank();
                    if (component is WeaponFocusParametrized choice && fact is Feature feature && feature.Param == spellWeapon.Type.Category)
                        focusBonus += caster.Progression.Features.Enumerable.Any(f => f.Param == feature.Param && f.Blueprint == choice.MythicFocus) ? 2 : 1;
                }
            int bonus = caster.Stats.BaseAttackBonus.ModifiedValue + caster.Stats.AdditionalAttackBonus.ModifiedValue + attribute.Bonus + focusBonus + odds.TargetBonus;
            string chance = odds.BandAt(bonus);
            string text = (touch ? (ko ? "예상 본체 접촉 명중 " : "estimated touch hit on target ") : (ko ? "예상 본체 명중 " : "estimated hit on target "))
                + chance + " (" + bonus.ToString("+0;-0;+0") + (touch ? (ko ? " / 접촉 AC " : " / touch AC ") : " / AC ") + odds.Armor + ")";
            if (odds.RangeAdjustment != odds.AlternateRangeAdjustment)
                text += (ko ? " · 거리 조건 변경 시 " : " · other range ") +
                    odds.At(bonus - odds.RangeAdjustment + odds.AlternateRangeAdjustment, odds.Armor);
            int flat = touch ? target.Stats.AC.FlatFootedTouch : target.Stats.AC.FlatFooted;
            int ordinary = touch ? target.Stats.AC.Touch : target.Stats.AC.ModifiedValue;
            if (!odds.FlatFooted && flat != ordinary)
                text += (ko ? " · 무방비 시 " : " · if flat-footed ") + odds.At(bonus, odds.Armor + flat - ordinary);
            text += odds.Details(ko);
            return text;
        }
    }
}
