using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.TempMapCode.Ambush;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Falconer
{
    internal static class Blueprints
    {
        internal static BlueprintCharacterClass Ranger;
        internal static BlueprintArchetype Archetype;
        internal static BlueprintUnit Bird;
        internal static BlueprintBuff Eye, Shot, Driven, Covered, PetHit, HawkHit, Precision;
        internal static BlueprintFeature Hawk, Auto, Drive, Cover, Hunt;
        static BlueprintAbilityResource uses;
        static T Make<T>(string key, Action<T> init)
            where T : SimpleBlueprint, new() => Create<T>("Falconer_" + key, Rules.Id(key), init);
        static void Name(BlueprintUnitFact fact, string key, Sprite icon)
        {
            SetField(fact, "m_DisplayName", Strings.L(key + ".name"));
            SetField(fact, "m_Description", Strings.L(key + ".desc"));
            SetField(fact, "m_DescriptionShort", NoText());
            SetField(fact, "m_Icon", icon);
        }

        static BlueprintFeature Feature(string key, Sprite icon) => Make<BlueprintFeature>(key, f =>
        {
            Name(f, key, icon);
            f.Ranks = 1;
            f.IsClassFeature = true;
        });
        static BlueprintBuff Buff(string id, string key, Sprite icon, bool targets = false) => Make<BlueprintBuff>(id, b =>
        {
            Name(b, key, icon);
            b.FxOnStart = new PrefabLink();
            EmptyFxOnRemove(b);
            if (targets)
                b.Stacking = StackingType.Stack;
        });
        static void Grant(BlueprintFeature f, BlueprintUnitFact ability)
        {
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(ability.AssetGuid.ToString()) });
            AddComponents(f, grant);
        }

        static BlueprintAbility Ability(string key, Sprite icon, bool personal)
        {
            var a = Make<BlueprintAbility>(key + "Action", b =>
            {
                Name(b, key, icon);
                b.Type = AbilityType.Extraordinary;
                b.ActionType = UnitCommand.CommandType.Swift;
                b.Range = personal ? AbilityRange.Personal : AbilityRange.Long;
                b.CanTargetSelf = personal;
                b.CanTargetEnemies = !personal;
                b.CanTargetFriends = false;
                b.CanTargetPoint = false;
                b.LocalizedDuration = b.LocalizedSavingThrow = NoText();
            });
            AddComponents(a, new HawkCommand { IsEye = personal });
            if (!personal)
                AddComponents(a, new DiveAvailable());
            return a;
        }

        static LevelEntry Row(int level, params BlueprintFeatureBase[] facts)
        {
            var row = new LevelEntry
            {
                Level = level
            };
            row.SetFeatures(facts);
            return row;
        }

        internal static void BuildAll()
        {
            Ranger = Get<BlueprintCharacterClass>(Rules.Ranger);
            var dive = Get<BlueprintAbility>(Rules.Charge).Icon;
            var eye = Get<BlueprintAbility>(Rules.TrueStrike).Icon;
            var quarry = Get<BlueprintFeature>("385260ca07d5f1b4e907ba22a02944fc").Icon;
            Hawk = Feature("Hawk", dive);
            Drive = Feature("Drive", quarry);
            Auto = Feature("Auto", dive);
            Cover = Feature("Cover", dive);
            Hunt = Feature("Hunt", quarry);
            AddComponents(Hunt, new LegacyEvasion());
            Eye = Buff("EyeBuff", "Eye", eye);
            Shot = Buff("ShotBuff", "Hawk", quarry, true);
            Driven = Buff("DrivenBuff", "Drive", quarry, true);
            Covered = Buff("CoverBuff", "Cover", dive, true);
            PetHit = Buff("PetHitBuff", "Hunt", quarry, true);
            HawkHit = Buff("HawkHitBuff", "Hunt", quarry, true);
            Precision = Buff("PrecisionBuff", "Hunt", quarry, true);
            var shell = Make<BlueprintFeature>("BirdShell", f =>
            {
                Name(f, "Hawk", dive);
                f.Ranks = 1;
                f.HideInUI = true;
            });
            var ignored = new AddMechanicsFeature();
            SetField(ignored, "m_Feature", AddMechanicsFeature.MechanicsFeatureType.IsIgnoredByCombat);
            AddComponents(shell, new Untargetable(), new MarkPassive(), ignored, new HawkStrikeEffects());
            Bird = CloneUnit(Get<BlueprintUnit>(Rules.Eagle), "Falconer_HuntingHawk", Rules.Id("Bird"));
            Bird.ComponentsArray = new BlueprintComponent[0];
            SetField(Bird, "m_AddFacts", new[] { Ref<BlueprintUnitFactReference>(shell.AssetGuid.ToString()) });
            SetField(Bird, "m_Faction", Ref<BlueprintFactionReference>("72f240260881111468db610b6c37c099"));
            SetField(Bird, "m_Brain", null);
            Bird.Size = Kingmaker.Enums.Size.Medium;
            Bird.Strength = 10;
            Bird.Dexterity = 10;
            AddComponents(Hawk, new HawkTraining());
            Grant(Hawk, Ability("Dive", dive, false));
            var focus = Feature("Eye", eye);
            uses = Make<BlueprintAbilityResource>("EyeUses", r =>
            {
                r.LocalizedName = Strings.L("Eye.name");
                r.LocalizedDescription = Strings.L("Eye.desc");
                SetField(r, "m_Icon", eye);
                var amt = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                SetField(amt, "BaseValue", 1);
                SetField(amt, "IncreasedByLevelStartPlusDivStep", true);
                SetField(amt, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(Rules.Ranger) });
                SetField(amt, "m_ArchetypesDiv", new BlueprintArchetypeReference[0]);
                SetField(amt, "StartingLevel", 19);
                SetField(amt, "StartingIncrease", 1);
                SetField(amt, "LevelStep", 20);
                SetField(amt, "PerStepIncrease", 0);
                SetField(r, "m_MaxAmount", amt);
            });
            var resource = new AddAbilityResources
            {
                RestoreAmount = true
            };
            SetField(resource, "m_Resource", Ref<BlueprintAbilityResourceReference>(uses.AssetGuid.ToString()));
            AddComponents(focus, resource);
            var activate = Ability("Eye", eye, true);
            var cost = new AbilityResourceLogic
            {
                Amount = 1
            };
            SetField(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(uses.AssetGuid.ToString()));
            SetField(cost, "m_IsSpendResource", true);
            AddComponents(activate, cost);
            Grant(focus, activate);
            var removed = new List<LevelEntry>
            {
                Row(3, Get<BlueprintFeature>(Rules.Endurance), Get<BlueprintFeature>(Rules.Terrain)),
                Row(12, Get<BlueprintFeature>(Rules.Camouflage))
            };
            foreach (int level in new[] { 8, 13, 18 })
                removed.Add(Row(level, Get<BlueprintFeature>(Rules.Terrain), Get<BlueprintFeature>(Rules.TerrainRank)));
            removed.Add(Row(Rules.ImprovedEvasionLevel, Get<BlueprintFeature>(Rules.ImprovedEvasion)));
            foreach (var row in removed)
                if (!row.Features.All(f => Ranger.Progression.LevelEntries.Any(e => e.Level == row.Level && e.Features.Contains(f))))
                    throw new InvalidOperationException("Ranger exchange missing at level " + row.Level);
            Archetype = Make<BlueprintArchetype>("Archetype", a =>
            {
                SetField(a, "m_ParentClass", Ranger);
                a.LocalizedName = Strings.L("Archetype.name");
                a.LocalizedDescription = a.LocalizedDescriptionShort = Strings.L("Archetype.desc");
                a.OverrideAttributeRecommendations = true;
                a.RecommendedAttributes = new[]
                {
                    StatType.Dexterity,
                    StatType.Wisdom
                };
                a.NotRecommendedAttributes = new StatType[0];
                a.RemoveFeatures = removed.OrderBy(row => row.Level).ToArray();
                a.AddFeatures = new[]
                {
                    Row(1, Hawk),
                    Row(4, Drive),
                    Row(8, Auto),
                    Row(10, Cover),
                    Row(11, focus),
                    Row(15, Feature("Third", dive)),
                    Row(16, Hunt),
                    Row(19, Feature("EyeMore", eye))
                };
            });
            RequireOwnLocalizedStrings();
            RequireOwnBuffFx(true);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass), "m_Archetypes").GetValue(Ranger);
            if (old.Any(r => r.Guid == Archetype.AssetGuid))
                throw new InvalidOperationException("Duplicate Falconer registration");
            SetField(Ranger, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Archetype.AssetGuid.ToString()) }).ToArray());
        }
    }
}
