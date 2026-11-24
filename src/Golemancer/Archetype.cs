using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 골렘술사 아키타입(연금술사). 블라스트 메이거스 src\BlastMagus\Archetype.cs · 스피릿 웨폰 샤먼 src\WeaponSpiritShaman\Archetype.cs 의 패턴:
    // Build = 블루프린트 + RemoveFeatures, Register = AlchemistClass.m_Archetypes 등록(BuildAll 맨 끝, 한 번만 — 판단은 순수 RegisterRule).
    // 레벨표 AddFeatures 는 각 빌더가 AddAt 으로 붙인다(Register 전에만). AddAt 은 같은 특성을 한 레벨에 count 번 둘 수 있다(반대 학파 선택 3회 — 계획 T1 Interfaces).
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registerCalled;
        // 되읽기 단언까지 끝난 등록만 true(점검 러너 BlueprintsBuilt 가 읽는다).
        public static bool Registered { get; private set; }

        // 설계 §4.2 표(AlchemistProgression LevelEntries 직독): 4쌍 전부 진행표의 직접 항목. 독 내성은 같은 GUID 랭크형 3단계(§0-3).
        // 시험(tests\GolemancerTests.cs)이 이 표를 dll 정적 초기화에서 읽어 ScenarioPlan.RemovedPairs·jbp 와 대조한다.
        // RemoveAt 이 실행 시 같은 조건을 진행표에서 되읽는다(그 레벨에 없으면 아키타입 선택 불가 → 빌드 실패로 먼저 드러낸다).
        static readonly (int level, string guid)[] Removed =
        {
            (2, Guids.Game.PoisonResistance),
            (5, Guids.Game.PoisonResistance),
            (8, Guids.Game.PoisonResistance),
            (10, Guids.Game.PoisonImmunity),
        };

        // 블루프린트만 만든다. 추천 능력치는 본편 그대로(설계 §4.1 「지능(본편 그대로)」 → OverrideAttributeRecommendations=false).
        // 단 RecommendedAttributes 배열에는 연금술사 값의 복사본을 둔다: 게임은 Override=false 면 이 배열을 읽지 않고(ClassData.get_RecommendedAttributes @0017~001C)
        // 공용 점검 TestUnit.Create 가 이 배열의 첫 값을 시험 유닛의 능력치 상승 대상으로 쓴다(src\Common\Checks\TestUnit.cs:111-114 — 비면 던짐).
        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("Golemancer: Archetype.Build() called twice");
            var alchemist = Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass);
            ProgressionBefore = Snapshot(out progressionBeforeError);
            Bp = Create<BlueprintArchetype>("GolemancerArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("Archetype.Name");
                a.LocalizedDescription = Strings.L("Archetype.Description");
                a.LocalizedDescriptionShort = Strings.L("Archetype.DescriptionShort");
                SetField(a, "m_ParentClass", alchemist);
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
                a.OverrideAttributeRecommendations = false;
                a.RecommendedAttributes = alchemist.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = alchemist.NotRecommendedAttributes.ToArray();
            });
            if (Bp.RecommendedAttributes.Length == 0 || !Bp.RecommendedAttributes.SequenceEqual(alchemist.RecommendedAttributes) || Bp.OverrideAttributeRecommendations)
                throw new InvalidOperationException("Golemancer: archetype recommended-attribute copy readback failed");
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), alchemist)) throw new InvalidOperationException("Golemancer: archetype parent class readback is not AlchemistClass");

            foreach (var r in Removed) RemoveAt(r.level, Get<BlueprintFeatureBase>(r.guid));
            int removed = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (removed != Removed.Length)
                throw new InvalidOperationException("Golemancer: RemoveFeatures has " + removed + " entries, expected " + Removed.Length);
        }

        // 등록 예외(계획 Global Constraints): AlchemistClass.m_Archetypes 에 「원본 원소 + 우리」 새 배열을 1회 대입하고 되읽는다.
        // 이 모드가 본편 블루프린트 객체에 쓰는 유일한 곳(시험 exec: base-blueprint writes 가 고정).
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("Golemancer: Archetype.Register() before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("Golemancer: Archetype.Register() called twice");
            registerCalled = true;
            var alchemist = Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass);
            var old = (BlueprintArchetypeReference[])GetF(alchemist, "m_Archetypes");
            var ours = Ref<BlueprintArchetypeReference>(Guids.Archetype);
            var merged = RegisterRule.Merge(old, ours, r => r.Guid.ToString());
            SetField(alchemist, "m_Archetypes", merged);
            var back = (BlueprintArchetypeReference[])GetF(alchemist, "m_Archetypes");
            RegisterRule.VerifyReadback(old, back, ours);
            // 게임이 읽는 경로(참조 해석)로도 정확히 한 번.
            int seen = alchemist.Archetypes.Count(x => ReferenceEquals(x, Bp));
            if (seen != 1) throw new InvalidOperationException("Golemancer: AlchemistClass.Archetypes resolves our archetype " + seen + " time(s), expected 1");
            ProgressionAfter = Snapshot(out progressionAfterError);
            Registered = true;
        }

        // 점검 S1 kept-vanilla-features(2026-11-15 판정 「우리 변경만 확인」): 우리 빌드 직전(Build 첫머리)·직후(Register 끝)의 AlchemistProgression 항목 「레벨:GUID」(중복 유지).
        // 다른 모드(TabletopTweaks AlternateCapstones 등)가 우리 전에 바꾼 진행표는 직전 스냅숏에 이미 들어 있다 — 비교는 우리 빌드가 바꾼 것만 본다(KeptRule).
        // 읽기만. 읽다가 실패(남의 모드의 해석 안 되는 참조 등)해도 빌드는 막지 않는다 — 점검 전용 자료라서. 대신 null + 오류 문구를 남기고 S1 이 「판정 불가」로 FAIL 한다(조용히 통과 금지).
        public static string[] ProgressionBefore { get; private set; }
        public static string[] ProgressionAfter { get; private set; }
        static string progressionBeforeError, progressionAfterError;




        static string[] Snapshot(out string error)
        {
            error = null;
            try
            {
                var prog = Get<BlueprintProgression>(Guids.Game.AlchemistProgression);
                if (prog.LevelEntries == null) { error = "AlchemistProgression.LevelEntries is null"; return null; }
                return prog.LevelEntries.SelectMany(e => e.Features.Select(f => e.Level + ":" + f.AssetGuid.ToString())).ToArray();
            }
            catch (Exception ex) { error = ex.GetType().Name + ": " + ex.Message; ModContext.LogError("Golemancer: AlchemistProgression snapshot for check S1 failed: " + error); return null; }
        }

        // level 에 f 를 count 번 둔다(기존 그 레벨 항목 뒤에 이어서). 되읽기 = 그 레벨의 f 개수가 정확히 count(같은 특성을 같은 레벨에 다시 AddAt 하면 실패).
        public static void AddAt(int level, BlueprintFeatureBase f, int count = 1)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (count < 1) throw new ArgumentOutOfRangeException("count", count, "Golemancer: Archetype.AddAt count must be >= 1");
            if (level < GolemRules.MinLevel || level > GolemRules.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "Golemancer: Archetype.AddAt level must be 1..20");
            if (Bp == null) throw new InvalidOperationException("Golemancer: Archetype.AddAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("Golemancer: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f, count);
            Verify(Bp.AddFeatures, level, f, count, "AddAt");
        }

        // 제거 대상은 연금술사 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패(TTT 등이 진행표를 바꾼 경우 여기서 드러난다).
        static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var prog = Get<BlueprintProgression>(Guids.Game.AlchemistProgression);
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("Golemancer: " + f.name + " is not in AlchemistProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f, 1);
            Verify(Bp.RemoveFeatures, level, f, 1, "RemoveAt");
        }

        // 항상 새 배열·새 LevelEntry (원본 배열을 직접 바꾸지 않음). LevelEntry.SetFeatures 는 m_Features 를 쓰고 캐시를 비운다(블라스트 메이거스와 같음).
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f, int count)
        {
            if (src == null) throw new InvalidOperationException("Golemancer: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
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
            int n = entry == null ? 0 : entry.Features.Count(x => x.AssetGuid == f.AssetGuid);
            if (n != count)
                throw new InvalidOperationException("Golemancer: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected " + count + ")");
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("Golemancer: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
