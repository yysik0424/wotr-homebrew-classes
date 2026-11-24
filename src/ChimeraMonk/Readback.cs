using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    // 인게임 S1 built-matches-plans 가 같은 함수를 다시 부른다(C4 — 대조식 한 곳). 반환 = 어긋난 칸마다 「키: 기대 / 실제」.
    public static class Readback
    {
        static string Pairs(IEnumerable<LevelEntry> es)
        {
            return string.Join(",", es.SelectMany(e => e.Features.Select(f => e.Level + ":" + (f == null ? "null" : f.AssetGuid.ToString()))).OrderBy(x => x, StringComparer.Ordinal));
        }

        public static List<string> LevelDiff()
        {
            var d = new List<string>();
            var bp = Archetype.Bp;
            if (bp == null) { d.Add("archetype: not built"); return d; }
            string wantRem = string.Join(",", LevelPlan.Removals.Select(r => r.Level + ":" + r.Guid).OrderBy(x => x, StringComparer.Ordinal));
            string gotRem = Pairs(bp.RemoveFeatures);
            if (wantRem != gotRem) d.Add("RemoveFeatures: want " + wantRem + " / got " + gotRem);
            var missingKeys = LevelPlan.Additions.Select(a => a.Key).Distinct().Where(k => !Archetype.Planned.ContainsKey(k)).ToList();
            if (missingKeys.Count > 0) d.Add("AddFeatures: plan keys never attached: " + string.Join(",", missingKeys));
            else
            {
                string wantAdd = string.Join(",", LevelPlan.Additions.Select(a => a.Level + ":" + Archetype.Planned[a.Key]).OrderBy(x => x, StringComparer.Ordinal));
                string gotAdd = Pairs(bp.AddFeatures);
                if (wantAdd != gotAdd) d.Add("AddFeatures: want " + wantAdd + " / got " + gotAdd);
            }
            if (Mutations.Selection != null)
            {
                int slots = bp.AddFeatures.Sum(e => e.Features.Count(f => f != null && f.AssetGuid == Mutations.Selection.AssetGuid));
                if (slots != ChimeraRules.Dials.MutationPicks) d.Add("mutation slots: want " + ChimeraRules.Dials.MutationPicks + " / got " + slots);
            }
            return d;
        }

        static List<string> Refs(BlueprintFeatureSelection s) { return ((BlueprintFeatureReference[])Archetype.GetF(s, "m_AllFeatures")).Select(r => r.Guid.ToString()).ToList(); }

        static List<string> NoFeatures(BlueprintScriptableObject bp)
        {
            return bp.ComponentsArray.OfType<PrerequisiteNoFeature>().Select(p => ((BlueprintReferenceBase)Archetype.GetF(p, "m_Feature")).Guid.ToString()).OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        public static List<string> MutationsDiff()
        {
            var d = new List<string>();
            var sel = Mutations.Selection;
            if (sel == null) { d.Add("mutation selection: not built"); return d; }
            var rows = MutationsPlan.Rows();
            var got = Refs(sel);
            if (!got.SequenceEqual(rows.Select(r => r.Guid))) d.Add("selection options: want " + string.Join(",", rows.Select(r => r.Guid)) + " / got " + string.Join(",", got));
            if (sel.AllFeatures.Count() != ChimeraRules.Dials.MutationOptions) d.Add("selection AllFeatures resolves " + sel.AllFeatures.Count() + ", want " + ChimeraRules.Dials.MutationOptions);
            foreach (var r in rows)
            {
                BlueprintFeature f;
                try { f = Mutations.Get(r.Id); } catch (KeyNotFoundException) { d.Add(r.Id + ": not built"); continue; }
                if (f.AssetGuid.ToString() != r.Guid) d.Add(r.Id + ": guid " + f.AssetGuid);
                if (f.Ranks != r.Ranks) d.Add(r.Id + ": Ranks " + f.Ranks);
                // 선행 조건 3종(최소 몽크 레벨 · 요구 특성 · 금지 특성)은 전부 선행 조건 표 MutationsPlan.PrereqOf 와 대조한다(변이·중첩 선택·선택지 모두 같은 함수).
                d.AddRange(PrereqDiff(f, r.Id, MutationsPlan.PrereqOf(r.Guid)));
                if (!r.IsNestedSelection) continue;
                var s = f as BlueprintFeatureSelection;
                var want = r.Nested.Select(o => o.Guid).ToList();
                if (s == null) { d.Add(r.Id + ": not a selection"); continue; }
                if (!Refs(s).SequenceEqual(want)) d.Add(r.Id + ": nested options " + string.Join(",", Refs(s)));
                foreach (var o in r.Nested)
                {
                    var of = Mutations.Option(r.Id, o.Key);
                    if (of.AssetGuid.ToString() != o.Guid) { d.Add(r.Id + "/" + o.Key + ": option guid " + of.AssetGuid); continue; }
                    d.AddRange(PrereqDiff(of, r.Id + "/" + o.Key, MutationsPlan.PrereqOf(o.Guid)));
                }
            }
            return d;
        }

        // 폐지된 변이(소유자 결정 2026-11-17: 추가 꼬리) == 「같은 GUID · 숨김 · 부품 0 · 어느 선택에도 없음」, 폐지 무기 == 「복제만 있고 우리 블루프린트 어디에도 사지로 붙지 않음」.
        //  Mutations.Build 끝(특성을 만든 직후)·NaturalWeapons.Build 끝(사지를 붙인 직후)·인게임 S1 built-matches-plans(All — 모든 빌더가 끝난 뒤)가 부른다.
        //  아직 안 만든 무기(Mutations.Build 시점)는 대조하지 않는다: 그때는 RetiredWeapons 가 비어 있고, 무기 쪽은 NaturalWeapons.Build 가 끝에서 다시 부른다(WeaponsDiff 가 개수를 센다).
        public static List<string> RetiredDiff()
        {
            var d = new List<string>();
            var sel = Mutations.Selection;
            if (sel == null) { d.Add("retired: mutation selection not built"); return d; }
            var offered = new HashSet<string>(Refs(sel));
            foreach (var r in MutationsPlan.Rows().Where(x => x.IsNestedSelection)) foreach (var g in Refs((BlueprintFeatureSelection)Mutations.Get(r.Id))) offered.Add(g);
            foreach (var r in MutationsPlan.RetiredRows())
            {
                BlueprintFeature f;
                try { f = Mutations.Retired(r.Id); } catch (KeyNotFoundException) { d.Add("retired " + r.Id + ": not built"); continue; }
                if (f.AssetGuid.ToString() != r.Guid) d.Add("retired " + r.Id + ": guid " + f.AssetGuid + ", want " + r.Guid);
                if (!ReferenceEquals(ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(r.Guid)), f)) d.Add("retired " + r.Id + ": " + r.Guid + " does not resolve to the retired feature");
                if (f is BlueprintFeatureSelection) d.Add("retired " + r.Id + ": is a selection");
                if (!f.HideInUI || !f.HideInCharacterSheetAndLevelUp) d.Add("retired " + r.Id + ": not hidden (HideInUI " + f.HideInUI + ", HideInCharacterSheetAndLevelUp " + f.HideInCharacterSheetAndLevelUp + ")");
                if (f.ComponentsArray.Length != 0) d.Add("retired " + r.Id + ": " + f.ComponentsArray.Length + " component(s), must have none (" + string.Join(",", f.ComponentsArray.Select(c => c.GetType().Name)) + ")");
                if (offered.Contains(r.Guid)) d.Add("retired " + r.Id + ": still offered by a selection");
            }
            // 폐지 무기를 사지로 붙이는 우리 블루프린트가 없어야 한다(변이·선택지·몸 특성·버프 전부 — BlueprintUtil.OwnBlueprints).
            var retiredWeapons = new HashSet<string>(NaturalWeaponsPlan.RetiredRows().Select(r => r.Guid));
            foreach (var bp in WotrClassMod.Common.BlueprintUtil.OwnBlueprints.OfType<BlueprintScriptableObject>())
                foreach (var limb in bp.ComponentsArray.OfType<AddAdditionalLimb>())
                {
                    string g = ((BlueprintReferenceBase)Archetype.GetF(limb, "m_Weapon")).Guid.ToString();
                    if (retiredWeapons.Contains(g)) d.Add("retired weapon " + g + ": attached as an additional limb by " + bp.name);
                }
            return d;
        }

        // 블루프린트 하나의 선행 조건 컴포넌트 == 선행 조건 표의 그 행(칸 3개 전부). 어긋난 칸마다 한 줄.
        static List<string> PrereqDiff(BlueprintFeature bp, string label, PrereqRow p)
        {
            var d = new List<string>();
            var cl = bp.ComponentsArray.OfType<PrerequisiteClassLevel>().ToList();
            if (p.MinMonkLevel > 0 ? (cl.Count != 1 || cl[0].Level != p.MinMonkLevel || cl[0].CharacterClass == null || cl[0].CharacterClass.AssetGuid.ToString() != Guids.Game.MonkClass || cl[0].Not) : cl.Count != 0)
                d.Add(label + ": class-level prerequisite " + string.Join(",", cl.Select(c => c.Level)) + " (want monk " + p.MinMonkLevel + ")");
            var pf = bp.ComponentsArray.OfType<PrerequisiteFeature>().Select(x => ((BlueprintReferenceBase)Archetype.GetF(x, "m_Feature")).Guid.ToString()).ToList();
            if (!pf.SequenceEqual(p.RequiresGuid == null ? new string[0] : new[] { p.RequiresGuid })) d.Add(label + ": feature prerequisite " + string.Join(",", pf) + " (want " + (p.RequiresGuid ?? "none") + ")");
            var forbid = p.Forbid.OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (!NoFeatures(bp).SequenceEqual(forbid)) d.Add(label + ": NoFeature " + NoFeatures(bp).Count + " (want " + forbid.Count + ": " + string.Join(",", forbid) + ")");
            int other = bp.ComponentsArray.OfType<Prerequisite>().Count() - cl.Count - pf.Count - NoFeatures(bp).Count;
            if (other != 0) d.Add(label + ": " + other + " prerequisite component(s) of a kind the plan does not have");
            return d;
        }

        static string CrcKey(ContextRankConfig c)
        {
            var cls = ((BlueprintCharacterClassReference[])Archetype.GetF(c, "m_Class")).Select(r => r.Guid.ToString());
            var items = (Array)Archetype.GetF(c, "m_CustomProgression");
            var table = items == null ? new int[0] : items.Cast<object>().Select(it => (int)Archetype.GetF(it, "ProgressionValue")).ToArray();
            return Archetype.GetF(c, "m_Type") + "/" + Archetype.GetF(c, "m_BaseValueType") + "/" + string.Join("+", cls) + "/" + Archetype.GetF(c, "m_Progression") + "/" + string.Join(",", table.Select(x => x.ToString()));
        }

        // T4: 변이약 버프 3 == MutagenPlan.Buff 행, 능력 3 의 제한·행동 종류, 자원 배선 == ResourcePlan.
        public static List<string> MutagenDiff()
        {
            var d = new List<string>();
            if (Mutagen.Resource == null || Mutagen.Feature == null) { d.Add("mutagen: not built"); return d; }
            var amount = Archetype.GetF(Mutagen.Resource, "m_MaxAmount");
            if ((int)Archetype.GetF(amount, "BaseValue") != ChimeraRules.Dials.MutagenDailyBase || !(bool)Archetype.GetF(amount, "IncreasedByStat") || (StatType)Archetype.GetF(amount, "ResourceBonusStat") != StatType.Wisdom
                || (int)Archetype.GetF(Mutagen.Resource, "m_Min") != ChimeraRules.Dials.MutagenDailyMin || (bool)Archetype.GetF(Mutagen.Resource, "m_UseMax"))
                d.Add("mutagen resource: want base " + ChimeraRules.Dials.MutagenDailyBase + " + Wisdom, min " + ChimeraRules.Dials.MutagenDailyMin);
            var monk = Guids.Game.MonkClass;
            foreach (var stat in MutagenPlan.Stats)
            {
                var spec = MutagenPlan.Buff(stat);
                var b = Mutagen.Buffs.FirstOrDefault(x => x.AssetGuid.ToString() == spec.BuffGuid);
                if (b == null) { d.Add("mutagen buff " + stat + ": missing"); continue; }
                var bon = b.ComponentsArray.OfType<AddContextStatBonus>().Select(c => c.Descriptor + "/" + c.Stat + "/" + c.Value.ValueRank).ToList();
                var wantBon = spec.Bonuses.Select(r => r.Descriptor + "/" + r.Stat + "/" + r.RankType).ToList();
                if (!bon.SequenceEqual(wantBon)) d.Add("mutagen buff " + stat + ": bonuses " + string.Join(",", bon) + " want " + string.Join(",", wantBon));
                var crc = b.ComponentsArray.OfType<ContextRankConfig>().Select(CrcKey).ToList();
                var wantCrc = spec.Ranks.Select(r => r.RankType + "/ClassLevel/" + monk + "/Custom/" + string.Join(",", r.Table.Select(x => x.ToString()))).ToList();
                if (!crc.SequenceEqual(wantCrc)) d.Add("mutagen buff " + stat + ": rank configs " + string.Join(" ; ", crc));
                if (b.ComponentsArray.Length != spec.Bonuses.Count + spec.Ranks.Count) d.Add("mutagen buff " + stat + ": " + b.ComponentsArray.Length + " components (mental penalty or extra component?)");
            }
            if (Mutagen.Abilities.Count != MutagenPlan.Stats.Length) d.Add("mutagen abilities: " + Mutagen.Abilities.Count);
            foreach (var a in Mutagen.Abilities)
            {
                if (a.ActionType != UnitCommand.CommandType.Standard) d.Add(a.name + ": action " + a.ActionType);
                if (a.Type != AbilityType.Supernatural) d.Add(a.name + ": type " + a.Type);
                if (a.ComponentsArray.OfType<AbilityCasterNotPolymorphed>().Count() != 1) d.Add(a.name + ": AbilityCasterNotPolymorphed count");
                var dur = a.ComponentsArray.OfType<ContextRankConfig>().Select(CrcKey).ToList();
                if (dur.Count != 1 || !dur[0].StartsWith("Default/ClassLevel/" + monk + "/AsIs/")) d.Add(a.name + ": duration rank " + string.Join(";", dur));
            }
            d.AddRange(ResourceDiff(r => r.Resource == Guids.MutagenResource));
            d.AddRange(MutagenFx.Diff(Mutagen.Abilities)); // FX(2026-11-15 승인) — FxTable 원장과 대조
            return d;
        }

        // 자원 배선(I01): ResourcePlan 행마다 능력의 AbilityResourceLogic(요구 자원, 소비 플래그, 양) + 부여 특성의 AddAbilityResources{그 자원, RestoreAmount}.
        public static List<string> ResourceDiff(Func<ResourceRow, bool> which)
        {
            var d = new List<string>();
            d.AddRange(ResourcePlan.Validate(ResourcePlan.Rows).Select(e => "resource plan: " + e));
            foreach (var r in ResourcePlan.Rows.Where(which))
            {
                var ab = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(r.Ability)) as BlueprintAbility;
                if (ab == null) { d.Add("resource " + r.Ability + ": ability not built"); continue; }
                var logic = ab.ComponentsArray.OfType<AbilityResourceLogic>().ToList();
                if (logic.Count != 1 || logic[0].RequiredResource == null || logic[0].RequiredResource.AssetGuid.ToString() != r.Resource || logic[0].IsSpendResource != r.IsSpend || logic[0].Amount != r.SpendAmount)
                    d.Add("resource " + ab.name + ": logic (resource, spend, amount) mismatch");
                var grant = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(r.GrantFeature)) as BlueprintScriptableObject;
                if (grant == null || !grant.ComponentsArray.OfType<AddAbilityResources>().Any(x => x.Resource != null && x.Resource.AssetGuid.ToString() == r.Resource && x.RestoreAmount))
                    d.Add("resource " + ab.name + ": granting feature " + r.GrantFeature + " has no AddAbilityResources{" + r.Resource + ", RestoreAmount}");
            }
            return d;
        }

        // T5: 무기 복제(행마다) == NaturalWeaponsPlan(우리 GUID·원본 형식), 사지 변이마다 AddAdditionalLimb{그 무기} 정확히 1, 몸 특성에 ChimeraLimbComponent 1.
        public static List<string> WeaponsDiff()
        {
            var d = new List<string>();
            var rows = NaturalWeaponsPlan.Rows();
            if (NaturalWeapons.Weapons.Count != rows.Count) d.Add("weapons: " + NaturalWeapons.Weapons.Count + " built, plan " + rows.Count);
            foreach (var r in rows)
            {
                Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon w;
                if (!NaturalWeapons.Weapons.TryGetValue(r.Key, out w)) { d.Add("weapon " + r.Key + ": not built"); continue; }
                if (w.AssetGuid.ToString() != r.Guid) d.Add("weapon " + r.Key + ": guid " + w.AssetGuid);
                if (w.Type == null || w.Type.AssetGuid.ToString() != r.TypeGuid || !w.IsNatural) d.Add("weapon " + r.Key + ": type/natural mismatch");
                var src = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(r.SourceGuid)) as Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon;
                if (src == null || src.AssetGuid.ToString() == w.AssetGuid.ToString()) d.Add("weapon " + r.Key + ": source " + r.SourceGuid + " missing");
                var limbs = Mutations.Get(r.Mutation).ComponentsArray.OfType<AddAdditionalLimb>().ToList();
                if (limbs.Count != 1 || limbs[0].Weapon == null || limbs[0].Weapon.AssetGuid.ToString() != r.Guid) d.Add("mutation " + r.Mutation + ": AddAdditionalLimb " + string.Join(",", limbs.Select(l => l.Weapon == null ? "null" : l.Weapon.AssetGuid.ToString())));
            }
            var others = MutationsPlan.Rows().Where(m => !rows.Any(r => r.Mutation == m.Id)).Select(m => m.Id).Where(id => Mutations.Get(id).ComponentsArray.OfType<AddAdditionalLimb>().Any()).ToList();
            if (others.Count > 0) d.Add("non-limb mutations with AddAdditionalLimb: " + string.Join(",", others));
            // 폐지 무기(2026-11-17): 복제는 같은 GUID·원본 형식으로 있어야 한다(세이브가 참조할 수 있다). 붙지 않았는지는 RetiredDiff 가 본다.
            var retired = NaturalWeaponsPlan.RetiredRows();
            if (NaturalWeapons.RetiredWeapons.Count != retired.Count) d.Add("retired weapons: " + NaturalWeapons.RetiredWeapons.Count + " built, plan " + retired.Count);
            foreach (var r in retired)
            {
                Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon w;
                if (!NaturalWeapons.RetiredWeapons.TryGetValue(r.Key, out w)) { d.Add("retired weapon " + r.Key + ": not built"); continue; }
                if (w.AssetGuid.ToString() != r.Guid) d.Add("retired weapon " + r.Key + ": guid " + w.AssetGuid);
                if (w.Type == null || w.Type.AssetGuid.ToString() != r.TypeGuid || !w.IsNatural) d.Add("retired weapon " + r.Key + ": type/natural mismatch");
                if (!ReferenceEquals(ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(r.Guid)), w)) d.Add("retired weapon " + r.Key + ": " + r.Guid + " does not resolve to the clone");
            }
            if (NaturalWeapons.BodyFeature == null || NaturalWeapons.BodyFeature.ComponentsArray.OfType<ChimeraLimbComponent>().Count() != 1) d.Add("body feature: ChimeraLimbComponent count != 1");
            return d;
        }

        const string VisualKeyPrefix = "Visual:";

        // 2026-11-16: 변이 외형 == VisualPlan. 변이마다(중첩 선택의 옵션 포함) 외형 컴포넌트 다중집합이 계획 행과 같아야 한다 — 행 없는 변이(날개 등)는 0개. (폐지된 추가 꼬리는 변이 행이 아니다 — 부품 0 은 RetiredDiff 가 본다.)
        public static List<string> VisualsDiff()
        {
            var d = new List<string>();
            if (!Visuals.Built) { d.Add("visuals: not built"); return d; }
            var rows = VisualPlan.Rows();
            var mutations = MutationsPlan.Rows();
            foreach (var r in rows) if (!mutations.Any(m => m.Id == r.Mutation)) d.Add("visual row " + r.Mutation + ": no such mutation");
            Func<BlueprintScriptableObject, string> got = bp => Bag(bp.ComponentsArray.Select(Key).Where(k => k != null && k.StartsWith(VisualKeyPrefix, StringComparison.Ordinal)));
            foreach (var m in mutations)
            {
                string want = Bag(rows.Where(r => r.Mutation == m.Id).Select(r => VisualKeyPrefix + (r.Kind == VisualKind.SingleAsset ? "Asset:" + r.AssetId : "Bundle:" + r.BundleGuid)));
                string have = got(Mutations.Get(m.Id));
                if (want != have) d.Add("visual " + m.Id + ": want [" + want + "] got [" + have + "]");
                if (!m.IsNestedSelection) continue;
                foreach (var o in m.Nested)
                {
                    string opt = got(Mutations.Option(m.Id, o.Key));
                    if (opt.Length != 0) d.Add("visual " + m.Id + "/" + o.Key + ": unexpected [" + opt + "]");
                }
            }
            return d;
        }

        // T6: 몸 변이 8 == BodyPlan(상시 컴포넌트·강화 버프 컴포넌트·BuffExtraEffects 배선). 컴포넌트 → 문자열 키로 바꿔 다중집합 대조.
        static string Key(BlueprintComponent c)
        {
            var sb = c as AddStatBonus; if (sb != null) return "Stat:" + sb.Descriptor + "/" + sb.Stat + "/" + sb.Value;
            var cs = c as AddContextStatBonus; if (cs != null) return "Ranked:" + cs.Descriptor + "/" + cs.Stat;
            var crc = c as ContextRankConfig; if (crc != null) return "Table:" + CrcKey(crc).Split(new[] { "/" }, StringSplitOptions.None).Last();
            var cmd = c as Kingmaker.Designers.Mechanics.Facts.CMDBonusAgainstManeuvers; if (cmd != null) return "Cmd:" + cmd.Descriptor + "/" + string.Join(",", cmd.Maneuvers.Select(m => m.ToString())) + "/" + cmd.Value.Value;
            var mf = c as AddMechanicsFeature; if (mf != null) return "Mech:" + Archetype.GetF(mf, "m_Feature");
            var af = c as AddFacts; if (af != null) return "Facts:" + string.Join("+", ((BlueprintUnitFactReference[])Archetype.GetF(af, "m_Facts")).Select(r => r.Guid.ToString()));
            var bs = c as Kingmaker.Designers.Mechanics.Facts.Blindsense; if (bs != null) return "Blind:" + (int)bs.Range.Value + "/" + bs.Blindsight;
            var bdi = c as BuffDescriptorImmunity; if (bdi != null) return "BuffImm:" + (Kingmaker.Blueprints.Classes.Spells.SpellDescriptor)bdi.Descriptor;
            var sid = c as SpellImmunityToSpellDescriptor; if (sid != null) return "SpellDescImm:" + (Kingmaker.Blueprints.Classes.Spells.SpellDescriptor)sid.Descriptor;
            var aci = c as AddConditionImmunity; if (aci != null) return "Cond:" + aci.Condition;
            var aei = c as AreaEffectImmunity; if (aei != null) return "AreaImm:" + Archetype.GetF(aei, "m_CasterType") + "/" + Archetype.GetF(aei, "m_SpecificAreaEffects") + "/" + string.Join("+", ((List<BlueprintAbilityAreaEffectReference>)Archetype.GetF(aei, "m_AreaEffects")).Select(r => r.Guid.ToString()));
            var fh = c as Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing; if (fh != null) return "FastHeal:" + fh.Heal;
            // 2026-11-16 외형 컴포넌트: 대조에 포함한다(계획에 없는 변이에 붙으면 BodyDiff·VisualsDiff 가 어긋남으로 센다). 키 접두 = VisualKeyPrefix.
            //  우리 컴포넌트 = 모드에 따라 Asset:<에셋> / Bundle:<묶음>. 본편 AddEquipmentEntity 는 계획에 없는 종류(VanillaAsset) — 어디에 붙어 있든 어긋남으로 센다(꼬리도 자체 컴포넌트를 쓴다).
            var ee = c as AddEquipmentEntity; if (ee != null) return VisualKeyPrefix + "VanillaAsset:" + (ee.EquipmentEntity == null ? "null" : ee.EquipmentEntity.AssetId);
            var bv = c as ChimeraBundleVisualComponent; if (bv != null) return VisualKeyPrefix + (bv.Kind == VisualKind.SingleAsset ? "Asset:" + bv.AssetId : "Bundle:" + bv.BundleGuid);
            return null;
        }

        static IEnumerable<string> Expect(EffectRow e, bool boost)
        {
            switch (e.Kind)
            {
                case EffectKind.StatBonus: yield return "Stat:" + e.Descriptor + "/" + e.Stat + "/" + e.Value; break;
                case EffectKind.RankedStatBonus:
                    {
                        var desc = Fab.EnumOf<ModifierDescriptor>(e.Descriptor);
                        var table = !boost ? e.ReplaceTable : BoostPlan.For(ModifierDescriptorHelper.IsStackable(desc)) == BoostShape.Replace ? e.ReplaceTable : e.AddTable;
                        yield return "Ranked:" + e.Descriptor + "/" + e.Stat;
                        yield return "Table:" + string.Join(",", table.Select(x => x.ToString()));
                    }
                    break;
                case EffectKind.CmdManeuvers: yield return "Cmd:" + e.Descriptor + "/" + e.Stat + "/" + e.Value; break;
                case EffectKind.Mechanics: foreach (var n in e.Refs) yield return "Mech:" + n; break;
                case EffectKind.Facts: yield return "Facts:" + string.Join("+", e.Refs); break;
                case EffectKind.Blindsense: yield return "Blind:" + e.Value + "/False"; break;
                case EffectKind.DescriptorImmunity: foreach (var n in e.Refs) { yield return "BuffImm:" + n; yield return "SpellDescImm:" + n; } break;
                case EffectKind.ConditionImmunity: foreach (var n in e.Refs) yield return "Cond:" + n; break;
                case EffectKind.AreaImmunity: yield return "AreaImm:Any/True/" + string.Join("+", e.Refs); break;
                case EffectKind.FastHealing: yield return "FastHeal:" + e.Value; break;
                case EffectKind.BreathSurge: break; // 브레스 분출 자원 쪽(T7)이 대조
            }
        }

        static string Bag(IEnumerable<string> xs) { return string.Join(" | ", xs.Where(x => x != null).OrderBy(x => x, StringComparer.Ordinal)); }

        public static List<string> BodyDiff()
        {
            var d = new List<string>();
            if (!BodyMutations.Built) { d.Add("body mutations: not built"); return d; }
            foreach (var row in BodyPlan.Rows())
            {
                var f = Mutations.Get(row.Id);
                string want = Bag(row.Base.SelectMany(e => Expect(e, false))), got = Bag(f.ComponentsArray.Select(Key));
                if (want != got) d.Add(row.Id + " base: want [" + want + "] got [" + got + "]");
                var extra = f.ComponentsArray.OfType<BuffExtraEffects>().ToList();
                if (row.BoostBuffGuid == null) { if (extra.Count != 0) d.Add(row.Id + ": unexpected BuffExtraEffects"); continue; }
                BlueprintBuff b;
                if (!BodyMutations.BoostBuffs.TryGetValue(row.Id, out b) || b.AssetGuid.ToString() != row.BoostBuffGuid) { d.Add(row.Id + ": boost buff missing"); continue; }
                string wantB = Bag(row.Boost.SelectMany(e => Expect(e, true))), gotB = Bag(b.ComponentsArray.Select(Key));
                if (wantB != gotB) d.Add(row.Id + " boost: want [" + wantB + "] got [" + gotB + "]");
                if (extra.Count != 1) { d.Add(row.Id + ": BuffExtraEffects count " + extra.Count); continue; }
                var checkedList = ((BlueprintBuffReference[])Archetype.GetF(extra[0], "m_CheckedBuffList")).Select(r => r.Guid.ToString()).OrderBy(x => x).ToList();
                var mut = Mutagen.Buffs.Select(x => x.AssetGuid.ToString()).OrderBy(x => x).ToList();
                var eb = (BlueprintReferenceBase)Archetype.GetF(extra[0], "m_ExtraEffectBuff");
                if (!checkedList.SequenceEqual(mut) || eb == null || eb.Guid.ToString() != row.BoostBuffGuid) d.Add(row.Id + ": BuffExtraEffects wiring (checked = mutagen buffs, extra = boost)");
            }
            return d;
        }

        // T7: 브레스 — 자원 2(기본 3·분출 1) · 원소마다 능력 2(원뿔 30 · 원소 피해 d6 × 순위 · Div2 · 몽크 지혜 DC · 기술자 BreathWeapon · 분출만 변이약 조건) · 폐 분출 +1.
        public static List<string> BreathDiff()
        {
            var d = new List<string>();
            if (Breath.Resource == null || Breath.SurgeResource == null) { d.Add("breath: not built"); return d; }
            Func<BlueprintAbilityResource, int> baseOf = r => (int)Archetype.GetF(Archetype.GetF(r, "m_MaxAmount"), "BaseValue");
            if (baseOf(Breath.Resource) != ChimeraRules.Dials.BreathDailyBase || baseOf(Breath.SurgeResource) != ChimeraRules.Dials.BreathSurgeBase) d.Add("breath resources: base " + baseOf(Breath.Resource) + " / surge " + baseOf(Breath.SurgeResource));
            var monk = Guids.Game.MonkClass;
            var mutagen = Mutagen.Buffs.Select(b => b.AssetGuid.ToString()).OrderBy(x => x).ToList();
            foreach (var e in MutationsPlan.BreathElements)
            {
                foreach (var surge in new[] { false, true })
                {
                    BlueprintAbility a;
                    if (!(surge ? Breath.SurgeAbilities : Breath.Abilities).TryGetValue(e.Key, out a)) { d.Add("breath " + e.Key + (surge ? " surge" : "") + ": not built"); continue; }
                    string tag = a.name + ": ";
                    var proj = a.ComponentsArray.OfType<AbilityDeliverProjectile>().ToList();
                    if (proj.Count != 1 || proj[0].Type != AbilityProjectileType.Cone || (int)((Kingmaker.Utility.Feet)Archetype.GetF(proj[0], "m_Length")).Value != ChimeraRules.Dials.BreathConeFeet) d.Add(tag + "not a " + ChimeraRules.Dials.BreathConeFeet + " ft cone");
                    var run = a.ComponentsArray.OfType<AbilityEffectRunAction>().ToList();
                    var dmg = run.Count == 1 ? run[0].Actions.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>().ToList() : new List<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>();
                    if (run.Count != 1 || run[0].SavingThrowType != SavingThrowType.Reflex || dmg.Count != 1 || dmg[0].DamageType.Energy.ToString() != e.Key || !dmg[0].HalfIfSaved
                        || dmg[0].Value.DiceType.ToString() != "D" + ChimeraRules.Dials.BreathDie.Sides || dmg[0].Value.DiceCountValue.ValueType != Kingmaker.UnitLogic.Mechanics.ContextValueType.Rank)
                        d.Add(tag + "run action is not Reflex-half " + e.Key + " d" + ChimeraRules.Dials.BreathDie.Sides + " x rank");
                    var crc = a.ComponentsArray.OfType<ContextRankConfig>().Select(CrcKey).ToList();
                    if (crc.Count != 1 || !crc[0].StartsWith("Default/ClassLevel/" + monk + "/Div2/")) d.Add(tag + "dice rank " + string.Join(";", crc));
                    var dc = a.ComponentsArray.OfType<ContextCalculateAbilityParamsBasedOnClass>().ToList();
                    if (dc.Count != 1 || dc[0].StatType != StatType.Wisdom || ((BlueprintReferenceBase)Archetype.GetF(dc[0], "m_CharacterClass")).Guid.ToString() != monk) d.Add(tag + "DC is not monk/Wisdom");
                    if (a.ComponentsArray.Any(c => c.GetType().Name == "ReplaceAbilitiesStat")) d.Add(tag + "ReplaceAbilitiesStat present");
                    var has = a.ComponentsArray.OfType<AbilityCasterHasFacts>().ToList();
                    if (surge ? (has.Count != 1 || has[0].NeedsAll || !((BlueprintUnitFactReference[])Archetype.GetF(has[0], "m_Facts")).Select(r => r.Guid.ToString()).OrderBy(x => x).SequenceEqual(mutagen)) : has.Count != 0)
                        d.Add(tag + (surge ? "surge is not gated on the mutagen buffs" : "base breath must not need the mutagen"));
                }
                var f = Mutations.Option("Breath", e.Key);
                var facts = f.ComponentsArray.OfType<AddFacts>().SelectMany(x => (BlueprintUnitFactReference[])Archetype.GetF(x, "m_Facts")).Select(r => r.Guid.ToString()).ToList();
                if (!facts.SequenceEqual(new[] { Breath.Abilities[e.Key].AssetGuid.ToString(), Breath.SurgeAbilities[e.Key].AssetGuid.ToString() })) d.Add("breath element " + e.Key + ": AddFacts " + string.Join(",", facts));
            }
            var lung = Mutations.Get("ExtraLung").ComponentsArray.OfType<IncreaseResourceAmount>().ToList();
            if (lung.Count != 1 || ((BlueprintReferenceBase)Archetype.GetF(lung[0], "m_Resource")).Guid.ToString() != Guids.BreathSurgeResource || lung[0].Value != ChimeraRules.Dials.LungSurgeBonus) d.Add("lung: IncreaseResourceAmount{surge, " + ChimeraRules.Dials.LungSurgeBonus + "} missing");
            if (BodyMutations.PendingSurge.Count != 0) d.Add("lung: " + BodyMutations.PendingSurge.Count + " surge row(s) still pending");
            d.AddRange(ResourceDiff(r => r.Resource == Guids.BreathResource || r.Resource == Guids.BreathSurgeResource));
            return d;
        }

        // T7: 독 — 특성(명중 트리거 → 표지 조건 → 버프 6라운드 + 표지) · 새 라운드 표지 제거 · DC 계산 · 버프(라운드마다 인내 → 실패 시 건강 1d2, 변이약 DC +2).
        public static List<string> VenomDiff()
        {
            var d = new List<string>();
            if (Venom.Buff == null || Venom.Spent == null) { d.Add("venom: not built"); return d; }
            var f = Mutations.Get("Venom");
            var trig = f.ComponentsArray.OfType<Kingmaker.UnitLogic.Mechanics.Components.AddInitiatorAttackWithWeaponTrigger>().ToList();
            if (trig.Count != 1 || !trig[0].OnlyHit || !trig[0].AllNaturalAndUnarmed || trig[0].ActionsOnInitiator) d.Add("venom: weapon trigger (OnlyHit, AllNaturalAndUnarmed, on target)");
            else
            {
                // 독립 검토 I-2: 표지 조건(Not = true) · 독 버프 적용(6라운드, 대상, 영구 아님) · 표지 적용(영구, 시전자) 을 값으로 되읽는다.
                var gate = trig[0].Action.Actions.OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>().ToList();
                var conds = gate.Count == 1 ? gate[0].ConditionsChecker.Conditions.OfType<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact>().ToList() : new List<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact>();
                if (gate.Count != 1 || trig[0].Action.Actions.Length != 1 || conds.Count != 1 || gate[0].ConditionsChecker.Conditions.Length != 1 || !conds[0].Not
                    || ((BlueprintReferenceBase)Archetype.GetF(conds[0], "m_Fact")).Guid.ToString() != Guids.VenomSpent) d.Add("venom: trigger gate is not [caster lacks VenomSpent]");
                else
                {
                    var applies = gate[0].IfTrue.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff>().ToList();
                    Func<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff, string> buffOf = a => ((BlueprintReferenceBase)Archetype.GetF(a, "m_Buff")).Guid.ToString();
                    var poison = applies.Where(a => buffOf(a) == Guids.VenomBuff).ToList();
                    var mark = applies.Where(a => buffOf(a) == Guids.VenomSpent).ToList();
                    if (applies.Count != 2 || gate[0].IfTrue.Actions.Length != 2 || gate[0].IfFalse.Actions.Length != 0) d.Add("venom: gate IfTrue is not [apply poison, apply marker] / IfFalse not empty");
                    if (poison.Count != 1 || poison[0].Permanent || poison[0].ToCaster || poison[0].AsChild || poison[0].DurationValue.Rate != Kingmaker.UnitLogic.Mechanics.DurationRate.Rounds
                        || poison[0].DurationValue.BonusValue.ValueType != Kingmaker.UnitLogic.Mechanics.ContextValueType.Simple || poison[0].DurationValue.BonusValue.Value != VenomPlan.Rounds
                        || poison[0].DurationValue.DiceType != Kingmaker.RuleSystem.DiceType.Zero)
                        d.Add("venom: poison buff is not applied to the target for " + VenomPlan.Rounds + " rounds");
                    if (mark.Count != 1 || !mark[0].Permanent || !mark[0].ToCaster) d.Add("venom: VenomSpent is not applied Permanent to the caster");
                }
            }
            var clear = f.ComponentsArray.OfType<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>().ToList();
            Func<Kingmaker.ElementsSystem.ActionList, bool> removesSpent = l => l != null && l.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff>().Any(r => ((BlueprintReferenceBase)Archetype.GetF(r, "m_Buff")).Guid.ToString() == Guids.VenomSpent);
            if (clear.Count != 1 || !removesSpent(clear[0].NewRound) || !removesSpent(clear[0].Deactivated)) d.Add("venom: VenomSpent not removed on NewRound/Deactivated");
            foreach (var bp in new BlueprintScriptableObject[] { f, Venom.Buff })
            {
                var p = bp.ComponentsArray.OfType<ContextCalculateAbilityParamsBasedOnClass>().ToList();
                if (p.Count != 1 || p[0].StatType != StatType.Wisdom || ((BlueprintReferenceBase)Archetype.GetF(p[0], "m_CharacterClass")).Guid.ToString() != Guids.Game.MonkClass) d.Add("venom: " + bp.name + " DC params not monk/Wisdom");
            }
            var rounds = Venom.Buff.ComponentsArray.OfType<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>().ToList();
            var save = rounds.Count == 1 ? rounds[0].NewRound.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow>().ToList() : new List<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow>();
            if (save.Count != 1 || save[0].Type.ToString() != VenomPlan.Save) d.Add("venom buff: NewRound saving throw " + VenomPlan.Save + " missing");
            else
            {
                var inc = (Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow.ConditionalDCIncrease[])Archetype.GetF(save[0], "m_ConditionalDCIncrease");
                if (inc == null || inc.Length != 1) d.Add("venom buff: conditional DC increase count " + (inc == null ? 0 : inc.Length));
                else
                {
                    // 독립 검토 I-2: +2 값 · 조건 = 변이약 버프 3 중 하나(Or, Not 없음).
                    var cs = inc[0].Condition == null ? new Kingmaker.ElementsSystem.Condition[0] : inc[0].Condition.Conditions;
                    var facts = cs.OfType<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact>().Where(c => !c.Not).Select(c => ((BlueprintReferenceBase)Archetype.GetF(c, "m_Fact")).Guid.ToString()).OrderBy(x => x).ToList();
                    if (inc[0].Value.ValueType != Kingmaker.UnitLogic.Mechanics.ContextValueType.Simple || inc[0].Value.Value != VenomPlan.DcMutagenBonus) d.Add("venom buff: conditional DC increase is not +" + VenomPlan.DcMutagenBonus);
                    if (inc[0].Condition == null || inc[0].Condition.Operation != Kingmaker.ElementsSystem.Operation.Or || cs.Length != facts.Count || !facts.SequenceEqual(Mutagen.Buffs.Select(b => b.AssetGuid.ToString()).OrderBy(x => x)))
                        d.Add("venom buff: DC increase condition is not [caster has any mutagen buff]");
                }
                var failed = save[0].Actions.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved>().SelectMany(x => x.Failed.Actions).OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>().ToList();
                if (failed.Count != 1 || failed[0].AbilityType.ToString() != VenomPlan.Stat || failed[0].Value.DiceType.ToString() != "D" + VenomPlan.Die.Sides || failed[0].Value.DiceCountValue.Value != VenomPlan.Die.Count)
                    d.Add("venom buff: failed save is not " + VenomPlan.Stat + " " + VenomPlan.Die);
            }
            return d;
        }

        // T7: 부분 변환 — 복제 그래프에 ShifterClass 0, 능력 복제에 ActivatableAbilityResourceLogic 0, 원본 시프터 버프 CRC 는 ShifterClass 그대로, 강화 표 == AspectPlan.
        public static List<string> AspectDiff()
        {
            var d = new List<string>();
            if (!Aspects.Built) { d.Add("aspects: not built"); return d; }
            string shifter = Guids.Game.ShifterClass;
            foreach (var kv in Aspects.GraphClones)
            {
                var bp = (BlueprintScriptableObject)kv.Value;
                var bad = bp.ComponentsArray.SelectMany(Aspects.ClassRefs).Where(g => g != Guids.Game.MonkClass).ToList();
                if (bad.Count > 0) d.Add("aspect clone " + bp.name + ": class refs " + string.Join(",", bad));
                var src = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(kv.Key)) as BlueprintScriptableObject;
                if (src == null || !src.ComponentsArray.SelectMany(Aspects.ClassRefs).All(g => g == shifter) || !src.ComponentsArray.SelectMany(Aspects.ClassRefs).Any()) d.Add("vanilla " + kv.Key + ": class refs no longer [ShifterClass] (original written?)");
                var shared = CloneKit.Shared(bp, src, BlueprintClone.KeepShared, CloneBoundary.DetachDepth);
                if (shared.Count > 0) d.Add("aspect clone " + bp.name + " shares " + string.Join(",", shared));
            }
            foreach (var form in AspectPlan.Forms)
            {
                BlueprintBuff buff, boost;
                Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility ab;
                if (!Aspects.Buffs.TryGetValue(form.Key, out buff) || !Aspects.Boosts.TryGetValue(form.Key, out boost) || !Aspects.Abilities.TryGetValue(form.Key, out ab)) { d.Add("aspect " + form.Key + ": not built"); continue; }
                if (ab.ComponentsArray.Any(c => c is Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic)) d.Add("aspect " + form.Key + ": ability still needs ShifterAspectResource");
                if (((BlueprintReferenceBase)Archetype.GetF(ab, "m_Buff")).Guid.ToString() != form.OurBuff) d.Add("aspect " + form.Key + ": ability buff is not our clone");
                // 2026-11-16: 복제 토글은 본편 배타 그룹 밖(가진 형태 전부 동시에 켬).
                if (ab.Group.ToString() != AspectPlan.ToggleGroup) d.Add("aspect " + form.Key + ": toggle group " + ab.Group + ", want " + AspectPlan.ToggleGroup);
                var tables = boost.ComponentsArray.OfType<ContextRankConfig>().Select(c => CrcKey(c).Split(new[] { "/" }, StringSplitOptions.None).Last()).ToList();
                var want = form.Crcs.Select(c => string.Join(",", AspectPlan.BoostTable(c, BoostPlan.For(ModifierDescriptorHelper.IsStackable(Fab.EnumOf<ModifierDescriptor>(c.Descriptor)))).Select(x => x.ToString()))).ToList();
                if (!tables.SequenceEqual(want)) d.Add("aspect " + form.Key + ": boost tables " + string.Join(" ; ", tables) + " want " + string.Join(" ; ", want));
                var extra = buff.ComponentsArray.OfType<BuffExtraEffects>().ToList();
                if (extra.Count != 1 || ((BlueprintReferenceBase)Archetype.GetF(extra[0], "m_ExtraEffectBuff")).Guid.ToString() != form.OurBoost) d.Add("aspect " + form.Key + ": boost not wired");
                foreach (var stage in ChimeraRules.AspectStages)
                {
                    var formFacts = Mutations.Option(stage.Id, form.Key).ComponentsArray.OfType<AddFacts>().SelectMany(x => (BlueprintUnitFactReference[])Archetype.GetF(x, "m_Facts")).Select(r => r.Guid.ToString()).ToList();
                    if (!formFacts.SequenceEqual(new[] { form.OurAbility })) d.Add("aspect " + stage.Id + "/" + form.Key + ": form feature AddFacts " + string.Join(",", formFacts));
                }
            }
            foreach (var row in AspectPlan.Graph)
            {
                SimpleBlueprint c;
                if (!Aspects.GraphClones.TryGetValue(row.SourceGuid, out c)) { d.Add("aspect graph row " + row.SourceGuid + ": not cloned"); continue; }
                var afl = ((BlueprintScriptableObject)c).ComponentsArray.OfType<AddFeatureOnClassLevel>().Select(a => (a.Feature == null ? "null" : a.Feature.AssetGuid.ToString()) + "@" + a.Level + "/" + a.BeforeThisLevel).OrderBy(x => x).ToList();
                var want = row.Overrides.Select(o => o.Item1 + "@" + o.Item2 + "/" + o.Item3).OrderBy(x => x).ToList();
                if (!afl.SequenceEqual(want)) d.Add("aspect graph " + c.name + ": " + string.Join(",", afl) + " want " + string.Join(",", want));
            }
            return d;
        }




    }
}
