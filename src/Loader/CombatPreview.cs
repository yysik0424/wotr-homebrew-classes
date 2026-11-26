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
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.MVVM._PCView.CombatLog;
using Kingmaker.UI.MVVM._PCView.InGame;
using Kingmaker.UI.MVVM._ConsoleView.InGame;
using Kingmaker.UI.MVVM._VM.Tooltip.Bricks;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Owlcat.Runtime.UI.Tooltips;
using UnityEngine;

namespace HomebrewClasses.Loader
{
    internal sealed class CombatPreview : IUnitDirectHoverUIHandler, IAbilityTargetSelectionUIHandler,
        IUnitCommandStartHandler, IGlobalRulebookHandler<RuleAttackRoll>, IGlobalRulebookHandler<RuleSavingThrow>,
        IGlobalRulebookHandler<RuleSpellResistanceCheck>, IGlobalRulebookHandler<RuleDealDamage>
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
        string notice;
        RuleAttackRoll lastDamageRoll;
        int lastPacket;
        float expires, nextRefresh;
        bool inCombat;
        CombatLogBaseView combatLog;
        CombatPreviewPanel panel;

        internal static void Install(Action<string> error)
        {
            report = error;
            try
            {
                var h = new Harmony("HomebrewClasses.CombatPreview");
                h.Patch(AccessTools.Method(typeof(TooltipTemplateAbility), "GetBody"), postfix: new HarmonyMethod(typeof(CombatPreview), nameof(AbilityTooltip)));
                h.Patch(AccessTools.Method(typeof(TooltipTemplateActivatableAbility), "GetBody"), postfix: new HarmonyMethod(typeof(CombatPreview), nameof(ToggleTooltip)));
                h.Patch(AccessTools.DeclaredMethod(typeof(CombatLogBaseView), "BindViewImplementation"), postfix: new HarmonyMethod(typeof(CombatPreview), nameof(BindLog)));
                h.Patch(AccessTools.DeclaredMethod(typeof(CombatLogBaseView), "DestroyViewImplementation"), postfix: new HarmonyMethod(typeof(CombatPreview), nameof(UnbindLog)));
                EventBus.Subscribe(Instance);
            }
            catch (Exception ex) { Report(ex); }
        }

        static void BindLog(CombatLogBaseView __instance)
        {
            // The world map and army battles have their own combat logs.
            if (__instance.GetComponentInParent<InGameStaticPartPCView>(true) != null ||
                __instance.GetComponentInParent<InGameStaticPartConsoleView>(true) != null)
                Instance.UseLog(__instance);
        }

        static void UnbindLog(CombatLogBaseView __instance)
        {
            if (ReferenceEquals(Instance.combatLog, __instance)) Instance.UseLog(null);
        }

        void UseLog(CombatLogBaseView view)
        {
            if (ReferenceEquals(combatLog, view)) return;
            Hide();
            if (panel != null) UnityEngine.Object.Destroy(panel.gameObject);
            panel = null;
            combatLog = view;
            nextRefresh = 0;
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

        static bool PartyCombat(UnitEntityData actor, UnitEntityData target)
        {
            return Active && actor != null && target != null && (actor.IsPlayerFaction || target.IsPlayerFaction) && actor.IsEnemy(target);
        }

        void Notice(UnitEntityData actor, UnitEntityData target, string detail)
        {
            notice = actor.CharacterName + " → " + target.CharacterName + "   " + detail;
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
        internal static string ResolvedAttack(RuleAttackRoll evt, bool ko)
        {
            string outcome;
            switch (evt.Result)
            {
                case AttackResult.CriticalHit: outcome = ko ? "치명타" : "critical hit"; break;
                case AttackResult.MirrorImage: outcome = ko ? "환영 적중" : "mirror image"; break;
                case AttackResult.Concealment: outcome = ko ? "은폐로 빗나감" : "concealment miss"; break;
                case AttackResult.Parried: outcome = ko ? "받아넘김" : "parried"; break;
                default: outcome = ko ? (evt.IsHit ? "명중" : "빗나감") : (evt.IsHit ? "hit" : "miss"); break;
            }
            if (evt.AutoMiss) return ko ? "자동 빗나감" : "automatic miss";
            if (evt.AutoHit) return evt.Result == AttackResult.Hit ? (ko ? "자동 명중" : "automatic hit") :
                (ko ? "자동 명중 · " : "automatic hit · ") + outcome;
            if (evt.D20 == null || !evt.D20.IsTriggered) return outcome;
            string result = "d20 " + evt.D20.Result + evt.AttackBonus.ToString(" +0; −0; +0") + " = " + evt.Roll +
                " / AC " + evt.TargetAC + " · " + outcome;
            if (evt.Initiator != null && evt.ACRule != null)
            {
                // The resolved rule carries the target-specific bonus and AC after native and mod listeners.
                // Keep rerolls and avoidance separate from the d20 threshold shown here.
                double chance = AttackOdds.Chance(evt.AttackBonus, evt.TargetAC, evt.Initiator.State.Features.AlwaysChance, 0, 0, 0, 0, 0, 0);
                result += (ko ? " · 실제 보정 d20 1회 " : " · single d20 at resolved modifiers ") + Math.Round(chance) + "%";
            }
            return result;
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (evt.IsFake || !PartyCombat(evt.Initiator, evt.Target)) return;
            try
            {
                bool ko = ActionChecks.Korean(evt.Reason?.Ability?.Blueprint) || ActionChecks.Hangul(evt.Target.CharacterName);
                string name = evt.Reason?.Ability?.Name ?? evt.WeaponStats?.Weapon?.Name;
                string text = name + " · " + ResolvedAttack(evt, ko);
                if (ReferenceEquals(evt, lastDamageRoll)) text += (ko ? " · 실제 피해 " : " · damage dealt ") + lastPacket;
                Notice(evt.Initiator, evt.Target, text);
            }
            catch (Exception ex) { Report(ex); }
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt) {}
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt.IsFake || evt.AttackRoll?.IsFake == true || !PartyCombat(evt.Initiator, evt.Target)) return;
            try
            {
                var roll = evt.AttackRoll;
                if (roll != null && (!ReferenceEquals(roll.Initiator, evt.Initiator) || !ReferenceEquals(roll.Target, evt.Target))) roll = null;
                bool ko = ActionChecks.Korean(evt.Reason?.Ability?.Blueprint) || ActionChecks.Hangul(evt.Target.CharacterName);
                string name = evt.Reason?.Ability?.Name ?? roll?.WeaponStats?.Weapon?.Name ?? evt.Reason?.Name;
                string detail = string.IsNullOrEmpty(name) ? "" : name + " · ";
                if (roll != null) detail += ResolvedAttack(roll, ko) + " · ";
                lastDamageRoll = roll;
                lastPacket = evt.Result;
                Notice(evt.Initiator, evt.Target, detail + (ko ? "실제 피해 " : "damage dealt ") + evt.Result);
            }
            catch (Exception ex) { Report(ex); }
        }
        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
            var caster = evt.Reason?.Caster ?? evt.Reason?.Ability?.Caster?.Unit;
            if (!Incoming(caster, evt.Initiator)) return;
            try
            {
                bool ko = ActionChecks.Korean(evt.Reason?.Ability?.Blueprint) || ActionChecks.Hangul(evt.Initiator.CharacterName);
                Notice(caster, evt.Initiator, evt.Reason?.Name + " · " + ActionChecks.SaveName(evt.Type, ko) + " DC " + evt.NonNegativeDC + " · "
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
                if (inCombat) { hover = null; shownAbility = null; shownTarget = null; notice = null; lastDamageRoll = null; WeaponPreview.Clear(); Hide(); inCombat = false; }
                return;
            }
            inCombat = true;
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            var selected = (turn == null ? null : TurnAbility(turn)) ?? Game.Instance.SelectedAbilityHandler?.SelectedAbility;
            var highlighted = turn == null ? null : TurnTarget(turn);
            if (selected == null && highlighted != null) selected = turn.CurrentAbility;
            var target = highlighted ?? hover;
            if (selected == shownAbility && ReferenceEquals(shownTarget, target) && Time.unscaledTime < nextRefresh) return;
            shownAbility = selected;
            shownTarget = target;
            nextRefresh = Time.unscaledTime + 0.25f;
            string text = null;
            if (selected != null) text = SkillPreview.Selected(selected, target, ActionChecks.Korean(selected.Blueprint));
            else if (target != null && turn?.SelectedUnit is UnitEntityData actor && actor.IsDirectlyControllable)
            {
                bool ko = ActionChecks.Korean(null) || ActionChecks.Hangul(actor.CharacterName);
                string values = WeaponPreview.Describe(actor, target, ko);
                if (values != null) text = actor.CharacterName + " → " + target.CharacterName + "\n" + values;
            }
            if (notice != null && Time.unscaledTime < expires)
                text = string.IsNullOrEmpty(text) ? notice : text + "\n" + notice;
            if (string.IsNullOrEmpty(text)) { Hide(); return; }
            if (panel == null)
            {
                var anchor = combatLog != null ? combatLog.GetTransform() : null;
                var font = UIRoot.Instance?.DefaultTMPFontAsset;
                if (anchor == null || font == null) return;
                panel = CombatPreviewPanel.Create(anchor, font);
            }
            panel.Show(text);
        }

        void Hide()
        {
            if (panel != null && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
        }
    }
}
