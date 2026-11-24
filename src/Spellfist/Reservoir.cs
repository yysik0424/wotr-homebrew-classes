using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spellfist
{
    // 저수지 특성(설계 §4.4, §11-7, §11-13). 본편 아카니스트 저수지 자원(cac948cb…)을 재사용한다: 자원 정의의 m_MaxAmount 는 BaseValue 3 + 레벨 가산(클래스 [아카니스트·위저드·슬레이어])이라 순수 브롤러는 3 — 이 전제는 CheckResourceShape 가 BaseValue == SpellfistRules.ReservoirBase, IncreasedByStat 거짓, m_Class/m_ClassDiv 에 브롤러 없음을 코드로 검사한다.
    // 최대 = 3 + 브롤러 레벨 + 지능 수정치 (설계 §11-13, 2026-11-16 소유자 결정 — §11-11 의 2배를 되돌림):
    //   IncreaseResourcesByClass.CalculateMaxResourceAmount(IL, 정찰 §4-1) = += BaseValue + Stat.Bonus (IL_0016~IL_0039), += GetClassLevel(m_CharacterClass) (IL_0040~IL_005E).
    //   → 컴포넌트 1개(Stat=Intelligence): +Int +L. 합 = 3 + L + Int. 레벨 항은 컴포넌트 1개당 한 번이므로 컴포넌트 수 = SpellfistRules.ReservoirLevelMultiplier(아래 가드).
    // 아카니스트 저수지 특성(55db1859…)과 휴식 버프는 주지 않는다: 휴식 때 ResourceBuff 가 절반만 채우는 대신, AddAbilityResources(RestoreAmount) 만 있으면 휴식 시 가득 찬다(설계 §11-7).
    public static class Reservoir
    {
        public static BlueprintFeature Feature { get; private set; }
        const int FeatureLevel = 1;

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Spellfist: Reservoir.Build() called twice");
            var resource = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(Guids.Game.ArcanistReservoirResource);
            if (resource == null) throw new InvalidOperationException("Spellfist: arcanist reservoir resource " + Guids.Game.ArcanistReservoirResource + " not in cache");
            var brawler = Archetype.Brawler();
            CheckResourceShape(resource);

            var f = Create<BlueprintFeature>("SpellfistReservoirFeature", Guids.ReservoirFeature, x => Spellbook.Names(x, "Reservoir.Name", "Reservoir.Description"));

            // (1) 자원을 유닛에 추가. 레벨업·휴식 때 최대치로.
            var add = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
            SetField(add, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.Game.ArcanistReservoirResource));
            if (add.Resource != resource || !add.RestoreAmount || !add.RestoreOnLevelUp) throw new InvalidOperationException("Spellfist: Reservoir AddAbilityResources readback failed");

            // (2) 지능 수정치 + 브롤러 레벨
            var incInt = new IncreaseResourcesByClass { Stat = StatType.Intelligence, BaseValue = 0 };
            SetField(incInt, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.Game.ArcanistReservoirResource));
            SetField(incInt, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.BrawlerClass));
            SetField(incInt, "m_Archetype", null);

            if (incInt.Resource != resource || incInt.CharacterClass != brawler || Util.GetF(incInt, "m_Archetype") != null || incInt.Archetype != null || incInt.BaseValue != 0)
                throw new InvalidOperationException("Spellfist: Reservoir IncreaseResourcesByClass resource/class/archetype/BaseValue readback failed");
            if (incInt.Stat != StatType.Intelligence)
                throw new InvalidOperationException("Spellfist: Reservoir IncreaseResourcesByClass Stat readback failed (" + incInt.Stat + "), expected Intelligence");

            // 레벨 가산 컴포넌트 수 = SpellfistRules.ReservoirLevelMultiplier (설계 §11-13, SpellfistRules.cs:22). 상수가 바뀌면 여기서 던진다.
            var levelTerms = new[] { incInt };
            if (levelTerms.Length != SpellfistRules.ReservoirLevelMultiplier)
                throw new InvalidOperationException("Spellfist: Reservoir builds " + levelTerms.Length + " IncreaseResourcesByClass components but SpellfistRules.ReservoirLevelMultiplier = " + SpellfistRules.ReservoirLevelMultiplier);
            AddComponents(f, add, incInt);
            var types = f.ComponentsArray.Select(c => c.GetType()).ToArray();
            var expect = new[] { typeof(AddAbilityResources), typeof(IncreaseResourcesByClass) };
            if (!types.SequenceEqual(expect))
                throw new InvalidOperationException("Spellfist: Reservoir feature components [" + string.Join(", ", types.Select(t => t.Name)) + "] != [" + string.Join(", ", expect.Select(t => t.Name)) + "]");
            if (!ReferenceEquals(f.ComponentsArray[0], add) || !ReferenceEquals(f.ComponentsArray[1], incInt))
                throw new InvalidOperationException("Spellfist: Reservoir feature component order/instances readback failed");
            Feature = f;

            Archetype.AddAt(FeatureLevel, Feature);
            if (Archetype.CountAt(Archetype.Bp.AddFeatures, FeatureLevel, Feature) != 1)
                throw new InvalidOperationException("Spellfist: Reservoir feature count at archetype level 1 readback is not 1");
        }

        // 「3」의 출처 = 자원 정의 자체의 m_MaxAmount.BaseValue(정찰 §4-1). 게임 데이터가 다르거나 브롤러 레벨 항이 이미 자원에 있으면(우리 컴포넌트와 이중 가산) 조용히 틀린 최대치가 되므로 여기서 던진다.
        static void CheckResourceShape(BlueprintAbilityResource resource)
        {
            var amt = Util.GetF(resource, "m_MaxAmount"); // BlueprintAbilityResource.Amount 는 protected 중첩 형식 → 리플렉션으로만 읽는다
            if (amt == null) throw new InvalidOperationException("Spellfist: arcanist reservoir m_MaxAmount is null");
            var baseValue = (int)Util.GetF(amt, "BaseValue");
            if (baseValue != SpellfistRules.ReservoirBase)
                throw new InvalidOperationException("Spellfist: arcanist reservoir m_MaxAmount.BaseValue is " + baseValue + ", SpellfistRules.ReservoirBase = " + SpellfistRules.ReservoirBase);
            if ((bool)Util.GetF(amt, "IncreasedByStat"))
                throw new InvalidOperationException("Spellfist: arcanist reservoir m_MaxAmount.IncreasedByStat is true (stat bonus would be added twice with our Intelligence component)");
             
            var divStep = (bool)Util.GetF(amt, "IncreasedByLevelStartPlusDivStep");
            if (divStep)
                throw new InvalidOperationException("Spellfist: arcanist reservoir m_MaxAmount.IncreasedByLevelStartPlusDivStep is " + divStep + " (expected false: extra level term would change the maximum)");
            var useMax = (bool)Util.GetF(resource, "m_UseMax");
            if (useMax)
                throw new InvalidOperationException("Spellfist: arcanist reservoir m_UseMax is " + useMax + " (expected false: a cap would silently clip 3 + L + Int)");
            var brawlerGuid = BlueprintGuid.Parse(Guids.Game.BrawlerClass);
            CheckNoBrawler(Util.GetF(amt, "m_Class"), "m_Class", brawlerGuid);
            CheckNoBrawler(Util.GetF(amt, "m_ClassDiv"), "m_ClassDiv", brawlerGuid);
        }

        static void CheckNoBrawler(object arr, string field, BlueprintGuid brawlerGuid)
        {
            if (arr == null) return; // 빈 배열 대신 null: 클래스 항 없음(브롤러 없음과 같다 — 레벨 가산할 클래스가 없다)
            var refs = arr as BlueprintCharacterClassReference[];
            if (refs == null)
                throw new InvalidOperationException("Spellfist: arcanist reservoir m_MaxAmount." + field + " is " + arr.GetType().FullName + ", expected BlueprintCharacterClassReference[] (cannot check for the brawler class)");
            if (refs.Any(r => r != null && r.Guid == brawlerGuid))
                throw new InvalidOperationException("Spellfist: arcanist reservoir m_MaxAmount." + field + " already contains the brawler class (level term would be added twice)");
        }
    }
}
