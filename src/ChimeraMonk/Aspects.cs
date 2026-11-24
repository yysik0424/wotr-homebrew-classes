using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    //  ① 그래프: 형태 버프에서 시작해 AddFeatureOnClassLevel.m_Feature·AddFacts.m_Facts 를 깊이 우선으로 따라가(AspectPlan.RetargetGraph — 순수, 시험이 합성 그래프로 실행)
    //     「클래스를 읽거나 복제할 자손을 가진」 블루프린트만 복제(버프 10 + 늑대 냄새 상위 1). 잎(Scent10/20/30·Diehard·Endurance)은 원본 참조.
    //  ② 재지정: ContextRankConfig.m_Class·AddFeatureOnClassLevel.m_Class → [Monk], m_AdditionalClasses → [], 참조 → 복제본, AspectPlan.Graph 덮어쓰기(W1).
    //  ③ 능력 복제: ActivatableAbilityResourceLogic 제거(상시 토글), m_Buff → 버프 복제, Group → AspectPlan.ToggleGroup(본편 배타 그룹에서 뺌 — 2026-11-16: 가진 형태 전부 동시에 켬).
    //     우리 형태 특성(T3 껍데기, 부분 변환 단계마다 하나씩)에 AddFacts{능력} — 어느 단계에서 골라도 같은 토글을 받는다(같은 형태는 한 단계에서만 고를 수 있다).
    //  ④ 강화: CRC 마다 BoostPlan.For(IsStackable(기술자)) — Replace = 한 단계 위 값 / Add = 차이(AspectPlan.BoostTable), 형태 버프 복제에 BuffExtraEffects{변이약 버프 → 강화}.
    public static class Aspects
    {
        public static readonly Dictionary<string, BlueprintBuff> Buffs = new Dictionary<string, BlueprintBuff>();
        public static readonly Dictionary<string, BlueprintActivatableAbility> Abilities = new Dictionary<string, BlueprintActivatableAbility>();
        public static readonly Dictionary<string, BlueprintBuff> Boosts = new Dictionary<string, BlueprintBuff>();
        public static readonly Dictionary<string, SimpleBlueprint> GraphClones = new Dictionary<string, SimpleBlueprint>(); // 원본 GUID → 복제
        public static bool Built { get; private set; }

        static readonly Type[] KnownClassReaders = { typeof(ContextRankConfig), typeof(AddFeatureOnClassLevel), typeof(PrerequisiteClassLevel) };

        static IEnumerable<FieldInfo> Fields(Type t)
        {
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                foreach (var f in cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!f.IsLiteral) yield return f;
        }

        // 컴포넌트가 직업 참조(BlueprintCharacterClassReference / 배열)를 가진 GUID 목록.
        internal static List<string> ClassRefs(BlueprintComponent c)
        {
            var l = new List<string>();
            foreach (var f in Fields(c.GetType()))
            {
                var v = f.GetValue(c);
                var one = v as BlueprintCharacterClassReference;
                if (one != null && !one.IsEmpty()) l.Add(one.Guid.ToString());
                var arr = v as BlueprintCharacterClassReference[];
                if (arr != null) l.AddRange(arr.Where(r => r != null && !r.IsEmpty()).Select(r => r.Guid.ToString()));
            }
            return l;
        }

        static GraphNode Resolve(string guid)
        {
            var bp = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(guid)) as BlueprintScriptableObject;
            if (bp == null) return null;
            bool reads = false;
            var children = new List<string>();
            foreach (var c in bp.ComponentsArray ?? new BlueprintComponent[0])
            {
                var refs = ClassRefs(c);
                if (refs.Count > 0)
                {
                    if (!KnownClassReaders.Contains(c.GetType()))
                    {
                        ChimeraErrors.Report(BuildPlan.BuilderSite, "aspect graph: " + bp.name + " has an unknown class-reading component " + c.GetType().Name);
                        throw new InvalidOperationException("ChimeraMonk: unknown class-reading component " + c.GetType().Name + " on " + bp.name);
                    }
                    reads = true;
                }
                var afl = c as AddFeatureOnClassLevel;
                if (afl != null && afl.Feature != null) children.Add(afl.Feature.AssetGuid.ToString());
                var af = c as AddFacts;
                if (af != null) children.AddRange(((BlueprintUnitFactReference[])Archetype.GetF(af, "m_Facts")).Select(r => r.Guid.ToString()));
            }
            return new GraphNode(reads, children);
        }

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("ChimeraMonk: Aspects.Build() called twice");
            var graphRows = AspectPlan.Graph;
            var monkRef = Ref<BlueprintCharacterClassReference>(Guids.Game.MonkClass);
            foreach (var form in AspectPlan.Forms)
            {
                var order = AspectPlan.RetargetGraph(form.BuffGuid, Resolve, ChimeraErrors.Report);
                var map = new Dictionary<string, string>();
                foreach (var g in order)
                {
                    if (g == form.BuffGuid) { map[g] = form.OurBuff; continue; }
                    var row = graphRows.FirstOrDefault(r => r.SourceGuid == g);
                    if (row == null || row.OurGuid == null) throw new InvalidOperationException("ChimeraMonk: aspect graph of " + form.Key + " needs a clone of " + g + " but AspectPlan.Graph has no row for it");
                    map[g] = row.OurGuid;
                }
                // 잎부터(깊은 것부터) 복제해 부모가 복제본을 가리키게.
                foreach (var g in Enumerable.Reverse(order))
                {
                    if (GraphClones.ContainsKey(g)) continue;
                    var src = Get<BlueprintScriptableObject>(g);
                    SimpleBlueprint clone;
                    // 2026-11-17: 복제 버프는 원본 FxOnRemove 를 그대로 쓰되 null 이면 빈 링크(BlueprintUtil.EmptyFxOnRemove 머리말 — Buff.OnRemove @00B0 NRE).
                    if (src is BlueprintBuff) clone = EmptyFxOnRemove(BlueprintClone.Clone((BlueprintBuff)src, "Chimera" + src.name, map[g], c => true));
                    else if (src.GetType() == typeof(BlueprintFeature)) clone = BlueprintClone.Clone((BlueprintFeature)src, "Chimera" + src.name, map[g], c => true);
                    else throw new InvalidOperationException("ChimeraMonk: aspect graph clone of unsupported type " + src.GetType().Name + " (" + src.name + ")");
                    Retarget((BlueprintScriptableObject)clone, map, monkRef, graphRows.FirstOrDefault(r => r.SourceGuid == g));
                    GraphClones.Add(g, clone);
                }
                var buff = (BlueprintBuff)GraphClones[form.BuffGuid];
                Buffs.Add(form.Key, buff);

                // 강화 버프(W2): CRC 마다 같은 기술자·능력치, 값 = Custom 20행(배수 1).
                var boost = Fab.Buff("ChimeraAspectBoost" + form.Key, form.OurBoost, "mut.Aspect." + form.Key, true);
                var bc = new List<BlueprintComponent>();
                foreach (var c in form.Crcs)
                {
                    var desc = Fab.EnumOf<ModifierDescriptor>(c.Descriptor);
                    var rank = Fab.EnumOf<AbilityRankType>(c.RankType);
                    bc.Add(Fab.Comp(new AddContextStatBonus { Descriptor = desc, Stat = Fab.EnumOf<StatType>(c.Stat), Multiplier = 1, Value = Fab.CvRank(rank) }));
                    bc.Add(Fab.RankConfig(rank, ContextRankProgression.Custom, AspectPlan.BoostTable(c, BoostPlan.For(ModifierDescriptorHelper.IsStackable(desc)))));
                }
                AddComponents(boost, bc.ToArray());
                Boosts.Add(form.Key, boost);
                var extra = Fab.Comp(new BuffExtraEffects());
                SetField(extra, "m_CheckedBuffList", Mutagen.ActiveBuffRefs);
                SetField(extra, "m_ExtraEffectBuff", Ref<BlueprintBuffReference>(form.OurBoost));
                AddComponents(buff, extra);

                var srcAb = Get<BlueprintActivatableAbility>(form.AbilityGuid);
                var ab = BlueprintClone.Clone(srcAb, "Chimera" + srcAb.name, form.OurAbility, c => !(c is ActivatableAbilityResourceLogic));
                SetField(ab, "m_Buff", Ref<BlueprintBuffReference>(form.OurBuff));
                ab.Group = Fab.EnumOf<ActivatableAbilityGroup>(AspectPlan.ToggleGroup);
                Abilities.Add(form.Key, ab);
                foreach (var stage in ChimeraRules.AspectStages)
                {
                    var facts = Fab.Comp(new AddFacts());
                    SetField(facts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(form.OurAbility) });
                    AddComponents(Mutations.Option(stage.Id, form.Key), facts);
                }
            }
            Built = true;
            var diff = Readback.AspectDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: aspect readback: " + string.Join(" | ", diff));
        }

        static void Retarget(BlueprintScriptableObject clone, Dictionary<string, string> map, BlueprintCharacterClassReference monk, GraphRow row)
        {
            foreach (var c in clone.ComponentsArray)
            {
                var crc = c as ContextRankConfig;
                if (crc != null && ((BlueprintCharacterClassReference[])Archetype.GetF(crc, "m_Class")).Length > 0)
                    SetField(crc, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(Guids.Game.MonkClass) });
                var afl = c as AddFeatureOnClassLevel;
                if (afl != null)
                {
                    SetField(afl, "m_Class", Ref<BlueprintCharacterClassReference>(Guids.Game.MonkClass));
                    SetField(afl, "m_AdditionalClasses", new BlueprintCharacterClassReference[0]);
                    string fg = afl.Feature == null ? null : afl.Feature.AssetGuid.ToString();
                    string to;
                    if (fg != null && map.TryGetValue(fg, out to)) SetField(afl, "m_Feature", Ref<BlueprintFeatureReference>(to));
                }
                var af = c as AddFacts;
                if (af != null)
                {
                    var refs = (BlueprintUnitFactReference[])Archetype.GetF(af, "m_Facts");
                    SetField(af, "m_Facts", refs.Select(r => { string to; return map.TryGetValue(r.Guid.ToString(), out to) ? Ref<BlueprintUnitFactReference>(to) : r; }).ToArray());
                }
            }
            if (row == null) return;
            // 덮어쓰기(W1): 그 블루프린트의 AddFeatureOnClassLevel 을 (특성 → 레벨, BeforeThisLevel) 로 정확히 맞춘다. 개수·대상이 다르면 던짐.
            var afls = clone.ComponentsArray.OfType<AddFeatureOnClassLevel>().ToList();
            if (afls.Count != row.Overrides.Count) throw new InvalidOperationException("ChimeraMonk: " + clone.name + " has " + afls.Count + " AddFeatureOnClassLevel, AspectPlan.Graph overrides " + row.Overrides.Count);
            foreach (var o in row.Overrides)
            {
                var hit = afls.Where(a => a.Feature != null && a.Feature.AssetGuid.ToString() == o.Item1).ToList();
                if (hit.Count != 1) throw new InvalidOperationException("ChimeraMonk: " + clone.name + " override target " + o.Item1 + " matched " + hit.Count + " component(s)");
                hit[0].Level = o.Item2;
                hit[0].BeforeThisLevel = o.Item3;
            }
        }
    }
}
