using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using WotrClassMod.Common;
using static ChaosWarrior.BuildKit;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
     
    // 추첨·적용·되읽기 판단은 순수 MutationCore(어댑터 ContextActionChaosMutations). 표식·껍질은 분노 버프의 자식으로 묶여 분노가 끝나면 함께 풀린다(Buff.OnRemove @0047~0090).
    // 개편 O3: 같은 변이가 겹친다 — 표식은 순위형(인스턴스 1개 · 순위 = 뽑힌 횟수) 또는 인스턴스형(인스턴스 수 = 뽑힌 횟수), 껍질은 순위형(MutationSpecs.StackRows · ShellStackingName).
    public static class Mutations
    {
        static readonly Dictionary<string, BlueprintBuff> markers = new Dictionary<string, BlueprintBuff>();
        static readonly List<BlueprintBuff> shells = new List<BlueprintBuff>();






        public static Kingmaker.Blueprints.Classes.BlueprintFeature Feature;

        public static bool Built { get { return Feature != null && markers.Count == ChaosRules.Dials.MutationOptions && shells.Count == ChaosRules.Dials.ShellElements.Length; } }

        public static BlueprintBuff Get(string id)
        {
            BlueprintBuff b;
            if (!markers.TryGetValue(id, out b)) throw new KeyNotFoundException("ChaosWarrior: mutation marker '" + id + "' not built");
            return b;
        }

        public static void Build()
        {
            if (Feature != null || markers.Count != 0) throw new InvalidOperationException("ChaosWarrior: Mutations.Build() called twice");
            MutationSpecs.Validate(); // 개편 O1: 분류 개수·겹 방식 행의 정적 검사 — 어긋나면 블루프린트를 만들기 전에 던진다
            // 개편 O3: 표식의 겹 방식은 사양 행에서(순위형 = Stacking Rank · Ranks = 뽑힐 수 있는 최대 횟수 / 인스턴스형 = Stacking Stack). 엔진 Buff.AddRank 는 Ranks 를 넘으면 기록 없이 무동작(정찰 §1.2)
            // — Ranks 를 채우지 않으면(기본 0) 두 번째 뽑기부터 순위가 오르지 않는다. 빌드 되읽기(VerifyEffects)가 Stacking·Ranks 를 사양과 대조한다.
            foreach (var id in MutationSpecs.Ids)
            {
                var marker = BuildKit.Buff("ChaosWarriorMutation" + id, MutationSpecs.MarkerOf(id), "mut." + id + ".name", "mut." + id + ".desc", false, MutationSpecs.StackingName(id));
                marker.Ranks = MutationSpecs.RanksOf(id);
                markers.Add(id, marker);
            }
            var elements = ChaosRules.Dials.ShellElements;
            for (int i = 0; i < elements.Length; i++)
            {
                var shell = BuildKit.Buff("ChaosWarriorShell" + elements[i], MutationSpecs.ShellBuffIds[i], "shell." + elements[i] + ".name", "shell." + elements[i] + ".desc", false, MutationSpecs.ShellStackingName);
                shell.Ranks = MutationSpecs.ShellRanks;
                shells.Add(shell);
            }
            Feature = BuildKit.Feature("ChaosWarriorMutationFeature", Guids.MutationFeature, "mutation.name", "mutation.desc", false);
            Archetype.AddAt(ChaosRules.Levels.MinLevel, Feature);
            BuildEffects();
            if (!Built) throw new InvalidOperationException("ChaosWarrior: mutation blueprints incomplete (" + markers.Count + " markers, " + shells.Count + " shells)");
        }

        public static BlueprintBuff HorrorShakenBuff;
        public static Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect HorrorArea;

        // T6: 효과 행(MutationSpecs.EffectRows) 해석 → 표식·껍질 버프에 부품. 끝에 되읽기(행 ↔ 실제 부품) — 다르면 던진다.
        static void BuildEffects()
        {
            // 끔찍한 형상: 본편 Shaken 의 부품을 빌드 때 읽어 복제(C4 — 수치를 옮겨 적지 않는다) + 5 ft 원통 오라(적만).
            var shaken = Get<BlueprintBuff>(Guids.Game.Shaken);
            HorrorShakenBuff = BuildKit.Buff("ChaosWarriorHorrorShakenBuff", Guids.HorrorShakenBuff, "horror.shaken.name", "horror.shaken.desc", false, RageSpec.Stacking);
            var copies = new List<BlueprintComponent>();
            foreach (var c in shaken.ComponentsArray) copies.Add(BuildReflect.CloneComponent(c));
            AddComponents(HorrorShakenBuff, copies.ToArray());
            var horrorRow = MutationSpecs.RowsFor("HorrificForm").Single();
            HorrorArea = Create<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect>("ChaosWarriorHorrorArea", Guids.HorrorArea, a =>
            {
                a.Shape = E<Kingmaker.UnitLogic.Abilities.Blueprints.AreaEffectShape>("Cylinder");
                a.Size = new Kingmaker.Utility.Feet(horrorRow.Value(ChaosRules.Levels.MinLevel));
                a.AffectEnemies = true;
                a.AggroEnemies = false;
                a.AffectDead = false;
                a.Fx = new Kingmaker.ResourceLinks.PrefabLink();
                FillEmpty(a);
            });
            var isEnemy = new List<Kingmaker.ElementsSystem.Condition> { BuildKit.Cond(new Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionIsEnemy { Not = false }) };
            var areaBuff = Comp(new Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff
            {
                Condition = new Kingmaker.ElementsSystem.ConditionsChecker { Operation = E<Kingmaker.ElementsSystem.Operation>("And"), Conditions = isEnemy.ToArray() },
                CheckConditionEveryRound = false,
            });
            SetField(areaBuff, "m_Buff", Ref<BlueprintBuffReference>(Guids.HorrorShakenBuff));
            AddComponents(HorrorArea, areaBuff);

            foreach (var id in MutationSpecs.Ids) AddRows(Get(id), MutationSpecs.RowsFor(id));
            var elements = ChaosRules.Dials.ShellElements;
            for (int i = 0; i < elements.Length; i++) AddRows(shells[i], MutationSpecs.RowsFor(MutationSpecs.ShellPrefix + elements[i]));

            // FX(2026-11-15 승인, FxTable 원장): 공포 복제본이 잃었던 Shaken FxOnStart 복원 · 표식 16 · 껍질 4. 표에 없는 대상은 FxTable.ForTarget 이 던진다.
            ApplyFx(HorrorShakenBuff, FxTable.TargetHorrorShaken);
            foreach (var id in MutationSpecs.Ids) ApplyFx(Get(id), FxTable.MutationTarget(id));
            for (int i = 0; i < elements.Length; i++) ApplyFx(shells[i], FxTable.ShellTarget(elements[i]));

            var diffs = VerifyEffects();
            if (diffs.Count != 0) throw new InvalidOperationException("ChaosWarrior: mutation effect read-back differs from MutationSpecs.EffectRows: " + string.Join(" | ", diffs.ToArray()));
        }

        // 지속 FX(FxOnStart) · ResourceAssetIds + 1회 FX 전용 부품(있을 때만). 효과 행 부품 뒤에 붙는다 — 행 대조는 ChaosFx.IsFxComponent 로 뺀다.
        static void ApplyFx(BlueprintBuff b, string target)
        {
            ChaosFx.ApplyPersistent(b, target);
            var c = ChaosFx.OneShotComponent(target);
            if (c != null) AddComponents(b, c);
        }

        static void AddRows(BlueprintBuff b, IReadOnlyList<MutationSpecs.EffectRow> rows)
        {
            if (rows.Count == 0) throw new InvalidOperationException("ChaosWarrior: no effect row for " + b.name);
            var comps = new List<BlueprintComponent>();
            foreach (var r in rows)
            {
                int v = r.Value(ChaosRules.Levels.MinLevel);
                    if (r.Component == "None") { }
                    else if (r.Component == "ChangeUnitSize")
                        {
                            var c = Comp(new Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize { SizeDelta = v });
                            SetField(c, "m_Type", Enum.Parse(BuildReflect.FieldType(typeof(Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize), "m_Type"), r.Extra));
                            comps.Add(c);
                        }
                    else if (r.Component == "AddStatBonus")
                    {
                        comps.Add(Comp(new Kingmaker.UnitLogic.FactLogic.AddStatBonus { Stat = E<Kingmaker.EntitySystem.Stats.StatType>(r.Stat), Descriptor = E<Kingmaker.Enums.ModifierDescriptor>(r.Descriptor), Value = v, ScaleByBasicAttackBonus = false }));
                    }
                    else if (r.Component == "BuffAllSavesBonus")
                    {
                        comps.Add(Comp(new Kingmaker.Designers.Mechanics.Buffs.BuffAllSavesBonus { Descriptor = E<Kingmaker.Enums.ModifierDescriptor>(r.Descriptor), Value = v }));
                    }
                    else if (r.Component == "AddDamageResistancePhysical")
                    {
                        comps.Add(Comp(new Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical { Value = CvRank(ScaledRankType), UsePool = false, Pool = Cv(0), Or = false, BypassedByMaterial = false, BypassedByForm = false, BypassedByMagic = false, BypassedByAlignment = false, BypassedByReality = false, BypassedByWeaponType = false, BypassedByMeleeWeapon = false, BypassedByEpic = false }));
                        comps.Add(RankConfig(ScaledRankType, RageSpec.RankBaseValueType, RageSpec.RankProgression, ChaosRules.ProgressionRows(r.Value)));
                    }
                    else if (r.Component == "AddEffectFastHealing")
                    {
                        comps.Add(Comp(new Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing { Heal = 0, Bonus = CvRank(ScaledRankType) }));
                        comps.Add(RankConfig(ScaledRankType, RageSpec.RankBaseValueType, RageSpec.RankProgression, ChaosRules.ProgressionRows(r.Value)));
                    }
                    else if (r.Component == "AddDamageResistanceEnergy")
                    {
                        comps.Add(Comp(new Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy { Type = E<Kingmaker.Enums.Damage.DamageEnergyType>(r.Extra), Value = Cv(v), UsePool = false, Pool = Cv(0), UseValueMultiplier = false, ValueMultiplier = Cv(0), HealOnDamage = false }));
                    }
                    else if (r.Component == "AddAdditionalLimb")
                        {
                            // 개편 O4: 사지 무기는 그 변이의 「우리 복제 무기」여야 한다(NaturalWeapons.Build 가 먼저 만든다). 본편 무기나 다른 변이의 무기면 사지 대조(MutationCore.CheckLimbs)가
                            // 그 사지를 세지 못한다 → 조용히 붙이지 않고 던진다.
                            if (NaturalWeaponPlan.MutationOf(r.Extra) != r.Id) throw new InvalidOperationException("ChaosWarrior: effect row " + r.Id + "/AddAdditionalLimb names weapon " + r.Extra + ", which is not that mutation's cloned natural weapon (NaturalWeaponPlan)");
                            var weapon = Get<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(r.Extra);
                            if (!NaturalWeapons.Weapons.ContainsKey(r.Id) || !ReferenceEquals(NaturalWeapons.Weapons[r.Id], weapon)) throw new InvalidOperationException("ChaosWarrior: natural weapon clone for " + r.Id + " is not built yet (NaturalWeapons.Build must run before Mutations.Build)");
                            var c = Comp(new Kingmaker.UnitLogic.FactLogic.AddAdditionalLimb());
                            SetField(c, "m_Weapon", Ref<BlueprintItemWeaponReference>(r.Extra));
                            comps.Add(c);
                        }
                    else if (r.Component == "AddAreaEffect")
                        {
                            var c = Comp(new Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect());
                            SetField(c, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(r.Extra));
                            comps.Add(c);
                        }
                    else if (r.Component == "AddTargetAttackWithWeaponTrigger")
                        {
                            // 개편 O2: 주사위 개수 = 값 × 표식 순위(가시 피부 n겹 = 주사위 n개). 행이 다른 겹 방식을 말하면 조용히 순위형으로 만들지 않는다.
                            RequireScale(r, MutationSpecs.RowScale.PerRankContext);
                            var dmg = Damage(E<Kingmaker.RuleSystem.Rules.Damage.DamageType>("Physical"), r.Extra, E<Kingmaker.RuleSystem.DiceType>("D" + ChaosRules.Dials.ThornsDiceSides), CvBuffRank(v), Cv(0));
                            var onAttacker = new List<Kingmaker.ElementsSystem.GameAction> { dmg };
                            comps.Add(Comp(new Kingmaker.UnitLogic.Mechanics.Components.AddTargetAttackWithWeaponTrigger
                            {
                                TriggerBeforeAttack = false, WaitForAttackResolve = true, OnlyHit = true, CriticalHit = false, OnlyOnFirstAttack = false, OnAttackOfOpportunity = false,
                                OnlyMelee = true, OnlyRanged = false, NotReach = false, OnlySneakAttack = false, NotSneakAttack = false, CheckCategory = false, DoNotPassAttackRoll = true, Not = false,
                                Categories = new Kingmaker.Enums.WeaponCategory[0], ActionsOnAttacker = Actions(onAttacker), ActionOnSelf = Actions(new List<Kingmaker.ElementsSystem.GameAction>()),
                            }));
                        }
                    else if (r.Component == "NewRoundDamage")
                        {
                            // 개편 O2: 피해 = 값 × 표식 순위(터지는 핏줄 n겹 = 라운드당 피해 n배).
                            RequireScale(r, MutationSpecs.RowScale.PerRankContext);
                            var dmg = Damage(E<Kingmaker.RuleSystem.Rules.Damage.DamageType>(r.Extra), null, E<Kingmaker.RuleSystem.DiceType>("Zero"), Cv(0), CvBuffRank(v));
                            var round = new List<Kingmaker.ElementsSystem.GameAction> { dmg };
                            var none = new List<Kingmaker.ElementsSystem.GameAction>();
                            comps.Add(Comp(new Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions { Activated = Actions(none), Deactivated = Actions(none), NewRound = Actions(round), Dispose = Actions(none) }));
                        }
                    else if (r.Component == "WeaponAttackTypeDamageBonus")
                        {
                            // 개편 O6(피의 갈증): 근접 피해 = Value × AttackBonus × 표식 순위 — 엔진이 순위를 곱한다(OnEventAboutToTrigger @0000~0051, 시험 `exec: two-sided engine`). 값은 고정(Cv), 순위형 값(CvBuffRank)을 쓰면 순위가 두 번 곱해진다.
                            // 기술자는 엔진 코드가 UntypedStackable 로 고정한다(Descriptor 필드는 읽히지 않는다) — 행의 기술자 칸을 그대로 넣어 되읽기·설명이 같은 값을 보게 한다.
                            RequireScale(r, MutationSpecs.RowScale.PerRank);
                            comps.Add(Comp(new Kingmaker.UnitLogic.Buffs.Components.WeaponAttackTypeDamageBonus
                            {
                                Type = E<Kingmaker.Enums.WeaponRangeType>(r.Extra),
                                AttackBonus = ChaosRules.Dials.WeaponDamageBonusMultiplier,
                                Descriptor = E<Kingmaker.Enums.ModifierDescriptor>(r.Descriptor),
                                Value = Cv(v),
                            }));
                        }
                    else throw new InvalidOperationException("ChaosWarrior: effect row component '" + r.Component + "' for " + r.Id + " has no builder");
            }
            if (comps.Count > 0) AddComponents(b, comps.ToArray());
        }

        // 레벨 비례 값(굳은 살갗·재생)의 순위 종류 — 버프마다 순위 하나.
        static readonly string ScaledRankType = "Default";

        static void RequireScale(MutationSpecs.EffectRow r, MutationSpecs.RowScale built)
        {
            if (r.Scale != built) throw new InvalidOperationException("ChaosWarrior: effect row " + r.Id + "/" + r.Component + " says scale " + r.Scale + " but the builder makes it " + built);
        }

        // 「값 × 이 버프의 순위」(개편 O2, 계획 Task O2 갈림 A). 엔진 근거(K1 — Assembly-CSharp IL 직독 2026-11-16, md5 앞 12 748b3a143b60; 시험 `wiring: mutations builder reads rows` 가 지문을 잠근다):
        //   ContextValue.Calculate(context, …) @019B~022E(CasterBuffRank = 11): bp = context.AssociatedBlueprint as BlueprintBuff — null 이면 0 반환;
        //     context.MaybeCaster?.Buffs?.GetFact(bp)?.GetRank() × Value — 시전자·버프가 없으면 0(기록 없음).
        //   BuffCollection.AddBuff(bp, context, duration) @0000~0009: context.CloneFor(bp, Owner, null) → MechanicsContext..ctor @00B5 가 AssociatedBlueprint = bp(붙이는 버프 자신),
        //     시전자 = 부모 문맥의 MaybeCaster(CloneFor @0000~000B). GameMutationWorld.ApplyChild 는 분노 문맥의 시전자에게 붙이므로(ParentIsActiveRage 가 Owner == caster 를 본다) 「문맥 시전자 = 표식 보유자」.
        //   부품 행동은 그 표식 버프의 문맥에서 돈다: AddTargetAttackWithWeaponTrigger.TryRunActions @0033~0055 · AddFactContextActions.OnNewRound @0009~0020 →
        //     Buff 의 IFactContextOwner.RunActionInContext @000C~0013(Buff.Context) → ContextActionDealDamage.GetDamageInfo @00C9~00DA(주사위 개수)·@0155~0166(보너스) = ContextValue.Calculate(Context).
        // 「없으면 0」은 엔진의 조용한 경로다 — 오프라인으로는 값을 볼 수 없어 게임 안 S5 thorns-1d6 · bursting-veins-1(순위 1 의 절대값: 0 이면 FAIL)에 기댄다(계획 「조용한 가정 닫기」 A6).
        static ContextValue CvBuffRank(int perRank)
        {
            return new ContextValue { ValueType = E<ContextValueType>("CasterBuffRank"), Value = perRank };
        }

        // 값 종류 줄(빌드 되읽기): 순위형 = "rank×<값>", 고정 = "<값>". 그 밖의 값 종류는 "?<종류>" — 사양 줄과 반드시 달라진다.
        const string RankTimes = "rank×";
        static string DescribeValue(ContextValue v)
        {
            if (v == null) return "?null";
            if (v.ValueType == E<ContextValueType>("CasterBuffRank")) return RankTimes + v.Value;
            if (v.ValueType == E<ContextValueType>("Simple")) return v.Value.ToString();
            return "?" + v.ValueType;
        }

        static Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage Damage(Kingmaker.RuleSystem.Rules.Damage.DamageType type, string form, Kingmaker.RuleSystem.DiceType dice, ContextValue count, ContextValue bonus)
        {
            var dt = new Kingmaker.RuleSystem.Rules.Damage.DamageTypeDescription { Type = type };
            if (form != null) dt.Physical.Form = E<Kingmaker.Enums.Damage.PhysicalDamageForm>(form);
            var a = Act(new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage
            {
                DamageType = dt,
                Duration = new Kingmaker.UnitLogic.Mechanics.ContextDurationValue { Rate = E<Kingmaker.UnitLogic.Mechanics.DurationRate>("Rounds"), DiceType = E<Kingmaker.RuleSystem.DiceType>("Zero"), DiceCountValue = Cv(0), BonusValue = Cv(0) },
                Value = new Kingmaker.UnitLogic.Mechanics.ContextDiceValue { DiceType = dice, DiceCountValue = count, BonusValue = bonus },
                Half = false, HalfIfSaved = false, IgnoreCritical = true,
            });
            SetField(a, "m_Type", Enum.Parse(BuildReflect.FieldType(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage), "m_Type"), "Damage"));
            return a;
        }

        // 생산 사양 ↔ 실제 부품(빌드 시 + 점검 S5 spec-readback-all). 줄 = "부품|스탯|기술자|값|보충". 레벨 비례 값은 CRC 행 대조.
        public static List<string> VerifyEffects()
        {
            var d = new List<string>();
            var targets = MutationSpecs.Ids.Select(id => Tuple.Create(id, Get(id))).Concat(ChaosRules.Dials.ShellElements.Select((e, i) => Tuple.Create(MutationSpecs.ShellPrefix + e, shells[i]))).ToList();
            foreach (var t in targets)
            {
                var want = MutationSpecs.RowsFor(t.Item1).Where(r => r.Component != "None").Select(r => Expect(r)).ToList();
                var got = (t.Item2.ComponentsArray ?? new BlueprintComponent[0]).Where(c => !(c is Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig) && !ChaosFx.IsFxComponent(c)).Select(Describe).ToList();
                d.AddRange(ChaosFx.Verify(FxTable.TargetOfRowId(t.Item1), t.Item2));
                if (!got.SequenceEqual(want)) d.Add(t.Item1 + ": [" + string.Join(" ; ", got.ToArray()) + "] != [" + string.Join(" ; ", want.ToArray()) + "]");
                var scaled = MutationSpecs.RowsFor(t.Item1).Where(r => r.Scaled).ToList();
                var ranks = t.Item2.ComponentsArray.OfType<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>().Select(DescribeRank).ToList();
                var wantRanks = scaled.Select(r => DescribeRows(ScaledRankType, RageSpec.RankBaseValueType, RageSpec.RankProgression, ChaosRules.ProgressionRows(r.Value))).ToList();
                if (!ranks.SequenceEqual(wantRanks)) d.Add(t.Item1 + " rank configs [" + string.Join(" ; ", ranks.ToArray()) + "] != [" + string.Join(" ; ", wantRanks.ToArray()) + "]");
                // 개편 O3: 겹 방식(Stacking · Ranks) == 사양 행. 순위형인데 Ranks 가 모자라면 엔진이 겹을 조용히 버린다(Buff.AddRank @0004~0019).
                bool isShell = shells.Contains(t.Item2);
                string wantStacking = isShell ? MutationSpecs.ShellStackingName : MutationSpecs.StackingName(t.Item1);
                int wantStackRanks = isShell ? MutationSpecs.ShellRanks : MutationSpecs.RanksOf(t.Item1);
                if (t.Item2.Stacking != E<StackingType>(wantStacking) || t.Item2.Ranks != wantStackRanks)
                    d.Add(t.Item1 + " stacking " + t.Item2.Stacking + " ranks " + t.Item2.Ranks + " != spec " + wantStacking + " ranks " + wantStackRanks);
            }
            var shaken = Get<BlueprintBuff>(Guids.Game.Shaken).ComponentsArray.Select(c => c.GetType().Name).ToList();
            var ours = HorrorShakenBuff.ComponentsArray.Select(c => c.GetType().Name).ToList();
            if (!shaken.SequenceEqual(ours)) d.Add("horror shaken components [" + string.Join(",", ours.ToArray()) + "] != vanilla Shaken [" + string.Join(",", shaken.ToArray()) + "]");
            d.AddRange(ChaosFx.Verify(FxTable.TargetHorrorShaken, HorrorShakenBuff));
            var ab = HorrorArea.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff>().ToArray();
            if (ab.Length != 1 || ab[0].Buff != HorrorShakenBuff || ab[0].Condition.Conditions.Length != 1 || !(ab[0].Condition.Conditions[0] is Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionIsEnemy)) d.Add("horror area buff/condition != (enemy -> horror shaken)");
            if (HorrorArea.Size.Value != ChaosRules.Dials.HorrorRadiusFeet) d.Add("horror area size " + HorrorArea.Size.Value + " != " + ChaosRules.Dials.HorrorRadiusFeet);
            return d;
        }

        static string Expect(MutationSpecs.EffectRow r)
        {
            // 순위에 곱해지는 값(PerRankContext)은 값 종류까지 대조한다: "rank×<값>". 레벨 비례 값은 "rank"(CRC 행 대조).
            string v = r.Scaled ? "rank" : (r.Scale == MutationSpecs.RowScale.PerRankContext ? RankTimes : "") + r.Value(ChaosRules.Levels.MinLevel);
            return r.Component + "|" + (r.Stat ?? "") + "|" + (r.Descriptor ?? "") + "|" + v + "|" + (r.Extra ?? "");
        }

        static string Describe(BlueprintComponent c)
        {
            var sb = c as Kingmaker.UnitLogic.FactLogic.AddStatBonus;
            if (sb != null) return "AddStatBonus|" + sb.Stat + "|" + sb.Descriptor + "|" + sb.Value + "|";
            var cs = c as Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize;
            if (cs != null) return "ChangeUnitSize|||" + cs.SizeDelta + "|" + GetF(cs, "m_Type");
            var sv = c as Kingmaker.Designers.Mechanics.Buffs.BuffAllSavesBonus;
            if (sv != null) return "BuffAllSavesBonus||" + sv.Descriptor + "|" + sv.Value + "|";
            var dr = c as Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical;
            if (dr != null) return "AddDamageResistancePhysical|||" + (dr.Value.ValueType == E<ContextValueType>("Rank") ? "rank" : dr.Value.Value.ToString()) + "|";
            var fh = c as Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing;
            if (fh != null) return "AddEffectFastHealing|||" + (fh.Heal == 0 && fh.Bonus.ValueType == E<ContextValueType>("Rank") ? "rank" : fh.Heal + "+" + fh.Bonus.Value) + "|";
            var er = c as Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy;
            if (er != null) return "AddDamageResistanceEnergy|||" + er.Value.Value + "|" + er.Type;
            var al = c as Kingmaker.UnitLogic.FactLogic.AddAdditionalLimb;
            if (al != null) return "AddAdditionalLimb|||0|" + ((BlueprintItemWeaponReference)GetF(al, "m_Weapon")).Guid;
            var ae = c as Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect;
            if (ae != null) return "AddAreaEffect|||" + HorrorArea.Size.Value + "|" + ((BlueprintAbilityAreaEffectReference)GetF(ae, "m_AreaEffect")).Guid;
            var tr = c as Kingmaker.UnitLogic.Mechanics.Components.AddTargetAttackWithWeaponTrigger;
            if (tr != null)
            {
                var dmg = tr.ActionsOnAttacker.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>().ToArray();
                // DoNotPassAttackRoll(2026-11-16): 반사 피해 묶음에 공격자의 무기가 실리지 않아야 분노 중 자연 무기 속성(RageStrikeSpec 단계 버프)이 반사 피해에 번지지 않는다(TryRunActions @000C~0031).
                // 개편 O2: 주사위 개수의 값 종류(rank×n)를 줄에 적고, 보너스는 고정 0 이어야 한다(순위형 값이 한 칸에만 있다).
                string ok = tr.OnlyHit && tr.OnlyMelee && tr.DoNotPassAttackRoll && dmg.Length == 1 && dmg[0].DamageType.Type == E<Kingmaker.RuleSystem.Rules.Damage.DamageType>("Physical") && dmg[0].Value.DiceType == E<Kingmaker.RuleSystem.DiceType>("D" + ChaosRules.Dials.ThornsDiceSides)
                    && DescribeValue(dmg[0].Value.BonusValue) == "0" ? "" : "!";
                return "AddTargetAttackWithWeaponTrigger|||" + ok + (dmg.Length == 1 ? DescribeValue(dmg[0].Value.DiceCountValue) : "?") + "|" + (dmg.Length == 1 ? dmg[0].DamageType.Physical.Form.ToString() : "?");
            }
            var wd = c as Kingmaker.UnitLogic.Buffs.Components.WeaponAttackTypeDamageBonus;
            if (wd != null)
            {
                // 개편 O6: 곱수(AttackBonus)가 원장 값이 아니면 「!」 — 사양 줄과 반드시 달라진다. 값은 고정이어야 한다(DescribeValue: Simple = "<값>", 그 밖 = "?<종류>" 또는 "rank×<값>").
                string ok = wd.AttackBonus == ChaosRules.Dials.WeaponDamageBonusMultiplier ? "" : "!";
                return "WeaponAttackTypeDamageBonus||" + wd.Descriptor + "|" + ok + DescribeValue(wd.Value) + "|" + wd.Type;
            }
            var fa = c as Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions;
            if (fa != null)
            {
                var dmg = fa.NewRound.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>().ToArray();
                // 개편 O2: 피해(보너스)의 값 종류(rank×n)를 줄에 적고, 주사위는 없어야 한다(Zero · 개수 고정 0).
                string ok = dmg.Length == 1 && dmg[0].Value.DiceType == E<Kingmaker.RuleSystem.DiceType>("Zero") && DescribeValue(dmg[0].Value.DiceCountValue) == "0" ? "" : "!";
                return "NewRoundDamage|||" + ok + (dmg.Length == 1 ? DescribeValue(dmg[0].Value.BonusValue) : "?") + "|" + (dmg.Length == 1 ? dmg[0].DamageType.Type.ToString() : "?");
            }
            return c.GetType().Name + "|?";
        }

        // 부모 버프가 저장한 사실 인스턴스(Buff.m_StoredFacts, 비공개 — 열거 API 없음, T5 Step 0). 저장 칸 그대로(같은 인스턴스가 두 번 저장돼 있으면 두 칸). 필드가 없으면 GetF 가 던진다(조용히 빈 목록 아님).
        internal static List<EntityFact> StoredFacts(Buff parent)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            var list = (List<EntityFact>)GetF(parent, "m_StoredFacts");
            return list == null ? new List<EntityFact>() : list.ToList();   // 엔진은 첫 StoreFact 때 목록을 만든다(Buff.StoreFact @0023~0033) — null = 아직 저장 0개
        }

        // 개편 O3: 저장 사실마다 (블루프린트 GUID, 순위) 한 행 — MutationCore 가 변이별 「표식 순위 합」을 여기서 센다. 순위 = EntityFact.GetRank()(버프는 Buff.GetRank @0000~0006 = Buff.Rank).
        // 블루프린트 없는 저장 사실은 무엇의 순위인지 알 수 없다 → 건너뛰지 않고 던진다(ActionGuard 가 계수).
        public static List<ChildFact> ChildFacts(Buff parent)
        {
            var r = new List<ChildFact>();
            foreach (var f in StoredFacts(parent))
            {
                if (f == null || f.Blueprint == null) throw new InvalidOperationException("ChaosWarrior: " + parent.Blueprint.name + " stores a fact without a blueprint (cannot read its mutation ranks)");
                r.Add(new ChildFact(f.Blueprint.AssetGuid.ToString(), f.GetRank()));
            }
            return r;
        }

        // 그 인스턴스가 이미 부모의 저장 목록에 있는가(참조 동일성). 순위형 버프는 엔진이 기존 인스턴스를 돌려주므로(BuffCollection.PrepareFactForAttach @025F~0269) 두 번째부터는 참이다.
        internal static bool IsStoredBy(Buff parent, EntityFact fact)
        {
            if (fact == null) throw new ArgumentNullException("fact");
            return StoredFacts(parent).Any(f => ReferenceEquals(f, fact));
        }
    }
}
