using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.ResourceLinks;
using static WotrClassMod.Common.BlueprintUtil;

namespace HellRider
{
    internal static class Magic
    {
        internal static BlueprintAbilityResource RayPool;
        internal static BlueprintBuff BreathCooldown;
        internal static int Level(UnitEntityData rider) => rider?.Descriptor.Progression.GetClassLevel(Blueprints.Cavalier) ?? 0;

        internal static BlueprintAbilityResource Pool(string key, BlueprintFeature feature, int uses, bool charisma = false)
        {
            var pool = Blueprints.Make<BlueprintAbilityResource>(key + "Uses", r =>
            {
                r.LocalizedName = WotrClassMod.Common.Strings.L(key + ".name");
                r.LocalizedDescription = WotrClassMod.Common.Strings.L(key + ".desc");
                SetField(r, "m_Icon", feature.Icon);
                var amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                SetField(amount, "BaseValue", uses);
                if (charisma)
                {
                    SetField(amount, "IncreasedByStat", true);
                    SetField(amount, "ResourceBonusStat", StatType.Charisma);
                }
                SetField(r, "m_MaxAmount", amount);
            });
            var grant = new AddAbilityResources { RestoreAmount = true };
            SetField(grant, "m_Resource", Ref<BlueprintAbilityResourceReference>(pool.AssetGuid.ToString()));
            AddComponents(feature, grant);
            return pool;
        }

        internal static void Spend(BlueprintAbility ability, BlueprintAbilityResource pool)
        {
            var cost = new AbilityResourceLogic { Amount = 1 };
            SetField(cost, "m_IsSpendResource", true);
            SetField(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(pool.AssetGuid.ToString()));
            AddComponents(ability, cost);
        }

        static BlueprintAbility CopySpell(BlueprintAbility source, string key, BlueprintAbilityResource pool)
        {
            var a = Blueprints.Copy(source, key, c => {
                var type = c.GetType().Name;
                return type != "SpellListComponent" && type != "CraftInfoComponent" &&
                    !type.StartsWith("Recommendation") && !(c is AbilityVariants || c is ContextAbilityParamsCalculator);
            });
            a.Type = AbilityType.SpellLike;
            Spend(a, pool);
            return a;
        }

        internal static BlueprintFeature BuildSpell(string id, string key, int spellLevel)
        {
            var source = Get<BlueprintAbility>(id);
            var feature = Blueprints.Feature(key, source.Icon);
            var pool = Pool(key, feature, 1);
            var a = CopySpell(source, key + "Action", pool);
            if (id == Ids.HellfireRay)
            {
                RayPool = pool;
                var rays = a.GetComponent<AbilityDeliverProjectile>();
                rays.ReplaceAttackRollBonusStat = true;
                rays.AttackRollBonusStat = StatType.Charisma;
            }
            if (id == Ids.CommandGreater)
            {
                var options = source.GetComponent<AbilityVariants>().Variants.ToArray();
                if (options.Length != 4) throw new InvalidOperationException("Expected four greater commands");
                var cloned = options.Select(v => {
                    var leaf = CopySpell(v, "Order" + v.AssetGuid, pool);
                    SetField(leaf, "m_Parent", Ref<BlueprintAbilityReference>(a.AssetGuid.ToString()));
                    return Ref<BlueprintAbilityReference>(leaf.AssetGuid.ToString());
                }).ToArray();
                var variants = new AbilityVariants();
                SetField(variants, "m_Variants", cloned);
                AddComponents(a, variants);
            }
            AddComponents(feature, new SpellParameters { Ability = a, SpellLevel = spellLevel });
            Blueprints.Grant(feature, a);
            return feature;
        }

        internal static BlueprintFeature BuildBreath()
        {
            var original = Get<BlueprintAbility>(Ids.NightmareBreathWeapon);
            var a = Blueprints.Copy(original, "BreathAction", c => !(c is ContextAbilityParamsCalculator || c is ContextRankConfig));
            Blueprints.Name(a, "Breath",original.Icon);
            BreathCooldown = Blueprints.Make<BlueprintBuff>("BreathCooldown", b => {
                Blueprints.Name(b,"Cooldown", original.Icon);
                b.FxOnStart = new PrefabLink(); EmptyFxOnRemove(b);
            });
            var rank = new ContextRankConfig();
            SetField(rank, "m_BaseValueType", ContextRankBaseValueType.CasterLevel);
            SetField(rank, "m_Progression", ContextRankProgression.Div2);
            var apply = new ContextActionApplyBuff { ToCaster = true, DurationValue = new ContextDurationValue {
                Rate = DurationRate.Rounds, DiceType = DiceType.D4, DiceCountValue = 1, BonusValue = 0 } };
            SetField(apply,"m_Buff",Ref<BlueprintBuffReference>(BreathCooldown.AssetGuid.ToString()));
            apply.Owner = a; a.ElementsArray.Add(apply);
            AddComponents(a, rank, new BreathReady(), new AbilityExecuteActionOnCast {
                Conditions = new ConditionsChecker { Conditions = new Condition[0] }, Actions = new ActionList { Actions = new GameAction[] { apply } } });
            var petGrant = Blueprints.Feature("BreathTraining", original.Icon);
            AddComponents(petGrant, new SpellParameters { Ability = a, Mount = true });
            Blueprints.Grant(petGrant, a);
            var riderGrant = Blueprints.Feature("Breath", original.Icon);
            var share = new AddFeatureToPet();
            SetField(share,"m_PetType",Kingmaker.Enums.PetType.AnimalCompanion);
            SetField(share,"m_Feature",Ref<BlueprintFeatureReference>(petGrant.AssetGuid.ToString()));
            AddComponents(riderGrant, share);
            return riderGrant;
        }
    }

    public sealed class BreathReady : BlueprintComponent, IAbilityCasterRestriction, IAbilityVisibilityProvider
    {
        public bool IsAbilityVisible(AbilityData a) => a.Caster.Unit.Blueprint == Blueprints.Nightmare;
        public bool IsCasterRestrictionPassed(UnitEntityData caster) => caster.Blueprint == Blueprints.Nightmare &&
            Magic.Level(caster.Master) >= 8 && !caster.Buffs.HasFact(Magic.BreathCooldown);
        public string GetAbilityCasterRestrictionUIText() => Magic.BreathCooldown.Name;
    }

    // The game routes both tooltip and casting calculations through this rule. A class
    // feature owns only its own spell and (for Greater Command) that spell's four variants.
    public sealed class SpellParameters : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAbilityParams>
    {
        public BlueprintAbility Ability;
        public int SpellLevel;
        public bool Mount;
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt.Blueprint != Ability && evt.Spell?.Parent != Ability) return;
            int level = Magic.Level(Mount ? Owner.Master : Owner);
            evt.ReplaceCasterLevel = level;
            evt.ReplaceSpellLevel = Mount ? level / 2 : SpellLevel;
            evt.ReplaceStat = Mount ? StatType.Constitution : StatType.Charisma;
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
    }

    [HarmonyPatch(typeof(BlueprintAbilityResource), nameof(BlueprintAbilityResource.GetMaxAmount))]
    internal static class RayCapacity
    {
        static void Postfix(BlueprintAbilityResource __instance, UnitDescriptor unit, ref int __result)
        {
            if (__instance == Magic.RayPool && unit != null)
                __result += Rules.RayUses(unit.Stats.Charisma.Bonus) - 1;
        }
    }
}
