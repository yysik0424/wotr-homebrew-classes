using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.DLC;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
    // 영병 선택지 · 시작 무기 59(+ 두 번째 머리 4) · 영병 유닛 59 · 영병 특성 59 · 랭크 진행표(계획 T3 Interfaces 「SpiritBlueprints.Build()」, 설계 §4.3·§4.4).
    // C4: 범주 = SpiritCategories.Rows(= CategoryPlan.Choose(jbp), 시험이 행마다 고정) 한 루프에서 무기·유닛·특성을 만든다. 선택지·숙련은 그 루프의 결과에서 파생.
    // 깊은 복사(F02): 기준 아이템·원형 유닛은 CloneKit 으로 값 복사하고, 바꾸는 하위 객체(컴포넌트·Body·시각 객체·배열)는 전부 새 인스턴스다.
    //   끝에서 Isolation.VerifyAfterBuild 가 본편 그래프와의 참조 분리 + 본편 값 불변을 단언한다(실패 → BuildFailed → 아키타입 미등록).
    public static class SpiritBlueprints
    {
        public sealed class Spirit
        {
            public CategoryRow Row;
            public WeaponCategory Category;
            public BlueprintWeaponType Type;
            public BlueprintItemWeapon BaseItem, BaseSecond, Weapon, Second;
            public BlueprintUnit Unit;
            public BlueprintFeature Feature;
            // 성장 특기(소유자 결정 2026-11-18): SpiritRules.SpiritGrowthFeats 순서. Grants = 주술사 쪽 숨김 특성(AddFeatureToPet), PetFeats = 펫 쪽 숨김 특성(AddParametrizedFeatures).
            public BlueprintFeature[] TierGrants, TierPetFeats;
        }

        public static readonly List<Spirit> Entries = new List<Spirit>();
        public static BlueprintFeatureSelection Selection;
        public static BlueprintProgression RankProgression;
        public static bool Built { get; private set; }

        public static void Build()
        {
            if (Built || Entries.Count > 0 || Selection != null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritBlueprints.Build() called twice");
            if (PetClass.Class == null || PetClass.SlotLock == null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritBlueprints.Build() before PetClass.Build()");
            var srcUnit = Get<BlueprintUnit>(Guids.Game.AnimalCompanionUnit);
            CheckSourceUnit(srcUnit);
            CheckNaturalArmorDial();
            SpiritPortrait.Build(); // 유닛 m_Portrait 가 가리킬 초상화(파티 바 펫 띠 포함, 2026-11-17) — 유닛보다 먼저
            RankProgression = BuildRankProgression();

            var available = SpiritCategories.Rows.Where(row => SpiritCategories.IsAvailable(row,
                guid => ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(guid)) != null)).ToArray();
            if (available.Length != SpiritCategories.Rows.Length)
                ModContext.Log("무기령 주술사: 톱날 세이버 원본이 없어 해당 선택지를 건너뜁니다. / Weapon Spirit Shaman: sawtooth sabre source missing, option omitted.");
            foreach (var row in available)
            {
                var s = new Spirit { Row = row, Category = (WeaponCategory)Enum.Parse(typeof(WeaponCategory), row.Category) };
                s.Weapon = BuildWeapon(s);
                s.Unit = BuildUnit(s, srcUnit);
                s.Feature = BuildFeature(s);
                Entries.Add(s);
            }

            PetClass.BuildProficiency(Entries.Select(e => e.Category).ToArray());
            Selection = BuildSelection();
            Archetype.AddAt(SpiritRules.SpiritSelectionLevel, Selection);

            // 수 단언(계획 T3): 선택지 == 무기 == 유닛 == 특성 == 행 수 == 59, 두 번째 머리 == 4.
            int options = Selection.AllFeatures.Count(), weapons = Entries.Count(e => e.Weapon != null), units = Entries.Count(e => e.Unit != null), feats = Entries.Count(e => e.Feature != null);
            int seconds = Entries.Count(e => e.Second != null), doubles = available.Count(r => r.Double);
            if (options != available.Length || weapons != options || units != options || feats != options || seconds != doubles)
                throw new InvalidOperationException("WeaponSpiritShaman: spirit counts differ: options " + options + ", weapons " + weapons + ", units " + units + ", features " + feats + ", rows " + available.Length + ", second heads " + seconds + "/" + doubles);
            foreach (var e in Entries) VerifyUnitFacts(e);

            Isolation.VerifyAfterBuild();
            Built = true;
            ModContext.Log("WSS spirits built: " + options + " categories (" + seconds + " double); " + string.Join(", ", Entries.Select(e => e.Row.Category + "=" + e.BaseItem.name + (e.Row.Source == BaseItemSource.Override ? "(override)" : "")).ToArray()));
        }

         
        // 붙일 사실(Dials.SpiritNaturalArmorGuid = NaturalArmor5)의 컴포넌트가 정확히 AddStatBonus{AC, NaturalArmor, Value == 다이얼} 하나가 아니면 throw.
        static void CheckNaturalArmorDial()
        {
            var fact = Get<Kingmaker.Blueprints.Facts.BlueprintUnitFact>(SpiritRules.Dials.SpiritNaturalArmorGuid);
            var comps = fact.ComponentsArray ?? new BlueprintComponent[0];
            var asb = comps.OfType<AddStatBonus>().ToArray();
            if (comps.Length != 1 || asb.Length != 1 || asb[0].Stat != StatType.AC || asb[0].Descriptor != ModifierDescriptor.NaturalArmor || asb[0].Value != SpiritRules.Dials.SpiritNaturalArmor)
                throw new InvalidOperationException("WeaponSpiritShaman: natural armor fact " + fact.name + " is not exactly AddStatBonus{AC, NaturalArmor, " + SpiritRules.Dials.SpiritNaturalArmor
                    + "} (components " + comps.Length + ", value " + (asb.Length == 1 ? asb[0].Value.ToString() : "-") + ") — Dials.SpiritNaturalArmor and the fact disagree");
        }

        // 원형 유닛 계약(C2): 컴포넌트 형식 목록이 SpiritRules.SpiritUnitComponentTypes 와 같고, 잠금·동물 사실이 없고, AddClassLevels 는 AnimalCompanionClass.
        static void CheckSourceUnit(BlueprintUnit u)
        {
            var types = (u.ComponentsArray ?? new BlueprintComponent[0]).Select(c => c == null ? "null" : c.GetType().Name).ToArray();
            if (!types.SequenceEqual(SpiritRules.SpiritUnitComponentTypes))
                throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionUnit components [" + string.Join(",", types) + "] != [" + string.Join(",", SpiritRules.SpiritUnitComponentTypes) + "]");
            var facts = (BlueprintUnitFactReference[])GetF(u, "m_AddFacts");
            if (facts == null || facts.Length != 0) throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionUnit m_AddFacts is not empty (" + (facts == null ? "null" : facts.Length.ToString()) + ")");
            var acl = u.GetComponent<AddClassLevels>();
            if (acl == null || acl.CharacterClass == null || acl.CharacterClass.AssetGuid.ToString() != Guids.Game.AnimalCompanionClass)
                throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionUnit AddClassLevels is not AnimalCompanionClass");
            if (u.Body == null || u.Visual == null) throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionUnit Body/Visual is null");
        }

        // 랭크 진행표 = WildlandShamanAnimalCompanionProgression edd41ea8 값 복사: 새 LevelEntries·항목, m_Classes = [새 ClassWithLevel{ShamanClass}].
        static BlueprintProgression BuildRankProgression()
        {
            var src = Get<BlueprintProgression>(Guids.Game.WildlandShamanAnimalCompanionProgression);
            var srcClasses = (BlueprintProgression.ClassWithLevel[])GetF(src, "m_Classes");
            if (srcClasses == null || srcClasses.Length != 1 || srcClasses[0].Class == null || srcClasses[0].Class.AssetGuid.ToString() != Guids.Game.ShamanClass)
                throw new InvalidOperationException("WeaponSpiritShaman: wildland rank progression m_Classes is not [ShamanClass]");
            if (src.LevelEntries == null || src.LevelEntries.Length != SpiritRules.MaxShamanLevel || src.LevelEntries.Any(e => e.Features.Count != 1 || e.Features[0].AssetGuid.ToString() != Guids.Game.AnimalCompanionRank))
                throw new InvalidOperationException("WeaponSpiritShaman: wildland rank progression is not L1..20 x AnimalCompanionRank");
            int additional = srcClasses[0].AdditionalLevel;
            var entries = src.LevelEntries.Select(e => new KeyValuePair<int, BlueprintFeatureBase[]>(e.Level, e.Features.ToArray())).ToArray();
            var p = Create<BlueprintProgression>("WeaponSpiritShamanRankProgression", Guids.RankProgression, x =>
            {
                CloneKit.CopyInto(src, x);
                x.ComponentsArray = new BlueprintComponent[0];
                x.LevelEntries = entries.Select(kv => { var le = new LevelEntry { Level = kv.Key }; le.SetFeatures(kv.Value); return le; }).ToArray();
                SetField(x, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(Guids.Game.ShamanClass), AdditionalLevel = additional } });
            });
            if (p.LevelEntries.Length != SpiritRules.MaxShamanLevel || ((BlueprintProgression.ClassWithLevel[])GetF(p, "m_Classes")).Single().Class.AssetGuid.ToString() != Guids.Game.ShamanClass)
                throw new InvalidOperationException("WeaponSpiritShaman: rank progression readback failed");
            return p;
        }

        // 시작 무기(+ 두 번째 머리): 기준 아이템 값 복사, 마법 부여 = [Masterwork] 새 배열, 값 0, 시각 객체 = 새 WeaponVisualParameters(R2-02), 컴포넌트 없음.
        static BlueprintItemWeapon BuildWeapon(Spirit s)
        {
            var row = s.Row;
            s.Type = Get<BlueprintWeaponType>(row.TypeGuid);
            s.BaseItem = Get<BlueprintItemWeapon>(row.ItemGuid);
            // 표 ↔ 게임 되읽기(C2): 범주·자연·모델·유형·Double·두 번째 머리가 표와 같아야 한다.
            if (s.Type.Category != s.Category) throw new InvalidOperationException("WeaponSpiritShaman: type " + s.Type.name + " category " + s.Type.Category + " != table " + row.Category);
            if (s.Type.IsNatural) throw new InvalidOperationException("WeaponSpiritShaman: type " + s.Type.name + " is natural");
            if (string.IsNullOrEmpty(ModelAssetId(s.Type.VisualParameters))) throw new InvalidOperationException("WeaponSpiritShaman: type " + s.Type.name + " has no weapon model link");
            if (!ReferenceEquals(s.BaseItem.Type, s.Type)) throw new InvalidOperationException("WeaponSpiritShaman: base item " + s.BaseItem.name + " is not of type " + s.Type.name);
            if (s.BaseItem.name != row.ItemName) throw new InvalidOperationException("WeaponSpiritShaman: base item name " + s.BaseItem.name + " != table " + row.ItemName);
            if (s.BaseItem.Double != row.Double) throw new InvalidOperationException("WeaponSpiritShaman: base item " + s.BaseItem.name + " Double=" + s.BaseItem.Double + " != table");
            if (row.Double)
            {
                string second = GuidOfRef(GetF(s.BaseItem, "m_SecondWeapon"));
                if (second != row.SecondGuid) throw new InvalidOperationException("WeaponSpiritShaman: " + s.BaseItem.name + " second head " + second + " != table " + row.SecondGuid);
                s.BaseSecond = Get<BlueprintItemWeapon>(row.SecondGuid);
                if (s.BaseSecond.Double || !string.IsNullOrEmpty(GuidOfRef(GetF(s.BaseSecond, "m_SecondWeapon"))))
                    throw new InvalidOperationException("WeaponSpiritShaman: second head " + s.BaseSecond.name + " is itself double");
                s.Second = CopyWeapon(s.BaseSecond, row, "weapon2", null, BaseItemSource.Standard);
            }
            return CopyWeapon(s.BaseItem, row, "weapon", row.Double ? DerivedGuids.For("weapon2", row.Category) : null, row.Source);
        }

        static BlueprintItemWeapon CopyWeapon(BlueprintItemWeapon baseItem, CategoryRow row, string kind, string secondGuid, BaseItemSource source)
        {
            // 다른 모드가 Build 전에 붙인 것(핫픽스 2026-11-14): ToggleableThrowingWeapons 의 ThrownCrossrefComponent(StandardDagger·StandardStarknife)는
            // ForeignComponents.Rules 의 (원본, 형식, 참조 GUID) 규칙으로만 알아보고 시작 무기에 넣지 않는다(시작 무기 컴포넌트는 아래에서 새로 정한다). 모르는 것은 Strip 이 던진다.
            var foreign = ForeignComponents.Strip(baseItem.name, baseItem.AssetGuid.ToString(), ForeignComponentViews.Of(baseItem));
            // 기준 아이템 컴포넌트: Standard* 는 0개, 예외표 아이템은 정확히 SpiritRules.OverrideItemComponentTypes(DlcCondition) — 새 인스턴스로 복사한다(O-T3-7).
            // 다른 모양이면 조용히 버리거나 공유하지 않고 던진다(jbp 직독 — 시험 `jbp: override item components`).
            var expected = source == BaseItemSource.Override ? SpiritRules.OverrideItemComponentTypes : new string[0];
            ForeignComponents.RequireKeptNames(foreign, expected);
            if (foreign.DroppedAt.Length > 0) ModContext.Log(foreign.Report());
            var enchantments = (BlueprintWeaponEnchantmentReference[])GetF(baseItem, "m_Enchantments");
            if (source != BaseItemSource.Override && (enchantments == null || enchantments.Length != 0)) throw new InvalidOperationException("WeaponSpiritShaman: base item " + baseItem.name + " has enchantments");
            if (baseItem.VisualParameters == null) throw new InvalidOperationException("WeaponSpiritShaman: base item " + baseItem.name + " has no VisualParameters");
            Isolation.AddItem(baseItem);
            string guid = DerivedGuids.For(kind, row.Category);
            var w = Create<BlueprintItemWeapon>("WeaponSpiritShaman" + (kind == "weapon2" ? "SecondHead_" : "Weapon_") + row.Category, guid, x =>
            {
                CloneKit.CopyInto(baseItem, x);
                x.ComponentsArray = new BlueprintComponent[0];
                SetField(x, "m_Enchantments", new[] { Ref<BlueprintWeaponEnchantmentReference>(SpiritRules.MasterworkGuid) });
                SetField(x, "m_Cost", 0);
                SetField(x, "m_VisualParameters", CloneKit.Copy(baseItem.VisualParameters));
                if (secondGuid != null) SetField(x, "m_SecondWeapon", Ref<BlueprintItemWeaponReference>(secondGuid));
            });
            // 되읽기: 마법 부여 = [Masterwork], 값 0, 제거 가능, 시각 객체 = 우리 것이고 Prototype = 유형 시각 객체(OnEnableWithLibrary @0023~0034), 모델 링크 있음.
            var ench = ((BlueprintWeaponEnchantmentReference[])GetF(w, "m_Enchantments")).Select(r => r.Guid.ToString()).ToArray();
            if (!ench.SequenceEqual(new[] { SpiritRules.MasterworkGuid }) || (int)GetF(w, "m_Cost") != 0 || w.IsNonRemovable)
                throw new InvalidOperationException("WeaponSpiritShaman: start weapon " + w.name + " readback failed (enchantments " + string.Join(",", ench) + ")");
            if (w.VisualParameters == null || ReferenceEquals(w.VisualParameters, baseItem.VisualParameters) || !ReferenceEquals(w.VisualParameters.Prototype, w.Type.VisualParameters))
                throw new InvalidOperationException("WeaponSpiritShaman: start weapon " + w.name + " visual parameters not our own instance linked to the type");
            if (string.IsNullOrEmpty(ModelAssetId(w.VisualParameters))) throw new InvalidOperationException("WeaponSpiritShaman: start weapon " + w.name + " resolves no weapon model link");
            if ((secondGuid == null) != string.IsNullOrEmpty(GuidOfRef(GetF(w, "m_SecondWeapon")))) throw new InvalidOperationException("WeaponSpiritShaman: start weapon " + w.name + " second head readback failed");
            if (source == BaseItemSource.Override)
            {
                // O-T3-7: DLC 보상 표시를 시작 무기에도 둔다 — 새 인스턴스, 값은 기준 아이템의 것과 같다(되읽기).
                var dlc = baseItem.ComponentsArray.OfType<DlcCondition>().Single();
                AddComponents(w, CloneKit.Copy(dlc));
                var back = w.ComponentsArray.OfType<DlcCondition>().ToArray();
                if (back.Length != 1 || ReferenceEquals(back[0], dlc) || GuidOfRef(GetF(back[0], "m_DlcReward")) == null
                    || GuidOfRef(GetF(back[0], "m_DlcReward")) != GuidOfRef(GetF(dlc, "m_DlcReward")) || !Equals(GetF(back[0], "m_HideInstead"), GetF(dlc, "m_HideInstead")))
                    throw new InvalidOperationException("WeaponSpiritShaman: start weapon " + w.name + " DlcCondition readback failed");
            }
            else if (w.ComponentsArray.Length != 0) throw new InvalidOperationException("WeaponSpiritShaman: start weapon " + w.name + " has components");
            return w;
        }

        // 영병 유닛: 원형 값 복사 + 새 컴포넌트(AddClassLevels → 우리 펫 직업, AllowDyingCondition·AddResurrectOnRest·AddFacts 복사), 새 Body(주무기 = 시작 무기),
        // 새 m_AddFacts, 늑대 능력치, 프리팹·초상화·크기·이동, 새 이름 자산. Visual 은 공유(읽기만).
        static BlueprintUnit BuildUnit(Spirit s, BlueprintUnit src)
        {
            var srcAcl = (AddClassLevels)src.ComponentsArray[0];
            var srcDying = (AllowDyingCondition)src.ComponentsArray[1];
            var srcResurrect = (AddResurrectOnRest)src.ComponentsArray[2];
            var srcFacts = (AddFacts)src.ComponentsArray[3];
            string weaponGuid = s.Weapon.AssetGuid.ToString();
            var u = Create<BlueprintUnit>("WeaponSpiritShamanUnit_" + s.Row.Category, DerivedGuids.For("unit", s.Row.Category), x =>
            {
                CloneKit.CopyInto(src, x);
                x.ComponentsArray = new BlueprintComponent[0];
                var body = CloneKit.Copy(src.Body);
                SetField(body, "m_PrimaryHand", Ref<BlueprintItemEquipmentHandReference>(weaponGuid));
                x.Body = body;
                SetField(x, "m_AddFacts", new[]
                {
                    Ref<BlueprintUnitFactReference>(Guids.Game.Airborne),
                    Ref<BlueprintUnitFactReference>(Guids.Game.ImmunityToProneCondition),
                    Ref<BlueprintUnitFactReference>(SpiritRules.Dials.SpiritNaturalArmorGuid),
                    Ref<BlueprintUnitFactReference>(Guids.SlotLockFeature),
                    Ref<BlueprintUnitFactReference>(Guids.ProficiencyFeature),
                    Ref<BlueprintUnitFactReference>(Guids.SpiritMirrorFeature),  
                });
                var st = SpiritRules.SpiritBaseStats;
                x.Strength = st[0]; x.Dexterity = st[1]; x.Constitution = st[2]; x.Intelligence = st[3]; x.Wisdom = st[4]; x.Charisma = st[5];
                x.Size = (Size)Enum.Parse(typeof(Size), SpiritRules.SpiritSizeName);
                x.Speed = new Kingmaker.Utility.Feet(SpiritRules.SpiritSpeed);
                x.Prefab = new UnitViewLink { AssetId = SpiritRules.SpiritPrefabAssetId };
                // 초상화 = 우리 SpiritPortrait(본편 AnimatedWeapons 그림 3장 + 펫 띠 그림, 2026-11-17). 본편 AnimatedWeapons 에는 펫 띠 그림이 없어 파티 바가 흰색이었다.
                SetField(x, "m_Portrait", Ref<BlueprintPortraitReference>(Guids.SpiritPortrait));
                var name = ScriptableObject.CreateInstance<SharedStringAsset>();
                name.String = Strings.L("Spirit.Name");
                x.LocalizedName = name;
            });
            var acl = CloneKit.Copy(srcAcl);
            SetField(acl, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.PetClass));
            // O-T3-13: 물기(Bite) 매개변수 자동 특기 2개를 이 영병의 범주로 — 그 항목만 새 SelectionEntry(CloneKit.Copy)로 바꾼 새 배열. 본편 항목은 쓰지 않는다.
            acl.Selections = acl.Selections.Select(e => RetargetBite(e, s.Category)).ToArray();
            AddComponents(u, acl, CloneKit.Copy(srcDying), CloneKit.Copy(srcResurrect), CloneKit.Copy(srcFacts));
            // 되읽기(게임이 읽는 경로).
            var back = u.GetComponent<AddClassLevels>();
            if (back == null || !ReferenceEquals(back.CharacterClass, PetClass.Class) || ReferenceEquals(back, srcAcl)) throw new InvalidOperationException("WeaponSpiritShaman: unit " + u.name + " AddClassLevels readback failed");
            if (!ReferenceEquals(u.Body.PrimaryHand, s.Weapon) || ReferenceEquals(u.Body, src.Body)) throw new InvalidOperationException("WeaponSpiritShaman: unit " + u.name + " body readback failed");
            var got = new[] { u.Strength, u.Dexterity, u.Constitution, u.Intelligence, u.Wisdom, u.Charisma };
            if (!got.SequenceEqual(SpiritRules.SpiritBaseStats) || u.Prefab.AssetId != SpiritRules.SpiritPrefabAssetId || u.Speed.Value != SpiritRules.SpiritSpeed)
                throw new InvalidOperationException("WeaponSpiritShaman: unit " + u.name + " stats/prefab/speed readback failed");
            if (u.ComponentsArray.OfType<LockEquipmentSlot>().Any()) throw new InvalidOperationException("WeaponSpiritShaman: unit " + u.name + " has a LockEquipmentSlot component");
            // 초상화 되읽기: 엔진 경로(PortraitSafe = m_Portrait.Get())로 우리 초상화이고 펫 띠 그림이 있다.
            if (SpiritPortrait.Portrait == null || !ReferenceEquals(u.PortraitSafe, SpiritPortrait.Portrait) || SpiritPortrait.PetEyeAssetOf(u.PortraitSafe) != SpiritRules.SpiritPetEyeSpriteAssetId)
                throw new InvalidOperationException("WeaponSpiritShaman: unit " + u.name + " portrait readback failed (m_Portrait " + GuidOfRef(GetF(u, "m_Portrait")) + ")");
            VerifyRetargeted(back, srcAcl, s);
            return u;
        }

        static readonly WeaponCategory BiteCategory = (WeaponCategory)Enum.Parse(typeof(WeaponCategory), SpiritRules.PetSelectionBiteCategory);

        // 물기 매개변수 항목이면 새 SelectionEntry(값 복사) + 범주 교체, 아니면 그대로(원소 공유 — 읽기만).
        static SelectionEntry RetargetBite(SelectionEntry e, WeaponCategory category)
        {
            if (e == null) throw new InvalidOperationException("WeaponSpiritShaman: null SelectionEntry in pet AddClassLevels");
            if (!e.IsParametrizedFeature || e.ParamWeaponCategory != BiteCategory) return e;
            var n = CloneKit.Copy(e);
            n.ParamWeaponCategory = category;
            return n;
        }

        // 되읽기(O-T3-13): 우리 항목 중 물기 0개, 다시 가리킨 항목 = SpiritRules.PetBiteSelectionFeatures 순서대로 이 범주의 새 인스턴스, 원본 항목은 그대로 물기.
        static void VerifyRetargeted(AddClassLevels ours, AddClassLevels src, Spirit s)
        {
            Func<SelectionEntry, string> feat = e => GuidOfRef(GetF(e, "m_ParametrizedFeature"));
            var srcBite = src.Selections.Where(e => e.IsParametrizedFeature && e.ParamWeaponCategory == BiteCategory).ToArray();
            var moved = ours.Selections.Where(e => !src.Selections.Contains(e)).ToArray();
            if (ours.Selections.Any(e => e.IsParametrizedFeature && e.ParamWeaponCategory == BiteCategory)
                || !srcBite.Select(feat).SequenceEqual(SpiritRules.PetBiteSelectionFeatures)
                || !moved.Select(feat).SequenceEqual(SpiritRules.PetBiteSelectionFeatures)
                || moved.Any(e => e.ParamWeaponCategory != s.Category || !e.IsParametrizedFeature)
                || ours.Selections.Length != src.Selections.Length)
                throw new InvalidOperationException("WeaponSpiritShaman: spirit " + s.Row.Category + " bite selection retarget readback failed");
        }

        // 영병 특성: AddPet(AnimalCompanion, 랭크 = AnimalCompanionRank) + [꺼 둔] 주술사 무기 중점화(범주) + 「동물 동료 없음」 전제(설계 §14 Q3) + 성장 특기 3단계.
        // 늑대 동료 특성(AnimalCompanionFeatureWolf)과 같은 설정.
        // 주술사 무기 중점화 제거(소유자 결정 2026-11-18): 컴포넌트를 빼지 않고 같은 자리(#1)에 Disabled 로 둔다. 컴포넌트 이름 = "$형식$GUID#자리"(BlueprintUtil.AddComponents)라
        //  자리가 그대로여야 옛 세이브의 런타임 컴포넌트가 이 블루프린트 컴포넌트에 다시 붙는다(EntityFact.PostLoadComponents — 이름으로 찾고, 못 찾으면 Deactivate 없이 Dispose → 무기 중점화가 영영 남는다).
        //  붙은 뒤 EntityFact.ApplyPostLoadFixes @001C 가 Disabled 를 보고 Deactivate → AddParametrizedFeatures.OnDeactivate 가 그때 붙였던 무기 중점화를 뗀다. 새 캐릭터는
        //  EntityFact.Setup @005D 가 Disabled 컴포넌트를 만들지 않는다(dnlib 2026-11-18 직독). 게임 안 확인은 README 「미검증」.
        static BlueprintFeature BuildFeature(Spirit s)
        {
            // 선택지 설명(2026-11-17): 범주마다 다른 문구 — 이 영병의 시작 무기 블루프린트에서 읽은 값으로 만든다(PutChoice 가 표에 넣고 키를 돌려준다).
            // 설명 생성기가 모르는 무기 표현(크기 ≠ 중형 · 근접/원거리 밖 공격 종류 등 — ChoiceText.Describe)이면 그 선택지만 「판정 불가」 설명 + 오류 줄이고 직업 등록은 계속된다(외부 검토 2026-11-17 지적 7).
            var outcome = ChoiceText.Describe(s.Row.Category, s.Row.Double, ChoiceOf(s.Weapon), s.Row.Double ? ChoiceOf(s.Second) : null);
            if (outcome.Undetermined != null) LogUndetermined(outcome, s, "build");
            bool changed;
            string descriptionKey = StringsTable.PutChoice(outcome, out changed);
            if (changed) throw new InvalidOperationException("WeaponSpiritShaman: choice description for " + s.Row.Category + " was already in the string table with another text");
            var f = Create<BlueprintFeature>("WeaponSpiritShamanSpirit_" + s.Row.Category, DerivedGuids.For("feature", s.Row.Category), x =>
            {
                SetField(x, "m_DisplayName", s.Type.TypeName);
                SetField(x, "m_Description", Strings.L(descriptionKey));
                SetField(x, "m_DescriptionShort", Strings.L(descriptionKey));
                SetField(x, "m_Icon", s.Type.Icon);
                x.Groups = new[] { FeatureGroup.AnimalCompanion };
                x.Ranks = 1;
                x.IsClassFeature = true;
                x.ReapplyOnLevelUp = true; // 늑대 동료 특성과 같음 — 주인 레벨업마다 AddPet 이 펫 레벨을 맞춘다
            });
            var addPet = new AddPet { Type = PetType.AnimalCompanion, ProgressionType = PetProgressionType.AnimalCompanion };
            SetField(addPet, "m_Pet", Ref<BlueprintUnitReference>(s.Unit.AssetGuid.ToString()));
            SetField(addPet, "m_LevelRank", Ref<BlueprintFeatureReference>(Guids.Game.AnimalCompanionRank));
            SetField(addPet, "m_LevelContextValue", new ContextValue());
            // m_UpgradeFeature 없음: TryLevelUpPet 은 UpgradeFeature == null 이면 업그레이드를 건너뛴다(@0174~0179).
            // AddParametrizedFeatures.FeatureData 는 비공개 중첩 형식 → 필드 형식에서 원소 형식을 얻어 만든다(이름 리터럴 없이 — 리플렉션 원장의 m_Features 에서 파생).
            var focus = new AddParametrizedFeatures();
            var dataType = FindField(typeof(AddParametrizedFeatures), "m_Features").FieldType.GetElementType();
            if (dataType == null) throw new MissingFieldException("WeaponSpiritShaman: AddParametrizedFeatures.m_Features is not an array");
            var data = Activator.CreateInstance(dataType, true);
            SetField(data, "m_Feature", Ref<BlueprintParametrizedFeatureReference>(Guids.Game.WeaponFocus));
            SetField(data, "ParamWeaponCategory", s.Category);
            var datas = Array.CreateInstance(dataType, 1);
            datas.SetValue(data, 0);
            SetField(focus, "m_Features", datas);
            focus.Disabled = true;
            var prereq = new PrerequisitePet { Type = PetType.AnimalCompanion, NoCompanion = true, Group = Prerequisite.GroupType.All };
            AddComponents(f, addPet, focus, prereq);
            BuildTierFeatures(s, f);
            var ap = f.GetComponent<AddPet>();
            if (ap == null || !ReferenceEquals(ap.Pet, s.Unit) || ap.UpgradeFeature != null) throw new InvalidOperationException("WeaponSpiritShaman: feature " + f.name + " AddPet readback failed");
            var fds = (Array)GetF(f.GetComponent<AddParametrizedFeatures>(), "m_Features");
            var fd = fds.Length == 1 ? fds.GetValue(0) : null;
            if (fd == null || GuidOfRef(GetF(fd, "m_Feature")) != Guids.Game.WeaponFocus || !s.Category.Equals(GetF(fd, "ParamWeaponCategory")))
                throw new InvalidOperationException("WeaponSpiritShaman: feature " + f.name + " weapon focus readback failed");
            if (!f.GetComponent<AddParametrizedFeatures>().Disabled)
                throw new InvalidOperationException("WeaponSpiritShaman: feature " + f.name + " shaman weapon focus component is not Disabled (owner decision 2026-11-18: the shaman gains no Weapon Focus)");
            VerifyTiers(f, s);
            return f;
        }

        // 성장 특기 3단계(소유자 결정 2026-11-18). 단계마다:
        //   펫 쪽 숨김 특성 P = AddParametrizedFeatures{본편 특기(SpiritGrowthFeat.FeatGuid), 고른 범주} — 엔진이 매개변수형 특기를 사실로 직접 붙인다(전제 조건 검사 없음, AddParametrizedFeatures.OnActivate @0022).
        //   주술사 쪽 숨김 특성 X = AddFeatureToPet{AnimalCompanion, P} — X 가 켜지면 주인의 동물 동료 펫에 P 를 붙이고(TryAdd), 꺼지면 뗀다(TryRemove). 펫이 나중에 생겨도
        //     HandleAddCompanion·OnTurnOn 이 다시 붙인다(dnlib 2026-11-18 직독).
        //   영병 특성(주술사) 컴포넌트 AddFeatureOnClassLevel{ShamanClass, Level = SpiritGrowthFeat.ShamanLevel, X} — 주술사 레벨(엔진 GetClassLevel(ShamanClass)) ≥ 단계 레벨이면 X 를 붙인다
        //     (IsFeatureShouldBeApplied, 레벨업 = HandleUnitGainLevel → Apply). 보조 직업·아키타입 조건은 비운다(빈 배열 = 조건 없음, CheckArchetype @000E).
        // AddFeatureOnClassLevel 은 SpiritGrowthFeats 순서로 영병 특성 f 에 붙인다(자리 #3..). 읽기 확인은 VerifyTiers.
        // 명시 배열 + 원소 newobj: 시험 `wiring: AddComponents arguments are new instances` 가 원소 출처를 IL 로 본다.
        static void BuildTierFeatures(Spirit s, BlueprintFeature f)
        {
            var tiers = SpiritRules.SpiritGrowthFeats;
            if (tiers == null || tiers.Length == 0) throw new InvalidOperationException("WeaponSpiritShaman: SpiritRules.SpiritGrowthFeats is empty");
            var dataType = FindField(typeof(AddParametrizedFeatures), "m_Features").FieldType.GetElementType();
            if (dataType == null) throw new MissingFieldException("WeaponSpiritShaman: AddParametrizedFeatures.m_Features is not an array");
            s.TierGrants = new BlueprintFeature[tiers.Length];
            s.TierPetFeats = new BlueprintFeature[tiers.Length];
            var result = new BlueprintComponent[tiers.Length];
            for (int i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                Get<BlueprintParametrizedFeature>(tier.FeatGuid); // 본편 특기가 없거나 매개변수형이 아니면 여기서 던진다(조용히 빈 참조를 만들지 않는다)
                var pet = Create<BlueprintFeature>("WeaponSpiritShamanSpiritFeat_" + tier.Key + "_" + s.Row.Category, DerivedGuids.For(tier.PetKind, s.Row.Category), HiddenTierFeature);
                var data = Activator.CreateInstance(dataType, true);
                SetField(data, "m_Feature", Ref<BlueprintParametrizedFeatureReference>(tier.FeatGuid));
                SetField(data, "ParamWeaponCategory", s.Category);
                var datas = Array.CreateInstance(dataType, 1);
                datas.SetValue(data, 0);
                var apf = new AddParametrizedFeatures();
                SetField(apf, "m_Features", datas);
                AddComponents(pet, apf);

                var grant = Create<BlueprintFeature>("WeaponSpiritShamanSpiritFeatGrant_" + tier.Key + "_" + s.Row.Category, DerivedGuids.For(tier.GrantKind, s.Row.Category), HiddenTierFeature);
                var toPet = new AddFeatureToPet();
                SetField(toPet, "m_PetType", PetType.AnimalCompanion);
                SetField(toPet, "m_Feature", Ref<BlueprintFeatureReference>(pet.AssetGuid.ToString()));
                AddComponents(grant, toPet);

                var onLevel = new AddFeatureOnClassLevel { Level = tier.ShamanLevel, BeforeThisLevel = false };
                SetField(onLevel, "m_Class", Ref<BlueprintCharacterClassReference>(Guids.Game.ShamanClass));
                SetField(onLevel, "m_Feature", Ref<BlueprintFeatureReference>(grant.AssetGuid.ToString()));
                SetField(onLevel, "m_AdditionalClasses", new BlueprintCharacterClassReference[0]);
                SetField(onLevel, "m_Archetypes", new BlueprintArchetypeReference[0]);
                s.TierGrants[i] = grant;
                s.TierPetFeats[i] = pet;
                result[i] = onLevel;
            }
            AddComponents(f, result);
            // 옛 세이브 주술사 무기 중점화 감사(2026-11-18 추가 수리) — 자리 #6(단계 뒤). Disabled 경로가 안 돌아 남은 무기 중점화를 지역 로드마다 가른다(LegacyFocusAudit).
            AddComponents(f, new LegacyFocusAudit());
        }

        static void HiddenTierFeature(BlueprintFeature x)
        {
            SetField(x, "m_DisplayName", Strings.L(SpiritRules.GrowthFeatNameKey));
            SetField(x, "m_Description", Strings.L(SpiritRules.GrowthFeatDescriptionKey));
            SetField(x, "m_DescriptionShort", Strings.L(SpiritRules.GrowthFeatDescriptionKey));
            x.HideInUI = true;
            x.IsClassFeature = true;
            x.Ranks = 1;
        }

        // 성장 특기 읽기 확인(게임이 읽는 경로 — 참조 해석 속성): 영병 특성의 컴포넌트 자리, AddFeatureOnClassLevel 이 SpiritGrowthFeats 순서·수 그대로 · 주술사 직업 · 단계 레벨 · X,
        // X 의 AddFeatureToPet = 동물 동료·P, P 의 AddParametrizedFeatures = 그 단계의 본편 특기·이 범주 하나. 하나라도 다르면 던진다(BuildFailed → 아키타입 미등록).
        static void VerifyTiers(BlueprintFeature f, Spirit s)
        {
            var tiers = SpiritRules.SpiritGrowthFeats;
            var ons = f.ComponentsArray.OfType<AddFeatureOnClassLevel>().ToArray();
            if (ons.Length != tiers.Length || s.TierGrants == null || s.TierPetFeats == null || s.TierGrants.Length != tiers.Length || s.TierPetFeats.Length != tiers.Length)
                throw new InvalidOperationException("WeaponSpiritShaman: feature " + f.name + " has " + ons.Length + " AddFeatureOnClassLevel, expected " + tiers.Length);
            // 컴포넌트 자리: AddPet #0 · 꺼 둔 무기 중점화 #1 · 전제 #2 · 단계 #3.. — 옛 세이브의 런타임 컴포넌트 이름이 이 자리에 묶여 있다(BuildFeature 주석).
            var comps = f.ComponentsArray;
            var wantTypes = new[] { typeof(AddPet), typeof(AddParametrizedFeatures), typeof(PrerequisitePet) }.Concat(tiers.Select(t => typeof(AddFeatureOnClassLevel))).Concat(new[] { typeof(LegacyFocusAudit) }).ToArray();
            if (comps.Length != wantTypes.Length || Enumerable.Range(0, comps.Length).Any(i => comps[i].GetType() != wantTypes[i] || comps[i].name != "$" + wantTypes[i].Name + "$" + f.AssetGuid + "#" + i))
                throw new InvalidOperationException("WeaponSpiritShaman: feature " + f.name + " component layout [" + string.Join(",", comps.Select(c => c.name).ToArray()) + "] is not AddPet#0, AddParametrizedFeatures#1, PrerequisitePet#2, AddFeatureOnClassLevel#3..5, LegacyFocusAudit#6");
            for (int i = 0; i < tiers.Length; i++)
            {
                var on = ons[i];
                var grant = s.TierGrants[i];
                var pet = s.TierPetFeats[i];
                if (on.Class == null || on.Class.AssetGuid.ToString() != Guids.Game.ShamanClass || on.Level != tiers[i].ShamanLevel || on.BeforeThisLevel
                    || !ReferenceEquals(on.Feature, grant) || on.AdditionalClasses.Count() != 0 || on.Archetypes.Count() != 0)
                    throw new InvalidOperationException("WeaponSpiritShaman: feature " + f.name + " tier " + tiers[i].Key + " AddFeatureOnClassLevel readback failed (level " + on.Level + ")");
                var toPet = grant.ComponentsArray.OfType<AddFeatureToPet>().ToArray();
                if (grant.ComponentsArray.Length != 1 || toPet.Length != 1 || !ReferenceEquals(toPet[0].Feature, pet) || !PetType.AnimalCompanion.Equals(GetF(toPet[0], "m_PetType")) || !grant.HideInUI)
                    throw new InvalidOperationException("WeaponSpiritShaman: tier grant " + grant.name + " AddFeatureToPet readback failed");
                var apf = pet.ComponentsArray.OfType<AddParametrizedFeatures>().ToArray();
                var pd = apf.Length == 1 ? (Array)GetF(apf[0], "m_Features") : null;
                var p0 = pd != null && pd.Length == 1 ? pd.GetValue(0) : null;
                var feat = p0 == null ? null : GetF(p0, "m_Feature") as BlueprintParametrizedFeatureReference;
                if (pet.ComponentsArray.Length != 1 || p0 == null || feat == null || feat.Get() == null || feat.Get().AssetGuid.ToString() != tiers[i].FeatGuid
                    || !s.Category.Equals(GetF(p0, "ParamWeaponCategory")) || apf[0].Disabled || !pet.HideInUI)
                    throw new InvalidOperationException("WeaponSpiritShaman: tier pet feature " + pet.name + " AddParametrizedFeatures readback failed (feat " + (feat == null ? "null" : GuidOfRef(feat)) + ")");
            }
        }

        // 선택지 설명에 적는 무기 값: 그 무기 블루프린트의 접근자(엔진이 전투에서 읽는 것과 같은 경로 — 아이템이 주사위를 덮어쓰면 덮어쓴 값, 아니면 무기 유형의 값)에서 읽는다.
        // 다른 모드가 무기 유형을 고쳤으면 고친 값이 그대로 나온다(표시 = 실제). 여기서는 판정하지 않고 날값만 옮긴다 — 설명이 적을 수 있는지는 순수 ChoiceText.Describe 가 정한다
        // (근접·원거리가 아닌 공격 종류, 중형이 아닌 크기 등은 그 선택지만 판정 불가 — 지적 7). 블루프린트·유형 자체가 없으면 「모름」이므로 던진다.
        // internal: 게임 안 점검 S26 choice-regenerated 가 같은 읽기로 지금의 무기 값에서 설명을 다시 만든다(Checks\TextChecks.cs — 표에 반영하지 않는다).
        internal static ChoiceRaw ChoiceOf(BlueprintItemWeapon w)
        {
            if (w == null) throw new ArgumentNullException("w", "WeaponSpiritShaman: choice description needs the weapon blueprint");
            if (w.Type == null) throw new InvalidOperationException("WeaponSpiritShaman: weapon " + w.name + " has no weapon type (choice description)");
            return new ChoiceRaw
            {
                Size = w.Size.ToString(),
                AttackType = w.AttackType.ToString(),
                Rolls = w.BaseDamage.Rolls,
                Sides = (int)w.BaseDamage.Dice,
                CritEdge = w.CriticalRollEdge,
                CritMultiplier = (int)w.CriticalModifier,
                ReachFeet = w.AttackRange.Value,
                TwoHanded = w.IsTwoHanded,
                Light = w.IsLight,
                Double = w.Double,
            };
        }

        // 판정 불가 선택지 하나를 모드 로그 오류 줄로(어느 범주 · 어느 값 · 어느 시점). 그 무기가 실제 전투에서 쓸 수 있는지는 여기서 판정하지 않는다(별도 문제 — 보고만).
        static void LogUndetermined(ChoiceOutcome o, Spirit s, string when)
        {
            ModContext.LogError("WeaponSpiritShaman: WSS choice text for " + o.Category + " is undetermined (" + when + "): " + o.Undetermined + " — start weapon " + s.Weapon.name
                + "; the choice shows an 'undetermined' description and the archetype still registers; whether this weapon itself is usable is not checked here");
        }

        // 첫 프레임 갱신 결과(로그용): 문구가 바뀐 범주 · 지금 판정 불가인 범주.
        internal sealed class ChoiceRefresh { public string[] Changed, Undetermined; }

        // 첫 프레임(ProgressionSync — 모든 모드의 BlueprintsCache.Init 포스트픽스가 끝난 뒤)에 한 번: 선택지 설명을 지금의 무기 블루프린트 값으로 다시 만든다.
        // BuildAll 은 다른 모드의 패치보다 먼저 돌 수 있다(2026-11-17 UMM 로그: 우리 빌드 뒤에 TabletopTweaks 「Patched:」 68줄) — 그 뒤에 무기 유형이 바뀌면 빌드 때 문구는 낡는다.
        // 원자적(지적 7): 59개를 전부 계산한 뒤(여기서 던지면 표는 빌드 때 그대로) StringsTable.ApplyChoices 가 한 번에 반영한다.
        // 판정 불가가 된 범주는 판정 불가 설명으로 바뀌고, 문구가 바뀐 판정 불가 범주마다 오류 줄 1개(빌드 때와 같은 문구면 빌드 때 이미 알렸다).
        // 던지면 호출자(ProgressionSync → FirstFrameSync)가 설명 갱신 실패로만 기록한다 — 펫 진행표 동기화 상태(Done)는 바뀌지 않는다.
        internal static ChoiceRefresh RefreshChoiceTexts()
        {
            if (!Built) throw new InvalidOperationException("WeaponSpiritShaman: RefreshChoiceTexts before SpiritBlueprints.Build");
            var outcomes = new ChoiceOutcome[Entries.Count];
            for (int i = 0; i < Entries.Count; i++)
            {
                var e = Entries[i];
                outcomes[i] = ChoiceText.Describe(e.Row.Category, e.Row.Double, ChoiceOf(e.Weapon), e.Row.Double ? ChoiceOf(e.Second) : null);
            }
            var changed = StringsTable.ApplyChoices(outcomes);
            for (int i = 0; i < outcomes.Length; i++)
                if (outcomes[i].Undetermined != null && changed.Contains(outcomes[i].Category)) LogUndetermined(outcomes[i], Entries[i], "first-frame refresh");
            return new ChoiceRefresh { Changed = changed, Undetermined = outcomes.Where(o => o.Undetermined != null).Select(o => o.Category).ToArray() };
        }

        // 선택지: WildlandShaman 동료 선택지의 설정값을 따르고, 컴포넌트는 우리 랭크 진행표 AddFeatureOnApply 하나(설계 §8 — 기승 표적·동료 아키타입 선택은 주지 않음).
        static BlueprintFeatureSelection BuildSelection()
        {
            var src = Get<BlueprintFeatureSelection>(Guids.Game.AnimalCompanionSelectionWildlandShaman);
            var first = src.ComponentsArray == null ? null : src.ComponentsArray.OfType<AddFeatureOnApply>().FirstOrDefault();
            if (first == null || first.Feature == null || first.Feature.AssetGuid.ToString() != Guids.Game.WildlandShamanAnimalCompanionProgression)
                throw new InvalidOperationException("WeaponSpiritShaman: wildland companion selection's first AddFeatureOnApply is not the rank progression");
            var refs = Entries.Select(e => Ref<BlueprintFeatureReference>(e.Feature.AssetGuid.ToString())).ToArray();
            var sel = Create<BlueprintFeatureSelection>("WeaponSpiritShamanSpiritSelection", Guids.SpiritSelection, x =>
            {
                x.Group = src.Group;
                x.Group2 = src.Group2;
                x.Mode = src.Mode;
                x.Obligatory = src.Obligatory;
                x.IgnorePrerequisites = src.IgnorePrerequisites;
                x.ExceptWhiteListed = src.ExceptWhiteListed;
                x.ShowThisSelection = src.ShowThisSelection;
                x.IsClassFeature = true;
                x.Ranks = 1;
                x.HideInUI = src.HideInUI;
                x.HideInCharacterSheetAndLevelUp = src.HideInCharacterSheetAndLevelUp;
                x.HideNotAvailibleInUI = src.HideNotAvailibleInUI;
                x.Groups = src.Groups == null ? new FeatureGroup[0] : src.Groups.ToArray();
                SetField(x, "m_DisplayName", Strings.L("SpiritSelection.Name"));
                SetField(x, "m_Description", Strings.L("SpiritSelection.Description"));
                SetField(x, "m_DescriptionShort", Strings.L("SpiritSelection.Description"));
                SetField(x, "m_Icon", GetF(src, "m_Icon"));
                SetField(x, "m_AllFeatures", refs);
                SetField(x, "m_Features", refs.ToArray());
            });
            var apply = new AddFeatureOnApply();
            SetField(apply, "m_Feature", Ref<BlueprintFeatureReference>(Guids.RankProgression));
            AddComponents(sel, apply);
            var back = sel.ComponentsArray.OfType<AddFeatureOnApply>().Select(a => a.Feature).ToArray();
            if (back.Length != 1 || !ReferenceEquals(back[0], RankProgression)) throw new InvalidOperationException("WeaponSpiritShaman: selection AddFeatureOnApply readback failed");
            var opts = sel.AllFeatures.ToArray();
            if (opts.Length != Entries.Count || opts.Where((o, i) => !ReferenceEquals(o, Entries[i].Feature)).Any()) throw new InvalidOperationException("WeaponSpiritShaman: selection options readback failed");
            return sel;
        }

         
        static void VerifyUnitFacts(Spirit e)
        {
            var facts = e.Unit.AddFacts.ToArray();
            var want = new[] { Guids.Game.Airborne, Guids.Game.ImmunityToProneCondition, SpiritRules.Dials.SpiritNaturalArmorGuid, Guids.SlotLockFeature, Guids.ProficiencyFeature, Guids.SpiritMirrorFeature };
            if (facts.Length != want.Length || facts.Any(f => f == null) || !facts.Select(f => f.AssetGuid.ToString()).SequenceEqual(want))
                throw new InvalidOperationException("WeaponSpiritShaman: unit " + e.Unit.name + " facts readback failed");
            if (facts.Any(f => f.AssetGuid.ToString() == Guids.Game.AnimalCompanionSlotFeature || f.AssetGuid.ToString() == Guids.Game.AnimalType))
                throw new InvalidOperationException("WeaponSpiritShaman: unit " + e.Unit.name + " has an animal lock/type fact");
        }

        // 무기 시각 객체가 실제로 쓸 모델 링크(get_Model @0000~003A 와 같은 규칙: 자기 링크가 비면 Prototype 의 것). 모델을 불러오지는 않는다(Build 중 자산 적재를 피함 — 실제 Model != null 은 점검 S3 weapon-model).
        internal static string ModelAssetId(WeaponVisualParameters vp)
        {
            var seen = new HashSet<WeaponVisualParameters>();
            for (; vp != null; vp = vp.Prototype)
            {
                if (!seen.Add(vp)) throw new InvalidOperationException("WeaponSpiritShaman: WeaponVisualParameters.Prototype chain has a cycle");
                var link = GetF(vp, "m_WeaponModel") as WeakResourceLink;
                if (link != null && !string.IsNullOrEmpty(link.AssetId)) return link.AssetId;
            }
            return null;
        }

        static string GuidOfRef(object r)
        {
            var b = r as BlueprintReferenceBase;
            if (r == null || b == null || b.Guid == BlueprintGuid.Empty) return null;
            return b.Guid.ToString();
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("WeaponSpiritShaman: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
