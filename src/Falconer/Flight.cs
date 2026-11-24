using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Visual.Animation.Kingmaker;
using Newtonsoft.Json;
using UnityEngine;
using WotrClassMod.Common;

namespace Falconer
{
    public sealed class HawkState : OldStyleUnitPart
    {
        [JsonProperty]
        public bool Spent, PrecisionSpent;
        [JsonProperty]
        public long AvailableAt;
        [NonSerialized]
        internal UnitEntityData Bird;
        [NonSerialized]
        internal HawkFlight View;
    }

    // The bird is a rules actor with natural weapons, never an animal companion or a party member.
    internal static class Flight
    {
        static float next;
        internal static int Level(UnitEntityData owner) => owner.Descriptor.Progression.GetClassLevel(Blueprints.Ranger);
        internal static UnitEntityData Pet(UnitEntityData owner) => owner.GetPet(PetType.AnimalCompanion);
        internal static void Tick(float dt)
        {
            if (Blueprints.Archetype == null || Time.unscaledTime < next)
                return;
            next = Time.unscaledTime + .25f;
            var game = Game.Instance;
            if (game?.Player == null || game.LoadedAreaState?.MainState == null || game.CurrentlyLoadedArea?.HideParty != false)
                return;
            foreach (var owner in game.Player.Party)
                if (owner.IsInGame && owner.View != null && !owner.State.IsDead && owner.Descriptor.HasFact(Blueprints.Hawk))
                    Ensure(owner).SyncSpells();
        }

        internal static HawkFlight Ensure(UnitEntityData owner)
        {
            var state = owner.Ensure<HawkState>();
            if (state.View != null && state.Bird != null && !state.Bird.IsDisposed && !state.Bird.DestroyMark && (state.Bird.HoldingState == null || state.Bird.HoldingState == Game.Instance.LoadedAreaState.MainState))
                return state.View;
            Remove(owner);
            var area = Game.Instance.LoadedAreaState.MainState;
            var bird = Game.Instance.EntityCreator.SpawnUnit(Blueprints.Bird, owner.Position, Quaternion.identity, area, null);
            bird.Ensure<UnitPartTrapActor>();
            try
            {
                bird.Descriptor.EnsureOwnInventory();
                bird.Descriptor.CustomName = Strings.Resolve(ModContext.StringPrefix + "Hawk.name");
                bird.View.ActivateColliders(false);
                bird.View.MovementAgent.enabled = false;
                var view = bird.View.gameObject.AddComponent<HawkFlight>();
                view.Init(owner, bird);
                state.View = view;
                state.Bird = bird;
                return view;
            }
            catch
            {
                bird.MarkForDestroy();
                throw;
            }
        }

        internal static void Remove(UnitEntityData owner)
        {
            var s = owner.Get<HawkState>();
            if (s == null)
                return;
            if (s.Bird != null && !s.Bird.IsDisposed && !s.Bird.DestroyMark)
                s.Bird.MarkForDestroy();
            s.Bird = null;
            s.View = null;
        }
    }

    public sealed class HawkFlight : MonoBehaviour
    {
        internal UnitEntityData Owner, Bird;
        readonly Dictionary<Buff, Buff> mirrors = new Dictionary<Buff, Buff>();
        Vector3 baseScale, launch, impact;
        float elapsed = 1f;
        bool swung;
        internal void Init(UnitEntityData owner, UnitEntityData bird)
        {
            Owner = owner;
            Bird = bird;
            baseScale = transform.localScale;
        }

        internal void DiveAt(UnitEntityData foe)
        {
            launch = Bird.Position;
            impact = foe.Position + Vector3.up;
            elapsed = 0f;
            swung = false;
        }

        void LateUpdate()
        {
            if (Owner == null || Bird == null || Bird.DestroyMark)
                return;
            if (Owner.IsDisposed || !Owner.IsInGame || Owner.View == null || Owner.State.IsDead || !Owner.Descriptor.HasFact(Blueprints.Hawk) || (Bird.HoldingState != null && Bird.HoldingState != Game.Instance?.LoadedAreaState?.MainState))
            {
                Bird.MarkForDestroy();
                return;
            }

            var body = Owner.View.transform;
            var perch = Owner.Position + Vector3.up * 2f - body.right * 1.1f;
            transform.localScale = baseScale * (.42f * Rules.Scale(Flight.Level(Owner)));
            elapsed += Game.Instance.TimeController.DeltaTime;
            Vector3 pos = elapsed < .25f ? Vector3.Lerp(launch, impact, elapsed / .25f) : Vector3.Lerp(impact, perch, Mathf.Clamp01((elapsed - .25f) / .38f));
            if (!swung && elapsed >= .22f && elapsed < .65f)
            {
                swung = true;
                var animator = Bird.View.AnimationManager;
                var swing = animator.CreateSpecialAttackHandle(UnitAnimationSpecialAttackType.Bite);
                if (swing != null)
                    animator.Execute(swing);
            }

            if ((pos - Bird.Position).sqrMagnitude > .0001f)
                Bird.ForceLookAt(pos);
            Bird.Position = pos;
        }

        internal static bool IsBeneficialSpell(Buff b)
        {
            return !b.IsSuppressed && !b.Blueprint.Harmful && CanShareSpell(b.IsFromSpell, b.Context?.SourceAbility);
        }

        internal static bool CanShareSpell(bool fromSpell, BlueprintAbility ability)
        {
            if (!fromSpell && ability?.Type != AbilityType.Spell)
                return false;
            // Some hostile spells leave the resulting buff's Harmful flag unset.
            return ability == null || (ability.EffectOnAlly != AbilityEffectOnUnit.Harmful &&
                (ability.EffectOnEnemy != AbilityEffectOnUnit.Harmful || ability.EffectOnAlly == AbilityEffectOnUnit.Helpful));
        }

        internal void SyncSpells()
        {
            var pet = Flight.Pet(Owner);
            var now = Game.Instance.TimeController.GameTime;
            var live = new HashSet<Buff>();
            if (pet != null)
                foreach (var buff in pet.Buffs.Enumerable)
                {
                    if (!IsBeneficialSpell(buff) || buff.Context?.MaybeCaster == null || Owner.IsEnemy(buff.Context.MaybeCaster))
                        continue;
                    // A copied parent creates its own child effects on the bird.
                    if (buff.SourceFact is Buff parent && parent.Owner == pet.Descriptor && IsBeneficialSpell(parent))
                        continue;
                    live.Add(buff);
                }
            foreach (var old in mirrors.Keys.Where(b => !live.Contains(b)).ToArray())
            {
                var copy = mirrors[old];
                if (copy != null && Bird.Buffs.Enumerable.Contains(copy))
                    copy.Remove();
                mirrors.Remove(old);
            }

            foreach (var source in live)
            {
                if (mirrors.TryGetValue(source, out var active))
                {
                    if (active != null && Bird.Buffs.Enumerable.Contains(active) && !source.IsPermanent && active.EndTime != source.EndTime)
                        active.SetEndTime(source.EndTime);
                    continue;
                }

                var existing = Bird.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint == source.Blueprint &&
                    b.Context?.MaybeCaster == source.Context.MaybeCaster &&
                    b.Context.SourceAbilityContext == source.Context.SourceAbilityContext);
                if (existing != null)
                {
                    mirrors.Add(source, null);
                    continue;
                }

                TimeSpan? left = source.IsPermanent ? (TimeSpan?)null : source.EndTime - now;
                if (left.HasValue && left.Value <= TimeSpan.Zero)
                    continue;
                var copy = Bird.Buffs.AddBuff(source.Blueprint, source.Context, left);
                if (copy != null)
                {
                    copy.IsFromSpell = source.IsFromSpell;
                    copy.IsNotDispelable = source.IsNotDispelable;
                }
                else ModContext.Log("Hawk resisted shared spell effect: " + source.Blueprint.name);
                mirrors.Add(source, copy);
            }
        }
    }
}
