using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
    // [순수] 흡수 계획기(계획 T2 「AbsorbPlanner」, 설계 §4.5). 게임 쪽은 영병 몸·무기를 아래 DTO 로 만들어 넣고 결과만 적용한다.

    public sealed class SourceEnch
    {
        public EnchShape Shape;
        public int? EnhancementBonus;   // 검사기 판정 Enhancement 일 때만 값(WeaponEnhancementBonus.EnhancementBonus), 그 밖은 null
        public bool RemoveOnUnequip;     // 인스턴스의 RemoveOnUnequipItem (임시 강화 — 비활성 세트에서는 엔진이 지운다, 설계 §4.5-3)
    }

    public sealed class SourceWeapon
    {
        public int SetIndex;             // 0..3, 활성 세트가 아님
        public bool IsPrimary;
        public string ItemId;
        public SourceEnch[] Enchantments; // 아이템 목록 순서 그대로
    }

    // 대상 = 활성 세트 주무기 → 없으면 보조 → 없으면 맨손 무기(설계 §4.5 「대상」). 맨손도 아이템 엔티티(ItemId 필수).
    // NonWeapon = 고른 칸에 무기가 아닌 것(방패 등)이 있음 — 무기 마법 부여가 실체화되지 않으므로 아무것도 비추지 않는다(리뷰 T4 m2, MirrorScope.ChooseTarget).
    public enum TargetKind { Primary, Secondary, EmptyHand, NonWeapon }

    public sealed class TargetInfo
    {
        public TargetKind Kind;
        public string ItemId;
        public string[] OwnGuids;   // 대상 자신의 마법 부여 GUID(우리 인스턴스 제외 — MirrorRun 이 확인)
        public int OwnEnhancement;  // 대상 자신의 강화 값(우리 인스턴스 제외)
    }

    // 대상 아이템 위 WeaponEnhancementBonus 컴포넌트 하나(어느 마법 부여에 붙었든 — 판정 분류와 무관).
    public sealed class WeaponEnhComponent
    {
        public int Bonus;   // WeaponEnhancementBonus.EnhancementBonus
        public bool Stack;  // WeaponEnhancementBonus.Stack
    }

    // 계획 경로(소유자 결정 2026-11-18 — 거울과 강신이 받는 범위가 다르다). Unset = 채우지 않은 입력 → Plan 이 던진다(어느 쪽으로도 조용히 읽지 않는다).
    //  Mirror     = 영병 무기에 비침(착용자 = 영병 펫): 무기 규칙형 · 강화 · 트리거형.
    //  Possession = 주술사 무기로 옮김(착용자 = 주술사): 위 + 장비 효과형(EquipmentEffect) + 소지자 의존형(WielderDependent).
    public enum AbsorbPath { Unset, Mirror, Possession }

    public sealed class PlanInput
    {
        public int ShamanLevel;
        public int ActiveSet;
        public SourceWeapon[] Sources;
        public TargetInfo Target;
        public AbsorbPath Path;
        // 마법 부여를 실제로 얹을 무기(트리거 무기 조건 대조). 거울 = Target 그 무기(Kind 가 같아야 한다), 강신 = 주술사 대상 무기(PossessionTarget.Carrier).
        public CarrierWeapon Carrier;
    }

    public enum RejectReason
    {
        // 검사기 분류(Accepted·Enhancement·Trigger 아닌 것). WielderDependent·EquipmentEffect 는 거울에서만 거부(강신은 허용).
        Empty, MixedEnhancement, NotMagic, Unabsorbable, TriggerNotPortable, WielderDependent, ContextValueDependent, HelperOnly, EquipmentEffect, EquipmentNotPortable,
        // 계획기 규칙
        Temporary, Duplicate, AlreadyOnTarget, FixedReserved, EnhancementNotHigher, Overflow,
        // 발동형 상한(SpiritRules.Dials.MaxTriggersPerPlan, 소유자 결정 2026-11-18)
        TriggerCapReached,
    }

    public sealed class Rejection
    {
        public readonly string Guid, Name, Detail;
        public readonly RejectReason Reason;
        public Rejection(string guid, string name, RejectReason reason, string detail) { Guid = guid; Name = name; Reason = reason; Detail = detail; }
        public override string ToString() { return Name + ":" + Reason; }
    }

    public sealed class AbsorbPlan
    {
        public int Budget;
        public TargetKind TargetKind;
        public string TargetItemId;
        public string EnhancementGuid;   // null = 강화 비추지 않음
        public int EnhancementValue;     // 0 = 없음
        public string[] Specials;        // 칸 순서
        public string[] Fixed;           // 예산 0
        public Rejection[] Rejected;     // 처리 순서

        // 대상에 놓일 우리 인스턴스 GUID 전부(사후 조건·강신 복사용).
        public IEnumerable<string> AllGuids()
        {
            if (EnhancementGuid != null) yield return EnhancementGuid;
            foreach (var g in Specials) yield return g;
            foreach (var g in Fixed) yield return g;
        }
    }

    public static class AbsorbPlanner
    {
        public const int SetCount = 4; // UnitBody.HandsEquipmentSets 4개 (정찰 §4.2)

         
        // 출처(Assembly-CSharp IL, 2026-11-09 직독):
        //   GameHelper.GetItemEnhancementBonus(ItemEntityWeapon) @0001~0083: Enchantments.SelectMany(SelectComponents<WeaponEnhancementBonus>) — 마법 부여 분류 없이 전부,
        //     Stack 으로 둘로 나눔(b__34_1/b__34_2), @008F~00AF 비중첩 = Max, @00C0~00E0 중첩 = Sum, @00E6~00EE min(Max + Sum, 5).
        //   전투 경로 WeaponEnhancementBonus.OnEventAboutToTrigger(RuleCalculateWeaponStats) @0011~0031: Modifier(값, Descriptor) → EnhancementValue.AddModifier →
        //     ModifiableBonus.Add @0013~0067: 중첩 아닌 같은 서술자는 큰 값만 남김. get_Descriptor @0000~000D: Stack ? UntypedStackable(25) : Enhancement(9),
        //     ModifierDescriptorHelper..cctor 중첩 집합 = {0,1,2,25,30,36,37,33,24,16,53} — Enhancement(9) 없음.
        // 우리 표준 EnhancementN(비중첩)과 겨루는 것은 비중첩 최댓값뿐이고, 중첩 값은 우리 것과 무관하게 더해진다 → 비교에서 뺀다.
        // 음수 최댓값은 0 으로 둔다: 같은 서술자에서 우리 +1..5 가 언제나 이기므로(ModifiableBonus.Add @0049~0055 큰 값) 비교 결과가 0 과 같다.
        public static int EngineOwnEnhancement(IEnumerable<WeaponEnhComponent> components)
        {
            if (components == null) throw new ArgumentNullException("components");
            int best = 0;
            foreach (var c in components)
            {
                if (c == null) throw new ArgumentException("AbsorbPlanner.EngineOwnEnhancement: null component");
                if (!c.Stack && c.Bonus > best) best = c.Bonus;
            }
            return best;
        }

        public static AbsorbPlan Plan(PlanInput input)
        {
            if (input == null) throw new ArgumentNullException("input");
            int budget = SpiritRules.Budget(input.ShamanLevel); // 범위 밖 throw
            if (input.ActiveSet < 0 || input.ActiveSet >= SetCount) throw new ArgumentOutOfRangeException("input", "AbsorbPlanner: active set " + input.ActiveSet + " out of 0.." + (SetCount - 1));
            if (input.Sources == null) throw new ArgumentException("AbsorbPlanner: Sources is null (no sources = empty array)");
            var t = input.Target;
            if (t == null) throw new ArgumentException("AbsorbPlanner: Target is null (empty hand is TargetKind.EmptyHand, not null)");
            if (!Enum.IsDefined(typeof(TargetKind), t.Kind)) throw new ArgumentException("AbsorbPlanner: undefined TargetKind " + (int)t.Kind);
            if (string.IsNullOrEmpty(t.ItemId)) throw new ArgumentException("AbsorbPlanner: target ItemId missing");
            if (t.OwnGuids == null) throw new ArgumentException("AbsorbPlanner: target OwnGuids is null");
            if (t.OwnEnhancement < 0) throw new ArgumentException("AbsorbPlanner: target OwnEnhancement < 0");
            if (input.Path != AbsorbPath.Mirror && input.Path != AbsorbPath.Possession) throw new ArgumentException("AbsorbPlanner: plan path is " + input.Path + " (Mirror or Possession required)");
            var carrier = input.Carrier;
            if (carrier == null) throw new ArgumentException("AbsorbPlanner: carrier weapon is null (a shield/other item is CarrierWeapon{Kind = NonWeapon}, not null)");
            carrier.Validate("AbsorbPlanner");
            if (input.Path == AbsorbPath.Mirror && carrier.Kind != t.Kind) throw new ArgumentException("AbsorbPlanner: mirror carrier kind " + carrier.Kind + " != target kind " + t.Kind + " (the mirror carrier is the target itself)");
            if (input.Path == AbsorbPath.Possession && carrier.Kind == TargetKind.Secondary) throw new ArgumentException("AbsorbPlanner: possession carrier is never the off hand (PossessionRules.ShamanTarget)");
            var own = new HashSet<string>(t.OwnGuids);

            var slots = new HashSet<string>();
            foreach (var s in input.Sources)
            {
                if (s == null) throw new ArgumentException("AbsorbPlanner: null source weapon");
                if (s.SetIndex < 0 || s.SetIndex >= SetCount) throw new ArgumentException("AbsorbPlanner: source set " + s.SetIndex + " out of range");
                if (s.SetIndex == input.ActiveSet) throw new InvalidOperationException("AbsorbPlanner: source " + s.ItemId + " is in the active set " + input.ActiveSet + " (active set weapons are the target, not sources)");
                if (string.IsNullOrEmpty(s.ItemId)) throw new ArgumentException("AbsorbPlanner: source ItemId missing");
                if (s.ItemId == t.ItemId) throw new InvalidOperationException("AbsorbPlanner: source item " + s.ItemId + " is the target item");
                if (!slots.Add(s.SetIndex + (s.IsPrimary ? "P" : "S"))) throw new ArgumentException("AbsorbPlanner: two sources in set " + s.SetIndex + (s.IsPrimary ? " primary" : " secondary"));
                if (s.Enchantments == null) throw new ArgumentException("AbsorbPlanner: source " + s.ItemId + " Enchantments is null");
            }

            // ② 원천 정렬 = 세트 번호 오름차순, 주 → 보조, 아이템 목록 순(설계 §4.5 채우는 순서 1). 안정 정렬.
            var ordered = input.Sources.Select((s, i) => new { s, i }).OrderBy(x => x.s.SetIndex).ThenBy(x => x.s.IsPrimary ? 0 : 1).ThenBy(x => x.i).Select(x => x.s).ToList();

            var plan = new AbsorbPlan { Budget = budget, TargetKind = t.Kind, TargetItemId = t.ItemId };
            if (t.Kind == TargetKind.NonWeapon)
            {
                // 리뷰 T4 m2: 무기 아닌 대상(ItemEntityShield.get_InstantiateEnchantments = false)에 붙여도 효과가 없다 → 고유 속성 포함 0개. 로그가 대상 종류를 남긴다(MirrorLog).
                plan.Fixed = new string[0];
                plan.Specials = new string[0];
                plan.Rejected = new Rejection[0];
                CheckInvariants(plan);
                return plan;
            }
            var rejected = new List<Rejection>();
            // ① Fixed 예약: 대상에 없는 고유 속성은 예산 0 으로 비춘다; 원천 후보에서 그 GUID 는 전부 FixedReserved.
            var fixedSet = new HashSet<string>(SpiritRules.FixedGuids);
            plan.Fixed = SpiritRules.FixedGuids.Where(g => !own.Contains(g)).ToArray();

            int bestE = 0;
            var enhCandidates = new List<Rejection>(); // (guid,name) 보관용 — 거부 표시에 재사용
            var enhValues = new List<int>();           // enhCandidates 와 같은 순서의 강화 값(채택 후보 = 최댓값의 첫 후보)
            var specials = new List<string>();
            var seen = new HashSet<string>();
            int triggers = 0; // 놓인 발동형(Verdict.Trigger) 수
            foreach (var src in ordered)
                foreach (var e in src.Enchantments)
                {
                    if (e == null || e.Shape == null) throw new ArgumentException("AbsorbPlanner: source " + src.ItemId + " has a null enchantment/shape");
                    var g = e.Shape.Guid;
                    var name = e.Shape.Name ?? g;
                    if (fixedSet.Contains(g)) { rejected.Add(new Rejection(g, name, RejectReason.FixedReserved, null)); continue; }
                    // ③ 임시 강화
                    if (e.RemoveOnUnequip) { rejected.Add(new Rejection(g, name, RejectReason.Temporary, null)); continue; }
                    // ④ 검사기
                    var v = EnchantInspector.Inspect(e.Shape);
                    if (v.Verdict == Verdict.Enhancement)
                    {
                        if (!e.EnhancementBonus.HasValue) throw new ArgumentException("AbsorbPlanner: enhancement " + name + " has no EnhancementBonus value");
                        if (e.EnhancementBonus.Value < 1) throw new ArgumentException("AbsorbPlanner: enhancement " + name + " value " + e.EnhancementBonus.Value + " < 1");
                        if (e.EnhancementBonus.Value > bestE) bestE = e.EnhancementBonus.Value;
                        enhCandidates.Add(new Rejection(g, name, RejectReason.EnhancementNotHigher, null));
                        enhValues.Add(e.EnhancementBonus.Value);
                        continue;
                    }
                    if (e.EnhancementBonus.HasValue) throw new ArgumentException("AbsorbPlanner: " + name + " is " + v.Verdict + " but carries an EnhancementBonus value");
                    if (!Allowed(v.Verdict, input.Path)) { rejected.Add(new Rejection(g, name, ToReason(v.Verdict), v.Reason)); continue; }
                    // ⑤-2 트리거 무기 조건 ↔ 얹을 무기(Carrier) 대조 — 돌지 않으면 TriggerNotPortable(칸을 쓰지 않는다).
                    var fit = TriggerFit.Check(e.Shape, carrier);
                    if (!fit.Ok) { rejected.Add(new Rejection(g, name, RejectReason.TriggerNotPortable, fit.Detail)); continue; }
                    // ⑥ 특수: 칸 순서, 같은 GUID 두 번째부터 Duplicate, 대상에 있으면 AlreadyOnTarget, 발동형 상한(Dials.MaxTriggersPerPlan) TriggerCapReached, B 개 초과 Overflow.
                    if (!seen.Add(g)) { rejected.Add(new Rejection(g, name, RejectReason.Duplicate, null)); continue; }
                    if (own.Contains(g)) { rejected.Add(new Rejection(g, name, RejectReason.AlreadyOnTarget, null)); continue; }
                    bool isTrigger = v.Verdict == Verdict.Trigger;
                    if (isTrigger && triggers >= SpiritRules.Dials.MaxTriggersPerPlan) { rejected.Add(new Rejection(g, name, RejectReason.TriggerCapReached, "at most " + SpiritRules.Dials.MaxTriggersPerPlan + " on-hit trigger enchantment(s)")); continue; }
                    if (specials.Count >= budget) { rejected.Add(new Rejection(g, name, RejectReason.Overflow, null)); continue; }
                    if (isTrigger) triggers++;
                    specials.Add(g);
                }

            // ⑤ 강화 = 후보 최대 E, 적용 min(E, B, 5); 대상 자기 강화 이하이면 효과 없음(비중첩 — 설계 §4.5-5) → 후보 전부 EnhancementNotHigher.
            if (bestE > 0)
            {
                int applied = Math.Min(bestE, Math.Min(budget, SpiritRules.MaxStandardEnhancement));
                if (applied <= t.OwnEnhancement) rejected.AddRange(enhCandidates);
                else
                {
                    plan.EnhancementValue = applied; plan.EnhancementGuid = SpiritRules.EnhancementGuid(applied);
                     
                    // Detail "not higher than the adopted +E"(E = 채택 후보 값, 상한으로 깎인 적용 값이 아니다). 처리 순서 그대로 뒤에 붙인다. 불변식과 무관(Rejected 는 검사 대상 아님).
                    int adopted = enhValues.IndexOf(bestE);
                    if (adopted < 0) throw new InvalidOperationException("AbsorbPlanner: best enhancement " + bestE + " not among the candidates");
                    for (int k = 0; k < enhCandidates.Count; k++)
                        if (k != adopted) rejected.Add(new Rejection(enhCandidates[k].Guid, enhCandidates[k].Name, RejectReason.EnhancementNotHigher, "not higher than the adopted +" + bestE));
                }
            }
            plan.Specials = specials.ToArray();
            plan.Rejected = rejected.ToArray();
            CheckInvariants(plan);
            return plan;
        }

        // 경로별 허용(소유자 결정 2026-11-18): 두 경로 = Accepted · Trigger, 강신만 = EquipmentEffect(착용자 = 주술사) · WielderDependent(소지자 = 주술사 — 막은 이유가 「펫이 소지자」).
        public static bool Allowed(Verdict v, AbsorbPath path)
        {
            if (path != AbsorbPath.Mirror && path != AbsorbPath.Possession) throw new ArgumentException("AbsorbPlanner.Allowed: path " + path);
            if (v == Verdict.Accepted || v == Verdict.Trigger) return true;
            return path == AbsorbPath.Possession && (v == Verdict.EquipmentEffect || v == Verdict.WielderDependent);
        }

        static RejectReason ToReason(Verdict v)
        {
            switch (v)
            {
                case Verdict.Empty: return RejectReason.Empty;
                case Verdict.MixedEnhancement: return RejectReason.MixedEnhancement;
                case Verdict.NotMagic: return RejectReason.NotMagic;
                case Verdict.Unabsorbable: return RejectReason.Unabsorbable;
                case Verdict.TriggerNotPortable: return RejectReason.TriggerNotPortable;
                case Verdict.WielderDependent: return RejectReason.WielderDependent;
                case Verdict.ContextValueDependent: return RejectReason.ContextValueDependent;
                case Verdict.HelperOnly: return RejectReason.HelperOnly;
                case Verdict.EquipmentEffect: return RejectReason.EquipmentEffect;
                case Verdict.EquipmentNotPortable: return RejectReason.EquipmentNotPortable;
                default: throw new ArgumentException("AbsorbPlanner: verdict " + v + " is not a rejection");
            }
        }

        // 불변식(위반 throw): 특수 ≤ B, 강화 ≤ min(B,5) 이고 GUID 가 표준 표와 일치, Specials·Fixed·Enhancement 간 GUID 중복 0, Fixed ⊆ FixedGuids.
        public static void CheckInvariants(AbsorbPlan p)
        {
            if (p == null) throw new ArgumentNullException("p");
            if (p.Specials == null || p.Fixed == null || p.Rejected == null) throw new InvalidOperationException("AbsorbPlan invariant: null list");
            if (p.Budget < SpiritRules.Budget(SpiritRules.MinShamanLevel) || p.Budget > SpiritRules.Budget(SpiritRules.MaxShamanLevel)) throw new InvalidOperationException("AbsorbPlan invariant: budget " + p.Budget + " out of range");
            if (p.Specials.Length > p.Budget) throw new InvalidOperationException("AbsorbPlan invariant: " + p.Specials.Length + " specials > budget " + p.Budget);
            int cap = Math.Min(p.Budget, SpiritRules.MaxStandardEnhancement);
            if (p.EnhancementValue < 0 || p.EnhancementValue > cap) throw new InvalidOperationException("AbsorbPlan invariant: enhancement " + p.EnhancementValue + " > min(B,5) = " + cap);
            if ((p.EnhancementValue == 0) != (p.EnhancementGuid == null)) throw new InvalidOperationException("AbsorbPlan invariant: enhancement value/guid mismatch");
            if (p.EnhancementValue > 0 && p.EnhancementGuid != SpiritRules.EnhancementGuid(p.EnhancementValue)) throw new InvalidOperationException("AbsorbPlan invariant: enhancement guid is not the standard Enhancement" + p.EnhancementValue);
            var all = p.AllGuids().ToList();
            if (all.Distinct().Count() != all.Count) throw new InvalidOperationException("AbsorbPlan invariant: duplicate GUID among enhancement/specials/fixed (" + string.Join(",", all) + ")");
            if (p.Fixed.Any(g => !SpiritRules.FixedGuids.Contains(g))) throw new InvalidOperationException("AbsorbPlan invariant: fixed list has a non-fixed GUID");
        }
    }
}
