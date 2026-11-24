using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Newtonsoft.Json;
using static PackWarden.BlueprintUtil;

namespace PackWarden
{
    // 설계 4.6. 상 효과는 "팩 버프"에만 달려 있고 팩 버프는 구성원에게만 붙는다. 지도자의 FierceBuff/ToughBuff 는 스탯을 건드리지 않는다.
    public static class Aspects
    {
        public static BlueprintBuff FierceBuff, ToughBuff, FiercePackBuff, ToughPackBuff, ToughTempHpBuff;
        public static BlueprintFeature AspectsFeature, BothFeature;
        public static BlueprintActivatableAbility FierceAbility, ToughAbility;

        public static void Build()
        {
            // 아이콘: 본편 헌터 동물 초점 특성(Guids.Game.HunterAnimalFocusFeature)의 아이콘을 여섯 블루프린트 모두에 재사용.
            var icon = Get<BlueprintUnitFact>(Guids.Game.HunterAnimalFocusFeature).Icon;
            if (icon == null) throw new InvalidOperationException("PackWarden: HunterAnimalFocusFeature has no icon");

            // 팩 버프(구성원에게 붙음, 아이콘 표시).
            FiercePackBuff = Buff("PackWardenAspectFiercePackBuff", Guids.AspectFiercePackBuff, "Aspect.Fierce", icon);
            AddComponents(FiercePackBuff,
                new PackLeaderWisdomStatBonus { Stat = StatType.AdditionalAttackBonus, Descriptor = ModifierDescriptor.UntypedStackable },
                new PackLeaderWisdomStatBonus { Stat = StatType.AdditionalDamage, Descriptor = ModifierDescriptor.UntypedStackable });

            ToughPackBuff = Buff("PackWardenAspectToughPackBuff", Guids.AspectToughPackBuff, "Aspect.Tough", icon);
            AddComponents(ToughPackBuff,
                new PackLeaderWisdomStatBonus { Stat = StatType.AC, Descriptor = ModifierDescriptor.UntypedStackable });

             
            ToughTempHpBuff = Buff("PackWardenAspectToughTempHpBuff", Guids.AspectToughTempHpBuff, "Aspect.ToughTempHp", icon);
            AddComponents(ToughTempHpBuff, new PackToughTempHp());

            // 지도자 버프(활성화 능력의 버프, 표시됨 — 어느 상이 켜졌는지 보이게). 스탯 컴포넌트 없음, 전파기만.
            FierceBuff = Buff("PackWardenAspectFierceBuff", Guids.AspectFierceBuff, "Aspect.Fierce", icon);
            AddComponents(FierceBuff, new PackAspectPropagator { m_PackBuff = Ref<BlueprintBuffReference>(Guids.AspectFiercePackBuff) });

            ToughBuff = Buff("PackWardenAspectToughBuff", Guids.AspectToughBuff, "Aspect.Tough", icon);
            AddComponents(ToughBuff, new PackAspectPropagator { m_PackBuff = Ref<BlueprintBuffReference>(Guids.AspectToughPackBuff) });

            FierceAbility = Ability("PackWardenAspectFierceAbility", Guids.AspectFierceAbility, Guids.AspectFierceBuff, "Aspect.Fierce", icon);
            ToughAbility = Ability("PackWardenAspectToughAbility", Guids.AspectToughAbility, Guids.AspectToughBuff, "Aspect.Tough", icon);
            if (FierceAbility.Buff != FierceBuff) throw new InvalidOperationException("PackWarden: FierceAbility.Buff readback failed");
            if (ToughAbility.Buff != ToughBuff) throw new InvalidOperationException("PackWarden: ToughAbility.Buff readback failed");

            AspectsFeature = PackCall.Feature("PackWardenAspectsFeature", Guids.AspectsFeature, "Aspects", icon);
            var addFacts = new AddFacts();
            SetField(addFacts, "m_Facts", new[]
            {
                Ref<BlueprintUnitFactReference>(Guids.AspectFierceAbility),
                Ref<BlueprintUnitFactReference>(Guids.AspectToughAbility)
            });
            AddComponents(AspectsFeature, addFacts);
            if (!addFacts.Facts.Contains(FierceAbility) || !addFacts.Facts.Contains(ToughAbility))
                throw new InvalidOperationException("PackWarden: AspectsFeature AddFacts readback failed");

            BothFeature = PackCall.Feature("PackWardenAspectBothFeature", Guids.AspectBothFeature, "AspectBoth", icon);
            AddComponents(BothFeature, new IncreaseActivatableAbilityGroupSize { Group = ActivatableAbilityGroup.HunterAnimalFocus });

            Archetype.AddAt(1, AspectsFeature);
            Archetype.AddAt(20, BothFeature);
        }

        static BlueprintBuff Buff(string name, string guid, string key, UnityEngine.Sprite icon)
        {
            return Create<BlueprintBuff>(name, guid, b =>
            {
                PackCall.Names(b, key, icon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace; // 같은 버프를 다시 붙여도 쌓이지 않고 교체(스탯 이중 적용 방지)
                b.FxOnStart = new PrefabLink();
                b.FxOnRemove = new PrefabLink();
                b.ResourceAssetIds = new string[0];
            });
        }

        // 자유 행동: ActivationType.Immediately + ActivatableAbilityUnitCommand 컴포넌트 없음 →
        // ActivatableAbility.TryStart 가 명령 비용을 보지 않고 바로 켠다(IL 확인: 명령 쿨다운 검사는 그 컴포넌트가 있을 때만).
        // 한 그룹 크기 1: OnDidTurnOn 이 그룹 안에서 켜진 순서(오래된 것부터)대로 가중치 합이 그룹 크기를 넘으면 꺼 버린다.
        static BlueprintActivatableAbility Ability(string name, string guid, string buffGuid, string key, UnityEngine.Sprite icon)
        {
            return Create<BlueprintActivatableAbility>(name, guid, a =>
            {
                PackCall.Names(a, key, icon);
                a.Group = ActivatableAbilityGroup.HunterAnimalFocus;
                a.WeightInGroup = 1;
                a.ActivationType = AbilityActivationType.Immediately;
                a.DeactivateImmediately = true;
                a.DoNotTurnOffOnRest = true;
                a.ResourceAssetIds = new string[0];
                SetField(a, "m_Buff", Ref<BlueprintBuffReference>(buffGuid));
            });
        }

        static void RequireBuilt()
        {
            if (FierceBuff == null || ToughBuff == null || FiercePackBuff == null || ToughPackBuff == null)
                throw new InvalidOperationException("PackWarden: Aspects.Build() has not run");
        }

        public static bool IsToughActive(UnitEntityData leader)
        {
            RequireBuilt();
            if (leader == null) throw new ArgumentNullException("leader");
            return leader.Buffs.GetBuff(ToughBuff) != null;
        }

        static bool IsFierceActive(UnitEntityData leader)
        {
            RequireBuilt();
            if (leader == null) throw new ArgumentNullException("leader");
            return leader.Buffs.GetBuff(FierceBuff) != null;
        }

         
        public static void ApplyCurrentAspectTo(UnitEntityData leader, UnitEntityData member)
        {
            RequireBuilt();
            if (leader == null) throw new ArgumentNullException("leader");
            if (member == null) throw new ArgumentNullException("member");
            if (ReferenceEquals(leader, member)) throw new InvalidOperationException("PackWarden: pack aspects must never be applied to the leader: " + leader);
            if (IsFierceActive(leader)) member.Buffs.AddBuff(FiercePackBuff, leader, null, null);
            if (IsToughActive(leader)) member.Buffs.AddBuff(ToughPackBuff, leader, null, null);
        }

        public static void RemoveAspectsFrom(UnitEntityData member)
        {
            RequireBuilt();
            if (member == null) throw new ArgumentNullException("member");
            RemoveBuff(member, FiercePackBuff, null);
            RemoveBuff(member, ToughPackBuff, null);
        }

        // leader 가 주어지면 그 지도자가 시전한 버프만 제거(한 파티에 무리 조련사 둘일 때 서로의 버프를 건드리지 않음).
        // null 이면 시전자 무관(RemoveAspectsFrom 공개 API 용). 버프가 없으면 이미 없는 것(정상).
        internal static void RemoveBuff(UnitEntityData unit, BlueprintBuff bp, UnitEntityData leader)
        {
            var b = unit.Buffs.GetBuff(bp);
            if (b == null) return;
            if (leader != null && !ReferenceEquals(b.Context.MaybeCaster, leader)) return;
            b.Remove();
        }

        // 제거용 목록: 생존 여부로 거르지 않는다. 전환 순간 게임 밖인 펫·보조에게도 이전 상의 버프가 남지 않게 한다.
        // 펫이 없거나 빈 슬롯/해석 안 되는 참조(null)만 건너뛴다.
        internal static List<UnitEntityData> AllMembersForRemoval(UnitEntityData leader)
        {
            var list = new List<UnitEntityData>();
            var pet = leader.GetPet(PetType.AnimalCompanion);
            if (pet != null) list.Add(pet);
            var part = leader.Get<UnitPartPack>();
            if (part != null)
            {
                part.Validate();
                for (int i = 0; i < PackRules.AuxSlotCount; i++)
                {
                    if (part.IsEmpty(i)) continue;
                    var u = part.Auxiliaries[i].Value;
                    if (u != null) list.Add(u);
                }
            }
            if (list.Contains(leader)) throw new InvalidOperationException("PackWarden: leader ended up in its own pack members: " + leader);
            return list;
        }

        // 무리 구성원 = 주 동물 동료(살아 있고 게임 안에 있을 때) + 살아 있는 보조. 지도자는 절대 포함하지 않는다.
        // 펫이 없거나 죽었거나 게임 밖이면 건너뛴다(정상 상태). UnitPartPack 이 없으면 보조가 없는 것.
        internal static List<UnitEntityData> PackMembers(UnitEntityData leader)
        {
            var list = new List<UnitEntityData>();
            var pet = leader.GetPet(PetType.AnimalCompanion);
            if (pet == null) Main.Log("PackWarden: " + leader + " has no animal companion; aspect not propagated to a pet");
            else if (!UnitPartPack.IsLive(pet)) Main.Log("PackWarden: animal companion " + pet + " is dead or not in game; skipped");
            else list.Add(pet);
            var part = leader.Get<UnitPartPack>();
            if (part != null)
                for (int i = 0; i < PackRules.AuxSlotCount; i++)
                {
                    UnitEntityData aux;
                    switch (part.SlotAt(i, out aux))  
                    {
                        case PackRules.SlotState.Live: list.Add(aux); break;
                        case PackRules.SlotState.Empty:
                        case PackRules.SlotState.Gone: break; // 빈 자리 · 확인된 죽음/파괴/게임 밖
                        // 해석되지 않는 ID: 그 유닛에는 버프를 걸 수 없다 — 그 자리만 건너뛰고(예전과 같은 동작) 판정 불가로 남긴다. 다른 구성원의 상 전파는 막지 않는다
                        // (상은 켜고 끄는 토글이라 무리 강화처럼 「불완전한 대상 목록에 횟수를 쓰는」 위험이 없다 — 판정 기록: 보고서 「LiveAt 호출자」 표).
                        case PackRules.SlotState.Unresolved: UnitPartPack.NoteUnresolvedSkip("aspect", leader, part, i); break;
                        default: throw new InvalidOperationException("PackWarden: UnitPartPack.SlotAt returned an unknown state for slot " + i);
                    }
                }
            if (list.Contains(leader)) throw new InvalidOperationException("PackWarden: leader ended up in its own pack members: " + leader);
            return list;
        }
    }

    // 지도자의 FierceBuff/ToughBuff 에 붙는다. 켜질 때 모든 구성원에 팩 버프를, 꺼질 때 거둔다.
    public class PackAspectPropagator : UnitBuffComponentDelegate
    {
        public BlueprintBuffReference m_PackBuff;

        BlueprintBuff PackBuff
        {
            get
            {
                var b = m_PackBuff == null ? null : m_PackBuff.Get();
                if (b == null) throw new InvalidOperationException("PackWarden: PackAspectPropagator.m_PackBuff is not set or unresolved");
                return b;
            }
        }

        // OnActivate/OnDeactivate = 버프 생애(상 켜짐 / 꺼짐·억제). 저장·불러오기(PreSave/PostLoad)는 TurnOff/TurnOn 만 부르고
        // Activate/Deactivate 는 부르지 않는다(dnlib: EntityFact.IsActive 는 [JsonProperty] 로 저장되고 호출자는 Attach/Reapply/랭크/억제뿐).
        // 그래서 저장마다 팩 버프를 지웠다 붙이지 않는다. 구성원의 팩 버프는 구성원과 함께 저장되므로 불러온 뒤 재전파는 필요 없다.
        protected override void OnActivate()
        {
            var pack = PackBuff;
            foreach (var member in Aspects.PackMembers(Owner))
                member.Buffs.AddBuff(pack, Owner, null, null);
        }

        protected override void OnDeactivate()
        {
            var pack = PackBuff;
            foreach (var member in Aspects.AllMembersForRemoval(Owner))
                Aspects.RemoveBuff(member, pack, Owner);
        }
    }

    // 팩 버프에만 붙는다. 값 = 지도자 지혜 수정치(음수는 0, 설계 4.6 — 주능력치 2026-11-07 매력→지혜). 지도자 = 이 버프의 시전자.
    public class PackLeaderWisdomStatBonus : UnitBuffComponentDelegate
    {
        public StatType Stat;
        public ModifierDescriptor Descriptor;

        protected override void OnTurnOn()
        {
            var leader = Buff.Context.MaybeCaster;
            if (leader == null)
                throw new InvalidOperationException("PackWarden: pack aspect buff on " + Owner + " has no caster (must be added with the leader as caster)");
            if (ReferenceEquals(leader, Owner))
                throw new InvalidOperationException("PackWarden: pack aspect buff added to its own leader " + Owner);
            int value = PackRules.StatBonus(leader.Stats.Wisdom.Bonus);
            if (value == 0) return; // 0 이하 지혜 수정치는 보너스 없음(설계 4.6). 게임의 AddStatBonus 도 0 이면 달지 않는다.
            Owner.Stats.GetStat(Stat).AddModifier(value, Runtime, Descriptor);
        }

        protected override void OnTurnOff()
        {
            Owner.Stats.GetStat(Stat).RemoveModifiersFrom(Runtime);
        }
    }

    // 본편 TemporaryHitPointsFromAbilityValueData 와 같은 모양: 수정치를 저장되는 데이터에 보관한다.
    public class PackToughTempHpData
    {
        [JsonProperty]
        public ModifiableValue.Modifier Modifier;
    }

     
    // 본편 TemporaryHitPointsFromAbilityValue 와 같은 패턴: OnActivate 에서 1회 부여 + Data.Modifier 보관, OnDeactivate 에서 제거.
    // 저장(PreSave)은 TurnOff/TurnOn 만 부르므로 피해로 깎인 임시 체력(Modifier.ModValue)은 저장·불러오기 후에도 유지된다.
    // RemoveWhenHitPointsEnd(피해로 바닥나면 버프 제거)는 적용하지 않았다: 버프 수명이 보조 수명(전투 종료 시 해산)과 같고,
    // 바닥난 수정치는 HandleDamage 가 스스로 제거하므로 남는 것은 효과 없는 버프 아이콘뿐이다.
    public class PackToughTempHp : UnitBuffComponentDelegate<PackToughTempHpData>
    {
        protected override void OnActivate()
        {
            var leader = Buff.Context.MaybeCaster;
            if (leader == null)
                throw new InvalidOperationException("PackWarden: temp HP buff on " + Owner + " has no caster (must be added with the leader as caster)");
            var thp = Owner.Stats.TemporaryHitPoints;
            if (thp == null) throw new InvalidOperationException("PackWarden: " + Owner + " has no TemporaryHitPoints stat");
            int value = PackRules.ToughTempHp(leader.Stats.Wisdom.Bonus, PackCall.HunterLevel(leader));
            if (value == 0) return;
            Data.Modifier = thp.AddModifier(value, Runtime, ModifierDescriptor.UntypedStackable);
        }

        protected override void OnDeactivate()
        {
            if (Data.Modifier != null) Data.Modifier.Remove(); // 이미 피해로 소진돼 떨어졌으면 false, 정상
            Data.Modifier = null;
        }
    }
}
