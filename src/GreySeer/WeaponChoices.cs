using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.DLC;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using WeaponSpiritShaman;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace GreySeer
{
    internal static class WeaponChoices
    {
        static readonly Dictionary<WeaponCategory, BlueprintFeature> choices = new Dictionary<WeaponCategory, BlueprintFeature>();
        static BlueprintFeature legacyLongsword;

        internal static string ChoiceId(WeaponCategory category)
        {
            using (var md5 = MD5.Create())
                return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes("GreySeer.StaffWeapon." + category))).ToString("N");
        }

        internal static BlueprintFeatureSelection Build(UnityEngine.Sprite icon)
        {
            legacyLongsword = Get<BlueprintFeature>(Guids.Longsword);
            var options = new List<BlueprintFeatureReference>();
            foreach (var row in SpiritCategories.Rows)
            {
                if (!SpiritCategories.IsAvailable(row, id => ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(id)) != null)) continue;
                var item = Get<BlueprintItemWeapon>(row.ItemGuid);
                if (!item.IsMelee || item.IsTwoHanded || item.Double || item.Category == WeaponCategory.Quarterstaff ||
                    item.Category == WeaponCategory.SpikedLightShield || item.Category == WeaponCategory.SpikedHeavyShield) continue;
                if (item.Category.ToString() != row.Category)
                    throw new InvalidOperationException("Grey Seer weapon category mismatch: " + row.Category);

                var category = item.Category;
                var choice = Create<BlueprintFeature>("GreySeerStaffWeapon" + row.Category, ChoiceId(category), f => {
                    SetField(f, "m_DisplayName", item.Type.TypeName);
                    SetField(f, "m_Description", Strings.L("choice.desc"));
                    SetField(f, "m_DescriptionShort", NoText());
                    SetField(f, "m_Icon", item.Icon ?? icon);
                    f.IsClassFeature = true;
                    f.Ranks = 1;
                });
                AddComponents(choice, new AddProficiencies {
                    WeaponProficiencies = new[] { category }, ArmorProficiencies = new ArmorProficiencyGroup[0]
                });
                foreach (var condition in item.ComponentsArray.OfType<DlcCondition>())
                {
                    var gate = new DlcCondition();
                    SetField(gate, "m_DlcReward", Ref<BlueprintDlcRewardReference>(condition.DlcReward.AssetGuid.ToString()));
                    SetField(gate, "m_HideInstead", condition.HideInstead);
                    AddComponents(choice, gate);
                }
                choices.Add(category, choice);
                options.Add(Ref<BlueprintFeatureReference>(choice.AssetGuid.ToString()));
            }
            if (!choices.ContainsKey(WeaponCategory.Longsword) || !choices.ContainsKey(WeaponCategory.Rapier))
                throw new InvalidOperationException("Grey Seer: missing core melee weapon choices.");
            return Create<BlueprintFeatureSelection>("GreySeerStaffWeaponSelection", Guids.WeaponSelection, f => {
                SetField(f, "m_DisplayName", Strings.L("choice.name"));
                SetField(f, "m_Description", Strings.L("choice.desc"));
                SetField(f, "m_DescriptionShort", NoText());
                SetField(f, "m_Icon", icon);
                SetField(f, "m_Features", options.ToArray());
                SetField(f, "m_AllFeatures", options.ToArray());
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
        }

        internal static bool Matches(UnitEntityData unit, ItemEntityWeapon weapon)
        {
            if (weapon == null || !weapon.Blueprint.IsMelee || weapon.Blueprint.IsTwoHanded || weapon.Blueprint.Double) return false;
            var category = weapon.Blueprint.Category;
            BlueprintFeature choice;
            if (!choices.TryGetValue(category, out choice)) return false;
            if (unit.Descriptor.HasFact(choice)) return true;
            // Earlier characters received the native longsword proficiency instead of this selection.
            return category == WeaponCategory.Longsword && unit.Descriptor.HasFact(legacyLongsword) &&
                !choices.Values.Any(feature => unit.Descriptor.HasFact(feature));
        }
    }
}
