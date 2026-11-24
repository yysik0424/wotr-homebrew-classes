using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
    // [순수] 그림자 메아리 연출의 단계 결정(소유자 결정 2026-11-16 밤 2·4: 메아리 때만 나타난다 · 한꺼번에 나타나 차례로 시전하고 함께 흩어진다). 게임 형식 없음(modinfo testSources).
    //   원래 주문 종료 → [등장] 그림자 N 개가 자리에 드러남 → k번째: 발사 예정 − 동작 선행에 [시전 동작] → [발사](기존 EchoQueue.Fire 그대로, 예정 시각 그대로) → … → 마지막 메아리의 주문이 끝남 → 유지 시간 → [흩어짐] 함께 숨김.
     
    //   연출이 그 시계에 맞춘다 — 등장 → 첫 시전 동작 = 간격 − 동작 선행(0.5 − 0.35 = 0.15 s, 파생값). 시전 동작을 발사 전에 시작하지 못했으면(프레임 멈칫) 그 메아리의 연출만 생략(Late — 본체에서 나간다).
    // 연출을 할 수 없는 상태(그림자 없음·본체 기절·변신·은신·컷신 등)에서는 등장하지 않는다 → 메아리는 같은 시각에 본체에서 나간다.
    public struct StageFacts
    {
        public Tri ShadowsReady;       // 본체의 그림자 묶음이 이 작업의 메아리 수만큼 있고 전부 쓸 수 있다
        public Tri CasterInGame;
        public Tri CasterDead;
        public Tri CasterConscious;
        public Tri CasterPolymorphed;
        public Tri CasterInStealth;
        public Tri ModeAllows;         // 컷신 잠금이 아니고 게임 모드가 일반(Default) 또는 일시정지(Pause)다 — 대화·컷신·전체 화면 UI·휴식 등에서는 드러내지 않는다
    }

    public static class StageReasons
    {
        public const string NoShadowUnits = "NoShadowUnits", CasterOutOfGame = "CasterOutOfGame", CasterDead = "CasterDead", CasterUnconscious = "CasterUnconscious",
            CasterPolymorphed = "CasterPolymorphed", CasterInStealth = "CasterInStealth", GameMode = "GameMode", CombatEnded = "CombatEnded", ShadowLost = "ShadowLost",
            // 2026-11-17 2차 독립 검토 중요-1: 그림자 자리가 본체 자신의 메아리 원뿔·선 밖이 아니다(ShadowClearance) — 등장 때(길 안으로 당겨진 자리) / 발사 직전(본체·대상이 움직였다).
            SlotNotClear = "SlotNotClear", InLineOfFire = "InLineOfFire",
             
            WindupLate = "WindupLate";
        public static readonly string[] All = { NoShadowUnits, CasterOutOfGame, CasterDead, CasterUnconscious, CasterPolymorphed, CasterInStealth, GameMode, CombatEnded, ShadowLost, SlotNotClear, InLineOfFire, WindupLate };
    }

    public enum StageVerdict { Show, Skip, Unknown }
    public enum StageCue { None, Windup, Late }
    public enum CloseStep { Keep, Hide }

    public static class EchoStageRule
    {
        // 드러내도 되는가(등장 때) / 계속 보여도 되는가(드러난 동안 매 틱). Unknown = 입력을 읽을 수 없다(reason = 입력 이름 — 호출자가 보고하고 드러내지 않는다).
        // Skip = 정당한 「지금은 아님」(reason = StageReasons — 오류가 아니다, 사유별 계수 + 정보 줄).
        public static StageVerdict Decide(StageFacts f, out string reason)
        {
            reason = f.ShadowsReady == Tri.Unknown ? "ShadowsReady" : f.CasterInGame == Tri.Unknown ? "CasterInGame" : f.CasterDead == Tri.Unknown ? "CasterDead" : f.CasterConscious == Tri.Unknown ? "CasterConscious"
                : f.CasterPolymorphed == Tri.Unknown ? "CasterPolymorphed" : f.CasterInStealth == Tri.Unknown ? "CasterInStealth" : f.ModeAllows == Tri.Unknown ? "ModeAllows" : null;
            if (reason != null) return StageVerdict.Unknown;
            reason = f.CasterInGame == Tri.No ? StageReasons.CasterOutOfGame : f.CasterDead == Tri.Yes ? StageReasons.CasterDead : f.CasterConscious == Tri.No ? StageReasons.CasterUnconscious
                : f.CasterPolymorphed == Tri.Yes ? StageReasons.CasterPolymorphed : f.CasterInStealth == Tri.Yes ? StageReasons.CasterInStealth : f.ModeAllows == Tri.No ? StageReasons.GameMode
                : f.ShadowsReady == Tri.No ? StageReasons.NoShadowUnits : null;
            return reason == null ? StageVerdict.Show : StageVerdict.Skip;
        }

         
        //   due = 그 메아리의 발사 예정 시각(EchoQueueCore.DueAt — 원래 주문이 안 끝났으면 null) · lastCue = 이미 Windup 또는 Late 로 처리한 메아리 번호(한 메아리에 한 번).
        //   Windup = 지금 시전 동작을 시작(due − castLead ≤ now < due) · Late = 이번 틱에 발사되는데(now ≥ due) 아직 동작을 시작하지 못했다 → 그 메아리의 연출을 생략(본체에서 나간다).
        public static StageCue Cue(double? due, double now, int index, int lastCue) { return CueCore(due, now, index, lastCue, ShadowVisualDials.CastLeadSeconds); }
        public static StageCue CueCore(double? due, double now, int index, int lastCue, float castLead)
        {
            if (index < 1) throw new ArgumentOutOfRangeException("index", index, "ShadowSorcerer: echo index must be >= 1");
            if (!(castLead >= 0)) throw new ArgumentOutOfRangeException("castLead", castLead, "ShadowSorcerer: cast lead must be >= 0");
            if (!due.HasValue || lastCue >= index) return StageCue.None;
            if (now >= due.Value) return StageCue.Late;
            return now >= due.Value - castLead ? StageCue.Windup : StageCue.None;
        }

        // 흩어짐: 작업이 끝난 시각(closingAt)부터, 쏜 메아리의 주문이 아직 도는 동안(anyPending)은 기다린다 — 한도(waitLimit)를 넘기면 더 기다리지 않는다.
        // 조용해진 시각(quietAt — 이 함수가 한 번 적는다)부터 유지 시간(linger)이 지나면 Hide.
        public static CloseStep Close(double closingAt, ref double? quietAt, double now, bool anyPending) { return CloseCore(closingAt, ref quietAt, now, anyPending, ShadowVisualDials.FadeWaitLimitSeconds, ShadowVisualDials.LingerSeconds); }
        public static CloseStep CloseCore(double closingAt, ref double? quietAt, double now, bool anyPending, float waitLimit, float linger)
        {
            if (!quietAt.HasValue)
            {
                if (anyPending && now - closingAt <= waitLimit) return CloseStep.Keep;
                quietAt = now;
            }
            return now - quietAt.Value >= linger ? CloseStep.Hide : CloseStep.Keep;
        }
    }

    // [순수] 그림자의 시전 동작 스타일 고르기. 스타일 = 엔진 UnitAnimationActionCastSpell.CastAnimationStyle 의 정수 값(게임 형식 없음 — 시험이 DLL 의 열거형과 대조한다).
    public enum CastStylePick { Exact, KnownSubstitute, UnexpectedSubstitute, None }

    public static class ShadowCastRule
    {
        // 시전 동작이 자세를 적용할 본체: 미룬 자리면 본체, 아니면 없음.
        public static T StanceOwner<T>(bool deferred, T owner) where T : class
        {
            if (owner == null) throw new ArgumentNullException("owner");
            return deferred ? owner : null;
        }

        // 그림자 모델의 애니메이션 세트(Ghost_AnimationSet_CastSpell)에 있는 시전 스타일. 출처: 설치본 Bundles\ghost.animations 직독 2026-11-17(UnityPy) —
        //   SelfTouch 0 · Touch 1 · Point 2 · Directional 3 · Omni 4 · Self 5. 주문이 이 밖의 스타일(Thrown 6 · BreathWeapon 11 등)을 원하면 대체가 「알려진 대체」(정보 줄),
        //   이 안의 스타일이 실행 때 세트에 없으면 「뜻밖의 대체」(오류 줄 — 모델·세트가 바뀌었다).
        public static readonly IReadOnlyList<int> ExpectedStyles = new[] { 0, 1, 2, 3, 4, 5 };
        // 대체 순서(작성자 다이얼 — 눈 점검으로 확정): Directional → Point → Omni → Touch → Self → SelfTouch. 대상 쪽으로 팔을 뻗는 동작을 먼저.
        public static readonly IReadOnlyList<int> SubstituteOrder = new[] { 3, 2, 4, 1, 5, 0 };
        // 시전 핸들의 CastingTime(초). 0 = 준비 동작 없이 시전 클립만 한 번(엔진 UnitAnimationActionCastSpell.OnStart IL_017F~01A7: CastingTime − 클립 길이 ÷ 속도 < 1 이면 시전 클립만).
        public static readonly float CastingTimeSeconds = 0f;

        // wanted = 주문의 스타일, available = 그림자의 시전 액션에 시전 클립이 있는 스타일들(실행 때 읽은 값).
        public static CastStylePick Pick(int wanted, IEnumerable<int> available, out int style)
        {
            if (available == null) throw new ArgumentNullException("available");
            var have = new HashSet<int>(available);
            if (have.Contains(wanted)) { style = wanted; return CastStylePick.Exact; }
            foreach (var s in SubstituteOrder)
                if (have.Contains(s)) { style = s; return ExpectedStyles.Contains(wanted) ? CastStylePick.UnexpectedSubstitute : CastStylePick.KnownSubstitute; }
            // 대체 순서에 없는 스타일만 남아 있다 = 세트가 알던 모양이 아니다 → 있는 것 중 가장 작은 값으로, 뜻밖의 대체.
            if (have.Count > 0) { style = have.Min(); return CastStylePick.UnexpectedSubstitute; }
            style = -1;
            return CastStylePick.None;
        }
    }
}
