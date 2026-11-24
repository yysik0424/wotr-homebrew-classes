using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace ShadowSorcerer
{
    // 격리 감시선의 규칙 쪽(독립 검토 F2, 2026-11-17). 시각 전용 그림자 유닛의 껍데기 특성(ShadowUnitBlueprint.Shell)에 붙는다 — 그 유닛에만 붙으므로 전역 패치·전역 구독이 아니다.
    // 그림자 유닛이 내성 굴림의 주체가 되거나(RuleSavingThrow.Initiator = 굴리는 유닛) 피해 규칙의 대상이 되면 한 번 시끄럽게 보고한다(ShadowDoubles.NoteRuleTarget — 던지지 않는다).
    //   배경: RuleDealDamage 는 대상 불가 유닛의 피해를 0 으로 끝내서 「피해 0」 되읽기만으로는 원뿔·선·연쇄에 낀 것을 볼 수 없었다. 규칙이 발행됐다는 사실 자체를 본다.
    // 읽기만 한다: 규칙을 바꾸지도, 새 규칙을 내지도 않는다(연출은 규칙을 바꾸지 않는다). 인스턴스 필드 0(블루프린트 컴포넌트는 모든 유닛이 공유한다).
    // 저장: 이 컴포넌트가 붙은 유닛은 저장되지 않는다(UnitPartTrapActor) — 저장 파일에 이 형식이 남지 않는다.
    public class ShadowTripwireComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleSavingThrow>, ITargetRulebookHandler<RuleDealDamage>
    {
        public const string SavingThrow = "RuleSavingThrow", DealDamage = "RuleDealDamage";

        public void OnEventAboutToTrigger(RuleSavingThrow evt) { } // 정당한 무동작: 발행된 뒤 한 번만 센다
        public void OnEventDidTrigger(RuleSavingThrow evt) { ShadowDoubles.NoteRuleTarget(Owner, SavingThrow); }

        public void OnEventAboutToTrigger(RuleDealDamage evt) { } // 정당한 무동작: 발행된 뒤 한 번만 센다
        public void OnEventDidTrigger(RuleDealDamage evt) { ShadowDoubles.NoteRuleTarget(Owner, DealDamage); }
    }
}
