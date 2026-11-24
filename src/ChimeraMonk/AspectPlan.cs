using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    //  CRC 마다 본편 진행을 행으로 갖는다: 9종 = StartPlusDivStep(1, 7, max 20) 1개, 울버린 = HP Custom 20행(m_UseMax false) + Con StartPlusDivStep(1, 14, max 20).
    //  「한 단계 위」(판정 W2) = 티어 + 1: StartPlusDivStep → 순위 + 1, 울버린 HP 표 → L × 2 × (티어 + 1)(본편 표 = L × 2 × 티어, 티어 = 1 + (L − 1) / 7).
    public enum CrcKind { StartPlusDivStep, Custom }

    public sealed class CrcRow
    {
        public readonly string RankType, Descriptor, Stat;
        public readonly CrcKind Kind;
        public readonly int Start, Step, Max, Multiplier;
        public readonly int[] Table, BoostedTable; // Custom: 20행 본편 표 · 한 단계 위 표
        public CrcRow(string rankType, CrcKind kind, int start, int step, int max, int[] table, int[] boosted, string descriptor, string stat, int multiplier)
        { RankType = rankType; Kind = kind; Start = start; Step = step; Max = max; Table = table; BoostedTable = boosted; Descriptor = descriptor; Stat = stat; Multiplier = multiplier; }
    }

    public sealed class FormRow
    {
        public readonly string Key, FeatureGuid, AbilityGuid, BuffGuid, OurAbility, OurBuff, OurBoost, OurForm;
        public readonly IReadOnlyList<CrcRow> Crcs;
        public FormRow(string key, string feature, string ability, string buff, string ourAbility, string ourBuff, string ourBoost, string ourForm, IReadOnlyList<CrcRow> crcs)
        { Key = key; FeatureGuid = feature; AbilityGuid = ability; BuffGuid = buff; OurAbility = ourAbility; OurBuff = ourBuff; OurBoost = ourBoost; OurForm = ourForm; Crcs = crcs; }
    }

    // 복제 그래프 행(부록 A-h): 원본 → 우리 GUID(null = 원본 참조 유지), 덮어쓰기(AddFeatureOnClassLevel 의 (특성, 레벨, BeforeThisLevel) 목록 — 판정 W1).
    public sealed class GraphRow
    {
        public readonly string SourceGuid, OurGuid;
        public readonly IReadOnlyList<Tuple<string, int, bool>> Overrides;
        public GraphRow(string source, string ours, IReadOnlyList<Tuple<string, int, bool>> overrides) { SourceGuid = source; OurGuid = ours; Overrides = overrides; }
    }

    public sealed class GraphNode
    {
        public readonly bool ReadsClass;
        public readonly IReadOnlyList<string> Children;
        public GraphNode(bool readsClass, IReadOnlyList<string> children) { ReadsClass = readsClass; Children = children ?? new string[0]; }
    }

    public static class AspectPlan
    {
        public const string RankDamageDice = "DamageDice", RankDefault = "Default";

        // 복제 토글의 배타 그룹(엔진 ActivatableAbilityGroup 이름). 소유자 결정 2026-11-16 4번: 가진 형태(최대 3)를 전부 동시에 켤 수 있다.
        //  본편 형태 토글 10개는 Group = ShifterAspect(48) · WeightInGroup 1 이라 하나만 켜진다(정찰 8-a, 시험 jbp 가 확인). 본편 그룹 크기(IncreaseActivatableAbilityGroupSize)는 건드리지 않는다
        //  — 시프터 다중 직업 캐릭터의 본편 형태까지 여러 개 켜지는 부작용 방지. 복제본만 그룹에서 뺀다(None = 0, 그룹 검사 대상 아님).
        public const string ToggleGroup = "None";

        static int[] Table(Func<int, int> f) { return Enumerable.Range(ChimeraRules.Levels.MinLevel, ChimeraRules.Levels.MaxLevel).Select(f).ToArray(); }
        public static int Tier7(int l) { return 1 + Math.Max(0, (l - 1) / 7); } // 엔진 ApplyProgression case 3(StartPlusDivStep 1, 7) @005C~006D

        static CrcRow Step7(string descriptor, string stat, int mult) { return new CrcRow(RankDamageDice, CrcKind.StartPlusDivStep, 1, 7, 20, null, null, descriptor, stat, mult); }

        static FormRow F(string key, string feature, string ability, string buff, params CrcRow[] crcs)
        {
            Func<Type, string> g = t => (string)t.GetField(key).GetRawConstantValue();
            return new FormRow(key, feature, ability, buff, g(typeof(Guids.AspectAbility)), g(typeof(Guids.AspectBuff)), g(typeof(Guids.AspectBoost)), g(typeof(Guids.AspectForm)), crcs);
        }

        public static IReadOnlyList<FormRow> Forms
        {
            get
            {
                return new[]
                {
                    F("Bear", Guids.Game.AspectBear, Guids.Game.AspectBearAbility, Guids.Game.AspectBearBuff, Step7("Inherent", "Constitution", 2)),
                    F("Boar", Guids.Game.AspectBoar, Guids.Game.AspectBoarAbility, Guids.Game.AspectBoarBuff, Step7("Inherent", "SaveFortitude", 2)),
                    F("Dinosaur", Guids.Game.AspectDinosaur, Guids.Game.AspectDinosaurAbility, Guids.Game.AspectDinosaurBuff, Step7("UntypedStackable", "Initiative", 2)),
                    F("Elephant", Guids.Game.AspectElephant, Guids.Game.AspectElephantAbility, Guids.Game.AspectElephantBuff, Step7("Inherent", "Strength", 2)),
                    F("Horse", Guids.Game.AspectHorse, Guids.Game.AspectHorseAbility, Guids.Game.AspectHorseBuff, Step7("Inherent", "Speed", 5)),
                    F("Lizard", Guids.Game.AspectLizard, Guids.Game.AspectLizardAbility, Guids.Game.AspectLizardBuff, Step7("Inherent", "SaveReflex", 2)),
                    F("Spider", Guids.Game.AspectSpider, Guids.Game.AspectSpiderAbility, Guids.Game.AspectSpiderBuff, Step7("Dodge", "AC", 2)),
                    F("Tiger", Guids.Game.AspectTiger, Guids.Game.AspectTigerAbility, Guids.Game.AspectTigerBuff, Step7("Inherent", "Dexterity", 2)),
                    F("Wolf", Guids.Game.AspectWolf, Guids.Game.AspectWolfAbility, Guids.Game.AspectWolfBuff, Step7("UntypedStackable", "AdditionalDamage", 2)),
                    F("Wolverine", Guids.Game.AspectWolverine, Guids.Game.AspectWolverineAbility, Guids.Game.AspectWolverineBuff,
                        new CrcRow(RankDamageDice, CrcKind.Custom, 0, 0, 0, Table(l => l * 2 * Tier7(l)), Table(l => l * 2 * (Tier7(l) + 1)), "UntypedStackable", "HitPoints", 1),
                        new CrcRow(RankDefault, CrcKind.StartPlusDivStep, 1, 14, 20, null, null, "Racial", "Constitution", 2)),
                };
            }
        }

        public static FormRow Form(string key)
        {
            var hit = Forms.Where(f => f.Key == key).ToArray();
            if (hit.Length != 1) throw new ArgumentException("ChimeraMonk: AspectPlan has " + hit.Length + " form(s) " + key, "key");
            return hit[0];
        }

        // 순위(엔진 ApplyProgression): StartPlusDivStep = min(max, 1 + max(0, (L − start) / step)); 한 단계 위 = 순위 + 1(W2).
        public static int Rank(CrcRow c, int level, bool boosted)
        {
            if (c.Kind != CrcKind.StartPlusDivStep) throw new InvalidOperationException("ChimeraMonk: Rank on a custom-table CRC");
            int r = Math.Min(c.Max, 1 + Math.Max(0, (level - c.Start) / c.Step));
            return boosted ? r + 1 : r;
        }

        // 그 CRC 가 주는 보너스 값(배수 포함).
        public static int Value(CrcRow c, int level, bool boosted)
        {
            if (level < ChimeraRules.Levels.MinLevel || level > ChimeraRules.Levels.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "ChimeraMonk: aspect level must be 1..20");
            if (c.Kind == CrcKind.Custom) return c.Multiplier * (boosted ? c.BoostedTable : c.Table)[level - 1];
            return c.Multiplier * Rank(c, level, boosted);
        }

        public static int Value(FormRow f, int crcIndex, int level, bool boosted) { return Value(f.Crcs[crcIndex], level, boosted); }

        // 강화 버프의 20행 표(배수 1): Replace = 한 단계 위 값, Add = 차이(BoostPlan 모양은 빌드 시 엔진 IsStackable).
        public static int[] BoostTable(CrcRow c, BoostShape shape)
        {
            return Table(l => shape == BoostShape.Replace ? Value(c, l, true) : Value(c, l, true) - Value(c, l, false));
        }

        // 복제 그래프(부록 A-h, 판정 W1): 늑대 냄새 상위만 우리 것으로 복제(클래스 읽기 2 + 덮어쓰기), 잎(Scent10/20/30·Diehard·Endurance)은 원본 참조.
        public static IReadOnlyList<GraphRow> Graph
        {
            get
            {
                return new[]
                {
                    new GraphRow(Guids.Game.WolfScentUpgrade, Guids.AspectWolfScentUpgrade, new[]
                    {
                        Tuple.Create(Guids.Game.WolfScent20, 15, true),  // 8~14 (본편과 같음)
                        Tuple.Create(Guids.Game.WolfScent30, 15, false), // 15+ (판정 W1 — 본편은 true 라 15레벨에 냄새 소실)
                    }),
                };
            }
        }

        // 생산 그래프 순회(I04): 루트에서 깊이 우선으로 자식을 따라가 「클래스를 읽거나 복제할 자손을 가진」 블루프린트를 복제 목록에 넣는다(루트 포함, 방문 순서).
        // 블루프린트가 없으면 report("builder", …) 후 던진다(건너뛰지 않음, C1). 반환 = 복제할 GUID 목록(잎은 원본 참조로 남는다).
        public static List<string> RetargetGraph(string root, Func<string, GraphNode> resolve, Action<string, string> report)
        {
            if (root == null || resolve == null || report == null) throw new ArgumentNullException("root/resolve/report");
            var order = new List<string>();
            var visiting = new HashSet<string>();
            Func<string, bool> walk = null;
            walk = g =>
            {
                if (!visiting.Add(g)) return order.Contains(g);
                var n = resolve(g);
                if (n == null) { report(BuildPlan.BuilderSite, "aspect graph: blueprint " + g + " not found"); throw new InvalidOperationException("ChimeraMonk: aspect graph blueprint " + g + " not found"); }
                int at = order.Count;
                bool any = false;
                foreach (var c in n.Children) any |= walk(c);
                bool clone = n.ReadsClass || any || g == root;
                if (clone) order.Insert(at, g);
                return clone;
            };
            walk(root);
            return order;
        }
    }
}
