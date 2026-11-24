using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    //  - 껍데기마다 Ranks 1 · IsClassFeature · 게이트 행 → PrerequisiteClassLevel{Monk, 행.GateLevel} · 선행 행 → PrerequisiteFeature{행.RequiresGuid}.
    //  - 중첩 선택(브레스 → 원소 4, 부분 변환 I·II·III → 형태 10씩): 선택형 특성은 MeetsPrerequisites @0026 에서 Ranks 검사를 건너뛴다(부록 A-g) → 반복·두 종류 방지를 PrerequisiteNoFeature 로:
    //      옵션마다 MutationsPlan.OptionExcludes(같은 묶음의 옵션 전부 + 부분 변환이면 다른 단계의 같은 형태 — 2026-11-16: 세 형태는 서로 달라야 한다),
    //      중첩 선택 자신에도 옵션 전부에 NoFeature(이미 가졌으면 그 변이 재선택 불가).
    //  - 선행 조건(레벨·요구·금지)은 전부 선행 조건 표 MutationsPlan.Prerequisites() 의 줄에서 ApplyPrerequisites 가 만든다(독립 검토 2026-11-16 — 표를 시험이 직접 검증한다).
    //  - 부분 변환 II·III 의 선행(앞 단계 보유)은 PrerequisiteFeature{앞 단계의 중첩 선택}: 중첩 선택도 고르면 사실로 남는다
    //      (SelectFeature.Apply IL @00FB~01EC, dnlib 2026-11-16 — 항목이 BlueprintFeatureSelection 이면 AddFact 까지 간다. 정찰 8-b 의 [미확인] 해소, 게임 안 확인은 눈 점검).
    //  - 효과는 T4~T7 빌더가 Get(id)·Option(…) 으로 받아 컴포넌트를 붙인다.
    //  - 폐지된 변이(소유자 결정 2026-11-17: 추가 꼬리, MutationsPlan.RetiredRows)는 BuildRetired 가 같은 GUID 로 「효과 부품 없는 숨김 특성」만 만든다 — 선택지·선행 조건 표에 없고 Get(id) 로는 못 얻는다.
    //      왜 남기는가: 이미 그 변이를 고른 캐릭터의 세이브가 이 GUID 의 사실을 참조한다. 블루프린트가 없으면 엔진은 「saves-support」 실행 인자가 있을 때만 그 사실을 지우고,
    //      없으면 null 블루프린트로 EntityFact.PostLoad 를 부른다(EntityFactsManager.PostLoad IL 0044~0088 — 귀검사 「재충전(폐지)」 StandBlueprints.cs:179 선례와 같은 근거).
    //      옛 세이브의 AddAdditionalLimb 런타임 컴포넌트는 로드 때 경고 1줄과 함께 버려진다(EntityFact.PostLoadComponents IL_006F~008B 「can't find source component」 → Dispose).
    public static class Mutations
    {
        public static BlueprintFeatureSelection Selection;
        static readonly Dictionary<string, BlueprintFeature> byId = new Dictionary<string, BlueprintFeature>();
        static readonly Dictionary<string, BlueprintFeature> options = new Dictionary<string, BlueprintFeature>();
        static readonly Dictionary<string, BlueprintFeature> retired = new Dictionary<string, BlueprintFeature>();

        public static BlueprintFeatureSelection BreathElementSelection { get { return (BlueprintFeatureSelection)Get("Breath"); } }
        public static BlueprintFeatureSelection AspectSelection { get { return (BlueprintFeatureSelection)Get("Aspect"); } }

        public static BlueprintFeature Get(string id)
        {
            BlueprintFeature f;
            if (!byId.TryGetValue(id ?? "", out f)) throw new KeyNotFoundException("ChimeraMonk: mutation '" + id + "' not built (Mutations.Build first)");
            return f;
        }

        // 폐지된 변이의 블루프린트(효과 없음 · 숨김). 되읽기(Readback.RetiredDiff)와 게임 안 점검만 쓴다 — 빌더가 여기에 부품을 붙이면 안 된다(시험 `retired: wiring` 이 호출자를 잠근다).
        public static BlueprintFeature Retired(string id)
        {
            BlueprintFeature f;
            if (!retired.TryGetValue(id ?? "", out f)) throw new KeyNotFoundException("ChimeraMonk: retired mutation '" + id + "' not built (Mutations.Build first)");
            return f;
        }

        // 중첩 선택의 옵션(원소·형태) — 키 = "Breath/Fire", "Aspect/Bear" …
        public static BlueprintFeature Option(string mutationId, string key)
        {
            BlueprintFeature f;
            if (!options.TryGetValue(mutationId + "/" + key, out f)) throw new KeyNotFoundException("ChimeraMonk: option " + mutationId + "/" + key + " not built");
            return f;
        }

        public static void Build()
        {
            if (Selection != null) throw new InvalidOperationException("ChimeraMonk: Mutations.Build() called twice");
            var problems = MutationsPlan.Problems();
            if (problems.Count > 0) throw new InvalidOperationException("ChimeraMonk: mutation plan: " + string.Join(" | ", problems));
            var rows = MutationsPlan.Rows();
            var made = new Dictionary<string, BlueprintFeature>(); // GUID → 이 빌더가 만든 변이·중첩 선택·선택지
            foreach (var r in rows)
            {
                BlueprintFeature f;
                if (r.IsNestedSelection)
                {
                    foreach (var o in r.Nested)
                    {
                        var of = Fab.Feature("ChimeraMutation" + r.Id + o.Key, o.Guid, o.StringKey, false);
                        made.Add(o.Guid, of);
                        options.Add(r.Id + "/" + o.Key, of);
                    }
                    f = Fab.Selection("ChimeraMutation" + r.Id, r.Guid, "mut." + r.Id, r.Nested.Select(o => o.Guid));
                }
                else f = Fab.Feature("ChimeraMutation" + r.Id, r.Guid, "mut." + r.Id, false);
                f.Ranks = r.Ranks;
                made.Add(r.Guid, f);
                byId.Add(r.Id, f);
            }
            // 선행 조건: 표의 모든 줄을 조건 없이 한 번씩 옮긴다(시험 `prereq: wiring` 이 이 반복문에 분기·걸러내기가 없음을 IL 로 잠근다). 표에 있는데 만들지 않은 것 = Made 가 던진다.
            var table = MutationsPlan.Prerequisites();
            foreach (var p in table) ApplyPrerequisites(Made(made, p.Guid), p);
            if (made.Count != table.Count) throw new InvalidOperationException("ChimeraMonk: " + made.Count + " mutation blueprint(s) built, prerequisite table has " + table.Count + " row(s)");
            Selection = Fab.Selection("ChimeraMutationSelection", Guids.MutationSelection, "mut.selection", rows.Select(r => r.Guid));
            Archetype.AddPlanned(LevelPlan.KeyMutationSelection, Selection);
            BuildRetired();
            var diff = Readback.MutationsDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: mutation readback: " + string.Join(" | ", diff));
            var retiredDiff = Readback.RetiredDiff();
            if (retiredDiff.Count > 0) throw new InvalidOperationException("ChimeraMonk: retired mutation readback: " + string.Join(" | ", retiredDiff));
        }

        // 폐지된 변이: 같은 GUID 의 특성만 만든다. 부품(컴포넌트)·선행 조건·선택지 등록 없음, UI 숨김(HideInUI + HideInCharacterSheetAndLevelUp — 귀검사 Refill10 선례).
        //  이름·설명은 숨김이 안 먹는 화면에 보일 때를 위한 것이다(StringsTable "mut.<Id>.name" / ".desc" — 「(폐지)」).
        static void BuildRetired()
        {
            foreach (var r in MutationsPlan.RetiredRows())
            {
                var f = Fab.Feature("ChimeraMutation" + r.Id, r.Guid, r.StringKey, true);
                f.HideInCharacterSheetAndLevelUp = true;
                retired.Add(r.Id, f);
            }
        }

        static BlueprintFeature Made(Dictionary<string, BlueprintFeature> made, string guid)
        {
            BlueprintFeature f;
            if (!made.TryGetValue(guid, out f)) throw new KeyNotFoundException("ChimeraMonk: prerequisite table row " + guid + " has no blueprint built by Mutations.Build");
            return f;
        }

        // 선행 조건 표의 한 줄을 블루프린트에 옮긴다 — 칸 3개 전부(최소 몽크 레벨 · 요구 특성 · 금지 특성). 선행 조건 컴포넌트를 만드는 곳은 여기 하나뿐이다(시험 `prereq: wiring`).
        static void ApplyPrerequisites(BlueprintFeature bp, PrereqRow row)
        {
            if (bp == null) throw new ArgumentNullException("bp");
            if (row == null) throw new ArgumentNullException("row");
            if (bp.AssetGuid.ToString() != row.Guid) throw new InvalidOperationException("ChimeraMonk: prerequisite row " + row.Label + " (" + row.Guid + ") applied to " + bp.name + " (" + bp.AssetGuid + ")");
            var list = new List<BlueprintComponent>();
            if (row.MinMonkLevel > 0)
            {
                var p = Fab.Comp(new PrerequisiteClassLevel());
                SetField(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.MonkClass));
                p.Level = row.MinMonkLevel;
                list.Add(p);
            }
            if (row.RequiresGuid != null)
            {
                var p = Fab.Comp(new PrerequisiteFeature());
                SetField(p, "m_Feature", Ref<BlueprintFeatureReference>(row.RequiresGuid));
                list.Add(p);
            }
            foreach (var g in row.Forbid) list.Add(NoFeature(g));
            if (list.Count > 0) AddComponents(bp, list.ToArray());
        }

        static PrerequisiteNoFeature NoFeature(string guid)
        {
            var p = Fab.Comp(new PrerequisiteNoFeature());
            SetField(p, "m_Feature", Ref<BlueprintFeatureReference>(guid));
            return p;
        }
    }
}
