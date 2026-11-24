using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using Newtonsoft.Json;
using UnityEngine;
using WotrClassMod.Common;
using ModContext = WotrClassMod.Common.ModContext;

namespace DreamThief
{
    public sealed class KnifeRequired : BlueprintComponent, IAbilityCasterRestriction
    {
        internal static bool IsKnife(ItemEntityWeapon item) => item?.Blueprint.Category == WeaponCategory.Starknife;
        internal static ItemEntityWeapon Held(UnitEntityData unit)
        {
            var primary = unit?.Body.PrimaryHand.MaybeWeapon;
            if (IsKnife(primary)) return primary;
            var secondary = unit?.Body.SecondaryHand.MaybeWeapon;
            return IsKnife(secondary) ? secondary : null;
        }
        public bool IsCasterRestrictionPassed(UnitEntityData caster) => Held(caster) != null;
        public string GetAbilityCasterRestrictionUIText() => Strings.Resolve(ModContext.StringPrefix + "knife");
    }

    public sealed class DreamTraining : UnitFactComponentDelegate<DreamTraining.GrantData>, IResourceAmountBonusHandler,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>, IInitiatorRulebookHandler<RuleCalculateAbilityParams>,
        IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
        public sealed class GrantData { [JsonProperty] public bool Received; }
        protected override void OnTurnOn()
        {
            // AddAbilityResources runs in OnActivate. The amount handler subscribes in TurnOn.
            // Fill once after subscription and preserve spent starlight across saves and toggles.
            if (Data.Received) return;
            if (Owner.Resources.GetResourceAmount(Blueprints.Starlight) < Blueprints.Starlight.GetMaxAmount(Owner))
                Owner.Resources.Restore(Blueprints.Starlight);
            Data.Received = true;
        }
        internal static int Level(UnitEntityData caster) => caster.Progression.GetClassLevel(Blueprints.Rogue);
        internal static int DC(UnitEntityData caster) => DreamRules.DifficultyClass(Level(caster), caster.Stats.Charisma.Bonus);
        public void CalculateMaxResourceAmount(BlueprintAbilityResource resource, ref int bonus)
        {
            if (resource == Blueprints.Starlight)
                bonus += DreamRules.Starlight(Level(Owner), Owner.Stats.Dexterity.Bonus, Owner.Stats.Charisma.Bonus);
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (KnifeRequired.IsKnife(evt.Weapon) && evt.Target.State.HasCondition(UnitCondition.Sleeping))
            {
                evt.ForceFlatFooted = true;
                evt.AutoCriticalThreat = true;
                evt.AutoCriticalConfirmation = true;
            }
        }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            // A starknife is a melee blueprint. Only this particular ranged throw uses Dexterity.
            var attack = evt.WeaponStats?.AttackWithWeapon;
            if (attack?.ReplaceAttackType == true && attack.AttackTypeReplacement == AttackType.Ranged &&
                attack.Reason.Context?.SourceAbility?.GetComponent<ThrowKnife>() != null)
                evt.AttackBonusStat = StatType.Dexterity;
        }
        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt.Blueprint == null || !TalentBlueprints.Casts.TryGetValue(evt.Blueprint.AssetGuid, out var row)) return;
            evt.ReplaceCasterLevel = Level(Owner); evt.ReplaceDC = DC(Owner);
            evt.ReplaceSpellLevel = Math.Max(0, row.SpellLevel);
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
    }

    public sealed class ReturningKnifeView : MonoBehaviour
    {
        internal bool Arrived { get; private set; }
        internal static Vector3 Aim(UnitEntityData unit) => unit.View?.CenterTorso?.position ?? unit.Position + Vector3.up * unit.Corpulence;

        private static Vector3 Hand(UnitEntityData owner, ItemEntityWeapon weapon)
        {
            var hands = owner.View?.HandsEquipment;
            if (hands == null || !hands.Sets.TryGetValue(owner.Body.CurrentHandsEquipmentSet, out var set)) return Aim(owner);
            var slot = owner.Body.PrimaryHand.MaybeWeapon == weapon ? set.MainHand : set.OffHand;
            return slot?.VisibleItem == weapon && slot.IsInHand && slot.VisualModel != null ? slot.VisualModel.transform.position : Aim(owner);
        }

        // One turn per leg. Travel time follows the stock thrown weapon and the game clock (including pause).
        private System.Collections.IEnumerator Fly(UnitEntityData destination, ItemEntityWeapon returningWeapon)
        {
            if (destination.IsDisposed) yield break;
            Vector3 start = transform.position;
            float duration = Vector3.Distance(start, Aim(destination)) / Blueprints.KnifeFlightSpeed;
            Quaternion rotation = transform.rotation;
            for (float elapsed = 0; elapsed < duration;)
            {
                yield return null;
                if (destination.IsDisposed) yield break;
                elapsed += Game.Instance.TimeController.DeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                Vector3 end = returningWeapon == null ? Aim(destination) : Hand(destination, returningWeapon);
                transform.position = Vector3.Lerp(start, end, progress);
                transform.rotation = Quaternion.AngleAxis(360f * progress, Vector3.up) * rotation;
            }
        }

        internal System.Collections.IEnumerator RoundTrip(UnitEntityData owner, UnitEntityData target, ItemEntityWeapon weapon)
        {
            try
            {
                yield return Fly(target, null);
                Arrived = true;
                yield return Fly(owner, weapon);
            }
            finally { Destroy(gameObject); }
        }
        internal static ReturningKnifeView Launch(UnitEntityData caster, UnitEntityData foe, ItemEntityWeapon knife)
        {
            GameObject blade = null;
            try
            {
                var prefab = knife.WeaponVisualParameters?.Model ?? knife.Blueprint.VisualParameters.Model;
                if (prefab == null || caster.View == null) throw new InvalidOperationException("Missing weapon model or caster view.");
                blade = Instantiate(prefab, Hand(caster, knife), Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(blade, caster.View.gameObject.scene);
                foreach (var collider in blade.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                blade.SetActive(true);
                var flight = blade.AddComponent<ReturningKnifeView>();
                flight.StartCoroutine(flight.RoundTrip(caster, foe, knife));
                return flight;
            }
            catch (Exception ex)
            {
                if (blade != null) Destroy(blade);
                ModContext.Entry.Logger.Warning("DreamThief: cannot show " + knife.Blueprint.name + ": " + ex.Message);
                return null;
            }
        }
    }

    public sealed class KnifeFlight : AbilityDeliverEffect
    {
        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.Caster;
            var foe = target.Unit;
            var knife = KnifeRequired.Held(caster);
            if (knife == null || foe == null || foe.IsDisposed) yield break;
            var flight = ReturningKnifeView.Launch(caster, foe, knife);
            while (flight != null && !flight.Arrived) yield return null;
            if (!foe.IsDisposed && !caster.IsDisposed) yield return new AbilityDeliveryTarget(target);
        }
    }

    public sealed class ThrowKnife : AbilityApplyEffect
    {
        public bool Sleep;
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.Caster; var foe = target.Unit; var knife = KnifeRequired.Held(caster);
            if (foe == null || foe.State.IsDead || !caster.IsEnemy(foe) || knife == null) return;
            var shot = new RuleAttackWithWeapon(caster, foe, knife, 0) {
                Reason = context, SkipAnimation = true, ReplaceAttackType = true, AttackTypeReplacement = AttackType.Ranged
            };
            context.TriggerRule(shot);
            // Return flight has no rulebook event or damage callback.
            if (!Sleep || shot.AttackRoll?.IsHit != true || foe.State.IsDead) return;
            // Resolve weapon damage before sleep. The blade remains the same equipped item throughout.
            var sleep = new MechanicsContext(caster, foe, Blueprints.SleepSource, context, foe);
            sleep.SetParams(new AbilityParams { DC = DreamTraining.DC(caster), CasterLevel = DreamTraining.Level(caster) });
            sleep.AddSpellDescriptor(SpellDescriptor.Sleep | SpellDescriptor.MindAffecting);
            using (sleep.GetDataScope(foe)) DreamSleep.Resolve(sleep, foe, "Throw");
        }
    }

    internal static class DreamSleep
    {
        internal static void Resolve(MechanicsContext context, UnitEntityData foe, string kind)
        {
            if (foe.Buffs.HasFact(Blueprints.Immune) || foe.State.HasConditionImmunity(UnitCondition.Sleeping) ||
                foe.Get<UnitPartSpellResistance>()?.IsImmune(context, false) == true)
            {
                Game.Instance.UI?.Bark(foe, Strings.Resolve(ModContext.StringPrefix + "resists"), -1);
                return;
            }
            int level = DreamTraining.Level(context.MaybeCaster);
            if (context.TriggerRule(new RuleSavingThrow(foe, SavingThrowType.Will, DreamTraining.DC(context.MaybeCaster))).Success)
            {
                // A neutral unit-sourced marker must survive its owner's own sleep immunity.
                foe.Buffs.AddBuff(Blueprints.Immune, foe, new Rounds(DreamRules.ImmunityRounds).Seconds); return;
            }
            var buff = kind == "Nightmare" ? Blueprints.Nightmare :
                BlueprintUtil.Get<BlueprintBuff>(kind == "Restless" ? GameAssets.RestlessBuff : GameAssets.Sleeping);
            int rounds = kind == "Slumber" || kind == "Restless" ? level : RulebookEvent.Dice.D4;
            foe.Buffs.AddBuff(buff, context, new Rounds(rounds).Seconds);
        }
    }

    public sealed class ActivateShroud : AbilityApplyEffect
    {
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.Caster;
            caster.Buffs.AddBuff(Blueprints.Shroud, context, new Rounds(DreamRules.ShroudRounds).Seconds);
        }
    }

    public sealed class ShroudCombat : UnitFactComponentDelegate, IInitiatorRulebookHandler<RulePrepareDamage>,
        IInitiatorRulebookHandler<RuleCalculateAttacksCount>
    {
        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            var hit = evt.ParentRule.AttackRoll;
            if (hit?.IsHit != true || !KnifeRequired.IsKnife(evt.DamageBundle.Weapon) ||
                hit.Weapon != evt.DamageBundle.Weapon || evt.DamageBundle.WeaponDamage == null) return;
            evt.Add(new EnergyDamage(new DiceFormula(1, DiceType.D6), DamageEnergyType.Divine) { SourceFact = Fact });
            int dice = DreamRules.BurstDice(DreamTraining.Level(Owner), hit.WeaponStats.CriticalMultiplier,
                hit.IsCriticalConfirmed && !hit.ImmuneToCriticalHit && !hit.FortificationNegatesCriticalHit);
            if (dice > 0) evt.Add(new EnergyDamage(new DiceFormula(dice, DiceType.D10), DamageEnergyType.Divine) { SourceFact = Fact });
        }
        public void OnEventDidTrigger(RulePrepareDamage evt) { }
        public void OnEventAboutToTrigger(RuleCalculateAttacksCount evt)
        {
            var knife = KnifeRequired.Held(Owner);
            if (knife != null) evt.AddExtraAttacks(1, true, false, knife);
        }
        public void OnEventDidTrigger(RuleCalculateAttacksCount evt) { }
    }

    public sealed class WalkTier : BlueprintComponent, IAbilityVisibilityProvider, IAbilityCasterRestriction
    {
        public int Tier;
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
        {
            int level = DreamTraining.Level(caster);
            return Tier == (level >= DreamRules.MoveLevel ? 2 : level >= DreamRules.ReachLevel ? 1 : 0);
        }
        public bool IsAbilityVisible(AbilityData data) => IsCasterRestrictionPassed(data.Caster);
        public string GetAbilityCasterRestrictionUIText() => Strings.Resolve(ModContext.StringPrefix + "walk.name");
    }

    public sealed class Dreamwalk : AbilityApplyEffect
    {
        static bool Clear(UnitEntityData caster, Vector3 point)
        {
            if (!ObstacleAnalyzer.IsPointInsideNavMesh(point)) return false;
            return !Game.Instance.State.Units.Any(u => u != caster && u.IsInGame && !u.State.IsDead &&
                (u.Position - point).sqrMagnitude < Mathf.Pow(u.Corpulence + caster.Corpulence, 2));
        }

        static void Blocked(UnitEntityData caster)
        {
            var text = Strings.Resolve(ModContext.StringPrefix + "blocked");
            ModContext.Log(text); Game.Instance.UI?.Bark(caster, text, -1);
        }

        static void Gate(Vector3 position, bool appear)
        {
            var link = appear ? Blueprints.GateFx.CasterAppearFx : Blueprints.GateFx.CasterDisappearFx;
            var prefab = link?.Load();
            if (prefab != null) FxHelper.SpawnFxOnPoint(prefab,position,false,Quaternion.identity,FxPriority.EventuallyImportant);
        }

        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.Caster; var foe = target.Unit; var knife = KnifeRequired.Held(caster);
            if (knife == null || foe == null || !foe.IsInGame || foe.State.IsDead || !caster.IsEnemy(foe)) return;
            var origin = caster.Position;
            var offset = origin - foe.Position; offset.y = 0;
            if (offset.magnitude - caster.Corpulence - foe.Corpulence > new Feet(DreamRules.WalkFeet(DreamTraining.Level(caster))).Meters) return;
            if (offset.sqrMagnitude < 0.001f) offset = Vector3.forward;
            offset = offset.normalized * (caster.Corpulence + foe.Corpulence + 0.05f);
            Vector3? adjacent = null;
            for (int i = 0; i < 12 && !adjacent.HasValue; i++)
            {
                var candidate = foe.Position + Quaternion.Euler(0, 30 * i, 0) * offset;
                var grounded = ObstacleAnalyzer.TraceAlongNavmesh(foe.Position, candidate);
                if ((grounded - candidate).sqrMagnitude < 0.05f && Clear(caster, grounded)) adjacent = grounded;
            }
            if (!adjacent.HasValue || !ObstacleAnalyzer.IsPointInsideNavMesh(origin)) { Blocked(caster); return; }
            try
            {
                Gate(origin, false);
                caster.Position = adjacent.Value;
                Gate(adjacent.Value, true);
                context.TriggerRule(new RuleAttackWithWeapon(caster, foe, knife, 0) {
                    Reason = context, SkipAnimation = true, ForceFlatFooted = true
                });
            }
            finally
            {
                if (Clear(caster, origin)) caster.Position = origin;
                else
                {
                    Blocked(caster);
                    // An immediate reaction can occupy the origin. Search outward for a safe return.
                    bool returned = false;
                    for (int ring = 1; ring <= 3 && !returned; ring++)
                        for (int angle = 0; angle < 360 && !returned; angle += 30)
                        {
                            var delta = Quaternion.Euler(0, angle, 0) * Vector3.forward * (caster.Corpulence + 0.1f) * ring;
                            var nearby = ObstacleAnalyzer.TraceAlongNavmesh(origin, origin + delta);
                            if (Clear(caster, nearby)) { caster.Position = nearby; returned = true; }
                        }
                }
                Gate(caster.Position, true);
            }
        }
    }
}
