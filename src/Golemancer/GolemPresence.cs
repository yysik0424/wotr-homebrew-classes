using System;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    public static class GolemPresence
    {
        public static void Grant(BlueprintFeature feature)
        {
            Make(true, Guids.CallGolem);
            Make(false, Guids.DismissGolem);
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.CallGolem), Ref<BlueprintUnitFactReference>(Guids.DismissGolem) });
            AddComponents(feature, grant);
        }

        static void Make(bool call, string guid)
        {
            var ability = Create<BlueprintAbility>(call ? "GolemancerCallGolem" : "GolemancerDismissGolem", guid, a =>
            {
                SetField(a, "m_DisplayName", Strings.L(call ? "GolemCall.Name" : "GolemDismiss.Name"));
                SetField(a, "m_Description", Strings.L(call ? "GolemCall.Description" : "GolemDismiss.Description"));
                SetField(a, "m_DescriptionShort", NoText());
                a.LocalizedDuration = NoText();
                a.LocalizedSavingThrow = NoText();
                a.Type = AbilityType.Extraordinary;
                a.ActionType = UnitCommand.CommandType.Standard;
                a.Range = AbilityRange.Personal;
                a.CanTargetSelf = true;
                a.CanTargetFriends = a.CanTargetEnemies = a.CanTargetPoint = false;
                a.SpellResistance = false;
            });
            AddComponents(ability, new GolemPresenceRestriction { Call = call },
                new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { new ContextActionGolemPresence { Call = call } } } });
        }

        static bool Mounted(UnitEntityData pet)
        {
            return pet.SaddledPart != null && pet.SaddledPart.Rider != null;
        }

        static bool FusionBusy(UnitEntityData master)
        {
            var fusion = master.Get<UnitPartGolemFusion>();
            return fusion != null && (fusion.HiddenByUs || fusion.CurState != FusionState.Idle);
        }

        static bool InCombat(UnitEntityData master, UnitEntityData pet)
        {
            return Game.Instance.Player.IsInCombat || master.IsInCombat || pet.IsInCombat;
        }

        static bool Overdriven(UnitEntityData master)
        {
            return Overdrive.Buff != null && master.Descriptor.HasFact(Overdrive.Buff);
        }

        static UnitEntityData Bound(UnitEntityData master, UnitPartGolemMaster part)
        {
            if (part == null || part.GolemId == null || part.Life.State == LifecycleState.Conflict || part.Life.State == LifecycleState.Detached) return null;
            var pets = master.Pets;
            for (int i = 0; i < pets.Count; i++)
            {
                var reference = pets[i];
                var pet = reference.Entity;
                if ((object)pet != null && pet.UniqueId == part.GolemId && !pet.Destroyed && !pet.DestroyMark
                    && ReferenceEquals(pet.Master, master) && ReferenceEquals(pet.Blueprint, GolemUnit.Unit)
                    && reference.EntityPart != null && reference.EntityPart.Type == PetType.AnimalCompanion) return pet;
            }
            return null;
        }

        // AddPet and owner/area transitions may set IsInGame again. Only reverse that change for the saved, stowed pet.
        // The separate UnitPartPet.ShouldBeHidden flag belongs to the game's story and fusion systems.
        internal static void Reconcile(UnitEntityData master, UnitPartGolemMaster part)
        {
            if (part == null || part.Called == true || part.Life.InFlight != null || Fusion.IsPreview(master)) return;
            var pet = Bound(master, part);
            if ((object)pet == null) return;
            if (!part.Called.HasValue)
            {
                var game = Game.Instance;
                if (game == null || game.Player == null || game.CurrentlyLoadedArea == null || master.HoldingState == null
                    || (LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess)) return;
                part.Called = GolemPresenceRules.KeepLegacyPet(InCombat(master, pet), Mounted(pet), FusionBusy(master), Overdriven(master), part.Broken || pet.State.IsDead);
            }
            if (part.Called == false && pet.IsInGame && !Mounted(pet) && !FusionBusy(master))
                pet.IsInGame = false;
        }

        static UnitEntityData Available(UnitEntityData master, bool call)
        {
            if ((object)master == null || master.Destroyed || master.DestroyMark || !master.IsInGame || !master.IsPlayerFaction || master.State.IsDead || Fusion.IsPreview(master)) return null;
            var part = master.Get<UnitPartGolemMaster>();
            if (part == null || !part.Called.HasValue || part.Called.Value == call || part.Broken || part.Life.InFlight != null) return null;
            var state = part.Life.State;
            if (state != LifecycleState.Ready && !(call && (state == LifecycleState.BoundPending || state == LifecycleState.Stale))) return null;
            var game = Game.Instance;
            if (game == null || game.Player == null || game.CurrentlyLoadedArea == null || game.CurrentlyLoadedArea.HideParty
                || game.CutsceneLock == null || game.CutsceneLock.Active || (LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess)) return null;
            var pet = Bound(master, part);
            if ((object)pet == null || pet.IsInGame == call || pet.State.IsDead || pet.HoldingState == null || !ReferenceEquals(pet.HoldingState, master.HoldingState)) return null;
            var companion = pet.Get<UnitPartPet>();
            if (companion == null || companion.ShouldBeHidden || Mounted(pet) || FusionBusy(master) || Overdriven(master)) return null;
            return GolemPresenceRules.AllowsCombat(call, InCombat(master, pet)) ? pet : null;
        }

        public static bool CanUse(UnitEntityData master, bool call) { return (object)Available(master, call) != null; }

        internal static bool IsPresentForAbility(UnitEntityData master, UnitPartGolemMaster part, UnitEntityData pet)
        {
            return (object)pet != null && part.Called != false && (pet.IsInGame || FusionBusy(master));
        }

        public static bool TryUse(UnitEntityData master, bool call)
        {
            var pet = Available(master, call);
            if ((object)pet == null) return false;
            var part = master.Get<UnitPartGolemMaster>();
            if (call)
            {
                FreePlaceSelector.PlaceSpawnPlaces(1, Math.Max(0.5f, pet.Corpulence), master.Position);
                pet.Position = FreePlaceSelector.GetRelaxedPosition(0, true);
            }
            // IsInGame publishes its event synchronously, so publish intent before changing the entity.
            part.Called = call;
            try { pet.IsInGame = call; }
            finally { part.Called = pet.IsInGame; }
            if (call && pet.IsInGame)
            {
                var before = part.Life.State;
                GolemMaster.Feed(master, LifeEventKind.Poll);
                if (before == LifecycleState.Ready && part.Life.State == LifecycleState.Stale)
                    GolemMaster.Feed(master, LifeEventKind.Poll);
                if (part.Life.State == LifecycleState.Ready) Taunt.Reconcile(master, pet, TauntSource.Golem);
            }
            return part.Called == call;
        }
    }

    public sealed class GolemPresenceRestriction : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool Call;
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return GolemPresence.CanUse(caster, Call); }
        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + (Call ? "GolemCall.Unavailable" : "GolemDismiss.Unavailable")); }
    }

    public sealed class ContextActionGolemPresence : ContextAction
    {
        public bool Call;
        public override string GetCaption() { return Call ? "Call the bound golem" : "Dismiss the bound golem"; }
        public override void RunAction() { GolemPresence.TryUse(Context.MaybeCaster, Call); }
    }
}
