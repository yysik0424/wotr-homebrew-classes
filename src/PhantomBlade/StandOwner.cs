using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace PhantomBlade
{
    // 스탠드 컴포넌트(흡수·원거리 방어·귀신참)가 소유자에게서 읽는 값. 결측은 전부 throw(C1/C2): 조용히 0 레벨·0 풀로 판정하지 않는다.
    // 레벨 = 귀검사 아키타입이 확인된 전사 레벨(ContextActionStandStrike.RunAction 과 같은 방식).
    internal static class StandOwner
    {
        static BlueprintCharacterClass fighter;

        internal static int Level(UnitEntityData unit, string who, int minLevel)
        {
            if (unit == null) throw new InvalidOperationException("PhantomBlade: " + who + " has no owner unit");
            if (Archetype.Bp == null) throw new InvalidOperationException("PhantomBlade: " + who + " before Archetype.Build()");
            if (!unit.Descriptor.Progression.IsArchetype(Archetype.Bp)) throw new InvalidOperationException("PhantomBlade: " + who + " owner " + unit.CharacterName + " is not a Phantom Blade");
            if (fighter == null) fighter = Get<BlueprintCharacterClass>(GameGuids.FighterClass); // 없으면 Get 이 던진다
            int raw = unit.Descriptor.Progression.GetClassLevel(fighter);
            // 표 레벨(StandRules.TableLevel, Task 6c fix 1): 20 초과(치트 등)는 20 행. 범위 밖이면 거기서 던진다. 바뀌었으면 유닛마다 한 번 알린다.
            int level = StandRules.TableLevel(raw);
            if (level != raw) WarnLegendOnce(unit, raw, level);
            if (level < minLevel) throw new InvalidOperationException("PhantomBlade: " + who + " owner " + unit.CharacterName + " is Phantom Blade level " + level + ", below " + minLevel);
            return level;
        }

        // 20 초과 전사 레벨 알림: 유닛마다 한 번(매 틱 부르는 행동 막대 경로에서도 로그가 쏟아지지 않게). 정보 줄이다(오류 아님 — 동작은 정의된 20 행).
        // 키 = 유닛 객체(WeakKeyTable — 세이브·유닛 수명에 묶이지 않는다). 다시 불러오면 새 객체라 한 번 더 알린다.
        static readonly WeakKeyTable<UnitEntityData, object> legendWarned = new WeakKeyTable<UnitEntityData, object>();

        static void WarnLegendOnce(UnitEntityData unit, int raw, int level)
        {
            object seen;
            if (legendWarned.TryGetValue(unit, out seen)) return; // 정당한 무동작: 이미 알렸다
            legendWarned.Add(unit, new object());
            ModContext.Log("PhantomBlade: NOTICE " + unit.CharacterName + " is Phantom Blade (fighter) level " + raw + " > " + StandRules.TableMaxLevel + "; stand tables use level " + level + " (rush, ghost slash, guard)");
        }

         
        // 「자원 자체가 없음」(리스펙·잘못된 사실·세이브 불일치)이 「정상적으로 다 씀」으로 보인다. 그래서 비용 계산(GhostSlashCost)이 먼저 확인해 던진다.
        // 이 경로는 행동 막대가 매 틱 부를 수 있다 → 원인 오류는 유닛마다 한 번만 기록하고, 던짐은 매번(정상 부족으로 위장하지 않음).
        // 2026-11-16(설계 §14 ③): 귀신참의 자원이 흡수 풀에서 전용 횟수(GhostSlashResource)로 바뀌어, 확인 대상도 그 자원이다(종전 RequirePool).
        static readonly WeakKeyTable<UnitEntityData, object> usesMissingLogged = new WeakKeyTable<UnitEntityData, object>();

        internal static void RequireGhostSlashUses(UnitEntityData unit, string who)
        {
            if (unit == null) throw new InvalidOperationException("PhantomBlade: " + who + " has no owner unit");
            try
            {
                GhostSlashUses(unit, who);
            }
            catch (InvalidOperationException e)
            {
                object seen;
                if (!usesMissingLogged.TryGetValue(unit, out seen))
                {
                    usesMissingLogged.Add(unit, new object());
                    ModContext.LogError("PhantomBlade: " + e.Message + " — the ability is unusable because its per-day uses resource is MISSING, not because the uses are spent (logged once per unit)");
                }
                throw;
            }
        }

        // 귀신참 전용 하루 횟수 자원(5레벨 특성 GhostSlash5 의 AddAbilityResources 가 준다). 없으면 던진다 — 위 주석.
        internal static BlueprintAbilityResource GhostSlashUses(UnitEntityData unit, string who)
        {
            var res = StandBlueprints.GhostSlashResource;
            if (res == null) throw new InvalidOperationException("PhantomBlade: " + who + " before StandBlueprints.Build() (GhostSlashResource is null)");
            if (!unit.Resources.ContainsResource(res)) throw new InvalidOperationException("PhantomBlade: " + who + " owner " + unit.CharacterName + " has no " + res.name + " (the level-" + GhostSlash.FirstLevel + " ghost slash feature's AddAbilityResources missing?)");
            return res;
        }

        // 흡수 풀 자원. Resources.Spend/Restore 는 자원이 없으면 PFLog 오류만 남기고 돌아간다(Spend IL 0020, Restore IL 0057) → 먼저 확인해 던진다.
        internal static BlueprintAbilityResource Pool(UnitEntityData unit, string who)
        {
            var res = StandBlueprints.PoolResource;
            if (res == null) throw new InvalidOperationException("PhantomBlade: " + who + " before StandBlueprints.Build() (PoolResource is null)");
            if (!unit.Resources.ContainsResource(res)) throw new InvalidOperationException("PhantomBlade: " + who + " owner " + unit.CharacterName + " has no " + res.name + " (StandFeature's AddAbilityResources missing?)");
            return res;
        }
    }
}
