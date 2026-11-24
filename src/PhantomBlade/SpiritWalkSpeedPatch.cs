using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Controllers.Optimization;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using WotrClassMod.Common;

namespace PhantomBlade
{
    // 령 걷기 동작 쓰기 지점(2026-11-17 3차 게임 안 점검 S12 walk-anim 0/71·11-15 동굴 0/151 재설계).
    // 원인(dnlib, Assembly-CSharp 2026-11-17):
    //   ① UnitCommandController.TickOnUnit IL_00F5~0107: 유닛 명령이 비어 있으면(Commands.get_Empty) View.StopMoving() 를 매 틱 부른다.
    //      령은 명령이 없다 → UnitEntityView.StopMoving IL_000E~0013 → UnitMovementAgent.Stop IL_00F0~00F6 「m_Speed = 0」.
    //   ② 같은 틱에서 그 뒤 UnitAnimationController.TickOnUnit IL_01B9~01E1 이 MovementAgent.Speed(= m_Speed, get_Speed IL_0001)를 읽어 동작 속도를 쓴다.
    //      컨트롤러 순서 = 등록 순서: GameModesFactory.Initialize IL_081A newobj UnitCommandController < IL_0BFC newobj UnitAnimationController.
    //   ③ 옛 설계는 SpiritFollower.LateUpdate(엔진 틱 뒤)에서 m_Speed 를 썼다 → 다음 틱 ①이 0 으로 덮고 ②가 0 을 읽음 → 걷기 동작 속도 0(미끄러짐), AgentReset 매 프레임.
    // 수리 = 쓰는 시점을 ①과 ② 사이, 정확히 ②가 읽기 직전으로: UnitAnimationController.TickOnUnit 전처리(Prefix)에서 우리 령일 때만 주인 속도를 넣는다.
    //   후처리(Postfix)는 엔진이 읽은 뒤의 값을 적어 LateUpdate 판정(SpiritRules.WalkEngineStep 의 agentSpeedBefore)에 넘긴다(TickOnUnit 은 m_Speed 를 쓰지 않음 — 시험이 잠금).
    // 우리 령이 아닌 유닛 = 표 조회 1번 뒤 무동작(정당). 패치가 안 걸렸으면 SpiritFollower 가 첫 판정에서 오류 줄 1번(IsApplied).
    // 대상 존재·인자 목록은 tools\patch-targets\PhantomBlade.txt + verify-patch-targets.ps1, 순서·IL 근거는 tests\PhantomBladeTests.cs(walk engine order) 가 잠근다.
    [HarmonyPatch(typeof(UnitAnimationController), "TickOnUnit", new[] { typeof(UnitEntityData), typeof(FrameContext) })]
    static class SpiritWalkSpeedPatch
    {
        // 인자 이름 unit = 엔진 매개변수 이름(dnlib: TickOnUnit(unit, context)). Harmony 는 이름으로 묶고, 어긋나면 PatchAll 이 던진다(시험도 이름을 잠금).
        [HarmonyPrefix]
        static void Prefix(UnitEntityData unit) { SpiritFollower.BeforeEngineAnimationTick(unit); }

        [HarmonyPostfix]
        static void Postfix(UnitEntityData unit) { SpiritFollower.AfterEngineAnimationTick(unit); }

        static readonly MethodBase Target = AccessTools.Method(typeof(UnitAnimationController), "TickOnUnit", new[] { typeof(UnitEntityData), typeof(FrameContext) });

        // 이 모드의 Harmony id 로 전·후처리가 둘 다 걸려 있는가. 대상 메서드가 없으면 이유와 함께 던진다(C2).
        internal static bool IsApplied()
        {
            if (Target == null) throw new MissingMethodException("PhantomBlade: UnitAnimationController.TickOnUnit(UnitEntityData, FrameContext) not found");
            var info = Harmony.GetPatchInfo(Target);
            if (info == null) return false;
            string id = ModContext.HarmonyId;
            // 이름 문자열 반사 대신 선언 형식 + nameof(컴파일 시점 결속).
            return info.Prefixes.Any(p => p.owner == id && p.PatchMethod.DeclaringType == typeof(SpiritWalkSpeedPatch) && p.PatchMethod.Name == nameof(Prefix))
                && info.Postfixes.Any(p => p.owner == id && p.PatchMethod.DeclaringType == typeof(SpiritWalkSpeedPatch) && p.PatchMethod.Name == nameof(Postfix));
        }
    }
}
