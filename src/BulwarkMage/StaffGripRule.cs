namespace BulwarkMage
{
     
    // 입력 7개의 128 조합 전부를 독립 진리표와 대조한다(`exec: staff grip rule`). Postfix 는 게임 값을 읽어 이 함수에 넘기고 결과대로만 행동한다.
    // 순서(앞이 이긴다): 원본이 이미 참 → 그대로(PrestigePlus OneHandGripFix 등 다른 Postfix 가 참으로 만든 것도 건드리지 않는다 — 참을 거짓으로 바꾸는 길이 없다)
    //   · 쥐는 유닛 없음 → 그대로(원본도 wielder == null 이면 첫 분기를 건너뛴다, 정찰 §5) · 무기 블루프린트 없음 → 보고(그대로)
    //   · 표지 특성 없음 → 그대로(중장법사가 아닌 유닛 — 정당한 무동작) · 쿼터스태프 && 양손 && 근접 → 한손 허용 · 그 밖(다른 무기) → 그대로.
    public enum StaffGripDecision { KeepOriginal, AllowOneHand, ReportMissingBlueprint }

    public static class StaffGripRule
    {
        public static StaffGripDecision Decide(bool original, bool hasWielder, bool hasBlueprint, bool ownerHasFeature, bool isQuarterstaff, bool isTwoHanded, bool isMelee)
        {
            if (original) return StaffGripDecision.KeepOriginal;
            if (!hasWielder) return StaffGripDecision.KeepOriginal;
            if (!hasBlueprint) return StaffGripDecision.ReportMissingBlueprint;
            if (!ownerHasFeature) return StaffGripDecision.KeepOriginal;
            if (isQuarterstaff && isTwoHanded && isMelee) return StaffGripDecision.AllowOneHand;
            return StaffGripDecision.KeepOriginal;
        }
    }
}
