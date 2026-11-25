using System;
using System.Collections.Generic;
using System.Linq;

namespace Spiritsteel
{
    public enum WeaponFeatKind
    {
        Acid, Fire, Cold, Electricity, Sonic, Ghost, Agile, Keen, Dodge, Focus, Specialization, Critical, Trip, Mobility, Fortitude, Reflex, Will, Initiative, Precise, Shield,
        ColdIron, Adamantine, Watchful, Silent, Charging, Opportunist, UndeadHunter, ConstructHunter, DragonHunter, DemonHunter,
        GreaterFocus, Anchored, GiantSlayer, Fortified, GreaterSpecialization, Evasive, Judgment, SpellWard, Sundering, Unfettered
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
            new WeaponFeatRow(WeaponFeatKind.ColdIron, 2, "냉철 각인", "Cold Iron Inscription",
                "결속 무기의 물리 타격을 냉철로 취급해 해당 피해 감소를 돌파합니다.", "Physical hits with the bound weapon count as cold iron for overcoming damage reduction.", "779179912e6c6fe458fa4cfb90d96e10"),
            new WeaponFeatRow(WeaponFeatKind.Adamantine, 6, "아다만틴 각인", "Adamantine Inscription",
                "결속 무기의 물리 타격을 아다만틴으로 취급해 해당 피해 감소를 돌파합니다.", "Physical hits with the bound weapon count as adamantine for overcoming damage reduction.", "9719015edcbf142409592e2cbaab7fe1"),
            new WeaponFeatRow(WeaponFeatKind.Watchful, 2, "신령의 눈", "Watchful Spirit",
                "결속 무기를 든 동안 지각 판정에 +3 역량 보너스를 받습니다.", "Gain a +3 competence bonus on Perception checks while wielding the bound weapon.", "f74c6bdf5c5f5374fb9302ecdc1f7d64"),
            new WeaponFeatRow(WeaponFeatKind.Silent, 2, "고요한 발걸음", "Quiet Step",
                "결속 무기를 든 동안 은신 판정에 +3 역량 보너스를 받습니다.", "Gain a +3 competence bonus on Stealth checks while wielding the bound weapon.", "3a8d34905eae4a74892aae37df3352b9"),
            new WeaponFeatRow(WeaponFeatKind.Charging, 4, "돌격의 선봉", "Vanguard Charge",
                "결속 무기로 돌격 공격을 할 때 명중에 +2 통찰 보너스를 받습니다.", "Gain a +2 insight bonus on charge attack rolls with the bound weapon.", "c78506dd0e14f7c45a599990e4e65038"),
            new WeaponFeatRow(WeaponFeatKind.Opportunist, 6, "빈틈 찌르기", "Opening Strike",
                "결속 무기로 기회공격을 할 때 명중에 +2 통찰 보너스를 받습니다.", "Gain a +2 insight bonus on attacks of opportunity with the bound weapon.", "0f8939ae6f220984e8fb568abbdfba95"),
            new WeaponFeatRow(WeaponFeatKind.UndeadHunter, 4, "언데드 사냥꾼", "Undead Hunter",
                "결속 무기로 언데드를 공격할 때 명중에 +2 통찰 보너스를 받습니다.", "Gain a +2 insight bonus on attack rolls with the bound weapon against undead.", "6ab3056345b78164fa34b2c947b52083"),
            new WeaponFeatRow(WeaponFeatKind.ConstructHunter, 4, "구조물 사냥꾼", "Construct Hunter",
                "결속 무기로 구조물을 공격할 때 명중에 +2 통찰 보너스를 받습니다.", "Gain a +2 insight bonus on attack rolls with the bound weapon against constructs.", "9719015edcbf142409592e2cbaab7fe1"),
            new WeaponFeatRow(WeaponFeatKind.DragonHunter, 6, "용 사냥꾼", "Dragon Hunter",
                "결속 무기로 드래곤 유형의 적을 공격할 때 명중에 +2 통찰 보너스를 받습니다.", "Gain a +2 insight bonus on attack rolls with the bound weapon against dragons.", "f4201c85a991369408740c6888362e20"),
            new WeaponFeatRow(WeaponFeatKind.DemonHunter, 8, "데몬 사냥꾼", "Demon Hunter",
                "결속 무기로 데몬 하위 유형의 적을 공격할 때 명중에 +2 통찰 보너스를 받습니다.", "Gain a +2 insight bonus on attack rolls with the bound weapon against creatures with the demon subtype.", "831e942864e924846a30d2e0678e438b"),
            new WeaponFeatRow(WeaponFeatKind.GreaterFocus, 12, "상급 무기 집중", "Greater Weapon Focus",
                "결속 무기 명중에 +1을 더합니다. 무기나 전사가 해당 범주의 무기 집중을 익혀야 합니다. 전사의 같은 범주 상급 무기 집중과 겹치면 한 번만 적용합니다.", "Gain an additional +1 on attack rolls with the bound weapon. Requires Weapon Focus from the inscription or for its category. Overlaps with Greater Weapon Focus for that category.", "09c9e82965fb4334b984a1e9df3bd088"),
            new WeaponFeatRow(WeaponFeatKind.Anchored, 12, "뿌리내린 자세", "Rooted Stance",
                "결속 무기를 든 동안 전법 방어도(CMD)에 +4 역량 보너스를 받습니다.", "Gain a +4 competence bonus to combat maneuver defense (CMD) while wielding the bound weapon.", "b3614622866fe7046b787a548bbd7f59"),
            new WeaponFeatRow(WeaponFeatKind.GiantSlayer, 14, "거체 사냥꾼", "Giant-Killer",
                "결속 무기로 크기가 대형 이상인 적을 공격할 때 명중에 +2 통찰 보너스를 받습니다.", "Gain a +2 insight bonus on bound-weapon attack rolls against Large or larger targets.", "c60969e7f264e6d4b84a1499fdcf9039"),
            new WeaponFeatRow(WeaponFeatKind.Fortified, 14, "신령의 요새", "Spirit Fortification",
                "결속 무기를 든 동안 본편의 25% 요새화를 얻어 치명타와 암습을 일반 명중으로 바꿀 수 있습니다.", "Gain the standard 25% fortification against critical hits and sneak attacks while wielding the bound weapon.", "165b74db9493ded40ac05b3521aab427"),
            new WeaponFeatRow(WeaponFeatKind.GreaterSpecialization, 16, "상급 무기 전문화", "Greater Weapon Specialization",
                "결속 무기의 물리 피해에 +2를 더합니다. 무기나 전사가 해당 범주의 무기 전문화를 익혀야 합니다. 전사의 같은 범주 상급 무기 전문화와 겹치면 한 번만 적용합니다.", "Gain an additional +2 to physical damage with the bound weapon. Requires Weapon Specialization from the inscription or for its category. Overlaps with Greater Weapon Specialization for that category.", "7cf5edc65e785a24f9cf93af987d66b3"),
            new WeaponFeatRow(WeaponFeatKind.Evasive, 16, "신령의 회피", "Spirit Evasion",
                "결속 무기를 든 동안 반사 내성 성공 시 피해가 절반이 되는 효과에 내성을 성공하면 피해를 받지 않습니다.", "While wielding the bound weapon, take no damage on a successful Reflex save against an effect that normally deals half damage on a success.", "576933720c440aa4d8d42b0c54b77e80"),
            new WeaponFeatRow(WeaponFeatKind.Judgment, 18, "심판의 일격", "Judgment Strike",
                "결속 무기로 악 성향의 적을 명중시키면 신성 피해 2d6을 추가합니다. 치명타에도 한 번만 더합니다.", "Bound-weapon hits against evil targets deal an extra 2d6 holy damage, rolled once even on a critical hit.", "ad5ed5ea4ec52334a94e975a64dad336"),
            new WeaponFeatRow(WeaponFeatKind.SpellWard, 18, "주문 방벽", "Spell Ward",
                "결속 무기를 든 동안 주문 저항 32를 얻습니다. 주문 저항이 여럿이면 가장 높은 값을 적용합니다.", "Gain spell resistance 32 while wielding the bound weapon. Use the highest applicable spell resistance.", "985d6dbff8c143e8b1ce5b96161b1ee0"),
            new WeaponFeatRow(WeaponFeatKind.Sundering, 20, "방벽 관통", "Pierce Defenses",
                "결속 무기의 물리 타격은 적의 피해 감소를 최대 10점 무시합니다. 본편 관통 타격 계열과 겹치면 높은 값만 적용합니다.", "Physical hits with the bound weapon ignore up to 10 points of damage reduction. Use the higher value when combined with the Penetrating Strike feats.", "eb6eb946c68ef094f89c7633f5bfdc9b"),
            new WeaponFeatRow(WeaponFeatKind.Unfettered, 20, "속박 해방", "Unfettered Spirit",
                "결속 무기를 든 동안 마비, 얽힘, 감속 및 험지의 영향을 받지 않습니다.", "While wielding the bound weapon, gain immunity to paralysis, entanglement, slow, and difficult terrain.", "4c349361d720e844e846ad8c19959b1e"),
        };

        internal bool Element { get { return Kind <= WeaponFeatKind.Sonic; } }
        internal bool OnBothHeads
        {
            get
            {
                return Element || Kind == WeaponFeatKind.Ghost || Kind == WeaponFeatKind.Agile ||
                    Kind == WeaponFeatKind.Keen || Kind == WeaponFeatKind.Focus || Kind == WeaponFeatKind.Specialization ||
                    Kind == WeaponFeatKind.Critical || Kind == WeaponFeatKind.Precise ||
                    Kind == WeaponFeatKind.ColdIron || Kind == WeaponFeatKind.Adamantine ||
                    Kind == WeaponFeatKind.Charging || Kind == WeaponFeatKind.Opportunist ||
                    Kind == WeaponFeatKind.UndeadHunter || Kind == WeaponFeatKind.ConstructHunter ||
                    Kind == WeaponFeatKind.DragonHunter || Kind == WeaponFeatKind.DemonHunter ||
                    Kind == WeaponFeatKind.GreaterFocus || Kind == WeaponFeatKind.GreaterSpecialization ||
                    Kind == WeaponFeatKind.GiantSlayer || Kind == WeaponFeatKind.Judgment || Kind == WeaponFeatKind.Sundering;
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
            table.Add("weaponfeat.GreaterSpecialization.requirement", ("무기 전문화 (무기 피트 또는 결속 범주의 캐릭터 피트)", "Weapon Specialization on the weapon or for its category"));
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
