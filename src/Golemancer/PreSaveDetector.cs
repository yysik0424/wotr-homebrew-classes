using System;

namespace Golemancer
{
    // [순수] 파트 OnPreSave 검출(계획 T2·T5): 저장 전 분리(FusionPreSavePatch → EndAllBeforeSave)가 끝났어야 하는데 Active·Ending 이 남아 있으면 ERROR(상태 유지 — 검출만).
    // Idle·AwaitRestore 는 저장해도 되는 상태(AwaitRestore = 종료 실패를 다음 로드에서 재시도하도록 저장, WSS FusionLifecycle Loaded). 게임 형식 없음.
    public enum PreSaveVerdict { Keep, ErrorKeep }

    public static class PreSaveDetector
    {
        public static PreSaveVerdict Decide(FusionState s)
        {
            switch (s)
            {
                case FusionState.Idle:
                case FusionState.AwaitRestore:
                    return PreSaveVerdict.Keep;
                case FusionState.Active:
                case FusionState.Ending:
                    return PreSaveVerdict.ErrorKeep;
            }
            throw new ArgumentException("PreSaveDetector.Decide: undefined fusion state " + (int)s);
        }
    }
}
