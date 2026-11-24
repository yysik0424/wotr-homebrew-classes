using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static OathboundRevenant.Parts;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    public static class Channel
    {
        public static BlueprintAbility Ability;
        public static BlueprintAbility DeathsTouchAbility;
        public static BlueprintUnitProperty DiceProperty;
        public static bool Built { get; private set; }

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("OathboundRevenant: Channel.Build() called twice");
            DiceProperty = Create<BlueprintUnitProperty>("OathboundRevenantChannelDice", Guids.ChannelDiceProperty, p => { FillEmpty(p); });
            AddComponents(DiceProperty, Comp(new ChannelDiceGetter()));

            Ability = Create<BlueprintAbility>("OathboundRevenantNegativeChannel", Guids.ChannelAbility, a =>
            {
                SetField(a, "m_DisplayName", Strings.L("negativechannel.name"));
                SetField(a, "m_Description", Strings.L("negativechannel.desc"));
                // 2026-11-16(툴팁 `<null>` 결함): 짧은 설명 = 문구 없음, 지속·내성 칸 = TooltipPlan(본편 채널과 같이 두 칸 모두 숨김).
                Tooltips.NoShortDescription(a);
                Tooltips.Ability(a);
                a.Type = (AbilityType)Enum.Parse(typeof(AbilityType), ChannelPlan.AbilityKind);
                a.Range = (AbilityRange)Enum.Parse(typeof(AbilityRange), ChannelPlan.Range);
                a.ActionType = (UnitCommand.CommandType)Enum.Parse(typeof(UnitCommand.CommandType), ChannelPlan.ActionType);
                a.CanTargetSelf = true; a.CanTargetEnemies = false; a.CanTargetFriends = false; a.CanTargetPoint = false;
                FillEmpty(a);
            });
            var harm = Get<BlueprintAbility>(Guids.Game.ChannelEnergyPaladinHarm);
            var negCleric = Get<BlueprintAbility>(Guids.Game.ChannelNegativeEnergy);
            var align = harm.ComponentsArray.OfType<AbilityCasterAlignment>().SingleOrDefault();
            var desc = negCleric.ComponentsArray.OfType<SpellDescriptorComponent>().SingleOrDefault();
            if (align == null || desc == null) throw new InvalidOperationException("OathboundRevenant: vanilla channel components (AbilityCasterAlignment / SpellDescriptorComponent) not found");

            var targets = Comp(new AbilityTargetsAround());
            SetField(targets, "m_Radius", new Feet(RevenantRules.Dials.ChannelRadiusFeet));
            SetField(targets, "m_TargetType", Enum.Parse(FindField(typeof(AbilityTargetsAround), "m_TargetType").FieldType, ChannelPlan.TargetType));
            SetField(targets, "m_IncludeDead", false);
            SetField(targets, "m_Condition", new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[0] });

            var damage = DealEnergy(StrikePlan.Negative, CvRank(AbilityRankType.DamageDice), RevenantRules.Dials.SmiteDie, false);
            damage.IsAoE = true; damage.HalfIfSaved = true;
            var heal = Act(new ChannelSelfHealAction { DamageAction = damage });
            var save = Act(new ContextActionSavingThrow { Type = SavingThrowType.Will, HasCustomDC = false, UseDCFromContextSavingThrow = false, FromBuff = false, CustomDC = CvZero(), Actions = List(damage) });
            var run = Comp(new AbilityEffectRunAction { SavingThrowType = SavingThrowType.Unknown, Actions = List(If(LivingEnemyConditions(), List(save, heal), List())) });

            var rank = Comp(new ContextRankConfig());
            SetField(rank, "m_Type", AbilityRankType.DamageDice);
            SetField(rank, "m_BaseValueType", ContextRankBaseValueType.CustomProperty);
            SetField(rank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(Guids.ChannelDiceProperty));
            SetField(rank, "m_Progression", ContextRankProgression.AsIs);
            var dc = Comp(new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma, UseKineticistMainStat = false });
            SetField(dc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.PaladinClass));

            AddComponents(Ability, ResourceLogic("NegativeChannel"), targets, run, rank, dc, CloneBoundary.CopyComponent(align, KeepShared, RevenantRules.Dials.CloneDepth), CloneBoundary.CopyComponent(desc, KeepShared, RevenantRules.Dials.CloneDepth),
                OathFx.AbilityFx("NegativeChannel"));   // FX(2026-11-15 승인, FxTable): 본편 ChannelNegativeEnergy 의 시전자 폭발 + 시전 동작 + 미리 읽기
            OathFx.CopyAnimation(Ability, "NegativeChannel");
            OathFx.SetResourceIds(Ability, "NegativeChannel");
            Hands.RebindElements(Ability);
            var errs = ChannelPlan.VerifyTree(DescribeRun(run));
            if (errs.Count > 0) throw new InvalidOperationException("OathboundRevenant: negative channel tree: " + string.Join("; ", errs));

            var feat = Archetype.Shell("NegativeChannel");
            var facts = new AddFacts();
            SetField(facts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.ChannelAbility) });
            AddComponents(feat, facts);
            Archetype.MarkFilled("NegativeChannel");
            Talents.MarkFilled("EmpoweredChannel"); // 표지 재주 — ChannelDiceGetter 가 읽는다

            BuildDeathsTouch();
            // 자원 되읽기는 여기서 하지 않는다: TerribleGaze·WailOfTheDead(Fear)·CommandUndead(Survival) 행은 아직 등록 전.
            // 유일한 호출 = Survival.Build(모든 행이 등록된 뒤). 시험 「wiring: resource verify order」가 위치를 고정한다.
            Built = true;
        }

        static bool KeepShared(object v) { return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent; }

        // 살아 있는 적(#3) = 적 ∧ ¬언데드 ∧ ¬구성체 ∧ ¬음에너지 친화 — 순서 = ChannelPlan.LivingCondition.
        internal static Condition[] LivingEnemyConditions()
        {
            return new Condition[] { Cond(new ContextConditionIsEnemy { Not = false }), HasFact(Guids.Game.UndeadType, true), HasFact(Guids.Game.ConstructType, true), HasFact(Guids.Game.NegativeEnergyAffinity, true) };
        }

        internal static AbilityResourceLogic ResourceLogic(string abilityKey)
        {
            var row = ResourcePlan.Row(abilityKey);
            var c = Comp(new AbilityResourceLogic { Amount = row.Amount, CostIsCustom = false });
            SetField(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(row.ResourceGuid));
            SetField(c, "m_IsSpendResource", true);
            return c;
        }

        static void BuildDeathsTouch()
        {
            var loh = Get<BlueprintAbility>(Guids.Game.LayOnHandsOthers);
            var touch = loh.ComponentsArray.OfType<AbilityDeliverTouch>().SingleOrDefault();
            if (touch == null) throw new InvalidOperationException("OathboundRevenant: vanilla LayOnHandsOthers has no AbilityDeliverTouch to copy");
            DeathsTouchAbility = Create<BlueprintAbility>("OathboundRevenantDeathsTouch", Guids.DeathsTouchAbility, a =>
            {
                SetField(a, "m_DisplayName", Strings.L("talent.deathstouch.name"));
                SetField(a, "m_Description", Strings.L("talent.deathstouch.desc"));
                // 2026-11-16(툴팁 `<null>` 결함): 짧은 설명 = 문구 없음, 지속·내성 칸 = TooltipPlan(본뜬 LayOnHandsOthers 와 같이 두 칸 모두 숨김).
                Tooltips.NoShortDescription(a);
                Tooltips.Ability(a);
                a.Type = AbilityType.Supernatural;
                a.Range = AbilityRange.Touch;
                a.ActionType = UnitCommand.CommandType.Standard;
                a.CanTargetEnemies = true; a.CanTargetFriends = false; a.CanTargetSelf = false; a.CanTargetPoint = false;
                a.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                a.Animation = loh.Animation;
                FillEmpty(a);
            });
            var b = StrikePlan.DamageBranches("DeathsTouch");
            var run = Comp(new AbilityEffectRunAction { SavingThrowType = SavingThrowType.Unknown, Actions = List(Polarity(b, () => CvRank(AbilityRankType.Default))) });
            AddComponents(DeathsTouchAbility, ResourceLogic("DeathsTouch"), CloneBoundary.CopyComponent(touch, KeepShared, RevenantRules.Dials.CloneDepth), Comp(new AbilityTargetIsAlly { Not = true }), run,
                RankClassLevel(AbilityRankType.Default, ContextRankProgression.Div2));
            Hands.RebindElements(DeathsTouchAbility);
            OathFx.SetResourceIds(DeathsTouchAbility, FxTable.Divine + "DeathsTouch");   // 신성 갈래 FX(Parts.Polarity) 미리 읽기
            var f = Talents.Get("DeathsTouch");
            var facts = new AddFacts();
            SetField(facts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.DeathsTouchAbility) });
            AddComponents(f, facts);
            Strike.VerifyPolarity(run.Actions, b, false);
            Talents.MarkFilled("DeathsTouch");
        }

        // 빌드 되읽기: ResourcePlan 의 능력마다 (요구 자원, 소비 플래그, 양) == 행.
        internal static void VerifyResources()
        {
            foreach (var r in ResourcePlan.Rows)
            {
                BlueprintAbility a = r.AbilityKey == "RevenantLayOnHands" ? Hands.Ability : r.AbilityKey == "NegativeChannel" ? Ability : r.AbilityKey == "DeathsTouch" ? DeathsTouchAbility : AbilityFor(r.AbilityKey);
                if (a == null) { if (r.Consumer == "Ability") throw new InvalidOperationException("OathboundRevenant: resource row " + r.AbilityKey + " has no built ability"); continue; }
                var l = a.ComponentsArray.OfType<AbilityResourceLogic>().SingleOrDefault();
                if (l == null || l.RequiredResource == null || l.RequiredResource.AssetGuid.ToString() != r.ResourceGuid || !l.IsSpendResource || l.Amount != r.Amount)
                    throw new InvalidOperationException("OathboundRevenant: " + r.AbilityKey + " resource wiring differs from ResourcePlan (" + (l == null ? "no AbilityResourceLogic" : "resource " + (l.RequiredResource == null ? "null" : l.RequiredResource.AssetGuid.ToString()) + ", spend " + l.IsSpendResource + ", amount " + l.Amount) + ")");
            }
        }

        // 이후 Task 의 능력(응시·외침·지배)을 찾는 자리 — Fear/Survival 이 등록한다.
        internal static readonly Dictionary<string, BlueprintAbility> MoreAbilities = new Dictionary<string, BlueprintAbility>();
        static BlueprintAbility AbilityFor(string key) { BlueprintAbility a; return MoreAbilities.TryGetValue(key, out a) ? a : null; }

        // 게임 행동 트리 → ChannelPlan.TreeNode(빌드 되읽기).
        internal static TreeNode DescribeRun(AbilityEffectRunAction run)
        {
            var root = new TreeNode("Run", run.SavingThrowType.ToString());
            foreach (var a in run.Actions.Actions) root.Children.Add(Describe(a));
            return root;
        }

        static TreeNode Describe(GameAction a)
        {
            var cond = a as Conditional;
            if (cond != null)
            {
                var n = new TreeNode("Conditional", string.Join(",", cond.ConditionsChecker.Conditions.Select(DescribeCondition)));
                foreach (var x in cond.IfTrue.Actions) n.Children.Add(Describe(x));
                foreach (var x in cond.IfFalse.Actions) n.Otherwise.Add(Describe(x));
                return n;
            }
            var save = a as ContextActionSavingThrow;
            if (save != null) { var n = new TreeNode("Save", save.Type.ToString()); foreach (var x in save.Actions.Actions) n.Children.Add(Describe(x)); return n; }
            var dmg = a as ContextActionDealDamage;
            if (dmg != null) return new TreeNode("Damage", dmg.DamageType.Energy.ToString(), dmg.IsAoE, dmg.HalfIfSaved);
            if (a is ChannelSelfHealAction) return new TreeNode("Heal", null);
            return new TreeNode(a.GetType().Name, null);
        }

        static string DescribeCondition(Condition c)
        {
            if (c is ContextConditionIsEnemy) return (c.Not ? "!" : "") + "IsEnemy";
            var hf = c as ContextConditionHasFact;
            if (hf != null)
            {
                string g = hf.Fact == null ? "null" : hf.Fact.AssetGuid.ToString();
                string n = g == Guids.Game.UndeadType ? "UndeadType" : g == Guids.Game.ConstructType ? "ConstructType" : g == Guids.Game.NegativeEnergyAffinity ? "NegativeEnergyAffinity" : g;
                return (c.Not ? "!" : "") + n;
            }
            return c.GetType().Name;
        }
    }

    // ½ 팔라딘 레벨 + 강화 채널 2 를 한 굴림으로(CRC CustomProperty — 본편 CRC 는 특성 보유 가산 불가, 부록 A-e).
    // 4레벨 미만(채널 없음) = 판정 불가 → Report + FailureLedger(현재 문맥) + 0(뒤 회복 부품은 멈춘다, #13).
    public class ChannelDiceGetter : PropertyValueGetter
    {
        protected override int GetBaseValue(UnitEntityData unit)
        {
            int result = 0;
            var data = ContextData<MechanicsContext.Data>.Current;
            var ctx = data == null ? null : data.Context;
            if (!Guard.Run(ErrorLedger.SiteChannelDice, ctx, () =>
            {
                if (unit == null) throw new InvalidOperationException("no unit");
                var paladin = Get<BlueprintCharacterClass>(Guids.Game.PaladinClass);
                int level = unit.Progression.GetClassLevel(paladin);
                if (level < RevenantRules.Levels.ChannelLevel)
                {
                    RevenantErrors.Report(ErrorLedger.SiteChannelDice, unit.CharacterName + " paladin level " + level + " < " + RevenantRules.Levels.ChannelLevel + " (no channel dice)");
                    if (ctx != null) FailureLedger.Mark(ctx, ErrorLedger.SiteChannelDice);
                    return;
                }
                result = RevenantRules.ChannelDice(level, unit.Descriptor.HasFact(Talents.Get("EmpoweredChannel")));
            }, RevenantErrors.Sink)) return 0; // 실패(주입·예외)는 계수 + 장부 기록됨 — 주사위 0
            return result;
        }
    }

    // 채널 자기 회복(설계 §8-b): 같은 시전의 엔진 AoE 첫 굴림(Context.AoEDamage[피해 액션] — ContextActionDealDamage.DealHitPointsDamage IL_004A~00A3, 반감 전 RollResult)으로 시전자 1회.
    // 살아 있는 적 분기 안에서만 돈다(맞힌 살아 있는 적 ≥ 1). 문맥별 1회 = HealOnceLedger. 판단 = ChannelHealRule(상류 실패면 추가 오류 없이 멈춤).
    public class ChannelSelfHealAction : ContextAction
    {
        public ContextActionDealDamage DamageAction;
        static string Caster { get { return HealKeys.Caster; } }

        public override string GetCaption() { return "Heal caster by the channel's AoE roll once (OathboundRevenant)"; }

        public override void RunAction()
        {
            var ctx = Context;
            if (!Guard.Run(ErrorLedger.SiteChannelHeal, ctx, () =>
            {
                if (ctx == null) throw new InvalidOperationException("no context");
                var caster = ctx.MaybeCaster;
                if (caster == null) throw new InvalidOperationException("no caster");
                if (DamageAction == null) throw new InvalidOperationException("no damage action linked");
                int? roll;
                if (!ctx.AoEDamage.TryGetValue(DamageAction, out roll)) roll = null;
                var v = ChannelHealRule.Decide(FailureLedger.Failed(ctx), HealOnceLedger.IsMarked(ctx, Caster), roll.HasValue);
                switch (v)
                {
                    case ChannelHealVerdict.StopSilently: return; // 상류 실패 — 이미 계수됨
                    case ChannelHealVerdict.Skip: return;         // 정당한 거절: 이 시전에서 이미 회복
                    case ChannelHealVerdict.MissingRoll: RevenantErrors.Report(ErrorLedger.SiteChannelHeal, "no AoE roll recorded for the channel damage"); return;
                }
                HealOnceLedger.Mark(ctx, Caster);
                ctx.TriggerRule(new RuleHealDamage(caster, caster, roll.Value));
                ChannelMountHeal.TryHeal(ctx, caster, roll.Value);
            }, RevenantErrors.Sink)) return; // 실패는 계수됨
        }
    }

    // T8 이 채우는 말 회복 고리(§8-d). T5 에서는 아무것도 하지 않는다.
    internal static partial class ChannelMountHeal
    {
        static partial void TryHealImpl(MechanicsContext ctx, UnitEntityData caster, int roll);
        internal static void TryHeal(MechanicsContext ctx, UnitEntityData caster, int roll) { TryHealImpl(ctx, caster, roll); }
    }
}
