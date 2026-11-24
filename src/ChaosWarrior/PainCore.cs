using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
     
    // 결과는 `return PainOutcome.X;` 로만(`dial: core literals` 면제 계약). 세계가 계약을 어기면(같은 시전자 인스턴스 2개 등) 던진다 → ActionGuard 가 계수·재던짐.
    // 정정(독립 검토 R3-F1·F5, 2026-11-17): 앞 판(bbfb86d)의 「해제 주문 TryDispelBuff 가 숨김 버프를 건너뛴다 @0059」는 TTT 패치에 기댄 판독이었다 — 본편 Buff.get_HiddenInInspector 는
    //   「BlueprintBuff.get_HiddenInInspector(@0001~0018: 이름이 비었음 ‖ HideFeatureInInspect 부품) ‖ UnitFact.Hidden」이라 이름 있는 숨김 버프는 본편에서 해제된다(TabletopTweaks-Base
    //   Buff_HiddenInInspector_Patch.Postfix 가 그 값을 덮어쓴 설치에서만 건너뛴다). 그래서 총 겹을 「해제 불가 계수 버프」 하나에 둔다 — TryDispelBuff @004E~0053 의 IsNotDispelable
    //   판정은 @0059 보다 먼저이고 이 설치본의 어느 모드도 그 메서드 · 속성을 패치하지 않는다(스캔 2026-11-17: 패치 대상은 RunAction 의 전처리 · 처리기 주입뿐).
    public interface IPainWorld
    {
        bool HasCaster { get; }
        bool HasTarget { get; }
        bool CasterRageActive { get; }
        bool TargetIsEnemyOfCaster { get; }
        int WarpriestLevel { get; }
        int WisdomMod { get; }
        // 시전자에게 붙은 그 변이 표식 인스턴스들의 순위(개편 O7 — 고통 증폭 · 고통 공명은 순위형이라 0개 또는 1개). 빈 목록 = 그 표식이 없음. null = 읽을 수 없음(핵심이 던진다 — 0 으로 바꾸지 않는다).
        // (본편 ContextValue{CasterBuffRank} 는 버프를 못 찾으면 조용히 0 을 준다 — 그래서 쓰지 않고 우리 코드가 직접 읽는다.)
        IReadOnlyList<int> MarkerRanksOnCaster(string mutationId);
        // 이 명중의 공격 굴림에 실린 무기를 읽을 수 있는가(게임: ContextData<ContextAttackData>.Current.AttackRoll.Weapon.Blueprint — 하나라도 null 이면 false).
        bool HitWeaponKnown { get; }
        // 그 무기 블루프린트의 GUID(HitWeaponKnown 일 때만 의미 — 아닐 때 읽으면 안 된다).
        string HitWeaponGuid { get; }
        bool ProbeCanApply(object unit);
        bool SpellImmune(object unit);
        // 대상의 고통 벌점 인스턴스 전부(모든 시전자) + 인스턴스의 시전자 — 시전자별 분리는 핵심이 한다(독립 검토 m1). null = 읽을 수 없음(던진다).
        IReadOnlyList<object> PainInstancesOn(object unit);
        // 대상의 고통 계수 인스턴스 전부(모든 시전자 — 총 겹의 단일 원장, 소유자 2026-11-17). null = 읽을 수 없음(던진다).
        IReadOnlyList<object> CounterInstancesOn(object unit);
        // 대상의 옛 숨김 초과 버프 인스턴스 전부(계수 이전 개발판 세이브 — 새로 만들지 않는다). null = 읽을 수 없음(던진다).
        IReadOnlyList<object> LegacyOverflowInstancesOn(object unit);
        object CasterOf(object inst);
        object Caster { get; }
        // 대상이 아직 살아 있는가(지연 해결 사이 사망 — AddBuff null 의 정당한 원인, 독립 검토 m4).
        bool TargetAlive { get; }
        int RankOf(object inst);
        // 계수 버프를 대상에 붙인다(순위 rank, rounds 라운드). 붙지 않으면 null.
        object AddCounter(object unit, int rank, int rounds);
        // 벌점 버프를 그 계수의 소유자에게 붙이고 계수의 자식으로 저장한다(게임: Buff.StoreFact — 계수가 지워질 때 엔진이 같이 지운다). 붙지 않으면 null.
        object AddPenaltyChild(object counter, int rank, int rounds);
        // 이미 붙은 벌점을 계수의 자식으로 저장한다(이미 저장돼 있으면 무동작).
        void AdoptChild(object counter, object penalty);
        // 그 벌점이 그 계수의 저장 사실인가(게임: 계수의 m_StoredFacts 에 그 인스턴스).
        bool IsChildOf(object penalty, object counter);
        // 그 계수 인스턴스가 해제 주문의 대상에서 빠지는가(게임: Buff.IsNotDispelable — 본편 NotDispelable 부품이 켠다).
        bool IsUndispellable(object counter);
        void SetRank(object inst, int rank);
        void RefreshRounds(object inst, int rounds);
        // 인스턴스를 지운다. 계수를 지우면 엔진이 그 자식(벌점)도 지운다(Buff.OnRemove @0047~0095).
        void RemoveInstance(object inst);
        object Target { get; }
        IReadOnlyList<(object Unit, bool IsEnemyOfCaster, bool Alive, int DistFeet)> Around(object unit, int feet);
        bool SaveWill(object unit, int dc);
        int RollConfusionRounds();
        bool ApplyConfusion(object unit, int rounds);
    }

    public enum PainOutcome { Stacked, Thresholded, Capped, SkippedNotEnemy, DudRageEnded, NoCaster, NoTarget, NoLevel, Failed, DudTargetDead, HitWeaponUnknown }

    public static class PainCore
    {
        public static PainOutcome Hit(IPainWorld w, Action<string, string> report, Action<string, string> dud)
        {
            if (w == null) throw new ArgumentNullException("w");
            if (report == null) throw new ArgumentNullException("report");
            if (dud == null) throw new ArgumentNullException("dud");
            const string site = ErrorLedger.SitePain;
            if (!w.HasCaster) { report(site, "no caster"); return PainOutcome.NoCaster; }
            if (!w.HasTarget) { report(site, "no target"); return PainOutcome.NoTarget; }
             
            if (!w.CasterRageActive) { dud(site, "rage ended before resolve"); return PainOutcome.DudRageEnded; }
            // 판정 2026-11-14: 아군(비적) 직접 명중은 고통 없음 — 무기록 무동작.
            if (!w.TargetIsEnemyOfCaster) return PainOutcome.SkippedNotEnemy;
            int level = w.WarpriestLevel;
            if (level < ChaosRules.Levels.MinLevel) { report(site, "no level (warpriest level " + level + ")"); return PainOutcome.NoLevel; }
            // 개편 O7: 명중 무기를 읽을 수 없으면 「판정 불가」 — 「신경독침 아님」으로 쓰지 않고 이 명중은 쌓지 않는다(계획 A10).
            // 엔진은 직접 · 지연 두 경로 모두 AbstractWeaponTrigger.RunActions @0029~0037 에서 ContextAttackData 를 싣는다(Step 0 (a)(b)) — 그래서 이 줄은 트리거 밖에서 이 행동이 불린 경우에만 나온다.
            if (!w.HitWeaponKnown) { report(site, "hit weapon unreadable (no attack context)"); return PainOutcome.HitWeaponUnknown; }

            object target = w.Target;
            bool immune = MindImmunity.Decide(w.ProbeCanApply(target), w.SpellImmune(target));
            string weapon = w.HitWeaponGuid;
            if (weapon == null) throw new InvalidOperationException("ChaosWarrior: the hit weapon is known but its GUID is null");
            bool neuro = weapon == NaturalWeaponPlan.GuidOf(MutationSpecs.NeuroStingerId);
            int add = PainRule.AddFor(RankOn(w, MutationSpecs.PainAmplifyId), RankOn(w, MutationSpecs.PainResonanceId), neuro);

            // 이 시전자 몫의 세 버프(소유자 2026-11-17 「계수 버프 하나로 재구성」): 계수 = 총 겹의 단일 원장(숨김 · 해제 불가) · 벌점 = 계수의 자식(순위 min(총, 상한), 해제 주문으로 지워질 수 있다) · 옛 초과 = 계수 이전 개발판 세이브의 잔재.
            var penaltyList = w.PainInstancesOn(target);
            if (penaltyList == null) throw new InvalidOperationException("ChaosWarrior: pain instances of the target unreadable");
            var counterList = w.CounterInstancesOn(target);
            if (counterList == null) throw new InvalidOperationException("ChaosWarrior: pain counter instances of the target unreadable");
            var legacyList = w.LegacyOverflowInstancesOn(target);
            if (legacyList == null) throw new InvalidOperationException("ChaosWarrior: legacy pain overflow instances of the target unreadable");
            object penalty = PainLedger.Find(penaltyList, w.CasterOf, w.Caster);
            object counter = PainLedger.Find(counterList, w.CasterOf, w.Caster);
            object legacy = PainLedger.Find(legacyList, w.CasterOf, w.Caster);

            int old;
            bool childOk = true;   // 벌점이 있으면 계수의 저장 사실인가(없으면 참 — 볼 것이 없다)
            if (counter == null)
            {
                if (penalty == null)
                {
                    // 옛 초과만 남음(계수 이전 판에서 벌점이 해제·만료된 상태): 총 겹을 알 수 없다 → 보고, 그 초과를 지우고 이 명중은 쌓지 않는다.
                    if (legacy != null)
                    {
                        report(site, "legacy pain overflow without penalty instance (a save from before the pain counter) — removed, this hit is not stacked");
                        w.RemoveInstance(legacy);
                        return PainOutcome.Failed;
                    }
                    old = 0;
                }
                else
                {
                    // 계수 없는 벌점: 지금 구조에서는 생기지 않는다(벌점은 늘 계수의 자식이라 계수가 지워지면 엔진이 같이 지운다).
                    // 계수 이전 판의 세이브이거나 결함 → 시끄럽게 보고하고, 값은 잃지 않게 그 벌점(+ 옛 초과)에서 총 겹을 되살려 새 계수로 옮긴다(LegacyTotal 이 옛 짝을 검사한다).
                    old = PainRule.LegacyTotal(w.RankOf(penalty), legacy == null ? (int?)null : w.RankOf(legacy));
                    report(site, "penalty without counter (penalty rank " + w.RankOf(penalty) + (legacy == null ? "" : ", legacy overflow rank " + w.RankOf(legacy)) + ") — a save from before the pain counter or a broken pair; moved into a new counter of " + old);
                    childOk = false;
                }
            }
            else
            {
                // 계수와 옛 초과가 같이 있음: 옛 초과는 계수를 만들 때 지운다 — 같이 남는 경로가 없다(계약 위반).
                if (legacy != null) throw new InvalidOperationException("ChaosWarrior: a pain counter and a legacy pain overflow instance from the same caster on one target");
                old = PainRule.CounterTotal(w.RankOf(counter));
                if (penalty != null)
                {
                    int pr = w.RankOf(penalty), wantPr = PainRule.PenaltyRankFor(old);
                    if (pr != wantPr) throw new InvalidOperationException("ChaosWarrior: pain pair mismatch — penalty rank " + pr + " with counter " + old + " (expected " + wantPr + ")");
                    childOk = w.IsChildOf(penalty, counter);
                    if (!childOk) report(site, "pain penalty is not stored by its counter (counter " + old + ") — stored again");
                }
            }
            // 계수는 있는데 벌점이 없음 = 벌점만 해제 주문(또는 다른 제거)으로 지워진 판독 가능한 정상 상태 — 이번 명중에서 계수 기준으로 다시 만든다.
            bool restore = counter != null && penalty == null;

            var step = PainRule.Next(old, add, immune);
            int rounds = ChaosRules.Dials.PainDurationRounds;
            int wantCounter, wantPenalty;

            if (step.Clear)
            {
                // 계수를 지우면 엔진이 자식 벌점을 같이 지운다 — 되읽기가 그 사실을 본다. 자식이 아닌 벌점(옛 세이브 · 저장 끊김)만 직접 지운다.
                // 한 번에 문턱을 건너뛴 첫 명중(0 + add ≥ 문턱)이면 지울 것이 없다.
                if (counter != null) w.RemoveInstance(counter);
                if (penalty != null && !childOk) w.RemoveInstance(penalty);
                if (legacy != null) w.RemoveInstance(legacy);
                wantCounter = 0; wantPenalty = 0;
            }
            else
            {
                wantCounter = step.NewTotal; wantPenalty = PainRule.PenaltyRankFor(step.NewTotal);
                if (counter == null)
                {
                    counter = w.AddCounter(target, wantCounter, rounds);
                     
                    if (counter == null)
                    {
                        if (!w.TargetAlive) { dud(site, "target died before the hit resolved"); return PainOutcome.DudTargetDead; }
                        report(site, "pain counter instance not applied");
                        return PainOutcome.Failed;
                    }
                }
                else
                {
                    w.SetRank(counter, wantCounter);
                    w.RefreshRounds(counter, rounds);
                }
                if (legacy != null) w.RemoveInstance(legacy);   // 옛 초과는 새 계수로 옮겼다
                if (penalty == null)
                {
                    if (w.AddPenaltyChild(counter, wantPenalty, rounds) == null)
                    {
                        if (!w.TargetAlive) { dud(site, "target died before the hit resolved"); return PainOutcome.DudTargetDead; }
                        report(site, "pain instance not applied");
                        return PainOutcome.Failed;
                    }
                }
                else
                {
                    w.SetRank(penalty, wantPenalty);
                    w.RefreshRounds(penalty, rounds);
                    if (!childOk) w.AdoptChild(counter, penalty);
                }
            }

            // 쓴 뒤 되읽기(엔진 Buff.SetRank 는 Ranks 를 넘으면 조용히 자른다 — 정찰 §1.2): 이 시전자의 계수 · 벌점 순위 == 기대(없음 = 0),
            //   벌점이 계수의 자식 · 계수가 해제 불가 · 옛 초과 없음. 하나라도 어긋나면 보고 1회 · Failed(문턱 · 광역 진행 안 함).
            var penaltyNow = PainLedger.Find(Readable(w.PainInstancesOn(target)), w.CasterOf, w.Caster);
            var counterNow = PainLedger.Find(Readable(w.CounterInstancesOn(target)), w.CasterOf, w.Caster);
            var legacyNow = PainLedger.Find(Readable(w.LegacyOverflowInstancesOn(target)), w.CasterOf, w.Caster);
            int gotC = counterNow == null ? 0 : w.RankOf(counterNow), gotP = penaltyNow == null ? 0 : w.RankOf(penaltyNow);
            var problems = new List<string>();
            if (gotC != wantCounter || gotP != wantPenalty) problems.Add("rank readback (counter " + gotC + ", penalty " + gotP + ") vs (" + wantCounter + ", " + wantPenalty + ")");
            if (counterNow != null && penaltyNow != null && !w.IsChildOf(penaltyNow, counterNow)) problems.Add("the penalty is not stored by its counter");
            if (counterNow != null && !w.IsUndispellable(counterNow)) problems.Add("the counter is dispellable");
            if (legacyNow != null) problems.Add("a legacy overflow instance is still attached");
            if (problems.Count > 0)
            {
                report(site, "pain readback: " + string.Join("; ", problems.ToArray()));
                return PainOutcome.Failed;
            }
            if (restore && !step.Clear) dud(site, "pain penalty restored from its counter (" + old + " -> " + step.NewTotal + ") after the penalty was removed");

            if (step.Save)
            {
                Resolve(w, target, level, report, dud);
                if (PainRule.AoeApplies(level))
                {
                    var around = w.Around(target, ChaosRules.Dials.PainAoeFeet);
                    if (around == null) throw new InvalidOperationException("ChaosWarrior: units around the target unreadable");
                    foreach (var a in around)
                    {
                        // 거리도 핵심이 다시 본다(세계의 반경 질의가 넓어도 15 ft 밖은 제외 — 독립 검토 m2).
                        if (!a.IsEnemyOfCaster || !a.Alive || ReferenceEquals(a.Unit, target) || a.DistFeet > ChaosRules.Dials.PainAoeFeet) continue;
                        if (MindImmunity.Decide(w.ProbeCanApply(a.Unit), w.SpellImmune(a.Unit))) continue;
                        Resolve(w, a.Unit, level, report, dud);
                    }
                }
                return PainOutcome.Thresholded;
            }
            if (immune && step.NewTotal == ChaosRules.Dials.PainConfusionThreshold) return PainOutcome.Capped;
            return PainOutcome.Stacked;
        }

        // 그 변이 표식의 순위(순위형 — 인스턴스 0개 = 0, 1개 = 그 순위). 읽을 수 없음(null) · 인스턴스 2개 이상 · 순위 < 1 → 던진다(조용히 0 이나 합으로 넘기지 않는다).
        static int RankOn(IPainWorld w, string mutationId)
        {
            var ranks = w.MarkerRanksOnCaster(mutationId);
            if (ranks == null) throw new InvalidOperationException("ChaosWarrior: " + mutationId + " marker ranks on the caster unreadable");
            if (ranks.Count > 1) throw new InvalidOperationException("ChaosWarrior: " + mutationId + " is rank-mode but the caster has " + ranks.Count + " marker instances");
            if (ranks.Any(r => r < 1)) throw new InvalidOperationException("ChaosWarrior: " + mutationId + " marker has rank below 1");
            return ranks.Count == 0 ? 0 : ranks[0];
        }

        static IReadOnlyList<object> Readable(IReadOnlyList<object> l)
        {
            if (l == null) throw new InvalidOperationException("ChaosWarrior: pain instances of the target unreadable after the write");
            return l;
        }

        static void Resolve(IPainWorld w, object unit, int level, Action<string, string> report, Action<string, string> dud)
        {
            const string site = ErrorLedger.SitePain;
            int dc = ChaosRules.PainDc(level, w.WisdomMod);
            if (w.SaveWill(unit, dc)) return;
            int rounds = w.RollConfusionRounds();
            int min = ChaosRules.Dials.ConfusionDiceCount, max = ChaosRules.Dials.ConfusionDiceCount * ChaosRules.Dials.ConfusionDiceSides;
            if (rounds < min || rounds > max) { report(site, "confusion rounds " + rounds + " outside " + min + ".." + max); return; }
             
            if (!w.ApplyConfusion(unit, rounds)) dud(site, "confusion blocked");
        }
    }
}
