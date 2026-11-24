using System;
using System.Collections.Generic;
using System.Linq;

namespace PhantomBlade
{
    // [순수] 귀신참 돌진 연출 FX 원장(소유자 결정 2026-11-16 밤: ① 령이 지나간 자리에 잔상 ② 베는 순간에 타격 효과 — 설계 §14.8). 게임 형식 없음(modinfo testSources).
    // FxTable(블루프린트 슬롯에 넣는 FX)과 다른 점: 이 FX 들은 우리 블루프린트에 넣지 않고 실행 중에 직접 띄운다(GhostSlashFx.cs — FxHelper.SpawnFxOnPoint / SpawnFxOnUnit).
    // 단일 원장(C4): 게임 쪽 GhostSlashFx(그 FX 를 처음 쓸 때 한 번)가 이 표대로 본편 원본 블루프린트의 슬롯을 읽어 (1) 원본 없음 · (2) 슬롯 비어 있음 · (3) 값이 AssetId 와 다름 ·
    //   (4) 번들 위치 목록에 없음 이면 오류 1줄 + 그 FX 끔(연출 실패가 귀신참 판정·빌드를 막지 않는다). 지어낸 링크 금지(Fx.Link 선례).
    // 시험 `dashfx:` 가 모든 행(고른 것·대안 모두)을 blueprints.zip 원본 jbp(AssetId·슬롯 값)와 Bundles\locationlist.json(AssetId → BundleName)에 대조한다.
    // 화면에 어떻게 보이는지는 오프라인으로 확인 못 한다(프리팹은 번들 안 — 눈 점검 E5). 이름·본편 쓰임새·번들 구성으로 골랐다.
    // 대안 조건: 「스스로 사라지는」 프리팹만 싣는다 — AutoDestroySeconds = 번들 직독(UnityPy 2026-11-16) AutoDestroy.Lifetime 최댓값, 반복 재생(looping) 입자 0개.
    //   실행 중에도 띄운 직후 AutoDestroy 가 없으면 오류 1줄 + 즉시 지우고 그 FX 를 끈다(GhostSlashFx.AfterSpawn) — 화면에 남는 사고 방지.
    //   (꼬리형 expeditiousretreat00_trail.fx · haste00_trail.fx 는 반복 재생 + AutoDestroy 없음이라 이 표에 싣지 않는다 — 붙였다 떼는 방식이 필요, 설계 §14.8 비교.)
    // 다른 모드 교차 확인(UMM Log.txt 2026-11-16 읽기): 아래 원본 7개의 GUID·이름은 `Patched:` 줄을 포함해 로그 어디에도 없다. ForcePunchEffect(130e0b43…)는
    //   TabletopTweaks-Base 가 메타매직 플래그를 바꾸므로 출처에서 뺐다(같은 프리팹 force00_crithitsnap.fx 는 쓰지 않는다).
    public enum GhostSlashFxUse { Trail, Hit }

    // 원본 블루프린트에서 읽는 슬롯. Buff* = BlueprintBuff 의 FxOnStart / FxOnRemove, Projectile* = BlueprintProjectile.ProjectileHit 의 HitFx / MissFx.
    public enum GhostSlashFxSlot { BuffFxOnStart, BuffFxOnRemove, ProjectileHitFx, ProjectileMissFx }

    public sealed class GhostSlashFxRow
    {
        public readonly string Key, SourceGuid, SourceName, JbpPath, AssetId, BundleName, Note;
        public readonly GhostSlashFxUse Use;
        public readonly GhostSlashFxSlot Slot;
        public readonly double AutoDestroySeconds;
        public GhostSlashFxRow(string key, GhostSlashFxUse use, string sourceGuid, string sourceName, string jbpPath, GhostSlashFxSlot slot, string assetId, string bundleName, double autoDestroySeconds, string note)
        {
            Key = key; Use = use; SourceGuid = sourceGuid; SourceName = sourceName; JbpPath = jbpPath; Slot = slot; AssetId = assetId; BundleName = bundleName; AutoDestroySeconds = autoDestroySeconds; Note = note;
        }
    }

    public static class GhostSlashFxTable
    {
        // ---- 잔상(지점에 띄움) ----
        // 1순위: 검은 구름. 본편에서 「그림자에서 적 앞으로 순간이동해 벤다」 능력(DarkLurkerBladeFromShadowsAbility · MantisZealotTeleportToEnemyAbility 의 DisappearFx)이 쓰는 프리팹 —
        //   귀신참 돌진과 같은 상황이고 령(그림자 악마)의 검은 색과 맞는다. 입자 9개 중 유닛 몸에 붙는 것(ParticlesSnapController)은 2개뿐이라 지점에 띄워도 대부분 보일 것으로 판단. 소리 있음.
        public const string TrailBlackCloud = "TrailBlackCloud";
        // 2순위: 유령이 흩어지는 모습(령이 쓰는 ShadowFX 버프의 FxOnRemove — 령이 사라질 때 나오는 바로 그 효과). 소리 없음. 입자 3개 전부 유닛 몸에 붙는 종류라 지점에서는 흐릴 수 있다.
        public const string TrailGhostFade = "TrailGhostFade";
        // 3순위: 차원문 시전자가 사라질 때의 효과(DimensionDoor.CasterDisappearFx 와 같은 프리팹). 보랏빛 차원문 색일 것 — 령 색과는 덜 맞는다. 소리 없음.
        public const string TrailDimensionDoor = "TrailDimensionDoor";

        // ---- 타격(대상 유닛에 띄움) ----
        // 1순위: 역장 표준 명중 섬광(귀신참 = 역장 피해). 본편 HitSystem 의 역장 StandardLink 와 같은 프리팹(Root/HitSystem.jbp DamageTypes[0].Hits.StandardLink = 84de6796…).
        public const string HitForceStandard = "HitForceStandard";
        // 2순위: 역장 큰 명중(Battering Blast). 같은 계열, 더 크다.
        public const string HitForceMajor = "HitForceMajor";
        // 3순위: 악마 발톱 할퀴기(본편 데빌 발톱 공격의 명중 효과 — 령의 Claw 동작과 맞는다). 발톱 모형이 통째로 나오는 프리팹이라 과할 수 있다.
        public const string HitDevilClaw = "HitDevilClaw";
        // 4순위: 음에너지 표준 명중(어두운 색 — 그림자 느낌).
        public const string HitNegative = "HitNegative";

        // 값 = blueprints.zip · Bundles\locationlist.json · 번들(UnityPy) 2026-11-16 직독.
        public static readonly IReadOnlyList<GhostSlashFxRow> Rows = new[]
        {
            new GhostSlashFxRow(TrailBlackCloud, GhostSlashFxUse.Trail, "926a5897681c4901a6e70989925b6d5b", "DLC3_NahyndrianHolySymbolPossesBuff", "Items/UsableSlotItem/DLC3/DLC3_NahyndrianHolySymbolPossesBuff.jbp",
                GhostSlashFxSlot.BuffFxOnStart, "f1f41fef03cb5734e95db1342f0c605e", "shadowboss_blackcloudtransformation00_dlc3.fx", 3.5, "검은 구름(그림자 순간이동 능력의 사라짐 효과와 같은 프리팹)"),
            // 원본 GUID = 령이 쓰는 ShadowFX 버프(8caafe9d…) — 값은 SpiritRules.Dials 한 곳(C4, 시험이 리터럴 1회를 잠근다).
            new GhostSlashFxRow(TrailGhostFade, GhostSlashFxUse.Trail, SpiritRules.Dials.ShadowFxBuffGuid, "ShadowFX", "Buffs/ShadowFX.jbp",
                GhostSlashFxSlot.BuffFxOnRemove, "39802c709edd65941a3da18e3c581965", "ghostform00_death.fx", 5.0, "유령이 흩어짐(령의 ShadowFX 버프가 끝날 때 효과)"),
            new GhostSlashFxRow(TrailDimensionDoor, GhostSlashFxUse.Trail, "c104ea7bb39e422fbc1d6ead61b63525", "ArmyDimentionDoorBuff", "Armies/Abilities/ArmyDimentionDoorBuff.jbp",
                GhostSlashFxSlot.BuffFxOnStart, "9535b8cda5cb3164eb9e53a621e53a60", "dimensiondoor00_casterbuff.fx", 3.0, "차원문 시전자 사라짐"),
            new GhostSlashFxRow(HitForceStandard, GhostSlashFxUse.Hit, "2e3992d1695960347a7f9bdf8122966f", "MagicMissile00", "FX/Spell Level 1/MagicMissile00.jbp",
                GhostSlashFxSlot.ProjectileMissFx, "84de679628897e24f844cecbb52075de", "force00_standarthit.fx", 4.0, "역장 표준 명중 섬광(마법 화살)"),
            new GhostSlashFxRow(HitForceMajor, GhostSlashFxUse.Hit, "7b744bce71ea65f4ab43df2090fe59f2", "BatteringBlast00", "FX/Spell Level 3/BatteringBlast00.jbp",
                GhostSlashFxSlot.ProjectileMissFx, "8c0b18138403c794d8aff52b0e74debf", "force00_majorhit.fx", 4.0, "역장 큰 명중(Battering Blast)"),
            new GhostSlashFxRow(HitDevilClaw, GhostSlashFxUse.Hit, "f76e194520d6f9946bb48d8852ce9e8c", "DevilClawRight00", "FX/Creatures/DevilClawRight00.jbp",
                GhostSlashFxSlot.ProjectileHitFx, "96d487e1ae841c242ac8ea8b5668f6b1", "devilclawright00.fx", 1.7, "악마 발톱 할퀴기"),
            new GhostSlashFxRow(HitNegative, GhostSlashFxUse.Hit, "3f068599c7549e84497154248278e5b0", "NegativeCommonProjectile00", "FX/Spell Level None/NegativeCommonProjectile00.jbp",
                GhostSlashFxSlot.ProjectileMissFx, "c33626c50e31a5f41893c2d49b374966", "negative00_standarthit.fx", 5.0, "음에너지 표준 명중"),
        };

        // 열쇠 → 행. 없거나 겹치면 던진다(조용히 null 금지).
        public static GhostSlashFxRow Get(string key)
        {
            var hit = Rows.Where(r => r.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("PhantomBlade: ghost slash FX row '" + key + "' is in GhostSlashFxTable " + hit.Length + " time(s)");
            return hit[0];
        }

        // 다이얼이 고른 열쇠 → 행. 용도가 다르면 던진다(잔상 자리에 타격 FX 를 넣는 실수 — 타격 FX 는 유닛에, 잔상 FX 는 지점에 띄운다).
        public static GhostSlashFxRow Require(string key, GhostSlashFxUse use)
        {
            var row = Get(key);
            if (row.Use != use) throw new InvalidOperationException("PhantomBlade: ghost slash FX '" + key + "' is a " + row.Use + " effect, but the dial uses it as " + use);
            return row;
        }

        static bool Hex32(string s) { return s != null && s.Length == 32 && s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')); }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리 블루프린트 GUID 전체(원본은 본편 것이어야 한다).
        public static List<string> Problems(IEnumerable<string> own)
        {
            var p = new List<string>();
            if (own == null) { p.Add("no own GUID set"); return p; }
            var ownSet = new HashSet<string>(own);
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            if (Rows.Count == 0) p.Add("GhostSlashFxTable has no rows");
            if (Rows.Select(r => r.Key).Distinct().Count() != Rows.Count) p.Add("duplicate FX key");
            if (Rows.Select(r => r.AssetId).Distinct().Count() != Rows.Count) p.Add("one prefab is listed twice");
            foreach (GhostSlashFxUse use in Enum.GetValues(typeof(GhostSlashFxUse)))
                if (!Rows.Any(r => r.Use == use)) p.Add("no " + use + " row");
            foreach (var r in Rows)
            {
                if (string.IsNullOrEmpty(r.Key)) p.Add("a row has no key");
                if (!Hex32(r.SourceGuid)) p.Add(r.Key + " source GUID '" + r.SourceGuid + "' is not 32 lowercase hex");
                if (!Hex32(r.AssetId)) p.Add(r.Key + " asset id '" + r.AssetId + "' is not 32 lowercase hex");
                if (ownSet.Contains(r.SourceGuid)) p.Add(r.Key + " source is one of our own blueprints (needs a vanilla source)");
                if (string.IsNullOrEmpty(r.SourceName) || string.IsNullOrEmpty(r.JbpPath) || !r.JbpPath.EndsWith("/" + r.SourceName + ".jbp", StringComparison.Ordinal) || string.IsNullOrEmpty(r.Note))
                    p.Add(r.Key + " has no source name / jbp path matching the name / note");
                if (string.IsNullOrEmpty(r.BundleName) || !r.BundleName.EndsWith(".fx", StringComparison.Ordinal)) p.Add(r.Key + " has no .fx bundle name");
                if (!Enum.IsDefined(typeof(GhostSlashFxUse), r.Use) || !Enum.IsDefined(typeof(GhostSlashFxSlot), r.Slot)) p.Add(r.Key + " has an undefined use / slot");
                // 스스로 사라지는 프리팹만: 수명이 유한한 양수여야 한다(0·NaN·∞ = 「자동 소멸 확인 안 됨」 → 표에 실을 수 없다).
                if (double.IsNaN(r.AutoDestroySeconds) || double.IsInfinity(r.AutoDestroySeconds) || r.AutoDestroySeconds <= 0) p.Add(r.Key + " has no finite auto-destroy lifetime (" + r.AutoDestroySeconds + ")");
            }
            return p;
        }
    }
}
