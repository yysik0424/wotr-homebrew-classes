using System;

namespace Spellfist
{
    // 본편 메이거스 주문 목록 원장: MagusSpellList(4d72e1e7) 의 주문 GUID → (학파, 레벨).
    // 생성: blueprints.zip md5 5da11edf2659a7198acfcb0385231042 (2026-11-25) 의 Classes/Magus/MagusSpellList.jbp SpellsByLevel[*].m_Spells
    //   + 각 주문 jbp 의 SpellComponent.School 직독(scratchpad recon-sf\genledger.py, 1회 생성). 손으로 고치지 말 것 — 시험(CheckSchoolLedger)이 zip 에서 다시 만들어 대조한다.
    // 런타임(SchoolLists.Build): 원장의 모든 GUID 가 자기 학파 목록의 자기 레벨에 있어야 한다(없거나 옮겨지면 throw). 원장 밖 주문(다른 모드 추가)은 INFO 로그 후 허용.
    // 학파 문자열은 SpellSchool 열거 이름(Universalist 포함 — 버리는 주문도 원장에 둔다).
    public static class SchoolLedger
    {
        // Magus spells from DLC folders in the game's blueprint archive. A missing blueprint may be omitted;
        // an installed spell still has to match the original school and level.
        static readonly System.Collections.Generic.HashSet<string> dlcSpells = new System.Collections.Generic.HashSet<string> {
            "2daf19a1e59545fba89b3d2a822c20b4", // SongOfDiscordGreater
            "64923c9c808143c7ab7adba5a27f0ca4", // KeenEdge
            "b0ffc8eaff404f2e9e0a3ee9f4c35486", // SilverDarts
            "b5f4ea77492d4f23ad3a6f429cbfa35e", // ArcanaTheft
            "c6c516e59ca34f34aefb82a850287b97", // ArchonsTrumpet
        };
        internal static bool IsDlcSpell(string guid) { return dlcSpells.Contains(guid); }

        public static readonly (string guid, string school, int level)[] Entries =
        {
            ("95f206566c5261c42aa5b3e7e0d1e36c", "Evocation", 0), // MageLight
            ("652739779aa05504a9ad5db1db6d02ae", "Necromancy", 0), // DisruptUndead
            ("0c852a2405dd9f14a8bbcfaf245ff823", "Conjuration", 0), // AcidSplash
            ("97a23111df7547fd8f6417f9ba9b9775", "Universalist", 0), // DismissAreaEffect
            ("55f14bc84d7c85446b07a1b5dd6b2b4c", "Enchantment", 0), // Daze
            ("5bf3315ce1ed4d94e8805706820ef64d", "Necromancy", 0), // TouchOfFatigueCast
            ("f0f8e5b9808f44e4eadd22b138131d52", "Evocation", 0), // Flare
            ("9af2ab69df6538f4793b2f9c3cc85603", "Evocation", 0), // RayOfFrost
            ("564c2ac83c7844beb1921e69ab159ac6", "Conjuration", 0), // Ignition
            ("9f10909f0be1f5141bf1c102041f93d9", "Conjuration", 1), // Snowball
            ("f001c73999fb5a543a199f890108d936", "Illusion", 1), // Vanish
            ("91da41b9793a4624797921f221db653c", "Illusion", 1), // ColorSpray
            ("ab395d2335d3f384e99dddee8562978f", "Evocation", 1), // ShockingGraspCast
            ("d7fdd79f0f6b6a2418298e936bb68e40", "Transmutation", 1), // MagicWeapon
            ("ef768022b0785eb43a18969903c537c4", "Abjuration", 1), // MageShield
            ("95810d2829895724f950c8c4086056e7", "Conjuration", 1), // CorrosiveTouchCast
            ("4f8181e7a7f1d904fbaea64220e83379", "Transmutation", 1), // ExpeditiousRetreat
            ("4ac47ddb9fa1eaf43a1b6809980cfbd2", "Evocation", 1), // MagicMissile
            ("4783c3709a74a794dbe7c8e7e0b1b038", "Evocation", 1), // BurningHands
            ("2c38da66e5a599347ac95b3294acbe00", "Divination", 1), // TrueStrike
            ("39a602aa80cc96f4597778b6d4d49c0a", "Evocation", 1), // FlareBurst
            ("95851f6e85fe87d4190675db0419d112", "Conjuration", 1), // Grease
            ("4e0e9aba6447d514f88eff1464cc4763", "Transmutation", 1), // ReducePerson
            ("85067a04a97416949b5d1dbf986d93f3", "Transmutation", 1), // StoneFist
            ("450af0402422b0b4980d9c2175869612", "Necromancy", 1), // RayOfEnfeeblement
            ("c60969e7f264e6d4b84a1499fdcf9039", "Transmutation", 1), // EnlargePerson
            ("d4b28341acdfa9443a3a779acb58be51", "Transmutation", 2), // AnimalAspectBase
            ("5181c2ed0190fc34b8a1162783af5bf4", "Conjuration", 2), // StoneCall
            ("42a65895ba0cb3a42b6019039dd2bff1", "Evocation", 2), // MoltenOrb
            ("9a46dfd390f943647ab4395fc997936d", "Conjuration", 2), // AcidArrow
            ("cdb106d53c65bbc4086183d54c3b97c7", "Evocation", 2), // ScorchingRay
            ("4c3d08935262b6544ae97599b3a9556d", "Transmutation", 2), // BullsStrength
            ("ce7dad2b25acf85429b6c9550787b2d9", "Conjuration", 2), // Glitterdust
            ("e1291272c8f48c14ab212a599ad17aac", "Transmutation", 2), // EffortlessArmor
            ("a900628aea19aa74aad0ece0e65d091a", "Transmutation", 2), // BearsEndurance
            ("14ec7a4e52e90fa47a4c8d63c69fd5c1", "Illusion", 2), // Blur
            ("3e4ab69ada402d145a5e0ad3ad4b8564", "Illusion", 2), // MirrorImage
            ("134cb6d492269aa4f8662700ef57449f", "Conjuration", 2), // Web
            ("89940cde01689fb46946b2f8cd7b66b7", "Illusion", 2), // Invisibility
            ("b6010dda6333bcf4093ce20f0063cd41", "Evocation", 2), // FrigidTouchCast
            ("de7a025d48ad5da4991e7d3c682cf69d", "Transmutation", 2), // CatsGrace
            ("8a28a811ca5d20d49a863e832c31cce1", "Necromancy", 3), // VampiricTouchCast
            ("0f92caa35619f234298d95a4b6dda90d", "Transmutation", 3), // MagicWeaponGreater
            ("486eaff58293f6441a5c2759c4872f98", "Transmutation", 3), // Haste
            ("d2cff9243a7ee804cb6d5be47af30c73", "Evocation", 3), // LightningBolt
            ("903092f6488f9ce45a80943923576ab3", "Illusion", 3), // Displacement
            ("fc58ddcff6ab1394eb6c18e9126bb990", "Evocation", 3), // ForcePunchCast
            ("f492622e473d34747806bdb39356eb89", "Transmutation", 3), // Slow
            ("68a9e6d7256f1354289a39003a46d826", "Conjuration", 3), // StinkingCloud
            ("045351f1421ee3f449a9143db701d192", "Transmutation", 3), // Blink
            ("2d81362af43aeac4387a3d4fced489c3", "Evocation", 3), // Fireball
            ("61a7ed778dd93f344a5dacdbad324cc9", "Transmutation", 3), // BeastShapeI
            ("92681f181b507b34ea87018e8f7a528a", "Abjuration", 3), // DispelMagic
            ("8eead52509987034ea9025d60cc05985", "Necromancy", 3), // RayOfExhaustion
            ("c9c56af3b25be3942aa0ffd12f11cf35", "Transmutation", 3), // AnimalAspectGreaterBase
            ("b0ffc8eaff404f2e9e0a3ee9f4c35486", "Conjuration", 3), // SilverDarts
            ("64923c9c808143c7ab7adba5a27f0ca4", "Transmutation", 3), // KeenEdge
            ("2daf19a1e59545fba89b3d2a822c20b4", "Enchantment", 3), // SongOfDiscordGreater
            ("c6c516e59ca34f34aefb82a850287b97", "Evocation", 3), // ArchonsTrumpet
            ("f72f8f03bf0136c4180cd1d70eb773a5", "Evocation", 4), // ControlledFireball
            ("5e826bcdfde7f82468776b55315b2403", "Evocation", 4), // DragonsBreath
            ("5d4028eb28a106d4691ed1b92bbb1915", "Transmutation", 4), // BeastShapeII
            ("f09453607e683784c8fca646eec49162", "Evocation", 4), // Shout
            ("62888999171921e4dafb46de83f4d67d", "Evocation", 4), // ShieldOfDawn
            ("fcb028205a71ee64d98175ff39a0abf9", "Evocation", 4), // IceStorm
            ("4a648b57935a59547b7a2ee86fb4f26a", "Conjuration", 4), // DimensionDoorBase
            ("c66e86905f7606c4eaa5c774f0357b2b", "Abjuration", 4), // Stoneskin
            ("6717dbaef00c0eb4897a1c908a75dfe5", "Illusion", 4), // PhantasmalKiller
            ("2427f2e3ca22ae54ea7337bbab555b16", "Transmutation", 4), // ReducePersonMass
            ("690c90a82bf2e58449c6b541cb8ea004", "Transmutation", 4), // ElementalBodyIBase
            ("66dc49bf154863148bd217287079245e", "Transmutation", 4), // EnlargePersonMass
            ("ecaa0def35b38f949bd1976a6c9539e0", "Illusion", 4), // InvisibilityGreater
            ("b5f4ea77492d4f23ad3a6f429cbfa35e", "Abjuration", 4), // ArcanaTheft
            ("c543eef6d725b184ea8669dd09b3894c", "Conjuration", 5), // AcidicSpray
            ("6d437be73b459594ab103acdcae5b9e2", "Transmutation", 5), // ElementalBodyIIBase
            ("e7c530f8137630f4d9d7ee1aa7b1edc0", "Evocation", 5), // ConeOfCold
            ("ebade19998e1f8542a1b55bd4da766b3", "Evocation", 5), // FireSnake
            ("a34921035f2a6714e9be5ca76c5e34b5", "Necromancy", 5), // VampiricShadowShield
            ("3105d6e9febdc3f41a08d2b7dda1fe74", "Transmutation", 5), // BalefulPolymorph
            ("9b93040dad242eb43ac7de6bb6547030", "Transmutation", 5), // BeastShapeIII
            ("548d339ba87ee56459c98e80167bdf10", "Conjuration", 5), // Cloudkill
            ("e243740dfdb17a246b116b334ed0b165", "Transmutation", 6), // StoneToFlesh
            ("f0f761b808dc4b149b08eaf44b99f633", "Abjuration", 6), // DispelMagicGreater
            ("f767399367df54645ac620ef7b2062bb", "Transmutation", 6), // FormOfTheDragonI
            ("1f6c94d56f178b84ead4c02f1b1e1c48", "Transmutation", 6), // CatsGraceMass
            ("6a234c6dcde7ae94e94e9c36fd1163a7", "Transmutation", 6), // BullsStrengthMass
            ("474ed0aa656cc38499cc9a073d113716", "Necromancy", 6), // UmbralStrike
            ("4aa7942c3e62a164387a73184bca3fc1", "Transmutation", 6), // Disintegrate
            ("4cf3d0fae3239ec478f51e86f49161cb", "Divination", 6), // TrueSeeingCast
            ("645558d63604747428d55f0dd3a4cb58", "Evocation", 6), // ChainLightning
            ("dbf99b00cd35d0a4491c6cc9e771b487", "Conjuration", 6), // AcidFog
            ("f6bcea6db14f0814d99b54856e918b92", "Transmutation", 6), // BearsEnduranceMass
            ("700cfcbd0cb2975419bcab7dbb8c6210", "Evocation", 6), // HellfireRay
            ("940a545a665194b48b722c1f9dd78d53", "Transmutation", 6), // BeastShapeIVBase
            ("27203d62eb3d4184c9aced94f22e1806", "Transmutation", 6), // Transformation
            ("093ed1d67a539ad4c939d9d05cfe192c", "Evocation", 6), // Sirocco
            ("459e6d5aab080a14499e13b407eb3b85", "Transmutation", 6), // ElementalBodyIIIBase
            ("368d7cf2fb69d8a46be5a650f5a5a173", "Conjuration", 6), // WalkThroughSpace
        };
    }
}
