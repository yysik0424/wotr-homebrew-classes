using System;
using System.Collections.Generic;
using System.Linq;

namespace Golemancer
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
        // Main.Load 가 Strings.Configure(Table, Guids.Game.AlchemistClass) 로 넘긴다. 내용 출처 = docs\2026-11-07-golemancer-design.md §1·§4.1~§4.3·§4.6·§12.
        // 아키타입 설명은 설계 전체(골렘·재질·합체 포함)를 적는다 — 기능은 이후 Task 에서 붙는다(스피릿 웨폰 샤먼 StringsTable 과 같은 방침).
        internal static readonly Dictionary<string, (string ko, string en)> Table = Merge(new Dictionary<string, (string ko, string en)>
        {
            ["Archetype.Name"] = ("골렘술사", "Golemancer"),
            ["Archetype.Description"] = (
                "골렘술사는 골렘을 만들어 앞에 세우는 연금술사다. 1레벨에 나무 골렘이 곁에서 적을 붙잡아 두고, 5·10·15레벨에 골렘의 재질을 골라 바꾼다. 10레벨부터는 골렘을 입어 직접 맞고 직접 때린다. 1레벨에 변환을 뺀 일곱 학파 가운데 반대 학파 셋을 고르며, 반대 학파 추출물은 준비할 때 슬롯 2칸을 쓴다. 하루 폭탄 수의 기본항(연금술사 레벨 + 지능 수정치)이 절반으로 줄고(내림), 특기·장비·신화로 늘어나는 폭탄은 그대로다. 대신 독 내성과 독 면역을 잃고, 인지약 발견(상위·최상위 포함)을 고를 수 없다. 폭탄 주사위·변이약·추출물·발견은 그대로다.",
                "The Golemancer is an alchemist who builds a golem and sends it to the front. At level 1 a wood golem fights at your side and holds enemies in place; at levels 5, 10 and 15 you choose a new material for it. From level 10 you can wear the golem and take and deal the blows yourself. At level 1 you choose three opposition schools among the seven schools other than transmutation; extracts of an opposition school take two slots to prepare. The base of your daily bombs (alchemist level + Intelligence modifier) is halved (rounded down); extra bombs from feats, equipment and mythic sources are unchanged. In exchange you lose poison resistance and poison immunity, and cannot choose the cognatogen discoveries (including greater and grand). Bomb damage, mutagens, extracts and discoveries are unchanged."),
            ["Archetype.DescriptionShort"] = (
                "골렘을 부리고 입는 연금술사. 반대 학파 셋, 폭탄 기본항 절반. 독 내성·면역과 인지약을 잃는다.",
                "An alchemist who commands and wears a golem. Three opposition schools, halved base bombs. Loses poison resistance and immunity and the cognatogens."),
            // 반대 학파(설계 §4.3, 정찰 §1): 선택 1개를 1레벨에 3번 둔다. 같은 학파는 엔진 규칙으로 두 번 못 고른다(CanSelect 랭크 검사 @0083~009B).
            ["OppositionSelection.Name"] = ("골렘술사 반대 학파", "Golemancer Opposition School"),
            // 선택 화면 설명("OppositionSelection.Description")과 학파 7개 설명("School.<학파>.Description")은 ChoiceText 가 규칙표에서 만든다(2026-11-17).
            ["School.Abjuration.Name"] = ("반대 학파 — 방호", "Opposition School — Abjuration"),
            ["School.Conjuration.Name"] = ("반대 학파 — 소환", "Opposition School — Conjuration"),
            ["School.Divination.Name"] = ("반대 학파 — 예지", "Opposition School — Divination"),
            ["School.Enchantment.Name"] = ("반대 학파 — 환혹", "Opposition School — Enchantment"),
            ["School.Evocation.Name"] = ("반대 학파 — 방출", "Opposition School — Evocation"),
            ["School.Illusion.Name"] = ("반대 학파 — 환영", "Opposition School — Illusion"),
            ["School.Necromancy.Name"] = ("반대 학파 — 사령", "Opposition School — Necromancy"),
            // 폭탄 절반(설계 §4.2): 일일 폭탄 = floor((연금술사 레벨 + 지능 수정치) / 2) + 다른 출처 증가분.
            ["BombHalf.Name"] = ("절반의 폭탄", "Halved Bombs"),
            ["BombHalf.Description"] = (
                "골렘술사가 하루에 만드는 폭탄의 기본 수(연금술사 레벨 + 지능 수정치)가 절반이 된다(내림, 최소 0). 특기·장비·신화로 늘어나는 폭탄은 그대로 더한다. 일시적인 지능 증가는 기본 수에 들어가지 않는다(본편과 같다).",
                "The base number of bombs the Golemancer can make per day (alchemist level + Intelligence modifier) is halved (rounded down, minimum 0). Extra bombs from feats, equipment and mythic sources are added unchanged. Temporary Intelligence bonuses do not count toward the base (as in the base game)."),
            // 골렘 펫(설계 §4.4·§4.7·§12): 1레벨 나무 골렘, HD = 연금술사 레벨, 도발 오라, 부서짐·휴식 수리.
            ["Golem.Name"] = ("골렘", "Golem"),
            ["GolemFeature.Name"] = ("골렘", "Golem"),
            ["GolemCall.Name"] = ("골렘 호출", "Call Golem"),
            ["GolemCall.Description"] = ("표준 행동으로 대기 중인 골렘을 자신의 곁으로 부른다. 골렘은 이전 체력과 재질을 유지한다. 전투 중에도 사용할 수 있다.", "As a standard action, call your waiting golem to your side. It keeps its hit points and material. You can use this ability during combat."),
            ["GolemCall.Unavailable"] = ("호출하려면 살아 있고 부서지지 않은 골렘이 대기 중이어야 한다. 합체, 폭주, 탑승 또는 스토리상 동료가 숨겨진 동안에는 사용할 수 없다.", "Your living, unbroken golem must be waiting. You cannot call it during fusion, overdrive, while mounted, or while the story hides companions."),
            ["GolemDismiss.Name"] = ("골렘 귀환", "Dismiss Golem"),
            ["GolemDismiss.Description"] = ("전투 밖에서 표준 행동으로 골렘을 돌려보낸다. 골렘은 현재 체력과 재질을 유지하고 다시 호출할 때까지 기다린다.", "Outside combat, use a standard action to send your golem away. It keeps its current hit points and material and waits until called again."),
            ["GolemDismiss.Unavailable"] = ("전투 밖에서 살아 있고 부서지지 않은 골렘이 나와 있어야 한다. 합체, 폭주 또는 탑승 중에는 귀환시킬 수 없다.", "Your living, unbroken golem must be present and out of combat. You cannot dismiss it during fusion, overdrive, or while mounted."),
            ["GolemFeature.Description"] = (
                "골렘술사는 나무 골렘과 결속한다. 골렘은 대기하다가 골렘 호출을 쓰면 곁에 나타난다. 전투 밖에서는 골렘 귀환으로 돌려보낼 수 있다. 골렘의 히트 다이스는 연금술사 레벨과 같다.\n구조물이라 수면·마비·공포·정신·독 등에 면역이며 치유 추출물로는 낫지 않는다.\n골렘 주위 10피트 안의 적은 골렘이 아닌 대상을 공격할 때 명중 −2를 받는다.\n골렘이 쓰러지면 부서지고, 성공한 휴식에서 되살아나 모두 수리된다.\n동물 동료와 함께 쓸 수 없다.",
                "The Golemancer bonds with a wood golem. It waits until called to their side with Call Golem. Outside combat, Dismiss Golem sends it away again. The golem's Hit Dice equal the alchemist level.\nAs a construct it is immune to sleep, paralysis, fear, mind-affecting effects, poison and more, and alchemical healing does not mend it.\nEnemies within 10 feet of the golem take a -2 penalty on attack rolls against targets other than the golem.\nA golem reduced to 0 hit points is broken; a successful rest revives and fully repairs it.\nCannot be combined with an animal companion."),
            ["MaterialWood.Name"] = ("골렘 재질 — 나무", "Golem Material — Wood"),
            // 나무 설명("MaterialWood.Description")·한 줄 요약("MaterialWood.Summary")은 ChoiceText 가 만든다(2026-11-17).
            // 재질(설계 §4.5·§12): 5·10·15레벨 선택, 이전 재질 교체. 버프 이름 = 재질 이름(같은 키).
            // 선택 화면 이름·설명("MaterialSelection.<레벨>.Name" / ".Description")과 재질 설명("Material.<id>.Description")·한 줄 요약("Material.<id>.Summary" — 재질 버프 툴팁·짧은 설명 칸)은 ChoiceText 가 만든다(2026-11-17).
            ["MaterialSelection.Summary"] = ("골렘의 재질을 새로 고른다. 이전 재질은 사라진다.", "Choose a new material for the golem. The previous material is replaced."),
            ["Material.stone.Name"] = ("골렘 재질 — 돌", "Golem Material — Stone"),
            ["Material.iron.Name"] = ("골렘 재질 — 철", "Golem Material — Iron"),
            ["Material.brass.Name"] = ("골렘 재질 — 황동", "Golem Material — Brass"),
            ["Material.gold.Name"] = ("골렘 재질 — 황금", "Golem Material — Gold"),
            ["Material.autumn.Name"] = ("골렘 재질 — 가을", "Golem Material — Autumn"),
            ["Material.summer.Name"] = ("골렘 재질 — 여름", "Golem Material — Summer"),
            ["Material.adamantine.Name"] = ("골렘 재질 — 아다만틴", "Golem Material — Adamantine"),
            ["Material.greatergold.Name"] = ("골렘 재질 — 상위 황금", "Golem Material — Greater Gold"),
            ["Material.mithral.Name"] = ("골렘 재질 — 미스랄", "Golem Material — Mithral"),
            // 소유자 결정 2026-11-17: 철 = 화염 면역만(회복 없음). 키 이름 FireRepair.* 는 옛 이름 그대로(문구만 바뀜).
            ["FireRepair.Name"] = ("화염 면역", "Fire Immunity"),
            ["FireRepair.Description"] = ("화염 피해를 받지 않는다.", "Takes no fire damage."),
            ["PrecisionImmunity.Name"] = ("급소 공격 면역", "Precision Immunity"),
            ["PrecisionImmunity.Description"] = ("급소 공격 등 정밀 피해에 면역이다.", "Immune to precision damage such as sneak attacks."),
            ["StoneSlowAura.Name"] = ("감속 오라", "Slowing Aura"),
            ["StoneSlowAura.Description"] = ("주위 10피트에 들어온 적은 의지 내성에 실패하면 7라운드 동안 감속된다.", "Enemies entering within 10 feet are slowed for 7 rounds on a failed Will save."),
             
            ["FusionFeature.Name"] = ("골렘 합체", "Golem Fusion"),
            ["FusionFeature.Description"] = (
                "10레벨부터 골렘술사는 신속 행동으로 골렘을 입는다.\n골렘은 사라지고 골렘술사가 골렘의 모습·크기·슬램 2회·자연 방어·DR·힘을 얻으며, 도발 오라도 골렘술사에게 옮겨 온다.\n받는 피해는 골렘의 HP 가 먼저 받고, 골렘 HP 가 다하면 합체가 풀리며 남은 피해를 골렘술사가 받는다.\n합체 중에는 주문·추출물·폭탄·변이약을 쓸 수 없다.\n끄기·휴식·저장·지역 이동·다른 변신·골렘술사의 죽음에 합체가 풀린다. 기절·마비·의식 없음으로는 풀리지 않는다.",
                "From level 10 the Golemancer can wear the golem as a swift action.\nThe golem disappears; the Golemancer takes its shape, size, two slams, natural armor, DR and Strength, and the taunting aura moves to the Golemancer.\nDamage taken goes to the golem's hit points first; when they run out the fusion ends and the rest hits the Golemancer.\nWhile fused you cannot use spells, extracts, bombs or mutagens.\nTurning it off, resting, saving, changing area, another polymorph or the Golemancer's death ends the fusion. Being stunned, paralyzed or unconscious does not."),
            ["FusionToggle.Name"] = ("골렘 합체", "Golem Fusion"),
            ["FusionToggle.Description"] = (
                "신속 행동으로 골렘을 입는다(끄면 다음 순간 풀린다). 골렘이 부서져 있거나 저장 중이거나 이미 다른 변신 중이면 켤 수 없다.",
                "Wear the golem as a swift action (turning it off ends the fusion a moment later). Cannot be turned on while the golem is broken, while saving, or while already polymorphed by something else."),
            ["FusionBuff.Name"] = ("골렘 합체 중", "Fused with the Golem"),
            ["FusionBuff.Description"] = ("골렘을 입고 있다. 받는 피해를 골렘 HP 가 먼저 받는다.", "Wearing the golem. Damage taken goes to the golem's hit points first."),
            // T7(owner approval 2026-11-14): 응급 수리 · 20레벨 면역 공유.
            ["Repair.Name"] = ("응급 수리", "Emergency Repair"),
            ["Repair.Description"] = (
                "표준 행동으로 준비해 둔 추출물 하나를 써서 인접한 골렘(합체 중이면 입고 있는 골렘)을 고친다. 골렘이 최대 HP × (추출물 레벨 × 10)% 만큼(내림) 회복하고 부서짐이 풀린다. 부서진 골렘은 그만큼의 HP 로 되살아난다. 그 레벨에 준비한 추출물이 없거나 고칠 것이 없으면 쓸 수 없다.",
                "As a standard action, spend one prepared extract to repair the adjacent golem (or the worn golem while fused). The golem regains max HP x (extract level x 10)% (rounded down) and is no longer broken; a broken golem returns with that many hit points. Cannot be used without a prepared extract of that level or when there is nothing to repair."),
            ["Repair.L1.Name"] = ("응급 수리 (1레벨 추출물)", "Emergency Repair (1st-level extract)"),
            ["Repair.L2.Name"] = ("응급 수리 (2레벨 추출물)", "Emergency Repair (2nd-level extract)"),
            ["Repair.L3.Name"] = ("응급 수리 (3레벨 추출물)", "Emergency Repair (3rd-level extract)"),
            ["Repair.L4.Name"] = ("응급 수리 (4레벨 추출물)", "Emergency Repair (4th-level extract)"),
            ["Repair.L5.Name"] = ("응급 수리 (5레벨 추출물)", "Emergency Repair (5th-level extract)"),
            ["Repair.L6.Name"] = ("응급 수리 (6레벨 추출물)", "Emergency Repair (6th-level extract)"),
            ["Repair.Unavailable"] = ("골렘이 인접해 있고(합체 중이면 거리 무관), 고칠 것이 있으며, 그 레벨에 준비한 추출물이 있어야 한다.", "Needs an adjacent golem (any distance while fused) with something to repair and a prepared extract of that level."),
            ["SharedImmunityFeature.Name"] = ("면역 공유", "Shared Immunity"),
            // 면역 공유 특성 설명("SharedImmunityFeature.Description")은 ChoiceText 가 카탈로그에서 만든다(2026-11-17).
            ["SharedImmunity.Name"] = ("면역 공유", "Shared Immunity"),
            ["SharedImmunity.Description"] = ("입고 있는 골렘 재질의 추가 면역을 함께 받는다.", "Sharing the worn golem material's additional immunities."),
            ["TauntBuff.Name"] = ("골렘의 도발", "Golem's Taunt"),
            ["TauntBuff.Description"] = (
                "골렘에게 붙잡혀 있다. 골렘이 아닌 대상을 공격할 때 명중 −2.",
                "Held in place by a golem: -2 on attack rolls against targets other than the golem."),
            // 소유자 결정 2026-11-16: 골렘 폭주(5레벨) · 추가 슬램(평소 11레벨부터 2회). 횟수·지속·학파 효과가 들어가는 문구(능력 설명·변형 이름/설명·학파 효과 버프)는
            //   표에서 생성한다(OverdriveRules.GeneratedStrings — 아래 Merge 가 합친다). 여기에는 수치가 없는 문구만 둔다.
            ["Overdrive.Name"] = ("골렘 폭주", "Golem Overdrive"),
            ["OverdriveBuff.Name"] = ("골렘 폭주", "Golem Overdrive"),
            ["OverdriveBuff.Description"] = (
                "골렘에게 추출물을 주입했다. 지속 동안 골렘(합체 중이면 골렘술사)의 슬램 횟수가 늘고 소모한 추출물 학파의 효과가 붙는다. 골렘이 부서지면 즉시 끝난다.",
                "An extract has been injected into the golem. For the duration the golem (the Golemancer while fused) makes more slam attacks and gains the effect of the spent extract's school. Ends at once if the golem is broken."),
            ["Overdrive.Block.Unknown"] = ("골렘이나 추출물의 상태를 읽을 수 없다(모드 로그에 오류가 남았다).", "The golem or the extracts could not be read (an error was written to the mod log)."),
            ["Overdrive.Block.AlreadyActive"] = ("이미 폭주 중이다.", "An overdrive is already running."),
            ["Overdrive.Block.NoGolem"] = ("골렘이 없다.", "There is no golem."),
            ["Overdrive.Block.GolemBroken"] = ("골렘이 부서져 있다.", "The golem is broken."),
            ["Overdrive.Block.FusionBusy"] = ("합체가 바뀌는 중이다.", "The fusion is changing."),
            ["Overdrive.Block.NoSlot"] = ("이 레벨·이 학파의 준비한 추출물이 없다.", "No prepared extract of this level and school."),
            ["SlamLimbs.Name"] = ("추가 슬램", "Additional Slams"),
            ["SlamLimbs.Description"] = ("한 라운드에 슬램을 더 친다.", "Makes additional slam attacks each round."),
        });

        // 표에서 생성한 문구를 합친다(키가 겹치면 던진다) + 골렘 설명에 기준 수치표와 평소 슬램 횟수(Dials.SlamCounts)를, 합체 설명에 힘 보너스 규칙과 폭주 중 횟수를 붙인다.
        // 골렘 설명은 슬램 횟수 문장으로 끝난다(시험 `strings: golem description ends with the normal slam counts`).
        static Dictionary<string, (string ko, string en)> Merge(Dictionary<string, (string ko, string en)> t)
        {
            foreach (var g in OverdriveRules.GeneratedStrings()) t.Add(g.Key, (g.Ko, g.En)); // 중복 키 → ArgumentException
            foreach (var g in ChoiceText.GeneratedStrings()) t.Add(g.Key, (g.Ko, g.En));      // 선택 화면·선택지 설명(2026-11-17) — 중복 키 → ArgumentException
            var n = OverdriveRules.NormalCountSummary;
            var table = ChoiceText.GolemBaseTable();
            var golem = t["GolemFeature.Description"];
            t["GolemFeature.Description"] = (golem.ko + "\n" + table.Key + "\n골렘은 한 라운드에 슬램을 친다: 연금술사 " + n.Key + ".", golem.en + "\n" + table.Value + "\nThe golem makes slam attacks each round: " + n.Value + " by alchemist level.");
            var str = ChoiceText.FusionStrengthRule();
            var fusion = t["FusionFeature.Description"];
            t["FusionFeature.Description"] = (fusion.ko + "\n" + str.Key + "\n골렘 폭주 중에는 합체한 골렘술사의 슬램 횟수가 폭주 값을 따른다(최소 " + GolemRules.Dials.FusionHandSlams + "회).", fusion.en + "\n" + str.Value + "\nDuring a golem overdrive the fused Golemancer's slams follow the overdrive number (minimum " + GolemRules.Dials.FusionHandSlams + ").");
            return t;
        }
    }
    // [순수] 레벨업·캐릭터 생성 선택 화면의 설명문(2026-11-17 소유자 피드백 「선택지 설명이 한 줄」). 선택 화면의 상세 창은 선택지 블루프린트의 m_Description 전체를 읽는다
    //  (엔진 CharGenFeatureSelectorItemVM.TooltipTemplate → TooltipTemplateLevelUpFeature.Prepare → BlueprintUnitFact.get_Description; m_DescriptionShort 는 읽지 않는다). 줄바꿈 = "\n".
    //  만드는 것: 재질 설명 10(나무 + 고르는 9) · 재질 한 줄 요약 10(재질 버프 13개의 툴팁과 재질 특성의 짧은 설명 칸 — 긴 설명과 키를 나눈다) · 재질 선택 화면 이름·설명 3 ·
    //  반대 학파 선택 화면 설명 1 · 반대 학파 설명 7 · 20레벨 면역 공유 설명 1 + 골렘·합체 설명에 붙는 줄(GolemBaseTable · FusionStrengthRule).
    // 수치 잠금(C4): 이 형식의 문구 틀(리터럴)에는 숫자를 쓰지 않는다 — 수치는 전부 {n} 자리에 규칙(GolemRules · MaterialCatalog · StatRecipe · SlamRules · OverdriveRules · CapstoneRules)에서 끼워 넣는다.
    //  규칙에 없는 값(본편 블루프린트·엔진이 정하는 값)은 아래 Vanilla 의 인스턴스 칸에 출처와 함께 둔다(수치 정적 필드는 `dial: single block` 이 금지) — 시험 `strings: choice text` 가 blueprints.zip 과 대조한다.
    //  시험이 잠그는 것: (1) 이 형식의 문자열 리터럴에 {n} 밖의 숫자 없음 (2) 만들어진 문구의 수치 열 == 손 전사 기대표 (3) 재질 수치 == StatRecipe.Target (4) ko/en 수치 집합·줄 수 일치 (5) 설명이 서로 다름.
    // 틀에 넘긴 인자를 틀이 쓰지 않으면 던진다(조용히 빠진 수치 없음). 카탈로그에 있는데 여기 문구가 없는 면역·특수 능력·재질·학파도 던진다(형식 초기화 실패 = 빌드 실패).
    static class ChoiceText
    {
        // 한/영 한 쌍. 인자로 넘기면 그 언어 쪽이 들어간다.
        sealed class P
        {
            public readonly string Ko, En;
            public P(string ko, string en)
            {
                if (string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en)) throw new ArgumentException("Golemancer: choice text pair with an empty side");
                Ko = ko; En = en;
            }
        }

        static P F(string ko, string en, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args", "Golemancer: choice text argument " + i + " is null (" + en + ")");
                string slot = "{" + i + "}";
                if (!ko.Contains(slot) || !en.Contains(slot)) throw new ArgumentException("Golemancer: choice text template does not use argument " + i + " in both languages (" + en + ")");
            }
            return new P(string.Format(ko, args.Select(a => a is P ? (object)((P)a).Ko : a).ToArray()), string.Format(en, args.Select(a => a is P ? (object)((P)a).En : a).ToArray()));
        }

        // 줄 묶음(null = 그 줄 없음). 한 줄도 없으면 던진다.
        static P Lines(IEnumerable<P> lines)
        {
            var l = lines.Where(x => x != null).ToArray();
            if (l.Length == 0) throw new ArgumentException("Golemancer: choice text without lines");
            return new P(string.Join("\n", l.Select(x => x.Ko)), string.Join("\n", l.Select(x => x.En)));
        }

        static P Join(string sepKo, string sepEn, IEnumerable<P> parts)
        {
            var l = parts.ToArray();
            if (l.Length == 0 || l.Any(x => x == null)) throw new ArgumentException("Golemancer: choice text list is empty or has a null entry");
            return new P(string.Join(sepKo, l.Select(x => x.Ko)), string.Join(sepEn, l.Select(x => x.En)));
        }

        // ---------------- 규칙에 없는 값(본편 블루프린트·엔진) ----------------
        // 출처는 전부 blueprints.zip 직독(2026-11-17) — 시험 `strings: choice text` 가 같은 파일과 대조한다. 우리 코드는 이 값을 계산에 쓰지 않는다(본편 능력을 그대로 참조하거나 복제한다) → 여기 값은 「설명에 옮겨 적은 사본」이다.
        sealed class Vanilla
        {
            public readonly int SplinterRadiusFeet = 20;                 // WoodGolemSplintering AbilityTargetsAround m_Radius 20
            public readonly Dice SplinterDamage = new Dice(6, 6);        // WoodGolemSplintering ContextActionDealDamage 6d6, 반사 반감
            public readonly int SlowRounds = 7;                          // StoneGolemSlow → SlowBuff 7 라운드(우리 오라는 빌드 때 이 값을 본편에서 읽는다 — Materials.VanillaSlowDuration)
            public readonly int IronCloudRadiusFeet = 5;                 // IronGolemCloudArea Size 5
            public readonly int IronCloudRounds = 1;                     // IronGolemBreathWeapon ContextActionSpawnAreaEffect 1 라운드
            public readonly int IronPoisonRounds = 4;                    // IronGolemPoisonBuff BuffPoisonStatDamage Ticks 4(구름이 거는 지속도 4 라운드)
            public readonly Dice IronPoisonDamage = new Dice(1, 4);      // IronGolemPoisonBuff 건강 1d4
            public readonly int BrassCloudRadiusFeet = 10;               // IncendiaryCloudArea(황동 골렘) Size 10
            public readonly Dice BrassCloudRounds = new Dice(1, 6);      // BrassGolemBreathWeapon ContextActionSpawnAreaEffect 1d6 라운드
            public readonly Dice BrassFireDamage = new Dice(6, 6);       // IncendiaryCloudArea Round: 화염 6d6, 반사 반감
            public readonly int SurgeFire = 20, SurgeAcid = 40, SurgeElectricity = 80; // PrismaticSurgeMelee / Ranged RandomAction: 화염 20 · 산성 40 · 전기 80(주사위 없음), 반사 반감
            public readonly int SurgePoisonRounds = 6;                   // PrismaticSurgePoisonBuff Ticks 6
            public readonly Dice SurgePoisonDamage = new Dice(1, 4);     // PrismaticSurgePoisonBuff 건강 1d4
            public readonly int AutumnAuraRadiusFeet = 10, SummerAuraRadiusFeet = 10; // GolemAutumnAura / GolemSummerAura Size 10
            public readonly Dice AuraRoundsDice = new Dice(1, 4);        // 두 오라의 속박(가을은 처참한 절망도) 지속 1d4 + 1 라운드
            public readonly int AuraRoundsBonus = 1;
            public readonly int GrabManeuverBonus = 4;                   // GolemAutumnGrabFeature ManeuverBonus Grapple +4
            public readonly Dice GrabDamage = new Dice(2, 10);           // GolemAutumnGrabFeature 잡기 성공 때 2d10 + 힘 수정치 × 1.5(ContextRankConfig StatBonus HalfMore)
            public readonly Dice GrappledDamage = new Dice(4, 10);       // GolemAutumnGrappledBuff NewRound 4d10 + 힘 수정치 × 2(MultiplyByModifier, m_StepLevel 2)
            public readonly int FastHealingAdamantine = 5;               // FastHealing5 AddEffectFastHealing Heal 5
            public readonly int FastHealingMithral = 20;                 // FastHealing20 AddEffectFastHealing Heal 20
            public readonly int BaseSpeedFeet = 30;                      // CR6_GolemWood(펫 원형) Speed 30 — 미스랄은 여기에 Material.SpeedBonus 를 더한다
            public readonly int OppositionSlots = 2;                     // 엔진: 반대 학파 주문은 준비할 때 그 레벨의 칸 2개(본편 OppositionSchoolSelection 설명 「two spell slots of that level」, 소모 = SpellSlot.Spend 의 연결 칸)
            public readonly int AlchemistExtracts = 71;                  // 본편 AlchemistSpellList 의 추출물 수(다른 모드가 더한 추출물은 세지 않는다)
        }

        // 본편 연금술사 추출물 목록의 학파별 개수와 대표 추출물(이름 = enGB / 한글 패치 표기, 2026-11-17 직독). 시험이 개수·GUID·레벨·학파를 blueprints.zip 과, 이름을 현지화 파일과 대조한다.
        internal sealed class Extract
        {
            public readonly string Guid, Ko, En;
            public readonly int Level;
            public Extract(string guid, int level, string ko, string en) { Guid = guid; Level = level; Ko = ko; En = en; }
        }

        internal sealed class SchoolExtracts
        {
            public readonly string School;
            public readonly int Count;
            public readonly Extract[] Examples;
            public SchoolExtracts(string school, int count, params Extract[] examples) { School = school; Count = count; Examples = examples; }
        }

        internal static SchoolExtracts[] ExtractTable()
        {
            return new[]
            {
                new SchoolExtracts("Abjuration", 12, new Extract("ef768022b0785eb43a18969903c537c4", 1, "방패", "Shield"), new Extract("21ffef7791ce73f468b6fca4d9371e8b", 2, "에너지 저항", "Resist Energy"), new Extract("c66e86905f7606c4eaa5c774f0357b2b", 4, "돌가죽", "Stoneskin")),
                new SchoolExtracts("Conjuration", 14, new Extract("5590652e1c2225c4ca30c4a699ab3649", 1, "가벼운 상처 치료", "Cure Light Wounds"), new Extract("e84fc922ccf952943b5240293669b171", 2, "상태회복, 하급", "Restoration, Lesser"), new Extract("5da172c4c89f9eb4cbb614f3a67357d3", 6, "치유", "Heal")),
                new SchoolExtracts("Divination", 4, new Extract("2c38da66e5a599347ac95b3294acbe00", 1, "진실의 일격", "True Strike"), new Extract("30e5dc243f937fc4b95d2f8f4e1b7ff3", 2, "투명화 탐지", "See Invisibility"), new Extract("4cf3d0fae3239ec478f51e86f49161cb", 6, "진실의 시야", "True Seeing")),
                new SchoolExtracts("Enchantment", 3, new Extract("03a9630394d10164a9410882d31572f0", 2, "지원", "Aid"), new Extract("5ab0d42fb68c9e34abae4921822b9d63", 3, "영웅심", "Heroism"), new Extract("97b991256e43bb140b263c326f690ce2", 3, "격노", "Rage")),
                new SchoolExtracts("Evocation", 1, new Extract("5e826bcdfde7f82468776b55315b2403", 4, "드래곤의 숨결", "Dragon's Breath")),
                new SchoolExtracts("Illusion", 4, new Extract("14ec7a4e52e90fa47a4c8d63c69fd5c1", 2, "흐릿한 형체", "Blur"), new Extract("89940cde01689fb46946b2f8cd7b66b7", 2, "투명화", "Invisibility"), new Extract("903092f6488f9ce45a80943923576ab3", 3, "환위", "Displacement")),
                new SchoolExtracts("Necromancy", 4, new Extract("7a5b5bf845779a941a67251539545762", 2, "거짓 생명", "False Life"), new Extract("e9cc9378fd6841f48ad59384e79e9953", 4, "죽음 방호", "Death Ward"), new Extract("3167d30dd3c622c46b0c0cb242061642", 6, "사안", "Eyebite")),
                new SchoolExtracts("Transmutation", 29),
            };
        }

        static SchoolExtracts ExtractsOf(string school)
        {
            var hit = ExtractTable().Where(x => x.School == school).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("Golemancer: choice text has " + hit.Length + " extract row(s) for school '" + school + "'");
            return hit[0];
        }

        // 반대 학파로 고를 수 있는 학파 = 여덟 학파에서 변환을 뺀 것(설계 §4.3 — 게임 쪽 OppositionSchools.Rows 와 같은 집합이어야 한다: 시험이 ScenarioPlan.OppositionSchools 와 대조).
        const string NotAnOpposition = "Transmutation";
        internal static string[] OppositionChoices() { return OverdriveRules.Schools.Where(s => s != NotAnOpposition).ToArray(); }

        // ---------------- 이름표 ----------------
        static P MaterialName(Material m)
        {
            switch (m.Id)
            {
                case "wood": return new P("나무", "wood");
                case "stone": return new P("돌", "stone");
                case "iron": return new P("철", "iron");
                case "brass": return new P("황동", "brass");
                case "gold": return new P("황금", "gold");
                case "autumn": return new P("가을", "autumn");
                case "summer": return new P("여름", "summer");
                case "adamantine": return new P("아다만틴", "adamantine");
                case "greatergold": return new P("상위 황금", "greater gold");
                case "mithral": return new P("미스랄", "mithral");
            }
            throw new KeyNotFoundException("Golemancer: choice text has no name for material '" + m.Id + "'");
        }

        static P SizeName(string size)
        {
            switch (size)
            {
                case "Medium": return new P("중형", "Medium");
                case "Large": return new P("대형", "Large");
                case "Huge": return new P("초대형", "Huge");
            }
            throw new KeyNotFoundException("Golemancer: choice text has no name for size '" + size + "'");
        }

        // 학파 이름: 한글 패치 표기 「방호학파」 = 폭주 문구의 학파 이름(OverdriveRules.SchoolName) + 「학파」.
        static P SchoolFull(string school)
        {
            var n = OverdriveRules.SchoolName(school);
            return F("{0}학파", "{0}", new P(n.Key, n.Value));
        }

        // 추가 면역(카탈로그 ImmunityGuids + 우리 면역형 특성 = CapstoneRules.SharedImmunities 와 같은 목록)의 이름.
        // detail = false: 한 줄 요약(버프 툴팁)용 짧은 이름.
        static P ImmunityLabel(string guid, bool detail)
        {
            switch (guid)
            {
                case Guids.Game.ImmunityToPetrification: return new P("석화", "petrification");
                case Guids.Game.AcidImmunity: return new P("산성", "acid");
                case Guids.Game.ElectricityImmunity: return new P("전기", "electricity");
                case Guids.Game.FireImmunity: return new P("화염", "fire");
                case Guids.Game.ColdImmunity: return new P("냉기", "cold");
                case Guids.Game.BlindnessImmunity: return new P("실명", "blindness");
                case Guids.Game.ImmunityToConfusion: return new P("혼란", "confusion");
                case Guids.Game.ImmunityToCritical: return new P("치명타", "critical hits");
                case Guids.Game.FreedomOfMovementBuffPermanent: return new P("이동의 자유(상시)", "freedom of movement (constant)");
                case Guids.Game.Evasion: return detail ? new P("회피(반사 내성 굴림에 성공하면 절반 대신 피해 없음)", "evasion (no damage instead of half on a successful Reflex save)") : new P("회피", "evasion");
                case Guids.FireRepairFeature: return detail ? new P("화염(화염 피해를 받지 않음)", "fire (takes no fire damage)") : new P("화염", "fire");
                case Guids.PrecisionImmunityFeature: return new P("암습 등 정밀 피해", "precision damage such as sneak attacks");
            }
            throw new KeyNotFoundException("Golemancer: choice text has no label for immunity " + guid);
        }

        static P[] ImmunityLabels(Material m, bool detail) { return CapstoneRules.SharedImmunities(m).Select(g => ImmunityLabel(g, detail)).ToArray(); }

        // 특수 능력(카탈로그 SpecialAbilities): 짧은 이름 + 상세 줄. 본편 이름 = 한글 패치 표기(숨결 무기 · 무지개 급류 · 가시 발사 · 죄여오는 덩굴 · 가시 돋친 절망의 오라 · 여름 더위의 오라 · 투명화 탐지 · 베어 가르기 · 전투 반사).
        sealed class Ability
        {
            public readonly P Name, Detail;
            public Ability(P name, P detail) { Name = name; Detail = detail; }
        }

        static P SurgeEffects(Vanilla v)
        {
            return F("여섯 효과 가운데 하나가 무작위로 나온다: 화염 피해 {0} / 산성 피해 {1} / 전기 피해 {2}(셋 다 반사 내성 굴림 성공 시 절반), 중독(인내 내성, {3}라운드 동안 라운드마다 건강 피해 {4}), 석화(인내 내성, 영구), 광기(의지 내성, 영구).",
                "one of six effects at random: {0} fire / {1} acid / {2} electricity damage (Reflex half for each), poison (Fortitude; {4} Constitution damage each round for {3} rounds), petrification (Fortitude, permanent), insanity (Will, permanent).",
                v.SurgeFire, v.SurgeAcid, v.SurgeElectricity, v.SurgePoisonRounds, v.SurgePoisonDamage);
        }

        static Ability AbilityOf(SpecialAbility a, Vanilla v)
        {
            switch (a.Guid)
            {
                case Guids.Game.WoodGolemSplintering:
                    return new Ability(new P("가시 발사", "splintering"),
                        F("가시 발사(자유 행동): 골렘 주위 {0}피트 안의 적에게 물리 피해 {1}(반사 내성 굴림 성공 시 절반). 내성 DC는 골렘의 레벨을 따른다.",
                          "Splintering (free action): {1} physical damage to enemies within {0} feet of the golem (Reflex half). The save DC follows the golem's level.", v.SplinterRadiusFeet, v.SplinterDamage));
                case Guids.Game.StoneGolemSlow:
                    return new Ability(new P("감속 오라", "slowing aura"),
                        F("감속 오라: 골렘 주위 {0}피트 안으로 들어온 적은 의지 내성 굴림에 실패하면 {1}라운드 동안 감속된다.",
                          "Slowing aura: enemies entering within {0} feet of the golem are slowed for {1} rounds on a failed Will save.", GolemRules.Dials.SlowAuraRadiusFeet, v.SlowRounds));
                case Guids.Game.IronGolemBreathWeapon:
                    return new Ability(new P("숨결 무기(독 구름)", "breath weapon (poison cloud)"),
                        F("숨결 무기(자유 행동): 접촉 거리의 대상 자리에 반경 {0}피트 독 구름을 {1}라운드 동안 만든다. 구름 안의 크리처는 적·아군을 가리지 않고 인내 내성 굴림에 실패하면 중독된다({2}라운드 동안 라운드마다 건강 피해 {3}, 인내 내성 굴림으로 막는다).",
                          "Breath weapon (free action): creates a poison cloud of {0}-foot radius at a target within touch range for {1} round. Any creature in it, friend or foe, is poisoned on a failed Fortitude save ({3} Constitution damage each round for {2} rounds, Fortitude negates).",
                          v.IronCloudRadiusFeet, v.IronCloudRounds, v.IronPoisonRounds, v.IronPoisonDamage));
                case Guids.Game.BrassGolemBreathWeapon:
                    return new Ability(new P("숨결 무기(소각 구름)", "breath weapon (incendiary cloud)"),
                        F("숨결 무기(신속 행동): 접촉 거리의 대상 자리에 반경 {0}피트 소각 구름을 {1}라운드 동안 만든다. 구름 안의 크리처는 골렘을 빼고 적·아군을 가리지 않고 매 라운드 화염 피해를 {2} 받는다(반사 내성 굴림 성공 시 절반).",
                          "Breath weapon (swift action): creates an incendiary cloud of {0}-foot radius at a target within touch range for {1} rounds. Each round every creature in it except the golem, friend or foe, takes {2} fire damage (Reflex half).",
                          v.BrassCloudRadiusFeet, v.BrassCloudRounds, v.BrassFireDamage));
                case Guids.Game.SeeInvisibilitytBuff:
                    return new Ability(new P("투명화 탐지", "see invisibility"), new P("투명화 탐지(상시).", "See invisibility (constant)."));
                case Guids.Game.PrismaticSurgeMelee:
                    return new Ability(new P("무지개 급류(접촉)", "prismatic surge (touch)"),
                        F("무지개 급류(신속 행동, 접촉 거리, 명중 굴림 없음): {0}", "Prismatic surge (swift action, touch range, no attack roll): {0}", SurgeEffects(v)));
                case Guids.Game.PrismaticSurgeRanged:
                    return new Ability(new P("무지개 급류(원거리)", "prismatic surge (ranged)"),
                        F("무지개 급류(신속 행동, 원거리 접촉 공격이 명중해야 한다): {0}", "Prismatic surge (swift action, needs a ranged touch attack to hit): {0}", SurgeEffects(v)));
                case Guids.Game.GolemAutumnEntangleAuraBuff:
                    return new Ability(new P("가시 돋친 절망의 오라", "aura of thorny despair"),
                        F("가시 돋친 절망의 오라: 골렘 주위 {0}피트 안으로 들어온 적은 반사 내성 굴림에 실패하면 {1}+{2}라운드 동안 속박되고, 의지 내성 굴림에 실패하면 {1}+{2}라운드 동안 처참한 절망에 걸린다. 내성 DC는 골렘의 레벨을 따른다.",
                          "Aura of thorny despair: enemies entering within {0} feet of the golem are entangled for {1}+{2} rounds on a failed Reflex save and suffer crushing despair for {1}+{2} rounds on a failed Will save. DCs follow the golem's level.",
                          v.AutumnAuraRadiusFeet, v.AuraRoundsDice, v.AuraRoundsBonus));
                case Guids.Game.GolemSummerEntangleAuraBuff:
                    return new Ability(new P("여름 더위의 오라", "aura of the summer heat"),
                        F("여름 더위의 오라: 골렘 주위 {0}피트 안의 적은 들어올 때와 매 라운드, 반사 내성 굴림에 실패하면 {1}+{2}라운드 동안 속박되고 인내 내성 굴림에 실패하면 피로해진다(지속 시간 제한 없음). 내성 DC는 골렘의 레벨을 따른다.",
                          "Aura of the summer heat: enemies within {0} feet of the golem, on entering and every round, are entangled for {1}+{2} rounds on a failed Reflex save and fatigued on a failed Fortitude save (no time limit). DCs follow the golem's level.",
                          v.SummerAuraRadiusFeet, v.AuraRoundsDice, v.AuraRoundsBonus));
                case Guids.Game.GolemAutumnGrabFeature:
                    return new Ability(new P("죄여오는 덩굴", "constricting vines"),
                        F("죄여오는 덩굴: 슬램이 명중하면 잡기 전법을 시도한다(전법 판정 +{0}). 성공하면 {1} + 힘 수정치의 한 배 반 피해를 주고 대상을 붙잡는다. 붙잡힌 적은 움직이지 못하고 매 라운드 {2} + 힘 수정치의 두 배 피해를 받는다. 붙잡고 있는 동안 골렘은 공격하지 못한다.",
                          "Constricting vines: a slam that hits attempts a grapple (+{0} on the check). Success deals {1} + one and a half times the Strength modifier and grapples; the grappled enemy cannot move and takes {2} + twice the Strength modifier each round. The golem cannot attack while grappling.",
                          v.GrabManeuverBonus, v.GrabDamage, v.GrappledDamage));
                case Guids.Game.FastHealing5:
                    return new Ability(F("빠른 치유 {0}", "fast healing {0}", v.FastHealingAdamantine), F("빠른 치유 {0}.", "Fast healing {0}.", v.FastHealingAdamantine));
                case Guids.Game.FastHealing20:
                    return new Ability(F("빠른 치유 {0}", "fast healing {0}", v.FastHealingMithral), F("빠른 치유 {0}.", "Fast healing {0}.", v.FastHealingMithral));
                case Guids.Game.ImprovedCriticalSlam:
                    // 알려진 문제(2026-11-17 jbp 직독): 본편 ImprovedCriticalSlam 의 WeaponTypeCriticalEdgeIncrease 무기 종류 7개에 우리 슬램의 종류(SlamType2d6 — Slam2d6 값 복사)가 없다.
                    //  시험 `strings: choice text` 가 「목록에 없음 ⇔ 이 줄이 있음」을 잠근다 — 결함을 고치면 시험이 이 줄을 지우라고 실패한다.
                    return new Ability(new P("향상된 치명타 (후려치기)", "Improved Critical (Slam)"),
                        new P("향상된 치명타 (후려치기) 특기. 알려진 문제: 본편 특기가 정한 무기 종류에 골렘 슬램의 종류가 없어 지금은 치명타 범위가 늘지 않는다.",
                              "Improved Critical (Slam) feat. Known issue: the feat's weapon type list does not include the golem's slam type, so the threat range is not increased at present."));
                case Guids.Game.CleaveFeature:
                    return new Ability(new P("베어 가르기", "Cleave"), new P("베어 가르기 특기.", "Cleave feat."));
                case Guids.Game.CombatReflexes:
                    return new Ability(new P("전투 반사", "Combat Reflexes"), new P("전투 반사 특기.", "Combat Reflexes feat."));
            }
            throw new KeyNotFoundException("Golemancer: choice text has no text for special ability " + a.Guid);
        }

        // ---------------- 수치 줄 ----------------
        static P DrKind(Material m) { return m.DrBypassAdamantine ? new P("아다만틴", "adamantine") : new P("-", "—"); }

        // 대표 레벨: 합법 레벨의 처음과 끝 + 슬램 주사위가 바뀌는 자리의 앞뒤(15~20레벨 재질 = 15 · 19 · 20).
        static int[] ShownLevels(int[] legal, Func<int, string> slamAt)
        {
            if (legal == null || legal.Length == 0) throw new ArgumentException("Golemancer: choice text: no legal level");
            var shown = new List<int> { legal[0] };
            for (int i = 1; i < legal.Length; i++)
                if (slamAt(legal[i]) != slamAt(legal[i - 1])) { shown.Add(legal[i - 1]); shown.Add(legal[i]); }
            shown.Add(legal[legal.Length - 1]);
            return shown.Distinct().OrderBy(x => x).ToArray();
        }

        static string SlamOf(Target t) { return new Dice(t.SlamRolls, t.SlamDie).ToString(); }

        static P StatLine(Material m, int level)
        {
            var t = StatRecipe.Target(m, level);
            return F("{0}레벨: 자연 방어 +{1}, 피해 감소 {2}/{3}, 슬램 {4}, 힘 {5}, 생명점 {6}", "Level {0}: natural armor +{1}, DR {2}/{3}, slam {4}, Strength {5}, {6} hit points",
                level, t.NaturalArmor, t.Dr, DrKind(m), SlamOf(t), t.StrengthDelta + GolemRules.BaseStrength, GolemRules.Hp(level, m.Size));
        }

        static P BaseLine(int level)
        {
            var r = GolemRules.Level(level);
            return F("{0}레벨: 자연 방어 +{1}, 피해 감소 {2}, 슬램 {3}, 힘 {4}", "Level {0}: natural armor +{1}, DR {2}, slam {3}, Strength {4}", level, r.NaturalArmor, r.Dr, new Dice(r.SlamRolls, r.SlamDie), r.Strength);
        }

        static int[] BaseShownLevels(int[] legal) { return ShownLevels(legal, l => { var r = GolemRules.Level(l); return new Dice(r.SlamRolls, r.SlamDie).ToString(); }); }

        // 재질을 쓰는 구간의 다음 선택 레벨(없으면 null = 마지막 재질).
        static int? NextChoiceLevel(Material m)
        {
            var ml = GolemRules.Dials.MaterialLevels;
            return m.Tier == ml.Count ? (int?)null : ml[m.Tier];
        }

        // ---------------- 재질 설명 ----------------
        static P MaterialDescription(Material m, Vanilla v)
        {
            var legal = MaterialCatalog.LegalLevels(m);
            int lo = legal[0], hi = legal[legal.Length - 1];
            var lines = new List<P>();
            int? next = NextChoiceLevel(m);
            lines.Add(next.HasValue
                ? F("{0} {1} 골렘. 연금술사 {2}~{3}레벨 동안 쓰는 재질이다({4}레벨에 새 재질로 바뀐다).", "A {0} {1} golem. Used from alchemist level {2} to {3} (replaced by a new material at level {4}).", SizeName(m.Size), MaterialName(m), lo, hi, next.Value)
                : F("{0} {1} 골렘. 연금술사 {2}레벨부터 {3}레벨까지 쓰는 마지막 재질이다.", "A {0} {1} golem. The last material, used from alchemist level {2} to {3}.", SizeName(m.Size), MaterialName(m), lo, hi));
            foreach (int level in ShownLevels(legal, l => SlamOf(StatRecipe.Target(m, l)))) lines.Add(StatLine(m, level));
            var imm = ImmunityLabels(m, true);
            lines.Add(imm.Length == 0
                ? new P("추가 면역: 없음(구조물 공통 면역만 있다).", "Additional immunities: none (only the common construct immunities).")
                : F("추가 면역: {0}.", "Additional immunities: {0}.", Join(", ", ", ", imm)));
            foreach (var a in m.SpecialAbilities) lines.Add(AbilityOf(a, v).Detail);
            if (m.SpeedBonus != 0) lines.Add(F("이동 속도 {0}피트(다른 재질은 {1}피트).", "Speed {0} feet (other materials {1} feet).", v.BaseSpeedFeet + m.SpeedBonus, v.BaseSpeedFeet));
            lines.Add(FusionLine(m, legal));
            return Lines(lines);
        }

        // 합체 중 본체가 받는 것: 합체 수치 특성 = 자연 방어 · 피해 감소 · 힘(FusionStrBonus), 형태 버프 = 같은 슬램(Materials.BuildFusionStats · Fusion.BuildForm). 추가 면역은 20레벨 형태에서만(Capstone).
        static P FusionLine(Material m, int[] legal)
        {
            int fusion = GolemRules.Dials.FusionLevel;
            int before = legal.Count(l => l < fusion);
            if (before == legal.Length) return F("골렘 합체는 {0}레벨에 얻는다. 이 재질은 그 전에 바뀌므로 합체에 쓰이지 않는다.", "Golem Fusion is gained at level {0}; this material is replaced before that and is never worn.", fusion);
            if (before != 0) throw new InvalidOperationException("Golemancer: choice text: material " + m.Id + " spans the fusion level " + fusion + " (levels " + string.Join(",", legal) + ")");
            int lo = legal[0], hi = legal[legal.Length - 1];
            int a = StatRecipe.Target(m, lo).FusionStrBonus, b = StatRecipe.Target(m, hi).FusionStrBonus;
            var shares = MaterialCatalog.Keys.Where(k => MaterialCatalog.MaterialOfKey(k).Id == m.Id).Select(CapstoneRules.FormGetsShared).ToArray();
            if (shares.Any(x => x))
                return F("합체 중 골렘술사: 위 자연 방어·피해 감소·슬램을 그대로 얻고 힘 +{0}({1}레벨)에서 +{2}({3}레벨). 특수 능력은 골렘만 갖고, 추가 면역은 {4}레벨부터 합체 중 골렘술사도 받는다.",
                    "Fused Golemancer: gains the natural armor, DR and slam above and Strength +{0} (level {1}) to +{2} (level {3}). Special abilities stay with the golem; from level {4} the additional immunities are shared too.",
                    a, lo, b, hi, GolemRules.Dials.CapstoneLevel);
            return F("합체 중 골렘술사: 위 자연 방어·피해 감소·슬램을 그대로 얻고 힘 +{0}({1}레벨)에서 +{2}({3}레벨). 추가 면역과 특수 능력은 골렘만 갖는다.",
                "Fused Golemancer: gains the natural armor, DR and slam above and Strength +{0} (level {1}) to +{2} (level {3}). Additional immunities and special abilities stay with the golem.", a, lo, b, hi);
        }

        // 한 줄 요약(재질 버프 13개의 툴팁 · 재질 특성의 짧은 설명 칸): 수치 없이 크기 · 추가 면역 이름 · 특수 능력 이름.
        static P MaterialSummary(Material m, Vanilla v)
        {
            var imm = ImmunityLabels(m, false);
            var names = m.SpecialAbilities.Select(a => AbilityOf(a, v).Name).ToList();
            if (m.SpeedBonus != 0) names.Add(F("이동 속도 {0}피트", "speed {0} feet", v.BaseSpeedFeet + m.SpeedBonus));
            if (names.Count == 0) throw new InvalidOperationException("Golemancer: choice text: material " + m.Id + " has no special ability to name");
            return F("{0} {1} 골렘. 추가 면역: {2}. 특수 능력: {3}.", "A {0} {1} golem. Additional immunities: {2}. Special abilities: {3}.",
                SizeName(m.Size), MaterialName(m), imm.Length == 0 ? new P("없음", "none") : Join(", ", ", ", imm), Join(", ", ", ", names));
        }

        // ---------------- 재질 선택 화면 ----------------
        static P MaterialSelectionName(int level) { return F("골렘 재질 ({0}레벨)", "Golem Material (level {0})", level); }

        static P MaterialSelectionDescription(int level)
        {
            var ml = GolemRules.Dials.MaterialLevels;
            int k = ml.IndexOf(level);
            if (k < 0) throw new ArgumentOutOfRangeException("level", "Golemancer: choice text: " + level + " is not a material level");
            var now = MaterialCatalog.Materials.Where(m => m.Tier == k + 1).ToArray();
            var prev = MaterialCatalog.Materials.Where(m => m.Tier == k).ToArray();
            if (now.Length != MaterialCatalog.SelectableAt(level) || prev.Length == 0) throw new InvalidOperationException("Golemancer: choice text: material tiers around level " + level + " are empty or inconsistent");
            var legal = MaterialCatalog.LegalLevels(now[0]);
            int lo = legal[0], hi = legal[legal.Length - 1];
            var lines = new List<P>();
            lines.Add(F("연금술사 {0}레벨의 골렘 재질 선택: {1} 가운데 하나를 고른다.", "Golem material choice at alchemist level {0}: choose one of {1}.", level, Join("·", ", ", now.Select(MaterialName))));
            var old = prev.Length == 1
                ? MaterialName(prev[0])
                : F("{0} 중 {1}레벨에 고른 것", "{0}, chosen at level {1}", Join("·", ", ", prev.Select(MaterialName)), ml[k - 1]);
            lines.Add(F("지금 재질({0})은 사라지고 골렘의 모습·크기·추가 면역·특수 능력·수치가 새 재질의 것으로 바뀐다.", "The current material ({0}) is removed; the golem's look, size, additional immunities, special abilities and numbers become the new material's.", old));
            lines.Add(k + 1 < ml.Count
                ? F("고른 재질은 {0}~{1}레벨 동안 쓰고 {2}레벨에 다시 고른다.", "The chosen material is used from level {0} to {1}; you choose again at level {2}.", lo, hi, ml[k + 1])
                : F("고른 재질은 {0}레벨부터 {1}레벨까지 쓴다. 마지막 재질 선택이다.", "The chosen material is used from level {0} to {1}: the last material choice.", lo, hi));
            lines.Add(new P("수치는 기준표에 재질 비율을 곱해 반올림한 값이며 선택지마다 실제 값이 적혀 있다. 이 구간의 기준표(비율을 곱하기 전):",
                "Numbers are the base table times the material ratio, rounded; each choice lists its actual values. Base table:"));
            foreach (int l in BaseShownLevels(legal)) lines.Add(BaseLine(l));
            var counts = OverdriveRules.NormalCountSummary;
            lines.Add(F("슬램(한글 패치 표기 「후려치기」) 횟수는 재질과 무관하다: 연금술사 {0}.", "Slams per round do not depend on the material: {0} by alchemist level.", new P(counts.Key, counts.Value)));
            lines.Add(new P("피해 감소 뒤의 「/아다만틴」은 아다만틴 무기에 뚫리고 「/-」는 어떤 무기에도 뚫리지 않는다.", "DR \"/adamantine\" is bypassed by adamantine weapons, \"/—\" by nothing."));
            int fusion = GolemRules.Dials.FusionLevel;
            if (hi < fusion)
                lines.Add(F("골렘 합체는 {0}레벨에 얻는다. 이번 재질은 그 전에 바뀌므로 합체에 쓰이지 않는다.", "Golem Fusion is gained at level {0}; this material is replaced before that and is never worn.", fusion));
            else if (lo < fusion)
                throw new InvalidOperationException("Golemancer: choice text: the material range " + lo + ".." + hi + " spans the fusion level " + fusion);
            else if (hi >= GolemRules.Dials.CapstoneLevel)
                lines.Add(F("합체 중에는 골렘술사가 이 재질의 자연 방어·피해 감소·슬램을 그대로 얻고 힘 보너스를 받는다. 특수 능력은 골렘만 갖고, 추가 면역은 {0}레벨부터 합체 중 골렘술사도 받는다.",
                    "While fused the Golemancer gains this material's natural armor, DR, slam and a Strength bonus. Special abilities stay with the golem; from level {0} the additional immunities are shared too.", GolemRules.Dials.CapstoneLevel));
            else
                lines.Add(new P("합체 중에는 골렘술사가 이 재질의 자연 방어·피해 감소·슬램을 그대로 얻고 힘 보너스를 받는다. 추가 면역과 특수 능력은 골렘만 갖는다.",
                    "While fused the Golemancer gains this material's natural armor, DR, slam and a Strength bonus. Additional immunities and special abilities stay with the golem."));
            return Lines(lines);
        }

        // ---------------- 반대 학파 ----------------
        static P ExtractList(SchoolExtracts x)
        {
            if (x.Examples.Length == 0 || x.Examples.Length > x.Count) throw new InvalidOperationException("Golemancer: choice text: school " + x.School + " has " + x.Examples.Length + " example(s) for " + x.Count + " extract(s)");
            var list = Join(", ", ", ", x.Examples.Select(e => F("{0}({1}레벨)", "{0} (level {1})", new P(e.Ko, e.En), e.Level)));
            return x.Examples.Length == x.Count ? list : F("{0} 등", "{0} and others", list);
        }

        static P SchoolDescription(string school, Vanilla v)
        {
            var name = SchoolFull(school);
            var x = ExtractsOf(school);
            var effect = OverdriveRules.EffectText(school);
            if (OverdriveRules.EffectOrNull(school) == null) throw new InvalidOperationException("Golemancer: choice text: school " + school + " has no overdrive effect");
            return Lines(new[]
            {
                F("{0}를 골렘술사의 반대 학파로 삼는다.", "Makes {0} one of the Golemancer's opposition schools.", name),
                F("대가: {0} 추출물은 준비할 때 그 레벨의 슬롯을 {1}칸 쓴다. 금지되는 것이 아니라 비싸지는 것이다.", "Cost: an extract of {0} takes {1} slots of its level to prepare. It is not forbidden, only more expensive.", name, v.OppositionSlots),
                F("{0} 추출물(본편 연금술사 목록 {1}개 중 {2}개): {3}.", "Extracts of {0} ({2} of the {1} on the base-game alchemist list): {3}.", name, v.AlchemistExtracts, x.Count, ExtractList(x)),
                F("골렘 폭주(연금술사 {0}레벨부터)에 {1} 추출물을 소모하면 지속 동안: {2}.", "Golem Overdrive (from alchemist level {0}) with a spent extract of {1}, for the duration: {2}.", SlamRules.OverdriveLevel, name, new P(effect.Key, effect.Value)),
                F("반대 학파로 고르면 이 효과를 내는 데에도 슬롯 {0}칸으로 준비한 추출물이 든다. 같은 학파는 한 번만 고를 수 있다.", "Chosen as an opposition school, this effect also costs an extract prepared in {0} slots. Each school can be chosen only once.", v.OppositionSlots),
            });
        }

        static P OppositionSelectionDescription(Vanilla v)
        {
            var choices = OppositionChoices();
            var rows = ExtractTable().OrderByDescending(x => x.Count).ToArray(); // 안정 정렬 — 같은 개수는 표 순서
            if (rows.Sum(x => x.Count) != v.AlchemistExtracts) throw new InvalidOperationException("Golemancer: choice text: extract counts sum to " + rows.Sum(x => x.Count) + ", expected " + v.AlchemistExtracts);
            if (!new HashSet<string>(rows.Select(x => x.School)).SetEquals(OverdriveRules.Schools)) throw new InvalidOperationException("Golemancer: choice text: the extract table does not cover exactly the eight schools");
            var dist = Join(", ", ", ", rows.Select(x => choices.Contains(x.School)
                ? F("{0} {1}개", "{0} {1}", SchoolFull(x.School), x.Count)
                : F("{0} {1}개(반대 학파로 고를 수 없다)", "{0} {1} (cannot be an opposition school)", SchoolFull(x.School), x.Count)));
            return Lines(new[]
            {
                F("{0}를 뺀 {1}개 학파 가운데 반대 학파를 고른다. {2}레벨에 {3}번 고르며 같은 학파는 한 번만 고를 수 있다.", "Choose opposition schools among the {1} schools other than {0}. You choose {3} times at level {2}; each school only once.",
                    SchoolFull(NotAnOpposition), choices.Length, GolemRules.MinLevel, GolemRules.OppositionSchoolPicks),
                F("반대 학파의 추출물은 준비할 때 그 레벨의 슬롯을 {0}칸 쓴다. 금지가 아니므로 준비해서 쓸 수는 있다.", "An extract of an opposition school takes {0} slots of its level to prepare. It is not forbidden: you can still prepare and use it.", v.OppositionSlots),
                F("본편 연금술사 추출물 {0}개의 학파: {1}.", "Schools of the {0} base-game alchemist extracts: {1}.", v.AlchemistExtracts, dist),
                F("연금술사 {0}레벨에 얻는 골렘 폭주는 소모한 추출물의 학파로 추가 효과가 정해진다. 반대 학파의 효과는 슬롯 {1}칸으로 준비한 추출물로만 낼 수 있다.",
                    "Golem Overdrive, gained at alchemist level {0}, takes its extra effect from the school of the spent extract. An opposition school's effect can only come from an extract prepared in {1} slots.", SlamRules.OverdriveLevel, v.OppositionSlots),
                F("선택지마다 그 학파의 추출물과 폭주 효과가 적혀 있다. 고른 {0}개 학파는 이 캐릭터의 추출물 준비에 계속 영향을 준다.", "Each choice lists that school's extracts and overdrive effect. The {0} chosen schools affect this character's extract preparation from then on.", GolemRules.OppositionSchoolPicks),
            });
        }

        // ---------------- 20레벨 면역 공유 ----------------
        static P SharedImmunityDescription()
        {
            var mats = CapstoneRules.CapstoneKeys().Select(k => MaterialCatalog.MaterialOfKey(k)).Distinct().ToArray();
            if (mats.Length == 0) throw new InvalidOperationException("Golemancer: choice text: no capstone material");
            var lines = new List<P>();
            lines.Add(F("{0}레벨부터 합체 중인 골렘술사는 입고 있는 골렘 재질의 추가 면역을 함께 받는다. 구조물 공통 면역과 특수 능력은 받지 않는다.",
                "From level {0} the fused Golemancer also gains the additional immunities of the worn golem's material. The common construct immunities and the special abilities are not shared.", GolemRules.Dials.CapstoneLevel));
            foreach (var m in mats) lines.Add(F("{0}: {1}", "{0}: {1}", MaterialName(m), Join(", ", ", ", ImmunityLabels(m, true))));
            lines.Add(new P("이미 같은 면역이 있으면 그대로 두고, 합체가 풀리면 받은 면역은 사라진다.", "An immunity you already have is left alone; the shared immunities end with the fusion."));
            return Lines(lines);
        }

        // ---------------- 골렘·합체 설명에 붙는 줄(StringsTable.Merge) ----------------
        // 기준 수치표: 각 슬램 단계가 시작하는 레벨(Dials.SlamTierLevels)의 레벨표 값.
        internal static KeyValuePair<string, string> GolemBaseTable()
        {
            var lines = new List<P> { new P("기준 수치(재질 비율을 곱하기 전, 재질마다의 실제 값은 재질 설명에 있다):", "Base numbers (before the material ratio; each material's description lists its actual values):") };
            foreach (int l in GolemRules.Dials.SlamTierLevels) lines.Add(BaseLine(l));
            var p = Lines(lines);
            return new KeyValuePair<string, string>(p.Ko, p.En);
        }

        // 합체 힘 보너스 = GolemRules.StrMod(재질 힘, AwayFromZero) — 힘이 홀수면 올림(StatRecipe.Target FusionStrBonus).
        internal static KeyValuePair<string, string> FusionStrengthRule()
        {
            var p = new P("합체 중 힘 보너스는 골렘의 힘 수정치와 같다(골렘의 힘이 홀수면 올림). 재질마다의 값은 재질 설명에 있다.", "The Strength bonus while fused equals the golem's Strength modifier (rounded up when the golem's Strength is odd); each material's description lists it.");
            return new KeyValuePair<string, string>(p.Ko, p.En);
        }

        // ---------------- 키 ----------------
        // 키 머리는 「X.Y」 모양이 아니게 쪼개 둔다(시험 `strings: every key literal` 은 그 모양의 리터럴을 완성된 키로 본다).
        static readonly string MaterialRoot = "Material";
        static readonly string SelectionRoot = "MaterialSelection";
        static readonly string SchoolRoot = "School";

        internal static string MaterialDescriptionKey(Material m) { MaterialCatalog.Get(m.Id); return m.Tier == 0 ? "MaterialWood.Description" : MaterialRoot + "." + m.Id + ".Description"; }
        internal static string MaterialSummaryKey(Material m) { MaterialCatalog.Get(m.Id); return m.Tier == 0 ? "MaterialWood.Summary" : MaterialRoot + "." + m.Id + ".Summary"; }
        internal static string MaterialSelectionNameKey(int level) { MaterialCatalog.SelectionGuid(level); return SelectionRoot + "." + level + ".Name"; }
        internal static string MaterialSelectionDescriptionKey(int level) { MaterialCatalog.SelectionGuid(level); return SelectionRoot + "." + level + ".Description"; }
        internal static string SchoolDescriptionKey(string school)
        {
            if (!OppositionChoices().Contains(school)) throw new ArgumentException("Golemancer: '" + school + "' is not an opposition school choice (" + string.Join(",", OppositionChoices()) + ")");
            return SchoolRoot + "." + school + ".Description";
        }

        // StringsTable 이 합치는 생성 문구 전부(키 중복 없음).
        internal static List<GeneratedString> GeneratedStrings()
        {
            var v = new Vanilla();
            var l = new List<GeneratedString>();
            Action<string, P> add = (key, p) => l.Add(new GeneratedString(key, p.Ko, p.En));
            foreach (var m in MaterialCatalog.Materials)
            {
                add(MaterialDescriptionKey(m), MaterialDescription(m, v));
                add(MaterialSummaryKey(m), MaterialSummary(m, v));
            }
            foreach (int level in GolemRules.Dials.MaterialLevels)
            {
                add(MaterialSelectionNameKey(level), MaterialSelectionName(level));
                add(MaterialSelectionDescriptionKey(level), MaterialSelectionDescription(level));
            }
            add("OppositionSelection.Description", OppositionSelectionDescription(v));
            foreach (var s in OppositionChoices()) add(SchoolDescriptionKey(s), SchoolDescription(s, v));
            add("SharedImmunityFeature.Description", SharedImmunityDescription());
            if (l.Select(x => x.Key).Distinct().Count() != l.Count) throw new InvalidOperationException("ChoiceText.GeneratedStrings: duplicate key");
            return l;
        }
    }
}
