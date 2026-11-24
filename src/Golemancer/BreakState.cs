using System;

namespace Golemancer
{
    // [순수] 부서짐 상태(설계 §4.7 — 무리 조련사 보조 사망 처리와 같은 방식). 저장 = UnitPartGolemMaster.Broken(T3). 게임 형식 없음.
    // 사망(펫 0 HP)·합체 피해 넘침 → Broken, 성공한 휴식 → Intact, 휴식 중단 → 유지, 외부 파괴 → 유지(신원 변화는 GolemLifecycle 의 Detached 가 다룬다).
    public enum BreakState { Intact, Broken }

    public enum BreakEvent { GolemDied, GolemDestroyed, RestSucceeded, RestInterrupted, FusionOverflow, Repaired }

    public static class BreakRules
    {
        public static BreakState Next(BreakState s, BreakEvent e)
        {
            if (!Enum.IsDefined(typeof(BreakState), s)) throw new ArgumentException("BreakRules.Next: undefined state " + (int)s);
            switch (e)
            {
                case BreakEvent.GolemDied:
                case BreakEvent.FusionOverflow:
                    return BreakState.Broken;
                case BreakEvent.RestSucceeded:
                case BreakEvent.Repaired: // T7 응급 수리(설계 §4.7 「부서짐 해제」)
                    return BreakState.Intact;
                case BreakEvent.RestInterrupted:
                case BreakEvent.GolemDestroyed:
                    return s;
            }
            throw new ArgumentException("BreakRules.Next: undefined event " + (int)e);
        }
    }
}
