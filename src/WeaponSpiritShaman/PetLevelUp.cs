using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using WotrClassMod.Common;

namespace WeaponSpiritShaman
{
    internal static class PetLevelUp
    {
        // The base unit already has the correct AddClassLevels before its first level.
        // Its serialized class history can also contain an ordinary animal level.
        internal static bool IsSpirit(UnitDescriptor unit)
        {
            if (unit == null || PetClass.Class == null) return false;
            var blueprint = unit.OriginalBlueprint ?? unit.Blueprint;
            return blueprint != null && blueprint.GetComponent<AddClassLevels>()?.CharacterClass == PetClass.Class;
        }

        internal static void RestoreWeaponSlots(UnitEntityData pet)
        {
            if (PetClass.AnimalSlotLock == null || !IsSpirit(pet.Descriptor)) return;
            var lockFact = pet.GetFact(PetClass.AnimalSlotLock);
            if (lockFact == null) return;
            // OnTurnOff releases exactly the locks acquired by this fact, even on relog.
            UnitHelper.RemoveFact(pet, lockFact);
            ModContext.Log("WSS restored weapon slots after animal companion level: " + pet.UniqueId);
        }
    }

    [HarmonyPatch(typeof(ProgressionRoot), "get_AvailablePetClasses")]
    internal static class SpiritPetClassList
    {
        static void Postfix(ref IEnumerable<BlueprintCharacterClass> __result)
        {
            if (Archetype.Registered) __result = Include(__result, PetClass.Class);
        }

        internal static IEnumerable<BlueprintCharacterClass> Include(IEnumerable<BlueprintCharacterClass> classes, BlueprintCharacterClass spirit)
        {
            foreach (var entry in classes)
            {
                if (entry != spirit) yield return entry;
            }
            yield return spirit;
        }
    }

    [HarmonyPatch(typeof(BlueprintCharacterClass), nameof(BlueprintCharacterClass.MeetsPrerequisites))]
    internal static class SpiritPetClassEligibility
    {
        static void Postfix(BlueprintCharacterClass __instance, UnitDescriptor __0, ref bool __result)
        {
            if (!__result || !Archetype.Registered) return;
            if (__instance != PetClass.AnimalClass && __instance != PetClass.Class) return;
            // Both classes hide restricted rows. The chooser and SelectClass.Check use this same method.
            __result = (__instance == PetClass.Class) == PetLevelUp.IsSpirit(__0);
        }
    }
}
