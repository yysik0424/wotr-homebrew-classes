using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
     
    // 단일 원장(C4): 게임 쪽 PossessionFx(빌드 시 본편 원본 버프의 FxOnStart 를 읽어 이 표의 AssetId 와 대조) · 시험 `fx:`(blueprints.zip 직독 + 배선)가 모두 이 표를 읽는다.
    // 원본이 없거나 · 이름이 다르거나 · FxOnStart 가 비었거나 · AssetId 가 표와 다르면 빌드가 던진다(BuildFailed) — 엔진은 빈 PrefabLink 를 조용히 건너뛰므로 여기서 시끄럽게 막는다.
    // 엔진: 버프 FxOnStart 는 버프가 켜져 있는 동안 유지되고 꺼질 때 지워진다(Buff.ClearParticleEffect → FxHelper.Destroy, Assembly-CSharp dnlib 2026-11-15).
    // FX 만 더한다 — 게임 동작 값(지속·DC·비행·흡수)은 건드리지 않는다.
    public sealed class FxTarget
    {
        public readonly string Guid, Kind, Reason;
        public FxTarget(string guid, string kind, string reason) { Guid = guid; Kind = kind; Reason = reason; }
    }

    public static class FxTable
    {
        // 원본(유일). 값 = blueprints.zip 직독(2026-11-15, scratchpad fx-recon/fxdump.py) — 시험 `fx: jbp` 가 다시 확인한다.
        public const string SourceKey = "OracleSpiritWalk";
        public const string SourceGuid = "d866e35ea697df445be59e3d1c69d9ba";
        public const string SourceName = "OracleRevelationSpiritWalkBuff";
        public const string SourceJbp = "Classes/Oracle/OracleRevelations/OracleRevelationSpiritWalkBuff.jbp";
        public const string Slot = "BlueprintBuff.FxOnStart";
        public const string AssetId = "81c4df5abd309ef4b86f787a95669f1f";
        public const string SourceReason = "오라클 계시 「영체 보행」(실체 없는 몸이 되는 버프)의 지속 FX — 무기 영이 몸에 깃든 모습(승인 2026-11-15)";

        // 승인된 대체 원본(첫 원본 FxOnStart 가 비었을 때만 — 2026-11-15 직독 결과 첫 원본이 비어 있지 않아 쓰지 않는다).
        // 빌드는 대체로 조용히 넘어가지 않는다: 첫 원본이 비면 던지고, 메시지가 이 대체를 가리킨다(전환은 사람이 표를 고쳐서). 시험이 대체 원본도 비어 있지 않음을 확인한다.
        public const string FallbackGuid = "20f79fea035330b479fc899fa201d232";
        public const string FallbackName = "GhostFX";
        public const string FallbackJbp = "Buffs/GhostFX.jbp";
        public const string FallbackAssetId = "3365595fd05222546989ede9a139283b";

        public const string KindBuff = "BuffFxOnStart";        // 버프: FxOnStart = 원본 id(지속), ResourceAssetIds = [id](미리 읽기, 본편 관례)
        public const string KindAbility = "AbilityPreload";    // 능력: ResourceAssetIds = [id] 만(보이는 FX 는 버프가 낸다 — 능력은 버프를 켜는 쪽)

        public static readonly IReadOnlyList<FxTarget> Targets = new[]
        {
            new FxTarget(Guids.PossessionBuff1, KindBuff, "강신 I 효과 버프(Possession.cs Buffs[0])"),
            new FxTarget(Guids.PossessionBuff2, KindBuff, "강신 II 효과 버프(Buffs[1])"),
            new FxTarget(Guids.PossessionBuff3, KindBuff, "강신 III 효과 버프(Buffs[2])"),
            new FxTarget(Guids.PossessionAbility, KindAbility, "강신 능력 — 버프 I~III 를 켠다"),
        };

        public static FxTarget For(string guid)
        {
            var hit = Targets.Where(t => t.Guid == guid).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("WeaponSpiritShaman: blueprint " + guid + " is in FxTable.Targets " + hit.Length + " time(s)");
            return hit[0];
        }

        static bool Hex32(string s) { return s != null && s.Length == 32 && s.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리 블루프린트 GUID(Guids 상수 전부). 원본은 우리 것이면 안 되고, 대상은 우리 것이어야 한다.
        public static List<string> Problems(IEnumerable<string> own)
        {
            var p = new List<string>();
            var ownSet = new HashSet<string>(own ?? new string[0]);
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            foreach (var g in new[] { SourceGuid, AssetId, FallbackGuid, FallbackAssetId }) if (!Hex32(g)) p.Add("FX id '" + g + "' is not 32 lowercase hex");
            if (ownSet.Contains(SourceGuid) || ownSet.Contains(FallbackGuid)) p.Add("FX source is one of our own blueprints (needs a vanilla source)");
            if (AssetId == FallbackAssetId || SourceGuid == FallbackGuid) p.Add("fallback is the same as the source");
            if (Targets.Count == 0) p.Add("no FX targets");
            if (Targets.Select(t => t.Guid).Distinct().Count() != Targets.Count) p.Add("duplicate FX target");
            foreach (var t in Targets)
            {
                if (!ownSet.Contains(t.Guid)) p.Add("FX target " + t.Guid + " is not one of our blueprints");
                if (t.Kind != KindBuff && t.Kind != KindAbility) p.Add("FX target " + t.Guid + " kind '" + t.Kind + "' unknown");
                if (string.IsNullOrEmpty(t.Reason)) p.Add("FX target " + t.Guid + " has no reason");
            }
            if (!Targets.Any(t => t.Kind == KindBuff)) p.Add("no buff carries the FX (nothing would be visible)");
            return p;
        }
    }
}
