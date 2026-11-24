using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
     
    // 조용한 통과 금지(C1·C2): 원본 없음(Get 이 던짐) · 이름 다름 · 칸 부품이 정확히 1개가 아님 · 칸이 비었음 · AssetId 가 표와 다름 — 전부 던진다(Rage.Build / Mutations.Build → BuildFailed).
    // 본편 객체는 읽기만 한다 — 새 PrefabLink 를 만든다(본편 PrefabLink 인스턴스를 공유하지 않는다). 선례 src\BulwarkMage\RebukeFx.cs.
    public static class ChaosFx
    {
        static readonly Dictionary<string, string> resolved = new Dictionary<string, string>();

        // 표 무결성을 첫 사용 전에(C2): 문제가 있으면 형식 초기화 실패(TypeInitializationException) — 빌드가 시끄럽게 멈춘다. own = Guids 의 우리 GUID 상수 전부(시험 fx: 와 같은 집합).
        static ChaosFx()
        {
            var own = typeof(Guids).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()).ToList();
            var p = FxTable.Problems(FxTable.ExpectedTargets(), own);
            if (p.Count != 0) throw new InvalidOperationException("ChaosWarrior: FxTable invalid: " + string.Join(" | ", p.Take(8).ToArray()));
        }

        public static string Resolve(FxSource s)
        {
            if (s == null) throw new ArgumentNullException("s");
            string id;
            if (resolved.TryGetValue(s.Key, out id)) return id;
            string got, name;
            if (s.Slot == FxTable.SlotAbilitySpawnFx)
            {
                var a = Get<BlueprintAbility>(s.SourceGuid);
                name = a.name;
                var fx = (a.ComponentsArray ?? new BlueprintComponent[0]).OfType<AbilitySpawnFx>().ToArray();
                if (fx.Length != 1) throw new InvalidOperationException("ChaosWarrior: FX source " + s.Key + " " + s.SourceGuid + " (" + a.name + ") has " + fx.Length + " AbilitySpawnFx components, FxTable expects exactly 1");
                got = fx[0].PrefabLink == null ? null : fx[0].PrefabLink.AssetId;
            }
            else if (s.Slot == FxTable.SlotBuffFxOnStart)
            {
                var b = Get<BlueprintBuff>(s.SourceGuid);
                name = b.name;
                got = b.FxOnStart == null ? null : b.FxOnStart.AssetId;
            }
            else if (s.Slot == FxTable.SlotBuffActivatedSpawnFx)
            {
                var b = Get<BlueprintBuff>(s.SourceGuid);
                name = b.name;
                var spawns = (b.ComponentsArray ?? new BlueprintComponent[0]).OfType<AddFactContextActions>()
                    .SelectMany(c => c.Activated == null || c.Activated.Actions == null ? new GameAction[0] : c.Activated.Actions).OfType<ContextActionSpawnFx>().ToArray();
                if (spawns.Length != 1) throw new InvalidOperationException("ChaosWarrior: FX source " + s.Key + " " + s.SourceGuid + " (" + b.name + ") has " + spawns.Length + " Activated ContextActionSpawnFx, FxTable expects exactly 1");
                got = spawns[0].PrefabLink == null ? null : spawns[0].PrefabLink.AssetId;
            }
            else throw new InvalidOperationException("ChaosWarrior: FX source " + s.Key + " has unknown slot '" + s.Slot + "'");
            if (name != s.SourceName) throw new InvalidOperationException("ChaosWarrior: FX source " + s.Key + " " + s.SourceGuid + " is '" + name + "', FxTable says '" + s.SourceName + "'");
            if (string.IsNullOrEmpty(got)) throw new InvalidOperationException("ChaosWarrior: FX source " + s.Key + " (" + name + ") " + s.Slot + " is empty");
            if (got != s.AssetId) throw new InvalidOperationException("ChaosWarrior: FX source " + s.Key + " (" + name + ") " + s.Slot + " = " + got + ", FxTable says " + s.AssetId);
            resolved.Add(s.Key, got);
            return got;
        }

        // 대상의 1회 FX 행동(켜질 때 버프 소유자 위 — ContextActionSpawnFx 는 문맥 대상 = 버프 소유자에 뿌린다). 없으면 빈 목록.
        public static List<GameAction> OneShotActions(string target)
        {
            var l = new List<GameAction>();
            foreach (var s in FxTable.OneShotsOf(target))
                l.Add(BuildKit.Act(new ContextActionSpawnFx { PrefabLink = new PrefabLink { AssetId = Resolve(s) } }));
            return l;
        }

        // 지속 FX(FxOnStart) + 미리 읽기 목록(ResourceAssetIds = 대상의 FX id 전부). 지속 원본이 없으면 FxOnStart 는 건드리지 않는다(빈 칸 그대로).
        public static void ApplyPersistent(BlueprintBuff b, string target)
        {
            if (b == null) throw new ArgumentNullException("b");
            var p = FxTable.PersistentOf(target);
            if (p != null) b.FxOnStart = new PrefabLink { AssetId = Resolve(p) };
            if (b.FxOnRemove == null) b.FxOnRemove = new PrefabLink(); // 엔진이 제거 때 null 검사 없이 읽는다(BuildKit.Buff 주석, 2026-11-16 결함)
            foreach (var s in FxTable.ForTarget(target)) Resolve(s);   // 1회 원본도 본편 칸 대조(ResourceAssetIds 에 적기 전에)
            b.ResourceAssetIds = FxTable.ResourceIds(target);
        }

        // 1회 FX 전용 부품(Activated = SpawnFx 들, 나머지 비어 있음). 1회 FX 가 없으면 null.
        public static BlueprintComponent OneShotComponent(string target)
        {
            var acts = OneShotActions(target);
            if (acts.Count == 0) return null;
            var none = new List<GameAction>();
            return BuildKit.Comp(new AddFactContextActions { Activated = BuildKit.Actions(acts), Deactivated = BuildKit.Actions(none), NewRound = BuildKit.Actions(none), Dispose = BuildKit.Actions(none) });
        }

        // OneShotComponent 모양(= FX 전용 부품) — 효과 행 대조에서 뺀다. 빠진 것은 Verify 가 표와 대조하므로 조용히 사라지지 않는다.
        public static bool IsFxComponent(BlueprintComponent c)
        {
            var fa = c as AddFactContextActions;
            if (fa == null || fa.Activated == null || fa.Activated.Actions == null || fa.Activated.Actions.Length == 0) return false;
            if (!fa.Activated.Actions.All(a => a is ContextActionSpawnFx)) return false;
            return Empty(fa.Deactivated) && Empty(fa.NewRound) && Empty(fa.Dispose);
        }

        static bool Empty(ActionList l) { return l == null || l.Actions == null || l.Actions.Length == 0; }

        // 되읽기(빌드 시 + 점검): 우리 버프의 FxOnStart · ResourceAssetIds · Activated 의 SpawnFx 가 표와 같은가. 빈 목록 = 일치.
        public static List<string> Verify(string target, BlueprintBuff b)
        {
            var d = new List<string>();
            if (b == null) { d.Add(target + ": FX target buff is null"); return d; }
            var p = FxTable.PersistentOf(target);
            string fo = b.FxOnStart == null || string.IsNullOrEmpty(b.FxOnStart.AssetId) ? "" : b.FxOnStart.AssetId;
            if (fo != (p == null ? "" : p.AssetId)) d.Add(target + " FxOnStart '" + fo + "' != FxTable '" + (p == null ? "" : p.AssetId) + "'");
            if (b.FxOnRemove == null) d.Add(target + " FxOnRemove is null (engine Buff.OnRemove dereferences it when FxOnStart spawned a particle — NRE, 2026-11-16 defect)");
            var want = FxTable.ResourceIds(target);
            if (b.ResourceAssetIds == null || !b.ResourceAssetIds.SequenceEqual(want)) d.Add(target + " ResourceAssetIds [" + (b.ResourceAssetIds == null ? "null" : string.Join(",", b.ResourceAssetIds)) + "] != FxTable [" + string.Join(",", want) + "]");
            var spawns = (b.ComponentsArray ?? new BlueprintComponent[0]).OfType<AddFactContextActions>()
                .SelectMany(c => c.Activated == null || c.Activated.Actions == null ? new GameAction[0] : c.Activated.Actions).OfType<ContextActionSpawnFx>()
                .Select(a => a.PrefabLink == null ? "null" : a.PrefabLink.AssetId ?? "null").ToList();
            var wantSpawns = FxTable.OneShotsOf(target).Select(s => s.AssetId).ToList();
            if (!spawns.SequenceEqual(wantSpawns)) d.Add(target + " Activated SpawnFx [" + string.Join(",", spawns) + "] != FxTable [" + string.Join(",", wantSpawns) + "]");
            if ((b.ComponentsArray ?? new BlueprintComponent[0]).Count(IsFxComponent) > 1) d.Add(target + " has more than one FX-only component");
            return d;
        }
    }
}
