using System;
using System.Collections.Generic;
using System.Linq;

namespace BlastMagus
{
     
    // 게임 안에서는 ElementModel.Extract 가 블루프린트에서, 오프라인 시험은 blueprints.zip 의 jbp JSON 에서 같은 DTO 를 만들어 같은 검사기를 실행한다.
    // 문자열 값 = 게임 쪽 GetType().Name / enum ToString() = jbp 의 "$type" 짧은 이름 / enum 문자열(같은 직렬화 이름). GUID = BlueprintGuid "N"(32자 소문자), 없음 = null.

    // 블루프린트 컴포넌트 하나. Type 외 칸은 그 형식일 때만 의미가 있다(AddFeatureIfHasFact: CheckedFact·Feature·Not, AddFacts: Facts). 다른 형식이면 null/false.
    public sealed class ComponentShape
    {
        public string Type;
        public string CheckedFact, Feature;
        public bool Not;
        public string[] Facts;
    }

    // AbilityShowIfCasterHasFact 하나.
    public sealed class ShowConditionShape
    {
        public string UnitFact;
        public bool Not;
    }

    // Conditional 의 조건 하나. IsEqual 이면 FirstValueType/SecondValueType(평가기 형식 이름), SecondValue 가 IntConstant 면 SecondInt.
    public sealed class ConditionShape
    {
        public string Type;
        public bool Not;
        public string FirstValueType, SecondValueType;
        public int? SecondInt;
    }

    // ContextActionDealDamage 의 피해 서술자 칸(유효 피해 유형 비교용). Energy/Form 은 쓰이지 않는 칸에도 값이 있다(jbp 기본값) — 비교는 EffectiveType 만.
    public sealed class DamageShape
    {
        public bool Half;
        public string Type, Energy, Form;
    }

    // 액션 트리 마디. Conditional = Conditions/IfTrue/IfFalse, ContextActionDealDamage = Damage, 그 밖 = Children(그 액션의 ActionList 형 필드들의 액션, 필드 이름 순).
    // null 배열 = 그 목록 자체가 없음(빈 목록과 다르다 — 계약이 필요한 곳에서 위반으로 센다).
    public sealed class ActionNode
    {
        public string Type;
        public DamageShape Damage;
        public ConditionShape[] Conditions;
        public ActionNode[] IfTrue, IfFalse;
        public ActionNode[] Children;
    }

    // ContextRankConfig 하나(m_Type, m_BaseValueType, m_Feature, m_FeatureList, m_CustomProperty).
    public sealed class RankConfigShape
    {
        public string Type, BaseValueType, Feature, CustomProperty;
        public string[] FeatureList;
    }

    // 능력(Base 또는 잎) 하나. 목록 칸은 「그 형식의 컴포넌트마다 한 항목」(개수 자체가 계약의 판정 대상).
    public sealed class AbilityShape
    {
        public string Guid;
        public string Parent;                      // m_Parent GUID, 없으면 null
        public bool SpellResistance;
        public string[][] VariantLists;            // AbilityVariants 컴포넌트마다 m_Variants GUID 목록
        public ShowConditionShape[] ShowConditions; // AbilityShowIfCasterHasFact 마다
        public int[] BurnCosts;                    // AbilityKineticist 마다 BlastBurnCost
        public string[] ProjectileWeaponTypes;     // AbilityDeliverProjectile 마다 m_Weapon 의 m_Type GUID(m_Weapon 또는 m_Type 참조가 빈 값이면 null 항목; 해석 안 되는 무기는 추출이 던진다)
        public RankConfigShape[] RankConfigs;      // ContextRankConfig 마다
        public ActionNode[][] RunActions;          // AbilityEffectRunAction 마다 Actions 최상위 목록
        public ActionNode[] OtherActions;          // 그 밖 컴포넌트의 ActionList 형 필드에 든 최상위 액션들(「능력 전체의 피해 액션」 계수용)
    }

    // 원소 하나의 외부 블루프린트 모양(계약 1번의 BlastFeature + 2~8번).
    public sealed class BlastShape
    {
        public string BlastFeatureGuid;
        public ComponentShape[] BlastFeatureComponents;
        public AbilityShape Base, Leaf;
        public string CompositeFeatureGuid;
        public ComponentShape[] CompositeFeatureComponents;
        public AbilityShape CompositeBase, CompositeLeaf;
    }

    // 우리 원소 특성(계약 1번의 우리 부분).
    public sealed class OurFeatureShape
    {
        public string Guid;
        public ComponentShape[] Components;
    }

    public static class BlastContract
    {
        // 계약 3번: Base 표시 조건의 사실 = ElementalFocusSelection(본편 단순 6 + 복합 4 Base 직독, 설계 §4.3-1a 3번).
        public const string ShowFact = Guids.Game.ElementalFocusSelection;
        // 계약 5번: 피해 주사위 랭크 원본 = KineticBlastFeature, 보너스 = KineticistMainStatProperty(정찰 11-08 §1·§3, 설계 §4.3-1a 5번).
        public const string RankFeature = Guids.Game.KineticBlastFeature;
        public const string MainStatProperty = Guids.Game.KineticistMainStatProperty;
        // 계약 6번: 단순 블라스트 번 = 0(본편 6종 잎·Base 의 AbilityKineticist.BlastBurnCost 직독, 설계 §4.3-1a 6번).
        public const int SimpleBurnCost = 0;
        // 계약 4번: 전달 레이어 비교 = IsEqual(DeliverEffectLayer, IntConstant 0)(본편 6종 직독, 설계 §4.3-1a 4번 「DeliverEffectLayer == 0」).
        public const int FirstDeliverLayer = 0;

        // 계약 7번: 투사체 무기 형식의 허용 집합(설계 §4.3-1a 7번, §4.7 D2). 매번 새 배열.
        public static string[] ProjectileWeaponTypes { get { return new[] { Guids.Game.KineticBlastEnergy, Guids.Game.KineticBlastPhysical }; } }

        // 유효 피해 유형(설계 §4.3-1a 4번): Type 과, Energy 면 Energy 이름, Physical 이면 Form. 나머지 칸은 보지 않는다.
        public static string EffectiveType(DamageShape d)
        {
            if (d == null) throw new ArgumentNullException("d");
            if (d.Type == "Energy") return "Energy/" + d.Energy;
            if (d.Type == "Physical") return "Physical/" + d.Form;
            return d.Type;
        }

        // 행이 기대하는 유효 피해 유형. 칸 배타는 DescriptorOf 가 검사한다.
        public static string EffectiveType(ElementRow r)
        {
            var d = ElementTable.DescriptorOf(r);
            return d.Kind == DamageKind.Energy ? "Energy/" + d.EnergyName : "Physical/" + d.PhysicalForm;
        }

        // 외부 계약(설계 §4.3-1a 0번: 1번의 BlastFeature + 2~8번). 위반 문구 목록(빈 배열 = 통과). 문구는 「<번호>: …」 로 시작한다.
        // 행에서 읽는 칸 = GUID 칸·SpellResistance·Kind/EnergyName/PhysicalForm 뿐. r.Id 는 읽지 않는다(R-PF4: 시험이 아직 enum 에 없는 원소의 시험용 행을 쓴다).
        public static string[] CheckExternal(BlastShape s, ElementRow r)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (r == null) throw new ArgumentNullException("r");
            var v = new List<string>();
            string want = EffectiveType(r);

            // 1. BlastFeature = AddFeatureIfHasFact{CheckedFact = Base, Feature = Base, Not = true} 정확히 1개(그 특성의 유일한 컴포넌트).
            if (s.BlastFeatureGuid != r.BlastFeatureGuid) v.Add("1: blast feature is " + s.BlastFeatureGuid + ", row says " + r.BlastFeatureGuid);
            var bf = s.BlastFeatureComponents;
            if (bf == null) v.Add("1: blast feature components missing");
            else if (bf.Length != 1 || bf[0] == null || bf[0].Type != "AddFeatureIfHasFact")
                v.Add("1: blast feature components [" + Types(bf) + "], expected exactly [AddFeatureIfHasFact]");
            else if (bf[0].CheckedFact != r.BaseGuid || bf[0].Feature != r.BaseGuid || !bf[0].Not)
                v.Add("1: blast feature AddFeatureIfHasFact{Checked " + bf[0].CheckedFact + ", Feature " + bf[0].Feature + ", Not " + bf[0].Not + "}, expected {" + r.BaseGuid + ", " + r.BaseGuid + ", True}");

            // 2. Base ↔ 잎 양방향, 잎은 변형 부모가 아님.
            CheckPair(v, "2", s.Base, s.Leaf, r.BaseGuid, r.AbilityGuid);
            // 3. Base 표시 조건.
            CheckShow(v, "3", s.Base);
            // 4. 전달 레이어 쌍.
            if (s.Leaf != null) CheckLayerPair(v, s.Leaf, want);
            // 5. 랭크·주능력 배선.
            if (s.Leaf != null) CheckRanks(v, s.Leaf);
            // 6. 단순 번 0(잎·Base).
            CheckBurn(v, "6", "leaf", s.Leaf, SimpleBurnCost);
            CheckBurn(v, "6", "base", s.Base, SimpleBurnCost);
            // 7. SR·투사체 형식.
            if (s.Leaf != null)
            {
                if (s.Leaf.SpellResistance != r.SpellResistance) v.Add("7: leaf SpellResistance " + s.Leaf.SpellResistance + ", row says " + r.SpellResistance);
                var pt = s.Leaf.ProjectileWeaponTypes;
                if (pt == null || pt.Length != 1) v.Add("7: leaf has " + (pt == null ? "no" : pt.Length.ToString()) + " AbilityDeliverProjectile, expected exactly 1");
                else if (pt[0] == null) v.Add("7: leaf projectile weapon reference (or its m_Type) is empty");
                else if (!ProjectileWeaponTypes.Contains(pt[0])) v.Add("7: leaf projectile weapon type " + pt[0] + " is not KineticBlastEnergy/KineticBlastPhysical");
                else if (pt[0] != r.ProjectileWeaponTypeGuid) v.Add("7: leaf projectile weapon type " + pt[0] + ", row says " + r.ProjectileWeaponTypeGuid);
            }
            // 8. 복합 사슬(피해 모양은 적용하지 않는다 — §4.3-1a 4번 끝).
            if (s.CompositeFeatureGuid != r.CompositeFeatureGuid) v.Add("8: composite feature is " + s.CompositeFeatureGuid + ", row says " + r.CompositeFeatureGuid);
            var cf = s.CompositeFeatureComponents;
            if (cf == null) v.Add("8: composite feature components missing");
            else if (!cf.Any(c => c != null && c.Type == "AddFacts" && c.Facts != null && c.Facts.Contains(r.CompositeBaseGuid)))
                v.Add("8: composite feature has no AddFacts containing the composite base " + r.CompositeBaseGuid + " (components [" + Types(cf) + "])");
            CheckPair(v, "8", s.CompositeBase, s.CompositeLeaf, r.CompositeBaseGuid, r.CompositeAbilityGuid);
            CheckShow(v, "8", s.CompositeBase);
            CheckBurn(v, "8", "composite leaf", s.CompositeLeaf, BlastRules.CompositeBurnReduction);
            return v.ToArray();
        }

        // 우리 특성 계약(설계 §4.3-1a 0·1번 + 2026-11-15 에너지 주입 판정): 우리 원소 특성 = AddFacts[BlastFeature, 그 원소의 에너지 주입 자식 토글] 정확히 하나
        // (그 특성의 유일한 컴포넌트, 목록 = 이 둘, 이 순서). 하나의 AddFacts 로 두는 이유: 컴포넌트 이름($AddFacts$<guid>#0)이 예전 세이브와 같아
        // 로드 시 AddFacts.UpdateFacts 가 빠진 자식 토글만 더한다(Energize.cs 「예전 세이브」). 자식 토글 GUID 는 호출자가 EnergizeTable 에서 넘긴다(R-PF4: 계약은 r.Id 를 읽지 않는다).
        public static string[] CheckOurFeature(OurFeatureShape s, ElementRow r, string energizeToggleGuid)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (r == null) throw new ArgumentNullException("r");
            if (string.IsNullOrEmpty(energizeToggleGuid)) throw new ArgumentException("BlastMagus: CheckOurFeature needs the element's energize child toggle GUID", "energizeToggleGuid");
            var v = new List<string>();
            if (s.Guid != r.OurFeatureGuid) v.Add("our feature is " + s.Guid + ", row says " + r.OurFeatureGuid);
            var c = s.Components;
            if (c == null) v.Add("our feature components missing");
            else if (c.Length != 1 || c[0] == null || c[0].Type != "AddFacts")
                v.Add("our feature components [" + Types(c) + "], expected exactly [AddFacts]");
            else if (c[0].Facts == null || c[0].Facts.Length != 2 || c[0].Facts[0] != r.BlastFeatureGuid || c[0].Facts[1] != energizeToggleGuid)
                v.Add("our feature AddFacts [" + (c[0].Facts == null ? "null" : string.Join(",", c[0].Facts)) + "], expected exactly [" + r.BlastFeatureGuid + "," + energizeToggleGuid + "]");
            return v.ToArray();
        }

        static string Types(ComponentShape[] cs) { return string.Join(", ", cs.Select(c => c == null ? "null" : c.Type)); }

        static void CheckPair(List<string> v, string n, AbilityShape b, AbilityShape leaf, string baseGuid, string leafGuid)
        {
            if (b == null) { v.Add(n + ": base " + baseGuid + " missing"); return; }
            if (leaf == null) { v.Add(n + ": leaf " + leafGuid + " missing"); return; }
            if (b.Guid != baseGuid) v.Add(n + ": base is " + b.Guid + ", row says " + baseGuid);
            if (leaf.Guid != leafGuid) v.Add(n + ": leaf is " + leaf.Guid + ", row says " + leafGuid);
            if (b.VariantLists == null || b.VariantLists.Length != 1 || b.VariantLists[0] == null)
                v.Add(n + ": base has " + (b.VariantLists == null ? "no" : b.VariantLists.Length.ToString()) + " AbilityVariants, expected exactly 1");
            else if (!b.VariantLists[0].Contains(leafGuid)) v.Add(n + ": base m_Variants does not contain the leaf " + leafGuid);
            if (leaf.Parent != baseGuid) v.Add(n + ": leaf m_Parent is " + (leaf.Parent ?? "null") + ", expected the base " + baseGuid);
            if (leaf.VariantLists == null || leaf.VariantLists.Length != 0) v.Add(n + ": leaf is a variant parent (AbilityVariants " + (leaf.VariantLists == null ? "unknown" : leaf.VariantLists.Length.ToString()) + ")");
        }

        static void CheckShow(List<string> v, string n, AbilityShape b)
        {
            if (b == null) return; // CheckPair 가 이미 보고했다
            var sc = b.ShowConditions;
            if (sc == null || sc.Length != 1 || sc[0] == null) { v.Add(n + ": base has " + (sc == null ? "no" : sc.Length.ToString()) + " AbilityShowIfCasterHasFact, expected exactly 1"); return; }
            if (sc[0].UnitFact != ShowFact) v.Add(n + ": base show condition fact " + sc[0].UnitFact + ", expected ElementalFocusSelection " + ShowFact);
            if (sc[0].Not) v.Add(n + ": base show condition Not = true (the blast would hide for the D1 fact holder)");
        }

        static void CheckBurn(List<string> v, string n, string what, AbilityShape a, int want)
        {
            if (a == null) return; // CheckPair 가 이미 보고했다
            if (a.BurnCosts == null || a.BurnCosts.Length != 1) v.Add(n + ": " + what + " has " + (a.BurnCosts == null ? "no" : a.BurnCosts.Length.ToString()) + " AbilityKineticist, expected exactly 1");
            else if (a.BurnCosts[0] != want) v.Add(n + ": " + what + " BlastBurnCost " + a.BurnCosts[0] + ", expected " + want);
        }

        static void CheckLayerPair(List<string> v, AbilityShape leaf, string want)
        {
            var ra = leaf.RunActions;
            if (ra == null || ra.Length != 1 || ra[0] == null) { v.Add("4: leaf has " + (ra == null ? "no" : ra.Length.ToString()) + " AbilityEffectRunAction, expected exactly 1"); return; }
            var conds = ra[0].Where(a => a != null && a.Type == "Conditional").ToArray();
            if (conds.Length != 1) { v.Add("4: leaf top-level actions [" + string.Join(", ", ra[0].Select(a => a == null ? "null" : a.Type)) + "] have " + conds.Length + " Conditional, expected exactly 1"); return; }
            var c = conds[0];
            var cc = c.Conditions;
            if (cc == null || cc.Length != 1 || cc[0] == null || cc[0].Type != "IsEqual" || cc[0].Not || cc[0].FirstValueType != "DeliverEffectLayer" || cc[0].SecondValueType != "IntConstant" || cc[0].SecondInt != FirstDeliverLayer)
                v.Add("4: layer condition is not exactly IsEqual(DeliverEffectLayer, IntConstant " + FirstDeliverLayer + ") with Not = false");
            var t = DirectDamage(c.IfTrue);
            var f = DirectDamage(c.IfFalse);
            if (t == null || t.Length != 1 || t[0].Half) v.Add("4: IfTrue has " + (t == null ? "no list" : t.Length + " ContextActionDealDamage") + ", expected exactly 1 with Half = false" + (t != null && t.Length == 1 ? " (got Half = true)" : ""));
            if (f == null || f.Length != 1 || !f[0].Half) v.Add("4: IfFalse has " + (f == null ? "no list" : f.Length + " ContextActionDealDamage") + ", expected exactly 1 with Half = true" + (f != null && f.Length == 1 ? " (got Half = false)" : ""));
            int total = ra[0].Sum(a => CountDamage(a)) + (leaf.OtherActions == null ? 0 : leaf.OtherActions.Sum(a => CountDamage(a)));
            if (total != 2) v.Add("4: leaf has " + total + " ContextActionDealDamage in all its action lists, expected exactly 2 (the layer pair)");
            foreach (var d in new[] { t, f })
                if (d != null && d.Length == 1 && EffectiveType(d[0]) != want)
                    v.Add("4: damage effective type " + EffectiveType(d[0]) + ", row says " + want);
        }

        static DamageShape[] DirectDamage(ActionNode[] list)
        {
            if (list == null) return null;
            return list.Where(a => a != null && a.Type == "ContextActionDealDamage").Select(a => a.Damage).ToArray();
        }

        static int CountDamage(ActionNode a)
        {
            if (a == null) return 0;
            int n = a.Type == "ContextActionDealDamage" ? 1 : 0;
            foreach (var l in new[] { a.IfTrue, a.IfFalse, a.Children })
                if (l != null) n += l.Sum(x => CountDamage(x));
            return n;
        }

        static void CheckRanks(List<string> v, AbilityShape leaf)
        {
            var rc = leaf.RankConfigs;
            if (rc == null) { v.Add("5: leaf rank configs missing"); return; }
            var dice = rc.Where(x => x != null && x.Type == "DamageDice").ToArray();
            var bonus = rc.Where(x => x != null && x.Type == "DamageBonus").ToArray();
            if (dice.Length != 1) v.Add("5: leaf has " + dice.Length + " DamageDice rank configs, expected exactly 1");
            else
            {
                var d = dice[0];
                bool fromRank = d.BaseValueType == "FeatureRank" && d.Feature == RankFeature;
                bool fromList = d.BaseValueType == "FeatureListRanks" && d.FeatureList != null && d.FeatureList.Contains(RankFeature);
                if (!fromRank && !fromList) v.Add("5: DamageDice source " + d.BaseValueType + " is not FeatureRank/FeatureListRanks of KineticBlastFeature " + RankFeature);
            }
            if (bonus.Length != 1) v.Add("5: leaf has " + bonus.Length + " DamageBonus rank configs, expected exactly 1");
            else if (bonus[0].BaseValueType != "CustomProperty" || bonus[0].CustomProperty != MainStatProperty)
                v.Add("5: DamageBonus source " + bonus[0].BaseValueType + " " + bonus[0].CustomProperty + " is not CustomProperty(KineticistMainStatProperty " + MainStatProperty + ")");
        }
    }

    // 원소 선택지 항목(우리 원소 특성)의 아이콘 출처(핫픽스 1, 2026-11-08 — 현장 보고 「KEE stage 2 FAILED (stage A: BlastMagus: TelekineticBlastFeature has no icon)」).
    // 아이콘이 필요한 곳 = UI 가 보여 주는 우리 원소 특성뿐. None = 출처 없음(게임 쪽 BlastBlueprints.ElementIconOf 가 던진다 = 2단계 A 실패 PresentFailed / 1단계 BuildFailed).
    public enum IconSource { Feature, Base, None }

    public static class ElementIcon
    {
         
         
         
         
        // 세 번째 출처(우리 대체 스프라이트)는 두지 않는다: 원소별로 인용할 수 있는 본편 블루프린트 아이콘이 없다(텔레키네틱·중력·나무·양·음은 본편 원소가 아니다). 전부 null = None.
        public static IconSource[] OrderFor(ElementSource s)
        {
            switch (s)
            {
                case ElementSource.Vanilla: return new[] { IconSource.Feature };
                case ElementSource.Kee: return new[] { IconSource.Feature, IconSource.Base };
                default: throw new ArgumentOutOfRangeException("s", s, "BlastMagus: undefined ElementSource");
            }
        }

        public static IconSource Pick(ElementSource s, bool featureHas, bool baseHas)
        {
            foreach (var o in OrderFor(s))
            {
                if (o == IconSource.Feature && featureHas) return o;
                if (o == IconSource.Base && baseHas) return o;
            }
            return IconSource.None;
        }

        // INFO 줄(원소마다, 쓰인 출처를 밝힌다). None 은 쓰인 출처가 아니므로 던진다.
        public static string InfoLine(string element, IconSource s, string blueprintName)
        {
            if (string.IsNullOrEmpty(element)) throw new ArgumentException("BlastMagus: ElementIcon.InfoLine needs the element");
            if (string.IsNullOrEmpty(blueprintName)) throw new ArgumentException("BlastMagus: ElementIcon.InfoLine needs the source blueprint name");
            string what;
            switch (s)
            {
                case IconSource.Feature: what = "blast feature"; break;
                case IconSource.Base: what = "base ability"; break;
                default: throw new ArgumentOutOfRangeException("s", s, "BlastMagus: ElementIcon.InfoLine for a non-source (" + s + ")");
            }
            return "KEE icon: " + element + " from " + what + " " + blueprintName;
        }
    }
}
