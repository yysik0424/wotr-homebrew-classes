using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
    // 20레벨 「강신 IV」 특성 + 제거된 융합의 세이브 호환(소유자 결정 2026-11-18: 융합 제거, 강신 IV 유지).
    //  - 강신 IV 자체(하루 4회·10라운드·강신 III 효과)는 강신 자원 식·PossessionRules.EffectBuffIndex 가 낸다 — 이 특성은 20레벨 레벨표의 설명이다.
    //  - 옛 특성 IV 는 AddFacts{융합 능력} 하나였다(#0). 같은 자리에 Disabled 로 남긴다: 옛 세이브 로드 때 엔진 EntityFact.ApplyPostLoadFixes @001C 가 Disabled 를 보고
    //    Deactivate → AddFacts.OnDeactivate → Clear 가 그때 붙인 융합 능력을 뗀다. 새 캐릭터는 EntityFact.Setup @005D 가 Disabled 컴포넌트를 만들지 않는다(dnlib 2026-11-18,
    //    주술사 무기 중점화 제거와 같은 방식). 그 경로가 안 돌아도 #1 RetiredFusionAudit 가 지역 로드마다 남은 융합 능력을 뗀다(그 블루프린트는 우리 것뿐 — 출처 확정).
    //  - 융합 능력·버프 블루프린트는 GUID 그대로 둔다(세이브의 사실이 블루프린트를 찾지 못하면 엔진이 그 사실을 못 읽는다 — EntityFact.set_ComponentsDictionary @0011).
    //    능력 = 시전 제한 RetiredFusionRestriction(언제나 불가)만, 효과·자원 없음. 버프 = 컴포넌트 없음(남아 있으면 감사가 떼고 오류 1줄).
    //  - 옛 주술사 파트 UnitPartSpiritFusion 은 저장 필드 4개만 남긴다(형식이 없으면 세이브의 파트를 역직렬화할 수 없다).
    public static class Fusion
    {
        public static BlueprintBuff Buff;
        public static BlueprintAbility Ability;
        public static BlueprintFeature Feature;

        public static void Build()
        {
            if (Possession.Buffs == null || Possession.Resource == null) throw new InvalidOperationException("WeaponSpiritShaman: Fusion.Build() before Possession.Build()");
            if (Buff != null) throw new InvalidOperationException("WeaponSpiritShaman: Fusion.Build() called twice");
            var tiers = PossessionRules.Tiers;
            if (tiers.Length <= Possession.Buffs.Length) throw new InvalidOperationException("WeaponSpiritShaman: Dials.PossessionTiers has " + tiers.Length + " rows — no possession IV tier beyond the " + Possession.Buffs.Length + " effect buffs");
            var icon = Possession.Ability.Icon;
            if (icon == null) throw new InvalidOperationException("WeaponSpiritShaman: possession ability has no icon (possession IV icon source)");

            // 제거된 융합 — 세이브 호환용 블루프린트(GUID 유지). 얻는 경로 없음.
            Buff = Create<BlueprintBuff>("WeaponSpiritShamanFusionBuff", Guids.FusionBuff, x =>
            {
                Possession.Names(x, "Fusion.Name", "Fusion.Description", icon);
                x.IsClassFeature = true;
                x.Stacking = StackingType.Replace;
                EmptyFxOnRemove(x);
            });
            Ability = Create<BlueprintAbility>("WeaponSpiritShamanFusionAbility", Guids.FusionAbility, a =>
            {
                Possession.Names(a, "Fusion.Name", "Fusion.Description", icon);
                a.Type = AbilityType.Supernatural;
                a.Range = AbilityRange.Personal;
                a.ActionType = Possession.CommandOf(SpiritRules.Dials.PossessionAction);
                a.CanTargetSelf = true;
                a.LocalizedDuration = NoText();
                a.LocalizedSavingThrow = NoText();
            });
            Possession.RequireTooltipFields(Ability, "");
            AddComponents(Ability, new RetiredFusionRestriction());
            if (Ability.ComponentsArray.Length != 1 || Ability.CasterRestrictions.Length != 1 || Buff.ComponentsArray.Length != 0)
                throw new InvalidOperationException("WeaponSpiritShaman: retired fusion blueprints readback failed (ability components " + Ability.ComponentsArray.Length + ", buff components " + Buff.ComponentsArray.Length + ")");

            Feature = Create<BlueprintFeature>("WeaponSpiritShamanPossessionFeature4", Guids.PossessionFeature4, x =>
            {
                Possession.Names(x, "Possession4.Name", "Possession4.Description", icon);
                x.IsClassFeature = true;
                x.Ranks = 1;
                x.Groups = new FeatureGroup[0];
            });
            var oldGrant = new AddFacts { Disabled = true }; // #0 = 옛 AddFacts{융합 능력} 자리(옛 세이브 정리 — 위 머리말)
            SetField(oldGrant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.FusionAbility) });
            AddComponents(Feature, oldGrant, new RetiredFusionAudit());
            var comps = Feature.ComponentsArray;
            if (comps.Length != 2 || !(comps[0] is AddFacts) || !comps[0].Disabled || comps[0].name != "$AddFacts$" + Feature.AssetGuid + "#0" || !(comps[1] is RetiredFusionAudit) || comps[1].Disabled)
                throw new InvalidOperationException("WeaponSpiritShaman: possession feature IV readback failed (expected [AddFacts#0 Disabled, RetiredFusionAudit#1], got [" + string.Join(",", comps.Select(c => c.name + (c.Disabled ? "(disabled)" : "")).ToArray()) + "])");
            int level = tiers[tiers.Length - 1].Level;
            Archetype.AddAt(level, Feature);
            var levels = Archetype.Bp.AddFeatures.Where(e => e.Features.Contains(Feature)).Select(e => e.Level).ToArray();
            if (levels.Length != 1 || levels[0] != level) throw new InvalidOperationException("WeaponSpiritShaman: possession feature IV archetype levels [" + string.Join(",", levels) + "], expected [" + level + "]");
            PossessionFx.Verify(); // FX 대상(강신 버프 3·능력)이 전부 만들어진 뒤 — 표와 실제 값 대조
        }
    }

    // 옛 세이브 정리(특성 IV 컴포넌트 #1). 지역 로드가 끝날 때마다 관측 → 순수 RetiredFusionRule.Decide → 실행. 로그는 실제로 무언가를 뗀/되돌린 경우만.
    public class RetiredFusionAudit : UnitFactComponentDelegate, IAreaHandler, IGlobalSubscriber
    {
        public void OnAreaDidLoad()
        {
            try { AuditNow(Owner); }
            catch (Exception e) { ModContext.LogError("WeaponSpiritShaman: UNKNOWN retired fusion audit for " + ((object)Owner == null ? "null" : Owner.CharacterName) + " failed: " + e); }
        }

        public void OnAreaBeginUnloading() { }

        public static RetiredFusionRule.Plan AuditNow(UnitEntityData unit)
        {
            if ((object)unit == null) throw new ArgumentNullException("unit");
            if (Fusion.Ability == null || Fusion.Buff == null) throw new InvalidOperationException("WeaponSpiritShaman: retired fusion audit before Fusion.Build");
            var abilities = unit.Facts.List.Where(f => ReferenceEquals(f.Blueprint, Fusion.Ability)).ToList();
            var buffs = unit.Buffs.Enumerable.Where(b => ReferenceEquals(b.Blueprint, Fusion.Buff)).ToList();
            var part = unit.Get<UnitPartSpiritFusion>();
            var plan = RetiredFusionRule.Decide(abilities.Count, buffs.Count, part != null, part == null ? RetiredFusionRule.IdleState : part.State,
                part == null ? 0 : part.PendingSteps, part != null && part.HiddenByUs, part != null && !string.IsNullOrEmpty(part.PetId));
            if (plan.Nothing) return plan;
            string who = unit.CharacterName + " (" + unit.UniqueId + ")";
            if (plan.RemoveAbilities)
            {
                foreach (var f in abilities) UnitHelper.RemoveFact(unit, f);
                ModContext.Log("WSS INFO retired fusion: removed " + abilities.Count + " Spirit Fusion ability fact(s) from " + who + " (fusion was removed 2026-11-18)");
            }
            if (plan.RemoveBuffs) foreach (var b in buffs) b.Remove();
            if (plan.Unhide)
            {
                var pet = Possession.SpiritOf(unit);
                var pp = (object)pet == null || pet.UniqueId != part.PetId ? null : pet.Get<UnitPartPet>();
                if (pp == null) plan.Error += "; the saved hidden weapon spirit " + part.PetId + " was not found";
                else pp.SetHidden(false);
            }
            if (plan.ResetPart) { part.State = RetiredFusionRule.IdleState; part.PendingSteps = 0; part.HiddenByUs = false; }
            if (plan.Error != null) ModContext.LogError("WeaponSpiritShaman: retired fusion on " + who + ": " + plan.Error);
            return plan;
        }
    }

    // 제거된 융합 능력의 시전 제한: 언제나 불가(옛 세이브에 능력 사실이 남은 짧은 동안에도 쓸 수 없다).
    public class RetiredFusionRestriction : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return false; }

        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "Fusion.Unavailable"); }
    }

    // 옛 융합 주술사 파트 — 세이브 호환 전용(저장 필드 4개, 동작 없음). 값은 RetiredFusionAudit 가 읽고 Idle 로 되돌린다.
    public class UnitPartSpiritFusion : UnitPart
    {
        [JsonProperty] public int State;
        [JsonProperty] public bool HiddenByUs;
        [JsonProperty] public int PendingSteps;
        [JsonProperty] public string PetId;
    }
}
