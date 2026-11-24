using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static ChaosWarrior.BuildKit;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
     
    // 피로·시전 금지 부품 0개(시험 `copy: no spell ban no fatigue`). 빌드 뒤 되읽기(자원·활성 플래그·CRC 2개) — 다르면 던진다(BuildFailed).
    public static class Rage
    {
        public static BlueprintAbilityResource Resource;
        public static BlueprintBuff Buff;
        public static BlueprintActivatableAbility Ability;
        public static BlueprintFeature Feature, GreaterFeature, MightyFeature;
        // 2026-11-16: 분노 중 자연 무기 단계 버프(RageStrikeSpec.Tiers 순서, 숨김). 분노 버프의 Activated 가 레벨 조건으로 자식 부여한다.
        static readonly List<BlueprintBuff> strikeBuffs = new List<BlueprintBuff>();
        public static IReadOnlyList<BlueprintBuff> StrikeBuffs { get { return strikeBuffs; } }

        public static bool Built { get { return Resource != null && Buff != null && Ability != null && Feature != null && GreaterFeature != null && MightyFeature != null && strikeBuffs.Count == RageStrikeSpec.Tiers.Count; } }

        public static void Build()
        {
            if (Resource != null) throw new InvalidOperationException("ChaosWarrior: Rage.Build() called twice");
            Get<BlueprintCharacterClass>(Guids.Game.WarpriestClass);

            Resource = Create<BlueprintAbilityResource>("ChaosWarriorRageResource", Guids.RageResource, r =>
            {
                r.LocalizedName = Strings.L("rage.name");
                r.LocalizedDescription = Strings.L("rage.desc");
                object amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                FillEmpty(amount);
                SetField(amount, "BaseValue", 0);
                SetField(amount, "IncreasedByLevel", false);
                SetField(amount, "IncreasedByLevelStartPlusDivStep", true);
                SetField(amount, "StartingLevel", ChaosRules.RageResourceSpec.StartingLevel);
                SetField(amount, "StartingIncrease", ChaosRules.RageResourceSpec.StartingIncrease);
                SetField(amount, "LevelStep", ChaosRules.RageResourceSpec.LevelStep);
                SetField(amount, "PerStepIncrease", ChaosRules.RageResourceSpec.PerStepIncrease);
                SetField(amount, "MinClassLevelIncrease", 0);
                var cls = new List<BlueprintCharacterClassReference> { Ref<BlueprintCharacterClassReference>(Guids.Game.WarpriestClass) };
                SetField(amount, "m_ClassDiv", cls.ToArray());
                SetField(amount, "IncreasedByStat", true);
                SetField(amount, "ResourceBonusStat", E<StatType>("Wisdom"));
                SetField(r, "m_MaxAmount", amount);
                SetField(r, "m_UseMax", false);
                SetField(r, "m_Min", ChaosRules.RageResourceSpec.Min);
            });

            Buff = BuildKit.Buff("ChaosWarriorRageBuff", Guids.RageBuff, "rage.name", "rage.desc", false, RageSpec.Stacking);
            var comps = new List<BlueprintComponent>();
            foreach (var row in RageSpec.Stats)
            {
                var stat = E<StatType>(row.Stat);
                var desc = E<ModifierDescriptor>(row.Descriptor);
                if (row.Source == RageStatSource.Ac)
                    comps.Add(Comp(new AddStatBonus { Descriptor = desc, Stat = stat, Value = ChaosRules.Dials.RageAc, ScaleByBasicAttackBonus = false }));
                else
                    comps.Add(Comp(new AddContextStatBonus { Descriptor = desc, Stat = stat, Multiplier = 1, Value = CvRank(row.Source == RageStatSource.StrCon ? RageSpec.StrConRankType : RageSpec.WillRankType), HasMinimal = false, Minimal = 0 }));
            }
            comps.Add(RankConfig(RageSpec.StrConRankType, RageSpec.RankBaseValueType, RageSpec.RankProgression, ChaosRules.ProgressionRows(ChaosRules.RageStrCon)));
            comps.Add(RankConfig(RageSpec.WillRankType, RageSpec.RankBaseValueType, RageSpec.RankProgression, ChaosRules.ProgressionRows(ChaosRules.RageWill)));
            // T5: 켤 때마다 광기의 변이 추첨(분노 버프 문맥 = Buff.Data 가 설정된 RunActionInContext 안 — 부록 A-c).
            var roll = new List<GameAction> { Act(new ContextActionChaosMutations()) };
            // FX(2026-11-15 승인, FxTable 원장): 본편 StandartRageBuff 처럼 Activated 안의 1회 SpawnFx(끝에 붙여 변이 추첨 [0] 위치 유지) + 지속 FxOnStart · ResourceAssetIds.
            roll.AddRange(ChaosFx.OneShotActions(FxTable.TargetRage));
            ChaosFx.ApplyPersistent(Buff, FxTable.TargetRage);
            // 2026-11-16 소유자 결정(몽크 기 일격 구조 3·7·10): 분노 중 자연 무기 = 마법 / 냉철·은 / 혼돈·악. 단계마다 숨김 버프 + 레벨 조건 게이트(RageStrikeSpec).
            // 게이트는 Activated 목록의 끝에 붙인다(변이 추첨 [0]·FX 위치 유지). 분노 버프의 부품 목록은 그대로다(#0~#7 — 저장된 런타임 부품은 이름으로 다시 맞춘다, EntityFact.PostLoadComponents @0019~0055).
            roll.AddRange(BuildStrike());
            comps.Add(Comp(new AddFactContextActions { Activated = Actions(roll), Deactivated = Actions(new List<GameAction>()), NewRound = Actions(new List<GameAction>()), Dispose = Actions(new List<GameAction>()) }));
            // T7: 분노 중 근접 무기 명중(기회공격 포함, 원거리 제외)마다 고통 — PainSpec.Trigger* 해석.
            var pain = new List<GameAction> { Act(new ContextActionChaosPain()) };
            var trig = Comp(new AddInitiatorAttackWithWeaponTrigger());
            trig.OnlyHit = PainSpec.TriggerOnlyHit;
            trig.CheckWeaponRangeType = PainSpec.TriggerCheckWeaponRangeType;
            trig.RangeType = E<Kingmaker.Enums.WeaponRangeType>(PainSpec.TriggerRangeType);
            trig.OnAttackOfOpportunity = PainSpec.TriggerOnAttackOfOpportunity;
            trig.ActionsOnInitiator = PainSpec.TriggerActionsOnInitiator;
            trig.Action = Actions(pain);
            comps.Add(trig);
            AddComponents(Buff, comps.ToArray());

            Ability = Create<BlueprintActivatableAbility>("ChaosWarriorRageAbility", Guids.RageAbility, a =>
            {
                Names(a, "rage.name", "rage.desc");
                SetField(a, "m_Buff", Ref<BlueprintBuffReference>(Guids.RageBuff));
                a.Group = E<ActivatableAbilityGroup>(RageSpec.Group);
                a.WeightInGroup = ChaosRules.Dials.RageWeightInGroup;
                a.IsOnByDefault = RageSpec.IsOnByDefault;
                a.DeactivateIfCombatEnded = RageSpec.DeactivateIfCombatEnded;
                a.DeactivateAfterFirstRound = false;
                a.DeactivateImmediately = RageSpec.DeactivateImmediately;
                a.DeactivateIfOwnerDisabled = false;
                a.DeactivateIfOwnerUnconscious = RageSpec.DeactivateIfOwnerUnconscious;
                a.OnlyInCombat = RageSpec.OnlyInCombat;
                a.DoNotTurnOffOnRest = false;
                a.ActivationType = E<AbilityActivationType>(RageSpec.ActivationType);
                SetField(a, "m_ActivateWithUnitCommand", E<UnitCommand.CommandType>(RageSpec.ActivateWithUnitCommand));
                a.ResourceAssetIds = new string[0];
                FillEmpty(a);
            });
            var logic = Comp(new ActivatableAbilityResourceLogic { SpendType = E<ActivatableAbilityResourceLogic.ResourceSpendType>(RageSpec.SpendType) });
            SetField(logic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.RageResource));
            AddComponents(Ability, logic, new RageStartCost());

            Feature = BuildKit.Feature("ChaosWarriorRageFeature", Guids.RageFeature, "rage.name", "rage.desc", false);
            var facts = Comp(new AddFacts());
            var factList = new List<BlueprintUnitFactReference> { Ref<BlueprintUnitFactReference>(Guids.RageAbility) };
            SetField(facts, "m_Facts", factList.ToArray());
            var res = Comp(new AddAbilityResources { RestoreAmount = true, UseThisAsResource = false, Amount = 0, RestoreOnLevelUp = false });
            SetField(res, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.RageResource));
            var featureComps = new List<BlueprintComponent> { facts, res };
            AddComponents(Feature, featureComps.ToArray());
            GreaterFeature = BuildKit.Feature("ChaosWarriorGreaterRageFeature", Guids.RageGreaterFeature, "rage.greater.name", "rage.greater.desc", false);
            MightyFeature = BuildKit.Feature("ChaosWarriorMightyRageFeature", Guids.RageMightyFeature, "rage.mighty.name", "rage.mighty.desc", false);

            var tiers = new List<BlueprintFeature> { Feature, GreaterFeature, MightyFeature };
            if (tiers.Count != ChaosRules.Levels.RageTierLevels.Length) throw new InvalidOperationException("ChaosWarrior: rage tier features " + tiers.Count + " != RageTierLevels " + ChaosRules.Levels.RageTierLevels.Length);
            for (int i = 0; i < tiers.Count; i++) Archetype.AddAt(ChaosRules.Levels.RageTierLevels[i], tiers[i]);

            var diffs = VerifyNow();
            if (diffs.Count != 0) throw new InvalidOperationException("ChaosWarrior: rage blueprint read-back differs from RageSpec / ChaosRules: " + string.Join(" | ", diffs.ToArray()));
        }

        // 단계 버프(숨김) + 그 단계의 게이트 행동. 반환 = 분노 버프 Activated 끝에 붙일 게이트(Tiers 순서).
        static List<GameAction> BuildStrike()
        {
            if (strikeBuffs.Count != 0) throw new InvalidOperationException("ChaosWarrior: Rage.BuildStrike() called twice");
            RageStrikeSpec.Validate();
            var gates = new List<GameAction>();
            foreach (var t in RageStrikeSpec.Tiers)
            {
                var b = BuildKit.Buff("ChaosWarriorRageStrike" + t.Id, t.BuffGuid, "rage.strike.name", "rage.strike.desc", true, RageSpec.Stacking);
                AddComponents(b, NaturalStrike(t));
                strikeBuffs.Add(b);
                gates.Add(StrikeGate(t));
            }
            return gates;
        }

        // 단계 한 줄 → 본편 AddOutgoingPhysicalDamageProperty 한 개. 판정·속성 필드 15개를 전부 명시한다(엔진 기본값에 맡기지 않는다 — 시험 strike: wiring 이 필드마다 대입 출처를 대조한다).
        // 대입은 한 줄에 하나, 값은 사양에서 읽은 것 그대로(식·리터럴 변형 금지 — 시험이 IL 의 값 출처를 본다).
        static AddOutgoingPhysicalDamageProperty NaturalStrike(StrikeTier t)
        {
            var c = new AddOutgoingPhysicalDamageProperty();
            c.NaturalAttacks = RageStrikeSpec.NaturalAttacks;
            c.NotUnarmed = RageStrikeSpec.NotUnarmed;
            c.AffectAnyPhysicalDamage = RageStrikeSpec.AffectAnyPhysicalDamage;
            c.AddMagic = t.Magic;
            c.AddMaterial = t.HasMaterials;
            c.Material = FlagsOrNone<Kingmaker.Enums.Damage.PhysicalDamageMaterial>(t.Materials);
            c.AddAlignment = t.HasAlignments;
            c.Alignment = FlagsOrNone<Kingmaker.Enums.Damage.DamageAlignment>(t.Alignments);
            c.MyAlignment = RageStrikeSpec.MyAlignment;
            c.AddForm = false;
            c.AddReality = false;
            c.CheckWeaponType = false;
            c.CheckRange = false;
            c.IsRanged = false;
            c.AgainstFactOwner = false;
            return Comp(c);
        }

        // 게이트: 문맥 시전자(분노를 켠 본인)의 워프리스트 레벨 >= 단계 레벨이면 단계 버프를 분노의 자식으로 영구 부여(본편 StandartRageBuff 의 Conditional → ContextActionApplyBuff 모양).
        static Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional StrikeGate(StrikeTier t)
        {
            var cond = Cond(new ContextConditionCharacterClass());
            cond.CheckCaster = RageStrikeSpec.GateCheckCaster;
            cond.MinLevel = t.Level;
            cond.Not = false;
            SetField(cond, "m_Class", Ref<BlueprintCharacterClassReference>(Guids.Game.WarpriestClass));
            var apply = Act(new ContextActionApplyBuff());
            SetField(apply, "m_Buff", Ref<BlueprintBuffReference>(t.BuffGuid));
            apply.Permanent = RageStrikeSpec.GatePermanent;
            apply.AsChild = RageStrikeSpec.GateAsChild;
            apply.ToCaster = RageStrikeSpec.GateToCaster;
            apply.IsNotDispelable = RageStrikeSpec.GateNotDispelable;
            apply.IsFromSpell = false;
            apply.UseDurationSeconds = false;
            apply.SameDuration = false;
            apply.NotLinkToAreaEffect = false;
            apply.IgnoreParentContext = false;
            // Permanent 면 읽지 않지만(CalculateDuration @0000~0011) null 로 두지 않는다(0 라운드 — Mutations.cs 의 같은 모양).
            apply.DurationValue = new Kingmaker.UnitLogic.Mechanics.ContextDurationValue { Rate = E<Kingmaker.UnitLogic.Mechanics.DurationRate>("Rounds"), DiceType = E<Kingmaker.RuleSystem.DiceType>("Zero"), DiceCountValue = Cv(0), BonusValue = Cv(0) };
            var conds = new List<Condition> { cond };
            var then = new List<GameAction> { apply };
            var gate = Act(new Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional());
            gate.Comment = "";
            gate.ConditionsChecker = new ConditionsChecker { Operation = E<Operation>("And"), Conditions = conds.ToArray() };
            gate.IfTrue = Actions(then);
            gate.IfFalse = Actions(new List<GameAction>());
            return gate;
        }

        // 실제 부품 → RageStrikeSpec.Line(사양 쪽 Expected 와 같은 함수). 재질·성향은 켜져 있을 때만 이름으로 푼다.
        // 꺼져 있는데 값이 0 이 아니면 "off:<값>" 을 넣어 사양 줄('-')과 반드시 달라지게 한다(꺼진 필드에 남은 값이 조용히 지나가지 않는다). 거리 칸은 CheckRange 또는 IsRanged 가 켜져 있으면 참.
        static string DescribeStrike(AddOutgoingPhysicalDamageProperty c)
        {
            return RageStrikeSpec.Line(c.NaturalAttacks, c.NotUnarmed, c.AffectAnyPhysicalDamage, c.AddMagic,
                c.AddMaterial ? FlagNames(c.Material) : OffNames(Convert.ToInt32(c.Material)), c.AddAlignment ? FlagNames(c.Alignment) : OffNames(Convert.ToInt32(c.Alignment)),
                c.MyAlignment, c.AddForm, c.AddReality, c.CheckWeaponType, c.CheckRange || c.IsRanged, c.AgainstFactOwner);
        }

        static List<string> OffNames(int raw)
        {
            var l = new List<string>();
            if (raw != 0) l.Add("off:" + raw);
            return l;
        }

        // 실제 게이트 행동 → RageStrikeSpec.GateLine. 모양이 다르면(조건·행동 개수, 형식) 그 사실을 적은 줄을 돌려준다 — 사양 줄과 같아질 수 없다.
        static string DescribeGate(Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional g)
        {
            if (g.ConditionsChecker == null || g.ConditionsChecker.Conditions == null || g.IfTrue == null || g.IfTrue.Actions == null || g.IfFalse == null || g.IfFalse.Actions == null) return "gate|unreadable (null checker or action list)";
            var conds = g.ConditionsChecker.Conditions;
            var then = g.IfTrue.Actions;
            var cc = conds.Length == 1 ? conds[0] as ContextConditionCharacterClass : null;
            var ap = then.Length == 1 ? then[0] as ContextActionApplyBuff : null;
            if (cc == null || ap == null) return "gate|shape conditions=" + conds.Length + " actions=" + then.Length + " (expected one ContextConditionCharacterClass and one ContextActionApplyBuff)";
            var cls = (BlueprintCharacterClassReference)GetF(cc, "m_Class");
            var buff = (BlueprintBuffReference)GetF(ap, "m_Buff");
            if (cls == null || buff == null) return "gate|unreadable (null class or buff reference)";
            return RageStrikeSpec.GateLine(cls.Guid.ToString(), cc.MinLevel, cc.CheckCaster, cc.Not, buff.Guid.ToString(), ap.Permanent && !ap.UseDurationSeconds && !ap.SameDuration, ap.AsChild && !ap.IgnoreParentContext, ap.ToCaster,
                ap.IsNotDispelable, g.IfFalse.Actions.Length);
        }

        // 생산 사양 ↔ 실제 블루프린트(빌드 시 + 점검). 빈 목록 = 일치.
        public static List<string> VerifyNow()
        {
            var d = new List<string>();
            if (!Built) { d.Add("rage blueprints not built"); return d; }
            object amount = GetF(Resource, "m_MaxAmount");
            if (!Equals(GetF(amount, "IncreasedByLevelStartPlusDivStep"), true) || !Equals(GetF(amount, "StartingLevel"), ChaosRules.RageResourceSpec.StartingLevel)
                || !Equals(GetF(amount, "StartingIncrease"), ChaosRules.RageResourceSpec.StartingIncrease) || !Equals(GetF(amount, "LevelStep"), ChaosRules.RageResourceSpec.LevelStep)
                || !Equals(GetF(amount, "PerStepIncrease"), ChaosRules.RageResourceSpec.PerStepIncrease) || !Equals(GetF(Resource, "m_Min"), ChaosRules.RageResourceSpec.Min)
                || !Equals(GetF(amount, "IncreasedByStat"), true) || !Equals(GetF(amount, "ResourceBonusStat"), E<StatType>("Wisdom")))
                d.Add("resource amount fields != RageResourceSpec");
            var div = (BlueprintCharacterClassReference[])GetF(amount, "m_ClassDiv");
            if (div.Length != 1 || div[0].Guid.ToString() != Guids.Game.WarpriestClass) d.Add("resource m_ClassDiv != [WarpriestClass]");
            if (Ability.DeactivateImmediately != RageSpec.DeactivateImmediately || Ability.DeactivateIfCombatEnded != RageSpec.DeactivateIfCombatEnded || Ability.OnlyInCombat != RageSpec.OnlyInCombat
                || Ability.IsOnByDefault != RageSpec.IsOnByDefault || Ability.DeactivateIfOwnerUnconscious != RageSpec.DeactivateIfOwnerUnconscious
                || Ability.Group != E<ActivatableAbilityGroup>(RageSpec.Group) || Ability.ActivationType != E<AbilityActivationType>(RageSpec.ActivationType)
                || !Equals(GetF(Ability, "m_ActivateWithUnitCommand"), E<UnitCommand.CommandType>(RageSpec.ActivateWithUnitCommand)) || Ability.Buff != Buff)
                d.Add("activatable ability flags != RageSpec");
            var logic = Ability.ComponentsArray.OfType<ActivatableAbilityResourceLogic>().ToArray();
            if (logic.Length != 1 || logic[0].SpendType != E<ActivatableAbilityResourceLogic.ResourceSpendType>(RageSpec.SpendType) || logic[0].RequiredResource != Resource || Ability.ComponentsArray.OfType<RageStartCost>().Count() != 1)
                d.Add("resource logic != SpendType " + RageSpec.SpendType + " on our resource");
            var ranks = Buff.ComponentsArray.OfType<ContextRankConfig>().Select(DescribeRank).ToList();
            var want = new List<string>
            {
                DescribeRows(RageSpec.StrConRankType, RageSpec.RankBaseValueType, RageSpec.RankProgression, ChaosRules.ProgressionRows(ChaosRules.RageStrCon)),
                DescribeRows(RageSpec.WillRankType, RageSpec.RankBaseValueType, RageSpec.RankProgression, ChaosRules.ProgressionRows(ChaosRules.RageWill)),
            };
            if (!ranks.SequenceEqual(want)) d.Add("rank configs [" + string.Join(" ; ", ranks.ToArray()) + "] != [" + string.Join(" ; ", want.ToArray()) + "]");
            var stats = Buff.ComponentsArray.Select(c => c is AddContextStatBonus ? ((AddContextStatBonus)c).Stat + "/" + ((AddContextStatBonus)c).Descriptor : c is AddStatBonus ? ((AddStatBonus)c).Stat + "/" + ((AddStatBonus)c).Descriptor + "/" + ((AddStatBonus)c).Value : null).Where(x => x != null).ToList();
            var wantStats = RageSpec.Stats.Select(r => r.Source == RageStatSource.Ac ? r.Stat + "/" + r.Descriptor + "/" + ChaosRules.Dials.RageAc : r.Stat + "/" + r.Descriptor).ToList();
            if (!stats.SequenceEqual(wantStats)) d.Add("buff stat components [" + string.Join(", ", stats.ToArray()) + "] != RageSpec.Stats [" + string.Join(", ", wantStats.ToArray()) + "]");
            if (Buff.Stacking != E<StackingType>(RageSpec.Stacking)) d.Add("buff stacking " + Buff.Stacking + " != " + RageSpec.Stacking);
            d.AddRange(ChaosFx.Verify(FxTable.TargetRage, Buff));
            // 2026-11-16 단계 표 되읽기(RageStrikeSpec.Tiers): (1) 분노 버프 자체에는 속성 부품이 없다(있으면 레벨과 무관하게 걸린다)
            // (2) 단계 버프마다 부품 정확히 1개, 줄이 그 단계의 사양과 같고 숨김 (3) Activated 의 게이트가 단계 수만큼, 순서·줄이 사양과 같다.
            int onRage = Buff.ComponentsArray.Count(c => c is AddOutgoingPhysicalDamageProperty);
            if (onRage != 0) d.Add("rage buff itself carries " + onRage + " natural strike component(s) (they would apply at every level; tiers live on the child buffs)");
            if (strikeBuffs.Count != RageStrikeSpec.Tiers.Count) d.Add("strike buffs " + strikeBuffs.Count + " != RageStrikeSpec.Tiers " + RageStrikeSpec.Tiers.Count);
            else
                for (int i = 0; i < strikeBuffs.Count; i++)
                {
                    var t = RageStrikeSpec.Tiers[i];
                    var b = strikeBuffs[i];
                    var got = b.ComponentsArray.Select(c => c is AddOutgoingPhysicalDamageProperty ? DescribeStrike((AddOutgoingPhysicalDamageProperty)c) : c.GetType().Name + "|?").ToList();
                    var wantLine = new List<string> { RageStrikeSpec.Expected(t) };
                    if (b.AssetGuid.ToString() != t.BuffGuid || !got.SequenceEqual(wantLine)) d.Add("strike tier " + t + " buff " + b.name + " [" + string.Join(" ; ", got.ToArray()) + "] != [" + wantLine[0] + "]");
                    if (!b.IsHiddenInUI) d.Add("strike tier " + t + " buff " + b.name + " is visible (IconTable lists it as hidden)");
                    if (b.Stacking != E<StackingType>(RageSpec.Stacking)) d.Add("strike tier " + t + " buff stacking " + b.Stacking + " != " + RageSpec.Stacking);
                }
            var gates = Buff.ComponentsArray.OfType<AddFactContextActions>().SelectMany(c => c.Activated.Actions).OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>().Select(DescribeGate).ToList();
            var wantGates = RageStrikeSpec.Tiers.Select(RageStrikeSpec.ExpectedGate).ToList();
            if (!gates.SequenceEqual(wantGates)) d.Add("strike gates [" + string.Join(" ; ", gates.ToArray()) + "] != RageStrikeSpec [" + string.Join(" ; ", wantGates.ToArray()) + "]");
            return d;
        }
    }
    // ResourceLogic with SpendType.None still exposes the real resource to the action bar.
    // Native TryStart sets IsStarted before spending. A paid activation lasts until the player or combat ends it.
    public sealed class RageStartCost : Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.ActivatableAbilityRestriction, IActivatableAbilitySpendResourceLogic
    {
        protected override bool IsAvailable() { return Fact.IsStarted || Owner.Resources.HasEnoughResource(Rage.Resource, 1); }
        public void OnStart() { Owner.Resources.Spend(Rage.Resource, 1); }
        public void OnAbilityTurnOn() { }
        public void OnNewRound() { }
        public void OnAttack(Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon weapon) { }
        public void OnHit() { }
        public void OnCrit() { }
        public void ManualSpendResource() { }
    }

}
