using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using UnityModManager = UnityModManagerNet.UnityModManager;




namespace PackWarden
{
    public static class Main
    {
        public static UnityModManager.ModEntry Entry;

        // 블루프린트 생성이 한 번이라도 실패하면 true. 게임 부팅은 막지 않고, 게임 안 점검 러너가 이 값을 읽는다.
        public static bool BuildFailed;
        // BlueprintsCache.Init 후크가 BuildAll 을 시도했으면 true. false 인 동안 BuildFailed=false 는 "성공"이 아니라 "아직 모름"이다(점검 결과 첫 줄 Unknown).
        public static bool BuildAttempted;

        public static string BuildSkipReasonKo { get; private set; }
        public static string BuildSkipReasonEn { get; private set; }

        public static void MarkSkipped(string ko, string en)
        {
            if (string.IsNullOrWhiteSpace(ko) || string.IsNullOrWhiteSpace(en))
                throw new ArgumentException("Both descriptions of the missing content are required.");
            BuildSkipReasonKo = ko;
            BuildSkipReasonEn = en;
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Entry = modEntry;
            new Harmony("PackWarden").PatchAll(Assembly.GetExecutingAssembly());



             
            string checkLine = "";

            Log("PackWarden loaded" + checkLine);
            return true;
        }

        public static void Log(string msg)
        {
            Entry?.Logger.Log(msg);
        }
    }

    public static class Blueprints
    {
        public static void BuildAll()
        {
            var missing = Guids.Game.MissingTandemSource(guid => ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(guid)) != null);
            if (missing != null)
            {
                Main.MarkSkipped("탠덤 집행자 원본이 없어 무리 조련사를 건너뜁니다.", "Pack Warden skipped because a Tandem Executioner blueprint is missing.");
                Main.Log(Main.BuildSkipReasonEn + " " + missing);
                return;
            }
            // 순서 고정: Archetype.Build() 가 첫째(AuxUnits.Build 가 Archetype.CompanionSelection 을 읽는다).
            Archetype.Build();
            Aspects.Build();
            // 그림자 무리 T2: 보조 복제본(AuxUnits.BuildClone)이 m_Brain 으로 그림자 두뇌를 가리키므로 그 앞.
            ShadowBrains.Build();
            AuxUnits.Build();
            PackCall.Build();
            // 무리 강화(소유자 결정 2026-11-16): 아키타입·보조 유닛이 다 만들어진 뒤. 하나라도 던지면 아래 Register 에 닿지 않는다.
            PackEmpower.Build();
            // 2026-11-17(툴팁 `<null>` 결함 전수): Create 로 만든 블루프린트 전부의 LocalizedString 필드가 null 이 아닌지. 등록 직전 — 어긋나면 아키타입을 올리지 않는다.
            // 2026-11-17(Buff.OnRemove @00B0 NRE — 다른 직업 모드에서 확인): 만든 버프 전부의 FxOnRemove null 금지(BlueprintUtil.RequireOwnBuffFx 머리말). 등록 전에 — 어긋나면 던진다.
            BlueprintUtil.RequireOwnBuffFx(true);
            BlueprintUtil.RequireOwnLocalizedStrings();
            // 맨 끝(최종 리뷰 I2): 위 빌드가 모두 성공했을 때만 HunterClass 목록에 올린다. 하나라도 던지면 여기 오지 않으므로
            // BuildFailed=true 이고 아키타입은 캐릭터 생성 화면에 보이지 않는다.
            Archetype.Register();
        }
    }

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    static class BlueprintsCacheInitPatch
    {
        static bool built;

        [HarmonyPrepare]
        static bool Prepare() { return !BundleHooks.Enabled; }

        [HarmonyPostfix]
        internal static void Postfix()
        {
            if (built) return;
            built = true;
            try
            {
                Blueprints.BuildAll();
                if (Main.BuildSkipReasonEn == null) Main.Log("PackWarden blueprints built");
            }
            catch (Exception e)
            {
                Main.BuildFailed = true;
                Main.Entry.Logger.Error("PackWarden BuildAll failed: " + e);
                Main.Entry.Logger.LogException(e);
            }
            finally
            {
                Main.BuildAttempted = true; // 성공·실패가 정해진 뒤에만 세운다(BuildFailed 가 이때부터 의미를 갖는다)



            }
        }
    }

    // OnLocaleChanged 는 private static void — Harmony 는 이름 문자열로 접근. verify-patch-targets.ps1 이 존재를 검사한다.
    [HarmonyPatch(typeof(LocalizationManager), "OnLocaleChanged")]
    static class LocaleChangedPatch
    {
        [HarmonyPrepare]
        static bool Prepare() { return !BundleHooks.Enabled; }

        [HarmonyPostfix]
        static void Postfix()
        {
            Strings.InvalidateLanguage();
        }
    }

    // LocalizedString.LoadString 이 부르는 실제 조회 경로: LocalizationPack.GetText(string, bool).
    // "PackWarden." 키만 가로채 ko/en 을 조회 시점에 돌려준다. 그 밖의 키는 원래 경로 그대로.
    [HarmonyPatch(typeof(LocalizationPack), nameof(LocalizationPack.GetText), new Type[] { typeof(string), typeof(bool) })]
    static class GetTextPatch
    {
        [HarmonyPrepare]
        static bool Prepare() { return !BundleHooks.Enabled; }

        [HarmonyPrefix]
        static bool Prefix(string key, ref string __result)
        {
            if (!Strings.IsOurKey(key)) return true;
            __result = Strings.Resolve(key);
            return false;
        }
    }
}
