using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Parts;
using Rounds = Kingmaker.Utility.Rounds;
using Newtonsoft.Json;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
     
    // 재계산을 MirrorRun 에 맡기며, 게임 객체를 읽고 쓰는 효과기(GameMirrorEffects)만 구현한다.
    //  - 흡수 상태는 펫 유닛 파트(UnitPartSpiritMirror, 세이브 대상)와 아이템 마법 부여에만 둔다(설계 §2 세이브 호환 — 정적 필드 금지, 시험 `persist: no static mutable state`).
    //  - 생명주기(MirrorLifecycle)는 저장하지 않는다: 로드마다 Pending 에서 다시 시작(R2-01).

    // 흡수 사실(영병 유닛 m_AddFacts 6번째) — 블루프린트만. Build 는 BuildAll 에서 SpiritBlueprints.Build 앞에 1회(VerifyUnitFacts 가 이 사실을 풀어야 한다).
    public static class SpiritMirror
    {
        public static BlueprintFeature Feature;

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritMirror.Build() called twice");
            Feature = Create<BlueprintFeature>("WeaponSpiritShamanSpiritMirror", Guids.SpiritMirrorFeature, x =>
            {
                SetField(x, "m_DisplayName", Strings.L("Mirror.Name"));
                SetField(x, "m_Description", Strings.L("Mirror.Description"));
                SetField(x, "m_DescriptionShort", Strings.L("Mirror.Description"));
                x.IsClassFeature = true;
                x.Ranks = 1;
                x.Groups = new FeatureGroup[0];
            });
            AddComponents(Feature, new SpiritMirrorComponent());
            var back = Feature.ComponentsArray.OfType<SpiritMirrorComponent>().ToArray();
            if (back.Length != 1 || Feature.ComponentsArray.Length != 1) throw new InvalidOperationException("WeaponSpiritShaman: SpiritMirror feature component readback failed");
        }




    }

    // 게임 세이브 직렬화 설정(계획 T5 S12 part-roundtrip 이 쓴다). 세이브 = SaveManager.<SerializeAndSaveThread>b__0 @0017 ldsfld DefaultJsonSettings.DefaultSettings → @001C JsonSerializer.Create.
    // 멤버는 리플렉션 원장(ReflectionLookups)에 둔다 — 시험이 Assembly-CSharp 에서 정확히 이 형식의 정적 필드로 찾는다(patch-targets 는 패치 대상 전용).
    public static class MirrorSave
    {



    }

    // 저장되는 기록 1개. 세이브 설정의 OptInContractResolver 는 이 모드 형식도 OptIn 으로 만든다(CreateProperties @0025~0028) → [JsonProperty] 없는 필드는 저장되지 않는다.
    // 그래서 순수 코어의 Record(Newtonsoft 참조 없음) 대신 이 형식을 저장하고, 재계산마다 코어 원장과 서로 옮긴다(ToLedger / Store).
    public sealed class SavedRecord
    {
        [JsonProperty] public string ItemId;
        [JsonProperty] public string FactId;
        [JsonProperty] public RecordKind Kind;

        // 저장 기록 ↔ 코어 원장(거울 파트·강신 파트가 같이 쓴다 — 같은 손상 판정, C4). null 목록·빈 칸 기록 = 손상 → throw(빈 원장으로 읽지 않는다).
        public static Ledger ToLedger(List<SavedRecord> applied, string owner)
        {
            if (applied == null) throw new InvalidOperationException("WeaponSpiritShaman: " + owner + ".Applied is null (corrupt save?)");
            var l = new Ledger();
            foreach (var r in applied)
            {
                if (r == null || string.IsNullOrEmpty(r.ItemId) || string.IsNullOrEmpty(r.FactId))
                    throw new InvalidOperationException("WeaponSpiritShaman: " + owner + " has an incomplete saved record (corrupt save?)");
                l.Applied.Add(new Record(r.ItemId, r.FactId, r.Kind));
            }
            return l;
        }

        public static List<SavedRecord> FromLedger(Ledger l)
        {
            if (l == null || l.Applied == null) throw new ArgumentNullException("l");
            return l.Applied.Select(r => new SavedRecord { ItemId = r.ItemId, FactId = r.FactId, Kind = r.Kind }).ToList();
        }
    }

    // 펫(영병) 소유 파트. 생성자 = 빈 목록 + 새 생명주기. 로드는 생성자 뒤 Applied 를 JSON 으로 바꿔 넣는다(Replace) — JSON 에 null 이면 손상 → ToLedger 가 던진다.
    public class UnitPartSpiritMirror : UnitPart
    {
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<SavedRecord> Applied;

        // 2026-11-18: 이 펫이 주인에 연결돼 Ready 에 든 적 있음(세이브 대상). 옛 세이브에는 없음 → false(근거 없음 → 로드 손상 판정 그대로).
        // 저장 → 로드 뒤 UnitPartPet 이 없는 펫(해체되지 않고 남은 상실 펫, AddPet.OnDeactivate @0095 m_DestroyPetOnDeactivate=false)을 손상과 가른다.
        [JsonProperty]
        public bool WasLinked;

        [JsonIgnore]
        readonly MirrorLifecycle life;

        public UnitPartSpiritMirror()
        {
            Applied = new List<SavedRecord>();
            life = new MirrorLifecycle();
        }

        public MirrorLifecycle Life { get { return life; } }

        public Ledger ToLedger() { return SavedRecord.ToLedger(Applied, "UnitPartSpiritMirror"); }

        public void Store(Ledger l) { Applied = SavedRecord.FromLedger(l); }
    }

    // 흡수 사실의 컴포넌트. 블루프린트 컴포넌트 1개를 모든 영병이 공유하므로 인스턴스 필드를 두지 않는다(상태 = 펫 파트).
    // 원시 사건 7개 → (장비·세트·레벨업은 MirrorTrigger.Decide 먼저) → Route: 그 순간 스냅샷 + LifecycleAdapter.Translate → MirrorLifecycle.Handle → 참이면 Recompute.
    public class SpiritMirrorComponent : UnitFactComponentDelegate,
        IUnitEquipmentHandler, IUnitActiveEquipmentSetHandler, IUnitLevelUpHandler, IPetHandler, IAreaHandler, IGlobalSubscriber
    {
        protected override void OnActivate() { Route(new RawEvent(RawKind.OnActivate, false, null)); }

        protected override void OnPostLoad() { Route(new RawEvent(RawKind.OnPostLoad, false, null)); }

        public void OnAreaDidLoad() { Route(new RawEvent(RawKind.OnAreaDidLoad, false, null)); }

        // 주인 연결(UnitPartPetMaster.AddPet → IPetHandler.HandleAddPet, 정찰 r1 §2.1). 남의 펫이면 코어 사건 0개(어댑터).
        public void HandleAddPet(UnitEntityData master, UnitEntityData pet)
        {
            if (!ReferenceEquals(pet, Owner)) return;
            Route(new RawEvent(RawKind.HandleAddPet, true, null));
        }

        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
        {
            var d = MirrorTrigger.Decide(TriggerKind.SlotUpdated, slot.Owner.Unit.UniqueId, Owner.UniqueId, null);
            if (d == TriggerDecision.Ignore) return;
            Route(new RawEvent(RawKind.SlotUpdated, false, d));
        }

        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit)
        {
            var d = MirrorTrigger.Decide(TriggerKind.ActiveSetChanged, unit.Unit.UniqueId, Owner.UniqueId, null);
            if (d == TriggerDecision.Ignore) return;
            Route(new RawEvent(RawKind.ActiveSetChanged, false, d));
        }

        public void HandleUnitAfterLevelUp(UnitEntityData unit, LevelUpController controller)
        {
            var owner = Owner;
            var master = owner.Master;
            string eventId = unit.UniqueId, petId = owner.UniqueId;
            var d = MirrorTrigger.Decide(TriggerKind.MasterLevelUp, eventId, petId, (object)master == null ? null : master.UniqueId);
            // The pet can gain forbidden external facts on its own level-up, even though its mirror needs no recompute.
            if (d == TriggerDecision.Ignore && eventId != petId) return;
            Route(new RawEvent(RawKind.MasterLevelUp, false, d));
        }

        // 인터페이스가 요구하지만 거울 사건이 아니다(설계 §4.5 재계산 시점 ①~④ 밖) — 본문 없음(시험이 비어 있음을 고정).
        public void HandleRemovePet(UnitEntityData master, UnitEntityData pet) { }
        public void OnAreaBeginUnloading() { }
        public void HandleUnitBeforeLevelUp(UnitEntityData unit) { }

        void Route(RawEvent raw)
        {
            // 핫픽스 2026-11-14 후속: 펫 직업에서 뺀 PrerequisiteNoFeature 의 가정(영병은 그 특성을 갖지 않음)을 영병에 관련된 사건마다 확인 — 위반은 LogError, 던지지 않는다.
            SpiritPetFactGuard.Check(Owner);
            if (raw.Trigger == TriggerDecision.Ignore) return; // own level-up with a known master only audits the pet's facts
            var part = Owner.Ensure<UnitPartSpiritMirror>();
            bool body = BodyReady(Owner);
            bool sync = ProgressionSync.Done;
            string masterId;
            var link = MasterLinkOf(Owner, out masterId);
            var snap = new Snapshot(link, masterId, body, sync, part.WasLinked);
            foreach (var e in LifecycleAdapter.Translate(raw, snap))
            {
                if (part.Life.Handle(e)) Recompute(part);
                // 저장 근거(연결된 적 있음) — 로드 뒤 파트 없는 펫이 저장된 상실인지 손상인지 가르는 유일한 근거(MirrorLifecycle.Handle 규칙 2).
                part.WasLinked = MirrorLifecycle.NextWasLinked(part.WasLinked, part.Life);
                // 2026-11-18: 주인 상실(정리 상태 전이, 펫당 WARN 1회)·재부착(무시 건수)·연결 못 읽음 통지. 없으면 null.
                var n = part.Life.TakeNotice();
                if (n != null) ModContext.Log("WSS " + n.Level + " mirror " + Owner.CharacterName + " (" + Owner.UniqueId + "): " + n.Text);
            }
        }

        // 주인 연결 관측(판정은 순수 LifecycleAdapter.ClassifyLink). 「주인 없음」을 null 하나로 읽지 않는다 — 연결 전/RemoveMaster(파트 없음) · DetachPet(연결 지워짐) ·
        // 개체를 못 찾음(일시 결측일 수 있음)을 가른다. m_MasterRef 는 비공개 → 리플렉션(원장 ReflectionLookups, 시험이 Assembly-CSharp 에서 다시 찾는다).
        // 정적 필드 없음(시험 `persist: no static mutable state`) — 주인이 해석되지 않을 때만 필드를 찾는다(드문 경로).
        static MasterLink MasterLinkOf(UnitEntityData u, out string masterId)
        {
            var master = u.Master;
            var petPart = u.Get<UnitPartPet>();
            string linkId = null;
            if ((object)master == null && petPart != null)
            {
                var f = typeof(UnitPartPet).GetField("m_MasterRef", BindingFlags.NonPublic | BindingFlags.Instance);
                if (f == null) throw new MissingFieldException("WeaponSpiritShaman: UnitPartPet.m_MasterRef not found");
                var r = (EntityPartRef<UnitEntityData, UnitPartPetMaster>)f.GetValue(petPart);
                linkId = r.EntityRef.Id;
            }
            var link = LifecycleAdapter.ClassifyLink((object)master != null, petPart != null, linkId);
            masterId = link == MasterLink.Linked ? master.UniqueId : null;
            return link;
        }

        // 몸 준비 = Body 있음 && 초기화 끝 && 무기 세트 4개(LifecycleAdapter.cs Snapshot 주석, R2-01). 초기화가 끝났는데 세트가 4개가 아니면 계약 위반 → throw
        // (「준비 안 됨」으로 읽으면 영원히 Pending 에 조용히 머문다 — C1).
        static bool BodyReady(UnitEntityData u)
        {
            var b = u.Body;
            if (b == null || b.IsInitializing) return false;
            var sets = b.HandsEquipmentSets;
            if (sets == null || sets.Count != AbsorbPlanner.SetCount)
                throw new InvalidOperationException("WeaponSpiritShaman: spirit " + u.CharacterName + " body is initialized but has " + (sets == null ? "no" : sets.Count.ToString()) + " weapon sets (expected " + AbsorbPlanner.SetCount + ")");
            return true;
        }

        void Recompute(UnitPartSpiritMirror part)
        {
            var fx = new GameMirrorEffects(Owner, Fact);
            var ledger = part.ToLedger();
            RunReport rep;
            try { rep = MirrorRun.Run(fx, ledger); }
            finally { part.Store(ledger); }
            ModContext.Log(MirrorLog.Line(Owner.CharacterName, fx.LastInput, rep, fx.NameOf));
        }
    }

    // IMirrorEffects 의 게임 구현(재계산 1회마다 새로 만든다 — 상태 없음).
    //  아이템 찾기 = 영병 손 칸 8개 + 맨손 무기 + 주인·영병 인벤토리의 「장착되지 않은」 아이템(다른 유닛이 든 아이템은 우리 것이 아니다 — 다른 영병과 서로 지우지 않게).
    //  우리 인스턴스 = 영병 손 칸 8개 + 맨손 무기의 마법 부여 중 ParentContext.AssociatedBlueprint 가 흡수 사실인 것.
    //  (맨손 무기 포함: 활성 세트가 비면 대상 = 맨손 무기 — AbsorbPlanner TargetKind.EmptyHand, 설계 §4.4.)
    public sealed class GameMirrorEffects : IMirrorEffects
    {
        readonly UnitEntityData pet;
        readonly EntityFact fact; // Add 에만 필요(부모 문맥). 해체 정리에서는 null.
        // 강신 입력용(계획 T6): BuildInput 의 대상 자기 목록에서 우리 거울 인스턴스를 뺀다 — 강신은 거울을 지우지 않고 「지금 비친 집합 = 계획 결과」를 읽는다.
        // 거울 재계산 경로(MirrorRun 은 BuildInput 전에 우리 인스턴스를 0 으로 만든다)는 false 그대로다(시험 `wiring: possession` 이 호출자를 고정).
        readonly bool excludeOurs;
        public PlanInput LastInput;

        public GameMirrorEffects(UnitEntityData pet, EntityFact fact) : this(pet, fact, false) { }

        GameMirrorEffects(UnitEntityData pet, EntityFact fact, bool excludeOurs)
        {
            if (pet == null) throw new ArgumentNullException("pet");
            this.pet = pet;
            this.fact = fact;
            this.excludeOurs = excludeOurs;
        }

        // 강신(Possession.cs GamePossessionEffects.BuildSpiritInput)만 부른다. Add 는 쓸 수 없다(fact null → Add 가 던진다).
        public static GameMirrorEffects ForPossessionInput(UnitEntityData pet) { return new GameMirrorEffects(pet, null, true); }

        UnitBody Body()
        {
            var b = pet.Body;
            if (b == null || b.HandsEquipmentSets == null || b.HandsEquipmentSets.Count != AbsorbPlanner.SetCount)
                throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " body/weapon sets not available");
            return b;
        }

        // 영병 손 칸 8개(세트 i 의 주 = 2i, 보조 = 2i+1). null 칸 = throw(엔진 계약 — 정찰 §4.2).
        HandSlot[] HandSlots()
        {
            var b = Body();
            var slots = new HandSlot[AbsorbPlanner.SetCount * 2];
            for (int i = 0; i < AbsorbPlanner.SetCount; i++)
            {
                var set = b.HandsEquipmentSets[i];
                if (set == null || set.PrimaryHand == null || set.SecondaryHand == null) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " weapon set " + i + " has a null hand slot");
                slots[2 * i] = set.PrimaryHand;
                slots[2 * i + 1] = set.SecondaryHand;
            }
            return slots;
        }

        // 후보 아이템(영병 손 칸·맨손 무기·주인/영병 인벤토리)마다 놓인 자리를 순수 MirrorScope.Classify 로 정한다. 범위 판정은 MirrorScope 만 한다(리뷰 T4 I1).
         
        // 기본 비교자 = 참조 비교 = 이전 List.Contains 와 같은 판정. 순서(손 → 맨손 → 인벤토리)는 목록 all 이 그대로 지킨다.
        List<KeyValuePair<ItemEntity, ItemHolder>> Candidates()
        {
            var hand = new HashSet<ItemEntity>();
            var all = new List<ItemEntity>();
            var inAll = new HashSet<ItemEntity>();
            foreach (var s in HandSlots()) if (s.HasItem && hand.Add(s.Item)) { all.Add(s.Item); inAll.Add(s.Item); }
            var empty = Body().EmptyHandWeapon;
            var inv = new HashSet<ItemEntity>();
            var invOrder = new List<ItemEntity>();
            var master = pet.Master;
            foreach (var coll in new[] { (object)master == null ? null : master.Inventory, pet.Inventory })
                if (coll != null)
                    foreach (var it in coll.Items) if (it != null && inv.Add(it)) invOrder.Add(it);
            if (empty != null && inAll.Add(empty)) all.Add(empty);
            foreach (var it in invOrder) if (inAll.Add(it)) all.Add(it);
            var r = new List<KeyValuePair<ItemEntity, ItemHolder>>(all.Count);
            foreach (var it in all)
                r.Add(new KeyValuePair<ItemEntity, ItemHolder>(it, MirrorScope.Classify(hand.Contains(it), empty != null && ReferenceEquals(it, empty), inv.Contains(it), it.HoldingSlot != null)));
            return r;
        }

        List<ItemEntity> OwnItems() { return Candidates().Where(kv => MirrorScope.ListsOurInstances(kv.Value)).Select(kv => kv.Key).ToList(); }

        List<ItemEntity> SearchItems() { return Candidates().Where(kv => MirrorScope.Searchable(kv.Value)).Select(kv => kv.Key).ToList(); }

        ItemEntity Find(string itemId)
        {
            var hit = SearchItems().Where(i => i.UniqueId == itemId).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("WeaponSpiritShaman: two items share UniqueId " + itemId);
            return hit.Length == 1 ? hit[0] : null;
        }

        ItemEntity Require(string itemId)
        {
            var it = Find(itemId);
            if (it == null) throw new InvalidOperationException("WeaponSpiritShaman: mirror item " + itemId + " not found on spirit " + pet.CharacterName);
            return it;
        }

        static ItemEnchantment FactOn(ItemEntity item, string factId)
        {
            var hit = item.Enchantments.Where(e => e.UniqueId == factId).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("WeaponSpiritShaman: two enchantments share UniqueId " + factId + " on " + item.UniqueId);
            return hit.Length == 1 ? hit[0] : null;
        }

        // 소유 판정은 순수 MirrorScope.IsOurs 하나(리뷰 T4 I1). 여기서는 부모 문맥 블루프린트 GUID 를 관측해 넘길 뿐이다(문맥·블루프린트 없음 = null).
        static bool IsOurs(ItemEnchantment e)
        {
            var ctx = e.ParentContext;
            string g = ctx == null || ctx.AssociatedBlueprint == null ? null : ctx.AssociatedBlueprint.AssetGuid.ToString();
            return MirrorScope.IsOurs(g);
        }

        public bool ItemExists(string itemId) { return Find(itemId) != null; }

         
        ItemEntity HeldByOtherItem(string itemId)
        {
            var hit = Candidates().Where(kv => MirrorScope.HeldByOther(kv.Value) && kv.Key.UniqueId == itemId).Select(kv => kv.Key).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("WeaponSpiritShaman: two items held by other units share UniqueId " + itemId);
            return hit.Length == 1 ? hit[0] : null;
        }

        public HeldByOtherState HeldByOtherFact(string itemId, string factId)
        {
            var it = HeldByOtherItem(itemId);
            if (it == null) return HeldByOtherState.ItemNotFound;
            return FactOn(it, factId) != null ? HeldByOtherState.FactPresent : HeldByOtherState.FactAbsent;
        }

        public void RemoveHeldByOther(string itemId, string factId)
        {
            var item = HeldByOtherItem(itemId);
            if (item == null) throw new InvalidOperationException("WeaponSpiritShaman: item " + itemId + " is not held by another unit");
            var e = FactOn(item, factId);
            if (e == null) throw new InvalidOperationException("WeaponSpiritShaman: enchantment " + factId + " not on " + itemId + " (held by another unit)");
            item.RemoveEnchantment(e);
            if (FactOn(item, factId) != null) throw new InvalidOperationException("WeaponSpiritShaman: RemoveEnchantment left " + factId + " on " + itemId + " (held by another unit)");
        }

        public bool FactExists(string itemId, string factId) { return FactOn(Require(itemId), factId) != null; }

        public void Remove(string itemId, string factId)
        {
            var item = Require(itemId);
            var e = FactOn(item, factId);
            if (e == null) throw new InvalidOperationException("WeaponSpiritShaman: enchantment " + factId + " not on " + itemId);
            item.RemoveEnchantment(e);
            if (FactOn(item, factId) != null) throw new InvalidOperationException("WeaponSpiritShaman: RemoveEnchantment left " + factId + " on " + itemId);
        }

        // 정찰 §5.2 방안 A: AddEnchantment(bp, 흡수 사실 문맥, 기간 없음) + RemoveOnUnequipItem = true(세트 전환·칸 비우기 때 엔진이 지운다, OnWillUnequip @00A1~00DA).
        public string Add(string itemId, string enchantmentGuid)
        {
            if (fact == null) throw new InvalidOperationException("WeaponSpiritShaman: GameMirrorEffects.Add without the mirror fact");
            var item = Require(itemId);
            var bp = Get<BlueprintItemEnchantment>(enchantmentGuid);
            var ctx = fact.MaybeContext;
            if (ctx == null) throw new InvalidOperationException("WeaponSpiritShaman: mirror fact on " + pet.CharacterName + " has no context");
            var e = item.AddEnchantment(bp, ctx, default(Rounds?));
            if (e == null) throw new InvalidOperationException("WeaponSpiritShaman: AddEnchantment returned null for " + bp.name + " on " + item.UniqueId);
            e.RemoveOnUnequipItem = true;
            if (!e.RemoveOnUnequipItem || !IsOurs(e) || FactOn(item, e.UniqueId) == null || string.IsNullOrEmpty(e.UniqueId))
            {
                item.RemoveEnchantment(e);
                throw new InvalidOperationException("WeaponSpiritShaman: mirrored " + bp.name + " readback failed on " + item.UniqueId + " (RemoveOnUnequip / parent context / presence)");
            }
            return e.UniqueId;
        }

        public IList<OurInstance> ListOurInstances()
        {
            var r = new List<OurInstance>();
            foreach (var item in OwnItems())
                foreach (var e in item.Enchantments)
                    if (IsOurs(e)) r.Add(new OurInstance(item.UniqueId, e.UniqueId, e.Blueprint.AssetGuid.ToString()));
            return r;
        }

        public void Log(string level, string message)
        {
            // MirrorRun 의 수준 = INFO(기록 대조) · WARN(고아 제거). 그 밖 수준은 오류 줄로 남긴다(조용히 묻지 않음).
            if (level == "INFO" || level == "WARN") ModContext.Log("WSS " + level + " " + message);
            else ModContext.LogError("WeaponSpiritShaman: " + level + " " + message);
        }

        // 주술사 레벨 = 주인 ClassData(ShamanClass).Level. 주인 없음·주술사 직업 없음 = throw(GetClassLevel 0 을 「레벨 0」으로 읽지 않는다, 정찰 §5.3).
        int ShamanLevel()
        {
            var master = pet.Master;
            if ((object)master == null) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " has no master at recompute");
            return ShamanLevelOf(master);
        }

        // 주술사 ClassData 레벨 — 거울(예산 B)과 강신(단계·지속)이 같이 쓴다. 주술사 직업이 정확히 1개가 아니면 throw(0 을 「레벨 0」으로 읽지 않는다).
        public static int ShamanLevelOf(UnitEntityData shaman)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            var cds = shaman.Progression.Classes.Where(c => c.CharacterClass != null && c.CharacterClass.AssetGuid.ToString() == Guids.Game.ShamanClass).ToArray();
            if (cds.Length != 1) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " has " + cds.Length + " ShamanClass entries (expected 1)");
            return cds[0].Level;
        }

        public PlanInput BuildInput()
        {
            var b = Body();
            int active = b.CurrentHandEquipmentSetIndex;
            if (active < 0 || active >= AbsorbPlanner.SetCount) throw new InvalidOperationException("WeaponSpiritShaman: active weapon set " + active + " out of range");
            var slots = HandSlots();
            var sources = new List<SourceWeapon>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (i / 2 == active || !slots[i].HasItem) continue; // 빈 칸은 원천이 아니다
                var it = slots[i].Item;
                sources.Add(new SourceWeapon { SetIndex = i / 2, IsPrimary = i % 2 == 0, ItemId = it.UniqueId, Enchantments = it.Enchantments.Select(ToSource).ToArray() });
            }
            // 대상 = 순수 MirrorScope.ChooseTarget(주·보조 칸 관측: 비었음/무기/무기 아님). 무기 아님(방패 등) = NonWeapon -> 계획이 0개(리뷰 T4 m2).
            int slot;
            var kind = MirrorScope.ChooseTarget(Content(slots[2 * active]), Content(slots[2 * active + 1]), out slot);
            ItemEntity target = slot >= 0 ? slots[2 * active + slot].Item : b.EmptyHandWeapon;
            if (target == null) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " active set is empty and it has no empty-hand weapon");
            // 대상 자기 마법 부여 그대로(우리 인스턴스는 MirrorRun 이 이 호출 전에 0 으로 만든다 — IMirrorEffects 계약). 강신 입력이면 우리 거울 인스턴스를 뺀다(ForPossessionInput).
            var ownEnch = target.Enchantments.Where(e => !(excludeOurs && IsOurs(e))).ToList();
            var own = ownEnch.Select(ToSource).ToArray();
             
            int ownEnh = AbsorbPlanner.EngineOwnEnhancement(ownEnch.SelectMany(e => e.Blueprint.ComponentsArray.OfType<WeaponEnhancementBonus>())
                .Select(c => new WeaponEnhComponent { Bonus = c.EnhancementBonus, Stack = c.Stack }).ToList());
            LastInput = new PlanInput
            {
                ShamanLevel = ShamanLevel(),
                ActiveSet = active,
                Sources = sources.ToArray(),
                Target = new TargetInfo { Kind = kind, ItemId = target.UniqueId, OwnGuids = own.Select(s => s.Shape.Guid).ToArray(), OwnEnhancement = ownEnh },
                Path = AbsorbPath.Mirror,
                Carrier = CarrierOf(target, kind),
            };
            return LastInput;
        }

        // 마법 부여를 얹을 무기의 엔진 값(순수 TriggerFit 의 입력). 조건 판정은 엔진 함수 그대로 부른다 — 범위 유형 = WeaponRangeTypeHelper.IsSuitableWeapon(트리거가 부르는 것, IL @0111),
        // 무기 군 = WeaponFighterGroupHelper.Contains(@0138). 무기 아님(NonWeapon) = 사실 없는 운반체. 그 밖에 무기가 아니거나 유형이 없으면 던진다(C1).
        public static CarrierWeapon CarrierOf(ItemEntity item, TargetKind kind)
        {
            if (kind == TargetKind.NonWeapon) return new CarrierWeapon { Kind = kind };
            var w = item as ItemEntityWeapon;
            if (w == null) throw new InvalidOperationException("WeaponSpiritShaman: " + kind + " carrier " + (item == null ? "null" : item.Blueprint.name) + " is not a weapon");
            var bp = w.Blueprint;
            var type = bp == null ? null : bp.Type;
            if (type == null) throw new InvalidOperationException("WeaponSpiritShaman: carrier weapon " + w.UniqueId + " has no blueprint/type");
            var groups = Enum.GetValues(typeof(WeaponFighterGroup)).Cast<WeaponFighterGroup>().Where(g => WeaponFighterGroupHelper.Contains(bp.FighterGroup, g)).Select(g => g.ToString()).ToArray();
            var ranges = Enum.GetValues(typeof(WeaponRangeType)).Cast<WeaponRangeType>().Where(r => WeaponRangeTypeHelper.IsSuitableWeapon(r, w)).Select(r => r.ToString()).ToArray();
            return new CarrierWeapon
            {
                Kind = kind, BlueprintGuid = bp.AssetGuid.ToString(), TypeGuid = type.AssetGuid.ToString(), Category = bp.Category.ToString(),
                FighterGroups = groups, RangeTypes = ranges, IsNatural = type.IsNatural, IsUnarmed = type.IsUnarmed,
            };
        }

        static HandContent Content(HandSlot s)
        {
            if (!s.HasItem) return HandContent.Empty;
            return s.Item is ItemEntityWeapon ? HandContent.Weapon : HandContent.NonWeapon;
        }

        // 마법 부여 1개 → 계획기 입력. 모양 = EnchShapeReader(시험이 jbp 725 로 jbp 추출기와 같음을 확인), 강화 값은 판정 Enhancement 일 때만 컴포넌트 값.
        SourceEnch ToSource(ItemEnchantment e)
        {
            var bp = e.Blueprint;
            if (bp == null) throw new InvalidOperationException("WeaponSpiritShaman: enchantment " + e.UniqueId + " has no blueprint");
            var shape = EnchShapeReader.Read(bp.AssetGuid.ToString(), bp.name, bp.ComponentsArray);
            int? value = null;
            if (EnchantInspector.Inspect(shape).Verdict == Verdict.Enhancement)
            {
                var c = bp.ComponentsArray.OfType<WeaponEnhancementBonus>().ToArray();
                if (c.Length != 1) throw new InvalidOperationException("WeaponSpiritShaman: enhancement " + bp.name + " has " + c.Length + " WeaponEnhancementBonus components");
                value = c[0].EnhancementBonus;
            }
            return new SourceEnch { Shape = shape, EnhancementBonus = value, RemoveOnUnequip = e.RemoveOnUnequipItem };
        }

        // 로그용 이름(고유 속성 등 원천에 없는 GUID). 블루프린트가 없으면 Get 이 던진다.
        public string NameOf(string guid) { return Get<BlueprintItemEnchantment>(guid).name; }
    }
}
