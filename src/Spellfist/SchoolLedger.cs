using System;

namespace Spellfist
{
    // 본편 위저드 주문 목록 원장: WizardSpellList(ba0401fd) 의 주문 GUID → (학파, 레벨).
    // 생성: blueprints.zip md5 5da11edf2659a7198acfcb0385231042 (2026-11-07) 의 Classes/Wizard/WizardSpellList.jbp SpellsByLevel[*].m_Spells
    //   + 각 주문 jbp 의 SpellComponent.School 직독(scratchpad recon-sf\genledger.py, 1회 생성). 손으로 고치지 말 것 — 시험(CheckSchoolLedger)이 zip 에서 다시 만들어 대조한다.
    // 런타임(SchoolLists.Build): 원장의 모든 GUID 가 자기 학파 목록의 자기 레벨에 있어야 한다(없거나 옮겨지면 throw). 원장 밖 주문(다른 모드 추가)은 INFO 로그 후 허용.
    // 학파 문자열은 SpellSchool 열거 이름(Universalist 포함 — 버리는 주문도 원장에 둔다).
    public static class SchoolLedger
    {
        // Wizard spells from DLC folders in the game's blueprint archive. A missing blueprint may be omitted;
        // an installed spell still has to match the original school and level.
        static readonly System.Collections.Generic.HashSet<string> dlcSpells = new System.Collections.Generic.HashSet<string> {
            "0da2046b4517427bb9b2e304ea6342bf", // BoneFists
            "0e1272506f9f4480b7c3e7e1e53b6439", // VengefulComets
            "1626dbd11c4143b2a5641da8f227b78c", // IncendiaryRunes
            "448aa456600749d18c14ad979cc88a67", // ExcruciatingDeformation
            "5e36df08c71748f7936bce310181fb71", // MeteorSwarm
            "64923c9c808143c7ab7adba5a27f0ca4", // KeenEdge
            "8b02310b46e54de1ae9ba7161831938d", // SymbolOfWeakness
            "8bc127ed66044685957e54f19d8dbdb7", // MortalTerror
            "9cebb7c46c8d4c55a0e98da9b103fd37", // SymbolOfVulnerability
            "a2b05555c704458aaadc34be52a63633", // GhoulTouchCast
            "b0ffc8eaff404f2e9e0a3ee9f4c35486", // SilverDarts
            "c0c2ef5955cb415ca482597f1bfc4eca", // CurseOfMagicNegation
            "c6c516e59ca34f34aefb82a850287b97", // ArchonsTrumpet
            "cb55ae9517a444548d3457f91f829679", // Repulsion
            "e03024c8a03f454db5b78660f524757d", // FingerOfDeathSithhud
            "ed22aa8751c049fa915dabfa29712c08", // HauntingMists
            "ff652882623d43f184eb6abef741e20e", // ExplosiveRunes
        };
        internal static bool IsDlcSpell(string guid) { return dlcSpells.Contains(guid); }

        public static readonly (string guid, string school, int level)[] Entries =
        {
            ("0c852a2405dd9f14a8bbcfaf245ff823", "Conjuration", 0), // AcidSplash
            ("55f14bc84d7c85446b07a1b5dd6b2b4c", "Enchantment", 0), // Daze
            ("97a23111df7547fd8f6417f9ba9b9775", "Universalist", 0), // DismissAreaEffect
            ("652739779aa05504a9ad5db1db6d02ae", "Necromancy", 0), // DisruptUndead
            ("f0f8e5b9808f44e4eadd22b138131d52", "Evocation", 0), // Flare
            ("16e23c7a8ae53cc42a93066d19766404", "Transmutation", 0), // Jolt
            ("95f206566c5261c42aa5b3e7e0d1e36c", "Evocation", 0), // MageLight
            ("9af2ab69df6538f4793b2f9c3cc85603", "Evocation", 0), // RayOfFrost
            ("7bc8e27cba24f0e43ae64ed201ad5785", "Abjuration", 0), // Resistance
            ("5bf3315ce1ed4d94e8805706820ef64d", "Necromancy", 0), // TouchOfFatigueCast
            ("564c2ac83c7844beb1921e69ab159ac6", "Conjuration", 0), // Ignition
            ("4783c3709a74a794dbe7c8e7e0b1b038", "Evocation", 1), // BurningHands
            ("bd81a3931aa285a4f9844585b5d97e51", "Necromancy", 1), // CauseFear
            ("91da41b9793a4624797921f221db653c", "Illusion", 1), // ColorSpray
            ("95810d2829895724f950c8c4086056e7", "Conjuration", 1), // CorrosiveTouchCast
            ("8e7cfa5f213a90549aadd18f8f6f4664", "Evocation", 1), // EarPiercingScream
            ("c60969e7f264e6d4b84a1499fdcf9039", "Transmutation", 1), // EnlargePerson
            ("4f8181e7a7f1d904fbaea64220e83379", "Transmutation", 1), // ExpeditiousRetreat
            ("39a602aa80cc96f4597778b6d4d49c0a", "Evocation", 1), // FlareBurst
            ("95851f6e85fe87d4190675db0419d112", "Conjuration", 1), // Grease
            ("3e9d1119d43d07c4c8ba9ebfd1671952", "Transmutation", 1), // HurricaneBow
            ("9e1ad5d6f87d19e4d8883d63a6e35568", "Conjuration", 1), // MageArmor
            ("ef768022b0785eb43a18969903c537c4", "Abjuration", 1), // MageShield
            ("4ac47ddb9fa1eaf43a1b6809980cfbd2", "Evocation", 1), // MagicMissile
            ("433b1faf4d02cc34abb0ade5ceda47c4", "Abjuration", 1), // ProtectionFromAlignment
            ("450af0402422b0b4980d9c2175869612", "Necromancy", 1), // RayOfEnfeeblement
            ("fa3078b9976a5b24caf92e20ee9c0f54", "Necromancy", 1), // RayOfSickening
            ("4e0e9aba6447d514f88eff1464cc4763", "Transmutation", 1), // ReducePerson
            ("ab395d2335d3f384e99dddee8562978f", "Evocation", 1), // ShockingGraspCast
            ("bb7ecad2d3d2c8247a38f44855c99061", "Enchantment", 1), // Sleep
            ("9f10909f0be1f5141bf1c102041f93d9", "Conjuration", 1), // Snowball
            ("85067a04a97416949b5d1dbf986d93f3", "Transmutation", 1), // StoneFist
            ("a5ec7892fb1c2f74598b3a82f3fd679f", "Abjuration", 1), // StunningBarrier
            ("8fd74eddd9b6c224693d9ab241f25e84", "Conjuration", 1), // SummonMonsterISingle
            ("ad10bfec6d7ae8b47870e3a545cc8900", "Transmutation", 1), // TouchOfGracelessnessCast
            ("2c38da66e5a599347ac95b3294acbe00", "Divination", 1), // TrueStrike
            ("f001c73999fb5a543a199f890108d936", "Illusion", 1), // Vanish
            ("88367310478c10b47903463c5d0152b0", "Enchantment", 1), // Hypnotism
            ("d7fdd79f0f6b6a2418298e936bb68e40", "Transmutation", 1), // MagicWeapon
            ("1626dbd11c4143b2a5641da8f227b78c", "Abjuration", 1), // IncendiaryRunes
            ("0da2046b4517427bb9b2e304ea6342bf", "Necromancy", 2), // BoneFists
            ("8bc127ed66044685957e54f19d8dbdb7", "Enchantment", 2), // MortalTerror
            ("9a46dfd390f943647ab4395fc997936d", "Conjuration", 2), // AcidArrow
            ("d4b28341acdfa9443a3a779acb58be51", "Transmutation", 2), // AnimalAspectBase
            ("a900628aea19aa74aad0ece0e65d091a", "Transmutation", 2), // BearsEndurance
            ("46fd02ad56c35224c9c91c88cd457791", "Necromancy", 2), // Blindness
            ("14ec7a4e52e90fa47a4c8d63c69fd5c1", "Illusion", 2), // Blur
            ("b7731c2b4fa1c9844a092329177be4c3", "Necromancy", 2), // Boneshaker
            ("4c3d08935262b6544ae97599b3a9556d", "Transmutation", 2), // BullsStrength
            ("eaac3d36e0336cb479209a6f65e25e7c", "Evocation", 2), // BurningArc
            ("de7a025d48ad5da4991e7d3c682cf69d", "Transmutation", 2), // CatsGrace
            ("0b101dd5618591e478f825f0eef155b4", "Necromancy", 2), // CommandUndead
            ("29ccc62632178d344ad0be0865fd3113", "Conjuration", 2), // CreatePit
            ("446f7bf201dc1934f96ac0a26e324803", "Transmutation", 2), // EaglesSplendor
            ("7a5b5bf845779a941a67251539545762", "Necromancy", 2), // FalseLife
            ("ae4d3ad6a8fda1542acf2e9bbc13d113", "Transmutation", 2), // FoxsCunning
            ("b6010dda6333bcf4093ce20f0063cd41", "Evocation", 2), // FrigidTouchCast
            ("ce7dad2b25acf85429b6c9550787b2d9", "Conjuration", 2), // Glitterdust
            ("fd4d9fd7f87575d47aafe2a64a6e2d8d", "Enchantment", 2), // HideousLaughter
            ("89940cde01689fb46946b2f8cd7b66b7", "Illusion", 2), // Invisibility
            ("3e4ab69ada402d145a5e0ad3ad4b8564", "Illusion", 2), // MirrorImage
            ("42a65895ba0cb3a42b6019039dd2bff1", "Evocation", 2), // MoltenOrb
            ("f0455c9295b53904f9e02fc571dd2ce1", "Transmutation", 2), // OwlsWisdom
            ("dee3074b2fbfb064b80b973f9b56319e", "Necromancy", 2), // PerniciousPoison
            ("2cadf6c6350e4684baa109d067277a45", "Abjuration", 2), // ProtectionFromAlignmentCommunal
            ("c28de1f98a3f432448e52e5d47c73208", "Abjuration", 2), // ProtectionFromArrows
            ("21ffef7791ce73f468b6fca4d9371e8b", "Abjuration", 2), // ResistEnergy
            ("08cb5f4c3b2695e44971bf5c45205df0", "Necromancy", 2), // Scare
            ("cdb106d53c65bbc4086183d54c3b97c7", "Evocation", 2), // ScorchingRay
            ("30e5dc243f937fc4b95d2f8f4e1b7ff3", "Divination", 2), // SeeInvisibility
            ("82962a820ebc0e7408b8582fdc3f4c0c", "Divination", 2), // SenseVitals
            ("5181c2ed0190fc34b8a1162783af5bf4", "Conjuration", 2), // StoneCall
            ("970c6db48ff0c6f43afc9dbb48780d03", "Conjuration", 2), // SummonElementalSmallBase
            ("1724061e89c667045a6891179ee2e8e7", "Conjuration", 2), // SummonMonsterIIBase
            ("134cb6d492269aa4f8662700ef57449f", "Conjuration", 2), // Web
            ("a2b05555c704458aaadc34be52a63633", "Necromancy", 2), // GhoulTouchCast
            ("ed22aa8751c049fa915dabfa29712c08", "Illusion", 2), // HauntingMists
            ("57fcf8016cf04da4a8b33d2add14de7e", "Necromancy", 3), // AnimateDeadLesser
            ("0a2f7c6aa81bc6548ac7780d8b70bcbc", "Evocation", 3), // BatteringBlast
            ("61a7ed778dd93f344a5dacdbad324cc9", "Transmutation", 3), // BeastShapeI
            ("045351f1421ee3f449a9143db701d192", "Transmutation", 3), // Blink
            ("7658b74f626c56a49939d9c20580885e", "Enchantment", 3), // DeepSlumber
            ("92681f181b507b34ea87018e8f7a528a", "Abjuration", 3), // DispelMagic
            ("903092f6488f9ce45a80943923576ab3", "Illusion", 3), // Displacement
            ("2d81362af43aeac4387a3d4fced489c3", "Evocation", 3), // Fireball
            ("fc58ddcff6ab1394eb6c18e9126bb990", "Evocation", 3), // ForcePunchCast
            ("486eaff58293f6441a5c2759c4872f98", "Transmutation", 3), // Haste
            ("5ab0d42fb68c9e34abae4921822b9d63", "Enchantment", 3), // Heroism
            ("c7104f7526c4c524f91474614054547e", "Enchantment", 3), // HoldPerson
            ("d2cff9243a7ee804cb6d5be47af30c73", "Evocation", 3), // LightningBolt
            ("96c9d98b6a9a7c249b6c4572e4977157", "Abjuration", 3), // ProtectionFromArrowsCommunal
            ("d2f116cfe05fcdd4a94e80143b67046f", "Abjuration", 3), // ProtectionFromEnergy
            ("97b991256e43bb140b263c326f690ce2", "Enchantment", 3), // Rage
            ("8eead52509987034ea9025d60cc05985", "Necromancy", 3), // RayOfExhaustion
            ("7bb0c402f7f789d4d9fae8ca87b4c7e2", "Abjuration", 3), // ResistEnergyCommunal
            ("1a045f845778dc54db1c2be33a8c3c0a", "Divination", 3), // SeeInvisibilityCommunal
            ("f492622e473d34747806bdb39356eb89", "Transmutation", 3), // Slow
            ("46097f610219ac445b4d6403fc596b9f", "Conjuration", 3), // SpikedPit
            ("68a9e6d7256f1354289a39003a46d826", "Conjuration", 3), // StinkingCloud
            ("5d61dde0020bbf54ba1521f7ca0229dc", "Conjuration", 3), // SummonMonsterIIIBase
            ("8a28a811ca5d20d49a863e832c31cce1", "Necromancy", 3), // VampiricTouchCast
            ("0f92caa35619f234298d95a4b6dda90d", "Transmutation", 3), // MagicWeaponGreater
            ("0e1272506f9f4480b7c3e7e1e53b6439", "Evocation", 3), // VengefulComets
            ("448aa456600749d18c14ad979cc88a67", "Transmutation", 3), // ExcruciatingDeformation
            ("ff652882623d43f184eb6abef741e20e", "Abjuration", 3), // ExplosiveRunes
            ("64923c9c808143c7ab7adba5a27f0ca4", "Transmutation", 3), // KeenEdge
            ("b0ffc8eaff404f2e9e0a3ee9f4c35486", "Conjuration", 3), // SilverDarts
            ("989ab5c44240907489aba0a8568d0603", "Necromancy", 4), // BestowCurse
            ("48e2744846ed04b4580be1a3343a5d3d", "Necromancy", 4), // Contagion
            ("4baf4109145de4345861fe0f2209d903", "Enchantment", 4), // CrushingDespair
            ("dd2918e4a77c50044acba1ac93494c36", "Enchantment", 4), // OverwhelmingGrief
            ("b48674cef2bff5e478a007cf57d8345b", "Abjuration", 4), // RemoveCurse
            ("1407fb5054d087d47a4c40134c809f12", "Conjuration", 4), // AcidPit
            ("c9c56af3b25be3942aa0ffd12f11cf35", "Transmutation", 4), // AnimalAspectGreaterBase
            ("4b76d32feb089ad4499c3a1ce8e1ac27", "Necromancy", 4), // AnimateDead
            ("5d4028eb28a106d4691ed1b92bbb1915", "Transmutation", 4), // BeastShapeII
            ("f2f1efac32ea2884e84ecaf14657298b", "Necromancy", 4), // Boneshatter
            ("cf6c901fb7acc904e85c63b342e9c949", "Enchantment", 4), // ConfusionSpell
            ("f72f8f03bf0136c4180cd1d70eb773a5", "Evocation", 4), // ControlledFireball
            ("4a648b57935a59547b7a2ee86fb4f26a", "Conjuration", 4), // DimensionDoorBase
            ("5e826bcdfde7f82468776b55315b2403", "Evocation", 4), // DragonsBreath
            ("690c90a82bf2e58449c6b541cb8ea004", "Transmutation", 4), // ElementalBodyIBase
            ("f34fb78eaaec141469079af124bcfa0f", "Necromancy", 4), // Enervation
            ("66dc49bf154863148bd217287079245e", "Transmutation", 4), // EnlargePersonMass
            ("dc6af3b4fd149f841912d8a3ce0983de", "Necromancy", 4), // FalseLifeGreater
            ("d2aeac47450c76347aebbc02e4f463e0", "Necromancy", 4), // Fear
            ("fcb028205a71ee64d98175ff39a0abf9", "Evocation", 4), // IceStorm
            ("ecaa0def35b38f949bd1976a6c9539e0", "Illusion", 4), // InvisibilityGreater
            ("e48638596c955a74c8a32dbc90b518c1", "Transmutation", 4), // ObsidianFlow
            ("6717dbaef00c0eb4897a1c908a75dfe5", "Illusion", 4), // PhantasmalKiller
            ("76a629d019275b94184a1a8733cac45e", "Abjuration", 4), // ProtectionFromEnergyCommunal
            ("4b8265132f9c8174f87ce7fa6d0fe47b", "Illusion", 4), // RainbowPattern
            ("2427f2e3ca22ae54ea7337bbab555b16", "Transmutation", 4), // ReducePersonMass
            ("caac251ca7601324bbe000372a0a1005", "Illusion", 4), // ShadowConjuration
            ("f09453607e683784c8fca646eec49162", "Evocation", 4), // Shout
            ("c66e86905f7606c4eaa5c774f0357b2b", "Abjuration", 4), // Stoneskin
            ("e42b1dbff4262c6469a9ff0a6ce730e3", "Conjuration", 4), // SummonElementalMediumBase
            ("7ed74a3ec8c458d4fb50b192fd7be6ef", "Conjuration", 4), // SummonMonsterIVBase
            ("48a555180a109e545a6e367b1bd3f535", "Divination", 4), // SymbolOfRevelation
            ("84ccca10da2a4484c89a837fbea2a829", "Conjuration", 4), // TouchOfSlimeCast
            ("16ce660837fb2544e96c3b7eaad73c63", "Evocation", 4), // VolcanicStorm
            ("c0c2ef5955cb415ca482597f1bfc4eca", "Abjuration", 4), // CurseOfMagicNegation
            ("75a10d5a635986641bfbcceceec87217", "Transmutation", 5), // AngelicAspect
            ("20b548bf09bb3ea4bafea78dcb4f3db6", "Transmutation", 5), // Echolocation
            ("c543eef6d725b184ea8669dd09b3894c", "Conjuration", 5), // AcidicSpray
            ("56923211d2ac95e43b8ac5031bab74d8", "Transmutation", 5), // AnimalGrowth
            ("3105d6e9febdc3f41a08d2b7dda1fe74", "Transmutation", 5), // BalefulPolymorph
            ("9b93040dad242eb43ac7de6bb6547030", "Transmutation", 5), // BeastShapeIII
            ("7792da00c85b9e042a0fdfc2b66ec9a8", "Abjuration", 5), // BreakEnchantment
            ("548d339ba87ee56459c98e80167bdf10", "Conjuration", 5), // Cloudkill
            ("e7c530f8137630f4d9d7ee1aa7b1edc0", "Evocation", 5), // ConeOfCold
            ("3fce8e988a51a2a4ea366324d6153001", "Enchantment", 5), // ConstrictingCoils
            ("95f7cdcec94e293489a85afdf5af1fd7", "Abjuration", 5), // Dismissal
            ("d7cbd2004ce66a042aeab2e95a3c5c61", "Enchantment", 5), // DominatePerson
            ("6d437be73b459594ab103acdcae5b9e2", "Transmutation", 5), // ElementalBodyIIBase
            ("444eed6e26f773a40ab6e4d160c67faa", "Enchantment", 5), // Feeblemind
            ("ebade19998e1f8542a1b55bd4da766b3", "Evocation", 5), // FireSnake
            ("07b608fab304f894880898dc0764e6e5", "Transmutation", 5), // Geniekind
            ("41e8a952da7a5c247b3ec1c2dbb73018", "Enchantment", 5), // HoldMonster
            ("f63f4d1806b78604a952b3958892ce1c", "Conjuration", 5), // HungryPit
            ("65e8d23aef5e7784dbeb27b1fca40931", "Evocation", 5), // IcyPrison
            ("265582bc494c4b12b5860b508a2f89a2", "Abjuration", 5), // LifeBubble
            ("eabf94e4edc6e714cabd96aa69f8b207", "Enchantment", 5), // MindFog
            ("12fb4a4c22549c74d949e2916a2f0b6a", "Illusion", 5), // PhantasmalWeb
            ("93d9d74dac46b9b458d4d2ea7f4b1911", "Transmutation", 5), // PolymorphBase
            ("237427308e48c3341b3d532b9d3a001f", "Illusion", 5), // ShadowEvocation
            ("7c5d556b9a5883048bf030e20daebe31", "Abjuration", 5), // StoneskinCommunal
            ("89404dd71edc1aa42962824b44156fe5", "Conjuration", 5), // SummonElementalLargeBase
            ("630c8b85d9f07a64f917d79cb5905741", "Conjuration", 5), // SummonMonsterVBase
            ("8fb1a1670b6e1f84b89ea846f589b627", "Divination", 5), // Thoughtsense
            ("a34921035f2a6714e9be5ca76c5e34b5", "Necromancy", 5), // VampiricShadowShield
            ("8878d0c46dfbd564e9d5756349d5e439", "Necromancy", 5), // WavesOfFatigue
            ("1cde0691195feae45bab5b83ea3f221e", "Necromancy", 5), // WrackingRay
            ("cb55ae9517a444548d3457f91f829679", "Abjuration", 6), // Repulsion
            ("f8cea58227f59c64399044a82c9735c4", "Conjuration", 6), // ChainsOfLight
            ("d316d3d94d20c674db2c24d7de96f6a7", "Enchantment", 6), // Serenity
            ("dbf99b00cd35d0a4491c6cc9e771b487", "Conjuration", 6), // AcidFog
            ("d42c6d3f29e07b6409d670792d72bc82", "Necromancy", 6), // BansheeBlast
            ("f6bcea6db14f0814d99b54856e918b92", "Transmutation", 6), // BearsEnduranceMass
            ("940a545a665194b48b722c1f9dd78d53", "Transmutation", 6), // BeastShapeIVBase
            ("6a234c6dcde7ae94e94e9c36fd1163a7", "Transmutation", 6), // BullsStrengthMass
            ("1f6c94d56f178b84ead4c02f1b1e1c48", "Transmutation", 6), // CatsGraceMass
            ("645558d63604747428d55f0dd3a4cb58", "Evocation", 6), // ChainLightning
            ("a89dcbbab8f40e44e920cc60636097cf", "Necromancy", 6), // CircleOfDeath
            ("7f71a70d822af94458dc1a235507e972", "Enchantment", 6), // CloakofDreams
            ("5ef85d426783a5347b420546f91a677b", "Evocation", 6), // ColdIceStrike
            ("4aa7942c3e62a164387a73184bca3fc1", "Transmutation", 6), // Disintegrate
            ("f0f761b808dc4b149b08eaf44b99f633", "Abjuration", 6), // DispelMagicGreater
            ("2caa607eadda4ab44934c5c9875e01bc", "Transmutation", 6), // EaglesSplendorMass
            ("6303b404df12b0f4793fa0763b21dd2c", "Evocation", 6), // ElementalAssessor
            ("459e6d5aab080a14499e13b407eb3b85", "Transmutation", 6), // ElementalBodyIIIBase
            ("3167d30dd3c622c46b0c0cb242061642", "Necromancy", 6), // Eyebite
            ("f767399367df54645ac620ef7b2062bb", "Transmutation", 6), // FormOfTheDragonI
            ("2b24159ad9907a8499c2313ba9c0f615", "Transmutation", 6), // FoxsCunningMass
            ("700cfcbd0cb2975419bcab7dbb8c6210", "Evocation", 6), // HellfireRay
            ("e15e5e7045fda2244b98c8f010adfe31", "Enchantment", 6), // HeroismGreater
            ("9f5ada581af3db4419b54db77f44e430", "Transmutation", 6), // OwlsWisdomMass
            ("1f2e6019ece86d64baa5effa15e81ecc", "Illusion", 6), // PhantasmalPutrefaction
            ("093ed1d67a539ad4c939d9d05cfe192c", "Evocation", 6), // Sirocco
            ("e243740dfdb17a246b116b334ed0b165", "Transmutation", 6), // StoneToFlesh
            ("766ec978fa993034f86a372c8eb1fc10", "Conjuration", 6), // SummonElementalHugeBase
            ("e740afbab0147944dab35d83faa0ae1c", "Conjuration", 6), // SummonMonsterVIBase
            ("7d700cdf260d36e48bb7af3a8ca5031f", "Transmutation", 6), // TarPool
            ("27203d62eb3d4184c9aced94f22e1806", "Transmutation", 6), // Transformation
            ("4cf3d0fae3239ec478f51e86f49161cb", "Divination", 6), // TrueSeeingCast
            ("a9a52760290591844a96d0109e30e04d", "Necromancy", 6), // UndeathToDeath
            ("76a11b460be25e44ca85904d6806e5a3", "Necromancy", 6), // CreateUndeadBase
            ("8b02310b46e54de1ae9ba7161831938d", "Necromancy", 7), // SymbolOfWeakness
            ("fa08cb49ade3eee42b5fd42bd33cb407", "Divination", 7), // TrueSeeingCommunal
            ("82a5b848c05e3f342b893dedb1f9b446", "Necromancy", 7), // PlagueStorm
            ("474ed0aa656cc38499cc9a073d113716", "Necromancy", 7), // UmbralStrike
            ("d361391f645db984bbf58907711a146a", "Abjuration", 7), // Banishment
            ("8c29e953190cc67429dc9c701b16b7c2", "Evocation", 7), // CausticEruption
            ("f333185ae986b2a45823cce86535a122", "Abjuration", 7), // CircleOfClarity
            ("376db0590f3ca4945a8b6dc16ed14975", "Transmutation", 7), // ElementalBodyIVBase
            ("6f1dcf6cfa92d1948a740195707c0dbe", "Necromancy", 7), // FingerOfDeath
            ("98734a2665c18cd4db71878b0532024a", "Transmutation", 7), // Firebrand
            ("666556ded3a32f34885e8c318c3a0ced", "Transmutation", 7), // FormOfTheDragonII
            ("defbbeaef79eda64abc645036228a31b", "Enchantment", 7), // HoldPersonMass
            ("89778dc261fe6094bb2445cb389842d2", "Transmutation", 7), // IceBody
            ("2b044152b3620c841badb090e01ed9de", "Enchantment", 7), // Insanity
            ("98310a099009bbd4dbdf66bcef58b4cd", "Illusion", 7), // InvisibilityMass
            ("15a04c40f84545949abeedef7279751a", "Conjuration", 7), // JoyfulRapture
            ("5c8cde7f0dcec4e49bfa2632dfe2ecc0", "Evocation", 7), // KiShout
            ("da1b292d91ba37948893cdbe9ea89e28", "Transmutation", 7), // LegendaryProportions
            ("a9fc28e147dbb364ea4a3c1831e7e55f", "Transmutation", 7), // PolymorphGreaterBase
            ("261e1788bfc5ac1419eec68b1d485dbc", "Enchantment", 7), // PowerWordBlind
            ("b22fd434bdb60fb4ba1068206402c4cf", "Evocation", 7), // PrismaticSpray
            ("df7d13c967bce6a40bec3ba7c9f0e64c", "Transmutation", 7), // ResonatingWord
            ("08255ea4cdd812341af93f9cd113acb9", "Illusion", 7), // ShadowConjurationGreater
            ("8eb769e3b583f594faabe1cfdb0bb696", "Conjuration", 7), // SummonElementalGreaterBase
            ("ab167fd8203c1314bac6568932f1752f", "Conjuration", 7), // SummonMonsterVIIBase
            ("368d7cf2fb69d8a46be5a650f5a5a173", "Conjuration", 7), // WalkThroughSpace
            ("1e2d1489781b10a45a3b70192bba9be3", "Enchantment", 7), // WavesOfEctasy
            ("3e4d3b9a5bd03734d9b053b9067c2f38", "Necromancy", 7), // WavesOfExhaustion
            ("e03024c8a03f454db5b78660f524757d", "Necromancy", 7), // FingerOfDeathSithhud
            ("c6c516e59ca34f34aefb82a850287b97", "Evocation", 7), // ArchonsTrumpet
            ("b1c7576bd06812b42bda3f09ab202f14", "Transmutation", 8), // AngelicAspectGreater
            ("fd0d3840c48cafb44bb29e8eb74df204", "Evocation", 8), // ShoutGreater
            ("8ba9b6e4df4c46a597154e2b8e7e6e4a", "Necromancy", 8), // CreateUndeadGreaterBase
            ("c3d2294a6740bc147870fff652f3ced5", "Necromancy", 8), // DeathClutch
            ("740d943e42b60f64a8de74926ba6ddf7", "Enchantment", 8), // EuphoricTranquilityCast
            ("1cdc4ad4c208246419b98a35539eafa6", "Transmutation", 8), // FormOfTheDragonIII
            ("e788b02f8d21014488067bdd3ba7b325", "Transmutation", 8), // FrightfulAspect
            ("08323922485f7e246acb3d2276515526", "Necromancy", 8), // HorridWilting
            ("198fcc43490993f49899ed086fe723c1", "Transmutation", 8), // IronBody
            ("df2a0ba6b6dcecf429cbb80a56fee5cf", "Abjuration", 8), // MindBlank
            ("17696c144a0194c478cbe402b496cb23", "Evocation", 8), // PolarRay
            ("f958ef62eea5050418fb92dfa944c631", "Enchantment", 8), // PowerWordStun
            ("0e67fa8f011662c43934d486acc50253", "Divination", 8), // PredictionOfFailure
            ("42aa71adc7343714fa92e471baa98d42", "Abjuration", 8), // ProtectionFromSpells
            ("dd3dacafcf40a0145a5824c838e2698d", "Conjuration", 8), // RiftOfRuin
            ("4dc60d08c6c4d3c47b413904e4de5ff0", "Illusion", 8), // ScintillatingPattern
            ("7ef49f184922063499b8f1346fb7f521", "Conjuration", 8), // Seamantle
            ("3c4a2d4181482e84d9cd752ef8edc3b6", "Illusion", 8), // ShadowEvocationGreater
            ("b4afacd337dac4a40a769a567c038ab7", "Necromancy", 8), // Soulreaver
            ("7cfbefe0931257344b2cb7ddc4cdff6f", "Evocation", 8), // Stormbolts
            ("8a7f8c1223bda1541b42fd0320cdbe2b", "Conjuration", 8), // SummonElementalElderBase
            ("d3ac756a229830243a72e84f3ab050d0", "Conjuration", 8), // SummonMonsterVIIIBase
            ("e96424f70ff884947b06f41a765b7658", "Evocation", 8), // Sunburst
            ("6101d0f0720927e4ca413de7b3c4b7e5", "Necromancy", 8), // BestowCurseGreater
            ("5e36df08c71748f7936bce310181fb71", "Evocation", 9), // MeteorSwarm
            ("9cebb7c46c8d4c55a0e98da9b103fd37", "Abjuration", 9), // SymbolOfVulnerability
            ("41cf93453b027b94886901dbfc680cb9", "Enchantment", 9), // OverwhelmingPresence
            ("01300baad090d634cb1a1b2defe068d6", "Conjuration", 9), // ClashingRocks
            ("3c17035ec4717674cae2e841a190e757", "Enchantment", 9), // DominateMonster
            ("37302f72b06ced1408bf5bb965766d46", "Necromancy", 9), // EnergyDrain
            ("08ccad78cac525040919d51963f9ac39", "Transmutation", 9), // FieryBody
            ("1f01a098d737ec6419aedc4e7ad61fdd", "Divination", 9), // Foresight
            ("43740dab07286fe4aa00a6ee104ce7c1", "Enchantment", 9), // HeroicInvocation
            ("7f4b66a2b1fdab142904a263c7866d46", "Enchantment", 9), // HoldMonsterMass
            ("1852a9393a23d5741b650a1ea7078abc", "Evocation", 9), // IcyPrisonMass
            ("87a29febd010993419f2a4a9bee11cfc", "Abjuration", 9), // MindBlankCommunal
            ("2f8a67c483dfa0f439b293e094ca9e3c", "Enchantment", 9), // PowerWordKill
            ("70e12191790f69a439ea0132c75f83aa", "Illusion", 9), // Shades
            ("52b5df2a97df18242aec67610616ded0", "Conjuration", 9), // SummonMonsterIXBase
            ("d8144161e352ca846a73cf90e85bf9ac", "Conjuration", 9), // Tsunami
            ("b24583190f36a8442b212e45226c54fc", "Necromancy", 9), // WailOfBanshee
            ("870af83be6572594d84d276d7fc583e0", "Illusion", 9), // Weird
            ("5d8f1da2fdc0b9242af9f326f9e507be", "Evocation", 9), // WindsOfVengeance
            ("22b9044aa229815429d57d0a30e4b739", "Transmutation", 9), // Shapechange
        };
    }
}
