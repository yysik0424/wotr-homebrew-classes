using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.DLC;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using WeaponSpiritShaman;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spiritsteel
{
    internal static partial class BoundWeapons
    {
        internal sealed class Entry
        {
            internal BlueprintFeature Feature;
            internal BlueprintItemWeapon Head;
            internal BlueprintItem Item;
        }

        static readonly Dictionary<WeaponCategory, Entry> entries = new Dictionary<WeaponCategory, Entry>();
        static BlueprintCharacterClass characterClass;
        static BlueprintWeaponEnchantment[] enhancements;
        static readonly BoundRespecHandler respecHandler = new BoundRespecHandler();

        internal static BlueprintFeatureSelection Build(BlueprintCharacterClass barbarian)
        {
            characterClass = barbarian;
            enhancements = BoundRules.Enhancements.Select(Get<BlueprintWeaponEnchantment>).ToArray();
            var options = new List<BlueprintFeatureReference>();
            foreach (var row in SpiritCategories.Rows.Where(r => SpiritCategories.IsAvailable(r,
                id => ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(id)) != null)))
            {
                var category = (WeaponCategory)Enum.Parse(typeof(WeaponCategory), row.Category);
                var source = Get<BlueprintItemWeapon>(row.ItemGuid);
                if (source.Type.Category != category || source.Double != row.Double)
                    throw new InvalidOperationException("Spiritsteel: unexpected weapon type for " + row.Category);
                var second = row.Double ? CopyHead(Get<BlueprintItemWeapon>(row.SecondGuid), row.Category, "second", null) : null;
                var head = CopyHead(source, row.Category, "weapon", second);
                BoundTexts.Describe(row.Category, head, second);
                string shield = BoundRules.ShieldSource(row.Category);
                BlueprintItem item = head;
                if (shield != null) item = CopyShield(Get<BlueprintItemShield>(shield), row.Category, head);
                var choice = Create<BlueprintFeature>("SpiritsteelBondChoice" + row.Category, BoundRules.Id("choice", row.Category), x =>
                {
                    SetField(x, "m_DisplayName", source.Type.TypeName);
                    SetField(x, "m_Description", Strings.L(BoundTexts.Key(row.Category)));
                    SetField(x, "m_DescriptionShort", Strings.L(BoundTexts.Key(row.Category)));
                    SetField(x, "m_Icon", source.Type.Icon);
                    x.IsClassFeature = true;
                    x.Ranks = 1;
                    x.Groups = new FeatureGroup[0];
                });
                AddComponents(choice, new AddProficiencies { WeaponProficiencies = new[] { category }, ArmorProficiencies = new ArmorProficiencyGroup[0] },
                    new BoundChoice { Category = category });
                entries.Add(category, new Entry { Head = head, Item = item, Feature = choice });
                options.Add(Ref<BlueprintFeatureReference>(choice.AssetGuid.ToString()));
            }
            var selection = Create<BlueprintFeatureSelection>("SpiritsteelBondSelection", BoundRules.Id("selection", "all"), x =>
            {
                SetField(x, "m_DisplayName", Strings.L("bound.name"));
                SetField(x, "m_Description", Strings.L("bound.desc"));
                SetField(x, "m_DescriptionShort", Strings.L("bound.desc"));
                SetField(x, "m_Icon", entries[WeaponCategory.Longsword].Head.Icon);
                SetField(x, "m_Features", options.ToArray());
                SetField(x, "m_AllFeatures", options.ToArray());
                x.Groups = new FeatureGroup[0];
                x.IsClassFeature = true;
                x.Ranks = 1;
            });
            EventBus.Subscribe(respecHandler);
            return selection;
        }

        static BlueprintItemWeapon CopyHead(BlueprintItemWeapon source, string category, string kind, BlueprintItemWeapon second)
        {
            var head = Create<BlueprintItemWeapon>("SpiritsteelBond" + kind + category, BoundRules.Id(kind, category), x =>
            {
                CloneKit.CopyInto(source, x);
                x.ComponentsArray = new BlueprintComponent[0];
                SetField(x, "m_Enchantments", new BlueprintWeaponEnchantmentReference[0]);
                SetField(x, "m_VisualParameters", CloneKit.Copy(source.VisualParameters));
                if (second != null) SetField(x, "m_SecondWeapon", Ref<BlueprintItemWeaponReference>(second.AssetGuid.ToString()));
                ConfigureItem(x, source, category);
            });
            foreach (var condition in (source.ComponentsArray ?? new BlueprintComponent[0]).OfType<DlcCondition>())
                AddComponents(head, CloneKit.Copy(condition));
            if (!ReferenceEquals(head.SecondWeapon, second) || head.Type != source.Type || ReferenceEquals(source.VisualParameters, head.VisualParameters))
                throw new InvalidOperationException("Spiritsteel: bound weapon readback failed for " + head.name);
            return head;
        }

        static BlueprintItemShield CopyShield(BlueprintItemShield source, string category, BlueprintItemWeapon head)
        {
            var shield = Create<BlueprintItemShield>("SpiritsteelBondShield" + category, BoundRules.Id("shield", category), x =>
            {
                CloneKit.CopyInto(source, x);
                x.ComponentsArray = new BlueprintComponent[0];
                SetField(x, "m_WeaponComponent", Ref<BlueprintItemWeaponReference>(head.AssetGuid.ToString()));
                if (source.VisualParameters != null) SetField(x, "m_VisualParameters", CloneKit.Copy(source.VisualParameters));
                ConfigureItem(x, head, category);
            });
            if (shield.WeaponComponent != head || shield.ArmorComponent != source.ArmorComponent)
                throw new InvalidOperationException("Spiritsteel: shield readback failed for " + category);
            return shield;
        }

        static void ConfigureItem(BlueprintItem item, BlueprintItemWeapon source, string category)
        {
            SetField(item, "m_DisplayNameText", source.Type.TypeName);
            SetField(item, "m_DescriptionText", Strings.L(BoundTexts.Key(category)));
            SetField(item, "m_Cost", 0);
            SetField(item, "m_IsNotable", true);
        }

        internal static WeaponCategory? SelectedCategory(UnitEntityData owner)
        {
            if (owner != null)
                foreach (var pair in entries)
                    if (owner.Descriptor.HasFact(pair.Value.Feature)) return pair.Key;
            return null;
        }

        internal static BlueprintItemWeapon BaseWeapon(WeaponCategory category)
        {
            Entry entry;
            return entries.TryGetValue(category, out entry) ? entry.Head : null;
        }
    }
}
