using System.Collections.Generic;

namespace BlastMagus
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
        // Main.Load 가 Strings.Configure(Table, GameGuids.MagusClass) 로 넘긴다. 수치·규칙 출처 = docs\2026-11-06-blast-magus-design.md §4.2~§4.9, §11.
         
         
        // 2026-11-17: 원소 선택 화면·원소 선택지 11개(Element*.Description · ElementSelection.Description)와 블라스트 랭크·원소 블라스트·폭발의 설명은 리터럴이 아니다 —
        // ChoiceText(ChoiceText.cs)가 BlastRules · ElementTable · BlastFacts 에서 만들어 넣는다(C4: 문구 속 수치 = 규칙 상수). 시험 `choice text` 가 잠근다.
        // 원소·복합 원소 이름은 한글 패치 표기(화염·대기·대지, 푸른 불꽃·번개폭풍 — WrathKoreanMod translation.json 2026-11-17 대조).
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();

        static Dictionary<string, (string ko, string en)> Build()
        {
            var d = new Dictionary<string, (string ko, string en)>
            {
                ["Archetype.Name"] = ("블라스트 메이거스", "Blast Magus"),
                ["Archetype.Description"] = (
                    "블라스트 메이거스는 칼날과 주문 사이에 원소의 폭발을 끼워 넣는 메이거스다.\n1레벨에 화염·냉기·전기(에너지)·대기·대지·물(물리) 중 하나(KineticistElementsExpanded 모드가 켜져 있으면 텔레키네틱·중력·나무·음에너지·양에너지도)를 골라 지능으로 쏘는 원거리 블라스트를 얻고, 무기에 선택한 원소의 피해(에너지 원소는 그 에너지, 물리 원소는 그 물리 피해)를 실어 휘두르며, 4레벨부터는 아케인 풀 1점으로 다음 무기 명중에 블라스트 피해를 통째로 실어 터뜨린다.\n대신 0레벨 주문(캔트립)과 탐지 마법, 주문 재활용 계열, 무기 강화 속성과 13레벨 비전 중장갑을 잃는다.\n아케인 풀은 무기 강화 능력이 없는 블라스트 풀로 바뀌고, 비전 선택지에서는 무기 강화 전용 비전 4종이 빠진다.\n주능력치는 지능이다. 20레벨부터 블라스트 강타의 폭발은 무기 치명타에도 일반 폭발 피해를 줍니다.",
                    "The Blast Magus is a magus who fits elemental detonations between blade and spell.\nAt level 1 you choose fire, cold or electricity (energy) or air, earth or water (physical) - or, with the KineticistElementsExpanded mod enabled, telekinetic, gravity, wood, negative or positive energy - and gain a ranged blast driven by Intelligence, you carry that element's damage in your weapon (energy for energy elements, physical damage for physical elements), and from level 4 you spend 1 arcane pool point to load your next weapon hit with a full blast of damage.\nIn exchange you lose cantrips and Detect Magic, spell recall, arcane weapon enhancements and the 13th-level Arcane Heavy Armor feature.\nYour arcane pool becomes a Blast Pool with no weapon enhancement ability, and the four weapon-enhancement-only arcana are removed from your arcana choices.\nIntelligence is the key ability. From level 20, the blast from Blast Strike deals its ordinary damage even when the weapon critically hits."),
                ["Archetype.DescriptionShort"] = (
                    "원소를 고르고, 무기에 그 원소의 피해를 싣고, 아케인 풀로 명중에 블라스트를 터뜨린다. 캔트립과 주문 재활용을 잃는다.",
                    "Choose an element, carry its damage in your weapon, and burst a blast through a hit with arcane pool. Loses cantrips and spell recall."),
                 
                ["BlastPool.Name"] = ("블라스트 풀", "Blast Pool"),
                ["BlastPool.Description"] = (
                    "아케인 풀과 같은 자원(메이거스 레벨의 절반 + 지능 수정치)을 얻지만, 무기에 강화를 거는 능력은 없다. 이 풀은 폭발과 메이거스 비전에 쓴다. 아케인 풀을 전제조건으로 하는 특기(추가 아케인 풀·풍부한 아케인 풀)는 고를 수 없다.",
                    "You gain the same resource as the arcane pool (half your magus level + your Intelligence modifier), but no ability to enhance your weapon. The pool fuels Blast Strike and magus arcana. Feats that require the arcane pool (Extra Arcane Pool, Abundant Arcane Pool) cannot be taken."),
                ["ArcanaSelection.Name"] = ("블라스트 메이거스 비전", "Blast Magus Arcana"),
                ["ArcanaSelection.Description"] = (
                    "3·6·9·12·15·18레벨에 메이거스 비전(한글 패치의 메이거스 비의)을 하나 고른다.\n블라스트 풀에는 무기 강화 능력이 없어 쓸모가 없는 비전 4종은 목록에서 빠진다: 파멸의 칼날(Bane Blade)·헌신적인 칼날(Devoted Blade)·지속되는 칼날(Enduring Blade)·유령 칼날(Ghost Blade).\n추가 비의(Extra Arcana) 특기는 고를 수 없다.",
                    "At levels 3, 6, 9, 12, 15 and 18 you choose one magus arcana.\nThe four arcana that only modify the arcane weapon enhancement are not offered, since the Blast Pool has no weapon enhancement ability: Bane Blade, Devoted Blade, Enduring Blade, Ghost Blade.\nThe Extra Arcana feat cannot be taken."),
                ["ElementSelection.Name"] = ("원소 선택", "Element Choice"),
                ["ElementFire.Name"] = ("화염", "Fire"),
                ["ElementCold.Name"] = ("냉기", "Cold"),
                ["ElementElectric.Name"] = ("전기", "Electricity"),
                 
                ["ElementAir.Name"] = ("대기", "Air"),
                ["ElementEarth.Name"] = ("대지", "Earth"),
                ["ElementWater.Name"] = ("물", "Water"),
                 
                ["ElementTelekinetic.Name"] = ("텔레키네틱", "Telekinetic"),
                ["ElementGravity.Name"] = ("중력", "Gravity"),
                ["ElementWood.Name"] = ("나무", "Wood"),
                ["ElementNegative.Name"] = ("음에너지", "Negative Energy"),
                ["ElementPositive.Name"] = ("양에너지", "Positive Energy"),
                ["Blast.Name"] = ("원소 블라스트", "Elemental Blast"),
                ["BlastRank.Name"] = ("블라스트 랭크 +1", "Blast Rank +1"),
                ["Energize.Name"] = ("에너지 주입", "Energize Weapon"),
                ["Energize.Description"] = (
                    "자유 행동으로 켜고 끈다.\n켜 두면 근접 제작 무기(맨손·자연 공격 제외)의 모든 공격에 선택한 원소의 피해가 추가된다(에너지 원소는 그 에너지 피해, 물리 원소는 그 물리 피해 — 무기 피해의 일부로 처리된다).\n가진 원소마다 하나씩 메뉴에 있고 한 번에 하나만 켤 수 있다(원소가 하나면 처음부터 켜져 있다).\n비용이 없고 휴식해도 꺼지지 않는다.",
                    "Toggle as a free action.\nWhile on, every attack with a melee manufactured weapon (not unarmed or natural attacks) deals extra damage of your chosen element (energy damage for energy elements, physical damage for physical elements - handled as part of the weapon damage).\nThe menu holds one entry per element you have and only one can be on at a time (with a single element it starts on).\nIt costs nothing and stays on through rest."),
                // 2026-11-15 (에너지 주입 = 본편 Energized Wild Shape 구조): 원소 자식 토글·버프 이름. 키 = EnergizeTable 의 NameKey(시험이 표와 대조).
                ["EnergizeFire.Name"] = ("에너지 주입: 화염", "Energize Weapon: Fire"),
                ["EnergizeCold.Name"] = ("에너지 주입: 냉기", "Energize Weapon: Cold"),
                ["EnergizeElectric.Name"] = ("에너지 주입: 전기", "Energize Weapon: Electricity"),
                ["EnergizeAir.Name"] = ("에너지 주입: 대기", "Energize Weapon: Air"),
                ["EnergizeEarth.Name"] = ("에너지 주입: 대지", "Energize Weapon: Earth"),
                ["EnergizeWater.Name"] = ("에너지 주입: 물", "Energize Weapon: Water"),
                ["EnergizeTelekinetic.Name"] = ("에너지 주입: 텔레키네틱", "Energize Weapon: Telekinetic"),
                ["EnergizeGravity.Name"] = ("에너지 주입: 중력", "Energize Weapon: Gravity"),
                ["EnergizeWood.Name"] = ("에너지 주입: 나무", "Energize Weapon: Wood"),
                ["EnergizeNegative.Name"] = ("에너지 주입: 음에너지", "Energize Weapon: Negative Energy"),
                ["EnergizePositive.Name"] = ("에너지 주입: 양에너지", "Energize Weapon: Positive Energy"),
                ["EnergizeFeature.Name"] = ("에너지 주입", "Energize Weapon"),
                ["EnergizeFeature.Description"] = (
                    "무기에 선택한 원소의 피해를 싣는 능력을 얻는다. 기본 피해는 1d6이고 7·13·19레벨에 1d6씩 늘어 최대 4d6이 된다.",
                    "You gain the ability to carry your chosen element's damage in your weapon. It deals 1d6 and gains 1d6 at levels 7, 13 and 19, up to 4d6."),
                ["Energize7.Name"] = ("에너지 주입: 2d6", "Energize Weapon: 2d6"),
                ["Energize7.Description"] = (
                    "7레벨: 에너지 주입의 추가 피해가 1d6 늘어 합계 2d6이 된다.",
                    "Level 7: the extra damage of Energize Weapon rises by 1d6, to 2d6 in total."),
                ["Energize13.Name"] = ("에너지 주입: 3d6", "Energize Weapon: 3d6"),
                ["Energize13.Description"] = (
                    "13레벨: 에너지 주입의 추가 피해가 1d6 늘어 합계 3d6이 된다.",
                    "Level 13: the extra damage of Energize Weapon rises by 1d6, to 3d6 in total."),
                ["Energize19.Name"] = ("에너지 주입: 4d6", "Energize Weapon: 4d6"),
                ["Energize19.Description"] = (
                    "19레벨: 에너지 주입의 추가 피해가 1d6 늘어 합계 4d6이 된다. 20레벨의 복합 원소는 이 피해를 늘리지 않는다.",
                    "Level 19: the extra damage of Energize Weapon rises by 1d6, to 4d6 in total. The level 20 composite element does not increase it."),
                ["BlastStrike.Name"] = ("폭발", "Blast Strike"),
                ["BlastStrikeSource.Name"] = ("폭발", "Blast Strike"),
                ["BlastStrike.Disabled"] = (
                    "블라스트 비활성: 이 캐릭터의 키네티시스트 구성이 블라스트 메이거스와 충돌한다(모드 로그 참조).",
                    "Blasts disabled: this character's kineticist setup conflicts with the Blast Magus (see the mod log)."),
                ["Composite20.Name"] = ("복합 원소", "Composite Element"),
                ["Composite20.Description"] = (
                    "20레벨: 고른 원소의 복합 블라스트를 얻는다.\n화염은 푸른 불꽃, 냉기·물은 얼음, 전기·대기는 번개폭풍, 대지는 금속이다(KineticistElementsExpanded 원소: 텔레키네틱은 역장, 중력·음에너지는 공허, 나무·양에너지는 신록).\n복합 블라스트는 원거리 블라스트로 쓴다.\n폭발은 원소의 피해 유형을 유지한 채 주사위가 2배가 되고, 에너지 주입은 늘어나지 않는다.",
                    "Level 20: you gain the composite blast of your chosen element.\nBlue flame for fire, ice for cold and water, thunderstorm for electricity and air, metal for earth (KineticistElementsExpanded elements: force for telekinetic, void for gravity and negative energy, verdant for wood and positive energy).\nThe composite blast is used as a ranged blast.\nBlast Strike keeps your element's damage type but rolls twice the dice; Energize Weapon does not increase."),
            };
            ChoiceText.AddTo(d);
            return d;
        }
    }
}
