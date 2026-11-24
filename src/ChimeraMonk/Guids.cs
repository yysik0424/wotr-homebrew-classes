namespace ChimeraMonk
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다, modinfo testSources). 발급: python -c "import uuid;print(uuid.uuid4().hex)"
    // 본편 GUID = Guids.Game(시험 `jbp: game guids` 가 blueprints.zip 경로·AssetId 로 대조), 외부 모드 GUID = Guids.External(시험 `jbp: external guids` 가 DarkCodex DLL·blueprints.txt 로 대조).
    public static class Guids
    {
         
        public const string Archetype = "c072e66adf3947ecbb31955ee6b9864f";

         
        public const string MutationSelection = "bd51830822b84303b4db63261702062c";
        public static class Mutation
        {
            public const string Tail = "5b5d98f4f52441f0932c6a607270f395";
            public const string Bite = "64115006fe6c43d0b4d194ebcddfa997";
            public const string Claw = "effce88850b6487d9cd5d7fbd3f12634";
            public const string Venom = "e9e0dcdbc2894d4abfc682afc4e1f079";
            public const string Scales = "c9e1c34af38247ee9f552a82afd614de";
            public const string Aspect = "096af2c941784cbabf7c64a070c66ba0";      // AspectSelection(중첩 선택)
            public const string Wings = "ea86a77c464646efb7b73311816b8525";
            public const string Breath = "9747062e5cc24dbc8c4fece55890a092";      // BreathElementSelection(중첩 선택)
            public const string ExtraArm = "a4f5b14793544a108cc714d008070afd";
            public const string ExtraLegs = "2f43a07cf9f545f197e8d1cbb2007768";
            // (추가 꼬리 9ce39150… 는 2026-11-17 폐지 — 아래 Guids.Retired.MutationExtraTail 로 옮김. 값은 그대로다.)
            public const string ExtraEyes = "94c1dbec822a403aaafe5921c6afbeed";
            public const string ExtraEars = "f01c1f4e940b475bbe8daf8a33811184";
            public const string ExtraSenses = "a81729e097d84b308d1ce4d5ee682a73";
            public const string ExtraHeart = "54b683b8379a4729a9239b0ef9618be9";
            public const string ExtraLung = "4beb02e030494e44836f48e84eb5bd50";
            // 2026-11-16 발급(소유자 결정 2번): 뿔 — 들이받기 자연 공격.
            public const string Horn = "889fcd4cfe114e699bb98bc9523ad2e3";
            // 2026-11-16 발급(소유자 결정 4번): 부분 변환 II · III(중첩 선택). 이름 = ChimeraRules.AspectStages 의 Id("Aspect" + 단계 번호).
            public const string Aspect2 = "4b2326dde0824983aaddff501216120d";
            public const string Aspect3 = "6610930fa49248a6b9cbe451eb1749da";
        }
        // 폐지된 블루프린트(소유자 결정 2026-11-17: 추가 꼬리 삭제 — 엔진이 꼬리 종류 추가 사지를 캐릭터당 1개로 제한해 공격이 늘지 않았다).
        // 지우지 않는다: 이 변이를 이미 고른 캐릭터의 세이브가 이 GUID 의 사실(특성)을 참조한다. 값은 2026-11-14 발급 당시 그대로이고 절대 바꾸지 않는다.
        //  - MutationExtraTail(옛 Guids.Mutation.ExtraTail): 효과 부품 없는 숨김 특성으로만 만든다(Mutations.BuildRetired). 변이 선택지(Guids.Mutation)에는 없다.
        //  - WeaponExtraTail(옛 Guids.WeaponExtraTail): 무기 복제만 만들고 어느 특성에도 붙이지 않는다(NaturalWeapons.Build).
        //    정상 저장은 엔티티를 끈 뒤 직렬화하고 다시 켜서(SaveManager.SaveRoutine IL_0681 · IL_06AF · IL_06BF TurnOff → … → IL_08FB~0934 TurnOn;
        //    끄면 AddAdditionalLimb.OnTurnOff → UnitBody.RemoveAdditionalLimb 가 사지 무기를 뺀다) 추가 사지 칸이 비지만,
        //    켜진 채 PreSave 가 불리면 엔진이 껐다가 직렬화 전에 다시 켠다(EntityDataBase.PreSave IL_0008~0026 · IL_005E~0062) — 그 경로의 세이브에는
        //    이 무기가 UnitBody.m_AdditionalLimbs / m_UnusedAdditionalLimbs(둘 다 JsonProperty)에 남을 수 있어 블루프린트를 남긴다.
        public static class Retired
        {
            public const string MutationExtraTail = "9ce39150ada849a1afe768c7d68044c6";
            public const string WeaponExtraTail = "d1a0a095f359496f930dbeb61404ba00";
        }
        public static class BreathElement
        {
            public const string Fire = "44b9ebd077d1460f85b15771916a0072";
            public const string Cold = "b7b0b8f5ca6d460d932b18ecaf95403a";
            public const string Acid = "613e893288aa4b94b7314a12246dfa80";
            public const string Electricity = "8d8e849139db4eb9936f2aaf2775bcae";
        }
         
        public const string MutagenResource = "b9bd1916ce8b4170bc30db89f478ae2c";
        public const string MutagenFeature = "09b44fae0c594e318494c46ccd62273d";
        public const string MutagenBuffStrength = "3d42a870706d4e71b1ad4c6cca61ae43";
        public const string MutagenBuffDexterity = "103b31008bc144f09cdb3c74c0f31392";
        public const string MutagenBuffWisdom = "bcd5f09ae97b4c6894bd0138b1aa4157";
        public const string MutagenAbilityStrength = "7cf0e145a8c64c1db28c9e57c6e5e6a3";
        public const string MutagenAbilityDexterity = "53a258cdb9b2408194814803886e65db";
        public const string MutagenAbilityWisdom = "f0cc6caa5c6f40bca3fc10d652b6e910";

         
        public const string WeaponTail = "60b9c263e39a4774907e33b310b551c8";
        public const string WeaponBite = "72c330ee80d44b7a9487d492f08df2d7";
        public const string WeaponClaw = "33f4e4e1990d40539e70ca45b688fa94";
        public const string WeaponArm = "a2efb28941d64e448576f899f68bdca8";
        public const string WeaponHorn = "c107dec86cd84f158aa3c7075485b3b6";      // 2026-11-16 발급: 뿔 변이의 들이받기 무기(Gore1d6 복제)
        public const string BodyFeature = "4dae6ecd14bf46ecbeef4ecfeff3519b";

         
        public static class Boost
        {
            public const string Scales = "62dd13202ac54ca68daa9351a5c98725";
            public const string ExtraLegs = "16dbc211663946168177b39adbf6bbe1";
            public const string ExtraEyes = "82548e4c8ff249ea9936c94d1167f582";
            public const string ExtraEars = "52d24bc635024955b9d3f99fa3bdf50e";
            public const string ExtraSenses = "8935c649ff8f4da3bc72ef0cbd5537cf";
            public const string ExtraHeart = "d25fe4b33cd3495aba1fd4d9631b32de";
            public const string Wings = "12684ee30e204166a8e0cce1b9476cde";
        }

         
        public const string BreathResource = "8e9ac7d3621a46b8b0ab93c345964574";
        public const string BreathSurgeResource = "4f15cdef688a4759901135a68fe0acda";
        public static class BreathAbility
        {
            public const string Fire = "920515b3b7824c8c94e77f584cffc38e";
            public const string Cold = "9eb0164de8904618a9407b6a10d5162d";
            public const string Acid = "def0757e3916438cb89a28b305861e61";
            public const string Electricity = "b2f0c834031c48c28c25d7ac8704c1c0";
        }
        public static class BreathSurgeAbility
        {
            public const string Fire = "1f0982db7b9b47af8ee7233430b2a204";
            public const string Cold = "f8ed913c064c45bcb9dac02e80041921";
            public const string Acid = "11f6fb320fa84281bbdfeb2eeb40c875";
            public const string Electricity = "5aa7da7e5eef4508960dfd089ef87b1e";
        }
        public const string VenomBuff = "ac103df18cbf421398f9f50ed76b2ded";
        public const string VenomSpent = "3cf3dc904df24926b6abd879b0e6c848";
        public const string AspectWolfScentUpgrade = "38df18853b774121986f59a520d3446e";
        public static class AspectAbility
        {
            public const string Bear = "c0960c9475b448eb9dab4d0fe95dada9";
            public const string Boar = "84da4028c9a14ee78cce0821d6a5d48e";
            public const string Dinosaur = "a512344d872f4e039ac2ea726d8cfca0";
            public const string Elephant = "e4dc2854cc854e5da109b6943ed655b0";
            public const string Horse = "78c8505606ee4f4383e323950a8e2fc1";
            public const string Lizard = "5fcd32229ad449a1adcb4b0c6603a79f";
            public const string Spider = "c5bfba2fb57e4610809264548b3a6b9a";
            public const string Tiger = "22e67f1e4aa242ecbc2c5174a0f2b5d3";
            public const string Wolf = "36de3157496649249c5e34a5e43acefd";
            public const string Wolverine = "d667158b23e9452d8872ac77a10724bf";
        }
        public static class AspectBuff
        {
            public const string Bear = "1dd1576c54c94253817959505c257b7e";
            public const string Boar = "6bf76ced2cdf44179789eec3455c711e";
            public const string Dinosaur = "278891d7c53844b0a4f617e6297b90b6";
            public const string Elephant = "0c801798fc7048c7a17ce7e0dbf5f75a";
            public const string Horse = "e0eaf98ce95949d9919ef971b0b09d28";
            public const string Lizard = "a5e1e2bcb3c047b4ab2745f8d7654727";
            public const string Spider = "588cdc0fa99246aabe98f923a251e828";
            public const string Tiger = "6e6918631e104746b13ccfffd990fbc3";
            public const string Wolf = "85b456b1ba4e4755a77aae04c35c4c5e";
            public const string Wolverine = "e3f2a7c06b1c4614b10fd050b008f58f";
        }
        public static class AspectBoost
        {
            public const string Bear = "bdd20da395a14296adbca64af8cde83a";
            public const string Boar = "7e478a6250c44b658dd2b04086447ad4";
            public const string Dinosaur = "89aa9de076724a03b03770099997f4f9";
            public const string Elephant = "9725a2965c4c49adb421212ccef8b95f";
            public const string Horse = "accfd52f40234e3da88b906a5025c3ed";
            public const string Lizard = "bb6650ff44ce40ee9c9761546ce8272c";
            public const string Spider = "f268cef72b1446989addf41d97916eb0";
            public const string Tiger = "888282beb0ea4cdf8d1b4b1c8bd86944";
            public const string Wolf = "90a3fe7de74948739561a0c91a3c2742";
            public const string Wolverine = "f108f1df63534e49853fd8bce47805c0";
        }

        public static class AspectForm
        {
            public const string Bear = "59cfa175800945b0aebd35ac7849c2d0";
            public const string Boar = "9879dfa85a334b8197ddf0f731338322";
            public const string Dinosaur = "0e4e6246fc864d81be51450fd04ec77f";
            public const string Elephant = "f120418d8410466fbdab38dfee5df526";
            public const string Horse = "f148b879ade84e3ebddc0814d50d2d9c";
            public const string Lizard = "e3d7a28760394b4dab2d9d763973e985";
            public const string Spider = "f9c2d585b0014456bfa10813f6e72a2d";
            public const string Tiger = "d699bc998ee04a6fba196ad80f98836f";
            public const string Wolf = "fbac21d802f54543a0c7823db1bdb7db";
            public const string Wolverine = "a9f96408f92e477eae7ec590001a7f91";
        }

        // 2026-11-16 발급(소유자 결정 4번): 부분 변환 II · III 의 형태 선택지 10씩. 형식 이름 = "AspectForm" + 단계 번호(MutationsPlan.AspectStageForms 가 이름으로 찾는다).
        public static class AspectForm2
        {
            public const string Bear = "746030fe98a34b19b7e2f710d1bd2207";
            public const string Boar = "01022cab626341aaadad9f2120e58fc5";
            public const string Dinosaur = "16434beee5d741b1a9d08a91683ed98b";
            public const string Elephant = "5eb5ba2f504a4cfdbb9610935459a346";
            public const string Horse = "ec2407cb838b46dfadce202eb23bff07";
            public const string Lizard = "046da5060a374629b1a3579107c8317f";
            public const string Spider = "5f96258b45b04d9c9fea58929e75e2c5";
            public const string Tiger = "7a487cc2644342888230de06fb4f1f8e";
            public const string Wolf = "c9788b48c35742c28a97ca34a9bd51de";
            public const string Wolverine = "9ceba8f060fa4bb58eb36e2f76d4cdd2";
        }
        public static class AspectForm3
        {
            public const string Bear = "da29f8a626c84995bb1074fe986dcf0b";
            public const string Boar = "991d13d41d4349bcafcfc30cbbf796d2";
            public const string Dinosaur = "8915e80394d1466482ba97688f280c38";
            public const string Elephant = "f772dbede320421b93a07e1792d2e08b";
            public const string Horse = "fde64d687307471fb716c48a928c7362";
            public const string Lizard = "4521ceb4d0d74ad598ac049fe0b4a08a";
            public const string Spider = "5e10ebd1ed0045778595c99179bdd212";
            public const string Tiger = "ec052058bed141e3bf2d2a9d6dbd5dbb";
            public const string Wolf = "01c39206002b4a07b914f990bcbc7406";
            public const string Wolverine = "5b5223467bd04a2bb5c3a6e814a86696";
        }

        public static class Game
        {
            // 몽크(blueprints.zip Classes/Monk/*.jbp AssetId, 2026-11-14 guids.tsv 대조).
            public const string MonkClass = "e8f21e5b58e0569468e420ebea456124";
            public const string MonkProgression = "8a91753b978e3b34b9425419179aafd6";
            public const string ImprovedUnarmedStrike = "7812ad3672a4b9a4fb894ea402095167";
            // 몽크 보너스 특기 선택(MonkProgression LevelEntries: Level1 = 1·2, Level6 = 6, Level10 = 10·14·18 — 계획 Review Focus 2).
            public const string MonkBonusFeatSelectionLevel1 = "ac3b7f5c11bce4e44aeb332f66b75bab";
            public const string MonkBonusFeatSelectionLevel6 = "b993f42cb119b4f40ac423ae76394374";
            public const string MonkBonusFeatSelectionLevel10 = "1051170c612d5b844bfaa817d6f4cfff";
            // 2026-11-15 생성 불변식 ③(TestUnitGuard): 시험 유닛 L1 배경 선택을 엔진 기본값 대신 명시(ChimeraTestUnit.Create). 시험 `jbp: game guids` 가 대조.
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c"; // Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6";           // Backgrounds/BackgroundsSelection/BackgroundNone.jbp — 효과 없음
            // 몽크 핵심(S1 monk-core-kept).
            public const string MonkFlurryOfBlowstUnlock = "fd99770e6bd240a4aab70f7af103e56a";
            public const string MonkACBonusUnlock = "2615c5f87b3d72b42ac0e73b56d895e0";
            public const string MonkUnarmedStrike = "c3fbeb2ffebaaa64aa38ce7a0bb18fb0";
            public const string KiPowerFeature = "e9590244effb4be4f830b1e3fffced13";
            // 소유자 결정 2026-11-16: 명경지수·빠른 이동 제거(대가 없음). 본편 MonkProgression LevelEntries(blueprints.zip 직독 2026-11-16):
            //   3레벨 = MonkFastMovementUnlock 하나(진행표에 한 번만 — 갑옷이 없을 때 MonkNoArmorFeatureUnlock 이 MonkFastMovement 를 준다. 속도는 그 안에서 몽크 레벨 3당 +10 ft),
            //   4레벨 = StillMind 하나(진행표에 한 번만 — 환혹 내성 +2). 레벨은 ChimeraRules.Levels, 제거 표는 LevelPlan.RemovedCoreFeatures.
            public const string MonkFastMovementUnlock = "73ab5b41fa465ee429ad6658368c6629"; // Classes/Monk/MonkFeatureNoArmorUnlocks/MonkFastMovementUnlock.jbp — 진행표 항목(제거 대상)
            public const string MonkFastMovement = "ac18d5741dfb2f541b222e3a416c5942";       // Classes/Monk/MonkFastMovement.jbp — 위 항목이 주는 실제 속도 특성(진행표 항목 아님 — 빌드·점검이 「진행표에 없음」·「유닛에 없음」 을 본다)
            public const string StillMind = "b8933d223d87087418a627e61ea42ae6";              // Classes/Monk/StillMind.jbp — 진행표 항목(제거 대상)
            // T5 자연 무기 복제 원본(Weapons/Items/Standard/NaturalWeapon/*.jbp)과 무기 형식(Weapons/Types/NaturalWeapon/*.jbp). 판정 D3: 추가 팔 = Slam.
            public const string Tail1d6 = "b21cd5b03fbb0f542815580e66f85915";
            public const string Bite1d6 = "a000716f88c969c499a535dadcf09286";
            public const string Claw1d6 = "65eb73689b94d894080d33a768cdf645";
            public const string Slam1d6 = "767e6932882a99c4b8ca95c88d823137";
            public const string TailType = "4ce435468ebd4364997da8cbd0c66133";
            public const string BiteType = "952e30e6cb40b454789a9db6e5f6dd09";
            public const string ClawType = "d4f7aee36efe0b54e810c9d3407b6ab3";
            public const string SlamType = "f18cbcb39a1b35643a8d129b1ec4e716";
             
            public const string Gore1d6 = "daf4ab765feba8548b244e174e7af5be";
            public const string GoreType = "352b738b4ebfd554b909852e7f3b690a";
            // T6: 추가 귀 = 본편 맹목 전투(Feats/BlindFight.jbp) · 날개 = 판정 D2 BuffWingsMutagen(Classes/Basic/Wings/BuffWingsMutagen.jbp).
            public const string BlindFight = "4e219f5894ad0ea4daa0699e28c37b1d";
            public const string BuffWingsMutagen = "e4979934bdb39d842b28bee614606823";
             
            //   EquipmentEntities/Mythic/KEE_Tail_TailMythicDragon_Any.jbp(황금 용 꼬리, 종족·성별 공용 에셋 1개) · EquipmentEntities/Mythic/KEE_Horns_HornsMythicDevilSmall.jbp(데빌 작은 뿔, 종족 × 성별).
            public const string KeeTailMythicDragonAny = "48c63fd0a8fb40a0a689ef1d2230b488";
            public const string KeeHornsMythicDevilSmall = "30ae829c3ca94670816f1627742f1dac";
            // T6 판정 D1 구름 주문 목록(Spells/Level3·5·6 *.jbp) + 음성 대조 본편 Poison 주문(Spells/Level3/Poison.jbp — 목록에 없어야 함).
            public const string StinkingCloud = "68a9e6d7256f1354289a39003a46d826";
            public const string Cloudkill = "548d339ba87ee56459c98e80167bdf10";
            public const string MindFog = "eabf94e4edc6e714cabd96aa69f8b207";
            public const string AcidFog = "dbf99b00cd35d0a4491c6cc9e771b487";
            public const string PoisonSpell = "d797007a142a6c0409a74b064065a15e";
            // 본편 「독」 주문이 거는 버프(Poison.jbp ContextActionApplyBuff m_Buff — 시험 `exec: lung cloud list` 대조). S13 「폐는 독 면역 아님」 양성 확인용.
            public const string PoisonBuff = "ba1ae42c58e228c4da28328ea6b4ae34";
            // 독립 검토 C-1(2026-11-14): 구름 주문은 효과를 「영역」(BlueprintAbilityAreaEffect)이 낸다 — 면역은 영역 단위(AreaEffectImmunity → UnitPartAreaEffectIgnorance,
            //   AreaEffectEntityData.UpdateUnit IL_0098~00AA). 각 주문의 AbilityEffectRunAction 이 소환하는 영역(Spells/Level*/<주문>Area.jbp, 시험 `exec: lung cloud list` 가 주문 → 영역 연결을 대조).
            public const string StinkingCloudArea = "aa2e0a0fe89693f4e9205fd52c5ba3e5";
            public const string CloudkillArea = "6df1ac314d4e6e9418e34470b79f90d8";
            public const string MindFogArea = "fe5102d734382b74586f56980086e5e8";
            public const string AcidFogArea = "f4dc3f53090627945b83f16ebf3146a6";
            // T7 브레스: 능력 필드 원본 = 스케일드 피스트 붉은 용 브레스(Classes/Monk/Archetypes/ScaledFist/DraconicAbilities/Breaths/ScaledFistRedBreathWeaponAbility.jbp).
            // 원뿔 투사체(FX/Spell Level None/*.jbp): 화염·냉기·산 = 스케일드 피스트 원뿔 브레스가 쓰는 것. 전기 원뿔 브레스 투사체는 본편에 없음 → 범용 AirCone30Feet00(시각만, 원장 편차).
            public const string ScaledFistRedBreath = "aec48af6d05577248a91b69ed843e4cf";
            public const string FireConeBreath = "52c3a84f628ddde4dbfb38e4a581b01a";
            public const string ColdConeBreath = "72b45860bdfb81f4284aa005c04594dd";
            public const string AcidConeBreath = "cf6b3f4577782be43bd7f22f288388b2";
            public const string AirCone = "5d395121ef0195c4580fef5363f095ff";
            public const string ShifterAspectResource = "1b096f343ea54ae0a4e3b6cf404bf62d";
            // 시프터 소형 형태(부록 A-b 표, Classes/Shifter/ShifterAspects/*.jbp). 기능 = ShifterAspectSelectionFeature.m_AllFeatures 원소(울버린은 진행표 4fc5f64d…).
            public const string ShifterClass = "a406d6ebea5c46bba3160246be03e96f";
            public const string AspectBear = "73ef2c330ff14e4bb2f5b7300622c552";
            public const string AspectBearAbility = "e66b1350f66949d997e31a57ce638fee";
            public const string AspectBearBuff = "a237792fc2644a4ebc6eefa2d325f181";
            public const string AspectBoar = "9ee4074cbae746ba96463c0b2a562226";
            public const string AspectBoarAbility = "43bc6e6b0f91413095d4f68206598a69";
            public const string AspectBoarBuff = "0fdc579eafbf4fceae649beed8188a5c";
            public const string AspectDinosaur = "b8ba81c46d904603b8fe4d3c361c5d5e";
            public const string AspectDinosaurAbility = "4b399131ebbf485e940ccd3690eab8d3";
            public const string AspectDinosaurBuff = "4a981c46dc60474cad17549a2b9f7f65";
            public const string AspectElephant = "f6d14c560a3d41edbaee68110bd21c62";
            public const string AspectElephantAbility = "1035bab01a9d420dbb070818afb88215";
            public const string AspectElephantBuff = "c4c39e7078224b6caaf3c8a02032b5cb";
            public const string AspectHorse = "e8c3d7a903a6433bbcfb3d70c97451ce";
            public const string AspectHorseAbility = "5107da0ffa3a4b7bac5fd24f87e7852d";
            public const string AspectHorseBuff = "0bdfd34b7e5a4639bf6b716fa2ac0098";
            public const string AspectLizard = "534e48fa35914f1d93f8813ff4213729";
            public const string AspectLizardAbility = "22386071787246c597748ae8378d4bc9";
            public const string AspectLizardBuff = "29c0e16558b043278f061b128b9d180c";
            public const string AspectSpider = "e3c2c68adaa344aca0354750044611a2";
            public const string AspectSpiderAbility = "56af94739fbf401799f6e54b091750e5";
            public const string AspectSpiderBuff = "49e196492d2c4aa588bafffea6db8c43";
            public const string AspectTiger = "ab2bffed0cc2447fae15ee4e0942578f";
            public const string AspectTigerAbility = "958fcd0e2e674c72b97dea83b806eec1";
            public const string AspectTigerBuff = "0555960c7ab8431d86de4e7db0c22160";
            public const string AspectWolf = "ddf5d65742c842d79f74ac0d5cfbedf5";
            public const string AspectWolfAbility = "2ec2c06d00574e6795c48cb65e17cb52";
            public const string AspectWolfBuff = "d61bd26255d4425d929d73e57ef0e6dd";
            public const string AspectWolverine = "4fc5f64d13ce4c0e8258a83ff63c50af";
            public const string AspectWolverineAbility = "28471f7cdf854170a1bdd8facbb2f054";
            public const string AspectWolverineBuff = "e0b35a32cf234381ab13c831106937f1";
            // 형태 참조 그래프 하위(부록 A-h): 늑대 냄새 상위·잎 3 · 울버린 잎 · 말 잎.
            public const string WolfScentUpgrade = "33684fea43334119b2b3ad5343955a0a";
            public const string WolfScent10 = "473ed22925c54c95a3bee6a5143fc637";
            public const string WolfScent20 = "4a6ceec9b2d9496daffc33169221b37b";
            public const string WolfScent30 = "c44c4a70fae549fc8d3e215af393f753";
            public const string Diehard = "86669ce8759f9d7478565db69b8c19ad";
            public const string Endurance = "54ee847996c25cd4ba8773d7b8555174";
        }

        public static class External
        {
            // DarkCodex v1.9.6 Mods\DarkCodex\blueprints.txt 59행 「FeralCombatTrainingFeature edb2546d… BlueprintFeature」(2026-11-14), DarkCodex.Monk::CreateFeralCombatTraining.
            public const string FeralCombatTraining = "edb2546d1215491ca404f8dd1a0c2af3";
            public const string FeralCombatTrainingName = "FeralCombatTrainingFeature";
        }
    }
}
