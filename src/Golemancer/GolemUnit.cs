using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Localization;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 원형 CR6_GolemWood 의 m_AddFacts 처분(결정 ① — 설계와 다른 점, 보고 대상).
    public enum FactDisposition
    {
        RemoveStatRecipe,     // 자연 방어·DR → 삭제(수치 특성 StatRecipe 가 단일 공급, T4)
        RemoveConstructType,  // 면역 → 삭제(우리 진행표 L1 ConstructType 이 구조물 면역 공급)
        RemoveT4Clone,        // 특기 능력(파편 폭발) → 삭제(T4 가 DC 직업을 재지정한 복제본을 단다 — 설계 §12 정정 6)
        KeepVisual,           // 시각·FX 전용 → 유지(본편 사실을 참조로)
    }

    public sealed class FactRule
    {
        public readonly string Guid;
        public readonly FactDisposition Disposition;
        public readonly string Why;
        public FactRule(string guid, FactDisposition disposition, string why) { Guid = guid; Disposition = disposition; Why = why; }
    }

    // 골렘 펫 유닛(계획 T3 「깊은 복사 계약」 표 3행, 정찰 §4.3·§4.1, 선례 WSS SpiritBlueprints.BuildUnit / PackWarden AuxUnits.BuildClone 위생).
    //   CR6_GolemWood b02f074f 값 복사(CloneKit.CopyInto) → ComponentsArray = [새 AddClassLevels{우리 직업, Levels 0, Selections []}] 만(Experience 버림),
    //   m_AddFacts = 처분표 결과(VanillaFactDisposition — 원본 전수, 표 밖 사실이 있으면 빌드 실패), Body = CloneKit.Copy(원본 Body) 뒤 m_AdditionalLimbs = [](T3 자연 공격 0 — T4 가 슬램 1),
    //   m_Faction = Player, m_Brain = Character_Brain, Prefab = 새 링크(같은 AssetId), LocalizedName = 새 이름 자산. 공유(읽기만) = Visual · 능력치 기본값(힘 18).
    //   AllowDyingCondition 은 원본에 없고 복제본에도 없음을 단언(설계 §4.7·§12 정정 1 — GUID 무관 단언).
    public static class GolemUnit
    {
        public static BlueprintUnit Unit;

        // 원본 CR6_GolemWood m_AddFacts 5개 전수(blueprints.zip 2026-11-09 직독 — 시험 `jbp: wood facts disposition complete` 가 이 표 == 원본 집합을 대조한다).
        public static readonly FactRule[] VanillaFactDisposition =
        {
            new FactRule(Guids.Game.NaturalArmor6, FactDisposition.RemoveStatRecipe, "AddStatBonus AC NaturalArmor 6"),
            new FactRule(Guids.Game.DRAdamantine5, FactDisposition.RemoveStatRecipe, "AddDamageResistancePhysical 5/adamantine"),
            new FactRule(Guids.Game.GolemWoodImmunity, FactDisposition.RemoveConstructType, "AddSpellImmunity (InvertedDescriptors) - all-spell immunity"),
            new FactRule(Guids.Game.WoodGolemSplintering, FactDisposition.RemoveT4Clone, "ability with ContextCalculateAbilityParamsBasedOnClass{ConstructClass}"),
            new FactRule(Guids.Game.GolemAutumnVisual, FactDisposition.KeepVisual, "buff with FxOnStart only"),
        };

        public static string[] KeptFactGuids { get { return VanillaFactDisposition.Where(r => r.Disposition == FactDisposition.KeepVisual).Select(r => r.Guid).ToArray(); } }

        public static void Build()
        {
            if (Unit != null) throw new InvalidOperationException("Golemancer: GolemUnit.Build() called twice");
            if (PetClass.Class == null) throw new InvalidOperationException("Golemancer: GolemUnit.Build() before PetClass.Build()");
            var src = Get<BlueprintUnit>(Guids.Game.GolemWood);
            // 원본 계약(C2): 컴포넌트 = [Experience, AddClassLevels{ConstructClass}] 정확히, m_AddFacts = 처분표 키 집합 정확히, 사지 무기 있음, 빈사 없음.
            var comps = src.ComponentsArray;
            if (comps == null || comps.Length != 2 || !(comps[0] is Kingmaker.Blueprints.Classes.Experience.Experience) || !(comps[1] is AddClassLevels))
                throw new InvalidOperationException("Golemancer: CR6_GolemWood components are not [Experience, AddClassLevels] (got " + (comps == null ? "null" : string.Join(",", comps.Select(c => c == null ? "null" : c.GetType().Name).ToArray())) + ")");
            var srcAcl = (AddClassLevels)comps[1];
            if (srcAcl.CharacterClass == null || srcAcl.CharacterClass.AssetGuid.ToString() != Guids.Game.ConstructClass) throw new InvalidOperationException("Golemancer: CR6_GolemWood AddClassLevels is not ConstructClass");
            var srcGuids = FactGuids(src);
            var ruled = VanillaFactDisposition.Select(r => r.Guid).ToArray();
            var unruled = srcGuids.Where(g => !ruled.Contains(g)).ToArray();
            var missing = ruled.Where(g => !srcGuids.Contains(g)).ToArray();
            if (unruled.Length > 0 || missing.Length > 0 || srcGuids.Distinct().Count() != srcGuids.Length || ruled.Distinct().Count() != ruled.Length)
                throw new InvalidOperationException("Golemancer: CR6_GolemWood m_AddFacts vs disposition table: unruled [" + string.Join(",", unruled) + "] missing [" + string.Join(",", missing) + "]");
            if (src.Body == null || LimbCount(src.Body) == 0) throw new InvalidOperationException("Golemancer: CR6_GolemWood has no additional limbs (slams)");
            if (comps.OfType<AllowDyingCondition>().Any()) throw new InvalidOperationException("Golemancer: CR6_GolemWood has AllowDyingCondition (design 0-9 says it has none)");
            if (src.Prefab == null || string.IsNullOrEmpty(src.Prefab.AssetId)) throw new InvalidOperationException("Golemancer: CR6_GolemWood has no prefab link");

            var kept = KeptFactGuids;
            Unit = Create<BlueprintUnit>("GolemancerGolemUnit", Guids.GolemUnit, x =>
            {
                CloneKit.CopyInto(src, x);
                x.ComponentsArray = new BlueprintComponent[0];
                var body = CloneKit.Copy(src.Body);
                SetField(body, "m_AdditionalLimbs", new BlueprintItemWeaponReference[0]); // T3 자연 공격 0(T4 가 슬램 1개)
                x.Body = body;
                SetField(x, "m_AddFacts", kept.Select(g => Ref<BlueprintUnitFactReference>(g)).ToArray());
                SetField(x, "m_Faction", Ref<BlueprintFactionReference>(Guids.Game.PlayerFaction));
                SetField(x, "m_Brain", Ref<BlueprintBrainReference>(Guids.Game.CharacterBrain));
                x.Prefab = new UnitViewLink { AssetId = src.Prefab.AssetId };
                var name = ScriptableObject.CreateInstance<SharedStringAsset>();
                name.String = Strings.L("Golem.Name");
                x.LocalizedName = name;
            });
            var acl = CloneKit.Copy(srcAcl);
            SetField(acl, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.PetClass));
            acl.Levels = 0; // 레벨은 AddPet(ContextValue = 연금술사 레벨) → TryLevelUpPet 이 준다(정찰 §4.1)
            acl.Selections = new SelectionEntry[0];
            AddComponents(Unit, acl);

            // 되읽기(게임이 읽는 경로).
            var back = Unit.ComponentsArray;
            if (back.Length != 1 || !(back[0] is AddClassLevels) || ReferenceEquals(back[0], srcAcl)) throw new InvalidOperationException("Golemancer: golem unit components are not exactly [our AddClassLevels]");
            var ourAcl = (AddClassLevels)back[0];
            if (!ReferenceEquals(ourAcl.CharacterClass, PetClass.Class) || ourAcl.Levels != 0 || ourAcl.Selections.Length != 0) throw new InvalidOperationException("Golemancer: golem AddClassLevels readback failed");
            if (Unit.ComponentsArray.OfType<AllowDyingCondition>().Any()) throw new InvalidOperationException("Golemancer: golem unit has AllowDyingCondition");
            if (ReferenceEquals(Unit.Body, src.Body) || LimbCount(Unit.Body) != 0) throw new InvalidOperationException("Golemancer: golem body readback failed (shared or limbs left)");
            var facts = FactGuids(Unit);
            if (!facts.SequenceEqual(kept)) throw new InvalidOperationException("Golemancer: golem m_AddFacts readback [" + string.Join(",", facts) + "] != kept [" + string.Join(",", kept) + "]");
            if (Unit.Faction == null || Unit.Faction.AssetGuid.ToString() != Guids.Game.PlayerFaction) throw new InvalidOperationException("Golemancer: golem faction readback is not Player");
            if (Unit.DefaultBrain == null || Unit.DefaultBrain.AssetGuid.ToString() != Guids.Game.CharacterBrain) throw new InvalidOperationException("Golemancer: golem brain readback is not Character_Brain");
            if (ReferenceEquals(Unit.Prefab, src.Prefab) || Unit.Prefab.AssetId != src.Prefab.AssetId) throw new InvalidOperationException("Golemancer: golem prefab link readback failed");
            if (Unit.Strength != GolemRules.BaseStrength || Unit.Size != src.Size) throw new InvalidOperationException("Golemancer: golem strength/size readback " + Unit.Strength + "/" + Unit.Size);
            GolemIsolation.VerifyAfterBuild();
        }

        // 블루프린트 Body 의 추가 사지(자연 무기) 참조 수 — 직렬화 필드 직독. null 배열은 던진다.
        public static int LimbCount(BlueprintUnit.UnitBody body)
        {
            if (body == null) throw new ArgumentNullException("body");
            var limbs = (BlueprintItemWeaponReference[])FindField(typeof(BlueprintUnit.UnitBody), "m_AdditionalLimbs").GetValue(body);
            if (limbs == null) throw new InvalidOperationException("Golemancer: unit body m_AdditionalLimbs is null");
            return limbs.Length;
        }

        // 유닛의 m_AddFacts GUID(참조 배열 직독 — 해석된 AddFacts 목록이 아니라 기록된 참조). null 배열은 던진다.
        public static string[] FactGuids(BlueprintUnit u)
        {
            if (u == null) throw new ArgumentNullException("u");
            var refs = (BlueprintUnitFactReference[])FindField(typeof(BlueprintUnit), "m_AddFacts").GetValue(u);
            if (refs == null) throw new InvalidOperationException("Golemancer: " + u.name + " m_AddFacts is null");
            return refs.Select(r => r.Guid.ToString()).ToArray();
        }
    }
}
