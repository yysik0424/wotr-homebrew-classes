using System;

namespace OathboundRevenant
{
     
    // 주입도 진짜 예외도 엔진에 넘기지 않는다(엔진 루프를 깨지 않음) — 대신 반드시 계수하고, 문맥 키가 있으면 FailureLedger 에 실패 사이트를 남긴다.
    // 반환값은 소비돼야 한다(wiring: guards consumed): false = 본문이 끝까지 돌지 않았다(주입 또는 예외, 둘 다 계수됨).
    public interface ErrorSink
    {
        bool TakeArmed(string site);
        void ReportInjected(string site, string what);
        void Report(string site, string what);
    }

    public static class Guard
    {
        public static bool Run(string site, object failureKey, Action body, ErrorSink sink)
        {
            if (string.IsNullOrEmpty(site)) throw new ArgumentException("OathboundRevenant: Guard.Run without a site", "site");
            if (body == null) throw new ArgumentNullException("body");
            if (sink == null) throw new ArgumentNullException("sink");
            if (sink.TakeArmed(site))
            {
                sink.ReportInjected(site, "armed fault (body not run)");
                if (failureKey != null) FailureLedger.Mark(failureKey, site);
                return false;
            }
            try
            {
                body();
                return true;
            }
            catch (Exception e)
            {
                sink.Report(site, e.GetType().Name + ": " + e.Message);
                if (failureKey != null) FailureLedger.Mark(failureKey, site);
                return false;
            }
        }
    }
}
