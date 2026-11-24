using System;
using System.Collections.Generic;
using WotrClassMod.Common;




namespace ChimeraMonk
{
     
    // 단언(점검): 시나리오마다 errors-0 = (Count − Injected) 증가량 0 · S11 주입 단계 = Count +1 · Injected +1 · INJECTED 줄 +1 · S0 = Count == Injected + 포착 0.
    public static class ChimeraErrors
    {
        static readonly ErrorLedger ledger = new ErrorLedger((level, line) =>
        {
            if (level == LedgerLevel.Error) ModContext.LogError(line);
            else ModContext.Log(line);
        });

        public static IReadOnlyList<string> Sites { get { return ErrorLedger.Sites; } }
        public static int Count { get { return ledger.Count; } }
        public static int Injected { get { return ledger.Injected; } }




        public static void Report(string where, string what) { ledger.Report(where, what, false); }
        public static void ReportException(string where, Exception e) { ledger.ReportException(where, e); }
        public static bool ReportOnce(string where, string key, string what) { return ledger.ReportOnce(where, key, what); }
        public static int SuppressedRepeats { get { return ledger.SuppressedRepeats; } }










        public static void MaybeFault(string site) { ledger.MaybeFault(site); }
        public static bool TakeArmed(string site) { return ledger.TakeArmed(site); }
    }
}
