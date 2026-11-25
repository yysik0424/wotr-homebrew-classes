using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ProphecyBreaker
{
    internal static class Blueprints
    {
        internal static BlueprintCharacterClass Oracle;
        internal static BlueprintArchetype Archetype;
        internal static BlueprintSpellbook Divine, Research;

        static T Make<T>(string key, Action<T> init) where T : SimpleBlueprint, new()
            => Create<T>("ProphecyBreaker_" + key, Rules.Id(key), init);

        static void CopyFields(object source, object target, Type stop)
        {
            for (var t = source.GetType(); t != stop; t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (!field.IsInitOnly && !field.IsNotSerialized) field.SetValue(target, field.GetValue(source));
        }

        static BlueprintSpellbook Book(string key, string source, Action<BlueprintSpellbook> changes)
        {
            var native = Get<BlueprintSpellbook>(source);
            if (native.ComponentsArray.Any()) throw new InvalidOperationException("Unexpected book components on " + native.name);
            return Make<BlueprintSpellbook>(key, b => {
                CopyFields(native, b, typeof(BlueprintScriptableObject));
                b.Name = Strings.L(key);
                b.CastingAttribute = StatType.Intelligence;
                SetField(b, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Ids.OracleClass));
                changes(b);
            });
        }

        static LevelEntry Row(int level, params BlueprintFeatureBase[] features)
        {
            var row = new LevelEntry { Level = level };
            row.SetFeatures(features);
            return row;
        }

        internal static void BuildAll()
        {
            Oracle = Get<BlueprintCharacterClass>(Ids.OracleClass);
            var mystery = Get<BlueprintFeature>(Ids.OracleMysterySelection);
            var revelation = Get<BlueprintFeature>(Ids.OracleRevelationSelection);
            var final = Get<BlueprintFeature>(Ids.OracleFinalRevelation);
            var orisons = Get<BlueprintFeature>(Ids.OracleOrisonsFeature);
            var nativeTable = Get<BlueprintSpellsTable>(Ids.ArcanistSpellSlots);
            var table = Make<BlueprintSpellsTable>("ResearchSlots", b => {
                b.Levels = nativeTable.Levels.Select((entry, level) => {
                    int tiers = Rules.MaxTier(level);
                    if (level <= 20 && entry.Count.Count(n => n > 0) != tiers)
                        throw new InvalidOperationException("Arcanist unlock schedule changed at " + level);
                    return new SpellsLevelEntry { Count = level == 0 ? new int[0] :
                        new[] {0}.Concat(Enumerable.Repeat(Rules.Slots, tiers)).ToArray() };
                }).ToArray();
            });
            var wizard = Get<BlueprintSpellList>(Ids.WizardSpellList);
            var list = Make<BlueprintSpellList>("ResearchList", b => {
                b.SpellsByLevel = Enumerable.Range(0, Rules.HighestTier + 1).Select(tier =>
                    tier == 0 ? new SpellLevelList(0) : wizard.SpellsByLevel.Single(e => e.SpellLevel == tier)).ToArray();
            });
            Divine = Book("Divine", Ids.OracleSpellbook, b => {});
            Research = Book("Research", Ids.ArcanistSpellbook, b => {
                SetField(b, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(table.AssetGuid.ToString()));
                SetField(b, "m_SpellSlots", Ref<BlueprintSpellsTableReference>(table.AssetGuid.ToString()));
                SetField(b, "m_SpellList", Ref<BlueprintSpellListReference>(list.AssetGuid.ToString()));
            });

            // Oracle orisons include a separate Charisma binding outside the stock spellbook.
            var prayers = Make<BlueprintFeature>("Orisons", b => {
                CopyFields(orisons,b,typeof(BlueprintScriptableObject));
                b.ComponentsArray = new BlueprintComponent[0];
            });
            foreach (var source in orisons.ComponentsArray)
            {
                var component = (BlueprintComponent)Activator.CreateInstance(source.GetType());
                CopyFields(source, component, typeof(BlueprintComponent));
                if (component is BindAbilitiesToClass binding) binding.Stat = StatType.Intelligence;
                AddComponents(prayers,component);
            }
            var grant = Make<BlueprintFeature>("ResearchTraining", b => {
                b.Ranks = 1; b.IsClassFeature = true;
                SetField(b,"m_DisplayName",Strings.L("Research"));
                SetField(b,"m_Description",Strings.L("Research.desc"));
                SetField(b,"m_DescriptionShort",NoText());
                SetField(b,"m_Icon",orisons.Icon);
            });
            var bookGrant = new AddSpellbook();
            SetField(bookGrant,"m_Spellbook",Ref<BlueprintSpellbookReference>(Research.AssetGuid.ToString()));
            SetField(bookGrant,"m_CasterLevel",new ContextValue { ValueType = ContextValueType.Rank });
            var rank = new ContextRankConfig();
            SetField(rank,"m_BaseValueType",ContextRankBaseValueType.ClassLevel);
            SetField(rank,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(Ids.OracleClass)});
            AddComponents(grant,rank,bookGrant,new ResearchParameters(),new LegacyBaseAttack());

            var removed = new[] {Row(1,mystery,revelation,orisons), Row(3,revelation),Row(7,revelation),
                Row(11,revelation),Row(15,revelation),Row(19,revelation),Row(20,final)};
            if (removed.Any(r => r.Features.Any(f => !Oracle.Progression.LevelEntries.Single(l => l.Level == r.Level).Features.Contains(f))))
                throw new InvalidOperationException("Oracle exchanges disagree with the installed progression");
            Archetype = Make<BlueprintArchetype>("Archetype", a => {
                SetField(a,"m_ParentClass",Oracle);
                SetField(a,"m_BaseAttackBonus",Ref<BlueprintStatProgressionReference>(Ids.LowBaseAttack));
                SetField(a,"m_ReplaceSpellbook",Ref<BlueprintSpellbookReference>(Divine.AssetGuid.ToString()));
                a.LocalizedName = Strings.L("Archetype.name");
                a.LocalizedDescription = a.LocalizedDescriptionShort = Strings.L("Archetype.desc");
                a.OverrideAttributeRecommendations = true;
                a.RecommendedAttributes = new[]{StatType.Intelligence}; a.NotRecommendedAttributes = new StatType[0];
                a.RemoveFeatures = removed;
                a.AddFeatures = new[]{Row(1,prayers,grant)};
            });
            RequireOwnLocalizedStrings();
            RequireOwnBuffFx(false);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass),"m_Archetypes").GetValue(Oracle);
            if (old.Any(a => a.Guid == Archetype.AssetGuid)) throw new InvalidOperationException("Duplicate Prophecy Breaker");
            SetField(Oracle,"m_Archetypes",old.Concat(new[]{Ref<BlueprintArchetypeReference>(Archetype.AssetGuid.ToString())}).ToArray());
        }
    }
}
