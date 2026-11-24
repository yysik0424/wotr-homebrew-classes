using System.Collections.Generic;

namespace DreamThief
{
    internal static class StringsTable
    {
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();
        static Dictionary<string, (string ko, string en)> Build()
        {
            var t = new Dictionary<string, (string ko, string en)>();
            void Write(string key, string ko, string en, string descKo, string descEn)
            {
                t.Add(key + ".name", (ko, en));
                t.Add(key + ".desc", (descKo, descEn));
            }
            Write("archetype", "꿈결 도둑", "Dream Thief", "데스나를 섬기는 로그입니다. 스타 나이프를 던져 적을 재우고 꿈길을 걸어 잠든 적의 급소를 노립니다.", "A rogue devoted to Desna who puts enemies to sleep with a thrown starknife, then steps through dreams to strike.");
            Write("pool","별빛","Starlight","휴식마다 로그 레벨의 절반 + 민첩 수정치 + 매력 수정치만큼 별빛을 회복합니다(최소 1). 18레벨부터 최대치가 3 증가합니다.","Your daily starlight equals half your rogue level + your Dexterity and Charisma modifiers (minimum 1). At level 18, add 3 to this pool.");
            Write("throw","별똥별 투척","Falling Star","표준 행동으로 손에 든 스타 나이프를 30피트 안의 적에게 던집니다. 칼은 손으로 돌아옵니다.","As a standard action, throw a wielded starknife at an enemy within 30 feet. The blade returns to your hand.");
            Write("dream","꿈결 투척","Dream Throw","별빛 1점을 써서 스타 나이프를 던집니다. 적중 시 적은 의지 내성에 실패하면 1d4라운드 동안 잠들며, 피해를 받으면 깨어납니다. DC는 10 + 로그 레벨의 ¾(내림) + 매력 수정치입니다. 내성에 성공하면 1분 동안 꿈결 도둑의 수면 능력에 면역입니다.","Spend 1 starlight to throw your starknife. On a hit, the enemy must save against Will or sleep for 1d4 rounds, waking upon damage. The DC is 10 + three quarters of your rogue level (rounded down) + your Charisma modifier. A successful save grants 1 minute of immunity to Dream Thief sleep effects.");
            Write("waking","깨우는 일격","Waking Strike","잠든 적을 스타 나이프로 명중시키면 치명타와 급소 공격을 가합니다. 대상의 치명타·정밀 피해 면역과 방어 요새화가 적용됩니다.","When your starknife hits a sleeping enemy, the hit is a confirmed critical and a sneak attack. Critical immunity, precision immunity and fortification apply normally.");
            Write("shroud","별빛 두르기","Starlight Shroud","신속 행동으로 별빛 1점을 써서 1분간 스타 나이프에 신성 피해 1d6을 더합니다. 전체 공격 시 최고 명중으로 추가 1회 공격합니다. 헤이스트와 중첩되지 않습니다.","Spend 1 starlight as a swift action. For 1 minute your starknife deals an extra 1d6 divine damage. Gain one extra starknife attack at your highest bonus on a full attack. This does not stack with haste.");
            Write("walk","꿈길 걸음","Dreamwalk","별빛 1점을 써서 30피트 안의 적 옆으로 순간이동해 스타 나이프로 공격합니다. 명중하면 급소 공격을 가하고 즉시 원래 자리로 돌아옵니다.","Spend 1 starlight to step beside an enemy within 30 feet, make a starknife attack that deals sneak damage on a hit, and return to your starting point.");
            Write("reach","먼 꿈길","Far Dreamwalk","꿈길 걸음의 사거리가 50피트가 됩니다.","Dreamwalk now reaches 50 feet.");
            Write("burst","터져 나오는 별빛","Starlight Burst","별빛 두르기 중 확정 치명타에 (치명타 배율 − 1)d10의 신성 피해를 더합니다.","While Starlight Shroud is active, confirmed critical hits deal an extra 1d10 divine damage per point of critical multiplier above one.");
            Write("swiftWalk","가벼운 꿈길","Fleet Dreamwalk","꿈길 걸음을 이동 행동으로 사용합니다.","Dreamwalk now uses a move action.");
            Write("reserve","깊은 별빛","Deep Starlight","하루 별빛 최대치가 3 증가합니다.","Your daily starlight pool increases by 3.");
            Write("immune","꿈에서 깨어남","Dreamward","꿈결 도둑의 수면 효과에 1분 동안 면역입니다.","Immune to Dream Thief sleep effects for 1 minute.");
            Write("talent","꿈 재능","Dream Talent","일반 로그 재능 또는 꿈 재능을 고릅니다. 꿈 재능은 로그 레벨을 시전자 레벨로 사용합니다.","Choose a rogue talent or a dream talent. Your caster level for dream talents equals your rogue level.");
            Write("Aura","자장가 오라","Lulling Aura","30피트 안의 적이 수면 내성에 −2를 받습니다. 12레벨부터 −4가 됩니다.","Enemies within 30 feet take a −2 penalty on saving throws against sleep, increasing to −4 at level 12.");
            Write("Nightmare","악몽","Nightmare","별빛 3점. 반경 10피트 안의 적은 의지 내성 실패 시 1d4라운드 동안 잠들고 매 라운드 2d10 피해를 받습니다. 깨어나면 10라운드 동안 혼란에 빠집니다.","Spend 3 starlight. Enemies in a 10-foot radius must save against Will or fall asleep for 1d4 rounds, taking 2d10 damage each round. On waking they are confused for 10 rounds.");
            var spells = new[] {
                ("Lullaby","자장가","Lullaby","대상을 졸리게 하여 수면 내성에 불리하게 합니다.","Make the target drowsy and weaken its saves against sleep."),
                ("Sleep","수면","Sleep","합계 HD가 로그 레벨 이하인 적들을 재웁니다.","Put creatures to sleep, up to a total number of Hit Dice equal to your rogue level."),
                ("DeepSlumber","깊은 잠","Deep Slumber","합계 HD가 로그 레벨 + 4 이하인 적들을 재웁니다.","Put creatures to sleep, up to a total number of Hit Dice equal to your rogue level + 4."),
                ("Haze","꿈의 아지랑이","Haze of Dreams","꿈결 같은 환각으로 대상의 움직임을 늦춥니다.","Slow the target's movement with dreamlike visions."),
                ("Cloak","꿈의 망토","Cloak of Dreams","주위 생물을 잠들게 하는 꿈의 안개를 두릅니다.","Surround yourself with a haze that can put nearby creatures to sleep."),
                ("Hypnotism","최면","Hypnotism","영역 안의 생물을 매혹하는 최면을 겁니다.","Hypnotize creatures in the area, leaving them fascinated."),
                ("Rainbow","무지개 무늬","Rainbow Pattern","빛나는 무지개 무늬가 범위 내의 생물을 매혹합니다.","Fascinate creatures in the area with a shifting pattern of light."),
                ("Killer","환영 살해자","Phantasmal Killer","대상에게 치명적인 공포의 환영을 보여 줍니다.","Confront a target with a potentially lethal illusion of its worst fear."),
                ("Web","환영 거미줄","Phantasmal Web","환영의 거미줄과 거미 떼로 적의 움직임을 막습니다.","Ensnare enemies in an illusory web filled with spiders."),
                ("Putrefaction","환영 부패","Phantasmal Putrefaction","공포스러운 부패의 환영으로 적들을 쇠약하게 합니다.","Weaken enemies with a terrifying illusion of decay."),
                ("Weird","괴이","Weird","여러 적에게 치명적인 공포의 환영을 보여 줍니다.","Confront multiple enemies with potentially lethal apparitions."),
                ("Slumber","잠","Slumber","의지 내성에 실패하면 로그 레벨당 1라운드 동안 잠들며 피해를 받으면 깨어납니다.","On a failed Will save, the enemy sleeps for 1 round per rogue level and wakes on damage."),
                ("Restless","불안한 잠","Restless Slumber","의지 내성 실패 시 로그 레벨당 1라운드 동안 잠들어 매 라운드 1d10 피해를 받고 깨어날 때 혼란에 빠집니다.","On a failed Will save, the enemy sleeps for 1 round per rogue level, takes 1d10 damage each round, and is confused upon waking.")
            };
            foreach (var s in spells)
            {
                var row = System.Array.Find(DreamTalents.All, r => r.Key == s.Item1);
                string ko = $"별빛 {row.Cost}점. {s.Item4} 시전자 레벨은 로그 레벨이며 DC는 10 + 로그 레벨의 ¾(내림) + 매력 수정치입니다.";
                string en = $"Costs {row.Cost} starlight. {s.Item5} Caster level equals rogue level. The save DC is 10 + three quarters of rogue level (rounded down) + Charisma modifier.";
                Write(s.Item1,s.Item2,s.Item3,ko,en);
            }
            t.Add("knife", ("스타 나이프를 장착해야 합니다.", "Wield a starknife."));
            t.Add("blocked", ("꿈길이 막혔습니다.", "The dream path is blocked."));
            t.Add("resists", ("수면 면역", "Immune to sleep"));
            return t;
        }
    }
}
