using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BlastMagus
{
     
    //   부모 토글(EnergizeParent): ActivatableAbilityVariants[원소 자식 토글 11개] + ActivationDisable, 버프 없음 — 눌러서 원소를 고르는 메뉴(본편 EnergizedWildShapeToggleAbility 0344dab8… 와 같은 모양).
    //   자식 토글(원소마다, HiddenInUI): 그 원소의 버프 → EnergizeWeapon(원소 고정). 전부 같은 활성 그룹 → 한 번에 하나만 켜진다(그룹 크기 1, 엔진 OnDidTurnOn).
    //   부여: EnergizeFeature(1레벨) = AddFacts[부모], 우리 원소 특성 = AddFacts[*BlastFeature, 그 원소 자식 토글](BlastContract.CheckOurFeature).
    //   → 원소 2개 이상(ToyBox featsMultiplier 등)이면 가진 원소마다 자식 토글이 생기고 플레이어가 하나를 고른다. 아무것도 안 켜져 있으면 주입 없음(UI 에 보인다).
    // 기본 켜짐(EnergizeDefaultChoice, EnergizeTable.OnGrant): 자식 토글이 붙은 직후 유닛의 자식 토글이 1개면 그것을 켜고, 2개 이상이면 전부 끈다
    //   (예전 단일 토글 = 원소 1개면 기본 켜짐, 2개 이상이면 판정 불가 → 주입 없음 과 같은 결과).
    // 주사위 수 = BlastRules.EnergizeDice(메이거스 레벨) — 7·13·19 특성은 설명 전용. 복합 2배 없음. 근접 제작 무기만. 번 없음. 레벨표 1·7·13·19 는 이 Build 가 직접 AddAt.
    //
    // 예전 세이브(2026-11-15 이전, 단일 토글 ddc21826… / 버프 84c3fa52…): 두 GUID 는 그대로 남긴다(LegacyToggle/LegacyBuff — 블루프린트가 없으면 로드가 그 사실을 오류와 함께 버린다,
    //   EntityFactsManager.PostLoad IL 0044~0082). 로드 시 일어나는 일(엔진 IL 근거):
    //   1) EnergizeFeature 의 AddFacts(같은 컴포넌트 이름 #0)가 ApplyPostLoadFixes → UpdateFacts 에서 목록에 없는 예전 토글을 RemoveFact, 부모 토글을 더한다
    //      (AddFacts.UpdateFacts IL 0039~0142·0187~01E0). 예전 토글이 행동 막대에 있으면 엔진이 지우지 않고 끄고(IsOn=false → 버프 제거) TemporarilyDisabled 로 남긴다
    //      (ActivatableAbilityCollection.PrepareFactForDetach IL 001A~003B). 예전 토글은 HiddenInUI, 예전 버프는 피해 컴포넌트가 없다(무해).
    //   2) 원소 특성의 AddFacts(#0)가 같은 경로로 그 원소 자식 토글을 더한다 → EnergizeDefaultChoice: 원소 1개면 켜짐, 2개 이상이면 전부 꺼짐.
    //   3) 예전 버프가 로드되면 LegacyEnergizeMarker 가 유닛당 1회 INFO 를 남긴다(그 뒤 1)이 버프를 지운다).
    public static class Energize
    {
        public static BlueprintActivatableAbility Parent, LegacyToggle;
        public static BlueprintBuff LegacyBuff;
        public static BlueprintFeature Feature, F7, F13, F19;

        // 설계 §5 레벨표: 에너지 주입 1레벨.
        const int GrantLevel = 1;

        // 활성 그룹: 본편 ActivatableAbilityGroup 에 빈 값이 없다 — blueprints.zip 2026-11-15 전수(BlueprintActivatableAbility 의 Group): None 외 58개 값이 전부 본편에서 쓰인다.
        // 임의 정수 값은 쓸 수 없다: UnitPartActivatableAbility 의 그룹 크기 배열 = EnumUtils.GetMaxValue + 1 칸(ctor IL 0001~000B), GetGroupSize = 배열[group](IL 0000~0007) → 범위 밖 예외.
        // 그래서 본편 Energized Wild Shape 의 그룹(5개 블루프린트: 그 부모·자식 4개, 그룹 크기 증가 0개)을 함께 쓴다 — 같은 유닛이 그 특기도 가지면 서로 끈다.
        // 그 경우와 그룹 크기 > 1(다른 모드의 IncreaseActivatableAbilityGroupSize)은 EnergizeWeapon.OnActivate 의 AuditGroup 이 오류 로그로 드러낸다.
        public const ActivatableAbilityGroup Group = ActivatableAbilityGroup.EnergizedWildShape;
        // 본편 Energized Wild Shape 토글들의 WeightInGroup(jbp 직독 2026-11-15) — 그룹 크기 1 에 하나만 들어간다.
        const int WeightInGroup = 1;

        // 원소 → (자식 토글, 자식 버프). EnergizeTable 의 행 순서(= Element enum 순서).
        static readonly Dictionary<Element, (BlueprintActivatableAbility toggle, BlueprintBuff buff)> children = new Dictionary<Element, (BlueprintActivatableAbility, BlueprintBuff)>();

        public static void Build()
        {
            if (Archetype.Bp == null) throw new InvalidOperationException("BlastMagus: Energize.Build() before Archetype.Build() (AddAt needs the archetype)");
            if (BlastBlueprints.BlastFeature == null || BlastBlueprints.ElementSelection == null) throw new InvalidOperationException("BlastMagus: Energize.Build() before BlastBlueprints.Build() (icon, element features, MagusClass)");
            if (Parent != null || children.Count != 0) throw new InvalidOperationException("BlastMagus: Energize.Build() called twice");
            // 아이콘 = 블라스트 특성 아이콘(본편 FireBlastFeature 의 것, BlastBlueprints.Build 가 null 이면 이미 던졌다).
            Sprite icon = BlastBlueprints.BlastFeature.Icon;
            if (icon == null) throw new InvalidOperationException("BlastMagus: BlastFeature has no icon (Energize icon source)");

            // --- 원소 자식(버프 + 숨은 토글). 1단계에서 11개 전부 만든다(KEE 원소 것도): 자식은 원소 표(ElementTable·EnergizeTable)와 우리 블루프린트만 쓰고
            //     KEE 블루프린트를 참조하지 않는다. KEE 가 없으면 KEE 자식은 아무도 부여하지 않는다(변형 목록은 가진 것만 보인다 — ActivatableAbilityVariants.GetConversions IL 0045~00B3).
            //     아이콘: 본편 원소 = 그 원소 특성 아이콘(BlastBlueprints.ElementIconOf), KEE 원소 = 부모 아이콘(1단계에 KEE 블루프린트가 없다 — 외형 한계). ---
            foreach (var row in EnergizeTable.All)
            {
                var el = ElementTable.Row(row.Id);
                Sprite childIcon = el.Source == ElementSource.Vanilla ? BlastBlueprints.ElementIconOf(BlastBlueprints.ResolvedOf(row.Id)).icon : icon;
                var buff = Create<BlueprintBuff>("BlastMagusEnergizeBuff" + row.Id, row.BuffGuid, b =>
                {
                    BlastBlueprints.Names(b, row.NameKey, "Energize.Description", childIcon);
                    b.IsClassFeature = true;
                    b.Stacking = StackingType.Replace;
                    // FX(2026-11-15 승인, FxTable): 원본이 있는 원소만 FxOnStart(켜져 있는 동안 유지). 없는 원소 = null(FxTable.EnergizeNoFx 에 이유) — 원본이 표와 다르면 BlastFx 가 던진다.
                    b.FxOnStart = BlastFx.EnergizeFxOnStart(row.Id);
                    // 2026-11-16 결함(카오스 워리어에서 발견, 같은 원인): 엔진 Buff.OnRemove IL_00A2~00B7 은 FxOnStart 로 입자를 띄운 버프를 지울 때
                    // Blueprint.FxOnRemove.Load 를 null 검사 없이 부른다 → 코드로 만든 버프의 null FxOnRemove 면 NRE(GameLogFull: 에너지 주입 토글 Stop 경로 1건).
                    // 본편 jbp 는 null 이어도 읽을 때 빈 링크가 된다 — 우리는 빈 링크를 직접 넣는다.
                    b.FxOnRemove = new Kingmaker.ResourceLinks.PrefabLink();
                    b.ResourceAssetIds = BlastFx.EnergizeResourceIds(row.Id);
                });
                if (buff.FxOnRemove == null) throw new InvalidOperationException("BlastMagus: " + buff.name + " FxOnRemove is null (engine Buff.OnRemove would NRE on removal)");
                var weapon = new EnergizeWeapon(row.Id);
                AddComponents(buff, weapon);
                if (!buff.IsClassFeature || buff.Stacking != StackingType.Replace || buff.ComponentsArray.Length != 1 || !ReferenceEquals(buff.ComponentsArray[0], weapon) || weapon.Element != row.Id)
                    throw new InvalidOperationException("BlastMagus: " + buff.name + " readback failed (IsClassFeature, Stacking Replace, exactly one EnergizeWeapon(" + row.Id + ") expected)");
                var fxWant = FxTable.ForEnergize(row.Id);
                string fxGot = buff.FxOnStart == null ? null : buff.FxOnStart.AssetId;
                if (fxGot != (fxWant == null ? null : fxWant.AssetId) || buff.ResourceAssetIds == null || !buff.ResourceAssetIds.SequenceEqual(fxWant == null ? new string[0] : new[] { fxWant.AssetId }))
                    throw new InvalidOperationException("BlastMagus: " + buff.name + " FX readback failed (FxOnStart " + (fxGot ?? "null") + ", FxTable " + (fxWant == null ? "none" : fxWant.AssetId) + ")");

                var toggle = Create<BlueprintActivatableAbility>("BlastMagusEnergizeToggle" + row.Id, row.ToggleGuid, t =>
                {
                    BlastBlueprints.Names(t, row.NameKey, "Energize.Description", childIcon);
                    SetField(t, "m_Buff", Ref<BlueprintBuffReference>(row.BuffGuid));
                    t.Group = Group;
                    t.WeightInGroup = WeightInGroup;
                    t.HiddenInUI = true;           // 본편 자식 4개와 같음(jbp HiddenInUI true) — 부모 메뉴로만 고른다
                    t.IsOnByDefault = false;       // 기본 켜짐은 EnergizeDefaultChoice 가 정한다(원소 수에 따라)
                    t.ActivationType = AbilityActivationType.Immediately;
                    t.DeactivateImmediately = true; // 예전 단일 토글 값(본편 자식은 false) — 원소를 바꾸면 이전 버프가 즉시 빠진다
                    t.DoNotTurnOffOnRest = true;    // 예전 단일 토글 값(설계 §4.2 「휴식해도 꺼지지 않는다」)
                });
                var choice = new EnergizeDefaultChoice();
                AddComponents(toggle, choice);
                if (toggle.Buff != buff || toggle.Group != Group || toggle.WeightInGroup != WeightInGroup || !toggle.HiddenInUI || toggle.IsOnByDefault
                    || toggle.ActivationType != AbilityActivationType.Immediately || !toggle.DeactivateImmediately || !toggle.DoNotTurnOffOnRest
                    || toggle.ComponentsArray.Length != 1 || !ReferenceEquals(toggle.ComponentsArray[0], choice))
                    throw new InvalidOperationException("BlastMagus: " + toggle.name + " readback failed (expected m_Buff " + buff.name + ", Group " + Group + ", WeightInGroup " + WeightInGroup
                        + ", HiddenInUI, not IsOnByDefault, ActivationType Immediately, DeactivateImmediately, DoNotTurnOffOnRest, one EnergizeDefaultChoice; got " + toggle.Group + ", " + toggle.WeightInGroup
                        + ", " + toggle.HiddenInUI + ", " + toggle.IsOnByDefault + ", " + toggle.ActivationType + ", " + toggle.DeactivateImmediately + ", " + toggle.DoNotTurnOffOnRest + ", " + toggle.ComponentsArray.Length + " components)");
                children.Add(row.Id, (toggle, buff));
            }

            // --- 부모(본편 EnergizedWildShapeToggleAbility 모양: 컴포넌트 = [ActivatableAbilityVariants, ActivationDisable], m_Buff 없음, 같은 그룹, HiddenInUI false, IsOnByDefault false). ---
            var variants = new ActivatableAbilityVariants();
            SetField(variants, "m_Variants", EnergizeTable.All.Select(r => Ref<BlueprintActivatableAbilityReference>(r.ToggleGuid)).ToArray());
            Parent = Create<BlueprintActivatableAbility>("BlastMagusEnergizeParent", Guids.EnergizeParentToggle, t =>
            {
                BlastBlueprints.Names(t, "Energize.Name", "Energize.Description", icon);
                t.Group = Group;
                t.WeightInGroup = WeightInGroup;
                t.HiddenInUI = false;
                t.IsOnByDefault = false;
                t.ActivationType = AbilityActivationType.Immediately;
            });
            var disable = new ActivationDisable();
            AddComponents(Parent, variants, disable);
            var back = ((BlueprintActivatableAbilityReference[])GetFieldValue(variants, "m_Variants")).Select(r => r.Get()).ToArray();
            if (Parent.Buff != null || Parent.Group != Group || Parent.HiddenInUI || Parent.IsOnByDefault || Parent.ComponentsArray.Length != 2
                || !ReferenceEquals(Parent.ComponentsArray[0], variants) || !ReferenceEquals(Parent.ComponentsArray[1], disable)
                || !back.SequenceEqual(EnergizeTable.All.Select(r => (BlueprintActivatableAbility)children[r.Id].toggle)))
                throw new InvalidOperationException("BlastMagus: EnergizeParent readback failed (no buff, Group " + Group + ", visible, not on by default, components [ActivatableAbilityVariants(every child toggle in EnergizeTable order), ActivationDisable]; got "
                    + back.Length + " variants, " + Parent.ComponentsArray.Length + " components)");

            // --- 예전 단일 토글·버프(세이브 호환 — 위 「예전 세이브」). 새로 부여하지 않는다(어떤 특성도 AddFacts 하지 않음). 숨김, 기본 꺼짐, 피해 없음. ---
            LegacyBuff = Create<BlueprintBuff>("BlastMagusEnergizeBuff", Guids.EnergizeBuff, b =>
            {
                BlastBlueprints.Names(b, "Energize.Name", "Energize.Description", icon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — 예전 세이브에 남은 이 버프가 지워질 때 NRE 방지)
            });
            var marker = new LegacyEnergizeMarker();
            AddComponents(LegacyBuff, marker);
            LegacyToggle = Create<BlueprintActivatableAbility>("BlastMagusEnergizeToggle", Guids.EnergizeToggle, t =>
            {
                BlastBlueprints.Names(t, "Energize.Name", "Energize.Description", icon);
                SetField(t, "m_Buff", Ref<BlueprintBuffReference>(Guids.EnergizeBuff));
                t.Group = ActivatableAbilityGroup.None;
                t.HiddenInUI = true;
                t.IsOnByDefault = false;
                t.ActivationType = AbilityActivationType.Immediately;
                t.DeactivateImmediately = true;
                t.DoNotTurnOffOnRest = true;
            });
            if (LegacyToggle.Buff != LegacyBuff || !LegacyToggle.HiddenInUI || LegacyToggle.IsOnByDefault || LegacyToggle.Group != ActivatableAbilityGroup.None || LegacyToggle.ComponentsArray.Length != 0
                || LegacyBuff.ComponentsArray.Length != 1 || !ReferenceEquals(LegacyBuff.ComponentsArray[0], marker))
                throw new InvalidOperationException("BlastMagus: legacy energize toggle/buff readback failed (hidden, off by default, no group, buff = [LegacyEnergizeMarker] only)");

            // --- 특성: 1레벨 = 부모 토글 부여, 7·13·19 = 설명 전용(컴포넌트 없음) ---
            Feature = BlastBlueprints.MakeFeature("BlastMagusEnergizeFeature", Guids.EnergizeFeature, "EnergizeFeature.Name", "EnergizeFeature.Description", icon);
            AddComponents(Feature, BlastBlueprints.Facts(Parent));
            F7 = BlastBlueprints.MakeFeature("BlastMagusEnergize7", Guids.Energize7, "Energize7.Name", "Energize7.Description", icon);
            F13 = BlastBlueprints.MakeFeature("BlastMagusEnergize13", Guids.Energize13, "Energize13.Name", "Energize13.Description", icon);
            F19 = BlastBlueprints.MakeFeature("BlastMagusEnergize19", Guids.Energize19, "Energize19.Name", "Energize19.Description", icon);
            if (Feature.ComponentsArray.Length != 1 || F7.ComponentsArray.Length != 0 || F13.ComponentsArray.Length != 0 || F19.ComponentsArray.Length != 0)
                throw new InvalidOperationException("BlastMagus: Energize feature component readback failed (Feature = AddFacts[EnergizeParent] only, F7/F13/F19 = none)");

            // --- 원소 특성 → 자식 토글 부여 되읽기(1단계 본편 원소). BlastBlueprints 는 자식이 생기기 전에 GUID 참조로 넣었다(ElementFacts) — 여기서 실제 블루프린트로 해석되는지 본다.
            //     KEE 원소 특성은 2단계 CreateElementFeature 가 만들며 그때 VerifyGrant 를 부른다. ---
            var made = BlastBlueprints.ElementSelection.AllFeatures.ToArray();
            if (made.Length != ElementTable.Vanilla.Length) throw new InvalidOperationException("BlastMagus: ElementSelection has " + made.Length + " features at stage 1, expected the " + ElementTable.Vanilla.Length + " vanilla elements");
            foreach (var r in ElementTable.Vanilla)
            {
                var f = made.FirstOrDefault(x => x != null && x.AssetGuid.ToString() == r.OurFeatureGuid);
                if (f == null) throw new InvalidOperationException("BlastMagus: element feature " + r.OurFeatureGuid + " (" + r.Id + ") is not in ElementSelection");
                VerifyGrant(f, r.Id);
            }

            // --- 레벨표(C4: 성장 레벨은 BlastRules.EnergizeGrowthLevels 단일 원장) ---
            // F7/F13/F19 의 이름·설명 문자열이 2d6/3d6/4d6 을 말하므로, i 번째 성장 레벨의 주사위 수가 i+2 인지 대조한다(원장이 바뀌면 문자열과 어긋나 실패).
            var growth = BlastRules.EnergizeGrowthLevels;
            var growthFeatures = new[] { F7, F13, F19 };
            if (growth.Length != growthFeatures.Length)
                throw new InvalidOperationException("BlastMagus: BlastRules.EnergizeGrowthLevels has " + growth.Length + " levels, Energize has " + growthFeatures.Length + " growth features");
            for (int i = 0; i < growth.Length; i++)
                if (BlastRules.EnergizeDice(growth[i]) != i + 2)
                    throw new InvalidOperationException("BlastMagus: EnergizeDice(" + growth[i] + ") = " + BlastRules.EnergizeDice(growth[i]) + " but " + growthFeatures[i].name + " text says " + (i + 2) + "d6");
            Archetype.AddAt(GrantLevel, Feature);
            Archetype.AddAt(growth[0], F7);
            Archetype.AddAt(growth[1], F13);
            Archetype.AddAt(growth[2], F19);

            // 되읽기: 네 특성이 각각 정확히 한 레벨, 그 레벨에만 있다(AddAt 은 같은 레벨 중복만 본다).
            var expect = new[] { (GrantLevel, Feature), (growth[0], F7), (growth[1], F13), (growth[2], F19) };
            foreach (var (level, f) in expect)
            {
                var levels = Archetype.Bp.AddFeatures.Where(e => e.Features.Contains(f)).Select(e => e.Level).ToArray();
                if (levels.Length != 1 || levels[0] != level)
                    throw new InvalidOperationException("BlastMagus: " + f.name + " archetype levels [" + string.Join(",", levels) + "], expected [" + level + "]");
            }
        }

        // 원소 특성 f 의 AddFacts 가 해석까지 [그 원소 *BlastFeature, 그 원소 자식 토글] 인지(GUID 대조는 계약 CheckOurFeature 가 이미 했다 — 여기는 자식 토글이 실제로 있는가).
        internal static void VerifyGrant(BlueprintFeature f, Element e)
        {
            if (f == null) throw new ArgumentNullException("f");
            var toggle = ChildToggle(e);
            var facts = f.ComponentsArray.OfType<AddFacts>().SelectMany(c => c.Facts).ToArray();
            if (facts.Length != 2 || facts[0] == null || facts[0].AssetGuid.ToString() != ElementTable.Row(e).BlastFeatureGuid || !ReferenceEquals(facts[1], toggle))
                throw new InvalidOperationException("BlastMagus: element feature " + f.name + " AddFacts resolve to [" + string.Join(",", facts.Select(x => x == null ? "null" : x.name)) + "], expected [its BlastFeature, " + toggle.name + "]");
        }

        public static BlueprintActivatableAbility ChildToggle(Element e) { return ChildOf(e).toggle; }




        static (BlueprintActivatableAbility toggle, BlueprintBuff buff) ChildOf(Element e)
        {
            if (children.Count == 0) throw new InvalidOperationException("BlastMagus: Energize child lookup before Energize.Build()");
            (BlueprintActivatableAbility toggle, BlueprintBuff buff) c;
            if (!children.TryGetValue(e, out c)) throw new ArgumentOutOfRangeException("e", e, "BlastMagus: no energize child for element");
            return c;
        }

        public static bool IsChildToggle(BlueprintActivatableAbility bp)
        {
            if (children.Count == 0) throw new InvalidOperationException("BlastMagus: Energize.IsChildToggle before Energize.Build()");
            return bp != null && children.Values.Any(c => ReferenceEquals(c.toggle, bp));
        }

        public static bool IsChildBuff(BlueprintScriptableObject bp)
        {
            if (children.Count == 0) throw new InvalidOperationException("BlastMagus: Energize.IsChildBuff before Energize.Build()");
            return bp != null && children.Values.Any(c => ReferenceEquals(c.buff, bp));
        }

        // 자식 버프가 켜질 때(EnergizeWeapon.OnActivate) 그룹 가정을 검사한다(C7: 틀리면 조용히 지나가는 가정 → 오류 로그로 드러낸다).
        //   (가) 우리 그룹(본편 EnergizedWildShape)에 우리 것이 아닌 활성 능력이 있다 → 서로 끈다. (나) 자식 버프가 2개 이상 → 주입이 겹친다(그룹 크기 > 1).
        internal static void AuditGroup(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            var foreign = u.ActivatableAbilities.RawFacts.Where(a => a.Blueprint.Group == Group && !ReferenceEquals(a.Blueprint, Parent) && !IsChildToggle(a.Blueprint)).Select(a => a.Blueprint.name).ToArray();
            if (foreign.Length > 0)
                ModContext.LogError("BlastMagus: " + u.CharacterName + " has other activatable abilities in the Energize group " + Group + " (" + string.Join(", ", foreign) + ") — turning one on turns the other off");
            var active = u.Buffs.RawFacts.Where(b => IsChildBuff(b.Blueprint)).Select(b => b.Blueprint.name).ToArray();
            if (active.Length > 1)
                ModContext.LogError("BlastMagus: " + u.CharacterName + " has " + active.Length + " energize element buffs at once (" + string.Join(", ", active) + ") — the group allows more than one (group size increased?)");
        }

        static object GetFieldValue(object o, string name)
        {
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("BlastMagus: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }

    // 자식 버프의 컴포넌트(원소 고정). 본편 WeaponEnergyDamageDice.OnEventAboutToTrigger(RuleCalculateWeaponStats) IL 0000~0056 의 4줄을 유닛 버프용으로 옮겼다
    // (본편은 무기 마법부여 컴포넌트라 rule.Weapon == Owner(무기) 검사, 우리는 유닛 사실이라 무기 종류 검사로 대체; 정찰 §6).
    // RuleCalculateWeaponStats 는 툴팁에서도 돈다(AttackWithWeapon == null) → 서술자 추가만 하고 자원·상태는 절대 쓰지 않는다.
    // 추가 항목은 목록 index 0 이 아니므로 치명타 배수를 받지 않는다(RuleAttackWithWeapon.CreateDamage @0x004E~0x00AE, 정찰 §6) — flaming 과 같다.
    // 인스턴스 필드 = 원소 하나(생성자에서만 정한다, 읽기 전용 — 블루프린트 설정값이지 유닛 상태가 아니다). 기본 생성자 없음: 원소가 빠진 컴포넌트(= 조용히 Fire(0))를 만들 수 없다.
    public class EnergizeWeapon : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateWeaponStats>
    {
        readonly Element element;

        public EnergizeWeapon(Element e)
        {
            if (!Enum.IsDefined(typeof(Element), e)) throw new ArgumentOutOfRangeException("e", e, "BlastMagus: EnergizeWeapon with an undefined element");
            element = e;
        }

        public Element Element { get { return element; } }

        // 버프가 켜질 때 1회(토글을 켤 때·원소를 바꿀 때 — 핫패스 아님). 로드 때는 돌지 않는다(EntityFact.PostLoad 는 Activate 를 부르지 않는다).
        protected override void OnActivate() { Energize.AuditGroup(Owner); }

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt == null) throw new ArgumentNullException("evt");
            var w = evt.Weapon;
            if (w == null) return; // legitimate no-op: stats without a weapon — nothing to energize
            if (w.Blueprint.IsNatural || w.Blueprint.IsUnarmed) return; // legitimate no-op: design §4.2 manufactured weapons only (펫도 같다: 자연 공격에는 절대 붙지 않는다)
            if (!w.Blueprint.IsMelee) return; // legitimate no-op: 설계 §4.4 근접 한정 (get_IsMelee = !IsRanged; IsRanged = AttackType Ranged|RangedTouch)
             
            // FighterGroup 형식 = WeaponFighterGroupFlags([Flags], 정의 None=1). 0 은 「플래그 하나도 없음」(None 아님) — 본편 jbp 에서 0 인 형식은
            // Stub·Touch·MeleeTouch·Ray·Incorporeal*·KineticBlast*(Energy/Physical/Blade)·Torch 뿐(blueprints.zip Weapons/Types 전수, 2026-11-07).
            if (w.Blueprint.FighterGroup == 0) return;
            if (!BlastState.IsEnabled(Owner)) return; // legitimate no-op: blasts disabled/unknown for this unit — BlastState already logged once (BlastPartGuard / IsEnabled)
            // 원소(2026-11-15 판정): 이 버프의 원소로 고정 — 유닛의 *BlastBase 를 세지 않는다(원소가 여럿이면 플레이어가 자식 토글로 고른다).
            var e = element;
            var magus = BlastBlueprints.MagusClass;
            if (magus == null) throw new InvalidOperationException("BlastMagus: EnergizeWeapon before BlastBlueprints.Build() (MagusClass is null)");
            int level = Owner.Progression.GetClassLevel(magus);
            if (level <= 0) throw new InvalidOperationException("BlastMagus: EnergizeWeapon on " + Owner.CharacterName + " with magus level " + level + " (energize buff without a magus level)");
            int dice = BlastRules.EnergizeDice(level);
            evt.DamageDescription.Add(new DamageDescription
            {
                // 원소 모델(설계 §4.3-1, 계획 T2): 서술자 = 그 원소 행의 DescriptorOf → 호출마다 새 DamageTypeDescription(가변 객체를 공유하지 않는다).
                TypeDescription = ElementModel.NewDamageType(ElementTable.Row(e)),
                Dice = new DiceFormula(dice, DiceType.D6),
                SourceFact = Fact,
            });
        }

        // 정당한 무동작: 서술자는 계산 전에 넣는다(본편 WeaponEnergyDamageDice 도 AboutToTrigger 만 쓴다).
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
    }

    // 자식 토글의 컴포넌트: 그 토글이 유닛에 새로 붙은 직후 기본 켜짐을 정한다(EnergizeTable.OnGrant).
    // 시점 = ActivatableAbilityCollection.OnFactDidAttach → UnitLogicCollection.OnFactDidAttach 의 IUnitGainFactHandler(IL 0029) — 엔진의 IsOnByDefault(IL 0007~0016)와 같은 자리.
    // EntityFactsManager.Add 에서만 온다(DelegateOnFactDidAttach) — 로드(PostLoad)에서는 오지 않으므로 플레이어가 고른 상태는 로드로 바뀌지 않는다.
    public class EnergizeDefaultChoice : UnitFactComponentDelegate, IUnitGainFactHandler
    {
        public void HandleUnitGainFact(EntityFact fact)
        {
            if (fact == null) throw new ArgumentNullException("fact");
            if (!ReferenceEquals(fact, Fact)) return; // legitimate no-op: another fact attached — only this toggle's own attach decides its default
            var self = Fact as ActivatableAbility;
            if (self == null) throw new InvalidOperationException("BlastMagus: EnergizeDefaultChoice on " + Fact.Blueprint.name + " which is not an activatable ability (wiring error)");
            var owned = Owner.ActivatableAbilities.RawFacts.Where(a => Energize.IsChildToggle(a.Blueprint)).ToList();
            if (!owned.Contains(self)) throw new InvalidOperationException("BlastMagus: " + self.Blueprint.name + " attached but not in " + Owner.CharacterName + "'s activatable abilities");
            switch (EnergizeTable.OnGrant(owned.Count))
            {
                case EnergizeDefault.TurnOnThis:
                    self.IsOn = true;
                    break;
                case EnergizeDefault.TurnAllOff:
                    foreach (var a in owned) a.IsOn = false;
                    ModContext.Log("BlastMagus: " + Owner.CharacterName + " has " + owned.Count + " energize elements (" + string.Join(", ", owned.Select(a => a.Blueprint.name)) + ") — none on by default; pick one in the Energize Weapon menu");
                    break;
                default: throw new InvalidOperationException("BlastMagus: undefined EnergizeDefault");
            }
        }
    }

    // 예전 단일 에너지 주입 버프(84c3fa52…)의 유일한 컴포넌트. 피해를 주지 않는다. 그 버프가 유닛에 있으면(예전 세이브 로드 직후 — Energize 「예전 세이브」) 유닛당 1회 INFO.
    // 상태는 정적 CWT(세이브에 아무것도 남기지 않는다 — BlastState 와 같은 방식).
    public class LegacyEnergizeMarker : UnitFactComponentDelegate
    {
        static readonly WeakKeyTable<UnitEntityData, object> reported = new WeakKeyTable<UnitEntityData, object>();

        protected override void OnActivate() { Report(); }
        protected override void OnPostLoad() { Report(); }

        void Report()
        {
            var u = Owner;
            if (u == null) throw new InvalidOperationException("BlastMagus: LegacyEnergizeMarker without an owner");
            object seen;
            if (reported.TryGetValue(u, out seen)) return; // legitimate no-op: already reported for this unit this session
            reported.Add(u, new object());
            ModContext.Log("BlastMagus: " + u.CharacterName + " carries the pre-2026-11-15 Energize Weapon buff (old save) — it is inert (no damage); the old toggle is replaced on load by the per-element Energize toggles");
        }
    }
}
