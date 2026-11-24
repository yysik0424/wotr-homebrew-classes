using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
     
    // 우리 인스턴스는 전부 RemoveOnUnequipItem=true → 엔진이 언제든 정상 삭제할 수 있다(OnWillUnequip @00A1~00DA, ItemsEnchantmentController.TickItem @0022~002B).
    // 그래서 사실 부재는 손상이 아니다. 손상은 마지막 사후 조건으로만 판단한다.

    public enum RecordKind { Mirror, Possession }

    public sealed class Record
    {
        public string ItemId, FactId;
        public RecordKind Kind;
        public Record() { }
        public Record(string itemId, string factId, RecordKind kind) { ItemId = itemId; FactId = factId; Kind = kind; }
        public override string ToString() { return ItemId + "/" + FactId + "/" + Kind; }
    }

    public sealed class Ledger
    {
        public List<Record> Applied = new List<Record>();
    }

    // 우리 인스턴스 하나(영병 4세트 무기 위, ParentContext 출처 = 흡수 사실).
    public sealed class OurInstance
    {
        public readonly string ItemId, FactId, Guid;
        public OurInstance(string itemId, string factId, string guid) { ItemId = itemId; FactId = factId; Guid = guid; }
    }

     
    //   ItemNotFound = 다른 유닛 손에도 없음(파괴·파티 밖) / FactAbsent = 다른 유닛이 들었고 기록 사실은 없음(엔진이 지움) / FactPresent = 기록 사실이 아직 붙어 있음.
    public enum HeldByOtherState { ItemNotFound, FactAbsent, FactPresent }

    // 기록 대조(①)·고아 정리(②)에 필요한 효과기 부분 — 거울(IMirrorEffects)과 강신(IPossessionEffects, 계획 T6)이 같은 규칙(LedgerReconcile)을 쓴다(C4).
    public interface IInstanceStore
    {
        bool ItemExists(string itemId);
        bool FactExists(string itemId, string factId);
        void Remove(string itemId, string factId);          // 실패 → throw
        IList<OurInstance> ListOurInstances();
        void Log(string level, string message);
        // ItemExists == false 인 기록에만 부른다. 같은 UniqueId 아이템이 둘 이상이면 throw.
        HeldByOtherState HeldByOtherFact(string itemId, string factId);
        // 다른 유닛 손 아이템에서 원장 기록 사실 id 로만 지운다(우리 사실 — 남의 마법 부여는 건드리지 않는다). 실패 → throw.
        void RemoveHeldByOther(string itemId, string factId);
    }

    public interface IMirrorEffects : IInstanceStore
    {
        string Add(string itemId, string enchantmentGuid);  // 새 인스턴스 사실 id. 실패 → throw (null 반환도 실패로 본다)
        // 계획 입력(영병의 현재 세트·대상·주인 레벨)을 지금 상태에서 만든다. Run 이 우리 인스턴스를 전부 지운 뒤에 부르므로
        // 대상 OwnGuids·OwnEnhancement 에 우리 것이 섞일 수 없다(구성상 보장 — 계획의 PlanInput 인자 대신, 보고서 §4).
        // 효과기 계약(T4): BuildInput 은 우리 인스턴스를 거르지 않아도 된다(대상의 마법 부여를 있는 그대로 읽는다) — Run 이 호출 시점의 우리 인스턴스 0 을 보장한다.
        PlanInput BuildInput();
    }

    public sealed class ReconcileCounts { public int Gone, EngineRemoved, Removed, Orphans; }

    // [순수] 기록 대조 3분류(①) + 고아 정리(②) — MirrorRun(거울)과 PossessionRun(강신, 계획 T6 「MirrorRun 과 같은 기록 대조 규칙」)이 함께 쓴다.
    // 우리 인스턴스는 전부 RemoveOnUnequipItem=true(+강신은 기간) → 사실 부재는 엔진의 정상 삭제(EngineRemoved INFO), 아이템 부재는 Gone INFO.
    // 둘 다 있으면 Remove → 성공한 기록만 즉시 원장에서 뺀다(Remove 가 던지면 그 기록은 남는다 = 실제 상태). ① 은 던지지 않고 끝나므로 ② 는 항상 도달.
     
    // WARN 줄 뒤 사실 id 로 제거하고 Removed 로 센다(로그 줄 형식은 그대로). 없으면 지금처럼 Gone INFO.
    public static class LedgerReconcile
    {
        public static void Validate(Ledger ledger, RecordKind kind, string owner)
        {
            if (ledger == null || ledger.Applied == null) throw new ArgumentNullException("ledger", owner + ": ledger (or its list) is null — corrupt part");
            foreach (var r in ledger.Applied)
            {
                if (r == null || string.IsNullOrEmpty(r.ItemId) || string.IsNullOrEmpty(r.FactId)) throw new InvalidOperationException(owner + ": ledger has an incomplete record (" + r + ")");
                if (r.Kind != kind) throw new InvalidOperationException(owner + ": ledger record " + r + " is not a " + kind + " record");
            }
        }

        // what = 로그 머리("mirror" / "possession"). 줄 모양: "<what> record gone (item missing): …", "<what> record engine-removed (fact missing): …", "<what> orphan removed: …".
        public static ReconcileCounts Clear(IInstanceStore fx, Ledger ledger, RecordKind kind, string what)
        {
            if (fx == null) throw new ArgumentNullException("fx");
            if (string.IsNullOrEmpty(what)) throw new ArgumentException("LedgerReconcile: log prefix is empty");
            Validate(ledger, kind, "LedgerReconcile(" + what + ")");
            var c = new ReconcileCounts();
            foreach (var r in ledger.Applied.ToList())
            {
                if (!fx.ItemExists(r.ItemId))
                {
                    var st = fx.HeldByOtherFact(r.ItemId, r.FactId);
                    if (st == HeldByOtherState.FactPresent)
                    {
                        fx.Log("WARN", what + " record on an item held by another unit still has our fact (engine did not remove it) — removing by ledger fact id: " + r);
                        fx.RemoveHeldByOther(r.ItemId, r.FactId);
                        ledger.Applied.Remove(r);
                        c.Removed++;
                        continue;
                    }
                    if (st != HeldByOtherState.ItemNotFound && st != HeldByOtherState.FactAbsent) throw new InvalidOperationException("LedgerReconcile: undefined HeldByOtherState " + (int)st);
                    c.Gone++; fx.Log("INFO", what + " record gone (item missing): " + r); ledger.Applied.Remove(r); continue;
                }
                if (!fx.FactExists(r.ItemId, r.FactId)) { c.EngineRemoved++; fx.Log("INFO", what + " record engine-removed (fact missing): " + r); ledger.Applied.Remove(r); continue; }
                fx.Remove(r.ItemId, r.FactId);
                ledger.Applied.Remove(r);
                c.Removed++;
            }
            foreach (var o in fx.ListOurInstances().ToList())
            {
                if (ledger.Applied.Any(r => r.ItemId == o.ItemId && r.FactId == o.FactId)) continue;
                fx.Log("WARN", what + " orphan removed: " + o.ItemId + "/" + o.FactId + " " + o.Guid);
                fx.Remove(o.ItemId, o.FactId);
                c.Orphans++;
            }
            return c;
        }
    }

    public sealed class RunReport
    {
        public int Gone, EngineRemoved, Removed, Orphans, Added;
        public AbsorbPlan Plan;
    }

    public static class MirrorRun
    {
        public static RunReport Run(IMirrorEffects fx, Ledger ledger)
        {
            if (fx == null) throw new ArgumentNullException("fx");
            LedgerReconcile.Validate(ledger, RecordKind.Mirror, "MirrorRun");
            var rep = new RunReport();

            // ① 기록 대조 3분류 + ② 고아 정리(원장에 없는 우리 인스턴스) — 공유 규칙(LedgerReconcile, 강신 PossessionRun 도 같은 것).
            var c = LedgerReconcile.Clear(fx, ledger, RecordKind.Mirror, "mirror");
            rep.Gone = c.Gone; rep.EngineRemoved = c.EngineRemoved; rep.Removed = c.Removed; rep.Orphans = c.Orphans;

            // ③ 계획 + 불변식. 입력은 정리 뒤 상태에서 만든다(우리 인스턴스 0 확인 후) — 대상 자기 목록에 우리 것이 섞이면 AlreadyOnTarget 으로 조용히 빠지기 때문.
            if (fx.ListOurInstances().Count != 0) throw new InvalidOperationException("MirrorRun: our instances remain after cleanup");
            var input = fx.BuildInput();
            if (input == null) throw new InvalidOperationException("MirrorRun: BuildInput returned null");
            var plan = AbsorbPlanner.Plan(input);
            AbsorbPlanner.CheckInvariants(plan);
            rep.Plan = plan;

            // ④ 추가 — 하나 추가할 때마다 즉시 원장에 append(부분 실패 시 원장 = 실제 상태; 다음 Run 이 수렴).
            foreach (var g in plan.AllGuids())
            {
                var fact = fx.Add(plan.TargetItemId, g);
                if (string.IsNullOrEmpty(fact)) throw new InvalidOperationException("MirrorRun: Add returned no fact for " + g + " on " + plan.TargetItemId);
                ledger.Applied.Add(new Record(plan.TargetItemId, fact, RecordKind.Mirror));
                rep.Added++;
            }

            // ⑤ 사후 조건(손상 검출은 여기): 대상의 우리 GUID 집합 == 계획, 대상 밖 우리 인스턴스 0, 원장 == 실제.
            var ours = fx.ListOurInstances();
            var onTarget = ours.Where(o => o.ItemId == plan.TargetItemId).Select(o => o.Guid).OrderBy(x => x, StringComparer.Ordinal).ToList();
            var want = plan.AllGuids().OrderBy(x => x, StringComparer.Ordinal).ToList();
            var elsewhere = ours.Where(o => o.ItemId != plan.TargetItemId).ToList();
            var actualPairs = new HashSet<string>(ours.Select(o => o.ItemId + "/" + o.FactId));
            var ledgerPairs = new HashSet<string>(ledger.Applied.Select(r => r.ItemId + "/" + r.FactId));
            if (!onTarget.SequenceEqual(want) || elsewhere.Count != 0 || !actualPairs.SetEquals(ledgerPairs) || actualPairs.Count != ours.Count || ledgerPairs.Count != ledger.Applied.Count)
                throw new InvalidOperationException("mirror post-condition: target ours [" + string.Join(",", onTarget) + "] vs plan [" + string.Join(",", want) + "], elsewhere " + elsewhere.Count
                    + ", actual " + actualPairs.Count + " vs ledger " + ledgerPairs.Count);
            return rep;
        }
    }
}
