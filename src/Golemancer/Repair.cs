using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using FeetExtension = Kingmaker.Utility.FeetExtension;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 응급 수리(설계 §4.7, owner approval 2026-11-14): 1레벨부터, 표준 행동, 추출물 슬롯 1개 소모(변형 1..6 = 슬롯 레벨), 인접 골렘(합체 중이면 거둔 골렘 Damage 감소),
    // 회복 = 최대 HP × 슬롯 레벨 × 10%(내림), 부서짐 해제(죽었으면 되살려 HP = 회복량). 슬롯 없음 → 못 씀. 판정 = 순수 RepairRules.
    // 엔진 경로(Step 0, Assembly-CSharp 748b3a14): 슬롯 = Spellbook.GetMemorizedSpellSlots(int) @0001~0008(m_MemorizedSpells[L]) · 소모 = SpellSlot.Spend @0002(Available=false, 반대 학파면 연결 칸도) ·
    //   순서 = Spellbook.SpendInternal @010A~0155(뒤에서 앞으로). 되살림 = UnitDescriptor.Resurrect(null, 1f, true)(GolemMaster.OnRestSucceeded 와 같은 호출).
    public static class Repair
    {
        public static BlueprintFeature Feature;
        public static BlueprintAbility Ability;
        static readonly BlueprintAbility[] variants = new BlueprintAbility[GolemRules.Dials.MaxExtractLevel];
        static readonly string[] VariantNameKeys = { "Repair.L1.Name", "Repair.L2.Name", "Repair.L3.Name", "Repair.L4.Name", "Repair.L5.Name", "Repair.L6.Name" };
        static readonly string[] VariantGuids = { Guids.RepairAbilityL1, Guids.RepairAbilityL2, Guids.RepairAbilityL3, Guids.RepairAbilityL4, Guids.RepairAbilityL5, Guids.RepairAbilityL6 };
        // 출처: 설계 §4.7 「응급 수리」 = 1레벨부터(설계 §4.9 「(분리 단계) 응급 수리(1레벨부터)」).
        static int RepairLevel { get { return GolemRules.MinLevel; } }





        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Golemancer: Repair.Build() called twice");
            if (VariantGuids.Length != GolemRules.Dials.MaxExtractLevel || VariantNameKeys.Length != GolemRules.Dials.MaxExtractLevel) throw new InvalidOperationException("Golemancer: " + VariantGuids.Length + " repair variant GUIDs for " + GolemRules.Dials.MaxExtractLevel + " extract levels");
            // 아이콘 원천 = 본편 CureLightWounds(m_Icon 있음 — 시험 `jbp: repair icon source has an icon`). 없으면 이름 붙여 실패.
            var icon = Get<BlueprintAbility>(Guids.Game.RepairIconSource).Icon;
            if (icon == null) throw new InvalidOperationException("Golemancer: repair icon source CureLightWounds (" + Guids.Game.RepairIconSource + ") has no icon (game update or another mod cleared it)");
            RepairFx.Resolve(); // FX(2026-11-15 승인): 본편 IronBodyBuff FxOnStart 대조 — 어긋나면 여기서 던진다(RepairFxTable)
            Ability = Create<BlueprintAbility>("GolemancerEmergencyRepair", Guids.RepairAbility, a => Configure(a, icon, "Repair.Name", "Repair.Description"));
            for (int l = 1; l <= GolemRules.Dials.MaxExtractLevel; l++)
            {
                int level = l;
                var v = Create<BlueprintAbility>("GolemancerEmergencyRepairL" + level, VariantGuids[level - 1], a => Configure(a, icon, VariantNameKeys[level - 1], "Repair.Description"));
                SetField(v, "m_Parent", Ref<BlueprintAbilityReference>(Guids.RepairAbility));
                var act = new ContextActionGolemRepair { SlotLevel = level };
                AddComponents(v, new GolemancerRepairRestriction { SlotLevel = level }, new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { act } } });
                var types = v.ComponentsArray.Select(c => c.GetType()).ToArray();
                if (!types.SequenceEqual(new[] { typeof(GolemancerRepairRestriction), typeof(AbilityEffectRunAction) }) || v.GetComponent<GolemancerRepairRestriction>().SlotLevel != level
                    || v.ActionType != UnitCommand.CommandType.Standard || v.Range != AbilityRange.Personal || !v.CanTargetSelf || v.Type != AbilityType.Extraordinary)
                    throw new InvalidOperationException("Golemancer: repair variant L" + level + " readback failed");
                variants[level - 1] = v;
            }
            var vs = new AbilityVariants();
            SetField(vs, "m_Variants", VariantGuids.Select(g => Ref<BlueprintAbilityReference>(g)).ToArray());
            AddComponents(Ability, vs);
            var back = Ability.GetComponent<AbilityVariants>();
            if (Ability.ComponentsArray.Length != 1 || back == null || !back.Variants.Select(x => x.AssetGuid.ToString()).SequenceEqual(VariantGuids)
                || variants.Any(x => !ReferenceEquals(x.Parent, Ability)))
                throw new InvalidOperationException("Golemancer: repair ability variants readback failed");
            if (new[] { Ability }.Concat(variants).Any(x => x.ResourceAssetIds == null || !x.ResourceAssetIds.SequenceEqual(new[] { RepairFxTable.AssetId })))
                throw new InvalidOperationException("Golemancer: repair ability ResourceAssetIds readback != [" + RepairFxTable.AssetId + "] (RepairFxTable)");

            Feature = Create<BlueprintFeature>("GolemancerEmergencyRepairFeature", Guids.RepairFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("Repair.Name"));
                SetField(f, "m_Description", Strings.L("Repair.Description"));
                SetField(f, "m_DescriptionShort", Strings.L("Repair.Description"));
                SetField(f, "m_Icon", icon);
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.RepairAbility) });
            AddComponents(Feature, grant);
            if (Feature.ComponentsArray.Length != 1 || grant.Facts.Count() != 1 || !ReferenceEquals(grant.Facts.First(), Ability)) throw new InvalidOperationException("Golemancer: repair feature readback failed");
            Archetype.AddAt(RepairLevel, Feature);
        }

        static void Configure(BlueprintAbility a, UnityEngine.Sprite icon, string name, string desc)
        {
            SetField(a, "m_DisplayName", Strings.L(name));
            SetField(a, "m_Description", Strings.L(desc));
            SetField(a, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
            SetField(a, "m_Icon", icon);
            a.Type = AbilityType.Extraordinary; // 합체 중에도 쓸 수 있게: CanBeCastByCaster @0129~0137 의 변신 차단(@017E)은 Spell·SpellLike 만 본다(Step 0)
            a.Range = AbilityRange.Personal;
            a.CanTargetSelf = true;
            a.CanTargetFriends = false;
            a.CanTargetEnemies = false;
            a.CanTargetPoint = false;
            a.ActionType = UnitCommand.CommandType.Standard;
            a.SpellResistance = false;
            a.ResourceAssetIds = RepairFx.ResourceIds(); // FX 미리 읽기(본편 관례) — 실제 FX 는 ContextActionGolemRepair 가 골렘에 띄운다
            // 2026-11-16 결함 수리(툴팁 지속·내성 `<null>`, 근거 IL = BlueprintUtil 「LocalizedString 필드」 주석): 코드로 만든 능력은 두 필드가 null 이었다. 둘 다 빈 키(칸 숨김):
            //   지속 = 즉시 회복 — 본편 즉시 회복 능력은 지속 칸을 비운다(CureLightWounds = 아이콘 원천, LayOnHandsSelf: LocalizedDuration 빈 키 — 시험 tooltip: 이 대조).
            //   내성 = 없음(자기 골렘 수리, 내성 굴림 없음) — 본편 LayOnHandsSelf 의 LocalizedSavingThrow 도 빈 키. (CureLightWounds 의 「Will half …」 는 언데드 피해용이라 해당 없음.)
            a.LocalizedDuration = NoText();
            a.LocalizedSavingThrow = NoText();
        }

        // ---------------- 관측 ----------------
        // 2026-11-16: 슬롯 관측·소모는 공용 ExtractSlots 로 옮겼다(골렘 폭주와 같은 경로 — 소유자 결정 「복제하지 말고 공용화」). 아래 둘은 기존 호출자(점검 S14)용 전달.
        public static List<SpellSlot> SlotsOf(UnitEntityData master, int slotLevel) { return ExtractSlots.SlotsOf(master, slotLevel); }

        public static int CountSpendable(UnitEntityData master, int slotLevel) { return ExtractSlots.CountSpendable(master, slotLevel); }

        static T? Try<T>(Func<T> f) where T : struct { try { return f(); } catch (Exception) { return null; } }

        public static RepairInputs Observe(UnitEntityData master, int slotLevel)
        {
            if (master == null) throw new ArgumentNullException("master");
            var x = new RepairInputs { SlotLevel = slotLevel };
            var part = master.Get<UnitPartGolemMaster>();
            if (part == null) return x; // 관측 결측 → Life null → Unknown(결속 파트 없는 시전자)
            x.Life = Try(() => part.Life.State);
            var pet = master.GetPet(PetType.AnimalCompanion);
            x.GolemBound = Try(() => GolemPresence.IsPresentForAbility(master, part, pet) && part.GolemId != null && pet.UniqueId == part.GolemId && GolemMaster.IsOurGolem(pet));
            x.Fusion = Try(() => GolemMaster.CurrentFusion(master));
            x.Broken = part.Broken;
            if (x.GolemBound == true)
            {
                x.GolemDead = Try(() => pet.State.IsDead);
                x.Damage = Try(() => pet.Descriptor.Damage);
                x.MaxHp = Try(() => pet.MaxHP);
                if (x.Fusion == FusionState.Idle)
                    x.Adjacent = Try(() =>
                    {
                        if (master.View == null || pet.View == null) throw new InvalidOperationException("no view"); // Corpulence 는 View 가 없으면 0.5 를 지어낸다(get_Corpulence @000D) — 그 값을 쓰지 않는다
                        return RepairRules.Adjacent(master.DistanceTo(pet), master.Corpulence, pet.Corpulence, FeetExtension.Feet(GolemRules.Dials.AdjacentFeet).Meters);
                    });
            }
            x.SpendableSlots = Try(() => CountSpendable(master, slotLevel));
            return x;
        }

        static readonly UnknownOnce unknownLogged = new UnknownOnce();

        // 시전 제한(엔진이 매 프레임 묻는다 — 예외 금지): 판정 불가면 막고 1회 로그.
        public static RepairBlock Check(UnitEntityData master, int slotLevel)
        {
            try
            {
                var x = Observe(master, slotLevel);
                var d = RepairRules.Decide(x);
                if (d == RepairBlock.Unknown && unknownLogged.First(UnknownOnce.KeyOf(master == null ? "?" : master.UniqueId, "repair L" + slotLevel, new InvalidOperationException("observation"))))
                    ModContext.LogError("Golemancer: UNKNOWN emergency repair observation for " + (master == null ? "?" : master.CharacterName) + " (L" + slotLevel + ") — blocked");
                return d;
            }
            catch (Exception ex)
            {
                if (unknownLogged.First(UnknownOnce.KeyOf(master == null ? "?" : master.UniqueId, "repair L" + slotLevel, ex))) ModContext.LogError("Golemancer: UNKNOWN emergency repair check threw " + ex.GetType().Name + ": " + ex.Message + " — blocked");
                return RepairBlock.Unknown;
            }
        }

        // ---------------- 실행 ----------------
        // 판정 → 결과 계산 → 슬롯 1칸 소모(되읽기 −1) → (죽었으면 사망 관측 → 되살림) → Damage 쓰기(되읽기) → 부서짐 해제 → (되살렸으면 생명주기 Poll = 재적용·도발).
        public static RepairOutcome Perform(UnitEntityData master, int slotLevel)
        {
            if (master == null) throw new ArgumentNullException("master");
            var part = GolemMaster.PartOf(master);
            var d = RepairRules.Decide(Observe(master, slotLevel));
            if (d != RepairBlock.None) { part.Warn("emergency repair L" + slotLevel + " refused at execution: " + d); return null; } // 명령과 실행 사이에 상태가 바뀐 정당한 거절(제한이 먼저 막는다)
            var golem = GolemMaster.GolemOf(master, part.GolemId);
            bool dead = golem.State.IsDead;
            int maxHp = golem.MaxHP, damage = golem.Descriptor.Damage, hpBefore = golem.HPLeft;
            var outcome = RepairRules.Outcome(maxHp, damage, dead, slotLevel);
            var spent = ExtractSlots.SpendOne(master, slotLevel, "repair"); // 고를 칸·소모·되읽기(−1) = 공용 경로(2026-11-16, 동작 변화 없음)
            int before = spent.Before, after = spent.After;
            string id = golem.UniqueId;
            if (outcome.Resurrect)
            {
                GolemMaster.Feed(master, LifeEventKind.Poll); // 되살리기 전 사망 관측(Ready → Stale, GolemMaster.OnRestSucceeded 와 같은 순서)
                golem.Descriptor.Resurrect(null, 1f, true);
                if (golem.State.IsDead || golem.UniqueId != id) throw new InvalidOperationException("Golemancer: golem " + id + " still dead (or replaced) after repair Resurrect");
                if (golem.MaxHP != maxHp) throw new InvalidOperationException("Golemancer: golem MaxHP " + maxHp + " -> " + golem.MaxHP + " across Resurrect");
            }
            golem.Descriptor.Damage = outcome.NewDamage;
            if (golem.Descriptor.Damage != outcome.NewDamage) throw new InvalidOperationException("Golemancer: golem damage readback " + golem.Descriptor.Damage + " != " + outcome.NewDamage);
            bool wasBroken = part.Broken;
            part.Broken = BreakRules.Next(part.Break, BreakEvent.Repaired) == BreakState.Broken;
            if (outcome.Resurrect) GolemMaster.Feed(master, LifeEventKind.Poll); // 되살아난 골렘 = 안정 경계 → Apply(재질·도발)
            part.Info("emergency repair L" + slotLevel + ": golem " + id + " HP " + (dead ? "dead" : hpBefore.ToString()) + " -> " + golem.HPLeft + "/" + golem.MaxHP + " (" + outcome + "), slots " + before + " -> " + after + ", Broken " + wasBroken + " -> " + part.Broken + ", fusion " + GolemMaster.CurrentFusion(master));
            return outcome;
        }
    }

    public class GolemancerRepairRestriction : BlueprintComponent, IAbilityCasterRestriction
    {
        public int SlotLevel;
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return Repair.Check(caster, SlotLevel) == RepairBlock.None; }
        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "Repair.Unavailable"); }
    }

    public class ContextActionGolemRepair : ContextAction
    {
        public int SlotLevel;
        public override string GetCaption() { return "Golemancer: emergency repair L" + SlotLevel; }
        public override void RunAction()
        {
            var caster = Context.MaybeCaster;
            if (caster == null) throw new InvalidOperationException("Golemancer: emergency repair without a caster");
            RepairOutcome outcome;
            try { outcome = Repair.Perform(caster, SlotLevel); }
            catch (Exception ex) { ModContext.LogError("Golemancer: emergency repair L" + SlotLevel + " on " + caster.CharacterName + " threw: " + ex); throw; } // ActionList.Run 은 예외를 삼킨다 — 모드 로그 먼저
            // FX(2026-11-15 승인): 수리가 실제로 된 경우만(null = 실행 시 거절) 골렘 몸에 1회. Personal 능력이라 AbilitySpawnFx 로는 골렘에 닿지 않는다(RepairFxTable 머리말).
            if (outcome != null) RepairFx.Play(caster);
        }
    }
}
