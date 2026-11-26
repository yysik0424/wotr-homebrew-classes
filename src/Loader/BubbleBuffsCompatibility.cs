using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UMM = UnityModManagerNet.UnityModManager;

namespace HomebrewClasses.Loader
{
    internal static class BubbleBuffsCompatibility
    {
        const string Owner = "HomebrewClasses.BubbleBuffs";
        static readonly Guid BlastStrike = new Guid("eb3bfd5c933d4a0d9d8ea6faad499079");
        static readonly Guid SpiritFullAttack = new Guid("43f07397b3b34211bfd8487d513a9319");
        static readonly HashSet<string> RoutineActions = new HashSet<string> {
            "PackWarden.PackEmpowerOnPack", "WeaponSpiritShaman.PossessionAction", "Golemancer.ContextActionGolemOverdrive"
        };
        sealed class Routine
        {
            internal Guid Marker;
            internal Func<UnitEntityData, bool> Present;
        }
        static readonly Dictionary<object, Routine> effects = new Dictionary<object, Routine>();
        static readonly HashSet<Guid> abilities = new HashSet<Guid>();
        static ConstructorInfo makeBuff, makeEffects;
        static Type effectInterface;
        static MethodInfo setLong;
        static FieldInfo targetUnit, taskAbility;
        static IDictionary cache, names;

        internal static void Install(UMM.ModEntry entry, Action<string> error)
        {
            if (entry == null || !entry.Active || entry.Assembly == null) return;
            InstallAssembly(entry.Assembly, error);
        }

        internal static void InstallAssembly(Assembly assembly, Action<string> error)
        {
            var patcher = new Harmony(Owner);
            try
            {
                var state = assembly.GetType("BubbleBuffs.BufferState", true);
                var combined = assembly.GetType("BubbleBuffs.AbilityCombinedEffects", true);
                effectInterface = assembly.GetType("BubbleBuffs.IBeneficialEffect", true);
                makeEffects = combined.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(effectInterface) });
                makeBuff = assembly.GetType("BubbleBuffs.BuffEffect", true).GetConstructor(new[] { typeof(Guid) });
                setLong = AccessTools.PropertySetter(combined, "IsLong");
                cache = (IDictionary)AccessTools.Field(state, "SpellsWithBeneficialBuffs").GetValue(null);
                names = (IDictionary)AccessTools.Field(state, "SpellNames").GetValue(null);
                targetUnit = AccessTools.Field(assembly.GetType("BubbleBuffs.UnitBuffData", true), "Unit");
                taskAbility = AccessTools.Field(assembly.GetType("BubbleBuffs.CastTask", true), "SpellToCast");
                if (makeBuff == null || makeEffects == null || setLong == null || targetUnit == null || taskAbility == null || cache == null || names == null)
                    throw new MissingMemberException("Bubble Buffs routine metadata");
                Patch(patcher, state, "AddBuff", nameof(Discover));
                Patch(patcher, combined, "IsPresent", nameof(Present));
                Patch(patcher, assembly.GetType("BubbleBuffs.BuffProvider", true), "CanTarget", nameof(CanTarget));
                Patch(patcher, assembly.GetType("BubbleBuffs.InstantExecutionEngine", true), "Cast", nameof(CanCast));
            }
            catch (Exception ex)
            {
                patcher.UnpatchAll(Owner);
                error("Bubble Buffs compatibility: " + ex);
            }
        }

        static void Patch(Harmony patcher, Type type, string name, string prefix)
        {
            patcher.Patch(AccessTools.DeclaredMethod(type, name), prefix: new HarmonyMethod(typeof(BubbleBuffsCompatibility), prefix));
        }

        static bool Discover(AbilityData spell)
        {
            var bp = spell.Blueprint;
            var id = new Guid(bp.AssetGuid.ToByteArray());
            if (id == SpiritFullAttack || id == BlastStrike) return false;
            if (spell.Spellbook != null || bp.HasVariants || cache.Contains(id)) return true;
            var actions = bp.GetComponent<AbilityEffectRunAction>()?.Actions?.Actions;
            if (actions == null || actions.Length != 1 || actions[0] == null) return true;
            var action = actions[0];
            var type = action.GetType();
            if (!RoutineActions.Contains(type.FullName)) return true;
            var marker = (BlueprintBuff)type.GetMethod("RoutineBuff").Invoke(action, null);
            if (marker == null) return false;
            var markerId = new Guid(marker.AssetGuid.ToByteArray());
            var effect = makeBuff.Invoke(new object[] { markerId });
            var list = Array.CreateInstance(effectInterface, 1);
            list.SetValue(effect, 0);
            var combined = makeEffects.Invoke(new object[] { list });
            setLong.Invoke(combined, new[] { type.GetMethod("RoutineIsLong").Invoke(action, null) });
            var present = (Func<UnitEntityData, bool>)Delegate.CreateDelegate(typeof(Func<UnitEntityData, bool>), action, type.GetMethod("RoutinePresent"));
            effects.Add(combined, new Routine { Marker = markerId, Present = present });
            abilities.Add(id);
            cache[id] = combined;
            names[id] = spell.Name;
            return true;
        }

        static bool Present(object __instance, object unitBuffData, HashSet<Guid> ignoreForOverwriteCheck, ref bool __result)
        {
            Routine routine;
            if (!effects.TryGetValue(__instance, out routine)) return true;
            __result = !ignoreForOverwriteCheck.Contains(routine.Marker) && routine.Present((UnitEntityData)targetUnit.GetValue(unitBuffData));
            return false;
        }

        static bool Ready(AbilityData spell)
        {
            return !abilities.Contains(new Guid(spell.Blueprint.AssetGuid.ToByteArray())) || spell.IsAvailable;
        }

        static bool CanTarget(AbilityData ___spell) { return Ready(___spell); }
        static bool CanCast(object task) { return Ready((AbilityData)taskAbility.GetValue(task)); }
    }
}
