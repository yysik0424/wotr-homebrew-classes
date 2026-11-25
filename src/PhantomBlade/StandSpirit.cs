using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;
using WotrClassMod.Common;

namespace PhantomBlade
{
     
    // 저장 제외: SceneEntitiesStateConverter.WriteJson IL 0008~002F 가 ShouldSerialize 를 통과한 엔티티만 쓰고, ShouldSerialize IL 0001~000E 는
    //   Get<UnitPartTrapActor>() 가 있으면 false. 그래서 SpawnUnit 바로 다음 명령으로 Ensure<UnitPartTrapActor>() 를 붙인다(시험이 IL 순서를 잠금).
    //   본편 선례 TrapObjectData.OnAreaDidLoad IL 006F SpawnUnit → 00B2 Ensure<UnitPartTrapActor>.
    // MarkExtra/IsInGame=false(같은 선례 007F·00C4)는 하지 않는다:
    //   - IsInGame=false: EntityDataBase.set_IsInGame IL 0024 View.UpdateViewActive → 뷰가 꺼진다(령이 안 보임). 본편 「숨김」의 정의도 !IsInGame(UnitIsHidden.CheckCondition IL 0011~0018).
    //   - MarkExtra(IsExtra): 저장과 무관(ShouldSerialize 가 읽지 않음). 소비자는 최적화뿐 — EntityVisibilityForPlayerController.Update IL 001B~002B(화면 밖이면 가시성 갱신 생략),
    //     SleepingUnitsController.ShouldBeSleeping IL 00B0~00C1(화면 밖이면 잠듦). 주인 곁에 붙어 다니는 령에 쓸 이유가 없다.
    // 상태 = 정적 ConditionalWeakTable 두 개(주인→령, 령→주인). 인스턴스 필드·세이브 필드 없음(StandRushState 선례).
     
    //   HoldingState·Game.State.Units 등록). 주인 기록의 상태 = 순수 SpiritRules.ClassifyTracked(없음/예약/생성 중/등록됨/낡음).
    //   - SpawnUnit 앞에서 주인을 예약한다(재진입 방어: 생성 도중 같은 주인의 Get/Ensure/Remove 는 Reserved 로 던진다).
    //   - 생성 중인 령도 Get 에서 「있음」(다음 이벤트가 Tick 전에 와도 지우고 다시 만들지 않는다).
    //   - 「정확히 하나」 단언은 등록이 관찰될 때: SpiritFollower 가 등록될 때까지 매 LateUpdate 에 ConfirmRegistration, 또는 등록 뒤의 Ensure(재사용) 경로.
    //     시간 제한은 두지 않는다 — EntityCreationController 는 Pause 모드에 없어(GameModesFactory.Initialize IL 1325~136D) 일시정지 중엔 등록이 미뤄진다.
    public static class StandSpirit
    {
        // Unit = null 이면 예약(SpawnUnit 진행 중). Target = 생성 목표 상태(그 지역의 MainState). Confirmed = 이 령에서 「정확히 하나」를 확인함.
        sealed class SpiritBox { public UnitEntityData Unit; public SceneEntitiesState Target; public bool Confirmed; }
        sealed class OwnerBox { public UnitEntityData Owner; }

        static readonly ConditionalWeakTable<UnitEntityData, SpiritBox> byOwner = new ConditionalWeakTable<UnitEntityData, SpiritBox>();
        static readonly ConditionalWeakTable<UnitEntityData, OwnerBox> bySpirit = new ConditionalWeakTable<UnitEntityData, OwnerBox>();




        // 저장 제외의 실행 단언(C5): 엔진의 판정 함수를 직접 부른다. private static(dnlib 2026-11-09).
         
        static readonly MethodInfo ShouldSerializeMethod = typeof(SceneEntitiesStateConverter).GetMethod("ShouldSerialize", BindingFlags.NonPublic | BindingFlags.Static);

        static void Forget(UnitEntityData owner)
        {
            SpiritBox b;
            if (!byOwner.TryGetValue(owner, out b)) return; // 정당한 무동작: 기록이 없다
            if ((object)b.Unit != null) bySpirit.Remove(b.Unit);
            byOwner.Remove(owner);
        }

        static bool IsTracked(UnitEntityData unit)
        {
            OwnerBox ob;
            SpiritBox sb;
            return bySpirit.TryGetValue(unit, out ob) && byOwner.TryGetValue(ob.Owner, out sb) && ReferenceEquals(sb.Unit, unit);
        }

        // 사라지는 중 = 파괴됨 ∨ 폐기됨 ∨ 파괴 표시. SpiritFollower 도 같은 정의를 쓴다(C4 — 한 곳).
        internal static bool Going(UnitEntityData u) { return u.Destroyed || u.IsDisposed || u.DestroyMark; }

        // 령의 주인이 사라지는 중인가. 표에 없는 령 = false 가 정당하다: KeepSpirit 은 tracked=false 로 이미 지운다(주인 판정이 필요 없음).
        static bool OwnerGoneOf(UnitEntityData spirit)
        {
            OwnerBox ob;
            if (!bySpirit.TryGetValue(spirit, out ob)) return false;
            return Going(ob.Owner);
        }

        // 살아 있음(등록된 유닛 기준 — DestroyUntracked 가 Game.State.Units 를 돌며 쓴다) = 파괴·폐기·파괴 표시가 아니고, 지금 불러온 지역의 주 상태에 들어 있다
        // (HoldingState: SceneEntitiesState.AddEntityData IL 006F). 불러온 지역 상태가 없으면(지역 전환·세이브 불러오기 중 Game.DisposeState IL 00C8) 그 안에 살아 있는 령도 없다 — 정당한 「아님」.
        static bool IsLive(UnitEntityData u, AreaPersistentState area)
        {
            return !Going(u) && area != null && area.MainState != null && ReferenceEquals(u.HoldingState, area.MainState);
        }

        // 주인 기록의 상태(순수 SpiritRules.ClassifyTracked 에 사실만 넘긴다). box = 기록(없으면 null).
        static SpiritRules.TrackedSpirit Classify(UnitEntityData owner, AreaPersistentState area, out SpiritBox box)
        {
            bool has = byOwner.TryGetValue(owner, out box);
            var u = has ? box.Unit : null;
            bool hasUnit = (object)u != null;
            var main = area == null ? null : area.MainState;
            var holding = hasUnit ? u.HoldingState : null;
            // The scene list owns membership. The global unit pool can still be rebuilding after a load.
            if (hasUnit && !Going(u) && ReferenceEquals(box.Target, main) && main != null &&
                (holding == null || ReferenceEquals(holding, main)) && !main.AllEntityData.Contains(u))
                return box.Confirmed ? SpiritRules.TrackedSpirit.Stale : SpiritRules.TrackedSpirit.Creating;
            return SpiritRules.ClassifyTracked(has, hasUnit, hasUnit && Going(u), holding != null,
                holding != null && main != null && ReferenceEquals(holding, main), has && main != null && ReferenceEquals(box.Target, main));
        }

        static Game RequireGame(string where)
        {
            var game = Game.Instance;
            if (game == null || game.State == null || game.EntityCreator == null) throw new InvalidOperationException("PhantomBlade: StandSpirit." + where + " without Game.Instance / State / EntityCreator");
            return game;
        }

        // 파티 공용 인벤토리(Player.Inventory). 없으면 던진다 — 비교 기준이 없으면 격리를 판정할 수 없다(C1).
        internal static ItemsCollection PartyInventory(Game game, string where)
        {
            var p = game.Player;
            if (p == null || p.Inventory == null || p.Inventory.Items == null) throw new InvalidOperationException("PhantomBlade: StandSpirit." + where + " without Game.Player.Inventory (cannot verify spirit inventory isolation)");
            return p.Inventory;
        }

        // 인벤토리 집계 = (항목 수, 수량 합). 점검 S12 의 전후 대조도 이 함수를 쓴다(C4).
        internal static (int entries, int total) Tally(ItemsCollection c)
        {
            if (c == null || c.Items == null) throw new ArgumentNullException("c");
            int total = 0;
            foreach (var it in c.Items)
            {
                if (it == null) throw new InvalidOperationException("PhantomBlade: inventory holds a null item entry (cannot count)");
                total += it.Count;
            }
            return (c.Items.Count, total);
        }

        static BlueprintUnit RequireBlueprint(string where)
        {
            var bp = SpiritBlueprints.SpiritUnit;
            if (bp == null) throw new InvalidOperationException("PhantomBlade: StandSpirit." + where + " before SpiritBlueprints.Build()");
            return bp;
        }

        // 령 블루프린트 판정은 AssetGuid 로(PackWarden 보조 판정 선례). 유닛의 블루프린트가 null 이면 령이 아니다(우리 령은 늘 SpiritUnit 으로 생성).
        static bool IsSpiritUnit(UnitEntityData u, BlueprintUnit bp)
        {
            var b = u.Blueprint;
            return b != null && b.AssetGuid == bp.AssetGuid;
        }

         
        // 호출자는 이 값으로 직접 분기하지 않고 SpiritRules.Resolve 의 currentSpirit 로만 넘긴다(StandSpiritLifecycle.Handle).
        public static UnitEntityData Get(UnitEntityData owner)
        {
            if ((object)owner == null) throw new ArgumentNullException("owner");
            SpiritBox box;
            if (!byOwner.TryGetValue(owner, out box)) return null; // 정당: 기록 없음(게임 상태를 읽을 필요도 없다)
            var game = RequireGame("Get");
            var st = Classify(owner, game.LoadedAreaState, out box);
            return SpiritRules.PresentForGet(st) ? box.Unit : null;
        }





        // 주인의 령을 하나 보장한다. 판정 = 순수 SpiritRules.EnsurePlan(생성 / 낡은 것 교체 / 생성 중 재사용 / 등록됨 재사용 + 정확히 하나 단언).
        public static UnitEntityData Ensure(UnitEntityData owner)
        {
            if ((object)owner == null) throw new ArgumentNullException("owner");
            var bp = RequireBlueprint("Ensure");
            if (ShouldSerializeMethod == null) throw new MissingMethodException("PhantomBlade: SceneEntitiesStateConverter.ShouldSerialize not found (save exclusion cannot be verified)");
            var game = RequireGame("Ensure");
            var area = game.LoadedAreaState;
            // Decide 가 areaLoaded=Yes 일 때만 여기 온다(StandSpiritLifecycle.AreaLoaded). 그런데 상태가 없으면 판정 입력과 실제가 어긋난 것 — 던진다.
            if (area == null || area.MainState == null) throw new InvalidOperationException("PhantomBlade: StandSpirit.Ensure with no loaded area state");

            SpiritBox box;
            var step = SpiritRules.EnsurePlan(Classify(owner, area, out box));
            switch (step)
            {
                case SpiritRules.EnsureStep.ReuseRegistered:
                    RequireSingle(game, bp, owner, box.Unit);
                    box.Confirmed = true;
                    if (box.Unit.View != null) Follower(box.Unit);
                    return box.Unit;
                case SpiritRules.EnsureStep.ReuseCreating:
                    return box.Unit; // 등록 전(다음 Tick) — 세지 않는다. 단언은 등록이 관찰될 때(ConfirmRegistration / 다음 Ensure).
                case SpiritRules.EnsureStep.ReplaceStale:
                {
                    // 옛 기록(지역이 바뀌었거나 엔진이 이미 지움): 기록을 버리고, 아직 남아 있으면 지운다.
                    var old = box.Unit;
                    Forget(owner);
                    if (!Going(old)) old.MarkForDestroy();
                    break;
                }
                case SpiritRules.EnsureStep.Create: break;
                default: throw new InvalidOperationException("PhantomBlade: StandSpirit.Ensure: unknown step " + step);
            }

            Vector3 op = owner.Position;
            float facing = owner.Orientation;
            var t = SpiritRules.FollowTarget(op.x, op.y, op.z, facing, SpiritRules.Dials.SpiritBehindMeters, SpiritRules.Dials.SpiritUpMeters);
            var pos = new Vector3((float)t.x, (float)t.y, (float)t.z);
            var rot = Quaternion.Euler(0f, facing, 0f);
            var creator = game.EntityCreator;
            var state = area.MainState;
            // 인벤토리 격리(결함 2026-11-14): 생성 전 파티 인벤토리를 센다. 생성 직후 같은 수여야 한다(SpiritRules.RequireNoInventoryLeak).
            var party = PartyInventory(game, "Ensure");
            var partyBefore = Tally(party);
             
            var mine = new SpiritBox { Unit = null, Target = state };
            byOwner.Add(owner, mine);
            UnitEntityData unit = null;
            try
            {
                // 아래 두 줄 사이에 아무것도 넣지 말 것(시험 SaveExclusionOrderViolation 이 IL 로 잠금): 생성 즉시 저장 제외 부품.
                // SpawnUnit 이 null 을 돌려주면(EntityCreationController.SpawnUnit IL 0000~006B 의 오류 기록 후 ret) 다음 줄이 NullReferenceException → catch 가 감싸 다시 던진다.
                unit = creator.SpawnUnit(bp, pos, rot, state, null);
                unit.Ensure<UnitPartTrapActor>();
                if ((object)unit.Get<UnitPartTrapActor>() == null) throw new InvalidOperationException("UnitPartTrapActor readback failed");
                // C5: 엔진이 이 령을 저장하지 않는다는 것을 엔진 함수로 확인. 대조군 = 주인(살아 있고 함정 부품 없음 → true). 대조가 거짓이면 이 탐침을 믿을 수 없다.
                // 판정은 순수 SpiritRules.RequireSaveExcluded(시험이 실행). 인자 순서(령, 주인)는 시험이 IL 로 잠금(검토 M1).
                bool spiritSerialized = (bool)ShouldSerializeMethod.Invoke(null, new object[] { unit });
                bool ownerSerialized = (bool)ShouldSerializeMethod.Invoke(null, new object[] { owner });
                SpiritRules.RequireSaveExcluded(spiritSerialized, ownerSerialized);

                // 인벤토리 격리(결함 2026-11-14). Player 진영 유닛은 파티 인벤토리를 공유한다(UnitDescriptor..ctor IL 00B4~00D8) →
                //   자기 인벤토리로 바꾼다: EnsureOwnInventory IL 0000~0016(IsPlayerInventory 이면 HasOwnInventory=true → SetupInventory(false)
                //   IL 0029~0049 새 ItemsCollection, 0097 대입). 그 뒤 인벤토리를 다시 가리키는 엔진 경로는 SwitchFactions·FixInventoryOnPlayerPostLoad·
                //   SetupSharedInventoryForPet 뿐(호출자 역추적 2026-11-14) — 령은 진영을 바꾸지 않고, 저장되지 않아 불러오지 않으며, 펫이 아니다.
                //   블루프린트에 아이템이 없으므로(SpiritBlueprints Body 사본) 옮겨질 것도 없다. 어긋나면 던진다 → 아래 catch 가 령을 지운다.
                unit.Descriptor.EnsureOwnInventory();
                var own = unit.Inventory;
                if (own == null) throw new InvalidOperationException("spirit has no inventory after EnsureOwnInventory");
                var partyAfter = Tally(party);
                SpiritRules.RequireNoInventoryLeak(partyBefore.entries, partyBefore.total, partyAfter.entries, partyAfter.total, ReferenceEquals(own, party) || own.IsPlayerInventory, own.Items.Count);

                var view = unit.View;
                if (view == null) throw new InvalidOperationException("spawned spirit has no view");
                // 정찰 2(c): 충돌체(UnitEntityView.ActivateColliders IL 0000~0032)와 이동 장애물(UnitMovementAgentBase.OnDisable → AllAgents 에서 빠짐)을 끈다.
                view.ActivateColliders(false);
                var agent = view.MovementAgent;
                if (agent == null) throw new InvalidOperationException("spirit view has no MovementAgent");
                agent.enabled = false;
                view.gameObject.AddComponent<SpiritFollower>().Init(owner);
                 
                mine.Unit = unit;
                bySpirit.Remove(unit);
                bySpirit.Add(unit, new OwnerBox { Owner = owner });



                return unit;
            }
            catch (Exception e)
            {
                // 반쯤 만든 령을 남기지 않는다(PackWarden AuxUnits.Summon catch 선례). 예약도 지운다(이 Ensure 의 것일 때만). 예외는 원인과 함께 다시 던진다.
                SpiritBox cur;
                if (byOwner.TryGetValue(owner, out cur) && ReferenceEquals(cur, mine)) byOwner.Remove(owner);
                if ((object)unit != null) bySpirit.Remove(unit);
                if ((object)unit != null && !Going(unit)) unit.MarkForDestroy();
                throw new InvalidOperationException("PhantomBlade: StandSpirit.Ensure failed for " + owner + ((object)unit == null ? " (SpawnUnit returned null or threw; see the engine log)" : "") + ": " + e.Message, e);
            }
        }

         
        // 판정 = 순수 SpiritRules.RegistrationStep. 등록되면 한 번 「정확히 하나」를 단언한다. 추적이 깨졌으면 던진다(따라가기가 오류 기록 후 꺼진다 — 시끄럽게).
        public static bool ConfirmRegistration(UnitEntityData owner, UnitEntityData spirit)
        {
            if ((object)owner == null) throw new ArgumentNullException("owner");
            if ((object)spirit == null) throw new ArgumentNullException("spirit");
            var bp = RequireBlueprint("ConfirmRegistration");
            var game = RequireGame("ConfirmRegistration");
            SpiritBox box;
            var st = Classify(owner, game.LoadedAreaState, out box);
            bool tracked = box != null && ReferenceEquals(box.Unit, spirit);
            var check = SpiritRules.RegistrationStep(st, tracked && box.Confirmed, tracked);
            switch (check)
            {
                case SpiritRules.RegistrationCheck.Done: return true;
                case SpiritRules.RegistrationCheck.Wait: return false;
                case SpiritRules.RegistrationCheck.AssertSingle:
                    RequireSingle(game, bp, owner, spirit);
                    box.Confirmed = true;
                    ModContext.Log("PhantomBlade: spirit of " + owner.CharacterName + " registered (exactly one live spirit confirmed)");
                    return true;
                default: throw new InvalidOperationException("PhantomBlade: StandSpirit.ConfirmRegistration: unknown check " + check);
            }
        }

        // 이 주인에게 묶인 살아 있는 령 유닛이 정확히 하나이고 그것이 expected 인지(이중 생성 검출). 전수 = Game.State.Units(등록된 유닛만 — 등록이 관찰된 뒤에만 부른다).
        static void RequireSingle(Game game, BlueprintUnit bp, UnitEntityData owner, UnitEntityData expected)
        {
            int n = 0;
            bool sawExpected = false;
            foreach (var entity in game.LoadedAreaState.MainState.AllEntityData)
            {
                var u = entity as UnitEntityData;
                if ((object)u == null || !IsSpiritUnit(u, bp) || Going(u)) continue; // 정당한 제외: 다른 유닛·이미 지워지는 유닛
                OwnerBox ob;
                if (!bySpirit.TryGetValue(u, out ob) || !ReferenceEquals(ob.Owner, owner)) continue; // 다른 주인의 령
                n++;
                if (ReferenceEquals(u, expected)) sawExpected = true;
            }
            SpiritRules.RequireExactlyOne(n, sawExpected, owner.ToString());
        }

        internal static SpiritFollower Follower(UnitEntityData spirit)
        {
            OwnerBox binding;
            var view = spirit.View;
            if (!bySpirit.TryGetValue(spirit, out binding) || view == null || !ReferenceEquals(view.EntityData, spirit))
                throw new InvalidOperationException("PhantomBlade: spirit view and tracked owner are not ready");
            foreach (var follower in view.GetComponents<SpiritFollower>())
            {
                if (follower.BelongsTo(binding.Owner, spirit)) return follower;
                follower.Retire();
            }
            view.ActivateColliders(false);
            if (view.MovementAgent == null) throw new InvalidOperationException("PhantomBlade: replacement spirit view has no movement agent");
            view.MovementAgent.enabled = false;
            var replacement = view.gameObject.AddComponent<SpiritFollower>();
            replacement.Init(binding.Owner);
            return replacement;
        }

        // 주인의 령을 지운다. 기록이 없으면 정당한 무동작(이미 지웠거나 만든 적 없음). 예약 중(생성 도중 재진입)이면 던진다.
        public static void Remove(UnitEntityData owner)
        {
            if ((object)owner == null) throw new ArgumentNullException("owner");
            SpiritBox box;
            if (!byOwner.TryGetValue(owner, out box)) return;
            var unit = box.Unit;
            if ((object)unit == null) throw new InvalidOperationException("PhantomBlade: StandSpirit.Remove while the spirit's creation is in progress (reentrant lifecycle event)");
            Forget(owner);
            if (!Going(unit)) unit.MarkForDestroy();
        }

        // 주인이 폐기·파괴됐는데 령이 남은 경우(SpiritRules.FollowerCheck = RemoveOrphan) SpiritFollower 가 부른다. 이 령을 가리키는 기록만 버린다:
        // 주인 → 령 기록이 다른 령을 가리키면 그 기록은 건드리지 않는다(Remove(owner) 를 쓰지 않는 이유 — 남의 령을 지울 수 있음).
        public static void RemoveOrphan(UnitEntityData spirit)
        {
            if ((object)spirit == null) throw new ArgumentNullException("spirit");
            OwnerBox ob;
            if (bySpirit.TryGetValue(spirit, out ob))
            {
                SpiritBox sb;
                if (byOwner.TryGetValue(ob.Owner, out sb) && ReferenceEquals(sb.Unit, spirit)) Forget(ob.Owner);
                else bySpirit.Remove(spirit);
            }
            if (!Going(spirit)) spirit.MarkForDestroy();
            ModContext.Log("PhantomBlade: StandSpirit.RemoveOrphan destroyed a spirit whose owner is gone");
        }

        // 표에 없거나 지금 지역 밖에 묶인 령 유닛을 지운다(PackWarden AuxUnits.DestroyUntracked 선례). 건수를 매번 기록하고, 지운 것이 있으면 오류로 남긴다.
        public static void DestroyUntracked()
        {
            var bp = RequireBlueprint("DestroyUntracked");
            var game = RequireGame("DestroyUntracked");
            var area = game.LoadedAreaState;
            var doomed = new List<UnitEntityData>();
            int kept = 0;
            foreach (var u in game.State.Units)
            {
                if ((object)u == null || !IsSpiritUnit(u, bp) || Going(u)) continue; // 정당한 제외: 다른 유닛·이미 지워지는 유닛
                // 주인이 폐기·파괴됐으면(EntityDataBase.Dispose 는 사실을 Deactivate 하지 않음, 검토 M3) 령도 지운다. 유지 판정은 순수 SpiritRules.KeepSpirit,
                // 세 인자는 이 순서의 호출 결과를 그대로 넘긴다(시험이 IL 로 잠금, 검토 M3-r).
                if (SpiritRules.KeepSpirit(IsTracked(u), IsLive(u, area), OwnerGoneOf(u))) { kept++; continue; }
                doomed.Add(u);
            }
            foreach (var u in doomed)
            {
                OwnerBox ob;
                if (bySpirit.TryGetValue(u, out ob)) Forget(ob.Owner);
                u.MarkForDestroy();
            }
            ModContext.Log("PhantomBlade: StandSpirit.DestroyUntracked kept " + kept + ", destroyed " + doomed.Count);
            if (doomed.Count > 0) ModContext.LogError("PhantomBlade: StandSpirit.DestroyUntracked destroyed " + doomed.Count + " spirit unit(s) that were untracked or outside the loaded area (a spirit escaped tracking)");
        }
    }

    // StandBuff 컴포넌트. 버프가 켜져 있는 동안 전역 이벤트를 받는다:
    //   EntityFactComponent.TurnOn IL 0019 Subscribe → EventBus.Subscribe IL 0000~0016(ISubscriptionProxy.GetSubscriber = 이 델리게이트, 0001~0006)
    //   → IGlobalSubscriber 이면 GlobalSubscribers(EventBus.Subscribe(IGlobalSubscriber,…) IL 0004~000B). 네 인터페이스 모두 IGlobalSubscriber 를 상속(dnlib).
    //   이벤트 실행 시 SubscriptionManager.ExecuteAction IL 0017~002A 가 RequestEventContext 로 Owner/Fact 문맥을 세운다(PackWarden PackPetAspectWatcher·StandRushState 선례).
    // 이 블루프린트 컴포넌트 인스턴스는 모든 귀검사가 공유한다 → 인스턴스 필드 금지, 상태는 StandSpirit 의 정적 표.
    // 예외: 엔진이 잡아 기록한다(ComponentRuntime.OnActivate IL 0020~002D, SubscriptionManager.RaiseEvent IL 00D9~00F1 "Exception in subscriber").
    //   Handle 은 ModContext.LogError 로 모드 로그에 먼저 남기고 그대로 다시 던진다(삼키지 않음).
    public class StandSpiritLifecycle : UnitFactComponentDelegate, IAreaHandler, IUnitLifeStateChanged, IInGameHandler, IGameModeHandler
    {
        protected override void OnActivate() { Handle(SpiritRules.LifecycleEvent.Activate); }
        protected override void OnTurnOn() { Handle(SpiritRules.LifecycleEvent.TurnOn); }
        protected override void OnTurnOff() { Handle(SpiritRules.LifecycleEvent.TurnOff); }
        protected override void OnDeactivate() { Handle(SpiritRules.LifecycleEvent.Deactivate); }
        public void OnAreaBeginUnloading() { Handle(SpiritRules.LifecycleEvent.AreaBeginUnloading); }
        public void OnAreaDidLoad() { Handle(SpiritRules.LifecycleEvent.AreaDidLoad); }
        // 전역 이벤트: 모든 유닛·엔티티의 변화가 온다. 주인 것이 아니면 계획 표가 Ignore(정당한 무동작) — null 은 주인일 수 없다(주인은 null 이 아님, Handle 첫 줄).
        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState) { Handle(ReferenceEquals(unit, Owner) ? SpiritRules.LifecycleEvent.OwnerLifeStateChanged : SpiritRules.LifecycleEvent.OtherUnitLifeStateChanged); }
        public void HandleObjectInGameChanged(EntityDataBase entityData) { Handle(ReferenceEquals(entityData, Owner) ? SpiritRules.LifecycleEvent.OwnerInGameChanged : SpiritRules.LifecycleEvent.OtherEntityInGameChanged); }
        public void OnGameModeStart(GameModeType gameMode) { Handle(SpiritRules.LifecycleEvent.GameModeChanged); }
        public void OnGameModeStop(GameModeType gameMode) { Handle(SpiritRules.LifecycleEvent.GameModeChanged); }

        void Handle(SpiritRules.LifecycleEvent e)
        {
            UnitEntityData owner = null;
            try
            {
                owner = Owner;
                if ((object)owner == null) throw new InvalidOperationException("no Owner in the component event context");
                var o = owner;
                SpiritRules.SpiritAction act;
                // 판정 조립은 전부 순수 SpiritRules.Resolve(시험이 같은 함수를 실행, 검토 I1). 여기는 위임만 넘긴다.
                var effect = SpiritRules.Resolve(e, StandSpirit.DestroyUntracked, () => StandSpirit.Get(o), () => ReadInputs(o), out act);
                // 결과 적용도 순수 SpiritRules.Apply(검토 I1-r): Ensure 자리 = StandSpirit.Ensure, Remove 자리 = StandSpirit.Remove(시험이 IL 로 잠금).
                SpiritRules.Apply(effect, () => StandSpirit.Ensure(o), () => StandSpirit.Remove(o));
            }
            catch (Exception ex)
            {
                ModContext.LogError("PhantomBlade: StandSpiritLifecycle " + e + " failed for " + ((object)owner == null ? "?" : owner.CharacterName) + ": " + ex);
                throw;
            }
        }

        // 다섯 입력을 이름 순서대로 읽는다(시험이 IL 호출 순서를 잠금).
        SpiritRules.LifecycleInputs ReadInputs(UnitEntityData owner)
        {
            return new SpiritRules.LifecycleInputs(StandOn(), OwnerConscious(owner), OwnerInGame(owner), AreaLoaded(), OwnerHiddenByEngine());
        }

        // ---- 다섯 입력(Tri). 원천 객체가 없으면 Unknown → Decide 가 입력 이름과 함께 던진다(C1). ----

        // 스탠드 켜짐 = 이 컴포넌트의 사실(StandBuff)이 활성. EntityFact.Activate IL 00AC set_IsActive(true) 가 컴포넌트 Activate(0121)보다 먼저다.
        SpiritRules.Tri StandOn()
        {
            var f = Fact;
            if (f == null) return SpiritRules.Tri.Unknown;
            return f.IsActive ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }

        // 의식 = UnitState.IsConscious. 생명 상태 이벤트는 값이 바뀐 뒤에 온다(UnitLifeController.SetLifeState IL 0079 UpdateLifeState → 014B Raise).
        static SpiritRules.Tri OwnerConscious(UnitEntityData owner)
        {
            var st = owner.State;
            if (st == null) return SpiritRules.Tri.Unknown;
            return st.IsConscious ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }

        // 게임 안 = EntityDataBase.IsInGame(본편 숨김 조건 UnitIsHidden 의 정의). 값 대입 뒤 IInGameHandler 가 온다(set_IsInGame IL 000B → 0036).
        static SpiritRules.Tri OwnerInGame(UnitEntityData owner)
        {
            return owner.IsInGame ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }

        // 지역 로드됨 = 장면 로더의 지역이 있고(Game.get_CurrentlyLoadedArea IL 0000~000B), 불러온 지역 상태가 그 지역의 것이며 주 상태가 있다.
        //   지역 없음 = 정당한 No(SceneLoader.ClearLoadedArea·UnloadAreaCoroutine IL 031D 가 지움). 지역은 있는데 상태가 없거나 다른 지역 것 =
        //   LoadAreaCoroutine IL 0091(지역 대입)과 056D(상태 대입) 사이·Game.DisposeState IL 00C8 뒤 — 아직 로드가 끝나지 않은 정당한 No.
        //   상태가 있는데 청사진을 못 찾음 = 깨진 상태 → Unknown.
        static SpiritRules.Tri AreaLoaded()
        {
            var game = Game.Instance;
            if (game == null || game.State == null) return SpiritRules.Tri.Unknown;
            var loaded = game.CurrentlyLoadedArea;
            if (loaded == null) return SpiritRules.Tri.No;
            var las = game.LoadedAreaState;
            if (las == null || las.MainState == null) return SpiritRules.Tri.No;
            var lbp = las.Blueprint;
            if (lbp == null) return SpiritRules.Tri.Unknown;
            return lbp.AssetGuid == loaded.AssetGuid ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }

        // 엔진이 주인을 숨김 = 잠금 컷신 ∨ 지역이 파티를 숨김(전역 지도). 결합은 순수 SpiritRules.OwnerHiddenByEngine(검토 I2).
         
        //   전역 지도 진입/이탈은 지역 로드다(GlobalMapView 등 → Game.LoadArea → AfterSceneLoaderFinishedArea IL 00BC~00FB StartMode(AreaStatGameMode:
        //   IsGlobalMap 이면 GlobalMap, get_AreaStatGameMode IL 0000~000E) → IGameModeHandler, 이어서 015E OnAreaDidLoad). 떠날 때는 OnAreaBeginUnloading 이 제거.
        internal static SpiritRules.Tri OwnerHiddenByEngine()
        {
            return SpiritRules.OwnerHiddenByEngine(CutsceneLocked(), AreaHidesParty());
        }

        // 지역이 파티를 숨김 = BlueprintArea.HideParty(get_HideParty IL 0000~0006 = IsGlobalMap; 본편 GameHelper.HideParty IL 0000~001B 와 같은 판정,
        //   SleepingUnitsController.ShouldBeSleeping IL 0046~005B 가 이것으로 동료를 재움). 지역 없음 = No: 숨길 지역 규칙이 없다(생성은 areaLoaded=No 가 막음).
        static SpiritRules.Tri AreaHidesParty()
        {
            var game = Game.Instance;
            if (game == null) return SpiritRules.Tri.Unknown;
            var area = game.CurrentlyLoadedArea;
            if (area == null) return SpiritRules.Tri.No;
            return area.HideParty ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }

        // 잠금 컷신 = Game.CutsceneLock.Active(get_Active IL 0000~000B CountingGuard). 주인 숨김 자체(IsInGame=false, HideUnit.RunAction IL 0050·005A)는
        //   OwnerInGame 이 따로 본다. 잠금 변화는 게임 모드 Cutscene 시작/끝으로 온다(CutsceneLock.UpdateModeForCutsceneLock IL 0070~007C).
        static SpiritRules.Tri CutsceneLocked()
        {
            var game = Game.Instance;
            if (game == null) return SpiritRules.Tri.Unknown;
            var lk = game.CutsceneLock;
            if (lk == null) return SpiritRules.Tri.Unknown;
            return lk.Active ? SpiritRules.Tri.Yes : SpiritRules.Tri.No;
        }
    }

    // The absorption pool belongs to the permanent feature, including when the phantom is dismissed.
    public sealed class AbsorptionPoolOnLoad : UnitFactComponentDelegate<AbsorptionPoolOnLoad.Migration>, IAreaHandler, IGlobalSubscriber
    {
        public sealed class Migration
        {
            [Newtonsoft.Json.JsonProperty] public bool Combined;
        }

        public void OnAreaBeginUnloading() { }
        public void OnAreaDidLoad()
        {
            var pool = StandBlueprints.PoolResource;
            int excess = Owner.Resources.GetResourceAmount(pool) - pool.GetMaxAmount(Owner);
            if (excess > 0) Owner.Resources.Spend(pool, excess);

            var legacy = StandBlueprints.LegacyFullAttackResource;
            var shared = StandBlueprints.GhostSlashResource;
            var resources = Owner.Resources;
            if (Data.Combined || !resources.ContainsResource(legacy) || !resources.ContainsResource(shared)) return;
            int available = resources.GetResourceAmount(shared);
            int carry = GhostSlash.SharedRemaining(shared.GetMaxAmount(Owner), available, resources.GetResourceAmount(legacy));
            if (carry < available) resources.Spend(shared, available - carry);
            Data.Combined = true;
            resources.Remove(legacy);
        }
    }
}
