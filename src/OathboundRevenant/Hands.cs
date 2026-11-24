using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    //   RevenantLayOnHands = 본편 LayOnHandsOthers 복제 + CanTargetEnemies/Self false + AbilityTargetIsAlly{Not = false}(HandsPlan).
    //   망자의 손 특성 = AddFacts[LayonHandsFact, RevenantLayOnHands] + ReplaceCasterLevelOfAbility{RevenantLayOnHands, PaladinClass}(본편 LayOnHandsFeature 의 묶음에서 Self 만 뺀 모양).
    //   20레벨 극대화 = 본편 HolyChampion 의 AutoMetamagic 복제, Abilities = [RevenantLayOnHands](본편 목록은 복제본을 모른다 — 부록 A-d).
    // 복제 경계: 컴포넌트는 CloneBoundary.CopyComponent(블루프린트·Unity 객체·다른 컴포넌트만 공유 — 행동(Element)도 복제), 복제한 행동의 Owner = 우리 블루프린트.
    public static class Hands
    {
        public static BlueprintAbility Ability;
        public static BlueprintFeature Feature;
        public static BlueprintFeature MaximizeFeature;

        static bool KeepSharedDeep(object v) { return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent; }

        public static void Build()
        {
            if (Ability != null) throw new InvalidOperationException("OathboundRevenant: Hands.Build() called twice");
            var src = Get<BlueprintAbility>(HandsPlan.SourceGuid);
            var srcTypes = (src.ComponentsArray ?? new BlueprintComponent[0]).Select(c => c.GetType().Name).ToList();
            if (!srcTypes.SequenceEqual(HandsPlan.KeptComponents))
                throw new InvalidOperationException("OathboundRevenant: vanilla LayOnHandsOthers components [" + string.Join(",", srcTypes) + "] != HandsPlan.KeptComponents (vanilla layout changed?)");
            if (!src.CanTargetEnemies) throw new InvalidOperationException("OathboundRevenant: vanilla LayOnHandsOthers.CanTargetEnemies is already false (the reason for the clone changed)");

            Ability = Create<BlueprintAbility>("OathboundRevenantLayOnHands", Guids.RevenantLayOnHands, a => CopyAbilityFields(src, a));
            foreach (var ch in HandsPlan.FieldChanges) SetAbilityFlag(Ability, ch.Key, ch.Value);
            var copies = src.ComponentsArray.Select(c => CloneBoundary.CopyComponent(c, KeepSharedDeep, RevenantRules.Dials.ActionCloneDepth)).ToList(); // 행동 트리(AbilityEffectRunAction) 포함
            var ally = new AbilityTargetIsAlly { Not = false };
            copies.Add(ally);
            var noNea = new AbilityTargetHasFact { Inverted = true, FromCaster = false };
            SetField(noNea, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(Guids.Game.NegativeEnergyAffinity) });
            copies.Add(noNea);
            AddComponents(Ability, copies.ToArray());
            RebindElements(Ability);
            VerifyAbility(src);

            // 망자의 손 특성(레벨 표 껍데기를 채움).
            Feature = Archetype.Shell("RevenantHands");
            var facts = new AddFacts();
            SetField(facts, "m_Facts", HandsPlan.FeatureFacts.Select(Ref<BlueprintUnitFactReference>).ToArray());
            var lohFeature = Get<BlueprintFeature>(Guids.Game.LayOnHandsFeature);
            var rclaSrc = lohFeature.ComponentsArray.OfType<ReplaceCasterLevelOfAbility>().FirstOrDefault(r => r.Spell != null && r.Spell.AssetGuid.ToString() == HandsPlan.SourceGuid);
            if (rclaSrc == null) throw new InvalidOperationException("OathboundRevenant: vanilla LayOnHandsFeature has no ReplaceCasterLevelOfAbility for LayOnHandsOthers");
            var rcla = CloneBoundary.CopyComponent(rclaSrc, KeepSharedDeep, RevenantRules.Dials.CloneDepth);
            SetField(rcla, "m_Spell", Ref<BlueprintAbilityReference>(Guids.RevenantLayOnHands));
            AddComponents(Feature, facts, rcla);
            if (rcla.Spell != Ability || rcla.Class == null || rcla.Class.AssetGuid.ToString() != Guids.Game.PaladinClass) throw new InvalidOperationException("OathboundRevenant: RevenantHands ReplaceCasterLevelOfAbility readback failed");
            var factGuids = facts.Facts.Select(f => f.AssetGuid.ToString()).ToList();
            if (!factGuids.SequenceEqual(HandsPlan.FeatureFacts) || factGuids.Contains(HandsPlan.SourceGuid))
                throw new InvalidOperationException("OathboundRevenant: RevenantHands facts [" + string.Join(",", factGuids) + "] != HandsPlan.FeatureFacts");
            Archetype.MarkFilled("RevenantHands");

            // 20레벨 극대화.
            MaximizeFeature = Archetype.Shell("RevenantHandsMaximize");
            var champ = Get<BlueprintFeature>(Guids.Game.HolyChampion);
            var amSrc = champ.ComponentsArray.OfType<AutoMetamagic>().ToArray();
            if (amSrc.Length != 1 || amSrc[0].Metamagic.ToString() != HandsPlan.MaximizeMetamagic) throw new InvalidOperationException("OathboundRevenant: vanilla HolyChampion has " + amSrc.Length + " AutoMetamagic (expected one Maximize)");
            var am = CloneBoundary.CopyComponent(amSrc[0], KeepSharedDeep, RevenantRules.Dials.CloneDepth);
            am.Abilities = new List<BlueprintAbilityReference> { Ref<BlueprintAbilityReference>(Guids.RevenantLayOnHands) };
            AddComponents(MaximizeFeature, am);
            MaximizeFeature.HideInUI = true;
            if (am.Abilities.Count != 1 || am.Abilities[0].Get() != Ability || amSrc[0].Abilities.Any(r => r.Guid.ToString() == Guids.RevenantLayOnHands))
                throw new InvalidOperationException("OathboundRevenant: maximize readback failed (our list must hold exactly the clone; HolyChampion's list must stay vanilla)");
            Archetype.MarkFilled("RevenantHandsMaximize");
        }

        // 직렬화 필드만 복사(캐시 m_Cached*·Cacheable·Nullable 캐시는 빼서 새로 계산되게). 이름·GUID·컴포넌트 배열은 Create 가 정한다.
        internal static void CopyAbilityFields(BlueprintAbility src, BlueprintAbility dst)
        {
            foreach (var t in new[] { typeof(BlueprintAbility), typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact) })
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.IsLiteral || f.IsInitOnly || f.Name.StartsWith("m_Cached")) continue;
                    var ft = f.FieldType;
                    if (ft.IsGenericType && (ft.GetGenericTypeDefinition() == typeof(Nullable<>) || ft.Name.StartsWith("Cacheable"))) continue;
                    f.SetValue(dst, CloneKit.CopyOf(f.GetValue(src), KeepSharedDeep, RevenantRules.Dials.CloneDepth));
                }
        }

        static void SetAbilityFlag(BlueprintAbility a, string name, bool value)
        {
            switch (name)
            {
                case "CanTargetEnemies": a.CanTargetEnemies = value; break;
                case "CanTargetFriends": a.CanTargetFriends = value; break;
                case "CanTargetSelf": a.CanTargetSelf = value; break;
                default: throw new ArgumentException("OathboundRevenant: HandsPlan field change '" + name + "' is not a known ability flag");
            }
        }

        // 복제 컴포넌트 안의 행동(Element)의 Owner = 우리 블루프린트(본편 이름을 달고 남지 않게).
        internal static int RebindElements(BlueprintScriptableObject owner)
        {
            int n = 0;
            foreach (var c in owner.ComponentsArray)
                GraphWalk.Visit(c, v => v is SimpleBlueprint || v is UnityEngine.Object || (v is BlueprintComponent && !ReferenceEquals(v, c)), RevenantRules.Dials.ActionCloneDepth, v =>
                {
                    var e = v as Element;
                    if (e != null) { e.Owner = owner; n++; }
                });
            return n;
        }

        // 빌드 되읽기: 형식 순서 == Kept + Added, 플래그 == HandsPlan, 아군 조건 Not == false, 본편과 공유하는 가변 하위 객체 0, 복제 행동의 Owner == 복제본, 원본 불변.
        static void VerifyAbility(BlueprintAbility src)
        {
            var types = Ability.ComponentsArray.Select(c => c.GetType().Name).ToList();
            var want = HandsPlan.KeptComponents.Concat(HandsPlan.AddedComponents).ToList();
            if (!types.SequenceEqual(want)) throw new InvalidOperationException("OathboundRevenant: RevenantLayOnHands components [" + string.Join(",", types) + "] != HandsPlan [" + string.Join(",", want) + "]");
            foreach (var ch in HandsPlan.FieldChanges)
            {
                bool got = ch.Key == "CanTargetEnemies" ? Ability.CanTargetEnemies : ch.Key == "CanTargetFriends" ? Ability.CanTargetFriends : Ability.CanTargetSelf;
                if (got != ch.Value) throw new InvalidOperationException("OathboundRevenant: RevenantLayOnHands." + ch.Key + " = " + got + ", HandsPlan wants " + ch.Value);
            }
            var ally = Ability.ComponentsArray.OfType<AbilityTargetIsAlly>().Single();
            if (ally.Not) throw new InvalidOperationException("OathboundRevenant: RevenantLayOnHands AbilityTargetIsAlly.Not must be false");
            for (int i = 0; i < src.ComponentsArray.Length; i++)
            {
                var sh = CloneKit.Shared(Ability.ComponentsArray[i], src.ComponentsArray[i], KeepSharedDeep, RevenantRules.Dials.ActionCloneDepth);
                if (sh.Count > 0) throw new InvalidOperationException("OathboundRevenant: RevenantLayOnHands component " + i + " shares mutable objects with vanilla: " + string.Join(",", sh));
                if (!ReferenceEquals(Ability.ComponentsArray[i].OwnerBlueprint, Ability)) throw new InvalidOperationException("OathboundRevenant: RevenantLayOnHands component " + i + " is not owned by the clone");
            }
            int foreign = 0;
            foreach (var c in Ability.ComponentsArray)
                GraphWalk.Visit(c, v => v is SimpleBlueprint || v is UnityEngine.Object || (v is BlueprintComponent && !ReferenceEquals(v, c)), RevenantRules.Dials.ActionCloneDepth, v => { var e = v as Element; if (e != null && !ReferenceEquals(e.Owner, Ability)) foreign++; });
            if (foreign > 0) throw new InvalidOperationException("OathboundRevenant: " + foreign + " cloned action(s) of RevenantLayOnHands still name another owner");
            if (!src.CanTargetEnemies || !src.CanTargetSelf) throw new InvalidOperationException("OathboundRevenant: vanilla LayOnHandsOthers flags changed during the clone");
        }
    }
}
