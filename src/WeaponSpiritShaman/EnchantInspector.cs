using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
     
    // 입력은 평평한 모양(EnchShape) — 게임 쪽 추출기와 시험 추출기가 같은 필드를 채워야 한다(T5 S21 `census-representatives` 가 동등성 확인).
    //   ComponentTypes      = 블루프린트 Components[i] 의 형식 짧은 이름(jbp "$type" 의 쉼표 뒤), 순서 그대로.
    //   InnerTypes          = 각 컴포넌트 안(컴포넌트 자신 제외)의 모든 중첩 객체 형식 짧은 이름(조건·액션 트리 포함).
    //   TrueFlags           = 각 컴포넌트 안 어느 깊이든 이름이 SpiritRules.WielderFlagNames 인 필드 중 값이 true 인 것의 이름.
    //   NonSimpleValueTypes = 각 컴포넌트 안 어느 깊이든 이름이 "ValueType" 인 필드 중 값이 "Simple" 이 아닌 것의 값.
    //   ComponentFacts      = 형식 이름으로 가를 수 없는 컴포넌트 설정(2026-11-18 흡수 범위 확대) — ComponentFacts.Of 한 함수가 두 추출기 공통으로 만든다(아래).
    public sealed class EnchShape
    {
        public string Guid, Name;
        public string[] ComponentTypes, InnerTypes, TrueFlags, NonSimpleValueTypes, ComponentFacts;
    }

    public enum Verdict
    {
        Empty,
        MixedEnhancement,
        Enhancement,
        NotMagic,
        Unabsorbable,
        TriggerNotPortable,
        WielderDependent,
        ContextValueDependent,
        HelperOnly,
        Accepted,
        // 2026-11-18 흡수 범위 확대(소유자 결정): 적중 시 발동형(거울·강신, 대상 무기 대조 — 계획기 TriggerFit) · 장비 효과형(강신만) · 장비 효과형 중 막는 것.
        Trigger,
        EquipmentEffect,
        EquipmentNotPortable,
    }

    // [순수] 컴포넌트 설정 사실(EnchShape.ComponentFacts). 두 추출기(jbp 사전 · 살아 있는 객체 리플렉션)가 필드 읽기만 IFieldSource 로 넘기고 이 함수가 사실 문자열을 만든다(C4).
    // 문자열 = "<컴포넌트 번호>:<키>" 또는 "<번호>:<키>=<값>". 트리거 컴포넌트는 항상 "<i>:Trigger" 를 먼저 낸다(검사기가 읽힘을 단언 — 빠지면 throw).
    //  트리거 무기 조건(SpiritRules.TriggerTypes 주석의 IL 위치): WeaponType=<BlueprintWeaponType GUID>, Category=<WeaponCategory>, RangeType=<WeaponRangeType>,
    //   Group=<WeaponFighterGroup>, NaturalOrUnarmed, WeaponBlueprint=<BlueprintItemWeapon GUID 또는 빈 값>, 그리고 대상 무기로 판정 불가: Duelist, PhysicalDamageForm.
    //  AddFactContextActions: FactActionsDeactivated=<최상위 동작 형식, 쉼표>, FactActionsNewRound=<같은 모양>(빈 목록 = "=" 뒤 빈 문자열).
    public static class ComponentFacts
    {
        public const string Trigger = "Trigger", WeaponType = "WeaponType", Category = "Category", RangeType = "RangeType", Group = "Group",
            NaturalOrUnarmed = "NaturalOrUnarmed", WeaponBlueprint = "WeaponBlueprint", Duelist = "Duelist", PhysicalDamageForm = "PhysicalDamageForm",
            Deactivated = "FactActionsDeactivated", NewRound = "FactActionsNewRound";
        public const string FactActionsType = "AddFactContextActions";
        // 트리거 형식 이름(SpiritRules.TriggerTypes 와 같은 집합 — 시험 `exec: component facts` 가 대조).
        public const string WeaponTriggerType = "AddInitiatorAttackWithWeaponTrigger", AttackRollTriggerType = "AddInitiatorAttackRollTrigger", DiceTriggerType = "AdditionalDiceOnAttack";

        // 필드 읽기. 없는 필드(형식에 없음)는 구현이 던진다. RefGuid = 참조가 없거나 GUID 가 비었으면 null(엔진 get_WeaponType: 참조 없음 = 조건 없음).
        public interface IFieldSource
        {
            bool Bool(string field);
            string EnumName(string field);
            string RefGuid(string field);
            string[] ActionTypes(string actionListField); // ActionList 의 최상위 동작 형식 이름(없음/null = 빈 배열)
        }

        static readonly string NullGuid = new string('0', 32);

        static string Ref(IFieldSource f, string field)
        {
            var g = f.RefGuid(field);
            return string.IsNullOrEmpty(g) || g == NullGuid ? null : g;
        }

        public static IEnumerable<string> Of(int index, string componentType, IFieldSource f)
        {
            if (componentType == null) throw new ArgumentNullException("componentType");
            if (f == null) throw new ArgumentNullException("f");
            string p = index + ":";
            var r = new List<string>();
            if (componentType == WeaponTriggerType || componentType == DiceTriggerType)
            {
                r.Add(p + Trigger);
                var wt = Ref(f, "m_WeaponType");
                if (wt != null) r.Add(p + WeaponType + "=" + wt);
                if (f.Bool("CheckWeaponCategory")) r.Add(p + Category + "=" + f.EnumName("Category"));
                if (f.Bool("CheckWeaponRangeType")) r.Add(p + RangeType + "=" + f.EnumName("RangeType"));
                if (f.Bool("CheckWeaponGroup")) r.Add(p + Group + "=" + f.EnumName("Group"));
                if (f.Bool("AllNaturalAndUnarmed")) r.Add(p + NaturalOrUnarmed);
                if (f.Bool("DuelistWeapon")) r.Add(p + Duelist);
                if (componentType == WeaponTriggerType)
                {
                    if (f.Bool("CheckWeaponBlueprint")) r.Add(p + WeaponBlueprint + "=" + (Ref(f, "m_WeaponBlueprint") ?? ""));
                    if (f.Bool("CheckPhysicalDamageForm")) r.Add(p + PhysicalDamageForm);
                }
            }
            else if (componentType == AttackRollTriggerType)
            {
                r.Add(p + Trigger);
                if (f.Bool("CheckWeapon")) r.Add(p + Category + "=" + f.EnumName("WeaponCategory"));
            }
            else if (componentType == FactActionsType)
            {
                r.Add(p + Deactivated + "=" + string.Join(",", f.ActionTypes("Deactivated")));
                r.Add(p + NewRound + "=" + string.Join(",", f.ActionTypes("NewRound")));
            }
            return r;
        }

        public sealed class Fact
        {
            public readonly int Index;
            public readonly string Key, Value; // Value = null 은 값 없는 사실
            public Fact(int index, string key, string value) { Index = index; Key = key; Value = value; }
        }

        public static Fact Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) throw new ArgumentException("ComponentFacts: empty fact");
            int c = s.IndexOf(':');
            int idx;
            if (c <= 0 || !int.TryParse(s.Substring(0, c), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out idx)) throw new ArgumentException("ComponentFacts: fact '" + s + "' has no component index");
            var rest = s.Substring(c + 1);
            int e = rest.IndexOf('=');
            return e < 0 ? new Fact(idx, rest, null) : new Fact(idx, rest.Substring(0, e), rest.Substring(e + 1));
        }
    }

    // 계획기가 트리거를 얹을 무기(거울 = 영병 대상 무기, 강신 = 주술사 대상 무기). 무기 조건 대조에 쓰는 엔진 값을 게임 쪽이 엔진 함수로 채운다(GameMirrorEffects.CarrierOf).
    public sealed class CarrierWeapon
    {
        public TargetKind Kind;          // NonWeapon 이면 아래는 비어 있어야 한다(무기 아님 — 트리거를 얹을 수 없음)
        public string BlueprintGuid;     // BlueprintItemWeapon
        public string TypeGuid;          // BlueprintItemWeapon.Type
        public string Category;          // BlueprintItemWeapon.Category (WeaponCategory 이름)
        public string[] FighterGroups;   // WeaponFighterGroupHelper.Contains(FighterGroup, g) 가 참인 WeaponFighterGroup 이름 전부
        public string[] RangeTypes;      // WeaponRangeTypeHelper.IsSuitableWeapon(r, 무기) 가 참인 WeaponRangeType 이름 전부
        public bool IsNatural, IsUnarmed; // Type.IsNatural / Type.IsUnarmed

        public void Validate(string owner)
        {
            if (!Enum.IsDefined(typeof(TargetKind), Kind)) throw new ArgumentException(owner + ": carrier has undefined kind " + (int)Kind);
            bool any = BlueprintGuid != null || TypeGuid != null || Category != null || FighterGroups != null || RangeTypes != null || IsNatural || IsUnarmed;
            if (Kind == TargetKind.NonWeapon) { if (any) throw new ArgumentException(owner + ": NonWeapon carrier must not carry weapon facts"); return; }
            if (string.IsNullOrEmpty(BlueprintGuid) || string.IsNullOrEmpty(TypeGuid) || string.IsNullOrEmpty(Category) || FighterGroups == null || RangeTypes == null)
                throw new ArgumentException(owner + ": weapon carrier (" + Kind + ") has a missing fact (blueprint/type/category/groups/range types)");
        }
    }

    public sealed class FitResult
    {
        public readonly bool Ok;
        public readonly string Detail; // 맞지 않으면 어느 트리거의 어느 조건인지
        public FitResult(bool ok, string detail) { Ok = ok; Detail = detail; }
    }

    // [순수] 트리거 무기 조건 ↔ 대상 무기 대조(엔진 AbstractWeaponTrigger.IsSuitable 등과 같은 조건 — SpiritRules.TriggerTypes 주석).
    // 트리거가 하나라도 대상 무기에서 돌지 않으면 맞지 않음(일부만 도는 마법 부여 = 원래 효과와 다름 → 보수적으로 거부).
    public static class TriggerFit
    {
        public static FitResult Check(EnchShape s, CarrierWeapon carrier)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (carrier == null) throw new ArgumentNullException("carrier");
            var facts = EnchantInspector.Facts(s);
            var trig = facts.Where(f => f.Key == ComponentFacts.Trigger).Select(f => f.Index).ToArray();
            if (trig.Length == 0) return new FitResult(true, null);
            if (carrier.Kind == TargetKind.NonWeapon) return new FitResult(false, "carrier is not a weapon");
            foreach (var f in facts.Where(x => trig.Contains(x.Index) && x.Key != ComponentFacts.Trigger))
            {
                string why = null;
                switch (f.Key)
                {
                    case ComponentFacts.WeaponType: if (carrier.TypeGuid != f.Value) why = "weapon type " + f.Value + " != " + carrier.TypeGuid; break;
                    case ComponentFacts.Category: if (carrier.Category != f.Value) why = "category " + f.Value + " != " + carrier.Category; break;
                    case ComponentFacts.RangeType: if (!carrier.RangeTypes.Contains(f.Value)) why = "range type " + f.Value + " not in [" + string.Join(",", carrier.RangeTypes) + "]"; break;
                    case ComponentFacts.Group: if (!carrier.FighterGroups.Contains(f.Value)) why = "fighter group " + f.Value + " not in [" + string.Join(",", carrier.FighterGroups) + "]"; break;
                    case ComponentFacts.NaturalOrUnarmed: if (!carrier.IsNatural && !carrier.IsUnarmed) why = "natural/unarmed only"; break;
                    case ComponentFacts.WeaponBlueprint: if (carrier.BlueprintGuid != f.Value) why = "weapon blueprint " + (f.Value == "" ? "(none)" : f.Value) + " != " + carrier.BlueprintGuid; break;
                    default: throw new InvalidOperationException("TriggerFit: " + s.Guid + " trigger " + f.Index + " has filter '" + f.Key + "' the fit check does not know (inspector should have rejected it)");
                }
                if (why != null) return new FitResult(false, "trigger " + f.Index + " " + why);
            }
            return new FitResult(true, null);
        }
    }

    public sealed class InspectResult
    {
        public readonly Verdict Verdict;
        public readonly string Reason; // 사람이 읽는 사유(형식 이름 포함) — 로그용
        public InspectResult(Verdict v, string reason) { Verdict = v; Reason = reason; }
        public override string ToString() { return Verdict + (Reason == null ? "" : " (" + Reason + ")"); }
    }

    public static class EnchantInspector
    {
        static readonly HashSet<string> Rule = new HashSet<string>(SpiritRules.RuleTypes);
        static readonly HashSet<string> Helper = new HashSet<string>(SpiritRules.HelperTypes);
        static readonly HashSet<string> Trigger = new HashSet<string>(SpiritRules.TriggerTypes);
        static readonly HashSet<string> Wielder = new HashSet<string>(SpiritRules.WielderTypes);
        static readonly HashSet<string> NotMagic = new HashSet<string>(SpiritRules.NotMagicGuids);
        static readonly HashSet<string> FlagNames = new HashSet<string>(SpiritRules.WielderFlagNames);
        static readonly HashSet<string> Equipment = new HashSet<string>(SpiritRules.EquipmentTypes);

        // 모양의 ComponentFacts 를 읽고 모양 규칙을 단언한다: 번호가 컴포넌트 범위 안, 트리거 컴포넌트마다 "<i>:Trigger" 정확히 1개(트리거 아닌 번호에는 없음),
        // AddFactContextActions 마다 Deactivated·NewRound 각 1개. 어긋나면 추출기가 덜 읽은 것 → throw(조용히 「조건 없음」으로 읽지 않는다 — C1).
        public static List<ComponentFacts.Fact> Facts(EnchShape s)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (s.ComponentFacts == null || s.ComponentTypes == null) throw new ArgumentException("EnchantInspector: shape " + s.Guid + " has a null array (extractor must fill every field, empty = none)");
            var facts = s.ComponentFacts.Select(ComponentFacts.Parse).ToList();
            for (int i = 0; i < s.ComponentTypes.Length; i++)
            {
                var t = s.ComponentTypes[i];
                int marks = facts.Count(f => f.Index == i && f.Key == ComponentFacts.Trigger);
                bool trig = Trigger.Contains(t);
                if (marks != (trig ? 1 : 0)) throw new ArgumentException("EnchantInspector: shape " + s.Guid + " component " + i + " (" + t + ") has " + marks + " trigger mark(s), expected " + (trig ? 1 : 0) + " (extractor did not read its trigger settings)");
                bool afca = t == ComponentFacts.FactActionsType;
                foreach (var k in new[] { ComponentFacts.Deactivated, ComponentFacts.NewRound })
                {
                    int n = facts.Count(f => f.Index == i && f.Key == k);
                    if (n != (afca ? 1 : 0)) throw new ArgumentException("EnchantInspector: shape " + s.Guid + " component " + i + " (" + t + ") has " + n + " '" + k + "' fact(s), expected " + (afca ? 1 : 0));
                }
            }
            var stray = facts.FirstOrDefault(f => f.Index < 0 || f.Index >= s.ComponentTypes.Length);
            if (stray != null) throw new ArgumentException("EnchantInspector: shape " + s.Guid + " has a fact for component " + stray.Index + " (only " + s.ComponentTypes.Length + " components)");
            return facts;
        }

        // 분류(순서대로 첫 일치 — 계획 T2, 2026-11-18 흡수 범위 확대로 개정):
        // Empty → MixedEnhancement → Enhancement → NotMagic → Unabsorbable(알려진 목록 밖 형식 ≥1, 미지 형식 포함 — 장비 효과형은 이제 목록 안)
        // → EquipmentNotPortable(SpiritRules.EquipmentBlocked · AddFactContextActions 모양) → TriggerNotPortable(대상 무기로 판정 불가한 트리거 조건)
        // → EquipmentEffect(장비 효과형 ≥1) → WielderDependent(소지자 의존 형식) → Trigger(트리거형 ≥1)
        // → WielderDependent(TrueFlags · InnerTypes 의 ContextConditionCaster*) → HelperOnly(무기 규칙형 없음 = 보조 형식만)
        // → ContextValueDependent(무기 규칙형 + 보조 형식 동반, 또는 NonSimpleValueTypes) → Accepted.
        // 읽기: 계획 문장의 ContextValueDependent 「보조 형식 동반」 은 무기 규칙형에 보조가 따라붙은 경우다(보조만이면 HelperOnly) — 본편 원장 HelperOnly 18 · ContextValue 0 과 일치(보고서 §4).
        public static InspectResult Inspect(EnchShape s)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (string.IsNullOrEmpty(s.Guid)) throw new ArgumentException("EnchantInspector: shape has no Guid");
            if (s.ComponentTypes == null || s.InnerTypes == null || s.TrueFlags == null || s.NonSimpleValueTypes == null)
                throw new ArgumentException("EnchantInspector: shape " + s.Guid + " has a null array (extractor must fill every field, empty = none)");
            if (s.ComponentTypes.Any(string.IsNullOrEmpty) || s.InnerTypes.Any(string.IsNullOrEmpty))
                throw new ArgumentException("EnchantInspector: shape " + s.Guid + " has an empty type name");
            var unknownFlag = s.TrueFlags.FirstOrDefault(f => !FlagNames.Contains(f));
            if (unknownFlag != null) throw new ArgumentException("EnchantInspector: shape " + s.Guid + " has TrueFlags entry '" + unknownFlag + "' that is not a wielder flag name");
            if (s.NonSimpleValueTypes.Any(v => string.IsNullOrEmpty(v) || v == SpiritRules.SimpleValueType))
                throw new ArgumentException("EnchantInspector: shape " + s.Guid + " NonSimpleValueTypes must list only non-'Simple' values");
            var facts = Facts(s);

            var ts = s.ComponentTypes;
            var set = new HashSet<string>(ts);
            if (ts.Length == 0) return new InspectResult(Verdict.Empty, "no components");
            bool hasEnh = set.Contains(SpiritRules.EnhancementComponentType);
            if (hasEnh && set.Count > 1) return new InspectResult(Verdict.MixedEnhancement, "WeaponEnhancementBonus + " + string.Join(",", set.Where(t => t != SpiritRules.EnhancementComponentType)));
            if (hasEnh) return new InspectResult(Verdict.Enhancement, null);
            if (NotMagic.Contains(s.Guid)) return new InspectResult(Verdict.NotMagic, "masterwork has no effect on an enhanced weapon");
            var unknown = ts.Where(t => !Rule.Contains(t) && !Helper.Contains(t) && !Trigger.Contains(t) && !Wielder.Contains(t) && !Equipment.Contains(t)).Distinct().ToArray();
            if (unknown.Length > 0) return new InspectResult(Verdict.Unabsorbable, string.Join(",", unknown));
            // 2026-11-18 흡수 범위 확대(소유자 결정). 장비 효과형 막기: GUID 목록(형식으로 못 가르는 것) → AddFactContextActions 모양(Deactivated 가 정리만 하는가).
            string blocked;
            if (SpiritRules.EquipmentBlocked.TryGetValue(s.Guid, out blocked)) return new InspectResult(Verdict.EquipmentNotPortable, blocked);
            foreach (var i in Enumerable.Range(0, ts.Length).Where(i => ts[i] == ComponentFacts.FactActionsType))
            {
                var de = facts.Single(f => f.Index == i && f.Key == ComponentFacts.Deactivated).Value;
                var nr = facts.Single(f => f.Index == i && f.Key == ComponentFacts.NewRound).Value;
                var deActs = de.Length == 0 ? new string[0] : de.Split(',');
                if (deActs.Length == 0 || deActs.Any(a => !SpiritRules.FactActionsCleanupTypes.Contains(a)) || nr.Length > 0)
                    return new InspectResult(Verdict.EquipmentNotPortable, "AddFactContextActions Deactivated [" + de + "] NewRound [" + nr + "]");
            }
            // 트리거 중 대상 무기로 판정 불가한 조건 → 보수적 거부(판정 불가 = 허용하지 않음).
            var unport = facts.Where(f => SpiritRules.TriggerUnportableFilters.Contains(f.Key)).Select(f => "trigger " + f.Index + " " + f.Key).ToArray();
            if (unport.Length > 0) return new InspectResult(Verdict.TriggerNotPortable, string.Join(",", unport));
            var eq = ts.Where(Equipment.Contains).Distinct().ToArray();
            if (eq.Length > 0) return new InspectResult(Verdict.EquipmentEffect, string.Join(",", eq));
            var wt = ts.Where(Wielder.Contains).Distinct().ToArray();
            if (wt.Length > 0) return new InspectResult(Verdict.WielderDependent, string.Join(",", wt));
            // 발동형: 그 안의 조건·값(시전자 조건·ContextRankConfig 등)은 트리거 문맥의 일부로 본다 — 거울에서는 펫이 시전자(소유자 결정 2026-11-18). 무기 조건은 계획기가 대조.
            var trig = ts.Where(Trigger.Contains).Distinct().ToArray();
            if (trig.Length > 0) return new InspectResult(Verdict.Trigger, string.Join(",", trig));
            if (s.TrueFlags.Length > 0) return new InspectResult(Verdict.WielderDependent, "flags " + string.Join(",", s.TrueFlags.Distinct()));
            var caster = s.InnerTypes.Where(t => t.StartsWith(SpiritRules.CasterConditionPrefix, StringComparison.Ordinal)).Distinct().ToArray();
            if (caster.Length > 0) return new InspectResult(Verdict.WielderDependent, string.Join(",", caster));
            bool hasRule = ts.Any(Rule.Contains);
            if (!hasRule) return new InspectResult(Verdict.HelperOnly, string.Join(",", set));
            var helpers = ts.Where(Helper.Contains).Distinct().ToArray();
            if (helpers.Length > 0) return new InspectResult(Verdict.ContextValueDependent, "helpers " + string.Join(",", helpers));
            if (s.NonSimpleValueTypes.Length > 0) return new InspectResult(Verdict.ContextValueDependent, "value types " + string.Join(",", s.NonSimpleValueTypes.Distinct()));
            return new InspectResult(Verdict.Accepted, null);
        }
    }
}
