using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic.Buffs;
using WotrClassMod.Common;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ChaosWarrior
{
    // 고아 변이·단계 버프 자동 정리(2026-11-16, 판정 = OrphanMutationRule). modEntry.OnUpdate(Main.Load). 매 프레임 도는 것은 「대기 중인가」 불 하나뿐이고,
    // 실제로 파티·동료·펫을 훑는 것은 (1) 지역 진입·불러오기가 끝난 뒤 1회 (2) 분노가 끝난 직후 1회(RequestAfterRageEnd — RageOffWatcher 가 요청)다. 주기 훑기는 없다.
    // 지운 것은 유닛·버프 이름·종류(변이 / 단계 버프 — OrphanMutationRule.RemovedLine)와 함께 정보 줄로 남긴다. 판정 불가(블루프린트 없는 버프)·지우기 실패는 오류 장부(SiteOrphanSweep)로 시끄럽게.
    public static class OrphanMutationSweep
    {
         
        // 지역 지도가 안정된 첫 프레임에 한 번만 훑는다. 그 외 프레임의 비용 = 불 하나 확인. 결함 수리 뒤 고아가 다시 보이면 새 결함의 신호(그래서 매번 기록).
        sealed class Gate { public bool Pending = true; }   // 게임 시작 뒤 첫 지역도 한 번 훑는다
        static readonly Gate gate = new Gate();
        public static int Removed { get; private set; }
        public static int RageEndRequests { get; private set; }
        // 끝까지 돈 훑기 횟수(독립 검토 R1-I2, 2026-11-17): 게임 안 점검이 「정리가 지운 수 0」을 단언하려면 그 사이 훑기가 실제로 돌았는지 알아야 한다(안 돌았으면 0 은 아무것도 말하지 않는다).
        public static int Sweeps { get; private set; }

        // 분노 종료 직후 1회 더(2026-11-16 독립 검토 B): 분노의 자식 제거가 도중에 끊기면 남은 자식(맨 끝에 붙는 숨김 단계 버프 포함)이 다음 지역 진입까지 분노 없이 살아 있다.
        // 요청만 세우고 실제 훑기는 Tick 의 같은 안정 조건(로딩 아님·지역 지도)에서 한다. 정상 상태에서는 아무것도 지우지 않는다(살아 있는 분노가 저장한 자식은 고아가 아니다 — OrphanMutationRule).
        public static void RequestAfterRageEnd()
        {
            RageEndRequests++;
            gate.Pending = true;
        }

        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            var lp = LoadingProcess.Instance;
            if (lp != null && lp.IsLoadingInProcess) { gate.Pending = true; return; }
            if (!gate.Pending) return;
            // 아직 안정 상태가 아님(게임 없음·메인 메뉴·전역 지도·블루프린트 미빌드) = 다음 프레임에 다시 본다(정당한 대기).
            if (lp == null || !Game.HasInstance || Game.Instance.Player == null || Rage.Buff == null || !Mutations.Built) return;
            var g = Game.Instance;
            if (g.RootUiContext == null || g.RootUiContext.IsMainMenu) return;
            var area = g.CurrentlyLoadedArea;
            if (area == null || area.IsGlobalMap) return;
            var units = g.Player.PartyAndPets;
            if (units == null || units.Count == 0) return;
            gate.Pending = false;
            ActionGuard.RunReportOnce(ErrorLedger.SiteOrphanSweep, () => { Sweep(units.ToList()); Sweeps++; }, ChaosErrors.Sink);
        }

        static void Sweep(List<UnitEntityData> units)
        {
            foreach (var u in units)
            {
                if (u == null || u.Destroyed || u.Buffs == null) continue;
                var buffs = u.Buffs.RawFacts.ToList();
                var stored = new HashSet<EntityFact>();
                foreach (var rage in buffs.Where(b => b != null && b.Blueprint == Rage.Buff && b.IsAttached && !b.IsDisposed))
                {
                    var list = (List<EntityFact>)BuildKit.GetF(rage, "m_StoredFacts"); // 필드가 없으면 GetF 가 던진다(조용히 빈 목록 아님)
                    if (list != null) foreach (var f in list) if (f != null) stored.Add(f);
                }
                var slots = buffs.Select(b => new BuffSlot(b == null || b.Blueprint == null ? null : b.Blueprint.AssetGuid.ToString(), b != null && stored.Contains(b))).ToList();
                int unknown;
                var orphans = OrphanMutationRule.Orphans(slots, out unknown);
                if (unknown > 0) throw new InvalidOperationException(u.CharacterName + " has " + unknown + " buff(s) without a blueprint (cannot judge orphan mutations)");
                foreach (int i in orphans)
                {
                    var b = buffs[i];
                    string name = b.Blueprint.name, guid = slots[i].Guid;
                    b.Remove();
                    if (u.Buffs.RawFacts.Contains(b)) throw new InvalidOperationException("orphan " + OrphanMutationRule.KindOf(guid) + " " + name + " on " + u.CharacterName + " is still attached after Buff.Remove()");
                    Removed++;
                    ModContext.Log("ChaosWarrior: " + OrphanMutationRule.RemovedLine(guid, name, u.CharacterName, Removed));
                }
            }
        }
    }
}
