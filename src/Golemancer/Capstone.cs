using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Newtonsoft.Json;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 20레벨 면역 공유(설계 §4.8, owner approval 2026-11-14 — 과부하는 제외). 판정은 순수 CapstoneRules.
    //   20레벨 형태 키(CapstoneRules.CapstoneKeys = adamantine/greatergold/mithral @T5)의 합체 형태 버프 Polymorph.m_Facts 에 재질별 공유 버프 1개를 더한다(Fusion.BuildForm).
    //   → 형태가 붙는 동안만 공유 버프가 있고, 형태가 빠지면 엔진이 공유 버프를 지운다(합체 수명과 같은 원천 — 따로 붙이고 떼는 코드 없음).
    //   공유 버프의 SharedImmunityGrant 가 추가 면역만 본체에 더한다. 이미 가진 사실은 더하지도 지우지도 않는다(CapstoneRules.GrantPlan — 엔진 AddFacts 를 쓰면 기존 사실을 지운다).
    // 알려진 외부 규칙(명시): 본체가 같은 사실을 원래 갖고 있으면(장비·신화 등) 그대로 둔다 + INFO. 그 밖의 모순(더한 뒤 그 사실이 새것이 아님)은 던진다.
    public static class Capstone
    {
        public static BlueprintFeature Feature;
        static readonly Dictionary<string, BlueprintBuff> buffsByMaterial = new Dictionary<string, BlueprintBuff>();

        // 재질 → 공유 버프 GUID. 키 집합 = CapstoneRules.CapstoneKeys 의 재질 집합(Build 가 대조 — 다르면 빌드 실패).
        static readonly Dictionary<string, string> BuffGuids = new Dictionary<string, string>
        {
            { "adamantine", Guids.SharedImmunityAdamantine },
            { "greatergold", Guids.SharedImmunityGreaterGold },
            { "mithral", Guids.SharedImmunityMithral },
        };

        // 형태 키 → Polymorph.m_Facts 에 더할 공유 버프 GUID(20레벨 형태가 아니면 빈 배열). Fusion.BuildForm 이 부른다.
        public static string[] FormExtraFactGuids(string buffKey)
        {
            if (!CapstoneRules.FormGetsShared(buffKey)) return new string[0];
            var m = MaterialCatalog.MaterialOfKey(buffKey);
            string g;
            if (!BuffGuids.TryGetValue(m.Id, out g)) throw new KeyNotFoundException("Golemancer: capstone form " + buffKey + " has no shared-immunity buff GUID");
            return new[] { g };
        }

        public static BlueprintBuff BuffOf(string materialId)
        {
            BlueprintBuff b;
            if (Feature == null || !buffsByMaterial.TryGetValue(materialId, out b)) throw new KeyNotFoundException("Golemancer: no shared-immunity buff for " + materialId + " (Capstone.Build not run?)");
            return b;
        }





        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Golemancer: Capstone.Build() called twice");
            if (Fusion.Toggle != null) throw new InvalidOperationException("Golemancer: Capstone.Build() must run before Fusion.Build() (forms reference the shared-immunity buffs)");
            var mats = CapstoneRules.CapstoneKeys().Select(k => MaterialCatalog.MaterialOfKey(k).Id).Distinct().ToArray();
            if (!new HashSet<string>(mats).SetEquals(BuffGuids.Keys))
                throw new InvalidOperationException("Golemancer: capstone materials [" + string.Join(",", mats) + "] != shared-immunity buff table [" + string.Join(",", BuffGuids.Keys) + "]");
            foreach (var id in mats)
            {
                var m = MaterialCatalog.Get(id);
                var shared = CapstoneRules.SharedImmunities(m);
                foreach (var g in shared) if (Get<BlueprintUnitFact>(g) == null) throw new InvalidOperationException("Golemancer: shared immunity " + g + " of " + id + " does not resolve");
                var b = Create<BlueprintBuff>("GolemancerSharedImmunity_" + id, BuffGuids[id], x =>
                {
                    SetField(x, "m_DisplayName", Strings.L("SharedImmunity.Name"));
                    SetField(x, "m_Description", Strings.L("SharedImmunity.Description"));
                    SetField(x, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                    x.Stacking = StackingType.Replace;
                    EmptyFxOnRemove(x); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — Buff.OnRemove @00B0 NRE)
                });
                var grant = new SharedImmunityGrant();
                grant.m_Facts = shared.Select(g => Ref<BlueprintUnitFactReference>(g)).ToArray();
                AddComponents(b, grant);
                var back = b.ComponentsArray.OfType<SharedImmunityGrant>().ToArray();
                if (b.ComponentsArray.Length != 1 || back.Length != 1 || !back[0].FactGuids.SequenceEqual(shared) || b.StayOnDeath)
                    throw new InvalidOperationException("Golemancer: shared-immunity buff " + b.name + " readback failed");
                buffsByMaterial.Add(id, b);
            }
            Feature = Create<BlueprintFeature>("GolemancerSharedImmunityFeature", Guids.SharedImmunityFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("SharedImmunityFeature.Name"));
                SetField(f, "m_Description", Strings.L("SharedImmunityFeature.Description"));
                SetField(f, "m_DescriptionShort", Strings.L("SharedImmunityFeature.Description"));
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
            Archetype.AddAt(GolemRules.Dials.CapstoneLevel, Feature);
        }
    }

    public class SharedImmunityGrantData
    {
        [JsonProperty] public List<UnitFact> Added = new List<UnitFact>();
    }

    // 공유 버프 컴포넌트(블루프린트 공유 — 상태는 Data). 켜질 때 계획대로 더하고, 꺼질 때 자기가 더한 것만 지운다.
    public class SharedImmunityGrant : UnitBuffComponentDelegate<SharedImmunityGrantData>
    {
        public BlueprintUnitFactReference[] m_Facts;

        public string[] FactGuids { get { if (m_Facts == null) throw new InvalidOperationException("Golemancer: SharedImmunityGrant without facts"); return m_Facts.Select(r => r.Guid.ToString()).ToArray(); } }

        protected override void OnActivate()
        {
            if (Data.Added.Count != 0) throw new InvalidOperationException("Golemancer: shared immunity grant activated twice on " + Owner.CharacterName + " (" + Data.Added.Count + " facts still recorded)");
            var before = Owner.Facts.List.ToList();
            var plan = CapstoneRules.GrantPlan(FactGuids, new HashSet<string>(before.Select(f => f.Blueprint.AssetGuid.ToString())));
            foreach (var g in plan.Value) Info("already has " + g + " — left as it is (not shared, not removed later)");
            foreach (var g in plan.Key)
            {
                var bp = Get<BlueprintUnitFact>(g);
                var fact = Owner.Descriptor.AddFact(bp) as UnitFact;
                if (fact == null || before.Contains(fact) || !Owner.Facts.List.Contains(fact))
                    throw new InvalidOperationException("Golemancer: shared immunity " + bp.name + " on " + Owner.CharacterName + " readback failed (" + (fact == null ? "null" : "not a new fact") + ")");
                Data.Added.Add(fact);
            }
            Info("shared " + Data.Added.Count + " immunity fact(s) [" + string.Join(",", Data.Added.Select(f => f.Blueprint.name).ToArray()) + "]");
        }

        protected override void OnDeactivate()
        {
            foreach (var f in Data.Added.ToList())
            {
                Owner.Descriptor.RemoveFact(f);
                if (Owner.Facts.List.Contains(f)) throw new InvalidOperationException("Golemancer: shared immunity " + f.Blueprint.name + " still on " + Owner.CharacterName + " after removal");
            }
            Data.Added.Clear();
        }

        void Info(string text)
        {
            var mp = Owner.Get<UnitPartGolemMaster>();
            if (mp != null) mp.Info("capstone: " + text); else ModContext.Log("Golemancer: capstone: " + text + " (" + Owner.CharacterName + ")");
        }
    }
}
