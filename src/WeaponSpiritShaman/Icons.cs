using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
    // 아이콘 적용(2026-11-15 결함 수리): IconTable(순수 원장)대로 본편 원본의 m_Icon 을 우리 블루프린트에 넣는다. BuildAll 의 Archetype.Register 바로 앞(시험 icons: wiring).
    // 조용한 통과 금지(C1·C2): 표 무결성 · 원본 없음 · 원본 아이콘 null · 기존 복사값과 표가 다름 · 무기 공개 Icon null · 「숨김」 주장이 거짓 · 보이는 사실에 아이콘 없음 — 전부 던진다(BuildFailed).
    // 아이콘(표시용 Sprite)만 쓴다 — 게임 동작 값은 건드리지 않는다. 본편 객체에는 쓰지 않는다(읽기만 — 시험 exec: base-blueprint writes).
    public static class Icons
    {
        public static bool Applied { get; private set; }

        public static void Apply()
        {
            if (Applied) throw new InvalidOperationException("WeaponSpiritShaman: Icons.Apply() called twice");
            var own = BlueprintUtil.OwnBlueprints;
            var byGuid = new Dictionary<string, SimpleBlueprint>();
            foreach (var bp in own) byGuid.Add(bp.AssetGuid.ToString(), bp);
            // 범주 행 = 실제로 만든 영병(SpiritBlueprints.Entries). SpiritCategories.Rows 는 SpiritBlueprints.Build 만 읽는다(시험 wiring: one loop builds all).
            if (!SpiritBlueprints.Built || SpiritBlueprints.Entries.Count == 0) throw new InvalidOperationException("WeaponSpiritShaman: Icons.Apply() before SpiritBlueprints.Build()");
            var rows = SpiritBlueprints.Entries.Select(e => e.Row).ToList();
            var problems = IconTable.Problems(byGuid.Keys, rows);
            if (problems.Count != 0) throw new InvalidOperationException("WeaponSpiritShaman: IconTable does not match the built blueprints: " + string.Join(" | ", problems.Take(8).ToArray()));

            foreach (var kv in IconTable.Targets)
            {
                var src = IconTable.SourceOf(kv.Key);
                var source = Get<BlueprintScriptableObject>(src.Guid);
                Put(byGuid[kv.Key], GetF(source, "m_Icon") as Sprite, src.Key + " " + src.Guid + " (" + source.name + ")");
            }
            // 영병 특성: 그 범주 무기 유형의 아이콘(BuildFeature 가 이미 넣었다 — 같은 그림이어야 한다).
            foreach (var kv in IconTable.TypeIconTargets(rows))
            {
                var type = Get<BlueprintWeaponType>(kv.Value);
                Put(byGuid[kv.Key], GetF(type, "m_Icon") as Sprite, "weapon type " + kv.Value + " (" + type.name + ")");
            }
            // 시작 무기·두 번째 머리: 쓰지 않는다. 엔진 폴백(아이템 m_Icon ?? Type.Icon)이 실제로 그림을 내는지만 본다.
            foreach (var kv in IconTable.ItemIconTargets(rows))
            {
                var w = byGuid[kv.Key] as BlueprintItemWeapon;
                if (w == null) throw new InvalidOperationException("WeaponSpiritShaman: IconTable says " + byGuid[kv.Key].name + " is a start weapon, but it is " + byGuid[kv.Key].GetType().Name);
                if (w.Type == null || w.Type.AssetGuid.ToString() != kv.Value) throw new InvalidOperationException("WeaponSpiritShaman: start weapon " + w.name + " type is not " + kv.Value);
                if (w.Icon == null) throw new InvalidOperationException("WeaponSpiritShaman: start weapon " + w.name + " has no icon (item m_Icon and type " + w.Type.name + " icon are both null)");
            }

            foreach (var kv in IconTable.NoIcon.Concat(IconTable.UnitNoIcon(rows)).Concat(IconTable.TierNoIcon(rows)))
            {
                var bp = byGuid[kv.Key];
                if (kv.Value == IconTable.ReasonHidden)
                {
                    bool? visible = Visible(bp);
                    if (visible != false) throw new InvalidOperationException("WeaponSpiritShaman: IconTable says " + bp.name + " is hidden, but it is " + (visible == null ? "not a unit fact" : "visible") + " - give it an icon source");
                }
                else if (kv.Value == IconTable.ReasonArchetype) { if (!(bp is BlueprintArchetype)) throw new InvalidOperationException("WeaponSpiritShaman: IconTable says " + bp.name + " is the archetype, but it is " + bp.GetType().Name); }
                else if (kv.Value == IconTable.ReasonPetClass) { if (!(bp is BlueprintCharacterClass)) throw new InvalidOperationException("WeaponSpiritShaman: IconTable says " + bp.name + " is the pet class, but it is " + bp.GetType().Name); }
                else if (kv.Value == IconTable.ReasonUnit) { if (!(bp is BlueprintUnit)) throw new InvalidOperationException("WeaponSpiritShaman: IconTable says " + bp.name + " is a spirit unit, but it is " + bp.GetType().Name); }
                else if (kv.Value == IconTable.ReasonPortrait) { if (!(bp is BlueprintPortrait)) throw new InvalidOperationException("WeaponSpiritShaman: IconTable says " + bp.name + " is the spirit portrait, but it is " + bp.GetType().Name); }
                else throw new InvalidOperationException("WeaponSpiritShaman: unknown no-icon reason '" + kv.Value + "' for " + bp.name);
            }

            // 마지막 문: 우리 것 중 보이는 사실은 전부 아이콘이 있다.
            var blank = own.Where(bp => Visible(bp) == true && (GetF(bp, "m_Icon") as Sprite) == null).Select(bp => bp.name).ToArray();
            if (blank.Length != 0) throw new InvalidOperationException("WeaponSpiritShaman: player-visible blueprints without an icon: " + string.Join(", ", blank));
            Applied = true;
        }

        static void Put(SimpleBlueprint target, Sprite icon, string what)
        {
            if (FindField(target.GetType(), "m_Icon") == null) throw new InvalidOperationException("WeaponSpiritShaman: icon target " + target.name + " (" + target.GetType().Name + ") has no m_Icon field");
            if (icon == null) throw new InvalidOperationException("WeaponSpiritShaman: icon source " + what + " has no icon (target " + target.name + ")");
            var cur = GetF(target, "m_Icon") as Sprite;
            // 이미 복사해 둔 것(강신·융합·선택지·영병 특성)은 표와 같은 그림이어야 한다(C4 — 두 곳이 어긋나면 실패).
            if (cur != null && !ReferenceEquals(cur, icon)) throw new InvalidOperationException("WeaponSpiritShaman: " + target.name + " already has icon '" + cur.name + "' but IconTable says " + what + " ('" + icon.name + "')");
            SetField(target, "m_Icon", icon);
            if (!ReferenceEquals(GetF(target, "m_Icon"), icon)) throw new InvalidOperationException("WeaponSpiritShaman: icon readback failed on " + target.name);
        }

        // 보임 = true / 숨김 = false / UI 에 사실로 보이지 않음 = null(유닛 — 초상화를 쓴다 · 사실이 아닌 것). 모르는 사실 형식은 던진다(조용히 「숨김」으로 치지 않는다).
        static bool? Visible(SimpleBlueprint bp)
        {
            if (!(bp is BlueprintUnitFact)) return null;
            if (bp is BlueprintUnit) return null;
            var f = bp as BlueprintFeature;   // BlueprintFeatureSelection · BlueprintProgression 포함
            if (f != null) return !f.HideInUI;
            var b = bp as BlueprintBuff;
            if (b != null) return !b.IsHiddenInUI;
            var a = bp as BlueprintAbility;
            if (a != null) return !a.Hidden;
            var t = bp as BlueprintActivatableAbility;
            if (t != null) return !t.HiddenInUI;
            throw new InvalidOperationException("WeaponSpiritShaman: Icons does not know whether " + bp.GetType().Name + " " + bp.name + " is shown in the UI");
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
