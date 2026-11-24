using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Properties;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
     
    // 엔진(정찰 §3): GetMaxAmount = Max(m_Min, ApplyMinMax(B) + bonus), B = Σ 클래스 레벨 @011D·@0288 + Intelligence.BonusWithoutTemp @0198, bonus = IResourceAmountBonusHandler 합 @0352~035D.
    // IncreaseResourceAmountBySharedValue{Decrease=true}(@0018 → @004A sub) 는 bonus 에서 P 를 뺀다 → P = ceil(B/2) 이면 결과 = floor(B/2) + 다른 증가분(GolemRules.BombDecrease).
    // P 는 우리 속성(BlueprintUnitProperty) → 자체 게터 BombHalfGetter(본편 게터로는 나누기·올림이 안 된다 — 설계 §4.2). 선례 src\PhantomBlade\StandBlueprints.cs:333-350.
    public static class BombHalf
    {
        public static BlueprintUnitProperty Property;
        public static BlueprintFeature Feature;

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Golemancer: BombHalf.Build() called twice");
            var resource = Get<BlueprintAbilityResource>(Guids.Game.AlchemistBombsResource);

            // 속성 = BaseValue 0 + Σ게터(게터 1개) = P. PropertyValueGetter.GetValue 는 Settings 가 null 이면 GetBaseValue 그대로(정찰 §3 @0002~001A).
            Property = Create<BlueprintUnitProperty>("GolemancerBombHalfProperty", Guids.BombHalfProperty, p =>
            {
                p.BaseValue = 0;
                p.OperationOnComponents = BlueprintUnitProperty.MathOperation.Sum;
            });
            AddComponents(Property, new BombHalfGetter());

            Feature = Create<BlueprintFeature>("GolemancerBombHalfFeature", Guids.BombHalfFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("BombHalf.Name"));
                SetField(f, "m_Description", Strings.L("BombHalf.Description"));
                SetField(f, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(f, "m_Icon", resource.Icon);
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
            var value = new ContextValue { ValueType = ContextValueType.CasterCustomProperty };
            SetField(value, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(Guids.BombHalfProperty));
            var dec = new IncreaseResourceAmountBySharedValue { Decrease = true, Value = value };
            SetField(dec, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.Game.AlchemistBombsResource));
            AddComponents(Feature, dec);
            if (!ReferenceEquals(dec.Resource, resource) || !dec.Decrease || dec.Value.ValueType != ContextValueType.CasterCustomProperty || !ReferenceEquals(dec.Value.CustomProperty, Property))
                throw new InvalidOperationException("Golemancer: GolemancerBombHalfFeature IncreaseResourceAmountBySharedValue readback failed (resource / Decrease / CasterCustomProperty)");

            Archetype.AddAt(1, Feature);
        }
    }

    // P = GolemRules.BombDecrease(B), B = 연금술사 레벨 + Intelligence.BonusWithoutTemp. 엔진 기본항과 같은 항을 읽는다(C4 — 일반 Bonus 를 읽으면 임시 보너스 때 어긋난다, 점검 S2 temp-int-getter-b).
    public sealed class BombHalfGetter : PropertyValueGetter
    {
        // 이 특성은 골렘술사(연금술사) 유닛에만 붙는다 → 연금술사 레벨 0 은 「붙으면 안 되는 곳에 붙음」(판정 불가) → 던진다(기본값 0 으로 흘리지 않는다, C1).
        public static int ComputeB(UnitEntityData unit)
        {
            if (unit == null) throw new ArgumentNullException("unit", "Golemancer: BombHalfGetter needs a unit");
            var alchemist = Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass);
            int level = unit.Progression.GetClassLevel(alchemist);
            if (level <= 0) throw new InvalidOperationException("Golemancer: BombHalfGetter on " + unit + " with alchemist level " + level + " (the halved-bombs feature belongs to golemancer alchemists only)");
            return level + unit.Stats.Intelligence.BonusWithoutTemp;
        }

        protected override int GetBaseValue(UnitEntityData unit)
        {
            return GolemRules.BombDecrease(ComputeB(unit));
        }
    }
}
