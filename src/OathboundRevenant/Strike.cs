using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static OathboundRevenant.Parts;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    // 트리거 명세·피해 갈래 = StrikePlan(생산 입력). 스마이트 대상 판정 = SmiteTargetCondition(자체), 라운드 1회 = RoundOnceAction(자체), 낙인 수명 = SmiteLinkComponent(자체).
    public static class Strike
    {
        public static BlueprintBuff SmiteMark, OathRegenSpent, LifeDrainSpent, PursuitSlow, CursedNoHeal;
        public static BlueprintFeature MarkOfDeathTalent;
        public static bool Built { get; private set; }

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("OathboundRevenant: Strike.Build() called twice");
            OathRegenSpent = NewBuff("OathboundRevenantOathRegenSpent", Guids.OathRegenSpent, "oathregeneration.name", "oathregeneration.desc", true);
            LifeDrainSpent = NewBuff("OathboundRevenantLifeDrainSpent", Guids.LifeDrainSpent, "talent.lifedrain.name", "talent.lifedrain.desc", true);
            BuildPursuitSlow();
            CursedNoHeal = NewBuff("OathboundRevenantCursedNoHeal", Guids.CursedNoHealBuff, "talent.cursedstrike.name", "talent.cursedstrike.desc", false);
            AddComponents(CursedNoHeal, Comp(new PreventHealing()));
            BuildSmiteMark();

            // 1레벨 맹세의 재생: 스마이트 대상 명중 → [라운드 1회] 시전자에게 HealTarget(팔라딘 레벨).
            var regen = Archetype.Shell("OathRegeneration");
            var heal = Act(new ContextActionHealTarget { Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = CvZero(), BonusValue = CvRank(AbilityRankType.Default) } });
            // FX(2026-11-15 승인, FxTable): 회복 뒤 안수 FX — ContextActionOnContextCaster 안이라 시전자 위.
            var onCaster = Act(new ContextActionOnContextCaster { Actions = List(heal, OathFx.SpawnAction("OathRegeneration")) });
            AddComponents(regen,
                WeaponTrigger(StrikePlan.Trigger("OathRegeneration"), List(If(Cond(new SmiteTargetCondition()), List(RoundOnce(Guids.OathRegenSpent, onCaster)), List()))),
                RankClassLevel(AbilityRankType.Default, ContextRankProgression.AsIs),
                MarkerClear(Guids.OathRegenSpent));
            Archetype.MarkFilled("OathRegeneration");

            // 1레벨 스마이트 음에너지: 스마이트 대상 명중 → 극성 분기 (Rank)d6, IgnoreCritical, CRC 사용자 표.
            var neg = Archetype.Shell("SmiteNegative");
            var branch = StrikePlan.DamageBranches("SmiteNegative");
            AddComponents(neg,
                WeaponTrigger(StrikePlan.Trigger("SmiteNegative"), List(If(Cond(new SmiteTargetCondition()), List(Polarity(branch, () => CvRank(AbilityRankType.DamageDice))), List()))),
                RankCustom(AbilityRankType.DamageDice, StrikePlan.SmiteNegativeProgression()));
            Archetype.MarkFilled("SmiteNegative");

            // 3레벨 묘지의 공포: 스마이트 시전 뒤(효과 적용 뒤 — AddAbilityUseTrigger.OnAbilityEffectApplied, Step 0 (1)) 대상에게 낙인(영구, 수명은 SmiteLinkComponent).
            var dread = Archetype.Shell("GraveyardDread");
            var use = Comp(new AddAbilityUseTrigger { ForOneSpell = true, AfterCast = true, ActionsOnTarget = true, ActionsOnAllTargets = false, Action = List(If(Cond(new SmiteTargetCondition()), List(ApplyBuff(Guids.SmiteMarkBuff, true, NoDuration(), false)), List())) });
            SetField(use, "m_Ability", Ref<BlueprintAbilityReference>(Guids.Game.SmiteEvilAbility));
            AddComponents(dread, use);
            Archetype.MarkFilled("GraveyardDread");

            // 재주: 추적의 맹세·저주받은 일격은 낙인의 자식 버프로 이미 연결(시전자 사실 조건) — 재주 자체는 표지 특성.
            Talents.MarkFilled("PursuitOath");
            Talents.MarkFilled("CursedStrike");
            MarkOfDeathTalent = Talents.Get("MarkOfDeath");
            Talents.MarkFilled("MarkOfDeath");

            // 생명 흡수(9): 스마이트 대상 명중 → [라운드 1회] 음성 레벨(EnergyDrain Temporary, 1분).
            var drain = Talents.Get("LifeDrain");
            var dd = Act(new ContextActionDealDamage
            {
                DamageType = new Kingmaker.RuleSystem.Rules.Damage.DamageTypeDescription(),
                Duration = new ContextDurationValue { Rate = DurationRate.Minutes, DiceType = DiceType.Zero, DiceCountValue = CvZero(), BonusValue = Cv(RevenantRules.Dials.LifeDrainDurationMinutes) },
                Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = CvZero(), BonusValue = Cv(RevenantRules.Dials.LifeDrainLevels) },
                EnergyDrainType = EnergyDrainType.Temporary,
                IgnoreCritical = true,
            });
            SetField(dd, "m_Type", Enum.Parse(FindField(typeof(ContextActionDealDamage), "m_Type").FieldType, "EnergyDrain"));
            AddComponents(drain,
                // FX(2026-11-15 승인, FxTable): 흡혈귀 에너지 흡수 FX — 라운드 1회 안(실제로 흡수할 때만), 대상 위.
                WeaponTrigger(StrikePlan.Trigger("LifeDrain"), List(If(Cond(new SmiteTargetCondition()), List(RoundOnce(Guids.LifeDrainSpent, dd, OathFx.SpawnAction("LifeDrain"))), List()))),
                MarkerClear(Guids.LifeDrainSpent));
            Talents.MarkFilled("LifeDrain");

            // 죽음의 칼날(12): 모든 근접 명중 → 극성 분기 1d6, IgnoreCritical(스마이트 조건 없음).
            var blade = Talents.Get("DeathBlade");
            var bb = StrikePlan.DamageBranches("DeathBlade");
            AddComponents(blade, WeaponTrigger(StrikePlan.Trigger("DeathBlade"), List(Polarity(bb, () => Cv(RevenantRules.Dials.DeathBladeDice)))));
            Talents.MarkFilled("DeathBlade");

            Verify();
            Built = true;
        }

        // 추적의 맹세 감속 = 「현재 속도의 절반」(소유자 결정 2026-11-17, 점검 S11).
        //   이전 BuffMovementSpeed{CappedOnMultiplier, 0.5} 는 OnTurnOn IL_005D~0080 에서 수정치 = 「기본」 속도 × (상한 − 1) 이라 갑옷으로 20 이 된 적이 30 × −0.5 = −15 → 5 가 됐다.
        //   지금 = 본편 감속 계열과 같은 부품 AddCondition{Slowed}(본편 DebilitatingInjuryHamperedEffectBuff·DuelistCripplingCriticalSlowEffectBuff 「속도 절반」 이 이 부품 하나, blueprints.zip 2026-11-17).
        //   엔진 UnitEntityData.CalculateSpeedModifier IL_0093~00AE: HasCondition(Slowed) → 배수 × 0.5, 이 배수는 CombatSpeedMps·ModifiedSpeedMps 가 「현재」 속도(Stats.Speed.Mps — 갑옷·버프 반영)에 곱한다.
        //   0.5 는 엔진 상수다 → 다이얼 PursuitSpeedMultiplier 와 같아야 한다(시험 `engine: pursuit slowed halves current speed` 가 IL 의 상수와 대조).
        //   부수 효과: 본편 자유 이동(FreedomOfMovement*) 등 Slowed 면역은 이 감속도 막는다(본편 「Hampered」 와 같음).
        static void BuildPursuitSlow()
        {
            PursuitSlow = NewBuff("OathboundRevenantPursuitSlow", Guids.PursuitSlowBuff, "talent.pursuitoath.name", "talent.pursuitoath.desc", false);
            AddComponents(PursuitSlow, Comp(new AddCondition { Condition = PursuitCondition }));
            VerifyPursuitSlow();
        }

        // 추적의 맹세가 거는 상태(엔진 배수 0.5 — 위 주석). 시험이 IL 에서 이 값을 읽는다.
        internal const Kingmaker.UnitLogic.UnitCondition PursuitCondition = Kingmaker.UnitLogic.UnitCondition.Slowed;

        // 되읽기: 감속 버프의 부품 = AddCondition{Slowed} 정확히 1개, 기본 속도 기준 BuffMovementSpeed 0개(다시 들어오면 「기본 속도의 절반을 뺌」 결함이 돌아온다).
        static void VerifyPursuitSlow()
        {
            var conds = PursuitSlow.ComponentsArray.OfType<AddCondition>().ToList();
            if (conds.Count != 1 || conds[0].Condition != PursuitCondition) throw new InvalidOperationException("OathboundRevenant: PursuitSlow must carry exactly one AddCondition{" + PursuitCondition + "} (got " + conds.Count + ")");
            if (PursuitSlow.ComponentsArray.OfType<BuffMovementSpeed>().Any()) throw new InvalidOperationException("OathboundRevenant: PursuitSlow carries a BuffMovementSpeed (base-speed based — the 2026-11-17 S11 defect)");
        }

        // 낙인 버프(대상): 부착 시 흔들림(자식) + 시전자가 추적의 맹세면 감속(자식) + 저주받은 일격이면 치유 불가(자식) · 수명 = SmiteLinkComponent.
        static void BuildSmiteMark()
        {
            SmiteMark = NewBuff("OathboundRevenantSmiteMark", Guids.SmiteMarkBuff, "graveyarddread.name", "graveyarddread.desc", true);
            var act = Comp(new AddFactContextActions
            {
                Activated = List(
                    ApplyBuff(Guids.Game.Shaken, true, NoDuration(), true),
                    If(CasterHasFact(Guids.Talent("PursuitOath"), false), List(ApplyBuff(Guids.PursuitSlowBuff, true, NoDuration(), true)), List()),
                    If(CasterHasFact(Guids.Talent("CursedStrike"), false), List(ApplyBuff(Guids.CursedNoHealBuff, true, NoDuration(), true)), List())),
                Deactivated = List(),
                NewRound = List(),
                Dispose = List(),
            });
            AddComponents(SmiteMark, act, Comp(new SmiteLinkComponent()));
        }

        static AddInitiatorAttackWithWeaponTrigger WeaponTrigger(StrikeTrigger t, ActionList actions)
        {
            if (t.Kind != "Weapon") throw new InvalidOperationException("OathboundRevenant: strike trigger " + t.Key + " is not a weapon trigger");
            var c = Comp(new AddInitiatorAttackWithWeaponTrigger { OnlyHit = t.OnlyHit, ActionsOnInitiator = false, Action = actions });
            if (t.Range == "Melee") { c.CheckWeaponRangeType = true; c.RangeType = WeaponRangeType.Melee; }
            else if (t.Range != "Any") throw new InvalidOperationException("OathboundRevenant: strike trigger " + t.Key + " range " + t.Range + " unknown");
            return c;
        }

        static RoundOnceAction RoundOnce(string marker, params GameAction[] inner)
        {
            if (inner == null || inner.Length == 0 || inner.Any(x => x == null)) throw new ArgumentException("OathboundRevenant: RoundOnce needs non-null inner actions");
            var a = Act(new RoundOnceAction { Actions = inner });
            a.m_Marker = Ref<BlueprintBuffReference>(marker);
            return a;
        }

        // 표지 해제 = 소유자(팔라딘) 새 라운드 · 특성 해제.
        static AddFactContextActions MarkerClear(string marker)
        {
            return Comp(new AddFactContextActions { Activated = List(), Deactivated = List(RemoveBuff(marker, false)), NewRound = List(RemoveBuff(marker, false)), Dispose = List() });
        }

        // 빌드 되읽기: 무기 트리거 4 + 시전 트리거 1 의 (OnlyHit, 범위, 스마이트 조건, 표지) == StrikePlan.Triggers · 극성 분기(형식·주사위·IgnoreCritical) == DamageBranches.
        static void Verify()
        {
            foreach (var t in StrikePlan.Triggers)
            {
                var owner = t.Key == "LifeDrain" || t.Key == "DeathBlade" ? Talents.Get(t.Key) : Archetype.Shell(t.Key);
                ActionList acts;
                if (t.Kind == "Weapon")
                {
                    var w = owner.ComponentsArray.OfType<AddInitiatorAttackWithWeaponTrigger>().SingleOrDefault();
                    if (w == null || w.OnlyHit != t.OnlyHit || (t.Range == "Melee") != (w.CheckWeaponRangeType && w.RangeType == WeaponRangeType.Melee)) throw new InvalidOperationException("OathboundRevenant: " + t.Key + " weapon trigger differs from StrikePlan");
                    acts = w.Action;
                }
                else
                {
                    var u = owner.ComponentsArray.OfType<AddAbilityUseTrigger>().SingleOrDefault();
                    if (u == null || !u.AfterCast || !u.ActionsOnTarget || u.Ability == null || u.Ability.AssetGuid.ToString() != Guids.Game.SmiteEvilAbility) throw new InvalidOperationException("OathboundRevenant: " + t.Key + " ability-use trigger differs from StrikePlan");
                    acts = u.Action;
                }
                var top = acts.Actions.Single() as Conditional;
                bool smiteGated = top != null && top.ConditionsChecker.Conditions.Length == 1 && top.ConditionsChecker.Conditions[0] is SmiteTargetCondition;
                if (smiteGated != t.RequiresSmiteTarget) throw new InvalidOperationException("OathboundRevenant: " + t.Key + " smite gating " + smiteGated + " != StrikePlan " + t.RequiresSmiteTarget);
                var inner = smiteGated ? top.IfTrue.Actions : acts.Actions;
                var ro = inner.OfType<RoundOnceAction>().SingleOrDefault();
                string marker = ro == null ? null : (ro.m_Marker == null ? "null" : ro.m_Marker.Guid.ToString());
                string want = t.RoundMarker == null ? null : (t.RoundMarker == "OathRegenSpent" ? Guids.OathRegenSpent : Guids.LifeDrainSpent);
                if (marker != want) throw new InvalidOperationException("OathboundRevenant: " + t.Key + " round marker " + (marker ?? "none") + " != StrikePlan " + (want ?? "none"));
                // FX 되읽기(FxTable): 생명 흡수 = [피해, SpawnFx], 맹세의 재생 = [시전자에게 [회복, SpawnFx]].
                if (t.Key == "LifeDrain")
                {
                    if (!Shape(ro.Actions, typeof(ContextActionDealDamage), typeof(ContextActionSpawnFx))) throw new InvalidOperationException("OathboundRevenant: LifeDrain round-once actions != [DealDamage, SpawnFx]");
                    OathFx.VerifySpawn(ro.Actions.Last(), "LifeDrain");
                }
                else if (t.Key == "OathRegeneration")
                {
                    var oc = Shape(ro.Actions, typeof(ContextActionOnContextCaster)) ? (ContextActionOnContextCaster)ro.Actions.Single() : null;
                    if (oc == null || !Shape(oc.Actions.Actions, typeof(ContextActionHealTarget), typeof(ContextActionSpawnFx))) throw new InvalidOperationException("OathboundRevenant: OathRegeneration round-once actions != [OnContextCaster[HealTarget, SpawnFx]]");
                    OathFx.VerifySpawn(oc.Actions.Actions.Last(), "OathRegeneration");
                }
            }
            VerifyPolarity(Archetype.Shell("SmiteNegative").ComponentsArray.OfType<AddInitiatorAttackWithWeaponTrigger>().Single().Action, StrikePlan.DamageBranches("SmiteNegative"), true);
            VerifyPolarity(Talents.Get("DeathBlade").ComponentsArray.OfType<AddInitiatorAttackWithWeaponTrigger>().Single().Action, StrikePlan.DamageBranches("DeathBlade"), false);
        }

        // 행동 목록의 정확한 형식 순서(null 원소 = 불일치).
        static bool Shape(GameAction[] actions, params Type[] types)
        {
            return actions != null && actions.Select(x => x == null ? null : x.GetType()).SequenceEqual(types);
        }

        internal static void VerifyPolarity(ActionList acts, DamageBranch b, bool smiteGated)
        {
            var list = acts.Actions;
            if (smiteGated) list = ((Conditional)list.Single()).IfTrue.Actions;
            var pol = list.Single() as Conditional;
            if (pol == null || pol.ConditionsChecker.Conditions.Length != 1) throw new InvalidOperationException("OathboundRevenant: " + b.Key + " polarity branch is not a single-condition Conditional");
            // 신성 갈래 = [피해, SpawnFx(FxTable Divine:<갈래>)], 기본 갈래 = [피해](Parts.Polarity).
            if (!Shape(pol.IfTrue.Actions, typeof(ContextActionDealDamage), typeof(ContextActionSpawnFx))) throw new InvalidOperationException("OathboundRevenant: " + b.Key + " affinity branch is [" + string.Join(", ", pol.IfTrue.Actions.Select(x => x == null ? "null" : x.GetType().Name)) + "], expected [DealDamage, SpawnFx]");
            OathFx.VerifySpawn(pol.IfTrue.Actions.Last(), FxTable.Divine + b.Key);
            var aff = pol.IfTrue.Actions.First() as ContextActionDealDamage;
            var plain = pol.IfFalse.Actions.Single() as ContextActionDealDamage;
            if (aff == null || plain == null) throw new InvalidOperationException("OathboundRevenant: " + b.Key + " polarity branches are not DealDamage");
            if (aff.DamageType.Energy.ToString() != b.AffinityEnergy || plain.DamageType.Energy.ToString() != b.PlainEnergy) throw new InvalidOperationException("OathboundRevenant: " + b.Key + " energy types differ from StrikePlan");
            if (aff.IgnoreCritical != b.IgnoreCritical || plain.IgnoreCritical != b.IgnoreCritical) throw new InvalidOperationException("OathboundRevenant: " + b.Key + " IgnoreCritical differs from StrikePlan");
            if (aff.Value.DiceType != Die(b.Sides) || plain.Value.DiceType != Die(b.Sides) || aff.Value.DiceCountValue.ValueType != plain.Value.DiceCountValue.ValueType || aff.Value.DiceCountValue.Value != plain.Value.DiceCountValue.Value || aff.Value.DiceCountValue.ValueRank != plain.Value.DiceCountValue.ValueRank)
                throw new InvalidOperationException("OathboundRevenant: " + b.Key + " dice differ between the two branches or from StrikePlan");
        }
    }
}
