namespace BlastMagus
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다). 발급: python -c "import uuid;print(uuid.uuid4().hex)"
    public static class Guids
    {
        public const string Archetype = "a9b7aae0c5b1493792c0ac2c74006772";
        public const string ElementSelection = "5f0d3e4f52eb4507bbb15e92fe41ea22";
        public const string ElementFire = "8239a9e819994097a16bb045319a9c6b";
        public const string ElementCold = "5c8646d3ced848a4830297c9b6ca5c01";
        public const string ElementElectric = "dac1e4cf3684437db9b5569096147c38";
        public const string BlastFeature = "660fd6beb9134c409b0622e717e0f9e4";
        public const string BurnZeroResource = "45898129868142b5b1a7264962c51108";
        public const string EnergizeToggle = "ddc21826367c4248b86633bad335f08a";
        public const string EnergizeBuff = "84c3fa525e114890b456704d1eff0adc";
        public const string EnergizeFeature = "7e3b4e7b79c1406cacd0de73025dd652";
        public const string Energize7 = "bec115dbfc854f7f8979881d360faa49";
        public const string Energize13 = "e2bd2358be0340fcbdfcf76bb404a809";
        public const string Energize19 = "a583e8785c7f4ae08236d8e56fa88ee3";
        public const string BlastStrikeAbility = "eb3bfd5c933d4a0d9d8ea6faad499079";
        public const string BlastStrikeBuff = "26cbddf293d1413a90f3e8af801de291";
        public const string BlastStrikeFeature = "785ddf2857e446a8abb25cf12848da1c";
        public const string BlastStrikeSourceAbility = "8b35c4821df545c3ac8462260d7c609c";
        public const string CompositeFeature20 = "bdf110739b9c42cbaba7e3d886437859";
        public const string BlastRankMarker = "2e98ff7895d24503b569361e3dfc215a";
         
        public const string BlastPoolFeature = "c1846d6cc8584546b29c3f2b9e502a72";
        public const string ArcanaSelection = "986d3cf066014a4c9919303136119914";
         
        public const string ElementAir = "e0f7b221daa24016a85a7e655f6e9dfe";
        public const string ElementEarth = "bcd633771435482f860603baeed0267b";
        public const string ElementWater = "424c9a162a3a40ba8f07ca466dd64713";
         
        public const string ElementTelekinetic = "4343cf943f454a079cffe982b5ea3379";
        public const string ElementGravity = "842be4469ba44eacb4dd72f85f3b01b9";
        public const string ElementWood = "f381dc7af69a404890efa378ba782b27";
        public const string ElementNegative = "4cc8d6529d24458a91e1a8c7ff49beea";
        public const string ElementPositive = "78fbb411fc3c4b1a841d428acdc0b48f";
         
        // 원소 → (토글, 버프) 대응은 EnergizeTable.cs 한 표가 갖는다. 위의 EnergizeToggle·EnergizeBuff 는 예전 단일 토글 — 세이브 호환을 위해 GUID 를 유지한다(Energize.cs 「예전 세이브」).
        public const string EnergizeParentToggle = "120b90339886464ea65bdb8b4db91390";
        public const string EnergizeToggleFire = "9931618b577d426eb3d08a8a8897572e";
        public const string EnergizeToggleCold = "3e296b10e3b8485787688e79472929ff";
        public const string EnergizeToggleElectric = "f7463001aac64bfbb67ef60262f7b0f2";
        public const string EnergizeToggleAir = "59d3c266d8db48c3b28c0f63ae8b3cab";
        public const string EnergizeToggleEarth = "52bcb8c5fb124863bbba44b2a96368d8";
        public const string EnergizeToggleWater = "563b8336ff5b4499a777de43e9b8a93c";
        public const string EnergizeToggleTelekinetic = "7fe445b652f9446d9e66133054db9a8a";
        public const string EnergizeToggleGravity = "9cb7a41adfc34786ab22c4155440d9a9";
        public const string EnergizeToggleWood = "dbc2256d82fd4bef8f3b470fe3d6891b";
        public const string EnergizeToggleNegative = "849986be8da94244b8d00786cfc04c39";
        public const string EnergizeTogglePositive = "c9db7e3e5ee645e9adef09bd72d4602b";
        public const string EnergizeBuffFire = "5dc1bea3b4b04ec1806a9f28cbe1aac1";
        public const string EnergizeBuffCold = "eb8d851204ba4da0a5d1d43e5ec580cc";
        public const string EnergizeBuffElectric = "54507fb781124e77b0393a76a10be1d8";
        public const string EnergizeBuffAir = "1b38755a23574d9aa35c457be65c8263";
        public const string EnergizeBuffEarth = "540a21b011ec4e5c931011ee96ab580e";
        public const string EnergizeBuffWater = "7e43e3abe03f4d5c86f4e70282842597";
        public const string EnergizeBuffTelekinetic = "1f4f66cba5ea4c74999d5a5604bb384c";
        public const string EnergizeBuffGravity = "f2e315cb6ca549599390b56ed51ef4e2";
        public const string EnergizeBuffWood = "40428977f6fc483abe4f46637866ebfd";
        public const string EnergizeBuffNegative = "745a6551177f47499b480510f611bbc9";
        public const string EnergizeBuffPositive = "04ab3182d90342a98c68b7695cd183ca";

         
        // jbp AssetId 에서 풀 GUID 를 읽어 적었다. tests\BlastMagusTests.cs 가 같은 경로의 jbp AssetId 와 대조한다).
        // MagusClass 는 여기 두지 않는다: 공용 코드가 읽으므로 WotrClassMod.Common.GameGuids.MagusClass 에 있다(두 곳에 두면 시험이 실패시킨다).
        public static class Game
        {
            // Stock ExtraArcanePool and AbundantArcanePool features and DarkCodex blueprints.txt.
            public const string ExtraArcanePool = "42f96fc8d6c80784194262e51b0a1d25";
            public const string AbundantArcanePool = "8acebba92ada26043873cae5b92cef7b";
            public const string MagusProgression = "1b912721a7e075d4f9cfe8dafa39414c"; // Classes/Magus/MagusProgression.jbp
            public const string MagusSpellbook = "5d8d04e76dff6c5439de99af0d57be63"; // Classes/Magus/MagusSpellbook.jbp
            public const string MagusCantripsFeature = "fa5799fb32844e94e88d4cb3430610ff"; // Classes/Magus/MagusCantripsFeature.jbp
            public const string DetectMagic = "ee0b69e90bac14446a4cf9a050f87f2e"; // Classes/Basic/DetectMagic.jbp
            public const string MagusSpellRecallFeature = "61fc0521e9992624e9c518060bf89c0f"; // Classes/Magus/SpellRecall/MagusSpellRecallFeature.jbp
            public const string MagusImprovedSpellRecallFeature = "0ef6ec1c2fdfc204fbd3bff9f1609490"; // Classes/Magus/SpellRecall/MagusImprovedSpellRecallFeature.jbp
            public const string ArcaneWeaponPlus2 = "36b609a6946733c42930c55ac540416b"; // Classes/Magus/ArcanePool/ArcaneWeaponPlus2.jbp
            public const string ArcaneWeaponPlus3 = "70be888059f99a245a79d6d61b90edc5"; // Classes/Magus/ArcanePool/ArcaneWeaponPlus3.jbp
            public const string ArcaneWeaponPlus4 = "1804187264121cd439d70a96234d4ddb"; // Classes/Magus/ArcanePool/ArcaneWeaponPlus4.jbp
            public const string ArcaneWeaponPlus5 = "3cbe3e308342b3247ba2f4fbaf5e6307"; // Classes/Magus/ArcanePool/ArcaneWeaponPlus5.jbp
            public const string ArcanePoolResourse = "effc3e386331f864e9e06d19dc218b37"; // Classes/Magus/ArcanePool/ArcanePoolResourse.jbp
            public const string ArcaneAccuracyAbility = "1b7fb8120390ca24c9da98ce87780b7f"; // Classes/Magus/Arcanas/ArcaneAccuracyAbility.jbp — 폭발 능력 모양의 선례(정찰 §8) + 2026-11-15 폭발 FX 원본(FxTable, 빌드 시 읽기만)
            public const string FireBlastFeature = "cbc88c4c166a0ce4a95375a0a721bd01"; // Classes/Kineticist/ElementalFocus/Fire/FireBlast/FireBlastFeature.jbp
            public const string ColdBlastFeature = "ce625487d909b154c9305e60e4fc7d60"; // Classes/Kineticist/ElementalFocus/Water/ColdBlast/ColdBlastFeature.jbp
            public const string ElectricBlastFeature = "c2c28b6f6f000314eb35fff49bb99920"; // Classes/Kineticist/ElementalFocus/Air/ElectricBlast/ElectricBlastFeature.jbp
            public const string FireBlastBase = "83d5873f306ac954cad95b6aeeeb2d8c"; // Classes/Kineticist/ElementalFocus/Fire/FireBlast/FireBlastBase.jbp
            public const string ColdBlastBase = "7980e876b0749fc47ac49b9552e259c1"; // Classes/Kineticist/ElementalFocus/Water/ColdBlast/ColdBlastBase.jbp
            public const string ElectricBlastBase = "45eb571be891c4c4581b6fcddda72bcd"; // Classes/Kineticist/ElementalFocus/Air/ElectricBlast/ElectricBlastBase.jbp
            public const string FireBlastAbility = "7b4f0c9a06db79345b55c39b2d5fb510"; // Classes/Kineticist/ElementalFocus/Fire/FireBlast/FireBlastAbility.jbp
            public const string ColdBlastAbility = "f6d32ecd20ebacb4e964e2ece1c70826"; // Classes/Kineticist/ElementalFocus/Water/ColdBlast/ColdBlastAbility.jbp
            public const string ElectricBlastAbility = "24f26ac07d21a0e4492899085d1302f6"; // Classes/Kineticist/ElementalFocus/Air/ElectricBlast/ElectricBlastAbility.jbp
            public const string KineticBlastFeature = "93efbde2764b5504e98e6824cab3d27c"; // Classes/Kineticist/ElementalFocus/KineticBlastFeature.jbp
            public const string KineticistMainStatProperty = "f897845bbbc008d4f9c1c4a03e22357a";  
            public const string KineticBlastEnergy = "45e79e93b97555047b3cf6996bfbda52"; // Weapons/Types/Special/KineticBlastEnergy.jbp
            public const string BurnFeature = "57e3577a0eb53294e9d7cc649d5239a3"; // Classes/Kineticist/Burn/BurnFeature.jbp
            public const string GatherPowerAbility = "6dcbffb8012ba2a4cb4ac374a33e2d9a"; // Classes/Kineticist/GatherPower/GatherPower.jbp
            public const string GatherPowerFeature = "0601925a028b788469365d5f8f39e14a"; // Classes/Kineticist/GatherPower/GatherPowerFeature.jbp
            public const string GatherPowerBuff1 = "e6b8b31e1f8c524458dc62e8a763cfb1"; // Classes/Kineticist/GatherPower/GatherPowerBuffI.jbp
            public const string GatherPowerBuff2 = "3a2bfdc8bf74c5c4aafb97591f6e4282"; // Classes/Kineticist/GatherPower/GatherPowerBuffII.jbp
            public const string GatherPowerBuff3 = "82eb0c274eddd8849bb89a8e6dbc65f8"; // Classes/Kineticist/GatherPower/GatherPowerBuffIII.jbp
            public const string KineticBladeEnableBuff = "426a9c079ee7ac34aa8e0054f2218074"; // Classes/Kineticist/Burn/KineticBladeEnableBuff.jbp
            public const string ElementalFocusSelection = "1f3a15a3ae8a5524ab8b97f469bf4e3d"; // Classes/Kineticist/ElementalFocus/ElementalFocusSelection.jbp
            public const string BlueFlameBlastFeature = "89dfce413170db049b0386fff333e9e1"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/BlueFlameBlast/BlueFlameBlastFeature.jbp
            public const string IceBlastFeature = "a8cc34ca1a5e55a4e8aa5394efe2678e"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/IceBlast/IceBlastFeature.jbp
            public const string ThunderstormBlastFeature = "295080cf4691df9438f58ff5ce79ee65"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/ThunderstormBlast/ThunderstormBlastFeature.jbp
            public const string BlueFlameBlastAbility = "322911b79eabdb64f8b079c7a2d95e68"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/BlueFlameBlast/BlueFlameBlastAbility.jbp
            public const string IceBlastAbility = "519e36decde7c964d87c2ffe4d3d8459"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/IceBlast/IceBlastAbility.jbp
            public const string ThunderstormBlastAbility = "fc432e7a63f5a3545a93118af13bcb89"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/ThunderstormBlast/ThunderstormBlastAbility.jbp
            public const string ZenArcherZenArcheryFeature = "379c0da9f384e7547a70c259445377f5"; // Classes/Monk/Archetypes/ZenArcher/ZenArcherZenArcheryFeature.jbp — 출처 기록용(런타임 미사용: AttackStatReplacement 선례)
             
            public const string StandardLongsword = "6fd0a849531617844b195f452661b2cd"; // Weapons/Items/Standard/MartialOneHanded/StandardLongsword.jbp
            public const string ShockingGraspCast = "ab395d2335d3f384e99dddee8562978f"; // Spells/Level1/ShockingGraspCast.jbp
            public const string ShockingGraspEffect = "17451c1327c571641a1345bd31155209"; // Spells/Level1/ShockingGraspEffect.jbp
            public const string SpellStrikeAbility = "e958891ef90f7e142a941c06c811181e"; // Classes/Magus/SpellStrike/SpellStrikeAbility.jbp
            public const string SpellStrikeFeature = "be50f4e97fff8a24ba92561f1694a945"; // Classes/Magus/SpellStrike/SpellStrikeFeature.jbp
            public const string SpellCombatAbility = "8898a573e8a8a184b8186dbc3a26da74"; // Classes/Magus/SpellCombat/SpellCombatAbility.jbp
            public const string BlueFlameBlastBase = "d29186edb20be6449b23660b39435398"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/BlueFlameBlast/BlueFlameBlastBase.jbp
            public const string MagusSpellLevels = "6326b540f7c6a604f9d6f82cc0e2293c"; // Classes/Magus/MagusSpellLevels.jbp
             
            public const string ArcanePoolFeature = "3ce9bb90749c21249adc639031d5eed1"; // Classes/Magus/ArcanePool/ArcanePoolFeature.jbp
            public const string ArcaneWeaponSwitchAbility = "3c89dfc82c2a3f646808ea250eb91b91"; // Classes/Magus/ArcanePool/ArcaneWeaponSwitchAbility.jbp
            public const string MagusArcanaSelection = "e9dc4dfc73eaaf94aae27e0ed6cc9ada"; // Classes/Magus/Arcanas/MagusArcanaSelection.jbp
            public const string BaneBladeFeature = "a2e0691dcfda2374e84d8bbf480e06a0"; // Classes/Magus/Arcanas/BaneBladeFeature.jbp
            public const string DevotedBladeFeature = "4be0bb10e110a35419e406da767bd1e3"; // Classes/Magus/Arcanas/DevotedBladeFeature.jbp
            public const string EnduringBladeFeature = "7a73bf165e8eda6478b4419f857d1ab5"; // Classes/Magus/Arcanas/EnduringBladeFeature.jbp
            public const string GhostBladeFeature = "8896f327c59569c4eaf129bf35b96c1f"; // Classes/Magus/Arcanas/GhostBladeFeature.jbp
            public const string ExtraArcanaSelection = "00727883edf145e2a6bce9ad176ecfd8"; // Feats/DLC5/ExtraArcanaSelection.jbp — 출처 기록용(런타임 미사용: 수용된 부수 효과 O1, 설계 §11-B)
             
            public const string KineticBlastPhysical = "35670ee12a5be184ebe64db4a2f2c4d5"; // Weapons/Types/Special/KineticBlastPhysical.jbp — 계약 7번 허용 투사체 형식
            public const string IceBlastBase = "403bcf42f08ca70498432cf62abee434"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/IceBlast/IceBlastBase.jbp
            public const string ThunderstormBlastBase = "b813ceb82d97eed4486ddd86d3f7771b"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/ThunderstormBlast/ThunderstormBlastBase.jbp
             
            public const string KineticBlastPhysicalBlade = "b05a206f6c1133a469b2f7e30dc970ef"; // Weapons/Types/Special/KineticBlastPhysicalBlade.jbp — 출처 기록용(런타임 미사용: 시험 `strike: physical magic source shape` 가 두 번째 컴포넌트 형식으로 대조)
             
            public const string AirBlastFeature = "cb09e292ad9acc3428fa0dfdcbb83883"; // Classes/Kineticist/ElementalFocus/Air/AirBlast/AirBlastFeature.jbp
            public const string AirBlastBase = "0ab1552e2ebdacf44bb7b20f5393366d"; // Classes/Kineticist/ElementalFocus/Air/AirBlast/AirBlastBase.jbp
            public const string AirBlastAbility = "31f668b12011e344aa542aa07ab6c8d9"; // Classes/Kineticist/ElementalFocus/Air/AirBlast/AirBlastAbility.jbp
            public const string EarthBlastFeature = "7f5f82c1108b961459c9884a0fa0f5c4"; // Classes/Kineticist/ElementalFocus/Earth/EarthBlast/EarthBlastFeature.jbp
            public const string EarthBlastBase = "e53f34fb268a7964caf1566afb82dadd"; // Classes/Kineticist/ElementalFocus/Earth/EarthBlast/EarthBlastBase.jbp
            public const string EarthBlastAbility = "b28c336c10eb51c4a8ded0258d5742e1"; // Classes/Kineticist/ElementalFocus/Earth/EarthBlast/EarthBlastAbility.jbp
            public const string WaterBlastFeature = "560887b5187098b428364de03e628b53"; // Classes/Kineticist/ElementalFocus/Water/WaterBlast/WaterBlastFeature.jbp
            public const string WaterBlastBase = "d663a8d40be1e57478f34d6477a67270"; // Classes/Kineticist/ElementalFocus/Water/WaterBlast/WaterBlastBase.jbp
            public const string WaterBlastAbility = "e3f41966c2d662a4e9582a0497621c46"; // Classes/Kineticist/ElementalFocus/Water/WaterBlast/WaterBlastAbility.jbp
            public const string MetalBlastFeature = "ad20bc4e586278c4996d4a81b2448998"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/MetalBlast/MetalBlastFeature.jbp
            public const string MetalBlastBase = "6276881783962284ea93298c1fe54c48"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/MetalBlast/MetalBlastBase.jbp
            public const string MetalBlastAbility = "665cfd3718c4f284d80538d85a2791c9"; // Classes/Kineticist/ElementalFocus/CompositeBlasts/MetalBlast/MetalBlastAbility.jbp
            // 2026-11-15 생성 불변식 ③(TestUnitGuard): 시험 유닛 L1 배경 선택을 엔진 기본값 대신 명시(BmTestUnit.DefaultPicks). blueprints.zip AssetId 직독, 시험 GamePaths 가 대조.
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c"; // Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6"; // Backgrounds/BackgroundsSelection/BackgroundNone.jbp — 효과 없음
        }

        // KineticistElementsExpanded 3.3.2(선택 모드, 설계 §4.3-4 R4). 형식 참조·AssemblyRef 없이 이 문자열만 쓴다(시험 `kee: no assembly reference`).
         
        // 런타임 값과 같은지는 2단계 A 검증 단계가 GUID 해석·외부 계약으로 판정한다(어긋나면 PresentFailed). 오프라인 시험은 blueprints.zip 에 없음과 리터럴 사본 일치를 단언한다(`kee: rows literal`).
        public static class Kee
        {
             
            public const string ModId = "KineticistElementsExpanded";
            public const string TelekineticBlastFeature = "e86649f76cba4483bed7f01859c6b425"; // blueprints.txt:3
            public const string TelekineticBlastBase = "ac038f9898ef4ba7b46bfcafdbc77818"; // blueprints.txt:5
            public const string TelekineticBlastAbility = "bc3c9fe311294c0dbc8fee81b326fb5f"; // blueprints.txt:4
            public const string GravityBlastFeature = "969553ee365642bf9389604cf52b6035"; // blueprints.txt:107
            public const string GravityBlastBase = "d927b99fb8a946c0831edfb97eb749a4"; // blueprints.txt:106
            public const string GravityBlastAbility = "f75f44162a7e48fa860e0c98c6429c0a"; // blueprints.txt:105
            public const string WoodBlastFeature = "b8d12c67c67a44c383382b5e62b4460d"; // blueprints.txt:256
            public const string WoodBlastBase = "c7451e98f60f4f4fa46b0a5fedea29c1"; // blueprints.txt:255
            public const string WoodBlastAbility = "97e5b81c049147418ab2faa05c45d19e"; // blueprints.txt:246
            public const string NegativeBlastFeature = "b8d46890ceaa4b6c878d9cce68894ff4"; // blueprints.txt:110
            public const string NegativeBlastBase = "036de5238a1447a293e4e4749b6724eb"; // blueprints.txt:109
            public const string NegativeBlastAbility = "f6319358af7843c180838d223a19d053"; // blueprints.txt:117
            public const string PositiveBlastFeature = "e61724bb0727488f97b84164590846cd"; // blueprints.txt:244
            public const string PositiveBlastBase = "64e7aabc2b55441d8a2513533fff7eb8"; // blueprints.txt:243
            public const string PositiveBlastAbility = "54beb778985445a0a89113ede52b313d"; // blueprints.txt:234
            // 20레벨 같은 원소 복합(설계 §4.3-3 KEE IL: 텔레키네틱 → 역장 Force, 중력·음 → 공허 Void, 나무·양 → 신록 Verdant).
            public const string ForceBlastFeature = "c6e4201d7b674cc78a2c95bae61b9d25"; // blueprints.txt:68
            public const string ForceBlastBase = "8dacff62b4a8413bbfb299458cf94839"; // blueprints.txt:67
            public const string ForceBlastAbility = "ff8b6c23fe4e4b3b816d1819372f2668"; // blueprints.txt:65
            public const string VoidBlastFeature = "69d4bdab76bd4288ba7d06c762403b02"; // blueprints.txt:196
            public const string VoidBlastBase = "ec30ffe2d10543d9a2bb04b11e5b1e3d"; // blueprints.txt:195
            public const string VoidBlastAbility = "26a4c8ea8ee6434caf19bae6e588b721"; // blueprints.txt:184
            public const string VerdantBlastFeature = "46703587324247a08bc45992f77c8b16"; // blueprints.txt:269
            public const string VerdantBlastBase = "815b096429ca401faa7c5979833a37de"; // blueprints.txt:268
            public const string VerdantBlastAbility = "2862a629a1ad4ddb9439d287cbb2a48d"; // blueprints.txt:259
        }

        // 본편 로컬라이즈 문자열 키(블루프린트 GUID 가 아니다 — 팩 키 형식 8-4-4-4-12). 2026-11-16 「폭발」 툴팁 지속 시간 `<null>` 결함 수리.
        // 시험 `tooltip:` 이 blueprints.zip(본편 능력의 Shared.stringkey)·enGB.json(문구)과 대조한다.
        public static class GameText
        {
            // 본편 공용 문구 「1 round」(SharedStringAsset 1f0132abf2df4bf48a1cf996328d7aca 의 stringkey). 폭발 능력이 본뜬 본편 ArcaneAccuracyAbility 의
            // LocalizedDuration 이 이것이다(Classes/Magus/Arcanas/ArcaneAccuracyAbility.jbp 직독 2026-11-16; 본편 94개 블루프린트가 쓴다). BlastStrike.DurationRounds = 1 과 짝.
            public const string DurationOneRound = "62761fbe-42ce-4474-ae10-14f9024f4c57";
             
            // (SharedStringAsset dd8f8acb88d6a6b4683826735f72d605; KiQuiveringPalm·PowerWordKill·PrismaticSpray 등 13개의 LocalizedDuration). 바꾸려면 BlastStrike.ConfigureAbility 한 줄.
        }

         
        // 점검 단계의 Act 가 처음 필요할 때 만든다(점검 모드가 아니면 만들지 않는다). 발급 2026-11-08 uuid4.
        public static class Checks
        {
            public const string DrMagicBuff = "fd424eb7c3d04342a4d9bbeeac9e8cf6"; // AddDamageResistancePhysical{ Value 10, BypassedByMagic } — S13 apply-dr-magic
            public const string SrBuff = "9275ab1cba3245e0b48a31b4b71bc03d"; // AddSpellResistance{ Value 99 } — S4 apply-sr-buff
        }
    }
}
