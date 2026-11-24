using System;
using System.Collections.Generic;
using WotrClassMod.Common;




namespace ChaosWarrior
{
     
    //   진짜 오류 → ModContext.LogError(「ChaosWarrior: ERROR …」, 공용 로그 포착에 잡힌다) · 주입·불발 → ModContext.Log(정보 등급).
    public static class ChaosErrors
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

        public static void Report(string where, string what) { ledger.Report(where, what, false); }
        public static void ReportException(string where, Exception e) { ledger.ReportException(where, e); }
        public static bool ReportOnce(string where, string key, string what) { return ledger.ReportOnce(where, key, what); }
        public static void Dud(string where, string what) { ledger.Dud(where, what); }







        public static bool TakeArmed(string site) { return ledger.TakeArmed(site); }

        // ActionGuard 가 쓰는 출구(어댑터 RunAction 은 ActionGuard.Run(site, body, ChaosErrors.Sink) 한 줄).
        public static readonly IErrorSink Sink = new LedgerSink();

        sealed class LedgerSink : IErrorSink
        {
            public bool TakeArmed(string site) { return ledger.TakeArmed(site); }
            public void ReportInjected(string site, string what) { ledger.Report(site, what, true); }
            public void ReportException(string site, Exception e) { ledger.ReportException(site, e); }
            public bool ReportExceptionOnce(string site, Exception e) { return ledger.ReportOnce(site, e.GetType().Name + ": " + e.Message, e.GetType().Name + ": " + e.Message); }
        }
    }
}
