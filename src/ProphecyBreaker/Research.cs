using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

namespace ProphecyBreaker
{
    internal static class Study
    {
        internal static bool Own(BlueprintSpellbook bp) => bp != null && ReferenceEquals(bp,Blueprints.Research);
        internal static int Level(Spellbook b) => b.Owner.Progression.GetClassLevel(Blueprints.Oracle);
        internal static bool Open(Spellbook b,int tier) => tier >= 1 && tier <= Rules.MaxTier(Level(b));
        internal static BlueprintAbility[] Entries(Spellbook b,int tier) => b.GetKnownSpells(tier)
            .Where(s => !s.IsTemporary).Select(s => s.Blueprint).Distinct().ToArray();

        internal static bool CanInscribe(Spellbook b,int tier,BlueprintAbility spell,BlueprintAbility old)
        {
            if (b == null || !Own(b.Blueprint) || !Open(b,tier) || spell == null ||
                !b.Blueprint.SpellList.SpellsByLevel[tier].Spells.Contains(spell) || b.IsKnownOnLevel(spell,tier)) return false;
            var entries = Entries(b,tier);
            return old == null ? entries.Length < Rules.Capacity : entries.Length == Rules.Capacity && entries.Contains(old);
        }

        // The permission belongs to exactly one synchronous AddKnown call. Reentrant callbacks
        // cannot use it for another spell, book or tier.
        [ThreadStatic] static Spellbook permittedBook;
        [ThreadStatic] static BlueprintAbility permittedSpell;
        [ThreadStatic] static int permittedTier;
        internal static bool MayAdd(Spellbook book,int tier,BlueprintAbility spell)
        {
            if (ReferenceEquals(book,permittedBook) && spell == permittedSpell && tier == permittedTier) {
                permittedBook = null; return true;
            }
            return Open(book,tier) && (book.IsKnownOnLevel(spell,tier) || Entries(book,tier).Length < Rules.Capacity);
        }

        internal static AbilityData Inscribe(Spellbook book,int tier,BlueprintAbility spell,BlueprintAbility old)
        {
            if (!CanInscribe(book,tier,spell,old)) return null;
            AbilityData learned;
            permittedBook = book; permittedTier = tier; permittedSpell = spell;
            try { learned = book.AddKnown(tier,spell); }
            finally { permittedBook = null; permittedSpell = null; }
            if (learned != null && old != null) Erase(book,tier,old);
            return learned;
        }

        static readonly Action<Spellbook,BlueprintAbility,int> removeLevel =
            AccessTools.MethodDelegate<Action<Spellbook,BlueprintAbility,int>>(AccessTools.Method(typeof(Spellbook),"RemoveKnownSpellLevel"));
        internal static void Erase(Spellbook book,int tier,BlueprintAbility old)
        {
            bool otherTier = Enumerable.Range(0,Rules.HighestTier+1).Any(i => i != tier && book.IsKnownOnLevel(old,i));
            bool Affected(AbilityData s,int slotTier) => s != null && s.Blueprint == old &&
                (!otherTier || slotTier - (s.MetamagicData?.SpellLevelCost ?? 0) == tier);
            for (int i=0;i<=Rules.HighestTier;i++) {
                foreach (var slot in book.GetMemorizedSpellSlots(i).Concat(book.GetHiddenCommonSpells(i)))
                    if (Affected(slot.SpellShell,i)) slot.Clear();
                foreach (var custom in book.GetCustomSpells(i).Where(s => Affected(s,i)).ToArray()) book.RemoveCustomSpell(custom);
            }
            book.GetKnownSpells(tier).RemoveAll(s => s.Blueprint == old && !s.IsTemporary);
            if (!book.GetKnownSpells(tier).Any(s => s.Blueprint == old)) removeLevel(book,old,tier);
        }
    }

    public sealed class ResearchParameters : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateAbilityParams>
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            var b = evt.AbilityData?.Spellbook ?? evt.Spellbook;
            if (!Study.Own(b?.Blueprint)) return;
            int level = Study.Level(b);
            evt.ReplaceCasterLevel = level;
            evt.ReplaceDC = Rules.SaveDC(level,b.Owner.Stats.Intelligence.Bonus);
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) {}
    }

    [HarmonyPatch(typeof(Spellbook),"GetSpellsPerDay")]
    internal static class DailyPool
    {
        static bool Prefix(Spellbook __instance,int __0,ref int __result)
        {
            return FixedSlots(__instance,__0,ref __result);
        }
        internal static bool FixedSlots(Spellbook book,int tier,ref int result)
        {
            if (!Study.Own(book.Blueprint)) return true;
            result = Study.Open(book,tier) ? Rules.Slots : 0;
            return false;
        }
    }

    [HarmonyPatch(typeof(Spellbook),"GetSpellSlotsCount")]
    internal static class PreparationSlots
    {
        static bool Prefix(Spellbook __instance,int __0,ref int __result) =>
            DailyPool.FixedSlots(__instance,__0,ref __result);
    }

    [HarmonyPatch(typeof(Spellbook),"GetMaxSpellLevel")]
    internal static class MaxResearchTier
    {
        static bool Prefix(Spellbook __instance,ref int __result)
        {
            if (!Study.Own(__instance.Blueprint)) return true;
            __result = Rules.MaxTier(Study.Level(__instance)); return false;
        }
    }

    [HarmonyPatch(typeof(Spellbook),"AddKnown")]
    internal static class InscriptionLimit
    {
        static bool Prefix(Spellbook __instance,int __0,BlueprintAbility __1) =>
            !Study.Own(__instance.Blueprint) || Study.MayAdd(__instance,__0,__1);
    }

    [HarmonyPatch(typeof(ApplySpellbook),"Apply")]
    internal static class OfferResearch
    {
        static void Postfix(LevelUpState __0,UnitDescriptor __1)
        {
            if (Blueprints.Archetype == null || __0.SelectedClass != Blueprints.Oracle ||
                __1.Progression.GetClassData(Blueprints.Oracle)?.Archetypes.Contains(Blueprints.Archetype) != true) return;
            var book = __1.DemandSpellbook(Blueprints.Research);
            int level = Study.Level(book);
            while (book.BaseLevel < level) {
                int previous = book.BaseLevel;
                book.AddBaseLevel();
                if (book.BaseLevel <= previous) throw new InvalidOperationException("Research book level did not advance");
            }
            __0.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList).SetExtraSpells(book.Blueprint.SpellsPerLevel, Rules.MaxTier(level));
        }
    }
}
