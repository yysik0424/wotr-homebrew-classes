using System;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using WotrClassMod.Common;

namespace ChimeraMonk
{
    // 이 모드의 블루프린트 생성 후크(자체 부품 ①, 판정 H1 승인). DarkCodex 는 FCT 를 BlueprintsCache.Init 뒤의 StartGameLoader.LoadAllJson Postfix(Shared.Main/Patches::Postfix2,
    // HarmonyPriority 400)에서 만든다(계획 부록 A-0: GameStarter <StartGameCoroutine>d__28 @037C LoadPackTOC → @03B1 LoadAllJson). 공용 BlueprintsCacheInitPatch 시점에는 FCT 가 캐시에 없다.
    // → 이 모드는 PatchAll 을 쓰지 않고(Main.Load 가 형식별로 패치) 공용 훅 형식은 패치하지 않으며, 이 훅이 DarkCodex 뒤 순서 속성을 달고
    // 공용 훅의 Postfix(빌드 1회 보장·try/catch → BuildFailed·BuildAttempted)를 리플렉션으로 부른다(Spellfist src\Spellfist\BrawlerHook.cs 복제).
    // 순서가 틀리면 FCT 결측 → 의존 관문이 ERROR + BuildFailed(시끄러움, S11 deps-resolved 가 FAIL).
    [HarmonyPatch(typeof(StartGameLoader), nameof(StartGameLoader.LoadAllJson))]
    static class DarkCodexHook
    {
        // DarkCodex 의 Harmony id = 그 모드 Info.json 의 Id(Shared.Main::Load @0043~004D ModInfo::Id → new Harmony). 시험이 둘 다 대조한다.
        internal const string DarkCodexHarmonyId = "DarkCodex";

        static MethodInfo commonPostfix;

        // Main.Load 가 패치 전에 부른다: 공용 Postfix 를 못 찾으면 모드 로드에서 던진다.
        internal static void Resolve()
        {
            var m = AccessTools.Method(typeof(BlueprintsCacheInitPatch), "Postfix");
            if (m == null || !m.IsStatic || m.ReturnType != typeof(void) || m.GetParameters().Length != 0)
                throw new MissingMethodException(ModContext.Id + ": common build hook BlueprintsCacheInitPatch.Postfix (static void, no parameters) not found");
            commonPostfix = m;
        }

        [HarmonyPostfix]
        [HarmonyAfter(DarkCodexHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        static void Postfix()
        {
            // Resolve 없이 패치될 수 없다(Main.Load 순서). 그래도 여기서 던지면 LoadAllJson 이 깨지므로 실패로 기록만 한다.
            if (commonPostfix == null)
            {
                ModContext.LogError(ModContext.Id + ": DarkCodexHook.Postfix ran before DarkCodexHook.Resolve — blueprints not built");
                ModContext.BuildFailed = true;
                ModContext.BuildAttempted = true;
                return;
            }
            commonPostfix.Invoke(null, null);
        }
    }
}
