using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.UnitLogic.Alignments;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
    // 카오스 워리어 아키타입(워프리스트). src\BulwarkMage\Archetype.cs 의 패턴:
    // Build = 블루프린트 + 성향 선행(CN·CE, ArchetypeAlignment — Rageshaper 선례), Register = WarpriestClass.m_Archetypes 등록(BuildAll 맨 끝, 한 번만 — 판단은 순수 RegisterRule).
    // 레벨표 AddFeatures 는 각 빌더가 AddAt 으로, RemoveFeatures 는 ApplyLevelTable(T3)이 RemoveAt 으로 붙인다(Register 전에만).
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registerCalled;
        public static bool Registered { get; private set; }

        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("ChaosWarrior: Archetype.Build() called twice");
            var warpriest = Get<BlueprintCharacterClass>(Guids.Game.WarpriestClass);
            Bp = Create<BlueprintArchetype>("ChaosWarriorArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("archetype.name");
                a.LocalizedDescription = Strings.L("archetype.desc");
                a.LocalizedDescriptionShort = Strings.L("archetype.desc");
                SetField(a, "m_ParentClass", warpriest);
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
                a.OverrideAttributeRecommendations = false;
                // 공용 TestUnit.Create 가 첫 값을 시험 유닛 능력치 상승 대상으로 쓴다(src\Common\Checks\TestUnit.cs:111-114 — 비면 던짐).
                a.RecommendedAttributes = warpriest.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = warpriest.NotRecommendedAttributes.ToArray();
            });
            if (Bp.RecommendedAttributes.Length == 0 || !Bp.RecommendedAttributes.SequenceEqual(warpriest.RecommendedAttributes) || Bp.OverrideAttributeRecommendations)
                throw new InvalidOperationException("ChaosWarrior: archetype recommended-attribute copy readback failed");
            if (!ReferenceEquals(BuildKit.GetF(Bp, "m_ParentClass"), warpriest)) throw new InvalidOperationException("ChaosWarrior: archetype parent class readback is not WarpriestClass");

            // 성향 CN·CE(설계 §3): 첫 캐릭터 레벨이면 성향 선택지를 이 마스크로 제한, 그 밖엔 현재 성향 검사(PrerequisiteAlignment.CheckInternal @0000~005B, 부록 A-e). 신 선행은 두지 않는다.
            var align = new PrerequisiteAlignment
            {
                Alignment = (AlignmentMaskType)ChaosRules.Alignment.Mask,
                ArchetypeAlignment = true,
                IngorePrerequisiteCheck = false,
                Group = Prerequisite.GroupType.All,
                CheckInProgression = false,
                HideInUI = false,
            };
            AddComponents(Bp, align);
            var back = Bp.ComponentsArray.OfType<PrerequisiteAlignment>().ToArray();
            if (back.Length != 1 || (int)back[0].Alignment != ChaosRules.Alignment.Mask || !back[0].ArchetypeAlignment)
                throw new InvalidOperationException("ChaosWarrior: archetype alignment prerequisite readback failed");
            if (Bp.ComponentsArray.Length != 1) throw new InvalidOperationException("ChaosWarrior: archetype has " + Bp.ComponentsArray.Length + " components, expected only the alignment prerequisite (no deity prerequisite)");
        }

         
        // → 통과하면 행마다 RemoveAt(같은 순회 = LevelTable.Removals) → 되읽기: RemoveFeatures 항목 합 == Removals 수.
        public static void ApplyLevelTable()
        {
            if (Bp == null) throw new InvalidOperationException("ChaosWarrior: Archetype.ApplyLevelTable() before Archetype.Build()");
            var prog = Get<BlueprintProgression>(Guids.Game.WarpriestProgression);
            var rows = prog.LevelEntries.SelectMany(e => e.Features.Select(f =>
            {
                if (f == null) throw new InvalidOperationException("ChaosWarrior: WarpriestProgression level " + e.Level + " has a null feature");
                return new LevelRow(e.Level, f.AssetGuid.ToString(), f.name);
            })).ToList();
            var removals = LevelTable.Removals;
            var left = LevelTable.Apply(rows, removals);
            if (left.Count != rows.Count - removals.Count) throw new InvalidOperationException("ChaosWarrior: LevelTable.Apply left " + left.Count + " of " + rows.Count + " entries, expected " + (rows.Count - removals.Count));
            foreach (var r in removals) RemoveAt(r.Level, r.Guid);
            int n = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (n != removals.Count) throw new InvalidOperationException("ChaosWarrior: RemoveFeatures has " + n + " entries, expected " + removals.Count);
        }

        // 등록 예외(계획 Global Constraints ①): WarpriestClass.m_Archetypes 에 「원본 + 우리」 새 배열 1회 대입 후 되읽기.
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("ChaosWarrior: Archetype.Register() before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("ChaosWarrior: Archetype.Register() called twice");
            registerCalled = true;
            var warpriest = Get<BlueprintCharacterClass>(Guids.Game.WarpriestClass);
            var old = (BlueprintArchetypeReference[])BuildKit.GetF(warpriest, "m_Archetypes");
            var ours = Ref<BlueprintArchetypeReference>(Guids.Archetype);
            var merged = RegisterRule.Merge(old, ours, r => r.Guid.ToString());
            SetField(warpriest, "m_Archetypes", merged);
            var back = (BlueprintArchetypeReference[])BuildKit.GetF(warpriest, "m_Archetypes");
            RegisterRule.VerifyReadback(old, back, ours);
            int seen = warpriest.Archetypes.Count(x => ReferenceEquals(x, Bp));
            if (seen != 1) throw new InvalidOperationException("ChaosWarrior: WarpriestClass.Archetypes resolves our archetype " + seen + " time(s), expected 1");
            Registered = true;
        }

        // level 에 f 를 count 번 둔다. 되읽기 = 그 레벨의 f 개수가 정확히 count.
        public static void AddAt(int level, BlueprintFeatureBase f, int count = 1)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (count < 1) throw new ArgumentOutOfRangeException("count", count, "ChaosWarrior: Archetype.AddAt count must be >= 1");
            ChaosRules.RequireLevel(level);
            if (Bp == null) throw new InvalidOperationException("ChaosWarrior: Archetype.AddAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("ChaosWarrior: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f, count);
            Verify(Bp.AddFeatures, level, f, count, "AddAt");
        }

        // 제거 대상은 워프리스트 진행표의 그 레벨에 실제로 있어야 한다(엔진 BlueprintArchetype.GetRemoveEntry = 같은 레벨·같은 GUID). 아니면 실패.
        public static void RemoveAt(int level, string guid)
        {
            if (string.IsNullOrEmpty(guid)) throw new ArgumentException("ChaosWarrior: RemoveAt without a GUID", "guid");
            ChaosRules.RequireLevel(level);
            if (Bp == null) throw new InvalidOperationException("ChaosWarrior: Archetype.RemoveAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("ChaosWarrior: Archetype.RemoveAt after Register()");
            var f = Get<BlueprintFeatureBase>(guid);
            var prog = Get<BlueprintProgression>(Guids.Game.WarpriestProgression);
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x != null && x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("ChaosWarrior: " + f.name + " is not in WarpriestProgression level " + level + " (nothing to remove)");
            int before = Bp.RemoveFeatures.Where(e => e.Level == level).SelectMany(e => e.Features).Count(x => x != null && x.AssetGuid == f.AssetGuid);
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f, 1);
            Verify(Bp.RemoveFeatures, level, f, before + 1, "RemoveAt");
        }

        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f, int count)
        {
            if (src == null) throw new InvalidOperationException("ChaosWarrior: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
            var list = src.ToList();
            int idx = list.FindIndex(e => e.Level == level);
            var features = new List<BlueprintFeatureBase>();
            if (idx >= 0) features.AddRange(list[idx].Features);
            for (int i = 0; i < count; i++) features.Add(f);
            var ne = new LevelEntry { Level = level };
            ne.SetFeatures(features);
            if (idx >= 0) list[idx] = ne; else list.Add(ne);
            return list.ToArray();
        }

        static void Verify(LevelEntry[] entries, int level, BlueprintFeatureBase f, int count, string what)
        {
            var entry = entries.FirstOrDefault(e => e.Level == level);
            int n = entry == null ? 0 : entry.Features.Count(x => x != null && x.AssetGuid == f.AssetGuid);
            if (n != count)
                throw new InvalidOperationException("ChaosWarrior: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected " + count + ")");
        }


    }
}
