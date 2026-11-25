using System.Linq;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Newtonsoft.Json;

namespace ProphecyBreaker
{
    // ApplyClassMechanics adds each level's BAB delta to the serialized base value.
    public sealed class LegacyBaseAttack : UnitFactComponentDelegate<LegacyBaseAttack.State>, IAreaHandler, IGlobalSubscriber
    {
        public sealed class State
        {
            [JsonProperty] public bool Reviewed;
        }

        protected override void OnTurnOn()
        {
            // New characters receive Research Training at oracle level one, where both tables give zero.
            if (Owner.Progression.GetClassLevel(Blueprints.Oracle) == 1) Data.Reviewed = true;
        }

        public void OnAreaBeginUnloading() { }

        public void OnAreaDidLoad()
        {
            if (Data.Reviewed) return;
            var classes = Owner.Progression;
            var oracle = classes.GetClassData(Blueprints.Oracle);
            if (oracle == null || !oracle.Archetypes.Contains(Blueprints.Archetype)) return;
            var stat = Owner.Stats.GetStat(StatType.BaseAttackBonus);
            int updated = UpdatedBase(classes, oracle, stat.BaseValue);
            if (updated != stat.BaseValue) stat.BaseValue = updated;
            Data.Reviewed = true;
        }

        internal static int UpdatedBase(UnitProgressionData progression, ClassData oracle, int current)
        {
            int delta = Blueprints.Oracle.BaseAttackBonus.GetBonus(oracle.Level) - oracle.BaseAttackBonus.GetBonus(oracle.Level);
            if (delta == 0) return current;
            var actualClasses = progression.Classes.Where(c => !c.CharacterClass.IsMythic).ToArray();
            if (actualClasses.Any(c => c.BaseAttackBonus == null)) return current;
            int updated = actualClasses.Sum(c => c.BaseAttackBonus.GetBonus(c.Level));
            // Preserve a custom base that cannot be identified as the old class total.
            return current == updated + delta ? updated : current;
        }
    }
}
