using System.Collections.Generic;

namespace Falconer
{
    internal static class StringsTable
    {
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();
        static Dictionary<string, (string ko, string en)> Build()
        {
            var t = new Dictionary<string, (string ko, string en)>();
            void Add(string key, string ko, string en, string description, string english)
            {
                t.Add(key + ".name", (ko, en));
                t.Add(key + ".desc", (description, english));
            }

            Add("Archetype", "팔코너", "Falconer", "성장하는 매와 함께 사냥합니다. 정식 동물 동료, 사냥감과 향상된 사냥감을 유지합니다. 9레벨에 회피를 얻고, 16레벨에는 향상된 회피 대신 세 갈래 사냥을 얻습니다.", "Hunt alongside a growing hawk. Keep your animal companion, quarry and improved quarry. You gain Evasion at level 9 and Threefold Hunt in place of Improved Evasion at level 16.");
            Add("Hawk", "사냥매 · 협공 사격", "Hunting Hawk", "신속 행동으로 매를 보내고, 매가 맞힌 표적에게 다음 화살의 명중 +2를 얻습니다. 급강하는 라운드에 한 번입니다.", "Send your hawk as a swift action once per round. Its hit grants +2 on your next arrow against that enemy.");
            Add("Dive", "급강하 명령", "Command Dive", "매가 적에게 돌진합니다. 8·15레벨에 공격이 한 번씩 늘어납니다. 모든 타격에 지혜, 선호 적과 명중 버프를 반영합니다.", "Send the hawk to attack an enemy. Gain an attack at levels 8 and 15. Wisdom, favored enemy and attack buffs apply.");
            Add("Drive", "몰아세우기", "Drive Prey", "동물 동료가 맞힌 적에게 다음 급강하 명중 +2. 동료에게 걸린 이로운 주문이 매에게도 적용됩니다.", "Your companion's hit grants +2 on the next dive against its target. Beneficial spells on the companion also affect the hawk.");
            Add("Auto", "자동 급강하", "Automatic Dive", "직접 무기 공격한 표적에게 매가 무료로 돌진합니다. 수동 명령과 라운드당 출격을 공유합니다.", "The hawk dives on your weapon attack target for free, sharing the manual dive's once-per-round limit.");
            Add("Cover", "날개 엄호", "Wing Cover", "매가 맞힌 적은 한 라운드 동안 동물 동료의 이동에 기회공격을 할 수 없습니다.", "An enemy hit by the hawk cannot make movement attacks of opportunity against your animal companion for one round.");
            Add("Eye", "매의 눈", "Hawk's Eye", "신속 행동. 지혜 수정치만큼 라운드 동안(최소 1) 각 급강하 첫 타격에 진정한 일격을 적용합니다. 하루 1회, 19레벨에 2회.", "Swift action. For rounds equal to your Wisdom modifier (minimum one), the first strike of each dive gains true strike. Once daily, twice at level 19.");
            Add("Third", "세 번째 타격", "Third Strike", "급강하 때 양 발톱에 이어 부리로 공격합니다.", "After both talons strike, the hawk attacks with its beak.");
            Add("Hunt", "세 갈래 사냥", "Threefold Hunt", "매와 동료가 한 라운드 안에 같은 적을 맞히면 다음 협공 사격이 정밀 피해 2d6을 추가합니다. 라운드당 한 번.", "When hawk and companion hit the same enemy within a round, your next coordinated arrow deals +2d6 precision damage, once per round.");
            Add("EyeMore", "매의 눈 추가 사용", "Greater Hawk's Eye", "매의 눈을 하루 두 번 사용합니다.", "Use Hawk's Eye twice a day.");
            return t;
        }
    }
}
