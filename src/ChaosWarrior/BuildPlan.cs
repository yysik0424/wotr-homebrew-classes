using System;
using System.Collections.Generic;

namespace ChaosWarrior
{
     
    // report("builder", 이름 + ": " + 메시지) 로 계수한 뒤 재던진다(다음 빌더는 부르지 않는다 — 공용 BuildHook 이 BuildFailed 를 기록).
    public static class BuildPlan
    {
        public const string Site = "builder";

        public static void Execute(Action<string, string> report, IReadOnlyList<KeyValuePair<string, Action>> builders)
        {
            if (report == null) throw new ArgumentNullException("report");
            if (builders == null) throw new ArgumentNullException("builders");
            if (builders.Count == 0) throw new ArgumentException("ChaosWarrior: BuildPlan.Execute with no builders", "builders");
            var seen = new HashSet<string>();
            foreach (var b in builders)
            {
                if (string.IsNullOrEmpty(b.Key) || b.Value == null) throw new ArgumentException("ChaosWarrior: BuildPlan builder without a name or body", "builders");
                if (!seen.Add(b.Key)) throw new ArgumentException("ChaosWarrior: BuildPlan builder '" + b.Key + "' listed twice", "builders");
            }
            foreach (var b in builders)
            {
                try { b.Value(); }
                catch (Exception e)
                {
                    report(Site, b.Key + ": " + e.GetType().Name + ": " + e.Message);
                    throw;
                }
            }
        }
    }
}
