using System;
using System.Runtime.CompilerServices;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using WotrClassMod.Common;

namespace PhantomBlade
{
     
    // 엔진 근거(Assembly-CSharp SHA-256 2CB7160B…, dnlib 2026-11-09):
    //   - RuleCalculateAttackBonusWithoutTarget 의 능력치 = 생성자에서 무기 블루프린트 값(.ctor IL 0015~002B), 단 OnTrigger 가 ContextData<AttackBonusStatReplacement>.Current 가
    //     있으면 비교 없이 그 능력치로 덮는다(IL 003E~0054). 개시자·무기 필터가 없다.
    //   - 종전(6d) 접촉 굴림은 굴림 전체를 문맥으로 감싸서, 같은 굴림 안에서 엔진이 계산하는 적의 받아넘기기(RuleAttackRoll.OnTrigger IL 0547 → ParryData.Trigger
    //     → 적 개시 RuleCalculateAttackBonus)까지 매력으로 바꿨다(I3). 귀신참은 엔진이 문맥을 열되 Living Grimoire(DomainsByIntelligenceFeature)면 지능으로 연다
    //     (<Deliver>d__36 IL 0447~0453, 능력 필터 없음 — I4).
    // 해법(하모니 없음): 주인 개시 RuleCalculateAttackBonusWithoutTarget 처리기에서, 그 계산이 스탠드 접촉 굴림·귀신참 굴림 「자신」의 것일 때만(같은 개시자 ∧ 같은 무기 인스턴스,
    //   판정 = 순수 StandRules.ChaScopeFor) AboutToTrigger 에서 매력 문맥을 열고 DidTrigger 에서 닫는다. 귀신참이면 엔진의 지능 문맥 위에 매력이 놓인다(ContextData 는 인덱스 스택 —
    //   Request ++ / Dispose --, LIFO). 받아넘기기 계산은 개시자가 적이라 열지 않는다.
    //   - 바깥 굴림 = 규칙 스택(Rulebook.CurrentContext.EventStack — Rulebook.TriggerEvent IL 003E PushEvent 가 처리기보다 먼저)의 가장 안쪽 RuleAttackRoll.
    //   - 짝: TriggerEventInternal 은 단계마다 SaveExecute(예외 기록 후 계속)라 OnTrigger 가 던져도 DidTrigger 가 온다 → 연 문맥은 같은 규칙에서 닫힌다.
    //   - 되읽기: 굴림 뒤 RequireApplied(roll) — 공격 보너스를 계산했으면 정확히 1번 열고 닫아 최종 능력치가 매력인지(StandRules.RequireChaApplied). 처리기가 없으면(버프 꺼짐 등) 던진다.
    // 열린 문맥은 DidTrigger가 제거하고, 굴림 기록은 규칙 자신의 CustomData에 둔다 — 인스턴스 필드 금지(블루프린트 컴포넌트는 모든 귀검사가 공유), 세이브에 들어가지 않는다.
    public class StandAttackStatScope : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
        sealed class OpenScope { public AttackBonusStatReplacement Data; public RuleAttackRoll Roll; }
        sealed class Applied { public int Opened, Closed; public StatType Final; }
        sealed class Marker { }

        static readonly ConditionalWeakTable<RuleCalculateAttackBonusWithoutTarget, OpenScope> open = new ConditionalWeakTable<RuleCalculateAttackBonusWithoutTarget, OpenScope>();
        static readonly RulebookEvent.CustomDataKey appliedKey = new RulebookEvent.CustomDataKey(nameof(StandAttackStatScope) + "." + nameof(appliedKey));
        static readonly RulebookEvent.CustomDataKey touchKey = new RulebookEvent.CustomDataKey(nameof(StandAttackStatScope) + "." + nameof(touchKey));
        static readonly WeakKeyTable<UnitEntityData, object> intelligenceNoticed = new WeakKeyTable<UnitEntityData, object>();

        // StandTouchStrike 가 굴리기 직전에 부른다: 이 굴림이 스탠드 접촉 굴림임을 표시(같은 굴림을 두 번 표시하면 배선 오류).
        internal static void MarkStandTouch(RuleAttackRoll roll)
        {
            if (roll == null) throw new ArgumentNullException("roll");
            Marker m;
            if (roll.TryGetCustomData(touchKey, out m)) throw new InvalidOperationException("PhantomBlade: stand touch roll marked twice");
            roll.SetCustomData(touchKey, new Marker());
        }

        // 굴림 뒤 되읽기(스탠드 접촉: StandTouchStrike, 귀신참: ContextActionGhostSlashDamage). 기록 없음 = 한 번도 열지 않음(0/0) — 정당한 값이며
        // 계산했는데 0 이면 RequireChaApplied 가 던진다(결측을 정상으로 읽지 않음).
        internal static void RequireApplied(RuleAttackRoll roll, string who)
        {
            if (roll == null) throw new InvalidOperationException("PhantomBlade: " + who + ": no attack roll to check the Charisma scope of");
            Applied a;
            bool has = roll.TryGetCustomData(appliedKey, out a);
            try
            {
                StandRules.RequireChaApplied(roll.AttackBonusRule != null, has ? a.Opened : 0, has ? a.Closed : 0, has && a.Final == StatType.Charisma);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException("PhantomBlade: " + who + ": " + e.Message + (has ? " (final stat " + a.Final + ")" : " (no scope record)"), e);
            }
        }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            UnitEntityData owner = null;
            try
            {
                owner = Owner;
                if ((object)owner == null) throw new InvalidOperationException("no Owner in the component event context");
                if (evt == null) throw new ArgumentNullException("evt");
                var roll = EnclosingRoll();
                bool inRoll = roll != null;
                var scope = StandRules.ChaScopeFor(inRoll, inRoll && IsStandTouch(roll), inRoll && IsGhostSlash(roll),
                    inRoll && ReferenceEquals(evt.Initiator, roll.Initiator), inRoll && ReferenceEquals(evt.Weapon, roll.Weapon));
                if (scope == StandRules.ChaScope.None) return; // 정당한 무동작: 스탠드·귀신참 굴림 자신의 계산이 아니다(굴림 밖 미리보기·다른 굴림·받아넘기기)
                OpenScope o;
                if (open.TryGetValue(evt, out o)) throw new InvalidOperationException("Charisma scope opened twice for one attack bonus rule");
                var before = ContextData<AttackBonusStatReplacement>.Current; // null = 바깥 문맥 없음(스탠드 접촉) — 정당
                var data = ContextData<AttackBonusStatReplacement>.Request().Setup(StatType.Charisma);
                open.Add(evt, new OpenScope { Data = data, Roll = roll });
                Applied a;
                if (!roll.TryGetCustomData(appliedKey, out a))
                {
                    a = new Applied();
                    roll.SetCustomData(appliedKey, a);
                }
                a.Opened++;
                if (scope == StandRules.ChaScope.GhostSlash && before != null && before.Stat != StatType.Charisma) NoticeOverride(owner, before.Stat);
            }
            catch (Exception ex)
            {
                ModContext.LogError("PhantomBlade: StandAttackStatScope (open) failed for " + ((object)owner == null ? "?" : owner.CharacterName) + ": " + ex);
                throw;
            }
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            UnitEntityData owner = null;
            try
            {
                owner = Owner;
                if (evt == null) throw new ArgumentNullException("evt");
                OpenScope o;
                if (!open.TryGetValue(evt, out o)) return; // 정당한 무동작: 이 계산에는 범위를 열지 않았다
                open.Remove(evt);
                // 인덱스 스택이라 맨 위가 우리 것이 아니면 Dispose 가 남의 문맥을 닫는다 → 닫지 않고 던진다(문맥 누수 — 되읽기가 Closed 0 으로 다시 잡는다).
                if (!ReferenceEquals(ContextData<AttackBonusStatReplacement>.Current, o.Data)) throw new InvalidOperationException("Charisma scope is not on top of ContextData<AttackBonusStatReplacement> (stack order broken) — not closed");
                o.Data.Dispose();
                Applied a;
                if (!o.Roll.TryGetCustomData(appliedKey, out a)) throw new InvalidOperationException("Charisma scope closed without an open record");
                a.Closed++;
                a.Final = evt.AttackBonusStat;
            }
            catch (Exception ex)
            {
                ModContext.LogError("PhantomBlade: StandAttackStatScope (close) failed for " + ((object)owner == null ? "?" : owner.CharacterName) + ": " + ex);
                throw;
            }
        }

        // 가장 안쪽 RuleAttackRoll(규칙 스택은 PushEvent = List.Add 순서 → 마지막이 가장 안쪽). 없음 = 굴림 밖의 계산(정당). 문맥 자체가 없으면 규칙 처리기 안이 아니다 → 던진다.
        static RuleAttackRoll EnclosingRoll()
        {
            var ctx = Rulebook.CurrentContext;
            if (ctx == null) throw new InvalidOperationException("no Rulebook.CurrentContext inside a rule handler");
            var stack = ctx.EventStack;
            if (stack == null) throw new InvalidOperationException("Rulebook.CurrentContext.EventStack is null");
            return StandRules.InnermostOf<RuleAttackRoll>(stack); // 선택 규칙은 순수 함수(시험이 실행)
        }

        static bool IsStandTouch(RuleAttackRoll roll)
        {
            Marker m;
            return roll.TryGetCustomData(touchKey, out m);
        }

        // 귀신참 굴림 = 원인 문맥의 능력이 귀신참(<Deliver>d__36 IL 0533 MechanicsContext.TriggerRule → Reason = 능력 실행 문맥, TriggerRule IL 0000~0019).
        // Reason·Context 없음 = 능력에서 온 굴림이 아니다 — 정당한 「아님」.
        static bool IsGhostSlash(RuleAttackRoll roll)
        {
            var gs = StandBlueprints.GhostSlashAbility;
            if (gs == null) throw new InvalidOperationException("StandBlueprints.GhostSlashAbility is null (blueprints not built)");
            var reason = roll.Reason;
            if (reason == null) return false;
            var ctx = reason.Context;
            if (ctx == null) return false;
            return ReferenceEquals(ctx.SourceAbility, gs);
        }

        // Living Grimoire 등으로 엔진이 귀신참에 다른 능력치(지능)를 열었고 이 범위가 매력으로 덮었다 — 유닛마다 한 번 알린다(정보, 게임 안 확인용).
        static void NoticeOverride(UnitEntityData owner, StatType engineStat)
        {
            object seen;
            if (intelligenceNoticed.TryGetValue(owner, out seen)) return; // 정당한 무동작: 이미 알렸다
            intelligenceNoticed.Add(owner, new object());
            ModContext.Log("PhantomBlade: NOTICE ghost slash of " + owner.CharacterName + ": the engine opened " + engineStat + " for the ray attack (e.g. Living Grimoire DomainsByIntelligence); the stand scope uses Charisma for the ray's own attack bonus");
        }
    }
}
