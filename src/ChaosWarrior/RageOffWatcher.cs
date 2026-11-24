using System;
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ChaosWarrior
{
    public static class RageOffWatcher
    {
        public static int Stops { get; private set; }
        static readonly RageWatchRoster<UnitEntityData, ActivatableAbility> roster = new RageWatchRoster<UnitEntityData, ActivatableAbility>();
        static readonly AbilityChanges changes = new AbilityChanges();
        static readonly Action runFrame = RunFrame;
        static readonly Func<UnitEntityData, ActivatableAbility> findAbility = FindAbility;
        static Player player;
        static object area;
        static bool wasLoading;

        sealed class AbilityChanges : IFactCollectionUpdatedHandler
        {
            public void HandleFactCollectionUpdated(EntityFactsProcessor collection)
            {
                // The engine notifies after attaching and just before detaching.
                // Re-read the affected owner's final collection on the next update.
                if (collection is ActivatableAbilityCollection && collection.Manager.Owner is UnitEntityData unit)
                    roster.Invalidate(unit);
            }
        }

        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            if (!Game.HasInstance || Game.Instance.Player == null || Rage.Ability == null || Rage.Buff == null)
            {
                if (player != null) { player = null; area = null; roster.Clear(); }
                return;
            }
            ActionGuard.RunReportOnce(ErrorLedger.SiteRageOff, runFrame, ChaosErrors.Sink);
        }

        static void RunFrame()
        {
            var game = Game.Instance;
            var nextPlayer = game.Player;
            var nextArea = game.LoadedAreaState;
            var lp = LoadingProcess.Instance;
            bool loading = lp == null || lp.IsLoadingInProcess;
            bool newPlayer = !ReferenceEquals(player, nextPlayer);
            if (newPlayer || !ReferenceEquals(area, nextArea) || (wasLoading && !loading))
            {
                if (!EventBus.IsGloballySubscribed(changes)) EventBus.Subscribe(changes);
                if (newPlayer) roster.Clear();
                else roster.InvalidateAll();
                player = nextPlayer;
                area = nextArea;
            }
            wasLoading = loading;
            // 불러오는 동안 PartyAndPets 를 읽으면 엔진이 아직 풀리지 않은 파티 참조마다 「Unit is null! (maybe you load old save ...)」 을 남긴다
            // (Player.UpdateCharacterLists → AddCharacterToLists, 2026-11-20 실플레이 GameLogFull 6줄, 호출 스택 = 이 함수). 불러오기가 끝난 첫 프레임에
            // 위 갈래(wasLoading && !loading)가 명단을 다시 읽으므로 그동안 건너뛴 꺼짐·종료 감지는 그 프레임에 따라잡는다.
            if (loading) return;
            var party = player.PartyAndPets;
            if (party == null) { roster.Clear(); return; }
            roster.SetMembers(party);
            for (int i = 0; i < roster.Count; i++)
            {
                var member = roster[i];
                if (member.Unit == null || member.Unit.Destroyed) continue;
                var ability = member.Ability(findAbility);
                if (ability != null || member.HadRage) StopTurnedOff(member, ability, loading);
            }
        }

        static ActivatableAbility FindAbility(UnitEntityData unit)
        {
            return unit.Descriptor.ActivatableAbilities.GetFact(Rage.Ability);
        }

        static Buff FindRageBuff(UnitEntityData unit)
        {
            var buffs = unit.Descriptor.Buffs.Enumerable;
            for (int i = 0; i < buffs.Count; i++)
            {
                var buff = buffs[i];
                if (ReferenceEquals(buff.Blueprint, Rage.Buff)) return buff;
            }
            return null;
        }

        static void StopTurnedOff(RageWatchRoster<UnitEntityData, ActivatableAbility>.Member member, ActivatableAbility ability, bool loading)
        {
            if (ability != null)
            {
                if (RageOffRule.ShouldStop(ability.IsStarted, ability.IsOn))
                {
                    ability.Stop(false);
                    Stops++;
                }
            }
            // Stop may remove the buff synchronously. Remember its edge even if an
            // external effect removes the ability at the same time.
            var rageBuff = FindRageBuff(member.Unit);
            bool had = member.HadRage, has = rageBuff != null;
            member.HadRage = has;
            if (RageOffRule.RageJustEnded(had, has)) OrphanMutationSweep.RequestAfterRageEnd();
            if (ability == null) return;
            if (RageOffRule.ShouldRefillQuicken(has, has && QuickenRefill.Seen(rageBuff), loading))
                QuickenRefill.RefillNow(member.Unit, rageBuff);
        }
    }
}
