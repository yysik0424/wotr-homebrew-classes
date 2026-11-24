using System;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using WotrClassMod.Common;

namespace Spellfist
{
    // 이 모드의 블루프린트 생성 후크. 브롤러 직업은 BrawlerClassWrath 가 BlueprintsCache.Init Postfix 에서 만든다(정찰 §8-1: 그 모드의 Init Postfix 3개,
    // owner = "BrawlerClassWrath") → 우리 빌드는 그 뒤여야 한다. 공용 BlueprintsCacheInitPatch(src\Common\BuildHook.cs)에는 순서 속성이 없고 공용은 고치지 않는다
    // → 이 모드는 PatchAll 을 쓰지 않고(Main.Load 가 형식별로 CreateClassProcessor) 공용 훅 형식은 패치하지 않으며, 이 훅이 순서 속성을 달고
    // 공용 훅의 Postfix(빌드 1회 보장·try/catch → BuildFailed·BuildAttempted)를 그대로 부른다(같은 어셈블리에 컴파일된 private static 메서드 → 리플렉션).
    // 순서: Harmony 2.3.6 은 before/after 를 우선순위보다 먼저 지킨다(정찰 §8-1; 0Harmony 정렬 IL 은 미독 — 브롤러 클래스가 없으면 BuildFailed 로 드러난다).
    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    static class BrawlerHook
    {
        // 브롤러 모드의 Harmony id = 그 모드 Info.json 의 Id(BrawlerClassWrath.Main.Load IL @0x0013 ldfld ModInfo::Id → @0x001D new Harmony). 시험이 둘 다 대조한다.
        internal const string BrawlerHarmonyId = "BrawlerClassWrath";

        static MethodInfo commonPostfix;

        // Main.Load 가 패치 전에 부른다: 공용 Postfix 를 못 찾으면 모드 로드에서 던진다(게임 부팅 전, UMM 이 모드 오류로 표시).
        internal static void Resolve()
        {
            var m = AccessTools.Method(typeof(BlueprintsCacheInitPatch), "Postfix");
            if (m == null || !m.IsStatic || m.ReturnType != typeof(void) || m.GetParameters().Length != 0)
                throw new MissingMethodException(ModContext.Id + ": common build hook BlueprintsCacheInitPatch.Postfix (static void, no parameters) not found");
            commonPostfix = m;
        }

        [HarmonyPostfix]
        [HarmonyAfter(BrawlerHarmonyId)]
        [HarmonyPriority(Priority.Low)]
        static void Postfix()
        {
            // Resolve 없이 패치될 수 없다(Main.Load 순서, 시험이 고정). 그래도 여기서 던지면 BlueprintsCache.Init 이 깨지므로(설계 §2) 실패로 기록만 한다.
            if (commonPostfix == null)
            {
                ModContext.LogError(ModContext.Id + ": BrawlerHook.Postfix ran before BrawlerHook.Resolve — blueprints not built");
                ModContext.BuildFailed = true;
                ModContext.BuildAttempted = true;
                return;
            }
            commonPostfix.Invoke(null, null);
        }
    }
}
