using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock
{
     
    // 재적용 사건·리스펙 최종 원본 = Final · OnPostLoad = VerifyOnly(로드 중 사실 추가·제거 금지 — 어긋나면 ERROR 만).
    // O3-1(2026-11-14, 정찰 §16-T4a): 리스펙 커밋(UnitHelper.RespecOnCommit)은 최종 원본에 newUnit 의 JSON 을 덮어쓴 뒤(@0624 PopulateObject)
    // ContextData<UnitEntityData.Respec> 범위(@06B6 Request ~ @06CA Dispose) 안에서 PostLoad(@06C3)를 부르고, 그 뒤 @0A5C 에서 리스펙 처리기를 발행한다.
    // 그 PostLoad 시점의 기록·키트는 newUnit 것(미리보기라 조정 안 됨)이라 어긋남이 정상이다 → RespecLoad = 검증을 처리기 뒤로 미룬다(사실 변경 없음, ERROR 없음).
    // AfterRespec = 처리기의 Final 조정 뒤 검증(어긋나면 ERROR — 「리스펙이 끝났는데도 어긋남」).
    public enum PactMode { Activate, Final, VerifyOnly, RespecLoad, AfterRespec }

    // 계약 컴포넌트의 진입점(엔진 사건) — PactRule.Route 가 진입점 + 엔진 리스펙 신호 + 미뤄 둔 검증 유무로 실행할 모드를 정한다.
    // Activate = OnActivate · LevelUp = HandleUnitReapplyFeaturesOnLevelUp · RespecEnd = HandleUnitChangedAfterRespec(Owner) · PostLoad = OnPostLoad.
    public enum PactEntry { Activate, LevelUp, RespecEnd, PostLoad }

    public sealed class PactRoute
    {
        public readonly PactMode[] Modes;  // 이 순서로 Run(mode)
        public readonly bool PendingAfter; // 진입 뒤 「리스펙 검증 미룸」 상태(게임 쪽 PactRespec 이 유닛마다 보관 — 저장 안 함)
        public readonly PactLine[] Lines;  // Run 전에 낼 줄(미뤄 둔 검증이 처리기 없이 남음 = ERROR)
        public PactRoute(PactMode[] modes, bool pendingAfter, PactLine[] lines) { Modes = modes; PendingAfter = pendingAfter; Lines = lines; }
    }

    // 로그 등급. Debug·Info·Warn = ModContext.Log(접두 DEBUG/INFO/WARN), Error = ModContext.LogError(점검 S15 no-mod-errors 가 센다).
    public enum PactLevel { Debug, Info, Warn, Error }

    // 키트 처리: None = 손대지 않음 · Reconcile = KitRule.Reconcile 적용(사실 추가·제거) · Verify = 차이만 보고(VerifyOnly 전용).
    public enum PactKit { None, Reconcile, Verify }

    // [순수] 「키마다 1회」 집합의 세대 범위(리뷰 T3 m-7): 세대(게임 쪽 = 지금 불러온 세이브의 Player 개체 — ThreadedGameLoader.DoLoad @0021 이 새로 단다)가 바뀌면 비운다.
    // 그래서 같은 세이브를 다시 불러오면 같은 유닛의 ERROR 가 다시 나온다(세션 전역이면 두 번째 로드부터 숨는다). 세대 null = 판정 불가 → 던진다.
    public sealed class OnceSet
    {
        readonly HashSet<string> keys = new HashSet<string>();
        object generation;
        public bool Add(object gen, string key)
        {
            if (gen == null) throw new ArgumentNullException("gen", "Warlock: once-set generation is null (no loaded game)");
            if (key == null) throw new ArgumentNullException("key");
            if (!ReferenceEquals(gen, generation)) { keys.Clear(); generation = gen; }
            return keys.Add(key);
        }
    }

    // [순수] 「미룬 검증」 표식 집합(리뷰 T4 I-2(a)·m-5): Set(true) 는 표시, Set(false) 는 지운다. 세대(= 불러온 세이브의 Player)가 바뀌면 비운다 — 리스펙은 한 세이브 안에서 끝나므로
    // 다른 세이브를 불러온 뒤에 옛 표시가 「처리기 없음」 거짓 ERROR 를 내지 않게. 세대 null·빈 키 = 판정 불가 → 던진다.
    public sealed class PendingSet
    {
        readonly HashSet<string> keys = new HashSet<string>();
        object generation;
        void Scope(object gen, string key)
        {
            if (gen == null) throw new ArgumentNullException("gen", "Warlock: pending-set generation is null (no loaded game)");
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Warlock: pending-set needs a key");
            if (!ReferenceEquals(gen, generation)) { keys.Clear(); generation = gen; }
        }
        public bool IsPending(object gen, string key) { Scope(gen, key); return keys.Contains(key); }
        public void Set(object gen, string key, bool value) { Scope(gen, key); if (value) keys.Add(key); else keys.Remove(key); }
    }

    public sealed class PactLine
    {
        public readonly PactLevel Level;
        public readonly string OnceKey; // null = 매번(Debug 정상 대기 줄 · Route 의 리스펙 미완 ERROR — 그 사건마다 1줄), 아니면 이 키로 1회(게임 쪽 UnknownOnce 집합)
        public readonly string Text;
        public PactLine(PactLevel level, string onceKey, string text) { Level = level; OnceKey = onceKey; Text = text; }
    }

    public sealed class PactPlan
    {
        public readonly PactLine[] Lines;
        public readonly bool WriteRecord;   // true 면 Data.FirstPatronGuid/GrantedGroupGuid 를 RecordFirst/RecordGroup 으로 쓴다
        public readonly string RecordFirst;
        public readonly Group? RecordGroup;
        public readonly PactKit Kit;
        public readonly Group? KitGroup;    // Kit != None 일 때만 값
        public PactPlan(PactLine[] lines, bool writeRecord, string recordFirst, Group? recordGroup, PactKit kit, Group? kitGroup)
        {
            Lines = lines; WriteRecord = writeRecord; RecordFirst = recordFirst; RecordGroup = recordGroup; Kit = kit; KitGroup = kitGroup;
        }
    }

    // [순수] 판정(PatronGroups.Decide) → 적용 계획. 게임 쪽(WarlockPactComponent.Run)은 이 계획만 따른다(로그·기록·키트) — 판정 규칙의 실행 시험이 여기에 붙는다.
     
    public static class PactRule
    {
        static string Guids(PactDecision d) { return "[" + string.Join(", ", d.Guids) + "]"; }

        // 추가 후견인 Notes → 줄. 같은 그룹 INFO, 다른 그룹·미매핑(어둠의 계약 포함) WARN — 키 = 유닛 + 종류 + GUID(1회).
        static IEnumerable<PactLine> NoteLines(PactDecision d, string unit)
        {
            foreach (var n in d.Notes)
            {
                if (n == null || n.Guid == null) throw new ArgumentException("Warlock: PactDecision has a null note");
                var level = n.Kind == NoteKind.SecondSameGroup ? PactLevel.Info : PactLevel.Warn;
                yield return new PactLine(level, unit + "|" + n.Kind + "|" + n.Guid, "Warlock: " + unit + " has an extra patron " + n.Guid + " (" + n.Kind + "); the first patron " + d.Guids[0] + " keeps group " + d.Group);
            }
        }

        public static PactPlan Plan(PactDecision d, PactMode mode, string unit)
        {
            if (d == null) throw new ArgumentNullException("d");
            if (string.IsNullOrEmpty(unit)) throw new ArgumentException("Warlock: PactRule.Plan needs a unit key");
            if (!Enum.IsDefined(typeof(PactMode), mode)) throw new ArgumentOutOfRangeException("mode", mode, "Warlock: unknown pact mode");
            if (d.Guids == null || d.Notes == null) throw new ArgumentException("Warlock: PactDecision with null Guids/Notes");
            var none = new PactLine[0];
            // 리스펙 커밋 안의 PostLoad: 기록·키트는 newUnit 의 것(조정 전) — 어떤 판정이든 처리기(RespecEnd → Final, AfterRespec)가 다시 본다. 사실·기록 변경 없음.
            if (mode == PactMode.RespecLoad)
                return new PactPlan(new[] { new PactLine(PactLevel.Debug, null, "Warlock: " + unit + " loaded inside a respec commit (" + d.Kind + "); verify deferred to HandleUnitChangedAfterRespec") }, false, null, null, PactKit.None, null);
            if (mode == PactMode.AfterRespec)
            {
                switch (d.Kind)
                {
                    case PactKind.Grant:
                    case PactKind.RecoverFromRecord:
                        // 처리기의 Final 이 그룹을 부여했어야 한다 — 여전히 그룹 0 = 리스펙이 어긋난 채 끝남.
                        if (d.Group == null || d.Guids.Length != 1) throw new ArgumentException("Warlock: " + d.Kind + " decision without a group / first patron");
                        return new PactPlan(new[] { new PactLine(PactLevel.Error, unit + "|" + d.Kind + "|respec", "Warlock: respec mismatch for " + unit + ": patron " + d.Guids[0] + " maps to group " + d.Group.Value + " but the unit still holds no warlock group after the respec handler") }, false, null, null, PactKit.None, null);
                    case PactKind.Keep:
                        if (d.Group == null) throw new ArgumentException("Warlock: Keep decision without a group");
                        return new PactPlan(NoteLines(d, unit).ToArray(), false, null, null, PactKit.Verify, d.Group.Value);
                    case PactKind.NoPatron:
                    case PactKind.Ambiguous:
                    case PactKind.Unmapped:
                    case PactKind.RecordMissing:
                    case PactKind.GroupMismatch:
                    case PactKind.FirstPatronMissing:
                    case PactKind.RecordStale:
                        // 같은 처리기의 Final 이 같은 입력으로 같은 판정을 이미 ERROR 로 냈다(아래 NoPatron·판정 불가 분기) — 두 번째 줄은 내지 않는다(정당한 무동작).
                        return new PactPlan(none, false, null, null, PactKit.None, null);
                    default:
                        throw new ArgumentOutOfRangeException("d", d.Kind, "Warlock: unknown pact decision kind");
                }
            }
            switch (d.Kind)
            {
                case PactKind.NoPatron:
                    // L1 생성 중(선택 전)의 OnActivate 는 정상 대기. 조정 사건(선택 적용 뒤)·로드 뒤에 후견인이 없으면 판정 불가 → ERROR.
                    if (mode == PactMode.Activate) return new PactPlan(new[] { new PactLine(PactLevel.Debug, null, "Warlock: " + unit + " has no patron yet (waiting for the L1 selection)") }, false, null, null, PactKit.None, null);
                    return new PactPlan(new[] { new PactLine(PactLevel.Error, unit + "|NoPatron|" + mode, "Warlock: " + unit + " has no witch patron after " + (mode == PactMode.Final ? "level-up/respec" : "load") + " — no warlock group, no blasts") }, false, null, null, PactKit.None, null);

                case PactKind.Grant:
                case PactKind.RecoverFromRecord:
                    {
                        if (d.Group == null || d.Guids.Length != 1) throw new ArgumentException("Warlock: " + d.Kind + " decision without a group / first patron");
                        var g = d.Group.Value;
                        var lines = new List<PactLine>();
                        if (mode == PactMode.VerifyOnly)
                        {
                            // 로드 중에는 사실을 더하지 않는다 — 기대(그룹 1)와 보유(그룹 0)가 어긋난 것 자체를 ERROR 로.
                            lines.Add(new PactLine(PactLevel.Error, unit + "|" + d.Kind + "|load", "Warlock: load mismatch for " + unit + ": patron " + d.Guids[0] + " maps to group " + g + " but the unit holds no warlock group (" + d.Kind + " expected); no change during load"));
                            lines.AddRange(NoteLines(d, unit));
                            return new PactPlan(lines.ToArray(), false, null, null, PactKit.None, null);
                        }
                        if (d.Kind == PactKind.RecoverFromRecord)
                            lines.Add(new PactLine(PactLevel.Warn, unit + "|Recover|" + g, "Warlock: " + unit + " lost its warlock group; recovering " + g + " from the record (first patron " + d.Guids[0] + ")"));
                        else
                            lines.Add(new PactLine(PactLevel.Info, unit + "|Grant|" + g, "Warlock: " + unit + " granted group " + g + " (patron " + d.Guids[0] + ")"));
                        lines.AddRange(NoteLines(d, unit));
                        return new PactPlan(lines.ToArray(), true, d.Guids[0], g, PactKit.Reconcile, g);
                    }

                case PactKind.Keep:
                    {
                        if (d.Group == null) throw new ArgumentException("Warlock: Keep decision without a group");
                        var lines = NoteLines(d, unit).ToArray();
                        return new PactPlan(lines, false, null, null, mode == PactMode.VerifyOnly ? PactKit.Verify : PactKit.Reconcile, d.Group.Value);
                    }

                case PactKind.Ambiguous:
                case PactKind.Unmapped:
                case PactKind.RecordMissing:
                case PactKind.GroupMismatch:
                case PactKind.FirstPatronMissing:
                case PactKind.RecordStale:
                    // 조용히 Keep/Grant 하지 않는다 — 그 유닛+종류마다 ERROR 1회, 키트는 손대지 않는다.
                    return new PactPlan(new[] { new PactLine(PactLevel.Error, unit + "|" + d.Kind, "Warlock: " + unit + " patron decision " + d.Kind + " (patrons " + Guids(d) + ") — group not changed") }, false, null, null, PactKit.None, null);

                default:
                    throw new ArgumentOutOfRangeException("d", d.Kind, "Warlock: unknown pact decision kind");
            }
        }

        // VerifyOnly 의 키트 차이 → ERROR 1회(차이 없음 = 줄 없음). diff = KitRule.Reconcile(Expected, Owned) 결과(Add = 없는 것, Remove = 남는 것).
        public static PactLine[] VerifyLines(string unit, Group g, KitPlan diff) { return VerifyLines(unit, g, diff, PactMode.VerifyOnly); }

        // mode = Verify 키트를 내는 두 모드만: VerifyOnly(로드) · AfterRespec(리스펙 처리기 뒤). 키·문구가 달라 둘이 1회 집합에서 서로를 지우지 않는다.
        public static PactLine[] VerifyLines(string unit, Group g, KitPlan diff, PactMode mode)
        {
            if (string.IsNullOrEmpty(unit)) throw new ArgumentException("Warlock: PactRule.VerifyLines needs a unit key");
            if (diff == null || diff.Add == null || diff.Remove == null) throw new ArgumentNullException("diff");
            if (mode != PactMode.VerifyOnly && mode != PactMode.AfterRespec) throw new ArgumentOutOfRangeException("mode", mode, "Warlock: VerifyLines is for VerifyOnly / AfterRespec only");
            if (diff.Add.Length == 0 && diff.Remove.Length == 0) return new PactLine[0];
            bool load = mode == PactMode.VerifyOnly;
            return new[] { new PactLine(PactLevel.Error, unit + (load ? "|Verify|" : "|RespecVerify|") + g, "Warlock: " + (load ? "load" : "respec") + " mismatch for " + unit + " (group " + g + "): kit missing [" + string.Join(", ", diff.Add) + "], extra [" + string.Join(", ", diff.Remove) + "]; " + (load ? "no change during load" : "after the respec handler")) };
        }

        // 진입점 → 실행할 모드(O3-1). respecScope = 엔진 신호 ContextData<UnitEntityData.Respec>.Current(PostLoad 에서만 읽는다 — 그 범위는 RespecOnCommit @06B6~@06CA 의 PostLoad 하나뿐,
        // 다른 요청처 0: 정찰 §16-T4a). pending = 이 유닛에 미뤄 둔 리스펙 검증이 남아 있는가.
        // 미뤄 둔 검증이 처리기 없이 남은 채 다른 진입(레벨업·일반 로드)이 오면 = 리스펙 처리기가 이 컴포넌트에 닿지 않았다 → ERROR(조용히 지우지 않는다).
        // Activate 는 리스펙 커밋 안(특성 복원)에서도 올 수 있어 상태를 바꾸지 않는다.
        public static PactRoute Route(PactEntry entry, bool respecScope, bool pending, string unit)
        {
            if (string.IsNullOrEmpty(unit)) throw new ArgumentException("Warlock: PactRule.Route needs a unit key");
            if (!Enum.IsDefined(typeof(PactEntry), entry)) throw new ArgumentOutOfRangeException("entry", entry, "Warlock: unknown pact entry");
            if (respecScope && entry != PactEntry.PostLoad) throw new ArgumentException("Warlock: the respec load flag is only read in OnPostLoad (got entry " + entry + ")");
            var none = new PactLine[0];
            Func<string, PactLine[]> stale = where => pending
                ? new[] { new PactLine(PactLevel.Error, null, "Warlock: " + unit + " had a respec load whose verify was deferred, but HandleUnitChangedAfterRespec never reached the pact component (seen at " + where + "); the kit was not verified after that respec") }
                : none;
            switch (entry)
            {
                case PactEntry.Activate: return new PactRoute(new[] { PactMode.Activate }, pending, none);
                case PactEntry.LevelUp: return new PactRoute(new[] { PactMode.Final }, false, stale("level-up"));
                case PactEntry.RespecEnd: return new PactRoute(new[] { PactMode.Final, PactMode.AfterRespec }, false, none);
                case PactEntry.PostLoad:
                    if (respecScope) return new PactRoute(new[] { PactMode.RespecLoad }, true, stale("a second respec load"));
                    return new PactRoute(new[] { PactMode.VerifyOnly }, false, stale("load"));
                default: throw new ArgumentOutOfRangeException("entry", entry, "Warlock: unknown pact entry");
            }
        }

        // 기록 Data.GrantedGroupGuid(그룹 특성 GUID) → 그룹. 6 그룹 특성 밖의 값 = 손상 기록 → 던진다(조용히 null 로 접지 않는다).
        public static Group GroupOfFeature(string featureGuid)
        {
            if (featureGuid == null) throw new ArgumentNullException("featureGuid");
            foreach (var g in PatronGroups.AllGroups) if (KitRule.GroupFeature(g) == featureGuid) return g;
            throw new ArgumentException("Warlock: recorded group GUID " + featureGuid + " is not one of the 6 warlock group features");
        }
    }
}
