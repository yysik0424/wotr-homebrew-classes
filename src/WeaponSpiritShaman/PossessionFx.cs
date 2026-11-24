using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
     
    // 조용한 통과 금지(C1·C2): 원본 없음(Get 이 던짐) · 이름 다름 · FxOnStart 빈칸 · AssetId 가 표와 다름 · 표 무결성 · 대상 누락(Verify) — 전부 던진다(Build → BuildFailed).
    // 본편 객체는 읽기만 한다 — 새 PrefabLink 를 만든다(본편 PrefabLink 인스턴스를 공유하지 않는다).
    public static class PossessionFx
    {
        static string resolved;

        public static string Resolve()
        {
            if (resolved != null) return resolved;
            var b = Get<BlueprintBuff>(FxTable.SourceGuid);
            if (b.name != FxTable.SourceName) throw new InvalidOperationException("WeaponSpiritShaman: FX source " + FxTable.SourceGuid + " is '" + b.name + "', FxTable says '" + FxTable.SourceName + "'");
            string got = b.FxOnStart == null ? null : b.FxOnStart.AssetId;
            if (string.IsNullOrEmpty(got))
                throw new InvalidOperationException("WeaponSpiritShaman: FX source " + b.name + " " + FxTable.Slot + " is empty — the approved fallback is " + FxTable.FallbackName + " " + FxTable.FallbackGuid + " (switch FxTable by hand)");
            if (got != FxTable.AssetId) throw new InvalidOperationException("WeaponSpiritShaman: FX source " + b.name + " " + FxTable.Slot + " = " + got + ", FxTable says " + FxTable.AssetId);
            resolved = got;
            return got;
        }

        // 버프: FxOnStart(켜져 있는 동안 유지) + ResourceAssetIds(미리 읽기). 대상이 표의 버프 행이어야 한다.
        public static void ApplyBuff(BlueprintBuff b)
        {
            if (b == null) throw new ArgumentNullException("b");
            var t = FxTable.For(b.AssetGuid.ToString());
            if (t.Kind != FxTable.KindBuff) throw new InvalidOperationException("WeaponSpiritShaman: " + b.name + " is FxTable kind " + t.Kind + ", not " + FxTable.KindBuff);
            var id = Resolve();
            b.FxOnStart = new PrefabLink { AssetId = id };
            // 2026-11-16 결함(카오스 워리어에서 발견, 같은 원인): 엔진 Buff.OnRemove IL_00A2~00B7 은 FxOnStart 로 입자를 띄운 버프를 지울 때
            // Blueprint.FxOnRemove.Load 를 null 검사 없이 부른다 → 코드로 만든 버프의 null FxOnRemove 면 NRE. 본편 jbp 는 읽을 때 빈 링크가 된다 — 빈 링크를 직접 넣는다.
            if (b.FxOnRemove == null) b.FxOnRemove = new PrefabLink();
            b.ResourceAssetIds = new[] { id };
        }

        // 능력: ResourceAssetIds 만(보이는 FX 는 능력이 켜는 버프가 낸다).
        public static void ApplyAbility(BlueprintAbility a)
        {
            if (a == null) throw new ArgumentNullException("a");
            var t = FxTable.For(a.AssetGuid.ToString());
            if (t.Kind != FxTable.KindAbility) throw new InvalidOperationException("WeaponSpiritShaman: " + a.name + " is FxTable kind " + t.Kind + ", not " + FxTable.KindAbility);
            a.ResourceAssetIds = new[] { Resolve() };
        }

        // 되읽기: 표의 대상마다 실제 블루프린트 값 == 표(빠진 대상·덮어쓴 값 = 차이). Fusion.Build 끝(대상 전부가 만들어진 뒤)에서 부른다.
        public static List<string> Diff()
        {
            var d = new List<string>();
            var id = Resolve();
            foreach (var t in FxTable.Targets)
            {
                var bp = Get<BlueprintScriptableObject>(t.Guid);
                string[] res; // ResourceAssetIds 는 형식마다 따로 선언된 필드(BlueprintBuff · BlueprintAbility — dnlib 2026-11-15)
                if (t.Kind == FxTable.KindBuff)
                {
                    var b = bp as BlueprintBuff;
                    if (b == null) { d.Add(bp.name + " is not a BlueprintBuff"); continue; }
                    if (b.FxOnStart == null || b.FxOnStart.AssetId != id) d.Add(bp.name + " FxOnStart " + (b.FxOnStart == null ? "null" : b.FxOnStart.AssetId) + " != " + id);
                    if (b.FxOnRemove == null) d.Add(bp.name + " FxOnRemove is null (engine Buff.OnRemove would NRE on removal, 2026-11-16 defect)");
                    res = b.ResourceAssetIds;
                }
                else
                {
                    var a = bp as BlueprintAbility;
                    if (a == null) { d.Add(bp.name + " is not a BlueprintAbility"); continue; }
                    res = a.ResourceAssetIds;
                }
                if (res == null || !res.SequenceEqual(new[] { id })) d.Add(bp.name + " ResourceAssetIds [" + (res == null ? "null" : string.Join(",", res)) + "] != [" + id + "]");
            }
            return d;
        }

        // 표 무결성(C2) + 되읽기. own = 지금까지 만든 우리 블루프린트 전부(Fusion.Build 끝 = 대상 6 이 다 있다). Type.GetFields 는 쓰지 않는다(CloneGraph 전용 — 시험 fix wave field order).
        public static void Verify()
        {
            var p = FxTable.Problems(OwnBlueprints.Select(b => b.AssetGuid.ToString()));
            if (p.Count != 0) throw new InvalidOperationException("WeaponSpiritShaman: FxTable invalid: " + string.Join(" | ", p.Take(8).ToArray()));
            var d = Diff();
            if (d.Count != 0) throw new InvalidOperationException("WeaponSpiritShaman: possession FX readback: " + string.Join(" | ", d.ToArray()));
        }
    }
}
