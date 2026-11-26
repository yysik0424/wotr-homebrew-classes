using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BlastMagus
{
    // 블라스트 메이거스 아키타입(메이거스). 귀검사 src\PhantomBlade\Archetype.cs 의 패턴을 그대로 따른다:
    // Build = 블루프린트 + RemoveFeatures, Register = MagusClass.m_Archetypes 등록(BuildAll 맨 끝, 한 번만). 레벨표 AddFeatures 는 각 빌더가 AddAt 으로 붙인다.
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registered;

        // 설계 §4.6 교환표 = 정찰 §1-1 (MagusProgression LevelEntries 직독, 2026-11-07): 16쌍 전부 진행표의 직접 항목.
        // 시험(tests\BlastMagusTests.cs)이 이 표를 dll 정적 초기화에서 읽어 정찰 표와 대조하고, blueprints.zip 의 MagusProgression 에 각 쌍이 있는지 본다.
        // RemoveAt 이 실행 시 같은 조건을 진행표에서 되읽는다(정찰 §1-2 CanAddArchetype: 그 레벨에 없으면 아키타입 선택 불가 → 우리는 빌드 실패로 먼저 드러낸다).
        static readonly (int level, string guid)[] Removed =
        {
            (1, Guids.Game.MagusCantripsFeature),
            (1, Guids.Game.DetectMagic),
            (4, Guids.Game.MagusSpellRecallFeature),
            (5, Guids.Game.ArcaneWeaponPlus2),
            (9, Guids.Game.ArcaneWeaponPlus3),
            (11, Guids.Game.MagusImprovedSpellRecallFeature),
            (13, Guids.Game.ArcaneWeaponPlus4),
            (13, Guids.Game.ArcaneHeavyArmor),
            (17, Guids.Game.ArcaneWeaponPlus5),
             
            // 레벨 = MagusProgression 직독(설계 §4.6 「정확히 레벨 3·6·9·12·15·18」, 시험 arcana: levels vs jbp). PoolArcana 는 이 표에서 레벨을 파생한다(RemovedLevels, C4).
            (1, Guids.Game.ArcanePoolFeature),
            (3, Guids.Game.MagusArcanaSelection),
            (6, Guids.Game.MagusArcanaSelection),
            (9, Guids.Game.MagusArcanaSelection),
            (12, Guids.Game.MagusArcanaSelection),
            (15, Guids.Game.MagusArcanaSelection),
            (18, Guids.Game.MagusArcanaSelection),
        };

        // 제거 표에서 그 GUID 가 지워지는 레벨들(오름차순). 표에 없으면 던진다(교체 특성을 붙일 레벨이 없다 = 표와 빌더가 어긋남).
        internal static int[] RemovedLevels(string guid)
        {
            if (guid == null) throw new ArgumentNullException("guid");
            var levels = Removed.Where(r => r.guid == guid).Select(r => r.level).OrderBy(x => x).ToArray();
            if (levels.Length == 0) throw new InvalidOperationException("BlastMagus: " + guid + " is not in Archetype.Removed");
            return levels;
        }

         
        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("BlastMagus: Archetype.Build() called twice");
            var magus = Get<BlueprintCharacterClass>(GameGuids.MagusClass);
            Bp = Create<BlueprintArchetype>("BlastMagusArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("Archetype.Name");
                a.LocalizedDescription = Strings.L("Archetype.Description");
                a.LocalizedDescriptionShort = Strings.L("Archetype.DescriptionShort");
                SetField(a, "m_ParentClass", magus);
                 
                a.OverrideAttributeRecommendations = true;
                a.RecommendedAttributes = new[] { StatType.Intelligence, StatType.Strength, StatType.Dexterity };
                a.NotRecommendedAttributes = new[] { StatType.Charisma };
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
            });
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), magus)) throw new InvalidOperationException("BlastMagus: archetype parent class readback is not MagusClass");

            foreach (var r in Removed) RemoveAt(r.level, Get<BlueprintFeatureBase>(r.guid));
            int removed = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (removed != Removed.Length)
                throw new InvalidOperationException("BlastMagus: RemoveFeatures has " + removed + " entries, expected " + Removed.Length);
        }

        // MagusClass.m_Archetypes 에 등록(새 배열). Blueprints.BuildAll 이 모든 빌드가 성공한 뒤 마지막에 한 번만 부른다.
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("BlastMagus: Archetype.Register() before Archetype.Build()");
            if (registered) throw new InvalidOperationException("BlastMagus: Archetype.Register() called twice");
            var magus = Get<BlueprintCharacterClass>(GameGuids.MagusClass);
            // 정찰 §1-2: 본편 MagusClass.m_Archetypes = 8개. null 이면 클래스가 깨진 것 → 빈 목록으로 덮지 않고 던진다.
            var old = (BlueprintArchetypeReference[])GetF(magus, "m_Archetypes");
            if (old == null) throw new InvalidOperationException("BlastMagus: MagusClass.m_Archetypes is null (broken magus class?)");
            if (old.Any(r => r != null && r.Get() == Bp)) throw new InvalidOperationException("BlastMagus: archetype already in MagusClass.m_Archetypes");
            SetField(magus, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Guids.Archetype) }).ToArray());
            var back = (BlueprintArchetypeReference[])GetF(magus, "m_Archetypes");
            if (back == null || back.Length != old.Length + 1 || !magus.Archetypes.Contains(Bp))
                throw new InvalidOperationException("BlastMagus: MagusClass.m_Archetypes readback does not contain the archetype exactly once more (" + (back == null ? "null" : back.Length.ToString()) + " vs " + old.Length + "+1)");
            registered = true;
        }

        public static void AddAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (Bp == null) throw new InvalidOperationException("BlastMagus: Archetype.AddAt before Archetype.Build()");
            if (registered) throw new InvalidOperationException("BlastMagus: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f);
            Verify(Bp.AddFeatures, level, f, "AddAt");
        }

        // 제거 대상은 메이거스 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패(TTT 등이 진행표를 바꾼 경우 여기서 드러난다, 정찰 §1-2).
        static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var prog = Get<BlueprintProgression>(Guids.Game.MagusProgression);
            // 정찰 §1-1: MagusProgression 은 레벨당 LevelEntry 1개 → FirstOrDefault 가 안전. 그래도 전부 합쳐서 본다(GetRemoveEntry 와 같은 의미).
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("BlastMagus: " + f.name + " is not in MagusProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f);
            Verify(Bp.RemoveFeatures, level, f, "RemoveAt");
        }

        // 항상 새 배열·새 LevelEntry (원본 배열을 직접 바꾸지 않음). LevelEntry.SetFeatures 는 m_Features 를 쓰고 캐시를 비운다(PhantomBlade·PackWarden 동일).
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f)
        {
            if (src == null) throw new InvalidOperationException("BlastMagus: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
            var list = src.ToList();
            int idx = list.FindIndex(e => e.Level == level);
            var features = new List<BlueprintFeatureBase>();
            if (idx >= 0) features.AddRange(list[idx].Features);
            features.Add(f);
            var ne = new LevelEntry { Level = level };
            ne.SetFeatures(features);
            if (idx >= 0) list[idx] = ne; else list.Add(ne);
            return list.ToArray();
        }

        // 게임이 읽는 경로(entry.Features)로 되읽어 확인한다. 같은 특성을 같은 레벨에 두 번 넣지 않는다.
        static void Verify(LevelEntry[] entries, int level, BlueprintFeatureBase f, string what)
        {
            var entry = entries.FirstOrDefault(e => e.Level == level);
            int n = entry == null ? 0 : entry.Features.Count(x => x.AssetGuid == f.AssetGuid);
            if (n != 1)
                throw new InvalidOperationException("BlastMagus: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected 1)");
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
