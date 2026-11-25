using System.Collections.Generic;

namespace ShadowSorcerer
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
         
         
        // {n}·{spell} 자리표시는 키를 나눠 쓴다(doubles.1~3, bonus.1~9) — LocalizedString 은 형식 인자를 받지 않는다.
        internal static readonly Dictionary<string, (string ko, string en)> Table = new Dictionary<string, (string ko, string en)>
        {
            ["archetype.name"] = ("그림자 소서러", "Shadow Sorcerer"),
            ["archetype.desc"] = (
                "1레벨부터 그림자를 켜면 분신 하나가 라운드마다 처음 시전한 해로운 소서러 주문을 같은 대상이나 지점에 한 번 되풀이한다. 메아리는 메타매직 없는 기본 주문으로, 보통 피해의 절반을 주며 불신에 성공한 대상에게는 4분의 1을 준다. 그림자가 켜진 동안 슬롯을 쓰는 소서러 주문은 슬롯 1회를 더 쓴다. 혈통 특기 세 개 대신 그림자·환영 주문 아홉 개를 배운다. 시전 능력치는 매력이다.",
                "From 1st level, while toggled on, one shadow repeats your first eligible harmful sorcerer spell each round against the same target or point. The echo casts the base spell without metamagic and normally deals half damage, or a quarter on a successful disbelief save. Paid sorcerer spells cost one extra slot while the shadow is on. You lose three bloodline feats. You learn nine additional shadow and illusion spells. Charisma governs your spellcasting."),
            ["echo.name"] = ("그림자 메아리", "Shadow Echo"),
            ["echo.desc"] = (
                "매 라운드 처음으로 조건을 충족한 해로운 소서러 주문(소마법 제외)의 효과가 끝나면, 0.5초 뒤 그림자 하나가 같은 대상이나 지점에 한 번 더 시전한다. 메아리는 행동과 주문 칸을 쓰지 않는다. 원본의 메타매직은 원본에만 적용된다. 메아리의 주문 내성과 불신에는 기본 주문 단계, 시전 능력치, 학파 집중 등 일반 보정을 쓴다. 일반 피해 주문의 메아리는 불신 실패 시 기본 피해의 절반, 성공 시 4분의 1을 준다. 불신에 성공하면 그림자 판정을 받는 비피해 효과도 무효화된다.",
                "Once per round, 0.5 seconds after your first eligible harmful sorcerer spell finishes, one shadow recasts it at the same target or point. Cantrips do not qualify. The original pays the toggle’s slot cost once. The echo requires no further slot or action. Metamagic applies only to the original. The echo uses the base spell level and normal casting bonuses, including your casting ability and Spell Focus, for its own saves and disbelief. A normal damaging echo deals half of its base damage on a failed Will disbelief save or one quarter on a success. Successful disbelief also negates non-damaging effects governed by disbelief."),
            ["doubles.1"] = ("그림자 분신", "Shadow Double"),
            ["doubles.2"] = ("그림자 분신 2", "Shadow Doubles 2"),
            ["doubles.3"] = ("그림자 분신 3", "Shadow Doubles 3"),
            ["doubles.desc"] = ("소서러 1~20레벨 동안 그림자 하나가 메아리를 시전한다.", "One shadow casts your echo from Sorcerer level 1 through 20."),
            ["slots.name"] = ("그림자에 바친 주문", "Spells Given to Shadow"),
            ["slots.desc"] = ("그림자를 켜면 슬롯을 쓰는 소서러 주문 하나에 해당 단계 슬롯을 1회 더 씁니다(보통 2회). 끄면 평소 비용으로 시전합니다. 하루 슬롯은 일반 소서러와 같습니다.", "While the shadow is on, casting a sorcerer spell that uses a slot costs one additional slot of that level (normally two). Switch it off to cast at the ordinary cost. Daily slots follow the standard sorcerer progression."),
            ["bonus.desc"] = ("그림자·환영 계열 주문 하나를 추가로 안다.", "You learn one additional shadow or illusion spell."),
            ["bonus.1"] = ("그림자 비전: 사라짐", "Shadow Arcana: Vanish"),
            ["bonus.2"] = ("그림자 비전: 거울 분신", "Shadow Arcana: Mirror Image"),
            ["bonus.3"] = ("그림자 비전: 변위", "Shadow Arcana: Displacement"),
            ["bonus.4"] = ("그림자 비전: 그림자 소환", "Shadow Arcana: Shadow Conjuration"),
            ["bonus.5"] = ("그림자 비전: 그림자 환기", "Shadow Arcana: Shadow Evocation"),
            ["bonus.6"] = ("그림자 비전: 환영 부패", "Shadow Arcana: Phantasmal Putrefaction"),
            ["bonus.7"] = ("그림자 비전: 상급 그림자 소환", "Shadow Arcana: Greater Shadow Conjuration"),
            ["bonus.8"] = ("그림자 비전: 상급 그림자 환기", "Shadow Arcana: Greater Shadow Evocation"),
            ["bonus.9"] = ("그림자 비전: 그림자들", "Shadow Arcana: Shades"),
            ["log.dud"] = ("그림자 메아리 소멸", "Shadow echo fades"),
            ["dud.TargetDead"] = ("대상이 이미 쓰러짐", "target already down"),
            ["dud.TargetInvalid"] = ("대상을 지정할 수 없음", "target no longer valid"),
            ["dud.CasterDown"] = ("시전자가 쓰러짐", "caster down"),
            ["dud.WaitTimeout"] = ("원래 주문이 끝나지 않음", "original spell did not finish"),
            ["dud.EchoCastFailed"] = ("메아리 시전 실패", "echo cast failed"),
            ["dud.Reloaded"] = ("다시 불러옴", "reloaded"),
        };
    }
}
