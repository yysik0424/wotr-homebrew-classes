using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ShadowSorcerer
{
    // 그림자 소서러 아키타입(소서러). 중장법사 src\BulwarkMage\Archetype.cs 의 패턴(복제):
    // Build = 블루프린트 + RemoveFeatures(BodyTable.Removed) + 1레벨 메아리·분신 표시와 구세이브 표시 특성, Register = SorcererClass.m_Archetypes 등록(BuildAll 맨 끝, 한 번만).
    // 레벨표 AddFeatures 는 각 빌더가 AddAt 으로 붙인다(Register 전에만).
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        public static BlueprintFeature EchoFeature;
        public static readonly List<BlueprintFeature> DoublesFeatures = new List<BlueprintFeature>();
        static bool registerCalled;
        public static bool Registered { get; private set; }
        static BlueprintCharacterClass registeredSorcerer;
        internal static BlueprintCharacterClass SorcererClass
        {
            get { return registeredSorcerer ?? Get<BlueprintCharacterClass>(Guids.Game.SorcererClass); }
        }

        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("ShadowSorcerer: Archetype.Build() called twice");
            var sorcerer = Get<BlueprintCharacterClass>(Guids.Game.SorcererClass);
            Bp = Create<BlueprintArchetype>("ShadowSorcererArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("archetype.name");
                a.LocalizedDescription = Strings.L("archetype.desc");
                a.LocalizedDescriptionShort = Strings.L("archetype.desc");
                SetField(a, "m_ParentClass", sorcerer);
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
                a.OverrideAttributeRecommendations = false;
                // 공용 TestUnit.Create 가 첫 값을 능력치 상승 대상으로 쓴다(src\Common\Checks\TestUnit.cs:111-114). 소서러 값 복사(매력).
                a.RecommendedAttributes = sorcerer.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = sorcerer.NotRecommendedAttributes.ToArray();
            });
            if (Bp.RecommendedAttributes.Length == 0 || !Bp.RecommendedAttributes.SequenceEqual(sorcerer.RecommendedAttributes) || Bp.OverrideAttributeRecommendations)
                throw new InvalidOperationException("ShadowSorcerer: archetype recommended-attribute copy readback failed");
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), sorcerer)) throw new InvalidOperationException("ShadowSorcerer: archetype parent class readback is not SorcererClass");

            var removed = BodyTable.Removed;
            if (removed == null || removed.Count == 0) throw new InvalidOperationException("ShadowSorcerer: BodyTable.Removed is empty");
            foreach (var r in removed) RemoveAt(r.level, Get<BlueprintFeatureBase>(r.guid));
            int n = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (n != removed.Count) throw new InvalidOperationException("ShadowSorcerer: RemoveFeatures has " + n + " entries, expected " + removed.Count);

            // 메아리 특성(L1). 관찰 컴포넌트는 T4(ShadowEcho.Build)가 붙인다.
            EchoFeature = Create<BlueprintFeature>("ShadowSorcererEchoFeature", Guids.EchoFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("echo.name"));
                SetField(f, "m_Description", Strings.L("echo.desc"));
                SetField(f, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 없음 = 본편 jbp 의 빈 키(엔진 get_DescriptionShort 가 빈 값이면 설명으로 대체, null 이면 "<null>")
                f.IsClassFeature = true;
            });
            AddAt(ShadowRules.MinLevel, EchoFeature);
            // 분신 표시는 이전 세이브의 GUID와 이름도 등록하며, 신규 레벨 상승에는 현재 단계표만 적용한다.
            string[] guids = { Guids.Doubles1, Guids.Doubles2, Guids.Doubles3 };
            for (int i = 0; i < guids.Length; i++)
            {
                int number = i + 1;
                var d = Create<BlueprintFeature>("ShadowSorcererDoubles" + number, guids[i], f =>
                {
                    SetField(f, "m_DisplayName", Strings.L("doubles." + number));
                    SetField(f, "m_Description", Strings.L("doubles.desc"));
                    SetField(f, "m_DescriptionShort", NoText()); // 2026-11-17: 위 메아리 특성과 같음(빈 키 = 설명으로 대체)
                    f.IsClassFeature = true;
                });
                DoublesFeatures.Add(d);
            }
            foreach (var row in ShadowRules.EchoCountLevels) AddAt(row.level, DoublesFeatures[row.count - 1]);
        }

        // 등록 예외(계획 Global Constraints): SorcererClass.m_Archetypes 에 「원본 원소 + 우리」 새 배열을 1회 대입하고 되읽는다.
        // 이 모드가 본편 블루프린트 객체에 쓰는 유일한 곳(시험 exec: base-blueprint writes 가 고정).
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("ShadowSorcerer: Archetype.Register() before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("ShadowSorcerer: Archetype.Register() called twice");
            registerCalled = true;
            var sorcerer = Get<BlueprintCharacterClass>(Guids.Game.SorcererClass);
            var old = (BlueprintArchetypeReference[])GetF(sorcerer, "m_Archetypes");
            var ours = Ref<BlueprintArchetypeReference>(Guids.Archetype);
            var merged = RegisterRule.Merge(old, ours, r => r.Guid.ToString());
            SetField(sorcerer, "m_Archetypes", merged);
            var back = (BlueprintArchetypeReference[])GetF(sorcerer, "m_Archetypes");
            RegisterRule.VerifyReadback(old, back, ours);
            int seen = sorcerer.Archetypes.Count(x => ReferenceEquals(x, Bp));
            if (seen != 1) throw new InvalidOperationException("ShadowSorcerer: SorcererClass.Archetypes resolves our archetype " + seen + " time(s), expected 1");
            registeredSorcerer = sorcerer;
            Registered = true;
        }

        public static void AddAt(int level, BlueprintFeatureBase f, int count = 1)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (count < 1) throw new ArgumentOutOfRangeException("count", count, "ShadowSorcerer: Archetype.AddAt count must be >= 1");
            if (level < ShadowRules.MinLevel || level > ShadowRules.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "ShadowSorcerer: Archetype.AddAt level must be 1..20");
            if (Bp == null) throw new InvalidOperationException("ShadowSorcerer: Archetype.AddAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("ShadowSorcerer: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f, count);
            Verify(Bp.AddFeatures, level, f, count, "AddAt");
        }

        // 제거 대상은 소서러 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패(다른 모드가 진행표를 바꾼 경우 여기서 드러난다).
        static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var prog = Get<BlueprintProgression>(Guids.Game.SorcererProgression);
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x != null && x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("ShadowSorcerer: " + f.name + " is not in SorcererProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f, 1);
            Verify(Bp.RemoveFeatures, level, f, 1, "RemoveAt");
        }

        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f, int count)
        {
            if (src == null) throw new InvalidOperationException("ShadowSorcerer: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
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
                throw new InvalidOperationException("ShadowSorcerer: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected " + count + ")");
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("ShadowSorcerer: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
