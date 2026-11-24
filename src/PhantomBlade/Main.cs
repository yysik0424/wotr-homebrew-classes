using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace PhantomBlade
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태는 WotrClassMod.Common.ModContext. 블루프린트 생성 후크·로컬라이즈 패치는 src\Common 이
        // 같은 어셈블리에 컴파일되어 아래 PatchAll 이 함께 집는다.
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "PhantomBlade", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, GameGuids.FighterClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
             
            GhostSlashVeilGlobalGuard.Register();



            return true;
        }
    }

    public static class Blueprints
    {
        public static void BuildAll()
        {
            // 순서 고정: Archetype.Build() 가 첫째(StandBlueprints 의 ClassLevelGetter·AddAt 이 Archetype.Bp 를 읽는다).
            Archetype.Build();
            StandBlueprints.Build();
             
            SpiritBlueprints.Build();
            // 2026-11-17(주먹 무기·령 껍데기 툴팁 `<null>`/예외 결함): 위에서 만든 모든 블루프린트(BlueprintUtil.Create)의 LocalizedString 필드가 null 이 아닌지. 등록 전에 — 어긋나면 아키타입을 올리지 않는다.
            // 령 유닛 사본(CloneUnit — Create 밖)은 SpiritBlueprints.BuildClone 의 되읽기가 같은 검사를 한다(본편 모델의 칸 그대로).
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 등록 전에 — 어긋나면 던진다.
            BlueprintUtil.RequireOwnBuffFx(true);
            BlueprintUtil.RequireOwnLocalizedStrings();
            // 맨 끝: 위 빌드가 모두 성공했을 때만 FighterClass 목록에 올린다(PackWarden 최종 리뷰 I2 와 같은 규칙).
            Archetype.Register();
        }
    }
}
