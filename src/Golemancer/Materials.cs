using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 재질 10종(계획 T4 Interfaces, 설계 §4.5·§12, 정찰 §5~§9·§20). 판정 원천은 순수 코어: MaterialCatalog(표) · StatRecipe(수치) · MaterialPlan(교정 계획) · MaterialRatios(비율) · SharePolicy(얕은 복제).
    //   선택 3(L5·10·15, 각 3) → 재질 특성 10 = [RemoveFeatureOnApply{이전 단계 재질} ×N, GolemMaterialComponent](주인 소유, 활성 시 생명주기 Activated 만 — 적용은 안정 경계 Apply)
    //   재질 버프 13(키 = 재질 × 슬램 단계) = StayOnDeath + Polymorph{프리팹·크기·주 손 슬램·m_Facts = 면역·특기·우리 특성(수치 특성 제외, R2-03)}
    //   펫 수치 특성 13 = 자연 방어·힘·DR(ContextRankConfig Custom = StatRecipe.Progression) + 크기 HP 보정 + 이동 — 소유·추적·회수는 모드(AddStat/OwnedStatFacts/RemoveStat).
    public static class Materials
    {
        public static BlueprintFeatureSelection[] Selections;           // [L5, L10, L15] — Dials.MaterialLevels 순서
        public static BlueprintFeature FireRepair, PrecisionImmunity;
        public static BlueprintAbilityAreaEffect SlowArea;
        public static BlueprintBuff SlowAuraBuff;
        public static Ratio[] ComputedRatios;                            // Build 가 게임 안 유닛 10개로 계산한 비율(점검 S11 ratio-table)
        static readonly Dictionary<string, BlueprintFeature> features = new Dictionary<string, BlueprintFeature>();     // 재질 id → 재질 특성
        static readonly Dictionary<string, BlueprintBuff> buffs = new Dictionary<string, BlueprintBuff>();             // 버프 키 → 재질 버프
        static readonly Dictionary<string, BlueprintFeature> petStats = new Dictionary<string, BlueprintFeature>();    // 버프 키 → 펫 수치 특성
        static readonly Dictionary<string, BlueprintItemWeapon> slams = new Dictionary<string, BlueprintItemWeapon>(); // 버프 키 → 슬램 무기
        static readonly Dictionary<string, BlueprintScriptableObject> clones = new Dictionary<string, BlueprintScriptableObject>(); // 원본 GUID → 복제본

        // 출처: 본편 AnimalCompanionStatFeature(Classes/Pets/AnimalCompanionArchetypes/Base, jbp 2026-11-12) = AddContextStatBonus{Strength, Racial}·{AC, NaturalArmor} — 펫 성장 선례의 서술자.
        const ModifierDescriptor StrengthDescriptor = ModifierDescriptor.Racial;
        const ModifierDescriptor NaturalArmorDescriptor = ModifierDescriptor.NaturalArmor;
        // 작성자 기본값(C7 「틀리면 시끄럽다」): 크기 HP 보정·미스랄 이동 = 중첩형 무형식 — 다른 보너스와 겹쳐도 깎이지 않게. 틀리면 S10 stone-hp-58·S11 mithral-speed-50 이 FAIL.
        const ModifierDescriptor SizeHpDescriptor = ModifierDescriptor.UntypedStackable;
        const ModifierDescriptor SpeedDescriptor = ModifierDescriptor.UntypedStackable;

        public enum StatSet { Pet, Fusion }




        public static BlueprintBuff BuffOf(string buffKey) { RequireBuilt(); BlueprintBuff b; if (!buffs.TryGetValue(buffKey, out b)) throw new KeyNotFoundException("Golemancer: no material buff for key " + buffKey); return b; }



        public static BlueprintItemWeapon SlamOf(string buffKey) { RequireBuilt(); BlueprintItemWeapon w; if (!slams.TryGetValue(buffKey, out w)) throw new KeyNotFoundException("Golemancer: no slam for key " + buffKey); return w; }



        public static IEnumerable<BlueprintBuff> AllBuffs { get { RequireBuilt(); return buffs.Values; } }

        static void RequireBuilt() { if (Selections == null) throw new InvalidOperationException("Golemancer: Materials.Build() has not run"); }

        public static void Build()
        {
            if (Selections != null) throw new InvalidOperationException("Golemancer: Materials.Build() called twice");
            if (GolemMaster.Feature == null || GolemMaster.WoodMaterialFeature == null || PetClass.Class == null || GolemUnit.Unit == null)
                throw new InvalidOperationException("Golemancer: Materials.Build() before GolemMaster/PetClass/GolemUnit");

            CheckRatios();
            BuildOwnFeatures();
            BuildClones();
            BuildSlowAura();
            // 재질 변신 FX(2026-11-15 승인): 합체와 같은 본편 원소 신체 외부 전환 에셋(FusionFxTable — 없거나 비면 Fusion.ElementalTransition 이 던진다).
            var transition = Fusion.ElementalTransition();
            foreach (var key in MaterialCatalog.Keys)
            {
                var m = MaterialCatalog.MaterialOfKey(key);
                int tier = int.Parse(key.Substring(key.IndexOf("@T", StringComparison.Ordinal) + 2));
                slams.Add(key, BuildSlam(key, StatRecipe.SlamOf(m, tier), m));
                petStats.Add(key, BuildPetStats(key, m));
                buffs.Add(key, BuildBuff(key, m, transition));
            }
            foreach (var m in MaterialCatalog.Materials) features.Add(m.Id, BuildFeature(m));
            Selections = GolemRules.Dials.MaterialLevels.Select((lvl, k) => BuildSelection(lvl, k + 1)).ToArray();

            // 되읽기: 13 키 · 10 재질 · 선택 3 × 3, 본편 값 무변경(격리 스냅샷).
            if (buffs.Count != MaterialCatalog.Keys.Count || petStats.Count != MaterialCatalog.Keys.Count || slams.Count != MaterialCatalog.Keys.Count || features.Count != MaterialCatalog.Materials.Count)
                throw new InvalidOperationException("Golemancer: Materials.Build readback counts " + buffs.Count + "/" + petStats.Count + "/" + slams.Count + "/" + features.Count);
            SnapshotGuard.ThrowIfChanged(GolemIsolation.SnapshotDiffNow(), "material build");
        }

        // ---------------- 비율(C4 ②): 본편 유닛 10개 → MaterialRatios.Compute(ToEven) == ExpectedRatios, 다르면 빌드 실패 ----------------
        static void CheckRatios()
        {
            var rows = MaterialCatalog.Materials.Select(m => UnitRowOf(m)).ToArray();
            ComputedRatios = MaterialRatios.Compute(rows, MidpointRounding.ToEven);
            var bad = new List<string>();
            foreach (var r in ComputedRatios)
            {
                var want = MaterialCatalog.RatioOf(MaterialCatalog.Get(r.Material));
                if (r.NaturalArmor != want.NaturalArmor || r.Dr != want.Dr || r.Slam != want.Slam || r.Strength != want.Strength)
                    bad.Add(r.Material + " computed " + r.NaturalArmor + "/" + r.Dr + "/" + r.Slam + "/" + r.Strength + " expected " + want.NaturalArmor + "/" + want.Dr + "/" + want.Slam + "/" + want.Strength);
            }
            if (bad.Count > 0 || ComputedRatios.Length != MaterialCatalog.ExpectedRatios.Count) throw new InvalidOperationException("Golemancer: material ratios from the base-game units differ from MaterialCatalog.ExpectedRatios: " + string.Join(" | ", bad.ToArray()));
        }

        // 설계 §4.5 「본편 유닛 수치(직독)」 규칙: 자연 방어 = m_AddFacts 의 AddStatBonus{AC, NaturalArmor} 합(정확히 1개), DR = AddDamageResistancePhysical(Simple) 최대값(황금 15·5 → 15, 정찰 §17-11),
        //   힘 = 유닛 Strength, 공격 1회분 = 주 손 무기(있으면, 황동 팔치온) 아니면 추가 사지 첫 무기의 BaseDamage 평균 + 무기 마법 부여 WeaponEnergyDamageDice 평균(황동 화염 2d6).
        //   규칙 밖 모양(사실 없음·Simple 아님·사지 무기 서로 다름·모르는 마법 부여 컴포넌트)은 던진다.
        static UnitRow UnitRowOf(Material m)
        {
            var u = Get<BlueprintUnit>(m.UnitGuid);
            var facts = ((BlueprintUnitFactReference[])FindField(typeof(BlueprintUnit), "m_AddFacts").GetValue(u)).Select(r => r.Get()).ToArray();
            if (facts.Any(f => f == null)) throw new InvalidOperationException("Golemancer: " + u.name + " has an unresolved m_AddFacts entry");
            var na = facts.SelectMany(f => f.ComponentsArray.OfType<AddStatBonus>()).Where(c => c.Stat == StatType.AC && c.Descriptor == ModifierDescriptor.NaturalArmor).ToArray();
            if (na.Length != 1) throw new InvalidOperationException("Golemancer: " + u.name + " has " + na.Length + " natural-armor AddStatBonus facts, expected 1");
            var dr = facts.SelectMany(f => f.ComponentsArray.OfType<AddDamageResistancePhysical>()).ToArray();
            if (dr.Length == 0 || dr.Any(d => d.Value == null || d.Value.ValueType != ContextValueType.Simple)) throw new InvalidOperationException("Golemancer: " + u.name + " DR facts missing or not Simple values (" + dr.Length + ")");
            var body = u.Body;
            if (body == null) throw new InvalidOperationException("Golemancer: " + u.name + " has no body");
            BlueprintItemWeapon w = body.PrimaryHand as BlueprintItemWeapon;
            if (body.PrimaryHand != null && w == null) throw new InvalidOperationException("Golemancer: " + u.name + " primary hand is not a weapon");
            if (w == null)
            {
                var limbs = body.AdditionalLimbs.ToArray();
                if (limbs.Length == 0 || limbs.Any(l => l == null) || limbs.Select(l => l.BaseDamage.ToString()).Distinct().Count() != 1) throw new InvalidOperationException("Golemancer: " + u.name + " has no weapon or its limbs differ");
                w = limbs[0];
            }
            decimal avg = Average(w.BaseDamage);
            foreach (var e in w.Enchantments)
                foreach (var c in e.ComponentsArray)
                {
                    var ed = c as WeaponEnergyDamageDice;
                    if (ed == null) throw new InvalidOperationException("Golemancer: " + u.name + " weapon enchantment " + e.name + " has an unknown component " + c.GetType().Name + " (attack-average rule covers WeaponEnergyDamageDice only)");
                    avg += Average(ed.EnergyDamageDice);
                }
            return new UnitRow(m.Id, m.Tier, na[0].Value, dr.Max(d => d.Value.Value), avg, u.Strength);
        }

        // DiceType 값 = 면 수(dnlib: Zero 0 · One 1 · D4 4 · D6 6 · D8 8 · D10 10 …, 정찰 §20). 평균 = rolls × (면 + 1) / 2.
        static decimal Average(DiceFormula d)
        {
            if (d.Rolls < 1 || (int)d.Dice < 2) throw new InvalidOperationException("Golemancer: dice " + d + " has no average for the ratio rule");
            return d.Rolls * ((int)d.Dice + 1) / 2m;
        }

        static DiceType DieOf(int sides)
        {
            if (!Enum.IsDefined(typeof(DiceType), sides) || sides < 2) throw new ArgumentOutOfRangeException("sides", "Golemancer: no DiceType with " + sides + " sides");
            return (DiceType)sides;
        }

        // ---------------- 우리 특성 2 ----------------
        static void BuildOwnFeatures()
        {
            FireRepair = Create<BlueprintFeature>("GolemancerFireRepairFeature", Guids.FireRepairFeature, f => Named(f, "FireRepair.Name", "FireRepair.Description", "FireRepair.Description"));
            // 소유자 결정 2026-11-17: 철 = 화염 면역만(회복 없음). 식별자 FireRepair·GUID 는 저장 호환을 위해 그대로 둔다(이름만 남은 옛 이름 — 화면 문구는 「화염 면역」).
            // 그 전(정찰 §8): AddEnergyDamageImmunity{Fire, HealOnDamage, DamageDiv3}. 회복은 실제로 0 이었다 — 엔진 AddEnergyDamageImmunity.OnEventDidTrigger(RuleDealDamage)
            // @0074~007C 가 DamageValue.FinalValue(면역으로 이미 0)를 더하고 @009E GetHealValue → @00A6 0 이하면 회복 없음(2026-11-17 동굴 점검 S10 iron-fire-heals FAIL).
            var immunity = new AddEnergyDamageImmunity { EnergyType = DamageEnergyType.Fire, HealOnDamage = false };
            AddComponents(FireRepair, immunity);
            PrecisionImmunity = Create<BlueprintFeature>("GolemancerPrecisionImmunityFeature", Guids.PrecisionImmunityFeature, f => Named(f, "PrecisionImmunity.Name", "PrecisionImmunity.Description", "PrecisionImmunity.Description"));
            AddComponents(PrecisionImmunity, new AddImmunityToPrecisionDamage()); // 정찰 §9
            var fireBack = FireRepair.GetComponents<AddEnergyDamageImmunity>().ToArray();
            if (fireBack.Length != 1 || fireBack[0].EnergyType != DamageEnergyType.Fire || fireBack[0].HealOnDamage
                || PrecisionImmunity.GetComponent<AddImmunityToPrecisionDamage>() == null)
                throw new InvalidOperationException("Golemancer: own feature readback failed");
        }

        // 2026-11-17: 짧은 설명 키를 따로 받는다 — 재질 특성은 m_Description = 여러 줄 상세(선택 화면 상세 창이 읽는다), m_DescriptionShort = 한 줄 요약. 우리 특성 2개는 두 칸에 같은 키.
        static void Named(BlueprintFeature f, string nameKey, string descKey, string shortKey)
        {
            SetField(f, "m_DisplayName", Strings.L(nameKey));
            SetField(f, "m_Description", Strings.L(descKey));
            SetField(f, "m_DescriptionShort", Strings.L(shortKey));
            f.IsClassFeature = true;
            f.Ranks = 1;
        }

        // ---------------- 특기 능력 DC 재지정 복제 4 ----------------
        static void BuildClones()
        {
            var specials = MaterialCatalog.Materials.SelectMany(m => m.SpecialAbilities).Where(a => a.Kind == AbilityKind.CloneRepointDc).GroupBy(a => a.Guid).ToArray();
            foreach (var g in specials)
            {
                var kinds = g.Select(a => string.Join(",", a.SharedComponentKinds)).Distinct().ToArray();
                if (kinds.Length != 1) throw new InvalidOperationException("Golemancer: catalog lists " + g.Key + " with different shared kinds");
                var a0 = g.First();
                var cloneGuid = MaterialCatalog.FactGuidOf(a0);
                var clone = AbilityClone.Repoint(a0.Guid, cloneGuid, "Golemancer" + Get<BlueprintScriptableObject>(a0.Guid).name);
                AbilityClone.VerifyShare(clone, Get<BlueprintScriptableObject>(a0.Guid), a0.SharedComponentKinds);
                clones.Add(a0.Guid, clone);
            }
            if (clones.Count != 4) throw new InvalidOperationException("Golemancer: " + clones.Count + " ability clones, expected 4 (splintering, autumn aura, summer aura, grab)");
        }

        // ---------------- 돌 감속 오라(NewAura, 정찰 §7): 영역 + 영역 버프 ----------------
        static void BuildSlowAura()
        {
            SlowArea = Create<BlueprintAbilityAreaEffect>("GolemancerStoneSlowArea", Guids.StoneSlowArea, a =>
            {
                a.Size = new Feet(GolemRules.Dials.SlowAuraRadiusFeet);
                // AreaEffectView.SpawnFxs calls Fx.Load even for a visual-free aura.
                a.Fx = new PrefabLink();
                a.Shape = AreaEffectShape.Cylinder;
                a.AggroEnemies = false; // 정찰 §7: 펫 오라는 어그로 플래그 끔(도발은 명중 보정으로)
                a.AffectEnemies = true;
                SetField(a, "m_TargetType", Enum.Parse(FindField(typeof(BlueprintAbilityAreaEffect), "m_TargetType").FieldType, "Enemy"));
            });
            // 기간 = 본편 StoneGolemSlow 실패 효과의 값 그대로(정찰 §20 jbp: 7 라운드) — 원본에서 읽어 새 객체로 옮긴다(C4 한 원천, 상수 복제 없음).
            var dv = VanillaSlowDuration();
            var apply = new ContextActionApplyBuff
            {
                DurationValue = new ContextDurationValue
                {
                    Rate = dv.Rate, DiceType = dv.DiceType,
                    DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = dv.DiceCountValue.Value },
                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = dv.BonusValue.Value },
                },
                AsChild = true,
            };
            SetField(apply, "m_Buff", Ref<BlueprintBuffReference>(Guids.Game.SlowBuff));
            var save = new ContextActionSavingThrow
            {
                Type = SavingThrowType.Will,
                CustomDC = new ContextValue(),
                Actions = new ActionList { Actions = new GameAction[] { new ContextActionConditionalSaved { Succeed = new ActionList(), Failed = new ActionList { Actions = new GameAction[] { apply } } } } },
            };
            var enter = new Conditional
            {
                ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { new ContextConditionIsEnemy() } },
                IfTrue = new ActionList { Actions = new GameAction[] { save } },
                IfFalse = new ActionList(),
            };
            AddComponents(SlowArea, new AbilityAreaEffectRunAction { UnitEnter = new ActionList { Actions = new GameAction[] { enter } }, UnitExit = new ActionList(), UnitMove = new ActionList(), Round = new ActionList() });

            SlowAuraBuff = Create<BlueprintBuff>("GolemancerStoneSlowAuraBuff", Guids.StoneSlowAuraBuff, b =>
            {
                SetField(b, "m_DisplayName", Strings.L("StoneSlowAura.Name"));
                SetField(b, "m_Description", Strings.L("StoneSlowAura.Description"));
                SetField(b, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(b, "m_Flags", FlagsOf("StayOnDeath"));
                b.Stacking = StackingType.Replace;
                EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — Buff.OnRemove @00B0 NRE)
            });
            var add = new AddAreaEffect();
            SetField(add, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(Guids.StoneSlowArea));
            // DC = 본편 StoneGolemSlow 와 같은 ContextCalculateAbilityParams{Constitution}(직업 무관, 정찰 §6·§7).
            AddComponents(SlowAuraBuff, add, new ContextCalculateAbilityParams { StatType = StatType.Constitution, CasterLevel = new ContextValue(), SpellLevel = new ContextValue() });
            if (!ReferenceEquals(add.AreaEffect, SlowArea) || SlowArea.Fx == null || !string.IsNullOrEmpty(SlowArea.Fx.AssetId) || SlowArea.Size.Value != GolemRules.Dials.SlowAuraRadiusFeet || ((ContextActionApplyBuff)((ContextActionConditionalSaved)save.Actions.Actions[0]).Failed.Actions[0]).Buff.AssetGuid.ToString() != Guids.Game.SlowBuff)
                throw new InvalidOperationException("Golemancer: stone slow aura readback failed");
        }

        // 본편 StoneGolemSlow 의 AbilityEffectRunAction 아래 SlowBuff 를 거는 ContextActionApplyBuff 정확히 1개의 기간(Simple 값만 — 다른 모양이면 던진다).
        static ContextDurationValue VanillaSlowDuration()
        {
            var slow = Get<BlueprintAbility>(Guids.Game.StoneGolemSlow);
            var run = slow.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().ToArray();
            if (run.Length != 1) throw new InvalidOperationException("Golemancer: StoneGolemSlow has " + run.Length + " AbilityEffectRunAction, expected 1");
            var applies = AbilityClone.ElementsUnder(run[0]).OfType<ContextActionApplyBuff>().Where(a => a.Buff != null && a.Buff.AssetGuid.ToString() == Guids.Game.SlowBuff).ToArray();
            if (applies.Length != 1) throw new InvalidOperationException("Golemancer: StoneGolemSlow applies SlowBuff " + applies.Length + " time(s), expected 1");
            var dv = applies[0].DurationValue;
            if (dv == null || applies[0].Permanent || dv.DiceCountValue == null || dv.BonusValue == null || dv.DiceCountValue.ValueType != ContextValueType.Simple || dv.BonusValue.ValueType != ContextValueType.Simple || dv.BonusValue.Value <= 0)
                throw new InvalidOperationException("Golemancer: StoneGolemSlow SlowBuff duration is not a simple positive value");
            return dv;
        }

        static object FlagsOf(string name) { return Enum.Parse(FindField(typeof(BlueprintBuff), "m_Flags").FieldType, name); }

        // ---------------- 슬램 13: CR6_GolemWood 사지 무기 Slam2d6 값 복사 + 주사위 덮어쓰기(정찰 §20: Polymorph 아래 플레이어 진영은 크기 배율 없음) ----------------
        static BlueprintItemWeapon BuildSlam(string key, Dice dice, Material m)
        {
            var src = Get<BlueprintItemWeapon>(Guids.Game.Slam2d6);
            if ((src.ComponentsArray ?? new BlueprintComponent[0]).Length != 0 || src.VisualParameters == null || src.Enchantments.Any())
                throw new InvalidOperationException("Golemancer: Slam2d6 shape changed (components / visual / enchantments)");
            var formula = new DiceFormula(dice.Rolls, DieOf(dice.Die));
            var w = Create<BlueprintItemWeapon>("GolemancerSlam_" + key.Replace("@", "_"), MaterialCatalog.SlamGuid(key), x =>
            {
                CloneKit.CopyInto(src, x);
                x.ComponentsArray = new BlueprintComponent[0];
                SetField(x, "m_VisualParameters", CloneKit.Copy(src.VisualParameters));
                SetField(x, "m_DamageDice", formula);
                SetField(x, "m_OverrideDamageDice", true);
                // 소유자 결정 2026-11-16(추가 슬램): 변신(m_KeepSlots = false) 중 추가 사지로 달리려면 무기가 KeepInPolymorph 여야 한다(UnitBody.AddLimbToIndex @0058~0066 · GeneratePolymorphAdditionalLimbs @0040).
                //   이 필드를 읽는 곳은 UnitBody.KeepInPolymorph 하나뿐(dnlib 2026-11-16) — 주 손·보조 손 슬램의 동작은 바뀌지 않는다. SlamLimbs.Build 가 이 값을 요구한다.
                x.KeepInPolymorph = true;
            });
            if (!w.KeepInPolymorph || src.KeepInPolymorph) throw new InvalidOperationException("Golemancer: slam " + w.name + " KeepInPolymorph readback " + w.KeepInPolymorph + " (source Slam2d6 " + src.KeepInPolymorph + " — must stay untouched)");
            if (!w.IsDamageDiceOverridden || w.BaseDamage.Rolls != dice.Rolls || (int)w.BaseDamage.Dice != dice.Die || ReferenceEquals(w.VisualParameters, src.VisualParameters) || !ReferenceEquals(w.Type, src.Type))
                throw new InvalidOperationException("Golemancer: slam " + w.name + " readback " + w.BaseDamage + " (expected " + dice + ")");
            return w;
        }

        // ---------------- 펫 수치 특성 13 ----------------
        static BlueprintFeature BuildPetStats(string key, Material m)
        {
            var f = Create<BlueprintFeature>("GolemancerPetStats_" + key.Replace("@", "_"), MaterialCatalog.PetStatFeature(key), x =>
            {
                x.HideInUI = true;
                x.HideInCharacterSheetAndLevelUp = true;
                x.Ranks = 1;
                Taunt.NoNames(x); // 2026-11-16: 숨은 수치 특성 — 문구 없음을 명시(null 금지)
            });
            var comps = new List<BlueprintComponent>
            {
                new AddContextStatBonus { Stat = StatType.AC, Descriptor = NaturalArmorDescriptor, Multiplier = 1, Value = RankValue(AbilityRankType.Default) },
                RankConfig(AbilityRankType.Default, m, StatAxis.NaturalArmor),
                new AddContextStatBonus { Stat = StatType.Strength, Descriptor = StrengthDescriptor, Multiplier = 1, Value = RankValue(AbilityRankType.StatBonus) },
                RankConfig(AbilityRankType.StatBonus, m, StatAxis.StrengthDelta),
                new AddDamageResistancePhysical { Value = RankValue(AbilityRankType.DamageDice), BypassedByMaterial = m.DrBypassAdamantine, Material = PhysicalDamageMaterial.Adamantite, Pool = new ContextValue() },
                RankConfig(AbilityRankType.DamageDice, m, StatAxis.Dr),
            };
            int hp = GolemRules.SizeHpDelta(m.Size);
            if (hp != 0) comps.Add(new AddStatBonus { Stat = StatType.HitPoints, Descriptor = SizeHpDescriptor, Value = hp });
            if (m.SpeedBonus != 0) comps.Add(new AddStatBonus { Stat = StatType.Speed, Descriptor = SpeedDescriptor, Value = m.SpeedBonus });
            AddComponents(f, comps.ToArray());
            // 되읽기: 진행표 = StatRecipe.Progression(m, 축) 항목 그대로(Custom).
            foreach (var rc in f.ComponentsArray.OfType<ContextRankConfig>())
            {
                var axis = AxisOf((AbilityRankType)FindField(typeof(ContextRankConfig), "m_Type").GetValue(rc));
                var items = ProgressionOf(rc);
                var want = StatRecipe.Progression(m, axis);
                if (items.Length != want.Length || items.Where((it, i) => it.Key != want[i].Level || it.Value != want[i].Value).Any()) throw new InvalidOperationException("Golemancer: " + f.name + " " + axis + " progression readback failed");
            }
            return f;
        }

        // ContextRankConfig/CustomProgressionItem 은 비공개 중첩 형식(dnlib: NestedPrivate, 필드 BaseValue·ProgressionValue public) → 필드 형식으로 만들고 읽는다.
        static Type ProgressionItemType()
        {
            var f = FindField(typeof(ContextRankConfig), "m_CustomProgression");
            var t = f == null ? null : f.FieldType.GetElementType();
            if (t == null) throw new MissingFieldException("Golemancer: ContextRankConfig.m_CustomProgression (array) not found");
            return t;
        }

        static Array ProgressionArray(ProgressionEntry[] entries)
        {
            var t = ProgressionItemType();
            var b = FindField(t, "BaseValue");
            var v = FindField(t, "ProgressionValue");
            if (b == null || v == null) throw new MissingFieldException("Golemancer: CustomProgressionItem.BaseValue/ProgressionValue not found");
            var arr = Array.CreateInstance(t, entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                var it = Activator.CreateInstance(t, true);
                b.SetValue(it, entries[i].Level);
                v.SetValue(it, entries[i].Value);
                arr.SetValue(it, i);
            }
            return arr;
        }

        public static KeyValuePair<int, int>[] ProgressionOf(ContextRankConfig rc)
        {
            if (rc == null) throw new ArgumentNullException("rc");
            var t = ProgressionItemType();
            var b = FindField(t, "BaseValue");
            var v = FindField(t, "ProgressionValue");
            var arr = FindField(typeof(ContextRankConfig), "m_CustomProgression").GetValue(rc) as Array;
            if (arr == null || b == null || v == null) throw new InvalidOperationException("Golemancer: ContextRankConfig custom progression unreadable");
            return arr.Cast<object>().Select(it => new KeyValuePair<int, int>((int)b.GetValue(it), (int)v.GetValue(it))).ToArray();
        }

        static AbilityRankType RankOf(StatAxis a)
        {
            switch (a)
            {
                case StatAxis.NaturalArmor: return AbilityRankType.Default;
                case StatAxis.StrengthDelta: return AbilityRankType.StatBonus;
                case StatAxis.Dr: return AbilityRankType.DamageDice;
            }
            throw new ArgumentException("Golemancer: no rank type for pet stat axis " + a);
        }

        static StatAxis AxisOf(AbilityRankType r)
        {
            foreach (var a in new[] { StatAxis.NaturalArmor, StatAxis.StrengthDelta, StatAxis.Dr }) if (RankOf(a) == r) return a;
            throw new ArgumentException("Golemancer: rank type " + r + " is not a pet stat axis");
        }

        static ContextValue RankValue(AbilityRankType r) { return new ContextValue { ValueType = ContextValueType.Rank, ValueRank = r }; }

        static ContextRankConfig RankConfig(AbilityRankType r, Material m, StatAxis axis)
        {
            if (RankOf(axis) != r) throw new ArgumentException("Golemancer: rank " + r + " does not carry axis " + axis);
            return RankConfigFor(r, m, axis, Guids.PetClass);
        }

        // 직업 레벨 기반 Custom 진행표(펫 = 펫 직업, 합체 = 연금술사 — 골렘 HD = 연금술사 레벨이라 같은 레벨 축).
        static ContextRankConfig RankConfigFor(AbilityRankType r, Material m, StatAxis axis, string classGuid)
        {
            var c = new ContextRankConfig();
            SetField(c, "m_Type", r);
            SetField(c, "m_BaseValueType", ContextRankBaseValueType.ClassLevel);
            SetField(c, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(classGuid) });
            SetField(c, "m_Progression", ContextRankProgression.Custom);
            SetField(c, "m_CustomProgression", ProgressionArray(StatRecipe.Progression(m, axis)));
            return c;
        }

         
        // 자연 방어 + DR + 힘(FusionStrBonus = 골렘 힘 수정치, 설계 §12 Q1). HP 보정·이동 없음. 직업 = 연금술사(본체 레벨 = 골렘 레벨), 재적용 안 함(레벨 갱신은 Fusion.ReconcileMaster 가 전부 회수 뒤 1개).
        // 축 ↔ 순위: 자연 방어 Default · 힘 StatBonus · DR DamageDice(펫과 같은 칸 배치, 힘 축만 FusionStrBonus).
        static readonly Dictionary<string, BlueprintFeature> fusionStats = new Dictionary<string, BlueprintFeature>();
        static readonly KeyValuePair<AbilityRankType, StatAxis>[] FusionAxes =
        {
            new KeyValuePair<AbilityRankType, StatAxis>(AbilityRankType.Default, StatAxis.NaturalArmor),
            new KeyValuePair<AbilityRankType, StatAxis>(AbilityRankType.StatBonus, StatAxis.FusionStrBonus),
            new KeyValuePair<AbilityRankType, StatAxis>(AbilityRankType.DamageDice, StatAxis.Dr),
        };

        public static BlueprintFeature FusionStatOf(string buffKey) { BlueprintFeature f; if (!fusionStats.TryGetValue(buffKey, out f)) throw new KeyNotFoundException("Golemancer: no fusion stat feature for key " + buffKey + " (Fusion.Build not run?)"); return f; }

        internal static void BuildFusionStats()
        {
            RequireBuilt();
            if (fusionStats.Count != 0) throw new InvalidOperationException("Golemancer: Materials.BuildFusionStats() called twice");
            foreach (var key in MaterialCatalog.Keys)
            {
                var m = MaterialCatalog.MaterialOfKey(key);
                var f = Create<BlueprintFeature>("GolemancerFusionStats_" + key.Replace("@", "_"), MaterialCatalog.FusionStatFeature(key), x =>
                {
                    x.HideInUI = true;
                    x.HideInCharacterSheetAndLevelUp = true;
                    x.Ranks = 1;
                    x.ReapplyOnLevelUp = false;
                    Taunt.NoNames(x); // 2026-11-16: 숨은 수치 특성 — 문구 없음을 명시(null 금지)
                });
                AddComponents(f,
                    new AddContextStatBonus { Stat = StatType.AC, Descriptor = NaturalArmorDescriptor, Multiplier = 1, Value = RankValue(AbilityRankType.Default) },
                    RankConfigFor(AbilityRankType.Default, m, StatAxis.NaturalArmor, Guids.Game.AlchemistClass),
                    new AddContextStatBonus { Stat = StatType.Strength, Descriptor = StrengthDescriptor, Multiplier = 1, Value = RankValue(AbilityRankType.StatBonus) },
                    RankConfigFor(AbilityRankType.StatBonus, m, StatAxis.FusionStrBonus, Guids.Game.AlchemistClass),
                    new AddDamageResistancePhysical { Value = RankValue(AbilityRankType.DamageDice), BypassedByMaterial = m.DrBypassAdamantine, Material = PhysicalDamageMaterial.Adamantite, Pool = new ContextValue() },
                    RankConfigFor(AbilityRankType.DamageDice, m, StatAxis.Dr, Guids.Game.AlchemistClass));
                // 되읽기: 순위 3칸 = FusionAxes, 진행표 = StatRecipe.Progression(m, 축), 직업 = 연금술사, HP·이동 보정 없음, 재적용 false.
                var alchemist = Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass);
                var rcs = f.ComponentsArray.OfType<ContextRankConfig>().ToArray();
                if (rcs.Length != FusionAxes.Length || f.ComponentsArray.Length != 6 || f.ComponentsArray.OfType<AddStatBonus>().Any() || f.ReapplyOnLevelUp)
                    throw new InvalidOperationException("Golemancer: " + f.name + " component readback failed");
                foreach (var rc in rcs)
                {
                    var r = (AbilityRankType)FindField(typeof(ContextRankConfig), "m_Type").GetValue(rc);
                    var hit = FusionAxes.Where(a => a.Key == r).ToArray();
                    if (hit.Length != 1) throw new InvalidOperationException("Golemancer: " + f.name + " rank " + r + " is not a fusion axis");
                    var items = ProgressionOf(rc);
                    var want = StatRecipe.Progression(m, hit[0].Value);
                    var cls = (BlueprintCharacterClassReference[])FindField(typeof(ContextRankConfig), "m_Class").GetValue(rc);
                    if (items.Length != want.Length || items.Where((it, i) => it.Key != want[i].Level || it.Value != want[i].Value).Any() || cls.Length != 1 || !ReferenceEquals(cls[0].Get(), alchemist))
                        throw new InvalidOperationException("Golemancer: " + f.name + " " + hit[0].Value + " progression / class readback failed");
                }
                fusionStats.Add(key, f);
            }
        }

        // ---------------- 재질 버프 13 ----------------
        static BlueprintBuff BuildBuff(string key, Material m, Kingmaker.Visual.PolymorphTransitionSettings transition)
        {
            if (!transition) throw new ArgumentNullException("transition", "Golemancer: material buff " + key + " needs the elemental transition asset");
            var b = Create<BlueprintBuff>("GolemancerMaterialBuff_" + key.Replace("@", "_"), MaterialCatalog.BuffGuid(key), x =>
            {
                SetField(x, "m_DisplayName", Strings.L(NameKey(m)));
                SetField(x, "m_Description", Strings.L(SummaryKey(m))); // 2026-11-17: 버프 툴팁은 한 줄 요약(선택지용 여러 줄 설명과 키를 나눴다)
                SetField(x, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(x, "m_Flags", FlagsOf("StayOnDeath")); // F07: 사망 때 빠지지 않는다(재질은 부서져도 유지 — 설계 §4.7)
                x.Stacking = StackingType.Replace;
                EmptyFxOnRemove(x); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — 재질 형태 버프 제거 때 Buff.OnRemove @00B0 NRE)
            });
            var facts = MaterialCatalog.BuffFactGuids(m);
            var poly = new Polymorph
            {
                Size = (Size)Enum.Parse(typeof(Size), m.Size),
                StrengthBonus = 0, DexterityBonus = 0, ConstitutionBonus = 0, NaturalArmor = 0, // 수치는 수치 특성이 소유(R2-03)
            };
            SetField(poly, "m_Prefab", new UnitViewLink { AssetId = MaterialCatalog.PrefabOf(m) });
            SetField(poly, "m_MainHand", Ref<BlueprintItemWeaponReference>(MaterialCatalog.SlamGuid(key)));
            SetField(poly, "m_OffHand", null); // 슬램 1회(설계 §4.4) — get_OffHand 는 null 참조를 null 로 돌려준다(@0006~000B)
            SetField(poly, "m_AdditionalLimbs", new BlueprintItemWeaponReference[0]);
            SetField(poly, "m_SecondaryAdditionalLimbs", new BlueprintItemWeaponReference[0]);
            SetField(poly, "m_KeepSlots", false);
            SetField(poly, "m_Facts", facts.Select(g => Ref<BlueprintUnitFactReference>(g)).ToArray());
            SetField(poly, "m_EnterTransition", new Polymorph.VisualTransitionSettings());
            SetField(poly, "m_ExitTransition", new Polymorph.VisualTransitionSettings());
            // 엔진은 외부 전환이 있으면 인라인 전환(위 두 줄) 대신 이것을 쓴다(FusionFxTable 머리말). 재질 버프가 살아 있는 View 에서 켜지고 꺼질 때만 재생 — 로드·생성 경로는 즉시(immediate) 교체라 FX 없음(Polymorph.HandleUnitSpawned @0014 ldc.i4.1).
            SetField(poly, FusionFxTable.TransitionField, transition);
            AddComponents(b, poly);
            var back = b.GetComponent<Polymorph>();
            if (!ReferenceEquals(FindField(typeof(Polymorph), FusionFxTable.TransitionField).GetValue(back), transition))
                throw new InvalidOperationException("Golemancer: material buff " + b.name + " transition FX readback failed");
            var backFacts = ((BlueprintUnitFactReference[])FindField(typeof(Polymorph), "m_Facts").GetValue(back)).Select(r => r.Guid.ToString()).ToArray();
            if (back.Size.ToString() != m.Size || ((UnitViewLink)FindField(typeof(Polymorph), "m_Prefab").GetValue(back)).AssetId != MaterialCatalog.PrefabOf(m)
                || !ReferenceEquals(((BlueprintItemWeaponReference)FindField(typeof(Polymorph), "m_MainHand").GetValue(back)).Get(), slams[key]) || !backFacts.SequenceEqual(facts)
                || backFacts.Intersect(MaterialCatalog.StatFeatureGuids).Any() || !b.StayOnDeath)
                throw new InvalidOperationException("Golemancer: material buff " + b.name + " readback failed");
            return b;
        }

        static string NameKey(Material m) { return m.Tier == 0 ? "MaterialWood.Name" : "Material." + m.Id + ".Name"; }
        // 설명 키 = 문구 생성기와 같은 원천(ChoiceText — 키가 표에 없으면 Strings.L 이 던진다).
        static string DescKey(Material m) { return ChoiceText.MaterialDescriptionKey(m); }
        static string SummaryKey(Material m) { return ChoiceText.MaterialSummaryKey(m); }

        // ---------------- 재질 특성 10(나무 = T3 자리 재사용) ----------------
        static BlueprintFeature BuildFeature(Material m)
        {
            var f = m.Tier == 0 ? GolemMaster.WoodMaterialFeature : Create<BlueprintFeature>("GolemancerMaterial_" + m.Id, MaterialCatalog.FeatureGuid(m), x => Named(x, NameKey(m), DescKey(m), SummaryKey(m)));
            if (f.AssetGuid.ToString() != MaterialCatalog.FeatureGuid(m)) throw new InvalidOperationException("Golemancer: material feature GUID mismatch for " + m.Id);
            var replaced = MaterialCatalog.ReplacedBy(m);
            var comps = new List<BlueprintComponent>();
            foreach (var r in replaced)
            {
                var rm = new RemoveFeatureOnApply();
                SetField(rm, "m_Feature", Ref<BlueprintUnitFactReference>(MaterialCatalog.FeatureGuid(r)));
                comps.Add(rm);
            }
            comps.Add(new GolemMaterialComponent()); // 마지막: 이전 재질이 지워진 뒤 활성(CurrentOf 정확히 1)
            AddComponents(f, comps.ToArray());
            var back = f.ComponentsArray;
            if (back.Length != replaced.Count + 1 || !(back[back.Length - 1] is GolemMaterialComponent)
                || !back.Take(replaced.Count).Select(c => (c as RemoveFeatureOnApply) == null ? "?" : ((RemoveFeatureOnApply)c).Feature.AssetGuid.ToString()).SequenceEqual(replaced.Select(MaterialCatalog.FeatureGuid)))
                throw new InvalidOperationException("Golemancer: material feature " + f.name + " components readback failed (remove-before-material)");
            return f;
        }

        static BlueprintFeatureSelection BuildSelection(int level, int tier)
        {
            var mats = MaterialCatalog.Materials.Where(m => m.Tier == tier).ToArray();
            if (mats.Length != MaterialCatalog.SelectableAt(level)) throw new InvalidOperationException("Golemancer: tier " + tier + " has " + mats.Length + " materials");
            var s = Create<BlueprintFeatureSelection>("GolemancerMaterialSelection" + level, MaterialCatalog.SelectionGuid(level), x =>
            {
                // 2026-11-17: 선택 화면마다 이름·설명이 다르다(레벨·고를 수 있는 재질·이전 재질·그 구간의 기준표). 짧은 설명은 공통 한 줄.
                SetField(x, "m_DisplayName", Strings.L(ChoiceText.MaterialSelectionNameKey(level)));
                SetField(x, "m_Description", Strings.L(ChoiceText.MaterialSelectionDescriptionKey(level)));
                SetField(x, "m_DescriptionShort", Strings.L("MaterialSelection.Summary"));
                x.IsClassFeature = true;
                x.Ranks = 1;
                SetField(x, "m_AllFeatures", mats.Select(m => Ref<BlueprintFeatureReference>(MaterialCatalog.FeatureGuid(m))).ToArray());
                SetField(x, "m_Features", new BlueprintFeatureReference[0]);
            });
            var all = s.AllFeatures.ToArray();
            if (all.Length != mats.Length || !all.Select(f => f.AssetGuid.ToString()).SequenceEqual(mats.Select(MaterialCatalog.FeatureGuid)) || s.Ranks != 1)
                throw new InvalidOperationException("Golemancer: material selection L" + level + " readback failed");
            Archetype.AddAt(level, s);
            return s;
        }

        // ---------------- 관측: 주인의 현재 재질 ----------------
        public static Material[] MaterialsOn(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            return master.Facts.List.Where(f => f.Blueprint is BlueprintFeature).Select(f => MaterialCatalog.OfFeatureGuidOrNull(f.Blueprint.AssetGuid.ToString())).Where(m => m != null).ToArray();
        }

        // 정확히 1개(0·2+ = throw).
        public static Material CurrentOf(UnitEntityData master)
        {
            var ms = MaterialsOn(master);
            if (ms.Length != 1) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has " + ms.Length + " material feature(s) [" + string.Join(",", ms.Select(m => m.Id).ToArray()) + "], expected exactly 1");
            return ms[0];
        }

        // 관측용: 0·2+ = null(생명주기 코어가 「재질 키 미확인」 Wait 로 다룬다 — 계획 T2 Obs.MaterialKey).
        public static Material CurrentOfOrNull(UnitEntityData master)
        {
            var ms = MaterialsOn(master);
            return ms.Length == 1 ? ms[0] : null;
        }

        // ---------------- 골렘 위 우리 버프·수치 사실 ----------------
        public static List<Buff> OurBuffs(UnitEntityData golem)
        {
            if (golem == null) throw new ArgumentNullException("golem");
            RequireBuilt();
            return golem.Buffs.Enumerable.Where(b => buffs.Values.Contains(b.Blueprint)).ToList();
        }

        public static string KeyOfBuff(Buff b)
        {
            if (b == null) throw new ArgumentNullException("b");
            var hit = buffs.Where(kv => ReferenceEquals(kv.Value, b.Blueprint)).Select(kv => kv.Key).ToArray();
            if (hit.Length != 1) throw new InvalidOperationException("Golemancer: buff " + b.Blueprint.name + " is not one of our material buffs");
            return hit[0];
        }

        // 우리 수치 특성(펫 13 + 합체 13 = MaterialCatalog.StatFeatureGuids) 블루프린트의 사실 전부 — 유닛 무관(T5 는 주인에게 부른다).
        public static List<EntityFact> OwnedStatFacts(UnitEntityData unit)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            return unit.Facts.List.Where(IsOwnedStatFact).Cast<EntityFact>().ToList();
        }

        // 「우리 수치 특성의 사실인가」 단일 판정(C4 — OwnedStatFacts 와 Fusion 매 틱 셈이 같이 쓴다). 사실 null·블루프린트 null 은 예전처럼 NullReferenceException.
        // 예전 = StatFeatureGuids.Contains(AssetGuid.ToString()) — 사실마다 문자열을 만들었다(매 프레임 캐릭터당 사실 수만큼). 지금 = BlueprintGuid 집합(IEquatable, 상자 없음).
        internal static readonly Func<EntityFact, bool> IsOwnedStatFact = f => OwnedStatGuids.Set.Contains(f.Blueprint.AssetGuid);

        static class OwnedStatGuids
        {
            // 같음의 근거: BlueprintGuid.ToString() = Guid.ToString("N")(빈 GUID 는 "" — dnlib 2026-11-15). 우리 문자열이 그 표준형이 아니면 두 판정이 갈라지므로
            // 형식 초기화에서 던진다(TypeInitializationException — 시끄럽게). 빈 GUID 는 우리 표에 없다(표준형 검사가 막는다).
            public static readonly HashSet<BlueprintGuid> Set = Build();
            static HashSet<BlueprintGuid> Build()
            {
                var s = new HashSet<BlueprintGuid>();
                foreach (var g in MaterialCatalog.StatFeatureGuids)
                {
                    var b = BlueprintGuid.Parse(g);
                    if (b.ToString() != g) throw new InvalidOperationException("Golemancer: stat feature GUID '" + g + "' is not in BlueprintGuid canonical form ('" + b + "')");
                    if (!s.Add(b)) throw new InvalidOperationException("Golemancer: stat feature GUID '" + g + "' listed twice");
                }
                if (s.Count != MaterialCatalog.StatFeatureGuids.Count) throw new InvalidOperationException("Golemancer: stat feature GUID set " + s.Count + " != " + MaterialCatalog.StatFeatureGuids.Count);
                return s;
            }
        }

         
        public static EntityFact AddStat(UnitEntityData unit, StatSet set, string buffKey)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            if (!Enum.IsDefined(typeof(StatSet), set)) throw new ArgumentException("Golemancer: undefined stat set " + (int)set);
            string guid = set == StatSet.Pet ? MaterialCatalog.PetStatFeature(buffKey) : MaterialCatalog.FusionStatFeature(buffKey);
            var bp = Get<BlueprintFeature>(guid); // 합체 수치 특성은 T5 가 만든다 — 그 전에 부르면 「blueprint not found」로 던진다
            var fact = unit.Descriptor.AddFact(bp);
            if (fact == null || fact.MaybeContext == null || !ReferenceEquals(fact.MaybeContext.MaybeCaster, unit))
                throw new InvalidOperationException("Golemancer: AddStat " + bp.name + " on " + unit.CharacterName + " readback failed (fact " + (fact == null ? "null" : "caster " + (fact.MaybeContext == null ? "no context" : (fact.MaybeContext.MaybeCaster == null ? "null" : fact.MaybeContext.MaybeCaster.CharacterName))) + ")");
            return fact;
        }

        public static void RemoveStat(UnitEntityData unit, EntityFact fact)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            if (fact == null) throw new ArgumentNullException("fact");
            if (!MaterialCatalog.StatFeatureGuids.Contains(fact.Blueprint.AssetGuid.ToString())) throw new InvalidOperationException("Golemancer: RemoveStat on " + fact.Blueprint.name + " (not our stat feature)");
            unit.Descriptor.RemoveFact(fact);
            if (unit.Facts.List.Contains(fact)) throw new InvalidOperationException("Golemancer: RemoveStat " + fact.Blueprint.name + " still on " + unit.CharacterName);
        }

        // 재질 버프가 붙인 사실(m_Facts) 중 골렘에 없는 것(사망 때 StayOnDeath 없는 본편 버프가 빠진다 — F07; 결정 D5: 빠졌으면 버프를 다시 건다).
        public static string[] MissingBuffFacts(UnitEntityData golem, Material m)
        {
            var have = new HashSet<string>(golem.Facts.List.Select(f => f.Blueprint.AssetGuid.ToString()));
            return MaterialCatalog.BuffFactGuids(m).Where(g => !have.Contains(g)).ToArray();
        }

        // ---------------- Apply ② 재질 단계(GolemMaster.MaterialsStep 이 부른다 — Apply 트랜잭션 안에서만) ----------------
        public static string Reconcile(UnitEntityData master, UnitEntityData golem, int level, string key)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (golem == null) throw new ArgumentNullException("golem");
            RequireBuilt();
            var m = MaterialCatalog.Get(key);
            MaterialCatalog.RequireLegal(m, level); // LegalLevels 검사 — 진행표 Custom 은 도메인 밖 입력에 엉뚱한 항목을 집는다(StatRecipe.Progression 주석)
            var cur = CurrentOf(master);
            if (cur.Id != m.Id) throw new InvalidOperationException("Golemancer: Reconcile key " + key + " but " + master.CharacterName + "'s material is " + cur.Id);
            string buffKey = StatRecipe.BuffKey(m, level);

            var ours = OurBuffs(golem);
            var stats = OwnedStatFacts(golem);
            var plan = MaterialPlan.Reconcile(ours.Select(KeyOfBuff).ToArray(), stats.Select(f => MaterialCatalog.StatKeyOfFeature(f.Blueprint.AssetGuid.ToString()).Key).ToArray(), buffKey, buffKey);
            var missing = plan.AddBuff == null ? MissingBuffFacts(golem, m) : new string[0];
            bool reapply = missing.Length > 0;
            if (reapply) WotrClassMod.Common.ModContext.Log("Golemancer: " + golem.CharacterName + " material buff " + buffKey + " lost facts [" + string.Join(",", missing) + "] — reapplying the buff");

            // 버프: 다른 키 전부 제거(+ 사실이 빠진 같은 키) → 변신이 풀린 것을 확인 → 기대 키 추가.
            bool removed = false;
            foreach (var b in ours)
            {
                string k = KeyOfBuff(b);
                if (plan.RemoveBuffs.Contains(k) || (reapply && k == buffKey)) { golem.Buffs.RemoveFact(b); removed = true; }
            }
            if (removed && golem.Body.IsPolymorphed && OurBuffs(golem).Count == 0) throw new InvalidOperationException("Golemancer: " + golem.CharacterName + " is still polymorphed after removing the material buffs");
            if (plan.AddBuff != null || reapply)
            {
                var bp = BuffOf(buffKey);
                var buff = golem.Descriptor.AddBuff(bp, golem, null);
                if (buff == null || buff.Context == null || !ReferenceEquals(buff.Context.MaybeCaster, golem)) throw new InvalidOperationException("Golemancer: AddBuff " + bp.name + " on " + golem.CharacterName + " readback failed (caster is not the golem)");
            }

             
            foreach (var f in stats) RemoveStat(golem, f);
            AddStat(golem, StatSet.Pet, buffKey);

            // 추가 슬램 사지(소유자 결정 2026-11-16): 표(Dials.SlamCounts)의 그 레벨 값 − 주 손 1 만큼. 평소 2회(11레벨부터)가 이 경로로 붙고 떨어진다(레벨업·재질 교체·휴식/수리 뒤 재적용·레벨 하락).
            // 폭주 중이면 폭주 값(재질 버프를 다시 걸어도 유지). Apply 는 살아 있는 골렘에만 온다 — 죽은 골렘이면(false) 던진다.
            string limbs = SlamLimbs.WantedForGolem(golem, level, Overdrive.IsActive(master));
            if (!SlamLimbs.Reconcile(golem, limbs)) throw new InvalidOperationException("Golemancer: Reconcile on a dead golem (slam limbs " + (limbs ?? "none") + " not applied)");

            // 되읽기: 우리 버프 정확히 1(기대 키, 시전자 골렘, 프리팹), 우리 수치 사실 정확히 1(기대 키, 시전자 골렘), 버프 사실 빠짐 없음.
            var after = OurBuffs(golem);
            var afterStats = OwnedStatFacts(golem);
            if (!MaterialPlan.Satisfied(after.Select(KeyOfBuff).ToArray(), afterStats.Select(f => MaterialCatalog.StatKeyOfFeature(f.Blueprint.AssetGuid.ToString()).Key).ToArray(), buffKey, buffKey))
                throw new InvalidOperationException("Golemancer: Reconcile readback: buffs [" + string.Join(",", after.Select(KeyOfBuff).ToArray()) + "] stats " + afterStats.Count + ", expected exactly " + buffKey);
            if (!ReferenceEquals(after[0].Context.MaybeCaster, golem) || !ReferenceEquals(afterStats[0].MaybeContext.MaybeCaster, golem))
                throw new InvalidOperationException("Golemancer: Reconcile readback: caster of the material buff / stat fact is not the golem");
            var activePrefab = ((UnitViewLink)FindField(typeof(Polymorph), "m_Prefab").GetValue(after[0].Blueprint.GetComponent<Polymorph>())).AssetId;
            if (activePrefab != MaterialCatalog.PrefabOf(m)) throw new InvalidOperationException("Golemancer: Reconcile readback: prefab " + activePrefab + " != " + MaterialCatalog.PrefabOf(m));
            var stillMissing = MissingBuffFacts(golem, m);
            if (stillMissing.Length > 0) throw new InvalidOperationException("Golemancer: Reconcile readback: material facts missing [" + string.Join(",", stillMissing) + "]");
            SlamLimbs.Readback(golem, limbs);
            return m.Id;
        }
    }

    // 재질 특성에 붙는다(주인 소유). 활성 = 생명주기 Activated 만(적용은 안정 경계의 Apply — 계획 T4 「활성 시 Life.Handle(Activated) 만 → Stale 예약」).
    // 블루프린트 컴포넌트는 모든 주인이 공유한다 → 인스턴스 필드 없음.
    public class GolemMaterialComponent : UnitFactComponentDelegate
    {
        protected override void OnActivate()
        {
            if (Owner.Get<UnitPartGolemMaster>() == null)
            {
                // 골렘 특성이 아직 활성 전(같은 레벨에서 골렘 특성 → 나무 재질 순, 로드 순서) — 골렘 특성 OnActivate 가 Activated 를 넣는다. 정당한 무동작.
                if (!Owner.Descriptor.HasFact(GolemMaster.Feature)) throw new InvalidOperationException("Golemancer: " + Owner.CharacterName + " has a golem material feature without the golem feature");
                return;
            }
            GolemMaster.Feed(Owner, LifeEventKind.Activated);
        }
    }
}
