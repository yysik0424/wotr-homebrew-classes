using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace OathboundRevenant
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태 = WotrClassMod.Common.ModContext. Harmony 는 공용 패치(블루프린트 빌드 후크·로컬라이즈)만 — 모드 고유 [HarmonyPatch] 0개
        // (계획 Global Constraints, tools\patch-targets\OathboundRevenant.txt = '# no mod targets').
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "OathboundRevenant", Blueprints.BuildAll);
            // 한국어 판별 클래스 = 기반 직업 PaladinClass(본편).
            Strings.Configure(StringsTable.Table, Guids.Game.PaladinClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            return true;
        }
    }

    public static class Blueprints
    {
         
        // 빌더마다 RevenantErrors.Builder = try { … } catch { Report("builder", 이름) ; throw; }(계획 「BuildAll 실패 계약」). Register 는 맨 끝 — 모든 빌더 성공 시에만 등록.
        // 시험 `wiring: BuildAll order` 가 이 목록을 고정한다.
        public static void BuildAll()
        {
            RevenantErrors.Builder("UndeadBody", UndeadBody.Build);
            RevenantErrors.Builder("Archetype", Archetype.Build);
            RevenantErrors.Builder("Hands", Hands.Build);
            RevenantErrors.Builder("Talents", Talents.Build);
            RevenantErrors.Builder("Strike", Strike.Build);
            RevenantErrors.Builder("Channel", Channel.Build);
            RevenantErrors.Builder("Auras", Auras.Build);
            RevenantErrors.Builder("Fear", Fear.Build);
            RevenantErrors.Builder("Survival", Survival.Build);
            RevenantErrors.Builder("DivineBond", DivineBond.Build);
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 우리가 만든 버프 전부의 FxOnRemove null 금지(Parts.NewBuff 주석). 마지막 버프 빌더(DivineBond) 뒤.
            RevenantErrors.Builder("BuffFx", BuffFx.Require);
            // 2026-11-15: 우리가 만든 블루프린트 전부에 IconTable 아이콘(보이는 것에 없으면 빌드 실패).
            RevenantErrors.Builder("Icons", Icons.Apply);
            // 2026-11-16(툴팁 `<null>` 결함): 우리가 만든 블루프린트 전부의 LocalizedString 필드 null 금지 + 능력의 지속·내성 칸 == TooltipPlan. 등록 직전 — 어긋나면 아키타입을 올리지 않는다.
            RevenantErrors.Builder("Tooltips", Tooltips.Require);
            RevenantErrors.Builder("Register", Archetype.Register);
        }




    }
}
