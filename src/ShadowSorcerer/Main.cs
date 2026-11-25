using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace ShadowSorcerer
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태는 WotrClassMod.Common.ModContext. 공용 Harmony 3(src\Common)은 같은 어셈블리로 컴파일되어 아래 PatchAll 이 함께 집는다.
         
        // Load 순서: ModContext.Init → Strings.Configure → PatchAll → 런타임 되읽기 OnUpdate → RUN_CHECKS 면 CheckRunner.Begin + 러너 OnUpdate.
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "ShadowSorcerer", Blueprints.BuildAll);
            // 한국어 판별 클래스 = 기반 직업 SorcererClass(본편).
            Strings.Configure(StringsTable.Table, Guids.Game.SorcererClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
            modEntry.OnUpdate += RuntimeReadback.Tick;
            // 메아리 대기열(비저장): 원래 주문이 끝난 뒤 간격을 두고 발사. 점검 러너보다 먼저 붙인다.
            // 그림자 유닛 묶음(비저장, 평소 숨김)을 대기열보다 먼저 맞춘다.
            modEntry.OnUpdate += ShadowDoubles.Tick;
            modEntry.OnUpdate += EchoQueue.Tick;



            return true;
        }
    }

    public static class Blueprints
    {
        // 순서 고정(계획 T1): ShadowRules.Validate() 첫 줄 → Archetype.Build() → (이후 Task 빌더) → Archetype.Register() 맨 끝.
        // 시험 `wiring: BuildAll order` 가 호출 목록을 고정한다.
        public static void BuildAll()
        {
            ShadowRules.Validate();
            ShadowVisualDials.Validate();
            Archetype.Build();
            SlotsFeature.Build();
            BonusSpells.Build();
            ShadowEchoComponent.Attach();
            // 2026-11-17 메아리 연출: 시각 전용 그림자 유닛. 껍데기 특성은 우리 블루프린트(Icons.Apply 가 분류를 확인), 유닛 사본은 본편 모양이 다르면 만들지 않고 오류 줄만(메아리는 그대로).
            ShadowUnitBlueprint.BuildShell();
            ShadowUnitBlueprint.TryBuildUnit();
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 이 모드는 빌드에서 버프를 만들지 않는다(버프 자리 = 점검 전용 2, 시험 buff fx:) → false.
            BlueprintUtil.RequireOwnBuffFx(true);
            // 2026-11-15: 우리가 만든 블루프린트 전부에 IconTable 아이콘(보이는 것에 없으면 빌드 실패).
            Icons.Apply();
            // 2026-11-17(특성 툴팁 짧은 설명 `<null>` 결함): 위에서 만든 모든 블루프린트(BlueprintUtil.Create)의 LocalizedString 필드가 null 이 아닌지. 등록 전에 — 어긋나면 아키타입을 올리지 않는다.
            // 그림자 유닛 사본(CloneUnit — Create 밖)은 ShadowUnitBlueprint.BuildClone 의 되읽기가 같은 검사를 한다(본편 Shadow 의 칸 그대로).
            BlueprintUtil.RequireOwnLocalizedStrings();
            Archetype.Register();
        }
    }
}
