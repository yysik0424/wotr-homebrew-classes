using System;

namespace WeaponSpiritShaman
{
     
    // 모든 원시 사건이 관측값(주인 id, 몸 준비, 동기화 완료)을 싣는다 → 사실 활성(Body 전) 뒤 어느 사건이든 주인·몸 준비를 실어 오면 Ready 전이가 완료된다.
     
     

    public enum RawKind { OnActivate, HandleAddPet, OnPostLoad, OnAreaDidLoad, SlotUpdated, ActiveSetChanged, MasterLevelUp }

    // bodyReady = Owner.Body != null && !Owner.Body.IsInitializing && Owner.Body.HandsEquipmentSets.Count == 4 (게임 쪽이 계산, T4).
    // link·masterId = SpiritMirrorComponent.MasterLinkOf → ClassifyLink (Linked ⇔ masterId non-null, LinkContract).
    public sealed class Snapshot
    {
        public readonly MasterLink Link;
        public readonly string MasterId;
        public readonly bool BodyReady, SyncDone;
        public readonly bool SavedLinked;
        public Snapshot(MasterLink link, string masterId, bool bodyReady, bool syncDone, bool savedLinked)
        {
            LinkContract.Check(link, masterId, "Snapshot");
            Link = link; MasterId = masterId; BodyReady = bodyReady; SyncDone = syncDone; SavedLinked = savedLinked;
        }
    }

    public sealed class RawEvent
    {
        public readonly RawKind Kind;
        // HandleAddPet: 사건의 펫이 이 거울의 주인(Owner)인가. 그 밖 원시 사건에서는 의미 없음(false 로 둔다).
        public readonly bool PetIsOwner;
        // 장비·세트·레벨업: MirrorTrigger.Decide 결과. 그 밖에서는 null.
        public readonly TriggerDecision? Trigger;
        public RawEvent(RawKind kind, bool petIsOwner, TriggerDecision? trigger) { Kind = kind; PetIsOwner = petIsOwner; Trigger = trigger; }
    }

    public static class LifecycleAdapter
    {
        // 엔진 관측 3개 → 주인 연결(MirrorLifecycle.cs MasterLink 주석의 IL 근거). 게임 쪽(MasterLinkOf)은 관측만 하고 판정은 여기 한 곳.
        //  masterResolved = pet.Master != null · hasPetPart = pet.Get<UnitPartPet>() != null · linkId = 그 파트 m_MasterRef 의 EntityRef.Id(파트 없으면 null).
        //  주인이 해석되면 파트 유무와 무관하게 Linked — 엔진 get_Master 자체가 파트 없을 때 PreviewOf 의 주인으로 내려간다(@0016~002A, 레벨업 미리보기 유닛).
        //  파트 없는데 id 있음 = 호출 계약 위반(id 는 그 파트에서만 읽는다) → throw. 빈 id(null·"") = EntityRef.IsIdEmpty 와 같은 판정 = Clear 된 연결.
        public static MasterLink ClassifyLink(bool masterResolved, bool hasPetPart, string linkId)
        {
            if (!hasPetPart && linkId != null) throw new ArgumentException("LifecycleAdapter: a link id without a UnitPartPet");
            if (masterResolved) return MasterLink.Linked;
            if (!hasPetPart) return MasterLink.NotLinked;
            return string.IsNullOrEmpty(linkId) ? MasterLink.Cleared : MasterLink.Unresolved;
        }

        // 원시 사건 → 0개 또는 1개의 코어 사건.
        //  OnActivate → FactActivated · HandleAddPet(pet == Owner) → MasterLinked(남의 펫이면 0개) · OnPostLoad → PostLoad · OnAreaDidLoad → AreaDidLoad
        //  SlotUpdated·ActiveSetChanged·MasterLevelUp → Trigger 가 Recompute 또는 Unknown 이면 Recompute, Ignore 면 0개.
        //  (Unknown = 판정 불가 — 부정 가드를 통과시키지 않는다: 재계산 요청으로 보내고, Pending 이면 Lifecycle 이 무시한다. C1)
        public static CoreEvent[] Translate(RawEvent raw, Snapshot snap)
        {
            if (raw == null) throw new ArgumentNullException("raw");
            if (snap == null) throw new ArgumentNullException("snap", "LifecycleAdapter: every raw event needs its snapshot");
            var obs = new Observation(snap.Link, snap.MasterId, snap.BodyReady, snap.SyncDone, snap.SavedLinked);
            bool trig = raw.Kind == RawKind.SlotUpdated || raw.Kind == RawKind.ActiveSetChanged || raw.Kind == RawKind.MasterLevelUp;
            if (trig != raw.Trigger.HasValue)
                throw new ArgumentException("LifecycleAdapter: raw " + raw.Kind + (trig ? " needs" : " must not carry") + " a MirrorTrigger decision");
            if (raw.PetIsOwner && raw.Kind != RawKind.HandleAddPet) throw new ArgumentException("LifecycleAdapter: PetIsOwner is only meaningful for HandleAddPet");
            switch (raw.Kind)
            {
                case RawKind.OnActivate: return new[] { new CoreEvent(CoreKind.FactActivated, obs) };
                case RawKind.HandleAddPet: return raw.PetIsOwner ? new[] { new CoreEvent(CoreKind.MasterLinked, obs) } : new CoreEvent[0];
                case RawKind.OnPostLoad: return new[] { new CoreEvent(CoreKind.PostLoad, obs) };
                case RawKind.OnAreaDidLoad: return new[] { new CoreEvent(CoreKind.AreaDidLoad, obs) };
                default:
                    return raw.Trigger.Value == TriggerDecision.Ignore ? new CoreEvent[0] : new[] { new CoreEvent(CoreKind.Recompute, obs) };
            }
        }
    }
}
