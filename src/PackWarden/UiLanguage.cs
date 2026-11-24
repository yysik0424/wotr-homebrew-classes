using System;

namespace PackWarden
{
    // 화면 언어 판정 규칙(게임 형식 없음 → tests\run-tests.ps1 이 컴파일해 값으로 실행한다). 2026-11-17: Strings.DetectKorean 의 판정식을 이 한 곳으로 옮겼다 —
    // 게임 안 점검 S14 choice-text 가 같은 규칙으로 「지금 어느 언어 문자열이 나와야 하는가」 를 정한다(C4: 판정식 복제 없음).
    // 근거(Strings.cs 머리 주석 · Assembly-CSharp 2026-11-17 직독): 이 게임(2.7.0x) 의 Locale 열거형에는 koKR 이 없다(enGB ruRU deDE frFR zhCN Sound esES ptBR itIT).
    // 한글 패치(WrathKoreanMod)는 영어 팩 문자열을 한국어로 덮어쓰므로 CurrentLocale 은 enGB 그대로다 → 본편 헌터 클래스 이름(현재 팩 문자열)에 한글이 있는지로 가른다.
    public static class UiLanguage
    {
        // 미래 대비: 열거형에 이 이름이 생기면 이름만으로 한국어.
        public const string KoreanLocaleName = "koKR";

        public static bool LocaleIsKorean(string localeName)
        {
            if (string.IsNullOrEmpty(localeName)) throw new InvalidOperationException("PackWarden: cannot detect UI language, the current locale name is empty");
            return localeName == KoreanLocaleName;
        }

        // 현재 팩에서 읽은 본편 헌터 클래스 이름. 비었으면 판정 불가 → 던진다(영어로 접지 않는다).
        public static bool NameIsKorean(string hunterClassName)
        {
            if (string.IsNullOrEmpty(hunterClassName)) throw new InvalidOperationException("PackWarden: cannot detect UI language, Hunter class name is empty");
            foreach (char c in hunterClassName)
                if (c >= '가' && c <= '힣') return true; // 한글 음절 '가'~'힣'
            return false;
        }
    }
}
