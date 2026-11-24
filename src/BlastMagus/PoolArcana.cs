using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BlastMagus
{
    // 블라스트 풀(설계 §4.6-1) + 블라스트 메이거스 비전 선택지(§4.6-2). 본편 선례: SpellDancerArchetype(풀), EldritchScion·Hexcrafter(비전 선택지 교체) — 정찰 11-08 §7.
    // 본편 블루프린트는 읽기만 한다(설계 §3): 값은 새 인스턴스로 복사하고 되읽어 대조한다(C4). 비전 거르기 판단은 순수 코어 ArcanaFilter(CorePlans.cs).
    public static class PoolArcana
    {
        public static BlueprintFeature Pool;
        public static BlueprintFeatureSelection Arcana;

        // 설계 §4.6-2 의 4종(원장 = ArcanaPlan.Excluded, 순수 파일 — 시험이 같은 배열을 jbp 와 대조한다).
        public static string[] ExcludedArcana { get { return ArcanaPlan.Excluded; } }

        // 본편 ArcanePoolFeature 컴포넌트 형식(blueprints.zip ArcanePoolFeature.jbp Components, 시험 pool: jbp shape). 다르면 복제 전제가 깨진 것 → 던진다.
        static readonly Type[] VanillaPoolShape = { typeof(AddFacts), typeof(AddAbilityResources) };

        public static void Build()
        {
            if (Archetype.Bp == null) throw new InvalidOperationException("BlastMagus: PoolArcana.Build() before Archetype.Build() (AddAt needs the archetype)");
            if (Pool != null || Arcana != null) throw new InvalidOperationException("BlastMagus: PoolArcana.Build() called twice");

            // ---- (1) 본편 아케인 풀 특성의 모양 되읽기 ----
            var vPool = Get<BlueprintFeature>(Guids.Game.ArcanePoolFeature);
            var comps = vPool.ComponentsArray;
            if (comps == null) throw new InvalidOperationException("BlastMagus: ArcanePoolFeature.ComponentsArray is null");
            var types = comps.Select(c => c == null ? null : c.GetType()).ToArray();
            if (!types.SequenceEqual(VanillaPoolShape))
                throw new InvalidOperationException("BlastMagus: ArcanePoolFeature components are [" + string.Join(", ", types.Select(t => t == null ? "null" : t.Name).ToArray()) + "], expected [AddFacts, AddAbilityResources] (design §4.6-1)");
            var vFacts = (AddFacts)comps[0];
            var factRefs = (BlueprintUnitFactReference[])GetF(vFacts, "m_Facts");
            if (factRefs == null || factRefs.Length != 1 || factRefs[0] == null || factRefs[0].Guid.ToString() != Guids.Game.ArcaneWeaponSwitchAbility)
                throw new InvalidOperationException("BlastMagus: ArcanePoolFeature AddFacts is [" + (factRefs == null ? "null" : string.Join(", ", factRefs.Select(r => r == null ? "null" : r.Guid.ToString()).ToArray())) + "], expected [ArcaneWeaponSwitchAbility " + Guids.Game.ArcaneWeaponSwitchAbility + "]");
            var vRes = (AddAbilityResources)comps[1];
            var vResRef = GetF(vRes, "m_Resource") as BlueprintAbilityResourceReference;
            // 폭발 비용(BlastStrike 의 AbilityResourceLogic)이 같은 GUID 를 쓴다 → 본편 풀이 다른 자원을 가리키면 폭발과 풀이 어긋난다.
            if (vResRef == null || vResRef.Guid.ToString() != Guids.Game.ArcanePoolResourse)
                throw new InvalidOperationException("BlastMagus: ArcanePoolFeature AddAbilityResources.m_Resource is " + (vResRef == null ? "null" : vResRef.Guid.ToString()) + ", expected ArcanePoolResourse " + Guids.Game.ArcanePoolResourse);

            // ---- (2) 블라스트 풀 = 새 특성 + AddAbilityResources 새 인스턴스(값은 본편에서 복사·되읽기) ----
            Pool = BlastBlueprints.MakeFeature("BlastMagusBlastPoolFeature", Guids.BlastPoolFeature, "BlastPool.Name", "BlastPool.Description", IconOf(vPool));
            // 본편은 Extra/Abundant Arcane Pool 을 가리킨다 — 우리는 그 특기의 전제조건이 아니다(수용된 부수 효과, 설계 §11-B).
            Pool.IsPrerequisiteFor = new List<BlueprintFeatureReference>();
            var res = new AddAbilityResources
            {
                UseThisAsResource = vRes.UseThisAsResource,
                Amount = vRes.Amount,
                RestoreAmount = vRes.RestoreAmount,
                RestoreOnLevelUp = vRes.RestoreOnLevelUp,
            };
            SetField(res, "m_Resource", Ref<BlueprintAbilityResourceReference>(vResRef.Guid.ToString()));
            AddComponents(Pool, res);
            var back = Pool.ComponentsArray;
            if (back == null || back.Length != 1 || !ReferenceEquals(back[0], res) || ReferenceEquals(res, vRes))
                throw new InvalidOperationException("BlastMagus: BlastPoolFeature components readback is not exactly the new AddAbilityResources");
            if (res.Resource == null || res.Resource != vRes.Resource || res.UseThisAsResource != vRes.UseThisAsResource || res.Amount != vRes.Amount
                || res.RestoreAmount != vRes.RestoreAmount || res.RestoreOnLevelUp != vRes.RestoreOnLevelUp)
                throw new InvalidOperationException("BlastMagus: BlastPoolFeature AddAbilityResources readback differs from ArcanePoolFeature's (resource/UseThisAsResource/Amount/RestoreAmount/RestoreOnLevelUp)");
            if (Pool.IsPrerequisiteFor == null || Pool.IsPrerequisiteFor.Count != 0) throw new InvalidOperationException("BlastMagus: BlastPoolFeature.IsPrerequisiteFor readback is not empty");

            // ---- (3) 비전 선택지 = 본편 선택지 값 복사 + m_Features [] + m_AllFeatures = ArcanaFilter(본편, 4종).kept ----
            var vSel = Get<BlueprintFeatureSelection>(Guids.Game.MagusArcanaSelection);
            var plan = ArcanaFilter.Filter(VanillaArcana(vSel), ExcludedArcana);
            if (vSel.Groups == null) throw new InvalidOperationException("BlastMagus: MagusArcanaSelection.Groups is null");
            // 본편 선택지 컴포넌트는 복제하지 않는다(blueprints.zip MagusArcanaSelection.jbp Components = [] — 시험 arcana: jbp … Components []).
            // 누가 더했다면 그 의미를 모른 채 버리게 되므로 조용히 넘기지 않고 던진다(fix 1 m1, C1).
            var vSelComps = vSel.ComponentsArray;
            if (vSelComps == null) throw new InvalidOperationException("BlastMagus: MagusArcanaSelection.ComponentsArray is null");
            if (vSelComps.Length > 0)
                throw new InvalidOperationException("BlastMagus: MagusArcanaSelection has components [" + string.Join(", ", vSelComps.Select(c => c == null ? "null" : c.GetType().Name).ToArray()) + "] that BlastMagusArcanaSelection would not copy (expected none)");
            bool allowNonContext = (bool)GetF(vSel, "m_AllowNonContextActions");
            var selIcon = IconOf(vSel);
            Arcana = Create<BlueprintFeatureSelection>("BlastMagusArcanaSelection", Guids.ArcanaSelection, s =>
            {
                BlastBlueprints.Names(s, "ArcanaSelection.Name", "ArcanaSelection.Description", selIcon);
                s.IsClassFeature = vSel.IsClassFeature;
                s.Ranks = vSel.Ranks;
                s.ReapplyOnLevelUp = vSel.ReapplyOnLevelUp;
                s.Groups = vSel.Groups.ToArray();
                s.HideInUI = vSel.HideInUI;
                s.HideInCharacterSheetAndLevelUp = vSel.HideInCharacterSheetAndLevelUp;
                s.HideNotAvailibleInUI = vSel.HideNotAvailibleInUI;
                s.IgnorePrerequisites = vSel.IgnorePrerequisites;
                s.ExceptWhiteListed = vSel.ExceptWhiteListed;
                s.Obligatory = vSel.Obligatory;
                s.Mode = vSel.Mode;
                s.Group = vSel.Group;
                s.Group2 = vSel.Group2;
                s.ShowThisSelection = vSel.ShowThisSelection;
                SetField(s, "m_AllowNonContextActions", allowNonContext);
                // 본편은 Extra Arcana 특기 2개를 가리킨다 — 우리 선택지는 그 전제조건이 아니다(O1 수용, 설계 §11-B).
                s.IsPrerequisiteFor = new List<BlueprintFeatureReference>();
                SetField(s, "m_Features", new BlueprintFeatureReference[0]);
            });
            if (Arcana.ComponentsArray == null || Arcana.ComponentsArray.Length != 0) throw new InvalidOperationException("BlastMagus: BlastMagusArcanaSelection components readback is not empty");
            var diff = SettingsDiff(Arcana, vSel);
            if (diff.Length > 0) throw new InvalidOperationException("BlastMagus: BlastMagusArcanaSelection readback differs from MagusArcanaSelection: " + string.Join(", ", diff));
            var featBack = (BlueprintFeatureReference[])GetF(Arcana, "m_Features");
            if (featBack == null || featBack.Length != 0 || Arcana.IsPrerequisiteFor == null || Arcana.IsPrerequisiteFor.Count != 0)
                throw new InvalidOperationException("BlastMagus: BlastMagusArcanaSelection m_Features / IsPrerequisiteFor readback is not empty");
            SetArcana(plan.kept);
            if (plan.missingExcluded.Length > 0)
                ModContext.Log("arcana: " + plan.missingExcluded.Length + " excluded arcana not in vanilla MagusArcanaSelection (nothing to remove): " + string.Join(", ", plan.missingExcluded));

            // ---- (4) 레벨표: 제거 표의 같은 레벨에 교체 특성(Archetype.Removed 가 단일 원장) ----
            var poolLevels = Archetype.RemovedLevels(Guids.Game.ArcanePoolFeature);
            if (poolLevels.Length != 1) throw new InvalidOperationException("BlastMagus: ArcanePoolFeature is removed at " + poolLevels.Length + " levels, expected 1");
            Archetype.AddAt(poolLevels[0], Pool);
            var arcanaLevels = Archetype.RemovedLevels(Guids.Game.MagusArcanaSelection);
            foreach (var lv in arcanaLevels) Archetype.AddAt(lv, Arcana);
            var got = Archetype.Bp.AddFeatures.Where(e => e.Features.Any(f => f == Arcana)).Select(e => e.Level).OrderBy(x => x).ToArray();
            if (!got.SequenceEqual(arcanaLevels))
                throw new InvalidOperationException("BlastMagus: BlastMagusArcanaSelection archetype levels [" + string.Join(",", got) + "] != removed MagusArcanaSelection levels [" + string.Join(",", arcanaLevels) + "]");
        }

        // 2단계 훅(T5)이 부른다: 그 시점의 본편 목록 - 4종으로 다시 맞추고 증감을 로그 1줄(설계 §4.6-2 의 2번).
        public static void Resync()
        {
            if (Pool == null || Arcana == null) throw new InvalidOperationException("BlastMagus: PoolArcana.Resync() before PoolArcana.Build()");
            var vSel = Get<BlueprintFeatureSelection>(Guids.Game.MagusArcanaSelection);
            var excluded = ExcludedArcana;
            var plan = ArcanaFilter.Filter(VanillaArcana(vSel), excluded);
            var before = OurArcana();
            int added = plan.kept.Count(g => !before.Contains(g));
            int removed = before.Count(g => !plan.kept.Contains(g));
            SetArcana(plan.kept);
            ModContext.Log("arcana resync: +" + added + " -" + removed + " (excluded " + (excluded.Length - plan.missingExcluded.Length) + ")"
                + (plan.missingExcluded.Length > 0 ? "; excluded arcana not in vanilla MagusArcanaSelection: " + string.Join(", ", plan.missingExcluded) : ""));
        }

        // 본편 선택지 목록(GUID "N" 형식). null 배열·null 참조는 던진다. 빈 GUID 는 "" 로 나와 ArcanaFilter 가 던진다.
        static string[] VanillaArcana(BlueprintFeatureSelection vSel)
        {
            var refs = (BlueprintFeatureReference[])GetF(vSel, "m_AllFeatures");
            if (refs == null) throw new InvalidOperationException("BlastMagus: MagusArcanaSelection.m_AllFeatures is null");
            for (int i = 0; i < refs.Length; i++)
                if (refs[i] == null) throw new InvalidOperationException("BlastMagus: MagusArcanaSelection.m_AllFeatures[" + i + "] is null");
            return refs.Select(r => r.Guid.ToString()).ToArray();
        }

        static string[] OurArcana()
        {
            var refs = (BlueprintFeatureReference[])GetF(Arcana, "m_AllFeatures");
            if (refs == null) throw new InvalidOperationException("BlastMagus: BlastMagusArcanaSelection.m_AllFeatures is null");
            return refs.Select(r => r == null ? "null" : r.Guid.ToString()).ToArray();
        }

        static void SetArcana(string[] kept)
        {
            if (kept == null) throw new ArgumentNullException("kept");
            SetField(Arcana, "m_AllFeatures", kept.Select(g => Ref<BlueprintFeatureReference>(g)).ToArray());
            var back = OurArcana();
            if (!back.SequenceEqual(kept))
                throw new InvalidOperationException("BlastMagus: BlastMagusArcanaSelection.m_AllFeatures readback [" + string.Join(",", back) + "] != [" + string.Join(",", kept) + "]");
        }

        // 복사한 설정 값 대조(어긋난 이름 목록, 같으면 빈 배열).
        static string[] SettingsDiff(BlueprintFeatureSelection a, BlueprintFeatureSelection v)
        {
            var d = new List<string>();
            if (a.IsClassFeature != v.IsClassFeature) d.Add("IsClassFeature");
            if (a.Ranks != v.Ranks) d.Add("Ranks");
            if (a.ReapplyOnLevelUp != v.ReapplyOnLevelUp) d.Add("ReapplyOnLevelUp");
            if (a.Groups == null || !a.Groups.SequenceEqual(v.Groups) || ReferenceEquals(a.Groups, v.Groups)) d.Add("Groups");
            if (a.HideInUI != v.HideInUI) d.Add("HideInUI");
            if (a.HideInCharacterSheetAndLevelUp != v.HideInCharacterSheetAndLevelUp) d.Add("HideInCharacterSheetAndLevelUp");
            if (a.HideNotAvailibleInUI != v.HideNotAvailibleInUI) d.Add("HideNotAvailibleInUI");
            if (a.IgnorePrerequisites != v.IgnorePrerequisites) d.Add("IgnorePrerequisites");
            if (a.ExceptWhiteListed != v.ExceptWhiteListed) d.Add("ExceptWhiteListed");
            if (a.Obligatory != v.Obligatory) d.Add("Obligatory");
            if (a.Mode != v.Mode) d.Add("Mode");
            if (a.Group != v.Group) d.Add("Group");
            if (a.Group2 != v.Group2) d.Add("Group2");
            if (a.ShowThisSelection != v.ShowThisSelection) d.Add("ShowThisSelection");
            if ((bool)GetF(a, "m_AllowNonContextActions") != (bool)GetF(v, "m_AllowNonContextActions")) d.Add("m_AllowNonContextActions");
            return d.ToArray();
        }

        static Sprite IconOf(BlueprintUnitFact f)
        {
            var icon = f.Icon;
            if (icon == null) throw new InvalidOperationException("BlastMagus: " + f.name + " has no icon");
            return icon;
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("BlastMagus: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
