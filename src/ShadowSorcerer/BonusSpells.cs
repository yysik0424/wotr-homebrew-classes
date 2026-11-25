using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.EntitySystem.Entities;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ShadowSorcerer
{
     
    // AddKnownSpell.AddSpell(dnlib 2026-11-14): 클래스 데이터·아키타입 일치 → 그 클래스 주문서에 아직 모르고 MaxSpellLevel ≥ SpellLevel 이면 AddKnown(…, true).
    // 소서러 레벨 2n+1 에 주문 레벨 n 이므로(소서러는 2n 에 n 레벨을 연다) 붙는 레벨에서 늘 MaxSpellLevel ≥ n.
    // 선검사(Prevalidate)가 9행을 모두 통과해야 만든다 — 하나라도 문제면 문제마다 Report(builder) 후 던진다(특성 0개, BuildFailed).
    public static class BonusSpells
    {
        public static readonly List<BlueprintFeature> Features = new List<BlueprintFeature>();

        public static void Build()
        {
            if (Features.Count > 0) throw new InvalidOperationException("ShadowSorcerer: BonusSpells.Build() called twice");
            var rows = BonusSpellPlan.Rows;
            var list = Get<BlueprintSpellList>(Guids.Game.WizardSpellList);
            var problems = BonusSpellPlan.Prevalidate(rows, guid => Resolve(list, guid));
            foreach (var p in problems) ShadowErrors.Report(ErrorLedger.SiteBuilder, "bonus spells: " + p);
            if (problems.Count > 0) throw new InvalidOperationException("ShadowSorcerer: bonus spell prevalidation failed (" + problems.Count + " problem(s)) - no bonus spell feature was made");
            var sorcerer = Ref<BlueprintCharacterClassReference>(Guids.Game.SorcererClass);
            foreach (var r in BonusSpellPlan.PlanCreates(rows, problems))
            {
                var spell = Get<BlueprintAbility>(r.SpellGuid);
                var f = Create<BlueprintFeature>("ShadowSorcererBonusSpell" + r.SpellLevel, r.FeatureGuid, x =>
                {
                    SetField(x, "m_DisplayName", Strings.L(r.NameKey));
                    SetField(x, "m_Description", Strings.L("bonus.desc"));
                    SetField(x, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 없음 = 본편 jbp 의 빈 키(엔진 get_DescriptionShort 가 빈 값이면 설명으로 대체, null 이면 "<null>")
                    x.IsClassFeature = true;
                });
                var c = new AddKnownSpell { SpellLevel = r.SpellLevel };
                SetField(c, "m_CharacterClass", sorcerer);
                SetField(c, "m_Spell", Ref<BlueprintAbilityReference>(r.SpellGuid));
                SetField(c, "m_Archetype", Ref<BlueprintArchetypeReference>(Guids.Archetype));
                AddComponents(f, c);
                if (c.Spell != spell || c.CharacterClass == null || c.CharacterClass.AssetGuid.ToString() != Guids.Game.SorcererClass || c.SpellLevel != r.SpellLevel)
                    throw new InvalidOperationException("ShadowSorcerer: AddKnownSpell readback failed for " + r);
                Archetype.AddAt(r.SorcererLevel, f);
                Features.Add(f);
            }
            if (Features.Count != rows.Count) throw new InvalidOperationException("ShadowSorcerer: made " + Features.Count + " bonus spell features, expected " + rows.Count);
        }

        static SpellFacts Resolve(BlueprintSpellList list, string guid)
        {
            var raw = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(guid)) as BlueprintAbility;
            if (raw == null) return new SpellFacts { Exists = false };
            var levels = list.SpellsByLevel.Where(l => l.Spells.Contains(raw)).Select(l => l.SpellLevel).ToList();
            return new SpellFacts { Exists = true, ListLevel = levels.Count == 1 ? levels[0] : (int?)null };
        }
    }

    // Grants the echo toggle at level one. The spellbook handles the original cast.
    public static class SlotsFeature
    {
        public static BlueprintFeature Feature;
        internal static BlueprintBuff Buff;
        internal static BlueprintActivatableAbility Toggle;
        internal static bool Active(UnitEntityData owner) => Buff != null && owner != null && owner.Buffs.HasFact(Buff);


        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("ShadowSorcerer: SlotsFeature.Build() called twice");
            Feature = Create<BlueprintFeature>("ShadowSorcererSlotsFeature", Guids.SlotsFeature, x =>
            {
                SetField(x, "m_DisplayName", Strings.L("slots.name"));
                SetField(x, "m_Description", Strings.L("slots.desc"));
                SetField(x, "m_DescriptionShort", NoText()); // 2026-11-17: 위 보너스 특성과 같음(빈 키 = 설명으로 대체)
                x.IsClassFeature = true;
            });
            Buff = Create<BlueprintBuff>("ShadowSorcererEchoBuff", Guids.EchoBuff, b => {
                SetField(b, "m_DisplayName", Strings.L("echo.name"));
                SetField(b, "m_Description", Strings.L("slots.desc"));
                SetField(b, "m_DescriptionShort", Strings.L("slots.desc"));
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                EmptyFxOnRemove(b);
            });
            Toggle = Create<BlueprintActivatableAbility>("ShadowSorcererEchoToggle", Guids.EchoToggle, t => {
                SetField(t, "m_DisplayName", Strings.L("echo.name"));
                SetField(t, "m_Description", Strings.L("slots.desc"));
                SetField(t, "m_DescriptionShort", Strings.L("slots.desc"));
                SetField(t, "m_Buff", Ref<BlueprintBuffReference>(Guids.EchoBuff));
                t.ActivationType = AbilityActivationType.Immediately;
                t.Group = ActivatableAbilityGroup.None;
                t.DeactivateImmediately = true;
                t.DoNotTurnOffOnRest = true;
            });
            var facts = new AddFacts();
            SetField(facts, "m_Facts", new BlueprintUnitFactReference[] { Ref<BlueprintUnitFactReference>(Guids.EchoToggle) });
            AddComponents(Feature, facts);
            Archetype.AddAt(ShadowRules.MinLevel, Feature);
        }
    }
}
