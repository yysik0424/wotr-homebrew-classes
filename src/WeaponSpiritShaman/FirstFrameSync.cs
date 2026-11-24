using System;

namespace WeaponSpiritShaman
{
    // 선택지 설명 첫 프레임 갱신의 상태. 진행표 동기화 상태(Done/Failed)와 따로 둔다(외부 검토 2026-11-17 지적 7).
    //  NotRun  = 아직 돌지 않았거나, BuildAll 실패로 건너뛰었다(설명은 빌드 때 문구도 없다 — 아키타입이 등록되지 않았다).
    //  Refreshed = 59개를 전부 계산해 한 번에 반영했다(그중 판정 불가 선택지가 있으면 그 범주마다 따로 오류 줄 — SpiritBlueprints).
    //  Failed  = 계산 또는 반영 중 예외 — 표는 빌드 때 문구 그대로(원자적 반영: StringsTable.ApplyChoices), 오류 줄 1개.
    public enum ChoiceTextState { NotRun, Refreshed, Failed }

    // [순수] 첫 프레임 두 일(펫 진행표 동기화 · 선택지 설명 갱신)의 상태·실패 경계(외부 검토 2026-11-17 지적 7).
    // 예전에는 두 일이 한 try 안에 있어서, 진행표를 이미 바꾼 뒤 설명 갱신이 던지면 Done=false·Failed=true 로 고정되고
    // 흡수 거울의 Ready 진입(MirrorLifecycle — SyncDone=false 면 던진다)이 영영 막혔다. 이제:
    //  · Done/Failed 는 진행표 동기화만의 결과다. 설명 갱신의 성공·실패는 Done/Failed 를 바꾸지 않는다.
    //  · 설명 갱신의 결과는 Texts 에만 남고, 실패는 오류 줄 1개(logError)로 한 번 알린다.
    //  · 두 일은 서로의 결과와 무관하게 각각 한 번 돈다(진행표 실패여도 설명은 갱신한다 — 설명은 펫을 쓰지 않는다).
    //  · BuildAll 실패면 둘 다 돌지 않는다: Failed=true(진행표 오류 줄 1개), Texts=NotRun.
    //  · BuildAttempted=false 동안은 「모름」이므로 아무 일도 하지 않고 기다린다(Ran=false).
    public sealed class FirstFrameSync
    {
        public bool Ran { get; private set; }
        public bool Done { get; private set; }
        public bool Failed { get; private set; }
        public ChoiceTextState Texts { get; private set; }

        public void Run(bool buildAttempted, bool buildFailed, Action syncProgression, Action refreshTexts, Action<string> logError)
        {
            if (syncProgression == null) throw new ArgumentNullException("syncProgression");
            if (refreshTexts == null) throw new ArgumentNullException("refreshTexts");
            if (logError == null) throw new ArgumentNullException("logError");
            if (Ran) return;
            if (!buildAttempted) return; // BlueprintsCache.Init 이 아직 — 「모름」이므로 기다린다(실패로 읽지 않는다)
            Ran = true;
            if (buildFailed)
            {
                Failed = true;
                logError("WeaponSpiritShaman: WSS progression sync skipped: BuildAll failed (pet progression not synced; spirit pets must not be used)");
                return;
            }
            try
            {
                syncProgression();
                Done = true;
            }
            catch (Exception e)
            {
                Failed = true;
                logError("WeaponSpiritShaman: WSS progression sync failed: " + e);
            }
            // 설명 갱신은 진행표 결과를 읽지도 바꾸지도 않는다(경계 분리).
            try
            {
                refreshTexts();
                Texts = ChoiceTextState.Refreshed;
            }
            catch (Exception e)
            {
                Texts = ChoiceTextState.Failed;
                logError("WeaponSpiritShaman: WSS choice texts refresh failed (the choice descriptions stay as built; pet progression sync is not affected): " + e);
            }
        }
    }
}
