using System;
using System.Collections.Generic;

namespace PackWarden
{
     
    // 인게임 S12 Verify(Checks\ShadowLog 가 관측을 보기로 바꿔 넘김)와 시험 exe(변이 입력 실행)가 같은 식을 쓴다(C4).
    // 보기는 닫힌 enum·정수·bool·ID 문자열만. null·빈 값·Unknown·미등록 값은 「정상 무동작」으로 읽지 않고 (UNKNOWN) 사유로 FAIL 한다(C1).
    // 반환: null = PASS, 아니면 FAIL 사유. 호출자 결함(null 인자·빈 기대 집합)은 던진다(C2).
    public static class ShadowAudit
    {
        public const string UnknownTag = "(UNKNOWN)";

        public sealed class SlotView { public int Slot; public ShadowRules.SlotStatus Status; public string AuxId; public ShadowRules.StrikeVerdict? Decision; /* null = 판정 없음 */ }
        public sealed class SnapView { public bool Complete; public ShadowRules.ShadowVerdict Verdict; public SlotView[] Slots; }
        public sealed class MainView { public int Index, Frame, AttackNumber, AttacksCount, RawPenalty, BabEntry, InitiatorBab, TargetHpAfter /* 기록 시점(그림자 뒤) 대상 HP — 프레임 안에서 HP 는 줄기만 한다 */; public bool IsFullAttack, IsFirstAttack; public string TargetId; public SnapView Snap; /* null = 처리기 스냅샷 없음 */ }
        public sealed class ChildView { public int Index, Frame, AttackNumber, AttacksCount, RawPenalty, BabEntry, InitiatorBab, ParentIndex; public bool IsFullAttack, IsFirstAttack; public string InitiatorId, TargetId; }
        public sealed class RecView { public int Index; public string InitiatorId; public bool Extra, AoO; }























































         
        // ClearView = 기록기 슬롯 비움 사건 1건(Slot = 기록기가 그 ID 를 본 슬롯, 못 봤으면 −1). freshSince ≥ 0 이면 그 색인 이후 사건이 정확히 1건(이 보조)이어야 한다
        // (전투 중 사망 창 — 해산 경로가 끼어들지 않았음), −1 이면 창 검사 생략(해산 뒤 재확인). 사건 색인 이후 죽은 보조가 개시한 기록 0(AoO·Extra 포함).
        public sealed class ClearView { public string AuxId; public int Slot, Index; }








    }
}
