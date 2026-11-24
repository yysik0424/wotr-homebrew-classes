using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace BlastMagus
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태는 WotrClassMod.Common.ModContext. 블루프린트 생성 후크·로컬라이즈 패치는 src\Common 이
         
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "BlastMagus", Blueprints.BuildAll);
            Strings.Configure(StringsTable.Table, GameGuids.MagusClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
             
            KeeStage.RecordSnapshot();



            return true;
        }
    }

    public static class Blueprints
    {
        // 순서 고정 — Archetype.Build(); PoolArcana.Build(); BlastBlueprints.Build(); Energize.Build(); BlastStrike.Build(); BlueprintUtil.RequireOwnLocalizedStrings(); Archetype.Register();
         
         
        // MagusClass 아키타입 목록에 올린다. PhantomBlade 와 같은 규칙. 시험이 첫 호출·마지막 호출을 고정한다.)
        public static void BuildAll()
        {
            Archetype.Build();
            PoolArcana.Build();
            BlastBlueprints.Build();
            Energize.Build();
            BlastStrike.Build();
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 등록 전에 — 어긋나면 아키타입을 올리지 않는다.
            BlueprintUtil.RequireOwnBuffFx(true);
            // 2026-11-16(「폭발」 툴팁 `<null>` 결함): 1단계에서 만든 모든 블루프린트의 LocalizedString 필드가 null 이 아닌지. 등록 전에 — 어긋나면 아키타입을 올리지 않는다.
            BlueprintUtil.RequireOwnLocalizedStrings();
            Archetype.Register();
        }
    }
}
