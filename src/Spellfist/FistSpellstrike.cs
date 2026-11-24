using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spellfist
{
     
    // 특성(L2) = AddMagusMechanicPart{Spellstrike} → AddFacts[본편 SpellStrikeAbility 토글] → SpellbookPin (이 순서, 판정 (a); 되읽기).
    // 전달은 엔진이 한다(코드 없음): 맨손 명중 = TouchSpellsController.OnEventDidTrigger(RuleAttackWithWeapon) IL_0022~006E(IsNatural && IsMelee, 누구나),
    // 근접 제작 무기 명중 = MagusController.OnEventDidTrigger IL_0045~009C(part + Spellstrike.Active). 둘은 EmptyHandWeapon 조건으로 배타.
    // 막힘 = UnitUseAbility.CreateCastCommand IL_00AE~00BF 의 UnitPartMagus.IsSpellFromMagusSpellList 하나 → SpellbookPin(주문서 고정) + SpellListPatch(Postfix).
     
    // J4 메이거스 멀티클래스(part 공유) 미지원 → SpellbookPin 이 LogError.
    public static class FistSpellstrike
    {
        public static BlueprintFeature Feature { get; private set; }

        // AddMagusMechanicPart/Feature 는 private 중첩 열거(dnlib 2026-11-08, Assembly-CSharp: SpellCombat=1, Spellstrike=2, WandWielder=3, Counterstrike=4, EldritchArcher=5)
        // → 정수로 넣고 이름을 되읽는다. tests\SpellfistTests.cs CheckTask6bDll 이 Assembly-CSharp 의 리터럴과 대조한다.
        const int MagusMechanicSpellstrike = 2;
        const string MagusMechanicSpellstrikeName = "Spellstrike";

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Spellfist: FistSpellstrike.Build() called twice");
            if (Archetype.Bp == null || Spellbook.Bp == null) throw new InvalidOperationException("Spellfist: FistSpellstrike.Build() before Archetype/Spellbook.Build()");
            var toggle = Get<BlueprintActivatableAbility>(Guids.Game.SpellStrikeAbility);
            var icon = toggle.Icon;
            if (icon == null) throw new InvalidOperationException("Spellfist: SpellStrikeAbility " + Guids.Game.SpellStrikeAbility + " has no icon");

            var feature = Create<BlueprintFeature>("SpellfistFistSpellstrikeFeature", Guids.FistSpellstrikeFeature, f =>
            {
                Spellbook.Names(f, "FistSpellstrike.Name", "FistSpellstrike.Description");
                SetField(f, "m_Icon", icon);
            });
            if (!ReferenceEquals(Util.GetF(feature, "m_Icon"), icon)) throw new InvalidOperationException("Spellfist: FistSpellstrikeFeature icon readback failed");

            // 1) AddMagusMechanicPart{Spellstrike}: OnActivate 가 Owner.Ensure<UnitPartMagus>() 후 Spellstrike.Unlock()(IL_0000~0x0048).
            var part = new AddMagusMechanicPart();
            var featureType = Util.GetF(part, "m_Feature").GetType();
            SetField(part, "m_Feature", Enum.ToObject(featureType, MagusMechanicSpellstrike));
            var setFeature = Util.GetF(part, "m_Feature");
            if (Convert.ToInt32(setFeature) != MagusMechanicSpellstrike || Enum.GetName(featureType, setFeature) != MagusMechanicSpellstrikeName)
                throw new InvalidOperationException("Spellfist: AddMagusMechanicPart.m_Feature readback is " + setFeature + " (" + Convert.ToInt32(setFeature) + "), expected " + MagusMechanicSpellstrikeName + " (" + MagusMechanicSpellstrike + ")");

            // 2) AddFacts[SpellStrikeAbility]: 본편 토글(기본 켜짐) → SpellStrikeBuff 의 SetMagusFeatureActive{Spellstrike} 가 Spellstrike.Active 를 켠다/끈다.
            var facts = new AddFacts();
            SetField(facts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.Game.SpellStrikeAbility) });
            if (facts.Facts.Count() != 1 || facts.Facts.First() != toggle) throw new InvalidOperationException("Spellfist: FistSpellstrikeFeature AddFacts[SpellStrikeAbility] readback failed");

            // 3) SpellbookPin: part 의 주문서를 우리 주문서로 고정.
            var pin = new SpellbookPin();
            AddComponents(feature, part, facts, pin);
            var comps = feature.ComponentsArray;
            if (comps.Length != 3 || !ReferenceEquals(comps[0], part) || !ReferenceEquals(comps[1], facts) || !ReferenceEquals(comps[2], pin))
                throw new InvalidOperationException("Spellfist: FistSpellstrikeFeature components [" + string.Join(", ", comps.Select(c => c.GetType().Name)) + "] != [AddMagusMechanicPart, AddFacts, SpellbookPin] (order matters: the part must exist before SpellbookPin.OnActivate)");

            Feature = feature;
            Archetype.AddAt(SpellfistRules.FistSpellstrikeLevel, Feature);
            if (Archetype.CountAt(Archetype.Bp.AddFeatures, SpellfistRules.FistSpellstrikeLevel, Feature) != 1)
                throw new InvalidOperationException("Spellfist: FistSpellstrike feature count at archetype level SpellfistRules.FistSpellstrikeLevel (" + SpellfistRules.FistSpellstrikeLevel + ") readback is not 1");
        }
    }

    // UnitPartMagus.m_Spellbook(비직렬화 — 정찰 §0-3, 로드마다 null) 을 우리 주문서로 고정한다. 인스턴스 필드 없음(블루프린트 컴포넌트는 모든 유닛이 공유).
    // 고정이 없으면: get_Spellbook 이 MagusClass 로 해석해 null(IL_0070~008B) → 바닐라 IsSpellFromMagusSpellList NRE / TTT-Base 판정 거짓(조용히 일반 접촉 공격).
    // 호출 시점(dnlib 2026-11-08, Assembly-CSharp):
    //   EntityFact.Activate IL_0108~0145 가 Components 를 목록 순서로 Activate — 목록 순서 = EntityFact.Setup IL_000E~0086 의 BlueprintComponents 순서 = ComponentsArray 순서
    //   → AddMagusMechanicPart.OnActivate(Ensure) 가 우리 OnActivate 보다 먼저 돈다(FistSpellstrike.Build 가 순서를 되읽음). TurnOn 은 모든 Activate 뒤(IL_0190).
    //   로드: EntityDataBase.PostLoad IL_0085 Facts.PostLoad 가 IL_0090 Parts.PostLoad 보다 먼저지만 Get<T> 는 역직렬화된 m_Parts 를 훑으므로(EntityPartsManager.Get IL_0020~005F) part 는 이미 보인다.
    //   UnitDescriptor.m_Spellbooks 는 [JsonProperty] 라 PostLoad 시점에 GetSpellbook 이 동작한다. 이후 엔티티 TurnOn 이 OnTurnOn 으로 다시 고정한다.
    // 각 단계의 예외는 엔진이 PFLog.Exception 으로 남긴다(EntityFact.Activate IL_0128, TurnOn IL_00D9, PostLoad) — 조용하지 않다.
    public class SpellbookPin : UnitFactComponentDelegate
    {
        protected override void OnActivate() { Pin(); }
        protected override void OnTurnOn() { Pin(); }
        protected override void OnPostLoad() { Pin(); }

        void Pin()
        {
            var owner = Owner;
            if (owner == null) throw new InvalidOperationException("Spellfist: SpellbookPin has no owner");
            var part = owner.Get<UnitPartMagus>();
            if (part == null) throw new InvalidOperationException("Spellfist: " + owner.CharacterName + " has no UnitPartMagus (AddMagusMechanicPart must precede SpellbookPin in FistSpellstrikeFeature)");
            // J4: 메이거스 클래스 판정은 엔진의 것(UnitPartMagus.Class = BlueprintRoot.SystemMechanics.MagusClass, get_Class IL_0000~000A)을 그대로 쓴다.
            var magusClass = part.Class;
            if (magusClass == null) throw new InvalidOperationException("Spellfist: UnitPartMagus.Class (BlueprintRoot.SystemMechanics.MagusClass) is null");
            if (owner.Progression.GetClassData(magusClass) != null)
            {
                ModContext.LogError("Spellfist: " + owner.CharacterName + " has magus levels — Fist Spellstrike with a magus multiclass is unsupported (design §11-12 J4); UnitPartMagus spellbook left to the magus");
                return;
            }
            if (Spellbook.Bp == null) throw new InvalidOperationException("Spellfist: SpellbookPin active but Spellbook.Build() did not run");
            var book = owner.Descriptor.GetSpellbook(Spellbook.Bp);
            if (book == null) throw new InvalidOperationException("Spellfist: " + owner.CharacterName + " has no Spellfist spellbook " + Spellbook.Bp.AssetGuid + " but holds FistSpellstrikeFeature");
            SetField(part, "m_Spellbook", book);
            if (!ReferenceEquals(Util.GetF(part, "m_Spellbook"), book) || !ReferenceEquals(part.Spellbook, book))
                throw new InvalidOperationException("Spellfist: UnitPartMagus.m_Spellbook readback failed for " + owner.CharacterName + " (field or UnitPartMagus.Spellbook is not the Spellfist spellbook)");
        }
    }

    // 판정 J1 의 Harmony 예외 1건. UnitPartMagus.IsSpellFromMagusSpellList(AbilityData spell) — 오버로드 1개(dnlib 2026-11-08, tools\patch-targets\Spellfist.txt).
    // 다른 모드 결과 보존: __result 가 이미 true 면 그대로(TTT-Base Prefix 대체·DarkCodex/COP/PrestigePlus Postfix 의 true 유지). false → true 만, 우리 주문서 주문일 때만.
    // 「우리 주문서」 = spell 자신 또는 그 StickyTouch 원 시전(UnitPartTouch.Init IL_00AC~00BD 가 전달 능력의 Spellbook 을 원 시전 책으로 둔다)의 AbilityData.Spellbook.Blueprint == Spellbook.Bp.
    // StickyTouch 가 없거나 Spellbook 이 없는 능력은 「우리 책 주문이 아님」이 사실 판정이다(판정 불가가 아님) → false 유지.
    [HarmonyPatch(typeof(UnitPartMagus), "IsSpellFromMagusSpellList", new[] { typeof(AbilityData) })]
    static class SpellListPatch
    {
        static void Postfix(UnitPartMagus __instance, AbilityData spell, ref bool __result)
        {
            if (__result || ModContext.BuildSkipReasonEn != null) return;
            if (ReferenceEquals(spell, null)) throw new ArgumentNullException("spell", "Spellfist: UnitPartMagus.IsSpellFromMagusSpellList called with null spell");
            if (Spellbook.Bp == null)
            {
                // 빌드 실패(브롤러 모드 없음 등)는 BuildAll 이 이미 LogError 했고 우리 주문서는 존재할 수 없다 → 본편 메이거스 판정을 깨지 않는다.
                if (ModContext.BuildFailed) return;
                throw new InvalidOperationException("Spellfist: SpellListPatch ran but Spellbook.Bp is null and the build did not fail");
            }
            if (IsOurs(spell) || IsOurs(spell.StickyTouch)) __result = true;
        }

        static bool IsOurs(AbilityData a)
        {
            if (ReferenceEquals(a, null)) return false;
            var book = a.Spellbook;
            return book != null && book.Blueprint == Spellbook.Bp;
        }
    }
}
