using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace HomebrewClasses.Loader
{
    internal static class PreviewDamage
    {
        static readonly AccessTools.FieldRef<ContextRankConfig, ContextRankBaseValueType> Base =
            AccessTools.FieldRefAccess<ContextRankConfig, ContextRankBaseValueType>("m_BaseValueType");
        static readonly AccessTools.FieldRef<ContextRankConfig, bool> UseMin = AccessTools.FieldRefAccess<ContextRankConfig, bool>("m_UseMin");
        static readonly AccessTools.FieldRef<ContextRankConfig, bool> UseMax = AccessTools.FieldRefAccess<ContextRankConfig, bool>("m_UseMax");
        static readonly AccessTools.FieldRef<ContextRankConfig, int> Min = AccessTools.FieldRefAccess<ContextRankConfig, int>("m_Min");
        static readonly AccessTools.FieldRef<ContextRankConfig, int> Max = AccessTools.FieldRefAccess<ContextRankConfig, int>("m_Max");
        static readonly AccessTools.FieldRef<ContextRankConfig, bool> Intensified = AccessTools.FieldRefAccess<ContextRankConfig, bool>("m_AffectedByIntensifiedMetamagic");
        static readonly Func<ContextRankConfig, int, int> Progress = (Func<ContextRankConfig, int, int>)
            Delegate.CreateDelegate(typeof(Func<ContextRankConfig, int, int>), AccessTools.Method(typeof(ContextRankConfig), "ApplyProgression"));

        static int? Value(ContextValue value, BlueprintAbility bp, AbilityData data, Metamagic meta)
        {
            if (PreviewDuration.Literal(value, out int literal)) return literal;
            if (value.ValueType != ContextValueType.Rank || data == null) return null;
            var cfg = bp.GetComponents<ContextRankConfig>().LastOrDefault(c => c.Type == value.ValueRank);
            if (cfg == null || Base(cfg) != ContextRankBaseValueType.CasterLevel) return null;
            int? cl = data.OverrideCasterLevel;
            if (!cl.HasValue && data.Caster != null) cl = data.Spellbook?.CasterLevel;
            if (!cl.HasValue || cl <= 0) return null;
            int result = Progress(cfg, cl.Value);
            if (UseMin(cfg)) result = Math.Max(result, Min(cfg));
            if (UseMax(cfg)) result = Math.Min(result, Max(cfg) + (Intensified(cfg) && (meta & Metamagic.Intensified) != 0 ? 5 : 0));
            return result;
        }

        internal static string Describe(ContextActionDealDamage action, BlueprintAbility bp, AbilityData data, Metamagic meta, bool ko)
        {
            if (!ActionChecks.SimpleDamage(action)) return null;
            string unknown = ko ? "피해량은 시전 시 결정" : "damage determined on cast";
            if (action.ReadPreRolledFromSharedValue || action.Value == null) return unknown;
            var dice = action.Value;
            int sides = (int)dice.DiceType;
            int? count = sides == 0 ? 0 : Value(dice.DiceCountValue, bp, data, meta);
            int? bonus = Value(dice.BonusValue, bp, data, meta);
            if (!count.HasValue || !bonus.HasValue || count < 0 || sides < 0) return unknown;
            long lo = (long)count.Value + bonus.Value, hi = (long)count.Value * sides + bonus.Value;
            string range = lo == hi ? lo.ToString() : lo + "–" + hi;
            string formula = count > 0 && sides > 1 ? " (" + count + "d" + sides + bonus.Value.ToString("+0;-0;#") + ")" : "";
            string text = (ko ? "1회 기본 피해 " : "base damage per instance ") + range + formula;
            if (action.Half) text += ko ? " · 절반 적용 전" : " · before halving";
            if (action.HalfIfSaved) text += ko ? " · 내성 성공 시 절반" : " · save for half";
            if ((meta & (Metamagic.Maximize | Metamagic.Empower | Metamagic.Bolstered)) != 0)
                text += ko ? " · 피해 메타매직 적용 전" : " · before damage metamagic";
            return text;
        }
    }
}
