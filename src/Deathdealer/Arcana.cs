using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Deathdealer
{
    public sealed class LearnedArcana : BlueprintComponent, IAbilityVisibilityProvider, IAbilityCasterRestriction
    {
        public BlueprintFeature Learning;
        public bool IsAbilityVisible(AbilityData ability) => IsCasterRestrictionPassed(ability.Caster.Unit);
        public bool IsCasterRestrictionPassed(UnitEntityData caster) => caster.Descriptor.HasFact(Learning);
        public string GetAbilityCasterRestrictionUIText() => Learning.Name;
    }
    internal static class Arcana
    {
        internal static BlueprintAbility Menu, Genie;
        internal static BlueprintFeature[] Build()
        {
            var source = Get<BlueprintAbility>(Rules.Spells[0]);
            Menu = Blueprints.Make<BlueprintAbility>("Menu", a => {
                Blueprints.Name(a,"Menu",source.Icon); Personal(a); a.LocalizedDuration = a.LocalizedSavingThrow = NoText();
            });
            var options = new List<BlueprintAbility>();
            var training = new List<BlueprintFeature>();
            for (int tier=1; tier<=Rules.Spells.Length; tier++)
            {
                source = Get<BlueprintAbility>(Rules.Spells[tier-1]);
                string key = "Tier"+tier;
                var feature = Blueprints.Feature(key,source.Icon);
                training.Add(feature);
                if (tier == 1) Blueprints.Grant(feature,Menu);
                var pool = Blueprints.Make<BlueprintAbilityResource>("Daily"+tier,r => {
                    r.LocalizedName = Strings.L(key+".name"); r.LocalizedDescription = Strings.L(key+".desc");
                    var amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource),"m_MaxAmount").FieldType);
                    SetField(amount,"BaseValue",1); SetField(r,"m_MaxAmount",amount); SetField(r,"m_Icon",source.Icon);
                });
                var grant = new AddAbilityResources { RestoreAmount = true };
                SetField(grant,"m_Resource",Ref<BlueprintAbilityResourceReference>(pool.AssetGuid.ToString()));
                AddComponents(feature,grant);
                var option = Option(source,key+"Ability",key,feature,pool,Menu,tier != Rules.GenieTier);
                options.Add(option);
                if (tier == Rules.GenieTier)
                {
                    Genie = option;
                    var shapes = source.GetComponent<AbilityVariants>().Variants.ToArray();
                    if (shapes.Length != 4) throw new InvalidOperationException("Expected four native Geniekind forms.");
                    Variants(option, shapes.Select(s=>Option(s,"Form"+s.AssetGuid,null,feature,pool,option,true)));
                }
            }
            Variants(Menu,options);
            return training.ToArray();
        }
        static void Personal(BlueprintAbility a)
        {
            a.Type = AbilityType.SpellLike; a.Range = AbilityRange.Personal; a.ActionType = UnitCommand.CommandType.Standard;
            a.CanTargetSelf = true; a.CanTargetFriends = a.CanTargetEnemies = a.CanTargetPoint = false;
            a.SpellResistance = false;
        }
        static BlueprintAbility Option(BlueprintAbility source,string id,string key,BlueprintFeature training,
            BlueprintAbilityResource pool,BlueprintAbility parent,bool castable)
        {
            var a = Blueprints.Make<BlueprintAbility>(id,b => {
                Personal(b);
                if (key != null) Blueprints.Name(b,key,source.Icon);
                else
                {
                    foreach (string field in new[]{"m_DisplayName","m_Description"})
                        SetField(b,field,FindField(typeof(BlueprintUnitFact),field).GetValue(source));
                    SetField(b,"m_DescriptionShort",NoText()); SetField(b,"m_Icon",source.Icon);
                }
                b.Animation = source.Animation;
                b.LocalizedDuration = source.LocalizedDuration ?? NoText(); b.LocalizedSavingThrow = source.LocalizedSavingThrow ?? NoText();
                SetField(b,"m_Parent",Ref<BlueprintAbilityReference>(parent.AssetGuid.ToString()));
            });
            AddComponents(a,new LearnedArcana {Learning=training});
            // The submenu displays the shared pool. Only a leaf can execute a cast.
            var cost = new AbilityResourceLogic { Amount = 1 };
            SetField(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(pool.AssetGuid.ToString()));
            SetField(cost,"m_IsSpendResource",true);
            AddComponents(a,cost);
            if (!castable) return a;
            var cl = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Intelligence };
            SetField(cl,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(Rules.Slayer));
            AddComponents(a,cl);
            // Copy native effect graphs including the original duration ranks, buffs and FX.
            // Target selection, area distribution, material costs and spell-list membership belong to the source spell.
            foreach (var c in source.ComponentsArray.Where(Effect))
            {
                var clone = (BlueprintComponent)DreamThief.CloneKit.CopyOf(c,x=>x is SimpleBlueprint || x is UnityEngine.Object,48);
                AddComponents(a,clone);
                Bind(clone,a,new HashSet<object>());
            }
            if (a.GetComponent<AbilityEffectRunAction>() == null || a.GetComponent<AbilityTargetsAround>() != null)
                throw new InvalidOperationException("Invalid personal spell effect on "+a.name);
            return a;
        }
        static bool Effect(BlueprintComponent c)
        {
            switch (c.GetType().Name)
            {
                case "AbilityEffectRunAction": case "AbilityExecuteActionOnCast": case "ContextRankConfig":
                case "AbilitySpawnFx": case "SpellDescriptorComponent": case "AbilityTargetHasFact": return true;
                default: return false;
            }
        }
        static void Bind(object item, BlueprintAbility owner, HashSet<object> seen)
        {
            if (item == null || !seen.Add(item)) return;
            if (item is Element e) { e.Owner = owner; owner.ElementsArray.Add(e); }
            if (item is IEnumerable list && !(item is string)) { foreach (var child in list) Bind(child,owner,seen); return; }
            if (!(item is BlueprintComponent || item is Element || item is ActionList || item is ConditionsChecker)) return;
            for (var t=item.GetType();t!=typeof(object);t=t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                    Bind(field.GetValue(item),owner,seen);
        }
        static void Variants(BlueprintAbility parent,IEnumerable<BlueprintAbility> children)
        {
            var v = new AbilityVariants();
            SetField(v,"m_Variants",children.Select(c=>Ref<BlueprintAbilityReference>(c.AssetGuid.ToString())).ToArray());
            AddComponents(parent,v);
        }
    }
}
