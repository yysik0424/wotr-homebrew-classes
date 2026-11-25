using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;
using WotrClassMod.Common;

namespace SorrowInquisitor
{
    // Only scalar values and asset/unit IDs travel in the save. A bead represents one cause.
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Bead
    {
        [JsonProperty] public string Id, MarkerId, BuffId, SourceSpell, Aggressor, Ally, AllyName, AggressorName;
        [JsonProperty] public long Duration, TickDelay, Interval;
        [JsonProperty] public int Rank, CasterLevel, SpellLevel;
        [JsonProperty] public SavingThrowType Save;
        [JsonProperty] public int[] Ranks, Shared;
        [JsonProperty] public Dictionary<string, PoisonStage> Poisons = new Dictionary<string, PoisonStage>();
        internal BlueprintBuff Blueprint => BlueprintUtil.Get<BlueprintBuff>(BuffId);
    }

    public sealed class PoisonStage
    {
        public int Ticks, Saves, BonusDC;
    }

    public enum SentenceOutcome { Applied, Saved, Unaffected }

    internal static class Afflictions
    {
        internal static long Now => Game.Instance.TimeController.GameTime.Ticks;
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static readonly PropertyInfo nextTick = typeof(Buff).GetProperty("NextTickTime", Members);
        static readonly PropertyInfo tick = typeof(Buff).GetProperty("TickTime", Members);
        static readonly FieldInfo contextField = typeof(Buff).GetField("m_Context", Members);
        static readonly FieldInfo grappleBuff = typeof(UnitPartGrappleTarget).GetField("m_Buff", Members);
        static readonly FieldInfo ranks = typeof(MechanicsContext).GetField("m_Ranks", Members);
        static readonly FieldInfo shared = typeof(MechanicsContext).GetField("m_SharedValues", Members);
        static readonly MethodInfo poisonRuntime = typeof(EntityFactComponentDelegate<UnitEntityData, BuffPoisonStatDamageData>).GetProperty("Runtime", Members)?.GetGetMethod(true);
        static int[] Snapshot(FieldInfo f, MechanicsContext ctx) => (int[])((int[])f.GetValue(ctx)).Clone();
        internal static void CheckBindings()
        {
            if (poisonRuntime == null || ranks?.FieldType != typeof(int[]) || shared?.FieldType != typeof(int[]) || nextTick?.PropertyType != typeof(TimeSpan) || tick?.PropertyType != typeof(TimeSpan) ||
                contextField?.FieldType != typeof(MechanicsContext) || grappleBuff?.FieldType != typeof(Buff))
                throw new InvalidOperationException("Unsupported buff timing, context or grapple layout.");
        }
        static readonly HashSet<UnitCondition> Debilitating = new HashSet<UnitCondition> {
            UnitCondition.Blindness, UnitCondition.Nauseated, UnitCondition.Fatigued, UnitCondition.Paralyzed,
            UnitCondition.Staggered, UnitCondition.Petrified, UnitCondition.Dazed, UnitCondition.Slowed,
            UnitCondition.Entangled, UnitCondition.Frightened, UnitCondition.Prone, UnitCondition.Sickened,
            UnitCondition.Sleeping, UnitCondition.CantMove, UnitCondition.Shaken, UnitCondition.Dazzled,
            UnitCondition.Stunned, UnitCondition.Confusion, UnitCondition.SpellCastingIsDifficult,
            UnitCondition.Cowering, UnitCondition.Exhausted, UnitCondition.SpellcastingForbidden,
            UnitCondition.SpellCastingIsVeryDifficult, UnitCondition.CantAct, UnitCondition.CanNotAttack
        };
        static bool BaseStat(StatType s) => s == StatType.Strength || s == StatType.Dexterity || s == StatType.Constitution ||
            s == StatType.Intelligence || s == StatType.Wisdom || s == StatType.Charisma;

        internal static bool Excluded(BlueprintComponent c)
        {
            if (c is Kingmaker.UnitLogic.Buffs.Components.AddGenericStatBonus g) return BaseStat(g.Stat);
            if (c is AddStatBonus a) return BaseStat(a.Stat);
            if (c is AddContextStatBonus b) return BaseStat(b.Stat);
            if (c is AddStatBonusAbilityValue v) return BaseStat(v.Stat);
            string name = c.GetType().Name;
            return name == "ForbidSpecificSpellsCast" || name.Contains("NegativeLevel") || name == "AddStatDamage";
        }

        // Exhaustion owns its native ability modifiers. They end together when exhaustion is cured.
        static bool KeepOnAlly(BlueprintBuff bp, BlueprintComponent c) => Excluded(c) && bp.AssetGuid.ToString() != Guids.Exhausted;

        static UnitCondition? Condition(BlueprintComponent c)
        {
            if (c is AddCondition a) return a.Condition;
            if (c is BuffStatusCondition b) return b.Condition;
            return null;
        }

        static bool Negative(BlueprintComponent c, MechanicsContext ctx)
        {
            if (Excluded(c)) return false;
            var condition = Condition(c);
            if (condition.HasValue) return Debilitating.Contains(condition.Value);
            if (c is BuffPoisonStatDamage) return true;
            if (c is Kingmaker.UnitLogic.Buffs.Components.AddGenericStatBonus g) return g.Value < 0;
            if (c is AddStatBonus a) return a.Value < 0;
            if (c is AddContextStatBonus b) return ctx != null && b.Multiplier * b.Value.Calculate(ctx) < 0;
            if (c is AddStatBonusAbilityValue v) return ctx != null && v.Value.Calculate(ctx) < 0;
            string n = c.GetType().Name;
            return n == "AddEnergyVulnerability" || n == "ShifterGrabTargetBuff";
        }

        internal static bool Eligible(Buff b)
        {
            if (b == null || b.IsDisposed || b.IsSuppressed || !b.IsActive || b.Context == null ||
                b.Context.ParentContext?.AssociatedBlueprint == Blueprints.Residual) return false;
            if (!b.IsPermanent && b.TimeLeft <= TimeSpan.Zero) return false;
            var bp = b.Blueprint;
            if (!bp.Harmful && b.Context.SourceAbility == null && !NativeEffects.Scripted.Contains(bp.AssetGuid.ToString()) &&
                !bp.ComponentsArray.Any(c => c is BuffPoisonStatDamage || c.GetType().Name == "ShifterGrabTargetBuff")) return false;
            return EligibleBlueprint(bp,b.Context);
        }

        internal static bool EligibleBlueprint(BlueprintBuff bp, MechanicsContext context)
        {
            if (NativeEffects.Scripted.Contains(bp.AssetGuid.ToString())) return true;
            // Avoid positive stances and class toggles with a drawback; encounter and spell scripts
            // with irreversible actions are only replayed if their specific native effect was audited.
            return bp.ComponentsArray.Any(c => Negative(c, context)) &&
                bp.ComponentsArray.All(c => NativeEffects.Simple.Contains(c.GetType().Name) || Excluded(c));
        }

        internal static Bead Capture(Buff b)
        {
            var ctx = b.Context;
            var spell = ctx.SourceAbility ?? b.SourceAbility;
            var initial = spell?.GetComponent<AbilityEffectRunAction>();
            var save = initial?.SavingThrowType ?? SavingThrowType.Unknown;
            var activeSave = ctx.SavingThrow;
            if (save == SavingThrowType.Unknown && activeSave?.Initiator == b.Owner.Unit)
                save = activeSave.Type;
            if (save == SavingThrowType.Unknown && initial == null)
                save = b.Blueprint.GetComponent<BuffPoisonStatDamage>()?.SaveType ??
                    b.Blueprint.GetComponent<BuffStatusCondition>()?.SaveType ?? SavingThrowType.Unknown;
            var bead = new Bead { Id = b.UniqueId, BuffId = b.Blueprint.AssetGuid.ToString(),
                SourceSpell = spell?.AssetGuid.ToString(), Aggressor = ctx.MaybeCaster?.UniqueId, Ally = b.Owner.Unit.UniqueId,
                AllyName = b.Owner.CharacterName, AggressorName = ctx.MaybeCaster?.CharacterName,
                Duration = b.IsPermanent ? long.MaxValue : b.TimeLeft.Ticks, Rank = b.Rank,
                Interval = ((TimeSpan)tick.GetValue(b)).Ticks,
                TickDelay = (TimeSpan)nextTick.GetValue(b) == TimeSpan.MaxValue ? long.MaxValue : Math.Max(0L, ((TimeSpan)nextTick.GetValue(b)).Ticks - Now),
                Save = save == SavingThrowType.Unknown ? SavingThrowType.Will : save,
                CasterLevel = ctx.Params.CasterLevel, SpellLevel = ctx.Params.SpellLevel, Ranks = Snapshot(ranks, ctx), Shared = Snapshot(shared, ctx) };
            foreach (var component in b.Components)
            {
                if (!(component.SourceBlueprintComponent is BuffPoisonStatDamage)) continue;
                var data = component.GetData<BuffPoisonStatDamageData>();
                bead.Poisons.Add(component.SourceBlueprintComponentName, new PoisonStage {
                    Ticks = data.TicksPassed, Saves = data.SavesSucceeded, BonusDC = data.BonusDC });
            }
            return bead;
        }

        static bool RankSupport(BlueprintComponent c) => c is Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig ||
            c is SpellDescriptorComponent;

        internal static bool ReleaseAlly(Buff b)
        {
            if (!Eligible(b)) return false;
            bool alreadyProjected = b.Context.ParentContext?.AssociatedBlueprint == Blueprints.Training;
            if (!alreadyProjected && b.Blueprint.ComponentsArray.Any(c => KeepOnAlly(b.Blueprint,c)))
            {
                // Keep the original fact, expiry and stat modifier handles on the ally; the
                // serialized parent marker also filters components when the engine rebuilds a save.
                var original = b.Context;
                var marker = new MechanicsContext(original.MaybeCaster, b.Owner, Blueprints.Residual, original.ParentContext, b.Owner.Unit);
                var remaining = new MechanicsContext(original.MaybeCaster, b.Owner, b.Blueprint, marker, b.Owner.Unit);
                remaining.SetParams(original.Params);
                ranks.SetValue(remaining, Snapshot(ranks, original));
                shared.SetValue(remaining, Snapshot(shared, original));
                contextField.SetValue(b, remaining);
                foreach (var c in b.Components.ToArray())
                {
                    if (KeepOnAlly(b.Blueprint,c.SourceBlueprintComponent) || RankSupport(c.SourceBlueprintComponent)) continue;
                    c.Deactivate(); b.Components.Remove(c); c.Dispose();
                }
                return true;
            }
            var unit = b.Owner.Unit;
            var grapple = unit.Get<UnitPartGrappleTarget>();
            if (grapple != null && ReferenceEquals(grappleBuff.GetValue(grapple), b)) grapple.Initiator.Value?.ReleaseGrapple();
            if (!b.IsDisposed && unit.Buffs.Enumerable.Contains(b)) b.Remove();
            return !unit.Buffs.Enumerable.Contains(b);
        }

        [HarmonyPatch(typeof(EntityFact), "get_BlueprintComponents")]
        internal static class Projection
        {
            static void Postfix(EntityFact __instance, ref List<BlueprintComponent> __result)
            {
                if (!Blueprints.Registered || !(__instance is Buff b)) return;
                var tag = b.Context?.ParentContext?.AssociatedBlueprint;
                if (tag == Blueprints.Residual) __result = __result.Where(c => KeepOnAlly(b.Blueprint,c) || RankSupport(c)).ToList();
                else if (tag == Blueprints.Training && __result.Any(c => KeepOnAlly(b.Blueprint,c)))
                    __result = __result.Where(c => !KeepOnAlly(b.Blueprint,c)).ToList();
            }
        }

        [ThreadStatic] static Bead resuming;
        [ThreadStatic] static Buff recipient;
        [HarmonyPatch(typeof(BuffPoisonStatDamage), "OnActivate")]
        internal static class ResumePoison
        {
            static bool Prefix(BuffPoisonStatDamage __instance)
            {
                if (resuming == null) return true;
                var runtime = (EntityFactComponent)poisonRuntime.Invoke(__instance, null);
                if (!ReferenceEquals(runtime.Fact, recipient)) return true;
                var saved = resuming.Poisons[runtime.SourceBlueprintComponentName];
                RestoreStage(__instance,runtime.GetData<BuffPoisonStatDamageData>(),saved);
                return false;
            }
        }

        internal static void RestoreStage(BuffPoisonStatDamage poison, BuffPoisonStatDamageData data, PoisonStage saved)
        {
            data.ContextValue = new ContextDiceValue { DiceCountValue = poison.Value.Rolls,
                DiceType = poison.Value.Dice, BonusValue = poison.Bonus };
            data.TicksPassed = saved.Ticks; data.SavesSucceeded = saved.Saves; data.BonusDC = saved.BonusDC;
        }

        internal static SentenceOutcome Inflict(UnitEntityData caster, UnitEntityData target, Bead bead, int dc)
        {
            var bp = bead.Blueprint;
            if (bp.ComponentsArray.Any(c => c.GetType().Name == "ShifterGrabTargetBuff"))
                bp = BlueprintUtil.Get<BlueprintBuff>(Guids.Restraint); // independent restraint, native break-free checks
            var spell = bead.SourceSpell == null ? null : BlueprintUtil.Get<BlueprintAbility>(bead.SourceSpell);
            if (spell != null && spell.ComponentsArray.OfType<IAbilityTargetRestriction>()
                .Any(r => !r.IsTargetRestrictionPassed(caster, target))) return SentenceOutcome.Unaffected;
            var conditions = bp.ComponentsArray.Select(Condition).Where(c => c.HasValue).Select(c => c.Value).ToArray();
            if (conditions.Length > 0 && conditions.All(c => target.State.HasConditionImmunity(c))) return SentenceOutcome.Unaffected;
            // A pre-existing non-stackable effect is not a new affliction. Avoid prolonging it or
            // replacing an existing stronger one and taking credit for divine damage.
            if (bp.Stacking != StackingType.Stack && target.Buffs.HasFact(bp)) return SentenceOutcome.Unaffected;
            var origin = spell == null ? null : new MechanicsContext(caster, target, spell, null, target);
            var marker = new MechanicsContext(caster, target, Blueprints.Training, origin, target);
            var ctx = new MechanicsContext(caster, target, bp, marker, target);
            ctx.SetParams(new AbilityParams { DC = dc, CasterLevel = bead.CasterLevel, SpellLevel = bead.SpellLevel });
            if (spell != null) ctx.AddSpellDescriptor(spell.SpellDescriptor);
            ranks.SetValue(ctx, (int[])bead.Ranks.Clone());
            shared.SetValue(ctx, (int[])bead.Shared.Clone());
            using (ctx.GetDataScope(target))
                return Resolve(target,bead,bp,ctx,dc);
        }

        static SentenceOutcome Resolve(UnitEntityData target, Bead bead, BlueprintBuff bp, MechanicsContext ctx, int dc)
        {
            if (ctx.TriggerRule(new RuleSavingThrow(target, bead.Save, dc)).Success) return SentenceOutcome.Saved;
            var length = TimeSpan.FromTicks(Math.Min(bead.Duration, TimeSpan.FromSeconds(Rules.ReleaseSeconds).Ticks));
            if (length <= TimeSpan.Zero) return SentenceOutcome.Unaffected;
            var buff = new Buff(bp, ctx, length);
            if (bead.Rank > 1) buff.SetRank(bead.Rank);
            var previous = resuming; var previousBuff = recipient;
            try
            {
                resuming = bead; recipient = buff;
                var added = target.Facts.Add(buff); // native PrepareFactForAttach runs immunity and stacking
                if (!ReferenceEquals(added, buff) || buff.IsDisposed || !buff.IsActive || buff.IsSuppressed ||
                    !target.Buffs.Enumerable.Contains(buff)) return SentenceOutcome.Unaffected;
                if (bead.TickDelay < TimeSpan.MaxValue.Ticks - Now)
                {
                    var next = TimeSpan.FromTicks(Now + bead.TickDelay);
                    nextTick.SetValue(buff, next);
                    tick.SetValue(buff, TimeSpan.FromTicks(bead.Interval));
                    target.Buffs.UpdateNextEvent();
                }
                return SentenceOutcome.Applied;
            }
            finally { resuming = previous; recipient = previousBuff; }
        }
    }
}
