using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
     
    // 조용한 통과 금지(C1·C2): 원본 없음(Get 이 던짐) · 이름 다름 · AbilitySpawnFx 가 정확히 1개가 아님 · 칸이 비었음 · AssetId 가 표와 다름 — 전부 던진다(Rebuke.Build → BuildFailed).
    // 본편 객체는 읽기만 한다 — 새 PrefabLink 를 만든다(본편 PrefabLink 인스턴스를 공유하지 않는다).
    public static class RebukeFx
    {
        static readonly Dictionary<string, string> resolved = new Dictionary<string, string>();

        // 표 무결성을 첫 사용 전에(C2): 문제가 있으면 형식 초기화 실패(TypeInitializationException) — Rebuke.Build 가 시끄럽게 멈춘다. own = Guids 의 우리 GUID 상수 전부(시험 fx: 와 같은 집합).
        static RebukeFx()
        {
            var own = typeof(Guids).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()).ToList();
            var p = FxTable.Problems(RebukeTable.Effects.Select(e => e.Id), own);
            if (p.Count != 0) throw new InvalidOperationException("BulwarkMage: FxTable invalid: " + string.Join(" | ", p.Take(8).ToArray()));
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
                var fx = (a.ComponentsArray ?? new Kingmaker.Blueprints.BlueprintComponent[0]).OfType<AbilitySpawnFx>().ToArray();
                if (fx.Length != 1) throw new InvalidOperationException("BulwarkMage: FX source " + s.Key + " " + s.SourceGuid + " (" + a.name + ") has " + fx.Length + " AbilitySpawnFx components, FxTable expects exactly 1");
                got = fx[0].PrefabLink == null ? null : fx[0].PrefabLink.AssetId;
            }
            else if (s.Slot == FxTable.SlotBuffFxOnStart)
            {
                var b = Get<BlueprintBuff>(s.SourceGuid);
                name = b.name;
                got = b.FxOnStart == null ? null : b.FxOnStart.AssetId;
            }
            else throw new InvalidOperationException("BulwarkMage: FX source " + s.Key + " has unknown slot '" + s.Slot + "'");
            if (name != s.SourceName) throw new InvalidOperationException("BulwarkMage: FX source " + s.Key + " " + s.SourceGuid + " is '" + name + "', FxTable says '" + s.SourceName + "'");
            if (string.IsNullOrEmpty(got)) throw new InvalidOperationException("BulwarkMage: FX source " + s.Key + " (" + name + ") " + s.Slot + " is empty");
            if (got != s.AssetId) throw new InvalidOperationException("BulwarkMage: FX source " + s.Key + " (" + name + ") " + s.Slot + " = " + got + ", FxTable says " + s.AssetId);
            resolved.Add(s.Key, got);
            return got;
        }

        // 효과의 FX 행동(FX 없는 효과 = null — FxTable.ForRebuke 가 NoFx 이유 없이는 던진다).
        public static ContextActionSpawnFx SpawnAction(string effectId)
        {
            var s = FxTable.ForRebuke(effectId);
            if (s == null) return null;
            var a = new ContextActionSpawnFx { PrefabLink = new PrefabLink { AssetId = Resolve(s) } };
            Rebuke.FillEmpty(a);
            return a;
        }

        // 준비됨 버프의 ResourceAssetIds(본편은 미리 읽기용으로 FX id 를 적는다 — HasteBuff 등 blueprints.zip). FX 없는 효과 = 빈 배열.
        public static string[] ResourceIds(string effectId)
        {
            var s = FxTable.ForRebuke(effectId);
            return s == null ? new string[0] : new[] { Resolve(s) };
        }
    }
}
