using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Warlock
{
    // 블라스트 표식(설계 §4.4 「표식」). 블라스트 식별은 이 컴포넌트 하나로만(C4 ④). Kind 는 빌드 때 정해지는 설정값(유닛 상태 아님 — 인스턴스 상태 없음 규칙의 예외는 이 설정 1개).
    public class WarlockBlastMarker : BlueprintComponent
    {
        public BlastKind Kind;
    }

    // 블라스트 능력 18(그룹 6 × Close/Medium/연속 사격). 명세 = KitSpec.Rays(). 설계 §4.4·§4.5, 정찰 §16-T3(광선 원형 RayOfFrost jbp 값).
    public static class Blasts
    {
        static readonly Dictionary<string, BlueprintAbility> byGuid = new Dictionary<string, BlueprintAbility>();
        public static bool Built { get; private set; }

        // 광선 원형 RayOfFrost 의 연출 값(jbp, 정찰 §16-T3): Animation Directional · 투사체 Type Simple · 무기 RayItem.
        // ContextActionDealDamage.m_Type(중첩 열거 Type 은 비공개 — 정찰 §16-T3 {Damage 0, AbilityDamage 1, EnergyDrain 2}). 새 액션의 기본값 0 을 되읽어 확인한다.
        const int ContextActionDealDamageTypeDamage = 0;
        const UnitAnimationActionCastSpell.CastAnimationStyle RayAnimation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;

        public static void Build()
        {
            if (Built || byGuid.Count > 0) throw new InvalidOperationException("Warlock: Blasts.Build() called twice");
            Get<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(Guids.Game.RayItem); // 해석 확인(없으면 Get 이 던진다)
            foreach (var r in KitSpec.Rays()) byGuid.Add(r.Guid, BuildRay(r));
            if (byGuid.Count != 18) throw new InvalidOperationException("Warlock: built " + byGuid.Count + " blasts, expected 18 (6 groups × Close/Medium/Double)");
            Built = true;
        }

        static BlueprintAbility BuildRay(RaySpec r)
        {
            var icon = KitBuild.Icon(r.IconGuid);
            var name = "Warlock" + r.Key + "Ability";
            var ab = Create<BlueprintAbility>(name, r.Guid, a =>
            {
                KitBuild.Names(a, r.Key, icon);
                a.Type = AbilityType.Supernatural;
                a.Range = (AbilityRange)EngineCodes.Range(r.Range);
                a.ActionType = (UnitCommand.CommandType)EngineCodes.ActionStandard;
                a.CanTargetEnemies = true;
                a.CanTargetFriends = false;
                a.CanTargetSelf = false;
                a.CanTargetPoint = false;
                a.SpellResistance = true; // T4 hit-sr-blocked-rank-5 가 SR 굴림을 요구한다(RuleSpellResistanceCheck.HasResistanceRoll = Ability.SpellResistance — BM BlastStrike.cs:166)
                a.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                a.EffectOnAlly = AbilityEffectOnUnit.None;
                a.Animation = RayAnimation;
                a.ShouldTurnToTarget = true;
                // 2026-11-16(툴팁 `<null>` 결함): 지속·내성 = 빈 키(칸 숨김). 본뜬 본편 RayOfFrost·ScorchingRay.jbp 의 두 칸 모두 { m_Key "", Shared null } — 시험 `tooltip:` 이 대조.
                // 즉시 피해·명중 굴림(내성 굴림 없음)이라 본편 광선과 같다. null 로 두지 않는다.
                a.LocalizedDuration = NoText();
                a.LocalizedSavingThrow = NoText();
            });
            KitBuild.RequireTooltipFields(ab, "");

            var deliver = new AbilityDeliverProjectile
            {
                Type = AbilityProjectileType.Simple,
                NeedAttackRoll = true,
                ReplaceAttackRollBonusStat = true,
                AttackRollBonusStat = StatType.Charisma,
                UseMaxProjectilesCount = false,
                DelayBetweenProjectiles = r.Delay,
            };
            SetField(deliver, "m_Projectiles", Enumerable.Range(0, r.Projectiles).Select(i => Ref<BlueprintProjectileReference>(r.ProjectileGuid)).ToArray());
            SetField(deliver, "m_Weapon", Ref<BlueprintItemWeaponReference>(Guids.Game.RayItem));

            var deal = new ContextActionDealDamage
            {
                DamageType = r.Element == BlastElement.Force ? new DamageTypeDescription { Type = DamageType.Force } : new DamageTypeDescription { Type = DamageType.Energy, Energy = KitBuild.Energy(r.Element) },
                Duration = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }, BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 } },
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.D6,
                    DiceCountValue = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.DamageDice },
                    BonusValue = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.DamageBonus },
                },
            };
            // 액션 요소의 Owner·name 은 JSON 로드 때만 채워진다(PhantomBlade StandBlueprints·BM BlastStrike.cs:106-109 선례) → 직접 단다.
            deal.name = "$ContextActionDealDamage$" + r.Guid + "#0";
            deal.Owner = ab;
            var run = new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { deal } } };

            var dice = KitBuild.RankOf(KitSpec.DiceRank);
            var bonus = KitBuild.RankOf(KitSpec.BonusRank);
            var cl = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma, UseKineticistMainStat = false };
            SetField(cl, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.WitchClass));
            var marker = new WarlockBlastMarker { Kind = r.Kind };
            AddComponents(ab, deliver, run, dice, bonus, cl, marker);

            // 되읽기(조용한 기본값 금지): 컴포넌트 형식 순서 · 투사체 수·대상 · 무기 · 피해 형식 · 시전자 레벨 직업 · 표식 1개.
            var expect = new[] { typeof(AbilityDeliverProjectile), typeof(AbilityEffectRunAction), typeof(ContextRankConfig), typeof(ContextRankConfig), typeof(ContextCalculateAbilityParamsBasedOnClass), typeof(WarlockBlastMarker) };
            var got = ab.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!got.SequenceEqual(expect)) throw new InvalidOperationException("Warlock: " + name + " components [" + string.Join(", ", got.Select(t => t.Name)) + "]");
            var projs = deliver.Projectiles.ToArray();
            if (projs.Length != r.Projectiles || projs.Any(p => p == null || p.AssetGuid.ToString() != r.ProjectileGuid) || deliver.Weapon == null || deliver.Weapon.AssetGuid.ToString() != Guids.Game.RayItem
                || !deliver.NeedAttackRoll || !deliver.ReplaceAttackRollBonusStat || deliver.AttackRollBonusStat != StatType.Charisma || deliver.UseMaxProjectilesCount || deliver.DelayBetweenProjectiles != r.Delay)
                throw new InvalidOperationException("Warlock: " + name + " AbilityDeliverProjectile readback failed");
            if (Convert.ToInt32(KitBuild.GetF(deal, "m_Type")) != ContextActionDealDamageTypeDamage || deal.Value.DiceType != DiceType.D6 || deal.DamageType.Type != (r.Element == BlastElement.Force ? DamageType.Force : DamageType.Energy)
                || (r.Element != BlastElement.Force && deal.DamageType.Energy != KitBuild.Energy(r.Element)))
                throw new InvalidOperationException("Warlock: " + name + " ContextActionDealDamage readback failed");
            if (cl.CharacterClass == null || cl.CharacterClass.AssetGuid.ToString() != Guids.Game.WitchClass || cl.StatType != StatType.Charisma)
                throw new InvalidOperationException("Warlock: " + name + " ContextCalculateAbilityParamsBasedOnClass readback failed");
            if (ab.ComponentsArray.OfType<WarlockBlastMarker>().Count() != 1) throw new InvalidOperationException("Warlock: " + name + " must carry exactly one WarlockBlastMarker");
            if (!ab.SpellResistance || ab.CanTargetFriends || ab.CanTargetSelf || ab.CanTargetPoint || !ab.CanTargetEnemies || ab.Type != AbilityType.Supernatural)
                throw new InvalidOperationException("Warlock: " + name + " targeting/SR readback failed");
            // 사거리·행동 종류(리뷰 T3 I-1·I-3): 엔진 열거 이름 == 명세 이름(Close/Medium) · Standard.
            if (ab.Range.ToString() != r.Range.ToString() || ab.ActionType != UnitCommand.CommandType.Standard)
                throw new InvalidOperationException("Warlock: " + name + " range/action readback " + ab.Range + "/" + ab.ActionType + ", expected " + r.Range + "/Standard");
            return ab;
        }





        // 블라스트 식별(T4 과열 계수가 쓴다): 표식 컴포넌트 1개면 그것, 0개면 null(= 블라스트 아님 — 입력 결측이 아니라 확정된 「아님」), 2개 이상이면 던진다.
        public static WarlockBlastMarker IsBlast(BlueprintAbility a)
        {
            if (!Built) throw new InvalidOperationException("Warlock: Blasts.IsBlast before Blasts.Build()");
            if (a == null) throw new ArgumentNullException("a");
            if (a.ComponentsArray == null) throw new InvalidOperationException("Warlock: " + a.name + " has a null ComponentsArray (cannot judge blast marker)");
            var m = a.ComponentsArray.OfType<WarlockBlastMarker>().ToArray();
            if (m.Length > 1) throw new InvalidOperationException("Warlock: " + a.name + " carries " + m.Length + " WarlockBlastMarker components");
            return m.Length == 1 ? m[0] : null;
        }
    }
}
