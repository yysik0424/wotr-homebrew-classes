using System;
using System.Collections.Generic;

namespace WeaponSpiritShaman
{
    // [순수] 스피릿 웨폰 샤먼 규칙 상수와 예산 식(게임 형식 없음 — 콘솔 시험이 컴파일한다, modinfo testSources).
     
     
    public static class SpiritRules
    {
        // 주술사 레벨 범위: ShamanProgression 이 L1~20 (정찰 §1, jbp Classes/Shaman/ShamanProgression.jbp LevelEntries 최대 20).
        public const int MinShamanLevel = 1;
        public const int MaxShamanLevel = 20;

         
        public static int Budget(int shamanLevel)
        {
            if (shamanLevel < MinShamanLevel || shamanLevel > MaxShamanLevel)
                throw new ArgumentOutOfRangeException("shamanLevel", shamanLevel, "SpiritRules.Budget: shaman level must be " + MinShamanLevel + ".." + MaxShamanLevel);
            return shamanLevel / 4 + 1;
        }

        // 본편 표준 강화 블루프린트 최댓값 = Enhancement5 (설계 §4.5-5, §8). 비추는 강화 = Enhancement{min(E, B, 5)}.
        public const int MaxStandardEnhancement = 5;

        // 표준 강화 Enhancement1..5 GUID (설계 §8, jbp Weapons/Enchantments/Enhancement{n}.jbp — 시험 `jbp: enhancement 1..5 guids & costs` 가 대조).
        // 인덱스 = 강화 값(0 은 비어 있음 — EnhancementGuid(0) 은 던진다).
        static readonly string[] EnhancementGuidTable =
        {
            null,
            "d42fc23b92c640846ac137dc26e000d4", // Enhancement1
            "eb2faccc4c9487d43b3575d7e77ff3f5", // Enhancement2
            "80bb8a737579e35498177e1e3c75899b", // Enhancement3
            "783d7d496da6ac44f9511011fc5f1979", // Enhancement4
            "bdba267e951851449af552aa9f9e3992", // Enhancement5
        };

        public static string EnhancementGuid(int value)
        {
            if (value < 1 || value > MaxStandardEnhancement)
                throw new ArgumentOutOfRangeException("value", value, "SpiritRules.EnhancementGuid: standard enhancement is 1.." + MaxStandardEnhancement);
            return EnhancementGuidTable[value];
        }

        public static IList<string> EnhancementGuids { get { return Array.AsReadOnly(EnhancementGuidTable); } }

         
        public const string GhostTouchGuid = "47857e1a5a3ec1a46adf6491b1423b4f";
        public static readonly string[] FixedGuids = { GhostTouchGuid };

        // 마력이 아님(흡수 대상 아님) = Masterwork (계획 T2: WeaponMasterwork.OnEventAboutToTrigger 는 강화 마법 부여가 없을 때만 +1 명중
         
        public const string MasterworkGuid = "6b38844e2bffbac48b63036b66e735be";
        public static readonly string[] NotMagicGuids = { MasterworkGuid };

        // 범주 기준 아이템 예외표: 본편에 마법 부여 없는 Standard* 톱니 세이버가 없다(jbp 전수, 정찰 r1 §4.4 보충) → SawtoothSabrePlus1 의 구조만 쓴다(마법 부여는 교체 — T3).
        public static readonly Dictionary<string, string> CategoryBaseItemOverrides = new Dictionary<string, string>
        {
            { "SawtoothSabre", "13b9e6657b26404eb0e2a683bb5b6a2c" }, // Weapons/Items/TotalPlus1/SawtoothSabrePlus1.jbp
        };

         
         
        public static readonly string[] RuleTypes =
        {
            "WeaponEnhancementBonus", "WeaponEnergyDamageDice", "WeaponEnergyBurst", "WeaponConditionalDamageDice", "WeaponConditionalEnhancementBonus",
            "WeaponCriticalEdgeIncrease", "WeaponCriticalEdgeStackable", "WeaponCriticalMultiplierIncrease", "WeaponDamageAgainstAlignment", "WeaponAlignment",
            "WeaponMaterial", "WeaponReality", "BrilliantEnergy", "MissAgainstFactOwner", "IgnoreResistanceForDamageFromEnchantment",
            "WeaponCritAutoconfirmAgainstAlignment", "WeaponCritAutoconfirmAgainstSize", "IgnoreTargetDREnchantment", "SuppressBane", "WeaponExtraAttack",
        };

        // 보조 형식(값 계산기) — 설계 §4.5-2 보조 목록. 아이템 문맥의 시전자 = 소지자 기준으로 값을 계산한다(ItemEnchantment.CreateContext @0012/0027).
        public static readonly string[] HelperTypes =
        {
            "ContextRankConfig", "ContextSetAbilityParams", "ContextCalculateSharedValue", "ContextCalculateAbilityParams", "ContextCalculateAbilityParamsBasedOnClass",
        };

        // 트리거형(적중 시 발동) — 소유자 결정 2026-11-18 로 Q9(거부)를 뒤집었다: 거울·강신 둘 다 허용, 단 대상 무기에서 발동하지 않는 것은 거부(아래 무기 대조).
        // 세 형식 모두 마법 부여에 붙으면 「그 무기로 한 공격」에서만 돈다: AbstractWeaponTrigger.IsSuitable(RuleAttackWithWeapon) @0027~004B
        // (Fact 가 ItemEnchantment 면 Owner == evt.Weapon), AddInitiatorAttackRollTrigger.CheckConditions @0001~0027, AdditionalDiceOnAttack.CheckCondition(RuleAttackWithWeapon) @0006~002A.
        // 무기 조건(대상 무기와 대조 — 검사기 ComponentFacts · 계획기 TriggerFit): m_WeaponType @00B5~00DA, CheckWeaponCategory @00DC~00FC, CheckWeaponRangeType @00FE~0119
        // (WeaponRangeTypeHelper.IsSuitableWeapon), CheckWeaponGroup @011B~0140, AllNaturalAndUnarmed @0142~0178, CheckWeaponBlueprint @00AA·@0217~022A (Assembly-CSharp IL, 2026-11-18 직독).
        public static readonly string[] TriggerTypes = { "AddInitiatorAttackWithWeaponTrigger", "AddInitiatorAttackRollTrigger", "AdditionalDiceOnAttack" };

        // 트리거 무기 조건 중 대상 무기만으로 판정할 수 없는 것 → 검사기가 TriggerNotPortable 로 거부한다(보수적, 판정 불가 = 허용하지 않음).
        //  DuelistWeapon: IsSuitable @018B~0213 — 무기 하위 범주(9·13) 또는 소지자 DuelingMastery·DamageGrace 항목(소지자 상태). 본편 1건(TriumphOfPainEnchantment).
        //  PhysicalDamageForm: IsSuitable(trigger, RuleDealDamage) @0000~0021 — 피해 묶음의 무기 피해 형태(대상 무기 정보 밖). 본편 0건.
        public static readonly string[] TriggerUnportableFilters = { ComponentFacts.Duelist, ComponentFacts.PhysicalDamageForm };

        // 장비 효과형 — 착용자(아이템 Owner.Wielder)에게 효과를 주는 마법 부여 컴포넌트. 소유자 결정 2026-11-18: 강신에서만 허용(주술사가 착용자), 거울(펫이 착용자)은 거부.
        // 출처(Assembly-CSharp IL, 2026-11-18 직독): AddUnitFeatureEquipment.OnActivate @0001~005F(Owner.Wielder 에 AddFact)·OnDeactivate @0001~001C(RemoveFact),
        //  AddStatBonusEquipment.OnTurnOn @0001~0135(Owner.Wielder.Stats 수정자), AddUnitFactEquipment.OnActivate/OnDeactivate(같은 모양), AddFactContextActions.OnActivate/OnDeactivate @0000~0025
        //  (Activated/Deactivated 를 문맥에서 실행), 나머지 다섯 = ItemEnchantmentComponentDelegate + 착용자 규칙 처리기(형식 머리 — IInitiatorRulebookHandler / IGlobalRulebookHandler).
        // AddSpellbookEquipment 는 넣지 않는다(소유자 결정: 강신에서도 거부 — 주문책을 기간 동안 붙였다 떼는 것) → 목록 밖 = Unabsorbable.
        public static readonly string[] EquipmentTypes =
        {
            "AddUnitFeatureEquipment", "AddStatBonusEquipment", "AddUnitFactEquipment", "AddFactContextActions", "AttackBonusAgainstFactOwnerEquipment",
            "ACBonusAgainstFactOwnerEquipment", "DamageBonusAgainstFactOwnerEquipment", "AddConditionImmunityEquipment", "WeaponRangeTypeAttackBonusEquipment",
        };

        // AddFactContextActions 가 강신에서 허용되는 모양(형식 검사): 강신 인스턴스는 지속이 끝나면 지워지므로(OnDeactivate) Deactivated 가 Activated 의 효과를 거둬야 한다.
        // 허용 = Deactivated 가 비어 있지 않고 그 최상위 동작이 전부 아래 형식, NewRound 동작 0개. 그 밖(Deactivated 없음 = TheTyranyOfMind 의 영구 버프가 남음,
        // Deactivated 에 내성 굴림 = GrimEnding 은 해제 때마다 부작용)은 EquipmentNotPortable. 출처: 소유자 결정 2026-11-18 · 본편 10건 전수(jbp 2026-11-18).
        public static readonly string[] FactActionsCleanupTypes = { "ContextActionRemoveBuff" };

        // 형식으로 가를 수 없어 GUID 로 막는 장비 효과형(소유자 결정 2026-11-18 — 강신에서도 거부). 값 = 사유(블루프린트 이름 · 조사 근거).
        // 조사 = 시험 `exec: equipment block list == jbp closure` 가 blueprints.zip 에서 다시 계산해 이 표와 같은지 본다(새로 생기거나 빠지면 FAIL — C4):
        //  (a) 붙이는 특성(AddUnitFeatureEquipment·AddUnitFactEquipment 등 컴포넌트가 가리키는 사실·버프·능력을 따라간 닫힘) 안에 CheckWeaponBlueprint = true 이고
        //      m_WeaponBlueprint 가 이 마법 부여를 가진 본편 무기 아이템 → 주술사 무기에서는 그 트리거가 돌지 않는다(AbstractWeaponTrigger.IsSuitable @00AA·@0217~022A).
        //  (b) 닫힘 안에 AddFeatureToPet → 주술사의 동물 동료에게 특성을 붙였다 뗀다(부작용).
        public static readonly Dictionary<string, string> EquipmentBlocked = new Dictionary<string, string>
        {
            { "b2cc94d271f3457fa1ad514fe15acffe", "BladeboundBlackBladeWeaponEnchantment: CheckWeaponBlueprint(own item) in BladeboundIndependentEgoBuff, BladeboundLifeDrinkerFeature, Bladebound*MainBuff" },
            { "79c0791367194232b1898310e3cf69e2", "JaggedClawEnchantment: CheckWeaponBlueprint(own item) in JaggedClawFeature" },
            { "80a4d80748a74bd59ad2034dcaa18e73", "MantisFavorEnchantment: CheckWeaponBlueprint(own item) in MantisFavorFeature" },
            { "a09ea5cce75926545a5f076020acdd16", "MightyBlowOfGoodEnchantment: CheckWeaponBlueprint(own item) in MightyBlowOfGoodBuff, MightyBlowOfGoodDamageBuff" },
            { "05c195d03ee442b48a9f1f9a1444ea03", "PrayingKillerEnchantment: CheckWeaponBlueprint(own item) in PrayingKillerFeature" },
            { "29093cf543a94380ab2ec4273d789fe4", "QuickPunishmentEnchantment: CheckWeaponBlueprint(own item) in QuickPunishmentFeature" },
            { "1fff8605ee2447268f00ad9362876749", "RampantCarnageEnchantment: CheckWeaponBlueprint(own item) in RampantCarnageFeature" },
            { "8dca7853d2c84684b8edc5786a0e60cf", "ThatWhoBathesInBloodEnchantment: CheckWeaponBlueprint(own item) in ThatWhoBathesInBloodFeature" },
            { "1b26f03c53eb49009208bfd2dba3d75f", "UltraThinEdgeEnchantment: CheckWeaponBlueprint(own item) in UltraThinEdgeFeature" },
            { "ea4da1b2cf1db1147b9e9974135d43ad", "CallLightningCritical: AddFeatureToPet in ThunderingClaw" },
            { "7ccd077d3db2d3f4fb0452cc0bafa1bc", "FeralChargeEnchantment: AddFeatureToPet in FeralChargeFeature" },
            { "20c5059bea535124e9efcae5106fb092", "FurybornChildEnchantment: AddFeatureToPet in FurybornChildFeature" },
        };

        // 소지자·시전자 의존 형식 — WeaponAngelDamageDice 는 @0012~002E Context.MaybeCaster.HasFact(MaximizeFeature)(R2-03).
        public static readonly string[] WielderTypes = { "IncreaseWeaponEnhancementBonusOnTargetFocus", "IgnoreConcealmentAgainstFactOwner", "WeaponDamageStatReplacement", "WeaponAngelDamageDice" };

        // 값이 true 이면 소지자·시전자 기준이 되는 설정 필드(WeaponConditionalEnhancementBonus.OnEventAboutToTrigger @001B~0030: CheckWielder 면 조건 대상 = 소지자).
        public static readonly string[] WielderFlagNames = { "CheckWielder", "CheckCaster", "CheckInitiator", "OnWielder" };

        // 컴포넌트 안 조건 트리에 이 접두사 형식이 있으면 시전자 상태를 본다.
        public const string CasterConditionPrefix = "ContextConditionCaster";

        // ContextValue.ValueType 중 상수인 값(이것 아닌 값은 소지자 문맥으로 계산된다 → ContextValueDependent).
        public const string SimpleValueType = "Simple";

        public const string EnhancementComponentType = "WeaponEnhancementBonus";

        // ---------------- 영병 유닛·펫 (계획 T3, 설계 §4.4) ----------------
         
        // 시험 `jbp: base unit stats 10 & wolf stats` 가 늑대 jbp 와 대조한다.
        public static readonly int[] SpiritBaseStats = { 13, 15, 15, 2, 12, 6 };
        // 영병 프리팹 = 본편 떠다니는 무기 15종 공통 AssetId(정찰 §3, 설계 §8). 시험이 jbp 15종과 대조.
        public const string SpiritPrefabAssetId = "ed6eacd06e204af489ddf77d639dfeb9";
        // 크기 = 중형, 이동 = 50 피트(정찰 §3 「공통: 중형, 이동 50」). 이름은 게임 Size 열거형 이름.
        public const string SpiritSizeName = "Medium";
        public const int SpiritSpeed = 50;
        // 스피릿 웨폰 초상화(2026-11-17 결함 수리 — 파티 바의 펫 띠 초상화가 흰색). 유닛 초상화 = 우리 BlueprintPortrait(Guids.SpiritPortrait) = 아래 원본 그림 3장(작은·반신·전신) 그대로 +
        // 펫 띠 그림(m_PetEyeImage). 원인: 원본 AnimatedWeapons(264ed688) jbp 에 m_PetEyeImage 가 없다 → PortraitData.get_PetEyePortrait @000A null →
        // PartyCharacterPetVM.<.ctor>b__9_0 @0057 → PartyCharacterPetPCView.<BindViewImplementation>b__10_3 @0007 Image.sprite = null(유니티 Image 는 스프라이트 없으면 흰 사각형).
        public static readonly string SpiritPortraitBaseGuid = Guids.Game.AnimatedWeaponsPortrait;
        // 펫 띠 그림 다이얼(한 곳). 값 = 원본 AnimatedWeapons.jbp 의 m_PortraitImage AssetId(작은 초상화와 같은 그림 — 지금 보이는 그림을 바꾸지 않는다).
        // 선례: 본편 DLC6 BlackSword_Portrait 95faf140(검은 검 펫 BlackBladeIndependentEgo 9a7f8f65 의 초상화)도 m_PetEyeImage = 자기 m_PortraitImage(71f8b06b…). blueprints.zip 2026-11-17 직독.
         
        public static readonly string SpiritPetEyeSpriteAssetId = "eba8589e8bd50c243bb6fe86d2309940";
        // 펫 진행표에서 정확히 1회씩 뺄 3개(설계 §4.4 「AnimalCompanionSlotFeature·AnimalType·Multiattack 제거」, 정찰 §2.3). 순서 = 로그 순서.
        public static readonly string[] RemovedPetFeatures = { Guids.Game.AnimalCompanionSlotFeature, Guids.Game.AnimalType, Guids.Game.Multiattack };
        // 영병 선택지를 얻는 주술사 레벨(설계 §4.3 「1레벨에 무기 범주 1개를 강제로 고른다」, 계획 T3 Files 「Archetype.cs(AddAt(1, 선택지))」).
        public const int SpiritSelectionLevel = 1;
        // 예외표 기준 아이템(SawtoothSabrePlus1 13b9e665, jbp 직독)의 컴포넌트 형식 = 정확히 이 목록 — 시작 무기에 새 인스턴스로 복사한다(DLC 보상 표시 유지).
         
        // 다른 형식이 있으면 SpiritBlueprints 가 던진다. 시험 `jbp: override item components` 가 jbp 와 대조.
        public static readonly string[] OverrideItemComponentTypes = { "DlcCondition" };
        // 영병 펫 AddClassLevels.Selections 중 물기(Bite) 매개변수 항목 = 무기 숙련 집중·향상된 치명타(AnimalCompanionUnit 67c08507 jbp 직독).
         
        public const string PetSelectionBiteCategory = "Bite";
        public static readonly string[] PetBiteSelectionFeatures = { Guids.Game.WeaponFocus, Guids.Game.ImprovedCritical };

        // 스피릿 웨폰 성장 특기(소유자 결정 2026-11-18): 고른 범주로 이 주술사 레벨에 얻는다(전제 조건 무시 — 컴포넌트가 사실을 직접 붙인다).
        // 레벨 = 주술사 레벨(ClassData(ShamanClass).Level — 흡수 B·강신 단계와 같은 셈, MirrorPart.ShamanLevelOf). 펫 레벨은 엔진 표(AddPet.GetPetLevel 의
        // RankToLevelAnimalCompanion, 주인 캐릭터 레벨 상한)라 주술사 레벨과 다르다 — 쓰지 않는다.
        // 값 = 본편 특기의 파이터 전제 레벨과 같은 시점(jbp PrerequisiteClassLevel{FighterClass} Level 4·8·12, blueprints.zip 2026-11-18 직독). 시험 `feat tiers: jbp` 가 대조.
        public const int SpiritWeaponSpecializationLevel = 4;
        public const int SpiritGreaterWeaponFocusLevel = 8;
        public const int SpiritGreaterWeaponSpecializationLevel = 12;
        public static readonly SpiritGrowthFeat[] SpiritGrowthFeats =
        {
            new SpiritGrowthFeat("spec", Guids.Game.WeaponSpecialization, SpiritWeaponSpecializationLevel),
            new SpiritGrowthFeat("gwf", Guids.Game.GreaterWeaponFocus, SpiritGreaterWeaponFocusLevel),
            new SpiritGrowthFeat("gws", Guids.Game.GreaterWeaponSpecialization, SpiritGreaterWeaponSpecializationLevel),
        };

        // 단계 숨김 특성(주술사 쪽 grant · 펫 쪽 petfeat, HideInUI)의 문자열 키 — 둘이 같은 한 쌍을 쓴다(StringsTable). 게임 안 점검 S26 이 이 키로 분류한다(ScenarioPlan 이 이 값을 그대로 쓴다).
        public const string GrowthFeatNameKey = "SpiritGrowthFeat.Name";
        public const string GrowthFeatDescriptionKey = "SpiritGrowthFeat.Description";

        // 단계 하나: Key = 파생 GUID 종류 접미(DerivedGuids.Kinds 의 "grant-<Key>"·"petfeat-<Key>"), FeatGuid = 본편 매개변수형 특기, ShamanLevel = 얻는 주술사 레벨.
        public sealed class SpiritGrowthFeat
        {
            public readonly string Key, FeatGuid;
            public readonly int ShamanLevel;
            public SpiritGrowthFeat(string key, string featGuid, int shamanLevel)
            {
                if (string.IsNullOrEmpty(key)) throw new ArgumentException("SpiritGrowthFeat: key is empty");
                if (string.IsNullOrEmpty(featGuid)) throw new ArgumentException("SpiritGrowthFeat " + key + ": feat GUID is empty");
                if (shamanLevel < MinShamanLevel || shamanLevel > MaxShamanLevel) throw new ArgumentOutOfRangeException("shamanLevel", shamanLevel, "SpiritGrowthFeat " + key + ": shaman level must be " + MinShamanLevel + ".." + MaxShamanLevel);
                Key = key; FeatGuid = featGuid; ShamanLevel = shamanLevel;
            }
            // 주술사 쪽 특성(AddFeatureToPet) · 펫 쪽 특성(AddParametrizedFeatures)의 파생 GUID 종류.
            public string GrantKind { get { return "grant-" + Key; } }
            public string PetKind { get { return "petfeat-" + Key; } }
        }
        // 영병 유닛 복사 원본(AnimalCompanionUnit 67c08507)의 컴포넌트 형식 = 이 순서 그대로(jbp 직독, 정찰 §2.3). 게임 쪽이 형식마다 새 인스턴스를 만든다 —
        // 다른 형식이 있으면(다른 모드가 더함 등) 조용히 버리거나 공유하지 않고 Build 가 던진다. 시험 `jbp: base unit has no locks` 가 jbp 와 대조.
        public static readonly string[] SpiritUnitComponentTypes = { "AddClassLevels", "AllowDyingCondition", "AddResurrectOnRest", "AddFacts" };
        // 영병 비무기 잠금 5칸(설계 §4.4 「Boots·Ring1·Ring2·Glasses·Shirt 만」). 게임 LockEquipmentSlot.SlotType 이름 — 무기 칸(MainHand·OffHand·Weapon1~8)은 잠그지 않는다.
        public static readonly string[] SpiritLockedSlots = { "Boots", "Ring1", "Ring2", "Glasses", "Shirt" };
        // 영병 갑옷 숙련(설계 §14 Q4): 경·중갑, 방패 없음. 게임 ArmorProficiencyGroup 이름.
        public static readonly string[] SpiritArmorProficiencies = { "Light", "Medium" };

         
        public static class Dials
        {



            public static readonly PossessionTier[] PossessionTiers =
            {
                new PossessionTier(5, 1, 3),
                new PossessionTier(10, 2, 5),
                new PossessionTier(15, 3, 7),
                new PossessionTier(20, 4, 10),
            };
             
            // readonly 면 쓰는 곳마다 ldsfld Dials::<이름> 이 남고, 시험 `fix wave: dial consumers` 가 그것을 고정한다(리터럴 30 으로 바꾸면 FAIL).
            public static readonly DialAction PossessionAction = DialAction.Swift;
            public static readonly int PossessionRangeFeet = 30;
            public static readonly int PossessionHexDcBonus = 2;
            // 영병 자연 갑옷 = NaturalArmor5 의 AddStatBonus{AC, NaturalArmor}.Value. 사실은 GUID 로 붙이므로 이 값은 SpiritBlueprints.CheckNaturalArmorDial 이
             
            public static readonly int SpiritNaturalArmor = 5;
            public const string SpiritNaturalArmorGuid = "7661741dbb9604842a642457456fd0e4"; // NaturalArmor5

            // 출처: 소유자 결정 2026-11-16 (스피릿 배러지 / Spirit Barrage — 설계 §16). 사거리 30피트는 새 값을 두지 않고 PossessionRangeFeet 를 그대로 쓴다(강신과 같은 조건).
            public const string SpiritBarrageSource = "소유자 결정 2026-11-16";
            // 얻는 주술사 레벨(소유자 결정 2026-11-16).
            public static readonly int SpiritBarrageLevel = 18;
            // 하루 횟수, 휴식으로 회복(소유자 결정 2026-11-16).
            public static readonly int SpiritBarrageUsesPerDay = 1;
            // 한 번 쓸 때 공격 수 상한 = 예비 칸 수(활성 아닌 세트 3 × 2칸, 소유자 결정 2026-11-16 「최대 6회」). 시험 `barrage: dials` 가 SpiritBarrageRules.SpareSlotCount 와 같음을 본다.
            public static readonly int SpiritBarrageMaxAttacks = 6;
            // 사용 행동(소유자 결정 2026-11-16 「신속 행동」).
            public static readonly DialAction SpiritBarrageAction = DialAction.Swift;

            // 출처: 소유자 결정 2026-11-18 (흡수 범위 확대 — 적중 시 발동형은 B 와 별개로 한 번에 1개만, 레벨 무관, 거울·강신 각각). 발동형 1개도 특수 1칸을 쓴다.
            // 「발동형」 = 검사기 판정 Verdict.Trigger(옛 TriggerNotPortable 에서 새로 허용한 것). 고르는 규칙 = 계획기 원천 순서(세트 번호 오름차순 → 주 → 보조 → 아이템 목록 순)에서
            // 앞 규칙(무기 대조·중복·대상에 있음)을 통과하고 칸이 남은 첫 번째. 그 뒤 발동형은 RejectReason.TriggerCapReached.
            public static readonly int MaxTriggersPerPlan = 1;
        }
    }

    // 강신 행동 종류(게임 UnitCommand.CommandType 과는 T6 에서 대응 — 순수 파일은 게임 형식을 쓰지 않는다).
    public enum DialAction { Swift }

    // 강신 단계 한 행: 이 주술사 레벨부터 하루 횟수·지속 라운드.
    public sealed class PossessionTier
    {
        public readonly int Level, UsesPerDay, Rounds;
        public PossessionTier(int level, int usesPerDay, int rounds) { Level = level; UsesPerDay = usesPerDay; Rounds = rounds; }
    }
}
