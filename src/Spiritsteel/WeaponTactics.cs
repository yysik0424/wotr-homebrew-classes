using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using TurnBased.Controllers;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spiritsteel
{
    internal static class WeaponTactics
    {
        internal static BlueprintBuff VigilUsed, DisarmUsed, LungeStance, LungeCost, CleaveUsed, CleaveReady;
        internal static BlueprintBuff NativeLunge, NativeDisrupt;
        internal static BlueprintFeature LungeGrant, CleaveGrant;

        internal static void Build()
        {
            NativeLunge = Get<BlueprintBuff>("589c977574ea8dd48bc26b5020533e82");
            NativeDisrupt = Get<BlueprintBuff>("00c4eb8ffaf4b7d47bea8c8307704f3c");
            VigilUsed = BuffFor(WeaponFeatKind.Vigil, "spent", new WeaponTacticWindow());
            DisarmUsed = BuffFor(WeaponFeatKind.DisarmCritical, "spent", new WeaponTacticWindow());
            LungeCost = BuffFor(WeaponFeatKind.Lunge, "defense", new WeaponTacticWindow { LungeCost = true }, new LungeDefense());
            LungeStance = BuffFor(WeaponFeatKind.Lunge, "stance", new LungePosture());
            CleaveUsed = BuffFor(WeaponFeatKind.Spellcleave, "spent", new WeaponTacticWindow { WholeCombat = true });
            CleaveReady = BuffFor(WeaponFeatKind.Spellcleave, "ready", new WeaponTacticWindow { WholeCombat = true }, new SpellcleaveHit());

            var lunge = Make<BlueprintActivatableAbility>(WeaponFeatKind.Lunge, "toggle", a => {
                SetField(a, "m_Buff", Ref<BlueprintBuffReference>(LungeStance.AssetGuid.ToString()));
                a.ActivationType = AbilityActivationType.Immediately;
                a.OnlyInCombat = true; a.DeactivateIfCombatEnded = true; a.DeactivateIfOwnerUnconscious = true;
            });
            var declare = Make<BlueprintAbility>(WeaponFeatKind.Spellcleave, "declare", a => {
                a.Type = AbilityType.Extraordinary; a.Range = AbilityRange.Personal; a.CanTargetSelf = true;
                a.ActionType = UnitCommand.CommandType.Free;
                a.CanTargetEnemies = false; a.CanTargetFriends = false; a.CanTargetPoint = false;
                a.LocalizedDuration = NoText(); a.LocalizedSavingThrow = NoText();
            });
            AddComponents(declare, new DeclareSpellcleave());
            LungeGrant = Grant(WeaponFeatKind.Lunge, lunge);
            CleaveGrant = Grant(WeaponFeatKind.Spellcleave, declare);
        }

        static T Make<T>(WeaponFeatKind kind, string suffix, Action<T> init) where T : BlueprintUnitFact, new()
        {
            var row = WeaponFeatRow.All.Single(r => r.Kind == kind);
            return Create<T>("Spiritsteel" + kind + "_" + suffix, WeaponFeats.StableId(kind + "." + suffix), bp => {
                WeaponFeats.Name(bp, row.Key("name"), row.Key("desc"), Get<BlueprintUnitFact>(row.IconSource));
                init(bp);
            });
        }
        static BlueprintBuff BuffFor(WeaponFeatKind kind, string suffix, params BlueprintComponent[] components)
        {
            var buff = Make<BlueprintBuff>(kind, suffix, b => {
                b.FxOnStart = new PrefabLink(); b.FxOnRemove = new PrefabLink(); b.Stacking = StackingType.Replace;
                if (suffix == "spent") SetField(b, "m_Flags", Enum.Parse(FindField(typeof(BlueprintBuff), "m_Flags").FieldType, "HiddenInUi"));
            });
            AddComponents(buff, components);
            return buff;
        }
        static BlueprintFeature Grant(WeaponFeatKind kind, BlueprintUnitFact action)
        {
            var feature = Make<BlueprintFeature>(kind, "carrier", f => { f.HideInUI = true; f.Ranks = 1; });
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(action.AssetGuid.ToString()) });
            AddComponents(feature, grant);
            return feature;
        }

        internal static void MarkRound(UnitEntityData unit, BlueprintBuff buff)
        {
            TimeSpan? duration = CombatController.IsInTurnBasedCombat() ? (TimeSpan?)null : new Rounds(1).Seconds;
            unit.Buffs.AddBuff(buff, unit, duration);
        }
        internal static bool Wields(ItemEntityWeapon weapon, UnitEntityData unit, WeaponFeatKind kind)
        {
            if (weapon == null || unit == null) return false;
            if (WeaponFeats.Inscribed(weapon, kind)) return true;
            var primary = unit.Body.PrimaryHand.MaybeWeapon;
            return primary != null && ReferenceEquals(primary.Second, weapon) && WeaponFeats.Inscribed(primary, kind);
        }
    }

    public sealed class WeaponTacticWindow : UnitBuffComponentDelegate, ITurnBasedModeHandler, ITurnBasedModeEnabledHandler, IUnitCombatHandler
    {
        public bool WholeCombat, LungeCost;
        bool Keep => WholeCombat || (LungeCost && Owner.Buffs.HasFact(WeaponTactics.LungeStance));
        public void HandleTurnStarted(UnitEntityData unit) { if (unit == Owner && !Keep) Buff.Remove(); }
        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            if (Keep) return;
            if (enabled) Buff.MakePermanent(); else Buff.SetDuration(new Rounds(1).Seconds);
        }
        public void HandleUnitLeaveCombat(UnitEntityData unit) { if (unit == Owner) Buff.Remove(); }
        public void HandleUnitJoinCombat(UnitEntityData unit) { }
        public void HandleRoundStarted(int round) { }
        public void HandleSurpriseRoundStarted() { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck roll) { }
        public void UpdateMandatoryTurnBasedModeState() { }
    }

    public sealed class LungePosture : UnitBuffComponentDelegate
    {
        protected override void OnTurnOn()
        {
            var cost = Owner.Buffs.GetBuff(WeaponTactics.LungeCost);
            if (cost == null) Owner.Buffs.AddBuff(WeaponTactics.LungeCost, Owner, null);
            else cost.MakePermanent();
        }
        protected override void OnTurnOff()
        {
            var cost = Owner.Buffs.GetBuff(WeaponTactics.LungeCost);
            if (cost == null) return;
            if (!Owner.IsInCombat) cost.Remove();
            else if (!CombatController.IsInTurnBasedCombat()) cost.SetDuration(new Rounds(1).Seconds);
        }
    }

    public sealed class LungeDefense : UnitBuffComponentDelegate, IUnitBuffHandler
    {
        void Set(bool apply)
        {
            Owner.Stats.AC.RemoveModifiersFrom(Runtime);
            if (apply) Owner.Stats.AC.AddModifier(-2, Runtime, ModifierDescriptor.Feat);
        }
        protected override void OnTurnOn() { Set(!Owner.Buffs.HasFact(WeaponTactics.NativeLunge)); }
        protected override void OnTurnOff() { Set(false); }
        public void HandleBuffDidAdded(Buff buff) { if (buff.Owner == Owner && buff.Blueprint == WeaponTactics.NativeLunge) Set(false); }
        public void HandleBuffDidRemoved(Buff buff) { if (buff.Owner == Owner && buff.Blueprint == WeaponTactics.NativeLunge) Set(true); }
    }

    public sealed class DeclareSpellcleave : AbilityApplyEffect, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster) =>
            caster.IsInCombat && !caster.Buffs.HasFact(WeaponTactics.CleaveUsed);
        public string GetAbilityCasterRestrictionUIText() => Strings.L("weaponfeat.Spellcleave.spent").ToString();
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.Caster;
            if (!IsCasterRestrictionPassed(caster)) return;
            caster.Buffs.AddBuff(WeaponTactics.CleaveUsed, caster, null);
            caster.Buffs.AddBuff(WeaponTactics.CleaveReady, caster, null);
        }
    }

    public sealed class SpellcleaveHit : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        internal static Buff Pick(UnitEntityData target)
        {
            return target.Buffs.Enumerable.Where(b => b.IsFromSpell && !b.IsNotDispelable && !b.HiddenInInspector &&
                !b.Blueprint.Harmful && b.Context?.Params != null).OrderByDescending(b => b.Context.Params.CasterLevel).FirstOrDefault();
        }
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.AttackRoll?.IsHit != true || !WeaponTactics.Wields(evt.Weapon, Owner, WeaponFeatKind.Spellcleave)) return;
            var caster = Owner;
            Buff.Remove();
            if (!caster.IsEnemy(evt.Target)) return;
            var targetBuff = Pick(evt.Target);
            if (targetBuff == null) return;
            var context = new MechanicsContext(caster, evt.Target, WeaponTactics.CleaveReady, null, evt.Target);
            context.SetParams(new AbilityParams { CasterLevel = caster.Progression.GetClassLevel(WeaponFeats.Barbarian) });
            context.TriggerRule(new RuleDispelMagic(caster, evt.Target, targetBuff, RuleDispelMagic.CheckType.CasterLevel, StatType.Unknown, null));
        }
    }
}
