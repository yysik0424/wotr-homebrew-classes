namespace HomebrewClasses.Loader
{
    internal static class LoaderText
    {
        internal const string Loaded = "실림 / Loaded";
        internal const string Off = "꺼짐 / Off";
        internal const string Pending = "준비 중 / Pending";
        internal const string Blocked = "막힘 · 단독 모드를 제거한 뒤 재시작하세요 / Blocked · Remove the standalone copy and restart";
        internal const string DuplicateWarning = "단독 직업 모드가 함께 설치되어 있습니다 / Standalone class mods were found";
        internal const string Restart = "변경 사항은 게임 재시작 후 적용됩니다 / Changes apply after restarting the game";
        internal const string InvalidSettings = "Settings.xml에 직업 설정이 없습니다. 설정 파일을 복원한 뒤 다시 시작하세요. / Settings.xml has no class settings. Restore the file and restart.";
        internal const string LegacySave = "기존 단독판 세이브는 일반 불러오기로 여세요. ModMenu에서 ‘추가 모드 비활성화’를 열었다면 ‘취소’를 눌러 통합본을 켜 두세요.\nUse normal Load for standalone saves. If you opened ModMenu’s “Disable extra mods”, choose Cancel to keep this bundle enabled.";
        internal static string Skipped(string ko, string en) { return "건너뜀 · " + ko + " / Skipped · " + en; }
        internal static string Failed(string reason) { return "실패 · 재시작 필요 / Failed · Restart required (" + reason + ")"; }
    }
}
