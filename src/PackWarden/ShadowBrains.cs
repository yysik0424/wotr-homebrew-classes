using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.Blueprints;
using WotrClassMod.Common;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static PackWarden.BlueprintUtil;

namespace PackWarden
{
     
    // 보조의 두뇌 = 우리 블루프린트 PackWardenShadowBrain(행동 = 추종 1개, 공격 행동 0). 독립 모드(T4)는 본편 Character_Brain 을 참조만 한다.
    //
    // 추종 AI 행동: 본편 BlueprintAiFollow 파생. 대상 필터만 바꾼다(대상 = 이 보조 소환자의 동물 동료).
    // 엔진 경로(dnlib 2026-11-14, SHA256 2CB7160B…8953): UnitBrain.SetBrain @003F → 행동마다 new AiActionSimple(bp) ·
    // AiAction.ScoreTarget @000E IsTargetValidInternal → @005B AiAction.IsTargetValid → AiActionSimple.IsTargetValid @0007 → 이 블루프린트의 IsTargetValid.
    // IsTargetValidInternal 의 거리 검사(@0034~0047, GetRange = −1000)는 이동 금지·도달 불가 대상에서만 돈다(@000B·@0032 분기) → 보통은 바로 이 재정의에 닿는다.
    // 명령은 기반 CreateCommand(@000C new UnitFollow(target, ApproachRange.Meters)).
    public class PackWardenAiFollowPet : BlueprintAiFollow
    {
        public PackWardenAiFollowPet()
        {
            // 고려 사항 없음 = 빈 배열(null 도 ReferenceArrayProxy.get_Length @0006~000B 가 0 으로 읽지만, 「없음」을 명시한다).
            m_TargetConsiderations = new ConsiderationReference[0];
            m_ActorConsiderations = new ConsiderationReference[0];
        }

        // 기반 BlueprintAiFollow.GetTargetType 은 필드 TargetType(생성자 기본 Enemy=2)을 돌려준다. 엔진에서 그 필드를 읽는 곳은 기반 GetTargetType 하나뿐
        // (dnlib 전수 스캔 2026-11-14) → 재정의가 유일한 원천이다(필드는 건드리지 않는다 — 재정의가 빠지면 Build 의 되읽기 단언이 던진다).
        public override TargetType GetTargetType(DecisionContext context) { return TargetType.Friend; }

        public override bool IsTargetValid(DecisionContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException("ctx");
            if (ctx.Unit == null) throw new InvalidOperationException("PackWarden: follow-pet AI action scored with no acting unit");
            var summoned = ctx.Unit.Get<UnitPartSummonedMonster>();
            // 우리 보조가 아닌 유닛이 이 두뇌를 가짐 = 배선 결함.
            if (summoned == null) throw new InvalidOperationException("PackWarden: " + ctx.Unit.CharacterName + " uses PackWardenShadowBrain but is not a summoned unit");
            // 엔진은 CalculateTargetsScore @004C 에서 Target 을 채운 뒤 부른다.
            if (ctx.Target == null) throw new InvalidOperationException("PackWarden: follow-pet AI action scored with no target");
            var leader = summoned.Summoner;
            var pet = leader == null ? null : leader.GetPet(PetType.AnimalCompanion);
            var f = ShadowRules.ClassifyFollowTarget(leader == null, pet == null, pet != null && ReferenceEquals(ctx.Target.Unit, pet));
            // 도달 계수(검토 Important-2): 판정마다 1회, 판정 불가 포함. 점검 S3 aux-brain 이 전투 진입 뒤 Accepted 증가 ≥ 1 을 단언한다.
            ShadowBrains.FollowTally.Record(f);
            switch (f)
            {
                case ShadowRules.FollowTarget.Accept: return true;
                case ShadowRules.FollowTarget.Reject: return false;
                case ShadowRules.FollowTarget.UnknownNoSummoner:
                case ShadowRules.FollowTarget.UnknownNoPet:
                    // 판정 불가(C1 ④): AI 루프 안이라 throw 대신 장부에 세고(Count++ 먼저) 첫 발생만 로그. 점검이 증가량 0 을 단언한다.
                    // 정상 흐름에선 펫 부재 = 독립 모드로 이미 전환돼(T4) 이 행동이 실행 목록에 없어야 한다.
                    ShadowBrains.FollowUnknown.Record(ctx.Unit.UniqueId, f.ToString(), ShadowBrains.FollowUnknownLog);
                    return false;
                default:
                    throw new InvalidOperationException("PackWarden: ClassifyFollowTarget returned " + f);
            }
        }
    }

    // 소유자 결정 2026-11-16: 주 동물이 싸우는 동안 보조는 주 동물이 아니라 그 공격 대상 곁으로 간다. 두 번째 추종 행동(대상 = 적, BaseScore 10).
    // 스스로 공격하지 않는다: 명령은 UnitFollow(이동만)이고 이 행동은 공격 행동이 아니다(ShadowRules.ExpectedAttackActions(Shadow) = 0 그대로). 따라치기는 PackShadowStrikes 그대로.
    // 기존 PackWardenAiFollowPet 은 그대로 두고 점수로 가른다: 교전이면 이 행동이 이기고, 교전 아님·판정 불가·도달 불가(엔진 AiAction.IsTargetValidInternal @002D
    // CombatState.AIData.UnreachableUnits 가 이 재정의보다 먼저 거른다)이면 유효 대상이 없어 펫 추종(1)만 남는다.
    public class PackWardenAiFollowEngaged : BlueprintAiFollow
    {
        public PackWardenAiFollowEngaged()
        {
            m_TargetConsiderations = new ConsiderationReference[0];
            m_ActorConsiderations = new ConsiderationReference[0];
        }

        // 적 목록만 점수 매긴다(AiBrainController.CalculateActionScore @0044 GetTargetType → Enemy 면 DecisionContext.Enemies).
        public override TargetType GetTargetType(DecisionContext context) { return TargetType.Enemy; }

        public override bool IsTargetValid(DecisionContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException("ctx");
            if (ctx.Unit == null) throw new InvalidOperationException("PackWarden: engaged-follow AI action scored with no acting unit");
            if (ctx.Target == null) throw new InvalidOperationException("PackWarden: engaged-follow AI action scored with no target");
            UnitEntityData engaged;
            string reason;
            var pick = ShadowBrains.Engagement(ctx.Unit, out engaged, out reason);
            switch (pick.Verdict)
            {
                case ShadowRules.EngageVerdict.Engaged:
                    {
                        bool hit = ReferenceEquals(ctx.Target.Unit, engaged);
                        // 도달 계수(점검 S3 aux-brain): 교전 추종이 유효 대상을 받아들인 횟수. 판정에 쓰지 않는다(관측만).
                        if (hit) ShadowBrains.EngagedFollowAccepted++;
                        return hit;
                    }
                case ShadowRules.EngageVerdict.NotEngaged: return false;
                case ShadowRules.EngageVerdict.Unknown:
                    // 판정 불가 = 이 행동은 쓰지 않는다(펫 추종이 남는다) + 장부(점검 S11·leave 단계가 증가량 0 을 단언).
                    ShadowBrains.FollowUnknown.Record(ctx.Unit.UniqueId, reason, ShadowBrains.FollowUnknownLog);
                    return false;
                default:
                    throw new InvalidOperationException("PackWarden: ClassifyEngagement returned " + pick.Verdict);
            }
        }

        // 접근 거리 = 보조 자신의 근접 사거리 = 따라치기 사정거리와 같은 엔진 식(UnitAttack.GetApproachRadius(무기, 보조, 대상) — PackShadowStrikes.Strike 와 같은 호출).
        // 무기는 엔진 UnitAttack.GetApproachRadius(UnitEntityData) 가 고르는 것과 같은 GetFirstWeapon. 무기가 없으면 판정 불가 → 펫 추종 명령 + 장부.
        public override UnitCommand CreateCommand(DecisionContext ctx, UnitEntityData target)
        {
            if (ctx == null) throw new ArgumentNullException("ctx");
            if (ctx.Unit == null) throw new InvalidOperationException("PackWarden: engaged-follow command with no acting unit");
            if (target == null) throw new ArgumentNullException("target", "PackWarden: engaged-follow command with no target");
            var aux = ctx.Unit;
            var weapon = aux.GetFirstWeapon();
            if (weapon == null)
            {
                ShadowBrains.FollowUnknown.Record(aux.UniqueId, "engaged follow: auxiliary has no weapon (approach range unknown)", ShadowBrains.FollowUnknownLog);
                var summoned = aux.Get<UnitPartSummonedMonster>();
                var pet = summoned == null || summoned.Summoner == null ? null : summoned.Summoner.GetPet(PetType.AnimalCompanion);
                if (pet == null) throw new InvalidOperationException("PackWarden: engaged-follow fallback has no main companion for " + aux.CharacterName);
                return new UnitFollow(pet, ShadowRules.FollowApproachFeet.Feet().Meters);
            }
            return new UnitFollow(target, UnitAttack.GetApproachRadius(weapon, aux, target));
        }
    }

    public static class ShadowBrains
    {
        public const string FollowUnknownLogHead = ShadowRules.FollowUnknownLogHead;
        public static readonly ShadowRules.UnknownLedger FollowUnknown = new ShadowRules.UnknownLedger(FollowUnknownLogHead);



        public static readonly ShadowRules.FollowTally FollowTally = new ShadowRules.FollowTally();
        // 교전 추종(PackWardenAiFollowEngaged.IsTargetValid)이 참을 돌려준 횟수. 엔진은 AvailableActions 를 BaseScore 내림차순으로 정렬하고
        // (UnitBrain.RestoreAvailableActions @006F~0094, 비교 <>c::<RestoreAvailableActions>b__28_0), BestScore > BaseScore 인 행동은 점수조차 매기지 않는다
        // (AiBrainController.FindBestAction @019D~01B6) → 교전 추종(10)이 유효하면 펫 추종(1)의 IsTargetValid 는 불리지 않는다. 점검 S3 aux-brain 이 둘 중 하나의 도달을 단언한다.
        public static int EngagedFollowAccepted;
        // 판정 불가 로그 출구(한 인스턴스 — IsTargetValid 안에서 람다 캐시 필드를 만들지 않게).
        internal static readonly Action<string> FollowUnknownLog = s => Main.Entry.Logger.Error(s);

        public static PackWardenAiFollowPet FollowPetAction;
        public static PackWardenAiFollowEngaged FollowEngagedAction;
        public static BlueprintBrain ShadowBrain;

        // ---- 교전 판정(2026-11-16) ----
        sealed class SwingNote { public readonly WeakReference TargetRef = new WeakReference(null); public double? At; }
        static readonly WeakKeyTable<UnitEntityData, SwingNote> s_LastSwing = new WeakKeyTable<UnitEntityData, SwingNote>();
        // AI 는 주 스레드에서 돈다. 판정마다 배열을 새로 만들지 않게 재사용(값은 호출마다 전부 다시 쓴다).
        static readonly bool?[] s_Flags = new bool?[ShadowRules.EngageCandidateCount];
        static readonly UnitEntityData[] s_Cands = new UnitEntityData[ShadowRules.EngageCandidateCount];

        // 게임 시각(초). 시계를 못 읽으면 null(판정 불가로 이어진다).
        static double? NowSeconds()
        {
            var g = Game.Instance;
            if (g == null || g.TimeController == null) return null;
            return g.TimeController.GameTime.TotalSeconds;
        }

        // PackShadowStrikes.OnEventDidTrigger 가 우리 펫의 휘두름마다 부른다(대상·게임 시각). 시계를 못 읽으면 시각 null 로 남겨 판정 불가가 되게 한다(던지지 않음 — 따라치기를 막지 않는다).
        public static void NoteMainSwing(UnitEntityData pet, UnitEntityData target)
        {
            if (pet == null) throw new ArgumentNullException("pet");
            if (target == null) return; // 대상 없는 휘두름은 교전 근거가 아니다(처리기가 뒤에서 결측으로 던진다)
            var n = s_LastSwing.GetValue(pet, _ => new SwingNote());
            n.TargetRef.Target = target;
            n.At = NowSeconds();
        }

        // ---- 교전 추종 관측(2026-11-16 가드) ----
        public static readonly ShadowRules.EngageMissTracker EngageMiss = new ShadowRules.EngageMissTracker();
        public static int EngageMissCount { get { return EngageMiss.ReportCount; } }
        public static string LastEngageMiss;

        // 지도자의 새 라운드마다(PackEngageWatcher). 살아 있는 보조마다: 교전 중이면 「대상 쪽 명령이 있거나 이미 사거리 안」 인지 보고, 아니면 원인을 갈라 연속 횟수를 센다.
        // 다이얼(ShadowRules.EngageMissObservationLimit)에 이르면 보조마다 1회 오류 로그. 판정 = 순수 ShadowRules.ClassifyEngageObservation·EngageMissTracker.
        public static void ObserveEngagedFollow(UnitEntityData leader)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            var part = leader.Get<UnitPartPack>();
            if (part == null) return; // 소집한 적 없음 = 관측할 보조 없음
            for (int i = 0; i < PackRules.AuxSlotCount; i++)
            {
                UnitEntityData aux;
                var st = part.SlotAt(i, out aux);  
                // 해석되지 않는 ID: 관측할 유닛이 없다 — 그 자리만 건너뛰고 판정 불가로 남긴다(관측은 보조마다 따로 세므로 다른 보조의 관측은 그대로).
                if (st == PackRules.SlotState.Unresolved) { UnitPartPack.NoteUnresolvedSkip("engage-observe", leader, part, i); continue; }
                if (st != PackRules.SlotState.Live) continue; // 빈 자리 · 확인된 죽음/파괴/게임 밖
                var brain = aux.Descriptor.Brain;
                // 독립 모드(메인이 쓰러진 동안 본편 두뇌)는 교전 추종 대상이 아니다. 두뇌를 못 읽으면 아래 관측이 판독 불가로 보고한다.
                if (brain != null && brain.Blueprint != null && !ReferenceEquals(brain.Blueprint, ShadowBrain)) { EngageMiss.Clear(aux.UniqueId); continue; }
                UnitEntityData target;
                string reason;
                var pick = Engagement(aux, out target, out reason);
                if (pick.Verdict != ShadowRules.EngageVerdict.Engaged) { EngageMiss.Clear(aux.UniqueId); continue; } // 교전 아님·판정 불가(후자는 IsTargetValid 가 장부에 남긴다)
                UnitEntityData actual;
                var o = Observe(aux, target, out actual);
                if (!EngageMiss.Observe(aux.UniqueId, o)) continue;
                LastEngageMiss = ShadowRules.EngageMissLogHead + "aux=" + aux.CharacterName + " (" + aux.UniqueId + ") target=" + target.CharacterName + " actual command target=" + (actual == null ? "(none)" : actual.CharacterName)
                    + " cause=" + o + " after " + ShadowRules.EngageMissObservationLimit + " consecutive leader rounds (leader " + leader.CharacterName + ")";
                Main.Entry.Logger.Error(LastEngageMiss);
            }
        }

        // 관측 1회의 입력을 읽는다. 못 읽은 값은 null(→ MissUnreadable). actual = 보조가 지금 갖고 있는 명령의 대상(없으면 null).
        static ShadowRules.EngageObservation Observe(UnitEntityData aux, UnitEntityData target, out UnitEntityData actual)
        {
            actual = null;
            bool? cmdTargets = null, hasOther = null;
            var cmds = aux.Commands;
            var raw = cmds == null ? null : cmds.Raw;
            if (raw != null)
            {
                cmdTargets = false; hasOther = false;
                foreach (var c in raw)
                {
                    if (c == null) continue;
                    var tu = c.Target == null ? null : c.Target.Unit;
                    if (ReferenceEquals(tu, target)) cmdTargets = true;
                    else { hasOther = true; if (actual == null) actual = tu; }
                }
                if (cmdTargets == true) actual = target;
            }
            bool? inRange = null;
            var limit = EngagedArrivalLimit(aux, target);
            if (limit != null) inRange = aux.DistanceTo(target) <= limit.Value;
            bool? canMove = aux.State == null ? (bool?)null : aux.State.CanAct && aux.State.CanMove;
            bool? unreachable = null;
            var cs = aux.CombatState;
            if (cs != null && cs.AIData != null && cs.AIData.UnreachableUnits != null) unreachable = cs.AIData.UnreachableUnits.Contains(target);
            bool? inEnemies = null;
            var group = aux.Group;
            var enemies = group == null || group.Memory == null ? null : group.Memory.Enemies;
            if (enemies != null)
            {
                inEnemies = false;
                foreach (var e in enemies) if (e != null && ReferenceEquals(e.Unit, target)) { inEnemies = true; break; }
            }
            return ShadowRules.ClassifyEngageObservation(cmdTargets, inRange, canMove, unreachable, inEnemies, hasOther);
        }

        // 교전 추종의 「도착」 한계(미터, 중심 거리 UnitEntityData.DistanceTo 와 비교). = 교전 추종 명령의 접근 거리(CreateCommand 와 같은 UnitAttack.GetApproachRadius(GetFirstWeapon, 보조, 대상)
        // — 엔진 IL: 무기 사거리 + 보조 몸 반경 + 대상 몸 반경) 와 같다. 일반 펫 추종의 거리 여유는 물기 사거리에 더하지 않는다. 무기가 없으면 null(판정 불가 — 접근 거리를 모른다).
        // 사용처 = 교전 추종 관측(Observe) · 점검 S12 follow(같은 한 곳 — C4).
        public static float? EngagedArrivalLimit(UnitEntityData aux, UnitEntityData target)
        {
            if (aux == null) throw new ArgumentNullException("aux");
            if (target == null) throw new ArgumentNullException("target");
            var weapon = aux.GetFirstWeapon();
            if (weapon == null) return null;
            return UnitAttack.GetApproachRadius(weapon, aux, target);
        }

        static bool Usable(UnitEntityData pet, UnitEntityData u)
        {
            return u != null && !u.Destroyed && u.IsInGame && u.State != null && !u.State.IsDead && pet.IsEnemy(u);
        }

        // 보조 aux 의 「주 동물이 교전 중인 대상」. 판정 = 순수 ShadowRules.ClassifyEngagement. reason = 판정 불가 사유(아니면 null).
        public static ShadowRules.EngagePick Engagement(UnitEntityData aux, out UnitEntityData target, out string reason)
        {
            if (aux == null) throw new ArgumentNullException("aux");
            target = null;
            reason = null;
            var summoned = aux.Get<UnitPartSummonedMonster>();
            var leader = summoned == null ? null : summoned.Summoner;
            var pet = leader == null ? null : leader.GetPet(PetType.AnimalCompanion);
            if (leader == null || pet == null)
            {
                reason = leader == null ? "engagement: no summoner" : "engagement: no main companion";
                return new ShadowRules.EngagePick(ShadowRules.EngageVerdict.Unknown, -1);
            }
            // 0: 펫의 현재 공격 명령 대상(UnitCommands.get_Attack = m_Commands[1] isinst UnitAttack, dnlib 2026-11-16).
            var pc = pet.Commands;
            s_Cands[0] = pc == null || pc.Attack == null ? null : pc.Attack.Target;
            s_Flags[0] = pc == null ? (bool?)null : Usable(pet, s_Cands[0]);
            // 1: 기승 중(지도자가 이 펫에 탐)이면 기수의 현재 공격 명령 대상.
            var rider = leader.Get<UnitPartRider>();
            bool mounted = rider != null && ReferenceEquals(rider.SaddledUnit, pet);
            var lc = leader.Commands;
            s_Cands[1] = !mounted || lc == null || lc.Attack == null ? null : lc.Attack.Target;
            s_Flags[1] = !mounted ? false : lc == null ? (bool?)null : Usable(pet, s_Cands[1]);
            // 2: 펫의 최근 근접 휘두름.
            SwingNote note;
            bool hasNote = s_LastSwing.TryGetValue(pet, out note);
            s_Cands[2] = hasNote ? note.TargetRef.Target as UnitEntityData : null;
            s_Flags[2] = !hasNote ? false : note.At == null ? (bool?)null : ShadowRules.RecentSwingCandidate(NowSeconds(), note.At, Usable(pet, s_Cands[2]));
            var pick = ShadowRules.ClassifyEngagement(s_Flags);
            if (pick.Verdict == ShadowRules.EngageVerdict.Engaged) target = s_Cands[pick.Index];
            else if (pick.Verdict == ShadowRules.EngageVerdict.Unknown)
                reason = "engagement: candidate " + pick.Index + " unreadable (" + (pick.Index == 0 ? "pet commands" : pick.Index == 1 ? "rider commands" : "game clock / swing time") + ")";
            return pick;
        }

        public static void Build()
        {
            if (ShadowBrain != null || FollowPetAction != null || FollowEngagedAction != null) throw new InvalidOperationException("PackWarden: ShadowBrains.Build() ran twice");
            // 본편 추종 행동 값 복사(BaseScore·MinDifficulty·CombatCount). 출처 = Brain/Actions/FollowEnemyAiAction.jbp. 우리 상수와 다르면 던진다(C4).
            var vanillaFollow = Get<BlueprintAiFollow>(Guids.Game.FollowEnemyAiAction);
            if (vanillaFollow.BaseScore != ShadowRules.FollowBaseScore)
                throw new InvalidOperationException("PackWarden: FollowEnemyAiAction.BaseScore is " + vanillaFollow.BaseScore + ", ShadowRules.FollowBaseScore = " + ShadowRules.FollowBaseScore);

            FollowPetAction = Create<PackWardenAiFollowPet>("PackWardenFollowPetAiAction", Guids.FollowPetAiAction, a =>
            {
                a.BaseScore = ShadowRules.FollowBaseScore;
                a.ApproachRange = ShadowRules.FollowApproachFeet.Feet();
                a.CombatCount = vanillaFollow.CombatCount;
                a.MinDifficulty = vanillaFollow.MinDifficulty;
            });
            // 2026-11-16: 교전 대상 추종(두 번째 추종 행동). 접근 거리는 CreateCommand 가 보조 무기로 정한다(ApproachRange 는 쓰지 않지만 기반 기본값 대신 같은 5 ft 로 둔다).
            if (!(ShadowRules.EngageFollowBaseScore > ShadowRules.FollowBaseScore)) throw new InvalidOperationException("PackWarden: EngageFollowBaseScore must exceed FollowBaseScore");
            FollowEngagedAction = Create<PackWardenAiFollowEngaged>("PackWardenFollowEngagedAiAction", Guids.FollowEngagedAiAction, a =>
            {
                a.BaseScore = ShadowRules.EngageFollowBaseScore;
                a.ApproachRange = ShadowRules.FollowApproachFeet.Feet();
                a.CombatCount = vanillaFollow.CombatCount;
                a.MinDifficulty = vanillaFollow.MinDifficulty;
            });
            ShadowBrain = Create<BlueprintBrain>("PackWardenShadowBrain", Guids.ShadowBrain, b =>
                SetField(b, "m_Actions", new[] { Ref<BlueprintAiActionReference>(Guids.FollowPetAiAction), Ref<BlueprintAiActionReference>(Guids.FollowEngagedAiAction) }));

            // 되읽기 단언: 그림자 두뇌 행동 집합 == 기대, 공격 행동 0, 행동 객체 = 우리 추종 행동.
            RequireActions(ShadowBrain, ShadowRules.PackMode.Shadow, "PackWardenShadowBrain");
            var only = ShadowBrain.Actions.ToList();
            if (only.Count != 2 || !only.Any(x => ReferenceEquals(x, FollowPetAction)) || !only.Any(x => ReferenceEquals(x, FollowEngagedAction)))
                throw new InvalidOperationException("PackWarden: PackWardenShadowBrain actions do not resolve to the follow-pet and engaged-follow actions");
            if (FollowEngagedAction.GetTargetType(null) != TargetType.Enemy) throw new InvalidOperationException("PackWarden: engaged-follow action target type is not Enemy");
            if (FollowEngagedAction.BaseScore != ShadowRules.EngageFollowBaseScore) throw new InvalidOperationException("PackWarden: engaged-follow BaseScore readback is " + FollowEngagedAction.BaseScore + ", expected " + ShadowRules.EngageFollowBaseScore);
            // 본편 Character_Brain(독립 모드의 두뇌, T4) — 읽기만.
            RequireActions(Get<BlueprintBrain>(Guids.Game.CharacterBrain), ShadowRules.PackMode.Independent, "Character_Brain");
            if (FollowPetAction.GetTargetType(null) != TargetType.Friend) throw new InvalidOperationException("PackWarden: follow-pet action target type is not Friend");
            if (FollowPetAction.ApproachRange.Meters != ShadowRules.FollowApproachFeet.Feet().Meters) throw new InvalidOperationException("PackWarden: follow-pet approach range readback failed");
            // 검토 R18: 우리 행동의 BaseScore 되읽기(0 이면 AI 가 고르지 않아 추종이 조용히 멈춘다).
            if (FollowPetAction.BaseScore != ShadowRules.FollowBaseScore) throw new InvalidOperationException("PackWarden: follow-pet BaseScore readback is " + FollowPetAction.BaseScore + ", expected " + ShadowRules.FollowBaseScore);
        }

        static void RequireActions(BlueprintBrain brain, ShadowRules.PackMode mode, string what)
        {
            var got = ActionGuids(brain, what);
            var want = ShadowRules.ExpectedActionGuids(mode).Select(g => BlueprintGuid.Parse(g).ToString()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (!got.SequenceEqual(want))
                throw new InvalidOperationException("PackWarden: " + what + " actions [" + string.Join(", ", got) + "], expected [" + string.Join(", ", want) + "] for " + mode);
            int attacks = brain.Actions.Count(IsAiAttack);
            if (attacks != ShadowRules.ExpectedAttackActions(mode))
                throw new InvalidOperationException("PackWarden: " + what + " has " + attacks + " attack action(s), expected " + ShadowRules.ExpectedAttackActions(mode) + " for " + mode);
        }

        // 본편 공격 AI 행동 형식. internal 이라(dnlib 2026-11-14: IsPublic=false, 기반 BlueprintAiAction) `is` 로 못 쓴다 → 형식 사슬의 이름으로 판별.
        // 시험(tests\PackRulesTests.cs CheckShadowWiringT2)이 이 이름의 형식이 Assembly-CSharp 에 있고 BlueprintAiAction 파생인지 대조한다.
        // 판정 = 순수 ShadowRules.ClassifyAiActionTypeChain(시험 exe 실행 표). 여기는 사슬 이름 목록만 만든다.
        public const string AiAttackTypeName = ShadowRules.AiAttackTypeName;

        public static bool IsAiAttack(BlueprintAiAction a)
        {
            if (a == null) throw new ArgumentNullException("a");
            var chain = new List<string>();
            for (var t = a.GetType(); t != null; t = t.BaseType) chain.Add(t.FullName);
            switch (ShadowRules.ClassifyAiActionTypeChain(chain))
            {
                case ShadowRules.AiActionKind.Attack: return true;
                case ShadowRules.AiActionKind.NotAttack: return false;
                default: throw new InvalidOperationException("PackWarden: cannot classify AI action " + a.name + " (type chain [" + string.Join(", ", chain.ToArray()) + "])");
            }
        }

        // 두뇌 블루프린트의 행동 GUID(정렬). 비었거나 풀리지 않는 항목은 던진다.
        public static string[] ActionGuids(BlueprintBrain brain, string what)
        {
            if (brain == null) throw new ArgumentNullException("brain", "PackWarden: " + what + " is null");
            var acts = brain.Actions;
            if (acts == null) throw new InvalidOperationException("PackWarden: " + what + " has no action list");
            var list = new List<string>();
            foreach (var a in acts)
            {
                if (a == null) throw new InvalidOperationException("PackWarden: " + what + " has an unresolved action reference");
                list.Add(a.AssetGuid.ToString());
            }
            return list.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }
    }

    // 지도자 CallFeature 의 네 번째 컴포넌트(2026-11-16 가드): 지도자의 새 라운드마다 교전 추종 관측. 기존 PackModeWatcher 는 건드리지 않는다(그 IL 잠금 유지).
    // 이 블루프린트 컴포넌트 인스턴스는 모든 지도자가 공유한다 → 인스턴스 필드 없음. 처리기 예외는 엔진이 삼키므로 ShadowPack.CountFailure 로 센 뒤 다시 던진다.
    public class PackEngageWatcher : Kingmaker.UnitLogic.UnitFactComponentDelegate, Kingmaker.PubSubSystem.IUnitNewCombatRoundHandler, Kingmaker.PubSubSystem.IGlobalSubscriber
    {
        public void HandleNewCombatRound(UnitEntityData unit)
        {
            try { if (ReferenceEquals(unit, Owner)) ShadowBrains.ObserveEngagedFollow(Owner); }
            catch (Exception e) { ShadowPack.CountFailure("PackEngageWatcher.HandleNewCombatRound", Owner, e); throw; }
        }
    }
}
