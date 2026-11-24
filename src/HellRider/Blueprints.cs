using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace HellRider
{
    internal static class Blueprints
    {
        internal static BlueprintCharacterClass Cavalier;
        internal static BlueprintArchetype Archetype;
        internal static BlueprintUnit Nightmare, Devil;
        internal static BlueprintFeature Greater;
        internal static T Make<T>(string key, Action<T> init) where T : SimpleBlueprint, new()
            => Create<T>("HellRider_" + key, Rules.Id(key), init);

        internal static void Name(BlueprintUnitFact fact, string key, Sprite icon)
        {
            SetField(fact, "m_DisplayName", Strings.L(key + ".name"));
            SetField(fact, "m_Description", Strings.L(key + ".desc"));
            SetField(fact, "m_DescriptionShort", NoText());
            SetField(fact, "m_Icon", icon);
        }

        internal static BlueprintFeature Feature(string key, Sprite icon)
            => Make<BlueprintFeature>(key, f => { Name(f, key, icon); f.Ranks = 1; f.IsClassFeature = true; });

        internal static void Grant(BlueprintFeature feature, BlueprintUnitFact fact)
        {
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(fact.AssetGuid.ToString()) });
            AddComponents(feature, grant);
        }

        // Preserve each native asset reference. Every mutable mechanics component and action
        // belongs to the new blueprint, including nested saves and spell variant parents.
        internal static T Copy<T>(T source, string key, Func<BlueprintComponent,bool> include = null)
            where T : BlueprintScriptableObject, new()
        {
            return Make<T>(key, b =>
            {
                for (var type = typeof(T); type != typeof(BlueprintScriptableObject); type = type.BaseType)
                    foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        if (!f.IsInitOnly && !f.IsNotSerialized) f.SetValue(b, f.GetValue(source));
                var components = source.ComponentsArray.Where(c => include == null || include(c))
                    .Select(c => (BlueprintComponent)DreamThief.CloneKit.CopyOf(c, x => x is SimpleBlueprint || x is UnityEngine.Object, 48)).ToArray();
                var visited = new HashSet<object>();
                foreach (var c in components) Bind(c, b, visited);
                b.ComponentsArray = components;
            });
        }

        static void Bind(object item, BlueprintScriptableObject owner, HashSet<object> seen)
        {
            if (item == null || !seen.Add(item)) return;
            if (item is Element e) { e.Owner = owner; owner.ElementsArray.Add(e); }
            if (item is IEnumerable items && !(item is string))
            {
                foreach (var child in items) Bind(child, owner, seen);
                return;
            }
            if (!(item is BlueprintComponent || item is Element || item is ActionList || item is ConditionsChecker)) return;
            for (var t = item.GetType(); t != typeof(object); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    Bind(field.GetValue(item), owner, seen);
        }

        static LevelEntry Row(int level, params BlueprintFeatureBase[] facts)
        {
            var row = new LevelEntry { Level = level };
            row.SetFeatures(facts);
            return row;
        }

        internal static void BuildAll()
        {
            Cavalier = Get<BlueprintCharacterClass>(Ids.CavalierClass);
            var nativeMount = Get<BlueprintFeatureSelection>(Ids.CavalierMountSelection);
            var horseFeature = Get<BlueprintFeature>(Ids.AnimalCompanionFeatureHorse);
            var horse = Get<BlueprintUnit>(Ids.AnimalCompanionUnitHorse);
            var nightmare = Get<BlueprintUnit>(Ids.CR5_NightmareFriendly);
            Nightmare = Copy(horse, "NightmareUnit");
            Nightmare.Prefab = nightmare.Prefab;
            Nightmare.LocalizedName = nightmare.LocalizedName;
            var facts = Nightmare.AddFacts.Select(f => Ref<BlueprintUnitFactReference>(f.AssetGuid.ToString())).ToList();
            facts.Add(Ref<BlueprintUnitFactReference>(Ids.NightmareHoofFeature));
            SetField(Nightmare, "m_AddFacts", facts.ToArray());
            var choice = Copy(horseFeature, "NightmareChoice");
            var companion = choice.GetComponent<AddPet>();
            SetField(companion, "m_Pet", Ref<BlueprintUnitReference>(Nightmare.AssetGuid.ToString()));
            Name(choice, "Mount", horseFeature.Icon);
            var selection = Copy(nativeMount, "MountSelection");
            Name(selection, "Mount", nativeMount.Icon);
            // Preserve the native continue-companion option and its prerequisites for multiclass riders.
            var choices = new[] { Ref<BlueprintFeatureReference>(choice.AssetGuid.ToString()), Ref<BlueprintFeatureReference>(Ids.AnimalCompanionEmptyCompanion) };
            SetField(selection, "m_AllFeatures", choices);
            SetField(selection, "m_Features", choices);
            var charge = Feature("Charge", Get<BlueprintFeature>(Ids.CavalierCharge).Icon);
            AddComponents(charge, new HellCharge());
            var banner = Standards.Build();
            var breath = Magic.BuildBreath();
            var command = Magic.BuildSpell(Ids.CommandGreater, "Command", 5);
            var ray = Magic.BuildSpell(Ids.HellfireRay, "Ray", 6);
            var brand = Magic.BuildSpell(Ids.Firebrand, "Brand", 7);
            var presence = Magic.BuildSpell(Ids.OverwhelmingPresence, "Presence", 9);
            Greater = Feature("Greater", banner.Icon);
            Devils.BuildVisual();
            var removed = new[] {
                Row(1,nativeMount), Row(5,Get<BlueprintFeature>(Ids.CavalierBanner)),
                Row(11,Get<BlueprintFeature>(Ids.CavalierMightyCharge)), Row(14,Get<BlueprintFeature>(Ids.CavalierBannerGreater)),
                Row(20,Get<BlueprintFeature>(Ids.CavalierSupremeCharge)) };
            if (removed.Any(r => !Cavalier.Progression.LevelEntries.Any(e => e.Level == r.Level && e.Features.Contains(r.Features.Single()))))
                throw new InvalidOperationException("Cavalier progression no longer matches the Hell Rider exchanges");
            Archetype = Make<BlueprintArchetype>("Archetype", a =>
            {
                SetField(a, "m_ParentClass", Cavalier);
                a.LocalizedName = Strings.L("Archetype.name");
                a.LocalizedDescription = a.LocalizedDescriptionShort = Strings.L("Archetype.desc");
                a.OverrideAttributeRecommendations = true;
                a.RecommendedAttributes = new[] { StatType.Strength, StatType.Charisma };
                a.NotRecommendedAttributes = new StatType[0];
                a.RemoveFeatures = removed;
                a.AddFeatures = new[] { Row(1,selection), Row(3,charge), Row(5,banner), Row(8,breath), Row(9,command),
                    Row(11,Feature("Two",charge.Icon), ray), Row(13,brand), Row(14,Greater),
                    Row(15,Feature("RayTwo",ray.Icon)), Row(17,presence), Row(19,Feature("RayThree",ray.Icon)), Row(20,Feature("Three",charge.Icon)) };
            });
            RequireOwnLocalizedStrings();
            RequireOwnBuffFx(true);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass), "m_Archetypes").GetValue(Cavalier);
            if (old.Any(a => a.Guid == Archetype.AssetGuid)) throw new InvalidOperationException("Duplicate Hell Rider");
            SetField(Cavalier, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Archetype.AssetGuid.ToString()) }).ToArray());
        }
    }
}
