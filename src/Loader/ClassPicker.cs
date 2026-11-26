using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.UI.MVVM._ConsoleView.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.Other.NestedSelectionGroup;
using Owlcat.Runtime.UI.VirtualListSystem;
using TMPro;
using UnityEngine;

namespace HomebrewClasses.Loader
{
    internal static class ClassPicker
    {
        internal const string Owner = "HomebrewClasses.ClassPicker";
        static readonly Type pc = typeof(CharGenClassSelectorPCView);
        static readonly PropertyInfo model = AccessTools.Property(pc, "ViewModel");
        internal static readonly MethodInfo Refresh = AccessTools.Method(pc, "OnCollectionChanged");
        static readonly FieldInfo virtualList = AccessTools.Field(pc, "VirtualList");
        static readonly FieldInfo viewport = AccessTools.Field(typeof(VirtualListComponent), "m_Viewport");
        static readonly PropertyInfo consoleModel = AccessTools.Property(typeof(CharGenClassSelectorItemConsoleView), "ViewModel");
        static Action<string> log;
        static bool reported;

        internal static void Install(Action<string> error)
        {
            log = error;
            var harmony = new Harmony(Owner);
            try
            {
                if (model == null || Refresh == null || virtualList == null || viewport == null || consoleModel == null)
                    throw new MissingMemberException("Class picker view bindings");
                Patch(harmony, pc, "get_HasFilter", nameof(EnableFilter));
                Patch(harmony, pc, "IsVisible", nameof(FilterRow));
                Patch(harmony, typeof(CharGenClassSelectorConsoleView), "EntityComparer", nameof(ConsoleOrder));
                Patch(harmony, typeof(CharGenClassSelectorItemConsoleView), "RefreshView", nameof(ConsoleLabel));
            }
            catch (Exception e) { harmony.UnpatchAll(Owner); Report(e); }
        }
        static void Patch(Harmony harmony, Type type, string method, string postfix)
        {
            harmony.Patch(AccessTools.DeclaredMethod(type, method), postfix: new HarmonyMethod(typeof(ClassPicker), postfix));
        }
        internal static void Report(Exception e)
        {
            if (reported) return;
            reported = true;
            if (log != null) log("Class picker: " + e);
        }
        static void EnableFilter(CharGenClassSelectorPCView __instance, ref bool __result)
        {
            var bar = __instance.GetComponent<ClassFamilyBar>();
            try
            {
                var group = (NestedSelectionGroupRadioVM<CharGenClassSelectorItemVM>)model.GetValue(__instance, null);
                if (bar != null && bar.Matches(group)) { __result |= bar.Filtering; return; }
                var phase = group == null ? null : group.NestedEntityCollections.Keys.OfType<CharGenClassPhaseVM>().FirstOrDefault();
                var list = ClassFamilyList.For(phase);
                if (list == null || !list.Usable) { if (bar != null) bar.ResetView(); return; }
                if (bar == null) bar = __instance.gameObject.AddComponent<ClassFamilyBar>();
                var scroll = (VirtualListComponent)virtualList.GetValue(__instance);
                bar.Bind(__instance, group, phase, list, (RectTransform)viewport.GetValue(scroll), scroll);
                __result |= bar.Filtering;
            }
            catch (Exception e) { if (bar != null) bar.ResetView(); Report(e); }
        }
        static void FilterRow(CharGenClassSelectorPCView __instance, CharGenClassSelectorItemVM __0, ref bool __result)
        {
            if (!__result) return;
            var bar = __instance.GetComponent<ClassFamilyBar>();
            if (bar != null && bar.Filtering) __result = bar.Shows(__0);
        }
        static void ConsoleOrder(CharGenClassSelectorItemVM __0, CharGenClassSelectorItemVM __1, ref int __result)
        {
            if (__0 == null || __1 == null || __0.IsArchetype || __1.IsArchetype) return;
            var list = ClassFamilyList.For(__0.source as CharGenClassPhaseVM);
            ClassFamily a, b;
            if (list != null && list.FamilyOf(__0, out a) && list.FamilyOf(__1, out b) && a != b) __result = a.CompareTo(b);
        }
        static void ConsoleLabel(CharGenClassSelectorItemConsoleView __instance, TextMeshProUGUI ___m_DisplayName)
        {
            var row = (CharGenClassSelectorItemVM)consoleModel.GetValue(__instance, null);
            if (row == null || row.IsArchetype) return;
            var list = ClassFamilyList.For(row.source as CharGenClassPhaseVM);
            ClassFamily family;
            if (list != null && list.FamilyOf(row, out family))
                ___m_DisplayName.text = list.Label(family) + " · " + ___m_DisplayName.text;
        }
    }
}
