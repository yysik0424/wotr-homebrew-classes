using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    // 껍데기 = TalentSpecRule.SpecFor(row) 결과로 PrerequisiteClassLevel{PaladinClass, ClassLevel} + (Requires → PrerequisiteFeature), Ranks, IsClassFeature.
    // 본문(효과)은 그 재주의 OwnerTask(T4~T7)가 Talents.Get(id) 에 붙이고 MarkFilled(id) 한다. Register 전 되읽기: 채워짐 ⇔ OwnerTask ∈ LevelPlan.ImplementedTasks.
    public static class Talents
    {
        public static BlueprintFeatureSelection Selection;
        static readonly Dictionary<string, BlueprintFeature> byId = new Dictionary<string, BlueprintFeature>();
        static readonly HashSet<string> filled = new HashSet<string>();

        public static void Build()
        {
            if (Selection != null) throw new InvalidOperationException("OathboundRevenant: Talents.Build() called twice");
            Selection = Archetype.Shell("TalentSelection") as BlueprintFeatureSelection;
            if (Selection == null) throw new InvalidOperationException("OathboundRevenant: TalentSelection shell is not a BlueprintFeatureSelection");
            var paladinRef = Guids.Game.PaladinClass;
            foreach (var row in TalentPlan.Rows)
            {
                var spec = TalentSpecRule.SpecFor(row);
                string k = "talent." + row.Id.ToLowerInvariant();
                var f = Create<BlueprintFeature>("OathboundRevenantTalent" + row.Id, Guids.Talent(row.Id), x =>
                {
                    SetField(x, "m_DisplayName", Strings.L(k + ".name"));
                    SetField(x, "m_Description", Strings.L(StringsTable.TalentChoiceKey(row.Id)));   // 선택지 설명은 여러 줄짜리 전용 키(능력·자원·버프 툴팁은 짧은 k + ".desc" 그대로) — 2026-11-17
                    Tooltips.NoShortDescription(x);   // 2026-11-16: LocalizedString 필드 null 금지(Tooltips 머리말)
                    x.Ranks = spec.Ranks;
                    x.IsClassFeature = true;
                });
                var pcl = new PrerequisiteClassLevel { Level = spec.ClassLevel };
                SetField(pcl, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(paladinRef));
                var comps = new List<BlueprintComponent> { pcl };
                if (spec.RequiresFeatureKey != null)
                {
                    var pf = new PrerequisiteFeature();
                    SetField(pf, "m_Feature", Ref<BlueprintFeatureReference>(Guids.Talent(spec.RequiresFeatureKey)));
                    comps.Add(pf);
                }
                AddComponents(f, comps.ToArray());
                byId.Add(row.Id, f);
            }
            Selection.Mode = SelectionMode.OnlyNew;
            Selection.Group = FeatureGroup.None;
            SetField(Selection, "m_AllFeatures", TalentPlan.Rows.Select(r => Ref<BlueprintFeatureReference>(Guids.Talent(r.Id))).ToArray());
            SetField(Selection, "m_Features", new BlueprintFeatureReference[0]);
            Verify();
            Archetype.MarkFilled("TalentSelection");
        }

        public static BlueprintFeature Get(string id)
        {
            BlueprintFeature f;
            if (!byId.TryGetValue(id, out f)) throw new InvalidOperationException("OathboundRevenant: talent " + id + " not built (Talents.Build not run, or id not in TalentPlan)");
            return f;
        }

        public static void MarkFilled(string id)
        {
            Get(id);
            if (!filled.Add(id)) throw new InvalidOperationException("OathboundRevenant: talent " + id + " filled twice");
        }

        public static bool IsFilled(string id) { return filled.Contains(id); }

        // 빌드 되읽기: 선택 20 == 생산 표 순서, 각 껍데기 선행 == TalentSpecRule.
        static void Verify()
        {
            var all = Selection.AllFeatures.ToList();
            if (all.Count != RevenantRules.Dials.TalentOptions || !all.Select(f => f.AssetGuid.ToString()).SequenceEqual(TalentPlan.Rows.Select(r => Guids.Talent(r.Id))))
                throw new InvalidOperationException("OathboundRevenant: talent selection has " + all.Count + " feature(s) / wrong order (expected " + RevenantRules.Dials.TalentOptions + " in TalentPlan order)");
            foreach (var row in TalentPlan.Rows)
            {
                var spec = TalentSpecRule.SpecFor(row);
                var f = byId[row.Id];
                var pcl = f.ComponentsArray.OfType<PrerequisiteClassLevel>().ToArray();
                var pf = f.ComponentsArray.OfType<PrerequisiteFeature>().ToArray();
                bool ok = pcl.Length == 1 && pcl[0].Level == spec.ClassLevel && pcl[0].CharacterClass != null && pcl[0].CharacterClass.AssetGuid.ToString() == Guids.Game.PaladinClass && f.Ranks == spec.Ranks
                    && (spec.RequiresFeatureKey == null ? pf.Length == 0 : pf.Length == 1 && pf[0].Feature != null && pf[0].Feature.AssetGuid.ToString() == Guids.Talent(spec.RequiresFeatureKey));
                if (!ok) throw new InvalidOperationException("OathboundRevenant: talent " + row.Id + " prerequisites differ from TalentSpecRule");
            }
        }

        // Register 전: 재주마다 「채워짐」 ⇔ OwnerTask ∈ LevelPlan.ImplementedTasks.
        public static void VerifyFilled()
        {
            var wrong = TalentPlan.Rows.Where(r => LevelPlan.ImplementedTasks.Contains(r.OwnerTask) != filled.Contains(r.Id)).Select(r => r.Id + "(" + r.OwnerTask + ", filled=" + filled.Contains(r.Id) + ")").ToList();
            if (wrong.Count > 0) throw new InvalidOperationException("OathboundRevenant: talents out of plan: " + string.Join(", ", wrong));
        }
    }
}
