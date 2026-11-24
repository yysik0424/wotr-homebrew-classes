using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Deathdealer
{
    internal static class Blueprints
    {
        internal static BlueprintCharacterClass Slayer;
        internal static BlueprintBuff Studied;
        internal static readonly BlueprintBuff[] Modes = new BlueprintBuff[3];
        internal static BlueprintFeature Research, Mastery;
        internal static BlueprintArchetype Archetype;
        internal static T Make<T>(string key, Action<T> setup) where T : SimpleBlueprint, new()
            => Create<T>("Deathdealer_" + key, Rules.Id(key), setup);

        internal static void Name(BlueprintUnitFact fact, string key, Sprite icon)
        {
            SetField(fact, "m_DisplayName", Strings.L(key + ".name"));
            SetField(fact, "m_Description", Strings.L(key + ".desc"));
            SetField(fact, "m_DescriptionShort", NoText());
            SetField(fact, "m_Icon", icon);
        }
        internal static BlueprintFeature Feature(string key, Sprite icon)
            => Make<BlueprintFeature>(key, f => { Name(f,key,icon); f.Ranks = 1; f.IsClassFeature = true; });
        internal static void Grant(BlueprintFeature feature, BlueprintUnitFact fact)
        {
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(fact.AssetGuid.ToString()) });
            AddComponents(feature,grant);
        }

        static BlueprintFeature Reaction(int index, string key, Sprite icon)
        {
            var buff = Make<BlueprintBuff>(key + "Buff", b => {
                Name(b,key,icon); b.FxOnStart = new PrefabLink(); EmptyFxOnRemove(b);
            });
            Modes[index] = buff;
            var toggle = Make<BlueprintActivatableAbility>(key + "Toggle", a => {
                Name(a,key,icon); a.ActivationType = AbilityActivationType.Immediately;
                a.Group = ActivatableAbilityGroup.None; a.IsOnByDefault = true;
                a.DeactivateIfCombatEnded = false; a.DeactivateIfOwnerDisabled = false;
                SetField(a,"m_Buff",Ref<BlueprintBuffReference>(buff.AssetGuid.ToString()));
            });
            var f = Feature(key,icon);
            Grant(f,toggle);
            if (index == 0) AddComponents(f,new ReactionTraining());
            return f;
        }

        static LevelEntry Row(int level, IEnumerable<BlueprintFeatureBase> facts)
        {
            var row = new LevelEntry { Level = level }; row.SetFeatures(facts.ToArray()); return row;
        }

        internal static void BuildAll()
        {
            Slayer = Get<BlueprintCharacterClass>(Rules.Slayer);
            Studied = Get<BlueprintBuff>(Rules.Studied);
            var attacks = new[] { "Counter", "Trip", "Interrupt" };
            var spells = Arcana.Build();
            var added = new SortedDictionary<int,List<BlueprintFeatureBase>>();
            void Add(int level, BlueprintFeatureBase f)
            {
                if (!added.TryGetValue(level,out var row)) added.Add(level,row = new List<BlueprintFeatureBase>());
                row.Add(f);
            }
            for (int tier = 1; tier <= spells.Length; tier++) Add(Rules.UnlockLevel(tier),spells[tier-1]);
            var removed = new List<LevelEntry>();
            for (int i = 0; i < Rules.ExchangeLevels.Length; i++)
            {
                int level = Rules.ExchangeLevels[i];
                var lost = Get<BlueprintFeature>(Rules.Exchanges[i]);
                if (!Slayer.Progression.LevelEntries.Any(r => r.Level == level && r.Features.Contains(lost)))
                    throw new InvalidOperationException("Slayer exchange missing at level " + level);
                removed.Add(Row(level,new[] { lost }));
                if (i < attacks.Length) Add(level,Reaction(i,attacks[i],Get<BlueprintAbility>(Rules.Spells[i]).Icon));
                else if (i == 3)
                {
                    Research = Feature("Research",lost.Icon); AddComponents(Research,new ResearchEffects()); Add(level,Research);
                }
                else { Mastery = Feature("Mastery",lost.Icon); Add(level,Mastery); }
            }
            Archetype = Make<BlueprintArchetype>("Archetype",a => {
                SetField(a,"m_ParentClass",Slayer); a.LocalizedName = Strings.L("Archetype.name");
                a.LocalizedDescription = a.LocalizedDescriptionShort = Strings.L("Archetype.desc");
                a.OverrideAttributeRecommendations = true; a.RecommendedAttributes = new[]{StatType.Intelligence,StatType.Dexterity};
                a.NotRecommendedAttributes = new StatType[0];
                a.AddFeatures = added.Select(p=>Row(p.Key,p.Value)).ToArray(); a.RemoveFeatures = removed.ToArray();
            });
            RequireOwnLocalizedStrings(); RequireOwnBuffFx(true);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass),"m_Archetypes").GetValue(Slayer);
            if (old == null || old.Any(r=>r == null || r.Guid == Archetype.AssetGuid)) throw new InvalidOperationException("Invalid slayer archetype registry.");
            SetField(Slayer,"m_Archetypes",old.Concat(new[]{Ref<BlueprintArchetypeReference>(Archetype.AssetGuid.ToString())}).ToArray());
            if (Slayer.Archetypes.Count(a=>a==Archetype)!=1) throw new InvalidOperationException("Deathdealer registration failed.");
        }
    }
}
