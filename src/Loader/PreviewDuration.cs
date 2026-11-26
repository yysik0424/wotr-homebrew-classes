using System;
using System.Globalization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;
using System.Linq;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace HomebrewClasses.Loader
{
    internal static class PreviewDuration
    {
        static string Number(double n) => n.ToString("0.##", CultureInfo.CurrentCulture);

        internal static string Clock(TimeSpan time, bool ko)
        {
            long sec = (long)Math.Ceiling(Math.Max(0, time.TotalSeconds));
            if (sec >= 86400) return sec / 86400 + (ko ? "일 " : "d ") + sec % 86400 / 3600 + (ko ? "시간" : "h");
            if (sec >= 3600) return sec / 3600 + (ko ? "시간 " : "h ") + sec % 3600 / 60 + (ko ? "분" : "m");
            if (sec >= 60) return sec / 60 + (ko ? "분 " : "m ") + sec % 60 + (ko ? "초" : "s");
            return sec + (ko ? "초" : "s");
        }

        static string Unlimited(BlueprintBuff buff, bool ko)
        {
            if (buff?.RemoveOnRest == true) return ko ? "휴식까지" : "until rest";
            if (buff?.ShouldRemoveWhenCombatEnded == true) return ko ? "전투 종료까지" : "until combat ends";
            return ko ? "시간 제한 없음" : "no time limit";
        }

        internal static string Remaining(Buff buff, bool ko)
        {
            var value = buff.IsPermanent ? Unlimited(buff.Blueprint, ko) : Clock(buff.TimeLeft, ko);
            return (ko ? "현재 " : "active: ") + (buff.IsPermanent ? value : (ko ? "남은 " : "remaining ") + value)
                + (buff.IsSuppressed ? (ko ? " (억제됨)" : " (suppressed)") : "");
        }

        internal static bool Literal(ContextValue v, out int n)
        {
            n = v?.Value ?? 0;
            return v == null || v.ValueType == ContextValueType.Simple;
        }

        internal static string Formula(ContextDurationValue d, Metamagic meta, bool ko)
        {
            if (d == null) return null;
            string unit;
            int scale = 1;
            switch (d.Rate)
            {
                case DurationRate.Rounds: unit = ko ? "라운드" : " rounds"; break;
                case DurationRate.Minutes: unit = ko ? "분" : " min"; break;
                case DurationRate.TenMinutes: unit = ko ? "분" : " min"; scale = 10; break;
                case DurationRate.Hours: unit = ko ? "시간" : " h"; break;
                case DurationRate.Days: unit = ko ? "일" : " days"; break;
                default: return null;
            }
            if ((meta & Metamagic.Extend) != 0 && d.IsExtendable) scale *= 2;
            int sides = (int)d.DiceType;
            int count = 0;
            if (!Literal(d.BonusValue, out int bonus) || (sides > 0 && !Literal(d.DiceCountValue, out count))) return null;
            long min = (long)scale * (bonus + (sides == 0 ? 0L : count));
            long max = (long)scale * (bonus + (long)count * sides);
            if (sides > 1)
            {
                if ((meta & Metamagic.Maximize) != 0) min = max;
                if ((meta & Metamagic.Empower) != 0)
                {
                    min += (min / scale / 2) * scale;
                    max += (max / scale / 2) * scale;
                }
            }
            return (min == max ? min.ToString() : min + "–" + max) + unit;
        }

        // AbilityExecutionContext initializes every rank from effective caster level. Rank configs override it.
        static readonly System.Reflection.FieldInfo RankBase = AccessTools.Field(typeof(ContextRankConfig), "m_BaseValueType");
        static readonly System.Reflection.FieldInfo Progression = AccessTools.Field(typeof(ContextRankConfig), "m_Progression");
        static readonly System.Reflection.FieldInfo UseMin = AccessTools.Field(typeof(ContextRankConfig), "m_UseMin");
        static readonly System.Reflection.FieldInfo UseMax = AccessTools.Field(typeof(ContextRankConfig), "m_UseMax");
        internal static string PerLevel(ContextDurationValue d, BlueprintAbility bp, AbilityData data, Metamagic meta, bool ko)
        {
            if (d?.BonusValue?.ValueType != ContextValueType.Rank ||
                ((int)d.DiceType != 0 && (!Literal(d.DiceCountValue, out int count) || count != 0))) return null;
            var config = bp.GetComponents<ContextRankConfig>().LastOrDefault(c => c.Type == d.BonusValue.ValueRank);
            if (config != null && ((ContextRankBaseValueType)RankBase.GetValue(config) != ContextRankBaseValueType.CasterLevel ||
                (ContextRankProgression)Progression.GetValue(config) != ContextRankProgression.AsIs ||
                (bool)UseMin.GetValue(config) || (bool)UseMax.GetValue(config))) return null;
            var one = new ContextDurationValue { Rate = d.Rate };
            // Extend applies to the source action, regardless of the temporary scale used to format one unit.
            bool doubled = (meta & Metamagic.Extend) != 0 && d.IsExtendable;
            one.BonusValue = doubled ? 2 : 1;
            string unit = Formula(one, 0, ko);
            if (unit == null) return null;
            var formula = ko ? "시전자 레벨당 " + unit : unit + "/caster level";
            int? cl = data?.OverrideCasterLevel;
            if (!cl.HasValue && data?.Caster != null) cl = data.Spellbook?.CasterLevel;
            if (cl.GetValueOrDefault() <= 0) return formula;
            one.BonusValue = cl.Value * (doubled ? 2 : 1);
            return formula + (ko ? " (기본 " : " (base ") + Formula(one, 0, ko) + ")";
        }

        internal static string Variable(ContextDurationValue d, Metamagic meta, bool ko)
        {
            var baseText = ko ? "시전 시 산정" : "determined on cast";
            return (meta & Metamagic.Extend) != 0 && d?.IsExtendable == true ? baseText + (ko ? " (연장 ×2)" : " (Extend ×2)") : baseText;
        }

        internal static string Application(ContextActionApplyBuff a, BlueprintBuff buff, Metamagic meta, bool ko, string ranked)
        {
            string value;
            if (a.Permanent) value = Unlimited(buff, ko);
            else if (a.SameDuration) value = ko ? "상위 효과의 남은 시간 (없으면 시간 제한 없음)" : "remaining parent duration (unlimited without parent)";
            else if (a.UseDurationSeconds) value = Number(a.DurationSeconds) + (ko ? "초" : "s");
            else value = Formula(a.DurationValue, meta, ko) ?? ranked ?? Variable(a.DurationValue, meta, ko);
            if (a.AsChild && a.Permanent && !a.SameDuration)
                value += ko ? " (상위 효과가 있으면 함께 종료)" : " (ends with parent if linked)";
            return value;
        }
    }
}
