using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.ResourceLinks;
using Kingmaker.Visual.CharacterSystem;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
    // 변이 외형(소유자 결정 2026-11-16 3번). 행 = 생산 입력 VisualPlan.Rows() — 이 빌더에는 GUID·에셋 id 리터럴이 없다(`visual: wiring`).
    //  꼬리(SingleAsset): 본편 묶음에서 에셋이 정확히 하나·종족 무관·계획 상수와 같은지 검사한 뒤 ChimeraBundleVisualComponent{SingleAsset, 그 에셋} 을 변이 특성에
    //    (본편 AddEquipmentEntity 는 쓰지 않는다 — 에셋 로드 실패가 조용히 지나간다. 독립 검토 2026-11-16).
    //  뿔(RaceBundle): 본편 묶음이 종족 의존·인간 칸 남녀 보유·에셋 id 형식인지 검사한 뒤 ChimeraBundleVisualComponent{RaceBundle, 묶음 GUID} 를 변이 특성에.
    //  묶음 블루프린트가 없거나 형식이 다르면 Get 이 던지고, 모양이 기대와 다르면 여기서 던진다(조용히 「외형 없음」으로 만들지 않는다 — C1·C2). 본편 블루프린트에는 쓰지 않는다(읽기만).
    public static class Visuals
    {
        public static bool Built { get; private set; }

        static IReadOnlyList<string> Ids(EquipmentEntityLink[] a)
        {
            // 직렬화되지 않은 배열(null) = 원소 0개. 비어 있음이 문제인지는 VisualPlan.*Problems / Pick 이 판정한다(여기서 통과시키지 않는다). null 원소는 null id 로 남겨 Problems 가 잡게 한다.
            return a == null ? new string[0] : a.Select(l => l == null ? null : l.AssetId).ToArray();
        }

        internal static BundleShape ShapeOf(KingmakerEquipmentEntity bundle)
        {
            if (bundle == null) throw new ArgumentNullException("bundle");
            var raceArrays = bundle.m_RaceDependentArrays;
            var races = raceArrays == null ? new BundleSlot[0] : raceArrays.Select(t => t == null ? null : new BundleSlot(Ids(t.MaleArray), Ids(t.FemaleArray))).ToArray();
            return new BundleShape(bundle.m_RaceDependent, Ids(bundle.m_MaleArray), Ids(bundle.m_FemaleArray), races);
        }

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("ChimeraMonk: Visuals.Build() called twice");
            foreach (var row in VisualPlan.Rows())
            {
                var bundle = Get<KingmakerEquipmentEntity>(row.BundleGuid); // 없음 · 다른 형식 = 던짐
                var shape = ShapeOf(bundle);
                var target = Mutations.Get(row.Mutation);
                if (row.Kind == VisualKind.SingleAsset)
                {
                    var problems = VisualPlan.SingleAssetProblems(shape, row.AssetId);
                    if (problems.Count > 0) throw new InvalidOperationException("ChimeraMonk: visual source " + row.BundleName + " (" + row.BundleGuid + ") for " + row.Mutation + ": " + string.Join(" | ", problems));
                    var c = Fab.Comp(new ChimeraBundleVisualComponent());
                    c.Kind = row.Kind;
                    c.BundleGuid = row.BundleGuid;
                    c.AssetId = row.AssetId;
                    AddComponents(target, c);
                }
                else if (row.Kind == VisualKind.RaceBundle)
                {
                    var problems = VisualPlan.BundleProblems(shape);
                    if (problems.Count > 0) throw new InvalidOperationException("ChimeraMonk: visual source " + row.BundleName + " (" + row.BundleGuid + ") for " + row.Mutation + ": " + string.Join(" | ", problems));
                    var c = Fab.Comp(new ChimeraBundleVisualComponent());
                    c.Kind = row.Kind;
                    c.BundleGuid = row.BundleGuid;
                    AddComponents(target, c);
                }
                else throw new InvalidOperationException("ChimeraMonk: visual kind " + row.Kind + " is not built here (" + row.Mutation + ")");
            }
            Built = true;
            var diff = Readback.VisualsDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: visual readback: " + string.Join(" | ", diff));
        }
    }
}
