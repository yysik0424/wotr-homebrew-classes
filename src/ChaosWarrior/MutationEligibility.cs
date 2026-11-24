using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // [순수] 이번 분노에서 지금까지 뽑힌 상태(개편 계획 2026-11-16 Task O3 · O5). Drawn[i] = 겹 방식 행 i(= ChaosRules.Mutations 색인)가 뽑힌 횟수.
    // 개편 O5: 자연 공격 변이(LimbCap 행)의 후보 판정에 필요한 「그 순간의 실제 상태」 접근자 셋 — 핵심(MutationCore.RollWith)이 뽑기마다 채운다. 셋 다 「읽을 수 없음」을 null 로 말한다.
    //   LimbsNow(범주)  그 범주의 무기 든 추가 사지 수(모든 출처 — 종족 · 다른 버프 · 다른 모드 포함). null = 읽을 수 없음
    //   LimbCap(범주)   그 범주의 엔진 상한(BlueprintPersonageLimits.GetWeaponSlotCount). 음수 = 상한 없음. null = 읽을 수 없음
    //   Polymorphed     시전자가 변신(폴리모프) 중인가(UnitBody.IsPolymorphed). null = 읽을 수 없음
    // LimbCap 행이 없는 표에서는 셋 다 비워 둬도 된다(읽지 않는다). LimbCap 행이 있는데 비어 있으면 후보 / 비후보 어느 쪽으로도 넘기지 않고 던진다.
    public sealed class DrawState
    {
        public readonly int[] Drawn;
        public Func<string, int?> LimbsNow;
        public Func<string, int?> LimbCap;
        public bool? Polymorphed;

        public DrawState(int rowCount)
        {
            if (rowCount <= 0) throw new ArgumentOutOfRangeException("rowCount", rowCount, "ChaosWarrior: a draw state needs at least one stack row");
            Drawn = new int[rowCount];
        }
    }

    // [순수] 다시 뽑기 = 후보 집합(개편 설계 §12-1): 「지금 뽑으면 효과가 없는 변이」를 뺀 나머지. MutationRoll.DrawOne 이 이 목록에서 균등하게 하나 뽑는다.
    //   Never       항상 후보
    //   OncePerRage 이 분노에서 아직 안 뽑혔을 때만 후보(굳은 살갗 · 끔찍한 형상 · 늘어난 사지 — 두 번째부터 효과가 없다)
    //   LimbCap     자연 공격 변이(개편 O5). 두 가지로 뺀다:
     
    //        변신 중에는 전체 공격의 사지 공격이 변신 쪽 사지 목록만 세고(UnitBody.AdditionalLimbs = m_PolymorphAdditionalLimbs ?? m_AdditionalLimbs),
    //        우리 사지는 사람 쪽 목록에만 들어간다(복제 원본 11종 전부 KeepInPolymorph false) → 그 사지로는 공격하지 못한다.
    //        「변신 중」의 판독 = UnitBody.IsPolymorphed. 장비 칸을 유지하는 변신(m_PolymorphKeepSlots)에서는 엔진이 우리 사지도 변신 쪽 목록에 옮기지만(KeepInPolymorph @0003~0012) 구분하지 않고 같이 뺀다 —
    //        엔진의 범주 상한은 사람 쪽 목록으로 세는데(FindAdditionalLimbsWith) 우리가 읽을 수 있는 것은 변신 쪽 목록이라, 그 상태의 상한 판정이 엔진 셈법과 어긋나기 때문이다.
    //     ② 그 범주의 실제 사지 수(모든 출처)가 엔진 상한에 찼으면 뺀다. 상한 없는 범주(음수)는 항상 후보.
    //        엔진 셈법과 같다: UnitBody.AddAdditionalLimb 는 먼저 더한 뒤 그 범주의 사지 수가 상한보다 「크면」 하나를 뺀다(@0026~0031) → 더하기 전의 수 < 상한이면 빠지는 사지가 없다.
    // 상태를 읽을 수 없는 입력(null · 길이 불일치 · 음수 횟수 · 모르는 규칙 · 읽을 수 없는 사지 수 / 상한 / 변신 여부)은 후보 / 비후보 어느 쪽으로도 넘기지 않고 던진다(C1).
    public static class MutationEligibility
    {
        public static bool IsEligible(IReadOnlyList<MutationSpecs.StackRow> rows, DrawState s, int i)
        {
            Require(rows, s);
            if (i < 0 || i >= rows.Count) throw new ArgumentOutOfRangeException("i", i, "ChaosWarrior: stack row index out of range 0.." + (rows.Count - 1));
            return Judge(rows[i], s, s.Drawn[i]);
        }

        // 후보인 행 색인(오름차순). 빈 목록일 수 있다 — 호출자(MutationCore)가 NoCandidate 로 보고한다.
        public static IReadOnlyList<int> Eligible(IReadOnlyList<MutationSpecs.StackRow> rows, DrawState s)
        {
            Require(rows, s);
            var l = new List<int>();
            for (int i = 0; i < rows.Count; i++)
                if (Judge(rows[i], s, s.Drawn[i])) l.Add(i);
            return l;
        }

        static void Require(IReadOnlyList<MutationSpecs.StackRow> rows, DrawState s)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (s == null) throw new ArgumentNullException("s");
            if (s.Drawn == null) throw new InvalidOperationException("ChaosWarrior: draw state has no draw counts (cannot judge candidates)");
            if (s.Drawn.Length != rows.Count) throw new InvalidOperationException("ChaosWarrior: draw state counts " + s.Drawn.Length + " mutation(s) but the stack table has " + rows.Count + " row(s)");
            if (s.Drawn.Any(n => n < 0)) throw new InvalidOperationException("ChaosWarrior: draw state has a negative draw count [" + string.Join(",", s.Drawn) + "]");
        }

        static bool Judge(MutationSpecs.StackRow row, DrawState s, int drawn)
        {
            switch (row.Redraw)
            {
                case MutationSpecs.RedrawRule.Never:
                    return true;
                case MutationSpecs.RedrawRule.OncePerRage:
                    return drawn == 0;
                case MutationSpecs.RedrawRule.LimbCap:
                    return JudgeLimb(row, s);
                default:
                    throw new InvalidOperationException("ChaosWarrior: stack row '" + row.Id + "' has redraw rule " + row.Redraw + " (no candidate rule)");
            }
        }

        static bool JudgeLimb(MutationSpecs.StackRow row, DrawState s)
        {
            string category = row.LimbCategory;
            if (string.IsNullOrEmpty(category)) throw new InvalidOperationException("ChaosWarrior: LimbCap stack row '" + row.Id + "' has no limb category (cannot judge its cap)");
            // ① 변신 중: 읽을 수 없으면 「아님」으로 넘기지 않는다.
            if (s.Polymorphed == null) throw new InvalidOperationException("ChaosWarrior: polymorph state unreadable (cannot judge natural attack mutation '" + row.Id + "')");
            if (s.Polymorphed.Value) return false;
            // ② 범주 상한: 그 순간의 실제 사지 수와 엔진 상한. 접근자가 없거나 null 을 주면 판정 불가.
            if (s.LimbsNow == null || s.LimbCap == null) throw new InvalidOperationException("ChaosWarrior: limb state unreadable for " + category + " (the draw state has no limb accessors; row '" + row.Id + "')");
            int? cap = s.LimbCap(category);
            int? now = s.LimbsNow(category);
            if (cap == null || now == null) throw new InvalidOperationException("ChaosWarrior: limb state unreadable for " + category + " (cap " + (cap == null ? "unreadable" : cap.Value.ToString()) + ", limbs " + (now == null ? "unreadable" : now.Value.ToString()) + "; row '" + row.Id + "')");
            if (now.Value < 0) throw new InvalidOperationException("ChaosWarrior: limb count " + now.Value + " for " + category + " is negative (row '" + row.Id + "')");
            if (cap.Value < 0) return true;   // 상한 없음(엔진: 표에 없는 범주 = −1)
            return now.Value < cap.Value;
        }
    }
}
