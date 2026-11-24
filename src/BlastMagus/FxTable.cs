using System;
using System.Collections.Generic;
using System.Linq;

namespace BlastMagus
{
     
    //   폭발(Blast Strike) 능력 = 본편 ArcaneAccuracyAbility 의 AbilitySpawnFx(시전 순간 1회).
    //   에너지 주입 원소 자식 버프 = FxOnStart(버프가 켜져 있는 동안 유지) — 본편 원본이 있는 원소만(화염·냉기). 나머지 원소는 FX 없음(지어내지 않는다, 이유를 적는다).
    // 단일 원장(C4): 게임 쪽 BlastFx.Resolve(빌드 시 본편 원본의 FX 칸을 읽어 표의 AssetId 와 대조) · BlastStrike/Energize 의 되읽기 · 시험 `fx:`(blueprints.zip 직독)이 모두 이 표를 읽는다.
    // 엔진은 빈 FX 칸을 조용히 건너뛴다(Buff.TrySpawnParticleEffect @008E~00A7: FxOnStart null → 아무것도 안 띄움) — 그래서 원본 없음 · 칸 빔 · 값 다름은 빌드가 던진다.
    public sealed class FxSource
    {
        public readonly string Key, SourceGuid, SourceName, JbpPath, Slot, AssetId, Reason;
        public FxSource(string key, string sourceGuid, string sourceName, string jbpPath, string slot, string assetId, string reason)
        { Key = key; SourceGuid = sourceGuid; SourceName = sourceName; JbpPath = jbpPath; Slot = slot; AssetId = assetId; Reason = reason; }
    }

    public static class FxTable
    {
        public const string SlotAbilitySpawnFx = "AbilitySpawnFx:PrefabLink";   // BlueprintAbility 의 AbilitySpawnFx 컴포넌트(정확히 1개) — 그 컴포넌트의 다른 칸(Time·Anchor…)도 그대로 옮긴다
        public const string SlotBuffFxOnStart = "BlueprintBuff:FxOnStart";

        public const string BlastStrikeKey = "BlastStrike";

        // 값 = blueprints.zip 직독(2026-11-15, scratchpad fx-recon/fxdump.py).
        public static readonly IReadOnlyList<FxSource> Sources = new[]
        {
            new FxSource(BlastStrikeKey, Guids.Game.ArcaneAccuracyAbility, "ArcaneAccuracyAbility", "Classes/Magus/Arcanas/ArcaneAccuracyAbility.jbp", SlotAbilitySpawnFx, "c388856d0e8855f429a83ccba67944ba", "폭발 — 같은 모양의 본편 신속 메이거스 능력(Time OnApplyEffect, Anchor Caster)"),
            new FxSource("EnergizeFire", "944fcfce863148c795fdfb22e185ef11", "ArmyMagusTouchOfCombustionBuff", "Armies/Abilities/ArmyMagusTouchOfCombustionBuff.jbp", SlotBuffFxOnStart, "91e5a56dd421a2941984a36a2af164b6", "에너지 주입(화염) — 메이거스 연소의 손길 무기 FX"),
            new FxSource("EnergizeCold", "82bae4552cc744e3ac76ba872f85b34f", "ArmyMagusChillTouchBuff", "Armies/Abilities/ArmyMagusChillTouchBuff.jbp", SlotBuffFxOnStart, "e9930f40a35b67c418e78a98c601c93b", "에너지 주입(냉기) — 메이거스 냉기 손길 무기 FX"),
            new FxSource("EnergizeElectric", "3d9e4ec7766843db801cccba28df3ec1", "ArmyTempestStrikeBuff", "Armies/Abilities/ArmyTempestStrikeBuff.jbp", SlotBuffFxOnStart, "1d1465ffa2699644ba8dfac48cb33195", "에너지 주입(전격) — 폭풍 일격 무기 FX(= Shock.WeaponFxPrefab, 2026-11-15 추가)"),
        };

        // 원소 → FX 원본 키.
        public static readonly IReadOnlyDictionary<Element, string> EnergizeFx = new Dictionary<Element, string>
        {
            [Element.Fire] = "EnergizeFire", [Element.Cold] = "EnergizeCold", [Element.Electric] = "EnergizeElectric",
        };

        // FX 없는 원소와 이유. 승인 목록(2026-11-15)의 산(acid) 원본 ArmyMagusCorrosiveTouchBuff 는 우리 원소 enum 에 산 원소가 없어 쓰지 않는다.
        const string NoApprovedSource = "승인된 본편 원본 없음(2026-11-15) — 만들지 않음";
        public static readonly IReadOnlyDictionary<Element, string> EnergizeNoFx = new Dictionary<Element, string>
        {
            [Element.Air] = NoApprovedSource, [Element.Earth] = NoApprovedSource, [Element.Water] = NoApprovedSource,
            [Element.Telekinetic] = NoApprovedSource, [Element.Gravity] = NoApprovedSource, [Element.Wood] = NoApprovedSource,
            [Element.Negative] = NoApprovedSource, [Element.Positive] = NoApprovedSource,
        };

        public static FxSource Get(string key)
        {
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("BlastMagus: FX source key '" + key + "' is in FxTable " + hit.Length + " time(s)");
            return hit[0];
        }

        // 원소의 FX(없으면 null — 단 NoFx 에 이유가 있어야 한다. 둘 다 없으면 던진다).
        public static FxSource ForEnergize(Element e)
        {
            string key;
            if (EnergizeFx.TryGetValue(e, out key)) return Get(key);
            if (EnergizeNoFx.ContainsKey(e)) return null;
            throw new KeyNotFoundException("BlastMagus: element " + e + " is neither in FxTable.EnergizeFx nor EnergizeNoFx");
        }

        static bool Hex32(string s) { return s != null && s.Length == 32 && s.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리 블루프린트 GUID(원본이 우리 것이면 안 된다).
        public static List<string> Problems(IEnumerable<string> own)
        {
            var p = new List<string>();
            var ownSet = new HashSet<string>(own ?? new string[0]);
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
                if (s.Key != BlastStrikeKey && !EnergizeFx.Values.Contains(s.Key)) p.Add("FX source " + s.Key + " is unused");
            }
            if (!Sources.Any(s => s.Key == BlastStrikeKey && s.Slot == SlotAbilitySpawnFx)) p.Add("no AbilitySpawnFx source for Blast Strike");
            foreach (var kv in EnergizeFx)
                if (!Sources.Any(s => s.Key == kv.Value && s.Slot == SlotBuffFxOnStart)) p.Add("energize " + kv.Key + " -> " + kv.Value + " is not a FxOnStart source");
            foreach (var e in EnergizeFx.Keys.Intersect(EnergizeNoFx.Keys)) p.Add("energize " + e + " is both FX and no-FX");
            foreach (Element e in Enum.GetValues(typeof(Element))) if (!EnergizeFx.ContainsKey(e) && !EnergizeNoFx.ContainsKey(e)) p.Add("energize " + e + " has neither an FX source nor a no-FX reason");
            foreach (var kv in EnergizeNoFx) if (string.IsNullOrEmpty(kv.Value)) p.Add("energize " + kv.Key + " no-FX reason empty");
            return p;
        }
    }
}
