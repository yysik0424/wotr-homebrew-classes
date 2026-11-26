using System;
using HarmonyLib;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;

namespace PackWarden
{
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
