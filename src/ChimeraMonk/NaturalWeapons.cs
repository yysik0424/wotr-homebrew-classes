using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    // 주사위는 1레벨 숨김 몸 특성의 ChimeraLimbComponent 가 사다리로 바꾼다.
    // 복제 경계(r2 N03 = Bulwark TowerShield.cs:51-62): 블루프린트·Unity 객체·다른 컴포넌트·Element 는 공유, 가변 하위 객체(배열·리스트·참조 래퍼)는 CloneKit.CopyOf 로 분리,
    // 컴포넌트는 CloneBoundary.CopyComponent 뒤 AddComponents 로 새 소유자에 재결합. 끝에 CloneKit.Shared(복제, 원본) 가 비어 있지 않으면 던진다.
    public static class NaturalWeapons
    {
        internal static bool Owns(BlueprintItemWeapon weapon) { return Weapons.ContainsValue(weapon) || RetiredWeapons.ContainsValue(weapon); }
        public static readonly Dictionary<string, BlueprintItemWeapon> Weapons = new Dictionary<string, BlueprintItemWeapon>();
        // 폐지된 변이의 무기 복제(세이브 호환용 — 블루프린트만 있다. 어느 특성에도 AddAdditionalLimb 로 붙지 않는다).
        public static readonly Dictionary<string, BlueprintItemWeapon> RetiredWeapons = new Dictionary<string, BlueprintItemWeapon>();
        public static BlueprintFeature BodyFeature;

        public static void Build()
        {
            if (BodyFeature != null) throw new InvalidOperationException("ChimeraMonk: NaturalWeapons.Build() called twice");
            var problems = NaturalWeaponsPlan.Problems();
            if (problems.Count > 0) throw new InvalidOperationException("ChimeraMonk: natural weapon plan: " + string.Join(" | ", problems));
            // 붙이는 행(Value = true)과 폐지 행(Value = false)을 한 반복문에서 복제한다(복제 호출은 한 곳). 폐지 행은 복제만 — 사지 부품을 만들지도, 어떤 특성에 붙이지도 않는다(시험 `retired: wiring`).
            //  폐지 행의 변이는 Mutations.Get 으로 얻을 수 없다(선택지가 아니다 — 던진다). 그래서 폐지 행을 붙이는 쪽으로 잘못 보내면 빌드가 실패한다.
            var work = NaturalWeaponsPlan.Rows().Select(r => new KeyValuePair<WeaponRow, bool>(r, true)).Concat(NaturalWeaponsPlan.RetiredRows().Select(r => new KeyValuePair<WeaponRow, bool>(r, false))).ToArray();
            foreach (var item in work)
            {
                var row = item.Key;
                var src = Get<BlueprintItemWeapon>(row.SourceGuid);
                if (src.Type == null || src.Type.AssetGuid.ToString() != row.TypeGuid) throw new InvalidOperationException("ChimeraMonk: " + src.name + " weapon type is " + (src.Type == null ? "null" : src.Type.AssetGuid.ToString()) + ", plan says " + row.TypeGuid);
                if (!src.IsNatural) throw new InvalidOperationException("ChimeraMonk: " + src.name + " is not a natural weapon (FCT would not apply)");
                var clone = BlueprintClone.Clone(src, "ChimeraMonkWeapon" + row.Key, row.Guid, c => true); // 복제 경계 한 곳(BlueprintClone.KeepShared — 독립 검토 M-2)
                if (!ReferenceEquals(clone.Type, src.Type) || clone.IsNatural != src.IsNatural) throw new InvalidOperationException("ChimeraMonk: weapon clone " + clone.name + " type readback failed");
                if (!item.Value) { RetiredWeapons.Add(row.Key, clone); continue; }
                Weapons.Add(row.Key, clone);
                var limb = Fab.Comp(new AddAdditionalLimb());
                SetField(limb, "m_Weapon", Ref<BlueprintItemWeaponReference>(row.Guid));
                AddComponents(Mutations.Get(row.Mutation), limb);
            }
            BodyFeature = Fab.Feature("ChimeraMonkBody", Guids.BodyFeature, "body", true);
            AddComponents(BodyFeature, new ChimeraLimbComponent());
            Archetype.AddPlanned(LevelPlan.KeyBody, BodyFeature);
            var diff = Readback.WeaponsDiff();
            if (diff.Count > 0) throw new InvalidOperationException("ChimeraMonk: natural weapon readback: " + string.Join(" | ", diff));
            var retiredDiff = Readback.RetiredDiff();
            if (retiredDiff.Count > 0) throw new InvalidOperationException("ChimeraMonk: retired mutation readback: " + string.Join(" | ", retiredDiff));
        }
    }
    [HarmonyLib.HarmonyPatch(typeof(Kingmaker.UnitLogic.Commands.AttackHandInfo), "GetAnimationType")]
    internal static class NaturalAttackAnimationPatch
    {
        static void Postfix(Kingmaker.UnitLogic.Commands.AttackHandInfo __instance,
            Kingmaker.Visual.Animation.Kingmaker.UnitAnimationType __0,
            ref Kingmaker.Visual.Animation.Kingmaker.UnitAnimationSpecialAttackType __1, ref int __2)
        {
            if (__0 == Kingmaker.Visual.Animation.Kingmaker.UnitAnimationType.SpecialAttack &&
                NaturalWeapons.Owns(__instance.Weapon?.Blueprint))
                WotrClassMod.Common.NaturalAttackAnimation.Resolve(__instance, ref __1, ref __2);
        }
    }

}
