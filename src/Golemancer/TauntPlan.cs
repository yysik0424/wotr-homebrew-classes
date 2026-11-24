using System;
using System.Collections.Generic;

namespace Golemancer
{
    // [순수] 도발 오라의 기대 원천(계획 T2 TauntPlan, R2-04·R3-02). 게임 형식 없음.
    // None/Golem/Master = 기대 원천. Both = 관측 전용(Taunt.SourceOf 가 두 곳 다 붙은 것을 본 경우) — 기대값으로는 쓸 수 없다.
    public enum TauntSource { None, Golem, Master, Both }

    public sealed class TauntFix
    {
        public readonly TauntSource[] Detach;     // Golem / Master 순
        public readonly TauntSource? Attach;      // null = 붙일 것 없음
        public TauntFix(TauntSource[] detach, TauntSource? attach) { Detach = detach; Attach = attach; }
    }

    public static class TauntPlan
    {
        // lifeAfter = 이번 적용이 성공했을 때 도달할 상태(Apply 는 항상 Ready 를 넘긴다 — R3-02). 비적용 호출(합체 Begin/Ending·휴식 전)은 현재 상태.
        // 미준비(Ready·Stale 아님) → None. 합체 Idle → 골렘, Active → 본체, Ending·AwaitRestore → None(전환 중 — 종료 완료 시 Golem).
        public static TauntSource Expected(FusionState fusion, LifecycleState lifeAfter)
        {
            if (!Enum.IsDefined(typeof(FusionState), fusion)) throw new ArgumentException("TauntPlan.Expected: undefined fusion state " + (int)fusion);
            if (!Enum.IsDefined(typeof(LifecycleState), lifeAfter)) throw new ArgumentException("TauntPlan.Expected: undefined lifecycle state " + (int)lifeAfter);
            if (lifeAfter != LifecycleState.Ready && lifeAfter != LifecycleState.Stale) return TauntSource.None;
            switch (fusion)
            {
                case FusionState.Idle: return TauntSource.Golem;
                case FusionState.Active: return TauntSource.Master;
                default: return TauntSource.None;
            }
        }

        // 현재 → 기대: 기대와 다른 원천은 전부 떼고(Both 면 둘 다), 기대 원천이 없으면 붙인다.
        public static TauntFix Fix(TauntSource current, TauntSource expected)
        {
            if (!Enum.IsDefined(typeof(TauntSource), current)) throw new ArgumentException("TauntPlan.Fix: undefined current " + (int)current);
            if (!Enum.IsDefined(typeof(TauntSource), expected) || expected == TauntSource.Both) throw new ArgumentException("TauntPlan.Fix: expected must be None, Golem or Master (got " + expected + ")");
            if (current == TauntSource.Both) return new TauntFix(new[] { TauntSource.Golem, TauntSource.Master }, expected == TauntSource.None ? (TauntSource?)null : expected);
            if (current == expected) return new TauntFix(new TauntSource[0], null);
            var detach = new List<TauntSource>();
            if (current != TauntSource.None) detach.Add(current);
            return new TauntFix(detach.ToArray(), expected == TauntSource.None ? (TauntSource?)null : expected);
        }
    }
}
