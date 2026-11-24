using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Warlock
{
    // 키트 빌더 공용 도우미(이름·설명·아이콘, 특성 만들기, 순위 설정). 블라스트 메이거스 BlastBlueprints.cs:366-397 와 같은 모양.
    internal static class KitBuild
    {
        internal static Sprite Icon(string abilityGuid)
        {
            var src = Get<BlueprintAbility>(abilityGuid);
            if (src.Icon == null) throw new InvalidOperationException("Warlock: icon source " + src.name + " has no icon");
            return src.Icon;
        }

        internal static void Names(BlueprintUnitFact f, string key, Sprite icon)
        {
            if (icon == null) throw new ArgumentNullException("icon");
            var displayName = Strings.L(key + ".Name");
            var description = Strings.L(key + ".Description");
            SetField(f, "m_DisplayName", displayName);
            SetField(f, "m_Description", description);
            SetField(f, "m_DescriptionShort", description);
            SetField(f, "m_Icon", icon);
            if (!ReferenceEquals(GetF(f, "m_DisplayName"), displayName) || !ReferenceEquals(GetF(f, "m_Description"), description) || !ReferenceEquals(GetF(f, "m_Icon"), icon))
                throw new InvalidOperationException("Warlock: " + f.name + " name/description/icon readback failed");
        }

        // 2026-11-16 결함 수리(능력 툴팁 지속 시간·내성 `<null>`): 툴팁의 지속·내성 칸은 BlueprintAbility.LocalizedDuration·LocalizedSavingThrow 를 읽는다
        // (TooltipTemplateAbility.Prepare @0x0168·@0x0183). 코드로 만든 능력은 두 필드가 null 이라 "<null>" 이 찍힌다(근거 IL = BlueprintUtil 「LocalizedString 필드」 주석).
        // 빌드 되읽기: 지속 칸의 키 == durationKey("" = 빈 키·칸 숨김), 내성 칸 = 빈 키(워락 능력에는 내성 굴림이 없다), 둘 다 null 아님·Shared 없음. 어긋나면 던진다.
        internal static void RequireTooltipFields(BlueprintAbility ab, string durationKey)
        {
            if (ab == null) throw new ArgumentNullException("ab");
            if (durationKey == null) throw new ArgumentNullException("durationKey");
            var d = ab.LocalizedDuration;
            var s = ab.LocalizedSavingThrow;
            if (d == null || s == null || d.Key != durationKey || s.Key != "" || d.Shared != null || s.Shared != null)
                throw new InvalidOperationException("Warlock: " + ab.name + " tooltip string readback failed (LocalizedDuration key '" + (d == null ? "null" : d.Key) + "', expected '" + durationKey
                    + "'; LocalizedSavingThrow key '" + (s == null ? "null" : s.Key) + "', expected ''; both non-null without a shared asset)");
        }

        internal static BlueprintFeature Feature(string name, string guid, string key, Sprite icon)
        {
            var f = Create<BlueprintFeature>(name, guid, x =>
            {
                Names(x, key, icon);
                x.IsClassFeature = true;
                x.Ranks = 1;
                x.ReapplyOnLevelUp = false;
            });
            if (f.Ranks != 1 || !f.IsClassFeature) throw new InvalidOperationException("Warlock: feature " + name + " readback failed (Ranks 1, IsClassFeature)");
            return f;
        }

        // 순위 설정(정찰 §16-T3: 필드 전부 private). 위치 직업 기반이면 m_Class = [WitchClass] 하나(멀티클래스 — 위치 레벨만, 계획 Review Focus 5).
        internal static ContextRankConfig Rank(AbilityRankType type, ContextRankBaseValueType baseType, ContextRankProgression progression, int start, int step, StatType stat, bool witchClass)
        {
            var c = new ContextRankConfig();
            SetField(c, "m_Type", type);
            SetField(c, "m_BaseValueType", baseType);
            SetField(c, "m_Progression", progression);
            SetField(c, "m_StartLevel", start);
            SetField(c, "m_StepLevel", step);
            SetField(c, "m_Stat", stat);
            SetField(c, "m_UseMin", false);
            SetField(c, "m_UseMax", false);
            SetField(c, "m_Class", witchClass ? new[] { Ref<BlueprintCharacterClassReference>(Guids.Game.WitchClass) } : new BlueprintCharacterClassReference[0]);
            SetField(c, "m_AdditionalArchetypes", new BlueprintArchetypeReference[0]);
            var cls = (BlueprintCharacterClassReference[])GetF(c, "m_Class");
            if (c.Type != type || (AbilityRankType)GetF(c, "m_Type") != type || (ContextRankBaseValueType)GetF(c, "m_BaseValueType") != baseType || (ContextRankProgression)GetF(c, "m_Progression") != progression
                || (int)GetF(c, "m_StartLevel") != start || (int)GetF(c, "m_StepLevel") != step || (StatType)GetF(c, "m_Stat") != stat
                || cls.Length != (witchClass ? 1 : 0) || (witchClass && cls[0].Get() != Get<BlueprintCharacterClass>(Guids.Game.WitchClass)))
                throw new InvalidOperationException("Warlock: ContextRankConfig " + type + " readback failed");
            return c;
        }

        internal static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("Warlock: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }

        // 속성 → 엔진 에너지 형식: 값 = 순수 EngineCodes.Energy(시험이 Assembly-CSharp 리터럴과 대조), 되읽기 = 엔진 열거 이름 == 속성 이름(리뷰 T3 I-2 — 자기 자신과만 비교하지 않는다).
        internal static DamageEnergyType Energy(BlastElement e)
        {
            var t = (DamageEnergyType)EngineCodes.Energy(e);
            if (t.ToString() != e.ToString()) throw new InvalidOperationException("Warlock: element " + e + " maps to DamageEnergyType." + t + " (EngineCodes.Energy drift)");
            return t;
        }

        // 순위 설정 = 순수 RankSpec 그대로(리뷰 T3 I-4). 되읽기: 엔진 열거 이름이 명세 기반·진행의 이름과 같다.
        internal static ContextRankConfig RankOf(RankSpec s)
        {
            if (s == null) throw new ArgumentNullException("s");
            var c = Rank((AbilityRankType)s.Type, (ContextRankBaseValueType)s.Base, (ContextRankProgression)s.Progression, s.Start, s.Step, s.StatCharisma ? StatType.Charisma : StatType.Unknown, s.WitchClass);
            var baseType = (ContextRankBaseValueType)GetF(c, "m_BaseValueType");
            var prog = (ContextRankProgression)GetF(c, "m_Progression");
            string wantBase = EngineCodes.RankBaseName(s.Base);
            string wantProg = EngineCodes.ProgressionName(s.Progression);
            if (baseType.ToString() != wantBase || prog.ToString() != wantProg || (baseType == ContextRankBaseValueType.ClassLevel && !s.WitchClass))
                throw new InvalidOperationException("Warlock: rank " + s.Name + " readback " + baseType + "/" + prog + ", expected " + wantBase + "/" + wantProg + (s.WitchClass ? " [WitchClass]" : ""));
            return c;
        }
    }

    // 그룹 특성 6(표식 + 설명문, 컴포넌트 0) + 가호 24(설계 §4.8, 명세 = KitSpec.AllWards). 부여·회수는 WarlockPactComponent(Kit) 만 한다.
    public static class Groups
    {
        static readonly Dictionary<Group, BlueprintFeature> features = new Dictionary<Group, BlueprintFeature>();
        static readonly Dictionary<string, BlueprintFeature> wardsByGuid = new Dictionary<string, BlueprintFeature>();
        public static bool Built { get; private set; }

        public static void Build()
        {
            if (Built || features.Count > 0) throw new InvalidOperationException("Warlock: Groups.Build() called twice");
            foreach (var g in PatronGroups.AllGroups)
            {
                var f = KitBuild.Feature("WarlockGroup" + g + "Feature", KitRule.GroupFeature(g), KitSpec.GroupKey(g), KitBuild.Icon(KitSpec.IconOf(g)));
                if (f.ComponentsArray != null && f.ComponentsArray.Length != 0) throw new InvalidOperationException("Warlock: group feature " + f.name + " must have 0 components");
                features.Add(g, f);
            }
            foreach (var w in KitSpec.AllWards()) wardsByGuid.Add(w.Guid, BuildWard(w));
            if (wardsByGuid.Count != PatronGroups.AllGroups.Length * WarlockRules.Levels.WardLevels.Length) throw new InvalidOperationException("Warlock: built " + wardsByGuid.Count + " wards");
            Built = true;
        }

        static BlueprintFeature BuildWard(WardSpec w)
        {
            var f = KitBuild.Feature("WarlockWard" + w.Group + w.Tier, w.Guid, w.Key, KitBuild.Icon(KitSpec.IconOf(w.Group)));
            switch (w.Kind)
            {
                case WardKind.Resist:
                    {
                        var c = new AddDamageResistanceEnergy { Type = KitBuild.Energy(w.Element), Value = new ContextValue { ValueType = ContextValueType.Simple, Value = w.Value }, UsePool = false, UseValueMultiplier = false, HealOnDamage = false };
                        AddComponents(f, c);
                        if (c.Type != KitBuild.Energy(w.Element) || c.Value.ValueType != ContextValueType.Simple || c.Value.Value != w.Value || c.UsePool) throw new InvalidOperationException("Warlock: ward " + f.name + " resistance readback failed");
                        break;
                    }
                case WardKind.Immune:
                    {
                        var c = new AddEnergyImmunity { Type = KitBuild.Energy(w.Element) };
                        AddComponents(f, c);
                        if (c.Type != KitBuild.Energy(w.Element)) throw new InvalidOperationException("Warlock: ward " + f.name + " immunity readback failed");
                        break;
                    }
                case WardKind.Deflection:
                    AddComponents(f, Deflection(w.Value));
                    break;
                case WardKind.DeflectionAndSr:
                    {
                        // SR = 위치 레벨 + StarSrBase: ContextRankConfig{ClassLevel, [WitchClass], BonusValue, StepLevel = StarSrBase}(ApplyProgression @009D~00A5 = 입력 + StepLevel, 계획 판정 줄).
                        var sr = new AddSpellResistance { AddCR = false, AllSpellResistancePenaltyDoNotUse = false, AddBonusToResistance = false, Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default } };
                        if (w.SrStepLevel != KitSpec.StarSpellResistanceRank.Step) throw new InvalidOperationException("Warlock: ward " + f.name + " SR step " + w.SrStepLevel + " != KitSpec.StarSpellResistanceRank.Step " + KitSpec.StarSpellResistanceRank.Step);
                        var rank = KitBuild.RankOf(KitSpec.StarSpellResistanceRank);
                        AddComponents(f, Deflection(w.Value), sr, rank);
                        if (sr.Value.ValueType != ContextValueType.Rank || sr.Value.ValueRank != AbilityRankType.Default || sr.AddCR || sr.AddBonusToResistance || sr.AllSpellResistancePenaltyDoNotUse)
                            throw new InvalidOperationException("Warlock: ward " + f.name + " spell resistance readback failed");
                        break;
                    }
                default: throw new ArgumentOutOfRangeException("w", w.Kind, "Warlock: unknown ward kind");
            }
            // 「설계와 달리 정한 것」 ④: 별 4단만 레벨업마다 재적용(AddSpellResistance 가 새 정수로 다시 등록 — UnitProgressionData.ReapplyFeaturesOnLevelUp @002E~0036).
            f.ReapplyOnLevelUp = w.ReapplyOnLevelUp;
            if (f.ReapplyOnLevelUp != w.ReapplyOnLevelUp) throw new InvalidOperationException("Warlock: ward " + f.name + " ReapplyOnLevelUp readback failed");
            return f;
        }

        static AddStatBonus Deflection(int ac)
        {
            var c = new AddStatBonus { Stat = StatType.AC, Descriptor = ModifierDescriptor.Deflection, Value = ac, ScaleByBasicAttackBonus = false };
            if (c.Stat != StatType.AC || c.Descriptor != ModifierDescriptor.Deflection || c.Value != ac) throw new InvalidOperationException("Warlock: deflection AddStatBonus readback failed");
            return c;
        }

        public static BlueprintFeature Feature(Group g)
        {
            BlueprintFeature f;
            if (!Built || !features.TryGetValue(g, out f)) throw new InvalidOperationException("Warlock: Groups.Feature(" + g + ") before Groups.Build()");
            return f;
        }




    }
}
