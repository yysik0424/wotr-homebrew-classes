using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Localization;

namespace PackWarden
{
    public static class Strings
    {
        const string Prefix = "PackWarden.";

        // 그림자 무리 규칙 1문장(한·영) — Call·CallToggleAbility·CallMounted 설명 끝에 붙는 한 원천(C4). 행위 출처: 설계
         
        // · 항목 7(숨김 = 함께 숨음, 영구 제거 = 해산 — ShadowRules.StateForMainPet). 시험이 세 키의 문장 포함을 잠근다(CheckT5Locks·CheckOwnerRulings).
        // 2026-11-16 소유자 결정: 「따라다니기만」 → 「따라다니고, 주 동물이 싸우는 동안에는 그 대상 곁으로 붙는다」.
        const string ShadowRuleKo = "보조 동물은 스스로 공격하지 않고 주 동물 동료를 따라다니며(주 동물이 싸우는 동안에는 그 대상 곁으로 붙는다), 주 동물이 근접 무기로 휘두를 때마다(능력으로 하는 근접 공격 포함) 닿는 거리에 있는 보조가 같은 대상을 한 번씩 함께 공격한다(기회 공격과 추가 공격에는 따라치지 않는다). 주 동물이 쓰러진 동안에는 보조가 스스로 싸운다. 주 동물이 숨으면 보조도 함께 숨고, 주 동물이 떠나면 무리는 흩어진다.";
        const string ShadowRuleEn = "Auxiliary animals do not attack on their own: they follow the main animal companion (while it is fighting, they move up beside its target), and each time it swings a melee weapon (including ability attacks such as Cleave or Vital Strike), every auxiliary within reach strikes the same target once (not on attacks of opportunity or extra attacks). While the main companion is down, the auxiliaries fight on their own; when it is hidden they hide with it, and when it leaves for good the pack disperses.";

        // 마릿수 구간 문구(소유자 결정 2026-11-16). 원천은 PackRules 의 표(AuxCountBands)이고 여기는 그 사본이다 — 설명이 컴파일 시점 상수여야 해서(시험이 DLL 의
        // 문자열 상수를 읽는다) 실행 중에 조립하지 않는다. 사본이 표와 어긋나면 시험(CheckRespawnWiring: 구간마다 「a~b레벨 n마리」·「n at levels a-b」 포함)이 실패한다.
        const string AuxCountKo = "1~6레벨 1마리, 7~12레벨 2마리, 13~18레벨 3마리, 19~20레벨 4마리";
        const string AuxCountEn = "1 at levels 1-6, 2 at levels 7-12, 3 at levels 13-18 and 4 at levels 19-20";
        // 재소집 규칙 1문장(소유자 결정 2026-11-16: 대기 삭제. 그 전 문구 = 「게임 시간 1시간 뒤에 다시 부를 수 있으며, 휴식하면 이 대기가 초기화된다」).
        const string RespawnRuleKo = "보조 동물이 죽으면 그 전투에서는 돌아오지 않고, 다음 전투에서 바로 다시 나타난다.";
        // 보조 동물의 본성(소유자 결정 2026-11-16): 소형 고정·자라지 않음, 적의 기회 공격을 받지 않음.
        const string NatureRuleKo = "보조 동물은 작은 크기(소형)로 나타나며 자라지 않는다(성장의 능력치는 받는다). 몸이 작아도 자연 무기의 피해 주사위는 줄지 않는다(원래 크기 기준으로 굴린다). 보조 동물은 적의 기회 공격을 받지 않는다.";
        const string NatureRuleEn = "Auxiliary animals appear at Small size and do not grow (they still receive the growth's ability bonuses). Their natural weapon damage dice are not reduced by the small body (they are rolled as for the original size). Enemies cannot make attacks of opportunity against auxiliary animals.";
        const string RespawnRuleEn = "An auxiliary animal that dies does not return during that combat; it appears again at once in the next combat.";

        // 한/영 표. 키는 Prefix 없이 쓴다. 이후 Task 가 키를 추가한다.
        static readonly Dictionary<string, (string ko, string en)> Table = new Dictionary<string, (string ko, string en)>
        {
            ["Archetype.Name"] = ("무리 조련사", "Pack Warden"),
            ["Archetype.Description"] = (
                "무리 조련사는 동물 여러 마리를 거느리고 다니는 헌터다. 본인은 거의 지휘만 하고, 짐승들이 대신 싸운다. 정식 동물 동료 한 마리에 같은 종의 보조 동물이 전투 중에만 함께하는데, 1~6레벨에는 무리 소집 토글을 켜 두면, 7레벨부터는 주 동물 동료에 탄 채 싸우면 나타나 함께 달려든다. 보조 동물의 수는 " + AuxCountKo + "다. 헌터가 표식을 찍은 적에게 무리가 한꺼번에 덤벼드는 장면이 가장 큰 무기다.",
                "The Pack Warden is a hunter who travels with several animals at once. The hunter mostly gives orders while the beasts do the fighting. Auxiliary animals of the same species as the main animal companion join only during combat: at levels 1-6 they appear while the Call the Pack toggle is on, and from level 7 while the hunter fights mounted on the main companion. Their number is " + AuxCountEn + ". The pack falling together upon the foe the hunter has marked is the archetype's greatest weapon."),
            ["Archetype.DescriptionShort"] = (
                "동물 여러 마리를 거느리고 다니며, 표식을 찍은 적에게 무리가 함께 달려든다.",
                "Leads a pack of animals that all fall upon the foe the hunter has marked."),
            ["CompanionSelection.Name"] = ("무리의 주 동물 동료", "Pack's Main Animal Companion"),
            ["Call.Name"] = ("무리 소집", "Call the Pack"),
            ["Call.Description"] = (
                "소집 조건을 만족한 채 전투에 들어가면 주 동물 동료와 같은 종의 보조 동물이 나타나 함께 싸운다. 전투가 끝나면 무리는 흩어진다.\n보조 동물의 수는 " + AuxCountKo + "이고, 레벨은 헌터 레벨 − 4(최소 1)이다.\n" + NatureRuleKo + "\n" + RespawnRuleKo + "\n" + ShadowRuleKo,
                "Entering combat while the call condition is met summons auxiliary animals of the same species as the main animal companion to fight alongside it. The pack disperses when combat ends.\nThe number of auxiliary animals is " + AuxCountEn + ", each of level equal to hunter level − 4 (minimum 1).\n" + NatureRuleEn + "\n" + RespawnRuleEn + "\n" + ShadowRuleEn),
            ["CallToggle.Name"] = ("무리 소집: 토글", "Call the Pack: Toggle"),
            ["CallToggle.Description"] = (
                "6레벨까지의 소집 조건이다. 무리 소집 토글을 켜 두면 전투에 들어갈 때 무리를 부르고, 전투 중에 켜면 바로 부른다. 7레벨에 기승 소집으로 바뀌면서 사라진다.",
                "The call condition up to level 6. With the Call the Pack toggle on, the pack is called on entering combat, and turning it on during combat calls it at once. Replaced by Mounted Call at level 7."),
            ["CallToggleAbility.Name"] = ("무리 소집", "Call the Pack"),
            ["CallToggleAbility.Description"] = (
                "켜 두면 전투에 들어갈 때 보조 동물을 부른다. 전투 중에 켜면 바로 부른다. 전투 중에 꺼도 이미 소집된 무리는 전투가 끝날 때까지 남는다.\n" + ShadowRuleKo,
                "While on, auxiliary animals are called on entering combat. Turning it on during combat calls them at once. Turning it off during combat does not dismiss a pack already called; it stays until combat ends.\n" + ShadowRuleEn),
            ["CallMounted.Name"] = ("기승 소집", "Mounted Call"),
            ["CallMounted.Description"] = (
                "7레벨부터의 소집 조건이다. 주 동물 동료에 탄 채 전투에 들어가거나 전투 중에 타면 무리를 부른다. 무리 소집 토글은 사라진다.\n스스로 내리면 무리가 흩어지고, 다시 타면 곧바로 돌아온다(그 전투에서 죽은 보조 동물은 빼고). 탈것이나 기수가 쓰러져 강제로 내려진 경우에는 무리가 남는다.\n" + ShadowRuleKo,
                "The call condition from level 7. Entering combat while riding the main animal companion, or mounting it during combat, calls the pack. The Call the Pack toggle is removed.\nDismounting by choice disperses the pack, and mounting again brings it back at once (except auxiliary animals that died in that combat). A forced dismount (the mount or the rider going down) leaves the pack in place.\n" + ShadowRuleEn),
            ["Aspects.Name"] = ("무리의 두 상", "Aspects of the Pack"),
            ["Aspects.Description"] = (
                "사나운 상과 질긴 상 중 하나를 켠다. 한 상을 켜면 켜져 있던 다른 상은 꺼지고, 전환은 자유 행동이며 횟수 제한이 없다.\n사나운 상: 무리 전체의 명중과 피해에 보너스.\n질긴 상: 무리 전체의 방어도에 보너스. 보조 동물은 소환될 때 한 번 지혜 수정치 × 헌터 레벨의 임시 체력도 받는다.\n보너스는 지혜 수정치만큼이며(지혜 수정치가 0 이하이면 0, 깎이지는 않는다) 주 동물 동료와 보조 동물 모두에게 간다. 주인공은 영향 없음.",
                "Switch on either the Fierce Aspect or the Tough Aspect. Switching one on turns the other off; switching is a free action with no limit.\nFierce Aspect: a bonus on the whole pack's attack and damage.\nTough Aspect: a bonus to the whole pack's AC. Auxiliary animals also receive temporary hit points equal to your Wisdom modifier × hunter level once, when summoned.\nThe bonus equals your Wisdom modifier (0 if the modifier is 0 or lower; it never becomes a penalty) and goes to the main animal companion and every auxiliary animal. The hunter is not affected."),
            ["Aspect.Fierce.Name"] = ("사나운 상", "Fierce Aspect"),
            ["Aspect.Fierce.Description"] = (
                "무리 전체(주 동물 동료와 보조 동물)의 명중과 피해에 지혜 수정치만큼 보너스를 준다. 주인공은 영향 없음. 질긴 상과 동시에 켤 수 없고(20레벨 제외), 전환은 자유 행동이다.",
                "Grants the whole pack (main animal companion and auxiliary animals) an attack and damage bonus equal to your Wisdom modifier. The hunter is not affected. Cannot be on together with the Tough Aspect (except at level 20); switching is a free action."),
            ["Aspect.Tough.Name"] = ("질긴 상", "Tough Aspect"),
            ["Aspect.Tough.Description"] = (
                "무리 전체(주 동물 동료와 보조 동물)의 방어도에 지혜 수정치만큼 보너스를 준다. 보조 동물은 소환될 때 한 번 지혜 수정치 × 헌터 레벨의 임시 체력도 받는다(상을 바꿔도 다시 채워지지 않는다). 주인공은 영향 없음. 사나운 상과 동시에 켤 수 없고(20레벨 제외), 전환은 자유 행동이다.",
                "Grants the whole pack (main animal companion and auxiliary animals) an AC bonus equal to your Wisdom modifier. Auxiliary animals also receive temporary hit points equal to your Wisdom modifier × hunter level once, when summoned (switching aspects does not refill them). The hunter is not affected. Cannot be on together with the Fierce Aspect (except at level 20); switching is a free action."),
            ["Aspect.ToughTempHp.Name"] = ("질긴 상: 임시 체력", "Tough Aspect: Temporary Hit Points"),
            ["Aspect.ToughTempHp.Description"] = (
                "보조 동물이 소환될 때 한 번 받는 임시 체력. 지혜 수정치 × 헌터 레벨이며, 지혜 수정치가 0 이하이면 0이다. 상을 바꿔도 다시 채워지지 않는다.",
                "Temporary hit points an auxiliary animal receives once when summoned: Wisdom modifier × hunter level, 0 if the modifier is 0 or lower. Switching aspects does not refill them."),
            ["AspectBoth.Name"] = ("두 상 동시 활성", "Both Aspects"),
            ["AspectBoth.Description"] = (
                "사나운 상과 질긴 상을 동시에 켤 수 있다. 각 상의 보너스는 지혜 수정치만큼이며 주 동물 동료와 보조 동물에게만 간다. 주인공은 영향 없음.",
                "The Fierce Aspect and the Tough Aspect can be on at the same time. Each bonus equals your Wisdom modifier and goes only to the main animal companion and auxiliary animals. The hunter is not affected."),
            ["AuxNature.Name"] = ("무리의 보조 동물", "Pack Auxiliary"),
            ["AuxNature.Description"] = (NatureRuleKo, NatureRuleEn),
            ["AuxDiceRestore.Name"] = ("무리의 이빨", "Pack's Fangs"),
            ["AuxDiceRestore.Description"] = (
                "이 보조 동물은 몸이 소형이어도 자연 무기의 피해 주사위를 원래 크기 기준으로 굴린다. 명중, 방어도, 사거리는 소형 그대로다.",
                "This auxiliary animal rolls its natural weapon damage dice as for its original size even though its body is Small. Attack, AC and reach stay those of a Small creature."),
            ["AuxStudiedBonus.Name"] = ("무리의 사냥감", "Pack's Quarry"),
            ["AuxStudiedBonus.Description"] = ("이전 버전의 특성입니다. 현재 효과가 없습니다.", "Retired feature. It has no effect."),
        };

        // 무리 강화(소유자 결정 2026-11-16): 문구는 순수 EmpowerRules 가 만든다(시험 exe 가 직접 실행). 키도 EmpowerRules 가 준다.
        // 2026-11-17: 선택지 특성은 여러 줄 설명(ChoiceDescription), 능력·자원은 짧은 설명(Description) — 키를 갈랐다.
        static Strings()
        {
            foreach (var c in EmpowerRules.Candidates)
            {
                Table.Add(EmpowerRules.NameKey(c), (EmpowerRules.NameKo(c), EmpowerRules.NameEn(c)));
                Table.Add(EmpowerRules.DescriptionKey(c), (EmpowerRules.DescriptionKo(c), EmpowerRules.DescriptionEn(c)));
                Table.Add(EmpowerRules.ChoiceDescriptionKey(c), (EmpowerRules.ChoiceDescriptionKo(c), EmpowerRules.ChoiceDescriptionEn(c)));
            }
            Table.Add(EmpowerRules.SelectionNameKey(), (EmpowerRules.SelectionNameKo(), EmpowerRules.SelectionNameEn()));
            Table.Add(EmpowerRules.SelectionDescriptionKey(), (EmpowerRules.SelectionDescriptionKo(), EmpowerRules.SelectionDescriptionEn()));
            Table.Add(EmpowerRules.NoTargetsKey, (EmpowerRules.NoTargetsKo, EmpowerRules.NoTargetsEn));
            Table.Add(EmpowerRules.DisabledKey, (EmpowerRules.DisabledKo, EmpowerRules.DisabledEn));
            Table.Add(EmpowerRules.DisabledInternalKey, (EmpowerRules.DisabledInternalKo, EmpowerRules.DisabledInternalEn));
            Table.Add(EmpowerRules.TargetsUnreadableKey, (EmpowerRules.TargetsUnreadableKo, EmpowerRules.TargetsUnreadableEn));
            // 주 동물 동료 선택 화면(2026-11-17): 문구는 순수 CompanionText 가 규칙 표에서 만든다(시험 exe 가 직접 실행). 키도 CompanionText 가 준다.
            Table.Add(CompanionText.DescriptionKey(), (CompanionText.DescriptionKo(), CompanionText.DescriptionEn()));
        }

        // 언어 판정. 이 게임(2.7.0x) 의 Locale 열거형에는 koKR 이 없다(어셈블리 확인: enGB ruRU deDE frFR zhCN Sound esES ptBR itIT).
        // 한국어 모드들은 영어 팩 문자열을 한국어로 덮어쓴다. 그래서 (1) 열거형 이름이 koKR 이면 한국어(미래 대비),
        // (2) 아니면 본편 헌터 클래스 이름에 한글이 있는지로 현재 팩이 한국어인지 본다. 특정 모드 존재에는 의존하지 않는다.
        // 조회 시점에 판정한다. 한국어(true)만 캐시한다: 한국어 모드가 영어 팩을 나중에 덮어쓸 수 있으므로 false 를 캐시하면
        // 그 뒤로 영원히 영어가 된다(최종 리뷰 M6). 로케일이 바뀌면 InvalidateLanguage 로 비운다.
        static bool koreanCached;

        public static void InvalidateLanguage() { koreanCached = false; }

        static bool IsKorean()
        {
            if (koreanCached) return true;
            bool ko = DetectKorean();
            if (ko) koreanCached = true;
            return ko;
        }

        static bool DetectKorean()
        {
            // 판정식 = UiLanguage(게임 형식 없음 — 시험 exe 가 실행하고, 게임 안 점검 S14 choice-text 가 같은 식으로 기대 언어를 정한다).
            if (UiLanguage.LocaleIsKorean(LocalizationManager.CurrentLocale.ToString())) return true;
            // 헌터 클래스 키는 "PackWarden." 이 아니므로 GetText 접두 패치가 통과시킨다(재귀 없음).
            var hunter = BlueprintUtil.Get<BlueprintCharacterClass>(Guids.Game.HunterClass);
            return UiLanguage.NameIsKorean(hunter.Name); // 비었으면 던진다(영어로 접지 않는다)
        }

        // 번들에서는 이미 접두가 확인된 전체 키를 재사용한다. 번역 본문은 원래 표에서 매번 읽어 갱신을 반영한다.
        static ConcurrentDictionary<string, string> bundleKeys;

        internal static void PrepareBundleKeys()
        {
            var keys = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in Table.Keys) keys.TryAdd(Prefix + key, key);
            bundleKeys = keys;
        }

        public static bool IsOurKey(string key)
        {
            return key != null && key.StartsWith(Prefix, StringComparison.Ordinal);
        }

        // GetText 접두 패치가 부른다. 없는 키는 예외(빈 문자열로 조용히 넘기지 않는다).
        public static string Resolve(string fullKey)
        {
            string key;
            if (bundleKeys != null && fullKey != null && bundleKeys.TryGetValue(fullKey, out key))
                return Lookup(key, fullKey);
            key = fullKey.Substring(Prefix.Length);
            string value = Lookup(key, fullKey);
            if (bundleKeys != null) bundleKeys.TryAdd(fullKey, key);
            return value;
        }

        static string Lookup(string key, string fullKey)
        {
            (string ko, string en) entry;
            if (!Table.TryGetValue(key, out entry))
                throw new KeyNotFoundException("PackWarden: unknown string key '" + fullKey + "'");
            return IsKorean() ? entry.ko : entry.en;
        }

        public static LocalizedString L(string key)
        {
            if (!Table.ContainsKey(key))
                throw new KeyNotFoundException("PackWarden: missing string key '" + key + "'");
            string fullKey = Prefix + key;
            if (bundleKeys != null) bundleKeys.TryAdd(fullKey, key);
            return new LocalizedString { Key = fullKey };
        }
    }
}
