using System;
using System.Text.RegularExpressions;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace WotrClassMod.Common
{
    // 모드 하나의 정체(이름·Harmony id·문자열 접두·UMM 항목)와 블루프린트 생성 상태. 공용 코드는 모드 이름을 리터럴로 갖지 않고 여기서 읽는다.
     
    // Init 전에 읽으면 예외(기본값으로 조용히 진행하지 않는다). Init 을 두 번 불러도 예외.
    public static class ModContext
    {
        // Id 는 Harmony id·문자열 접두·로그 접두로 쓰인다. build.ps1 의 -Mod 검사와 같은 규칙(tools\build.ps1 줄 3).
        static readonly Regex IdPattern = new Regex("^[A-Za-z][A-Za-z0-9_]*$");

        static string id, harmonyId, stringPrefix;
        static UnityModManager.ModEntry entry;
        static Action buildAll;

        public static bool IsInitialized { get; private set; }

        public static string Id { get { Require(); return id; } }
        public static string HarmonyId { get { Require(); return harmonyId; } }
        public static string StringPrefix { get { Require(); return stringPrefix; } }
        public static UnityModManager.ModEntry Entry { get { Require(); return entry; } }
        public static Action BuildAll { get { Require(); return buildAll; } }

        // 블루프린트 생성이 한 번이라도 실패하면 true. 게임 부팅은 막지 않고, 게임 안 점검 러너가 이 값을 읽는다.
        public static bool BuildFailed { get; internal set; }
        // BlueprintsCache.Init 후크가 BuildAll 을 시도했으면 true. false 인 동안 BuildFailed=false 는 "성공"이 아니라 "아직 모름"이다(점검 결과 첫 줄 Unknown).
        public static bool BuildAttempted { get; internal set; }

        public static string BuildSkipReasonKo { get; private set; }
        public static string BuildSkipReasonEn { get; private set; }

        public static void MarkSkipped(string ko, string en)
        {
            if (string.IsNullOrWhiteSpace(ko) || string.IsNullOrWhiteSpace(en))
                throw new ArgumentException("Both descriptions of the missing content are required.");
            BuildSkipReasonKo = ko;
            BuildSkipReasonEn = en;
        }

        public static void Init(UnityModManager.ModEntry modEntry, string modId, Action modBuildAll)
        {
            if (IsInitialized) throw new InvalidOperationException("ModContext.Init called twice (already initialized as '" + id + "', second call '" + modId + "')");
            if (modEntry == null) throw new ArgumentNullException("modEntry");
            if (modId == null || !IdPattern.IsMatch(modId)) throw new ArgumentException("ModContext.Init: invalid mod id '" + modId + "'", "modId");
            if (modBuildAll == null) throw new ArgumentNullException("modBuildAll");
            id = modId;
            harmonyId = modId;
            stringPrefix = modId + ".";
            entry = modEntry;
            buildAll = modBuildAll;
            IsInitialized = true;
        }

        public static void Log(string msg) { Entry.Logger.Log(msg); }

        public static void LogError(string msg) { Entry.Logger.Error(msg); }

        static void Require()
        {
            if (!IsInitialized) throw new InvalidOperationException("ModContext used before ModContext.Init (call it first in Main.Load)");
        }
    }
}
