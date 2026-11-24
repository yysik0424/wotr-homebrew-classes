using System.Collections.Generic;
namespace SorrowInquisitor
{
    internal static class NativeEffects
    {
        // Audited native effects with scripted mechanics such as surrendering an item slot.
        internal static readonly HashSet<string> Scripted = new HashSet<string> {
            "25ec6cb6ab1845c48a95f9c20b034220", // Shaken
            "f08a7239aa961f34c8301518e71d4cdf", // Frightened
            "187f88d96a0ef464280706b63635f2af", // BlindnessBuff
            "11cb2fe4fe9c44b448cfe1788ae1ab59", // HoldPersonBuff
            "2cfcce5b62d3e6d4082ec31b58468cc8", // HoldMonsterBuff
            "c0f4e1c24c9cd334ca988ed1bd9d201f", // DominatePersonBuff
            "886c7407dc629dc499b9f1465ff382df", // Confusion
            "4b1f07a71a982824988d7f48cd49f3f8", // HideousLaughterBuff
            "0bc608c3f2b548b44b7146b7530613ac", // SlowBuff
            "a719abac0ea0ce346b401060754cc1c0", // WebGrappled
            "bf6c03b98af9a374c8d61988b5f3ba96", // PhantasmalWebGrappledBuff
            "beb813f8a5f24117ab0587497c094b37", // ShifterWebGrappled
            "ea0361160ee769242b6de33b4e82c093", // PetrifiedBuff
            "77106f698ef4cd54ba277de5e6d0e06d", // ShamblingMoundGrappledBuff
            "f7db19748af8b69469073485a65f37cf", // DisarmMainHandBuff
            "afb1ea46a8c41e04eaab833b7b1b9321", // DisarmOffHandBuff
            "f508d20a350acb34db6293f5654f0b24", // SunderArmorBuff
            "e69fd2ebb67749968f79519f6dc5a5f1", // BurningBuff
            "e52b0f098512c0b4388ef42764c51bdf", // AcidArrowBuff
            "36897b36a4bd63146b1d8509a17fc5ad", // CrushingDespairBuff
            "caae9592917719a41b601b678a8e6ddf", // BestowCurseDeteriorationBuff
            "8f8835d083f31c547a39ebc26ae42159", // BestowCurseGreaterDeteriorationBuff
            "0a52d8761bfd125429842103aed48b90", // BalefulPolymorphBuff
            "4f918c1923ee2964aaa0f4c57bd32501", // ShamanHexEvilEyeACBuff
            "ef8d0845d542b644d9e99c9130a5b110", // ShamanHexEvilEyeAttackBuff
            "5fc895512a6a35d4ca0827bf09eff01a", // ShamanHexEvilEyeSavesBuff
            "3dd413240a9cf614bbd48dacb172d048", // WitchHexEvilEyeACBuff
            "58ce9c1ad7596c64a8a0e1fd4395c2c7", // WitchHexEvilEyeAttackBuff
            "71e20ae6290617b428d03a8047874ea7", // WitchHexEvilEyeSavesBuff
            "5eb68bfe186d71a438d4f85579ce40c1", // Bleed1d4Buff
            "75039846c3d85d940aa96c249b97e562", // Bleed1d6Buff
            "16249b8075ab8684ca105a78a047a5ef", // Bleed2d6Buff
            "8e67a58acc0e4384ba1de26d559d6321", // Bleed3d6Buff
            "dc9ed761b7721c64e98fab507e2a7755", // BleedEnchantment1d6Buff
            "9c70d2ae017665b4b845e6c299cb7439", // FascinateCommonBuff
            "2d4bd347dec7d8648afd502ee40ae661", // FascinateEffectBuff
            "b07a4bdbcf5c46e8832009c693724d30", // FascinateEffectBuffFey
            "e6f2fc5d73d88064583cb828801212f4", // Fatigued
            "46d1b9cc3d0fd36469a471b047d773a2", // Exhausted
            "956331dba5125ef48afe41875a00ca0e", // Nauseated
            "af1e2d232ebbb334aaf25e2a46a92591", // Paralyzed
            "24cf3deb078d3df4d92ba24b176bda97", // Prone
            "4e42460798665fd4cb9173ffa7ada323", // Sickened
            "5e0cd801bac0e95429bb7e4d1bc61a23", // Sleeping
            "df3950af5a783bd4d91ab73eb8fa0fd3", // Staggered
            "09d39b38bb7c6014394b6daced9bacd3", // Stunned
            "5f0f235d30430e040829cf4b1bf1655b", // GrapplingInfusionEffectBuff
            "d36e15046cc86c0418c69b8e13f29602", // WitchHexSlumberBuff
            "7e53a38f2df6bc645bbcb8a6ffb0fd19", // WitchHexAgonyBuff
            "1cf03f5321c4450498b1813f1a72ac82", // WitchHexRestlessSlumberBuff
            "df6d1025da07524429afbae248845ecc", // DazzledBuff
            "02924d88f80c5374eacbbccb499d5778", // CoweringCommonBuff
            "9bd6fd2dcf313c742867c505cb3b8726", // FearBuff
            "e4b8a158e8aabfe41b5e0bd334d8c386", // ThrowingAxeOfSilenceBuff
            "73e09d4c3e3a4d918988a0aae069f372", // SlaughteringSilenceBuff
            "9934fedff1b14994ea90205d189c8759", // DazeBuff
            "d1aea643c260c5e4ea66012876f2b7f5", // EntangledBuff
        };
        internal static readonly HashSet<string> Simple = new HashSet<string> {
            "AddCondition", "BuffStatusCondition", "BuffPoisonStatDamage", "SpellDescriptorComponent",
            "ContextRankConfig", "RemoveWhenCombatEnded", "RemoveBuffIfPartyNotInCombat",
            "RemoveBuffIfCasterIsMissing", "AddStatBonus", "AddContextStatBonus", "AddStatBonusAbilityValue",
            "AddGenericStatBonus", "AddEnergyVulnerability", "BuffDescriptorImmunity", "SpellImmunityToSpellDescriptor", "ShifterGrabTargetBuff"
        };
    }
}
