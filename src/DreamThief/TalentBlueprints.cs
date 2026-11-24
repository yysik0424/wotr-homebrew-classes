using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace DreamThief
{
    internal static class TalentBlueprints
    {
        internal static readonly Dictionary<BlueprintGuid, Talent> Casts = new Dictionary<BlueprintGuid, Talent>();

        internal static BlueprintFeatureSelection Build(BlueprintFeatureSelection normal)
        {
            var choices = new List<BlueprintFeature>();
            foreach (var row in DreamTalents.All)
            {
                BlueprintUnitFact power = row.Key == "Aura" ? (BlueprintUnitFact)Aura() : Spell(row);
                var choice = Blueprints.Feature("Talent" + row.Key, row.Key, power.Icon, power);
                var require = new PrerequisiteClassLevel { Level = row.Level };
                SetField(require, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(GameAssets.Rogue));
                AddComponents(choice, require);
                choices.Add(choice);
            }
            return BlueprintCopy.Copy(normal, "TalentSelection", c => true, s => {
                Blueprints.Name(s, "talent", Blueprints.SleepSource.Icon);
                // Preserve the source selection's visibility split as well as its complete list.
                SetField(s,"m_Features", normal.Features.Concat(choices).Select(x => Ref<BlueprintFeatureReference>(x.AssetGuid.ToString())).ToArray());
                SetField(s,"m_AllFeatures", normal.AllFeatures.Concat(choices).Select(x => Ref<BlueprintFeatureReference>(x.AssetGuid.ToString())).ToArray());
            });
        }

        static BlueprintAbility Spell(Talent row)
        {
            var original = Get<BlueprintAbility>(row.SourceId);
            bool hex = row.SpellLevel < 0;
            var a = BlueprintCopy.Copy(original, "Cast" + row.Key, c => {
                var name = c.GetType().Name;
                if (name == "SpellListComponent" || name == "CraftInfoComponent" || name == "CantripComponent" ||
                    name == "RecommendationNoFeatFromGroup" || c is ContextSetAbilityParams) return false;
                return !hex || (!(c is AbilityEffectRunAction) && name != "AbilityTargetHasFact" && !(c is ContextRankConfig));
            }, clone => {
                clone.Type = AbilityType.SpellLike;
                clone.ActionType = original.ActionType;
                Blueprints.Name(clone, row.Key, original.Icon);
            });
            if (hex)
            {
                if (row.Key == "Nightmare")
                {
                    Blueprints.Nightmare = BlueprintCopy.Copy(Get<BlueprintBuff>(GameAssets.NightmareBuff), "NightmareBuff",
                        c => c.GetType().Name != "CombatStateTrigger", b => {
                            Blueprints.Name(b,"Nightmare",original.Icon);
                            var wake = b.GetComponent<AddIncomingDamageTrigger>();
                            wake.IgnoreDamageFromThisFact = true;
                            wake.Actions = new ActionList { Actions = new GameAction[] { new ContextActionRemoveSelf() } };
                        });
                    EmptyFxOnRemove(Blueprints.Nightmare);
                }
                AddComponents(a,new HexEffect { Kind = row.Key });
            }
            if (row.Key == "Sleep" || row.Key == "DeepSlumber")
            {
                var shared = a.GetComponent<ContextCalculateSharedValue>();
                shared.Value.DiceType = DiceType.One;
                shared.Value.DiceCountValue = new ContextValue { ValueType = ContextValueType.Rank };
                shared.Value.BonusValue = new ContextValue { Value = DreamRules.SleepHitDice(0, row.Key == "DeepSlumber") };
                var run = a.GetComponent<AbilityEffectRunAction>();
                run.Actions = GuardSleep(run.Actions);
            }
            if (row.Key == "Cloak") ConfigureCloak(a);
            Blueprints.Cost(a,row.Cost);
            Casts.Add(a.AssetGuid,row);
            return a;
        }

        static void ConfigureCloak(BlueprintAbility a)
        {
            var area = BlueprintCopy.Copy(Get<BlueprintAbilityAreaEffect>("6c116b31887c6284fbd41c070f6422f6"),
                "CloakArea", c => true, x => {
                    var run = x.GetComponent<AbilityAreaEffectRunAction>();
                    run.Round = GuardSleep(run.Round);
                });
            var buff = BlueprintCopy.Copy(Get<BlueprintBuff>("2e4b85213927f0a4ea2198e0f2a6028b"),
                "CloakBuff", c => true, x => {
                    var grant = x.GetComponent<AddAreaEffect>();
                    SetField(grant, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area.AssetGuid.ToString()));
                });
            EmptyFxOnRemove(buff);
            var apply = a.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
            SetField(apply, "m_Buff", Ref<BlueprintBuffReference>(buff.AssetGuid.ToString()));
            AddComponents(a, new SpellDescriptorComponent { Descriptor = new SpellDescriptorWrapper(SpellDescriptor.Sleep) });
        }

        static ActionList GuardSleep(ActionList actions)
        {
            int saves = AppendDreamward(actions);
            if (saves == 0) throw new InvalidOperationException("Dream sleep action no longer has a native Will-save branch.");
            return new ActionList { Actions = new GameAction[] { new EligibleSleepActions { Inner = actions } } };
        }

        static int AppendDreamward(ActionList list)
        {
            int count = 0;
            foreach (var action in list.Actions ?? new GameAction[0])
            {
                if (action is ContextActionConditionalSaved branch)
                {
                    branch.Succeed.Actions = (branch.Succeed.Actions ?? new GameAction[0]).Concat(new[] { new ApplyDreamward() }).ToArray();
                    count++;
                }
                else if (action is Conditional condition)
                    count += AppendDreamward(condition.IfTrue) + AppendDreamward(condition.IfFalse);
                else if (action is ContextActionSavingThrow save)
                    count += AppendDreamward(save.Actions);
            }
            return count;
        }

        static BlueprintActivatableAbility Aura()
        {
            var original = Get<BlueprintBuff>(GameAssets.LullAuraBuff);
            var effect = BlueprintCopy.Copy(Get<BlueprintBuff>(GameAssets.LullAuraEffect), "LullingEffect", c => false,
                b => Blueprints.Name(b,"Aura",original.Icon));
            EmptyFxOnRemove(effect);
            AddComponents(effect, new LullingPenalty());
            var area = BlueprintCopy.Copy(Get<BlueprintAbilityAreaEffect>(GameAssets.LullAuraArea), "LullingArea", c => true, x => {
                x.Size = new Feet(DreamRules.AuraFeet); x.AffectEnemies = true; x.AggroEnemies = false;
                SetField(x, "m_TargetType", Enum.Parse(FindField(typeof(BlueprintAbilityAreaEffect), "m_TargetType").FieldType, "Enemy"));
                SetField(x.GetComponent<AbilityAreaEffectBuff>(),"m_Buff", Ref<BlueprintBuffReference>(effect.AssetGuid.ToString()));
            });
            var aura = BlueprintCopy.Copy(original,"LullingAura",c => true, b => {
                Blueprints.Name(b,"Aura",original.Icon);
                SetField(b.GetComponent<AddAreaEffect>(), "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area.AssetGuid.ToString()));
            });
            EmptyFxOnRemove(aura);
            return Create<BlueprintActivatableAbility>("DreamThief_LullingToggle", Blueprints.Id("LullingToggle"), toggle => {
                Blueprints.Name(toggle,"Aura",original.Icon);
                SetField(toggle,"m_Buff", Ref<BlueprintBuffReference>(aura.AssetGuid.ToString()));
                toggle.ActivationType = AbilityActivationType.Immediately;
                toggle.DeactivateIfOwnerDisabled = true;
                SetField(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);
            });
        }
    }

    public sealed class EligibleSleepActions : ContextAction
    {
        public ActionList Inner;
        public override string GetCaption() => "Apply dream sleep to a susceptible target";
        public override void RunAction()
        {
            var unit = Target.Unit;
            if (unit != null && !unit.Buffs.HasFact(Blueprints.Immune) && !unit.State.HasConditionImmunity(UnitCondition.Sleeping))
                Inner.Run();
        }
    }

    public sealed class ApplyDreamward : ContextAction
    {
        public override string GetCaption() => "Apply immunity to Dream Thief sleep";
        public override void RunAction()
        {
            var unit = Target.Unit;
            if (unit != null) unit.Buffs.AddBuff(Blueprints.Immune, unit, new Rounds(DreamRules.ImmunityRounds).Seconds);
        }
    }

    public sealed class HexEffect : AbilityApplyEffect
    {
        public string Kind;
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var foe = target.Unit;
            if (foe != null && !foe.State.IsDead && context.Caster.IsEnemy(foe)) DreamSleep.Resolve(context,foe,Kind);
        }
    }

    public sealed class LullingPenalty : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleSavingThrow>
    {
        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var source = Fact.MaybeContext?.MaybeCaster;
            var context = evt.Reason.Context;
            if (source != null && context != null && (context.SpellDescriptor & SpellDescriptor.Sleep) != 0)
                evt.AddModifier(DreamRules.AuraPenalty(DreamTraining.Level(source)),Fact,ModifierDescriptor.Penalty);
        }
        public void OnEventDidTrigger(RuleSavingThrow evt) { }
    }
}
