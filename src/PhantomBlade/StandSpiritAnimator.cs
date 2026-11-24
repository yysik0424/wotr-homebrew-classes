using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.View.Animation;
using Kingmaker.Visual.Animation.Kingmaker;
using UnityEngine;
using WotrClassMod.Common;

namespace PhantomBlade
{
     
    // 언제: 주인이 개시한 RuleAttackWithWeapon 이 끝난 뒤(IInitiatorRulebookHandler.OnEventDidTrigger). 스탠드 주먹은 러시 심장
    //   ContextActionStandStrike.RunAction(StandStrike.cs)이 개시자 = 주인으로 발행하므로 여기로 온다 — 심장은 손대지 않는다.
    // 무엇: 주먹 명중(StandBlueprints.IsStandFist + SpiritRules.IsStandFistHit — 점검 기록기 Checks/StrikeLog 와 같은 두 함수, C4)마다
    //   같은 프레임 묶음(SpiritRules.BurstStep, 상한 MaxSwingsPerBurst) 안에서 령이 휘두르는 동작만 재생한다.
    //   재생 경로 = 본편 공격 명령과 같은 공개 API: AttackHandInfo.CreateAnimationHandleForAttack IL 009C~013D
    //   (2026-11-16 그림자 악마: 특수 공격 Claw — View.AnimationManager.CreateSpecialAttackHandle(Dials.SpiritSwingSpecialAttack), 자연 무기 공격과 같은 경로)
    //   → UnitCommand.StartAnimation IL 0024 의 AnimationManager.Execute(handle).
    // 령은 규칙·명령을 만들지 않는다(RuleAttackWithWeapon 생성·Rulebook.Trigger·UnitCommand 0 — 시험이 IL 로 잠금). 명령이 없으니 동작의 Act 이벤트가 부를 것도 없다.
     
    //   규칙 파이프라인으로 다시 던지지 않는다 — 동작 실패가 전투 판정을 끊지 않는다. 휘두름 조건 = 주먹 공격(명중·빗나감 모두, SpiritRules.SpiritSwingsOn*).
    // 이 블루프린트 컴포넌트 인스턴스는 모든 귀검사가 공유한다 → 인스턴스 필드 금지. 버스트 기록은 령 유닛 키의 정적 약한 표.
    // Task 6d(귀신 접촉): 10레벨부터 스탠드 타격은 RuleAttackWithWeapon 없는 독립 RuleAttackRoll(StandTouchStrike)이다 → 두 번째 처리기
    //   IInitiatorRulebookHandler<RuleAttackRoll> 가 그 굴림의 명중(SpiritRules.IsStandTouchHit — 무기 공격 안의 굴림은 제외)을 같은 Swing 으로 보낸다(S12-6 과 같은 규칙: 스탠드 명중마다 휘두름).
     
    // 2026-11-16(소유자 결정, 설계 §14.6): 귀신참은 제자리 1회 대신 「돌진」 — TryStartDash 가 SpiritFollower.BeginDash 로 시작하고, 도착 뒤 휘두름(발 수만큼)은 DashSwing(= 같은 Swing).
    //   돌진할 수 없을 때만 종전 제자리 휘두름(명시적 분기).
    public class StandSpiritAnimator : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>, IInitiatorRulebookHandler<RuleAttackRoll>, IInitiatorRulebookHandler<RuleCastSpell>
    {
        sealed class Burst
        {
            public int Frame = SpiritRules.NoFrame;
            public int Hits;
        }

        static readonly WeakKeyTable<UnitEntityData, Burst> bursts = new WeakKeyTable<UnitEntityData, Burst>();
        static int playedCount;
        static UnitAnimationActionHandle lastHandle;
        static string lastSpecialAttack;










        // Execute 직후 아직 시작 안 된(순서 대기) 휘두름 수.
        static int queuedCount;
        public static int QueuedCount { get { return queuedCount; } }

        // 정당한 무동작: 명중은 DidTrigger 에서야 정해진다(RuleAttackWithWeapon.OnTrigger IL 008D set_AttackRoll → 00B1 Trigger).
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            UnitEntityData owner = null;
            try
            {
                owner = Owner;
                if ((object)owner == null) throw new InvalidOperationException("no Owner in the component event context");
                if (evt == null) throw new ArgumentNullException("evt");
                // RulebookEventBus.OnEventDidTrigger IL 0052~0073: 개시자 처리기는 evt.Initiator 의 구독자에게만 간다 → 다르면 배선 오류.
                if (!ReferenceEquals(evt.Initiator, owner)) throw new InvalidOperationException("initiator handler got an attack by " + (evt.Initiator == null ? "null" : evt.Initiator.CharacterName));
                // RuleAttackWithWeapon.OnTrigger IL 005A~005F 가 Weapon.Blueprint 를 무조건 읽는다 → DidTrigger 에서 무기·블루프린트가 없으면 깨진 규칙.
                if (evt.Weapon == null) throw new InvalidOperationException("attack without a weapon");
                var bp = evt.Weapon.Blueprint;
                if (bp == null) throw new InvalidOperationException("attack weapon has no blueprint");
                bool isFist = StandBlueprints.IsStandFist(bp);
                // 굴림이 없으면 null 로 넘긴다 → IsStandFistHit 가 던진다(OnTrigger IL 0025~008D 가 굴림을 무조건 만들므로 null 은 깨진 규칙, 「빗나감」이 아님).
                var roll = evt.AttackRoll;
                bool? rolledHit = roll == null ? (bool?)null : roll.IsHit;
                 
                if (!SpiritRules.SpiritSwingsOnFist(isFist, rolledHit)) return;
                Swing(owner, evt.Target, true);
            }
            catch (Exception ex)
            {
                 
                Failed(owner, "rush fist swing", ex);
            }
        }

        // 정당한 무동작: 명중은 DidTrigger 에서야 정해진다.
        public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

        // Task 6d: 스탠드 자기 접촉 굴림(독립 RuleAttackRoll, 개시자 = 주인). 무기 공격 안의 굴림(본체 공격·6b 얹힘 주먹)은 위 무기 처리기가 센다 → IsStandTouchHit 가 거른다.
        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            UnitEntityData owner = null;
            try
            {
                owner = Owner;
                if ((object)owner == null) throw new InvalidOperationException("no Owner in the component event context");
                if (evt == null) throw new ArgumentNullException("evt");
                if (!ReferenceEquals(evt.Initiator, owner)) throw new InvalidOperationException("initiator handler got a roll by " + (evt.Initiator == null ? "null" : evt.Initiator.CharacterName));
                // RuleAttackRoll 생성자가 무기를 무조건 읽는다(.ctor IL 003B~0040 Weapon.Blueprint.AttackType) → 무기 없으면 깨진 규칙.
                var weapon = evt.Weapon;
                if (weapon == null || weapon.Blueprint == null) throw new InvalidOperationException("attack roll without a weapon blueprint");
                bool isFist = StandBlueprints.IsStandFist(weapon.Blueprint);
                bool partOfWeaponAttack = evt.RuleAttackWithWeapon != null;
                // 정당한 무동작: 스탠드 접촉 굴림이 아님(주인의 무기 공격 굴림·귀신참 광선). 빗나가도 휘두른다(판정 2026-11-15, SpiritSwingsOnTouch).
                if (!SpiritRules.SpiritSwingsOnTouch(isFist, partOfWeaponAttack, evt.IsHit)) return;
                Swing(owner, evt.Target, true);
            }
            catch (Exception ex)
            {
                Failed(owner, "stand touch swing", ex);
            }
        }

        // 정당한 무동작: 성공 여부(Success)는 DidTrigger 에서야 정해진다(RuleCastSpell.OnTrigger IL 005A/0239 set_Success).
        public void OnEventAboutToTrigger(RuleCastSpell evt) { }

         
        //   (new RuleCastSpell(Ability, Target) → Rulebook.Trigger; 개시자 = Spell.Caster.Unit — RuleCastSpell..ctor IL 0002~000C) → 주인의 개시자 처리기.
        // 판정은 순수 SpiritRules.CastSwingFor(시험 실행), 재생은 주먹과 같은 Swing(버스트 상한·령 부재 판정 공유 — C4).
         
        //   다시 던지지 않는다 — 던지면 엔진이 시전마다 PFLog.Exception 을 남긴다(RulebookSubscribersList catch). 실패가 시전 규칙을 바꾸지는 않는다.
        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            UnitEntityData owner = null;
            try
            {
                owner = Owner;
                if ((object)owner == null) throw new InvalidOperationException("no Owner in the component event context");
                if (evt == null) throw new ArgumentNullException("evt");
                if (!ReferenceEquals(evt.Initiator, owner)) throw new InvalidOperationException("initiator handler got a cast by " + (evt.Initiator == null ? "null" : evt.Initiator.CharacterName));
                // RuleCastSpell..ctor IL 0013 이 Spell 을 무조건 담고 IL 0023 이 Spell.CreateExecutionContext 를 부른다 → DidTrigger 에서 null 은 깨진 규칙.
                if (evt.Spell == null || evt.Spell.Blueprint == null) throw new InvalidOperationException("cast without an ability blueprint");
                var bp = evt.Spell.Blueprint;
                if (StandBlueprints.GhostSlashAbility == null || StandBlueprints.FullAttackAbility == null) throw new InvalidOperationException("stand abilities are not built");
                var t = evt.SpellTarget;
                if (t == null) throw new InvalidOperationException("cast without a target wrapper");
                var unit = t.Unit;
                var decision = SpiritRules.CastSwingFor(bp == StandBlueprints.GhostSlashAbility, bp == StandBlueprints.FullAttackAbility, evt.Success, (object)unit != null, ReferenceEquals(unit, owner));
                // 정당한 무동작: 우리 능력이 아니거나 시전 실패.
                if (decision == SpiritRules.CastSwing.None) return;
                // 소유자 결정 2026-11-16(설계 §14.6): 귀신참이면 령이 대상 앞으로 돌진해 발 수만큼 휘두르고 돌아온다 — 휘두름은 돌진이 도착한 뒤 따라가기가 낸다(DashSwing).
                //   돌진할 수 없으면(TryStartDash 가 사유를 한 줄 기록하고 false) 종전대로 제자리에서 한 번 휘두른다 — 명시적 분기.
                if (decision == SpiritRules.CastSwing.FaceTarget && TryStartDash(owner, unit))
                {
                    castDashes++;
                    return;
                }
                Swing(owner, unit, decision == SpiritRules.CastSwing.FaceTarget);
                castSwings++;
            }
            catch (Exception ex)
            {
                Failed(owner, "cast swing", ex);
            }
        }

         
        //   그 주인의 첫 실패만 ERROR(예외 전체, S11 이 센다), 이후는 횟수만. 다시 던지지 않는다 — 동작 실패가 전투 판정(규칙 파이프라인)을 끊지 않게.
        //   주인이 null(배선 결함)이면 표에 넣을 키가 없어 매번 기록한다(그 자체가 결함이라 숨기지 않는다).
        static void Failed(UnitEntityData owner, string what, Exception ex)
        {
            swingFailures++;
            if ((object)owner != null && failLogged.TryGetValue(owner, out _)) return;
            if ((object)owner != null) failLogged.Add(owner, new object());
            ModContext.LogError("PhantomBlade: StandSpiritAnimator could not start the spirit's " + what + " for " + ((object)owner == null ? "?" : owner.CharacterName)
                + " (ERROR once per unit; later failures are only counted in StandSpiritAnimator.SwingFailures, now " + swingFailures + "): " + ex);
        }

        // 귀신참 돌진 시작(소유자 결정 2026-11-16, 설계 §14.6). true = 돌진을 시작했다(휘두름은 따라가기가 도착 뒤 DashSwing 으로 낸다).
        //   false = 돌진하지 않는다 → 호출자가 종전의 제자리 휘두름(Swing)으로 간다. 사유는 여기서 한 줄 기록(조용한 폴백 아님):
        //     ① 대상이 사라지는 중·이미 쓰러짐·게임 밖(순수 SpiritRules.DashStartFor) ② 령이 없음(부재 판정·기록은 Swing 이 한다 — 설명되면 INFO, 아니면 던짐).
        //   판독 실패(대상 상태 없음·령 뷰·따라가기 없음·레벨 판독 실패)는 던진다 → 시전 처리기의 catch → 공용 Failed.
        //   발 수 = GhostSlash.Projectiles(주인 레벨) — 광선 수와 같은 원장(C4). 규칙·명령은 만들지 않는다.
        static bool TryStartDash(UnitEntityData owner, UnitEntityData target)
        {
            if ((object)target == null) throw new InvalidOperationException("ghost slash dash without a target unit");
            var state = target.State;
            if (state == null) throw new InvalidOperationException("ghost slash target " + target.CharacterName + " has no State (cannot tell whether it is down)");
            var start = SpiritRules.DashStartFor(StandSpirit.Going(target), state.IsDead, target.IsInGame ? SpiritRules.Tri.Yes : SpiritRules.Tri.No);
            if (start == SpiritRules.DashStart.SwingInPlace)
            {
                ModContext.Log("PhantomBlade: StandSpiritAnimator: the spirit of " + owner.CharacterName + " swings in place for this ghost slash (no dash: target " + target.CharacterName
                    + " is " + (StandSpirit.Going(target) ? "being removed" : state.IsDead ? "already down" : "not in the game") + ")");
                return false;
            }
            if (start != SpiritRules.DashStart.Dash) throw new InvalidOperationException("unknown dash start " + start);
            var spirit = StandSpirit.Get(owner);
            if ((object)spirit == null)
            {
                ModContext.Log("PhantomBlade: StandSpiritAnimator: the spirit of " + owner.CharacterName + " swings in place for this ghost slash (no dash: no spirit right now — Swing judges the absence)");
                return false;
            }
            var view = spirit.View;
            if (view == null) throw new InvalidOperationException("spirit has no view");
            var follower = StandSpirit.Follower(spirit);
            follower.BeginDash(target, GhostSlash.Projectiles(StandOwner.Level(owner, "ghost slash dash", GhostSlash.FirstLevel)));
            return true;
        }

        // 돌진 중 휘두름 1회(SpiritFollower.DashPlace 가 머묾 단계에서 부른다). 재생은 공용 Swing 그대로(령 조회·버스트 상한·Claw 동작 — C4), 실패는 공용 Failed.
        //   true = 재생됨, false = 이번엔 재생 안 됨(같은 프레임 버스트 상한에 걸렸거나 령 부재가 설명됨 — 따라가기가 다음 프레임에 다시 부른다), null = 실패(보고됨, 돌진 중단).
        internal static bool? DashSwing(UnitEntityData owner, UnitEntityData target)
        {
            try
            {
                int before = playedCount;
                Swing(owner, target, true);
                bool played = playedCount > before;
                if (played) dashSwings++;
                return played;
            }
            catch (Exception ex)
            {
                Failed(owner, "ghost slash dash swing", ex);
                return null;
            }
        }

        static readonly WeakKeyTable<UnitEntityData, object> failLogged = new WeakKeyTable<UnitEntityData, object>();
        static int castSwings, swingFailures, castDashes, dashSwings;



        public static int DashSwings { get { return dashSwings; } }
        // 점검용 읽기 전용: 시전으로 시작한 휘두름 요청 수(버스트 상한으로 재생이 빠질 수 있음), 휘두름 실패 수(세 처리기 합, 프로세스 누적).
        public static int CastSwings { get { return castSwings; } }




        // 스탠드 명중·시전 하나 → 령 조회 → 버스트(같은 프레임 상한) → 재생. 세 처리기가 이것 하나를 부른다(C4: 부재 판정·상한 중복 없음).
        // faceTarget = 대상 쪽을 볼지(주먹·귀신참 = 참, 전력 공격 = 거짓 — 대상이 자신).
        static void Swing(UnitEntityData owner, UnitEntityData target, bool faceTarget)
        {
            var spirit = StandSpirit.Get(owner);
            if ((object)spirit == null)
            {
                // 이 처리기는 스탠드 버프가 켜져 구독 중일 때만 불린다(EntityFactComponent.TurnOn IL 0019 Subscribe) → 「토글 꺼짐」 부재는 여기 올 수 없다.
                // 공격한 주인은 의식·게임 안·지역 로드 상태다 → 남는 정당한 부재는 엔진이 주인을 숨긴 경우뿐(수명 컴포넌트와 같은 입력 함수, SpiritRules 주석).
                if (SpiritRules.SpiritAbsenceExplained(StandSpiritLifecycle.OwnerHiddenByEngine()))
                {
                    ModContext.Log("PhantomBlade: StandSpiritAnimator skipped a swing for " + owner.CharacterName + " (no spirit: the engine hides the owner — cutscene lock or global map)");
                    return;
                }
                throw new InvalidOperationException("stand fist hit while the stand buff is on, but the owner has no live spirit (StandSpirit.Get == null)");
            }

            var b = bursts.GetValue(spirit, _ => new Burst());
            var step = SpiritRules.BurstStep(b.Frame, b.Hits, Time.frameCount);
            b.Frame = step.frame;
            b.Hits = step.hits;
            // 정당한 무동작: 이 버스트에서 이미 MaxSwingsPerBurst 번 휘둘렀다(상한).
            if (!step.play) return;
            Play(spirit, target, faceTarget);
        }

        // (faceTarget 면) 대상 쪽을 보고(정찰 2(e) ForceLookAt) → 그 방향을 따라가기가 덮지 않게 유지 → 핸들 생성·스타일·실행. 어느 하나라도 없으면 던진다(빈 동작 금지).
        // ForceLookAt 은 방향(Orientation·transform.rotation)만 쓴다 — 이동 에이전트를 건드리지 않는다(UnitEntityData.ForceLookAt IL 0003~0047) → 걷기 복사(AgentReset)와 무관.
        static void Play(UnitEntityData spirit, UnitEntityData target, bool faceTarget)
        {
            if (faceTarget && (object)target == null) throw new InvalidOperationException("spirit swing should face its target, but there is none");
            var view = spirit.View;
            if (view == null) throw new InvalidOperationException("spirit has no view");
            var follower = StandSpirit.Follower(spirit);
            var am = view.AnimationManager;
            if (am == null) throw new InvalidOperationException("spirit view has no AnimationManager");
            // 2026-11-16 모델 = 그림자 악마: 동작 세트에 MainHandAttack 이 없고 SpecialAttack(Bite·Claw)만 있다(정찰 §1.3). 본편 자연 무기 공격과 같은 경로
            //   (AttackHandInfo::GetAnimationType IL 001A~0037 특수 애니메이션 → CreateSpecialAttackHandle). 동작이 없으면 엔진은 오류 줄만 남기고 null 을 돌려준다
            //   (CreateSpecialAttackHandle IL 0011~0038) → 여기서 미리 GetAction 으로 단언하고, null 핸들도 던진다(조용히 빈 휘두름 금지 — 정찰 §0-D).
            var special = SwingSpecialAttack();
            if (am.GetAction(special) == null) throw new InvalidOperationException("spirit model has no SpecialAttack " + special + " animation (Dials.SpiritSwingSpecialAttack)");

            if (faceTarget)
            {
                spirit.ForceLookAt(target.Position);
                follower.HoldFacingUntil((float)SpiritRules.FacingHoldEnd(Time.unscaledTime));
            }
            var h = am.CreateSpecialAttackHandle(special);
            if (h == null) throw new InvalidOperationException("UnitAnimationManager.CreateSpecialAttackHandle(" + special + ") returned null");
            am.Execute(h);
            // Execute 는 거부해도 던지지 않고 Debug.LogError 만 한다(AnimationManager.Execute IL 0034~0063: 다른 관리자 핸들·이미 시작) — 새 핸들이라 거기 갈 수 없다.
            //   시작 안 됨이 남는 정상 경우 = 현재 동작 뒤 순서 대기(IL 018C~0193 Enqueue). 그것은 숨기지 않고 센다(점검·눈 점검이 읽는다).
            if (!h.IsStarted) queuedCount++;
            playedCount++;
            lastHandle = h;
            lastSpecialAttack = special.ToString();
        }

        // 다이얼 이름 → 게임 열거값. 모르는 이름이면 던진다(Enum.Parse).
        static UnitAnimationSpecialAttackType SwingSpecialAttack()
        {
            return (UnitAnimationSpecialAttackType)Enum.Parse(typeof(UnitAnimationSpecialAttackType), SpiritRules.Dials.SpiritSwingSpecialAttack);
        }

    }
}
