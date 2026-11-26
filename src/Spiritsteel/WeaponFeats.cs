using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.WeaponEnchants;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spiritsteel
{
    internal static class WeaponFeats
    {
        internal const string PointBlankGuid = "0da0c194d6e1d43419eb8d990b28e0ab";
        private const string BasicSelectionGuid = "247a4068296e8be42890143f451b4b45";
        internal static BlueprintCharacterClass Barbarian { get; private set; }
        internal static readonly Dictionary<WeaponFeatKind, BlueprintFeature> Features = new Dictionary<WeaponFeatKind, BlueprintFeature>();
        internal static readonly Dictionary<WeaponFeatKind, BlueprintFeature> Native = new Dictionary<WeaponFeatKind, BlueprintFeature>();
        private static readonly Dictionary<WeaponFeatKind, BlueprintWeaponEnchantment[]> Effects = new Dictionary<WeaponFeatKind, BlueprintWeaponEnchantment[]>();
        private static readonly HashSet<BlueprintItemEnchantment> OurEffects = new HashSet<BlueprintItemEnchantment>();
        internal static BlueprintFeature PointBlank;
        internal static BlueprintAbility BasicTrip;
        internal static BlueprintFeatureSelection Selection;

        // Stock weapon enchantments provide the matching held-weapon effect.
        private static readonly Dictionary<WeaponFeatKind, string> ElementEnchantments = new Dictionary<WeaponFeatKind, string> {
            { WeaponFeatKind.Acid, "633b38ff1d11de64a91d490c683ab1c8" },
            { WeaponFeatKind.Fire, "30f90becaaac51f41bf56641966c4121" },
            { WeaponFeatKind.Cold, "421e54078b7719d40915ce0672511d0b" },
            { WeaponFeatKind.Electricity, "7bda5277d36ad114f9f9fd21d0dab658" },
            { WeaponFeatKind.Sonic, "690e762f7704e1f4aa1ac69ef0ce6a96" }
        };

        internal static void AddStrings(Dictionary<string, (string ko, string en)> table) { WeaponFeatRow.AddStrings(table); }

        internal static string StableId(string key)
        {
            using (var hash = MD5.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("HomebrewClasses.Spiritsteel.WeaponFeats|" + key))).Replace("-", "").ToLowerInvariant();
        }

        internal static BlueprintFeatureSelection Build(BlueprintCharacterClass barbarian)
        {
            if (Barbarian != null || barbarian == null) throw new InvalidOperationException("Spiritsteel: invalid weapon feat build");
            Barbarian = barbarian;
            PointBlank = Get<BlueprintFeature>(PointBlankGuid);
            BasicTrip = Get<BlueprintAbility>("6fd05c4ecfebd6f4d873325de442fc17");
            var template = Get<BlueprintFeatureSelection>(BasicSelectionGuid);
            WeaponTactics.Build();
            foreach (var row in WeaponFeatRow.All)
            {
                var source = Get<BlueprintUnitFact>(row.IconSource);
                if (source.Icon == null) throw new InvalidOperationException("Spiritsteel: missing weapon feat icon " + source.name);
                if (source is BlueprintFeature) Native.Add(row.Kind, (BlueprintFeature)source);
                var feature = Create<BlueprintFeature>("SpiritsteelWeaponFeat" + row.Kind, StableId(row.Kind + ".choice"), f =>
                {
                    Name(f, row.Key("name"), row.Key("desc"), source);
                    f.Ranks = 1; f.IsClassFeature = true; f.Groups = new FeatureGroup[0];
                });
                Features.Add(row.Kind, feature);
                var minimum = new PrerequisiteClassLevel { Level = row.MinLevel };
                SetField(minimum, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(barbarian.AssetGuid.ToString()));
                AddComponents(feature, minimum, new WeaponFeatChange { Kind = row.Kind });
                if (row.MeleeOnly || row.Kind == WeaponFeatKind.CloseShot || row.Element || row.Kind == WeaponFeatKind.Agile || row.Kind == WeaponFeatKind.Specialization ||
                    row.Kind == WeaponFeatKind.Mobility || row.Kind == WeaponFeatKind.Shield || row.Kind == WeaponFeatKind.Precise ||
                    row.Kind == WeaponFeatKind.GreaterFocus || row.Kind == WeaponFeatKind.GreaterSpecialization)
                    AddComponents(feature, new WeaponFeatRequirement { Kind = row.Kind });
                int tiers = row.Element || row.Kind == WeaponFeatKind.Specialization ? WeaponFeatRow.DamageTiers : 1;
                var effects = new BlueprintWeaponEnchantment[tiers];
                var weaponFx = row.Element ? Get<BlueprintWeaponEnchantment>(ElementEnchantments[row.Kind]).WeaponFxPrefab : null;
                if (row.Element && string.IsNullOrEmpty(weaponFx?.AssetId))
                    throw new InvalidOperationException("Spiritsteel: missing native weapon FX for " + row.Kind);
                for (int i = 0; i < tiers; i++)
                {
                    int tier = i + 1;
                    var bp = Create<BlueprintWeaponEnchantment>("SpiritsteelWeaponEffect" + row.Kind + tier, StableId(row.Kind + ".effect." + tier), e =>
                    {
                        SetField(e, "m_EnchantName", Strings.L(row.Key(tiers == 1 ? "name" : "tier" + tier)));
                        SetField(e, "m_Description", Strings.L(row.Key("desc")));
                        SetField(e, "m_Prefix", NoText()); SetField(e, "m_Suffix", NoText());
                        e.WeaponFxPrefab = weaponFx ?? new PrefabLink();
                    });
                    AddComponents(bp, EnchantmentComponents(row, tier));
                    effects[i] = bp;
                    OurEffects.Add(bp);
                }
                Effects.Add(row.Kind, effects);
            }
            var options = WeaponFeatRow.All.Select(r => Ref<BlueprintFeatureReference>(Features[r.Kind].AssetGuid.ToString())).ToArray();
            Selection = Create<BlueprintFeatureSelection>("SpiritsteelWeaponFeatSelection", StableId("selection"), f =>
            {
                Name(f, "weaponfeat.selection.name", "weaponfeat.selection.desc", template);
                f.Mode = template.Mode; f.Group = FeatureGroup.None; f.Group2 = FeatureGroup.None;
                f.IgnorePrerequisites = false; f.Obligatory = true; f.IsClassFeature = true;
                f.Ranks = 1; f.Groups = new FeatureGroup[0]; f.ShowThisSelection = template.ShowThisSelection;
                SetField(f, "m_AllFeatures", options); SetField(f, "m_Features", options.ToArray());
            });
            return Selection;
        }

        internal static void Name(BlueprintUnitFact fact, string name, string description, BlueprintUnitFact source)
        {
            SetField(fact, "m_DisplayName", Strings.L(name)); SetField(fact, "m_Description", Strings.L(description));
            SetField(fact, "m_DescriptionShort", NoText()); SetField(fact, "m_Icon", source.Icon);
        }

        private static BlueprintComponent[] EnchantmentComponents(WeaponFeatRow row, int tier)
        {
            // These two marks are read when the game chooses its threat hand and defensive casting DC.
            if (row.Kind == WeaponFeatKind.Vigil || row.Kind == WeaponFeatKind.Disrupt) return Array.Empty<BlueprintComponent>();
            if (row.Kind != WeaponFeatKind.Unfettered) return new[] { Effect(row, tier) };
            // The four equipment immunities on the native Permanent Freedom boots.
            return new[] { UnitCondition.Paralyzed, UnitCondition.Entangled, UnitCondition.Slowed, UnitCondition.DifficultTerrain }
                .Select(condition => (BlueprintComponent)new AddConditionImmunityEquipment { Condition = condition }).ToArray();
        }

        private static BlueprintComponent Effect(WeaponFeatRow row, int tier)
        {
            if (row.Element)
            {
                var energy = (DamageEnergyType)Enum.Parse(typeof(DamageEnergyType), row.Kind.ToString());
                return new WeaponEnergyDamageDice { Element = energy, EnergyDamageDice = new DiceFormula(tier, DiceType.D6) };
            }
            switch (row.Kind)
            {
                case WeaponFeatKind.SightlessStrike: return new WeaponBlindFight();
                case WeaponFeatKind.CloseShot: return new WeaponCloseShot();
                case WeaponFeatKind.DisarmCritical: return new WeaponCriticalDisarm();
                case WeaponFeatKind.Lunge: return GrantFeature(WeaponTactics.LungeGrant.AssetGuid.ToString());
                case WeaponFeatKind.Spellcleave: return GrantFeature(WeaponTactics.CleaveGrant.AssetGuid.ToString());
                case WeaponFeatKind.Ghost: return new WeaponReality { Reality = DamageRealityType.Ghost };
                case WeaponFeatKind.Keen: return new WeaponCriticalEdgeIncrease();
                case WeaponFeatKind.Agile: return new WeaponDamageStatReplacement { Stat = StatType.Dexterity, RequiresFinesse = true };
                case WeaponFeatKind.Dodge: return new AddStatBonusEquipment { Stat = StatType.AC, Descriptor = ModifierDescriptor.Dodge, Value = 2 };
                case WeaponFeatKind.Fortitude: return WearerStat(row.Kind, StatType.SaveFortitude, 2);
                case WeaponFeatKind.Reflex: return WearerStat(row.Kind, StatType.SaveReflex, 2);
                case WeaponFeatKind.Will: return WearerStat(row.Kind, StatType.SaveWill, 2);
                case WeaponFeatKind.Initiative: return WearerStat(row.Kind, StatType.Initiative, 4);
                case WeaponFeatKind.Shield: return WearerStat(row.Kind, StatType.AC, 1);
                case WeaponFeatKind.Mobility: return new WeaponFeatMobility();
                case WeaponFeatKind.Trip: return new WeaponFeatTrip();
                case WeaponFeatKind.ColdIron: return new WeaponMaterial { Material = PhysicalDamageMaterial.ColdIron };
                case WeaponFeatKind.Adamantine: return new WeaponMaterial { Material = PhysicalDamageMaterial.Adamantite };
                case WeaponFeatKind.Watchful: return new AddStatBonusEquipment { Stat = StatType.SkillPerception, Descriptor = ModifierDescriptor.Competence, Value = 3 };
                case WeaponFeatKind.Silent: return new AddStatBonusEquipment { Stat = StatType.SkillStealth, Descriptor = ModifierDescriptor.Competence, Value = 3 };
                case WeaponFeatKind.Charging:
                case WeaponFeatKind.Opportunist:
                    return new WeaponFeatTacticalAttack { Kind = row.Kind };
                case WeaponFeatKind.GroundAssault:
                    return new WeaponFeatAttackAgainstCondition { Condition = UnitCondition.Prone };
                case WeaponFeatKind.FearPursuit:
                    return new WeaponFeatAttackAgainstCondition { Condition = UnitCondition.Shaken, Alternative = UnitCondition.Frightened };
                case WeaponFeatKind.EmberWard: return GrantFeature("a6acd8df82219d24c8f746659212543b");
                case WeaponFeatKind.GraveWard: return GrantFeature("a77db677646ebfa4792bf6fa0875d5da");
                case WeaponFeatKind.Speed: return new WeaponFeatSpeed();
                case WeaponFeatKind.ThunderingCritical: return new WeaponEnergyBurst { Element = DamageEnergyType.Sonic, Dice = DiceType.D8 };
                case WeaponFeatKind.Anchored: return new AddStatBonusEquipment { Stat = StatType.AdditionalCMD, Descriptor = ModifierDescriptor.Competence, Value = 4 };
                case WeaponFeatKind.GiantSlayer: return new WeaponFeatGiantSlayer();
                case WeaponFeatKind.Evasive: return new WeaponFeatReflexEvasion();
                case WeaponFeatKind.Judgment: return new WeaponFeatJudgment();
                case WeaponFeatKind.Sundering: return new WeaponFeatPenetration();
                case WeaponFeatKind.Fortified:
                case WeaponFeatKind.SpellWard:
                case WeaponFeatKind.SpiritSense:
                    return GrantFeature(row.IconSource);
                case WeaponFeatKind.GreaterFocus:
                case WeaponFeatKind.GreaterSpecialization:
                case WeaponFeatKind.Focus:
                case WeaponFeatKind.Specialization:
                case WeaponFeatKind.Critical:
                case WeaponFeatKind.Precise:
                    return new WeaponFeatAttack { Kind = row.Kind, Value = tier + 1 };
                default: throw new ArgumentOutOfRangeException(nameof(row.Kind), row.Kind, "Unexpected weapon inscription");
            }
        }

        private static AddUnitFeatureEquipment GrantFeature(string id)
        {
            var grant = new AddUnitFeatureEquipment();
            SetField(grant, "m_Feature", Ref<BlueprintFeatureReference>(id));
            return grant;
        }

        private static WeaponFeatWearerStat WearerStat(WeaponFeatKind kind, StatType stat, int value)
        {
            return new WeaponFeatWearerStat { Kind = kind, Stat = stat, Value = value };
        }

        internal static bool Inscribed(ItemEntityWeapon weapon, WeaponFeatKind kind)
        {
            BlueprintWeaponEnchantment[] effects;
            return weapon != null && Effects.TryGetValue(kind, out effects) && weapon.GetEnchantment(effects[0]) != null;
        }

        internal static bool HasNative(UnitEntityData unit, WeaponFeatKind kind, WeaponCategory? category = null)
        {
            var feat = Native[kind];
            if (!category.HasValue) return unit.Descriptor.HasFact(feat);
            return unit.Progression.Features.Enumerable.Any(f => f.Blueprint == feat && f.Param != null && f.Param.WeaponCategory == category);
        }

        // Reconcile only this module's enchantment identities. Game/scroll/artifact effects remain owned by their original sources.
        internal static void Synchronize(UnitEntityData owner, ItemEntityWeapon primary, ItemEntityWeapon second)
        {
            if (Selection == null || primary == null) return;
            int tier = WeaponFeatRow.DamageTier(owner.Progression.GetClassLevel(Barbarian));
            var firstEffects = new HashSet<BlueprintItemEnchantment>();
            var secondEffects = new HashSet<BlueprintItemEnchantment>();
            foreach (var row in WeaponFeatRow.All)
            {
                if (!owner.Descriptor.HasFact(Features[row.Kind]) || tier == 0) continue;
                var variants = Effects[row.Kind];
                var bp = variants[variants.Length == 1 ? 0 : tier - 1];
                firstEffects.Add(bp);
                if (row.OnBothHeads) secondEffects.Add(bp);
            }
            Place(primary, firstEffects);
            if (second != null && second != primary) Place(second, secondEffects);
        }

        private static void Place(ItemEntityWeapon item, HashSet<BlueprintItemEnchantment> wanted)
        {
            foreach (var old in item.Enchantments.Where(e => OurEffects.Contains(e.Blueprint)).ToArray())
                if (!wanted.Remove(old.Blueprint)) item.RemoveEnchantment(old);
            foreach (var bp in wanted)
                if (item.AddEnchantment(bp, null, default(Rounds?)) == null)
                    throw new InvalidOperationException("Spiritsteel: could not attach " + bp.name + " to " + item.UniqueId);
        }

        internal static void Forget(UnitEntityData owner, WeaponFeatKind kind, UnitFact departing)
        {
            if (!owner.PreviewOf.IsEmpty || ContextData<UnitEntityData.RespecCommit>.Current ||
                owner.Facts.List.Any(f => f != departing && f.Blueprint == departing.Blueprint)) return;
            var head = BoundWeapons.Item(owner);
            if (head == null) return;
            var variants = Effects[kind];
            foreach (var item in new[] { head, head.Second })
                if (item != null)
                    foreach (var old in item.Enchantments.Where(e => variants.Any(bp => bp == e.Blueprint)).ToArray())
                        item.RemoveEnchantment(old);
        }
    }

    public class WeaponFeatChange : UnitFactComponentDelegate
    {
        public WeaponFeatKind Kind;
        protected override void OnActivate() { BoundWeapons.Refresh(Owner); }
        protected override void OnDeactivate() { WeaponFeats.Forget(Owner, Kind, Fact); }
    }

    public class WeaponFeatRequirement : Prerequisite
    {
        public WeaponFeatKind Kind;

        protected override bool CheckInternal(FeatureSelectionState selection, UnitDescriptor unit, LevelUpState state)
        {
            var owner = unit.Unit;
            var category = BoundWeapons.SelectedCategory(owner);
            if (!category.HasValue) return false;
            if (Kind <= WeaponFeatKind.Sonic)
            {
                int selected = WeaponFeatRow.All.Count(r => r.Element && r.Kind != Kind &&
                    (unit.HasFact(WeaponFeats.Features[r.Kind]) || (state != null && state.Selections.Any(s => s != selection && s.IsSelectedInChildren(WeaponFeats.Features[r.Kind])))));
                return WeaponFeatRow.CanAddElement(selected);
            }
            var row = WeaponFeatRow.All.Single(r => r.Kind == Kind);
            if (row.MeleeOnly) return BoundWeapons.BaseWeapon(category.Value).IsMelee;
            if (Kind == WeaponFeatKind.CloseShot) return BoundWeapons.BaseWeapon(category.Value).IsRanged;
            switch (Kind)
            {
                case WeaponFeatKind.Agile: return WeaponFeats.HasNative(owner, Kind) && category.Value.HasSubCategory(WeaponSubCategory.Finessable) && BoundWeapons.BaseWeapon(category.Value).IsMelee;
                case WeaponFeatKind.GreaterFocus:
                case WeaponFeatKind.Specialization: return unit.HasFact(WeaponFeats.Features[WeaponFeatKind.Focus]) || WeaponFeats.HasNative(owner, WeaponFeatKind.Focus, category);
                case WeaponFeatKind.GreaterSpecialization: return unit.HasFact(WeaponFeats.Features[WeaponFeatKind.Specialization]) || WeaponFeats.HasNative(owner, WeaponFeatKind.Specialization, category);
                case WeaponFeatKind.Mobility: return unit.HasFact(WeaponFeats.Features[WeaponFeatKind.Dodge]) || WeaponFeats.HasNative(owner, WeaponFeatKind.Dodge);
                case WeaponFeatKind.Precise: return unit.HasFact(WeaponFeats.PointBlank) && BoundWeapons.BaseWeapon(category.Value).IsRanged;
                case WeaponFeatKind.Shield: return category == WeaponCategory.SpikedLightShield || category == WeaponCategory.SpikedHeavyShield;
                default: throw new InvalidOperationException("Spiritsteel: unexpected weapon feat requirement " + Kind);
            }
        }

        protected override string GetUITextInternal(UnitDescriptor unit)
        {
            var row = WeaponFeatRow.All.Single(r => r.Kind == Kind);
            string requirementKey = row.MeleeOnly ? "weaponfeat.melee.requirement" : Kind == WeaponFeatKind.GreaterFocus ? "weaponfeat.Specialization.requirement" : row.Key("requirement");
            string requirement = Strings.L(row.Element ? "weaponfeat.element.limit" : requirementKey).ToString();
            if (unit == null || (!row.Element && Kind != WeaponFeatKind.Specialization)) return requirement;
            int level = unit.Progression.GetClassLevel(WeaponFeats.Barbarian);
            int next = row.NextLevel(level);
            string growth = next > 0
                ? string.Format(Strings.L("weaponfeat.next").ToString(), row.ValueAt(level), next, row.ValueAt(next))
                : string.Format(Strings.L("weaponfeat.final").ToString(), row.ValueAt(level));
            return requirement + "\n" + growth;
        }
    }
}
