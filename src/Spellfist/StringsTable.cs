using System.Collections.Generic;

namespace Spellfist
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
        // Main.Load 가 Strings.Configure(Table, GameGuids.FighterClass) 로 넘긴다. 규칙 출처 = docs\2026-11-07-spellfist-design.md §4, §11(구현 전 정정).
        // 선택 화면·선택지 설명문(SchoolSelection.Description · School*.Description · ExploitSelection.Description · Exploit*.Description / .AbilityDescription / .BuffDescription)은
        // 리터럴이 아니다 — ChoiceText(ChoiceText.cs)가 SpellfistRules 와 ExploitFacts 에서 만들어 넣는다(C4: 문구 속 수치 = 규칙 상수·본편 블루프린트 값).
        // 익스플로잇 복제본이 아르카니스트 레벨·매력 대신 쓰는 값: Exploits.Build 는 ContextRankConfig 의 클래스(→ 브롤러)·능력치(→ 지능)와
        // ContextCalculateAbilityParams.StatType(→ 지능)만 바꾸고 ReplaceCasterLevel=false 를 그대로 둔다 → 주문서 없는 능력의 암묵 시전자 레벨은
        // 엔진 RuleCalculateAbilityParams.OnTrigger 가 캐릭터 레벨에서 얻는다(IL_0074~009A). 그래서 시전자 레벨 기반 수치(에너지 방패의 저항 값, 내성 DC)는 캐릭터 레벨.
        // 용어는 한글 패치 표기(WrathKoreanMod translation.json, 2026-11-17 대조): 학파 이름(방호학파 …)·익스플로잇 이름(차원 건너기 · 에너지 방패 · 나무 피부 · 얼음 유도탄 …)·
        // 일반 행동 · 내성 굴림 · 임시 생명점 · 자연 방어 · 굴절 · 아르카니스트. 「익스플로잇」「비전 저수지」는 이 모드의 이름이라 그대로 둔다(한글 패치의 비기 · 비전 에너지 저장소).
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();

        static Dictionary<string, (string ko, string en)> Build()
        {
            var d = new Dictionary<string, (string ko, string en)>
            {
                ["Archetype.Name"] = ("주문 격투가", "Spellfist"),
                ["Archetype.Description"] = (
                    "주문 격투가는 주먹과 비전 주문을 한 몸에 담은 브롤러다.\n1레벨에 비전 학파 셋을 골라 그 학파의 메이거스 주문만 아르카니스트처럼 배우고 준비하며, 16레벨에 6레벨 주문에 이른다. 주문 능력치는 지능이다.\n하나의 비전 저수지로 주문을 신속 시전하고 아르카니스트 익스플로잇을 쓰며, 2레벨부터는 접촉 주문을 맨주먹 공격에 실어 터뜨린다.\n대신 기동 훈련, 녹아웃, 어썸 블로우와 개선된 어썸 블로우를 잃는다.\n브롤러 모드의 다른 아키타입과 함께 고를 수 없다.",
                    "The Spellfist is a brawler who carries arcane spells in the same body as the fists.\nAt level 1 you choose three arcane schools and learn and prepare only magus spells of those schools, arcanist-style, reaching 6th-level spells at level 16. Intelligence is the casting ability.\nOne arcane reservoir pays for quickened spells and arcanist exploits, and from level 2 you discharge touch spells through your unarmed attacks.\nIn exchange you lose maneuver training, Knockout, Awesome Blow and Improved Awesome Blow.\nIt cannot be combined with the brawler mod's other archetypes."),
                ["Archetype.DescriptionShort"] = (
                    "학파 셋의 비전 주문을 배우고, 저수지로 신속 시전·익스플로잇을 쓰며, 접촉 주문을 주먹에 싣는다. 기동 훈련·녹아웃·어썸 블로우를 잃는다.",
                    "Learns arcane spells from three schools, spends a reservoir on quickened spells and exploits, and delivers touch spells through the fists. Loses maneuver training, Knockout and Awesome Blow."),

                ["SchoolSelection.Name"] = ("비전 학파", "Arcane School"),
                ["SchoolAbjuration.Name"] = ("방호학파", "Abjuration"),
                ["SchoolConjuration.Name"] = ("소환학파", "Conjuration"),
                ["SchoolDivination.Name"] = ("예지학파", "Divination"),
                ["SchoolEnchantment.Name"] = ("환혹학파", "Enchantment"),
                ["SchoolEvocation.Name"] = ("방출학파", "Evocation"),
                ["SchoolIllusion.Name"] = ("환영학파", "Illusion"),
                ["SchoolNecromancy.Name"] = ("사령학파", "Necromancy"),
                ["SchoolTransmutation.Name"] = ("변환학파", "Transmutation"),

                ["Spellbook.Name"] = ("주문 격투가", "Spellfist"),

                ["Reservoir.Name"] = ("비전 저수지", "Arcane Reservoir"),
                ["Reservoir.Description"] = (
                    "주문 격투가는 비전 에너지를 담는 저수지를 가진다. 최대치는 3 + 브롤러 레벨의 절반(내림) + 지능 수정치이며, 휴식하면 가득 찬다. 이 저수지로 주문을 신속 시전하고 익스플로잇을 쓴다.",
                    "The Spellfist holds arcane energy in a reservoir. Its maximum is 3 + half your brawler level (rounded down) + your Intelligence modifier, and it refills fully when you rest. It pays for quickened spells and for exploits."),

                ["Quicken.Name"] = ("저수지 신속 시전", "Reservoir Quicken"),
                ["Quicken.Description"] = (
                    "켜 두면, 저수지 잔량 이하 레벨(최대 9레벨)의 주문을 신속 행동으로 시전하고, 시전에 성공할 때마다 주문 레벨만큼(최소 1점) 저수지에서 차감한다.\n전투 중에만 적용된다(전투 밖 시전은 보통 행동이고 차감하지 않는다).\n토글을 끄면 대기 중인 신속 시전 명령은 취소된다.\n한계: 신속 효과는 시전자 전체에 걸리므로, 멀티클래스로 다른 주문서가 있으면 그 주문서의 주문도 저수지 차감 없이 신속화된다(주문 격투가 주문서만 다루는 것은 단일 클래스에서만 성립).",
                    "While active, spells of a level up to your remaining reservoir points (at most 9th) are cast as swift actions, and each successful cast drains points equal to the spell level (at least 1).\nWorks only in combat (out of combat, spells use their normal action and drain nothing).\nTurning the toggle off cancels pending quickened casts.\nLimitation: the quicken effect applies to the caster as a whole, so a multiclass character with another spellbook also gets that book's spells quickened without draining the reservoir (limiting it to the Spellfist spellbook only holds for a single-class character)."),

                ["ExploitSelection.Name"] = ("주문 격투가 익스플로잇", "Spellfist Exploits"),
                ["GreaterExploitsMarker.Name"] = ("상급 익스플로잇", "Greater Exploits"),
                ["GreaterExploitsMarker.Description"] = (
                    "11레벨부터 상급 익스플로잇(상급 주문 저항)을 고를 수 있다.",
                    "From level 11 you can choose greater exploits (Greater Spell Resistance)."),

                ["FistSpellstrike.Name"] = ("주먹 주문 타격", "Fist Spellstrike"),
                ["FistSpellstrike.Description"] = (
                    "2레벨부터, 주문 격투가 주문서의 접촉 주문을 시전하면 그 주문을 손에 쥔 채 맨주먹 공격(연타 포함)이나 근접 무기 공격으로 전달할 수 있다.\n공격이 명중하면 주문이 대상에게 터진다. 쥐고 있는 충전은 한 번에 하나다.\n맨손으로는 토글 상태와 관계없이 전달한다. 메이거스 주문 타격 토글을 켜면 근접 무기로도 전달한다.\n메이거스와의 멀티클래스는 지원하지 않는다.",
                    "From level 2, when you cast a touch spell from your Spellfist spellbook you can hold it and deliver it with an unarmed strike (flurry included) or a melee weapon attack.\nWhen the attack hits, the spell discharges on the target. You hold one charge at a time.\nUnarmed strikes deliver the charge whether Spellstrike is on or off. Turn on the magus Spellstrike toggle to deliver it through a melee weapon as well.\nA magus multiclass is not supported."),

                // 익스플로잇 이름 = 한글 패치의 본편 이름(복제한 능력·버프는 본편 이름을 그대로 쓰므로 특성 이름도 같아야 한다). 설명은 ChoiceText.
                ["ExploitDimensionalSlide.Name"] = ("차원 건너기", "Dimensional Slide"),
                ["ExploitArcaneBarrier.Name"] = ("비전 방벽", "Arcane Barrier"),
                ["ExploitEnergyShield.Name"] = ("에너지 방패", "Energy Shield"),
                ["ExploitForceStrike.Name"] = ("역장 타격", "Force Strike"),
                ["ExploitSpellResistance.Name"] = ("주문 저항", "Spell Resistance"),
                ["ExploitGreaterSpellResistance.Name"] = ("상급 주문 저항", "Greater Spell Resistance"),
                ["ExploitShadowVeil.Name"] = ("그림자 장막", "Shadow Veil"),
                ["ExploitWoodenFlesh.Name"] = ("나무 피부", "Wooden Flesh"),
                ["ExploitArmoredMask.Name"] = ("갑옷 가면", "Armored Mask"),
                ["ExploitIceMissile.Name"] = ("얼음 유도탄", "Ice Missile"),
                ["ExploitLightningLance.Name"] = ("번개 창", "Lightning Lance"),
            };
            ChoiceText.AddTo(d);
            return d;
        }
    }
}
