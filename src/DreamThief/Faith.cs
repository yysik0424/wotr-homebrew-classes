using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.Other.NestedSelectionGroup;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using static WotrClassMod.Common.BlueprintUtil;

namespace DreamThief
{
    internal static class Faith
    {
        internal const string DeitySelection = "59e7a76987fe3b547b9cce045f4db3e4";
        internal static BlueprintFeature Desna;
        internal static BlueprintFeatureSelection Choice;
        internal static bool OtherFaith(UnitDescriptor unit) =>
            Choice.AllFeatures.Any(f => f != Desna && unit.HasFact(f));

        internal static void Prepare(BlueprintArchetype archetype, BlueprintFeature desna)
        {
            Desna = desna; Choice = Get<BlueprintFeatureSelection>(DeitySelection);
            if (!Choice.AllFeatures.Contains(desna) || Choice.AllFeatures.Count() < 2)
                throw new InvalidOperationException("Desna missing from the game's deity selection.");
            var worship = new PrerequisiteFeature { Group = Prerequisite.GroupType.Any };
            SetField(worship, "m_Feature", Ref<BlueprintFeatureReference>(GameAssets.Desna));
            var pending = new PendingFaith { Group = Prerequisite.GroupType.Any };
            SetField(pending, "m_Feature", Ref<BlueprintFeatureReference>(DeitySelection));
            AddComponents(archetype, worship, pending);
        }

        // Match Mantis Zealot: class choice precedes deity choice on new characters.
        // Keep the original deity features and all their prerequisites, adding one class exclusion.
        internal static void RestrictOtherDeities()
        {
            foreach (var deity in Choice.AllFeatures.Where(d => d != Desna))
            {
                var block = new PrerequisiteNoArchetype { name = "$DreamThief$ExcludeOtherFaith", HideInUI = true };
                SetField(block, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(GameAssets.Rogue));
                SetField(block, "m_Archetype", Ref<BlueprintArchetypeReference>(Blueprints.Archetype.AssetGuid.ToString()));
                deity.ComponentsArray = deity.ComponentsArray.Concat(new[] { block }).ToArray();
                // Original components keep the same blueprint owner, ordering and identity.
                block.OwnerBlueprint = deity;
            }
        }

        [HarmonyPatch(typeof(CharGenClassSelectorItemVM), "GetArchetypesList")]
        internal static class HideUnavailable
        {
            static readonly FieldInfo Controller = typeof(CharGenClassSelectorItemVM).GetField("LevelUpController",
                BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new MissingFieldException("LevelUpController");
            static void Postfix(CharGenClassSelectorItemVM __instance, BlueprintCharacterClass __0,
                List<NestedSelectionGroupEntityVM> __result)
            {
                if (!Blueprints.Registered || __0 != Blueprints.Rogue) return;
                var controller = (LevelUpController)Controller.GetValue(__instance);
                if (!OtherFaith(controller.Unit)) return;
                __result.RemoveAll(item => item is CharGenClassSelectorItemVM option && option.Archetype == Blueprints.Archetype);
            }
        }
    }

    public sealed class PendingFaith : PrerequisiteNoFeature
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            // Some NPCs and old saves hold a deity fact without the deity selection marker.
            return !Faith.OtherFaith(unit) && base.CheckInternal(selectionState, unit, state);
        }
    }
}
