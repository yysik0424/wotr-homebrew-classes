using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.BundlesLoading;
using Kingmaker.ResourceLinks;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.TempMapCode.Ambush;
using Kingmaker.EntitySystem.Persistence.Versioning;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;
using ExperienceComponent = Kingmaker.Blueprints.Classes.Experience.Experience;

namespace PhantomBlade
{
     
     
     
    public static class SpiritBlueprints
    {
        public static BlueprintFeature SpiritShellFeature;
        public static BlueprintUnit SpiritUnit;
        public static BlueprintBuff SpiritTranslucentFxBuff; // Dials.SpiritTranslucentFx == None 이면 null(만들지 않음)

        const string SpiritUnitName = "PhantomBladeSpiritUnit";

        // 원본(모델) 컴포넌트 분류 — 정확한 형식 일치만 본다(파생 형식도 「모름」). 두 표 어디에도 없는 형식이 원본에 있으면 Build 가 던진다:
        // 게임·다른 모드가 모델을 바꾸면 조용히 따라가지 않고 시끄럽게 멈춘다(C2). 출처 = blueprints.zip GhostHeraldNew.jbp(2026-11-09):
        // AddClassLevels, Experience, MobCaster, BuffOnEntityCreated, AddFacts — tests\PhantomBladeTests.cs HeraldComponentTypes 가 jbp 와 대조하고
        // 이 표(typeof)에 그 다섯이 다 있는지 확인한다(C4).
         
        static readonly Type[] KeepComponentTypes = { typeof(AddClassLevels) };
        static readonly Type[] DropComponentTypes =
        {
            typeof(ExperienceComponent),   // 경험치(정찰 2(b)). 원본: Mob CR18
            typeof(AddLoot),               // 전리품(정찰 2(b)). 원본에는 없음(계획 목록)
            typeof(BuffOnEntityCreated),   // 생성 때 버프를 붙임(원본: AasimarHaloBuff 0b1c9d29… 후광) — 「사실·버프·FX 를 더하는 일」이라 뺀다
            typeof(UnitUpgraderComponent),  
            typeof(AddFacts),              // 사실 추가(원본: 투명 보기·죽음 막기·황소 힘 등 주문 버프 6개) — 사실·FX 를 더하는 일
            typeof(MobCaster),             // AI 무리 시전자 표식(빈 컴포넌트, .ctor 만). 령은 두뇌 없음(m_Brain=null)·MarkPassive 라 쓸 일이 없다
        };

        public static void Build()
        {
             
            SpiritRules.Dials.Validate();
            if (StandBlueprints.StandBuff == null) throw new InvalidOperationException("PhantomBlade: SpiritBlueprints.Build() before StandBlueprints.Build()");
            if (SpiritUnit != null || SpiritShellFeature != null || SpiritTranslucentFxBuff != null) throw new InvalidOperationException("PhantomBlade: SpiritBlueprints.Build() called twice");

            var model = Get<BlueprintUnit>(SpiritRules.Dials.ModelUnitGuid);
            var shadowFx = Get<BlueprintBuff>(SpiritRules.Dials.ShadowFxBuffGuid);
            // 금지 FX 도 실제로 있는 블루프린트인지 확인한다: 아래 「GhostFX 없음」 단언이 엉뚱한 GUID 와 비교해 헛통과하지 않게.
            var ghostFx = Get<BlueprintBuff>(SpiritRules.Dials.GhostFxBuffGuid);
            var player = Get<BlueprintFaction>(Guids.Game.PlayerFaction);
             
            if (model.Size.ToString() != SpiritRules.Dials.ExpectedSizeName)
                throw new InvalidOperationException("PhantomBlade: spirit model " + model.name + " Size is " + model.Size + ", expected " + SpiritRules.Dials.ExpectedSizeName);

            BuildShell();
            if (SpiritRules.TranslucentFxEnabled(SpiritRules.Dials.SpiritTranslucentFx)) BuildTranslucentFx();
            SpiritUnit = BuildClone(model, shadowFx, ghostFx, player);
        }

         
        // 기전(dnlib 2026-11-14): 엔진의 은신 외형은 UnitEntityView.UpdateStealthMarks IL 004D~007A 가 BlueprintRoot.StealthEffectPrefab 을
        //   FxHelper.SpawnFxOnUnit 으로 붙이는 것뿐이다(은신 상태를 읽는 뷰 코드는 이것·걸음 동작(UnitAnimationController.TickOnUnit 00FA)·소리 RTPC 뿐 — 렌더러를
        //   바꾸는 은신 전용 코드는 없다). 버프 FxOnStart 도 Buff.TrySpawnParticleEffect IL 008E~010E 에서 같은 FxHelper.SpawnFxOnUnit 으로 뜬다 → 같은 프리팹이면 같은 모습.
        // PrefabLink 는 지어내지 않는다: 본편 InvisibilityBuff_Stealth_Cutscene 의 FxOnStart 를 읽어 상수와 대조한 뒤 그 AssetId 로 새 링크를 만든다(PackWarden AuxUnits.RequireFx 선례).
        //   그리고 번들 위치 목록에 실제로 있는지 BundlesLoadService.HasLocation(IL 000D~001E: LocationList.GuidToBundle.ContainsKey)로 확인 — 없으면 빌드가 던진다.
        //   위치 목록은 블루프린트 캐시보다 먼저 읽힌다(GameStarter.StartGameCoroutine IL 0299 RequestCommonBundlesCoroutine → IL 037C LoadPackTOC = BlueprintsCache.Init).
        static void BuildTranslucentFx()
        {
            string assetId = SpiritRules.TranslucentFxAssetId(SpiritRules.Dials.SpiritTranslucentFx);
            var precedent = Get<BlueprintBuff>(SpiritRules.Dials.StealthFxPrecedentBuffGuid);
            var precedentLink = precedent.FxOnStart;
            if (precedentLink == null || string.IsNullOrEmpty(precedentLink.AssetId))
                throw new InvalidOperationException("PhantomBlade: " + precedent.name + ".FxOnStart is empty; expected SpiritRules.Dials.StealthFxAssetId = " + assetId);
            if (precedentLink.AssetId != assetId)
                throw new InvalidOperationException("PhantomBlade: " + precedent.name + ".FxOnStart is " + precedentLink.AssetId + ", but SpiritRules.Dials.StealthFxAssetId = " + assetId);
            var bundles = BundlesLoadService.Instance;
            if (bundles == null) throw new InvalidOperationException("PhantomBlade: BundlesLoadService.Instance is null at blueprint build; cannot verify the stealth FX prefab " + assetId + " exists");
            if (!bundles.HasLocation(assetId))
                throw new InvalidOperationException("PhantomBlade: stealth FX prefab " + assetId + " (SpiritRules.Dials.StealthFxAssetId) is not in the bundle location list");

            var flagsField = typeof(BlueprintBuff).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);
            if (flagsField == null) throw new MissingFieldException("PhantomBlade: BlueprintBuff.m_Flags not found");
            object hiddenInUi = Enum.Parse(flagsField.FieldType, "HiddenInUi"); // 이름으로 읽는다(값 리터럴 없음). 없으면 Enum.Parse 가 던진다.

            SpiritTranslucentFxBuff = Create<BlueprintBuff>("PhantomBladeSpiritTranslucentFxBuff", Guids.SpiritTranslucentFxBuff, b =>
            {
                // 이름·설명·아이콘 없음(정당한 결측): UI 숨김 버프이고 령은 대상 불가라 어떤 UI 에도 안 나온다(SpiritShellFeature 와 같음).
                // 2026-11-17: 문구 칸은 null 금지 — 빈 키(SpiritShellFeature 와 같음, 다이얼이 켜졌을 때만 InGameAudit 숨긴 칸 목록에 든다).
                SetField(b, "m_DisplayName", NoText());
                SetField(b, "m_Description", NoText());
                SetField(b, "m_DescriptionShort", NoText());
                b.Stacking = StackingType.Replace;
                b.FxOnStart = new PrefabLink { AssetId = precedentLink.AssetId };
                // 빈 링크(제거 FX 없음). null 금지 — 2026-11-16 결함: 엔진 Buff.OnRemove IL_00A2~00B7 이 FxOnStart 로 입자를 띄운 버프를 지울 때
                // Blueprint.FxOnRemove.Load 를 null 검사 없이 부른다(코드로 만든 null 이면 NRE). 본편 jbp 의 null 은 읽을 때 빈 링크가 된다.
                b.FxOnRemove = new PrefabLink();
                b.ResourceAssetIds = new string[0];
                b.ComponentsArray = new BlueprintComponent[0];
            });
            flagsField.SetValue(SpiritTranslucentFxBuff, hiddenInUi);

            // --- 되읽기(틀리면 빌드가 던진다) ---
            var t = SpiritTranslucentFxBuff;
            if (t.ComponentsArray == null || t.ComponentsArray.Length != 0) throw new InvalidOperationException("PhantomBlade: " + t.name + " must have no components (FX only)");
            if (!t.IsHiddenInUI) throw new InvalidOperationException("PhantomBlade: " + t.name + " IsHiddenInUI readback failed");
            if (!Equals(flagsField.GetValue(t), hiddenInUi)) throw new InvalidOperationException("PhantomBlade: " + t.name + " m_Flags readback is " + flagsField.GetValue(t) + ", expected only HiddenInUi");
            if (t.FxOnStart == null || t.FxOnStart.AssetId != assetId || ReferenceEquals(t.FxOnStart, precedentLink))
                throw new InvalidOperationException("PhantomBlade: " + t.name + " FxOnStart readback failed (expected a new PrefabLink with AssetId " + assetId + ")");
            if (t.FxOnRemove == null || !string.IsNullOrEmpty(t.FxOnRemove.AssetId)) throw new InvalidOperationException("PhantomBlade: " + t.name + " FxOnRemove must be an empty link (null makes engine Buff.OnRemove NRE)");
            // --- 원본 불변 ---
            if (!ReferenceEquals(precedent.FxOnStart, precedentLink) || precedentLink.AssetId != assetId)
                throw new InvalidOperationException("PhantomBlade: " + precedent.name + ".FxOnStart was modified");
        }

        // 령 껍데기 특성(정찰 2(a)(b)): 클릭·대상 불가(Untargetable), AI 정지·기회 공격 없음(MarkPassive), 전투 참여 판정 제외(IsIgnoredByCombat).
        // Hidden 은 금지 — EntityVisibilityForPlayerController.IsVisible 이 즉시 「안 보임」을 돌려 령 자체가 사라진다(정찰 2(a)).
        static void BuildShell()
        {
            SpiritShellFeature = Create<BlueprintFeature>("PhantomBladeSpiritShellFeature", Guids.SpiritShellFeature, f =>
            {
                // 이름·설명·아이콘이 없다(정당한 결측): 숨김 특성이고 령은 대상 불가라 어떤 UI 에도 안 나온다.
                // 2026-11-17: 그래도 문구 칸은 null 로 두지 않는다 — null LocalizedString 은 op_Implicit 가 "<null>" 을 돌려준다(@000C~000F). 빈 키 = 「문구 없음」(InGameAudit 숨긴 칸 목록).
                SetField(f, "m_DisplayName", NoText());
                SetField(f, "m_Description", NoText());
                SetField(f, "m_DescriptionShort", NoText());
                f.HideInUI = true;
                f.Ranks = 1;
            });
            var ignored = new AddMechanicsFeature();
            SetField(ignored, "m_Feature", AddMechanicsFeature.MechanicsFeatureType.IsIgnoredByCombat);
            if (ignored.Feature != AddMechanicsFeature.MechanicsFeatureType.IsIgnoredByCombat) throw new InvalidOperationException("PhantomBlade: AddMechanicsFeature.m_Feature readback failed (" + ignored.Feature + ")");
            AddComponents(SpiritShellFeature, new Untargetable(), new MarkPassive(), ignored);

            var comps = SpiritShellFeature.ComponentsArray;
            if (comps == null || comps.Length != 3 || !(comps[0] is Untargetable) || !(comps[1] is MarkPassive) || !ReferenceEquals(comps[2], ignored))
                throw new InvalidOperationException("PhantomBlade: SpiritShellFeature components readback failed (" + (comps == null ? "null" : string.Join(",", comps.Select(c => c == null ? "null" : c.GetType().Name))) + ")");
            if (!SpiritShellFeature.HideInUI) throw new InvalidOperationException("PhantomBlade: SpiritShellFeature.HideInUI readback failed");
        }

        // 복제 위생(PackWarden AuxUnits.BuildClone 선례): 복제본에서 바꾸는 것은 ComponentsArray(분류표로 거른 새 배열), m_AddFacts(새 배열),
        // m_Faction(새 참조), m_Brain(null), Body(아이템을 뺀 새 사본), m_StartingInventory(빈 새 배열) 뿐. 원본 블루프린트(본편 유령 전령)는 그대로여야 한다.
        // Body(결함 2026-11-14): 예전에는 원본 Body 를 공유했다. 령은 Player 진영이라 인벤토리가 파티 공용이고(UnitDescriptor..ctor IL 00B4~00D8),
        //   원본 손 무기 성검 +2·방패 +3 은 Player 진영 장착 검사에 걸려(ItemEntity.CanBeEquippedBy IL 0001~0016) 손에 들리지 못한 채
        //   UnitBody.TryInsertItem IL 005B~0068 이 파티 인벤토리에 넣었다(령 생성마다 2개 — 게임 로그 "can't insert item"). 령 손에는 처음부터 아무것도 없었다
        //   (휘두름 스타일은 맨손 무기 — WeaponSlot.get_MaybeWeapon IL 0009~001B). 그래서 외형은 그대로, 아이템 블루프린트만 뺀다(SpiritRules.BodyFieldPlan).
        static BlueprintUnit BuildClone(BlueprintUnit src, BlueprintBuff shadowFx, BlueprintBuff ghostFx, BlueprintFaction player)
        {
            var srcComps = src.ComponentsArray;
            if (srcComps == null) throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " has null ComponentsArray");
            var srcCompsCopy = srcComps.ToArray();
            var srcFacts = (BlueprintUnitFactReference[])GetF(src, "m_AddFacts");
            if (srcFacts == null) throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " has null m_AddFacts");
            var srcFactsCopy = srcFacts.ToArray();
            var srcFactionRef = GetF(src, "m_Faction");
            var srcBrainRef = GetF(src, "m_Brain");
            var srcSize = src.Size;
            var srcBody = src.Body;
            if (srcBody == null) throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " has no Body");
            var bodyFields = BodyFields();
            var srcBodyValues = bodyFields.Select(f => f.GetValue(srcBody)).ToArray();
            var srcStart = (BlueprintItemReference[])GetF(src, "m_StartingInventory");

            var keep = new List<BlueprintComponent>();
            foreach (var c in srcComps)
            {
                if (c == null) throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " has a null component");
                var t = c.GetType();
                if (KeepComponentTypes.Contains(t)) keep.Add(c);
                else if (!DropComponentTypes.Contains(t))
                    throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " has unclassified component " + t.FullName + " (" + c.name + "); add it to SpiritBlueprints Keep/DropComponentTypes after checking what it does");
            }

            var clone = CloneUnit(src, SpiritUnitName, Guids.SpiritUnit);
            clone.ComponentsArray = keep.ToArray();
            var factGuids = SpiritFactGuids();
            SetField(clone, "m_AddFacts", factGuids.Select(g => Ref<BlueprintUnitFactReference>(g)).ToArray());
            SetField(clone, "m_Faction", Ref<BlueprintFactionReference>(Guids.Game.PlayerFaction));
            // 두뇌 없음: UnitBrain..ctor 가 DefaultBrain(null 허용, get_DefaultBrain IL 0006~000B)을 SetBrain 에 넘기고, SetBrain 은 null 이면 행동 목록을 비운 채 끝난다(IL 0013~001A).
            SetField(clone, "m_Brain", null);
            // 아이템 없는 Body 사본(결함 2026-11-14). 단일 참조 = null(게터가 null 을 돌려준다: BlueprintUnit/UnitBody.get_PrimaryHand·get_Armor IL 0006~000B),
            //   배열 = 같은 길이의 빈 칸 배열(원본 jbp 의 빠른 칸이 이미 전부 null 이고 엔진이 그대로 돈다 — TryInsertItem IL 0000~0006 이 null 이면 아무것도 안 만듦).
            var body = (BlueprintUnit.UnitBody)MemberwiseCloneMethod.Invoke(srcBody, null);
            foreach (var f in bodyFields)
            {
                if (SpiritRules.BodyFieldPlan(f.Name) != SpiritRules.BodyField.ClearItem) continue; // Keep = 원본 값 그대로(정당)
                if (f.FieldType.IsArray)
                {
                    var old = (Array)f.GetValue(body);
                    // 추가 팔다리(2026-11-16 그림자 악마: m_AdditionalLimbs = [발톱]) = 길이 0 — 빈 칸 팔다리 슬롯을 남기지 않는다. 빠른 칸 등 나머지는 같은 길이의 빈 칸(원본 관례).
                    bool limbs = f.Name == "m_AdditionalLimbs" || f.Name == "m_AdditionalSecondaryLimbs";
                    f.SetValue(body, Array.CreateInstance(f.FieldType.GetElementType(), limbs || old == null ? 0 : old.Length));
                }
                else f.SetValue(body, null);
            }
            clone.Body = body;
            // 시작 인벤토리(UnitDescriptor.AddStartingInventory IL 0041~0048 이 Inventory.Add 로 넣는다 — 같은 파티 인벤토리). 원본도 비어 있지만 원본 배열을 공유하지 않는다.
            SetField(clone, "m_StartingInventory", new BlueprintItemReference[0]);

            // --- 되읽기(틀리면 빌드가 던진다) ---
            var facts = clone.AddFacts.ToArray();
            var expectedFacts = new List<BlueprintUnitFact> { SpiritShellFeature, shadowFx };
            if (factGuids.Length == 3)
            {
                if (SpiritTranslucentFxBuff == null) throw new InvalidOperationException("PhantomBlade: translucent FX is on but SpiritTranslucentFxBuff was not built");
                expectedFacts.Add(SpiritTranslucentFxBuff);
            }
            if (factGuids.Length != expectedFacts.Count || facts.Length != expectedFacts.Count || facts.Where((f, i) => !ReferenceEquals(f, expectedFacts[i])).Any())
                throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " facts readback is [" + string.Join(",", facts.Select(f => f == null ? "null" : f.name)) + "], expected [" + string.Join(",", expectedFacts.Select(f => f.name)) + "]");
            if (!SpiritRules.TranslucentFxEnabled(SpiritRules.Dials.SpiritTranslucentFx) && SpiritTranslucentFxBuff != null)
                throw new InvalidOperationException("PhantomBlade: translucent FX is off but SpiritTranslucentFxBuff exists");
            if (facts.Any(f => f.AssetGuid == ghostFx.AssetGuid)) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " still carries the forbidden GhostFX");
            var cc = clone.ComponentsArray;
            if (ReferenceEquals(cc, srcComps) || cc.Length != keep.Count || cc.Where((c, i) => !ReferenceEquals(c, keep[i])).Any())
                throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " components readback failed");
            if (cc.Any(c => DropComponentTypes.Contains(c.GetType()) || !KeepComponentTypes.Contains(c.GetType())))
                throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " kept a dropped/unknown component");
            if (clone.Faction != player) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " faction readback is " + (clone.Faction == null ? "null" : clone.Faction.name) + ", expected Player");
            if (clone.DefaultBrain != null) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " brain readback is " + clone.DefaultBrain.name + ", expected none");
            if (clone.Size != srcSize || clone.Size.ToString() != SpiritRules.Dials.ExpectedSizeName) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " Size is " + clone.Size + ", expected " + SpiritRules.Dials.ExpectedSizeName);
            if (ReferenceEquals(clone.Body, srcBody) || clone.Body == null) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " Body is the source Body or null (item-free copy not installed)");
            // 2026-11-17: 문구 칸 null 금지(사본은 본편 모델의 인스턴스를 그대로 가리킨다 — 본편 jbp 는 세 칸 모두 빈 키, 시험 tooltip: jbp: 가 대조).
            RequireLocalizedStrings(clone);
            foreach (var f in bodyFields)
            {
                var v = f.GetValue(clone.Body);
                bool clear = SpiritRules.BodyFieldPlan(f.Name) == SpiritRules.BodyField.ClearItem;
                bool hasItem = v is Array a ? a.Cast<object>().Any(x => x != null) : v != null;
                if (clear && hasItem) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " Body." + f.Name + " still references an item after the strip");
                if (!clear && !Equals(v, f.GetValue(srcBody))) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " Body." + f.Name + " (kept field) differs from the source");
            }
            var cloneStart = (BlueprintItemReference[])GetF(clone, "m_StartingInventory");
            if (cloneStart == null || cloneStart.Length != 0 || ReferenceEquals(cloneStart, srcStart)) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " m_StartingInventory readback is not a fresh empty array");
            if (clone.AssetGuid.ToString() != Guids.SpiritUnit || clone.name != SpiritUnitName) throw new InvalidOperationException("PhantomBlade: " + SpiritUnitName + " identity readback failed");

            // --- 원본 불변 ---
            if (!ReferenceEquals(src.ComponentsArray, srcComps) || !srcComps.SequenceEqual(srcCompsCopy))
                throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " ComponentsArray was modified");
            var srcFactsAfter = (BlueprintUnitFactReference[])GetF(src, "m_AddFacts");
            if (!ReferenceEquals(srcFactsAfter, srcFacts) || !srcFacts.SequenceEqual(srcFactsCopy))
                throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " m_AddFacts was modified");
            if (!ReferenceEquals(GetF(src, "m_Faction"), srcFactionRef) || !ReferenceEquals(GetF(src, "m_Brain"), srcBrainRef) || src.Size != srcSize)
                throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " faction/brain/size was modified");
            if (!ReferenceEquals(src.Body, srcBody) || bodyFields.Where((f, i) => !ReferenceEquals(f.GetValue(srcBody), srcBodyValues[i]) && !Equals(f.GetValue(srcBody), srcBodyValues[i])).Any())
                throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " Body was modified");
            if (!ReferenceEquals(GetF(src, "m_StartingInventory"), srcStart)) throw new InvalidOperationException("PhantomBlade: spirit model " + src.name + " m_StartingInventory was modified");
            return clone;
        }

        // 령 사실 GUID 열 = SpiritRules.SpiritFactGuids(단일 원장). 점검 S12 도 이 함수로 기대 사실을 만든다.
        public static string[] SpiritFactGuids()
        {
            return SpiritRules.SpiritFactGuids(SpiritRules.Dials.SpiritTranslucentFx, Guids.SpiritShellFeature, SpiritRules.Dials.ShadowFxBuffGuid, Guids.SpiritTranslucentFxBuff);
        }

        static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        // BlueprintUnit.UnitBody 의 인스턴스 필드 전부. 모든 이름은 SpiritRules.BodyFieldPlan 이 분류해야 한다(모르는 이름이면 그 함수가 던진다 — 여기서 미리 한 번 부른다).
        static FieldInfo[] BodyFields()
        {
            if (MemberwiseCloneMethod == null) throw new MissingMethodException("PhantomBlade: object.MemberwiseClone not found");
            var fs = typeof(BlueprintUnit.UnitBody).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fs.Length == 0) throw new InvalidOperationException("PhantomBlade: BlueprintUnit.UnitBody has no instance fields (cannot strip items)");
            foreach (var f in fs) SpiritRules.BodyFieldPlan(f.Name);
            if (!fs.Any(f => SpiritRules.BodyFieldPlan(f.Name) == SpiritRules.BodyField.ClearItem)) throw new InvalidOperationException("PhantomBlade: BlueprintUnit.UnitBody has no item field to clear (field names changed?)");
            return fs;
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("PhantomBlade: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
