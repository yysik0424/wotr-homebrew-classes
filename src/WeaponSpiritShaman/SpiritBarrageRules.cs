using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
    // [순수] 스피릿 배러지 / Spirit Barrage 규칙(소유자 결정 2026-11-16, 설계 docs\2026-11-08-weapon-spirit-shaman-design.md §16,
     
    // 수치는 전부 SpiritRules.Dials 에서 읽는다(18레벨·하루 1회·최대 6회·신속 행동). 30피트·살아 있음·보임 조건은 강신 규칙(PossessionRules.Observe)을 그대로 쓴다 — 여기에 복제하지 않는다.
    // 게임 쪽(SpiritBarrage.cs)은 스피릿 웨폰의 예비 칸 6개를 SpareSlot 으로 만들어 넣고, 결과(누가 공격하고 누가 왜 빠지는지)만 따른다.

    // 예비 칸 하나에 든 것. 판정 기준(게임 쪽 SpiritBarrage.Spares): 아이템 없음 = Empty · 방패 아이템 = Shield · 무기이고 원거리(AttackType Ranged·RangedTouch) = RangedWeapon
    // · 그 밖의 무기 = MeleeWeapon · 무기도 방패도 아닌 아이템 = NotWeapon.
    public enum SpareKind { Empty, MeleeWeapon, RangedWeapon, Shield, NotWeapon }

    public sealed class SpareSlot
    {
        public int SetIndex;     // 0..3, 활성 세트가 아님
        public bool IsPrimary;   // 주무기 칸 = true, 보조 칸 = false
        public SpareKind Kind;
        public string ItemId;    // Empty 면 null, 그 밖에는 아이템 UniqueId(필수)
        public string Name;      // 기록용 블루프린트 이름(Empty 면 null)
        // 그 무기에 「장착 해제 때 지워지는」 마법 부여가 있음. 임시 장착 처리(정찰 §2 방법 A)의 해제가 그것을 지우므로 그런 무기는 참여시키지 않는다.
        public bool Volatile;

        public string Label { get { return "set" + SetIndex + (IsPrimary ? "/main" : "/off"); } }
    }

    // 칸이 공격에 빠진 사유. Empty·Ranged·Shield·NotWeapon = 소유자 결정의 네 가지, VolatileEnchant = 상태 보존(위), OverLimit = 상한 초과, OutOfReach = 그 무기가 대상에 닿지 않음,
    // ResidualOnEquip = 그 무기를 장착했다 해제하면 스피릿 웨폰에 무언가 남는다(사전 왕복으로 확인 — Residue).
    public enum SpiritBarrageSkip { Empty, Ranged, Shield, NotWeapon, VolatileEnchant, OverLimit, OutOfReach, ResidualOnEquip }

    public sealed class SpiritBarrageSkipped
    {
        public readonly SpareSlot Slot;
        public readonly SpiritBarrageSkip Reason;
        public SpiritBarrageSkipped(SpareSlot slot, SpiritBarrageSkip reason) { Slot = slot; Reason = reason; }
    }

    public sealed class SpiritBarragePlan
    {
        public readonly SpareSlot[] Attacks;      // 세트 번호 → 주·보조 순서
        public readonly SpiritBarrageSkipped[] Skipped;   // 같은 순서
        public SpiritBarragePlan(SpareSlot[] attacks, SpiritBarrageSkipped[] skipped) { Attacks = attacks; Skipped = skipped; }
    }

    // 스피릿 웨폰의 상태 지문(게임 쪽 SpiritBarrage.Fingerprint 가 만든다). 임시 장착 왕복(장착 → 해제, 공격 없음) 전후에 떠서 Residue 로 대조한다.
    public sealed class SpiritFingerprint
    {
        public string[] Facts;                       // 사실 인스턴스마다 "블루프린트 GUID#랭크"(버프 포함 전부, 순서 무관 — 개수로 센다)
        public string[] Buffs;                       // 버프 인스턴스마다 "블루프린트 GUID#랭크"
        public KeyValuePair<string, int>[] Stats;    // (수치 이름, 수정치 반영 값) — 능력치 6·AC·명중·내성 3·이동 속도·HP 최대치 등
        public int ActiveSet;                        // 활성 무기 세트 번호
        public string[] SlotItemIds;                 // 손 칸 8개의 아이템 id(빈 칸 = null)
    }

    // 예비 칸 판정 3상태(C1): 참여 무기 1개 이상 / 0개 / 판정 불가(스피릿 웨폰을 찾을 수 없음). Unknown 은 NoMelee 가 아니다.
    public enum SpareVerdict { HasMelee, NoMelee, Unknown }

    // 사용 불가 사유. Spirit = 강신과 같은 조건이 깨짐(SpiritBarrageCheck.SpiritReason 이 무엇인지 말한다) · Unknown = 판정 불가(차단).
    public enum SpiritBarrageBlock { None, Spirit, NoUses, NoMeleeSpare, Unknown }

    public sealed class SpiritBarrageCheck
    {
        public readonly SpiritBarrageBlock Block;
        public readonly ActivateBlock SpiritReason; // Block == Spirit 일 때만 뜻이 있다
        public SpiritBarrageCheck(SpiritBarrageBlock block, ActivateBlock spiritReason) { Block = block; SpiritReason = spiritReason; }
        public bool Ok { get { return Block == SpiritBarrageBlock.None; } }
    }

    // 공격 1회의 결과. TargetDown = 앞선 공격으로 대상이 이미 쓰러져 굴리지 않음.
    public enum StrikeOutcome { Hit, Critical, Miss, TargetDown }

    public static class SpiritBarrageRules
    {
        // 예비 칸 수 = 활성 아닌 세트 × (주·보조). AbsorbPlanner.SetCount(세트 4개, 2026-11-08 정찰 §4.2)에서 파생한다.
        public const int SpareSlotCount = (AbsorbPlanner.SetCount - 1) * 2;

        // 얻었는가(레벨 게이트). 범위 밖 레벨은 던진다(0 을 「레벨 0」으로 읽지 않는다 — SpiritRules.Budget 과 같은 검사).
        public static bool Unlocked(int shamanLevel)
        {
            SpiritRules.Budget(shamanLevel); // 범위 검사(1..20, 밖이면 throw)
            return shamanLevel >= SpiritRules.Dials.SpiritBarrageLevel;
        }

        // 그 주술사 레벨의 하루 횟수(얻기 전 = 0).
        public static int MaxUses(int shamanLevel)
        {
            return Unlocked(shamanLevel) ? SpiritRules.Dials.SpiritBarrageUsesPerDay : 0;
        }

        public static SpiritBarragePlan Plan(IList<SpareSlot> spares) { return Plan(spares, SpiritRules.Dials.SpiritBarrageMaxAttacks); }

        // 참여 무기 고르기. 입력 = 예비 칸 정확히 SpareSlotCount 개(세트 3개 × 주·보조, 빈 칸도 Empty 로 넣는다 — 빠진 칸을 「빈 칸」으로 읽지 않는다).
        public static SpiritBarragePlan Plan(IList<SpareSlot> spares, int maxAttacks)
        {
            if (spares == null) throw new ArgumentNullException("spares");
            if (maxAttacks < 1) throw new ArgumentOutOfRangeException("maxAttacks", maxAttacks, "SpiritBarrageRules.Plan: the attack limit must be at least 1");
            if (spares.Count != SpareSlotCount) throw new ArgumentException("SpiritBarrageRules.Plan: need exactly " + SpareSlotCount + " spare slots (got " + spares.Count + ")");
            var seen = new HashSet<int>();
            foreach (var s in spares)
            {
                if (s == null) throw new ArgumentException("SpiritBarrageRules.Plan: a spare slot is null");
                if (s.SetIndex < 0 || s.SetIndex >= AbsorbPlanner.SetCount) throw new ArgumentOutOfRangeException("spares", "SpiritBarrageRules.Plan: set index " + s.SetIndex + " out of 0.." + (AbsorbPlanner.SetCount - 1));
                if (!seen.Add(s.SetIndex * 2 + (s.IsPrimary ? 0 : 1))) throw new ArgumentException("SpiritBarrageRules.Plan: slot " + s.Label + " listed twice");
                if (!Enum.IsDefined(typeof(SpareKind), s.Kind)) throw new ArgumentException("SpiritBarrageRules.Plan: slot " + s.Label + " has undefined kind " + (int)s.Kind);
                if (s.Kind == SpareKind.Empty) { if (s.ItemId != null) throw new ArgumentException("SpiritBarrageRules.Plan: slot " + s.Label + " is Empty but names item " + s.ItemId); }
                else if (string.IsNullOrEmpty(s.ItemId)) throw new ArgumentException("SpiritBarrageRules.Plan: slot " + s.Label + " holds a " + s.Kind + " without an item id");
            }
            int sets = spares.Select(s => s.SetIndex).Distinct().Count();
            if (sets != AbsorbPlanner.SetCount - 1) throw new ArgumentException("SpiritBarrageRules.Plan: spare slots span " + sets + " sets, expected " + (AbsorbPlanner.SetCount - 1) + " (every set but the active one)");

            var attacks = new List<SpareSlot>();
            var skipped = new List<SpiritBarrageSkipped>();
            foreach (var s in InSlotOrder(spares))
            {
                switch (s.Kind)
                {
                    case SpareKind.Empty: skipped.Add(new SpiritBarrageSkipped(s, SpiritBarrageSkip.Empty)); break;
                    case SpareKind.RangedWeapon: skipped.Add(new SpiritBarrageSkipped(s, SpiritBarrageSkip.Ranged)); break;
                    case SpareKind.Shield: skipped.Add(new SpiritBarrageSkipped(s, SpiritBarrageSkip.Shield)); break;
                    case SpareKind.NotWeapon: skipped.Add(new SpiritBarrageSkipped(s, SpiritBarrageSkip.NotWeapon)); break;
                    case SpareKind.MeleeWeapon:
                        if (s.Volatile) skipped.Add(new SpiritBarrageSkipped(s, SpiritBarrageSkip.VolatileEnchant));
                        else if (attacks.Count >= maxAttacks) skipped.Add(new SpiritBarrageSkipped(s, SpiritBarrageSkip.OverLimit));
                        else attacks.Add(s);
                        break;
                    default: throw new ArgumentException("SpiritBarrageRules.Plan: unhandled kind " + s.Kind);
                }
            }
            return new SpiritBarragePlan(attacks.ToArray(), skipped.ToArray());
        }

        static IEnumerable<SpareSlot> InSlotOrder(IEnumerable<SpareSlot> slots) { return slots.OrderBy(s => s.SetIndex * 2 + (s.IsPrimary ? 0 : 1)); }

        // 계획 → 3상태. plan == null = 스피릿 웨폰(의 예비 칸)을 읽을 수 없었다 = 판정 불가 — 「0개」로 읽지 않는다(C1).
        public static SpareVerdict Availability(SpiritBarragePlan plan)
        {
            if (plan == null) return SpareVerdict.Unknown;
            if (plan.Attacks == null || plan.Skipped == null) throw new ArgumentException("SpiritBarrageRules.Availability: plan without attack / skip lists");
            return plan.Attacks.Length > 0 ? SpareVerdict.HasMelee : SpareVerdict.NoMelee;
        }

        // 사용 가능 판정. spirit = PossessionRules.Observe(강신과 같은 조건 + 이 능력의 남은 횟수), null = 관측 불가.
        // 강신 버프 상태는 입력이 아니다(강신 중에도 쓸 수 있다). 융합 중에는 스피릿 웨폰이 숨어 있어 spirit 이 PetHidden 으로 막는다.
        public static SpiritBarrageCheck Decide(ActivateCheck spirit, SpareVerdict spares)
        {
            if (!Enum.IsDefined(typeof(SpareVerdict), spares)) throw new ArgumentException("SpiritBarrageRules.Decide: undefined spare verdict " + (int)spares);
            if (spirit == null) return new SpiritBarrageCheck(SpiritBarrageBlock.Unknown, ActivateBlock.None);
            if (!spirit.Ok)
            {
                if (spirit.Reason == ActivateBlock.None) throw new ArgumentException("SpiritBarrageRules.Decide: spirit check is not ok but gives no reason");
                return spirit.Reason == ActivateBlock.NoUses ? new SpiritBarrageCheck(SpiritBarrageBlock.NoUses, ActivateBlock.None) : new SpiritBarrageCheck(SpiritBarrageBlock.Spirit, spirit.Reason);
            }
            switch (spares)
            {
                case SpareVerdict.HasMelee: return new SpiritBarrageCheck(SpiritBarrageBlock.None, ActivateBlock.None);
                case SpareVerdict.NoMelee: return new SpiritBarrageCheck(SpiritBarrageBlock.NoMeleeSpare, ActivateBlock.None);
                default: return new SpiritBarrageCheck(SpiritBarrageBlock.Unknown, ActivateBlock.None);
            }
        }

        // 대상에 닿는 무기만 남긴다. inReach[i] = plan.Attacks[i] 가 대상에 닿는가(게임 쪽이 무기마다 엔진 판정으로 잰다). 닿지 않는 무기 = OutOfReach.
        public static SpiritBarragePlan Aim(SpiritBarragePlan plan, IList<bool> inReach) { return Exclude(plan, inReach, SpiritBarrageSkip.OutOfReach); }

        // 잔류가 없는 무기만 남긴다. clean[i] = plan.Attacks[i] 의 장착 왕복이 아무것도 남기지 않았는가. 남긴 무기 = ResidualOnEquip.
        public static SpiritBarragePlan Screen(SpiritBarragePlan plan, IList<bool> clean) { return Exclude(plan, clean, SpiritBarrageSkip.ResidualOnEquip); }

        // keep[i] 가 거짓인 공격을 reason 사유로 뺀다(칸 순서 유지). 답의 개수가 공격 수와 다르면 던진다(빠진 답을 「통과」로 읽지 않는다).
        static SpiritBarragePlan Exclude(SpiritBarragePlan plan, IList<bool> keep, SpiritBarrageSkip reason)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (keep == null) throw new ArgumentNullException("keep");
            if (keep.Count != plan.Attacks.Length) throw new ArgumentException("SpiritBarrageRules: " + keep.Count + " answers (" + reason + ") for " + plan.Attacks.Length + " attacks");
            var attacks = new List<SpareSlot>();
            var skipped = plan.Skipped.ToList();
            for (int i = 0; i < plan.Attacks.Length; i++)
            {
                if (keep[i]) attacks.Add(plan.Attacks[i]);
                else skipped.Add(new SpiritBarrageSkipped(plan.Attacks[i], reason));
            }
            return new SpiritBarragePlan(attacks.ToArray(), skipped.OrderBy(s => s.Slot.SetIndex * 2 + (s.Slot.IsPrimary ? 0 : 1)).ToArray());
        }

        // 지문 두 개 → 차이 목록(빈 목록 = 잔류 없음). 장착 왕복 전(before)과 후(after).
        // 입력 계약(C2): 두 지문 모두 목록이 있어야 하고, 수치 이름 집합·칸 수가 같아야 한다 — 한쪽에 없는 수치를 「같다」로 읽지 않고 던진다.
        public static List<string> Residue(SpiritFingerprint before, SpiritFingerprint after)
        {
            CheckFingerprint(before, "before");
            CheckFingerprint(after, "after");
            var diff = new List<string>();
            CountDiff("fact", before.Facts, after.Facts, diff);
            CountDiff("buff", before.Buffs, after.Buffs, diff);
            var a = new Dictionary<string, int>();
            foreach (var kv in after.Stats) a.Add(kv.Key, kv.Value);
            if (a.Count != before.Stats.Length || before.Stats.Any(kv => !a.ContainsKey(kv.Key)))
                throw new ArgumentException("SpiritBarrageRules.Residue: the two fingerprints list different stats (" + before.Stats.Length + " vs " + after.Stats.Length + ")");
            foreach (var kv in before.Stats)
                if (a[kv.Key] != kv.Value) diff.Add("stat " + kv.Key + " " + kv.Value + " -> " + a[kv.Key]);
            if (before.ActiveSet != after.ActiveSet) diff.Add("active set " + before.ActiveSet + " -> " + after.ActiveSet);
            if (before.SlotItemIds.Length != after.SlotItemIds.Length) throw new ArgumentException("SpiritBarrageRules.Residue: the two fingerprints list different slot counts");
            for (int i = 0; i < before.SlotItemIds.Length; i++)
                if (before.SlotItemIds[i] != after.SlotItemIds[i]) diff.Add("slot " + i + " " + (before.SlotItemIds[i] ?? "empty") + " -> " + (after.SlotItemIds[i] ?? "empty"));
            return diff;
        }

        static void CheckFingerprint(SpiritFingerprint f, string name)
        {
            if (f == null) throw new ArgumentNullException(name);
            if (f.Facts == null || f.Buffs == null || f.Stats == null || f.SlotItemIds == null) throw new ArgumentException("SpiritBarrageRules.Residue: fingerprint '" + name + "' has a missing list");
            if (f.Stats.Length == 0) throw new ArgumentException("SpiritBarrageRules.Residue: fingerprint '" + name + "' has no stats (nothing would be compared)");
            if (f.Facts.Any(string.IsNullOrEmpty) || f.Buffs.Any(string.IsNullOrEmpty) || f.Stats.Any(kv => string.IsNullOrEmpty(kv.Key))) throw new ArgumentException("SpiritBarrageRules.Residue: fingerprint '" + name + "' has an empty entry");
            if (f.Stats.Select(kv => kv.Key).Distinct().Count() != f.Stats.Length) throw new ArgumentException("SpiritBarrageRules.Residue: fingerprint '" + name + "' lists a stat twice");
        }

        // 여러 번 나올 수 있는 항목(사실·버프)을 개수로 대조한다: 늘면 "+", 줄면 "-".
        static void CountDiff(string what, string[] before, string[] after, List<string> diff)
        {
            var count = new Dictionary<string, int>();
            foreach (var x in after) { int c; count.TryGetValue(x, out c); count[x] = c + 1; }
            foreach (var x in before) { int c; count.TryGetValue(x, out c); count[x] = c - 1; }
            foreach (var kv in count.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (kv.Value > 0) diff.Add(what + " +" + kv.Key + " x" + kv.Value);
                else if (kv.Value < 0) diff.Add(what + " -" + kv.Key + " x" + (-kv.Value));
            }
        }

        static string OutcomeWord(StrikeOutcome o)
        {
            switch (o)
            {
                case StrikeOutcome.Hit: return "hit";
                case StrikeOutcome.Critical: return "crit";
                case StrikeOutcome.Miss: return "miss";
                case StrikeOutcome.TargetDown: return "target-down";
                default: throw new ArgumentException("SpiritBarrageRules: undefined strike outcome " + (int)o);
            }
        }

        static string SkipWord(SpiritBarrageSkip s)
        {
            switch (s)
            {
                case SpiritBarrageSkip.Empty: return "empty";
                case SpiritBarrageSkip.Ranged: return "ranged";
                case SpiritBarrageSkip.Shield: return "shield";
                case SpiritBarrageSkip.NotWeapon: return "not-weapon";
                case SpiritBarrageSkip.VolatileEnchant: return "volatile-enchant";
                case SpiritBarrageSkip.OverLimit: return "over-limit";
                case SpiritBarrageSkip.OutOfReach: return "out-of-reach";
                case SpiritBarrageSkip.ResidualOnEquip: return "residual-on-equip";
                default: throw new ArgumentException("SpiritBarrageRules: undefined skip reason " + (int)s);
            }
        }

        static void CheckOutcomes(SpiritBarragePlan aimed, IList<StrikeOutcome> outcomes)
        {
            if (aimed == null) throw new ArgumentNullException("aimed");
            if (outcomes == null) throw new ArgumentNullException("outcomes");
            if (outcomes.Count != aimed.Attacks.Length) throw new ArgumentException("SpiritBarrageRules: " + outcomes.Count + " outcomes for " + aimed.Attacks.Length + " attacks");
        }

        // 모드 로그 한 줄: 공격한 무기와 결과, 빠진 칸과 사유 전부.
        public static string Line(string shaman, string spirit, string target, SpiritBarragePlan aimed, IList<StrikeOutcome> outcomes)
        {
            if (shaman == null) throw new ArgumentNullException("shaman");
            if (spirit == null) throw new ArgumentNullException("spirit");
            if (target == null) throw new ArgumentNullException("target");
            CheckOutcomes(aimed, outcomes);
            var a = aimed.Attacks.Select((s, i) => s.Label + " " + s.Name + " " + OutcomeWord(outcomes[i]));
            var k = aimed.Skipped.Select(s => s.Slot.Label + " " + SkipWord(s.Reason) + (s.Slot.Kind == SpareKind.Empty ? "" : " " + s.Slot.Name));
            return "WSS barrage " + shaman + ": spirit " + spirit + " -> " + target + ": " + aimed.Attacks.Length + " attack(s) [" + string.Join("; ", a.ToArray())
                + "]; left out " + aimed.Skipped.Length + " [" + string.Join("; ", k.ToArray()) + "]";
        }

        // 전투 기록 한 줄(현지화 틀): {0} = 굴린 공격 수, {1} = 명중 수(치명타 포함), {2} = 빠진 칸 수, {3} = 사유별 개수(없으면 none).
        public static string Summary(string template, string none, Func<SpiritBarrageSkip, string> word, SpiritBarragePlan aimed, IList<StrikeOutcome> outcomes)
        {
            if (string.IsNullOrEmpty(template)) throw new ArgumentNullException("template");
            if (string.IsNullOrEmpty(none)) throw new ArgumentNullException("none");
            if (word == null) throw new ArgumentNullException("word");
            CheckOutcomes(aimed, outcomes);
            foreach (var o in outcomes) OutcomeWord(o); // 정의되지 않은 결과 = throw
            int rolled = outcomes.Count(o => o != StrikeOutcome.TargetDown);
            int hits = outcomes.Count(o => o == StrikeOutcome.Hit || o == StrikeOutcome.Critical);
            var reasons = new List<string>();
            foreach (SpiritBarrageSkip r in Enum.GetValues(typeof(SpiritBarrageSkip)))
            {
                int n = aimed.Skipped.Count(s => s.Reason == r);
                if (n == 0) continue;
                string w = word(r);
                if (string.IsNullOrEmpty(w)) throw new InvalidOperationException("SpiritBarrageRules.Summary: no word for skip reason " + r);
                reasons.Add(w + " x" + n);
            }
            return string.Format(template, rolled, hits, aimed.Skipped.Length, reasons.Count == 0 ? none : string.Join(", ", reasons.ToArray()));
        }
    }
}
