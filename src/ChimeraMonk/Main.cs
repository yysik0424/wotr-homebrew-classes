using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Kingmaker.Blueprints;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace ChimeraMonk
{
    public static class Main
    {
        // PatchAll 을 쓰지 않는다: 공용 BlueprintsCacheInitPatch(BuildHook)는 BlueprintsCache.Init 에서 BuildAll 을 부르는데, 그때 DarkCodex FCT 가 아직 없다(계획 부록 A-0).
        // 그래서 공용 Postfix(→ BuildAll)는 DarkCodex 뒤 순서 속성을 단 DarkCodexHook 이 StartGameLoader.LoadAllJson 에서 대신 부른다(DarkCodexHook.cs).
        // 아래 형식만 하나씩 패치한다 = 이 DLL 의 [HarmonyPatch] 형식 전부에서 공용 BlueprintsCacheInitPatch 를 뺀 것(시험 `wiring: hook is LoadAllJson after DarkCodex` 가 집합을 고정).
        static readonly Type[] PatchTypes = { typeof(LocaleChangedPatch), typeof(GetTextPatch), typeof(DarkCodexHook), typeof(NaturalAttackAnimationPatch) };

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "ChimeraMonk", Blueprints.BuildAll);
            // 한국어 판별 탐침 = 기반 직업 MonkClass(본편).
            Strings.Configure(StringsTable.Table, Guids.Game.MonkClass);
            DarkCodexHook.Resolve();
            var harmony = new Harmony(ModContext.HarmonyId);
            foreach (var t in PatchTypes)
            {
                if (BundleHooks.Enabled && (t == typeof(LocaleChangedPatch) || t == typeof(GetTextPatch))) continue;
                // PatchClassProcessor.Patch 는 [HarmonyPatch] 가 없는 형식이면 null 을 돌려준다(0Harmony 2.3.6 IL_0006) → 조용히 넘어가지 않게 던진다.
                var patched = harmony.CreateClassProcessor(t).Patch();
                if (patched == null || patched.Count == 0) throw new InvalidOperationException(ModContext.Id + ": Harmony patch class " + t.FullName + " patched nothing");
            }



            return true;
        }
    }

    public static class Blueprints
    {
        // DarkCodexHook(DarkCodex 뒤)가 공용 빌드 경로로 한 번 부른다. 실행 계약 = 순수 BuildPlan.Execute(시험 `exec: build gate path` 가 같은 형식을 실행):
        // 첫 줄 = 외부 의존 관문(DependencyGate.Required). 하나라도 없음 → 없는 것마다 ERROR + BuildFailed 후 return(아키타입 미생성·미등록, 던지지 않음).
        // 그 뒤 빌더 순서 고정(시험 `wiring: BuildAll order` 가 마지막 = Archetype.Register 를 고정). 이후 Task 가 Register 앞에 넣는다.
        public static void BuildAll()
        {
            BuildPlan.Execute(Resolves, ChimeraErrors.Report, v => ModContext.MarkSkipped("DarkCodex 또는 필요한 변신 능력이 없어 키메라 몽크를 건너뜁니다.", "Chimera Monk skipped because DarkCodex or a required form is missing."), Builders());
        }

        // 실제 해석기(관문과 S11 점검이 같은 것을 쓴다 — C4). 캐시에 있는(본편 번들·다른 모드가 추가한) 블루프린트면 true.
        internal static bool Resolves(string guid) { return ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(guid)) != null; }

        static KeyValuePair<string, Action>[] Builders()
        {
            return new[]
            {
                new KeyValuePair<string, Action>("Archetype.Build", Archetype.Build),
                new KeyValuePair<string, Action>("Mutations.Build", Mutations.Build),
                new KeyValuePair<string, Action>("Mutagen.Build", Mutagen.Build),
                new KeyValuePair<string, Action>("NaturalWeapons.Build", NaturalWeapons.Build),
                new KeyValuePair<string, Action>("BodyMutations.Build", BodyMutations.Build),
                new KeyValuePair<string, Action>("Breath.Build", Breath.Build),
                new KeyValuePair<string, Action>("Venom.Build", Venom.Build),
                new KeyValuePair<string, Action>("Aspects.Build", Aspects.Build),
                // 2026-11-16: 꼬리·뿔 변이 외형(VisualPlan). 변이 껍데기(Mutations.Build) 뒤면 된다.
                new KeyValuePair<string, Action>("Visuals.Build", Visuals.Build),
                // 2026-11-15: 우리가 만든 블루프린트 전부에 IconTable 아이콘(보이는 것에 없으면 빌드 실패).
                new KeyValuePair<string, Action>("Icons.Apply", Icons.Apply),
                // 2026-11-17(툴팁 짧은 설명 `<null>` 결함): 위에서 만든 모든 블루프린트의 LocalizedString 필드 null 0 — 어긋나면 던져 아키타입을 등록하지 않는다.
                new KeyValuePair<string, Action>("Blueprints.RequireTexts", RequireTexts),
                new KeyValuePair<string, Action>("Archetype.Register", Archetype.Register),
            };
        }

        // BlueprintUtil.RequireOwnLocalizedStrings(검사 개수를 돌려준다)를 Builders 의 Action 모양으로. null 칸이 있으면 그 함수가 던진다(BuildPlan 이 보고 뒤 다시 던짐).
        // 복제 27개(자연 무기·형태 버프/토글·늑대 냄새)도 Create 를 거치므로 대상이다 — 원본 jbp 의 문구 칸이 null 아님은 시험 `tooltip: clone source` 가 대조한다.
        static void RequireTexts()
        {
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 만든·복제한 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 등록 전에 — 어긋나면 던진다.
            BlueprintUtil.RequireOwnBuffFx(true);
            int n = BlueprintUtil.RequireOwnLocalizedStrings();
            ModContext.Log("ChimeraMonk: LocalizedString fields checked on " + n + " blueprint(s), none null");
        }
    }
}
