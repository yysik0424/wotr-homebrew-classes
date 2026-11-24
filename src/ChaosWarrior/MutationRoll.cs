using System;
using System.Collections.Generic;

namespace ChaosWarrior
{
    // [순수] 광기의 변이 추첨(개편 설계 §4·§12-1, 계획 2026-11-16 Task O3): 개수 = 레벨 구간의 균등 정수, 뽑기 = 「지금 후보인 변이」 목록에서 균등 1개 — 복원 추출(같은 변이가 다시 나올 수 있다).
    // 다시 뽑기는 거부 반복이 아니다: 후보 목록(MutationEligibility.Eligible)을 먼저 만들고 그 안에서 한 번 뽑는다 → 뽑기는 정확히 k회, 반복이 없다. 후보 0 은 호출자(MutationCore)가 먼저 본다.
    // rangeInclusive(a, b) = a..b 균등 정수(게임: UnityEngine.Random.Range(a, b + 1), 시험: System.Random 고정 시드).
    public static class MutationRoll
    {
        // 이번 분노의 뽑기 횟수 k: 레벨 구간(ChaosRules.CountBands)의 MinCount..MaxCount 균등.
        public static int DrawCount(int level, Func<int, int, int> rangeInclusive)
        {
            if (rangeInclusive == null) throw new ArgumentNullException("rangeInclusive");
            var band = ChaosRules.BandFor(level);
            return Checked(rangeInclusive, band.MinCount, band.MaxCount);
        }

        // 후보 목록에서 균등 1개. 빈 목록은 던진다(후보 0 을 「0번 변이」나 「안 뽑음」으로 넘기지 않는다 — C1).
        public static int DrawOne(IReadOnlyList<int> eligible, Func<int, int, int> rangeInclusive)
        {
            if (eligible == null) throw new ArgumentNullException("eligible");
            if (rangeInclusive == null) throw new ArgumentNullException("rangeInclusive");
            if (eligible.Count == 0) throw new InvalidOperationException("ChaosWarrior: no candidate mutation to draw from (empty eligible list)");
            return eligible[Checked(rangeInclusive, 0, eligible.Count - 1)];
        }

        // 원소 껍질 원소 색인 0..(ShellElements − 1), 균등.
        public static int PickElement(Func<int, int, int> rangeInclusive)
        {
            if (rangeInclusive == null) throw new ArgumentNullException("rangeInclusive");
            return Checked(rangeInclusive, 0, ChaosRules.Dials.ShellElements.Length - 1);
        }

        // 난수 공급자가 범위 밖 값을 주면 조용히 잘라 쓰지 않고 던진다(C1).
        static int Checked(Func<int, int, int> rangeInclusive, int min, int max)
        {
            int v = rangeInclusive(min, max);
            if (v < min || v > max) throw new InvalidOperationException("ChaosWarrior: random source returned " + v + " for range " + min + ".." + max);
            return v;
        }
    }
}
