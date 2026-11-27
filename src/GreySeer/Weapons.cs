using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;

namespace GreySeer
{
    internal static class Weapons
    {
        static bool IsMelee(ItemEntityWeapon weapon, WeaponCategory category)
        {
            return weapon != null && weapon.Blueprint.IsMelee && weapon.Blueprint.Category == category;
        }

        internal static HandSlot StrikeHand(UnitEntityData unit)
        {
            if (unit == null || !unit.Body.HandsAreEnabled) return null;
            var first = unit.Body.PrimaryHand;
            var second = unit.Body.SecondaryHand;
            if (!first.Active || !second.Active) return null;
            var a = first.MaybeItem as ItemEntityWeapon;
            var b = second.MaybeItem as ItemEntityWeapon;
            if (IsMelee(b, WeaponCategory.Quarterstaff) && WeaponChoices.Matches(unit, a)) return first;
            if (IsMelee(a, WeaponCategory.Quarterstaff) && WeaponChoices.Matches(unit, b)) return second;
            return null;
        }
    }

    [HarmonyPatch(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.CanTakeOneHand))]
    internal static class StaffGrip
    {
        static void Postfix(ItemEntityWeapon __instance, UnitEntityData unit, ref bool __result)
        {
            if (__result || unit == null || Blueprints.Training == null) return;
            var bp = __instance.Blueprint;
            if (bp == null)
            {
                WotrClassMod.Common.ModContext.LogError("Grey Seer: weapon has no blueprint: " + __instance.UniqueId);
                return;
            }
            if (bp.Category == WeaponCategory.Quarterstaff && bp.IsTwoHanded && bp.IsMelee && unit.Descriptor.HasFact(Blueprints.Training))
                __result = true;
        }
    }

    public class WeaponAccuracy : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            var sword = Weapons.StrikeHand(Owner);
            if (sword == null || evt.Weapon == null ||
                (!ReferenceEquals(evt.Weapon, sword.MaybeItem) && !ReferenceEquals(evt.Weapon, sword.PairSlot.MaybeItem))) return;
            if (!Owner.Progression.IsArchetype(Blueprints.Archetype)) return;
            int level = Owner.Progression.GetClassLevel(Blueprints.Arcanist);
            int delta = Blueprints.Fighter.BaseAttackBonus.GetBonus(level) - Blueprints.Arcanist.BaseAttackBonus.GetBonus(level);
            if (delta != 0) evt.AddModifier(delta, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
    }
}
