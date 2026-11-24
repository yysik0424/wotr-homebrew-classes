using System;
using System.Collections.Generic;

namespace OathboundRevenant
{
     
    // 결측은 판정 불가(Missing, C1) — 부정으로 읽히지 않는다. 반환값은 게임 쪽 부품이 분기에 쓴다(wiring: guards consumed).

    public enum SmiteTargetVerdict { Yes, No, Missing }

    public struct SmiteTargetDecision
    {
        public readonly SmiteTargetVerdict Verdict;
        public readonly string MissingInput;
        public SmiteTargetDecision(SmiteTargetVerdict v, string missing) { Verdict = v; MissingInput = missing; }
    }

    public static class SmiteTargetRule
    {
        public static SmiteTargetDecision Decide(bool casterPresent, bool targetPresent, bool smiteBlueprintPresent, bool targetHasSmiteFromCaster)
        {
            if (!casterPresent) return new SmiteTargetDecision(SmiteTargetVerdict.Missing, "caster");
            if (!targetPresent) return new SmiteTargetDecision(SmiteTargetVerdict.Missing, "target");
            if (!smiteBlueprintPresent) return new SmiteTargetDecision(SmiteTargetVerdict.Missing, "blueprint");
            return new SmiteTargetDecision(targetHasSmiteFromCaster ? SmiteTargetVerdict.Yes : SmiteTargetVerdict.No, null);
        }
    }

    public enum RoundOnceVerdict { Skip, Run, MarkerFailed }

    public static class RoundOnceRule
    {
        // 표지 있음 = 이 라운드에 이미 발동(정당한 거절) · 부착 성공 = 본문 실행 · 부착 실패 = 본문 미실행 + 보고(#11 — 이중 발동·무음 무동작 방지).
        public static RoundOnceVerdict Decide(bool markerPresent, bool markerApplied)
        {
            if (markerPresent) return RoundOnceVerdict.Skip;
            return markerApplied ? RoundOnceVerdict.Run : RoundOnceVerdict.MarkerFailed;
        }
    }

    public enum SmiteLinkOnRemove { RemoveMark, Ignore }
    public enum SmiteLinkOnDeath { Transfer, None }

    public static class SmiteLinkRule
    {
        // 대상에게서 버프가 떨어질 때: 우리 시전자의 스마이트가 떨어진 것이면 낙인(자식 흔들림·감속·치유 불가)도 즉시 뗀다(#4).
        public static SmiteLinkOnRemove OnBuffRemoved(bool removedIsSmite, bool removedCasterIsOurs)
        {
            return removedIsSmite && removedCasterIsOurs ? SmiteLinkOnRemove.RemoveMark : SmiteLinkOnRemove.Ignore;
        }

        // 대상 사망: 우리 스마이트가 그 순간 살아 있고 시전자가 죽음의 낙인을 가졌을 때만 이전(#4 — 먼저 풀린 스마이트는 이전하지 않음).
        public static SmiteLinkOnDeath OnOwnerDeath(bool smiteStillActiveFromCaster, bool hasMarkOfDeath)
        {
            return smiteStillActiveFromCaster && hasMarkOfDeath ? SmiteLinkOnDeath.Transfer : SmiteLinkOnDeath.None;
        }
    }

    public sealed class TransferCandidate
    {
        public readonly bool Alive, Enemy, Evil, IsOriginal;
        public readonly float EdgeFeet;
        public TransferCandidate(bool alive, bool enemy, bool evil, bool isOriginal, float edgeFeet) { Alive = alive; Enemy = enemy; Evil = evil; IsOriginal = isOriginal; EdgeFeet = edgeFeet; }
    }

    public static class SmiteTransferRule
    {
        // 살아 있음 ∧ 적 ∧ 악 ∧ 원래 대상 아님 ∧ 가장자리 거리 ≤ radius 중 최근접(동률은 목록 순). 없으면 −1(정당한 무동작).
        public static int Pick(IReadOnlyList<TransferCandidate> c, float radiusFeet)
        {
            if (c == null) throw new ArgumentNullException("c");
            if (float.IsNaN(radiusFeet) || radiusFeet < 0) throw new ArgumentOutOfRangeException("radiusFeet", radiusFeet, "OathboundRevenant: transfer radius must be >= 0");
            int best = -1;
            for (int i = 0; i < c.Count; i++)
            {
                var x = c[i];
                if (x == null) throw new ArgumentException("OathboundRevenant: null transfer candidate at " + i);
                if (!x.Alive || !x.Enemy || !x.Evil || x.IsOriginal || x.EdgeFeet > radiusFeet) continue;
                if (best < 0 || x.EdgeFeet < c[best].EdgeFeet) best = i;
            }
            return best;
        }
    }
}
