using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.UnitLogic.ActivatableAbilities;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.Inspect;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;

namespace HomebrewClasses.Loader
{
    internal sealed class ActionChecks
    {
        internal AttackType? Attack;
        internal bool SpellResistance;
        internal readonly List<SavingThrowType> Saves = new List<SavingThrowType>();
        internal readonly List<DamageEnergyType> Energy = new List<DamageEnergyType>();
        static readonly Func<ContextActionDealDamage, bool> SimpleDamage = (Func<ContextActionDealDamage, bool>)Delegate.CreateDelegate(typeof(Func<ContextActionDealDamage, bool>), AccessTools.PropertyGetter(typeof(ContextActionDealDamage), "IsSimpleDamage"));
        // Optional component description for abilities that resolve their checks in managed code.
        static readonly Dictionary<Type, MethodInfo> providers = new Dictionary<Type, MethodInfo>();
        static string ComponentText(BlueprintScriptableObject bp, bool korean)
        {
            if (bp?.ComponentsArray == null) return null;
            foreach (var component in bp.ComponentsArray)
            {
                if (component == null) continue;
                var type = component.GetType();
                if (!providers.TryGetValue(type, out var provider))
                {
                    provider = type.GetMethod("GetCombatPreviewText", new[] { typeof(bool) });
                    if (provider != null && provider.ReturnType != typeof(string)) provider = null;
                    providers[type] = provider;
                }
                if (provider != null) return (string)provider.Invoke(component, new object[] { korean });
            }
            return null;
        }
        internal static string ToggleText(BlueprintActivatableAbility toggle, bool korean)
        {
            var own = ComponentText(toggle.Buff, korean);
            if (own != null) return own;
            var variants = toggle.GetComponent<ActivatableAbilityVariants>();
            if (variants == null) return null;
            var refs = (BlueprintActivatableAbilityReference[])AccessTools.Field(typeof(ActivatableAbilityVariants), "m_Variants").GetValue(variants);
            if (refs == null || refs.Length == 0) return null;
            string first = ComponentText(refs[0]?.Get()?.Buff, korean);
            return first != null && refs.Skip(1).All(r => ComponentText(r?.Get()?.Buff, korean) == first) ? first : null;
        }

        internal static bool Korean(BlueprintUnitFact fact)
        {
            return LocalizationManager.CurrentLocale.ToString() == "koKR" || (fact != null && Hangul(fact.Name));
        }
        internal static bool Hangul(string value) { return value != null && value.Any(c => c >= '가' && c <= '힣'); }
        internal static string SaveName(SavingThrowType save, bool ko)
        {
            if (!ko) return save.ToString();
            return save == SavingThrowType.Fortitude ? "인내" : save == SavingThrowType.Reflex ? "반사" : "의지";
        }
        internal static string AttackName(AttackType attack, bool ko)
        {
            if (attack == AttackType.RangedTouch) return ko ? "원거리 접촉" : "ranged touch";
            if (attack == AttackType.Touch) return ko ? "근접 접촉" : "melee touch";
            return ko ? "무기 공격" : "weapon attack";
        }
        static string ElementName(DamageEnergyType e, bool ko)
        {
            if (!ko) return e.ToString();
            switch (e)
            {
                case DamageEnergyType.Fire: return "화염";
                case DamageEnergyType.Cold: return "냉기";
                case DamageEnergyType.Electricity: return "전기";
                case DamageEnergyType.Acid: return "산성";
                case DamageEnergyType.Sonic: return "음파";
                case DamageEnergyType.PositiveEnergy: return "양에너지";
                case DamageEnergyType.NegativeEnergy: return "음에너지";
                case DamageEnergyType.Divine: return "신성";
                case DamageEnergyType.Holy: return "성스러운 피해";
                case DamageEnergyType.Unholy: return "부정한 피해";
                default: return e.ToString();
            }
        }

        internal static ActionChecks Read(BlueprintAbility bp)
        {
            var checks = new ActionChecks { SpellResistance = bp.SpellResistance };
            var delivery = bp.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility;
            if (delivery != null) { bp = delivery; checks.Attack = AttackType.Touch; checks.SpellResistance |= bp.SpellResistance; }
            if (bp.GetComponent<AbilityDeliverTouch>() != null) checks.Attack = AttackType.Touch;
            var projectile = bp.GetComponent<AbilityDeliverProjectile>();
            if (projectile != null && projectile.NeedAttackRoll && projectile.Weapon != null) checks.Attack = projectile.Weapon.AttackType;
            var run = bp.GetComponent<AbilityEffectRunAction>();
            if (run != null) { checks.Save(run.SavingThrowType); checks.Scan(run.Actions, 0); }
            return checks;
        }

        void Save(SavingThrowType s) { if (s != SavingThrowType.Unknown && !Saves.Contains(s)) Saves.Add(s); }
        void Scan(ActionList list, int depth)
        {
            if (list?.Actions == null || depth > 10) return;
            foreach (var action in list.Actions)
            {
                if (action is ContextActionDealDamage damage && SimpleDamage(damage) && damage.DamageType.Type == Kingmaker.RuleSystem.Rules.Damage.DamageType.Energy && !Energy.Contains(damage.DamageType.Energy))
                    Energy.Add(damage.DamageType.Energy);
                else if (action is ContextActionSavingThrow save) { Save(save.Type); Scan(save.Actions, depth + 1); }
                else if (action is ContextActionConditionalSaved saved) { Scan(saved.Succeed, depth + 1); Scan(saved.Failed, depth + 1); }
                else if (action is Conditional condition) { Scan(condition.IfTrue, depth + 1); Scan(condition.IfFalse, depth + 1); }
            }
        }

        internal static string AbilityText(BlueprintAbility bp, bool ko)
        {
            string supplied = ComponentText(bp, ko);
            if (supplied != null) return supplied;
            var checks = Read(bp);
            var parts = new List<string>();
            if (checks.Attack.HasValue) parts.Add(AttackName(checks.Attack.Value, ko));
            if (checks.SpellResistance) parts.Add(ko ? "주문 저항 판정" : "spell resistance check");
            parts.AddRange(checks.Saves.Select(s => SaveName(s, ko) + (ko ? " 내성" : " save")));
            parts.AddRange(checks.Energy.Select(e => ElementName(e, ko) + (ko ? " 피해" : " damage")));
            return string.Join(" · ", parts);
        }

        internal static bool Revealed(UnitEntityData unit, UnitInfoPart part)
        {
            return unit.IsPlayerFaction || Game.Instance.Player.InspectUnitsManager.GetInfo(unit)?.IsUnlocked(part) == true;
        }
        internal static string Defenses(AbilityData ability, UnitEntityData target, bool ko)
        {
            var bp = ability.Blueprint;
            var c = Read(bp);
            if (!Revealed(target, UnitInfoPart.Defence))
            {
                var text = AbilityText(bp, ko);
                return text.Length == 0 ? "" : text + (ko ? " · 대상 방어 미확인" : " · target defenses unknown");
            }
            var parts = new List<string>();
            if (c.Attack.HasValue)
            {
                var a = c.Attack.Value;
                var estimate = HitEstimate.Touch(ability, target, a, ko);
                if (estimate != null) parts.Add(estimate);
                bool touch = a == AttackType.Touch || a == AttackType.RangedTouch;
                parts.Add(AttackName(a, ko) + (touch ? (ko ? " · 접촉 AC " : " · touch AC ") : " · AC ") + (touch ? target.Stats.AC.Touch : target.Stats.AC.ModifiedValue));
            }
            if (c.SpellResistance)
            {
                var sr = target.Get<UnitPartSpellResistance>();
                var value = sr == null ? 0 : sr.GetValue(bp, ability.Caster.Unit);
                if (sr != null && sr.IsImmune(bp, ability.Caster.Unit)) parts.Add(ko ? "주문 면역" : "spell immunity");
                parts.Add((ko ? "주문 저항 " : "SR ") + value);
            }
            foreach (var save in c.Saves)
            {
                var stat = save == SavingThrowType.Fortitude ? target.Stats.SaveFortitude : save == SavingThrowType.Reflex ? target.Stats.SaveReflex : target.Stats.SaveWill;
                parts.Add(SaveName(save, ko) + " " + stat.ModifiedValue.ToString("+0;-0;0"));
            }
            var dr = c.Energy.Count == 0 ? null : target.Get<UnitPartDamageReduction>();
            foreach (var energy in c.Energy)
            {
                string element = ElementName(energy, ko);
                bool immune = dr != null && dr.IsImmune(energy);
                var values = dr?.AllSources.Where(x => x.SourceBlueprintComponent is AddDamageResistanceEnergy resist && resist.Type == energy)
                    .Select(x => x.GetCurrentValue()).Where(x => x > 0).Distinct().OrderByDescending(x => x).ToArray();
                if (immune) parts.Add(element + (ko ? " 면역" : " immunity"));
                else if (values != null && values.Length != 0) parts.Add(element + (ko ? " 저항 " : " resistance ") + string.Join("/", values));
                else parts.Add(element + (ko ? " 피해" : " damage"));
                if ((immune || (values != null && values.Length != 0)) && ability.Caster.Unit.Facts.List.Any(f => f.Active &&
                    f.Blueprint.GetComponents<Kingmaker.Designers.Mechanics.Facts.AscendantElement>().Any(e => e.Element == energy)))
                    parts.Add(ko ? "초월 원소로 관통" : "bypassed by Ascendant Element");
            }
            return string.Join(" · ", parts);
        }
    }
}
