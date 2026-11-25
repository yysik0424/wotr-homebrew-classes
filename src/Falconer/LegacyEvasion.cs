using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.PubSubSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using static WotrClassMod.Common.BlueprintUtil;

namespace Falconer
{
    // Threefold Hunt is the permanent Falconer feature granted at the former exchange level.
    public sealed class LegacyEvasion : UnitFactComponentDelegate, IAreaHandler, IGlobalSubscriber
    {
        public void OnAreaBeginUnloading() { }

        public void OnAreaDidLoad()
        {
            var improved = Get<BlueprintFeature>(Rules.ImprovedEvasion);
            if (Owner.GetFact(improved) is Feature fact && ShouldRemove(Owner, fact))
                Owner.RemoveFact(fact);
        }

        internal static bool ShouldRemove(UnitEntityData owner, Feature fact)
        {
            var improved = fact.Blueprint;
            var progression = owner.Progression;
            if (progression.GetClassData(Blueprints.Ranger)?.Archetypes.Contains(Blueprints.Archetype) != true
                || !HasOldSource(fact)) return false;
            // Rank-one features can have another grant without an additional rank entry.
            // PostLoad has rebuilt the effective entries, including archetype additions and removals.
            return !(progression.GetAllProgressions.Any(p => p.Value.LevelEntries.Any(row =>
                    row.Level <= p.Value.Level && row.Features.Contains(improved)))
                || progression.Selections.Values.Any(s => s.SelectionsByLevel.Values.Any(picks => picks.Contains(improved)))
                || owner.Facts.List.Any(f => f.Blueprint is BlueprintScriptableObject bp
                    && bp.GetComponents<AddFacts>().Any(grant => grant.Facts.Contains(improved))));
        }

        internal static bool HasOldSource(Feature fact)
        {
            bool FromRanger(BlueprintScriptableObject bp) =>
                bp == Blueprints.Ranger || bp == Blueprints.Ranger.Progression || bp == Blueprints.Archetype;
            return fact.SourceLevel == Rules.ImprovedEvasionLevel && FromRanger(fact.Source?.Blueprint)
                && fact.SourceFact == null && fact.SourceItem == null
                && fact.RankToSource?.Any(s => !FromRanger(s.Blueprint) || s.Level != Rules.ImprovedEvasionLevel) != true;
        }
    }
}
