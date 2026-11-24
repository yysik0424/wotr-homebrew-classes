using System;
using System.Collections.Generic;
using System.Linq;

namespace Golemancer
{
     
    //   결함: 응급 수리는 Personal 능력(시전자 = 주인)이라 본편 AbilitySpawnFx 를 달아도 FX 가 골렘에 닿지 않는다 — 지금은 완전히 조용했다.
    //   방식: ContextActionGolemRepair 가 Repair.Perform 성공 뒤 RepairFx.Play 로 골렘 View 에 FxHelper.SpawnFxOnUnit(선례 PackWarden AuxUnits.SpawnFx).
    // 원천 = 본편 IronBodyBuff(강철 몸) FxOnStart. 이 프리팹은 버프가 켜져 있는 동안 유지되는 지속형 용도다(StoneSkin 류 버프 FxOnStart 로 쓰인다, blueprints.zip 2026-11-15) —
    //   스스로 끝나는지 오프라인으로 확인할 수 없다(프리팹은 에셋 번들 안). 그래서 수명을 우리가 정한다: LifetimeRounds 라운드 뒤 FxHelper.Destroy(handle, false)
    //   = 버프가 꺼질 때 엔진이 하는 것과 같은 호출(Buff.ClearParticleEffect @0016, FxFadeOut 이 있으면 서서히 사라짐 — Assembly-CSharp dnlib 2026-11-15). 영원히 남지 않는다.
    //   그래서 승인된 대체(CureLightWounds AbilitySpawnFx)는 쓰지 않는다 — 시험이 대체 원본도 비어 있지 않음을 확인해 둔다.
    // 조용한 통과 금지(C1·C2): 빌드 시 RepairFx.Resolve 가 원본 없음·이름 다름·FxOnStart 빈칸·id 불일치를 던진다(BuildFailed).
    //   게임 중 FX 를 못 띄우는 경우(골렘 View 없음·프리팹 로드 실패·FX 시스템 거부·예외)는 수리 결과를 되돌리지 않고 경고 1줄(part.Warn) — 선례 PackWarden FxWarn 과 같은 규칙.
    //   합체 중(골렘을 거둠)에는 골렘이 화면에 없으므로 같은 FX 를 골렘을 입은 주인 View 에 띄운다(2026-11-15 판정 — TargetOf, 같은 수명·실패 처리).
    public static class RepairFxTable
    {
        public const string SourceGuid = "2eabea6a1f9a58246a822f207e8ca79e";
        public const string SourceName = "IronBodyBuff";
        public const string SourceJbp = "Spells/Level8/IronBodyBuff.jbp";
        public const string Slot = "BlueprintBuff.FxOnStart";
        public const string AssetId = "eec87237ed7b61149a952f56da85bbb1";
        public const string Reason = "강철 몸 변신 버프의 금속 질감 FX — 골렘(구조물)이 고쳐지는 느낌(승인 2026-11-15)";

        // 수명 = GolemRules.Dials.RepairFxLifetimeRounds 라운드(다이얼 단일 블록 — 시험 `dial: single block`). 초 = 엔진 Rounds.Seconds = 값 × 6초(Kingmaker.Utility.Rounds::get_Seconds @0007).
        public static int LifetimeRounds { get { return GolemRules.Dials.RepairFxLifetimeRounds; } }

        // 승인된 대체(원천이 끝나지 않는 지속형일 때만) — 위 수명 규칙으로 끝을 보장하므로 쓰지 않는다.
        public const string FallbackGuid = "47808d23c67033d4bbab86a1070fd62f";
        public const string FallbackName = "CureLightWounds";
        public const string FallbackJbp = "Spells/Level1/CureLightWounds.jbp";
        public const string FallbackAssetId = "61602c5b0ac793d489c008e9cb58f631";

         
        public const string TargetGolem = "Golem";
        public const string TargetMaster = "Master";
        public static string TargetOf(bool fusionIdle) { return fusionIdle ? TargetGolem : TargetMaster; }

        static bool Hex32(string s) { return s != null && s.Length == 32 && s.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리 블루프린트 GUID(원천은 본편이어야 한다).
        public static List<string> Problems(IEnumerable<string> own)
        {
            var p = new List<string>();
            var ownSet = new HashSet<string>(own ?? new string[0]);
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            foreach (var g in new[] { SourceGuid, AssetId, FallbackGuid, FallbackAssetId }) if (!Hex32(g)) p.Add("FX id '" + g + "' is not 32 lowercase hex");
            if (ownSet.Contains(SourceGuid) || ownSet.Contains(FallbackGuid)) p.Add("FX source is one of our own blueprints (needs a vanilla source)");
            if (LifetimeRounds < 1) p.Add("LifetimeRounds " + LifetimeRounds + " < 1 (the FX would be destroyed at once or never)");
            if (string.IsNullOrEmpty(Reason) || string.IsNullOrEmpty(SourceName) || string.IsNullOrEmpty(SourceJbp)) p.Add("FX table has an empty value");
            return p;
        }
    }
}
