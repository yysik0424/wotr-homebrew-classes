using System;
using System.Collections.Generic;

namespace Warlock
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
        // Main.Load 가 Strings.Configure(Table, Guids.Game.WitchClass) 로 넘긴다(빈 표는 Configure 가 거부한다). 내용 출처 = docs\2026-11-07-warlock-design.md §1·§4·§5·§12(§13-1).
        // T1 = 아키타입 이름·설명. T3 = 키트(그룹 특성 6·블라스트 18·가호 24·마커 10·소환 부모 6·계약 1)의 이름·설명 — 키는 KitSpec.AllKeys() 가 원장, 문구는 아래 그룹 표에서 만든다.
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();

        // 그룹 이름·속성 이름(설계 §4.3 표 「그룹」「속성」 열).
        static (string ko, string en) GroupName(Group g)
        {
            switch (g)
            {
                case Group.Hell: return ("지옥", "Hell");
                case Group.Abyss: return ("심연", "Abyss");
                case Group.Fey: return ("요정", "Fey");
                case Group.Storm: return ("폭풍", "Storm");
                case Group.Death: return ("죽음", "Death");
                case Group.Star: return ("별", "Star");
                default: throw new ArgumentOutOfRangeException("g", g, "Warlock: unknown group");
            }
        }

        static (string ko, string en) ElementName(BlastElement e)
        {
            switch (e)
            {
                case BlastElement.Fire: return ("화염", "fire");
                case BlastElement.Acid: return ("산성", "acid");
                case BlastElement.Cold: return ("냉기", "cold");
                case BlastElement.Electricity: return ("전기", "electricity");
                case BlastElement.NegativeEnergy: return ("음에너지", "negative energy");
                case BlastElement.Force: return ("역장", "force");
                default: throw new ArgumentOutOfRangeException("e", e, "Warlock: unknown element");
            }
        }

        // 그룹마다 주적(데몬) 저항 한 줄(설계 §4.4 「폭풍 그룹 설명문」 + 「다른 그룹 설명문에도 주적 저항 한 줄」). 데몬 사실 = SubtypeDemon AddFacts(jbp 2026-11-14):
        // ElectricityImmunity · AcidResistance10 · ColdResistance10 · FireResistance10 — 음에너지·역장 저항은 없다.
        static (string ko, string en) EnemyLine(Group g)
        {
            switch (g)
            {
                case Group.Hell: return ("데몬은 화염 저항 10을 가진다 — 피해가 깎여도 명중하면 과열은 오른다.", "Demons resist fire 10 — even when the damage is reduced, a hit still raises Heat.");
                case Group.Abyss: return ("데몬은 산성 저항 10을 가진다 — 피해가 깎여도 명중하면 과열은 오른다.", "Demons resist acid 10 — even when the damage is reduced, a hit still raises Heat.");
                case Group.Fey: return ("데몬은 냉기 저항 10을 가진다 — 피해가 깎여도 명중하면 과열은 오른다.", "Demons resist cold 10 — even when the damage is reduced, a hit still raises Heat.");
                case Group.Storm: return ("데몬은 전기 면역 — 폭풍 워락의 블라스트는 데몬에게 피해를 주지 못한다(피해 0). 단 명중하면 과열은 오른다.", "Demons are immune to electricity — a Storm warlock's blast deals them no damage (0). A hit still raises Heat.");
                case Group.Death: return ("데몬은 음에너지 저항이 없다.", "Demons have no negative energy resistance.");
                case Group.Star: return ("역장 피해는 에너지 저항으로 깎이지 않는다(데몬도 같다).", "Force damage is not reduced by energy resistance (demons included).");
                default: throw new ArgumentOutOfRangeException("g", g, "Warlock: unknown group");
            }
        }

        static void Add(Dictionary<string, (string ko, string en)> t, string key, (string ko, string en) name, (string ko, string en) desc)
        {
            t.Add(key + ".Name", name);
            t.Add(key + ".Description", desc);
        }

        static Dictionary<string, (string ko, string en)> Build()
        {
            var t = new Dictionary<string, (string ko, string en)>
            {
                ["Archetype.Name"] = ("워락", "Warlock"),
                 
                ["Archetype.DescriptionShort"] = ("후견인에 의해 개화된 위치입니다. 후견인의 특성에 따른 원소 블라스트를 쏘며, 명중할수록 과열이 쌓이고, 과열을 태워 후견인의 하수인을 소환합니다. 그들은 위치보다 좀더 후견인의 명령에 좌우되는 행동을 합니다.",
                    "A witch awakened by her patron. She fires elemental blasts shaped by her patron's nature; each hit builds Heat, and burning Heat summons her patron's servants — who answer more to the patron's commands than to the witch."),
                ["Archetype.Description"] = (
                    "워락은 후견인과 맺은 계약으로 주술 대신 블라스트를 쏘는 위치다. 후견인에 따라 여섯 그룹(지옥·심연·요정·폭풍·죽음·별) 중 하나가 정해지고, 그 속성의 블라스트(1d6, 홀수 레벨마다 +1d6, 매력 기반)를 무제한으로 쏜다. 블라스트가 명중할 때마다 과열이 1 오르고(최대 9, 전투가 끝나면 절반(내림)이 남고 휴식하면 0), 과열을 써서 그룹의 생물을 부르며, 과열이 9가 되면 부를 수 있는 가장 높은 단계의 생물이 저절로 나온다. 주술을 모두 잃고, 주문은 스티그마 위치처럼 매력으로 자유 시전한다. 본편 프리스티지 직업으로는 워락 주문서를 진행할 수 없다.",
                    "The Warlock is a witch whose pact with her patron replaces hexes with blasts. The patron decides one of six groups (Hell, Abyss, Fey, Storm, Death, Star), and you fire that group's blast at will (1d6, +1d6 every odd level, Charisma-based). Each blast hit raises Heat by 1 (up to 9; when combat ends half of it remains, rounded down, and a rest resets it to 0); Heat pays for summoning the group's creatures, and at 9 Heat the highest creature you can summon appears on its own. You lose all hexes and cast spontaneously with Charisma like a Stigmatized Witch. The base game's prestige classes cannot advance the warlock's spellbook."),
            };

            // ---- T3 키트 (설계 §4.3~4.9·§5) ----
            Add(t, KitSpec.PactKey, ("워락 계약", "Warlock Pact"),
                ("후견인에 따라 워락 그룹을 정하고, 그 그룹의 블라스트·가호·과열 소환을 준다. 레벨이 오르면 그룹의 능력을 레벨에 맞게 바꾼다.", "Your patron decides your warlock group; the pact grants that group's blast, ward and Heat summoning and keeps them matched to your level."));
            foreach (var g in PatronGroups.AllGroups)
            {
                var n = GroupName(g); var el = ElementName(PatronGroups.ElementOf(g)); var enemy = EnemyLine(g);
                Add(t, KitSpec.GroupKey(g), ("워락 그룹: " + n.ko, "Warlock Group: " + n.en),
                    ("후견인이 정한 워락 그룹. 블라스트 속성: " + el.ko + ". " + enemy.ko, "The warlock group your patron decided. Blast element: " + el.en + ". " + enemy.en));
                Add(t, KitSpec.ParentKey(g), (n.ko + " 과열 소환", n.en + " Heat Summoning"),
                    ("과열을 써서 " + n.ko + " 그룹의 생물을 부른다(단계 = 쓰는 과열, 최고 주문 레벨 이하).", "Spend Heat to summon a creature of the " + n.en + " group (tier = Heat spent, up to your highest spell level)."));
            }
            foreach (var r in KitSpec.Rays())
            {
                var n = GroupName(r.Group); var el = ElementName(r.Element); var enemy = EnemyLine(r.Group);
                string dmgKo = "1d6 + 2레벨마다 1d6 " + el.ko + " 피해 + 매력 수정치 ÷ 2", dmgEn = "1d6 + 1d6 per two levels " + el.en + " damage + Charisma modifier / 2";
                string rangeKo = r.Range == BlastRange.Close ? "근거리" : "중거리", rangeEn = r.Range == BlastRange.Close ? "close" : "medium";
                if (r.Kind == BlastKind.Double)
                    Add(t, r.Key, (n.ko + " 연속 사격", n.en + " Double Blast"),
                        ("표준 행동 한 번에 같은 대상에게 광선 두 줄기(" + rangeKo + "). 줄기마다 명중 −2, 명중하면 " + dmgKo + ". 주문 저항이 적용된다. 명중할 때마다 과열 +1. " + enemy.ko,
                         "Two rays at the same target in one standard action (" + rangeEn + " range). Each ray takes −2 to hit; on a hit it deals " + dmgEn + ". Spell resistance applies. Each hit raises Heat by 1. " + enemy.en));
                else
                    Add(t, r.Key, (n.ko + " 블라스트" + (r.Range == BlastRange.Medium ? " (중거리)" : ""), n.en + " Blast" + (r.Range == BlastRange.Medium ? " (Medium)" : "")),
                        ("광선 원거리 접촉 공격(" + rangeKo + ", 명중은 매력). 명중하면 " + dmgKo + ". 주문 저항이 적용된다. 명중할 때마다 과열 +1. " + enemy.ko,
                         "Ranged touch ray (" + rangeEn + " range, Charisma to hit). On a hit it deals " + dmgEn + ". Spell resistance applies. Each hit raises Heat by 1. " + enemy.en));
            }
            foreach (var w in KitSpec.AllWards())
            {
                var n = GroupName(w.Group); var el = ElementName(w.Element);
                (string ko, string en) d;
                switch (w.Kind)
                {
                    case WardKind.Resist: d = (el.ko + " 저항 " + w.Value + ".", el.en + " resistance " + w.Value + "."); break;
                    case WardKind.Immune: d = (el.ko + " 면역.", "Immunity to " + el.en + "."); break;
                    case WardKind.Deflection: d = ("편향 AC +" + w.Value + ".", "+" + w.Value + " deflection bonus to AC."); break;
                    case WardKind.DeflectionAndSr: d = ("편향 AC +" + w.Value + ", 주문 저항 " + w.SrStepLevel + " + 위치 레벨.", "+" + w.Value + " deflection bonus to AC and spell resistance " + w.SrStepLevel + " + witch level."); break;
                    default: throw new ArgumentOutOfRangeException("w", w.Kind, "Warlock: unknown ward kind");
                }
                Add(t, w.Key, (n.ko + " 가호 " + w.Tier, n.en + " Ward " + w.Tier), d);
            }
            Add(t, "MarkerWard1", ("그룹 가호 1단", "Group Ward I"), ("그룹 가호 1단계를 연다.", "Unlocks the first tier of your group ward."));
            Add(t, "MarkerWard2", ("그룹 가호 2단", "Group Ward II"), ("그룹 가호가 2단계로 오른다.", "Your group ward rises to the second tier."));
            Add(t, "MarkerWard3", ("그룹 가호 3단", "Group Ward III"), ("그룹 가호가 3단계로 오른다.", "Your group ward rises to the third tier."));
            Add(t, "MarkerWard4", ("그룹 가호 4단", "Group Ward IV"), ("그룹 가호가 4단계(속성 면역, 별 그룹은 주문 저항)로 오른다.", "Your group ward rises to the fourth tier (element immunity; spell resistance for the Star group)."));
            Add(t, "MarkerAugment", ("워락 소환 강화", "Warlock Augment Summoning"), ("워락 소환물이 소환 강화(힘·건강 +4)를 받는다.", "Your summoned creatures gain Augment Summoning (+4 Strength and Constitution)."));
            Add(t, "MarkerReach", ("블라스트 사거리", "Blast Reach"), ("블라스트·연속 사격의 사거리가 중거리가 된다.", "Your blast and double blast reach medium range."));
            Add(t, "MarkerDouble", ("연속 사격", "Double Blast"), ("연속 사격(광선 두 줄기)을 쓸 수 있다.", "You can use Double Blast (two rays)."));
            Add(t, "MarkerHeatAccel", ("과열 가속", "Heat Acceleration"), ("블라스트 치명타 확정 명중은 과열 +2.", "A confirmed critical blast hit raises Heat by 2."));
            Add(t, "MarkerExtend", ("소환 지속 2배", "Extended Summons"), ("과열 소환물의 지속 시간이 두 배가 된다.", "Your Heat summons last twice as long."));
            Add(t, "MarkerCapstone", ("대사자", "Grand Summoner"), ("9단계 과열 소환물은 전투가 끝날 때까지 남는다.", "Your tier IX Heat summons remain until combat ends."));
            // 과열 랭크 버프(설계 §4.7): 아이콘 숫자 = 랭크. 0 = 버프 없음.
            Add(t, "Heat", ("과열", "Heat"), ("블라스트가 명중할 때마다 1씩 오른다(최대 9). 전투가 끝나면 절반(내림)이 남고, 휴식을 마치면 0 이 된다. 남은 과열은 전투 밖에서도 쓸 수 있다. 과열을 써서 후견인 그룹의 생물을 부른다.",
                "Rises by 1 each time a blast hits (up to 9). When combat ends, half of it remains (rounded down); after a full rest it drops to 0. Heat that remains can also be spent outside combat. Spend Heat to summon a creature of your patron's group."));
            // ---- T5 과열 소환 변형 54 · 대사자 표식 · 제한 문구 (설계 §4.6·§4.9) ----
            string[] roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
            foreach (var g in PatronGroups.AllGroups)
                for (int tier = 1; tier <= WarlockRules.Dials.SummonTiers; tier++)
                {
                    var n = GroupName(g);
                    Add(t, KitSpec.VariantKey(g, tier), (n.ko + " 과열 소환 " + roman[tier - 1], n.en + " Heat Summoning " + roman[tier - 1]),
                        ("과열 " + tier + " 을 써서 " + n.ko + " 그룹의 " + roman[tier - 1] + "단계 생물 1마리를 부른다(신속 행동, 최고 주문 레벨 이하). 이미 부른 워락 소환물은 사라진다. 지속 = 위치 레벨 라운드.",
                         "Spend " + tier + " Heat to summon one tier " + roman[tier - 1] + " creature of the " + n.en + " group (swift action, up to your highest spell level). Your previous warlock summon vanishes. Lasts 1 round per witch level."));
                }
            Add(t, KitSpec.GrandSummonerKey, ("대사자의 소환물", "Grand Summoner's Summon"), ("전투가 끝날 때까지 남는다.", "Remains until combat ends."));
             
            Add(t, KitSpec.ServantKey, ("후견인의 하수인", "Patron's Servant"),
                ("후견인의 명령을 따른다: 직접 조종할 수 없고, 자기 판단(AI)으로 아군 편에서 싸운다.", "Answers to the patron: it cannot be directly controlled and fights on your side on its own (AI)."));
             
            Add(t, KitSpec.IxBoostKey, ("후견인의 총애", "Patron's Favor"),
                ("이 9단계 소환물은 소환 강화를 한 번 더 받는다: 힘·건강 +" + WarlockRules.Dials.IxAugmentBonus + "(다른 보너스와 쌓인다). 언데드처럼 건강 능력치가 없으면 건강 대신 매력 +" + WarlockRules.Dials.IxAugmentBonus + "(언데드의 체력은 매력으로 계산된다).",
                 "This tier IX summon gains Augment Summoning a second time: +" + WarlockRules.Dials.IxAugmentBonus + " Strength and Constitution (stacks with other bonuses). A summon without a Constitution score, such as an undead, gains +" + WarlockRules.Dials.IxAugmentBonus + " Charisma instead of Constitution (undead hit points use Charisma)."));
            t.Add("Summon.Restriction", ("과열이 단계보다 적거나, 단계가 최고 주문 레벨보다 높다.", "Not enough Heat, or the tier is above your highest spell level."));
            return t;
        }
    }
}
