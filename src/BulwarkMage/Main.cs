using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace BulwarkMage
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태는 WotrClassMod.Common.ModContext. 블루프린트 생성 후크·로컬라이즈 패치(공용 3개)는 src\Common 이
        // 같은 어셈블리에 컴파일되어 아래 PatchAll 이 함께 집는다. 이 모드 고유 [HarmonyPatch] = 정확히 H1 OneHandStaffPatch(OneHandStaff.cs, T3) — 계획 Global Constraints.
        // Load 순서: ModContext.Init → Strings.Configure → PatchAll → 런타임 되읽기 OnUpdate → RUN_CHECKS 면 CheckRunner.Begin + 러너 OnUpdate.
        // 되읽기 Tick 을 러너보다 먼저 붙인다(같은 프레임이면 되읽기가 먼저 돈다 — S1 foreign-readback-complete-keys 가 주 실행 결과를 본다).
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "BulwarkMage", Blueprints.BuildAll);
            // 한국어 판별 클래스 = 기반 직업 WizardClass(본편, 공용 GameGuids 밖 — Guids.Game).
            Strings.Configure(StringsTable.Table, Guids.Game.WizardClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
            modEntry.OnUpdate += RuntimeReadback.Tick;
            // A3(2026-11-14): 반격 결과 관측기 전역 구독 — 첫 프레임에 한 번(RebukeOutcomeTracker.Tick).
            modEntry.OnUpdate += RebukeOutcomeTracker.Tick;



            return true;
        }
    }

    public static class Blueprints
    {
        // 순서 고정(계획 T1·T3): Archetype.Build(); Proficiency.Build(); TowerShield.Build(); ArmorTrainingGrant.Build(); OneHandStaff.Build(); Archetype.Register();
        // 이후 Task 의 빌더는 Register 앞에 온다(AddAt 은 Register 전에만). Register 는 맨 끝: 위 빌드가 모두 성공했을 때만 WizardClass 아키타입 목록에 올린다.
        // 시험 `wiring: BuildAll order` 가 호출 목록을 고정한다.
        public static void BuildAll()
        {
            Archetype.Build();
            Proficiency.Build();
            TowerShield.Build();
            ArmorTrainingGrant.Build();
            OneHandStaff.Build();
            Rebuke.Build();
            BondedShield.Build();
            IronConcentration.Build();
            // 2026-11-17(Buff.OnRemove @00B0 NRE 약 100건: 강화·훈련 버프): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 등록 전에 — 어긋나면 던진다.
            BlueprintUtil.RequireOwnBuffFx(true);
            // 2026-11-15: 우리가 만든 블루프린트 전부에 IconTable 아이콘(보이는 것에 없으면 빌드 실패).
            Icons.Apply();
            // 2026-11-17(툴팁 짧은 설명 `<null>` 결함): 위에서 만든 모든 블루프린트(BlueprintUtil.Create)의 LocalizedString 필드가 null 이 아닌지. 등록 전에 — 어긋나면 던져 아키타입을 올리지 않는다.
            BlueprintUtil.RequireOwnLocalizedStrings();
            Archetype.Register();
        }
    }
}
