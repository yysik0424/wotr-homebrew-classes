using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using static WotrClassMod.Common.BlueprintUtil;

namespace BlastMagus
{
     
    // 조용한 통과 금지(C1·C2): 원본 없음(Get 이 던짐) · 이름 다름 · AbilitySpawnFx 가 정확히 1개가 아님 · 칸 빔 · AssetId 다름 — 전부 던진다(BuildAll → BuildFailed).
    // 본편 객체는 읽기만 한다(새 컴포넌트·새 PrefabLink 를 만든다 — 시험 base-blueprint writes).
    public static class BlastFx
    {
        static readonly Dictionary<string, string> resolved = new Dictionary<string, string>();

        // 표 무결성을 첫 사용 전에(C2): 문제가 있으면 형식 초기화 실패(TypeInitializationException) — 빌드가 시끄럽게 멈춘다. own = Guids 의 우리 GUID 상수 전부(시험 fx: 와 같은 집합).
        static BlastFx()
        {
            var own = typeof(Guids).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()).ToList();
            var p = FxTable.Problems(own);
            if (p.Count != 0) throw new InvalidOperationException("BlastMagus: FxTable invalid: " + string.Join(" | ", p.Take(8).ToArray()));
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
                var fx = OnlySpawnFx(s, a);
                got = fx.PrefabLink == null ? null : fx.PrefabLink.AssetId;
            }
            else if (s.Slot == FxTable.SlotBuffFxOnStart)
            {
                var b = Get<BlueprintBuff>(s.SourceGuid);
                name = b.name;
                got = b.FxOnStart == null ? null : b.FxOnStart.AssetId;
            }
            else throw new InvalidOperationException("BlastMagus: FX source " + s.Key + " has unknown slot '" + s.Slot + "'");
            if (name != s.SourceName) throw new InvalidOperationException("BlastMagus: FX source " + s.Key + " " + s.SourceGuid + " is '" + name + "', FxTable says '" + s.SourceName + "'");
            if (string.IsNullOrEmpty(got)) throw new InvalidOperationException("BlastMagus: FX source " + s.Key + " (" + name + ") " + s.Slot + " is empty");
            if (got != s.AssetId) throw new InvalidOperationException("BlastMagus: FX source " + s.Key + " (" + name + ") " + s.Slot + " = " + got + ", FxTable says " + s.AssetId);
            resolved.Add(s.Key, got);
            return got;
        }

        static AbilitySpawnFx OnlySpawnFx(FxSource s, BlueprintAbility a)
        {
            var fx = (a.ComponentsArray ?? new BlueprintComponent[0]).OfType<AbilitySpawnFx>().ToArray();
            if (fx.Length != 1) throw new InvalidOperationException("BlastMagus: FX source " + s.Key + " " + s.SourceGuid + " (" + a.name + ") has " + fx.Length + " AbilitySpawnFx components, FxTable expects exactly 1");
            return fx[0];
        }

        // 폭발 능력용 새 AbilitySpawnFx: 본편 원본 컴포넌트의 시점·기준점 칸을 그대로 옮기고 PrefabLink 는 대조된 AssetId 로 새로 만든다.
        public static AbilitySpawnFx NewBlastStrikeSpawnFx()
        {
            var s = FxTable.Get(FxTable.BlastStrikeKey);
            if (s.Slot != FxTable.SlotAbilitySpawnFx) throw new InvalidOperationException("BlastMagus: FxTable Blast Strike source is not an AbilitySpawnFx slot");
            string id = Resolve(s);
            var src = OnlySpawnFx(s, Get<BlueprintAbility>(s.SourceGuid));
            return new AbilitySpawnFx
            {
                PrefabLink = new PrefabLink { AssetId = id },
                Time = src.Time, Anchor = src.Anchor, WeaponTarget = src.WeaponTarget, DestroyOnCast = src.DestroyOnCast, Delay = src.Delay,
                PositionAnchor = src.PositionAnchor, OrientationAnchor = src.OrientationAnchor, OrientationMode = src.OrientationMode,
            };
        }

        // 에너지 주입 자식 버프의 FxOnStart(FX 없는 원소 = null — FxTable.ForEnergize 가 이유 없이는 던진다).
        public static PrefabLink EnergizeFxOnStart(Element e)
        {
            var s = FxTable.ForEnergize(e);
            if (s == null) return null;
            if (s.Slot != FxTable.SlotBuffFxOnStart) throw new InvalidOperationException("BlastMagus: FxTable energize " + e + " source is not a FxOnStart slot");
            return new PrefabLink { AssetId = Resolve(s) };
        }

        public static string[] EnergizeResourceIds(Element e)
        {
            var s = FxTable.ForEnergize(e);
            return s == null ? new string[0] : new[] { Resolve(s) };
        }
    }
}
