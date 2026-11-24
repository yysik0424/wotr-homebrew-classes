using System;
using System.Collections.Generic;

namespace ChaosWarrior
{
     
    // 같은 라운드 재점화가 TryStart @0000~0018 에서 조기 반환된다(추가 소비·재추첨 없음). 시작됨 ∧ 꺼짐 이면 감시기가 공개 Stop(false) 를 부른다.
    // IsOn 은 근접 공격 경로(OnAttack @001D~0056)에서 바뀌지 않으므로 「마지막 1회 유지」(critical)와 충돌하지 않는다.
    public static class RageOffRule
    {
        public static bool ShouldStop(bool isStarted, bool isOn) { return isStarted && !isOn; }

        // 분노 종료 가장자리(2026-11-16 독립 검토 B): 지난 감시 때 그 유닛에 우리 분노 버프가 붙어 있었고 지금은 없다 → 고아 정리 1회 요청(OrphanMutationSweep.RequestAfterRageEnd).
        // 끄기·전투 종료·기절·해제 어느 경로로 끝나도 같은 가장자리다. 계속 켜져 있음·계속 꺼져 있음·방금 켜짐은 요청하지 않는다.
        public static bool RageJustEnded(bool hadRageBuff, bool hasRageBuff) { return hadRageBuff && !hasRageBuff; }

        // 불러오기 재충전(독립 검토 R4-F2): 우리 분노 버프가 붙어 있고 · 그 인스턴스를 이번 실행에서 처음 보고(굴림 경로도 재충전도 안 지남 = 세이브에서 되살아난 객체) · 로딩 중이 아니면 → 권리를 다시 채운다.
        // 새로 켠 분노 · 억제 뒤 재활성 · 레벨업 재적용(ActivatableAbility.ReapplyBuff 가 분노 버프를 새로 붙인다 — 굴림이 다시 돈다)은 굴림 경로에서 이미 본 인스턴스라 채우지 않는다.
        public static bool ShouldRefillQuicken(bool hasRageBuff, bool seenThisSession, bool loading) { return hasRageBuff && !seenThisSession && !loading; }
    }

    // The engine reuses the party list and may reorder or replace members in place.
    // Take a snapshot only when membership/order changes, keeping each remaining
    // member's observations across reordering. A different player starts fresh.
    internal sealed class RageWatchRoster<TUnit, TAbility> where TUnit : class where TAbility : class
    {
        internal sealed class Member
        {
            internal readonly TUnit Unit;
            internal bool HadRage;
            internal bool Dirty = true;
            TAbility ability;

            internal Member(TUnit unit) { Unit = unit; }
            internal TAbility Ability(Func<TUnit, TAbility> find)
            {
                if (Dirty) { ability = find(Unit); Dirty = false; }
                return ability;
            }
        }

        List<Member> members = new List<Member>();
        internal int Count { get { return members.Count; } }
        internal Member this[int index] { get { return members[index]; } }

        internal void Clear() { members.Clear(); }

        internal void SetMembers(IList<TUnit> units)
        {
            int index = 0;
            if (units.Count == members.Count)
            {
                while (index < units.Count && ReferenceEquals(units[index], members[index].Unit)) index++;
                if (index == units.Count) return;
            }
            var next = new List<Member>(units.Count);
            for (int i = 0; i < units.Count; i++) next.Add(Find(units[i]) ?? new Member(units[i]));
            members = next;
        }

        Member Find(TUnit unit)
        {
            for (int i = 0; i < members.Count; i++)
                if (ReferenceEquals(members[i].Unit, unit)) return members[i];
            return null;
        }

        internal void Invalidate(TUnit unit)
        {
            var member = Find(unit);
            if (member != null) member.Dirty = true;
        }

        internal void InvalidateAll()
        {
            for (int i = 0; i < members.Count; i++) members[i].Dirty = true;
        }
    }
}
