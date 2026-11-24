using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.FactLogic;
using static PackWarden.BlueprintUtil;

namespace PackWarden
{
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        public static BlueprintFeatureSelection CompanionSelection;
        static bool registered;

        static FieldInfo Fld(Type t, string name)
        {
            var f = FindField(t, name); // 기반 형식의 private 필드까지(BlueprintUtil.FindField)
            if (f == null) throw new MissingFieldException("PackWarden: " + t.Name + "." + name + " not found");
            return f;
        }

        static void Set(object o, string name, object v) { Fld(o.GetType(), name).SetValue(o, v); }
        static object GetF(object o, string name) { return Fld(o.GetType(), name).GetValue(o); }

        static readonly FieldInfo ComponentFlagsField = typeof(BlueprintComponent).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);

        static string Norm(string guid) { return BlueprintGuid.Parse(guid).ToString(); }
        static string Join(IEnumerable<string> xs) { var a = xs.ToArray(); return a.Length == 0 ? "(none)" : string.Join(", ", a); }

        // 블루프린트만 만든다. HunterClass.m_Archetypes 등록은 Register() 가 BuildAll 의 맨 끝에서 한다(최종 리뷰 I2):
        // 뒤따르는 빌드가 하나라도 실패하면 아키타입은 목록에 오르지 않는다.
        public static void Build()
        {
            var origSel = Get<BlueprintFeatureSelection>(Guids.Game.AnimalCompanionSelectionHunter);
            string emptyGuid = Norm(Guids.Game.AnimalCompanionEmptyCompanion);

             
            // 다른 모드(예: ExpandedContent 의 BlueprintsCache.Init 후크)가 원본 선택지에 더한 동료는 허용 목록 밖이라 뺀다(이름을 한 줄로 기록).
            // 본편 종은 크기 필터가 정확히 SizeExcludedCompanions 3종만 떨어뜨려야 하고, 남는 집합은 PackCompanions 13종과 같아야 한다.
            var expectedKept = new HashSet<string>(Guids.Game.PackCompanions.Select(r => Norm(r.FeatureGuid)));
            var expectedSizeDrop = new HashSet<string>(Guids.Game.SizeExcludedCompanions.Select(Norm));
            if (expectedKept.Count != Guids.Game.PackCompanions.Length) throw new InvalidOperationException("PackWarden: Guids.Game.PackCompanions has duplicate feature GUIDs");
            if (expectedKept.Overlaps(expectedSizeDrop)) throw new InvalidOperationException("PackWarden: PackCompanions and SizeExcludedCompanions overlap");

            var kept = new List<BlueprintFeatureReference>();
            var keptGuids = new List<string>();
            var keptNames = new List<string>();
            var sizeDropped = new List<string>();
            var sizeDroppedNames = new List<string>();
            var otherMods = new List<string>();
            var all = (BlueprintFeatureReference[])GetF(origSel, "m_AllFeatures");
            if (all == null) throw new InvalidOperationException("PackWarden: AnimalCompanionSelectionHunter.m_AllFeatures is null");
            foreach (var fr in all)
            {
                var feat = fr.Get();
                if (feat == null) throw new InvalidOperationException("PackWarden: unresolved companion reference");
                string g = feat.AssetGuid.ToString();
                if (g == emptyGuid) continue;
                if (!expectedKept.Contains(g) && !expectedSizeDrop.Contains(g))
                {
                    otherMods.Add(feat.name + "(" + g + ")");
                    continue;
                }
                var addPet = feat.GetComponent<AddPet>();
                if (addPet == null) throw new InvalidOperationException("PackWarden: companion feature has no AddPet: " + feat.name);
                var pet = addPet.Pet;
                if (pet == null) throw new InvalidOperationException("PackWarden: AddPet has no pet unit: " + feat.name);
                if (PackRules.IsRideableBaseSize((int)pet.Size, (int)Size.Medium))
                {
                    kept.Add(fr);
                    keptGuids.Add(g);
                    keptNames.Add(feat.name + "(" + pet.Size + ")");
                }
                else
                {
                    sizeDropped.Add(g);
                    sizeDroppedNames.Add(feat.name + "(" + pet.Size + ")");
                }
            }
            Main.Log("PackWarden: companions from other mods excluded: " + Join(otherMods));
            if (sizeDropped.Count != expectedSizeDrop.Count || !expectedSizeDrop.SetEquals(sizeDropped))
                throw new InvalidOperationException("PackWarden: size filter dropped [" + Join(sizeDroppedNames) + "], expected exactly [" + Join(expectedSizeDrop) + "]");
            if (keptGuids.Count != expectedKept.Count || !expectedKept.SetEquals(keptGuids))
                throw new InvalidOperationException("PackWarden: kept companions [" + Join(keptNames.Zip(keptGuids, (n, g) => n + " " + g)) + "] != expected ["
                    + Join(Guids.Game.PackCompanions.Select(r => r.Name + " " + Norm(r.FeatureGuid))) + "]");
            Main.Log("PackWarden companions kept: " + Join(keptNames) + " | dropped by size: " + Join(sizeDroppedNames));

            // 원본 선택지의 컴포넌트(AddFeatureOnApply 3개: 동물 동료 진행표 = 펫 레벨, 기승 토글, 동물 동료 아키타입 선택)를 원본에서 파생해
            // 새 인스턴스로 만든다(최종 리뷰 C1). 모르는 컴포넌트 형식은 조용히 버리지 않고 실패한다.
            var applied = new List<BlueprintComponent>();
            var appliedGuids = new List<string>();
            var srcComps = origSel.ComponentsArray;
            if (srcComps == null) throw new InvalidOperationException("PackWarden: AnimalCompanionSelectionHunter.ComponentsArray is null");
            foreach (var c in srcComps)
            {
                if (c == null) throw new InvalidOperationException("PackWarden: null component on AnimalCompanionSelectionHunter");
                if (c.GetType() != typeof(AddFeatureOnApply))
                    throw new InvalidOperationException("PackWarden: AnimalCompanionSelectionHunter has unexpected component " + c.GetType().FullName + " (" + c.name + "); refusing to drop unknown wiring");
                // BlueprintComponent.m_Flags(private enum, dnlib 2026-11-07)는 복사하지 않는다: 본편 3개는 모두 0(blueprints.zip "m_Flags": 0).
                 
                if (ComponentFlagsField == null) throw new MissingFieldException("PackWarden: BlueprintComponent.m_Flags not found");
                if (Convert.ToInt32(ComponentFlagsField.GetValue(c)) != 0)
                    throw new InvalidOperationException("PackWarden: AnimalCompanionSelectionHunter component " + c.name + " has m_Flags=" + ComponentFlagsField.GetValue(c) + " (expected 0)");
                var srcFeat = ((AddFeatureOnApply)c).Feature;
                if (srcFeat == null) throw new InvalidOperationException("PackWarden: AddFeatureOnApply " + c.name + " has no resolvable m_Feature");
                var copy = new AddFeatureOnApply();
                // AddFeatureOnApply.m_Feature: private BlueprintFeatureReference, get_Feature 가 읽는 유일한 필드(dnlib 2026-11-07)
                SetField(copy, "m_Feature", Ref<BlueprintFeatureReference>(srcFeat.AssetGuid.ToString()));
                if (copy.Feature != srcFeat) throw new InvalidOperationException("PackWarden: copied AddFeatureOnApply does not resolve to " + srcFeat.name);
                applied.Add(copy);
                appliedGuids.Add(srcFeat.AssetGuid.ToString());
            }
            var expectedApplied = new HashSet<string>(Guids.Game.CompanionSelectionAppliedFeatures.Select(Norm));
            if (applied.Count != expectedApplied.Count || !expectedApplied.SetEquals(appliedGuids))
                throw new InvalidOperationException("PackWarden: AnimalCompanionSelectionHunter AddFeatureOnApply features [" + Join(appliedGuids) + "] != expected [" + Join(expectedApplied) + "]");

            CompanionSelection = Create<BlueprintFeatureSelection>("PackWardenCompanionSelection", Guids.CompanionSelection, s =>
            {
                s.Group = origSel.Group;
                s.Group2 = origSel.Group2;
                s.Mode = origSel.Mode;
                s.Obligatory = origSel.Obligatory;
                s.IgnorePrerequisites = origSel.IgnorePrerequisites;
                s.ExceptWhiteListed = origSel.ExceptWhiteListed;
                s.ShowThisSelection = origSel.ShowThisSelection;
                s.IsClassFeature = origSel.IsClassFeature;
                s.Ranks = origSel.Ranks;
                s.HideInUI = origSel.HideInUI;
                s.HideInCharacterSheetAndLevelUp = origSel.HideInCharacterSheetAndLevelUp;
                s.HideNotAvailibleInUI = origSel.HideNotAvailibleInUI;
                s.Groups = origSel.Groups != null ? origSel.Groups.ToArray() : null;
                Set(s, "m_DisplayName", Strings.L("CompanionSelection.Name"));
                Set(s, "m_Description", Strings.L(CompanionText.DescriptionKey()));
                Set(s, "m_DescriptionShort", Strings.L(CompanionText.DescriptionKey()));
                Set(s, "m_Icon", GetF(origSel, "m_Icon"));
                Set(s, "m_AllFeatures", kept.ToArray());
                Set(s, "m_Features", kept.ToArray());
            });

            AddComponents(CompanionSelection, applied.ToArray());
            var readback = CompanionSelection.ComponentsArray.OfType<AddFeatureOnApply>().Select(x => x.Feature).ToArray();
            if (CompanionSelection.ComponentsArray.Length != expectedApplied.Count || readback.Length != expectedApplied.Count
                || readback.Any(f => f == null) || !expectedApplied.SetEquals(readback.Select(f => f.AssetGuid.ToString())))
                throw new InvalidOperationException("PackWarden: CompanionSelection AddFeatureOnApply readback failed");

            var hunter = Get<BlueprintCharacterClass>(Guids.Game.HunterClass);
            // --- Step 2: 아키타입 ---
            Bp = Create<BlueprintArchetype>("PackWardenArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("Archetype.Name");
                a.LocalizedDescription = Strings.L("Archetype.Description");
                a.LocalizedDescriptionShort = Strings.L("Archetype.DescriptionShort");
                Set(a, "m_ParentClass", hunter);
                a.RemoveSpellbook = true;
                a.OverrideAttributeRecommendations = true;
                 
                a.RecommendedAttributes = new[] { StatType.Wisdom, StatType.Dexterity, StatType.Constitution };
                a.NotRecommendedAttributes = new[] { StatType.Charisma, StatType.Intelligence };
                Set(a, "m_Difficulty", 2);
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
            });

            // --- Step 3: 레벨표 고정 항목 ---
            RemoveAt(1, Get<BlueprintFeatureBase>(Guids.Game.AnimalCompanionSelectionHunter));
            RemoveAt(1, Get<BlueprintFeatureBase>(Guids.Game.HunterOrisonsFeature));
            RemoveAt(1, Get<BlueprintFeatureBase>(Guids.Game.HunterAnimalFocusFeature));
            RemoveAt(8, Get<BlueprintFeatureBase>(Guids.Game.HunterExtraAnimalFocus));
             

            var tandemStudy = Get<BlueprintFeatureBase>(Guids.Game.TandemStudyTarget);
            var tandemBase = Get<BlueprintFeatureBase>(Guids.Game.TandemBase);
            var tandemTech = Get<BlueprintFeatureBase>(Guids.Game.TandemTechniques);
            var swift = Get<BlueprintFeatureBase>(Guids.Game.SlayerSwiftStudyTarget);
            AddAt(1, CompanionSelection);
            AddAt(1, tandemStudy);
            AddAt(4, tandemBase);
            AddAt(4, tandemTech);
            AddAt(7, swift);
            AddAt(7, tandemTech);
            AddAt(10, tandemTech);
            AddAt(13, tandemTech);
            AddAt(16, tandemTech);
            AddAt(19, tandemTech);

        }

        // HunterClass.m_Archetypes 에 등록(새 배열). Blueprints.BuildAll 이 모든 빌드가 성공한 뒤 마지막에 한 번만 부른다.
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("PackWarden: Archetype.Register() before Archetype.Build()");
            if (registered) throw new InvalidOperationException("PackWarden: Archetype.Register() called twice");
            var hunter = Get<BlueprintCharacterClass>(Guids.Game.HunterClass);
            var old = (BlueprintArchetypeReference[])GetF(hunter, "m_Archetypes") ?? new BlueprintArchetypeReference[0];
            if (old.Any(r => r != null && r.Get() == Bp)) throw new InvalidOperationException("PackWarden: archetype already in HunterClass.m_Archetypes");
            Set(hunter, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Guids.Archetype) }).ToArray());
            if (!hunter.Archetypes.Contains(Bp)) throw new InvalidOperationException("PackWarden: HunterClass.Archetypes readback does not contain the archetype");
            registered = true;
        }

        public static void AddAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f);
            Verify(Bp.AddFeatures, level, f, true, "AddAt");
        }

        // 제거 대상은 헌터 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패.
        public static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var prog = Get<BlueprintProgression>(Guids.Game.HunterProgression);
            var entry = prog.LevelEntries.FirstOrDefault(e => e.Level == level);
            if (entry == null || !entry.Features.Any(x => x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("PackWarden: " + f.name + " is not in HunterProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f);
            Verify(Bp.RemoveFeatures, level, f, true, "RemoveAt");
        }

        // 항상 새 배열·새 LevelEntry 를 돌려준다(원본 배열을 직접 바꾸지 않음).
        // LevelEntry.SetFeatures 는 m_Features 를 쓰고 m_FeaturesList 캐시를 비운다(IL 확인 2026-11-06).
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f)
        {
            var list = (src ?? new LevelEntry[0]).ToList();
            int idx = list.FindIndex(e => e.Level == level);
            var features = new List<BlueprintFeatureBase>();
            if (idx >= 0) features.AddRange(list[idx].Features);
            features.Add(f);
            var ne = new LevelEntry { Level = level };
            ne.SetFeatures(features);
            if (idx >= 0) list[idx] = ne; else list.Add(ne);
            return list.ToArray();
        }

        // 게임이 읽는 경로(entry.Features)로 되읽어 확인한다. 쓰기가 안 보이면 빌드 시점에 실패.
        static void Verify(LevelEntry[] entries, int level, BlueprintFeatureBase f, bool shouldBePresent, string what)
        {
            var entry = entries.FirstOrDefault(e => e.Level == level);
            bool present = entry != null && entry.Features.Any(x => x.AssetGuid == f.AssetGuid);
            if (present != shouldBePresent)
                throw new InvalidOperationException("PackWarden: " + what + " readback failed: " + f.name + " level " + level + " present=" + present);
        }
    }
}
