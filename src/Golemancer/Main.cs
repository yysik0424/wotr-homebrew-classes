using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace Golemancer
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태는 WotrClassMod.Common.ModContext. 블루프린트 생성 후크·로컬라이즈 패치(공용 3개)는 src\Common 이
        // 같은 어셈블리에 컴파일되어 아래 PatchAll 이 함께 집는다. 이 모드 고유 [HarmonyPatch] = CognatogenGatePatch(T1) · FusionPreSavePatch(T5) — 계획 Global Constraints.
        // Load 순서: ModContext.Init → Strings.Configure → PatchAll → RUN_CHECKS 면 CheckRunner.Begin (시험 wiring: 이 순서를 고정).
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "Golemancer", Blueprints.BuildAll);
            // 한국어 판별 클래스 = 기반 직업 AlchemistClass(본편, 공용 GameGuids 밖 — Guids.Game).
            Strings.Configure(StringsTable.Table, Guids.Game.AlchemistClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());



            // 골렘 생명주기 폴링(계획 T3 「GolemMaster.Tick(ModEntry, float)(OnUpdate)」) — 점검 모드와 무관하게 항상.
            modEntry.OnUpdate += GolemMaster.Tick;
             
            modEntry.OnUpdate += Fusion.Tick;
            return true;
        }
    }

    public static class Blueprints
    {
        // 순서 고정(계획 T1·T3·T4·T5): Archetype.Build(); OppositionSchools.Build(); BombHalf.Build(); PetClass.Build(); Taunt.Build(); GolemUnit.Build(); GolemMaster.Build(); Materials.Build(); Capstone.Build(); Fusion.Build(); Repair.Build(); SlamLimbs.Build(); Overdrive.Build(); Icons.Apply(); BlueprintUtil.RequireOwnLocalizedStrings(); Archetype.Register();
        // 이후 Task 의 빌더는 Register 앞에 온다(AddAt 은 Register 전에만). PetClass 가 격리 스냅샷을 먼저 찍고 GolemUnit 이 끝에서 격리를 단언한다(도발은 그 사이에 만든다).
        // Register 는 맨 끝: 위 빌드가 모두 성공했을 때만 AlchemistClass 아키타입 목록에 올린다. 시험 `wiring: BuildAll order` 가 호출 목록을 고정한다.
        static bool HasMaterials()
        {
            var missing = MaterialCatalog.MissingUnits(id =>
                Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.BlueprintUnit>(id) != null);
            if (missing.Count == 0) return true;
            foreach (string id in missing)
                if (id != Guids.Game.GolemBrass) throw new System.InvalidOperationException("Golemancer: required golem blueprint missing " + id);
            ModContext.MarkSkipped("DLC 3 황동 골렘이 없어 골렘술사를 건너뜁니다.", "Golemancer skipped because the DLC 3 brass golem is missing.");
            return false;
        }

        public static void BuildAll()
        {
            if (!HasMaterials()) return;
            Archetype.Build();
            OppositionSchools.Build();
            BombHalf.Build();
            PetClass.Build();
            Taunt.Build();
            GolemUnit.Build();
            GolemMaster.Build();
            Materials.Build();
            Capstone.Build();
            Fusion.Build();
            Repair.Build();
            // 소유자 결정 2026-11-16: 추가 슬램 사지 버프(평소 2회·폭주 공용) → 골렘 폭주(5레벨).
            SlamLimbs.Build();
            Overdrive.Build();
            // 2026-11-15: 우리가 만든 블루프린트 전부에 IconTable 아이콘(보이는 것에 없으면 빌드 실패).
            Icons.Apply();
            // 2026-11-17(Buff.OnRemove @00B0 NRE: 도발·합체 형태 버프): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 등록 전에 — 어긋나면 던진다.
            BlueprintUtil.RequireOwnBuffFx(true);
            // 2026-11-16(능력 툴팁 `<null>` 결함): 위에서 만든 모든 블루프린트(BlueprintUtil.Create)의 LocalizedString 필드가 null 이 아닌지. 등록 전에 — 어긋나면 아키타입을 올리지 않는다.
            // AbilityClone 복제본(Create 밖)은 본편 능력의 얕은 복제라 문자열 칸이 본편 인스턴스 그대로다 — 원본 jbp 의 칸이 null 이 아님은 시험 tooltip: 이 대조한다.
            BlueprintUtil.RequireOwnLocalizedStrings();
            Archetype.Register();
        }
    }
}
