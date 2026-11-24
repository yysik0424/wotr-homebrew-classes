using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
     
    // 게임 쪽 RuntimeReadback(RuntimeReadback.cs)이 판독기(블루프린트 캐시 + 리플렉션)를 만들어 RunOn 을 부른다.
    // 「0 차이」는 Status == Complete 이고 CheckedKeys 가 기대 키 집합과 같을 때만 뜻이 있다 — 미실행(NotRun)·빈 입력·읽기 실패(Failed)는 「차이 없음」이 아니다.
    public enum ReadbackStatus { NotRun, Running, Complete, Failed }

    public enum ProbeOutcome { Ok, MissingBlueprint, MissingComponent, AmbiguousComponent, MissingField }

    public sealed class ProbeRead
    {
        public readonly ProbeOutcome Outcome;
        public readonly string Value;   // Ok 일 때만 의미(그 밖에는 null)
        public readonly string Detail;
        ProbeRead(ProbeOutcome o, string v, string d) { Outcome = o; Value = v; Detail = d; }
        public static ProbeRead Ok(string value) { if (value == null) throw new ArgumentNullException("value", "ShadowSorcerer: ProbeRead.Ok(null) - a read value is never null (null references read as \"null\")"); return new ProbeRead(ProbeOutcome.Ok, value, null); }
        public static ProbeRead Fail(ProbeOutcome o, string detail)
        {
            if (o == ProbeOutcome.Ok) throw new ArgumentException("ShadowSorcerer: ProbeRead.Fail(Ok)");
            return new ProbeRead(o, null, detail ?? o.ToString());
        }
    }

    // 판독기. Exists = 캐시에 블루프린트가 있는가, ComponentTypes = 그 블루프린트 컴포넌트의 형식 전체 이름(null 컴포넌트는 null 원소),
    // Read = 행 하나의 값(블루프린트 자체: componentType == "").
    public interface IBlueprintProbe
    {
        bool Exists(string guid);
        IReadOnlyList<string> ComponentTypes(string guid);
        ProbeRead Read(string guid, string componentType, ReadbackPath path);
    }

    public sealed class ReadbackResult
    {
        public ReadbackStatus Status = ReadbackStatus.NotRun;
        public readonly List<string> CheckedKeys = new List<string>();
        public int UnknownDifferences;
        public int FailedRows;
        public int KnownForeign;
        public readonly List<string> Lines = new List<string>();
        // 키 → 읽은 실제 값(판독 Ok 인 행만). 나중 재실행과 칸 단위로 비교한다(ReadbackCore.Compare — 조율자 판정 2026-11-14 「점검 시점 재실행」).
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public override string ToString() { return Status + " keys=" + CheckedKeys.Count + " unknownDifferences=" + UnknownDifferences + " failedRows=" + FailedRows + " knownForeign=" + KnownForeign; }
    }

    public static class ReadbackCore
    {
        public const string Site = ErrorLedger.SiteReadback;

        // rows 를 전부 대조한다. injected = 점검 주입 실행(그 실행의 보고는 주입분으로 센다 — ShadowErrors.TakeArmed("readback") 가 정한다).
        // 행마다: 블루프린트 없음·컴포넌트 없음/둘 이상·필드 없음·판독 예외 → 그 행 실패(키는 기록) + 보고 1. 판정 거부 → UnknownDifferences++ + 보고 1.
        // 끝: 실패 행이 하나라도 있으면 Failed, 아니면 Complete. 빈 목록·null·같은 키 두 번 = Failed + 보고 1(행을 보지 않는다).
        public static ReadbackResult RunOn(IReadOnlyList<Row> rows, IBlueprintProbe probe, ErrorLedger ledger, bool injected)
        {
            if (probe == null) throw new ArgumentNullException("probe");
            if (ledger == null) throw new ArgumentNullException("ledger");
            var r = new ReadbackResult { Status = ReadbackStatus.Running };
            if (rows == null || rows.Count == 0 || rows.Any(x => x == null))
            {
                r.Status = ReadbackStatus.Failed;
                ledger.Report(Site, "no readback rows to check (" + (rows == null ? "null list" : rows.Count == 0 ? "empty list" : "null row") + ") - a readback that checked nothing is not a clean readback", injected);
                return r;
            }
            var dupes = rows.GroupBy(x => x.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            if (dupes.Length > 0)
            {
                r.Status = ReadbackStatus.Failed;
                ledger.Report(Site, "duplicate readback keys: " + string.Join(", ", dupes), injected);
                return r;
            }
            if (!rows.Any(x => x.IsValueRow))
            {
                r.Status = ReadbackStatus.Failed;
                ledger.Report(Site, "readback rows contain only allowances (nothing to compare)", injected);
                return r;
            }
            foreach (var row in rows.Where(x => x.IsValueRow))
            {
                r.CheckedKeys.Add(row.Key);
                try
                {
                    if (!probe.Exists(row.Guid))
                    {
                        Fail(r, ledger, injected, row, "blueprint " + row.Guid + " not found");
                        continue;
                    }
                    var read = probe.Read(row.Guid, row.ComponentType, row.Path);
                    if (read == null) { Fail(r, ledger, injected, row, "probe returned null"); continue; }
                    if (read.Outcome != ProbeOutcome.Ok) { Fail(r, ledger, injected, row, read.Outcome + ": " + read.Detail); continue; }
                    r.Values[row.Key] = read.Value;
                    var extras = ExtraTypes(rows, row, probe);
                    var verdict = ForeignTolerance.Judge(row, read.Value, extras);
                    if (!verdict.Item1)
                    {
                        r.UnknownDifferences++;
                        r.Lines.Add("DIFF " + verdict.Item2);
                        ledger.Report(Site, "unknown difference: " + verdict.Item2 + " [" + row.Key + "]", injected);
                    }
                    else
                    {
                        if (row.Kind == ToleranceKind.KnownForeignValue && read.Value == row.ForeignValue) r.KnownForeign++;
                        r.Lines.Add("OK " + verdict.Item2);
                    }
                }
                catch (Exception e)
                {
                    Fail(r, ledger, injected, row, "probe threw " + e.GetType().Name + ": " + e.Message);
                }
            }
            r.Status = r.FailedRows > 0 ? ReadbackStatus.Failed : ReadbackStatus.Complete;
            return r;
        }

        // 첫 실행(baseline)과 나중 재실행(now)의 차이(조율자 판정 2026-11-14: 첫 OnUpdate 1회만 보면 다른 모드가 나중에 바꾼 값이 조용히 낡는다).
        // 차이 = 기준 상태가 Complete 아님(비교 불가는 「같음」이 아니다) · 재실행 상태 ≠ Complete · 검사 키 집합 차이 · 키마다 값 차이(읽지 못한 값 포함) · 미지 차이 수 변화.
        // 빈 목록 = 같다. 순서 = 위 순서, 키는 서수 정렬(서명이 결정적).
        public static List<string> Compare(ReadbackResult baseline, ReadbackResult now)
        {
            if (baseline == null) throw new ArgumentNullException("baseline");
            if (now == null) throw new ArgumentNullException("now");
            var d = new List<string>();
            if (baseline.Status != ReadbackStatus.Complete) d.Add("baseline status " + baseline.Status + " (not Complete - nothing to compare against)");
            if (now.Status != ReadbackStatus.Complete) d.Add("re-run status " + now.Status + " (was " + baseline.Status + ")");
            var bk = new HashSet<string>(baseline.CheckedKeys);
            var nk = new HashSet<string>(now.CheckedKeys);
            foreach (var k in bk.Except(nk).OrderBy(x => x, StringComparer.Ordinal)) d.Add("key no longer checked: " + k);
            foreach (var k in nk.Except(bk).OrderBy(x => x, StringComparer.Ordinal)) d.Add("key newly checked: " + k);
            foreach (var k in bk.Intersect(nk).OrderBy(x => x, StringComparer.Ordinal))
            {
                string a, b;
                bool ha = baseline.Values.TryGetValue(k, out a), hb = now.Values.TryGetValue(k, out b);
                if (!ha && !hb) continue; // 둘 다 읽지 못함 — 위 상태 차이(Complete 아님)가 이미 차이로 적혔다
                if (!ha || !hb || a != b) d.Add("value changed at " + k + ": '" + (ha ? a : "<unread>") + "' -> '" + (hb ? b : "<unread>") + "'");
            }
            if (baseline.UnknownDifferences != now.UnknownDifferences) d.Add("unknown differences " + baseline.UnknownDifferences + " -> " + now.UnknownDifferences);
            return d;
        }





        static void Fail(ReadbackResult r, ErrorLedger ledger, bool injected, Row row, string why)
        {
            r.FailedRows++;
            r.Lines.Add("FAILED " + row.Name + ": " + why);
            ledger.Report(Site, "row " + row.Name + " [" + row.Key + "] could not be checked: " + why, injected);
        }

        // 형식을 읽는 행(컴포넌트 값 행 또는 Components#type= 개수 행)이 있는 블루프린트는 컴포넌트 형식 집합을 본다: 그 블루프린트의 컴포넌트 형식 중 어떤 행(값·허용)도
        // 이름 붙이지 않은 것. 블루프린트 자체 필드만 읽는 블루프린트(행이 전부 자체 행)는 보지 않는다(빈 목록 — 무령 주술사 사고 규칙).
        // 판정은 블루프린트마다 한 번: 그 블루프린트의 첫 형식 읽기 값 행이 맡는다(같은 미지 형식을 행마다 되풀이해 세지 않는다).
        // (T3 독립 검토 I-1: 개수 행만 있는 BurstBarrier·NegatePenalty·TotalCover 에서 형식 집합 검사가 꺼져 있었다.)
        // 이름 붙임 = 컴포넌트 행의 형식(색인 @i 는 떼고) 또는 Components#type= 개수 행의 형식(T2).
        public static bool ReadsTypes(Row x) { return x != null && (x.Component != null || (x.Path != null && x.Path.TypeName != null)); }

        static IEnumerable<string> ExtraTypes(IReadOnlyList<Row> rows, Row row, IBlueprintProbe probe)
        {
            if (!ReadsTypes(row)) return new string[0];
            var anchor = rows.FirstOrDefault(x => x.Guid == row.Guid && x.IsValueRow && ReadsTypes(x));
            if (!ReferenceEquals(anchor, row)) return new string[0];
            var named = new HashSet<string>(rows.Where(x => x.Guid == row.Guid).SelectMany(x => new[] { x.Component == null ? null : x.Component.TypeName, x.Path == null ? null : x.Path.TypeName }).Where(t => t != null));
            var types = probe.ComponentTypes(row.Guid);
            if (types == null) throw new InvalidOperationException("ShadowSorcerer: probe returned no component list for " + row.Guid);
            return types.Where(t => t == null || !named.Contains(t)).Select(t => t ?? "").ToArray();
        }
    }

     
    //  빌드 전: 아무것도 안 함(정당한 무동작 — 아직 모름). 빌드 실패: 실행하지 않음(NotRun 그대로 — S1 이 FAIL 한다), 사유를 1회 기록.
    //  빌드 성공: run 을 정확히 1번. run 이 던지면 Failed 결과 + 오류 보고(report)(조용히 NotRun 으로 남기지 않는다). 빌드 실패 사유는 정보 줄(info) — 빌드 실패 자체가 이미 ERROR 다.
    public sealed class ReadbackOnce
    {
        public ReadbackResult Result { get; private set; }
        public bool SkippedForBuildFailure { get; private set; }
        public int Runs { get; private set; }





        public void Tick(bool buildAttempted, bool buildFailed, Func<ReadbackResult> run, Action<string> report, Action<string> info)
        {
            if (run == null) throw new ArgumentNullException("run");
            if (report == null) throw new ArgumentNullException("report");
            if (info == null) throw new ArgumentNullException("info");
            if (Result != null || SkippedForBuildFailure) return; // 정당한 무동작: 이미 한 번 정해졌다
            if (!buildAttempted) return;                          // 정당한 무동작: 빌드 전(아직 모름)
            if (buildFailed)
            {
                SkippedForBuildFailure = true;
                info("runtime readback not run: blueprint build failed (status stays NotRun; the build failure itself is already an ERROR)");
                return;
            }
            Runs++;
            ReadbackResult got;
            try { got = run(); }
            catch (Exception e)
            {
                Result = new ReadbackResult { Status = ReadbackStatus.Failed };
                report("runtime readback threw " + e.GetType().Name + ": " + e.Message);
                return;
            }
            if (got == null || got.Status == ReadbackStatus.NotRun || got.Status == ReadbackStatus.Running)
            {
                Result = new ReadbackResult { Status = ReadbackStatus.Failed };
                report("runtime readback returned " + (got == null ? "no result" : "status " + got.Status) + " - treated as Failed");
                return;
            }
            Result = got;
        }
    }

    // 지역 로드마다의 재실행 관문(조율자 판정 2026-11-14 — 일반 플레이: 지역 로드 때 다시 돌려 차이가 보이면 UMM 로그에 ERROR 1줄, UI 없음).
    //  기준(주 실행)이 없거나 Complete 아님 → 건너뜀(Skipped++; 주 실행의 실패는 그때 이미 ERROR 로 보고됐다 — 같은 실패를 지역마다 되풀이하지 않는다).
    //  재실행 → Compare. 차이가 있고 그 서명(차이 줄 묶음)이 직전에 보고한 것과 다르면 report 1회(한 줄). 같은 차이가 지역마다 되풀이되면 다시 보고하지 않는다.
    //  차이가 사라지면 서명을 지운다(다시 생기면 다시 보고). rerun 이 던지면 report 1회(그 실행은 차이 1로 센다).
    public sealed class RecheckGate
    {
        public int Runs { get; private set; }
        public int Skipped { get; private set; }
        public int Reports { get; private set; }
        public int LastDifferenceCount { get; private set; }
        public string LastSignature { get; private set; }

        public List<string> Observe(ReadbackResult baseline, Func<ReadbackResult> rerun, Action<string> report)
        {
            if (rerun == null) throw new ArgumentNullException("rerun");
            if (report == null) throw new ArgumentNullException("report");
            if (baseline == null || baseline.Status != ReadbackStatus.Complete) { Skipped++; return new List<string>(); }
            Runs++;
            List<string> diffs;
            try
            {
                var now = rerun();
                diffs = now == null ? new List<string> { "re-run returned no result" } : ReadbackCore.Compare(baseline, now);
            }
            catch (Exception e) { diffs = new List<string> { "re-run threw " + e.GetType().Name + ": " + e.Message }; }
            LastDifferenceCount = diffs.Count;
            if (diffs.Count == 0) { LastSignature = null; return diffs; }
            string sig = string.Join(" | ", diffs.ToArray());
            if (sig != LastSignature)
            {
                LastSignature = sig;
                Reports++;
                report("runtime readback changed after the first check (" + diffs.Count + " difference(s)): " + sig);
            }
            return diffs;
        }
    }
}
