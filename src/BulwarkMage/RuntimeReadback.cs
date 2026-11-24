using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.PubSubSystem;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
     
    // 모든 모드의 BlueprintsCache.Init Postfix 가 끝난 뒤 첫 Main.OnUpdate(ModContext.BuildAttempted && !BuildFailed)에 ForeignTolerance.Rows 를 1회 대조한다.
    // 판정·상태는 순수 ReadbackCore / ReadbackOnce(시험 exe 가 실행), 여기는 판독기(블루프린트 캐시 + 리플렉션)와 연결만.
    // 나중 변형(조율자 판정 2026-11-14): 점검 S1 은 그 시점에 다시 돌려 주 실행과 비교하고(차이 = FAIL), 일반 플레이는 지역 로드마다 다시 돌려 차이가 새로 보이면
    // UMM 로그에 ERROR 1줄(RecheckGate). 한계(README): 지역 로드 사이의 변형은 다음 지역 로드까지 안 보인다. 설정 화면 경고 줄은 없다 — UMM 설정 화면(OnGUI)은 IMGUI 참조가 필요한데 build.ps1 참조 목록에 없다(tools 무수정, 보고서 미결).
    public static class RuntimeReadback
    {
        static readonly ReadbackOnce once = new ReadbackOnce();

















        // Main.Load 가 modEntry.OnUpdate 에 붙인다(점검 러너보다 먼저). 정당한 무동작: 빌드 전 / 이미 실행함.
        // 주 실행이 끝난 뒤(Runs > 0) 지역 로드 재실행 처리기를 1번 구독한다(조율자 판정 2026-11-14). 구독 되읽기 실패 = ERROR(조용히 재실행 없이 남지 않는다).
        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            once.Tick(ModContext.BuildAttempted, ModContext.BuildFailed, Run,
                msg => BulwarkErrors.Report(ReadbackCore.Site, msg),
                msg => ModContext.Log("BulwarkMage: " + msg));
            if (once.Runs > 0 && areaHook == null) SubscribeAreaHook();
        }

        // ---- 점검 시점·지역 로드 재실행(조율자 판정 2026-11-14: 첫 OnUpdate 1회만 보면 나중 변형이 조용히 낡는다) ----
        // 재실행은 주 상태(once)를 바꾸지 않고, 오류 장부에도 쓰지 않는다(조용한 장부 — 차이는 호출자가 판정·보고한다). 「readback」 주입 무장도 소비하지 않는다.
        static readonly RecheckGate areaGate = new RecheckGate();
        static AreaHook areaHook;

















        public sealed class RecheckResult
        {
            public ReadbackResult Live;
            public List<string> Differences;
        }

        static ReadbackResult LiveRun()
        {
            return ReadbackCore.RunOn(ForeignTolerance.Rows, new CacheProbe(), new ErrorLedger((level, line) => { }), false);
        }





        // 지역 로드 처리기 본문. 엔진은 전역 구독자의 예외를 삼킨다 → 여기서 잡아 오류 계약으로 센다.
        static void OnAreaLoaded()
        {
            try { areaGate.Observe(once.Result, LiveRun, msg => BulwarkErrors.Report(ReadbackCore.Site, msg)); }
            catch (Exception e) { BulwarkErrors.ReportException(ReadbackCore.Site, e); }
        }

        static void SubscribeAreaHook()
        {
            var h = new AreaHook();
            areaHook = h; // 구독 실패여도 다시 시도하지 않는다(매 프레임 오류 반복 방지) — 실패는 1번 ERROR, S1 이 AreaRechecks == 0 으로 FAIL
            try
            {
                EventBus.Subscribe(h);
                if (!EventBus.IsGloballySubscribed(h)) BulwarkErrors.Report(ReadbackCore.Site, "area-load re-check subscription did not register (EventBus.IsGloballySubscribed false) - later changes by other mods will not be seen");
            }
            catch (Exception e) { BulwarkErrors.ReportException(ReadbackCore.Site, e); }
        }

        sealed class AreaHook : IAreaHandler
        {
            public void OnAreaBeginUnloading() { } // 정당한 무동작: 로드 완료만 본다
            public void OnAreaDidLoad() { OnAreaLoaded(); }
        }

        // 주 실행 — 진짜 실행이라 주입분이 아니다(TakeArmed 를 부르지 않는다).
        static ReadbackResult Run()
        {
            var r = ReadbackCore.RunOn(ForeignTolerance.Rows, new CacheProbe(), BulwarkErrors.Ledger, false);
            Summarize("main", r);
            return r;
        }





        static void Summarize(string what, ReadbackResult r)
        {
            ModContext.Log("BulwarkMage: runtime readback (" + what + "): " + r);
            foreach (var l in r.Lines) ModContext.Log("BulwarkMage:   " + l);
        }

        // 판독기: 블루프린트 캐시(BlueprintsCache.Load — 없으면 null, BlueprintUtil.Get 과 같은 경로) + 필드 리플렉션(BlueprintUtil.FindField, 기반 형식까지).
        sealed class CacheProbe : IBlueprintProbe
        {
            static SimpleBlueprint Load(string guid) { return ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(guid)); }

            public bool Exists(string guid) { return Load(guid) != null; }

            public IReadOnlyList<string> ComponentTypes(string guid)
            {
                var bp = Load(guid) as BlueprintScriptableObject;
                if (bp == null) return null;
                return (bp.ComponentsArray ?? new BlueprintComponent[0]).Select(c => c == null ? null : c.GetType().FullName).ToArray();
            }

            public ProbeRead Read(string guid, string componentType, ReadbackPath path)
            {
                if (path == null) throw new ArgumentNullException("path");
                var bp = Load(guid);
                if (bp == null) return ProbeRead.Fail(ProbeOutcome.MissingBlueprint, guid);
                object owner = bp;
                var spec = ComponentSpec.Parse(componentType);
                if (path.TypeName != null)
                {
                    // Components#type=<형식>: 블루프린트 자체 행만(Row 생성자가 강제). 정확히 같은 런타임 형식의 개수.
                    if (spec != null) return ProbeRead.Fail(ProbeOutcome.MissingField, "component count path on a component row");
                    var types = ComponentTypes(guid);
                    if (types == null) return ProbeRead.Fail(ProbeOutcome.MissingComponent, bp.GetType().Name + " has no components");
                    return ProbeRead.Ok(types.Count(t => t == path.TypeName).ToString(CultureInfo.InvariantCulture));
                }
                if (spec != null)
                {
                    var so = bp as BlueprintScriptableObject;
                    if (so == null) return ProbeRead.Fail(ProbeOutcome.MissingComponent, bp.GetType().Name + " has no components");
                    var hits = (so.ComponentsArray ?? new BlueprintComponent[0]).Where(c => c != null && c.GetType().FullName == spec.TypeName).ToArray();
                    if (hits.Length == 0) return ProbeRead.Fail(ProbeOutcome.MissingComponent, spec.TypeName);
                    if (spec.Index.HasValue)
                    {
                        if (spec.Index.Value >= hits.Length) return ProbeRead.Fail(ProbeOutcome.MissingComponent, spec.TypeName + "@" + spec.Index.Value + " (only " + hits.Length + ")");
                        owner = hits[spec.Index.Value];
                    }
                    else
                    {
                        if (hits.Length > 1) return ProbeRead.Fail(ProbeOutcome.AmbiguousComponent, hits.Length + " x " + spec.TypeName);
                        owner = hits[0];
                    }
                }
                // 점 경로: 마지막 조각 앞까지 안쪽 객체로 내려간다. 중간이 null 이면 읽기 실패(「null」 값으로 비교하지 않는다).
                for (int i = 0; i < path.Segments.Count - 1; i++)
                {
                    var mid = FindField(owner.GetType(), path.Segments[i]);
                    if (mid == null) return ProbeRead.Fail(ProbeOutcome.MissingField, owner.GetType().FullName + "." + path.Segments[i]);
                    owner = mid.GetValue(owner);
                    if (owner == null) return ProbeRead.Fail(ProbeOutcome.MissingField, "null at " + string.Join(".", path.Segments.Take(i + 1).ToArray()));
                }
                string last = path.Segments[path.Segments.Count - 1];
                var f = FindField(owner.GetType(), last);
                if (f == null) return ProbeRead.Fail(ProbeOutcome.MissingField, owner.GetType().FullName + "." + last);
                object v = f.GetValue(owner);
                if (path.CountGuid == null) return ProbeRead.Ok(Format(v));
                if (path.Level.HasValue)
                {
                    var entries = v as LevelEntry[];
                    if (entries == null) return ProbeRead.Fail(ProbeOutcome.MissingField, path.Field + " is not a LevelEntry[] (" + (v == null ? "null" : v.GetType().Name) + ")");
                    int n = 0;
                    foreach (var e in entries.Where(e => e != null && e.Level == path.Level.Value))
                    {
                        var refs = GetF(e, "m_Features") as IEnumerable;
                        if (refs == null) return ProbeRead.Fail(ProbeOutcome.MissingField, "LevelEntry.m_Features is null at level " + path.Level.Value);
                        foreach (var x in refs) if (GuidOf(x) == path.CountGuid) n++;
                    }
                    return ProbeRead.Ok(n.ToString(CultureInfo.InvariantCulture));
                }
                var seq = v as IEnumerable;
                if (seq == null || v is string) return ProbeRead.Fail(ProbeOutcome.MissingField, path.Field + " is not a collection (" + (v == null ? "null" : v.GetType().Name) + ")");
                int count = 0;
                foreach (var x in seq)
                {
                    string g = GuidOf(x);
                    if (g == null && x != null) return ProbeRead.Fail(ProbeOutcome.MissingField, path.Field + " holds a " + x.GetType().Name + " (not a blueprint reference)");
                    if (g == path.CountGuid) count++;
                }
                return ProbeRead.Ok(count.ToString(CultureInfo.InvariantCulture));
            }

            static string GuidOf(object x)
            {
                var r = x as BlueprintReferenceBase;
                if (r != null) return r.Guid.ToString();
                var b = x as SimpleBlueprint;
                if (b != null) return b.AssetGuid.ToString();
                return null;
            }

            static string Format(object v)
            {
                if (v == null) return "null";
                var r = v as BlueprintReferenceBase;
                if (r != null) return r.Guid.ToString();
                var f = v as IFormattable;
                if (f != null) return f.ToString(null, CultureInfo.InvariantCulture);
                return v.ToString();
            }

            static object GetF(object o, string name)
            {
                var f = FindField(o.GetType(), name);
                if (f == null) throw new MissingFieldException("BulwarkMage: " + o.GetType().Name + "." + name + " not found");
                return f.GetValue(o);
            }
        }
    }
}
