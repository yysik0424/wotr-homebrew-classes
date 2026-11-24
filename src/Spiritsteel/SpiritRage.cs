using System;
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
        [JsonProperty] public bool InBattle, Paid, Tripped, Running;
        [JsonProperty] public int Remaining;
        [JsonProperty] public long Boundary;
        internal static readonly long Round = new Rounds(1).Seconds.Ticks;
        internal static long Now => Game.Instance.TimeController.GameTime.Ticks;

        internal void Begin(int wisdom)
        {
            if (InBattle) return;
            InBattle = true; Remaining = Rules.Rounds(wisdom);
            Paid = Running = Tripped = false; Boundary = 0;
        }
        internal bool Pay()
        {
            if (Paid) return true;
            if (Remaining <= 0) return false;
            Remaining--; Paid = true;
            return true;
        }
        internal void RealTime(long now)
        {
            if (!Paid || now < Boundary) return;
            int elapsed = checked((int)((now - Boundary) / Round + 1));
            Paid = false; Tripped = false;
            if (Running && Remaining >= elapsed)
            {
                Remaining -= elapsed; Boundary += elapsed * Round; Paid = true;
            }
            else if (Running) Remaining = 0;
        }
        internal bool Available(UnitEntityData u)
        {
            if (!u.IsInCombat || !u.Buffs.HasFact(Blueprints.Native) || !BoundWeapons.IsEquipped(u)) return false;
            Begin(u.Stats.Wisdom.Bonus);
            if (!CombatController.IsInTurnBasedCombat()) RealTime(Now);
            return Paid || Remaining > 0;
        }
    }

    public sealed class RageGate : ActivatableAbilityRestriction
    {
        protected override bool IsAvailable() { return Owner.Ensure<SpiritState>().Available(Owner); }
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
                if (!u.Ensure<SpiritState>().Available(u)) return;
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
            u.Ensure<SpiritState>().Begin(u.Stats.Wisdom.Bonus);
            if (u.Buffs.HasFact(Blueprints.Native)) Set(u, true);
        }
        public void HandleUnitLeaveCombat(UnitEntityData u)
        {
            if (u != Owner) return;
            Set(u, false);
            u.Ensure<SpiritState>().InBattle = false;
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
            var state = u.Ensure<SpiritState>();
            state.Paid = state.Tripped = false;
            if (state.Running && !state.Pay()) Set(u, false);
        }
        public void UpdateMandatoryTurnBasedModeState() { }
        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            var state = Owner.Get<SpiritState>();
            if (state == null || !state.InBattle) return;
            if (enabled) state.RealTime(SpiritState.Now);
            else if (state.Paid) state.Boundary = SpiritState.Now + SpiritState.Round;
            var buff = Owner.Buffs.GetBuff(Blueprints.Spirit);
            if (buff == null) return;
            if (!state.Paid && !state.Pay()) { Set(Owner, false); return; }
            SpiritEffects.SetClock(buff, state, enabled);
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
        internal static void SetClock(Buff b, SpiritState state, bool turns)
        {
            if (turns) b.MakePermanent();
            else b.SetDuration(TimeSpan.FromTicks(Math.Max(0, state.Boundary - SpiritState.Now) + state.Remaining * SpiritState.Round));
        }
        protected override void OnActivate()
        {
            var s = Owner.Ensure<SpiritState>();
            bool turns = CombatController.IsInTurnBasedCombat();
            if (!s.Available(Owner)) { Buff.Remove(); return; }
            if (!s.Paid) s.Boundary = SpiritState.Now + SpiritState.Round;
            s.Pay(); s.Running = true;
            SetClock(Buff, s, turns);
            if (Owner.View != null) Owner.View.StartCoroutine(Pulse(Owner, Buff));
        }
        protected override void OnDeactivate()
        {
            var s = Owner.Ensure<SpiritState>();
            if (!CombatController.IsInTurnBasedCombat()) s.RealTime(SpiritState.Now);
            s.Running = false;
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
            if (!CombatController.IsInTurnBasedCombat()) s.RealTime(SpiritState.Now);
            if (!s.Paid || s.Tripped) return;
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
