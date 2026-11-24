using System;
using System.Collections;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
     
    // 빌드 시(Repair.Build) Resolve 가 원본을 대조하고 어긋나면 던진다. 게임 중 실패는 수리를 되돌리지 않고 경고(머리말 RepairFxTable).
    public static class RepairFx
    {
        // 원천 프리팹을 지연 생성 우선순위로(선례 PackWarden AppearFxPriority — Important 는 같은 호출 안에서 동기 생성되어 예외가 수리 경로로 새어 나올 수 있다).
        internal const FxPriority Priority = FxPriority.EventuallyImportant;

        static PrefabLink link;

        public static string Resolve()
        {
            if (link != null) return link.AssetId;
            var p = RepairFxTable.Problems(OwnBlueprints.Select(b => b.AssetGuid.ToString()));
            if (p.Count != 0) throw new InvalidOperationException("Golemancer: RepairFxTable invalid: " + string.Join(" | ", p.ToArray()));
            var b = Get<BlueprintBuff>(RepairFxTable.SourceGuid);
            if (b.name != RepairFxTable.SourceName) throw new InvalidOperationException("Golemancer: repair FX source " + RepairFxTable.SourceGuid + " is '" + b.name + "', RepairFxTable says '" + RepairFxTable.SourceName + "'");
            string got = b.FxOnStart == null ? null : b.FxOnStart.AssetId;
            if (string.IsNullOrEmpty(got)) throw new InvalidOperationException("Golemancer: repair FX source " + b.name + " " + RepairFxTable.Slot + " is empty");
            if (got != RepairFxTable.AssetId) throw new InvalidOperationException("Golemancer: repair FX source " + b.name + " " + RepairFxTable.Slot + " = " + got + ", RepairFxTable says " + RepairFxTable.AssetId);
            link = new PrefabLink { AssetId = got }; // 본편 PrefabLink 인스턴스를 공유하지 않는다
            return got;
        }

        // 수리 능력의 미리 읽기 목록(본편 관례 ResourceAssetIds).
        public static string[] ResourceIds() { return new[] { Resolve() }; }

        public static float LifetimeSeconds { get { return (float)new Rounds(RepairFxTable.LifetimeRounds).Seconds.TotalSeconds; } }

        // 수리 성공 뒤(ContextActionGolemRepair). 반환 = FX 를 띄웠는가. 예외를 던지지 않는다 — 수리는 이미 끝났다. 못 띄운 이유는 전부 기록한다.
        public static bool Play(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (link == null) throw new InvalidOperationException("Golemancer: RepairFx.Play before Repair.Build resolved the FX");
            var part = GolemMaster.PartOf(master);
            try
            {
                var fusion = GolemMaster.CurrentFusion(master);
                // 합체 중(골렘을 거둠) = 골렘을 입은 주인에게(2026-11-15 판정, RepairFxTable.TargetOf). 모르는 값은 던진다 → 아래 catch 가 WARN.
                string target = RepairFxTable.TargetOf(fusion == FusionState.Idle);
                UnitEntityData unit;
                if (target == RepairFxTable.TargetGolem) unit = GolemMaster.GolemOf(master, part.GolemId);
                else if (target == RepairFxTable.TargetMaster) unit = master;
                else throw new InvalidOperationException("RepairFxTable.TargetOf returned '" + target + "'");
                string who = target + " " + unit.UniqueId + " (fusion " + fusion + ")";
                if (unit.Destroyed) { part.Warn("emergency repair FX skipped: " + who + " is destroyed"); return false; }
                var view = unit.View;
                if (view == null) { part.Warn("emergency repair FX skipped: " + who + " has no view"); return false; }
                var prefab = link.Load(false, false);
                if (prefab == null) { part.Warn("emergency repair FX skipped: prefab " + link.AssetId + " did not load"); return false; }
                var handle = FxHelper.SpawnFxOnUnit(prefab, view, master.IsPlayerFaction, null, Vector3.zero, Priority);
                if (handle == null) { part.Warn("emergency repair FX skipped: FxHelper.SpawnFxOnUnit returned null for " + who + " (DisableAllFx on the unit, or view detached)"); return false; }
                CoroutineRunner.Start(EndAfter(handle, LifetimeSeconds), ex => part.Warn("emergency repair FX end failed: " + ex.GetType().Name + ": " + ex.Message));
                part.Info("emergency repair FX " + link.AssetId + " on " + who + " for " + LifetimeSeconds + " s");
                return true;
            }
            catch (Exception ex)
            {
                part.Warn("emergency repair FX skipped: " + ex.GetType().Name + " from the FX system: " + ex.Message);
                return false;
            }
        }

        // 수명 끝 = 엔진이 버프 FX 를 지울 때와 같은 호출(Buff.ClearParticleEffect → FxHelper.Destroy(handle, false)). 이미 지워졌으면(골렘 View 가 먼저 사라짐) 엔진이 무동작으로 처리한다(Destroy @0004 IsDestroyed).
        static IEnumerator EndAfter(IFxHandle handle, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            FxHelper.Destroy(handle, false);
        }
    }
}
