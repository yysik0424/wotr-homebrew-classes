using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UI.MVVM._PCView.ActionBar;
using Kingmaker.UI.MVVM._ConsoleView.ActionBar;
using TMPro;

namespace Deathdealer
{
    // The game opens variants for a normal ability slot, but a popup child is a
    // converted-spell slot. Show the four forms in the existing popup when that child is chosen.
    [HarmonyPatch(typeof(ActionBarSlotVM),nameof(ActionBarSlotVM.OnShowConvertRequest))]
    static class ArcanaPopup
    {
        static readonly List<WeakReference<ActionBarSlotVM>> open = new List<WeakReference<ActionBarSlotVM>>();

        static void Postfix(ActionBarSlotVM __instance)
        {
            if (!(__instance.MechanicActionBarSlot is MechanicActionBarSlotAbility slot) ||
                slot.Ability?.Blueprint != Arcana.Menu || Arcana.Menu == null) return;
            open.RemoveAll(r=>!r.TryGetTarget(out var vm) || vm==__instance || vm.ConvertedVm.Value==null);
            if (__instance.ConvertedVm.Value != null) open.Add(new WeakReference<ActionBarSlotVM>(__instance));
        }
        internal static bool ChooseForm(ActionBarSlotVM child)
        {
            if (!(child.MechanicActionBarSlot is MechanicActionBarSlotSpontaneusConvertedSpell slot) ||
                Arcana.Genie == null || slot.Spell?.Blueprint != Arcana.Genie) return false;
            foreach (var reference in open)
            {
                if (!reference.TryGetTarget(out var root) || root.ConvertedVm.Value?.Slots.Contains(child)!=true) continue;
                if (slot.Spell.GetAvailableForCastCount() > 0 && slot.Spell.IsVisible())
                {
                    var forms = slot.Spell.GetConversions().Where(a=>a.IsVisible()).ToList();
                    if (forms.Count != 4) throw new InvalidOperationException("Geniekind form list is incomplete.");
                    var slots = new SlotConversion(forms).GetMechanicSlots(slot.Unit).ToList();
                    root.CloseConvert();
                    root.ConvertedVm.Value = new ActionBarConvertedVM(slots,root.CloseConvert);
                }
                return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ActionBarSlotVM),nameof(ActionBarSlotVM.OnMainClick))]
    static class ArcanaPopupSelection
    {
        static bool Prefix(ActionBarSlotVM __instance) => !ArcanaPopup.ChooseForm(__instance);
    }

    [HarmonyPatch(typeof(ActionBarBaseSlotView), "SetResourceCount")]
    static class DailyCount
    {
        internal static void Display(ActionBarBaseSlotView view, int remaining, TMP_Text label)
        {
            if (remaining < 0) return;
            var slot = view.MechanicActionBarSlot;
            var ability = (slot as MechanicActionBarSlotSpontaneusConvertedSpell)?.Spell ??
                (slot as MechanicActionBarSlotAbility)?.Ability;
            if (ability?.Blueprint.GetComponent<LearnedArcana>() != null)
                label.text = remaining + "/1";
        }
        static void Postfix(ActionBarBaseSlotView __instance, int value, TextMeshProUGUI ___m_ResourceCount)
            => Display(__instance,value,___m_ResourceCount);
    }
    [HarmonyPatch(typeof(ActionBarBaseSlotConsoleView), "SetResourceCount")]
    static class ConsoleDailyCount
    {
        static void Postfix(ActionBarBaseSlotConsoleView __instance, int value, TextMeshProUGUI ___m_ResourceCount)
            => DailyCount.Display(__instance,value,___m_ResourceCount);
    }
}
