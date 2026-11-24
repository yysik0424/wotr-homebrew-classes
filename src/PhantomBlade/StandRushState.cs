using System;
using WotrClassMod.Common;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using TurnBased.Controllers;

namespace PhantomBlade
{
     
    // 이번 라운드에 스탠드 타격을 일으킨 본체 명중 수를 세고, 「이번 명중이 칠 수 있는가」는 순수 StandRules.MayStrike(사용 수, 레벨, 전력 공격)가 정한다.
    // 트리거의 OnlyOnFirstHit 는 라운드 제한이 아니다(AbstractWeaponTrigger.IsSuitable IL 0054~006A 빗나감 조기 반환 → 031D~0334 의 HadHit 초기화를
    // 못 밟음 / 기회공격은 IsFirstAttack 이 없음) → 소유자별 「이번 라운드에 썼는가」를 여기서 들고, ContextActionStandStrike 가 실행 시 TryConsume 한다.
    // 라운드 경계 = IUnitNewCombatRoundHandler(전역 구독자). dnlib 확인(2026-11-07, Assembly-CSharp SHA-256 2CB7160B…):
    //   - 실시간(RTwP): UnitCombatCooldownsController.TickOnUnit — TickOnUnitTurnBased 가 턴제가 아니면 true(IL 0000~0008) → 계속,
    //     표준 행동 쿨다운의 이전 값 > 0 을 IL 0049~0054 에서 잡고, 감산 뒤 ≤ 0 이면 IL 0100~011E 에서 OnNewRound + RaiseEvent<IUnitNewCombatRoundHandler>(tUnit).
    //     공격은 UnitEntityData.SpendAction(Standard) 로 쿨다운 6 초(IL 00AE~00B6) → 공격 시퀀스 하나 = 그 유닛의 한 라운드.
    //   - 턴제(TB): TurnController.Prepare IL 0220~024E — 그 유닛의 턴 시작에 OnNewRound + RaiseEvent(b__138_0 = HandleNewCombatRound(Rider)).
    // 상태는 정적 WeakKeyTable(유닛 키) — 인스턴스 필드 없음 → 세이브에 아무것도 들어가지 않는다(로드 직후는 「안 씀」).
    // 상태는 유닛의 것이지 버프 인스턴스의 것이 아니다: 지우는 곳은 새 라운드·전투 이탈뿐(참가는 fix round 2 R4 로 뺌). 버프 해제(토글 끔)로 지우지 않는다
    // (자유 행동 토글 껐다 켜기로 같은 라운드에 러시를 다시 쓰는 것을 막는다 — 컨트롤러 판정 fix round 1 ①).
    // 보조 경계(②): RTwP 에서 표준 행동을 안 쓰는 유닛(기회공격만)은 TickOnUnit 의 >0→≤0 전이가 없어 새 라운드 이벤트가 오지 않는다 →
    // 그 라운드 첫 소모 시각에서 1 라운드(엔진 Rounds(1).Seconds = 1 × 6 초, Rounds.get_Seconds IL 0000~000D)가 지나면 새 라운드로 본다(실시간·전투 밖만, 턴제는 제외). 이벤트 리셋이 1차다.
    // 시퀀스 중간에 이르게 풀리지 않음: 시퀀스 동안은 표준 쿨다운 > 0(SpendAction IL 00AE~00B6)이라 이벤트가 반드시 온다 → 대체 경계를 닫는다
     
    // (Task 6b: 기준 시각은 마지막이 아니라 그 라운드 「첫」 소모 시각 — 종전 1회 규칙에서는 둘이 같았다.)
    public class StandRushState : UnitFactComponentDelegate, IUnitNewCombatRoundHandler, IUnitCombatHandler
    {
        sealed class Box
        {
            public int Used;            // 이번 라운드에 스탠드 타격을 일으킨 본체 명중 수(10레벨부터는 공격 수 — Task 6d; 전력 공격 라운드 것은 세지 않는다)
            public TimeSpan FirstUsedAt; // 그 라운드 첫 소모의 게임 시각(TimeController.GameTime = Player.GameTime, get_GameTime IL 0000~000A). Used == 0 이면 의미 없음.
        }

        // 1 라운드 길이 = 엔진 Rounds(1).Seconds(6 초). 리터럴을 두지 않고 엔진에서 읽는다.
        static TimeSpan RoundLength { get { return new Rounds(1).Seconds; } }

        static readonly WeakKeyTable<UnitEntityData, Box> state = new WeakKeyTable<UnitEntityData, Box>();

        // 이번 본체 공격의 러시 계획(StandRules.PlanStrike — 순수, 오프라인 시험이 실행으로 고정)을 돌려주고 새 라운드 상태를 저장한다. plan.Allowed = false 면
        // 이번 라운드 타수를 다 썼거나 10레벨 미만의 빗나감이다(Task 6d). level = 주인의 현재 전사 레벨(StandOwner.Level), fullAttackRound = 주인에게 전력 공격 버프가 있는가
        // (제한 해제, 세지 않음 — 설계 §4.3), ownerHit = 이번 본체 공격의 명중 여부(ContextActionStandStrike.OwnerAttackHit — 트리거는 빗나감에도 온다).
        public static StandRules.RushPlan TryConsume(UnitEntityData owner, int level, bool fullAttackRound, bool ownerHit)
        {
            if (owner == null) throw new InvalidOperationException("PhantomBlade: StandRushState.TryConsume with a null owner");
            // 전투 안팎 같은 규칙(fix round 2 R4): 전투 참가는 공격 도중이 아니라 늦게(UnitCombatJoinController.TickUnit 0130) 일어나므로
            // 「전투 밖이면 매번 허용」+「참가 시 리셋」은 첫 시퀀스에 러시를 두 번 줬다 → 특례 없음. 전투 밖은 아래 시간 경계로만 풀린다.
            var game = Game.Instance;
            if (game == null || game.TimeController == null) throw new InvalidOperationException("PhantomBlade: StandRushState.TryConsume without Game.Instance.TimeController");
            TimeSpan now = game.TimeController.GameTime;
            var box = state.GetValue(owner, _ => new Box());
            // 턴제(fix round 2 R2): 경계는 그 유닛 턴 시작 이벤트(TurnController.Prepare 0226~0249) 하나뿐. 턴 지연(CombatController.HandleDelayTurn) 뒤엔
            // 게임 시각이 6 초 넘게 흘러 시간 경계가 먼저 열리고 턴 시작이 또 리셋 → 한 라운드 두 번. 그래서 턴제 전투 중엔 시간 경계를 쓰지 않는다(PlanStrike 의 turnBased).
             
            // 당겨도 시퀀스 도중에 열리지 않는다. 판정은 순수 StandRules.RoundEventPending. 전투 상태가 없으면 읽을 수 없다 → 던진다.
            var combat = owner.CombatState;
            if (combat == null || combat.Cooldown == null) throw new InvalidOperationException("PhantomBlade: StandRushState.TryConsume: owner " + owner.CharacterName + " has no CombatState.Cooldown (cannot tell whether a new-round event is pending)");
            bool turnBased = CombatController.IsInTurnBasedCombat();
            bool inCombat = owner.IsInCombat;
            float standardCooldown = combat.Cooldown.StandardAction;
            bool eventPending = StandRules.RoundEventPending(turnBased, inCombat, standardCooldown);
            var plan = StandRules.PlanStrike(box.Used, box.FirstUsedAt, now, RoundLength, turnBased, eventPending, level, fullAttackRound, ownerHit);
            box.Used = plan.UsedAfter;
            box.FirstUsedAt = plan.FirstUsedAtAfter;
            return plan;
        }

        public static void Reset(UnitEntityData owner)
        {
            if (owner == null) throw new InvalidOperationException("PhantomBlade: StandRushState.Reset with a null owner");
            state.Remove(owner);
        }

        public void HandleNewCombatRound(UnitEntityData unit)
        {
            // 정당한 무동작: 전역 이벤트라 모든 유닛의 새 라운드가 온다 — 이 버프 주인의 것만 처리.
            if (!ReferenceEquals(unit, Owner)) return;
            Reset(Owner);
        }

        // 정당한 무동작: 전투 참가는 늦게 온다(UnitCombatJoinController.TickUnit 0130) — 첫 시퀀스 도중 리셋하면 러시가 두 번(fix round 2 R4). 참가로 지우지 않는다.
        public void HandleUnitJoinCombat(UnitEntityData unit) { }

        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            // 정당한 무동작: 다른 유닛(UnitEntityData.LeaveCombat IL 00AC~00B9 가 그 유닛으로 전역 발행).
            if (!ReferenceEquals(unit, Owner)) return;
            Reset(Owner);
        }
    }
}
