using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
         
        // T1 은 아키타입·중장 숙련 두 줄만 쓴다. 나머지 키(한손 지팡이·반격·유대 방패·철벽 집중)는 그 기능의 Task 가 더한다(아키타입 설명은 설계 전체를 적는다).
        // 선택 화면 설명문은 리터럴이 아니다 — 아래 ChoiceText 가 규칙 상수(BulwarkRules · RebukeTable · BodyTable)에서 만들어 넣는다(C4: 문구 속 수치 = 규칙 상수):
        //   "archetype.desc"(직업 선택 화면의 아키타입 설명) · "rebuke.selection.desc"(방패 반격 배우기 화면) · "e1.choice.desc" ~ "e7.choice.desc"(그 화면의 선택지 = Learn 특성).
        //   "e?.desc"(짧은 한 줄)는 토글·토글 버프 툴팁용으로 그대로 둔다 — 선택지용 긴 설명이 툴팁까지 길게 만들지 않게 키를 갈랐다(2026-11-17).
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();

        // 방패 반격 배우기 화면(BulwarkRebukeSelection)의 설명 키.
        public const string SelectionDescKey = "rebuke.selection.desc";

        // 선택지(Learn 특성) 설명 키. 표에 없는 효과면 던진다(RebukeTable.Get).
        public static string ChoiceKey(string effectId) { return RebukeTable.Get(effectId).Id.ToLowerInvariant() + ".choice.desc"; }

        static Dictionary<string, (string ko, string en)> Build()
        {
            var d = Literals();
            ChoiceText.AddTo(d);
            return d;
        }

        static Dictionary<string, (string ko, string en)> Literals()
        {
            return new Dictionary<string, (string ko, string en)>
        {
            ["archetype.name"] = ("중장법사", "Bulwark Mage"),
            // 아키타입의 짧은 설명(LocalizedDescriptionShort). 긴 설명 "archetype.desc" 는 ChoiceText 가 만든다.
            ["archetype.short"] = (
                "타워실드와 중갑을 두르고 9단계 주문을 그대로 쓰는 위저드. 근접 공격자에게 방패가 되갚는다. 핵심 능력치는 지능이다.",
                "A wizard who casts in full plate behind a tower shield; the shield answers melee attackers. Intelligence governs your spells and daily shield rebukes."),
            ["prof.name"] = ("중장 숙련", "Bulwark Proficiency"),
            ["prof.desc"] = (
                "경·중·중갑 갑옷과 모든 방패(타워실드 포함)에 숙련되며, 이를 착용해도 비전 주문 실패 확률이 없다.",
                "Proficient with light, medium and heavy armor and all shields including tower shields; no arcane spell failure from them."),
             
            ["staff.name"] = ("한손 지팡이", "One-Handed Staff"),
            ["staff.desc"] = (
                "쿼터스태프를 한 손으로 쥘 수 있다(이때 양손 힘 보너스는 없다).",
                "You can wield a quarterstaff in one hand (no two-handed Strength bonus)."),
             
            ["rebuke.name"] = ("방패 반격", "Shield Rebuke"),
            ["rebuke.desc"] = (
                "1·5·10·15·20레벨에 반격 효과를 하나씩 배운다. 배운 것 중 하나를 켜 두면, 보조손에 타워실드를 든 채 근접 무기 공격에 맞았을 때 공격자에게 그 효과가 들어간다(자기 라운드당 1회, 하루 3 + 지능 수정치 회, 최소 1). 효과가 들어가지 못해도 횟수는 쓴다.",
                "Learn one rebuke at levels 1, 5, 10, 15 and 20. Keep one active: when a melee weapon attack hits you while you hold a tower shield in your off hand, the attacker suffers it (once per your round, 3 + Int modifier times per day, minimum 1). A use is spent even if it has no effect."),
            ["rebuke.selection"] = ("방패 반격 배우기", "Learn Shield Rebuke"),
            ["e1.name"] = ("역장 반격", "Force Rebuke"),
            ["e1.desc"] = ("공격자에게 위저드 레벨 2당 1d6(올림) 역장 피해.", "The attacker takes 1d6 force damage per two wizard levels (round up)."),
            ["e2.name"] = ("전격 반격", "Shock Rebuke"),
            ["e2.desc"] = ("위저드 레벨 2당 1d6 전기 피해, 반사 내성 시 절반.", "1d6 electricity damage per two wizard levels; Reflex half."),
            ["e3.name"] = ("서리 반격", "Frost Rebuke"),
            ["e3.desc"] = ("위저드 레벨 2당 1d4 냉기 피해, 인내 내성 실패 시 1라운드 비틀거림.", "1d4 cold damage per two wizard levels; staggered for 1 round on a failed Fortitude save."),
            ["e4.name"] = ("밀쳐내기", "Repulse"),
            ["e4.desc"] = ("공격자를 약 10피트(3미터) 밀어낸다.", "Pushes the attacker about 10 feet (3 meters) away."),
            ["e5.name"] = ("감속 반격", "Slowing Rebuke"),
            ["e5.desc"] = ("의지 내성 실패 시 1 + 위저드 레벨 5당 1라운드 동안 감속.", "On a failed Will save the attacker is slowed for 1 round + 1 per five wizard levels."),
            ["e6.name"] = ("현혹 반격", "Dazing Rebuke"),
            ["e6.desc"] = ("의지 내성 실패 시 1라운드 멍해짐.", "On a failed Will save the attacker is dazed for 1 round."),
            ["e7.name"] = ("해제 반격", "Dispelling Rebuke"),
            ["e7.desc"] = ("공격자에게 대상 마법 해제를 시도한다(시전자 레벨 = 캐릭터 레벨).", "Attempts a targeted dispel magic on the attacker (caster level = character level)."),
            ["log.dud"] = ("방패 반격 불발: {0} — {1}", "Shield rebuke fizzles: {0} — {1}"),
            ["dud.down"] = ("공격자가 이미 쓰러짐", "attacker already down"),
             
            ["dud.nodamage"] = ("피해가 들어가지 않음(면역·저항)", "no damage got through (immune or resisted)"),
            ["dud.saved"] = ("내성 성공 또는 면역", "saved or immune"),
            ["dud.nodamagesaved"] = ("피해 없음, 내성 성공 또는 면역", "no damage, saved or immune"),
            ["dud.notpushed"] = ("밀려나지 않음", "not pushed"),
            ["dud.nothingtodispel"] = ("해제할 마법 없음", "nothing to dispel"),
            ["dud.dispelfailed"] = ("해제 판정 실패", "dispel check failed"),
            ["dud.dispelnotcast"] = ("해제 주문이 시전되지 않음", "the dispel was not cast"),
            ["dud.untargetable"] = ("대상으로 삼을 수 없음", "cannot be targeted"),
             
             
            ["iron.name"] = ("철벽 집중", "Iron Concentration"),
            // 미스릴 괄호: 엔진 ItemEntityArmor.ArmorType() @0001 IsMithral → 한 단계 낮은 군(미스릴 풀 플레이트 = Medium, PF 규칙과 같음) — 최종 검토 m-4.
            ["iron.desc"] = ("무거운 갑옷(미스릴 중갑은 중간 갑옷으로 친다)을 입고 있는 동안 피해로 인한 집중 판정에 자동으로 성공한다.", "While wearing heavy armor (mithral heavy armor counts as medium), you automatically succeed on concentration checks caused by damage."),
            ["bond.name"] = ("유대 방패", "Bonded Shield"),
            ["bond.desc"] = ("보조손에 타워실드를 들고 있는 동안, 하루 한 번 자유 행동으로 이미 시전한 위저드 주문 하나를 되찾는다. 5·10·15·20레벨에 보조손에 든 타워실드에 강화 +1씩(기존 강화와 겹치지 않음, 다른 손 세트의 방패에는 붙지 않음). 강화 단계마다 이 방패의 방어구 판정 페널티가 1 감소한다.", "While holding a tower shield in your off hand, once per day as a free action regain one cast wizard spell. +1 enhancement to the tower shield in your off hand at levels 5/10/15/20 (does not stack with existing enhancement; shields in other weapon sets get none). Each step reduces that shield’s armor check penalty by 1."),
        };
        }
    }
    // [순수] 선택 화면의 설명문 — 레벨업 선택 화면의 상세 창이 읽는 BlueprintUnitFact.m_Description(엔진 CharGenFeatureSelectorItemVM.TooltipTemplate
    //  → TooltipTemplateLevelUpFeature.Prepare → FeatureInformation.get_Description; m_DescriptionShort 는 읽지 않는다)과 아키타입 설명. 줄바꿈 = "\n"(본편 설명문과 같은 표기).
    // 수치 잠금(C4): 아래 문구 틀(문자열 리터럴)에는 숫자를 쓰지 않는다 — 수치는 전부 {n} 자리에 규칙 상수(BulwarkRules · RebukeTable · BodyTable)를 끼워 넣는다.
    //  시험 `strings: choice text` 가 (1) 이 형식의 문자열 리터럴에 {n} 밖의 숫자가 없음 (2) 만들어진 문구의 수치 열 == 손 전사 기대값 (3) ko/en 줄 수·수치 집합 일치
    //  (4) 선택지 설명이 서로 다름을 잠근다. 규칙에 없는 값(엔진·본편 블루프린트가 정하는 값)은 AddTo 안의 지역 변수로 두고 출처를 옆에 적는다
    //  (정적 필드는 `dial: single block` 이 금지) — 시험이 blueprints.zip · Assembly-CSharp 와 대조한다.
    // 틀에 넘긴 인자를 틀이 쓰지 않거나, 규칙 표에 문구가 없는 효과·특성이 생기면 던진다(조용히 빠진 수치·줄 없음).
    static class ChoiceText
    {
        // 한/영 한 쌍. 인자로 넘기면 그 언어 쪽이 들어간다.
        sealed class P
        {
            public readonly string Ko, En;
            public P(string ko, string en)
            {
                if (string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en)) throw new ArgumentException("BulwarkMage: choice text pair with an empty side");
                Ko = ko; En = en;
            }
        }

        static int Min { get { return BulwarkRules.Levels.MinLevel; } }
        static int Max { get { return BulwarkRules.Levels.MaxLevel; } }

        static P F(string ko, string en, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args", "BulwarkMage: choice text argument " + i + " is null (" + en + ")");
                string slot = "{" + i + "}";
                if (!ko.Contains(slot) || !en.Contains(slot)) throw new ArgumentException("BulwarkMage: choice text template does not use argument " + i + " in both languages (" + en + ")");
            }
            return new P(string.Format(ko, args.Select(a => a is P ? (object)((P)a).Ko : a).ToArray()), string.Format(en, args.Select(a => a is P ? (object)((P)a).En : a).ToArray()));
        }

        // 줄 묶음. 빈 묶음·null 줄이면 던진다.
        static P Lines(params P[] lines)
        {
            if (lines == null || lines.Length == 0 || lines.Any(x => x == null)) throw new ArgumentException("BulwarkMage: choice text without lines or with a missing line");
            return new P(string.Join("\n", lines.Select(x => x.Ko)), string.Join("\n", lines.Select(x => x.En)));
        }

        // 레벨 구간(같은 값이 이어지는 구간)을 「a~b레벨 값, c레벨부터 값」으로. 구간이 하나면 값만.
        static P Ranges(Func<int, P> at)
        {
            var segs = new List<Tuple<int, int, P>>();
            int start = Min;
            P cur = at(Min);
            for (int l = Min + 1; l <= Max + 1; l++)
            {
                P v = l <= Max ? at(l) : null;
                if (v != null && v.Ko == cur.Ko && v.En == cur.En) continue;
                segs.Add(Tuple.Create(start, l - 1, cur));
                start = l; cur = v;
            }
            if (segs.Count == 1) return segs[0].Item3;
            return new P(
                string.Join(", ", segs.Select(s => (s.Item1 == s.Item2 ? s.Item1 + "레벨 " : s.Item2 == Max ? s.Item1 + "레벨부터 " : s.Item1 + "~" + s.Item2 + "레벨 ") + s.Item3.Ko)),
                string.Join(", ", segs.Select(s => s.Item3.En + (s.Item1 == s.Item2 ? " at level " + s.Item1 : s.Item2 == Max ? " from level " + s.Item1 : " at levels " + s.Item1 + "-" + s.Item2))));
        }

        static string List(IReadOnlyList<int> levels, string what)
        {
            if (levels == null || levels.Count == 0) throw new InvalidOperationException("BulwarkMage: choice text: " + what + " is empty");
            return string.Join(", ", levels);
        }

        static P Rounds(int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException("n", n, "BulwarkMage: choice text duration must be positive");
            return new P(n + "라운드", n + (n == 1 ? " round" : " rounds")); // 영어 단수·복수(문법)
        }

        static int Sides(RebukeDie die)
        {
            switch (die)
            {
                case RebukeDie.D4: return 4;
                case RebukeDie.D6: return 6;
            }
            throw new ArgumentOutOfRangeException("die", die, "BulwarkMage: choice text has no side count for this die");
        }

        static P Energy(RebukeEnergy e)
        {
            switch (e)
            {
                case RebukeEnergy.Force: return new P("역장", "force");
                case RebukeEnergy.Electricity: return new P("전기", "electricity");
                case RebukeEnergy.Cold: return new P("냉기", "cold");
            }
            throw new ArgumentOutOfRangeException("e", e, "BulwarkMage: choice text has no name for this damage type");
        }

        // 공격자에게 거는 본편 상태의 이름(한글 패치 표기: Buffs/Staggered = 비틀거림, ConditionSlowed = 감속, Buffs/Daze = 몽롱).
        static P Condition(string buffGuid)
        {
            if (buffGuid == Guids.Game.Staggered) return new P("비틀거림", "staggered");
            if (buffGuid == Guids.Game.SlowBuff) return new P("감속", "slowed");
            if (buffGuid == Guids.Game.DazeBuff) return new P("몽롱", "dazed");
            throw new KeyNotFoundException("BulwarkMage: choice text has no condition name for the buff " + buffGuid);
        }

        static void Expect(bool ok, string what)
        {
            if (!ok) throw new InvalidOperationException("BulwarkMage: choice text is written for a different rule: " + what);
        }

        static P NameOf(Dictionary<string, (string ko, string en)> d, string key)
        {
            (string ko, string en) v;
            if (!d.TryGetValue(key, out v)) throw new KeyNotFoundException("BulwarkMage: choice text needs the name " + key);
            return new P(v.ko, v.en);
        }

        static void Put(Dictionary<string, (string ko, string en)> d, string key, P text) { d.Add(key, (text.Ko, text.En)); }

        internal static void AddTo(Dictionary<string, (string ko, string en)> d)
        {
            if (d == null) throw new ArgumentNullException("d");

            // ---- 규칙에 없는 값(엔진·본편 블루프린트). 시험 `strings: choice text` 가 blueprints.zip · Assembly-CSharp 와 대조한다.
            int dispelCount = 1;   // 본편 Spells/Level3/DispelMagicTarget.jbp ContextActionDispelMagic.m_CountToRemove = 1, m_StopAfterCountRemoved = true (다른 모드의 Patched: 줄 없음 — UMM Log.txt 2026-11-17)
            int dispelDie = 20;    // 엔진 RuleDispelMagic.OnTrigger @0053 ldc.i4.s 20 → RuleRollD20
            int dispelDcBase = 11; // 엔진 RuleDispelMagic.OnTrigger @0069 ldc.i4.s 11 + 그 버프의 AbilityParams.CasterLevel

            // ---- 문구가 기대는 규칙 모양(바뀌면 던진다 — 문구의 「절반(내림)」·「올림」·「내림」이 거짓이 된다).
            int perDie = BulwarkRules.Dials.RebukeLevelsPerDie, slowBase = BulwarkRules.Dials.RebukeSlowBaseRounds, slowPer = BulwarkRules.Dials.RebukeSlowLevelsPerRound;
            Expect(BulwarkRules.Dials.SaveDcLevelDivisor == 2, "the save DC adds half the wizard level");
            for (int l = Min; l <= Max; l++)
            {
                Expect(RebukeTable.DiceCount(l) == (l + perDie - 1) / perDie, "dice count is the wizard level over " + perDie + " rounded up");
                Expect(RebukeTable.SlowRounds(l) == slowBase + l / slowPer, "slow lasts " + slowBase + " + wizard level over " + slowPer + " rounded down");
            }

            // ---- 모든 반격 공통 줄
            P dc = F("DC {0} + 위저드 레벨의 절반(내림) + 지능 수정치", "DC {0} + half your wizard level (rounded down) + your Intelligence modifier", BulwarkRules.Dials.SaveDcBase);
            P trigger = F("발동: 이 효과를 켜 둔 채 보조 손에 타워 실드를 들고 근접 무기 공격에 명중당했을 때. 자기 라운드마다 {0}번, 하루에 {1} + 지능 수정치번(최소 {2}번)이며, 효과가 들어가지 않아도 횟수는 쓴다.",
                          "Trigger: while this is on and you hold a tower shield in your off hand, when a melee weapon attack hits you. {0} per round of yours and {1} + your Intelligence modifier per day (minimum {2}); a use is spent even when it has no effect.",
                          BulwarkRules.Dials.RebukesPerRound, BulwarkRules.Dials.RebukeDailyBase, BulwarkRules.Dials.RebukeDailyMin);
            P oneOn = new P("반격 효과는 한 번에 하나만 켜 둘 수 있다.", "Only one rebuke can be on at a time.");
            P noCrit = new P("이 피해는 공격자의 치명타로 늘지 않는다.", "This damage is not increased by the attacker's critical hit.");
            P noSave = new P("내성 굴림: 없음.", "Saving throw: none.");
            P noDamage = new P("피해는 없다.", "It deals no damage.");
            Func<RebukeSpec, P> damage = s =>
            {
                Expect(s.Die != null && s.DiceCount != null && s.Energy != null, s.Id + " deals damage");
                string die = "d" + Sides(s.Die.Value);
                return F("피해: 위저드 레벨 {0}당 {1} 하나(올림) — 위저드 {2}.", "Damage: one {1} per {0} wizard levels (rounded up): {2}.",
                         perDie, die, Ranges(l => { string v = s.DiceCount(l) + die; return new P(v, v); }));
            };

            // ---- 선택 화면
            Put(d, StringsTable.SelectionDescKey, Lines(
                F("위저드 {0}레벨에 반격 효과를 하나씩 배운다(모두 {1}번, 선택지 {2}종). 같은 효과는 한 번만 배울 수 있다.",
                  "At wizard levels {0} you learn one rebuke ({1} picks, {2} options). Each rebuke can be learned only once.",
                  List(BulwarkRules.Levels.RebukeLearnLevels, "RebukeLearnLevels"), BulwarkRules.Levels.RebukeLearnLevels.Count, RebukeTable.Effects.Count),
                new P("배운 효과는 켜고 끄는 능력이 된다. 한 번에 한 효과만 켜 둘 수 있고, 켜고 끄는 데 행동을 쓰지 않으며, 휴식해도 꺼지지 않는다.",
                      "A learned rebuke is an ability you switch on and off. Only one rebuke can be on at a time; switching takes no action and it stays on through rest."),
                new P("발동: 효과를 켜 둔 채 보조 손에 타워 실드를 들고 있을 때 근접 무기 공격에 명중당하면, 공격자에게 그 효과가 들어간다.",
                      "Trigger: while a rebuke is on and you hold a tower shield in your off hand, an attacker who hits you with a melee weapon attack suffers that rebuke."),
                F("횟수: 자기 라운드마다 {0}번, 하루에 {1} + 지능 수정치번(최소 {2}번). 효과가 들어가지 않아도(내성 굴림 성공·면역 등) 횟수는 쓴다.",
                  "Uses: {0} per round of yours and {1} + your Intelligence modifier per day (minimum {2}). A use is spent even when the rebuke has no effect (successful save, immunity and so on).",
                  BulwarkRules.Dials.RebukesPerRound, BulwarkRules.Dials.RebukeDailyBase, BulwarkRules.Dials.RebukeDailyMin),
                F("내성 굴림 {0}.", "Saving throw {0}.", dc),
                new P("반격 피해는 공격자의 치명타로 늘지 않는다.", "Rebuke damage is not increased by the attacker's critical hit.")));

            // ---- 선택지 7종(RebukeTable.Effects 의 모든 행 — 문구가 없는 행이 생기면 던진다)
            foreach (var s in RebukeTable.Effects)
            {
                P text;
                switch (s.Id)
                {
                    case "E1":
                        Expect(s.Save == RebukeSave.None && s.BuffGuid == null, s.Id + " is damage without a save");
                        text = Lines(F("공격자에게 {0} 피해를 준다.", "The attacker takes {0} damage.", Energy(s.Energy.Value)), damage(s), noSave, trigger, oneOn, noCrit);
                        break;
                    case "E2":
                        Expect(s.Save == RebukeSave.ReflexHalf && s.BuffGuid == null, s.Id + " is damage with a Reflex save for half");
                        text = Lines(F("공격자에게 {0} 피해를 준다.", "The attacker takes {0} damage.", Energy(s.Energy.Value)), damage(s),
                            F("내성 굴림: 반사 — 성공하면 피해 절반({0}).", "Saving throw: Reflex for half damage ({0}).", dc), trigger, oneOn, noCrit);
                        break;
                    case "E3":
                        Expect(s.Save == RebukeSave.FortNegatesBuff && s.BuffRounds != null, s.Id + " is damage plus a buff on a failed Fortitude save");
                        text = Lines(F("공격자에게 {0} 피해를 주고, {1} 상태로 만들 수 있다.", "The attacker takes {0} damage and may become {1}.", Energy(s.Energy.Value), Condition(s.BuffGuid)), damage(s),
                            F("피해에는 내성 굴림이 없다. 인내 내성 굴림({0})에 실패한 공격자는 {1} 동안 {2} 상태가 된다.",
                              "The damage allows no saving throw. An attacker who fails a Fortitude save ({0}) is {2} for {1}.", dc, Ranges(l => Rounds(s.BuffRounds(l))), Condition(s.BuffGuid)),
                            trigger, oneOn, noCrit);
                        break;
                    case "E4":
                        Expect(s.Save == RebukeSave.None && s.PushFeet != null && s.Energy == null, s.Id + " is a push without a save or damage");
                         
                        Expect(s.PushEngineDistance != null && s.PushEngineDistance.Value == RebukeTable.FeetToEngineDistance(s.PushFeet.Value), s.Id + " engine push distance is FeetToEngineDistance of its feet");
                        text = Lines(F("공격자를 약 {0}피트({1}미터) 밀어낸다. {2}", "Pushes the attacker about {0} feet ({1} meters) away. {2}", s.PushFeet.Value, s.PushEngineDistance.Value, noDamage),
                            new P("내성 굴림: 없음. 레벨에 따라 강해지지 않는다.", "Saving throw: none. It does not grow with level."),
                            new P("밀려나는 공격자는 기회 공격을 유발하지 않는다.", "The pushed attacker does not provoke attacks of opportunity."),
                            trigger, oneOn);
                        break;
                    case "E5":
                        Expect(s.Save == RebukeSave.WillNegates && s.BuffRounds != null && s.Energy == null, s.Id + " is a buff on a failed Will save");
                        text = Lines(F("공격자를 {0} 상태로 만든다. {1}", "The attacker becomes {0}. {1}", Condition(s.BuffGuid), noDamage),
                            F("내성 굴림: 의지 — 성공하면 무효({0}).", "Saving throw: Will negates ({0}).", dc),
                            F("지속: {0}에 위저드 레벨 {1}마다 한 라운드씩 추가(내림) — 위저드 {2}.", "Duration: {0} plus one more round per {1} wizard levels (rounded down): {2}.",
                              Rounds(slowBase), slowPer, Ranges(l => Rounds(s.BuffRounds(l)))),
                            trigger, oneOn);
                        break;
                    case "E6":
                        Expect(s.Save == RebukeSave.WillNegates && s.BuffRounds != null && s.Energy == null, s.Id + " is a buff on a failed Will save");
                        P dazeRounds = Ranges(l => Rounds(s.BuffRounds(l)));
                        Expect(dazeRounds.Ko == Rounds(BulwarkRules.Dials.RebukeDazeRounds).Ko, s.Id + " lasts the same at every level");
                        text = Lines(F("공격자를 {0} 상태로 만든다. {1}", "The attacker becomes {0}. {1}", Condition(s.BuffGuid), noDamage),
                            F("내성 굴림: 의지 — 성공하면 무효({0}).", "Saving throw: Will negates ({0}).", dc),
                            F("지속: {0}. 레벨에 따라 늘지 않는다.", "Duration: {0}. It does not grow with level.", dazeRounds),
                            trigger, oneOn);
                        break;
                    case "E7":
                        Expect(s.Save == RebukeSave.DispelCheck && s.CastSpellGuid == Guids.Game.DispelMagicTarget, s.Id + " casts the vanilla targeted dispel magic");
                        text = Lines(
                            F("공격자에게 걸린 주문 효과 {0}개를 해제하려 시도한다({1} 주문과 같은 효과). {2}", "Attempts to dispel {0} spell effect(s) on the attacker, like the spell {1}. {2}",
                              dispelCount, new P("「마법 해제 (대상)」", "Dispel Magic (Target)"), noDamage),
                            F("해제 판정: d{0} 굴림 + 시전자 레벨 대 DC {1} + 그 주문의 시전자 레벨. 내성 굴림은 없다.",
                              "Dispel check: d{0} + caster level against DC {1} + the caster level of that spell. There is no saving throw.", dispelDie, dispelDcBase),
                            new P("이 반격의 시전자 레벨은 위저드 레벨이 아니라 캐릭터 레벨이다.", "The caster level of this rebuke is your character level, not your wizard level."),
                            new P("공격자에게 해제할 주문이 없거나 판정에 실패해도 횟수는 쓴다.", "A use is spent even if the attacker has no spell to dispel or the check fails."),
                            trigger, oneOn);
                        break;
                    default:
                        throw new InvalidOperationException("BulwarkMage: choice text has no description for the rebuke " + s.Id);
                }
                Put(d, StringsTable.ChoiceKey(s.Id), text);
            }

            // ---- 아키타입(직업 선택 화면에서 고르는 항목): 얻는 것 · 잃는 것
            // 본편 특성 이름(한글 패치 · enGB 표기 — blueprints.zip 의 m_DisplayName 을 2026-11-17 에 읽음). 표에 없는 GUID 가 BodyTable 에 생기면 던진다.
            var vanilla = new Dictionary<string, P>
            {
                { Guids.Game.BurstBarrier, new P("폭발 방벽", "Burst Barrier") },
                { Guids.Game.TowerShieldNegatePenalty, new P("타워 실드 전문가", "Tower Shield Specialist") },
                { Guids.Game.TowerShieldTouchShield, new P("타워 실드 방어", "Tower Shield Defense") },
                { Guids.Game.TowerShieldTotalCover, new P("전방위 엄폐", "Total Cover") },
                { Guids.Game.TowerShieldEvasion, new P("타워 실드 회피", "Tower Shield Evasion") },
                { Guids.Game.TowerShieldImprovedEvasion, new P("향상된 타워 실드 회피", "Tower Shield Improved Evasion") },
                { Guids.Game.ArcaneBondSelection, new P("비전 유대", "Arcane Bond") },
                { Guids.Game.SpecialistSchoolSelection, new P("전문 학파", "Specialist School") },
                { Guids.Game.ScribingScrolls, new P("두루마리 작성", "Scribe Scrolls") },
                { Guids.Game.WizardFeatSelection, new P("위저드 보너스 특기", "Wizard Bonus Feat") },
            };
            Func<IEnumerable<(int level, string guid)>, string, P> byFeature = (rows, what) =>
            {
                var groups = rows.GroupBy(r => r.guid).ToArray();
                if (groups.Length == 0) throw new InvalidOperationException("BulwarkMage: choice text: " + what + " is empty");
                var parts = groups.Select(g =>
                {
                    P name;
                    if (!vanilla.TryGetValue(g.Key, out name)) throw new KeyNotFoundException("BulwarkMage: choice text has no name for the vanilla feature " + g.Key + " (" + what + ")");
                    return F("{0}({1}레벨)", "{0} ({1})", name, string.Join(", ", g.Select(r => r.level)));
                }).ToArray();
                return new P(string.Join(", ", parts.Select(p => p.Ko)), string.Join(", ", parts.Select(p => p.En)));
            };
            Put(d, "archetype.desc", Lines(
                new P("타워 실드와 중장 갑옷을 두르고 싸우는 위저드. 주문 시전은 일반 위저드와 같고, 근접 공격자에게는 방패가 되갚는다. 핵심 능력치는 지능이다.",
                      "An armored wizard whose tower shield answers melee attackers. Intelligence governs spells and daily shield rebukes."),
                F("{0}레벨에 얻는 것: {1}(모든 갑옷과 방패에 숙련, 그로 인한 비전 주문 실패 확률 없음), {2}(육척봉을 한 손으로 쥔다), {3}, {4}.",
                  "Gained at level {0}: {1} (proficiency with all armor and shields, no arcane spell failure chance from them), {2} (quarterstaff in one hand), {3}, {4}.",
                  Min, NameOf(d, "prof.name"), NameOf(d, "staff.name"), NameOf(d, "rebuke.name"), NameOf(d, "bond.name")),
                F("{0}: 위저드 {1}레벨에 반격 효과를 하나씩 배운다(보조 손에 타워 실드를 들어야 발동한다).",
                  "{0}: learn one rebuke at each of wizard levels {1} (requires an off-hand tower shield).",
                  NameOf(d, "rebuke.name"), List(BulwarkRules.Levels.RebukeLearnLevels, "RebukeLearnLevels")),
                F("{0}: 보조 손에 타워 실드를 든 동안 하루에 {1}번, 시전한 위저드 주문 하나를 되찾는다. 위저드 {2}레벨마다 보조 손 타워 실드의 강화 보너스가 한 단계 오른다.",
                  "{0}: while holding a tower shield in your off hand, regain one cast wizard spell {1} time(s) per day. At each of wizard levels {2} the enhancement bonus of your off-hand tower shield rises one step.",
                  NameOf(d, "bond.name"), BulwarkRules.Dials.BondRecallsPerDay, List(BulwarkRules.Levels.BondEnhancementLevels, "BondEnhancementLevels")),
                F("타워 실드 스페셜리스트(파이터)의 기능(괄호 안은 얻는 레벨): {0}, {1}({2}레벨).", "Tower Shield Specialist (fighter) features and their levels: {0}, {1} ({2}).",
                  byFeature(BodyTable.TowerShieldGrants, "TowerShieldGrants"), new P("타워 실드 훈련", "Tower Shield Training"), List(BulwarkRules.Levels.TowerTrainingLevels, "TowerTrainingLevels")),
                F("{0}({1}레벨), {2}({3}레벨 — 중장 갑옷을 입으면 피해로 인한 집중 판정에 자동으로 성공).",
                  "{0} ({1}), {2} ({3}: in heavy armor, concentration checks caused by damage succeed automatically).",
                  new P("갑옷 훈련", "Armor Training"), List(BulwarkRules.Levels.ArmorTrainingLevels, "ArmorTrainingLevels"), NameOf(d, "iron.name"), BulwarkRules.Levels.IronConcentrationLevel),
                F("잃는 것(괄호 안은 원래 얻는 레벨): {0}.", "Lost (with their levels): {0}.", byFeature(BodyTable.Removed, "Removed"))));
        }
    }
}
