using System;
using System.Collections.Generic;
using System.Linq;
using WotrClassMod.Common;
using Kingmaker.AI.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using static PackWarden.BlueprintUtil;




namespace PackWarden
{
     
    // 원하는 모드 = 메인 펫 상태 하나(ShadowRules.ModeFor). 보조의 현재 상태는 불변식 관측(Observe → ShadowRules.ModeMatches)으로 판정하고,
    // 어긋나면 ApplyMode: SetBrain → RestoreAvailableActions(본편 ChangeBrain.RunAction @0017→@0021 순서) → InterruptAll(AI 명령만, 대기열 포함) → 되읽기.
    // 엔진 사실(dnlib 2026-11-14, SHA256 2CB7160B…8953):
    //   UnitBrain.SetBrain @0002~0044 = Blueprint·Actions 만 교체 · RestoreAvailableActions @0001 ClearDefaultActions → @0051 Add → @0094 Sort.
    //   UnitCommands.InterruptAll(Func) @0004~0036 현재 슬롯 + @0038~007C 대기열, 술어가 참이면 Interrupt(true) 후 제거.
    //   UnitCommand.Interrupt @0000~000F: Result == Interrupt(2) 또는 IsFinished 면 무동작, 아니면 @0013 set_Result(2).
    //   EventBus 구독자 예외는 SubscriptionManager.RaiseEvent @00B8~00F6 이 삼킨다 → 감시자 실패는 FailCount 로 센다(T3 과 같은 계약: 증가 → 로그 → 재throw).
    public static class ShadowPack
    {
        public sealed class Observation
        {
            public string BrainGuid;
            public string[] ActionGuids, AvailableGuids; // 정렬
            public int AttackActions, AiAttackCmds, AiFollowCmds, AiFollowWrongTarget;
            public override string ToString()
            {
                return "brain=" + BrainGuid + " actions=[" + string.Join(",", ActionGuids) + "] available=[" + string.Join(",", AvailableGuids) + "] attackActions=" + AttackActions
                    + " aiAttackCmds=" + AiAttackCmds + " aiFollowCmds=" + AiFollowCmds + " aiFollowWrongTarget=" + AiFollowWrongTarget;
            }
        }

        // 전환 카운터(경로 계수 — 계획 d). [0] = Shadow, [1] = Independent. 쓰는 곳 = Bump 하나(시험 잠금).
        static readonly int[] transitions = new int[2];




        // 감시자·소환 전환 실패 수(엔진이 예외를 삼키므로). 기준선 = 점검 실행 시작(Scenarios.RegisterAll), 판정 = ShadowRules.ShadowHealthVerdict 4번째 항.
        static int failCount;




        // 주기 재조정 도달 수(전투 진입·새 라운드 — 펫 사건 없이 펫이 빠지는 경로 EtudeBracketDetachPet.OnEnter @0083 SetPetAsEx 를 덮는다).
        // 점검 S3 aux-brain 이 toggle-on 대비 ≥ 1 을 단언(지도자 전투 진입이 이 훅을 지났다는 증거). 쓰는 곳 = CountPeriodic 하나.
        // 재검토 m-1(2026-11-14): 전역 수만으로는 「지도자 자신의 훅」을 증명하지 못한다(필터가 뒤집혀도 다른 유닛의 라운드로 오른다).
        // 두 무리 조련사가 같은 전투에 참가할 수 있으므로, 각 감시자가 처리한 사건과 그중 소유자 본인의 사건을 따로 센다.
        static int periodicCount;






        internal static void CountPeriodic(UnitEntityData owner, UnitEntityData unit)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            periodicCount++;



        }

        static int Slot(ShadowRules.PackMode mode)
        {
            switch (mode)
            {
                case ShadowRules.PackMode.Shadow: return 0;
                case ShadowRules.PackMode.Independent: return 1;
                default: throw new ArgumentOutOfRangeException("mode", mode, "PackWarden: unknown PackMode");
            }
        }









        static void Bump(UnitEntityData aux, ShadowRules.PackMode mode)
        {
            int s = Slot(mode);
            transitions[s]++;



        }





        // FailCount 계약(한 정의): 증가 → 로그. 호출자가 그다음 재throw 한다(throw;).
        internal static void CountFailure(string where, UnitEntityData leader, Exception e)
        {
            failCount++;
            Main.Entry.Logger.Error(ShadowRules.ModeFailLogHead + where + " (leader " + (leader == null ? "null" : leader.CharacterName) + "): " + e);
        }

        // 지금 무리 상태(제거 사건 밖). 사건 밖에서는 판정 불가·해산이 나올 수 없다 — 나오면 DecideState 결함이라 던진다(C2).
        public static ShadowRules.PackState StateOf(UnitEntityData leader)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            string unknown;
            var state = DecideState(leader, null, out unknown);
            if (unknown != null) throw new InvalidOperationException("PackWarden: StateOf got an UNKNOWN without a removal event: " + unknown);
            if (state == ShadowRules.PackState.Dismiss) throw new InvalidOperationException("PackWarden: StateOf got Dismiss without a removal event");
            return state;
        }

        // 게임 값만 모아 순수 판정(ShadowRules.StateForMainPet) 하나를 부른다. removed = 처리 중인 HandleRemovePet 의 pet 인자(그 밖에는 null).
        static ShadowRules.PackState DecideState(UnitEntityData leader, UnitEntityData removed, out string unknown)
        {
            var pet = leader.GetPet(PetType.AnimalCompanion);
            bool found = pet != null;
            bool removedByEvent = removed != null && (found ? ReferenceEquals(pet, removed) : IsCompanionOf(removed, leader));
            return ShadowRules.StateForMainPet(found,
                found && !pet.Destroyed,
                found && !pet.IsInGame,
                removedByEvent,
                found && pet.State.IsDead,
                found && pet.State.IsConscious,
                out unknown);
        }

        // 제거 사건이 알린 유닛이 이 지도자의 동물 동료였는가(목록에서 이미 빠진 뒤 — RemovePet @001E Remove → @0043 발행 → @006C UnitPartPet.Clear 순이라 발행 시점엔 파트가 남아 있다).
        static bool IsCompanionOf(UnitEntityData unit, UnitEntityData leader)
        {
            var pp = unit.Get<UnitPartPet>();
            return pp != null && pp.Type == PetType.AnimalCompanion && ReferenceEquals(pp.Master, leader);
        }

        // 숨김·보이기·제거 해산 수(진단 — 점검 S3 hide-with-main / show-with-main 이 증가를 본다). 쓰는 곳 = SetAuxInGame·ReconcileCore 의 해산 분기.
        static int hideCount, showCount, removalDismissCount;






        public static int RemovalDismissCount { get { return removalDismissCount; } }

        // 보조를 메인과 함께 숨기거나 보인다 — 엔진이 펫을 숨기는 것과 같은 API(EntityDataBase.set_IsInGame: HideAllPets.TryHidePet @0082, UnitPartPet.SetHidden @002E).
        // 사망·해산이 아니다: 슬롯을 비우지 않고(RaiseSlotCleared·Forget 없음), 두뇌·전환 카운터도 건드리지 않는다(ApplyMode·Bump 없음). 되읽기가 다르면 던진다.
        static void SetAuxInGame(UnitEntityData leader, UnitEntityData aux, bool visible)
        {
            aux.IsInGame = visible;
            if (aux.IsInGame != visible) throw new InvalidOperationException("PackWarden: " + aux.CharacterName + " IsInGame readback " + aux.IsInGame + " after setting " + visible);
            if (visible) showCount++; else hideCount++;
            Main.Entry.Logger.Log("PackWarden: " + aux.CharacterName + (visible ? " shown again with" : " hidden with") + " the main companion (leader " + leader.CharacterName + ")");
        }

        // 살아 있는 보조(숨은 것 포함)의 보이기를 상태에 맞춘다(순서 = 순수 ShadowRules.VisibilitySteps).
        static void ReconcileVisibility(UnitEntityData leader, UnitPartPack part, bool visible)
        {
            var present = new List<UnitEntityData>();
            var inGame = new List<bool>();
            for (int i = 0; i < PackRules.AuxSlotCount; i++)
            {
                var aux = part.PresentAt(i);
                if (aux == null) continue; // 빈 슬롯·죽은·파괴된 보조 = 보일 것이 없다
                present.Add(aux);
                inGame.Add(aux.IsInGame);
            }
            foreach (int k in ShadowRules.VisibilitySteps(inGame, visible)) SetAuxInGame(leader, present[k], visible);
        }

        public static Observation Observe(UnitEntityData aux)
        {
            if (aux == null) throw new ArgumentNullException("aux");
            var summoned = aux.Get<UnitPartSummonedMonster>();
            if (summoned == null || summoned.Summoner == null) throw new InvalidOperationException("PackWarden: Observe on " + aux.CharacterName + " without a summoner (not a pack auxiliary)");
            var pet = summoned.Summoner.GetPet(PetType.AnimalCompanion);
            var brain = aux.Descriptor.Brain;
            if (brain == null) throw new InvalidOperationException("PackWarden: " + aux.CharacterName + " has no UnitBrain");
            if (brain.Blueprint == null) throw new InvalidOperationException("PackWarden: " + aux.CharacterName + " brain has no blueprint");
            var o = new Observation
            {
                BrainGuid = brain.Blueprint.AssetGuid.ToString(),
                ActionGuids = GuidsOf(brain.Actions, aux, "Actions"),
                AvailableGuids = GuidsOf(brain.AvailableActions, aux, "AvailableActions"),
            };
            foreach (var a in brain.Actions)
            {
                var bp = a.Blueprint as BlueprintAiAction;
                if (bp == null) throw new InvalidOperationException("PackWarden: " + aux.CharacterName + " brain action blueprint is not a BlueprintAiAction");
                if (ShadowBrains.IsAiAttack(bp)) o.AttackActions++;
            }
            var raw = aux.Commands.Raw;
            var queue = aux.Commands.Queue;
            if (raw == null || queue == null) throw new InvalidOperationException("PackWarden: " + aux.CharacterName + " command lists are null");
            // 교전 판정은 지연 계산(펫을 알고 대상이 펫이 아닌 추종 명령이 처음 나올 때 1회) — 펫 없음(독립 모드의 정상 상태)에서 판정 불가를 장부에 남기지 않게.
            ShadowRules.EngageVerdict? engage = null;
            UnitEntityData engagedTarget = null;
            string engageReason = null;
            foreach (var c in raw.Concat(queue))
            {
                if (c == null || c.AiAction == null) continue; // 빈 슬롯 칸(엔진 GetCommand<T> @000C 도 건너뜀)·플레이어/AoO 명령은 모드와 무관
                if (c is UnitAttack) o.AiAttackCmds++;
                else if (c is UnitFollow)
                {
                    o.AiFollowCmds++;
                    bool targetIsPet = pet != null && ReferenceEquals(c.TargetUnit, pet);
                    bool? engagedEnemy = null; // 계산 안 함(펫 없음 또는 대상 = 펫) — FollowTargetStale 계약
                    if (pet != null && !targetIsPet)
                    {
                        // 2026-11-18: 교전 추종 명령(대상 = 주 동물이 교전 중인 적)은 유효. 판정은 추종 행동과 같은 ShadowBrains.Engagement(C4).
                        if (engage == null) engage = ShadowBrains.Engagement(aux, out engagedTarget, out engageReason).Verdict;
                        engagedEnemy = ShadowRules.TargetIsEngagedEnemy(engage.Value, engagedTarget != null && ReferenceEquals(c.TargetUnit, engagedTarget));
                    }
                    var stale = ShadowRules.FollowTargetStale(pet != null, targetIsPet, engagedEnemy);
                    // 판정 불가: 기존 동작(낡음으로 세어 재적용) + 장부 1건(첫 발생만 오류 로그) — 점검 ShadowHealth 가 증가 0 을 단언한다.
                    if (stale == ShadowRules.FollowStale.Unknown)
                        ShadowBrains.FollowUnknown.Record(aux.UniqueId, "observe follow command (counted stale): " + (engageReason ?? "engagement unknown without reason"), ShadowBrains.FollowUnknownLog);
                    if (ShadowRules.CountsAsWrongTarget(stale)) o.AiFollowWrongTarget++;
                }
            }
            return o;
        }

        static string[] GuidsOf(List<Kingmaker.AI.AiAction> list, UnitEntityData aux, string what)
        {
            if (list == null) throw new InvalidOperationException("PackWarden: " + aux.CharacterName + " Brain." + what + " is null");
            var r = new List<string>();
            foreach (var a in list)
            {
                if (a == null || a.Blueprint == null) throw new InvalidOperationException("PackWarden: " + aux.CharacterName + " Brain." + what + " has an entry without blueprint");
                r.Add(a.Blueprint.AssetGuid.ToString());
            }
            r.Sort(StringComparer.Ordinal);
            return r.ToArray();
        }

        public static bool Matches(Observation o, ShadowRules.PackMode mode)
        {
            if (o == null) throw new ArgumentNullException("o");
            return ShadowRules.ModeMatches(o.BrainGuid, o.ActionGuids, o.AvailableGuids, o.AttackActions, o.AiAttackCmds, o.AiFollowWrongTarget, mode);
        }

        // 트랜잭션: 두뇌 → 목록 → AI 명령 중단(현재 + 대기열) → 되읽기(실패면 던짐) → 중단 목록 기록 → 카운터 → 로그.
        public static void ApplyMode(UnitEntityData leader, UnitEntityData aux, ShadowRules.PackMode mode)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            if (aux == null) throw new ArgumentNullException("aux");
            var bp = Get<BlueprintBrain>(ShadowRules.BrainGuidFor(mode));
            var brain = aux.Descriptor.Brain;
            if (brain == null) throw new InvalidOperationException("PackWarden: " + aux.CharacterName + " has no UnitBrain");
            brain.SetBrain(bp);
            brain.RestoreAvailableActions();
            var hit = new List<UnitCommand>();
            aux.Commands.InterruptAll(c =>
            {
                bool stop = ShadowRules.InterruptsOnSwitch(c.AiAction != null); // AoO·플레이어 명령 보존(D4) — 판정은 순수 함수
                if (stop) hit.Add(c);
                return stop;
            });
            var o = Observe(aux);
            var problem = ShadowRules.SwitchReadbackProblem(Matches(o, mode), o.AiAttackCmds, o.AiFollowCmds);
            if (problem != null) throw new InvalidOperationException("PackWarden: ApplyMode(" + mode + ") on " + aux.CharacterName + ": " + problem + " (" + o + ")");



            Bump(aux, mode);
            Main.Entry.Logger.Log("PackWarden: " + aux.CharacterName + " -> " + mode + " (leader " + leader.CharacterName + ", interrupted=" + hit.Count + ")");
        }

        // 살아 있는 보조마다 불변식이 원하는 모드와 맞지 않으면 ApplyMode(같으면 정당한 무동작). GUID 비교 단독 금지(SP-04).
        public static void Reconcile(UnitEntityData leader)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            ReconcileCore(leader, null);
        }

        // 펫 제거 사건(IPetHandler.HandleRemovePet)용: 사건이 알린 유닛으로 판정해 맞춘다(목록에서 빠짐 → 해산, 숨김 → 함께 숨김).
        // 엔진 사실(dnlib 2026-11-14, SHA256 2CB7160B…8953): RemovePet 은 @001E m_Pets.Remove 뒤 @0043 에 발행(GetPet 은 이미 그 펫을 안 돌려준다);
        // HideAllPets.TryHidePet 은 m_Pets 를 그대로 두고 @0076 SetHidden(true)·@0082 IsInGame=false 뒤 @00CF 에 발행(GetPet 이 숨긴 펫을 돌려준다 → inWorld=false 로 부재).
        // 발행처 둘 다 pet 인자는 null 이 아니다(RemovePet 은 Remove 성공 @0023 뒤에만, TryHidePet 은 펫 파트가 있을 때 @002B 뒤에만 발행) → null 은 던진다(C2).
        public static void ReconcileAfterRemoval(UnitEntityData leader, UnitEntityData removedPet)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            if (removedPet == null) throw new ArgumentNullException("removedPet", "PackWarden: HandleRemovePet without the removed unit (engine raisers always pass it)");
            ReconcileCore(leader, removedPet);
        }

        // 상태(ShadowRules.StateForMainPet)에 맞춘다: 해산 → PackCall.Dismiss(기존 해산 경로) · 보이기 맞춤(숨김 ↔ 보임) · 보이는 보조마다 불변식이 원하는 모드와 맞지 않으면 ApplyMode(같으면 정당한 무동작).
        // 판정 불가(unknown)면 보조를 먼저 맞춘 뒤(부재 = 독립으로) 던진다 → 감시자 catch 가 CountFailure(FailCount++ → 로그) → 재throw.
        static void ReconcileCore(UnitEntityData leader, UnitEntityData removedPet)
        {
            string unknown;
            var state = DecideState(leader, removedPet, out unknown);
            var part = leader.Get<UnitPartPack>();
            if (state == ShadowRules.PackState.Dismiss)
            {
                 
                if (part == null) return; // 한 번도 소환한 적 없음 = 해산할 보조가 없음(PackCall.Dismiss 와 같은 읽기)
                PackCall.Dismiss(leader);
                removalDismissCount++;
                Main.Entry.Logger.Log("PackWarden: main companion of " + leader.CharacterName + " was removed (HandleRemovePet) -> auxiliaries dismissed");
                return;
            }
            bool visible = ShadowRules.AuxVisibleFor(state);
            if (part != null) ReconcileVisibility(leader, part, visible);
            var live = new List<UnitEntityData>();
            var matching = new List<bool>();
            if (part != null && visible) // part 없음 = 한 번도 소환한 적 없음 = 맞출 보조가 없음. 숨김이면 두뇌는 그대로 둔다(보일 때 다시 맞춘다)
            {
                for (int i = 0; i < PackRules.AuxSlotCount; i++)
                {
                    UnitEntityData aux;
                    var st = part.SlotAt(i, out aux);  
                    // 해석되지 않는 ID: 맞출 두뇌가 없다 — 그 자리만 건너뛰고 판정 불가로 남긴다(다른 보조의 모드 맞춤은 그대로).
                    if (st == PackRules.SlotState.Unresolved) { UnitPartPack.NoteUnresolvedSkip("brain-reconcile", leader, part, i); continue; }
                    if (st != PackRules.SlotState.Live) continue; // 빈 슬롯·죽은 보조 = 두뇌가 돌지 않는다
                    live.Add(aux);
                    matching.Add(Matches(Observe(aux), ShadowRules.BrainModeFor(state)));
                }
            }
            // 재검토 m-2: 순서(맞지 않는 보조를 모두 맞춘 뒤, 판정 불가면 마지막에 던짐)는 순수 ShadowRules.ReconcileSteps 하나가 정한다(실행 표).
            foreach (int step in ShadowRules.ReconcileSteps(matching, unknown != null))
            {
                if (step == ShadowRules.ReconcileThrowStep) throw new InvalidOperationException("PackWarden: Reconcile(" + leader.CharacterName + "): " + unknown + "; auxiliaries set to " + state);
                ApplyMode(leader, live[step], ShadowRules.BrainModeFor(state));
            }
        }
    }

    // 지도자 CallFeature 의 세 번째 컴포넌트. 메인 펫 생사·소생·펫 교체·지역 로드 때 Reconcile. 처리기 예외는 엔진이 삼키므로 CountFailure → 재throw.
    // 소생 경로(dnlib): UnitDescriptor.Resurrect @03CD IUnitResurrectedHandler 발행 시점엔 아직 LifeState = Dead(ForceUnitConscious 는 @04A9) →
    // 그 처리기의 Reconcile 은 무동작이고, 실제 복귀는 @04A9 → SetLifeState @0079 UpdateLifeState → @014B RaiseLifestateChanged(동기) 의 IUnitLifeStateChanged 에서 일어난다.
    // 주기 재조정(펫 사건에 기대지 않음): 지도자 전투 진입(IUnitCombatHandler.HandleUnitJoinCombat — UnitEntityData.JoinCombat @006C 발행)과
    // 지도자 새 라운드(IUnitNewCombatRoundHandler.HandleNewCombatRound — 실시간 UnitCombatCooldownsController.TickOnUnit @0118, 턴제 TurnController.Prepare @0243 발행).
    // 판정은 같은 ReconcileCore → ShadowRules.ModeForMainPet. 맞으면 무동작(전환 카운터 불변).
     
    // 메인 펫이 숨거나(HideAllPets.TryHidePet @0076 SetHidden → @002E set_IsInGame, 파티 밖 지도자 OnIsInGameChanged @00BB) 다시 보이면(HideAllPets.OnTurnOff @006D SetHidden(false))
    // 그 자리에서 Reconcile. IPetHandler 사건(TryHidePet @00CF HandleRemovePet · OnTurnOff @00BD HandleAddPet)은 그 뒤에 오고 같은 판정이라 무동작이다.
    public class PackModeWatcher : UnitFactComponentDelegate, IUnitLifeStateChanged, IUnitResurrectedHandler, IPetHandler, IAreaHandler, IUnitCombatHandler, IUnitNewCombatRoundHandler, IInGameHandler, IGlobalSubscriber
    {
        public void HandleObjectInGameChanged(Kingmaker.EntitySystem.EntityDataBase entity)
        {
            try { if (IsMainPet(entity as UnitEntityData)) ShadowPack.Reconcile(Owner); }
            catch (Exception e) { ShadowPack.CountFailure("HandleObjectInGameChanged", Owner, e); throw; }
        }

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
            try { if (ReferenceEquals(unit, Owner)) { ShadowPack.CountPeriodic(Owner, unit); ShadowPack.Reconcile(Owner); } }
            catch (Exception e) { ShadowPack.CountFailure("HandleUnitJoinCombat", Owner, e); throw; }
        }

        public void HandleUnitLeaveCombat(UnitEntityData unit) { } // 정당한 무동작: 해산은 PackCall 이 맡는다

        public void HandleNewCombatRound(UnitEntityData unit)
        {
            try { if (ReferenceEquals(unit, Owner)) { ShadowPack.CountPeriodic(Owner, unit); ShadowPack.Reconcile(Owner); } }
            catch (Exception e) { ShadowPack.CountFailure("HandleNewCombatRound", Owner, e); throw; }
        }

        bool IsMainPet(UnitEntityData unit)
        {
            return unit != null && ReferenceEquals(unit, Owner.GetPet(PetType.AnimalCompanion));
        }

        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        {
            try { if (IsMainPet(unit)) ShadowPack.Reconcile(Owner); }
            catch (Exception e) { ShadowPack.CountFailure("HandleUnitLifeStateChanged", Owner, e); throw; }
        }

        public void HandleUnitResurrected(UnitEntityData unit)
        {
            try { if (IsMainPet(unit)) ShadowPack.Reconcile(Owner); }
            catch (Exception e) { ShadowPack.CountFailure("HandleUnitResurrected", Owner, e); throw; }
        }

        public void HandleAddPet(UnitEntityData master, UnitEntityData pet)
        {
            try { if (ReferenceEquals(master, Owner)) ShadowPack.Reconcile(Owner); }
            catch (Exception e) { ShadowPack.CountFailure("HandleAddPet", Owner, e); throw; }
        }

        public void HandleRemovePet(UnitEntityData master, UnitEntityData pet)
        {
            // 사건이 알린 펫으로 판정한다(ShadowPack.ReconcileAfterRemoval): 목록에서 빠짐 = 해산, 숨김 = 함께 숨김. 발행 순서는 그 메서드 주석(RemovePet @001E/@0043, TryHidePet @0082/@00CF).
            try { if (ReferenceEquals(master, Owner)) ShadowPack.ReconcileAfterRemoval(Owner, pet); }
            catch (Exception e) { ShadowPack.CountFailure("HandleRemovePet", Owner, e); throw; }
        }

        public void OnAreaDidLoad()
        {
            try { RetiredStudy.AfterLoad(Owner); ShadowPack.Reconcile(Owner); }
            catch (Exception e) { ShadowPack.CountFailure("OnAreaDidLoad", Owner, e); throw; }
        }

        public void OnAreaBeginUnloading() { } // 정당한 무동작: 내리는 중에는 맞출 것이 없다
    }
}
