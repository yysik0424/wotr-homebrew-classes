using System;
using System.IO;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace Spellfist
{
    public static class Main
    {
        // 모드 정체·로그·빌드 상태는 WotrClassMod.Common.ModContext.
        // PatchAll 을 쓰지 않는다: 공용 BlueprintsCacheInitPatch(BuildHook)에는 Harmony 순서 속성(HarmonyAfter/Priority)이 없어, 함께 걸리면 BrawlerClassWrath 의
        // BlueprintsCache.Init Postfix 보다 먼저 BuildAll 이 돌 수 있다 → 그때 브롤러 클래스가 캐시에 없어 BuildFailed. 그래서 공용 Postfix(→ BuildAll)는 브롤러 Postfix 뒤 순서 속성을 단 BrawlerHook 이 대신 부른다(BrawlerHook.cs).
        // 대신 아래 형식만 하나씩 패치한다 = 이 DLL 의 [HarmonyPatch] 형식 전부에서 공용 BlueprintsCacheInitPatch 를 뺀 것(시험이 집합 일치를 고정 — 공용에 패치가 늘면 시험이 빨개진다).
        // 공용 Checks\CheckPatches 는 [HarmonyPatch] 가 없고 CheckRunner.Begin 이 RUN_CHECKS.txt 가 있을 때 직접 건다(PatchAll 과 무관하게 그대로 동작).
        // SpellListPatch(FistSpellstrike.cs) = 판정 J1 의 Harmony 예외 1건(UnitPartMagus.IsSpellFromMagusSpellList Postfix, 설계 §11-12).
        static readonly Type[] PatchTypes = { typeof(LocaleChangedPatch), typeof(GetTextPatch), typeof(BrawlerHook), typeof(SpellListPatch) };

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModContext.Init(modEntry, "Spellfist", Blueprints.BuildAll);
            // 한국어 판별 탐침은 본편 파이터(공용 GameGuids.FighterClass): 브롤러는 모드 클래스라 번역 여부를 보장할 수 없다(영문이면 판별이 조용히 영어로 기운다).
            Strings.Configure(StringsTable.Table, GameGuids.FighterClass);
            BrawlerHook.Resolve();
            var harmony = new Harmony(ModContext.HarmonyId);
            foreach (var t in PatchTypes)
            {
                if (BundleHooks.Enabled && (t == typeof(LocaleChangedPatch) || t == typeof(GetTextPatch))) continue;
                // PatchClassProcessor.Patch 는 [HarmonyPatch] 가 없는 형식이면 null 을 돌려준다(0Harmony 2.3.6 IL_0006) → 조용히 넘어가지 않게 던진다.
                var patched = harmony.CreateClassProcessor(t).Patch();
                if (patched == null || patched.Count == 0) throw new InvalidOperationException(ModContext.Id + ": Harmony patch class " + t.FullName + " patched nothing");
            }
            // 2026-11-17(소유자 결정 「게임 시작 뒤 다시 맞춤」): 모든 모드의 블루프린트 패치가 끝난 첫 프레임에 학파 목록을 위저드 목록에서 다시 가른다(SchoolLists.FirstFrameTick — 한 번만).
            // 점검 모드와 무관하게 건다. 점검 러너 OnUpdate 는 아래에서 += 로 덧붙는다(= 로 덮으면 재동기화가 빠진다).
            modEntry.OnUpdate += SchoolLists.FirstFrameTick;



            return true;
        }
    }

    public static class Blueprints
    {
        // BrawlerHook(브롤러 모드 뒤)가 공용 빌드 경로로 한 번 부른다.
        // 첫 줄: 브롤러 클래스가 캐시에 없으면 로그 오류 + BuildFailed 후 return — 던지지 않는다(설계 §2: Init 을 깨뜨리면 게임 부팅이 깨짐). 아키타입은 등록되지 않는다.
        // 순서 고정 — Archetype.Build(); SchoolLists.Build(); Spellbook.Build(); Reservoir.Build(); Exploits.Build(); Quicken.Build(); FistSpellstrike.Build(); Icons.Apply(); BlueprintUtil.RequireOwnLocalizedStrings(); Archetype.Register();
        // (Register 는 맨 끝: 위 빌드가 모두 성공했을 때만 브롤러 아키타입 목록에 올린다. 시험이 첫 호출·분기·순서를 고정한다.)
        public static void BuildAll()
        {
            if (ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Guids.Game.BrawlerClass) == null)
            {
                ModContext.LogError("Spellfist: BrawlerClassWrath class " + Guids.Game.BrawlerClass + " not in cache — archetype not registered");
                ModContext.MarkSkipped("BrawlerClassWrath 직업이 없어 주문 격투가를 건너뜁니다.", "Spellfist skipped because the BrawlerClassWrath class is missing.");
                return;
            }
            Archetype.Build();
            SchoolLists.Build();
            Spellbook.Build();
            Reservoir.Build();
            Exploits.Build();
            Quicken.Build();
            FistSpellstrike.Build();
            // 2026-11-15: 우리가 만든 블루프린트 전부에 IconTable 아이콘(보이는 것에 없으면 빌드 실패).
            Icons.Apply();
            // 2026-11-17(툴팁 `<null>` 결함 전수 — 다른 직업에서 발견, 이 모드는 null 0 확인): 우리가 Create 로 만든 블루프린트 전부(익스플로잇 복제 37 포함 — Exploits.CloneAs 도 Create)의
            // LocalizedString 필드가 null 이 아닌지. 등록 전에 — 어긋나면 아키타입을 올리지 않는다(앞으로 생기는 null 을 빌드에서 막는다).
            // 2026-11-17(Buff.OnRemove @00B0 NRE): 만든·복제한 버프 전부의 FxOnRemove null 금지(BlueprintUtil.EmptyFxOnRemove 머리말). 등록 전에 — 어긋나면 던진다.
            BlueprintUtil.RequireOwnBuffFx(true);
            BlueprintUtil.RequireOwnLocalizedStrings();
            Archetype.Register();
        }
    }
}
