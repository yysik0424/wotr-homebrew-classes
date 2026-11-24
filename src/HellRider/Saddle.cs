using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.Animation;
using Kingmaker.Visual.Mounts;
using UnityEngine;
using static WotrClassMod.Common.BlueprintUtil;

namespace HellRider
{
    // The rider's IK controller looks on the mount view itself for the offsets component.
    // Copy horse mounting anchors onto this nightmare instance; never edit either prefab.
    [HarmonyPatch(typeof(IKController), "MountedCombatIk")]
    internal static class Saddle
    {
        static void Prefix(UnitEntityData __0)
        {
            if (Blueprints.Nightmare == null || __0?.Blueprint != Blueprints.Nightmare || __0.View == null) return;
            var view = __0.View;
            var offsets = view.GetComponent<MountOffsets>();
            if (offsets != null && offsets.Root != null && offsets.LargeOffsetsConfig != null) return;
            var horse = Get<BlueprintUnit>(Ids.AnimalCompanionUnitHorse).Prefab.Load();
            var model = horse.GetComponent<MountOffsets>();
            if (model == null || model.Root == null)
                throw new InvalidOperationException("The native horse has no mounting anchors");
            if (offsets == null) offsets = view.gameObject.AddComponent<MountOffsets>();
            // Both shipped prefabs use LowerTorso/UpperTorso. The rigs directly above
            // them have different names, and the two mounting roots attach to that rig.
            var horseRig = horse.transform.Find("HorseArmored_RIG");
            var nightmareRig = view.transform.Find("HorseCountry_RIG");
            if (horseRig == null || nightmareRig == null)
                throw new InvalidOperationException("The native horse and nightmare riding rigs have changed");
            var mapped = new Dictionary<Transform,Transform> {
                { horse.transform, view.transform }, { horseRig, nightmareRig }
            };
            var bones = view.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(r => r.bones).Where(t => t != null).Distinct().ToLookup(t => t.name);
            Transform Anchor(Transform source)
            {
                if (source == null) return null;
                if (mapped.TryGetValue(source,out var existing)) return existing;
                var parent = Anchor(source.parent);
                if (parent == null) throw new InvalidOperationException("Horse mounting anchor lies outside the model");
                var child = parent.Find(source.name);
                var sameName = bones[source.name].ToArray();
                if (child == null && sameName.Length == 1) child = sameName[0];
                if (child == null)
                {
                    child = new GameObject(source.name).transform;
                    child.SetParent(parent,false);
                    child.localPosition = source.localPosition;
                    child.localRotation = source.localRotation;
                    child.localScale = source.localScale;
                }
                mapped.Add(source,child);
                return child;
            }
            foreach (var f in typeof(MountOffsets).GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
            {
                if (f.FieldType == typeof(Transform)) f.SetValue(offsets,Anchor((Transform)f.GetValue(model)));
                else if (f.FieldType == typeof(RaceMountOffsetsConfig) || f.FieldType == typeof(float))
                    f.SetValue(offsets,f.GetValue(model));
            }
            if (offsets.Root == null || offsets.PelvisIkTarget == null || offsets.LargeOffsetsConfig == null)
                throw new InvalidOperationException("Nightmare mount setup did not supply the native horse anchors");
        }
    }
}
