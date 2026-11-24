using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    // 원소 특성마다 능력 2: 기본(기본 자원) · 분출(분출 자원 + AbilityCasterHasFacts{변이약 버프 3, NeedsAll false} — 변이약 중에만).
    // 능력 구성 = 스케일드 피스트 붉은 용 브레스의 투사체·피해 행동 복제(원소·투사체만 바꿈) + CRC{ClassLevel, [Monk], Div2}(레벨 2당 1d6) + DC = ContextCalculateAbilityParamsBasedOnClass{Monk, Wisdom}
    // (스케일드 피스트의 매력 ReplaceAbilitiesStat·기 자원은 가져오지 않는다).
    public static class Breath
    {
        public static BlueprintAbilityResource Resource, SurgeResource;
        public static readonly Dictionary<string, BlueprintAbility> Abilities = new Dictionary<string, BlueprintAbility>();
        public static readonly Dictionary<string, BlueprintAbility> SurgeAbilities = new Dictionary<string, BlueprintAbility>();

        static readonly AbilityRankType DiceRank = AbilityRankType.Default;

        static string ProjectileFor(string element)
        {
            switch (element)
            {
                case "Fire": return Guids.Game.FireConeBreath;
                case "Cold": return Guids.Game.ColdConeBreath;
                case "Acid": return Guids.Game.AcidConeBreath;
                case "Electricity": return Guids.Game.AirCone; // 본편에 전기 원뿔 브레스 투사체 없음(원장 편차 — 시각만)
                default: throw new InvalidOperationException("ChimeraMonk: no breath projectile for element " + element);
            }
        }

        static BlueprintAbilityResource NewResource(string name, string guid, int baseValue)
        {
            return Create<BlueprintAbilityResource>(name, guid, r =>
            {
                r.LocalizedName = Strings.L("breath.name");
                r.LocalizedDescription = Strings.L("mut.Breath.desc");
                object amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                Fab.FillEmpty(amount);
                SetField(amount, "BaseValue", baseValue);
                SetField(amount, "IncreasedByStat", false);
                SetField(r, "m_MaxAmount", amount);
                SetField(r, "m_UseMax", false);
            });
        }

        public static void Build()
        {
            if (Resource != null) throw new InvalidOperationException("ChimeraMonk: Breath.Build() called twice");
            Resource = NewResource("ChimeraBreathResource", Guids.BreathResource, ChimeraRules.Dials.BreathDailyBase);
            SurgeResource = NewResource("ChimeraBreathSurgeResource", Guids.BreathSurgeResource, ChimeraRules.Dials.BreathSurgeBase);
            var red = Get<BlueprintAbility>(Guids.Game.ScaledFistRedBreath);
            var redProj = red.ComponentsArray.OfType<AbilityDeliverProjectile>().ToList();
            var redRun = red.ComponentsArray.OfType<AbilityEffectRunAction>().ToList();
            if (redProj.Count != 1 || redRun.Count != 1) throw new InvalidOperationException("ChimeraMonk: ScaledFistRedBreathWeaponAbility layout changed (projectile " + redProj.Count + ", run action " + redRun.Count + ")");

            foreach (var e in MutationsPlan.BreathElements)
            {
                var feature = Mutations.Option("Breath", e.Key);
                string baseGuid = (string)typeof(Guids.BreathAbility).GetField(e.Key).GetRawConstantValue();
                string surgeGuid = (string)typeof(Guids.BreathSurgeAbility).GetField(e.Key).GetRawConstantValue();
                var a = MakeAbility("ChimeraBreath" + e.Key, baseGuid, "breath." + e.Key, red, redProj[0], redRun[0], e.Key, Guids.BreathResource, false);
                var s = MakeAbility("ChimeraBreathSurge" + e.Key, surgeGuid, "breath.surge." + e.Key, red, redProj[0], redRun[0], e.Key, Guids.BreathSurgeResource, true);
                Abilities.Add(e.Key, a);
                SurgeAbilities.Add(e.Key, s);
                var facts = Fab.Comp(new AddFacts());
                SetField(facts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(baseGuid), Ref<BlueprintUnitFactReference>(surgeGuid) });
                AddComponents(feature, facts, Grant(Guids.BreathResource), Grant(Guids.BreathSurgeResource));
            }

            // 추가 폐 「분출 +1」(T6 이 남긴 행): IncreaseResourceAmount{분출 자원, 행.Value} — 상시(강화 버프가 아니라 특성에. 분출은 변이약 중에만 쓰이므로 상시 최대 +1 = 「변이약 중 +1」).
            if (BodyMutations.PendingSurge.Count == 0) throw new InvalidOperationException("ChimeraMonk: no pending lung surge row (BodyMutations.Build must run first)");
            foreach (var p in BodyMutations.PendingSurge)
            {
                var inc = Fab.Comp(new IncreaseResourceAmount { Value = p.Item2.Value });
                SetField(inc, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.BreathSurgeResource));
                AddComponents(p.Item1, inc);
            }
            BodyMutations.PendingSurge.Clear();

            var diff = Readback.BreathDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: breath readback: " + string.Join(" | ", diff));
        }

        // 설계 §7 「몽크 레벨 N 당 1d6」 → 엔진 진행(ApplyProgression case 1 Div2 = 레벨 / 2). 엔진에 대응 진행이 없는 N 이면 던진다(조용히 다른 값으로 바꾸지 않음).
        static ContextRankProgression DiceProgression()
        {
            if (ChimeraRules.Dials.BreathLevelsPerDie == 2) return ContextRankProgression.Div2;
            throw new InvalidOperationException("ChimeraMonk: no engine rank progression for 1 die per " + ChimeraRules.Dials.BreathLevelsPerDie + " levels");
        }

        static AddAbilityResources Grant(string resource)
        {
            var g = Fab.Comp(new AddAbilityResources { RestoreAmount = true, UseThisAsResource = false, Amount = 0, RestoreOnLevelUp = false });
            SetField(g, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            return g;
        }

        static BlueprintAbility MakeAbility(string name, string guid, string key, BlueprintAbility red, AbilityDeliverProjectile redProj, AbilityEffectRunAction redRun, string element, string resource, bool surge)
        {
            var a = Fab.Ability(name, guid, key, x =>
            {
                x.Type = red.Type; x.Range = red.Range; x.ActionType = red.ActionType;
                x.CanTargetPoint = red.CanTargetPoint; x.CanTargetEnemies = red.CanTargetEnemies; x.CanTargetFriends = red.CanTargetFriends; x.CanTargetSelf = red.CanTargetSelf;
                x.ShouldTurnToTarget = red.ShouldTurnToTarget; x.SpellResistance = red.SpellResistance; x.EffectOnAlly = red.EffectOnAlly; x.EffectOnEnemy = red.EffectOnEnemy;
                x.Animation = red.Animation; x.HasFastAnimation = red.HasFastAnimation; x.AnimationStyle = red.AnimationStyle;
                SetField(x, "m_Icon", Fab.GetF(red, "m_Icon"));
            });
            var proj = (AbilityDeliverProjectile)CloneBoundary.CopyComponent(redProj, BlueprintClone.KeepShared);
            SetField(proj, "m_Projectiles", new[] { Ref<BlueprintProjectileReference>(ProjectileFor(element)) });
            SetField(proj, "m_Length", new Feet(ChimeraRules.Dials.BreathConeFeet));
            var run = (AbilityEffectRunAction)CloneBoundary.CopyComponent(redRun, BlueprintClone.KeepShared);
            BlueprintClone.CloneElements(run, a, CloneBoundary.ActionDepth); // 실측 7(DealDamage.Duration…m_CustomProperty) — DetachDepth 6 이면 depth exhausted
            var dmg = run.Actions.Actions.OfType<ContextActionDealDamage>().ToList();
            if (dmg.Count != 1 || run.Actions.Actions.Length != 1) throw new InvalidOperationException("ChimeraMonk: red breath run action is not a single DealDamage");
            dmg[0].DamageType.Energy = Fab.EnumOf<DamageEnergyType>(element);
            dmg[0].Value.DiceType = Fab.EnumOf<DiceType>("D" + ChimeraRules.Dials.BreathDie.Sides);
            dmg[0].Value.DiceCountValue = Fab.CvRank(DiceRank);
            dmg[0].HalfIfSaved = true;
            var crc = Fab.RankConfig(DiceRank, DiceProgression(), null);
            var dc = Fab.Comp(new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Wisdom, UseKineticistMainStat = false });
            SetField(dc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.MonkClass));
            var desc = Fab.Comp(new SpellDescriptorComponent { Descriptor = new SpellDescriptorWrapper(Fab.EnumOf<SpellDescriptor>("BreathWeapon")) });
            var logic = Fab.Comp(new AbilityResourceLogic { Amount = 1, CostIsCustom = false });
            SetField(logic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            SetField(logic, "m_IsSpendResource", true);
            var comps = new List<BlueprintComponent> { proj, run, crc, dc, desc, logic };
            if (surge)
            {
                var has = Fab.Comp(new AbilityCasterHasFacts { NeedsAll = false });
                SetField(has, "m_Facts", Mutagen.Buffs.Select(b => Ref<BlueprintUnitFactReference>(b.AssetGuid.ToString())).ToArray());
                comps.Add(has);
            }
            AddComponents(a, comps.ToArray());
            return a;
        }
    }
}
