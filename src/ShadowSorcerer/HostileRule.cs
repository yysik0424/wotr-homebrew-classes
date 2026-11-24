using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
     
    // 1. HostileOverrides.Table(출처 필수)에 있으면 그 값. 2. 아니면 식 CanTargetEnemies && (EffectOnEnemy == Harmful || !CanTargetFriends) && !IsCantrip && !IsSummoning.
    // 3. 사실 결측 → Unknown. 식 근거: 정찰 §2 표본 14건(CLW 반례: EoE Harmful 인데 적 대상 불가 · Web 반례: 적대인데 EoE None).
    public sealed class HostileFacts
    {
        public string SpellGuid;
        public bool? CanTargetEnemies, CanTargetFriends, EffectOnEnemyHarmful, IsCantrip, IsSummoning;
    }

    public static class HostileRule
    {
        public static GateResult Classify(HostileFacts f) { return ClassifyCore(f, HostileOverrides.Table); }

        public static GateResult ClassifyCore(HostileFacts f, IReadOnlyList<HostileOverride> overrides)
        {
            if (f == null) return GateResult.Unknown("no hostile facts");
            if (f.SpellGuid != null && overrides != null)
            {
                var o = overrides.Where(x => x.SpellGuid == f.SpellGuid).ToArray();
                if (o.Length > 1) return GateResult.Unknown("hostile override listed " + o.Length + " times for " + f.SpellGuid);
                if (o.Length == 1) return o[0].Echo ? new GateResult(Decision.Echo, "override", 0) : GateResult.Skip(GateReasons.NotHostile);
            }
            if (!f.CanTargetEnemies.HasValue || !f.CanTargetFriends.HasValue || !f.EffectOnEnemyHarmful.HasValue || !f.IsCantrip.HasValue || !f.IsSummoning.HasValue)
                return GateResult.Unknown("hostile facts unread for " + (f.SpellGuid ?? "?"));
            bool hostile = f.CanTargetEnemies.Value && (f.EffectOnEnemyHarmful.Value || !f.CanTargetFriends.Value) && !f.IsCantrip.Value && !f.IsSummoning.Value;
            return hostile ? new GateResult(Decision.Echo, null, 0) : GateResult.Skip(GateReasons.NotHostile);
        }
    }
}
