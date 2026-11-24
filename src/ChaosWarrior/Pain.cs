using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static ChaosWarrior.BuildKit;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
     
    // 판단은 순수 PainCore(어댑터 ContextActionChaosPain). 분노 버프의 근접 명중 트리거(Rage.cs)가 어댑터를 부른다.
    public static class Pain
    {
        // 소유자 결정 2026-11-17 「계수 버프 하나로 재구성」(독립 검토 R3-F1):
        //   CounterBuff = 고통 계수(총 겹 0..문턱의 단일 원장 — 순위 = 총 겹, 숨김, 효과 부품 없음, 본편 NotDispelable 부품으로 해제 불가) ·
        //   Buff = 벌점 버프(순위 = min(총 겹, PainPenaltyCap), 겹당 벌점) — 계수의 자식(Buff.StoreFact: 계수가 지워지면 엔진이 같이 지운다), 해제 주문으로 지워질 수 있고 다음 명중이 계수 기준으로 되살린다 ·
        //   LegacyOverflowBuff = 계수 이전 개발판(bbfb86d~0a07353)의 숨김 초과 버프 — 새로 붙이지 않는다. 그 판의 세이브를 불러올 때 블루프린트가 없어 사실이 떨어지지 않게 등록만 해 두고,
        //     남은 인스턴스는 그 시전자의 다음 명중이 새 계수로 옮긴 뒤 지운다(PainCore) — 명중이 없으면 원래 지속(3라운드) 뒤 만료된다(부품이 없어 그동안 효과도 없다).
        public static BlueprintBuff Buff, CounterBuff, LegacyOverflowBuff, ProbeBuff, SaveSource;
        public static BlueprintFeature AoeFeature;

        public static bool Built { get { return Buff != null && CounterBuff != null && LegacyOverflowBuff != null && ProbeBuff != null && SaveSource != null && AoeFeature != null; } }

        public static void Build()
        {
            if (Buff != null) throw new InvalidOperationException("ChaosWarrior: Pain.Build() called twice");
            PainRule.ValidateCaps(ChaosRules.Dials.PainPenaltyCap, ChaosRules.Dials.PainConfusionThreshold);
            if (!PainSpec.CounterNotDispelable) throw new InvalidOperationException("ChaosWarrior: PainSpec.CounterNotDispelable is false — the pain counter must not be dispellable (owner decision 2026-11-17)");
            Buff = BuildKit.Buff("ChaosWarriorPainBuff", Guids.PainBuff, "pain.name", "pain.desc", false, PainSpec.Stacking);
            Buff.Ranks = ChaosRules.Dials.PainPenaltyCap;
            // 계수: 시전자마다 따로(같은 Stack), 부품 = NotDispelable 하나뿐 — 벌점은 벌점 버프만 낸다(개편 설계 §6-b 「6~8겹 = −5 그대로」).
            CounterBuff = BuildKit.Buff("ChaosWarriorPainCounterBuff", Guids.PainCounterBuff, "pain.count.name", "pain.count.desc", true, PainSpec.Stacking);
            CounterBuff.Ranks = PainRule.CounterRanks;
            AddComponents(CounterBuff, Comp(new NotDispelable()));
            LegacyOverflowBuff = BuildKit.Buff("ChaosWarriorPainOverflowBuff", Guids.PainOverflowBuff, "pain.over.name", "pain.over.desc", true, PainSpec.Stacking);
            LegacyOverflowBuff.Ranks = ChaosRules.Dials.PainLegacyOverflowRanks;
            var desc = E<ModifierDescriptor>(PainSpec.PenaltyDescriptor);
            var comps = new List<BlueprintComponent>();
            foreach (var s in PainSpec.PenaltyStats)
                comps.Add(Comp(new AddStatBonus { Stat = E<StatType>(s), Descriptor = desc, Value = ChaosRules.Dials.PainPerStack, ScaleByBasicAttackBonus = false }));
            if (PainSpec.PenaltySaves) comps.Add(Comp(new BuffAllSavesBonus { Descriptor = desc, Value = ChaosRules.Dials.PainPerStack }));
            AddComponents(Buff, comps.ToArray());

            // 탐침: 정신 효과 기술자만 가진 숨김 버프(RuleCanApplyBuff·UnitPartSpellResistance.IsImmune 의 문맥 연관 블루프린트).
            ProbeBuff = BuildKit.Buff("ChaosWarriorPainProbeBuff", Guids.PainProbeBuff, "pain.probe.name", "pain.probe.desc", true, RageSpec.Stacking);
            var probe = Comp(new SpellDescriptorComponent());
            probe.Descriptor = new SpellDescriptorWrapper(E<SpellDescriptor>(PainSpec.ProbeDescriptor));
            AddComponents(ProbeBuff, probe);

            // 내성 이유: 본편 Confusion 의 SpellDescriptor 부품을 빌드 때 복제(C4 — 기술자 값을 옮겨 적지 않는다).
            var confusion = Get<BlueprintBuff>(Guids.Game.Confusion);
            var cd = confusion.ComponentsArray.OfType<SpellDescriptorComponent>().ToArray();
            if (cd.Length != 1) throw new InvalidOperationException("ChaosWarrior: vanilla Confusion has " + cd.Length + " SpellDescriptorComponent(s), expected 1");
            SaveSource = BuildKit.Buff("ChaosWarriorPainSaveSource", Guids.PainSaveSource, "pain.save.name", "pain.save.desc", true, RageSpec.Stacking);
            AddComponents(SaveSource, BuildReflect.CloneComponent(cd[0]));

            AoeFeature = BuildKit.Feature("ChaosWarriorPainAoeFeature", Guids.PainAoeFeature, "pain.aoe.name", "pain.aoe.desc", false);
            Archetype.AddAt(ChaosRules.Levels.PainAoeLevel, AoeFeature);

            var diffs = VerifyNow();
            if (diffs.Count != 0) throw new InvalidOperationException("ChaosWarrior: pain blueprint read-back differs from PainSpec: " + string.Join(" | ", diffs.ToArray()));
        }

        public static List<string> VerifyNow()
        {
            var d = new List<string>();
            if (!Built) { d.Add("pain blueprints not built"); return d; }
            if (Buff.Stacking != E<StackingType>(PainSpec.Stacking) || Buff.Ranks != ChaosRules.Dials.PainPenaltyCap) d.Add("pain buff stacking/ranks " + Buff.Stacking + "/" + Buff.Ranks);
            if (CounterBuff.Stacking != E<StackingType>(PainSpec.Stacking) || CounterBuff.Ranks != PainRule.CounterRanks) d.Add("pain counter buff stacking/ranks " + CounterBuff.Stacking + "/" + CounterBuff.Ranks);
            var cc = CounterBuff.ComponentsArray ?? new BlueprintComponent[0];
            if (cc.Length != 1 || !(cc[0] is NotDispelable)) d.Add("pain counter buff components [" + string.Join(",", cc.Select(x => x == null ? "null" : x.GetType().Name).ToArray()) + "] (expected exactly NotDispelable)");
            if (!CounterBuff.IsHiddenInUI) d.Add("pain counter buff is not hidden");
            if (LegacyOverflowBuff.Stacking != E<StackingType>(PainSpec.Stacking) || LegacyOverflowBuff.Ranks != ChaosRules.Dials.PainLegacyOverflowRanks) d.Add("legacy pain overflow buff stacking/ranks " + LegacyOverflowBuff.Stacking + "/" + LegacyOverflowBuff.Ranks);
            if ((LegacyOverflowBuff.ComponentsArray ?? new BlueprintComponent[0]).Length != 0) d.Add("legacy pain overflow buff has " + LegacyOverflowBuff.ComponentsArray.Length + " components (expected none)");
            var three = new[] { Buff, CounterBuff, LegacyOverflowBuff };
            if (three.Select(b => b.AssetGuid).Distinct().Count() != three.Length) d.Add("pain penalty / counter / legacy overflow buffs share a GUID");
            var stats = Buff.ComponentsArray.OfType<AddStatBonus>().Select(c => c.Stat + "/" + c.Descriptor + "/" + c.Value).ToList();
            var want = PainSpec.PenaltyStats.Select(s => s + "/" + PainSpec.PenaltyDescriptor + "/" + ChaosRules.Dials.PainPerStack).ToList();
            if (!stats.SequenceEqual(want)) d.Add("pain stat rows [" + string.Join(",", stats.ToArray()) + "] != PainSpec");
            var saves = Buff.ComponentsArray.OfType<BuffAllSavesBonus>().ToArray();
            if (saves.Length != 1 || saves[0].Descriptor != E<ModifierDescriptor>(PainSpec.PenaltyDescriptor) || saves[0].Value != ChaosRules.Dials.PainPerStack) d.Add("pain saves row != PainSpec");
            if (Buff.ComponentsArray.Length != PainSpec.PenaltyStats.Length + saves.Length) d.Add("pain buff has " + Buff.ComponentsArray.Length + " components");
            var probe = ProbeBuff.ComponentsArray.OfType<SpellDescriptorComponent>().ToArray();
            if (probe.Length != 1 || probe[0].Descriptor.Value != E<SpellDescriptor>(PainSpec.ProbeDescriptor) || ProbeBuff.ComponentsArray.Length != 1) d.Add("probe buff != MindAffecting only");
            var src = SaveSource.ComponentsArray.OfType<SpellDescriptorComponent>().ToArray();
            var conf = Get<BlueprintBuff>(Guids.Game.Confusion).ComponentsArray.OfType<SpellDescriptorComponent>().ToArray();
            if (src.Length != 1 || conf.Length != 1 || src[0].Descriptor.Value != conf[0].Descriptor.Value) d.Add("save source descriptor != vanilla Confusion's");
            return d;
        }
    }
}
