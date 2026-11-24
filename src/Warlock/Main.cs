using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace Warlock
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태는 WotrClassMod.Common.ModContext. 블루프린트 생성 후크·로컬라이즈 패치(공용 3개)는 src\Common 이
         
        // 계획 파일 구조의 Load 순서: ModContext.Init → Strings.Configure → PatchAll → OnUpdate += L20Sync.Tick → RUN_CHECKS 면 CheckRunner.Begin.
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "Warlock", Blueprints.BuildAll);
            // 한국어 판별 클래스 = 기반 직업 WitchClass(본편, 공용 GameGuids 밖 — Guids.Game).
            Strings.Configure(StringsTable.Table, Guids.Game.WitchClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
             
            modEntry.OnUpdate += L20Sync.Tick;
            modEntry.OnUpdate += AutoSummon.Tick;



            return true;
        }
    }

    public static class Blueprints
    {
        // 최종 순서(계획 「Blueprints.BuildAll 최종 순서」, T3~T6 이 가운데를 채운다):
        //   Archetype.Build(); Groups.Build(); Blasts.Build(); Summons.Build(); Heat.Build(); LevelFeatures.Build(); Pact.Build(); PrestigeBlock.Apply(); BlueprintUtil.RequireOwnLocalizedStrings(); Archetype.Register();
        // Register 는 맨 끝: 위 빌드가 모두 성공했을 때만 WitchClass 아키타입 목록에 올린다(AddAt 은 Register 전에만 — Archetype.AddAt). 시험이 호출 목록을 고정한다.
        public static void BuildAll()
        {
            Archetype.Build();
            Groups.Build();
            Blasts.Build();
            Summons.Build();
            Heat.Build();
            LevelFeatures.Build();
            Pact.Build();
            PrestigeBlock.Apply();
            // 2026-11-16(능력 툴팁 `<null>` 결함): 위에서 만든 모든 블루프린트의 LocalizedString 필드가 null 이 아닌지. 등록 전에 — 어긋나면 아키타입을 올리지 않는다.
            // (이 모드는 BuildAll 뒤에 블루프린트를 만들지 않는다 — Create 호출처 전수 = 시험 `tooltip: Create call sites`.)
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 등록 전에 — 어긋나면 던진다.
            BlueprintUtil.RequireOwnBuffFx(true);
            BlueprintUtil.RequireOwnLocalizedStrings();
            Archetype.Register();
        }
    }
}
