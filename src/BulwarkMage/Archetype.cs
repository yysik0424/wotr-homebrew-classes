using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
    // 중장법사 아키타입(위저드). 골렘술사 src\Golemancer\Archetype.cs 의 패턴:
    // Build = 블루프린트 + RemoveFeatures(BodyTable.Removed), Register = WizardClass.m_Archetypes 등록(BuildAll 맨 끝, 한 번만 — 판단은 순수 RegisterRule).
    // 레벨표 AddFeatures 는 각 빌더가 AddAt 으로 붙인다(Register 전에만).
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registerCalled;
        // 되읽기 단언까지 끝난 등록만 true(점검 러너 BlueprintsBuilt 가 읽는다).
        public static bool Registered { get; private set; }

        // 추천 능력치는 본편 그대로(OverrideAttributeRecommendations=false) — 단 RecommendedAttributes 에는 위저드 값의 복사본을 둔다:
        // 공용 점검 TestUnit.Create 가 이 배열의 첫 값을 시험 유닛의 능력치 상승 대상으로 쓴다(src\Common\Checks\TestUnit.cs:111-114 — 비면 던짐). 골렘술사와 같은 방침.
        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("BulwarkMage: Archetype.Build() called twice");
            var wizard = Get<BlueprintCharacterClass>(Guids.Game.WizardClass);
            Bp = Create<BlueprintArchetype>("BulwarkMageArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("archetype.name");
                a.LocalizedDescription = Strings.L("archetype.desc");
                a.LocalizedDescriptionShort = Strings.L("archetype.short");
                SetField(a, "m_ParentClass", wizard);
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
                a.OverrideAttributeRecommendations = false;
                a.RecommendedAttributes = wizard.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = wizard.NotRecommendedAttributes.ToArray();
            });
            if (Bp.RecommendedAttributes.Length == 0 || !Bp.RecommendedAttributes.SequenceEqual(wizard.RecommendedAttributes) || Bp.OverrideAttributeRecommendations)
                throw new InvalidOperationException("BulwarkMage: archetype recommended-attribute copy readback failed");
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), wizard)) throw new InvalidOperationException("BulwarkMage: archetype parent class readback is not WizardClass");

            var removed = BodyTable.Removed;
            if (removed == null || removed.Count == 0) throw new InvalidOperationException("BulwarkMage: BodyTable.Removed is empty");
            foreach (var r in removed) RemoveAt(r.level, Get<BlueprintFeatureBase>(r.guid));
            int n = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (n != removed.Count)
                throw new InvalidOperationException("BulwarkMage: RemoveFeatures has " + n + " entries, expected " + removed.Count);
        }

        // 등록 예외(계획 Global Constraints): WizardClass.m_Archetypes 에 「원본 원소 + 우리」 새 배열을 1회 대입하고 되읽는다.
        // 이 모드가 본편 블루프린트 객체에 쓰는 유일한 곳(시험 exec: base-blueprint writes 가 고정).
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("BulwarkMage: Archetype.Register() before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("BulwarkMage: Archetype.Register() called twice");
            registerCalled = true;
            var wizard = Get<BlueprintCharacterClass>(Guids.Game.WizardClass);
            var old = (BlueprintArchetypeReference[])GetF(wizard, "m_Archetypes");
            var ours = Ref<BlueprintArchetypeReference>(Guids.Archetype);
            var merged = RegisterRule.Merge(old, ours, r => r.Guid.ToString());
            SetField(wizard, "m_Archetypes", merged);
            var back = (BlueprintArchetypeReference[])GetF(wizard, "m_Archetypes");
            RegisterRule.VerifyReadback(old, back, ours);
            // 게임이 읽는 경로(참조 해석)로도 정확히 한 번.
            int seen = wizard.Archetypes.Count(x => ReferenceEquals(x, Bp));
            if (seen != 1) throw new InvalidOperationException("BulwarkMage: WizardClass.Archetypes resolves our archetype " + seen + " time(s), expected 1");
            Registered = true;
        }

        // level 에 f 를 count 번 둔다(기존 그 레벨 항목 뒤에 이어서). 되읽기 = 그 레벨의 f 개수가 정확히 count(같은 특성을 같은 레벨에 다시 AddAt 하면 실패).
        public static void AddAt(int level, BlueprintFeatureBase f, int count = 1)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (count < 1) throw new ArgumentOutOfRangeException("count", count, "BulwarkMage: Archetype.AddAt count must be >= 1");
            if (level < BulwarkRules.Levels.MinLevel || level > BulwarkRules.Levels.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "BulwarkMage: Archetype.AddAt level must be 1..20");
            if (Bp == null) throw new InvalidOperationException("BulwarkMage: Archetype.AddAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("BulwarkMage: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f, count);
            Verify(Bp.AddFeatures, level, f, count, "AddAt");
        }

        // 제거 대상은 위저드 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패(다른 모드가 진행표를 바꾼 경우 여기서 드러난다).
        static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var prog = Get<BlueprintProgression>(Guids.Game.WizardProgression);
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x != null && x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("BulwarkMage: " + f.name + " is not in WizardProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f, 1);
            Verify(Bp.RemoveFeatures, level, f, 1, "RemoveAt");
        }

        // 항상 새 배열·새 LevelEntry (원본 배열을 직접 바꾸지 않음). LevelEntry.SetFeatures 는 m_Features 를 쓰고 캐시를 비운다(골렘술사와 같음).
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f, int count)
        {
            if (src == null) throw new InvalidOperationException("BulwarkMage: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
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

        // 게임이 읽는 경로(entry.Features)로 되읽어 확인한다. 그 레벨의 f 개수 == count.
        static void Verify(LevelEntry[] entries, int level, BlueprintFeatureBase f, int count, string what)
        {
            var entry = entries.FirstOrDefault(e => e.Level == level);
            int n = entry == null ? 0 : entry.Features.Count(x => x != null && x.AssetGuid == f.AssetGuid);
            if (n != count)
                throw new InvalidOperationException("BulwarkMage: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected " + count + ")");
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("BulwarkMage: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
