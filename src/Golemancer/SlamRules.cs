using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Golemancer
{
    // [순수] 골렘 슬램 횟수·「골렘 폭주」 판정(소유자 결정 2026-11-16). 게임 형식 없음 → 콘솔 시험이 직접 실행한다(modinfo testSources).
    //   횟수의 유일한 원장 = GolemRules.Dials.SlamCounts(구간 시작 레벨 → 평소, 폭주). 여기 함수는 전부 그 표에서 파생한다(C4):
    //     골렘 슬램 = 평소 또는 폭주 값 · 합체 중 본체 슬램 = max(Dials.FusionHandSlams, 그 값) · 추가 사지 수 = 그 값 − 이미 손에 든 슬램 수.
     
    //   게임 쪽(SlamLimbs.cs·Overdrive.cs)은 관측값을 넣고 결과만 적용한다.

    // 슬램 횟수 표 한 행. Overdrive == null = 그 구간에는 폭주가 없다(0 이 아니다 — 0 회로 읽히지 않게).
    public sealed class SlamCountRow
    {
        public readonly int FromLevel, Normal;
        public readonly int? Overdrive;
        public SlamCountRow(int fromLevel, int normal, int? overdrive) { FromLevel = fromLevel; Normal = normal; Overdrive = overdrive; }
        public override string ToString() { return "L" + FromLevel + "+ " + Normal + "/" + (Overdrive.HasValue ? Overdrive.Value.ToString() : "-"); }
    }

    // 추가 사지 버프 교정 계획(유닛 하나): 지울 버프 id 들 + 더할 id(null = 더하지 않음).
    public sealed class LimbPlan
    {
        public readonly string[] Remove;
        public readonly string Add;
        public LimbPlan(string[] remove, string add) { Remove = remove; Add = add; }
        public override string ToString() { return "-[" + string.Join(",", Remove) + "] +" + (Add ?? "-"); }
    }

    public static class SlamRules
    {
        // 표 불변식(빈 목록 = 일치). rows 를 인자로 받는다 — 시험이 어긋난 표를 넣어 각 불변식이 실제로 잡는지 본다.
        public static List<string> TableProblems(IList<SlamCountRow> rows)
        {
            var p = new List<string>();
            if (rows == null) { p.Add("no table"); return p; }
            if (rows.Count == 0) { p.Add("empty table"); return p; }
            if (rows.Any(r => r == null)) { p.Add("null row"); return p; }
            if (rows[0].FromLevel != GolemRules.MinLevel) p.Add("first row starts at level " + rows[0].FromLevel + ", expected " + GolemRules.MinLevel);
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.FromLevel < GolemRules.MinLevel || r.FromLevel > GolemRules.MaxLevel) p.Add("row " + r + " starts outside " + GolemRules.MinLevel + ".." + GolemRules.MaxLevel);
                if (r.Normal < GolemRules.Dials.GolemHandSlams) p.Add("row " + r + ": normal " + r.Normal + " < the hand slam " + GolemRules.Dials.GolemHandSlams);
                if (r.Overdrive.HasValue && r.Overdrive.Value < r.Normal + 1) p.Add("row " + r + ": overdrive " + r.Overdrive.Value + " < normal + 1");
                if (i == 0) continue;
                var q = rows[i - 1];
                if (r.FromLevel <= q.FromLevel) p.Add("row " + r + " does not start after " + q);
                if (r.Normal < q.Normal) p.Add("row " + r + ": normal decreases from " + q);
                if (q.Overdrive.HasValue && !r.Overdrive.HasValue) p.Add("row " + r + ": overdrive disappears after " + q);
                if (q.Overdrive.HasValue && r.Overdrive.HasValue && r.Overdrive.Value < q.Overdrive.Value) p.Add("row " + r + ": overdrive decreases from " + q);
            }
            if (!rows.Any(r => r.Overdrive.HasValue)) p.Add("no row has an overdrive value");
            return p;
        }

        static readonly ReadOnlyCollection<SlamCountRow> Table = Checked();

        static ReadOnlyCollection<SlamCountRow> Checked()
        {
            var problems = TableProblems(GolemRules.Dials.SlamCounts);
            if (problems.Count != 0) throw new InvalidOperationException("SlamRules: Dials.SlamCounts breaks its invariants: " + string.Join(" | ", problems.ToArray()));
            return GolemRules.Dials.SlamCounts;
        }

        static void RequireLevel(int level, string what)
        {
            if (level < GolemRules.MinLevel || level > GolemRules.MaxLevel)
                throw new ArgumentOutOfRangeException("level", "SlamRules." + what + ": level " + level + " outside " + GolemRules.MinLevel + ".." + GolemRules.MaxLevel);
        }

        // 그 레벨의 행 = 구간 시작 ≤ level 인 마지막 행.
        public static SlamCountRow Row(int level)
        {
            RequireLevel(level, "Row");
            SlamCountRow hit = null;
            foreach (var r in Table) if (r.FromLevel <= level) hit = r;
            if (hit == null) throw new InvalidOperationException("SlamRules.Row: no row covers level " + level);
            return hit;
        }

        public static int NormalSlams(int level) { return Row(level).Normal; }

        public static bool HasOverdrive(int level) { return Row(level).Overdrive.HasValue; }

        // 폭주가 없는 레벨에 물으면 던진다(「폭주 없음」을 평소 값이나 0 으로 돌려주지 않는다).
        public static int OverdriveSlams(int level)
        {
            var r = Row(level);
            if (!r.Overdrive.HasValue) throw new InvalidOperationException("SlamRules.OverdriveSlams: no overdrive at level " + level + " (first at level " + OverdriveLevel + ")");
            return r.Overdrive.Value;
        }

        // 폭주를 얻는 레벨 = 폭주 값이 처음 있는 행의 시작(아키타입 진행표·점검·설명 문구가 이것을 쓴다 — 따로 적지 않는다).
        public static int OverdriveLevel { get { return Table.First(r => r.Overdrive.HasValue).FromLevel; } }

        // 골렘의 한 라운드 슬램 횟수.
        public static int GolemSlams(int level, bool overdrive) { return overdrive ? OverdriveSlams(level) : NormalSlams(level); }

        // 합체한 골렘술사의 한 라운드 슬램 횟수 = max(기존 합체 횟수, 표 값).
        public static int FusedSlams(int level, bool overdrive) { return Math.Max(GolemRules.Dials.FusionHandSlams, GolemSlams(level, overdrive)); }

        // 추가 사지 수 = 횟수 − 이미 손에 든 슬램 수(골렘 = 주 손 1, 합체 본체 = 주 손 + 보조 손 2). 음수면 표·다이얼이 어긋난 것 → 던진다.
        public static int GolemExtraLimbs(int level, bool overdrive) { return Extra(GolemSlams(level, overdrive), GolemRules.Dials.GolemHandSlams, "golem", level); }
        public static int MasterExtraLimbs(int level, bool overdrive) { return Extra(FusedSlams(level, overdrive), GolemRules.Dials.FusionHandSlams, "fused master", level); }

        static int Extra(int slams, int hands, string who, int level)
        {
            if (slams < hands) throw new InvalidOperationException("SlamRules: " + who + " slams " + slams + " < hand slams " + hands + " at level " + level);
            return slams - hands;
        }

        // ---------------- 추가 사지 버프(키 = 재질 버프 키 × 추가 수) ----------------

        public const char LimbIdSeparator = '#';

        public static string LimbId(string buffKey, int extra)
        {
            MaterialCatalog.MaterialOfKey(buffKey); // 모르는 키면 던진다
            if (extra < 1) throw new ArgumentOutOfRangeException("extra", "SlamRules.LimbId: extra " + extra + " < 1 (zero extra limbs = no buff)");
            return buffKey + LimbIdSeparator + extra;
        }

        public static KeyValuePair<string, int> ParseLimbId(string id)
        {
            if (id == null) throw new ArgumentNullException("id");
            int at = id.LastIndexOf(LimbIdSeparator);
            int extra;
            if (at <= 0 || !int.TryParse(id.Substring(at + 1), out extra) || extra < 1) throw new ArgumentException("SlamRules.ParseLimbId: '" + id + "' is not '<buff key>#<extra>'");
            string key = id.Substring(0, at);
            MaterialCatalog.MaterialOfKey(key);
            return new KeyValuePair<string, int>(key, extra);
        }

        // 그 유닛이 지금 달고 있어야 하는 추가 사지 버프 id(null = 없음). level 은 그 재질의 합법 레벨이어야 한다(StatRecipe.BuffKey 가 던진다).
        public static string WantedLimbId(Material m, int level, bool overdrive, bool fusedMaster)
        {
            if (m == null) throw new ArgumentNullException("m");
            MaterialCatalog.RequireLegal(m, level); // 추가 수가 0 이어도 (재질, 레벨) 이 합법인지 먼저 본다
            int extra = fusedMaster ? MasterExtraLimbs(level, overdrive) : GolemExtraLimbs(level, overdrive);
            return extra == 0 ? null : LimbId(StatRecipe.BuffKey(m, level), extra);
        }

        // 필요한 버프 전부: 재질마다 합법 레벨 × (평소 / 폭주가 있으면 폭주) × (골렘 / 합체 본체) 의 추가 수 중 0 이 아닌 것. 카탈로그 키 순서 → 추가 수 오름차순.
        public static ReadOnlyCollection<string> LimbIds { get { return limbIds; } }
        static readonly ReadOnlyCollection<string> limbIds = BuildLimbIds();

        static ReadOnlyCollection<string> BuildLimbIds()
        {
            var ids = new List<string>();
            foreach (var key in MaterialCatalog.Keys)
            {
                var m = MaterialCatalog.MaterialOfKey(key);
                var extras = new SortedSet<int>();
                foreach (int level in MaterialCatalog.LegalLevels(m))
                {
                    if (StatRecipe.BuffKey(m, level) != key) continue;
                    foreach (bool od in new[] { false, true })
                    {
                        if (od && !HasOverdrive(level)) continue;
                        extras.Add(GolemExtraLimbs(level, od));
                        extras.Add(MasterExtraLimbs(level, od));
                    }
                }
                extras.Remove(0);
                foreach (int e in extras) ids.Add(LimbId(key, e));
            }
            return ids.AsReadOnly();
        }

        // id → 버프 GUID. 표의 id 집합이 LimbIds 와 다르면 형식 초기화가 던진다(표를 바꿨는데 GUID 를 안 맞춘 경우).
        static readonly ReadOnlyDictionary<string, string> LimbGuids = BuildLimbGuids();

        static ReadOnlyDictionary<string, string> BuildLimbGuids()
        {
            var kv = new[]
            {
                "stone@T2#1", Guids.SlamLimbsStoneT2x1, "iron@T2#1", Guids.SlamLimbsIronT2x1, "brass@T2#1", Guids.SlamLimbsBrassT2x1,
                "gold@T3#1", Guids.SlamLimbsGoldT3x1, "gold@T3#2", Guids.SlamLimbsGoldT3x2, "autumn@T3#1", Guids.SlamLimbsAutumnT3x1, "autumn@T3#2", Guids.SlamLimbsAutumnT3x2,
                "summer@T3#1", Guids.SlamLimbsSummerT3x1, "summer@T3#2", Guids.SlamLimbsSummerT3x2,
                "adamantine@T4#1", Guids.SlamLimbsAdamantineT4x1, "adamantine@T4#2", Guids.SlamLimbsAdamantineT4x2, "adamantine@T4#3", Guids.SlamLimbsAdamantineT4x3,
                "greatergold@T4#1", Guids.SlamLimbsGreaterGoldT4x1, "greatergold@T4#2", Guids.SlamLimbsGreaterGoldT4x2, "greatergold@T4#3", Guids.SlamLimbsGreaterGoldT4x3,
                "mithral@T4#1", Guids.SlamLimbsMithralT4x1, "mithral@T4#2", Guids.SlamLimbsMithralT4x2, "mithral@T4#3", Guids.SlamLimbsMithralT4x3,
                "adamantine@T5#1", Guids.SlamLimbsAdamantineT5x1, "adamantine@T5#2", Guids.SlamLimbsAdamantineT5x2, "adamantine@T5#3", Guids.SlamLimbsAdamantineT5x3,
                "greatergold@T5#1", Guids.SlamLimbsGreaterGoldT5x1, "greatergold@T5#2", Guids.SlamLimbsGreaterGoldT5x2, "greatergold@T5#3", Guids.SlamLimbsGreaterGoldT5x3,
                "mithral@T5#1", Guids.SlamLimbsMithralT5x1, "mithral@T5#2", Guids.SlamLimbsMithralT5x2, "mithral@T5#3", Guids.SlamLimbsMithralT5x3,
            };
            var d = new Dictionary<string, string>();
            for (int i = 0; i < kv.Length; i += 2) d.Add(kv[i], kv[i + 1]); // 중복 id → ArgumentException
            if (!new HashSet<string>(d.Keys).SetEquals(LimbIds) || d.Count != LimbIds.Count)
                throw new InvalidOperationException("SlamRules: limb buff GUID table ids (" + string.Join(",", d.Keys.ToArray()) + ") != needed ids (" + string.Join(",", LimbIds.ToArray()) + ") — Dials.SlamCounts changed without the GUID table");
            if (d.Values.Distinct().Count() != d.Count) throw new InvalidOperationException("SlamRules: limb buff GUID listed twice");
            return new ReadOnlyDictionary<string, string>(d);
        }

        public static string LimbGuid(string limbId)
        {
            if (limbId == null) throw new ArgumentNullException("limbId");
            string g;
            if (!LimbGuids.TryGetValue(limbId, out g)) throw new KeyNotFoundException("SlamRules: no limb buff for '" + limbId + "' (needed ids: " + string.Join(",", LimbIds.ToArray()) + ")");
            return g;
        }

        public static IEnumerable<string> AllLimbGuids { get { return LimbGuids.Values; } }

        // 교정 계획: 기대 id 가 정확히 1개 있으면 그대로 두고 나머지를 지운다. 없으면 전부 지우고 1개 더한다. wanted == null → 전부 지운다.
        // 같은 id 가 2개 이상이면(있을 수 없는 상태) 전부 지우고 다시 1개(조용히 하나를 고르지 않는다).
        public static LimbPlan PlanLimbs(IList<string> current, string wanted)
        {
            if (current == null) throw new ArgumentNullException("current");
            foreach (var c in current) ParseLimbId(c);
            if (wanted != null) ParseLimbId(wanted);
            int same = current.Count(c => c == wanted);
            if (wanted != null && same == 1) return new LimbPlan(current.Where(c => c != wanted).ToArray(), null);
            return new LimbPlan(current.ToArray(), wanted);
        }

        public static bool LimbsSatisfied(IList<string> current, string wanted)
        {
            if (current == null) throw new ArgumentNullException("current");
            return wanted == null ? current.Count == 0 : current.Count == 1 && current[0] == wanted;
        }
    }

    // ---------------- 골렘 폭주(소유자 결정 2026-11-16) ----------------
    // 5레벨(= 표에서 폭주 값이 처음 있는 행), 신속 행동, 비용 = 준비한 추출물 1개(응급 수리와 같은 소모 경로 ExtractSlots — 변형 = 추출물 레벨 × 학파),
    //   지속 = 추출물 레벨 × Dials.OverdriveRoundsPerExtractLevel 라운드. 전투 여부와 무관(미리 켤 수 있고, 전투가 끝나도 남은 지속은 흐른다).
    //   지속 중: 골렘 슬램 횟수 = 표의 폭주 값 + 소모한 추출물 학파의 추가 효과(Dials.OverdriveSchoolEffects — 학파 없음은 효과 없음). 합체 중에는 둘 다 본체에.
    //   골렘이 부서지거나 사라지면 즉시 끝난다. 이미 폭주 중이면 다시 못 쓴다(남은 지속을 덮어쓰지 않는다).

    // 학파 효과의 종류(값의 뜻은 SchoolEffectRow 주석). 게임 쪽 OverdriveEffects.Build 가 종류마다 본편 컴포넌트를 단다 — 모르는 종류는 던진다.
    public enum SchoolEffectKind { Strength, FastHealing, ArmorClass, AttackAndCritConfirm, Concealment, SlamNegativeDice, SlamShaken, SlamElectricityDice }

    // 학파 효과 표 한 행. A·B 의 뜻: Strength A = 힘 보너스 · FastHealing A = 라운드당 회복 · ArmorClass A = AC 보너스 · AttackAndCritConfirm A = 명중, B = 치명타 확정 굴림 ·
    //   Concealment A = 빗나감 % · SlamNegativeDice / SlamElectricityDice A = 주사위 수, B = 주사위 면 · SlamShaken A = 동요 라운드. 쓰지 않는 B 는 0 이어야 한다(EffectProblems).
    public sealed class SchoolEffectRow
    {
        public readonly string School;
        public readonly SchoolEffectKind Kind;
        public readonly int A, B;
        public SchoolEffectRow(string school, SchoolEffectKind kind, int a, int b) { School = school; Kind = kind; A = a; B = b; }
        public override string ToString() { return School + " " + Kind + " " + A + "/" + B; }
    }

    // 폭주를 못 쓰는 사유. Unknown = 관측 결측(판정 불가 — C1: 「불가」로 뭉개지 않는다. 막고 오류로 보고한다).
    public enum OverdriveBlock { None, Unknown, AlreadyActive, NoGolem, GolemBroken, FusionBusy, NoSlot }

    // 판정 입력. null = 관측 못 함. 기본값 없음 — 게임 어댑터가 모든 칸을 채운다(못 채우면 null).
    public sealed class OverdriveInputs
    {
        public int SlotLevel;         // 소모할 추출물 레벨(능력 변형) — 1..MaxExtractLevel 밖이면 던진다
        public string School;         // 소모할 추출물 학파(능력 변형) — OverdriveRules.SchoolChoices 밖이면 던진다
        public int? MasterLevel;      // 연금술사 레벨
        public bool? Active;          // 폭주 버프가 이미 있다
        public LifecycleState? Life;
        public bool? GolemBound;      // 펫 칸의 펫 == 결속 GolemId 이고 우리 골렘
        public bool? GolemDead;
        public bool? Broken;
        public FusionState? Fusion;
        public int? SpendableSlots;   // 그 레벨·그 학파의 쓸 수 있는(준비·미사용·주 칸) 추출물 수
    }

    // 진행 중인 폭주를 끝낼 사유. Unknown = 관측 결측 — 게임 쪽은 오류로 보고하고 끝낸다(근거 없이 버프를 남기지 않는다).
    public enum OverdriveEnd { Keep, Unknown, GolemLost, GolemBroken }

    public sealed class OverdriveEndInputs
    {
        public bool? GolemBound;
        public bool? GolemDead;
        public bool? Broken;
    }

    public sealed class GeneratedString
    {
        public readonly string Key, Ko, En;
        public GeneratedString(string key, string ko, string en) { Key = key; Ko = ko; En = en; }
    }

    public static class OverdriveRules
    {
        // ---------------- 학파 ----------------
        // 학파 값 = 엔진 SpellSchool 의 이름. 학파 없는 추출물 = NoSchool 이라는 명시적 값(기본 학파로 떨어뜨리지 않는다). 모르는 이름·null 은 던진다.
        public const string NoSchool = "None";
        // 엔진 Kingmaker.Blueprints.Classes.Spells.SpellSchool 의 여덟 학파(시험 `wiring: overdrive schools` 가 게임 열거형 전체 == Schools ∪ NoSchoolNames 를 대조). 본편 연금술사 추출물 71개의 학파는 전부 이 안이다(정찰 §5).
        public static readonly ReadOnlyCollection<string> Schools = Array.AsReadOnly(new[]
        {
            "Abjuration", "Conjuration", "Divination", "Enchantment", "Evocation", "Illusion", "Necromancy", "Transmutation",
        });
        // 「학파 없음」으로 다루는 엔진 값: None(SpellComponent 없음 — BlueprintAbility.get_School @000D) · Universalist(위저드 「전문화 없음」 표식 — 주문의 학파가 아니다). 명시 목록 — 그 밖의 값은 던진다.
        public static readonly ReadOnlyCollection<string> NoSchoolNames = Array.AsReadOnly(new[] { "None", "Universalist" });
        // 능력 변형의 학파 선택지 = 여덟 학파 + 학파 없음.
        public static readonly ReadOnlyCollection<string> SchoolChoices = Array.AsReadOnly(Schools.Concat(new[] { NoSchool }).ToArray());

        public static string SchoolOf(string engineSchoolName)
        {
            if (engineSchoolName == null) throw new ArgumentNullException("engineSchoolName", "OverdriveRules.SchoolOf: the extract's school was not read");
            if (NoSchoolNames.Contains(engineSchoolName)) return NoSchool;
            if (!Schools.Contains(engineSchoolName)) throw new ArgumentException("OverdriveRules.SchoolOf: unknown spell school '" + engineSchoolName + "' (known: " + string.Join(",", Schools.ToArray()) + "; no school: " + string.Join(",", NoSchoolNames.ToArray()) + ")");
            return engineSchoolName;
        }

        static void RequireChoice(string school, string what)
        {
            if (school == null) throw new ArgumentNullException("school");
            if (!SchoolChoices.Contains(school)) throw new ArgumentException("OverdriveRules." + what + ": '" + school + "' is not a school choice (" + string.Join(",", SchoolChoices.ToArray()) + ")");
        }

        // ---------------- 학파 효과 표(Dials.OverdriveSchoolEffects) ----------------
        // 표 불변식(빈 목록 = 일치): 여덟 학파가 정확히 한 번씩, 학파 없음은 표에 없다, 값이 종류의 범위 안.
        // 엔진이 표현할 수 있는 값인지(주사위 면 = DiceType, 은폐 % = Concealment 단계)는 게임 형식이 필요하다 → 빌드(Overdrive.EffectComponents 가 던진다)와 시험 `wiring: overdrive effects`(게임 열거형 dnlib 대조)가 본다.
        public static List<string> EffectProblems(IList<SchoolEffectRow> rows)
        {
            var p = new List<string>();
            if (rows == null) { p.Add("no table"); return p; }
            if (rows.Any(r => r == null || r.School == null)) { p.Add("null row / school"); return p; }
            foreach (var s in Schools)
            {
                int n = rows.Count(r => r.School == s);
                if (n != 1) p.Add("school " + s + " is in the table " + n + " time(s), expected 1");
            }
            foreach (var r in rows)
            {
                if (!Schools.Contains(r.School)) p.Add("row " + r + ": not one of the eight schools (no-school has no effect row)");
                if (!Enum.IsDefined(typeof(SchoolEffectKind), r.Kind)) { p.Add("row " + r + ": undefined kind"); continue; }
                if (r.A < 1) p.Add("row " + r + ": A < 1");
                bool usesB = r.Kind == SchoolEffectKind.AttackAndCritConfirm || r.Kind == SchoolEffectKind.SlamNegativeDice || r.Kind == SchoolEffectKind.SlamElectricityDice;
                if (usesB && r.B < 1) p.Add("row " + r + ": B < 1");
                if (!usesB && r.B != 0) p.Add("row " + r + ": B must be 0 for " + r.Kind);
            }
            if (rows.Select(r => r.Kind).Distinct().Count() != rows.Count) p.Add("an effect kind is used twice");
            return p;
        }

        static readonly ReadOnlyCollection<SchoolEffectRow> Effects = CheckedEffects();

        static ReadOnlyCollection<SchoolEffectRow> CheckedEffects()
        {
            var problems = EffectProblems(GolemRules.Dials.OverdriveSchoolEffects);
            if (problems.Count != 0) throw new InvalidOperationException("OverdriveRules: Dials.OverdriveSchoolEffects breaks its invariants: " + string.Join(" | ", problems.ToArray()));
            return GolemRules.Dials.OverdriveSchoolEffects;
        }

        // 학파 → 효과 행. 학파 없음 = null(「추가 효과 없음」이라는 뜻 — 결측이 아니다). 선택지 밖의 값은 던진다.
        public static SchoolEffectRow EffectOrNull(string school)
        {
            RequireChoice(school, "EffectOrNull");
            if (school == NoSchool) return null;
            return Effects.Single(r => r.School == school);
        }

        // ---------------- 변형(추출물 레벨 × 학파) · GUID ----------------
        public static string VariantId(int slotLevel, string school)
        {
            RepairRules.RequireSlotLevel(slotLevel);
            RequireChoice(school, "VariantId");
            return "L" + slotLevel + ":" + school;
        }

        // 전부: 레벨 오름차순 → 학파(SchoolChoices 순서). 6 × 9 = 54. (정적 필드로 두지 않는다 — 수치 정적 필드는 Dials 에만: 시험 `dial: single block`.)
        public static KeyValuePair<int, string>[] Variants
        {
            get { return Enumerable.Range(1, GolemRules.Dials.MaxExtractLevel).SelectMany(l => SchoolChoices.Select(s => new KeyValuePair<int, string>(l, s))).ToArray(); }
        }

        static readonly ReadOnlyDictionary<string, string> VariantGuids = Table(new[]
        {
                "L1:Abjuration", Guids.OverdriveL1Abjuration,
                "L1:Conjuration", Guids.OverdriveL1Conjuration,
                "L1:Divination", Guids.OverdriveL1Divination,
                "L1:Enchantment", Guids.OverdriveL1Enchantment,
                "L1:Evocation", Guids.OverdriveL1Evocation,
                "L1:Illusion", Guids.OverdriveL1Illusion,
                "L1:Necromancy", Guids.OverdriveL1Necromancy,
                "L1:Transmutation", Guids.OverdriveL1Transmutation,
                "L1:None", Guids.OverdriveL1None,
                "L2:Abjuration", Guids.OverdriveL2Abjuration,
                "L2:Conjuration", Guids.OverdriveL2Conjuration,
                "L2:Divination", Guids.OverdriveL2Divination,
                "L2:Enchantment", Guids.OverdriveL2Enchantment,
                "L2:Evocation", Guids.OverdriveL2Evocation,
                "L2:Illusion", Guids.OverdriveL2Illusion,
                "L2:Necromancy", Guids.OverdriveL2Necromancy,
                "L2:Transmutation", Guids.OverdriveL2Transmutation,
                "L2:None", Guids.OverdriveL2None,
                "L3:Abjuration", Guids.OverdriveL3Abjuration,
                "L3:Conjuration", Guids.OverdriveL3Conjuration,
                "L3:Divination", Guids.OverdriveL3Divination,
                "L3:Enchantment", Guids.OverdriveL3Enchantment,
                "L3:Evocation", Guids.OverdriveL3Evocation,
                "L3:Illusion", Guids.OverdriveL3Illusion,
                "L3:Necromancy", Guids.OverdriveL3Necromancy,
                "L3:Transmutation", Guids.OverdriveL3Transmutation,
                "L3:None", Guids.OverdriveL3None,
                "L4:Abjuration", Guids.OverdriveL4Abjuration,
                "L4:Conjuration", Guids.OverdriveL4Conjuration,
                "L4:Divination", Guids.OverdriveL4Divination,
                "L4:Enchantment", Guids.OverdriveL4Enchantment,
                "L4:Evocation", Guids.OverdriveL4Evocation,
                "L4:Illusion", Guids.OverdriveL4Illusion,
                "L4:Necromancy", Guids.OverdriveL4Necromancy,
                "L4:Transmutation", Guids.OverdriveL4Transmutation,
                "L4:None", Guids.OverdriveL4None,
                "L5:Abjuration", Guids.OverdriveL5Abjuration,
                "L5:Conjuration", Guids.OverdriveL5Conjuration,
                "L5:Divination", Guids.OverdriveL5Divination,
                "L5:Enchantment", Guids.OverdriveL5Enchantment,
                "L5:Evocation", Guids.OverdriveL5Evocation,
                "L5:Illusion", Guids.OverdriveL5Illusion,
                "L5:Necromancy", Guids.OverdriveL5Necromancy,
                "L5:Transmutation", Guids.OverdriveL5Transmutation,
                "L5:None", Guids.OverdriveL5None,
                "L6:Abjuration", Guids.OverdriveL6Abjuration,
                "L6:Conjuration", Guids.OverdriveL6Conjuration,
                "L6:Divination", Guids.OverdriveL6Divination,
                "L6:Enchantment", Guids.OverdriveL6Enchantment,
                "L6:Evocation", Guids.OverdriveL6Evocation,
                "L6:Illusion", Guids.OverdriveL6Illusion,
                "L6:Necromancy", Guids.OverdriveL6Necromancy,
                "L6:Transmutation", Guids.OverdriveL6Transmutation,
                "L6:None", Guids.OverdriveL6None,
        }, Variants.Select(v => VariantId(v.Key, v.Value)).ToArray(), "overdrive variant");

        static readonly ReadOnlyDictionary<string, string> EffectGuids = Table(new[]
        {
                "Abjuration", Guids.OverdriveEffectAbjuration,
                "Conjuration", Guids.OverdriveEffectConjuration,
                "Divination", Guids.OverdriveEffectDivination,
                "Enchantment", Guids.OverdriveEffectEnchantment,
                "Evocation", Guids.OverdriveEffectEvocation,
                "Illusion", Guids.OverdriveEffectIllusion,
                "Necromancy", Guids.OverdriveEffectNecromancy,
                "Transmutation", Guids.OverdriveEffectTransmutation,
        }, Schools.ToArray(), "overdrive school effect");

        static ReadOnlyDictionary<string, string> Table(string[] kv, string[] wantedKeys, string what)
        {
            if (kv.Length % 2 != 0) throw new InvalidOperationException("OverdriveRules: odd " + what + " GUID list");
            var d = new Dictionary<string, string>();
            for (int i = 0; i < kv.Length; i += 2) d.Add(kv[i], kv[i + 1]); // 중복 키 → ArgumentException
            if (!new HashSet<string>(d.Keys).SetEquals(wantedKeys) || d.Count != wantedKeys.Length)
                throw new InvalidOperationException("OverdriveRules: " + what + " GUID table keys (" + string.Join(",", d.Keys.ToArray()) + ") != needed (" + string.Join(",", wantedKeys) + ")");
            if (d.Values.Distinct().Count() != d.Count) throw new InvalidOperationException("OverdriveRules: " + what + " GUID listed twice");
            return new ReadOnlyDictionary<string, string>(d);
        }

        public static string VariantGuid(int slotLevel, string school) { return VariantGuids[VariantId(slotLevel, school)]; }
        public static IEnumerable<string> AllVariantGuids { get { return Variants.Select(v => VariantGuid(v.Key, v.Value)); } }

        // 학파 효과 버프 GUID(여덟 학파만 — 학파 없음에 물으면 던진다).
        public static string EffectGuid(string school)
        {
            if (EffectOrNull(school) == null) throw new InvalidOperationException("OverdriveRules.EffectGuid: " + school + " has no effect buff");
            return EffectGuids[school];
        }
        public static IEnumerable<string> AllEffectGuids { get { return Schools.Select(EffectGuid); } }

        // ---------------- 판정 ----------------
        // 판정 순서: 결측 레벨 → 폭주 없는 레벨(능력이 있을 수 없다 = 모순 관측) → 이미 폭주 → 골렘 없음 → 부서짐 → 합체 전이 중 → 추출물 없음.
        public static OverdriveBlock Decide(OverdriveInputs x)
        {
            if (x == null) throw new ArgumentNullException("x");
            RepairRules.RequireSlotLevel(x.SlotLevel);
            RequireChoice(x.School, "Decide");
            if (!x.MasterLevel.HasValue) return OverdriveBlock.Unknown;
            if (x.MasterLevel.Value < GolemRules.MinLevel || x.MasterLevel.Value > GolemRules.MaxLevel) return OverdriveBlock.Unknown;
            if (!SlamRules.HasOverdrive(x.MasterLevel.Value)) return OverdriveBlock.Unknown;
            if (!x.Active.HasValue) return OverdriveBlock.Unknown;
            if (x.Active.Value) return OverdriveBlock.AlreadyActive;
            if (!x.Life.HasValue) return OverdriveBlock.Unknown;
            if (x.Life.Value != LifecycleState.Ready && x.Life.Value != LifecycleState.Stale) return OverdriveBlock.NoGolem;
            if (!x.GolemBound.HasValue) return OverdriveBlock.Unknown;
            if (!x.GolemBound.Value) return OverdriveBlock.NoGolem;
            if (!x.GolemDead.HasValue || !x.Broken.HasValue) return OverdriveBlock.Unknown;
            if (x.GolemDead.Value || x.Broken.Value) return OverdriveBlock.GolemBroken;
            if (!x.Fusion.HasValue) return OverdriveBlock.Unknown;
            if (x.Fusion.Value != FusionState.Idle && x.Fusion.Value != FusionState.Active) return OverdriveBlock.FusionBusy;
            if (!x.SpendableSlots.HasValue || x.SpendableSlots.Value < 0) return OverdriveBlock.Unknown;
            if (x.SpendableSlots.Value == 0) return OverdriveBlock.NoSlot;
            return OverdriveBlock.None;
        }

        // 지속(라운드) = 소모한 추출물 레벨 × Dials.OverdriveRoundsPerExtractLevel.
        public static int DurationRounds(int slotLevel)
        {
            RepairRules.RequireSlotLevel(slotLevel);
            if (GolemRules.Dials.OverdriveRoundsPerExtractLevel < 1) throw new InvalidOperationException("OverdriveRules: Dials.OverdriveRoundsPerExtractLevel " + GolemRules.Dials.OverdriveRoundsPerExtractLevel + " < 1");
            return slotLevel * GolemRules.Dials.OverdriveRoundsPerExtractLevel;
        }

        // 종료 판정 순서: 골렘 없음 → 부서짐. 결측이 먼저면 Unknown. (지속 만료는 엔진 버프 지속이 맡는다 — 여기 판정이 아니다.)
        public static OverdriveEnd DecideEnd(OverdriveEndInputs x)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (!x.GolemBound.HasValue) return OverdriveEnd.Unknown;
            if (!x.GolemBound.Value) return OverdriveEnd.GolemLost;
            if (!x.GolemDead.HasValue || !x.Broken.HasValue) return OverdriveEnd.Unknown;
            if (x.GolemDead.Value || x.Broken.Value) return OverdriveEnd.GolemBroken;
            return OverdriveEnd.Keep;
        }

        // 학파 효과 버프가 있어야 하는 유닛: 폭주 중이 아니면 어디에도 없음. 폭주 중이면 골렘에 항상(거둔 골렘은 버프가 꺼져 있다), 본체에는 합체 형태일 때만.
        // 학파 없음 = 효과 없음(null). 반환 = 그 유닛이 달고 있어야 하는 효과의 학파(null = 없음).
        public static string WantedEffect(bool overdrive, string school, bool fusedMaster, bool masterHasForm)
        {
            if (!overdrive) return null;
            RequireChoice(school, "WantedEffect");
            if (EffectOrNull(school) == null) return null;
            if (fusedMaster && !masterHasForm) return null;
            return school;
        }

        // 버프 하나 교정 계획(일반형 — 추가 사지 SlamRules.PlanLimbs 와 같은 규칙): 기대가 정확히 1개 있으면 나머지만 지우고, 아니면 전부 지우고 1개 더한다.
        public static LimbPlan PlanOne(IList<string> current, string wanted)
        {
            if (current == null) throw new ArgumentNullException("current");
            if (current.Any(c => c == null)) throw new ArgumentException("OverdriveRules.PlanOne: null id in the current set");
            int same = current.Count(c => c == wanted);
            if (wanted != null && same == 1) return new LimbPlan(current.Where(c => c != wanted).ToArray(), null);
            return new LimbPlan(current.ToArray(), wanted);
        }

        // 사유 → 문자열 키(StringsTable). None 에는 문구가 없다 → 던진다.
        public static string ReasonKey(OverdriveBlock b)
        {
            if (b == OverdriveBlock.None || !Enum.IsDefined(typeof(OverdriveBlock), b)) throw new ArgumentException("OverdriveRules.ReasonKey: " + b + " has no reason text");
            return KeyRoot + ".Block." + b;
        }

        // 제한 컴포넌트를 다는 사유 전부(None 제외) — 능력 변형이 사유마다 하나씩 단다(엔진은 처음 실패한 제한의 문구를 보여 준다).
        public static readonly ReadOnlyCollection<OverdriveBlock> Reasons = Array.AsReadOnly(
            Enum.GetValues(typeof(OverdriveBlock)).Cast<OverdriveBlock>().Where(b => b != OverdriveBlock.None).ToArray());

        // ---------------- 문구(표에서 생성 — 수치를 문구에 따로 적지 않는다, C4) ----------------
        static readonly ReadOnlyDictionary<string, KeyValuePair<string, string>> SchoolNames = new ReadOnlyDictionary<string, KeyValuePair<string, string>>(new Dictionary<string, KeyValuePair<string, string>>
        {
            { "Abjuration", new KeyValuePair<string, string>("방호", "Abjuration") }, { "Conjuration", new KeyValuePair<string, string>("소환", "Conjuration") },
            { "Divination", new KeyValuePair<string, string>("예지", "Divination") }, { "Enchantment", new KeyValuePair<string, string>("환혹", "Enchantment") },
            { "Evocation", new KeyValuePair<string, string>("방출", "Evocation") }, { "Illusion", new KeyValuePair<string, string>("환영", "Illusion") },
            { "Necromancy", new KeyValuePair<string, string>("사령", "Necromancy") }, { "Transmutation", new KeyValuePair<string, string>("변환", "Transmutation") },
            { NoSchool, new KeyValuePair<string, string>("학파 없음", "No School") },
        });

        public static KeyValuePair<string, string> SchoolName(string school)
        {
            RequireChoice(school, "SchoolName");
            return SchoolNames[school];
        }

        // 효과 한 줄(한, 영). 학파 없음 = 「추가 효과 없음」.
        public static KeyValuePair<string, string> EffectText(string school)
        {
            var r = EffectOrNull(school);
            if (r == null) return new KeyValuePair<string, string>("추가 효과 없음", "no additional effect");
            switch (r.Kind)
            {
                case SchoolEffectKind.Strength: return new KeyValuePair<string, string>("골렘의 힘 +" + r.A + "(강화 보너스)", "the golem's Strength +" + r.A + " (enhancement bonus)");
                case SchoolEffectKind.FastHealing: return new KeyValuePair<string, string>("골렘에게 빠른 치유 " + r.A, "the golem gains fast healing " + r.A);
                case SchoolEffectKind.ArmorClass: return new KeyValuePair<string, string>("골렘의 AC +" + r.A + "(갑옷 보너스)", "the golem's AC +" + r.A + " (armor bonus)");
                case SchoolEffectKind.AttackAndCritConfirm: return new KeyValuePair<string, string>("골렘의 명중 +" + r.A + "(통찰 보너스), 치명타 확정 굴림 +" + r.B, "the golem's attack rolls +" + r.A + " (insight bonus), critical confirmation rolls +" + r.B);
                case SchoolEffectKind.Concealment: return new KeyValuePair<string, string>("골렘에게 은폐 " + r.A + "%(골렘을 노린 공격의 " + r.A + "%가 빗나간다)", "the golem gains " + r.A + "% concealment (attacks against it miss " + r.A + "% of the time)");
                case SchoolEffectKind.SlamNegativeDice: return new KeyValuePair<string, string>("골렘의 슬램에 음에너지 피해 +" + r.A + "d" + r.B, "the golem's slams deal +" + r.A + "d" + r.B + " negative energy damage");
                case SchoolEffectKind.SlamShaken: return new KeyValuePair<string, string>("골렘의 슬램에 맞은 적이 " + r.A + "라운드 동안 동요한다(내성 없음, 정신 효과)", "enemies hit by the golem's slams are shaken for " + r.A + " round" + (r.A == 1 ? "" : "s") + " (no save, mind-affecting)");
                case SchoolEffectKind.SlamElectricityDice: return new KeyValuePair<string, string>("골렘의 슬램에 전기 피해 +" + r.A + "d" + r.B, "the golem's slams deal +" + r.A + "d" + r.B + " electricity damage");
            }
            throw new InvalidOperationException("OverdriveRules.EffectText: undefined kind " + r.Kind);
        }

        // 키 머리(리터럴을 쪼개 둔다 — 시험 `strings: every key literal` 은 「X.Y」 모양 리터럴을 완성된 키로 본다. 조립되는 키의 머리는 그 모양이 아니어야 한다).
        static readonly string KeyRoot = "Overdrive";

        public static string VariantNameKey(int slotLevel, string school) { VariantId(slotLevel, school); return KeyRoot + ".V.L" + slotLevel + "." + school + ".Name"; }
        public static string VariantDescriptionKey(string school) { RequireChoice(school, "VariantDescriptionKey"); return KeyRoot + ".V." + school + ".Description"; }
        public static string EffectNameKey(string school) { EffectGuid(school); return "OverdriveEffect." + school + ".Name"; }
        public static string EffectDescriptionKey(string school) { EffectGuid(school); return "OverdriveEffect." + school + ".Description"; }

        // 슬램 횟수 요약(표에서): 「5~10레벨 2회, 11~15레벨 3회, 16~20레벨 4회」.
        static KeyValuePair<string, string> CountSummary(bool overdrive)
        {
            var rows = GolemRules.Dials.SlamCounts;
            var ko = new List<string>(); var en = new List<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (overdrive && !rows[i].Overdrive.HasValue) continue;
                int n = overdrive ? rows[i].Overdrive.Value : rows[i].Normal;
                int j = i; // 같은 값이 이어지는 행은 한 구간으로 묶는다(평소 1~10레벨 1회)
                while (j + 1 < rows.Count && (overdrive ? rows[j + 1].Overdrive : rows[j + 1].Normal) == n) j++;
                int to = j + 1 < rows.Count ? rows[j + 1].FromLevel - 1 : GolemRules.MaxLevel;
                ko.Add(rows[i].FromLevel + "~" + to + "레벨 " + n + "회");
                en.Add(n + " at levels " + rows[i].FromLevel + "-" + to);
                i = j;
            }
            return new KeyValuePair<string, string>(string.Join(", ", ko.ToArray()), string.Join(", ", en.ToArray()));
        }

        public static KeyValuePair<string, string> NormalCountSummary { get { return CountSummary(false); } }
        public static KeyValuePair<string, string> OverdriveCountSummary { get { return CountSummary(true); } }

        // StringsTable 이 합치는 생성 문구 전부(키 중복 없음). 능력 설명 = 공통 + 학파 효과 표 요약, 변형 설명 = 그 학파의 효과 한 줄 + 공통.
        public static List<GeneratedString> GeneratedStrings()
        {
            var l = new List<GeneratedString>();
            var counts = OverdriveCountSummary;
            int per = GolemRules.Dials.OverdriveRoundsPerExtractLevel;
            string commonKo = "신속 행동으로 준비해 둔 추출물 하나를 골렘에게 주입한다. 추출물 레벨 × " + per + " 라운드 동안 골렘의 한 라운드 슬램 횟수가 늘어난다(연금술사 " + counts.Key
                + "). 합체 중이면 골렘술사의 슬램이 그 횟수가 되고(최소 " + GolemRules.Dials.FusionHandSlams + "회) 학파 효과도 골렘술사가 받는다. 반대 학파 추출물도 1개로 센다. 이미 폭주 중이면 쓸 수 없고, 골렘이 부서지면 즉시 끝난다.";
            string commonEn = "As a swift action, inject one prepared extract into the golem. For extract level x " + per + " rounds the golem makes more slam attacks each round (" + counts.Value
                + " by alchemist level). While fused the Golemancer's slams use that number instead (minimum " + GolemRules.Dials.FusionHandSlams + ") and the Golemancer receives the school effect. An opposition-school extract still counts as one. Cannot be used while an overdrive is running; it ends at once if the golem is broken.";
            var listKo = new List<string>(); var listEn = new List<string>();
            foreach (var s in SchoolChoices)
            {
                var n = SchoolName(s); var e = EffectText(s);
                listKo.Add(n.Key + " — " + e.Key); listEn.Add(n.Value + " - " + e.Value);
                l.Add(new GeneratedString(VariantDescriptionKey(s), "소모한 추출물의 학파가 " + n.Key + ": " + e.Key + ".\n\n" + commonKo, "Spent extract of the school " + n.Value + ": " + e.Value + ".\n\n" + commonEn));
                if (EffectOrNull(s) != null)
                {
                    l.Add(new GeneratedString(EffectNameKey(s), "골렘 폭주 — " + n.Key, "Golem Overdrive - " + n.Value));
                    l.Add(new GeneratedString(EffectDescriptionKey(s), "골렘 폭주가 이어지는 동안: " + e.Key + ".", "While the golem overdrive lasts: " + e.Value + "."));
                }
            }
            l.Add(new GeneratedString("Overdrive.Description",
                commonKo + "\n\n소모한 추출물의 학파에 따라 지속 동안 추가 효과가 붙는다: " + string.Join("; ", listKo.ToArray()) + ".",
                commonEn + "\n\nThe school of the spent extract adds an effect for the duration: " + string.Join("; ", listEn.ToArray()) + "."));
            foreach (var v in Variants)
            {
                var n = SchoolName(v.Value);
                l.Add(new GeneratedString(VariantNameKey(v.Key, v.Value), "골렘 폭주 — " + n.Key + " " + v.Key + "레벨", "Golem Overdrive - " + n.Value + ", level " + v.Key));
            }
            if (l.Select(x => x.Key).Distinct().Count() != l.Count) throw new InvalidOperationException("OverdriveRules.GeneratedStrings: duplicate key");
            return l;
        }
    }
}
