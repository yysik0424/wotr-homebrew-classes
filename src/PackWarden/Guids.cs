namespace PackWarden
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다). 발급: python -c "import uuid;print(uuid.uuid4().hex)"
    public static class Guids
    {
    public const string Archetype = "cb6d255e84c74f39bca9e688e1b58be8";
    public const string CompanionSelection = "bfa3669825584951a5c74fce6f26dcbc";
    public const string AspectFierceAbility = "1b0ac4554ef84ffab79c93595d038cbd";
    public const string AspectFierceBuff = "0a87c6d882c341b8a795cfb4acb3f437";
    public const string AspectFiercePackBuff = "9aaaeb3181fc4ee98945d8bfd2caec04";
    public const string AspectToughAbility = "d4250ef626bc45ab8d8720d1ae7b927c";
    public const string AspectToughBuff = "9279ce27e1214e58a28317416276cd42";
    public const string AspectToughPackBuff = "fdbc602cdfb34380a692f298d552eb9c";
    public const string AspectToughTempHpBuff = "e062e5bf57694ee8807e40105194e3a5";
    public const string AspectBothFeature = "5b2d0fa5056d43ec8613fa4f985882fc";
    public const string AspectsFeature = "2039f76dd5de4d988e4fd2ceac661d88";
    public const string CallFeature = "94fbb46ed2fd4a06947c06eb296068a8";
    public const string CallToggleAbility = "a3ac49e98427496d8aa175da0456ce56";
    public const string CallToggleBuff = "499e6eb560c544989a552bca5365e132";
    public const string CallMountedFeature = "7a59ab225a29469690823fc5c0efe000";
    public const string AuxStudiedBonusFeature = "38e5800a71db40f8877ede9653fc7e38";
    public const string AuxSummonPool = "4bff63192533451e984e226d512aa6ed";
    public const string AuxUnit_Bear = "5543a01275824d58801012b78c94e2ab";
    public const string AuxUnit_Boar = "8cf725befe4044beae85bab32053cfff";
    public const string AuxUnit_Centipede = "7e53cbe8d9d24231b69b31f163c2bb10";
    public const string AuxUnit_Dog = "8b356c65d23a4c81aa629cc1c48e78e9";
    public const string AuxUnit_Elk = "2888392b0b6848dd93e31cc440079bb4";
    public const string AuxUnit_Horse = "8987a034ee91426490808f1596227309";
    public const string AuxUnit_Mammoth = "8517a553187a4237a5cb95b928845e03";
    public const string AuxUnit_Smilodon = "63d564bb89014b2e99139a5e392ad37a";
    public const string AuxUnit_Triceratops = "f878e612519b4a3ebe672067d3f13281";
    public const string AuxUnit_Wolf = "990774ec3fca4f3e8d7a6d9bcf257412";
    public const string AuxUnit_HorsePreorder = "3d75e6bf2ca0493198263d331e0eb99c";
    public const string AuxUnit_SmilodonPreorder = "52d5389b5d7247f78008d290c9bd7992";
    public const string AuxUnit_TriceratopsPreorder = "f73063d33eb345b3965c7506888cb6b3";
    public const string CallToggleFeature = "274931bdbd4c4fa5b2b8c5ac26d10864";
    // 그림자 무리(2026-11-14 발급, 계획 2026-11-09-pack-warden-shadow-pack.md T1): 보조 복제본의 m_Brain(추종 행동 1개)과 그 추종 AI 행동.
    public const string ShadowBrain = "49a4599bca0e40cabfd4ff2c8d931881";
    public const string FollowPetAiAction = "51276c8d03b645ffb1e8651e6c74695c";
    // 2026-11-16 소유자 결정(발급 같은 날): 주 동물이 싸우는 동안 그 대상 곁으로 가는 두 번째 추종 행동 · 보조 전용 숨은 특성(소형 고정 + 기회 공격 면역).
    public const string FollowEngagedAiAction = "d0158bee6ad54d06b6854c35a0910d99";
    public const string AuxNatureFeature = "650b195844a94548aafc09f3cb432e76";
    // 2026-11-16 소유자 결정 「보조는 크기가 자라지 않는다」: 종 성장 특성에서 ChangeUnitSize 하나만 뺀 보조 전용 사본. 성장 특성 이름 → 사본 GUID.
    // 크기 변경이 있는 성장 특성 = 정확히 이 9개(blueprints.zip Classes/Pets 2026-11-16 직독 — 말 성장은 크기 변경이 없어 원본을 그대로 쓴다). 시험이 게임 데이터와 대조한다.
    // 실행 중에는 그 크기 부품이 본편 ChangeUnitSize 가 아닐 수 있다(TabletopTweaks-Base 가 ChangeUnitBaseSize 로 바꾼다 — 2026-11-16 실사고). 어느 부품을 뺄지는 GrowthSizeLayout 이 판정한다.
     
    // 단계 수 = 원래 크기 − 소형(PackRules.AuxDiceRestoreSteps 가 블루프린트 기본 크기 + 성장 크기 증가에서 계산). 지금 게임 데이터로 나오는 단계 = 1(중형)·2(대형).
    // 표에 없는 단계가 나오면 Build·소환이 던진다(0 단계로 넘기지 않음).
    public static readonly AuxDiceRestoreRow[] AuxDiceRestoreFeatures =
    {
        new AuxDiceRestoreRow(1, "2a8a344070014f46990fdfe4749a9efb"),
        new AuxDiceRestoreRow(2, "a71544372f2b48a3a63653adb0d6c9f9"),
    };
    public static readonly AuxUpgradeRow[] AuxUpgradesWithoutSize =
    {
        new AuxUpgradeRow("AnimalCompanionUpgradeBear", "633a512d07a945949e879045d54be656"),
        new AuxUpgradeRow("AnimalCompanionUpgradeBoar", "f8a3ef9e233d47088ec53e0cc54236f8"),
        new AuxUpgradeRow("AnimalCompanionUpgradeCentipede", "6acb559902f84cf4a483a284d4ac6b58"),
        new AuxUpgradeRow("AnimalCompanionUpgradeDog", "6520b8883fc146bea0586e03b7a83847"),
        new AuxUpgradeRow("AnimalCompanionUpgradeElk", "2304a063b6b9499fb7545bd49a5149a6"),
        new AuxUpgradeRow("AnimalCompanionUpgradeMammoth", "2411490ad95a44529be17e8897560c29"),
        new AuxUpgradeRow("AnimalCompanionUpgradeSmilodon", "b87b05c7b00149dea8896d010314130d"),
        new AuxUpgradeRow("AnimalCompanionUpgradeTriceratops", "fe92a391c9094c76b9829d9c4fb10020"),
        new AuxUpgradeRow("AnimalCompanionUpgradeWolf", "e911420705454f678646539dc7d7a90c"),
    };

        // 출처: blueprints.zip 2026-11-06 대조, 설계 문서 8절
        public static class Game
        {
            public const string HunterClass = "34ecd1b5e1b90b9498795791b0855239";
            public const string HunterOrisonsFeature = "85a5e66b176c0aa4c8ed1acdf946321e";
            public const string AnimalCompanionSelectionHunter = "715ac15eb8bd5e342bc8a0a3c9e3e38f";
            public const string AnimalCompanionEmptyCompanion = "472091361cf118049a2b4339c4ea836a";
            public const string HunterAnimalFocusFeature = "443365823b7d6d14b8d12f4e7bce1077";
            public const string HunterExtraAnimalFocus = "cbc90fcdb60809744989d386a98cd21c";
            public const string MasterHunter = "d8a126a3ed3b62943a597c937a4bf840"; // 코드 참조 없음: 20레벨에 그대로 유지(2026-11-07). 설계 8절 대조용
            public const string TandemStudyTarget = "3dac0ae65dec4b619c035df07751b59d";
            public const string TandemBase = "7a5dea90412e48d982a8d609365f4362";
            public const string TandemTechniques = "d03312858cd5420e8bad2756ce4a72ba";
            public const string SlayerSwiftStudyTarget = "40d4f55a5ac0e4f469d67d36c0dfc40b";
            internal static string MissingTandemSource(System.Func<string, bool> available)
            {
                foreach (var guid in new[] { TandemBase, TandemTechniques })
                    if (!available(guid)) return guid;
                return null;
            }
            public const string AnimalCompanionClass = "26b10d4340839004f960f9816f6109fe";
            public const string AnimalCompanionRank = "1670990255e4fe948a863bafd5dbda5d";
            public const string HunterProgression = "f4943530be50c3c4da841a7b96cfae98";
            public const string HunterTactics = "1b9916f7675d6ef4fb427081250d49de";
            public const string HunterRaiseCompanion = "27ad1316abbbbb34b8ffb9a87f38c10e";
            public const string AnimalCompanionFeatureWolf = "67a9dc42b15d0954ca4689b13e8dedea";
            // BlueprintRoot.SystemMechanics.m_SummonedUnitBuff (blueprints.zip Root/SystemMechanics/Conditions/SummonedUnitBuff.jbp, 2026-11-07).
            // RuleSummonUnit.OnTrigger IL_00AD~0113 이 모든 소환체에 붙인다(Unlootable 조건 + 해체 FX, StayOnDeath).
            public const string SummonedUnitBuff = "8728e884eeaa8b047be04197ecf1a0e4";

            // --- 그림자 무리(2026-11-14): 독립 모드 두뇌 = 본편 동물 동료 두뇌(설계 §11 D6). 참조만, 쓰기 0. ---
            // blueprints.zip Brain/Companions/Character_Brain.jbp AssetId, m_Actions = {ManualAttackAiAction, AttackAiAction}(2026-11-14 직독).
            // 13종 AnimalCompanionUnit*.jbp 의 m_Brain 이 전부 이 값(tests\PackRulesTests.cs CheckShadowRules 가 대조).
            public const string CharacterBrain = "cf986dd7ba9d4ec46ad8a3a0406d02ae";
            public const string ManualAttackAiAction = "a4a34d4a6382ab14ea6838c3bcc0012c"; // Brain/Actions/ManualAttackAiAction.jbp (BlueprintAiAttack, BaseScore 10)
            public const string AttackAiAction = "866ffa6c34000cd4a86fb1671f86c7d8";       // Brain/Actions/AttackAiAction.jbp (BlueprintAiAttack, BaseScore 1)
            // 그림자 두뇌의 추종 행동이 값을 복사해 오는 본편 추종 행동(Brain/Actions/FollowEnemyAiAction.jbp, BlueprintAiFollow, BaseScore 1.0, MinDifficulty Story, CombatCount 0 — 2026-11-14 직독). 참조만.
            public const string FollowEnemyAiAction = "959d92846619fc846bef1e5360be9ce9";

            // --- 보조 등장·소멸 연출(2026-11-09, 소유자 결정 = 본편 소환 주문의 등장 이펙트를 기본값). 규칙 영향 0: AuxUnits 가 FxHelper.SpawnFxOnUnit 만 부른다. ---
            // 등장: 본편 Summon Nature's Ally I(Spells/Level1/SummonNaturesAllyI.jbp, HunterSpelllist d090b791… 1레벨)의 ContextActionSpawnMonster.AfterSpawn 이
            // 붙이는 버프 Buffs/SummonedCreatureSpawnAllyI-III.jbp 의 FxOnStart.AssetId. 본편은 이 버프의 Buff.TrySpawnParticleEffect(IL_00C4~0113)가 띄운다.
            // SummonedUnitAppearBuff(6179bbe7…)는 FxOnStart=null 인 2초 CantAct/CantMove/Untargetable 규칙 버프라 연출의 출처가 아니다(blueprints.zip 직독).
            // 소환 괴물 I 계열의 등장 프리팹은 79e9da3c32c920642b50529568c40ab3(Buffs/SummonedCreatureSpawnMonsterI-III.jbp) — 동물 보조라 자연의 동료 쪽을 택했다.
            public const string SummonedCreatureSpawnAllyBuff = "2c3b98e56c1b4e9479f692d7ef95b2d2";
            public const string SummonAppearFx = "79dfc7defb517b84d838371f9425bd2d";
            // 소멸: 본편 SummonedUnitBuff 컴포넌트 OnRemoved(IL_0030~011A)는 소환체 블루프린트의 Visual.BloodPuddleFx(=DismemberFx, 용해 프리팹)를
            // FxHelper.SpawnFxOnUnit 으로 띄운다. 보조의 복제 원본(Classes/Pets/AnimalCompanionUnit*.jbp)은 그 값이 null 이라 본편 자연의 동료 동물 소환체 2종에서 빌린다.
            // 휴리스틱(크기 기준): 보조 크기 Large 이상 = 스밀로돈 소환체 값, 그 미만 = 표범 소환체 값. 본편은 모델마다 지정하므로 정확한 대응이 아니다.
            public const string LeopardSummonedUnit = "768275c9885dd954fb3c84ba69ac4281";   // Units/Monsters/Leopard/LeopardSummoned.jbp (Size=Medium)
            public const string SmilodonSummonedUnit = "beae4985629a6f64eb98081e3171e4c1";  // Units/Monsters/Smilodon/SmilodonSummoned.jbp (Size=Large)
            public const string SummonDissolveFxMedium = "d70dea81d480dea4ca3d076a043b1899"; // LeopardSummoned.Visual.BloodPuddleFx
            public const string SummonDissolveFxLarge = "efc249c7c2d61a541a81c039c63ca262";  // SmilodonSummoned.Visual.BloodPuddleFx

            // --- 게임 안 점검(과제 9) 전용. 일반 플레이 경로는 읽지 않는다. ---
            // 용병 템플릿: blueprints.zip Root/CustomCompanion.jbp AssetId = BlueprintRoot.m_CustomCompanion (Root/BlueprintRoot.jbp, 2026-11-07).
            // m_Faction = Player(72f24026…), m_Race = null → TestUnit 이 HumanRace 를 넣는다. 실행 때 BlueprintRoot.CustomCompanion 과 같은지 검사.
            public const string CustomCompanionUnit = "baaff53a675a84f4983f1e2113b24552";
            // blueprints.zip Races/HumanRace.jbp AssetId (2026-11-07). 용병 템플릿에 종족이 없어서 레벨업 전에 SetRace 로 넣는다.
            public const string HumanRace = "0a5d473ead98b0646b94495af250fdc4";
            // 적대 저레벨 유닛: blueprints.zip Units/Monsters/Kobold/CR0.5_KoboldMeleeWarriorLevel1.jbp AssetId (2026-11-07).
            // Experience.CR = 1(정수 필드, 이름은 CR0.5), m_Faction = Factions/Mobs.jbp(0f539bab…, m_AttackFactions 에 Player 포함).
            public const string HostileKobold = "d0ed34c1f6600e04592e7921c1dd6ff1";
            // 검수 S1(과제 10): 무리 동료 선택지에 없어야 하는 소형 시작 종. blueprints.zip Classes/Pets/AnimalCompanionFeature{Leopard,Monitor,Velociraptor}.jbp AssetId (2026-11-07).
            public const string AnimalCompanionFeatureLeopard = "2ee2ba60850dd064e8b98bf5c2c946ba";
            public const string AnimalCompanionFeatureMonitor = "ece6bde3dfc76ba4791376428e70621a";
            public const string AnimalCompanionFeatureVelociraptor = "89420de28b6bb9443b62ce489ae5423b";
            // 크기 필터(기본 크기 중형 미만)가 떨어뜨려야 하는 본편 종: 정확히 이 3개(설계 4.3). Archetype.Build 가 단언한다.
            public static readonly string[] SizeExcludedCompanions = { AnimalCompanionFeatureLeopard, AnimalCompanionFeatureMonitor, AnimalCompanionFeatureVelociraptor };

             
            // 출처: blueprints.zip 2026-11-06, task-4-report.md kept list (Classes/Pets/<Name>.jbp AssetId, 2026-11-07 재대조).
            // 보조 유닛 GUID 열은 같은 행에 둔다: AuxUnits 의 이름→보조 표가 이 표에서 파생되므로 두 표가 어긋날 수 없다.
            // 시험(tests\PackRulesTests.cs)이 blueprints.zip 의 AnimalCompanionSelectionHunter.m_AllFeatures 와 이 표 + SizeExcludedCompanions 를 대조한다.
            public static readonly PackCompanion[] PackCompanions =
            {
                new PackCompanion("AnimalCompanionFeatureBear", "f6f1cdcc404f10c4493dc1e51208fd6f", AuxUnit_Bear),
                new PackCompanion("AnimalCompanionFeatureBoar", "afb817d80b843cc4fa7b12289e6ebe3d", AuxUnit_Boar),
                new PackCompanion("AnimalCompanionFeatureCentipede", "f9ef7717531f5914a9b6ecacfad63f46", AuxUnit_Centipede),
                new PackCompanion("AnimalCompanionFeatureDog", "f894e003d31461f48a02f5caec4e3359", AuxUnit_Dog),
                new PackCompanion("AnimalCompanionFeatureElk", "aa92fea676be33d4dafd176d699d7996", AuxUnit_Elk),
                new PackCompanion("AnimalCompanionFeatureHorse", "9dc58b5901677c942854019d1dd98374", AuxUnit_Horse),
                new PackCompanion("AnimalCompanionFeatureMammoth", "6adc3aab7cde56b40aa189a797254271", AuxUnit_Mammoth),
                new PackCompanion("AnimalCompanionFeatureSmilodon", "126712ef923ab204983d6f107629c895", AuxUnit_Smilodon),
                new PackCompanion("AnimalCompanionFeatureTriceratops", "2d3f409bb0956d44187e9ec8340163f8", AuxUnit_Triceratops),
                new PackCompanion("AnimalCompanionFeatureWolf", AnimalCompanionFeatureWolf, AuxUnit_Wolf),
                new PackCompanion("AnimalCompanionFeatureHorse_PreorderBonus", "bfeb9be0a3c9420b8b2beecc8171029c", AuxUnit_HorsePreorder),
                new PackCompanion("AnimalCompanionFeatureSmilodon_PreorderBonus", "44f4d77689434e07a5a44dcb65b25f71", AuxUnit_SmilodonPreorder),
                new PackCompanion("AnimalCompanionFeatureTriceratops_PreorderBonus", "52c854f77105445a9457572ab5826c00", AuxUnit_TriceratopsPreorder),
            };

            // 본편 AnimalCompanionSelectionHunter.ComponentsArray 의 AddFeatureOnApply 3개가 가리키는 특성(blueprints.zip
            // Classes/Hunter/AnimalCompanionSelectionHunter.jbp Components, 2026-11-07). 우리 선택지는 원본에서 파생해 복제하고(Archetype.Build),
            // 이 값들은 그 결과를 잠그는 단언에만 쓴다. 빠지면 펫 레벨 0·기승 토글 없음이 조용히 생긴다(최종 리뷰 C1).
            public const string HunterAnimalCompanionProgression = "446fe89490cab7d44957efebeb8cc2b1"; // AnimalCompanionRank 를 헌터 레벨마다 준다 = 펫 레벨
            public const string MountTargetFeature = "cb06f0e72ffb5c640a156bd9f8000c1d";               // 기승 토글(Units/Abilities/Mounts/MountTargetFeature.jbp)
            public const string AnimalCompanionArchetypeSelection = "65af7290b4efd5f418132141aaa36c1b"; // 동물 동료 아키타입 선택
            public static readonly string[] CompanionSelectionAppliedFeatures = { HunterAnimalCompanionProgression, MountTargetFeature, AnimalCompanionArchetypeSelection };
        }

        // AuxDiceRestoreFeatures 의 한 행(게임 형식 없음).
        public sealed class AuxDiceRestoreRow
        {
            public readonly int Steps; public readonly string Guid;
            public AuxDiceRestoreRow(int steps, string guid) { Steps = steps; Guid = guid; }
        }

        // AuxUpgradesWithoutSize 의 한 행(게임 형식 없음).
        public sealed class AuxUpgradeRow
        {
            public readonly string UpgradeName, AuxGuid;
            public AuxUpgradeRow(string upgradeName, string auxGuid) { UpgradeName = upgradeName; AuxGuid = auxGuid; }
        }

        // Game.PackCompanions 의 한 행. 게임 형식 없이 문자열만(콘솔 시험이 컴파일한다).
        public sealed class PackCompanion
        {
            public readonly string Name, FeatureGuid, AuxGuid;
            public PackCompanion(string name, string featureGuid, string auxGuid) { Name = name; FeatureGuid = featureGuid; AuxGuid = auxGuid; }
        }
    }
}
