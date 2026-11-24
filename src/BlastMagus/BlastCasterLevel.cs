using System;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;

namespace BlastMagus
{
    // 블라스트 시전자 레벨 = 메이거스 레벨(설계 §11-6, 정찰 §0-5·§2). BlastFeature 의 컴포넌트 (4).
    // 문제: 냉기·전기·복합 블라스트는 ContextCalculateAbilityParamsBasedOnClass{Kineticist} 라 메이거스에겐 CL 0, 불은 그 컴포넌트가 없어 CL = 캐릭터 레벨.
     
    //   - ContextCalculateAbilityParamsBasedOnClass 는 규칙 처리기가 아니라 ContextAbilityParamsCalculator 다. Calculate(…) 가 새 RuleCalculateAbilityParams 에
    //     ReplaceCasterLevel(IL 0096)·ReplaceSpellLevel(IL 00B9)을 먼저 넣고 그 다음 Rulebook.Trigger(IL 00C3/00CF) 한다. 불 경로는 AbilityData.CalculateParams IL 007E 의 맨 규칙.
    //   - Rulebook.TriggerEventInternal: b__6_1(IL 0040, OnEventAboutToTrigger) → b__6_2(IL 0058, OnTrigger) → b__6_4(IL 009C, DidTrigger).
    //   - RuleCalculateAbilityParams.OnTrigger 는 IL 0001 에서 ReplaceCasterLevel 을 처음 읽는다.
    //   → 우리 AboutToTrigger 가 본편 값 뒤·계산 전에 덮는다. 같은 규칙의 다른 AboutToTrigger 처리기(다른 모드)와의 순서는 구독 순서라 미확인(보고서 C6).
    public class BlastCasterLevel : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAbilityParams>
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt == null) throw new ArgumentNullException("evt");
            var spell = evt.Spell;
            if (spell == null) return; // legitimate no-op: params for a non-ability blueprint (get_Spell = Blueprint isinst BlueprintAbility, IL 0006)
            if (!BlastBlueprints.IsBlastAbility(spell)) return; // legitimate no-op: not one of the 6 blasts nor the blast strike SR source (IsBlastAbility throws before Build)
            // 키네티시스트 part 가 우리 것이 아니면(멀티클래스) 그 유닛의 블라스트는 키네티시스트 CL 이 맞다 → 덮지 않는다. 알 수 없음은 IsEnabled 가 오류 로그.
            if (!BlastState.IsEnabled(Owner)) return; // legitimate no-op: blasts disabled for this unit (BlastPartGuard logged why)
            var magus = BlastBlueprints.MagusClass;
            if (magus == null) throw new InvalidOperationException("BlastMagus: BlastCasterLevel before BlastBlueprints.Build() (MagusClass is null)");
            int level = Owner.Descriptor.Progression.GetClassLevel(magus);
            if (level <= 0) throw new InvalidOperationException("BlastMagus: BlastCasterLevel on " + Owner.CharacterName + " with magus level " + level + " (blast feature without a magus level)");
            evt.ReplaceCasterLevel = level;
            evt.ReplaceSpellLevel = level / BlastBlueprints.SpellLevelDivisor;
        }

        // 정당한 무동작: 덮어쓰기는 계산 전에 끝난다(OnTrigger IL 0001 이 읽음).
        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
    }
}
