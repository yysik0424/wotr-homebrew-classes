using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.TempMapCode.Ambush;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace HellRider
{
    internal static class Standards
    {
        internal static BlueprintAbilityAreaEffect Small, Large;
        internal static BlueprintFeature Build()
        {
            var icon = Get<BlueprintFeature>(Ids.CavalierBanner).Icon;
            var banner = Blueprints.Feature("Banner", icon);
            var aura = Blueprints.Make<BlueprintBuff>("BannerBoon", b => {
                Blueprints.Name(b,"Greater",icon);
                b.FxOnStart = new PrefabLink(); EmptyFxOnRemove(b);
            });
            AddComponents(aura, new AddStatBonus { Stat = StatType.AdditionalAttackBonus, Value = 2, Descriptor = ModifierDescriptor.Morale },
                new BuffDescriptorImmunity { Descriptor = SpellDescriptor.Fear });
            BlueprintAbilityAreaEffect Area(string key, int radius)
            {
                var area = Blueprints.Copy(Get<BlueprintAbilityAreaEffect>(Ids.CavalierBannerArea), key, c => false);
                area.Size = new Feet(radius);
                area.AffectEnemies = true;
                area.Fx = new PrefabLink { AssetId = "946740b41f0bdc14086c4435d162b4b7" };
                SetField(area,"m_TickRoundAfterSpawn",true);
                SetField(area,"m_SkipTickRoundAfterSpawnRealtime",false);
                AddComponents(area,new StandardVolley());
                return area;
            }
            Small = Area("Banner20",20);
            Large = Area("Banner30",30);
            var boon = new AbilityAreaEffectBuff { Condition = new ConditionsChecker {
                Conditions = new Condition[] { new ContextConditionIsAlly() } }, CheckConditionEveryRound = true };
            SetField(boon,"m_Buff",Ref<BlueprintBuffReference>(aura.AssetGuid.ToString()));
            AddComponents(Large,boon);
            var a = Blueprints.Make<BlueprintAbility>("Plant", b => {
                Blueprints.Name(b,"Banner",icon);
                b.Type = AbilityType.Supernatural; b.Range = AbilityRange.Medium;
                b.ActionType = UnitCommand.CommandType.Standard;
                b.CanTargetPoint = true;
                b.LocalizedDuration = b.LocalizedSavingThrow = NoText();
            });
            AddComponents(a,new PlantStandard());
            Magic.Spend(a,Magic.Pool("Banner",banner,3,true));
            Blueprints.Grant(banner,a);
            return banner;
        }
    }

    public sealed class PlantStandard : AbilityApplyEffect
    {
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var area = context.Caster.Descriptor.HasFact(Blueprints.Greater) ? Standards.Large : Standards.Small;
            AreaEffectsController.Spawn(context, area, new TargetWrapper(target.Point), new Rounds(5).Seconds);
        }
    }

    public sealed class StandardRounds : EntityPart
    {
        [JsonProperty] public int Elapsed;
        internal bool Take()
        {
            if (Elapsed >= 5) return false;
            Elapsed++;
            return true;
        }
    }

    public sealed class StandardVolley : AbilityAreaEffectLogic
    {
        protected override void OnRound(MechanicsContext context, AreaEffectEntityData area)
        {
            if (!area.Ensure<StandardRounds>().Take()) return;
            var rider = context.MaybeCaster;
            if (rider == null || rider.State.IsDead) return;
            var enemies = Devils.Enemies(rider, area.Position, area.Blueprint.Size.Meters);
            foreach (var foe in enemies.Take(Rules.Devils(Magic.Level(rider))).ToArray())
                Devils.Hit(context,rider,foe,0);
        }
    }

    public sealed class HellCharge : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (!evt.IsCharge || !evt.IsFirstAttack || evt.AttackRoll == null || evt.AttackRoll.IsFake) return;
            var primary = evt.Target;
            var cause = Context ?? new MechanicsContext(Owner, primary, Fact.Blueprint, null, primary);
            var nearby = Devils.Enemies(Owner,primary.Position,new Feet(10).Meters).Where(u => u != primary)
                .Take(Rules.Devils(Magic.Level(Owner))).ToArray();
            for (int i = 0; i < Rules.Devils(Magic.Level(Owner)); i++)
                Devils.Hit(cause,Owner,i < nearby.Length ? nearby[i] : primary,i);
        }
    }

    internal static class Devils
    {
        internal static IEnumerable<UnitEntityData> Enemies(UnitEntityData owner, Vector3 position, float radius)
            => Game.Instance.State.Units.Where(u => u.IsInGame && !u.State.IsDead && owner.IsEnemy(u) &&
                (u.Position - position).sqrMagnitude <= radius * radius)
                .OrderBy(u => (u.Position - position).sqrMagnitude).ThenBy(u => u.UniqueId,StringComparer.Ordinal);

        internal static void BuildVisual()
        {
            var src = Get<BlueprintUnit>(Ids.CR17_DevilApostateStandard);
            Blueprints.Devil = CloneUnit(src, "HellRider_DevilApparition", Rules.Id("ApparitionUnit"));
            Blueprints.Devil.ComponentsArray = new BlueprintComponent[0];
            var shell = Blueprints.Feature("Apparition", null);
            shell.HideInUI = true;
            var ignored = new AddMechanicsFeature();
            SetField(ignored,"m_Feature",AddMechanicsFeature.MechanicsFeatureType.IsIgnoredByCombat);
            AddComponents(shell,new Untargetable(), new MarkPassive(), ignored);
            SetField(Blueprints.Devil,"m_AddFacts",new[] { Ref<BlueprintUnitFactReference>(shell.AssetGuid.ToString()) });
            SetField(Blueprints.Devil,"m_Brain",null);
            SetField(Blueprints.Devil,"m_Faction",Ref<BlueprintFactionReference>("d64258e86eeb1d8479f35a9b16f6590a"));
        }

        internal static void Hit(MechanicsContext ctx, UnitEntityData rider, UnitEntityData target, int index)
        {
            int level = Magic.Level(rider);
            if (!target.State.IsDead)
            {
                var saved = ctx.TriggerRule(new RuleSavingThrow(target,SavingThrowType.Reflex,Rules.SaveDC(level,rider.Stats.Charisma.Bonus))).Success;
                int total = ctx.TriggerRule(new RuleRollDice(rider,new DiceFormula(Rules.DevilDice(level),DiceType.D6))).Result;
                if (saved) total /= 2;
                if (total > 0)
                {
                    var fire = new EnergyDamage(new DiceFormula(0,DiceType.Zero),total / 2,DamageEnergyType.Fire);
                    var unholy = new EnergyDamage(new DiceFormula(0,DiceType.Zero),total - total / 2,DamageEnergyType.Unholy);
                    ctx.TriggerRule(new RuleDealDamage(rider,target,new DamageBundle(fire,unholy)) { Reason = ctx });
                }
            }
            try { DevilView.Show(target,index); }
            catch (Exception ex) { WotrClassMod.Common.ModContext.LogError("Devil apparition: " + ex); }
        }
    }

    public sealed class DevilView : MonoBehaviour
    {
        static readonly PrefabLink Portal = new PrefabLink { AssetId = "dc21c754292adb647a3bb7136e894092" };
        UnitEntityData unit;
        Vector3 ground;
        float elapsed;
        internal static void Show(UnitEntityData target, int index)
        {
            if (target.View == null || Game.Instance.LoadedAreaState?.MainState == null) return;
            var dir = Quaternion.Euler(0, 120 * index, 0) * target.View.transform.forward;
            var position = target.Position + dir * (target.View.Corpulence + .5f);
            var actor = Game.Instance.EntityCreator.SpawnUnit(Blueprints.Devil,position,Quaternion.LookRotation(-dir),Game.Instance.LoadedAreaState.MainState,null);
            actor.Ensure<UnitPartTrapActor>();
            try
            {
                var view = actor.View;
                actor.Descriptor.EnsureOwnInventory();
                view.ActivateColliders(false);
                view.MovementAgent.enabled = false;
                var fx = view.gameObject.AddComponent<DevilView>();
                fx.unit = actor; fx.ground = position;
                actor.Position = position - Vector3.up;
                var portal = Portal.Load();
                if (portal != null) FxHelper.SpawnFxOnPoint(portal, position, false, Quaternion.identity, FxPriority.EventuallyImportant);
                var hand = actor.GetThreatHandMelee();
                if (hand != null)
                {
                    var swing = new AttackHandInfo(hand,0);
                    swing.CreateAnimationHandleForAttack(Array.Empty<AttackHandInfo>(),false);
                    if (swing.AnimationHandle != null) view.AnimationManager.Execute(swing.AnimationHandle);
                }
            }
            catch { actor.MarkForDestroy(); throw; }
        }
        void LateUpdate()
        {
            if (unit == null || unit.IsDisposed || unit.DestroyMark) return;
            elapsed += Game.Instance.TimeController.DeltaTime;
            if (elapsed >= 1f || unit.HoldingState != null && unit.HoldingState != Game.Instance.LoadedAreaState?.MainState) unit.MarkForDestroy();
            else unit.Position = ground - Vector3.up * Mathf.Clamp01(1f - elapsed * 4f);
        }
    }
}
