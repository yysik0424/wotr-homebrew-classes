using System;

namespace ShadowSorcerer
{
    // 지역 재로드 정리의 순서(엔진 형식 없음 — 시험에서 실행한다): 항목 하나의 실패를 보고하고 다음 항목으로 가며, 마무리는 어떤 경우에도 한 번 실행한다.
     
    // 보고 자체가 실패해도 나머지 항목과 마무리를 끝낸 뒤 그 실패를 다시 던진다(r3 지적 2) — 삼키지 않는다.
    public static class ReloadSweep
    {
        public static void Run<T>(T[] items, Action<T> visit, Action<Exception> report, Action finish)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (visit == null) throw new ArgumentNullException("visit");
            if (report == null) throw new ArgumentNullException("report");
            if (finish == null) throw new ArgumentNullException("finish");
            Exception reportFailure = null;
            try
            {
                foreach (var item in items)
                {
                    try { visit(item); }
                    catch (Exception e)
                    {
                        try { report(e); }
                        catch (Exception r) { if (reportFailure == null) reportFailure = r; }
                    }
                }
            }
            finally { finish(); }
            if (reportFailure != null) throw new InvalidOperationException("ShadowSorcerer: reload error report failed", reportFailure);
        }
    }
}
