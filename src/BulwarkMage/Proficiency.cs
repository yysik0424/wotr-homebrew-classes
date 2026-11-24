using System;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
    // 중장 숙련(설계 §5.3(1)(2), 정찰 §4) — 1레벨 특성 하나:
    //   AddProficiencies{ArmorProficiencies = 경·중·중갑 + 버클러·경방패·중방패·타워실드}
    //   ArcaneArmorProficiency{Armor = 같은 7군}: 본편 OnEventAboutToTrigger(RuleCalculateArcaneSpellFailureChance) 가 갑옷 군이 목록에 있으면 IgnoreArmor,
    //   보조손 방패 군이 목록에 있으면 IgnoreShield(정찰 §4 @0006~008A). 선례 = 바드 숙련 fa3d3b22…(Light + 방패 4군). 유닛 단위(다른 비전 주문서에도 적용 — 기본값 Q8, README).
    // 숙련군 이름 표 = BodyTable.ArmorProficiencyGroups(한 곳). 우리 블루프린트라 컴포넌트 정확 대조가 안전하다(다른 모드는 우리 GUID 를 모른다).
    public static class Proficiency
    {
        public static BlueprintFeature Feature;

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("BulwarkMage: Proficiency.Build() called twice");
            var groups = BodyTable.ArmorProficiencyGroups.Select(Parse).ToArray();
            if (groups.Length == 0 || groups.Distinct().Count() != groups.Length) throw new InvalidOperationException("BulwarkMage: armor proficiency group table is empty or has duplicates");
            Feature = Create<BlueprintFeature>("BulwarkProficiencyFeature", Guids.ProficiencyFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("prof.name"));
                SetField(f, "m_Description", Strings.L("prof.desc"));
                SetField(f, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                f.IsClassFeature = true;
                f.Ranks = 1;
                f.Groups = new FeatureGroup[0];
                f.ReapplyOnLevelUp = false;
            });
            var add = new AddProficiencies { ArmorProficiencies = groups.ToArray(), WeaponProficiencies = new WeaponCategory[0] };
            var asf = new ArcaneArmorProficiency { Armor = groups.ToArray() };
            AddComponents(Feature, add, asf);
            // 되읽기: 우리 블루프린트의 컴포넌트 = 정확히 이 둘(우리 GUID 라 외부 모드가 붙일 수 없다 — 「정확히 N개」 금지 규칙은 본편 블루프린트 대상).
            var comps = Feature.ComponentsArray;
            if (comps.Length != 2 || !ReferenceEquals(comps[0], add) || !ReferenceEquals(comps[1], asf))
                throw new InvalidOperationException("BulwarkMage: proficiency feature components readback failed (" + string.Join(", ", comps.Select(c => c == null ? "null" : c.GetType().Name).ToArray()) + ")");
            if (!add.ArmorProficiencies.SequenceEqual(groups) || !asf.Armor.SequenceEqual(groups) || add.WeaponProficiencies.Length != 0)
                throw new InvalidOperationException("BulwarkMage: proficiency arrays readback failed");
            Archetype.AddAt(1, Feature);
        }

        static ArmorProficiencyGroup Parse(string name)
        {
            if (string.IsNullOrEmpty(name) || !Enum.IsDefined(typeof(ArmorProficiencyGroup), name))
                throw new InvalidOperationException("BulwarkMage: '" + name + "' is not an ArmorProficiencyGroup name in this game version");
            return (ArmorProficiencyGroup)Enum.Parse(typeof(ArmorProficiencyGroup), name);
        }
    }
}
