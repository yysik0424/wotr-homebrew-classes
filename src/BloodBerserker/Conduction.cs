using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;
using WotrClassMod.Common;
using ModContext = WotrClassMod.Common.ModContext;

namespace BloodBerserker
{
    public class ChargeSpell : AbilityApplyEffect, IAbilityCasterRestriction
    {
        public bool Vampiric;
        // The engine's impact step runs the original target's immunities, spell resistance and effects.
        // Using this step lets the chain's actual hit replace the touch delivery without another cast or slot.
        internal delegate void ApplyImpact(AbilityExecutionContext context, TargetWrapper target, AbilityApplyEffect effect);
        internal static ApplyImpact Impact;

        internal static void BindImpact()
        {
            var method = typeof(AbilityExecutionProcess).GetMethod("DoApplyEffect", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(AbilityExecutionContext), typeof(TargetWrapper), typeof(AbilityApplyEffect) }, null);
            if (method == null) throw new MissingMethodException(typeof(AbilityExecutionProcess).FullName, "DoApplyEffect");
            Impact = (ApplyImpact)Delegate.CreateDelegate(typeof(ApplyImpact), method);
        }

        static AbilityData Known(UnitEntityData u, bool vamp)
        {
            var book = u.Descriptor.GetSpellbook(Blueprints.Bloodrager);
            var spell = vamp ? Blueprints.Vamp : Blueprints.Shock;
            if (book == null || !book.IsKnown(spell)) return null;
            return book.GetKnownSpells(spell).FirstOrDefault(a => a.Blueprint == spell &&
                (a.MetamagicData == null || a.MetamagicData.MetamagicMask == 0));
        }

        public bool IsCasterRestrictionPassed(UnitEntityData u)
        {
            var known = Known(u, Vampiric);
            return known != null && known.IsAvailableForCast && known.Spellbook.GetAvailableForCastSpellCount(known) > 0 && !u.Buffs.HasFact(Blueprints.VampBuff) && !u.Buffs.HasFact(Blueprints.ShockBuff);
        }

        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "needsSlot"); }

        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var u = context.Caster;
            if (!BloodrageRequired.Active(u) || !IsCasterRestrictionPassed(u)) return;
            var spell = Known(u, Vampiric);
            if (!spell.Spellbook.Spend(spell)) return;
            u.Buffs.AddBuff(Vampiric ? Blueprints.VampBuff : Blueprints.ShockBuff, u, null);
        }

        internal static void Discharge(UnitEntityData u, TargetWrapper target)
        {
            var charge = u.Buffs.GetBuff(Blueprints.VampBuff) ?? u.Buffs.GetBuff(Blueprints.ShockBuff);
            if (charge == null) return;
            var original = Known(u, charge.Blueprint == Blueprints.VampBuff);
            charge.Remove();
            if (original == null) { ModContext.LogError("Blood Berserker lost the loaded spellbook entry."); return; }
            var delivery = original.Blueprint.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility;
            var spell = new AbilityData(original, delivery);
            if (!spell.CanTarget(target)) return;
            var context = spell.CreateExecutionContext(target, original.CalculateParams());
            // PrepareCast recalculates the delivery blueprint ranks before impact (including each spell's damage cap).
            context.Recalculate();
            using (context.GetDataScope(target)) Impact(context, target, delivery.GetComponent<AbilityEffectRunAction>());
        }
    }
}
