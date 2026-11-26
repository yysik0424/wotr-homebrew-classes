using System.Collections.Generic;
namespace GreySeer
{
    internal static class StringsTable
    {
        internal static readonly Dictionary<string, (string ko, string en)> Table = new Dictionary<string, (string ko, string en)>
        {
            { "archetype.name", ("그레이 시어", "Grey Seer") },
            { "archetype.short", ("장검과 지팡이를 들고 주문 직후 적을 베는 아카니스트.", "An arcanist who follows a spell with a longsword strike while wielding sword and staff.") },
            { "archetype.desc", ("그레이 시어는 본편 아카니스트의 주문 성장을 따릅니다. 1·5·9·13·17레벨 익스플로잇을 교환하고, 1·7·13레벨에 쌍수 전투 계열 특기를 받습니다. 장검과 지팡이를 함께 들면 두 무기의 근접 명중이 향상됩니다.", "A grey seer retains arcanist spell progression and trades the exploits at levels 1, 5, 9, 13 and 17 for sword and staff training, gaining the two-weapon fighting feats at levels 1, 7 and 13.") },
            { "training.name", ("검과 지팡이", "Sword and Staff") },
            { "training.desc", ("쿼터스태프를 한 손에 들 수 있습니다. 장검과 쿼터스태프를 함께 들면 두 무기의 근접 명중에 아카니스트 레벨에 해당하는 전사와 아카니스트 기본 공격 보너스의 차이를 더합니다. 본인 아카니스트 주문 뒤에 벨 적을 직접 지정하거나 일반·안전 자동 검격 중 하나를 켤 수 있습니다.", "You can wield a quarterstaff in one hand. While wielding both a longsword and a quarterstaff, add the difference between fighter and arcanist base attack bonuses at your arcanist level to melee attacks with either weapon. Designate a foe before casting or toggle Automatic Spell and Sword or Automatic Guarded Casting.") },
            { "mark.name", ("검격 대상 지정", "Designate for Spell and Sword") },
            { "mark.desc", ("다음 아카니스트 주문 뒤 벨 적을 고릅니다. 주문은 자유롭게 겨냥할 수 있습니다. 시전이 성공하고 지정한 적이 장검 사거리 안에 있으면 저수지 1점을 써서 추가 행동 없이 한 번 공격합니다. 다시 지정하면 이전 선택을 대체합니다.", "Choose a foe before casting your next arcanist spell. The spell may have any target. After a successful cast, spend 1 reservoir point to strike the designated foe once if within longsword reach. Designating a foe again replaces the previous choice.") },
            { "safe.name", ("안전 검격 대상 지정", "Designate for Guarded Casting") },
            { "safe.desc", ("검격을 지정하고 방어적 시전이 필요할 때 자동 성공합니다. 슬롯 0~3단계는 1점, 4~6단계는 2점, 7~9단계는 3점을 먼저 지불하며 검격 비용을 포함합니다. 부족하면 일반 방어적 시전으로 돌아가고 검격은 취소됩니다. 보호 비용을 낸 뒤 주문이 끊기거나 적이 이탈해도 소비한 점수는 돌아오지 않습니다. 방어적 시전이 필요 없으면 검격에만 1점을 씁니다.", "Designate a foe and automatically succeed if the spell requires defensive casting. Pay a total of 1 point for slots 0–3, 2 for 4–6, or 3 for 7–9 at the defensive check. Insufficient points cancel the strike and leave the usual check in place. Interruptions or a lost target do not refund protection. Without a defensive check, only a successful follow-up costs 1 point.") },
            { "auto.name", ("자동 검격", "Automatic Spell and Sword") },
            { "auto.desc", ("본인 아카니스트 주문을 쓸 때 장검이 닿는 주문 대상을 우선합니다. 그럴 수 없으면 자신과 교전 중인 적 중 장검이 닿는 가장 가까운 적을 고릅니다. 시전 후 그 적을 공격할 때 저수지 1점을 씁니다. 직접 지정하면 그 주문 한 번에 우선합니다. 켜면 자동 안전 검격이 꺼집니다.", "When you cast your own arcanist spell, choose its enemy target if within longsword reach. Otherwise choose the nearest visible opponent engaged with you and within reach. Spend 1 reservoir point when making the follow-up attack. A manual designation overrides this mode for one spell. Enabling this turns off Automatic Guarded Casting.") },
            { "autoSafe.name", ("자동 안전 검격", "Automatic Guarded Casting") },
            { "autoSafe.desc", ("자동 검격과 같은 규칙으로 적을 고릅니다. 적을 골랐고 방어적 시전이 필요하면 저수지 1점(슬롯 0~3단계), 2점(4~6단계), 3점(7~9단계)을 먼저 써서 자동 성공합니다. 검격 비용을 포함하며 보호를 받으면 돌려받지 못합니다. 점수가 부족하면 일반 판정만 합니다. 방어적 시전이 필요 없으면 검격에 1점만 씁니다. 수동 지정은 한 번 우선하며, 켜면 자동 검격이 꺼집니다.", "Select a foe as for Automatic Spell and Sword. With a foe selected, pay 1 point for slots 0–3, 2 for 4–6, or 3 for 7–9 to pass a required defensive casting check. This includes the strike and is spent even if the strike is lost. Insufficient points leave the normal check and cancel the strike. Without a check, only the strike costs 1 point. A manual designation takes priority for one spell. Enabling this turns off Automatic Spell and Sword.") },
            { "equipment", ("장검과 쿼터스태프를 함께 장착해야 합니다.", "Wield a longsword and a quarterstaff together.") }
        };
    }
}
