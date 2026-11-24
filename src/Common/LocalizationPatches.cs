using System;
using HarmonyLib;
using Kingmaker.Localization;

namespace WotrClassMod.Common
{
    // OnLocaleChanged 는 private static void — Harmony 는 이름 문자열로 접근. verify-patch-targets.ps1 이 존재를 검사한다(tools\patch-targets\common.txt).
    [HarmonyPatch(typeof(LocalizationManager), "OnLocaleChanged")]
    static class LocaleChangedPatch
    {
        [HarmonyPrepare]
        static bool Prepare() { return !BundleHooks.Enabled; }

        [HarmonyPostfix]
        static void Postfix()
        {
            Strings.InvalidateLanguage();
        }
    }

    // LocalizedString.LoadString 이 부르는 실제 조회 경로: LocalizationPack.GetText(string, bool).
    // 우리 접두(ModContext.StringPrefix) 키만 가로채 ko/en 을 조회 시점에 돌려준다. 그 밖의 키는 원래 경로 그대로.
    [HarmonyPatch(typeof(LocalizationPack), nameof(LocalizationPack.GetText), new Type[] { typeof(string), typeof(bool) })]
    static class GetTextPatch
    {
        [HarmonyPrepare]
        static bool Prepare() { return !BundleHooks.Enabled; }

        [HarmonyPrefix]
        static bool Prefix(string key, ref string __result)
        {
            if (!Strings.IsOurKey(key)) return true;
            __result = Strings.Resolve(key);
            return false;
        }
    }
}
