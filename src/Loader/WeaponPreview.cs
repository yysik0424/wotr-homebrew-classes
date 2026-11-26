using Kingmaker.EntitySystem.Entities;
using Kingmaker.Inspect;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using UnityEngine;

namespace HomebrewClasses.Loader
{
    internal static class WeaponPreview
    {
        // Use the inventory's target-independent calculations for the lead attack. Never issue an attack command.
        static UnitEntityData owner;
        static ItemEntityWeapon cachedWeapon;
        static float refreshAt;
        static int? attack;
        static string damage;

        internal static void Clear()
        {
            owner = null;
            cachedWeapon = null;
            refreshAt = 0;
        }

        internal static string Describe(UnitEntityData actor, UnitEntityData target, bool ko, bool meleeOnly = false)
        {
            if (actor == null || target == null || target.Destroyed || !actor.IsPlayerFaction || !actor.IsEnemy(target)) return null;
            var hand = meleeOnly ? actor.GetThreatHandMelee(false) : actor.GetThreatHand();
            var weapon = hand?.MaybeWeapon;
            if (weapon == null) return null;
            if (!ReferenceEquals(actor, owner) || !ReferenceEquals(weapon, cachedWeapon) || Time.unscaledTime >= refreshAt)
            {
                var body = actor.Body;
                var slot = ReferenceEquals(hand, body.PrimaryHand) ? UIUtilityItem.AttackHandType.Primary :
                    ReferenceEquals(hand, body.SecondaryHand) ? UIUtilityItem.AttackHandType.Secondary : UIUtilityItem.AttackHandType.Additional;
                var sheet = UIUtilityItem.GetAttackParametersEntity(weapon, actor.Descriptor, slot);
                attack = sheet.Attacks != null && sheet.Attacks.Count > 0 ? (int?)sheet.Attacks[0] : null;
                damage = sheet.DamageData != null && sheet.DamageData.Count > 0 ? UIUtilityItem.GetMinMaxDamageText(sheet.DamageData, 1f) : null;
                owner = actor;
                cachedWeapon = weapon;
                refreshAt = Time.unscaledTime + 2f;
            }
            bool revealed = ActionChecks.Revealed(target, UnitInfoPart.Defence);
            var type = weapon.Blueprint.AttackType;
            bool touch = type == AttackType.Touch || type == AttackType.RangedTouch;
            int? armor = revealed ? (int?)(touch ? target.Stats.AC.Touch : target.Stats.AC.ModifiedValue) : null;
            return Numbers(attack, armor, touch, damage, ko);
        }

        internal static string Numbers(int? bonus, int? armor, bool touch, string range, bool ko)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (bonus.HasValue)
            {
                string line = (ko ? "첫 타격 명중 " : "first attack ") + bonus.Value.ToString("+0;-0;+0");
                if (armor.HasValue)
                    line += (touch ? (ko ? " / 접촉 AC " : " / touch AC ") : " / AC ") + armor +
                        (ko ? " · 기본 명중률 " : " · base hit ") + HitEstimate.Percent(bonus.Value, armor.Value) + "%";
                parts.Add(line);
            }
            if (!armor.HasValue) parts.Add(ko ? "대상 방어 미확인" : "target defenses unknown");
            if (!string.IsNullOrEmpty(range)) parts.Add((ko ? "기본 피해 " : "base damage ") + range);
            return string.Join(" · ", parts);
        }
    }
}
