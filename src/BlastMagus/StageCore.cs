using System;
using System.Collections.Generic;
using System.Linq;

namespace BlastMagus
{
     
    // 게임 쪽(StageTwo.cs KeeStage)은 여기의 판정·트랜잭션 엔진·1회 규칙을 부르고 그 결과를 따르기만 한다(시험 `kee: hook uses …`).

    // 설계 §4.3-4a 상태 표 7행. 점검 판정은 허용 목록(KeeStatus.PassingStates) — 목록 밖은 전부 FAIL.
    public enum KeeState { Absent, AbsentDisabled, PresentOK, PresentFailed, RollbackFailed, NotRun, Stage1Failed }

     
    // 기본값(default(RegSnapshot)) = 기록 안 됨(Recorded false) — 소비자는 그 값을 받으면 던진다(결측을 「KEE 없음」으로 읽지 않는다, C1).
    public struct RegSnapshot
    {
        public readonly bool Recorded, Found, Enabled;

        public RegSnapshot(bool found, bool enabled)
        {
            // UMM 에 없는 모드는 켜져 있을 수 없다(FindMod == null → Enabled 를 읽을 항목이 없다). 그런 입력은 호출자 결함.
            if (!found && enabled) throw new ArgumentException("BlastMagus: RegSnapshot(found: false, enabled: true) is impossible (no UMM entry to be enabled)");
            Recorded = true; Found = found; Enabled = enabled;
        }

        public void Require(string who)
        {
            if (!Recorded) throw new InvalidOperationException("BlastMagus: " + who + " needs the KEE registration snapshot, but it was not recorded (KeeStage.RecordSnapshot must run in Main.Load before CheckRunner.Begin)");
        }

        public override string ToString() { return Recorded ? "found=" + Found + " enabled=" + Enabled : "not recorded"; }
    }

    public enum TxOutcome { Committed, RolledBack, RollbackFailed }

    // StageTransaction.Run 의 결과. Cause = 실패 원인(Committed 면 null — 원인이 없다는 뜻이 정의상 맞는 유일한 경우).
    public sealed class TxResult
    {
        public readonly TxOutcome Outcome;
        public readonly string Cause;

        public TxResult(TxOutcome outcome, string cause)
        {
            if (!Enum.IsDefined(typeof(TxOutcome), outcome)) throw new ArgumentOutOfRangeException("outcome", outcome, "BlastMagus: undefined TxOutcome");
            if ((outcome == TxOutcome.Committed) != (cause == null)) throw new ArgumentException("BlastMagus: TxResult " + outcome + " with cause '" + cause + "' (Committed has no cause, failures must name one)");
            Outcome = outcome; Cause = cause;
        }
    }

    // KeeStatus.Decide 의 입력(계획 T5 Interfaces + R-PF16). 게임 쪽은 판정 시점의 UMM 값(Found/Enabled/Active)과 등록 스냅샷을 함께 넣는다.
    public sealed class KeeInput
    {
        public bool HookRan;             // 2단계 훅이 판정에 이르렀는가(false = NotRun; S0·kee-stage-built 가 Verdict 없음일 때 쓴다 — R-PF22)
        public bool Stage1Attempted;     // ModContext.BuildAttempted
        public bool Stage1Failed;        // ModContext.BuildFailed
        public bool Found, Enabled, Active; // 판정 시점의 UMM 값(FindMod != null, ModEntry.Enabled, ModEntry.Active)
        public RegSnapshot Snapshot;     // Main.Load 시점 값(R-PF16: 지금 값과 다르면 PresentFailed)
        public string ContractError;     // A 검증 단계 실패 원인(null = A 단계 통과 또는 아직 안 함 — B 단계 결과 Transaction 과 함께 해석된다)
        public TxResult Transaction;     // B 쓰기 단계 결과(null = 실행 안 함)
    }

    // 판정 결과(설계 §4.3-4a 표의 한 행 + 원인).
    public sealed class KeeVerdict
    {
        public KeeState State;
        public Tuple<bool?, string, string> ElementStatus { get; internal set; }
        public string Cause;        // PresentFailed·RollbackFailed 만 원인이 있다(나머지 null)
        public string LogLine;      // UMM·Player.log 줄. NotRun 은 null(설계 표 「(없음) — 그래서 S0 이 잡는다」)
        public bool LogIsError;
        public bool CheckPasses;    // 허용 목록(KeeStatus.PassingStates) 안인가
        public string ResultLine;   // 결과 파일 줄 `kee: state=… [cause=…]`(CheckRunner.Info, S0 사유)
        public bool RegistersS14;   // = KeeStatus.RegistersS14(입력 스냅샷)
        public string S14SkipReason; // RegistersS14 면 null, 아니면 KeeStatus.S14SkipReason
    }

    public static class KeeStatus
    {
        // 설계 §4.3-4a 「점검 판정」 열: PASS 는 이 셋뿐(Absent = 선택 모드 부재 예외 §11-B, disabled = O10 종결, PresentOK). 목록 밖 = FAIL.
        public static readonly KeeState[] PassingStates = { KeeState.Absent, KeeState.AbsentDisabled, KeeState.PresentOK };

        // 설계 §4.3-4 KEE Harmony id·UMM id(Guids.Kee.ModId) — 로그 문구의 모드 이름. 로그 문구는 설계 §4.3-4a 표 그대로.
        const string ModName = "KineticistElementsExpanded";
         
        public const string SnapshotMismatchCause = "UMM state changed between registration and stage 2";
         
        public const string NotActiveCause = "KEE enabled but not active (load error?)";

         
        public static bool RegistersS14(RegSnapshot s)
        {
            s.Require("KeeStatus.RegistersS14()");
            return s.Found && s.Enabled;
        }

        // 설계 §4.3-4a 점검 경로 ②: Absent 면 `KineticistElementsExpanded not installed`, disabled 면 `… disabled`. 등록되는 스냅샷에 사유를 물으면 결함 → 던진다.
        public static string S14SkipReason(RegSnapshot s)
        {
            if (RegistersS14(s)) throw new InvalidOperationException("BlastMagus: S14SkipReason asked for a snapshot that registers S14 (" + s + ")");
            return s.Found ? ModName + " disabled" : ModName + " not installed";
        }

        // 2단계가 A·B 단계로 들어가는가(그 밖의 입력은 Decide 로 바로 끝난다). = 스냅샷 일치 ∧ Found ∧ Enabled ∧ Active, 1단계 성공.
        public static bool ShouldBuild(KeeInput i)
        {
            if (i == null) throw new ArgumentNullException("i");
            if (!i.HookRan || !i.Stage1Attempted || i.Stage1Failed) return false;
            i.Snapshot.Require("KeeStatus.ShouldBuild()");
            return !Mismatch(i) && i.Found && i.Enabled && i.Active;
        }

        static bool Mismatch(KeeInput i) { return i.Snapshot.Found != i.Found || i.Snapshot.Enabled != i.Enabled; }

        // 설계 §4.3-4a 상태 표. 입력이 표의 어느 행에도 맞지 않으면(모순) 던진다 — 모순을 「정상」으로 읽지 않는다.
        public static KeeVerdict Decide(KeeInput i)
        {
            if (i == null) throw new ArgumentNullException("i");
            int keeRows = ElementTable.Kee.Length, vanillaRows = ElementTable.Vanilla.Length; // 「5」·「6」 = 표의 행 수(설계 §4.3-4a)
            if (!i.HookRan) return Make(KeeState.NotRun, null, i, keeRows, vanillaRows);
            if (!i.Stage1Attempted) throw new ArgumentException("BlastMagus: KeeStatus.Decide with HookRan but stage 1 not attempted (StageOnce.Begin returns NotYet there; no verdict is made)");
            if (i.Stage1Failed) return Make(KeeState.Stage1Failed, null, i, keeRows, vanillaRows);
            i.Snapshot.Require("KeeStatus.Decide()");
            if (!i.Found && (i.Enabled || i.Active)) throw new ArgumentException("BlastMagus: KeeStatus.Decide: Found false but Enabled=" + i.Enabled + " Active=" + i.Active);
            bool built = i.ContractError != null || i.Transaction != null;
            if (Mismatch(i)) return NoBuild(i, built, Make(KeeState.PresentFailed, SnapshotMismatchCause, i, keeRows, vanillaRows));
            if (!i.Found) return NoBuild(i, built, Make(KeeState.Absent, null, i, keeRows, vanillaRows));
            if (!i.Enabled) return NoBuild(i, built, Make(KeeState.AbsentDisabled, null, i, keeRows, vanillaRows));
            if (!i.Active) return NoBuild(i, built, Make(KeeState.PresentFailed, NotActiveCause, i, keeRows, vanillaRows));
            if (i.ContractError != null)
            {
                if (i.Transaction != null) throw new ArgumentException("BlastMagus: KeeStatus.Decide: both a stage-A contract error and a stage-B transaction result (stage B must not run after stage A failed)");
                if (i.ContractError.Length == 0) throw new ArgumentException("BlastMagus: KeeStatus.Decide: empty contract error");
                return Make(KeeState.PresentFailed, i.ContractError, i, keeRows, vanillaRows);
            }
            if (i.Transaction == null) throw new ArgumentException("BlastMagus: KeeStatus.Decide: KEE active, stage A passed, but no stage-B transaction result");
            switch (i.Transaction.Outcome)
            {
                case TxOutcome.Committed: return Make(KeeState.PresentOK, null, i, keeRows, vanillaRows);
                case TxOutcome.RolledBack: return Make(KeeState.PresentFailed, i.Transaction.Cause, i, keeRows, vanillaRows);
                case TxOutcome.RollbackFailed: return Make(KeeState.RollbackFailed, i.Transaction.Cause, i, keeRows, vanillaRows);
                default: throw new ArgumentOutOfRangeException("i", i.Transaction.Outcome, "BlastMagus: undefined TxOutcome");
            }
        }

        // A·B 단계 결과가 있는데 그 단계로 들어갈 수 없던 입력 = 호출자 결함.
        static KeeVerdict NoBuild(KeeInput i, bool built, KeeVerdict v)
        {
            if (built) throw new ArgumentException("BlastMagus: KeeStatus.Decide: stage A/B results given for a " + v.State + " input (stage 2 must not build then)");
            return v;
        }

        static KeeVerdict Make(KeeState s, string cause, KeeInput i, int keeRows, int vanillaRows)
        {
            var v = new KeeVerdict { State = s, Cause = cause, CheckPasses = PassingStates.Contains(s) };
            switch (s)
            {
                case KeeState.Absent:
                    v.ElementStatus = Tuple.Create<bool?, string, string>(true, "기본 원소 " + vanillaRows + "종 · 확장 모드 미설치",
                        vanillaRows + " base elements · expansion not installed");
                    v.LogLine = ModName + " not installed: " + keeRows + " elements not offered";
                    v.ResultLine = "kee: state=Absent(not installed)";
                    break;
                case KeeState.AbsentDisabled:
                    v.ElementStatus = Tuple.Create<bool?, string, string>(true, "기본 원소 " + vanillaRows + "종 · 확장 모드 꺼짐",
                        vanillaRows + " base elements · expansion disabled");
                    v.LogLine = ModName + " not active (disabled): " + keeRows + " elements not offered";
                    v.ResultLine = "kee: state=Absent(disabled)";
                    break;
                case KeeState.PresentOK:
                    v.ElementStatus = Tuple.Create<bool?, string, string>(true, "원소 " + (vanillaRows + keeRows) + "종 (기본 " + vanillaRows + "종, 확장 " + keeRows + "종)",
                        (vanillaRows + keeRows) + " elements (" + vanillaRows + " base, " + keeRows + " expansion)");
                    v.LogLine = "BlastMagus: KEE stage 2: " + keeRows + " elements offered (" + string.Join(", ", ElementTable.Kee.Select(r => r.Id.ToString()).ToArray()) + ")";
                    v.ResultLine = "kee: state=PresentOK";
                    break;
                case KeeState.PresentFailed:
                    v.ElementStatus = Tuple.Create<bool?, string, string>(true, "기본 원소 " + vanillaRows + "종 사용 가능 · 확장 로드 실패",
                        vanillaRows + " base elements available · expansion failed (" + cause + ")");
                    v.LogLine = "BlastMagus: KEE stage 2 FAILED (" + cause + "); " + vanillaRows + " elements offered";
                    v.LogIsError = true;
                    v.ResultLine = "kee: state=PresentFailed cause=" + cause;
                    break;
                case KeeState.RollbackFailed:
                    v.ElementStatus = Tuple.Create<bool?, string, string>(false, "원소 복구 실패 · 원소 목록을 확인할 수 없습니다",
                        "Element rollback failed · element availability is unknown (" + cause + ")");
                    v.LogLine = "BlastMagus: KEE stage 2 ROLLBACK FAILED (" + cause + "); element state inconsistent";
                    v.LogIsError = true;
                    v.ResultLine = "kee: state=RollbackFailed cause=" + cause;
                    break;
                case KeeState.NotRun:
                    v.LogLine = null; // 설계 표: 로그 없음(훅이 판정하지 않았다) — S0 이 잡는다
                    v.ResultLine = "kee: state=NotRun";
                    break;
                case KeeState.Stage1Failed:
                    v.ElementStatus = Tuple.Create<bool?, string, string>(false, "기본 원소 생성 실패", "Base element build failed");
                    v.LogLine = "BlastMagus: stage 2 skipped (stage 1 failed)";
                    v.LogIsError = true;
                    v.ResultLine = "kee: state=Stage1Failed";
                    break;
                default: throw new ArgumentOutOfRangeException("s", s, "BlastMagus: undefined KeeState");
            }
            if ((cause != null) != (s == KeeState.PresentFailed || s == KeeState.RollbackFailed))
                throw new InvalidOperationException("BlastMagus: KeeStatus internal: state " + s + " with cause '" + cause + "'");
            // 스냅샷은 모든 상태에서 필요하다(S14 등록 여부가 판정 결과에 붙는다). 기록 안 됐으면 RegistersS14 가 던진다.
            v.RegistersS14 = RegistersS14(i.Snapshot);
            v.S14SkipReason = v.RegistersS14 ? null : S14SkipReason(i.Snapshot);
            return v;
        }








    }

     
    // 규칙: 적용을 Add 순서대로 → 예외 또는 finalCheck 위반이면 「적용이 끝난 단계만」 역순 되돌림(되돌림 예외는 모아 둔다) → rollbackCheck →
    // 되돌림 예외도 위반도 없으면 RolledBack(원인), 하나라도 있으면 RollbackFailed(원인 + 되돌림 문제). Run 은 한 번만.
    public sealed class StageTransaction
    {
        sealed class StepDef { public string Name; public Action Apply, Undo; }
        readonly List<StepDef> steps = new List<StepDef>();
        bool ran;

        // 실행 기록(apply:<이름> / undo:<이름> / final / rollback-check) — 시험이 순서를 단언한다.
        public readonly List<string> Trace = new List<string>();

        public int Count { get { return steps.Count; } }

        public void Add(string name, Action apply, Action undo)
        {
            if (ran) throw new InvalidOperationException("BlastMagus: StageTransaction.Add after Run");
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("BlastMagus: StageTransaction step name is empty");
            if (apply == null) throw new ArgumentNullException("apply");
            if (undo == null) throw new ArgumentNullException("undo");
            if (steps.Any(s => s.Name == name)) throw new ArgumentException("BlastMagus: StageTransaction duplicate step " + name);
            steps.Add(new StepDef { Name = name, Apply = apply, Undo = undo });
        }

        public TxResult Run(Func<string> finalCheck, Func<string> rollbackCheck)
        {
            if (finalCheck == null) throw new ArgumentNullException("finalCheck");
            if (rollbackCheck == null) throw new ArgumentNullException("rollbackCheck");
            if (ran) throw new InvalidOperationException("BlastMagus: StageTransaction.Run called twice");
            ran = true;
            if (steps.Count == 0) throw new InvalidOperationException("BlastMagus: StageTransaction.Run with no steps");
            var applied = new List<StepDef>();
            string cause = null;
            foreach (var s in steps)
            {
                Trace.Add("apply:" + s.Name);
                try { s.Apply(); }
                catch (Exception e) { cause = "write '" + s.Name + "' threw " + e.GetType().Name + ": " + e.Message; break; }
                applied.Add(s);
            }
            if (cause == null)
            {
                Trace.Add("final");
                string v;
                try { v = finalCheck(); }
                catch (Exception e) { v = "final check threw " + e.GetType().Name + ": " + e.Message; }
                if (v == null) return new TxResult(TxOutcome.Committed, null);
                cause = "final check: " + v;
            }
            var problems = new List<string>();
            for (int k = applied.Count - 1; k >= 0; k--)
            {
                Trace.Add("undo:" + applied[k].Name);
                try { applied[k].Undo(); }
                catch (Exception e) { problems.Add("undo '" + applied[k].Name + "' threw " + e.GetType().Name + ": " + e.Message); }
            }
            Trace.Add("rollback-check");
            string rc;
            try { rc = rollbackCheck(); }
            catch (Exception e) { rc = "rollback check threw " + e.GetType().Name + ": " + e.Message; }
            if (rc != null) problems.Add("after rollback: " + rc);
            if (problems.Count == 0) return new TxResult(TxOutcome.RolledBack, cause);
            return new TxResult(TxOutcome.RollbackFailed, cause + "; rollback: " + string.Join("; ", problems.ToArray()));
        }
    }

    // 2단계 캐시 규칙(설계 §4.3-4a A·B.1). 게임 쪽은 캐시 판독(isCached)만 넘긴다.
    public static class KeeChecks
    {
        // 행들의 우리 원소 특성 GUID 중 캐시에 이미 있는 것. A 검증 단계(BlueprintUtil.Create 는 있으면 던진다 — src/Common/BlueprintUtil.cs:30)와 되돌림 뒤 되읽기가 「빈 목록」을 요구한다.
        public static string[] OurGuidsCached(IEnumerable<ElementRow> rows, Func<string, bool> isCached)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (isCached == null) throw new ArgumentNullException("isCached");
            var list = rows.ToList();
            if (list.Count == 0 || list.Any(r => r == null)) throw new ArgumentException("BlastMagus: OurGuidsCached needs a non-empty row list without nulls", "rows");
            return list.Where(r => isCached(r.OurFeatureGuid)).Select(r => r.Id + ":" + r.OurFeatureGuid).ToArray();
        }

        // 설계 §4.3-4a B.1·Global Constraints: BlueprintsCache.RemoveCachedBlueprint 는 「우리 GUID 에만」 — 그중에서도 2단계가 만든 KEE 행의 우리 특성만.
        // 그 밖(본편 GUID, 1단계의 우리 블루프린트)이면 던진다. 게임 쪽 유일한 RemoveCachedBlueprint 호출 지점이 이것을 먼저 부른다(시험 `kee: RemoveCachedBlueprint only on our GUIDs`).
        public static void RequireRemovable(string guid)
        {
            if (guid == null) throw new ArgumentNullException("guid");
            if (!ElementTable.Kee.Any(r => r.OurFeatureGuid == guid))
                throw new InvalidOperationException("BlastMagus: refusing to remove blueprint " + guid + " from the cache (only the stage-2 KEE element features may be removed)");
        }
    }

    public enum StageOnceDecision { Proceed, NotYet, AlreadyDone }

     
    // 그 뒤의 호출은 AlreadyDone(쓰기·로그 없음). 실패 뒤 재시도 없음.
    public sealed class StageOnce
    {
        bool consumed;

        public StageOnceDecision Begin(bool stage1Attempted)
        {
            if (consumed) return StageOnceDecision.AlreadyDone;
            if (!stage1Attempted) return StageOnceDecision.NotYet;
            consumed = true;
            return StageOnceDecision.Proceed;
        }
    }
}
