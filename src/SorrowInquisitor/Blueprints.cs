using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace SorrowInquisitor
{
    internal static class Blueprints
    {
        internal static BlueprintCharacterClass Inquisitor;
        internal static BlueprintFeature Training, Residual;
        internal static BlueprintBuff Ward, Stored;
        internal static bool Registered;

        internal static void BuildAll()
        {
            Afflictions.CheckBindings();
            Inquisitor = Get<BlueprintCharacterClass>(Guids.Inquisitor);
            var gaze = Get<BlueprintFeature>(Guids.Gaze);
            var solo = Get<BlueprintFeature>(Guids.Solo);
            var team = Get<BlueprintFeature>(Guids.Team);
            var icon = gaze.Icon ?? team.Icon;
            var bane = Get<BlueprintBuff>(Guids.Bane).Icon ?? icon;
            Stored = Buff(Guids.BeadStatus, "rosary", icon);
            Stored.Stacking = StackingType.Stack;
            AddComponents(Stored, new BeadStatus());
            Ward = Buff(Guids.WardBuff, "vow", icon);
            Ward.Stacking = StackingType.Stack;
            AddComponents(Ward, new VowWatcher());
            MakeAbility(Guids.Gather, "gather", icon, Rite.Gather, UnitCommand.CommandType.Standard);
            MakeAbility(Guids.Sentence, "sentence", icon, Rite.Standard, UnitCommand.CommandType.Standard);
            MakeAbility(Guids.Move, "move", icon, Rite.Move, UnitCommand.CommandType.Move);
            MakeAbility(Guids.Swift, "swift", icon, Rite.Swift, UnitCommand.CommandType.Swift);
            MakeAbility(Guids.Arm, "blade", bane, Rite.Blade, UnitCommand.CommandType.Free);
            MakeAbility(Guids.Guard, "vow", icon, Rite.Vow, UnitCommand.CommandType.Move);
            MakeAbility(Guids.Discard, "discard", icon, Rite.Discard, UnitCommand.CommandType.Free);
            Training = Feature(Guids.Training, "rosary", icon, Guids.Gather, Guids.Sentence, Guids.Discard);
            AddComponents(Training, new RosaryEvents());
            Residual = Feature(Guids.Residual, "residual", icon);
            var blade = Feature(Guids.Blade, "blade", bane, Guids.Arm);
            var move = Feature(Guids.MoveFeature, "move", icon, Guids.Move);
            var swift = Feature(Guids.SwiftFeature, "swift", icon, Guids.Swift);
            var reprisal = Feature(Guids.Reprisal, "reprisal", icon);
            var vow = Feature(Guids.Ward, "vow", icon, Guids.Guard);
            var divine = Feature(Guids.Divine, "divine", bane);
            var removals = new[] { Entry(1, gaze), Entry(3, solo, team), Entry(6, team), Entry(9, team),
                Entry(12, team), Entry(15, team), Entry(18, team) };
            foreach (var row in removals)
                if (!row.Features.All(f => Inquisitor.Progression.LevelEntries.Any(e => e.Level == row.Level && e.Features.Contains(f))))
                    throw new InvalidOperationException("Inquisitor progression exchange missing at " + row.Level);
            var archetype = Create<BlueprintArchetype>("SorrowInquisitorArchetype", Guids.Archetype, a => {
                SetField(a, "m_ParentClass", Inquisitor);
                a.LocalizedName = Strings.L("archetype.name"); a.LocalizedDescription = Strings.L("archetype.desc");
                a.LocalizedDescriptionShort = Strings.L("archetype.short");
                a.RecommendedAttributes = Inquisitor.RecommendedAttributes.ToArray();
                a.NotRecommendedAttributes = Inquisitor.NotRecommendedAttributes.ToArray();
                a.RemoveFeatures = removals;
                a.AddFeatures = new[] { Entry(1, Training), Entry(5, blade), Entry(6, move), Entry(7, reprisal),
                    Entry(11, swift, vow), Entry(16, divine) };
            });
            RequireOwnLocalizedStrings(); RequireOwnBuffFx(true);
            var old = (BlueprintArchetypeReference[])FindField(typeof(BlueprintCharacterClass), "m_Archetypes").GetValue(Inquisitor);
            if (old == null || old.Any(r => r == null || r.Guid.ToString() == Guids.Archetype))
                throw new InvalidOperationException("Invalid or duplicate inquisitor archetype.");
            SetField(Inquisitor, "m_Archetypes", old.Concat(new[] { Ref<BlueprintArchetypeReference>(Guids.Archetype) }).ToArray());
            Registered = Inquisitor.Archetypes.Count(a => a == archetype) == 1;
            if (!Registered) throw new InvalidOperationException("Sorrow Inquisitor registration failed.");
        }

        static void MakeAbility(string guid, string key, Sprite icon, Rite rite, UnitCommand.CommandType action)
        {
            bool self = rite == Rite.Blade || rite == Rite.Discard;
            var bp = Create<BlueprintAbility>("SorrowInquisitorAbility_" + key, guid, a => {
                Name(a, key, icon); a.Type = AbilityType.Supernatural; a.ActionType = action;
                a.Range = self ? AbilityRange.Personal : AbilityRange.Custom; a.CustomRange = new Feet(Rules.RangeFeet);
                a.CanTargetSelf = self; a.CanTargetPoint = false; a.CanTargetEnemies = !self;
                a.CanTargetFriends = rite == Rite.Gather || rite == Rite.Vow;
                a.SpellResistance = false; a.LocalizedDuration = a.LocalizedSavingThrow = NoText();
            });
            AddComponents(bp, new PerformRite { Kind = rite }, new RiteGate { Kind = rite });
        }
        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features)
        {
            var row = new LevelEntry { Level = level };
            row.SetFeatures(features);
            return row;
        }

        static void Name(BlueprintUnitFact f, string key, Sprite icon)
        {
            if (icon == null) throw new InvalidOperationException("Missing Sorrow Inquisitor icon: " + key);
            SetField(f, "m_DisplayName", Strings.L(key + ".name"));
            SetField(f, "m_Description", Strings.L(key + ".desc"));
            SetField(f, "m_DescriptionShort", NoText());
            SetField(f, "m_Icon", icon);
        }

        static BlueprintFeature Feature(string id, string key, Sprite icon, params string[] abilities)
        {
            var f = Create<BlueprintFeature>("SorrowInquisitorFeature_" + key, id, x => {
                Name(x, key, icon); x.Ranks = 1; x.IsClassFeature = true;
            });
            if (abilities.Length > 0)
            {
                var grant = new AddFacts();
                SetField(grant, "m_Facts", abilities.Select(Ref<BlueprintUnitFactReference>).ToArray());
                AddComponents(f, grant);
            }
            return f;
        }

        static BlueprintBuff Buff(string id, string key, Sprite icon)
        {
            return Create<BlueprintBuff>("SorrowInquisitorBuff_" + key, id, x => {
                Name(x, key, icon);
                x.Stacking = StackingType.Replace;
                x.FxOnStart = new PrefabLink();
                x.FxOnRemove = new PrefabLink();
            });
        }

    }
}
