using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Golemancer
{
    // [순수] 재질 → 유닛·프리팹·크기·DR 우회·면역·특기 능력·이동 보너스의 단일 표(계획 C4 ④), 버프 키 13, 수치 특성 GUID 26(C4 ⑥), 비율 기대값. 게임 형식 없음.
    // 출처: 설계 §4.5 재질표·본편 유닛 표 · §8 참조 GUID · §12 정정 4(미스랄 빠른 치유 20)·정정 6(DC 직업 재지정 복제)·정정 7(철 불로 수리 = 우리 특성, T4).
    // 특기 능력 Kind 는 jbp 판정(시험 `jbp: catalog units`): 직업 기반 DC 컴포넌트(ContextCalculateAbilityParamsBasedOnClass, 원본 직업 = ConstructClass·PlantClass)가 있으면 CloneRepointDc,
    //   돌 감속은 1회성 능력을 오라로 재구성(NewAura, 정찰 §7), 나머지(숨결 2·프리즘 쇄도 2 = 직업 기반 아님, 면역·특기 특성) = Reference.
    public enum AbilityKind { Reference, CloneRepointDc, NewAura }

    public sealed class SpecialAbility
    {
        public readonly string Guid;
        public readonly AbilityKind Kind;
        // CloneRepointDc 만: R2-08 허용 공유 컴포넌트 형식 이름 목록. **T4 Step 0 fills**(jbp·dnlib 로 확정) — 그 전까지 명시적 null.
        // 빈 배열로 두지 않는다(「공유 0개 허용」이라는 조용한 기본값이 된다). T4 AbilityClone.VerifyShare 가 null 이면 던진다.
        // Reference·NewAura 는 해당 없음 = null(생성자가 강제).
        public readonly string[] SharedComponentKinds;
        public SpecialAbility(string guid, AbilityKind kind, string[] sharedComponentKinds)
        {
            if (string.IsNullOrEmpty(guid)) throw new ArgumentException("SpecialAbility: empty guid");
            if (!Enum.IsDefined(typeof(AbilityKind), kind)) throw new ArgumentException("SpecialAbility " + guid + ": undefined kind " + (int)kind);
            if (kind != AbilityKind.CloneRepointDc && sharedComponentKinds != null) throw new ArgumentException("SpecialAbility " + guid + ": SharedComponentKinds only applies to CloneRepointDc");
            Guid = guid; Kind = kind; SharedComponentKinds = sharedComponentKinds;
        }
    }

    public sealed class Material
    {
        public readonly string Id;
        public readonly int Tier;                 // 단계 색인: 0 = L1(나무), k = Dials.MaterialLevels[k-1] 에서 고르는 재질
        public readonly string UnitGuid, PrefabAssetId, Size;
        public readonly bool DrBypassAdamantine;  // DR 우회 재질 = 아다만틴(true) / 없음 「—」(false) — 설계 §4.5 본편 유닛 표
        public readonly ReadOnlyCollection<string> ImmunityGuids;
        public readonly ReadOnlyCollection<SpecialAbility> SpecialAbilities;
        public readonly int SpeedBonus;           // 펫 원형(나무 30피트) 대비 이동 보너스 — 미스랄 +20(설계 §4.5 「이동 50」, 계획 Global Constraints 「미스랄 이동 +20 은 MaterialCatalog.SpeedBonus」)
        public Material(string id, int tier, string unitGuid, string prefabAssetId, string size, bool drBypassAdamantine, string[] immunityGuids, SpecialAbility[] specialAbilities, int speedBonus)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Material: empty id");
            if (tier < 0 || tier > GolemRules.Dials.MaterialLevels.Count) throw new ArgumentOutOfRangeException("tier", "Material " + id + ": tier " + tier);
            if (string.IsNullOrEmpty(unitGuid) || string.IsNullOrEmpty(prefabAssetId)) throw new ArgumentException("Material " + id + ": unit / prefab missing");
            GolemRules.SizeHpDelta(size); // 알려진 크기가 아니면 여기서 던진다
            if (immunityGuids == null || immunityGuids.Any(string.IsNullOrEmpty)) throw new ArgumentException("Material " + id + ": immunity list missing or has an empty entry");
            if (specialAbilities == null || specialAbilities.Any(a => a == null)) throw new ArgumentException("Material " + id + ": special ability list missing or has a null entry");
            if (speedBonus < 0) throw new ArgumentOutOfRangeException("speedBonus");
            Id = id; Tier = tier; UnitGuid = unitGuid; PrefabAssetId = prefabAssetId; Size = size; DrBypassAdamantine = drBypassAdamantine;
            ImmunityGuids = Array.AsReadOnly((string[])immunityGuids.Clone());
            SpecialAbilities = Array.AsReadOnly((SpecialAbility[])specialAbilities.Clone());
            SpeedBonus = speedBonus;
        }
        public override string ToString() { return Id; }
    }

    public static class MaterialCatalog
    {
        static SpecialAbility Ref(string g) { return new SpecialAbility(g, AbilityKind.Reference, null); }
        static SpecialAbility Clone(string g, string[] sharedKinds)
        {
            if (sharedKinds == null || sharedKinds.Length == 0) throw new ArgumentException("MaterialCatalog: CloneRepointDc " + g + " needs its shared component kinds (T4 Step 0)");
            return new SpecialAbility(g, AbilityKind.CloneRepointDc, (string[])sharedKinds.Clone());
        }
        static SpecialAbility Aura(string g) { return new SpecialAbility(g, AbilityKind.NewAura, null); }

        // CloneRepointDc 복제본이 원본과 공유하는 컴포넌트 형식(R2-08 허용 목록). 교체되는 1개 = DcComponentKind.
        // 출처: 정찰 §20(T4 Step 0, 2026-11-12) — jbp 컴포넌트 직독 + dnlib stfld 검사(실행 중 자기 필드 쓰기 0 = 읽기 전용). 시험 `jbp: shared component kinds` 가 jbp 와 대조.
        public const string DcComponentKind = "ContextCalculateAbilityParamsBasedOnClass";
        static readonly string[] SplinterShared = { "AbilityEffectRunAction", "AbilityTargetsAround" };            // WoodGolemSplintering(능력)
        static readonly string[] AuraShared = { "AddAreaEffect" };                                                // Golem{Autumn,Summer}EntangleAuraBuff(버프)
        static readonly string[] GrabShared = { "AddInitiatorAttackWithWeaponTrigger", "ManeuverBonus", "ContextRankConfig" }; // GolemAutumnGrabFeature(특성)

        public static IReadOnlyList<string> MissingUnits(Func<string, bool> unitExists)
        {
            if (unitExists == null) throw new ArgumentNullException(nameof(unitExists));
            return Materials.Select(m => m.UnitGuid).Distinct().Where(id => !unitExists(id)).ToArray();
        }

        public static readonly ReadOnlyCollection<Material> Materials = Array.AsReadOnly(new[]
        {
            new Material("wood", 0, Guids.Game.GolemWood, Guids.Game.Prefabs.WoodStoneAutumnSummer, "Medium", true, new string[0], new[] { Clone(Guids.Game.WoodGolemSplintering, SplinterShared) }, 0),
            new Material("stone", 1, Guids.Game.GolemStone, Guids.Game.Prefabs.WoodStoneAutumnSummer, "Large", true, new[] { Guids.Game.ImmunityToPetrification, Guids.Game.AcidImmunity }, new[] { Aura(Guids.Game.StoneGolemSlow) }, 0),
            new Material("iron", 1, Guids.Game.GolemIron, Guids.Game.Prefabs.Iron, "Large", true, new[] { Guids.Game.ElectricityImmunity }, new[] { Ref(Guids.Game.IronGolemBreathWeapon) }, 0),
            new Material("brass", 1, Guids.Game.GolemBrass, Guids.Game.Prefabs.Brass, "Large", true, new[] { Guids.Game.FireImmunity }, new[] { Ref(Guids.Game.BrassGolemBreathWeapon), Ref(Guids.Game.SeeInvisibilitytBuff) }, 0),
            new Material("gold", 2, Guids.Game.GolemGold, Guids.Game.Prefabs.Gold, "Large", true, new[] { Guids.Game.BlindnessImmunity, Guids.Game.ImmunityToConfusion }, new[] { Ref(Guids.Game.PrismaticSurgeMelee) }, 0),
            new Material("autumn", 2, Guids.Game.GolemAutumn, Guids.Game.Prefabs.WoodStoneAutumnSummer, "Large", false, new[] { Guids.Game.ColdImmunity, Guids.Game.FreedomOfMovementBuffPermanent }, new[] { Clone(Guids.Game.GolemAutumnEntangleAuraBuff, AuraShared), Clone(Guids.Game.GolemAutumnGrabFeature, GrabShared) }, 0),
            new Material("summer", 2, Guids.Game.GolemSummer, Guids.Game.Prefabs.WoodStoneAutumnSummer, "Large", false, new[] { Guids.Game.FireImmunity, Guids.Game.FreedomOfMovementBuffPermanent }, new[] { Clone(Guids.Game.GolemSummerEntangleAuraBuff, AuraShared), Clone(Guids.Game.GolemAutumnGrabFeature, GrabShared) }, 0),
            new Material("adamantine", 3, Guids.Game.GolemAdamantine, Guids.Game.Prefabs.Adamantine, "Huge", false, new[] { Guids.Game.ImmunityToCritical }, new[] { Ref(Guids.Game.FastHealing5), Ref(Guids.Game.ImprovedCriticalSlam) }, 0),
            new Material("greatergold", 3, Guids.Game.GolemGreaterGold, Guids.Game.Prefabs.Gold, "Large", true, new[] { Guids.Game.ImmunityToConfusion, Guids.Game.ImmunityToPetrification }, new[] { Ref(Guids.Game.PrismaticSurgeRanged), Ref(Guids.Game.CleaveFeature) }, 0),
            new Material("mithral", 3, Guids.Game.GolemMithral, Guids.Game.Prefabs.Mithral, "Large", false, new[] { Guids.Game.ElectricityImmunity, Guids.Game.Evasion }, new[] { Ref(Guids.Game.CombatReflexes), Ref(Guids.Game.FastHealing20) }, 20),
        });

        // 설계 §4.5 「상대 비율」 표 = MaterialRatios.Compute(본편 유닛 10, ToEven) 의 기대값(시험 `exec: ratios` 와 T4 빌드가 대조, C4 ②).
        public static readonly ReadOnlyCollection<Ratio> ExpectedRatios = Array.AsReadOnly(new[]
        {
            new Ratio("wood", 1.00m, 1.00m, 1.00m, 1.00m),
            new Ratio("stone", 0.90m, 0.75m, 0.97m, 0.91m),
            new Ratio("iron", 1.00m, 1.12m, 0.97m, 1.04m),
            new Ratio("brass", 1.10m, 1.12m, 1.06m, 1.04m),
            new Ratio("gold", 0.89m, 1.29m, 1.35m, 1.13m),
            new Ratio("autumn", 1.05m, 0.86m, 0.82m, 0.93m),
            new Ratio("summer", 1.05m, 0.86m, 0.82m, 0.93m),
            new Ratio("adamantine", 0.93m, 1.00m, 0.88m, 0.95m),
            new Ratio("greatergold", 0.79m, 1.00m, 0.96m, 1.00m),
            new Ratio("mithral", 1.29m, 1.00m, 1.17m, 1.05m),
        });

        public static Material Get(string id)
        {
            if (id == null) throw new ArgumentNullException("id");
            var m = Materials.SingleOrDefault(x => x.Id == id);
            if (m == null) throw new KeyNotFoundException("MaterialCatalog: no material '" + id + "'");
            return m;
        }

        public static Ratio RatioOf(Material m)
        {
            if (m == null) throw new ArgumentNullException("m");
            var r = ExpectedRatios.SingleOrDefault(x => x.Material == m.Id);
            if (r == null) throw new KeyNotFoundException("MaterialCatalog: no expected ratio for '" + m.Id + "'");
            return r;
        }

        public static string PrefabOf(Material m)
        {
            if (m == null) throw new ArgumentNullException("m");
            return m.PrefabAssetId;
        }

        // 고를 수 있는 재질(나무 제외).
        public static int Selectable { get { return Materials.Count(m => m.Tier > 0); } }

        // 선택 레벨(Dials.MaterialLevels)에서 고를 수 있는 재질 수. 선택 레벨이 아니면 throw.
        public static int SelectableAt(int level)
        {
            int k = GolemRules.Dials.MaterialLevels.IndexOf(level);
            if (k < 0) throw new ArgumentOutOfRangeException("level", "MaterialCatalog.SelectableAt: " + level + " is not a material level (" + string.Join(",", GolemRules.Dials.MaterialLevels) + ")");
            return Materials.Count(m => m.Tier == k + 1);
        }

        // 단계의 합법 레벨: 나무 MinLevel..(첫 재질 레벨 − 1) · 단계 k = MaterialLevels[k-1]..(다음 경계 − 1, 마지막은 MaxLevel).
        public static int[] LegalLevels(Material m)
        {
            if (m == null) throw new ArgumentNullException("m");
            var ml = GolemRules.Dials.MaterialLevels;
            int lo = m.Tier == 0 ? GolemRules.MinLevel : ml[m.Tier - 1];
            int hi = m.Tier == ml.Count ? GolemRules.MaxLevel : ml[m.Tier] - 1;
            return Enumerable.Range(lo, hi - lo + 1).ToArray();
        }

        public static void RequireLegal(Material m, int level)
        {
            if (m == null) throw new ArgumentNullException("m");
            if (!LegalLevels(m).Contains(level))
                throw new ArgumentOutOfRangeException("level", "MaterialCatalog: level " + level + " is not a legal level for " + m.Id + " (" + string.Join(",", LegalLevels(m)) + ")");
        }

        // 버프 키 = (재질, 슬램 단계) — 계획 T2 StatRecipe.BuffKey.
        public static string BuffKeyOf(Material m, int slamTier)
        {
            if (m == null) throw new ArgumentNullException("m");
            if (slamTier < 1 || slamTier > GolemRules.Dials.SlamTierLevels.Count) throw new ArgumentOutOfRangeException("slamTier");
            return m.Id + "@T" + slamTier;
        }

        // 13 = 재질마다 합법 레벨의 슬램 단계 수(나무 1 · L5 3 · L10 3 · L15 3 × 2).
        public static readonly ReadOnlyCollection<string> Keys = Array.AsReadOnly(Materials.SelectMany(m => LegalLevels(m).Select(GolemRules.SlamTier).Distinct().Select(t => BuffKeyOf(m, t))).ToArray());

        // 버프 키 → 수치 특성 GUID(펫 / 합체). 키 집합이 Keys 와 다르면 형식 초기화가 던진다(아래 Check).
        static readonly ReadOnlyDictionary<string, string> PetStats = Map(new[]
        {
            "wood@T1", Guids.PetStatsWoodT1, "stone@T2", Guids.PetStatsStoneT2, "iron@T2", Guids.PetStatsIronT2, "brass@T2", Guids.PetStatsBrassT2,
            "gold@T3", Guids.PetStatsGoldT3, "autumn@T3", Guids.PetStatsAutumnT3, "summer@T3", Guids.PetStatsSummerT3,
            "adamantine@T4", Guids.PetStatsAdamantineT4, "adamantine@T5", Guids.PetStatsAdamantineT5, "greatergold@T4", Guids.PetStatsGreaterGoldT4, "greatergold@T5", Guids.PetStatsGreaterGoldT5,
            "mithral@T4", Guids.PetStatsMithralT4, "mithral@T5", Guids.PetStatsMithralT5,
        });
        static readonly ReadOnlyDictionary<string, string> FusionStats = Map(new[]
        {
            "wood@T1", Guids.FusionStatsWoodT1, "stone@T2", Guids.FusionStatsStoneT2, "iron@T2", Guids.FusionStatsIronT2, "brass@T2", Guids.FusionStatsBrassT2,
            "gold@T3", Guids.FusionStatsGoldT3, "autumn@T3", Guids.FusionStatsAutumnT3, "summer@T3", Guids.FusionStatsSummerT3,
            "adamantine@T4", Guids.FusionStatsAdamantineT4, "adamantine@T5", Guids.FusionStatsAdamantineT5, "greatergold@T4", Guids.FusionStatsGreaterGoldT4, "greatergold@T5", Guids.FusionStatsGreaterGoldT5,
            "mithral@T4", Guids.FusionStatsMithralT4, "mithral@T5", Guids.FusionStatsMithralT5,
        });

        // T4: 버프 키 → 재질 버프(Polymorph) · 슬램 무기 GUID. 키 집합은 Keys 와 같아야 한다(Map 이 형식 초기화 때 검사).
        static readonly ReadOnlyDictionary<string, string> BuffGuids = Map(new[]
        {
            "wood@T1", Guids.MaterialBuffWoodT1, "stone@T2", Guids.MaterialBuffStoneT2, "iron@T2", Guids.MaterialBuffIronT2, "brass@T2", Guids.MaterialBuffBrassT2,
            "gold@T3", Guids.MaterialBuffGoldT3, "autumn@T3", Guids.MaterialBuffAutumnT3, "summer@T3", Guids.MaterialBuffSummerT3,
            "adamantine@T4", Guids.MaterialBuffAdamantineT4, "adamantine@T5", Guids.MaterialBuffAdamantineT5, "greatergold@T4", Guids.MaterialBuffGreaterGoldT4, "greatergold@T5", Guids.MaterialBuffGreaterGoldT5,
            "mithral@T4", Guids.MaterialBuffMithralT4, "mithral@T5", Guids.MaterialBuffMithralT5,
        });
        static readonly ReadOnlyDictionary<string, string> SlamGuids = Map(new[]
        {
            "wood@T1", Guids.SlamWoodT1, "stone@T2", Guids.SlamStoneT2, "iron@T2", Guids.SlamIronT2, "brass@T2", Guids.SlamBrassT2,
            "gold@T3", Guids.SlamGoldT3, "autumn@T3", Guids.SlamAutumnT3, "summer@T3", Guids.SlamSummerT3,
            "adamantine@T4", Guids.SlamAdamantineT4, "adamantine@T5", Guids.SlamAdamantineT5, "greatergold@T4", Guids.SlamGreaterGoldT4, "greatergold@T5", Guids.SlamGreaterGoldT5,
            "mithral@T4", Guids.SlamMithralT4, "mithral@T5", Guids.SlamMithralT5,
        });

        // T5: 버프 키 → 합체 형태 버프(본체 Polymorph) GUID. 키 집합 = Keys(Map 검사).
        static readonly ReadOnlyDictionary<string, string> FusionFormGuids = Map(new[]
        {
            "wood@T1", Guids.FusionFormWoodT1, "stone@T2", Guids.FusionFormStoneT2, "iron@T2", Guids.FusionFormIronT2, "brass@T2", Guids.FusionFormBrassT2, "gold@T3", Guids.FusionFormGoldT3, "autumn@T3", Guids.FusionFormAutumnT3, "summer@T3", Guids.FusionFormSummerT3, "adamantine@T4", Guids.FusionFormAdamantineT4, "adamantine@T5", Guids.FusionFormAdamantineT5, "greatergold@T4", Guids.FusionFormGreaterGoldT4, "greatergold@T5", Guids.FusionFormGreaterGoldT5, "mithral@T4", Guids.FusionFormMithralT4, "mithral@T5", Guids.FusionFormMithralT5,
        });

        public static string BuffGuid(string buffKey) { return Lookup(BuffGuids, buffKey, "material buff"); }
        public static string FusionFormGuid(string buffKey) { return Lookup(FusionFormGuids, buffKey, "fusion form"); }
        public static string SlamGuid(string buffKey) { return Lookup(SlamGuids, buffKey, "slam weapon"); }
        public static IEnumerable<string> AllBuffGuids { get { return BuffGuids.Values; } }

        // 재질 → 재질 특성 GUID(나무 = T3 자리 MaterialWoodFeature 재사용, 원장 판정 T3 「T4 reuses」).
        static readonly ReadOnlyDictionary<string, string> FeatureGuids = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            { "wood", Guids.MaterialWoodFeature }, { "stone", Guids.MaterialStoneFeature }, { "iron", Guids.MaterialIronFeature }, { "brass", Guids.MaterialBrassFeature },
            { "gold", Guids.MaterialGoldFeature }, { "autumn", Guids.MaterialAutumnFeature }, { "summer", Guids.MaterialSummerFeature },
            { "adamantine", Guids.MaterialAdamantineFeature }, { "greatergold", Guids.MaterialGreaterGoldFeature }, { "mithral", Guids.MaterialMithralFeature },
        });

        public static string FeatureGuid(Material m)
        {
            if (m == null) throw new ArgumentNullException("m");
            string g;
            if (!FeatureGuids.TryGetValue(m.Id, out g)) throw new KeyNotFoundException("MaterialCatalog: no material feature GUID for '" + m.Id + "'");
            return g;
        }

        // 재질 특성 GUID → 재질(관측: Materials.CurrentOf 가 주인 사실에서 거꾸로 찾는다). 우리 재질 특성이 아니면 null — 「재질 특성인가」의 판정이지 결측 기본값이 아니다.
        public static Material OfFeatureGuidOrNull(string featureGuid)
        {
            if (string.IsNullOrEmpty(featureGuid)) throw new ArgumentException("MaterialCatalog.OfFeatureGuidOrNull: empty guid");
            var hit = FeatureGuids.Where(kv => kv.Value == featureGuid).Select(kv => kv.Key).ToArray();
            return hit.Length == 0 ? null : Get(hit.Single());
        }

        // 선택 레벨 → 재질 선택 GUID(Dials.MaterialLevels 와 같은 순서). 선택 레벨이 아니면 throw.
        public static string SelectionGuid(int level)
        {
            var guids = new[] { Guids.MaterialSelection5, Guids.MaterialSelection10, Guids.MaterialSelection15 };
            if (guids.Length != GolemRules.Dials.MaterialLevels.Count) throw new InvalidOperationException("MaterialCatalog: " + guids.Length + " selection GUIDs for " + GolemRules.Dials.MaterialLevels.Count + " material levels");
            int k = GolemRules.Dials.MaterialLevels.IndexOf(level);
            if (k < 0) throw new ArgumentOutOfRangeException("level", "MaterialCatalog.SelectionGuid: " + level + " is not a material level");
            return guids[k];
        }

        // 교체 규칙(설계 §4.5 「이전 재질 교체」, 선례 GreaterCognatogenFeature RemoveFeatureOnApply ×3 — 정찰 §2): 단계 k 재질을 고르면 단계 k-1 재질 전부를 지운다(나무 → 없음).
        public static ReadOnlyCollection<Material> ReplacedBy(Material m)
        {
            if (m == null) throw new ArgumentNullException("m");
            return Array.AsReadOnly(m.Tier == 0 ? new Material[0] : Materials.Where(x => x.Tier == m.Tier - 1).ToArray());
        }

        // 우리 특성(재질 버프 m_Facts 에 넣는다): 철 불로 수리(설계 §12 정정 7, 정찰 §8) · 아다만틴 급소 면역(설계 §4.5 「단독 특성 블루프린트 없음 → 우리 특성」, 정찰 §9).
        static readonly ReadOnlyDictionary<string, string[]> OwnFeatures = new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>
        {
            { "iron", new[] { Guids.FireRepairFeature } },
            { "adamantine", new[] { Guids.PrecisionImmunityFeature } },
        });

        public static string[] OwnFeatureGuidsOf(Material m)
        {
            if (m == null) throw new ArgumentNullException("m");
            Get(m.Id); // 카탈로그 밖 재질이면 던진다
            string[] g;
            return OwnFeatures.TryGetValue(m.Id, out g) ? (string[])g.Clone() : new string[0];
        }

        // 특기 능력 원본 → 우리 블루프린트(CloneRepointDc = DC 직업 재지정 복제 · NewAura = 돌 감속 오라 버프). Reference 는 원본 그대로(표에 없음).
        static readonly ReadOnlyDictionary<string, string> OursForSpecial = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            { Guids.Game.WoodGolemSplintering, Guids.CloneSplintering },
            { Guids.Game.GolemAutumnEntangleAuraBuff, Guids.CloneAutumnAuraBuff },
            { Guids.Game.GolemSummerEntangleAuraBuff, Guids.CloneSummerAuraBuff },
            { Guids.Game.GolemAutumnGrabFeature, Guids.CloneGrabFeature },
            { Guids.Game.StoneGolemSlow, Guids.StoneSlowAuraBuff },
        });

        public static string FactGuidOf(SpecialAbility a)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (a.Kind == AbilityKind.Reference) return a.Guid;
            string g;
            if (!OursForSpecial.TryGetValue(a.Guid, out g)) throw new KeyNotFoundException("MaterialCatalog: no blueprint of ours for " + a.Kind + " " + a.Guid);
            return g;
        }

         
        // 시험 `wiring: material buffs exclude stat features`(exec + 인게임 S11 materials-catalog-vs-blueprints)가 StatFeatureGuids 와 교집합 0 을 단언한다.
        public static string[] BuffFactGuids(Material m)
        {
            if (m == null) throw new ArgumentNullException("m");
            var l = m.ImmunityGuids.Concat(m.SpecialAbilities.Select(FactGuidOf)).Concat(OwnFeatureGuidsOf(m)).ToArray();
            if (l.Distinct().Count() != l.Length) throw new InvalidOperationException("MaterialCatalog: " + m.Id + " buff facts have a duplicate (" + string.Join(",", l) + ")");
            if (l.Intersect(StatFeatureGuids).Any()) throw new InvalidOperationException("MaterialCatalog: " + m.Id + " buff facts contain a stat feature (R2-03: stat features are owned by the mod, never by Polymorph m_Facts)");
            return l;
        }

        // 수치 특성 GUID → (버프 키, 합체 여부). 우리 수치 특성이 아니면 throw(호출자는 StatFeatureGuids 로 먼저 거른다).
        public static KeyValuePair<string, bool> StatKeyOfFeature(string statFeatureGuid)
        {
            if (string.IsNullOrEmpty(statFeatureGuid)) throw new ArgumentException("MaterialCatalog.StatKeyOfFeature: empty guid");
            // 2026-11-15 perf: 매 틱(Fusion.IsFusionTickTarget) 불리므로 LINQ ToArray 대신 미리 센 역표(StatKeyIndex). 값·예외·문구는 예전과 같다(시험 `perf: count` 가 대조).
            StatKeyEntry e;
            if (!StatKeyIndex.TryGetValue(statFeatureGuid, out e)) throw new KeyNotFoundException("MaterialCatalog: " + statFeatureGuid + " is not exactly one stat feature (pet 0, fusion 0)");
            if (e.Pet + e.Fusion != 1) throw new KeyNotFoundException("MaterialCatalog: " + statFeatureGuid + " is not exactly one stat feature (pet " + e.Pet + ", fusion " + e.Fusion + ")");
            return new KeyValuePair<string, bool>(e.Key, e.Fusion == 1);
        }

        sealed class StatKeyEntry { public string Key; public int Pet, Fusion; }

        // GUID → (마지막 키, 펫 수, 합체 수). 같은 GUID 가 여러 번이면 수가 1 을 넘고 StatKeyOfFeature 가 예전처럼 던진다(조용히 첫 것을 고르지 않는다).
        static Dictionary<string, StatKeyEntry> BuildStatKeyIndex()
        {
            var d = new Dictionary<string, StatKeyEntry>();
            Action<string, string, bool> add = (g, k, fusion) =>
            {
                StatKeyEntry e;
                if (!d.TryGetValue(g, out e)) { e = new StatKeyEntry(); d.Add(g, e); }
                e.Key = k;
                if (fusion) e.Fusion++; else e.Pet++;
            };
            foreach (var kv in PetStats) add(kv.Value, kv.Key, false);
            foreach (var kv in FusionStats) add(kv.Value, kv.Key, true);
            return d;
        }

        // 버프 키 → 재질(키 형식 「<id>@T<단계>」 — BuffKeyOf 와 같은 원천). 키가 Keys 에 없으면 throw.
        public static Material MaterialOfKey(string buffKey)
        {
            if (buffKey == null) throw new ArgumentNullException("buffKey");
            if (!Keys.Contains(buffKey)) throw new KeyNotFoundException("MaterialCatalog: unknown buff key '" + buffKey + "'");
            return Get(buffKey.Substring(0, buffKey.IndexOf('@')));
        }

        static ReadOnlyDictionary<string, string> Map(string[] kv)
        {
            if (kv.Length % 2 != 0) throw new InvalidOperationException("MaterialCatalog: odd key/value list");
            var d = new Dictionary<string, string>();
            for (int i = 0; i < kv.Length; i += 2) d.Add(kv[i], kv[i + 1]); // 중복 키 → ArgumentException
            if (!new HashSet<string>(d.Keys).SetEquals(Keys) || d.Count != Keys.Count)
                throw new InvalidOperationException("MaterialCatalog: stat feature table keys (" + string.Join(",", d.Keys) + ") != Keys (" + string.Join(",", Keys) + ")");
            return new ReadOnlyDictionary<string, string>(d);
        }

        public static string PetStatFeature(string buffKey) { return Lookup(PetStats, buffKey, "pet"); }
        public static string FusionStatFeature(string buffKey) { return Lookup(FusionStats, buffKey, "fusion"); }

        static string Lookup(ReadOnlyDictionary<string, string> d, string key, string what)
        {
            if (key == null) throw new ArgumentNullException("buffKey");
            string g;
            if (!d.TryGetValue(key, out g)) throw new KeyNotFoundException("MaterialCatalog: no " + what + " stat feature for key '" + key + "'");
            return g;
        }

        // 펫 13 + 합체 13 — 「우리 수치 특성」 판정 원천(T4 Materials.OwnedStatFacts, 계획 C4 ⑥).
        public static readonly ReadOnlyCollection<string> StatFeatureGuids = Array.AsReadOnly(PetStats.Values.Concat(FusionStats.Values).ToArray());

        // StatKeyOfFeature 역표 — PetStats·FusionStats 보다 뒤에 선언(정적 초기화는 글 순서).
        static readonly Dictionary<string, StatKeyEntry> StatKeyIndex = BuildStatKeyIndex();
    }
}
