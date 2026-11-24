using System;

namespace ChaosWarrior
{
     
    public interface IErrorSink
    {
        bool TakeArmed(string site);
        void ReportInjected(string site, string what);
        void ReportException(string site, Exception e);
        // 같은 (사이트, 예외 형식·메시지)는 한 번만 보고(그 뒤는 반복 수만 센다). 보고했으면 true.
        bool ReportExceptionOnce(string site, Exception e);
    }

    public static class ActionGuard
    {
        // 반환 true = 본문 실행됨.
        // ① 무장돼 있으면 주입분 보고(Count++·Injected++·INJECTED 줄) 후 본문을 부르지 않고 false — 던지지 않는다(엔진 ActionList.Run 의 예외 기록·공용 포착에 닿지 않게).
        // ② 본문 예외는 ReportException(진짜 오류) 후 재던짐 — 우리 장부·엔진 로그·공용 포착 셋 다 시끄럽게.
        public static bool Run(string site, Action body, IErrorSink sink)
        {
            if (string.IsNullOrEmpty(site)) throw new ArgumentException("ChaosWarrior: ActionGuard.Run without a site", "site");
            if (body == null) throw new ArgumentNullException("body");
            if (sink == null) throw new ArgumentNullException("sink");
            if (sink.TakeArmed(site))
            {
                sink.ReportInjected(site, "check-runner injected fault; body not run");
                return false;
            }
            try { body(); }
            catch (Exception e)
            {
                sink.ReportException(site, e);
                throw;
            }
            return true;
        }

        // 프레임마다 도는 감시기용(독립 검토 m5): 주입은 Run 과 같고, 본문 예외는 같은 종류를 한 번만 보고한 뒤 삼킨다 — 매 프레임 같은 오류로 로그가 덮이지 않게.
        // 첫 보고는 진짜 오류(Count++ · ERROR 줄 · 공용 포착), 반복은 장부의 SuppressedRepeats 로 남는다. 반환 true = 본문이 끝까지 돌았다.
        public static bool RunReportOnce(string site, Action body, IErrorSink sink)
        {
            if (string.IsNullOrEmpty(site)) throw new ArgumentException("ChaosWarrior: ActionGuard.RunReportOnce without a site", "site");
            if (body == null) throw new ArgumentNullException("body");
            if (sink == null) throw new ArgumentNullException("sink");
            if (sink.TakeArmed(site))
            {
                sink.ReportInjected(site, "check-runner injected fault; body not run");
                return false;
            }
            try { body(); }
            catch (Exception e)
            {
                sink.ReportExceptionOnce(site, e);
                return false;
            }
            return true;
        }
    }
}
