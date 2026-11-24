using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Parts;

namespace ShadowSorcerer
{
    // Keep every engine object at its normal type. Delayed projectile/effect queries can outlive the queue.
    static class EchoAbilityIdentity
    {
        // AbilityData.Equals(object) requires identical runtime types. Use the actual stock AbilityData, with
        // its engine-created immutable ID string. Compare that string by reference; lookup never hashes its text.
        sealed class IdReferenceComparer : IEqualityComparer<string>
        {
            public bool Equals(string left, string right) { return ReferenceEquals(left, right); }
            public int GetHashCode(string id) { return RuntimeHelpers.GetHashCode(id); }
        }

        static readonly IdReferenceComparer ids = new IdReferenceComparer();
        static readonly object writer = new object();
        static volatile Dictionary<string, WeakReference> snapshot = new Dictionary<string, WeakReference>(ids);

        internal static void Mark(AbilityData data)
        {
            lock (writer)
            {
                var next = new Dictionary<string, WeakReference>(snapshot.Count + 1, ids);
                foreach (var pair in snapshot) if (pair.Value.IsAlive) next.Add(pair.Key, pair.Value);
                next.Add(data.UniqueId, new WeakReference(data));
                snapshot = next;
            }
        }

        internal static bool IsEchoCalculation(RuleCalculateAbilityParams rule)
        {
            return rule != null && (ReferenceEquals(rule.AbilityData, null) ? EchoRegistry.InConstruction : Contains(rule.AbilityData));
        }

        internal static bool Contains(AbilityData data)
        {
            var current = snapshot;
            if (current.Count == 0 || ReferenceEquals(data, null) || data.UniqueId == null) return false;
            WeakReference found;
            return current.TryGetValue(data.UniqueId, out found) && ReferenceEquals(found.Target, data);
        }
    }

    public static class ParamsCopy
    {
        // These selections have public getters and private setters. They belong to the spell variant or its target.
        static readonly PropertyInfo[] selections =
        {
            typeof(AbilityData).GetProperty("ForceTargetOwner"),
            typeof(AbilityData).GetProperty("ParamSpellSlot"),
            typeof(AbilityData).GetProperty("ParamSpellbook"),
            typeof(AbilityData).GetProperty("ParamSpellLevel"),
            typeof(AbilityData).GetProperty("ParamUnitFact"),
        };

        public static AbilityData Capture(AbilityData original, out AbilityData parameterSource)
        {
            if (ReferenceEquals(original, null) || original.Spellbook == null) throw new InvalidOperationException("ShadowSorcerer: echo has no source spellbook");
            var book = original.Spellbook;
            // A delivered touch is an ability fact with OverrideParams from the charge. Its original spell supplies the base DC.
            var charge = original.StickyTouch;
            var selected = CleanChain(charge ?? original, book);
            parameterSource = selected;
            if (ReferenceEquals(charge, null)) return selected;
            return CleanChain(original, book, selected.SpellLevel);
        }

        static AbilityData CleanChain(AbilityData selected, Spellbook book, int? deliveryLevel = null)
        {
            var chain = new List<AbilityData>();
            for (var node = selected; !ReferenceEquals(node, null); node = node.ConvertedFrom)
            {
                foreach (var prior in chain)
                    if (ReferenceEquals(node, prior)) throw new InvalidOperationException("ShadowSorcerer: cyclic spell conversion");
                chain.Add(node);
            }
            var root = chain[chain.Count - 1];
            // Spellbook.GetSpellLevel adds MetamagicData.SpellLevelCost to this raw level; never copy AbilityData.SpellLevel.
            int level = deliveryLevel ?? root.MagicHackData?.SpellLevel ?? root.SpellLevelInSpellbook ?? book.GetMinSpellLevel(root.Blueprint);
            if (level < 0 && root.Blueprint.Parent != null) level = book.GetMinSpellLevel(root.Blueprint.Parent);
            if (level < 0) throw new InvalidOperationException("ShadowSorcerer: base spell level unavailable for " + root.Blueprint.name);
            AbilityData clean = null;
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                var source = chain[i];
                // The original selected delivery/variant is the leaf. A shadow conversion reads its immediate parent for reality.
                clean = ReferenceEquals(clean, null) ? new AbilityData(source.Blueprint, book, level) : new AbilityData(clean, source.Blueprint);
                clean.MagicHackData = source.MagicHackData?.Clone();
                clean.IsFromMythicSpellList = source.IsFromMythicSpellList;
                foreach (var selection in selections) selection.SetValue(clean, selection.GetValue(source, null), null);
                EchoAbilityIdentity.Mark(clean);
            }
            return clean;
        }

        public static void Apply(AbilityExecutionContext context, AbilityData source)
        {
            ShadowErrors.MaybeFault(ErrorLedger.SiteParamsCopy);
            if (context == null || !EchoAbilityIdentity.Contains(context.Ability) || !EchoAbilityIdentity.Contains(source))
                throw new InvalidOperationException("ShadowSorcerer: expected independent echo ability data");
            if (!ReferenceEquals(context.Ability, source))
            {
                // Sticky delivery uses the charging spell's school, level and ordinary caster bonuses on its effect and disbelief.
                using (EchoConstructionScope.Enter()) context.SetParams(source.CalculateParams());
            }
            var result = context.Params;
            if (result == null || ReferenceEquals(result, AbilityParams.Empty) || result.Metamagic != 0)
                throw new InvalidOperationException("ShadowSorcerer: base echo spell parameters missing or metamagic was reapplied");
        }
    }

    [HarmonyPatch(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.AddMetamagic))]
    static class EchoAddMetamagicPatch
    {
        static bool Prefix(RuleCalculateAbilityParams __instance) { return !EchoAbilityIdentity.IsEchoCalculation(__instance); }
    }

    [HarmonyPatch(typeof(UnitPartSpecialMetamagic), nameof(UnitPartSpecialMetamagic.GetMetamagic))]
    static class EchoSpecialMetamagicPatch
    {
        static bool Prefix(AbilityData __0, ref Metamagic __result)
        {
            if (!EchoAbilityIdentity.Contains(__0)) return true;
            __result = 0;
            return false;
        }
    }
}
