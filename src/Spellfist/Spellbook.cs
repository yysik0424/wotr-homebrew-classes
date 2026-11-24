using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spellfist
{
    // 아카니스트 주문서 복제(설계 §4.3, §11-4) + 학파 특성 8개·학파 선택(설계 §4.2, §11-1, §11-5; 정찰 §1, §2).
    public static class Spellbook
    {
        public static BlueprintSpellbook Bp { get; private set; }
        public static BlueprintFeatureSelection SchoolSelection { get; private set; }
        public static BlueprintFeature[] SchoolFeatures { get; private set; } // School 순서

        // School 순서(SchoolLists.ListGuids 와 같은 순서).
        static readonly string[] FeatureGuids =
        {
            Guids.SchoolAbjurationFeature, Guids.SchoolConjurationFeature, Guids.SchoolDivinationFeature, Guids.SchoolEnchantmentFeature,
            Guids.SchoolEvocationFeature, Guids.SchoolIllusionFeature, Guids.SchoolNecromancyFeature, Guids.SchoolTransmutationFeature,
        };

        // 학파 선택은 1레벨(설계 §5 레벨표).
        const int SchoolSelectionLevel = 1;

        // 복제하는 필드 = BlueprintSpellbook 이 선언한 인스턴스 필드 전부(dnlib 2026-11-07, Assembly-CSharp md5 748b3a143b60):
        // Name, IsMythic, m_SpellsPerDay, m_SpellsKnown, m_SpellSlots, m_SpellList, m_MythicSpellList, m_CharacterClass, CastingAttribute, Spontaneous,
        // SpellsPerLevel, AllSpellsKnown, CantripsType, CasterLevelModifier, CanCopyScrolls, IsArcane, IsArcanist, IsIgnoreAddSpellKnownTemporary,
        // HasSpecialSpellList, SpecialSpellListName (+ 기반 형식의 ComponentsArray — 원본은 빈 배열, 정찰 §1-1).
        // 게임 갱신으로 필드가 늘면 Build 가 실패한다(복사 누락이 조용히 기본값으로 남지 않게).
        static readonly string[] ClonedFields =
        {
            "Name", "IsMythic", "m_SpellsPerDay", "m_SpellsKnown", "m_SpellSlots", "m_SpellList", "m_MythicSpellList", "m_CharacterClass",
            "CastingAttribute", "Spontaneous", "SpellsPerLevel", "AllSpellsKnown", "CantripsType", "CasterLevelModifier", "CanCopyScrolls",
            "IsArcane", "IsArcanist", "IsIgnoreAddSpellKnownTemporary", "HasSpecialSpellList", "SpecialSpellListName",
        };

        public static void Build()
        {
            if (Bp != null) throw new InvalidOperationException("Spellfist: Spellbook.Build() called twice");
            if (Archetype.Bp == null || SchoolLists.Empty == null) throw new InvalidOperationException("Spellfist: Spellbook.Build() before Archetype.Build()/SchoolLists.Build()");
            var brawler = Archetype.Brawler();
            BuildSpellbook(brawler);
            BuildSchools(brawler);

            // 레벨표(이 Build 의 몫): 같은 선택을 1레벨에 SchoolPicks 번(정찰 §2).
            for (int k = 0; k < SpellfistRules.SchoolPicks; k++) Archetype.AddAt(SchoolSelectionLevel, SchoolSelection);
            int n = Archetype.CountAt(Archetype.Bp.AddFeatures, SchoolSelectionLevel, SchoolSelection);
            if (n != SpellfistRules.SchoolPicks)
                throw new InvalidOperationException("Spellfist: SchoolSelection count at level 1 != SchoolPicks " + SpellfistRules.SchoolPicks + " (got " + n + ")");
        }

        static void BuildSpellbook(BlueprintCharacterClass brawler)
        {
            var declared = typeof(BlueprintSpellbook).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Select(f => f.Name).ToList();
            if (!new System.Collections.Generic.HashSet<string>(declared).SetEquals(ClonedFields) || declared.Count != ClonedFields.Length)
                throw new InvalidOperationException("Spellfist: BlueprintSpellbook instance fields changed: [" + string.Join(",", declared) + "] vs cloned [" + string.Join(",", ClonedFields) + "]");

            var src = Get<BlueprintSpellbook>(Guids.Game.ArcanistSpellbook);
            // 원본 전제(정찰 §1-1 직독값). 「원본과 같게」 둘 값이 설계가 기대한 값이 아니면 복제가 다른 책이 된다 → 실패.
            if (!src.Spontaneous || !src.IsArcanist || !src.IsArcane || src.CastingAttribute != StatType.Intelligence || src.CantripsType != CantripsType.Cantrips
                || src.AllSpellsKnown || src.IsMythic || src.CasterLevelModifier != 0 || src.HasSpecialSpellList || src.IsIgnoreAddSpellKnownTemporary)
                throw new InvalidOperationException("Spellfist: ArcanistSpellbook flags differ from recon §1-1 (Spontaneous/IsArcanist/IsArcane/Intelligence/Cantrips/...)");
            if (src.ComponentsArray != null && src.ComponentsArray.Length != 0) throw new InvalidOperationException("Spellfist: ArcanistSpellbook has components (recon §1-1 says none) — clone would drop them");
            // 두 표는 원본 참조를 그대로 쓴다(정찰 §0-4: IsArcanist 면 GetSpellSlotsCount 가 m_SpellSlots 를 읽는다 — 비면 NullReference).
            var perDay = Util.GetF(src, "m_SpellsPerDay") as BlueprintReferenceBase;
            var slots = Util.GetF(src, "m_SpellSlots") as BlueprintReferenceBase;
            if (perDay == null || perDay.IsEmpty() || perDay.Guid != BlueprintGuid.Parse(Guids.Game.ArcanistSpellsPerDayTable))
                throw new InvalidOperationException("Spellfist: ArcanistSpellbook m_SpellsPerDay is null/empty or not ArcanistSpellsPerDayTable");
            if (slots == null || slots.IsEmpty() || slots.Guid != BlueprintGuid.Parse(Guids.Game.ArcanistSpellSlots))
                throw new InvalidOperationException("Spellfist: ArcanistSpellbook m_SpellSlots is null/empty or not ArcanistSpellSlots");
            var known = Util.GetF(src, "m_SpellsKnown");
            var mythic = Util.GetF(src, "m_MythicSpellList");

            var name = Strings.L("Spellbook.Name");
            Bp = Create<BlueprintSpellbook>("SpellfistSpellbook", Guids.Spellbook, b =>
            {
                // 원본 그대로
                b.IsMythic = src.IsMythic;
                SetField(b, "m_SpellsPerDay", perDay);
                SetField(b, "m_SpellsKnown", known);
                SetField(b, "m_SpellSlots", slots);
                SetField(b, "m_MythicSpellList", mythic);
                b.CastingAttribute = src.CastingAttribute;
                b.Spontaneous = src.Spontaneous;
                b.AllSpellsKnown = src.AllSpellsKnown;
                b.CantripsType = src.CantripsType;
                b.CasterLevelModifier = src.CasterLevelModifier;
                b.IsArcane = src.IsArcane;
                b.IsArcanist = src.IsArcanist;
                b.IsIgnoreAddSpellKnownTemporary = src.IsIgnoreAddSpellKnownTemporary;
                b.HasSpecialSpellList = src.HasSpecialSpellList;
                b.SpecialSpellListName = src.SpecialSpellListName;
                b.ComponentsArray = new BlueprintComponent[0];
                // 바꾸는 것(설계 §4.3, 정찰 §1-4·§1-5·§3)
                b.Name = name;
                SetField(b, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.BrawlerClass));
                SetField(b, "m_SpellList", Ref<BlueprintSpellListReference>(Guids.EmptySpellList));
                b.SpellsPerLevel = 0;
                b.CanCopyScrolls = false;
            });

            if (!ReferenceEquals(Util.GetF(Bp, "m_SpellsPerDay"), perDay) || Bp.SpellsPerDay == null)
                throw new InvalidOperationException("Spellfist: spellbook m_SpellsPerDay readback is null or not the ArcanistSpellbook reference");
            if (!ReferenceEquals(Util.GetF(Bp, "m_SpellSlots"), slots) || Bp.SpellSlots == null)
                throw new InvalidOperationException("Spellfist: spellbook m_SpellSlots readback is null or not the ArcanistSpellbook reference");
            if (Bp.SpellList != SchoolLists.Empty || Bp.CharacterClass != brawler || !ReferenceEquals(Bp.Name, name))
                throw new InvalidOperationException("Spellfist: spellbook m_SpellList/m_CharacterClass/Name readback failed");
            if (Bp.SpellsPerLevel != 0 || Bp.CanCopyScrolls || !Bp.Spontaneous || !Bp.IsArcanist || !Bp.IsArcane || Bp.CastingAttribute != StatType.Intelligence
                || Bp.CantripsType != CantripsType.Cantrips || Bp.AllSpellsKnown || Bp.IsMythic || !Bp.MemorizeSpells)
                throw new InvalidOperationException("Spellfist: spellbook flag readback failed");
            if (Archetype.Bp.ReplaceSpellbook != Bp) throw new InvalidOperationException("Spellfist: Archetype.ReplaceSpellbook does not resolve to Spellbook.Bp");
        }

        static void BuildSchools(BlueprintCharacterClass brawler)
        {
            if (SchoolSelection != null) throw new InvalidOperationException("Spellfist: school features built twice");
            var schools = (School[])Enum.GetValues(typeof(School));
            if (schools.Length != FeatureGuids.Length) throw new InvalidOperationException("Spellfist: School enum has " + schools.Length + " values, FeatureGuids " + FeatureGuids.Length);
            var feats = new BlueprintFeature[schools.Length];
            for (int i = 0; i < schools.Length; i++)
            {
                var s = schools[i];
                var list = SchoolLists.For(s);
                var f = Create<BlueprintFeature>("SpellfistSchool" + s + "Feature", FeatureGuids[i], x =>
                {
                    Names(x, "School" + s + ".Name", "School" + s + ".Description");
                    x.IsClassFeature = true;
                    x.Ranks = 1;
                });
                // 정찰 §2: LearnList 는 ClassData(brawler) + 아키타입 소속 확인 뒤 DemandSpellbook(brawler) 에 MaxSpellLevel 이하 레벨을 AddKnown.
                var c = new LearnSpellList();
                SetField(c, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.BrawlerClass));
                SetField(c, "m_Archetype", Ref<BlueprintArchetypeReference>(Guids.Archetype));
                SetField(c, "m_SpellList", Ref<BlueprintSpellListReference>(list.AssetGuid.ToString()));
                if (c.CharacterClass != brawler || c.Archetype != Archetype.Bp || c.SpellList != list)
                    throw new InvalidOperationException("Spellfist: " + f.name + " LearnSpellList readback failed");
                AddComponents(f, c);
                if (f.ComponentsArray.Length != 1 || !ReferenceEquals(f.ComponentsArray[0], c) || f.Ranks != 1)
                    throw new InvalidOperationException("Spellfist: " + f.name + " components/Ranks readback failed");
                feats[i] = f;
            }
            SchoolFeatures = feats;

            var sel = Create<BlueprintFeatureSelection>("SpellfistSchoolSelection", Guids.SchoolSelection, x =>
            {
                Names(x, "SchoolSelection.Name", "SchoolSelection.Description");
                x.IsClassFeature = true;
                x.Ranks = 1;
                // 같은 선택을 1레벨에 3번 둔다(정찰 §2) — OnlyNew 는 이미 가진(랭크>0) 학파를 다시 못 고르게 한다(CanSelect IL @0x009C~0x00AA).
                x.Mode = SelectionMode.OnlyNew;
                // 그룹 없음: Group 은 UI 아이콘 묶음(UIUtilityUnit.GetFeatureGroup)과 SelectFeature.CalculatePriority(None → 17 = 일반 특성 순서)에만 쓰인다.
                // IsGroupNone 호출처 0건, CanSelect 는 Group 을 읽지 않는다(dnlib 2026-11-07) → 본편 그룹과 겹치지 않는 None.
                x.Group = FeatureGroup.None;
                x.Group2 = FeatureGroup.None;
                SetField(x, "m_AllFeatures", feats.Select(f => Ref<BlueprintFeatureReference>(f.AssetGuid.ToString())).ToArray());
                SetField(x, "m_Features", new BlueprintFeatureReference[0]);
            });
            var allBack = (BlueprintFeatureReference[])Util.GetF(sel, "m_AllFeatures");
            var featBack = (BlueprintFeatureReference[])Util.GetF(sel, "m_Features");
            if (allBack == null || allBack.Length != feats.Length || !feats.All(f => sel.AllFeatures.Contains(f)) || featBack == null || featBack.Length != 0
                || sel.Mode != SelectionMode.OnlyNew || sel.Group != FeatureGroup.None || sel.Group2 != FeatureGroup.None)
                throw new InvalidOperationException("Spellfist: SchoolSelection readback failed (m_AllFeatures = 8 school features, m_Features empty, Mode OnlyNew, no group)");
            SchoolSelection = sel;
        }

        // 이름·설명(아이콘 없음 — 새 GUID 를 늘리지 않는다). BlueprintUnitFact 의 비공개 필드.
        internal static void Names(BlueprintUnitFact f, string nameKey, string descKey)
        {
            var displayName = Strings.L(nameKey);
            var description = Strings.L(descKey);
            SetField(f, "m_DisplayName", displayName);
            SetField(f, "m_Description", description);
            SetField(f, "m_DescriptionShort", description);
            if (!ReferenceEquals(Util.GetF(f, "m_DisplayName"), displayName) || !ReferenceEquals(Util.GetF(f, "m_Description"), description) || !ReferenceEquals(Util.GetF(f, "m_DescriptionShort"), description))
                throw new InvalidOperationException("Spellfist: " + f.name + " name/description readback failed");
        }
    }
}
