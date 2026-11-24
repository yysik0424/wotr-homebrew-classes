using HarmonyLib;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using WotrClassMod.Common;

namespace Golemancer
{
    // 인지약 차단(설계 §4.2 (가), §12-9 확정, 정찰 §2): BlueprintFeatureSelection.CanSelect Postfix 1개. 본편 객체 무수정, 모든 선택지(발견·추가 발견 특기·해부학자)를 한 지점에서 덮는다.
    // 대상(dnlib 2026-11-09, tools\patch-targets\Golemancer.txt): public virtual CanSelect(UnitDescriptor unit, LevelUpState state, FeatureSelectionState selectionState, IFeatureSelectionItem item)
    //   — 이 형식의 유일한 오버로드. MeetsPrerequisites @006E 뒤 랭크·Mode 검사 @0077~00C8. 재정의는 BlueprintFeatureSelectMythicSpellbook·BlueprintParametrizedFeature 뿐(발견 선택 형식 아님).
    // 판정은 순수 CognatogenRule.Decide 한 곳(이 파일은 입력을 모으고 결과를 따른다 — 시험 `wiring: gate calls CognatogenRule.Decide`).
    [HarmonyPatch(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.CanSelect), new[] { typeof(UnitDescriptor), typeof(LevelUpState), typeof(FeatureSelectionState), typeof(IFeatureSelectionItem) })]
    static class CognatogenGatePatch
    {
        // 판정 불가 오류는 1회만(CanSelect 는 레벨업 화면에서 매 항목 불린다 — 같은 원인으로 로그를 채우지 않는다). 엔진 판정은 그대로 둔다(계획의 제한된 fail-open, README 알려진 한계).
        static bool loggedUnknown;

        static void Postfix(ref bool __result, UnitDescriptor unit, IFeatureSelectionItem item)
        {
            // item 이 null 이면 엔진이 이미 false(@0000~0005) — 특성 GUID 없음 = 인지약 아님으로 넘겨도 결과는 엔진 false 그대로(Decide 는 막기만 한다).
            if (ModContext.BuildSkipReasonEn != null) return;
            var feature = item == null ? null : item.Feature;
            string guid = feature == null ? null : feature.AssetGuid.ToString();
            // 「골렘술사인가」: 우리 아키타입 블루프린트가 없거나(빌드 실패) 유닛·진행 데이터가 없으면 판정 불가(null).
            // IsArchetype(null) 은 조용히 false 를 돌려주므로(@0000~0004) null 인 Bp 를 넘기지 않는다. IsArchetype 의 예외는 잡지 않는다(전파 — 계획).
            // 판정은 순수 CognatogenRule.ResolveIsGolemancer(시험이 실행으로 고정, 게이트 IL 은 입력이 Bp·unit·unit.Progression 에서 오는지 고정).
            var bp = Archetype.Bp;
            bool? isGolemancer = CognatogenRule.ResolveIsGolemancer(bp != null, unit != null, unit != null && unit.Progression != null, () => unit.Progression.IsArchetype(bp));
            var d = CognatogenRule.Decide(__result, guid, isGolemancer);
            if (d.logUnknown && !loggedUnknown)
            {
                loggedUnknown = true;
                ModContext.LogError("Golemancer: UNKNOWN cognatogen gate — cannot tell whether " + (unit == null ? "(null unit)" : unit.CharacterName) + " is a Golemancer (archetype blueprint "
                    + (bp == null ? "missing" : "present") + ", progression " + (unit == null || unit.Progression == null ? "missing" : "present") + "); engine verdict kept (logged once)");
            }
            __result = d.result;
        }
    }
}
