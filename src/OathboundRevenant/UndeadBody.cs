using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    // UndeadImmunities·NegativeEnergyAffinity 는 UndeadType 이 스스로 붙인다(부록 A-a·A-b) → 따로 붙이지 않는다(wiring: undead body 가 고정).
    public static class UndeadBody
    {
        public static BlueprintFeature Feature;

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("OathboundRevenant: UndeadBody.Build() called twice");
            var undead = Get<BlueprintFeature>(Guids.Game.UndeadType);
            Feature = Create<BlueprintFeature>("OathboundRevenantUndeadBody", Guids.UndeadBodyFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("undead.name"));
                SetField(f, "m_Description", Strings.L("undead.desc"));
                Tooltips.NoShortDescription(f);   // 2026-11-16: LocalizedString 필드 null 금지(Tooltips 머리말)
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
            var add = new AddFacts();
            SetField(add, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.Game.UndeadType) });
            AddComponents(Feature, add);
            // 되읽기: AddFacts 정확히 1개, 대상 = UndeadType(게임이 읽는 경로 Facts 로).
            var adds = Feature.ComponentsArray.OfType<AddFacts>().ToArray();
            if (adds.Length != 1 || adds[0].Facts.Count() != 1 || !ReferenceEquals(adds[0].Facts.First(), undead))
                throw new InvalidOperationException("OathboundRevenant: UndeadBody readback failed (AddFacts " + adds.Length + ", expected one fact UndeadType)");
        }
    }
}
