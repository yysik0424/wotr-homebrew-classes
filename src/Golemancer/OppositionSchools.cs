using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
     
    // 선택 1개(GolemancerOppositionSchoolSelection, 본편 OppositionSchoolSelection 값 복사 + Ranks 3) + 학파 특성 7(변환 제외). 1레벨에 선택을 3번 둔다(Archetype.AddAt count 3).
    // 「같은 학파 중복 불가」는 엔진 기본: 학파 특성 Ranks 1 → CanSelect 랭크 검사 @0083~009B 가 false(점검 S1 duplicate-school-rejected).
    // 「선택을 3번 두면 3회 고른다」는 작성자 가정(C6; 본편 직접 선례 없음) — 검출: TestUnit.Create 되읽기(3 특성 HasFact) + S1, 캐릭터 생성 UI 는 README 수동 완료 조건.
    public static class OppositionSchools
    {
        public sealed class Row
        {
            public readonly string Guid, VanillaGuid, NameKey, Name;
            public readonly SpellSchool School;
            public Row(string guid, SpellSchool school, string vanillaGuid)
            {
                Guid = guid; School = school; VanillaGuid = vanillaGuid;
                NameKey = "School." + school + ".Name";
                Name = "GolemancerOppositionSchool" + school;
            }
        }

        // 변환을 뺀 7학파(설계 §4.3). 본편 위저드 반대 학파 특성은 아이콘·Groups 복사 원본(읽기만).
        public static readonly Row[] Rows =
        {
            new Row(Guids.SchoolAbjuration, SpellSchool.Abjuration, Guids.Game.OppositionSchoolAbjuration),
            new Row(Guids.SchoolConjuration, SpellSchool.Conjuration, Guids.Game.OppositionSchoolConjuration),
            new Row(Guids.SchoolDivination, SpellSchool.Divination, Guids.Game.OppositionSchoolDivination),
            new Row(Guids.SchoolEnchantment, SpellSchool.Enchantment, Guids.Game.OppositionSchoolEnchantment),
            new Row(Guids.SchoolEvocation, SpellSchool.Evocation, Guids.Game.OppositionSchoolEvocation),
            new Row(Guids.SchoolIllusion, SpellSchool.Illusion, Guids.Game.OppositionSchoolIllusion),
            new Row(Guids.SchoolNecromancy, SpellSchool.Necromancy, Guids.Game.OppositionSchoolNecromancy),
        };

        public static BlueprintFeatureSelection Selection;
        public static BlueprintFeature[] Schools;

        public static void Build()
        {
            if (Selection != null) throw new InvalidOperationException("Golemancer: OppositionSchools.Build() called twice");
            if (Rows.Any(r => r.School == SpellSchool.Transmutation)) throw new InvalidOperationException("Golemancer: transmutation must not be an opposition school (design §4.3)");
            var alchemist = Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass);

            Schools = Rows.Select(r => BuildSchool(r, alchemist)).ToArray();

            var vanilla = Get<BlueprintFeatureSelection>(Guids.Game.OppositionSchoolSelection);
            Selection = Create<BlueprintFeatureSelection>("GolemancerOppositionSchoolSelection", Guids.OppositionSchoolSelection, s =>
            {
                // 본편 OppositionSchoolSelection 의 선택 규칙 값 복사(이름·설명·선택지·Ranks 만 우리 것).
                s.Mode = vanilla.Mode;
                s.Group = vanilla.Group;
                s.Group2 = vanilla.Group2;
                s.IgnorePrerequisites = vanilla.IgnorePrerequisites;
                s.Obligatory = vanilla.Obligatory;
                s.ExceptWhiteListed = vanilla.ExceptWhiteListed;
                s.ShowThisSelection = vanilla.ShowThisSelection;
                s.IsClassFeature = vanilla.IsClassFeature;
                s.HideInUI = vanilla.HideInUI;
                s.HideInCharacterSheetAndLevelUp = vanilla.HideInCharacterSheetAndLevelUp;
                s.HideNotAvailibleInUI = vanilla.HideNotAvailibleInUI;
                s.ReapplyOnLevelUp = vanilla.ReapplyOnLevelUp;
                s.Groups = vanilla.Groups.ToArray();
                s.Ranks = GolemRules.OppositionSchoolPicks;
                SetField(s, "m_DisplayName", Strings.L("OppositionSelection.Name"));
                SetField(s, "m_Description", Strings.L("OppositionSelection.Description"));
                SetField(s, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(s, "m_AllFeatures", Rows.Select(r => Ref<BlueprintFeatureReference>(r.Guid)).ToArray());
                SetField(s, "m_Features", new BlueprintFeatureReference[0]);
            });
            var all = Selection.AllFeatures.ToArray();
            if (all.Length != Rows.Length || !all.Select(f => f.AssetGuid.ToString()).SequenceEqual(Rows.Select(r => r.Guid)))
                throw new InvalidOperationException("Golemancer: opposition selection AllFeatures readback = [" + string.Join(", ", all.Select(f => f.name).ToArray()) + "], expected our " + Rows.Length + " schools");
            if (Selection.Ranks != GolemRules.OppositionSchoolPicks || Selection.Mode != vanilla.Mode || Selection.Group != vanilla.Group)
                throw new InvalidOperationException("Golemancer: opposition selection Ranks/Mode/Group readback failed");

            Archetype.AddAt(1, Selection, GolemRules.OppositionSchoolPicks);
        }

        static BlueprintFeature BuildSchool(Row r, BlueprintCharacterClass alchemist)
        {
            var vanilla = Get<BlueprintFeature>(r.VanillaGuid);
            var f = Create<BlueprintFeature>(r.Name, r.Guid, x =>
            {
                SetField(x, "m_DisplayName", Strings.L(r.NameKey));
                SetField(x, "m_Description", Strings.L(ChoiceText.SchoolDescriptionKey(r.School.ToString()))); // 2026-11-17: 학파마다 자기 설명(추출물·폭주 효과) — 한 문구를 돌려쓰지 않는다
                SetField(x, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(x, "m_Icon", vanilla.Icon);
                x.Groups = vanilla.Groups.ToArray();
                x.IsClassFeature = true;
                x.Ranks = 1;
            });
            var comp = new AddOppositionSchool { School = r.School };
            SetField(comp, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.AlchemistClass));
            AddComponents(f, comp);
            // 되읽기: 연금술사 직업으로 그 학파 1개(정찰 §1 — 이 필드로 주문서를 찾는다).
            var back = f.ComponentsArray.OfType<AddOppositionSchool>().ToArray();
            if (back.Length != 1 || back[0].School != r.School || !ReferenceEquals(back[0].CharacterClass, alchemist))
                throw new InvalidOperationException("Golemancer: " + r.Name + " AddOppositionSchool readback failed (expected AlchemistClass / " + r.School + ")");
            if (f.Ranks != 1) throw new InvalidOperationException("Golemancer: " + r.Name + " Ranks readback " + f.Ranks + ", expected 1 (duplicate pick must be rejected by the engine rank check)");
            return f;
        }
    }
}
