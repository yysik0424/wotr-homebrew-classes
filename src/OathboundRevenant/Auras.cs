using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static OathboundRevenant.Parts;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    // 행 = AuraPlan(DeathAura · DenseFear · DeathsResolve; OathShield 는 T7). Size = AuraGeometry.SizeFor(가장자리 반경, Medium 체구) — 엔진 ScriptZoneCylinder.Contains 식(부록 A-f, T6 Step 0).
    public sealed class ReplaceLesserFearAura : UnitBuffComponentDelegate
    {
        protected override void OnActivate()
        {
            var caster = Context == null ? null : Context.MaybeCaster;
            if (caster == null) return;
            var lesser = Auras.Effects["DeathAura"];
            foreach (var buff in Owner.Buffs.Enumerable.Where(b => b.Blueprint == lesser && b.Context != null && b.Context.MaybeCaster == caster).ToArray())
                buff.Remove();
        }
    }

    public static class Auras
    {
        public static readonly Dictionary<string, BlueprintAbilityAreaEffect> Areas = new Dictionary<string, BlueprintAbilityAreaEffect>();
        public static readonly Dictionary<string, BlueprintBuff> Effects = new Dictionary<string, BlueprintBuff>();
        public static bool Built { get; private set; }

        public static void Build()
        {
            if (Built) throw new InvalidOperationException("OathboundRevenant: Auras.Build() called twice");
            foreach (var row in AuraPlan.Rows)
            {
                if (!LevelPlan.ImplementedTasks.Contains(row.OwnerTask)) continue; // 그 Task 가 아직 없다(Talents/Archetype 의 채움 되읽기가 같은 규칙으로 잡는다)
                if (row.Effect == "OathShield" && !OathShieldReady) continue;
                BuildAura(row);
            }
            Verify();
            Built = true;
        }

        // T7 이 맹세의 방패 효과 부품을 준비하면 true(그 전에는 OathShield 행을 만들지 않는다).
        internal static bool OathShieldReady { get { return OathShieldFactory != null; } }
        internal static Func<BlueprintComponent> OathShieldFactory;

        // 효과 부품이 뒤 Task 에서 준비되는 행(OathShield) — 그 빌더가 부른다.
        internal static void BuildLate(string key)
        {
            if (Areas.ContainsKey(key)) throw new InvalidOperationException("OathboundRevenant: aura " + key + " built twice");
            var row = AuraPlan.Row(key);
            if (!LevelPlan.ImplementedTasks.Contains(row.OwnerTask)) throw new InvalidOperationException("OathboundRevenant: aura " + key + " built before its task " + row.OwnerTask + " is implemented");
            BuildAura(row);
            Verify();
        }

        static void BuildAura(AuraRow row)
        {
            var ids = AuraGuids(row.Key);
            var effect = NewBuff("OathboundRevenant" + row.Key + "Effect", ids[2], NameKey(row.Key), DescKey(row.Key), false);
            AddComponents(effect, EffectComponents(row).ToArray());
            if (row.Key == "DenseFear") AddComponents(effect, Comp(new ReplaceLesserFearAura()));
            Effects[row.Key] = effect;

            var area = Create<BlueprintAbilityAreaEffect>("OathboundRevenant" + row.Key + "Area", ids[1], a =>
            {
                a.Shape = AreaEffectShape.Cylinder;
                a.Size = new Feet(row.SizeFeet);
                SetField(a, "m_TargetType", Enum.Parse(FindField(typeof(BlueprintAbilityAreaEffect), "m_TargetType").FieldType, row.Target));
                a.AffectEnemies = row.Target == "Enemy";
                a.AggroEnemies = false;
                a.Fx = OathFx.AreaFx(FxTable.Aura + row.Key);   // FX(2026-11-15 승인, FxTable): 적 대상 죽음 오라 = 음에너지 영역 FX, 아군 인접 오라 = 없음(NoFx 이유)
                FillEmpty(a);
            });
            var conds = new List<Condition>();
            conds.Add(row.Target == "Enemy" ? (Condition)Cond(new ContextConditionIsEnemy { Not = false }) : Cond(new ContextConditionIsAlly { Not = false }));
            if (row.NegatedByFact != null) conds.Add(CasterHasFact(Guids.Talent(row.NegatedByFact), true));
            var aeb = Comp(new AbilityAreaEffectBuff { Condition = new ConditionsChecker { Operation = Operation.And, Conditions = conds.ToArray() }, CheckConditionEveryRound = row.CheckEveryRound });
            SetField(aeb, "m_Buff", Ref<BlueprintBuffReference>(ids[2]));
            AddComponents(area, aeb);
            Areas[row.Key] = area;

            var auraBuff = NewBuff("OathboundRevenant" + row.Key + "Aura", ids[0], NameKey(row.Key), DescKey(row.Key), true);
            var add = Comp(new AddAreaEffect());
            SetField(add, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(ids[1]));
            AddComponents(auraBuff, add);

            var owner = row.Key == "DeathAura" ? Archetype.Shell(row.Key) : Talents.Get(row.Key);
            var afc = Comp(new AuraFeatureComponent());
            SetField(afc, "m_Buff", Ref<BlueprintBuffReference>(ids[0]));
            AddComponents(owner, afc);
            if (row.Key == "DeathAura") Archetype.MarkFilled(row.Key); else Talents.MarkFilled(row.Key);
        }

        static IEnumerable<BlueprintComponent> EffectComponents(AuraRow row)
        {
            switch (row.Effect)
            {
                case "FearPenaltyHalfHeal":
                    var st = Comp(new SavingThrowBonusAgainstDescriptor { ModifierDescriptor = ModifierDescriptor.UntypedStackable, Value = row.FearPenalty, Bonus = CvZero() });
                    st.SpellDescriptor = SpellDescriptor.Fear;
                    yield return st;
                    yield return Comp(new HealResistance { Resistance = RevenantRules.Dials.HealResistance });
                    break;
                case "FearImmunity":
                    // 본편 AuraOfCourageFeature 자기 면역 두 줄 선례(BuffDescriptorImmunity{Fear} + SpellImmunityToSpellDescriptor{Fear}).
                    var bdi = Comp(new BuffDescriptorImmunity());
                    bdi.Descriptor = SpellDescriptor.Fear;
                    yield return bdi;
                    var sid = Comp(new SpellImmunityToSpellDescriptor());
                    sid.Descriptor = SpellDescriptor.Fear;
                    yield return sid;
                    break;
                case "OathShield":
                    if (OathShieldFactory == null) throw new InvalidOperationException("OathboundRevenant: OathShield aura effect requested before Survival registered its component factory");
                    yield return OathShieldFactory();
                    break;
                default: throw new InvalidOperationException("OathboundRevenant: aura effect " + row.Effect + " has no builder");
            }
        }

        static string NameKey(string key) { return key == "DeathAura" ? "deathaura.name" : "talent." + key.ToLowerInvariant() + ".name"; }
        static string DescKey(string key) { return key == "DeathAura" ? "deathaura.desc" : "talent." + key.ToLowerInvariant() + ".desc"; }

        static string[] AuraGuids(string key)
        {
            switch (key)
            {
                case "DeathAura": return new[] { Guids.DeathAuraBuff, Guids.DeathAuraArea, Guids.DeathAuraEffect };
                case "DenseFear": return new[] { Guids.DenseFearBuff, Guids.DenseFearArea, Guids.DenseFearEffect };
                case "DeathsResolve": return new[] { Guids.ResolveBuff, Guids.ResolveArea, Guids.ResolveEffect };
                case "OathShield": return new[] { Guids.OathShieldBuff, Guids.OathShieldArea, Guids.OathShieldEffect };
            }
            throw new ArgumentException("OathboundRevenant: no aura GUIDs for " + key);
        }

        // Size 되읽기 = Feet 끼리 정확 비교(op_Inequality → Single.Equals). Feet.Value 는 쓰지 않는다: 엔진 Feet::get_Value 가 int(IL_0006 conv.i4, 소수 버림)라
        // 비정수 Size 11.64 를 11 로 읽어 정상 빌드를 실패시켰다(2026-11-15 인게임 BuildAll). 엔진 기하는 get_Meters(실수)로 읽는다(AreaEffectView.InitShape).
        internal static void VerifySize(string key, BlueprintAbilityAreaEffect a)
        {
            if (a == null) throw new ArgumentNullException("a", "OathboundRevenant: aura " + key + " area is null");
            var planned = new Feet(AuraPlan.Row(key).SizeFeet);
            if (a.Size != planned) throw new InvalidOperationException("OathboundRevenant: aura " + key + " Size " + a.Size.Meters.ToString("R") + " m != AuraPlan " + planned.Meters.ToString("R") + " m (" + AuraPlan.Row(key).SizeFeet.ToString("R") + " ft)");
        }

        // 빌드 되읽기: 영역마다 Size·대상·EveryRound·무효 조건 == AuraPlan, 효과 수치 == 행.
        static void Verify()
        {
            foreach (var kv in Areas)
            {
                var row = AuraPlan.Row(kv.Key);
                var a = kv.Value;
                VerifySize(kv.Key, a);
                var aeb = a.ComponentsArray.OfType<AbilityAreaEffectBuff>().Single();
                if (aeb.CheckConditionEveryRound != row.CheckEveryRound) throw new InvalidOperationException("OathboundRevenant: aura " + kv.Key + " CheckConditionEveryRound differs from AuraPlan");
                bool negated = aeb.Condition.Conditions.OfType<ContextConditionCasterHasFact>().Any(c => c.Not);
                if (negated != (row.NegatedByFact != null)) throw new InvalidOperationException("OathboundRevenant: aura " + kv.Key + " negation condition differs from AuraPlan");
                if (row.Effect == "FearPenaltyHalfHeal")
                {
                    var e = Effects[kv.Key];
                    var st = e.ComponentsArray.OfType<SavingThrowBonusAgainstDescriptor>().Single();
                    var hr = e.ComponentsArray.OfType<HealResistance>().Single();
                    if (st.Value != row.FearPenalty || hr.Resistance != RevenantRules.Dials.HealResistance) throw new InvalidOperationException("OathboundRevenant: aura " + kv.Key + " effect numbers differ from AuraPlan");
                }
            }
        }
    }
}
