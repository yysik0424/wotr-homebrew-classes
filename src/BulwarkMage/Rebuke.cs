using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
     
    //   자원(하루 max(1, 3 + 지능)) · 1레벨 자원 특성 · 배우기 선택(1·5·10·15·20) · 효과마다 배움 → 토글(그룹 WizardDivinationAura, 하나만) → 토글 버프
    //   (보조손 타워실드일 때만 문 특성) → 문(무지개빛 분출 패턴: 활성·새 라운드에 준비됨 버프) → 준비됨(근접 무기 명중 트리거 → A_i).
    // 만든 뒤 블루프린트마다 Describe(실제 객체를 되읽기 문법 줄로) == RebukePlan.ExpectedLines 를 대조한다 — 다르면 던진다(BuildFailed). 점검 S8 이 같은 대조를 점검 시점에 다시 돈다(VerifyNow).
    // 본편 블루프린트에는 쓰지 않는다(참조만 — 시험 exec: base-blueprint writes).
    public static class Rebuke
    {
        public static BlueprintAbilityResource Resource;
        public static BlueprintFeature BaseFeature;
        public static BlueprintFeatureSelection Selection;
        static readonly Dictionary<string, BlueprintScriptableObject> built = new Dictionary<string, BlueprintScriptableObject>();





        public static T Part<T>(RebukeRole role, string effectId) where T : BlueprintScriptableObject
        {
            BlueprintScriptableObject bp;
            string g = RebukePlan.GuidOf(role, effectId);
            if (!built.TryGetValue(g, out bp)) throw new InvalidOperationException("BulwarkMage: rebuke " + role + "/" + effectId + " not built");
            var t = bp as T;
            if (t == null) throw new InvalidCastException("BulwarkMage: rebuke " + role + "/" + effectId + " is " + bp.GetType().Name + ", expected " + typeof(T).Name);
            return t;
        }

        public static void Build()
        {
            if (built.Count != 0 || Resource != null) throw new InvalidOperationException("BulwarkMage: Rebuke.Build() called twice");
            // 재사용 본편(버프 3·주문 1·위저드 직업)이 캐시에 있는지 — 읽기만(없으면 Get 이 던진다).
            Get<BlueprintCharacterClass>(Guids.Game.WizardClass);
            foreach (var e in RebukeTable.Effects)
            {
                if (e.BuffGuid != null) Get<BlueprintBuff>(e.BuffGuid);
                if (e.CastSpellGuid != null) Get<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(e.CastSpellGuid);
            }
            foreach (var n in RebukePlan.Nodes) built.Add(n.Guid, Make(n));
            Resource = (BlueprintAbilityResource)built[Guids.RebukeResource];
            BaseFeature = (BlueprintFeature)built[Guids.RebukeBaseFeature];
            Selection = (BlueprintFeatureSelection)built[Guids.RebukeSelection];
            Archetype.AddAt(1, BaseFeature);
            var levels = BulwarkRules.Levels.RebukeLearnLevels;
            if (levels == null || levels.Count == 0) throw new InvalidOperationException("BulwarkMage: RebukeLearnLevels is empty");
            foreach (int l in levels) Archetype.AddAt(l, Selection);
            var diffs = VerifyNow();
            if (diffs.Count != 0) throw new InvalidOperationException("BulwarkMage: rebuke blueprint read-back differs from RebukePlan: " + string.Join(" | ", diffs.Take(8).ToArray()));
        }

        // 38개 전부: 실제 블루프린트 → 줄 == 원장 기대 줄. 차이 목록(빈 목록 = 일치). 빌드 시 + 점검 S8 toggle-per-learned-effect.
        public static List<string> VerifyNow()
        {
            var d = new List<string>();
            if (built.Count != RebukePlan.Nodes.Count) d.Add("built " + built.Count + " of " + RebukePlan.Nodes.Count);
            foreach (var n in RebukePlan.Nodes)
            {
                BlueprintScriptableObject bp;
                if (!built.TryGetValue(n.Guid, out bp)) { d.Add(n + " missing"); continue; }
                if (bp.AssetGuid.ToString() != n.Guid || bp.name != n.Name) d.Add(n + " guid/name readback " + bp.AssetGuid + " " + bp.name);
                foreach (var x in RebukePlan.Diff(RebukePlan.ExpectedLines(n), Describe(bp), 3)) d.Add(n.Name + " " + x);
            }
            // 타워실드 군 플래그는 본편 TSS 타워실드 훈련과 같은 값(TowerShield 이름이 같은 비트를 뜻하는지 교차 확인).
            var tss = Get<BlueprintFeature>(Guids.Game.TowerShieldTraining).ComponentsArray.OfType<HasShieldFeatureUnlock>().ToArray();
            foreach (var e in RebukeTable.Effects)
            {
                var ours = Part<BlueprintBuff>(RebukeRole.ToggleBuff, e.Id).ComponentsArray.OfType<HasShieldFeatureUnlock>().ToArray();
                if (tss.Length != 1 || ours.Length != 1 || !Equals(GetF(ours[0], "m_ShiledProficiencyGroupEntries"), GetF(tss[0], "m_ShiledProficiencyGroupEntries")))
                    d.Add(e.Id + " toggle buff shield group flag != vanilla TowerShieldTraining's");
                // FX 미리 읽기 목록 = FxTable 의 그 효과 AssetId(없으면 빈 목록) — 원장과 실제 블루프린트 대조(C4).
                var fxs = FxTable.ForRebuke(e.Id);
                var want = fxs == null ? new string[0] : new[] { fxs.AssetId };
                var got = Part<BlueprintBuff>(RebukeRole.Ready, e.Id).ResourceAssetIds;
                if (got == null || !got.SequenceEqual(want)) d.Add(e.Id + " ready buff ResourceAssetIds [" + (got == null ? "null" : string.Join(",", got)) + "] != FxTable [" + string.Join(",", want) + "]");
            }
            return d;
        }

        // ---------------- 만들기 ----------------
        static BlueprintScriptableObject Make(RebukeNode n)
        {
            var spec = n.EffectId == null ? null : RebukeTable.Get(n.EffectId);
            string nameKey = spec == null ? "rebuke.name" : spec.NameKey, descKey = spec == null ? "rebuke.desc" : spec.DescKey;
            switch (n.Role)
            {
                case RebukeRole.Resource:
                    return Create<BlueprintAbilityResource>(n.Name, n.Guid, r =>
                    {
                        r.LocalizedName = Strings.L(nameKey);
                        r.LocalizedDescription = Strings.L(descKey);
                        object amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                        FillEmpty(amount);
                        SetField(amount, "BaseValue", BulwarkRules.Dials.RebukeDailyBase);
                        SetField(amount, "IncreasedByStat", true);
                        SetField(amount, "ResourceBonusStat", StatType.Intelligence);
                        SetField(r, "m_MaxAmount", amount);
                        SetField(r, "m_UseMax", false);
                        SetField(r, "m_Min", BulwarkRules.Dials.RebukeDailyMin);
                    });
                case RebukeRole.BaseFeature:
                    {
                        var f = NewFeature(n, nameKey, descKey, false);
                        var c = Comp(new AddAbilityResources { RestoreAmount = true, UseThisAsResource = false, Amount = 0, RestoreOnLevelUp = false });
                        SetField(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.RebukeResource));
                         
                        var spentClear = Comp(new AddFactContextActions
                        {
                            Activated = List(),
                            Deactivated = List(RemoveBuff(Guids.RebukeSpent, false)),
                            NewRound = List(RemoveBuff(Guids.RebukeSpent, false)),
                            Dispose = List(),
                        });
                        AddComponents(f, c, spentClear);
                        return f;
                    }
                case RebukeRole.Spent:
                    return NewBuff(n, nameKey, descKey, true);
                case RebukeRole.Selection:
                    return Create<BlueprintFeatureSelection>(n.Name, n.Guid, s =>
                    {
                        SetField(s, "m_DisplayName", Strings.L("rebuke.selection"));
                        SetField(s, "m_Description", Strings.L(StringsTable.SelectionDescKey));   // 선택 화면 설명(무엇을 언제 몇 개 고르는가 + 공통 규칙 + DC 식) — 2026-11-17
                        SetField(s, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                        FeatureFlags(s, false);
                        s.Mode = SelectionMode.Default;
                        s.Group = FeatureGroup.None;
                        s.Group2 = FeatureGroup.None;
                        s.IgnorePrerequisites = false;
                        s.Obligatory = false;
                        SetField(s, "m_AllFeatures", n.References.Select(Ref<BlueprintFeatureReference>).ToArray());
                        SetField(s, "m_Features", new BlueprintFeatureReference[0]);
                        FillEmpty(s);
                    });
                case RebukeRole.Learn:
                    {
                        var f = NewFeature(n, nameKey, StringsTable.ChoiceKey(n.EffectId), false);   // 선택지 설명은 여러 줄짜리 전용 키(토글·버프 툴팁은 짧은 descKey 그대로) — 2026-11-17
                        var c = Comp(new AddFacts());
                        SetField(c, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(RebukePlan.GuidOf(RebukeRole.Toggle, n.EffectId)) });
                        AddComponents(f, c);
                        return f;
                    }
                case RebukeRole.Toggle:
                    {
                        var t = Create<BlueprintActivatableAbility>(n.Name, n.Guid, a =>
                        {
                            SetField(a, "m_DisplayName", Strings.L(nameKey));
                            SetField(a, "m_Description", Strings.L(descKey));
                            SetField(a, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                            SetField(a, "m_Buff", Ref<BlueprintBuffReference>(RebukePlan.GuidOf(RebukeRole.ToggleBuff, n.EffectId)));
                            a.Group = ActivatableAbilityGroup.WizardDivinationAura;
                            a.WeightInGroup = BulwarkRules.Dials.RebukeToggleWeight;
                            a.ActivationType = AbilityActivationType.Immediately;
                            SetField(a, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
                            a.DeactivateImmediately = false;   // 계획 I04: 사용 0 에서 자기 공격(OnAttack @0037~0056)으로 꺼지지 않게
                            a.DoNotTurnOffOnRest = true;
                            a.IsOnByDefault = false;
                            a.DeactivateIfCombatEnded = false;
                            a.DeactivateAfterFirstRound = false;
                            a.DeactivateIfOwnerDisabled = false;
                            a.DeactivateIfOwnerUnconscious = false;
                            a.OnlyInCombat = false;
                            a.ResourceAssetIds = new string[0];
                            FillEmpty(a);
                        });
                        var c = Comp(new ActivatableAbilityResourceLogic { SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.Never });
                        SetField(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.RebukeResource));
                        AddComponents(t, c);
                        return t;
                    }
                case RebukeRole.ToggleBuff:
                    {
                        var b = NewBuff(n, nameKey, descKey, false);
                        var c = Comp(new HasShieldFeatureUnlock());
                        SetField(c, "FilterByShieldProficiencyGroup", true);
                        SetField(c, "m_ShiledProficiencyGroupEntries", ArmorProficiencyGroupFlag.TowerShield);
                        SetField(c, "FilterByBlueprintShieldTypes", false);
                        SetField(c, "m_DisableWhenHasArmor", false);
                        SetField(c, "m_NewFact", Ref<BlueprintUnitFactReference>(RebukePlan.GuidOf(RebukeRole.Gate, n.EffectId)));
                        AddComponents(b, c);
                        return b;
                    }
                case RebukeRole.Gate:
                    {
                        var f = NewFeature(n, nameKey, descKey, true);
                        string ready = RebukePlan.GuidOf(RebukeRole.Ready, n.EffectId);
                        var hasNot = Cond(new ContextConditionHasBuff { Not = true });
                        SetField(hasNot, "m_Buff", Ref<BlueprintBuffReference>(ready));
                        var c = Comp(new AddFactContextActions
                        {
                            Activated = List(ApplyBuff(ready, true, NoDuration(), true)),
                            Deactivated = List(RemoveBuff(ready, false)),
                            NewRound = List(If(hasNot, List(ApplyBuff(ready, true, NoDuration(), true)), List())),
                            Dispose = List(),
                        });
                        AddComponents(f, c);
                        return f;
                    }
                case RebukeRole.Ready:
                    {
                        var b = NewBuff(n, nameKey, descKey, true);
                        b.ResourceAssetIds = RebukeFx.ResourceIds(n.EffectId);   // FX 미리 읽기(본편 관례) — FX 없는 효과 = []
                        var calc =Comp(new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Intelligence, UseKineticistMainStat = false });
                        SetField(calc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.WizardClass));
                        var comps = new List<BlueprintComponent> { calc };
                        var rank = RebukePlan.RankFunction(spec);
                        if (rank != null) comps.Add(RankConfig(rank));
                        var trig = Comp(new AddTargetAttackWithWeaponTrigger
                        {
                            TriggerBeforeAttack = false, WaitForAttackResolve = true, OnlyHit = true, CriticalHit = false, OnlyOnFirstAttack = false, OnAttackOfOpportunity = false,
                            OnlyMelee = true, OnlyRanged = false, NotReach = false, OnlySneakAttack = false, NotSneakAttack = false, CheckCategory = false,
                            DoNotPassAttackRoll = true,   // 계획 I06: 공격자의 명중 굴림(치명)을 반격 피해 문맥에 넘기지 않는다(IgnoreCritical 과 이중)
                            Not = false, Categories = new WeaponCategory[0],
                            ActionsOnAttacker = AttackerActions(spec),
                            ActionOnSelf = List(),
                        });
                        comps.Add(trig);
                        AddComponents(b, comps.ToArray());
                        return b;
                    }
            }
            throw new ArgumentOutOfRangeException("n", n.Role, "BulwarkMage: unknown rebuke role");
        }

        static BlueprintFeature NewFeature(RebukeNode n, string nameKey, string descKey, bool hidden)
        {
            return Create<BlueprintFeature>(n.Name, n.Guid, f =>
            {
                SetField(f, "m_DisplayName", Strings.L(nameKey));
                SetField(f, "m_Description", Strings.L(descKey));
                SetField(f, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                FeatureFlags(f, hidden);
                FillEmpty(f);
            });
        }

        static void FeatureFlags(BlueprintFeature f, bool hidden)
        {
            f.Ranks = 1;
            f.IsClassFeature = true;
            f.HideInUI = hidden;
            f.Groups = new FeatureGroup[0];
            f.ReapplyOnLevelUp = false;
        }

        static BlueprintBuff NewBuff(RebukeNode n, string nameKey, string descKey, bool hidden)
        {
            return Create<BlueprintBuff>(n.Name, n.Guid, b =>
            {
                SetField(b, "m_DisplayName", Strings.L(nameKey));
                SetField(b, "m_Description", Strings.L(descKey));
                SetField(b, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                b.Stacking = StackingType.Replace;
                var flagsType = FindField(typeof(BlueprintBuff), "m_Flags").FieldType;
                SetField(b, "m_Flags", hidden ? Enum.Parse(flagsType, "HiddenInUi") : Enum.ToObject(flagsType, 0));
                b.IsClassFeature = true;
                FillEmpty(b);
                EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — FillEmpty 는 PrefabLink 를 채우지 않는다)
            });
        }

        // A_i(RebukePlan.AttackerActionLines 와 같은 모양): [ 자원 조건 ∧ 표지 없음 → [준비됨 제거, 하루 1 소모, 표지 붙임, 생존 ? 관측(효과) : 불발 기록], 아니면 [] ].
        // 표지(A1) = 본편 부품만: ContextConditionCasterHasFact{Not} + ContextActionApplyBuff{ToCaster, 영구} + 자원 특성 NewRound 의 RemoveBuff.
        static ActionList AttackerActions(RebukeSpec s)
        {
            string ready = RebukePlan.GuidOf(RebukeRole.Ready, s.Id);
            var has = Cond(new ContextConditionCasterHasResource { Not = false, Amount = BulwarkRules.Dials.RebukeUsesPerFiring });
            has.m_Resource = Ref<BlueprintAbilityResourceReference>(Guids.RebukeResource);
            var notSpent = Cond(new ContextConditionCasterHasFact { Not = true });
            SetField(notSpent, "m_Fact", Ref<BlueprintUnitFactReference>(Guids.RebukeSpent));
            var spend = Act(new ContextSpendResource { ContextValueSpendure = false, Value = Cv(BulwarkRules.Dials.RebukeUsesPerFiring) });
            SetField(spend, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.RebukeResource));
            var mark = ApplyBuff(Guids.RebukeSpent, true, NoDuration(), false, true);
            var alive = Cond(new ContextConditionAlive { Not = false });
            var dud = Act(new ContextActionLogRebukeDud { Reason = DudReason.AttackerDown, EffectId = s.Id });
            return List(If(new Condition[] { has, notSpent }, List(RemoveBuff(ready, true), spend, mark, If(alive, Observed(s), List(dud))), List()));
        }

        // 생존 분기(A3·A4 판정 2026-11-14): 효과 묶음을 관측 행동 하나로 감싼다. E7 은 앞에 대상 사전 차단(거짓 = 불발 기록 Untargetable, 본편 시전 오류 로그 없음).
        static ActionList Observed(RebukeSpec s)
        {
            var obs = Act(new ContextActionRebukeEffect { EffectId = s.Id, Actions = WithFx(s, Effect(s)) });
            if (s.CastSpellGuid == null) return List(obs);
            var can = Cond(new ContextConditionRebukeCanTarget { Not = false });
            can.m_Spell = Ref<BlueprintAbilityReference>(s.CastSpellGuid);
            var untargetable = Act(new ContextActionLogRebukeDud { Reason = DudReason.Untargetable, EffectId = s.Id });
            return List(If(can, List(obs), List(untargetable)));
        }

        // FX(2026-11-15 승인): 효과 목록 끝에 공격자(문맥 대상) 위 1회 ContextActionSpawnFx — FxTable 원장, 원본 대조는 RebukeFx.Resolve(어긋나면 던진다).
        // 끝에 붙인다: 기존 효과 행동의 위치([0]·[1])는 그대로(RebukePlan.InnerEffectLines 가 같은 규칙으로 기대 줄을 만든다).
        static ActionList WithFx(RebukeSpec s, ActionList effect)
        {
            var fx = RebukeFx.SpawnAction(s.Id);
            return fx == null ? effect : List(effect.Actions.Concat(new GameAction[] { fx }).ToArray());
        }

        static ActionList Effect(RebukeSpec s)
        {
            switch (s.Id)
            {
                case "E1": return List(Damage(s, false));
                case "E2": return List(Save(SavingThrowType.Reflex, List(Damage(s, true))));
                case "E3": return List(Damage(s, false), BuffOnFailedSave(s, SavingThrowType.Fortitude));
                case "E4":
                    if (s.PushEngineDistance == null) throw new InvalidOperationException("BulwarkMage: rebuke E4 without a push distance");
                    return List(Act(new ContextActionPush { Distance = Cv(s.PushEngineDistance.Value), ProvokeAttackOfOpportunity = false }));  
                case "E5": case "E6": return List(BuffOnFailedSave(s, SavingThrowType.Will));
                case "E7":
                    {
                        var cast = Act(new ContextActionCastSpell { OverrideDC = false, DC = Cv(0), OverrideSpellLevel = false, SpellLevel = Cv(0), CastByTarget = false, MarkAsChild = false, LogIfCanNotTarget = false });
                        SetField(cast, "m_Spell", Ref<BlueprintAbilityReference>(s.CastSpellGuid));
                        return List(cast);
                    }
            }
            throw new InvalidOperationException("BulwarkMage: rebuke effect " + s.Id + " has no builder");
        }

        static GameAction Damage(RebukeSpec s, bool halfIfSaved)
        {
            var dt = new DamageTypeDescription();
            if (s.Energy == RebukeEnergy.Force) dt.Type = DamageType.Force;
            else if (s.Energy == RebukeEnergy.Electricity) { dt.Type = DamageType.Energy; dt.Energy = DamageEnergyType.Electricity; }
            else if (s.Energy == RebukeEnergy.Cold) { dt.Type = DamageType.Energy; dt.Energy = DamageEnergyType.Cold; }
            else throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " energy " + s.Energy + " has no engine damage type");
            DiceType die;
            if (s.Die == RebukeDie.D6) die = DiceType.D6;
            else if (s.Die == RebukeDie.D4) die = DiceType.D4;
            else throw new InvalidOperationException("BulwarkMage: rebuke " + s.Id + " die " + s.Die + " has no engine dice type");
            var a = Act(new ContextActionDealDamage
            {
                DamageType = dt,
                Duration = NoDuration(),
                Value = new ContextDiceValue { DiceType = die, DiceCountValue = CvRank(), BonusValue = Cv(0) },
                Half = false,
                HalfIfSaved = halfIfSaved,
                IgnoreCritical = true,   // 계획 I06: 치명 배율 미적용(GetDamageInfo @0069 — 주사위 자체는 그대로, T5 Step 0 #15)
            });
            SetField(a, "m_Type", Enum.Parse(FindField(typeof(ContextActionDealDamage), "m_Type").FieldType, "Damage"));
            return a;
        }

        static ContextActionSavingThrow Save(SavingThrowType type, ActionList actions)
        {
            var st = Act(new ContextActionSavingThrow { Type = type, HasCustomDC = false, UseDCFromContextSavingThrow = false, FromBuff = false, CustomDC = Cv(0), Actions = actions });
            return st;
        }

        static GameAction BuffOnFailedSave(RebukeSpec s, SavingThrowType type)
        {
            int? fixedRounds = RebukePlan.ConstantRounds(s);
            var duration = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = Cv(0), BonusValue = fixedRounds.HasValue ? Cv(fixedRounds.Value) : CvRank() };
            // 적에게 거는 버프: AsChild = false 명시(계획 I07 — 생성자 기본 true, 부모(중장법사의 준비됨) ≠ 소유자(적)면 RunAction @00FE~0142 오류).
            var saved = Act(new ContextActionConditionalSaved { Succeed = List(), Failed = List(ApplyBuff(s.BuffGuid, false, duration, false)) });
            return Save(type, List(saved));
        }

        static ContextRankConfig RankConfig(Func<int, int> f)
        {
            var c = Comp(new ContextRankConfig());
            SetField(c, "m_Type", AbilityRankType.Default);
            SetField(c, "m_BaseValueType", ContextRankBaseValueType.ClassLevel);
            SetField(c, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(Guids.Game.WizardClass) });
            SetField(c, "m_ExceptClasses", false);
            SetField(c, "m_Progression", ContextRankProgression.Custom);
            var rows = RebukeTable.CustomProgression(f);
            var itemType = FindField(typeof(ContextRankConfig), "m_CustomProgression").FieldType.GetElementType();
            var items = Array.CreateInstance(itemType, rows.Length);
            for (int i = 0; i < rows.Length; i++) { var it = Activator.CreateInstance(itemType); SetField(it, "BaseValue", rows[i].baseValue); SetField(it, "ProgressionValue", rows[i].progressionValue); items.SetValue(it, i); }
            SetField(c, "m_CustomProgression", items);
            SetField(c, "m_UseMin", false);
            SetField(c, "m_UseMax", false);
            return c;
        }

        static ContextActionApplyBuff ApplyBuff(string buff, bool permanent, ContextDurationValue duration, bool asChild, bool toCaster = false)
        {
            var a = Act(new ContextActionApplyBuff { Permanent = permanent, UseDurationSeconds = false, DurationValue = duration, IsFromSpell = false, IsNotDispelable = false, ToCaster = toCaster, AsChild = asChild, SameDuration = false });
            SetField(a, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return a;
        }

        static ContextActionRemoveBuff RemoveBuff(string buff, bool toCaster)
        {
            var a = Act(new ContextActionRemoveBuff { ToCaster = toCaster, RemoveRank = false, OnlyFromCaster = false });
            SetField(a, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return a;
        }

        static Conditional If(Condition c, ActionList t, ActionList f) { return If(new[] { c }, t, f); }

        static Conditional If(Condition[] cs, ActionList t, ActionList f)
        {
            return Act(new Conditional { ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = cs }, IfTrue = t, IfFalse = f });
        }

        static ContextDurationValue NoDuration() { return new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = Cv(0), BonusValue = Cv(0) }; }
        static ContextValue Cv(int v) { return new ContextValue { ValueType = ContextValueType.Simple, Value = v }; }
        static ContextValue CvRank() { return new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default }; }
        static ActionList List(params GameAction[] a) { return new ActionList { Actions = a }; }
        static T Act<T>(T a) where T : GameAction { FillEmpty(a); return a; }
        static T Cond<T>(T c) where T : Condition { FillEmpty(c); return c; }
        static T Comp<T>(T c) where T : BlueprintComponent { FillEmpty(c); return c; }

        // 새 객체의 null 컬렉션·참조 필드를 빈 값으로(직렬화본은 [] / 빈 참조를 갖는다 — 엔진이 null 을 순회하다 죽지 않게). 이미 값이 있는 필드는 건드리지 않는다.
        internal static void FillEmpty(object o)
        {
            for (var t = o.GetType(); t != null && t != typeof(object); t = t.BaseType)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.IsInitOnly || f.GetValue(o) != null) continue;
                    var ft = f.FieldType;
                    if (ft.IsArray) f.SetValue(o, Array.CreateInstance(ft.GetElementType(), 0));
                    else if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>)) f.SetValue(o, Activator.CreateInstance(ft));
                    else if (typeof(BlueprintReferenceBase).IsAssignableFrom(ft) && !ft.IsAbstract) f.SetValue(o, Activator.CreateInstance(ft));
                }
        }

        // ---------------- 되읽기(Describe) — RebukePlan 의 줄 문법 ----------------
        public static List<string> Describe(BlueprintScriptableObject bp)
        {
            var l = new List<string> { "type=" + bp.GetType().Name };
            var res = bp as BlueprintAbilityResource;
            if (res != null)
            {
                object a = GetF(res, "m_MaxAmount");
                l.Add("m_MaxAmount.BaseValue=" + V(GetF(a, "BaseValue")));
                l.Add("m_MaxAmount.IncreasedByLevel=" + V(GetF(a, "IncreasedByLevel")));
                l.Add("m_MaxAmount.IncreasedByLevelStartPlusDivStep=" + V(GetF(a, "IncreasedByLevelStartPlusDivStep")));
                l.Add("m_MaxAmount.IncreasedByStat=" + V(GetF(a, "IncreasedByStat")));
                l.Add("m_MaxAmount.ResourceBonusStat=" + V(GetF(a, "ResourceBonusStat")));
                l.Add("m_UseMax=" + V(GetF(res, "m_UseMax")));
                l.Add("m_Min=" + V(GetF(res, "m_Min")));
            }
            var feat = bp as BlueprintFeature;
            if (feat != null)
            {
                l.Add("Ranks=" + V(feat.Ranks));
                l.Add("IsClassFeature=" + V(feat.IsClassFeature));
                l.Add("HideInUI=" + V(feat.HideInUI));
                l.Add("Groups=" + string.Join(",", (feat.Groups ?? new FeatureGroup[0]).Select(g => g.ToString()).ToArray()));
            }
            var sel = bp as BlueprintFeatureSelection;
            if (sel != null)
            {
                l.Add("Mode=" + V(sel.Mode));
                l.Add("Group=" + V(sel.Group));
                l.Add("IgnorePrerequisites=" + V(sel.IgnorePrerequisites));
                l.Add("Obligatory=" + V(sel.Obligatory));
                l.Add("m_AllFeatures=" + V(GetF(sel, "m_AllFeatures")));
                l.Add("m_Features=" + V(GetF(sel, "m_Features")));
            }
            var act = bp as BlueprintActivatableAbility;
            if (act != null)
            {
                l.Add("m_Buff=" + V(GetF(act, "m_Buff")));
                foreach (var f in new[] { "Group", "WeightInGroup", "ActivationType", "m_ActivateWithUnitCommand", "DeactivateImmediately", "DoNotTurnOffOnRest", "IsOnByDefault", "DeactivateIfCombatEnded", "DeactivateAfterFirstRound", "DeactivateIfOwnerDisabled", "DeactivateIfOwnerUnconscious", "OnlyInCombat" })
                    l.Add(f + "=" + V(GetF(act, f)));
            }
            var buff = bp as BlueprintBuff;
            if (buff != null)
            {
                l.Add("Stacking=" + V(buff.Stacking));
                l.Add("IsHiddenInUI=" + V(buff.IsHiddenInUI));
            }
            var comps = bp.ComponentsArray ?? new BlueprintComponent[0];
            l.Add("components=" + string.Join(",", comps.Select(c => c == null ? "null" : c.GetType().Name).ToArray()));
            for (int i = 0; i < comps.Length; i++) DescribeComponent(l, "c" + i.ToString(CultureInfo.InvariantCulture), comps[i]);
            return l;
        }

        static void Fields(List<string> l, string p, object o, params string[] names) { foreach (var n in names) l.Add(p + "." + n + "=" + V(GetF(o, n))); }

        static void DescribeComponent(List<string> l, string p, BlueprintComponent c)
        {
            if (c == null) return;
            if (c is AddAbilityResources) Fields(l, p, c, "m_Resource", "RestoreAmount", "UseThisAsResource", "Amount", "RestoreOnLevelUp");
            else if (c is AddFacts) Fields(l, p, c, "m_Facts");
            else if (c is ActivatableAbilityResourceLogic) Fields(l, p, c, "m_RequiredResource", "SpendType");
            else if (c is HasShieldFeatureUnlock) Fields(l, p, c, "FilterByShieldProficiencyGroup", "m_ShiledProficiencyGroupEntries", "FilterByBlueprintShieldTypes", "m_DisableWhenHasArmor", "m_NewFact");
            else if (c is AddFactContextActions)
            {
                var a = (AddFactContextActions)c;
                DescribeList(l, p + ".Activated", a.Activated);
                DescribeList(l, p + ".Deactivated", a.Deactivated);
                DescribeList(l, p + ".NewRound", a.NewRound);
                DescribeList(l, p + ".Dispose", a.Dispose);
            }
            else if (c is ContextCalculateAbilityParamsBasedOnClass) Fields(l, p, c, "m_CharacterClass", "StatType", "UseKineticistMainStat");
            else if (c is ContextRankConfig) Fields(l, p, c, "m_Type", "m_BaseValueType", "m_Class", "m_ExceptClasses", "m_Progression", "m_CustomProgression", "m_UseMin", "m_UseMax");
            else if (c is AddTargetAttackWithWeaponTrigger)
            {
                var t = (AddTargetAttackWithWeaponTrigger)c;
                Fields(l, p, c, "TriggerBeforeAttack", "WaitForAttackResolve", "OnlyHit", "CriticalHit", "OnlyOnFirstAttack", "OnAttackOfOpportunity", "OnlyMelee", "OnlyRanged", "NotReach", "OnlySneakAttack", "NotSneakAttack", "CheckCategory", "DoNotPassAttackRoll", "Not");
                DescribeList(l, p + ".ActionsOnAttacker", t.ActionsOnAttacker);
                DescribeList(l, p + ".ActionOnSelf", t.ActionOnSelf);
            }
            else l.Add(p + ".unknown=" + c.GetType().FullName);
        }

        static void DescribeList(List<string> l, string p, ActionList al)
        {
            var acts = al == null || al.Actions == null ? null : al.Actions;
            if (acts == null) { l.Add(p + "#=null"); return; }
            l.Add(p + "#=" + acts.Length.ToString(CultureInfo.InvariantCulture));
            for (int k = 0; k < acts.Length; k++) DescribeAction(l, p + "[" + k.ToString(CultureInfo.InvariantCulture) + "]", acts[k]);
        }

        static void DescribeAction(List<string> l, string p, GameAction a)
        {
            if (a == null) { l.Add(p + "=null"); return; }
            l.Add(p + "=" + a.GetType().Name);
            if (a is Conditional)
            {
                var c = (Conditional)a;
                var cc = c.ConditionsChecker;
                l.Add(p + ".ConditionsChecker.Operation=" + (cc == null ? "null" : V(cc.Operation)));
                var conds = cc == null || cc.Conditions == null ? new Condition[0] : cc.Conditions;
                l.Add(p + ".ConditionsChecker.Conditions#=" + conds.Length.ToString(CultureInfo.InvariantCulture));
                for (int k = 0; k < conds.Length; k++) DescribeCondition(l, p + ".ConditionsChecker.Conditions[" + k.ToString(CultureInfo.InvariantCulture) + "]", conds[k]);
                DescribeList(l, p + ".IfTrue", c.IfTrue);
                DescribeList(l, p + ".IfFalse", c.IfFalse);
            }
            else if (a is ContextActionApplyBuff) Fields(l, p, a, "m_Buff", "Permanent", "UseDurationSeconds", "DurationValue", "IsFromSpell", "IsNotDispelable", "ToCaster", "AsChild", "SameDuration");
            else if (a is ContextActionRemoveBuff) Fields(l, p, a, "m_Buff", "ToCaster", "RemoveRank", "OnlyFromCaster");
            else if (a is ContextSpendResource) Fields(l, p, a, "m_Resource", "ContextValueSpendure", "Value");
            else if (a is ContextActionDealDamage)
            {
                var d = (ContextActionDealDamage)a;
                l.Add(p + ".m_Type=" + V(GetF(d, "m_Type")));
                l.Add(p + ".DamageType.Type=" + (d.DamageType == null ? "null" : V(d.DamageType.Type)));
                if (d.DamageType != null && d.DamageType.Type == DamageType.Energy) l.Add(p + ".DamageType.Energy=" + V(d.DamageType.Energy));
                Fields(l, p, a, "Value", "Half", "HalfIfSaved", "IgnoreCritical");
            }
            else if (a is ContextActionSavingThrow)
            {
                Fields(l, p, a, "Type", "HasCustomDC", "UseDCFromContextSavingThrow", "FromBuff");
                DescribeList(l, p + ".Actions", ((ContextActionSavingThrow)a).Actions);
            }
            else if (a is ContextActionConditionalSaved)
            {
                DescribeList(l, p + ".Succeed", ((ContextActionConditionalSaved)a).Succeed);
                DescribeList(l, p + ".Failed", ((ContextActionConditionalSaved)a).Failed);
            }
            else if (a is ContextActionPush) Fields(l, p, a, "Distance", "ProvokeAttackOfOpportunity");
            else if (a is ContextActionCastSpell) Fields(l, p, a, "m_Spell", "OverrideDC", "OverrideSpellLevel", "CastByTarget", "MarkAsChild", "LogIfCanNotTarget");
            else if (a is ContextActionLogRebukeDud) Fields(l, p, a, "Reason", "EffectId");
            else if (a is ContextActionSpawnFx)
            {
                var link = ((ContextActionSpawnFx)a).PrefabLink;
                l.Add(p + ".PrefabLink.AssetId=" + (link == null ? "null" : string.IsNullOrEmpty(link.AssetId) ? "<empty>" : link.AssetId));
            }
            else if (a is ContextActionRebukeEffect)
            {
                Fields(l, p, a, "EffectId");
                DescribeList(l, p + ".Actions", ((ContextActionRebukeEffect)a).Actions);
            }
            else l.Add(p + ".unknown=" + a.GetType().FullName);
        }

        static void DescribeCondition(List<string> l, string p, Condition c)
        {
            if (c == null) { l.Add(p + "=null"); return; }
            l.Add(p + "=" + c.GetType().Name);
            l.Add(p + ".Not=" + V(c.Not));
            if (c is ContextConditionHasBuff) Fields(l, p, c, "m_Buff");
            else if (c is ContextConditionCasterHasResource) Fields(l, p, c, "m_Resource", "Amount");
            else if (c is ContextConditionAlive) { }
            else if (c is ContextConditionCasterHasFact) Fields(l, p, c, "m_Fact");
            else if (c is ContextConditionRebukeCanTarget) Fields(l, p, c, "m_Spell");
            else l.Add(p + ".unknown=" + c.GetType().FullName);
        }

        // 값 → 줄 문자열(RebukePlan 의 값 문법).
        static string V(object o)
        {
            if (o == null) return "null";
            if (o is bool) return (bool)o ? "True" : "False";
            if (o is int) return ((int)o).ToString(CultureInfo.InvariantCulture);
            if (o is string) return (string)o;
            if (o is Enum) return o.ToString();
            var r = o as BlueprintReferenceBase;
            if (r != null) return r.Guid.ToString();
            var cv = o as ContextValue;
            if (cv != null)
            {
                if (cv.ValueType == ContextValueType.Simple) return RebukePlan.Simple(cv.Value);
                if (cv.ValueType == ContextValueType.Rank) return "Rank:" + cv.ValueRank;
                return cv.ValueType + ":?";
            }
            var dv = o as ContextDiceValue;
            if (dv != null) return RebukePlan.Dice(V(dv.DiceType), V(dv.DiceCountValue), V(dv.BonusValue));
            var du = o as ContextDurationValue;
            if (du != null) return RebukePlan.Duration(V(du.Rate), V(du.DiceType), V(du.DiceCountValue), V(du.BonusValue));
            if (o is Array && o.GetType().GetElementType().Name == "CustomProgressionItem")
                return RebukePlan.Progression(((Array)o).Cast<object>().Select(x => ((int)GetF(x, "BaseValue"), (int)GetF(x, "ProgressionValue"))).ToArray());
            var arr = o as IEnumerable;
            if (arr != null) return string.Join(",", arr.Cast<object>().Select(V).ToArray());
            return o.GetType().Name + "?";
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("BulwarkMage: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
