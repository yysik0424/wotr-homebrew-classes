using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using TurnBased.Controllers;

namespace ShadowSorcerer
{
     
    // 전투 회차(r2 2.5 — CombatState 에 시작 시각 필드 없음): 자체 CombatEpoch 를 파티 전투 시작(IPartyCombatHandler.HandlePartyCombatStateChanged(true))마다 +1. 전투 밖 = RoundRule.OutOfCombatEpoch.
    public static class RoundLedger
    {
        static readonly Dictionary<string, RoundStamp> last = new Dictionary<string, RoundStamp>();
        static long epoch;
        static CombatHook hook;

        public static long CombatEpoch { get { return epoch; } }
        public static int Count { get { return last.Count; } }

        // EchoQueue.Tick 이 게임이 준비된 뒤 1번 부른다. 구독 되읽기 실패 = 오류(조용히 회차가 안 바뀌지 않는다).
        public static void EnsureSubscribed()
        {
            if (hook != null) return;
            hook = new CombatHook();
            EventBus.Subscribe(hook);
            if (!EventBus.IsGloballySubscribed(hook)) ShadowErrors.Report(ErrorLedger.SiteEchoQueue, "party combat subscription did not register - combat epochs will not advance");
        }

        public static bool Subscribed { get { return hook != null; } }

        public static RoundFacts Facts(UnitEntityData caster)
        {
            if (caster == null) throw new ArgumentNullException("caster");
            var game = Game.Instance;
            var f = new RoundFacts();
            if (game == null || game.Player == null || game.TimeController == null) return f; // 결측 → RoundRule Unknown
            f.CombatEpoch = game.Player.IsInCombat ? epoch : RoundRule.OutOfCombatEpoch;
            bool tb = CombatController.IsInTurnBasedCombat();
            f.TurnBased = tb;
            if (tb && game.TurnBasedCombatController != null) f.RoundNumber = game.TurnBasedCombatController.RoundNumber;
            f.GameTime = game.TimeController.GameTime.TotalSeconds;
            RoundStamp s;
            f.Last = last.TryGetValue(caster.UniqueId, out s) ? s : new RoundStamp();
            return f;
        }

        public static void Commit(UnitEntityData caster, RoundFacts f) { last[caster.UniqueId] = RoundRule.Commit(f); }

        public static void Clear() { last.Clear(); }





        sealed class CombatHook : IPartyCombatHandler
        {
            public void HandlePartyCombatStateChanged(bool inCombat) { if (inCombat) epoch++; }
        }
    }
}
