using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Utility;

namespace BulwarkMage
{
     
    // (이전 본편 BuffEnchantArmor 는 OnActivate @0025~0053 에서 Owner.Body.Items — 모든 손 세트의 칸, UnitBody.CollectAllSlots @001C~004B — 의 모든 방패에 붙였다.)
    // 반응하는 사건(본편 인터페이스, IL 확인 2026-11-14):
    //   IUnitEquipmentHandler.HandleEquipmentSlotUpdated(ItemSlot, ItemEntity previousItem) — 손 칸(HandSlot)이 바뀔 때. 떠난 방패(previousItem)도 판정에 넣어 거기서 뗀다.
    //   IUnitActiveEquipmentSetHandler.HandleUnitChangeActiveEquipmentSet(UnitDescriptor) — 손 세트 전환. (문 HasShieldFeatureUnlock 도 같은 두 사건으로 CheckEligibility 를 돈다.)
    //   OnActivate / OnDeactivate — 버프가 붙고 떨어질 때(문이 보조손 타워실드를 잃으면 버프가 떨어지고, 특성을 잃으면 문이 사라져 버프도 떨어진다).
    // 배치 판정 = 순수 BondPlacement.Decide(활성, 손 칸 방패들 + 떠난 방패): 대상 = 활성 보조손 타워실드(문과 같은 계산 — RestrictionsHelper.CheckShield @001C~0032
    //   SecondaryHand.Shield.ArmorComponent.ArmorType()), 대상엔 정확히 1개, 나머지엔 0개.
    // 저장 상태 없음: 「우리 것」 = 방패 방어구 구성요소의 마법부여 중 블루프린트가 BondEnchant{n} 인 것(엔진이 아이템 사실로 저장 — ItemEnchantment.m_CurrentContext [Json]).
    //   떼는 시점: 방패가 보조손을 떠날 때(칸 사건, 떠난 방패에서) · 세트를 바꿀 때(쉬는 세트 방패에서) · 버프가 떨어질 때(OnDeactivate, 유닛 손 칸의 모든 방패에서).
    //   손 칸을 거치지 않고 몸에서 사라진 방패(사건 없음)는 보지 못한다 — README 한계. 모드를 지운 세이브의 남은 마법부여도 README(최종 검토 m-1 와 같은 성질).
    // 예외 계약: 사건마다 try { MaybeFault("bond-placement"); … } catch → ReportException(엔진으로 던지지 않는다). 판정 불가(타워 여부를 못 읽음 등)는 붙이지 않고 ERROR.
    public class BondOffHandEnchant : UnitBuffComponentDelegate, IUnitEquipmentHandler, IUnitActiveEquipmentSetHandler
    {
        public BlueprintItemEnchantmentReference m_Enchantment;
        // The shield gate can deactivate the buff before its slot-change subscriber receives previousItem.
        static readonly ConditionalWeakTable<Buff, ItemEntityShield> lastShield = new ConditionalWeakTable<Buff, ItemEntityShield>();

        protected override void OnActivate() { Reconcile(true, null); }
        protected override void OnDeactivate() { Reconcile(false, null); }
        protected override void OnTurnOn()
        {
            var off = Owner?.Body?.SecondaryHand?.MaybeShield;
            if (TowerOf(off) == ShieldTowerKind.Tower && !lastShield.TryGetValue(Buff, out _))
                lastShield.Add(Buff, off);
        }

        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
        {
            if (slot == null || !(slot is HandSlot) || Owner == null || slot.Owner == null || !ReferenceEquals(slot.Owner.Unit, Owner)) return; // 다른 유닛·손이 아닌 칸 = 정당한 무동작(본편 BuffEnchantArmor @000D~0028 과 같은 유닛 비교)
            Reconcile(true, previousItem as ItemEntityShield);
        }

        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit)
        {
            if (unit == null || Owner == null || !ReferenceEquals(unit.Unit, Owner)) return; // 다른 유닛 = 정당한 무동작
            Reconcile(true, null);
        }

        internal static ShieldTowerKind TowerOf(ItemEntityShield s)
        {
            var armor = s == null ? null : s.ArmorComponent;
            if (armor == null || armor.Blueprint == null) return ShieldTowerKind.Unknown;
            return armor.ArmorType() == ArmorProficiencyGroup.TowerShield ? ShieldTowerKind.Tower : ShieldTowerKind.NotTower;
        }

        internal static int OursOn(ItemEntityShield s, BlueprintItemEnchantment ours)
        {
            var armor = s == null ? null : s.ArmorComponent;
            return armor == null ? 0 : armor.Enchantments.Count(e => ReferenceEquals(e.Blueprint, ours));
        }

        void Reconcile(bool active, ItemEntityShield left)
        {
            try
            {
                BulwarkErrors.MaybeFault(ErrorLedger.SiteBondPlacement);
                var owner = Owner;
                if (owner == null) { BulwarkErrors.Report(ErrorLedger.SiteBondPlacement, "bond enhancement component without an owner - nothing placed"); return; }
                var ours = m_Enchantment == null ? null : m_Enchantment.Get();
                if (ours == null) { BulwarkErrors.Report(ErrorLedger.SiteBondPlacement, "bond enhancement reference does not resolve - nothing placed"); return; }
                var body = owner.Body;
                var shields = new List<ItemEntityShield>();
                foreach (var set in body.HandsEquipmentSets)
                    foreach (var hand in new[] { set.PrimaryHand, set.SecondaryHand })
                    {
                        var s = hand == null ? null : hand.MaybeShield;
                        if (s != null && !shields.Contains(s)) shields.Add(s);
                    }
                if (left != null && !shields.Contains(left)) shields.Add(left);
                ItemEntityShield previous;
                lastShield.TryGetValue(Buff, out previous);
                if (previous != null && !previous.IsDisposed && !shields.Contains(previous)) shields.Add(previous);
                var off = body.SecondaryHand == null ? null : body.SecondaryHand.MaybeShield;
                var target = active && TowerOf(off) == ShieldTowerKind.Tower ? off : null;
                if (!ReferenceEquals(previous, target))
                {
                    lastShield.Remove(Buff);
                    if (target != null) lastShield.Add(Buff, target);
                }
                var views = shields.Select((s, i) => new ShieldView("shield" + i + ":" + s.Blueprint.name, ReferenceEquals(s, off), TowerOf(s), OursOn(s, ours))).ToList();
                var plan = BondPlacement.Decide(active, views);
                foreach (var p in plan.Problems) BulwarkErrors.Report(ErrorLedger.SiteBondPlacement, owner.CharacterName + ": " + p);
                foreach (var ch in plan.Changes)
                {
                    var s = shields[views.FindIndex(v => v.Id == ch.Key)];
                    var armor = s.ArmorComponent;
                    if (armor == null) { BulwarkErrors.Report(ErrorLedger.SiteBondPlacement, owner.CharacterName + ": shield " + ch.Key + " has no armor component - cannot place"); continue; }
                    if (ch.Value > 0) armor.AddEnchantment(ours, Context, (Rounds?)null);
                    else foreach (var e in armor.Enchantments.Where(x => ReferenceEquals(x.Blueprint, ours)).Take(-ch.Value).ToList()) armor.RemoveEnchantment(e);
                }
            }
            catch (Exception e)
            {
                BulwarkErrors.ReportException(ErrorLedger.SiteBondPlacement, e);
            }
        }
    }
}
