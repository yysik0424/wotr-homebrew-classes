



namespace ChaosWarrior
{
     
    // 일반 플레이에서 값이 남아 있으면(누군가 설정) 사용하지 않고 한 번 시끄럽게 보고한다 — 조용히 무시하지도, 조용히 쓰지도 않는다.
     
    // 아래 세 멤버를 읽는다. 강제 값을 정하는 점검 러너가 없으므로 배포 본문은 언제나 「강제 없음」(Honored false · ForcePicks null · ForceShellElement −1).
    public static class CheckHooks
    {



        public static int[] ForcePicks { get { return null; } }
        public static int ForceShellElement { get { return -1; } }
        internal static bool Honored(string site) { return false; }

    }
}
