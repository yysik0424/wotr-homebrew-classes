using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
    // [순수] 강신 발동·종료 절차 + 기록 대조(계획 T6 「PossessionPart」, 설계 §4.7, 판정 Q2 = 자체 파트 + 기간 마법 부여 + RemoveOnUnequipItem).
    // 게임 효과기(Possession.cs GamePossessionEffects)와 시험 가짜 세계가 IPossessionEffects 를 구현한다. 원장 기록 대조·고아 정리는 거울과 같은 LedgerReconcile(C4).
    //  발동 = ① 남은 기록·고아 정리 → 우리 인스턴스 0 확인 → 주술사 대상(주무기, 비었으면 맨손) → 영병의 지금 PlanInput(거울과 같은 게임 쪽 입력 생성기)을
    //         강신 경로(Path = Possession, Carrier = 주술사 무기)로 AbsorbPlanner.Plan → CheckInvariants
    //         → PossessionRules.CopySet(대상 자기 GUID 건너뜀) → 하나 추가할 때마다 즉시 원장 append → 사후 조건.
    //  종료 = 버프 OnDeactivate 에서 ① 정리만(엔진이 이미 지운 것 = EngineRemoved INFO, 아이템 없음 = Gone INFO) → 사후 조건(우리 인스턴스 0, 원장 0).

    public sealed class PossessionTarget
    {
        public TargetKind Kind;       // Primary(주무기) · EmptyHand(맨손) · NonWeapon(주 손에 무기 아닌 것 — 얹지 않음). Secondary 는 쓰지 않는다(PossessionRules.ShamanTarget).
        public string ItemId;
        public string[] OwnGuids;     // 대상의 지금 마법 부여 GUID(우리 강신 인스턴스는 발동 ① 뒤라 0)
        public CarrierWeapon Carrier; // 대상 무기의 엔진 값(트리거 무기 조건 대조 — Kind 는 위 Kind 와 같다). 게임 쪽 = GameMirrorEffects.CarrierOf
    }

    public interface IPossessionEffects : IInstanceStore
    {
        // 영병의 지금 계획 입력(거울 GameMirrorEffects.BuildInput 과 같은 생성기 — 대상 자기 목록에서는 거울 인스턴스를 뺀다: 비친 집합 = 계획 결과).
        PlanInput BuildSpiritInput();
        PossessionTarget ShamanTarget();
        string Add(string itemId, string enchantmentGuid, int rounds); // 새 인스턴스 사실 id. 실패 → throw (null 반환도 실패로 본다)
    }

    public sealed class PossessionReport
    {
        public int Gone, EngineRemoved, Removed, Orphans, Added, Rounds;
        public AbsorbPlan Plan;          // 발동만
        public PossessionTarget Target;  // 발동만
        public CopyResult Copy;          // 발동만
    }

    // [순수] 강신 인스턴스 소유 판정(거울 MirrorScope.IsOurs 와 같은 방식). 우리 것 = 부모 문맥 AssociatedBlueprint 가 강신 효과 버프 I~III 중 하나인 마법 부여.
    // 놓인 자리·찾는 범위는 MirrorScope.Classify / ListsOurInstances / Searchable 을 그대로 쓴다(「영병 손」 자리 = 강신에서는 주술사 손).
    public static class PossessionScope
    {
        // 2026-11-18 융합 제거: 융합 버프는 더 이상 강신 복사를 하지 않는다(컴포넌트 없음) → 대상은 강신 효과 버프 I~III 뿐.
        static readonly string[] OurBuffGuids = { Guids.PossessionBuff1, Guids.PossessionBuff2, Guids.PossessionBuff3 };

        public static bool IsOurs(string parentContextBlueprintGuid)
        {
            return parentContextBlueprintGuid != null && Array.IndexOf(OurBuffGuids, parentContextBlueprintGuid) >= 0;
        }

        public static IList<string> BuffGuids { get { return Array.AsReadOnly(OurBuffGuids); } }
    }

    public static class PossessionRun
    {
        const string What = "possession";

        public static PossessionReport Activate(IPossessionEffects fx, Ledger ledger, int rounds)
        {
            if (fx == null) throw new ArgumentNullException("fx");
            if (rounds < 1) throw new ArgumentOutOfRangeException("rounds", rounds, "PossessionRun: possession lasts at least 1 round");
            LedgerReconcile.Validate(ledger, RecordKind.Possession, "PossessionRun");
            var rep = Cleared(fx, ledger);
            rep.Rounds = rounds;
            if (fx.ListOurInstances().Count != 0) throw new InvalidOperationException("PossessionRun: our possession instances remain after cleanup");

            // 2026-11-18 흡수 범위 확대(소유자 결정): 강신은 거울 계획을 복사하지 않고, 영병의 같은 원천으로 강신 경로 계획(AbsorbPath.Possession)을 따로 세운다 —
            // 장비 효과형·소지자 의존형은 강신에서만 허용되고, 트리거 무기 조건은 주술사 무기와 대조한다(그래서 주술사 대상을 먼저 읽는다).
            var target = fx.ShamanTarget();
            if (target == null || string.IsNullOrEmpty(target.ItemId) || target.OwnGuids == null || target.Carrier == null) throw new InvalidOperationException("PossessionRun: ShamanTarget returned an incomplete target");
            if (!Enum.IsDefined(typeof(TargetKind), target.Kind) || target.Kind == TargetKind.Secondary) throw new InvalidOperationException("PossessionRun: shaman target kind " + target.Kind + " is not Primary/EmptyHand/NonWeapon");
            if (target.Carrier.Kind != target.Kind) throw new InvalidOperationException("PossessionRun: shaman target carrier kind " + target.Carrier.Kind + " != target kind " + target.Kind);
            rep.Target = target;

            var spirit = fx.BuildSpiritInput();
            if (spirit == null) throw new InvalidOperationException("PossessionRun: BuildSpiritInput returned null");
            if (spirit.Path != AbsorbPath.Mirror) throw new InvalidOperationException("PossessionRun: BuildSpiritInput path is " + spirit.Path + " (the spirit's mirror input is expected)");
            var input = new PlanInput { ShamanLevel = spirit.ShamanLevel, ActiveSet = spirit.ActiveSet, Sources = spirit.Sources, Target = spirit.Target, Path = AbsorbPath.Possession, Carrier = target.Carrier };
            var plan = AbsorbPlanner.Plan(input);
            AbsorbPlanner.CheckInvariants(plan);
            rep.Plan = plan;
            if (target.Kind == TargetKind.NonWeapon)
            {
                // 무기 아닌 것에는 무기 마법 부여가 실체화되지 않는다(거울 T4 m2 와 같은 규칙) — 얹지 않고 알린다(사용 횟수는 이미 썼다).
                rep.Copy = new CopyResult { Copy = new string[0], Skipped = new string[0] };
                fx.Log("WARN", What + " target is not a weapon (" + target.ItemId + ") — nothing copied");
            }
            else rep.Copy = PossessionRules.CopySet(plan, target.OwnGuids);

            foreach (var g in rep.Copy.Copy)
            {
                var fact = fx.Add(target.ItemId, g, rounds);
                if (string.IsNullOrEmpty(fact)) throw new InvalidOperationException("PossessionRun: Add returned no fact for " + g + " on " + target.ItemId);
                ledger.Applied.Add(new Record(target.ItemId, fact, RecordKind.Possession));
                rep.Added++;
            }

            // 사후 조건: 대상의 우리 GUID 집합 == 복사 집합, 대상 밖 우리 인스턴스 0, 원장 == 실제.
            var ours = fx.ListOurInstances();
            var onTarget = ours.Where(o => o.ItemId == target.ItemId).Select(o => o.Guid).OrderBy(x => x, StringComparer.Ordinal).ToList();
            var want = rep.Copy.Copy.OrderBy(x => x, StringComparer.Ordinal).ToList();
            var elsewhere = ours.Count(o => o.ItemId != target.ItemId);
            if (!onTarget.SequenceEqual(want) || elsewhere != 0 || !LedgerEqualsActual(ours, ledger))
                throw new InvalidOperationException("possession post-condition: target ours [" + string.Join(",", onTarget) + "] vs copy [" + string.Join(",", want) + "], elsewhere " + elsewhere
                    + ", actual " + ours.Count + " vs ledger " + ledger.Applied.Count);
            return rep;
        }

        public static PossessionReport End(IPossessionEffects fx, Ledger ledger)
        {
            if (fx == null) throw new ArgumentNullException("fx");
            LedgerReconcile.Validate(ledger, RecordKind.Possession, "PossessionRun");
            var rep = Cleared(fx, ledger);
            var left = fx.ListOurInstances();
            if (left.Count != 0 || ledger.Applied.Count != 0)
                throw new InvalidOperationException("possession post-condition (end): our instances " + left.Count + ", ledger " + ledger.Applied.Count + " (both must be 0)");
            return rep;
        }

        static PossessionReport Cleared(IPossessionEffects fx, Ledger ledger)
        {
            var c = LedgerReconcile.Clear(fx, ledger, RecordKind.Possession, What);
            return new PossessionReport { Gone = c.Gone, EngineRemoved = c.EngineRemoved, Removed = c.Removed, Orphans = c.Orphans };
        }

        static bool LedgerEqualsActual(IList<OurInstance> ours, Ledger ledger)
        {
            var actual = new HashSet<string>(ours.Select(o => o.ItemId + "/" + o.FactId));
            var led = new HashSet<string>(ledger.Applied.Select(r => r.ItemId + "/" + r.FactId));
            return actual.SetEquals(led) && actual.Count == ours.Count && led.Count == ledger.Applied.Count;
        }

        // 로그 한 줄(설계 §14 Q6 — 무엇을 얹었고 무엇을 왜 건너뛰었는지). nameOf = GUID → 이름(게임 쪽 블루프린트 이름).
        public static string Line(string shamanName, string phase, PossessionReport rep, Func<string, string> nameOf)
        {
            if (rep == null) throw new ArgumentNullException("rep");
            if (nameOf == null) throw new ArgumentNullException("nameOf");
            string led = "ledger(gone=" + rep.Gone + ",engine=" + rep.EngineRemoved + ",removed=" + rep.Removed + ",orphan=" + rep.Orphans + ")";
            if (rep.Target == null) return "WSS possession " + phase + " " + shamanName + " " + led;
            return "WSS possession " + phase + " " + shamanName + " rounds=" + rep.Rounds + " target=" + rep.Target.Kind
                + " copied[" + string.Join(",", rep.Copy.Copy.Select(nameOf).ToArray()) + "] skipped[" + string.Join(",", rep.Copy.Skipped.Select(nameOf).ToArray()) + "] "
                + led + " added=" + rep.Added;
        }
    }
}
