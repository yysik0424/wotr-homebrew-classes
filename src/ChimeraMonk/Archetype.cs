using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
    // 키메라 몽크 아키타입(본편 몽크). 중장법사 src\BulwarkMage\Archetype.cs 의 패턴:
    // Build = 블루프린트 + 레벨표(T1: 1레벨 FCT; T3 부터 LevelPlan), Register = MonkClass.m_Archetypes 등록(Builders 맨 끝, 한 번만 — 판단은 순수 RegisterRule).
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registerCalled;
        // 되읽기 단언까지 끝난 등록만 true(점검 러너 BlueprintsBuilt 가 읽는다).
        public static bool Registered { get; private set; }

        // 추천 능력치는 본편 그대로(OverrideAttributeRecommendations=false) — RecommendedAttributes 에는 몽크 값의 복사본을 둔다:
        // 공용 점검 TestUnit.Create 가 이 배열의 첫 값을 시험 유닛의 능력치 상승 대상으로 쓴다(src\Common\Checks\TestUnit.cs:111-114 — 비면 던짐).
        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("ChimeraMonk: Archetype.Build() called twice");
            var monk = Get<BlueprintCharacterClass>(Guids.Game.MonkClass);
            Bp = Create<BlueprintArchetype>("ChimeraMonkArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("archetype.name");
                a.LocalizedDescription = Strings.L("archetype.desc");
                a.LocalizedDescriptionShort = Strings.L("archetype.desc");
                SetField(a, "m_ParentClass", monk);
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
                a.OverrideAttributeRecommendations = false;
                a.RecommendedAttributes = monk.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = monk.NotRecommendedAttributes.ToArray();
            });
            if (Bp.RecommendedAttributes.Length == 0 || !Bp.RecommendedAttributes.SequenceEqual(monk.RecommendedAttributes) || Bp.OverrideAttributeRecommendations)
                throw new InvalidOperationException("ChimeraMonk: archetype recommended-attribute copy readback failed");
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), monk)) throw new InvalidOperationException("ChimeraMonk: archetype parent class readback is not MonkClass");

            // 제거 = 생산 입력 LevelPlan.Removals(레벨·GUID 리터럴 없음 — `wiring: removals`). 본편 진행표의 그 레벨에 없으면 RemoveAt 이 던진다.
            var removals = LevelPlan.Removals;
            if (removals == null || removals.Count == 0) throw new InvalidOperationException("ChimeraMonk: LevelPlan.Removals is empty");
            foreach (var r in removals) RemoveAt(r.Level, Get<BlueprintFeatureBase>(r.Guid));
            int n = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (n != removals.Count) throw new InvalidOperationException("ChimeraMonk: RemoveFeatures has " + n + " entries, expected " + removals.Count);
            VerifyCoreRemovals();

            // 1레벨 FCT(설계 §6 [판정]). 관문(DependencyGate)이 이미 해석을 확인했다 — 여기서 없으면 Get 이 던진다(빌더 실패로 계수).
            var fct = Get<BlueprintFeature>(Guids.External.FeralCombatTraining);
            if (fct.name != Guids.External.FeralCombatTrainingName) throw new InvalidOperationException("ChimeraMonk: FCT blueprint name is '" + fct.name + "', expected " + Guids.External.FeralCombatTrainingName);
            AddPlanned(LevelPlan.KeyFct, fct);
        }

        // 계획 키 → 붙인 블루프린트 GUID(Register 의 되읽기·점검 built-matches-plans 가 읽는다).
        static readonly Dictionary<string, string> planned = new Dictionary<string, string>();
        public static IReadOnlyDictionary<string, string> Planned { get { return planned; } }

        // LevelPlan.Additions 의 key 행마다(같은 레벨 행 수만큼) f 를 붙인다. 키가 계획에 없거나 두 번 = 던짐.
        public static void AddPlanned(string key, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var rows = LevelPlan.Additions.Where(r => r.Key == key).ToList();
            if (rows.Count == 0) throw new InvalidOperationException("ChimeraMonk: LevelPlan.Additions has no row for key " + key);
            if (planned.ContainsKey(key)) throw new InvalidOperationException("ChimeraMonk: AddPlanned(" + key + ") called twice");
            foreach (var g in rows.GroupBy(r => r.Level)) AddAt(g.Key, f, g.Count());
            planned.Add(key, f.AssetGuid.ToString());
        }

        // 등록 예외(계획 Global Constraints): MonkClass.m_Archetypes 에 「원본 원소 + 우리」 새 배열을 1회 대입하고 되읽는다.
        // 이 모드가 본편 블루프린트 객체에 쓰는 유일한 곳.
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("ChimeraMonk: Archetype.Register() before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("ChimeraMonk: Archetype.Register() called twice");
            registerCalled = true;
            // 등록 전 되읽기: 레벨표 == LevelPlan(제거·추가 전 행), 변이 선택 9칸(계획 T3 Step 3). 어긋나면 던짐 → 빌더 실패로 계수, 등록 안 함.
            var diff = Readback.LevelDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: level table readback: " + string.Join(" | ", diff));
            var monk = Get<BlueprintCharacterClass>(Guids.Game.MonkClass);
            var old = (BlueprintArchetypeReference[])GetF(monk, "m_Archetypes");
            var ours = Ref<BlueprintArchetypeReference>(Guids.Archetype);
            var merged = RegisterRule.Merge(old, ours, r => r.Guid.ToString());
            SetField(monk, "m_Archetypes", merged);
            var back = (BlueprintArchetypeReference[])GetF(monk, "m_Archetypes");
            RegisterRule.VerifyReadback(old, back, ours);
            int seen = monk.Archetypes.Count(x => ReferenceEquals(x, Bp));
            if (seen != 1) throw new InvalidOperationException("ChimeraMonk: MonkClass.Archetypes resolves our archetype " + seen + " time(s), expected 1");
            Registered = true;
        }

        // level 에 f 를 count 번 둔다(기존 그 레벨 항목 뒤에 이어서). 되읽기 = 그 레벨의 f 개수가 정확히 count.
        public static void AddAt(int level, BlueprintFeatureBase f, int count = 1)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (count < 1) throw new ArgumentOutOfRangeException("count", count, "ChimeraMonk: Archetype.AddAt count must be >= 1");
            if (level < ChimeraRules.Levels.MinLevel || level > ChimeraRules.Levels.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "ChimeraMonk: Archetype.AddAt level must be 1..20");
            if (Bp == null) throw new InvalidOperationException("ChimeraMonk: Archetype.AddAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("ChimeraMonk: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f, count);
            Verify(Bp.AddFeatures, level, f, count, "AddAt");
        }

        // 제거 대상은 몽크 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패.
        public static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (Bp == null) throw new InvalidOperationException("ChimeraMonk: Archetype.RemoveAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("ChimeraMonk: Archetype.RemoveAt after Register()");
            var prog = Get<BlueprintProgression>(Guids.Game.MonkProgression);
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x != null && x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("ChimeraMonk: " + f.name + " is not in MonkProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f, 1);
            Verify(Bp.RemoveFeatures, level, f, 1, "RemoveAt");
        }

        // 진행표·레벨표에서 guid 가 있는 레벨 목록(같은 레벨에 두 번이면 두 번 — 정렬).
        static List<int> LevelsOf(IEnumerable<LevelEntry> entries, string guid)
        {
            return entries.SelectMany(e => e.Features.Where(x => x != null && x.AssetGuid.ToString() == guid).Select(x => e.Level)).OrderBy(l => l).ToList();
        }

        // 소유자 결정 2026-11-16(명경지수·빠른 이동 제거): 제거가 「빠진 레벨 없이 전부」 인지 실행 시점의 본편 진행표로 확인한다. 어긋나면 던짐(조용히 남기지 않는다).
        //  ① 블루프린트 이름 == 표의 이름 ② 진행표에서 그 특성이 있는 레벨 == 표의 레벨(다른 모드가 레벨을 옮기거나 더했으면 다름) ③ 우리 RemoveFeatures 의 그 특성 레벨 == 표의 레벨
        //  ④ 제거 대상이 주는 특성(MonkFastMovement)은 진행표의 직접 항목이 아니다(직접 항목이면 제거가 반쪽).
        static void VerifyCoreRemovals()
        {
            var rows = LevelPlan.RemovedCoreFeatures;
            if (rows == null || rows.Count == 0) throw new InvalidOperationException("ChimeraMonk: LevelPlan.RemovedCoreFeatures is empty");
            var prog = Get<BlueprintProgression>(Guids.Game.MonkProgression);
            foreach (var row in rows)
            {
                var f = Get<BlueprintFeatureBase>(row.Guid);
                if (f.name != row.Name) throw new InvalidOperationException("ChimeraMonk: core removal blueprint " + row.Guid + " is named '" + f.name + "', expected " + row.Name);
                var live = LevelsOf(prog.LevelEntries, row.Guid);
                if (!live.SequenceEqual(row.Levels))
                    throw new InvalidOperationException("ChimeraMonk: MonkProgression has " + row.Name + " at level(s) {" + string.Join(",", live) + "}, the removal table expects {" + string.Join(",", row.Levels) + "} (monk progression changed — removal would be incomplete)");
                var ours = LevelsOf(Bp.RemoveFeatures, row.Guid);
                if (!ours.SequenceEqual(row.Levels))
                    throw new InvalidOperationException("ChimeraMonk: RemoveFeatures has " + row.Name + " at level(s) {" + string.Join(",", ours) + "}, expected {" + string.Join(",", row.Levels) + "}");
            }
            var granted = LevelPlan.RemovedGrantedFeatures;
            if (granted == null || granted.Count == 0) throw new InvalidOperationException("ChimeraMonk: LevelPlan.RemovedGrantedFeatures is empty");
            foreach (var g in granted)
            {
                var f = Get<BlueprintFeatureBase>(g.Guid);
                if (f.name != g.Name) throw new InvalidOperationException("ChimeraMonk: granted-feature blueprint " + g.Guid + " is named '" + f.name + "', expected " + g.Name);
                var live = LevelsOf(prog.LevelEntries, g.Guid);
                if (live.Count != 0)
                    throw new InvalidOperationException("ChimeraMonk: MonkProgression lists " + g.Name + " directly at level(s) {" + string.Join(",", live) + "} — removing its unlock feature would not remove it");
            }
        }

        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f, int count)
        {
            if (src == null) throw new InvalidOperationException("ChimeraMonk: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
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
                throw new InvalidOperationException("ChimeraMonk: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected " + count + ")");
        }

        internal static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("ChimeraMonk: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
