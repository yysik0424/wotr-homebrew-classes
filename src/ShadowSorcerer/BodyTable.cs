using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
    // [순수] 그림자 소서러 본체 표(게임 형식 없음, modinfo testSources). 시험 `exec: removed vs jbp` 가 SorcererProgression jbp 와 대조한다.
    public static class BodyTable
    {
        // 잃는 것(아키타입 RemoveFeatures) = (레벨, 본편 GUID). 출처: 설계 §5 + SorcererProgression.jbp L7/L13/L19 직독(T1 Step 0).
        // 레벨은 ShadowRules.RemovedFeatSelectionLevels 에서 파생한다(같은 값을 두 곳에 두지 않는다 — C4). 같은 선택이 레벨마다 1항목씩 3항목.
        public static readonly IReadOnlyList<(int level, string guid)> Removed = BuildRemoved();

        static (int level, string guid)[] BuildRemoved()
        {
            var levels = ShadowRules.RemovedFeatSelectionLevels;
            if (levels == null || levels.Count == 0) throw new InvalidOperationException("ShadowSorcerer: ShadowRules.RemovedFeatSelectionLevels is empty");
            return levels.Select(l => (l, Guids.Game.SorcererFeatSelection)).ToArray();
        }
    }
}
