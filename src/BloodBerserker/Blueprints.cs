using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BloodBerserker
{
    internal static class Blueprints
    {
        internal static BlueprintCharacterClass Bloodrager;
        internal static BlueprintArchetype Archetype;
        internal static BlueprintFeature Training;
        internal static BlueprintBuff Rage, Knot, Ward, ShockBuff, VampBuff;
        internal static BlueprintAbility Shock, Vamp;
        internal static bool Registered;

        internal static void BuildAll()
        {
            ChargeSpell.BindImpact();
            Bloodrager = Get<BlueprintCharacterClass>(Guids.Bloodrager);
            Rage = Get<BlueprintBuff>(Guids.Rage);
            Shock = Get<BlueprintAbility>(Guids.Shock);
            Vamp = Get<BlueprintAbility>(Guids.Vamp);
            foreach (var spell in new[] { Shock, Vamp })
                if (spell.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility?.GetComponent<AbilityEffectRunAction>() == null)
                    throw new InvalidOperationException("Missing touch spell effect: " + spell.name);
            var spear = Get<BlueprintAbility>(Guids.Spear);
            var source = spear.GetComponent<AbilityDeliverProjectile>();
            if (source == null || !source.Projectiles.Any(p => p.AssetGuid.ToString() == Guids.Projectile) ||
                source.Weapon.AssetGuid.ToString() != Guids.RayWeapon)
                throw new InvalidOperationException("Blood chain projectile or weapon differs from installed chain spear.");

            BloodTether.BuildProjectile(Get<BlueprintProjectile>(Guids.Projectile));
            var speed = Get<BlueprintFeature>(Guids.Speed);
            var dr = Get<BlueprintFeature>(Guids.Reduction);
            var removals = new[] { Entry(1, speed) }.Concat(Rules.ReductionLevels.Select(l => Entry(l, dr))).ToArray();
            foreach (var row in removals)
                if (!Bloodrager.Progression.LevelEntries.Any(e => e.Level == row.Level && e.Features.Contains(row.Features.Single())))
                    throw new InvalidOperationException("Missing bloodrager class exchange at level " + row.Level);

            Knot = Buff(Guids.Knot, "knot", spear.Icon);
            AddComponents(Knot, new KnotWindow());
            Ward = Buff(Guids.Ward, "ward", Rage.Icon);
            var rank = new ContextRankConfig();
            SetField(rank, "m_BaseValueType", ContextRankBaseValueType.ClassLevel);
            SetField(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(Guids.Bloodrager) });
            AddComponents(Ward, rank, new TemporaryHitPointsFromAbilityValue {
                Value = new ContextValue { ValueType = ContextValueType.Rank },
                Descriptor = ModifierDescriptor.UntypedStackable
            });
            ShockBuff = Buff(Guids.ShockBuff, "chargeShock", Shock.Icon);
            VampBuff = Buff(Guids.VampBuff, "chargeVamp", Vamp.Icon);
            Chain(Guids.Chain, "chain", spear.Icon, false);
            Chain(Guids.LongChain, "reach", spear.Icon, true);
            Charge(Guids.ChargeShock, "chargeShock", Shock.Icon, false);
            Charge(Guids.ChargeVamp, "chargeVamp", Vamp.Icon, true);
            Training = Feature(Guids.Training, "chain", spear.Icon, Guids.Chain);
            AddComponents(Training, new KnotCombat());
            var weapon = Feature(Guids.Weapon, "weapon", Rage.Icon);
            AddComponents(weapon, new BloodWeapon(), new BloodBladeVisual());
            var reach = Feature(Guids.Reach, "reach", spear.Icon, Guids.LongChain);
            var conduction = Feature(Guids.Conduction, "conduction", Shock.Icon, Guids.ChargeShock, Guids.ChargeVamp);
            var critical = Feature(Guids.Critical, "critical", Vamp.Icon);
            Archetype = Create<BlueprintArchetype>("BloodBerserkerArchetype", Guids.Archetype, a => {
                SetField(a, "m_ParentClass", Bloodrager);
                a.LocalizedName = Strings.L("archetype.name");
                a.LocalizedDescription = Strings.L("archetype.desc");
                a.LocalizedDescriptionShort = Strings.L("archetype.short");
                a.RecommendedAttributes = Bloodrager.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = Bloodrager.NotRecommendedAttributes.ToArray();
                a.RemoveFeatures = removals;
                a.AddFeatures = new[] { Entry(1, Training, weapon), Entry(Rules.ReachLevel, reach),
                    Entry(Rules.ConductionLevel, conduction), Entry(Rules.CriticalLevel, critical) };
            });
            RequireOwnLocalizedStrings();
            RequireOwnBuffFx(true);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass), "m_Archetypes").GetValue(Bloodrager);
            if (old == null || old.Any(r => r == null || r.Guid.ToString() == Guids.Archetype))
                throw new InvalidOperationException("Invalid or duplicate bloodrager archetype.");
            SetField(Bloodrager, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Guids.Archetype) }).ToArray());
            Registered = Bloodrager.Archetypes.Count(a => a == Archetype) == 1;
            if (!Registered) throw new InvalidOperationException("Blood Berserker registration failed.");
        }

        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features)
        {
            var row = new LevelEntry { Level = level };
            row.SetFeatures(features);
            return row;
        }

        static void Name(BlueprintUnitFact f, string key, Sprite icon)
        {
            if (icon == null) throw new InvalidOperationException("Missing Blood Berserker icon: " + key);
            SetField(f, "m_DisplayName", Strings.L(key + ".name"));
            SetField(f, "m_Description", Strings.L(key + ".desc"));
            SetField(f, "m_DescriptionShort", NoText());
            SetField(f, "m_Icon", icon);
        }

        static BlueprintFeature Feature(string id, string key, Sprite icon, params string[] abilities)
        {
            var f = Create<BlueprintFeature>("BloodBerserkerFeature_" + key, id, x => {
                Name(x, key, icon); x.Ranks = 1; x.IsClassFeature = true;
            });
            if (abilities.Length > 0)
            {
                var grant = new AddFacts();
                SetField(grant, "m_Facts", abilities.Select(Ref<BlueprintUnitFactReference>).ToArray());
                AddComponents(f, grant);
            }
            return f;
        }

        static BlueprintBuff Buff(string id, string key, Sprite icon)
        {
            return Create<BlueprintBuff>("BloodBerserkerBuff_" + key, id, x => {
                Name(x, key, icon);
                x.Stacking = StackingType.Replace;
                x.FxOnStart = new PrefabLink();
                x.FxOnRemove = new PrefabLink();
            });
        }

        static BlueprintAbility Ability(string id, string key, Sprite icon, bool personal, int feet)
        {
            return Create<BlueprintAbility>("BloodBerserkerAbility_" + key, id, a => {
                Name(a, key, icon);
                a.Type = AbilityType.Supernatural;
                a.SpellResistance = false;
                a.ActionType = personal ? UnitCommand.CommandType.Swift : UnitCommand.CommandType.Move;
                a.Range = personal ? AbilityRange.Personal : AbilityRange.Custom;
                a.CustomRange = new Feet(feet);
                a.CanTargetEnemies = !personal;
                a.CanTargetSelf = personal;
                a.CanTargetPoint = a.CanTargetFriends = false;
                a.LocalizedDuration = a.LocalizedSavingThrow = NoText();
            });
        }

        static void Chain(string id, string key, Sprite icon, bool extended)
        {
            var a = Ability(id, key, icon, false, extended ? Rules.ExtendedFeet : Rules.ChainFeet);
            var flight = new ChainFlight { Extended = extended, Type = AbilityProjectileType.Simple, NeedAttackRoll = true };
            SetField(flight, "m_Projectiles", new[] { Ref<BlueprintProjectileReference>(Guids.BloodProjectile) });
            SetField(flight, "m_Weapon", Ref<BlueprintItemWeaponReference>(Guids.RayWeapon));
            AddComponents(a, flight, new ChainImpact(), new BloodrageRequired(), new ChainTarget { Extended = extended });
        }

        static void Charge(string id, string key, Sprite icon, bool vampiric)
        {
            var a = Ability(id, key, icon, true, 0);
            AddComponents(a, new BloodrageRequired(), new ChargeSpell { Vampiric = vampiric });
        }
    }
}
