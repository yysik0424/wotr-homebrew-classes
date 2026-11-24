namespace ChaosWarrior
{
     
    public static class PainSpec
    {
        // 출처: 설계 §8 명중마다(OnlyHit) · AbstractWeaponTrigger.IsSuitable @0055~0062
        public static readonly bool TriggerOnlyHit = true;
        // 출처: 설계 §10-b 근접 무기 명중만(원거리 제외) — AbstractWeaponTrigger.IsSuitable @00FE~0119 CheckWeaponRangeType + RangeType
        public static readonly bool TriggerCheckWeaponRangeType = true;
        // 출처: 설계 §10-b 근접만 = 엔진 WeaponRangeType.Melee
        public static readonly string TriggerRangeType = "Melee";
        // 출처: 설계 §10-b 기회공격 포함 — OnAttackOfOpportunity true 면 AoO 전용 필터(@03DF~03EF) → false
        public static readonly bool TriggerOnAttackOfOpportunity = false;
        // 출처: 트리거 행동 대상 = 공격 대상(ActionsOnInitiator false — 고통은 맞은 적에게)
        public static readonly bool TriggerActionsOnInitiator = false;
        // 출처: 설계 §10-b 시전자마다 따로 = StackingType.Stack(BuffCollection.PrepareFactForAttach @00FE~010C 가 Stack 이면 합치지 않는다)
        public static readonly string Stacking = "Stack";
         
        public static readonly string[] PenaltyStats =
        {
            "AdditionalAttackBonus", "SkillMobility", "SkillAthletics", "SkillPersuasion", "SkillThievery", "SkillLoreNature", "SkillPerception",
            "SkillStealth", "SkillUseMagicDevice", "SkillLoreReligion", "SkillKnowledgeWorld", "SkillKnowledgeArcana",
        };
        // 출처: 설계 §8 겹당 내성 −1 = BuffAllSavesBonus 1행(순위 배수 @0007~0017)
        public static readonly bool PenaltySaves = true;
        // 출처: 판정 필요 PD 기본값 선택 규칙(T7 Step 0) — DefaultStackingDescriptors 밖 + 본편 음수 사용 0건인 첫 기술자(열거 순서) = Deflection(8). 스캔 표: 원장 T7.
        public static readonly string PenaltyDescriptor = "Deflection";
         
        public static readonly string ProbeDescriptor = "MindAffecting";
        // 출처: 소유자 결정 2026-11-17 「계수 버프 하나로 재구성」 — 총 겹의 단일 원장(계수 버프)은 해제되지 않는다. 수단 = 본편 부품 NotDispelable(OnActivate @0006~000D 가 Buff.IsNotDispelable = true)
        //   — 해제 주문 ContextActionDispelMagic.TryDispelBuff @004E~0053 이 IsNotDispelable 을 HiddenInInspector(@0059 — TTT 패치가 바꾸는 값)보다 먼저 보고 건너뛴다(Assembly-CSharp IL 직독 2026-11-17).
        public static readonly bool CounterNotDispelable = true;
    }
}
