using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BlastMagus
{
     
     
     
    public static class BlastBlueprints
    {
        public static BlueprintAbilityResource BurnZero;
        public static BlueprintFeatureSelection ElementSelection;
        public static BlueprintFeature BlastFeature, BlastRankMarker, Composite20;
        public static BlueprintCharacterClass MagusClass;

        // 정찰 §1-1: MagusProgression 의 최고 레벨 항목 = 20(TrueMagusFeature). Build 가 진행표에서 되읽어 확인하고, 시험이 blueprints.zip 으로 확인한다.
        public const int MaxLevel = 20;
        // 설계 §5 레벨표: 원소 선택·블라스트는 1레벨.
        const int GrantLevel = 1;
        // CL → 주문 레벨: 본편 ContextCalculateAbilityParamsBasedOnClass.Calculate IL 00A8~00B9 (GetClassLevel(class) / 2 → ReplaceSpellLevel). BlastCasterLevel 이 같은 식을 쓴다.
        public const int SpellLevelDivisor = 2;

         
        const Element IconElement = Element.Fire;

         
        static ResolvedElement[] resolved;

        // BlastCasterLevel 이 시전자 레벨을 덮는 능력들: Build 가 해석 행의 단순 잎 + 복합 잎(중복 제거)을 채우고(설계 §11-6),
        // BlastStrike.Build 가 AddCasterLevelAbility 로 폭발의 SR 근거 능력(SourceAbility)을 더한다(컨트롤러 판정 5: SR 판정 CL = 메이거스 레벨).
        static BlueprintAbility[] blastAbilities;
        // ElementOf 의 판별 표: 원소 → 그 *BlastBase(본편 *BlastFeature 가 AddFeatureIfHasFact(Not) 로 주는 능력, 정찰 §9).
        static (Element element, BlueprintAbility baseAbility)[] elementBases;
        // 수정 웨이브 F2: 원소 → 그 복합 *BlastFeature(Composite20 이 AddFeatureIfHasFact 로 주는 것). BuildComposite 가 채운다. 폭발 주사위 ×2 판정용.
        static (Element element, BlueprintFeature feature)[] compositeFeatures;

        public static void Build()
        {
            if (Archetype.Bp == null) throw new InvalidOperationException("BlastMagus: BlastBlueprints.Build() before Archetype.Build() (AddAt needs the archetype)");
            if (BlastFeature != null) throw new InvalidOperationException("BlastMagus: BlastBlueprints.Build() called twice");
            MagusClass = Get<BlueprintCharacterClass>(GameGuids.MagusClass);
            var prog = Get<BlueprintProgression>(Guids.Game.MagusProgression);
            int top = prog.LevelEntries.Max(e => e.Level);
            if (top != MaxLevel) throw new InvalidOperationException("BlastMagus: MagusProgression highest level entry is " + top + ", expected " + MaxLevel + " (rank loop bound)");

            // --- 원소 모델(설계 §4.3-1a 0번 순서: 외부 계약 → 생성 → 우리 특성 계약). 위반이면 Resolve 가 위반 전부를 담아 던진다 = 1단계 BuildFailed. ---
             
            var rows = ElementTable.Vanilla;
            var res = new List<ResolvedElement>();
            foreach (var row in rows) res.Add(ElementModel.Resolve(row));
            resolved = res.ToArray();
            elementBases = resolved.Select(r => (r.Row.Id, r.Base)).ToArray();
            blastAbilities = resolved.Select(r => r.Ability).Concat(resolved.Select(r => r.CompositeAbility)).Distinct().ToArray();
            Sprite fireIcon = IconOf(ResolvedOf(IconElement).BlastFeature);

            BurnZero = MakeZeroResource();

            // --- 원소 특성(정찰 §9 권고, 설계 §11-1): 본편 진행표가 아니라 *BlastFeature 를 직접 준다. 행마다 하나, 만든 뒤 우리 특성 계약. ---
            var feats = new List<(Element, BlueprintFeature)>();
            foreach (var r in resolved)
            {
                var f = MakeFeature("BlastMagusElement" + r.Row.Id, r.Row.OurFeatureGuid, r.Row.NameKey, r.Row.DescriptionKey, ElementIconOf(r).icon);
                AddComponents(f, ElementFacts(r));
                ElementModel.CheckOurFeature(f, r.Row);
                feats.Add((r.Row.Id, f));
            }

            var elements = feats.Select(x => x.Item2).ToArray();
            ElementSelection = Create<BlueprintFeatureSelection>("BlastMagusElementSelection", Guids.ElementSelection, s =>
            {
                Names(s, "ElementSelection.Name", "ElementSelection.Description", fireIcon);
                s.IsClassFeature = true;
                s.Ranks = 1;
                s.Mode = SelectionMode.Default;
                s.Group = FeatureGroup.None;
                s.Group2 = FeatureGroup.None;
                // 본편 선택지(ElementalFocusSelection·WaterBlastSelection jbp)와 같게: m_Features = [], m_AllFeatures = 선택지 전부.
                SetField(s, "m_AllFeatures", elements.Select(e => Ref<BlueprintFeatureReference>(e.AssetGuid.ToString())).ToArray());
                SetField(s, "m_Features", new BlueprintFeatureReference[0]);
            });
            var allBack = (BlueprintFeatureReference[])GetF(ElementSelection, "m_AllFeatures");
            var featBack = (BlueprintFeatureReference[])GetF(ElementSelection, "m_Features");
            if (allBack == null || allBack.Length != elements.Length || !new HashSet<BlueprintFeature>(ElementSelection.AllFeatures).SetEquals(elements) || featBack == null || featBack.Length != 0
                || ElementSelection.Mode != SelectionMode.Default || ElementSelection.Group != FeatureGroup.None)
                throw new InvalidOperationException("BlastMagus: ElementSelection readback failed (m_AllFeatures must equal the " + elements.Length + " element features of the resolved rows, m_Features empty, Mode Default, no group)");

            BuildBlastFeature(fireIcon);

            // --- 랭크(설계 §11-2, 방법 B′): 본편 KineticBlastFeature 를 홀수 레벨마다 직접. 표식은 UI 용(같은 레벨에 하나씩) ---
            int rankCount = Enumerable.Range(1, MaxLevel).Count(BlastRules.RankLevel);
            if (rankCount != BlastRules.BlastRank(MaxLevel))
                throw new InvalidOperationException("BlastMagus: rank levels in 1.." + MaxLevel + " = " + rankCount + " but BlastRules.BlastRank(" + MaxLevel + ") = " + BlastRules.BlastRank(MaxLevel));
            var kbf = Get<BlueprintFeature>(Guids.Game.KineticBlastFeature);
            // 정찰 §3: KineticBlastFeature Ranks = 20. 우리가 주는 랭크 수보다 작으면 위쪽 레벨의 AddFact 가 랭크를 못 올린다 → 빌드 실패.
            if (kbf.Ranks < rankCount) throw new InvalidOperationException("BlastMagus: KineticBlastFeature.Ranks = " + kbf.Ranks + " < " + rankCount + " rank grants");
            BlastRankMarker = MakeFeature("BlastMagusBlastRankMarker", Guids.BlastRankMarker, "BlastRank.Name", "BlastRank.Description", fireIcon);
            BlastRankMarker.Ranks = rankCount;

            BuildComposite();

            // --- 레벨표(컨트롤러 판정 1: 이 Build 의 몫만) ---
            Archetype.AddAt(GrantLevel, ElementSelection);
            Archetype.AddAt(GrantLevel, BlastFeature);
            AddRanks(kbf);
            Archetype.AddAt(BlastRules.CompositeLevel, Composite20);

            // 되읽기: KineticBlastFeature 가 정확히 RankLevel 레벨들에, 각 1번(Archetype.AddAt 이 레벨별 1개를 이미 확인) → 레벨 집합 대조.
            var kbfLevels = Archetype.Bp.AddFeatures.Where(e => e.Features.Any(f => f == kbf)).Select(e => e.Level).OrderBy(x => x).ToArray();
            var expect = Enumerable.Range(1, MaxLevel).Where(BlastRules.RankLevel).ToArray();
            if (!kbfLevels.SequenceEqual(expect))
                throw new InvalidOperationException("BlastMagus: KineticBlastFeature archetype levels [" + string.Join(",", kbfLevels) + "] != BlastRules.RankLevel levels [" + string.Join(",", expect) + "]");
        }

        // 설계 §5: 랭크가 오르는 레벨마다 본편 KineticBlastFeature + 우리 표식.
        static void AddRanks(BlueprintFeature kbf)
        {
            for (int lv = 1; lv <= MaxLevel; lv++)
            {
                if (!BlastRules.RankLevel(lv)) continue; // legitimate no-op: ranks rise on odd levels 1..19 only (design §5, BlastRules.RankLevel)
                Archetype.AddAt(lv, kbf);
                Archetype.AddAt(lv, BlastRankMarker);
            }
        }

         
         
        // 그래서 (3) BlastPartGuard 의 OnTurnOn 은 (2) AddKineticistPart.OnTurnOn(part 생성, IL 005B~0061) 뒤에 돈다.
        static void BuildBlastFeature(Sprite icon)
        {
            var burn = Get<BlueprintFeature>(Guids.Game.BurnFeature);
            var src = burn.GetComponent<AddKineticistPart>();
            if (src == null) throw new InvalidOperationException("BlastMagus: BurnFeature has no AddKineticistPart (copy source for GatherPower*/m_Blasts)");
            BlastFeature = MakeFeature("BlastMagusBlastFeature", Guids.BlastFeature, "Blast.Name", "Blast.Description", icon);

            // (1) 0 자원(정찰 §4: 자원이 유닛에 없으면 AddKineticistPart.OnActivate 의 Restore 가 「has no resource」 오류 로그 → 같은 특성에 붙인다)
            var res = new AddAbilityResources { RestoreAmount = true };
            SetField(res, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.BurnZeroResource));
            if (res.Resource != BurnZero || !res.RestoreAmount) throw new InvalidOperationException("BlastMagus: AddAbilityResources(BurnZero) readback failed");

            // (2) 키네티시스트 part(정찰 §4 필드표·권고). MainStat = 지능(경로 1, 정찰 §2).
            var part = new AddKineticistPart { MainStat = StatType.Intelligence };
            SetField(part, "m_Class", Ref<BlueprintCharacterClassReference>(GameGuids.MagusClass));
            SetField(part, "m_MaxBurn", Ref<BlueprintAbilityResourceReference>(Guids.BurnZeroResource));
            SetField(part, "m_MaxBurnPerRound", Ref<BlueprintAbilityResourceReference>(Guids.BurnZeroResource));
            SetField(part, "m_GatherPowerAbility", SameAs<BlueprintAbilityReference>(src, "m_GatherPowerAbility", Guids.Game.GatherPowerAbility));
            SetField(part, "m_GatherPowerIncreaseFeature", SameAs<BlueprintFeatureReference>(src, "m_GatherPowerIncreaseFeature", Guids.Game.GatherPowerFeature));
            SetField(part, "m_GatherPowerBuff1", SameAs<BlueprintBuffReference>(src, "m_GatherPowerBuff1", Guids.Game.GatherPowerBuff1));
            SetField(part, "m_GatherPowerBuff2", SameAs<BlueprintBuffReference>(src, "m_GatherPowerBuff2", Guids.Game.GatherPowerBuff2));
            SetField(part, "m_GatherPowerBuff3", SameAs<BlueprintBuffReference>(src, "m_GatherPowerBuff3", Guids.Game.GatherPowerBuff3));
            SetField(part, "m_BladeActivatedBuff", SameAs<BlueprintBuffReference>(src, "m_BladeActivatedBuff", Guids.Game.KineticBladeEnableBuff));
            // 컨트롤러 판정 4: 본편 배열을 새 배열로 복사(원소 참조는 같은 객체).
            var srcBlasts = (BlueprintAbilityReference[])GetF(src, "m_Blasts");
            if (srcBlasts == null || srcBlasts.Length == 0) throw new InvalidOperationException("BlastMagus: BurnFeature.AddKineticistPart.m_Blasts is " + (srcBlasts == null ? "null" : "empty"));
            SetField(part, "m_Blasts", srcBlasts.ToArray());
            // 정찰 §4: Quiver·Shield 는 null 안전(GetQuiverResource @0x0006, Psychokineticist 선례). 명시적으로 비운다.
            SetField(part, "m_QuiverBuff", null);
            SetField(part, "m_CanGatherPowerWithShieldBuff", null);

            // 되읽기(컨트롤러 판정 3): m_MaxBurn/m_MaxBurnPerRound null 금지(정찰 §4: null 이면 TryGetValue(null) 예외, DarkCodex Finalizer 가 삼켜 조용히 0).
            if (GetF(part, "m_MaxBurn") == null || part.MaxBurn == null || part.MaxBurn != BurnZero)
                throw new InvalidOperationException("BlastMagus: AddKineticistPart.m_MaxBurn readback is null or not BurnZero");
            if (GetF(part, "m_MaxBurnPerRound") == null || part.MaxBurnPerRound == null || part.MaxBurnPerRound != BurnZero)
                throw new InvalidOperationException("BlastMagus: AddKineticistPart.m_MaxBurnPerRound readback is null or not BurnZero");
            if (part.MainStat != StatType.Intelligence || part.Class != MagusClass)
                throw new InvalidOperationException("BlastMagus: AddKineticistPart MainStat/m_Class readback failed (" + part.MainStat + ", " + (part.Class == null ? "null" : part.Class.name) + ")");
            if (part.GatherPowerAbility == null || part.GatherPowerAbility.AssetGuid.ToString() != Guids.Game.GatherPowerAbility
                || part.GatherPowerIncreaseFeature == null || part.GatherPowerIncreaseFeature.AssetGuid.ToString() != Guids.Game.GatherPowerFeature
                || part.GatherPowerBuff1 == null || part.GatherPowerBuff1.AssetGuid.ToString() != Guids.Game.GatherPowerBuff1
                || part.GatherPowerBuff2 == null || part.GatherPowerBuff2.AssetGuid.ToString() != Guids.Game.GatherPowerBuff2
                || part.GatherPowerBuff3 == null || part.GatherPowerBuff3.AssetGuid.ToString() != Guids.Game.GatherPowerBuff3
                || part.BladeActivatedBuff == null || part.BladeActivatedBuff.AssetGuid.ToString() != Guids.Game.KineticBladeEnableBuff)
                throw new InvalidOperationException("BlastMagus: AddKineticistPart GatherPower*/BladeActivatedBuff readback failed");
            var blastsBack = (BlueprintAbilityReference[])GetF(part, "m_Blasts");
            if (blastsBack == null || ReferenceEquals(blastsBack, srcBlasts) || blastsBack.Length != srcBlasts.Length || Enumerable.Range(0, srcBlasts.Length).Any(i => !ReferenceEquals(blastsBack[i], srcBlasts[i])))
                throw new InvalidOperationException("BlastMagus: AddKineticistPart.m_Blasts readback is not a new array with the " + srcBlasts.Length + " BurnFeature references (got " + (blastsBack == null ? "null" : blastsBack.Length.ToString()) + ")");
            if (GetF(part, "m_QuiverBuff") != null || GetF(part, "m_CanGatherPowerWithShieldBuff") != null)
                throw new InvalidOperationException("BlastMagus: AddKineticistPart m_QuiverBuff/m_CanGatherPowerWithShieldBuff readback is not null");

            // (3)(4) 우리 컴포넌트(인스턴스 필드 없음 — 블루프린트 컴포넌트는 모든 유닛이 공유한다)
            var guard = new BlastPartGuard();
            var casterLevel = new BlastCasterLevel();

            // (5) 판정 D1 기본안 (a): 블라스트 Base 의 AbilityShowIfCasterHasFact(ElementalFocusSelection) 표시 조건 충족(정찰 §9).
            var focus = Get<BlueprintFeatureSelection>(Guids.Game.ElementalFocusSelection);
            var showFact = Facts(focus);

            // (6) 판정 D2 기본안: 명중 능력치 지능(엔진 의미 = 지능 보너스 ≥ 현재 보너스일 때 교체, AttackStatReplacement IL 003E~004D).
            // CheckWeaponTypes = true 면 SubCategory 는 읽지 않는다(IL 0053~0079 분기).
             
            var wanted = ElementTable.BlastProjectileWeaponTypes;
            var weaponTypes = wanted.Select(g => Get<BlueprintWeaponType>(g)).ToArray();
            var atk = new AttackStatReplacement { ReplacementStat = StatType.Intelligence, CheckWeaponTypes = true };
            SetField(atk, "m_WeaponTypes", wanted.Select(g => Ref<BlueprintWeaponTypeReference>(g)).ToArray());
            var wtBack = (BlueprintWeaponTypeReference[])GetF(atk, "m_WeaponTypes");
            if (wtBack == null || wtBack.Length != wanted.Length || !new HashSet<string>(wtBack.Select(w => w.Guid.ToString())).SetEquals(wanted)
                || !new HashSet<BlueprintWeaponType>(wtBack.Select(w => w.Get())).SetEquals(weaponTypes) || atk.ReplacementStat != StatType.Intelligence || !atk.CheckWeaponTypes)
                throw new InvalidOperationException("BlastMagus: AttackStatReplacement readback failed (expected Intelligence on exactly [" + string.Join(", ", wanted) + "])");
            var uncovered = resolved.Where(r => !wanted.Contains(r.Row.ProjectileWeaponTypeGuid)).Select(r => r.Row.Id + ":" + r.Row.ProjectileWeaponTypeGuid).ToArray();
            if (uncovered.Length > 0)
                throw new InvalidOperationException("BlastMagus: AttackStatReplacement does not cover the projectile weapon type of [" + string.Join(", ", uncovered) + "]");

            AddComponents(BlastFeature, res, part, guard, casterLevel, showFact, atk);

            // 컴포넌트 수·형식 순서 되읽기(컨트롤러 판정 3)
            var expectTypes = new[] { typeof(AddAbilityResources), typeof(AddKineticistPart), typeof(BlastPartGuard), typeof(BlastCasterLevel), typeof(AddFacts), typeof(AttackStatReplacement) };
            var got = BlastFeature.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!got.SequenceEqual(expectTypes))
                throw new InvalidOperationException("BlastMagus: BlastFeature components [" + string.Join(", ", got.Select(t => t.Name)) + "] != [" + string.Join(", ", expectTypes.Select(t => t.Name)) + "]");
            if (!ReferenceEquals(BlastFeature.GetComponent<AddKineticistPart>(), part))
                throw new InvalidOperationException("BlastMagus: BlastFeature.GetComponent<AddKineticistPart>() is not the instance we built (BlastPartGuard compares against it)");
        }

        // 20레벨 복합(설계 §11-3, 정찰 §10): 원소 Base 를 가지면 그 복합 *BlastFeature 를 준다 + 복합 번 2 → 0.
        static void BuildComposite()
        {
            // 해석 행마다 (원소 Base → 그 복합 *BlastFeature). 번 감소 대상 = 복합 잎(중복 제거: 본편 Ice·Thunderstorm 은 두 원소가 공유 — T4).
            var compositeAbilities = resolved.Select(r => r.CompositeAbility).Distinct().ToArray();
            Composite20 = MakeFeature("BlastMagusComposite20", Guids.CompositeFeature20, "Composite20.Name", "Composite20.Description", IconOf(ResolvedOf(IconElement).CompositeFeature));

            var comps = new List<BlueprintComponent>();
            var composites = new List<(Element, BlueprintFeature)>();
            foreach (var r in resolved)
            {
                // C4: 복합 이름은 원소 표(BlastRules.CompositeOf = 행의 CompositeName)가 원장 → 본편 특성 이름이 그것으로 시작하는지 대조.
                if (!r.CompositeFeature.name.StartsWith(BlastRules.CompositeOf(r.Row.Id), StringComparison.Ordinal))
                    throw new InvalidOperationException("BlastMagus: composite feature for " + r.Row.Id + " is " + r.CompositeFeature.name + ", BlastRules.CompositeOf says " + BlastRules.CompositeOf(r.Row.Id));
                comps.Add(IfHas(r.Base, r.CompositeFeature));
                composites.Add((r.Row.Id, r.CompositeFeature));
            }
            compositeFeatures = composites.ToArray();
            // 되읽기: 해석 행(1단계 = 본편 행)의 원소마다 정확히 하나(ElementOf 와 같은 원소 집합).
            foreach (var r in resolved)
                if (compositeFeatures.Count(c => c.element == r.Row.Id) != 1)
                    throw new InvalidOperationException("BlastMagus: composite feature map has " + compositeFeatures.Count(c => c.element == r.Row.Id) + " entries for " + r.Row.Id + ", expected 1");
            comps.Add(BurnModifier(compositeAbilities));
            AddComponents(Composite20, comps.ToArray());
            if (Composite20.ComponentsArray.OfType<AddFeatureIfHasFact>().Count() != resolved.Length || Composite20.ComponentsArray.OfType<AddKineticistBurnModifier>().Count() != 1)
                throw new InvalidOperationException("BlastMagus: Composite20 component readback failed");
        }

        // 복합 번 2 → 0(설계 §4.3-3). 1단계(본편 복합)와 2단계(KEE 복합)가 같은 생성 지점을 쓴다(시험 `blast: AddKineticistBurnModifier.Value stored once`).
        // 정찰 §10: HandleKineticistCalculateAbilityCost IL 0019~0047 은 능력 또는 그 Parent 가 m_AppliableTo 에 있으면 적용, 음수면 Decrease(IL 0099~00B0).
        static AddKineticistBurnModifier BurnModifier(BlueprintAbility[] compositeAbilities)
        {
            if (compositeAbilities == null || compositeAbilities.Length == 0) throw new ArgumentException("BlastMagus: BurnModifier needs at least one composite ability");
            var burnMod = new AddKineticistBurnModifier { BurnType = KineticistBurnType.Blast, Value = -BlastRules.CompositeBurnReduction };
            SetField(burnMod, "m_AppliableTo", compositeAbilities.Select(a => Ref<BlueprintAbilityReference>(a.AssetGuid.ToString())).ToArray());
            var appBack = (BlueprintAbilityReference[])GetF(burnMod, "m_AppliableTo");
            if (appBack == null || appBack.Length != compositeAbilities.Length || Enumerable.Range(0, compositeAbilities.Length).Any(i => appBack[i].Get() != compositeAbilities[i])
                || burnMod.Value != -BlastRules.CompositeBurnReduction || burnMod.BurnType != KineticistBurnType.Blast || burnMod.UseContextValue)
                throw new InvalidOperationException("BlastMagus: AddKineticistBurnModifier readback failed (expected Blast, " + (-BlastRules.CompositeBurnReduction) + ", the " + compositeAbilities.Length + " composite abilities)");
            return burnMod;
        }

        static AddFeatureIfHasFact IfHas(BlueprintUnitFact checkedFact, BlueprintFeature feature)
        {
            var c = new AddFeatureIfHasFact { Not = false };
            SetField(c, "m_CheckedFact", Ref<BlueprintUnitFactReference>(checkedFact.AssetGuid.ToString()));
            SetField(c, "m_Feature", Ref<BlueprintUnitFactReference>(feature.AssetGuid.ToString()));
            if (c.CheckedFact != checkedFact || c.Feature != feature || c.Not)
                throw new InvalidOperationException("BlastMagus: AddFeatureIfHasFact readback failed for " + checkedFact.name + " -> " + feature.name);
            return c;
        }

        // 본편 BurnFeature 의 참조와 Guids.Game 의 GUID 가 같은지 확인하고 새 참조를 만든다(어긋나면 본편 데이터 또는 우리 표가 바뀐 것 → 실패).
        static TRef SameAs<TRef>(AddKineticistPart src, string field, string expectedGuid) where TRef : BlueprintReferenceBase, new()
        {
            var r = GetF(src, field) as BlueprintReferenceBase;
            if (r == null || r.IsEmpty()) throw new InvalidOperationException("BlastMagus: BurnFeature.AddKineticistPart." + field + " is empty");
            if (r.Guid.ToString() != expectedGuid) throw new InvalidOperationException("BlastMagus: BurnFeature.AddKineticistPart." + field + " = " + r.Guid + ", Guids.Game says " + expectedGuid);
            return Ref<TRef>(expectedGuid);
        }

        // 설계 §4.5/정찰 §5: 번 최대 0. UnitPartKineticist.MaxBurn 은 현재량(GetResourceAmount) → 최대 0 자원이면 0.
        static BlueprintAbilityResource MakeZeroResource()
        {
            var res = Create<BlueprintAbilityResource>("BlastMagusBurnZeroResource", Guids.BurnZeroResource, r =>
            {
                r.LocalizedName = Strings.L("Blast.Name");
                r.LocalizedDescription = Strings.L("Blast.Description");
                object amount = GetF(r, "m_MaxAmount"); // 새 블루프린트 기본값을 상자에 담은 복사본(Amount 는 비공개 중첩 구조체)
                SetField(amount, "BaseValue", 0);
                SetField(amount, "m_Class", new BlueprintCharacterClassReference[0]);
                SetField(amount, "m_Archetypes", new BlueprintArchetypeReference[0]);
                SetField(amount, "m_ClassDiv", new BlueprintCharacterClassReference[0]);
                SetField(amount, "m_ArchetypesDiv", new BlueprintArchetypeReference[0]);
                SetField(r, "m_MaxAmount", amount);
                SetField(r, "m_Max", 0);
            });
            var back = GetF(res, "m_MaxAmount");
            if ((int)GetF(back, "BaseValue") != 0 || (bool)GetF(back, "IncreasedByLevel") || (bool)GetF(back, "IncreasedByStat"))
                throw new InvalidOperationException("BlastMagus: BurnZero.m_MaxAmount readback is not a plain BaseValue 0");
            foreach (var arr in new[] { "m_Class", "m_Archetypes", "m_ClassDiv", "m_ArchetypesDiv" })
            {
                var a = GetF(back, arr) as Array;
                if (a == null || a.Length != 0) throw new InvalidOperationException("BlastMagus: BurnZero.m_MaxAmount." + arr + " readback is " + (a == null ? "null" : "length " + a.Length) + ", expected an empty array");
            }
            if ((int)GetF(res, "m_Max") != 0 || (bool)GetF(res, "m_UseMax"))
                throw new InvalidOperationException("BlastMagus: BurnZero m_Max/m_UseMax readback is not 0/false");
            return res;
        }

        // 해석 결과(BlastStrike 의 SR 기준 능력 등). Build 전이면 던진다. 매번 새 배열.
        public static ResolvedElement[] Resolved
        {
            get
            {
                if (resolved == null) throw new InvalidOperationException("BlastMagus: BlastBlueprints.Resolved before BlastBlueprints.Build()");
                return resolved.ToArray();
            }
        }

        internal static ResolvedElement ResolvedOf(Element e)
        {
            if (resolved == null) throw new InvalidOperationException("BlastMagus: ResolvedOf before the element model is resolved");
            foreach (var r in resolved) if (r.Row.Id == e) return r;
            throw new ArgumentOutOfRangeException("e", e, "BlastMagus: element " + e + " is not in the resolved element model");
        }

        // ================= 2단계 확장 API(설계 §4.3-4a B, 계획 T5) =================
        // KeeStage 가 StageTransaction 에 「적용 / 되돌림」 쌍으로 등록한다. 쓰기는 전부 새 배열(되돌림 = 스냅샷 배열 되쓰기), 대상은 우리 블루프린트·우리 정적 표뿐.

        public enum SnapshotPart { Tables, Composite, Selection }

        // 스냅샷(B 시작 전): 판별표·CL 대상·SR 기준표(= resolved 의 잎)·복합 표, Composite20.ComponentsArray, 선택지 m_AllFeatures. 배열 참조 그대로(쓰기는 항상 새 배열이라 원본이 바뀌지 않는다).
        public sealed class Snapshot
        {
            internal ResolvedElement[] Resolved;
            internal (Element element, BlueprintAbility baseAbility)[] Bases;
            internal BlueprintAbility[] ClAbilities;
            internal (Element element, BlueprintFeature feature)[] Composites;
            internal BlueprintComponent[] Composite20Components;
            internal BlueprintFeatureReference[] Selection;
        }

        public static Snapshot TakeSnapshot()
        {
            RequireBuilt("TakeSnapshot");
            return new Snapshot
            {
                Resolved = resolved, Bases = elementBases, ClAbilities = blastAbilities, Composites = compositeFeatures,
                Composite20Components = Composite20.ComponentsArray, Selection = (BlueprintFeatureReference[])GetF(ElementSelection, "m_AllFeatures"),
            };
        }

        static void RequireBuilt(string who)
        {
            if (resolved == null || elementBases == null || blastAbilities == null || compositeFeatures == null || Composite20 == null || ElementSelection == null)
                throw new InvalidOperationException("BlastMagus: BlastBlueprints." + who + " before BlastBlueprints.Build()");
        }

        static void RequireKee(ResolvedElement[] kee, string who)
        {
            if (kee == null || kee.Length == 0) throw new ArgumentException("BlastMagus: BlastBlueprints." + who + " needs the resolved KEE rows");
            if (kee.Any(r => r == null || r.Row.Source != ElementSource.Kee)) throw new ArgumentException("BlastMagus: BlastBlueprints." + who + " got a non-KEE or null row");
            if (kee.Any(r => resolved.Any(x => x.Row.Id == r.Row.Id))) throw new InvalidOperationException("BlastMagus: BlastBlueprints." + who + ": a KEE element is already in the resolved model");
        }

        // A 검증 단계(쓰기 0)의 KEE 쪽 추가 확인: 복합 이름 접두(C4 — BuildComposite 와 같은 규칙), 잎이 변형 부모 아님(SR 기준 능력), 원소 특성 아이콘 출처(ElementIconOf), 문자열 키.
        // (행 투사체 형식 ⊂ D2 집합은 표의 순수 사실이라 오프라인 시험 `wiring: AttackStatReplacement covers both projectiles` 가 11행 전부를 고정하고, 실제 투사체는 외부 계약 7번이 본다.)
        internal static void CheckKeeCandidates(ResolvedElement[] kee)
        {
            RequireBuilt("CheckKeeCandidates");
            RequireKee(kee, "CheckKeeCandidates");
            foreach (var r in kee)
            {
                if (!r.CompositeFeature.name.StartsWith(BlastRules.CompositeOf(r.Row.Id), StringComparison.Ordinal))
                    throw new InvalidOperationException("BlastMagus: composite feature for " + r.Row.Id + " is " + r.CompositeFeature.name + ", BlastRules.CompositeOf says " + BlastRules.CompositeOf(r.Row.Id));
                if (r.Ability.HasVariants) throw new InvalidOperationException("BlastMagus: " + r.Ability.name + " has variants (cannot be the blast strike SR reference ability)");
                ElementIconOf(r); // 출처 없음(None)이면 던진다 = A 단계 실패. INFO 줄은 실제로 쓰는 CreateElementFeature 가 남긴다.
                if (!StringsTable.Table.ContainsKey(r.Row.NameKey) || !StringsTable.Table.ContainsKey(r.Row.DescriptionKey))
                    throw new InvalidOperationException("BlastMagus: " + r.Row.Id + " string keys " + r.Row.NameKey + " / " + r.Row.DescriptionKey + " missing from StringsTable");
            }
        }

        // 쓰기 1(행 하나): 우리 원소 특성 생성(BlueprintUtil.Create — 캐시 등록이 즉시 일어난다). 되돌림 = RemoveKeeFeature.
        internal static BlueprintFeature CreateElementFeature(ResolvedElement r)
        {
            RequireBuilt("CreateElementFeature");
            RequireKee(new[] { r }, "CreateElementFeature");
            var picked = ElementIconOf(r);
            ModContext.Log(ElementIcon.InfoLine(r.Row.Id.ToString(), picked.source, picked.source == IconSource.Base ? r.Base.name : r.BlastFeature.name));
            var f = MakeFeature("BlastMagusElement" + r.Row.Id, r.Row.OurFeatureGuid, r.Row.NameKey, r.Row.DescriptionKey, picked.icon);
            AddComponents(f, ElementFacts(r));
            return f; // 자식 토글 해석(Energize.VerifyGrant)은 2단계 최종 되읽기(KeeStage.FinalCheck)가 본다 — 실패면 전체 되돌림

        }

        // 쓰기 1 의 되돌림(설계 §4.3-4a B.1): 엔진 BlueprintsCache.RemoveCachedBlueprint — 2단계가 만드는 KEE 원소 특성 GUID 에만(KeeChecks.RequireRemovable 이 먼저 확인).
        // 공용 ownGuids 집합에는 남지만 AddComponents 허용 목록일 뿐이라 무해(설계 같은 줄). 캐시에 없으면 엔진 Dictionary.Remove 가 아무것도 하지 않는다(IL_0007).
        internal static void RemoveKeeFeature(ElementRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            KeeChecks.RequireRemovable(row.OurFeatureGuid);
            ResourcesLibrary.BlueprintsCache.RemoveCachedBlueprint(BlueprintGuid.Parse(row.OurFeatureGuid));
        }

        // 쓰기 2: 판별표(ElementOf)·CL 대상(IsBlastAbility: 단순 잎 + 복합 잎)·SR 기준표/해석 표(resolved)·복합 표에 KEE 행 추가(새 배열).
        public static void ApplyKeeTables(ResolvedElement[] kee)
        {
            RequireBuilt("ApplyKeeTables");
            RequireKee(kee, "ApplyKeeTables");
            resolved = resolved.Concat(kee).ToArray();
            elementBases = elementBases.Concat(kee.Select(r => (r.Row.Id, r.Base))).ToArray();
            blastAbilities = blastAbilities.Concat(kee.Select(r => r.Ability).Concat(kee.Select(r => r.CompositeAbility)).Distinct().Where(a => !blastAbilities.Contains(a))).ToArray();
            compositeFeatures = compositeFeatures.Concat(kee.Select(r => (r.Row.Id, r.CompositeFeature))).ToArray();
        }

        // 쓰기 3: Composite20 에 KEE 원소마다 AddFeatureIfHasFact(Base → 복합 특성) + 두 번째 번 수정자(KEE 복합 잎). AddComponents 는 새 배열을 만든다(BlueprintUtil.cs AddComponents).
        public static void ApplyCompositeComponents(ResolvedElement[] kee)
        {
            RequireBuilt("ApplyCompositeComponents");
            if (kee == null || kee.Length == 0 || kee.Any(r => r == null || r.Row.Source != ElementSource.Kee)) throw new ArgumentException("BlastMagus: ApplyCompositeComponents needs the resolved KEE rows");
            var comps = new List<BlueprintComponent>();
            foreach (var r in kee) comps.Add(IfHas(r.Base, r.CompositeFeature));
            comps.Add(BurnModifier(kee.Select(r => r.CompositeAbility).Distinct().ToArray()));
            AddComponents(Composite20, comps.ToArray());
        }

        // 쓰기 4(마지막): 선택지 m_AllFeatures = 지금 목록 + 만든 KEE 원소 특성(새 배열).
        public static void ApplySelection(BlueprintFeature[] made)
        {
            RequireBuilt("ApplySelection");
            if (made == null || made.Length == 0 || made.Any(f => f == null)) throw new ArgumentException("BlastMagus: ApplySelection needs every created KEE element feature (a creation step did not run?)");
            var cur = (BlueprintFeatureReference[])GetF(ElementSelection, "m_AllFeatures");
            if (cur == null) throw new InvalidOperationException("BlastMagus: ElementSelection.m_AllFeatures is null");
            SetField(ElementSelection, "m_AllFeatures", cur.Concat(made.Select(f => Ref<BlueprintFeatureReference>(f.AssetGuid.ToString()))).ToArray());
        }

        public static void Restore(Snapshot s, SnapshotPart part)
        {
            if (s == null) throw new ArgumentNullException("s");
            switch (part)
            {
                case SnapshotPart.Tables:
                    resolved = s.Resolved; elementBases = s.Bases; blastAbilities = s.ClAbilities; compositeFeatures = s.Composites;
                    break;
                case SnapshotPart.Composite:
                    Composite20.ComponentsArray = s.Composite20Components;
                    break;
                case SnapshotPart.Selection:
                    SetField(ElementSelection, "m_AllFeatures", s.Selection);
                    break;
                default: throw new ArgumentOutOfRangeException("part", part, "BlastMagus: undefined SnapshotPart");
            }
        }

        // 되돌림 뒤 되읽기(설계 §4.3-4a 커밋 지점): 모든 표·배열이 스냅샷과 같은 객체·같은 길이. null = 일치, 아니면 사유.
        public static string VerifySnapshot(Snapshot s)
        {
            if (s == null) throw new ArgumentNullException("s");
            var bad = new List<string>();
            if (!ReferenceEquals(resolved, s.Resolved) || resolved.Length != s.Resolved.Length) bad.Add("resolved table");
            if (!ReferenceEquals(elementBases, s.Bases) || elementBases.Length != s.Bases.Length) bad.Add("element bases");
            if (!ReferenceEquals(blastAbilities, s.ClAbilities) || blastAbilities.Length != s.ClAbilities.Length) bad.Add("caster-level abilities");
            if (!ReferenceEquals(compositeFeatures, s.Composites) || compositeFeatures.Length != s.Composites.Length) bad.Add("composite table");
            if (!ReferenceEquals(Composite20.ComponentsArray, s.Composite20Components) || Composite20.ComponentsArray.Length != s.Composite20Components.Length) bad.Add("Composite20 components");
            var sel = (BlueprintFeatureReference[])GetF(ElementSelection, "m_AllFeatures");
            if (!ReferenceEquals(sel, s.Selection) || sel.Length != s.Selection.Length) bad.Add("ElementSelection.m_AllFeatures");
            return bad.Count == 0 ? null : "differs from the snapshot: " + string.Join(", ", bad.ToArray());
        }

        // 최종 되읽기(설계 §4.3-4a B.5): 선택지 == 해석 행의 우리 특성(expected 개), 판별표·CL·복합·Composite20 이 같은 행 집합. null = 일치, 아니면 사유.
        public static string VerifyElements(int expected)
        {
            RequireBuilt("VerifyElements");
            var bad = new List<string>();
            var ids = resolved.Select(r => r.Row.Id).ToArray();
            if (resolved.Length != expected || ids.Distinct().Count() != expected) bad.Add("resolved " + resolved.Length + " rows");
            var sel = ElementSelection.AllFeatures.Select(f => f == null ? "null" : f.AssetGuid.ToString()).ToArray();
            if (sel.Length != expected || !new HashSet<string>(sel).SetEquals(resolved.Select(r => r.Row.OurFeatureGuid))) bad.Add("selection [" + sel.Length + "] != our features of the resolved rows");
            if (elementBases.Length != expected || !new HashSet<Element>(elementBases.Select(b => b.element)).SetEquals(ids) || elementBases.Any(b => b.baseAbility != ResolvedOf(b.element).Base)) bad.Add("element bases");
            if (compositeFeatures.Length != expected || !new HashSet<Element>(compositeFeatures.Select(c => c.element)).SetEquals(ids)) bad.Add("composite table");
            if (resolved.Any(r => !IsBlastAbility(r.Ability) || !IsBlastAbility(r.CompositeAbility) || r.Ability.HasVariants)) bad.Add("caster-level / SR reference abilities");
            var c20 = Composite20.ComponentsArray;
            var ifHas = c20.OfType<AddFeatureIfHasFact>().ToArray();
            var burns = c20.OfType<AddKineticistBurnModifier>().ToArray();
            var covered = new HashSet<BlueprintAbility>(burns.SelectMany(b => ((BlueprintAbilityReference[])GetF(b, "m_AppliableTo")).Select(x => x.Get())));
            if (ifHas.Length != expected || resolved.Any(r => ifHas.Count(c => c.CheckedFact == r.Base && c.Feature == r.CompositeFeature) != 1)
                || resolved.Any(r => !covered.Contains(r.CompositeAbility)) || c20.Any(c => !ReferenceEquals(c.OwnerBlueprint, Composite20)))
                bad.Add("Composite20 (" + ifHas.Length + " AddFeatureIfHasFact, " + burns.Length + " burn modifiers)");
            return bad.Count == 0 ? null : string.Join("; ", bad.ToArray());
        }

        // 원소 판별(설계 §4.8): *BlastBase 보유로. 0 개(블라스트 없음) 또는 2 개 이상(키네티시스트 멀티 등)이면 판정 불가 → 던진다.
        // 폭발 경로(BlastStrike)가 쓴다 — 명중 시에만 돌아 핫패스가 아니므로 예전대로 매번 던진다. 판정 규칙(정확히 1개)은 ElementVerdict.Of 한 곳(C4).
        public static Element ElementOf(UnitEntityData u)
        {
            var v = ElementVerdict.Of(BasesOf(u, "ElementOf"));
            if (!v.IsDetermined)
                throw new InvalidOperationException("BlastMagus: " + u.CharacterName + " has " + v.Have.Length + " element blast bases (" + string.Join(",", v.Have) + "), expected exactly 1");
            return v.Element;
        }

        // (2026-11-15) 에너지 주입은 원소를 판별하지 않는다 — 원소마다 자식 토글·버프가 있고 EnergizeWeapon 은 자기 원소로 고정(Energize.cs). JudgeElement·ElementJudge 제거.

        // 판별표 순서대로, 유닛이 가진 *BlastBase 의 원소.
        static Element[] BasesOf(UnitEntityData u, string caller)
        {
            if (u == null) throw new ArgumentNullException("u");
            if (elementBases == null) throw new InvalidOperationException("BlastMagus: " + caller + " before BlastBlueprints.Build()");
            return elementBases.Where(p => u.Descriptor.HasFact(p.baseAbility)).Select(p => p.element).ToArray();
        }

        // 수정 웨이브 F2: 그 원소의 복합 *BlastFeature 보유(폭발 주사위 ×2, 설계 §4.3). 우리 Composite20 보유와 어긋나면 배선 결함 → 던진다
        // (Composite20 이 있는데 복합 특성이 없으면 20레벨 폭발이 조용히 랭크 주사위로 떨어지고, 반대면 남의 복합으로 2배가 된다).
        public static bool HasCompositeBlast(UnitEntityData u, Element e)
        {
            if (u == null) throw new ArgumentNullException("u");
            if (compositeFeatures == null || Composite20 == null) throw new InvalidOperationException("BlastMagus: HasCompositeBlast before BlastBlueprints.Build()");
            BlueprintFeature feature = null;
            foreach (var c in compositeFeatures) if (c.element == e) feature = c.feature;
            if (feature == null) throw new ArgumentOutOfRangeException("e", e, "BlastMagus: no composite feature for element");
            bool hasComposite = u.Descriptor.HasFact(feature);
            bool hasOurs = u.Descriptor.HasFact(Composite20);
            if (hasComposite != hasOurs)
                throw new InvalidOperationException("BlastMagus: " + u.CharacterName + " has " + feature.name + "=" + hasComposite + " but " + Composite20.name + "=" + hasOurs + " (composite wiring mismatch)");
            return hasComposite;
        }

         
        public static void AddCasterLevelAbility(BlueprintAbility a)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (blastAbilities == null) throw new InvalidOperationException("BlastMagus: AddCasterLevelAbility before BlastBlueprints.Build()");
            if (blastAbilities.Contains(a)) throw new InvalidOperationException("BlastMagus: " + a.name + " is already a caster-level ability");
            blastAbilities = blastAbilities.Concat(new[] { a }).ToArray();
            if (!IsBlastAbility(a)) throw new InvalidOperationException("BlastMagus: AddCasterLevelAbility readback failed for " + a.name);
        }

        // BlastCasterLevel 이 쓰는 판정(6종 + AddCasterLevelAbility 로 더한 것). 능력 자체 또는 그 Parent 가 목록에 있으면 true.
        // Parent 분기는 현재 해당 없음(6종 모두 변형 잎이고 목록에 Base 가 없다) — 목록에 Base 를 넣게 되면 그 변형까지 덮도록 남겨 둔 방어 분기.
        public static bool IsBlastAbility(BlueprintAbility a)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (blastAbilities == null) throw new InvalidOperationException("BlastMagus: IsBlastAbility before BlastBlueprints.Build()");
            return blastAbilities.Contains(a) || (a.Parent != null && blastAbilities.Contains(a.Parent));
        }

        // 우리 원소 특성(선택지 항목)의 아이콘(핫픽스 1): 출처 순서는 순수 코어 ElementIcon(BlastContract.cs)이 정하고 여기는 따른다. 출처 없음 = 던진다(조용한 null 금지).
        // 본편 행 = 그 *BlastFeature 아이콘만(행동 불변), KEE 행 = *BlastFeature → *BlastBase(KEE 특성은 아이콘 null — ElementIcon 주석의 IL 근거).
        internal static (Sprite icon, IconSource source) ElementIconOf(ResolvedElement r)
        {
            if (r == null) throw new ArgumentNullException("r");
            Sprite fi = r.BlastFeature.Icon, bi = r.Base.Icon;
            var source = ElementIcon.Pick(r.Row.Source, fi != null, bi != null);
            switch (source)
            {
                case IconSource.Feature: return (fi, source);
                case IconSource.Base: return (bi, source);
                case IconSource.None:
                    throw new InvalidOperationException("BlastMagus: " + r.BlastFeature.name + " has no icon"
                        + (ElementIcon.OrderFor(r.Row.Source).Contains(IconSource.Base) ? " (nor its base ability " + r.Base.name + ")" : ""));
                default: throw new InvalidOperationException("BlastMagus: undefined IconSource " + source);
            }
        }

        internal static Sprite IconOf(BlueprintUnitFact f)
        {
            var icon = f.Icon;
            if (icon == null) throw new InvalidOperationException("BlastMagus: " + f.name + " has no icon");
            return icon;
        }

        internal static BlueprintFeature MakeFeature(string name, string guid, string nameKey, string descKey, Sprite icon)
        {
            var made = Create<BlueprintFeature>(name, guid, f =>
            {
                Names(f, nameKey, descKey, icon);
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
            // 2026-11-16: 2단계(KEE 원소 특성)는 BuildAll 끝의 전수 검사 뒤에 만들어지므로 특성은 만든 자리에서도 본다(null LocalizedString 이면 던진다).
            RequireLocalizedStrings(made);
            return made;
        }

        internal static void Names(BlueprintUnitFact f, string nameKey, string descKey, Sprite icon)
        {
            if (icon == null) throw new ArgumentNullException("icon");
            var displayName = Strings.L(nameKey);
            var description = Strings.L(descKey);
            var descriptionShort = Strings.L(descKey);
            SetField(f, "m_DisplayName", displayName);
            SetField(f, "m_Description", description);
            SetField(f, "m_DescriptionShort", descriptionShort);
            SetField(f, "m_Icon", icon);
            if (!ReferenceEquals(GetF(f, "m_DisplayName"), displayName) || !ReferenceEquals(GetF(f, "m_Description"), description)
                || !ReferenceEquals(GetF(f, "m_DescriptionShort"), descriptionShort) || !ReferenceEquals(GetF(f, "m_Icon"), icon))
                throw new InvalidOperationException("BlastMagus: " + f.name + " name/description/icon readback failed");
        }

        // 우리 원소 특성의 AddFacts = [그 *BlastFeature, 그 원소의 에너지 주입 자식 토글](BlastContract.CheckOurFeature 계약, 자식 GUID = EnergizeTable — C4).
        // 1단계 본편 행은 자식 토글을 Energize.Build 가 이 뒤에 만들므로 여기서는 GUID 참조만 되읽는다(해석은 Energize.Build·CreateElementFeature 의 Energize.VerifyGrant).
        internal static AddFacts ElementFacts(ResolvedElement r)
        {
            if (r == null) throw new ArgumentNullException("r");
            string blast = r.BlastFeature.AssetGuid.ToString(), toggle = EnergizeTable.Of(r.Row.Id).ToggleGuid;
            var c = new AddFacts();
            SetField(c, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(blast), Ref<BlueprintUnitFactReference>(toggle) });
            var back = (BlueprintUnitFactReference[])GetF(c, "m_Facts");
            if (back == null || back.Length != 2 || back[0].Guid.ToString() != blast || back[1].Guid.ToString() != toggle)
                throw new InvalidOperationException("BlastMagus: element " + r.Row.Id + " AddFacts reference readback failed (expected [" + blast + "," + toggle + "])");
            return c;
        }

        internal static AddFacts Facts(BlueprintUnitFact fact)
        {
            var c = new AddFacts();
            SetField(c, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(fact.AssetGuid.ToString()) });
            if (c.Facts.Count() != 1 || c.Facts.First() != fact) throw new InvalidOperationException("BlastMagus: AddFacts readback failed for " + fact.name);
            return c;
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("BlastMagus: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
