using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    //  변이 껍데기(T3)에 상시 컴포넌트를 붙이고, 강화 버프가 있는 행은 특성에 BuffExtraEffects{m_CheckedBuffList = Mutagen.ActiveBuffRefs, m_ExtraEffectBuff = 강화}(부록 A-e — 해제 OnDeactivate 가 강화 제거).
    //  강화 모양 = BoostPlan.For(ModifierDescriptorHelper.IsStackable(기술자)) — 빌드 시 엔진 값(TTT Postfix 반영)을 읽는다.
    //  분출 +1(추가 폐) 행은 브레스 분출 자원을 만드는 Breath.Build(T7)가 소비한다(PendingSurge) — 그 전에 끝나면 Readback 이 실패로 센다.
    public static class BodyMutations
    {
        public static readonly Dictionary<string, BlueprintBuff> BoostBuffs = new Dictionary<string, BlueprintBuff>();
        public static readonly List<Tuple<BlueprintFeature, EffectRow>> PendingSurge = new List<Tuple<BlueprintFeature, EffectRow>>();
        public static bool Built { get; private set; }

        static readonly AbilityRankType RankForTables = AbilityRankType.StatBonus;

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("ChimeraMonk: BodyMutations.Build() called twice");
            foreach (var row in BodyPlan.Rows())
            {
                var f = Mutations.Get(row.Id);
                var comps = new List<BlueprintComponent>();
                foreach (var e in row.Base) comps.AddRange(Make(e, false));
                if (row.Base.Any(e => e.Kind == EffectKind.RankedStatBonus)) f.ReapplyOnLevelUp = true;
                if (row.BoostBuffGuid != null)
                {
                    var b = Fab.Buff("ChimeraBoost" + row.Id, row.BoostBuffGuid, "mut." + row.Id, true);
                    var bc = row.Boost.SelectMany(e => Make(e, true)).ToArray();
                    if (bc.Length == 0) throw new InvalidOperationException("ChimeraMonk: boost buff for " + row.Id + " has no components");
                    AddComponents(b, bc);
                    BoostBuffs.Add(row.Id, b);
                    var extra = Fab.Comp(new BuffExtraEffects());
                    SetField(extra, "m_CheckedBuffList", Mutagen.ActiveBuffRefs);
                    SetField(extra, "m_ExtraEffectBuff", Ref<BlueprintBuffReference>(row.BoostBuffGuid));
                    comps.Add(extra);
                }
                else
                {
                    foreach (var e in row.Boost)
                    {
                        if (e.Kind != EffectKind.BreathSurge) throw new InvalidOperationException("ChimeraMonk: " + row.Id + " boost row " + e + " needs a boost buff");
                        PendingSurge.Add(Tuple.Create(f, e));
                    }
                }
                AddComponents(f, comps.ToArray());
            }
            Built = true;
            var diff = Readback.BodyDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: body mutation readback: " + string.Join(" | ", diff));
        }

        static SpellDescriptorWrapper Descriptor(string name) { return new SpellDescriptorWrapper(Fab.EnumOf<SpellDescriptor>(name)); }

        static IEnumerable<BlueprintComponent> Make(EffectRow e, bool boost)
        {
            switch (e.Kind)
            {
                case EffectKind.StatBonus:
                    yield return Fab.Comp(new AddStatBonus { Descriptor = Fab.EnumOf<ModifierDescriptor>(e.Descriptor), Stat = Fab.EnumOf<StatType>(e.Stat), Value = e.Value, ScaleByBasicAttackBonus = false });
                    break;
                case EffectKind.RankedStatBonus:
                    {
                        var desc = Fab.EnumOf<ModifierDescriptor>(e.Descriptor);
                        int[] table = e.ReplaceTable;
                        if (boost) table = BoostPlan.For(ModifierDescriptorHelper.IsStackable(desc)) == BoostShape.Replace ? e.ReplaceTable : e.AddTable;
                        if (table == null) throw new InvalidOperationException("ChimeraMonk: ranked stat row " + e + " has no table for " + (boost ? "boost" : "base"));
                        yield return Fab.Comp(new AddContextStatBonus { Descriptor = desc, Stat = Fab.EnumOf<StatType>(e.Stat), Multiplier = 1, Value = Fab.CvRank(RankForTables) });
                        yield return Fab.RankConfig(RankForTables, ContextRankProgression.Custom, table);
                    }
                    break;
                case EffectKind.CmdManeuvers:
                    yield return Fab.Comp(new CMDBonusAgainstManeuvers { Descriptor = Fab.EnumOf<ModifierDescriptor>(e.Descriptor), Value = Fab.Cv(e.Value), Maneuvers = e.Stat.Split(',').Select(Fab.EnumOf<CombatManeuver>).ToArray() });
                    break;
                case EffectKind.Mechanics:
                    foreach (var n in e.Refs)
                    {
                        var m = Fab.Comp(new AddMechanicsFeature());
                        SetField(m, "m_Feature", Fab.EnumOf<AddMechanicsFeature.MechanicsFeatureType>(n));
                        yield return m;
                    }
                    break;
                case EffectKind.Facts:
                    {
                        var a = Fab.Comp(new AddFacts());
                        SetField(a, "m_Facts", e.Refs.Select(Ref<BlueprintUnitFactReference>).ToArray());
                        yield return a;
                    }
                    break;
                case EffectKind.Blindsense:
                    yield return Fab.Comp(new Blindsense { Range = new Feet(e.Value), Blindsight = false });
                    break;
                case EffectKind.DescriptorImmunity:
                    foreach (var n in e.Refs)
                    {
                        yield return Fab.Comp(new BuffDescriptorImmunity { Descriptor = Descriptor(n), CheckFact = false });
                        yield return Fab.Comp(new SpellImmunityToSpellDescriptor { Descriptor = Descriptor(n) });
                    }
                    break;
                case EffectKind.ConditionImmunity:
                    foreach (var n in e.Refs) yield return Fab.Comp(new AddConditionImmunity { Condition = Fab.EnumOf<UnitCondition>(n) });
                    break;
                case EffectKind.AreaImmunity:
                    {
                        // 본편 AreaEffectImmunity → UnitPartAreaEffectIgnorance.AddEntry(특정 영역 목록, 시전자 Any). 영역이 유닛을 갱신할 때(AreaEffectEntityData.UpdateUnit IL_0098~00AA)
                        //  ShouldIgnoreAreaEffect 가 참이면 그 유닛에 진입·라운드 효과를 주지 않는다(IgnoranceEntry.ShouldIgnore: 목록 HasReference + CasterType Any(2) 면 시전자 무관).
                        var a = Fab.Comp(new AreaEffectImmunity());
                        SetField(a, "m_CasterType", Fab.EnumOf<Kingmaker.UnitLogic.Abilities.Components.TargetType>("Any"));
                        SetField(a, "m_SpecificAreaEffects", true);
                        SetField(a, "m_AreaEffects", e.Refs.Select(Ref<BlueprintAbilityAreaEffectReference>).ToList());
                        yield return a;
                    }
                    break;
                case EffectKind.FastHealing:
                    yield return Fab.Comp(new AddEffectFastHealing { Heal = e.Value, Bonus = Fab.Cv(0) });
                    break;
                default:
                    throw new InvalidOperationException("ChimeraMonk: body effect kind " + e.Kind + " is not built here (" + e + ")");
            }
        }
    }
}
