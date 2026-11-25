using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;
using static PackWarden.BlueprintUtil;

namespace PackWarden
{
    // 지도자(헌터)에 붙는 무리 상태. 슬롯 수 = PackRules.AuxSlotCount(소유자 결정 2026-11-16: 4). 빈 슬롯 = UniqueId 가 없는 default UnitReference.
    // UnitReference 는 구조체라 null 을 못 담는다. 게임 EntityRefConverter 는 default 를 null 로 쓰고 null 을 UniqueId=null 로 읽는다(dnlib 확인).
    //
    // 저장 형식(2026-11-16 변경): { Auxiliaries: 4칸, ToggleOn }. 그 전 = { Auxiliaries: 2칸, RespawnReadyAt: 2칸, ToggleOn }.
    //   · RespawnReadyAt(자리별 재소집 대기 시각)는 규칙과 함께 없앴다. 옛 저장의 그 값은 받을 멤버가 없어 역직렬화가 버린다 — 저장 설정
    //     Kingmaker.EntitySystem.Persistence.JsonUtility.DefaultJsonSettings..cctor 는 MissingMemberHandling 을 건드리지 않고(Assembly-CSharp 전체에 set_MissingMemberHandling
    //     호출 0, dnlib 2026-11-16 — 시험이 잠근다) Newtonsoft 기본값은 Ignore 다.
    //   · 옛 2칸 목록은 OnPostLoad 가 4칸으로 늘린다(PackRules.MigrateSavedSlots). 그 밖의 길이는 손상 → OnPostLoad 가 던지고(엔진 EntityPart.PostLoad 가 잡아
    //     기록, 불러오기는 계속) 처음 쓰는 곳의 Validate 가 다시 던진다.
    public class UnitPartPack : UnitPart
    {
        // 생성자가 칸을 채우므로 로드 때 Newtonsoft 기본(Reuse)이면 저장된 칸이 그 뒤에 덧붙는다 → Replace.
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<UnitReference> Auxiliaries;

        // 토글 버프 상태의 거울(진단용). 판정은 ShouldSummon 이 버프를 직접 본다(단일 원천).
        [JsonProperty]
        public bool ToggleOn;

        // 이번 전투에서 죽은 보조 수(소유자 결정 2026-11-16: 죽은 보조는 그 전투에서는 안 돌아오고 다음 전투에 바로 돌아온다).
        // 저장하지 않는다: 전투 단위 상태이고 게임은 전투 중 저장을 막는다. 불러온 직후 = 0.
        [JsonIgnore]
        public readonly PackRules.CombatLosses Losses = new PackRules.CombatLosses();

        public UnitPartPack()
        {
            Auxiliaries = new List<UnitReference>(new UnitReference[PackRules.AuxSlotCount]);
        }

        // 엔진: EntityDataBase.PostLoad @0095 → EntityPartsManager.PostLoad @0039 → EntityPart.PostLoad @0028 callvirt OnPostLoad(try 안 — 예외는 PFLog 로 기록되고 삼켜진다),
        // 그 뒤 @00BA TurnOn. 즉 구독자가 이 파트를 읽기 전에 돈다(dnlib 2026-11-16).
        [JsonIgnore] internal bool RetiredAuxiliariesPending;

        protected override void OnPostLoad()
        {
            base.OnPostLoad();
            if (Auxiliaries == null) throw new InvalidOperationException("PackWarden: UnitPartPack.Auxiliaries is null after load (corrupt save?)");
            int saved = Auxiliaries.Count;
            Auxiliaries = new List<UnitReference>(PackRules.MigrateSavedSlots(Auxiliaries)); // 2칸(옛 저장)·4칸만 받는다. 그 밖은 던진다.
            if (saved != PackRules.AuxSlotCount)
                Main.Log("PackWarden: migrated a pre-2026-11-16 pack state of " + Owner + " from " + saved + " to " + PackRules.AuxSlotCount + " slots (old respawn timers are dropped)");
        }

        // 직렬화 사고로 길이가 다르면 조용히 맞추지 않고 실패(옛 저장의 2칸은 OnPostLoad 가 이미 늘렸다).
        internal void TrimForLevel(int target)
        {
            foreach (int index in PackRules.ExcessSlots(Auxiliaries, target, reference => !string.IsNullOrEmpty(reference.UniqueId)))
            {
                Auxiliaries[index] = default(UnitReference);
                RetiredAuxiliariesPending = true;
            }
        }

        public void Validate()
        {
            if (Auxiliaries == null)
                throw new InvalidOperationException("PackWarden: UnitPartPack.Auxiliaries is null (corrupt save?)");
            if (Auxiliaries.Count != PackRules.AuxSlotCount)
                throw new InvalidOperationException("PackWarden: UnitPartPack expects " + PackRules.AuxSlotCount + " slots, got Auxiliaries=" + Auxiliaries.Count);
        }

        public bool IsEmpty(int slot) { Validate(); return string.IsNullOrEmpty(Auxiliaries[slot].UniqueId); }

        public static bool IsLive(UnitEntityData u) { return u != null && !u.Destroyed && u.IsInGame && !u.State.IsDead; }

         
        // 판정 불가를 「없음」 으로 접지 않는다). 규칙 소비자는 SlotAt(4상태)·Tally 를 쓴다; 이것은 점검 하네스(Checks\Scenarios)용으로 남긴다.
        public UnitEntityData LiveAt(int slot)
        {
            UnitEntityData u;
            var st = SlotAt(slot, out u);
            if (st == PackRules.SlotState.Unresolved) throw new InvalidOperationException("PackWarden: pack slot " + slot + " holds auxiliary id '" + Auxiliaries[slot].UniqueId + "' that does not resolve to a unit (LiveAt cannot answer 'absent')");
            return st == PackRules.SlotState.Live ? u : null;
        }

         
        public PackRules.LiveTally Tally()
        {
            var states = new PackRules.SlotState[PackRules.AuxSlotCount];
            for (int i = 0; i < PackRules.AuxSlotCount; i++) { UnitEntityData u; states[i] = SlotAt(i, out u); }
            return PackRules.LiveTally.Of(states);
        }

        // 판정 불가 자리를 건너뛰는 소비자(상 전파·두뇌 맞춤·교전 관측)가 그 사실을 남기는 곳: (기능, 지도자, 자리 ID)별 첫 1회 오류 줄, 횟수는 매번 센다.
        // 「없음」 으로 기록하지 않는다 — 줄 문구가 판정 불가(UNDETERMINED)라고 밝힌다.
        public const string UnresolvedSkipLogHead = "PackWarden: pack slot UNDETERMINED ";
        public static int UnresolvedSkipCount;
        static readonly HashSet<string> unresolvedSkipNoted = new HashSet<string>();
        internal static void NoteUnresolvedSkip(string feature, UnitEntityData leader, UnitPartPack part, int slot)
        {
            UnresolvedSkipCount++;
            string id = part.Auxiliaries[slot].UniqueId;
            string who = PackEmpower.Who(leader);
            if (!unresolvedSkipNoted.Add(feature + "\u0001" + who + "\u0001" + id)) return;
            Main.Entry.Logger.Error(UnresolvedSkipLogHead + "feature=" + feature + " leader=" + who + " slot=" + slot + " id='" + id + "': the auxiliary id does not resolve to a unit; this slot is skipped as undetermined (not as absent) — repeats are counted only");
        }

        // 슬롯 상태 4가지(검토 2026-11-17 지적 2 — 판정은 순수 PackRules.ClassifySlot): Empty / Live(unit = 그 유닛) / Gone(해석됐지만 죽음·파괴·게임 밖) /
        // Unresolved(UniqueId 가 있는데 UnitReference.Value 가 null — 엔진은 해석 실패를 예외 없이 null 로 준다). Live 가 아니면 unit = null.
        public PackRules.SlotState SlotAt(int slot, out UnitEntityData unit)
        {
            Validate();
            unit = null;
            bool empty = IsEmpty(slot);
            var u = empty ? null : Auxiliaries[slot].Value;
            var state = PackRules.ClassifySlot(empty, u != null, u != null && IsLive(u));
            if (state == PackRules.SlotState.Live) unit = u;
            return state;
        }

         
        // LiveAt 과 다른 점은 IsInGame 을 보지 않는다는 것 하나(보이기 맞춤 ShadowPack.ReconcileVisibility 전용).
        public UnitEntityData PresentAt(int slot)
        {
            Validate();
            if (IsEmpty(slot)) return null;
            var u = Auxiliaries[slot].Value;
            return u != null && !u.Destroyed && !u.State.IsDead ? u : null;
        }

        // 확정된 살아 있는 보조 수. 판정 불가 자리가 있으면 던진다(PackRules.LiveTally.CompleteLiveCount — 하한을 정확한 수로 읽지 않는다). 규칙 소비자는 Tally 를 쓴다.
        public int LiveCount() { return Tally().CompleteLiveCount(); }

        // 그 유닛이 들어 있는 슬롯 번호, 이 지도자의 보조가 아니면 -1.
        public int SlotOf(UnitEntityData aux)
        {
            Validate();
            if (aux == null) throw new ArgumentNullException("aux");
            for (int i = 0; i < PackRules.AuxSlotCount; i++)
                if (!IsEmpty(i) && Auxiliaries[i].UniqueId == aux.UniqueId) return i;
            return -1;
        }

        public void Forget(UnitEntityData aux)
        {
            int slot = SlotOf(aux);
            if (slot < 0) throw new InvalidOperationException("PackWarden: Forget called for a unit that is not in the pack: " + aux);
            string id = Auxiliaries[slot].UniqueId; // SlotOf >= 0 이면 비어 있지 않다(IsEmpty = UniqueId 빈 문자열)
            Auxiliaries[slot] = default(UnitReference);
            RaiseSlotCleared(Owner, id);
        }

         
        // 없으면 무동작(점검이 아닐 때 정상). Harmony 아님. 비우는 곳은 Forget·PackCall.Dismiss 둘뿐(Auxiliaries[...] = default grep, 2026-11-14).
        public static event Action<UnitEntityData, string> SlotCleared;

        internal static void RaiseSlotCleared(UnitEntityData leader, string auxId)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            if (string.IsNullOrEmpty(auxId)) throw new ArgumentException("PackWarden: RaiseSlotCleared needs the cleared auxiliary id", "auxId");
            var h = SlotCleared;
            if (h != null) h(leader, auxId);
        }

        // 보조 사망(PackCallComponent.HandleUnitDeath): 이번 전투의 손실로 세고 슬롯을 비운다. 센 뒤 Forget 이 던지면(슬롯에 없는 유닛) 호출자가 미리 걸렀어야 한다.
        public void RecordDeathAndForget(UnitEntityData aux)
        {
            if (SlotOf(aux) < 0) throw new InvalidOperationException("PackWarden: death recorded for a unit that is not in the pack: " + aux);
            Losses.RecordDeath();
            Forget(aux);
        }

        // 전투 종료(지도자가 의식 있는 채 전투를 떠남 · 파티 전투 종료): 다음 전투에서는 전원이 다시 나타난다.
        public void EndCombat() { Losses.CombatEnded(); }
    }

    public static class PackCall
    {
        public static BlueprintFeature CallFeature, CallToggleFeature, CallMountedFeature;
        public static BlueprintActivatableAbility ToggleAbility;
        public static BlueprintBuff ToggleBuff;

        // SaddledUnitController.TickDismount 실행 중 = 탈것·기수 기절/사망 등 강제 하차(설계 4.5 "유지"). PackCallPatches 가 세우고 되돌린다.
        [ThreadStatic] public static bool SuppressDismiss;
        // 메인이 숨어 있어 소환을 건너뛴 수(진단).
        public static int SummonSkippedWhileHidden;

        static BlueprintCharacterClass hunter;
        static readonly HashSet<string> warnedNoLevel = new HashSet<string>();

        public static void Build()
        {
            // 아이콘: 본편 헌터 동물 초점 특성(Guids.Game.HunterAnimalFocusFeature)의 아이콘을 재사용.
            var icon = Get<BlueprintUnitFact>(Guids.Game.HunterAnimalFocusFeature).Icon;
            if (icon == null) throw new InvalidOperationException("PackWarden: HunterAnimalFocusFeature has no icon");

            ToggleBuff = Create<BlueprintBuff>("PackWardenCallToggleBuff", Guids.CallToggleBuff, b =>
            {
                Names(b, "CallToggleAbility", icon);
                b.IsClassFeature = true;
                b.FxOnStart = new PrefabLink();
                b.FxOnRemove = new PrefabLink();
                b.ResourceAssetIds = new string[0];
            });
            AddComponents(ToggleBuff, new PackToggleTurnOn());

            ToggleAbility = Create<BlueprintActivatableAbility>("PackWardenCallToggleAbility", Guids.CallToggleAbility, a =>
            {
                Names(a, "CallToggleAbility", icon);
                a.ActivationType = AbilityActivationType.Immediately;
                a.DeactivateImmediately = true;
                a.DoNotTurnOffOnRest = true;
                a.IsOnByDefault = false;
                a.ResourceAssetIds = new string[0];
                SetField(a, "m_Buff", Ref<BlueprintBuffReference>(Guids.CallToggleBuff));
            });
            if (ToggleAbility.Buff != ToggleBuff) throw new InvalidOperationException("PackWarden: ToggleAbility.Buff readback failed");

            CallToggleFeature = Feature("PackWardenCallToggleFeature", Guids.CallToggleFeature, "CallToggle", icon);
            var addFacts = new AddFacts();
            SetField(addFacts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.CallToggleAbility) });
            AddComponents(CallToggleFeature, addFacts);
            if (!addFacts.Facts.Contains(ToggleAbility)) throw new InvalidOperationException("PackWarden: CallToggleFeature AddFacts readback failed");

            CallMountedFeature = Feature("PackWardenCallMountedFeature", Guids.CallMountedFeature, "CallMounted", icon);
            var remove = new RemoveFeatureOnApply();
            SetField(remove, "m_Feature", Ref<BlueprintUnitFactReference>(Guids.CallToggleFeature));
            AddComponents(CallMountedFeature, remove);
            if (remove.Feature != CallToggleFeature) throw new InvalidOperationException("PackWarden: RemoveFeatureOnApply readback failed");

            CallFeature = Feature("PackWardenCallFeature", Guids.CallFeature, "Call", icon);
            // 그림자 타격(T3): 같은 특성의 두 번째 컴포넌트 = 전역 규칙 처리기(PackShadowStrikes — Harmony 아님).
            // 메인 부재 전환(T4): 세 번째 컴포넌트 = 펫 생사·소생·교체·지역 로드 감시자(PackModeWatcher — Harmony 아님).
            // 2026-11-16 가드: 네 번째 컴포넌트 = 교전 추종 관측(PackEngageWatcher — 지도자의 새 라운드마다, Harmony 아님).
            AddComponents(CallFeature, new PackCallComponent(), new PackShadowStrikes(), new PackModeWatcher(), new PackEngageWatcher());

            Archetype.AddAt(1, CallFeature);
            CallFeature.HideInUI = true;
            CallFeature.HideInCharacterSheetAndLevelUp = true;
            Archetype.AddAt(PackRules.MountedCallLevel, CallMountedFeature);
        }

        internal static void Names(BlueprintUnitFact f, string key, UnityEngine.Sprite icon)
        {
            Names(f, key + ".Name", key + ".Description", icon);
        }

        // 이름 키와 설명 키를 따로 받는다(2026-11-17): 선택지 특성의 여러 줄 설명이 같은 이름을 쓰는 능력의 툴팁까지 길게 만들지 않도록. Strings.L 은 없는 키에서 던진다(null 을 넣지 않는다).
        internal static void Names(BlueprintUnitFact f, string nameKey, string descriptionKey, UnityEngine.Sprite icon)
        {
            SetField(f, "m_DisplayName", Strings.L(nameKey));
            SetField(f, "m_Description", Strings.L(descriptionKey));
            SetField(f, "m_DescriptionShort", Strings.L(descriptionKey));
            SetField(f, "m_Icon", icon);
        }

        internal static BlueprintFeature Feature(string name, string guid, string key, UnityEngine.Sprite icon)
        {
            return Feature(name, guid, key + ".Name", key + ".Description", icon);
        }

        internal static BlueprintFeature Feature(string name, string guid, string nameKey, string descriptionKey, UnityEngine.Sprite icon)
        {
            return Create<BlueprintFeature>(name, guid, f =>
            {
                Names(f, nameKey, descriptionKey, icon);
                f.IsClassFeature = true;
                f.Ranks = 1;
                f.Groups = new FeatureGroup[0];
            });
        }

        public static int HunterLevel(UnitEntityData leader)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            if (hunter == null) hunter = Get<BlueprintCharacterClass>(Guids.Game.HunterClass); // 없으면 Get 이 예외
            return leader.Progression.GetClassLevel(hunter);
        }

        public static bool ShouldSummon(UnitEntityData leader)
        {
            int level = HunterLevel(leader);
            if (level <= 0)
            {
                // 이 컴포넌트는 아키타입 특성에만 있으므로 일어나면 안 된다. 소환은 하지 않고 한 번 경고.
                if (warnedNoLevel.Add(leader.UniqueId))
                    Main.Entry.Logger.Warning("PackWarden: " + leader + " has the pack call feature but Hunter level 0; not summoning");
                return false;
            }
            if (!leader.IsInCombat || !PackRules.UsesMountGate(level)) return false;
            var pet = leader.GetPet(PetType.AnimalCompanion);
            var mount = leader.Get<UnitPartRider>()?.SaddledUnit;
            return pet != null && ReferenceEquals(mount, pet);
        }

        // 멱등: 살아 있는 보조가 목표 수에 이르면 아무것도 안 한다. 목표 = PackRules.AuxCount(헌터 레벨) − 이번 전투에서 죽은 수(PackRules.SlotsToFill).
        // 소유자 결정 2026-11-16: 재소집 대기 없음 — 죽은 보조는 그 전투에서는 안 돌아오고(내렸다 다시 타도), 다음 전투에서 바로 돌아온다.
        public static void TrySummon(UnitEntityData leader)
        {
            if (!ShouldSummon(leader)) return;
            int level = HunterLevel(leader);
             
            if (!ShadowRules.SummonAllowed(ShadowPack.StateOf(leader))) { SummonSkippedWhileHidden++; return; }
            var part = leader.Ensure<UnitPartPack>();
            part.Validate();
            var live = new bool[PackRules.AuxSlotCount];
            for (int i = 0; i < PackRules.AuxSlotCount; i++)
            {
                UnitEntityData u;
                var st = part.SlotAt(i, out u);  
                live[i] = st == PackRules.SlotState.Live;
                // 비어 있지 않은데 살아 있지 않다 = 사망/파괴 이벤트를 놓쳤거나(Gone) 유닛이 해석되지 않는다(Unresolved). 그 죽음을 세지 못한 채 덮어쓰지 않도록 실패(예전과 같은 동작, 문구만 갈랐다).
                if (st == PackRules.SlotState.Gone)
                    throw new InvalidOperationException("PackWarden: pack slot " + i + " of " + leader + " references a dead or missing unit that was never forgotten");
                if (st == PackRules.SlotState.Unresolved)
                    throw new InvalidOperationException("PackWarden: pack slot " + i + " of " + leader + " holds auxiliary id '" + part.Auxiliaries[i].UniqueId + "' that does not resolve to a unit (undetermined — not overwritten)");
                if (st != PackRules.SlotState.Live && st != PackRules.SlotState.Empty) throw new InvalidOperationException("PackWarden: pack slot " + i + " state " + st + " is not handled");
            }
            foreach (int slot in PackRules.SlotsToFill(PackRules.AuxCount(level), live, part.Losses.Fallen))
            {
                var aux = AuxUnits.Summon(leader, slot);
                if (aux == null) throw new InvalidOperationException("PackWarden: AuxUnits.Summon returned null for slot " + slot);
                part.Auxiliaries[slot] = aux;
            }
        }

        // 보조 전부 파괴 표시, 슬롯 비움. 이번 전투에서 죽은 수(part.Losses)는 건드리지 않는다: 해산은 전투 종료가 아니다(전투 중 하차 → 재기승으로 죽은 보조가
        // 돌아오면 안 된다). 전투 종료 쪽 호출자(PackCallComponent)가 EndCombat 을 따로 부른다.
        public static void Dismiss(UnitEntityData leader)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            var part = leader.Get<UnitPartPack>();
            if (part == null) return; // 한 번도 소환한 적 없음 = 해산할 보조가 없음
            part.Validate();
            for (int i = 0; i < PackRules.AuxSlotCount; i++)
            {
                if (part.IsEmpty(i)) continue;
                string id = part.Auxiliaries[i].UniqueId; // IsEmpty 가 아니면 비어 있지 않다(PackCall.cs IsEmpty)
                var u = part.Auxiliaries[i].Value;
                if (u != null && !u.Destroyed)
                {
                    // 소멸 연출(2026-11-09): 파괴 표시 직전 1회. 사망한 보조(시체)·이미 표시된 유닛·메인과 함께 숨은 보조(게임 밖 — 보일 것이 없다)는 제외. 파괴 표시 자체는 그대로.
                    if (!u.State.IsDead && !u.DestroyMark && u.IsInGame) AuxUnits.PlayDismissFx(u);
                    u.MarkForDestroy();
                }
                part.Auxiliaries[i] = default(UnitReference);
                UnitPartPack.RaiseSlotCleared(leader, id);
            }
            // 풀 등록은 파괴 시 SummonPoolsManager.HandleUnitDestroyed 가 해제한다. 시체·추적이 끊긴 보조는 여기서 정리(과제 7).
            AuxUnits.DestroyUntracked(leader);
        }
    }

    // 지도자의 CallFeature 에 붙는다. TurnOn 때 게임이 EventBus.Subscribe(런타임)을 부르고, 런타임(ISubscriptionProxy)이
    // 이 델리게이트를 전역 구독자로 등록한다(EntityFactComponent.TurnOn → Subscribe, dnlib 확인). 이벤트마다 Owner 문맥이 설정된다.
    // 보조 사망은 전역 IUnitHandler.HandleUnitDeath 로 받는다. IUnitDieHandler 는 IUnitSubscriber(죽는 유닛 자신의 구독자에게만 감)라 못 쓴다.
    // 2026-11-16: 휴식 처리기(IRestFinishedHandler — 「휴식하면 대기 초기화」)는 재소집 대기와 함께 없앴다.
    public class PackCallComponent : UnitFactComponentDelegate, IUnitCombatHandler, IUnitHandler, IPartyCombatHandler, IGlobalSubscriber, IUnitLevelUpHandler
    {
        public void HandleUnitBeforeLevelUp(UnitEntityData unit) { }

        public void HandleUnitAfterLevelUp(UnitEntityData unit, Kingmaker.UnitLogic.Class.LevelUp.LevelUpController controller)
        {
            if (ReferenceEquals(unit, Owner)) PackCall.TrySummon(Owner);
        }

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
            if (ReferenceEquals(unit, Owner)) PackCall.TrySummon(Owner);
        }

        // 실시간 모드에서는 지도자가 의식을 잃어도 게임이 LeaveCombat 을 부른다(UnitLifeController.SetLifeState IL_0082~00A5).
        // 무리는 쓰러진 주인을 지켜야 하므로(컨트롤러 판정 2026-11-06) 그때는 해산하지 않는다.
        // IsConscious = LifeState == Conscious → 기절·사망 둘 다 제외(dnlib IL 확인). 정리는 파티 전투 종료가 맡는다.
        // 의식 있는 채 떠남 = 지도자에게 그 전투는 끝났다 → 이번 전투 손실도 지운다(다음 전투에 전원). 쓰러져서 떠난 경우는 무리도 손실 수도 그대로 —
        // 같은 전투에서 깨어나 다시 들어와도(HandleUnitJoinCombat → TrySummon) 죽은 보조는 돌아오지 않는다.
        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            if (ReferenceEquals(unit, Owner) && Owner.State.IsConscious) EndCombat();
        }

        // 해산 + 이번 전투 손실 지우기. 파트가 없으면 소집한 적이 없다 = 지울 손실도 없다(Dismiss 와 같은 읽기).
        void EndCombat()
        {
            PackCall.Dismiss(Owner);
            var part = Owner.Get<UnitPartPack>();
            if (part != null) part.EndCombat();
        }

        // 파티 전투 종료 = 무조건 해산. 지도자가 쓰러진 채 전투가 끝나도 "보조는 전투 밖에 없다"를 지킨다.
        // 게임은 Player.UpdateIsInCombat(현재 값)·UnitCombatLeaveController.TickGroups(false)·게임패드 UI 재설정(현재 값)에서 올린다. Dismiss 는 멱등.
        public void HandlePartyCombatStateChanged(bool inCombat)
        {
            if (!inCombat) EndCombat();
        }

        public void HandleUnitDeath(UnitEntityData unit)
        {
            var part = Owner.Get<UnitPartPack>();
            if (part == null) return; // 파트가 없으면 이 지도자의 보조도 없다
            if (part.SlotOf(unit) < 0) return; // 이 지도자의 보조가 아니다
            // 소유자 결정 2026-11-16: 대기 시각을 걸지 않는다. 이번 전투의 손실로만 센다(다음 전투에 바로 돌아온다).
            part.RecordDeathAndForget(unit);
        }

        // 사망 없이 사라진 보조(외부 파괴): 슬롯만 비운다. 손실로 세지 않는다(같은 전투에서 다시 소집될 수 있다 — 2026-11-16 이전의 「대기 없음」 과 같은 취급).
        public void HandleUnitDestroyed(UnitEntityData unit)
        {
            var part = Owner.Get<UnitPartPack>();
            if (part == null) return;
            if (part.SlotOf(unit) >= 0) part.Forget(unit);
        }

        // IUnitHandler 가 IUnitSpawnHandler 를 상속해서 구현해야 한다. 무리 규칙과 무관.
        public void HandleUnitSpawned(UnitEntityData unit) { }
    }

    // Retained for existing saves that reference the retired toggle component.
    public class PackToggleTurnOn : UnitBuffComponentDelegate { }
}
