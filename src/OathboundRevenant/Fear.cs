using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static OathboundRevenant.Parts;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    // DC = ContextCalculateAbilityParamsBasedOnClass{Paladin, Charisma}. 자원 = 새 하루 자원(ResourcePlan 행), 부여 = 그 재주의 AddAbilityResources{RestoreAmount}.
    public static class Fear
    {
        public static BlueprintAbility Gaze, Wail;
        public static BlueprintAbilityResource GazeResource, WailResource;
        public static bool Built { get; private set; }

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("OathboundRevenant: Fear.Build() called twice");
            GazeResource = Survival.NewDailyResource("OathboundRevenantGazeResource", Guids.GazeResource, "talent.terriblegaze.name", "talent.terriblegaze.desc", FearPlan.Row("TerribleGaze").Daily);
            WailResource = Survival.NewDailyResource("OathboundRevenantWailResource", Guids.WailResource, "talent.wailofthedead.name", "talent.wailofthedead.desc", FearPlan.Row("WailOfTheDead").Daily);
            Gaze = BuildFear(FearPlan.Row("TerribleGaze"), Guids.GazeAbility);
            Wail = BuildFear(FearPlan.Row("WailOfTheDead"), Guids.WailAbility);
            Channel.MoreAbilities["TerribleGaze"] = Gaze;
            Channel.MoreAbilities["WailOfTheDead"] = Wail;
            Survival.Grant(Talents.Get("TerribleGaze"), Gaze, Guids.GazeResource);
            Survival.Grant(Talents.Get("WailOfTheDead"), Wail, Guids.WailResource);
            Talents.MarkFilled("TerribleGaze");
            Talents.MarkFilled("WailOfTheDead");
            // 자원 되읽기는 Survival.Build 에서(CommandUndead 행이 그때 등록된다) — 시험 「wiring: resource verify order」.
            Built = true;
        }

        static BlueprintAbility BuildFear(FearRow row, string guid)
        {
            string k = "talent." + row.Key.ToLowerInvariant();
            var a = Create<BlueprintAbility>("OathboundRevenant" + row.Key, guid, x =>
            {
                SetField(x, "m_DisplayName", Strings.L(k + ".name"));
                SetField(x, "m_Description", Strings.L(k + ".desc"));
                // 2026-11-16(툴팁 `<null>` 결함): 짧은 설명 = 문구 없음, 지속·내성 칸 = TooltipPlan(본편 FrightfulMoan 과 같이 지속 숨김 · 내성 「Will negates」 공용 문구).
                Tooltips.NoShortDescription(x);
                Tooltips.Ability(x);
                x.Type = AbilityType.Supernatural;
                x.Range = (AbilityRange)Enum.Parse(typeof(AbilityRange), row.Range);
                x.ActionType = UnitCommand.CommandType.Standard;
                bool single = row.Targets == "Single";
                x.CanTargetEnemies = single; x.CanTargetSelf = !single; x.CanTargetFriends = false; x.CanTargetPoint = false;
                x.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                FillEmpty(x);
            });
            var dur = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = Die(FearPlan.DurationDiceSides), DiceCountValue = Cv(FearPlan.DurationDiceCount), BonusValue = CvZero() };
            var failed = Act(new ContextActionConditionalSaved { Succeed = List(), Failed = List(ApplyBuff(Guids.Game.Frightened, false, dur, false)) });
            var run = Comp(new AbilityEffectRunAction { SavingThrowType = (SavingThrowType)Enum.Parse(typeof(SavingThrowType), FearPlan.OuterSave), Actions = List(failed) });
            var dc = Comp(new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma, UseKineticistMainStat = false });
            SetField(dc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.PaladinClass));
            // FX(2026-11-15 승인, FxTable): 본편 CauseFear AbilitySpawnFx(대상 위 — 외침은 자신 대상이라 시전자 위).
            var comps = new System.Collections.Generic.List<BlueprintComponent> { Channel.ResourceLogic(row.Key), run, dc, FearDescriptor(), OathFx.AbilityFx(row.Key) };
            OathFx.SetResourceIds(a, row.Key);
            if (row.Targets == "Single") comps.Add(Comp(new AbilityTargetIsAlly { Not = true }));
            else
            {
                var t = Comp(new AbilityTargetsAround());
                SetField(t, "m_Radius", new Feet(row.RadiusFeet));
                SetField(t, "m_TargetType", Enum.Parse(FindField(typeof(AbilityTargetsAround), "m_TargetType").FieldType, "Enemy"));
                SetField(t, "m_IncludeDead", false);
                SetField(t, "m_Condition", new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[0] });
                comps.Add(t);
            }
            AddComponents(a, comps.ToArray());
            Hands.RebindElements(a);
            VerifyDescriptor(a);
            return a;
        }

        // 검토 I3: 공포 기술자(FearPlan.Descriptors). 문맥 생성자가 블루프린트의 SpellDescriptorComponent 를 읽어 MechanicsContext.SpellDescriptor 로 싣는다
        // (MechanicsContext..ctor IL_0159~0181) → 내성 규칙의 SavingThrowBonusAgainstDescriptor{Fear}(죽음의 오라 −4/−6 등)가 걸린다.
        internal static SpellDescriptor FearFlags()
        {
            if (FearPlan.Descriptors.Count == 0) throw new InvalidOperationException("OathboundRevenant: FearPlan.Descriptors is empty");
            return FearPlan.Descriptors.Select(n => (SpellDescriptor)Enum.Parse(typeof(SpellDescriptor), n)).Aggregate((x, y) => x | y);
        }

        internal static SpellDescriptorComponent FearDescriptor() { return Comp(new SpellDescriptorComponent { Descriptor = FearFlags() }); }

        // 되읽기: 정확히 하나의 SpellDescriptorComponent, 값 == FearFlags().
        internal static void VerifyDescriptor(BlueprintScriptableObject bp)
        {
            var d = bp.ComponentsArray.OfType<SpellDescriptorComponent>().ToList();
            if (d.Count != 1 || (SpellDescriptor)d[0].Descriptor != FearFlags()) throw new InvalidOperationException("OathboundRevenant: " + bp.name + " must carry exactly one SpellDescriptorComponent = " + FearFlags() + " (got " + d.Count + ")");
        }
    }
}
