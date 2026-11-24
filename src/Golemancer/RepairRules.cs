using System;
using System.Collections.Generic;
using System.Linq;

namespace Golemancer
{
    // [순수] T7 분리 단계 판정(owner approval 2026-11-14): 응급 수리(설계 §4.7) · 20레벨 면역 공유(설계 §4.8). 과부하는 소유자가 뺐다(「과부하 빼기」).
    // 게임 형식 없음 → 콘솔 시험이 직접 실행한다(modinfo testSources). 게임 쪽(Repair.cs·Capstone.cs)은 관측값을 넣고 결과만 적용한다.

    // 응급 수리를 못 쓰는 사유. Unknown = 관측 결측(판정 불가 — C1: 결측을 「됨/안 됨」으로 읽지 않는다, 막는다).
    public enum RepairBlock { None, Unknown, NoGolem, FusionBusy, NothingToRepair, NoSlot, NotAdjacent }

    // 추출물 슬롯 한 칸의 관측(엔진 SpellSlot: Available · SpellShell != null · IsMainSlot — 반대 학파 2칸 추출물은 주 칸 1개로 센다).
    public struct SlotView
    {
        public readonly bool Available, HasSpell, IsMain;
        // 그 칸 추출물의 학파(엔진 SpellSchool 이름). null = 읽지 않았거나 추출물 없음 — 학파 조건으로 고를 때 null 인 쓸 수 있는 칸을 만나면 던진다(2026-11-16 골렘 폭주).
        public readonly string School;
        public SlotView(bool available, bool hasSpell, bool isMain, string school = null) { Available = available; HasSpell = hasSpell; IsMain = isMain; School = school; }
        public bool Spendable { get { return Available && HasSpell && IsMain; } }
    }

    // 판정 입력. null = 관측 못 함(Unknown). 기본값 없음 — 게임 어댑터가 모든 칸을 채운다(못 채우면 null).
    public sealed class RepairInputs
    {
        public int SlotLevel;
        public LifecycleState? Life;
        public bool? GolemBound;      // 펫 칸의 펫 == 결속 GolemId 이고 우리 골렘
        public FusionState? Fusion;
        public bool? GolemDead;
        public int? Damage, MaxHp;
        public bool? Broken;
        public int? SpendableSlots;   // 그 레벨의 쓸 수 있는(준비·미사용·주 칸) 추출물 수
        public bool? Adjacent;        // 합체 Idle 일 때만 판정에 쓴다(합체 중 골렘은 거두어져 있다)
    }

    public sealed class RepairOutcome
    {
        public readonly bool Resurrect;
        public readonly int NewDamage, Healed;
        public RepairOutcome(bool resurrect, int newDamage, int healed) { Resurrect = resurrect; NewDamage = newDamage; Healed = healed; }
        public override string ToString() { return (Resurrect ? "resurrect, " : "") + "damage -> " + NewDamage + " (healed " + Healed + ")"; }
    }

    public static class RepairRules
    {
        public static void RequireSlotLevel(int slotLevel)
        {
            if (slotLevel < 1 || slotLevel > GolemRules.Dials.MaxExtractLevel)
                throw new ArgumentOutOfRangeException("slotLevel", "RepairRules: extract slot level " + slotLevel + " outside 1.." + GolemRules.Dials.MaxExtractLevel + " (slot 0 / cantrips cannot pay for a repair)");
        }

        // 회복량 = floor(최대 HP × 슬롯 레벨 × RepairPercentPerSlotLevel / 100). 반올림 = 내림(정수 나눗셈, 양수) — PF1 「따로 말이 없으면 내림」(CRB 1장 Rounding).
        public static int Amount(int maxHp, int slotLevel)
        {
            if (maxHp < 1) throw new ArgumentOutOfRangeException("maxHp", "RepairRules.Amount: max HP " + maxHp + " < 1");
            RequireSlotLevel(slotLevel);
            return (int)((long)maxHp * slotLevel * GolemRules.Dials.RepairPercentPerSlotLevel / 100);
        }

        // 결과: 살아 있으면 피해 − 회복량(바닥 0). 죽었으면(부서짐) 되살려 HP = 회복량(최대 HP 상한) — 「부서짐 해제」와 「회복」이 둘 다 성립하는 유일한 읽기(설계 §4.7).
        public static RepairOutcome Outcome(int maxHp, int damage, bool dead, int slotLevel)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException("damage", "RepairRules.Outcome: damage " + damage + " < 0");
            int amount = Amount(maxHp, slotLevel);
            if (dead)
            {
                int hp = Math.Min(maxHp, amount);
                return new RepairOutcome(true, maxHp - hp, hp);
            }
            int nd = Math.Max(0, damage - amount);
            return new RepairOutcome(false, nd, damage - nd);
        }

        // 쓸 칸: 엔진 Spellbook.SpendInternal @010A~0155 와 같은 순서(뒤에서 앞으로) 중 첫 쓸 수 있는 칸. 없으면 -1.
        // school == null = 학파 무관(응급 수리). school != null = 그 학파(OverdriveRules.SchoolChoices 의 값 — 「학파 없음」 포함)의 추출물만(골렘 폭주, 소유자 결정 2026-11-16 「레벨 + 학파」).
        public static int PickSlot(IList<SlotView> slots, string school = null)
        {
            if (slots == null) throw new ArgumentNullException("slots");
            for (int i = slots.Count - 1; i >= 0; i--) if (Matches(slots[i], school)) return i;
            return -1;
        }

        public static int CountSpendable(IList<SlotView> slots, string school = null)
        {
            if (slots == null) throw new ArgumentNullException("slots");
            return slots.Count(s => Matches(s, school));
        }

        // 쓸 수 있고(준비·미사용·주 칸) 학파 조건에 맞는 칸인가. 학파 조건이 있는데 쓸 수 있는 칸의 학파를 모르면(null·모르는 이름) 던진다 — 「안 맞음」으로 넘기지 않는다.
        static bool Matches(SlotView s, string school)
        {
            if (!s.Spendable) return false;
            if (school == null) return true;
            if (!OverdriveRules.SchoolChoices.Contains(school)) throw new ArgumentException("RepairRules: '" + school + "' is not a school choice");
            if (s.School == null) throw new InvalidOperationException("RepairRules: a spendable slot has no school reading while picking by school '" + school + "'");
            return OverdriveRules.SchoolOf(s.School) == school;
        }

        // 인접: 중심 거리 − 두 Corpulence ≤ AdjacentFeet(미터로 넘김). 음수·NaN 입력은 던진다.
        public static bool Adjacent(float centerDistance, float corpulenceA, float corpulenceB, float adjacentMeters)
        {
            if (float.IsNaN(centerDistance) || float.IsInfinity(centerDistance) || centerDistance < 0) throw new ArgumentOutOfRangeException("centerDistance");
            if (float.IsNaN(corpulenceA) || corpulenceA < 0 || float.IsNaN(corpulenceB) || corpulenceB < 0) throw new ArgumentOutOfRangeException("corpulence");
            if (float.IsNaN(adjacentMeters) || adjacentMeters <= 0) throw new ArgumentOutOfRangeException("adjacentMeters");
            return centerDistance - corpulenceA - corpulenceB <= adjacentMeters;
        }

        // 판정 순서: 골렘 없음 → 합체 전이 중 → 결측 → 고칠 것 없음 → 슬롯 없음 → (합체 아님이면) 인접.
        public static RepairBlock Decide(RepairInputs x)
        {
            if (x == null) throw new ArgumentNullException("x");
            RequireSlotLevel(x.SlotLevel);
            if (!x.Life.HasValue) return RepairBlock.Unknown;
            if (x.Life.Value != LifecycleState.Ready && x.Life.Value != LifecycleState.Stale) return RepairBlock.NoGolem;
            if (!x.GolemBound.HasValue) return RepairBlock.Unknown;
            if (!x.GolemBound.Value) return RepairBlock.NoGolem;
            if (!x.Fusion.HasValue) return RepairBlock.Unknown;
            if (x.Fusion.Value != FusionState.Idle && x.Fusion.Value != FusionState.Active) return RepairBlock.FusionBusy;
            if (!x.GolemDead.HasValue || !x.Damage.HasValue || !x.MaxHp.HasValue || !x.Broken.HasValue) return RepairBlock.Unknown;
            if (x.Fusion.Value == FusionState.Active && x.GolemDead.Value) return RepairBlock.Unknown; // 합체 중 죽은 골렘 = 모순 관측(PetDowned 가 합체를 끝냈어야 한다)
            if (!x.GolemDead.Value && x.Damage.Value <= 0 && !x.Broken.Value) return RepairBlock.NothingToRepair;
            if (!x.SpendableSlots.HasValue) return RepairBlock.Unknown;
            if (x.SpendableSlots.Value <= 0) return RepairBlock.NoSlot;
            if (x.Fusion.Value == FusionState.Idle)
            {
                if (!x.Adjacent.HasValue) return RepairBlock.Unknown;
                if (!x.Adjacent.Value) return RepairBlock.NotAdjacent;
            }
            return RepairBlock.None;
        }
    }

     
    public static class CapstoneRules
    {
        // 공유 목록 = 설계 §4.5 「추가 면역」 열 = 카탈로그 면역(ImmunityGuids) + 우리 면역형 특성(철 불로 수리 = 설계 §12 정정 7 「화염을 추가 면역 셈에」, 아다만틴 급소 면역 = 표의 「급소 공격」).
        // 구조물 공통 면역(ConstructImmunities)은 들어가지 않는다 — 카탈로그 어느 칸에도 없다(시험 `exec: capstone shared list`가 확인).
        public static string[] SharedImmunities(Material m)
        {
            if (m == null) throw new ArgumentNullException("m");
            var l = m.ImmunityGuids.Concat(MaterialCatalog.OwnFeatureGuidsOf(m)).ToArray();
            if (l.Distinct().Count() != l.Length) throw new InvalidOperationException("CapstoneRules: " + m.Id + " shared immunity list has a duplicate");
            return l;
        }

        // 형태 키(재질@슬램단계)가 20레벨 형태인가: 그 단계의 합법 레벨이 전부 ≥ CapstoneLevel → true, 전부 < → false, 섞이면 던진다(다이얼이 어긋나면 빌드에서 시끄럽게).
        public static bool FormGetsShared(string buffKey)
        {
            var m = MaterialCatalog.MaterialOfKey(buffKey);
            int tier = int.Parse(buffKey.Substring(buffKey.IndexOf("@T", StringComparison.Ordinal) + 2));
            var levels = MaterialCatalog.LegalLevels(m).Where(l => GolemRules.SlamTier(l) == tier).ToArray();
            if (levels.Length == 0) throw new InvalidOperationException("CapstoneRules: key " + buffKey + " has no legal level");
            int at = levels.Count(l => l >= GolemRules.Dials.CapstoneLevel);
            if (at == levels.Length) return true;
            if (at == 0) return false;
            throw new InvalidOperationException("CapstoneRules: form key " + buffKey + " spans levels " + string.Join(",", levels) + " across CapstoneLevel " + GolemRules.Dials.CapstoneLevel + " — the slam tier boundary must match the capstone level");
        }

        public static string[] CapstoneKeys()
        {
            var keys = MaterialCatalog.Keys.Where(FormGetsShared).ToArray();
            foreach (var k in keys)
                if (SharedImmunities(MaterialCatalog.MaterialOfKey(k)).Length == 0) throw new InvalidOperationException("CapstoneRules: capstone form " + k + " has no additional immunity to share");
            return keys;
        }

        // 부여 계획: 이미 가진 사실은 더하지도 지우지도 않는다(엔진 EntityFactsManager.Add 는 랭크 1 특성의 기존 사실을 되돌려 주고, 지울 때 그 기존 사실을 지운다 —
        // FeatureCollection.PrepareFactForAttach @0057~0061 · Feature.AddRank @0011 · PrepareFactForDetach @000F~0018. 그대로 쓰면 본체가 원래 가진 회피 등을 잃는다).
        public static KeyValuePair<string[], string[]> GrantPlan(IList<string> wanted, ICollection<string> alreadyOwned)
        {
            if (wanted == null) throw new ArgumentNullException("wanted");
            if (alreadyOwned == null) throw new ArgumentNullException("alreadyOwned");
            if (wanted.Distinct().Count() != wanted.Count) throw new ArgumentException("CapstoneRules.GrantPlan: duplicate wanted fact");
            var add = wanted.Where(g => !alreadyOwned.Contains(g)).ToArray();
            var skip = wanted.Where(alreadyOwned.Contains).ToArray();
            return new KeyValuePair<string[], string[]>(add, skip);
        }
    }
}
