using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using static PackWarden.BlueprintUtil;

namespace PackWarden
{
    internal static class RetiredStudy
    {
        // Called on the pack owner's existing area-load event, after facts and class sources are restored.
        internal static void AfterLoad(UnitEntityData owner)
        {
            var hunter = Get<BlueprintCharacterClass>(Guids.Game.HunterClass);
            if (owner.Progression.GetClassData(hunter)?.Archetypes.Contains(Archetype.Bp) != true) return;
            Remove(owner, hunter, Guids.Game.TandemStudyTarget, 1);
            Remove(owner, hunter, Guids.Game.SlayerSwiftStudyTarget, 7);
        }

        static void Remove(UnitEntityData owner, BlueprintCharacterClass hunter, string id, int level)
        {
            var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(id);
            if (blueprint == null || !(owner.GetFact(blueprint) is Feature fact)) return;
            var source = fact.Source?.Blueprint;
            bool FromPack(BlueprintScriptableObject bp) => bp == hunter || bp == hunter.Progression || bp == Archetype.Bp;
            if (fact.SourceLevel != level || !FromPack(source) || fact.SourceFact != null || fact.SourceItem != null
                || fact.RankToSource?.Any(s => !FromPack(s.Blueprint) || s.Level != level) == true) return;
            // A rank-one native feature can be shared without recording a second rank.
            foreach (var data in owner.Progression.Classes)
            {
                bool Grants(LevelEntry row) => row.Level <= data.Level && row.Features.Contains(blueprint);
                if (data.Archetypes.Any(a => a != Archetype.Bp && a.AddFeatures.Any(Grants))) return;
                if (data.CharacterClass != hunter && data.CharacterClass.Progression?.LevelEntries.Any(Grants) == true) return;
            }
            owner.RemoveFact(fact);
        }
    }
}
