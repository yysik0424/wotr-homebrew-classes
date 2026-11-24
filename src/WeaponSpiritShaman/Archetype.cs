using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
    // 스피릿 웨폰 샤먼 아키타입(주술사). 블라스트 메이거스 src\BlastMagus\Archetype.cs 의 패턴을 따른다:
    // Build = 블루프린트 + RemoveFeatures, Register = ShamanClass.m_Archetypes 등록(BuildAll 맨 끝, 한 번만 — 등록 예외 F12, 판단은 순수 RegisterRule).
    // 레벨표 AddFeatures 는 이후 Task 의 빌더가 AddAt 으로 붙인다.
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registerCalled;
        // 되읽기 단언까지 끝난 등록만 true(점검 러너 BlueprintsBuilt 가 읽는다).
        public static bool Registered { get; private set; }
        // 등록 직전 ShamanClass.m_Archetypes 배열(점검 S22 의 「우리 변경만」 기준). Register 전 = null.
        public static BlueprintArchetypeReference[] BeforeRegister { get; private set; }

        // 설계 §4.2 표 = 정찰 §1 (ShamanProgression LevelEntries 직독, 2026-11-08): 4쌍 전부 진행표의 직접 항목, 스피릿 헌터 RemoveFeatures 와 같다.
        // 시험(tests\WeaponSpiritShamanTests.cs)이 이 표를 dll 정적 초기화에서 읽어 ScenarioPlan.RemovedPairs·jbp 와 대조한다.
        // RemoveAt 이 실행 시 같은 조건을 진행표에서 되읽는다(그 레벨에 없으면 아키타입 선택 불가 → 빌드 실패로 먼저 드러낸다).
        static readonly (int level, string guid)[] Removed =
        {
            (1, Guids.Game.ShamanSpiritAnimalSelection),
            (4, Guids.Game.ShamanWanderingSpiritFeature),
            (6, Guids.Game.ShamanWanderingHexFeature),
            (14, Guids.Game.ShamanWanderingHexFeature2),
        };

        // 블루프린트만 만든다. 추천 능력치는 본편 그대로(설계 §4.1 「지혜(본편 그대로)」 → OverrideAttributeRecommendations=false, 덮지 않는다).
        // 단 RecommendedAttributes 배열에는 주술사 값의 복사본을 둔다: 게임은 Override=false 면 이 배열을 읽지 않고(ClassData.get_RecommendedAttributes @0017~001C)
        // 공용 점검 TestUnit.Create 가 이 배열의 첫 값을 시험 유닛의 능력치 상승 대상으로 쓴다(src\Common\Checks\TestUnit.cs BuildLevels — 비면 던짐).
        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("WeaponSpiritShaman: Archetype.Build() called twice");
            var shaman = Get<BlueprintCharacterClass>(Guids.Game.ShamanClass);
            Bp = Create<BlueprintArchetype>("WeaponSpiritShamanArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("Archetype.Name");
                a.LocalizedDescription = Strings.L("Archetype.Description");
                a.LocalizedDescriptionShort = Strings.L("Archetype.DescriptionShort");
                SetField(a, "m_ParentClass", shaman);
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
                a.OverrideAttributeRecommendations = false;
                a.RecommendedAttributes = shaman.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = shaman.NotRecommendedAttributes.ToArray();
            });
            if (Bp.RecommendedAttributes.Length == 0 || !Bp.RecommendedAttributes.SequenceEqual(shaman.RecommendedAttributes) || Bp.OverrideAttributeRecommendations)
                throw new InvalidOperationException("WeaponSpiritShaman: archetype recommended-attribute copy readback failed");
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), shaman)) throw new InvalidOperationException("WeaponSpiritShaman: archetype parent class readback is not ShamanClass");

            foreach (var r in Removed) RemoveAt(r.level, Get<BlueprintFeatureBase>(r.guid));
            int removed = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (removed != Removed.Length)
                throw new InvalidOperationException("WeaponSpiritShaman: RemoveFeatures has " + removed + " entries, expected " + Removed.Length);
        }

        // 등록 예외(설계 §3 보충·§14, 계획 F12): ShamanClass.m_Archetypes 에 「원본 원소 + 우리」 새 배열을 1회 대입하고 되읽는다.
        // 이 모드가 본편 블루프린트 객체에 쓰는 유일한 곳(시험 exec: base-blueprint writes 가 고정).
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("WeaponSpiritShaman: Archetype.Register() before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("WeaponSpiritShaman: Archetype.Register() called twice");
            registerCalled = true;
            var shaman = Get<BlueprintCharacterClass>(Guids.Game.ShamanClass);
            var old = (BlueprintArchetypeReference[])GetF(shaman, "m_Archetypes");
            // 점검 S22 vanilla-untouched 의 기준(2026-11-15): 등록 직전 원소 배열(Merge 가 새 배열을 만들므로 이 배열·원소는 바뀌지 않는다). 동작에는 쓰지 않는다.
            BeforeRegister = old;
            var ours = Ref<BlueprintArchetypeReference>(Guids.Archetype);
            var merged = RegisterRule.Merge(old, ours, r => r.Guid.ToString());
            SetField(shaman, "m_Archetypes", merged);
            var back = (BlueprintArchetypeReference[])GetF(shaman, "m_Archetypes");
            RegisterRule.VerifyReadback(old, back, ours);
            // 게임이 읽는 경로(참조 해석)로도 정확히 한 번.
            int seen = shaman.Archetypes.Count(x => ReferenceEquals(x, Bp));
            if (seen != 1) throw new InvalidOperationException("WeaponSpiritShaman: ShamanClass.Archetypes resolves our archetype " + seen + " time(s), expected 1");
            Registered = true;
        }

        public static void AddAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (Bp == null) throw new InvalidOperationException("WeaponSpiritShaman: Archetype.AddAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("WeaponSpiritShaman: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f);
            Verify(Bp.AddFeatures, level, f, "AddAt");
        }

        // 제거 대상은 주술사 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패(TTT 등이 진행표를 바꾼 경우 여기서 드러난다).
        static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var prog = Get<BlueprintProgression>(Guids.Game.ShamanProgression);
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("WeaponSpiritShaman: " + f.name + " is not in ShamanProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f);
            Verify(Bp.RemoveFeatures, level, f, "RemoveAt");
        }

        // 항상 새 배열·새 LevelEntry (원본 배열을 직접 바꾸지 않음). LevelEntry.SetFeatures 는 m_Features 를 쓰고 캐시를 비운다(블라스트 메이거스와 같음).
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f)
        {
            if (src == null) throw new InvalidOperationException("WeaponSpiritShaman: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
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
                throw new InvalidOperationException("WeaponSpiritShaman: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected 1)");
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("WeaponSpiritShaman: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
