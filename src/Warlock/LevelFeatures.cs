using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Warlock
{
    // 레벨표 마커 10(그룹 무관 — 계획 Global Constraints): Ward1(L2)·Augment4·Reach6·Ward2(L8)·Double10·HeatAccel12·Ward3(L14)·Extend16·Ward4(L18)·Capstone20.
    // 명세·레벨 = KitSpec.Markers()(레벨 숫자는 WarlockRules.Levels 원장). Augment4 = AddFacts[AugmentSummoning 38155ca9…], 나머지 컴포넌트 0(규칙이 HasFact 로 읽는다).
    public static class LevelFeatures
    {
        static readonly Dictionary<string, BlueprintFeature> byGuid = new Dictionary<string, BlueprintFeature>();
        public static bool Built { get; private set; }

        public static void Build()
        {
            if (Built || byGuid.Count > 0) throw new InvalidOperationException("Warlock: LevelFeatures.Build() called twice");
            var icon = KitBuild.Icon(Guids.Game.MagicMissile);
            foreach (var m in KitSpec.Markers())
            {
                var f = KitBuild.Feature("Warlock" + m.Key + "Feature", m.Guid, m.Key, icon);
                if (m.FactGuid != null)
                {
                    var fact = Get<BlueprintFeature>(m.FactGuid);
                    var c = new AddFacts();
                    SetField(c, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(m.FactGuid) });
                    AddComponents(f, c);
                    if (c.Facts.Count() != 1 || c.Facts.First() != fact || f.ComponentsArray.Length != 1) throw new InvalidOperationException("Warlock: marker " + f.name + " AddFacts readback failed");
                }
                else if (f.ComponentsArray != null && f.ComponentsArray.Length != 0) throw new InvalidOperationException("Warlock: marker " + f.name + " must have 0 components");
                Archetype.AddAt(m.Level, f);
                byGuid.Add(m.Guid, f);
            }
            if (byGuid.Count != 10) throw new InvalidOperationException("Warlock: built " + byGuid.Count + " level markers, expected 10");
            Built = true;
        }

        public static BlueprintFeature Marker(string guid)
        {
            BlueprintFeature f;
            if (!Built || guid == null || !byGuid.TryGetValue(guid, out f)) throw new InvalidOperationException("Warlock: LevelFeatures.Marker(" + guid + ") before Build() or unknown");
            return f;
        }
    }
}
