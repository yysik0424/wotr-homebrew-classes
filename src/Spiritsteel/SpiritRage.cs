using System.Collections;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Utility;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using Newtonsoft.Json;
using TurnBased.Controllers;
using UnityEngine;

namespace Spiritsteel
{
    public sealed class SpiritState : UnitPart
    {
        // Keep the saved trip flag and round boundary when loading an existing combat.
        [JsonProperty] public bool Tripped;
        [JsonProperty] public long Boundary;
        internal static readonly long Round = new Rounds(1).Seconds.Ticks;
        internal static long Now => Game.Instance.TimeController.GameTime.Ticks;

        internal void UpdateTripRound(long now)
        {
            if (Boundary == 0) Boundary = now + Round;
            else if (now >= Boundary)
            {
                Boundary += ((now - Boundary) / Round + 1) * Round;
                Tripped = false;
            }
        }
    }

    public sealed class RageGate : ActivatableAbilityRestriction
    {
        internal static bool CanAwaken(UnitEntityData unit) =>
            unit.IsInCombat && unit.Buffs.GetBuff(Blueprints.Native) != null && BoundWeapons.IsEquipped(unit);

        protected override bool IsAvailable() { return CanAwaken(Owner); }
    }
    public sealed class RageKeeper : UnitFactComponentDelegate, IUnitBuffHandler, IUnitCombatHandler, IUnitEquipmentHandler,
        IUnitActiveEquipmentSetHandler, IUnitNewCombatRoundHandler, ITurnBasedModeEnabledHandler
    {
        internal static void Set(UnitEntityData u, bool on)
        {
            var toggle = u.ActivatableAbilities.Enumerable.FirstOrDefault(t => t.Blueprint == Blueprints.Toggle);
            if (toggle == null) return;
            if (on)
            {
                if (!RageGate.CanAwaken(u)) return;
                toggle.IsOn = true;
                toggle.TryStart();
            }
            else toggle.TurnOffImmediately();
        }
        public void HandleBuffDidAdded(Buff b)
        {
            if (b.Owner == Owner && b.Blueprint == Blueprints.Native) Set(Owner, true);
        }
        public void HandleBuffDidRemoved(Buff b)
        {
            if (b.Owner == Owner && b.Blueprint == Blueprints.Native) Set(Owner, false);
        }
        public void HandleUnitJoinCombat(UnitEntityData u)
        {
            if (u != Owner) return;
            if (u.Buffs.HasFact(Blueprints.Native)) Set(u, true);
        }
        public void HandleUnitLeaveCombat(UnitEntityData u)
        {
            if (u != Owner) return;
            Set(u, false);
            var state = u.Get<SpiritState>();
            if (state != null) { state.Tripped = false; state.Boundary = 0; }
        }
        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
        {
            if (slot is HandSlot && slot.Owner?.Unit == Owner && !BoundWeapons.IsEquipped(Owner)) Set(Owner, false);
        }
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor u)
        {
            if (u.Unit == Owner && !BoundWeapons.IsEquipped(Owner)) Set(Owner, false);
        }
        public void HandleNewCombatRound(UnitEntityData u)
        {
            if (u != Owner || !CombatController.IsInTurnBasedCombat()) return;
            var state = u.Get<SpiritState>();
            if (state != null) state.Tripped = false;
        }
        public void UpdateMandatoryTurnBasedModeState() { }
        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            var state = Owner.Get<SpiritState>();
            if (state == null || !Owner.IsInCombat) return;
            if (enabled) state.UpdateTripRound(SpiritState.Now);
            else state.Boundary = SpiritState.Now + SpiritState.Round;
        }
    }

    public sealed class BoundSolo : UnitFactComponentDelegate<BoundSolo.SoloData>, IUnitEquipmentHandler, IUnitActiveEquipmentSetHandler
    {
        public sealed class SoloData { [JsonProperty] public bool Granted; }
        void Set(bool equipped)
        {
            if (Data.Granted == equipped) return;
            if (equipped) Owner.State.Features.SoloTactics.Retain();
            else Owner.State.Features.SoloTactics.Release();
            Data.Granted = equipped;
        }
        protected override void OnTurnOn() { Set(BoundWeapons.IsEquipped(Owner)); }
        protected override void OnTurnOff() { Set(false); }
        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
        {
            if (slot is HandSlot && slot.Owner?.Unit == Owner) Set(BoundWeapons.IsEquipped(Owner));
        }
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor u)
        {
            if (u.Unit == Owner) Set(BoundWeapons.IsEquipped(Owner));
        }
    }
    public sealed class SpiritEffects : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleCalculateCMB>,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        protected override void OnActivate()
        {
            if (!RageGate.CanAwaken(Owner)) { Buff.Remove(); return; }
            if (!CombatController.IsInTurnBasedCombat()) Owner.Ensure<SpiritState>().UpdateTripRound(SpiritState.Now);
            if (Owner.View != null) Owner.View.StartCoroutine(Pulse(Owner, Buff));
        }
        protected override void OnPostLoad()
        {
            base.OnPostLoad();
            // Earlier real-time saves gave the spirit its own expiry date.
            if (!Buff.IsPermanent) Buff.MakePermanent();
        }
        static IEnumerator Pulse(UnitEntityData u, Buff active)
        {
            // Leave a brief gap between native rage and the spirit's visual response.
            yield return new WaitForSeconds(0.22f);
            if (u.View == null || u.Buffs.GetBuff(Blueprints.Spirit) != active) yield break;
            var prefab = Blueprints.RageBurst.Load(false, false);
            if (prefab == null) { WotrClassMod.Common.ModContext.Entry.Logger.Warning("Spirit rage FX failed to load."); yield break; }
            var handle = FxHelper.SpawnFxOnUnit(prefab, u.View, u.IsPlayerFaction, null, Vector3.zero, FxPriority.EventuallyImportant);
            if (handle == null) WotrClassMod.Common.ModContext.Entry.Logger.Warning("Spirit rage burst was not spawned.");
        }
        public void OnEventAboutToTrigger(RuleCalculateCMB evt)
        {
            int wisdom = Owner.Stats.Wisdom.Bonus;
            if (wisdom > 0) evt.AddModifier(wisdom, Fact, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateCMB evt) { }
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.AttackRoll?.IsHit != true || !Owner.Descriptor.HasFact(Blueprints.Trip) || !BoundWeapons.IsBound(Owner, evt.Weapon)) return;
            var s = Owner.Ensure<SpiritState>();
            if (!CombatController.IsInTurnBasedCombat()) s.UpdateTripRound(SpiritState.Now);
            if (s.Tripped) return;
            s.Tripped = true;
            if (evt.Target.State.IsDead || evt.Target.State.Prone.Active) return;
            var trip = new RuleCombatManeuver(Owner, evt.Target, CombatManeuver.Trip, null);
            WeaponFeatTrip.Mark(trip, evt.Weapon);
            Rulebook.Trigger(trip);
        }
    }
    [HarmonyPatch(typeof(RuleCalculateWeaponStats), nameof(RuleCalculateWeaponStats.OnTrigger))]
    static class SpiritWeaponSize
    {
        static void Prefix(RuleCalculateWeaponStats __instance, ref int ___m_SizeShift)
        {
            var u = __instance.Initiator;
            if (___m_SizeShift <= 0 && Blueprints.Spirit != null && u.Buffs.HasFact(Blueprints.Spirit)
                && BoundWeapons.IsBound(u, __instance.Weapon)) ___m_SizeShift++;
        }
    }
}
