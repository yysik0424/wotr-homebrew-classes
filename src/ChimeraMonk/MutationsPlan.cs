using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    // 행 = ChimeraRules.Mutations(설계 §7 표 + 2026-11-16 소유자 결정) + 우리 GUID + 중첩 선택(브레스 → 원소 4, 부분 변환 단계마다 → 형태 10).
    public sealed class MutationRow
    {
        public readonly string Id, Guid, Requires, RequiresGuid;
        public readonly int GateLevel, Ranks;
        public readonly MutationKind Kind;
        public readonly IReadOnlyList<NestedOption> Nested; // null = 중첩 선택 아님
        public MutationRow(string id, string guid, int gate, string requires, string requiresGuid, MutationKind kind, IReadOnlyList<NestedOption> nested)
        { Id = id; Guid = guid; GateLevel = gate; Requires = requires; RequiresGuid = requiresGuid; Ranks = 1; Kind = kind; Nested = nested; }
        public bool IsNestedSelection { get { return Nested != null; } }
    }

    public sealed class NestedOption
    {
        public readonly string Key, Guid, StringKey; // StringKey = 표시 문구 키(StringsTable 의 "<StringKey>.name" / ".desc")
        public NestedOption(string key, string guid, string stringKey)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(stringKey)) throw new ArgumentException("ChimeraMonk: nested option without key / guid / string key");
            Key = key; Guid = guid; StringKey = stringKey;
        }
    }

    // 폐지된 변이 한 줄(소유자 결정 2026-11-17). 선택지가 아니다 — 같은 GUID 의 블루프린트만 남는다(효과 부품 없음 · 숨김). StringKey = 표시 문구 키("<StringKey>.name" / ".desc").
    public sealed class RetiredRow
    {
        public readonly string Id, Guid, StringKey;
        public RetiredRow(string id, string guid, string stringKey)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(stringKey)) throw new ArgumentException("ChimeraMonk: retired mutation row without id / guid / string key");
            Id = id; Guid = guid; StringKey = stringKey;
        }
    }

    // 선행 조건 표의 한 줄 = 우리 특성 하나(변이 · 중첩 선택 · 선택지)가 블루프린트에 갖는 선행 조건 전부.
    //  MinMonkLevel(0 = 없음) → PrerequisiteClassLevel{Monk} · RequiresGuid(null = 없음) → PrerequisiteFeature · Forbid → 원소마다 PrerequisiteNoFeature.
    public sealed class PrereqRow
    {
        public readonly string Guid, Label, RequiresGuid;
        public readonly int MinMonkLevel;
        public readonly IReadOnlyList<string> Forbid;
        public PrereqRow(string guid, string label, int minMonkLevel, string requiresGuid, IReadOnlyList<string> forbid)
        {
            if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(label)) throw new ArgumentException("ChimeraMonk: prerequisite row without guid / label");
            if (minMonkLevel < 0 || forbid == null) throw new ArgumentException("ChimeraMonk: prerequisite row " + label + " with a negative level or a null forbid list");
            Guid = guid; Label = label; MinMonkLevel = minMonkLevel; RequiresGuid = requiresGuid; Forbid = forbid;
        }
    }

    public static class MutationsPlan
    {
        // 판정 D4(작성자 결정, 계획 「판정 기록」): 화염·냉기·산·전기.
        public static readonly IReadOnlyList<NestedOption> BreathElements = new[]
        {
            Element("Fire", Guids.BreathElement.Fire), Element("Cold", Guids.BreathElement.Cold),
            Element("Acid", Guids.BreathElement.Acid), Element("Electricity", Guids.BreathElement.Electricity),
        };

        static NestedOption Element(string key, string guid) { return new NestedOption(key, guid, "mut.Breath." + key); }

        // 시프터 소형 형태 10종(부록 A-b 표 순서, 판정 D6 = 울버린 포함 10종 유지).
        public static readonly IReadOnlyList<string> AspectFormKeys = new[] { "Bear", "Boar", "Dinosaur", "Elephant", "Horse", "Lizard", "Spider", "Tiger", "Wolf", "Wolverine" };

        // 부분 변환 단계 n 의 형태 선택지 10(소유자 결정 2026-11-16 4번). 단계마다 선택지 특성이 따로다(GUID = Guids.AspectForm / AspectForm2 / AspectForm3 — 단계 번호에서 형식 이름을 파생, 없으면 던짐).
        // 표시 문구는 단계와 무관하게 "mut.Aspect.<형태>" 한 벌(C4), 주는 능력도 같은 복제 토글(Aspects.Build).
        public static IReadOnlyList<NestedOption> AspectStageForms(int stage)
        {
            if (!ChimeraRules.AspectStages.Any(s => s.Number == stage)) throw new ArgumentOutOfRangeException("stage", stage, "ChimeraMonk: no partial aspect stage " + stage);
            string typeName = "AspectForm" + (stage == 1 ? "" : stage.ToString());
            var t = typeof(Guids).GetNestedType(typeName);
            if (t == null) throw new InvalidOperationException("ChimeraMonk: no Guids." + typeName + " for partial aspect stage " + stage);
            return AspectFormKeys.Select(k =>
            {
                var f = t.GetField(k);
                if (f == null || !f.IsLiteral) throw new InvalidOperationException("ChimeraMonk: no Guids." + typeName + "." + k);
                return new NestedOption(k, (string)f.GetRawConstantValue(), "mut.Aspect." + k);
            }).ToArray();
        }

        // 첫 단계(부분 변환)의 형태 선택지 — 기존 이름 유지.
        public static IReadOnlyList<NestedOption> AspectForms { get { return AspectStageForms(1); } }

        public static string GuidOf(string id)
        {
            var f = typeof(Guids.Mutation).GetField(id);
            if (f == null) throw new ArgumentException("ChimeraMonk: no Guids.Mutation." + id, "id");
            return (string)f.GetRawConstantValue();
        }

        static IReadOnlyList<NestedOption> NestedOf(string id)
        {
            if (id == "Breath") return BreathElements;
            var stage = ChimeraRules.AspectStages.Where(s => s.Id == id).ToArray();
            return stage.Length == 1 ? AspectStageForms(stage[0].Number) : null;
        }

        public static IReadOnlyList<MutationRow> Rows()
        {
            return ChimeraRules.Mutations.Select(m => new MutationRow(m.Id, GuidOf(m.Id), m.GateLevel, m.Requires, m.Requires == null ? null : GuidOf(m.Requires), m.Kind, NestedOf(m.Id))).ToArray();
        }

        // 폐지된 변이(ChimeraRules.RetiredMutations)의 행. GUID = Guids.Retired.Mutation<Id>(없으면 던짐 — 폐지 목록에 올리고 GUID 를 안 남기면 세이브의 사실이 블루프린트를 잃는다).
        //  선택지 행(Rows)·선행 조건 표(Prerequisites)에는 넣지 않는다 → 선택 화면·CanPick·MutationPicks 어디에서도 고를 수 없다(Row(id) 가 던진다).
        public static IReadOnlyList<RetiredRow> RetiredRows()
        {
            return ChimeraRules.RetiredMutations.Select(id =>
            {
                string constName = "Mutation" + id; // 우리 상수 이름(게임 멤버가 아니다)
                var f = typeof(Guids.Retired).GetField(constName);
                if (f == null || !f.IsLiteral) throw new InvalidOperationException("ChimeraMonk: no Guids.Retired." + constName + " for the retired mutation " + id);
                return new RetiredRow(id, (string)f.GetRawConstantValue(), "mut." + id);
            }).ToArray();
        }

        // 변이 표 무결성(C2) — 빈 목록 = 일치. Mutations.Build 가 첫머리에서 부르고 비어 있지 않으면 던진다. 시험 `retired: plan` 이 같은 함수를 실행한다.
        //  ① 선택지 수 == Dials.MutationOptions(선택 화면 문구의 「선택지 N종」이 이 상수다)
        //  ② 폐지 변이는 선택지에 없고, 폐지 GUID 는 어떤 선택지·중첩 선택지의 GUID 와도 겹치지 않는다
        //  ③ 조건(게이트·선행) 없는 일반 변이 수 ≥ Dials.MutationPicks — 어느 변이 레벨에서도 고를 것이 남는다(각 변이는 한 번, 조건 없는 것만으로 모든 칸을 채울 수 있다)
        public static List<string> Problems()
        {
            var p = new List<string>();
            var rows = Rows();
            var retired = RetiredRows();
            if (rows.Count != ChimeraRules.Dials.MutationOptions) p.Add(rows.Count + " mutation option(s), Dials.MutationOptions says " + ChimeraRules.Dials.MutationOptions);
            if (rows.Select(r => r.Id).Distinct().Count() != rows.Count) p.Add("duplicate mutation id");
            var optionGuids = new HashSet<string>(rows.SelectMany(r => new[] { r.Guid }.Concat(r.IsNestedSelection ? r.Nested.Select(o => o.Guid) : new string[0])));
            optionGuids.Add(Guids.MutationSelection);
            if (retired.Select(r => r.Id).Distinct().Count() != retired.Count || retired.Select(r => r.Guid).Distinct().Count() != retired.Count) p.Add("duplicate retired mutation id / guid");
            foreach (var r in retired)
            {
                if (rows.Any(x => x.Id == r.Id)) p.Add("retired mutation " + r.Id + " is still a selection option");
                if (optionGuids.Contains(r.Guid)) p.Add("retired mutation " + r.Id + " shares its guid " + r.Guid + " with a selection option");
                if (rows.Any(x => x.Requires == r.Id)) p.Add("a selection option requires the retired mutation " + r.Id);
            }
            int unconditional = rows.Count(r => r.GateLevel == 0 && r.Requires == null && !r.IsNestedSelection);
            if (unconditional < ChimeraRules.Dials.MutationPicks) p.Add(unconditional + " mutation(s) without a condition, fewer than the " + ChimeraRules.Dials.MutationPicks + " picks (a level could have nothing to choose)");
            return p;
        }

        public static MutationRow Row(string id)
        {
            var hit = Rows().Where(r => r.Id == id).ToArray();
            if (hit.Length != 1) throw new ArgumentException("ChimeraMonk: MutationsPlan has " + hit.Length + " row(s) for " + id, "id");
            return hit[0];
        }

        // 중첩 선택의 선택지 하나가 「이미 가졌으면 못 고르는」 특성 GUID 목록 = 그 선택지의 PrerequisiteNoFeature(Mutations.Build 가 이 목록으로 만들고 Readback 이 대조한다).
        //  ① 같은 묶음의 선택지 전부(선택형 특성은 Ranks 검사를 건너뛴다 — 부록 A-g: 한 묶음에서 하나만),
        //  ② 부분 변환 단계: 다른 단계의 같은 형태(소유자 결정 2026-11-16 4번 — 세 형태는 서로 달라야 한다).
        public static IReadOnlyList<string> OptionExcludes(MutationRow row, NestedOption option)
        {
            if (row == null) throw new ArgumentNullException("row");
            if (option == null) throw new ArgumentNullException("option");
            if (!row.IsNestedSelection || !row.Nested.Any(o => o.Guid == option.Guid)) throw new ArgumentException("ChimeraMonk: " + option.Key + " is not an option of " + row.Id, "option");
            var list = row.Nested.Select(o => o.Guid).ToList();
            if (ChimeraRules.AspectStages.Any(s => s.Id == row.Id))
                foreach (var other in ChimeraRules.AspectStages.Where(s => s.Id != row.Id))
                    list.Add(AspectStageForms(other.Number).Single(o => o.Key == option.Key).Guid);
            return list;
        }

        // 선행 조건 표(유일한 원장, 독립 검토 2026-11-16): 우리 특성마다 한 줄 — 변이 행 순서, 중첩 선택 바로 뒤에 그 선택지들.
        //  Mutations.Build 가 이 표의 줄로만 선행 조건 컴포넌트를 만들고(Mutations.ApplyPrerequisites), Readback.MutationsDiff 가 빌드된 블루프린트를 이 표와 대조하고,
        //  CanPick 이 이 표를 해석한다 — 모형과 구현이 한 표에서 나온다.
        //   일반 변이: (게이트 레벨, 선행 변이, 금지 없음) · 중첩 선택: (게이트 레벨, 선행 변이, 금지 = 자기 선택지 전부 — 이미 하나 가졌으면 재선택 불가)
        //   선택지: (0, 없음, 금지 = OptionExcludes).
        public static IReadOnlyList<PrereqRow> Prerequisites()
        {
            var list = new List<PrereqRow>();
            foreach (var r in Rows())
            {
                var own = r.IsNestedSelection ? r.Nested.Select(o => o.Guid).ToArray() : new string[0];
                list.Add(new PrereqRow(r.Guid, r.Id, r.GateLevel, r.RequiresGuid, own));
                if (!r.IsNestedSelection) continue;
                foreach (var o in r.Nested) list.Add(new PrereqRow(o.Guid, r.Id + "/" + o.Key, 0, null, OptionExcludes(r, o)));
            }
            return list;
        }

        public static PrereqRow PrereqOf(string guid)
        {
            var hit = Prerequisites().Where(p => p.Guid == guid).ToArray();
            if (hit.Length != 1) throw new ArgumentException("ChimeraMonk: prerequisite table has " + hit.Length + " row(s) for " + guid, "guid");
            return hit[0];
        }

        // 표의 한 줄을 엔진 선행 조건처럼 해석: 레벨 ≥ 최소 · 요구 특성 보유 · 금지 특성 하나도 없음.
        static bool Meets(PrereqRow p, int monkLevel, ICollection<string> owned)
        {
            if (monkLevel < p.MinMonkLevel) return false;
            if (p.RequiresGuid != null && !owned.Contains(p.RequiresGuid)) return false;
            return !p.Forbid.Any(owned.Contains);
        }

        // [순수 규칙] 변이(중첩 선택이면 그 선택지까지)를 지금 고를 수 있는가 = 선행 조건 표(Prerequisites)의 그 줄(들)을 Meets 로 해석한 결과
        //  + 일반 특성은 한 번(Ranks 1 — 엔진의 랭크 검사, 표 밖). 중첩 선택은 선택 줄과 선택지 줄을 둘 다 통과해야 한다.
        //  owned = 가진 우리 특성 GUID(변이·선택지). 중첩 선택인데 optionKey 가 없거나 모르는 키면 던진다(조용히 false 가 아니다).
        public static bool CanPick(string mutationId, string optionKey, int monkLevel, ICollection<string> owned)
        {
            if (owned == null) throw new ArgumentNullException("owned");
            if (monkLevel < ChimeraRules.Levels.MinLevel || monkLevel > ChimeraRules.Levels.MaxLevel) throw new ArgumentOutOfRangeException("monkLevel", monkLevel, "ChimeraMonk: monk level must be " + ChimeraRules.Levels.MinLevel + ".." + ChimeraRules.Levels.MaxLevel);
            var row = Row(mutationId);
            if (!row.IsNestedSelection && optionKey != null) throw new ArgumentException("ChimeraMonk: " + mutationId + " has no nested options", "optionKey");
            NestedOption option = null;
            if (row.IsNestedSelection)
            {
                if (string.IsNullOrEmpty(optionKey)) throw new ArgumentException("ChimeraMonk: " + mutationId + " needs a nested option", "optionKey");
                var hit = row.Nested.Where(o => o.Key == optionKey).ToArray();
                if (hit.Length != 1) throw new ArgumentException("ChimeraMonk: " + mutationId + " has no option " + optionKey, "optionKey");
                option = hit[0];
            }
            if (!Meets(PrereqOf(row.Guid), monkLevel, owned)) return false;
            if (!row.IsNestedSelection) return !owned.Contains(row.Guid);
            return !owned.Contains(option.Guid) && Meets(PrereqOf(option.Guid), monkLevel, owned);
        }
    }
}
