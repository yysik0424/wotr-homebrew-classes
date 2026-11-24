using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using static WotrClassMod.Common.BlueprintUtil;

namespace DreamThief
{
    internal static class BlueprintCopy
    {
        // Keep engine assets referenced by ID. Detach all nested mutable effect and targeting data,
        // including ActionList elements. Create calls OnEnable after the complete tree is installed.
        static bool IsAsset(object item) => item is SimpleBlueprint || item is UnityEngine.Object;

        internal static T Copy<T>(T source, string key, Func<BlueprintComponent, bool> keep, Action<T> configure)
            where T : BlueprintScriptableObject, new()
        {
            return Create<T>("DreamThief_" + key, Blueprints.Id(key), result => {
                for (var type = typeof(T); type != typeof(BlueprintScriptableObject); type = type.BaseType)
                    foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        if (!f.IsInitOnly && !f.IsNotSerialized)
                            f.SetValue(result, CloneKit.CopyOf(f.GetValue(source), IsAsset, 48));
                result.ComponentsArray = source.ComponentsArray.Where(keep).Select(component =>
                    (BlueprintComponent)CloneKit.CopyOf(component, IsAsset, 48)).ToArray();
                configure(result);
            });
        }

        // OnEnable sets owners on ElementsArray, but never discovers new actions. The native
        // asset importer normally fills this list. Rebuild it after the last edit to our graphs.
        internal static void BindElements(BlueprintScriptableObject blueprint)
        {
            var list = blueprint.ElementsArray;
            list.Clear();
            var seen = new HashSet<object>();
            foreach (var component in blueprint.ComponentsArray) Visit(component, blueprint, seen, list);
        }

        static void Visit(object item, BlueprintScriptableObject owner, HashSet<object> seen, List<Element> elements)
        {
            if (item == null || !seen.Add(item)) return;
            if (item is Element element) { element.Owner = owner; elements.Add(element); }
            if (item is IEnumerable sequence && !(item is string))
            {
                foreach (var child in sequence) Visit(child, owner, seen, elements);
                return;
            }
            if (!(item is Element || item is ActionList || item is ConditionsChecker || item is BlueprintComponent)) return;
            for (var type = item.GetType(); type != typeof(object); type = type.BaseType)
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    Visit(field.GetValue(item), owner, seen, elements);
        }
    }
}
