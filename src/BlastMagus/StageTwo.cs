using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using WotrClassMod.Common;



using UnityModManager = UnityModManagerNet.UnityModManager;

namespace BlastMagus
{
     
    // 그래서 같은 메서드의 Postfix 로, KEE 뒤(HarmonyAfter)·맨 끝(Priority.Last — 같은 Postfix 의 다른 모드가 더한 비전까지 Resync 가 읽게, 설계 §4.3-4) 돈다.
    // 우리 모드의 유일한 Harmony 패치(시험 `kee: harmony attributes`). 대상 문자열은 tools\patch-targets\BlastMagus.txt 가 verify-patch-targets.ps1 로 확인한다.
    [HarmonyPatch(typeof(StartGameLoader), nameof(StartGameLoader.LoadAllJson))]
    [HarmonyAfter(Guids.Kee.ModId)]
    [HarmonyPriority(Priority.Last)]
    static class StageTwoHook
    {
        // 몸통 전부 try/catch, 다시 던지지 않는다 — BuildHook.cs:21-35 와 같은 이유(모드 오류로 게임 로딩을 멈추지 않는다). 실패는 KeeStage 가 오류 로그·판정으로 드러낸다.
        [HarmonyPostfix]
        static void Postfix()
        {
            PoolCompatibility.SyncAfterBlueprints();
            try { KeeStage.Run(); }
            catch (Exception e) { KeeStage.Crashed(e); }
        }
    }

    // 2단계 상태·게임 쪽 쓰기(설계 §4.3-4a). 판단은 순수 코어(StageCore.cs: KeeStatus·StageTransaction·StageOnce·KeeChecks)가 하고 여기는 그 결과를 따른다.
    public static class KeeStage
    {
         
        public static RegSnapshot RegSnapshot { get; private set; }
        // 2단계 판정(R-PF22: 판정 전 null). S0·S1·S14 는 CurrentVerdict() 로 읽는다.
        public static KeeVerdict Verdict { get; private set; }
        static string stageError;
        static readonly Tuple<bool?, string, string> pending = Tuple.Create<bool?, string, string>(null,
            "원소 확장 확인 대기", "Waiting for the element expansion check");

        // The bundle reads this after the base blueprints have been built.
        // Null means the second hook is pending. True includes a usable base-only fallback.
        public static Tuple<bool?, string, string> ElementStatus
        {
            get
            {
                var verdict = Verdict;
                if (verdict != null && verdict.State != KeeState.NotRun) return verdict.ElementStatus;
                return stageError == null ? pending : Tuple.Create<bool?, string, string>(false,
                    "원소 확장 확인 중 오류가 발생했습니다", "Element expansion status could not be determined (" + stageError + ")");
            }
        }

        static readonly StageOnce Once = new StageOnce();
        static KeeInput current; // 판정 입력(예외 처리기 Crashed 가 남은 판정을 내릴 때 쓴다)

         
        // Enabled 가 이 시점에 설정에서 채워졌는지는 미확인(설계 같은 절, R-PF24) — 2단계 대조가 어긋남을 PresentFailed 로 잡는다.
        public static void RecordSnapshot()
        {
            if (RegSnapshot.Recorded) throw new InvalidOperationException("BlastMagus: KeeStage.RecordSnapshot called twice");
            var mod = UnityModManager.FindMod(Guids.Kee.ModId);
            RegSnapshot = mod == null ? new RegSnapshot(false, false) : new RegSnapshot(true, mod.Enabled);
            ModContext.Log("KEE registration snapshot (Main.Load): " + RegSnapshot);
        }





        internal static void Run()
        {
            switch (Once.Begin(ModContext.BuildAttempted))
            {
                case StageOnceDecision.NotYet:
                    ModContext.Log("KEE stage 2 not started: StartGameLoader.LoadAllJson ran before stage 1 (BlueprintsCache.Init) was attempted; waiting for the next call");
                    return;
                case StageOnceDecision.AlreadyDone:
                    return; // 정당한 무동작: 1회 규칙(설계 §4.3-4a) — 종료 상태 뒤의 호출은 쓰기·로그 없이 끝난다
                case StageOnceDecision.Proceed:
                    break;
                default: throw new InvalidOperationException("BlastMagus: undefined StageOnce decision");
            }
            var input = new KeeInput { HookRan = true, Stage1Attempted = true, Stage1Failed = ModContext.BuildFailed, Snapshot = RegSnapshot };
            current = input;
            if (input.Stage1Failed) { Finish(KeeStatus.Decide(input)); return; } // R-PF21: 판정 + Info + 오류 로그, Resync 없음

            // 비전 재동기화(설계 §4.6-2 의 2번): KEE 와 무관 — 실패는 오류 로그만, KEE 상태는 그대로 진행.
            try { PoolArcana.Resync(); }
            catch (Exception e)
            {
                ModContext.LogError("BlastMagus: stage 2 arcana resync failed (KEE state unaffected): " + e.GetType().Name + ": " + e.Message);
                ModContext.Entry.Logger.LogException(e);
            }

            var mod = UnityModManager.FindMod(Guids.Kee.ModId);
            input.Found = mod != null;
            input.Enabled = mod != null && mod.Enabled;
            input.Active = mod != null && mod.Active;
            if (!KeeStatus.ShouldBuild(input)) { Finish(KeeStatus.Decide(input)); return; }

            ResolvedElement[] kee;
            try { kee = StageA(); }
            catch (Exception e)
            {
                input.ContractError = "stage A: " + e.Message + (e.Message.Contains("does not resolve") ? " (KEE's LoadAllJson postfix may not have run before ours — patch order)" : "");
                Finish(KeeStatus.Decide(input));
                return;
            }
            input.Transaction = StageB(kee);
            Finish(KeeStatus.Decide(input));
        }

         
        static ResolvedElement[] StageA()
        {
            var rows = ElementTable.Kee;
            var kee = rows.Select(ElementModel.Resolve).ToArray();
            BlastBlueprints.CheckKeeCandidates(kee);
            var present = KeeChecks.OurGuidsCached(rows, IsCached);
            if (present.Length > 0) throw new InvalidOperationException("our KEE element feature GUID(s) already in the blueprint cache: " + string.Join(", ", present));
            return kee;
        }

        // B 쓰기 단계(설계 §4.3-4a B 1~5): 스냅샷 → 적용/되돌림 쌍(R-PF15: 쓰기 1 = 원소마다 따로) → 최종 되읽기 / 되돌림 뒤 되읽기.
        static TxResult StageB(ResolvedElement[] kee)
        {
            var snap = BlastBlueprints.TakeSnapshot();
            var made = new BlueprintFeature[kee.Length];
            var tx = new StageTransaction();
            for (int k = 0; k < kee.Length; k++)
            {
                int i = k;
                tx.Add("create " + kee[i].Row.Id, () => made[i] = BlastBlueprints.CreateElementFeature(kee[i]), () => BlastBlueprints.RemoveKeeFeature(kee[i].Row));
            }
            tx.Add("tables", () => BlastBlueprints.ApplyKeeTables(kee), () => BlastBlueprints.Restore(snap, BlastBlueprints.SnapshotPart.Tables));
            tx.Add("composite", () => BlastBlueprints.ApplyCompositeComponents(kee), () => BlastBlueprints.Restore(snap, BlastBlueprints.SnapshotPart.Composite));
            tx.Add("selection", () => BlastBlueprints.ApplySelection(made), () => BlastBlueprints.Restore(snap, BlastBlueprints.SnapshotPart.Selection));
            return tx.Run(() => FinalCheck(kee, made), () => RollbackCheck(snap));
        }

         
        static string FinalCheck(ResolvedElement[] kee, BlueprintFeature[] made)
        {
            var v = BlastBlueprints.VerifyElements(ElementTable.All.Length);
            if (v != null) return v;
            for (int i = 0; i < kee.Length; i++)
            {
                try { ElementModel.CheckOurFeature(made[i], kee[i].Row); Energize.VerifyGrant(made[i], kee[i].Row.Id); }
                catch (Exception e) { return e.Message; }
            }
            return null;
        }

        // 되돌림 뒤 되읽기: 스냅샷 대조 + 우리 KEE 원소 특성 GUID 가 캐시에 없음.
        static string RollbackCheck(BlastBlueprints.Snapshot snap)
        {
            var v = BlastBlueprints.VerifySnapshot(snap);
            if (v != null) return v;
            var left = KeeChecks.OurGuidsCached(ElementTable.Kee, IsCached);
            return left.Length == 0 ? null : "our KEE element feature GUID(s) still cached: " + string.Join(", ", left);
        }

        // 캐시 판독(R-PF14: 공용 BlueprintUtil.IsCached 의 모드 쪽 사본 — 공용 무수정). 필드 원장 = CommonLookups(src/Common/ReflectionLookupTypes.cs:28).
        static FieldInfo loadedField;
        static bool IsCached(string guid)
        {
            if (loadedField == null)
            {
                loadedField = typeof(BlueprintsCache).GetField("m_LoadedBlueprints", BindingFlags.NonPublic | BindingFlags.Instance);
                if (loadedField == null) throw new MissingFieldException("BlastMagus: BlueprintsCache.m_LoadedBlueprints not found");
            }
            var dict = loadedField.GetValue(ResourcesLibrary.BlueprintsCache) as IDictionary;
            if (dict == null) throw new InvalidOperationException("BlastMagus: BlueprintsCache.m_LoadedBlueprints is not an IDictionary");
            return dict.Contains(BlueprintGuid.Parse(guid));
        }

        static void Finish(KeeVerdict v)
        {
            Verdict = v;
            if (v.LogLine != null)
            {
                if (v.LogIsError) ModContext.LogError(v.LogLine);
                else ModContext.Log(v.LogLine);
            }
             



        }

        // Postfix 의 예외 처리기. 오류 로그 + (판정 전이고 쓰기 단계 전이면) PresentFailed 판정. 자기 자신도 던지지 않는다.
        internal static void Crashed(Exception e)
        {
            try
            {
                stageError = e.GetType().Name + ": " + e.Message;
                ModContext.LogError("BlastMagus: KEE stage 2 crashed: " + e);
                ModContext.Entry.Logger.LogException(e);
                if (Verdict == null && current != null && KeeStatus.ShouldBuild(current) && current.Transaction == null && current.ContractError == null)
                {
                    current.ContractError = "stage 2 crashed: " + e.GetType().Name + ": " + e.Message;
                    Finish(KeeStatus.Decide(current));
                }
            }
            catch (Exception e2)
            {
                // 마지막 수단: 로그 경로 자체가 실패. 판정은 null 로 남아 S0 이 NotRun 으로 FAIL 한다.
                UnityEngine.Debug.LogException(e2);
            }
        }
    }
}
