using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using WotrClassMod.Common;

namespace PhantomBlade
{
    // 스탠드 흡수(설계 §4.4): 스탠드가 켜져 있는 동안(StandBuff 컴포넌트) 본체가 받는 피해의 25%를 흡수 풀이 대신 받는다.
    // 훅 위치(컨트롤러 판정 Y, 2026-11-07):
    // - 계산·감산 = ITargetRulebookHandler<RuleCalculateDamage>.OnEventDidTrigger. RuleCalculateDamage.OnTrigger 의 IUnitApplyDamageReduction 이벤트(IL 006D)에서는
    //   UnitPartDamageReduction(DR·저항·경도)이 구독 순서대로 같은 이벤트에서 감산하므로 그 뒤에 오는 DR 을 못 본다. DidTrigger 는 OnTrigger 뒤에
    //   전역→대상→개시자 순으로 돈다(RulebookEventBus.OnEventDidTrigger IL) → 모든 감산이 끝난 FinalValue 를 읽는다.
    // - 감산 = BaseDamage.SetMaximum(본편 AddIncomingDamageMaximum.ApplyDamageReduction IL 0027 과 같은 API). FinalValue = max(0, min(MaximumValue, VWR − Reduction))
    //   (DamageValue.get_FinalValue IL) 이라 상한은 IgnoreReduction·TacticalCombatDRModifier 에 영향받지 않는다.
    // - 풀 소모 = ITargetRulebookHandler<RuleDealDamage>.OnEventDidTrigger, !IsFake 일 때만. RuleDealDamage.OnTrigger 는 IsFake 여도 계산 규칙을 돌리고(IL 0026→0073)
    //   HP 반영만 건너뛴다(IL 022B) → 계산 단계에서 소모하면 미리보기가 풀을 깎는다.
    // - 기억 = 규칙 인스턴스(RuleDealDamage) 키의 ConditionalWeakTable. 같은 규칙이 두 번 계산되면 throw(조용한 덮어쓰기 금지).
    public class StandAbsorb : UnitFactComponentDelegate, ITargetRulebookHandler<RuleCalculateDamage>, ITargetRulebookHandler<RuleDealDamage>
    {
        sealed class Pending
        {
            public int Absorbed;
            public int ExpectedFinal; // 계산 시점 상한 적용 뒤 Σ FinalValue (= incoming − absorb). 뒤늦은 후처리 검출용(값 비교).
        }

        static readonly ConditionalWeakTable<RuleDealDamage, Pending> pending = new ConditionalWeakTable<RuleDealDamage, Pending>();

        // 머리 위 글 지속 시간 = 엔진 기본(-1). 출처: 본편 InteractionRestrictionPart.ShowSuccessBark IL 0011 `ldc.r4 -1` → UIAccess.Bark(…, -1).
        const float BarkDurationEngineDefault = -1f;

        // 정당한 무동작: 감산은 아직 적용 전이라 여기서는 읽을 것이 없다(위 주석 — 계산은 DidTrigger).
        public void OnEventAboutToTrigger(RuleCalculateDamage evt) { }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            // RulebookEventBus 는 대상 핸들러를 GetRuleTarget() 의 구독자에게만 준다 → 다른 대상이면 배선 오류다.
            if (evt.Target != Owner) throw new InvalidOperationException("PhantomBlade: StandAbsorb got RuleCalculateDamage for " + (evt.Target == null ? "null" : evt.Target.CharacterName) + ", owner is " + (Owner == null ? "null" : Owner.CharacterName));
            var parent = evt.ParentRule;
            if (parent == null) throw new InvalidOperationException("PhantomBlade: StandAbsorb got RuleCalculateDamage without ParentRule");
            // legitimate no-op: preview/precalc (UnitHelper.CalculateAbilityDamage IL 0007~0009 sets parent IsFake=true, 0042~0048 triggers only
             
            // 순서: 중복 계산 throw 보다 먼저 — 사전 계산이 기억을 남기면 실계산이 그 throw 에 걸린다. 미리보기(IsFake) 피해 숫자는 흡수 전 값으로 보인다.
            if (parent.IsFake) return;
            Pending existing;
            if (pending.TryGetValue(parent, out existing)) throw new InvalidOperationException("PhantomBlade: StandAbsorb saw a second damage calculation for the same RuleDealDamage before it was dealt");
            // legitimate no-op: engine discards caps (OnTrigger 01AE-01BC) — IgnoreDamageReduction 이면 Result = DamageWithoutReduction 이라 상한이 무시된다.
            if (parent.IgnoreDamageReduction) return;

            int level = StandOwner.Level(Owner, "StandAbsorb", StandRules.StandLevel);
            var res = StandOwner.Pool(Owner, "StandAbsorb");
            int pool = Owner.Resources.GetResourceAmount(res);
            var list = evt.CalculatedDamage;
            if (list == null) throw new InvalidOperationException("PhantomBlade: StandAbsorb got RuleCalculateDamage.CalculatedDamage == null");
            var finals = list.Select(d => d.FinalValue).ToArray();
            int incoming = finals.Sum();
            int absorb = StandRules.AbsorbAmount(incoming, StandRules.AbsorbPercent(level), pool);
            // 정당한 무동작(감산 없음): 흡수량 0(받은 피해 0·내림 0·풀 0) — 깎을 것이 없다. 기억은 남긴다(뒤늦은 피해 검출).
            if (absorb == 0) { pending.Add(parent, new Pending { Absorbed = 0, ExpectedFinal = incoming }); return; }

            // 원천별 비례 배분(내림) 뒤 남은 몫을 앞에서부터 1씩(각 원천의 FinalValue 를 넘지 않게). Σshare == absorb ≤ incoming = ΣFinalValue 이므로 항상 채워진다.
            var share = new int[finals.Length];
            int given = 0;
            for (int i = 0; i < finals.Length; i++) { share[i] = (int)((long)absorb * finals[i] / incoming); given += share[i]; }
            for (int i = 0; given < absorb && i < finals.Length; i++)
                if (share[i] < finals[i]) { share[i]++; given++; }
            if (given != absorb) throw new InvalidOperationException("PhantomBlade: StandAbsorb could not distribute " + absorb + " over " + incoming);
            for (int i = 0; i < finals.Length; i++)
                if (share[i] > 0) list[i].Source.SetMaximum(finals[i] - share[i]);
            // 되읽기: 같은 BaseDamage 가 두 번 들어 있거나 상한이 먹지 않으면 합이 어긋난다 → throw.
            int after = list.Sum(d => d.FinalValue);
            if (after != incoming - absorb) throw new InvalidOperationException("PhantomBlade: StandAbsorb readback: damage " + incoming + " - absorb " + absorb + " expected " + (incoming - absorb) + ", got " + after);
            // 배분·되읽기가 성공한 뒤에만 기억한다: 위에서 던지면 Pending 이 없어 DealDamage DidTrigger 가 일어나지 않은 감산을 소모하지 않는다.
            pending.Add(parent, new Pending { Absorbed = absorb, ExpectedFinal = after });
        }

        // 정당한 무동작: 규칙 시작 전에는 계산이 없다.
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            Pending p;
            // 정당한 무동작: 이 규칙은 계산 단계를 밟지 않았다(IgnoreDamageReduction, 또는 OnTrigger IL 0012/0046 의 조기 반환).
            if (!pending.TryGetValue(evt, out p)) return;
            pending.Remove(evt);
            // 정당한 무동작: 미리보기(IsFake)는 HP 를 건드리지 않는다(OnTrigger IL 022B) → 풀도 건드리지 않는다.
            if (evt.IsFake) return;

            // 판정 기본값 3: 계산 뒤 후처리(예: 개시자 AddOutgoingDamageBonus.OnEventDidTrigger IL 00B8~00EF 가 같은 Source 로 DamageValue 를 다시 넣음)로
            // 최종 피해가 바뀌면 그 차이는 흡수되지 않는다 → 값 비교로 검출해 UMM 로그로 드러낸다(전투 로그 도배 회피). Source 동일성으로는 못 잡는다(같은 Source 재사용).
            var result = evt.ResultList;
            if (result == null) throw new InvalidOperationException("PhantomBlade: StandAbsorb got RuleDealDamage.ResultList == null after calculation");
             
            // ApplyDifficultyModifiers) 적용 전 단위다. ExpectedFinal 도 같은 단위(계산 시점)라 이 비교는 일관된다. 풀 차감도 이 단위라
            // 「파티 피해 배율 ≠ 1」이면 실제 HP 에서 막은 양 ≠ 풀 소모(README 알려진 한계). 기본 판정 2(보통 난이도 수치) 유지.
            int finalNow = 0;
            foreach (var d in result) finalNow += d.FinalValue;
            if (finalNow != p.ExpectedFinal)
                ModContext.Log("StandAbsorb: final damage changed after the absorb calculation (expected " + p.ExpectedFinal + ", now " + finalNow + ", difference " + (finalNow - p.ExpectedFinal) + " not absorbed) (" + (Owner == null ? "?" : Owner.CharacterName) + ")");

            // 정당한 무동작: 흡수량 0 이면 소모할 것이 없다.
            if (p.Absorbed == 0) return;
            var res = StandOwner.Pool(Owner, "StandAbsorb");
            int have = Owner.Resources.GetResourceAmount(res);
            // Spend 는 모자라면 오류 로그 후 남은 만큼만 깎는다(Spend IL 004F~00A8) → 계산과 소모 사이에 풀이 줄었으면 조용히 어긋나므로 여기서 던진다.
            if (have < p.Absorbed) throw new InvalidOperationException("PhantomBlade: StandAbsorb pool dropped between calculation and damage (absorbed " + p.Absorbed + ", pool now " + have + ")");
            Owner.Resources.Spend(res, p.Absorbed);

             
            // 남은 풀 = 소모 뒤 되읽은 값. Spend 는 모자라면 남은 만큼만 깎으므로(위 주석) 되읽기가 기대(have − 흡수량)와 다르면 로그 숫자가 틀린다 → 던진다.
            int left = Owner.Resources.GetResourceAmount(res);
            if (left != have - p.Absorbed) throw new InvalidOperationException("PhantomBlade: StandAbsorb pool read-back after spend: had " + have + ", spent " + p.Absorbed + ", now " + left);
            string line = AbsorbText.LogLine(Strings.Resolve(ModContext.StringPrefix + "Absorb.LogLine"), p.Absorbed, left);
            // ② 전투 로그: 본편 GameLog.RunAction(IL 000D)과 같은 공개 경로 — GameLogEventMessage.EventsHandler(HandleLogMessage IL 0002) → MessageLogThread(경고 색).
            // MessageLogThread 는 Common·TacticalCombat 채널에만 있어(LogThreadService.Setup) 「전체(All)」 탭에 보이고 「전투(Combat)」 탭(AnyCombat+InGameCombat,
            // CombatLogVM.CreateInGameChannels IL 006A~007D)에는 안 보인다. 전투 탭에 넣는 공개 경로는 없다(LogThreadBase.AddMessage 는 protected) — README.
            bool delivered = EventBus.RaiseEvent<ILogMessageUIHandler>(h => h.HandleLogMessage(line), true);
            if (!delivered) ModContext.Log("StandAbsorb (combat log not available): " + line);
            // ③ 머리 위 글: 공개 UIAccess.Bark(EntityDataBase, string, float)(본편 InteractionRestrictionPart.ShowSuccessBark 와 같은 호출). 엔진이 IBarkUIHandler 를 올리고(IL 007B)
            // BarkManager.ShowBark 로 본체 머리 위에 그린다. 그 이벤트는 GameLogEventBark 로 「대화」·「전체」 탭에도 한 줄을 남긴다(README 에 적음).
            var game = Game.Instance;
            if (game == null || game.UI == null) throw new InvalidOperationException("PhantomBlade: StandAbsorb: Game.Instance.UI is null (cannot show the overhead absorb text)");
            game.UI.Bark(Owner, AbsorbText.Overhead(Strings.Resolve(ModContext.StringPrefix + "Absorb.Overhead"), p.Absorbed), BarkDurationEngineDefault);
        }
    }
}
