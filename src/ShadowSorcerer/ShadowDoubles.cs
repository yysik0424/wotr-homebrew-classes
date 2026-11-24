using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;
using ModContext = WotrClassMod.Common.ModContext;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ShadowSorcerer
{
    // 시각 전용 그림자 유닛의 묶음(본체마다 분신 수만큼) — 생성·숨김·정리(소유자 결정 2026-11-16 밤 1·2, 정찰 §3.3·§5.1·§7-2).
    // 선례 = 귀검사 령(phantom-blade StandSpirit.cs). 그쪽이 겪은 함정에 대한 방어:
    //   저장 흔적      SpawnUnit 바로 다음 명령으로 Ensure<UnitPartTrapActor>()(엔진 SceneEntitiesStateConverter.ShouldSerialize 가 그 부품이 있으면 저장하지 않는다) + 엔진 판정 함수로 실행 단언.
    //   인벤토리 유출  블루프린트 Body 에서 아이템 제거(ShadowUnitBlueprint) + 생성 직후 EnsureOwnInventory + 생성 전후 파티 인벤토리 집계 단언(2026-11-14 귀검사 실사고).
    //   지연 등록      SpawnUnit 은 대기열에만 넣고 다음 엔진 틱에 Game.State.Units 에 등록된다 → 「정확히 N」은 등록이 관찰된 뒤에만 센다(등록 전에 세면 0 개라 첫 생성마다 지우게 된다).
    //   길막·충돌      충돌체 끔 + 이동 에이전트 끔(엔진이 되켜서 매 틱 다시 끈다).
    //   숨김           UnitMechanicFeatures.Hidden 깃발(엔진 가시성 판정의 입력)을 Retain / Release — 드러낼 때 SetVisible 을 직접 부르지 않는다(다음 틱에 엔진이 되돌려 깜빡인다).
    //   범위 주문 격리 숨긴 동안은 유닛을 「게임 밖」(EntityDataBase.IsInGame = false)에 둔다(독립 검토 F1, 2026-11-17).
    //                  배경: 원뿔·선(AbilityDeliverProjectile.StepBasedDeliver IL_014F~0214)·연쇄(AbilityDeliverChain.SelectNextTargetByMinDistance)·튕김은 Game.State.Units 를 돌며
    //                  「죽지 않음·시전자 아님·기하」만 보고 대상 불가(IsUntargetable)를 보지 않는다 → 본체 곁에 숨어 있는 그림자가 누구의 범위 주문에든 낄 수 있었다.
    //                  엔진의 유닛 열거자(EntityPoolEnumerator.MoveNext IL_001A)는 ShouldBeEnumeratedByEntityPoolEnumerator(= m_IsInGame && !m_Suppressed)가 거짓인 유닛을 건너뛰고,
    //                  뷰의 범위 판정용 충돌체(SphereBoundsCollider)도 같은 값으로 꺼진다(EntityViewBase.UpdateViewActive IL_006B~0083). 본편이 숨긴 펫·숨겨서 부른 유닛에 쓰는 상태다
    //                  (UnitPartPet.SetHidden IL_002E · EntityCreationController.SpawnUnit IL_0200~0208 SpawnHidden).
    //                  「먼 좌표에 둔다」는 안은 쓰지 않았다: 주문 사거리에 상한이 없다(BlueprintAbility.GetRange IL_00D2 — AbilityRange.Unlimited = +∞ 피트, CustomRange 는 블루프린트 값)
    //                  — 어떤 유한 좌표도 「닿지 않는다」를 보장하지 못한다.
    //                  드러나 있는 몇 초(등장 → 흩어짐)는 게임 안이다 — 그동안의 노출은 남는다(README 알려진 한계). 감시선(Maintain · ShadowTripwireComponent)이 흔적을 보고한다.
    // 평소에는 숨겨 둔다(메아리 때만 드러낸다 — 소유자 결정 2). 미리 만들어 두는 이유: 생성은 지연 등록 + 프리팹 번들 동기 로드라 메아리 순간에 만들면 늦는다(정찰 §5.1).
    // 계약: 공개 진입점은 던지지 않는다. 실패 = 오류 줄 + 그 지역에서는 다시 시도하지 않음 + 메아리는 연출 없이 그대로.
    // 상태 = 정적 표(비저장). 본체·그림자 어느 쪽에도 우리 사실·부품·저장 필드를 새로 달지 않는다(그림자의 UnitPartTrapActor 는 엔진 부품이고 그 유닛은 저장되지 않는다).
    public static class ShadowDoubles
    {
        // Units 가 비어 있고 FailedIn 이 있으면 「이 지역에서 생성 실패」 표식.
        // HiddenByUs = 의도 장부(우리가 숨겨 두려는가) · Retained = 실제로 건 Hidden 깃발 Retain 장부(짝 맞추기용 — 의도와 따로, 2차 독립 검토 중요-2).
        // BuffsAtRegistration / BuffRefsAtRegistration = 등록이 확인된 순간의 버프 집합(guid · 블루프린트 참조). 확인 전에는 null(감시선을 돌리지 않는다 — 중요-3).
        sealed class Pool { public UnitEntityData[] Units = new UnitEntityData[0]; public bool[] HiddenByUs = new bool[0]; public bool[] Retained = new bool[0]; public string[][] BuffsAtRegistration = new string[0][]; public object[][] BuffRefsAtRegistration = new object[0][]; public SceneEntitiesState Target; public bool Confirmed; public bool Failed; public SceneEntitiesState FailedIn; }
        sealed class OwnerBox { public UnitEntityData Owner; }
        // 본체가 지금 지역에서 묶음을 만든 횟수 · 숨긴 그림자를 게임 밖으로 다시 돌린 횟수(묶음 기록과 따로 — 기록은 지울 때마다 사라진다).
        sealed class Budget { public readonly WeakReference Area = new WeakReference(null); public int Creations; public int Restores; }

        static readonly ConditionalWeakTable<UnitEntityData, Pool> byOwner = new ConditionalWeakTable<UnitEntityData, Pool>();
        static readonly ConditionalWeakTable<UnitEntityData, OwnerBox> byShadow = new ConditionalWeakTable<UnitEntityData, OwnerBox>();
        static readonly WotrClassMod.Common.WeakKeyTable<UnitEntityData, Budget> budgets = new WotrClassMod.Common.WeakKeyTable<UnitEntityData, Budget>();
        static readonly List<UnitEntityData> watched = new List<UnitEntityData>();
        static UnitEntityData[] watchedSnapshot;
        static AreaHook areaHook;
        static SceneEntitiesState scannedFor; // 파티를 마지막으로 훑은 지역 주 상태(비저장)

        // 엔진의 저장 판정 함수(private static — 시험이 DLL 지문으로 존재·서명을 잠근다).
        static readonly MethodInfo ShouldSerializeMethod = typeof(SceneEntitiesStateConverter).GetMethod("ShouldSerialize", BindingFlags.NonPublic | BindingFlags.Static);

        // 점검용 계수.
        public static int Created { get; private set; }
        public static int Destroyed { get; private set; }
        public static int UntrackedDestroyed { get; private set; }
        // 감시선이 본 누출 수 — (유닛, 종류, 세부)마다 1회(LeakTally, 2026-11-17: 계속 남는 누출을 매 틱 세던 것을 고침 — 점검 S15 「495」). 보고(ReportOnce)는 종류당 1회 그대로.
        static readonly LeakTally leakTally = new LeakTally();



        public static int EngineDefaultRebaselines { get; private set; } // 엔진 기본 토글 버프(ShadowDoubleRule.EngineDefaultToggleBuffs)만 달라 감시선 기준을 옮긴 횟수(진단)
        public static int ExposedRestores { get; private set; }  // 게임 안에 돌아온 숨긴 그림자를 다시 게임 밖으로 돌린 횟수(2차 독립 검토 중요-2)
        public static int RuleTargetHits { get; private set; }   // 그림자 유닛이 규칙의 당사자가 된 횟수(ShadowTripwireComponent)

        internal static bool Going(UnitEntityData u) { return u.Destroyed || u.IsDisposed || u.DestroyMark; }

        public static bool IsShadow(UnitEntityData u)
        {
            OwnerBox b;
            return (object)u != null && byShadow.TryGetValue(u, out b);
        }

        // ---- 감시 대상(메아리 특성 보유자) ----
        // 메아리 특성이 켜질 때(ShadowEchoComponent.OnTurnOn) · 메아리가 대기열에 들어갈 때 · 지역을 불러왔을 때(파티 전수) 부른다. 던지지 않는다.
        public static void Watch(UnitEntityData owner)
        {
            try
            {
                if ((object)owner == null) throw new ArgumentNullException("owner");
                if (IsShadow(owner)) return; // 정당한 무동작: 그림자 유닛 자신(메아리 특성이 없지만 방어)
                if (!watched.Contains(owner))
                {
                    watched.Add(owner);
                    watchedSnapshot = null;
                }
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "watch-" + e.GetType().Name, "shadow doubles: watch failed (" + e.GetType().Name + ": " + e.Message + ")"); }
        }

        // 메아리 특성이 꺼질 때. 그 본체의 그림자를 지운다. 던지지 않는다.
        public static void Unwatch(UnitEntityData owner)
        {
            try
            {
                if ((object)owner == null) throw new ArgumentNullException("owner");
                ForgetOwner(owner);
                Remove(owner);
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "unwatch-" + e.GetType().Name, "shadow doubles: unwatch failed (" + e.GetType().Name + ": " + e.Message + ")"); }
        }

        static void ForgetOwner(UnitEntityData owner)
        {
            if (watched.Remove(owner)) watchedSnapshot = null;
        }

        // An ongoing pass keeps its own array if a callback changes the roster.
        static UnitEntityData[] WatchedOwners()
        {
            return watchedSnapshot ?? (watchedSnapshot = watched.ToArray());
        }

        // modEntry.OnUpdate. 던지지 않는다.
        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            try
            {
                if (!Game.HasInstance || Game.Instance.Player == null || Game.Instance.State == null) return; // 정당한 무동작: 게임 준비 전
                EnsureHook();
                // 지역 주 상태가 바뀐 것을 처음 본 틱에 파티를 한 번 훑는다 — 지역 이벤트 구독이 첫 지역 로드보다 늦게 붙어도(구독은 첫 틱) 불러온 본체를 놓치지 않는다.
                var las = Game.Instance.LoadedAreaState;
                var mainNow = las == null ? null : las.MainState;
                if (mainNow != null && !ReferenceEquals(mainNow, scannedFor)) { scannedFor = mainNow; WatchParty(Game.Instance, !Game.Instance.IsLoadingSave); }
                if (ShadowUnitBlueprint.Unit == null) return; // 정당한 무동작: 블루프린트 없음(빌드 때 이미 보고)
                if (watched.Count == 0) return; // 정당한 무동작: 대상 없음
                foreach (var owner in WatchedOwners())
                {
                    try { Reconcile(owner); }
                    catch (Exception e) { Fail(owner, e); }
                }
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "tick-" + e.GetType().Name, "shadow doubles: tick failed (" + e.GetType().Name + ": " + e.Message + ")"); }
        }

        static void Reconcile(UnitEntityData owner)
        {
            if (Going(owner)) { ForgetOwner(owner); Remove(owner); return; }
            var game = Game.Instance;
            var area = game.LoadedAreaState;
            var main = area == null ? null : area.MainState;
            var facts = ReadFacts(owner, game);
            var e = ShadowDoubleRule.Eligible(facts);
            if (e == DoubleEligibility.Unknown)
                ShadowErrors.ReportOnce(EchoVisuals.Site, "eligibility-" + owner.UniqueId + "-" + (facts.UnknownName() ?? "WantCount"), "shadow doubles: cannot decide for " + owner.CharacterName + " (" + (facts.UnknownName() ?? "double count " + facts.WantCount) + " unreadable) - no shadow units, echoes keep working without the visual");
            Pool pool;
            bool has = byOwner.TryGetValue(owner, out pool);
            bool hasUnits = has && pool.Units.Length > 0;
            bool anyGoing = false;
            if (hasUnits) foreach (var u in pool.Units) if (Going(u)) anyGoing = true;
            bool failedHere = has && pool.Failed && ReferenceEquals(pool.FailedIn, main);
            var step = ShadowDoubleRule.Plan(e, hasUnits, hasUnits && ReferenceEquals(pool.Target, main), anyGoing, hasUnits ? pool.Units.Length : 0, facts.WantCount, failedHere);
            switch (step)
            {
                case PoolStep.Nothing: return;
                case PoolStep.Remove: Remove(owner); return;
                case PoolStep.Replace: SpendBudget(owner, main); Remove(owner); Create(owner, game, main, facts.WantCount); return;
                case PoolStep.Create: SpendBudget(owner, main); Create(owner, game, main, facts.WantCount); return;
                case PoolStep.Keep: Maintain(owner, pool, game, main); return;
            }
            throw new InvalidOperationException("unknown pool step " + step);
        }

        // 생성 예산(독립 검토 F8): 만들기 직전에 한 번 쓴다. 한도에 닿았으면 던진다 → Tick 의 Fail 이 묶음을 지우고 이 지역에 실패 표식을 남긴다(오류 줄 1회, 연출만 꺼진다).
        static void SpendBudget(UnitEntityData owner, SceneEntitiesState main)
        {
            var b = BudgetOf(owner, main);
            ShadowDoubleRule.RequireCreateBudget(b.Creations, ShadowVisualLimits.MaxPoolCreationsPerArea, owner.CharacterName);
            b.Creations++;
        }

        // 지역이 바뀌면 두 계수를 함께 0 으로.
        static Budget BudgetOf(UnitEntityData owner, SceneEntitiesState main)
        {
            if (main == null) throw new ArgumentNullException("main");
            Budget b;
            if (!budgets.TryGetValue(owner, out b)) { b = new Budget(); budgets.Add(owner, b); }
            if (!ReferenceEquals(b.Area.Target, main)) { b.Area.Target = main; b.Creations = 0; b.Restores = 0; }
            return b;
        }

        // 버프 블루프린트 guid(정렬). null = 읽을 수 없다(호출자가 판정 불가로 다룬다 — 빈 목록으로 넘기지 않는다). 할당이 있다 — 등록 확인 때 1회와 참조 비교가 다를 때만 부른다(경미-6).
        static string[] BuffGuids(UnitEntityData u)
        {
            var d = u.Descriptor;
            var buffs = d == null ? null : d.Buffs;
            var raw = buffs == null ? null : buffs.RawFacts;
            if (raw == null) return null;
            var list = new List<string>();
            foreach (var b in raw)
            {
                if (b == null || b.Blueprint == null) return null;
                list.Add(b.Blueprint.AssetGuid.ToString());
            }
            list.Sort(StringComparer.Ordinal);
            return list.ToArray();
        }

        // 버프 블루프린트 참조(기준용 — 등록 확인 때 1회). null = 읽을 수 없다.
        static object[] BuffRefs(UnitEntityData u)
        {
            var d = u.Descriptor;
            var buffs = d == null ? null : d.Buffs;
            var raw = buffs == null ? null : buffs.RawFacts;
            if (raw == null) return null;
            var refs = new object[raw.Count];
            for (int i = 0; i < raw.Count; i++)
            {
                if (raw[i] == null || raw[i].Blueprint == null) return null;
                refs[i] = raw[i].Blueprint;
            }
            return refs;
        }

        // 지금 버프 집합이 기준과 같은가(참조 비교, 할당 없음 — 람다는 갈무리가 없어 컴파일러가 한 번만 만든다). null = 읽을 수 없다.
        static bool? SameBuffs(UnitEntityData u, object[] baseline)
        {
            var d = u.Descriptor;
            var buffs = d == null ? null : d.Buffs;
            var raw = buffs == null ? null : buffs.RawFacts;
            return ShadowDoubleRule.SameRefs(raw, b => b == null ? null : (object)b.Blueprint, baseline);
        }

        static DoubleFacts ReadFacts(UnitEntityData owner, Game game)
        {
            var f = new DoubleFacts();
            // 지역 로드됨: 장면 로더의 지역이 있고, 불러온 지역 상태가 그 지역의 것이며 주 상태가 있다(귀검사 StandSpiritLifecycle.AreaLoaded 와 같은 판정).
            var loaded = game.CurrentlyLoadedArea;
            var las = game.LoadedAreaState;
            if (loaded == null || las == null || las.MainState == null) f.AreaLoaded = Tri.No;
            else if (las.Blueprint == null) f.AreaLoaded = Tri.Unknown;
            else f.AreaLoaded = las.Blueprint.AssetGuid == loaded.AssetGuid ? Tri.Yes : Tri.No;
            f.AreaHidesParty = loaded == null ? Tri.No : loaded.HideParty ? Tri.Yes : Tri.No; // 지역 없음 = 숨길 지역 규칙이 없다(생성은 AreaLoaded = No 가 막는다)
            f.OwnerInGame = owner.IsInGame ? Tri.Yes : Tri.No;
            // 레벨업 화면의 미리보기 유닛도 메아리 특성을 갖고 특성 켜짐 이벤트를 낸다 — 게임 상태에 등록된 유닛에만 그림자를 만든다(HoldingState: 등록 때 엔진이 적는다).
            f.OwnerInState = owner.HoldingState != null ? Tri.Yes : Tri.No;
            var st = owner.Descriptor == null ? null : owner.Descriptor.State;
            f.OwnerDead = st == null ? Tri.Unknown : st.IsDead ? Tri.Yes : Tri.No;
            var echo = Archetype.EchoFeature;
            f.HasEchoFeature = echo == null || owner.Descriptor == null ? Tri.Unknown : owner.Descriptor.HasFact(echo) ? Tri.Yes : Tri.No;
            if (f.HasEchoFeature == Tri.Yes) f.WantCount = ShadowRules.EchoCount(owner.Progression.GetClassLevel(Archetype.SorcererClass));
            return f;
        }

        // 파티 공용 인벤토리의 (항목 수, 수량 합). 없으면 던진다 — 비교 기준이 없으면 격리를 판정할 수 없다.
        static void Tally(ItemsCollection c, out int entries, out int total)
        {
            if (c == null || c.Items == null) throw new InvalidOperationException("no party inventory (cannot verify shadow inventory isolation)");
            total = 0;
            foreach (var it in c.Items)
            {
                if (it == null) throw new InvalidOperationException("inventory holds a null item entry (cannot count)");
                total += it.Count;
            }
            entries = c.Items.Count;
        }

        static void Create(UnitEntityData owner, Game game, SceneEntitiesState main, int want)
        {
            var bp = ShadowUnitBlueprint.Unit;
            if (bp == null) throw new InvalidOperationException("shadow unit blueprint is not built");
            if (main == null) throw new InvalidOperationException("no loaded area state");
            if (want < 1) throw new ArgumentOutOfRangeException("want", want, "shadow double count must be >= 1");
            if (ShouldSerializeMethod == null) throw new MissingMethodException("SceneEntitiesStateConverter.ShouldSerialize not found (save exclusion cannot be verified)");
            var creator = game.EntityCreator;
            if (creator == null) throw new InvalidOperationException("no Game.EntityCreator");
            var party = game.Player.Inventory;
            int beforeEntries, beforeTotal;
            Tally(party, out beforeEntries, out beforeTotal);
            var made = new List<UnitEntityData>();
            UnitEntityData unit = null;
            try
            {
                var rot = Quaternion.Euler(0f, owner.Orientation, 0f);
                for (int k = 0; k < want; k++)
                {
                    // 아래 두 줄 사이에 아무것도 넣지 말 것(시험이 IL 로 잠금): 생성 즉시 저장 제외 부품. SpawnUnit 이 null 을 돌려주면 다음 줄이 NullReferenceException → 아래 catch.
                    unit = creator.SpawnUnit(bp, owner.Position, rot, main, null);
                    unit.Ensure<UnitPartTrapActor>();
                    made.Add(unit);
                    if ((object)unit.Get<UnitPartTrapActor>() == null) throw new InvalidOperationException("UnitPartTrapActor readback failed");
                    // 엔진이 이 그림자를 저장하지 않는다는 것을 엔진 함수로 확인. 대조군 = 본체(저장되는 유닛 → true).
                    bool shadowSerialized = (bool)ShouldSerializeMethod.Invoke(null, new object[] { unit });
                    bool ownerSerialized = (bool)ShouldSerializeMethod.Invoke(null, new object[] { owner });
                    ShadowDoubleRule.RequireSaveExcluded(shadowSerialized, ownerSerialized);
                    // 인벤토리 격리: Player 진영 유닛은 파티 인벤토리를 공유한다 → 자기 인벤토리로 바꾸고, 파티 것이 그대로인지 센다.
                    unit.Descriptor.EnsureOwnInventory();
                    var own = unit.Inventory;
                    if (own == null) throw new InvalidOperationException("shadow unit has no inventory after EnsureOwnInventory");
                    int afterEntries, afterTotal;
                    Tally(party, out afterEntries, out afterTotal);
                    ShadowDoubleRule.RequireNoInventoryLeak(beforeEntries, beforeTotal, afterEntries, afterTotal, ReferenceEquals(own, party) || own.IsPlayerInventory, own.Items.Count);
                    var view = unit.View;
                    if (view == null) throw new InvalidOperationException("spawned shadow unit has no view");
                    view.ActivateColliders(false);
                    var agent = view.MovementAgent;
                    if (agent == null) throw new InvalidOperationException("shadow unit view has no MovementAgent");
                    agent.enabled = false;
                    // 평소에는 숨김.
                    var hidden = HiddenFlag(unit);
                    hidden.Retain();
                    if (!hidden) throw new InvalidOperationException("Hidden.Retain() did not raise the flag");
                    // 엔진 가시성 갱신(EntityVisibilityForPlayerController.Tick)은 등록된 유닛만 돈다 — 등록 전 한두 틱 동안 본체 자리에 비치지 않게 지금 한 번 끈다.
                    // 깃발과 같은 방향이라 엔진과 다투지 않는다(엔진이 뒤에 부르는 SetVisible(false, false) 는 값이 같아 무동작 — EntityViewBase.SetVisible IL_0028~0034).
                    view.SetVisible(false, true);
                    // 숨긴 동안은 게임 밖(파일 머리 「범위 주문 격리」). 등록(다음 엔진 틱)보다 먼저 — 등록된 순간부터 유닛 열거에 들지 않는다(본편 SpawnHidden 과 같은 순서).
                    unit.IsInGame = false;
                    if (unit.IsInGame) throw new InvalidOperationException("IsInGame = false did not take (the hidden shadow would stay in the unit enumeration)");
                    // 감시선의 기대 버프 집합은 여기서 찍지 않는다 — 등록(다음 엔진 틱: EntityCreationController.Tick IL_0026 AddEntityData → IL_00DD IUnitSpawnHandler)
                    // 때 버프를 다는 모드가 있으면 지역마다 오탐이 난다(2차 독립 검토 중요-3). Maintain 이 등록을 확인한 순간 찍는다.
                    byShadow.Add(unit, new OwnerBox { Owner = owner });
                }
                var pool = new Pool { Units = made.ToArray(), HiddenByUs = new bool[want], Retained = new bool[want], BuffsAtRegistration = new string[want][], BuffRefsAtRegistration = new object[want][], Target = main };
                for (int k = 0; k < want; k++) { pool.HiddenByUs[k] = true; pool.Retained[k] = true; } // 위에서 Retain 한 깃발 1개씩
                byOwner.Remove(owner);
                byOwner.Add(owner, pool);
                Created += want;
                ModContext.Log("ShadowSorcerer: shadow doubles created for " + owner.CharacterName + ": " + want + " hidden unit(s) (registration pending)");
            }
            catch
            {
                // 반쯤 만든 것을 남기지 않는다. 방금 SpawnUnit 이 돌려준 유닛이 made 에 아직 없을 수 있다(Ensure 가 던진 경우).
                if ((object)unit != null && !made.Contains(unit)) made.Add(unit);
                foreach (var u in made) byShadow.Remove(u);
                foreach (var u in made) if (!Going(u)) u.MarkForDestroy();
                throw;
            }
        }

        // 묶음 유지: 매 틱 이동 에이전트가 되켜졌으면 다시 끈다. 등록이 관찰되면 한 번 「정확히 N」 단언 + 감시선 기준(버프 집합)을 찍고,
        // 그 뒤로는 매 틱 격리 감시선을 읽는다(등록 전에는 유닛이 게임 상태에 없다 — 어떤 주문도 고를 수 없고 기준도 아직 없다).
        static void Maintain(UnitEntityData owner, Pool pool, Game game, SceneEntitiesState main)
        {
            bool registered = true;
            for (int k = 0; k < pool.Units.Length; k++)
            {
                var u = pool.Units[k];
                if (!ReferenceEquals(u.HoldingState, main)) registered = false;
                var view = u.View; // 뷰가 아직/이미 없을 수 있다 — 등록 관찰은 HoldingState 로, 사라짐은 Going 으로 본다. 감시선은 뷰 없이도 읽는다(아래 고리)
                var agent = view == null ? null : view.MovementAgent;
                if (agent != null && agent.enabled) agent.enabled = false;
            }
            if (!pool.Confirmed)
            {
                if (registered) Confirm(owner, pool, game);
                return;
            }
            for (int k = 0; k < pool.Units.Length; k++)
            {
                var u = pool.Units[k];
                // 격리 감시선(독립 검토 F2): 시각 전용 유닛은 맞지도, 버프가 걸리지도, 전투에 끼지도, 숨긴 동안 게임 안에 있지도 않아야 한다.
                // 새면 조용히 지나가지 않게 오류 줄로 드러낸다(종류당 1회. 유닛은 그대로 둔다: 지우면 원인을 볼 수 없다). 읽을 수 없는 입력도 「새지 않음」으로 넘기지 않는다.
                string detail;
                var facts = ReadIsolation(u, pool, k);
                var leak = ShadowDoubleRule.IsolationLeak(facts, out detail);
                if (leak == IsolationLeakKind.None) continue;
                if (leak != IsolationLeakKind.Unreadable) leakTally.Note(u.UniqueId, leak, detail);
                ShadowErrors.ReportOnce(EchoVisuals.Site, "isolation-" + leak + (leak == IsolationLeakKind.Unreadable ? "-" + detail : ""), "shadow doubles: a visual-only shadow unit of " + owner.CharacterName + LeakText(leak, detail, u) + " (isolation " + (leak == IsolationLeakKind.Unreadable ? "cannot be verified" : "leak") + ")");
                // 2차 독립 검토 중요-2: 보고만 하고 두면 숨긴 그림자가 게임 안(범위 주문의 유닛 열거)에 그대로 남는다 → 보고 뒤 다시 게임 밖으로 돌린다.
                //   지역당 한도(ShadowVisualLimits.MaxExposedRestoresPerArea)를 넘으면 던진다 → Tick 의 Fail 이 묶음을 지우고 이 지역의 연출을 끈다(줄다리기 금지).
                 
                if (ShadowDoubleRule.NeedsRestore(facts)) RestoreHidden(owner, k, main);
            }
        }

        static void RestoreHidden(UnitEntityData owner, int k, SceneEntitiesState main)
        {
            var b = BudgetOf(owner, main);
            ShadowDoubleRule.RequireRestoreBudget(b.Restores, ShadowVisualLimits.MaxExposedRestoresPerArea, owner.CharacterName);
            b.Restores++;
            ExposedRestores++;
            SetHidden(owner, k + 1, true); // 의도(숨김)는 그대로 — 실제 상태(깃발·뷰·게임 밖)를 의도에 맞춘다
        }

        // 등록이 관찰된 첫 틱: 「정확히 N」 단언 → 감시선 기준(버프 집합) 기록 → 확인 표시. 기준을 읽을 수 없으면 던진다(Tick 의 Fail — 기준 없는 감시선은 돌리지 않는다).
        static void Confirm(UnitEntityData owner, Pool pool, Game game)
        {
            int live = 0, inSet = 0;
            var bp = ShadowUnitBlueprint.Unit;
            // 숨긴 그림자는 게임 밖이라 Game.State.Units 의 열거자에 나오지 않는다 → 원본 목록(All)으로 센다.
            var all = game.State.Units.All;
            if (all == null) throw new InvalidOperationException("Game.State.Units.All is null (cannot count shadow units)");
            foreach (var u in all)
            {
                if ((object)u == null || u.Blueprint == null || u.Blueprint.AssetGuid != bp.AssetGuid || Going(u)) continue; // 정당한 제외: 다른 유닛·이미 지워지는 유닛
                OwnerBox ob;
                if (!byShadow.TryGetValue(u, out ob) || !ReferenceEquals(ob.Owner, owner)) continue; // 다른 본체의 그림자
                live++;
                if (Array.IndexOf(pool.Units, u) >= 0) inSet++;
            }
            ShadowDoubleRule.RequireExactly(live, inSet, pool.Units.Length, owner.CharacterName);
            // 감시선의 기대 버프 집합 = 등록이 확인된 지금의 되읽기(2차 독립 검토 중요-3: 등록 때 IUnitSpawnHandler 로 버프를 다는 모드가 있어도 그것까지 기준에 든다).
            for (int k = 0; k < pool.Units.Length; k++)
            {
                var guids = BuffGuids(pool.Units[k]);
                var refs = BuffRefs(pool.Units[k]);
                if (guids == null || refs == null) throw new InvalidOperationException("cannot read shadow unit " + (k + 1) + "'s buff list at registration (no baseline for the isolation tripwire)");
                pool.BuffsAtRegistration[k] = guids;
                pool.BuffRefsAtRegistration[k] = refs;
            }
            pool.Confirmed = true;
            ModContext.Log("ShadowSorcerer: shadow doubles of " + owner.CharacterName + " registered (exactly " + pool.Units.Length + " live unit(s) confirmed, out of game while hidden; tripwire baseline taken now)");
        }

        // 감시선 입력을 그 유닛에서 되읽는다. 읽을 수 없는 것은 null / Unknown 으로 둔다(0·false 로 채우지 않는다).
        // 버프: 먼저 블루프린트 참조로 비교(할당 없음 — 경미-6). 같으면 기준 배열을 그대로 넘기고(규칙이 같은 배열이면 문자열 비교를 건너뛴다), 다를 때만 guid 문자열을 만든다.
        static IsolationFacts ReadIsolation(UnitEntityData u, Pool pool, int k)
        {
            var f = new IsolationFacts();
            var d = u.Descriptor;
            if (d != null) f.Damage = d.Damage;
            f.InCombat = u.IsInCombat ? Tri.Yes : Tri.No;
            f.HiddenByUs = pool.HiddenByUs[k] ? Tri.Yes : Tri.No;
            f.InGame = u.IsInGame ? Tri.Yes : Tri.No;
            f.BuffsAtRegistration = pool.BuffsAtRegistration[k];
            var same = SameBuffs(u, pool.BuffRefsAtRegistration[k]);
            if (same == true) f.BuffsNow = f.BuffsAtRegistration;
            else if (same == false)
            {
                f.BuffsNow = BuffGuids(u);
                // 다른 것이 엔진 기본 토글 버프(ShadowDoubleRule.EngineDefaultToggleBuffs)뿐이면 기준을 지금 집합으로 옮긴다 — 규칙(IsolationLeak)은 어차피 그 차이를 세지 않고,
                // 옮겨 두면 다음 틱부터 참조 비교의 빠른 길로 돌아간다(경미-6). 참조를 못 읽으면 옮기지 않는다(규칙이 guid 로 계속 판정한다).
                if (f.BuffsNow != null && f.BuffsAtRegistration != null && ShadowDoubleRule.OnlyEngineDefaultsDiffer(f.BuffsAtRegistration, f.BuffsNow))
                {
                    var refs = BuffRefs(u);
                    if (refs != null)
                    {
                        pool.BuffsAtRegistration[k] = f.BuffsNow;
                        pool.BuffRefsAtRegistration[k] = refs;
                        f.BuffsAtRegistration = f.BuffsNow;
                        if (EngineDefaultRebaselines++ == 0) ModContext.Log("ShadowSorcerer: shadow double tripwire baseline moved to include the engine's default toggle buff(s) " + string.Join(",", ShadowDoubleRule.EngineDefaultToggleBuffs) + " (BasicFeatsProgression, IsOnByDefault) - not a leak");
                    }
                }
            }
            // same == null: 읽을 수 없다 → BuffsNow 는 null 그대로(규칙이 Unreadable 「BuffsNow」 로 보고)
            return f;
        }

        static string LeakText(IsolationLeakKind leak, string detail, UnitEntityData u)
        {
            switch (leak)
            {
                case IsolationLeakKind.Damage: return " has taken damage (" + u.Descriptor.Damage + ") - something targets it";
                case IsolationLeakKind.Combat: return " joined combat";
                case IsolationLeakKind.Buffs: return " has a different buff set than when it was registered: " + detail + " - cause unknown (an area spell may have picked it, or another mod changed its buffs)";
                case IsolationLeakKind.Exposed: return " is in game while it should be hidden out of game - something put it back; moved out of game again";
                case IsolationLeakKind.Unreadable: return ": " + detail + " is unreadable";
            }
            throw new InvalidOperationException("unknown isolation leak kind " + leak);
        }

        // ShadowTripwireComponent(껍데기 특성)가 부른다: 이 그림자 유닛이 규칙(내성 굴림·피해)의 당사자가 됐다. 던지지 않는다.
        internal static void NoteRuleTarget(UnitEntityData shadow, string rule)
        {
            try
            {
                RuleTargetHits++;
                OwnerBox ob;
                string owner = (object)shadow != null && byShadow.TryGetValue(shadow, out ob) && (object)ob.Owner != null ? ob.Owner.CharacterName : "(untracked)";
                ShadowErrors.ReportOnce(EchoVisuals.Site, "isolation-rule-" + rule, "shadow doubles: a visual-only shadow unit of " + owner + " was the subject of " + rule + " (" + ((object)shadow != null && shadow.IsInGame ? "while shown" : "while hidden") + ") - something picked it as a target (isolation leak)");
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "rule-target-" + e.GetType().Name, "shadow doubles: rule-target tripwire failed (" + e.GetType().Name + ": " + e.Message + ")"); }
        }

        static void Fail(UnitEntityData owner, Exception e)
        {
            try
            {
                Remove(owner);
                var area = Game.Instance == null ? null : Game.Instance.LoadedAreaState;
                byOwner.Add(owner, new Pool { Failed = true, FailedIn = area == null ? null : area.MainState });
            }
            catch (Exception e2) { ShadowErrors.ReportOnce(EchoVisuals.Site, "fail-" + e2.GetType().Name, "shadow doubles: cleanup after a failure failed (" + e2.GetType().Name + ": " + e2.Message + ")"); }
            ShadowErrors.ReportOnce(EchoVisuals.Site, "pool-" + owner.UniqueId + "-" + e.GetType().Name + "-" + e.Message, "shadow doubles failed for " + owner.CharacterName + " (" + e.GetType().Name + ": " + e.Message + ") - no retry in this area, echoes keep working without the shadow visual");
        }

        // 본체의 묶음을 지운다(기록 포함 — 실패 표식도 지운다). 기록이 없으면 정당한 무동작.
        static void Remove(UnitEntityData owner)
        {
            Pool pool;
            if (!byOwner.TryGetValue(owner, out pool)) return;
            byOwner.Remove(owner);
            foreach (var u in pool.Units) byShadow.Remove(u);
            foreach (var u in pool.Units)
            {
                if (!Going(u)) { u.MarkForDestroy(); Destroyed++; }
            }
        }

        static Kingmaker.Utility.CountableFlag HiddenFlag(UnitEntityData shadow)
        {
            var st = shadow.Descriptor == null ? null : shadow.Descriptor.State;
            if (st == null || st.Features == null || st.Features.Hidden == null) throw new InvalidOperationException("shadow unit has no State.Features.Hidden");
            return st.Features.Hidden;
        }

        // ---- 연출 쪽(ShadowStage)이 쓰는 문 ----
        // 본체의 slot 번(1부터) 그림자. null = 지금은 쓸 수 없다(묶음 없음·생성 실패·사라지는 중) — 호출자가 사유를 남기고 연출 없이 진행한다.
        public static UnitEntityData Get(UnitEntityData owner, int slot)
        {
            if ((object)owner == null) throw new ArgumentNullException("owner");
            if (slot < 1) throw new ArgumentOutOfRangeException("slot", slot, "shadow slot must be >= 1");
            Pool pool;
            if (!byOwner.TryGetValue(owner, out pool) || slot > pool.Units.Length) return null;
            var u = pool.Units[slot - 1];
            return Going(u) || u.View == null ? null : u;
        }

        // 본체의 묶음 크기(0 = 없음).
        public static int Count(UnitEntityData owner)
        {
            if ((object)owner == null) throw new ArgumentNullException("owner");
            Pool pool;
            return byOwner.TryGetValue(owner, out pool) ? pool.Units.Length : 0;
        }

        // 묶음의 slot 번 자리가 바로 이 유닛인가(뷰 유무·사라짐과 무관 — 숨길 때는 뷰가 없는 유닛도 숨겨야 한다, 독립 검토 F8).
        public static bool IsSlot(UnitEntityData owner, int slot, UnitEntityData unit)
        {
            if ((object)owner == null) throw new ArgumentNullException("owner");
            if ((object)unit == null) throw new ArgumentNullException("unit");
            if (slot < 1) throw new ArgumentOutOfRangeException("slot", slot, "shadow slot must be >= 1");
            Pool pool;
            return byOwner.TryGetValue(owner, out pool) && slot <= pool.Units.Length && ReferenceEquals(pool.Units[slot - 1], unit);
        }

        // 그 유닛이 지금 그 자리에 드러나 있는가: 묶음의 그 자리이고, 우리가 숨기지 않았고, 게임 안이고, 사라지는 중이 아니다.
        // 메아리에 이미 묶인 출발점(EchoLaunchAnchor)이 흩어진 뒤에도 그림자를 가리키지 않게 H12·H13 이 묻는다(독립 검토 F6).
        public static bool IsShown(UnitEntityData owner, int slot, UnitEntityData unit)
        {
            if (!IsSlot(owner, slot, unit) || Going(unit)) return false;
            Pool pool;
            byOwner.TryGetValue(owner, out pool);
            return !pool.HiddenByUs[slot - 1] && unit.IsInGame;
        }

        // 숨김/드러냄. 의도를 먼저 적고(HiddenByUs) 실제 상태(깃발·뷰·게임 안/밖)를 그 의도에 맞춘다 — 같은 방향으로 다시 불러도 실제 상태를 다시 맞춘다(2차 독립 검토 중요-2:
        //   드러내는 도중 IsInGame = true 에서 던진 뒤 catch 의 SetHidden(true) 가 「장부상 이미 숨김」이라 무동작이던 구멍 · 감시선 Exposed 뒤 복구가 이 함수를 쓴다).
        // Retain 과 Release 는 늘 짝을 이룬다: 짝은 실제로 건 Retain 장부(Retained)로 센다(ShadowDoubleRule.NextHidden). 묶음에 없는 유닛이면 던진다.
        //   숨김   = (아직 안 걸었으면) Hidden 깃발 올림 → 뷰를 지금 끔(게임 밖 유닛은 엔진 가시성 갱신이 돌지 않는다 — EntityVisibilityForPlayerController.Tick 이 같은 열거자를 쓴다) → 게임 밖.
        //   드러냄 = 게임 안 → (걸었으면) Hidden 깃발 내림(뷰는 엔진 가시성 갱신이 다음 틱에 켠다).
        public static void SetHidden(UnitEntityData owner, int slot, bool hidden)
        {
            Pool pool;
            if ((object)owner == null || !byOwner.TryGetValue(owner, out pool) || slot < 1 || slot > pool.Units.Length) throw new InvalidOperationException("no shadow unit in slot " + slot);
            int k = slot - 1;
            pool.HiddenByUs[k] = hidden; // 의도 먼저 — 아래 어디서 던져도 장부는 이번 요청이다(감시선이 실제 상태와의 차이를 본다)
            var u = pool.Units[k];
            if (Going(u)) return; // 정당한 무동작: 사라지는 유닛의 깃발·게임 상태는 만질 것이 없다
            var flag = HiddenFlag(u);
            int delta;
            ShadowDoubleRule.NextHidden(pool.Retained[k], hidden, out delta);
            if (hidden)
            {
                if (delta > 0) { flag.Retain(); pool.Retained[k] = true; }
                var view = u.View;
                if (view != null) view.SetVisible(false, true); // 뷰가 없으면 끌 것이 없다(깃발과 게임 밖 상태는 뷰 없이도 건다)
                u.IsInGame = false;
            }
            else
            {
                u.IsInGame = true;
                if (delta < 0) { flag.Release(); pool.Retained[k] = false; }
            }
            if (u.IsInGame == hidden) throw new InvalidOperationException("shadow unit in slot " + slot + " IsInGame is " + u.IsInGame + " after " + (hidden ? "hiding" : "revealing") + " it");
        }





        // ---- 지역 ----
        static void EnsureHook()
        {
            if (areaHook != null) return;
            areaHook = new AreaHook();
            EventBus.Subscribe(areaHook);
            if (!EventBus.IsGloballySubscribed(areaHook)) ShadowErrors.Report(EchoVisuals.Site, "shadow doubles: area subscription did not register - shadow units will not be rebuilt on area load");
        }

        // 지역을 떠날 때·불러왔을 때: 묶음을 전부 버린다(유닛은 지역 상태와 함께 사라지고 저장되지 않는다). 불러온 뒤에는 표에 없는 우리 유닛을 지우고(있으면 오류 — 저장 제외가 샌 것),
        // 파티에서 메아리 특성 보유자를 다시 감시한다(불러오면 본체 객체가 새로 만들어진다). 다음 Tick 이 새로 만든다.
        static void OnArea(bool loaded)
        {
            foreach (var owner in WatchedOwners())
            {
                Remove(owner);
                if (Going(owner)) ForgetOwner(owner);
            }
            if (!loaded) return;
            var game = Game.Instance;
            var bp = ShadowUnitBlueprint.Unit;
            if (game == null || game.State == null || game.Player == null) throw new InvalidOperationException("area loaded without Game.Instance / State / Player");
            if (bp != null)
            {
                var doomed = new List<UnitEntityData>();
                // 원본 목록(All): 게임 밖(숨겨 둔 채 저장 제외가 샌) 그림자도 찾아야 한다 — 열거자는 게임 밖 유닛을 건너뛴다.
                var all = game.State.Units.All;
                if (all == null) throw new InvalidOperationException("Game.State.Units.All is null (cannot look for leftover shadow units)");
                foreach (var u in all)
                    if ((object)u != null && u.Blueprint != null && u.Blueprint.AssetGuid == bp.AssetGuid && !Going(u)) doomed.Add(u);
                foreach (var u in doomed) { byShadow.Remove(u); u.MarkForDestroy(); }
                UntrackedDestroyed += doomed.Count;
                if (doomed.Count > 0) ShadowErrors.Report(EchoVisuals.Site, "shadow doubles: " + doomed.Count + " shadow unit(s) were present right after an area load (a shadow unit escaped save exclusion or tracking) - destroyed");
            }
            scannedFor = game.LoadedAreaState == null ? null : game.LoadedAreaState.MainState;
            WatchParty(game, true);
        }

        // 파티(동료 전원)에서 메아리 특성 보유자를 감시 목록에 올린다. 지역 밖 동료는 감시만 되고 묶음은 만들지 않는다(OwnerInGame = No).
        static void WatchParty(Game game, bool reconcileSlots)
        {
            var echo = Archetype.EchoFeature;
            if (echo == null) return; // 정당한 무동작: 아키타입 빌드 전·실패(메아리 자체가 없다)
            foreach (var c in game.Player.AllCharacters)
                if ((object)c != null && !Going(c) && c.Descriptor != null)
                {
                    if (reconcileSlots) SpellsPerDayPatch.ClampLoadedSlots(c.Descriptor);
                    if (c.Descriptor.HasFact(echo)) Watch(c);
                }
        }

        sealed class AreaHook : IAreaHandler
        {
            public void OnAreaBeginUnloading()
            {
                try { OnArea(false); }
                catch (Exception e) { ShadowErrors.ReportException(EchoVisuals.Site, e); }
            }
            public void OnAreaDidLoad()
            {
                try { OnArea(true); }
                catch (Exception e) { ShadowErrors.ReportException(EchoVisuals.Site, e); }
            }
        }
    }
}
