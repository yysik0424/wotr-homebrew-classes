using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
     
    // 단일 원장(C4): RebukePlan 의 기대 줄 · 게임 쪽 RebukeFx.Resolve(빌드 시 본편 원본 블루프린트의 FX 칸을 읽어 이 표의 AssetId 와 대조) · 시험 `fx:`(blueprints.zip 직독)이 모두 이 표를 읽는다.
    // 원본이 없거나 · FX 칸이 비었거나 · 칸의 AssetId 가 표와 다르면 빌드가 던진다(BuildFailed) — 엔진은 빈 PrefabLink 를 조용히 건너뛰므로(ContextActionSpawnFx.RunAction @0016 Load → null) 여기서 시끄럽게 막는다.
    // FX 만 더한다 — 게임 동작 값(피해·내성·버프)은 건드리지 않는다.
    public sealed class FxSource
    {
        public readonly string Key, SourceGuid, SourceName, JbpPath, Slot, AssetId, Reason;
        public FxSource(string key, string sourceGuid, string sourceName, string jbpPath, string slot, string assetId, string reason)
        { Key = key; SourceGuid = sourceGuid; SourceName = sourceName; JbpPath = jbpPath; Slot = slot; AssetId = assetId; Reason = reason; }
    }

    public static class FxTable
    {
        // 원본 칸 이름(게임·시험이 같은 문자열로 분기 — 모르는 칸은 던진다).
        public const string SlotAbilitySpawnFx = "AbilitySpawnFx.PrefabLink";   // BlueprintAbility 의 AbilitySpawnFx 컴포넌트(정확히 1개) PrefabLink
        public const string SlotBuffFxOnStart = "BlueprintBuff.FxOnStart";      // BlueprintBuff.FxOnStart

        // 값 = blueprints.zip 직독(2026-11-15, scratchpad fx-recon/fxdump.py): 원본 AssetId · 경로 · 칸의 PrefabLink.AssetId.
        public static readonly IReadOnlyList<FxSource> Sources = new[]
        {
            new FxSource("ForcePunch", "130e0b433e205294c964c5ce3e0c17ed", "ForcePunchEffect", "Spells/Level3/ForcePunchEffect.jbp", SlotAbilitySpawnFx, "01d23d0623adaa2478e8c7ebb608c3d7", "E1 역장·E4 밀쳐내기 — 역장 주먹 타격(대상 기준 Anchor=SelectedTarget)"),
            new FxSource("ShockingGrasp", "17451c1327c571641a1345bd31155209", "ShockingGraspEffect", "Spells/Level1/ShockingGraspEffect.jbp", SlotAbilitySpawnFx, "3ab291fca61cf3b4da311da82340ee9e", "E2 전격 — 전격 손아귀 타격"),
            new FxSource("ChillTouchEnemy", "3f1354f2c75d48e8857c7c3f4010975f", "ArmyMagusChillTouchBuffEnemy", "Armies/Abilities/ArmyMagusChillTouchBuffEnemy.jbp", SlotBuffFxOnStart, "ce04b565b3d63ec4cb047cdba3b969a5", "E3 서리 — 냉기 접촉을 맞은 적 쪽 FX"),
        };

        // 반격 효과 → FX 원본 키. 표에 없는 효과는 NoFx 에 이유와 함께(둘 다 아니면 Problems).
        public static readonly IReadOnlyDictionary<string, string> RebukeEffectFx = new Dictionary<string, string>
        {
            ["E1"] = "ForcePunch", ["E2"] = "ShockingGrasp", ["E3"] = "ChillTouchEnemy", ["E4"] = "ForcePunch",
        };

        // FX 를 더하지 않는 효과와 이유(본편 부품이 이미 FX 를 갖는다 — blueprints.zip 2026-11-15).
        public static readonly IReadOnlyDictionary<string, string> RebukeNoFx = new Dictionary<string, string>
        {
            ["E5"] = "SlowBuff 0bc608c3… FxOnStart d6a8c6ae… (내성 실패 시에만 보임)",
            ["E6"] = "DazeBuff 9934fedf… FxOnStart 396af91a… + Activated SpawnFx 28b3cd92… (내성 실패 시에만 보임)",
            ["E7"] = "DispelMagicTarget 143775c4… AbilitySpawnFx 3eda0e7f… (주문 시전 자체)",
        };

        public static FxSource Get(string key)
        {
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("BulwarkMage: FX source key '" + key + "' is in FxTable " + hit.Length + " time(s)");
            return hit[0];
        }

        // 효과의 FX(없으면 null — 단 NoFx 에 이유가 있어야 한다. 둘 다 없으면 던진다: 「표에 없음」이 「FX 없음」으로 새지 않게).
        public static FxSource ForRebuke(string effectId)
        {
            string key;
            if (RebukeEffectFx.TryGetValue(effectId, out key)) return Get(key);
            if (RebukeNoFx.ContainsKey(effectId)) return null;
            throw new KeyNotFoundException("BulwarkMage: rebuke effect '" + effectId + "' is neither in FxTable.RebukeEffectFx nor RebukeNoFx");
        }

        static bool Hex32(string s) { return s != null && s.Length == 32 && s.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // 표 무결성(C2) — 빈 목록 = 일치. effectIds = RebukeTable 의 효과 전부, own = 우리 블루프린트 GUID(원본이 우리 것이면 안 된다).
        public static List<string> Problems(IEnumerable<string> effectIds, IEnumerable<string> own)
        {
            var p = new List<string>();
            var ids = (effectIds ?? new string[0]).ToList();
            var ownSet = new HashSet<string>(own ?? new string[0]);
            if (ids.Count == 0) p.Add("no rebuke effect ids");
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            if (Sources.Count == 0) p.Add("no FX sources");
            if (Sources.Select(s => s.Key).Distinct().Count() != Sources.Count) p.Add("duplicate FX source key");
            foreach (var s in Sources)
            {
                if (!Hex32(s.SourceGuid)) p.Add("FX source " + s.Key + " GUID '" + s.SourceGuid + "' is not 32 lowercase hex");
                if (!Hex32(s.AssetId)) p.Add("FX source " + s.Key + " asset id '" + s.AssetId + "' is not 32 lowercase hex");
                if (s.Slot != SlotAbilitySpawnFx && s.Slot != SlotBuffFxOnStart) p.Add("FX source " + s.Key + " slot '" + s.Slot + "' unknown");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.SourceName)) p.Add("FX source " + s.Key + " has no reason / path / name");
                if (ownSet.Contains(s.SourceGuid)) p.Add("FX source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (!RebukeEffectFx.Values.Contains(s.Key)) p.Add("FX source " + s.Key + " is unused");
            }
            foreach (var kv in RebukeEffectFx) if (!Sources.Any(s => s.Key == kv.Value)) p.Add("rebuke " + kv.Key + " -> unknown FX key " + kv.Value);
            foreach (var e in RebukeEffectFx.Keys.Intersect(RebukeNoFx.Keys)) p.Add("rebuke " + e + " is both FX and no-FX");
            foreach (var e in RebukeEffectFx.Keys.Concat(RebukeNoFx.Keys)) if (!ids.Contains(e)) p.Add("FxTable names rebuke " + e + " which RebukeTable does not have");
            foreach (var e in ids) if (!RebukeEffectFx.ContainsKey(e) && !RebukeNoFx.ContainsKey(e)) p.Add("rebuke " + e + " has neither an FX source nor a no-FX reason");
            foreach (var kv in RebukeNoFx) if (string.IsNullOrEmpty(kv.Value)) p.Add("rebuke " + kv.Key + " no-FX reason empty");
            return p;
        }
    }
}
