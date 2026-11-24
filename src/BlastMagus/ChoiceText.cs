using System;
using System.Collections.Generic;
using System.Linq;

namespace BlastMagus
{
    // 설명문에 옮겨 적는 값 가운데 BlastRules 에 없는 것(C3). 게임 형식 없음(콘솔 시험이 컴파일한다 — modinfo testSources).
    // 시험 `choice text: facts` 가 출처(이 모드의 dll·소스, blueprints.zip, 엔진 IL)와 하나씩 대조한다.
    public static class BlastFacts
    {
        // 블라스트 피해 주사위의 면 수. 폭발 = BlastStrike.cs `new DiceFormula(plan.Dice, DiceType.D6)`, 에너지 주입 = Energize.cs `new DiceFormula(dice, DiceType.D6)`,
        // 본편 단순 블라스트 6종(*BlastAbility.jbp) = ContextActionDealDamage.Value.DiceType D6.
        public const int Die = 6;
        // 메이거스 레벨 범위. 상한 = BlastBlueprints.MaxLevel(MagusProgression 의 최고 레벨 항목, 빌드가 되읽는다).
        public const int MinLevel = 1;
        public const int MaxLevel = 20;
        // 본편 단순 블라스트 6종(*BlastAbility.jbp)의 Range = Close → 엔진 BlueprintAbility.GetRange IL_0065: 30피트 고정. ActionType = Standard.
        public const int VanillaBlastRangeFeet = 30;
        // 본편 복합 블라스트 잎(*BlastAbility.jbp, 20레벨에 주는 복합 특성의 능력)의 AbilityKineticist.BlastBurnCost. 빌드는 BlastContract 계약 8 로 같은 값을
        //  BlastRules.CompositeBurnReduction 과 대조하고, 시험 `choice text: facts: composite burn` 이 blueprints.zip 의 본편 복합 잎과 대조한다.
        //  실제 비용 = 이 값 − CompositeBurnReduction(Composite20 의 AddKineticistBurnModifier, BlastBlueprints.BurnModifier).
        public const int CompositeBlastBurnCost = 2;
    }

    // [순수] 원소 선택 화면과 원소 선택지 11개, 그리고 수치가 빠져 있던 자동 획득 특성 3개(블라스트 랭크 · 원소 블라스트 · 폭발)의 설명문.
    // 레벨업 선택 화면의 상세 창은 BlueprintUnitFact.m_Description 전체를 읽는다(엔진 CharGenFeatureSelectorItemVM.TooltipTemplate → TooltipTemplateLevelUpFeature.Prepare
    //  → BlueprintUnitFact.get_Description). 줄바꿈 = "\n".
    // 수치 잠금(C4): 아래 문구 틀(리터럴)에는 숫자를 쓰지 않는다 — 수치는 전부 {n} 자리에 BlastRules(이 모드의 규칙)와 BlastFacts 를 끼워 넣고, 원소별 내용은 ElementTable 행에서 나온다.
    //  시험 `choice text` 가 (1) 이 형식의 문자열 리터럴에 {n} 밖의 숫자가 없음 (2) 만들어진 문구의 수치 == 손 전사 기대값 (3) ko/en 줄 수·수치 집합 일치
    //  (4) 원소 설명이 서로 다름 (5) 종류별 문구 == 원소 표의 종류·SR·투사체 (6) 본편 블라스트 값 == blueprints.zip 을 잠근다.
    // KEE 원소 5종의 문구도 항상 만들어진다(KineticistElementsExpanded 가 꺼져 있어도 키는 있다 — 2단계가 그 키로 특성을 만든다, BlastBlueprints.CheckKeeCandidates).
    //  KEE 블라스트 능력은 그 모드가 코드로 만들어 blueprints.zip 에 없으므로, 원거리 블라스트의 사거리·피해 식은 KEE 원소 문구에 적지 않는다(확인한 것만 적는다).
    // 용어: 원소·복합 원소 이름과 규칙 용어(일반 행동 · 원거리 접촉 공격 · 주문 저항 · 피해 감소 · 부수기/찌르기/베기 · 치명타)는 한글 패치 표기(WrathKoreanMod, 2026-11-17 대조).
    //  「블라스트」「아케인 풀」「에너지 주입」「폭발」은 이 모드의 이름이라 그대로 둔다(한글 패치의 블래스트 · 비전력).
    // 틀에 넘긴 인자를 틀이 쓰지 않으면 던진다(조용히 빠진 수치 없음).
    static class ChoiceText
    {
        // 한/영 한 쌍. 인자로 넘기면 그 언어 쪽이 들어간다.
        sealed class P
        {
            public readonly string Ko, En;
            public P(string ko, string en)
            {
                if (string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en)) throw new ArgumentException("BlastMagus: choice text pair with an empty side");
                Ko = ko; En = en;
            }
        }

        static P F(string ko, string en, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args", "BlastMagus: choice text argument " + i + " is null (" + en + ")");
                string slot = "{" + i + "}";
                if (!ko.Contains(slot) || !en.Contains(slot)) throw new ArgumentException("BlastMagus: choice text template does not use argument " + i + " in both languages (" + en + ")");
            }
            return new P(string.Format(ko, args.Select(a => a is P ? (object)((P)a).Ko : a).ToArray()), string.Format(en, args.Select(a => a is P ? (object)((P)a).En : a).ToArray()));
        }

        // 줄 묶음(null = 그 줄 없음). 한 줄도 없으면 던진다.
        static P Lines(params P[] lines)
        {
            var l = lines.Where(x => x != null).ToArray();
            if (l.Length == 0) throw new ArgumentException("BlastMagus: choice text without lines");
            return new P(string.Join("\n", l.Select(x => x.Ko)), string.Join("\n", l.Select(x => x.En)));
        }

        // 「a~b레벨 값, c레벨부터 값」(같은 값이 이어지는 레벨 구간). 구간이 하나면 던진다 — 구간 표기를 쓸 이유가 없는 값이다.
        static P Ranges(Func<int, string> at)
        {
            int min = BlastFacts.MinLevel, max = BlastFacts.MaxLevel;
            var segs = new List<Tuple<int, int, string>>();
            int start = min;
            string cur = at(min);
            for (int l = min + 1; l <= max + 1; l++)
            {
                string v = l <= max ? at(l) : null;
                if (v == cur) continue;
                if (string.IsNullOrEmpty(cur)) throw new InvalidOperationException("BlastMagus: choice text value for level " + start + " is empty");
                segs.Add(Tuple.Create(start, l - 1, cur));
                start = l; cur = v;
            }
            if (segs.Count < 2) throw new InvalidOperationException("BlastMagus: choice text level ranges: the value never changes");
            return new P(
                string.Join(", ", segs.Select(s => (s.Item1 == s.Item2 ? s.Item1 + "레벨 " : s.Item2 == max ? s.Item1 + "레벨부터 " : s.Item1 + "~" + s.Item2 + "레벨 ") + s.Item3)),
                string.Join(", ", segs.Select(s => s.Item3 + (s.Item1 == s.Item2 ? " at level " + s.Item1 : s.Item2 == max ? " from level " + s.Item1 : " at levels " + s.Item1 + "-" + s.Item2))));
        }

        static string Dice(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException("count", count, "BlastMagus: choice text dice count must be positive");
            return count + "d" + BlastFacts.Die;
        }

        static P NameOf(Dictionary<string, (string ko, string en)> d, string nameKey)
        {
            (string ko, string en) v;
            if (!d.TryGetValue(nameKey, out v)) throw new KeyNotFoundException("BlastMagus: choice text needs the name " + nameKey);
            return new P(v.ko, v.en);
        }

        static void Put(Dictionary<string, (string ko, string en)> d, string key, P text) { d.Add(key, (text.Ko, text.En)); }

        // 에너지 피해 이름(행의 EnergyName = 엔진 DamageEnergyType 이름). 표에 없는 이름이면 던진다.
        static P EnergyWord(string energyName)
        {
            switch (energyName)
            {
                case "Fire": return new P("화염", "fire");
                case "Cold": return new P("냉기", "cold");
                case "Electricity": return new P("전기", "electricity");
                case "NegativeEnergy": return new P("음에너지", "negative energy");
                case "PositiveEnergy": return new P("양에너지", "positive energy");
                default: throw new ArgumentOutOfRangeException("energyName", energyName, "BlastMagus: choice text has no word for this energy type");
            }
        }

        // 물리 피해 형태(행의 PhysicalForm = 엔진 PhysicalDamageForm 플래그 이름, ", " 구분).
        static P FormWord(string physicalForm)
        {
            switch (physicalForm)
            {
                case "Bludgeoning": return new P("부수기", "bludgeoning");
                case "Bludgeoning, Piercing, Slashing": return new P("부수기·찌르기·베기", "bludgeoning, piercing and slashing");
                default: throw new ArgumentOutOfRangeException("physicalForm", physicalForm, "BlastMagus: choice text has no word for this physical damage form");
            }
        }

        // 복합 원소 이름(행의 CompositeName = 본편·KEE 특성 이름 접두).
        static P CompositeWord(string compositeName)
        {
            switch (compositeName)
            {
                case "BlueFlame": return new P("푸른 불꽃", "blue flame");
                case "Ice": return new P("얼음", "ice");
                case "Thunderstorm": return new P("번개폭풍", "thunderstorm");
                case "Metal": return new P("금속", "metal");
                case "Force": return new P("역장", "force");
                case "Void": return new P("공허", "void");
                case "Verdant": return new P("신록", "verdant");
                default: throw new ArgumentOutOfRangeException("compositeName", compositeName, "BlastMagus: choice text has no word for this composite element");
            }
        }

        // 에너지 원소의 원거리 블라스트 = 접촉 공격(투사체 무기 형식 KineticBlastEnergy 의 m_AttackType RangedTouch), 물리 원소 = 일반 원거리 공격(KineticBlastPhysical, Ranged).
        static bool IsTouch(ElementRow r)
        {
            if (r.ProjectileWeaponTypeGuid == Guids.Game.KineticBlastEnergy) return true;
            if (r.ProjectileWeaponTypeGuid == Guids.Game.KineticBlastPhysical) return false;
            throw new InvalidOperationException("BlastMagus: choice text: element " + r.Id + " has an unknown projectile weapon type " + r.ProjectileWeaponTypeGuid);
        }

        // 종류와 표의 다른 칸이 서로 맞는 행만 문구로 옮긴다(에너지 = 접촉·SR, 물리 = 일반 원거리·SR 없음). 어긋난 행은 문구가 사실과 달라지므로 던진다.
        static void RequireConsistent(ElementRow r)
        {
            bool energy = r.Kind == DamageKind.Energy;
            if (!energy && r.Kind != DamageKind.Physical) throw new ArgumentOutOfRangeException("r", r.Kind, "BlastMagus: choice text: element " + r.Id + " damage kind undefined");
            if (IsTouch(r) != energy || r.SpellResistance != energy)
                throw new InvalidOperationException("BlastMagus: choice text: element " + r.Id + " is " + r.Kind + " but touch = " + IsTouch(r) + ", spell resistance = " + r.SpellResistance + " (the wording assumes energy = touch + SR, physical = neither)");
        }

        // 블라스트 랭크 성장을 한 문장으로 적기 위한 값: 시작 레벨의 랭크, 오르는 폭, 마지막으로 오르는 레벨, 최고 랭크. 「홀수 레벨마다」가 규칙과 다르면 던진다.
        sealed class RankShape { public int FirstLevel, FirstRank, Step, LastLevel, TopRank; }

        static RankShape Rank()
        {
            int min = BlastFacts.MinLevel, max = BlastFacts.MaxLevel;
            var s = new RankShape { FirstLevel = min, FirstRank = BlastRules.BlastRank(min), TopRank = BlastRules.BlastRank(max) };
            if (!BlastRules.RankLevel(min)) throw new InvalidOperationException("BlastMagus: choice text: level " + min + " is not a rank level");
            s.Step = -1;
            for (int l = min + 1; l <= max; l++)
            {
                int up = BlastRules.BlastRank(l) - BlastRules.BlastRank(l - 1);
                bool odd = l % 2 != 0;
                if ((up != 0) != BlastRules.RankLevel(l) || (up != 0 && !odd)) throw new InvalidOperationException("BlastMagus: choice text: blast rank does not rise exactly on the odd rank levels (level " + l + ")");
                if (up == 0) continue;
                if (s.Step != -1 && s.Step != up) throw new InvalidOperationException("BlastMagus: choice text: blast rank rises by different amounts");
                s.Step = up;
                s.LastLevel = l;
            }
            if (s.Step <= 0 || s.LastLevel <= min) throw new InvalidOperationException("BlastMagus: choice text: blast rank never rises");
            // 「홀수 레벨마다」: 시작 레벨과 마지막으로 오르는 레벨 사이의 홀수 레벨이 전부 랭크 레벨이어야 한다.
            for (int l = min; l <= s.LastLevel; l++)
                if (l % 2 != 0 && !BlastRules.RankLevel(l)) throw new InvalidOperationException("BlastMagus: choice text: odd level " + l + " is not a rank level");
            return s;
        }

        static P RankLine()
        {
            var s = Rank();
            return F("블라스트 랭크: {0}레벨에 {1}, 홀수 레벨마다 {2}씩 올라 {3}레벨에 {4}.", "Blast rank: {1} at level {0}, +{2} on every odd level, {4} at level {3}.",
                s.FirstLevel, s.FirstRank, s.Step, s.LastLevel, s.TopRank);
        }

        // 폭발 주사위의 보기: 해금 레벨, 랭크가 마지막으로 오르는 레벨, 복합 원소 레벨.
        static P StrikeDiceLine()
        {
            var s = Rank();
            int open = BlastRules.BlastStrikeLevel, comp = BlastRules.CompositeLevel;
            return F("주사위: 블라스트 랭크당 {0}({1}레벨 {2}, {3}레벨 {4}). {5}레벨에 복합 원소를 얻으면 {6}배({7}).",
                "Dice: {0} per blast rank ({2} at level {1}, {4} at level {3}). With the composite element at level {5} they are multiplied by {6} ({7}).",
                Dice(1), open, Dice(BlastRules.StrikeDice(BlastRules.BlastRank(open), false)), s.LastLevel, Dice(BlastRules.StrikeDice(BlastRules.BlastRank(s.LastLevel), false)),
                comp, BlastRules.CompositeDiceMultiplier, Dice(BlastRules.StrikeDice(BlastRules.BlastRank(comp), true)));
        }

        static P CritLine()
        {
            return F("무기 공격이 치명타로 확정되면 폭발 피해도 {0}배가 된다.", "A confirmed critical hit of the weapon attack multiplies the Blast Strike damage by {0}.", BlastRules.BlastStrikeCritMultiplier);
        }

        static P EnergizeRanges() { return Ranges(l => Dice(BlastRules.EnergizeDice(l))); }

        internal static void AddTo(Dictionary<string, (string ko, string en)> d)
        {
            if (d == null) throw new ArgumentNullException("d");
            var rows = ElementTable.All;
            foreach (var r in rows) RequireConsistent(r);
            foreach (var r in rows) Put(d, r.DescriptionKey, ElementText(d, r));
            Put(d, "ElementSelection.Description", Selection(d, rows));
            Put(d, "BlastRank.Description", BlastRank());
            Put(d, "Blast.Description", Blast(d, rows));
            Put(d, "BlastStrike.Description", BlastStrike());
        }

        // ---- 원소 선택지 ----
        static P ElementText(Dictionary<string, (string ko, string en)> d, ElementRow r)
        {
            bool energy = r.Kind == DamageKind.Energy, kee = r.Source == ElementSource.Kee;
            var name = NameOf(d, r.NameKey);
            var comp = CompositeWord(r.CompositeName);
            P first, ranged, strike;
            if (energy)
            {
                var dmg = EnergyWord(r.EnergyName);
                first = F("{0} 원소(에너지): 원거리 블라스트, 에너지 주입, 폭발이 모두 {1} 피해를 입힌다.", "{0} element (energy): ranged blast, Energize Weapon and Blast Strike all deal {1} damage.", name, dmg);
                ranged = kee
                    ? new P("원거리 블라스트(KineticistElementsExpanded 모드의 능력 — 사거리와 피해 식은 그 모드가 정한다): 원거리 접촉 공격, 주문 저항을 받는다.",
                        "Ranged blast (from the KineticistElementsExpanded mod, which sets its range and damage): ranged touch attack, spell resistance applies.")
                    : F("원거리 블라스트: 일반 행동, 사거리 {0}피트, 원거리 접촉 공격, 주문 저항을 받는다. 피해는 블라스트 랭크당 {1} + 지능 수정치의 절반.",
                        "Ranged blast: standard action, {0} feet, ranged touch attack, spell resistance applies. Damage: {1} per blast rank + half your Intelligence modifier.",
                        BlastFacts.VanillaBlastRangeFeet, Dice(1));
                strike = F("폭발({0}레벨부터, 신속 행동, 아케인 풀 {1}점): 다음 근접 제작 무기 명중에 블라스트 랭크당 {2} + 지능 수정치의 절반을 싣는다({3}레벨 {4}). 주문 저항을 받는다.",
                    "Blast Strike (level {0}+, swift action, {1} arcane pool point): your next melee manufactured-weapon hit also deals {2} per blast rank + half your Intelligence modifier ({4} at level {3}); spell resistance applies.",
                    BlastRules.BlastStrikeLevel, BlastRules.BlastStrikeCost, Dice(1), Rank().LastLevel, Dice(BlastRules.StrikeDice(BlastRules.BlastRank(Rank().LastLevel), false)));
            }
            else
            {
                var forms = FormWord(r.PhysicalForm);
                first = F("{0} 원소(물리): 원거리 블라스트, 에너지 주입, 폭발이 모두 물리 피해({1})를 입힌다.", "{0} element (physical): ranged blast, Energize Weapon and Blast Strike all deal physical damage ({1}).", name, forms);
                ranged = kee
                    ? new P("원거리 블라스트(KineticistElementsExpanded 모드의 능력 — 사거리와 피해 식은 그 모드가 정한다): 접촉이 아닌 원거리 공격, 주문 저항을 받지 않는다.",
                        "Ranged blast (from the KineticistElementsExpanded mod, which sets its range and damage): ranged attack (not touch), no spell resistance.")
                    : F("원거리 블라스트: 일반 행동, 사거리 {0}피트, 접촉이 아닌 원거리 공격, 주문 저항을 받지 않는다. 피해는 블라스트 랭크당 {1} + 블라스트 랭크 + 지능 수정치.",
                        "Ranged blast: standard action, {0} feet, ranged attack (not touch), no spell resistance. Damage: {1} per blast rank + blast rank + Intelligence modifier.",
                        BlastFacts.VanillaBlastRangeFeet, Dice(1));
                strike = F("폭발({0}레벨부터, 신속 행동, 아케인 풀 {1}점): 다음 근접 제작 무기 명중에 블라스트 랭크당 {2} + 주사위 수 + 지능 수정치를 싣는다({3}레벨 {4}). 주문 저항을 받지 않고, 피해 감소에는 마법 피해로 취급된다.",
                    "Blast Strike (level {0}+, swift action, {1} arcane pool point): your next melee manufactured-weapon hit also deals {2} per blast rank + the number of dice plus your Intelligence modifier ({4} at level {3}); no spell resistance, and it counts as magic against damage reduction.",
                    BlastRules.BlastStrikeLevel, BlastRules.BlastStrikeCost, Dice(1), Rank().LastLevel, Dice(BlastRules.StrikeDice(BlastRules.BlastRank(Rank().LastLevel), false)));
            }
            P warning = null;
            if (r.Id == BlastMagus.Element.Negative)
                warning = new P("주의: 언데드에게는 효과가 없거나 오히려 회복시킬 수 있다(엔진 처리 그대로).", "Warning: undead may be unaffected or even healed (the engine's own handling).");
            else if (r.Id == BlastMagus.Element.Positive)
                warning = new P("주의: 살아 있는 생물에게는 효과가 없거나 오히려 회복시킬 수 있다(엔진 처리 그대로).", "Warning: living creatures may be unaffected or even healed (the engine's own handling).");
            return Lines(
                first,
                ranged,
                RankLine(),
                strike,
                F("에너지 주입(토글, 비용 없음): 근접 제작 무기의 모든 공격에 같은 피해를 더한다 — {0}.", "Energize Weapon (toggle, no cost): each melee manufactured-weapon attack also deals this damage: {0}.", EnergizeRanges()),
                F("{0}레벨 복합 원소: {1}. 이때부터 폭발 주사위가 {2}배({3})가 되고, 에너지 주입은 늘지 않는다.",
                    "Level {0} composite element: {1}. Blast Strike then rolls {2} times the dice ({3}); Energize Weapon does not grow.",
                    BlastRules.CompositeLevel, comp, BlastRules.CompositeDiceMultiplier, Dice(BlastRules.StrikeDice(BlastRules.BlastRank(BlastRules.CompositeLevel), true))),
                warning,
                kee ? new P("그 모드가 켜져 있을 때만 선택지에 나온다.", "Offered only while that mod is enabled.") : null);
        }

        static P NameList(Dictionary<string, (string ko, string en)> d, IEnumerable<ElementRow> rows)
        {
            var l = rows.Select(r => NameOf(d, r.NameKey)).ToArray();
            if (l.Length == 0) throw new InvalidOperationException("BlastMagus: choice text: an element group is empty");
            return new P(string.Join("·", l.Select(x => x.Ko)), string.Join(", ", l.Select(x => x.En)));
        }

        // ---- 원소 선택 화면 ----
        static P Selection(Dictionary<string, (string ko, string en)> d, ElementRow[] rows)
        {
            var s = Rank();
            int[] growth = BlastRules.EnergizeGrowthLevels;
            if (growth.Length == 0) throw new InvalidOperationException("BlastMagus: choice text: no energize growth levels");
            Func<ElementSource, DamageKind, IEnumerable<ElementRow>> group = (src, kind) => rows.Where(r => r.Source == src && r.Kind == kind);
            return Lines(
                new P("메이거스 첫 레벨에 블라스트의 원소를 하나 고른다. 한번 고르면 바꿀 수 없다.", "At your first magus level you choose the element of your blasts. It cannot be changed later."),
                F("에너지 원소({0}): 원거리 블라스트가 원거리 접촉 공격이고, 폭발의 피해 보너스는 지능 수정치의 절반이며, 둘 다 주문 저항을 받는다.",
                    "Energy elements ({0}): ranged touch attack; Blast Strike damage bonus = half your Intelligence modifier; spell resistance applies to both.",
                    NameList(d, group(ElementSource.Vanilla, DamageKind.Energy))),
                F("물리 원소({0}): 원거리 블라스트가 접촉이 아닌 원거리 공격이고, 폭발의 피해 보너스는 주사위 수 + 지능 수정치이며, 주문 저항을 받지 않고 피해 감소에는 마법 피해로 취급된다.",
                    "Physical elements ({0}): ranged attack (not touch); Blast Strike damage bonus = the number of dice plus your Intelligence modifier; no spell resistance, and the damage counts as magic against damage reduction.",
                    NameList(d, group(ElementSource.Vanilla, DamageKind.Physical))),
                F("KineticistElementsExpanded 모드가 켜져 있으면 {0}(물리)와 {1}(에너지)도 고를 수 있다.",
                    "With the KineticistElementsExpanded mod enabled you can also choose {0} (physical) and {1} (energy).",
                    NameList(d, group(ElementSource.Kee, DamageKind.Physical)), NameList(d, group(ElementSource.Kee, DamageKind.Energy))),
                F("원소와 상관없이 같은 것: 블라스트 랭크({0}레벨 {1}, {2}레벨 {3}), 폭발({4}레벨부터, 아케인 풀 {5}점 — 한글 패치의 비전력), 에너지 주입({6}, {7}레벨에 {8}씩 늘어 {9}).",
                    "The same for every element: blast rank ({1} at level {0}, {3} at level {2}), Blast Strike (from level {4}, {5} arcane pool point), Energize Weapon ({6}, gaining {8} at levels {7}, up to {9}).",
                    s.FirstLevel, s.FirstRank, s.LastLevel, s.TopRank, BlastRules.BlastStrikeLevel, BlastRules.BlastStrikeCost,
                    Dice(BlastRules.EnergizeDice(BlastFacts.MinLevel)), new P(string.Join("·", growth), string.Join(", ", growth)),
                    Dice(BlastRules.EnergizeDice(growth[0]) - BlastRules.EnergizeDice(growth[0] - 1)), Dice(BlastRules.EnergizeDice(BlastFacts.MaxLevel))),
                F("이 선택이 원거리 블라스트·에너지 주입·폭발의 피해 유형과 {0}레벨 복합 원소를 정한다.",
                    "The choice sets the damage type of your ranged blast, Energize Weapon and Blast Strike, and your level {0} composite element.", BlastRules.CompositeLevel));
        }

        // ---- 자동 획득 특성(수치가 빠져 있던 것) ----
        static P BlastRank()
        {
            var s = Rank();
            return Lines(
                F("블라스트 랭크가 {0} 오른다.", "Your blast rank rises by {0}.", s.Step),
                F("{0}레벨에 {1}로 시작해 홀수 레벨마다 {2}씩 올라 {3}레벨에 {4}이 된다.", "It starts at {1} at level {0} and rises by {2} on every odd level, reaching {4} at level {3}.",
                    s.FirstLevel, s.FirstRank, s.Step, s.LastLevel, s.TopRank),
                F("랭크마다 폭발의 피해 주사위가 {0}씩 늘어난다(본편 원소의 원거리 블라스트도 같다).", "Each rank adds {0} to your Blast Strike damage dice (and to the ranged blast of the base-game elements).", Dice(s.Step)));
        }

        static P Blast(Dictionary<string, (string ko, string en)> d, ElementRow[] rows)
        {
            // 20레벨 복합 블라스트의 실제 번 비용(BlastBlueprints.BuildComposite: 원소 Base 가 있으면 복합 특성 + 번 감소). 「역시 비용이 없다」는 0 일 때만 참이다.
            int compositeBurn = BlastFacts.CompositeBlastBurnCost - BlastRules.CompositeBurnReduction;
            if (compositeBurn != 0) throw new InvalidOperationException("BlastMagus: choice text says the level " + BlastRules.CompositeLevel + " composite blast costs no Burn, but its cost is " + compositeBurn);
            return Lines(
                new P("선택한 원소의 블라스트를 쏜다. 에너지 원소는 원거리 접촉 공격, 물리 원소는 원거리 공격이다.",
                    "You fire a blast of your chosen element: a ranged touch attack for energy elements, a ranged attack for physical elements."),
                F("본편 원소({0})의 블라스트: 일반 행동, 사거리 {1}피트. 피해는 블라스트 랭크당 {2}에 에너지 원소는 지능 수정치의 절반, 물리 원소는 블라스트 랭크 + 지능 수정치를 더한다.",
                    "Blasts of the base-game elements ({0}): standard action, range {1} feet. Damage is {2} per blast rank, plus half your Intelligence modifier for energy elements or your blast rank + your Intelligence modifier for physical elements.",
                    NameList(d, rows.Where(r => r.Source == ElementSource.Vanilla)), BlastFacts.VanillaBlastRangeFeet, Dice(1)),
                F("번(Burn)이 없어 처음에는 비용 없이 쓸 수 있는 단순 블라스트만 쓴다. {0}레벨부터는 선택한 원소의 복합 블라스트(원소 선택지에 적힌 복합 원소)도 쓴다 — 원래 번 비용 {1}점이 {2}점 줄어 {3}점이라 역시 비용이 없다.",
                    "You have no Burn, so at first you use only simple blasts, at no cost. From level {0} you also use the composite blast of your chosen element (the composite element named in its choice): its original Burn cost of {1} is reduced by {2} to {3}, so it is free as well.",
                    BlastRules.CompositeLevel, BlastFacts.CompositeBlastBurnCost, BlastRules.CompositeBurnReduction, compositeBurn),
                new P("명중은 민첩·지능 중 높은 쪽을, 피해 보너스는 지능을 따른다.", "Attack uses the higher of Dexterity and Intelligence; the damage bonus follows Intelligence."));
        }

        static P BlastStrike()
        {
            return Lines(
                F("신속 행동으로 아케인 풀 {0}점을 쓴다. 이 라운드에 성공시키는 다음 근접 제작 무기 명중 한 번에 블라스트 피해 전체(주사위와 블라스트 피해 보너스)가 실린다.",
                    "Swift action, spending {0} arcane pool point. The next melee manufactured-weapon hit you land this round carries the full blast damage (its dice and its blast damage bonus).", BlastRules.BlastStrikeCost),
                StrikeDiceLine(),
                new P("에너지 원소의 폭발은 피해 보너스가 지능 수정치의 절반이고 주문 저항을 받는다.", "For energy elements the damage bonus is half your Intelligence modifier and spell resistance applies."),
                new P("물리 원소의 폭발은 피해 보너스가 주사위 수 + 지능 수정치이고, 주문 저항을 받지 않으며, 피해 감소에는 마법 피해로 취급된다(DR/마법 통과).",
                    "For physical elements the damage bonus is the number of dice plus your Intelligence modifier, spell resistance does not apply, and the damage counts as magic against damage reduction."),
                CritLine(),
                new P("주문 타격의 접촉 주문도 같은 명중에 함께 실을 수 있고, 명중하지 못하면 라운드가 끝날 때 사라진다.",
                    "A spell strike touch spell can ride the same hit, and the effect ends at the end of the round if you do not hit."));
        }
    }
}
