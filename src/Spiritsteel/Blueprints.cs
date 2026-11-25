using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spiritsteel
{
    internal static class Blueprints
    {
        internal static BlueprintBuff Native, Spirit;
        internal static BlueprintAbilityResource RageRounds;
        internal static BlueprintArchetype Archetype;
        internal static PrefabLink RageBurst;
        internal static BlueprintFeature Tactics, Trip;
        internal static BlueprintActivatableAbility Toggle;
        internal static bool Registered;
        static void Name(BlueprintUnitFact f, string key, Sprite icon)
        {
            if (!icon) throw new InvalidOperationException("Spiritsteel missing icon for " + key);
            SetField(f, "m_DisplayName", Strings.L(key + ".name"));
            SetField(f, "m_Description", Strings.L(key + ".desc"));
            SetField(f, "m_DescriptionShort", NoText());
            SetField(f, "m_Icon", icon);
        }
        static BlueprintFeature Feature(string guid, string key, Sprite icon)
        {
            return Create<BlueprintFeature>("SpiritsteelFeature_" + key, guid, f => {
                Name(f, key, icon); f.IsClassFeature = true; f.Ranks = 1;
            });
        }
        static BlueprintFeatureSelection Select(string id, string key, Sprite icon, IEnumerable<BlueprintFeature> options)
        {
            var refs = options.Distinct().Select(f => Ref<BlueprintFeatureReference>(f.AssetGuid.ToString())).ToArray();
            if (refs.Length == 0) throw new InvalidOperationException("Spiritsteel empty selection " + key);
            return Create<BlueprintFeatureSelection>("SpiritsteelSelection_" + key, id, f => {
                Name(f, key, icon); f.IsClassFeature = true; f.Ranks = 1;
                SetField(f, "m_Features", refs); SetField(f, "m_AllFeatures", refs);
            });
        }
        static LevelEntry Row(int level, params BlueprintFeatureBase[] facts)
        {
            var row = new LevelEntry { Level = level };
            row.SetFeatures(facts);
            return row;
        }
        internal static void BuildAll()
        {
            var barbarian = Get<BlueprintCharacterClass>(Guids.Barbarian);
            var powers = Get<BlueprintFeatureSelection>(Guids.RagePowers);
            var fast = Get<BlueprintFeature>(Guids.Fast);
            Native = Get<BlueprintBuff>(Guids.NativeRage);
            var nativeToggle = Get<BlueprintActivatableAbility>(Guids.NativeToggle);
            RageRounds = nativeToggle.ComponentsArray.OfType<ActivatableAbilityResourceLogic>().Single().RequiredResource
                ?? throw new InvalidOperationException("Barbarian rage has no daily resource.");
            var icon = nativeToggle.Icon;
            RageBurst = Native.ComponentsArray.OfType<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>()
                .Where(c => c.Activated?.Actions != null).SelectMany(c => c.Activated.Actions)
                .OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx>()
                .Single(a => a.PrefabLink?.AssetId == "a536a9fb639035140b8771b2e3a21ffd").PrefabLink;
            var sizeShift = typeof(Kingmaker.RuleSystem.Rules.RuleCalculateWeaponStats).GetField("m_SizeShift",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sizeShift?.FieldType != typeof(int))
                throw new InvalidOperationException("Native weapon size adjustment must be an integer.");
            var binding = BoundWeapons.Build(barbarian);
            var feats = WeaponFeats.Build(barbarian);
            Spirit = Create<BlueprintBuff>("SpiritsteelRageBuff", Guids.Buff, b => {
                Name(b, "rage", icon);
                b.FxOnStart = new PrefabLink(); b.FxOnRemove = new PrefabLink();
                b.Stacking = StackingType.Replace;
            });
            AddComponents(Spirit, new SpiritEffects());
            Toggle = Create<BlueprintActivatableAbility>("SpiritsteelRageToggle", Guids.Toggle, a => {
                Name(a, "rage", icon);
                SetField(a, "m_Buff", Ref<BlueprintBuffReference>(Guids.Buff));
                a.ActivationType = AbilityActivationType.Immediately;
                a.OnlyInCombat = true;
                a.DeactivateIfCombatEnded = true;
                a.DeactivateIfOwnerUnconscious = true;
            });
            AddComponents(Toggle, new RageGate { IgnoreIfStarted = true });
            var rage = Feature(Guids.Rage, "rage", icon);
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.Toggle) });
            AddComponents(rage, grant, new RageKeeper());
            // The native Solo Tactics feature carries a mechanic flag but has no icon.
            var teamworkIcon = Get<BlueprintFeature>(Guids.CoordinatedManeuvers).Icon ?? icon;
            Tactics = Feature(Guids.Tactics, "solo", teamworkIcon);
            AddComponents(Tactics, new BoundSolo());
            Trip = Feature(Guids.Trip, "trip", icon);
            var nativeTeams = new[] {Guids.InquisitorTeam, Guids.HunterTeam, Guids.CavalierTeam, Guids.PackTeam}
                .SelectMany(id => Get<BlueprintFeatureSelection>(id).AllFeatures).Distinct().ToArray();
            var team = Select(Guids.Team, "team", teamworkIcon, nativeTeams);
            var flexible = Select(Guids.Flexible, "choice", powers.Icon ?? icon, powers.AllFeatures.Concat(nativeTeams));
            var removed = new List<LevelEntry> { Row(1, fast) };
            var added = new List<LevelEntry> { Row(1, binding, rage) };
            for (int level = 2; level <= 20; level += 2)
            {
                removed.Add(Row(level, powers));
                added.Add(level == 2 ? Row(level, feats, Tactics, team) :
                    level == Rules.TripLevel ? Row(level, feats, Trip) : Row(level, feats, flexible));
            }
            foreach (var row in removed)
                if (!row.Features.All(f => barbarian.Progression.LevelEntries.Any(e => e.Level == row.Level && e.Features.Contains(f))))
                    throw new InvalidOperationException("Barbarian progression missing expected exchange at " + row.Level);
            var archetype = Create<BlueprintArchetype>("SpiritsteelArchetype", Guids.Archetype, a => {
                SetField(a, "m_ParentClass", barbarian);
                a.LocalizedName = Strings.L("class.name"); a.LocalizedDescription = Strings.L("class.desc");
                a.LocalizedDescriptionShort = Strings.L("class.short");
                a.RecommendedAttributes = barbarian.RecommendedAttributes.Concat(new[]{StatType.Wisdom}).Distinct().ToArray();
                a.NotRecommendedAttributes = barbarian.NotRecommendedAttributes.Where(t => t != StatType.Wisdom).ToArray();
                a.RemoveFeatures = removed.ToArray(); a.AddFeatures = added.ToArray();
            });
            RequireOwnLocalizedStrings(); RequireOwnBuffFx(true);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass), "m_Archetypes").GetValue(barbarian);
            if (old == null || old.Any(r => r == null || r.Guid.ToString() == Guids.Archetype))
                throw new InvalidOperationException("Invalid or duplicate Spiritsteel archetype entry.");
            SetField(barbarian, "m_Archetypes", old.Concat(new[] {Ref<BlueprintArchetypeReference>(Guids.Archetype)}).ToArray());
            Registered = barbarian.Archetypes.Count(a => a == archetype) == 1;
            if (!Registered) throw new InvalidOperationException("Spiritsteel registration failed.");
            Archetype = archetype;
        }
    }
}
