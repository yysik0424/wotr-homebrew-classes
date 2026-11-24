using System;
using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
     
     
     
    public static class OneHandStaff
    {
        public static BlueprintFeature Feature;

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("BulwarkMage: OneHandStaff.Build() called twice");
            Feature = Create<BlueprintFeature>("BulwarkOneHandStaffFeature", Guids.OneHandStaffFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("staff.name"));
                SetField(f, "m_Description", Strings.L("staff.desc"));
                SetField(f, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                f.IsClassFeature = true;
                f.Ranks = 1;
                f.Groups = new FeatureGroup[0];
                f.ReapplyOnLevelUp = false;
            });
            if (Feature.ComponentsArray != null && Feature.ComponentsArray.Length != 0) throw new InvalidOperationException("BulwarkMage: one-handed staff marker feature has components (it must be a bare fact)");
            Archetype.AddAt(1, Feature);
        }
    }

     
    // 같은 메서드에 PrestigePlus OneHandGripFix.Postfix 가 있다(dnlib 2026-11-14 @0000~0094: 이미 참이면 끝, QuarterstaffMaster 특성 + 범주 10 / 창술 버프 + 무기군 11·12 → 참).
    // 둘 다 「거짓 → 참」만 하고 참을 거짓으로 바꾸지 않으므로 순서와 무관하게 결과 = 두 조건의 OR(점검 S5 prestigeplus-patch-coexists).
    // 예외 계약: Postfix 의 예외는 엔진 장착·UI 경로로 전파된다 → 본문 전체를 try 로 감싸 BulwarkErrors 로 세고, 결과는 들어온 값(원본 + 앞선 Postfix)으로 되돌린다.
    // 본문 맨 앞(try 안) MaybeFault("h1-postfix") = 점검 S17 inject-h1-postfix 의 결함 주입 자리(점검 러너가 돌 때만 무장 가능).
    [HarmonyPatch(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.CanTakeOneHand))]
    static class OneHandStaffPatch
    {
        // 매개변수 이름 `unit` 은 원본 CanTakeOneHand(UnitEntityData unit) 와 같아야 한다 — Harmony 가 이름으로 묶는다(틀리면 PatchAll 이 HarmonyException, 시험 「wiring: postfix checks category 10 and fact」 가 Assembly-CSharp 에서 읽어 대조).
        static void Postfix(ref bool __result, ItemEntityWeapon __instance, UnitEntityData unit)
        {
            bool original = __result;
            try
            {
                BulwarkErrors.MaybeFault(ErrorLedger.SiteH1Postfix);
                if (original || unit == null) return; // StaffGripRule 의 첫 두 줄과 같은 결론(Keep) — 뜨거운 경로라 사실 조회 전에 끝낸다
                var bp = __instance == null ? null : __instance.Blueprint;
                // 표지 특성이 없으면(빌드 실패 — 그 자체가 이미 BuildFailed + ERROR) 아무 유닛도 표지를 가질 수 없다 → 그대로.
                var feature = OneHandStaff.Feature;
                bool hasFeature = bp != null && feature != null && unit.Descriptor.HasFact(feature);
                var d = StaffGripRule.Decide(original, true, bp != null, hasFeature,
                    bp != null && bp.Category == WeaponCategory.Quarterstaff, bp != null && bp.IsTwoHanded, bp != null && bp.IsMelee);
                if (d == StaffGripDecision.AllowOneHand) __result = true;
                // 같은 무기 엔티티는 세션에 한 번만 보고(T3 검토 m-5 — 뜨거운 경로, 되풀이는 SuppressedRepeats 로 센다).
                else if (d == StaffGripDecision.ReportMissingBlueprint) BulwarkErrors.ReportOnce(ErrorLedger.SiteH1Postfix, __instance == null ? "<null weapon>" : __instance.UniqueId, "CanTakeOneHand on a weapon entity without a blueprint (wielder " + unit.CharacterName + ") - result left as it was");
            }
            catch (Exception e)
            {
                __result = original;
                BulwarkErrors.ReportException(ErrorLedger.SiteH1Postfix, e);
            }
        }
    }
}
