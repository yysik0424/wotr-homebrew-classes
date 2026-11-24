using System;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;

namespace WotrClassMod.Common
{
    // 블루프린트 캐시가 준비된 직후 모드의 BuildAll(ModContext.Init 으로 받음)을 한 번 부른다.
    // 실패해도 게임 부팅은 막지 않는다: 오류를 로그에 남기고 ModContext.BuildFailed 를 세운다(점검 러너·패치가 읽는다).
    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    static class BlueprintsCacheInitPatch
    {
        static bool built;

        [HarmonyPrepare]
        static bool Prepare() { return !BundleHooks.Enabled; }

        [HarmonyPostfix]
        internal static void Postfix()
        {
            if (built) return;
            built = true;
            // Init 전이면 여기서 예외(PatchAll 은 Init 뒤에만 불리므로 정상 경로에서는 일어나지 않는다).
            Action buildAll = ModContext.BuildAll;
            try
            {
                buildAll();
                ModContext.Log(ModContext.Id + " blueprints built");
            }
            catch (Exception e)
            {
                ModContext.BuildFailed = true;
                ModContext.LogError(ModContext.Id + " BuildAll failed: " + e);
                ModContext.Entry.Logger.LogException(e);
            }
            finally
            {
                ModContext.BuildAttempted = true; // 성공·실패가 정해진 뒤에만 세운다(BuildFailed 가 이때부터 의미를 갖는다)



            }
        }
    }
}
