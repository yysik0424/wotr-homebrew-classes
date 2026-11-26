using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Inspect;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.MVVM._VM.Tooltip.Bricks;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Owlcat.Runtime.UI.Tooltips;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HomebrewClasses.Loader
{
    internal sealed class CombatPreview : IUnitDirectHoverUIHandler, IAbilityTargetSelectionUIHandler,
        IUnitCommandStartHandler, IGlobalRulebookHandler<RuleAttackRoll>, IGlobalRulebookHandler<RuleSavingThrow>,
        IGlobalRulebookHandler<RuleSpellResistanceCheck>
    {
        static readonly CombatPreview Instance = new CombatPreview();
        static Action<string> report;
        static bool failed;
        // These are the same references that TurnController updates for map, portrait and target-selector hover.
        static readonly AccessTools.FieldRef<TurnBased.Controllers.TurnController, AbilityData> TurnAbility =
            AccessTools.FieldRefAccess<TurnBased.Controllers.TurnController, AbilityData>("m_CurrentAbilityInTargetSelector");
        static readonly AccessTools.FieldRef<TurnBased.Controllers.TurnController, UnitEntityData> TurnTarget =
            AccessTools.FieldRefAccess<TurnBased.Controllers.TurnController, UnitEntityData>("m_HighlightedUnit");
        UnitEntityData hover;
        UnitEntityData shownTarget;
        AbilityData shownAbility;
        string incoming;
        float expires, nextRefresh;
        bool inCombat;
        TextMeshProUGUI label;
        GameObject panel;

        internal static void Install(Action<string> error)
        {
            report = error;
            try
            {
                var h = new Harmony("HomebrewClasses.CombatPreview");
                h.Patch(AccessTools.Method(typeof(TooltipTemplateAbility), "GetBody"), postfix: new HarmonyMethod(typeof(CombatPreview), nameof(AbilityTooltip)));
                h.Patch(AccessTools.Method(typeof(TooltipTemplateActivatableAbility), "GetBody"), postfix: new HarmonyMethod(typeof(CombatPreview), nameof(ToggleTooltip)));
                EventBus.Subscribe(Instance);
                h.Patch(AccessTools.Method(typeof(TooltipTemplateBuff), "GetBody"), postfix: new HarmonyMethod(typeof(CombatPreview), nameof(BuffTooltip)));
            }
            catch (Exception ex) { Report(ex); }
        }

        internal static bool Active
        {
            get { return Game.HasInstance && Game.Instance.Player != null && !Game.Instance.IsFullScreenUI &&
                Game.Instance.CurrentMode != GameModeType.EscMode && TurnBased.Controllers.CombatController.IsInTurnBasedCombat(); }
        }

        internal static void Report(Exception ex)
        {
            if (failed) return;
            failed = true;
            report?.Invoke("Turn-based combat preview: " + ex);
        }

        static void AddLine(string line, ref IEnumerable<ITooltipBrick> body)
        {
            if (!string.IsNullOrEmpty(line)) body = new ITooltipBrick[] { new TooltipBrickText(line, TooltipTextType.Bold) }.Concat(body);
        }

        static void AbilityTooltip(BlueprintAbility ___BlueprintAbility, AbilityData ___m_AbilityData, ref IEnumerable<ITooltipBrick> __result)
        {
            if (!Active || ___BlueprintAbility == null) return;
            try
            {
                bool ko = ActionChecks.Korean(___BlueprintAbility);
                AddLine(SkillPreview.Tooltip(___BlueprintAbility, ___m_AbilityData, ko), ref __result);
            }
            catch (Exception ex) { Report(ex); }
        }
        static void ToggleTooltip(BlueprintActivatableAbility ___BlueprintActivatableAbility, ActivatableAbility ___m_Ability, ref IEnumerable<ITooltipBrick> __result)
        {
            if (!Active || ___BlueprintActivatableAbility == null) return;
            try { AddLine(SkillPreview.Toggle(___BlueprintActivatableAbility, ___m_Ability, ActionChecks.Korean(___BlueprintActivatableAbility)), ref __result); }
            catch (Exception ex) { Report(ex); }
        }

        static void BuffTooltip(Buff ___Buff, ref IEnumerable<ITooltipBrick> __result)
        {
            if (!Active || ___Buff == null) return;
            try { AddLine(PreviewDuration.Remaining(___Buff, ActionChecks.Korean(___Buff.Blueprint)), ref __result); }
            catch (Exception ex) { Report(ex); }
        }

        public void HandleHoverChange(UnitEntityView unit, bool isHover)
        {
            if (unit == null) return;
            if (isHover && Active) hover = unit.Data;
            else if (ReferenceEquals(hover, unit.Data)) hover = null;
            nextRefresh = 0;
        }
        public void HandleAbilityTargetSelectionStart(AbilityData ability) { nextRefresh = 0; }
        public void HandleAbilityTargetSelectionEnd(AbilityData ability) { nextRefresh = 0; }

        static bool Incoming(UnitEntityData actor, UnitEntityData target)
        {
            return Active && actor != null && target != null && target.IsPlayerFaction && actor.IsEnemy(target);
        }

        void Notice(UnitEntityData actor, UnitEntityData target, string detail)
        {
            incoming = actor.CharacterName + " → " + target.CharacterName + "   " + detail;
            expires = Time.unscaledTime + 4f;
            nextRefresh = 0;
        }
        public void HandleUnitCommandDidStart(UnitCommand command)
        {
            if (!Incoming(command.Executor, command.TargetUnit)) return;
            try
            {
                var target = command.TargetUnit;
                if (command is UnitUseAbility use && use.Ability != null)
                {
                    var ability = use.Ability;
                    var checks = ActionChecks.Defenses(ability, target, ActionChecks.Korean(ability.Blueprint));
                    Notice(command.Executor, target, ability.Name + (checks.Length == 0 ? "" : " · " + checks));
                }
                else if (command is UnitAttack)
                {
                    var weapon = command.Executor.Body.PrimaryHand.MaybeWeapon;
                    if (weapon != null) Notice(command.Executor, target, weapon.Name);
                }
            }
            catch (Exception ex) { Report(ex); }
        }
        public void OnEventAboutToTrigger(RuleAttackRoll evt) {}
        public void OnEventAboutToTrigger(RuleSavingThrow evt) {}
        public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt) {}
        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (evt.IsFake || !Incoming(evt.Initiator, evt.Target)) return;
            try
            {
                bool ko = ActionChecks.Korean(evt.Reason.Ability?.Blueprint) || ActionChecks.Hangul(evt.Target.CharacterName) || ActionChecks.Hangul(evt.Initiator.CharacterName);
                var attack = ActionChecks.AttackName(evt.AttackType, ko);
                string name = evt.Reason.Ability?.Name ?? evt.Weapon?.Name;
                string result = ko ? (evt.IsHit ? "명중" : "빗나감") : (evt.IsHit ? "hit" : "miss");
                Notice(evt.Initiator, evt.Target, name + " · " + attack + " · AC " + evt.TargetAC + " · " + result);
            }
            catch (Exception ex) { Report(ex); }
        }
        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
            var caster = evt.Reason.Caster ?? evt.Reason.Ability?.Caster?.Unit;
            if (!Incoming(caster, evt.Initiator)) return;
            try
            {
                bool ko = ActionChecks.Korean(evt.Reason.Ability?.Blueprint) || ActionChecks.Hangul(evt.Initiator.CharacterName);
                Notice(caster, evt.Initiator, evt.Reason.Name + " · " + ActionChecks.SaveName(evt.Type, ko) + " DC " + evt.NonNegativeDC + " · "
                    + (ko ? (evt.Success ? "내성 성공" : "내성 실패") : (evt.Success ? "saved" : "failed save")));
            }
            catch (Exception ex) { Report(ex); }
        }
        public void OnEventDidTrigger(RuleSpellResistanceCheck evt)
        {
            if (!Incoming(evt.Initiator, evt.Target) || (!evt.HasResistanceRoll && !evt.TargetIsImmune)) return;
            try
            {
                bool ko = ActionChecks.Korean(evt.Ability) || ActionChecks.Hangul(evt.Target.CharacterName);
                Notice(evt.Initiator, evt.Target, evt.Ability?.Name + " · SR " + evt.SpellResistance + " · "
                    + (ko ? (evt.IsSpellResisted ? "저항 성공" : "관통") : (evt.IsSpellResisted ? "resisted" : "penetrated")));
            }
            catch (Exception ex) { Report(ex); }
        }

        internal static void Tick()
        {
            try { Instance.Update(); }
            catch (Exception ex) { Report(ex); Instance.Hide(); }
        }
        void Update()
        {
            if (!Active)
            {
                if (inCombat) { hover = null; shownAbility = null; shownTarget = null; incoming = null; Hide(); inCombat = false; }
                return;
            }
            inCombat = true;
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            var selected = (turn == null ? null : TurnAbility(turn)) ?? Game.Instance.SelectedAbilityHandler?.SelectedAbility;
            var target = (turn == null ? null : TurnTarget(turn)) ?? hover;
            if (selected == shownAbility && ReferenceEquals(shownTarget, target) && Time.unscaledTime < nextRefresh) return;
            shownAbility = selected;
            shownTarget = target;
            nextRefresh = Time.unscaledTime + 0.25f;
            string text = null;
            if (selected != null) text = SkillPreview.Selected(selected, target, ActionChecks.Korean(selected.Blueprint));
            if (string.IsNullOrEmpty(text) && Time.unscaledTime < expires) text = incoming;
            if (string.IsNullOrEmpty(text)) { Hide(); return; }
            if (!EnsurePanel()) return;
            if (!panel.activeSelf) panel.SetActive(true);
            if (label.text != text) label.text = text;
        }
        void Hide() { if (panel != null && panel.activeSelf) panel.SetActive(false); }
        bool EnsurePanel()
        {
            if (panel != null) return true;
            var canvas = Game.Instance.UI?.Canvas;
            if (canvas == null || UIRoot.Instance?.DefaultTMPFontAsset == null) return false;
            panel = new GameObject("Homebrew combat preview", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = new Vector2(0.18f, 0.76f);
            rect.anchorMax = new Vector2(0.82f, 0.76f);
            rect.sizeDelta = new Vector2(0, 82);
            rect.anchoredPosition = Vector2.zero;
            var bg = panel.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = new Color(0.06f, 0.05f, 0.04f, 0.92f);
            var group = panel.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var caption = new GameObject("Checks", typeof(RectTransform), typeof(TextMeshProUGUI));
            var inner = (RectTransform)caption.transform;
            inner.SetParent(rect, false);
            inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(12, 4); inner.offsetMax = new Vector2(-12, -4);
            label = caption.GetComponent<TextMeshProUGUI>();
            label.font = UIRoot.Instance.DefaultTMPFontAsset;
            label.fontSize = 20; label.enableAutoSizing = true; label.fontSizeMin = 15; label.fontSizeMax = 20;
            label.alignment = TextAlignmentOptions.Center;
            label.richText = false;
            label.raycastTarget = false;
            return true;
        }
    }
}
