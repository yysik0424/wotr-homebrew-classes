namespace PhantomBlade
{
    // 주의: 아래 새 GUID 는 커밋되는 순간 영구 고정이다(세이브 호환). 절대 바꾸거나 재발급하지 말 것.
    // 이 파일은 게임 타입 없이 문자열만 둔다(콘솔 시험이 컴파일한다). 발급: python -c "import uuid;print(uuid.uuid4().hex)"
    public static class Guids
    {
        public const string Archetype = "2dc3194b043e4f2e8210df11602d5d4e";
        public const string StandToggleAbility = "f69d0d47b88044b7893af7aba146f579";
        public const string StandBuff = "664156aa220c4b358354aa91bf79cdd1";
        public const string StandFeature = "5d5665df9427489aad8ca95f5341438f";
        public const string RushFeature3 = "1875ea3d2b30407599f531251df1b3af";
        public const string RushFeature7 = "1c1be51edab04c649f8ec55e3ec2308e";
        public const string RushFeature11 = "170e769093334a33a333a012cdd80768";
        public const string RushFeature15 = "d81d1e484cce41ecbd81040c3a8713f6";
        public const string RangedGuard6 = "979de9bd46624e96b157832d4259c135";
        public const string RangedGuard14 = "142bad522174409b88c4655408a2e3bc";
        // 폐지된 10레벨 「재충전」 특성(소유자 결정 2026-11-16). 지우지 않는다: 이 특성을 가진 캐릭터의 세이브가 이 GUID 의 사실을 참조한다(StandBlueprints 가 효과 없는 숨김 특성으로 만든다).
        public const string RefillFeature10 = "ce340662ff7d43538ab8837fec2b87a8";
        public const string Absorb18Feature = "e7cb5bf3dd7e42cf992ffd75133c21ff";
        public const string FullAttackAbility = "43f07397b3b34211bfd8487d513a9319";
        public const string FullAttackBuff = "6c42e9b0bcd44f17bfbe050b1c5da0a2";
        public const string FullAttackFeature = "8e1bfe903e35482db5065e82a805365c";
        public const string StandPoolResource = "9be2f97a91bf4a899a8a7d14385d3d8b";
        public const string FullAttackResource = "279e05937d0a467e959fa3c74befb43d";
        public const string StandPoolProperty = "f9fe720baa254168a8002e8721b1f94e";
        public const string ChaMin1Property = "5833ff9336774f12b9a9beefedf5b197";
        public const string StandFist1d6 = "a7519f94ca0b44e6a30b8d499552c3e0";
        public const string StandFist1d8 = "260c8382b8724add9a067df24290c37c";
        public const string StandFist2d6 = "fa946c9654014544a662aa2a73330f01";
         
        public const string RushFeature19 = "ec6cf97229c241128438b6c3b9a0396b";
        public const string StandFist1d10 = "2d172ea04b514f179842d3db4d002ce9";
        public const string StandFist2d8 = "066c5050d48d4adabe0d2be7081b68e8";
        public const string StandFist2d10 = "5282bff16f794ae584339a7656941c38";
         
        public const string SpiritShellFeature = "bcd7383934cd45fda7ffb23184736362";
        public const string SpiritUnit = "0ff888c824af4a289905f07e9990c2c4";
         
        public const string SpiritTranslucentFxBuff = "87cd74b679a347cd8321f9652320b82e";
        // 귀신참(설계 §13.2, Task 6c). 2026-11-09 발급.
        public const string GhostSlashAbility = "7b020c7d2a7e4025a6e760f102e19896";
        public const string GhostSlashFeature5 = "27dae103dbe848369c01e795e31d298e";
        public const string GhostSlashFeature10 = "cd7657f477994951ac4f394d06772894";
        public const string GhostSlashFeature15 = "8380bd2b22114482be0dc1ab0b8614ad";
        public const string GhostSlashFeature20 = "ca45e9a4cac7414e8ba3fbc5a8d915de";
        public const string GhostSlashProjectilesProperty = "0e38ea56309e47fbb85a5f47ddc64629";
        // 귀신참 전용 하루 횟수 자원(소유자 결정 2026-11-16, 설계 §14 ③ — 흡수 풀에서 분리, 전력 공격 자원과도 별개). 2026-11-16 발급.
        public const string GhostSlashResource = "5864d816b4c84b73a5e45784a96bb0e1";

         
        // FighterClass 는 여기 두지 않는다: 공용 코드가 읽으므로 WotrClassMod.Common.GameGuids.FighterClass 에 있다(같은 값을 두 곳에 두면 시험이 실패시킨다).
        public static class Game
        {
            public const string FighterProgression = "b50e94b57be32f74892f381ae2a8905a";       // 정찰 §8
            public const string Bravery = "f6388946f9f472f4585591b80e9f2452";                 // 정찰 §8
            public const string ArmorTraining = "3c380607706f209499d951b29d3c44f3";           // 정찰 §8
            public const string ArmorMastery = "ae177f17cfb45264291d4d7c2cb64671";             
            public const string ForceWispTouchWeapon = "6e56b5dce5666ee48b91a9f8463e58ae";    // 정찰 §3 (스탠드 주먹 무기 템플릿)
            public const string ClawType = "d4f7aee36efe0b54e810c9d3407b6ab3";                // 정찰 §3 (자연 무기 타입 후보)
            public const string FighterCoverBuff = "0259871ec3e423c4a90a48257712f407";        // 정찰 §6 (원거리 AC 선례)
            public const string RecursionGuardPrecedent = "9bd149b1bcfd479f8c69129701e55f70"; // 정찰 §1 (참고: 코드 참조 없음, NotExtraAttack 선례)
            public const string FxMantisAspect = "4f2530d8de621d34fa4376dbd0b540a0";          // 정찰 §7 (FX 후보)
            public const string FxWolfAspect = "e63d75d913e1d9f4a9dbd3f67fceba3f";            // 정찰 §7 (FX 후보)
            public const string FxRage = "53c86872d2be80b48afc218af1b204d7";                  // 정찰 §7 (FX 후보)
            // 정찰 §7: FxMantisAspect 를 FxOnStart 로 가진 본편 버프. 스탠드 버프·토글 아이콘을 여기서 빌리고, 빌드 때 이 버프의 FxOnStart 가 FxMantisAspect 와 같은지 검사한다.
            public const string MantisAspectBuff = "2131750aa5ee424ca0a9c60ef382579b";

             
            // 시험 유닛의 주무기(S3·S4·S8 러시 라운드, S6 근접 AC 측정). Weapons/Items/Standard/MartialTwoHanded/StandardGreatsword.jbp
            public const string StandardGreatsword = "2fff2921851568a4d80ed52f76cccdb6";
            // S6 원거리 AC 측정용 임시 무기(IsRanged). Weapons/Items/Standard/MartialRanged/StandardShortbow.jbp
            public const string StandardShortbow = "6d40d84e239bdf345b349ff52e3c00a9";
            // 2026-11-15 생성 불변식 ③: 시험 유닛 L1 배경 = 「배경 없음」 명시(PbTestUnit.Create). 점검은 기술·배경을 읽지 않는다. tests Task6Game 이 jbp 와 대조.
            // Backgrounds/BackgroundsSelection/BackgroundsBaseSelection.jbp
            public const string BackgroundsBaseSelection = "f926dabeee7f8a54db8f2010b323383c";
            // Backgrounds/BackgroundsSelection/BackgroundNone.jbp — 목록 첫 항목, 효과 없음
            public const string BackgroundNone = "7d300f497584d9245ac24c062dce0bd6";
            // S5 dr-hit: 흡수보다 DR 이 먼저인지 보는 고정 DR 5/—(AddDamageResistancePhysical 하나, 우회 없음). CreatureAbilities/DamageReduction/DR5.jbp
            public const string DamageReduction5 = "f4b3dfedcc150d249be42c0d12302b20";

            // 핫픽스 1 (2026-11-08): TabletopTweaks-Base 2.6.18 이 만드는 블루프린트(본편 아님). 그 모드가 없으면 블루프린트도 없으므로 Get 하지 않고
             
            // 값 = <게임>\Mods\TabletopTweaks-Base\UserSettings\Blueprints.json 130행 "ArmorTrainingSelection"(대시 제거). TTT 가 L7·11·15 의 ArmorTraining 자리에 넣는다.
            public const string TttArmorTrainingSelection = "354f1a4426d24ea38718905108f48e72";
            // 값 = 같은 파일 131행 "ArmorTrainingSpeedFeature". TTT 가 ArmorTraining 이 있던 모든 레벨(3·7·11·15)에 더한다(ArmorSpeedPenaltyRemoval).
            public const string TttArmorTrainingSpeedFeature = "1238eb1fb3a946b5868f500f07b974d5";

             
            // 플레이어 진영이면 NPC 고리 표식이 안 붙는다; GhostRider 말과 같은 값). tests\PhantomBladeTests.cs SpiritGame 이 jbp 를 다시 읽는다.
            public const string PlayerFaction = "72f240260881111468db610b6c37c099";

             
            // 본편 투명화(2레벨 주문) 버프 Spells/Level2/InvisibilityBuff.jbp — BuffInvisibility 1개(OnTurnOn IL 0022 AddCondition(Invisible)),
            // blueprints.zip 2026-11-09 대조. tests\PhantomBladeTests.cs Task6Game 이 jbp 와 컴포넌트를 다시 읽는다.
            public const string InvisibilityBuff = "525f980cb29bc2240b93e953974cb325";

            // 귀신참(Task 6c, 2026-11-09 blueprints.zip 대조 — tests\PhantomBladeTests.cs GhostSlashGame 이 jbp 를 다시 읽는다).
            // 광선 무기(RayType = RangedTouch): 본편 작열 광선 Spells/Level2/ScorchingRay.jbp 의 AbilityDeliverProjectile.m_Weapon. Weapons/Types/Special/RayItem.jbp
            public const string RayItem = "f6ef95b1f7bb52b408a5b345a330ffe8";
             
            //   = FX/Spell Level 1/RayOfEnfeeblement00.jbp (BlueprintProjectile, 속도 15.0, View 53279164…, 명중·빗나감 FX e197ea88… — blueprints.zip 2026-11-16 직독).
            //   모습만 빌린다(쇠약 광선의 효과는 가져오지 않는다). 날아가는 View 는 숨기고 명중 FX 만 남긴다(GhostSlashVeil.cs, 설계 §14.7).
            //   종전(2026-11-09) = 마법 화살 FX/Spell Level 1/MagicMissile00.jbp 2e3992d1695960347a7f9bdf8122966f(속도 15.3, 별도 명중 FX 없음) — 상수 삭제.
            public const string RayOfEnfeeblementProjectile = "fe47a7660448bc54289823b07547bbe8";
        }
    }
}
