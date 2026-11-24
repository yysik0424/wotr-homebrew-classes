using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using TargetWrapper = Kingmaker.Utility.TargetWrapper;
using EntityFact = Kingmaker.EntitySystem.EntityFact;
using StatType = Kingmaker.EntitySystem.Stats.StatType;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
     
    // 주술사 18레벨, 신속 행동, 초자연, 하루 1회: 스피릿 웨폰이 예비 칸의 근접 무기마다 1회씩 대상을 공격한다(최고 공격 보너스, 그 무기 자신의 주사위·강화·속성).
    // 판정은 전부 순수 코어: 참여 무기·3상태·레벨 게이트·기록 = SpiritBarrageRules, 「살아 있고 보이며 30피트 안」 = 강신 것(Possession.ObserveSpirit → PossessionRules.Observe).
    // 여기서는 블루프린트를 만들고, 엔진 질의(시전 제한·대상 제한)와 능력 실행을 코어로 넘기고, 게임 객체를 읽는 관측과 공격 발동만 한다.
    //  - 자원 「스피릿 배러지」: 최대 = Dials.SpiritBarrageUsesPerDay(고정값). 휴식 회복 = 엔진 Replenish(강신 자원과 같은 경로).
    //  - 능력: 행동 = Dials.SpiritBarrageAction, 비용 = 자원 1, 대상 = 적 하나, 범위 Unlimited + LineOfSightIgnorance(주술사는 다가가지 않는다 — 간격·시야는 스피릿 웨폰 기준, 정찰 §3).
    //  - 공격 1회 = 예비 무기를 그 공격 동안만 임시 장착 처리(ItemEntity.OnDidEquipped → RuleAttackWithWeapon → OnWillUnequip, 정찰 §2 방법 A).
    //    세트·칸·마법 부여 목록을 바꾸지 않고 장비 사건도 올리지 않는다 → 흡수·강신 상태에 닿지 않는다. 그 불변을 공격 전후에 코드가 다시 읽어 확인한다(어긋나면 던진다).
    // 상태: 없음(정적 필드 = 블루프린트 참조 3개, 횟수는 엔진 자원). 버프를 만들지 않는다.
    public static class SpiritBarrage
    {
        public static BlueprintAbilityResource Resource;
        public static BlueprintAbility Ability;
        public static BlueprintFeature Feature;

        public static void Build()
        {
            if (Archetype.Bp == null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritBarrage.Build() before Archetype.Build() (AddAt needs the archetype)");
            if (Possession.Ability == null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritBarrage.Build() before Possession.Build() (icon source)");
            if (Resource != null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritBarrage.Build() called twice");
            var icon = Possession.Ability.Icon;
            if (icon == null) throw new InvalidOperationException("WeaponSpiritShaman: possession ability has no icon (barrage icon source)");

            Resource = MakeResource(icon);

            var action = new SpiritBarrageAction();
            Ability = Create<BlueprintAbility>("WeaponSpiritShamanSpiritBarrageAbility", Guids.SpiritBarrageAbility, a =>
            {
                Possession.Names(a, "SpiritBarrage.Name", "SpiritBarrage.Description", icon);
                a.Type = AbilityType.Supernatural;
                a.Range = AbilityRange.Unlimited;
                a.ActionType = Possession.CommandOf(SpiritRules.Dials.SpiritBarrageAction);
                a.CanTargetEnemies = true;
                a.CanTargetFriends = false;
                a.CanTargetSelf = false;
                a.CanTargetPoint = false;
                a.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                a.SpellResistance = false;
                // 2026-11-16 결함 수리(툴팁 지속·내성 칸 `<null>`): 즉시 무기 공격(버프 없음·내성 굴림 없음) → 둘 다 빈 키(칸 숨김).
                // 본편 관례: 무기 공격 능력 VitalStrikeAbility(Feats/VitalStrike/VitalStrikeAbility.jbp)의 두 칸 모두 { m_Key "", Shared null } — 시험이 대조.
                a.LocalizedDuration = NoText();
                a.LocalizedSavingThrow = NoText();
            });
            Possession.RequireTooltipFields(Ability, "");
            // 액션 요소의 Owner·name 은 JSON 로드 때만 채워진다(강신 PossessionAction 과 같음) → 직접 단다.
            action.name = "$SpiritBarrageAction$" + Guids.SpiritBarrageAbility + "#0";
            action.Owner = Ability;
            var resLogic = new AbilityResourceLogic { Amount = 1 }; // 발동 1회 = 자원 1
            SetField(resLogic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.SpiritBarrageResource));
            SetField(resLogic, "m_IsSpendResource", true);
            AddComponents(Ability, resLogic, new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { action } } }, new SpiritBarrageGate(), new LineOfSightIgnorance());
            var types = Ability.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!types.SequenceEqual(new[] { typeof(AbilityResourceLogic), typeof(AbilityEffectRunAction), typeof(SpiritBarrageGate), typeof(LineOfSightIgnorance) }) || resLogic.RequiredResource != Resource || !resLogic.IsSpendResource || resLogic.Amount != 1
                || Ability.ActionType != Possession.CommandOf(SpiritRules.Dials.SpiritBarrageAction) || Ability.Range != AbilityRange.Unlimited || !Ability.CanTargetEnemies || Ability.CanTargetSelf || Ability.CanTargetFriends || Ability.CanTargetPoint
                || !ReferenceEquals(action.Owner, Ability) || Ability.CasterRestrictions.Length != 1 || Ability.TargetRestrictions.Length != 1)
                throw new InvalidOperationException("WeaponSpiritShaman: barrage ability readback failed (components [" + string.Join(", ", types.Select(t => t.Name)) + "], resource, action type, range, targets, owner, restrictions)");

            Feature = Create<BlueprintFeature>("WeaponSpiritShamanSpiritBarrageFeature", Guids.SpiritBarrageFeature, x =>
            {
                Possession.Names(x, "SpiritBarrage.Name", "SpiritBarrage.Description", icon);
                x.IsClassFeature = true;
                x.Ranks = 1;
                x.Groups = new FeatureGroup[0];
            });
            var res = new AddAbilityResources { RestoreAmount = true };
            SetField(res, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.SpiritBarrageResource));
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.SpiritBarrageAbility) });
            AddComponents(Feature, res, grant);
            if (res.Resource != Resource || !res.RestoreAmount || Feature.ComponentsArray.Length != 2 || grant.Facts.Count() != 1 || !ReferenceEquals(grant.Facts.First(), Ability))
                throw new InvalidOperationException("WeaponSpiritShaman: barrage feature readback failed (AddAbilityResources + AddFacts[ability])");
            int level = SpiritRules.Dials.SpiritBarrageLevel;
            Archetype.AddAt(level, Feature);
            var levels = Archetype.Bp.AddFeatures.Where(e => e.Features.Contains(Feature)).Select(e => e.Level).ToArray();
            if (levels.Length != 1 || levels[0] != level) throw new InvalidOperationException("WeaponSpiritShaman: barrage feature archetype levels [" + string.Join(",", levels) + "], expected [" + level + "]");
        }

        // 자원: 최대 = BaseValue 고정(레벨·능력치 증가 없음). 강신 자원(Possession.MakeResource)과 같은 방법 — m_MaxAmount 는 비공개 중첩 구조체라 상자에 담아 쓴다.
        static BlueprintAbilityResource MakeResource(UnityEngine.Sprite icon)
        {
            int perDay = SpiritRules.Dials.SpiritBarrageUsesPerDay;
            if (perDay < 1) throw new InvalidOperationException("WeaponSpiritShaman: Dials.SpiritBarrageUsesPerDay is " + perDay + " (must be at least 1)");
            var made = Create<BlueprintAbilityResource>("WeaponSpiritShamanSpiritBarrageResource", Guids.SpiritBarrageResource, r =>
            {
                r.LocalizedName = Strings.L("SpiritBarrage.Name");
                r.LocalizedDescription = Strings.L("SpiritBarrage.Description");
                SetField(r, "m_Icon", icon);
                object amount = GetF(r, "m_MaxAmount");
                SetField(amount, "BaseValue", perDay);
                SetField(amount, "IncreasedByLevel", false);
                SetField(amount, "IncreasedByStat", false);
                SetField(amount, "m_Class", new BlueprintCharacterClassReference[0]);
                SetField(amount, "m_Archetypes", new BlueprintArchetypeReference[0]);
                SetField(amount, "IncreasedByLevelStartPlusDivStep", false);
                SetField(amount, "m_ClassDiv", new BlueprintCharacterClassReference[0]);
                SetField(amount, "m_ArchetypesDiv", new BlueprintArchetypeReference[0]);
                SetField(amount, "StartingLevel", 0);
                SetField(amount, "StartingIncrease", 0);
                SetField(amount, "LevelStep", 0);
                SetField(amount, "PerStepIncrease", 0);
                SetField(amount, "MinClassLevelIncrease", 0);
                SetField(amount, "OtherClassesModifier", 0f);
                SetField(r, "m_MaxAmount", amount);
                SetField(r, "m_UseMax", false);
                SetField(r, "m_Max", 0);
            });
            var back = GetF(made, "m_MaxAmount");
            if ((int)GetF(back, "BaseValue") != perDay || (bool)GetF(back, "IncreasedByLevel") || (bool)GetF(back, "IncreasedByStat") || (bool)GetF(back, "IncreasedByLevelStartPlusDivStep")
                || ((Array)GetF(back, "m_Class")).Length != 0 || ((Array)GetF(back, "m_Archetypes")).Length != 0 || ((Array)GetF(back, "m_ClassDiv")).Length != 0 || ((Array)GetF(back, "m_ArchetypesDiv")).Length != 0
                || (bool)GetF(made, "m_UseMax") || (int)GetF(made, "m_Min") != 0)
                throw new InvalidOperationException("WeaponSpiritShaman: barrage resource m_MaxAmount readback failed (BaseValue = Dials.SpiritBarrageUsesPerDay, no level / stat increase)");
            return made;
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("WeaponSpiritShaman: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }

        // ---------------- 판정 ----------------

        // 강신과 같은 조건(살아 있음·보임·30피트) + 이 능력의 남은 횟수. 관측 = Possession.ObserveSpirit, 판정 = PossessionRules.Observe(moment, …)(복제하지 않는다).
        // moment = Effect(실행 시점 재판정)면 횟수를 보지 않는다 — 강신·융합과 같은 규칙(PossessionRules.CheckMoment 주석).
        static ActivateCheck ObserveSpirit(UnitEntityData shaman, CheckMoment moment)
        {
            if (!shaman.Descriptor.Resources.ContainsResource(Resource)) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " has the barrage ability but no barrage resource");
            int left = shaman.Descriptor.Resources.GetResourceAmount(Resource);
            var o = Possession.ObserveSpirit(shaman);
            return PossessionRules.Observe(moment, o.Present, o.Alive, o.Hidden, o.Feet, left);
        }

        // 시전 제한 질의(SpiritBarrageGate, 엔진이 매 프레임 묻는다) — 던지지 않는다. 판정 불가(관측이 던짐·스피릿 웨폰 없음)는 Unknown = 차단이고, 「근접 무기 0개」와 섞지 않는다(C1).
        public static SpiritBarrageCheck Check(UnitEntityData shaman) { return Judge(shaman, CheckMoment.Cast); }

        // 판정 본체(던지지 않는다). moment = Effect 는 실행 시점 재판정(Run) 전용 — ObserveSpirit 주석.
        static SpiritBarrageCheck Judge(UnitEntityData shaman, CheckMoment moment)
        {
            if ((object)shaman == null || Resource == null)
            {
                Possession.LogUnknown(shaman, "barrage: check (no caster or SpiritBarrage.Build() not run)", new InvalidOperationException("precondition"));
                return SpiritBarrageRules.Decide(null, SpareVerdict.Unknown);
            }
            ActivateCheck spirit;
            try { spirit = ObserveSpirit(shaman, moment); }
            catch (Exception ex) { Possession.LogUnknown(shaman, "barrage: spirit check", ex); spirit = null; }
            SpiritBarragePlan plan;
            try
            {
                var pet = Possession.SpiritOf(shaman);
                // 스피릿 웨폰이 없으면 예비 칸을 읽을 수 없다 → 계획 없음(null) = 판정 불가. 빈 계획(0개)으로 만들지 않는다.
                plan = (object)pet == null ? null : SpiritBarrageRules.Plan(Spares(pet).Select(s => s.Key).ToList());
            }
            catch (Exception ex) { Possession.LogUnknown(shaman, "barrage: spare slots", ex); plan = null; }
            // 판정 자체가 던지면(불변 위반) 그것도 판정 불가다 — 엔진 질의 경로로 예외를 흘리지 않는다.
            try { return SpiritBarrageRules.Decide(spirit, SpiritBarrageRules.Availability(plan)); }
            catch (Exception ex) { Possession.LogUnknown(shaman, "barrage: decide", ex); return new SpiritBarrageCheck(SpiritBarrageBlock.Unknown, ActivateBlock.None); }
        }

        // 대상 제한 질의(SpiritBarrageGate — AbilityData.CanTarget @005C~008B 가 try 없이 부른다) — 던지지 않는다. 판정 불가 = 차단 + 원인 1회 기록.
        public static bool TargetCheck(UnitEntityData shaman, TargetWrapper target)
        {
            bool? ok;
            try { ok = AnyReach(shaman, target); }
            catch (Exception ex) { Possession.LogUnknown(shaman, "barrage: target reach", ex); ok = null; }
            return PolledQuery.Passes(ok);
        }

        // 참여 무기 중 하나 이상이 대상에 닿는가. 스피릿 웨폰·대상 유닛이 없으면 판정할 수 없다 → 던진다(호출자가 판정 불가로 받는다).
        static bool AnyReach(UnitEntityData shaman, TargetWrapper target)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            UnitEntityData unit = ReferenceEquals(target, null) ? null : target.Unit;
            if ((object)unit == null) throw new InvalidOperationException("WeaponSpiritShaman: barrage target is not a unit");
            var pet = Possession.SpiritOf(shaman);
            if ((object)pet == null) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " has no weapon spirit (barrage target check)");
            var spares = Spares(pet);
            var plan = SpiritBarrageRules.Plan(spares.Select(s => s.Key).ToList());
            return SpiritBarrageRules.Aim(plan, plan.Attacks.Select(a => InReach(pet, unit, WeaponOf(spares, a))).ToList()).Attacks.Length > 0;
        }

        // ---------------- 관측 ----------------

        // 스피릿 웨폰의 예비 칸 6개(활성 아닌 세트 3개 × 주·보조). 세트 4개·활성 번호 0~3 이 아니면 던진다(빈 목록으로 읽지 않는다).
        internal static List<KeyValuePair<SpareSlot, ItemEntity>> Spares(UnitEntityData pet)
        {
            if ((object)pet == null) throw new ArgumentNullException("pet");
            var b = pet.Body;
            if (b == null || b.HandsEquipmentSets == null || b.HandsEquipmentSets.Count != AbsorbPlanner.SetCount)
                throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " body/weapon sets not available (barrage)");
            int active = b.CurrentHandEquipmentSetIndex;
            if (active < 0 || active >= AbsorbPlanner.SetCount) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " active weapon set " + active + " out of range (barrage)");
            var r = new List<KeyValuePair<SpareSlot, ItemEntity>>(SpiritBarrageRules.SpareSlotCount);
            for (int i = 0; i < AbsorbPlanner.SetCount; i++)
            {
                if (i == active) continue;
                var set = b.HandsEquipmentSets[i];
                if (set == null || set.PrimaryHand == null || set.SecondaryHand == null) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " weapon set " + i + " has a null hand slot (barrage)");
                r.Add(Describe(i, true, set.PrimaryHand));
                r.Add(Describe(i, false, set.SecondaryHand));
            }
            return r;
        }

        // 칸 하나 → 순수 입력. 종류 판정: 아이템 없음 = Empty · 방패 = Shield · 무기(원거리 = AttackType Ranged·RangedTouch — BlueprintWeaponType.get_IsRanged) · 그 밖의 아이템 = NotWeapon.
        static KeyValuePair<SpareSlot, ItemEntity> Describe(int set, bool primary, HandSlot slot)
        {
            var it = slot.MaybeItem;
            if (it == null) return new KeyValuePair<SpareSlot, ItemEntity>(new SpareSlot { SetIndex = set, IsPrimary = primary, Kind = SpareKind.Empty }, null);
            var w = it as ItemEntityWeapon;
            SpareKind kind = it is ItemEntityShield ? SpareKind.Shield : w == null ? SpareKind.NotWeapon : w.Blueprint.IsRanged ? SpareKind.RangedWeapon : SpareKind.MeleeWeapon;
            bool vol = w != null && (w.Enchantments.Any(e => e.RemoveOnUnequipItem) || (w.Second != null && w.Second.Enchantments.Any(e => e.RemoveOnUnequipItem)));
            return new KeyValuePair<SpareSlot, ItemEntity>(new SpareSlot { SetIndex = set, IsPrimary = primary, Kind = kind, ItemId = it.UniqueId, Name = it.Blueprint.name, Volatile = vol }, it);
        }

        static ItemEntityWeapon WeaponOf(List<KeyValuePair<SpareSlot, ItemEntity>> spares, SpareSlot slot)
        {
            var hit = spares.Where(s => ReferenceEquals(s.Key, slot)).ToArray();
            if (hit.Length != 1) throw new InvalidOperationException("WeaponSpiritShaman: barrage slot " + slot.Label + " is in the spare list " + hit.Length + " time(s)");
            var w = hit[0].Value as ItemEntityWeapon;
            if (w == null) throw new InvalidOperationException("WeaponSpiritShaman: barrage slot " + slot.Label + " was planned as a melee weapon but holds " + (hit[0].Value == null ? "nothing" : hit[0].Value.Blueprint.name));
            return w;
        }

        // 그 무기가 대상에 닿는가 = 엔진 근접 판정식(UnitHelper.IsReach 와 같은 식 — 정찰 §3): 사거리 = 스피릿 웨폰 기준 그 무기의 사거리(유형 사거리 + 간격 보정), 시야 포함.
        static bool InReach(UnitEntityData pet, UnitEntityData target, ItemEntityWeapon weapon)
        {
            if (pet.View == null || target.View == null) throw new InvalidOperationException("WeaponSpiritShaman: barrage reach needs both unit views (spirit " + (pet.View == null ? "missing" : "ok") + ", target " + (target.View == null ? "missing" : "ok") + ")");
            return UnitHelper.IsAttackOfOpportunityReach(pet, target, pet.Descriptor.GetWeaponRange(weapon.Blueprint).Meters);
        }

        static SetSnapshot Layout(UnitEntityData pet)
        {
            var b = pet.Body;
            if (b == null || b.HandsEquipmentSets == null || b.HandsEquipmentSets.Count != AbsorbPlanner.SetCount) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " body/weapon sets not available (barrage layout)");
            var ids = new string[SetChangeDetector.Slots];
            for (int i = 0; i < AbsorbPlanner.SetCount; i++)
            {
                var set = b.HandsEquipmentSets[i];
                if (set == null || set.PrimaryHand == null || set.SecondaryHand == null) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " weapon set " + i + " has a null hand slot (barrage layout)");
                ids[2 * i] = set.PrimaryHand.MaybeItem == null ? null : set.PrimaryHand.MaybeItem.UniqueId;
                ids[2 * i + 1] = set.SecondaryHand.MaybeItem == null ? null : set.SecondaryHand.MaybeItem.UniqueId;
            }
            return new SetSnapshot { Active = b.CurrentHandEquipmentSetIndex, SlotItemIds = ids };
        }

        static List<string> MirrorInstances(UnitEntityData pet)
        {
            return new GameMirrorEffects(pet, null).ListOurInstances().Select(o => o.ItemId + "/" + o.FactId + "/" + o.Guid).ToList();
        }

        static List<string> EnchantIds(ItemEntityWeapon w)
        {
            var r = w.Enchantments.Select(e => e.UniqueId).ToList();
            if (w.Second != null) r.AddRange(w.Second.Enchantments.Select(e => "2:" + e.UniqueId));
            return r;
        }

        static bool AnyActive(ItemEntityWeapon w)
        {
            return w.Enchantments.Any(e => e.IsActive) || (w.Second != null && w.Second.Enchantments.Any(e => e.IsActive));
        }

        // ---------------- 실행 ----------------

        // 한 번 쓴 결과(점검 시나리오 S24 가 읽는다): 겨눈 계획, 공격마다의 결과와 규칙 객체.
        public sealed class Report
        {
            public SpiritBarragePlan Aimed;
            public List<StrikeOutcome> Outcomes = new List<StrikeOutcome>();
            public List<RuleAttackWithWeapon> Rules = new List<RuleAttackWithWeapon>();
        }

        // 능력 효과(SpiritBarrageAction.RunAction).
        public static void Run(MechanicsContext context, TargetWrapper targetWrapper)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (Ability == null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritBarrage.Build() has not run");
            var shaman = context.MaybeCaster;
            if ((object)shaman == null) throw new InvalidOperationException("WeaponSpiritShaman: barrage ability context has no caster");
            UnitEntityData target = ReferenceEquals(targetWrapper, null) ? null : targetWrapper.Unit;
            if ((object)target == null) throw new InvalidOperationException("WeaponSpiritShaman: barrage has no target unit (ContextAction.Target.Unit is null)");
            if (ReferenceEquals(target, shaman)) throw new InvalidOperationException("WeaponSpiritShaman: barrage target is the shaman itself (" + shaman.CharacterName + ")");
            int level = GameMirrorEffects.ShamanLevelOf(shaman);
            if (!SpiritBarrageRules.Unlocked(level)) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " used the barrage at shaman level " + level + " (below the level gate)");
            // 재판정(횟수 제외 — ObserveSpirit 주석): 시전과 효과 사이에 조건이 깨졌으면 공격하지 않고 던진다.
            var check = Judge(shaman, CheckMoment.Effect);
            if (!check.Ok) throw new InvalidOperationException("WeaponSpiritShaman: barrage started on " + shaman.CharacterName + " while blocked (" + check.Block + (check.Block == SpiritBarrageBlock.Spirit ? "/" + check.SpiritReason : "") + ") — the caster restriction should have refused");
            var pet = Possession.SpiritOf(shaman);
            if ((object)pet == null) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " has no weapon spirit at the barrage");
            var rep = Strike(context, shaman, pet, target);
            // 전투 기록 한 줄(현지화). 공개 경로 = ILogMessageUIHandler(귀검사 StandAbsorb 와 같은 경로 — 「전체」 탭). 받는 쪽이 없으면 모드 로그에 남긴다.
            string line = SpiritBarrageRules.Summary(Strings.Resolve(ModContext.StringPrefix + "SpiritBarrage.LogLine"), Strings.Resolve(ModContext.StringPrefix + "SpiritBarrage.None"),
                r => Strings.Resolve(ModContext.StringPrefix + "SpiritBarrageSkip." + r), rep.Aimed, rep.Outcomes);
            bool delivered = EventBus.RaiseEvent<ILogMessageUIHandler>(h => h.HandleLogMessage(line), true);
            if (!delivered) ModContext.Log("WSS barrage (combat log not available): " + line);
        }

        // 공격 묶음. context == null 은 점검 시나리오 전용(규칙을 능력 문맥 없이 직접 발동) — 능력 경로(Run)는 항상 문맥을 넘긴다.
        // 불변(정찰 §5): 공격 전후로 스피릿 웨폰의 칸 8개·활성 세트·흡수 인스턴스 목록이 같다. 어긋나면 던진다.
        public static Report Strike(MechanicsContext context, UnitEntityData shaman, UnitEntityData pet, UnitEntityData target)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            if ((object)pet == null) throw new ArgumentNullException("pet");
            if ((object)target == null) throw new ArgumentNullException("target");
            if (ReferenceEquals(target, pet)) throw new InvalidOperationException("WeaponSpiritShaman: barrage target is the weapon spirit itself");
            var spares = Spares(pet);
            var plan = SpiritBarrageRules.Plan(spares.Select(s => s.Key).ToList());
            var aimed = SpiritBarrageRules.Aim(plan, plan.Attacks.Select(a => InReach(pet, target, WeaponOf(spares, a))).ToList());
            if (aimed.Attacks.Length == 0)
                throw new InvalidOperationException("WeaponSpiritShaman: barrage on " + target.CharacterName + ": no spare melee weapon of " + pet.CharacterName + " takes part (" + plan.Attacks.Length + " melee, none in reach) — the restrictions should have refused");
            var layoutBefore = Layout(pet);
            var mirrorBefore = MirrorInstances(pet);
            var rep = new Report { Aimed = aimed };
            var weapons = new List<ItemEntityWeapon>();
            try
            {
                // 잔류 사전 왕복(Probe): 무기마다 공격 없이 장착 → 해제를 한 번 해 보고, 스피릿 웨폰에 무언가 남긴 무기는 뺀다(ResidualOnEquip, 오류 1줄 — Probe 가 남긴다).
                var clean = aimed.Attacks.Select(a => Probe(pet, WeaponOf(spares, a))).ToList();
                rep.Aimed = SpiritBarrageRules.Screen(aimed, clean);
                weapons = rep.Aimed.Attacks.Select(a => WeaponOf(spares, a)).ToList();
                for (int i = 0; i < weapons.Count; i++)
                {
                    // 앞선 공격으로 대상이 쓰러졌으면 남은 무기는 굴리지 않는다.
                    if (target.Descriptor.State.IsDead) { rep.Outcomes.Add(StrikeOutcome.TargetDown); continue; }
                    var rule = StrikeOne(context, pet, target, weapons[i], i, weapons.Count);
                    rep.Rules.Add(rule);
                    rep.Outcomes.Add(!rule.AttackRoll.IsHit ? StrikeOutcome.Miss : rule.AttackRoll.IsCriticalConfirmed ? StrikeOutcome.Critical : StrikeOutcome.Hit);
                }
            }
            catch (Exception ex)
            {
                // 도중 실패: 배치가 어긋났는지 먼저 남기고(예외를 가리지 않는다) 원래 예외를 그대로 올린다.
                string drift;
                try { drift = Drift(pet, layoutBefore, mirrorBefore); }
                catch (Exception dx) { drift = "the state comparison itself failed (" + dx.GetType().Name + ": " + dx.Message + ")"; }
                ModContext.LogError("WeaponSpiritShaman: barrage strike failed on " + pet.CharacterName + " after " + rep.Outcomes.Count + " of " + weapons.Count + " attack(s): " + ex.GetType().Name + ": " + ex.Message
                    + (drift == null ? " (spirit layout and mirror unchanged)" : " — AND " + drift));
                throw;
            }
            string changed = Drift(pet, layoutBefore, mirrorBefore);
            if (changed != null) throw new InvalidOperationException("WeaponSpiritShaman: barrage changed the weapon spirit's state: " + changed);
            ModContext.Log(SpiritBarrageRules.Line(shaman.CharacterName, pet.CharacterName, target.CharacterName, rep.Aimed, rep.Outcomes));
            return rep;
        }

        // 배치·흡수 불변 대조. 같으면 null, 다르면 무엇이 달라졌는지.
        static string Drift(UnitEntityData pet, SetSnapshot layoutBefore, List<string> mirrorBefore)
        {
            var change = SetChangeDetector.Compare(layoutBefore, Layout(pet));
            var mirrorNow = MirrorInstances(pet);
            bool mirrorSame = mirrorNow.SequenceEqual(mirrorBefore);
            if (!change.ActiveChanged && !change.SourcesChanged && !change.TargetChanged && mirrorSame) return null;
            return "hand layout " + change + ", mirror instances " + mirrorBefore.Count + " -> " + mirrorNow.Count + (mirrorSame ? " (same)" : " (different)");
        }

        // ---------------- 잔류 사전 왕복(2026-11-16) ----------------
        // 「임시 장착 중 소지자 효과(능력치·사실)가 붙었다 떨어져도 남는 것이 없다」를 가정으로 두지 않고 무기마다 확인한다.
        // 방식(b): 실제 공격 없이 장착 → 해제만 한 번 해 보고, 그 전후 스피릿 웨폰 지문을 대조한다(SpiritBarrageRules.Residue).
        //  · 사이에 공격 굴림이 없으므로 공격이 정당하게 일으키는 변화(피해·반격 디버프·적중 효과)가 끼지 않는다 → 허용 목록이 필요 없고 오탐이 적다.
        //  · 같은 프레임 안이라 라운드 경과 만료도 끼지 않는다. 결과는 캐시하지 않는다(스피릿 웨폰 상태에 따라 달라지고, 하루 1회 · 최대 6자루라 매번 해도 싸다 — 정적 상태도 만들지 않는다).
        // 잔류가 있으면: 새로 생긴 사실 인스턴스를 떼서 되돌려 보고, 다시 대조해 남은 것을 오류 1줄에 적고, 그 무기는 참여에서 뺀다(ResidualOnEquip).
        internal static bool Probe(UnitEntityData pet, ItemEntityWeapon weapon)
        {
            if ((object)pet == null) throw new ArgumentNullException("pet");
            if (weapon == null) throw new ArgumentNullException("weapon");
            if (weapon.Wielder != null) throw new InvalidOperationException("WeaponSpiritShaman: barrage probe: spare weapon " + weapon.Blueprint.name + " already has a wielder (" + weapon.Wielder.CharacterName + ")");
            if (AnyActive(weapon)) throw new InvalidOperationException("WeaponSpiritShaman: barrage probe: spare weapon " + weapon.Blueprint.name + " has active enchantments while unwielded");
            var enchBefore = EnchantIds(weapon);
            var before = Fingerprint(pet);
            var factIdsBefore = new HashSet<string>(pet.Facts.List.Select(f => f.UniqueId));
            bool released = false;
            weapon.OnDidEquipped(pet);
            try
            {
                if (weapon.Wielder == null || !ReferenceEquals(weapon.Wielder.Unit, pet)) throw new InvalidOperationException("WeaponSpiritShaman: barrage probe: temporary wield of " + weapon.Blueprint.name + " did not set the spirit as wielder");
            }
            finally
            {
                if (weapon.Wielder != null && ReferenceEquals(weapon.Wielder.Unit, pet)) { weapon.OnWillUnequip(); released = true; }
                else ModContext.LogError("WeaponSpiritShaman: barrage probe: " + weapon.Blueprint.name + " lost its temporary wielder (now " + (weapon.Wielder == null ? "none" : weapon.Wielder.CharacterName) + ") — not released by the barrage");
            }
            if (!released) throw new InvalidOperationException("WeaponSpiritShaman: barrage probe: temporary wield of " + weapon.Blueprint.name + " was ended by something else");
            if (weapon.Wielder != null || AnyActive(weapon)) throw new InvalidOperationException("WeaponSpiritShaman: barrage probe: " + weapon.Blueprint.name + " is still wielded / active after release");
            if (!EnchantIds(weapon).SequenceEqual(enchBefore)) throw new InvalidOperationException("WeaponSpiritShaman: barrage probe changed the enchantments of " + weapon.Blueprint.name);
            var residue = SpiritBarrageRules.Residue(before, Fingerprint(pet));
            if (residue.Count == 0) return true;
            // 되돌리기: 왕복 뒤 새로 생긴 사실 인스턴스를 뗀다(그 사실이 단 수정치도 함께 떨어진다). 사라진 사실은 되살릴 수 없다 — 아래 다시 대조에 남는다.
            var appeared = pet.Facts.List.Where(f => !factIdsBefore.Contains(f.UniqueId)).ToList();
            foreach (var f in appeared) UnitHelper.RemoveFact(pet.Descriptor, f);
            var left = SpiritBarrageRules.Residue(before, Fingerprint(pet));
            ModContext.LogError("WeaponSpiritShaman: barrage: wielding " + weapon.Blueprint.name + " left [" + string.Join("; ", residue.ToArray()) + "] on " + pet.CharacterName
                + " — the weapon is left out (ResidualOnEquip); removed " + appeared.Count + " new fact(s); " + (left.Count == 0 ? "reverted" : "NOT reverted: [" + string.Join("; ", left.ToArray()) + "]"));
            return false;
        }

        // 스피릿 웨폰 상태 지문: 사실 전부·버프·주요 수치(수정치 반영 값)·HP 최대치·활성 세트·손 칸 8개. 수치를 못 읽으면 던진다(빠진 수치를 「같다」로 읽지 않는다).
        internal static SpiritFingerprint Fingerprint(UnitEntityData pet)
        {
            if ((object)pet == null) throw new ArgumentNullException("pet");
            if (pet.Facts == null || pet.Buffs == null || pet.Stats == null) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " has no facts / buffs / stats (barrage fingerprint)");
            var facts = pet.Facts.List.Select(FactKey).ToArray();
            var buffs = pet.Buffs.Enumerable.Select(b => FactKey(b)).ToArray();
            var stats = new List<KeyValuePair<string, int>>();
            // 지시 2026-11-16: 주요 능력치 6 · AC · 명중(기본 + 추가) · 내성 3 · 이동 속도 · HP(수치) + HP 최대치. 하나씩 부른다(배열 초기화는 IL 상수가 남지 않아 시험이 셀 수 없다).
            AddStat(stats, pet, StatType.Strength);
            AddStat(stats, pet, StatType.Dexterity);
            AddStat(stats, pet, StatType.Constitution);
            AddStat(stats, pet, StatType.Intelligence);
            AddStat(stats, pet, StatType.Wisdom);
            AddStat(stats, pet, StatType.Charisma);
            AddStat(stats, pet, StatType.AC);
            AddStat(stats, pet, StatType.BaseAttackBonus);
            AddStat(stats, pet, StatType.AdditionalAttackBonus);
            AddStat(stats, pet, StatType.SaveFortitude);
            AddStat(stats, pet, StatType.SaveReflex);
            AddStat(stats, pet, StatType.SaveWill);
            AddStat(stats, pet, StatType.Speed);
            AddStat(stats, pet, StatType.HitPoints);
            stats.Add(new KeyValuePair<string, int>("MaxHP", pet.MaxHP));
            var layout = Layout(pet);
            return new SpiritFingerprint { Facts = facts, Buffs = buffs, Stats = stats.ToArray(), ActiveSet = layout.Active, SlotItemIds = layout.SlotItemIds };
        }

        static string FactKey(EntityFact f)
        {
            if (f == null || f.Blueprint == null) throw new InvalidOperationException("WeaponSpiritShaman: a spirit fact has no blueprint (barrage fingerprint)");
            return f.Blueprint.AssetGuid.ToString() + "#" + f.GetRank();
        }

        static void AddStat(List<KeyValuePair<string, int>> list, UnitEntityData pet, StatType t)
        {
            var v = pet.Stats.GetStat(t);
            if (v == null) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " has no stat " + t + " (barrage fingerprint)");
            list.Add(new KeyValuePair<string, int>(t.ToString(), v.ModifiedValue));
        }

        // 공격 1회: 예비 무기를 이 공격 동안만 임시 장착 처리한다(정찰 §2 방법 A — 엔진이 세트 전환 때 아이템에 부르는 쌍 ItemSlot.OnActiveChanged @0000~0034).
        //  전: 소지자 없음·마법 부여 꺼짐·「해제 때 지워지는」 마법 부여 없음(있으면 해제가 지운다 — 계획 단계에서 이미 뺐다).
        //  후: 소지자 없음·마법 부여 꺼짐·마법 부여 목록 그대로. 하나라도 어긋나면 던진다.
        static RuleAttackWithWeapon StrikeOne(MechanicsContext context, UnitEntityData pet, UnitEntityData target, ItemEntityWeapon weapon, int number, int count)
        {
            if (weapon.Wielder != null) throw new InvalidOperationException("WeaponSpiritShaman: barrage spare weapon " + weapon.Blueprint.name + " already has a wielder (" + weapon.Wielder.CharacterName + ") — it is not in an inactive set");
            if (AnyActive(weapon)) throw new InvalidOperationException("WeaponSpiritShaman: barrage spare weapon " + weapon.Blueprint.name + " has active enchantments while unwielded");
            var before = EnchantIds(weapon);
            if (weapon.Enchantments.Any(e => e.RemoveOnUnequipItem) || (weapon.Second != null && weapon.Second.Enchantments.Any(e => e.RemoveOnUnequipItem)))
                throw new InvalidOperationException("WeaponSpiritShaman: barrage spare weapon " + weapon.Blueprint.name + " carries a remove-on-unequip enchantment (the planner should have left it out)");
            RuleAttackWithWeapon rule = null;
            bool released = false;
            weapon.OnDidEquipped(pet);
            try
            {
                if (weapon.Wielder == null || !ReferenceEquals(weapon.Wielder.Unit, pet)) throw new InvalidOperationException("WeaponSpiritShaman: barrage temporary wield of " + weapon.Blueprint.name + " did not set the spirit as wielder");
                // 최고 공격 보너스 = 벌점 0(RuleCalculateAttackBonusWithoutTarget.OnTrigger @001C~0038). ExtraAttack = 평소 공격 밖의 추가 공격(본편 ContextActionMeleeAttack.RunAttackRule 과 같은 필드).
                rule = new RuleAttackWithWeapon(pet, target, weapon, 0) { ExtraAttack = true, AttackNumber = number, AttacksCount = count };
                if (context != null) { rule.Reason = context; context.TriggerRule(rule); }
                else Kingmaker.Game.Instance.Rulebook.TriggerEvent(rule);
            }
            finally
            {
                // 해제는 소지자가 여전히 스피릿 웨폰일 때만: 공격 도중 누가 이미 해제했으면 OnWillUnequip 은 Wielder.Body 에서 NRE(ItemEntity.OnWillUnequip @0078).
                if (weapon.Wielder != null && ReferenceEquals(weapon.Wielder.Unit, pet)) { weapon.OnWillUnequip(); released = true; }
                else ModContext.LogError("WeaponSpiritShaman: barrage spare weapon " + weapon.Blueprint.name + " lost its temporary wielder during the attack (now " + (weapon.Wielder == null ? "none" : weapon.Wielder.CharacterName) + ") — not released by the barrage");
            }
            if (!released) throw new InvalidOperationException("WeaponSpiritShaman: barrage temporary wield of " + weapon.Blueprint.name + " was ended by something else during the attack");
            if (weapon.Wielder != null || AnyActive(weapon)) throw new InvalidOperationException("WeaponSpiritShaman: barrage spare weapon " + weapon.Blueprint.name + " is still wielded / active after release");
            var after = EnchantIds(weapon);
            if (!after.SequenceEqual(before)) throw new InvalidOperationException("WeaponSpiritShaman: barrage changed the enchantments of " + weapon.Blueprint.name + " (" + before.Count + " -> " + after.Count + ")");
            if (rule.AttackRoll == null) throw new InvalidOperationException("WeaponSpiritShaman: barrage attack with " + weapon.Blueprint.name + " produced no attack roll");
            return rule;
        }
    }

    // 능력 효과 요소. 인스턴스 필드 없음(블루프린트 요소 1개를 모든 주술사가 공유).
    public class SpiritBarrageAction : ContextAction
    {
        public override string GetCaption() { return "WeaponSpiritShaman: spirit barrage"; }

        public override void RunAction() { SpiritBarrage.Run(Context, Target); }
    }

    // 시전 제한(스피릿 웨폰 조건·예비 근접 무기·횟수) + 대상 제한(예비 무기의 근접 간격). 엔진이 매 프레임·대상마다 묻고 catch 가 없다 → 판정 불가는 차단, 예외는 SpiritBarrage 쪽에서 삼키고 1회 기록.
    // UI 문구에는 유닛이 넘어오지 않는다(IAbilityCasterRestriction.GetAbilityCasterRestrictionUIText()) → 조건 전체를 한 문장으로 보인다(강신 PossessionGate 와 같음).
    public class SpiritBarrageGate : BlueprintComponent, IAbilityCasterRestriction, IAbilityTargetRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return SpiritBarrage.Check(caster).Ok; }

        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "SpiritBarrage.Unavailable"); }

        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target) { return SpiritBarrage.TargetCheck(caster, target); }

        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target) { return Strings.Resolve(ModContext.StringPrefix + "SpiritBarrage.OutOfReach"); }
    }
}
