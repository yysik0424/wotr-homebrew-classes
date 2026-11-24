using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 아이콘 적용(2026-11-15 결함 수리, 선례 BulwarkMage Icons 84d70ed): IconTable(순수 원장)대로 본편 원본의 m_Icon 을 우리 블루프린트에 넣는다. BuildAll 의 Archetype.Register 바로 앞(시험 icons: wiring).
    // 우리 블루프린트 = BlueprintUtil.OwnBlueprints(Create) + AbilityClone.Clones(얕은 복제 — Create 밖).
    // 조용한 통과 금지(C1·C2): 표 무결성 · 원본 없음 · 원본 아이콘 null · 기존 복사값과 표가 다름 · 아이콘 없음 이유가 거짓 · 보이는 사실·항목에 아이콘 없음 — 전부 던진다(BuildFailed).
    // 아이콘(표시용 Sprite)만 쓴다 — 게임 동작 값은 건드리지 않는다. 본편 객체에는 쓰지 않는다(읽기만).
    public static class Icons
    {
        public static bool Applied { get; private set; }

        public static void Apply()
        {
            if (Applied) throw new InvalidOperationException("Golemancer: Icons.Apply() called twice");
            var own = BlueprintUtil.OwnBlueprints.Concat(AbilityClone.Clones).ToList();
            var byGuid = new Dictionary<string, SimpleBlueprint>();
            foreach (var bp in own)
            {
                if (bp == null) throw new InvalidOperationException("Golemancer: Icons.Apply: null own blueprint");
                byGuid.Add(bp.AssetGuid.ToString(), bp); // 같은 GUID 둘 = ArgumentException(시끄럽게)
            }
            var problems = IconTable.Problems(byGuid.Keys);
            if (problems.Count != 0) throw new InvalidOperationException("Golemancer: IconTable does not match the built blueprints: " + string.Join(" | ", problems.Take(8).ToArray()));

            foreach (var kv in IconTable.Targets)
            {
                var src = IconTable.SourceOf(kv.Key);
                var target = byGuid[kv.Key];
                if (FindField(target.GetType(), "m_Icon") == null) throw new InvalidOperationException("Golemancer: icon target " + target.name + " (" + target.GetType().Name + ") has no m_Icon field");
                var source = Get<BlueprintScriptableObject>(src.Guid);
                var icon = GetF(source, "m_Icon") as Sprite;
                if (icon == null) throw new InvalidOperationException("Golemancer: icon source " + src.Key + " " + src.Guid + " (" + source.name + ") has no icon");
                var cur = GetF(target, "m_Icon") as Sprite;
                // 빌더가 이미 복사해 둔 것(학파 7·폭탄 절반·합체 3·응급 수리 8)은 표와 같은 그림이어야 한다(C4 — 두 곳이 어긋나면 실패).
                if (cur != null && !ReferenceEquals(cur, icon)) throw new InvalidOperationException("Golemancer: " + target.name + " already has icon '" + cur.name + "' but IconTable says " + src.Key + " ('" + icon.name + "')");
                SetField(target, "m_Icon", icon);
                if (!ReferenceEquals(GetF(target, "m_Icon"), icon)) throw new InvalidOperationException("Golemancer: icon readback failed on " + target.name);
            }

            var exempt = new HashSet<string>();
            foreach (var kv in IconTable.NoIcon)
            {
                var bp = byGuid[kv.Key];
                if (kv.Value == IconTable.ReasonHidden)
                {
                    bool? visible = Visible(bp);
                    if (visible != false) throw new InvalidOperationException("Golemancer: IconTable says " + bp.name + " is hidden, but it is " + (visible == null ? "not a unit fact / item" : "visible") + " - give it an icon source");
                }
                else if (kv.Value == IconTable.ReasonNoIconField)
                {
                    if (FindField(bp.GetType(), "m_Icon") != null) throw new InvalidOperationException("Golemancer: IconTable says " + bp.name + " has no m_Icon field, but " + bp.GetType().Name + " has one");
                    exempt.Add(kv.Key);
                }
                else if (kv.Value == IconTable.ReasonArchetype)
                {
                    if (!(bp is BlueprintArchetype)) throw new InvalidOperationException("Golemancer: IconTable says " + bp.name + " is the archetype, but it is " + bp.GetType().Name);
                    exempt.Add(kv.Key);
                }
                else if (kv.Value == IconTable.ReasonUnit)
                {
                    if (!(bp is BlueprintUnit)) throw new InvalidOperationException("Golemancer: IconTable says " + bp.name + " is a unit, but it is " + bp.GetType().Name);
                    exempt.Add(kv.Key);
                }
                else throw new InvalidOperationException("Golemancer: unknown no-icon reason '" + kv.Value + "' for " + bp.name);
            }

            // 마지막 문: 우리 것 중(형식으로 확인한 예외 밖) 보이는 사실·항목·직업은 전부 아이콘이 있다.
            var blank = own.Where(bp => !exempt.Contains(bp.AssetGuid.ToString()) && Visible(bp) == true && (GetF(bp, "m_Icon") as Sprite) == null).Select(bp => bp.name).ToArray();
            if (blank.Length != 0) throw new InvalidOperationException("Golemancer: player-visible blueprints without an icon: " + string.Join(", ", blank));
            Applied = true;
        }

        // 보임 = true / 숨김 = false / 아이콘을 보이는 형식이 아님 = null. 모르는 사실 형식은 던진다(조용히 「숨김」으로 치지 않는다).
        static bool? Visible(SimpleBlueprint bp)
        {
            if (bp is BlueprintItem) return true;            // 손 칸·인벤토리에 보인다(슬램)
            var c = bp as BlueprintCharacterClass;
            if (c != null) return !c.HideInUI;
            if (!(bp is BlueprintUnitFact)) return null;
            var f = bp as BlueprintFeature;                  // BlueprintFeatureSelection·BlueprintProgression 포함
            if (f != null) return !f.HideInUI;
            var b = bp as BlueprintBuff;
            if (b != null) return !b.IsHiddenInUI;
            var a = bp as BlueprintAbility;
            if (a != null) return !a.Hidden;
            var t = bp as BlueprintActivatableAbility;
            if (t != null) return !t.HiddenInUI;
            throw new InvalidOperationException("Golemancer: Icons does not know whether " + bp.GetType().Name + " " + bp.name + " is shown in the UI");
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("Golemancer: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
