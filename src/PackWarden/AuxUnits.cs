using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using static PackWarden.BlueprintUtil;

namespace PackWarden
{
    // 설계 4.5 / 4.6 / 4.7. 보조 = 본편 동물 동료 유닛의 얕은 복제본(CloneUnit). 복제본의 하위 객체는 원본과 공유되므로
    // 바꾸는 것은 AssetGuid·name·m_AddFacts(새 배열)·ComponentsArray(AllowDyingCondition 을 뺀 새 배열)·m_Brain(새 참조 객체 → 그림자 두뇌, 그림자 무리 T2 —
    // 원장 67행 복제 위생 개정)뿐이다. 원본의 m_Brain 참조 객체·Character_Brain 은 읽기만 한다. 생성 직후 BuildClone 이 이를 검사한다.
    public static class AuxUnits
    {
        public static BlueprintSummonPool Pool;
        public static BlueprintFeature StudiedBonusFeature;
        // 2026-11-16 소유자 결정: 보조 전용 특성(소형 고정 ChangeUnitSize + 기회 공격 면역 AddCondition). 복제 유닛의 m_AddFacts 에 들어간다.
        public static BlueprintFeature NatureFeature;
        // 동물 동료 특성 guid → 보조가 받는 성장 특성(크기 부품을 뺀 사본 — 본편 ChangeUnitSize 또는 TabletopTweaks ChangeUnitBaseSize, 크기 변경이 없는 성장은 원본, 성장 없는 종은 null).
        static readonly Dictionary<string, BlueprintFeature> auxUpgradeByFeature = new Dictionary<string, BlueprintFeature>();
         
        // TabletopTweaks ChangeUnitBaseSize, 배치 판정 = GrowthSizeLayout; 크기 부품이 없는 성장(말)만 0). 복원 단계 계산의 입력.
        static readonly Dictionary<string, int> growthSizeDeltaByFeature = new Dictionary<string, int>();
        // 성장 특성 이름 → 판정한 크기 부품 배치(GrowthLayoutOf 가 채운다). Build 끝에서 전체 판정(섞임 = 진단 줄)과 로그 한 줄에 쓴다.
        static readonly Dictionary<string, GrowthSizeLayout.Kind> growthLayoutByUpgrade = new Dictionary<string, GrowthSizeLayout.Kind>();
        // 복원 단계 → 본편 WeaponSizeChange(그 단계)를 단 보조 전용 특성(Guids.AuxDiceRestoreFeatures).
        static readonly Dictionary<int, BlueprintFeature> diceRestoreBySteps = new Dictionary<int, BlueprintFeature>();
        public static Dictionary<string /*companion feature guid*/, BlueprintUnit> AuxByCompanionFeature;

        // 동물 동료 특성 이름 → 보조 유닛 GUID. 단일 원천 = Guids.Game.PackCompanions(13행, 출처는 그 표 주석).
        // Archetype.Build 의 허용 목록도 같은 표에서 파생되므로 두 표가 어긋날 수 없다. 표에 없는 특성이 오면 Build 가 실패한다.
        public static readonly Dictionary<string, string> AuxGuidByFeatureName = Guids.Game.PackCompanions.ToDictionary(r => r.Name, r => r.AuxGuid);

        // 본편 데이터에서 AddPet.m_UpgradeFeature 가 비어 있는 종(blueprints.zip AnimalCompanionFeatureHorse_PreorderBonus:
        // UpgradeLevel=4, m_UpgradeFeature=null). 본편 AddPet.TryLevelUpPet 도 UpgradeFeature==null 이면 업그레이드를 건너뛴다(IL_0179).
        // 이 목록 밖의 종이 업그레이드 특성 없이 오거나, 이 목록의 종에 특성이 생기면 Build 가 실패한다.
        static readonly HashSet<string> SpeciesWithoutUpgrade = new HashSet<string> { "AnimalCompanionFeatureHorse_PreorderBonus" };

        static readonly Dictionary<string, AddPet> addPetByFeature = new Dictionary<string, AddPet>();
        static readonly HashSet<BlueprintGuid> auxGuids = new HashSet<BlueprintGuid>(); // 보조 판정은 AssetGuid 로(과제 8 판정 5)
        static BlueprintBuff summonedUnitBuff;
        static readonly FieldInfo AddFactsField = typeof(BlueprintUnit).GetField("m_AddFacts", BindingFlags.NonPublic | BindingFlags.Instance);
        // 원본 m_Brain 참조 객체가 복제 전후 같은지 보려고 읽기만 한다(쓰기는 BuildClone 의 SetField — 복제본에만).
        static readonly FieldInfo BrainField = typeof(BlueprintUnit).GetField("m_Brain", BindingFlags.NonPublic | BindingFlags.Instance);

        // ---- 등장·소멸 연출(2026-11-09, 소유자 결정 = 본편 소환 주문의 등장 이펙트). 선택지 B: 본편 프리팹을 FxHelper.SpawnFxOnUnit 으로 직접 띄운다. ----
        // A(본편 SummonedUnitAppearBuff 부착)는 FxOnStart 가 없고 2초 CantAct/CantMove/Untargetable 규칙을 끌고 와서 기각, C(자체 FX 버프)는 보조에 fact 가 하나 더
        // 붙고 저장에 실려서 B 보다 규칙 표면이 넓어 기각. B 는 본편 Buff.TrySpawnParticleEffect(IL_00C4~0113)·SummonedUnitBuff.OnRemoved(IL_00C5~0115)가 결국 부르는 호출과 같다.
        // 링크 인스턴스는 본편 블루프린트의 것을 그대로 참조한다(새로 만들지 않음, 변경 없음). Build 가 AssetId 를 Guids.Game 상수와 대조해 어긋나면 던진다.
        static PrefabLink appearFx, dissolveFxMedium, dissolveFxLarge;
        // AssetId → 링크. 어느 크기에 어느 프리팹인지는 PackRules.DissolveFxFor(순수 함수, 오프라인 시험)가 정하고 여기서는 id 로 찾기만 한다.
        static Dictionary<string, PrefabLink> dissolveById;
        // 등장은 본편 버프와 같은 우선순위(Buff.TrySpawnParticleEffect IL_010D ldc.i4.1 = EventuallyImportant, 큐에 들어가 FxSpawnController.Tick 이 생성).
        // 소멸은 MarkForDestroy 직전에 부르므로 큐를 거치면 다음 틱의 EntityDestructionController.Tick(Destroy→Dispose→DestroyViewObject) 뒤에 생성될 수 있다 →
        // Important(0) 로 즉시 생성(FxSpawnController.Spawn IL_0025~0037: priority==0 이면 SpawnInternal). 본편 OnRemoved 는 유닛을 Delay 초 더 살려 두므로 1 을 쓴다 — 우리는 안 살려 둔다.
        internal const FxPriority AppearFxPriority = FxPriority.EventuallyImportant;
        internal const FxPriority DismissFxPriority = FxPriority.Important;
        // 진단 카운터(게임 안 점검 S3 appear-fx/dismiss-fx 가 읽는다). 규칙에는 쓰지 않는다.
        public static int AppearFxCount, DismissFxCount, FxWarningCount;
        public static string LastFxWarning;

        public static void Build()
        {
            if (Archetype.CompanionSelection == null) throw new InvalidOperationException("PackWarden: Archetype.Build() must run before AuxUnits.Build()");
            if (Aspects.AspectsFeature == null) throw new InvalidOperationException("PackWarden: Aspects.Build() must run before AuxUnits.Build()");
            if (AddFactsField == null) throw new MissingFieldException("PackWarden: BlueprintUnit.m_AddFacts not found");
            if (BrainField == null) throw new MissingFieldException("PackWarden: BlueprintUnit.m_Brain not found");
            if (ShadowBrains.ShadowBrain == null) throw new InvalidOperationException("PackWarden: ShadowBrains.Build() must run before AuxUnits.Build()");
            var icon = Get<BlueprintUnitFact>(Guids.Game.HunterAnimalFocusFeature).Icon;
            if (icon == null) throw new InvalidOperationException("PackWarden: HunterAnimalFocusFeature has no icon");

            // Resolve the old fact for saved auxiliaries without granting its attack bonus.
            StudiedBonusFeature = PackCall.Feature("PackWardenAuxStudiedBonusFeature", Guids.AuxStudiedBonusFeature, "AuxStudiedBonus", icon);
            StudiedBonusFeature.HideInUI = StudiedBonusFeature.HideInCharacterSheetAndLevelUp = true;

            NatureFeature = BuildNature(icon);
            foreach (var row in Guids.AuxDiceRestoreFeatures)
            {
                if (diceRestoreBySteps.ContainsKey(row.Steps)) throw new InvalidOperationException("PackWarden: duplicate AuxDiceRestoreFeatures row for " + row.Steps + " steps");
                diceRestoreBySteps[row.Steps] = BuildDiceRestore(row.Steps, row.Guid, icon);
            }

            Pool = Create<BlueprintSummonPool>("PackWardenAuxSummonPool", Guids.AuxSummonPool, p =>
            {
                p.Limit = PackRules.AuxSlotCount;   // 자리 수 = 표의 최대 마릿수(소유자 결정 2026-11-16: 4). 등록(Register)은 이 값을 강제하지 않는다(ContextActionSpawnMonster 만 읽음).
                p.DoNotRemoveDeadUnits = false;     // 죽은 유닛은 SummonPoolsManager.HandleUnitDeath 가 풀에서 뺀다(시체 파괴는 안 함 → DestroyUntracked).
            });

            summonedUnitBuff = Get<BlueprintBuff>(Guids.Game.SummonedUnitBuff);

            // 연출 프리팹 잠금: 본편 블루프린트에서 읽어 상수와 대조(단일 원천 = 게임 데이터, 상수 = 잠금). 없거나 다르면 빌드 실패(조용한 빈 PrefabLink 금지).
            appearFx = RequireFx(Get<BlueprintBuff>(Guids.Game.SummonedCreatureSpawnAllyBuff).FxOnStart, Guids.Game.SummonAppearFx, "SummonedCreatureSpawnAllyI-III.FxOnStart", "Guids.Game.SummonAppearFx");
            dissolveFxMedium = RequireFx(RequireVisual(Get<BlueprintUnit>(Guids.Game.LeopardSummonedUnit)).BloodPuddleFx, Guids.Game.SummonDissolveFxMedium, "LeopardSummoned.Visual.BloodPuddleFx", "Guids.Game.SummonDissolveFxMedium");
            dissolveFxLarge = RequireFx(RequireVisual(Get<BlueprintUnit>(Guids.Game.SmilodonSummonedUnit)).BloodPuddleFx, Guids.Game.SummonDissolveFxLarge, "SmilodonSummoned.Visual.BloodPuddleFx", "Guids.Game.SummonDissolveFxLarge");
            dissolveById = new Dictionary<string, PrefabLink> { { dissolveFxMedium.AssetId, dissolveFxMedium }, { dissolveFxLarge.AssetId, dissolveFxLarge } };
            // PackRules 가 돌려줄 수 있는 두 id 가 모두 여기에 있어야 한다(두 표가 어긋나면 빌드 실패).
            foreach (int ordinal in new[] { PackRules.SizeOrdinalMedium, PackRules.SizeOrdinalLarge })
                if (!dissolveById.ContainsKey(PackRules.DissolveFxFor(ordinal))) throw new InvalidOperationException("PackWarden: PackRules.DissolveFxFor(" + ordinal + ") is not a loaded dissolve prefab");

            AuxByCompanionFeature = new Dictionary<string, BlueprintUnit>();
            var upgradeClones = new Dictionary<string, BlueprintFeature>(); // 성장 특성 이름 → 사본(스밀로돈·트리케라톱스 성장은 두 종이 같이 쓴다)
            var all = Archetype.CompanionSelection.AllFeatures;
            foreach (var f in all)
            {
                if (f == null) throw new InvalidOperationException("PackWarden: unresolved feature in CompanionSelection");
                string guid;
                if (!AuxGuidByFeatureName.TryGetValue(f.name, out guid))
                    throw new InvalidOperationException("PackWarden: no auxiliary unit GUID mapped for companion feature " + f.name);
                var row = Guids.Game.PackCompanions.Single(r => r.Name == f.name);
                if (BlueprintGuid.Parse(row.FeatureGuid) != f.AssetGuid)
                    throw new InvalidOperationException("PackWarden: companion feature " + f.name + " has GUID " + f.AssetGuid + ", table says " + row.FeatureGuid);
                var addPet = f.GetComponent<AddPet>();
                if (addPet == null) throw new InvalidOperationException("PackWarden: companion feature has no AddPet: " + f.name);
                var src = addPet.Pet;
                if (src == null) throw new InvalidOperationException("PackWarden: AddPet has no pet unit: " + f.name);
                bool knownNoUpgrade = SpeciesWithoutUpgrade.Contains(f.name);
                if ((addPet.UpgradeFeature == null) != knownNoUpgrade)
                    throw new InvalidOperationException("PackWarden: " + f.name + " UpgradeFeature=" + (addPet.UpgradeFeature == null ? "null" : addPet.UpgradeFeature.name)
                        + " but SpeciesWithoutUpgrade says " + (knownNoUpgrade ? "none" : "present"));
                if (src.GetComponent<AddClassLevels>() == null) throw new InvalidOperationException("PackWarden: pet unit has no AddClassLevels: " + src.name);
                string key = f.AssetGuid.ToString();
                if (AuxByCompanionFeature.ContainsKey(key)) throw new InvalidOperationException("PackWarden: duplicate companion feature " + f.name);
                var clone = BuildClone(src, "PackWardenAux_" + f.name, guid);
                AuxByCompanionFeature[key] = clone;
                addPetByFeature[key] = addPet;
                auxUpgradeByFeature[key] = addPet.UpgradeFeature == null ? null : AuxUpgradeOf(addPet.UpgradeFeature, upgradeClones);
                // 주사위 복원: 이 종의 성장 전·후 복원 단계를 여기서 계산해 두 단계 모두 특성이 있는지 본다(없으면 아키타입 등록 실패 — 소환 때 처음 드러나게 두지 않는다).
                int growthDelta = addPet.UpgradeFeature == null ? 0 : GrowthSizeDelta(addPet.UpgradeFeature);
                growthSizeDeltaByFeature[key] = growthDelta;
                foreach (int delta in new[] { 0, growthDelta })
                {
                    int steps = PackRules.AuxDiceRestoreSteps((int)clone.Size, delta); // 기본 크기를 못 읽으면(범위 밖)·소형보다 작으면 던진다
                    if (steps != 0 && !diceRestoreBySteps.ContainsKey(steps))
                        throw new InvalidOperationException("PackWarden: " + f.name + " (size " + clone.Size + ", growth +" + delta + ") needs a " + steps + "-step dice restore but Guids.AuxDiceRestoreFeatures has no such row");
                }
                auxGuids.Add(clone.AssetGuid);
            }
            if (AuxByCompanionFeature.Count != AuxGuidByFeatureName.Count)
                throw new InvalidOperationException("PackWarden: expected " + AuxGuidByFeatureName.Count + " auxiliary clones, built " + AuxByCompanionFeature.Count);
            // 성장 사본 표의 모든 행이 쓰였어야 한다(표에 있는데 게임 데이터에 크기 변경 성장이 없으면 표가 낡았다).
            var unused = Guids.AuxUpgradesWithoutSize.Where(r => !upgradeClones.ContainsKey(r.UpgradeName)).Select(r => r.UpgradeName).ToArray();
            if (unused.Length > 0) throw new InvalidOperationException("PackWarden: AuxUpgradesWithoutSize rows not matched by any companion upgrade: " + string.Join(", ", unused));
            // 성장 크기 부품 배치(본편 / TabletopTweaks / 섞임)를 로그에 한 줄로 남긴다. 섞임은 진단일 뿐이다 — 처리는 성장마다의 판정을 쓴다(GrowthSizeLayout.Overall 주석, 검토 2026-11-17 지적 6).
            Main.Log("PackWarden " + GrowthSizeLayout.Describe(growthLayoutByUpgrade));

            // 판정 6: 주 동물 동료가 나중에 나타나면(새로 고름·되살아남) 켜진 상을 붙인다. 상 특성에 전역 구독 컴포넌트로 단다.
            AddComponents(Aspects.AspectsFeature, new PackPetAspectWatcher());
        }

        // 보조 전용 특성(소유자 결정 2026-11-16). ① ChangeUnitSize(Value = 소형): 엔진 UnitPartSizeModifier.UpdateSize 는 마지막 크기 변경 사실 하나로 State.Size 를 정하고
        // (@0006 LastItem), 모델 배율·몸 반경은 State.Size − OriginalSize 로 줄어든다(UnitEntityView.CalculateSizeScale·get_Corpulence) → 규칙·모습·자리 모두 소형.
        // 블루프린트 Size 를 바꾸면 OriginalSize 도 같이 바뀌어 모델·몸 반경이 그대로라(배율 1) 이 방식을 택했다. 성장의 크기 +1 은 사본에서 뺀다(AuxUpgradeOf).
        // ② AddCondition(ImmuneToAttackOfOpportunity): 본편 GraceBuff 와 같은 컴포넌트. 엔진 기회 공격의 유일한 깔때기 UnitCombatState.AttackOfOpportunity @01D2 가 본다.
        static BlueprintFeature BuildNature(UnityEngine.Sprite icon)
        {
            var f = PackCall.Feature("PackWardenAuxNatureFeature", Guids.AuxNatureFeature, "AuxNature", icon);
            var size = new ChangeUnitSize();
            var changeType = typeof(ChangeUnitSize).GetNestedType("ChangeType", BindingFlags.Public | BindingFlags.NonPublic);
            if (changeType == null) throw new MissingMemberException("PackWarden: ChangeUnitSize.ChangeType not found");
            SetField(size, "m_Type", Enum.Parse(changeType, "Value"));
            SetField(size, "Size", (Size)PackRules.AuxFixedSizeOrdinal);
            var cond = new AddCondition();
            SetField(cond, "Condition", (UnitCondition)Enum.Parse(typeof(UnitCondition), PackRules.AuxAoOImmunityCondition));
            AddComponents(f, size, cond);
            // 되읽기: 크기 변경은 Value 형식·소형, 조건은 기회 공격 면역, 컴포넌트는 정확히 이 둘.
            var typeField = typeof(ChangeUnitSize).GetField("m_Type", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var sizeField = typeof(ChangeUnitSize).GetField("Size", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (typeField == null || sizeField == null) throw new MissingFieldException("PackWarden: ChangeUnitSize.m_Type / Size not found");
            if (!Equals(typeField.GetValue(size), Enum.Parse(changeType, "Value")) || (Size)sizeField.GetValue(size) != (Size)PackRules.AuxFixedSizeOrdinal)
                throw new InvalidOperationException("PackWarden: AuxNature ChangeUnitSize readback is not Value " + (Size)PackRules.AuxFixedSizeOrdinal);
            if (cond.Condition.ToString() != PackRules.AuxAoOImmunityCondition) throw new InvalidOperationException("PackWarden: AuxNature AddCondition readback is " + cond.Condition);
            if (f.ComponentsArray.Length != 2 || !ReferenceEquals(f.ComponentsArray[0], size) || !ReferenceEquals(f.ComponentsArray[1], cond))
                throw new InvalidOperationException("PackWarden: AuxNature components are not exactly [ChangeUnitSize, AddCondition]");
            return f;
        }

         
        // (OnEventAboutToTrigger @0021~0039; CheckWeaponCategory=false 면 무기 종류·근접/원거리·자연 무기 여부를 가리지 않는다). IncreaseWeaponSize 는 m_SizeShift 만 쓴다
        // → 피해 주사위의 크기만 올라가고 명중·AC·은신·사거리·모델·몸 반경은 소형 그대로. 본편 선례: DemonRageBuff*(2단계), AshGiantOversizedWeaponFeature(1단계, 종류 제한 없음).
        static BlueprintFeature BuildDiceRestore(int steps, string guid, UnityEngine.Sprite icon)
        {
            if (steps <= 0) throw new ArgumentOutOfRangeException("steps", steps, "PackWarden: a dice restore feature needs at least 1 step");
            var f = PackCall.Feature("PackWardenAuxDiceRestore" + steps + "Feature", guid, "AuxDiceRestore", icon);
            var c = new WeaponSizeChange { SizeCategoryChange = steps, CheckWeaponCategory = false };
            AddComponents(f, c);
            if (f.ComponentsArray.Length != 1 || !ReferenceEquals(f.ComponentsArray[0], c) || c.SizeCategoryChange != steps || c.CheckWeaponCategory)
                throw new InvalidOperationException("PackWarden: dice restore feature for " + steps + " steps readback failed");
            return f;
        }

        // 성장 특성의 크기 부품 배치를 판정하고(순수 GrowthSizeLayout.Classify — 모르는 모양은 던진다) 그 부품 인스턴스를 돌려준다(None 이면 null).
        // 형식은 전체 이름 문자열로 가린다: TabletopTweaks 의 ChangeUnitBaseSize 는 컴파일 의존 없이 다뤄야 한다(2026-11-16 실사고 — GrowthSizeLayout 머리 주석).
        static GrowthSizeLayout.Kind GrowthLayoutOf(BlueprintFeature upgrade, out BlueprintComponent sizeComp)
        {
            if (upgrade == null) throw new ArgumentNullException("upgrade");
            var comps = upgrade.ComponentsArray ?? new BlueprintComponent[0];
            if (comps.Any(c => c == null)) throw new InvalidOperationException("PackWarden: null component on growth feature " + upgrade.name);
            var names = comps.Select(c => c.GetType().FullName).ToArray();
            bool listed = Guids.AuxUpgradesWithoutSize.Any(r => r.UpgradeName == upgrade.name);
            var kind = GrowthSizeLayout.Classify(upgrade.name, names, listed);
            sizeComp = null;
            int strip = GrowthSizeLayout.StripIndex(upgrade.name, names, listed);
            if ((strip < 0) != (kind == GrowthSizeLayout.Kind.None)) throw new InvalidOperationException("PackWarden: " + upgrade.name + " classified as " + kind + " but the strip index is " + strip);
            if (strip >= 0)
            {
                sizeComp = comps[strip];
                string want = GrowthSizeLayout.SizeComponentOf(kind);
                if (sizeComp.GetType().FullName != want) throw new InvalidOperationException("PackWarden: " + upgrade.name + " component #" + strip + " is " + sizeComp.GetType().FullName + ", expected " + want);
            }
            GrowthSizeLayout.Kind prev;
            if (growthLayoutByUpgrade.TryGetValue(upgrade.name, out prev) && prev != kind)
                throw new InvalidOperationException("PackWarden: " + upgrade.name + " growth layout changed from " + prev + " to " + kind + " during Build");
            growthLayoutByUpgrade[upgrade.name] = kind;
            return kind;
        }

        // 성장 특성이 주는 크기 증가. 크기 부품이 없으면 0(말 — 실제로 크기 성장이 없다; 표에 행이 있는데 없으면 GrowthLayoutOf 가 던진다), 있으면 Delta 형식이어야 하고 그 SizeDelta.
        // 본편 배치 = ChangeUnitSize(형식 있는 접근), TabletopTweaks 배치 = ChangeUnitBaseSize(리플렉션 — 필드 이름은 GrowthSizeLayout 상수, 못 찾으면 던진다).
        // 판정(형식이 Delta 인가, 1 이상인가, 못 읽었는가)은 순수 GrowthSizeLayout.SizeDelta 가 한다.
        static int GrowthSizeDelta(BlueprintFeature upgrade)
        {
            BlueprintComponent sizeComp;
            var kind = GrowthLayoutOf(upgrade, out sizeComp);
            string typeName = null; int? delta = null;
            if (kind == GrowthSizeLayout.Kind.Vanilla)
            {
                var vanilla = sizeComp as ChangeUnitSize;
                if (vanilla == null) throw new InvalidOperationException("PackWarden: " + upgrade.name + " size component " + sizeComp.GetType().FullName + " is not the game's ChangeUnitSize type");
                var typeField = typeof(ChangeUnitSize).GetField("m_Type", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (typeField == null) throw new MissingFieldException("PackWarden: ChangeUnitSize.m_Type not found");
                typeName = typeField.GetValue(vanilla).ToString();
                delta = vanilla.SizeDelta;
            }
            else if (kind == GrowthSizeLayout.Kind.TabletopTweaks)
            {
                var t = sizeComp.GetType();
                var typeField = FindField(t, GrowthSizeLayout.TabletopTweaksTypeField);
                var deltaField = FindField(t, GrowthSizeLayout.TabletopTweaksDeltaField);
                if (typeField == null || deltaField == null)
                    throw new MissingFieldException("PackWarden: " + t.FullName + "." + GrowthSizeLayout.TabletopTweaksTypeField + " / " + GrowthSizeLayout.TabletopTweaksDeltaField + " not found (TabletopTweaks-Core changed?) on " + upgrade.name);
                object tv = typeField.GetValue(sizeComp), dv = deltaField.GetValue(sizeComp);
                if (tv == null || !tv.GetType().IsEnum) throw new InvalidOperationException("PackWarden: " + upgrade.name + " " + t.Name + "." + GrowthSizeLayout.TabletopTweaksTypeField + " is not an enum value (" + (tv == null ? "null" : tv.GetType().FullName) + ")");
                if (!(dv is int)) throw new InvalidOperationException("PackWarden: " + upgrade.name + " " + t.Name + "." + GrowthSizeLayout.TabletopTweaksDeltaField + " is not an int (" + (dv == null ? "null" : dv.GetType().FullName) + ")");
                typeName = tv.ToString();
                delta = (int)dv;
            }
            else if (kind != GrowthSizeLayout.Kind.None) throw new InvalidOperationException("PackWarden: unknown growth layout " + kind + " for " + upgrade.name);
            return GrowthSizeLayout.SizeDelta(upgrade.name, kind, typeName, delta);
        }

         
        // (유닛, 무기, null, null) 로 공격 없이 굴린다: UnitDescriptionHelper.ExtractAttacks 등 17곳)을 굴려 나온 주사위를 「원래 크기 기준 기대값」 과 대조한다.
        // 정책 = 크기 되읽기와 같다: 불일치도 판정 불가도 던져서 소환을 취소한다. 고른 까닭: 틀린 주사위로 싸우는 보조는 규칙 위반인데 화면에 드러나지 않는다 —
        // 보조가 안 나오는 쪽이 눈에 보이고 UMM 로그에 사유가 남는다. 본편 13종은 시험이 전 무기·성장 전후의 변환 행을 확인해 두었다(걸리면 다른 모드가 무기를 바꾼 것).
        static void VerifyAuxDice(UnitEntityData unit, int originalSizeOrdinal)
        {
            var body = unit.Body;
            if (body == null) throw new InvalidOperationException("PackWarden: dice check UNKNOWN: " + unit.Blueprint.name + " has no Body");
            var slots = new List<Kingmaker.Items.Slots.WeaponSlot>();
            if (body.PrimaryHand != null && body.PrimaryHand.HasWeapon) slots.Add(body.PrimaryHand);
            if (body.SecondaryHand != null && body.SecondaryHand.HasWeapon) slots.Add(body.SecondaryHand);
            if (body.AdditionalLimbs != null)
                foreach (var l in body.AdditionalLimbs) if (l != null && l.HasWeapon) slots.Add(l);
            if (slots.Count == 0) throw new InvalidOperationException("PackWarden: dice check UNKNOWN: " + unit.Blueprint.name + " has no weapon slot to read");
            foreach (var slot in slots)
            {
                var weapon = slot.MaybeWeapon;
                var wbp = weapon == null ? null : weapon.Blueprint;
                int? baseRolls = null, baseSides = null, rolls = null, sides = null;
                if (wbp != null)
                {
                    var bd = wbp.BaseDamage;
                    baseRolls = bd.Rolls; baseSides = (int)bd.Dice;
                    var rule = Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.RuleCalculateWeaponStats(unit, weapon, null, null));
                    var mdf = rule == null ? null : rule.WeaponDamageDice;
                    if (mdf != null) { var v = mdf.ModifiedValue; rolls = v.Rolls; sides = (int)v.Dice; }
                }
                string detail;
                var verdict = PackRules.CheckAuxDice(rolls, sides, baseRolls, baseSides, originalSizeOrdinal, out detail);
                string where = unit.Blueprint.name + " weapon " + (wbp == null ? "(none)" : wbp.name);
                switch (verdict)
                {
                    case PackRules.DiceCheck.Match: break;
                    case PackRules.DiceCheck.Mismatch: throw new InvalidOperationException("PackWarden: dice check MISMATCH: " + where + ": " + detail);
                    case PackRules.DiceCheck.Unknown: throw new InvalidOperationException("PackWarden: dice check UNKNOWN: " + where + ": " + detail);
                    default: throw new InvalidOperationException("PackWarden: CheckAuxDice returned " + verdict);
                }
            }
        }

         
        // 던지지 않는다: 이미 전투 중이라 호출자가 보고만 한다. 원래 크기 = 블루프린트 기본 크기 + (성장 사본을 갖고 있으면 그 종의 성장 크기 증가).
        public static string RecheckDice(UnitEntityData aux)
        {
            RequireBuilt();
            if (aux == null) throw new ArgumentNullException("aux");
            string key = null;
            foreach (var kv in AuxByCompanionFeature) if (ReferenceEquals(kv.Value, aux.Blueprint)) { key = kv.Key; break; }
            if (key == null) return "UNKNOWN: " + aux.CharacterName + " is not built from an auxiliary blueprint";
            var upgrade = auxUpgradeByFeature[key];
            int delta = upgrade != null && aux.Descriptor.HasFact(upgrade) ? growthSizeDeltaByFeature[key] : 0;
            try { VerifyAuxDice(aux, (int)aux.Blueprint.Size + delta); return null; }
            catch (InvalidOperationException e) { return e.Message; }
        }

        // 종 성장 특성 → 보조가 받는 것. 크기 부품 0개 = 원본 그대로(말), 1개 = 그것만 뺀 사본(Guids.AuxUpgradesWithoutSize 의 GUID), 2개 이상·표와 어긋남·모르는 모양 = 실패
        // (판정 = GrowthLayoutOf → GrowthSizeLayout.Classify). 빼는 부품은 배치에 따라 본편 ChangeUnitSize 또는 TabletopTweaks 의 ChangeUnitBaseSize 다 —
        // TabletopTweaks 배치에서 그 부품을 남기면 UnitState.Size getter 후크가 +1 을 더해 보조가 소형이 아니게 된다(소환의 크기 되읽기가 던진다).
        // 크기 외의 성장(능력치·자연 갑옷·사지·특성)은 같은 인스턴스·같은 순서로 남는다(소유자 결정 2026-11-16: 크기만 뺀다).
        static BlueprintFeature AuxUpgradeOf(BlueprintFeature upgrade, Dictionary<string, BlueprintFeature> cache)
        {
            BlueprintComponent sizeComp;
            var kind = GrowthLayoutOf(upgrade, out sizeComp);
            if (kind == GrowthSizeLayout.Kind.None) return upgrade;
            if (sizeComp == null) throw new InvalidOperationException("PackWarden: " + upgrade.name + " classified as " + kind + " without a size component");
            var row = Guids.AuxUpgradesWithoutSize.Single(r => r.UpgradeName == upgrade.name); // 행이 없으면 Classify 가 이미 던졌다
            BlueprintFeature clone;
            if (cache.TryGetValue(upgrade.name, out clone)) return clone;
            var before = upgrade.ComponentsArray.ToArray();
            clone = CloneFeature(upgrade, "PackWardenAuxUpgrade_" + upgrade.name, row.AuxGuid, c => !ReferenceEquals(c, sizeComp));
            var want = before.Where(c => !ReferenceEquals(c, sizeComp)).ToArray();
            if (want.Length != before.Length - 1 || clone.ComponentsArray.Length != want.Length || clone.ComponentsArray.Where((c, i) => !ReferenceEquals(c, want[i])).Any())
                throw new InvalidOperationException("PackWarden: " + clone.name + " components are not the source's minus its size component (" + sizeComp.GetType().FullName + ")");
            // 사본에는 어느 배치의 크기 부품도 남아 있지 않아야 한다(형식 이름으로 다시 본다 — 판정과 제거가 어긋나면 여기서 던진다).
            var leftover = clone.ComponentsArray.Where(c => c.GetType().FullName == GrowthSizeLayout.VanillaSizeComponent || c.GetType().FullName == GrowthSizeLayout.TabletopTweaksSizeComponent).ToArray();
            if (leftover.Length != 0) throw new InvalidOperationException("PackWarden: " + clone.name + " still has " + leftover.Length + " size component(s) after stripping");
            if (!upgrade.ComponentsArray.SequenceEqual(before)) throw new InvalidOperationException("PackWarden: " + upgrade.name + " components changed while cloning");
            cache[upgrade.name] = clone;
            return clone;
        }





        // 복제 위생(판정 1·3): 복제본에서 바꾸는 것은 m_AddFacts(새 배열)와 ComponentsArray(AllowDyingCondition 만 뺀 새 배열)뿐.
        // 컴포넌트는 추가하지 않으므로 복제본 배열은 원본의 나머지 인스턴스를 같은 순서로 담아야 하고, 원본 배열은 그대로여야 한다.
        // Body 공유는 MemberwiseClone 의 사실이며 그대로임을 단언해 문서화한다.
        static BlueprintUnit BuildClone(BlueprintUnit src, string name, string guid)
        {
            var srcComps = src.ComponentsArray;
            if (srcComps == null) throw new InvalidOperationException("PackWarden: source unit " + src.name + " has null ComponentsArray");
            var srcFacts = (BlueprintUnitFactReference[])AddFactsField.GetValue(src);
            if (srcFacts == null) throw new InvalidOperationException("PackWarden: source unit " + src.name + " has null m_AddFacts");
            int srcFactCount = srcFacts.Length;
            // 그림자 무리 T2: 원본 두뇌 = 본편 Character_Brain(13종 잠금, SP-12). 원본 참조 객체와 해소 결과를 기억해 두고 끝에서 그대로인지 본다.
            var srcBrainRef = BrainField.GetValue(src);
            if (srcBrainRef == null) throw new InvalidOperationException("PackWarden: source unit " + src.name + " has null m_Brain");
            var srcBrain = src.DefaultBrain;
            // 전제 판정 = 순수 ShadowRules.SourceBrainProblem(검토 R17 — 시험 exe 가 13종 jbp m_Brain 으로 실행).
            var brainProblem = ShadowRules.SourceBrainProblem(srcBrain == null ? null : srcBrain.AssetGuid.ToString());
            if (brainProblem != null)
                throw new InvalidOperationException("PackWarden: source unit " + src.name + " brain is " + (srcBrain == null ? "null" : srcBrain.name + " " + srcBrain.AssetGuid) + ", expected Character_Brain " + Guids.Game.CharacterBrain + " (" + brainProblem + ")");
            var clone = CloneUnit(src, name, guid);
            // 복제본에만 새 참조 객체(공유 참조 객체를 고치지 않는다).
            SetField(clone, "m_Brain", Ref<BlueprintBrainReference>(Guids.ShadowBrain));
            // 판정 3(2026-11-07): AllowDyingCondition(0HP 에서 빈사)을 뺀 새 배열 → 보조는 소환체처럼 바로 죽고 사망·재소환 규칙이 적용된다.
            // 공유 인스턴스는 건드리지 않고 배열에서만 뺀다. 13종 모두 정확히 1개(blueprints.zip).
            var dropped = clone.ComponentsArray.Where(c => c is AllowDyingCondition).ToArray();
            if (dropped.Length != 1) throw new InvalidOperationException("PackWarden: " + src.name + " has " + dropped.Length + " AllowDyingCondition components, expected 1");
            clone.ComponentsArray = clone.ComponentsArray.Where(c => !(c is AllowDyingCondition)).ToArray();
            var facts = (BlueprintUnitFactReference[])AddFactsField.GetValue(clone);
            if (ReferenceEquals(facts, srcFacts)) throw new InvalidOperationException("PackWarden: clone shares m_AddFacts array with " + src.name);
            AddFactsField.SetValue(clone, facts.Concat(new[] { Ref<BlueprintUnitFactReference>(Guids.AuxNatureFeature) }).ToArray());

            var comps = clone.ComponentsArray;
            if (!ReferenceEquals(src.ComponentsArray, srcComps)) throw new InvalidOperationException("PackWarden: source unit " + src.name + " ComponentsArray was replaced while cloning");
            // 복제본 배열 = 원본 배열 − AllowDyingCondition 1개 (같은 인스턴스, 같은 순서). 새로 더한 컴포넌트는 없다.
            var expected = srcComps.Where(c => !ReferenceEquals(c, dropped[0])).ToArray();
            if (comps.Length != srcComps.Length - 1 || comps.Length != expected.Length) throw new InvalidOperationException("PackWarden: clone " + name + " component count is " + comps.Length + ", expected " + (srcComps.Length - 1));
            for (int i = 0; i < comps.Length; i++)
                if (!ReferenceEquals(comps[i], expected[i])) throw new InvalidOperationException("PackWarden: clone " + name + " component #" + i + " replaced");
            if (!srcComps.Contains(dropped[0])) throw new InvalidOperationException("PackWarden: source " + src.name + " lost its AllowDyingCondition (shared array mutated)");
            if (!ReferenceEquals(clone.Body, src.Body)) throw new InvalidOperationException("PackWarden: clone " + name + " Body is not the shared source Body (CloneUnit semantics changed?)");
            if (!ReferenceEquals(clone.LocalizedName, src.LocalizedName)) throw new InvalidOperationException("PackWarden: clone " + name + " name string changed");
            var srcAfter = (BlueprintUnitFactReference[])AddFactsField.GetValue(src);
            if (!ReferenceEquals(srcAfter, srcFacts) || srcAfter.Length != srcFactCount) throw new InvalidOperationException("PackWarden: source unit " + src.name + " m_AddFacts was modified");
            if (clone.AddFacts.Contains(StudiedBonusFeature) || !clone.AddFacts.Contains(NatureFeature)) throw new InvalidOperationException("PackWarden: clone " + name + " AddFacts readback failed");
            if (!clone.AddFacts.Contains(NatureFeature)) throw new InvalidOperationException("PackWarden: clone " + name + " AddFacts lacks the auxiliary nature feature (Small size, AoO immunity)");
            // 두뇌 되읽기: 원본 참조 객체·해소 결과 불변, 복제본 = 그림자 두뇌(같은 객체), 행동 집합 = 그림자 모드 기대.
            if (!ReferenceEquals(BrainField.GetValue(src), srcBrainRef)) throw new InvalidOperationException("PackWarden: source unit " + src.name + " m_Brain reference object was replaced");
            if (!ReferenceEquals(src.DefaultBrain, srcBrain)) throw new InvalidOperationException("PackWarden: source unit " + src.name + " DefaultBrain changed while cloning");
            if (ReferenceEquals(BrainField.GetValue(clone), srcBrainRef)) throw new InvalidOperationException("PackWarden: clone " + name + " still shares the source m_Brain reference object");
            if (!ReferenceEquals(clone.DefaultBrain, ShadowBrains.ShadowBrain)) throw new InvalidOperationException("PackWarden: clone " + name + " DefaultBrain readback is not PackWardenShadowBrain");
            var want = ShadowRules.ExpectedActionGuids(ShadowRules.PackMode.Shadow).Select(g => BlueprintGuid.Parse(g).ToString()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var got = ShadowBrains.ActionGuids(clone.DefaultBrain, "clone " + name + " brain");
            if (!got.SequenceEqual(want)) throw new InvalidOperationException("PackWarden: clone " + name + " brain actions [" + string.Join(", ", got) + "], expected [" + string.Join(", ", want) + "]");
            return clone;
        }

        // 본편 블루프린트의 PrefabLink 가 있고 AssetId 가 우리 상수와 같아야 한다. 게임 데이터가 바뀌면(패치·다른 모드) 여기서 BuildAll 이 실패해 아키타입이 등록되지 않는다.
        static PrefabLink RequireFx(PrefabLink link, string expectedAssetId, string what, string constName)
        {
            if (link == null || string.IsNullOrEmpty(link.AssetId)) throw new InvalidOperationException("PackWarden: " + what + " is empty; expected " + constName + " = " + expectedAssetId);
            if (link.AssetId != expectedAssetId) throw new InvalidOperationException("PackWarden: " + what + " is " + link.AssetId + ", but " + constName + " = " + expectedAssetId);
            return link;
        }

        static UnitVisualParams RequireVisual(BlueprintUnit bp)
        {
            if (bp.Visual == null) throw new InvalidOperationException("PackWarden: " + bp.name + " has no Visual params");
            return bp.Visual;
        }

        static void RequireBuilt()
        {
            if (AuxByCompanionFeature == null || Pool == null || StudiedBonusFeature == null || NatureFeature == null || appearFx == null || dissolveFxMedium == null || dissolveFxLarge == null || dissolveById == null)
                throw new InvalidOperationException("PackWarden: AuxUnits.Build() has not run");
        }

        // 등장 연출: 생성 경로(Summon) 끝에서 1회. 본편 Buff.TrySpawnParticleEffect 와 같은 호출(prefab, View, caster.IsPlayerFaction, null, zero, EventuallyImportant).
        // 뷰는 EntityCreationController.SpawnUnit → SpawnEntityWithView IL_00AA AttachView 가 같은 호출 안에서 붙이므로(다음 틱이 아님) 여기서 바로 쓸 수 있다.
        public static bool PlayAppearFx(UnitEntityData unit, UnitEntityData leader)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            bool ok = SpawnFx(appearFx, unit, leader, AppearFxPriority, "appear");
            if (ok) AppearFxCount++;
            return ok;
        }

        // 소멸 연출: 해산 경로(PackCall.Dismiss·DestroyUntracked)에서 MarkForDestroy 직전 1회. 본편 SummonedUnitBuff.OnRemoved 와 같은 호출이되 프리팹은 크기로 고른다
        // (PackRules.DissolveFxFor: Medium·Large 만 대응, 그 밖은 WARN + 재생 안 함). 호출자가 사망·이미 파괴 표시된 유닛을 걸러서 부른다.
        // 본편은 용해 Delay 초 동안 유닛을 살려 두지만 우리는 즉시 파괴 표시한다(규칙 불변) — 다음 틱에 뷰가 사라지므로 연출이 잘릴 수 있다. 게임 안에서만 확인된다(README 눈 점검).
        public static bool PlayDismissFx(UnitEntityData unit)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            RequireBuilt();
            var size = unit.Descriptor.State.Size;
            string id = PackRules.DissolveFxFor((int)size);
            if (id == null) return FxWarn("dismiss fx skipped: " + unit + " size " + size + " has no mapped dissolve prefab (mapped: Medium, Large)");
            PrefabLink link;
            if (!dissolveById.TryGetValue(id, out link)) throw new InvalidOperationException("PackWarden: PackRules.DissolveFxFor(" + size + ") = " + id + " is not a loaded dissolve prefab");
            var summoned = unit.Get<UnitPartSummonedMonster>();
            bool ok = SpawnFx(link, unit, summoned != null ? summoned.Summoner : null, DismissFxPriority, "dismiss");
            if (ok) DismissFxCount++;
            return ok;
        }

        // 유일하게 허용된 약한 경로(연출은 해산·소환을 막지 않는다): 뷰 없음·이미 파괴·프리팹 로드 실패·FX 시스템 거부·FX 엔진 예외는 WARN 1줄 + false.
        // 그 밖(인자 null·Build 전)은 던진다. 수정 1(I1, 2026-11-09): Important(0) 소멸 FX 는 FxSpawnController.Spawn 이 같은 호출 안에서 FxHelper.Spawn(인스턴스화·
        // SnapToLocator·DismemberUnitFX.Init)까지 동기 실행하므로, 거기서 난 예외가 PackCall.Dismiss 루프(뒤 슬롯 파괴 표시·슬롯 비우기)나 Summon 의 catch(소환 취소)로
        // 번지지 않게 Load~SpawnFxOnUnit 구간만 잡는다. 잡은 예외도 FxWarningCount 에 들어가 S3 appear-fx/dismiss-fx 가 FAIL 로 드러낸다.
        static bool SpawnFx(PrefabLink link, UnitEntityData unit, UnitEntityData caster, FxPriority priority, string what)
        {
            RequireBuilt();
            if (unit == null) throw new ArgumentNullException("unit");
            if (link == null) throw new ArgumentNullException("link");
            if (unit.Destroyed) return FxWarn(what + " fx skipped: " + unit + " is already destroyed");
            var view = unit.View;
            if (view == null) return FxWarn(what + " fx skipped: " + unit + " has no view");
            try
            {
                var prefab = link.Load(false, false);
                if (prefab == null) return FxWarn(what + " fx skipped: prefab " + link.AssetId + " did not load");
                var handle = FxHelper.SpawnFxOnUnit(prefab, view, caster != null ? (bool?)caster.IsPlayerFaction : null, null, Vector3.zero, priority);
                if (handle == null) return FxWarn(what + " fx skipped: FxHelper.SpawnFxOnUnit returned null for " + unit + " (DisableAllFx on the unit, or view detached)");
                return true;
            }
            catch (Exception e)
            {
                return FxWarn(what + " fx skipped: " + e.GetType().Name + " from the FX system for " + unit + ": " + e.Message);
            }
        }

        static bool FxWarn(string msg)
        {
            FxWarningCount++;
            LastFxWarning = msg;
            Main.Entry.Logger.Warning("PackWarden: " + msg);
            return false;
        }

        public static bool IsAuxBlueprint(BlueprintUnit bp) { RequireBuilt(); return bp != null && auxGuids.Contains(bp.AssetGuid); }

        // 리더가 가진 동물 동료 특성(13개 중 정확히 1개). 0개·2개 이상은 판정 불가 → 실패.
        static string CompanionFeatureKey(UnitEntityData leader)
        {
            string found = null;
            foreach (var f in Archetype.CompanionSelection.AllFeatures)
            {
                if (!leader.Descriptor.HasFact(f)) continue;
                if (found != null) throw new InvalidOperationException("PackWarden: " + leader + " has more than one pack companion feature");
                found = f.AssetGuid.ToString();
            }
            if (found == null) throw new InvalidOperationException("PackWarden: " + leader + " has no pack companion feature");
            return found;
        }

        // 슬롯 기록은 호출자 PackCall.TrySummon 한 곳에서만 한다(단일 기록자). 여기서는 만들고 돌려준다.
        // 생성 순서는 ContextActionSpawnMonster.RunAction → RuleSummonUnit.OnTrigger 를, 위치는 AddPet.TryUpdatePet 을 따른다.
        public static UnitEntityData Summon(UnitEntityData leader, int slot)
        {
            RequireBuilt();
            if (leader == null) throw new ArgumentNullException("leader");
            if (slot < 0 || slot >= PackRules.AuxSlotCount) throw new ArgumentOutOfRangeException("slot", slot, "PackWarden: slot must be 0.." + (PackRules.AuxSlotCount - 1));
            if (!leader.IsInGame) throw new InvalidOperationException("PackWarden: Summon for a leader not in game: " + leader);
            string key = CompanionFeatureKey(leader);
            var bp = AuxByCompanionFeature[key];
            var addPet = addPetByFeature[key];
            var acl = bp.GetComponent<AddClassLevels>();
            if (acl == null) throw new InvalidOperationException("PackWarden: auxiliary blueprint has no AddClassLevels: " + bp.name);
            int auxLevel = PackRules.AuxLevel(PackCall.HunterLevel(leader));

            // AddPet.TryUpdatePet IL_0158~0175: PlaceSpawnPlaces(1, 0.5, 주인 위치) → GetRelaxedPosition(0, true)
            // (2026-11-16 소유자 결정으로 보조는 소형 고정 — 아래 「대형 종」 문구는 그 이전 기준. 소형이면 몸 반경이 줄어 겹침 여지도 준다.)
            // 혼잡(2026-11-16, 보조 최대 4): 보조는 TrySummon 이 한 마리씩 차례로 만든다. PlaceSpawnPlaces 는 끝에 RelaxPoints 를 불러 8 m 안의 살아 있는 게임 안 유닛
            // (FreePlaceSelector.CollectUnitObstacles — 위치 + Corpulence, 방금 만든 앞 보조 포함)에서 점을 밀어내고 길찾기 그물 위로 맞춘다(최대 30회 반복, dnlib 2026-11-16).
            // 즉 뒤 보조는 앞 보조·펫·기수와 겹치지 않는 쪽으로 밀린다. 단 새 보조의 반경은 실제 몸 반경이 아니라 본편 값 0.5 로 계산된다(대형 종은 처음에 조금 겹칠 수 있다 — 미확인, README E6).
            FreePlaceSelector.PlaceSpawnPlaces(1, 0.5f, leader.Position);
            Vector3 pos = FreePlaceSelector.GetRelaxedPosition(0, true);
            // RuleSummonUnit.OnTrigger IL_000A~0031: SpawnUnit(bp, pos, Quaternion.identity, Initiator.HoldingState, null)
            var unit = Game.Instance.EntityCreator.SpawnUnit(bp, pos, Quaternion.identity, leader.HoldingState, null);
            if (unit == null) throw new InvalidOperationException("PackWarden: SpawnUnit returned null for " + bp.name);
            try
            {
                // RuleSummonUnit.OnTrigger IL_0049~0072 (DoNotLinkToCaster=false): 진영·그룹을 시전자에 맞춤
                unit.Descriptor.SwitchFactions(leader.Faction, true);
                unit.GroupId = leader.GroupId;
                unit.UpdateGroup();
                // IL_0077~008E: Ensure<UnitPartSummonedMonster>().Init(Initiator)
                 
                unit.Ensure<UnitPartSummonedMonster>().Init(leader);
                // IL_00E1~0113 (Context 없음 분기): UnitHelper.AddBuff(desc, SystemMechanics.SummonedUnitBuff, Initiator, duration, null).
                // 지속시간은 null(영구): 보조 수명은 전투 단위이며 PackCall.Dismiss 가 끝낸다. 본편 루트와 같은 버프인지 확인.
                if (!ReferenceEquals(Game.Instance.BlueprintRoot.SystemMechanics.SummonedUnitBuff, summonedUnitBuff))
                    throw new InvalidOperationException("PackWarden: Guids.Game.SummonedUnitBuff does not match BlueprintRoot.SystemMechanics.SummonedUnitBuff");
                if (unit.Descriptor.AddBuff(summonedUnitBuff, leader, null, null) == null)
                    throw new InvalidOperationException("PackWarden: SummonedUnitBuff was not applied to " + bp.name);
                // RuleSummonUnit.OnTrigger IL_01F3~0223 의 SummonedUnitAppearBuff(2초 등장 버프)는 의도적으로 생략: 보조는 기승 순간 바로 싸운다(설계 4.5).
                // ContextActionSpawnMonster.RunAction IL_01A6~01AE: GameHelper.RegisterUnitInSummonPool(pool, unit)
                GameHelper.RegisterUnitInSummonPool(Pool, unit);

                // AddPet.TryLevelUpPet: delta = 목표 레벨 − 현재 CharacterLevel, AddClassLevels.LevelUp(acl, desc, delta, null).
                // 본편 동물 유닛은 Levels=0·DoNotApplyAutomatically=true 라 생성 직후 0레벨(blueprints.zip). 결과가 auxLevel 이 아니면 실패.
                int delta = auxLevel - unit.Progression.CharacterLevel;
                if (delta < 0) throw new InvalidOperationException("PackWarden: spawned " + bp.name + " is already level " + unit.Progression.CharacterLevel + " > " + auxLevel);
                if (delta > 0) AddClassLevels.LevelUp(acl, unit.Descriptor, delta, null);
                if (unit.Progression.CharacterLevel != auxLevel)
                    throw new InvalidOperationException("PackWarden: " + bp.name + " level is " + unit.Progression.CharacterLevel + " after level-up, expected " + auxLevel);

                // AddPet.TryLevelUpPet IL_0165~0192: 레벨 ≥ UpgradeLevel 이고 UpgradeFeature 가 있으면 Progression.Features.AddFeature
                // UpgradeFeature==null 은 Build 가 SpeciesWithoutUpgrade 에 있는 종에만 허용했다(본편 데이터상 업그레이드 없음).
                // 2026-11-16: 성장은 크기 변경을 뺀 보조 전용 사본(AuxUpgradeOf)을 준다 — 보조는 자라지 않는다.
                var auxUpgrade = auxUpgradeByFeature[key];
                if ((auxUpgrade == null) != (addPet.UpgradeFeature == null)) throw new InvalidOperationException("PackWarden: auxiliary upgrade table disagrees with AddPet.UpgradeFeature for " + bp.name);
                if (PackRules.AuxGetsUpgrade(auxLevel, addPet.UpgradeLevel) && auxUpgrade != null)
                {
                    unit.Progression.Features.AddFeature(auxUpgrade, null);
                    if (!unit.Descriptor.HasFact(auxUpgrade)) throw new InvalidOperationException("PackWarden: upgrade " + auxUpgrade.name + " not applied to " + bp.name);
                }
                 
                // 보조는 소환할 때마다 그때의 헌터 레벨로 새로 만들어지므로(전투가 끝나면 해산, 전투 중에는 레벨이 오르지 않는다) 살아 있는 보조가 성장 지점을 넘는 일은 없다 —
                // 성장 지점을 넘긴 뒤의 첫 소환이 여기서 새 단계를 받는다.
                bool grown = PackRules.AuxGetsUpgrade(auxLevel, addPet.UpgradeLevel) && auxUpgrade != null;
                int growthDelta = grown ? growthSizeDeltaByFeature[key] : 0;
                int restoreSteps = PackRules.AuxDiceRestoreSteps((int)bp.Size, growthDelta);
                if (restoreSteps != 0)
                {
                    BlueprintFeature restore;
                    if (!diceRestoreBySteps.TryGetValue(restoreSteps, out restore)) throw new InvalidOperationException("PackWarden: no dice restore feature for " + restoreSteps + " steps (" + bp.name + ")");
                    unit.Progression.Features.AddFeature(restore, null);
                    if (!unit.Descriptor.HasFact(restore)) throw new InvalidOperationException("PackWarden: dice restore " + restore.name + " not applied to " + bp.name);
                }
                // 소유자 결정 2026-11-16: 보조 크기 = 소형 고정. 레벨·성장까지 다 붙인 뒤 엔진이 정한 크기를 되읽는다(다르면 소환 취소 — 아래 catch).
                if ((int)unit.Descriptor.State.Size != PackRules.AuxFixedSizeOrdinal)
                    throw new InvalidOperationException("PackWarden: " + bp.name + " size is " + unit.Descriptor.State.Size + " after summon, expected " + (Size)PackRules.AuxFixedSizeOrdinal);
                VerifyAuxDice(unit, (int)bp.Size + growthDelta);

                Aspects.ApplyCurrentAspectTo(leader, unit);
                // 그림자 무리 T4: 생성자엔 Restore 가 없다 → 여기서 모드를 정하고 두뇌·행동 목록을 채운다. 실패는 FailCount 로 센 뒤 아래 catch 가 반쯤 만든 보조를 지운다.
                try { ShadowPack.ApplyMode(leader, unit, ShadowRules.BrainModeFor(ShadowPack.StateOf(leader))); }
                catch (Exception e) { ShadowPack.CountFailure("AuxUnits.Summon", leader, e); throw; }
                // 설계 4.6: 소환 시 1회
                if (Aspects.IsToughActive(leader) && unit.Buffs.AddBuff(Aspects.ToughTempHpBuff, leader, null, null) == null)
                    throw new InvalidOperationException("PackWarden: ToughTempHpBuff was not applied to " + bp.name);
                // 등장 연출(2026-11-09): 풀 등록·레벨·업그레이드(크기)·상이 다 정해진 뒤 1회. 실패는 WARN 뿐(연출이라 소환을 되돌리지 않는다).
                PlayAppearFx(unit, leader);
                return unit;
            }
            catch
            {
                unit.MarkForDestroy(); // 반쯤 만든 보조를 남기지 않는다. 예외는 그대로 다시 던진다.
                throw;
            }
        }

        // 이 지도자가 소환한 보조 중 슬롯에 없는 것(시체 포함: 풀은 죽은 유닛을 빼기만 하고 파괴하지 않는다, 전투 중 되살아나
        // 추적이 끊긴 것)을 파괴 표시한다. PackCall.Dismiss 가 슬롯을 비운 뒤 부르므로 그때는 이 지도자의 보조 전부가 대상이다.
        public static void DestroyUntracked(UnitEntityData leader)
        {
            RequireBuilt();
            if (leader == null) throw new ArgumentNullException("leader");
            var part = leader.Get<UnitPartPack>();
            var doomed = new List<UnitEntityData>();
            foreach (var u in Game.Instance.State.Units)
            {
                if (u == null || u.Destroyed || !IsAuxBlueprint(u.Blueprint)) continue;
                var summoned = u.Get<UnitPartSummonedMonster>();
                if (summoned == null || !ReferenceEquals(summoned.Summoner, leader)) continue;
                if (part != null && part.SlotOf(u) >= 0) continue;
                doomed.Add(u);
            }
            foreach (var u in doomed)
            {
                // 소멸 연출은 살아 있고 아직 파괴 표시가 없는 보조에만(시체·같은 틱에 Dismiss 가 이미 표시한 유닛 제외 — MarkForDestroy 는 다음 틱까지 Destroyed 를 세우지 않는다).
                if (!u.State.IsDead && !u.DestroyMark && u.IsInGame) PlayDismissFx(u); // 숨은 보조(게임 밖)는 연출 없음(PackCall.Dismiss 와 같은 조건)
                u.MarkForDestroy();
            }
        }
    }

    // AspectsFeature 에 붙는다(지도자 소유, 전역 구독). 주 동물 동료가 새로 붙거나(UnitPartPetMaster.AddPet → IPetHandler.HandleAddPet(master, pet))
    // 되살아나면(UnitDescriptor.Resurrect → IUnitResurrectedHandler) 켜진 상의 팩 버프를 붙인다. 팩 버프는 Stacking=Replace 라 다시 붙여도 안전.
    public class PackPetAspectWatcher : UnitFactComponentDelegate, IPetHandler, IUnitResurrectedHandler, IGlobalSubscriber
    {
        public void HandleAddPet(UnitEntityData master, UnitEntityData pet)
        {
            if (!ReferenceEquals(master, Owner) || pet == null) return;
            if (!ReferenceEquals(Owner.GetPet(PetType.AnimalCompanion), pet)) return; // 동물 동료가 아닌 펫(다른 종류)은 무리가 아니다
            Aspects.ApplyCurrentAspectTo(Owner, pet);
        }

        public void HandleRemovePet(UnitEntityData master, UnitEntityData pet) { } // 제거 쪽은 Aspects 전파기가 맡는다

        public void HandleUnitResurrected(UnitEntityData unit)
        {
            if (unit == null || ReferenceEquals(unit, Owner)) return;
            if (!ReferenceEquals(Owner.GetPet(PetType.AnimalCompanion), unit)) return;
            Aspects.ApplyCurrentAspectTo(Owner, unit);
        }
    }
}
