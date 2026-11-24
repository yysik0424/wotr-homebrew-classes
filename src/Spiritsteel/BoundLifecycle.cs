using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Newtonsoft.Json;

namespace Spiritsteel
{
    public sealed class BoundWeaponPart : UnitPart, IUnitPartSurviveRespec, IAreaHandler, IGlobalSubscriber
    {
        [JsonProperty] internal EntityRef<ItemEntity> Relic;
        [JsonProperty] internal string RelicCategory;
        [JsonProperty] internal List<EntityRef<ItemEntity>> Retired = new List<EntityRef<ItemEntity>>();
        [JsonProperty] internal string MainEnhancement, SecondEnhancement;
        [JsonIgnore] internal bool Synchronizing;
        [JsonIgnore] internal string ReportedMissingId;

        public bool HasRelicId { get { return !string.IsNullOrEmpty(Relic.GetId()); } }
        public bool ShouldSurviveRespec { get { return HasRelicId || Retired.Count > 0; } }
        public void CopyAfterRespec(UnitEntityData unit)
        {
            var target = unit.Ensure<BoundWeaponPart>();
            target.Relic = Relic;
            target.RelicCategory = RelicCategory;
            target.Retired = new List<EntityRef<ItemEntity>>(Retired);
            target.MainEnhancement = MainEnhancement;
            target.SecondEnhancement = SecondEnhancement;
        }
        public void OnAreaBeginUnloading() { }
        public void OnAreaDidLoad()
        {
            if (!Owner.PreviewOf.IsEmpty) return;
            BoundWeapons.CleanRetired(this);
            BoundWeapons.Refresh(Owner);
        }
    }

    public sealed class BoundChoice : UnitFactComponentDelegate, IUnitReapplyFeaturesOnLevelUpHandler
    {
        public WeaponCategory Category;
        protected override void OnActivate() { BoundWeapons.Refresh(Owner, Category); }
        protected override void OnPostLoad() { BoundWeapons.Refresh(Owner, Category); }
        public void HandleUnitReapplyFeaturesOnLevelUp() { BoundWeapons.Refresh(Owner, Category); }
        protected override void OnDeactivate() { BoundWeapons.OnRemove(Owner, Category); }
    }

    internal sealed class BoundRespecHandler : IUnitChangedAfterRespecHandler, IUnitLevelUpHandler, IGlobalSubscriber
    {
        public void HandleUnitBeforeLevelUp(UnitEntityData unit) { }
        public void HandleUnitAfterLevelUp(UnitEntityData unit, LevelUpController controller) { BoundWeapons.Refresh(unit); }
        public void HandleUnitChangedAfterRespec(UnitEntityData unit) { BoundWeapons.AfterRespec(unit); }
    }

    internal static partial class BoundWeapons
    {
        static ItemEntity Physical(UnitEntityData owner)
        {
            var part = owner?.Get<BoundWeaponPart>();
            if (part == null || !part.HasRelicId) return null;
            var item = part.Relic.Entity;
            if (item != null && !item.IsDisposed) return item;
            return owner.Inventory.Items.FirstOrDefault(i => i.UniqueId == part.Relic.GetId() && !i.IsDisposed);
        }

        static ItemEntityWeapon Head(ItemEntity item)
        {
            var shield = item as ItemEntityShield;
            return shield == null ? item as ItemEntityWeapon : shield.WeaponComponent;
        }

        internal static ItemEntityWeapon Item(UnitEntityData owner) { return Head(Physical(owner)); }

        internal static bool IsBound(UnitEntityData owner, ItemEntityWeapon weapon)
        {
            if (weapon == null) return false;
            var head = Item(owner);
            return head != null && (ReferenceEquals(head, weapon) || ReferenceEquals(head.Second, weapon));
        }

        internal static bool IsEquipped(UnitEntityData owner)
        {
            var item = Physical(owner);
            return item != null && (ReferenceEquals(owner.Body.PrimaryHand.MaybeItem, item)
                || ReferenceEquals(owner.Body.SecondaryHand.MaybeItem, item));
        }

        internal static void Refresh(UnitEntityData owner)
        {
            if (owner == null || !owner.PreviewOf.IsEmpty || ContextData<UnitEntityData.RespecCommit>.Current) return;
            var selected = SelectedCategory(owner);
            if (selected.HasValue) Refresh(owner, selected.Value);
        }

        internal static void Refresh(UnitEntityData owner, WeaponCategory category)
        {
            if (owner == null || !owner.PreviewOf.IsEmpty || ContextData<UnitEntityData.RespecCommit>.Current) return;
            Synchronize(owner, entries[category]);
        }

        internal static void AfterRespec(UnitEntityData owner)
        {
            if (owner == null || !owner.PreviewOf.IsEmpty) return;
            var selected = SelectedCategory(owner);
            if (selected.HasValue) Synchronize(owner, entries[selected.Value]);
            else if (owner.Get<BoundWeaponPart>() != null) Retire(owner.Get<BoundWeaponPart>());
        }

        internal static void OnRemove(UnitEntityData owner, WeaponCategory category)
        {
            if (owner == null || !owner.PreviewOf.IsEmpty || ContextData<UnitEntityData.RespecCommit>.Current) return;
            var part = owner.Get<BoundWeaponPart>();
            if (part != null && (part.RelicCategory == category.ToString() || Physical(owner)?.Blueprint == entries[category].Item))
                Retire(part);
        }

        static void Synchronize(UnitEntityData owner, Entry entry)
        {
            var rank = BoundRules.EnhancementRank(owner.Progression.GetClassLevel(characterClass));
            if (rank == 0) return;
            var part = owner.Ensure<BoundWeaponPart>();
            if (part.Synchronizing) return;
            part.Synchronizing = true;
            try
            {
                var item = Physical(owner);
                // During load another area's container may not have registered its entities yet.
                // An existing saved ID always takes precedence over minting a replacement.
                string category = entry.Head.Category.ToString();
                if (part.HasRelicId && (item != null ? item.Blueprint != entry.Item : part.RelicCategory != null && part.RelicCategory != category))
                {
                    Retire(part);
                    item = null;
                }
                if (item == null && part.HasRelicId)
                {
                    if (part.ReportedMissingId != part.Relic.GetId())
                    {
                        part.ReportedMissingId = part.Relic.GetId();
                        WotrClassMod.Common.ModContext.Log("Spiritsteel: bound item " + part.ReportedMissingId +
                            " is not loaded for " + owner.UniqueId + "; retaining the saved bond and retrying on area load.");
                    }
                    return;
                }
                part.ReportedMissingId = null;
                part.RelicCategory = category;
                bool isNew = item == null;
                if (isNew)
                {
                    item = ItemsEntityFactory.CreateEntity(entry.Item);
                    item.ForceStackable = false;
                    item.Identify();
                    part.Relic = item;
                    owner.Inventory.Add(item);
                }
                item.ForceStackable = false;
                var head = Head(item);
                if (head == null || head.Blueprint != entry.Head) throw new InvalidOperationException("Spiritsteel: missing bound weapon head on " + item.UniqueId);
                var enchant = enhancements[rank - 1];
                Enhance(head, enchant, ref part.MainEnhancement);
                if (head.Second != null) Enhance(head.Second, enchant, ref part.SecondEnhancement);
                WeaponFeats.Synchronize(owner, head, head.Second);
                if (isNew) EquipIfEmpty(owner, item, head);
            }
            finally { part.Synchronizing = false; }
        }

        static void Enhance(ItemEntityWeapon item, BlueprintWeaponEnchantment enchant, ref string trackedId)
        {
            string id = trackedId;
            var old = id == null ? null : item.Enchantments.FirstOrDefault(e => e.UniqueId == id);
            if (old != null && old.Blueprint == enchant) return;
            var applied = item.AddEnchantment(enchant, null, null);
            if (applied == null) throw new InvalidOperationException("Spiritsteel: failed to enchant bound weapon " + item.UniqueId);
            trackedId = applied.UniqueId;
            if (old != null) item.RemoveEnchantment(old);
        }

        static void EquipIfEmpty(UnitEntityData owner, ItemEntity item, ItemEntityWeapon head)
        {
            bool shield = item is ItemEntityShield;
            var slot = shield ? owner.Body.SecondaryHand : owner.Body.PrimaryHand;
            if (slot.HasItem || (!shield && (head.Blueprint.IsTwoHanded || head.Blueprint.Double) && owner.Body.SecondaryHand.HasItem)) return;
            if (shield && owner.Body.PrimaryHand.MaybeWeapon?.HoldInTwoHands == true) return;
            if (slot.CanInsertItem(item, false)) slot.InsertItem(item);
        }

        static void Retire(BoundWeaponPart part)
        {
            if (part.HasRelicId) part.Retired.Add(part.Relic);
            part.Relic = default(EntityRef<ItemEntity>);
            part.RelicCategory = null;
            part.MainEnhancement = null;
            part.SecondEnhancement = null;
            CleanRetired(part);
        }

        // A respec can select a different category while the old weapon is in an unloaded stash.
        // Defer only removal of that exact instance. The new choice can still receive its weapon.
        internal static void CleanRetired(BoundWeaponPart part)
        {
            for (int i = part.Retired.Count - 1; i >= 0; --i)
            {
                var item = part.Retired[i].Entity;
                if (item == null) continue;
                if (!item.IsDisposed)
                {
                    if (item.HoldingSlot != null && !item.HoldingSlot.RemoveItem(true)) continue;
                    item.Collection?.Remove(item);
                    if (!item.IsDisposed) item.Dispose();
                }
                part.Retired.RemoveAt(i);
            }
        }
    }
}
