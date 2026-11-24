using System.Collections.Generic;
namespace HellRider
{
    internal static class StringsTable
    {
        internal static readonly Dictionary<string,(string ko,string en)> Table = new Dictionary<string,(string ko,string en)> {
            { "Archetype.name", ("지옥의 기수", "Hell Rider") },
            { "Archetype.desc", ("나이트메어를 타고 돌격하며 배교자 데빌, 지옥 깃발과 지옥의 마법을 부립니다.", "Ride a nightmare and command apostate devils, infernal standards and the magic of Hell.") },
            { "Mount.name", ("나이트메어 탈것", "Nightmare Mount") },
            { "Mount.desc", ("말 동료처럼 성장합니다. 발굽 명중마다 화염 1d4를 더합니다.", "Your nightmare advances as a horse companion. Its hooves deal an additional 1d4 fire damage.") },
            { "Charge.name", ("지옥 돌격", "Infernal Charge") },
            { "Charge.desc", ("도보·기승 돌격의 첫 공격이 적중하거나 빗나가면 주변 10피트의 적을 데빌이 공격합니다. 남는 데빌은 돌격 표적을 칩니다. 데빌 한 마리당 카발리어 3레벨마다 1d6의 절반 화염·절반 부정 피해를 줍니다. 반사 내성 성공 시 절반입니다.", "On the first attack of a mounted or unmounted charge, hit or miss, devils strike enemies within 10 feet; surplus devils strike the charge target. Each deals 1d6 per three cavalier levels, half fire and half unholy. Reflex halves the damage.") },
            { "Banner.name", ("지옥 깃발", "Infernal Standard") },
            { "Banner.desc", ("일반 행동으로 깃발을 세웁니다. 5라운드 동안 매 라운드 20피트 안의 적을 데빌들이 공격합니다. 하루 3 + 매력 수정치회.", "Plant a standard as a standard action. For five rounds devils strike enemies within 20 feet once per round. Daily uses equal 3 + your Charisma modifier.") },
            { "Greater.name", ("상급 지옥 깃발", "Greater Infernal Standard") },
            { "Greater.desc", ("깃발 반경이 30피트로 늘어납니다. 영역 안의 아군은 명중 +2와 공포 면역을 얻습니다.", "Your standard reaches 30 feet. Allies inside gain +2 to attack rolls and immunity to fear.") },
            { "Two.name", ("두 번째 데빌", "Second Devil") },
            { "Two.desc", ("돌격과 깃발이 두 마리의 데빌을 부릅니다.", "Your charge and standard call two devils.") },
            { "Three.name", ("세 번째 데빌", "Third Devil") },
            { "Three.desc", ("돌격과 깃발이 세 마리의 데빌을 부릅니다.", "Your charge and standard call three devils.") },
            { "Breath.name", ("나이트메어의 화염 숨결", "Nightmare's Fire Breath") },
            { "Breath.desc", ("나이트메어의 일반 행동. 15피트 원뿔에 카발리어 2레벨마다 1d6 화염 피해. 반사 내성 성공 시 절반. 1d4라운드 뒤 다시 사용합니다.", "Your nightmare breathes a 15-foot cone of fire as a standard action. Deal 1d6 per two cavalier levels, Reflex half. Recharge in 1d4 rounds.") },
            { "BreathTraining.name", ("나이트메어의 화염 숨결", "Nightmare's Fire Breath") },
            { "BreathTraining.desc", ("나이트메어에게 화염 숨결을 부여합니다.", "Grants the nightmare its fire breath.") },
            { "Cooldown.name", ("숨결 회복 중", "Breath Recharging") },
            { "Cooldown.desc", ("나이트메어가 숨결을 회복하고 있습니다.", "The nightmare is recovering its breath.") },
            { "Command.name", ("상급 명령", "Greater Command") },
            { "Command.desc", ("하루 1회. 범위 안의 적에게 한 가지 명령을 내립니다. 네 명령은 사용 횟수를 공유합니다.", "Once per day, issue one command to nearby enemies. All four commands share one daily use.") },
            { "Ray.name", ("지옥불 광선", "Hellfire Ray") },
            { "Ray.desc", ("매력으로 원거리 접촉 공격을 합니다. 카발리어 레벨당 1d6(최대 15d6), 절반 화염·절반 부정. 15·19레벨에 광선이 하나씩 늘어납니다. 하루 1회 + 양의 매력 수정치 2당 1회.", "Use Charisma for ranged touch attacks. Each ray deals 1d6 per cavalier level (maximum 15d6), half fire and half unholy. Gain extra rays at levels 15 and 19. One daily use plus one per two points of positive Charisma modifier.") },
            { "Brand.name", ("화염 인장", "Firebrand") },
            { "Brand.desc", ("하루 1회. 아군의 무기와 자연 공격에 화염 1d6을 더합니다. 인장을 소모해 화염 광선을 쏠 수 있습니다.", "Once per day, brand allies to add 1d6 fire to weapon and natural attacks. Each bearer can expend their brand to fire a ray.") },
            { "Presence.name", ("압도적인 존재감", "Overwhelming Presence") },
            { "Presence.desc", ("하루 1회. 의지 내성에 실패한 적들이 엎드려 움츠러듭니다. 첫 내성에 성공해도 1라운드 비틀거립니다.", "Once per day, compel enemies who fail a Will save to prostrate themselves and cower. Initial success still staggers them for one round.") },
            { "RayTwo.name", ("지옥불 광선 2발", "Second Hellfire Ray") },
            { "RayTwo.desc", ("지옥불 광선이 같은 적에게 2발 발사됩니다.", "Fire two hellfire rays at the same enemy.") },
            { "RayThree.name", ("지옥불 광선 3발", "Third Hellfire Ray") },
            { "RayThree.desc", ("지옥불 광선이 같은 적에게 3발 발사됩니다.", "Fire three hellfire rays at the same enemy.") },
            { "Apparition.name", ("배교자 데빌", "Apostate Devil") },
            { "Apparition.desc", ("기수의 공격을 나타내는 순간적인 현신입니다.", "An apparition called by the Hell Rider.") },
        };
    }
}
