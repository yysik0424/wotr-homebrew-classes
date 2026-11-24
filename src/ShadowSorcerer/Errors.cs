using System;
using System.Collections.Generic;
using WotrClassMod.Common;




namespace ShadowSorcerer
{
     
    //   Report = LogError + Count++ · 주입 Report = Injected++ + 정보 등급 INJECTED 줄 · Dud 는 오류가 아니다(사유별 계수).
    // 단언: 시나리오마다 errors-0 = Δ(Count − Injected) 0 · S17 주입 단계 = Count +1 · Injected +1 · INJECTED 줄 +1 · S0 = Count == Injected + 포착 0.
    public static class ShadowErrors
    {
        static readonly ErrorLedger ledger = new ErrorLedger((level, line) =>
        {
            if (level == LedgerLevel.Error) ModContext.LogError(line);
            else ModContext.Log(line);
        });

        public static IReadOnlyList<string> Sites { get { return ErrorLedger.Sites; } }



        public static int Injected { get { return ledger.Injected; } }






        internal static ErrorLedger Ledger { get { return ledger; } }

        public static void Report(string where, string what) { ledger.Report(where, what, false); }
        public static void ReportException(string where, Exception e) { ledger.ReportException(where, e); }
        public static bool ReportOnce(string where, string key, string what) { return ledger.ReportOnce(where, key, what); }
        public static int SuppressedRepeats { get { return ledger.SuppressedRepeats; } }
        public static void Dud(string where, string what) { ledger.Dud(where, what); }







        public static void MaybeFault(string site) { ledger.MaybeFault(site); }




        // 사유별 계수(불발·건너뜀). 오류가 아니다 — 점검이 사유 분포를 읽는다(S6 skip-reasons-recorded · S7 drop-reason-*).
        static readonly Dictionary<string, int> skipByReason = new Dictionary<string, int>();
        static readonly Dictionary<string, int> dudByReason = new Dictionary<string, int>();
        static readonly Dictionary<string, int> guardHits = new Dictionary<string, int>();









        public static void CountSkip(string reason) { Bump(skipByReason, reason); }
        public static void CountDud(string reason) { Bump(dudByReason, reason); }
        public static void GuardHit(string site) { Bump(guardHits, site); }




        static void Bump(Dictionary<string, int> d, string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("ShadowSorcerer: empty counter key");
            int n;
            d[key] = d.TryGetValue(key, out n) ? n + 1 : 1;
        }
    }
}
