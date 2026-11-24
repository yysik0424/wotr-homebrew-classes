using System.Collections.Generic;

namespace Deathdealer
{
    internal static class StringsTable
    {
        internal static readonly Dictionary<string,(string ko,string en)> Table = Build();
        static Dictionary<string,(string ko,string en)> Build()
        {
            var t = new Dictionary<string,(string ko,string en)>();
            void Add(string key,string ko,string en,string kd,string ed)
            {
                t.Add(key+".name",(ko,en)); t.Add(key+".desc",(kd,ed));
            }
            Add("Archetype","데스딜러","Deathdealer","연구 대상의 행동을 읽고 공격, 이동, 주문 시전에 대응하는 슬레이어입니다. 자신에게 비전 주문유사 능력을 사용합니다.","A slayer who anticipates a studied enemy's attacks, movement and spells, and uses personal arcane spell-like abilities.");
            Add("Menu","비전 주문유사 능력","Arcane Abilities","배운 능력을 골라 자신에게 사용합니다. 각 능력은 하루 1회, 일반 행동으로 발동합니다. 시전자 레벨은 슬레이어 레벨입니다.","Choose a personal spell-like ability. Each is usable once per day as a standard action, with caster level equal to your slayer level.");
            Add("Counter","연구 반격","Studied Riposte","위협하는 연구 대상의 무기 공격이 끝나면 적중 여부와 무관하게 한 번 반격합니다. 세 반응은 한 라운드에 합쳐 한 번, 기회공격 한 번을 사용합니다. 반응 명중에는 기존 능력치와 지능 중 높은 값을 씁니다.","After a threatened studied target makes a weapon attack, strike once, hit or miss. The three reactions share one use per round and spend an attack of opportunity. Use the better of Intelligence and your normal attack ability.");
            Add("Trip","이동 저지","Intercept Movement","위협하는 연구 대상이 자발적으로 이동하면 떠나기 전에 넘어뜨립니다. 지능과 통상 전법 능력치 중 높은 값을 사용합니다.","Trip a threatened studied target as it starts moving voluntarily. Use the higher of Intelligence and the normal maneuver ability.");
            Add("Interrupt","시전 대응","Disrupt Casting","위협하는 연구 대상이 주문을 시전하면 효과가 나오기 전에 공격합니다. 명중 시 치명타 위협이 되며 피해를 받은 시전자는 집중을 판정합니다.","Strike a threatened studied target before its spell resolves. A hit threatens a critical. Damage can disrupt the spell through concentration.");
            Add("Research","심화 연구","Intensive Study","자신의 연구 대상에게 무기 명중 +2를 얻고 치명타 위협을 자동 확정합니다.","Gain +2 on weapon attack rolls and automatically confirm critical threats against your studied target.");
            Add("Mastery","향상된 심화 연구","Improved Intensive Study","자신의 연구 대상을 공격할 때 무기 치명타 배율이 한 단계 증가합니다.","Increase your weapon's critical multiplier by one against your studied target.");
            var ko = new[]{"진정한 일격","거울상","가속","돌가죽","정령화신","상급 영웅심","전설적인 비율","바다의 망토","예지"};
            var en = new[]{"True Strike","Mirror Image","Haste","Stoneskin","Geniekind","Greater Heroism","Legendary Proportions","Seamantle","Foresight"};
            for (int i=0;i<ko.Length;i++)
                Add("Tier"+(i+1),ko[i],en[i],"자신에게 "+ko[i]+" 효과를 부여합니다. 하루 1회, 일반 행동.",
                    "Gain the effects of "+en[i]+" on yourself once per day as a standard action.");
            return t;
        }
    }
}
