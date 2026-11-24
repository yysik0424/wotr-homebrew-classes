namespace PackWarden
{
     
    // 개발 구성(tools\build.ps1 -Config Dev, /define:DEV_BUILD)에서는 러너의 Active 를 그대로, 배포 구성에서는 Checks 폴더가 컴파일되지 않으므로 항상 false.
    // 공용 폴더를 쓰지 않는 직업(무리 조련사)은 같은 모양의 사본을 자기 폴더에 둔다 — 두 파일이 namespace 줄 말고 어긋나면 tests\ReleaseBuildTests.ps1 이 실패한다.
    public static class DevFlags
    {



        public static bool ChecksActive { get { return false; } }

    }
}
