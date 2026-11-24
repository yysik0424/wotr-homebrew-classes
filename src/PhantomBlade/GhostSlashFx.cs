using System;
using Kingmaker.Blueprints;
using Kingmaker.BundlesLoading;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using AutoDestroy = Kingmaker.Utility.AutoDestroy;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace PhantomBlade
{
    // 귀신참 돌진의 잔상·타격 효과(소유자 결정 2026-11-16 밤: ① 「령이 지나간 자리에 잔상」 ② 「베는 순간에 타격 효과」 — 설계 §14.8). 연출만이다.
    //   규칙·명령·유닛 파트·사실·버프를 만들지 않는다 → 세이브에 남는 것이 없다(상태는 이 클래스의 정적 필드 = 프로세스 메모리뿐).
    // 어느 FX 인지 = SpiritRules.Dials.DashTrailFxKey / DashHitFxKey → GhostSlashFxTable 의 행(순수 원장). 판정(띄울지)·잔상 지점 계산 = SpiritRules(순수, 시험이 실행).
    // 띄우는 경로 = 본편과 같은 공개 API:
    //   잔상 = FxHelper.SpawnFxOnPoint(프리팹, 지점, 파티 여부, 회전 없음, 우선순위) — 본편 ContextActionSpawnFx.RunAction IL 008C~0097 과 같은 인자 모양.
    //   타격 = FxHelper.SpawnFxOnUnit(프리팹, 대상 뷰, null, null, 0 벡터, 우선순위) — 본편 명중 효과 HitPlayer.PlayHits IL 00E6~00FC 와 같은 인자.
    //   우선순위 = EventuallyImportant: 프레임당 생성 상한(FxSpawnController.Tick IL 002E~003A)을 지키면서 버려지지 않는다(NotImportant 만 버려짐 — Tick IL 0073~00AE).
    // 실패는 시끄럽게, 그러나 판정을 막지 않게: 어느 단계든 실패하면 SpiritVisualErrors.ReportOnce(오류 1줄 — 점검 S11 이 센다) + Failures++ 후 돌아간다. 던지지 않는다
    //   (호출자 = SpiritFollower.DashPlace 의 돌진 진행·휘두름, 그 위의 귀신참 광선 판정과 무관).
    // 화면에 남는 사고 방지: 표에는 AutoDestroy 가 있는 프리팹만 싣고(번들 직독), 띄운 직후 실제 객체에 AutoDestroy 가 없으면 오류 1줄 + 즉시 지우고 그 FX 를 끈다(AfterSpawn).
    public static class GhostSlashFx
    {
        // FX 하나의 상태. 3 상태: 아직 쓴 적 없음(Link == null ∧ Problem == null) / 준비됨(Link != null) / 꺼짐(Problem != null — 이미 한 번 보고함).
        sealed class Slot
        {
            public readonly GhostSlashFxUse Use;
            public readonly string DialKey;
            public GhostSlashFxRow Row;
            public PrefabLink Link;
            public string Problem;
            public Slot(GhostSlashFxUse use, string dialKey) { Use = use; DialKey = dialKey; }
        }

        static readonly Slot trail = new Slot(GhostSlashFxUse.Trail, SpiritRules.Dials.DashTrailFxKey);
        static readonly Slot hit = new Slot(GhostSlashFxUse.Hit, SpiritRules.Dials.DashHitFxKey);
        static int trailSpawned, hitSpawned, trailSkipped, hitSkipped, failures;























        // 처음 쓸 때 한 번(Prefab 이 부른다 — 그때는 본편 블루프린트가 전부 읽혀 있다). 표의 원본 블루프린트 슬롯을 읽어 대조하고 링크를 만든다.
        //   블루프린트 빌드(Blueprints.BuildAll)에 넣지 않은 이유: 연출 FX 의 실패가 모드 빌드·직업 등록을 막으면 안 되고, BuildAll 의 호출 순서는 시험이 잠가 둔 그대로 둔다.
        //   FX 마다 따로 잡는다 — 하나가 실패해도 다른 하나는 켜진다. 실패 = 오류 1줄 + 그 FX 끔(다시 시도하지 않는다).
        static void ResolveOne(Slot s)
        {
            try
            {
                var row = GhostSlashFxTable.Require(s.DialKey, s.Use);
                string got = ReadSource(row);
                if (string.IsNullOrEmpty(got)) throw new InvalidOperationException("source " + row.SourceName + " (" + row.SourceGuid + ") " + row.Slot + " is empty; GhostSlashFxTable expects " + row.AssetId);
                if (got != row.AssetId) throw new InvalidOperationException("source " + row.SourceName + " " + row.Slot + " is " + got + ", GhostSlashFxTable expects " + row.AssetId + " (another mod or a game update changed the source blueprint)");
                var bundles = BundlesLoadService.Instance;
                if (bundles == null) throw new InvalidOperationException("BundlesLoadService.Instance is null; cannot verify prefab " + got);
                if (!bundles.HasLocation(got)) throw new InvalidOperationException("prefab " + got + " (" + row.BundleName + ") is not in the bundle location list");
                s.Row = row;
                s.Link = new PrefabLink { AssetId = got };
                ModContext.Log("PhantomBlade: ghost slash " + s.Use + " FX = " + row.Key + " (" + row.BundleName + " " + got + " from " + row.SourceName + "." + row.Slot + ")");
            }
            catch (Exception e)
            {
                Disable(s, "could not be resolved: " + e.Message);
            }
        }

        // 원본 블루프린트의 슬롯 값(AssetId). 원본이 없거나 형식이 다르면 Get 이 던진다. 슬롯이 비었으면 null 을 돌려주고 호출자가 던진다(빈 링크로 진행 금지).
        static string ReadSource(GhostSlashFxRow row)
        {
            PrefabLink link;
            string name;
            switch (row.Slot)
            {
                case GhostSlashFxSlot.BuffFxOnStart: { var b = Get<BlueprintBuff>(row.SourceGuid); name = b.name; link = b.FxOnStart; break; }
                case GhostSlashFxSlot.BuffFxOnRemove: { var b = Get<BlueprintBuff>(row.SourceGuid); name = b.name; link = b.FxOnRemove; break; }
                case GhostSlashFxSlot.ProjectileHitFx: { var p = Get<BlueprintProjectile>(row.SourceGuid); name = p.name; link = p.ProjectileHit == null ? null : p.ProjectileHit.HitFx; break; }
                case GhostSlashFxSlot.ProjectileMissFx: { var p = Get<BlueprintProjectile>(row.SourceGuid); name = p.name; link = p.ProjectileHit == null ? null : p.ProjectileHit.MissFx; break; }
                default: throw new InvalidOperationException("source slot " + row.Slot + " has no reader");
            }
            if (name != row.SourceName) throw new InvalidOperationException("source " + row.SourceGuid + " is '" + name + "', GhostSlashFxTable says '" + row.SourceName + "'");
            return link == null ? null : link.AssetId;
        }

        // 이 FX 를 끈다 + 오류 1줄(FX 마다 한 번 — 같은 FX 를 다시 끄려 하면 줄은 안 나오고 횟수만).
        static void Disable(Slot s, string why)
        {
            failures++;
            if (s.Problem == null) s.Problem = why;
            s.Link = null;
            SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteDashFx, s.Use + "|disabled", "ghost slash " + s.Use + " effect (" + s.DialKey + ") is switched off: " + why + " (the ghost slash itself is unaffected)");
        }

        // 띄울 프리팹. null = 띄울 수 없다(이미 보고했거나 여기서 보고한다) — 호출자는 그 FX 만 건너뛴다.
        static GameObject Prefab(Slot s)
        {
            if (s.Problem != null) { failures++; return null; } // 꺼짐(이미 오류 1줄) — 횟수만
            if (s.Link == null)
            {
                ResolveOne(s); // 처음 한 번: 성공하면 Link, 실패하면 Problem(오류 1줄)
                if (s.Link == null) return null;
            }
            // 본편 버프 FX 와 같은 읽기(Buff.TrySpawnParticleEffect → PrefabLink.Load): 이미 읽혀 있으면 그 객체, 아니면 번들에서 읽는다(WeakResourceLink`1.Load IL 0017~0073).
            var prefab = s.Link.Load(false, false);
            if (prefab == null)
            {
                Disable(s, "prefab " + s.Link.AssetId + " (" + s.Row.BundleName + ") did not load (PrefabLink.Load returned null)");
                return null;
            }
            return prefab;
        }

        // 띄운 뒤(대기열에 있었으면 실제로 생성된 뒤 — FxHandle.RunAfterSpawn IL 0000~001F): 스스로 사라지는 장치(AutoDestroy)가 없으면 화면에 남는다 → 즉시 지우고 그 FX 를 끈다.
        //   엔진은 생성 때 자식의 AutoDestroy 를 전부 켠다(FxHelper.EnableAutoDestroy IL 0000~0042) — 없으면 아무도 이 객체를 지우지 않는다.
        static void AfterSpawn(Slot s, IFxHandle handle, GameObject spawned)
        {
            try
            {
                if (spawned == null) { Disable(s, "the engine spawned no object for prefab " + s.Row.AssetId); return; }
                // 뿌리 객체의 AutoDestroy 여야 한다: 자식의 것은 그 자식만 끈다(AutoDestroy.Destroy IL 0000~002F), 뿌리의 것이 객체 전체를 풀로 돌려보낸다(IL 007B~0087).
                //   표의 7개 프리팹은 전부 뿌리에 있다(번들 직독 2026-11-16, 수명 = GhostSlashFxRow.AutoDestroySeconds).
                if (spawned.GetComponent<AutoDestroy>() != null) return; // 정상: 스스로 사라진다
                FxHelper.Destroy(handle, true);
                Disable(s, "spawned prefab " + s.Row.BundleName + " has no AutoDestroy on its root object (it would stay on screen) — destroyed it at once");
            }
            catch (Exception e)
            {
                Disable(s, "post-spawn check failed: " + e.Message);
            }
        }

        // 생성 요청 하나의 결과를 센다. true = 띄웠다(또는 대기열에 들어갔다).
        static bool Track(Slot s, IFxHandle handle)
        {
            if (handle == null) { Disable(s, "FxHelper returned no handle"); return false; }
            // 엔진이 FX 생성을 꺼 둔 상태(FxSpawnController.Spawn IL 0000~0016: 꺼져 있으면 이미 파괴된 핸들을 돌려준다) — 본편 FX 도 전부 안 뜨는 정당한 무동작.
            if (handle.IsDestroyed) return false;
            handle.RunAfterSpawn(go => AfterSpawn(s, handle, go));
            return true;
        }

        // 가는 길의 잔상 지점: 출발점 → 대상 앞. 대상 앞 = 순수 SpiritRules.DashDestination(돌진 상태기 DashStep 이 쓰는 바로 그 함수·같은 다이얼 — C4).
        //   실패하면 보고하고 「잔상 없음」(빈 배열) — 돌진은 시작한다.
        internal static Vector3[] PlanOutbound(Vector3 start, Vector3 target, float targetRadius)
        {
            try
            {
                var dest = SpiritRules.DashDestination(((double)start.x, (double)start.y, (double)start.z), ((double)target.x, (double)target.y, (double)target.z), targetRadius,
                    SpiritRules.Dials.DashMarginMeters, SpiritRules.Dials.SpiritUpMeters);
                return PlanTrail(start, new Vector3((float)dest.x, (float)dest.y, (float)dest.z));
            }
            catch (Exception e)
            {
                failures++;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteDashFx, "trail-plan-outbound", "ghost slash outbound trail could not be planned (no trail on the way out): " + e.Message);
                return new Vector3[0];
            }
        }

        // 잔상 지점 계획(순수 SpiritRules.DashTrailPoints 에 다이얼을 넘긴다). 실패하면 보고하고 「잔상 없음」(빈 배열) — 돌진 자체는 계속된다.
        internal static Vector3[] PlanTrail(Vector3 from, Vector3 to)
        {
            try
            {
                var pts = SpiritRules.DashTrailPoints(((double)from.x, (double)from.y, (double)from.z), ((double)to.x, (double)to.y, (double)to.z),
                    SpiritRules.Dials.DashTrailSpacingMeters, SpiritRules.Dials.DashTrailMaxPoints);
                var r = new Vector3[pts.Length];
                for (int i = 0; i < pts.Length; i++) r[i] = new Vector3((float)pts[i].x, (float)pts[i].y, (float)pts[i].z);
                return r;
            }
            catch (Exception e)
            {
                failures++;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteDashFx, "trail-plan", "ghost slash trail points could not be planned (no trail for this leg): " + e.Message);
                return new Vector3[0];
            }
        }

        // 잔상: points[first .. first + count) 에 하나씩. 돌려주는 값 = 띄운 수. spiritHidden = 령이 숨겨진 상태(SpiritFollower.hiddenByUs).
        //   건너뜀(다이얼 꺼짐·령 숨김)은 명시적 분기(SpiritRules.DashTrailFor) — TrailSkipped 에 센다.
        internal static int Trail(UnitEntityData owner, Vector3[] points, int first, int count, bool spiritHidden)
        {
            try
            {
                if ((object)owner == null) throw new ArgumentNullException("owner");
                if (points == null) throw new ArgumentNullException("points");
                if (first < 0 || count < 0 || first + count > points.Length) throw new ArgumentOutOfRangeException("count", "trail range " + first + "+" + count + " outside " + points.Length + " point(s)");
                if (count == 0) return 0; // 정당: 이번에 띄울 지점이 없다
                var decision = SpiritRules.DashTrailFor(SpiritRules.Dials.DashTrailEnabled, spiritHidden);
                if (decision != SpiritRules.DashFx.Show)
                {
                    if (decision != SpiritRules.DashFx.Disabled && decision != SpiritRules.DashFx.SpiritHidden) throw new InvalidOperationException("unknown trail decision " + decision);
                    trailSkipped += count;
                    return 0;
                }
                var prefab = Prefab(trail);
                if (prefab == null) return 0; // 보고됨
                int made = 0;
                for (int i = first; i < first + count; i++)
                {
                    var handle = FxHelper.SpawnFxOnPoint(prefab, points[i], owner.IsPlayerFaction, Quaternion.identity, FxPriority.EventuallyImportant);
                    if (Track(trail, handle)) made++;
                }
                trailSpawned += made;
                return made;
            }
            catch (Exception e)
            {
                failures++;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteDashFx, "trail-failed", "ghost slash trail effect failed (the dash continues): " + e);
                return 0;
            }
        }

        // 타격 효과: 휘두름 1회마다 대상 유닛에 하나. true = 띄웠다. 대상이 사라지는 중·게임 밖·이미 쓰러짐·령 숨김·다이얼 꺼짐이면 띄우지 않는다
        //   (명시적 분기 SpiritRules.DashHitFxFor — 한 줄 기록, HitSkipped 에 센다). 대상 상태를 읽을 수 없으면 실패로 보고한다(「쓰러지지 않음」으로 넘기지 않는다).
        internal static bool Hit(UnitEntityData owner, UnitEntityData target, bool spiritHidden)
        {
            try
            {
                if ((object)owner == null) throw new ArgumentNullException("owner");
                if ((object)target == null) throw new ArgumentNullException("target");
                var state = target.State;
                if (state == null) throw new InvalidOperationException("target " + target.CharacterName + " has no State (cannot tell whether it is down)");
                var decision = SpiritRules.DashHitFxFor(SpiritRules.Dials.DashHitFxEnabled, spiritHidden, StandSpirit.Going(target), target.IsInGame ? SpiritRules.Tri.Yes : SpiritRules.Tri.No, state.IsDead);
                if (decision != SpiritRules.DashHitFx.Show)
                {
                    hitSkipped++;
                    if (decision != SpiritRules.DashHitFx.Disabled)
                        ModContext.Log("PhantomBlade: ghost slash hit effect skipped for " + owner.CharacterName + " -> " + target.CharacterName + " (" + decision + ")");
                    return false;
                }
                var view = target.View;
                if (view == null) throw new InvalidOperationException("target " + target.CharacterName + " has no view while in the game");
                var prefab = Prefab(hit);
                if (prefab == null) return false; // 보고됨
                var handle = FxHelper.SpawnFxOnUnit(prefab, view, null, null, default(Vector3), FxPriority.EventuallyImportant);
                if (!Track(hit, handle)) return false;
                hitSpawned++;
                return true;
            }
            catch (Exception e)
            {
                failures++;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteDashFx, "hit-failed", "ghost slash hit effect failed (the swing and the rays are unaffected): " + e);
                return false;
            }
        }
    }
}
