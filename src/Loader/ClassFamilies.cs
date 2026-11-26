using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Localization;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace HomebrewClasses.Loader
{
    internal enum ClassFamily { Core, Base, Hybrid, Alternate, Occult, Prestige }

    internal static class ClassFamilies
    {
        internal static readonly string[] English = { "Core", "Base", "Hybrid", "Alternate", "Occult", "Prestige" };
        internal static readonly string[] Korean = { "코어", "베이스", "하이브리드", "얼터네이트", "오컬트", "프레스티지" };

        // Pathfinder First Edition class lists. The Brawler mod uses RMBrawlerHybridClass.
        static readonly Dictionary<string, ClassFamily> families = CreateMap();
        static Dictionary<string, ClassFamily> CreateMap()
        {
            var result = new Dictionary<string, ClassFamily>(StringComparer.OrdinalIgnoreCase);
            Add(result, ClassFamily.Core, "Barbarian Bard Cleric Druid Fighter Monk Paladin Ranger Rogue Sorcerer Wizard");
            Add(result, ClassFamily.Hybrid, "Arcanist Bloodrager Brawler RMBrawlerHybrid Hunter Investigator Shaman Skald Slayer Swashbuckler Warpriest");
            Add(result, ClassFamily.Alternate, "Antipaladin Ninja Samurai");
            Add(result, ClassFamily.Occult, "Kineticist Medium Mesmerist Occultist Psychic Spiritualist");
            return result;
        }
        static void Add(Dictionary<string, ClassFamily> map, ClassFamily family, string names)
        {
            foreach (var name in names.Split(' ')) map.Add(name, family);
        }
        internal static ClassFamily Find(string blueprintName, bool prestige)
        {
            if (prestige) return ClassFamily.Prestige;
            string name = blueprintName ?? "";
            if (name.EndsWith("Class", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 5);
            if (name.StartsWith("Unchained", StringComparison.OrdinalIgnoreCase)) name = name.Substring(9);
            ClassFamily family;
            return families.TryGetValue(name, out family) ? family : ClassFamily.Base;
        }

        internal static ClassFamily? Toggle(ClassFamily? open, ClassFamily clicked)
        {
            return open == clicked ? (ClassFamily?)null : clicked;
        }
    }

    // The game builds one root list per phase, including unavailable prestige classes.
    internal sealed class ClassFamilyList
    {
        static readonly ConditionalWeakTable<CharGenClassPhaseVM, ClassFamilyList> lists =
            new ConditionalWeakTable<CharGenClassPhaseVM, ClassFamilyList>();
        static readonly System.Reflection.FieldInfo controllerField = AccessTools.Field(typeof(CharGenClassPhaseVM), "m_LevelUpController");
        readonly Dictionary<BlueprintCharacterClass, ClassFamily> families = new Dictionary<BlueprintCharacterClass, ClassFamily>();
        internal readonly bool[] Available = new bool[ClassFamilies.English.Length];
        internal readonly bool Korean;
        internal readonly ClassFamily First;
        internal bool Usable { get { return families.Count > 0; } }

        ClassFamilyList(CharGenClassPhaseVM phase)
        {
            var controller = (LevelUpController)controllerField.GetValue(phase);
            if (phase.Mode == LevelUpState.CharBuildMode.Mythic || controller == null || controller.Unit == null || controller.Unit.IsPet) return;
            Korean = LocalizationManager.CurrentLocale.ToString() == "koKR";
            CharGenClassSelectorItemVM highest = null;
            foreach (var row in phase.ExtractNestedEntities().OfType<CharGenClassSelectorItemVM>())
            {
                if (row.Class == null || row.Class.IsMythic) continue;
                var family = ClassFamilies.Find(row.Class.name, row.Class.PrestigeClass);
                families[row.Class] = family;
                Available[(int)family] = true;
                if (row.DisplayName != null && row.DisplayName.Any(ch => ch >= '가' && ch <= '힣')) Korean = true;
                if (row.Level > 0 && (highest == null || row.Level > highest.Level)) highest = row;
            }
            if (!Usable) return;
            var initial = phase.SelectedClassVM.Value ?? highest;
            if (initial == null || !families.TryGetValue(initial.Class, out First))
                First = (ClassFamily)Array.IndexOf(Available, true);
        }
        internal bool FamilyOf(CharGenClassSelectorItemVM row, out ClassFamily family)
        {
            family = ClassFamily.Base;
            return row != null && row.Class != null && families.TryGetValue(row.Class, out family);
        }
        internal string Label(ClassFamily family) { return (Korean ? ClassFamilies.Korean : ClassFamilies.English)[(int)family]; }
        internal static ClassFamilyList For(CharGenClassPhaseVM phase)
        {
            return phase == null ? null : lists.GetValue(phase, p => new ClassFamilyList(p));
        }
    }
}
