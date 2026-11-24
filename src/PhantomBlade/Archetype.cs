using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace PhantomBlade
{
    // 귀검사 아키타입(전사). 무리 조련사 src\PackWarden\Archetype.cs 의 패턴을 그대로 따른다:
    // Build = 블루프린트만, Register = FighterClass.m_Archetypes 등록(BuildAll 맨 끝, 한 번만).
    public static class Archetype
    {
        public static BlueprintArchetype Bp;
        static bool registered;

        // 정찰 §8 (FighterProgression LevelEntries 직독): 용맹 L2·6·10·14·18. 설계 §4.1 교환 대상. 갑옷 숙달 L19 는 StandRules.ArmorMasteryRemovedLevel(핫픽스 2).
        // 갑옷 훈련 L3·7·11·15 는 ArmorTrainingLayout.Levels (핫픽스 1: 무엇을 지울지는 실행 중 진행표 배치로 정한다 — 본편 또는 TabletopTweaks).
        // 그 레벨들은 StandRules.RushTable(Task 6b) 에서 갑옷 숙달 자리를 뺀 것이다 — 러시 특성(StandBlueprints.AddRushFeatures)이 같은 표로 그 자리에 붙는다.
        // 각 쌍은 RemoveAt 이 실행 중 진행표의 그 레벨에 실제로 있는지 되읽어 확인한다(없으면 빌드 실패).
        static readonly int[] BraveryLevels = { 2, 6, 10, 14, 18 };

        // 블루프린트만 만든다. 레벨표의 스탠드 특성은 StandBlueprints.Build 가 AddAt 으로 붙인다.
        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("PhantomBlade: Archetype.Build() called twice");
            var fighter = Get<BlueprintCharacterClass>(GameGuids.FighterClass);
            Bp = Create<BlueprintArchetype>("PhantomBladeArchetype", Guids.Archetype, a =>
            {
                a.LocalizedName = Strings.L("Archetype.Name");
                a.LocalizedDescription = Strings.L("Archetype.Description");
                a.LocalizedDescriptionShort = Strings.L("Archetype.DescriptionShort");
                SetField(a, "m_ParentClass", fighter);
                // 설계 §4.1: 주능력치 매력, 추천 = 매력·힘·건강(제어자 판정: 비추천 = 지능)
                a.OverrideAttributeRecommendations = true;
                a.RecommendedAttributes = new[] { StatType.Charisma, StatType.Strength, StatType.Constitution };
                a.NotRecommendedAttributes = new[] { StatType.Intelligence };
                a.AddFeatures = new LevelEntry[0];
                a.RemoveFeatures = new LevelEntry[0];
            });
            if (!ReferenceEquals(GetF(Bp, "m_ParentClass"), fighter)) throw new InvalidOperationException("PhantomBlade: archetype parent class readback is not FighterClass");

            var bravery = Get<BlueprintFeatureBase>(Guids.Game.Bravery);
            foreach (int lv in BraveryLevels) RemoveAt(lv, bravery);
             
             
            RemoveAt(StandRules.ArmorMasteryRemovedLevel, Get<BlueprintFeatureBase>(Guids.Game.ArmorMastery));
            const int armorMasteryRemoved = 1;

            // 핫픽스 1: 갑옷 훈련 계열은 실행 중 진행표에서 배치를 판정해 실제로 있는 것을 지운다(모르는 배치면 Classify 가 던진다).
            var prog = Get<BlueprintProgression>(Guids.Game.FighterProgression);
            var live = new Dictionary<int, IList<string>>();
            foreach (int lv in ArmorTrainingLayout.Levels)
            {
                var entry = prog.LevelEntries.FirstOrDefault(e => e.Level == lv);
                live[lv] = entry == null ? null : entry.Features.Select(x =>
                {
                    if (x == null) throw new InvalidOperationException("PhantomBlade: FighterProgression level " + lv + " has an unresolved (null) feature reference");
                    return x.AssetGuid.ToString();
                }).ToList();
            }
            var layout = ArmorTrainingLayout.Classify(live);
            int armorRemoved = 0;
            foreach (var kv in layout.Remove)
                foreach (var guid in kv.Value)
                {
                    var entry = prog.LevelEntries.First(e => e.Level == kv.Key);
                    RemoveAt(kv.Key, entry.Features.Single(x => x.AssetGuid.ToString() == guid));
                    armorRemoved++;
                }
            ModContext.Log("PhantomBlade " + ArmorTrainingLayout.Describe(layout));

            int removed = Bp.RemoveFeatures.Sum(e => e.Features.Count);
            if (removed != BraveryLevels.Length + armorMasteryRemoved + armorRemoved)
                throw new InvalidOperationException("PhantomBlade: RemoveFeatures has " + removed + " entries, expected " + (BraveryLevels.Length + armorMasteryRemoved + armorRemoved));
        }

        // FighterClass.m_Archetypes 에 등록(새 배열). Blueprints.BuildAll 이 모든 빌드가 성공한 뒤 마지막에 한 번만 부른다.
        public static void Register()
        {
            if (Bp == null) throw new InvalidOperationException("PhantomBlade: Archetype.Register() before Archetype.Build()");
            if (registered) throw new InvalidOperationException("PhantomBlade: Archetype.Register() called twice");
            var fighter = Get<BlueprintCharacterClass>(GameGuids.FighterClass);
            // 최종 리뷰 M5: 본편 전사는 아키타입이 여럿이다(blueprints.zip FighterClass m_Archetypes). null 이면 클래스가 깨진 것 → 빈 목록으로 덮지 않고 던진다.
            var old = (BlueprintArchetypeReference[])GetF(fighter, "m_Archetypes");
            if (old == null) throw new InvalidOperationException("PhantomBlade: FighterClass.m_Archetypes is null (broken fighter class?)");
            if (old.Any(r => r != null && r.Get() == Bp)) throw new InvalidOperationException("PhantomBlade: archetype already in FighterClass.m_Archetypes");
            SetField(fighter, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Guids.Archetype) }).ToArray());
            if (!fighter.Archetypes.Contains(Bp)) throw new InvalidOperationException("PhantomBlade: FighterClass.Archetypes readback does not contain the archetype");
            registered = true;
        }

        public static void AddAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (Bp == null) throw new InvalidOperationException("PhantomBlade: Archetype.AddAt before Archetype.Build()");
            if (registered) throw new InvalidOperationException("PhantomBlade: Archetype.AddAt after Register()");
            Bp.AddFeatures = WithEntry(Bp.AddFeatures, level, f);
            Verify(Bp.AddFeatures, level, f, "AddAt");
        }

        // 제거 대상은 전사 진행표의 그 레벨에 실제로 있어야 한다. 아니면 조용히 남는 대신 실패.
        static void RemoveAt(int level, BlueprintFeatureBase f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var prog = Get<BlueprintProgression>(Guids.Game.FighterProgression);
            var entry = prog.LevelEntries.FirstOrDefault(e => e.Level == level);
            if (entry == null || !entry.Features.Any(x => x.AssetGuid == f.AssetGuid))
                throw new InvalidOperationException("PhantomBlade: " + f.name + " is not in FighterProgression level " + level + " (nothing to remove)");
            Bp.RemoveFeatures = WithEntry(Bp.RemoveFeatures, level, f);
            Verify(Bp.RemoveFeatures, level, f, "RemoveAt");
        }

        // 항상 새 배열·새 LevelEntry (원본 배열을 직접 바꾸지 않음). LevelEntry.SetFeatures 는 m_Features 를 쓰고 캐시를 비운다(IL 확인 2026-11-06, PackWarden 동일).
        static LevelEntry[] WithEntry(LevelEntry[] src, int level, BlueprintFeatureBase f)
        {
            // 최종 리뷰 M5: src 는 Build 가 new LevelEntry[0] 으로 채운 Bp.AddFeatures/RemoveFeatures 다 → null 이면 배선 오류.
            if (src == null) throw new InvalidOperationException("PhantomBlade: Archetype level entry array is null (Build() did not initialise AddFeatures/RemoveFeatures)");
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
                throw new InvalidOperationException("PhantomBlade: " + what + " readback failed: " + f.name + " level " + level + " count=" + n + " (expected 1)");
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("PhantomBlade: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
