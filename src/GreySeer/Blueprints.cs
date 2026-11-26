using System;
using System.Linq;
using System.Reflection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace GreySeer
{
    internal static class Blueprints
    {
        internal static BlueprintFeature Training, Cantrips;
        internal static BlueprintActivatableAbility AutoStrike, AutoGuarded;
        internal static BlueprintArchetype Archetype;
        internal static BlueprintCharacterClass Arcanist, Fighter;
        internal static BlueprintAbilityResource Reservoir;
        internal static bool Registered;
        internal static FieldInfo OriginalAbility;

        public static void BuildAll()
        {
            OriginalAbility = typeof(UnitUseAbility).GetField("m_AbilityOriginal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (OriginalAbility == null || OriginalAbility.FieldType != typeof(AbilityData))
                throw new MissingFieldException("Grey Seer cannot locate the original ability used for slot spending.");
            Arcanist = Get<BlueprintCharacterClass>(Guids.Arcanist);
            Fighter = Get<BlueprintCharacterClass>(Guids.Fighter);
            Cantrips = Get<BlueprintFeature>(Guids.Cantrips);
            Reservoir = Get<BlueprintAbilityResource>(Guids.Reservoir);
            if (Arcanist.Spellbook.AssetGuid.ToString() != Guids.Spellbook || Arcanist.BaseAttackBonus == null || Fighter.BaseAttackBonus == null)
                throw new InvalidOperationException("Grey Seer: missing attack progression or unexpected arcanist spellbook.");
            var exploit = Get<BlueprintFeature>(Guids.Exploit);
            var removals = Rules.ExchangeLevels.Select(level => {
                if (!Arcanist.Progression.LevelEntries.Any(e => e.Level == level && e.Features.Contains(exploit)))
                    throw new InvalidOperationException("Grey Seer: missing arcanist exploit at level " + level);
                return Entry(level, exploit);
            }).ToArray();
            var twoWeapon = Get<BlueprintFeature>(Guids.TwoWeapon);
            var weaponChoice = WeaponChoices.Build(twoWeapon.Icon);
            var strikeIcon = Get<BlueprintAbility>(Guids.VitalStrikeAbility).Icon;
            var guardedIcon = Get<BlueprintFeature>(Guids.Improved).Icon;
            MakeMark(Guids.Mark, "mark", false, strikeIcon);
            MakeMark(Guids.SafeMark, "safe", true, guardedIcon);
            AutoStrike = MakeToggle("AutoStrike", "auto", Guids.AutoStrike, Guids.AutoStrikeBuff, twoWeapon.Icon);
            AutoGuarded = MakeToggle("AutoGuarded", "autoSafe", Guids.AutoGuarded, Guids.AutoGuardedBuff, Get<BlueprintFeature>(Guids.Greater).Icon);
            Training = Create<BlueprintFeature>("GreySeerSwordAndStaff", Guids.Training, f => {
                Name(f, "training", twoWeapon.Icon);
                f.Ranks = 1;
                f.IsClassFeature = true;
            });
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.Mark), Ref<BlueprintUnitFactReference>(Guids.SafeMark),
                Ref<BlueprintUnitFactReference>(Guids.AutoStrike), Ref<BlueprintUnitFactReference>(Guids.AutoGuarded) });
            AddComponents(Training, grant, new WeaponAccuracy(), new SpellLink());
            Archetype = Create<BlueprintArchetype>("GreySeerArchetype", Guids.Archetype, a => {
                SetField(a, "m_ParentClass", Arcanist);
                a.LocalizedName = Strings.L("archetype.name");
                a.LocalizedDescription = Strings.L("archetype.desc");
                a.LocalizedDescriptionShort = Strings.L("archetype.short");
                a.RecommendedAttributes = Arcanist.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = Arcanist.NotRecommendedAttributes.ToArray();
                a.RemoveFeatures = removals;
                a.AddFeatures = new[] { Entry(1, weaponChoice, Training, twoWeapon), Entry(7, Get<BlueprintFeature>(Guids.Improved)), Entry(13, Get<BlueprintFeature>(Guids.Greater)) };
            });
            RequireOwnLocalizedStrings();
            RequireOwnBuffFx(true);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass), "m_Archetypes").GetValue(Arcanist);
            if (old == null || old.Any(r => r == null || r.Guid.ToString() == Guids.Archetype))
                throw new InvalidOperationException("Grey Seer: invalid or already registered arcanist archetype entries.");
            SetField(Arcanist, "m_Archetypes", old.Concat(new[]{Ref<BlueprintArchetypeReference>(Guids.Archetype)}).ToArray());
            Registered = Arcanist.Archetypes.Count(a => a == Archetype) == 1;
            if (!Registered) throw new InvalidOperationException("Grey Seer: failed to register with arcanist.");
        }

        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features);
            return entry;
        }

        static void Name(BlueprintUnitFact fact, string key, UnityEngine.Sprite icon)
        {
            if (icon == null) throw new InvalidOperationException("Grey Seer: missing icon for " + key);
            SetField(fact, "m_DisplayName", Strings.L(key + ".name"));
            SetField(fact, "m_Description", Strings.L(key + ".desc"));
            SetField(fact, "m_DescriptionShort", NoText());
            SetField(fact, "m_Icon", icon);
        }

        static BlueprintActivatableAbility MakeToggle(string id, string key, string guid, string buffGuid, UnityEngine.Sprite icon)
        {
            var buff = Create<BlueprintBuff>("GreySeer" + id + "Buff", buffGuid, b => {
                Name(b, key, icon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                b.FxOnStart = new Kingmaker.ResourceLinks.PrefabLink();
                EmptyFxOnRemove(b);
            });
            var toggle = Create<BlueprintActivatableAbility>("GreySeer" + id, guid, t => {
                Name(t, key, icon);
                SetField(t, "m_Buff", Ref<BlueprintBuffReference>(buffGuid));
                t.ActivationType = AbilityActivationType.Immediately;
                t.Group = ActivatableAbilityGroup.None;
                t.DeactivateImmediately = true;
                t.DoNotTurnOffOnRest = true;
                t.IsOnByDefault = false;
            });
            if (toggle.Buff != buff) throw new InvalidOperationException("Grey Seer: missing stance buff for " + id);
            return toggle;
        }

        static void MakeMark(string guid, string key, bool guarded, UnityEngine.Sprite icon)
        {
            var ability = Create<BlueprintAbility>("GreySeer" + (guarded ? "GuardedDesignation" : "Designation"), guid, a => {
                Name(a, key, icon);
                a.Type = AbilityType.Supernatural;
                a.ActionType = UnitCommand.CommandType.Free;
                a.Range = AbilityRange.Unlimited;
                a.CanTargetEnemies = true;
                a.CanTargetSelf = a.CanTargetFriends = a.CanTargetPoint = false;
                a.SpellResistance = false;
                a.LocalizedDuration = NoText();
                a.LocalizedSavingThrow = NoText();
            });
            AddComponents(ability, new DesignateTarget { Guarded = guarded }, new WeaponRequirement());
        }
    }
}
