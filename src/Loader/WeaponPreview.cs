using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Inspect;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using UnityEngine;

namespace HomebrewClasses.Loader
{
    internal static class WeaponPreview
    {
        // UIUtilityItem is the same target-independent calculation the inventory uses for each hand.
        static UnitEntityData owner;
        static float refreshAt;
        sealed class HandStats { internal int[] Attacks; internal string Damage; }
        static readonly Dictionary<ItemEntityWeapon, HandStats> cache = new Dictionary<ItemEntityWeapon, HandStats>();

        internal static void Clear()
        {
            owner = null;
            cache.Clear();
            refreshAt = 0;
        }

        static HandStats Sheet(UnitEntityData actor, WeaponSlot hand)
        {
            var weapon = hand.MaybeWeapon;
            if (cache.TryGetValue(weapon, out var stats)) return stats;
            var body = actor.Body;
            var slot = ReferenceEquals(hand, body.PrimaryHand) ? UIUtilityItem.AttackHandType.Primary :
                ReferenceEquals(hand, body.SecondaryHand) ? UIUtilityItem.AttackHandType.Secondary : UIUtilityItem.AttackHandType.Additional;
            var sheet = UIUtilityItem.GetAttackParametersEntity(weapon, actor.Descriptor, slot);
            stats = new HandStats {
                Attacks = sheet.Attacks?.ToArray(),
                Damage = sheet.DamageData != null && sheet.DamageData.Count > 0 ? UIUtilityItem.GetMinMaxDamageText(sheet.DamageData, 1f) : null
            };
            cache[weapon] = stats;
            return stats;
        }

        internal static string Describe(UnitEntityData actor, UnitEntityData target, bool ko, bool meleeOnly = false, bool showFullAttack = true)
        {
            if (actor == null || target == null || target.Destroyed || !actor.IsPlayerFaction || !actor.IsEnemy(target)) return null;
            var hand = meleeOnly ? actor.GetThreatHandMelee(false) : actor.GetThreatHand();
            var weapon = hand?.MaybeWeapon;
            if (weapon == null) return null;
            if (!ReferenceEquals(actor, owner) || Time.unscaledTime >= refreshAt)
            {
                cache.Clear();
                owner = actor;
                refreshAt = Time.unscaledTime + 1f;
            }
            var main = Sheet(actor, hand);
            bool revealed = ActionChecks.Revealed(target, UnitInfoPart.Defence);
            bool touch = weapon.Blueprint.AttackType.IsTouch();
            if (!revealed || main.Attacks == null || main.Attacks.Length == 0)
                return Numbers(main.Attacks != null && main.Attacks.Length > 0 ? (int?)main.Attacks[0] : null,
                    revealed ? (int?)(touch ? target.Stats.AC.Touch : target.Stats.AC.ModifiedValue) : null, touch, main.Damage, ko);
            var odds = AttackOdds.Read(actor, target, weapon.Blueprint.AttackType, true);
            var text = FormatForecast(odds, main.Attacks, touch, main.Damage, ko, showFullAttack);
            if (!showFullAttack) return text + (ko ? " · 기술 발동 보정 별도" : " · activation modifiers pending");
            var hands = new List<WeaponSlot> { actor.Body.PrimaryHand, actor.Body.SecondaryHand };
            if (actor.Body.AdditionalLimbs != null) hands.AddRange(actor.Body.AdditionalLimbs);
            var visited = new HashSet<ItemEntityWeapon> { weapon };
            var contexts = new Dictionary<AttackType, AttackOdds> { { weapon.Blueprint.AttackType, odds } };
            foreach (var other in hands)
            {
                if (other == null || !other.HasWeapon || !visited.Add(other.Weapon)) continue;
                var stats = Sheet(actor, other);
                if (stats.Attacks == null || stats.Attacks.Length == 0) continue;
                var type = other.Weapon.Blueprint.AttackType;
                if (!contexts.TryGetValue(type, out var side)) contexts[type] = side = AttackOdds.Read(actor, target, type, true);
                text += " · " + other.Weapon.Name + " " + string.Join("/", stats.Attacks.Select(v => side.At(v + side.TargetBonus, side.Armor)));
            }
            return text;
        }

        internal static string FormatForecast(AttackOdds odds, int[] attacks, bool touch, string range, bool ko, bool full)
        {
            int first = attacks[0] + odds.TargetBonus;
            var line = (ko ? "예상 본체 명중 " : "estimated hit on target ") + odds.At(first, odds.Armor)
                + " (" + first.ToString("+0;-0;+0") + (touch ? (ko ? " / 접촉 AC " : " / touch AC ") : " / AC ") + odds.Armor + ")";
            if (full && attacks.Length > 1)
                line += (ko ? " · 전력공격 후속타(현 상태) " : " · full-attack follow-ups (current state) ") +
                    string.Join("/", attacks.Skip(1).Select(b => odds.At(b + odds.TargetBonus, odds.Armor)));
            line += odds.Details(ko);
            if (!string.IsNullOrEmpty(range)) line += (ko ? " · 기본 피해 " : " · base damage ") + range;
            return line;
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
