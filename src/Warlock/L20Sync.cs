using System;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace Warlock
{
     
    // BuildAll 은 공용 BlueprintsCache.Init 포스트픽스에서 돈다 — TTT-Base 의 Witch_AlternateCapstone 포스트픽스(L20 에 WitchAlternateCapstone 추가, 정찰 §7)보다
    // 먼저일 수 있다. 그래서 모든 Init 포스트픽스가 끝난 뒤의 첫 OnUpdate 프레임에 한 번, 라이브 WitchProgression L20 을 다시 읽어 새 제거 항목만 RemoveAt(20, …) 한다.
    // 상태 4개(RemoveRule.L20SyncState): NotAttempted(아직 — 성공 아님) · BuildFailed · Done · SyncFailed. 게이트 판정은 순수 RemoveRule.GateL20.
    // 점검 러너 BlueprintsBuilt 는 Done 만 받는다(Main.Load).
    public static class L20Sync
    {
        public static L20SyncState State { get; private set; }

        // Main.Load 가 modEntry.OnUpdate += L20Sync.Tick 로 건다. 매 프레임 불리지만 판정은 한 번.
        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            switch (RemoveRule.GateL20(State, ModContext.BuildAttempted, ModContext.BuildFailed))
            {
                case L20Gate.Idle:
                    return; // 정당한 무동작 — 이미 한 번 판정했다(Done/BuildFailed/SyncFailed 는 다시 바뀌지 않는다)
                case L20Gate.Wait:
                    return; // 정당한 무동작 — BlueprintsCache.Init 이 아직(첫 Tick 이 Init 전일 수 있다). 「모름」이라 실패로 읽지 않고 기다린다
                case L20Gate.ReportBuildFailed:
                    State = L20SyncState.BuildFailed;
                    ModContext.LogError("Warlock: L20 sync skipped: BuildAll failed (archetype not built; L20 hex leak not checked)");
                    return;
                case L20Gate.Run:
                    try
                    {
                        SyncNow();
                        State = L20SyncState.Done;
                    }
                    catch (Exception e)
                    {
                        State = L20SyncState.SyncFailed;
                        ModContext.LogError("Warlock: L20 sync failed: " + e);
                    }
                     
                    try
                    {
                        PrestigeBlock.ApplyForeignItems();
                    }
                    catch (Exception e)
                    {
                        ModContext.LogError("Warlock: foreign prestige item block failed: " + e);
                    }
                    return;
                default:
                    throw new InvalidOperationException("Warlock: unknown L20 gate result");
            }
        }

        static void SyncNow()
        {
            // BuildAttempted && !BuildFailed 인데 아키타입이 없으면 불변식 위반(BuildAll 이 성공했다면 Archetype.Build 가 돌았다) → 던진다(SyncFailed + ERROR).
            if (Archetype.Bp == null) throw new InvalidOperationException("Warlock: L20 sync: BuildAll succeeded but Archetype.Bp is null");
            var plan = RemoveRule.PlanL20(Archetype.ReadL20(), Archetype.ResolveHexNode);
            var added = Archetype.ApplyL20Plan(plan, "first-frame sync");
            ModContext.Log(RemoveRule.L20SyncLine(added, plan.Unknown, plan.Kept));
        }
    }
}
