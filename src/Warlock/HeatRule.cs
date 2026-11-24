using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock
{
    // 자동 소환 예약 상태의 관측(T4 UnitPartWarlockAuto.Obs(now)). 나이는 해당 깃발이 true 일 때만 읽는다.
    public struct AutoObs
    {
        public bool Pending; // 살아 있는 예약(Reserved != null)
        public float PendingAgeSeconds; // now − ReservedAt
        public bool ApprovedOpen; // 승인된 실행이 소환 사건(RuleSummonUnit)을 아직 내지 않음
        public float ApprovedAgeSeconds; // now − ApprovedAt
    }

    public enum HeatCmdKind { AddRanks, Reserve, HoldAtMax, HoldPending, TimeoutPending, ApprovedTimedOut }

    // N: AddRanks = 더할 랭크 · Reserve = 소환 단계(maxTier) · HoldAtMax / HoldPending = 써 둘 랭크(HeatMax) · TimeoutPending / ApprovedTimedOut = 0.
    public struct HeatCmd
    {
        public readonly HeatCmdKind Kind;
        public readonly int N;
        public HeatCmd(HeatCmdKind kind, int n) { Kind = kind; N = n; }
        public override string ToString() { return Kind + ":" + N; }
    }

    // 굴림 한 건의 분류(T4 WarlockHeatComponent.OnEventDidTrigger): 미리보기 굴림 → 블라스트 표식 없음(무기·주문·이유 없음) → 빗나감 → 명중(피해 0·SR 저항도 명중 — 설계 §4.7 Q7=(A)).
    public enum HitVerdict { Fake, NotBlast, Miss, Hit }

    // 명령 적용 효과(T4 WarlockHeatComponent.ApplyEffects 가 순서대로 실행). WriteRank N = 그 랭크를 쓴다(WarlockHeat.Write) · ObsoleteReservation = Reserved 를 Obsolete 로 옮기고 비운다 ·
    // CloseApproved = ApprovedOpen 을 비운다 · Error/Debug = 로그(OnceKey null = 매번) · Reserve N = 자동 소환 예약(T5 AutoSummon.Reserve; T4 는 도달 불가 → throw).
    // ReleaseReservation = 이 예약(방금 만든 실행)을 비운다(T5 AutoSummon — 즉시 시전 실패).
    public enum HeatEffectKind { WriteRank, ObsoleteReservation, CloseApproved, Error, Debug, Reserve, ReleaseReservation }

    public struct HeatEffect
    {
        public readonly HeatEffectKind Kind;
        public readonly int N;
        public readonly string OnceKey;
        public readonly string Text;
        public HeatEffect(HeatEffectKind kind, int n, string onceKey, string text) { Kind = kind; N = n; OnceKey = onceKey; Text = text; }
    }

     
    // Settled = 전투 중이었고 이 호출의 정산(절반)을 이미 했다(같은 호출에 사건이 또 와도 다시 하지 않는다).
    public enum LeaveState { InCombat, NotInCombat, Settled }

    // 이탈 사건 판정: Settle = Rank 를 쓴다(절반) · NotInCombat / AlreadySettled = 쓰지 않는다 · Unknown = 캡처 없음(판정 불가) → 쓰지 않고 오류.
    public enum LeaveKind { Settle, NotInCombat, AlreadySettled, Unknown }

    public struct LeaveVerdict
    {
        public readonly LeaveKind Kind;
        public readonly int Rank; // Settle = 쓸 랭크 · 그 밖 = 들어온 랭크 그대로(쓰지 않는다)
        public LeaveVerdict(LeaveKind kind, int rank) { Kind = kind; Rank = rank; }
        public override string ToString() { return Kind + ":" + Rank; }
    }

    // [순수] 유닛 하나의 이탈 캡처 더미. 패치(LeaveCombatCapturePatch)의 Prefix 가 Push, Finalizer 가 Pop — 이탈 호출이 겹치면(사건 처리 중 다시 LeaveCombat) 안쪽 호출이 위에 쌓인다.
    // 비어 있음 = 캡처 없음(Top() == null). 짝이 안 맞는 Pop / MarkSettled 는 던진다.
    public sealed class LeaveCaptures
    {
        readonly List<LeaveState> items = new List<LeaveState>();





        public void Push(bool wasInCombat) { items.Add(wasInCombat ? LeaveState.InCombat : LeaveState.NotInCombat); }

        public LeaveState? Top() { return items.Count == 0 ? (LeaveState?)null : items[items.Count - 1]; }

        public void MarkSettled()
        {
            if (items.Count == 0 || items[items.Count - 1] != LeaveState.InCombat) throw new InvalidOperationException("Warlock: MarkSettled needs an InCombat capture on top (got " + (items.Count == 0 ? "none" : items[items.Count - 1].ToString()) + ")");
            items[items.Count - 1] = LeaveState.Settled;
        }

        public void Pop()
        {
            if (items.Count == 0) throw new InvalidOperationException("Warlock: leave capture Pop without a Push");
            items.RemoveAt(items.Count - 1);
        }
    }

     
    public static class HeatRule
    {
        // 버프에 저장된 랭크의 유효성(리뷰 T4 m-2): 과열 버프가 있으면 랭크는 1..HeatMax — 0 은 RemoveRank 류가 남긴 「0 랭크 버프」(계획 판정), 초과는 상한 무시. 둘 다 손상 → 던진다.
        public static int RequireStoredRank(int r)
        {
            if (r < 1 || r > WarlockRules.Dials.HeatMax) throw new InvalidOperationException("Warlock: heat buff has rank " + r + " (expected 1.." + WarlockRules.Dials.HeatMax + ")");
            return r;
        }

        // 전투 이탈 사건이 이 컴포넌트 주인의 것인가(IUnitCombatHandler 는 전역 — 적이 죽어도 그 적의 LeaveCombat 이 온다: UnitLifeController.SetLifeState @00A5).
        // 주인이 없으면 판정 불가 → 던진다. 사건 유닛이 없으면 판정 불가 → 던진다(호출자가 세고 ERROR — 조용히 0 을 쓰지도, 무시하지도 않는다).
        public static bool IsOwnerEvent(object unit, object owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            if (unit == null) throw new ArgumentNullException("unit", "Warlock: leave-combat event without a unit");
            return ReferenceEquals(unit, owner);
        }

        // 전투가 끝난 뒤의 과열(소유자 결정 2026-11-16): 절반, 내림. rank 는 0..HeatMax(범위 밖 = 던진다 — 기본값으로 진행하지 않는다).
        // 0 이상 정수의 C# 나눗셈은 0 방향 절삭 = 내림(7 → 3 · 5 → 2 · 1 → 0 · 9 → 4).
        public static int AfterCombat(int rank)
        {
            RequireRank(rank);
            return rank / WarlockRules.Dials.HeatCombatEndDivisor;
        }

        // 휴식을 마친 뒤의 과열(설계 §6-6, 소유자 결정 2026-11-16 에서도 그대로): 항상 Dials.HeatAfterRest(0). 범위 밖 rank 는 던진다.
        public static int AfterRest(int rank)
        {
            RequireRank(rank);
            return WarlockRules.Dials.HeatAfterRest;
        }

         
        // 엔진은 전투 중이 아니어도 이탈 사건을 낸다(@00AD~00B9) → 호출 직전 전투 중이었던 이탈에만 절반(Settle).
        // null(캡처 없음 = 판정 불가)은 「전투 중이었음」으로도 「아니었음」으로도 읽지 않는다 → Unknown(호출자: 과열 그대로 + 오류 1회).
        // 범위 밖 rank 는 어느 캡처에서도 던진다.
        public static LeaveVerdict OnLeaveCombat(int rank, LeaveState? capture)
        {
            RequireRank(rank);
            if (!capture.HasValue) return new LeaveVerdict(LeaveKind.Unknown, rank);
            switch (capture.Value)
            {
                case LeaveState.InCombat: return new LeaveVerdict(LeaveKind.Settle, AfterCombat(rank));
                case LeaveState.NotInCombat: return new LeaveVerdict(LeaveKind.NotInCombat, rank);
                case LeaveState.Settled: return new LeaveVerdict(LeaveKind.AlreadySettled, rank);
                default: throw new ArgumentOutOfRangeException("capture", capture.Value, "Warlock: unknown leave capture");
            }
        }

        // 계수 대상 판정. marker = 굴림 이유 능력의 WarlockBlastMarker.Kind(없으면 null — 이유·능력이 없는 굴림 포함).
        public static HitVerdict Observe(bool isFake, BlastKind? marker, bool isHit)
        {
            if (isFake) return HitVerdict.Fake;
            if (!marker.HasValue) return HitVerdict.NotBlast;
            return isHit ? HitVerdict.Hit : HitVerdict.Miss;
        }

        // 굴림 보정: 연속 사격(Double 표식) 줄기마다 Dials.DoublePenalty(설계 §4.5), 그 밖 0.
        public static int AttackPenalty(BlastKind? marker)
        {
            return marker == BlastKind.Double ? WarlockRules.Dials.DoublePenalty : 0;
        }

        static bool IsTerminal(HeatCmdKind k) { return k == HeatCmdKind.AddRanks || k == HeatCmdKind.Reserve || k == HeatCmdKind.HoldAtMax || k == HeatCmdKind.HoldPending; }

        // OnBlastHit 의 명령 배열 → 효과. 배열 모양(0~2 만료 명령이 이 순서로, 그다음 끝 명령 정확히 1)이 어긋나면 던진다. rank = 명중 전 랭크.
        public static HeatEffect[] Apply(int rank, HeatCmd[] cmds, string unit)
        {
            RequireRank(rank);
            if (cmds == null) throw new ArgumentNullException("cmds");
            if (string.IsNullOrEmpty(unit)) throw new ArgumentException("Warlock: HeatRule.Apply needs a unit key");
            if (cmds.Length == 0 || !IsTerminal(cmds[cmds.Length - 1].Kind) || cmds.Take(cmds.Length - 1).Any(c => IsTerminal(c.Kind)))
                throw new ArgumentException("Warlock: heat commands must end with exactly one AddRanks/Reserve/HoldAtMax/HoldPending (got [" + string.Join(",", cmds.Select(c => c.ToString()).ToArray()) + "])");
            var pre = cmds.Take(cmds.Length - 1).Select(c => c.Kind).ToArray();
            if (pre.Distinct().Count() != pre.Length || (pre.Length == 2 && (pre[0] != HeatCmdKind.TimeoutPending || pre[1] != HeatCmdKind.ApprovedTimedOut)))
                throw new ArgumentException("Warlock: heat expiry commands must be at most TimeoutPending then ApprovedTimedOut (got [" + string.Join(",", pre.Select(k => k.ToString()).ToArray()) + "])");
            var fx = new List<HeatEffect>();
            foreach (var c in cmds)
            {
                switch (c.Kind)
                {
                    case HeatCmdKind.TimeoutPending:
                        fx.Add(new HeatEffect(HeatEffectKind.ObsoleteReservation, 0, null, null));
                        fx.Add(new HeatEffect(HeatEffectKind.Error, 0, null, "Warlock: " + unit + " auto-summon reservation older than " + WarlockRules.Dials.AutoPendingTimeoutSeconds + " s; the old execution is marked obsolete (TimeoutPending)"));
                        break;
                    case HeatCmdKind.ApprovedTimedOut:
                        fx.Add(new HeatEffect(HeatEffectKind.CloseApproved, 0, null, null));
                        fx.Add(new HeatEffect(HeatEffectKind.Error, 0, null, "Warlock: " + unit + " approved auto-summon produced no RuleSummonUnit within " + WarlockRules.Dials.AutoPendingTimeoutSeconds + " s (ApprovedTimedOut)"));
                        break;
                    case HeatCmdKind.AddRanks:
                        if (c.N < 1) throw new ArgumentOutOfRangeException("cmds", c.N, "Warlock: AddRanks needs N >= 1");
                        if (rank + c.N >= WarlockRules.Dials.HeatMax) throw new InvalidOperationException("Warlock: AddRanks " + c.N + " from rank " + rank + " reaches " + WarlockRules.Dials.HeatMax + " (OnBlastHit returns a hold/reserve there)");
                        fx.Add(new HeatEffect(HeatEffectKind.WriteRank, rank + c.N, null, null));
                        break;
                    case HeatCmdKind.HoldAtMax:
                        if (c.N != WarlockRules.Dials.HeatMax) throw new ArgumentException("Warlock: HoldAtMax must hold " + WarlockRules.Dials.HeatMax + " (got " + c.N + ")");
                        fx.Add(new HeatEffect(HeatEffectKind.WriteRank, c.N, null, null));
                        fx.Add(new HeatEffect(HeatEffectKind.Error, 0, unit + "|HoldAtMax", "Warlock: " + unit + " heat held at " + c.N + ": the max summon tier is undeterminable (spellbook / max spell level unknown) — no auto summon (HoldAtMax)"));
                        break;
                    case HeatCmdKind.HoldPending:
                        if (c.N != WarlockRules.Dials.HeatMax) throw new ArgumentException("Warlock: HoldPending must hold " + WarlockRules.Dials.HeatMax + " (got " + c.N + ")");
                        fx.Add(new HeatEffect(HeatEffectKind.WriteRank, c.N, null, null));
                        fx.Add(new HeatEffect(HeatEffectKind.Debug, 0, null, "Warlock: " + unit + " heat held at " + c.N + " while an auto-summon reservation is pending (HoldPending)"));
                        break;
                    case HeatCmdKind.Reserve:
                        if (c.N < 1 || c.N > WarlockRules.Dials.SummonTiers) throw new ArgumentOutOfRangeException("cmds", c.N, "Warlock: Reserve tier must be 1.." + WarlockRules.Dials.SummonTiers);
                        fx.Add(new HeatEffect(HeatEffectKind.Reserve, c.N, null, null));
                        break;
                    default: throw new ArgumentOutOfRangeException("cmds", c.Kind, "Warlock: unknown heat command");
                }
            }
            return fx.ToArray();
        }

        static void RequireRank(int rank)
        {
            if (rank < 0 || rank > WarlockRules.Dials.HeatMax) throw new ArgumentOutOfRangeException("rank", rank, "Warlock: heat rank must be 0.." + WarlockRules.Dials.HeatMax);
        }

        static void RequireAge(bool flag, float age, string what)
        {
            if (flag && (float.IsNaN(age) || float.IsInfinity(age) || age < 0f)) throw new ArgumentOutOfRangeException(what, age, "Warlock: " + what + " must be a finite age >= 0");
        }

        // 반환 = 실행 순서대로 0~2개의 만료 명령(TimeoutPending, ApprovedTimedOut) + 정확히 1개의 AddRanks / Reserve / HoldAtMax / HoldPending (진행 원장 판정).
        // maxTier = min(9, 최고 주문 레벨)(WarlockRules.MaxTierAt) 또는 null = 판정 불가.
        public static HeatCmd[] OnBlastHit(int rank, bool confirmedCrit, bool hasAccel, int? maxTier, AutoObs auto)
        {
            RequireRank(rank);
            if (maxTier.HasValue && (maxTier.Value < 1 || maxTier.Value > WarlockRules.Dials.SummonTiers))
                throw new ArgumentOutOfRangeException("maxTier", maxTier, "Warlock: maxTier must be 1.." + WarlockRules.Dials.SummonTiers + " or null (undeterminable)");
            RequireAge(auto.Pending, auto.PendingAgeSeconds, "PendingAgeSeconds");
            RequireAge(auto.ApprovedOpen, auto.ApprovedAgeSeconds, "ApprovedAgeSeconds");

            var cmds = new List<HeatCmd>();
            bool pending = auto.Pending;
            // 전처리: 시한을 넘긴 예약은 폐기(게임 쪽: Obsolete 등록 + ERROR) 뒤 같은 호출에서 Idle 로 재판정한다.
            if (pending && auto.PendingAgeSeconds > WarlockRules.Dials.AutoPendingTimeoutSeconds)
            {
                cmds.Add(new HeatCmd(HeatCmdKind.TimeoutPending, 0));
                pending = false;
            }
            // 승인된 실행이 시한까지 소환 사건을 내지 않음 → ERROR 1회(게임 쪽이 승인 기록을 닫는다), 판정은 계속.
            if (auto.ApprovedOpen && auto.ApprovedAgeSeconds > WarlockRules.Dials.AutoPendingTimeoutSeconds)
                cmds.Add(new HeatCmd(HeatCmdKind.ApprovedTimedOut, 0));

            int gain = confirmedCrit && hasAccel ? WarlockRules.Dials.HeatOnConfirmedCrit : WarlockRules.Dials.HeatOnHit;
            if (rank + gain < WarlockRules.Dials.HeatMax) cmds.Add(new HeatCmd(HeatCmdKind.AddRanks, gain));
            // 임계: 판정 불가 → 9 유지 + ERROR · 예약 대기 중 → 9 유지(예약을 덮지 않는다) · 아니면 최고 단계 예약(초과분 버림 — 「설계와 달리 정한 것」 ②).
            else if (!maxTier.HasValue) cmds.Add(new HeatCmd(HeatCmdKind.HoldAtMax, WarlockRules.Dials.HeatMax));
            else if (pending) cmds.Add(new HeatCmd(HeatCmdKind.HoldPending, WarlockRules.Dials.HeatMax));
            else cmds.Add(new HeatCmd(HeatCmdKind.Reserve, maxTier.Value));
            return cmds.ToArray();
        }

        // 자동 소환 즉시 시전 결과(T5 AutoSummon.Reserve 가 Rulebook.Trigger(RuleCastSpell) 뒤에 따른다 — 계획 Global Constraints 「즉시 시전 실패 = 예약 해제 + 9 복원 + ERROR」, 진행 원장 O2/M28).
        // 성공 = 효과 없음(완료는 변형의 차감 액션이 예약을 소비할 때). 실패 = [ReleaseReservation, WriteRank 9, Error] — 다음 명중이 다시 예약한다(exec: auto sequences (5)).
        public static HeatEffect[] AfterReserveCast(bool success, int tier, string unit)
        {
            if (tier < 1 || tier > WarlockRules.Dials.SummonTiers) throw new ArgumentOutOfRangeException("tier", tier, "Warlock: reserved tier must be 1.." + WarlockRules.Dials.SummonTiers);
            if (string.IsNullOrEmpty(unit)) throw new ArgumentException("Warlock: AfterReserveCast needs a unit key");
            if (success) return new HeatEffect[0];
            return new[]
            {
                new HeatEffect(HeatEffectKind.ReleaseReservation, 0, null, null),
                new HeatEffect(HeatEffectKind.WriteRank, WarlockRules.Dials.HeatMax, null, null),
                new HeatEffect(HeatEffectKind.Error, 0, null, "Warlock: " + unit + " auto-summon cast of tier " + tier + " failed immediately (RuleCastSpell.Success false); reservation released, heat restored to " + WarlockRules.Dials.HeatMax),
            };
        }

        // 차감 뒤 랭크. 예약 소비는 랭크 불변(예약 때 이미 0 을 썼고, 그 뒤 명중분은 보존 — 계획 sequence (2)).
        public static int Spend(int rank, int cost, SpendKind kind)
        {
            RequireRank(rank);
            if (cost < 1 || cost > WarlockRules.Dials.SummonTiers) throw new ArgumentOutOfRangeException("cost", cost, "Warlock: heat cost must be 1.." + WarlockRules.Dials.SummonTiers);
            switch (kind)
            {
                case SpendKind.MatchingReservation: return rank;
                case SpendKind.ObsoleteReservation: return rank;
                case SpendKind.KnownManual:
                    if (rank < cost) throw new InvalidOperationException("Warlock: manual heat spend " + cost + " with rank " + rank + " (restriction WarlockHeatCost should have blocked it)");
                    return rank - cost;
                case SpendKind.MissingContext: throw new InvalidOperationException("Warlock: heat spend without ability context / auto part (MissingContext)");
                default: throw new ArgumentOutOfRangeException("kind", kind, "Warlock: unknown spend kind");
            }
        }
    }
}
