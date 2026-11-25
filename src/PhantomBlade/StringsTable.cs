using System.Collections.Generic;

namespace PhantomBlade
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
        // Main.Load 가 Strings.Configure(Table, GameGuids.FighterClass) 로 넘긴다. 수치 출처 = docs\2026-11-07-phantom-blade-design.md 4.2~4.5.
        internal static readonly Dictionary<string, (string ko, string en)> Table = new Dictionary<string, (string ko, string en)>
        {
            ["Archetype.Name"] = ("귀검사", "Phantom Blade"),
            ["Archetype.Description"] = (
                "귀검사는 수호령을 부리는 전사다. 본체는 앞에서 검을 휘두르고, 본체를 둘러싼 반투명한 거대 형상 「귀검(鬼劍)」이 본체의 매 라운드 첫 몇 번의 근접 명중마다 주먹을 꽂아 넣으며(레벨이 오를수록 늘어난다) 본체가 맞을 피해의 25%를 대신 받아 준다. 10레벨부터는 매 라운드 정해진 횟수만큼 본체의 첫 근접 공격들에 맞춰, 빗나가도 귀검이 스스로 접촉 공격을 한다. 용맹·갑옷 훈련·갑옷 숙달 대신 수호령 능력을 얻는다. 주능력치는 매력이다.",
                "The Phantom Blade is a fighter accompanied by a Guardian Phantom. The body fights with a sword in front while a vast translucent figure, the Phantom Blade, surrounds it: it lands extra fist blows on its master's first few melee hits each round (more as it grows) and takes 25% of the damage its master would suffer; from level 10 it makes its own touch attack on the first eligible melee attacks each round, even when its master misses. It replaces bravery, armor training and armor mastery with Guardian Phantom abilities. Charisma is the key ability."),
            ["Archetype.DescriptionShort"] = (
                "수호령이 본체의 라운드 첫 근접 명중들에 주먹을 꽂고 피해의 25%를 대신 받는다.",
                "A Guardian Phantom lands extra fist blows on the round's first melee hits and absorbs 25% of the damage."),
            ["Stand.Name"] = ("귀검 / Phantom Blade", "Phantom Blade"),
            ["Stand.Description"] = (
                // 소유자 결정 2026-11-16(설계 §14): 풀 ×2(StandRules.PoolMultiplier — 시험이 이 숫자를 상수와 대조), 전투가 끝나도 차지 않음(휴식·레벨업만).
                "본체를 둘러싼 반투명한 거대 형상 「귀검」. 켜져 있는 동안 본체가 받는 피해의 25%를 흡수 풀이 대신 받는다. 풀 최대치는 귀검사 레벨 × 매력 수정치(최소 1) × 2 이다. 풀은 휴식을 마치거나 레벨이 오르면 가득 차고, 전투가 끝나도 차지 않는다.",
                "A vast translucent figure, the Phantom Blade, surrounds its master. While it is manifest, its absorption pool takes 25% of the damage its master would suffer. The pool holds Phantom Blade level × Charisma modifier (minimum 1) × 2 points. It refills when you finish a rest and on level-up; it does not refill when combat ends."),
            ["Pool.Name"] = ("귀검 흡수 풀", "Phantom Blade Absorption Pool"),
            ["Pool.Description"] = (
                "수호령이 대신 받을 수 있는 피해의 남은 양. 최대치는 귀검사 레벨 × 매력 수정치(최소 1) × 2 이고, 휴식을 마치거나 레벨이 오르면 가득 찬다.",
                "How much more damage the Guardian Phantom can take in your place. The maximum is Phantom Blade level × Charisma modifier (minimum 1) × 2; it refills when you finish a rest and on level-up."),
            ["StandToggle.Name"] = ("수호령 발현", "Manifest Guardian Phantom"),
            ["StandToggle.Description"] = (
                "자유 행동으로 수호령을 불러내거나 거둔다. 켜져 있는 동안 수호령의 러시와 방어가 작동하며, 비용은 없고 휴식해도 꺼지지 않는다.",
                "Summon or dismiss the Guardian Phantom as a free action. While active, the Guardian Phantom's rush and guard work. It costs nothing and stays on through rest."),
             
            ["Rush3.Name"] = ("수호령 러시 (라운드당 1회)", "Guardian Phantom Rush (1 per round)"),
            ["Rush3.Description"] = (
                "3레벨: 라운드마다 본체의 첫 근접 명중에 수호령이 같은 대상에 역장 피해 주먹 1타를 추가로 꽂는다(라운드당 1타). 피해 보너스는 매력 수정치다. 주먹 피해는 전사 레벨을 따라 본편 몽크의 맨손 피해처럼 커진다: 1d6(1~3)·1d8(4~7)·1d10(8~11)·2d6(12~15)·2d8(16~19)·2d10(20). 10레벨부터는 명중에 얹히지 않고, 본체의 그 라운드 첫 몇 번의 근접 공격마다(빗나가도) 수호령이 직접 근접 접촉 공격(기본 공격 보너스 + 매력 대 접촉 AC)을 하고 명중하면 같은 주먹 피해를 준다.",
                "Level 3: the first melee hit you land each round makes the Guardian Phantom strike the same target with a force-damage fist (1 blow per round). Its damage bonus is your Charisma modifier. Fist damage grows with your fighter level like a monk's unarmed strike: 1d6 (levels 1-3), 1d8 (4-7), 1d10 (8-11), 2d6 (12-15), 2d8 (16-19), 2d10 (20). From level 10 the blows no longer ride on your hits: for each of your first few melee attacks in a round, even a miss, the Guardian Phantom makes its own melee touch attack (base attack bonus + Charisma modifier against touch AC) and deals the same fist damage on a hit."),
            ["Rush7.Name"] = ("수호령 러시 (라운드당 2회)", "Guardian Phantom Rush (2 per round)"),
            ["Rush7.Description"] = (
                "7레벨: 러시가 라운드당 2타로 는다 — 본체의 그 라운드 첫 2번 근접 명중마다 수호령이 같은 대상에 역장 피해 주먹 1타씩을 꽂는다. 피해 보너스는 매력 수정치, 주사위는 전사 레벨을 따른다.",
                "Level 7: Rush rises to 2 blows per round — each of the first 2 melee hits you land in a round makes the Guardian Phantom strike the same target once with a force-damage fist. Its damage bonus is your Charisma modifier; the dice follow your fighter level."),
            ["Rush11.Name"] = ("수호령 러시 (라운드당 3회)", "Guardian Phantom Rush (3 per round)"),
            ["Rush11.Description"] = (
                "11레벨: 러시가 라운드당 3타로 는다 — 본체의 그 라운드 첫 3번 근접 공격마다(빗나가도) 수호령이 같은 대상에 근접 접촉 공격 1타씩(기본 공격 보너스 + 매력 대 접촉 AC)을 하고, 명중하면 역장 피해 주먹을 꽂는다. 피해 보너스는 매력 수정치, 주사위는 전사 레벨을 따른다.",
                "Level 11: Rush rises to 3 blows per round — for each of the first 3 melee attacks you make in a round, even on a miss, the Guardian Phantom makes one melee touch attack (base attack bonus + Charisma modifier against touch AC) on the same target and deals force-damage fist damage on a hit. Its damage bonus is your Charisma modifier; the dice follow your fighter level."),
            ["Rush15.Name"] = ("수호령 러시 (라운드당 4회)", "Guardian Phantom Rush (4 per round)"),
            ["Rush15.Description"] = (
                "15레벨: 러시가 라운드당 4타로 는다 — 본체의 그 라운드 첫 4번 근접 공격마다(빗나가도) 수호령이 같은 대상에 근접 접촉 공격 1타씩(기본 공격 보너스 + 매력 대 접촉 AC)을 하고, 명중하면 역장 피해 주먹을 꽂는다. 피해 보너스는 매력 수정치, 주사위는 전사 레벨을 따른다.",
                "Level 15: Rush rises to 4 blows per round — for each of the first 4 melee attacks you make in a round, even on a miss, the Guardian Phantom makes one melee touch attack (base attack bonus + Charisma modifier against touch AC) on the same target and deals force-damage fist damage on a hit. Its damage bonus is your Charisma modifier; the dice follow your fighter level."),
            ["Rush19.Name"] = ("수호령 러시 (라운드당 5회)", "Guardian Phantom Rush (5 per round)"),
            ["Rush19.Description"] = (
                "19레벨: 러시가 라운드당 5타로 는다 — 본체의 그 라운드 첫 5번 근접 공격마다(빗나가도) 수호령이 같은 대상에 근접 접촉 공격 1타씩(기본 공격 보너스 + 매력 대 접촉 AC)을 하고, 명중하면 역장 피해 주먹을 꽂는다. 피해 보너스는 매력 수정치, 주사위는 전사 레벨을 따른다. 갑옷 숙달 대신 얻는다.",
                "Level 19: Rush rises to 5 blows per round — for each of the first 5 melee attacks you make in a round, even on a miss, the Guardian Phantom makes one melee touch attack (base attack bonus + Charisma modifier against touch AC) on the same target and deals force-damage fist damage on a hit. Its damage bonus is your Charisma modifier; the dice follow your fighter level. Replaces armor mastery."),
            ["RangedGuard.Name"] = ("수호령 원거리 방어", "Guardian Phantom Ranged Guard"),
            ["RangedGuard.Description"] = (
                "수호령이 켜져 있는 동안 날아드는 화살을 쳐낸다. 원거리 공격에 대한 AC 가 6레벨에 +2, 14레벨에 +2 더 늘어 합계 +4 가 된다.",
                "While the Guardian Phantom is active, it swats incoming missiles aside. AC against ranged attacks rises by 2 at level 6 and by another 2 at level 14, for +4 in total."),
            // 폐지된 10레벨 특성(소유자 결정 2026-11-16). 블루프린트는 세이브 호환으로 남고 UI 에서 숨긴다 — 이 문구는 숨김이 안 먹는 화면에 보일 때를 위한 것이다.
            ["Refill.Name"] = ("수호령 재충전 (폐지)", "Guardian Phantom Refill (retired)"),
            ["Refill.Description"] = (
                "이 특성은 더 이상 효과가 없다. 수호령의 흡수 풀은 휴식을 마치거나 레벨이 오를 때만 가득 찬다.",
                "This feature no longer has any effect. The Guardian Phantom's absorption pool refills only when you finish a rest or gain a level."),
            ["Absorb18.Name"] = ("수호령 완전 흡수", "Guardian Phantom Greater Absorption"),
            ["Absorb18.Description"] = (
                "수호령은 모든 레벨에서 피해의 25%를 대신 받는다.",
                "The Guardian Phantom absorbs 25% of damage at every level."),
             
            ["Absorb.LogLine"] = ("귀신이 {0} 피해를 흡수했다 (남은 풀 {1})", "The phantom absorbed {0} damage (pool left {1})"),
            // 흡수 때 본체 머리 위 짧은 글(엔진 UIAccess.Bark, Task 6e ③): {0} = 흡수량. 채우기 = AbsorbText.Overhead.
            ["Absorb.Overhead"] = ("흡수 {0}", "Absorb {0}"),
            ["FullAttack.Name"] = ("수호령 전력 공격", "Guardian Phantom Full Assault"),
            ["FullAttack.Description"] = (
                "신속 행동. 수호령이 켜져 있는 동안 쓸 수 있다. 그 라운드 동안 러시의 라운드당 타수 제한이 풀려 모든 근접 공격마다(빗나가도) 수호령이 2타의 접촉 공격을 한다. 하루 매력 수정치 회(최소 1), 휴식하거나 레벨이 오르면 회복한다.",
                "Swift action, usable while the Guardian Phantom is active. For that round the per-round limit on Rush is lifted and the Guardian Phantom makes 2 touch blows on every melee attack, even on a miss. Usable a number of times per day equal to your Charisma modifier (minimum 1), restored by rest and on level-up."),
            ["Fist.Name"] = ("귀검의 주먹 / Phantom Fist", "Phantom Fist"),
            ["Fist.Description"] = ("수호령 귀검이 꽂아 넣는 역장의 주먹.", "The force fist the Phantom Blade Guardian Phantom drives home."),
            ["FullAttack.Duration"] = ("1 라운드", "1 round"),
            ["SavingThrow.None"] = ("없음", "None"),
             
            // 소유자 결정 2026-11-16(설계 §14 ③): 비용 = 전용 하루 횟수(매력 수정치 회, 최소 1 — 전력 공격과 같은 식, 따로 셈). 흡수 풀을 쓰지 않는다(종전 「풀에서 레벨의 절반(올림)」).
            ["GhostSlash.Name"] = ("귀신참", "Ghost Slash"),
            ["GhostSlash.Description"] = (
                "표준 행동. 수호령이 켜져 있는 동안 30피트 안의 적 하나에게 귀검이 역장의 참격을 날린다. 투사체 수: 5레벨 1발, 10레벨 2발, 15레벨 3발, 20레벨 4발(모두 같은 대상). 투사체마다 따로 원거리 접촉 공격을 굴리고, 명중 보너스는 기본 공격 보너스 + 매력 수정치다(민첩과 무관). 피해는 러시 주먹과 같은 주사위(전사 레벨의 몽크 맨손 표) + 매력 수정치의 역장 피해이고, 치명타는 광선과 같다. 하루 매력 수정치 회(최소 1) 쓸 수 있고, 휴식하거나 레벨이 오르면 회복한다. 이 횟수는 수호령 전력 공격과 따로 세며, 귀신참은 흡수 풀을 쓰지 않는다. 남은 횟수가 없으면 쓸 수 없다.",
                "Standard action, usable while the Guardian Phantom is active. The Phantom Blade hurls slashes of force at one enemy within 30 feet. Projectiles: level 5: 1, level 10: 2, level 15: 3, level 20: 4 (all at the same target). Each projectile makes its own ranged touch attack with your base attack bonus + Charisma modifier (not Dexterity). Each deals force damage equal to the Rush fist dice (the monk unarmed table by fighter level) + your Charisma modifier, and crits like a ray. Usable a number of times per day equal to your Charisma modifier (minimum 1), restored by rest and on level-up. These uses are counted separately from Guardian Phantom Full Assault, and Ghost Slash does not spend the absorption pool. It cannot be used when no uses remain."),
            ["GhostSlash.Duration"] = ("즉시", "Instantaneous"),
            // 귀신참 전용 횟수 자원(GhostSlashResource)의 이름·설명.
            ["GhostSlashUses.Name"] = ("귀신참 횟수", "Ghost Slash Uses"),
            ["GhostSlashUses.Description"] = (
                "귀신참을 쓸 수 있는 남은 횟수. 하루 매력 수정치 회(최소 1)이고 휴식하거나 레벨이 오르면 회복한다. 수호령 전력 공격과 따로 세며, 귀신참은 흡수 풀을 쓰지 않는다.",
                "How many more times you can use Ghost Slash. Usable a number of times per day equal to your Charisma modifier (minimum 1), restored by rest and on level-up. Counted separately from Guardian Phantom Full Assault; Ghost Slash does not spend the absorption pool."),
            ["GhostSlash5.Name"] = ("귀신참: 1발", "Ghost Slash: 1 Projectile"),
            ["GhostSlash5.Description"] = (
                "5레벨: 귀신참을 얻는다 — 표준 행동으로 30피트 안의 적에게 역장 투사체 1발(원거리 접촉, 매력)을 쏜다. 하루 매력 수정치 회(최소 1) 쓸 수 있고, 귀신참은 흡수 풀을 쓰지 않는다.",
                "Level 5: gain Ghost Slash — as a standard action, fire 1 force projectile (ranged touch, Charisma) at an enemy within 30 feet. Usable a number of times per day equal to your Charisma modifier (minimum 1); Ghost Slash does not spend the absorption pool."),
            ["GhostSlash10.Name"] = ("귀신참: 2발", "Ghost Slash: 2 Projectiles"),
            ["GhostSlash10.Description"] = ("10레벨: 귀신참이 한 번에 2발을 쏜다.", "Level 10: Ghost Slash fires 2 projectiles."),
            ["GhostSlash15.Name"] = ("귀신참: 3발", "Ghost Slash: 3 Projectiles"),
            ["GhostSlash15.Description"] = ("15레벨: 귀신참이 한 번에 3발을 쏜다.", "Level 15: Ghost Slash fires 3 projectiles."),
            ["GhostSlash20.Name"] = ("귀신참: 4발", "Ghost Slash: 4 Projectiles"),
            ["GhostSlash20.Description"] = ("20레벨: 귀신참이 한 번에 4발을 쏜다.", "Level 20: Ghost Slash fires 4 projectiles."),
        };
    }
}
