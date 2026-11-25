using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Rounds = Kingmaker.Utility.Rounds;
using Newtonsoft.Json;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 골렘 폭주(소유자 결정 2026-11-16): 연금술사 5레벨(= SlamRules.OverdriveLevel), 신속 행동, 준비한 추출물 1개 소모(변형 54 = 추출물 레벨 6 × (학파 8 + 학파 없음),
    //   공용 ExtractSlots — 응급 수리와 같은 경로에 학파 조건), 지속 = 추출물 레벨 × Dials.OverdriveRoundsPerExtractLevel 라운드. 전투 여부와 무관.
    //   지속 중: 골렘의 슬램 횟수 = 표의 폭주 값(합체 중 본체 = max(2, 그 값)) + 소모한 추출물 학파의 추가 효과(Dials.OverdriveSchoolEffects — 골렘에게, 합체 중에는 본체에게도).
    //   이미 폭주 중이면 못 쓴다(남은 지속을 덮어쓰지 않는다). 골렘이 부서지거나 사라지면 즉시 끝난다(수리해도 되살아나지 않는다). 판정 = 순수 OverdriveRules · SlamRules.
    // 구조: 폭주 버프(골렘술사, 보임, 엔진 지속) 1개가 상태의 원천. 그 컴포넌트가 켜질 때·형태 버프가 붙고 떨어질 때·매 라운드·꺼질 때 추가 사지 버프(SlamLimbs)와 학파 효과 버프를 골렘과 본체에 맞춘다
    //   — 학파 효과·추가 사지에는 자기 지속이 없다(폭주 버프가 꺼지면 OnDeactivate 가 지운다 = 같은 수명). 소모한 추출물의 학파·레벨은 UnitPartGolemOverdrive 에 기록한다.
     
    public static class Overdrive
    {
        public static BlueprintFeature Feature;
        public static BlueprintAbility Ability;
        public static BlueprintBuff Buff;
        static readonly Dictionary<string, BlueprintAbility> variants = new Dictionary<string, BlueprintAbility>(); // OverdriveRules.VariantId → 변형
        static readonly Dictionary<string, BlueprintBuff> effects = new Dictionary<string, BlueprintBuff>();        // 학파 → 학파 효과 버프(여덟 학파)




        public static BlueprintBuff EffectOf(string school) { RequireBuilt(); OverdriveRules.EffectGuid(school); return effects[school]; }
        public static IEnumerable<BlueprintBuff> AllEffects { get { RequireBuilt(); return effects.Values; } }

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Golemancer: Overdrive.Build() called twice");
            if (Fusion.Feature == null || GolemMaster.Feature == null) throw new InvalidOperationException("Golemancer: Overdrive.Build() before GolemMaster/Fusion");
            // 엔진 학파 열거형의 모든 값이 여덟 학파 또는 「학파 없음」 목록에 속해야 한다(새 값이 생기면 여기서 던진다 — 조용히 어느 학파로 떨어지지 않게).
            foreach (var n in Enum.GetNames(typeof(Kingmaker.Blueprints.Classes.Spells.SpellSchool))) OverdriveRules.SchoolOf(n);
            foreach (var s in OverdriveRules.Schools.Concat(OverdriveRules.NoSchoolNames))
                if (!Enum.IsDefined(typeof(Kingmaker.Blueprints.Classes.Spells.SpellSchool), s)) throw new InvalidOperationException("Golemancer: OverdriveRules school '" + s + "' is not an engine SpellSchool value");
            // 아이콘 원천 = 본편 MutagenStrength(변이약 — 「골렘에게 약을 주입한다」, m_Icon 있음: 시험 icons: jbp). 없으면 이름 붙여 실패. IconTable 이 같은 원본을 가리킨다(Icons.Apply 가 대조).
            var icon = Get<BlueprintAbility>(Guids.Game.OverdriveIconSource).Icon;
            if (icon == null) throw new InvalidOperationException("Golemancer: overdrive icon source MutagenStrength (" + Guids.Game.OverdriveIconSource + ") has no icon (game update or another mod cleared it)");

            Buff = Create<BlueprintBuff>("GolemancerOverdriveBuff", Guids.OverdriveBuff, x =>
            {
                SetField(x, "m_DisplayName", Strings.L("OverdriveBuff.Name"));
                SetField(x, "m_Description", Strings.L("OverdriveBuff.Description"));
                SetField(x, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(x, "m_Icon", icon);
                x.Stacking = StackingType.Ignore; // 이미 있으면 새로 걸지 않는다(남은 지속을 덮어쓰지 않는다 — 판정 AlreadyActive 가 먼저 막고, 이것은 엔진 쪽 두 번째 문)
                x.FxOnStart = new PrefabLink();   // null 금지: 엔진 Buff.OnRemove 가 null 링크에서 예외(빈 링크 = FX 없음)
                x.FxOnRemove = new PrefabLink();
            });
            AddComponents(Buff, new GolemancerOverdriveComponent());
            if (Buff.ComponentsArray.Length != 1 || !(Buff.ComponentsArray[0] is GolemancerOverdriveComponent) || Buff.IsHiddenInUI || Buff.StayOnDeath || Buff.Stacking != StackingType.Ignore || !EmptyFx(Buff))
                throw new InvalidOperationException("Golemancer: overdrive buff readback failed");

            foreach (var s in OverdriveRules.Schools) effects.Add(s, BuildEffect(s, icon));
            if (effects.Count != OverdriveRules.Schools.Count) throw new InvalidOperationException("Golemancer: " + effects.Count + " school effect buffs for " + OverdriveRules.Schools.Count + " schools");

            Ability = Create<BlueprintAbility>("GolemancerOverdrive", Guids.OverdriveAbility, a => Configure(a, icon, "Overdrive.Name", "Overdrive.Description"));
            var reasons = OverdriveRules.Reasons;
            foreach (var kv in OverdriveRules.Variants)
            {
                int level = kv.Key; string school = kv.Value;
                var v = Create<BlueprintAbility>("GolemancerOverdriveL" + level + school, OverdriveRules.VariantGuid(level, school),
                    a => Configure(a, icon, OverdriveRules.VariantNameKey(level, school), OverdriveRules.VariantDescriptionKey(school)));
                SetField(v, "m_Parent", Ref<BlueprintAbilityReference>(Guids.OverdriveAbility));
                var comps = new List<BlueprintComponent>();
                foreach (var r in reasons) comps.Add(new GolemancerOverdriveRestriction { SlotLevel = level, School = school, Reason = r }); // 사유마다 하나 — 엔진은 처음 실패한 제한의 문구를 보여 준다
                comps.Add(new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { new ContextActionGolemOverdrive { SlotLevel = level, School = school } } } });
                AddComponents(v, comps.ToArray());
                var back = v.ComponentsArray;
                var rs = back.OfType<GolemancerOverdriveRestriction>().ToArray();
                var run = back[back.Length - 1] as AbilityEffectRunAction;
                var act = run == null || run.Actions == null || run.Actions.Actions == null || run.Actions.Actions.Length != 1 ? null : run.Actions.Actions[0] as ContextActionGolemOverdrive;
                if (back.Length != reasons.Count + 1 || rs.Length != reasons.Count || !rs.Select(x => x.Reason).SequenceEqual(reasons) || rs.Any(x => x.SlotLevel != level || x.School != school)
                    || act == null || act.SlotLevel != level || act.School != school
                    || v.ActionType != UnitCommand.CommandType.Swift || v.Range != AbilityRange.Personal || !v.CanTargetSelf || v.Type != AbilityType.Extraordinary)
                    throw new InvalidOperationException("Golemancer: overdrive variant L" + level + " " + school + " readback failed");
                variants.Add(OverdriveRules.VariantId(level, school), v);
            }
            var guids = OverdriveRules.AllVariantGuids.ToArray();
            var vs = new AbilityVariants();
            SetField(vs, "m_Variants", guids.Select(g => Ref<BlueprintAbilityReference>(g)).ToArray());
            AddComponents(Ability, vs);
            var vb = Ability.GetComponent<AbilityVariants>();
            if (Ability.ComponentsArray.Length != 1 || vb == null || variants.Count != OverdriveRules.Variants.Length || !vb.Variants.Select(x => x.AssetGuid.ToString()).SequenceEqual(guids) || variants.Values.Any(x => !ReferenceEquals(x.Parent, Ability)))
                throw new InvalidOperationException("Golemancer: overdrive ability variants readback failed");

            Feature = Create<BlueprintFeature>("GolemancerOverdriveFeature", Guids.OverdriveFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("Overdrive.Name"));
                SetField(f, "m_Description", Strings.L("Overdrive.Description"));
                SetField(f, "m_DescriptionShort", Strings.L("Overdrive.Description"));
                SetField(f, "m_Icon", icon);
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.OverdriveAbility) });
            AddComponents(Feature, grant);
            if (Feature.ComponentsArray.Length != 1 || grant.Facts.Count() != 1 || !ReferenceEquals(grant.Facts.First(), Ability)) throw new InvalidOperationException("Golemancer: overdrive feature readback failed");
            Archetype.AddAt(SlamRules.OverdriveLevel, Feature); // 얻는 레벨 = 표에서 폭주 값이 처음 있는 행(따로 적지 않는다)
        }

        static bool EmptyFx(BlueprintBuff b) { return b.FxOnStart != null && b.FxOnRemove != null && string.IsNullOrEmpty(b.FxOnStart.AssetId) && string.IsNullOrEmpty(b.FxOnRemove.AssetId); }

        static void Configure(BlueprintAbility a, UnityEngine.Sprite icon, string name, string description)
        {
            SetField(a, "m_DisplayName", Strings.L(name));
            SetField(a, "m_Description", Strings.L(description));
            SetField(a, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
            SetField(a, "m_Icon", icon);
            a.Type = AbilityType.Extraordinary; // 합체 중에도 쓸 수 있게(Repair.Configure 와 같은 근거: CanBeCastByCaster 의 변신 차단은 Spell·SpellLike 만 본다)
            a.Range = AbilityRange.Personal;
            a.CanTargetSelf = true;
            a.CanTargetFriends = false;
            a.CanTargetEnemies = false;
            a.CanTargetPoint = false;
            a.ActionType = UnitCommand.CommandType.Swift;
            a.SpellResistance = false;
            // 2026-11-16 결함 수리(툴팁 지속·내성 `<null>`): 툴팁은 BlueprintAbility.LocalizedDuration·LocalizedSavingThrow 를 읽는다(근거 IL = BlueprintUtil 「LocalizedString 필드」 주석).
            // 코드로 만든 능력은 두 필드가 null 이라 "<null>" 이 찍혔다. 둘 다 빈 키(칸 숨김):
            //   지속 = 추출물 레벨 × Dials.OverdriveRoundsPerExtractLevel 라운드(변형마다 2·4·…·12) — 이 값들에 맞는 본편 공용 문구 묶음이 없다(blueprints.zip 능력 전수 2026-11-16:
            //     공용 「2 rounds」 4c969acc-ae5f-4efd-bad2-a8bb90ec9713 1건, 「6 rounds」 는 퀘스트 아이템 전용 키 1건, 4·8·10·12 라운드 없음; 「1 round/level」 의 level 은 시전자 레벨이라 뜻이 다르다).
             
            //   내성 = 없음(자기·자기 골렘 강화). 본편 자기 강화 능력(MutagenStrength = 아이콘 원천)의 LocalizedSavingThrow 도 빈 키(시험 tooltip: 이 대조).
            a.LocalizedDuration = NoText();
            a.LocalizedSavingThrow = NoText();
        }

        // ---------------- 학파 효과 버프 8(표 Dials.OverdriveSchoolEffects 의 행마다 본편 컴포넌트 — 선례는 정찰 §6) ----------------
        static ContextValue Simple(int v) { return new ContextValue { ValueType = ContextValueType.Simple, Value = v }; }

        static BlueprintComponent[] EffectComponents(SchoolEffectRow r)
        {
            switch (r.Kind)
            {
                case SchoolEffectKind.Strength: // 선례 BullsStrengthBuff = AddStatBonus{Strength, Enhancement}
                    return new BlueprintComponent[] { new AddStatBonus { Stat = StatType.Strength, Descriptor = ModifierDescriptor.Enhancement, Value = r.A } };
                case SchoolEffectKind.FastHealing: // 선례 FastHealing5 = AddEffectFastHealing{Heal} — 라운드마다 GameHelper.HealDamage(구조물에도 동작: 본편 CR19_GolemAdamantine 이 FastHealing10)
                    return new BlueprintComponent[] { new AddEffectFastHealing { Heal = r.A, Bonus = Simple(0) } };
                case SchoolEffectKind.ArmorClass: // 선례 MageArmorBuff = AddStatBonus{AC, Armor, 4}
                    return new BlueprintComponent[] { new AddStatBonus { Stat = StatType.AC, Descriptor = ModifierDescriptor.Armor, Value = r.A } };
                case SchoolEffectKind.AttackAndCritConfirm: // 명중 = AddStatBonus{AdditionalAttackBonus, Insight} · 확정 = 선례 CriticalFocus CriticalConfirmationBonus{Value}
                    return new BlueprintComponent[]
                    {
                        new AddStatBonus { Stat = StatType.AdditionalAttackBonus, Descriptor = ModifierDescriptor.Insight, Value = r.A },
                        new CriticalConfirmationBonus { Value = Simple(r.B), Bonus = 0, OnlyPositiveValue = false, CheckWeaponRangeType = false },
                    };
                case SchoolEffectKind.Concealment: // 선례 BlurBuff = AddConcealment{Blur, Partial}
                    return new BlueprintComponent[] { new AddConcealment { Descriptor = ConcealmentDescriptor.Blur, Concealment = ConcealmentOf(r.A), CheckWeaponRangeType = false, CheckDistance = false, OnlyForAttacks = false } };
                case SchoolEffectKind.SlamNegativeDice: return new BlueprintComponent[] { Dice(r, DamageEnergyType.NegativeEnergy) };
                case SchoolEffectKind.SlamElectricityDice: return new BlueprintComponent[] { Dice(r, DamageEnergyType.Electricity) };
                case SchoolEffectKind.SlamShaken: // 명중 시 대상에게 본편 Shaken 버프(서술자 Shaken·Fear·MindAffecting — 정신 면역 대상은 엔진이 거부), 내성 없음
                {
                    var apply = new ContextActionApplyBuff
                    {
                        DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = Simple(0), BonusValue = Simple(r.A) },
                        Permanent = false, UseDurationSeconds = false, IsFromSpell = false, ToCaster = false, AsChild = false,
                    };
                    SetField(apply, "m_Buff", Ref<BlueprintBuffReference>(Guids.Game.ShakenBuff));
                    return new BlueprintComponent[] { new AddInitiatorAttackWithWeaponTrigger { OnlyHit = true, Action = new ActionList { Actions = new GameAction[] { apply } } } };
                }
            }
            throw new InvalidOperationException("Golemancer: no components for school effect kind " + r.Kind);
        }

        static Concealment ConcealmentOf(int percent)
        {
            // 엔진 은폐 단계(RuleAttackRoll 빗나감 확률): Partial = 20% · Total = 50%. 그 밖의 값은 표현할 수 없다 → 던진다.
            if (percent == 20) return Concealment.Partial;
            if (percent == 50) return Concealment.Total;
            throw new InvalidOperationException("Golemancer: the engine has no " + percent + "% concealment step");
        }

        // 선례 GowrowSonicShortspearBuff = AdditionalDiceOnAttack{AllAttacks, OnHit, Value 주사위, DamageType Energy}. 폭주 중 운반자(골렘·합체한 본체)의 공격은 슬램뿐이다(변신 손 + 추가 사지).
        static AdditionalDiceOnAttack Dice(SchoolEffectRow r, DamageEnergyType energy)
        {
            var c = new AdditionalDiceOnAttack
            {
                AttackType = AdditionalDiceOnAttack.WeaponOptions.AllAttacks, OnHit = true,
                InitiatorConditions = new ConditionsChecker { Conditions = new Condition[0] }, TargetConditions = new ConditionsChecker { Conditions = new Condition[0] },
                Value = new ContextDiceValue { DiceType = (DiceType)Enum.Parse(typeof(DiceType), "D" + r.B), DiceCountValue = Simple(r.A), BonusValue = Simple(0) },
                DamageType = new DamageTypeDescription { Type = DamageType.Energy, Energy = energy },
            };
            var entries = FindField(typeof(AdditionalDiceOnAttack), "m_DamageEntries");
            if (entries == null) throw new MissingFieldException("Golemancer: AdditionalDiceOnAttack.m_DamageEntries not found");
            SetField(c, "m_DamageEntries", Activator.CreateInstance(entries.FieldType)); // 직렬화가 채우는 빈 목록(null 로 두지 않는다)
            return c;
        }

        // 빌드된 효과 버프를 표의 행과 같은 모양의 문자열로 되읽는다(되읽기·시험·점검이 표와 대조). 모르는 컴포넌트가 섞이면 던진다.
        public static string DescribeEffect(BlueprintBuff b)
        {
            if (b == null) throw new ArgumentNullException("b");
            var parts = new List<string>();
            foreach (var c in b.ComponentsArray)
            {
                var sb = c as AddStatBonus;
                var fh = c as AddEffectFastHealing;
                var cc = c as CriticalConfirmationBonus;
                var co = c as AddConcealment;
                var di = c as AdditionalDiceOnAttack;
                var tr = c as AddInitiatorAttackWithWeaponTrigger;
                if (sb != null) parts.Add("stat " + sb.Stat + " " + sb.Descriptor + " " + sb.Value);
                else if (fh != null) parts.Add("fastheal " + fh.Heal + "+" + fh.Bonus.Value);
                else if (cc != null) parts.Add("critconfirm " + cc.Value.Value + "+" + cc.Bonus);
                else if (co != null) parts.Add("conceal " + co.Descriptor + " " + co.Concealment);
                else if (di != null) parts.Add("dice " + di.Value.DiceCountValue.Value + di.Value.DiceType + "+" + di.Value.BonusValue.Value + " " + di.DamageType.Type + " " + di.DamageType.Energy + " " + di.AttackType + (di.OnHit ? " onhit" : ""));
                else if (tr != null)
                {
                    var ap = tr.Action == null || tr.Action.Actions == null || tr.Action.Actions.Length != 1 ? null : tr.Action.Actions[0] as ContextActionApplyBuff;
                    if (ap == null) throw new InvalidOperationException("Golemancer: " + b.name + " attack trigger does not hold exactly one ContextActionApplyBuff");
                    parts.Add("onhit buff " + ap.Buff.AssetGuid + " " + ap.DurationValue.BonusValue.Value + " " + ap.DurationValue.Rate + (tr.OnlyHit ? " onlyhit" : ""));
                }
                else throw new InvalidOperationException("Golemancer: " + b.name + " has an unknown component " + c.GetType().Name);
            }
            return string.Join("; ", parts.ToArray());
        }

        // 표의 행이 만들어야 하는 DescribeEffect 문자열(단일 원천: 값은 전부 행에서).
        public static string ExpectedEffect(SchoolEffectRow r)
        {
            switch (r.Kind)
            {
                case SchoolEffectKind.Strength: return "stat Strength Enhancement " + r.A;
                case SchoolEffectKind.FastHealing: return "fastheal " + r.A + "+0";
                case SchoolEffectKind.ArmorClass: return "stat AC Armor " + r.A;
                case SchoolEffectKind.AttackAndCritConfirm: return "stat AdditionalAttackBonus Insight " + r.A + "; critconfirm " + r.B + "+0";
                case SchoolEffectKind.Concealment: return "conceal Blur " + ConcealmentOf(r.A);
                case SchoolEffectKind.SlamNegativeDice: return "dice " + r.A + "D" + r.B + "+0 Energy NegativeEnergy AllAttacks onhit";
                case SchoolEffectKind.SlamElectricityDice: return "dice " + r.A + "D" + r.B + "+0 Energy Electricity AllAttacks onhit";
                case SchoolEffectKind.SlamShaken: return "onhit buff " + Guids.Game.ShakenBuff + " " + r.A + " Rounds onlyhit";
            }
            throw new InvalidOperationException("Golemancer: no expected text for school effect kind " + r.Kind);
        }

        static BlueprintBuff BuildEffect(string school, UnityEngine.Sprite icon)
        {
            var row = OverdriveRules.EffectOrNull(school);
            if (row == null) throw new InvalidOperationException("Golemancer: school " + school + " has no effect row");
            if (row.Kind == SchoolEffectKind.SlamShaken && Get<BlueprintBuff>(Guids.Game.ShakenBuff).name != "Shaken") throw new InvalidOperationException("Golemancer: " + Guids.Game.ShakenBuff + " is not the vanilla Shaken buff");
            var b = Create<BlueprintBuff>("GolemancerOverdriveEffect" + school, OverdriveRules.EffectGuid(school), x =>
            {
                SetField(x, "m_DisplayName", Strings.L(OverdriveRules.EffectNameKey(school)));
                SetField(x, "m_Description", Strings.L(OverdriveRules.EffectDescriptionKey(school)));
                SetField(x, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(x, "m_Icon", icon);
                x.Stacking = StackingType.Replace;
                x.FxOnStart = new PrefabLink();  // null 금지(위 폭주 버프와 같은 이유)
                x.FxOnRemove = new PrefabLink();
            });
            AddComponents(b, EffectComponents(row));
            string got = DescribeEffect(b), want = ExpectedEffect(row);
            if (got != want || b.IsHiddenInUI || b.StayOnDeath || !EmptyFx(b)) throw new InvalidOperationException("Golemancer: school effect buff " + b.name + " readback '" + got + "', expected '" + want + "'");
            return b;
        }

        // ---------------- 관측 ----------------
        static void RequireBuilt() { if (Buff == null || Feature == null) throw new InvalidOperationException("Golemancer: Overdrive.Build() has not run"); }

        public static List<Buff> BuffsOn(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (Buff == null) throw new InvalidOperationException("Golemancer: Overdrive.Build() has not run");
            return master.Buffs.Enumerable.Where(b => ReferenceEquals(b.Blueprint, Buff)).ToList();
        }

        // 폭주 중인가 = 폭주 버프 정확히 1개. 2개 이상은 있을 수 없는 상태 → 던진다(조용히 「폭주 중」으로 치지 않는다).
        public static bool IsActive(UnitEntityData master)
        {
            int n = BuffsOn(master).Count;
            if (n > 1) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has " + n + " overdrive buffs");
            return n == 1;
        }

        public static List<Buff> EffectsOn(UnitEntityData unit)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            RequireBuilt();
            return unit.Buffs.Enumerable.Where(b => effects.ContainsValue(b.Blueprint)).ToList();
        }

        public static string SchoolOfEffect(Buff b)
        {
            if (b == null) throw new ArgumentNullException("b");
            var hit = effects.Where(kv => ReferenceEquals(kv.Value, b.Blueprint)).Select(kv => kv.Key).ToArray();
            if (hit.Length != 1) throw new InvalidOperationException("Golemancer: buff " + b.Blueprint.name + " is not one of our school effect buffs");
            return hit[0];
        }

        static T? Try<T>(Func<T> f) where T : struct { try { return f(); } catch (Exception) { return null; } }

        public static OverdriveInputs Observe(UnitEntityData master, int slotLevel, string school)
        {
            if (master == null) throw new ArgumentNullException("master");
            var x = new OverdriveInputs { SlotLevel = slotLevel, School = school };
            x.MasterLevel = Try(() => SlamLimbs.AlchemistLevel(master));
            x.Active = Try(() => IsActive(master));
            var part = master.Get<UnitPartGolemMaster>();
            if (part == null) return x; // 관측 결측 → Life null → Unknown(결속 파트 없는 시전자)
            x.Life = Try(() => part.Life.State);
            var pet = master.GetPet(PetType.AnimalCompanion);
            x.GolemBound = Try(() => GolemPresence.IsPresentForAbility(master, part, pet) && part.GolemId != null && pet.UniqueId == part.GolemId && GolemMaster.IsOurGolem(pet));
            x.Broken = part.Broken;
            if (x.GolemBound == true) x.GolemDead = Try(() => pet.State.IsDead);
            x.Fusion = Try(() => GolemMaster.CurrentFusion(master));
            x.SpendableSlots = Try(() => ExtractSlots.CountSpendable(master, slotLevel, school)); // 학파를 읽을 수 없는 추출물이 섞여 있으면 던진다 → null → Unknown
            return x;
        }

        public static OverdriveEndInputs ObserveEnd(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            var x = new OverdriveEndInputs();
            var part = master.Get<UnitPartGolemMaster>();
            if (part == null) return x; // 관측 결측 → Unknown
            var pet = master.GetPet(PetType.AnimalCompanion);
            x.GolemBound = Try(() => pet != null && part.GolemId != null && pet.UniqueId == part.GolemId && GolemMaster.IsOurGolem(pet));
            x.Broken = part.Broken;
            if (x.GolemBound == true) x.GolemDead = Try(() => pet.State.IsDead);
            return x;
        }

        static readonly UnknownOnce unknownLogged = new UnknownOnce();

        // 같은 프레임·같은 시전자·같은 변형의 판정은 한 번만 계산한다(변형마다 제한 컴포넌트가 사유 수만큼 있다 — 엔진이 차례로 묻는다).
        sealed class CheckCache { public string Unit, School; public int Level, Frame = -1; public OverdriveBlock Result; }
        static readonly CheckCache cache = new CheckCache();

        // 시전 제한(엔진이 매 프레임 묻는다 — 예외 금지): 판정 불가면 막고 1회 로그.
        public static OverdriveBlock Check(UnitEntityData master, int slotLevel, string school)
        {
            try
            {
                int frame = UnityEngine.Time.frameCount;
                if (master != null && frame == cache.Frame && slotLevel == cache.Level && school == cache.School && master.UniqueId == cache.Unit) return cache.Result;
                var d = OverdriveRules.Decide(Observe(master, slotLevel, school));
                if (d == OverdriveBlock.Unknown && unknownLogged.First(UnknownOnce.KeyOf(master == null ? "?" : master.UniqueId, "overdrive L" + slotLevel + " " + school, new InvalidOperationException("observation"))))
                    ModContext.LogError("Golemancer: UNKNOWN golem overdrive observation for " + (master == null ? "?" : master.CharacterName) + " (L" + slotLevel + " " + school + ") — blocked");
                cache.Unit = master.UniqueId; cache.Level = slotLevel; cache.School = school; cache.Frame = frame; cache.Result = d;
                return d;
            }
            catch (Exception ex)
            {
                if (unknownLogged.First(UnknownOnce.KeyOf(master == null ? "?" : master.UniqueId, "overdrive L" + slotLevel + " " + school, ex))) ModContext.LogError("Golemancer: UNKNOWN golem overdrive check threw " + ex.GetType().Name + ": " + ex.Message + " — blocked");
                return OverdriveBlock.Unknown;
            }
        }

        // ---------------- 실행 ----------------
        // 판정 → 지속 계산 → 다음에 소모될 추출물 확인(학파 == 변형의 학파) → 추출물 1개 소모(되읽기 −1) → 기록(학파·레벨) → 폭주 버프(지속) → 되읽기.
        // 추가 사지·학파 효과는 버프 컴포넌트 OnActivate 가 맞춘다(Sync).
        public static bool Perform(UnitEntityData master, int slotLevel, string school)
        {
            if (master == null) throw new ArgumentNullException("master");
            var part = GolemMaster.PartOf(master);
            var d = OverdriveRules.Decide(Observe(master, slotLevel, school));
            if (d != OverdriveBlock.None) { part.Warn("golem overdrive L" + slotLevel + " " + school + " refused at execution: " + d); return false; } // 명령과 실행 사이에 상태가 바뀐 정당한 거절(제한이 먼저 막는다)
            int rounds = OverdriveRules.DurationRounds(slotLevel);
            var next = ExtractSlots.PeekNext(master, slotLevel, "overdrive", school);
            if (OverdriveRules.SchoolOf(next.SchoolName) != school) throw new InvalidOperationException("Golemancer: overdrive L" + slotLevel + " " + school + " would spend " + next + " (another school)");
            var spent = ExtractSlots.SpendOne(master, slotLevel, "overdrive", school);
            if (spent.SchoolName != next.SchoolName || spent.BlueprintName != next.BlueprintName) throw new InvalidOperationException("Golemancer: overdrive spent " + spent + " but peeked " + next);
            var record = master.Ensure<UnitPartGolemOverdrive>();
            record.School = school;
            record.ExtractLevel = slotLevel;
            var buff = master.Descriptor.AddBuff(Buff, master, new Rounds(rounds).Seconds);
            if (buff == null || buff.IsPermanent || !IsActive(master))
                throw new InvalidOperationException("Golemancer: overdrive buff readback failed on " + master.CharacterName + " (" + (buff == null ? "AddBuff returned null" : buff.IsPermanent ? "permanent" : "not on the unit") + ") after spending " + spent);
            var row = OverdriveRules.EffectOrNull(school);
            part.Info("golem overdrive L" + slotLevel + " " + school + ": spent " + spent + ", " + rounds + " rounds, slams " + SlamRules.OverdriveSlams(SlamLimbs.AlchemistLevel(master)) + ", school effect " + (row == null ? "none" : row.ToString()) + ", fusion " + GolemMaster.CurrentFusion(master));
            return true;
        }

        // 유닛의 학파 효과 버프를 기대 학파(null = 없음)로 맞춘다. 죽은 유닛에는 더하지 않는다(false) — 폭주는 골렘이 죽으면 끝나므로 다시 더할 일이 없다.
        public static bool ReconcileEffect(UnitEntityData unit, string wantedSchool)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            var ours = EffectsOn(unit);
            var plan = OverdriveRules.PlanOne(ours.Select(SchoolOfEffect).ToArray(), wantedSchool);
            foreach (var b in ours) if (plan.Remove.Contains(SchoolOfEffect(b))) unit.Buffs.RemoveFact(b);
            bool added = true;
            if (plan.Add != null)
            {
                if (unit.State.IsDead) added = false;
                else
                {
                    var bp = EffectOf(plan.Add);
                    var buff = unit.Descriptor.AddBuff(bp, unit, null);
                    if (buff == null) throw new InvalidOperationException("Golemancer: AddBuff " + bp.name + " on " + unit.CharacterName + " returned null");
                }
            }
            var after = EffectsOn(unit).Select(SchoolOfEffect).ToArray();
            bool ok = wantedSchool == null || !added ? after.Length == 0 : after.Length == 1 && after[0] == wantedSchool;
            if (!ok) throw new InvalidOperationException("Golemancer: school effect readback on " + unit.CharacterName + ": [" + string.Join(",", after) + "], expected " + (added ? wantedSchool ?? "none" : "none (dead)"));
            return added;
        }

        // 추가 사지·학파 효과 맞추기(골렘 + 본체). 폭주 버프 컴포넌트·종료 경로가 부른다 — 사건 처리기 안이라 예외를 밖으로 내지 않고 ERROR 로 남긴다(다음 라운드 틱·생명주기 Apply 가 다시 맞춘다).
        public static void Sync(UnitEntityData master, bool overdrive, string why)
        {
            if (master == null) throw new ArgumentNullException("master");
            var part = master.Get<UnitPartGolemMaster>();
            try
            {
                if (part == null) throw new InvalidOperationException("no UnitPartGolemMaster");
                int level = SlamLimbs.AlchemistLevel(master);
                string school = null;
                if (overdrive)
                {
                    var record = master.Get<UnitPartGolemOverdrive>();
                    if (record == null || record.School == null) throw new InvalidOperationException("overdrive without a record of the spent extract");
                    school = record.School;
                }
                bool hasForm = Fusion.FormsOn(master).Count > 0;
                var pet = master.GetPet(PetType.AnimalCompanion);
                if (pet != null && part.GolemId != null && pet.UniqueId == part.GolemId && GolemMaster.IsOurGolem(pet))
                {
                    string want = SlamLimbs.WantedForGolem(pet, level, overdrive);
                    if (!SlamLimbs.Reconcile(pet, want)) part.Info("overdrive sync (" + why + "): golem is dead — limb buffs removed, " + (want ?? "none") + " is re-applied by the lifecycle after repair");
                    ReconcileEffect(pet, OverdriveRules.WantedEffect(overdrive, school, false, hasForm));
                }
                else part.Warn("overdrive sync (" + why + "): no bound golem in the pet slot — golem limbs / school effect not touched");
                SlamLimbs.Reconcile(master, SlamLimbs.WantedForMaster(master, level, overdrive));
                ReconcileEffect(master, OverdriveRules.WantedEffect(overdrive, school, true, hasForm));
                var activeRecord = master.Get<UnitPartGolemOverdrive>();
                if (activeRecord != null) activeRecord.FormSyncPending = false;
            }
            catch (Exception ex)
            {
                string text = "overdrive sync (" + why + ", overdrive " + overdrive + ") threw " + ex.GetType().Name + ": " + ex.Message;
                if (part != null) part.Error(master, text); else ModContext.LogError("Golemancer: " + master.CharacterName + ": " + text);
            }
        }

        // 종료 판정(골렘 사망·파괴 사건, 매 라운드). Keep 이 아니면 폭주 버프를 지운다 — Unknown 도 끝낸다(근거 없이 남기지 않는다, ERROR 로 보고).
        public static void CheckEnd(UnitEntityData master, string why)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (Buff == null || ModContext.BuildFailed) return; // 정당한 무동작: 빌드 안 됨
            var part = master.Get<UnitPartGolemMaster>();
            try
            {
                if (!IsActive(master)) return; // 정당한 무동작: 폭주 중이 아니다
                var d = OverdriveRules.DecideEnd(ObserveEnd(master));
                if (d == OverdriveEnd.Keep) return;
                string text = "golem overdrive ends (" + why + "): " + d;
                if (d == OverdriveEnd.Unknown) { if (part != null) part.Error(master, text + " — golem state could not be read"); else ModContext.LogError("Golemancer: " + master.CharacterName + ": " + text); }
                else if (part != null) part.Info(text);
                End(master);
            }
            catch (Exception ex)
            {
                string text = "overdrive end check (" + why + ") threw " + ex.GetType().Name + ": " + ex.Message;
                if (part != null) part.Error(master, text); else ModContext.LogError("Golemancer: " + master.CharacterName + ": " + text);
            }
        }

        // 폭주 버프를 지운다(컴포넌트 OnDeactivate 가 추가 사지·학파 효과·기록을 정리한다). 되읽기 = 0개.
        public static void End(UnitEntityData master)
        {
            foreach (var b in BuffsOn(master)) b.Remove();
            if (BuffsOn(master).Count != 0) throw new InvalidOperationException("Golemancer: overdrive buff still on " + master.CharacterName + " after End");
        }

        // 폭주 버프가 꺼질 때(지속 만료·End·사망·해제 어느 경로든): 추가 사지를 평소 값으로, 학파 효과를 없음으로, 기록을 지운다.
        internal static void OnEnded(UnitEntityData master)
        {
            var record = master.Get<UnitPartGolemOverdrive>();
            var part = master.Get<UnitPartGolemMaster>();
            if (part != null) part.Info("golem overdrive over (recorded extract: " + (record == null ? "no record" : "L" + record.ExtractLevel + " " + record.School) + ")");
            Sync(master, false, "overdrive over");
            if (record != null) master.Remove<UnitPartGolemOverdrive>();
        }




    }

    // 폭주를 켤 때 소모한 추출물의 기록(주인 소유, 폭주 버프와 함께 저장). 학파 = OverdriveRules.SchoolChoices 의 값(「학파 없음」 = OverdriveRules.NoSchool).
    public class UnitPartGolemOverdrive : UnitPart
    {
        [JsonProperty] public string School;
        [JsonProperty] public int ExtractLevel;
        internal bool FormSyncPending;

        internal void FinishFormSync()
        {
            if (!FormSyncPending) return;
            FormSyncPending = false;
            if (Overdrive.IsActive(Owner)) Overdrive.Sync(Owner, true, "fusion form changed");
        }
    }

    // 폭주 버프에 붙는다(주인 소유). 블루프린트 컴포넌트는 모든 주인이 공유한다 → 인스턴스 필드 없음.
    public class GolemancerOverdriveComponent : UnitBuffComponentDelegate, ITickEachRound, IUnitBuffHandler, IGlobalSubscriber
    {
        protected override void OnActivate() { Overdrive.Sync(Owner, true, "overdrive on"); }

        protected override void OnDeactivate() { Overdrive.OnEnded(Owner); }

        // 매 라운드: 끝낼 사유가 있으면 끝내고(사건을 놓친 경우의 그물), 아니면 추가 사지·학파 효과를 다시 맞춘다.
        public void OnNewRound()
        {
            Overdrive.CheckEnd(Owner, "round");
            if (Overdrive.BuffsOn(Owner).Count == 1) Overdrive.Sync(Owner, true, "round");
        }

        // 합체 형태 버프가 본체에 붙거나 떨어질 때: 본체 쪽 추가 사지·학파 효과를 맞춘다(합체 = max(2, 폭주 값) + 효과, 풀면 본체에서 빠지고 골렘 쪽에 이어진다).
        public void HandleBuffDidAdded(Buff buff) { OnForm(buff); }
        public void HandleBuffDidRemoved(Buff buff) { OnForm(buff); }

        void OnForm(Buff buff)
        {
            if (buff?.Owner == null || !ReferenceEquals(buff.Owner.Unit, Owner) || !Fusion.IsForm(buff.Blueprint)) return;
            // The engine publishes removal before detaching the form. Reconciling limb or school
            // buffs here mutates EventBus subscriptions during that same notification.
            var record = Owner.Get<UnitPartGolemOverdrive>();
            if (record != null) record.FormSyncPending = true;
        }
    }

    // 사유마다 하나씩 단다: 판정 결과가 이 사유면 막는다(엔진이 이 컴포넌트의 문구를 보여 준다).
    public class GolemancerOverdriveRestriction : BlueprintComponent, IAbilityCasterRestriction
    {
        public int SlotLevel;
        public string School;
        public OverdriveBlock Reason;
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return Overdrive.Check(caster, SlotLevel, School) != Reason; }
        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + OverdriveRules.ReasonKey(Reason)); }
    }

    public class ContextActionGolemOverdrive : ContextAction
    {
        public int SlotLevel;
        public string School;
        public override string GetCaption() { return "Golemancer: golem overdrive L" + SlotLevel + " " + School; }
        public override void RunAction()
        {
            var caster = Context.MaybeCaster;
            if (caster == null) throw new InvalidOperationException("Golemancer: golem overdrive without a caster");
            try { Overdrive.Perform(caster, SlotLevel, School); }
            catch (Exception ex) { ModContext.LogError("Golemancer: golem overdrive L" + SlotLevel + " " + School + " on " + caster.CharacterName + " threw: " + ex); throw; } // ActionList.Run 은 예외를 삼킨다 — 모드 로그 먼저
        }
    }
}
