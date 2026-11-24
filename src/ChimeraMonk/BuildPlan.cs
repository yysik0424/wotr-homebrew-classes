using System;
using System.Collections.Generic;

namespace ChimeraMonk
{
     
    //  ① 의존 관문: 하나라도 없으면 setBuildFailed(true) 후 빌더 0개 호출·false(던지지 않는다 — 빌드 훅 경로를 깨지 않음, Spellfist Main.BuildAll 선례).
    //  ② 빌더: 순서대로 부른다. 예외 → report("builder", 이름 + ": " + 메시지) 후 그대로 재던짐(공용 훅 BlueprintsCacheInitPatch.Postfix 가 BuildFailed 를 세운다).
    public static class BuildPlan
    {
        public const string BuilderSite = "builder";

        public static bool Execute(Func<string, bool> resolves, Action<string, string> report, Action<bool> setBuildFailed, IReadOnlyList<KeyValuePair<string, Action>> builders)
        {
            if (resolves == null) throw new ArgumentNullException("resolves");
            if (report == null) throw new ArgumentNullException("report");
            if (setBuildFailed == null) throw new ArgumentNullException("setBuildFailed");
            if (builders == null) throw new ArgumentNullException("builders");
            if (builders.Count == 0) throw new ArgumentException("ChimeraMonk: BuildPlan.Execute with no builders", "builders");
            if (!DependencyGate.Run(resolves, report))
            {
                setBuildFailed(true);
                return false;
            }
            foreach (var b in builders)
            {
                if (string.IsNullOrEmpty(b.Key) || b.Value == null) throw new ArgumentException("ChimeraMonk: BuildPlan builder entry without a name or action", "builders");
                try { b.Value(); }
                catch (Exception e)
                {
                    report(BuilderSite, b.Key + ": " + e.Message);
                    throw;
                }
            }
            return true;
        }
    }
}
