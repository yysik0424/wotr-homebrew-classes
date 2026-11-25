using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Newtonsoft.Json;
using TurnBased.Controllers;
using WotrClassMod.Common;
using ModContext = WotrClassMod.Common.ModContext;

namespace SorrowInquisitor
{
    public sealed class SorrowPart : UnitPart
    {
        [JsonProperty] public List<Bead> Beads = new List<Bead>();
        [JsonProperty] public int Spent;
        [JsonProperty] public string[] Armed = Array.Empty<string>();
        [JsonProperty] public long ArmedUntil, NextRelease;
        [JsonProperty] public bool UsedTurn;
        [JsonProperty] public string WardId;
        [JsonProperty] public UnitReference WardTarget;

        [JsonIgnore] internal bool Gathering;

        internal void Prune(long now, bool turns)
        {
            if (!turns && now >= ArmedUntil) Armed = Array.Empty<string>();
        }
        protected override void OnPostLoad()
        {
            base.OnPostLoad();
            foreach (var bead in Beads) BeadStatus.RestoreDisplay(Owner,bead);
        }
        internal void ClearCombat()
        {
            Armed = Array.Empty<string>(); NextRelease = 0; UsedTurn = false;
        }
        internal void ClearWard()
        {
            if (WardId == null) return;
            var old = WardTarget.Value?.Buffs.Enumerable.FirstOrDefault(b => b.UniqueId == WardId);
            WardId = null; WardTarget = default(UnitReference); old?.Remove();
        }
    }

    internal static class Rosary
    {
        internal static string Text(string key) => Strings.Resolve(ModContext.StringPrefix + key);
        internal static void Log(string key, params object[] args)
        {
            var line = string.Format(Text(key), args);
            EventBus.RaiseEvent<ILogMessageUIHandler>(h => h.HandleLogMessage(line));
        }
        internal static int Wisdom(UnitEntityData u) => u.Stats.Wisdom.Bonus;
        internal static int Level(UnitEntityData u) => u.Progression.GetClassLevel(Blueprints.Inquisitor);
        internal static bool Party(UnitEntityData u) => Game.Instance.Player.PartyAndPets.Contains(u);
        internal static bool Ally(UnitEntityData u, UnitEntityData t) =>
            t != null && t != u && !t.State.IsDead && (u.IsAlly(t) || (Party(u) && Party(t)));
        internal static bool Enemy(UnitEntityData u, UnitEntityData t) =>
            t != null && !t.State.IsDead && u.IsEnemy(t) && !(Party(u) && Party(t));
        internal static bool Bane(UnitEntityData u) => u.Buffs.HasFact(BlueprintUtil.Get<BlueprintBuff>(Guids.Bane)) ||
            u.Buffs.HasFact(BlueprintUtil.Get<BlueprintBuff>(Guids.GreaterBane));
        internal static SorrowPart Part(UnitEntityData u)
        {
            var p = u.Ensure<SorrowPart>();
            p.Prune(Afflictions.Now, CombatController.IsInTurnBasedCombat());
            return p;
        }
        internal static int Uses(UnitEntityData u, SorrowPart p) => Math.Max(0, Rules.Daily(Wisdom(u)) - p.Spent);
        internal static bool Ready(SorrowPart p) => CombatController.IsInTurnBasedCombat() ? !p.UsedTurn : Afflictions.Now >= p.NextRelease;
        internal static int Capacity(UnitEntityData u, SorrowPart p, Rite kind)
        {
            int level = Level(u);
            if (kind == Rite.Gather || kind == Rite.Vow)
                return Uses(u,p) == 0 ? 0 : Math.Max(0, Rules.Slots(level) - p.Beads.Count);
            if (kind == Rite.Standard && level >= 6 || kind == Rite.Move && level < 6 || kind == Rite.Swift && level < 11) return 0;
            if (kind != Rite.Discard && (!Ready(p) || kind == Rite.Blade && !Bane(u))) return 0;
            return Math.Min(p.Beads.Count, Rules.Limit(level, kind));
        }
        internal static int Gather(UnitEntityData u, UnitEntityData ally, IEnumerable<string> ids)
        {
            if (!Ally(u,ally)) return 0;
            var p = Part(u);
            if (p.Gathering) return 0;
            int left = Capacity(u,p,Rite.Gather), count = 0;
            p.Gathering = true;
            try
            {
                foreach (var id in ids.Distinct().Take(left))
                {
                    var buff = ally.Buffs.Enumerable.FirstOrDefault(b => b.UniqueId == id);
                    if (!Afflictions.Eligible(buff)) continue;
                    var bead = Afflictions.Capture(buff);
                    if (!Afflictions.ReleaseAlly(buff)) continue;
                    // Pay as soon as the first removal commits, including during a vow.
                    if (count++ == 0) p.Spent++;
                    p.Beads.Add(bead);
                    BeadStatus.Show(u,bead);
                    ReportGather(u,ally,bead);
                }
            }
            finally { p.Gathering = false; }
            return count;
        }
        static void ReportGather(UnitEntityData u, UnitEntityData ally, Bead bead) =>
            Log("log.gather",u.CharacterName,ally.CharacterName,bead.Blueprint.Name);

        static void Report(UnitEntityData foe, Bead bead, SentenceOutcome result)
        {
            var key = result == SentenceOutcome.Applied ? "log.applied" : result == SentenceOutcome.Saved ? "log.saved" : "log.immune";
            Log(key,foe.CharacterName,bead.Blueprint.Name);
        }
        internal static EnergyDamage DivinePacket(int level, int applied)
        {
            int dice = Rules.Dice(level, applied);
            return dice > 0 ? new EnergyDamage(new DiceFormula(dice, DiceType.D10), DamageEnergyType.Divine) : null;
        }
        internal static void Release(UnitEntityData u, UnitEntityData foe, Rite kind, string[] ids, MechanicsContext context)
        {
            if (!Enemy(u,foe)) return;
            var p = Part(u);
            var selected = ids.Distinct().Select(id => p.Beads.FirstOrDefault(b => b.Id == id))
                .Where(b => b != null).Take(Capacity(u,p,kind)).ToArray();
            if (selected.Length == 0) return;
            p.Armed = Array.Empty<string>();
            p.UsedTurn = true; p.NextRelease = Afflictions.Now + TimeSpan.FromSeconds(Rules.RoundSeconds).Ticks;
            int applied = 0, level = Level(u);
            foreach (var bead in selected)
            {
                if (foe.State.IsDead || !p.Beads.Remove(bead)) break;
                BeadStatus.Remove(u,bead);
                int dc = Rules.DC(level, Wisdom(u), bead.Aggressor == foe.UniqueId);
                var result = Afflictions.Inflict(u, foe, bead, dc);
                if (result == SentenceOutcome.Applied) applied++;
                Report(foe, bead, result);
            }
            var damage = DivinePacket(level, applied);
            if (damage != null)
            {
                var cause = context ?? new MechanicsContext(u,foe,Blueprints.Training,null,foe);
                cause.TriggerRule(new RuleDealDamage(u, foe, damage) { Reason = cause });
            }
        }
    }

    public sealed class RiteGate : BlueprintComponent, IAbilityCasterRestriction, IAbilityTargetRestriction
    {
        public Rite Kind;
        public bool IsCasterRestrictionPassed(UnitEntityData u)
        {
            if (u == null || !Blueprints.Registered || !u.HasFact(Blueprints.Training)) return false;
            return Kind == Rite.Discard || Rosary.Capacity(u,Rosary.Part(u),Kind) > 0;
        }
        public bool IsTargetRestrictionPassed(UnitEntityData u, TargetWrapper target)
        {
            var t = target.Unit;
            if (Kind == Rite.Discard || Kind == Rite.Blade) return u == t;
            if (Kind == Rite.Gather || Kind == Rite.Vow)
                return Rosary.Ally(u,t) && (Kind == Rite.Vow || t.Buffs.Enumerable.Any(Afflictions.Eligible));
            return Rosary.Enemy(u,t);
        }
        public string GetAbilityCasterRestrictionUIText() => Rosary.Text("unavailable");
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target) => Rosary.Text("target");
    }

    public sealed class PerformRite : AbilityApplyEffect
    {
        public Rite Kind;
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var u = context.Caster; var t = target.Unit;
            var gate = context.AbilityBlueprint.GetComponent<RiteGate>();
            if (t == null || !gate.IsCasterRestrictionPassed(u) || !gate.IsTargetRestrictionPassed(u,target)) return;
            if (u.DistanceTo(t) - u.Corpulence - t.Corpulence > new Feet(Rules.RangeFeet).Meters) return;
            var p = Rosary.Part(u);
            if (Kind == Rite.Vow)
            {
                p.ClearWard();
                var ward = t.Buffs.AddBuff(Blueprints.Ward, u, new Rounds(1).Seconds);
                if (ward != null) { p.WardId = ward.UniqueId; p.WardTarget = t; }
                return;
            }
            var ids = (context.Ability as ChosenRite)?.Selection;
            if (ids == null || ids.Length == 0) return;
            if (Kind == Rite.Gather) Rosary.Gather(u,t,ids);
            else if (Kind == Rite.Discard)
            {
                foreach (var bead in p.Beads.Where(b => ids.Contains(b.Id)).ToArray())
                {
                    p.Beads.Remove(bead);
                    BeadStatus.Remove(u,bead);
                }
            }
            else if (Kind == Rite.Blade)
            {
                p.Armed = ids.Distinct().Where(id => p.Beads.Any(b => b.Id == id)).Take(Rosary.Capacity(u,p,Kind)).ToArray();
                p.ArmedUntil = Afflictions.Now + TimeSpan.FromSeconds(Rules.RoundSeconds).Ticks;
            }
            else Rosary.Release(u,t,Kind,ids,context);
        }
    }

    public sealed class RosaryEvents : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>,
        ITurnBasedModeHandler, ITurnBasedUnitTurnEndedHandler, ITurnBasedModeEnabledHandler, IUnitCombatHandler, IRestFinishedHandler
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.AttackRoll?.IsHit != true || evt.Weapon == null) return;
            var p = Rosary.Part(Owner);
            if (p.Armed.Length == 0 || !evt.Weapon.Enchantments.Any(e => {
                string id = e.Blueprint.AssetGuid.ToString(); return id == Guids.BaneEnchant || id == Guids.GreaterEnchant;
            })) return;
            if (evt.Target.State.IsDead && Owner.IsEnemy(evt.Target))
            {
                p.Armed = Array.Empty<string>(); // No living recipient; keep the beads.
                return;
            }
            Rosary.Release(Owner,evt.Target,Rite.Blade,p.Armed,Context);
        }
        public void HandleTurnStarted(UnitEntityData unit) { if (unit == Owner) Rosary.Part(Owner).UsedTurn = false; }
        public void HandleUnitTurnEnded(UnitEntityData unit) { if (unit == Owner) Rosary.Part(Owner).Armed = Array.Empty<string>(); }
        public void HandleUnitLeaveCombat(UnitEntityData unit) { if (unit == Owner) Rosary.Part(Owner).ClearCombat(); }
        public void HandleTurnBasedModeStateChanged(bool enabled)
        {
            if (enabled) { var p = Rosary.Part(Owner); p.UsedTurn = Afflictions.Now < p.NextRelease; }
        }
        public void UpdateMandatoryTurnBasedModeState() { }
        public void HandleRestFinished(RestStatus status) { if (status.RestSucceeded) Reset(); }
        protected override void OnDeactivate() { Reset(); }
        void Reset()
        {
            var p = Owner.Get<SorrowPart>();
            if (p == null) return;
            p.ClearWard();
            foreach (var bead in p.Beads.ToArray()) BeadStatus.Remove(Owner,bead);
            Owner.Remove<SorrowPart>();
        }
        public void HandleUnitJoinCombat(UnitEntityData unit) { }
        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck roll) { }
    }

    public sealed class VowWatcher : UnitBuffComponentDelegate, IUnitBuffHandler
    {
        public void HandleBuffDidAdded(Buff buff)
        {
            if (buff == Buff || buff.Owner != Owner || !Afflictions.Eligible(buff)) return;
            var u = Context.MaybeCaster;
            var p = u?.Get<SorrowPart>();
            if (p == null || p.WardId != Buff.UniqueId) return;
            if (Rosary.Gather(u,Owner,new[] { buff.UniqueId }) > 0) p.ClearWard();
        }
        public void HandleBuffDidRemoved(Buff buff) { }
    }
}
