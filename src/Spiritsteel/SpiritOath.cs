using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;

namespace Spiritsteel
{
    public sealed class SpiritOath : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>
    {
        public BlueprintFeatureReference Enemy;
        // RangerProgression advances the same favored enemy at 5, 10, 15 and 20.
        static int Bonus(UnitEntityData attacker)
        {
            int level = attacker.Progression.GetClassLevel(WeaponFeats.Barbarian);
            return level > 0 ? 2 * (1 + System.Math.Min(level, 20) / 5) : 0;
        }

        bool Matches(UnitEntityData attacker, ItemEntityWeapon weapon, UnitEntityData target)
        {
            if (target == null || !BoundWeapons.IsBound(attacker, weapon)) return false;
            foreach (var fact in Enemy.Get().GetComponent<FavoredEnemy>().CheckedFacts)
                if (target.Descriptor.HasFact(fact)) return true;
            return false;
        }
        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (Matches(evt.Initiator, evt.Weapon, evt.Target))
                evt.AddModifier(Bonus(evt.Initiator), Fact, ModifierDescriptor.FavoredEnemy);
        }
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (Matches(evt.Initiator, evt.Weapon, evt.AttackWithWeapon?.Target))
                evt.AddDamageModifier(Bonus(evt.Initiator), Fact, ModifierDescriptor.FavoredEnemy);
        }
        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
    }
}
