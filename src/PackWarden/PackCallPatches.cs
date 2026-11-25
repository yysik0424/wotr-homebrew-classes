using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;

namespace PackWarden
{
    // 과제 8: 기승 연동 Harmony 패치. 대상 메서드는 전부 tools\verify-patch-targets.ps1 이 시그니처까지 검사한다.
     
    //   Dismount() → Dismount(false), DismountForce() → Dismount(true). Dismount(bool) → ClearAndRemoveSelf(force) 가 탑승 능력
    //   (SystemMechanics.MountAbility = MountTargetSwitch)을 끄고, 그 버프 MountedBuff 의 Deactivated 액션 ContextActionDismount 가
    //   DismountForce() 를 다시 부른다. 플레이어의 자의 하차는 이 토글을 끄는 것이므로 ContextActionDismount 가 가장 바깥 진입로다.
    static class PackPatchState
    {
        // 기수(Owner)별 Dismount(bool) 재진입 깊이. 판정은 그 기수의 가장 바깥(1) 호출에서만.
        // 기수별인 까닭(과제 8 수정 2): A 의 하차 중에 B 의 하차가 불려도 B 는 B 의 바깥 호출로 판정돼야 한다.
        // 열쇠는 UnitPartRider 가 아니라 Owner: ClearAndRemoveSelf 가 파트를 떼어 내도 같은 기수로 묶인다.
        [ThreadStatic] static Dictionary<UnitEntityData, int> dismountDepth;
        // ContextActionDismount.RunAction 이 가장 바깥 진입로인 기수(시전자)별 중첩 수.
        [ThreadStatic] static Dictionary<UnitEntityData, int> dismountAction;

        static int Get(Dictionary<UnitEntityData, int> map, UnitEntityData u)
        {
            int d;
            return map != null && map.TryGetValue(u, out d) ? d : 0; // 열쇠 없음 = 그 기수는 지금 하차 중이 아님(결측 아님)
        }

        static int Inc(ref Dictionary<UnitEntityData, int> map, UnitEntityData u)
        {
            if (map == null) map = new Dictionary<UnitEntityData, int>();
            int d = Get(map, u) + 1;
            map[u] = d;
            return d;
        }

        // Inc 한 쪽만 부른다(Prefix 의 __state 로 짝을 맞춤). 그래도 0 이하가 되면 짝이 깨진 것 → 0 으로 되돌리고 시끄럽게 기록(수정 3).
        static void Dec(Dictionary<UnitEntityData, int> map, UnitEntityData u, string what)
        {
            int d = Get(map, u);
            if (d <= 0)
            {
                if (map != null) map.Remove(u);
                Main.Entry.Logger.Error("PackWarden: " + what + " counter for " + u + " would go below 0 (unpaired finalizer); reset to 0");
                return;
            }
            if (d == 1) map.Remove(u); else map[u] = d - 1;
        }

        internal static int DismountDepth(UnitEntityData rider) { return Get(dismountDepth, rider); }
        internal static int EnterDismount(UnitEntityData rider) { return Inc(ref dismountDepth, rider); }
        internal static void LeaveDismount(UnitEntityData rider) { Dec(dismountDepth, rider, "dismount depth"); }
        internal static bool InsideDismountAction(UnitEntityData rider) { return Get(dismountAction, rider) > 0; }
        internal static void EnterDismountAction(UnitEntityData rider) { Inc(ref dismountAction, rider); }
        internal static void LeaveDismountAction(UnitEntityData rider) { Dec(dismountAction, rider, "dismount action"); }

        // 블루프린트 생성 실패는 부팅 때 BlueprintsCacheInitPatch 가 오류로 남겼다(Main.BuildFailed). 그때는 무리 유닛·특성이 없으므로
        // 모든 기수의 하차·기승마다 예외를 던져 본편 경로를 망가뜨리지 않고 패치를 끈다. 생성 전 호출은 일어날 수 없으니 실패.
        internal static bool Ready()
        {
            if (Main.BuildFailed || Main.BuildSkipReasonEn != null) return false;
            if (PackCall.CallFeature == null) throw new InvalidOperationException("PackWarden: mount patch ran before PackCall.Build()");
            return true;
        }

        // 기승 훅이 다루는 지도자 = 무리 소집 특성 보유 + 7레벨 이상(설계 4.5). 7레벨 미만은 과제 6 토글 경로만 쓴다.
        internal static UnitEntityData MountGatedLeader(UnitEntityData unit)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            if (!unit.Descriptor.HasFact(PackCall.CallFeature)) return null;
            return PackRules.UsesMountGate(PackCall.HunterLevel(unit)) ? unit : null;
        }

        internal static UnitEntityData RiderOwner(UnitPartRider rider)
        {
            if (rider == null) throw new ArgumentNullException("rider");
            var owner = rider.Owner;
            if (owner == null) throw new InvalidOperationException("PackWarden: UnitPartRider has no owner");
            return owner;
        }
    }

    // Mount 오버로드는 이것 하나뿐(dnlib). 조기 반환(부적합 탈것·기승 금지 조건)이 있어도 ShouldSummon 이 실제 SaddledUnit==pet 을 다시 본다.
    [HarmonyPatch(typeof(UnitPartRider), nameof(UnitPartRider.Mount), new[] { typeof(UnitEntityData), typeof(bool) })]
    static class RiderMountPatch
    {
        [HarmonyPostfix]
        static void Postfix(UnitPartRider __instance)
        {
            if (!PackPatchState.Ready()) return;
            var leader = PackPatchState.MountGatedLeader(PackPatchState.RiderOwner(__instance));
            if (leader == null) return;
            PackCall.TrySummon(leader);
        }
    }

    // 모든 하차의 깔때기(private). 원인 판정은 PackRules.ClassifyDismount / DismountDismisses(시험 대상)에 둔다.
    // 같은 private 대상에 KineticArchetypes 가 Postfix 를 건다 → 우리 Postfix 를 먼저(High) 돌려 그쪽 예외가 우리 판정을 건너뛰게 하지 않는다(수정 4).
    [HarmonyPatch(typeof(UnitPartRider), "Dismount", new[] { typeof(bool) })]
    static class RiderDismountPatch
    {
        internal struct State { public UnitEntityData Rider; public bool Entered, Outermost, WasMounted, MountConscious; }

        [HarmonyPrefix]
        static void Prefix(UnitPartRider __instance, out State __state)
        {
            __state = default(State);
            var rider = PackPatchState.RiderOwner(__instance); // 던지면 Entered=false → Finalizer 는 감소하지 않는다
            __state.Rider = rider;
            int depth = PackPatchState.EnterDismount(rider);
            __state.Entered = true;
            __state.Outermost = depth == 1;
            if (!__state.Outermost) return;
            var mount = __instance.SaddledUnit; // 원본이 ClearSaddledUnit 으로 지우기 전 값
            __state.WasMounted = mount != null;
            __state.MountConscious = mount != null && mount.State.IsConscious;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        static void Postfix(UnitPartRider __instance, bool force, State __state, bool __runOriginal)
        {
            if (!__runOriginal) return; // 다른 Prefix 가 원본 하차를 취소함 → 하차가 없었다
            if (!__state.Outermost) return;
            if (!PackPatchState.Ready()) return;
            var leader = PackPatchState.MountGatedLeader(__state.Rider);
            if (leader == null) return;
            bool insideAction = PackPatchState.InsideDismountAction(leader);
            var cause = PackRules.ClassifyDismount(force, PackCall.SuppressDismiss, insideAction);
            bool mountedAfter = __instance.SaddledUnit != null;
            if (PackRules.DismountDismisses(cause, __state.WasMounted, mountedAfter, leader.State.IsConscious, leader.State.CanAct, __state.MountConscious))
                PackCall.Dismiss(leader);
        }

        // void Finalizer: 원본·Postfix 의 예외는 그대로 다시 던져진다(Harmony 2.3.6 실측, task-8-report 수정 1회차). 우리 Prefix 가 올린 깊이만 되돌린다.
        [HarmonyFinalizer]
        static void Finalizer(State __state)
        {
            if (__state.Entered) PackPatchState.LeaveDismount(__state.Rider);
        }
    }

    // 자의 하차 표식(시전자별). 그 시전자가 이미 하차 중(깊이 > 0)이면 연쇄 호출이므로 표식을 세우지 않는다.
    // ContextAction.Context 는 protected → 게터를 한 번 묶는다(verify-patch-targets 가 존재를 검사). 없으면 형식 초기화에서 실패.
    [HarmonyPatch(typeof(ContextActionDismount), nameof(ContextActionDismount.RunAction))]
    static class DismountActionPatch
    {
        static readonly Func<ContextAction, MechanicsContext> GetContext =
            AccessTools.MethodDelegate<Func<ContextAction, MechanicsContext>>(
                AccessTools.PropertyGetter(typeof(ContextAction), "Context")
                ?? throw new MissingMethodException("PackWarden: ContextAction.get_Context not found"));

        [HarmonyPrefix]
        static void Prefix(ContextActionDismount __instance, out UnitEntityData __state)
        {
            __state = null;
            var ctx = GetContext(__instance);
            if (ctx == null) return; // 원본이 같은 null 에서 NRE 로 실패한다(IL: get_Context → callvirt get_MaybeCaster). 우리가 감출 일 아님
            var caster = ctx.MaybeCaster;
            if (caster == null) return; // 원본도 아무것도 안 하고 끝난다(IL_000B~000F)
            if (PackPatchState.DismountDepth(caster) != 0) return;
            PackPatchState.EnterDismountAction(caster);
            __state = caster;
        }

        [HarmonyFinalizer]
        static void Finalizer(UnitEntityData __state)
        {
            if (__state != null) PackPatchState.LeaveDismountAction(__state);
        }
    }

    // 설계 4.5: 기승 금지 구역 진입 = 해산(과제 8 수정 1). 원본은 RiderPart 가 있는 파티원에만 Dismount() 를 부르므로,
    // 이미 강제 하차해 무리를 유지 중인 지도자(파트 없음)는 하차 깔때기에 닿지 않는다 → 원본이 도는 같은 목록(Player.Party)을 직접 훑는다.
    // 하차 깔때기의 NoMountZone 해산과 겹쳐도 Dismiss 는 멱등.
    [HarmonyPatch(typeof(DisableMountRiding), "OnTurnOn")]
    static class NoMountZonePatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            if (!PackPatchState.Ready()) return;
            var game = Game.Instance;
            if (game == null || game.Player == null) throw new InvalidOperationException("PackWarden: DisableMountRiding.OnTurnOn without Game.Instance.Player");
            foreach (var u in game.Player.Party)
            {
                if (u == null) throw new InvalidOperationException("PackWarden: null unit in Player.Party");
                if (!u.Descriptor.HasFact(PackCall.CallFeature)) continue;
                if (PackRules.NoMountZoneEntryDismisses(true, PackCall.HunterLevel(u))) PackCall.Dismiss(u);
            }
        }
    }

    // 탈것·기수 기절/사망 등 강제 하차(설계 4.5 "유지"). TickDismount 가 부르는 Dismount() 와 그 안의 연쇄 호출 전부를 덮는다.
    // Finalizer 라서 원본이 예외를 던져도 표식이 남지 않는다.
    [HarmonyPatch(typeof(SaddledUnitController), "TickDismount", new[] { typeof(UnitPartRider), typeof(UnitPartSaddled) })]
    static class TickDismountPatch
    {
        [HarmonyPrefix]
        static void Prefix(out bool __state)
        {
            __state = PackCall.SuppressDismiss;
            PackCall.SuppressDismiss = true;
        }

        [HarmonyFinalizer]
        static void Finalizer(bool __state) { PackCall.SuppressDismiss = __state; }
    }

    // 과제 7 우려 C1: 소환자(지도자)가 의식을 잃거나 죽으면 본편은 연결된 소환체를 ForceKill 한다(TickOnUnit 에서 IsLinkedToSummoner 일 때만 호출).
    // 설계 4.5 "보조는 남아서 주인을 지킨다" + 과제 6 판정(지도자 기절 ≠ 해산) → 보조는 건너뛴다. 정리는 파티 전투 종료가 맡는다.
    // 소환자 자체가 없으면(null) 지킬 주인도 해산해 줄 구독자도 없으므로 본편대로 죽게 둔다.
    [HarmonyPatch(typeof(SummonedUnitsController), "DestroyUnitIfCasterUnconsciousOrMissing", new[] { typeof(UnitEntityData), typeof(UnitPartSummonedMonster) })]
    static class LeaderDownExemptionPatch
    {
        [HarmonyPrefix]
        static bool Prefix(UnitEntityData unit, UnitPartSummonedMonster part)
        {
            if (Main.BuildFailed || Main.BuildSkipReasonEn != null) return true; // 보조 블루프린트가 없으니 보조도 없다
            if (unit == null) throw new ArgumentNullException("unit");
            if (part == null) throw new ArgumentNullException("part");
            if (!AuxUnits.IsAuxBlueprint(unit.Blueprint)) return true;
            return part.Summoner == null;
        }
    }
}
