using System;
using System.Collections.Generic;
using System.Linq;

namespace Spellfist
{
     
    public static class SchoolListRules
    {
        // GUID 비교 규칙: 32자 hex 이므로 대소문자 무시 서수 비교(BlueprintGuid.Parse = Guid.TryParse 도 대소문자 무시).
        public static readonly StringComparer GuidComparer = StringComparer.OrdinalIgnoreCase;

        // 되읽은 한 칸. Guid == null = 참조가 해석되지 않음, School == null = SpellComponent 없음(학파 판정 불가).
        public struct ReadbackEntry
        {
            public string Guid;
            public string Name;
            public string School;
        }

        // 중복 제거 규칙(이름: 첫 등장 유지). 한 레벨 안에서 같은 GUID 가 두 번 이상 나오면 첫 번째만 남기고 나머지를 removed 로 돌려준다(순서 유지).
        // 근거: 엔진 GetSpells 는 중복을 거르지 않는다(SpellLevelList.get_SpellsFiltered IL_0031~005A = Where(!IsDlcRestricted) 뿐) → 그대로 두면 같은 주문이 두 칸.
        // 호출자는 removed 를 INFO 로 남긴다(조용히 버리지 않는다). GUID 가 null 이면 판정 불가 → throw.
        public static List<T> DedupeKeepFirst<T>(IEnumerable<T> items, Func<T, string> guidOf, out List<T> removed)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (guidOf == null) throw new ArgumentNullException("guidOf");
            var seen = new HashSet<string>(GuidComparer);
            var kept = new List<T>();
            removed = new List<T>();
            int i = 0;
            foreach (var it in items)
            {
                var g = guidOf(it);
                if (string.IsNullOrEmpty(g)) throw new InvalidOperationException("Spellfist: DedupeKeepFirst: item #" + i + " has no GUID (cannot decide duplicate)");
                if (seen.Add(g)) kept.Add(it); else removed.Add(it);
                i++;
            }
            return kept;
        }

        // 한 레벨 되읽기 대조. 만든 GUID 집합(expected)과 저장된 m_Spells 를 해석한 결과(actual)를 같은 것끼리 비교한다:
        // 고유 GUID 집합 동일 + 각 칸이 해석됨 + 각 칸의 실제 SpellComponent.School 이 이 목록의 학파 + 어느 쪽에도 중복 없음.
        // 문제마다 한 줄(GUID·이름·실제 학파·출처)을 돌려준다. 빈 목록 = 일치. 호출자가 비어 있지 않으면 throw.
        public static List<string> CompareLevel(string school, IList<string> expected, IList<ReadbackEntry> actual, Func<string, string> sourceOf)
        {
            if (string.IsNullOrEmpty(school)) throw new ArgumentException("Spellfist: CompareLevel: school is empty");
            if (expected == null) throw new ArgumentNullException("expected");
            if (actual == null) throw new ArgumentNullException("actual");
            if (sourceOf == null) throw new ArgumentNullException("sourceOf");
            var problems = new List<string>();

            foreach (var g in expected.GroupBy(x => x, GuidComparer).Where(x => x.Count() > 1))
                problems.Add("built list has " + g.Key + " " + g.Count() + " times [" + sourceOf(g.Key) + "]");
            var exp = new HashSet<string>(expected, GuidComparer);

            var resolved = new List<ReadbackEntry>();
            for (int i = 0; i < actual.Count; i++)
            {
                if (string.IsNullOrEmpty(actual[i].Guid)) problems.Add("read-back slot #" + i + " is an unresolved reference");
                else resolved.Add(actual[i]);
            }
            foreach (var g in resolved.GroupBy(x => x.Guid, GuidComparer).Where(x => x.Count() > 1))
                problems.Add(g.First().Name + " " + g.Key + " appears " + g.Count() + " times in the read-back level [" + sourceOf(g.Key) + "]");
            var act = new HashSet<string>(resolved.Select(x => x.Guid), GuidComparer);

            foreach (var g in exp.Where(x => !act.Contains(x)))
                problems.Add(g + " built but not read back [" + sourceOf(g) + "]");
            foreach (var e in resolved.Where(x => !exp.Contains(x.Guid)).GroupBy(x => x.Guid, GuidComparer).Select(x => x.First()))
                problems.Add(e.Name + " " + e.Guid + " read back but not built [" + sourceOf(e.Guid) + "]");
            foreach (var e in resolved.GroupBy(x => x.Guid, GuidComparer).Select(x => x.First()))
            {
                if (e.School == null) problems.Add(e.Name + " " + e.Guid + " has no school (no SpellComponent) [" + sourceOf(e.Guid) + "]");
                else if (e.School != school) problems.Add(e.Name + " " + e.Guid + " is " + e.School + ", list is " + school + " [" + sourceOf(e.Guid) + "]");
            }
            return problems;
        }

        // ---- 첫 프레임 재동기화(2026-11-17, 소유자 결정 「게임 시작 뒤 다시 맞춤」) ----
        // CharacterOptionsPlus 는 새 주문(ChillTouch 3f184aa1… 등)을 BlueprintCore AddToSpellLists 로 만들고, 그 목록 추가는 지연 구성
        // (BlueprintCacheInitHandler.AfterBlueprintCachePatches → RootConfigurator.ConfigureDelayedBlueprints, CharacterOptionsPlus.dll dnlib 2026-11-17)에서 한다.
        // 그 지연 구성은 TTT-Core 의 StartGameLoader.LoadPackTOC Postfix 가 부른다 = 우리 BlueprintsCache.Init 빌드보다 뒤(UMM 로그 11709 「Spellfist blueprints built」 → 11761 「COP+Main Configuring feats delayed」).
        // LoadPackTOC = { BlueprintsCache.Init(); ret } 이고 GameStarter.StartGameCoroutine.MoveNext @037C 가 한 번에 부른다(Assembly-CSharp dnlib) → 빌드 뒤 첫 OnUpdate 프레임은 그 Postfix 뒤다.

        // 한 칸(학파 목록 한 레벨의 주문 하나). Guid == null 은 판독 불가 — Diff 가 던진다.
        public struct SpellAt
        {
            public string Guid;
            public string Name;
            public string School;
            public int Level;
        }

        public sealed class ResyncDiff
        {
            public List<SpellAt> Added = new List<SpellAt>();
            public List<SpellAt> Removed = new List<SpellAt>();
        }

        // [순수] 재동기화 전(지금 우리 목록) · 뒤(지금 메이거스 목록을 다시 가른 것)의 차이. 같은 칸 = (학파, 레벨, GUID 대소문자 무시).
        // GUID·학파가 비었거나 레벨이 음수면 판정 불가 → 던진다(조용히 빼지 않는다). 같은 칸이 한쪽에 두 번이면 던진다(가르기 전에 DedupeKeepFirst 가 돌아야 한다).
        public static ResyncDiff Diff(IList<SpellAt> before, IList<SpellAt> after)
        {
            if (before == null) throw new ArgumentNullException("before");
            if (after == null) throw new ArgumentNullException("after");
            Func<SpellAt, string> key = s =>
            {
                if (string.IsNullOrEmpty(s.Guid) || string.IsNullOrEmpty(s.School) || s.Level < 0)
                    throw new InvalidOperationException("Spellfist: ResyncDiff: entry " + (s.Name ?? "<no name>") + " has no GUID / school / level (cannot compare)");
                return s.School + "|" + s.Level + "|" + s.Guid.ToLowerInvariant();
            };
            var b = new Dictionary<string, SpellAt>();
            foreach (var s in before) { var k = key(s); if (b.ContainsKey(k)) throw new InvalidOperationException("Spellfist: ResyncDiff: " + k + " twice in the current lists"); b[k] = s; }
            var a = new Dictionary<string, SpellAt>();
            foreach (var s in after) { var k = key(s); if (a.ContainsKey(k)) throw new InvalidOperationException("Spellfist: ResyncDiff: " + k + " twice in the re-split magus list"); a[k] = s; }
            var d = new ResyncDiff();
            foreach (var s in after) if (!b.ContainsKey(key(s))) d.Added.Add(s);
            foreach (var s in before) if (!a.ContainsKey(key(s))) d.Removed.Add(s);
            return d;
        }

        // [순수] 재동기화 결과 로그 1줄(추가·제거 주문을 모두 적는다 — 개수만 적지 않는다).
        public static string DescribeResync(ResyncDiff d, int totalBefore, int totalAfter)
        {
            if (d == null) throw new ArgumentNullException("d");
            Func<IEnumerable<SpellAt>, string> list = xs => xs.Any() ? string.Join(", ", xs.Select(s => s.Name + " " + s.Guid + " " + s.School + " L" + s.Level).ToArray()) : "none";
            return "Spellfist: INFO school lists re-synced from MagusSpellList on the first frame (after all mods' blueprint patches): "
                + "added " + d.Added.Count + " [" + list(d.Added) + "], removed " + d.Removed.Count + " [" + list(d.Removed) + "], total " + totalBefore + " -> " + totalAfter;
        }

        // 재동기화 상태 3+1: 아직(NotRun) · 맞춤(Synced) · 실패(Failed — 목록은 빌드 때 그대로) · 건너뜀(Skipped — BuildAll 실패로 목록 자체가 없다).
        // 점검 S1 school-list-resync 와 S2·S3·S7 의 아는 주문 단계가 Synced 가 아니면 FAIL 한다(NotRun 을 「바뀐 것 없음」으로 읽지 않는다).
        public enum ResyncState { NotRun, Synced, Failed, Skipped }

        // [순수] 첫 프레임 한 번 실행 경계. buildAttempted=false 동안은 「모름」이라 기다린다(Ran 아님). 한 번 돈 뒤에는 다시 돌지 않는다.
        // resync 가 던지면 Failed + 오류 줄 1개(목록 유지는 resync 자신의 책임 — SchoolLists.Resync 가 적용 전 계산·적용 실패 시 되돌림).
        public sealed class ResyncGate
        {
            public ResyncState State { get; private set; }
            public string LastLine { get; private set; }

            public void Run(bool buildAttempted, bool buildFailed, Func<string> resync, Action<string> log, Action<string> logError)
            {
                if (resync == null) throw new ArgumentNullException("resync");
                if (log == null) throw new ArgumentNullException("log");
                if (logError == null) throw new ArgumentNullException("logError");
                if (State != ResyncState.NotRun) return;
                if (!buildAttempted) return;
                if (buildFailed)
                {
                    State = ResyncState.Skipped;
                    LastLine = "Spellfist: school lists re-sync skipped: BuildAll failed (no school lists to re-sync)";
                    logError(LastLine);
                    return;
                }
                string line;
                try
                {
                    line = resync();
                    if (string.IsNullOrEmpty(line)) throw new InvalidOperationException("Spellfist: school lists re-sync returned no summary line");
                }
                catch (Exception e)
                {
                    State = ResyncState.Failed;
                    LastLine = "Spellfist: school lists re-sync failed - the school lists stay as built (spells another mod added to MagusSpellList after our build are missing from them): " + e;
                    logError(LastLine);
                    return;
                }
                State = ResyncState.Synced;
                LastLine = line;
                log(line);
            }
        }
    }
}
