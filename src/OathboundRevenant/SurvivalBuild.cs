using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static OathboundRevenant.Parts;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    public static partial class Survival
    {
        public static BlueprintAbilityResource CommandResource, RiposteResource, UnyieldingResource;
        public static BlueprintAbility CommandUndeadAbility, LastBreathAbility;
        public static bool Built { get; private set; }

        static bool KeepShared(object v) { return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent; }

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("OathboundRevenant: Survival.Build() called twice");
            // 무덤의 갑주: 본편 HolyChampion 의 AddDamageResistancePhysical 복제(정렬 우회만 반대 — 선).
            var champ = Get<BlueprintFeature>(Guids.Game.HolyChampion);
            var drSrc = champ.ComponentsArray.OfType<AddDamageResistancePhysical>().SingleOrDefault();
            if (drSrc == null) throw new InvalidOperationException("OathboundRevenant: vanilla HolyChampion has no single AddDamageResistancePhysical");
            var dr = CloneBoundary.CopyComponent(drSrc, KeepShared, RevenantRules.Dials.CloneDepth);
            dr.Value = Cv(SurvivalPlan.GraveArmorDr);
            dr.BypassedByAlignment = true;
            dr.Alignment = (DamageAlignment)Enum.Parse(typeof(DamageAlignment), SurvivalPlan.GraveArmorBypass);
            AddComponents(Talents.Get("GraveArmor"), dr);
            Talents.MarkFilled("GraveArmor");

            AddComponents(Talents.Get("IronBones"), Comp(new AddFortification { UseContextValue = false, Bonus = SurvivalPlan.IronBonesPercent, Value = CvZero() }));
            Talents.MarkFilled("IronBones");

            AddComponents(Talents.Get("GhostStride"),
                Comp(new AddConditionImmunity { Condition = (UnitCondition)Enum.Parse(typeof(UnitCondition), SurvivalPlan.GhostStrideImmunity) }),
                Comp(new AddStatBonus { Stat = StatType.Speed, Descriptor = (ModifierDescriptor)Enum.Parse(typeof(ModifierDescriptor), SurvivalPlan.GhostStrideDescriptor), Value = SurvivalPlan.GhostStrideSpeed }));
            Talents.MarkFilled("GhostStride");

            // 영혼 수확: 무기로 HP 0 이하로 만든 적이 실제로 죽었을 때 시전자 회복(팔라딘 레벨) — 자체 부품 SoulHarvestComponent(판정 기록 C1, 검토 C1).
            AddComponents(Talents.Get("SoulHarvest"), Comp(new SoulHarvestComponent()));
            OathFx.PrepareCSharp("SoulHarvest");   // FX(2026-11-15 승인): 회복 순간 안수 FX — 링크는 여기서 본편 대조, 실행은 SoulHarvestComponent.Heal
            Talents.MarkFilled("SoulHarvest");

            // 원한의 반격(3/일).
            RiposteResource = NewDailyResource("OathboundRevenantRiposteResource", Guids.RiposteResource, "talent.vengefulriposte.name", "talent.vengefulriposte.desc", RevenantRules.Dials.RiposteDaily);
            var rip = Comp(new AddTargetAttackWithWeaponTrigger { OnlyHit = true, OnlyMelee = true, OnAttackOfOpportunity = false, WaitForAttackResolve = true, ActionsOnAttacker = List(Act(new RiposteAction())), ActionOnSelf = List() });
            var ripRes = Comp(new Kingmaker.Designers.Mechanics.Facts.AddAbilityResources { RestoreAmount = true, UseThisAsResource = false, RestoreOnLevelUp = false });
            SetField(ripRes, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.RiposteResource));
            AddComponents(Talents.Get("VengefulRiposte"), rip, ripRes);
            Talents.MarkFilled("VengefulRiposte");

            // 언데드 지배(1/일): 본편 CommandUndead 복제(초자연·우리 자원·팔라딘 DC, 주문 목록·주문 학파 제거, 적 대상).
            CommandResource = NewDailyResource("OathboundRevenantCommandResource", Guids.CommandResource, "talent.commandundead.name", "talent.commandundead.desc", RevenantRules.Dials.CommandDaily);
            BuildCommandUndead();
            Grant(Talents.Get("CommandUndead"), CommandUndeadAbility, Guids.CommandResource);
            Channel.MoreAbilities["CommandUndead"] = CommandUndeadAbility;
            Talents.MarkFilled("CommandUndead");

            // 꺼지지 않는 맹세(1/일) + 마지막 숨(숨겨진 폭발 능력).
            UnyieldingResource = NewDailyResource("OathboundRevenantUnyieldingResource", Guids.UnyieldingResource, "talent.unyieldingoath.name", "talent.unyieldingoath.desc", RevenantRules.Dials.UnyieldingDaily);
            var ures = Comp(new Kingmaker.Designers.Mechanics.Facts.AddAbilityResources { RestoreAmount = true, UseThisAsResource = false, RestoreOnLevelUp = false });
            SetField(ures, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.UnyieldingResource));
            AddComponents(Talents.Get("UnyieldingOath"), Comp(new UnyieldingOathComponent()), ures);
            Talents.MarkFilled("UnyieldingOath");
            BuildLastBreath();
            Talents.MarkFilled("LastBreath");

            // 맹세의 방패: 오라 효과 공장 등록 → 오라 사슬(AuraPlan 행 OathShield).
            Auras.OathShieldFactory = () => Comp(new OathShieldComponent());
            Auras.BuildLate("OathShield");

            Channel.VerifyResources();
            // 되읽기: 영혼 수확 = 자체 부품 1개뿐(본편 무기 트리거 없음) · 반격·맹세의 소비 주체 부품이 실제로 붙었다(ResourcePlan 의 비능력 소비자).
            var sh = Talents.Get("SoulHarvest").ComponentsArray;
            if (sh.OfType<SoulHarvestComponent>().Count() != 1 || sh.OfType<AddInitiatorAttackWithWeaponTrigger>().Any()) throw new InvalidOperationException("OathboundRevenant: SoulHarvest must carry exactly one SoulHarvestComponent and no weapon trigger");
            if (!Talents.Get("VengefulRiposte").ComponentsArray.OfType<AddTargetAttackWithWeaponTrigger>().Any(x => x.ActionsOnAttacker.Actions.OfType<RiposteAction>().Count() == 1)) throw new InvalidOperationException("OathboundRevenant: VengefulRiposte has no RiposteAction consumer");
            if (Talents.Get("UnyieldingOath").ComponentsArray.OfType<UnyieldingOathComponent>().Count() != 1) throw new InvalidOperationException("OathboundRevenant: UnyieldingOath has no UnyieldingOathComponent consumer");
            Built = true;
        }

        static void BuildCommandUndead()
        {
            var src = Get<BlueprintAbility>(Guids.Game.CommandUndead);
            CommandUndeadAbility = Create<BlueprintAbility>("OathboundRevenantCommandUndead", Guids.CommandUndeadAbility, a =>
            {
                Hands.CopyAbilityFields(src, a);
                SetField(a, "m_DisplayName", Strings.L("talent.commandundead.name"));
                SetField(a, "m_Description", Strings.L("talent.commandundead.desc"));
                a.Type = AbilityType.Supernatural;
            });
            var kept = src.ComponentsArray.Where(c => !(c is SpellListComponent) && !(c is SpellComponent) && !(c is ContextAbilityParamsCalculator) && !(c is AbilityResourceLogic)).Select(c => (BlueprintComponent)CloneBoundary.CopyComponent(c, KeepShared, RevenantRules.Dials.ActionCloneDepth)).ToList(); // 행동 트리(AbilityEffectRunAction) 포함
            var dc = Comp(new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma, UseKineticistMainStat = false });
            SetField(dc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.PaladinClass));
            kept.Add(Channel.ResourceLogic("CommandUndead"));
            kept.Add(dc);
            kept.Add(Comp(new AbilityTargetIsAlly { Not = true }));
            AddComponents(CommandUndeadAbility, kept.ToArray());
            Hands.RebindElements(CommandUndeadAbility);
            if (CommandUndeadAbility.ComponentsArray.OfType<SpellListComponent>().Any()) throw new InvalidOperationException("OathboundRevenant: command undead clone still has spell list components");
            if (src.ComponentsArray.OfType<SpellListComponent>().Count() != RevenantRules.Dials.CommandUndeadSpellLists) throw new InvalidOperationException("OathboundRevenant: vanilla CommandUndead spell list components changed (expected " + RevenantRules.Dials.CommandUndeadSpellLists + ")");
            if (!CommandUndeadAbility.ComponentsArray.OfType<AbilityTargetHasFact>().Any()) throw new InvalidOperationException("OathboundRevenant: command undead clone lost AbilityTargetHasFact[UndeadType]");
        }

        // 마지막 숨(판정 기록 M2·R2 「채널의 2배, 강화 채널 포함」): 30 ft 적, 바깥 Unknown → 적 → Will 1회 → 극성 분기(2 × 채널 주사위, IsAoE, 반감).
        static void BuildLastBreath()
        {
            LastBreathAbility = Create<BlueprintAbility>("OathboundRevenantLastBreath", Guids.LastBreathAbility, a =>
            {
                SetField(a, "m_DisplayName", Strings.L("talent.lastbreath.name"));
                SetField(a, "m_Description", Strings.L("talent.lastbreath.desc"));
                // 2026-11-16(툴팁 `<null>` 결함): 숨은 능력이지만 null 로 두지 않는다. 짧은 설명 = 문구 없음, 지속·내성 칸 = TooltipPlan(본편 채널과 같이 두 칸 모두 숨김).
                Tooltips.NoShortDescription(a);
                Tooltips.Ability(a);
                a.Type = AbilityType.Supernatural; a.Range = AbilityRange.Personal; a.ActionType = UnitCommand.CommandType.Free;
                a.CanTargetSelf = true; a.Hidden = true;
                FillEmpty(a);
            });
            var b = StrikePlan.DamageBranches("LastBreath");
            var pol = Polarity(b, () => CvRank(AbilityRankType.DamageDice), d => { d.IsAoE = true; d.HalfIfSaved = true; });
            var save = Act(new ContextActionSavingThrow { Type = SavingThrowType.Will, HasCustomDC = false, UseDCFromContextSavingThrow = false, FromBuff = false, CustomDC = CvZero(), Actions = List(pol) });
            var run = Comp(new AbilityEffectRunAction { SavingThrowType = SavingThrowType.Unknown, Actions = List(If(Cond(new ContextConditionIsEnemy { Not = false }), List(save), List())) });
            var t = Comp(new AbilityTargetsAround());
            SetField(t, "m_Radius", new Feet(RevenantRules.Dials.LastBreathRadiusFeet));
            SetField(t, "m_TargetType", Enum.Parse(FindField(typeof(AbilityTargetsAround), "m_TargetType").FieldType, "Enemy"));
            SetField(t, "m_IncludeDead", false);
            SetField(t, "m_Condition", new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[0] });
            var dc = Comp(new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma, UseKineticistMainStat = false });
            SetField(dc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.PaladinClass));
            // 판정 기록 R2: 채널의 2배(강화 채널 포함) = 채널 주사위 속성(ChannelDiceGetter: ½ 레벨 + 강화 2) × LastBreathChannelMultiplier
            // (ContextRankConfig MultiplyByModifier = 값 × m_StepLevel, ApplyProgression IL_008E~0096).
            var rank = Comp(new ContextRankConfig());
            SetField(rank, "m_Type", AbilityRankType.DamageDice);
            SetField(rank, "m_BaseValueType", ContextRankBaseValueType.CustomProperty);
            SetField(rank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(Guids.ChannelDiceProperty));
            SetField(rank, "m_Progression", ContextRankProgression.MultiplyByModifier);
            SetField(rank, "m_StepLevel", RevenantRules.Dials.LastBreathChannelMultiplier);
            // FX(2026-11-15 승인, FxTable): 채널과 같은 모양의 폭발이라 채널 FX(시전자 기준, Time OnApplyEffect — RuleCastSpell 경로 AbilityExecutionProcess.SpawnFxs 가 띄운다).
            AddComponents(LastBreathAbility, t, run, rank, dc, OathFx.AbilityFx("LastBreath"));
            OathFx.SetResourceIds(LastBreathAbility, "LastBreath", FxTable.Divine + "LastBreath");
            Hands.RebindElements(LastBreathAbility);
            Strike.VerifyPolarity(((ContextActionSavingThrow)((Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional)run.Actions.Actions.Single()).IfTrue.Actions.Single()).Actions, b, false);
        }
    }
}
