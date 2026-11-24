namespace WeaponSpiritShaman
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다, modinfo testSources). 발급: python -c "import uuid;print(uuid.uuid4().hex)"
    public static class Guids
    {
        public const string Archetype = "2e39ae370407439388477b11a246fcb4";

         
        public const string SpiritSelection = "7e3419f961434faea128cc6c404a28ac";
        public const string PetClass = "775cdbd6f06e4a119131bce0fbcbff8e";
        public const string PetProgression = "605746898de7446cbb9571db6e6ba624";
        public const string RankProgression = "6eea05b3926346648cad36d6eb0769c6";
        public const string SlotLockFeature = "060eae999c4243d98da588713a48051e";
        public const string ProficiencyFeature = "b4871e74603a40f884670ea33cbf450e";
         
        public const string SpiritMirrorFeature = "bb5ce7b4af5d44e2974d86626b9f7e3a";
         
        public const string PossessionResource = "b1cdc7ee92b4447a9242322dff0edcb9";
        public const string PossessionAbility = "3c9c7de25ce34aacba271fb9164a81b8";
        public const string PossessionBuff1 = "68ed39ca738f4d6381dc24c68f09952e";
        public const string PossessionBuff2 = "d8baf5f12ddb41ec951ff218d281c53c";
        public const string PossessionBuff3 = "142e10143fe74b08b8e634a3011f5694";
        public const string PossessionFeature1 = "2daa6ec3b8d44af2807cf0e4a1a36572";
        public const string PossessionFeature2 = "ab16b145411543cd881057ec3cbab114";
        public const string PossessionFeature3 = "7380621868594252a6ea0378a6acb149";
         
        public const string FusionBuff = "701504a9dc5448659dbc375e78fb7d69";
        public const string FusionAbility = "9f1d433d51e146e89240852585027b10";
        public const string PossessionFeature4 = "e2cb650e0ef64c6ebe7f349e8c4dad35";
        // 2026-11-16 발급 — 스피릿 배러지 / Spirit Barrage(소유자 결정 2026-11-16, 설계 §16): 하루 1회 자원·능력·18레벨 특성.
        public const string SpiritBarrageResource = "0d1d058b01de439782f4c5b606e7f5d9";
        public const string SpiritBarrageAbility = "5867ae8dc72e4e6692dc81ed67b8c421";
        public const string SpiritBarrageFeature = "547a5035eefc40a6b77199e5d157db9f";
        // 2026-11-17 발급 — 스피릿 웨폰 초상화(BlueprintPortrait, SpiritPortrait.Build). 본편 AnimatedWeapons 264ed688 의 그림 3장 + 파티 바 펫 띠(m_PetEyeImage).
        // 영병 유닛 59개의 m_Portrait 가 이것을 가리킨다. 펫 UISettings 는 보통 초상화를 저장하지 않지만(null → 유닛 블루프린트에서 매번 읽음) GUID 는 영구 고정으로 취급한다.
        public const string SpiritPortrait = "18a305ff5447411383115d81a420c3b7";

        // 범주별 파생 GUID 의 이름공간(DerivedGuids.For). 커밋 뒤 변경 금지 — 바꾸면 59 범주의 무기·유닛·특성 GUID 가 전부 바뀐다(세이브 호환 깨짐).
        // 중첩 형식에 두는 이유: Guids 자신의 문자열 상수는 전부 32자 hex 새 GUID 로 시험이 센다(tests\WeaponSpiritShamanTests.cs ExpectedNewGuidNames).
        public static class Derive
        {
            public const string Namespace = "WeaponSpiritShaman.Derived.2026-11-08";
        }

         
        // blueprints.zip 2026-11-08 직독). tests\WeaponSpiritShamanTests.cs GamePaths 가 같은 경로의 jbp AssetId 와 대조한다(이름 집합도 같아야 한다).
        // ShamanClass 는 공용 코드(src\Common)가 읽지 않으므로 여기 둔다(Strings.Configure 의 한국어 판별 클래스로 이 모드가 넘긴다).
        public static class Game
        {
            public const string ShamanClass = "145f1d3d360a7ad48bd95d392c81b38e"; // Classes/Shaman/ShamanClass.jbp
            public const string ShamanProgression = "d04a543c82b45694c805301bce9dac24"; // Classes/Shaman/ShamanProgression.jbp
            public const string ShamanSpiritAnimalSelection = "d22f319fefac4ca4b90f03ac5cb9c714"; // Classes/Shaman/ShamanSpiritAnimalSelection.jbp — 제거 L1
            public const string ShamanWanderingSpiritFeature = "70e6075a72633ba49804ccb6203fb18c"; // Classes/Shaman/ShamanSpirits/ShamanWanderingSpiritFeature.jbp — 제거 L4
            public const string ShamanWanderingHexFeature = "2674f98616188814fbba9f8fccf43564"; // Classes/Shaman/ShamanHexes/ShamanWanderingHexFeature.jbp — 제거 L6
            public const string ShamanWanderingHexFeature2 = "f15d95a394e950a42b1d6afbb9f781d9"; // Classes/Shaman/ShamanHexes/ShamanWanderingHexFeature2.jbp — 제거 L14
            public const string ShamanSPiritSelection = "00c8c566d1825dd4a871250f35285982"; // Classes/Shaman/ShamanSpirits/ShamanSPiritSelection.jbp — 유지 L1
            public const string ShamanHexSelection = "4223fe18c75d4d14787af196a04e14e7"; // Classes/Shaman/ShamanHexes/ShamanHexSelection.jbp — 유지 8회
            // 2026-11-15 생성 불변식 ③: 점검 유닛(WssTestUnit.Create)의 L1 선택 명시 — 각 선택 m_AllFeatures 첫 항목(엔진 기본값이 집는 것).
            public const string ShamanBattleSpiritProgression = "26e81d83d13dbf6448b10019c802612d"; // Classes/Shaman/ShamanSpirits/ShamanBattleSpirit/ShamanBattleSpiritProgression.jbp — ShamanSPiritSelection 첫 항목(L1·8·16·20 특성 = AddFacts·AddAbilityResources 뿐, 상시 수치 없음)
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c"; // Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6"; // Backgrounds/BackgroundsSelection/BackgroundNone.jbp — 효과 없음
            public const string SpiritHunterArchetype = "39b2fd5000e5eb142bfde2afa6c83f76"; // Classes/Shaman/Archetypes/SpiritHunter/SpiritHunterArchetype.jbp — 제거 4쌍이 같은 선례(설계 §4.2)

             
            public const string AnimalCompanionClass = "26b10d4340839004f960f9816f6109fe"; // Classes/Monster/Animal/AnimalCompanionClass.jbp — 펫 직업 복사 원본
            public const string AnimalCompanionBaseProgression = "f54159e8cfda69f4dbcfdf6ffbbd3293"; // Classes/Pets/AnimalCompanionArchetypes/Base/AnimalCompanionBaseProgression.jbp — 펫 진행표 복사 원본
            public const string AnimalCompanionUnit = "67c085077a5988445984e05d330c7798"; // Classes/Pets/AnimalCompanionUnit.jbp — 영병 유닛 복사 원본(원형, 잠금 없음 — 정찰 §2.3)
            public const string WildlandShamanAnimalCompanionProgression = "edd41ea87f1740d4be2de56961a80bb5"; // Classes/Shaman/Archetypes/WildlandShaman/WildlandShamanAnimalCompanionProgression.jbp — 랭크 진행표 복사 원본
            public const string AnimalCompanionSelectionWildlandShaman = "164c875d6b27483faba479c7f5261915"; // Classes/Shaman/Archetypes/WildlandShaman/AnimalCompanionSelectionWildlandShaman.jbp — 선택지 설정 원본
            public const string AnimalCompanionRank = "1670990255e4fe948a863bafd5dbda5d"; // Classes/Pets/AnimalCompanionRank.jbp — AddPet.m_LevelRank
            public const string AnimalCompanionSlotFeature = "75bb2b3c41c99e041b4743fdb16a4289"; // Classes/Pets/AnimalCompanionArchetypes/Base/AnimalCompanionSlotFeature.jbp — 펫 진행표에서 제거
            public const string AnimalType = "a95311b3dc996964cbaa30ff9965aaf6"; // Traits/Races/Common/AnimalType.jbp — 펫 진행표에서 제거
            public const string Multiattack = "8ac319e47057e2741b42229210eb43ed"; // CreatureAbilities/Multiattack.jbp — 펫 진행표에서 제거
            public const string WeaponFocus = "1e1f627d26ad36f43bbd26cc2bf8ac7e"; // Feats/WeaponFocus/WeaponFocus.jbp — 펫 자동 특기(물기 → 범주) · 영병 특성의 꺼 둔(Disabled) 옛 주술사 컴포넌트
            // 스피릿 웨폰 성장 특기(소유자 결정 2026-11-18, jbp 2026-11-18 직독): 펫 쪽 단계 특성의 AddParametrizedFeatures(범주) — SpiritRules.SpiritGrowthFeats.
            public const string WeaponSpecialization = "31470b17e8446ae4ea0dacd6c5817d86"; // Feats/WeaponSpecialization/WeaponSpecialization.jbp
            public const string GreaterWeaponFocus = "09c9e82965fb4334b984a1e9df3bd088"; // Feats/WeaponFocusGreater/WeaponFocusGreater.jbp
            public const string GreaterWeaponSpecialization = "7cf5edc65e785a24f9cf93af987d66b3"; // Feats/WeaponSpecializationGreater/WeaponSpecializationGreater.jbp
            public const string ImprovedCritical = "f4201c85a991369408740c6888362e20"; // Feats/ImprovedCritical/ImprovedCritical.jbp — 펫 AddClassLevels 의 물기 매개변수 항목(O-T3-13)
            public const string Airborne = "70cffb448c132fa409e49156d013b175"; // CreatureAbilities/Subtypes/Airborne.jbp — 영병 사실
            public const string ImmunityToProneCondition = "7e3cd4e16a990ab4e9ffa5d9ca3c4870"; // Buffs/ImmunityToProneCondition.jbp — 영병 사실
            public const string AnimatedWeaponsPortrait = "264ed6884cdc408fb9a701f5a2481332"; // Units/Portraits/Crusade/AnimatedWeapons.jbp — 영병 초상화
             
            public const string ArcaneWeaponSwitchAbility = "3c89dfc82c2a3f646808ea250eb91b91"; // Classes/Magus/ArcanePool/ArcaneWeaponSwitchAbility.jbp
        }

        // 본편 로컬라이즈 문자열 키(블루프린트 GUID 가 아니다 — 팩 키 형식 8-4-4-4-12). 2026-11-16 능력 툴팁 지속 시간·내성 칸 `<null>` 결함 수리(선례 = 블라스트 메이거스).
        // 시험 `tooltip:` 이 blueprints.zip(본편 능력의 Shared.stringkey)·enGB.json(문구)과 대조한다.
        public static class GameText
        {
            // 본편 공용 문구 「1 minute」(SharedStringAsset 391447c8b8710e343add686d2d2916bd 의 stringkey; 본편 46개 능력의 LocalizedDuration). 융합 능력의 지속 칸:
            // 강신·융합의 엔진 선례인 본편 ArcaneWeaponSwitchAbility(초자연·신속·자기 대상)의 LocalizedDuration 이 이것이다(Classes/Magus/ArcanePool/ArcaneWeaponSwitchAbility.jbp
            // 직독 2026-11-16). 융합 지속 = 강신 표 마지막 단계의 라운드(10) = 1분 — Fusion.Build 가 엔진 Rounds 로 「마지막 단계 == 1분」을 확인한다(어긋나면 던진다).
            public const string DurationOneMinute = "70e2c2f0-b2c6-423a-b6ec-c05084530366";
             
            // 「Instantaneous」 = "3d7fbfb9-10e8-4509-bab6-a8e4d3dbc3c8"(13개). 강신(단계별 3·5·7·10 라운드)에 맞는 고정 공용 문구는 본편에 없다 → 강신 지속 칸은 빈 키(칸 숨김, 설명문이 단계별 지속을 적는다).
        }
    }
}
