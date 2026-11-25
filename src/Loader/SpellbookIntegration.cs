using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace HomebrewClasses.Loader
{
    internal static class SpellbookIntegration
    {
        // Archetype IDs come from the three modules. Donor IDs are the stock books they copy or reuse.
        internal const string Prophecy = "09cc446731088be7719c00cb30609661", Research = "3475e29082e736adda61bc596f0f97d2";
        internal const string Fist = "06c043db412b4e0d930d37ab19146992", Pact = "d74096f01a884dd5b33aff12cb86b36f";
        internal const string Oracle = "6c03364712b415941a98f74522a81273", Magus = "5d8d04e76dff6c5439de99af0d57be63";
        internal const string Stigma = "b897fe0947e4b804082b1a687c21e6e2", StigmaArchetype = "c5f6e53e71059fb4d802ce81a277a12d";
        internal const string Witch = "dd04f9239f655ea438976742728e4909";
        const string LegacyArcanist = "33903fe5c4abeaa45bc249adb9d98848";
        const string Angel = "e1fbb0e0e610a3a4d91e5e5284587939", Lich = "3f16e9caf7c683c40884c7c455ed26af";

        internal sealed class Family
        {
            internal BlueprintArchetype Archetype;
            internal BlueprintSpellbook Main, Donor;
            internal bool Warlock;
        }

        static readonly HashSet<BlueprintFeatureSelectMythicSpellbook> warlockOnly = new HashSet<BlueprintFeatureSelectMythicSpellbook>();
        static Family prophecy, warlock;
        static HashSet<string> enabled;
        static Action<string> error;
        static bool ready;
        static readonly MethodInfo shallowCopy = AccessTools.Method(typeof(object), "MemberwiseClone");

        internal static void Install(IEnumerable<string> modules, Action<string> logError)
        {
            enabled = new HashSet<string>(modules);
            if (!enabled.Overlaps(new[] {"ProphecyBreaker","Spellfist","Warlock"})) return;
            error = logError;
            var h = new Harmony(BundlePatches.Owner + ".Spellbooks");
            h.Patch(AccessTools.Method(typeof(StartGameLoader), "LoadAllJson", new[] { typeof(string) }),
                postfix: new HarmonyMethod(typeof(SpellbookIntegration), nameof(Build)) { priority = Priority.Last });
            if (enabled.Contains("ProphecyBreaker"))
                h.Patch(AccessTools.Method(typeof(AddSpellbookLevel), "OnActivate"),
                    transpiler: new HarmonyMethod(typeof(SpellbookIntegration), nameof(RouteLevelGain)));
            if (enabled.Contains("Warlock"))
            {
                h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelectMythicSpellbook), "CanSelect"),
                    postfix: new HarmonyMethod(typeof(SpellbookIntegration), nameof(CheckWarlock)));
                h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelectMythicSpellbook), "ExtractSelectionItems"),
                    postfix: new HarmonyMethod(typeof(SpellbookIntegration), nameof(FilterWarlockItems)));
            }
        }

        static T Get<T>(string id) where T : BlueprintScriptableObject =>
            ResourcesLibrary.TryGetBlueprint<T>(id) ?? throw new InvalidOperationException("Missing blueprint " + id);
        static FieldInfo Field(object obj, string name) =>
            AccessTools.Field(obj.GetType(), name) ?? throw new MissingFieldException(obj.GetType().FullName, name);
        internal static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new()
        {
            var r = new T();
            r.ReadGuidFromGuid(b.AssetGuid);
            return r;
        }

        static void Build()
        {
            if (ready) return;
            try { BuildSelections(); ready = true; }
            catch (Exception ex) { error("Spellbook integration initialization: " + ex); }
        }

        static void BuildSelections()
        {
            var families = new List<Family>();
            Collect(families, "ProphecyBreaker", Prophecy, Oracle);
            Collect(families, "Spellfist", Fist, Magus);
            Collect(families, "Warlock", Pact, Stigma);
            if (families.Count == 0) return;
            var mythics = new HashSet<BlueprintFeatureSelectMythicSpellbook> {
                Get<BlueprintFeatureSelectMythicSpellbook>(Angel), Get<BlueprintFeatureSelectMythicSpellbook>(Lich)
            };
            // Some prestige mods grant nested selectors through facts. Identify those by their
            // already-loaded replacement options without resolving unrelated selection entries.
            var cachedSelections = new List<BlueprintFeatureSelection>();
            var donorOptions = new HashSet<BlueprintGuid>();
            var donorIds = new HashSet<BlueprintGuid>(families.Select(f => f.Donor.AssetGuid));
            var fist = families.FirstOrDefault(f => f.Archetype.AssetGuid == BlueprintGuid.Parse(Fist));
            Family legacyFist = fist == null ? null : new Family { Archetype = fist.Archetype, Main = fist.Main, Donor = Get<BlueprintSpellbook>(LegacyArcanist) };
            if (legacyFist != null) donorIds.Add(legacyFist.Donor.AssetGuid);
            var bookField = AccessTools.Field(typeof(BlueprintFeatureReplaceSpellbook), "m_Spellbook");
            ResourcesLibrary.BlueprintsCache.ForEachLoaded((id, bp) => {
                if (bp is BlueprintFeatureSelectMythicSpellbook mythic) mythics.Add(mythic);
                if (bp is BlueprintFeatureSelection selection) cachedSelections.Add(selection);
                if (bp is BlueprintFeatureReplaceSpellbook option && bookField.GetValue(option) is BlueprintSpellbookReference r && donorIds.Contains(r.Guid))
                    donorOptions.Add(id);
            });
            foreach (var selection in mythics) ExtendMythic(selection, families);
            // Spell advancement is chosen from selections on prestige level entries (including TTT's replacement Loremaster selection).
            var direct = BlueprintRoot.Instance.Progression.CharacterClasses
                .Where(c => c.PrestigeClass && c.Progression != null)
                .SelectMany(c => c.Progression.LevelEntries)
                .SelectMany(entry => entry.Features).OfType<BlueprintFeatureSelection>();
            var optionsField = AccessTools.Field(typeof(BlueprintFeatureSelection), "m_AllFeatures");
            var nested = cachedSelections.Where(s => ((BlueprintFeatureReference[])optionsField.GetValue(s))
                ?.Any(r => r != null && donorOptions.Contains(r.Guid)) == true);
            foreach (var selection in direct.Concat(nested).Distinct().ToArray())
            {
                var options = selection.AllFeatures.OfType<BlueprintFeatureReplaceSpellbook>().ToArray();
                if (options.Length == 0) continue;
                // Old saves may reference an option derived from an Arcanist template. Keep its blueprint resolvable.
                if (legacyFist != null)
                    foreach (var old in options.Where(o => o.Spellbook == legacyFist.Donor && !o.ComponentsArray.OfType<PrerequisiteArchetypeLevel>().Any()))
                        CreateOption(old, legacyFist);
                foreach (var family in families)
                {
                    try { ExtendPrestige(selection, options, family); }
                    catch (Exception ex) { error("Spellbook integration " + selection.name + " / " + family.Archetype.name + ": " + ex); }
                }
            }
        }

        static void Collect(List<Family> families, string module, string archetypeId, string donor)
        {
            if (!enabled.Contains(module)) return;
            var archetype = ResourcesLibrary.TryGetBlueprint<BlueprintArchetype>(archetypeId);
            if (archetype?.ReplaceSpellbook == null)
            {
                error("Spellbook integration: missing registered main book for " + module);
                return;
            }
            var f = new Family { Archetype = archetype, Main = archetype.ReplaceSpellbook, Donor = Get<BlueprintSpellbook>(donor), Warlock = module == "Warlock" };
            if (module == "ProphecyBreaker") prophecy = f;
            if (f.Warlock) warlock = f;
            families.Add(f);
        }

        internal static void ExtendMythic(BlueprintFeatureSelectMythicSpellbook selection, IEnumerable<Family> families)
        {
            var field = Field(selection, "m_AllowedSpellbooks");
            var original = (BlueprintSpellbookReference[])field.GetValue(selection);
            if (original == null) return;
            var books = original.Where(r => r == null || r.Guid != BlueprintGuid.Parse(Research)).ToList();
            foreach (var f in families)
            {
                var anchor = f.Warlock ? BlueprintGuid.Parse(Witch) : f.Donor.AssetGuid;
                if (!books.Any(r => r?.Guid == anchor) || books.Any(r => r?.Guid == f.Main.AssetGuid)) continue;
                books.Add(Ref<BlueprintSpellbookReference>(f.Main));
                if (f.Warlock) warlockOnly.Add(selection);
            }
            if (books.SequenceEqual(original)) return;
            field.SetValue(selection, books.ToArray());
            Field(selection, "m_CachedItems").SetValue(selection, null);
        }

        static void CheckWarlock(BlueprintFeatureSelectMythicSpellbook __instance, UnitDescriptor unit, IFeatureSelectionItem item, ref bool __result)
        {
            if (__result && warlock != null && item.Param.Blueprint == warlock.Main && warlockOnly.Contains(__instance))
                __result = unit.Progression.GetClassData(warlock.Main.CharacterClass)?.Archetypes.Contains(warlock.Archetype) == true;
        }

        static void FilterWarlockItems(BlueprintFeatureSelectMythicSpellbook __instance, UnitDescriptor previewUnit, ref IEnumerable<IFeatureSelectionItem> __result)
        {
            if (warlock != null && warlockOnly.Contains(__instance) &&
                previewUnit.Progression.GetClassData(warlock.Main.CharacterClass)?.Archetypes.Contains(warlock.Archetype) != true)
                __result = __result.Where(item => item.Param.Blueprint != warlock.Main);
        }

        // Keep the original feature and its behavior for existing characters. Only its fixed Oracle target needs routing.
        static IEnumerable<CodeInstruction> RouteLevelGain(IEnumerable<CodeInstruction> instructions)
        {
            var demand = AccessTools.Method(typeof(UnitEntityData), "DemandSpellbook", new[] {typeof(BlueprintSpellbook)});
            var routed = AccessTools.Method(typeof(SpellbookIntegration), nameof(DemandMain));
            int matches = 0;
            foreach (var ins in instructions)
            {
                if (ins.Calls(demand)) { ins.opcode = OpCodes.Call; ins.operand = routed; matches++; }
                yield return ins;
            }
            if (matches != 1) throw new InvalidOperationException("AddSpellbookLevel.DemandSpellbook count " + matches);
        }

        internal static Spellbook DemandMain(UnitEntityData owner, BlueprintSpellbook book)
        {
            if (prophecy != null && book == prophecy.Donor && owner.Progression.GetClassData(book.CharacterClass)?.Spellbook == prophecy.Main)
                book = prophecy.Main;
            return owner.DemandSpellbook(book);
        }

        internal static void ExtendPrestige(BlueprintFeatureSelection selection, BlueprintFeatureReplaceSpellbook[] options, Family f)
        {
            var templates = options.Where(o => o.Spellbook == f.Donor).ToArray();
            if (templates.Length == 0) return;
            if (!f.Warlock && options.Any(o => o.Spellbook == f.Main)) return;
            foreach (var template in templates)
            {
                // Warlocks already qualify for a book-ownership-based entry such as TTT's Loremaster.
                if (f.Warlock && !template.ComponentsArray.OfType<PrerequisiteArchetypeLevel>()
                    .Any(p => p.Archetype?.AssetGuid == BlueprintGuid.Parse(StigmaArchetype))) continue;
                if (!f.Warlock && template.ComponentsArray.OfType<PrerequisiteArchetypeLevel>().Any()) continue;
                var copy = CreateOption(template, f);
                AppendOption(selection, "m_AllFeatures", template, copy);
                AppendOption(selection, "m_Features", template, copy);
                if (!f.Warlock && f.Main.CharacterClass == f.Donor.CharacterClass)
                    ExcludeArchetype(template, f);
            }
        }

        static void AppendOption(BlueprintFeatureSelection selection, string name, BlueprintFeature donor, BlueprintFeature copy)
        {
            var field = Field(selection, name);
            var refs = (BlueprintFeatureReference[])field.GetValue(selection);
            if (refs == null || !refs.Any(r => r?.Guid == donor.AssetGuid) || refs.Any(r => r?.Guid == copy.AssetGuid)) return;
            field.SetValue(selection, refs.Concat(new[] {Ref<BlueprintFeatureReference>(copy)}).ToArray());
        }

        static void ExcludeArchetype(BlueprintFeature template, Family f)
        {
            if (template.ComponentsArray.OfType<PrerequisiteNoArchetype>().Any(p => p.Archetype == f.Archetype)) return;
            var guard = new PrerequisiteNoArchetype { name = "$HomebrewExclude$" + f.Archetype.AssetGuid };
            Field(guard, "m_CharacterClass").SetValue(guard, Ref<BlueprintCharacterClassReference>(f.Main.CharacterClass));
            Field(guard, "m_Archetype").SetValue(guard, Ref<BlueprintArchetypeReference>(f.Archetype));
            guard.OwnerBlueprint = template;
            template.ComponentsArray = template.ComponentsArray.Concat(new BlueprintComponent[] {guard}).ToArray();
        }

        internal static BlueprintGuid OptionId(BlueprintFeature template, BlueprintArchetype archetype)
        {
            using (var hash = SHA256.Create())
            {
                var key = "HomebrewClasses.Prestige.v1/" + template.AssetGuid + "/" + archetype.AssetGuid;
                return BlueprintGuid.Parse(BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(key)), 0, 16).Replace("-", ""));
            }
        }

        static readonly Dictionary<BlueprintGuid, BlueprintFeatureReplaceSpellbook> created = new Dictionary<BlueprintGuid, BlueprintFeatureReplaceSpellbook>();
        static BlueprintFeatureReplaceSpellbook CreateOption(BlueprintFeatureReplaceSpellbook template, Family f)
        {
            var id = OptionId(template, f.Archetype);
            if (created.TryGetValue(id, out var existing)) return existing;
            var copy = (BlueprintFeatureReplaceSpellbook)shallowCopy.Invoke(template, null);
            copy.AssetGuid = id;
            copy.name = template.name + "_" + f.Archetype.name;
            Field(copy,"m_DisplayName").SetValue(copy, f.Archetype.LocalizedName);
            Field(copy,"m_Spellbook").SetValue(copy, Ref<BlueprintSpellbookReference>(f.Main));
            var components = new List<BlueprintComponent>();
            foreach (var original in template.ComponentsArray)
            {
                if (original is PrerequisiteNoArchetype excluded && excluded.CharacterClass == f.Donor.CharacterClass
                    && (f.Main.CharacterClass != f.Donor.CharacterClass || excluded.Archetype == f.Archetype)) continue;
                var component = (BlueprintComponent)shallowCopy.Invoke(original, null);
                if (f.Warlock && component is PrerequisiteArchetypeLevel required && required.Archetype?.AssetGuid == BlueprintGuid.Parse(StigmaArchetype))
                    Field(required,"m_Archetype").SetValue(required, Ref<BlueprintArchetypeReference>(f.Archetype));
                if (component is PrerequisiteClassSpellLevel spell && spell.CharacterClass == f.Donor.CharacterClass)
                    Field(component,"m_CharacterClass").SetValue(component, Ref<BlueprintCharacterClassReference>(f.Main.CharacterClass));
                // Optional TabletopTweaks-Core component, intentionally no assembly dependency.
                if (component.GetType().FullName == "TabletopTweaks.Core.NewComponents.Prerequisites.PrerequisiteSpellbook")
                {
                    var field = Field(component,"Spellbook");
                    if (field.GetValue(component) is BlueprintSpellbookReference book && book.Guid == f.Donor.AssetGuid)
                        field.SetValue(component, Ref<BlueprintSpellbookReference>(f.Main));
                }
                if (component.GetType().FullName == "PrestigePlus.Modify.PrerequisiteSpellBook")
                {
                    var field = Field(component,"Book");
                    if (Guid.TryParse(field.GetValue(component) as string, out var book) && book.ToString("N") == f.Donor.AssetGuid.ToString())
                        field.SetValue(component, f.Main.AssetGuid.ToString());
                }
                components.Add(component);
            }
            var gate = new PrerequisiteArchetypeLevel { Level = 1, name = "$HomebrewArchetype$" + f.Archetype.AssetGuid };
            Field(gate,"m_CharacterClass").SetValue(gate, Ref<BlueprintCharacterClassReference>(f.Main.CharacterClass));
            Field(gate,"m_Archetype").SetValue(gate, Ref<BlueprintArchetypeReference>(f.Archetype));
            components.Add(gate);
            copy.ComponentsArray = components.ToArray();
            copy.OnEnable();
            ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(id, copy);
            created.Add(id, copy);
            return copy;
        }
    }
}
