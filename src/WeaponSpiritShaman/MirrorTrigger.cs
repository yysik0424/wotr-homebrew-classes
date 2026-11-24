using System;

namespace WeaponSpiritShaman
{
    // [순수] 엔진 사건 → 재계산 필요 여부(계획 T2 「MirrorTrigger」, 설계 §4.5 재계산 시점 ①②③).
    //  SlotUpdated(칸 주인 == 펫) · ActiveSetChanged(unit == 펫) · MasterLevelUp(unit == 주인) → Recompute, 그 밖 Ignore.
     
    //  null 사건 유닛·펫 id → throw. 주인 id 는 MasterLevelUp 에서만 쓰며, 그때 null(아직 연결 전)이면 Unknown(판정 불가) — Ignore 로 읽지 않는다(C1).
    public enum TriggerKind { SlotUpdated, ActiveSetChanged, MasterLevelUp }

    public enum TriggerDecision { Recompute, Ignore, Unknown }

    public static class MirrorTrigger
    {
        public static TriggerDecision Decide(TriggerKind kind, string eventUnitId, string petId, string masterId)
        {
            if (eventUnitId == null) throw new ArgumentNullException("eventUnitId");
            if (petId == null) throw new ArgumentNullException("petId");
            switch (kind)
            {
                case TriggerKind.SlotUpdated:
                case TriggerKind.ActiveSetChanged:
                    return eventUnitId == petId ? TriggerDecision.Recompute : TriggerDecision.Ignore;
                case TriggerKind.MasterLevelUp:
                    if (masterId == null) return TriggerDecision.Unknown;
                    return eventUnitId == masterId ? TriggerDecision.Recompute : TriggerDecision.Ignore;
                default:
                    throw new ArgumentException("MirrorTrigger: undefined kind " + (int)kind);
            }
        }
    }
}
