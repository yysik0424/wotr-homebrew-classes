using System;
using System.Collections.Generic;
using System.Linq;

namespace Spellfist
{
    // 익스플로잇 설명문에 옮겨 적는 수치의 원장(C3). 게임 형식 없음(콘솔 시험이 컴파일한다 — modinfo testSources).
    // 값의 원천은 이 모드가 아니라 본편 블루프린트(복제 원본, ExploitGuids.Chain)와 엔진 IL 이다. 복제(Exploits.Build)는 클래스(→ 브롤러)·능력치(매력 → 지능)만 바꾸므로
    // 아래 값은 복제본에서도 그대로다. 시험 `choice text: vanilla facts` 가 blueprints.zip 의 해당 jbp 와 하나씩 대조한다(본편 패치로 값이 바뀌면 빨개진다).
    // jbp 경로는 Classes/Arcanist/ArcanistExploits/ArcanistExploit<이름>.jbp (2026-11-17 직독), 엔진 IL 은 Assembly-CSharp(dnlib 2026-11-17).
    public static class ExploitFacts
    {
        // 능력 10 + 에너지 방패 변형 4: AbilityResourceLogic.Amount = 1, m_IsSpendResource = true.
        public const int ReservoirCost = 1;
        // 비전 방벽 AbilityResourceLogic.ResourceCostIncreasingFacts = [CostBuff, CostBuff2] (2개). 가진 표식 하나마다 비용 +CostStep.
        public const int BarrierCostFacts = 2;
        // 엔진 AbilityResourceLogic.CalculateCost IL_006A~0074: HasFact 이면 ldc.i4.1 · add.
        public const int CostStep = 1;
        // 지속이 「순위 값 그대로」(ContextRankConfig m_Progression AsIs, 엔진 ApplyProgression IL_004E) 인 능력의 레벨당 단위 수.
        public const int DurationPerLevel = 1;
        // 엔진 BlueprintAbility.GetRange IL_0065: AbilityRange.Close → 30피트 고정(레벨 무관). 차원 건너기·역장 타격·얼음 유도탄·번개 창이 Range = Close.
        public const int CloseRangeFeet = 30;
        // 엔진 RuleCalculateAbilityParams.OnTrigger IL_023C~0240: DC = 10 + 주문 레벨 + 능력 수정치.
        public const int DcBase = 10;
        // 엔진 AbilityData.get_SpellLevel IL_0078~0093: 주문서·아이템·PretendSpellLevel 이 없는 능력의 주문 레벨 = min(9, 캐릭터 레벨 / 2).
        public const int DcSpellLevelCap = 9;
        public const int DcLevelDivisor = 2;

        // 에너지 방패 버프 4종: ContextCalculateSharedValue(DiceType One × 2 + 순위) × Modifier 5.0, 순위 = CasterLevel DivStep 5 → (2 + 시전자 레벨 / 5) × 5.
        public const int EnergyShieldBaseSteps = 2;
        public const int EnergyShieldUnit = 5;
        public const int EnergyShieldLevelStep = 5;
        // 역장 타격: ContextActionDealDamage Force, DiceType D4 × 1 + 순위(클래스 레벨 AsIs).
        public const int ForceStrikeDiceCount = 1;
        public const int ForceStrikeDie = 4;
        // 주문 저항 버프: 순위 = 클래스 레벨, BonusValue m_StepLevel 6 → 레벨 + 6. 상급 버프는 11.
        public const int SpellResistanceBase = 6;
        public const int GreaterSpellResistanceBase = 11;
        // 그림자 장막 버프: SetAttackerMissChance 20, AddStatBonus SkillStealth 5. 능력 지속: 순위 = 능력 수정치, BonusValue m_StepLevel 1 → 수정치 + 1 라운드.
        public const int ShadowVeilMissChance = 20;
        public const int ShadowVeilStealth = 5;
        public const int ShadowVeilExtraRounds = 1;
        // 나무 피부 버프: AddStatBonus AC NaturalArmor 2 (+ AddDamageResistancePhysical 순위 = 능력 수정치, BypassedByForm Slashing; AddFacts PlantTypeFake).
        public const int WoodenFleshNaturalArmor = 2;
        // 갑옷 가면이 거는 본편 Spells/Level1/MageArmorBuff.jbp: AddStatBonus AC Armor 4.
        public const int MageArmorBonus = 4;
        // 갑옷 가면의 대체 효과 Spells/Level1/ShieldOfFaithBuff.jbp: 순위 = CasterLevel StartPlusDivStep(m_StartLevel -6, m_StepLevel 6), m_UseMax m_Max 5.
        public const int ShieldOfFaithStart = -6;
        public const int ShieldOfFaithStep = 6;
        public const int ShieldOfFaithMax = 5;
        // 얼음 유도탄·번개 창: DiceType D6 × 순위 DamageDice(클래스 레벨 StartPlusDivStep m_StartLevel 1, m_StepLevel 2) + 순위 DamageBonus(능력 수정치).
        public const int MissileDie = 6;
        public const int MissileStart = 1;
        public const int MissileStep = 2;
        // 얼음 유도탄: 내성 실패 시 Buffs/Staggered.jbp 를 DiceType One × 1 라운드. 번개 창: LightningLanceBuff(SetFactOwnerMissChance 20)를 같은 식으로 1 라운드.
        public const int MissileDebuffRounds = 1;
        public const int LanceMissChance = 20;
        // 얼음 유도탄·번개 창의 원본 Conditional(복제가 그대로 둔다 — 조건의 특성은 Exploits.Build 의 바꿈표 밖이다): 시전자가 엘리멘탈 마스터 아르카니스트의
        //  원소 중점화 진행표 9레벨 숨은 특성(물 ElementalMasterPowerfulExploitWater f08b7f2f…, 대기 ElementalMasterPowerfulExploitAir 2833951e…)을 가지면 IfTrue = DiceType D8
        //  (주사위 개수·보너스 순위는 IfFalse 와 같다). 진행표 = Classes/Arcanist/Archetypes/ElementalMaster/ElementalFocus<원소>.jbp LevelEntries Level 9, m_Classes = 아르카니스트(2026-11-17 직독).
        //  멀티클래스(브롤러 + 엘리멘탈 마스터 아르카니스트 9레벨)면 권법술사도 이 갈래를 탄다 — 시험 `choice text: vanilla facts` 가 D8 과 특성 GUID·레벨을 잠근다.
        public const int PowerfulExploitDie = 8;
        public const int PowerfulExploitArcanistLevel = 9;
        // 수치를 레벨 구간으로 적을 때의 범위(브롤러·캐릭터 레벨 1~20).
        public const int MinLevel = 1;
        public const int MaxLevel = 20;

        // 능력의 행동 종류(jbp ActionType). 표에 없는 익스플로잇 능력은 Standard — 시험이 능력 14개 전부와 대조한다.
        public const string ActionStandard = "Standard", ActionMove = "Move", ActionSwift = "Swift";
        public static string ActionOf(string exploit)
        {
            if (exploit == "DimensionalSlide") return ActionMove;
            if (exploit == "ArcaneBarrier") return ActionSwift;
            return ActionStandard;
        }

        static void RequireLevel(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException("level", level, "Spellfist: level out of " + MinLevel + ".." + MaxLevel);
        }

        // 엔진 ContextRankConfig.ApplyProgression IL_005C~0073 (StartPlusDivStep): 1 + max((값 - m_StartLevel) / m_StepLevel, 0). C# 정수 나눗셈 = IL div.
        public static int StartPlusDivStep(int value, int start, int step)
        {
            if (step <= 0) throw new ArgumentOutOfRangeException("step", step, "Spellfist: StartPlusDivStep step must be positive");
            return 1 + Math.Max((value - start) / step, 0);
        }

        // 얼음 유도탄·번개 창의 주사위 수(브롤러 레벨).
        public static int MissileDice(int brawlerLevel)
        {
            RequireLevel(brawlerLevel);
            return StartPlusDivStep(brawlerLevel, MissileStart, MissileStep);
        }

        // 에너지 방패의 저항 값(캐릭터 레벨 = 주문서 없는 능력의 시전자 레벨, 엔진 RuleCalculateAbilityParams.OnTrigger IL_0074~009A). DivStep = 값 / m_StepLevel (IL_0074~007C).
        public static int EnergyShieldResistance(int characterLevel)
        {
            RequireLevel(characterLevel);
            return (EnergyShieldBaseSteps + characterLevel / EnergyShieldLevelStep) * EnergyShieldUnit;
        }

        // 신념의 방패 굴절 보너스(캐릭터 레벨). m_UseMax → min(값, m_Max) (엔진 ApplyMinMax IL_0036~0048).
        public static int ShieldOfFaithDeflection(int characterLevel)
        {
            RequireLevel(characterLevel);
            return Math.Min(StartPlusDivStep(characterLevel, ShieldOfFaithStart, ShieldOfFaithStep), ShieldOfFaithMax);
        }

        // 비전 방벽의 최대 비용: 기본 비용 + 표식 수 × 증가량.
        public static int BarrierMaxCost { get { return ReservoirCost + BarrierCostFacts * CostStep; } }
    }

    // [순수] 선택 화면(비전 학파 · 익스플로잇)과 선택지의 설명문. 레벨업 선택 화면의 상세 창은 BlueprintUnitFact.m_Description 전체를 읽는다
    //  (엔진 CharGenFeatureSelectorItemVM.TooltipTemplate → TooltipTemplateLevelUpFeature.Prepare → BlueprintUnitFact.get_Description). 줄바꿈 = "\n".
    // 수치 잠금(C4): 아래 문구 틀(리터럴)에는 숫자를 쓰지 않는다 — 수치는 전부 {n} 자리에 SpellfistRules(이 모드의 규칙)와 ExploitFacts(본편 블루프린트·엔진 값)를 끼워 넣는다.
    //  시험 `choice text` 가 (1) 이 형식의 문자열 리터럴에 {n} 밖의 숫자가 없음 (2) 만들어진 문구의 수치 == 손 전사 기대값 (3) ko/en 줄 수·수치 집합 일치
    //  (4) 선택지 설명이 서로 다름 (5) ExploitFacts == blueprints.zip 을 잠근다.
    // 익스플로잇은 특성(선택지)·능력·버프가 다른 키를 쓴다: 선택지 = 전체 줄, 능력 = 효과·사용 줄, 버프 = 걸려 있는 효과 줄만(버프 툴팁이 선택지용 긴 설명으로 길어지지 않게).
    // 틀에 넘긴 인자를 틀이 쓰지 않으면 던진다(조용히 빠진 수치 없음).
    static class ChoiceText
    {
        internal const string AbilitySuffix = ".AbilityDescription", BuffSuffix = ".BuffDescription";

        // 한/영 한 쌍. 인자로 넘기면 그 언어 쪽이 들어간다.
        sealed class P
        {
            public readonly string Ko, En;
            public P(string ko, string en)
            {
                if (string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en)) throw new ArgumentException("Spellfist: choice text pair with an empty side");
                Ko = ko; En = en;
            }
        }

        static P F(string ko, string en, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args", "Spellfist: choice text argument " + i + " is null (" + en + ")");
                string slot = "{" + i + "}";
                if (!ko.Contains(slot) || !en.Contains(slot)) throw new ArgumentException("Spellfist: choice text template does not use argument " + i + " in both languages (" + en + ")");
            }
            return new P(string.Format(ko, args.Select(a => a is P ? (object)((P)a).Ko : a).ToArray()), string.Format(en, args.Select(a => a is P ? (object)((P)a).En : a).ToArray()));
        }

        // 줄 종류: 능력 설명에도 싣는 줄, 버프 설명에도 싣는 줄. 선택지(특성) 설명은 모든 줄을 싣는다.
        const int A = 1, B = 2;

        sealed class Body
        {
            readonly List<Tuple<P, int>> lines = new List<Tuple<P, int>>();
            public Body Add(P line, int where)
            {
                if (line == null) throw new ArgumentNullException("line");
                lines.Add(Tuple.Create(line, where));
                return this;
            }
            public P All() { return Join(lines.Select(l => l.Item1)); }
            public bool Has(int where) { return lines.Any(l => (l.Item2 & where) != 0); }
            public P Only(int where) { return Join(lines.Where(l => (l.Item2 & where) != 0).Select(l => l.Item1)); }
            static P Join(IEnumerable<P> ps)
            {
                var l = ps.ToArray();
                if (l.Length == 0) throw new ArgumentException("Spellfist: choice text without lines");
                return new P(string.Join("\n", l.Select(x => x.Ko)), string.Join("\n", l.Select(x => x.En)));
            }
        }

        // 「a~b레벨 값, c레벨부터 값」(같은 값이 이어지는 레벨 구간). 구간이 하나면 던진다 — 구간 표기를 쓸 이유가 없는 값이다.
        static P Ranges(Func<int, string> at)
        {
            int min = ExploitFacts.MinLevel, max = ExploitFacts.MaxLevel;
            var segs = new List<Tuple<int, int, string>>();
            int start = min;
            string cur = at(min);
            for (int l = min + 1; l <= max + 1; l++)
            {
                string v = l <= max ? at(l) : null;
                if (v == cur) continue;
                if (string.IsNullOrEmpty(cur)) throw new InvalidOperationException("Spellfist: choice text value for level " + start + " is empty");
                segs.Add(Tuple.Create(start, l - 1, cur));
                start = l; cur = v;
            }
            if (segs.Count < 2) throw new InvalidOperationException("Spellfist: choice text level ranges: the value never changes");
            return new P(
                string.Join(", ", segs.Select(s => (s.Item1 == s.Item2 ? s.Item1 + "레벨 " : s.Item2 == max ? s.Item1 + "레벨부터 " : s.Item1 + "~" + s.Item2 + "레벨 ") + s.Item3)),
                string.Join(", ", segs.Select(s => s.Item3 + (s.Item1 == s.Item2 ? " at level " + s.Item1 : s.Item2 == max ? " from level " + s.Item1 : " at levels " + s.Item1 + "-" + s.Item2))));
        }

        static string Dice(int count, int die)
        {
            if (count <= 0 || die <= 0) throw new ArgumentOutOfRangeException("count", count, "Spellfist: choice text dice must be positive");
            return count + "d" + die;
        }

        static string Plus(int v)
        {
            if (v <= 0) throw new ArgumentOutOfRangeException("v", v, "Spellfist: choice text bonus must be positive");
            return "+" + v;
        }

        static P NameOf(Dictionary<string, (string ko, string en)> d, string key)
        {
            (string ko, string en) v;
            if (!d.TryGetValue(key + ".Name", out v)) throw new KeyNotFoundException("Spellfist: choice text needs the name " + key + ".Name");
            return new P(v.ko, v.en);
        }

        static void Put(Dictionary<string, (string ko, string en)> d, string key, P text) { d.Add(key, (text.Ko, text.En)); }

        static P ActionName(string exploit)
        {
            string a = ExploitFacts.ActionOf(exploit);
            if (a == ExploitFacts.ActionStandard) return new P("일반 행동", "standard action");
            if (a == ExploitFacts.ActionMove) return new P("이동 행동", "move action");
            if (a == ExploitFacts.ActionSwift) return new P("신속 행동", "swift action");
            throw new InvalidOperationException("Spellfist: choice text: unknown action type '" + a + "' for " + exploit);
        }

        static P Use(string exploit)
        {
            return F("사용: {0}, 비전 저수지 {1}점.", "Use: {0}; costs {1} arcane reservoir point.", ActionName(exploit), ExploitFacts.ReservoirCost);
        }

        static P SaveDc()
        {
            return F("내성 DC = {0} + 캐릭터 레벨의 절반(최대 {1}) + 지능 수정치.", "Save DC = {0} + half your character level (at most {1}) + your Intelligence modifier.",
                ExploitFacts.DcBase, ExploitFacts.DcSpellLevelCap);
        }

        internal static void AddTo(Dictionary<string, (string ko, string en)> d)
        {
            if (d == null) throw new ArgumentNullException("d");
            if (ExploitFacts.DcLevelDivisor != 2) throw new InvalidOperationException("Spellfist: choice text says 'half your character level' but ExploitFacts.DcLevelDivisor is " + ExploitFacts.DcLevelDivisor);
            Schools(d);
            Exploits(d);
        }

        // ---- 비전 학파(브롤러 첫 레벨에 SchoolPicks 번) ----
        static void Schools(Dictionary<string, (string ko, string en)> d)
        {
            var schools = (School[])Enum.GetValues(typeof(School));
            int levels = SpellfistRules.SchoolCounts.GetLength(1);
            if (SpellfistRules.SchoolCounts.GetLength(0) != schools.Length) throw new InvalidOperationException("Spellfist: SchoolCounts rows != School enum values");
            if (levels != SpellfistRules.MaxSpellLevel + 1) throw new InvalidOperationException("Spellfist: SchoolCounts columns != spell levels up to " + SpellfistRules.MaxSpellLevel);
            var totals = new int[schools.Length];
            for (int i = 0; i < schools.Length; i++)
            {
                var name = NameOf(d, "School" + schools[i]);
                var counts = Enumerable.Range(0, levels).Select(l => SpellfistRules.SchoolCounts[i, l]).ToArray();
                if (counts.Any(c => c < 0)) throw new InvalidOperationException("Spellfist: negative spell count for " + schools[i]);
                totals[i] = counts.Sum();
                if (totals[i] <= 0) throw new InvalidOperationException("Spellfist: school " + schools[i] + " has no spells");
                int cantrips = counts[0];
                var body = new Body()
                    .Add(F("{0}의 위저드 주문을 주문서에 더한다. 모두 {1}개다.", "Adds the wizard spells of the {0} school to your spellbook: {1} in all.", name, totals[i]), 0)
                    .Add(F("주문 레벨별 개수({0}레벨부터 {1}레벨까지 차례로): {2}.", "Spells per spell level (level {0} through {1}, in order): {2}.", 0, SpellfistRules.MaxSpellLevel, string.Join(", ", counts)), 0)
                    .Add(cantrips > 0
                        ? F("캔트립 {0}개가 포함된다.", "This includes {0} cantrip(s).", cantrips)
                        : F("이 게임의 {0}에는 캔트립이 {1}개라, 이 학파는 캔트립을 주지 않는다.", "{0} has no cantrips in this game (count {1}), so this school grants no cantrips.", name, cantrips), 0)
                    .Add(F("쓸 수 있는 주문 레벨의 {0} 주문은 전부 자동으로 알게 되고, 새 주문 레벨이 열리면 그 레벨의 주문도 전부 알게 된다. 레벨업 때 주문을 따로 고르지 않는다.",
                        "You automatically know every {0} spell of each spell level you can cast, and gain the spells of each new spell level as it opens. You do not pick spells on level-up.", name), 0)
                    .Add(new P("개수는 DLC 주문을 포함한 수다. 해당 DLC가 없으면 그 주문은 들어오지 않는다.", "The counts include DLC spells; without the DLC those spells are not added."), 0)
                    .Add(F("학파는 모두 {0}개를 고르며, 고르지 않은 학파의 주문과 범용 주문은 배울 수 없다(두루마리로도 배울 수 없다).",
                        "You choose {0} schools in all; spells of the other schools and universal spells cannot be learned, not even from scrolls.", SpellfistRules.SchoolPicks), 0);
                Put(d, "School" + schools[i] + ".Description", body.All());
            }

            var listKo = string.Join(", ", schools.Select((s, i) => NameOf(d, "School" + s).Ko + " " + totals[i]));
            var listEn = string.Join(", ", schools.Select((s, i) => NameOf(d, "School" + s).En + " " + totals[i]));
            var sel = new Body()
                .Add(F("브롤러 첫 레벨에 비전 학파 {0}개를 고른다(이 선택 화면이 {0}번 이어진다).", "At your first brawler level you choose {0} arcane schools (this screen comes up {0} times).", SpellfistRules.SchoolPicks), 0)
                .Add(new P("고른 학파의 위저드 주문만 주문서에 들어온다. 쓸 수 있는 주문 레벨의 주문은 전부 자동으로 알게 되므로 레벨업 때 주문을 따로 고르지 않는다.",
                    "Only wizard spells of the chosen schools enter your spellbook. You automatically know all of them for every spell level you can cast, so you do not pick spells on level-up."), 0)
                .Add(F("고르지 않은 학파 {0}개의 주문과 범용 주문은 배울 수 없다(두루마리로도 배울 수 없다).", "Spells of the {0} schools you did not choose, and universal spells, cannot be learned, not even from scrolls.", schools.Length - SpellfistRules.SchoolPicks), 0)
                .Add(F("학파별 주문 수: {0}.", "Spells per school: {0}.", new P(listKo, listEn)), 0)
                .Add(new P("한번 고르면 바꿀 수 없다.", "The choice cannot be changed later."), 0);
            Put(d, "SchoolSelection.Description", sel.All());
        }

        // ---- 익스플로잇(브롤러 레벨 ExploitLevels 에 하나씩) ----
        static void Exploits(Dictionary<string, (string ko, string en)> d)
        {
            var roots = ExploitGuids.Chain.Where(r => r.kind == "Feature").Select(r => r.exploit).ToArray();
            var perLevel = ExploitFacts.DurationPerLevel;
            int range = ExploitFacts.CloseRangeFeet;

            var sel = new Body()
                .Add(F("브롤러 레벨 {0}에 익스플로잇을 하나씩, 모두 {1}개 배운다. 선택지는 {2}종이고 같은 것은 한 번만 고를 수 있다.",
                    "At brawler levels {0} you learn one exploit each, {1} in all. There are {2} to choose from, and each can be chosen only once.",
                    new P(string.Join("·", SpellfistRules.ExploitLevels), string.Join(", ", SpellfistRules.ExploitLevels)), SpellfistRules.ExploitLevels.Length, roots.Length), 0)
                .Add(new P("본편 아르카니스트 비기(익스플로잇)와 같은 능력이지만 아르카니스트 레벨 대신 브롤러 레벨을, 매력 대신 지능을 쓴다. 시전자 레벨을 따르는 수치는 캐릭터 레벨을 쓴다.",
                    "They are the arcanist exploits of the base game, but use your brawler level instead of arcanist level and Intelligence instead of Charisma. Values based on caster level use your character level."), 0)
                .Add(F("비용은 비전 저수지에서 낸다(한 번에 {0}점, 비전 방벽만 최대 {1}점). 저수지 신속 시전과 같은 저수지를 쓴다.",
                    "The cost is paid from your arcane reservoir ({0} point per use; only Arcane Barrier can cost up to {1}). Reservoir Quicken draws on the same reservoir.",
                    ExploitFacts.ReservoirCost, ExploitFacts.BarrierMaxCost), 0)
                .Add(F("내성 굴림이 있는 익스플로잇의 DC = {0} + 캐릭터 레벨의 절반(최대 {1}) + 지능 수정치.",
                    "The save DC of exploits that allow a saving throw = {0} + half your character level (at most {1}) + your Intelligence modifier.", ExploitFacts.DcBase, ExploitFacts.DcSpellLevelCap), 0)
                .Add(F("상급 주문 저항은 브롤러 레벨 {0}부터 고를 수 있다.", "Greater Spell Resistance can be chosen from brawler level {0}.", SpellfistRules.GreaterExploitsLevel), 0);
            Put(d, "ExploitSelection.Description", sel.All());

            var bodies = new Dictionary<string, Body>();

            bodies["DimensionalSlide"] = new Body()
                .Add(F("차원의 틈을 지나 {0}피트 안의 지점으로 순간이동한다.", "Step through a dimensional crack to a point within {0} feet.", range), A)
                .Add(new P("거리는 레벨이 올라도 늘지 않는다.", "The distance does not grow with level."), A)
                .Add(Use("DimensionalSlide"), A);

            bodies["ArcaneBarrier"] = new Body()
                .Add(new P("피해를 대신 받는 임시 생명점을 브롤러 레벨 + 지능 수정치만큼 얻는다.", "Gain temporary hit points equal to your brawler level + your Intelligence modifier; damage is taken from them first."), A | B)
                .Add(F("지속: 브롤러 레벨당 {0}분, 또는 임시 생명점이 다 떨어질 때까지.", "Duration: {0} minute per brawler level, or until the temporary hit points are gone.", perLevel), A | B)
                .Add(new P("다시 쓰면 임시 생명점과 지속 시간이 새 값으로 바뀐다(겹쳐 쌓이지 않는다).", "Using it again replaces the temporary hit points and the duration (they do not stack)."), A)
                .Add(F("사용: {0}. 비용은 비전 저수지 {1}점에서 시작해 쓸 때마다 {2}점씩 올라 최대 {3}점이 되며, 휴식하면 처음 값으로 돌아온다.",
                    "Use: {0}. The cost starts at {1} arcane reservoir point, rises by {2} with each use up to {3}, and resets when you rest.",
                    ActionName("ArcaneBarrier"), ExploitFacts.ReservoirCost, ExploitFacts.CostStep, ExploitFacts.BarrierMaxCost), A);

            bodies["EnergyShield"] = new Body()
                .Add(new P("쓸 때 산성·냉기·전기·화염 중 하나를 골라 그 에너지에 대한 저항을 얻는다.", "Each time you use it, choose acid, cold, electricity or fire and gain resistance to that energy."), A | B)
                .Add(F("에너지 저항(캐릭터 레벨 기준): {0}.", "Energy resistance (by character level): {0}.", Ranges(l => ExploitFacts.EnergyShieldResistance(l).ToString())), A | B)
                .Add(F("지속: 브롤러 레벨당 {0}분.", "Duration: {0} minute per brawler level.", perLevel), A | B)
                .Add(Use("EnergyShield"), A);

            bodies["ForceStrike"] = new Body()
                .Add(F("{0}피트 안의 적 하나를 역장으로 타격한다. 명중 굴림도 내성 굴림도 없이 반드시 맞는다.", "Strike one enemy within {0} feet with force. It always hits: no attack roll and no saving throw.", range), A)
                .Add(F("피해: 역장 {0} + 브롤러 레벨.", "Damage: {0} force + your brawler level.", Dice(ExploitFacts.ForceStrikeDiceCount, ExploitFacts.ForceStrikeDie)), A)
                .Add(Use("ForceStrike"), A);

            bodies["SpellResistance"] = new Body()
                .Add(F("주문 저항 {0} + 브롤러 레벨을 얻는다.", "Gain spell resistance equal to {0} + your brawler level.", ExploitFacts.SpellResistanceBase), A | B)
                .Add(new P("지속: 지능 수정치만큼의 라운드.", "Duration: a number of rounds equal to your Intelligence modifier."), A | B)
                .Add(Use("SpellResistance"), A)
                .Add(F("상급 주문 저항을 배우면 주문 저항이 {0} + 브롤러 레벨로 오른다.", "With Greater Spell Resistance the spell resistance becomes {0} + your brawler level.", ExploitFacts.GreaterSpellResistanceBase), 0);

            bodies["GreaterSpellResistance"] = new Body()
                .Add(F("주문 저항 익스플로잇으로 얻는 주문 저항이 {0} + 브롤러 레벨에서 {1} + 브롤러 레벨로 오른다.",
                    "The spell resistance granted by the Spell Resistance exploit rises from {0} + your brawler level to {1} + your brawler level.",
                    ExploitFacts.SpellResistanceBase, ExploitFacts.GreaterSpellResistanceBase), B)
                .Add(new P("따로 쓰는 능력이 아니다. 주문 저항 익스플로잇을 쓸 때 자동으로 적용되고, 지속 시간과 비용은 그대로다.",
                    "It is not a separate ability: it applies whenever you use the Spell Resistance exploit, whose duration and cost stay the same."), 0)
                .Add(F("전제조건: 주문 저항 익스플로잇, 브롤러 레벨 {0}.", "Prerequisites: the Spell Resistance exploit, brawler level {0}.", SpellfistRules.GreaterExploitsLevel), 0);

            bodies["ShadowVeil"] = new Body()
                .Add(F("그림자 장막을 둘러, 자신을 노리는 모든 공격이 {0}% 확률로 빗나가게 한다.", "Wrap yourself in a veil of shadows: every attack against you has a {0}% miss chance.", ExploitFacts.ShadowVeilMissChance), A | B)
                .Add(F("은신 판정에 {0} 보너스도 얻는다.", "You also gain a {0} bonus on Stealth checks.", Plus(ExploitFacts.ShadowVeilStealth)), A | B)
                .Add(F("지속: 지능 수정치 + {0}라운드. 레벨이 올라도 효과는 커지지 않는다.", "Duration: your Intelligence modifier + {0} rounds. The effect does not grow with level.", ExploitFacts.ShadowVeilExtraRounds), A | B)
                .Add(Use("ShadowVeil"), A);

            bodies["WoodenFlesh"] = new Body()
                .Add(F("방어도에 자연 방어 보너스 {0}를 얻고, 베기가 아닌 물리 피해에 지능 수정치만큼의 피해 감소(피해 감소 N/베기)를 얻는다.",
                    "Gain a {0} natural armor bonus to AC and damage reduction equal to your Intelligence modifier against physical damage that is not slashing (DR N/slashing).", Plus(ExploitFacts.WoodenFleshNaturalArmor)), A | B)
                .Add(new P("효과가 이어지는 동안 식물 크리처로도 취급된다.", "While it lasts you also count as a plant creature."), A | B)
                .Add(F("지속: 브롤러 레벨당 {0}분.", "Duration: {0} minute per brawler level.", perLevel), A | B)
                .Add(Use("WoodenFlesh"), A);

            bodies["ArmoredMask"] = new Body()
                .Add(F("마법사의 갑옷 주문과 같은 효과를 얻는다: 방어도에 갑옷 보너스 {0}, 지속은 브롤러 레벨당 {1}시간.",
                    "Gain the effect of the mage armor spell: a {0} armor bonus to AC for {1} hour per brawler level.", Plus(ExploitFacts.MageArmorBonus), perLevel), A)
                .Add(F("이미 마법사의 갑옷 효과를 받고 있으면 대신 신념의 방패 효과를 브롤러 레벨당 {0}분 동안 얻는다: 방어도에 굴절 보너스(캐릭터 레벨 기준) {1}.",
                    "If you already have mage armor, you instead gain the effect of shield of faith for {0} minute per brawler level: a deflection bonus to AC (by character level) of {1}.",
                    perLevel, Ranges(l => Plus(ExploitFacts.ShieldOfFaithDeflection(l)))), A)
                .Add(new P("신화 능력 대마법사의 갑옷이 있으면 마법사의 갑옷 대신 그 강화판이 걸린다.", "With the Archmage Armor mythic ability you get its improved version of mage armor instead."), A)
                .Add(new P("갑옷 보너스는 입은 갑옷의 갑옷 보너스와 겹쳐 쌓이지 않고 높은 쪽만 적용된다.", "The armor bonus does not stack with the armor bonus of worn armor; only the higher one applies."), 0)
                .Add(Use("ArmoredMask"), A);

            // 원소 중점화 진행표 이름 = 한글 패치 translation.json / enGB.json 의 ElementalFocusWater·ElementalFocusAir m_DisplayName(c9af83ea… / 43189b06…).
            bodies["IceMissile"] = Missile("IceMissile", new P("얼음 유도탄", "a missile of ice"), new P("냉기", "cold"), new P("원소 중점화 — 물", "Elemental Focus — Water"),
                F("명중한 대상은 인내 내성 굴림에 실패하면 {0}라운드 동안 비틀거림 상태가 된다. 피해는 내성 굴림과 상관없이 들어간다.",
                    "A target that is hit and fails a Fortitude save is staggered for {0} round. The damage applies regardless of the save.", ExploitFacts.MissileDebuffRounds));

            bodies["LightningLance"] = Missile("LightningLance", new P("번개 창", "a lance of lightning"), new P("전기", "electricity"), new P("원소 중점화 — 대기", "Elemental Focus — Air"),
                F("명중한 대상은 인내 내성 굴림에 실패하면 {0}라운드 동안 자신의 모든 공격이 {1}% 확률로 빗나간다(실명 상태는 아니다). 피해는 내성 굴림과 상관없이 들어간다.",
                    "A target that is hit and fails a Fortitude save has a {1}% miss chance on all of its attacks for {0} round (it is not blinded). The damage applies regardless of the save.",
                    ExploitFacts.MissileDebuffRounds, ExploitFacts.LanceMissChance));

            if (!new HashSet<string>(bodies.Keys).SetEquals(roots)) throw new InvalidOperationException("Spellfist: choice text exploits [" + string.Join(",", bodies.Keys) + "] != ExploitGuids.Chain features [" + string.Join(",", roots) + "]");
            foreach (var e in roots)
            {
                var body = bodies[e];
                bool hasAbility = ExploitGuids.Chain.Any(r => r.exploit == e && r.kind == "Ability"), hasBuff = ExploitGuids.Chain.Any(r => r.exploit == e && r.kind == "Buff");
                if (body.Has(A) != hasAbility) throw new InvalidOperationException("Spellfist: choice text " + e + ": ability lines " + body.Has(A) + " but ExploitGuids.Chain ability rows " + hasAbility);
                if (body.Has(B) != hasBuff) throw new InvalidOperationException("Spellfist: choice text " + e + ": buff lines " + body.Has(B) + " but ExploitGuids.Chain buff rows " + hasBuff);
                Put(d, "Exploit" + e + ".Description", body.All());
                if (hasAbility) Put(d, "Exploit" + e + AbilitySuffix, body.Only(A));
                if (hasBuff) Put(d, "Exploit" + e + BuffSuffix, body.Only(B));
            }
        }

        // 얼음 유도탄·번개 창(같은 구조, 피해 종류와 내성 실패 효과만 다르다).
        static Body Missile(string exploit, P what, P energy, P focus, P onFailedSave)
        {
            int die = ExploitFacts.MissileDie;
            // 주사위가 처음 느는 레벨과 그 뒤의 간격(StartPlusDivStep): 시작 + 간격, 간격.
            int firstStep = ExploitFacts.MissileStart + ExploitFacts.MissileStep;
            if (ExploitFacts.MissileDice(firstStep) != ExploitFacts.MissileDice(ExploitFacts.MinLevel) + 1 || ExploitFacts.MissileDice(firstStep - 1) != ExploitFacts.MissileDice(ExploitFacts.MinLevel))
                throw new InvalidOperationException("Spellfist: choice text: missile dice do not first grow at level " + firstStep);
            var atKo = string.Join(", ", SpellfistRules.ExploitLevels.Select(l => l + "레벨 " + Dice(ExploitFacts.MissileDice(l), die)));
            var atEn = string.Join(", ", SpellfistRules.ExploitLevels.Select(l => Dice(ExploitFacts.MissileDice(l), die) + " at level " + l));
            var body = new Body()
                .Add(F("{0}피트 안의 적 하나에게 원거리 접촉 공격으로 {1}을 쏜다.", "Hurl {1} at one enemy within {0} feet as a ranged touch attack.", ExploitFacts.CloseRangeFeet, what), A)
                .Add(F("피해: {0} {1} + 지능 수정치. 브롤러 레벨 {2}부터 {3}레벨마다 주사위가 하나씩 는다({4}).",
                    "Damage: {1} {0} + your Intelligence modifier. From brawler level {2} it gains one die every {3} levels ({4}).",
                    energy, Dice(ExploitFacts.MissileDice(ExploitFacts.MinLevel), die), firstStep, ExploitFacts.MissileStep, new P(atKo, atEn)), A)
                .Add(F("멀티클래스로 엘리멘탈 마스터 아르카니스트가 {0}레벨에 얻는 강렬한 비기({1})를 가지고 있으면 피해 주사위가 d{2}로 바뀐다(주사위 개수는 같다).",
                    "If you also have Powerful Exploit ({1}), which an Elemental Master arcanist gains at level {0}, the damage dice become d{2} (the number of dice stays the same).",
                    ExploitFacts.PowerfulExploitArcanistLevel, focus, ExploitFacts.PowerfulExploitDie), A)
                .Add(onFailedSave, exploit == "LightningLance" ? A | B : A)
                .Add(SaveDc(), A)
                .Add(Use(exploit), A);
            return body;
        }
    }
}
