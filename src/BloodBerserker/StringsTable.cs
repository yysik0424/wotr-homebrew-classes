using System.Collections.Generic;
namespace BloodBerserker
{
    internal static class StringsTable
    {
        internal static readonly Dictionary<string, (string ko, string en)> Table = new Dictionary<string, (string ko, string en)>
        {
            { "archetype.name", ("블러드 버서커", "Blood Berserker") },
            { "archetype.short", ("핏빛 사슬로 적과의 거리를 좁히고 피를 입힌 무기로 싸우는 블러드레이저.", "A bloodrager who closes the distance with a chain of blood and coats melee weapons in blood.") },
            { "archetype.desc", ("혈분노 중 자신의 피로 사슬을 만들고 근접 무기에 피를 입힙니다. 1레벨 고속 이동과 7레벨부터의 직업 피해 감소를 핏빛 사슬과 피의 무기로 교환합니다. 피매듭을 회수하면 블러드레이저 레벨만큼 임시 생명력을 2라운드 동안 얻으며 다시 회수하면 갱신합니다.", "During bloodrage, form a chain from your own blood and coat your melee weapons in it. These abilities replace fast movement and all five ranks of bloodrager damage reduction. Collecting a blood knot grants temporary hit points equal to your bloodrager level for two rounds. Another knot refreshes this protection.") },
            { "chain.name", ("핏빛 사슬", "Blood Chain") },
            { "chain.desc", ("혈분노 중 이동 행동으로 30피트 안의 적에게 사슬을 쏩니다. 원거리 접촉 명중 시 자신과 같거나 작은 적에게 당기기 전투기동을 시도하고, 큰 적에게는 자신이 끌려갑니다. 명중한 적에게 피매듭을 맺습니다. 다음 자기 턴이 끝나기 전(실시간 전투에서는 1라운드 안)에 그 적에게 근접 무기를 명중시키면 직업 레벨만큼 임시 생명력을 2라운드 동안 얻습니다. 다시 회수하면 남은 임시 생명력을 새 값으로 교체합니다.", "As a move action during bloodrage, shoot a chain at a foe within 30 feet. On a ranged touch hit, attempt a pull maneuver against an equal or smaller foe, or pull yourself toward a larger foe. The hit ties a knot. Hit that foe with a melee weapon by the end of your next turn (within 1 round in real time) to gain temporary hit points equal to your bloodrager level for 2 rounds. Collecting another knot replaces and refreshes these hit points.") },
            { "knot.name", ("피매듭", "Blood Knot") },
            { "knot.desc", ("사슬이 명중한 마지막 적에게 근접 무기를 명중시키면 피매듭을 회수합니다. 매듭은 한 번만 회수할 수 있습니다.", "Your next melee weapon hit on the last foe struck by your chain collects this knot once.") },
            { "ward.name", ("되감긴 피", "Returning Blood") },
            { "ward.desc", ("회수한 피매듭이 2라운드 동안 블러드레이저 레벨만큼 임시 생명력을 줍니다.", "A collected knot grants temporary hit points equal to your bloodrager level for 2 rounds. Collecting another knot replaces and refreshes these hit points.") },
            { "reach.name", ("피의 대가", "Blood Price") },
            { "reach.desc", ("핏빛 사슬의 최대 사거리가 60피트입니다. 30피트를 넘겨 쏠 때 실제 생명력 5를 지불합니다. 생명력이 5 이하라면 연장할 수 없습니다.", "Fire a blood chain up to 60 feet. A shot beyond 30 feet costs 5 actual hit points at launch. You must have more than 5 hit points for an extended shot.") },
            { "conduction.name", ("혈류 전도", "Blood Conduction") },
            { "conduction.desc", ("혈분노 중 신속 행동으로 자신이 아는 전격의 손길 또는 흡혈의 손길 하나를 장전합니다. 해당 블러드레이저 슬롯 하나를 소비하며 다음 사슬 명중 시 주문을 전달합니다. 혈분노가 끝나면 장전이 풀립니다.", "As a swift action during bloodrage, load one known shocking grasp or vampiric touch using a bloodrager slot. Your next chain hit delivers it. The charge expires when bloodrage ends.") },
            { "chargeShock.name", ("혈류 전도 — 전격의 손길", "Blood Conduction — Shocking Grasp") },
            { "chargeShock.desc", ("블러드레이저 주문 슬롯 하나로 전격의 손길을 사슬에 장전합니다. 다음 사슬 명중 시 한 번 전달합니다.", "Spend a bloodrager spell slot to load shocking grasp into your chain. Deliver it once on your next chain hit.") },
            { "chargeVamp.name", ("혈류 전도 — 흡혈의 손길", "Blood Conduction — Vampiric Touch") },
            { "chargeVamp.desc", ("블러드레이저 주문 슬롯 하나로 흡혈의 손길을 사슬에 장전합니다. 다음 사슬 명중 시 한 번 전달합니다.", "Spend a bloodrager spell slot to load vampiric touch into your chain. Deliver it once on your next chain hit.") },
            { "weapon.name", ("피의 무기", "Blood-Coated Weapons") },
            { "weapon.desc", ("혈분노 중 손에 든 근접 무기 적중마다 같은 유형의 물리 피해를 1d6 더합니다. 7·13·19레벨에 2d6·3d6·4d6으로 증가합니다. 13레벨부터 확정 치명타 시 이 추가 주사위를 두 배로 굴립니다.", "During bloodrage each hit with a wielded melee weapon deals 1d6 extra physical damage of the weapon's form. This increases to 2d6, 3d6 and 4d6 at levels 7, 13 and 19. From level 13 a confirmed critical doubles these extra dice.") },
            { "critical.name", ("피의 무기 — 치명상", "Blood-Coated Weapons — Critical Wound") },
            { "critical.desc", ("13레벨부터 치명타가 확정되면 피의 무기 주사위를 두 배로 굴립니다. 무기의 치명타 배율에 관계없이 13레벨에는 6d6, 19레벨에는 8d6입니다.", "From level 13, roll twice the blood weapon dice on a confirmed critical. The extra damage is 6d6 at level 13 and 8d6 at level 19 regardless of the weapon's critical multiplier.") },
            { "needsRage", ("혈분노 중에 사용할 수 있습니다.", "Requires active bloodrage.") },
            { "needsLife", ("살아 있는 적이 필요합니다. 연장 사거리에서는 생명력이 5보다 높아야 합니다.", "Requires a living enemy. Beyond normal range you need more than 5 actual hit points.") },
            { "needsSlot", ("아는 블러드레이저 주문, 사용 가능한 슬롯, 비어 있는 사슬이 필요합니다.", "Requires the known bloodrager spell, an available slot and an empty chain.") },
        };
    }
}
