namespace ChaosWarrior
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다, modinfo testSources). 발급: python -c "import uuid;print(uuid.uuid4().hex)" (2026-11-14).
    public static class Guids
    {
         
        public const string Archetype = "a810903a2ebc4a77a65a0f1cb8350c13";
         
        public const string RageResource = "2a6c2e2c0e0c430aa392873fd6ad0f15";
        public const string RageBuff = "0f846ed999844949b869cd34c6f0cc23";
        public const string RageAbility = "5845286e950c4dcdb639f9766d0a2c79";
        public const string RageFeature = "69b99f7fa6e74dad8268d8c35969bc7f";
        public const string RageGreaterFeature = "1a0b0d3e944e4205abd925fe360a9e0d";
        public const string RageMightyFeature = "f3775b667d604c4c90eadcf9e213f46e";
        // 2026-11-16 소유자 결정(몽크 기 일격 구조 3·7·10): 분노 중 자연 무기 단계 버프(숨김, 분노 버프의 자식) — 마법 · 냉철·은 · 혼돈·악.
        public const string RageStrikeMagicBuff = "fffe317e51b84719a39b8020068dea6b";
        public const string RageStrikeMaterialBuff = "2e0e468e46cf453f96e36ade5ac5a08f";
        public const string RageStrikeAlignmentBuff = "da185653937748e680355c3def55de25";
         
        public const string MutationFeature = "66a1dadb237e427798e3b92a2c04a72a";
        public const string MarkEnlarge = "bd308951c6d84ffcb8c7be01deb56869";
        public const string MarkToughHide = "480c6733119b4babb0360825539de709";
        public const string MarkRegeneration = "c04be94fb71645438f316dfbde4a8b03";
        public const string MarkSwiftness = "7000a9c1a9394363a2d7d476db21afb9";
        public const string MarkThornSkin = "491f61111a7344d0885d3a8f6488f753";
        public const string MarkChaosBlood = "b183e95c07e340b5ada4b4a1476be851";
        public const string MarkElementalShell = "041f9a40768b49649606d95b9e4308d3";
        public const string MarkPainAmplify = "f9d5498200754232965a5027008f9904";
        public const string MarkExtraArm = "31add83a99a4415b84fe8e6cd06dc3c4";
        public const string MarkHorrificForm = "16c0b00e31c04c29a50a3a6464b08e76";
        public const string MarkLongLimbs = "7b0838189d3b48a1ab289a511c5ab0a6";
        public const string MarkTwistedLegs = "fa8d56db36984c2dbc84d48d6c4a11c5";
        public const string MarkBlurredEyes = "f4a4194d918e403a9a378bba904516c1";
        public const string MarkExposedNerves = "757e10d074974c2295a97c68a45e4c1f";
        public const string MarkBurstingVeins = "64c294ab937b476ebff91aa5894391b3";
        public const string MarkSeethingMind = "5a628f69d8d244a6a36201e12ce497c4";
        // 개편 O5(계획 2026-11-16 원장 A #16~26): 자연 공격 변이 표식 11(ChaosRules.Mutations 색인 16~26 순서). 발급 2026-11-17: python -I -c "import uuid;print(uuid.uuid4().hex)" 하나씩, 발급 직후 git grep 0건 확인.
        public const string MarkFangs = "ead2138d2fed404682bf555bad0f96a2";
        public const string MarkClaws = "73ecbc7c15e545d9905720aab294e89d";
        public const string MarkHorns = "53ab079b37c942c38bc12b6962b8c6e2";
        public const string MarkTail = "b8890db4be08447f8fef609469153f9d";
        public const string MarkTentacle = "729893c372c94c2e91d1ae56837796a1";
        public const string MarkStinger = "bb9bfe9996ef48e79a7a5faa609e248f";
        public const string MarkWings = "4344b452d0ec4d39b69073e5533fb9bd";
        public const string MarkHooves = "67220f2d4f26496890773e689873d33a";
        public const string MarkTalons = "74f4eba3206241678af96ffd8e3f3150";
        public const string MarkPincer = "46afa0b4e1044540a9060a9132082cb8";
        public const string MarkNeuroStinger = "faf48d336da542af9e705716b72355ed";
        // 개편 O6(계획 2026-11-16 원장 A #27~31): 양면 변이 표식 5(ChaosRules.Mutations 색인 27~31 순서). 발급 2026-11-17: python -I -c "import uuid;print(uuid.uuid4().hex)" 하나씩, 발급 직후 git grep 0건 확인.
        public const string MarkBloodthirst = "e530a69b3c64470cb7f41cb49cce7193";
        public const string MarkBloatedMuscle = "babab8ccab804d15b965ea6b25705f17";
        public const string MarkStoneShell = "c0f3c937bba440e792ab7e63d619de1e";
        public const string MarkRawNerves = "291fd08f6a074ba78fa86351eb34e833";
        public const string MarkPainResonance = "149cdb03e9784bd0b3f5f598bdd29b60";
        public const string ShellFire = "1fead04efae944da8bafd7e32e107aab";
        public const string ShellCold = "1a36a788ca58495ca0aff56ade647f55";
        public const string ShellAcid = "a843e91d999644e4870844c79405c856";
        public const string ShellElectricity = "2e974899f2204761a595cc27ef9e90f2";
         
        public const string HorrorArea = "684d304ad413453b82e8c0b3b2eba646";
        public const string HorrorShakenBuff = "8e5548e5ca3348c1beda0c0d5c80f6ed";
         
        public const string PainBuff = "0a1f6f81b43448ab88119bbead719c6f";
        public const string PainProbeBuff = "aa6f81a655324c78bc65bb9e6e5220dd";
        public const string PainSaveSource = "d07ffb469a4a4c868f9f904d4cf46e06";
        public const string PainAoeFeature = "badc657d5a2b4674856895e3cca7ba0e";
        // 개편 O7(계획 2026-11-16 Task O7): 고통 초과 계수 버프(벌점 상한을 넘은 겹 — 숨김, 부품 0). 생성 python -I uuid4 2026-11-17, git grep 중복 0.
        //   독립 검토 R3-F1 재구성(소유자 2026-11-17) 뒤에는 「옛 세이브 전용」 — 등록만 하고 새로 붙이지 않는다(Pain.LegacyOverflowBuff). GUID 는 세이브 호환 때문에 그대로.
        public const string PainOverflowBuff = "8f07090aa73b42a3a33464bcceeb4922";
        // 독립 검토 R3-F1 재구성(소유자 2026-11-17 「계수 버프 하나로 재구성」): 고통 계수 버프(총 겹 0..8 의 단일 원장 — 숨김, NotDispelable, 벌점 버프의 부모). 생성 python -I uuid4 2026-11-17, git grep 중복 0.
        public const string PainCounterBuff = "1c5cbdd5c72e4448952672de73f56a42";
         
        public const string CheckWillMinus50 = "3e61983462c14c9b8228bdfb89f2ee71";
        public const string CheckWillPlus50 = "aca28fe0bcf645348e715ae9b7a8140c";
        public const string CheckMindBuffImmune = "3132c31ce02143be881aab5c4eda7d5e";
        public const string CheckMindSpellImmune = "43fb5e141d2d4a558ab68f67cd5b840a";
        // 개편 O5 점검 전용(S5c foreign-bite-excludes-fangs): 본편 Bite1d6 사지를 주는 특성 — 「다른 출처의 물기」 흉내. 발급 2026-11-17(같은 방법, git grep 0건).
        public const string CheckForeignBite = "77f540a3724e4a7ba8075faecdba5433";
        // 개편 O4(계획 2026-11-16 「단일 원장 B」): 자연 무기 복제 12(NaturalWeaponPlan.Rows 순서). 발급 2026-11-17: python -I -c "import uuid;print(uuid.uuid4().hex)" 하나씩,
        // 발급 직후 git grep 0건 확인. 중복·본편 충돌은 시험 `exec: own guids unique` · `jbp: own guids not vanilla`, 게임 안에서는 BlueprintUtil.Create 가 캐시에 이미 있는 GUID 를 거부한다.
        public const string WeaponSlam = "dfdd7e160e7f4bffb443efd3db8eeaea";
        public const string WeaponFangs = "753df8284c3f4de3b4797b6174223e5b";
        public const string WeaponClaws = "73c729305227484caa228ea377906979";
        public const string WeaponHorns = "cf27d2b156554e1684f477a893d9275a";
        public const string WeaponTail = "6deaa2f52d3c4be5ba0c55b87c0ff39e";
        public const string WeaponTentacle = "6f3c9fe685b84d82a7ab21d37a9c9b16";
        public const string WeaponStinger = "1af62a5e4bb149a3ab1fb42b8cd71150";
        public const string WeaponWings = "ebf3aa0d9bcb4f3cb1a48669aafe6d2f";
        public const string WeaponHooves = "5af81d1d82184ed5ad64ba4d7ea1399b";
        public const string WeaponTalons = "92dda3299e1a4544a6909f5013c77061";
        public const string WeaponPincer = "77de31f631524eb8834496343d910d8f";
        public const string WeaponNeuroStinger = "3af57349b7a8487ab5f611838ad08554";
        // 개편 O8(계획 2026-11-16 Task O8): 분노 중 신속 시전 권리 버프 6(상한 1~6 — QuickenSpec.Rights 순서 = ChaosRules.QuickenCaps 의 서로 다른 상한 오름차순).
        // 발급 2026-11-17: python -I -c "import uuid;print(uuid.uuid4().hex)" 하나씩, 발급 직후 git grep 0건 확인.
        public const string QuickenRight1 = "71ef90ff9fc048bfbdf996344ce3d9e8";
        public const string QuickenRight2 = "954147c2231f41c3b4c3db275e3b4f35";
        public const string QuickenRight3 = "e5e0b586a9fe4478ba6bd07eda892f81";
        public const string QuickenRight4 = "d14d2bc96a974aafbdaee263fee98654";
        public const string QuickenRight5 = "d9e43a08a4924dac8e7b72402199b0b9";
        public const string QuickenRight6 = "c4828bb87c1f4887928a7736d695269c";

        // 본편 블루프린트(시험 `jbp: game guids` 가 blueprints.zip 의 AssetId·파일 이름과 대조한다).
        public static class Game
        {
            // Classes/Warpriest/WarpriestClass.jbp
            public const string WarpriestClass = "30b5e47d47a0e37438cc5a80c96cfb99";
            // Classes/Warpriest/WarpriestProgression.jbp
            public const string WarpriestProgression = "062b215705430ad47b7174c1e75d285e";
            // Classes/Shifter/Archetypes/Rageshaper/RageshaperShifterArchetype.jbp (성향 선례, 부록 A-e)
            public const string RageshaperShifterArchetype = "41d5b44e421048d09d32623689173e11";
            // Classes/Basic/DeitySelection.jbp · Classes/Basic/Deities/LamashtuFeature.jbp(CN·NE·CE) · Classes/Warpriest/Blessings/Chaos/ChaosBlessingFeature.jbp — 점검 유닛 선택값
            public const string DeitySelection = "59e7a76987fe3b547b9cce045f4db3e4";
            public const string LamashtuFeature = "f86bc8fbf13221f4f9041608a1fb8585";
            public const string ChaosBlessingFeature = "528e316f9f092954b9e38d3a82b1634a";
            // Classes/Warpriest/Blessings/BlessingSelection.jbp
            public const string BlessingSelection = "6d9dcc2a59210a14891aeedb09d406aa";
            // 2026-11-15 생성 불변식 ③(엔진 기본값 금지)으로 명시하는 선택(blueprints.zip, SelItems 덤프):
            //   Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp → BackgroundNone.jbp(목록 첫 항목, 효과 없음)
            //   Classes/Warpriest/WarpriestSpontaneousSelection.jbp → WarpriestSpointaneousNegative.jbp(목록 첫 항목 = 엔진 기본값과 같음, 라마슈투 악 성향과 맞음)
            //   Classes/Warpriest/WarpriestWeaponFocusSelection.jbp → Feats/WeaponFocus/WeaponFocus.jbp(유일 항목. 무기 종류 매개변수는 TestUnit.Create 쌍으로 못 줌 → 정책 허용 목록)
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c";
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6";
            public const string WarpriestSpontaneousSelection = "2d26a3364d65c4e4e9fb470172f638a9";
            public const string WarpriestSpontaneousNegative = "da7be78f753c11544a0264d72a51d831";
            public const string WarpriestWeaponFocusSelection = "ac384183dbfbbd7499410a21d749bef1";
            public const string WeaponFocus = "1e1f627d26ad36f43bbd26cc2bf8ac7e";
             
            public const string SecondBlessingSelection = "b7ce4a67287cda746a59b31c042305cf";
            public const string WarpriestFervorBase = "2d5140fd9f19a8e41be32d300eea2e18";
            public const string WarpriestFervorHealDamageSelection = "b0dc39bfbc6f85646adbfc6413f843b0";
            public const string WarpriestFervorQuickenCast = "866236a323bdf634abf0cc54dd30ad3b";
            public const string WarpriestChannelEnergySelection = "1a871294410971745bba12ef11e41f6e";
            public const string WarpriestFervorHealDamageRank = "bfcafb19f397da542be30d77126f1097";
            public const string SacredArmorFeature = "35e2d9525c240ce4c8ae47dd387b6e53";
            public const string SacredArmorEnchantPlus2 = "ec327c67f6a6b2f49a8ca218466a8818";
            public const string SacredArmorEnchantPlus3 = "bd292463fa74d664086f0a3e4e425c47";
            public const string SacredArmorEnchantPlus4 = "ee65ff63443ce42488515db6a43fee5e";
            public const string SacredArmorEnchantPlus5 = "1e560784dfcb00f4da1415bbad3226c3";
             
            public const string WarpriestFeatSelection = "303fd456ddb14437946e344bad9a893b";
            public const string SacredWeaponEnchantFeature = "6db4ccce643b7b2468717effb303afcf";
            public const string SacredWeaponEnchantPlus2 = "4ee3e74f79d5e4f47b7a0091984b2518";
            public const string SacredWeaponEnchantPlus3 = "bc1a85932b7d82f49852d6ded3049837";
            public const string SacredWeaponEnchantPlus4 = "b7295813f02c8be45a6e1fce541584df";
            public const string SacredWeaponEnchantPlus5 = "5fffe6a74adb1b742a8ebf64b8bb90fb";
            public const string WarpriestAspectOfWar = "65cc7abc21826a344aa156e2a40dcecc";
            public const string WarpriestFervorResource = "da0fb35828917f344b1cd72c98b70498";
            public const string BlessingResource = "d128a6332e4ea7c4a9862b9fdb358cca";
            // S1 spell-list-unchanged: 워프리스트 주문 목록 · Rage 주문 · Confusion 주문(목록에 없어야 한다)
            public const string WarpriestSpelllist = "c5a1b8df32914d74c9b44052ba3e686a";
            public const string RageSpell = "97b991256e43bb140b263c326f690ce2";
            public const string ConfusionSpell = "cf6c901fb7acc904e85c63b342e9c949";
            // T4 S2 cast-spell-while-raging: 워프리스트 1레벨 자기 대상 주문(WarpriestSpelllist 1레벨에 있음, zip) · 그 결과 버프
            public const string DivineFavor = "9d5d2d3ffdd73c648af3eb3e585b1113";
            public const string DivineFavorBuff = "5d2833d39901b844b828f9f13a0353fe";
            // T6 변이 효과: 추가 팔 무기 Weapons/Items/Standard/NaturalWeapon/Slam1d6.jbp(개편 O4 부터는 복제 원본 — 직접 쓰지 않는다) · 흔들림 원본 Buffs/Shaken.jbp(복제)
            public const string Slam1d6 = "767e6932882a99c4b8ca95c88d823137";
            public const string Shaken = "25ec6cb6ab1845c48a95f9c20b034220";
            // 개편 O4 자연 무기 복제 원본(계획 2026-11-16 원장 B — Weapons/Items/Standard/NaturalWeapon/, 새 상수 10 + 위 Slam1d6 = 서로 다른 원본 11; 12행 중 독침·신경독침이 Sting1d6 을 같이 쓴다)
            public const string Bite1d6 = "a000716f88c969c499a535dadcf09286";
            public const string Claw1d6 = "65eb73689b94d894080d33a768cdf645";
            public const string Gore1d6 = "daf4ab765feba8548b244e174e7af5be";
            public const string Tail1d6 = "b21cd5b03fbb0f542815580e66f85915";
            public const string Tentacle1d4 = "808f600afec12b2498f2e48ec2f935d9";
            public const string Sting1d6 = "af0be9fbbda53dc469de6817346f9755";
            public const string Wing1d6 = "cdbf5fdd86eb4d238cef15f7835e42c3";
            public const string Hoof1d6 = "92386f3c3cd6b6a4cbe16e25164cd764";
            public const string Talon1d4 = "2051b156321946245b94caf84f4cc686";
            public const string Pincers1d6 = "d8706fda7904db8448258800f0bdc006";
            // 개편 O4 본편 자연 무기 형식 11(Weapons/Types/NaturalWeapon/ — 복제본이 공유한다, 복제하지 않는다)
            public const string SlamType = "f18cbcb39a1b35643a8d129b1ec4e716";
            public const string BiteType = "952e30e6cb40b454789a9db6e5f6dd09";
            public const string ClawType = "d4f7aee36efe0b54e810c9d3407b6ab3";
            public const string GoreType = "352b738b4ebfd554b909852e7f3b690a";
            public const string TailType = "4ce435468ebd4364997da8cbd0c66133";
            public const string TentacleType = "a4f0141e6df8db04dadccd6b8c1474df";
            public const string StingType = "34c90ab7a0d6e05449f081c122ca219f";
            public const string WingType = "e03738c69ae8c914c827e72a3ca91f83";
            public const string HoofType = "ad298a0ee3ca1ba419d0c973d1a905f2";
            public const string TalonsType = "88291a2a52c7cb04a9a59f51df9880eb";
            public const string PincersType = "e302f0fcfeae0aa4f94e65b591b7a893";
            // 개편 O5 엔진의 추가 사지 범주 상한 표: Root/SystemMechanics/BlueprintPersonageLimits.jbp(BlueprintRoot.m_PersonageLimits 가 가리킨다 — 엔진 GetWeaponSlotCount 가 읽는 그 표)
            public const string PersonageLimits = "2a609e8ce9844f98ae11097bef7be8bb";
            // T7 고통: 혼란 상태 버프 Buffs/Confusion.jbp · 정신 면역 언데드 CreatureAbilities/Immunities/UndeadImmunities.jbp(점검 S8)
            public const string Confusion = "886c7407dc629dc499b9f1465ff382df";
            public const string UndeadImmunities = "8a75eb16bfff86949a4ddcb3dd2f83ae";
            // 점검 S6 원거리 명중: Weapons/Items/Standard/MartialRanged/StandardLongbow.jbp
            public const string StandardLongbow = "201f6150321e09048bd59e9b7f558cb0";
            // 2026-11-17 3차 점검 「no weapon for the forced hit」: 시험 유닛 손이 비어 GetFirstWeapon == null(맨손은 건너뜀, IL_0000~00B2) → 근접 강제 명중 공격자에게 명시 장착하는 무기.
            //   Weapons/Items/Standard/MartialOneHanded/StandardLongsword.jbp (워프리스트 = 군용 무기 숙련)
            public const string StandardLongsword = "6fd0a849531617844b195f452661b2cd";
            // T8 점검 S10: 망토(분노 인정 단일 기준 소비자) · 그 추가 효과 버프(Equipment/Cloaks/PF2/Chapter2/…)
            public const string CloakOfBloodScentFeature = "9db109eee03524f4aa89881c4539a14d";
            public const string CloakOfBloodScentBuff = "4e850d3b0c68a6045abd10e386d07af5";
            // 독립 검토 I1 점검 S10: 억제 버프(Classes/Bard/Archetypes/TranquilWhisperer/InspireTranquility/InspireTranquilityEffectBuff.jbp)
            public const string InspireTranquilityEffectBuff = "d7204fb8ac8a2c943bc81ec70eff6356";
            // 개편 O8 신속 시전: 권리 버프의 AutoMetamagic.m_Spellbook = 워프리스트 주문서(Classes/Warpriest/WarpriestSpellbook.jbp — CheckSpellbook 이 그 SpellList 소속을 본다)
            //   · 주문 칸 표(Classes/Warpriest/WarpriestSpellSlotsTable.jbp — 시험이 「한 단계 늦게」를 대조) · 본편 선례 Classes/Bloodrager/BloodragerRage/BloodragerGreaterBloodrageBuff.jbp(시험이 부품 모양을 대조)
            public const string WarpriestSpellbook = "7d7d51be2948d2544b3c2e1596fd7603";
            public const string WarpriestSpellSlotsTable = "c73a394ec54adc243aef8ac967e39324";
            public const string BloodragerGreaterBloodrageBuff = "711192a30f8a4b2aa3d3f644511a0e99";
            // 개편 O8 점검 S13(K11 — WarpriestSpelllist 직독 2026-11-17, 자기에게 거는 표준 행동 주문): 2레벨 Spells/Level2/EffortlessArmor.jbp · 3레벨 Spells/Level3/ArchonsAura.jbp
            public const string EffortlessArmor = "e1291272c8f48c14ab212a599ad17aac";
            public const string ArchonsAura = "e67efd8c84f69d24ab472c9f546fff7e";
            // 독립 검토 R4-F3 점검 S13(WarpriestSpelllist 직독 2026-11-17 — 변형 부모는 목록에 있고 자식은 없다, 자식의 m_Parent = 부모, 자기 대상 가능 · 표준 행동):
            //   3레벨 부모 Spells/Level3/ProtectionFromEnergy.jbp → 자식 Spells/Level3/ProtectionFromFire.jbp · 2레벨 부모 Spells/Level2/ResistEnergy.jbp → 자식 Spells/Level2/ResistFire.jbp
            public const string ProtectionFromEnergy = "d2f116cfe05fcdd4a94e80143b67046f";
            public const string ProtectionFromFire = "3f9605134d34e1243b096e1f6cb4c148";
            public const string ResistEnergy = "21ffef7791ce73f468b6fca4d9371e8b";
            public const string ResistFire = "ddfb4ac970225f34dbff98a10a4a8844";
        }
    }
}
