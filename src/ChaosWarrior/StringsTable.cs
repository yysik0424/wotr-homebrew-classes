using System;
using System.Collections.Generic;
using System.Linq;
using Dl = ChaosWarrior.ChaosRules.Dials;

namespace ChaosWarrior
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 붙인다). Main.Load 가 Strings.Configure(Table, Guids.Game.WarpriestClass) 로 넘긴다.
        // 이름과 수치 없는 내부 설명만 리터럴이다. 플레이어가 읽는 설명문(아키타입 · 분노 · 변이 · 고통 · 신속 시전)은 아래 ChaosText 가 규칙 상수(ChaosRules · MutationSpecs · RageStrikeSpec ·
        // QuickenSpec)에서 만든다(개편 계획 2026-11-16 Task O9 — 본보기 chimera-monk 8206a8f StringsTable.MutationText). 문구 속 수치 = 규칙 상수(C4). 용어 = 한글 패치 표기(translation.json 2026-07-27 판 직독).
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();

        static Dictionary<string, (string ko, string en)> Build()
        {
            var d = new Dictionary<string, (string ko, string en)>
            {
                ["archetype.name"] = ("카오스 워리어", "Chaos Warrior"),
                // T4: 혼돈의 분노(설계 §6·§10-b)
                ["rage.name"] = ("혼돈의 분노", "Chaos Rage"),
                // 2026-11-16: 분노 중 자연 무기 단계 버프(숨김 — 플레이어 대면 설명은 rage.desc)
                ["rage.strike.name"] = ("혼돈의 일격", "Chaos Strike"), ["rage.strike.desc"] = ("혼돈의 분노 중 자연 무기 공격의 무기 속성(내부 효과).", "Weapon properties of natural attacks during a chaos rage (internal effect)."),
                ["rage.greater.name"] = ("상급 혼돈의 분노", "Greater Chaos Rage"),
                ["rage.mighty.name"] = ("최상급 혼돈의 분노", "Mighty Chaos Rage"),
                // T5: 광기의 변이(설계 §7) — 표식 · 원소 껍질 · 1레벨 특성
                ["mutation.name"] = ("광기의 변이", "Mutations of Madness"),
                ["mut.Enlarge.name"] = ("거대화", "Enlarge"),
                ["mut.ToughHide.name"] = ("굳은 살갗", "Tough Hide"),
                ["mut.Regeneration.name"] = ("재생", "Regeneration"),
                ["mut.Swiftness.name"] = ("신속", "Swiftness"),
                ["mut.ThornSkin.name"] = ("가시 피부", "Thorn Skin"),
                ["mut.ChaosBlood.name"] = ("혼돈의 피", "Chaos Blood"),
                ["mut.ElementalShell.name"] = ("원소 껍질", "Elemental Shell"),
                ["mut.PainAmplify.name"] = ("고통 증폭", "Amplified Pain"),
                ["mut.ExtraArm.name"] = ("추가 팔", "Extra Arm"),
                ["mut.HorrificForm.name"] = ("끔찍한 형상", "Horrific Form"),
                ["mut.LongLimbs.name"] = ("늘어난 사지", "Long Limbs"),
                ["mut.TwistedLegs.name"] = ("비틀린 다리", "Twisted Legs"),
                ["mut.BlurredEyes.name"] = ("흐린 눈", "Blurred Eyes"),
                ["mut.ExposedNerves.name"] = ("드러난 신경", "Exposed Nerves"),
                ["mut.BurstingVeins.name"] = ("터지는 핏줄", "Bursting Veins"),
                ["mut.SeethingMind.name"] = ("들끓는 정신", "Seething Mind"),
                // 개편 O5: 자연 공격 변이 11종(계획 2026-11-16 원장 A 「한 / 영」)
                ["mut.Fangs.name"] = ("송곳니", "Fangs"),
                ["mut.Claws.name"] = ("발톱", "Claws"),
                ["mut.Horns.name"] = ("뿔", "Horns"),
                ["mut.Tail.name"] = ("꼬리", "Tail"),
                ["mut.Tentacle.name"] = ("촉수", "Tentacle"),
                ["mut.Stinger.name"] = ("독침", "Stinger"),
                ["mut.Wings.name"] = ("날개", "Wings"),
                ["mut.Hooves.name"] = ("굽", "Hooves"),
                ["mut.Talons.name"] = ("맹금 발톱", "Talons"),
                ["mut.Pincer.name"] = ("집게", "Pincer"),
                ["mut.NeuroStinger.name"] = ("신경독침", "Neuro-Stinger"),
                // 개편 O6: 양면 변이 5종(계획 2026-11-16 원장 A 「한 / 영」)
                ["mut.Bloodthirst.name"] = ("피의 갈증", "Bloodthirst"),
                ["mut.BloatedMuscle.name"] = ("비대한 근육", "Bloated Muscle"),
                ["mut.StoneShell.name"] = ("돌 껍질", "Stone Shell"),
                ["mut.RawNerves.name"] = ("과민한 신경", "Raw Nerves"),
                ["mut.PainResonance.name"] = ("고통 공명", "Pain Resonance"),
                ["shell.Fire.name"] = ("원소 껍질: 화염", "Elemental Shell: Fire"),
                ["shell.Cold.name"] = ("원소 껍질: 냉기", "Elemental Shell: Cold"),
                ["shell.Acid.name"] = ("원소 껍질: 산성", "Elemental Shell: Acid"),
                ["shell.Electricity.name"] = ("원소 껍질: 전기", "Elemental Shell: Electricity"),
                // T6: 끔찍한 형상의 동요(한글 패치 ConditionShaken = 「동요」)
                ["horror.shaken.name"] = ("동요(끔찍한 형상)", "Shaken (Horrific Form)"),
                ["horror.shaken.desc"] = ("끔찍한 형상 곁에서 동요한다.", "Shaken next to a horrific form."),
                // T7: 고통(설계 §8·§10-b·§11)
                ["pain.name"] = ("고통", "Pain"),
                ["pain.probe.name"] = ("고통 면역 판정", "Pain immunity probe"), ["pain.probe.desc"] = ("내부 판정용.", "Internal check."),
                // 개편 O7 · 독립 검토 R3-F1: 숨김 초과 버프(옛 판) · 숨김 계수 버프
                ["pain.over.name"] = ("고통(초과, 옛 판)", "Pain (overflow, legacy)"), ["pain.over.desc"] = ("이전 판의 고통 초과 겹(내부 — 다음 명중에서 고통 계수로 옮겨진다).", "Pain stacks beyond the penalty cap from an older version (internal; moved into the pain counter on the next hit)."),
                ["pain.count.name"] = ("고통(계수)", "Pain (counter)"), ["pain.count.desc"] = ("고통 총 겹의 내부 계수(해제되지 않는다).", "Internal counter of the total pain stacks (cannot be dispelled)."),
                ["pain.save.name"] = ("고통의 광기", "Madness of Pain"), ["pain.save.desc"] = ("고통 문턱의 의지 내성 출처.", "Source of the pain threshold Will save."),
                ["pain.aoe.name"] = ("번지는 광기", "Spreading Madness"),
                // 개편 O8: 분노 중 신속 시전 권리 버프 6개(설명 quicken.desc.N 은 ChaosText 가 상한마다 만든다)
                ["quicken.name"] = ("광란의 시전", "Frenzied Casting"),
                // 점검 전용 블루프린트(CheckOnlyBlueprints — 점검 러너가 돌 때만 만든다)
                ["check.name"] = ("카오스 워리어 점검용", "Chaos Warrior check helper"), ["check.desc"] = ("자동 점검 전용 효과.", "Automated check helper effect."),
            };
            ChaosText.AddTo(d);
            return d;
        }
    }

    // [순수] 플레이어가 읽는 설명문 생성기(개편 계획 2026-11-16 Task O9 · 본보기 chimera-monk 8206a8f MutationText). 틀(문자열 리터럴)에는 숫자가 없다 — 수치는 {n} 자리에 규칙 상수에서만 들어온다(시험 `strings: overhaul text`).
    // 줄은 "\n" 으로 잇는다(레벨업 화면 · 툴팁은 m_Description 전체를 읽는다 — 엔진 TooltipTemplateLevelUpFeature.Prepare IL_002E → BlueprintUnitFact.get_Description, m_DescriptionShort 는 읽지 않는다).
    // 틀에 넘긴 인자를 틀이 쓰지 않거나 · 인자가 null 이거나 · 변이에 문구 규칙이 없으면 던진다(조용히 빠진 수치 · 변이 없음).
    static class ChaosText
    {
        sealed class P
        {
            public readonly string Ko, En;
            public P(string ko, string en)
            {
                if (string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en)) throw new ArgumentException("ChaosWarrior: description text pair with an empty side");
                Ko = ko; En = en;
            }
        }

        static P F(string ko, string en, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args", "ChaosWarrior: description argument " + i + " is null (" + en + ")");
                string slot = "{" + i + "}";
                if (!ko.Contains(slot) || !en.Contains(slot)) throw new ArgumentException("ChaosWarrior: description template does not use argument " + i + " in both languages (" + en + ")");
            }
            return new P(string.Format(ko, args.Select(a => a is P ? (object)((P)a).Ko : a).ToArray()), string.Format(en, args.Select(a => a is P ? (object)((P)a).En : a).ToArray()));
        }

        static P Lines(params P[] lines)
        {
            var l = lines.Where(x => x != null).ToArray();
            if (l.Length == 0) throw new ArgumentException("ChaosWarrior: description without lines");
            return new P(string.Join("\n", l.Select(x => x.Ko)), string.Join("\n", l.Select(x => x.En)));
        }

        // 부호 있는 수치: ko 는 U+2212(−), en 은 ASCII(-). 0 은 던진다(부호 없는 「0」 효과는 설명할 것이 없다 — 표가 깨진 것이다).
        static P Signed(int v)
        {
            if (v == 0) throw new ArgumentOutOfRangeException("v", v, "ChaosWarrior: a described effect value is zero");
            return v > 0 ? new P("+" + v, "+" + v) : new P("−" + (-v), "-" + (-v));
        }

        static string Plus(int v)
        {
            if (v <= 0) throw new ArgumentOutOfRangeException("v", v, "ChaosWarrior: description bonus must be positive");
            return "+" + v;
        }

        static string Dice(int count, int sides)
        {
            if (count <= 0 || sides <= 0) throw new ArgumentOutOfRangeException("count", count, "ChaosWarrior: description dice " + count + "d" + sides);
            return count + "d" + sides;
        }

        static string Name(Dictionary<string, (string ko, string en)> d, string key, bool ko)
        {
            (string ko, string en) v;
            if (!d.TryGetValue(key + ".name", out v)) throw new KeyNotFoundException("ChaosWarrior: description needs the name " + key + ".name");
            return ko ? v.ko : v.en;
        }

        static P NameOf(Dictionary<string, (string ko, string en)> d, string key) { return new P(Name(d, key, true), Name(d, key, false)); }

        static void Put(Dictionary<string, (string ko, string en)> d, string key, P text) { d.Add(key, (text.Ko, text.En)); }

        // 「5레벨마다 +1」의 두 수(첫 단계 레벨 = 간격 · 단계마다 +1). 표가 등간격 · +1 이 아니면 던진다(그 문구가 거짓이 된다).
        static int ScaleStep()
        {
            var s = ChaosRules.Levels.ScaleStepLevels;
            if (s.Length == 0) throw new InvalidOperationException("ChaosWarrior: description: no scale step levels");
            for (int i = 0; i < s.Length; i++) if (s[i] != s[0] * (i + 1)) throw new InvalidOperationException("ChaosWarrior: description: scale steps [" + string.Join(",", s) + "] are not every " + s[0] + " levels");
            for (int l = ChaosRules.Levels.MinLevel; l <= ChaosRules.Levels.MaxLevel; l++)
                if (ChaosRules.ScaledByFive(0, l) != l / s[0]) throw new InvalidOperationException("ChaosWarrior: description: ScaledByFive does not add one per " + s[0] + " levels at level " + l);
            return s[0];
        }

        // 효과 행 하나의 값(레벨과 무관한 행만 — 레벨마다 다르면 던진다: 고정 수치로 적을 수 없다).
        static int Fixed(string id, string component, string stat = null)
        {
            var rows = MutationSpecs.RowsFor(id).Where(r => r.Component == component && r.Stat == stat).ToArray();
            if (rows.Length != 1) throw new InvalidOperationException("ChaosWarrior: description: " + id + " has " + rows.Length + " effect row(s) " + component + "/" + (stat ?? "-"));
            int v = rows[0].Value(ChaosRules.Levels.MinLevel);
            for (int l = ChaosRules.Levels.MinLevel; l <= ChaosRules.Levels.MaxLevel; l++) if (rows[0].Value(l) != v) throw new InvalidOperationException("ChaosWarrior: description: " + id + " " + component + " changes with level");
            return v;
        }

        // 엔진 사지 범주 → 공격 이름(한글 패치 표기: 물기 · 발톱 · 들이받기 · 꼬리 · 후려치기 · 촉수 · 침 · 날개 · 발굽 · 집게). 표에 없는 범주는 던진다.
        static readonly Dictionary<string, P> AttackNoun = new Dictionary<string, P>
        {
            { "Slam", new P("후려치기", "slam") }, { "Bite", new P("물기", "bite") }, { "Claw", new P("발톱", "claw") }, { "Gore", new P("들이받기", "gore") }, { "Tail", new P("꼬리", "tail") },
            { "Tentacle", new P("촉수", "tentacle") }, { "Sting", new P("침", "sting") }, { "Wing", new P("날개", "wing") }, { "Hoof", new P("발굽", "hoof") }, { "Talon", new P("갈고리발톱", "talon") },
        };
        // 자연 공격 변이 중 사지 범주와 다른 이름을 쓰는 것(집게 = Claw 범주의 집게).
        static readonly Dictionary<string, P> AttackNounOverride = new Dictionary<string, P> { { "Pincer", new P("집게", "pincer") }, { "ExtraArm", new P("추가 팔의 후려치기", "extra-arm slam") } };

        // 분노 중 자연 무기 속성 이름(RageStrikeSpec 행의 속성 · 한글 패치: 한철 = Cold Iron).
        static readonly Dictionary<string, P> StrikeWord = new Dictionary<string, P>
        {
            { "Magic", new P("마법", "magic") }, { "ColdIron", new P("한철", "cold iron") }, { "Silver", new P("은", "silver") }, { "Chaotic", new P("혼돈", "chaotic") }, { "Evil", new P("악", "evil") },
        };

        static readonly Dictionary<string, P> ElementName = new Dictionary<string, P>
        {
            { "Fire", new P("화염", "fire") }, { "Cold", new P("냉기", "cold") }, { "Acid", new P("산성", "acid") }, { "Electricity", new P("전기", "electricity") },
        };

        static P Element(string e)
        {
            P p;
            if (!ElementName.TryGetValue(e, out p)) throw new KeyNotFoundException("ChaosWarrior: description has no name for the element " + e);
            return p;
        }

        // 신속 시전 상한 표 「1~6레벨 1, 7~9레벨 2, …」 / "1 at levels 1-6, …".
        static P QuickenRanges()
        {
            var b = ChaosRules.QuickenCaps;
            if (b.Length == 0) throw new InvalidOperationException("ChaosWarrior: description: empty quicken cap table");
            return new P(string.Join(", ", b.Select(x => x.MinLevel + "~" + x.MaxLevel + "레벨 " + x.MaxSpellLevel)), string.Join(", ", b.Select(x => x.MaxSpellLevel + " at levels " + x.MinLevel + "-" + x.MaxLevel)));
        }

        // 개수 표 「1~5레벨 1~3개, …」 / "1-3 at levels 1-5, …".
        static P CountRanges()
        {
            var b = ChaosRules.CountBands;
            if (b.Length == 0) throw new InvalidOperationException("ChaosWarrior: description: empty count band table");
            return new P(string.Join(", ", b.Select(x => x.MinLevel + "~" + x.MaxLevel + "레벨 " + x.MinCount + "~" + x.MaxCount + "개")), string.Join(", ", b.Select(x => x.MinCount + "-" + x.MaxCount + " at levels " + x.MinLevel + "-" + x.MaxLevel)));
        }

        static int Cap(string category)
        {
            int c = ChaosRules.LimbCapExpected(category);
            return c;
        }

        internal static void AddTo(Dictionary<string, (string ko, string en)> d)
        {
            if (d == null) throw new ArgumentNullException("d");
            int step = ScaleStep();
            var once = new P("한 분노에 한 번만 나온다(다시 뽑히면 다른 변이를 뽑는다).", "Appears at most once per rage (if drawn again, another mutation is drawn instead).");
            var again = new P("겹칠 때마다 다시 더해진다.", "Applies again each time it is drawn.");
            var secondary = F("손에 무기를 든 채면 보조 공격이다(명중 굴림 {0}, 힘 보너스 절반).", "While you hold a weapon it is a secondary attack ({0} on the attack roll, half the Strength bonus).", Signed(Dl.SecondaryNaturalAttackPenalty));
            var polyNat = new P("변신 중에 켠 분노에서는 나오지 않는다.", "It does not appear in a rage begun while polymorphed.");
            string dice = Dice(Dl.NaturalAttackDiceCount, Dl.NaturalAttackDiceSides);
            int clawCap = Cap("Claw");

            // ---- 아키타입
            Put(d, "archetype.desc", Lines(
                F("신의 광기를 받은 전사 사제(혼돈 중립·혼돈 악만). 축복은 하나만 받고, 열정·에너지 방출·성스러운 갑옷 대신 {0}레벨부터 혼돈의 분노에 든다.",
                  "A warrior priest touched by divine madness (chaotic neutral or chaotic evil only). Gains one blessing instead of two and, instead of fervor, channel energy and sacred armor, enters a chaos rage from level {0}.",
                  ChaosRules.Levels.RageTierLevels[0]),
                F("분노할 때마다 몸이 무작위로 뒤틀리고(변이 {0}가지), 분노 중 근접 무기로 적을 칠 때마다 고통이 쌓여 {1}겹에서 적이 미쳐 버릴 수 있다.",
                  "Each rage twists the body at random ({0} mutations), and every melee weapon hit on an enemy while raging stacks pain; at {1} stacks the enemy may go mad.",
                  Dl.MutationOptions, Dl.PainConfusionThreshold),
                new P("전투당 한 번, 분노 중 처음 시전하는 조건에 맞는 워프리스트 주문은 신속 행동으로 나간다. 지혜는 주문과 분노 횟수, 고통의 내성 DC를 결정한다.", "Once per combat, the first eligible warpriest spell cast while raging is cast as a swift action. Wisdom determines spellcasting, daily rage uses and the save DC for pain.")));

            // ---- 혼돈의 분노
            int[] tiers = ChaosRules.Levels.RageTierLevels;
            if (tiers.Length != Dl.RageStrCon.Length || tiers.Length != Dl.RageWill.Length || tiers.Length != 3) throw new InvalidOperationException("ChaosWarrior: description expects three rage tiers (base, greater, mighty)");
            var strike = RageStrikeSpec.Tiers.Select((t, i) =>
            {
                var props = new List<string>();
                if (t.Magic) props.Add("Magic");
                props.AddRange(t.Materials);
                props.AddRange(t.Alignments);
                var words = props.Select(p => { P w; if (!StrikeWord.TryGetValue(p, out w)) throw new KeyNotFoundException("ChaosWarrior: description has no word for the strike property " + p); return w; }).ToList();
                var ko = (i == 0 ? "워프리스트 " : "") + t.Level + "레벨부터 " + string.Join("·", words.Select(w => w.Ko));
                var en = string.Join(" and ", words.Select(w => w.En)) + (i == 0 ? " weapons" : "") + " from " + Ordinal(t.Level) + (i == 0 ? " warpriest" : "") + " level";
                return new P(ko, en);
            }).ToList();
            if (strike.Count == 0) throw new InvalidOperationException("ChaosWarrior: description: no rage strike tier");
            var strikeKo = string.Join(", ", strike.Select(s => s.Ko));
            var strikeEn = strike.Count == 1 ? strike[0].En : string.Join(", ", strike.Take(strike.Count - 1).Select(s => s.En)) + ", and " + strike[strike.Count - 1].En;
            Put(d, "rage.desc", Lines(
                F("자유 행동으로 분노에 든다(전투 중에만, 전투가 끝나면 풀린다). 켤 때마다 하루 횟수를 하나 쓴다: 워프리스트 레벨의 절반 + 지혜 수정치(최소 {0}회).",
                  "Enter a rage as a free action (in combat only; it ends when combat ends). Each activation spends one daily use (half your warpriest level + Wisdom modifier per day, minimum {0}).", ChaosRules.RageResourceSpec.Min),
                F("분노 중 힘·건강 {0}, 의지 내성 {1}, AC {2}({3}레벨 {4}/{5}, {6}레벨 {7}/{8}). 분노 중에도 주문을 시전할 수 있고 끝나도 피로하지 않다. 마지막 횟수를 써도 끄거나 전투가 끝날 때까지 유지된다.",
                  "While raging: {0} Strength and Constitution, {1} Will saves, {2} AC ({4}/{5} at level {3}, {7}/{8} at level {6}). You can cast spells while raging and are not fatigued afterwards; spending the last use keeps the rage until you end it or combat ends.",
                  Signed(Dl.RageStrCon[0]), Signed(Dl.RageWill[0]), Signed(Dl.RageAc), tiers[1], Signed(Dl.RageStrCon[1]), Signed(Dl.RageWill[1]), tiers[2], Signed(Dl.RageStrCon[2]), Signed(Dl.RageWill[2])),
                F("분노 중 자연 무기 공격(손에 든 무기·비무장 타격 제외)은 {0} 무기로 취급된다(본인 성향과 무관). 피해 감소·비실체·재생처럼 무기 속성을 보는 판정에만 쓰이며, 명중·피해 보너스는 없다.",
                  "While raging, your natural weapon attacks (not manufactured weapons or unarmed strikes) are treated as {0}, whatever your own alignment. It matters for damage reduction, incorporeal creatures and regeneration; there is no bonus on attack or damage rolls.",
                  new P(strikeKo, strikeEn)),
                F("광란의 시전: 전투당 한 번, 분노 중 처음 시전하는 워프리스트 주문 하나가 신속 행동으로 나간다(대상 무관, 주문 슬롯은 평소대로 쓴다). 주문 레벨 상한(주문 슬롯 레벨 — 메타매직 비용 포함): {0}.",
                  "Frenzied Casting: once per combat, the first eligible warpriest spell you cast while raging is cast as a swift action (any target; it still uses its spell slot). Spell level cap (the slot level, metamagic cost included): {0}.", QuickenRanges()),
                new P("이 효과를 쓴 뒤에는 다음 전투에서 다시 쓸 수 있다. 자세한 규칙은 분노 중 붙는 「광란의 시전」 효과의 설명.", "Frenzied Casting refreshes in your next combat.")));
            Put(d, "rage.greater.desc", F("{0}레벨부터 분노 보너스: 힘·건강 {1}, 의지 내성 {2}.", "From level {0} the rage bonus becomes {1} Strength and Constitution and {2} Will.", tiers[1], Signed(Dl.RageStrCon[1]), Signed(Dl.RageWill[1])));
            Put(d, "rage.mighty.desc", F("{0}레벨부터 분노 보너스: 힘·건강 {1}, 의지 내성 {2}.", "From level {0} the rage bonus becomes {1} Strength and Constitution and {2} Will.", tiers[2], Signed(Dl.RageStrCon[2]), Signed(Dl.RageWill[2])));

            // ---- 광기의 변이(1레벨 특성)
            Put(d, "mutation.desc", Lines(
                F("혼돈의 분노에 들 때마다 몸이 뒤틀린다: 변이 {0}가지(자연 공격 {1} · 이로운 {2} · 해로운 {3} · 양면 {4}) 가운데 개수만큼 무작위로 뽑는다. 분노가 끝나면 모두 풀린다.",
                  "Each time you enter a chaos rage your body twists: mutations are drawn at random from {0} (natural attacks {1}, beneficial {2}, harmful {3}, two-sided {4}). They all end with the rage.",
                  Dl.MutationOptions, Dl.NaturalAttackMutations, Dl.BeneficialMutations, Dl.HarmfulMutations, Dl.TwoSidedMutations),
                F("개수는 레벨 구간마다 고르게 정한다: {0}(최대 {1}개).", "The count is rolled uniformly by level: {0} (at most {1}).", CountRanges(), ChaosRules.MaxDraws),
                new P("같은 변이가 다시 뽑힐 수 있고, 겹치면 효과가 쌓인다(각 변이 설명).", "The same mutation can be drawn again and then stacks (see each mutation)."),
                F("지금 효과가 없는 변이는 뽑지 않고 다른 것을 뽑는다: 자리가 찬 자연 공격(물기 {0} · 들이받기 {1} · 꼬리 {2} · 발톱과 집게 합쳐 {3} — 종족·다른 효과의 같은 공격도 센다), 이미 나온 굳은 살갗 · 끔찍한 형상 · 늘어난 사지.",
                  "A mutation that would do nothing now is skipped and another is drawn: a natural attack whose slots are full (bite {0}, gore {1}, tail {2}, claws and pincers together {3} — the same attacks from your race or other effects count), or Tough Hide, Horrific Form or Long Limbs already drawn.",
                  Cap("Bite"), Cap("Gore"), Cap("Tail"), clawCap),
                new P("변신 중에 켠 분노에서는 자연 공격 변이가 나오지 않는다.", "In a rage begun while polymorphed, natural attack mutations do not appear."),
                F("손에 무기를 든 채면 자연 공격은 보조 공격이다(명중 굴림 {0}, 힘 보너스 절반).", "While you hold a weapon, natural attacks are secondary attacks ({0} on the attack roll, half the Strength bonus).", Signed(Dl.SecondaryNaturalAttackPenalty))));

            // ---- 변이 32종(ChaosRules.Mutations 순서 — 문구 규칙이 없는 변이는 던진다)
            foreach (var m in ChaosRules.Mutations)
            {
                var row = MutationSpecs.StackOf(m.Id);
                P text;
                if (m.Kind == ChaosRules.MutationKind.NaturalAttack) text = NaturalText(m.Id, row, dice, secondary, polyNat, clawCap);
                else text = OtherText(d, m.Id, row, step, once, again);
                Put(d, "mut." + m.Id + ".desc", text);
            }

            // ---- 원소 껍질 4종
            foreach (var e in Dl.ShellElements)
                Put(d, "shell." + e + ".desc", Lines(
                    F("{0} 저항 {1}.", "Resistance {1} to {0}.", Element(e), Fixed(MutationSpecs.ShellPrefix + e, "AddDamageResistanceEnergy")),
                    F("같은 원소가 다시 뽑히면 저항이 {0}씩 더해진다.", "Drawn again with the same element: {0} more resistance each time.", Fixed(MutationSpecs.ShellPrefix + e, "AddDamageResistanceEnergy"))));

            // ---- 고통
            Put(d, "pain.desc", Lines(
                F("카오스 워리어가 분노 중 근접 무기로 맞힐 때마다 고통이 쌓인다: 명중마다 {0}겹, 고통 증폭 · 고통 공명마다 {1}겹 더(겹친 만큼), 신경독침 명중이면 {2}겹 더.",
                  "Each melee weapon hit by a raging chaos warrior stacks pain: {0} per hit, {1} more for each Amplified Pain and Pain Resonance (as many times as drawn), {2} more for a Neuro-Stinger hit.",
                  Dl.PainStacksPerHit, Dl.PainAmplifyExtra == Dl.PainResonanceExtra ? Dl.PainAmplifyExtra : Throw<int>("pain: amplify and resonance extras differ — the line names one value"), Dl.PainNeuroStingExtra),
                F("겹마다 명중 굴림·내성 굴림·기술 판정 {0} — 페널티는 {1}겹({2})까지만 커진다. 마지막으로 쌓인 뒤 {3}라운드 유지.",
                  "{0} on attack rolls, saving throws and skill checks per stack — the penalty grows only up to {1} stacks ({2}). Lasts {3} rounds after the last stack.",
                  Signed(Dl.PainPerStack), Dl.PainPenaltyCap, Signed(Dl.PainPerStack * Dl.PainPenaltyCap), Dl.PainDurationRounds),
                F("{0}겹 이상이 되는 순간 의지 내성(DC {1} + 워프리스트 레벨의 절반 + 지혜 수정치) — 실패하면 {2}라운드 혼란. 성패와 무관하게 고통은 사라진다.",
                  "On reaching {0} or more stacks the target makes a Will save (DC {1} + half the warpriest level + Wisdom modifier) or is confused for {2} rounds; the pain ends either way.",
                  Dl.PainConfusionThreshold, Dl.PainDcBase, Dice(Dl.ConfusionDiceCount, Dl.ConfusionDiceSides)),
                F("여러 카오스 워리어의 고통은 따로 쌓이고 가장 큰 것 하나만 적용된다. 정신 영향 효과에 면역인 대상은 {0}겹에서 멈춘다.",
                  "Stacks from different chaos warriors are separate and only the largest applies. Creatures immune to mind-affecting effects stop at {0} stacks.", Dl.PainConfusionThreshold),
                new P("마법 해제는 페널티만 지우고 겹 수는 남는다 — 다음 명중에서 페널티가 겹 수에 맞게 돌아온다.", "Dispel magic removes only the penalty; the stack count stays and the next hit restores the penalty to match it.")));
            Put(d, "pain.aoe.desc", F("{0}레벨: 고통 문턱에서 대상 반경 {1}피트 안의 다른 적도 같은 의지 내성을 한다.", "At level {0}, when pain reaches its threshold, other enemies within {1} ft of the target make the same Will save.",
                ChaosRules.Levels.PainAoeLevel, Dl.PainAoeFeet));

            // ---- 신속 시전 권리 6개(상한마다)
            foreach (var r in QuickenSpec.Rights)
                Put(d, QuickenSpec.DescKey(r.MaxSpellLevel), Lines(
                    F("전투당 한 번, 혼돈의 분노 중 처음 시전하는 {0}레벨 이하 워프리스트 주문 하나가 신속 행동으로 나간다(대상 무관, 주문 슬롯은 평소대로 쓴다).",
                      "Once per combat, the first warpriest spell of level {0} or lower you cast during this chaos rage is cast as a swift action (any target; it still uses its spell slot).", r.MaxSpellLevel),
                    new P("레벨은 주문 슬롯의 레벨이다(메타매직 비용 포함). 여러 형태 가운데 고르는 주문은 고르기 전 주문의 레벨로 본다.",
                          "The level is the level of the spell slot (metamagic cost included). A spell with variants counts at the level of the spell you choose the variant from."),
                    new P("워프리스트 주문 = 워프리스트 주문 목록에 든 주문이다. 다른 주문서로 시전해도 해당하고 이 효과를 쓴다. 원래 신속 행동인 주문, 신속 주문으로 준비한 주문에도 이 효과를 쓴다.",
                          "A warpriest spell is any spell on the warpriest spell list: cast from another spellbook it still counts and spends this effect. A spell that is already a swift action, or one prepared with Quicken Spell, also spends it."),
                    new P("그 라운드에 신속 행동을 이미 썼으면, 일반 행동이 남아 있을 때는 일반 행동으로 나가고 이 효과는 사라진다(일반 행동도 썼으면 신속 행동을 기다린다).",
                          "If you have already used your swift action this round and still have your standard action, it is cast as a standard action and this effect is still spent (if both are used, it waits for your swift action)."),
                    new P("분노를 다시 켜면 변이를 다시 뽑는다. 소모한 신속화는 다음 전투에서 돌아온다.", "Starting another rage rerolls mutations. A spent quicken becomes available in the next combat.")));
        }

        static T Throw<T>(string what) { throw new InvalidOperationException("ChaosWarrior: description: " + what); }

        static string Ordinal(int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException("n", n, "ChaosWarrior: description ordinal");
            if (n % 100 >= 11 && n % 100 <= 13) return n + "th";
            return n + (n % 10 == 1 ? "st" : n % 10 == 2 ? "nd" : n % 10 == 3 ? "rd" : "th");
        }

        // 자연 공격 변이: 공격 한 줄 · 상한 또는 「뽑힐 때마다」 · 보조 공격 · 변신 중 · (변이별 한 줄).
        static P NaturalText(string id, MutationSpecs.StackRow row, string dice, P secondary, P polyNat, int clawCap)
        {
            if (row.Redraw != MutationSpecs.RedrawRule.LimbCap || row.Mode != MutationSpecs.StackMode.Instances || row.LimbCategory == null)
                throw new InvalidOperationException("ChaosWarrior: description: natural attack mutation " + id + " is not an instance-mode limb-cap row");
            P noun;
            if (!AttackNounOverride.TryGetValue(id, out noun) && !AttackNoun.TryGetValue(row.LimbCategory, out noun)) throw new KeyNotFoundException("ChaosWarrior: description has no attack name for the limb category " + row.LimbCategory + " (" + id + ")");
            var gain = F("전력 공격에 {0} 자연 공격({1})이 하나 더해진다.", "Adds one more natural attack to a full attack: {0} ({1}).", noun, dice);
            int cap = Cap(row.LimbCategory);
            P stack;
            if (cap == Dl.LimbCapNone) stack = F("뽑힐 때마다 공격이 {0}회 더 늘어난다.", "{0} more attack each time it is drawn.", Dl.LimbsPerNaturalAttack);
            else if (row.LimbCategory == "Claw")
                stack = F("발톱과 집게는 합쳐 캐릭터당 {0}개까지라(종족·다른 효과 포함), 이미 찼으면 뽑히지 않는다.", "Claws and pincers share a limit of {0} per character (race and other effects count); never drawn while it is full.", clawCap);
            else stack = F("{0} 공격은 캐릭터당 {1}개까지라(종족·다른 효과 포함), 이미 찼으면 뽑히지 않는다.", "A character can have only {1} {0} attack (race and other effects count), so this is never drawn while it is full.", noun, cap);
            P extra = null;
            if (id == "Stinger") extra = new P("독은 없다.", "It carries no poison.");
            if (id == "Wings") extra = new P("날지는 못한다.", "It grants no flight.");
            if (id == "NeuroStinger") extra = F("이 침이 명중하면 고통이 {0}겹 더 쌓인다.", "A hit with this sting stacks {0} more pain.", Dl.PainNeuroStingExtra);
            return Lines(gain, stack, extra, secondary, polyNat);
        }

        // 자연 공격이 아닌 변이: 효과 줄 + 겹 줄(사양 행에서 — 규칙이 없으면 던진다).
        static P OtherText(Dictionary<string, (string ko, string en)> d, string id, MutationSpecs.StackRow row, int step, P once, P again)
        {
            Func<P> stackLine = () =>
            {
                if (row.Redraw == MutationSpecs.RedrawRule.OncePerRage) return once;
                if (row.Redraw != MutationSpecs.RedrawRule.Never) throw new InvalidOperationException("ChaosWarrior: description: " + id + " has redraw rule " + row.Redraw + " (no stacking sentence)");
                if (row.Mode == MutationSpecs.StackMode.Rank && MutationSpecs.RowsFor(id).All(r => r.Scale == MutationSpecs.RowScale.PerRank || r.Scale == MutationSpecs.RowScale.Core)) return again;
                throw new InvalidOperationException("ChaosWarrior: description: " + id + " needs its own stacking sentence (" + row.Mode + ")");
            };
            switch (id)
            {
                case "Enlarge":
                    return Lines(F("크기가 {0}단계 커지고 힘 {1}(크기 보너스).", "Size increases by {0} step; {1} Strength (size bonus).", Fixed(id, "ChangeUnitSize"), Signed(Fixed(id, "AddStatBonus", "Strength"))),
                        F("다시 뽑히면 크기는 그대로이고 힘만 {0}씩 더해진다.", "Drawn again: size stays, {0} Strength more each time.", Signed(Fixed(id, "AddStatBonus", "Strength"))));
                case "ToughHide":
                    return Lines(F("피해 감소(DR) {0}/—, 워프리스트 {1}레벨마다 {2}.", "Damage reduction {0}/—, {2} per {1} warpriest levels.", Dl.ToughHideBase, step, Signed(1)),
                        new P("다른 피해 감소와는 큰 것 하나만 적용된다.", "Only the highest of it and other damage reduction applies."), stackLine());
                case "Regeneration":
                    if (row.Mode != MutationSpecs.StackMode.Instances) throw new InvalidOperationException("ChaosWarrior: description: Regeneration is expected to be instance mode");
                    return Lines(F("빠른 치유 {0}, 워프리스트 {1}레벨마다 {2}.", "Fast healing {0}, {2} per {1} warpriest levels.", Dl.RegenBase, step, Signed(1)),
                        new P("뽑힐 때마다 빠른 치유 효과가 하나 더 붙는다(효과 아이콘도 따로 보인다).", "Each time it is drawn another fast healing effect is added (it shows as another effect icon)."));
                case "Swiftness":
                    return Lines(F("속도 {0}피트, 반사 내성 {1}.", "{0} ft speed, {1} Reflex saves.", Signed(Fixed(id, "AddStatBonus", "Speed")), Signed(Fixed(id, "AddStatBonus", "SaveReflex"))), stackLine());
                case "ThornSkin":
                    return Lines(F("근접 공격으로 당신을 맞힌 공격자가 찌르기 피해({0})를 입는다.", "Melee attackers that hit you take {0} piercing damage.", Dice(Dl.ThornsDiceCount, Dl.ThornsDiceSides)),
                        F("뽑힐 때마다 주사위가 {0}개 더 늘어난다.", "{0} more die each time it is drawn.", Dl.ThornsDiceCount));
                case "ChaosBlood":
                    return Lines(F("모든 내성 굴림 {0}.", "{0} on all saving throws.", Signed(Fixed(id, "BuffAllSavesBonus"))), stackLine());
                case "ElementalShell":
                    return Lines(F("무작위 원소({0}) 저항 {1}.", "Resistance {1} to a random element ({0}).", new P(string.Join("·", Dl.ShellElements.Select(e => Element(e).Ko)), string.Join(", ", Dl.ShellElements.Select(e => Element(e).En))), Dl.ElementResist),
                        F("뽑힐 때마다 원소를 새로 정하고, 같은 원소면 저항이 {0}씩 더해진다.", "Each time it is drawn the element is rolled again; the same element adds {0} more resistance.", Dl.ElementResist));
                case "PainAmplify":
                    return Lines(F("명중 시 고통이 {0}겹 더 쌓인다.", "Hits stack {0} more pain.", Dl.PainAmplifyExtra), F("겹칠 때마다 {0}겹씩 더 쌓인다.", "{0} more for each time it is drawn.", Dl.PainAmplifyExtra));
                case "HorrificForm":
                    return Lines(F("{0}피트 안의 적이 내성 굴림 없이 동요한다.", "Enemies within {0} ft are shaken without a saving throw.", Fixed(id, "AddAreaEffect")), stackLine());
                case "LongLimbs":
                    return Lines(F("근접 사거리 {0}피트.", "{0} ft melee reach.", Signed(Fixed(id, "AddStatBonus", "Reach"))), stackLine());
                case "TwistedLegs":
                    return Lines(F("속도 {0}피트.", "{0} ft speed.", Signed(Fixed(id, "AddStatBonus", "Speed"))), stackLine());
                case "BlurredEyes":
                    return Lines(F("명중 굴림 {0}.", "{0} on attack rolls.", Signed(Fixed(id, "AddStatBonus", "AdditionalAttackBonus"))), stackLine());
                case "ExposedNerves":
                    return Lines(F("AC {0}.", "{0} AC.", Signed(Fixed(id, "AddStatBonus", "AC"))), stackLine());
                case "BurstingVeins":
                    return Lines(F("매 라운드 피해 {0}을 입는다.", "Take {0} damage every round.", Dl.BurstingVeinsDamage), F("뽑힐 때마다 피해가 {0} 더 늘어난다.", "{0} more damage each time it is drawn.", Dl.BurstingVeinsDamage));
                case "SeethingMind":
                    return Lines(F("의지 내성 {0}.", "{0} on Will saves.", Signed(Fixed(id, "AddStatBonus", "SaveWill"))), stackLine());
                case "Bloodthirst":
                    return Lines(F("근접 피해 {0}(자연 무기 포함), AC {1}.", "{0} melee damage (natural weapons too), {1} AC.", Signed(Fixed(id, "WeaponAttackTypeDamageBonus") * Dl.WeaponDamageBonusMultiplier), Signed(Fixed(id, "AddStatBonus", "AC"))), stackLine());
                case "BloatedMuscle":
                    return Lines(F("힘 {0}, 민첩 {1}.", "{0} Strength, {1} Dexterity.", Signed(Fixed(id, "AddStatBonus", "Strength")), Signed(Fixed(id, "AddStatBonus", "Dexterity"))), stackLine());
                case "StoneShell":
                    return Lines(F("자연 방어 {0}, 속도 {1}피트.", "{0} natural armor, {1} ft speed.", Signed(Fixed(id, "AddStatBonus", "AC")), Signed(Fixed(id, "AddStatBonus", "Speed"))), stackLine());
                case "RawNerves":
                    return Lines(F("반사 내성 {0}, 피하기 AC {1}, 의지 내성 {2}.", "{0} Reflex saves, {1} dodge AC, {2} Will saves.", Signed(Fixed(id, "AddStatBonus", "SaveReflex")), Signed(Fixed(id, "AddStatBonus", "AC")), Signed(Fixed(id, "AddStatBonus", "SaveWill"))), stackLine());
                case "PainResonance":
                    return Lines(F("명중 시 고통이 {0}겹 더 쌓이고, 자신의 모든 내성 굴림 {1}.", "Hits stack {0} more pain; {1} on all your saving throws.", Dl.PainResonanceExtra, Signed(Fixed(id, "BuffAllSavesBonus"))), stackLine());
                default:
                    throw new InvalidOperationException("ChaosWarrior: description has no text rule for the mutation " + id + " (" + NameOf(d, "mut." + id).En + ")");
            }
        }
    }
}
