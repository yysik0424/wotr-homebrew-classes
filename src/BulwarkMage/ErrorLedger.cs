using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
     
    // (`exec: errors injected accounting`). 게임 쪽 BulwarkErrors(Errors.cs)는 이 장부 하나에 로그 출구(ModContext.LogError / ModContext.Log)만 붙인다.
    //  - 진짜 오류: Count++ · 오류 등급 줄 「BulwarkMage: ERROR <where>: <what>」(공용 로그 포착이 센다 → S0 no-mod-errors FAIL).
    //  - 점검 주입 오류: Count++ · Injected++ · 정보 등급 줄 「BulwarkMage: INJECTED <where>: <what>」(공용 포착에 안 잡힘) + InjectedLines 에 기록.
    //  - 불발(dud)은 오류가 아니다: DudCount++ · 정보 등급 줄.
    // 주입은 점검 러너가 돌 때만 무장할 수 있다(Arm 의 checksActive 거짓이면 던진다). 무장은 사이트마다 1회분이며 TakeArmed/MaybeFault 가 소비한다.
    public sealed class InjectedFaultException : InvalidOperationException
    {
        public readonly string Site;
        public InjectedFaultException(string site) : base("injected fault " + site) { Site = site; }
    }

    public enum LedgerLevel { Error, Info }

    public struct ErrorSnapshot
    {
        public readonly int Count, Injected, DudCount, InjectedLines;
        public ErrorSnapshot(int count, int injected, int dud, int injectedLines) { Count = count; Injected = injected; DudCount = dud; InjectedLines = injectedLines; }



        public override string ToString() { return "Count=" + Count + " Injected=" + Injected + " Dud=" + DudCount + " InjectedLines=" + InjectedLines; }
    }

    public sealed class ErrorLedger
    {
         
        public const string SiteH1Postfix = "h1-postfix";
        public const string SiteDudAction = "dud-action";
        public const string SiteResourceCondition = "resource-condition";
        public const string SiteIronConcentration = "iron-concentration";
        public const string SiteReadback = "readback";
         
        public const string SiteRebukeOutcome = "rebuke-outcome";
        public const string SiteRebukeDispelEnd = "rebuke-dispel-end";
        public const string SiteRebukeCanTarget = "rebuke-cantarget";
        public const string SiteBondPlacement = "bond-placement";
        public static readonly IReadOnlyList<string> Sites = new[] { SiteH1Postfix, SiteDudAction, SiteResourceCondition, SiteIronConcentration, SiteReadback, SiteRebukeOutcome, SiteRebukeDispelEnd, SiteRebukeCanTarget, SiteBondPlacement };

        const string Prefix = "BulwarkMage: ";

        readonly Action<LedgerLevel, string> sink;
        readonly HashSet<string> armed = new HashSet<string>();
        readonly List<string> injectedLines = new List<string>();

        public int Count { get; private set; }
        public int Injected { get; private set; }
        public int DudCount { get; private set; }




        public ErrorLedger(Action<LedgerLevel, string> sink)
        {
            if (sink == null) throw new ArgumentNullException("sink");
            this.sink = sink;
        }

        static void RequireSite(string site)
        {
            if (site == null || !Sites.Contains(site)) throw new ArgumentException("BulwarkMage: unknown fault-injection site '" + site + "' (known: " + string.Join(", ", Sites.ToArray()) + ")", "site");
        }









        // 무장돼 있으면 해제하고 true(그 실행의 보고는 주입분으로 센다), 아니면 false.
        public bool TakeArmed(string site)
        {
            RequireSite(site);
            return armed.Remove(site);
        }

        // 자체 부품 본문 맨 앞(try 안)에서 부른다: 무장돼 있으면 1회 해제하고 던진다 → 그 부품의 catch 가 ReportException 으로 주입분을 센다.
        public void MaybeFault(string site)
        {
            if (TakeArmed(site)) throw new InjectedFaultException(site);
        }

        public void Report(string where, string what, bool injected)
        {
            if (string.IsNullOrEmpty(where)) throw new ArgumentException("BulwarkMage: Report without a location", "where");
            Count++;
            if (injected)
            {
                Injected++;
                string line = Prefix + "INJECTED " + where + ": " + what;
                injectedLines.Add(line);
                sink(LedgerLevel.Info, line);
            }
            else sink(LedgerLevel.Error, Prefix + "ERROR " + where + ": " + what);
        }

        // 뜨거운 경로용(T3 검토 m-5 — H1 의 블루프린트 없는 무기 보고는 UI 새로고침마다 불릴 수 있다): 같은 (where, key) 는 세션에 한 번만 보고(Count +1, ERROR 줄 1),
        // 그 뒤 같은 키는 SuppressedRepeats 만 센다(오류가 사라지지 않게 개수는 남기되 로그·Count 를 도배하지 않는다). 보고했으면 true.
        readonly HashSet<string> reportedOnce = new HashSet<string>();
        public int SuppressedRepeats { get; private set; }

        public bool ReportOnce(string where, string key, string what)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("BulwarkMage: ReportOnce without a dedupe key", "key");
            if (!reportedOnce.Add(where + "|" + key)) { SuppressedRepeats++; return false; }
            Report(where, what, false);
            return true;
        }

        // catch 블록용: 주입 예외(InjectedFaultException)면 주입분, 그 밖은 진짜 오류.
        public void ReportException(string where, Exception e)
        {
            if (e == null) throw new ArgumentNullException("e");
            Report(where, e.GetType().Name + ": " + e.Message, e is InjectedFaultException);
        }

        public void Dud(string where, string what)
        {
            if (string.IsNullOrEmpty(where)) throw new ArgumentException("BulwarkMage: Dud without a location", "where");
            DudCount++;
            sink(LedgerLevel.Info, Prefix + "DUD " + where + ": " + what);
        }




    }
}
