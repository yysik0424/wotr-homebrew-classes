using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    // 게임 쪽 ChimeraErrors(Errors.cs)는 이 장부 하나에 로그 출구(ModContext.LogError / ModContext.Log)만 붙인다.
    //  - 진짜 오류: Count++ · 오류 등급 줄 「ChimeraMonk: ERROR <where>: <what>」(공용 로그 포착이 센다 → S0 FAIL).
    //  - 점검 주입 오류: Count++ · Injected++ · 정보 등급 줄 「ChimeraMonk: INJECTED <where>: <what>」(공용 포착에 안 잡힘) + InjectedLines.
    // 주입은 점검 러너가 돌 때만 무장할 수 있다(Arm 의 checksActive 거짓이면 던진다). 무장은 사이트마다 1회분이며 TakeArmed/MaybeFault 가 소비한다.
    // 불발(dud) 계수는 없다 — 이 모드에는 불발 개념이 없다(Bulwark 와의 차이).
    public sealed class InjectedFaultException : InvalidOperationException
    {
        public readonly string Site;
        public InjectedFaultException(string site) : base("injected fault " + site) { Site = site; }
    }

    public enum LedgerLevel { Error, Info }

    public struct ErrorSnapshot
    {
        public readonly int Count, Injected, InjectedLines;
        public ErrorSnapshot(int count, int injected, int injectedLines) { Count = count; Injected = injected; InjectedLines = injectedLines; }



        public override string ToString() { return "Count=" + Count + " Injected=" + Injected + " InjectedLines=" + InjectedLines; }
    }

    public sealed class ErrorLedger
    {
         
        public const string SiteDependency = "dependency";
        public const string SiteLimbDice = "limb-dice";
        public const string SiteBuilder = "builder";
        public static readonly IReadOnlyList<string> Sites = new[] { SiteDependency, SiteLimbDice, SiteBuilder };

        const string Prefix = "ChimeraMonk: ";

        readonly Action<LedgerLevel, string> sink;
        readonly HashSet<string> armed = new HashSet<string>();
        readonly List<string> injectedLines = new List<string>();

        public int Count { get; private set; }
        public int Injected { get; private set; }




        public ErrorLedger(Action<LedgerLevel, string> sink)
        {
            if (sink == null) throw new ArgumentNullException("sink");
            this.sink = sink;
        }

        static void RequireSite(string site)
        {
            if (site == null || !Sites.Contains(site)) throw new ArgumentException("ChimeraMonk: unknown fault-injection site '" + site + "' (known: " + string.Join(", ", Sites.ToArray()) + ")", "site");
        }





        public bool IsArmed(string site) { RequireSite(site); return armed.Contains(site); }

        public bool TakeArmed(string site)
        {
            RequireSite(site);
            return armed.Remove(site);
        }

        public void MaybeFault(string site)
        {
            if (TakeArmed(site)) throw new InjectedFaultException(site);
        }

        public void Report(string where, string what, bool injected)
        {
            if (string.IsNullOrEmpty(where)) throw new ArgumentException("ChimeraMonk: Report without a location", "where");
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

        // 뜨거운 경로용(사지 주사위 규칙은 공격마다 불린다): 같은 (where, key) 는 세션에 한 번만 보고(Count +1), 그 뒤는 SuppressedRepeats 만 센다.
        readonly HashSet<string> reportedOnce = new HashSet<string>();
        public int SuppressedRepeats { get; private set; }

        public bool ReportOnce(string where, string key, string what)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("ChimeraMonk: ReportOnce without a dedupe key", "key");
            if (!reportedOnce.Add(where + "|" + key)) { SuppressedRepeats++; return false; }
            Report(where, what, false);
            return true;
        }

        public void ReportException(string where, Exception e)
        {
            if (e == null) throw new ArgumentNullException("e");
            Report(where, e.GetType().Name + ": " + e.Message, e is InjectedFaultException);
        }








    }
}
