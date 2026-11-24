namespace OathboundRevenant
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다, modinfo testSources). 발급: python -I -c "import uuid;print(uuid.uuid4().hex)"
    public static class Guids
    {
         
        public const string Archetype = "72e4b516994d4b8fb9439d13f510ad2c";
        public const string UndeadBodyFeature = "f2fbfe2261d343aea8f59552ef48f202";
         
        public const string OathRegeneration = "77820d7369474e9db3bee9817a5e0f2e";
        public const string SmiteNegative = "939579d0ba6343dca7871910c36e77d7";
        public const string RevenantHands = "ab934e76538f441e9bf99c9826e12fd2";
        public const string GraveyardDread = "56026f7b03614f1e9e2cebd5ee2b33be";
        public const string DeathAura = "7a47b448db97429a93c3041f1c8331d8";
        public const string NegativeChannel = "3462286a58f04944940aa9255f94bba6";
        public const string RevenantHandsMaximize = "a0fe399e19f14e328824865e18992464";
        public const string TalentSelection = "c37e43a484ef43ec9efd3fa8d5018881";
        public const string RevenantLayOnHands = "e93b80e5d52e48d29842a9e8051db064";
        public const string TalentPursuitOath = "0f692f8492094d1ba16234a6054a6b12";
        public const string TalentTerribleGaze = "e95b3e1f900b41d39d9b65faba081af8";
        public const string TalentDeathsTouch = "c34da85f7b1a4e4f85fd130d0c6609bc";
        public const string TalentGhostStride = "c4e6a1ca80a5406ea62e1991509df168";
        public const string TalentDeathsResolve = "2be2d7cf531645d29cadb5d5d1b743bf";
        public const string TalentDenseFear = "f84368fd5dce4714b95001442b4fb25c";
        public const string TalentGraveArmor = "c70ba4c4c0bf4bcc9e7745cc46e5dfe8";
        public const string TalentOathShield = "26a5e1ee546847ef833e7a2daf0f66e5";
        public const string TalentSoulHarvest = "69f296b4a8f64516a5a14fa592b783b4";
        public const string TalentLifeDrain = "fe61f406556247739a4e10532734459d";
        public const string TalentCommandUndead = "544005f90461418ca63929c3179d0893";
        public const string TalentWailOfTheDead = "52256f3dca14467ea3f7ee184e9bf311";
        public const string TalentCursedStrike = "2ba4e8891eee4536a8cd79c62ff824c4";
        public const string TalentIronBones = "3e21e4f753d64ab695946ce85b3cdfba";
        public const string TalentDeathBlade = "6e4e8af307564f199b442875f49076ce";
        public const string TalentEmpoweredChannel = "4f508f703f794d09b2ea2ebf68527807";
        public const string TalentVengefulRiposte = "7358b2a165a24aa8b7a72a1b536ff5af";
        public const string TalentMarkOfDeath = "4c2cfd983de64a8d8d9875dcdffa6f3b";
        public const string TalentUnyieldingOath = "537f9c9eb3994f70afdae9d12c971fa2";
        public const string TalentLastBreath = "1fd8d55b1ae24e939812c3be135d02e9";
         
        public const string SmiteMarkBuff = "9ae8017187fc4f4a8a7b0be79f04b1c7";
        public const string OathRegenSpent = "d49507b8c2f64b959831c40b303f6280";
        public const string LifeDrainSpent = "87353d5f51dd44138941d6cbf4b8c9aa";
        public const string PursuitSlowBuff = "253e7f9e3eac48349daf6abe1d867c28";
        public const string CursedNoHealBuff = "97fa0788d21f4765b63358833da6f832";
         
        public const string ChannelAbility = "01f839a1f05f41a9a8b38625372c3f72";
        public const string ChannelDiceProperty = "457fe6928f30461a83216926e26b4b48";
        public const string DeathsTouchAbility = "f48ac160756c44308e73ecbca01f58ef";
         
        public const string DeathAuraBuff = "edc4267d61c74c2b870246dbc4caa2e1";
        public const string DeathAuraArea = "3f0930d0a71a46dbaae0cf2ddececd76";
        public const string DeathAuraEffect = "311d8ea197cf402eaab33da2fa5f8f6d";
        public const string DenseFearBuff = "342bbb70f57d42df90f1359cc96d294d";
        public const string DenseFearArea = "6a6f2c692caa4c1d817a39b723e808e7";
        public const string DenseFearEffect = "e3b23f6b0167420b9c9e58c3f3795a5b";
        public const string ResolveBuff = "744aaac3f9fd42879104551f52afdb81";
        public const string ResolveArea = "a0d74bae4e2047e5be27aae591ba433e";
        public const string ResolveEffect = "4a8bd98cd96b4bcbbe450ffbba8753b0";
        public const string GazeAbility = "e70a108f32574b51a88246ed7ae166ff";
        public const string GazeResource = "f287e7dc39244ab29c0fc8267d73aad8";
        public const string WailAbility = "da718d7c245643388a44e06b72b31ef1";
        public const string WailResource = "4c20ebd991aa4e1781c5bbe119dd5c9e";
        public const string OathShieldBuff = "1cba1e6124464195aca93e46e3241fe4";
        public const string OathShieldArea = "c4b93124f64a4b8cb0b1bcb9954487a8";
        public const string OathShieldEffect = "e41012824118413f8daa1e9aed5032b4";
         
        public const string CommandUndeadAbility = "b5bad7eff4fc4d5fa2eb4fb7154c2452";
        public const string CommandResource = "c844e4cb8b9d4c318db4665eaa7d6bd7";
        public const string RiposteResource = "6bf31ca221254fc4b10b8c1d6040e102";
        public const string UnyieldingResource = "db3e7e08d26f450ea068d0f4436e3a78";
        public const string LastBreathAbility = "66f3746a043840079e9bf35ece0ae613";
         
        public const string RevenantUndeadMount = "be549ad659b24f1fb0cd21957ff45c08";
        public const string RevenantUndeadMountInner = "a7caa7f2997e46dc8cc132d3c49e80c2";
        public const string RevenantMountHp = "def8aceced5c4b5ea95871b0f5f6dfbc";
        public const string RevenantWeaponBond = "b2349d8dea784aeb96d0c4d7d426ce39";
        public const string RevenantWeaponChoices = "3d22913db51a4187b00665e2c12af118";
        public const string RevenantNecroticEnchant = "4ea50b2b44f34ac28ae90974222e7d3b";
        public const string RevenantVampiricEnchant = "edfcba97e8634da58199091cc0c86c5d";
        public const string RevenantDreadBladeEnchant = "0cb767a4bcc34af6846e884b05afcb51";
        public const string BondNecroticBuff = "370b1f572fa3460f88cdb34d48bba376";
        public const string BondVampiricBuff = "26367460efbf4dc3a8af164b1eae1e57";
        public const string BondGhostTouchBuff = "c89314c54cb4441693db2b1679baad88";
        public const string BondDreadBladeBuff = "c8510a57d15d4331b5f102ae6dcb5a7e";
        public const string BondNecroticChoice = "4b7a52dddede4c5498f2a85d6ffda0d9";
        public const string BondVampiricChoice = "e0e6adc37ad44ca99ef6be026f565447";
        public const string BondGhostTouchChoice = "50ae70e781144cfdb9c0d6ebb50104a8";
        public const string BondDreadBladeChoice = "e8b1d9ea45e245e4bd0787a9bb3d2e9c";

        // 재주 Id → GUID(TalentPlan.Rows 의 Id 와 같은 이름 — 시험이 20행 모두 짝이 있음을 고정).
        public static string Talent(string id)
        {
            switch (id)
            {
                case "PursuitOath": return TalentPursuitOath; case "TerribleGaze": return TalentTerribleGaze; case "DeathsTouch": return TalentDeathsTouch;
                case "GhostStride": return TalentGhostStride; case "DeathsResolve": return TalentDeathsResolve; case "DenseFear": return TalentDenseFear;
                case "GraveArmor": return TalentGraveArmor; case "OathShield": return TalentOathShield; case "SoulHarvest": return TalentSoulHarvest;
                case "LifeDrain": return TalentLifeDrain; case "CommandUndead": return TalentCommandUndead; case "WailOfTheDead": return TalentWailOfTheDead;
                case "CursedStrike": return TalentCursedStrike; case "IronBones": return TalentIronBones; case "DeathBlade": return TalentDeathBlade;
                case "EmpoweredChannel": return TalentEmpoweredChannel; case "VengefulRiposte": return TalentVengefulRiposte; case "MarkOfDeath": return TalentMarkOfDeath;
                case "UnyieldingOath": return TalentUnyieldingOath; case "LastBreath": return TalentLastBreath;
            }
            throw new System.ArgumentException("OathboundRevenant: no GUID for talent " + id);
        }

        // 본편 GUID(blueprints.zip 2026-11-14 대조, 시험 jbp: game guids 가 경로·이름을 고정). 공용 GameGuids 와 같은 값을 두지 않는다.
        public static class Game
        {
            public const string PaladinClass = "bfa11238e7ae3544bbeb4d0b92e897ec";
            public const string PaladinProgression = "fd325cbba872e5f40b618970678db002";
            public const string UndeadType = "734a29b693e9ec346ba2951b27987e33";
            public const string ConstructType = "fd389783027d63343b4a5634bd81645f";
            public const string UndeadImmunities = "8a75eb16bfff86949a4ddcb3dd2f83ae";
            public const string NegativeEnergyAffinity = "d5ee498e19722854198439629c1841a5";
            public const string DivineGrace = "8a5b5e272e5c34e41aa8b4facbb746d3";
            public const string DeitySelection = "59e7a76987fe3b547b9cce045f4db3e4";
            public const string IomedaeFeature = "88d5da04361b16746bf5b65795e0c38c";
            // 2026-11-15 생성 불변식 ③: 시험 유닛 L1 배경 = 「배경 없음」 명시(RevenantTestUnit). 점검은 기술·배경을 읽지 않는다.
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c"; // Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6"; // Backgrounds/BackgroundsSelection/BackgroundNone.jbp — 목록 첫 항목, 효과 없음
            // 점검 입력(S3·S9): 본편 주문·버프.
            public const string CureLightWounds = "47808d23c67033d4bbab86a1070fd62f";
            public const string InflictLightWounds = "e5cb4c4459e437e49a4cd73fde6b9063";
            public const string ChannelPositiveHarm = "279447a6bf2d3544d93a0a39c3b8e91d";
            public const string Bless = "90e59f4a4ada87243b7b3535a06d0638";
            public const string BlessBuff = "87b8c6270ea85c743afc734dfe99afee";
            public const string Heroism = "5ab0d42fb68c9e34abae4921822b9d63";
            public const string HeroismBuff = "87ab2fed7feaaff47b62a3320a57ad8d";
            public const string InspireCourageEffectBuff = "6d6d9e06b76f5204a8b7856c78607d5d";
            public const string Frightened = "f08a7239aa961f34c8301518e71d4cdf";
            public const string Shaken = "25ec6cb6ab1845c48a95f9c20b034220";
            // 본편 Sleep 주문(bb7ecad2…)이 붙이는 수면 버프(이름 Sleep, 기술자 MindAffecting|Sleep — blueprints.zip 2026-11-14).
            public const string SleepBuff = "c9937d7846aa9ae46991e9f298be644a";
            public const string VampireEnergyDrain = "f934e1faf8e987243a62648d0c8032ff";
            // T3: 팔라딘 진행표 제거 대상·안수 묶음·유지 대상(부록 A-d·A-g).
            public const string LayOnHandsFeature = "858a3689c285c844d9e6ce278e686491";
            public const string LayonHandsFact = "a9f54ba0585042c489232f318a7b4295";
            public const string LayOnHandsOthers = "caae1dc6fcf7b37408686971ee27db13";
            public const string LayOnHandsSelf = "8d6073201e5395d458b8251386d72df1";
            public const string LayOnHandsResource = "9dedf41d995ff4446a181f143c3db98c";
            public const string HolyChampion = "eff3b63f744868845a2f511e9929f0de";
            public const string DivineHealth = "41d1d0de15e672349bf4262a5acf06ce";
            public const string AuraOfCourageFeature = "e45ab30f49215054e83b4ea12165409f";
            public const string SelectionMercy = "02b187038a8dce545bb34bbfb346428d";
            public const string ChannelEnergyPaladinFeature = "cb6d55dda5ab906459d18a435994a760";
            public const string SmiteEvilFeature = "3a6db57fce75b0244a6a5819528ddf26";
            public const string SmiteEvilAdditionalUse = "0f5c99ffb9c084545bbbe960b825d137";
            public const string AuraOfResolveFeature = "a28693b24cc412c478b8b85877f2dad2";
            public const string AuraOfJusticeFeature = "9f13fdd044ccb8a439f27417481cb00e";
            public const string AuraOfFaithFeature = "0437f4af5ad49b544bccf48aa7a51319";
            public const string AuraOfRighteousnessFeature = "6bd4a71232014254e80726f3a3756962";
            public const string PaladinDivineBondSelection = "ad7dc4eba7bf92f4aba23f716d7a9ba6";
            public const string PaladinSpellbook = "bce4989b070ce924b986bf346f59e885";
            public const string WarriorOfTheHolyLightArchetype = "cc70a9fdcd8781a4ca2f0e594f066964";
            public const string SmiteEvilAbility = "7bb9eb2042e67bf489ccd1374423cdec";
            public const string SmiteEvilBuff = "b6570b8cbb32eaf4ca8255d0ec3310b0";
            // T5: 본편 팔라딘 양채널(정렬 조건 원형) · 성직자 음채널(기술자 원형).
            public const string ChannelEnergyPaladinHarm = "4937473d1cfd7774a979b625fb833b47";
            public const string ChannelNegativeEnergy = "89df18039ef22174b81052e2e419c728";
            // 점검 입력(살아 있는 아군): 본편 팔라딘 아키타입 중 권장 능력치가 있는 Divine Hunter(PaladinClass.m_Archetypes 1번, blueprints.zip 2026-11-14).
            public const string DivineHunterArchetype = "fec08c1a3187da549abd6b85f27e4432";
            // T7: 본편 언데드 지배(복제 원형).
            public const string CommandUndead = "0b101dd5618591e478f825f0eef155b4";
            // T8: 신성한 유대(부록 A-n).
            public const string PaladinDivineMountProgression = "b82ac7a5661a3044596398a203188c22";
            public const string PaladinDivineMountSelection = "e2f0e0efc9e155e43ba431984429678e";
            public const string AnimalCompanionFeatureHorse = "9dc58b5901677c942854019d1dd98374";
            public const string WeaponBondProgression = "e08a817f475c8794aa56fdd904f43a57";
            public const string WeaponBondFeature = "1c7cdc1605554954f838d85bbdd22d90";
            public const string WeaponBondFlamingChoice = "7902941ef70a0dc44bcfc174d6193386";
            public const string WeaponBondFlamingBuff = "b3d7a8ddf339989478aacd7dd8d97841";
            public const string Necrotic = "bad4134798e182c4487819dce9b43003";
            public const string GhostTouch = "47857e1a5a3ec1a46adf6491b1423b4f";
            public const string UndeadMountFeature = "a46d64cfd32fc3749a5897657247a680";
            public const string WeaponBondSwitchAbility = "7ff088ab58c69854b82ea95c2b0e35b4";
        }

        // 본편 로컬라이즈 문자열 키(블루프린트 GUID 가 아니다 — 팩 키 형식 8-4-4-4-12). 2026-11-16 툴팁 `<null>` 결함 수리(TooltipPlan 이 쓴다).
        // 시험 `tooltip:` 이 blueprints.zip(본편 능력의 Shared.stringkey)·enGB.json(문구)과 대조한다.
        public static class GameText
        {
            // 본편 공용 문구 「Will negates」(SharedStringAsset d19ee0ab49f413c44aebd7a4cffa214f 의 stringkey; 본편 131개 블루프린트의 LocalizedSavingThrow —
            // blueprints.zip 전수 2026-11-16). 공포의 응시·사자의 외침(의지 1회, 실패 시 공포)과 같은 모양의 본편 초자연 공포 능력
            // Units/NPC/Commoners/Ghost/GhostBrain/FrightfulMoan.jbp(의지 무효 → 공포 버프 d4 라운드)가 이 키를 쓴다(직독 2026-11-16).
            public const string SaveWillNegates = "7ac9f1bb-ab14-4d64-8543-4c97a64a71bd";
             
            // 고유 문자열이다(Shared 없음). 본편 FrightfulMoan 은 지속 칸이 빈 키라 우리도 비운다. 본편 CauseFear 는 고유 문구 「1d4 rounds or 1 round」, 내성 「Will partial」
            // (03e30000-0921-4296-a8b4-9566a9777a5d) — 우리 능력은 성공 시 효과가 없어 맞지 않는다.
        }
    }
}
