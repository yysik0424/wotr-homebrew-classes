using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Warlock
{
    // 워락 아키타입(위치). 블라스트 메이거스 src\BlastMagus\Archetype.cs:35-84 패턴 + WSS 등록(Archetype.cs:63-78, 판단은 순수 RegisterRule):
     
    // Register = WitchClass.m_Archetypes 등록(BuildAll 맨 끝, 한 번만 — 본편 쓰기 예외 ①). 레벨표의 나머지 AddFeatures 는 이후 Task 의 빌더가 AddAt 으로 붙인다.
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registerCalled;
        // 되읽기 단언까지 끝난 등록만 true(점검 러너 BlueprintsBuilt 가 읽는다).
        public static bool Registered { get; private set; }

        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("Warlock: Archetype.Build() called twice");
            var witch = Get<BlueprintCharacterClass>(Guids.Game.WitchClass);
            var spellbook = Get<BlueprintSpellbook>(Guids.Game.AccursedWitchSpellbook);
            Bp = Create<BlueprintArchetype>("WarlockArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("Archetype.Name");
                a.LocalizedDescription = Strings.L("Archetype.Description");
                a.LocalizedDescriptionShort = Strings.L("Archetype.DescriptionShort");
                SetField(a, "m_ParentClass", witch);
                // 설계 §4.2: 스티그마 위치 주문서(AccursedWitchSpellbook b897fe09…) 본편 블루프린트 직접 재사용. 시전자 형식은 위치 그대로(ChangeCasterType=false — 스티그마 jbp 와 같음).
                SetField(a, "m_ReplaceSpellbook", Ref<BlueprintSpellbookReference>(Guids.Game.AccursedWitchSpellbook));
                a.RemoveSpellbook = false;
                a.ChangeCasterType = false;
                // 설계 §4.1: 추천 능력치 매력(스티그마 위치 jbp 와 같은 값). 공용 TestUnit.BuildLevels 도 이 1순위를 능력치 상승 대상으로 쓴다.
                a.OverrideAttributeRecommendations = true;
                a.RecommendedAttributes = new[] { StatType.Charisma };
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
            });
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), witch)) throw new InvalidOperationException("Warlock: archetype parent class readback is not WitchClass");
            if (!ReferenceEquals(Bp.ReplaceSpellbook, spellbook) || Bp.RemoveSpellbook || Bp.ChangeCasterType)
                throw new InvalidOperationException("Warlock: archetype spellbook readback: ReplaceSpellbook is " + (Bp.ReplaceSpellbook == null ? "null" : Bp.ReplaceSpellbook.name) + ", expected AccursedWitchSpellbook (RemoveSpellbook/ChangeCasterType false)");
            if (!Bp.OverrideAttributeRecommendations || Bp.RecommendedAttributes == null || !Bp.RecommendedAttributes.SequenceEqual(new[] { StatType.Charisma }))
                throw new InvalidOperationException("Warlock: archetype recommended attributes readback is not [Charisma]");

            // 제거 13쌍(설계 §4.1 교환표 = RemoveRule.Pairs, C4 ①). 각 쌍은 위치 진행표의 그 레벨에 실제로 있어야 한다(RemoveAt 되읽기).
            foreach (var p in RemoveRule.Pairs) RemoveAt(p.Item1, Get<BlueprintFeatureBase>(p.Item2));
             
            var added = ApplyL20Plan(RemoveRule.PlanL20(ReadL20(), ResolveHexNode), "build");
            int removed = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (removed != RemoveRule.Pairs.Length + added.Length)
                throw new InvalidOperationException("Warlock: RemoveFeatures has " + removed + " entries, expected " + RemoveRule.Pairs.Length + " pairs + " + added.Length + " L20 extension(s)");

            // 겹침 UI 층(설계 §4.10, 정찰 §0-15): 스티그마 위치를 가진 위치에게 워락을 회색으로. 실제 차단은 엔진 CanAddArchetype(§0-5).
            // PrerequisiteNoArchetype.CheckInternal 은 ClassData 가 없으면 true(Spellfist Archetype.cs:61-62 와 같은 IL) → 1레벨 선택 화면에서도 안전.
            var accursed = Get<BlueprintArchetype>(Guids.Game.AccursedWitchArchetype);
            var pre = new PrerequisiteNoArchetype();
            SetField(pre, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.WitchClass));
            SetField(pre, "m_Archetype", Ref<BlueprintArchetypeReference>(Guids.Game.AccursedWitchArchetype));
            // 해석 안 되는 참조는 Contains(null)=false → 전제가 조용히 항상 통과한다. 지금 해석해 확인(Spellfist 선례).
            if (!ReferenceEquals(pre.CharacterClass, witch) || !ReferenceEquals(pre.Archetype, accursed))
                throw new InvalidOperationException("Warlock: PrerequisiteNoArchetype{WitchClass, AccursedWitchArchetype} readback failed");
            AddComponents(Bp, pre);
            var back = Bp.ComponentsArray.OfType<PrerequisiteNoArchetype>().ToArray();
            if (back.Length != 1 || !ReferenceEquals(back[0].Archetype, accursed) || Bp.ComponentsArray.Length != 1)
                throw new InvalidOperationException("Warlock: archetype components readback: expected exactly 1 PrerequisiteNoArchetype(Accursed), got " + Bp.ComponentsArray.Length + " component(s)");

            // 캔트립 교체(설계 §4.1·§4.2): 매력 캔트립 = 본편 AccursedWitchCantripsFeature 재사용. 이 AddAt 은 여기 한 곳(시험 `wiring: cantrip AddAt only in Archetype.Build`).
            AddAt(1, Get<BlueprintFeatureBase>(Guids.Game.AccursedWitchCantripsFeature));
        }

        // 등록 예외 ①(계획 Global Constraints): WitchClass.m_Archetypes 에 「원본 원소 + 우리」 새 배열을 1회 대입하고 되읽는다. 본편 객체 쓰기 허용 목록의 1번(시험 exec: base-blueprint writes).
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("Warlock: Archetype.Register() before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("Warlock: Archetype.Register() called twice");
            registerCalled = true;
            var witch = Get<BlueprintCharacterClass>(Guids.Game.WitchClass);
            var old = (BlueprintArchetypeReference[])GetF(witch, "m_Archetypes");
            var ours = Ref<BlueprintArchetypeReference>(Guids.Archetype);
            var merged = RegisterRule.Merge(old, ours, r => r.Guid.ToString());
            SetField(witch, "m_Archetypes", merged);
            var back = (BlueprintArchetypeReference[])GetF(witch, "m_Archetypes");
            RegisterRule.VerifyReadback(old, back, ours);
            int seen = witch.Archetypes.Count(x => ReferenceEquals(x, Bp));
            if (seen != 1) throw new InvalidOperationException("Warlock: WitchClass.Archetypes resolves our archetype " + seen + " time(s), expected 1");
            Registered = true;
        }

        public static void AddAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (Bp == null) throw new InvalidOperationException("Warlock: Archetype.AddAt before Archetype.Build()");
            if (registerCalled) throw new InvalidOperationException("Warlock: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f);
            Verify(Bp.AddFeatures, level, f, "AddAt");
        }

        // 제거 대상은 위치 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패(TTT 등이 진행표를 바꾼 경우 여기서 드러난다).
        // internal: L20Sync(첫 프레임 재검)가 등록 뒤에도 L20 항목을 더한다(우리 블루프린트만 바꾼다).
        internal static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (Bp == null) throw new InvalidOperationException("Warlock: Archetype.RemoveAt before Archetype.Build()");
            var prog = Get<BlueprintProgression>(Guids.Game.WitchProgression);
            var atLevel = prog.LevelEntries.Where(e => e.Level == level).SelectMany(e => e.Features).ToList();
            if (!atLevel.Any(x => x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("Warlock: " + f.name + " is not in WitchProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f);
            Verify(Bp.RemoveFeatures, level, f, "RemoveAt");
        }

        // 라이브 WitchProgression L20 항목 전수(TTT 가 m_Features 에 더한 것 포함, 정찰 §7) → 순수 RemoveRule.L20Item. 종류·자식은 HexNodeOf 와 같은 규칙.
        // 해석 안 되는 항목(null)은 판정 불가 → 던진다(빠진 항목을 「모르는 것 없음」으로 접지 않는다).
        internal static RemoveRule.L20Item[] ReadL20()
        {
            var prog = Get<BlueprintProgression>(Guids.Game.WitchProgression);
            var items = new List<RemoveRule.L20Item>();
            foreach (var f in prog.LevelEntries.Where(e => e.Level == 20).SelectMany(e => e.Features))
            {
                if (f == null) throw new InvalidOperationException("Warlock: WitchProgression level 20 has an unresolved feature reference");
                var n = HexNodeOf(f);
                items.Add(new RemoveRule.L20Item(f.AssetGuid.ToString(), f.GetType().FullName, n.Kind, n.Children));
            }
            return items.ToArray();
        }

        // 라이브 블루프린트 → 순수 주술 운반 노드(RemoveRule.CarriesHex 의 재료 — PlanL20 과 S1/S2 신탁이 같은 것을 쓴다).
        // 선택지 = AllFeatures · 진행표 = LevelEntries 전 레벨 Features · 그 밖 = 자식 없음. 해석 안 되는 자식(null) = 판정 불가 → 던진다.
        internal static RemoveRule.HexNode HexNodeOf(BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var sel = f as BlueprintFeatureSelection;
            if (sel != null)
            {
                var feats = sel.AllFeatures;
                if (feats.Any(x => x == null)) throw new InvalidOperationException("Warlock: selection " + sel.name + " has an unresolved AllFeatures entry");
                return new RemoveRule.HexNode(f.AssetGuid.ToString(), RemoveRule.HexNodeKind.Selection, feats.Select(x => x.AssetGuid.ToString()).ToArray());
            }
            var prog = f as BlueprintProgression;
            if (prog != null)
            {
                if (prog.LevelEntries == null) throw new InvalidOperationException("Warlock: progression " + prog.name + " has null LevelEntries");
                var feats = prog.LevelEntries.SelectMany(e => e.Features).ToArray();
                if (feats.Any(x => x == null)) throw new InvalidOperationException("Warlock: progression " + prog.name + " has an unresolved LevelEntries feature");
                return new RemoveRule.HexNode(f.AssetGuid.ToString(), RemoveRule.HexNodeKind.Progression, feats.Select(x => x.AssetGuid.ToString()).ToArray());
            }
            return new RemoveRule.HexNode(f.AssetGuid.ToString(), RemoveRule.HexNodeKind.Feature, null);
        }

        // CarriesHex 의 resolve: GUID → 라이브 블루프린트(Get 은 없으면 던진다) → HexNodeOf.
        internal static RemoveRule.HexNode ResolveHexNode(string guid)
        {
            return HexNodeOf(Get<BlueprintFeatureBase>(guid));
        }

        // 계획의 Remove 중 아직 없는 것만 RemoveAt(20, …)(순수 RemoveRule.NewL20Removals), Unknown·Contradicted 는 ERROR 목록. 반환 = 새로 제거한 GUID.
        // Kept(알려진 다른 모드 항목, 보존)는 여기서 쓰지 않는다 — L20Sync.SyncNow 의 동기화 줄이 부팅마다 한 번 INFO 로 적는다.
        // Build(빌드 시점)와 L20Sync.SyncNow(첫 프레임 — 다른 모드의 Init 포스트픽스 뒤)가 같은 절차를 쓴다.
        internal static string[] ApplyL20Plan(RemoveRule.L20Plan plan, string when)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (Bp == null) throw new InvalidOperationException("Warlock: ApplyL20Plan before Archetype.Build()");
            var already = Bp.RemoveFeatures.Where(e => e.Level == 20).SelectMany(e => e.Features).Select(x => x.AssetGuid.ToString()).ToArray();
            var add = RemoveRule.NewL20Removals(already, plan);
            foreach (var g in add) RemoveAt(20, Get<BlueprintFeatureBase>(g));
            if (plan.Unknown.Length > 0)
                ModContext.LogError("Warlock: WitchProgression level 20 has unknown entries (" + when + "): [" + string.Join(", ", plan.Unknown) + "] — not removed; check other mods' witch progression changes");
            if (plan.Contradicted.Length > 0)
                ModContext.LogError("Warlock: known foreign L20 entries now carry WitchHexSelection (" + when + "): [" + string.Join(", ", plan.Contradicted) + "] — removed as hex carriers; RemoveRule.L20ForeignRules is stale for this mod version");
            return add;
        }

        // 항상 새 배열·새 LevelEntry (원본 배열을 직접 바꾸지 않음). LevelEntry.SetFeatures 는 m_Features 를 쓰고 캐시를 비운다(블라스트 메이거스와 같음).
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f)
        {
            if (src == null) throw new InvalidOperationException("Warlock: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
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
                throw new InvalidOperationException("Warlock: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected 1)");
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("Warlock: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
