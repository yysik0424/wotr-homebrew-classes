using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
    // 판정 3상태(C1): 강제 성공 · 그대로(정당한 무동작) · 판정 불가(갑옷 군을 읽지 못함 — 「중갑 아님」으로 새지 않는다, 호출자가 Report).
    public enum IronVerdict { Force, Leave, Unknown }

     
    //   피해 원인 집중(UnitConcentrationController.Tick → UnitUseAbility.MakeConcentrationCheck(RuleDealDamage) @001F, Damage != null)만 본다 — 다른 집중(시전 곤란 등)은 갑옷을 읽지 않고 그대로.
    //   중갑 = 몸 갑옷의 ArmorProficiencyGroup 이 Heavy. 갑옷 없음 = "None"(그대로).
    public static class IronConcentrationRule
    {
        // 출처: 엔진 ArmorProficiencyGroup 열거 이름(dnlib 2026-11-14, 정찰 §4 — Light 1 … TowerShield 7, 바딩 8~10) + 갑옷 없음 표지 "None"(이 규칙의 입력 약속).
        static readonly IReadOnlyList<string> KnownGroups = new[] { "None", "Light", "Medium", "Heavy", "Buckler", "LightShield", "HeavyShield", "TowerShield", "LightBarding", "MediumBarding", "HeavyBarding" };
        const string HeavyGroup = "Heavy";

        public static IronVerdict Decide(bool damageCaused, bool? wearingHeavy)
        {
            if (!damageCaused) return IronVerdict.Leave;              // 피해 원인이 아님 — 정당한 무동작(갑옷을 보지 않는다)
            if (!wearingHeavy.HasValue) return IronVerdict.Unknown;     // 판정 불가 — 강제도 「아님」도 아니다
            return wearingHeavy.Value ? IronVerdict.Force : IronVerdict.Leave;
        }

        // 갑옷 군 이름 → 중갑 여부. null = 판정 불가(읽지 못함), 모르는 이름 = 던짐(엔진 열거가 바뀌면 시끄럽게).
        public static bool? IsHeavy(string armorGroup)
        {
            if (armorGroup == null) return null;
            if (!KnownGroups.Contains(armorGroup)) throw new ArgumentException("BulwarkMage: unknown armor proficiency group '" + armorGroup + "'", "armorGroup");
            return armorGroup == HeavyGroup;
        }
    }
}
