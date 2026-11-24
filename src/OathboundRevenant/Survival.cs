using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static OathboundRevenant.Parts;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    public static partial class Survival
    {
        // 하루 자원(BlueprintAbilityResource.m_MaxAmount.BaseValue = daily, 능력치·레벨 가산 없음). Bulwark Rebuke.cs:110-121 의 모양.
        internal static BlueprintAbilityResource NewDailyResource(string name, string guid, string nameKey, string descKey, int daily)
        {
            if (daily < 1) throw new ArgumentOutOfRangeException("daily", daily, "OathboundRevenant: daily resource must be >= 1");
            var r = Create<BlueprintAbilityResource>(name, guid, x =>
            {
                x.LocalizedName = Strings.L(nameKey);
                x.LocalizedDescription = Strings.L(descKey);
                object amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                FillEmpty(amount);
                SetField(amount, "BaseValue", daily);
                SetField(amount, "IncreasedByStat", false);
                SetField(x, "m_MaxAmount", amount);
                SetField(x, "m_UseMax", false);
                FillEmpty(x);
            });
            return r;
        }

        // 재주 특성 = AddFacts[능력] + AddAbilityResources{자원, RestoreAmount}(계획 「자원 배선」 — 자원마다 부여 특성).
        internal static void Grant(BlueprintFeature feature, BlueprintAbility ability, string resourceGuid)
        {
            var facts = new AddFacts();
            SetField(facts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(ability.AssetGuid.ToString()) });
            var res = Comp(new AddAbilityResources { RestoreAmount = true, UseThisAsResource = false, RestoreOnLevelUp = false });
            SetField(res, "m_Resource", Ref<BlueprintAbilityResourceReference>(resourceGuid));
            AddComponents(feature, facts, res);
        }
    }
}
