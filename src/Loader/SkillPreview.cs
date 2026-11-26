using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Inspect;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;

namespace HomebrewClasses.Loader
{
    // Read the actions, never run them. In particular a duration calculation can roll dice.
    internal sealed class SkillPreview
    {
        readonly bool ko;
        readonly BlueprintAbility blueprint;
        readonly AbilityData data;
        readonly List<string> effects = new List<string>();
        readonly Metamagic meta;
        bool needsDescription;
        SkillPreview(BlueprintAbility blueprint, AbilityData data, bool ko)
        {
            this.blueprint = blueprint;
            this.ko = ko;
            this.data = data;
            meta = data?.MetamagicData?.MetamagicMask ?? 0;
        }
        string L(string korean, string english) => ko ? korean : english;

        internal static bool? Branch(ConditionsChecker checker, UnitEntityData known)
        {
            if (checker?.Conditions == null || ReferenceEquals(known, null)) return null;
            bool and = checker.Operation == Operation.And;
            bool unknown = false;
            foreach (var condition in checker.Conditions)
            {
                bool test;
                if (condition?.GetType() == typeof(ContextConditionHasBuff) && ((ContextConditionHasBuff)condition).Buff != null)
                    test = known.Buffs.HasFact(((ContextConditionHasBuff)condition).Buff);
                else if (condition?.GetType() == typeof(ContextConditionHasFact) && ((ContextConditionHasFact)condition).Fact != null)
                    test = known.Descriptor.HasFact(((ContextConditionHasFact)condition).Fact);
                else { unknown = true; continue; }
                if (condition.Not) test = !test;
                if (test != and) return test;
            }
            return unknown ? (bool?)null : and;
        }

        void Add(string effect, bool conditional, string recipient)
        {
            if (string.IsNullOrEmpty(effect)) return;
            string text = (conditional ? L("조건부 ", "conditional ") : "") + recipient + effect;
            if (!effects.Contains(text)) effects.Add(text);
        }

        string Name(BlueprintBuff buff) => buff?.Name ?? L("효과", "effect");
        string Existing(BlueprintBuff buff, UnitEntityData known)
        {
            var active = buff == null || ReferenceEquals(known, null) ? null : known.Buffs.GetBuff(buff);
            if (active == null) return "";
            string policy = "";
            switch (buff.Stacking)
            {
                case StackingType.Replace: policy = L("재적용 시 교체", "recast replaces"); break;
                case StackingType.Prolong: policy = L("재적용 시 연장", "recast extends"); break;
                case StackingType.Ignore: policy = L("중복 적용 불가", "duplicate ignored"); break;
                case StackingType.Stack: policy = L("별도 중첩", "stacks separately"); break;
            }
            return " (" + (buff.Stacking == StackingType.Stack ? L("한 중첩 · ", "one stack · ") : "") + PreviewDuration.Remaining(active, ko) + (policy.Length > 0 ? " · " + policy : "") + ")";
        }

        string Duration(ContextDurationValue d)
        {
            string value = PreviewDuration.Formula(d, meta, ko) ?? PreviewDuration.PerLevel(d, blueprint, data, meta, ko);
            if (value != null) return value;
            needsDescription = true;
            return PreviewDuration.Variable(d, meta, ko);
        }

        static readonly Dictionary<Type, MethodInfo> durationProviders = new Dictionary<Type, MethodInfo>();
        void Managed(GameAction action, UnitEntityData known, bool conditional, string recipient)
        {
            if (action == null) return;
            Type type = action.GetType();
            if (!durationProviders.TryGetValue(type, out var method))
            {
                method = type.GetMethod("GetCombatPreviewDurations", new[] { typeof(UnitEntityData) });
                if (method != null && !typeof(IEnumerable<KeyValuePair<BlueprintBuff, TimeSpan?>>).IsAssignableFrom(method.ReturnType)) method = null;
                durationProviders[type] = method;
            }
            if (method == null) return;
            bool pack = type.FullName == "PackWarden.PackEmpowerOnPack";
            var entries = (IEnumerable<KeyValuePair<BlueprintBuff, TimeSpan?>>)method.Invoke(action, new object[] { KnownCaster() });
            foreach (var entry in entries)
            {
                if (entry.Key == null) continue;
                string time = entry.Value.HasValue ? PreviewDuration.Clock(entry.Value.Value, ko) : L("시간 제한 없음", "no time limit");
                Add(Name(entry.Key) + L(" · 기본 ", " · base ") + time + Existing(entry.Key, pack ? null : known),
                    conditional, pack ? L("무리 구성원 · ", "pack members · ") : recipient);
            }
        }

        void Walk(ActionList list, UnitEntityData known, string recipient, bool conditional, int depth)
        {
            if (list?.Actions == null || depth > 16) return;
            foreach (var a in list.Actions)
            {
                string result = null;
                if (a is Conditional branch)
                {
                    var selected = Branch(branch.ConditionsChecker, known);
                    if (selected != false) Walk(branch.IfTrue, known, recipient, conditional || !selected.HasValue, depth + 1);
                    if (selected != true) Walk(branch.IfFalse, known, recipient, conditional || !selected.HasValue, depth + 1);
                }
                else if (a is ContextActionConditionalSaved saved)
                {
                    Walk(saved.Succeed, known, recipient + L("내성 성공 시 ", "on save "), conditional, depth + 1);
                    Walk(saved.Failed, known, recipient + L("내성 실패 시 ", "on failed save "), conditional, depth + 1);
                }
                else if (a is ContextActionSavingThrow save) Walk(save.Actions, known, recipient, conditional, depth + 1);
                else if (a is ContextActionsOnPet pet) Walk(pet.Actions, null, L("동물 동료 · ", "companion · "), conditional, depth + 1);
                else if (a is ContextActionPartyMembers party) Walk(party.Action, null, L("파티원 · ", "party · "), conditional, depth + 1);
                else if (a is ContextActionApplyBuff buff && buff.Buff != null)
                {
                    var ranked = buff.Permanent || buff.SameDuration || buff.UseDurationSeconds ? null :
                        PreviewDuration.PerLevel(buff.DurationValue, blueprint, data, meta, ko);
                    string time = PreviewDuration.Application(buff, buff.Buff, meta, ko, ranked);
                    if (!buff.Permanent && !buff.SameDuration && !buff.UseDurationSeconds && ranked == null &&
                        PreviewDuration.Formula(buff.DurationValue, meta, ko) == null) needsDescription = true;
                    var owner = buff.ToCaster ? KnownCaster() : known;
                    string who = buff.ToCaster ? L("시전자 · ", "caster · ") : recipient;
                    Add(Name(buff.Buff) + L(" · 기본 ", " · base ") + time + Existing(buff.Buff, owner), conditional, who);
                }
                else if (a is ContextActionRemoveBuff remove && remove.Buff != null)
                    result = (remove.ToCaster ? L("시전자 · ", "caster · ") : "") + Name(remove.Buff) + L(" 제거", " removed");
                else if (a is ContextActionHealTarget)
                    result = L("생명력 회복", "healing") + (ReferenceEquals(known, null) ? "" : L(" · 잃은 생명력 ", " · missing HP ") + known.Damage);
                else if (a is ContextActionRemoveBuffsByDescriptor clean)
                    result = L("상태 효과 제거", "remove status effects") + " (" + clean.SpellDescriptor + ")";
                else if (a is ContextActionResurrect) result = L("부활", "resurrection");
                else if (a is ContextActionDispelMagic) result = L("마법 해제", "dispel");
                else if (a is ContextActionCombatManeuver) result = L("전투 책략", "combat maneuver");
                else if (a is ContextActionMeleeAttack melee) result = melee.AutoHit ? L("자동 명중 근접 공격", "automatic melee hit") : L("근접 공격", "melee attack");
                else if (a is ContextActionEnchantWornItem enchant)
                    result = (enchant.ToCaster ? L("시전자 · ", "caster · ") : "") + (enchant.Enchantment?.Name ?? L("장비 강화", "equipment enchantment"))
                        + " · " + Duration(enchant.DurationValue) + (enchant.RemoveOnUnequip ? L(" (장비 해제 시 종료)", " (ends on unequip)") : "");
                else if (a is ContextActionSpawnMonster summon) result = L("소환 · ", "summon · ") + Duration(summon.DurationValue);
                else if (a is ContextActionSpawnAreaEffect area) result = L("지역 효과 · ", "area effect · ") + Duration(area.DurationValue);
                else
                {
                    try { Managed(a, known, conditional, recipient); }
                    catch (Exception ex) { CombatPreview.Report(ex); }
                }
                Add(result, conditional, recipient);
            }
        }

        UnitEntityData KnownCaster()
        {
            var caster = data?.Caster?.Unit;
            return !ReferenceEquals(caster,null) && ActionChecks.Revealed(caster, UnitInfoPart.Defence) ? caster : null;
        }

        static string ActionName(string action, bool ko)
        {
            if (!ko) return action;
            switch (action)
            {
                case "Standard": return "일반 행동";
                case "Move": return "이동 행동";
                case "Swift": return "신속 행동";
                case "Free": return "자유 행동";
                default: return action;
            }
        }

        internal static string Describe(BlueprintAbility original, AbilityData data, UnitEntityData known, bool ko, string checks, bool compact = false)
        {
            var bp = original.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility ?? original;
            var p = new SkillPreview(bp, data, ko);
            foreach (var run in bp.GetComponents<AbilityEffectRunAction>())
                if (!run.IgnoreCaster || ReferenceEquals(known, null) || !ReferenceEquals(known, data?.Caster?.Unit))
                    p.Walk(run.Actions, known, "", false, 0);
            if (bp.GetComponent<AbilityCustomCharge>() != null) p.Add(p.L("돌격", "charge"), false, "");
            if (bp.GetComponent<AbilityCustomDimensionDoor>() != null) p.Add(p.L("순간이동", "teleport"), false, "");
            var apply = bp.GetComponent<AbilityApplyFact>();
            if (apply != null)
            {
                var buff = data?.ParamUnitFact as BlueprintBuff;
                var d = (ContextDurationValue)AccessTools.Field(typeof(AbilityApplyFact), "m_Duration").GetValue(apply);
                var time = apply.HasDuration ? p.Duration(d) : p.L("시간 제한 없음", "no time limit");
                p.Add((buff != null ? p.Name(buff) : p.L("선택한 효과", "selected effect")) + " · " + time + p.Existing(buff, known), false, "");
            }
            string hint = null;
            if (p.needsDescription || p.effects.Count == 0)
            {
                hint = original.LocalizedDuration?.ToString()?.Trim();
                if (string.IsNullOrEmpty(hint) && bp != original) hint = bp.LocalizedDuration?.ToString()?.Trim();
            }
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(checks)) parts.Add(checks);
            if (compact && p.effects.Count > 3)
            {
                parts.AddRange(p.effects.Take(3));
                parts.Add("+" + (p.effects.Count - 3) + p.L("개 효과", " effects"));
            }
            else parts.AddRange(p.effects);
            if (!string.IsNullOrWhiteSpace(hint)) parts.Add(p.L("기술 지속시간 ", "ability duration ") + hint);
            if (parts.Count == 0)
            {
                string action = (data?.RequireFullRoundAction ?? original.IsFullRoundAction) ? p.L("전력 행동", "full-round action")
                    : ActionName((data?.RuntimeActionType ?? original.ActionType).ToString(), ko);
                var targets = new List<string>();
                if (original.CanTargetSelf || original.Range == AbilityRange.Personal) targets.Add(p.L("자신", "self"));
                if (original.CanTargetFriends) targets.Add(p.L("아군", "allies"));
                if (original.CanTargetEnemies) targets.Add(p.L("적", "enemies"));
                if (original.CanTargetPoint) targets.Add(p.L("지점", "point"));
                parts.Add(action);
                if (targets.Count != 0) parts.Add(string.Join("/", targets));
            }
            return string.Join(" · ", parts);
        }

        internal static string Tooltip(BlueprintAbility bp, AbilityData data, bool ko)
        {
            UnitEntityData self = null;
            var caster = data?.Caster?.Unit;
            if (!ReferenceEquals(caster, null) && data.TargetAnchor == AbilityTargetAnchor.Owner && bp.GetComponent<AbilityTargetsAround>() == null
                && ActionChecks.Revealed(caster, UnitInfoPart.Defence))
                self = caster;
            return Describe(bp, data, self, ko, ActionChecks.Summary(bp, ko, self));
        }

        internal static string Selected(AbilityData data, UnitEntityData hover, bool ko)
        {
            var anchor = data.TargetAnchor;
            var around = data.Blueprint.GetComponent<AbilityTargetsAround>();
            if (around != null)
            {
                string side = around.Targets == TargetType.Ally ? (ko ? "아군" : "allies") :
                    around.Targets == TargetType.Enemy ? (ko ? "적" : "enemies") : (ko ? "모든 대상" : "all units");
                string scope = side + (ko ? " · 반경 " : " · radius ") + around.AoERadius.Value + (ko ? "피트" : " ft");
                if (!ReferenceEquals(hover, null) && !hover.Destroyed && (around.Targets == TargetType.Any ||
                    (around.Targets == TargetType.Enemy ? data.Caster.Unit.IsEnemy(hover) : data.Caster.Unit.IsAlly(hover))))
                {
                    bool seen = ActionChecks.Revealed(hover, UnitInfoPart.Defence);
                    return data.Name + " · " + scope + "\n" + hover.CharacterName + (ko ? " (효과 범위에 들 경우) · " : " (if affected) · ")
                        + Describe(data.Blueprint, data, seen ? hover : null, ko, ActionChecks.Defenses(data, hover, ko), true);
                }
                return data.Name + " · " + scope + "\n" + Describe(data.Blueprint, data, null, ko, ActionChecks.Summary(data.Blueprint, ko, null), true);
            }
            var target = anchor == AbilityTargetAnchor.Owner ? data.Caster.Unit : anchor == AbilityTargetAnchor.Unit ? hover : null;
            string title = data.Name;
            if (anchor == AbilityTargetAnchor.Point) title += ko ? " → 지점" : " → point";
            if (ReferenceEquals(target,null) || target.Destroyed)
                return title + "\n" + Describe(data.Blueprint, data, null, ko, ActionChecks.Summary(data.Blueprint, ko, null), true);
            title += " → " + target.CharacterName;
            if (!data.CanTarget(new TargetWrapper(target))) return title + "\n" + (ko ? "이 대상으로 지정할 수 없음" : "invalid target");
            bool revealed = ActionChecks.Revealed(target, UnitInfoPart.Defence);
            string detail = ActionChecks.Defenses(data, target, ko);
            string body = Describe(data.Blueprint, data, revealed ? target : null, ko, detail, true);
            return title + "\n" + (ko ? "대상 지정 가능 · " : "valid target · ") + body;
        }

        internal static string Toggle(BlueprintActivatableAbility bp, ActivatableAbility data, bool ko)
        {
            var parts = new List<string>();
            parts.Add(data == null ? (ko ? "토글" : "toggle") : data.IsOn ? (ko ? "켜짐" : "on") : (ko ? "꺼짐" : "off"));
            if (data?.AppliedBuff != null) parts.Add(PreviewDuration.Remaining(data.AppliedBuff, ko));
            if (bp.DeactivateAfterFirstRound) parts.Add(ko ? "첫 라운드 후 해제" : "deactivates after first round");
            if (bp.DeactivateIfCombatEnded) parts.Add(ko ? "전투 종료 시 해제" : "deactivates after combat");
            var note = ActionChecks.ToggleText(bp, ko);
            if (!string.IsNullOrEmpty(note)) parts.Add(note);
            return string.Join(" · ", parts);
        }
    }
}
