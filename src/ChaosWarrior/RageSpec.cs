namespace ChaosWarrior
{
     
     
    // 값 순서: 빌더가 비교하는 Ac(0)·StrCon(1)이 IL 리터럴 {−1,0,1} 안에 들도록(`dial: core literals`).
    public enum RageStatSource { Ac, StrCon, Will }

    public struct StatRow
    {
        public readonly string Stat, Descriptor;
        public readonly RageStatSource Source;
        public StatRow(string stat, string descriptor, RageStatSource source) { Stat = stat; Descriptor = descriptor; Source = source; }
        public override string ToString() { return Stat + "/" + Descriptor + "/" + Source; }
    }

    public static class RageSpec
    {
         
        public static readonly bool DeactivateImmediately = false;
        // 출처: 설계 §10-b 전투 종료 시 자동 해제 · DemonRageAbility DeactivateIfCombatEnded true(zip)
        public static readonly bool DeactivateIfCombatEnded = true;
        // 출처: StandartRageActivateableAbility DeactivateIfOwnerUnconscious true(zip)
        public static readonly bool DeactivateIfOwnerUnconscious = true;
         
        public static readonly bool OnlyInCombat = true;
         
        public static readonly bool IsOnByDefault = false;
        // 출처: 설계 §6 켤 때 1회 소모 = RageStartCost.OnStart. Native None tests for 0 remaining charges while active.
        public static readonly string SpendType = "None";
         
        public static readonly string Group = "Rage";
        // 출처: DemonRageAbility ActivationType Immediately(zip)
        public static readonly string ActivationType = "Immediately";
        // 출처: DemonRageAbility m_ActivateWithUnitCommand Free(zip) — 자유 행동
        public static readonly string ActivateWithUnitCommand = "Free";
         
        public static readonly string Stacking = "Replace";
        // 출처: 블러드레이지 선례 — 분노 수치 기술자 Rage(ModifierDescriptor 47)
        public static readonly string Descriptor = "Rage";
         
        public static readonly string StrConRankType = "StatBonus";
         
        public static readonly string WillRankType = "DamageBonus";
         
        public static readonly string RankBaseValueType = "ClassLevel";
         
        public static readonly string RankProgression = "Custom";
         
        public static readonly StatRow[] Stats =
        {
            new StatRow("Strength", "Rage", RageStatSource.StrCon),
            new StatRow("Constitution", "Rage", RageStatSource.StrCon),
            new StatRow("SaveWill", "Rage", RageStatSource.Will),
            new StatRow("AC", "Rage", RageStatSource.Ac),
        };
    }
}
