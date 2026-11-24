using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
        // Main.Load 가 Strings.Configure(Table, Guids.Game.MonkClass) 로 넘긴다. 문구 출처 = 설계 docs\2026-11-14-chimera-monk-design.md §1·§6·§7·§8.
        // 각 Task 가 자기 기능의 키를 더한다.
        // 변이 설명문("mut.*.desc")은 리터럴이 아니다 — 아래 MutationText 가 규칙 상수(ChimeraRules · AspectPlan · MutationsPlan · MutagenPlan)에서 만들어 넣는다(C4: 문구 속 수치 = 규칙 상수).
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();

        static Dictionary<string, (string ko, string en)> Build()
        {
            var d = new Dictionary<string, (string ko, string en)>
            {
                ["archetype.name"] = ("키메라 몽크", "Chimera Monk"),
                ["archetype.desc"] = (
                    "수행으로 몸을 바꾸는 몽크. 2레벨부터 짝수 레벨마다 열여덟 변이 중 하나를 고른다. 키메라 변이약은 능력치와 자연 방어를 높이고 선택한 변이를 강화한다. 지혜는 변이의 내성 DC와 변이약 사용 횟수를 결정한다. 플러리·맨손 공격·지혜 AC·기 능력은 그대로다. 몽크 보너스 특기 세 개와 명경지수, 빠른 이동을 잃는다.",
                    "A monk who reshapes the body through practice. Starting at 2nd level, choose one of eighteen mutations at each even level. Chimeric mutagen raises an ability score and natural armor and strengthens each mutation. Wisdom determines mutation save DCs and daily mutagen uses. Flurry, unarmed strike, Wisdom AC and ki powers are kept. You give up three monk bonus feats, Still Mind and Fast Movement."),
                // T3: 변이 선택 + 변이(설계 §7 표 16종, 판정 D1·D5 반영 + 2026-11-16 소유자 결정 3종 − 2026-11-17 소유자 결정 추가 꼬리 삭제 = 18). 「변이약 중」 = 키메라 변이약 버프가 걸려 있는 동안.
                ["mut.selection.name"] = ("키메라 변이", "Chimeric Mutation"),
                ["mut.Tail.name"] = ("꼬리", "Tail"),
                ["mut.Bite.name"] = ("물기", "Bite"),
                ["mut.Claw.name"] = ("발톱", "Claw"),
                // 2026-11-16 소유자 결정 2번: 뿔(들이받기). 문구 형식 = 꼬리 변이.
                ["mut.Horn.name"] = ("뿔", "Horns"),
                ["mut.Venom.name"] = ("독", "Venom"),
                ["mut.Scales.name"] = ("비늘", "Scales"),
                ["mut.Aspect.name"] = ("부분 변환", "Partial Aspect"),
                // 2026-11-16 소유자 결정 4번: 부분 변환 II · III(형태 선택지 문구는 "mut.Aspect.<형태>" 한 벌을 같이 쓴다).
                ["mut.Aspect2.name"] = ("부분 변환 II", "Partial Aspect II"),
                ["mut.Aspect3.name"] = ("부분 변환 III", "Partial Aspect III"),
                ["mut.Wings.name"] = ("날개", "Wings"),
                ["mut.Breath.name"] = ("브레스", "Breath"),
                ["mut.ExtraArm.name"] = ("추가 팔", "Extra Arm"),
                ["mut.ExtraLegs.name"] = ("추가 다리", "Extra Legs"),
                // 폐지된 변이(소유자 결정 2026-11-17). 블루프린트는 세이브 호환으로 남고 UI 에서 숨긴다 — 이 이름·설명은 숨김이 안 먹는 화면에 보일 때를 위한 것이다(귀검사 「스탠드 재충전 (폐지)」 선례).
                ["mut.ExtraTail.name"] = ("추가 꼬리 (폐지)", "Extra Tail (retired)"),
                ["mut.ExtraEyes.name"] = ("추가 눈", "Extra Eyes"),
                ["mut.ExtraEars.name"] = ("추가 귀", "Extra Ears"),
                ["mut.ExtraSenses.name"] = ("추가 감각", "Extra Senses"),
                ["mut.ExtraHeart.name"] = ("추가 심장", "Extra Heart"),
                ["mut.ExtraLung.name"] = ("추가 폐", "Extra Lung"),
                ["mut.Breath.Fire.name"] = ("브레스: 화염", "Breath: Fire"),
                ["mut.Breath.Cold.name"] = ("브레스: 냉기", "Breath: Cold"),
                ["mut.Breath.Acid.name"] = ("브레스: 산", "Breath: Acid"),
                ["mut.Breath.Electricity.name"] = ("브레스: 전기", "Breath: Electricity"),
                ["mut.Aspect.Bear.name"] = ("부분 변환: 곰", "Aspect: Bear"),
                ["mut.Aspect.Boar.name"] = ("부분 변환: 멧돼지", "Aspect: Boar"),
                ["mut.Aspect.Dinosaur.name"] = ("부분 변환: 공룡", "Aspect: Dinosaur"),
                ["mut.Aspect.Elephant.name"] = ("부분 변환: 코끼리", "Aspect: Elephant"),
                ["mut.Aspect.Horse.name"] = ("부분 변환: 말", "Aspect: Horse"),
                ["mut.Aspect.Lizard.name"] = ("부분 변환: 도마뱀", "Aspect: Lizard"),
                ["mut.Aspect.Spider.name"] = ("부분 변환: 거미", "Aspect: Spider"),
                ["mut.Aspect.Tiger.name"] = ("부분 변환: 호랑이", "Aspect: Tiger"),
                ["mut.Aspect.Wolf.name"] = ("부분 변환: 늑대", "Aspect: Wolf"),
                // T5: 1레벨 숨김 몸 특성(자연 무기 사다리 주사위).
                ["body.name"] = ("키메라의 몸", "Chimeric Body"),
                ["body.desc"] = ("자연 무기 변이의 피해 주사위가 몽크 레벨에 따라 자란다.", "Natural weapon mutations' damage dice grow with monk level."),
                // T7: 브레스 능력·독 버프.
                ["breath.name"] = ("키메라 브레스", "Chimeric Breath"),
                ["breath.Fire.name"] = ("화염 브레스", "Fire Breath"), ["breath.Fire.desc"] = ("30피트 원뿔, 몽크 레벨 2당 1d6 화염, 반사 반감.", "30 ft cone, 1d6 fire per two monk levels, Reflex half."),
                ["breath.Cold.name"] = ("냉기 브레스", "Cold Breath"), ["breath.Cold.desc"] = ("30피트 원뿔, 몽크 레벨 2당 1d6 냉기, 반사 반감.", "30 ft cone, 1d6 cold per two monk levels, Reflex half."),
                ["breath.Acid.name"] = ("산 브레스", "Acid Breath"), ["breath.Acid.desc"] = ("30피트 원뿔, 몽크 레벨 2당 1d6 산, 반사 반감.", "30 ft cone, 1d6 acid per two monk levels, Reflex half."),
                ["breath.Electricity.name"] = ("전기 브레스", "Electricity Breath"), ["breath.Electricity.desc"] = ("30피트 원뿔, 몽크 레벨 2당 1d6 전기, 반사 반감.", "30 ft cone, 1d6 electricity per two monk levels, Reflex half."),
                ["breath.surge.Fire.name"] = ("화염 브레스 — 폭주", "Fire Breath — Surge"), ["breath.surge.Fire.desc"] = ("변이약 중에만 쓸 수 있는 추가 브레스(휴식마다 충전).", "Extra breath usable only while mutated (refills on rest)."),
                ["breath.surge.Cold.name"] = ("냉기 브레스 — 폭주", "Cold Breath — Surge"), ["breath.surge.Cold.desc"] = ("변이약 중에만 쓸 수 있는 추가 브레스(휴식마다 충전).", "Extra breath usable only while mutated (refills on rest)."),
                ["breath.surge.Acid.name"] = ("산 브레스 — 폭주", "Acid Breath — Surge"), ["breath.surge.Acid.desc"] = ("변이약 중에만 쓸 수 있는 추가 브레스(휴식마다 충전).", "Extra breath usable only while mutated (refills on rest)."),
                ["breath.surge.Electricity.name"] = ("전기 브레스 — 폭주", "Electricity Breath — Surge"), ["breath.surge.Electricity.desc"] = ("변이약 중에만 쓸 수 있는 추가 브레스(휴식마다 충전).", "Extra breath usable only while mutated (refills on rest)."),
                ["venom.buff.name"] = ("키메라 독", "Chimeric Venom"), ["venom.buff.desc"] = ("6라운드 동안 매 라운드 인내 내성, 실패하면 건강 1d2 피해.", "Fortitude save each round for 6 rounds or take 1d2 Constitution damage."),
                ["venom.spent.name"] = ("독 — 이번 라운드 사용", "Venom — used this round"), ["venom.spent.desc"] = ("이번 라운드에 이미 독을 주입했다.", "Venom already delivered this round."),
                // T4: 키메라 변이약(설계 §8).
                ["mutagen.name"] = ("키메라 변이약", "Chimeric Mutagen"),
                ["mutagen.desc"] = ("표준 행동으로 마신다(변신 중 불가, 한 번에 하나). 힘·민첩·지혜 중 하나에 연금술 +4(10레벨 +6, 16레벨 +8), 자연 갑옷 +2(+4, +6), 정신 능력치 페널티 없음. 몽크 레벨 라운드 지속, 하루 3 + 지혜 수정치 회(최소 1).", "Drink as a standard action (not while polymorphed, one at a time): +4 alchemical to Strength, Dexterity or Wisdom (+6 at 10, +8 at 16) and +2 natural armor (+4, +6), no mental penalty. Lasts 1 round per monk level, 3 + Wisdom modifier per day (minimum 1)."),
                ["mutagen.Strength.name"] = ("키메라 변이약 — 힘", "Chimeric Mutagen — Strength"),
                ["mutagen.Strength.desc"] = ("힘 연금술 보너스와 자연 갑옷.", "Alchemical Strength bonus and natural armor."),
                ["mutagen.Dexterity.name"] = ("키메라 변이약 — 민첩", "Chimeric Mutagen — Dexterity"),
                ["mutagen.Dexterity.desc"] = ("민첩 연금술 보너스와 자연 갑옷.", "Alchemical Dexterity bonus and natural armor."),
                ["mutagen.Wisdom.name"] = ("키메라 변이약 — 지혜", "Chimeric Mutagen — Wisdom"),
                ["mutagen.Wisdom.desc"] = ("지혜 연금술 보너스와 자연 갑옷.", "Alchemical Wisdom bonus and natural armor."),
                ["mut.Aspect.Wolverine.name"] = ("부분 변환: 울버린", "Aspect: Wolverine"),
            };
            MutationText.AddTo(d);
            return d;
        }
    }

    // [순수] 변이 선택 화면의 설명문(레벨업 선택 화면의 상세 창이 읽는 BlueprintUnitFact.m_Description — 엔진 CharGenFeatureSelectorItemVM.TooltipTemplate
    //  → TooltipTemplateLevelUpFeature.Prepare IL_002E FeatureInformation.get_Description → BlueprintUnitFact.get_Description). 줄바꿈 = "\n"(본편 설명문과 같은 표기).
    // 수치 잠금(C4): 아래 문구 틀(리터럴)에는 숫자를 쓰지 않는다 — 수치는 전부 {n} 자리에 규칙 상수(ChimeraRules · AspectPlan · MutagenPlan · LungPlan)를 끼워 넣는다.
    //  시험 `strings: mutation text` 가 (1) 이 구역의 문자열 리터럴에 {n} 밖의 숫자가 없음 (2) 만들어진 문구의 수치 == 손 전사 기대값 (3) ko/en 줄 수·수치 집합 일치를 잠근다.
    //  규칙에 없는 값(엔진·본편 블루프린트가 정하는 값)은 Build 안의 지역 변수로 두고 출처를 옆에 적는다(정적 필드는 `dial: single block` 이 금지) — 시험이 blueprints.zip 과 대조한다.
    // 틀에 넘긴 인자를 틀이 쓰지 않거나 모자라면 던진다(조용히 빠진 수치 없음).
    static class MutationText
    {
        // 한/영 한 쌍. 인자로 넘기면 그 언어 쪽이 들어간다.
        sealed class P
        {
            public readonly string Ko, En;
            public P(string ko, string en)
            {
                if (string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en)) throw new ArgumentException("ChimeraMonk: mutation text pair with an empty side");
                Ko = ko; En = en;
            }
        }

        static int Min { get { return ChimeraRules.Levels.MinLevel; } }
        static int Max { get { return ChimeraRules.Levels.MaxLevel; } }

        static P F(string ko, string en, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args", "ChimeraMonk: mutation text argument " + i + " is null (" + en + ")");
                string slot = "{" + i + "}";
                if (!ko.Contains(slot) || !en.Contains(slot)) throw new ArgumentException("ChimeraMonk: mutation text template does not use argument " + i + " in both languages (" + en + ")");
            }
            return new P(string.Format(ko, args.Select(a => a is P ? (object)((P)a).Ko : a).ToArray()), string.Format(en, args.Select(a => a is P ? (object)((P)a).En : a).ToArray()));
        }

        // 줄 묶음(null = 그 줄 없음). 한 줄도 없으면 던진다.
        static P Lines(params P[] lines)
        {
            var l = lines.Where(x => x != null).ToArray();
            if (l.Length == 0) throw new ArgumentException("ChimeraMonk: mutation text without lines");
            return new P(string.Join("\n", l.Select(x => x.Ko)), string.Join("\n", l.Select(x => x.En)));
        }

        // 레벨 구간(같은 값이 이어지는 구간): (첫 레벨, 끝 레벨, 값).
        static List<Tuple<int, int, string>> Segments(Func<int, string> at)
        {
            var segs = new List<Tuple<int, int, string>>();
            int start = Min;
            string cur = at(Min);
            for (int l = Min + 1; l <= Max + 1; l++)
            {
                string v = l <= Max ? at(l) : null;
                if (v == cur) continue;
                if (string.IsNullOrEmpty(cur)) throw new InvalidOperationException("ChimeraMonk: mutation text value for level " + start + " is empty");
                segs.Add(Tuple.Create(start, l - 1, cur));
                start = l; cur = v;
            }
            return segs;
        }

        // 「a~b레벨 값, c레벨부터 값」. 구간이 하나면 값만.
        static P Ranges(Func<int, string> at)
        {
            var segs = Segments(at);
            if (segs.Count == 1) return new P(segs[0].Item3, segs[0].Item3);
            return new P(
                string.Join(", ", segs.Select(s => (s.Item1 == s.Item2 ? s.Item1 + "레벨 " : s.Item2 == Max ? s.Item1 + "레벨부터 " : s.Item1 + "~" + s.Item2 + "레벨 ") + s.Item3)),
                string.Join(", ", segs.Select(s => s.Item3 + (s.Item1 == s.Item2 ? " at level " + s.Item1 : s.Item2 == Max ? " from level " + s.Item1 : " at levels " + s.Item1 + "-" + s.Item2))));
        }

        // 변이약 중 값의 나열 「값 / 값 / 값」 — 기본 값과 같은 레벨 구간이어야 한다(구간이 어긋나면 던짐: 나열이 어느 레벨의 값인지 알 수 없게 된다).
        static string Steps(Func<int, string> baseAt, Func<int, string> boostedAt)
        {
            var a = Segments(baseAt);
            var b = Segments(boostedAt);
            if (!a.Select(s => s.Item1).SequenceEqual(b.Select(s => s.Item1))) throw new InvalidOperationException("ChimeraMonk: mutation text: boosted values change at other levels than the base values");
            return string.Join(" / ", b.Select(s => s.Item3));
        }

        // 값 문구가 언어마다 다른 경우(단위 「피트」 / "ft").
        static P RangesP(Func<int, P> at) { return new P(Ranges(l => at(l).Ko).Ko, Ranges(l => at(l).En).En); }
        static P StepsP(Func<int, P> baseAt, Func<int, P> boostedAt) { return new P(Steps(l => baseAt(l).Ko, l => boostedAt(l).Ko), Steps(l => baseAt(l).En, l => boostedAt(l).En)); }

        static string Plus(int v)
        {
            if (v <= 0) throw new ArgumentOutOfRangeException("v", v, "ChimeraMonk: mutation text bonus must be positive");
            return "+" + v;
        }

        static string Name(Dictionary<string, (string ko, string en)> d, string key, bool ko)
        {
            (string ko, string en) v;
            if (!d.TryGetValue(key + ".name", out v)) throw new KeyNotFoundException("ChimeraMonk: mutation text needs the name " + key + ".name");
            return ko ? v.ko : v.en;
        }

        static P NameOf(Dictionary<string, (string ko, string en)> d, string key) { return new P(Name(d, key, true), Name(d, key, false)); }

        static void Put(Dictionary<string, (string ko, string en)> d, string key, P text) { d.Add(key + ".desc", (text.Ko, text.En)); }

        internal static void AddTo(Dictionary<string, (string ko, string en)> d)
        {
            if (d == null) throw new ArgumentNullException("d");
            int gate = ChimeraRules.Levels.GateLevel;

            // ---- 규칙에 없는 값(엔진·본편 블루프린트). 시험 `strings: mutation text` 가 blueprints.zip 과 대조한다(엔진 IL 값은 출처만).
            int dcBase = 10;            // 엔진 ContextCalculateAbilityParamsBasedOnClass: DC = 10 + 직업 레벨 / 2 + 능력 수정치(게임 안 점검 S7 DC 20 · S14 DC 17 로 확인, README)
            int secondaryPenalty = 5;   // 엔진 RuleCalculateAttackBonusWithoutTarget.OnTrigger IL_0154 ldc.i4.s -5 (보조 자연 공격 명중 페널티)
            int capTail = 1, capBite = 1, capGore = 1, capClaw = 2; // 본편 Root/SystemMechanics/BlueprintPersonageLimits.jbp m_DopLimbMaxCount (엔진 UnitBody.AddAdditionalLimb IL_0012~0031)
            int wingsDodge = 3;         // 본편 Classes/Basic/Wings/BuffWingsMutagen.jbp ACBonusAgainstAttacks.ArmorClassBonus (AgainstMeleeOnly, Dodge)
            int scentNear = 10, scentMid = 20, scentFar = 30; // 본편 ShifterAspectWolfScent10/20/30.jbp Blindsense.Range
            int scentMidLevel = 8;      // 본편 ShifterAspectWolfBuff.jbp AddFeatureOnClassLevel Level 8 (Scent10 은 그 전, 상위 특성은 그 뒤)
            int diehardLevel = 8;       // 본편 ShifterAspectWolverineBuff.jbp AddFeatureOnClassLevel Level 8 (Diehard)
            int scentFarLevel = AspectPlan.Graph.SelectMany(g => g.Overrides).Single(o => o.Item1 == Guids.Game.WolfScent30).Item2; // 판정 W1(우리 복제본의 덮어쓰기 표)

            P mutagen = NameOf(d, "mutagen");
            var whileMutagen = F("{0} 효과 중:", "While the {0} is active:", mutagen);

            // ---- 선택 화면
            var gated = ChimeraRules.Mutations.Where(m => m.GateLevel == gate && !ChimeraRules.AspectStages.Any(s => s.Id == m.Id)).ToArray();
            if (gated.Length == 0) throw new InvalidOperationException("ChimeraMonk: mutation text: no mutation gated at monk level " + gate);
            // 선행 변이를 요구하는 것은 부분 변환 뒤 단계뿐이다(2026-11-17: 추가 꼬리 삭제로 「추가 꼬리 — 꼬리 변이 필요」 문구가 없어졌다). 다른 변이가 선행을 갖게 되면 던진다 — 전제 조건 줄에 그 문구를 넣어야 한다.
            var requiring = ChimeraRules.Mutations.Where(m => m.Requires != null && !ChimeraRules.AspectStages.Any(s => s.Id == m.Id)).ToArray();
            if (requiring.Length != 0) throw new InvalidOperationException("ChimeraMonk: mutation text expects no non-aspect mutation with a required mutation, got " + string.Join(",", requiring.Select(m => m.Id)) + " (the prerequisite line of the selection text does not mention it)");
            var laterStages = ChimeraRules.AspectStages.Where(s => s.Requires != null).ToArray();
            if (laterStages.Length == 0) throw new InvalidOperationException("ChimeraMonk: mutation text: no later partial aspect stage");
            P alch = Ranges(l => Plus(MutagenPlan.Progression(ChimeraRules.Dials.MutagenAlchemical, ChimeraRules.Levels.MutagenTierLevels)[l - 1]));
            P nat = Ranges(l => Plus(MutagenPlan.Progression(ChimeraRules.Dials.MutagenNaturalArmor, ChimeraRules.Levels.MutagenTierLevels)[l - 1]));
            Put(d, "mut.selection", Lines(
                F("몽크 {0}레벨에 변이를 하나씩 고른다(모두 {1}번, 선택지 {2}종). 같은 변이는 한 번만 고를 수 있다.",
                  "Choose one mutation at each of monk levels {0} ({1} picks, {2} options). Each mutation can be taken only once.",
                  string.Join(", ", ChimeraRules.Levels.MutationLevels), ChimeraRules.Dials.MutationPicks, ChimeraRules.Dials.MutationOptions),
                F("전제 조건: {0} — 몽크 {1}레벨부터.",
                  "Prerequisites: {0} need monk level {1}.",
                  new P(string.Join("·", gated.Select(m => Name(d, "mut." + m.Id, true))), string.Join(", ", gated.Select(m => Name(d, "mut." + m.Id, false)))), gate),
                Lines(laterStages.Select(s => F("{0} — {1} 변이와 몽크 {2}레벨 필요.", "{0} needs {1} and monk level {2}.", NameOf(d, "mut." + s.Id), NameOf(d, "mut." + s.Requires), s.GateLevel)).ToArray()),
                F("{0}: 키메라 몽크가 {1}레벨에 얻는 능력이다. 일반 행동으로 마시면 몽크 레벨과 같은 수의 라운드 동안 힘·민첩·지혜 가운데 고른 하나에 연금술 보너스(몽크 {2})와 AC에 자연 방어 보너스(몽크 {3})를 얻는다. 하루에 {4} + 지혜 수정치만큼(최소 {5}회) 쓸 수 있고, 변신 중에는 마실 수 없다.",
                  "{0}: an ability the Chimera Monk gains at level {1}. Drinking it is a standard action; for a number of rounds equal to your monk level you gain an alchemical bonus to Strength, Dexterity or Wisdom (your choice; {2}) and a natural armor bonus to AC ({3}). Usable {4} + your Wisdom modifier times per day (minimum {5}); it cannot be drunk while polymorphed.",
                  mutagen, Min, alch, nat, ChimeraRules.Dials.MutagenDailyBase, ChimeraRules.Dials.MutagenDailyMin),
                F("각 변이 설명의 「{0}」 줄은 변이약이 지속되는 동안에만 받는 추가 효과다.",
                  "The line \"{0}\" in each mutation lists the extra effect you get only while the mutagen lasts.", whileMutagen)));

            // ---- 자연 공격(사지) 변이
            P dice = Ranges(l => ChimeraRules.LimbDice(l, false).ToString());
            string diceBoosted = Steps(l => ChimeraRules.LimbDice(l, false).ToString(), l => ChimeraRules.LimbDice(l, true).ToString());
            var damageLine = F("피해: 몽크 {0}.", "Damage by monk level: {0}.", dice);
            var primaryLine = F("두 손에 무기를 들지 않으면 주 공격이고, 손에 무기를 들면 보조 공격(명중 굴림 -{0})이 된다. 질풍격과 비무장 타격은 그대로 유지된다.",
                                "It is a primary attack while both hands are empty and a secondary attack (-{0} on the attack roll) while you hold a weapon. Flurry of blows and unarmed strikes are kept as they are.", secondaryPenalty);
            var boostedDice = F("{0} 피해 주사위가 한 단계 위({1}).", "{0} the damage die is one step higher ({1}).", whileMutagen, diceBoosted);
            Func<P, P> gain = noun => F("{0} 자연 공격을 하나 얻는다. 전력 공격 때 다른 공격에 더해 한 번 공격한다.", "Gain one {0} natural attack. In a full attack it is made once, in addition to your other attacks.", noun);
            Func<P, int, P> cap = (noun, n) => F("주의: {0} 공격은 캐릭터당 {1}개까지만 쓰인다(다른 출처의 {0} 공격과 겹치면 피해 주사위가 큰 쪽이 남는다).",
                                                 "Note: a character uses at most {1} {0} attack(s); if another source also grants a {0} attack, the one with the larger damage die stays.", noun, n);
            var hidden = new P("변신 중에는 보이지 않고, 동물 동료에게는 붙지 않는다.", "It is hidden while polymorphed and is never shown on an animal companion.");
            P tail = new P("꼬리", "tail"), bite = new P("물기", "bite"), claw = new P("발톱", "claw"), gore = new P("들이받기", "gore"), slam = new P("추가 팔 후려치기", "extra-arm slam");

            Put(d, "mut.Tail", Lines(gain(tail), damageLine, primaryLine, boostedDice, cap(tail, capTail),
                F("외형: 꼬리가 생긴다. {0}", "Appearance: you grow a tail. {0}", hidden)));
            Put(d, "mut.Bite", Lines(gain(bite), damageLine, primaryLine, boostedDice, cap(bite, capBite)));
            Put(d, "mut.Claw", Lines(gain(claw), new P("손은 그대로 비무장 타격에 쓴다.", "Your hands still make unarmed strikes."), damageLine, primaryLine, boostedDice, cap(claw, capClaw)));
            Put(d, "mut.Horn", Lines(gain(gore), damageLine, primaryLine, boostedDice, cap(gore, capGore),
                F("외형: 뿔이 생긴다(뿔 외형이 없는 종족도 있다). {0}", "Appearance: you grow horns (some races have no horn model). {0}", hidden)));
            Put(d, "mut.ExtraArm", Lines(gain(slam), damageLine, primaryLine,
                F("{0} 피해 주사위가 한 단계 위({1})이고, 이 공격의 명중 굴림 +{2}.", "{0} the damage die is one step higher ({1}) and this attack gets +{2} on the attack roll.", whileMutagen, diceBoosted, ChimeraRules.Dials.ArmMutagenAttack),
                F("전제: 몽크 {0}레벨.", "Prerequisite: monk level {0}.", gate)));
            // ---- 폐지된 변이(소유자 결정 2026-11-17): 선택지에 없다. 이미 고른 캐릭터의 특성 목록에 (숨김이 안 먹는 화면에서) 보일 수 있는 설명.
            if (!ChimeraRules.RetiredMutations.SequenceEqual(new[] { "ExtraTail" })) throw new InvalidOperationException("ChimeraMonk: mutation text has a retired text only for ExtraTail, retired mutations are " + string.Join(",", ChimeraRules.RetiredMutations));
            Put(d, "mut.ExtraTail", Lines(
                new P("폐지된 변이다. 아무 효과도 없다.", "This mutation has been retired. It has no effect."),
                F("폐지 이유: 게임 엔진이 꼬리 공격을 캐릭터당 {0}개로 제한해, 이 변이를 골라도 꼬리 공격 횟수가 늘지 않았다.",
                  "Why: the game engine allows only {0} tail attack per character, so taking this mutation never added an attack.", capTail),
                new P("이미 고른 캐릭터는 재교육으로 다른 변이를 고를 수 있다.", "A character who already took it can retrain and choose another mutation instead.")));

            // ---- 독 · 비늘
            Put(d, "mut.Venom", Lines(
                new P("자연 공격이나 비무장 타격이 명중하면 대상에게 독을 주입한다(라운드당 한 번).", "When one of your natural attacks or unarmed strikes hits, the target is poisoned (once per round)."),
                F("독: {0}라운드 동안 매 라운드 인내 내성 굴림을 하고, 실패할 때마다 건강 능력치 피해 {1}.",
                  "Poison: for {0} rounds the target makes a Fortitude save each round and takes {1} Constitution damage on each failure.", ChimeraRules.Dials.VenomRounds, ChimeraRules.Dials.VenomDie.ToString()),
                F("DC: {0} + 몽크 레벨의 절반 + 지혜 수정치.", "DC: {0} + half your monk level + your Wisdom modifier.", dcBase),
                F("{0} DC +{1}.", "{0} DC +{1}.", whileMutagen, ChimeraRules.Dials.VenomMutagenDc)));
            Put(d, "mut.Scales", Lines(
                new P("AC에 자연 방어 보너스를 얻는다.", "Gain a natural armor bonus to AC."),
                F("수치: 몽크 {0}.", "Value by monk level: {0}.", Ranges(l => Plus(ChimeraRules.ScalesValue(l, false)))),
                F("자연 방어의 부적, 나무껍질 주문, {0}의 자연 방어와 모두 합산된다.", "It stacks with an amulet of natural armor, barkskin and the natural armor of the {0}.", mutagen),
                F("{0} +{1} 추가.", "{0} an additional +{1}.", whileMutagen, ChimeraRules.Dials.ScalesMutagenBonus),
                new P("주의: 같은 종류의 자연 방어를 주는 변신 효과와는 큰 쪽만 적용된다.", "Note: with a polymorph effect that grants the same kind of natural armor, only the larger applies.")));

            // ---- 부분 변환(단계)
            var toggle = new P("신속 행동으로 켜며, 횟수와 시간 제한이 없다.", "Activating it is a swift action; there is no limit on uses or duration.");
            var boostedForms = F("{0} 켜 둔 형상의 수치가 모두 한 단계 위.", "{0} every active aspect is one step higher.", whileMutagen);
            foreach (var s in ChimeraRules.AspectStages)
            {
                if (s.Requires == null)
                    Put(d, "mut." + s.Id, Lines(
                        F("시프터의 형상 {0}종 가운데 하나를 골라, 켜고 끄는 능력으로 얻는다(형상은 다음 화면에서 고른다).", "Choose one of {0} shifter aspects and gain it as an ability you can switch on and off (the aspect is chosen on the next screen).", MutationsPlan.AspectFormKeys.Count),
                        toggle,
                        new P("형상이 주는 수치는 몽크 레벨에 따라 오른다. 뒤 단계의 부분 변환으로 얻은 다른 형상과 동시에 켤 수 있다.", "Its values grow with monk level. It can be active together with the aspects from the later Partial Aspect mutations."),
                        boostedForms));
                else
                    Put(d, "mut." + s.Id, Lines(
                        new P("이미 가진 것과 다른 시프터 형상 하나를, 켜고 끄는 능력으로 더 얻는다(형상은 다음 화면에서 고른다).", "Gain one more shifter aspect, different from those you already have, as an ability you can switch on and off (the aspect is chosen on the next screen)."),
                        toggle,
                        new P("가진 형상은 모두 동시에 켤 수 있다.", "All aspects you have can be active at the same time."),
                        boostedForms,
                        F("전제: {0}, 몽크 {1}레벨.", "Prerequisites: {0}, monk level {1}.", NameOf(d, "mut." + s.Requires), s.GateLevel)));
            }

            // ---- 부분 변환(형상 10종): AspectPlan 의 CRC 행에서 수치를 만든다.
            var animals = new Dictionary<string, P>
            {
                { "Bear", new P("곰", "bear") }, { "Boar", new P("멧돼지", "boar") }, { "Dinosaur", new P("공룡", "dinosaur") }, { "Elephant", new P("코끼리", "elephant") }, { "Horse", new P("말", "horse") },
                { "Lizard", new P("도마뱀", "lizard") }, { "Spider", new P("거미", "spider") }, { "Tiger", new P("호랑이", "tiger") }, { "Wolf", new P("늑대", "wolf") }, { "Wolverine", new P("울버린", "wolverine") },
            };
            var stats = new Dictionary<string, P>
            {
                { "Constitution", new P("건강", "Constitution") }, { "Strength", new P("힘", "Strength") }, { "Dexterity", new P("민첩", "Dexterity") },
                { "SaveFortitude", new P("인내 내성 굴림", "Fortitude saves") }, { "SaveReflex", new P("반사 내성 굴림", "Reflex saves") }, { "Initiative", new P("우선권", "initiative") },
                { "Speed", new P("이동 속도", "speed") }, { "AC", new P("AC", "AC") }, { "AdditionalDamage", new P("무기 피해 굴림", "weapon damage rolls") }, { "HitPoints", new P("생명점", "hit points") },
            };
            // 보너스 종류: (이름, 같은 종류끼리 합산되는가 — 설명 문구용. 실제 합산은 엔진 ModifierDescriptorHelper.IsStackable 이 정한다)
            var kinds = new Dictionary<string, Tuple<P, bool>>
            {
                { "Inherent", Tuple.Create(new P("고유 보너스", "inherent"), false) }, { "Racial", Tuple.Create(new P("종족 보너스", "racial"), false) },
                { "Dodge", Tuple.Create(new P("피하기 보너스", "dodge"), true) }, { "UntypedStackable", Tuple.Create(new P("보너스(종류 없음)", "untyped"), true) },
            };
            var feet = new P("피트", " ft");
            foreach (var form in AspectPlan.Forms)
            {
                P animal;
                if (!animals.TryGetValue(form.Key, out animal)) throw new KeyNotFoundException("ChimeraMonk: mutation text has no animal name for aspect " + form.Key);
                var lines = new List<P>
                {
                    F("{0}의 형상을 켜고 끄는 능력으로 얻는다. 켜 둔 동안 아래 보너스를 받는다.", "Gain the aspect of the {0} as an ability you can switch on and off. While it is active you get the bonuses below.", animal),
                };
                var notes = new List<P>();
                foreach (var crc in form.Crcs)
                {
                    P stat; Tuple<P, bool> kind;
                    if (!stats.TryGetValue(crc.Stat, out stat)) throw new KeyNotFoundException("ChimeraMonk: mutation text has no name for the stat " + crc.Stat + " (aspect " + form.Key + ")");
                    if (!kinds.TryGetValue(crc.Descriptor, out kind)) throw new KeyNotFoundException("ChimeraMonk: mutation text has no name for the bonus kind " + crc.Descriptor + " (aspect " + form.Key + ")");
                    var c = crc;
                    if (c.Stat == "HitPoints" && c.Kind == CrcKind.Custom)
                    {
                        // 울버린 생명점 = 몽크 레벨 × 배수(표가 레벨로 나누어떨어지지 않으면 던짐 — 「레벨 × 배수」 문구가 거짓이 된다).
                        Func<bool, Func<int, string>> mult = boosted => l =>
                        {
                            int v = AspectPlan.Value(c, l, boosted);
                            if (v % l != 0) throw new InvalidOperationException("ChimeraMonk: mutation text: " + form.Key + " hit points at level " + l + " (" + v + ") are not a multiple of the level");
                            return (v / l).ToString();
                        };
                        lines.Add(F("{0} {1} = 몽크 레벨 × 배수. 배수: 몽크 {2}. {3} 배수 {4}.", "Bonus to {0} ({1}) = monk level × a multiplier. Multiplier: {2}. {3} multiplier {4}.",
                            stat, kind.Item1, Ranges(mult(false)), whileMutagen, Steps(mult(false), mult(true))));
                    }
                    else
                    {
                        // 이동 속도만 단위(피트)가 붙는다.
                        P unit = c.Stat == "Speed" ? feet : new P(" ", " ");
                        Func<bool, Func<int, P>> val = boosted => l => new P((Plus(AspectPlan.Value(c, l, boosted)) + unit.Ko).Trim(), (Plus(AspectPlan.Value(c, l, boosted)) + unit.En).Trim());
                        lines.Add(F("{0}에 {1}: 몽크 {2}. {3} {4}.", "Bonus to {0} ({1}): {2}. {3} {4}.", stat, kind.Item1, RangesP(val(false)), whileMutagen, StepsP(val(false), val(true))));
                    }
                    if (!kind.Item2) notes.Add(F("주의: {0} — 같은 종류의 다른 보너스와 합산되지 않고 큰 쪽만 적용된다.", "Note: {0} bonuses do not stack with other bonuses of the same kind; only the larger applies.", kind.Item1));
                }
                if (form.Key == "Horse") lines.Add(new P("지구력 특기도 얻는다.", "You also gain the Endurance feat."));
                if (form.Key == "Wolf")
                    lines.Add(F("후각(장님 감각): 몽크 {0}.", "Scent (blindsense): {0}.",
                        RangesP(l => { int v = l < scentMidLevel ? scentNear : l < scentFarLevel ? scentMid : scentFar; return new P(v + feet.Ko, v + feet.En); })));
                if (form.Key == "Wolverine") lines.Add(F("몽크 {0}레벨부터 질긴 목숨 특기도 얻는다.", "From monk level {0} you also gain the Diehard feat.", diehardLevel));
                lines.Add(toggle);
                lines.AddRange(notes.GroupBy(n => n.En).Select(g => g.First()));
                Put(d, "mut.Aspect." + form.Key, Lines(lines.ToArray()));
            }

            // ---- 날개
            Put(d, "mut.Wings", Lines(
                new P("날개가 돋는다.", "You grow wings."),
                F("근접 공격에 대한 AC에 피하기 보너스 +{0}, 험지 효과에 면역.", "+{0} dodge bonus to AC against melee attacks and immunity to difficult terrain.", wingsDodge),
                F("{0} 이동 속도 +{1}피트.", "{0} +{1} ft speed.", whileMutagen, ChimeraRules.Dials.WingsMutagenSpeedFeet),
                new P("외형: 날개가 보인다.", "Appearance: the wings are visible."),
                F("전제: 몽크 {0}레벨.", "Prerequisite: monk level {0}.", gate)));

            // ---- 브레스(부모 + 원소)
            if (ChimeraRules.Dials.BreathDie.Count != 1) throw new InvalidOperationException("ChimeraMonk: mutation text expects a breath die count of one per step");
            Func<int, string> breathAt = l => (l / ChimeraRules.Dials.BreathLevelsPerDie) + "d" + ChimeraRules.Dials.BreathDie.Sides;
            Func<P, P> breathDamage = element => F("범위와 피해: {0}피트 원뿔, 몽크 레벨 {1}당 {2}의 {3} 피해({4}레벨 {5}, {6}레벨 {7}). 반사 내성 굴림에 성공하면 절반.",
                "Area and damage: {0} ft cone, {2} {3} damage per {1} monk levels ({5} at level {4}, {7} at level {6}); a successful Reflex save halves it.",
                ChimeraRules.Dials.BreathConeFeet, ChimeraRules.Dials.BreathLevelsPerDie, ChimeraRules.Dials.BreathDie.ToString(), element, gate, breathAt(gate), Max, breathAt(Max));
            var breathDc = F("DC: {0} + 몽크 레벨의 절반 + 지혜 수정치.", "DC: {0} + half your monk level + your Wisdom modifier.", dcBase);
            var breathUse = F("사용: 일반 행동, 하루 {0}회(휴식하면 회복).", "Use: standard action, {0} times per day (restored by resting).", ChimeraRules.Dials.BreathDailyBase);
            var breathSurge = F("{0} 「폭주」 능력으로 {1}회 더 쓸 수 있다(휴식마다 {1}회분만 차고, 변이약을 다시 마셔도 늘지 않는다).",
                "{0} the Surge ability gives {1} more use (only {1} per rest; drinking the mutagen again does not add more).", whileMutagen, ChimeraRules.Dials.BreathSurgeBase);
            var breathGate = F("전제: 몽크 {0}레벨.", "Prerequisite: monk level {0}.", gate);
            var elements = new Dictionary<string, P> { { "Fire", new P("화염", "fire") }, { "Cold", new P("냉기", "cold") }, { "Acid", new P("산성", "acid") }, { "Electricity", new P("전기", "electricity") } };
            if (!MutationsPlan.BreathElements.Select(e => e.Key).SequenceEqual(elements.Keys)) throw new InvalidOperationException("ChimeraMonk: mutation text element names do not match MutationsPlan.BreathElements");
            Put(d, "mut.Breath", Lines(
                F("원소({0}) 하나를 골라 그 원소의 숨결을 내뿜는 능력을 얻는다(원소는 다음 화면에서 고르며 바꿀 수 없다).", "Choose one element ({0}) and gain a breath weapon of that element (the element is chosen on the next screen and cannot be changed).",
                  new P(string.Join("·", elements.Values.Select(e => e.Ko)), string.Join(", ", elements.Values.Select(e => e.En)))),
                breathDamage(new P("원소", "energy")), breathDc, breathUse, breathSurge, breathGate));
            foreach (var e in MutationsPlan.BreathElements)
                Put(d, e.StringKey, Lines(
                    F("{0} 숨결을 내뿜는 능력을 얻는다. 이 원소를 고르면 다른 원소는 고를 수 없다.", "Gain a breath weapon that deals {0} damage. Choosing this element rules out the others.", elements[e.Key]),
                    breathDamage(elements[e.Key]), breathDc, breathUse, breathSurge));

            // ---- 몸·감각 변이
            int legs = ChimeraRules.Dials.LegsSpeedFeet, legsBoost = ChimeraRules.Dials.LegsMutagenSpeedFeet;
            Put(d, "mut.ExtraLegs", Lines(
                F("이동 속도 +{0}피트.", "+{0} ft speed.", legs),
                F("넘어뜨리기와 황소 돌진에 대한 전법 방어 +{0}.", "+{0} CMD against trip and bull rush.", ChimeraRules.Dials.LegsManeuverDefense),
                F("{0} 이동 속도 +{1}피트 추가(합계 +{2}피트).", "{0} an additional +{1} ft speed (+{2} ft in total).", whileMutagen, legsBoost, legs + legsBoost)));
            Func<int, int, P> perception = (b, m) => Lines(
                F("지각 판정 +{0}.", "+{0} on Perception checks.", b),
                F("{0} 지각 +{1} 추가(합계 +{2}).", "{0} an additional +{1} Perception (+{2} in total).", whileMutagen, m, b + m));
            Put(d, "mut.ExtraEyes", Lines(
                new P("협공당하지 않는다.", "You cannot be flanked."),
                perception(ChimeraRules.Dials.EyesPerception, ChimeraRules.Dials.EyesMutagenPerception),
                F("{0}의 지각 보너스와 합산된다.", "Stacks with the Perception bonus of {0}.", NameOf(d, "mut.ExtraEars"))));
            Put(d, "mut.ExtraEars", Lines(
                new P("눈감고 싸우기 특기를 얻는다.", "Gain the Blind-Fight feat."),
                perception(ChimeraRules.Dials.EarsPerception, ChimeraRules.Dials.EarsMutagenPerception),
                F("{0}의 지각 보너스와 합산된다.", "Stacks with the Perception bonus of {0}.", NameOf(d, "mut.ExtraEyes"))));
            Put(d, "mut.ExtraSenses", Lines(
                F("{0}피트 범위의 장님 감각을 얻는다.", "Gain blindsense with a range of {0} ft.", ChimeraRules.Dials.SensesFeet),
                F("{0} 범위 {1}피트.", "{0} the range is {1} ft.", whileMutagen, ChimeraRules.Dials.SensesMutagenFeet),
                F("전제: 몽크 {0}레벨.", "Prerequisite: monk level {0}.", gate)));
            Put(d, "mut.ExtraHeart", Lines(
                F("생명점 +{0}(레벨과 상관없는 고정값).", "+{0} hit points (a flat amount, not per level).", ChimeraRules.Dials.HeartHp),
                new P("출혈 효과에 면역.", "Immunity to bleed effects."),
                F("{0} 빠른 치유 {1}(매 라운드 생명점 {1} 회복).", "{0} fast healing {1} (recover {1} hit points every round).", whileMutagen, ChimeraRules.Dials.HeartMutagenFastHealing)));
            int surge = ChimeraRules.Dials.BreathSurgeBase, lungSurge = ChimeraRules.Dials.LungSurgeBonus;
            Put(d, "mut.ExtraLung", Lines(
                new P("피로 상태에 면역.", "Immunity to fatigue."),
                F("구름 주문 {0}종(악취 구름·살인구름·정신 안개·산성 안개)이 만드는 영역의 효과에 면역. 독 전체에 대한 면역은 아니다.",
                  "Immunity to the areas created by {0} cloud spells (stinking cloud, cloudkill, mind fog, acid fog). This is not immunity to poison in general.", LungPlan.CloudSpells.Count),
                F("{0} 변이가 있으면: 변이약 효과 중에 쓰는 「폭주」 횟수 +{1}(합계 {2}).", "With the {0} mutation: +{1} use of Surge while the mutagen is active ({2} in total).", NameOf(d, "mut.Breath"), lungSurge, surge + lungSurge),
                new P("주의: 구름 안에 있는 동안 이 변이를 얻으면, 구름에서 나갔다가 다시 들어갈 때부터 적용된다.", "Note: if you gain this while standing in such a cloud, it applies only after you leave and re-enter it.")));
        }
    }
}
