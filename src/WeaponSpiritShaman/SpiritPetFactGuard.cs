using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using WotrClassMod.Common;

namespace WeaponSpiritShaman
{
    // [게임] 「영병은 그 특성을 갖지 않는다」(ForeignComponents.Rules 의 RefAbsentOnSpiritPet 규칙) 가정의 런타임 확인(후속 2026-11-14).
    //  - ResolveAtBuild: PetClass.Build(Strip 직후)가 1회 부른다 — 참조 블루프린트가 풀리는가(BlueprintsCache.Load, 없는 GUID 는 @001F→@00FD 로 null, 로그 없음)를 명시 상태로 고정 + INFO 1줄.
    //  - Check: SpiritMirrorComponent.Route(영병의 활성화·연결·복구·지역 로드, 영병의 장비/세트, 주인 또는 영병의 레벨업, 주인 미확정 시의 레벨업)가 맨 앞에서 부른다.
    //    위반 → ModContext.LogError(유닛·특성·규칙 이름), (유닛, 특성, 모드)마다 1회. 판정 불가(throw) → LogError(유닛·메시지마다 1회), 거울 사건 처리는 계속한다.
    //  사실 GUID = Facts.List 의 Blueprint.AssetGuid — PrerequisiteNoFeature.CheckInternal @0007 UnitHelper.HasFact → Facts.Get(blueprint) 과 같은 목록.
    internal static class SpiritPetFactGuard
    {
        static Dictionary<string, RefResolution> resolution; // null = ResolveAtBuild 전(그때 Check 가 불리면 판정 불가 → LogError)
        static readonly HashSet<string> reported = new HashSet<string>();

        internal static void ResolveAtBuild()
        {
            if (resolution != null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritPetFactGuard.ResolveAtBuild called twice");
            resolution = ForeignComponents.ResolveAbsenceRefs(ForeignComponents.Rules, g => ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(g)) != null);
            ModContext.Log(ForeignComponents.ResolutionReport(resolution));
        }

        internal static void Check(UnitEntityData pet)
        {
            if ((object)pet == null) throw new ArgumentNullException("pet");
            string name = pet.CharacterName, id = pet.UniqueId;
            try
            {
                if (resolution == null) throw new InvalidOperationException("SpiritPetFactGuard.Check before ResolveAtBuild (PetClass.Build did not run)");
                var guids = pet.Facts.List.Select(f => f == null || f.Blueprint == null ? null : f.Blueprint.AssetGuid.ToString()).ToList();
                var checks = ForeignComponents.JudgeSpiritPetFacts(ForeignComponents.Rules, guids, resolution);
                foreach (var rule in ForeignComponents.Violations(checks))
                    if (reported.Add(id + "|" + rule.RefGuid + "|" + rule.Mod)) ModContext.LogError(ForeignComponents.ViolationMessage(name, id, rule));
            }
            catch (Exception ex)
            {
                if (reported.Add(id + "|error|" + ex.Message))
                    ModContext.LogError("WeaponSpiritShaman: spirit pet " + name + " (" + id + ") forbidden-feature check cannot judge: " + ex);
            }
        }
    }
}
