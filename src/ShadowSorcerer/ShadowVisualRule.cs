using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
    // [순수] 그림자 메아리 연출(소유자 결정 2026-11-16 밤)의 수치와 결정. 게임 형식 없음(modinfo testSources) — 시험 exe 가 이 형식 그대로 실행한다.
     

    // 작성자 다이얼 — 눈 점검으로 확정. 출처 = 없음(「보기 좋은가」가 기준인 값, 정찰 §8 J5 「임시값으로 빌드 후 눈 점검으로 확정」).
    // const 금지(ShadowRules 와 같은 이유): 소비하는 규칙의 IL 이 ldsfld 로 읽어야 시험이 소비를 확인한다.
    public static class ShadowVisualDials
    {
        // 자리(미터). 「앞」 = 본체에서 주문 대상 쪽. 1번 = 왼쪽 뒤, 2번 = 오른쪽 뒤, 3번 = 바로 뒤.
        // 2026-11-17 2차 독립 검토 중요-1: 세 자리 모두 본체 「뒤」다(옛 1·2번 = 앞 0.6 m · 옆 1.1 m 는 본체 자신의 메아리 원뿔·선에 들었다).
        //   값은 보기 좋음 다이얼이지만, 아래 ValidateCore 가 ShadowClearance(엔진 원뿔·선 판정의 충분조건)를 설계 몸집으로 통과하는지 검사한다 — 통과 못 하는 값은 빌드가 멈춘다.
        //   1·2번: 앞 성분 −1.3 · 거리 √(1.3² + 1.0²) = 1.64 m ≥ √2 × (몸집 0.5 + 여유 0.5) = 1.41 m. 3번: 앞 성분 −2.0 · 거리 2.0 m.
        public static readonly float SlotPairBackMeters = 1.3f; // 1·2번이 본체 뒤로 물러나는 거리
        public static readonly float SlotSideMeters = 1.0f;     // 1·2번이 본체 옆으로 벌어지는 거리
        public static readonly float SlotBackMeters = 2.0f;     // 3번이 본체 바로 뒤로 물러나는 거리
        // 그림자 손 뼈대를 읽을 수 없을 때 투사체가 나가는 높이(그림자 발밑 기준, 미터). 중형 사람 모델의 가슴 높이 어림.
        public static readonly float FallbackLaunchHeightMeters = 1.3f;
         
        //   등장 → 첫 시전 동작은 파생값 = 간격 − 동작 선행(0.5 − 0.35 = 0.15 s). 동작 선행은 간격보다 짧아야 한다(ValidateCore).
        public static readonly float CastLeadSeconds = 0.35f;   // 시전 동작 시작 → 주문 발사
        public static readonly float LingerSeconds = 0.9f;      // 마지막 메아리의 주문이 끝난 뒤 흩어지기까지
        // 흩어짐이 주문 종료를 기다리는 한도(게임 초). 넘으면 그대로 흩어진다(그림자가 남지 않게) — 오래 도는 주문(지속 광선 등)의 상한.
        public static readonly float FadeWaitLimitSeconds = 8f;

        // BuildAll 이 부른다. 어긋나면 던진다(빌드 실패). interval = 메아리 간격(규칙 값 — 읽기만 한다).
        public static void Validate() { ValidateCore(SlotPairBackMeters, SlotSideMeters, SlotBackMeters, FallbackLaunchHeightMeters, ShadowRules.EchoIntervalSeconds, CastLeadSeconds, LingerSeconds, FadeWaitLimitSeconds); }

        public static void ValidateCore(float pairBack, float side, float back, float height, float interval, float lead, float linger, float fadeWait)
        {
            Finite("SlotPairBackMeters", pairBack); Finite("SlotSideMeters", side); Finite("SlotBackMeters", back); Finite("FallbackLaunchHeightMeters", height);
            Finite("EchoIntervalSeconds", interval); Finite("CastLeadSeconds", lead); Finite("LingerSeconds", linger); Finite("FadeWaitLimitSeconds", fadeWait);
            // 동작 선행이 메아리 간격 이상이면 등장 → 첫 동작 사이가 0 이하(등장하자마자 동작) — 연출이 발사 시계에 맞출 자리가 없다.
            if (!(lead < interval)) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualDials.CastLeadSeconds " + lead + " must be shorter than the echo interval " + interval + " (the visual fits the echo clock, it never delays it)");
            if (!(pairBack > 0) || !(back > 0)) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualDials pair-back/back must be > 0 - every shadow stands behind the caster (got " + pairBack + " / " + back + ")");
            // 좌우가 0 이면 1·2번 그림자가 같은 자리에 겹친다.
            if (!(side > 0)) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualDials.SlotSideMeters " + side + " must be > 0 (slots 1 and 2 would overlap)");
            // 2차 독립 검토 중요-1: 세 자리 모두 본체 자신의 메아리 원뿔·선 밖(설계 몸집 + 여유로) — 아니면 빌드가 멈춘다.
            for (int slot = 1; slot <= ShadowPlacement.SlotCount; slot++)
            {
                double f, r;
                ShadowPlacement.SlotOffset(slot, pairBack, side, back, out f, out r);
                var v = ShadowClearance.Check(0, 0, 0, 10, r, f, ShadowClearance.DesignCorpulenceMeters, ShadowVisualLimits.ClearanceMarginMeters);
                if (v != ClearanceVerdict.Clear) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualDials slot " + slot + " (forward " + f + ", right " + r + ") is not clear of the caster's own cone/line (" + v + ")");
            }
            if (!(height > 0)) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualDials.FallbackLaunchHeightMeters " + height + " must be > 0");
            if (lead < 0 || linger < 0) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualDials times must be >= 0 (got " + lead + " / " + linger + ")");
            if (!(fadeWait > 0)) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualDials.FadeWaitLimitSeconds " + fadeWait + " must be > 0");
        }

        static void Finite(string name, float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualDials." + name + " is not a finite number");
        }
    }

    // 연출의 안전 한도(다이얼과 따로 둔다 — 다이얼은 「보기 좋은가」, 여기는 「폭주하지 않는가」). const 금지(다이얼과 같은 이유).
    public static class ShadowVisualLimits
    {
        // 한 본체가 한 지역에서 그림자 묶음을 만들 수 있는 횟수(첫 생성 포함). 넘으면 오류 줄 1회 뒤 그 지역에서는 연출을 끈다 — 메아리 규칙은 그대로다.
        // 출처: 없음(작성자 값 — 소유자 확인 대상). 근거: 정당한 재생성은 분신 수가 바뀌는 레벨업(ShadowRules.EchoCountLevels 3 단 → 최대 2 회)과 본체 사망·부활·지역 이탈 복귀뿐이라
        //   한 지역에서 8 회를 넘기면 「무언가 그림자를 계속 지운다 / 자격이 매 틱 뒤집힌다」로 본다(독립 검토 F8: 재생성 무제한 — 매 틱 유닛 생성).
        public static readonly int MaxPoolCreationsPerArea = 8;

        // 숨겨 둔 그림자가 게임 안에 돌아와 있는 것(감시선 Exposed)을 우리가 다시 게임 밖으로 돌린 횟수의 지역당 한도(본체마다). 넘으면 그 지역의 연출을 끈다(묶음을 지우고 실패 표식 — 오류 줄 1회).
        // 출처: 없음(작성자 값 — 소유자 확인 대상). 근거: 우리 코드에는 숨긴 유닛을 게임 안에 넣는 경로가 없다(SetHidden 은 의도를 먼저 적고 실제 상태를 맞춘다) →
        //   정당하게 생길 수 있는 것은 컷신·다른 모드가 한두 번 되돌리는 정도. 3 회를 넘기면 「무언가 매 틱 되돌린다(줄다리기)」로 보고 더 다투지 않는다(2차 독립 검토 중요-2).
        public static readonly int MaxExposedRestoresPerArea = 3;

        // 그림자 자리 판정(ShadowClearance)의 여유(미터) — 본체 자신의 메아리 원뿔·선이 드러난 그림자를 고르지 않게 하는 안전 거리.
        // 출처: 없음(작성자 값 — 소유자 확인 대상). 근거: 우리가 자리를 확인하는 순간(EchoVisuals.Bind — Rulebook.Trigger 직전)과 엔진이 시전 위치를 읽는 순간
        //   (그 메아리 실행 과정의 첫 Tick — EchoQueue.Fire 주석: 효과는 AbilityExecutionController 의 다음 Tick) 사이에 본체가 움직일 수 있는 거리 + 수치 오차.
        //   한두 프레임 이동(수 cm ~ 수십 cm)보다 넉넉히 크게, 그림자 몸집(0.5 m)과 같은 크기로 잡았다. 작아서 그림자가 맞으면 감시선(버프 집합·규칙 당사자)이 오류 줄로 드러낸다.
        public static readonly float ClearanceMarginMeters = 0.5f;

        public static void Validate() { ValidateCore(MaxPoolCreationsPerArea); ValidateRestores(MaxExposedRestoresPerArea); ValidateMargin(ClearanceMarginMeters); }
        public static void ValidateCore(int maxCreations)
        {
            if (maxCreations < 1) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualLimits.MaxPoolCreationsPerArea " + maxCreations + " must be >= 1 (the first creation counts)");
        }
        public static void ValidateRestores(int maxRestores)
        {
            if (maxRestores < 1) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualLimits.MaxExposedRestoresPerArea " + maxRestores + " must be >= 1");
        }
        public static void ValidateMargin(float margin)
        {
            if (float.IsNaN(margin) || float.IsInfinity(margin) || !(margin > 0)) throw new InvalidOperationException("ShadowSorcerer: ShadowVisualLimits.ClearanceMarginMeters " + margin + " must be a finite number > 0");
        }
    }

    public enum ClearanceVerdict { Clear, InFront, TooClose, NoDirection, Unreadable }

    // [순수] 드러난 그림자가 본체 자신의 메아리 원뿔·선에 들지 않는가(2차 독립 검토 중요-1). 엔진 판정을 옮긴 사본이 아니라 그 충분조건이다(시험이 엔진 사본으로 전수 대조).
    // 엔진(Assembly-CSharp 직독 2026-11-17):
    //   선   AbilityDeliverProjectile.WouldTargetPointLine IL_0017~003B: dot = (To2D(눈 위치 − 시전 위치) · 방향) 이 0 이하면 거짓 — 폭과 무관.
    //   원뿔 WouldTargetPointCone IL_0032~007F: 방향과 45° 안이면 참, 아니면 ±45° 경계선(시전 위치에서 앞으로 길이 distance)이 반지름 = 몸집 원에 닿으면 참.
    //        경계선은 시전 위치에서 「앞으로만」 뻗으므로 뒤쪽 점(dot < 0, 방향과 90° 넘게)과 경계선의 거리는 |p| × sin(각 − 45°) ≥ |p| × sin 45° 이다.
    //   시전 위치 = 시전자 EyePosition(2D = 발 위치, <Deliver>d__35 IL_0081), 방향 = 대상 지점 − 시전 위치(XZ 정규화, <Deliver>d__36 IL_00C8~00E3).
    // 충분조건: dot ≤ −여유 ∧ |p| ≥ √2 × (몸집 + 여유) → 선·원뿔 판정 모두 거짓(어떤 선 폭·원뿔 길이에도).
    // NoDirection = 대상이 본체 위(방향을 정할 수 없다 — 엔진은 이때 원뿔이 방향 0 벡터라 사거리 안 전부를 고른다: Vector2.Angle(v, 0) = 0). 원뿔·선 주문이면 안전하지 않다.
    public static class ShadowClearance
    {
        // 그림자 유닛의 설계 몸집(미터). 출처: Bundles\9dc6267b1d7e2a947a0cb7e6328f8905.unit(BCT_ShadowHalfElfMale) 의 UnitEntityView.m_Corpulence = 0.5(타입트리 직독 2026-11-17)
        //   × 크기 배율(중형 = 원본 크기) · 엔진 UnitEntityView.get_Corpulence IL_002C~0045 = max(m_Corpulence × 배율, 0.5). 실행 때는 이 값이 아니라 유닛의 Corpulence 를 되읽는다 — 이 값은 다이얼 검사용.
        public static readonly double DesignCorpulenceMeters = 0.5;

        public static ClearanceVerdict Check(double casterX, double casterZ, double targetX, double targetZ, double shadowX, double shadowZ, double corpulence, double margin)
        {
            foreach (var v in new[] { casterX, casterZ, targetX, targetZ, shadowX, shadowZ, corpulence, margin })
                if (double.IsNaN(v) || double.IsInfinity(v)) return ClearanceVerdict.Unreadable;
            if (!(corpulence > 0) || !(margin > 0)) return ClearanceVerdict.Unreadable;
            double dx = targetX - casterX, dz = targetZ - casterZ;
            double len = Math.Sqrt(dx * dx + dz * dz);
            if (len < ShadowPlacement.MinDirectionMeters) return ClearanceVerdict.NoDirection;
            dx /= len; dz /= len;
            double px = shadowX - casterX, pz = shadowZ - casterZ;
            if (px * dx + pz * dz > -margin) return ClearanceVerdict.InFront;
            if (Math.Sqrt(px * px + pz * pz) < Math.Sqrt(2.0) * (corpulence + margin)) return ClearanceVerdict.TooClose;
            return ClearanceVerdict.Clear;
        }

        // 그 판정으로 그림자를 보여도 되는가. lineOrCone = 메아리 주문이 원뿔·선 투사체로 전달되는가(null = 읽을 수 없다).
        //   Clear = 된다 · NoDirection = 원뿔·선이 아니라고 읽혔을 때만(선은 방향 0 이면 dot = 0 이라 거짓이지만 원뿔은 참 — 하나로 묶어 막는다) · 그 밖 = 안 된다.
        public static bool Allows(ClearanceVerdict v, bool? lineOrCone)
        {
            if (v == ClearanceVerdict.Clear) return true;
            if (v == ClearanceVerdict.NoDirection) return lineOrCone == false;
            return false;
        }
    }

    public struct Vec3d
    {
        public readonly double X, Y, Z;
        public Vec3d(double x, double y, double z) { X = x; Y = y; Z = z; }
        public override string ToString() { return "(" + X.ToString("0.###") + ", " + Y.ToString("0.###") + ", " + Z.ToString("0.###") + ")"; }
    }

    public enum FacingSource { Target, CasterFacing }

    public struct SlotPlace
    {
        public Vec3d Point;
        public double FacingDegrees;   // 그림자가 바라볼 방향(Y축 각도, 0 = +Z, 90 = +X — 엔진 Orientation 과 같은 약속)
        public FacingSource Source;
    }

    // 자리 계산: 본체 위치·대상 지점·본체 방향·슬롯 번호 → 그림자 자리(지면 높이는 본체 것 — 길 안으로 당기기는 게임 쪽).
    public static class ShadowPlacement
    {
        // 대상이 본체와 이 거리(미터) 안이면 「대상 방향」을 정할 수 없다(자기 중심 주문) → 본체가 보는 방향을 쓴다. 출처: 수치 안정용 하한(0 으로 나누기 방지), 연출 다이얼 아님.
        public static readonly double MinDirectionMeters = 0.05;

        // 자리 수 = 최대 분신 수(ShadowRules.EchoCountLevels 의 3). 시험이 그 표와 대조한다.
        public const int SlotCount = 3;

        // (앞, 오른쪽) 미터. 슬롯은 1부터. 표 밖 = 던진다(조용한 겹침 금지). 세 자리 모두 앞 성분이 음수(본체 뒤 — 2차 독립 검토 중요-1).
        public static void SlotOffset(int slot, float pairBack, float side, float back, out double f, out double r)
        {
            switch (slot)
            {
                case 1: f = -pairBack; r = -side; return;
                case 2: f = -pairBack; r = side; return;
                case 3: f = -back; r = 0; return;
            }
            throw new ArgumentOutOfRangeException("slot", slot, "ShadowSorcerer: shadow slot must be 1..3");
        }

        public static SlotPlace Place(Vec3d caster, Vec3d target, double casterFacingDegrees, int slot)
        {
            return PlaceCore(caster, target, casterFacingDegrees, slot, ShadowVisualDials.SlotPairBackMeters, ShadowVisualDials.SlotSideMeters, ShadowVisualDials.SlotBackMeters);
        }

        public static SlotPlace PlaceCore(Vec3d caster, Vec3d target, double casterFacingDegrees, int slot, float pairBack, float side, float back)
        {
            foreach (var v in new[] { caster.X, caster.Y, caster.Z, target.X, target.Y, target.Z, casterFacingDegrees })
                if (double.IsNaN(v) || double.IsInfinity(v)) throw new ArgumentException("ShadowSorcerer: shadow placement input is not a finite number");
            double f, r;
            SlotOffset(slot, pairBack, side, back, out f, out r);
            double dx = target.X - caster.X, dz = target.Z - caster.Z;
            double len = Math.Sqrt(dx * dx + dz * dz);
            var p = new SlotPlace();
            if (len >= MinDirectionMeters) { dx /= len; dz /= len; p.Source = FacingSource.Target; }
            else
            {
                double rad = casterFacingDegrees * Math.PI / 180.0;
                dx = Math.Sin(rad); dz = Math.Cos(rad);
                p.Source = FacingSource.CasterFacing;
            }
            // 오른쪽 = 앞을 Y축으로 +90° 돌린 방향(왼손 좌표계, 앞 (0,0,1) 의 오른쪽 = (1,0,0)).
            double rx = dz, rz = -dx;
            p.Point = new Vec3d(caster.X + dx * f + rx * r, caster.Y, caster.Z + dz * f + rz * r);
            p.FacingDegrees = Math.Atan2(dx, dz) * 180.0 / Math.PI;
            if (p.FacingDegrees < 0) p.FacingDegrees += 360.0;
            return p;
        }
    }

    // 메아리 실행 범위(H11 이 여닫는다): 「지금 Tick 이 도는 능력 실행 과정이 어느 메아리의 것인가」. 게임 메인 스레드 하나.
    //   - 메아리가 아닌 과정은 범위가 비어 있으면 아무것도 쌓지 않는다(뜨거운 경로 무동작). 메아리 범위 안에서 다른 과정이 돌면 가림 프레임을 쌓는다 —
    //     안쪽 과정의 투사체가 바깥 메아리의 그림자에서 나가지 않게.
    //   - 프레임은 열린 Unity 프레임 번호를 기억한다. Tick 은 한 프레임 안에서 끝나므로 다른 프레임 번호의 프레임 = 닫히지 않고 샌 것 → Stale(손대지 않는다).
    //   - Exit 는 자기 깊이까지 잘라 낸다 — 안쪽이 새도 바깥이 닫힐 때 함께 사라진다. 어긋났으면 던진다(호출자가 보고).
    public struct ScopeToken
    {
        public readonly int Depth;      // 0 = 아무것도 쌓지 않았다
        public readonly object Process;
        public ScopeToken(int depth, object process) { Depth = depth; Process = process; }
    }

    public enum ScopeState { Empty, Masked, Stale, Anchored }

    public sealed class EchoLaunchScope
    {
        struct Frame { public object Process, Anchor; public long Stamp; }
        readonly List<Frame> frames = new List<Frame>();

        public int Depth { get { return frames.Count; } }

        public ScopeToken Enter(object process, object anchorOrNull, long stamp)
        {
            if (process == null) throw new ArgumentNullException("process");
            // 다른 프레임 번호의 프레임이 남아 있다 = 샌 범위. 새 범위를 그 위에 쌓지 않고 먼저 비운다(샌 것이 이후 Tick 을 가리지 않게) — 호출자가 보고하도록 던지지는 않고 LeakedDropped 로 센다.
            if (frames.Count > 0 && frames[frames.Count - 1].Stamp != stamp) { LeakedDropped += frames.Count; frames.Clear(); }
            if (anchorOrNull == null && frames.Count == 0) return new ScopeToken(0, null);
            frames.Add(new Frame { Process = process, Anchor = anchorOrNull, Stamp = stamp });
            return new ScopeToken(frames.Count, process);
        }

        public void Exit(ScopeToken t)
        {
            if (t.Depth == 0) return; // 정당한 무동작: 쌓지 않은 Tick
            bool ok = frames.Count == t.Depth && ReferenceEquals(frames[t.Depth - 1].Process, t.Process);
            int had = frames.Count;
            if (frames.Count >= t.Depth) frames.RemoveRange(t.Depth - 1, frames.Count - (t.Depth - 1));
            if (!ok) throw new InvalidOperationException("ShadowSorcerer: echo launch scope closed out of order (depth " + had + ", token depth " + t.Depth + ")");
        }

        // 샌 프레임을 버린 수(누계).
        public int LeakedDropped { get; private set; }
        int leaksTaken;

        // 지난번 호출 뒤로 새로 버린 샌 프레임 수(0 = 없음). H11 이 매 Tick 읽어 0 이 아니면 보고한다 — 버리기만 하고 아무도 모르는 일이 없게(독립 검토 F7).
        public int TakeNewLeaks()
        {
            int d = LeakedDropped - leaksTaken;
            leaksTaken = LeakedDropped;
            return d;
        }

        public ScopeState Current(long stamp, out object anchor)
        {
            anchor = null;
            if (frames.Count == 0) return ScopeState.Empty;
            var top = frames[frames.Count - 1];
            if (top.Stamp != stamp) return ScopeState.Stale;
            if (top.Anchor == null) return ScopeState.Masked;
            anchor = top.Anchor;
            return ScopeState.Anchored;
        }
    }

    // KeepDelivery = 그 메아리 주문의 전달 형식이 「출발점을 옮겨도 규칙이 같다」고 확정된 목록(EchoLaunchRule.MovableDeliveries)에 없어 엔진 값 그대로(H12 가 센다).
    public enum LaunchChoice { Keep, Move, ReportStale, ReportUnknown, ReportGone, KeepDelivery }

    // 메아리 주문의 전달 형식(그 블루프린트의 AbilityDeliverEffect 컴포넌트들 — EchoVisuals.DeliveryOf 가 읽고 EchoLaunchRule.FoldDelivery 가 접는다).
    //   NoDelivery = 전달 컴포넌트가 없다 · Simple = AbilityDeliverProjectile(Type = Simple) · LineOrCone = AbilityDeliverProjectile(Type ≠ Simple) · Chain = AbilityDeliverChain
    //   · Other = 그 밖의 전달 컴포넌트(튕김·무기·접촉·지연·사용자 정의 등)이거나 형식이 섞였다 · Unknown = 읽을 수 없다(빈 칸·블루프린트 없음).
    public enum DeliveryKind { NoDelivery, Simple, LineOrCone, Chain, Other, Unknown }
    public enum FxAnchorChoice { Keep, Move, ReportGone }

    // 메아리에 묶인 그림자의 지금 상태. Shown = 그 자리에 드러나 있다 · Hidden = 숨겨졌다(흩어진 뒤 — 게임 밖에 있다) 또는 묶음이 바뀌었다 · Gone = 유닛이 사라지는 중이다.
    public enum ShadowUse { Shown, Hidden, Gone }

    public static class EchoLaunchRule
    {
         
        //   근거(Assembly-CSharp 직독 2026-11-17, SHA-256 2CB7160B…EF8953): 출발 좌표 변경은 보이는 것만이 아니다 — ProjectileController.CreateView 가 customLaunchPosition 을
        //   Projectile.set_LaunchPosition 으로 넣고, 그 setter IL_000B 가 set_CorePosition 을 부른다(실제 비행 시작점). Projectile.Tick IL_01C8~01DE 는 Distance(CorePosition, 끝점) < 0.1 로 IsHit →
        //   비행 거리가 바뀌면 「도착 시각」이 바뀐다. 그 시각이 규칙에 쓰이는 곳:
        //   · Chain   AbilityDeliverChain <DeliverInternal>d__15 가 IsHit 을 기다리고, <Deliver>d__14 IL_01CE 가 그 뒤에 SelectNextTargetByMinDistance(그때의 유닛 위치)로 다음 대상을 고른다.
        //   · LineOrCone  StepBasedDeliver 의 마지막 전체 사거리 훑기 시각(2차 검토 중요-4, 아래 Projectile 주석).
        //   · Simple  <Deliver>d__36 은 명중 굴림을 발사 때 하지만(IL_046F~0538), IsHit 뒤에 거울상 소모(IL_05FA ConsumeMirrorImageIfNecessary)·Cleared 확인(IL_0605)을 하고
        //             AbilityDeliveryTarget(projectile.Target) 을 낸다(IL_0610~0643) → 효과(피해·내성 굴림)가 도착 시각에 돈다. 대상은 고정이지만 그 시각의 대상 상태(거울상 수·버프·생사)가 판정에 쓰이고,
        //             다음 메아리의 대상 재확인(EchoQueue.Fire → TargetRule.AtFire — 대상 사망이면 남은 메아리 소멸)이 그 시각보다 앞인지 뒤인지가 바뀌면 메아리 횟수가 바뀐다.
        //   · 그 밖(Other·NoDelivery·Unknown): 확정하지 않았다 → 기본값 = 옮기지 않는다.
         
        public static readonly IReadOnlyList<DeliveryKind> MovableDeliveries = new DeliveryKind[0];

        // 투사체 출발점(H12). Move 는 아래가 모두 성립할 때만:
        //   범위가 이 프레임의 메아리 과정을 가리킨다 · 엔진이 출발 좌표를 따로 정하지 않았다 · 발사자가 유닛이다 · 그 유닛이 그 메아리의 본체다
        //   · 그 메아리 주문의 전달 형식이 movable 에 있다(생산 = MovableDeliveries — 비어 있다) · 투사체가 지형을 따라가지 않는다 · 그 메아리의 그림자가 지금 드러나 있다.
        // launcherIsAnchorCaster: null = 발사자가 유닛이 아니다(지점 발사 — 본편 조종 투사체 등) → 손대지 않는다.
        // delivery: Unknown = 전달 형식을 읽을 수 없다 → ReportUnknown(호출자가 한 번 보고, 엔진 값 그대로 — 「옮겨도 된다」로 넘기지 않는다). 목록 밖 = KeepDelivery(엔진 값 그대로, 센다).
        //   (원뿔·선: StepBasedDeliver d__38 IL_014F~026F 는 매 틱 PassedDistance(= Σ dt × Speed — 출발점 무관)로 유닛을 훑고 IsHit 이면 끝나는데(IL_028C), IsHit 시각은 출발점에 달려 있다
        //    → 마지막 전체 사거리 훑기(PassedDistance = MaxRange, Tick IL_01FB~020F)의 시각이 바뀌어 그사이 움직인 유닛의 포함 여부가 바뀔 수 있다 — 2차 독립 검토 중요-4.)
        // followsTerrain: 투사체 블루프린트의 FollowTerrain. true 면 손대지 않는다 — 엔진 Projectile.Tick IL_0146~0162 가 현재 위치(= 출발 좌표에서 시작)로
        //   UnitMovementAgentBase.Move(벽 충돌 판정)를 부르므로 출발 좌표가 「어디서 막히는가」에 들어간다(독립 검토 F9). null = 블루프린트를 읽을 수 없다 → ReportUnknown.
        // shadow: Hidden 이면 손대지 않는다(독립 검토 F6: 흩어진 뒤 늦게 나가는 투사체가 숨은 그림자에서 나가지 않게). Gone = ReportGone(호출자가 한 번 보고, 엔진 값 그대로).
        public static LaunchChoice Projectile(ScopeState scope, bool engineSetPosition, bool? launcherIsAnchorCaster, bool? followsTerrain, DeliveryKind delivery, ShadowUse shadow)
        {
            return ProjectileCore(scope, engineSetPosition, launcherIsAnchorCaster, followsTerrain, delivery, shadow, MovableDeliveries);
        }

        public static LaunchChoice ProjectileCore(ScopeState scope, bool engineSetPosition, bool? launcherIsAnchorCaster, bool? followsTerrain, DeliveryKind delivery, ShadowUse shadow, IReadOnlyList<DeliveryKind> movable)
        {
            if (movable == null) throw new ArgumentNullException("movable");
            if (scope == ScopeState.Stale) return LaunchChoice.ReportStale;
            if (scope != ScopeState.Anchored) return LaunchChoice.Keep;
            if (engineSetPosition) return LaunchChoice.Keep;
            if (!launcherIsAnchorCaster.HasValue || !launcherIsAnchorCaster.Value) return LaunchChoice.Keep;
            if (delivery == DeliveryKind.Unknown) return LaunchChoice.ReportUnknown;
            if (!movable.Contains(delivery)) return LaunchChoice.KeepDelivery;
            if (!followsTerrain.HasValue) return LaunchChoice.ReportUnknown;
            if (followsTerrain.Value) return LaunchChoice.Keep;
            if (shadow == ShadowUse.Gone) return LaunchChoice.ReportGone;
            if (shadow != ShadowUse.Shown) return LaunchChoice.Keep;
            return LaunchChoice.Move;
        }

        // 주문의 컴포넌트들 → 전달 형식. parts[k] = 컴포넌트 k 의 형식 · null = 전달 컴포넌트가 아니다(세지 않는다) · Unknown = 그 칸을 읽을 수 없다.
        //   하나라도 Unknown = Unknown(판독 불가를 「아님」으로 넘기지 않는다) · 전달 칸이 없다 = NoDelivery · 모두 같은 형식 = 그 형식 · 섞였다 = Other. parts 자체가 null = Unknown.
        public static DeliveryKind FoldDelivery(DeliveryKind?[] parts)
        {
            if (parts == null) return DeliveryKind.Unknown;
            DeliveryKind? seen = null;
            bool mixed = false;
            foreach (var p in parts)
            {
                if (!p.HasValue) continue;
                if (p.Value == DeliveryKind.Unknown) return DeliveryKind.Unknown;
                if (p.Value == DeliveryKind.NoDelivery) throw new ArgumentException("ShadowSorcerer: a component part cannot be NoDelivery (use null for a non-delivery component)");
                if (seen.HasValue && seen.Value != p.Value) mixed = true;
                seen = p.Value;
            }
            return !seen.HasValue ? DeliveryKind.NoDelivery : mixed ? DeliveryKind.Other : seen.Value;
        }

        // 주문의 전달 컴포넌트들 → 원뿔·선인가. parts[k] = 컴포넌트 k 가 원뿔·선 투사체 전달이다(true) / 아니다(false) / 읽을 수 없다(null — 빈 칸·판독 실패).
        //   하나라도 true = true(원뿔·선이 있다 — 판독 불가 칸이 있어도 결론은 같다: 옮기지 않는다) · 그다음 하나라도 null = null · 모두 false = false. parts 자체가 null = null.
        public static bool? AnyLineOrCone(bool?[] parts)
        {
            if (parts == null) return null;
            bool unknown = false;
            foreach (var p in parts)
            {
                if (p == true) return true;
                if (!p.HasValue) unknown = true;
            }
            return unknown ? (bool?)null : false;
        }

        // 주문 FX 앵커(H13). 컨텍스트 자체가 열쇠다(범위 불필요): 앵커 = 시전자 · 엔진이 낸 결과가 있음 · 그 컨텍스트에 메아리 연출이 묶여 있음 · 그 그림자가 드러나 있음.
        // shadow 는 연출이 묶여 있을 때만 뜻이 있다(묶이지 않았으면 읽지 않는다). Hidden = 손대지 않는다(F6), Gone = ReportGone.
        public static FxAnchorChoice FxAnchor(bool anchorIsCaster, bool engineResolved, bool contextHasEchoAnchor, ShadowUse shadow)
        {
            if (!anchorIsCaster || !engineResolved || !contextHasEchoAnchor) return FxAnchorChoice.Keep;
            if (shadow == ShadowUse.Gone) return FxAnchorChoice.ReportGone;
            return shadow == ShadowUse.Shown ? FxAnchorChoice.Move : FxAnchorChoice.Keep;
        }
    }
}
