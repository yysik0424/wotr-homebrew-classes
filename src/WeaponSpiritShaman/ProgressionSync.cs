using System;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace WeaponSpiritShaman
{
     
    // BuildAll 은 공용 BlueprintsCache.Init 포스트픽스에서 돈다 — TabletopTweaks 의 동물 동료 진행표 재배치 포스트픽스(HarmonyPriority(0))보다 먼저일 수 있다.
    // 그래서 모든 Init 포스트픽스가 끝난 뒤의 첫 OnUpdate 프레임(메인 메뉴·세이브 로드 전)에 한 번, 라이브 AnimalCompanionBaseProgression.LevelEntries 를 다시 읽어
    // ProgressionEdit.RemoveExactlyOnce(GUID 정확히 1회씩) → 우리 진행표 LevelEntries 만 새 항목으로 교체한다(원본은 읽기만, 되돌리기 금지).
    // 상태 3개: 아직(Done=false, Failed=false) · 완료(Done) · 실패(Failed). 실패·BuildAll 실패는 오류 로그 1줄 — 이후 영병 거울의 Ready 진입(MirrorLifecycle, T4)이
    // SyncDone=false 로 던지고, 점검 러너의 BlueprintsBuilt 가 false 가 된다(동기화 없이 펫을 쓰지 않는다).
    // 선택지 설명 갱신(RefreshTexts)은 같은 첫 프레임에 돌지만 상태·실패 경계가 따로다(외부 검토 2026-11-17 지적 7): 그 실패는 Done/Failed 를 바꾸지 않고
    // ChoiceTexts = Failed + 오류 줄 1개로만 남는다. 경계 규칙은 순수 FirstFrameSync 가 갖고(시험이 실행으로 잠근다), 여기는 게임 쪽 일 두 개를 넘길 뿐이다.
    public static class ProgressionSync
    {
        static readonly FirstFrameSync State = new FirstFrameSync();
        public static bool Done { get { return State.Done; } }






        // 첫 프레임 설명 갱신의 결과(바뀐 범주 · 판정 불가 범주). 갱신 전·실패면 null(「모름」 — 읽는 쪽이 판정 불가로 다룬다). 게임 안 점검 S26 first-frame-state 가 판정 불가 0 을 본다.
        static SpiritBlueprints.ChoiceRefresh lastChoiceRefresh;




        // Main.Load 가 modEntry.OnUpdate += ProgressionSync.Tick 로 건다. 매 프레임 불리지만 일은 한 번(FirstFrameSync.Ran).
        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            State.Run(ModContext.BuildAttempted, ModContext.BuildFailed, SyncNow, RefreshTexts, ModContext.LogError);
        }

        // 선택지 설명(2026-11-17): 다른 모드의 패치가 다 끝난 지금의 무기 값으로 다시 만든다 — 빌드 뒤에 무기 유형이 바뀌었으면 문구가 따라간다(바뀐 범주를 로그에 남긴다).
        // 59개를 전부 계산한 뒤 한 번에 반영한다(중간에 던지면 표는 빌드 때 그대로). 판정 불가가 된 범주는 RefreshChoiceTexts 가 범주마다 오류 줄을 쓴다.
        static void RefreshTexts()
        {
            var r = SpiritBlueprints.RefreshChoiceTexts();
            lastChoiceRefresh = r;
            ModContext.Log("WSS choice texts refreshed from live weapon blueprints: " + r.Changed.Length + " changed since build" + (r.Changed.Length == 0 ? "" : " [" + string.Join(", ", r.Changed) + "]")
                + "; undetermined " + r.Undetermined.Length + (r.Undetermined.Length == 0 ? "" : " [" + string.Join(", ", r.Undetermined) + "]"));
        }

        static void SyncNow()
        {
            if (PetClass.Progression == null) throw new InvalidOperationException("WeaponSpiritShaman: ProgressionSync before PetClass.Build");
            var live = Get<BlueprintProgression>(Guids.Game.AnimalCompanionBaseProgression);
            var entries = PetClass.ToEntries(live.LevelEntries);
            var edit = ProgressionEdit.RemoveExactlyOnce(entries, SpiritRules.RemovedPetFeatures);
            PetClass.ReplaceEntries(edit.Kept);
            string removed = string.Join(" ", edit.RemovedAt.Select(r => Name(r.Guid) + "@L" + r.Level).ToArray());
            string levels = string.Join(" ", edit.Kept.Select(e => e.Level + ":" + e.Features.Length).ToArray());
            ModContext.Log("WSS progression synced: removed " + removed + "; source levels " + levels);
        }

        static string Name(string guid)
        {
            if (guid == Guids.Game.AnimalCompanionSlotFeature) return "SlotFeature";
            if (guid == Guids.Game.AnimalType) return "AnimalType";
            if (guid == Guids.Game.Multiattack) return "Multiattack";
            throw new InvalidOperationException("WeaponSpiritShaman: unexpected removed guid " + guid);
        }
    }
}
