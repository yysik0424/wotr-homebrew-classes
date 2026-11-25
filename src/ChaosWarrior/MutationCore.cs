using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // 부모 분노 버프가 저장한 사실 하나(개편 계획 2026-11-16 Task O3). Guid = 그 사실의 블루프린트 GUID, Rank = 그 버프 인스턴스의 순위.
    // 저장 목록의 「칸마다 한 행」이다 — 같은 인스턴스가 두 번 저장돼 있으면 두 행으로 온다(이중 저장을 순위 합이 드러낸다).
    public struct ChildFact
    {
        public readonly string Guid;
        public readonly int Rank;
        public ChildFact(string guid, int rank) { Guid = guid; Rank = rank; }
    }

    // 시전자 몸의 「무기가 든 추가 사지」 하나(개편 계획 2026-11-16 Task O4). WeaponGuid = 그 칸에 든 무기 블루프린트의 GUID, Category = 그 무기 형식의 엔진 WeaponCategory 이름.
    // 모든 출처(종족 · 다른 버프 · 다른 모드)의 사지가 한 행씩 온다 — 우리 것인지는 무기 GUID 로 가린다(NaturalWeaponPlan.MutationOf). 범주는 O5 의 상한 판정이 쓴다.
    // KeepInPolymorph = 그 칸 무기 블루프린트의 KeepInPolymorph(독립 검토 R3-F3 — 변신 쪽 목록에 복사되는지는 사지마다 엔진 UnitBody.KeepInPolymorph @0003~0012 = 칸 유지 ‖ 무기 KeepInPolymorph 로 정해진다).
    public struct LimbFact
    {
        public readonly string WeaponGuid, Category;
        public readonly bool KeepInPolymorph;
        public LimbFact(string weaponGuid, string category, bool keepInPolymorph) { WeaponGuid = weaponGuid; Category = category; KeepInPolymorph = keepInPolymorph; }
    }

     
    // 시험 exe 가 생산 Roll 을 가짜 세계로 직접 실행한다. 결과는 `return MutationOutcome.X;` 로만 낸다(`dial: core literals` 면제 계약).
    public interface IMutationWorld
    {
        bool HasCaster { get; }
        bool QuickenSpent { get; }
        // 지금 문맥의 부모 버프(Buff.Data)가 붙어 있는 우리 분노 버프인가 — 아니면 AsChild 가 조용히 안 묶이는 경로(부록 A-c)라 진행하지 않는다.
        bool ParentIsActiveRage { get; }
        int WarpriestLevel { get; }
        int RangeInclusive(int min, int max);
        int[] ForcedPicks { get; }
        int ForcedElement { get; }
        // 효과 버프 하나를 시전자에게 붙이고 부모 분노 버프에 「한 번만」 저장한다(순위형은 엔진이 기존 인스턴스를 돌려준다 — 이미 저장돼 있으면 다시 저장하지 않는다). 붙지 않으면(AddBuff null) false.
        bool ApplyChild(string buffId);
        // 부모 분노 버프가 저장한 사실 — 저장 칸마다 한 행. null = 읽을 수 없음(핵심이 실패로 보고한다. 빈 목록으로 바꾸지 않는다).
        IReadOnlyList<ChildFact> ChildrenOfParent();
        // 시전자에게 「실제로 붙어 있는」 우리 변이 버프(표식 · 원소 껍질) — 인스턴스마다 한 행(독립 검토 R1-M3). 부모가 저장했는지와 무관하게 유닛의 버프 목록에서 읽는다. null = 읽을 수 없음.
        IReadOnlyList<ChildFact> AttachedMutationBuffs();
        // 시전자 몸의 무기가 든 추가 사지 전부(모든 출처) — 엔진의 사지 공격이 세는 목록(UnitBody.AdditionalLimbs = m_PolymorphAdditionalLimbs ?? m_AdditionalLimbs). null = 읽을 수 없음(핵심이 LimbsUnreadable 로 보고한다. 「사지 0개」인 빈 목록으로 바꾸지 않는다).
        IReadOnlyList<LimbFact> Limbs();
        // 사람 쪽 사지 목록(UnitBody.m_AdditionalLimbs — 우리 사지가 실제로 들어가는 곳, 변신이 풀리면 다시 공격에 쓰이는 목록; 독립 검토 R2-F1). null = 읽을 수 없음(LimbsUnreadable).
        IReadOnlyList<LimbFact> HumanLimbs();
        // Limbs() 가 읽는 목록이 사람 쪽 목록과 같은 객체인가(비변신이면 엔진이 m_PolymorphAdditionalLimbs = null 로 두어 같다). null = 읽을 수 없음(LimbsUnreadable — 「같다」로 넘기지 않는다).
        bool? LimbListsShared { get; }
        // 변신이 장비 칸을 유지하는가(UnitBody.IsPolymorphKeepSlots — 참이면 엔진이 사람 쪽 사지를 전부 변신 쪽에 복사한다). null = 읽을 수 없음(LimbsUnreadable).
        bool? PolymorphKeepsSlots { get; }
        // 그 범주의 엔진 추가 사지 상한(개편 O5 — 게임: BlueprintPersonageLimits.GetWeaponSlotCount). 음수 = 상한 없음. null = 읽을 수 없음(후보 판정이 던진다 — 「상한 없음」으로 바꾸지 않는다).
        int? LimbCap(string category);
        // 시전자가 변신(폴리모프) 중인가(개편 O5 — 게임: UnitBody.IsPolymorphed). null = 읽을 수 없음(핵심이 PolymorphUnreadable 로 보고한다. 「변신 아님」으로 바꾸지 않는다).
        bool? CasterPolymorphed { get; }
        // 시전자에게 지금 붙어 있는 우리 신속 시전 권리 버프의 GUID 전부(개편 O8 — 인스턴스마다 한 행, 부모가 저장했는지와 무관). null = 읽을 수 없음(핵심이 QuickenFailed 로 보고한다. 「0개」로 바꾸지 않는다).
        IReadOnlyList<string> QuickenRightsOnCaster();
        // 시전자에게 붙은 우리 권리 버프를 전부 떼고, 부모 분노의 저장 목록에서 권리 참조(붙어 있든 Once 로 이미 떨어졌든)를 뺀다(독립 검토 R4-F2·F4).
        // false = 할 수 없었음(버프 목록·저장 목록을 읽을 수 없음). 핵심이 뒤에서 「남은 권리 0」을 되읽는다.
        bool RemoveQuickenRights();
    }

    public enum MutationOutcome { Applied, NoCaster, NoParent, NoLevel, ApplyFailed, ReadbackMismatch, Resumed, NoCandidate, LimbsUnreadable, LimbMismatch, PolymorphUnreadable, QuickenFailed }

    public static class MutationCore
    {
        // 생산 경로: 겹 방식 표 = MutationSpecs.StackRows.
        public static MutationOutcome Roll(IMutationWorld w, Action<string, string> report, Action<string, string> dud, out string[] applied)
        {
            return RollWith(MutationSpecs.StackRows, w, report, dud, out applied);
        }

        // 추첨 · 다시 뽑기 · 되읽기(개편 설계 §4·§12-1, 계획 Task O3 「본문 순서」). 시험이 가짜 표로 후보 0 경로를 밟는다 — 생산 경로와 같은 본문.
        //   추첨     k = DrawCount(레벨)회, 매번 후보 목록에서 균등 1개(복원 추출 — 같은 변이가 다시 나올 수 있다)
        //   다시 뽑기 거부 반복이 아니라 「후보 목록(MutationEligibility.Eligible)에서 한 번 뽑기」 — 반복이 없어 뽑기는 정확히 k회. 후보 0 → NoCandidate 보고 후 중단
        //   되읽기   뽑을 때마다 + 끝에: 변이 Id 마다 Σ(그 표식 GUID 인 저장 사실의 순위) == 뽑힌 횟수(엔진 Buff.AddRank 는 상한에서 기록 없이 무동작 — 정찰 §1.2)
        //   사지 대조 끝에(개편 O4): 자연 공격 변이 Id 마다 「그 변이의 우리 무기 GUID 를 든 추가 사지 수 == 뽑힌 횟수」(엔진 UnitBody.AddAdditionalLimb 는 범주 상한 초과분을 기록 없이 뺀다 — 정찰 §2.1)
        //   범주 상한(개편 O5): 뽑기마다 실제 사지를 다시 읽어(직전 뽑기로 붙은 우리 사지가 반영된다) 「그 범주의 사지 수(모든 출처) < 엔진 상한」인 자연 공격 변이만 후보로 둔다(MutationEligibility).
        //            자연 공격 변이를 부여한 직후 「그 범주의 사지 수 == 부여 전 + 1」을 확인한다 — 아니면 엔진이 사지를 안 붙였거나 뺀 것이다 → LimbMismatch.
         
        //            (굴림은 한 프레임 안의 동기 실행이라 도중에 바뀌지 않는다). 읽을 수 없으면 PolymorphUnreadable — 「변신 아님」으로 넘기지 않는다. 사지 대조는 변신 중에도 그대로 한다(끄지 않는다):
        //            변신 중에는 자연 공격을 뽑지 않으므로 기대 = 사람 쪽 「우리 무기 사지 0」 · 변신 쪽 「복사분(= 0)」이고, 어느 목록에든 우리 무기가 있으면 어긋남으로 보고한다(ReconcileLimbs — 독립 검토 R2-F1).
        //            분노 도중에 변신이 시작 / 해제되는 경우는 굴림이 없다 — 억제 뒤 재활성(Resumed)은 굴리지 않고 저장된 표식 인스턴스 수로 사지만 대조한다(R2-F6). 이미 붙은 사지는 엔진이 사람 쪽 목록에 그대로 두고 변신이 풀리면 다시 공격에 쓰인다.
        // rows 의 색인 = 뽑기 색인(생산 표에서는 ChaosRules.Mutations 색인 — MutationSpecs.Validate 가 순서를 묶는다). 강제 선택(점검 훅)도 같은 색인.
        internal static MutationOutcome RollWith(IReadOnlyList<MutationSpecs.StackRow> rows, IMutationWorld w, Action<string, string> report, Action<string, string> dud, out string[] applied)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (w == null) throw new ArgumentNullException("w");
            if (report == null) throw new ArgumentNullException("report");
            if (dud == null) throw new ArgumentNullException("dud");
            applied = new string[0];
            const string site = ErrorLedger.SiteMutationRoll;
            if (!w.HasCaster) { report(site, "no caster"); return MutationOutcome.NoCaster; }
            if (!w.ParentIsActiveRage) { report(site, "no parent rage buff"); return MutationOutcome.NoParent; }
            int level = w.WarpriestLevel;
            if (level < ChaosRules.Levels.MinLevel) { report(site, "no level (warpriest level " + level + ")"); return MutationOutcome.NoLevel; }

             
            // 부모 분노가 이미 우리 변이 표식을 갖고 있으면 굴리지 않고 무효과 기록(INFO). 표식이 아닌 자식(껍질·단계 버프·남의 버프)은 세지 않는다.
            var existing = w.ChildrenOfParent();
            if (Unreadable(existing)) { report(site, "readback: parent children unreadable"); return MutationOutcome.ReadbackMismatch; }
            int already = existing.Count(c => MutationSpecs.IsMarker(c.Guid));
            if (already > 0)
            {
                // 독립 검토 R2-F6: 재활성 때도 사지를 대조한다 — 억제가 표식까지 껐다 켰다면 엔진 AddAdditionalLimb 가 사지를 다시 붙이며 상한 초과분을 기록 없이 뺄 수 있다.
                // 기대 = 자연 공격 변이마다 「저장된 표식 인스턴스 수」(인스턴스형 — 인스턴스 하나가 사지 하나). 굴림 없음 · 변신 여부는 읽지 않는다(사지 대조 자체는 변신 여부와 무관한 두 목록 대조다).
                var resumedDraws = new int[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                    if (rows[i].LimbCategory != null) resumedDraws[i] = existing.Count(c => c.Guid == MutationSpecs.MarkerOf(rows[i].Id));
                var resumedLimbs = ReconcileLimbs(rows, w, resumedDraws, report, site, " (resumed rage)");
                if (resumedLimbs != MutationOutcome.Applied) return resumedLimbs;
                dud(site, "resumed rage already has " + already + " mutation marker(s); no re-roll");
                return MutationOutcome.Resumed;
            }

            // 신속 시전 권리(개편 O8): 줄 버프는 레벨 표에서 굴림 「전에」 정한다 — 레벨이 표 밖이거나 표가 깨졌으면 아무것도 붙이기 전에 던진다(ActionGuard 가 계수). 부여는 맨 끝(GrantQuickenRight).
            // 위의 Resumed 반환이 이 줄보다 앞이라 억제 뒤 재활성은 권리를 다시 주지 않는다(정찰 §5.2 「분노마다 1개」).
            string quickenRight = QuickenSpec.RightFor(level);

            int[] forced = w.ForcedPicks;
            if (forced != null) ValidateForced(forced, rows.Count);
            int k = forced != null ? forced.Length : MutationRoll.DrawCount(level, w.RangeInclusive);

            // 변신 여부(개편 O5): 굴림 시작 때 한 번. 읽을 수 없으면 판정 불가로 보고하고 굴리지 않는다(자연 공격 변이를 「후보」로도 「비후보」로도 두지 않는다).
            bool? polymorphed = w.CasterPolymorphed;
            if (polymorphed == null) { report(site, "polymorph state unreadable"); return MutationOutcome.PolymorphUnreadable; }
            // 자연 공격 행 = 사지 범주가 있는 행(MutationSpecs.Validate 가 LimbCap ⇔ LimbCategory 를 묶는다).
            if (polymorphed.Value && rows.Any(r => r.LimbCategory != null))
                dud(site, "caster is polymorphed: natural attack mutations are not candidates for this rage (their limbs would not attack in the polymorph form)");

            var state = new DrawState(rows.Count) { Polymorphed = polymorphed, LimbCap = w.LimbCap };
            var shellDraws = new int[MutationSpecs.ShellBuffIds.Count];
            var order = new List<string>();
            for (int t = 0; t < k; t++)
            {
                string at = "draw " + (t + 1) + " of " + k;
                // 범주 상한 판정은 「그 순간의 실제 사지」로(개편 설계 §12-1): 뽑기마다 다시 읽는다. 읽을 수 없으면 뽑기 전에 실패(붙이지 않는다).
                var limbsBefore = w.Limbs();
                if (LimbsUnreadable(limbsBefore)) { report(site, "limbs unreadable at " + at); return MutationOutcome.LimbsUnreadable; }
                state.LimbsNow = category => CountCategory(limbsBefore, category);
                // 다시 뽑기: 지금 효과가 없는 변이를 뺀 후보에서만 뽑는다. 후보 0 이면 실패(개편 설계 §12-1 「후보가 0이면 실패 — 코드가 검사」).
                var eligible = MutationEligibility.Eligible(rows, state);
                if (eligible.Count == 0) { report(site, "no candidate at " + at); return MutationOutcome.NoCandidate; }
                int i;
                if (forced != null)
                {
                    i = forced[t];
                    // 강제 선택(점검 훅)도 같은 계약: 후보가 아닌 값은 던진다(중복은 허용 — 1회 한정 변이의 두 번째만 여기서 걸린다).
                    if (!eligible.Contains(i)) throw new ArgumentException("ChaosWarrior: forced mutation pick " + i + " (" + rows[i].Id + ") is not a candidate at " + at + " [" + string.Join(",", forced) + "]");
                }
                else i = MutationRoll.DrawOne(eligible, w.RangeInclusive);

                string id = rows[i].Id;
                // 원소는 뽑힐 때마다 새로 굴린다(개편 설계 §5 「원소 껍질: 뽑힐 때마다 원소를 새로 굴림」).
                int e = -1;
                if (id == MutationSpecs.ElementalShellId) e = w.ForcedElement >= 0 ? w.ForcedElement : MutationRoll.PickElement(w.RangeInclusive);
                foreach (var buff in MutationSpecs.RequiredBuffs(id, e))
                    if (!w.ApplyChild(buff)) { report(site, "apply failed " + buff + " (" + id + ") at " + at); return MutationOutcome.ApplyFailed; }
                state.Drawn[i]++;
                if (e >= 0) shellDraws[e]++;
                order.Add(id);

                // 뽑을 때마다 즉시 되읽기 — 어느 뽑기에서 어긋났는지 보고에 남긴다.
                var now = w.ChildrenOfParent();
                if (Unreadable(now)) { report(site, "readback: parent children unreadable at " + at); return MutationOutcome.ReadbackMismatch; }
                var bad = new List<string>();
                CheckMarker(now, rows[i], state.Drawn[i], bad);
                if (e >= 0) CheckShell(now, e, shellDraws[e], bad);
                if (bad.Count > 0) { report(site, "readback: " + string.Join("; ", bad) + " at " + at); return MutationOutcome.ReadbackMismatch; }

                // 자연 공격 변이를 부여한 직후: 그 범주의 사지 수(모든 출처)가 정확히 하나 늘었는가. 안 늘었으면 엔진이 사지를 붙이지 않았고(억제 등), 늘었다 줄었으면 범주 상한으로 하나를 뺀 것이다
                // (UnitBody.AddAdditionalLimb @0026~0031 MarkWorstLimbItemAsUnused — 빠진 것이 다른 출처의 사지여도 여기서 드러난다). 후보 판정이 엔진 셈법과 어긋났다는 신호다.
                string limbCategory = rows[i].LimbCategory;
                if (limbCategory != null)
                {
                    var limbsAfter = w.Limbs();
                    if (LimbsUnreadable(limbsAfter)) { report(site, "limbs unreadable at " + at); return MutationOutcome.LimbsUnreadable; }
                    int before = CountCategory(limbsBefore, limbCategory), after = CountCategory(limbsAfter, limbCategory);
                    if (after != before + ChaosRules.Dials.LimbsPerNaturalAttack)
                    {
                        report(site, "limb not added or evicted: " + id + " (" + limbCategory + " limbs " + before + " -> " + after + ") at " + at);
                        return MutationOutcome.LimbMismatch;
                    }
                }
            }

            // 끝 되읽기(전부): 변이마다 순위 합 == 뽑힌 횟수 · 겹 방식의 모양 · 1회 한정 ≤ 1 · 껍질 원소마다 순위 합 == 그 원소가 굴려진 횟수 · 전체 표식 순위 합 == k.
            var children = w.ChildrenOfParent();
            if (Unreadable(children)) { report(site, "readback: parent children unreadable"); return MutationOutcome.ReadbackMismatch; }
            var problems = new List<string>();
            for (int i = 0; i < rows.Count; i++) CheckMarker(children, rows[i], state.Drawn[i], problems);
            for (int e = 0; e < shellDraws.Length; e++) CheckShell(children, e, shellDraws[e], problems);
            // 표에 없는 우리 표식이 남아 있어도(가짜 표·낡은 자식) 여기서 걸린다: 저장된 「모든」 표식의 순위 합.
            int total = children.Where(c => MutationSpecs.IsMarker(c.Guid)).Sum(c => c.Rank);
            if (total != k) problems.Add("all markers rank sum " + total + " vs draws " + k);
            if (order.Count != k) problems.Add("drew " + order.Count + " time(s) vs draws " + k);
            if (problems.Count > 0) { report(site, "readback: " + string.Join("; ", problems)); return MutationOutcome.ReadbackMismatch; }

            // 붙어 있는 것 == 저장된 것(독립 검토 R1-M3, 2026-11-17): 위 되읽기는 부모의 저장 목록만 본다 — 앞 분노가 남긴 인스턴스형 표식(재생 등)이 붙은 채 새 분노가 시작되면
            // 저장 목록은 맞는데 실제 효과는 뽑힌 수보다 많다. 유닛의 실제 버프 목록에서 우리 표식 · 껍질의 (GUID, 순위) 다중집합을 읽어 저장 목록의 그것과 대조한다. 읽을 수 없으면 실패.
            var attached = w.AttachedMutationBuffs();
            if (Unreadable(attached)) { report(site, "readback: attached mutation buffs unreadable"); return MutationOutcome.ReadbackMismatch; }
            var loose = AttachedVsStored(attached, children);
            if (loose.Count > 0) { report(site, "readback: attached != stored: " + string.Join("; ", loose)); return MutationOutcome.ReadbackMismatch; }

            // 사지 대조(개편 설계 §12-1 「우리 자연 공격 변이 수 = 우리 무기 GUID 의 실제 사지 수. 어긋나면 오류 줄」, 계획 Task O4): 표식이 붙었어도 사지는 엔진이 뺄 수 있다.
            // 자연 공격을 하나도 안 뽑았어도 읽는다(뽑힌 횟수 0 == 우리 사지 0 — 앞 분노의 사지가 남아 있으면 여기서 드러난다). 읽을 수 없으면 「0개」가 아니라 실패.
            // 변신 중에도 같은 대조다(독립 검토 R2-F1 — 사람 쪽 목록과 변신 쪽 목록을 각각 대조한다, ReconcileLimbs).
            var endLimbs = ReconcileLimbs(rows, w, state.Drawn, report, site, "");
            if (endLimbs != MutationOutcome.Applied) return endLimbs;

            // 변이는 전부 붙고 되읽기가 맞았다 — 뽑힌 목록은 권리 단계의 성패와 무관하게 사실대로 돌려준다(모드 로그 한 줄 · 점검 LastRoll).
            applied = order.ToArray();
            // 신속 시전 권리(개편 설계 §12-2, 계획 Task O8): 변이 부여 · 되읽기 · 사지 대조가 전부 통과한 뒤 분노의 자식으로 1개.
            return GrantQuickenRight(w, quickenRight, report, site);
        }

        // 권리 부여(계획 Task O8 Interfaces ①~④ · 독립 검토 R4-F4). Applied = 정확히 1개가 부모의 자식이자 시전자의 유일한 권리. 실패는 QuickenFailed + 보고 1(조용히 0개 · 2개로 넘기지 않는다).
        //   ① 시전자의 권리를 읽는다 — 읽을 수 없으면 실패.
        //   ② 이미 있으면(앞 분노의 잔존 — 결함 경로) 오류로 보고하고 그것을 지운 뒤(ClearRights — 남은 권리 0 을 되읽는다) 새로 준다(R4-F4 소유자 결정 2026-11-17). 분노당 1개는 ④가 지킨다.
        //   ③ ApplyChild(그 레벨의 권리) — 붙지 않으면 실패. ④ 되읽기: 부모의 자식 중 권리 정확히 1개 · 그 GUID == 줄 것 · 시전자 전체에서도 정확히 1개 · 같은 GUID.
        static MutationOutcome GrantQuickenRight(IMutationWorld w, string right, Action<string, string> report, string site)
        {
            if (right == null) throw new ArgumentNullException("right");
            if (w.QuickenSpent) return MutationOutcome.Applied;
            var before = w.QuickenRightsOnCaster();
            if (before == null || before.Any(g => g == null)) { report(site, "quicken rights unreadable (before the grant)"); return MutationOutcome.QuickenFailed; }
            if (before.Count > 0)
            {
                report(site, "stale quicken right before grant: " + string.Join(",", before) + " (one right per rage — removing it, then granting " + right + ")");
                var cleared = ClearRights(w, report, site, "stale right before the grant");
                if (cleared != MutationOutcome.Applied) return cleared;
            }
            if (!w.ApplyChild(right)) { report(site, "apply failed quicken right " + right); return MutationOutcome.QuickenFailed; }
            var children = w.ChildrenOfParent();
            if (Unreadable(children)) { report(site, "readback: parent children unreadable after the quicken right"); return MutationOutcome.QuickenFailed; }
            var after = w.QuickenRightsOnCaster();
            if (after == null || after.Any(g => g == null)) { report(site, "quicken rights unreadable (after the grant)"); return MutationOutcome.QuickenFailed; }
            var held = children.Where(c => QuickenSpec.IsRight(c.Guid)).Select(c => c.Guid).ToList();
            var problems = new List<string>();
            if (held.Count != 1 || held[0] != right) problems.Add("the rage holds " + held.Count + " quicken right(s) [" + string.Join(",", held) + "], expected exactly " + right);
            if (after.Count != 1 || after[0] != right) problems.Add("the caster has " + after.Count + " quicken right(s) [" + string.Join(",", after) + "], expected exactly " + right);
            if (problems.Count > 0) { report(site, "quicken right readback: " + string.Join("; ", problems)); return MutationOutcome.QuickenFailed; }
            return MutationOutcome.Applied;
        }

        // 권리 전부 지우기 + 되읽기: RemoveQuickenRights 뒤 시전자의 권리 0 · 부모가 저장한 권리 0. 아니거나 읽을 수 없으면 QuickenFailed + 보고 1. Applied = 비었다.
        static MutationOutcome ClearRights(IMutationWorld w, Action<string, string> report, string site, string why)
        {
            if (!w.RemoveQuickenRights()) { report(site, "could not remove quicken right(s) (" + why + ")"); return MutationOutcome.QuickenFailed; }
            var left = w.QuickenRightsOnCaster();
            if (left == null || left.Any(g => g == null)) { report(site, "quicken rights unreadable after removal (" + why + ")"); return MutationOutcome.QuickenFailed; }
            var children = w.ChildrenOfParent();
            if (Unreadable(children)) { report(site, "readback: parent children unreadable after removing quicken right(s) (" + why + ")"); return MutationOutcome.QuickenFailed; }
            int stored = children.Count(c => QuickenSpec.IsRight(c.Guid));
            if (left.Count != 0 || stored != 0) { report(site, "quicken right(s) left after removal (" + why + "): caster " + left.Count + " [" + string.Join(",", left) + "], stored by the rage " + stored); return MutationOutcome.QuickenFailed; }
            return MutationOutcome.Applied;
        }

        // 불러오기 뒤 재충전(독립 검토 R4-F2 소유자 결정 2026-11-17 「본편처럼 다시 채움」). 부르는 곳 = RageOffWatcher(QuickenRefill — 불러온 세이브에서 처음 보는 분노 인스턴스).
        //   본편 선례: BuffExtraEffects.OnApplyPostLoadFixes → CheckAndReapply @0000~0073(이전 AppliedBuff 를 지우고, 분노가 있으면 새로 AddBuff — 그 사이 썼는지와 무관).
        //   여기도 같다: 분노가 살아 있으면 이전 권리(붙은 것 · 저장 목록의 참조)를 지우고(정상 경로 — 정보 줄) 그 레벨의 권리를 분노의 자식으로 새로 준다(GrantQuickenRight 의 되읽기 그대로).
        //   그래서 분노 중 저장 → 불러오기를 되풀이하면 신속 시전을 여러 번 얻는다(본편 블러드레이저와 같은 성질 — 설명문 · README).
        //   억제 뒤 재활성(Resumed)은 이 경로가 아니다(같은 인스턴스 — 이미 본 분노). 결과는 Applied / NoCaster / NoParent / NoLevel / QuickenFailed(보고 1).
        public static MutationOutcome RefillQuickenRight(IMutationWorld w, Action<string, string> report, Action<string, string> info)
        {
            if (w == null) throw new ArgumentNullException("w");
            if (report == null) throw new ArgumentNullException("report");
            if (info == null) throw new ArgumentNullException("info");
            const string site = ErrorLedger.SiteMutationRoll;
            if (!w.HasCaster) { report(site, "reload refill: no caster"); return MutationOutcome.NoCaster; }
            if (!w.ParentIsActiveRage) { report(site, "reload refill: no live rage buff"); return MutationOutcome.NoParent; }
            int level = w.WarpriestLevel;
            if (level < ChaosRules.Levels.MinLevel) { report(site, "reload refill: no level (warpriest level " + level + ")"); return MutationOutcome.NoLevel; }
            string right = QuickenSpec.RightFor(level);
            if (w.QuickenSpent) return MutationOutcome.Applied;
            var attached = w.QuickenRightsOnCaster();
            if (attached == null || attached.Any(g => g == null)) { report(site, "reload refill: quicken rights unreadable"); return MutationOutcome.QuickenFailed; }
            var children = w.ChildrenOfParent();
            if (Unreadable(children)) { report(site, "reload refill: parent children unreadable"); return MutationOutcome.QuickenFailed; }
            int stored = children.Count(c => QuickenSpec.IsRight(c.Guid));
            if (attached.Count > 0 || stored > 0)
            {
                var cleared = ClearRights(w, report, site, "reload refill");
                if (cleared != MutationOutcome.Applied) return cleared;
            }
            var granted = GrantQuickenRight(w, right, report, site);
            if (granted == MutationOutcome.Applied)
                info(site, "reload refill: quicken right " + right + " granted to the loaded rage (removed old: " + attached.Count + " attached, " + stored + " stored) — as vanilla BuffExtraEffects.OnApplyPostLoadFixes");
            return granted;
        }

        // 사지 대조(독립 검토 R2-F1, 2026-11-17 — Assembly-CSharp IL 직독, md5 앞 12 748b3a143b60):
        //   엔진 UnitBody.get_AdditionalLimbs @0001~000B = m_PolymorphAdditionalLimbs ?? m_AdditionalLimbs. 우리 사지는 사람 쪽(m_AdditionalLimbs)에 들어간다(AddLimbToIndex @0006~0046).
        //   변신 쪽 목록은 변신 때마다 새로 만들어지고(GeneratePolymorphAdditionalLimbs @0000~0006 — 빈 목록일 수 있다) 사람 쪽 사지는 KeepInPolymorph(@0003~0012 = 무기 KeepInPolymorph || m_PolymorphKeepSlots)일 때만 복사된다
        //   (GeneratePolymorphAdditionalLimbs @0015~006A, 변신 중 새로 붙을 때 AddLimbToIndex @0058~0066; 빼 둔 칸은 복사하지 않는다 TryAddPolymorphAdditionalLimbItem @000D~001B; 사람 쪽을 지우면 사본도 지운다 RemoveAdditionalLimbItem @0035~0037).
        //   그래서 변신 중에 공격 목록만 보면 사람 쪽에 남은 우리 사지(앞 분노의 잔류)가 「0개」로 보이고, 변신이 풀리면 공격에 쓰인다 → 두 목록을 각각 대조한다:
        //     사람 쪽: 그 변이의 우리 무기 사지 수 == 뽑힌 횟수(CheckLimbs).
        //     변신 쪽(두 목록이 다른 객체일 때만 — 같은 객체면 한 번만 센다, 이중 계산 금지): 우리 무기 사지 수 == 사람 쪽에서 복사 조건을 만족하는 사지 수. 복사 조건(사지마다, 엔진 UnitBody.KeepInPolymorph @0003~0012 와 같은 식)
        //       = 변신이 칸을 유지 ‖ 그 사지 무기의 KeepInPolymorph(LimbFact.KeepInPolymorph — 독립 검토 R3-F3: 상수가 아니라 실제 칸 무기에서 읽는다).
        //   어느 목록이든 · 같은 객체 여부 · 칸 유지 여부를 읽을 수 없으면 LimbsUnreadable(「0개」·「같다」·「유지 안 함」으로 넘기지 않는다). Applied = 일치(호출자가 그 밖의 값을 그대로 돌려준다).
        // at = 보고 끝에 붙는 문맥(" (resumed rage)" 등).
        static MutationOutcome ReconcileLimbs(IReadOnlyList<MutationSpecs.StackRow> rows, IMutationWorld w, int[] drawn, Action<string, string> report, string site, string at)
        {
            var human = w.HumanLimbs();
            if (LimbsUnreadable(human)) { report(site, "limbs unreadable (human-side list)" + at); return MutationOutcome.LimbsUnreadable; }
            bool? shared = w.LimbListsShared;
            if (shared == null) { report(site, "limbs unreadable (cannot tell whether the attack list is the human-side list)" + at); return MutationOutcome.LimbsUnreadable; }
            var problems = new List<string>();
            CheckLimbs(human, rows, drawn, problems);
            if (!shared.Value)
            {
                var attack = w.Limbs();
                if (LimbsUnreadable(attack)) { report(site, "limbs unreadable (polymorph-side list)" + at); return MutationOutcome.LimbsUnreadable; }
                bool? keep = w.PolymorphKeepsSlots;
                if (keep == null) { report(site, "limbs unreadable (polymorph keep-slots flag)" + at); return MutationOutcome.LimbsUnreadable; }
                CheckPolymorphLimbs(attack, human, rows, keep.Value, problems);
            }
            if (problems.Count > 0) { report(site, "limbs: " + string.Join("; ", problems) + at); return MutationOutcome.LimbMismatch; }
            return MutationOutcome.Applied;
        }

        // 변신 쪽 목록의 우리 무기 사지 수 == 사람 쪽의 그 무기 사지 중 (keepSlots ‖ 그 사지의 KeepInPolymorph) 인 것의 수. 표의 사지 행에 없는 우리 무기가 변신 쪽에 있으면 기대 0 과 대조해 어긋남. internal: 시험이 직접 밟는다.
        internal static void CheckPolymorphLimbs(IReadOnlyList<LimbFact> attack, IReadOnlyList<LimbFact> human, IReadOnlyList<MutationSpecs.StackRow> rows, bool keepSlots, List<string> problems)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].LimbCategory == null) continue;
                string weapon = NaturalWeaponPlan.GuidOf(rows[i].Id);
                int humanN = human.Count(l => l.WeaponGuid == weapon), got = attack.Count(l => l.WeaponGuid == weapon), want = human.Count(l => l.WeaponGuid == weapon && (keepSlots || l.KeepInPolymorph));
                if (got != want) problems.Add(rows[i].Id + " " + got + " polymorph-side limb(s) vs " + want + " expected (human-side " + humanN + ", keep-slots " + keepSlots + ")");
            }
            foreach (var g in attack.Select(l => l.WeaponGuid).Distinct())
            {
                string id = NaturalWeaponPlan.MutationOf(g);
                if (id == null) continue;
                if (!rows.Any(r => r.Id == id && r.LimbCategory != null)) problems.Add(id + " " + attack.Count(l => l.WeaponGuid == g) + " polymorph-side limb(s) vs no draw(s) (not a limb row of the stack table)");
            }
        }

        // 뽑힌 변이 목록의 모드 로그 줄(개편 설계 §12-4, 계획 Task O3): "rage mutations (k=<n>): <Id>x<횟수>, …" — 처음 뽑힌 순서, 횟수 = 그 Id 가 목록에 나온 수. 어댑터가 「ChaosWarrior: 」 뒤에 붙여 적는다.
        public static string RollLine(string[] applied)
        {
            if (applied == null) throw new ArgumentNullException("applied");
            if (applied.Any(a => a == null)) throw new ArgumentException("ChaosWarrior: a drawn mutation id is null", "applied");
            var ids = applied.Distinct().ToArray();
            return "rage mutations (k=" + applied.Length + "): " + string.Join(", ", ids.Select(id => id + "x" + applied.Count(a => a == id)));
        }

        // 우리 변이 버프(표식 · 원소 껍질)인가 — 단계 버프 · 남의 버프는 이 대조에서 세지 않는다.
        static bool IsMutationBuff(string guid) { return MutationSpecs.IsMarker(guid) || MutationSpecs.ShellBuffIds.Contains(guid); }

        // 붙어 있는 우리 변이 버프와 부모가 저장한 우리 변이 버프의 (GUID, 순위) 다중집합 대조. GUID 마다 순위 목록(정렬)이 다르면 한 줄. 빈 목록 = 같다.
        // internal: 시험이 직접 밟는다.
        internal static List<string> AttachedVsStored(IReadOnlyList<ChildFact> attached, IReadOnlyList<ChildFact> stored)
        {
            if (attached == null) throw new ArgumentNullException("attached");
            if (stored == null) throw new ArgumentNullException("stored");
            var d = new List<string>();
            foreach (var guid in attached.Concat(stored).Select(c => c.Guid).Where(IsMutationBuff).Distinct())
            {
                var a = attached.Where(c => c.Guid == guid).Select(c => c.Rank).OrderBy(x => x).ToArray();
                var s = stored.Where(c => c.Guid == guid).Select(c => c.Rank).OrderBy(x => x).ToArray();
                if (!a.SequenceEqual(s))
                    d.Add((MutationSpecs.IdOfMarker(guid) ?? "shell " + guid) + " attached rank(s) [" + string.Join(",", a) + "] vs stored [" + string.Join(",", s) + "]");
            }
            return d;
        }

        // 저장 목록을 읽을 수 없음: null, 또는 블루프린트 GUID 가 없는 행(무엇의 순위인지 알 수 없다 — 0 으로 세지 않는다).
        static bool Unreadable(IReadOnlyList<ChildFact> children)
        {
            return children == null || children.Any(c => c.Guid == null);
        }

        // 그 범주의 사지 수(모든 출처). 목록은 LimbsUnreadable 을 통과한 것만 넘긴다(null 행 없음).
        static int CountCategory(IReadOnlyList<LimbFact> limbs, string category)
        {
            return limbs.Count(l => l.Category == category);
        }

        // 사지 목록을 읽을 수 없음: null, 또는 무기 GUID·범주가 없는 행(무엇의 사지인지 알 수 없다 — 「우리 것이 아님」으로 세지 않는다).
        static bool LimbsUnreadable(IReadOnlyList<LimbFact> limbs)
        {
            return limbs == null || limbs.Any(l => l.WeaponGuid == null || l.Category == null);
        }

        // 사지 대조. 「우리 사지 수」 = 무기 GUID 가 그 변이의 우리 복제 무기(NaturalWeaponPlan.GuidOf)인 추가 사지의 수 — 다른 출처의 사지(본편 Slam1d6 등)는 세지 않는다(그래서 무기를 복제했다).
        //   표의 사지 행(LimbCategory 가 있는 행 = 자연 공격 변이, MutationSpecs.Validate 가 묶는다)마다: 우리 사지 수 == 뽑힌 횟수(같지 않으면 — 모자라도, 남아도 — 어긋남).
        //   표의 사지 행에 없는 우리 무기가 사지에 있으면(가짜 표 · 표에서 빠진 변이의 남은 사지): 뽑힌 횟수 0 과 대조해 어긋남.
        // internal: 시험이 직접 밟는다.
        internal static void CheckLimbs(IReadOnlyList<LimbFact> limbs, IReadOnlyList<MutationSpecs.StackRow> rows, int[] drawn, List<string> problems)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].LimbCategory == null) continue;   // 사지를 주지 않는 변이
                string weapon = NaturalWeaponPlan.GuidOf(rows[i].Id);   // 무기 행이 없으면 던진다(대조할 GUID 없이 「0개」로 넘기지 않는다)
                int got = limbs.Count(l => l.WeaponGuid == weapon);
                if (got != drawn[i]) problems.Add(rows[i].Id + " " + got + " limb(s) vs " + drawn[i] + " draw(s)");
            }
            foreach (var g in limbs.Select(l => l.WeaponGuid).Distinct())
            {
                string id = NaturalWeaponPlan.MutationOf(g);
                if (id == null) continue;   // 우리 무기가 아니다 = 다른 출처의 사지
                if (!rows.Any(r => r.Id == id && r.LimbCategory != null)) problems.Add(id + " " + limbs.Count(l => l.WeaponGuid == g) + " limb(s) vs no draw(s) (not a limb row of the stack table)");
            }
        }

        // 변이 하나의 표식 대조. 「표식 순위 합」 = 그 표식 GUID 를 가진 저장 사실들의 순위 합(순위형 = 인스턴스 1개·순위 n, 인스턴스형 = 인스턴스 n개·순위 1).
        // internal: 시험이 「1회 한정인데 두 번 뽑힘」 갈래를 직접 밟는다 — 후보 배제(MutationEligibility)가 살아 있는 한 RollWith 로는 닿지 않는 두 번째 방어선이다.
        internal static void CheckMarker(IReadOnlyList<ChildFact> children, MutationSpecs.StackRow row, int drawn, List<string> problems)
        {
            string marker = MutationSpecs.MarkerOf(row.Id);
            var facts = children.Where(c => c.Guid == marker).ToList();
            int sum = facts.Sum(c => c.Rank);
            if (sum != drawn) problems.Add(row.Id + " rank sum " + sum + " vs draws " + drawn + " (" + facts.Count + " stored instance(s))");
            if (facts.Any(c => c.Rank < 1)) problems.Add(row.Id + " has a stored marker with rank below 1");
            if (row.Mode == MutationSpecs.StackMode.Rank)
            {
                // 순위형: 인스턴스는 하나여야 한다(둘이면 엔진이 Rank 로 합치지 않았거나 같은 인스턴스를 두 번 저장했다).
                if (facts.Count > 1) problems.Add(row.Id + " is rank-mode but has " + facts.Count + " stored instance(s)");
            }
            else if (row.Mode == MutationSpecs.StackMode.Instances)
            {
                // 인스턴스형: 인스턴스마다 순위 1(엔진 Buff 의 첫 순위). 1 이 아니면 엔진이 새 인스턴스를 만들지 않고 순위를 올린 것이다 — 효과(사지·빠른 치유)는 인스턴스 수만큼만 생긴다.
                if (facts.Any(c => c.Rank != 1)) problems.Add(row.Id + " is instance-mode but has a stored instance with rank != 1");
            }
            else throw new InvalidOperationException("ChaosWarrior: stack mode " + row.Mode + " of '" + row.Id + "' has no read-back rule");
            if (row.Redraw == MutationSpecs.RedrawRule.OncePerRage && drawn > 1) problems.Add(row.Id + " is once-per-rage but was drawn " + drawn + " time(s)");
        }

        // 원소 껍질 하나의 대조: 껍질 버프는 순위형(같은 원소가 다시 굴려지면 저항이 더해진다) — 순위 합 == 그 원소가 굴려진 횟수, 인스턴스 ≤ 1.
        static void CheckShell(IReadOnlyList<ChildFact> children, int element, int rolled, List<string> problems)
        {
            string shell = MutationSpecs.ShellBuffIds[element];
            var facts = children.Where(c => c.Guid == shell).ToList();
            int sum = facts.Sum(c => c.Rank);
            string name = "shell " + ChaosRules.Dials.ShellElements[element] + " " + shell;
            if (sum != rolled) problems.Add(name + " rank sum " + sum + " vs element rolls " + rolled + " (" + facts.Count + " stored instance(s))");
            if (facts.Any(c => c.Rank < 1)) problems.Add(name + " has a stored instance with rank below 1");
            if (facts.Count > 1) problems.Add(name + " is rank-mode but has " + facts.Count + " stored instance(s)");
        }

        // 강제 선택(점검 훅)의 모양: 1개 이상, 전부 표의 색인. 중복은 허용한다(개편 — 같은 변이가 겹친다). 후보 여부는 뽑는 순간에 본다(RollWith). 어기면 던진다(ActionGuard 가 계수·재던짐).
        static void ValidateForced(int[] picks, int rowCount)
        {
            if (picks.Length == 0) throw new ArgumentException("ChaosWarrior: no mutation picked");
            if (picks.Any(i => i < 0 || i >= rowCount)) throw new ArgumentException("ChaosWarrior: mutation index out of range [" + string.Join(",", picks) + "]");
        }
    }
}
