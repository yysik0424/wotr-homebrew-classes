using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
    public enum LegacyFocusAction { Keep, Remove, Undetermined }

    // [순수] 옛 세이브 주술사 무기 중점화 감사의 판정 규칙(2026-11-18 추가 수리, 게임 형식 없음 — modinfo testSources).
    // 배경: 영병 특성의 옛 AddParametrizedFeatures{무기 중점화, 범주}(#1)는 Disabled 로 남겼고, 엔진 ApplyPostLoadFixes 가 로드 때 그것을 Deactivate 해 무기 중점화를 뗀다.
    //  그 경로가 안 돌면 무기 중점화가 조용히 남는다 — 이 규칙이 남은 것을 셋으로 가른다(LegacyFocusAudit 가 게임 사실을 관측해 넘긴다).
    // 출처 근거(dnlib 2026-11-18): AddParametrizedFeatures.OnActivate @0022 는 UnitHelper.AddFact(owner, 특기, null, 매개변수)로 붙이고 결과를
    //  AddParametrizedFeaturesData.AppliedFeatures 에 넣는다 — 이 목록이 「우리 컴포넌트가 붙였다」의 유일한 엔진 기록이다(사실 자신의 EntityFact.SourceFact 는
    //  유닛 사실에 쓰는 곳이 없고 — set_SourceFact 호출은 Etude 둘뿐 — Feature.m_Source 는 레벨업·선택 경로의 Feature.SetSource 만 쓴다).
    //   - HeldByLegacyComponent: 아직 살아 있는 우리 옛 컴포넌트 런타임의 AppliedFeatures 에 이 사실이 있다 → 우리 것으로 확정 → Remove.
    //   - HasLevelUpSource: Feature.Source 또는 RankToSource 가 있다(선택·진행표·레벨업이 붙임 — 플레이어가 고른 특기) → Keep(우리 것이 아님).
    //   - 둘 다 아님: 기록이 사라진 우리 옛 무기 중점화일 수도, 다른 특성이 붙인 것일 수도 있다 → Undetermined(떼지 않는다, 로그 없음 — 소유자 결정 2026-11-18).
    //   - 둘 다: 모순 → Undetermined(떼지 않는다).
    public static class LegacyFocusRule
    {
        public sealed class FocusFact
        {
            public readonly string FactId;
            public readonly bool HeldByLegacyComponent, HasLevelUpSource;
            public FocusFact(string factId, bool heldByLegacyComponent, bool hasLevelUpSource)
            {
                if (string.IsNullOrEmpty(factId)) throw new ArgumentException("LegacyFocusRule: fact id is empty");
                FactId = factId; HeldByLegacyComponent = heldByLegacyComponent; HasLevelUpSource = hasLevelUpSource;
            }
        }

        public sealed class Decision
        {
            public readonly string FactId, Reason;
            public readonly LegacyFocusAction Action;
            public Decision(string factId, LegacyFocusAction action, string reason) { FactId = factId; Action = action; Reason = reason; }
        }

        public const string ReasonBoth = "it is in the disabled spirit-feature component's applied list but also has a level-up source (contradiction) — not removed";
        public const string ReasonNeither = "it has no level-up source and no live owner record: it may be the old spirit-feature Weapon Focus whose owner record is gone, or one granted by another feature — not removed";

        // 입력 순서대로 결정 1개씩. 후보 0개 = 빈 목록(Disabled 경로가 정상 — 아무것도 하지 않는다).
        public static List<Decision> Decide(IEnumerable<FocusFact> facts)
        {
            if (facts == null) throw new ArgumentNullException("facts");
            var list = facts.ToList();
            if (list.Any(f => f == null)) throw new ArgumentException("LegacyFocusRule: null candidate fact");
            var dup = list.GroupBy(f => f.FactId).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            if (dup.Length > 0) throw new ArgumentException("LegacyFocusRule: duplicate candidate fact id(s) " + string.Join(",", dup));
            return list.Select(f =>
                f.HeldByLegacyComponent && f.HasLevelUpSource ? new Decision(f.FactId, LegacyFocusAction.Undetermined, ReasonBoth)
                : f.HeldByLegacyComponent ? new Decision(f.FactId, LegacyFocusAction.Remove, "in the disabled spirit-feature component's applied list")
                : f.HasLevelUpSource ? new Decision(f.FactId, LegacyFocusAction.Keep, "has a level-up source (chosen feat)")
                : new Decision(f.FactId, LegacyFocusAction.Undetermined, ReasonNeither)).ToList();
        }
    }
}
