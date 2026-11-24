using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace DreamThief
{
    internal static class Blueprints
    {
        internal static BlueprintCharacterClass Rogue;
        internal static BlueprintArchetype Archetype;
        internal static BlueprintAbilityResource Starlight;
        internal static BlueprintBuff Shroud, Immune, Nightmare;
        internal static BlueprintAbility SleepSource;
        internal static float KnifeFlightSpeed;
        internal static AbilityCustomDimensionDoor GateFx;
        internal static bool Registered;

        // These keys are save identifiers. Never derive them from localized names or row positions.
        internal static string Id(string key)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("WotrClassMod.DreamThief.v1/" + key)), 0, 16).Replace("-", "").ToLowerInvariant();
        }

        internal static void Name(BlueprintUnitFact fact, string key, Sprite icon)
        {
            SetField(fact, "m_DisplayName", Strings.L(key + ".name"));
            SetField(fact, "m_Description", Strings.L(key + ".desc"));
            SetField(fact, "m_DescriptionShort", NoText());
            SetField(fact, "m_Icon", icon);
        }

        internal static BlueprintFeature Feature(string id, string key, Sprite icon, params BlueprintUnitFact[] facts)
        {
            var feature = Create<BlueprintFeature>("DreamThief_" + id, Id(id), f => {
                Name(f, key, icon); f.Ranks = 1; f.IsClassFeature = true;
            });
            if (facts.Length > 0) Grant(feature, facts);
            return feature;
        }

        internal static void Grant(BlueprintFeature feature, params BlueprintUnitFact[] facts)
        {
            var grant = new AddFacts();
            SetField(grant, "m_Facts", facts.Select(f => Ref<BlueprintUnitFactReference>(f.AssetGuid.ToString())).ToArray());
            AddComponents(feature, grant);
        }

        internal static BlueprintBuff Buff(string id, string key, Sprite icon)
        {
            return Create<BlueprintBuff>("DreamThief_" + id, Id(id), f => {
                Name(f, key, icon); f.Stacking = StackingType.Replace;
                f.FxOnStart = new PrefabLink(); f.FxOnRemove = new PrefabLink();
            });
        }

        internal static void Cost(BlueprintAbility ability, int amount)
        {
            if (amount == 0) return;
            var cost = new AbilityResourceLogic { Amount = amount };
            SetField(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Id("StarlightResource")));
            SetField(cost, "m_IsSpendResource", true);
            AddComponents(ability, cost);
        }

        static BlueprintAbility Ability(string id, string key, Sprite icon, int feet, UnitCommand.CommandType action, int cost)
        {
            bool personal = feet == 0;
            var ability = Create<BlueprintAbility>("DreamThief_" + id, Id(id), a => {
                Name(a, key, icon); a.Type = AbilityType.Supernatural; a.ActionType = action; a.SpellResistance = false;
                a.Range = personal ? AbilityRange.Personal : AbilityRange.Custom;
                a.CustomRange = new Feet(feet); a.CanTargetEnemies = !personal; a.CanTargetSelf = personal;
                a.CanTargetPoint = a.CanTargetFriends = false;
                a.LocalizedDuration = a.LocalizedSavingThrow = NoText();
            });
            Cost(ability, cost);
            return ability;
        }

        static LevelEntry Entry(int level, IEnumerable<BlueprintFeatureBase> features)
        {
            var row = new LevelEntry { Level = level }; row.SetFeatures(features.ToArray()); return row;
        }

        internal static void BuildAll()
        {
            Rogue = Get<BlueprintCharacterClass>(GameAssets.Rogue);
            var trap = Get<BlueprintFeature>(GameAssets.TrapFinding);
            var danger = Get<BlueprintFeature>(GameAssets.DangerSense);
            var normal = Get<BlueprintFeatureSelection>(GameAssets.TalentSelection);
            var desna = Get<BlueprintFeature>(GameAssets.Desna);
            var knife = Get<BlueprintFeature>(GameAssets.KnifeProficiency);
            SleepSource = Get<BlueprintAbility>(DreamTalents.All.Single(x => x.Key == "Slumber").SourceId);
            var starIcon = desna.Icon ?? SleepSource.Icon;
            var knifeIcon = knife.Icon ?? starIcon;
            var door = Get<BlueprintAbility>("5bdc37e4acfa209408334326076a43bc");
            GateFx = door.GetComponent<AbilityCustomDimensionDoor>() ?? throw new InvalidOperationException("Missing dimension door FX.");
            var teleIcon = door.Icon;
            // Use the speed of the game's physical throwing axe. The visible model comes from the equipped knife.
            KnifeFlightSpeed = Get<BlueprintProjectile>("dbcc51cfd11fc1441a495daf9df9b340").Speed;
            if (!(KnifeFlightSpeed > 0)) throw new InvalidOperationException("Invalid thrown weapon speed.");
            Starlight = Create<BlueprintAbilityResource>("DreamThief_StarlightResource", Id("StarlightResource"), r => {
                r.LocalizedName = Strings.L("pool.name"); r.LocalizedDescription = Strings.L("pool.desc");
                var amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                SetField(r, "m_MaxAmount", amount);
                SetField(r, "m_Icon", starIcon);
            });
            var pool = Feature("StarlightFeature", "pool", starIcon);
            var resourceGrant = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
            SetField(resourceGrant, "m_Resource", Ref<BlueprintAbilityResourceReference>(Starlight.AssetGuid.ToString()));
            AddComponents(pool, resourceGrant, new DreamTraining());
            Immune = Buff("DreamwardBuff", "immune", SleepSource.Icon);
            Shroud = Buff("ShroudBuff", "shroud", starIcon);
            AddComponents(Shroud, new ShroudCombat());
            var star = Ability("FallingStar", "throw", knifeIcon, DreamRules.ThrowFeet, UnitCommand.CommandType.Standard, 0);
            var dream = Ability("DreamThrow", "dream", SleepSource.Icon, DreamRules.ThrowFeet, UnitCommand.CommandType.Standard, 1);
            foreach (var throwAbility in new[] { star, dream })
            {
                AddComponents(throwAbility, new KnifeRequired(), new KnifeFlight(), new ThrowKnife { Sleep = throwAbility == dream });
            }
            var shroud = Ability("ShroudAbility", "shroud", starIcon, 0, UnitCommand.CommandType.Swift, 1);
            AddComponents(shroud, new ActivateShroud());
            var walkFeatures = new List<BlueprintFeature>();
            for (int i = 0; i < 3; i++)
            {
                string key = new[] { "walk", "reach", "swiftWalk" }[i];
                var ability = Ability("Dreamwalk" + i, "walk", teleIcon, i == 0 ? DreamRules.ThrowFeet : DreamRules.ExtendedFeet,
                    i == 2 ? UnitCommand.CommandType.Move : UnitCommand.CommandType.Standard, 1);
                AddComponents(ability, new KnifeRequired(), new WalkTier { Tier = i }, new Dreamwalk());
                walkFeatures.Add(Feature("WalkFeature" + i, key, teleIcon, ability));
            }
            var talents = TalentBlueprints.Build(normal);
            var removed = new List<LevelEntry>(); var added = new List<LevelEntry>();
            for (int level = 1; level <= 20; level++)
            {
                var lost = new List<BlueprintFeatureBase>(); var gained = new List<BlueprintFeatureBase>();
                if (level == 1)
                {
                    lost.Add(trap);
                    gained.AddRange(new[] { knife, pool, Feature("ThrowFeature", "throw", knifeIcon, star),
                        Feature("DreamThrowFeature", "dream", SleepSource.Icon, dream), Feature("WakingFeature", "waking", knifeIcon) });
                }
                if (level % 3 == 0 && level <= 18) lost.Add(danger);
                if (level % 2 == 0) { lost.Add(normal); gained.Add(talents); }
                if (level == DreamRules.ShroudLevel) gained.Add(Feature("ShroudFeature", "shroud", starIcon, shroud));
                if (level == DreamRules.DreamwalkLevel) gained.Add(walkFeatures[0]);
                if (level == DreamRules.ReachLevel) gained.Add(walkFeatures[1]);
                if (level == DreamRules.BurstLevel) gained.Add(Feature("BurstFeature", "burst", starIcon));
                if (level == DreamRules.MoveLevel) gained.Add(walkFeatures[2]);
                if (level == DreamRules.ReserveLevel) gained.Add(Feature("ReserveFeature", "reserve", starIcon));
                if (lost.Count > 0)
                {
                    if (!lost.All(f => Rogue.Progression.LevelEntries.Any(r => r.Level == level && r.Features.Contains(f))))
                        throw new InvalidOperationException("Missing Rogue class exchange at " + level);
                    removed.Add(Entry(level, lost));
                }
                if (gained.Count > 0) added.Add(Entry(level, gained));
            }
            Archetype = Create<BlueprintArchetype>("DreamThiefArchetype", Id("Archetype"), a => {
                SetField(a, "m_ParentClass", Rogue); a.LocalizedName = Strings.L("archetype.name");
                a.LocalizedDescription = a.LocalizedDescriptionShort = Strings.L("archetype.desc");
                a.OverrideAttributeRecommendations = true;
                a.RecommendedAttributes = new[] { StatType.Dexterity, StatType.Charisma };
                a.NotRecommendedAttributes = new StatType[0]; a.RemoveFeatures = removed.ToArray(); a.AddFeatures = added.ToArray();
            });
            Faith.Prepare(Archetype, desna);
            foreach (var blueprint in OwnBlueprints.OfType<BlueprintScriptableObject>()) BlueprintCopy.BindElements(blueprint);
            RequireOwnLocalizedStrings(); RequireOwnBuffFx(true);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass), "m_Archetypes").GetValue(Rogue);
            if (old == null || old.Any(r => r == null || r.Guid == Archetype.AssetGuid)) throw new InvalidOperationException("Duplicate or invalid rogue archetype.");
            Faith.RestrictOtherDeities();
            SetField(Rogue, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Archetype.AssetGuid.ToString()) }).ToArray());
            Registered = Rogue.Archetypes.Count(a => a == Archetype) == 1;
            if (!Registered) throw new InvalidOperationException("Dream Thief registration failed.");
        }
    }
}
