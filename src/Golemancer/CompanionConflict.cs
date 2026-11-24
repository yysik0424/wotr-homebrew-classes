using System;
using System.Linq;

namespace Golemancer
{
     
    // → 추가 사건에 기대지 않고 안정 시점마다 주인의 사실을 직접 본다. 게임 쪽(T3)이 평평한 값으로 넣는다. 게임 형식 없음.
    //   addPetUnitGuidsOnMaster  = 주인 위 AddPet 컴포넌트들의 펫 유닛 블루프린트 GUID(우리 골렘 포함)
    //   companionSelectionFactGuids = 주인이 가진 동물 동료 선택 계열 사실 GUID(드루이드·헌터 등 — 골렘보다 먼저든 나중이든)
    // 우리 것이 아닌 펫 원천이 하나라도 있거나 동료 선택 계열 사실이 하나라도 있으면 충돌.
    public static class CompanionConflict
    {
        public static bool Decide(string[] addPetUnitGuidsOnMaster, string ourUnitGuid, string[] companionSelectionFactGuids)
        {
            if (addPetUnitGuidsOnMaster == null) throw new ArgumentNullException("addPetUnitGuidsOnMaster");
            if (string.IsNullOrEmpty(ourUnitGuid)) throw new ArgumentNullException("ourUnitGuid");
            if (companionSelectionFactGuids == null) throw new ArgumentNullException("companionSelectionFactGuids");
            if (addPetUnitGuidsOnMaster.Any(string.IsNullOrEmpty) || companionSelectionFactGuids.Any(string.IsNullOrEmpty))
                throw new ArgumentException("CompanionConflict.Decide: empty GUID in the observed lists (unresolved reference is not 'no conflict')");
            return addPetUnitGuidsOnMaster.Any(g => g != ourUnitGuid) || companionSelectionFactGuids.Length > 0;
        }
    }
}
