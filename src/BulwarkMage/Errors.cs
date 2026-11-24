using System;
using System.Collections.Generic;
using WotrClassMod.Common;




namespace BulwarkMage
{
     
    //   진짜 오류 → ModContext.LogError("BulwarkMage: ERROR …")(공용 로그 포착이 센다), 주입·불발 → ModContext.Log(정보 등급 — 공용 포착 CheckPatches 에 안 잡힌다).
    // 단언(점검): 시나리오마다 errors-0 = (Count − Injected) 증가량 0 · S17 주입 단계 = Count +1 · Injected +1 · INJECTED 줄 +1 · S0 = Count == Injected + 포착 0.
     
    public static class BulwarkErrors
    {
        static readonly ErrorLedger ledger = new ErrorLedger((level, line) =>
        {
            if (level == LedgerLevel.Error) ModContext.LogError(line);
            else ModContext.Log(line);
        });

        public static IReadOnlyList<string> Sites { get { return ErrorLedger.Sites; } }
        public static int Count { get { return ledger.Count; } }
        public static int Injected { get { return ledger.Injected; } }






        internal static ErrorLedger Ledger { get { return ledger; } }

        public static void Report(string where, string what) { ledger.Report(where, what, false); }
        public static void ReportException(string where, Exception e) { ledger.ReportException(where, e); }
        public static bool ReportOnce(string where, string key, string what) { return ledger.ReportOnce(where, key, what); }
        public static int SuppressedRepeats { get { return ledger.SuppressedRepeats; } }
        public static void Dud(string where, string what) { ledger.Dud(where, what); }







        public static void MaybeFault(string site) { ledger.MaybeFault(site); }




         
        // 점검 러너가 돌 때만 기록한다(일반 플레이에서는 아무것도 남지 않는다). 읽기 = 시나리오 S11 spend-before-effect-probe · S14 dud-spend-before-log-probe.
        // 값은 인스턴스 필드(새 객체마다)라 정적 수치 필드가 없다(dial: single block).
         
         
        public static class Probe
        {



            public static void RecordCheck(string owner, int resource) { }
            public static void RecordDud(string owner, int resource) { }

        }
    }
}
