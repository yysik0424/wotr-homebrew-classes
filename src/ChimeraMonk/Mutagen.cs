using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    //  - 자원: 3 + 지혜 수정치, 최소 1(BlueprintAbilityResource.GetMaxAmount IL_0363~0377 Math.Max(m_Min, …)).
    //  - 버프 3(힘·민첩·지혜): MutagenPlan.Buff 행만으로 — AddContextStatBonus{Alchemical, 능력치, 순위} + {NaturalArmor, AC, 순위} + CRC{ClassLevel, [Monk], Custom 20행} ×2. 정신 페널티 0.
    //  - 능력 3: Supernatural · Personal · Standard · AbilityResourceLogic{m_IsSpendResource = true, Amount 1}(I01 — 생성자는 소비 플래그를 켜지 않는다) · AbilityCasterNotPolymorphed ·
    //    AbilityEffectRunAction{우리 버프 3 제거 → 자기 버프 적용(몽크 레벨 라운드)} · CRC{ClassLevel, [Monk], AsIs}.
    //  - 「변이약 중」 판정은 ActiveBuffRefs 하나에서 파생(C4 — T5 ChimeraLimbComponent · T6/T7 BuffExtraEffects · T7 AbilityCasterHasFacts).
    public static class Mutagen
    {
        public static BlueprintAbilityResource Resource;
        public static BlueprintFeature Feature;
        public static readonly List<BlueprintBuff> Buffs = new List<BlueprintBuff>();
        public static readonly List<BlueprintAbility> Abilities = new List<BlueprintAbility>();

        public static BlueprintBuffReference[] ActiveBuffRefs
        {
            get
            {
                if (Buffs.Count != MutagenPlan.Stats.Length) throw new InvalidOperationException("ChimeraMonk: Mutagen.ActiveBuffRefs before Mutagen.Build");
                return Buffs.Select(b => Ref<BlueprintBuffReference>(b.AssetGuid.ToString())).ToArray();
            }
        }

        // 지속 순위 형식(능력 CRC — 버프 수치 순위와 겹치지 않게 Default).
        static readonly AbilityRankType DurationRank = AbilityRankType.Default;

        public static void Build()
        {
            if (Resource != null) throw new InvalidOperationException("ChimeraMonk: Mutagen.Build() called twice");
            Resource = Create<BlueprintAbilityResource>("ChimeraMutagenResource", Guids.MutagenResource, r =>
            {
                r.LocalizedName = Strings.L("mutagen.name");
                r.LocalizedDescription = Strings.L("mutagen.desc");
                object amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                Fab.FillEmpty(amount);
                SetField(amount, "BaseValue", ChimeraRules.Dials.MutagenDailyBase);
                SetField(amount, "IncreasedByStat", true);
                SetField(amount, "ResourceBonusStat", StatType.Wisdom);
                SetField(r, "m_MaxAmount", amount);
                SetField(r, "m_UseMax", false);
                SetField(r, "m_Min", ChimeraRules.Dials.MutagenDailyMin);
            });

            var specs = MutagenPlan.Stats.Select(MutagenPlan.Buff).ToList();
            foreach (var spec in specs)
            {
                var b = Fab.Buff("ChimeraMutagenBuff" + spec.Stat, spec.BuffGuid, "mutagen." + spec.Stat, false);
                var comps = new List<BlueprintComponent>();
                foreach (var row in spec.Bonuses)
                    comps.Add(Fab.Comp(new AddContextStatBonus { Descriptor = Fab.EnumOf<ModifierDescriptor>(row.Descriptor), Stat = Fab.EnumOf<StatType>(row.Stat), Multiplier = 1, Value = Fab.CvRank(Fab.EnumOf<AbilityRankType>(row.RankType)) }));
                foreach (var rank in spec.Ranks)
                    comps.Add(Fab.RankConfig(Fab.EnumOf<AbilityRankType>(rank.RankType), ContextRankProgression.Custom, rank.Table));
                AddComponents(b, comps.ToArray());
                Buffs.Add(b);
            }

            foreach (var spec in specs)
            {
                var a = Fab.Ability("ChimeraMutagenAbility" + spec.Stat, spec.AbilityGuid, "mutagen." + spec.Stat, x =>
                {
                    x.Type = AbilityType.Supernatural;
                    x.Range = AbilityRange.Personal;
                    x.ActionType = UnitCommand.CommandType.Standard;
                    x.CanTargetSelf = true;
                    x.CanTargetFriends = false;
                    x.CanTargetEnemies = false;
                    x.CanTargetPoint = false;
                    x.EffectOnAlly = AbilityEffectOnUnit.Helpful;
                    x.EffectOnEnemy = AbilityEffectOnUnit.None;
                    MutagenFx.ApplyAnimation(x); // FX(2026-11-15 승인): 동작 = 본편 MutagenStrength 의 물약 마시기(Potion / CastActionPotion) + 미리 읽기 — FxTable 원장
                });
                var logic = Fab.Comp(new AbilityResourceLogic { Amount = 1, CostIsCustom = false });
                SetField(logic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.MutagenResource));
                SetField(logic, "m_IsSpendResource", true);
                var actions = specs.Select(s => (Kingmaker.ElementsSystem.GameAction)Fab.RemoveBuff(s.BuffGuid, false)).ToList();
                actions.Add(Fab.ApplyBuff(spec.BuffGuid, false, Fab.RoundsOfRank(DurationRank), false, false));
                var run = Fab.Comp(new AbilityEffectRunAction { SavingThrowType = SavingThrowType.Unknown, IgnoreCaster = false, Actions = Fab.List(actions.ToArray()) });
                // 끝에 AbilitySpawnFx(마시는 FX, 시전자 1회) — 기존 컴포넌트 위치는 그대로. 원본 대조는 MutagenFx(어긋나면 던진다).
                AddComponents(a, logic, Fab.Comp(new AbilityCasterNotPolymorphed()), run, Fab.RankConfig(DurationRank, ContextRankProgression.AsIs, null), MutagenFx.SpawnComponent());
                Abilities.Add(a);
            }

            Feature = Fab.Feature("ChimeraMutagenFeature", Guids.MutagenFeature, "mutagen", false);
            var facts = Fab.Comp(new AddFacts());
            SetField(facts, "m_Facts", Abilities.Select(x => Ref<BlueprintUnitFactReference>(x.AssetGuid.ToString())).ToArray());
            var res = Fab.Comp(new AddAbilityResources { RestoreAmount = true, UseThisAsResource = false, Amount = 0, RestoreOnLevelUp = false });
            SetField(res, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.MutagenResource));
            AddComponents(Feature, facts, res);
            Archetype.AddPlanned(LevelPlan.KeyMutagen, Feature);

            var diff = Readback.MutagenDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: mutagen readback: " + string.Join(" | ", diff));
        }
    }
}
