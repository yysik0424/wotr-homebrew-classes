using System.Collections.Generic;

namespace ShadowSorcerer
{
     
    // 판정 종류: EchoRule = 인자 RuleCastSpell 이 메아리 규칙 · EchoContext = 인자 규칙의 Context 가 메아리 컨텍스트 · EchoConstruction = 메아리 RuleCastSpell 생성 중(EchoConstructionScope).
    // 행 3(AddAbilityUseTrigger.OnEventDidTrigger(RuleApplySpell))은 T6 Step 0 에서 본문이 `ret` 하나(비활성)라 뺐다 — 계획 「행 3 없음 = 12행, SUMMARY 126」.
    // 시험 `dll: guard targets exist` 가 Assembly-CSharp 에서 각 시그니처를, `plan: every guard row has an in-game step` 이 S14 단계 이름과 1:1 을 잠근다.
    public enum GuardKind { EchoRule, EchoContext, EchoConstruction }

    public sealed class GuardRow
    {
        public readonly int PlanRow;
        public readonly string TypeFullName, Method, ArgTypeFullName, Step;
        public readonly GuardKind Kind;
        public GuardRow(int planRow, string type, string method, string arg, GuardKind kind, string step) { PlanRow = planRow; TypeFullName = type; Method = method; ArgTypeFullName = arg; Kind = kind; Step = step; }
        public string TargetLine { get { return TypeFullName + "|" + Method + "|" + ArgTypeFullName; } }
    }

    public static class TriggerGuardPlan
    {
        const string Cast = "Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell", Apply = "Kingmaker.RuleSystem.Rules.Abilities.RuleApplySpell", Params = "Kingmaker.RuleSystem.Rules.Abilities.RuleCalculateAbilityParams";
        const string About = "OnEventAboutToTrigger", Did = "OnEventDidTrigger";

        public static readonly IReadOnlyList<GuardRow> Targets = new[]
        {
            new GuardRow(1, "Kingmaker.UnitLogic.FactLogic.AddAbilityUseTrigger", About, Cast, GuardKind.EchoRule, "aaut-cast-about-once"),
            new GuardRow(2, "Kingmaker.UnitLogic.FactLogic.AddAbilityUseTrigger", About, Apply, GuardKind.EchoContext, "aaut-apply-about-once"),
            new GuardRow(4, "Kingmaker.UnitLogic.FactLogic.AddAbilityUseTargetTrigger", About, Cast, GuardKind.EchoContext, "aautt-about-once"),
            new GuardRow(5, "Kingmaker.UnitLogic.FactLogic.AddAbilityUseTargetTrigger", Did, Cast, GuardKind.EchoContext, "aautt-did-once"),
            new GuardRow(6, "Kingmaker.UnitLogic.FactLogic.AbilityUsagesCountTrigger", Did, Cast, GuardKind.EchoRule, "usages-count-once"),
            new GuardRow(7, "Kingmaker.Designers.Mechanics.Facts.SpendChargesOnSpellCast", About, Cast, GuardKind.EchoRule, "spend-charges-not-doubled"),
            new GuardRow(8, "Kingmaker.UnitLogic.FactLogic.ConduitSurge", Did, Cast, GuardKind.EchoRule, "conduit-surge-once"),
            new GuardRow(9, "Kingmaker.Designers.EventConditionActionSystem.Events.SpellCastTrigger", Did, Cast, GuardKind.EchoRule, "spell-cast-trigger-direct-call"),
            // 검토 지적(2026-11-14, 소수 1): DLC3 주문 레벨 보너스 · 학파별 사용 반응도 「시전 시」 반응이다(본문 IL 52 · 55 — 활성).
            new GuardRow(14, "Kingmaker.Designers.Mechanics.Facts.IncreaseAttackAndDamageBonusBySpellLevel", Did, Cast, GuardKind.EchoRule, "spell-level-bonus-once"),
            new GuardRow(15, "Kingmaker.UnitLogic.FactLogic.AddAbilityUseTriggerBySpellSchool", About, Apply, GuardKind.EchoContext, "aaut-school-apply-about-once"),
            new GuardRow(10, "Kingmaker.Designers.Mechanics.Facts.MetamagicOnNextSpell", About, Params, GuardKind.EchoConstruction, "next-spell-buff-not-bound-to-echo"),
            new GuardRow(11, "Kingmaker.Designers.Mechanics.Facts.MetamagicOnNextSpell", About, Cast, GuardKind.EchoRule, "next-spell-buff-between-preserved"),
            new GuardRow(12, "Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", About, Params, GuardKind.EchoConstruction, "auto-metamagic-not-recorded-for-echo"),
            new GuardRow(13, "Kingmaker.Designers.Mechanics.Facts.AutoMetamagic", About, Cast, GuardKind.EchoRule, "auto-metamagic-once-between-preserved"),
        };

        // 순수 판정(Prefix 의 반환값 그대로): 원래 본문을 실행하는가. 막을 때만 false. 사실을 못 읽으면 unread = true 와 함께 실행(본편 동작 유지 — 호출자가 보고).
        public static bool RunOriginal(GuardKind kind, bool? ruleIsEcho, bool? contextIsEcho, bool inConstruction, out bool unread)
        {
            var b = ShouldBlock(kind, ruleIsEcho, contextIsEcho, inConstruction);
            unread = !b.HasValue;
            return !(b.HasValue && b.Value);
        }

        // 순수 판정: 이 호출을 막는가. 사실 결측(null)은 막지 않는다 — 호출자가 Report 한다(가드는 본편 동작을 지우지 않는 쪽이 안전: 막지 못하면 부작용 1+N 으로 S14 가 FAIL).
        public static bool? ShouldBlock(GuardKind kind, bool? ruleIsEcho, bool? contextIsEcho, bool inConstruction)
        {
            switch (kind)
            {
                case GuardKind.EchoRule: return ruleIsEcho;
                case GuardKind.EchoContext: return contextIsEcho;
                case GuardKind.EchoConstruction: return inConstruction;
            }
            return null;
        }
    }
}
