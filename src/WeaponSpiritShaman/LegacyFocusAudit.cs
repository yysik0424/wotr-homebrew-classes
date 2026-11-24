using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
    // 옛 세이브 주술사 무기 중점화 감사(2026-11-18 추가 수리). 영병 특성(주술사, 범주 선택) 컴포넌트 #6 — 지역 로드가 끝날 때마다(IAreaHandler.OnAreaDidLoad:
    // 엔진 ApplyPostLoadFixes 뒤) 그 주술사에 남은 「고른 범주의 무기 중점화」를 관측해 순수 LegacyFocusRule.Decide 로 가른다.
    //   Remove(우리 옛 컴포넌트 런타임의 AppliedFeatures 에 있음 = 출처 확정) → 떼고 INFO 1줄 · Keep(레벨업 출처 = 플레이어 선택) → 아무것도 안 함 ·
    //   Undetermined(출처 확정 불가) → 떼지 않고 로그 없음 · 후보 0 = Disabled 경로 정상 → 로그 없음.
    // 로그는 실제로 떼어 낸 경우의 INFO 1줄(세이브가 바뀌는 사건)과 감사 실패 LogError 뿐이다 — 미확정·미실행 표지 줄은 소유자 결정 2026-11-18 로 뺐다(거슬림).
    // 블루프린트 컴포넌트 1개를 59 영병 특성이 각자 갖고, 인스턴스 필드는 두지 않는다(상태 = 정적 WARN 기록뿐).
    public class LegacyFocusAudit : UnitFactComponentDelegate, IAreaHandler, IGlobalSubscriber
    {
        public void OnAreaDidLoad()
        {
            try { AuditNow(Owner, true); }
            catch (Exception e) { ModContext.LogError("WeaponSpiritShaman: UNKNOWN legacy weapon focus audit for " + (Owner == null ? "null" : Owner.CharacterName) + " failed: " + e); }
        }

        public void OnAreaBeginUnloading() { }

        // apply = false 면 관측·판정만(점검 S28). 반환 = 후보마다의 결정(입력 순서).
        public static List<LegacyFocusRule.Decision> AuditNow(UnitEntityData unit, bool apply)
        {
            if ((object)unit == null) throw new ArgumentNullException("unit");
            if (!SpiritBlueprints.Built) throw new InvalidOperationException("WeaponSpiritShaman: legacy weapon focus audit before SpiritBlueprints.Build");
            var byFeature = SpiritBlueprints.Entries.ToDictionary(e => (BlueprintFeature)e.Feature, e => e);
            var spiritFacts = unit.Facts.List.Where(f => f.Blueprint is BlueprintFeature && byFeature.ContainsKey((BlueprintFeature)f.Blueprint)).ToList();
            if (spiritFacts.Count == 0) return new List<LegacyFocusRule.Decision>();
            if (spiritFacts.Count != 1) throw new InvalidOperationException("WeaponSpiritShaman: " + unit.CharacterName + " has " + spiritFacts.Count + " spirit features (expected 1 — PrerequisitePet NoCompanion)");
            var fact = spiritFacts[0];
            var spirit = byFeature[(BlueprintFeature)fact.Blueprint];

            // 우리 옛 컴포넌트(영병 특성의 AddParametrizedFeatures — 지금은 Disabled)의 런타임이 아직 살아 있으면 그 AppliedFeatures 가 출처 기록이다.
            var held = new List<Feature>();
            foreach (var c in fact.Components)
            {
                if (c == null || c.IsDisposed || !(c.SourceBlueprintComponent is AddParametrizedFeatures)) continue;
                var rt = c as IComponentDelegateRuntime;
                var data = rt == null ? null : rt.Data as AddParametrizedFeaturesData;
                if (data == null) throw new InvalidOperationException("WeaponSpiritShaman: " + unit.CharacterName + " spirit feature AddParametrizedFeatures runtime has no AddParametrizedFeaturesData (" + c.GetType().FullName + ")");
                if (data.AppliedFeatures != null) held.AddRange(data.AppliedFeatures.Where(f => f != null));
            }

            var wf = Get<BlueprintFeature>(Guids.Game.WeaponFocus);
            var live = new HashSet<EntityFact>(unit.Facts.List);
            var candidates = unit.Facts.List.OfType<Feature>()
                .Where(f => ReferenceEquals(f.Blueprint, wf) && f.Param != null && f.Param.WeaponCategory == spirit.Category)
                .Concat(held.Where(f => live.Contains(f))).Distinct().ToList();
            var decisions = LegacyFocusRule.Decide(candidates.Select(f => new LegacyFocusRule.FocusFact(f.UniqueId, held.Contains(f),
                f.Source.HasValue || (f.RankToSource != null && f.RankToSource.Count > 0))));
            if (!apply) return decisions;

            string who = unit.CharacterName + " (" + unit.UniqueId + ")";
            for (int i = 0; i < decisions.Count; i++)
            {
                var d = decisions[i];
                var f = candidates[i];
                string what = f.Blueprint.name + "(" + (f.Param == null || f.Param.WeaponCategory == null ? "?" : f.Param.WeaponCategory.ToString()) + ") fact " + f.UniqueId;
                if (d.Action == LegacyFocusAction.Remove)
                {
                    UnitHelper.RemoveFact(unit, f);
                    foreach (var c in fact.Components)
                    {
                        var data = (c as IComponentDelegateRuntime)?.Data as AddParametrizedFeaturesData;
                        if (data != null && data.AppliedFeatures != null) data.AppliedFeatures.Remove(f);
                    }
                    ModContext.Log("WSS INFO legacy weapon focus removed: " + who + " category " + spirit.Row.Category + " " + what + " — " + d.Reason);
                }
            }
            return decisions;
        }
    }
}
