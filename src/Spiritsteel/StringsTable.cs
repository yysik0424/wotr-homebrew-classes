using System.Collections.Generic;

namespace Spiritsteel
{
    internal static class StringsTable
    {
        public static readonly Dictionary<string, (string ko, string en)> Table = Make();
        static Dictionary<string, (string ko, string en)> Make()
        {
            var t = new Dictionary<string, (string ko, string en)> {
                { "class.name", ("스피릿스틸","Spiritsteel") },
                { "class.short", ("살아 있는 부족의 무기와 함께 분노하는 바바리안.","A barbarian who rages alongside the spirit of an ancestral weapon.") },
                { "class.desc", ("부족이 준 무기 한 자루와 그 안의 신령이 전사를 따라 성장합니다. 빠른 이동과 2·6레벨 격노 능력을 결속 무기와 신령의 전술로 교환합니다. 신령은 바바리안 분노가 지속되는 동안 깨어 있을 수 있으며, 지혜는 공격 전법을 강화합니다.","An ancestral weapon and its spirit grow with the wielder. Spiritsteel exchanges fast movement and the rage powers at levels 2 and 6 for a bound weapon and spirit tactics. The spirit can stay awake for the duration of barbarian rage. Wisdom improves offensive combat maneuvers.") },
                { "rage.name", ("신령의 분노","Spirit Rage") },
                { "rage.desc", ("전투 중 결속 무기를 든 채 분노하면 신령도 깨어납니다. 신령의 분노는 바바리안 분노 중에 따로 끄거나 켤 수 있습니다. 바바리안 분노의 일일 라운드만 소모하며, 끝없는 격노가 있으면 두 효과를 라운드 소모 없이 유지합니다. 신령이 깨어 있는 동안 결속 무기 피해 주사위가 한 크기 단계 증가하고 공격 전법에 지혜 보너스를 더합니다.","While you wield your bound weapon in combat, entering barbarian rage awakens its spirit. Toggle the spirit separately during rage. Only barbarian rage spends daily rounds. With Limitless Rage, both effects can stay active without spending rounds. While the spirit is awake, your bound weapon deals damage as if one size larger and you add your Wisdom bonus to offensive maneuvers.") },
                { "solo.name", ("신령과의 단독 전술","Spirit Solo Tactics") },
                { "solo.desc", ("결속 무기를 들면 단독 전술을 얻습니다. 신령의 분노 중에는 자신이 배운 팀워크 피트의 인접과 협공을 신령이 도우며, 결속 무기의 근접 공격으로 혼자서도 협공할 수 있습니다. 동료의 공격·장비가 필요한 효과는 해당 동료의 실제 행동과 장비를 따릅니다.","Wielding the bound weapon grants solo tactics. In spirit rage its spirit counts as an adjacent ally for your teamwork feats and as a flanking partner for your bound melee weapon. Effects requiring an ally's attacks or equipment use actual allies.") },
                { "trip.name", ("신령의 넘어뜨리기","Spirit Trip") },
                { "trip.desc", ("신령의 분노 중 라운드마다 결속 무기의 첫 명중으로 대상을 넘어뜨리려 시도합니다.","During spirit rage, your first hit each round with the bound weapon attempts a trip maneuver.") },
                { "team.name", ("팀워크 피트","Teamwork Feat") },
                { "team.desc", ("본편 팀워크 피트 하나를 선택합니다. 해당 피트의 선결조건을 따릅니다.","Select a teamwork feat for which you qualify.") },
                { "choice.name", ("격노 능력 또는 팀워크 피트","Rage Power or Teamwork Feat") },
                { "choice.desc", ("격노 능력 하나 또는 조건을 충족한 팀워크 피트 하나를 얻습니다.","Choose a rage power or a teamwork feat for which you qualify.") }
            };
            BoundTexts.Add(t);
            WeaponFeats.AddStrings(t);
            return t;
        }
    }
}
