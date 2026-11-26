using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace WeaponSpiritShaman
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태는 WotrClassMod.Common.ModContext. 블루프린트 생성 후크·로컬라이즈 패치(공용 3개)는 src\Common 이
        // 같은 어셈블리에 컴파일되어 아래 PatchAll 이 함께 집는다. 동료 수동 레벨업 목록과 직업 요건은 PetLevelUp.cs에서 보완한다.
        // 계획 파일 구조의 Load 순서: ModContext.Init → PatchAll → OnUpdate += ProgressionSync.Tick → RUN_CHECKS 면 CheckRunner.Begin.
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "WeaponSpiritShaman", Blueprints.BuildAll);
            // 한국어 판별 클래스 = 기반 직업 ShamanClass(본편, 공용 GameGuids 밖 — Guids.Game).
            Strings.Configure(StringsTable.Table, Guids.Game.ShamanClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
             
            modEntry.OnUpdate += ProgressionSync.Tick;



            return true;
        }
    }

    public static class Blueprints
    {
         
         
        // Register 는 맨 끝: 위 빌드가 모두 성공했을 때만 ShamanClass 아키타입 목록에 올린다(블라스트 메이거스와 같은 규칙). 시험이 호출 목록을 고정한다.
        public static void BuildAll()
        {
            Archetype.Build();
            PetClass.Build();
            SpiritMirror.Build();
            SpiritBlueprints.Build();
            Possession.Build();
            Fusion.Build();
            SpiritBarrage.Build(); // 2026-11-16 스피릿 배러지(특성 AddAt 18 — Register 전, 아이콘 원본 = 강신 능력이라 Possession 뒤)
            // 2026-11-15: 우리가 만든 블루프린트 전부에 IconTable 아이콘(보이는 것에 없으면 빌드 실패).
            Icons.Apply();
            // 2026-11-16(능력 툴팁 `<null>` 결함): 위에서 만든 모든 블루프린트(복사본 포함)의 LocalizedString 필드가 null 이 아닌지. 등록 전에 — 어긋나면 아키타입을 올리지 않는다.
            // 이 뒤에 만들어지는 블루프린트는 없다(ProgressionSync 는 진행표 항목만 바꾼다 — 시험 `tooltip: every BlueprintUtil.Create site` 가 만드는 곳 전부를 고정).
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 등록 전에 — 어긋나면 던진다.
            BlueprintUtil.RequireOwnBuffFx(true);
            BlueprintUtil.RequireOwnLocalizedStrings();
            Archetype.Register();
        }
    }
}
