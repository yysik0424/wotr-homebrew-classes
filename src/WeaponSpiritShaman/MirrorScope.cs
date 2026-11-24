using System;

namespace WeaponSpiritShaman
{
    // [순수] 흡수 거울의 범위·소유·대상 판정(리뷰 T4 I1·m2). 게임 쪽 GameMirrorEffects 는 이 함수들의 결과만 따른다(시험이 IL 로 고정, 판정은 여기서 실행 시험).
    //
    // 아이템이 놓인 자리(게임 쪽이 관측해서 넘긴다). 「소유 유닛」 = 그 효과기가 다루는 유닛 —
     
    //   OwnerHand        소유 유닛의 손 칸(세트 × 주·보조) 중 하나에 든 아이템
    //   OwnerEmptyHand   소유 유닛의 맨손 무기(UnitBody.EmptyHandWeapon) — 활성 세트가 비면 대상(설계 §4.4)
    //   UnheldInventory  효과기가 보는 인벤토리(거울 = 주인·영병, 강신 = 주술사)에 있고 어느 칸에도 들려 있지 않은 아이템(HoldingSlot == null)
    //   OtherHolder      그 인벤토리 컬렉션에 있지만 소유 유닛이 아닌 다른 유닛의 칸에 들려 있는 아이템
    //                    (거울: 주술사·다른 영병·다른 파티원, 강신: 영병·다른 파티원)
    public enum ItemHolder { OwnerHand, OwnerEmptyHand, UnheldInventory, OtherHolder }

    // 활성 세트의 한 손 칸 관측: 비었음 / 무기 / 무기 아닌 것(방패 등 — ItemEntityShield 는 무기 마법 부여를 본체에 실체화하지 않는다,
    // ItemEntityShield.get_InstantiateEnchantments = ldc.i4.0, 무기 마법 부여는 WeaponComponent 쪽 — 리뷰 T4 m2).
    public enum HandContent { Empty, Weapon, NonWeapon }

    public static class MirrorScope
    {
        // 놓인 자리 판정. 우선순위: 소유 유닛 손 칸 > 맨손 무기 > (칸에 들림 → 다른 유닛 손) > 들리지 않은 인벤토리.
        // 소유 유닛 손 칸에 든 아이템도 공유 인벤토리 컬렉션에 들어 있을 수 있으므로 손 칸을 먼저 본다.
        // 어느 출처에도 없는 아이템(셋 다 false 이고 인벤토리에도 없음)은 판정 대상이 아니다 → throw.
        public static ItemHolder Classify(bool inOwnerHandSlot, bool isOwnerEmptyHand, bool inInventory, bool heldBySomeSlot)
        {
            if (inOwnerHandSlot) return ItemHolder.OwnerHand;
            if (isOwnerEmptyHand) return ItemHolder.OwnerEmptyHand;
            if (!inInventory) throw new ArgumentException("MirrorScope.Classify: item is neither in the owner unit's hands, its empty hand, nor an inventory");
            return heldBySomeSlot ? ItemHolder.OtherHolder : ItemHolder.UnheldInventory;
        }

        // 우리 인스턴스를 「찾아 지우는」 범위(MirrorRun ② 고아 정리·⑤ 사후 조건) = 소유 유닛이 실제로 쥔 것뿐.
        // 인벤토리·다른 유닛 손은 넣지 않는다: 다른 유닛 손이면 다른 영병의 정상 인스턴스를 고아로 지운다(스피릿 웨폰 샤먼 둘 파티).
        public static bool ListsOurInstances(ItemHolder h)
        {
            switch (h)
            {
                case ItemHolder.OwnerHand:
                case ItemHolder.OwnerEmptyHand: return true;
                case ItemHolder.UnheldInventory:
                case ItemHolder.OtherHolder: return false;
                default: throw new ArgumentException("MirrorScope: undefined ItemHolder " + (int)h);
            }
        }

        // 원장 기록의 아이템을 「찾는」 범위(MirrorRun ① 기록 대조) = 소유 유닛 손·맨손 + 들리지 않은 인벤토리(벗겨져 주인 가방으로 간 아이템).
        // 다른 유닛 손에 든 아이템은 이 범위에서 찾지 않는다(그 유닛의 것). 다만 원장 기록 사실 id 로만 확인·제거하는 별도 범위가 있다(HeldByOther, 항목 m-6).
        public static bool Searchable(ItemHolder h)
        {
            switch (h)
            {
                case ItemHolder.OwnerHand:
                case ItemHolder.OwnerEmptyHand:
                case ItemHolder.UnheldInventory: return true;
                case ItemHolder.OtherHolder: return false;
                default: throw new ArgumentException("MirrorScope: undefined ItemHolder " + (int)h);
            }
        }

         
        // 찾기 범위(Searchable)와 겹치지 않는다 — 기록 아이템이 찾기 범위에 없을 때만 여기를 본다.
        public static bool HeldByOther(ItemHolder h)
        {
            switch (h)
            {
                case ItemHolder.OtherHolder: return true;
                case ItemHolder.OwnerHand:
                case ItemHolder.OwnerEmptyHand:
                case ItemHolder.UnheldInventory: return false;
                default: throw new ArgumentException("MirrorScope: undefined ItemHolder " + (int)h);
            }
        }

        // 우리 인스턴스 = 부모 문맥의 AssociatedBlueprint 가 흡수 사실(Guids.SpiritMirrorFeature)인 마법 부여.
        // 부모 문맥이 없거나(null) 다른 블루프린트면 우리 것이 아니다 — 우리 Add 는 항상 흡수 사실 문맥을 달고 되읽기로 단언한다(GameMirrorEffects.Add).
        public static bool IsOurs(string parentContextBlueprintGuid)
        {
            return parentContextBlueprintGuid == Guids.SpiritMirrorFeature;
        }

        // 거울 대상(설계 §4.5 「대상 = 활성 세트 주무기. 주무기가 없으면 활성 세트 보조 무기, 둘 다 없으면 펫 맨손 무기」). slot: 0 = 주, 1 = 보조, -1 = 맨손.
         
        //   그 밖에 무기 아닌 것이 손에 있으면 NonWeapon(비추지 않음, 리뷰 T4 m2) — 주에 있으면 칸 0, 주가 비고 보조에만 있으면 칸 1.
         
        public static TargetKind ChooseTarget(HandContent primary, HandContent secondary, out int slot)
        {
            if (!Enum.IsDefined(typeof(HandContent), primary) || !Enum.IsDefined(typeof(HandContent), secondary)) throw new ArgumentException("MirrorScope.ChooseTarget: undefined HandContent");
            if (primary == HandContent.Weapon) { slot = 0; return TargetKind.Primary; }
            if (secondary == HandContent.Weapon) { slot = 1; return TargetKind.Secondary; }
            if (primary == HandContent.NonWeapon) { slot = 0; return TargetKind.NonWeapon; }
            if (secondary == HandContent.NonWeapon) { slot = 1; return TargetKind.NonWeapon; }
            slot = -1;
            return TargetKind.EmptyHand;
        }
    }
}
