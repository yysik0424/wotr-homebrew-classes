using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    // 값 칸 = ChimeraRules.Dials 이름(Dial) + 그 값(Value). 엔진 이름(기술자·능력치·조건·주문 기술자·전투 기동)은 문자열 — 빌더가 Enum.Parse(없으면 던짐).
    public enum EffectKind { StatBonus, RankedStatBonus, CmdManeuvers, Mechanics, Facts, Blindsense, DescriptorImmunity, ConditionImmunity, AreaImmunity, FastHealing, BreathSurge }

    public sealed class EffectRow
    {
        public readonly EffectKind Kind;
        public readonly string Descriptor, Stat, Dial;   // StatBonus/RankedStatBonus/CmdManeuvers: 기술자·능력치(CmdManeuvers 는 Stat = 기동 목록 "Trip,BullRush")
        public readonly int Value;                       // Dial 의 값(표가 아닌 행)
        public readonly int[] ReplaceTable, AddTable;    // RankedStatBonus 강화: Replace 모양 = 한 단계 위 값 20행, Add 모양 = 차이값 20행
        public readonly string[] Refs;                   // Facts/AreaImmunity: 블루프린트 GUID, Mechanics/DescriptorImmunity/ConditionImmunity: 엔진 이름
        public EffectRow(EffectKind kind, string descriptor, string stat, string dial, int value, int[] replaceTable, int[] addTable, string[] refs)
        { Kind = kind; Descriptor = descriptor; Stat = stat; Dial = dial; Value = value; ReplaceTable = replaceTable; AddTable = addTable; Refs = refs ?? new string[0]; }
        public override string ToString() { return Kind + "(" + Descriptor + "/" + Stat + "/" + Dial + "=" + Value + (Refs.Length > 0 ? "/" + string.Join("+", Refs) : "") + ")"; }
    }

    public sealed class BodyRow
    {
        public readonly string Id, BoostBuffGuid;
        public readonly IReadOnlyList<EffectRow> Base, Boost;
        public BodyRow(string id, string boostBuff, IReadOnlyList<EffectRow> b, IReadOnlyList<EffectRow> boost) { Id = id; BoostBuffGuid = boostBuff; Base = b; Boost = boost; }
    }

    public static class BodyPlan
    {
        public const string Untyped = "UntypedStackable";
        static int Dial(string name)
        {
            var f = typeof(ChimeraRules.Dials).GetField(name);
            if (f == null || f.FieldType != typeof(int)) throw new ArgumentException("ChimeraMonk: no int dial ChimeraRules.Dials." + name, "name");
            return (int)f.GetValue(null);
        }

        static EffectRow Stat(string descriptor, string stat, string dial) { return new EffectRow(EffectKind.StatBonus, descriptor, stat, dial, Dial(dial), null, null, null); }
        static EffectRow Named(EffectKind k, params string[] refs) { return new EffectRow(k, null, null, null, 0, null, null, refs); }

        static int[] Table(Func<int, int> f) { return Enumerable.Range(ChimeraRules.Levels.MinLevel, ChimeraRules.Levels.MaxLevel).Select(f).ToArray(); }

        public static IReadOnlyList<BodyRow> Rows()
        {
            // 비늘(설계 §7·§11, 부록 A-d): 기술자 NaturalArmorForm(목걸이·바크스킨 = NaturalArmorEnhancement, 변이약 = NaturalArmor 와 다름).
            var scalesBase = new EffectRow(EffectKind.RankedStatBonus, "NaturalArmorForm", "AC", "ScalesBase", ChimeraRules.Dials.ScalesBase, Table(l => ChimeraRules.ScalesValue(l, false)), null, null);
            var scalesBoost = new EffectRow(EffectKind.RankedStatBonus, "NaturalArmorForm", "AC", "ScalesMutagenBonus", ChimeraRules.Dials.ScalesMutagenBonus,
                Table(l => ChimeraRules.ScalesValue(l, true)), Table(l => ChimeraRules.ScalesValue(l, true) - ChimeraRules.ScalesValue(l, false)), null);
            return new[]
            {
                new BodyRow("Scales", Guids.Boost.Scales, new[] { scalesBase }, new[] { scalesBoost }),
                new BodyRow("ExtraLegs", Guids.Boost.ExtraLegs,
                    new[] { Stat(Untyped, "Speed", "LegsSpeedFeet"), new EffectRow(EffectKind.CmdManeuvers, Untyped, "Trip,BullRush", "LegsManeuverDefense", Dial("LegsManeuverDefense"), null, null, null) },
                    new[] { Stat(Untyped, "Speed", "LegsMutagenSpeedFeet") }),
                new BodyRow("ExtraEyes", Guids.Boost.ExtraEyes,
                    new[] { Named(EffectKind.Mechanics, "CannotBeFlanked"), Stat(Untyped, "SkillPerception", "EyesPerception") },
                    new[] { Stat(Untyped, "SkillPerception", "EyesMutagenPerception") }),
                new BodyRow("ExtraEars", Guids.Boost.ExtraEars,
                    new[] { Named(EffectKind.Facts, Guids.Game.BlindFight), Stat(Untyped, "SkillPerception", "EarsPerception") },
                    new[] { Stat(Untyped, "SkillPerception", "EarsMutagenPerception") }), // 판정 D5
                new BodyRow("ExtraSenses", Guids.Boost.ExtraSenses,
                    new[] { new EffectRow(EffectKind.Blindsense, null, null, "SensesFeet", Dial("SensesFeet"), null, null, null) },
                    new[] { new EffectRow(EffectKind.Blindsense, null, null, "SensesMutagenFeet", Dial("SensesMutagenFeet"), null, null, null) }),
                new BodyRow("ExtraHeart", Guids.Boost.ExtraHeart,
                    new[] { Stat(Untyped, "HitPoints", "HeartHp"), Named(EffectKind.DescriptorImmunity, "Bleed") },
                    new[] { new EffectRow(EffectKind.FastHealing, null, null, "HeartMutagenFastHealing", Dial("HeartMutagenFastHealing"), null, null, null) }),
                new BodyRow("ExtraLung", null,
                    new[] { Named(EffectKind.ConditionImmunity, "Fatigued"), Named(EffectKind.DescriptorImmunity, "Fatigue"), Named(EffectKind.AreaImmunity, LungPlan.CloudSpells.Select(c => c.Item3).ToArray()) },
                    new[] { new EffectRow(EffectKind.BreathSurge, null, null, "LungSurgeBonus", Dial("LungSurgeBonus"), null, null, null) }),
                new BodyRow("Wings", Guids.Boost.Wings,
                    new[] { Named(EffectKind.Facts, Guids.Game.BuffWingsMutagen) },                        // 판정 D2
                    new[] { Stat(Untyped, "Speed", "WingsMutagenSpeedFeet") }),
            };
        }
    }

    // [순수] 판정 D1: 추가 폐 = 구름 주문 목록 면역(독 전체 면역 아님). (이름, 주문, 그 주문이 소환하는 영역) — 면역은 영역에 건다(독립 검토 C-1:
    //  AddSpellImmunity{Specific} 는 영역 진입 경로 TryOvercomeSpellResistance → IsImmune(ctx, attackOnly) 가 보지 않아 실제로 막지 못했다).
    public static class LungPlan
    {
        public static readonly IReadOnlyList<Tuple<string, string, string>> CloudSpells = new[]
        {
            Tuple.Create("StinkingCloud", Guids.Game.StinkingCloud, Guids.Game.StinkingCloudArea),
            Tuple.Create("Cloudkill", Guids.Game.Cloudkill, Guids.Game.CloudkillArea),
            Tuple.Create("MindFog", Guids.Game.MindFog, Guids.Game.MindFogArea),
            Tuple.Create("AcidFog", Guids.Game.AcidFog, Guids.Game.AcidFogArea),
        };
    }
}
