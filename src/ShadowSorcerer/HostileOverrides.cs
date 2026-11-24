using System.Collections.Generic;

namespace ShadowSorcerer
{
     
     
     
    //   InflictModerateWoundsMass 는 수용 표(「식 Echo 가 옳다」)에서 예외표로 옮겼다(수용 25 → 24).
     
     
    public enum OverrideKind { Classification, DisbeliefLeak, SummonUnflagged }

    public sealed class HostileOverride
    {
        public readonly string SpellGuid, Name, Reason, Source;
        public readonly bool Echo;
        public readonly OverrideKind Kind;
        public HostileOverride(string guid, string name, bool echo, string reason, string source) : this(guid, name, echo, reason, source, OverrideKind.Classification) { }
        public HostileOverride(string guid, string name, bool echo, string reason, string source, OverrideKind kind) { SpellGuid = guid; Name = name; Echo = echo; Reason = reason; Source = source; Kind = kind; }
    }

    // 교차 검사에서 「식이 옳다」고 본 불일치(분류는 바꾸지 않는다 — 시험 exec: hostile cross check 가 미해결 불일치 0 을 단언하는 데 쓴다).
    public sealed class HostileAccepted
    {
        public readonly string SpellGuid, Name, Reason;
        public HostileAccepted(string guid, string name, string reason) { SpellGuid = guid; Name = name; Reason = reason; }
    }

    public static class HostileOverrides
    {
        const string Jbp = "blueprints.zip 2026-11-14 ";
        const string Jbp16 = "blueprints.zip 2026-11-16 ";
        const OverrideKind Leak = OverrideKind.DisbeliefLeak;
        public static readonly IReadOnlyList<HostileOverride> Table = new[]
        {
            new HostileOverride("68a9e6d7256f1354289a39003a46d826", "StinkingCloud", true, "initial row: area of nauseating gas on enemies", Jbp + "Spells/Level3/StinkingCloud.jbp EffectOnEnemy None, CanTargetEnemies true, CanTargetFriends true"),
            new HostileOverride("8c29e953190cc67429dc9c701b16b7c2", "CausticEruption", true, "initial row: acid damage around the caster", Jbp + "Spells/Level7/CausticEruption.jbp Personal, CanTargetEnemies false, AbilityTargetsAround"),
            new HostileOverride("b0ffc8eaff404f2e9e0a3ee9f4c35486", "SilverDarts", true, "T3 cross check (J10 default): deals damage to enemies", Jbp + "Spells/DLC6NewSpells/SilverDarts.jbp EffectOnEnemy None, CanTargetFriends true, ContextActionDealDamage"),
            new HostileOverride("c6c516e59ca34f34aefb82a850287b97", "ArchonsTrumpet", true, "T3 cross check (J10 default): damages / paralyses enemies", Jbp + "Spells/DLC6NewSpells/ArchonsTrumpet.jbp EffectOnEnemy None, CanTargetFriends true, saving throw + damage"),
            new HostileOverride("48a555180a109e545a6e367b1bd3f535", "SymbolOfRevelation", true, "T3 cross check (J10 default): hostile symbol area at a point", Jbp + "Spells/Level4/SymbolOfRevelation.jbp CanTargetEnemies false, CanTargetPoint true, area effect"),
            new HostileOverride("8b02310b46e54de1ae9ba7161831938d", "SymbolOfWeakness", true, "T3 cross check (J10 default): hostile symbol area at a point", Jbp + "Spells/DLC5NewSpells/SymbolOfWeakness.jbp CanTargetEnemies false, CanTargetPoint true, area effect"),
            new HostileOverride("9cebb7c46c8d4c55a0e98da9b103fd37", "SymbolOfVulnerability", true, "T3 cross check (J10 default): hostile symbol area at a point", Jbp + "Spells/DLC5NewSpells/SymbolOfVulnerability.jbp CanTargetEnemies false, CanTargetPoint true, area effect"),
             
            new HostileOverride("5d28f75db5d3cc141ba5783a1a139f66", "Relativity", false, "owner ruling 2026-11-16 (was Echo by J10b): hostile, but its Haste removal (RemoveBuffSingleStack) reaches an enemy that disbelieved - no echo", Jbp16 + "Mythic/Aeon/AeonSpells/Relativity.jbp EffectOnEnemy None, CanTargetFriends true, ContextActionRemoveBuffSingleStack x2", Leak),
            new HostileOverride("1325e698f4a3f224b880e3b83a551228", "Supernova", true, "owner ruling (review 2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Aeon/AeonSpells/Supernova.jbp (merge / bloodline source)"),
            new HostileOverride("756f1d07f9ae29448888ecf016fa40a7", "ZoneOfPredetermination", false, "J17b default: hostile, but an Owner/Unit-anchored area without AbilityTargetsAround cannot keep the original centre (FreezeAtEnqueue Unknown) - no echo until ruled", Jbp + "Mythic/Aeon/AeonSpells/ZoneOfPredetermination.jbp (merge / bloodline source)"),
            new HostileOverride("cc8be337fff61284caabe3340fc48294", "AngelBladeOfTheSun", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Angel/AngelSpells/AngelBladeOfTheSun.jbp (merge / bloodline source)"),
            new HostileOverride("3c4c33f314a8bd64994d34ed5db2b96f", "AngelCleansingFlames", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Angel/AngelSpells/AngelCleansingFlames.jbp (merge / bloodline source)"),
            new HostileOverride("a948e10ecf1fa674dbae5eaae7f25a7f", "AngelEyeOfTheSun", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Angel/AngelSpells/AngelEyeOfTheSun.jbp (merge / bloodline source)"),
            new HostileOverride("87bcf706e376b80418903ae0d4303feb", "AngelRadiantGround", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Angel/AngelSpells/AngelRadiantGround.jbp (merge / bloodline source)"),
            new HostileOverride("041581ac635afe24daa421de5d2c2ee1", "AngelRepelTheProfane", false, "J17b default: hostile, but an Owner/Unit-anchored area without AbilityTargetsAround cannot keep the original centre (FreezeAtEnqueue Unknown) - no echo until ruled", Jbp + "Mythic/Angel/AngelSpells/AngelRepelTheProfane.jbp (merge / bloodline source)"),
            new HostileOverride("edc7d27fb42b9a94bac0bee1f3286b49", "AngelWallOfLight", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Angel/AngelSpells/AngelWallOfLight.jbp (merge / bloodline source)"),
            new HostileOverride("ae13b1831a5c79c468584e57ea94d07d", "AngelWrathOfTheRighteous", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Angel/AngelSpells/AngelWrathOfTheRighteous.jbp (merge / bloodline source)"),
            new HostileOverride("b2d9ad1cc95d718459c09499a2e9dbf9", "FieldOfFlowers", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Azata/AzataSpells/FieldOfFlowers.jbp (merge / bloodline source)"),
            new HostileOverride("319c5a450b01f834599389853753d0f0", "Revolt", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Azata/AzataSpells/Revolt.jbp (merge / bloodline source)"),
            new HostileOverride("3b86de15c18e4b44bb7315fc6c116b4d", "AbyssalChains", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Demon/DemonSpells/AbyssalChains.jbp (merge / bloodline source)"),
            new HostileOverride("58e9e2883bca1574e9c932e72fd361f9", "AbyssalStorm", true, "owner ruling (review 2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Demon/DemonSpells/AbyssalStorm.jbp (merge / bloodline source)"),
            new HostileOverride("eacb5f788001ca84cb83c3091c1ced19", "DevourCast", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Demon/DemonSpells/DevourCast.jbp (merge / bloodline source)"),
            new HostileOverride("3bbb3398e37929343b236bba6b4008a8", "InfectiousRage", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Demon/DemonSpells/InfectiousRage.jbp (merge / bloodline source)"),
            new HostileOverride("9f4c296805f823442a236f66adc36594", "TelekineticBurstPull", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Demon/DemonSpells/TelekineticBurstPull.jbp (merge / bloodline source)"),
            new HostileOverride("5c891419ee90c9d479901875c9e6df19", "TelekineticBurstPush", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Demon/DemonSpells/TelekineticBurstPush.jbp (merge / bloodline source)"),
            new HostileOverride("a9d1eff37681a294eb1ddaa50741b0b0", "TelekineticStrikePull", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Demon/DemonSpells/TelekineticStrikePull.jbp (merge / bloodline source)"),
            new HostileOverride("c1d0482e10b11494287a18f777d6ea3e", "TelekineticStrikePush", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Demon/DemonSpells/TelekineticStrikePush.jbp (merge / bloodline source)"),
            new HostileOverride("cc51242ff01192b49a2e25adf096e2d0", "BoneExplosion", true, "independent review C1 (2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Lich/LichSpells/BoneExplosion.jbp (merge / bloodline source)"),
            new HostileOverride("bbbcaa880ac0fa0479ce3ee8ac937d50", "CorruptedBlood", true, "independent review C1 (2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Lich/LichSpells/CorruptedBlood.jbp (merge / bloodline source)"),
            new HostileOverride("291524731e8b8f440b44915be833ae07", "Exsanguinate", true, "independent review C1 (2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Lich/LichSpells/Exsanguinate.jbp (merge / bloodline source)"),
            new HostileOverride("edf6e91ca5a468849b8326bcc9c569b2", "FeastOfBlood", true, "independent review C1 (2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Lich/LichSpells/FeastOfBlood.jbp (merge / bloodline source)"),
            new HostileOverride("921ed6a6751d71140b4e75ab7bcb9890", "MasteryOfFlesh", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Lich/LichSpells/MasteryOfFlesh.jbp (merge / bloodline source)"),
            new HostileOverride("5c377ad96e3fc4f4d9b74eba9d38f4f8", "NegativeEruption", true, "independent review C1 (2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Lich/LichSpells/NegativeEruption.jbp (merge / bloodline source)"),
            new HostileOverride("7bd52a86498c7854ebe99bc3cfb85bfe", "SiphonLife", true, "independent review C1 (2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Lich/LichSpells/SiphonLife.jbp (merge / bloodline source)"),
             
            new HostileOverride("e346cc550c5bfdf428faa7cdb4849d8e", "SiphonTime", false, "owner ruling 2026-11-16 (was Echo by J10b): hostile, but its Haste removal (RemoveBuffSingleStack) reaches an enemy that disbelieved - no echo", Jbp16 + "Mythic/Lich/LichSpells/SiphonTime.jbp EffectOnEnemy None, CanTargetFriends true, ContextActionRemoveBuffSingleStack x2", Leak),
            new HostileOverride("770eef29433299e4f9d21142d0706cfb", "TricksterBearsGrace", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Trickster/TricksterSpells/TricksterBearsGrace.jbp (merge / bloodline source)"),
            new HostileOverride("bc9f91f243634c04bcee43028362b5c3", "TricksterBreathOfMoney", false, "J17b default: hostile, but an Owner/Unit-anchored area without AbilityTargetsAround cannot keep the original centre (FreezeAtEnqueue Unknown) - no echo until ruled", Jbp + "Mythic/Trickster/TricksterSpells/TricksterBreathOfMoney.jbp (merge / bloodline source)"),
            new HostileOverride("3d1c5aa69827faf419bb17b0a8cadf9b", "TricksterFishMissile", true, "owner ruling (review 2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Trickster/TricksterSpells/TricksterFishMissile.jbp (merge / bloodline source)"),
            new HostileOverride("a68fc6abb6cb03f46a658b734d476630", "TricksterHallucinogenicCloud", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Trickster/TricksterSpells/TricksterHallucinogenicCloud.jbp (merge / bloodline source)"),
            new HostileOverride("a759d339d98572a42a0c6e1dcc33453d", "TricksterRainOfHalberds", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Trickster/TricksterSpells/TricksterRainOfHalberds.jbp (merge / bloodline source)"),
            new HostileOverride("d7e6b7a26d2855f4cbf658122301abb7", "TricksterRecreationalPit", true, "cross check over sorcerer-reachable lists (J10b default): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Mythic/Trickster/TricksterSpells/TricksterRecreationalPit.jbp (merge / bloodline source)"),
            new HostileOverride("0fd00984a2c0e0a429cf1a911b4ec5ca", "Entangle", true, "independent review C1 (2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Spells/Level1/Entangle.jbp (merge / bloodline source)"),
            new HostileOverride("ba48abb52b142164eba309fd09898856", "PolarMidnight", true, "independent review C1 (2026-11-14): harmful to enemies (damage / save / debuff action), formula misses it", Jbp + "Spells/Level9/PolarMidnight.jbp (merge / bloodline source)"),
            new HostileOverride("b3e8e307811b2a24387c2c9226fb4c10", "DemonTeleport", false, "owner ruling (review 2026-11-14): not harmful (teleport / utility), formula would echo", Jbp + "Mythic/Demon/DemonSpells/DemonTeleport.jbp (merge source)"),
            new HostileOverride("6fd7bdd6dfa9dd943b36d65faf97ac41", "CorruptMagic", false, "independent review C1 (2026-11-14): not harmful (teleport / utility), formula would echo", Jbp + "Mythic/Lich/LichSpells/CorruptMagic.jbp (merge source)"),
            new HostileOverride("de5f875a5f98d99459e45e6af8ce5dcd", "Repurpose", false, "independent review C1 (2026-11-14): not harmful (teleport / utility), formula would echo", Jbp + "Mythic/Lich/LichSpells/Repurpose.jbp (merge source)"),
            new HostileOverride("a9b8be9b87865744382f7c64e599aeb2", "DimensionDoorCasterOnly", false, "T3 cross check (J10 default): teleport, not harmful", Jbp + "Spells/Level4/DimensionDoorCasterOnly.jbp CanTargetEnemies true, CanTargetFriends false, EffectOnEnemy None (formula would echo)"),
             
            // 자식 주문(ContextActionCastSpell)은 부모 컨텍스트 없이 만들어져 메아리 표식·그림자 계수가 끊김 → 불신과 무관하게 피해 100%.
            new HostileOverride("03944622fbe04824684ec29ff2cec6a7", "InflictModerateWoundsMass", false, "owner ruling 2026-11-16: hostile, but the damage comes from a child spell (ContextActionCastSpell) that loses the echo mark and shadow factor - full damage even on a disbelief", Jbp16 + "Spells/Level6/InflictModerateWoundsMass.jbp EffectOnEnemy Harmful, ContextActionCastSpell x2 (InflictModerateWoundsMasssDamage)", Leak),
            // 불신 성공 시 변신 서술자 버프 전부 제거가 새어 나감.
            new HostileOverride("3105d6e9febdc3f41a08d2b7dda1fe74", "BalefulPolymorph", false, "owner ruling 2026-11-16: hostile, but its polymorph-descriptor buff removal (RemoveBuffsByDescriptor) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/Level5/BalefulPolymorph.jbp EffectOnEnemy Harmful, ContextActionRemoveBuffsByDescriptor x2", Leak),
            // 불신 성공 시 변신 서술자 버프 전부 제거가 새어 나감(즉사 갈래는 H9 가 막는다).
            new HostileOverride("b22fd434bdb60fb4ba1068206402c4cf", "PrismaticSpray", false, "owner ruling 2026-11-16: hostile, but its polymorph-descriptor buff removal (RemoveBuffsByDescriptor) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/Level7/PrismaticSpray.jbp EffectOnEnemy Harmful, ContextActionRemoveBuffsByDescriptor x6", Leak),
            // 불신 성공 시 넘어뜨림(반사 실패 시)이 새어 나감.
            new HostileOverride("01300baad090d634cb1a1b2defe068d6", "ClashingRocks", false, "owner ruling 2026-11-16: hostile, but its knockdown (ContextActionKnockdownTarget, on a failed Reflex) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/Level9/ClashingRocks.jbp EffectOnEnemy Harmful, ContextActionKnockdownTarget x6", Leak),
            // 불신 성공 시 넘어뜨림이 내성 결과와 무관하게 새어 나감.
            new HostileOverride("cd7b6981218a0274c916db0a2fc29855", "WaterTorrent", false, "owner ruling 2026-11-16: hostile, but its knockdown (ContextActionKnockdownTarget, regardless of the save) reaches an enemy that disbelieved - no echo", Jbp16 + "Mythic/Azata/AzataSpells/WaterTorrent.jbp EffectOnEnemy Harmful, ContextActionKnockdownTarget x2", Leak),
            // 불신 성공 시 밀쳐냄(인내 실패 시)이 새어 나감 — 접촉 전달 효과 ForcePunchEffect 안.
            new HostileOverride("fc58ddcff6ab1394eb6c18e9126bb990", "ForcePunchCast", false, "owner ruling 2026-11-16: hostile, but the delivered effect's push (ContextActionPush, on a failed Fortitude) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/Level3/ForcePunchCast.jbp EffectOnEnemy Harmful, sticky touch -> ForcePunchEffect ContextActionPush", Leak),
            // 불신 성공 시 자식 시전(BestJokes 특성이 있을 때 다른 적에게)이 새어 나감.
            new HostileOverride("fd4d9fd7f87575d47aafe2a64a6e2d8d", "HideousLaughter", false, "owner ruling 2026-11-16: hostile, but its child cast on another enemy (ContextActionCastSpell with BestJokes) leaves the echo mark behind - no echo", Jbp16 + "Spells/Level2/HideousLaughter.jbp EffectOnEnemy Harmful, ContextActionCastSpell x2", Leak),
            // 불신 성공 시 신속(Haste) 제거가 새어 나감.
            new HostileOverride("f492622e473d34747806bdb39356eb89", "Slow", false, "owner ruling 2026-11-16: hostile, but its Haste removal (RemoveBuffSingleStack) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/Level3/Slow.jbp EffectOnEnemy Harmful, ContextActionRemoveBuffSingleStack x2", Leak),
            // 불신 성공 시 선한 희망(GoodHope) 제거가 새어 나감.
            new HostileOverride("4baf4109145de4345861fe0f2209d903", "CrushingDespair", false, "owner ruling 2026-11-16: hostile, but its Good Hope removal (RemoveBuffSingleStack) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/Level3/CrushingDespair.jbp EffectOnEnemy None, CanTargetFriends false, ContextActionRemoveBuffSingleStack x2", Leak),
            // 불신 성공 시 공포 제거(RemoveFear) 버프 제거가 새어 나감.
            new HostileOverride("bd81a3931aa285a4f9844585b5d97e51", "CauseFear", false, "owner ruling 2026-11-16: hostile, but its Remove Fear removal (RemoveBuffSingleStack) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/Level1/CauseFear.jbp EffectOnEnemy Harmful, ContextActionRemoveBuffSingleStack x2", Leak),
            // 불신 성공 시 공포 제거(RemoveFear) 버프 제거가 새어 나감.
            new HostileOverride("8bc127ed66044685957e54f19d8dbdb7", "MortalTerror", false, "owner ruling 2026-11-16: hostile, but its Remove Fear removal (RemoveBuffSingleStack) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/DLC4NewSpells/MortalTerror.jbp EffectOnEnemy Harmful, ContextActionRemoveBuffSingleStack x2", Leak),
            // 불신 성공 시 공포 제거(RemoveFear) 버프 제거가 새어 나감.
            new HostileOverride("08cb5f4c3b2695e44971bf5c45205df0", "Scare", false, "owner ruling 2026-11-16: hostile, but its Remove Fear removal (RemoveBuffSingleStack) reaches an enemy that disbelieved - no echo", Jbp16 + "Spells/Level2/Scare.jbp EffectOnEnemy Harmful, ContextActionRemoveBuffSingleStack x4", Leak),
            // 소환 효과(ContextActionSpawnMonster)가 있는데 소환 서술자가 없어 식이 놓침 — 「소환 주문은 메아리 없음」 규칙에 맞춤.
            new HostileOverride("cc74245ba989480488925214dd925100", "PitOfDespair", false, "owner ruling 2026-11-16: spawns monsters (ContextActionSpawnMonster) without the Summoning descriptor, so the formula misses it - summons never echo", Jbp16 + "Mythic/Lich/LichSpells/PitOfDespair.jbp CanTargetEnemies true, CanTargetFriends false, no SpellDescriptor Summoning, ContextActionSpawnMonster x2", OverrideKind.SummonUnflagged),
        };

        public static readonly IReadOnlyList<HostileAccepted> Accepted = new[]
        {
            new HostileAccepted("450af0402422b0b4980d9c2175869612", "RayOfEnfeeblement", "debuff ray without save/damage action - formula Echo is right"),
            new HostileAccepted("ce7dad2b25acf85429b6c9550787b2d9", "Glitterdust", "blinding area debuff - formula Echo is right"),
            new HostileAccepted("dee3074b2fbfb064b80b973f9b56319e", "PerniciousPoison", "poison debuff - formula Echo is right"),
            new HostileAccepted("8878d0c46dfbd564e9d5756349d5e439", "WavesOfFatigue", "fatigue cone - formula Echo is right"),
            new HostileAccepted("261e1788bfc5ac1419eec68b1d485dbc", "PowerWordBlind", "blinding word - formula Echo is right"),
            new HostileAccepted("df7d13c967bce6a40bec3ba7c9f0e64c", "ResonatingWord", "deafening word - formula Echo is right"),
            new HostileAccepted("3e4d3b9a5bd03734d9b053b9067c2f38", "WavesOfExhaustion", "exhaustion cone - formula Echo is right"),
            new HostileAccepted("f958ef62eea5050418fb92dfa944c631", "PowerWordStun", "stunning word - formula Echo is right"),
            new HostileAccepted("4dc60d08c6c4d3c47b413904e4de5ff0", "ScintillatingPattern", "disabling pattern - formula Echo is right"),
            new HostileAccepted("eb0d3e22826725d4199a3aac0db0ad50", "AngelBringBackCast", "resurrection - formula Skip is right"),
            new HostileAccepted("d570b3ca66f80a14586bfe68fab6336b", "RainbowDomeAcid", "protective dome (Helpful) - formula Skip is right"),
            new HostileAccepted("8df980e0a5e254b41b08f097f3a87f05", "RainbowDomeCold", "protective dome (Helpful) - formula Skip is right"),
            new HostileAccepted("f8ba0223241905e40ba55f8d80c57987", "RainbowDomeElecticity", "protective dome (Helpful) - formula Skip is right"),
            new HostileAccepted("0468209f599da384bacde4800be4965c", "RainbowDomeFire", "protective dome (Helpful) - formula Skip is right"),
            new HostileAccepted("07b0170699247f7488381270cafbb865", "RainbowDomeSonic", "protective dome (Helpful) - formula Skip is right"),
            new HostileAccepted("584a2f29a8f50044fa2b6d9c2a6fb82a", "VoiceOfRenewal", "renewal for allies (J10b) - formula Skip kept"),
            new HostileAccepted("25f741db26e3e27428c4fcb5e60a4ab9", "ChannelRage", "ally rage buff - formula Skip is right"),
            new HostileAccepted("ea373f990b4d8a947a4688e6cbcb1d5f", "ChannelRageCommunal", "ally rage buff - formula Skip is right"),
            new HostileAccepted("644d2c0d029e54d4188bc34216d9d8c0", "ConsumeFear", "self buff - formula Skip is right"),
            new HostileAccepted("0e633cc133207a849915e4d15dec8410", "ConsumeUndead", "consumes own undead (J10b) - formula Skip kept"),
            new HostileAccepted("0a3cd675bbee86a45a22b8bc63b5ec29", "TricksterPhantasmalHealer", "healer for allies (J10b) - formula Skip kept"),
            new HostileAccepted("4bd71b7351aea8047b74ec77c7689237", "TricksterSummonHogOfDesolation", "summon (J10b) - formula Skip kept"),
            new HostileAccepted("d57fcfdfcffa2d346985648cb77390fb", "EdictOfRetaliation", "enemy-only debuff edict (J10b) - formula Echo kept"),
            new HostileAccepted("d47d7434afbc21c4086145718b13f875", "CircleOfClarityPoint", "beneficial area (heuristic sees an area spawn) - formula Skip is right"),
        };
    }
}
