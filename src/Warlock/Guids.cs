namespace Warlock
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다, modinfo testSources). 발급: python -c "import uuid;print(uuid.uuid4().hex)"
    public static class Guids
    {
         
        public const string Archetype = "d74096f01a884dd5b33aff12cb86b36f"; // 워락 아키타입(WitchClass)
        public const string PactFeature = "3f8c0ddf408542bdaa27d572c1cac5e6"; // L1 계약 특성 — WarlockPactComponent 소유(그룹·키트 조정, 과열 계수)
        public const string HeatBuff = "d989209b025f470fbbf6a8804689c4ae"; // 과열 랭크 버프(Ranks 9, StackingType.Rank)
        public const string CapstoneSummonBuff = "d3edadc4caf44e008bb1d77bcff6f4e8"; // L20 대사자 표식 버프(9단계 소환물 전투 종료까지)
        public const string SummonPool = "571540b2fd264452970cdbbbe26dd079"; // 워락 소환 풀(그룹 공용)
        public const string GroupHell = "bb924fff061945caad66330583694966"; // 그룹 특성 Hell
        public const string GroupAbyss = "a1b9215b25a840e7a7db77974a37780b"; // 그룹 특성 Abyss
        public const string GroupFey = "5d4e64cf27254b6a935d97c37c8a72cf"; // 그룹 특성 Fey
        public const string GroupStorm = "67b4f7c277234257a561fe81f73e7fa5"; // 그룹 특성 Storm
        public const string GroupDeath = "6b982d5620054ed48482b9f96701a37c"; // 그룹 특성 Death
        public const string GroupStar = "b470fd1649f1481c8d51c17a522c0e06"; // 그룹 특성 Star
        public const string SummonParentHell = "35343c70fa23467cae86d522bd6f2150"; // 과열 소환 부모 능력 Hell(AbilityVariants; 변형 9 = DerivedGuids summon)
        public const string SummonParentAbyss = "5d0ab9f56066410e8bbb4a173bc843fd"; // 과열 소환 부모 능력 Abyss(AbilityVariants; 변형 9 = DerivedGuids summon)
        public const string SummonParentFey = "cc17988dc3ba4e8788b3be581dbd0e77"; // 과열 소환 부모 능력 Fey(AbilityVariants; 변형 9 = DerivedGuids summon)
        public const string SummonParentStorm = "0857a8de45764333a09623b321670033"; // 과열 소환 부모 능력 Storm(AbilityVariants; 변형 9 = DerivedGuids summon)
        public const string SummonParentDeath = "8d8798d29190426692f3b5fc44f540a4"; // 과열 소환 부모 능력 Death(AbilityVariants; 변형 9 = DerivedGuids summon)
        public const string SummonParentStar = "c1aae3f055be499eb691095ff1b1c68d"; // 과열 소환 부모 능력 Star(AbilityVariants; 변형 9 = DerivedGuids summon)
        public const string Ward1Feature = "974b764d13914171957736b7aaed5a4a"; // L2 그룹 가호 1단 마커
        public const string Augment4Feature = "fa94c0d17db3481582f1ffcecea245f5"; // L4 소환 강화 마커
        public const string Reach6Feature = "d0c5b6a805f849c6b87ac882814282c1"; // L6 사거리 Medium 마커
        public const string Ward2Feature = "f555834c60d24b85a8229f9935c11cd3"; // L8 그룹 가호 2단 마커
        public const string Double10Feature = "c19f1fc0ac9f4e3cb4733d11e4037ec3"; // L10 연속 사격 마커
        public const string HeatAccel12Feature = "4059cd2a7ae949e09f7da3eb51ae18aa"; // L12 과열 가속 마커
        public const string Ward3Feature = "74b723a15d3f4cd3b797273c92c6e3d7"; // L14 그룹 가호 3단 마커
        public const string Extend16Feature = "e88b32e1c73d46ed9e3f17da3836b164"; // L16 소환 지속 2배 마커
        public const string Ward4Feature = "75e1660412e445b696b6db16197a8c0e"; // L18 그룹 가호 4단(면역 / 별 SR) 마커
        public const string Capstone20Feature = "a5280449bcdb47f1a9b95bdd557dd114"; // L20 대사자 마커
         
        public const string IxAugmentBuff = "98b9bfb4f18e4f90a1a5af2ccc981cfc"; // 항목 9: 지옥·폭풍·죽음 IX 소환물의 두 번째 소환 강화(힘·건강 +4, 쌓이는 무형 보너스)
        public const string ServantBuff = "a09ab15a39194c0bbebdbd3076f7493e"; // 항목 4b: 워락 소환물 54칸 전부 — 직접 조종 불가(PreventDirectControl), 자기 AI 로 싸운다

        // 범주별 파생 GUID 의 이름공간(DerivedGuids.For). 커밋 뒤 변경 금지 — 바꾸면 파생 96 GUID 가 전부 바뀐다(세이브 호환 깨짐).
        // 중첩 형식에 두는 이유: Guids 자신의 문자열 상수는 전부 32자 hex 새 GUID 로 시험이 센다(tests\WarlockTests.cs ExpectedNewGuidNames).
        public static class Derive
        {
            public const string Namespace = "Warlock/2026-11-09";
        }

         
        // 줄마다 jbp 경로 = blueprints.zip 2026-11-09 직독(md5 5da11edf2659). tests\WarlockTests.cs GamePaths 가 같은 경로의 jbp AssetId 와 대조한다(이름 집합도 같아야 한다).
        // 공용 GameGuids 와 같은 값을 두지 않는다(시험 「Guids: all distinct」). 상수 이름 = jbp 이름(예외: SummonedFaction = Summoned, HexChannelerArchetype = HexChannelerWitchArchetype,
        // SummonedCreatureSpawnMonster*_* = jbp 이름의 하이픈을 밑줄로).
        public static class Game
        {
            // ---- 기반·제거·교체·유지 (설계 §8 = §4.1·§4.2·§4.10) ----
            public const string WitchClass = "1b9873f1e7bfe5449bc84d03e9c8e3cc"; // Classes/Witch/WitchClass.jbp — 기반 직업 · 등록 예외 ① · 한국어 판별
            public const string WitchProgression = "c5e9e2b086a9814409fb6a29a5acdd0f"; // Classes/Witch/WitchProgression.jbp — 기반 진행표(제거 13쌍)
            public const string WitchHexSelection = "9846043cf51251a4897728ed6e24e76f"; // Classes/Witch/WitchHexes/WitchHexSelection.jbp — 제거 L1·2·4·…·20 (11)
            public const string WitchBetterHexProgression = "38d01811fcb32444a8fe372c029fa0c6"; // Classes/Witch/WitchBetterHexProgression.jbp — 제거 L1
            public const string WitchCantripsFeature = "c213af60aba83ed4993948dce6b947b8"; // Classes/Witch/WitchCantripsFeature.jbp — 제거 L1(교체)
            public const string AccursedWitchCantripsFeature = "d83becdd1ab644b995ae4aa96a493351"; // Classes/Witch/Archetypes/AccursedWitch/AccursedWitchCantripsFeature.jbp — 교체 L1(매력 캔트립)
            public const string WitchPatronSelection = "381cf4c890815d049a4420c6f31d063f"; // Classes/Witch/WitchPatrons/WitchPatronSelection.jbp — 유지 L1(후견인 13)
            public const string WitchFamiliarSelection = "29a333b7ccad3214ea3a51943fa0d8e9"; // Classes/Witch/WitchFamiliarSelection.jbp — 유지 L1
            // 2026-11-15 생성 불변식 ③: 점검 유닛(WarlockTestUnit.Create)의 L1 선택 명시 — 각 선택 m_AllFeatures 첫 항목(엔진 기본값이 집던 것).
            public const string CatFamiliarBondFeature = "1cb0b559ca2e31e4d9dc65de012fa82f"; // Classes/Wizard/ArcaneBond/FamiliarBond/CatFamiliarBondFeature.jbp — WitchFamiliarSelection 첫 항목(사역마는 꺼진 토글 CatFamiliarAbility → AddFamiliar 버프)
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c"; // Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6"; // Backgrounds/BackgroundsSelection/BackgroundNone.jbp — 효과 없음
            public const string WitchSpellbook = "dd04f9239f655ea438976742728e4909"; // Classes/Witch/WitchSpellbook.jbp — 본편 지능 주문서(프리스티지 항목의 주문서)
            public const string WitchSpellList = "e17df9977b879b64e8a8cbb4b3569f19"; // Classes/Witch/WitchSpellList.jbp — 본편 주문 목록
            public const string AccursedWitchArchetype = "c5f6e53e71059fb4d802ce81a277a12d"; // Classes/Witch/Archetypes/AccursedWitch/AccursedWitchArchetype.jbp — 겹침 금지(스티그마)
            public const string AccursedWitchSpellbook = "b897fe0947e4b804082b1a687c21e6e2"; // Classes/Witch/Archetypes/AccursedWitch/AccursedWitchSpellbook.jbp — 재사용 주문서
            public const string SorcererSpellsDailyTable = "dbff16956df2eda48a1da5c9617cc836"; // Classes/Sorcerer/SorcererSpellsDailyTable.jbp — 주문서 표(읽기만)
            public const string SorcererSpellsKnownTable = "d232bc78d967a964bac4d4d38e7ca5f4"; // Classes/Sorcerer/SorcererSpellsKnownTable.jbp — 주문서 표(읽기만)
            public const string OracleCurseSelection = "b0a5118b4fb793241bc7042464b23fab"; // Classes/Oracle/OracleCurses/OracleCurseSelection.jbp — 스티그마 전용 — 안 씀
            public const string RayCalculateFeature = "d3e6275cfa6e7a04b9213b7b292a011c"; // Weapons/Types/Special/RayCalculateFeature.jbp — 유지 L1(Step 0 정찰 §16-T2)
            public const string SimpleWeaponProficiency = "e70ecf1ed95ca2f40b754f1adb22bbdd"; // Classes/Basic/Proficiencies/SimpleWeaponProficiency.jbp — 유지 L1(Step 0 정찰 §16-T2)

            // ---- 후견인 진행표 13 + 어둠의 계약 (설계 §4.3·§8; WitchPatronSelection.m_AllFeatures 13 = jbp 2026-11-09) ----
            public const string WitchAgilityPatronProgression = "08518b2a62446c74b9ae08ee73664047"; // Classes/Witch/WitchPatrons/PatronAgility/WitchAgilityPatronProgression.jbp — 후견인
            public const string WitchAncestorsPatronProgression = "b9c4e782706099f42a2ebc901acf492d"; // Classes/Witch/WitchPatrons/PatronAncestors/WitchAncestorsPatronProgression.jbp — 후견인
            public const string WitchDeceptionPatronProgression = "3a4214e3c2eab3c40bc491d7abea7045"; // Classes/Witch/WitchPatrons/PatronDeception/WitchDeceptionPatronProgression.jbp — 후견인
            public const string WitchDevotionPatronProgression = "facb0ed7d8e52b04cacf351bea430ce9"; // Classes/Witch/WitchPatrons/PatronDevotion/WitchDevotionPatronProgression.jbp — 후견인
            public const string WitchElementsPatronProgression = "67e85f52b1f020847aaa738d8999d4cd"; // Classes/Witch/WitchPatrons/PatronElements/WitchElementsPatronProgression.jbp — 후견인
            public const string WitchEndurancePatronProgression = "8fe0a14c90d3ea94a833d087b8a09bb9"; // Classes/Witch/WitchPatrons/PatronEndurance/WitchEndurancePatronProgression.jbp — 후견인
            public const string WitchHealingPatronProgression = "a3e4ef40ad99f4d47af15cc5f16afc97"; // Classes/Witch/WitchPatrons/PatronHealing/WitchHealingPatronProgression.jbp — 후견인
            public const string WitchInsanityPatronProgression = "eafc47304da734a4d922ae663d82f1e5"; // Classes/Witch/WitchPatrons/PatronInsanity/WitchInsanityPatronProgression.jbp — 후견인
            public const string WitchMercyPatronProgression = "cad7c2fdabeb9574f95f4b9ffee20afe"; // Classes/Witch/WitchPatrons/PatronMercy/WitchMercyPatronProgression.jbp — 후견인
            public const string WitchShadowPatronProgression = "f48bfffe3618c274dbd42dfff8d0df56"; // Classes/Witch/WitchPatrons/PatronShadow/WitchShadowPatronProgression.jbp — 후견인
            public const string WitchStrengthPatronProgression = "850ac8a2bc65d814db9f3fea871c18bb"; // Classes/Witch/WitchPatrons/PatronStrength/WitchStrengthPatronProgression.jbp — 후견인
            public const string WitchTransformationhPatronProgression = "23ea5ade326f80b488164f75580c03af"; // Classes/Witch/WitchPatrons/PatronTransformation/WitchTransformationhPatronProgression.jbp — 후견인
            public const string WitchWinterPatronProgression = "e98d8d9f907c1814aa7376d6cdaac012"; // Classes/Witch/WitchPatrons/PatronWinter/WitchWinterPatronProgression.jbp — 후견인
            public const string WitchDarkPactPatronProgression = "f4f3d8395db347938237c1bc77820781"; // Classes/Witch/Archetypes/DarkSisterWitch/WitchDarkPactPatronProgression.jbp — 어둠의 계약 — 매핑 안 함(설계 §4.3 도달 불가 행)

            // ---- 블라스트 원형 (설계 §4.4·§4.5·§8) ----
            public const string RayItem = "f6ef95b1f7bb52b408a5b345a330ffe8"; // Weapons/Types/Special/RayItem.jbp — 광선 무기
            public const string FireCommonProjectile00 = "30a5f408ea9d163418c86a7107fc4326"; // FX/Spell Level None/FireCommonProjectile00.jbp — 지옥 투사체
            public const string AcidCommonProjectile00 = "d8abd128c02331a45a4f250a62722e8b"; // FX/Spell Level None/AcidCommonProjectile00.jbp — 심연 투사체
            public const string ColdCommonProjectile00 = "e82d266e0c068ab418a163fc41c40731"; // FX/Spell Level None/ColdCommonProjectile00.jbp — 요정 투사체
            public const string ElectroCommonProjectile00 = "1af8385214ca8774b98922b56caa0e92"; // FX/Spell Level None/ElectroCommonProjectile00.jbp — 폭풍 투사체
            public const string NegativeCommonProjectile00 = "3f068599c7549e84497154248278e5b0"; // FX/Spell Level None/NegativeCommonProjectile00.jbp — 죽음 투사체
            public const string MagicMissile00 = "2e3992d1695960347a7f9bdf8122966f"; // FX/Spell Level 1/MagicMissile00.jbp — 별 투사체(역장)
            public const string ScorchingRay = "cdb106d53c65bbc4086183d54c3b97c7"; // Spells/Level2/ScorchingRay.jbp — 연속 사격 원형(DelayBetweenProjectiles 0.2)
            public const string RayOfFrost = "9af2ab69df6538f4793b2f9c3cc85603"; // Spells/Level0/RayOfFrost.jbp — 블라스트 원형
            public const string Jolt = "16e23c7a8ae53cc42a93066d19766404"; // Spells/Level0/Jolt.jbp — 블라스트 원형 · 점검 S6 jolt-hit-no-heat
            public const string AcidSplash = "0c852a2405dd9f14a8bbcfaf245ff823"; // Spells/Level0/AcidSplash.jbp — 블라스트 원형

            // ---- 소환 (설계 §4.6·§4.9·§8, 정찰 §3-3·§10, Step 0 정찰 §16-T1) ----
            public const string SummonMonsterPool = "d94c93e7240f10e41ae41db4c83d1cbe"; // Spells/CutsceneSpells/SummonMonsterPool.jbp — 본편 풀(안 씀, 참고)
            public const string SummonMonsterFiendishBuffI = "c707fdbe58d0c614a89a872b76777f4b"; // Spells/Level1/SummonMonsterFiendishBuffI.jbp — 악 템플릿 I
            public const string SummonMonsterCelestialBuffI = "83a8a909e7ad19b4fa57e306884cc8bd"; // Spells/Level1/SummonMonsterCelestialBuffI.jbp — 선 템플릿 I
            public const string SummonMonsterFiendishBuffII = "4b808ccd7e0b7bd43b10b2d47733b1a4"; // Spells/Level4/SummonMonsterFiendishBuffII.jbp — 악 템플릿 II
            public const string SummonMonsterCelestialBuffII = "ec906031bb8766f42a37a52cf1d89836"; // Spells/Level4/SummonMonsterCelestialBuffII.jbp — 선 템플릿 II
            public const string AugmentSummoning = "38155ca9e4055bb48a89240a2055dcc3"; // Feats/AugmentSummoning/AugmentSummoning.jbp — L4 소환 강화
            public const string AugmentSummoningBuff = "169d03bbccdbdc542ae1a57d83673d80"; // Feats/AugmentSummoning/AugmentSummoningBuff.jbp — L4 소환 강화 버프
            public const string SummonedFaction = "1b08d9ed04518ec46a9b3e4e23cb5105"; // Factions/Summoned.jbp — 소환 54칸 유닛 진영(42 유닛 전부 m_Faction = 이것, Step 0)
            public const string SummonMonsterISingle = "8fd74eddd9b6c224693d9ab241f25e84"; // Spells/Level1/SummonMonsterISingle.jbp — 본편 단계 기준 1
            public const string SummonMonsterIISingle = "7ab27a0d547742741beb5d089f1c3852"; // Spells/Level2/SummonMonsterIISingle.jbp — 본편 단계 기준 2
            public const string SummonMonsterIIISingle = "15b5efe371d47c944b58444e7b734ffb"; // Spells/Level3/SummonMonsterIIISingle.jbp — 본편 단계 기준 3
            public const string SummonMonsterIVSingle = "efa433a38e9c7c14bb4e780f8a3fe559"; // Spells/Level4/SummonMonsterIVSingle.jbp — 본편 단계 기준 4
            public const string SummonMonsterVSingle = "0964bf88b582bed41b74e79596c4f6d9"; // Spells/Level5/SummonMonsterVSingle.jbp — 본편 단계 기준 5
            public const string SummonMonsterVISingle = "02de4dd8add69aa42a3d1330b573e2ab"; // Spells/Level6/SummonMonsterVISingle.jbp — 본편 단계 기준 6
            public const string SummonMonsterVIISingle = "2920d48574933c24391fbb9e18f87bf5"; // Spells/Level7/SummonMonsterVIISingle.jbp — 본편 단계 기준 7
            public const string SummonMonsterVIIISingle = "eb6df7ddfc0669d4fb3fc9af4bd34bca"; // Spells/Level8/SummonMonsterVIIISingle.jbp — 본편 단계 기준 8
            public const string SummonMonsterIXSingle = "e96593e67d206ab49ad1b567327d1e75"; // Spells/Level9/SummonMonsterIXSingle.jbp — 본편 단계 기준 9
            public const string SummonedCreatureSpawnMonsterI_III = "6fcdf014694b2b542a867763b4369cb3"; // Buffs/SummonedCreatureSpawnMonsterI-III.jbp — SM I~III AfterSpawn 연출 버프
            public const string SummonedCreatureSpawnMonsterIV_VI = "50d51854cf6a3434d96a87d050e1d09a"; // Buffs/SummonedCreatureSpawnMonsterIV-VI.jbp — SM IV~VI AfterSpawn 연출 버프
            public const string SummonedCreatureSpawnMonsterVI_IX = "0dff842f06edace43baf8a2f44207045"; // Buffs/SummonedCreatureSpawnMonsterVI-IX.jbp — SM VII~IX AfterSpawn 연출 버프(jbp 이름은 VI-IX)
            public const string SummonedUnitBuff = "8728e884eeaa8b047be04197ecf1a0e4"; // Root/SystemMechanics/Conditions/SummonedUnitBuff.jbp — 소환물 수명 버프(PackWarden Guids.cs:64 와 같은 값)
            public const string DismemberedUnitBuff = "c98d765d063f57a49a03f13d4f697c33"; // Root/SystemMechanics/Conditions/DismemberedUnitBuff.jbp — 소멸 중 버프(컴포넌트 DropLootAndDestroyOnDeactivate — SummonRule.Phase Dismissing)
            public const string DogSummoned = "77f3f2ddf1ec2da45ab956c433e3b557"; // Units/Monsters/Dog/DogSummoned.jbp — 소환 유닛(SummonTable)
            public const string WolfSummon = "76597216769b0d540aafafa07edf0cec"; // Units/Monsters/Wolf/WolfSummon.jbp — 소환 유닛(SummonTable)
            public const string MonitorLizardSummoned = "4109b40f6bbb49640840644cc84ada67"; // Units/Monsters/Monitor Lizard/MonitorLizardSummoned.jbp — 소환 유닛(SummonTable)
            public const string DireWolfSummon = "03dd28e92faf2e44eb9564a6ba01fdd0"; // Units/Monsters/Wolf/DireWolfSummon.jbp — 소환 유닛(SummonTable)
            public const string HellhoundSummoned = "ece348345859351439e1263115f5fdb9"; // Units/Monsters/Hellhound/HellhoundSummoned.jbp — 소환 유닛(SummonTable)
            public const string ManticoreSummoned = "7b7701ffc8f335a47a9ed97516531b71"; // Units/Monsters/Manticore/ManticoreSummoned.jbp — 소환 유닛(SummonTable)
            public const string SummonedAbrikandiluFrozen = "3bbb848c7bb749f9a8366d2bb4392293"; // Units/NPC/Commoners/DLC5_Storys_End/SummonedAbrikandiluFrozen.jbp — 소환 유닛(SummonTable)
            public const string BabauSummon = "352a52290c96f0e4eb40107fa43ecba3"; // Units/Monsters/Demon Babau/BabauSummon.jbp — 소환 유닛(SummonTable)
            public const string VrockSummon = "49ba14970b872ea4e9fac40c25912ed9"; // Units/Monsters/Demon Vrock/VrockSummon.jbp — 소환 유닛(SummonTable)
            public const string GibrilethSummon = "cffe1c1aff5ca5c468b78ede814d6d78"; // Units/Monsters/Demon Gibrileth/GibrilethSummon.jbp — 소환 유닛(SummonTable)
            public const string CR12_NabasuAdvancedSummoned = "a1351a09365f46c4afb71e710eec328b"; // Units/Monsters/Demon Nabasu/CR12_NabasuAdvancedSummoned.jbp — 소환 유닛(SummonTable)
            public const string OolioddrooSummon = "ad4fb978719b2eb4d861c23cab5dc0ef"; // Units/Monsters/Demon Oolioddroo/OolioddrooSummon.jbp — 소환 유닛(SummonTable)
            public const string MiteSummoned = "0c433dcdefcaaeb4db78b07c3ebf4c94"; // Units/Monsters/Mite/MiteSummoned.jbp — 소환 유닛(SummonTable)
            public const string RedcapSummoned = "af8bcff074b93c84b97938e486184222"; // Units/Monsters/Redcap/RedcapSummoned.jbp — 소환 유닛(SummonTable)
            public const string WillOWispSummon = "421c46ca1d602494694f317f865126ff"; // Units/Monsters/Will-o-Wisp/WillOWispSummon.jbp — 소환 유닛(SummonTable)
            public const string BogeymanSummoned = "4f04734e4ad930e438eec3182dfd95fe"; // Units/Monsters/Bogeyman/BogeymanSummoned.jbp — 소환 유닛(SummonTable)
            public const string NereidSummoned = "1618961b217a446459c6a91481065d2c"; // Units/Monsters/Nereid/NereidSummoned.jbp — 소환 유닛(SummonTable)
            public const string WildHunt_ArcherSummon = "5333ac43535e27b4bb4cd13ebff95faf"; // Units/Monsters/WildHunt/WildHunt_ArcherSummon.jbp — 소환 유닛(SummonTable)
            public const string SummonedAirElementalSmall = "04944455200bc224d955a8e9bbd64f3f"; // Units/Monsters/AirElemental/SummonedAirElementalSmall.jbp — 소환 유닛(SummonTable)
            public const string SummonedAirElementalMedium = "676f8b7d0a170674cb6e504e0e30b4f0"; // Units/Monsters/AirElemental/SummonedAirElementalMedium.jbp — 소환 유닛(SummonTable)
            public const string SummonedAirElementalLarge = "3764b43791a00e1468257adbca43ce9b"; // Units/Monsters/AirElemental/SummonedAirElementalLarge.jbp — 소환 유닛(SummonTable)
            public const string SummonedAirElementalHuge = "2e24256e459468743b91fbb9aa85e1ab"; // Units/Monsters/AirElemental/SummonedAirElementalHuge.jbp — 소환 유닛(SummonTable)
            public const string SummonedAirElementalGreater = "e770cfbb96b528c4db258d7d03fe6533"; // Units/Monsters/AirElemental/SummonedAirElementalGreater.jbp — 소환 유닛(SummonTable)
            public const string SummonedAirElementalElder = "33bb90ffd13c87b4c8e45d920313752a"; // Units/Monsters/AirElemental/SummonedAirElementalElder.jbp — 소환 유닛(SummonTable)
            public const string SummonedWaterElementalSmall = "56372b0a2749c224392a5ee74105c534"; // Units/Monsters/Water Elemental/SummonedWaterElementalSmall.jbp — 소환 유닛(SummonTable)
            public const string SummonedWaterElementalMedium = "62a3e860e6e72e6499c38bb8b2fe303e"; // Units/Monsters/Water Elemental/SummonedWaterElementalMedium.jbp — 소환 유닛(SummonTable)
            public const string SummonedWaterElementalLarge = "680b5b61c80af664daec46af7644486c"; // Units/Monsters/Water Elemental/SummonedWaterElementalLarge.jbp — 소환 유닛(SummonTable)
            public const string SummonedWaterElementalElder = "3bd31a0b4d800f04a8c5b7b1a6d7061e"; // Units/Monsters/Water Elemental/SummonedWaterElementalElder.jbp — 소환 유닛(SummonTable)
            public const string SummonedFireElementalHuge = "640fb7efb7c916945837bbcab995267e"; // Units/Monsters/Fire Elemental/SummonedFireElementalHuge.jbp — 소환 유닛(SummonTable)
            public const string SummonedFireElementalGreater = "b0b4091bdaebb464e903857a95189dea"; // Units/Monsters/Fire Elemental/SummonedFireElementalGreater.jbp — 소환 유닛(SummonTable)
            public const string SummonedFireElementalElder = "ea0f0bbc6e5e471428d535501b21eb26"; // Units/Monsters/Fire Elemental/SummonedFireElementalElder.jbp — 소환 유닛(SummonTable)
            public const string SummonedSkeletonChampion = "53e228ba7fe18104c93dc4b7294a1b30"; // Units/NPC/Commoners/Skeleton/SummonedSkeletonChampion.jbp — 소환 유닛(SummonTable)
            public const string SummonedSkeletonAbomination = "be744eb2d1394f4f87e69491cc7952b4"; // Units/NPC/Commoners/Skeleton/SummonedSkeletonAbomination.jbp — 소환 유닛(SummonTable)
            public const string SpectreSummoned = "e0f28fa1fce0a734e82b8a1da6b8dcce"; // Units/NPC/Commoners/Spectre/SpectreSummoned.jbp — 소환 유닛(SummonTable)
            public const string FallenSummon = "87756af93dc37b6458f17b58021757e0"; // Units/NPC/Commoners/Fallen/FallenSummon.jbp — 소환 유닛(SummonTable)
            public const string CR9_ShadowGreaterAdvancedSummoned = "6ea333f52fd2421a9892ef9be6fbb03b"; // Units/NPC/Commoners/Shadow/CR9_ShadowGreaterAdvancedSummoned.jbp — 소환 유닛(SummonTable)
            public const string CR11_GraveknightSummoned = "7237a32613fe55e479d1141682f2bbd4"; // Units/NPC/Commoners/Skeleton/CR11_GraveknightSummoned.jbp — 소환 유닛(SummonTable)
            public const string AzataBralaniSummoned = "58574e8d1d4dc464c976f396d9115b1a"; // Units/NPC/Commoners/Azata Bralani/AzataBralaniSummoned.jbp — 소환 유닛(SummonTable)
            public const string CR7_AzataLillendSummoned = "4f7d1d6ce22774449be3ca56ef5db135"; // Units/Monsters/AzataLillend/CR7_AzataLillendSummoned.jbp — 소환 유닛(SummonTable)
            public const string MovanicDevaSummoned = "afe56099ff6046b40a359b7562c0424e"; // Units/NPC/Commoners/Movanic Deva/MovanicDevaSummoned.jbp — 소환 유닛(SummonTable)
            public const string MonadicDevaSummoned = "0df5585bc5189d8498280c3e67fafc59"; // Units/NPC/Commoners/Monadic Deva/MonadicDevaSummoned.jbp — 소환 유닛(SummonTable)
            public const string AzataGhaelSummoned = "bc8ca1437c0f48948b317b7e64febf0d"; // Units/NPC/Commoners/Azata Ghaele/AzataGhaelSummoned.jbp — 소환 유닛(SummonTable)

            // ---- 프리스티지 「위치 주문서」 항목 6 · 담는 선택지 6 (설계 §4.11·§8, 정찰 §6) ----
            public const string ArcaneTricksterWitch = "693916d54890b7e448b5acd7d08a21f9"; // Classes/ArcaneTrickster/ArcaneTricksterSpellbooks/ArcaneTricksterWitch.jbp — 등록 예외 ② 대상 항목
            public const string EldritchKnightWitch = "0d8491fcf41718740b5e62561bfb369a"; // Classes/EldritchKnight/EldritchKnightSpellbooks/EldritchKnightWitch.jbp — 등록 예외 ② 대상 항목
            public const string HellknightSigniferWitch = "e22ec5a115850d141b6e21680ed6a5c0"; // Classes/HellknightSignifer/HellknightSigniferArcaneSpellbooks/HellknightSigniferWitch.jbp — 등록 예외 ② 대상 항목
            public const string LoremasterWitch = "e741eba3664dd834c803bb7c39193b21"; // Classes/Loremaster/LoremasterArcaneSpellbooks/LoremasterWitch.jbp — 등록 예외 ② 대상 항목
            public const string MysticTheurgeWitch = "c62262b52a20c1042b55abf3d744c80e"; // Classes/MysticTheurge/MysticTheurgeArcaneSpellbooks/MysticTheurgeWitch.jbp — 등록 예외 ② 대상 항목
            public const string WinterWitchWitch = "3473fa3a4f9e8da44a75e884c03b1cbb"; // Classes/WinterWitch/WinterWitchSpellbooks/WinterWitchWitch.jbp — 등록 예외 ② 대상 항목
            public const string ArcaneTricksterSpellbookSelection = "ae04b7cdeb88b024b9fd3882cc7d3d76"; // Classes/ArcaneTrickster/ArcaneTricksterSpellbooks/ArcaneTricksterSpellbookSelection.jbp — 항목을 담는 선택지
            public const string EldritchKnightSpellbookSelection = "dc3ab8d0484467a4787979d93114ebc3"; // Classes/EldritchKnight/EldritchKnightSpellbooks/EldritchKnightSpellbookSelection.jbp — 항목을 담는 선택지
            public const string HellknightSigniferSpellbook = "68782aa7a302b6d43a42a71c6e9b5277"; // Classes/HellknightSignifer/HellknightSigniferSpellbook.jbp — 항목을 담는 선택지
            public const string LoremasterSpellbookSelection = "7a28ab4dfc010834eabc770152997e87"; // Classes/Loremaster/LoremasterSpellbookSelection.jbp — 항목을 담는 선택지
            public const string MysticTheurgeArcaneSpellbookSelection = "97f510c6483523c49bc779e93e4c4568"; // Classes/MysticTheurge/MysticTheurgeArcaneSpellbooks/MysticTheurgeArcaneSpellbookSelection.jbp — 항목을 담는 선택지
            public const string WinterWitchSpellbookSelection = "ea20b26d9d0ede540af3c74246dade41"; // Classes/WinterWitch/WinterWitchSpellbooks/WinterWitchSpellbookSelection.jbp — 항목을 담는 선택지

            // ---- 겹침·도달 불가 근거 (설계 §4.3·§4.10·§8) · 대조군 ----
            public const string ElementalWitchPatronSelection = "3172b6960c774e19ad029c5e4a96d3e4"; // Classes/Witch/Archetypes/ElementalWitch/ElementalWitchPatronSelection.jbp — 도달 불가 후견인 경로
            public const string DarkSisterArchetype = "411fa458481e44f0855d47a19358874b"; // Classes/Witch/Archetypes/DarkSisterWitch/DarkSisterArchetype.jbp — 도달 불가·겹침
            public const string WitchOfTheVeilArchetype = "08045a390a764d71b2150fd122e05e19"; // Classes/Witch/Archetypes/WitchOfTheVeil/WitchOfTheVeilArchetype.jbp — 도달 불가·겹침
            public const string CauldronWitchArchetype = "e0012a7015774e140be217f4a1480b6f"; // Classes/Witch/Archetypes/CauldronWitch/CauldronWitchArchetype.jbp — 미등록 아키타입(TTT 가 등록할 수 있음)
            public const string LeyLineGuardianWitchArchetype = "ee92a9813c04b1440a3af3c766b6b02d"; // Classes/Witch/Archetypes/LeyLineGuardianWitch/LeyLineGuardianWitchArchetype.jbp — 프리스티지 6 항목의 본편 둘째 PrerequisiteNoArchetype(정찰 §16-T6)
            public const string HexChannelerArchetype = "191b4be2152fde541ac8984c7f36e91d";  

            // ---- 주적·점검 (설계 §0·§4.4·§6, Step 0 정찰 §16-T1) ----
            public const string SubtypeDemon = "dc960a234d365cb4f905bdc5937e623a"; // CreatureAbilities/Subtypes/SubtypeDemon.jbp — 데몬 유형(전기 면역 근거)
            public const string SubtypeDevil = "6026db1a1a84ac14d9e94404af3baf0a"; // CreatureAbilities/Subtypes/SubtypeDevil.jbp — 저항 근거
            public const string SubtypeDaemon = "75c3a26f37be0c74196deaf64d81ee1a"; // CreatureAbilities/Subtypes/SubtypeDaemon.jbp — 저항 근거
            public const string ElectricityImmunity = "cd1e5ab641a833c49994aff99db98952"; // CreatureAbilities/EnergyImmunity/ElectricityImmunity.jbp — SubtypeDemon AddFacts 의 전기 면역(Step 0)
            public const string ExpeditiousRetreat = "4f8181e7a7f1d904fbaea64220e83379"; // Spells/Level1/ExpeditiousRetreat.jbp — 민첩 후견인 L2 주문
            public const string CatsGrace = "de7a025d48ad5da4991e7d3c682cf69d"; // Spells/Level2/CatsGrace.jbp — 민첩 후견인 L4 주문
            public const string Shapechange = "22b9044aa229815429d57d0a30e4b739"; // Spells/Level9/Shapechange/Shapechange.jbp — 민첩 후견인 L18 주문
            public const string Enervation = "f34fb78eaaec141469079af124bcfa0f"; // Spells/Level4/Enervation.jbp — 점검용 본편 주문
            public const string MagicMissile = "4ac47ddb9fa1eaf43a1b6809980cfbd2"; // Spells/Level1/MagicMissile.jbp — 별 블라스트 아이콘·역장 피해 원형(주문)
            public const string UnrelentingAssaultBuff = "4beb8f4181259ec49ac016c38d606e00"; // Mythic/MythicAbilities/UnrelentingAssaultBuff.jbp — 랭크 버프 선례(Stacking Rank, Ranks 10, 정찰 §16-T4)
            public const string CR2_DretchStandard = "2d643696b0653b846bf95c62be792e85"; // Units/Monsters/Demon Dretch/CR2_DretchStandard.jbp — 데몬 시험 유닛(Mobs 진영, SubtypeDemon → ElectricityImmunity, CR2 — 작성자 선택 §16-T1)
            public const string SubtypeFire = "23dc7b90d148b9d439f48e015a520a9c"; // CreatureAbilities/Subtypes/SubtypeFire.jbp — S7 지옥 VI~IX 화염 유형 단언(정찰 §16-T5)
            public const string Dlc5 = "95a25ca16bd54ce3b3ea56f83538fa0d";  
        }

        // 본편 로컬라이즈 문자열 키(블루프린트 GUID 가 아니다 — 팩 키 형식 8-4-4-4-12). 2026-11-16 능력 툴팁 지속 시간·내성 `<null>` 결함 수리
        // (코드로 만든 BlueprintAbility 의 LocalizedDuration·LocalizedSavingThrow 가 null — 근거 IL = BlueprintUtil 「LocalizedString 필드」 주석).
        // 시험 `tooltip:` 이 blueprints.zip(본편 능력의 Shared.stringkey)·enGB.json(문구)과 대조한다.
        public static class GameText
        {
            // 본편 공용 문구 「1 round/level」(SharedStringAsset ce23d48c23024d84b8d5f2f0fa9b6abb 의 stringkey). 과열 소환이 본뜬 본편 소환 주문
            // (Spells/Level1/SummonMonsterISingle.jbp ~ Level9/SummonMonsterIXSingle.jbp, 부모 SummonMonsterII~IXBase.jbp)의 LocalizedDuration 이 전부 이것이다(jbp 직독 2026-11-16).
            // 워락 소환의 기본 지속 = 위치 레벨 × WarlockRules.Dials.SummonRoundsPerLevel(1) 라운드(KitSpec.SummonRoundsRank)와 짝 — 시험이 다이얼 == 1 을 함께 잠근다.
            public const string DurationOneRoundPerLevel = "6250ccf0-1ed0-460f-8ce7-094c2da7e198";
        }
    }
}
