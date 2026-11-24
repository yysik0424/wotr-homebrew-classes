using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Stats;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spellfist
{
    // 아키타입 본체(브롤러 모드 클래스). PhantomBlade\Archetype.cs 패턴: Build = 블루프린트만, Register = 브롤러 m_Archetypes 등록(BuildAll 맨 끝, 한 번만).
     
    public static class Archetype
    {
        public static BlueprintArchetype Bp { get; private set; }
        static bool registered;

        public static void Build()
        {
            // 무변조 기록(S9)은 우리 쓰기보다 먼저 — 빌드 경로의 첫 호출(두 번째 Build 면 Integrity.Capture 가 먼저 던진다).
            Integrity.Capture();
            if (Bp != null) throw new InvalidOperationException("Spellfist: Archetype.Build() called twice");
            var brawler = Brawler();
            Bp = Create<BlueprintArchetype>("SpellfistArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("Archetype.Name");
                a.LocalizedDescription = Strings.L("Archetype.Description");
                a.LocalizedDescriptionShort = Strings.L("Archetype.DescriptionShort");
                SetField(a, "m_ParentClass", brawler);
                // 주문서 교체(설계 §4.3). Spellbook.Build 가 뒤에 만들므로 GUID 참조만 둔다 — 대상 해석 되읽기는 Spellbook.Build·Register.
                SetField(a, "m_ReplaceSpellbook", Ref<BlueprintSpellbookReference>(Guids.Spellbook));
                a.RemoveSpellbook = false;
                // 설계 §3: 비시전 클래스 → 비전 시전자(본편 선례 EldritchScoundrelArchetype jbp: ChangeCasterType=true, IsArcaneCaster=true, IsDivineCaster=false).
                a.ChangeCasterType = true;
                a.IsArcaneCaster = true;
                a.IsDivineCaster = false;
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
                // 추천 능력치(캐릭터 생성 화면 표시 + 공용 TestUnit.BuildLevels 의 종족·레벨 능력치 대상 = 1순위). 기본값은 빈 배열이라 공용 TestUnit 이 던진다
                // (TestUnit.cs BuildLevels "archetype has no recommended attributes"). 지능 시전 + 맨손 근접(힘·민첩) — 블라스트 메이거스 Archetype.cs:47 과 같은 배치.
                a.RecommendedAttributes = new[] { StatType.Intelligence, StatType.Strength, StatType.Dexterity };
            });
            if (!ReferenceEquals(Util.GetF(Bp, "m_ParentClass"), brawler)) throw new InvalidOperationException("Spellfist: archetype m_ParentClass readback is not the brawler class");
            // 핫픽스 1(2026-11-08): GUID 만 되읽는다. IsEmpty() 금지 — BlueprintReferenceBase.IsEmpty 는 deserializedGuid==Empty 이거나
            // GetBlueprint()==null 이면 true(Assembly-CSharp IL_0013 GetBlueprint → op_Implicit → ceq 0)라, 뒤의 Spellbook.Build 가 만드는 주문서는 여기서 언제나 「비었다」.
             
            CheckForwardRefGuid(Util.GetF(Bp, "m_ReplaceSpellbook"), "m_ReplaceSpellbook", Guids.Spellbook);
            if (!Bp.ChangeCasterType || !Bp.IsArcaneCaster || Bp.IsDivineCaster || Bp.RemoveSpellbook) throw new InvalidOperationException("Spellfist: archetype caster-type flags readback failed");
            if (Bp.RecommendedAttributes == null || Bp.RecommendedAttributes.Length == 0 || Bp.RecommendedAttributes[0] != StatType.Intelligence)
                throw new InvalidOperationException("Spellfist: archetype RecommendedAttributes readback: first entry is not Intelligence");

            // 교환표(설계 §4.7, 정찰 §8-1). 각 쌍은 브롤러 진행표의 그 레벨에 실제로 있어야 한다.
            var prog = Mod<BlueprintProgression>(Guids.Game.BrawlerProgression);
            foreach (var r in SpellfistRules.RemovedFeatures) RemoveAt(prog, r.level, Mod<BlueprintFeatureBase>(r.guid));
            int removed = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (removed != SpellfistRules.RemovedFeatures.Length)
                throw new InvalidOperationException("Spellfist: RemoveFeatures has " + removed + " entries, expected " + SpellfistRules.RemovedFeatures.Length);

            // 병행 금지(정찰 §8-2). PrerequisiteNoArchetype.CheckInternal 은 ClassData 가 없으면 true(IL @0x0013 brfalse → @0x002A ldc.i4.1) → 1레벨 선택 화면에서도 안전.
            var prereqs = new List<PrerequisiteNoArchetype>();
            foreach (var g in SpellfistRules.BrawlerModArchetypes)
            {
                var p = new PrerequisiteNoArchetype();
                SetField(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.BrawlerClass));
                SetField(p, "m_Archetype", Ref<BlueprintArchetypeReference>(g));
                // 해석 안 되는 참조는 Contains(null)=false → 전제가 조용히 항상 통과한다. 지금 해석해 확인.
                if (!ReferenceEquals(p.CharacterClass, brawler) || p.Archetype == null || p.Archetype.AssetGuid != BlueprintGuid.Parse(g))
                    throw new InvalidOperationException("Spellfist: PrerequisiteNoArchetype readback failed for brawler archetype " + g);
                prereqs.Add(p);
            }
            AddComponents(Bp, prereqs.ToArray());
            var back = Bp.ComponentsArray.OfType<PrerequisiteNoArchetype>().Select(p => p.Archetype.AssetGuid).ToList();
            if (back.Count != SpellfistRules.BrawlerModArchetypes.Length || !new HashSet<BlueprintGuid>(back).SetEquals(SpellfistRules.BrawlerModArchetypes.Select(BlueprintGuid.Parse)))
                throw new InvalidOperationException("Spellfist: PrerequisiteNoArchetype readback: " + back.Count + " components, expected the " + SpellfistRules.BrawlerModArchetypes.Length + " brawler mod archetypes");
        }

        // 아직 만들지 않은 블루프린트를 가리키는 참조의 GUID 되읽기(핫픽스 1). 세 상태: 일치 → 통과 / 불일치 → 실제 GUID 를 담아 throw /
        // 읽을 수 없음(null·형식 다름·빈 GUID·기대값 파싱 실패) → throw. 참조를 해석하지 않는다(IsEmpty·GetBlueprint·Get 금지):
        // 대상이 아직 캐시에 없어 해석은 언제나 null 이다. Guid 게터 = ldfld deserializedGuid(Assembly-CSharp get_Guid IL_0001), Ref<T> 가 ReadGuidFromGuid 로 채우는 필드.
        internal static void CheckForwardRefGuid(object raw, string field, string expectedGuid)
        {
            if (raw == null) throw new InvalidOperationException("Spellfist: archetype " + field + " readback: field is null (cannot read the reference)");
            var r = raw as BlueprintReferenceBase;
            if (r == null) throw new InvalidOperationException("Spellfist: archetype " + field + " readback: value is " + raw.GetType().FullName + ", not a BlueprintReferenceBase");
            // BlueprintGuid.Parse 는 형식이 틀리면 던지지 않고 Empty 를 준다(Assembly-CSharp Parse IL_0011 TryParse 실패 → 로그 + IL_0032 default Guid).
            var expected = BlueprintGuid.Parse(expectedGuid);
            if (expected == BlueprintGuid.Empty) throw new InvalidOperationException("Spellfist: archetype " + field + " readback: expected GUID '" + expectedGuid + "' parses to Empty");
            var got = r.Guid;
            if (got == BlueprintGuid.Empty) throw new InvalidOperationException("Spellfist: archetype " + field + " readback: reference GUID is empty (deserializedGuid unset), expected " + expectedGuid);
            if (got != expected) throw new InvalidOperationException("Spellfist: archetype " + field + " readback: GUID is " + got + ", expected " + expectedGuid);
        }

        // 브롤러 m_Archetypes 에 등록(새 배열). Blueprints.BuildAll 이 모든 빌드가 성공한 뒤 마지막에 한 번만 부른다.
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("Spellfist: Archetype.Register() before Archetype.Build()");
            if (registered) throw new InvalidOperationException("Spellfist: Archetype.Register() called twice");
            if (Spellbook.Bp == null || Bp.ReplaceSpellbook != Spellbook.Bp) throw new InvalidOperationException("Spellfist: Register: archetype ReplaceSpellbook does not resolve to Spellbook.Bp");
            var brawler = Brawler();
            var old = (BlueprintArchetypeReference[])Util.GetF(brawler, "m_Archetypes");
            if (old == null) throw new InvalidOperationException("Spellfist: brawler m_Archetypes is null (broken brawler class?)");
            if (old.Any(r => r != null && r.Guid == Bp.AssetGuid)) throw new InvalidOperationException("Spellfist: archetype already in brawler m_Archetypes");
            // PrerequisiteNoArchetype 는 이 8종만 막는다 → 다른 아키타입이 더 있으면(다른 모드) 조용히 병행 가능해지므로 실패.
            var oldSet = old.Select(r => r == null ? default(BlueprintGuid) : r.Guid).ToList();
            if (oldSet.Count != SpellfistRules.BrawlerModArchetypes.Length || !new HashSet<BlueprintGuid>(oldSet).SetEquals(SpellfistRules.BrawlerModArchetypes.Select(BlueprintGuid.Parse)))
                throw new InvalidOperationException("Spellfist: brawler m_Archetypes = [" + string.Join(",", oldSet) + "], expected exactly the 8 BrawlerModArchetypes (PrerequisiteNoArchetype covers only those)");
            SetField(brawler, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Guids.Archetype) }).ToArray());
            if (!brawler.Archetypes.Contains(Bp) || brawler.Archetypes.Count() != old.Length + 1)
                throw new InvalidOperationException("Spellfist: brawler Archetypes readback does not contain the archetype (or wrong count)");
            registered = true;
        }

        // 같은 특성을 같은 레벨에 여러 번 둘 수 있다(학파 선택 ×3, 정찰 §2) → 되읽기는 「이전 개수 + 1」.
        public static void AddAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (Bp == null) throw new InvalidOperationException("Spellfist: Archetype.AddAt before Archetype.Build()");
            if (registered) throw new InvalidOperationException("Spellfist: Archetype.AddAt after Register()");
            int before = CountAt(Bp.AddFeatures, level, f);
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f);
            int after = CountAt(Bp.AddFeatures, level, f);
            if (after != before + 1) throw new InvalidOperationException("Spellfist: AddAt readback failed: " + f.name + " level " + level + " count " + before + " -> " + after);
        }

        public static int CountAt(LevelEntry[] entries, int level, BlueprintFeatureBase f)
        {
            if (entries == null) throw new InvalidOperationException("Spellfist: Archetype level entry array is null");
            var entry = entries.FirstOrDefault(e => e.Level == level);
            return entry == null ? 0 : entry.Features.Count(x => x.AssetGuid == f.AssetGuid);
        }

        static void RemoveAt(BlueprintProgression prog, int level, BlueprintFeatureBase f)
        {
            var entry = prog.LevelEntries.FirstOrDefault(e => e.Level == level);
            if (entry == null || !entry.Features.Any(x => x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("Spellfist: " + f.name + " is not in brawler progression " + Guids.Game.BrawlerProgression + " level " + level + " (nothing to remove)");
            int before = CountAt(Bp.RemoveFeatures, level, f);
            if (before != 0) throw new InvalidOperationException("Spellfist: RemoveAt " + f.name + " level " + level + " twice");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f);
            if (CountAt(Bp.RemoveFeatures, level, f) != 1) throw new InvalidOperationException("Spellfist: RemoveAt readback failed: " + f.name + " level " + level);
        }

        // 항상 새 배열·새 LevelEntry. LevelEntry.SetFeatures 는 m_Features 를 쓰고 캐시를 비운다(PhantomBlade 와 같은 근거).
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f)
        {
            if (src == null) throw new InvalidOperationException("Spellfist: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
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

        // 브롤러 클래스: BuildAll 이 이미 null 을 걸렀지만 여기서도 null 이면 던진다(호출 순서가 바뀌어도 조용히 진행하지 않음).
        internal static BlueprintCharacterClass Brawler()
        {
            var c = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Guids.Game.BrawlerClass);
            if (c == null) throw new InvalidOperationException("Spellfist: brawler class " + Guids.Game.BrawlerClass + " not in cache");
            return c;
        }

        // 브롤러 모드 블루프린트(런타임 생성 — 캐시에만 있다).
        internal static T Mod<T>(string guid) where T : BlueprintScriptableObject
        {
            var bp = ResourcesLibrary.TryGetBlueprint<T>(guid);
            if (bp == null) throw new InvalidOperationException("Spellfist: brawler mod blueprint " + guid + " (" + typeof(T).Name + ") not in cache");
            return bp;
        }
    }
}
