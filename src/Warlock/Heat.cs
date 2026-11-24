using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Newtonsoft.Json;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Warlock
{
     
    // 선례: 본편 UnrelentingAssaultBuff 4beb8f41…(Stacking Rank, Ranks 10 — jbp 시험), 생성 모양 PhantomBlade StandBlueprints.cs:67-73.
    public static class Heat
    {
        public static BlueprintBuff Buff;

        public static void Build()
        {
            if (Buff != null) throw new InvalidOperationException("Warlock: Heat.Build() called twice");
            // 아이콘 = 본편 ScorchingRay(작성자 결정 — 설계는 아이콘을 정하지 않는다, O3-4 와 같은 눈 점검 대상).
            var icon = KitBuild.Icon(Guids.Game.ScorchingRay);
            var b = Create<BlueprintBuff>("WarlockHeatBuff", Guids.HeatBuff, x =>
            {
                KitBuild.Names(x, "Heat", icon);
                x.IsClassFeature = true;
                x.Stacking = StackingType.Rank;
                x.Ranks = WarlockRules.Dials.HeatMax;
                // 2026-11-17(GameLogFull: WarlockHeat.Write → 과열 버프 제거 → Buff.OnRemove @00B0 NRE 62건): 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말).
                EmptyFxOnRemove(x);
            });
            if (b.Ranks != WarlockRules.Dials.HeatMax || b.Stacking != StackingType.Rank || !b.IsClassFeature || b.ComponentsArray == null || b.ComponentsArray.Length != 0)
                throw new InvalidOperationException("Warlock: WarlockHeatBuff readback failed (Ranks " + b.Ranks + ", Stacking " + b.Stacking + ", components " + (b.ComponentsArray == null ? "null" : b.ComponentsArray.Length.ToString()) + ")");
            Buff = b;
        }
    }

    // 과열 상태 API(C4 ⑥ — 랭크를 읽고 쓰는 곳은 여기뿐). 과열 0 = 버프 없음(설계 §4.7). AddRank/RemoveRank 는 쓰지 않는다(AddRank 는 상한을 무시, RemoveRank 는 랭크 0 버프를 남김 — 계획 판정).
    public static class WarlockHeat
    {
        static Buff[] Buffs(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            if (Heat.Buff == null) throw new InvalidOperationException("Warlock: heat read before Heat.Build()");
            return u.Buffs.Enumerable.Where(b => ReferenceEquals(b.Blueprint, Heat.Buff)).ToArray();
        }

        public static int Rank(UnitEntityData u)
        {
            var bs = Buffs(u);
            if (bs.Length == 0) return 0; // 과열 0 의 정의(버프 없음) — 결측이 아니다
            if (bs.Length > 1) throw new InvalidOperationException("Warlock: " + u.CharacterName + " carries " + bs.Length + " heat buffs");
            return HeatRule.RequireStoredRank(bs[0].Rank); // 0·>9 = 손상 → 던진다(리뷰 T4 m-2)
        }

        public static void Write(UnitEntityData u, int rank)
        {
            if (rank < 0 || rank > WarlockRules.Dials.HeatMax) throw new ArgumentOutOfRangeException("rank", rank, "Warlock: heat rank must be 0.." + WarlockRules.Dials.HeatMax);
            var bs = Buffs(u);
            u.Ensure<UnitPartWarlockAuto>(); // 리뷰 T5 C-1: 명중 밖에서 과열을 쓰는 경로(점검 주입·휴식·전투 종료)도 부품을 갖게 한다(멱등)
            if (bs.Length > 1) throw new InvalidOperationException("Warlock: " + u.CharacterName + " carries " + bs.Length + " heat buffs");
            var buff = bs.Length == 1 ? bs[0] : null;
            if (rank == 0)
            {
                if (buff != null) buff.Remove(); // 버프 없음 + rank 0 = 이미 0(정당한 무동작)
            }
            else
            {
                if (buff == null)
                {
                    buff = u.Descriptor.AddBuff(Heat.Buff, u, null, null);
                    if (buff == null) throw new InvalidOperationException("Warlock: AddBuff(WarlockHeatBuff) returned null for " + u.CharacterName);
                }
                buff.SetRank(rank, false);
            }
            int back = Rank(u);
            if (back != rank) throw new InvalidOperationException("Warlock: heat readback for " + u.CharacterName + ": wrote " + rank + ", read " + back);
        }
    }

    // 게임 시각(예약 나이). PackWarden PackCall.cs:180-186 과 같은 경로·실패.
    public static class HeatClock
    {
        public static TimeSpan Now()
        {
            var game = Game.Instance;
            if (game == null) throw new InvalidOperationException("Warlock: Game.Instance is null");
            if (game.TimeController == null) throw new InvalidOperationException("Warlock: Game.Instance.TimeController is null");
            return game.TimeController.GameTime;
        }
    }

    // Engine execution queues are transient. Persist the right to a refund until a creature actually spawns.
    public class UnitPartWarlockAuto : UnitPart
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal bool RefundDue;
        internal RuleCastSpell Cast;
        internal bool Submitting;
        bool refunding;
        public AbilityData Reserved;          // 살아 있는 예약(덮어쓰지 않는다 — 시한 초과면 Obsolete 로)
        public int ReservedTier;
        public TimeSpan ReservedAt;
        public List<AbilityData> Obsolete = new List<AbilityData>(); // 폐기된 실행 — 그 차감 액션은 ObsoleteReservation
        public AbilityData ApprovedOpen;      // 승인됐으나 아직 RuleSummonUnit 이 안 온 실행
        public TimeSpan ApprovedAt;
        public SpendDecision LastDecision;    // T5: 마지막 차감 액션의 판정(게이트 ConditionWarlockApproved 가 읽는다 — 같은 실행 문맥·능력일 때만 승인)
         
        // 전투 이탈 처리기가 맨 위를 읽는다. 호출이 끝나면 비어 있다 → 저장할 것이 없다(런타임 전용).
        public readonly LeaveCaptures LeaveCaptures = new LeaveCaptures();

        internal bool Owes(AbilityData ability)
        {
            return RefundDue && Cast != null && ReferenceEquals(Cast.Spell, ability);
        }

        internal void Spawned(AbilityData ability)
        {
            if (!Owes(ability)) return;
            RefundDue = false;
            Cast = null;
        }

        internal void Refund()
        {
            if (!RefundDue || refunding) return;
            refunding = true;
            try
            {
                if (Cast != null && ReferenceEquals(ApprovedOpen, Cast.Spell)) ApprovedOpen = null;
                if (Cast != null && LastDecision != null && ReferenceEquals(LastDecision.Ability, Cast.Spell)) LastDecision = null;
                Reserved = null;
                Cast = null;
                WarlockHeat.Write(Owner, WarlockRules.Dials.HeatMax);
                RefundDue = false; // A failed buff write remains retryable. Writing 9 is idempotent.
            }
            finally { refunding = false; }
        }

        internal void RecoverIfAbandoned()
        {
            if (!RefundDue || Submitting || refunding) return;
            // CancelAbilityExecution nulls Result. Stop() drops the queue without ending processes.
            if (Cast != null && Cast.Result != null && !Cast.Result.IsEnded)
            {
                var age = (HeatClock.Now() - ReservedAt).TotalSeconds;
                if (age >= 0 && age <= WarlockRules.Dials.AutoPendingTimeoutSeconds) return;
            }
            Refund();
        }

        protected override void OnPostLoad()
        {
            base.OnPostLoad();
            if (!RefundDue || Cast != null || !Owner.PreviewOf.IsEmpty) return;
            AutoSummon.Watch(this);
            Refund();
        }

        public AutoObs Obs(TimeSpan now)
        {
            var o = new AutoObs { Pending = RefundDue || Reserved != null, ApprovedOpen = ApprovedOpen != null };
            if (o.Pending) o.PendingAgeSeconds = (float)(now - ReservedAt).TotalSeconds;
            if (o.ApprovedOpen) o.ApprovedAgeSeconds = (float)(now - ApprovedAt).TotalSeconds;
            return o; // 나이는 깃발이 참일 때만 의미가 있다(HeatRule.OnBlastHit 가 그때만 읽고, 음수·NaN 이면 던진다)
        }
    }

    // 차감 액션의 판정 한 건(T5). Context = 그 실행의 MechanicsContext(같은 AbilityData 를 다시 쓰는 수동 시전과 구별 — 정찰 §16-T5 1).
    public sealed class SpendDecision
    {
        public readonly Kingmaker.UnitLogic.Mechanics.MechanicsContext Context;
        public readonly AbilityData Ability;
        public readonly bool Approved;
        public readonly SpendKind Kind; // 리뷰 T5 m-4: 소환 확인이 되돌릴 과열(수동 = 단계, 예약 = 9)을 정한다
        public readonly int Tier;
        public SpendDecision(Kingmaker.UnitLogic.Mechanics.MechanicsContext context, AbilityData ability, bool approved, SpendKind kind, int tier) { Context = context; Ability = ability; Approved = approved; Kind = kind; Tier = tier; }
    }

    // 과열 로그 1회 집합과 콜백 실패 수(PactLog 와 같은 계약). 엔진은 전역·개시자 규칙 구독자의 예외를 삼킨다(RulebookSubscribersList.OnEventDidTrigger catch 002C~003A, 정찰 §16-T4)
    // → 처리기 안에서 잡아서 세고 ERROR 로 남긴다. S6·S9a 단계가 Failures == 0 을, S15 no-mod-errors 가 ERROR 줄을 단언한다.
    public static class HeatLog
    {
        static readonly OnceSet once = new OnceSet();
        public static int Failures { get; private set; }

        internal static void Emit(bool error, string onceKey, string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            if (onceKey != null && !once.Add(PactLog.Generation(), onceKey)) return; // 정당한 무동작: 같은 키는 이 로드에서 1회만(UnknownOnce, 세대 = PactLog.Generation)
            if (error) ModContext.LogError(text); else ModContext.Log("DEBUG " + text);
        }

        // UI 가 매 프레임 부르는 곳(사용 제한): 실패는 매번 세고 ERROR 는 키마다 이 로드에서 1회.
        internal static void Failed(string where, UnitEntityData u, Exception ex, string onceKey)
        {
            if (onceKey == null) throw new ArgumentNullException("onceKey");
            Failures++;
            if (!once.Add(PactLog.Generation(), onceKey)) return; // 정당한 무동작: 같은 실패는 이 로드에서 이미 ERROR 로 남았다(수는 셈)
            ModContext.LogError("Warlock: heat " + where + " failed for " + (u == null ? "(no unit)" : u.CharacterName + " (" + u.UniqueId + ")") + ": " + ex);
        }

        internal static void Failed(string where, UnitEntityData u, Exception ex)
        {
            Failures++;
            ModContext.LogError("Warlock: heat " + where + " failed for " + (u == null ? "(no unit)" : u.CharacterName + " (" + u.UniqueId + ")") + ": " + ex);
        }
    }

     
    // 대상 UnitEntityData.LeaveCombat 은 모든 유닛의 이탈에서 돈다: 과열 부품(UnitPartWarlockAuto)이 없는 유닛에는 부품 조회 한 번뿐이고 아무것도 쓰지 않는다.
    // Prefix = 호출 직전 IsInCombat(= CombatState.m_InCombat — 원래 메서드가 곧바로 false 로 만든다)을 그 유닛의 더미에 쌓는다. void → 원래 메서드를 건너뛰지 않는다.
    // Finalizer = 원래 메서드가 끝나면(예외로 끝나도) 쌓은 것을 내린다. void → 예외를 삼키지 않는다. 과열은 여기서 읽지도 쓰지도 않는다(정산 = WarlockHeatComponent.HandleUnitLeaveCombat 한 곳).
    // 캡처를 못 하면(예외) 쌓지 않는다 → 처리기가 「판정 불가」로 본다(과열 그대로 + 오류).
    [HarmonyPatch(typeof(UnitEntityData), nameof(UnitEntityData.LeaveCombat))]
    static class LeaveCombatCapturePatch
    {
        [HarmonyPrefix]
        static void Prefix(UnitEntityData __instance, out bool __state)
        {
            __state = false;
            try
            {
                var part = __instance.Get<UnitPartWarlockAuto>();
                if (part == null) return; // 정당한 무동작: 과열 부품이 없는 유닛(워락이 아님)
                part.LeaveCaptures.Push(__instance.IsInCombat);
                __state = true;
            }
            catch (Exception ex) { HeatLog.Failed("leave capture", __instance, ex, "leave-capture"); }
        }

        [HarmonyFinalizer]
        static void Finalizer(UnitEntityData __instance, bool __state)
        {
            if (!__state) return; // 정당한 무동작: 이 호출에서 쌓지 않았다
            try
            {
                var part = __instance.Get<UnitPartWarlockAuto>();
                if (part == null) throw new InvalidOperationException("Warlock: UnitPartWarlockAuto vanished during LeaveCombat");
                part.LeaveCaptures.Pop();
            }
            catch (Exception ex) { HeatLog.Failed("leave capture release", __instance, ex, "leave-capture-release"); }
        }
    }

     
    public class WarlockHeatComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackRoll>, IUnitCombatHandler, IRestFinishedHandler, IGlobalSubscriber
    {
        // 굴림 이유의 블라스트 표식(없으면 null = 블라스트 아님: 무기 공격·주문·이유 없는 굴림). 이유 능력 = RuleReason.Ability(정찰 §16-T4).
        static BlastKind? MarkerOf(RuleAttackRoll evt)
        {
            var reason = evt.Reason;
            var ability = reason == null ? null : reason.Ability;
            if (ability == null) return null; // 능력이 아닌 굴림(무기·직접 규칙) — 블라스트가 아니다
            var marker = Blasts.IsBlast(ability.Blueprint);
            return marker == null ? (BlastKind?)null : marker.Kind;
        }

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            try
            {
                int penalty = HeatRule.AttackPenalty(MarkerOf(evt));
                // 연속 사격 줄기마다(설계 §4.5); 그 밖 정당한 무동작. RulebookEvent.AddModifier(int, EntityFact, ModifierDescriptor) 는 protected(정찰 §16-T4) →
                // 공개 AddModifierCopy(Modifier) @0002~0007(= Modifier.Copy → 같은 private AddModifier(Modifier) 저장소)로 같은 항목을 단다.
                if (penalty != 0) evt.AddModifierCopy(new Modifier(penalty, Fact, ModifierDescriptor.Penalty));
            }
            catch (Exception ex) { HeatLog.Failed("attack penalty", Owner, ex); }
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            try
            {
                var marker = MarkerOf(evt);
                var verdict = HeatRule.Observe(evt.IsFake, marker, evt.IsHit);
                if (verdict != HitVerdict.Hit)
                {
                    // 미리보기·블라스트 아님·빗나감 = 세지 않는다(블라스트가 아닌 공격이 대부분이라 경고는 소음 — 설계 §4.7). 유닛 + 판정마다 디버그 1줄.
                    HeatLog.Emit(false, Owner.UniqueId + "|" + verdict, "Warlock: " + Owner.CharacterName + " attack roll not counted for heat (" + verdict + ")");
                    return;
                }
                var part = Owner.Ensure<UnitPartWarlockAuto>();
                part.RecoverIfAbandoned();
                int rank = WarlockHeat.Rank(Owner);
                bool accel = Owner.HasFact(LevelFeatures.Marker(Guids.HeatAccel12Feature));
                int? maxTier = Summons.MaxTier(Owner); // T5: 주문서 최고 주문 레벨 → 최고 단계(null = 판정 불가 → HoldAtMax + ERROR)
                var cmds = HeatRule.OnBlastHit(rank, evt.IsCriticalConfirmed, accel, maxTier, part.Obs(HeatClock.Now()));
                ApplyEffects(HeatRule.Apply(rank, cmds, Owner.UniqueId), part);
            }
            catch (Exception ex) { HeatLog.Failed("hit counter", Owner, ex); }
        }

        void ApplyEffects(HeatEffect[] effects, UnitPartWarlockAuto part)
        {
            foreach (var e in effects)
            {
                switch (e.Kind)
                {
                    case HeatEffectKind.WriteRank: WarlockHeat.Write(Owner, e.N); break;
                    case HeatEffectKind.ObsoleteReservation:
                        if (part.Reserved == null) throw new InvalidOperationException("Warlock: TimeoutPending without a reservation");
                        part.Obsolete.Add(part.Reserved);
                        part.Reserved = null;
                        break;
                    case HeatEffectKind.CloseApproved:
                        if (part.ApprovedOpen == null) throw new InvalidOperationException("Warlock: ApprovedTimedOut without an approved execution");
                        part.ApprovedOpen = null;
                        break;
                    case HeatEffectKind.Error: HeatLog.Emit(true, e.OnceKey, e.Text); break;
                    case HeatEffectKind.Debug: HeatLog.Emit(false, e.OnceKey, e.Text); break;
                    case HeatEffectKind.Reserve: AutoSummon.Reserve(Owner, e.N); break;
                    default: throw new ArgumentOutOfRangeException("effects", e.Kind, "Warlock: unknown heat effect");
                }
            }
        }

        public void HandleUnitJoinCombat(UnitEntityData unit) { } // 정당한 무동작: 전투 시작에는 과열을 바꾸지 않는다

        // 전투 종료 = 절반, 내림(소유자 결정 2026-11-16 — 옛 규칙 「0」은 설계 §4.7). 판정 시점은 옛 규칙과 같은 사건: 본편 CombatStateTrigger 와 같은 사건(「설계와 달리 정한 것」 ⑤),
        // 실시간 의식 상실도 이 사건(UnitLifeController.SetLifeState @00A5) — README.
        // 정산은 이 처리기 한 곳(패치는 캡처만): 엔진 UnitEntityData.LeaveCombat 은 전투 중이 아니어도 사건을 낸다(@00AD~00B9) → 그 호출의 캡처(LeaveCombatCapturePatch)가
        // 「호출 직전 전투 중이었다」일 때만 절반을 쓰고, 쓴 뒤 캡처를 Settled 로 바꿔 같은 호출의 사건이 또 와도 다시 쓰지 않는다.
        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            try
            {
                if (!HeatRule.IsOwnerEvent(unit, Owner)) return; // 정당한 무동작: 다른 유닛(전역 구독 — 적이 죽어도 그 적의 사건이 온다, 리뷰 T4 I-2(b))
                var part = Owner.Ensure<UnitPartWarlockAuto>();
                var verdict = HeatRule.OnLeaveCombat(WarlockHeat.Rank(Owner), part.LeaveCaptures.Top());
                switch (verdict.Kind)
                {
                    case LeaveKind.Settle:
                        WarlockHeat.Write(Owner, verdict.Rank);
                        part.LeaveCaptures.MarkSettled(); // 쓰기가 성공한 뒤에만
                        break;
                    case LeaveKind.NotInCombat: // 정당한 무동작: 전투 중이 아니던 유닛의 이탈 호출(엔진은 그래도 사건을 낸다). 유닛마다 디버그 1줄.
                        HeatLog.Emit(false, Owner.UniqueId + "|leave-not-in-combat", "Warlock: " + Owner.CharacterName + " leave-combat event ignored: the unit was not in combat before this LeaveCombat call");
                        break;
                    case LeaveKind.AlreadySettled: // 정당한 무동작: 같은 이탈 호출에 사건이 또 왔다. 유닛마다 디버그 1줄.
                        HeatLog.Emit(false, Owner.UniqueId + "|leave-dup", "Warlock: " + Owner.CharacterName + " leave-combat event ignored: heat already halved for this LeaveCombat call");
                        break;
                    case LeaveKind.Unknown: // 판정 불가: 과열을 건드리지 않고 유닛마다 이 로드에서 오류 1회
                        HeatLog.Emit(true, Owner.UniqueId + "|leave-unknown", "Warlock: " + Owner.CharacterName + " (" + Owner.UniqueId + ") leave-combat event without a captured combat state (the UnitEntityData.LeaveCombat patch did not run for this call, or the event was raised outside LeaveCombat); heat left unchanged");
                        break;
                    default: throw new ArgumentOutOfRangeException("verdict", verdict.Kind, "Warlock: unknown leave verdict");
                }
            }
            catch (Exception ex) { HeatLog.Failed("leave combat", Owner, ex); }
        }

        // 휴식을 마치면 0(설계 §6-6 「휴식 직후 0」). 중단된 휴식은 그대로(PackWarden PackCall.cs:315-323 선례).
        public void HandleRestFinished(RestStatus status)
        {
            try
            {
                if (status == null) throw new ArgumentNullException("status");
                if (!status.RestSucceeded) return; // 정당한 무동작: 중단 휴식
                WarlockHeat.Write(Owner, WarlockRules.Dials.HeatAfterRest); // = HeatRule.AfterRest(모든 랭크) — 랭크를 읽지 않는다(손상된 랭크 버프도 휴식이 지운다, 옛 동작 유지)
            }
            catch (Exception ex) { HeatLog.Failed("rest finished", Owner, ex); }
        }
    }
}
