using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using WotrClassMod.Common;

namespace Falconer
{
    public sealed class DiveAvailable : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData u) => !Hunting.Spent(u);
        public string GetAbilityCasterRestrictionUIText() => Strings.Resolve(WotrClassMod.Common.ModContext.StringPrefix + "Dive.desc");
    }

    public sealed class HawkCommand : AbilityApplyEffect
    {
        public bool IsEye;
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var owner = context.Caster;
            if (IsEye)
                owner.Buffs.AddBuff(Blueprints.Eye, context, new Rounds(Rules.EyeRounds(owner.Stats.Wisdom.Bonus)).Seconds);
            else
                Hunting.Dive(owner, target.Unit);
        }
    }

    internal static class Hunting
    {
        [ThreadStatic]
        internal static HawkFlight Strike;
        [ThreadStatic]
        internal static int Index;
        [ThreadStatic]
        internal static Buff Drive;
        [ThreadStatic]
        internal static HashSet<Buff> SingleAttackBuffs;
        internal static bool Spent(UnitEntityData owner)
        {
            var state = owner.Get<HawkState>();
            return state != null && state.Spent && (owner.IsInCombat || Game.Instance.TimeController.GameTime.Ticks < state.AvailableAt);
        }

        internal static Buff Find(UnitEntityData target, BlueprintBuff bp, UnitEntityData owner) => target?.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint == bp && b.Context?.MaybeCaster == owner);
        internal static void Mark(UnitEntityData target, BlueprintBuff bp, UnitEntityData owner)
        {
            var found = Find(target, bp, owner);
            var round = new Rounds(1).Seconds;
            if (found != null)
                found.SetDuration(round);
            else
                target.Buffs.AddBuff(bp, owner, round);
        }

        internal static bool Arrow(ItemEntityWeapon weapon) => weapon != null && (weapon.Blueprint.Category == WeaponCategory.Longbow || weapon.Blueprint.Category == WeaponCategory.Shortbow);
        internal static void Hit(UnitEntityData owner, UnitEntityData foe, bool bird)
        {
            if (bird)
            {
                Mark(foe, Blueprints.Shot, owner);
                if (owner.Descriptor.HasFact(Blueprints.Cover))
                    Mark(foe, Blueprints.Covered, owner);
            }
            else
                Mark(foe, Blueprints.Driven, owner);
            if (!owner.Descriptor.HasFact(Blueprints.Hunt))
                return;
            Mark(foe, bird ? Blueprints.HawkHit : Blueprints.PetHit, owner);
            if (Find(foe, bird ? Blueprints.PetHit : Blueprints.HawkHit, owner) != null && owner.Get<HawkState>()?.PrecisionSpent != true)
            {
                Mark(foe, Blueprints.Shot, owner);
                Mark(foe, Blueprints.Precision, owner);
            }
        }

        internal static ItemEntityWeapon[] DiveWeapons(ItemEntityWeapon primary, ItemEntityWeapon secondary, IEnumerable<ItemEntityWeapon> additional)
        {
            var weapons = new[] { primary, secondary }.Concat(additional).Where(weapon => weapon != null).Distinct().ToArray();
            var claws = weapons.Where(weapon => weapon.Blueprint.Category == WeaponCategory.Claw).Take(2).ToArray();
            var bite = weapons.FirstOrDefault(weapon => weapon.Blueprint.Category == WeaponCategory.Bite);
            if (claws.Length != 2 || bite == null)
                throw new InvalidOperationException("Falconer: eagle requires two talons and a beak.");
            return new[] { claws[0], claws[1], bite };
        }

        internal static void Dive(UnitEntityData owner, UnitEntityData foe)
        {
            if (Strike != null || foe == null || foe.State.IsDead || !owner.IsEnemy(foe) || !owner.State.CanAct)
                return;
            var state = owner.Ensure<HawkState>();
            if (Spent(owner))
                return;
            var flight = Flight.Ensure(owner);
            flight.SyncSpells();
            // UnitBody.Initialize places the eagle's first natural weapon (bite) in the primary hand when ordinary hands are disabled.
            var body = flight.Bird.Body;
            var weapons = DiveWeapons(body.PrimaryHand.MaybeItem as ItemEntityWeapon, body.SecondaryHand.MaybeItem as ItemEntityWeapon,
                body.AdditionalLimbs.Select(slot => slot.MaybeWeapon));
            state.Spent = true;
            state.AvailableAt = (Game.Instance.TimeController.GameTime + new Rounds(1).Seconds).Ticks;
            flight.DiveAt(foe);
            int count = Rules.Strikes(Flight.Level(owner));
            var driven = Find(foe, Blueprints.Driven, owner);
            try
            {
                Strike = flight;
                Drive = driven;
                SingleAttackBuffs = new HashSet<Buff>();
                for (Index = 0; Index < count && !foe.State.IsDead; Index++)
                {
                    var weapon = weapons[Index];
                    var attack = new RuleAttackWithWeapon(flight.Bird, foe, weapon, Index * 5)
                    {
                        SkipAnimation = true,
                        IsFirstAttack = Index == 0,
                        AttackNumber = Index,
                        AttacksCount = count
                    };
                    Rulebook.Trigger(attack);
                    if (attack.AttackRoll != null && !attack.AttackRoll.IsFake)
                        foreach (var buff in SingleAttackBuffs)
                            if (owner.Buffs.Enumerable.Contains(buff))
                                buff.Remove();
                    SingleAttackBuffs.Clear();
                    if (attack.AttackRoll?.IsHit == true)
                        Hit(owner, foe, true);
                }
            }
            finally
            {
                if (driven != null)
                    driven.Remove();
                Strike = null;
                Drive = null;
                SingleAttackBuffs = null;
                Index = 0;
            }
        }
    }

    public sealed class HawkTraining : UnitFactComponentDelegate, IUnitNewCombatRoundHandler, IUnitCombatHandler, IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IInitiatorRulebookHandler<RuleAttackRoll>, IInitiatorRulebookHandler<RulePrepareDamage>
    {
        sealed class PrecisionArrow
        {
        }

        static readonly WeakKeyTable<RuleAttackRoll, PrecisionArrow> arrows = new WeakKeyTable<RuleAttackRoll, PrecisionArrow>();
        void Reset(UnitEntityData u)
        {
            if (u == Owner && u.Get<HawkState>()is HawkState s)
            {
                s.Spent = false;
                s.PrecisionSpent = false;
            }
        }

        public void HandleNewCombatRound(UnitEntityData u) => Reset(u);
        public void HandleUnitJoinCombat(UnitEntityData u)
        {
            if (u == Owner && u.Get<HawkState>()is HawkState s && Game.Instance.TimeController.GameTime.Ticks >= s.AvailableAt)
                Reset(u);
        }

        public void HandleUnitLeaveCombat(UnitEntityData u)
        {
        }

        protected override void OnTurnOff()
        {
            Flight.Remove(Owner);
        }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus e)
        {
            if (Hunting.Arrow(e.Weapon) && Hunting.Find(e.Target, Blueprints.Shot, Owner)is Buff b)
                e.AddModifier(Rules.ShotBonus, b, ModifierDescriptor.Circumstance);
        }

        public void OnEventDidTrigger(RuleAttackRoll e)
        {
            if (e.IsFake || !Hunting.Arrow(e.Weapon))
                return;
            var shot = Hunting.Find(e.Target, Blueprints.Shot, Owner);
            if (shot == null)
                return;
            shot.Remove();
            var precision = Hunting.Find(e.Target, Blueprints.Precision, Owner);
            if (precision == null)
                return;
            precision.Remove();
            var s = Owner.Ensure<HawkState>();
            if (s.PrecisionSpent)
                return;
            s.PrecisionSpent = true;
            if (e.IsHit)
                arrows.Add(e, new PrecisionArrow());
        }

        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            var roll = evt.ParentRule?.AttackRoll;
            if (roll == null || !arrows.TryGetValue(roll, out _))
                return;
            arrows.Remove(roll);
            evt.Add(new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(2, DiceType.D6)), 0, PhysicalDamageForm.Piercing) { Precision = true, SourceFact = Fact });
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus e)
        {
        }

        public void OnEventAboutToTrigger(RuleAttackRoll e)
        {
        }

        public void OnEventDidTrigger(RulePrepareDamage e)
        {
        }
    }

    public sealed class HawkStrikeEffects : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookHandler<RuleAttackRoll>
    {
        [ThreadStatic]
        static bool sampling;
        bool Ours => !sampling && Hunting.Strike?.Bird == Owner;

        // Native attack rules separate fixed stat bonuses from conditional bonuses. Read both
        // so that morale/insight from the pet and the ranger still share one stacking limit.
        static List<Modifier> Bonuses(RuleCalculateAttackBonus sample)
        {
            var modifiers = sample.AllBonuses.Where(m => m.Value > 0).ToList();
            foreach (var m in sample.StatModifiersAtTheMoment)
                if (m.ModValue > 0 && !modifiers.Any(v => v.Fact == m.Source && v.Value == m.ModValue && v.Descriptor == m.ModDescriptor))
                    modifiers.Add(new Modifier(m.ModValue, m.Source, m.ModDescriptor));
            return modifiers;
        }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus attack)
        {
            if (!Ours)
                return;
            var ranger = Hunting.Strike.Owner;
            var nativeStat = Owner.Stats.GetStat(attack.AttackBonusStat) as ModifiableValueAttributeStat;
            int training = Flight.Level(ranger) + ranger.Stats.Wisdom.Bonus + Rules.DiveBonus;
            attack.AddModifier(training - Owner.Stats.BaseAttackBonus.ModifiedValue - (nativeStat?.Bonus ?? 0), Fact, ModifierDescriptor.UntypedStackable);
            ranger.Get<UnitPartFavoredEnemy>()?.OnEventAboutToTrigger(attack);
            List<Modifier> native, inherited;
            sampling = true;
            try
            {
                native = Bonuses(Rulebook.Trigger(new RuleCalculateAttackBonus(Owner, attack.Target, attack.Weapon, 0)));
                inherited = Bonuses(Rulebook.Trigger(new RuleCalculateAttackBonus(ranger, attack.Target, attack.Weapon, 0))).Where(m => m.Fact is Buff b && b.Owner == ranger.Descriptor).ToList();
            }
            finally
            {
                sampling = false;
            }

            if (Hunting.Drive != null)
                inherited.Add(new Modifier(Rules.DrivingBonus, Hunting.Drive, ModifierDescriptor.Circumstance));
            if (Hunting.Index == 0 && ranger.Buffs.HasFact(Blueprints.Eye))
                inherited.Add(new Modifier(Rules.EyeBonus, Fact, ModifierDescriptor.Insight));
            foreach (var group in inherited.GroupBy(m => m.Descriptor))
            {
                if (group.First().IsStackable)
                {
                    foreach (var m in group)
                    {
                        if (m.Fact is Buff b && Owner.Buffs.HasFact(b.Blueprint))
                            continue;
                        attack.AddModifier(m.Value, m.Fact, m.Descriptor);
                    }
                }
                else
                {
                    var best = group.OrderByDescending(m => m.Value).First();
                    int existing = native.Where(m => m.Descriptor == group.Key).Select(m => m.Value).DefaultIfEmpty().Max();
                    int extra = Rules.MissingBonus(best.Value, existing);
                    if (extra > 0)
                        attack.AddModifier(extra, best.Fact, ModifierDescriptor.UntypedStackable);
                }
            }

            foreach (var buff in inherited.Select(m => m.Fact).OfType<Buff>())
                if (buff.Owner == ranger.Descriptor && buff.Blueprint.GetComponent<RemoveBuffOnAttack>() != null)
                    Hunting.SingleAttackBuffs.Add(buff);
        }

        public void OnEventAboutToTrigger(RuleAttackRoll e)
        {
            if (Ours && Hunting.Index == 0 && Hunting.Strike.Owner.Buffs.HasFact(Blueprints.Eye))
                e.IgnoreConcealment = true;
        }

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats e)
        {
            if (!Ours)
                return;
            var level = Flight.Level(Hunting.Strike.Owner);
            var die = (DiceType)Rules.Die(level);
            e.WeaponDamageDice.Modify(new DiceFormula(1, die), Fact);
            e.DoNotScaleDamage = true;
            e.IsSecondaryOverride = false;
            e.DamageModifiers.Add(new Modifier(level / 2, Fact, ModifierDescriptor.UntypedStackable));
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus e)
        {
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats e)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll e)
        {
        }
    }

    [HarmonyPatch(typeof(RuleAttackWithWeaponResolve), nameof(RuleAttackWithWeaponResolve.OnTrigger))]
    static class HuntingAttack
    {
        static void Postfix(RuleAttackWithWeaponResolve __instance)
        {
            var attack = __instance.AttackWithWeapon;
            if (Blueprints.Archetype == null || Hunting.Strike != null || attack?.Weapon == null || attack.AttackRoll == null || attack.AttackRoll.IsFake)
                return;
            var user = attack.Initiator;
            var foe = attack.Target;
            if (foe == null || !user.IsEnemy(foe) || foe.State.IsDead || !user.IsInCombat)
                return;
            if (attack.Weapon.Blueprint.Category == WeaponCategory.Ray || attack.Weapon.Blueprint.Category == WeaponCategory.Touch)
                return;
            var master = user.Master;
            if (attack.AttackRoll.IsHit && master != null && master.Descriptor.HasFact(Blueprints.Drive) && Flight.Pet(master) == user)
                Hunting.Hit(master, foe, false);
            if (user.Descriptor.HasFact(Blueprints.Auto))
                Hunting.Dive(user, foe);
        }
    }

    [HarmonyPatch(typeof(UnitCombatState), nameof(UnitCombatState.AttackOfOpportunity))]
    static class WingCover
    {
        static bool Prefix(UnitCombatState __instance, UnitEntityData target, bool disengaging)
        {
            var master = target?.Master;
            return !disengaging || master == null || Blueprints.Archetype == null || !master.Descriptor.HasFact(Blueprints.Cover) || Flight.Pet(master) != target || Hunting.Find(__instance.Unit, Blueprints.Covered, master) == null;
        }
    }
}
