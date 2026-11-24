using System;

namespace WeaponSpiritShaman
{
    public enum BaseItemSource { Standard, StandardDouble, Override }

    public sealed class CategoryRow
    {
        public string Category, TypeGuid, TypePath, ItemGuid, ItemName, SecondGuid;
        // 두 번째 머리 아이템의 m_Type(이중 무기만, 아니면 null). 첫 머리 유형과 다를 수 있다 — 후크 해머 018ad48f·우르그로시 c4e95e3c(blueprints.zip 직독 2026-11-15).
        // 2026-11-15 결함: Icons.Apply 가 두 번째 머리 유형 = TypeGuid 로 가정해 게임 안 BuildAll 이 실패했다.
        public string SecondTypeGuid;
        public bool Double;
        public BaseItemSource Source;
    }

    // [순수] 영병 범주 59행 = CategoryPlan.Choose(blueprints.zip 의 Weapons/Types/ 유형 행 + BlueprintItemWeapon 행) 의 결과를 그대로 적은 표.
    // 왜 표인가: 게임 안에서는 블루프린트 경로(Weapons/Types/…, Special/)를 알 수 없고 25만 블루프린트를 다 읽어 유형·아이템 행을 만들 수 없다
    // (BlueprintsCache 는 GUID→오프셋만 가진다). 그래서 규칙(CategoryPlan)은 오프라인에서 jbp 로 실행하고, 게임은 이 표 한 루프만 돈다(C4).
    // 복제 잠금(C4): 시험 `plan: SpiritCategories.Rows == CategoryPlan.Choose(jbp)` 가 매 실행 jbp 로 다시 계산해 행마다 같아야 한다(다르면 FAIL — 게임 판 변경 시 재생성).
    // 게임 쪽 SpiritBlueprints.Build 는 행마다 유형·아이템 블루프린트를 되읽어 범주·Double·두 번째 머리가 표와 같은지 확인한다(다르면 throw).
    // 생성: scratchpad wss3\cats.py(2026-11-09, blueprints.zip 직독) → 시험이 대조. 행 순서 = 범주 이름순(CategoryPlan 과 같음).
    public static class SpiritCategories
    {
        internal static bool IsAvailable(CategoryRow row, Func<string, bool> sourceExists)
        {
            return row.Category != "SawtoothSabre" || (sourceExists(row.TypeGuid) && sourceExists(row.ItemGuid));
        }

        public static readonly CategoryRow[] Rows =
        {
            new CategoryRow { Category = "Bardiche", TypeGuid = "b1cbf457fd471d148b39ae56667f405a", ItemGuid = "5bfdaaa6416cc604cba121d003db11ef", ItemName = "StandardBardiche", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "BastardSword", TypeGuid = "d2fe2c5516b56f04da1d5ea51ae3ddfe", ItemGuid = "07531989333442348b7d0102b24af236", ItemName = "StandardBastardSword", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Battleaxe", TypeGuid = "2bc77aa47f97de348aefcf03ec8fa43b", ItemGuid = "6080738f7e97b5646980a0efad2da676", ItemName = "StandardBattleaxe", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Club", TypeGuid = "26aa0672af2c7d84ba93bec37758c712", ItemGuid = "07863393521453a4da9e98626531eb5f", ItemName = "StandardClub", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Dagger", TypeGuid = "07cc1a7fceaee5b42b3e43da960fe76d", ItemGuid = "aa514dbf4c3d61f4e9c0738bd4d373cb", ItemName = "StandardDagger", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Dart", TypeGuid = "f415ae950523a7843a74d7780dd551af", ItemGuid = "a20308f698383b744bb459847333d64b", ItemName = "StandardDart", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "DoubleAxe", TypeGuid = "87d76c7534506a546a065731ee6a38cd", ItemGuid = "1d9acaa3c344c1244bcea18095652955", ItemName = "StandardDoubleAxe", Double = true, SecondGuid = "b4fe35bae9ef0f64182a0345007f8eb0", SecondTypeGuid = "87d76c7534506a546a065731ee6a38cd", Source = BaseItemSource.StandardDouble },
            new CategoryRow { Category = "DoubleSword", TypeGuid = "e95efb85a0912a7489be69d5f03a5308", ItemGuid = "b665770f14e49bc49999d7c3c11c1d61", ItemName = "StandardDoubleSword", Double = true, SecondGuid = "83a19b2e36d74f24daa8978e3a029309", SecondTypeGuid = "e95efb85a0912a7489be69d5f03a5308", Source = BaseItemSource.StandardDouble },
            new CategoryRow { Category = "DuelingSword", TypeGuid = "a6f7e3dc443ff114ba68b4648fd33e9f", ItemGuid = "4697667a33a6774489e5265a955675a5", ItemName = "StandardDuelingSword", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "DwarvenWaraxe", TypeGuid = "a6925f5f897801449a648d865637e5a0", ItemGuid = "30711e40771796340ac67172abfd3279", ItemName = "StandardDwarvenWaraxe", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "EarthBreaker", TypeGuid = "54e960775daa4714fa52e0954d8cf862", ItemGuid = "fc47ddc975f1f804bbef320e1c574cd7", ItemName = "StandardEarthBreaker", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "ElvenCurvedBlade", TypeGuid = "b5e6838ad2a62b146b49619bcf9f42aa", ItemGuid = "f58e421cb8b4ed64ba195123df754055", ItemName = "StandardElvenCurvedBlade", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Estoc", TypeGuid = "d516765b3c2904e4a939749526a52a9a", ItemGuid = "4c863a47d69b63647b3b16bbb01d5ba8", ItemName = "StandardEstoc", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Falcata", TypeGuid = "1af5621e2ae551e42bd1dd6744d98639", ItemGuid = "ee28827188e12a64ba75222b37ac8092", ItemName = "StandardFalcata", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Falchion", TypeGuid = "6ddc9acbbb6e40746a6a1671df1f7b47", ItemGuid = "0f0e6834458d01049a408cd304053b1b", ItemName = "StandardFalchion", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Fauchard", TypeGuid = "7a40899c4defec94bb9c291bde74f1a8", ItemGuid = "c91f331cdf7e331468490323a2e1613d", ItemName = "StandardFauchard", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Flail", TypeGuid = "bf1e53f7442ed0c43bf52d3abe55e16a", ItemGuid = "8bdfa4f81bbc7b540919d770095720be", ItemName = "StandardFlail", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Glaive", TypeGuid = "7a14a1b224cd173449cb7ffc77d5f65c", ItemGuid = "f83415c0e7ea1994d8a7f3dec8f5a861", ItemName = "StandardGlaive", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Greataxe", TypeGuid = "e8059a8eac62cd74f9171d748a5ae428", ItemGuid = "6efea466862f014469cec6c3f2b85cb7", ItemName = "StandardGreataxe", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Greatclub", TypeGuid = "1b8c24cd1f9358c48839bb39266468c3", ItemGuid = "c926ffbdccc4d124c8e8dedfe2e6f499", ItemName = "StandardGreatclub", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Greatsword", TypeGuid = "5f824fbb0766a3543bbd6ae50248688f", ItemGuid = "2fff2921851568a4d80ed52f76cccdb6", ItemName = "StandardGreatsword", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Handaxe", TypeGuid = "54e7473ff73910c47812fd39c897dc1a", ItemGuid = "238ac092fad27144c9514f82917fbec9", ItemName = "StandardHandaxe", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "HeavyCrossbow", TypeGuid = "36d0551b8a28587438a47fcbbf53c083", ItemGuid = "19a5092244dcf99478dcd73c974828b1", ItemName = "StandardHeavyCrossbow", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "HeavyFlail", TypeGuid = "8fefb7e0da38b06408f185e29372c703", ItemGuid = "7f7c8e1e4fdd99e438b30ed9622e9e3f", ItemName = "StandardHeavyFlail", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "HeavyMace", TypeGuid = "d5a167f0f0208dd439ec7481e8989e21", ItemGuid = "766689ccb57d9eb4f8fc88e2e85a919f", ItemName = "StandardHeavyMace", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "HeavyPick", TypeGuid = "a492410f3d65f744c892faf09daad84a", ItemGuid = "e7d3a58b4eb3d7e419ab4cc40f283a32", ItemName = "StandardHeavyPick", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "HookedHammer", TypeGuid = "91645b645cf121a479c1fabc5678dcb3", ItemGuid = "8998da2cfe0884f47943bd28823c3a51", ItemName = "StandardGnomeHookedHammer", Double = true, SecondGuid = "e8be459f78e10624596268c316a65a21", SecondTypeGuid = "018ad48ffd3460d47900491656d2ff26", Source = BaseItemSource.StandardDouble },
            new CategoryRow { Category = "Javelin", TypeGuid = "a70cea34b275522458654beb3c53fe3f", ItemGuid = "c73e20a04b9cae74ca0d5affffeebd34", ItemName = "StandardJavelin", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Kama", TypeGuid = "f5872eb0deb3a1b48a36549f8d92c19e", ItemGuid = "46e685e26290d2c468d96439198e6896", ItemName = "StandardKama", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Kukri", TypeGuid = "006ecd4715809b343b7001e859e3ddb2", ItemGuid = "3125ac6c819db9f4697312710699b637", ItemName = "StandardKukri", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "LightCrossbow", TypeGuid = "d525e7a6d8d5aa648a976ac41194b8d0", ItemGuid = "511c97c1ea111444aa186b1a58496664", ItemName = "StandardLightCrossbow", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "LightHammer", TypeGuid = "bbf7d0ea57e4c9349b65c82abc6787b4", ItemGuid = "79e044277b90a05448a71ae3bcaf581a", ItemName = "StandardLightHammer", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "LightMace", TypeGuid = "cf3e703db4b81904e982a66d7b8474ea", ItemGuid = "355659b342d28e641bf29fe6d8084d19", ItemName = "StandardLightMace", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "LightPick", TypeGuid = "da922f4cdb60ef548a2ec0168620568a", ItemGuid = "f20e85bd5bb8dc74785afc129284bcda", ItemName = "StandardLightPick", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Longbow", TypeGuid = "7a1211c05ec2c46428f41e3c0db9423f", ItemGuid = "201f6150321e09048bd59e9b7f558cb0", ItemName = "StandardLongbow", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Longspear", TypeGuid = "fa2dd17cbde7d3f4aa918d467c30516e", ItemGuid = "f28f6031c2908d84d945865a80f67177", ItemName = "StandardLongspear", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Longsword", TypeGuid = "d56c44bc9eb10204c8b386a02c7eed21", ItemGuid = "6fd0a849531617844b195f452661b2cd", ItemName = "StandardLongsword", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Nunchaku", TypeGuid = "4703b4c0922142f4cbe8895c10a47a9f", ItemGuid = "c0ce1d36d1d3ae246a7587bb17296f07", ItemName = "StandardNunchaku", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "PunchingDagger", TypeGuid = "89dc4dc5f386e2049b5e0a5e209a3407", ItemGuid = "43ff56218554d8547840e7659816db5e", ItemName = "StandardPunchingDagger", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Quarterstaff", TypeGuid = "629736dabac7f9f4a819dc854eaed2d6", ItemGuid = "ada85dae8d12eda4bbe6747bb8b5883c", ItemName = "StandardQuarterstaff", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Rapier", TypeGuid = "2ece38f30500f454b8569136221e55b0", ItemGuid = "1546a05eb151d424eb9132832d5511bb", ItemName = "StandardRapier", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Sai", TypeGuid = "0944f411666c7594aa1398a7476ecf7d", ItemGuid = "d60d0d4c78570ef408a0402f9d4313a6", ItemName = "StandardSai", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "SawtoothSabre", TypeGuid = "ef50f200e4ab429b8bcb66a56f886c2f", ItemGuid = "13b9e6657b26404eb0e2a683bb5b6a2c", ItemName = "SawtoothSabrePlus1", Double = false, SecondGuid = null, Source = BaseItemSource.Override },
            new CategoryRow { Category = "Scimitar", TypeGuid = "d9fbec4637d71bd4ebc977628de3daf3", ItemGuid = "5363519e36752d84698e03a86fb33afb", ItemName = "StandardScimitar", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Scythe", TypeGuid = "4eacfc7e152930a45a1a16217c35011c", ItemGuid = "1052a1f7128861942aa0c2ee6078531e", ItemName = "StandardScythe", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Shortbow", TypeGuid = "99ce02fb54639b5439d07c99c55b8542", ItemGuid = "6d40d84e239bdf345b349ff52e3c00a9", ItemName = "StandardShortbow", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Shortspear", TypeGuid = "cf72040b79c99504785976b28d54b2b7", ItemGuid = "926d02c8af0352b46874791d4de9764f", ItemName = "StandardShortspear", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Shortsword", TypeGuid = "a7da36e0e7bb60e42b9f23462ce2f4fc", ItemGuid = "f717b39c351b8b44388c471d4d272f4e", ItemName = "StandardShortsword", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Sickle", TypeGuid = "ec2da496c7936e14c9a28ce616a6b4cd", ItemGuid = "bfe24b51e4d943a42b0976aaee7e1b7c", ItemName = "StandardSickle", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "SlingStaff", TypeGuid = "25da2dc95ed4a6b419608c678f2a9cc3", ItemGuid = "dda1a4f8cbf8ad34ca7845ca17313e86", ItemName = "StandardSlingStaff", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Spear", TypeGuid = "4b289eccefe6d704093201e52eb6d123", ItemGuid = "4abc27631e2894f4b8b70270e31694f1", ItemName = "StandardSpear", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "SpikedHeavyShield", TypeGuid = "a1b85d048fb5003438f34356df938a9f", ItemGuid = "7c8f6712c444cf446a4bd3b8b717cb5c", ItemName = "StandardSpikedHeavyShield", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "SpikedLightShield", TypeGuid = "98a0dc03586a6d04791901c41700e516", ItemGuid = "b12650bdb547d7e499cdc29e913088cb", ItemName = "StandardSpikedLightShield", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Starknife", TypeGuid = "5a939137fc039084580725b2b0845c3f", ItemGuid = "d19662d357d752447a801951b7bec798", ItemName = "StandardStarknife", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "ThrowingAxe", TypeGuid = "ca131c71f4fefcb48b30b5991520e01d", ItemGuid = "b9ed902d07b622b4f8ec223808f754c1", ItemName = "StandardThrowingAxe", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Tongi", TypeGuid = "13fa38737d46c9e4abc7f4d74aaa59c3", ItemGuid = "b47455bac4f039747ad5e4ddc4e981a4", ItemName = "StandardTongi", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Trident", TypeGuid = "6ff66364e0a2c89469c2e52ebb46365e", ItemGuid = "231f325de2b32dd4585707f8d0c87af3", ItemName = "StandardTrident", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
            new CategoryRow { Category = "Urgrosh", TypeGuid = "0ec97c08fdf87e44f8f16ba87b511743", ItemGuid = "c20f347c84b6605479c9a7b28ccb236b", ItemName = "StandardDwarvenUrgrosh", Double = true, SecondGuid = "78e7be71f5e44fb448434217fe2942d1", SecondTypeGuid = "c4e95e3c489721e4382fd8514a522f9d", Source = BaseItemSource.StandardDouble },
            new CategoryRow { Category = "Warhammer", TypeGuid = "fac41e149f49cba4a8e06ce39b41a6fa", ItemGuid = "3f35d5c01e11d564daa59938dec3db4b", ItemName = "StandardWarhammer", Double = false, SecondGuid = null, Source = BaseItemSource.Standard },
        };
    }
}
