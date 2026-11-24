using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock
{
     
    public static class RemoveRule
    {
        // 제거 13쌍 = 설계 §4.10 「워락이 제거하는 것」: L1 WitchHexSelection·WitchCantripsFeature·WitchBetterHexProgression + L2·4·…·20 WitchHexSelection.
        // 주술 자리 = WarlockRules.Levels.HexLevels(11). jbp 시험 `jbp: removed pairs` 가 WitchProgression 과 대조한다.
        public static readonly Tuple<int, string>[] Pairs = BuildPairs();

        static Tuple<int, string>[] BuildPairs()
        {
            var list = new List<Tuple<int, string>>
            {
                Tuple.Create(1, Guids.Game.WitchHexSelection),
                Tuple.Create(1, Guids.Game.WitchCantripsFeature),
                Tuple.Create(1, Guids.Game.WitchBetterHexProgression),
            };
            list.AddRange(WarlockRules.Levels.HexLevels.Where(l => l != 1).Select(l => Tuple.Create(l, Guids.Game.WitchHexSelection)));
            return list.ToArray();
        }

        // 진행표 L20 항목 하나(게임 쪽 T2 Archetype.Build 가 LevelEntries 를 읽어 평평하게 넣는다).
        // TypeFullName = 해석된 블루프린트의 실제 형식(GetType().FullName) — 알려진 다른 모드 항목 규칙이 GUID 와 함께 대조한다. 비면 판정 불가 → 던진다.
        // Kind/Children = 주술 운반 판정(CarriesHex)의 뿌리 노드 재료. Children 이 null 인 선택지·진행표는 판정 불가 → PlanL20 이 던진다(생성자는 받기만 한다).
        public sealed class L20Item
        {
            public readonly string Guid;
            public readonly string TypeFullName;
            public readonly HexNodeKind Kind;
            public readonly string[] Children; // 선택지 = m_AllFeatures · 진행표 = LevelEntries 전 레벨 특성 · 특성 = null/빈 배열
            public bool IsSelection { get { return Kind == HexNodeKind.Selection; } }
            public string[] AllFeatures { get { return IsSelection ? Children : null; } } // IsSelection 일 때 m_AllFeatures(없으면 판정 불가 → PlanL20 이 던진다)
            public L20Item(string guid, string typeFullName, bool isSelection, string[] allFeatures)
                : this(guid, typeFullName, isSelection ? HexNodeKind.Selection : HexNodeKind.Feature, allFeatures) { }
            public L20Item(string guid, string typeFullName, HexNodeKind kind, string[] children)
            {
                if (string.IsNullOrEmpty(typeFullName)) throw new ArgumentException("Warlock: L20 item " + (guid ?? "null") + " without a blueprint type name (undeterminable)", "typeFullName");
                if (!Enum.IsDefined(typeof(HexNodeKind), kind)) throw new ArgumentOutOfRangeException("kind", kind, "Warlock: unknown HexNodeKind");
                Guid = guid; TypeFullName = typeFullName; Kind = kind; Children = children;
            }
        }

        // ---- 주술 운반(hex carrier) — 한 곳의 정의(C4). PlanL20 · 게임 쪽 S1/S2 신탁(Checks\Scenarios) 이 모두 이것을 쓴다. ----
        // carrier(n) = n.Guid == WitchHexSelection ∨ ∃ c ∈ children(n): carrier(resolve(c)).
        // children: 선택지 = m_AllFeatures, 진행표 = LevelEntries 전 레벨의 특성, 그 밖의 특성 = 없음. 주술 자신은 펼치지 않는다(찾았으니 끝).
        public enum HexNodeKind { Feature, Selection, Progression }

        public sealed class HexNode
        {
            public readonly string Guid;
            public readonly HexNodeKind Kind;
            public readonly string[] Children;
            public HexNode(string guid, HexNodeKind kind, string[] children)
            {
                if (string.IsNullOrEmpty(guid)) throw new ArgumentException("Warlock: hex node without a GUID", "guid");
                if (!Enum.IsDefined(typeof(HexNodeKind), kind)) throw new ArgumentOutOfRangeException("kind", kind, "Warlock: unknown HexNodeKind");
                if (kind == HexNodeKind.Feature)
                {
                    if (children != null && children.Length > 0) throw new ArgumentException("Warlock: plain feature " + guid + " given children (only selections/progressions nest)", "children");
                    children = new string[0];
                }
                else if (children == null) throw new ArgumentException("Warlock: " + kind + " " + guid + " without its children list (undeterminable)", "children");
                if (children.Any(string.IsNullOrEmpty)) throw new ArgumentException("Warlock: " + kind + " " + guid + " has a null/empty child GUID (unresolved entry)", "children");
                Guid = guid; Kind = kind; Children = children;
            }
        }

        // 펼칠 수 있는 중첩 깊이(뿌리 = 0). 이 깊이의 노드가 자식을 가지면 던진다(판정 불가 — 「주술 없음」으로 접지 않는다).
        // 왜 16: blueprints.zip 전수 측정(2026-11-14, BlueprintFeatureSelection 460 + BlueprintProgression 633 을 각각 뿌리로 이 알고리즘 그대로 —
        // 방문 집합·주술 단락 포함) 자식 있는 노드의 최대 깊이 = 7(뿌리 87bc9abf… → … → 39fd1f34…). S2 신탁은 유닛의 모든 선택지를 뿌리로 쓰므로 본편만으로 7 까지 간다.
        // 16 = 본편 7 의 2배 이상 + 다른 모드 여유(작성자 선택값 — 넘으면 빌드·첫 프레임 재검·S1/S2 가 시끄럽게 실패). L20 의 TTT 사슬 = 대체 캡스톤(0) → PerfectBodyFlawlessMindProgression(1) → 특성(2).
        public const int HexCarrierMaxDepth = 16;

        // resolve(guid) = 그 GUID 의 노드(못 찾으면 null 이 아니라 던져야 하지만, null 이 와도 여기서 던진다). 순환은 방문 집합으로 끊는다(이미 본 노드 = 그 노드의 결과는 이미 반영됨/계산 중).
        public static bool CarriesHex(HexNode root, Func<string, HexNode> resolve)
        {
            if (root == null) throw new ArgumentNullException("root");
            if (resolve == null) throw new ArgumentNullException("resolve");
            return CarriesHexAt(root, resolve, new HashSet<string>(), 0);
        }

        static bool CarriesHexAt(HexNode n, Func<string, HexNode> resolve, HashSet<string> visited, int depth)
        {
            if (n.Guid == Guids.Game.WitchHexSelection) return true;
            if (!visited.Add(n.Guid)) return false; // 순환·마름모: 이 노드는 이미 펼쳤거나 펼치는 중이다
            if (n.Children.Length == 0) return false;
            if (depth >= HexCarrierMaxDepth) throw new InvalidOperationException("Warlock: hex-carrier check: " + n.Kind + " " + n.Guid + " at nesting depth " + depth + " has children — deeper than HexCarrierMaxDepth " + HexCarrierMaxDepth + " (undeterminable)");
            foreach (var c in n.Children)
            {
                if (c == Guids.Game.WitchHexSelection) return true;
                var cn = resolve(c);
                if (cn == null) throw new InvalidOperationException("Warlock: hex-carrier check: child " + c + " of " + n.Guid + " did not resolve (undeterminable)");
                if (cn.Guid != c) throw new InvalidOperationException("Warlock: hex-carrier check: resolve(" + c + ") returned node " + cn.Guid);
                if (CarriesHexAt(cn, resolve, visited, depth + 1)) return true;
            }
            return false;
        }

        // Remove = 제거(주술·주술 운반) · Unknown = 모르는 항목(게임 쪽 ERROR, 제거 안 함) · Kept = 알려진 다른 모드 항목(주술 없음 확인, 보존 — 첫 프레임 동기화 줄에 INFO 1회)
        // · Contradicted = 알려진 규칙의 GUID 인데 주술을 품음(규칙이 낡음 — Remove 에도 들어가고 게임 쪽 ERROR).
        public sealed class L20Plan
        {
            public readonly string[] Remove, Unknown, Kept, Contradicted;
            public L20Plan(string[] remove, string[] unknown, string[] kept, string[] contradicted) { Remove = remove; Unknown = unknown; Kept = kept; Contradicted = contradicted; }
        }

        // 다른 모드가 위치 진행표 L20 에 더하는 것으로 확인된 항목(PrestigeRule.ForeignRules 의 COP 규칙과 같은 모양). 처분 = 보존(주술 누수가 아니다).
        public sealed class L20ForeignRule
        {
            public readonly string Guid, TypeFullName, Name, Mod, Source, Reason;
            public L20ForeignRule(string guid, string typeFullName, string name, string mod, string source, string reason)
            {
                foreach (var kv in new[] { Tuple.Create("guid", guid), Tuple.Create("typeFullName", typeFullName), Tuple.Create("name", name), Tuple.Create("mod", mod), Tuple.Create("source", source), Tuple.Create("reason", reason) })
                    if (string.IsNullOrEmpty(kv.Item2)) throw new ArgumentException("L20ForeignRule: " + kv.Item1 + " is empty");
                Guid = guid; TypeFullName = typeFullName; Name = name; Mod = mod; Source = source; Reason = reason;
            }
        }

        // 엔진 선택지 형식(TTT IL CreateBlueprint<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection> — 시험 `jbp: engine type names` 가 Assembly-CSharp 에 있는지 잠근다).
        public const string FeatureSelectionType = "Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection";
        // TabletopTweaks-Base 2.6.18 의 WitchAlternateCapstone GUID: Mods\TabletopTweaks-Base\UserSettings\Blueprints.json:1411 "WitchAlternateCapstone": "c198bdba-5ad8-44ef-baee-61c698619af8".
        public const string TttWitchAltSelection = "c198bdba5ad844efbaee61c698619af8";

        // 알려진 것 밖의 L20 항목은 Unknown(ERROR) 그대로. GUID 가 같아도 형식이 다르거나 선택지가 아니면 Unknown, 주술을 품으면 Remove + Contradicted.
        public static readonly L20ForeignRule[] L20ForeignRules =
        {
            new L20ForeignRule(TttWitchAltSelection, FeatureSelectionType, "WitchAlternateCapstone", "TabletopTweaks-Base 2.6.18",
                "TabletopTweaks-Base.dll NewContent.AlternateCapstones.Witch::AddAlternateCapstones @0045-0060 CreateBlueprint<BlueprintFeatureSelection>(\"WitchAlternateCapstone\") -> <>c__DisplayClass1_0::<AddAlternateCapstones>b__1 @006B-0082 AddFeatures(Generic.PerfectBodyFlawlessMindProgression, Generic.GreatBeastMasterFeature) (WitchHexSelection only as icon @0030-003A); Bugfixes.Classes.Witch/Witch_AlternateCapstone_Patch (HarmonyPriority 0) adds it to WitchProgression L20 (recon §7)",
                "TTT's alternate witch capstone offers generic capstones, not hexes — no hex leak; kept so the warlock keeps TTT's capstone choice"),
        };

        // 주술 운반(CarriesHex — 중첩 선택지·진행표까지 재귀) → Remove(알려진 규칙의 GUID 여도 — 그때는 Contradicted 에도: 「주술 없음」이 깨졌으니 시끄럽게).
        // 주술 없음 + 알려진 규칙(GUID·형식·선택지) → Kept. 그 밖 → Unknown(게임 쪽 ERROR 목록).
        // 비어 있으면 던진다(위치 진행표 L20 에는 주술이 있어야 한다 — 없으면 아키타입 빌드 실패가 맞다). resolve = 중첩 자식 GUID → 노드(게임 쪽 Archetype.ResolveHexNode).
        public static L20Plan PlanL20(L20Item[] items, Func<string, HexNode> resolve)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (resolve == null) throw new ArgumentNullException("resolve");
            if (items.Length == 0) throw new ArgumentException("Warlock: WitchProgression L20 has no entries (expected WitchHexSelection)", "items");
            var remove = new List<string>();
            var unknown = new List<string>();
            var kept = new List<string>();
            var contradicted = new List<string>();
            foreach (var it in items)
            {
                if (it == null || it.Guid == null) throw new ArgumentException("Warlock: PlanL20 got a null item / GUID", "items");
                if (it.IsSelection && it.AllFeatures == null) throw new ArgumentException("Warlock: L20 selection " + it.Guid + " without m_AllFeatures (undeterminable)", "items");
                bool carriesHex = CarriesHex(new HexNode(it.Guid, it.Kind, it.Children), resolve);
                var rule = ForeignRuleOf(it.Guid);
                if (carriesHex)
                {
                    remove.Add(it.Guid);
                    if (rule != null) contradicted.Add(it.Guid);
                }
                else if (rule != null && it.IsSelection && it.TypeFullName == rule.TypeFullName) kept.Add(it.Guid);
                else unknown.Add(it.Guid);
            }
            return new L20Plan(remove.ToArray(), unknown.ToArray(), kept.ToArray(), contradicted.ToArray());
        }

        // GUID 의 알려진 규칙(없으면 null — 「모르는 항목」은 정상 결과다). 같은 GUID 규칙이 둘이면 표 결함 → 던진다.
        public static L20ForeignRule ForeignRuleOf(string guid)
        {
            var hit = L20ForeignRules.Where(r => r.Guid == guid).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("Warlock: L20ForeignRules lists " + guid + " " + hit.Length + " times");
            return hit.Length == 1 ? hit[0] : null;
        }

        // 계획(Remove) 중 아키타입 RemoveFeatures L20 에 아직 없는 것만, 계획 순서대로(게임 쪽 Archetype.ApplyL20Plan 이 이것만 RemoveAt(20, …) 한다 — 기존 항목 중복 없음).
        // 이미 있는 목록에 중복이 있거나 계획에 중복이 있으면 던진다(되읽기가 "정확히 1회"를 단언하므로 중복은 앞 단계의 결함이다).
        public static string[] NewL20Removals(string[] alreadyRemovedAt20, L20Plan plan)
        {
            if (alreadyRemovedAt20 == null) throw new ArgumentNullException("alreadyRemovedAt20");
            if (plan == null || plan.Remove == null || plan.Unknown == null || plan.Kept == null || plan.Contradicted == null) throw new ArgumentNullException("plan");
            if (alreadyRemovedAt20.Any(g => g == null) || alreadyRemovedAt20.Distinct().Count() != alreadyRemovedAt20.Length) throw new ArgumentException("Warlock: archetype RemoveFeatures L20 has a null or duplicate GUID", "alreadyRemovedAt20");
            if (plan.Remove.Any(g => g == null) || plan.Remove.Distinct().Count() != plan.Remove.Length) throw new ArgumentException("Warlock: L20 plan has a null or duplicate GUID", "plan");
            return plan.Remove.Where(g => !alreadyRemovedAt20.Contains(g)).ToArray();
        }

         
        // kept = 보존한 알려진 다른 모드 항목(부팅마다 이 한 줄로만 INFO — 빌드 시점 ApplyL20Plan 은 Kept 를 쓰지 않는다). 규칙에 없는 GUID 는 던진다.
        public static string L20SyncLine(string[] added, string[] unknown, string[] kept)
        {
            if (added == null) throw new ArgumentNullException("added");
            if (unknown == null) throw new ArgumentNullException("unknown");
            if (kept == null) throw new ArgumentNullException("kept");
            var k = new List<string>(); // 반복문(닫힘 람다 없음)
            foreach (var g in kept)
            {
                var r = ForeignRuleOf(g);
                if (r == null) throw new ArgumentException("Warlock: L20 kept entry " + (g ?? "null") + " has no known foreign rule", "kept");
                k.Add(g + " (" + r.Mod + " " + r.Name + ", no hex)");
            }
            string u = "unknown [" + string.Join(",", unknown) + "], kept [" + string.Join(", ", k.ToArray()) + "]";
            return added.Length == 0 ? "Warlock L20 sync: no change, " + u : "Warlock L20 sync: +" + added.Length + " removed [" + string.Join(",", added) + "], " + u;
        }

        // L20 첫 프레임 재검의 게이트(WSS ProgressionSync.cs:25-43 선례를 순수 판정으로). 게임 쪽 L20Sync.Tick 이 매 프레임 부르고 결과를 따른다.
        //   이미 판정함(NotAttempted 아님) → Idle(한 번만) · BuildAttempted 아님 → Wait(아직 모름, 로그 없음) · BuildFailed → ReportBuildFailed(ERROR 1회) · 아니면 Run.
        //   BuildAttempted=false 인데 BuildFailed=true 는 공용 ModContext 가 만들 수 없는 조합 → 던진다(판정 불가를 Wait 로 접지 않는다).
        public static L20Gate GateL20(L20SyncState state, bool buildAttempted, bool buildFailed)
        {
            if (!Enum.IsDefined(typeof(L20SyncState), state)) throw new ArgumentOutOfRangeException("state", state, "Warlock: unknown L20SyncState");
            if (!buildAttempted && buildFailed) throw new InvalidOperationException("Warlock: BuildFailed without BuildAttempted (impossible ModContext state)");
            if (state != L20SyncState.NotAttempted) return L20Gate.Idle;
            if (!buildAttempted) return L20Gate.Wait;
            if (buildFailed) return L20Gate.ReportBuildFailed;
            return L20Gate.Run;
        }
    }

    // L20 첫 프레임 재검 상태(계획 T2 Interfaces 「L20Sync」). NotAttempted 는 성공이 아니다 — 점검 러너 BlueprintsBuilt 는 Done 만 받는다(시험 `wiring: BlueprintsBuilt reads L20Sync.State == Done`).
    public enum L20SyncState { NotAttempted, BuildFailed, Done, SyncFailed }

    public enum L20Gate { Wait, ReportBuildFailed, Run, Idle }
}
