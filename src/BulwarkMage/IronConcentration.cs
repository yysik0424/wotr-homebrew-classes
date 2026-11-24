using System;
using Kingmaker.Blueprints.Classes;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
     
    //   T7 Step 0(dnlib 2026-11-14): 피해 원인 집중은 UnitConcentrationController.Tick @0073 → UnitUseAbility.MakeConcentrationCheck(RuleDealDamage) @001F 의 new RuleCheckConcentration(실행자, 주문, 피해) 하나뿐.
    //   Initiator = 시전자 → IInitiatorRulebookHandler 가 받는다. CustomDC 는 OnTrigger @001C~0067 에서(모든 AboutToTrigger 뒤) DC 로 소비된다: CustomDC 가 있으면 DC = CustomDC, 없으면 10 + 주문 레벨 + 피해/2.
    //   성공 = d20 + Concentration ≥ DC(get_Success @0000~0011, 자연 1 규칙 없음). 실패할 수 있는 남은 길(README 한계 A5): 집중 수치 ≤ −1001(+ AddTwiceSpellLevel 이면 −992 근처) ·
    //   다른 구독자가 이 뒤에 CustomDC 를 다시 쓰는 경우(설치 모드에서는 찾지 못함 — PrestigePlus 붙잡기는 피해 아닌 집중) · AddTwiceSpellLevel(엔진은 시전 곤란 경로에서만 켠다 @02F9).
    public static class IronConcentration
    {
        public static BlueprintFeature Feature;




        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("BulwarkMage: IronConcentration.Build() called twice");
            Feature = Create<BlueprintFeature>("BulwarkIronConcentration", Guids.IronConcentrationFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("iron.name"));
                SetField(f, "m_Description", Strings.L("iron.desc"));
                SetField(f, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                f.Ranks = 1;
                f.IsClassFeature = true;
                f.HideInUI = false;
                f.Groups = new FeatureGroup[0];
                f.ReapplyOnLevelUp = false;
                Rebuke.FillEmpty(f);
            });
            AddComponents(Feature, new IronConcentrationComponent());
            Archetype.AddAt(BulwarkRules.Levels.IronConcentrationLevel, Feature);
        }
    }

    // 중갑을 입은 시전자의 피해 원인 집중 판정만 강제 성공(CustomDC = Dials.IronForcedDC). 예외 계약: 본문 전체 try, 맨 앞 MaybeFault, catch = ReportException(엔진으로 던지지 않음 — 그 판정은 그대로).
    public class IronConcentrationComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCheckConcentration>
    {
        public void OnEventAboutToTrigger(RuleCheckConcentration evt)
        {
            try
            {
                BulwarkErrors.MaybeFault(ErrorLedger.SiteIronConcentration);
                if (evt == null) { BulwarkErrors.Report(ErrorLedger.SiteIronConcentration, "null concentration rule"); return; }
                bool damageCaused = evt.Damage != null;
                bool? heavy = null;
                if (damageCaused)
                {
                    var body = Owner == null ? null : Owner.Body;
                    var slot = body == null ? null : body.Armor;
                    if (slot != null) heavy = IronConcentrationRule.IsHeavy(slot.HasArmor ? (slot.MaybeArmor == null ? null : slot.MaybeArmor.ArmorType().ToString()) : "None");
                }
                var verdict = IronConcentrationRule.Decide(damageCaused, heavy);
                if (verdict == IronVerdict.Unknown) { BulwarkErrors.Report(ErrorLedger.SiteIronConcentration, "cannot read the body armor group of " + (Owner == null ? "<no owner>" : Owner.CharacterName) + " - concentration left to the normal DC"); return; }
                if (verdict == IronVerdict.Force) evt.CustomDC = BulwarkRules.Dials.IronForcedDC;
            }
            catch (Exception e)
            {
                BulwarkErrors.ReportException(ErrorLedger.SiteIronConcentration, e);
            }
        }

        public void OnEventDidTrigger(RuleCheckConcentration evt) { }
    }
}
