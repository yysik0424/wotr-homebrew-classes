using System;
using System.Linq;

namespace Golemancer
{
    // [순수] 격리 스냅샷 차이 → 던짐 판정(T8b, 검토 지시 5). 게임 쪽 두 곳(GolemIsolation.VerifyAfterBuild · Materials.Build 끝)이 차이 배열을 그대로 넘긴다.
    // 콘솔 시험 `exec: snapshot guard` 가 실행한다(빈 차이 → 무동작, 차이 1+ → InvalidOperationException, 결측 입력 → 던짐). 게임 형식 없음 → modinfo testSources.
    public static class SnapshotGuard
    {
        public static void ThrowIfChanged(string[] diff, string where)
        {
            if (diff == null) throw new ArgumentNullException("diff");
            if (string.IsNullOrEmpty(where)) throw new ArgumentException("SnapshotGuard.ThrowIfChanged: empty site name");
            if (diff.Length > 0) throw new InvalidOperationException("Golemancer: base-game values changed during " + where + ": " + string.Join(" | ", diff.Take(10).ToArray()));
            // 정당한 무동작: 차이 0 = 본편 값 그대로
        }
    }
}
