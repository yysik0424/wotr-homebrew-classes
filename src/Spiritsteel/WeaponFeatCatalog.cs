using System;
using System.Collections.Generic;
using System.Linq;

namespace Spiritsteel
{
    public enum WeaponFeatKind
    {
        Acid, Fire, Cold, Electricity, Sonic, Ghost, Agile, Keen, Dodge, Focus, Specialization, Critical, Trip, Mobility, Fortitude, Reflex, Will, Initiative, Precise, Shield
    }

    // Level thresholds and scope are from the approved 2026-11-24 weapon-feats design.
    // Icon sources are vanilla feats or spells from the local blueprints.zip.
    internal sealed class WeaponFeatRow
    {
        internal readonly WeaponFeatKind Kind;
        internal readonly int MinLevel;
        internal readonly string Korean, English, KoreanEffect, EnglishEffect, IconSource;

        private WeaponFeatRow(WeaponFeatKind kind, int minLevel, string ko, string en,
            string koEffect, string enEffect, string icon)
        {
            Kind = kind; MinLevel = minLevel; Korean = ko; English = en;
            KoreanEffect = koEffect; EnglishEffect = enEffect; IconSource = icon;
        }

        internal static readonly WeaponFeatRow[] All =
        {
            new WeaponFeatRow(WeaponFeatKind.Acid, 2, "부식", "Corrosive",
                "산성", "acid", "0c852a2405dd9f14a8bbcfaf245ff823"),
            new WeaponFeatRow(WeaponFeatKind.Fire, 2, "화염", "Flaming",
                "화염", "fire", "4783c3709a74a794dbe7c8e7e0b1b038"),
            new WeaponFeatRow(WeaponFeatKind.Cold, 2, "냉기", "Frost",
                "냉기", "cold", "9af2ab69df6538f4793b2f9c3cc85603"),
            new WeaponFeatRow(WeaponFeatKind.Electricity, 2, "전격", "Shock",
                "전기", "electricity", "16e23c7a8ae53cc42a93066d19766404"),
            new WeaponFeatRow(WeaponFeatKind.Sonic, 8, "뇌명", "Thundering",
                "음파", "sonic", "c3893092a333b93499fd0a21845aa265"),
            new WeaponFeatRow(WeaponFeatKind.Ghost, 4, "유령 접촉", "Ghost Touch",
                "결속 무기로 비실체 대상을 공격할 때 유령 접촉 효과를 받습니다.", "The bound weapon can strike incorporeal targets as a ghost touch weapon.", "f4201c85a991369408740c6888362e20"),
            new WeaponFeatRow(WeaponFeatKind.Agile, 4, "기민", "Agile",
                "무기 기교를 배운 전사는 기교에 맞는 결속 근접 무기의 피해에 더 높은 민첩 수정치를 쓸 수 있습니다.", "With Weapon Finesse, use a higher Dexterity modifier for damage with a finesse-compatible melee bound weapon.", "90e54424d682d104ab36436bd527af09"),
            new WeaponFeatRow(WeaponFeatKind.Keen, 8, "예리", "Keen",
                "결속 무기의 기본 치명타 위협 범위가 두 배가 됩니다. 향상된 치명타와 함께 한 번만 적용됩니다.", "Double the bound weapon’s base critical threat range. This overlaps with Improved Critical.", "f4201c85a991369408740c6888362e20"),
            new WeaponFeatRow(WeaponFeatKind.Dodge, 2, "피하기", "Dodge",
                "결속 무기를 든 동안 방어도에 +2 회피 보너스를 받습니다.", "Gain a +2 dodge bonus to AC while wielding the bound weapon.", "97e216dbb46ae3c4faef90cf6bbe6fd5"),
            new WeaponFeatRow(WeaponFeatKind.Focus, 2, "무기 집중", "Weapon Focus",
                "결속 무기 명중 굴림에 +1을 받습니다. 해당 범주의 무기 집중과는 한 번만 적용됩니다.", "Gain +1 on attack rolls with the bound weapon. This overlaps with Weapon Focus for its category.", "1e1f627d26ad36f43bbd26cc2bf8ac7e"),
            new WeaponFeatRow(WeaponFeatKind.Specialization, 4, "무기 전문화", "Weapon Specialization",
                "결속 무기의 물리 피해가 바바리안 4·8·14·20레벨에 +2·+3·+4·+5 증가합니다. 무기 또는 전사의 해당 범주 무기 집중이 필요합니다. 일반 무기 전문화와는 높은 값을 적용합니다.", "Add +2/+3/+4/+5 to bound weapon damage at barbarian levels 4/8/14/20. Requires bound Weapon Focus or Weapon Focus for this category. Use the higher bonus with ordinary Weapon Specialization.", "31470b17e8446ae4ea0dacd6c5817d86"),
            new WeaponFeatRow(WeaponFeatKind.Critical, 10, "치명타 집중", "Critical Focus",
                "결속 무기의 치명타 확정 굴림에 +4를 받습니다. 전사의 치명타 집중과는 한 번만 적용됩니다.", "Gain +4 on confirmation rolls with the bound weapon. This overlaps with Critical Focus.", "8ac59959b1b23c347a0361dc97cc786d"),
            new WeaponFeatRow(WeaponFeatKind.Trip, 6, "걸어넘기기", "Trip",
                "결속 무기로 시도하는 넘어뜨리기에 +2를 받습니다. 신령의 분노 고유 넘어뜨리기에도 적용됩니다.", "Gain +2 on trip checks with the bound weapon, including Spirit Rage’s trip.", "0f15c6f70d8fb2b49aa6cc24239cc5fa"),
            new WeaponFeatRow(WeaponFeatKind.Mobility, 4, "전투 기동", "Combat Mobility",
                "결속 무기를 든 동안 이동으로 유발된 기회공격에 대해 방어도 +4를 받습니다. 무기 또는 전사의 피하기가 필요합니다.", "While wielding the bound weapon, gain +4 AC against attacks of opportunity provoked by movement. Requires Dodge from the weapon or the wielder.", "2a6091b97ad940943b46262600eaeaeb"),
            new WeaponFeatRow(WeaponFeatKind.Fortitude, 2, "강인한 체질", "Great Fortitude",
                "결속 무기를 든 동안 인내 내성에 +2를 받습니다. 전사의 동일 피트와는 한 번만 적용됩니다.", "Gain +2 on Fortitude saves while wielding the bound weapon. This overlaps with Great Fortitude.", "79042cb55f030614ea29956177977c52"),
            new WeaponFeatRow(WeaponFeatKind.Reflex, 2, "번개 반사", "Lightning Reflexes",
                "결속 무기를 든 동안 반사 내성에 +2를 받습니다. 전사의 동일 피트와는 한 번만 적용됩니다.", "Gain +2 on Reflex saves while wielding the bound weapon. This overlaps with Lightning Reflexes.", "15e7da6645a7f3d41bdad7c8c4b9de1e"),
            new WeaponFeatRow(WeaponFeatKind.Will, 2, "강철 의지", "Iron Will",
                "결속 무기를 든 동안 의지 내성에 +2를 받습니다. 전사의 동일 피트와는 한 번만 적용됩니다.", "Gain +2 on Will saves while wielding the bound weapon. This overlaps with Iron Will.", "175d1577bb6c9a04baf88eec99c66334"),
            new WeaponFeatRow(WeaponFeatKind.Initiative, 4, "선제 대응", "Improved Initiative",
                "결속 무기를 들고 우선권을 굴릴 때 +4를 받습니다. 전사의 선제 대응과는 한 번만 적용됩니다.", "Gain +4 on initiative rolls while wielding the bound weapon. This overlaps with Improved Initiative.", "797f25d709f559546b29e7bcb181cc74"),
            new WeaponFeatRow(WeaponFeatKind.Precise, 2, "정밀 사격", "Precise Shot",
                "근접 교전 중인 적을 쏠 때 결속 무기의 명중 페널티를 없앱니다. 원거리·투척 무기와 근거리 사격이 필요합니다.", "Remove the shooting-into-melee penalty with the bound weapon. Requires a ranged or thrown weapon and Point-Blank Shot.", "8f3d1e6b4be006f4d896081f2f889665"),
            new WeaponFeatRow(WeaponFeatKind.Shield, 2, "방패 집중", "Shield Focus",
                "방패 가시를 결속했을 때 그 방패가 주는 방패 보너스가 1 증가합니다. 전사의 방패 집중과는 한 번만 적용됩니다.", "Increase the shield bonus from the shield bearing the bound spikes by 1. This overlaps with Shield Focus.", "ac57069b6bf8c904086171683992a92a"),
        };

        internal bool Element { get { return Kind <= WeaponFeatKind.Sonic; } }
        internal bool OnBothHeads
        {
            get
            {
                return Element || Kind == WeaponFeatKind.Ghost || Kind == WeaponFeatKind.Agile ||
                    Kind == WeaponFeatKind.Keen || Kind == WeaponFeatKind.Focus || Kind == WeaponFeatKind.Specialization ||
                    Kind == WeaponFeatKind.Critical || Kind == WeaponFeatKind.Precise;
            }
        }

        internal const int MaxElements = 2;
        internal const int DamageTiers = 4;
        internal static int AdditionalBonus(int weapon, int character) { return Math.Max(0, weapon - character); }
        internal static bool CanAddElement(int knownOthers) { return knownOthers < MaxElements; }
        internal int NextLevel(int level)
        {
            if (level < MinLevel) return MinLevel;
            if (level < 8) return 8;
            if (level < 14) return 14;
            return level < 20 ? 20 : 0;
        }

        internal string ValueAt(int level)
        {
            int tier = DamageTier(level);
            if (level < MinLevel) return "0";
            return Element ? tier + "d6" : "+" + (tier + 1);
        }
        internal static int DamageTier(int level)
        {
            if (level < 2) return 0;
            if (level < 8) return 1;
            if (level < 14) return 2;
            return level < 20 ? 3 : 4;
        }

        internal string Key(string suffix) { return "weaponfeat." + Kind + "." + suffix; }

        internal static void AddStrings(Dictionary<string, (string ko, string en)> table)
        {
            table.Add("weaponfeat.selection.name", ("무기 피트", "Weapon Feat"));
            table.Add("weaponfeat.selection.desc", ("짝수 스피릿스틸 레벨마다 결속 무기에 영구 특성 하나를 붙입니다. 원소는 두 종류까지 익히며 함께 발동합니다.", "At each even Spiritsteel level, teach your bound weapon one permanent trait. Up to two elements can be learned and both apply on a hit."));
            table.Add("weaponfeat.element.limit", ("배운 원소가 두 종류 미만", "Fewer than two elements learned"));
            table.Add("weaponfeat.next", ("현재 {0} · 바바리안 {1}레벨에 {2}", "Current {0} · At barbarian level {1}: {2}"));
            table.Add("weaponfeat.final", ("현재 {0} (최종 단계)", "Current {0} (maximum)"));
            table.Add("weaponfeat.Agile.requirement", ("무기 기교 및 기교 가능한 결속 근접 무기", "Weapon Finesse and a finesse-compatible melee bound weapon"));
            table.Add("weaponfeat.Mobility.requirement", ("무기 또는 캐릭터의 피하기", "Dodge on the bound weapon or the character"));
            table.Add("weaponfeat.Specialization.requirement", ("무기 집중 (무기 피트 또는 결속 범주의 캐릭터 피트)", "Weapon Focus on the weapon or for its category"));
            table.Add("weaponfeat.Precise.requirement", ("근거리 사격 및 결속 원거리·투척 무기", "Point-Blank Shot and a ranged or thrown bound weapon"));
            table.Add("weaponfeat.Shield.requirement", ("결속 방패 가시", "Bound shield spikes"));
            foreach (var row in All)
            {
                string ko = row.Element ? "결속 무기 명중 시 " + row.KoreanEffect + " 피해를 추가합니다. 바바리안 2·8·14·20레벨에 각각 1d6·2d6·3d6·4d6입니다. 치명타 시 한 번 적용됩니다." : row.KoreanEffect;
                string en = row.Element ? "On a bound-weapon hit, deal extra " + row.EnglishEffect + " damage: 1d6/2d6/3d6/4d6 at barbarian levels 2/8/14/20. The dice apply once on a critical hit." : row.EnglishEffect;
                table.Add(row.Key("name"), (row.Korean, row.English));
                table.Add(row.Key("desc"), (ko, en));
                if (row.Element || row.Kind == WeaponFeatKind.Specialization)
                    for (int tier = 1; tier <= DamageTiers; tier++)
                    {
                        string value = row.Element ? tier + "d6" : "+" + (tier + 1);
                        table.Add(row.Key("tier" + tier), (row.Korean + " " + value, row.English + " " + value));
                    }
            }
        }
    }
}
