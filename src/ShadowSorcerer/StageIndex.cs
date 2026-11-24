using System;
using System.Collections.Generic;

namespace ShadowSorcerer
{
     
    // 일반 사전에 넣고 명시적으로 뺀다. 작업이 큐를 떠나는 경로는 둘이다: EchoStage.JobFinished(Take) · EchoStage.DropAll(Clear).
    public sealed class StageIndex<TJob, TShow> where TJob : class where TShow : class
    {
        readonly Dictionary<TJob, TShow> entries = new Dictionary<TJob, TShow>();

        public int Count { get { return entries.Count; } }

        public void Add(TJob job, TShow show)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (show == null) throw new ArgumentNullException("show");
            entries.Add(job, show); // 같은 작업의 두 번째 무대는 결함이다: 던진다
        }

        public bool TryGetValue(TJob job, out TShow show)
        {
            if (job == null) throw new ArgumentNullException("job");
            return entries.TryGetValue(job, out show);
        }

        // 작업이 끝났다: 있으면 꺼내면서 지운다.
        public bool Take(TJob job, out TShow show)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (!entries.TryGetValue(job, out show)) return false;
            entries.Remove(job);
            return true;
        }

        public void Clear() { entries.Clear(); }
    }
}
