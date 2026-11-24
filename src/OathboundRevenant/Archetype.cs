using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
    // 맹세의 망자 아키타입(팔라딘). Bulwark src\BulwarkMage\Archetype.cs 의 패턴:
    // Build = 블루프린트 + 레벨표(T1 = 1레벨 언데드 몸, T3 이 LevelPlan 순회로 바꾼다), Register = PaladinClass.m_Archetypes 등록(BuildAll 맨 끝, 1회 — 판단은 순수 RegisterRule).
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registerCalled;
        // 되읽기 단언까지 끝난 등록만 true(점검 러너 BlueprintsBuilt 가 읽는다).
        public static bool Registered { get; private set; }

        // 추천 능력치는 본편 그대로 — 공용 TestUnit.Create 가 RecommendedAttributes 첫 값을 시험 유닛의 능력치 상승 대상으로 쓴다(TestUnit.cs:111-114, 비면 던짐).
        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("OathboundRevenant: Archetype.Build() called twice");
            if (UndeadBody.Feature == null) throw new InvalidOperationException("OathboundRevenant: Archetype.Build() before UndeadBody.Build()");
            var paladin = Get<BlueprintCharacterClass>(Guids.Game.PaladinClass);
            Bp = Create<BlueprintArchetype>("OathboundRevenantArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("archetype.name");
                a.LocalizedDescription = Strings.L("archetype.desc");
                a.LocalizedDescriptionShort = Strings.L("archetype.short");
                SetField(a, "m_ParentClass", paladin);
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
                a.OverrideAttributeRecommendations = false;
                a.RecommendedAttributes = paladin.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = paladin.NotRecommendedAttributes.ToArray();
            });
            if (Bp.RecommendedAttributes.Length == 0 || !Bp.RecommendedAttributes.SequenceEqual(paladin.RecommendedAttributes) || Bp.OverrideAttributeRecommendations)
                throw new InvalidOperationException("OathboundRevenant: archetype recommended-attribute copy readback failed");
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), paladin)) throw new InvalidOperationException("OathboundRevenant: archetype parent class readback is not PaladinClass");

            // §8-e 주문 시전 제거 — 본편 WarriorOfTheHolyLightArchetype 플래그 그대로(ArchetypeFlags, 부록 A-o).
            Bp.RemoveSpellbook = ArchetypeFlags.RemoveSpellbook;
            Bp.ChangeCasterType = ArchetypeFlags.ChangeCasterType;
            Bp.IsArcaneCaster = ArchetypeFlags.IsArcaneCaster;
            Bp.IsDivineCaster = ArchetypeFlags.IsDivineCaster;
            if (ArchetypeFlags.ReplaceSpellbookGuid != null) throw new InvalidOperationException("OathboundRevenant: ArchetypeFlags.ReplaceSpellbookGuid must be null (no replacement spellbook)");
            if (Bp.RemoveSpellbook != ArchetypeFlags.RemoveSpellbook || Bp.ChangeCasterType != ArchetypeFlags.ChangeCasterType || Bp.IsArcaneCaster != ArchetypeFlags.IsArcaneCaster || Bp.IsDivineCaster != ArchetypeFlags.IsDivineCaster || GetF(Bp, "m_ReplaceSpellbook") != null)
                throw new InvalidOperationException("OathboundRevenant: archetype spellcasting flags readback differs from ArchetypeFlags");

             
            foreach (var r in LevelPlan.Removals) RemoveAt(r.Level, r.Guid);
            foreach (var r in LevelPlan.DistinctAdditionKeys())
            {
                if (r.Key == "UndeadBody") { shells[r.Key] = UndeadBody.Feature; filled.Add(r.Key); continue; }
                shells[r.Key] = r.IsSelection ? (BlueprintFeature)MakeShell<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(r) : MakeShell<BlueprintFeature>(r);
            }
            foreach (var r in LevelPlan.Additions) AddAt(r.Level, Shell(r.Key));
            VerifyLevelTable();
        }

        // ---- 레벨 표 추가 특성 껍데기 ----
        static readonly Dictionary<string, BlueprintFeature> shells = new Dictionary<string, BlueprintFeature>();
        static readonly HashSet<string> filled = new HashSet<string>();

        static T MakeShell<T>(LevelRow r) where T : BlueprintFeature, new()
        {
            string k = r.Key.ToLowerInvariant();
            return Create<T>("OathboundRevenant" + r.Key, r.Guid, f =>
            {
                SetField(f, "m_DisplayName", Strings.L(k + ".name"));
                SetField(f, "m_Description", Strings.L(k + ".desc"));
                Tooltips.NoShortDescription(f);   // 2026-11-16: LocalizedString 필드 null 금지(Tooltips 머리말)
                f.IsClassFeature = true;
            });
        }

        public static BlueprintFeature Shell(string key)
        {
            BlueprintFeature f;
            if (!shells.TryGetValue(key, out f)) throw new InvalidOperationException("OathboundRevenant: level-table feature " + key + " was not created (Archetype.Build not run, or key not in LevelPlan.Additions)");
            return f;
        }

        // 채우는 빌더가 본문을 붙인 뒤 부른다(같은 키 두 번 = 던짐).
        public static void MarkFilled(string key)
        {
            Shell(key);
            if (registerCalled) throw new InvalidOperationException("OathboundRevenant: MarkFilled(" + key + ") after Register()");
            if (!filled.Add(key)) throw new InvalidOperationException("OathboundRevenant: level-table feature " + key + " filled twice");
        }

        public static bool IsFilled(string key) { return filled.Contains(key); }

        // 빌드 되읽기: RemoveFeatures == LevelPlan.Removals, AddFeatures == LevelPlan.Additions(레벨·GUID 쌍, 순서 무관 다중집합).
        static void VerifyLevelTable()
        {
            Func<IEnumerable<LevelEntry>, List<string>> pairs = es => es.SelectMany(e => e.Features.Select(f => e.Level + ":" + (f == null ? "null" : f.AssetGuid.ToString()))).OrderBy(x => x, StringComparer.Ordinal).ToList();
            var rem = pairs(Bp.RemoveFeatures);
            var wantRem = LevelPlan.Removals.Select(r => r.Level + ":" + r.Guid).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (!rem.SequenceEqual(wantRem)) throw new InvalidOperationException("OathboundRevenant: RemoveFeatures [" + string.Join(",", rem) + "] != LevelPlan.Removals [" + string.Join(",", wantRem) + "]");
            var add = pairs(Bp.AddFeatures);
            var wantAdd = LevelPlan.Additions.Select(r => r.Level + ":" + r.Guid).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (!add.SequenceEqual(wantAdd)) throw new InvalidOperationException("OathboundRevenant: AddFeatures [" + string.Join(",", add) + "] != LevelPlan.Additions [" + string.Join(",", wantAdd) + "]");
        }

        // Register 전 되읽기: 추가 행마다 「채워짐」 ⇔ FilledBy ∈ ImplementedTasks(껍데기가 조용히 남거나, 계획보다 앞서 채워진 것 = 던짐).
        static void VerifyShells()
        {
            var wrong = LevelPlan.DistinctAdditionKeys().Where(r => LevelPlan.IsImplemented(r) != filled.Contains(r.Key)).Select(r => r.Key + "(" + r.FilledBy + ", filled=" + filled.Contains(r.Key) + ")").ToList();
            if (wrong.Count > 0) throw new InvalidOperationException("OathboundRevenant: level-table shells out of plan: " + string.Join(", ", wrong) + " (implemented tasks: " + string.Join(",", LevelPlan.ImplementedTasks) + ")");
        }

        // 등록 예외(계획 Global Constraints): PaladinClass.m_Archetypes 에 「원본 원소 + 우리」 새 배열을 1회 대입하고 되읽는다. 본편 블루프린트에 쓰는 유일한 곳.
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("OathboundRevenant: Archetype.Register() before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("OathboundRevenant: Archetype.Register() called twice");
            VerifyShells();
            Talents.VerifyFilled();
            registerCalled = true;
            var paladin = Get<BlueprintCharacterClass>(Guids.Game.PaladinClass);
            var old = (BlueprintArchetypeReference[])GetF(paladin, "m_Archetypes");
            var ours = Ref<BlueprintArchetypeReference>(Guids.Archetype);
            var merged = RegisterRule.Merge(old, ours, r => r.Guid.ToString());
            SetField(paladin, "m_Archetypes", merged);
            var back = (BlueprintArchetypeReference[])GetF(paladin, "m_Archetypes");
            RegisterRule.VerifyReadback(old, back, ours);
            int seen = paladin.Archetypes.Count(x => ReferenceEquals(x, Bp));
            if (seen != 1) throw new InvalidOperationException("OathboundRevenant: PaladinClass.Archetypes resolves our archetype " + seen + " time(s), expected 1");
            Registered = true;
        }

        // level 에 f 를 count 번 둔다. 되읽기 = 그 레벨의 f 개수가 정확히 count.
        public static void AddAt(int level, BlueprintFeatureBase f, int count = 1)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (count < 1) throw new ArgumentOutOfRangeException("count", count, "OathboundRevenant: Archetype.AddAt count must be >= 1");
            if (level < RevenantRules.Levels.MinLevel || level > RevenantRules.Levels.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "OathboundRevenant: Archetype.AddAt level must be 1..20");
            if (Bp == null) throw new InvalidOperationException("OathboundRevenant: Archetype.AddAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("OathboundRevenant: Archetype.AddAt after Register()");
            int before = CountAt(Bp.AddFeatures, level, f);
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f, count);
            Verify(Bp.AddFeatures, level, f, before + count, "AddAt");
        }

        // 제거 대상은 팔라딘 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패(다른 모드가 진행표를 바꾼 경우 여기서 드러난다).
        public static void RemoveAt(int level, string featureGuid)
        {
            if (string.IsNullOrEmpty(featureGuid)) throw new ArgumentException("OathboundRevenant: RemoveAt without a feature GUID");
            if (Bp == null) throw new InvalidOperationException("OathboundRevenant: Archetype.RemoveAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("OathboundRevenant: Archetype.RemoveAt after Register()");
            var f = Get<BlueprintFeatureBase>(featureGuid);
            var prog = Get<BlueprintProgression>(Guids.Game.PaladinProgression);
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x != null && x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("OathboundRevenant: " + f.name + " is not in PaladinProgression level " + level + " (nothing to remove)");
            int before = CountAt(Bp.RemoveFeatures, level, f);
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f, 1);
            Verify(Bp.RemoveFeatures, level, f, before + 1, "RemoveAt");
        }

        static int CountAt(LevelEntry[] entries, int level, BlueprintFeatureBase f)
        {
            var entry = entries.FirstOrDefault(e => e.Level == level);
            return entry == null ? 0 : entry.Features.Count(x => x != null && x.AssetGuid == f.AssetGuid);
        }

        // 항상 새 배열·새 LevelEntry. LevelEntry.SetFeatures 는 m_Features 를 쓰고 캐시를 비운다.
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f, int count)
        {
            if (src == null) throw new InvalidOperationException("OathboundRevenant: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
            var list = src.ToList();
            int idx = list.FindIndex(e => e.Level == level);
            var features = new List<BlueprintFeatureBase>();
            if (idx >= 0) features.AddRange(list[idx].Features);
            for (int i = 0; i < count; i++) features.Add(f);
            var ne = new LevelEntry { Level = level };
            ne.SetFeatures(features);
            if (idx >= 0) list[idx] = ne; else list.Add(ne);
            return list.OrderBy(e => e.Level).ToArray();
        }

        static void Verify(LevelEntry[] entries, int level, BlueprintFeatureBase f, int count, string what)
        {
            int n = CountAt(entries, level, f);
            if (n != count)
                throw new InvalidOperationException("OathboundRevenant: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected " + count + ")");
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("OathboundRevenant: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
