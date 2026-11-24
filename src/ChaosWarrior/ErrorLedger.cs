using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // [순수] 오류 계약의 계수부 — src\BulwarkMage\ErrorLedger.cs 복제 + 사이트 표만 이 모드 것. 게임 형식 없음 → 시험 exe 가 직접 실행한다.
     
     
    //  - 진짜 오류: Count++ · 오류 등급 줄 「ChaosWarrior: ERROR <where>: <what>」(공용 로그 포착에 잡힌다).
    //  - 점검 주입: Count++ · Injected++ · 정보 등급 줄 「ChaosWarrior: INJECTED <where>: <what>」 + InjectedLines.
    //  - 무효과(dud): 오류가 아니다 — DudCount++ · 정보 등급 줄.
    public enum LedgerLevel { Error, Info }

    public struct ErrorSnapshot
    {
        public readonly int Count, Injected, DudCount, InjectedLines;
        public ErrorSnapshot(int count, int injected, int dud, int injectedLines) { Count = count; Injected = injected; DudCount = dud; InjectedLines = injectedLines; }



        public override string ToString() { return "Count=" + Count + " Injected=" + Injected + " Dud=" + DudCount + " InjectedLines=" + InjectedLines; }
    }

    public sealed class ErrorLedger
    {
         
        public const string SiteBuilder = "builder";
        public const string SiteMutationRoll = "mutation-roll";
        public const string SitePain = "pain";
        public const string SiteRageAlias = "rage-alias";
        public const string SiteRageOff = "rage-off";
        public const string SiteOrphanSweep = "orphan-sweep"; // 2026-11-16 고아 변이 자동 정리(OrphanMutationSweep)
        public static readonly IReadOnlyList<string> Sites = new[] { SiteBuilder, SiteMutationRoll, SitePain, SiteRageAlias, SiteRageOff, SiteOrphanSweep };

        const string Prefix = "ChaosWarrior: ";

        readonly Action<LedgerLevel, string> sink;
        readonly HashSet<string> armed = new HashSet<string>();
        readonly List<string> injectedLines = new List<string>();
        readonly HashSet<string> reportedOnce = new HashSet<string>();

        public int Count { get; private set; }
        public int Injected { get; private set; }
        public int DudCount { get; private set; }
        public int SuppressedRepeats { get; private set; }
        public IReadOnlyList<string> InjectedLines { get { return injectedLines; } }

        public ErrorLedger(Action<LedgerLevel, string> sink)
        {
            if (sink == null) throw new ArgumentNullException("sink");
            this.sink = sink;
        }

        static void RequireSite(string site)
        {
            if (site == null || !Sites.Contains(site)) throw new ArgumentException("ChaosWarrior: unknown fault-injection site '" + site + "' (known: " + string.Join(", ", Sites.ToArray()) + ")", "site");
        }





        public bool IsArmed(string site) { RequireSite(site); return armed.Contains(site); }

        // 무장돼 있으면 해제하고 true(그 실행은 주입분으로 보고한다), 아니면 false.
        public bool TakeArmed(string site)
        {
            RequireSite(site);
            return armed.Remove(site);
        }

        public void Report(string where, string what, bool injected)
        {
            if (string.IsNullOrEmpty(where)) throw new ArgumentException("ChaosWarrior: Report without a location", "where");
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

        // 반복 경로용: 같은 (where, key) 는 세션에 한 번만 보고(Count +1, ERROR 줄 1). 그 뒤는 SuppressedRepeats 만 센다. 보고했으면 true.
        public bool ReportOnce(string where, string key, string what)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("ChaosWarrior: ReportOnce without a dedupe key", "key");
            if (!reportedOnce.Add(where + "|" + key)) { SuppressedRepeats++; return false; }
            Report(where, what, false);
            return true;
        }

        // catch 블록용: 언제나 진짜 오류(주입은 예외를 만들지 않는다).
        public void ReportException(string where, Exception e)
        {
            if (e == null) throw new ArgumentNullException("e");
            Report(where, e.GetType().Name + ": " + e.Message, false);
        }

        public void Dud(string where, string what)
        {
            if (string.IsNullOrEmpty(where)) throw new ArgumentException("ChaosWarrior: Dud without a location", "where");
            DudCount++;
            sink(LedgerLevel.Info, Prefix + "DUD " + where + ": " + what);
        }




    }
}
