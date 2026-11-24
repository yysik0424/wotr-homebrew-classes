using System;

namespace ShadowSorcerer
{
     
     
    //   원래 주문이 끝나기를 기다리는 동안 작업 시작(T0)부터 시한(EchoWaitGameSeconds, J8 = 12 s)을 넘기면 남은 것 Drop(WaitTimeout). 끝난 뒤에는 시한이 없다. 원래 종료 사실 결측 = Abort.
    //   메아리 사이는 실제 발사 시각 기준 ≥ 간격(프레임이 멈칫해도 한 번에 여러 개를 쏘지 않는다).
    //   쏜 결과: Ok → 다음 · 대상 죽음/무효·시전자 쓰러짐 → 남은 것 모두 Drop · 메아리 시전 실패 → 그 1개 Drop(EchoCastFailed) · 결과 결측·대상 Unknown → Abort(남은 것 발사 0).
    public enum QueueAction { Wait, Fire, Drop, Abort, Done }

    public struct QueueStep
    {
        public QueueAction Action;
        public int Count;
        public string Reason;
        public override string ToString() { return Action + (Count > 0 ? " x" + Count : "") + (Reason == null ? "" : " (" + Reason + ")"); }
    }

    public enum FireOutcome { Ok, TargetDead, TargetInvalid, CasterDown, EchoCastFailed, ResultMissing, TargetUnknown }

    public static class DudReasons
    {
        public const string TargetDead = "TargetDead", TargetInvalid = "TargetInvalid", CasterDown = "CasterDown", WaitTimeout = "WaitTimeout", EchoCastFailed = "EchoCastFailed", Reloaded = "Reloaded";
        public static readonly string[] All = { TargetDead, TargetInvalid, CasterDown, WaitTimeout, EchoCastFailed, Reloaded };
    }

    // EchoQueueCore.Step 의 바깥(생산 = EchoQueue.StepHost, 시험 = 가짜). 연출 훅 셋(Stage*)은 돌려주는 값이 없다 — 대기열 결정에 들어갈 길이 없다.
    public interface IEchoStepHost
    {
        bool? OriginalEnded();                                   // 원래 주문이 끝났는가(null = 읽을 수 없다 → Abort)
        FireOutcome Fire();                                      // 메아리 1발(규칙)
        void Report(QueueStep s);                                // Drop / Abort 기록(규칙)
        void StageBeforeNext(double now, bool? originalEnded);   // 연출 전용
        void StageAfterFire(int index, FireOutcome outcome);     // 연출 전용
        void StageFailed(Exception e);                           // 연출 훅이 던졌다 — 보고만(작업은 계속)
    }

    public sealed class EchoJobState
    {
        public readonly double T0;
        public readonly int Total;
        public int Fired, Dropped;
        public double? EndedAt;
        public double? LastFireAt; // 마지막으로 쏜(쏘려던) 시각 — 다음 메아리는 여기서 간격 뒤(멈칫 뒤 몰아 쏘기 금지, 검토 소수 3)
        public bool Aborted;
         
        public EchoJobState(double t0, int total)
        {
            if (total <= 0) throw new ArgumentOutOfRangeException("total", total, "ShadowSorcerer: echo job with no echoes");
            T0 = t0; Total = total;
        }
        public int Remaining { get { return Total - Fired - Dropped; } }
        public bool Finished { get { return Aborted || Remaining == 0; } }
    }

    public static class EchoQueueCore
    {
        public static QueueStep Next(EchoJobState j, double now, bool? originalEnded) { return NextCore(j, now, originalEnded, ShadowRules.EchoIntervalSeconds, ShadowRules.EchoWaitGameSeconds); }

        public static QueueStep NextCore(EchoJobState j, double now, bool? originalEnded, float interval, float waitLimit)
        {
            if (j == null) throw new ArgumentNullException("j");
            if (j.Finished) return new QueueStep { Action = QueueAction.Done };
            if (!j.EndedAt.HasValue)
            {
                if (!originalEnded.HasValue) { j.Aborted = true; return new QueueStep { Action = QueueAction.Abort, Count = j.Remaining, Reason = "original spell state unread" }; }
                if (!originalEnded.Value)
                    return now - j.T0 > waitLimit ? Drop(j, j.Remaining, DudReasons.WaitTimeout) : new QueueStep { Action = QueueAction.Wait }; // 시한 = 원래 주문이 끝나기를 기다리는 창(계획 J8)
                j.EndedAt = now;
            }
            double due = DueAt(j, interval).Value;
            if (now < due) return new QueueStep { Action = QueueAction.Wait };
            j.LastFireAt = now;
            return new QueueStep { Action = QueueAction.Fire, Count = 1 };
        }

        // 다음 메아리의 발사 예정 시각. 원래 주문이 아직 안 끝났으면(끝난 시각을 적기 전) null.
         
        public static double? DueAt(EchoJobState j, float interval)
        {
            if (j == null) throw new ArgumentNullException("j");
            if (!j.EndedAt.HasValue) return null;
            return (j.LastFireAt.HasValue ? j.LastFireAt.Value : j.EndedAt.Value) + interval;
        }

         
        //   연출 훅(StageBeforeNext·StageAfterFire)은 값을 돌려줄 수 없고(void), 던지면 여기서 잡아 StageFailed 로 넘긴 뒤 그대로 진행한다(연출 실패가 작업을 중단시키지 않는다).
        //   훅이 받는 것은 판독값(원래 주문 종료 여부·발사 결과)뿐이다. 그 밖의 결정(Next·AfterFire)·보고(Report)·발사(Fire)는 연출 유무와 같은 순서로 같은 값을 쓴다.
        public static void Step(EchoJobState j, double now, IEchoStepHost host)
        {
            if (j == null) throw new ArgumentNullException("j");
            if (host == null) throw new ArgumentNullException("host");
            for (int guard = 0; guard < j.Total + 1; guard++)
            {
                bool? ended = host.OriginalEnded();
                try { host.StageBeforeNext(now, ended); }
                catch (Exception e) { host.StageFailed(e); }
                var s = Next(j, now, ended);
                if (s.Action == QueueAction.Wait || s.Action == QueueAction.Done) return;
                if (!Apply(j, s, host)) return;
                if (s.Action != QueueAction.Fire) return;
                int index = j.Fired + j.Dropped + 1;
                var outcome = host.Fire();
                try { host.StageAfterFire(index, outcome); }
                catch (Exception e) { host.StageFailed(e); }
                if (!Apply(j, AfterFire(j, outcome), host)) return;
            }
        }

        // Drop → 소멸 기록, Abort → 오류 보고(둘 다 host.Report). 계속해도 되면 true.
        static bool Apply(EchoJobState j, QueueStep s, IEchoStepHost host)
        {
            switch (s.Action)
            {
                case QueueAction.Drop: host.Report(s); return !j.Finished;
                case QueueAction.Abort: host.Report(s); return false;
                case QueueAction.Done: return false;
            }
            return true;
        }

        // 쏜(또는 쏘려던) 결과를 반영하고 다음 할 일(Drop/Abort/Wait)을 돌려준다.
        public static QueueStep AfterFire(EchoJobState j, FireOutcome o)
        {
            if (j == null) throw new ArgumentNullException("j");
            switch (o)
            {
                case FireOutcome.Ok: j.Fired++; return new QueueStep { Action = j.Finished ? QueueAction.Done : QueueAction.Wait };
                case FireOutcome.EchoCastFailed: return Drop(j, 1, DudReasons.EchoCastFailed);
                case FireOutcome.TargetDead: return Drop(j, j.Remaining, DudReasons.TargetDead);
                case FireOutcome.TargetInvalid: return Drop(j, j.Remaining, DudReasons.TargetInvalid);
                case FireOutcome.CasterDown: return Drop(j, j.Remaining, DudReasons.CasterDown);
                case FireOutcome.ResultMissing: case FireOutcome.TargetUnknown:
                    j.Aborted = true;
                    return new QueueStep { Action = QueueAction.Abort, Count = j.Remaining, Reason = o.ToString() };
            }
            throw new ArgumentOutOfRangeException("o", o, "ShadowSorcerer: unknown fire outcome");
        }

        // 지역을 다시 불러오면 남은 것 모두 Drop(Reloaded).
        public static QueueStep Reloaded(EchoJobState j) { return Drop(j, j.Remaining, DudReasons.Reloaded); }

        static QueueStep Drop(EchoJobState j, int n, string reason)
        {
            if (n < 0 || n > j.Remaining) throw new InvalidOperationException("ShadowSorcerer: drop " + n + " of " + j.Remaining + " remaining");
            j.Dropped += n;
            return new QueueStep { Action = QueueAction.Drop, Count = n, Reason = reason };
        }
    }
}
