using System;

namespace Golemancer
{
    // [순수] 인지약 차단 판정(설계 §4.2 (가), 정찰 §2 — 권장안 확정 §12-9). 게임 형식 없음 → 콘솔 시험이 직접 실행한다(modinfo testSources).
    // CognatogenGate(Harmony Postfix, BlueprintFeatureSelection.CanSelect)가 엔진 판정·선택지 특성 GUID·「골렘술사인가」를 넣고 결과를 그대로 따른다(시험 `wiring: gate calls CognatogenRule.Decide`).
    // 첫 고리(CognatogenFeature)만 막는다: Greater 는 PrerequisiteFeature(Cognatogen), Grand 는 PrerequisiteFeature(Greater) 라 사슬 전체가 막힌다(설계 §4.2, 시험 `jbp: cognatogen chain`).
    public static class CognatogenRule
    {
        // 「골렘술사인가」 판정(순수 — 게이트는 입력 존재 여부와 엔진 질의 델리게이트만 넘긴다, 시험 `exec: resolve is golemancer`).
        // 입력 중 하나라도 없으면 null(판정 불가) — 델리게이트를 부르지 않는다. 엔진 IsArchetype(null) 은 조용히 false(@0000~0004)라
        // 아키타입 블루프린트가 없을 때 부르면 「판정 불가」가 「골렘술사 아님」으로 흐른다(C1) → 여기서 막는다. 델리게이트 예외는 잡지 않는다(전파 — 계획).
        public static bool? ResolveIsGolemancer(bool archetypeBlueprintExists, bool unitExists, bool progressionExists, Func<bool> isArchetype)
        {
            if (isArchetype == null) throw new ArgumentNullException("isArchetype");
            if (!archetypeBlueprintExists || !unitExists || !progressionExists) return null;
            return isArchetype();
        }

        // isGolemancer: true = 골렘술사 아키타입 보유, false = 아님, null = 판정 불가(아키타입 블루프린트 미생성·유닛/진행 데이터 없음).
        // 판정 불가 = 계획이 정한 「제한된 fail-open」: 엔진 판정을 그대로 두고 logUnknown=true(게이트가 ERROR 1회 기록). README 알려진 한계.
        //   (모드 기본값을 만들어 넣지 않는다 — 결과는 엔진 판정 그대로이고, 판정 불가는 로그로 시끄럽게 드러난다.)
        // 엔진이 이미 거부(false)했으면 무엇이든 false 그대로(우리는 막기만 하고 열지 않는다).
        public static (bool result, bool logUnknown) Decide(bool engineResult, string itemFeatureGuid, bool? isGolemancer)
        {
            if (isGolemancer == null) return (engineResult, true);
            if (engineResult && isGolemancer.Value && string.Equals(itemFeatureGuid, Guids.Game.CognatogenFeature, StringComparison.Ordinal)) return (false, false);
            return (engineResult, false);
        }
    }
}
