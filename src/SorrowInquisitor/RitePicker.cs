using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json;
using UnityEngine;

namespace SorrowInquisitor
{
    // Store choices on this individual command. Queued commands from two inquisitors cannot exchange beads.
    public sealed class ChosenRite : AbilityData
    {
        [JsonProperty] public string[] Selection;
        [JsonConstructor] private ChosenRite(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility blueprint, Kingmaker.UnitLogic.UnitDescriptor caster) : base(blueprint, caster) { }
        internal ChosenRite(AbilityData original, string[] ids) : base(original.Blueprint,original.Caster) { Selection = ids; }
    }

    [HarmonyPatch(typeof(UnitCommands), "Run", new[] { typeof(UnitCommand), typeof(bool), typeof(bool) })]
    internal static class SelectBeforeAction
    {
        static bool Prefix(UnitCommand __0, bool __1, bool __2)
        {
            if (!Blueprints.Registered || !(__0 is UnitUseAbility use) || use.Ability is ChosenRite || use.Cutscene) return true;
            var rite = use.Ability.Blueprint.GetComponent<PerformRite>();
            if (rite == null || rite.Kind == Rite.Vow) return true;
            if (Rosary.Party(use.Ability.Caster.Unit)) RitePicker.Open(use,rite.Kind,__1,__2);
            return false;
        }
    }

    public sealed class RitePicker : MonoBehaviour
    {
        static RitePicker current;
        UnitUseAbility original;
        Rite kind;
        readonly HashSet<string> picked = new HashSet<string>();
        Font pickerFont;
        bool fromQueue, keepQueue;
        static readonly Action<UnitCommands,UnitCommand,bool,bool> run = AccessTools.MethodDelegate<Action<UnitCommands,UnitCommand,bool,bool>>(
            AccessTools.Method(typeof(UnitCommands), "Run", new[]{typeof(UnitCommand),typeof(bool),typeof(bool)}));
        Rect rect;
        Vector2 scroll;
        internal static void Open(UnitUseAbility use, Rite kind, bool fromQueue, bool keepQueue)
        {
            if (current != null) current.Close();
            current = new GameObject("Sorrow Inquisitor Rosary").AddComponent<RitePicker>();
            current.fromQueue = fromQueue; current.keepQueue = keepQueue;
            current.pickerFont = Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Noto Sans CJK KR","Arial"},16);
            current.original = use; current.kind = kind;
            float w = Mathf.Min(Screen.width - 16, 540), h = Mathf.Min(Screen.height - 16, 385);
            current.rect = new Rect((Screen.width-w)/2, (Screen.height-h)/2, w, h);
        }
        void Close() { original = null; if (current == this) current = null; Destroy(gameObject); }
        void OnDestroy() { if (current == this) current = null; if (pickerFont != null) Destroy(pickerFont); }
        void OnGUI()
        {
            if (original == null) return;
            var u = original.Ability.Caster.Unit;
            if (Game.Instance?.Player == null || !Rosary.Party(u) || !u.IsInGame || u.State.IsDead ||
                original.Target.Unit == null || !original.Target.Unit.IsInGame) { Close(); return; }
            GUI.depth = -1000;
            var oldFont = GUI.skin.font;
            try
            {
                if (pickerFont != null) GUI.skin.font = pickerFont;
                rect = GUI.ModalWindow(GetInstanceID(),rect,Draw,original.Ability.Blueprint.Name);
            }
            finally { GUI.skin.font = oldFont; }
        }
        void Draw(int window)
        {
            var u = original.Ability.Caster.Unit; var t = original.Target.Unit;
            var p = Rosary.Part(u);
            int limit = Rosary.Capacity(u,p,kind);
            var rows = new List<KeyValuePair<string,string>>();
            if (kind == Rite.Gather)
            {
                foreach (var b in t.Buffs.Enumerable.Where(Afflictions.Eligible))
                {
                    var poison = b.Components.FirstOrDefault(c => c.SourceBlueprintComponent is Kingmaker.Designers.Mechanics.Buffs.BuffPoisonStatDamage);
                    var stage = poison == null ? "" : string.Format(Rosary.Text("stage"),poison.GetData<Kingmaker.Designers.Mechanics.Buffs.BuffPoisonStatDamageData>().TicksPassed);
                    rows.Add(new KeyValuePair<string,string>(b.UniqueId,b.Blueprint.Name + "   " +
                        (b.Context.MaybeCaster?.CharacterName ?? Rosary.Text("environment")) + "   " +
                        (b.IsPermanent ? "∞" : Math.Ceiling(b.TimeLeft.TotalSeconds) + " s") + stage));
                }
            }
            else foreach (var b in p.Beads)
                rows.Add(new KeyValuePair<string,string>(b.Id,b.Blueprint.Name + "   " + (b.AggressorName ?? Rosary.Text("environment")) +
                    " → " + b.AllyName + "   " + Math.Ceiling(TimeSpan.FromTicks(b.Expires - Afflictions.Now).TotalSeconds) + " s"));
            picked.IntersectWith(rows.Select(r => r.Key));
            GUILayout.BeginArea(new Rect(16,30,rect.width-32,rect.height-42));
            GUILayout.Label(string.Format(Rosary.Text("counts"),u.CharacterName,p.Beads.Count,Rules.Slots(Rosary.Level(u)),Rosary.Uses(u,p),limit));
            GUILayout.Label(new string('●', p.Beads.Count) + new string('○', Math.Max(0,Rules.Slots(Rosary.Level(u))-p.Beads.Count)));
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var row in rows)
            {
                bool before = picked.Contains(row.Key);
                GUI.enabled = before || picked.Count < limit;
                bool after = GUILayout.Toggle(before,row.Value,GUI.skin.button,GUILayout.MinHeight(32));
                if (after) picked.Add(row.Key); else picked.Remove(row.Key);
            }
            GUI.enabled = true;
            if (rows.Count == 0) GUILayout.Label(Rosary.Text("empty"));
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Rosary.Text("cancel"))) Close();
            GUI.enabled = original != null && picked.Count > 0 && picked.Count <= limit;
            if (GUILayout.Button(Rosary.Text("confirm")))
            {
                var chosen = new ChosenRite(original.Ability,rows.Where(r => picked.Contains(r.Key)).Select(r => r.Key).ToArray());
                var command = UnitUseAbility.CreateCastCommand(chosen, original.Target, original.Type);
                Close(); run(u.Commands, command, fromQueue, keepQueue);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
    }
}
