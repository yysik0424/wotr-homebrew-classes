using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.UI.MVVM._ConsoleView.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using UniRx;
using Owlcat.Runtime.UI.VirtualListSystem;
using TMPro;
using UnityEngine;

namespace HomebrewClasses.Loader
{
    internal static class ClassPicker
    {
        internal const string Owner = "HomebrewClasses.ClassPicker";
        static readonly Type pc = typeof(CharGenClassSelectorPCView);
        static readonly Type phaseView = typeof(CharGenClassPhaseDetailedPCView);
        static readonly PropertyInfo model = AccessTools.Property(phaseView, "ViewModel");
        static readonly FieldInfo visibleRows = AccessTools.Field(pc, "VisibleCollection");
        internal static readonly MethodInfo Refresh = AccessTools.Method(pc, "OnCollectionChanged");
        static readonly FieldInfo virtualList = AccessTools.Field(pc, "VirtualList");
        static readonly FieldInfo viewport = AccessTools.Field(typeof(VirtualListComponent), "m_Viewport");
        static readonly PropertyInfo consoleModel = AccessTools.Property(typeof(CharGenClassSelectorItemConsoleView), "ViewModel");
        static Action<string> log;
        static Action<string> info;

        internal static void Install(Action<string> error, Action<string> trace)
        {
            log = error;
            info = trace;
            var harmony = new Harmony(Owner);
            try
            {
                if (visibleRows == null || model == null || Refresh == null || virtualList == null || viewport == null)
                    throw new MissingMemberException("Class picker view bindings");
                Patch(harmony, phaseView, "BindViewImplementation", nameof(BindClassPhase));
                Trace("PC class phase hook installed");
            }
            catch (Exception e) { Report(e); }
            try
            {
                if (consoleModel == null) throw new MissingMemberException("Controller class picker view model");
                Patch(harmony, typeof(CharGenClassSelectorConsoleView), "EntityComparer", nameof(ConsoleOrder));
                Patch(harmony, typeof(CharGenClassSelectorItemConsoleView), "RefreshView", nameof(ConsoleLabel));
            }
            catch (Exception e) { Report(e); }
        }
        static void Patch(Harmony harmony, Type type, string method, string postfix)
        {
            harmony.Patch(AccessTools.DeclaredMethod(type, method), postfix: new HarmonyMethod(typeof(ClassPicker), postfix));
        }
        internal static void Report(Exception e)
        {
            log?.Invoke("Class picker: " + e);
        }
        internal static void Trace(string message) { info?.Invoke("Class picker: " + message); }

        // Both PC creation and level-up bind their selector in this concrete method.
        // VisibleCollection is the same reactive collection that the virtual list renders.
        static void BindClassPhase(CharGenClassPhaseDetailedPCView __instance, CharGenClassSelectorPCView ___ClassListSelectorPcView)
        {
            ClassFamilyBar bar = null;
            try
            {
                var view = ___ClassListSelectorPcView;
                bar = view.GetComponent<ClassFamilyBar>();
                var phase = (CharGenClassPhaseVM)model.GetValue(__instance, null);
                var families = ClassFamilyList.For(phase);
                if (families == null || !families.Usable)
                {
                    if (bar != null) bar.ResetView();
                    Trace("skipped phase " + phase?.Mode);
                    return;
                }
                var scroll = (VirtualListComponent)virtualList.GetValue(view);
                var rows = (ReactiveCollection<CharGenClassSelectorItemVM>)visibleRows.GetValue(view);
                if (bar == null) bar = view.gameObject.AddComponent<ClassFamilyBar>();
                bar.Bind(view, phase, families, (RectTransform)viewport.GetValue(scroll), scroll, rows);
                Trace(phase.Mode + " bound, " + rows.Count + " visible rows");
            }
            catch (Exception e) { if (bar != null) bar.ResetView(); Report(e); }
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
