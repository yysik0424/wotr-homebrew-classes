namespace Golemancer
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다, modinfo testSources). 발급: python -c "import uuid;print(uuid.uuid4().hex)"
    public static class Guids
    {
         
        public const string Archetype = "4a4f91ee1c824fb19b532da2a879ad56";
        public const string OppositionSchoolSelection = "e7617ca389474cc78ae2a573f9e6def9";
        public const string SchoolAbjuration = "8e3a5ade86674badbfd7ab9ff7a1cf80";
        public const string SchoolConjuration = "19ab43dc72614ecb8e223d82c4fadb38";
        public const string SchoolDivination = "5b14ec4664f14cf986c2ffc5a8a59878";
        public const string SchoolEnchantment = "a2b8b8602957493c8c2a3d3795b8b1e8";
        public const string SchoolEvocation = "09f8aeef6a704347af0b3e33a64cbe49";
        public const string SchoolIllusion = "2d8ca13996e040be931974f6dceefc49";
        public const string SchoolNecromancy = "9a03f3e727404b72b411f5b73e74e80d";
        public const string BombHalfProperty = "b877e9837a104306a9fab166a661ee7d";
        public const string BombHalfFeature = "c332d2a14abe48a1a834eaec6e87e1bb";

         
        // 블루프린트는 T4(펫)·T5(합체)가 만든다. 이 GUID 집합이 MaterialCatalog.StatFeatureGuids(= Materials.OwnedStatFacts 판정 원천, 계획 C4 ⑥)다.
        public const string PetStatsWoodT1 = "148c98a994da42dbae47dd4ff7dfd6a6";
        public const string PetStatsStoneT2 = "e3bb24dc52a24f3cbefec61ded8fcc72";
        public const string PetStatsIronT2 = "dd350aedfb684adaa07db423d13bfc0e";
        public const string PetStatsBrassT2 = "e9b5a5793adc41b596f2af285ff50bff";
        public const string PetStatsGoldT3 = "1edd4009045d4055b1470ddd7c97cf7d";
        public const string PetStatsAutumnT3 = "1fa4d8eea9f041f2af359a94e4b6b668";
        public const string PetStatsSummerT3 = "3c892c572d8e4dd8b9ca4e165ac570cd";
        public const string PetStatsAdamantineT4 = "04ab7ff133cb40aaa6fc49cf0d459adf";
        public const string PetStatsAdamantineT5 = "a41e653414754f67a11b7ac29c97c5e6";
        public const string PetStatsGreaterGoldT4 = "261eb8d36d2a4584b8f5ea1b3e95d531";
        public const string PetStatsGreaterGoldT5 = "cd160c93abf34d659094fd55bd2d3a80";
        public const string PetStatsMithralT4 = "9812b495d1b54841adfbefe0024de5aa";
        public const string PetStatsMithralT5 = "5f4727a389974f97a7733be0442b3acf";
        public const string FusionStatsWoodT1 = "8b0e05c3f76540c0bb8a22edd1b1bf68";
        public const string FusionStatsStoneT2 = "55e01f3789ad45779a90ac5f7aed27eb";
        public const string FusionStatsIronT2 = "8cbdfb0e83974315b8a7a059dff232fc";
        public const string FusionStatsBrassT2 = "4d660de71e9845039c86c0789d35b24f";
        public const string FusionStatsGoldT3 = "dbffdec0e6fd4bdd8fd8f3de73c6b44b";
        public const string FusionStatsAutumnT3 = "e1332ac8292a451e93ac889a457ddf90";
        public const string FusionStatsSummerT3 = "c5c4a8795d0d46e6ba5622c679873c0d";
        public const string FusionStatsAdamantineT4 = "5c77068b7b94420daff1739f0707da31";
        public const string FusionStatsAdamantineT5 = "e4ae117d2c964286ae083e3eabc3e84d";
        public const string FusionStatsGreaterGoldT4 = "424ef183d7144f99af3d29a6da70dec3";
        public const string FusionStatsGreaterGoldT5 = "f8d08fcfff0e40a89cb080f0ba68a8e0";
        public const string FusionStatsMithralT4 = "08eeb4b73fa14f228b81beb19f6c0879";
        public const string FusionStatsMithralT5 = "03dca7b59bb64d09bead4bf777972bad";

         
        public const string PetClass = "26f5cd119530423b9ac9eca5bbb136e9";
        public const string PetProgression = "88f7043bfb194a548c2e70ce76e2b3b1";
        public const string GolemUnit = "2f8bf798fbca41d0b2f3006058d8515d";
        public const string GolemFeature = "fccc462c3d84494ebb74094a5be8750a";
        public const string CallGolem = "1b147d25982f4b3ba98302d368735b30";
        public const string DismissGolem = "d4ce46cfb26c447db246a7c69ca0640d";
        public const string GolemLevelProperty = "53c023cacca24ce6ab843e5543943006";
        public const string MaterialWoodFeature = "440bfb3612ee49a384ef1e2fd9e4c34a";
        public const string TauntArea = "5a38e99a8b6a4088af1d0b2ae9c9674b";
        public const string TauntAreaBuff = "d7bd88a01df84e65ae994410d5375759";
        public const string TauntBuff = "98f32ae968134c28b88cb2f98a93af1c";
        public const string TauntMarkerBuff = "df3d80e5298b4931841d46fa27803c87";

         
        //   슬램 무기 13(키마다) · 특기 능력 DC 재지정 복제 4 · 돌 감속 오라(영역 + 영역 버프) · 우리 특성 2(철 불로 수리 · 아다만틴 급소 면역).
        public const string MaterialSelection5 = "aada3760644c47ce8292dde538fa6732";
        public const string MaterialSelection10 = "a82b68f1287c455eb7e82fd39aecffe8";
        public const string MaterialSelection15 = "a14a7c6b38124a45b2639980274e84de";
        public const string MaterialStoneFeature = "fdfc7cd0752c4f91b6ec23c37e0d7728";
        public const string MaterialIronFeature = "9eb759101c16418f839c823809428304";
        public const string MaterialBrassFeature = "6d0637a0d73d4ed687c6b87f262cca92";
        public const string MaterialGoldFeature = "2432bcab7b474889abab5a00a28e9bbb";
        public const string MaterialAutumnFeature = "c2b16cdda3e54318b16e21afc398750d";
        public const string MaterialSummerFeature = "1059a491810f4abca50bf31ca6d9a8ac";
        public const string MaterialAdamantineFeature = "7a731e20d6ca4c529d28e2ec4aed29f8";
        public const string MaterialGreaterGoldFeature = "916774500e5d453fae7d7eeef14d3e2e";
        public const string MaterialMithralFeature = "9c064a1bce0e4d338a512f38108f1268";
        public const string MaterialBuffWoodT1 = "4a942239acdb441b9ca41ad1a162090c";
        public const string MaterialBuffStoneT2 = "46d5c169325d47768a517d8335cd39fc";
        public const string MaterialBuffIronT2 = "adaa1f3e7821443cb02421942b45d366";
        public const string MaterialBuffBrassT2 = "1c7ba99ded6a4a59b863557b7f68d4bd";
        public const string MaterialBuffGoldT3 = "b18a973132e14b59aeb581c50e0800e1";
        public const string MaterialBuffAutumnT3 = "32aed514179e472eb34ab6ad187fadfb";
        public const string MaterialBuffSummerT3 = "c7c20752207b4762965dd61fb13dae67";
        public const string MaterialBuffAdamantineT4 = "80d1ad3963914195b62d38bc1850e282";
        public const string MaterialBuffAdamantineT5 = "05342e97376441f7bbe2f65f4b175478";
        public const string MaterialBuffGreaterGoldT4 = "d1ac6a86fa4e42b488c9252f6281b3ca";
        public const string MaterialBuffGreaterGoldT5 = "a39b8f3956ff444789e697de3e92356f";
        public const string MaterialBuffMithralT4 = "f02b30ac271349668b0b301da2bf053e";
        public const string MaterialBuffMithralT5 = "75dbf64573824a11af6efdaa01fbcdb0";
        public const string SlamWoodT1 = "079d9b37a82c4eb89572f3ad1993dc48";
        public const string SlamStoneT2 = "05590607a1a14b7b99eb3a8e4c3de115";
        public const string SlamIronT2 = "4ab1dde0b0444d02bcf49f0c0b9f78ba";
        public const string SlamBrassT2 = "da448d21b9ea49ea90d46883342635d9";
        public const string SlamGoldT3 = "d129901616e241368a0588527a4fbe4c";
        public const string SlamAutumnT3 = "ca508e922f3349fc9e3700080eaa507b";
        public const string SlamSummerT3 = "ef7f4ca036f24d26b9aa8de7c3a91bbd";
        public const string SlamAdamantineT4 = "51e2570750cc47bba4350cd2e2131c6b";
        public const string SlamAdamantineT5 = "374378240a294960abf347381f550c60";
        public const string SlamGreaterGoldT4 = "d37705013af64a029b4607b27abd5d61";
        public const string SlamGreaterGoldT5 = "c818fce89c81457581f0dc6e354cdfeb";
        public const string SlamMithralT4 = "27cd496f4e994853aff746ee5e211886";
        public const string SlamMithralT5 = "d199d15d5e694dd7ae0f30f3140809a2";
        public const string CloneSplintering = "eec338a0edb646028631f66847bf4b25";
        public const string CloneAutumnAuraBuff = "d7c6d716a68141fabf5fe97836691757";
        public const string CloneSummerAuraBuff = "e85efa54922f42788290968b70c7e2a9";
        public const string SummerEntangleArea = "7edfadb31f1f452daa0c82bf28d348ec";
        public const string CloneGrabFeature = "00cc8f5b904647dfad6d64e19f73b439";
        public const string StoneSlowArea = "c7183ab78f3744b896a110e8fd6e8987";
        public const string StoneSlowAuraBuff = "f609092fd49d411b90fc932179ea74b9";
        public const string FireRepairFeature = "de1aa6d1da1349f6a9b65ec8ceacbbb8";
        public const string PrecisionImmunityFeature = "626efae34dd4444696fe0ddc4c4ceabd";

         
         
        public const string FusionToggle = "b11739e72fc04b1da2ad8667b9919388";
        public const string FusionControlBuff = "b81592787c0248a29869eced84db0591";
        public const string FusionFeature = "a89898e3ad64467082f03f2b24669cad";
        public const string FusionFormWoodT1 = "7f4ca3567883414882b5986b9e1496fe";
        public const string FusionFormStoneT2 = "5e2f0388b9ca4b6c9cec0d8e09914710";
        public const string FusionFormIronT2 = "0b3a546c8560401787cf7f050c9a57e7";
        public const string FusionFormBrassT2 = "689f3632f22d4c719fc64d2c84a2470f";
        public const string FusionFormGoldT3 = "98597d31b70545e9ac8e2fb8eb84ef0f";
        public const string FusionFormAutumnT3 = "49aa42a64075404b89100d5b98430f28";
        public const string FusionFormSummerT3 = "b4138ab5772f44e495e0b3e726e0ed67";
        public const string FusionFormAdamantineT4 = "8d1ab14f1ddd4230958b2f2a2dc9d155";
        public const string FusionFormAdamantineT5 = "2d2fefb514af470f9a03c142381482f0";
        public const string FusionFormGreaterGoldT4 = "1a576f2c01fc400e97322591a89b36d8";
        public const string FusionFormGreaterGoldT5 = "9ce33391c6d54f488daf05e7aadd2bb1";
        public const string FusionFormMithralT4 = "70f627cf9fbd451bbe90d8cbee3015ad";
        public const string FusionFormMithralT5 = "f00a30890b6543e1bb7ce77cdc1c3cda";

         
        public const string RepairFeature = "d1b7e7990dd44185a558b0f3af0e8478";
        public const string RepairAbility = "42becdde62854e5295b7aa8c07deaf2f";
        public const string RepairAbilityL1 = "9565b037f3954a4e95df0345e4900df5";
        public const string RepairAbilityL2 = "321dc84a2b7a4b9b911b1f9b85f5ada4";
        public const string RepairAbilityL3 = "a5f6ba3f8e90417ab0d578b6b8c6128d";
        public const string RepairAbilityL4 = "3b410f8466d44aef95e1e961a85312fd";
        public const string RepairAbilityL5 = "003104c7afa243ac98288a9b7adf144e";
        public const string RepairAbilityL6 = "8a0339a6e65b4cbe8db3d84b8903634a";
        public const string SharedImmunityFeature = "88a6f727bbfd461980c21df8c5f484e1";
        public const string SharedImmunityAdamantine = "d8a24b1002e240f8b25736fa3999b8ec";
        public const string SharedImmunityGreaterGold = "893b8f7d3df1443886546a7be57483ff";
        public const string SharedImmunityMithral = "3e94a8c9bcde47e6b174e6c6f3b458c2";

        // 소유자 결정 2026-11-16 (같은 날 발급): 골렘 폭주 — 특성 1 · 능력 1 + 변형 54(추출물 레벨 6 × (학파 8 + 학파 없음)) · 학파 효과 버프 8 · 폭주 버프 1(골렘술사) ·
        //   추가 슬램 사지 버프 27(키 = 재질 버프 키 × 추가 수, SlamRules.LimbIds — 평소 2회(11레벨부터)와 폭주가 같은 버프 묶음을 쓴다).
        public const string OverdriveFeature = "108c2b771a4c46ee9ecfdbd41540f77f";
        public const string OverdriveAbility = "5e10a7dbea874457af635bbbc78027a7";
        public const string OverdriveL1Abjuration = "02210670ae7840a0bf81d12b802b8cd5";
        public const string OverdriveL1Conjuration = "821434c78cd840f3ac84c20c49b51247";
        public const string OverdriveL1Divination = "2fa7a10054804db1ad8563943b388e82";
        public const string OverdriveL1Enchantment = "69e3f201a41347cd919438d8d5c4eebf";
        public const string OverdriveL1Evocation = "9f67e20394f14037af9c01dbd8b68ca9";
        public const string OverdriveL1Illusion = "5c0d19678abd4f65aa2fa164a24146bf";
        public const string OverdriveL1Necromancy = "890cd1fd59cd404bb2feda51570ad87e";
        public const string OverdriveL1Transmutation = "f23e406419024034bee413af24d01401";
        public const string OverdriveL1None = "16a82e1ec3f147699fa998083124b8f8";
        public const string OverdriveL2Abjuration = "be48d2e0362e4475b1e37972765a2e2c";
        public const string OverdriveL2Conjuration = "221500bd27b943d3a0fadb5cd07266ba";
        public const string OverdriveL2Divination = "f9ed491d644d4d1bade8f66a8b07323e";
        public const string OverdriveL2Enchantment = "ffe5aae626aa4e36a9b2289178d54a3c";
        public const string OverdriveL2Evocation = "55e35785841a4e9fad4635f6da7fca47";
        public const string OverdriveL2Illusion = "dfb5fb3848e147ebae416b0a31ff3388";
        public const string OverdriveL2Necromancy = "e618a20738194ed994d895e850fc78a1";
        public const string OverdriveL2Transmutation = "024c48ada6874656ba0c388b9a9947be";
        public const string OverdriveL2None = "6dbb332be67d405c890adffccffc18fd";
        public const string OverdriveL3Abjuration = "c2658cc23e7d4b56a59d264b08b33eee";
        public const string OverdriveL3Conjuration = "bd4f7ee589044c19bfdf43eab4a9379f";
        public const string OverdriveL3Divination = "1b80f9843e704d9b830451279c2298cb";
        public const string OverdriveL3Enchantment = "af3a53493a0f4848b14d662bfde1a5dc";
        public const string OverdriveL3Evocation = "27f0c02a6a8d41d49f741364b356b574";
        public const string OverdriveL3Illusion = "9e6814442aef47899ca9e65899f639ce";
        public const string OverdriveL3Necromancy = "719069e7854044309d85a9debf6ec87b";
        public const string OverdriveL3Transmutation = "96e6ecf1036346928a4ad1437ea8f310";
        public const string OverdriveL3None = "9eefcc66a18f412dabb83f55fc061f48";
        public const string OverdriveL4Abjuration = "0a31785bbcbe4984b8898ba3db88e5b7";
        public const string OverdriveL4Conjuration = "a936bd90ec234f4888a1eb0b1d319f40";
        public const string OverdriveL4Divination = "8226f36d0c1d4ee883b30cb81d5b9724";
        public const string OverdriveL4Enchantment = "b45700349d4841809c6100328511ff4c";
        public const string OverdriveL4Evocation = "eb19e2ba359c4fe3ae148b6ed2bd45df";
        public const string OverdriveL4Illusion = "c3f86b4e22e9460c9e1d1bda32fcbca6";
        public const string OverdriveL4Necromancy = "9b2f1ace5aed4199921c9a032efa6fe7";
        public const string OverdriveL4Transmutation = "f0695404f5b842d082d5fc0ade4b63c6";
        public const string OverdriveL4None = "5b71da6761f141ff81cab335b85b558d";
        public const string OverdriveL5Abjuration = "4a7dfd95fd314071a14e69d2390ffc3c";
        public const string OverdriveL5Conjuration = "e9ee56297e4a47a2aa2bd81ddbe6e769";
        public const string OverdriveL5Divination = "81e6c434ddeb4562ac7eaafc9c01f8e6";
        public const string OverdriveL5Enchantment = "68eb90035aba4e3db733a2e5fab5dd78";
        public const string OverdriveL5Evocation = "f6e9d3b56c144a0f9b9836e01c8a6b45";
        public const string OverdriveL5Illusion = "3d9ac2fe38534839bc23bb77a120dc9a";
        public const string OverdriveL5Necromancy = "223e871985d74f5d91e2a567ea675448";
        public const string OverdriveL5Transmutation = "8822c39de5ee448da016dc25e77e00f0";
        public const string OverdriveL5None = "180a85bdb4d7484a931f5d688d91c4db";
        public const string OverdriveL6Abjuration = "4035ab4e53164ccb9cf18e1bc62a23d5";
        public const string OverdriveL6Conjuration = "e3a8d54c8f794613bb8afcc4ae35725b";
        public const string OverdriveL6Divination = "965e0faac74946f3b9609712c60ca2e0";
        public const string OverdriveL6Enchantment = "d13164a13e0a476fba82d5d018822824";
        public const string OverdriveL6Evocation = "d0ad298a31474fd5830a1de743340b40";
        public const string OverdriveL6Illusion = "b46148fd070040149ec0be41c9f5fd68";
        public const string OverdriveL6Necromancy = "bcf61274c4e140c9a792e7ffafaa9710";
        public const string OverdriveL6Transmutation = "aa75bb53096942af85cafb89d95fc98a";
        public const string OverdriveL6None = "45a8d278dc4e4b698e9de1f63bd831b1";
        public const string OverdriveEffectAbjuration = "a698073fa83b441e85b1a918e261b98f";
        public const string OverdriveEffectConjuration = "e63006e370e946f19bdc8a6a02633907";
        public const string OverdriveEffectDivination = "980232cdde304c3baa21a9d3b05553e1";
        public const string OverdriveEffectEnchantment = "61eaea706adb4dbabb180c7a71534fa3";
        public const string OverdriveEffectEvocation = "0604397c49a04004b1f7aebf604f369e";
        public const string OverdriveEffectIllusion = "708b7caa55c245a7896ebc86b9e0473b";
        public const string OverdriveEffectNecromancy = "7e125439f4dd4d219ba1de4bbfb45321";
        public const string OverdriveEffectTransmutation = "2e8a3aae7d394cf084a0887c97927acc";
        public const string OverdriveBuff = "a8af20d392bd415eaf1edc92f859a00d";
        public const string SlamLimbsStoneT2x1 = "a5d7d0e7f435496cb0e0057e6fddcea4";
        public const string SlamLimbsIronT2x1 = "b4397ff4603540ac99be698c4697a39f";
        public const string SlamLimbsBrassT2x1 = "107bb4b3d16b4763bcee92d552f2fbbf";
        public const string SlamLimbsGoldT3x1 = "3ba3d173635c4c5b9a06c4efe7d01c40";
        public const string SlamLimbsGoldT3x2 = "5fa952baf04645028de0708fb24e8567";
        public const string SlamLimbsAutumnT3x1 = "c21b109431974f86a8c0a6d86cee6301";
        public const string SlamLimbsAutumnT3x2 = "7cfc6e15c69b4ebabf80ec13e4a70483";
        public const string SlamLimbsSummerT3x1 = "58546443a5f1453ba4e52620bc742465";
        public const string SlamLimbsSummerT3x2 = "e598da906bb24d818dc39dad10df1402";
        public const string SlamLimbsAdamantineT4x1 = "5c8899d4d4764a59830633830f583567";
        public const string SlamLimbsAdamantineT4x2 = "8439ac40f6b4437381a65b78fbc5e6fc";
        public const string SlamLimbsAdamantineT4x3 = "8e3f7c97f43a498ea30b8180bf55c839";
        public const string SlamLimbsGreaterGoldT4x1 = "fa55f8ae24e2430499e42780bb25d436";
        public const string SlamLimbsGreaterGoldT4x2 = "991f025daa624f3bafceedcc1a81a964";
        public const string SlamLimbsGreaterGoldT4x3 = "1ada81dcfa9c4f2c8191c4b897bbdcdd";
        public const string SlamLimbsMithralT4x1 = "639cbeb3a7c34f6ca98f2ae2ee2b6231";
        public const string SlamLimbsMithralT4x2 = "30f1e9a221f44e3790ffbb6d1ff377cb";
        public const string SlamLimbsMithralT4x3 = "3691edbbd87842ff814ec82d6885d6b3";
        public const string SlamLimbsAdamantineT5x1 = "198f069a1cf04497bf6a128238e88c33";
        public const string SlamLimbsAdamantineT5x2 = "25a71baf81914bd9acd3a118ffcb7110";
        public const string SlamLimbsAdamantineT5x3 = "877cdee2fe224bee8b33d702f3e3d161";
        public const string SlamLimbsGreaterGoldT5x1 = "0779f5251e8241c7907deb6187b817fe";
        public const string SlamLimbsGreaterGoldT5x2 = "2958718f0b8e498db3a8b27bfee2597a";
        public const string SlamLimbsGreaterGoldT5x3 = "b83ab3c0f22b4fa1adf6a30088835740";
        public const string SlamLimbsMithralT5x1 = "fb63c04703424a38b096d2c117b02c9b";
        public const string SlamLimbsMithralT5x2 = "b603b69f626346bc914fc4a745822252";
        public const string SlamLimbsMithralT5x3 = "c40ea4e4fa6d4569ac3238d552033a48";

         
        // tests\GolemancerTests.cs GamePaths 가 같은 경로의 jbp AssetId 와 대조한다(이름 집합도 같아야 한다).
        public static class Game
        {
            public const string AlchemistClass = "0937bec61c0dabc468428f496580c721"; // Classes/Alchemist/AlchemistClass.jbp — 기반 직업
            public const string AlchemistProgression = "efd55ff9be2fda34981f5b9c83afe4f1"; // Classes/Alchemist/AlchemistProgression.jbp — 성장표(읽기만)
            public const string PoisonResistance = "c9022272c87bd66429176ce5c597989c"; // Classes/Alchemist/PoisonResistance.jbp — 제거 L2·5·8
            public const string PoisonImmunity = "202af59b918143a4ab7c33d72c8eb6d5"; // Classes/Alchemist/PoisonImmunity.jbp — 제거 L10
            public const string AlchemistBombsFeature = "c59b2f256f5a70a4d896568658315b7d"; // Classes/Alchemist/Bombs/AlchemistBombsFeature.jbp — 유지(랭크 = 주사위)
            public const string AlchemistBombsResource = "1633025edc9d53f4691481b48248edd7"; // Classes/Alchemist/Bombs/AlchemistBombsResource.jbp — 폭탄 절반 대상
            public const string MutagenFeature = "cee8f65448ce71c4b8b8ca13751dd8ea"; // Classes/Alchemist/Mutagens/MutagenFeature.jbp — 유지 L1
            public const string PersistantMutagen = "75ba281feb2b96547a3bfb12ecaff052"; // Classes/Alchemist/PersistantMutagen.jbp — 유지 L14
            public const string DiscoverySelection = "cd86c437488386f438dcc9ae727ea2a6"; // Classes/Alchemist/Discoveries/DiscoverySelection.jbp — 유지(28개)
            public const string GrandDiscoverySelection = "2729af328ab46274394cedc3582d6e98"; // Classes/Alchemist/Discoveries/GrandDiscovery/GrandDiscoverySelection.jbp — 유지 L20
            public const string CognatogenFeature = "e3f460ea61fcc504183c7d6818bbbf7a"; // Classes/Alchemist/Discoveries/Cognatogens/CognatogenFeature.jbp — 차단(첫 고리)
            public const string GreaterCognatogenFeature = "18eb29676492e844eb5a55d1c855ce69"; // Classes/Alchemist/Discoveries/GreaterCognatogens/GreaterCognatogenFeature.jbp — 사슬 2
            public const string GrandCognatogenFeature = "af4a320648eb5724889d6ff6255090b2"; // Classes/Alchemist/Discoveries/GrandCognatogens/GrandCognatogenFeature.jbp — 사슬 3
            public const string ExtraDiscoverySelection = "537965879fc24ad3948aaffa7a1a3a66"; // Feats/DLC5/ExtraDiscoverySelection.jbp — 특기 「추가 발견」(정찰 §2 누수 경로)
            public const string OppositionSchoolSelection = "6c29030e9fea36949877c43a6f94ff31"; // Classes/Wizard/OppositionSchoolSelection.jbp — 선택 값 복사 원본
            // 본편 위저드 반대 학파 특성(AddOppositionSchool 선례, 정찰 §1) — 우리 학파 특성의 아이콘·Groups 복사 원본(읽기만).
            public const string OppositionSchoolAbjuration = "7f8c1b838ff2d2e4f971b42ccdfa0bfd"; // Classes/Wizard/OppositionSchoolAbjuration.jbp
            public const string OppositionSchoolConjuration = "ca4a0d68c0408d74bb83ade784ebeb0d"; // Classes/Wizard/OppositionSchoolConjuration.jbp
            public const string OppositionSchoolDivination = "09595544116fe5349953f939aeba7611"; // Classes/Wizard/OppositionSchoolDivination.jbp
            public const string OppositionSchoolEnchantment = "875fff6feb84f5240bf4375cb497e395"; // Classes/Wizard/OppositionSchoolEnchantment.jbp
            public const string OppositionSchoolEvocation = "c3724cfbe98875f4a9f6d1aabd4011a6"; // Classes/Wizard/OppositionSchoolEvocation.jbp
            public const string OppositionSchoolIllusion = "6750ead44c0c034428c6509c68110375"; // Classes/Wizard/OppositionSchoolIllusion.jbp
            public const string OppositionSchoolNecromancy = "a9bb3dcb2e8d44a49ac36c393c114bd9"; // Classes/Wizard/Necromancy/OppositionSchoolNecromancy.jbp
            // 점검 S4 대조군(T1 Step 0, 컨트롤러 판정 Q1): 발견 선택을 손대지 않고 독 내성·면역 4쌍만 빼는 본편 아키타입. 시험 `jbp: control archetype leaves discoveries` 가 고정.
            public const string ControlArchetype = "063440d550b25524abcb8dc2590d0168"; // Classes/Alchemist/Archetypes/Chirurgeon/ChirurgeonArchetype.jbp
            // 2026-11-15 생성 불변식 ③: 시험 유닛 L1 배경 선택을 엔진 기본값(목록에서 고른 장인 배경 묶음) 대신 「배경 없음」으로 명시(GolemTestUnit.Create). 점검은 기술·배경을 읽지 않는다.
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c"; // Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6"; // Backgrounds/BackgroundsSelection/BackgroundNone.jbp — 목록 첫 항목, 효과 없음

             
            // 재질 유닛 10(비율 입력·프리팹·크기 원천).
            public const string GolemWood = "b02f074f82b26df4ba67d5cb369afdd1"; // Units/Monsters/Golem Wood/CR6_GolemWood.jbp — L1 나무(펫 복제 원형)
            public const string GolemStone = "dfd21dba15fe7dd4f95961ff27d91836"; // Units/Monsters/Golem Stone/CR11_GolemStone.jbp — L5 돌
            public const string GolemIron = "64589573eba29d541b5240c7485e1aec"; // Units/Monsters/Golem Iron/CR13_GolemIron.jbp — L5 철
            public const string GolemBrass = "3641fc50b791437db22d045eb08d89c8"; // Units/Monsters/DLC3_Mobs/DLC3_AnyTir_Golems/DLC3_CR14_GolemBrass.jbp — L5 황동
            public const string GolemGold = "9b65460d19d5f11458b6d15bd27dc85c"; // Units/Monsters/Golem Gold/CR15_GolemGold.jbp — L10 황금
            public const string GolemAutumn = "ce7a339ed253a2a449eb455096b207c1"; // Units/Monsters/Golem Autumn/CR15_GolemAutumn.jbp — L10 가을
            public const string GolemSummer = "4616c995e1c9c7649a952af0a63ca505"; // Units/Monsters/Golem Summer/CR15_GolemSummer.jbp — L10 여름
            public const string GolemAdamantine = "0f31ef3f80dc69a4894542074ac38f19"; // Units/Monsters/Golem Adamantine/CR19_GolemAdamantine.jbp — L15 아다만틴
            public const string GolemGreaterGold = "338ad824744a4d38880c9d6a4ce0c24b"; // Units/Monsters/Golem Gold/CR20_GolemGold.jbp — L15 황금 상위
            public const string GolemMithral = "42cdfa00fddc6f445a9ce4d20a621b42"; // Units/NPC/Commoners/Act_5_HeraldOfTheIvoryLabyrinth/AreshkagalDungeon/AreshkagalBosses/CR24_Areshkagal_GolemMithralMiniBoss.jbp — L15 미스랄
            // 재질별 추가 면역(설계 §4.5 표·§8).
            public const string ImmunityToPetrification = "b625283fc6eb72c47a2fc5e2a3ff9eb4"; // CreatureAbilities/Immunities/ImmunityToPetrification.jbp
            public const string AcidImmunity = "c994f1a0dfce1c54f94420588da61617"; // CreatureAbilities/EnergyImmunity/AcidImmunity.jbp
            public const string ElectricityImmunity = "cd1e5ab641a833c49994aff99db98952"; // CreatureAbilities/EnergyImmunity/ElectricityImmunity.jbp
            public const string FireImmunity = "11ac3433adfa74642a93111624376070"; // CreatureAbilities/EnergyImmunity/FireImmunity.jbp
            public const string BlindnessImmunity = "12b100acb883a5a478560054ceae7f3b"; // Units/Monsters/WildHunt/BlindnessImmunity.jbp
            public const string ImmunityToConfusion = "f429d531259c48e41a2b30c98d4812f9"; // CreatureAbilities/Immunities/ImmunityToConfusion.jbp — 「광기」(설계 §12 Q2 그대로 둠)
            public const string ColdImmunity = "9ae23798a9284e044ad2716a772a410e"; // CreatureAbilities/EnergyImmunity/ColdImmunity.jbp
            public const string FreedomOfMovementBuffPermanent = "235533b62159790499ced35860636bb2"; // Spells/Level4/FreedomOfMovementBuffPermanent.jbp
            public const string ImmunityToCritical = "ced0f4e5d02d5914a9f9ff74acacf26d"; // CreatureAbilities/Immunities/ImmunityToCritical.jbp
            public const string Evasion = "576933720c440aa4d8d42b0c54b77e80"; // Classes/Rogue/Evasion.jbp
            // 재질별 특기 능력(설계 §4.5 표·§8, §12 정정 4 미스랄 빠른 치유 20).
            public const string WoodGolemSplintering = "eba7737aef48d304fb6788d748a2df69"; // Units/Monsters/Golem Wood/WoodGolemBrain/WoodGolemSplintering.jbp
            public const string StoneGolemSlow = "341cd9a1610563d44938145e4b8d5432"; // Units/Monsters/Golem Stone/StoneGolemBrain/StoneGolemSlow.jbp — 오라 재구성 원형
            public const string IronGolemBreathWeapon = "54935b5704b75724db798395c12b2c2e"; // Units/Monsters/Golem Iron/IronGolemBrain/IronGolemBreathWeapon.jbp
            public const string BrassGolemBreathWeapon = "f1a81f0eee121464eb2f37d887b12ca7"; // Units/Monsters/Golem Brass/BrassGolemBrain/BrassGolemBreathWeapon.jbp
            public const string SeeInvisibilitytBuff = "0adecbf63b614e846bfe15c33f34507e"; // Spells/Level2/SeeInvisibilitytBuff.jbp
            public const string PrismaticSurgeMelee = "da3aa56bd3879c24dbcf6248e4840a2e"; // Units/Monsters/Golem Gold/GoldGolemBrain/PrismaticSurgeMelee.jbp
            public const string PrismaticSurgeRanged = "4ba9818b263827049adc102196c65522"; // Units/Monsters/Golem Gold/GoldGolemBrain/PrismaticSurgeRanged.jbp
            public const string GolemAutumnEntangleAuraBuff = "52608768f529ef840933e8e85841d7e0"; // Units/Monsters/Golem Autumn/GolemAutumnEntangleAuraBuff.jbp
            public const string GolemSummerEntangleAuraBuff = "f8ff09d49cdc6a64dbfeca21031bca49"; // Units/Monsters/Golem Summer/GolemSummerEntangleAuraBuff.jbp
            public const string GolemAutumnGrabFeature = "0d1d262d3437ec143b16b47e24317bbc"; // Units/Monsters/Golem Autumn/GolemAutumnGrabFeature.jbp — 가을·여름 공용
            public const string FastHealing5 = "37a5e51e9e3a23049a77ba70b4e7b2d2"; // CreatureAbilities/FastHealing/FastHealing5.jbp — 아다만틴
            public const string FastHealing20 = "f931c7254cb7c274bb976b7164a68c48"; // CreatureAbilities/FastHealing/FastHealing20.jbp — 미스랄(설계 §12 정정 4)
            public const string ImprovedCriticalSlam = "8ba54b2c84a1ffe40b872f6211b27434"; // Feats/ImprovedCritical/ImprovedCriticalSlam.jbp
            public const string CleaveFeature = "d809b6c4ff2aaff4fa70d712a70f7d7b"; // Feats/Cleave/CleaveFeature.jbp
            public const string CombatReflexes = "0f8939ae6f220984e8fb568abbdfba95"; // Feats/CombatReflexes.jbp

             
            public const string ConstructClass = "fd66bdea5c33e5f458e929022322e6bf"; // Classes/Monster/Construct/ConstructClass.jbp — 펫 직업 값 복사 원본(d10, BAB 완전, 내성 Low)
            public const string ConstructProgression = "f71829aa4aab621429aaaa6e88b8a4a6"; // Classes/Monster/Construct/ConstructProgression.jbp — 원본 진행표(L1 ConstructType + 특기 5, 쓰지 않음·스냅샷만)
            public const string ConstructType = "fd389783027d63343b4a5634bd81645f"; // Traits/Races/Common/ConstructType.jbp — 우리 진행표 L1(ConstructHealth + ConstructImmunities)
            public const string ConstructImmunities = "ccf4b37a13f14b728e8e04aca5c8a27c"; // Traits/Races/Common/ConstructImmunities.jbp — 구조물 공통 면역(점검 S7 construct-immunities)
            public const string CharacterBrain = "cf986dd7ba9d4ec46ad8a3a0406d02ae"; // Brain/Companions/Character_Brain.jbp — 펫 두뇌(정찰 §4.1 Triceratops 선례)
            public const string PlayerFaction = "72f240260881111468db610b6c37c099"; // Factions/Player.jbp — 펫 진영(정찰 §4.1)
            // CR6_GolemWood m_AddFacts 5개(처분표 GolemUnit.VanillaFactDisposition — 시험 `jbp: wood facts disposition complete`). WoodGolemSplintering 은 위 T2 상수.
            public const string NaturalArmor6 = "987ba44303e88054c9504cb3083ba0c9"; // CreatureAbilities/NaturalArmor/NaturalArmor6.jbp — 자연 방어(삭제: StatRecipe 단일 공급, T4)
            public const string DRAdamantine5 = "fb88b018013dc8e419150f86540c07f2"; // CreatureAbilities/DamageReduction/DRAdamantine5.jbp — DR(삭제: StatRecipe 단일 공급, T4)
            public const string GolemWoodImmunity = "be8dcb83f4bd3e24185ceb0cea084a06"; // Units/Monsters/Golem Wood/GolemWoodImmunity.jbp — 전 주문 면역(삭제: 설계 §0-5·§4.4)
            public const string GolemAutumnVisual = "40aaecaebce743e48a1d35957583fcc6"; // Units/Monsters/Golem Autumn/GolemAutumnVisual.jbp — FxOnStart 만 있는 시각 버프(유지)
            // 도발 선례 3단(설계 §12 정정 8, 정찰 §13) — 우리 도발은 새로 만든다. 원본은 값 비교·격리 스냅샷용(읽기만).
            public const string TauntPrecedentArea = "989cce4ccae12554083059fd75248b4f"; // Classes/Cavalier/Archetypes/KnightOfTheWall/KnightOfTheWallDefensiveChallengeArea.jbp
            public const string TauntPrecedentAreaBuff = "5e80a7ddcb97d794690dfd83221da482"; // Classes/Cavalier/Archetypes/KnightOfTheWall/KnightOfTheWallDefensiveChallengeAreaBuff.jbp
            public const string TauntPrecedentBuff = "d064c2b40f9b78240a1527bead977df3"; // Classes/Cavalier/Archetypes/KnightOfTheWall/KnightOfTheWallDefensiveChallengeBuff.jbp
            // 합체 토글·버프·특성 아이콘 원천(hotfix 2026-11-14): 본편 m_Icon 이 있는 기사 도전 능력. 도발 선례 버프는 m_Icon = null(설치 빌드 BuildAll 실패 원인).
            public const string FusionIconSource = "9d5d58ff40e39ff4681670463abe99c9"; // Classes/Cavalier/CavalierChallengeAbility.jbp — m_Icon {guid 7f1b2c99…, fileid 21300000}
            // T7 응급 수리 능력·특성 아이콘 원천(m_Icon 있음 — 시험 `jbp: repair icon source has an icon`).
            // 골렘 폭주 능력·특성·버프 아이콘 원천(소유자 결정 2026-11-16, m_Icon 있음 — 시험 icons: jbp).
            public const string OverdriveIconSource = "7af42862f58edcc4cb825862ff2a0d10"; // Classes/Alchemist/Mutagens/MutagenStrength.jbp — m_Icon {guid 8d1328dd…, fileid 21300000}
            public const string ShakenBuff = "25ec6cb6ab1845c48a95f9c20b034220"; // Buffs/Shaken.jbp — 골렘 폭주 환혹 효과가 거는 본편 동요 버프(AddCondition Shaken + 서술자 Shaken·Fear·MindAffecting)
            public const string RepairIconSource = "47808d23c67033d4bbab86a1070fd62f"; // Spells/Level1/CureLightWounds.jbp — m_Icon {guid 97333312…, fileid 21300000}

             
            public const string PlantClass = "9393cc36ea29d084bab7433e3a28d40b"; // Classes/Monster/Plant/PlantClass.jbp — 가을·여름 특기 능력의 원본 DC 직업(복제본은 펫 직업으로 재지정)
            public const string Slam2d6 = "c6d3cd958772be148952c011b3a15452"; // Weapons/Items/Standard/NaturalWeapon/Slam2d6.jbp — CR6_GolemWood 사지 무기(슬램 13 의 값 복사 원형)
            public const string SlowBuff = "0bc608c3f2b548b44b7146b7530613ac"; // Spells/Level3/SlowBuff.jbp — StoneGolemSlow 의 실패 효과(돌 감속 오라가 같은 버프를 건다)
            public const string GolemAutumnAura = "bc1077a5cd1f5d945a6fc5fb3cd2c86a"; // Units/Monsters/Golem Autumn/GolemAutumnAura.jbp — 오라 틀(정찰 §7) · 가을 오라 버프의 영역(점검 S11 DC 관측)
            public const string GolemSummerAura = "034ad7d17998e7046b02be1a34e03517"; // Units/Monsters/Golem Summer/GolemSummerAura.jbp — 여름 오라 버프의 영역(점검 S11 DC 관측)

            // 영역 FX AssetId — 블루프린트 GUID 가 아니다(ProtectiveAuraAreaEffect.jbp "Fx"."AssetId", 정찰 §13 후보·작성자 기본값, 반경은 눈 점검).
            // 중첩 형식이라 GamePaths 경로 대조 목록에 들지 않는다 — 시험 `jbp: taunt fx source` 가 그 jbp 의 Fx.AssetId 와 대조한다.
            public static class Fx
            {
                public const string TauntArea = "bbd6decdae32bce41ae8f06c6c5eb893"; // CreatureAbilities/ProtectiveAura/ProtectiveAuraAreaEffect.jbp Fx.AssetId
            }

            // 유닛 프리팹 AssetId — 블루프린트 GUID 가 아니다(유닛 jbp 의 "Prefab"."AssetId", 합체 Polymorph.m_Prefab 원천, 설계 §8 「골렘 프리팹 AssetId」).
            // 중첩 형식이라 GamePaths 경로 대조 목록에 들지 않는다 — 시험 `jbp: catalog units` 가 재질 유닛마다 이 값과 대조한다.
            public static class Prefabs
            {
                public const string WoodStoneAutumnSummer = "f59e51021e055b1459b0260a76cc4e54"; // CR6_GolemWood·CR11_GolemStone·CR15_GolemAutumn·CR15_GolemSummer 공용
                public const string Iron = "a20c13073b56eb14d923415f8278b466"; // CR13_GolemIron
                public const string Brass = "1ca33c8e546358846877c576672c849a"; // DLC3_CR14_GolemBrass
                public const string Gold = "0ffc222e8178a704cb452ebda8584ec5"; // CR15_GolemGold·CR20_GolemGold 공용
                public const string Adamantine = "c9f3318f6aa6a3a4a9ce476989a07df5"; // CR19_GolemAdamantine
                public const string Mithral = "ca5e63f618799d048aa57785de20c52e"; // CR24_Areshkagal_GolemMithralMiniBoss
            }
        }
    }
}
