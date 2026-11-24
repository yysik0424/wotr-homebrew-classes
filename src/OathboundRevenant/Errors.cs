using System;
using System.Collections.Generic;
using WotrClassMod.Common;




namespace OathboundRevenant
{
    // 오류 계약 — 게임 쪽 출구(계획 Global Constraints). 계수·주입 판단은 순수 ErrorLedger, 여기는 로그 출구만:
    //   진짜 오류 → ModContext.LogError("OathboundRevenant: ERROR …")(공용 로그 포착이 센다), 주입 → ModContext.Log(정보 등급).
    // 단언(점검): 시나리오마다 errors-0 = (Count − Injected) 증가량 0 · S15 주입 단계 = Count +1 · Injected +1 · INJECTED 줄 +1 · S0 = Count == Injected + 포착 0.
    public static class RevenantErrors
    {
        static readonly ErrorLedger ledger = new ErrorLedger((level, line) =>
        {
            if (level == LedgerLevel.Error) ModContext.LogError(line);
            else ModContext.Log(line);
        });

        public static IReadOnlyList<string> Sites { get { return ErrorLedger.Sites; } }
        public static int Count { get { return ledger.Count; } }
        public static int Injected { get { return ledger.Injected; } }
        public static IReadOnlyList<string> InjectedLines { get { return ledger.InjectedLines; } }
        // Guard.Run 이 받는 출구(자체 부품 본문은 전부 Guard.Run(site, key, body, RevenantErrors.Sink)).
        public static ErrorSink Sink { get { return ledger; } }

        public static void Report(string where, string what) { ledger.Report(where, what, false); }
        public static bool ReportOnce(string where, string key, string what) { return ledger.ReportOnce(where, key, what); }
        public static int SuppressedRepeats { get { return ledger.SuppressedRepeats; } }







        public static bool TakeArmed(string site) { return ledger.TakeArmed(site); }




        // 빌더 실패 계약(계획 Global Constraints 「BuildAll 실패 계약」): 계수 후 재던짐.
        public static void Builder(string name, Action build)
        {
            if (build == null) throw new ArgumentNullException("build");
            try { build(); }
            catch (Exception e)
            {
                Report(ErrorLedger.SiteBuilder, name + ": " + e.GetType().Name + ": " + e.Message);
                throw;
            }
        }
    }
}
