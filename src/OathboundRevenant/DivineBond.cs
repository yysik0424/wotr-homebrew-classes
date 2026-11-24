using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using UnityEngine;
using WotrClassMod.Common;
using static OathboundRevenant.Parts;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    //   (가) 탈것: RevenantUndeadMount = AddFeatureIfHasFact{PaladinDivineMountProgression → Inner}, Inner = AddFeatureToPet{AnimalCompanion, UndeadType} + AddFeatureToPet{…, RevenantMountHp}(본편 UndeadMountFeature 선례).
    //   (나) 무기: RevenantWeaponBond = AddFeatureIfHasFact{WeaponBondFeature → Choices}, Choices = AddFacts[선택지 4] — 선택지 = 본편 WeaponBondFlamingChoice 복제(활성 능력 명시 예외 1).
    public static class DivineBond
    {
        public static BlueprintFeature MountFeature, MountInner, MountHp, WeaponFeature, WeaponChoices;
        public static readonly Dictionary<string, BlueprintActivatableAbility> Choices = new Dictionary<string, BlueprintActivatableAbility>();
        public static readonly Dictionary<string, BlueprintItemEnchantment> EnchantBps = new Dictionary<string, BlueprintItemEnchantment>();
        public static bool Built { get; private set; }

        static bool KeepShared(object v) { return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent; }

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("OathboundRevenant: DivineBond.Build() called twice");
            // (가) 탈것.
            MountHp = NewFeature("OathboundRevenantMountHp", Guids.RevenantMountHp, "mounthp.name", "mounthp.desc", true);
            AddComponents(MountHp, Comp(new MountChaHpComponent()));
            MountInner = NewFeature("OathboundRevenantUndeadMountInner", Guids.RevenantUndeadMountInner, "revenantundeadmount.name", "revenantundeadmount.desc", true);
            AddComponents(MountInner, ToPet(Guids.Game.UndeadType), ToPet(Guids.RevenantMountHp));
            MountFeature = Archetype.Shell("RevenantUndeadMount");
            AddComponents(MountFeature, IfHas(BondPlan.Rows.Single(r => r.Key == "RevenantUndeadMount")));
            Archetype.MarkFilled("RevenantUndeadMount");

            // (나) 무기 인챈트·선택지.
            BuildEnchants();
            var flaming = Get<BlueprintActivatableAbility>(Guids.Game.WeaponBondFlamingChoice);
            var flamingBuff = Get<BlueprintBuff>(Guids.Game.WeaponBondFlamingBuff);
            var abp = flamingBuff.ComponentsArray.OfType<AddBondProperty>().SingleOrDefault();
            if (abp == null) throw new InvalidOperationException("OathboundRevenant: vanilla WeaponBondFlamingBuff has no single AddBondProperty");
            var choiceGuids = new List<string>();
            foreach (var e in BondPlan.Enchants)
            {
                var ids = ChoiceGuids(e.Key);
                var buff = Create<BlueprintBuff>("OathboundRevenantBond" + e.Key + "Buff", ids[0], b => { CopyFields(new[] { typeof(BlueprintBuff), typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact) }, flamingBuff, b); SetField(b, "m_DisplayName", Strings.L("bond." + e.Key.ToLowerInvariant() + ".name")); SetField(b, "m_Description", Strings.L("bond." + e.Key.ToLowerInvariant() + ".desc")); });
                var prop = CloneBoundary.CopyComponent(abp, KeepShared, RevenantRules.Dials.CloneDepth);
                SetField(prop, "m_Enchant", Ref<BlueprintItemEnchantmentReference>(e.Guid));
                AddComponents(buff, prop);
                var choice = Create<BlueprintActivatableAbility>("OathboundRevenantBond" + e.Key + "Choice", ids[1], a => { CopyFields(new[] { typeof(BlueprintActivatableAbility), typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact) }, flaming, a); SetField(a, "m_Icon", null); /* 2026-11-15: 불꽃 아이콘은 따라오지 않게 — 그림은 IconTable(Icons.Apply)이 넣는다 */ SetField(a, "m_DisplayName", Strings.L("bond." + e.Key.ToLowerInvariant() + ".name")); SetField(a, "m_Description", Strings.L("bond." + e.Key.ToLowerInvariant() + ".desc")); });
                SetField(choice, "m_Buff", Ref<BlueprintBuffReference>(ids[0]));
                // 검토 I2: 유대 풀(DivineWeaponProperty 묶음 크기)에서 비용만큼 차지한다 — 본편 선택지 WeightInGroup = 인챈트 비용(Holy·Axiomatic·Disruption 2, Speed 3, BrilliantEnergy 4, Flaming 1 — blueprints.zip 2026-11-14).
                // 엔진 ActivatableAbility.OnDidTurnOn IL_0019~00EB: 켜진 무게 합 > 묶음 크기이면 오래된 것부터(자기 포함) 끈다.
                choice.WeightInGroup = e.Cost;
                AddComponents(choice, flaming.ComponentsArray.Select(c => (BlueprintComponent)CloneBoundary.CopyComponent(c, KeepShared, RevenantRules.Dials.CloneDepth)).ToArray());
                Choices[e.Key] = choice;
                choiceGuids.Add(ids[1]);
            }
            WeaponChoices = NewFeature("OathboundRevenantWeaponChoices", Guids.RevenantWeaponChoices, "revenantweaponbond.name", "revenantweaponbond.desc", true);
            var facts = new AddFacts();
            SetField(facts, "m_Facts", choiceGuids.Select(Ref<BlueprintUnitFactReference>).ToArray());
            AddComponents(WeaponChoices, facts);
            WeaponFeature = Archetype.Shell("RevenantWeaponBond");
            AddComponents(WeaponFeature, IfHas(BondPlan.Rows.Single(r => r.Key == "RevenantWeaponBond")));
            Archetype.MarkFilled("RevenantWeaponBond");
            Verify(flaming);
            VerifyRecheck(MountFeature);
            VerifyRecheck(WeaponFeature);
            OathFx.VerifyAllUsed();   // 마지막 FX 빌더 — FxTable.ExpectedUses 가 모두 지나갔는가(빠지면 던진다)
            Built = true;
        }

        static AddFeatureToPet ToPet(string feature)
        {
            var c = Comp(new AddFeatureToPet());
            SetField(c, "m_PetType", PetType.AnimalCompanion);
            SetField(c, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            return c;
        }

        // 유대 특성 = AddFeatureIfHasFact + 레벨업 끝 재판정(BondRecheckComponent — BondRecheckRule 머리말: 판정이 유대 선택보다 앞서던 S16·S17 결함).
        static BlueprintComponent[] IfHas(BondRow row)
        {
            var c = Comp(new AddFeatureIfHasFact { Not = false });
            SetField(c, "m_CheckedFact", Ref<BlueprintUnitFactReference>(row.CheckedFactGuid));
            SetField(c, "m_Feature", Ref<BlueprintUnitFactReference>(row.InnerKey == "RevenantUndeadMountInner" ? Guids.RevenantUndeadMountInner : Guids.RevenantWeaponChoices));
            return new BlueprintComponent[] { c, Comp(new BondRecheckComponent()) };
        }

        // 되읽기: 유대 특성마다 AddFeatureIfHasFact 1 + BondRecheckComponent 1(빠지면 레벨업 직후 탈것·선택지가 한 박자 늦는 결함이 돌아온다).
        static void VerifyRecheck(BlueprintFeature f)
        {
            int ifs = f.ComponentsArray.OfType<AddFeatureIfHasFact>().Count(), rc = f.ComponentsArray.OfType<BondRecheckComponent>().Count();
            if (ifs != 1 || rc != 1) throw new InvalidOperationException("OathboundRevenant: bond feature " + f.name + " has " + ifs + " AddFeatureIfHasFact / " + rc + " BondRecheckComponent (expected 1 / 1)");
        }

        static void BuildEnchants()
        {
            var vanillaNecrotic = Get<BlueprintWeaponEnchantment>(Guids.Game.Necrotic);
            var wcd = vanillaNecrotic.ComponentsArray.OfType<WeaponConditionalDamageDice>().SingleOrDefault();
            if (wcd == null) throw new InvalidOperationException("OathboundRevenant: vanilla Necrotic has no single WeaponConditionalDamageDice");
            foreach (var e in BondPlan.Enchants)
            {
                if (e.IsVanilla) { EnchantBps[e.Key] = Get<BlueprintItemEnchantment>(e.Guid); continue; }
                var en = Create<BlueprintWeaponEnchantment>("OathboundRevenant" + e.Key + "Enchant", e.Guid, x =>
                {
                    SetField(x, "m_EnchantName", Strings.L("bond." + e.Key.ToLowerInvariant() + ".name"));
                    SetField(x, "m_Description", Strings.L("bond." + e.Key.ToLowerInvariant() + ".desc"));
                    // 2026-11-16: 접두·접미 문구 없음을 명시(null LocalizedString 은 읽는 곳마다 "<null>" — Tooltips 머리말). 본편 Necrotic 은 접두 「Necrotic」 공용 문구·접미 빈 키.
                    SetField(x, "m_Prefix", NoText());
                    SetField(x, "m_Suffix", NoText());
                    SetField(x, "m_EnchantmentCost", e.Cost);
                    FillEmpty(x);
                });
                AddComponents(en, EnchantComponents(e.Key, wcd).ToArray());
                Hands.RebindElements(en);
                EnchantBps[e.Key] = en;
            }
        }

        static IEnumerable<BlueprintComponent> EnchantComponents(string key, WeaponConditionalDamageDice wcd)
        {
            switch (key)
            {
                case "Necrotic":
                    foreach (var row in BondPlan.NecroticRows)
                    {
                        var c = CloneBoundary.CopyComponent(wcd, KeepShared, RevenantRules.Dials.ActionCloneDepth); // Conditions(ConditionsChecker) 포함
                        CloneKit.DetachFields(c, v => v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent, RevenantRules.Dials.ActionCloneDepth);
                        c.Damage.TypeDescription.Energy = Energy(row.Energy);
                        c.Damage.Dice = new DiceFormula(row.Dice, Die(RevenantRules.Dials.SmiteDie));
                        c.Conditions = new ConditionsChecker { Operation = Operation.And, Conditions = row.Condition.Split(',').Select(ConditionFor).ToArray() };
                        yield return c;
                    }
                    break;
                case "Vampiric":
                    var heal = Act(new ContextActionHealTarget { Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = CvZero(), BonusValue = CvRank(AbilityRankType.Default) } });
                    // FX(2026-11-15 승인, FxTable): 회복 뒤 흡혈의 손길 FX — ActionsOnInitiator 라 휘두른 자 위.
                    yield return Comp(new AddInitiatorAttackWithWeaponTrigger { OnlyHit = true, ActionsOnInitiator = true, Action = List(heal, OathFx.SpawnAction("VampiricBond")) });
                    yield return RankClassLevel(AbilityRankType.Default, ContextRankProgression.Div2);
                    break;
                case "DreadBlade":
                    var dur = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = Die(FearPlan.DurationDiceSides), DiceCountValue = Cv(FearPlan.DurationDiceCount), BonusValue = CvZero() };
                    var failed = Act(new ContextActionConditionalSaved { Succeed = List(), Failed = List(ApplyBuff(Guids.Game.Shaken, false, dur, false)) });
                    var save = Act(new ContextActionSavingThrow { Type = SavingThrowType.Will, HasCustomDC = false, UseDCFromContextSavingThrow = false, FromBuff = false, CustomDC = CvZero(), Actions = List(failed) });
                    yield return Comp(new AddInitiatorAttackWithWeaponTrigger { OnlyHit = true, ActionsOnInitiator = false, Action = List(save) });
                    var dc = Comp(new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma, UseKineticistMainStat = false });
                    SetField(dc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.PaladinClass));
                    yield return dc;
                    yield return Fear.FearDescriptor(); // 검토 I3: 공포의 칼날 내성 = 공포 내성
                    break;
                default: throw new InvalidOperationException("OathboundRevenant: bond enchant " + key + " has no builder");
            }
        }

        static Condition ConditionFor(string token)
        {
            bool not = token.StartsWith("!");
            string name = not ? token.Substring(1) : token;
            string guid = name == "NegativeEnergyAffinity" ? Guids.Game.NegativeEnergyAffinity : name == "ConstructType" ? Guids.Game.ConstructType : name == "UndeadType" ? Guids.Game.UndeadType : null;
            if (guid == null) throw new ArgumentException("OathboundRevenant: unknown necrotic condition token " + token);
            return HasFact(guid, not);
        }

        static string[] ChoiceGuids(string key)
        {
            switch (key)
            {
                case "Necrotic": return new[] { Guids.BondNecroticBuff, Guids.BondNecroticChoice };
                case "Vampiric": return new[] { Guids.BondVampiricBuff, Guids.BondVampiricChoice };
                case "GhostTouch": return new[] { Guids.BondGhostTouchBuff, Guids.BondGhostTouchChoice };
                case "DreadBlade": return new[] { Guids.BondDreadBladeBuff, Guids.BondDreadBladeChoice };
            }
            throw new ArgumentException("OathboundRevenant: no bond choice GUIDs for " + key);
        }

        // 직렬화 필드 복사(캐시 제외) — Hands.CopyAbilityFields 와 같은 규칙.
        internal static void CopyFields(Type[] types, object src, object dst)
        {
            foreach (var t in types)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.IsLiteral || f.IsInitOnly || f.Name.StartsWith("m_Cached")) continue;
                    var ft = f.FieldType;
                    if (ft.IsGenericType && (ft.GetGenericTypeDefinition() == typeof(Nullable<>) || ft.Name.StartsWith("Cacheable"))) continue;
                    f.SetValue(dst, CloneKit.CopyOf(f.GetValue(src), KeepShared, RevenantRules.Dials.CloneDepth));
                }
        }

        // 빌드 되읽기: 선택지 플래그 == 본편(BondPlan) · 비용 == 원장 · 죽음의 기운 갈래 == NecroticRows · 원본 불변.
        static void Verify(BlueprintActivatableAbility flaming)
        {
            foreach (var kv in Choices)
            {
                var a = kv.Value;
                if (a.DeactivateImmediately != BondPlan.ChoiceDeactivateImmediately || a.ActivationType.ToString() != BondPlan.ChoiceActivationType || a.Group.ToString() != BondPlan.ChoiceGroup || a.IsOnByDefault != BondPlan.ChoiceOnByDefault)
                    throw new InvalidOperationException("OathboundRevenant: bond choice " + kv.Key + " flags differ from BondPlan / vanilla");
                if (a.ComponentsArray.Any(c => c.GetType().Name == "ActivatableAbilityResourceLogic")) throw new InvalidOperationException("OathboundRevenant: bond choice " + kv.Key + " has a resource logic (DeactivateImmediately risk)");
                if (a.WeightInGroup != BondPlan.Enchant(kv.Key).Cost) throw new InvalidOperationException("OathboundRevenant: bond choice " + kv.Key + " weight " + a.WeightInGroup + " != BondPlan cost " + BondPlan.Enchant(kv.Key).Cost + " (the pool would not enforce the cost)");
            }
            if (Get<BlueprintActivatableAbility>(Guids.Game.WeaponBondFlamingChoice).WeightInGroup != BondPlan.Enchant("Necrotic").Cost) throw new InvalidOperationException("OathboundRevenant: vanilla flaming choice weight changed (cost-1 precedent)");
            Fear.VerifyDescriptor(EnchantBps["DreadBlade"]);
            foreach (var e in BondPlan.Enchants)
                if (EnchantBps[e.Key].EnchantmentCost != e.Cost) throw new InvalidOperationException("OathboundRevenant: enchant " + e.Key + " cost " + EnchantBps[e.Key].EnchantmentCost + " != BondPlan " + e.Cost);
            var nec = EnchantBps["Necrotic"].ComponentsArray.OfType<WeaponConditionalDamageDice>().ToList();
            if (nec.Count != BondPlan.NecroticRows.Count) throw new InvalidOperationException("OathboundRevenant: necrotic branches " + nec.Count + " != BondPlan");
            for (int i = 0; i < nec.Count; i++)
                if (nec[i].Damage.TypeDescription.Energy.ToString() != BondPlan.NecroticRows[i].Energy) throw new InvalidOperationException("OathboundRevenant: necrotic branch " + i + " energy differs from BondPlan");
            if (Get<BlueprintBuff>(Guids.Game.WeaponBondFlamingBuff).ComponentsArray.OfType<AddBondProperty>().Single().Enchant == null) throw new InvalidOperationException("OathboundRevenant: vanilla flaming bond buff lost its enchant");
            var v = Get<BlueprintWeaponEnchantment>(Guids.Game.Necrotic).ComponentsArray.OfType<WeaponConditionalDamageDice>().Single();
            if (v.Damage.TypeDescription.Energy.ToString() != StrikePlan.Negative) throw new InvalidOperationException("OathboundRevenant: vanilla Necrotic changed during the clone");
        }
    }

    // 말 쪽 특성(RevenantMountHp)의 부품: 주인(팔라딘) 매력 수정치를 말의 매력 점수에 수정치 1개로 건다(P14 — HP 는 UndeadHealth 매력 규칙이 따라감).
    // 다시 계산 = 켜질 때 + 말의 새 라운드마다(주인 능력치 변경을 말 쪽에서 받는 본편 경로 없음 — 부록 A-n, 지연 ≤ 1 라운드). 주인 결측 = Report.
    public class MountChaHpComponent : UnitFactComponentDelegate, ITickEachRound
    {
        protected override void OnActivate() { Recalculate(false); }
        protected override void OnDeactivate() { Owner.Stats.Charisma.RemoveModifiersFrom(Runtime); }
        public void OnNewRound() { Recalculate(true); }

        // 검토 M6: 켜질 때(로드 중 — Master 참조 미해결 가능) 주인이 없으면 미룬다(다음 라운드 틱에서 다시). 라운드 틱에서도 없으면 판정 불가 → 유닛당 1회 보고.
        void Recalculate(bool atRoundTick)
        {
            if (!Guard.Run(ErrorLedger.SiteMountHp, Fact, () =>
            {
                var master = Owner.Master;
                var v = MountHpRule.OnMaster(master != null, atRoundTick);
                if (v == MountHpVerdict.Defer) return; // 정당한 미룸: 다음 라운드 틱이 다시 계산
                if (v == MountHpVerdict.Missing) { RevenantErrors.ReportOnce(ErrorLedger.SiteMountHp, Owner.UniqueId, Owner.CharacterName + " has no master at a round tick (cannot read the paladin's Charisma)"); return; }
                int bonus = MountHpRule.ChaScoreBonus(master.Stats.Charisma.Bonus);
                var cha = Owner.Stats.Charisma;
                cha.RemoveModifiersFrom(Runtime);
                if (bonus != 0) cha.AddModifierUnique(bonus, Runtime, ModifierDescriptor.UntypedStackable);
            }, RevenantErrors.Sink)) return; // 실패는 계수됨 — 수정치 변경 없음
        }
    }

    // 유대 특성의 레벨업 끝 재판정(BondRecheckRule 머리말). 같은 블루프린트의 AddFeatureIfHasFact 하나를 읽어, 확인 사실이 있는데 안쪽 특성이 없으면
    // 이 특성을 한 번 다시 적용한다(EntityFact.Reapply = Deactivate + Activate → AddFeatureIfHasFact.OnActivate → Apply). 안쪽 특성이 이미 있으면 무동작(선택지 켜짐 상태 보존).
    // 엔진에 예외를 넘기지 않는다: 실패는 RevenantErrors.Report(WhereBondRecheck) 로 계수 → 시나리오 errors-0 / S0 FAIL.
    public class BondRecheckComponent : UnitFactComponentDelegate, IUnitReapplyFeaturesOnLevelUpHandler
    {
        public void HandleUnitReapplyFeaturesOnLevelUp()
        {
            try
            {
                var ifs = Fact.Blueprint.ComponentsArray.OfType<AddFeatureIfHasFact>().ToList();
                if (ifs.Count != 1) { RevenantErrors.Report(ErrorLedger.WhereBondRecheck, Fact.Blueprint.name + " has " + ifs.Count + " AddFeatureIfHasFact (expected 1) — cannot re-check"); return; }
                var c = ifs[0];
                if (c.CheckedFact == null || c.Feature == null) { RevenantErrors.Report(ErrorLedger.WhereBondRecheck, Fact.Blueprint.name + " AddFeatureIfHasFact has an unresolved reference (checked " + (c.CheckedFact != null) + ", feature " + (c.Feature != null) + ")"); return; }
                if (c.Not) { RevenantErrors.Report(ErrorLedger.WhereBondRecheck, Fact.Blueprint.name + " AddFeatureIfHasFact is negated (this re-check only covers Not = false)"); return; }
                var v = BondRecheckRule.Decide(Owner.Descriptor.HasFact(c.CheckedFact), Owner.Descriptor.HasFact(c.Feature));
                if (v == BondRecheckVerdict.Reapply) Fact.Reapply();
            }
            catch (Exception e) { RevenantErrors.Report(ErrorLedger.WhereBondRecheck, e.GetType().Name + ": " + e.Message); }
        }
    }

    // 채널의 말 회복(§8-d): 같은 엔진 AoE 굴림·같은 조건(살아 있는 적 분기 안)·채널 영역 안(P20)·언데드 말만. 판단 = ChannelHealRule.DecideFor(Mount).
    internal static partial class ChannelMountHeal
    {
        static string Mount { get { return HealKeys.Mount; } }

        static partial void TryHealImpl(MechanicsContext ctx, UnitEntityData caster, int roll)
        {
            var pet = caster.Pets.Select(p => p.Entity).FirstOrDefault(p => p != null && p.Descriptor.HasFact(DivineBond.MountHp));
            if (pet == null) return; // 정당한 무동작: 언데드 말이 없다(무기 유대·말 없음)
            float centerFt = Vector2.Distance(new Vector2(caster.Position.x, caster.Position.z), new Vector2(pet.Position.x, pet.Position.z)) / RevenantRules.Dials.MetersPerFoot;
            bool eligible = !pet.State.IsDead && pet.Descriptor.HasFact(Get<BlueprintFeature>(Guids.Game.UndeadType))
                && AuraGeometry.Inside(centerFt, RevenantRules.Dials.ChannelRadiusFeet, pet.Corpulence / RevenantRules.Dials.MetersPerFoot);
            var v = ChannelHealRule.DecideFor(true, FailureLedger.Failed(ctx), HealOnceLedger.IsMarked(ctx, Mount), true, eligible);
            if (v != ChannelHealVerdict.Heal) return; // Skip(영역 밖·이미 회복) / StopSilently(상류 실패)
            HealOnceLedger.Mark(ctx, Mount);
            ctx.TriggerRule(new RuleHealDamage(caster, pet, roll));
        }
    }
}
