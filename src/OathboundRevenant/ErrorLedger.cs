using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
     
    // 게임 형식 없음 → 시험 exe 가 그대로 실행한다(exec: guard injected no throw). 게임 쪽 RevenantErrors(Errors.cs)는 로그 출구만 붙인다.
    //  - 진짜 오류: Count++ · 오류 등급 줄 「OathboundRevenant: ERROR <where>: <what>」(공용 로그 포착이 센다 → S0 FAIL).
    //  - 점검 주입: Count++ · Injected++ · 정보 등급 줄 「OathboundRevenant: INJECTED <where>: <what>」(공용 포착에 안 잡힘).
     
    public enum LedgerLevel { Error, Info }

    public struct ErrorSnapshot
    {
        public readonly int Count, Injected, InjectedLines;
        public ErrorSnapshot(int count, int injected, int injectedLines) { Count = count; Injected = injected; InjectedLines = injectedLines; }



        public override string ToString() { return "Count=" + Count + " Injected=" + Injected + " InjectedLines=" + InjectedLines; }
    }

    public sealed class ErrorLedger : ErrorSink
    {
        // 런타임 사이트(계획 Global Constraints 「오류 계약」 RevenantErrors.Sites). builder = 빌드 시점 보고(주입 대상 아님, Report 의 where 로만 쓴다).
        public const string SiteBuilder = "builder";
        public const string SiteSmiteTarget = "smite-target";
        public const string SiteRoundOnce = "round-once";
        public const string SiteSmiteTransfer = "smite-transfer";
        public const string SiteChannelHeal = "channel-heal";
        public const string SiteChannelDice = "channel-dice";
        public const string SiteUnyielding = "unyielding";
        public const string SiteRiposte = "riposte";
        public const string SiteOathShield = "oath-shield";
        // T8: 말 HP 매력 추종 부품(MountChaHpComponent).
        public const string SiteMountHp = "mount-hp";
        // 검토 C1: 영혼 수확 부품(SoulHarvestComponent — 치명 무기 피해 기록 · 실제 사망 시 회복).
        public const string SiteSoulHarvest = "soul-harvest";
        // 2026-11-17: 보고 위치(주입 사이트 아님 — SiteBuilder 처럼 Report 의 where 로만). 유대 특성 레벨업 끝 재판정(BondRecheckComponent)의 판정 불가·예외.
        public const string WhereBondRecheck = "bond-recheck";
        public static readonly IReadOnlyList<string> Sites =new[] { SiteBuilder, SiteSmiteTarget, SiteRoundOnce, SiteSmiteTransfer, SiteChannelHeal, SiteChannelDice, SiteUnyielding, SiteRiposte, SiteOathShield, SiteMountHp, SiteSoulHarvest };

        const string Prefix = "OathboundRevenant: ";

        readonly Action<LedgerLevel, string> sink;
        readonly HashSet<string> armed = new HashSet<string>();
        readonly List<string> injectedLines = new List<string>();

        public int Count { get; private set; }
        public int Injected { get; private set; }
        public IReadOnlyList<string> InjectedLines { get { return injectedLines; } }

        public ErrorLedger(Action<LedgerLevel, string> sink)
        {
            if (sink == null) throw new ArgumentNullException("sink");
            this.sink = sink;
        }

        static void RequireSite(string site)
        {
            if (site == null || !Sites.Contains(site)) throw new ArgumentException("OathboundRevenant: unknown fault-injection site '" + site + "' (known: " + string.Join(", ", Sites.ToArray()) + ")", "site");
        }









        // 무장돼 있으면 해제하고 true, 아니면 false.
        public bool TakeArmed(string site)
        {
            RequireSite(site);
            return armed.Remove(site);
        }

        public void Report(string where, string what, bool injected)
        {
            if (string.IsNullOrEmpty(where)) throw new ArgumentException("OathboundRevenant: Report without a location", "where");
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

        // ErrorSink(Guard 가 부르는 면).
        public void Report(string site, string what) { Report(site, what, false); }
        public void ReportInjected(string site, string what) { Report(site, what, true); }

        // 뜨거운 경로용: 같은 (where, key) 는 한 번만 보고하고 그 뒤는 SuppressedRepeats 만 센다.
        readonly HashSet<string> reportedOnce = new HashSet<string>();
        public int SuppressedRepeats { get; private set; }

        public bool ReportOnce(string where, string key, string what)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("OathboundRevenant: ReportOnce without a dedupe key", "key");
            if (!reportedOnce.Add(where + "|" + key)) { SuppressedRepeats++; return false; }
            Report(where, what, false);
            return true;
        }




    }
}
