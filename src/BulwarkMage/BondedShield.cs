using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
     
    //   회복: BulwarkBondResource(본편 ItemBondResource 값: 하루 1) · BulwarkBondAbility(본편 ItemBondAbility 의 AbilityResourceLogic·AbilityRestoreSpellSlot 복사, 자원 = 우리 것)
    //         · BulwarkBondAbilityFact(AddFacts[능력]) · BulwarkBondFeature(L1) = AddAbilityResources + HasShieldFeatureUnlock{TowerShield → 능력 부여 문 특성}
    //         → 타워실드를 보조손에 들지 않으면 능력 자체가 없다. 능력은 본편처럼 숨김이며, 소모된 주문 칸의 변환(AbilityData.GetConversions @0233~0297)으로 쓴다.
    //   강화: BulwarkBondEnhance{n}(n = 1..4, 레벨 BondEnhancementLevels[n−1]) = HasShieldFeatureUnlock{TowerShield → BulwarkBondEnhanceBuff{n}},
    //         BulwarkBondEnhanceBuff{n} = [BondOffHandEnchant{m_Enchantment = BulwarkBondEnchant{n}}] — 지금 보조손의 타워실드 하나에만 붙인다(A2b, 배치 판정 = BondPlacement).
    //         BulwarkBondEnchant{n} = 본편 ArmorEnhancementBonus{n} 의 값 복사(컴포넌트 [ArmorEnhancementBonus{n}, AdvanceArmorStats{ACP +1}] 복사본 + 표시 문구·비용 필드) —
    //         우리 블루프린트라 방패 위의 「우리 것」을 알아볼 수 있다(본편 AEB 는 마법 의복 등이 같은 시전자로 붙일 수 있어 구별 불가). 강화 종류가 같아 큰 값 하나(BondTable.Effective),
    //         층마다 ACP +1 누적(A6)은 이전과 같다. 이전 BuffEnchantArmor 의 고정 색인(BuffScaling) 문제와 캐릭터 레벨 21 한계는 함께 사라졌다.
    // 본편 블루프린트에는 쓰지 않는다(읽기·복사만 — 시험 exec: base-blueprint writes). 만든 뒤 VerifyNow 가 값·참조·공유를 되읽는다(차이 = 던짐 → BuildFailed). 점검 S15 가 같은 대조를 다시 돈다.
    public static class BondedShield
    {
        public static BlueprintAbilityResource Resource;
        public static BlueprintAbility Ability;
        public static BlueprintFeature AbilityFact;
        public static BlueprintFeature Feature;
        static readonly List<BlueprintFeature> enhance = new List<BlueprintFeature>();
        static readonly List<BlueprintBuff> enhanceBuffs = new List<BlueprintBuff>();
        static readonly List<BlueprintArmorEnchantment> enchants = new List<BlueprintArmorEnchantment>();






        public static IReadOnlyList<BlueprintArmorEnchantment> Enchants { get { return enchants; } }

        // 새 GUID(Guids.cs) — 층 n 의 강화 특성·버프.
        static readonly string[] EnhanceGuids = { Guids.BondEnhance1, Guids.BondEnhance2, Guids.BondEnhance3, Guids.BondEnhance4 };
        static readonly string[] EnhanceBuffGuids = { Guids.BondEnhanceBuff1, Guids.BondEnhanceBuff2, Guids.BondEnhanceBuff3, Guids.BondEnhanceBuff4 };

        public static bool Built
        {
            get { return Resource != null && Ability != null && AbilityFact != null && Feature != null && enhance.Count == BulwarkRules.Levels.BondEnhancementLevels.Count && enhanceBuffs.Count == enhance.Count && enchants.Count == enhance.Count; }
        }

        public static void Build()
        {
            if (Resource != null || Ability != null || enhance.Count != 0) throw new InvalidOperationException("BulwarkMage: BondedShield.Build() called twice");
            var levels = BulwarkRules.Levels.BondEnhancementLevels;
            if (levels == null || levels.Count == 0 || levels.Count != EnhanceGuids.Length || levels.Count != EnhanceBuffGuids.Length) throw new InvalidOperationException("BulwarkMage: BondEnhancementLevels does not match the issued bond GUIDs");
            BuildRecall();
            Archetype.AddAt(BulwarkRules.Levels.MinLevel, Feature);
            for (int n = 1; n <= levels.Count; n++)
            {
                enchants.Add(BuildEnchant(n));
                var buff = BuildEnhanceBuff(n);
                enhanceBuffs.Add(buff);
                var f = NewFeature("BulwarkBondEnhance" + n, EnhanceGuids[n - 1], true);
                AddComponents(f, ShieldGate(EnhanceBuffGuids[n - 1]));
                enhance.Add(f);
            }
            for (int k = 0; k < levels.Count; k++) Archetype.AddAt(levels[k], enhance[k]);
            var diffs = VerifyNow();
            if (diffs.Count != 0) throw new InvalidOperationException("BulwarkMage: bonded shield read-back differs: " + string.Join(" | ", diffs.Take(8).ToArray()));
        }

        static void BuildRecall()
        {
            var srcRes = Get<BlueprintAbilityResource>(Guids.Game.ItemBondResource);
            var srcAb = Get<BlueprintAbility>(Guids.Game.ItemBondAbility);
            Resource = Create<BlueprintAbilityResource>("BulwarkBondResource", Guids.BondResource, r =>
            {
                r.LocalizedName = Strings.L("bond.name");
                r.LocalizedDescription = Strings.L("bond.desc");
                object amount = Activator.CreateInstance(FindField(typeof(BlueprintAbilityResource), "m_MaxAmount").FieldType);
                Rebuke.FillEmpty(amount);
                SetField(amount, "BaseValue", BulwarkRules.Dials.BondRecallsPerDay);
                SetField(amount, "IncreasedByLevel", false);
                SetField(amount, "IncreasedByStat", false);
                SetField(amount, "IncreasedByLevelStartPlusDivStep", false);
                SetField(r, "m_MaxAmount", amount);
                SetField(r, "m_UseMax", false);
                SetField(r, "m_Min", GetF(srcRes, "m_Min"));
            });
            // 능력: 본편 값 복사(표시 문구만 우리 것). FillEmpty 는 쓰지 않는다 — 제한 캐시(m_CachedRestrictions 등)를 빈 배열로 채우면 AbilityRestoreSpellSlot 제한이 안 보인다.
            Ability = Create<BlueprintAbility>("BulwarkBondAbility", Guids.BondAbility, a =>
            {
                SetField(a, "m_DisplayName", Strings.L("bond.name"));
                SetField(a, "m_Description", Strings.L("bond.desc"));
                SetField(a, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                SetField(a, "m_Icon", GetF(srcAb, "m_Icon"));
                SetField(a, "m_AutoUseIsForbidden", GetF(srcAb, "m_AutoUseIsForbidden"));
                SetField(a, "m_IsFullRoundAction", GetF(srcAb, "m_IsFullRoundAction"));
                a.Type = srcAb.Type; a.Range = srcAb.Range; a.CanTargetPoint = srcAb.CanTargetPoint; a.CanTargetEnemies = srcAb.CanTargetEnemies; a.CanTargetFriends = srcAb.CanTargetFriends;
                a.CanTargetSelf = srcAb.CanTargetSelf; a.ShouldTurnToTarget = srcAb.ShouldTurnToTarget; a.SpellResistance = srcAb.SpellResistance; a.ActionBarAutoFillIgnored = srcAb.ActionBarAutoFillIgnored;
                a.Hidden = srcAb.Hidden; a.NeedEquipWeapons = srcAb.NeedEquipWeapons; a.NotOffensive = srcAb.NotOffensive; a.EffectOnAlly = srcAb.EffectOnAlly; a.EffectOnEnemy = srcAb.EffectOnEnemy;
                a.Animation = srcAb.Animation; a.HasFastAnimation = srcAb.HasFastAnimation; a.AnimationStyle = srcAb.AnimationStyle; a.ActionType = srcAb.ActionType; a.AvailableMetamagic = srcAb.AvailableMetamagic;
                a.LocalizedDuration = new LocalizedString();
                a.LocalizedSavingThrow = new LocalizedString();
                a.ResourceAssetIds = new string[0];
            });
            var logic = TowerShield.Copy(TowerShield.Exactly<AbilityResourceLogic>(srcAb, 1)[0]);
            SetField(logic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.BondResource));
            var restore = TowerShield.Copy(TowerShield.Exactly<AbilityRestoreSpellSlot>(srcAb, 1)[0]);
            AddComponents(Ability, logic, restore);

            AbilityFact = NewFeature("BulwarkBondAbilityFact", Guids.BondAbilityFact, true);
            var facts = Comp(new AddFacts());
            SetField(facts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.BondAbility) });
            AddComponents(AbilityFact, facts);

            Feature = NewFeature("BulwarkBondFeature", Guids.BondFeature, false);
            var res = Comp(new AddAbilityResources { RestoreAmount = true, UseThisAsResource = false, Amount = 0, RestoreOnLevelUp = false });
            SetField(res, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.BondResource));
            AddComponents(Feature, res, ShieldGate(Guids.BondAbilityFact));
        }

        // 우리 강화 마법부여 n = 본편 ArmorEnhancementBonus{n} 의 값 복사(본편 객체에는 쓰지 않는다 — 컴포넌트는 TowerShield.Copy 사본, 문구는 같은 LocalizedString 을 가리킨다).
        static BlueprintArmorEnchantment BuildEnchant(int n)
        {
            var src = Get<BlueprintArmorEnchantment>(BondTable.EnhancementGuid(n));
            var aeb = TowerShield.Exactly<ArmorEnhancementBonus>(src, 1)[0];
            var aas = TowerShield.Exactly<AdvanceArmorStats>(src, 1)[0];
            if ((src.ComponentsArray ?? new BlueprintComponent[0]).Length != 2) throw new InvalidOperationException("BulwarkMage: vanilla " + src.name + " has " + src.ComponentsArray.Length + " components, expected [ArmorEnhancementBonus, AdvanceArmorStats] (vanilla layout changed?)");
            var e = Create<BlueprintArmorEnchantment>("BulwarkBondEnchant" + n, BondTable.OurEnchantmentGuid(n), x =>
            {
                SetField(x, "m_EnchantName", GetF(src, "m_EnchantName"));
                SetField(x, "m_Description", GetF(src, "m_Description"));
                SetField(x, "m_Prefix", GetF(src, "m_Prefix"));
                SetField(x, "m_Suffix", GetF(src, "m_Suffix"));
                SetField(x, "m_EnchantmentCost", GetF(src, "m_EnchantmentCost"));
                SetField(x, "m_IdentifyDC", GetF(src, "m_IdentifyDC"));
                SetField(x, "m_AllowNonContextActions", GetF(src, "m_AllowNonContextActions"));
                SetField(x, "m_HiddenInUI", GetF(src, "m_HiddenInUI"));
            });
            AddComponents(e, TowerShield.Copy(aeb), TowerShield.Copy(aas));
            return e;
        }

        // 복사하는 마법부여 필드(BlueprintItemEnchantment 선언, dnlib 2026-11-14): 표시 문구 4 · 비용 · 감정 난이도 · 비문맥 행동 허용 · UI 숨김.
        static readonly string[] EnchantFields = { "m_EnchantName", "m_Description", "m_Prefix", "m_Suffix", "m_EnchantmentCost", "m_IdentifyDC", "m_AllowNonContextActions", "m_HiddenInUI" };

        static BlueprintBuff BuildEnhanceBuff(int n)
        {
            var b = Create<BlueprintBuff>("BulwarkBondEnhanceBuff" + n, EnhanceBuffGuids[n - 1], x =>
            {
                SetField(x, "m_DisplayName", Strings.L("bond.name"));
                SetField(x, "m_Description", Strings.L("bond.desc"));
                SetField(x, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                x.Stacking = StackingType.Replace;
                SetField(x, "m_Flags", Enum.Parse(FindField(typeof(BlueprintBuff), "m_Flags").FieldType, "HiddenInUi"));
                x.IsClassFeature = true;
                Rebuke.FillEmpty(x);
                // 2026-11-17(GameLogFull: 강화 버프 제거 → Buff.OnRemove @00B0 NRE): 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말). FillEmpty 는 PrefabLink 를 채우지 않는다.
                EmptyFxOnRemove(x);
            });
            var c = Comp(new BondOffHandEnchant());
            c.m_Enchantment = Ref<BlueprintItemEnchantmentReference>(BondTable.OurEnchantmentGuid(n));
            AddComponents(b, c);
            return b;
        }

        // 보조손 타워실드일 때만 newFact(본편 TowerShieldTraining 과 같은 문 — 방패 숙련군 플래그 TowerShield, VerifyNow 가 본편 값과 교차 확인).
        static HasShieldFeatureUnlock ShieldGate(string newFact)
        {
            var c = Comp(new HasShieldFeatureUnlock());
            SetField(c, "FilterByShieldProficiencyGroup", true);
            SetField(c, "m_ShiledProficiencyGroupEntries", ArmorProficiencyGroupFlag.TowerShield);
            SetField(c, "FilterByBlueprintShieldTypes", false);
            SetField(c, "m_DisableWhenHasArmor", false);
            SetField(c, "m_NewFact", Ref<BlueprintUnitFactReference>(newFact));
            return c;
        }

        static BlueprintFeature NewFeature(string name, string guid, bool hidden)
        {
            return Create<BlueprintFeature>(name, guid, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("bond.name"));
                SetField(f, "m_Description", Strings.L("bond.desc"));
                SetField(f, "m_DescriptionShort", NoText()); // 2026-11-17: 짧은 설명 = 빈 키(설명으로 대체) — null 이면 툴팁에 "<null>"(BlueprintUtil 「LocalizedString 필드」 주석)
                f.Ranks = 1;
                f.IsClassFeature = true;
                f.HideInUI = hidden;
                f.Groups = new FeatureGroup[0];
                f.ReapplyOnLevelUp = false;
                Rebuke.FillEmpty(f);
            });
        }

        static T Comp<T>(T c) where T : BlueprintComponent { Rebuke.FillEmpty(c); return c; }

        // ---------------- 되읽기(빌드 시 + 점검 S15) — 차이 목록(빈 목록 = 일치) ----------------
        public static List<string> VerifyNow()
        {
            var d = new List<string>();
            if (!Built) { d.Add("not built (resource/ability/facts/" + enhance.Count + " enhancement features)"); return d; }
            var srcRes = Get<BlueprintAbilityResource>(Guids.Game.ItemBondResource);
            var srcAb = Get<BlueprintAbility>(Guids.Game.ItemBondAbility);
            // 자원: 본편 값과 같음(하루 1, 증가 없음).
            object a = GetF(Resource, "m_MaxAmount"), sa = GetF(srcRes, "m_MaxAmount");
            foreach (var n in new[] { "BaseValue", "IncreasedByLevel", "IncreasedByStat", "IncreasedByLevelStartPlusDivStep" })
                if (!Equals(GetF(a, n), GetF(sa, n))) d.Add("resource m_MaxAmount." + n + " " + GetF(a, n) + " != vanilla " + GetF(sa, n));
            if (!Equals(GetF(Resource, "m_Min"), GetF(srcRes, "m_Min")) || !Equals(GetF(Resource, "m_UseMax"), GetF(srcRes, "m_UseMax"))) d.Add("resource m_Min/m_UseMax differ from vanilla ItemBondResource");
            if (!Equals(GetF(a, "BaseValue"), BulwarkRules.Dials.BondRecallsPerDay)) d.Add("resource BaseValue != Dials.BondRecallsPerDay");
            // 능력: 복사한 값 == 본편, 컴포넌트 = [자원 논리(우리 자원), 칸 회복], 본편과 공유 없음.
            if (Ability.Type != srcAb.Type || Ability.Range != srcAb.Range || Ability.ActionType != srcAb.ActionType || Ability.Hidden != srcAb.Hidden || Ability.CanTargetSelf != srcAb.CanTargetSelf
                || Ability.CanTargetEnemies != srcAb.CanTargetEnemies || Ability.CanTargetFriends != srcAb.CanTargetFriends || Ability.CanTargetPoint != srcAb.CanTargetPoint || Ability.AvailableMetamagic != srcAb.AvailableMetamagic
                || !Equals(GetF(Ability, "m_AutoUseIsForbidden"), GetF(srcAb, "m_AutoUseIsForbidden")) || !Equals(GetF(Ability, "m_IsFullRoundAction"), GetF(srcAb, "m_IsFullRoundAction")))
                d.Add("ability copied fields differ from vanilla ItemBondAbility");
            var comps = Ability.ComponentsArray ?? new BlueprintComponent[0];
            var logic = comps.OfType<AbilityResourceLogic>().ToArray();
            var restore = comps.OfType<AbilityRestoreSpellSlot>().ToArray();
            if (comps.Length != 2 || logic.Length != 1 || restore.Length != 1) d.Add("ability components [" + string.Join(",", comps.Select(c => c == null ? "null" : c.GetType().Name).ToArray()) + "], expected [AbilityResourceLogic, AbilityRestoreSpellSlot]");
            else
            {
                var sl = TowerShield.Exactly<AbilityResourceLogic>(srcAb, 1)[0];
                var sr = TowerShield.Exactly<AbilityRestoreSpellSlot>(srcAb, 1)[0];
                if (RefGuid(GetF(logic[0], "m_RequiredResource")) != Guids.BondResource) d.Add("ability resource logic does not point at BulwarkBondResource");
                if (logic[0].Amount != sl.Amount || logic[0].CostIsCustom != sl.CostIsCustom || !Equals(GetF(logic[0], "m_IsSpendResource"), GetF(sl, "m_IsSpendResource"))) d.Add("ability resource logic values differ from vanilla");
                if (restore[0].AnySpellLevel != sr.AnySpellLevel || restore[0].SpellLevel != sr.SpellLevel || restore[0].CheckSpellBook != sr.CheckSpellBook
                    || !(restore[0].SpellbooksReference ?? new BlueprintSpellbookReference[0]).Select(r => r.Guid.ToString()).SequenceEqual((sr.SpellbooksReference ?? new BlueprintSpellbookReference[0]).Select(r => r.Guid.ToString())))
                    d.Add("ability restore-slot values differ from vanilla");
                if (!restore[0].SpellbooksReference.Any(r => r.Guid.ToString() == Guids.Game.WizardSpellbook)) d.Add("ability restore-slot does not list the wizard spellbook");
                foreach (var p in new[] { Tuple.Create((object)logic[0], (object)sl, "resource logic"), Tuple.Create((object)restore[0], (object)sr, "restore slot") })
                {
                    if (ReferenceEquals(p.Item1, p.Item2)) d.Add("ability " + p.Item3 + " is the vanilla instance");
                    var shared = CloneKit.Shared(p.Item1, p.Item2, TowerShield.KeepShared, 6);
                    if (shared.Count != 0) d.Add("ability " + p.Item3 + " shares mutable sub-objects with vanilla: " + string.Join(", ", shared.ToArray()));
                }
                if (comps.Any(c => !ReferenceEquals(c.OwnerBlueprint, Ability))) d.Add("ability component not owned by BulwarkBondAbility");
            }
            // 문: 타워실드 숙련군 플래그 == 본편 TowerShieldTraining 의 값(같은 비트인지 교차 확인).
            var tss = Get<BlueprintFeature>("d2dbc71b45efa8749bd9c1924534b2bb").ComponentsArray.OfType<HasShieldFeatureUnlock>().ToArray();
            if (tss.Length != 1) d.Add("vanilla TowerShieldTraining has " + tss.Length + " HasShieldFeatureUnlock");
            Action<BlueprintFeature, string> gate = (f, want) =>
            {
                var g = f.ComponentsArray.OfType<HasShieldFeatureUnlock>().ToArray();
                if (g.Length != 1) { d.Add(f.name + " has " + g.Length + " HasShieldFeatureUnlock"); return; }
                if (tss.Length == 1 && !Equals(GetF(g[0], "m_ShiledProficiencyGroupEntries"), GetF(tss[0], "m_ShiledProficiencyGroupEntries"))) d.Add(f.name + " shield flag != vanilla TowerShieldTraining's");
                if (!Equals(GetF(g[0], "FilterByShieldProficiencyGroup"), true) || !Equals(GetF(g[0], "FilterByBlueprintShieldTypes"), false) || !Equals(GetF(g[0], "m_DisableWhenHasArmor"), false)) d.Add(f.name + " gate filters wrong");
                if (RefGuid(GetF(g[0], "m_NewFact")) != want) d.Add(f.name + " gate grants " + RefGuid(GetF(g[0], "m_NewFact")) + ", expected " + want);
            };
            gate(Feature, Guids.BondAbilityFact);
            var ar = Feature.ComponentsArray.OfType<AddAbilityResources>().ToArray();
            if (ar.Length != 1 || RefGuid(GetF(ar[0], "m_Resource")) != Guids.BondResource || !ar[0].RestoreAmount) d.Add("bond feature AddAbilityResources wrong");
            var af = AbilityFact.ComponentsArray.OfType<AddFacts>().ToArray();
            var afRefs = af.Length == 1 ? (BlueprintUnitFactReference[])GetF(af[0], "m_Facts") : null;
            if (afRefs == null || afRefs.Length != 1 || afRefs[0].Guid.ToString() != Guids.BondAbility) d.Add("ability fact AddFacts != [BulwarkBondAbility]");
            // 강화: 층 n = 레벨 BondEnhancementLevels[n−1] 에 정확히 1번, 문 → 버프 n, 버프 = [BondOffHandEnchant{우리 BondEnchant{n}}], 우리 마법부여 n = 본편 AEB{n} 값 복사(공유 없음).
            var levels = BulwarkRules.Levels.BondEnhancementLevels;
            for (int n = 1; n <= enhance.Count; n++)
            {
                var f = enhance[n - 1];
                int at = Archetype.Bp.AddFeatures.Where(e => e.Level == levels[n - 1]).SelectMany(e => e.Features).Count(x => x != null && x.AssetGuid == f.AssetGuid);
                int anywhere = Archetype.Bp.AddFeatures.SelectMany(e => e.Features).Count(x => x != null && x.AssetGuid == f.AssetGuid);
                if (at != 1 || anywhere != 1) d.Add(f.name + " is at level " + levels[n - 1] + " " + at + " time(s), anywhere " + anywhere);
                gate(f, EnhanceBuffGuids[n - 1]);
                var boe = enhanceBuffs[n - 1].ComponentsArray.OfType<BondOffHandEnchant>().ToArray();
                if (boe.Length != 1 || enhanceBuffs[n - 1].ComponentsArray.Length != 1) { d.Add(enhanceBuffs[n - 1].name + " is not exactly [BondOffHandEnchant]"); continue; }
                if (boe[0].m_Enchantment == null || boe[0].m_Enchantment.Guid.ToString() != BondTable.OurEnchantmentGuid(n) || !ReferenceEquals(boe[0].m_Enchantment.Get(), enchants[n - 1])) { d.Add(enhanceBuffs[n - 1].name + " does not place BulwarkBondEnchant" + n); continue; }
                foreach (var x in VerifyEnchant(n)) d.Add(x);
            }
            return d;
        }

        // 우리 마법부여 n == 본편 ArmorEnhancementBonus{n}: 필드 값 · 컴포넌트 형식 순서 · 컴포넌트 값 · 소유 · 하위 객체 공유 0. 본편 값 자체(강화 n·ACP 1)는 런타임 되읽기 행(T6)과 시험 jbp 가 잠근다.
        static List<string> VerifyEnchant(int n)
        {
            var d = new List<string>();
            var ours = enchants[n - 1];
            var src = Get<BlueprintArmorEnchantment>(BondTable.EnhancementGuid(n));
            if (ours.AssetGuid.ToString() != BondTable.OurEnchantmentGuid(n) || ours.name != "BulwarkBondEnchant" + n) d.Add("enchantment " + n + " guid/name readback " + ours.AssetGuid + " " + ours.name);
            foreach (var f in EnchantFields) if (!Equals(GetF(ours, f), GetF(src, f))) d.Add(ours.name + "." + f + " != vanilla " + src.name);
            var a = ours.ComponentsArray ?? new BlueprintComponent[0];
            var s = src.ComponentsArray ?? new BlueprintComponent[0];
            if (a.Length != 2 || s.Length != 2 || !(a[0] is ArmorEnhancementBonus) || !(a[1] is AdvanceArmorStats)) { d.Add(ours.name + " components [" + string.Join(",", a.Select(c => c == null ? "null" : c.GetType().Name).ToArray()) + "], expected [ArmorEnhancementBonus, AdvanceArmorStats]"); return d; }
            var ae = (ArmorEnhancementBonus)a[0]; var se = s.OfType<ArmorEnhancementBonus>().Single();
            var aa = (AdvanceArmorStats)a[1]; var sa = s.OfType<AdvanceArmorStats>().Single();
            if (ae.EnhancementValue != se.EnhancementValue || ae.EnhancementValue != n) d.Add(ours.name + " EnhancementValue " + ae.EnhancementValue + ", vanilla " + se.EnhancementValue + ", layer " + n);
            if (aa.ArmorCheckPenaltyShift != sa.ArmorCheckPenaltyShift || aa.MaxDexBonusShift != sa.MaxDexBonusShift || aa.ArcaneSpellFailureShift != sa.ArcaneSpellFailureShift) d.Add(ours.name + " AdvanceArmorStats differ from vanilla");
            foreach (var p in new[] { Tuple.Create((object)ae, (object)se), Tuple.Create((object)aa, (object)sa) })
            {
                if (ReferenceEquals(p.Item1, p.Item2)) d.Add(ours.name + " component " + p.Item1.GetType().Name + " is the vanilla instance");
                var shared = CloneKit.Shared(p.Item1, p.Item2, TowerShield.KeepShared, 6);
                if (shared.Count != 0) d.Add(ours.name + " " + p.Item1.GetType().Name + " shares mutable sub-objects with vanilla: " + string.Join(", ", shared.ToArray()));
            }
            if (a.Any(c => !ReferenceEquals(c.OwnerBlueprint, ours))) d.Add(ours.name + " component not owned by it");
            return d;
        }

        static string RefGuid(object r)
        {
            var b = r as BlueprintReferenceBase;
            return b == null ? "null" : b.Guid.ToString();
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("BulwarkMage: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
