using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace ChaosWarrior
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태 = WotrClassMod.Common.ModContext. 공용 패치 3개(블루프린트 생성 후크·로컬라이즈)가 같은 어셈블리에 컴파일돼 PatchAll 이 함께 집는다.
         
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "ChaosWarrior", Blueprints.BuildAll);
            // 한국어 판별 클래스 = 기반 직업 WarpriestClass(본편).
            Strings.Configure(StringsTable.Table, Guids.Game.WarpriestClass);
            new Harmony(ModContext.HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
             
            modEntry.OnUpdate += RageOffWatcher.Tick;
            modEntry.OnUpdate += OrphanMutationSweep.Tick; // 2026-11-16: FxOnRemove 결함으로 분노 없이 남은 변이·껍질 버프 자동 정리



            return true;
        }
    }

    public static class Blueprints
    {
         
        // 이후 Task 가 Register 앞에 빌더를 넣는다. 시험 `wiring: BuildAll order` 가 이 목록을 고정.
        public static readonly IReadOnlyList<KeyValuePair<string, Action>> Builders = new[]
        {
            new KeyValuePair<string, Action>("Archetype.Build", Archetype.Build),
            new KeyValuePair<string, Action>("Archetype.ApplyLevelTable", Archetype.ApplyLevelTable),
            new KeyValuePair<string, Action>("Rage.Build", Rage.Build),
            // 개편 O4: 자연 무기 복제 12개 — 변이 표식의 AddAdditionalLimb 가 참조하므로 Mutations.Build 앞.
            new KeyValuePair<string, Action>("NaturalWeapons.Build", NaturalWeapons.Build),
            new KeyValuePair<string, Action>("Mutations.Build", Mutations.Build),
            // 개편 O8: 신속 시전 권리 버프 6개 — 분노 버프(Rage.Build) 뒤 · 아이콘(Icons.Apply)·등록 앞. 부여는 변이 추첨 행동이 한다(빌더는 블루프린트만).
            new KeyValuePair<string, Action>("Quicken.Build", Quicken.Build),
            new KeyValuePair<string, Action>("Pain.Build", Pain.Build),
            new KeyValuePair<string, Action>("RageAlias.Build", RageAlias.Build),
            // 2026-11-15: 우리가 만든 블루프린트 전부에 IconTable 아이콘(보이는 것에 없으면 빌드 실패).
            new KeyValuePair<string, Action>("Icons.Apply", Icons.Apply),
            // 2026-11-17(툴팁 짧은 설명 `<null>` 결함): 위에서 만든 모든 블루프린트의 LocalizedString 필드 null 0 — 어긋나면 던져 아키타입을 등록하지 않는다.
            new KeyValuePair<string, Action>("Blueprints.RequireTexts", RequireTexts),
            new KeyValuePair<string, Action>("Archetype.Register", Archetype.Register),
        };

        public static void BuildAll()
        {
            BuildPlan.Execute(ChaosErrors.Report, Builders);
            // 점검 모드(2026-11-17 수리): 점검 전용 블루프린트는 모든 빌더·검증(Icons.Apply·RequireTexts)과 등록이 성공한 뒤에만 만든다.
            // Execute 가 던지면 여기 오지 않는다. 일반 실행(CheckRunner.Active 거짓)에서는 부르지 않는다 — Build 자신도 거짓이면 던진다.



        }

        // BlueprintUtil.RequireOwnLocalizedStrings(검사 개수를 돌려준다)를 Builders 의 Action 모양으로. null 칸이 있으면 그 함수가 던진다(BuildPlan 이 보고 뒤 다시 던짐).
        // 복제(자연 무기 12)도 Create 를 거치므로 대상이다 — 원본 jbp 의 문구 칸이 null 아님은 시험 `jbp: natural weapon sources` 가 대조한다.
        static void RequireTexts()
        {
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 버프 자리는 BuildKit.Buff 하나(2026-11-16 빈 링크 선례) — 이 검사는 전수 되읽기.
            BlueprintUtil.RequireOwnBuffFx(true);
            int n = BlueprintUtil.RequireOwnLocalizedStrings();
            ModContext.Log("ChaosWarrior: LocalizedString fields checked on " + n + " blueprint(s), none null");
        }

        // 점검 러너 BlueprintsBuilt: 핵심 산출물이 다 있는지.
        public static bool Built()
        {
            return Archetype.Bp != null && Archetype.Registered && Rage.Built && NaturalWeapons.Built && Mutations.Built && Quicken.Built && Pain.Built && RageAlias.Complete;
        }
    }
}
