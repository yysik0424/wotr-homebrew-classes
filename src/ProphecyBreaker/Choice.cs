using System;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Newtonsoft.Json;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Spells;
using UniRx;
using UnityEngine;
using WotrClassMod.Common;

namespace ProphecyBreaker
{
    // A level-up plan can be replayed on a different unit than its preview.
    // Persist the outgoing spell on the action, never on a window or a unit instance.
    public sealed class ResearchSelection : SelectSpell, ILevelUpAction
    {
        [JsonProperty] public string Outgoing;
        public ResearchSelection() {}
        internal ResearchSelection(SelectSpell pick,BlueprintAbility old)
            : base(pick.Spellbook,pick.SpellList,pick.SpellLevel,pick.Spell,pick.SlotIndex)
        { Outgoing = old?.AssetGuid.ToString(); }

        BlueprintAbility Previous => Outgoing == null ? null : BlueprintUtil.Get<BlueprintAbility>(Outgoing);
        bool ILevelUpAction.Check(LevelUpState state,UnitDescriptor unit) => base.Check(state,unit) &&
            Study.CanInscribe(unit.GetSpellbook(Spellbook),SpellLevel,Spell,Previous);
        void ILevelUpAction.Apply(LevelUpState state,UnitDescriptor unit)
        {
            var book = unit.GetSpellbook(Spellbook);
            if (!base.Check(state,unit) || Study.Inscribe(book,SpellLevel,Spell,Previous) == null)
                throw new InvalidOperationException("Research selection no longer valid");
            state.DemandSpellSelection(Spellbook,SpellList).SpendSlot(SpellLevel,Spell,SlotIndex);
        }
    }

    [HarmonyPatch(typeof(LevelUpController),"AddAction")]
    internal static class BeforeLevelChoice
    {
        static readonly Func<LevelUpController,ILevelUpAction,bool,bool> submit =
            AccessTools.MethodDelegate<Func<LevelUpController,ILevelUpAction,bool,bool>>(AccessTools.Method(typeof(LevelUpController),"AddAction"));
        static bool Prefix(LevelUpController __instance,ref ILevelUpAction __0,bool __1)
        {
            if (!(__0 is SelectSpell pick) || pick is ResearchSelection || !Study.Own(pick.Spellbook)) return true;
            var book = (__instance.Preview ?? __instance.Unit)?.Descriptor.GetSpellbook(pick.Spellbook);
            if (book == null || !Study.Open(book,pick.SpellLevel)) return false;
            var options = Study.Entries(book,pick.SpellLevel);
            if (options.Length < Rules.Capacity) { __0 = new ResearchSelection(pick,null); return true; }
            bool flag = __1;
            bool Active() => Game.Instance?.LevelUpController == __instance && __instance.Preview != null &&
                pick.Check(__instance.State,__instance.Preview.Descriptor);
            ReplacementPicker.Open(pick.SpellLevel,pick.Spell,options,Active,old => {
                if (Active()) submit(__instance,new ResearchSelection(pick,old),flag);
            });
            return false;
        }
    }

    // The native selection view checks an item before submitting the level-up action.
    // Reflect pending/cancelled selections in that same collection, which also updates its counter.
    [HarmonyPatch(typeof(CharGenSpellsPhaseVM),"OnSpellChosen")]
    internal static class PendingSelectionView
    {
        static void Postfix(CharGenSpellsPhaseVM __instance,CollectionAddEvent<CharGenSpellSelectorItemVM> __0,Spellbook ___PreviewSpellbook)
        {
            var item = __0.Value;
            if (Study.Own(___PreviewSpellbook?.Blueprint) && ReplacementPicker.Pending(item.Spell) &&
                !__instance.MechanicSelectedSpells.Contains(item.Spell))
                __instance.SelectedSpellVMs.Remove(item);
        }
    }

    [HarmonyPatch(typeof(CopyScroll),"CanCopySpell")]
    internal static class ScrollTier
    {
        static void Postfix(BlueprintAbility __0,Spellbook __1,ref bool __result)
        {
            if (__result && Study.Own(__1.Blueprint)) __result = Study.Open(__1,__1.Blueprint.SpellList.GetLevel(__0));
        }
    }

    [HarmonyPatch(typeof(CopyItem),"Copy")]
    internal static class BeforeScroll
    {
        static readonly Func<CopyScroll,ItemEntity,BlueprintAbility> extract =
            AccessTools.MethodDelegate<Func<CopyScroll,ItemEntity,BlueprintAbility>>(AccessTools.Method(typeof(CopyScroll),"ExtractSpell"));
        static readonly Func<BlueprintAbility,Spellbook,bool> eligible =
            AccessTools.MethodDelegate<Func<BlueprintAbility,Spellbook,bool>>(AccessTools.Method(typeof(CopyScroll),"CanCopySpell"));

        static bool Prefix(CopyItem __instance,ItemEntity __0,UnitEntityData __1)
        {
            if (IsCompleting(__instance,__0,__1)) return true;
            if (!(__instance is CopyScroll scroll) || Blueprints.Research == null || __1 == null) return true;
            var book = __1.Descriptor.GetSpellbook(Blueprints.Research);
            if (book == null) return true;
            var spell = extract(scroll,__0);
            if (spell == null || !eligible(spell,book)) return spell != null && __1.Descriptor.Spellbooks.Any(b => eligible(spell,b));
            int tier = book.Blueprint.SpellList.GetLevel(spell);
            var options = Study.Entries(book,tier);
            if (options.Length < Rules.Capacity) return true;
            bool Active() => __0 != null && Game.Instance?.Player?.Inventory != null && __0.Collection == Game.Instance.Player.Inventory &&
                __0.Count > 0 && ReferenceEquals(book,__1.Descriptor.GetSpellbook(Blueprints.Research)) && eligible(spell,book);
            if (Active()) ReplacementPicker.Open(tier,spell,options,Active,old => {
                if (!Active()) return;
                var learned = Study.Inscribe(book,tier,spell,old);
                if (learned == null) return;
                learned.CopiedFromScroll = true;
                // Research is now already known. Continue the native copy for other eligible
                // books and its normal scroll conservation / consumption rules.
                FinishCopy(scroll,__0,__1);
            });
            return false;
        }

        [ThreadStatic] static CopyItem completing;
        [ThreadStatic] static ItemEntity completingItem;
        [ThreadStatic] static UnitEntityData completingUnit;
        internal static bool IsCompleting(CopyItem copy,ItemEntity item,UnitEntityData unit) =>
            ReferenceEquals(copy,completing) && ReferenceEquals(item,completingItem) && ReferenceEquals(unit,completingUnit);
        static void FinishCopy(CopyScroll scroll,ItemEntity item,UnitEntityData unit)
        {
            completing = scroll; completingItem = item; completingUnit = unit;
            try { scroll.Copy(item,unit); }
            finally { completing = null; completingItem = null; completingUnit = null; }
        }
    }

    public sealed class ReplacementPicker : MonoBehaviour
    {
        static ReplacementPicker current;
        BlueprintAbility incoming;
        internal static bool Pending(BlueprintAbility spell) => current != null && current.incoming == spell;
        BlueprintAbility[] choices;
        Func<bool> active;
        Action<BlueprintAbility> confirm;
        string title;
        Font font;
        Rect rect;

        internal static void Open(int tier,BlueprintAbility spell,BlueprintAbility[] choices,Func<bool> active,Action<BlueprintAbility> confirm)
        {
            if (current != null) current.Close();
            var p = new GameObject("Prophecy Breaker Research").AddComponent<ReplacementPicker>();
            current = p;
            p.title = string.Format(Strings.L("Replace").ToString(),tier,spell.Name);
            p.incoming = spell; p.choices = choices; p.active = active; p.confirm = confirm;
            p.font = Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Noto Sans CJK KR","Arial"},16);
            float w = Mathf.Min(550,Screen.width-16), h = Mathf.Min(300,Screen.height-16);
            p.rect = new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h);
        }
        void Close() { confirm = null; if (current == this) current = null; Destroy(gameObject); }
        void OnDestroy() { if (current == this) current = null; if (font != null) Destroy(font); }
        void OnGUI()
        {
            if (confirm == null) return;
            if (!active()) { Close(); return; }
            var previous = GUI.skin.font;
            int depth = GUI.depth;
            try {
                if (font != null) GUI.skin.font = font;
                GUI.depth = -1000;
                rect = GUI.ModalWindow(GetInstanceID(),rect,Draw,title);
            } finally { GUI.skin.font = previous; GUI.depth = depth; }
        }
        void Draw(int id)
        {
            GUILayout.BeginArea(new Rect(16,34,rect.width-32,rect.height-48));
            foreach (var spell in choices)
                if (GUILayout.Button(spell.Name,GUILayout.MinHeight(32)) && confirm != null) {
                    var accept = confirm; Close(); accept(spell);
                }
            if (GUILayout.Button(Strings.L("Cancel").ToString())) Close();
            GUILayout.EndArea();
        }
    }
}
