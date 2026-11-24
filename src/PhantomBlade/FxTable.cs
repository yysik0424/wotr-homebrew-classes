using System;
using System.Collections.Generic;
using System.Linq;

namespace PhantomBlade
{
     
    // 단일 원장(C4): 게임 쪽 Fx.Link 가 이 표대로 본편 원본 블루프린트의 슬롯을 읽어 (1) 원본 없음 · (2) 슬롯 비어 있음 · (3) 값이 ExpectedAssetId 와 다름 ·
    //   (4) 번들 위치 목록에 없음 이면 던진다(빌드 실패). 같은 값으로 새 PrefabLink 를 만들어 우리 블루프린트에 넣고, ResourceAssetIds(미리 읽기 목록)에도 적는다.
    // 시험 `fx:` 가 이 표를 Guids 와 대조하고 blueprints.zip 의 원본 jbp(AssetId·슬롯 값)를 직독한다.
    // FX 는 표시용이다 — 게임 규칙 값은 건드리지 않는다. 화면에 어떻게 보이는지는 오프라인으로 확인 못 한다(눈 점검).
    public enum FxSlot { BuffFxOnStart }

    public sealed class FxRow
    {
        public readonly string Key, TargetGuid, SourceGuid, SourceName, JbpPath, ExpectedAssetId, Reason;
        public readonly FxSlot TargetSlot, SourceSlot;
        public FxRow(string key, string targetGuid, FxSlot targetSlot, string sourceGuid, string sourceName, string jbpPath, FxSlot sourceSlot, string expectedAssetId, string reason)
        {
            Key = key; TargetGuid = targetGuid; TargetSlot = targetSlot; SourceGuid = sourceGuid; SourceName = sourceName; JbpPath = jbpPath; SourceSlot = sourceSlot; ExpectedAssetId = expectedAssetId; Reason = reason;
        }
    }

    public static class FxTable
    {
        // 값 = blueprints.zip(2026-11-15 직독) Spells/Level3/HasteBuff.jbp: AssetId 03464790…, FxOnStart.AssetId 91ef30ab…(본편 Haste.jbp·HasteBuff.jbp 의 ResourceAssetIds 에도 이 값).
        public const string FullAttackHasteKey = "FullAttackHaste";
        public static readonly IReadOnlyList<FxRow> Rows = new[]
        {
            new FxRow(FullAttackHasteKey, Guids.FullAttackBuff, FxSlot.BuffFxOnStart,
                "03464790f40c3c24aa684b57155f3280", "HasteBuff", "Spells/Level3/HasteBuff.jbp", FxSlot.BuffFxOnStart, "91ef30ab58fa0d3449d4d2ccc20cb0f8",
                "스탠드 전력 공격 1라운드 버프 — 가속 버프의 몸 FX(2026-11-15 승인)"),
        };

        // 미리 읽기(ResourceAssetIds): 우리 블루프린트 → 정확히 이 FX 들. 본편 선례 = Haste.jbp(능력)·HasteBuff.jbp(버프) 둘 다 버프 FX 를 적는다.
        public static readonly IReadOnlyDictionary<string, string[]> Preload = new Dictionary<string, string[]>
        {
            [Guids.FullAttackBuff] = new[] { FullAttackHasteKey },
            [Guids.FullAttackAbility] = new[] { FullAttackHasteKey },
        };

         
        public static readonly IReadOnlyList<string> NoFx = new[] { Guids.StandBuff };

        public static FxRow Get(string key)
        {
            var hit = Rows.Where(r => r.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("PhantomBlade: FX row '" + key + "' is in FxTable " + hit.Length + " time(s)");
            return hit[0];
        }

        // 블루프린트가 미리 읽어야 할 FX AssetId(표 순서). 표에 없으면 던진다(조용히 빈 목록 금지 — 미리 읽기 없는 블루프린트는 이 함수를 부르지 않는다).
        public static string[] PreloadIds(string ourGuid)
        {
            string[] keys;
            if (!Preload.TryGetValue(ourGuid, out keys)) throw new KeyNotFoundException("PhantomBlade: no FX preload entry for " + ourGuid);
            return keys.Select(k => Get(k).ExpectedAssetId).ToArray();
        }

        static bool Hex32(string s) { return s != null && s.Length == 32 && s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')); }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리 블루프린트 GUID 전체(시험: Guids 상수 전부).
        public static List<string> Problems(IEnumerable<string> own)
        {
            var p = new List<string>();
            if (own == null) { p.Add("no own GUID set"); return p; }
            var ownSet = new HashSet<string>(own);
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            if (Rows.Count == 0) p.Add("FxTable has no rows");
            if (Rows.Select(r => r.Key).Distinct().Count() != Rows.Count) p.Add("duplicate FX key");
            if (Rows.Select(r => r.TargetGuid + "/" + r.TargetSlot).Distinct().Count() != Rows.Count) p.Add("one target slot is filled twice");
            foreach (var r in Rows)
            {
                if (!Hex32(r.SourceGuid)) p.Add(r.Key + " source GUID '" + r.SourceGuid + "' is not 32 lowercase hex");
                if (!Hex32(r.ExpectedAssetId)) p.Add(r.Key + " asset id '" + r.ExpectedAssetId + "' is not 32 lowercase hex");
                if (!Hex32(r.TargetGuid) || !ownSet.Contains(r.TargetGuid)) p.Add(r.Key + " target " + r.TargetGuid + " is not one of our blueprints");
                if (ownSet.Contains(r.SourceGuid)) p.Add(r.Key + " source is one of our own blueprints (needs a vanilla source)");
                if (string.IsNullOrEmpty(r.SourceName) || string.IsNullOrEmpty(r.JbpPath) || !r.JbpPath.EndsWith("/" + r.SourceName + ".jbp", StringComparison.Ordinal) || string.IsNullOrEmpty(r.Reason))
                    p.Add(r.Key + " has no source name / jbp path matching the name / reason");
                if (NoFx.Contains(r.TargetGuid)) p.Add(r.Key + " targets " + r.TargetGuid + ", which is ruled FX-free");
                if (!Enum.IsDefined(typeof(FxSlot), r.TargetSlot) || !Enum.IsDefined(typeof(FxSlot), r.SourceSlot)) p.Add(r.Key + " has an undefined slot");
            }
            foreach (var kv in Preload)
            {
                if (!ownSet.Contains(kv.Key)) p.Add("preload target " + kv.Key + " is not one of our blueprints");
                if (kv.Value == null || kv.Value.Length == 0) p.Add("preload target " + kv.Key + " lists no FX");
                else foreach (var k in kv.Value) if (!Rows.Any(r => r.Key == k)) p.Add("preload target " + kv.Key + " -> unknown FX key " + k);
                if (NoFx.Contains(kv.Key)) p.Add("preload target " + kv.Key + " is ruled FX-free");
            }
            // 슬롯에 넣은 FX 는 그 블루프린트가 미리 읽기 목록에도 적는다(본편 HasteBuff.jbp 선례).
            foreach (var r in Rows)
            {
                string[] keys;
                if (!Preload.TryGetValue(r.TargetGuid, out keys) || !keys.Contains(r.Key)) p.Add(r.Key + " target " + r.TargetGuid + " does not preload its own FX");
            }
            foreach (var g in NoFx) if (!ownSet.Contains(g)) p.Add("no-FX entry " + g + " is not one of our blueprints");
            return p;
        }
    }
}
