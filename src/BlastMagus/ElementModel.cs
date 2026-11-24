using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BlastMagus
{
    // 원소 하나의 해석 결과(계약 통과한 블루프린트 객체). BlastBlueprints·BlastStrike 가 이것만 쓴다(원소 GUID 를 직접 다루지 않는다).
    public sealed class ResolvedElement
    {
        public readonly ElementRow Row;
        public readonly BlueprintFeature BlastFeature, CompositeFeature;
        public readonly BlueprintAbility Base, Ability, CompositeBase, CompositeAbility;

        internal ResolvedElement(ElementRow row, BlueprintFeature blastFeature, BlueprintAbility baseAbility, BlueprintAbility ability,
            BlueprintFeature compositeFeature, BlueprintAbility compositeBase, BlueprintAbility compositeAbility)
        {
            Row = row; BlastFeature = blastFeature; Base = baseAbility; Ability = ability;
            CompositeFeature = compositeFeature; CompositeBase = compositeBase; CompositeAbility = compositeAbility;
        }
    }

     
    // 형식 판정은 정확한 형식(GetType() == typeof) — jbp 의 "$type" 이름과 같은 기준(파생 형식은 다른 이름이라 계약이 개수로 시끄럽게 잡는다).
    public static class ElementModel
    {
         
        const int MaxActionDepth = 16;

        public static BlastShape Extract(ElementRow r)
        {
            if (r == null) throw new ArgumentNullException("r");
            return ShapeOf(Fetch(r));
        }

        // Extract + 외부 계약(R-PF1: CheckExternal). 위반이 하나라도 있으면 위반 전부를 담아 던진다(우회 없음).
        public static ResolvedElement Resolve(ElementRow r)
        {
            if (r == null) throw new ArgumentNullException("r");
            var e = Fetch(r);
            string[] violations = BlastContract.CheckExternal(ShapeOf(e), r);
            if (violations.Length > 0)
                throw new InvalidOperationException("BlastMagus: element " + r.Id + " fails the blast contract (" + violations.Length + "): " + string.Join(" | ", violations));
            return e;
        }

        // 우리 특성 계약(설계 §4.3-1a 0번): 우리 원소 특성을 만든 뒤 부른다. 위반이면 던진다.
        public static void CheckOurFeature(BlueprintFeature f, ElementRow r)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (r == null) throw new ArgumentNullException("r");
            var shape = new OurFeatureShape { Guid = f.AssetGuid.ToString(), Components = ComponentsOf(f) };
            string[] violations = BlastContract.CheckOurFeature(shape, r, EnergizeTable.Of(r.Id).ToggleGuid);
            if (violations.Length > 0)
                throw new InvalidOperationException("BlastMagus: element " + r.Id + " our feature " + f.name + " fails the contract: " + string.Join(" | ", violations));
        }

        // 피해 서술자(설계 §4.3-1a 끝 「호출마다 새로」): DescriptorOf → 새 DamageTypeDescription. 가변 클래스라 공유하면 한 처리기의 수정이 다른 공격에 번진다.
        public static DamageTypeDescription NewDamageType(ElementRow r)
        {
            var d = ElementTable.DescriptorOf(r); // null·칸 배타 위반은 여기서 던진다
            var t = new DamageTypeDescription(); // ctor 가 Common·Physical 을 새로 만든다(IL_0001~0011)
            if (d.Kind == DamageKind.Energy)
            {
                t.Type = DamageType.Energy;
                t.Energy = ParseExact<DamageEnergyType>(d.EnergyName, r);
                if (t.Type != DamageType.Energy || t.Energy.ToString() != d.EnergyName)
                    throw new InvalidOperationException("BlastMagus: NewDamageType readback for " + r.Id + " is " + t.Type + "/" + t.Energy + ", expected Energy/" + d.EnergyName);
            }
            else
            {
                if (t.Physical == null) throw new InvalidOperationException("BlastMagus: new DamageTypeDescription has no Physical data (engine ctor changed?)");
                t.Type = DamageType.Physical;
                t.Physical.Form = ParseExact<PhysicalDamageForm>(d.PhysicalForm, r);
                if (t.Type != DamageType.Physical || t.Physical.Form.ToString() != d.PhysicalForm)
                    throw new InvalidOperationException("BlastMagus: NewDamageType readback for " + r.Id + " is " + t.Type + "/" + t.Physical.Form + ", expected Physical/" + d.PhysicalForm);
            }
            return t;
        }

        // 이름 → enum 값. 숫자 문자열·순서가 다른 플래그 이름처럼 「통과하지만 다른 값」이 되는 입력은 되읽기 문자열 비교로 거부한다.
        static T ParseExact<T>(string name, ElementRow r) where T : struct
        {
            object v;
            try { v = Enum.Parse(typeof(T), name, false); }
            catch (ArgumentException e) { throw new InvalidOperationException("BlastMagus: element " + r.Id + " descriptor '" + name + "' is not a " + typeof(T).Name, e); }
            if (v.ToString() != name) throw new InvalidOperationException("BlastMagus: element " + r.Id + " descriptor '" + name + "' parses to " + typeof(T).Name + "." + v + " (not canonical)");
            return (T)v;
        }

        static ResolvedElement Fetch(ElementRow r)
        {
            return new ResolvedElement(r,
                Load<BlueprintFeature>(r, "BlastFeatureGuid", r.BlastFeatureGuid), Load<BlueprintAbility>(r, "BaseGuid", r.BaseGuid), Load<BlueprintAbility>(r, "AbilityGuid", r.AbilityGuid),
                Load<BlueprintFeature>(r, "CompositeFeatureGuid", r.CompositeFeatureGuid), Load<BlueprintAbility>(r, "CompositeBaseGuid", r.CompositeBaseGuid),
                Load<BlueprintAbility>(r, "CompositeAbilityGuid", r.CompositeAbilityGuid));
        }

        static T Load<T>(ElementRow r, string field, string guid) where T : SimpleBlueprint
        {
            try { return Get<T>(guid); }
            catch (Exception e) when (e is InvalidOperationException || e is InvalidCastException || e is FormatException || e is ArgumentException)
            {
                throw new InvalidOperationException("BlastMagus: element " + r.Id + " " + field + " " + guid + " does not resolve: " + e.Message, e);
            }
        }

        static BlastShape ShapeOf(ResolvedElement e)
        {
            return new BlastShape
            {
                BlastFeatureGuid = e.BlastFeature.AssetGuid.ToString(),
                BlastFeatureComponents = ComponentsOf(e.BlastFeature),
                Base = AbilityOf(e.Base),
                Leaf = AbilityOf(e.Ability),
                CompositeFeatureGuid = e.CompositeFeature.AssetGuid.ToString(),
                CompositeFeatureComponents = ComponentsOf(e.CompositeFeature),
                CompositeBase = AbilityOf(e.CompositeBase),
                CompositeLeaf = AbilityOf(e.CompositeAbility),
            };
        }

        static BlueprintComponent[] Components(BlueprintScriptableObject bp)
        {
            var cs = bp.ComponentsArray;
            if (cs == null) throw new InvalidOperationException("BlastMagus: " + bp.name + " " + bp.AssetGuid + " has a null ComponentsArray");
            return cs;
        }

        static ComponentShape[] ComponentsOf(BlueprintScriptableObject bp)
        {
            return Components(bp).Select(c =>
            {
                if (c == null) return new ComponentShape { Type = "null" };
                var s = new ComponentShape { Type = c.GetType().Name };
                if (c.GetType() == typeof(AddFeatureIfHasFact))
                {
                    s.CheckedFact = RefGuid(GetF(c, "m_CheckedFact"));
                    s.Feature = RefGuid(GetF(c, "m_Feature"));
                    s.Not = ((AddFeatureIfHasFact)c).Not;
                }
                else if (c.GetType() == typeof(AddFacts)) s.Facts = RefGuids(GetF(c, "m_Facts"));
                return s;
            }).ToArray();
        }

        static AbilityShape AbilityOf(BlueprintAbility a)
        {
            var cs = Components(a);
            Func<Type, IEnumerable<BlueprintComponent>> of = t => cs.Where(c => c != null && c.GetType() == t);
            return new AbilityShape
            {
                Guid = a.AssetGuid.ToString(),
                Parent = RefGuid(GetF(a, "m_Parent")),
                SpellResistance = a.SpellResistance,
                VariantLists = of(typeof(AbilityVariants)).Select(c => RefGuids(GetF(c, "m_Variants"))).ToArray(),
                ShowConditions = of(typeof(AbilityShowIfCasterHasFact)).Select(c => new ShowConditionShape { UnitFact = RefGuid(GetF(c, "m_UnitFact")), Not = ((AbilityShowIfCasterHasFact)c).Not }).ToArray(),
                BurnCosts = of(typeof(AbilityKineticist)).Select(c => ((AbilityKineticist)c).BlastBurnCost).ToArray(),
                ProjectileWeaponTypes = of(typeof(AbilityDeliverProjectile)).Select(c =>
                {
                    // 빈 참조(GUID 없음) = null 항목(계약 7번이 「비어 있음」 위반으로 센다). GUID 가 있는데 블루프린트가 없으면 「unresolved」 로 던진다(빈 값과 구분).
                    string wg = RefGuid(GetF(c, "m_Weapon"));
                    if (wg == null) return null;
                    var w = ((AbilityDeliverProjectile)c).Weapon; // m_Weapon?.Get() (get_Weapon IL_0000~0011)
                    if (w == null) throw new InvalidOperationException("BlastMagus: " + a.name + " projectile weapon unresolved " + wg);
                    return RefGuid(GetF(w, "m_Type"));
                }).ToArray(),
                RankConfigs = of(typeof(ContextRankConfig)).Select(c => new RankConfigShape
                {
                    Type = ((ContextRankConfig)c).Type.ToString(), // get_Type = m_Type (IL_0000~0006)
                    BaseValueType = GetF(c, "m_BaseValueType").ToString(),
                    Feature = RefGuid(GetF(c, "m_Feature")),
                    FeatureList = RefGuids(GetF(c, "m_FeatureList")),
                    CustomProperty = RefGuid(GetF(c, "m_CustomProperty")),
                }).ToArray(),
                RunActions = of(typeof(AbilityEffectRunAction)).Select(c => Nodes(((AbilityEffectRunAction)c).Actions, 0)).ToArray(),
                OtherActions = cs.Where(c => c != null && c.GetType() != typeof(AbilityEffectRunAction))
                    .SelectMany(c => ActionLists(c).SelectMany(l => Nodes(l, 0) ?? new ActionNode[0])).ToArray(),
            };
        }

        static ActionNode[] Nodes(ActionList l, int depth)
        {
            if (l == null || l.Actions == null) return null; // 목록 자체가 없음(계약이 필요한 곳에서 위반으로 센다)
            if (depth > MaxActionDepth) throw new InvalidOperationException("BlastMagus: action tree deeper than " + MaxActionDepth + " (cycle?)");
            return l.Actions.Select(a => Node(a, depth)).ToArray();
        }

        static ActionNode Node(GameAction a, int depth)
        {
            if (a == null) return new ActionNode { Type = "null" };
            var n = new ActionNode { Type = a.GetType().Name };
            if (a.GetType() == typeof(ContextActionDealDamage))
            {
                var dt = ((ContextActionDealDamage)a).DamageType;
                if (dt == null || dt.Physical == null) throw new InvalidOperationException("BlastMagus: " + a.name + " has no DamageType/Physical data");
                n.Damage = new DamageShape { Half = ((ContextActionDealDamage)a).Half, Type = dt.Type.ToString(), Energy = dt.Energy.ToString(), Form = dt.Physical.Form.ToString() };
            }
            else if (a.GetType() == typeof(Conditional))
            {
                var c = (Conditional)a;
                n.Conditions = c.ConditionsChecker == null || c.ConditionsChecker.Conditions == null ? null : c.ConditionsChecker.Conditions.Select(ConditionOf).ToArray();
                n.IfTrue = Nodes(c.IfTrue, depth + 1);
                n.IfFalse = Nodes(c.IfFalse, depth + 1);
            }
            else n.Children = ActionLists(a).SelectMany(l => Nodes(l, depth + 1) ?? new ActionNode[0]).ToArray();
            return n;
        }

        static ConditionShape ConditionOf(Condition q)
        {
            if (q == null) return new ConditionShape { Type = "null" };
            var s = new ConditionShape { Type = q.GetType().Name, Not = q.Not };
            var eq = q as IsEqual;
            if (eq != null && q.GetType() == typeof(IsEqual))
            {
                s.FirstValueType = eq.FirstValue == null ? null : eq.FirstValue.GetType().Name;
                s.SecondValueType = eq.SecondValue == null ? null : eq.SecondValue.GetType().Name;
                var k = eq.SecondValue as IntConstant;
                s.SecondInt = k == null ? (int?)null : k.Value;
            }
            return s;
        }

        // 객체의 ActionList 형 인스턴스 필드 값(필드 이름 순, null 필드는 목록 없음 = 건너뜀). jbp 쪽 시험 추출기는 「값이 {Actions:[…]} 인 키」를 같은 규칙으로 돈다.
        static IEnumerable<ActionList> ActionLists(object o)
        {
            var fields = new List<FieldInfo>();
            for (var t = o.GetType(); t != null && t != typeof(object); t = t.BaseType)
                fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(f => f.FieldType == typeof(ActionList)));
            return fields.OrderBy(f => f.Name, StringComparer.Ordinal).Select(f => (ActionList)f.GetValue(o)).Where(l => l != null).ToList();
        }

        // 참조 → GUID("N"). 빈 참조(Guid == BlueprintGuid.Empty) = null(「없음」 — 계약이 그 칸을 요구하면 위반으로 센다). 해석은 하지 않는다:
        // 엔진 IsEmpty 는 GUID 가 있어도 블루프린트를 못 찾으면 참(IL_0012~0020 GetBlueprint)이라 「빈 값」과 「해석 안 됨」을 섞는다 → 쓰지 않는다.
        // 계약은 GUID 를 행(해석된 블루프린트의 GUID)과 대조하므로 해석되지 않는 참조는 GUID 불일치로 드러난다. 데이터가 필요한 참조(무기)는 호출자가 해석하고 실패면 던진다.
        static string RefGuid(object o)
        {
            if (o == null) return null;
            var r = o as BlueprintReferenceBase;
            if (r == null) throw new InvalidCastException("BlastMagus: expected a blueprint reference, got " + o.GetType().Name);
            return r.Guid == BlueprintGuid.Empty ? null : r.Guid.ToString();
        }

        static string[] RefGuids(object o)
        {
            if (o == null) return null;
            var a = o as Array;
            if (a == null) throw new InvalidCastException("BlastMagus: expected a reference array, got " + o.GetType().Name);
            return a.Cast<object>().Select(RefGuid).ToArray();
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("BlastMagus: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
