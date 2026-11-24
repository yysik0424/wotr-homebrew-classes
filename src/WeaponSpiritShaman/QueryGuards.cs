using System;
using System.Collections.Generic;

namespace WeaponSpiritShaman
{
    // [순수] 엔진이 매 프레임 묻는 질의(IAbilityCasterRestriction — ActionBarVM IsEnable 람다·UnitBrain.GetAvailableAutoUseAbility·UnitUseSpellsOnRest 의 능력 순회에 catch 없음,
     
    // 2026-11-18 융합 제거(소유자 결정)로 FusionGate.cs 에서 옮겼다 — 강신·스피릿 배러지 시전 제한이 쓴다.
    public static class PolledQuery
    {
        public static bool Passes(bool? verdict) { return verdict == true; }
    }

    public sealed class UnknownOnce
    {
        readonly HashSet<string> seen = new HashSet<string>();

         
        public static string KeyOf(string unitId, string what, Exception ex)
        {
            if (unitId == null) throw new ArgumentNullException("unitId");
            if (string.IsNullOrEmpty(what)) throw new ArgumentException("UnknownOnce.KeyOf: observation name is empty");
            if (ex == null) throw new ArgumentNullException("ex");
            return unitId + "|" + what + "|" + ex.GetType().FullName + "|" + ex.Message;
        }
        // 이 키의 첫 판정 불가면 true(기록할 것), 이후 false.
        public bool First(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            return seen.Add(key);
        }
    }
}
