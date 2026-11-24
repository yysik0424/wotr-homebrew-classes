using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 붙인다). Main.Load 가 Strings.Configure(Table, Guids.Game.PaladinClass) 로 넘긴다.
        // 문구 출처 = 설계 docs\2026-11-14-oathbound-revenant-design.md §5·§6·§8. 각 기능의 Task 가 자기 키를 더한다.
        // 선택 화면 설명문은 리터럴이 아니다 — 아래 ChoiceText 가 규칙 상수(RevenantRules · TalentPlan · LevelPlan · AuraPlan · FearPlan)에서 만들어 넣는다(C4: 문구 속 수치 = 규칙 상수):
        //   "archetype.desc"(직업 선택 화면의 아키타입 설명) · "talentselection.desc"(죽음 기사의 재주 화면) · "talent.<id>.choice.desc" 20개(그 화면의 선택지 = 재주 특성).
        //   "talent.<id>.desc"(짧은 한 줄)는 그 재주의 능력·자원·버프 툴팁용이다 — 선택지용 긴 설명이 툴팁까지 길게 만들지 않게 키를 갈랐다(2026-11-17).
        //   능력·자원·버프가 없는 재주 7종(망령의 걸음·무덤의 갑주·영혼 수확·강철 뼈·죽음의 칼날·강화 채널·죽음의 낙인)은 짧은 키를 쓰는 곳이 없어져 지웠다(시험 `strings: choice text` 가 「짧은 키 == 소비자가 있는 재주」를 잠근다).
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();

        // 죽음 기사의 재주 선택 화면(레벨 표 껍데기 TalentSelection — Archetype.MakeShell 이 "talentselection" + ".desc" 로 조립)의 설명 키.
        public const string TalentSelectionDescKey = "talentselection.desc";

        // 선택지(재주 특성) 설명 키. TalentPlan 에 없는 재주면 던진다.
        public static string TalentChoiceKey(string talentId)
        {
            if (talentId == null) throw new ArgumentNullException("talentId");
            if (TalentPlan.Rows.Count(r => r.Id == talentId) != 1) throw new KeyNotFoundException("OathboundRevenant: no single talent '" + talentId + "' in TalentPlan (choice text key)");
            return "talent." + talentId.ToLowerInvariant() + ".choice.desc";
        }

        static Dictionary<string, (string ko, string en)> Build()
        {
            var d = Literals();
            ChoiceText.AddTo(d);
            return d;
        }

        static Dictionary<string, (string ko, string en)> Literals()
        {
            return new Dictionary<string, (string ko, string en)>
        {
            ["archetype.name"] = ("맹세의 망자", "Oathbound Revenant"),
            // 아키타입의 짧은 설명(LocalizedDescriptionShort). 긴 설명 "archetype.desc" 는 ChoiceText 가 만든다.
            ["archetype.short"] = (
                "맹세를 지키려 죽음에서 돌아온 팔라딘. 1레벨부터 언데드이며, 스마이트 대상을 칠 때마다 맹세가 몸을 되살린다. 안수는 아군에게만, 4레벨부터 양에너지 대신 음에너지를 방출한다. 주문을 시전하지 않는다.",
                "A paladin returned from death to keep an oath. Undead from 1st level; striking the target of a smite renews the body. Lays hands on allies only, channels negative energy from 4th level, and casts no spells."),
            ["undead.name"] = ("언데드 몸", "Undead Body"),
            ["undead.desc"] = (
                "언데드 형식을 얻는다: 언데드 면역, 음에너지에 회복되고 양에너지에 피해를 입으며, 체력 대신 매력이 생명력을 정한다.",
                "Gains the undead type: undead immunities, healed by negative energy and harmed by positive energy, and uses Charisma instead of Constitution for hit points."),
            // T3: 레벨 표 추가 특성(키 = LevelPlan 추가 키 소문자 — Archetype.MakeShell 이 조립) · 재주 선택 · 재주 20(키 = "talent." + Id 소문자).
            ["oathregeneration.name"] = ("맹세의 재생", "Oath Regeneration"),
            ["oathregeneration.desc"] = ("스마이트 대상을 무기로 맞히면 팔라딘 레벨만큼 회복한다(자기 라운드당 1회).", "When you hit the target of your smite with a weapon, you heal hit points equal to your paladin level (once per your round)."),
            ["smitenegative.name"] = ("스마이트 음에너지", "Smite: Negative Energy"),
            ["smitenegative.desc"] = ("스마이트 대상을 무기로 맞히면 1d6 음에너지 피해를 더한다(7·13·19레벨에 +1d6, 최대 4d6, 치명타에 곱하지 않음). 대상이 음에너지 친화면 같은 주사위가 신성 피해가 된다.", "Weapon hits against your smite target deal +1d6 negative energy (+1d6 at 7th, 13th and 19th, max 4d6, not multiplied on a critical). Against creatures with negative energy affinity the same dice are divine damage."),
            ["revenanthands.name"] = ("망자의 손", "Revenant's Hands"),
            ["revenanthands.desc"] = ("안수를 아군에게만 쓸 수 있다(자기 자신·적에게는 쓸 수 없다).", "You can lay on hands on allies only (not on yourself or enemies)."),
            ["graveyarddread.name"] = ("묘지의 공포", "Graveyard Dread"),
            ["graveyarddread.desc"] = ("스마이트 대상은 스마이트가 이어지는 동안 흔들린다.", "The target of your smite is shaken for as long as the smite lasts."),
            ["deathaura.name"] = ("죽음의 오라", "Aura of Death"),
            ["deathaura.desc"] = ("10피트 안의 적은 공포 내성에 −4, 받는 치유가 절반이 된다.", "Enemies within 10 feet take −4 on saves against fear and receive only half healing."),
            ["negativechannel.name"] = ("음에너지 방출", "Channel Negative Energy"),
            ["negativechannel.desc"] = ("안수 2회를 써서 30피트 안의 살아 있는 적에게 팔라딘 레벨 절반 d6 음에너지 피해(의지 반감)를 주고, 한 명이라도 맞으면 굴린 값만큼 회복한다.", "Spend two uses of lay on hands: living enemies within 30 feet take 1d6 negative energy per two paladin levels (Will half); if at least one is hit, you heal the rolled amount."),
            ["revenanthandsmaximize.name"] = ("망자의 손 극대화", "Revenant's Hands Maximized"),
            ["revenanthandsmaximize.desc"] = ("홀리 챔피언: 망자의 손 안수가 극대화된다.", "Holy Champion: your Revenant's Hands lay on hands is maximized."),
            ["talentselection.name"] = ("죽음 기사의 재주", "Death Knight Talent"),
            ["talent.pursuitoath.name"] = ("추적의 맹세", "Oath of Pursuit"),
            ["talent.pursuitoath.desc"] = ("스마이트 대상의 이동 속도가 절반이 된다.", "The target of your smite moves at half speed."),
            ["talent.terriblegaze.name"] = ("공포의 응시", "Terrible Gaze"),
            ["talent.terriblegaze.desc"] = ("하루 3회, 적 하나를 겁에 질리게 한다(의지 내성, DC 10 + 팔라딘 레벨 절반 + 매력).", "Three times per day, frighten one enemy (Will negates, DC 10 + half paladin level + Cha)."),
            ["talent.deathstouch.name"] = ("죽음의 안수", "Death's Touch"),
            ["talent.deathstouch.desc"] = ("안수 1회를 써서 적 하나에게 팔라딘 레벨 절반 d6 음에너지 피해를 준다(음에너지 친화 대상은 신성 피해).", "Spend one use of lay on hands to deal 1d6 negative energy per two paladin levels to an enemy (divine against negative energy affinity)."),
            ["talent.ghoststride.name"] = ("망령의 걸음", "Ghost Stride"),
            ["talent.deathsresolve.name"] = ("죽음의 결의", "Death's Resolve"),
            ["talent.deathsresolve.desc"] = ("인접한 아군은 공포에 면역이 된다.", "Adjacent allies are immune to fear."),
            ["talent.densefear.name"] = ("짙은 공포", "Dense Fear"),
            ["talent.densefear.desc"] = ("죽음의 오라가 20피트로 넓어지고 공포 내성 벌칙이 −6이 된다.", "Your aura of death grows to 20 feet and the fear save penalty becomes −6."),
            ["talent.gravearmor.name"] = ("무덤의 갑주", "Grave Armor"),
            ["talent.oathshield.name"] = ("맹세의 방패", "Oath Shield"),
            ["talent.oathshield.desc"] = ("인접한 아군이 받는 근접 피해 중 5를 대신 받는다.", "You take 5 of the melee damage dealt to an adjacent ally instead of them."),
            ["talent.soulharvest.name"] = ("영혼 수확", "Soul Harvest"),
            ["talent.lifedrain.name"] = ("생명 흡수", "Life Drain"),
            ["talent.lifedrain.desc"] = ("스마이트 대상을 맞히면 음성 레벨 1을 준다(1분, 자기 라운드당 1회).", "Hitting your smite target bestows one negative level (1 minute, once per your round)."),
            ["talent.commandundead.name"] = ("언데드 지배", "Command Undead"),
            ["talent.commandundead.desc"] = ("하루 1회, 적 언데드 하나를 지배한다(의지 내성, 팔라딘 DC).", "Once per day, command one enemy undead (Will negates, paladin DC)."),
            ["talent.wailofthedead.name"] = ("사자의 외침", "Wail of the Dead"),
            ["talent.wailofthedead.desc"] = ("하루 1회, 30피트 안의 적을 겁에 질리게 한다(의지 내성, 팔라딘 DC).", "Once per day, frighten enemies within 30 feet (Will negates, paladin DC)."),
            ["talent.cursedstrike.name"] = ("저주받은 일격", "Cursed Strike"),
            ["talent.cursedstrike.desc"] = ("스마이트 대상은 스마이트가 이어지는 동안 치유받지 못한다.", "The target of your smite cannot be healed while the smite lasts."),
            ["talent.ironbones.name"] = ("강철 뼈", "Iron Bones"),
            ["talent.deathblade.name"] = ("죽음의 칼날", "Death Blade"),
            ["talent.empoweredchannel.name"] = ("강화 채널", "Empowered Channel"),
            ["talent.vengefulriposte.name"] = ("원한의 반격", "Vengeful Riposte"),
            ["talent.vengefulriposte.desc"] = ("하루 3회, 근접 공격에 맞으면 사거리 안의 공격자에게 맨손이나 근접 무기로 한 번 반격한다. 실제 반격할 때만 횟수를 소모한다.", "When a melee attack hits you, you can strike its attacker within reach with an unarmed strike or melee weapon. You can riposte three times per day, and spend a use only when you make the attack."),
            ["talent.markofdeath.name"] = ("죽음의 낙인", "Mark of Death"),
            ["talent.unyieldingoath.name"] = ("꺼지지 않는 맹세", "Unyielding Oath"),
            ["talent.unyieldingoath.desc"] = ("하루 1회, HP 가 0 이하로 떨어질 피해를 받으면 HP 1 로 버틴다.", "Once per day, when damage would reduce you to 0 hit points or below, you stay at 1."),
            // T8(설계 §8-d): 신성한 유대 언데드화.
            ["revenantundeadmount.name"] = ("망자의 말", "Revenant's Steed"),
            ["revenantundeadmount.desc"] = ("신성한 유대로 탈것을 고르면 말이 언데드가 된다. 말의 매력 점수에 팔라딘의 매력 수정치가 더해지고, 음에너지 방출이 말도 같은 값만큼 회복한다.", "If you choose a mount for your divine bond, it becomes undead: your Charisma modifier is added to its Charisma score, and your negative channel heals it by the same roll."),
            ["revenantweaponbond.name"] = ("망자의 무기 유대", "Revenant's Weapon Bond"),
            ["revenantweaponbond.desc"] = ("신성한 무기 유대의 선택지에 죽음의 기운·흡혈·유령 접촉·공포의 칼날이 더해진다.", "Your divine weapon bond can also grant necrotic, vampiric, ghost touch or dread blade."),
            ["mounthp.name"] = ("망자의 말 생명력", "Revenant's Steed Vitality"),
            ["mounthp.desc"] = ("주인 팔라딘의 매력 수정치가 이 말의 매력 점수에 더해진다.", "The master paladin's Charisma modifier is added to this steed's Charisma score."),
            ["bond.necrotic.name"] = ("죽음의 기운", "Necrotic"),
            ["bond.necrotic.desc"] = ("명중 시 1d6 음에너지 피해(음에너지 친화 대상은 신성 피해, 구성체 제외).", "+1d6 negative energy on hit (divine against negative energy affinity; not constructs)."),
            ["bond.vampiric.name"] = ("흡혈", "Vampiric"),
            ["bond.vampiric.desc"] = ("명중할 때마다 팔라딘 레벨 절반만큼 회복한다.", "Each hit heals you by half your paladin level."),
            ["bond.ghosttouch.name"] = ("유령 접촉", "Ghost Touch"),
            ["bond.ghosttouch.desc"] = ("무기가 비실체 생물에게 온전히 피해를 준다.", "The weapon fully harms incorporeal creatures."),
            ["bond.dreadblade.name"] = ("공포의 칼날", "Dread Blade"),
            ["bond.dreadblade.desc"] = ("명중한 대상이 의지 내성에 실패하면 1d4 라운드 흔들린다(DC 10 + 팔라딘 레벨 절반 + 매력).", "A creature you hit is shaken for 1d4 rounds on a failed Will save (DC 10 + half paladin level + Cha)."),
            ["talent.lastbreath.name"] = ("마지막 숨", "Last Breath"),
            ["talent.lastbreath.desc"] = ("꺼지지 않는 맹세가 발동하면 주변 적에게 음에너지 폭발을 일으킨다(음에너지 친화 대상은 신성 피해).", "When Unyielding Oath triggers, release a burst of negative energy at nearby enemies (divine against negative energy affinity)."),
        };
        }
    }
    // [순수] 선택 화면의 설명문 — 레벨업 선택 화면의 상세 창이 읽는 BlueprintUnitFact.m_Description(엔진 CharGenFeatureSelectorItemVM.TooltipTemplate
    //  → TooltipTemplateLevelUpFeature.Prepare → FeatureInformation.get_Description; m_DescriptionShort 는 읽지 않는다)과 아키타입 설명. 줄바꿈 = "\n"(본편 설명문과 같은 표기).
    // 수치 잠금(C4): 아래 문구 틀(문자열 리터럴)에는 숫자를 쓰지 않는다 — 수치는 전부 {n} 자리에 규칙 상수(RevenantRules · TalentPlan · LevelPlan · AuraPlan · FearPlan)를 끼워 넣는다.
    //  시험 `strings: choice text` 가 (1) 이 형식의 문자열 리터럴에 {n} 밖의 숫자가 없음 (2) 만들어진 문구의 수치 열 == 손 전사 기대값 (3) ko/en 줄 수·수치 집합 일치
    //  (4) 선택지 설명이 서로 다름을 잠근다. 규칙에 없는 값(엔진·본편 블루프린트가 정하는 값)은 AddTo 안의 지역 변수로 두고 출처를 옆에 적는다
    //  (정적 필드는 `dial: single block` 이 금지) — 시험이 blueprints.zip · Assembly-CSharp 와 대조한다.
    // 틀에 넘긴 인자를 틀이 쓰지 않거나, 원장(TalentPlan · LevelPlan)에 문구가 없는 재주·특성이 생기면 던진다(조용히 빠진 수치·줄 없음).
    // 게임 용어는 한글 패치 표기(악 처단 · 안수 치료 · 겁먹음 · 음수 레벨 · 험지 · 암습 · 일반 행동 · 라운드 전체 행동 · 내성 굴림 · 생명점).
    static class ChoiceText
    {
        // 한/영 한 쌍. 인자로 넘기면 그 언어 쪽이 들어간다.
        sealed class P
        {
            public readonly string Ko, En;
            public P(string ko, string en)
            {
                if (string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en)) throw new ArgumentException("OathboundRevenant: choice text pair with an empty side");
                Ko = ko; En = en;
            }
        }

        static int Max { get { return RevenantRules.Levels.MaxLevel; } }

        static P F(string ko, string en, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args", "OathboundRevenant: choice text argument " + i + " is null (" + en + ")");
                string slot = "{" + i + "}";
                if (!ko.Contains(slot) || !en.Contains(slot)) throw new ArgumentException("OathboundRevenant: choice text template does not use argument " + i + " in both languages (" + en + ")");
            }
            return new P(string.Format(ko, args.Select(a => a is P ? (object)((P)a).Ko : a).ToArray()), string.Format(en, args.Select(a => a is P ? (object)((P)a).En : a).ToArray()));
        }

        // 줄 묶음. 빈 묶음·null 줄이면 던진다.
        static P Lines(params P[] lines)
        {
            if (lines == null || lines.Length == 0 || lines.Any(x => x == null)) throw new ArgumentException("OathboundRevenant: choice text without lines or with a missing line");
            return new P(string.Join("\n", lines.Select(x => x.Ko)), string.Join("\n", lines.Select(x => x.En)));
        }

        // 레벨 구간(from..최고 레벨에서 같은 값이 이어지는 구간): (첫 레벨, 끝 레벨, 값).
        static List<Tuple<int, int, string>> Segments(int from, Func<int, string> at)
        {
            if (from < RevenantRules.Levels.MinLevel || from > Max) throw new ArgumentOutOfRangeException("from", from, "OathboundRevenant: choice text level range starts outside the level table");
            var segs = new List<Tuple<int, int, string>>();
            int start = from;
            string cur = at(from);
            for (int l = from + 1; l <= Max + 1; l++)
            {
                string v = l <= Max ? at(l) : null;
                if (v == cur) continue;
                if (string.IsNullOrEmpty(cur)) throw new InvalidOperationException("OathboundRevenant: choice text value for level " + start + " is empty");
                segs.Add(Tuple.Create(start, l - 1, cur));
                start = l; cur = v;
            }
            return segs;
        }

        // 「a~b레벨 값, c레벨부터 값」. 구간이 하나면 값만.
        static P Ranges(int from, Func<int, string> at)
        {
            var segs = Segments(from, at);
            if (segs.Count == 1) return new P(segs[0].Item3, segs[0].Item3);
            return new P(
                string.Join(", ", segs.Select(s => (s.Item1 == s.Item2 ? s.Item1 + "레벨 " : s.Item2 == Max ? s.Item1 + "레벨부터 " : s.Item1 + "~" + s.Item2 + "레벨 ") + s.Item3)),
                string.Join(", ", segs.Select(s => s.Item3 + (s.Item1 == s.Item2 ? " at level " + s.Item1 : s.Item2 == Max ? " from level " + s.Item1 : " at levels " + s.Item1 + "-" + s.Item2))));
        }

        // 다른 조건의 값 나열 「값 / 값」 — 기본 값과 같은 레벨 구간이어야 한다(어긋나면 던짐: 나열이 어느 레벨의 값인지 알 수 없게 된다).
        static string Steps(int from, Func<int, string> baseAt, Func<int, string> otherAt)
        {
            var a = Segments(from, baseAt);
            var b = Segments(from, otherAt);
            if (!a.Select(s => s.Item1).SequenceEqual(b.Select(s => s.Item1))) throw new InvalidOperationException("OathboundRevenant: choice text: the second value list changes at other levels than the first");
            return string.Join(" / ", b.Select(s => s.Item3));
        }

        static string List(IEnumerable<int> levels, string what)
        {
            var l = levels == null ? null : levels.ToList();
            if (l == null || l.Count == 0) throw new InvalidOperationException("OathboundRevenant: choice text: " + what + " is empty");
            return string.Join(", ", l);
        }

        static string Dice(int count, int sides)
        {
            if (count <= 0 || sides <= 0) throw new ArgumentOutOfRangeException("count", count + "d" + sides, "OathboundRevenant: choice text dice must be positive");
            return count + "d" + sides;
        }

        static void Expect(bool ok, string what)
        {
            if (!ok) throw new InvalidOperationException("OathboundRevenant: choice text is written for a different rule: " + what);
        }

        static P NameOf(Dictionary<string, (string ko, string en)> d, string key)
        {
            (string ko, string en) v;
            if (!d.TryGetValue(key, out v)) throw new KeyNotFoundException("OathboundRevenant: choice text needs the name " + key);
            return new P(v.ko, v.en);
        }

        static P Talent(Dictionary<string, (string ko, string en)> d, string id)
        {
            if (TalentPlan.Rows.Count(r => r.Id == id) != 1) throw new KeyNotFoundException("OathboundRevenant: choice text names the talent " + id + " which is not in TalentPlan exactly once");
            return NameOf(d, "talent." + id.ToLowerInvariant() + ".name");
        }

        static P Join(IEnumerable<P> parts, string koSep, string enSep)
        {
            var l = parts.ToList();
            if (l.Count == 0) throw new ArgumentException("OathboundRevenant: choice text joins an empty list");
            return new P(string.Join(koSep, l.Select(x => x.Ko)), string.Join(enSep, l.Select(x => x.En)));
        }

        internal static void AddTo(Dictionary<string, (string ko, string en)> d)
        {
            if (d == null) throw new ArgumentNullException("d");

            // ---- 규칙에 없는 값(엔진·본편 블루프린트). 시험 `strings: choice text` 가 blueprints.zip · Assembly-CSharp 와 대조한다.
            int closeFeet = 30;   // 엔진 BlueprintAbility.GetRange: AbilityRange.Close(= 1) → switch 둘째 갈래 IL_0065 ldc.i4.s 30 → Feet. 공포의 응시(FearPlan "Close") · 언데드 지배(본편 CommandUndead.jbp Range Close 를 복사)

            // ---- 문구가 기대는 규칙 모양(바뀌면 던진다 — 문구의 「절반」·「내림」·「선」 같은 낱말이 거짓이 된다).
            Expect(RevenantRules.Dials.SaveDcLevelDivisor == 2 && RevenantRules.Dials.HalfLevelDivisor == 2, "the save DC and the dice count use half the paladin level");
            Expect(RevenantRules.Dials.PursuitSpeedMultiplier == 0.5f && RevenantRules.Dials.HealResistance == 0.5f, "pursuit halves the speed and the aura halves healing");
            Expect(SurvivalPlan.GraveArmorBypass == "Good" && SurvivalPlan.GhostStrideImmunity == "DifficultTerrain" && SurvivalPlan.GhostStrideDescriptor == "UntypedStackable", "grave armor is bypassed by good, ghost stride ignores difficult terrain with an untyped bonus");
            Expect(FearPlan.Row("TerribleGaze").Range == "Close" && FearPlan.Row("TerribleGaze").Targets == "Single" && FearPlan.Row("WailOfTheDead").Targets == "EnemiesAround" && FearPlan.OuterSave == "Will" && FearPlan.Descriptors.Contains("Fear") && FearPlan.Descriptors.Contains("MindAffecting"),
                "the gaze is a close single-target Will fear effect and the wail hits the enemies around");
            Expect(StrikePlan.AllBranches.All(b => b.Sides == RevenantRules.Dials.SmiteDie && b.PlainEnergy == StrikePlan.Negative && b.AffinityEnergy == StrikePlan.Divine), "every damage branch is negative energy, divine against negative energy affinity, with the same die");
            Expect(StrikePlan.DamageBranches("DeathBlade").IgnoreCritical && StrikePlan.Trigger("DeathBlade").Range == "Melee" && !StrikePlan.Trigger("DeathBlade").RequiresSmiteTarget && StrikePlan.Trigger("DeathBlade").OnlyHit, "death blade is on every melee hit, not multiplied on a critical");
            Expect(StrikePlan.Trigger("LifeDrain").RequiresSmiteTarget && StrikePlan.Trigger("LifeDrain").RoundMarker != null && StrikePlan.Trigger("LifeDrain").OnlyHit, "life drain is once per round on a hit against the smite target");
            Expect(AuraPlan.Row("DeathAura").NegatedByFact == "DenseFear" && AuraPlan.Row("DenseFear").Effect == AuraPlan.Row("DeathAura").Effect && AuraPlan.Row("DeathsResolve").Target == "Ally" && AuraPlan.Row("OathShield").Target == "Ally", "dense fear replaces the death aura with the same effect; resolve and shield are ally auras");
            Expect(RevenantRules.Dials.TalentPicks == RevenantRules.Levels.TalentLevels.Length && RevenantRules.Dials.TalentOptions == TalentPlan.Rows.Count, "talent picks and options match the level table and the talent ledger");
            Expect(ArchetypeFlags.RemoveSpellbook, "the archetype has no spellbook");
            for (int l = RevenantRules.Levels.HandsReplaceLevel; l <= Max; l++) Expect(RevenantRules.LohDice(l) == l / RevenantRules.Dials.HalfLevelDivisor, "lay on hands dice are half the paladin level rounded down");

            // ---- 공통 낱말·줄
            P smite = new P("악 처단", "smite evil");
            P hands = new P("안수 치료", "lay on hands");
            P handsName = new P("안수 치료", "Lay on Hands");   // 목록·문장 첫머리용(영어 대문자)
            P deathAura = NameOf(d, "deathaura.name");
            P channel = NameOf(d, "negativechannel.name");
            P dc = F("DC {0} + 팔라딘 레벨의 절반(내림) + 매력 수정치", "DC {0} + half your paladin level (rounded down) + your Charisma modifier", RevenantRules.Dials.SaveDcBase);
            P affinity = new P("음에너지 친화가 있는 대상(언데드 등)에게는 같은 주사위의 신성 피해가 된다.", "Against a target with negative energy affinity (undead and the like) the same dice are divine damage.");
            P whileSmite = F("{0}이 이어지는 동안 지속되고, {0}이 끝나면 바로 풀린다. 내성 굴림은 없다.", "It lasts as long as the {0} does and ends as soon as the {0} ends. There is no saving throw.", smite);
            P fearLine = F("정신 작용 공포 효과다. 공포에 대한 내성 굴림 보너스와 페널티({0} 포함)가 이 내성 굴림에 적용된다.",
                           "It is a mind-affecting fear effect: bonuses and penalties on saving throws against fear (including your {0}) apply to this save.", deathAura);
            P fearSave = F("내성 굴림: 의지 — 성공하면 무효({0}). 실패한 적은 {1}라운드 동안 겁먹음 상태가 된다.", "Saving throw: Will negates ({0}). An enemy that fails is frightened for {1} rounds.",
                           dc, Dice(FearPlan.DurationDiceCount, FearPlan.DurationDiceSides));
            Func<TalentSpec, P> prereq = row =>
            {
                TalentSpecRule.SpecFor(row);   // 게이트·선행이 원장 규칙에 맞는지(어긋나면 던진다)
                return row.Requires == null
                    ? F("전제: 팔라딘 {0}레벨.", "Prerequisite: paladin level {0}.", row.GateLevel)
                    : F("전제: 팔라딘 {0}레벨, {1} 재주.", "Prerequisites: paladin level {0}, the {1} talent.", row.GateLevel, Talent(d, row.Requires));
            };

            // ---- 선택 화면
            var gates = TalentPlan.Rows.Select(r => r.GateLevel).Distinct().OrderBy(x => x).ToList();
            var gateLines = gates.Select(g => F("{0}레벨부터: {1}.", "From level {0}: {1}.", g,
                Join(TalentPlan.Rows.Where(r => r.GateLevel == g).Select(r => r.Requires == null ? Talent(d, r.Id) : F("{0}({1} 필요)", "{0} (needs {1})", Talent(d, r.Id), Talent(d, r.Requires))), "·", ", "))).ToList();
            var selection = new List<P>
            {
                F("팔라딘 {0}레벨에 재주를 하나씩 고른다(모두 {1}번, 선택지 {2}종). 같은 재주는 한 번만 고를 수 있다.",
                  "At paladin levels {0} you choose one talent ({1} picks, {2} options). Each talent can be taken only once.",
                  List(RevenantRules.Levels.TalentLevels, "TalentLevels"), RevenantRules.Dials.TalentPicks, RevenantRules.Dials.TalentOptions),
                new P("재주마다 고를 수 있는 가장 낮은 팔라딘 레벨이 정해져 있다.", "Each talent has a lowest paladin level at which it can be chosen."),
            };
            selection.AddRange(gateLines);
            selection.Add(F("{0}·{1}·{2}·{3}은 {4}을 건 적에게만 효과가 있다.", "{0}, {1}, {2} and {3} work only on an enemy under your {4}.",
                Talent(d, "PursuitOath"), Talent(d, "LifeDrain"), Talent(d, "CursedStrike"), Talent(d, "MarkOfDeath"), smite));
            selection.Add(F("{0}는 {1} 사용 횟수를 쓰고, {2}·{3}·{4}·{5}·{6}는 저마다 하루 사용 횟수가 따로 있다.",
                "{0} spends uses of {1}; {2}, {3}, {4}, {5} and {6} each have their own uses per day.",
                Talent(d, "DeathsTouch"), hands, Talent(d, "TerribleGaze"), Talent(d, "WailOfTheDead"), Talent(d, "CommandUndead"), Talent(d, "VengefulRiposte"), Talent(d, "UnyieldingOath")));
            d.Add(StringsTable.TalentSelectionDescKey, (Lines(selection.ToArray()).Ko, Lines(selection.ToArray()).En));

            // ---- 재주 20종(TalentPlan 의 모든 행 — 문구가 없는 행이 생기면 던진다)
            int die = RevenantRules.Dials.SmiteDie;
            foreach (var row in TalentPlan.Rows)
            {
                int gate = row.GateLevel;
                P text;
                switch (row.Id)
                {
                    case "PursuitOath":
                        text = Lines(F("{0} 대상의 이동 속도가 절반이 된다.", "The target of your {0} moves at half speed.", smite), whileSmite, prereq(row));
                        break;
                    case "TerribleGaze":
                        text = Lines(new P("적 하나를 겁먹음 상태로 만드는 초자연 능력을 얻는다.", "Gain a supernatural ability that makes one enemy frightened."),
                            F("사용: 일반 행동, 사거리 {0}피트, 하루 {1}번.", "Use: standard action, range {0} feet, {1} times per day.", closeFeet, FearPlan.Row(row.Id).Daily),
                            fearSave, fearLine, prereq(row));
                        break;
                    case "DeathsTouch":
                        text = Lines(F("{0}의 힘으로 적 하나에게 음에너지 피해를 주는 초자연 능력을 얻는다.", "Gain a supernatural ability that turns {0} into negative energy damage against one enemy.", hands),
                            F("사용: 일반 행동, 근접 접촉 공격. {0} 사용 횟수를 {1}번 쓴다.", "Use: standard action, melee touch attack. It spends {1} use(s) of {0}.", hands, RevenantRules.Dials.DeathsTouchCostLoh),
                            F("피해: 팔라딘 레벨 {0}당 d{1} 하나(내림) — 팔라딘 {2}. 내성 굴림은 없다.", "Damage: one d{1} per {0} paladin levels (rounded down): {2}. There is no saving throw.",
                              RevenantRules.Dials.HalfLevelDivisor, die, Ranges(gate, l => Dice(RevenantRules.LohDice(l), die))),
                            affinity, prereq(row));
                        break;
                    case "GhostStride":
                        text = Lines(new P("험지를 무시하고 이동 속도가 빨라진다.", "Ignore difficult terrain and move faster."),
                            F("이동 속도 +{0}피트(종류 없는 보너스라 다른 보너스와 합산된다). 험지에서도 느려지지 않는다.", "+{0} feet speed (an untyped bonus, so it stacks with other bonuses). Difficult terrain does not slow you.", SurvivalPlan.GhostStrideSpeed),
                            prereq(row));
                        break;
                    case "DeathsResolve":
                        text = Lines(new P("가까이 있는 아군을 공포로부터 지키는 오라를 얻는다.", "Gain an aura that shields nearby allies from fear."),
                            F("{0}피트 안의 아군은 공포 효과(공포 종류의 주문과 상태)에 면역이 된다.", "Allies within {0} feet are immune to fear effects (spells and conditions of the fear type).", AuraPlan.Row(row.Id).EdgeRadiusFeet),
                            new P("범위를 벗어난 아군은 면역을 잃는다.", "An ally who leaves the area loses the immunity."),
                            prereq(row));
                        break;
                    case "DenseFear":
                        text = Lines(F("{0}가 넓어지고 강해진다.", "Your {0} grows wider and stronger.", deathAura),
                            F("범위 {0}피트 → {1}피트, 범위 안 적의 공포에 대한 내성 굴림 페널티 -{2} → -{3}.", "Radius {0} feet → {1} feet; the penalty on saving throws against fear for enemies inside goes from -{2} to -{3}.",
                              AuraPlan.Row("DeathAura").EdgeRadiusFeet, AuraPlan.Row(row.Id).EdgeRadiusFeet, Math.Abs(AuraPlan.Row("DeathAura").FearPenalty), Math.Abs(AuraPlan.Row(row.Id).FearPenalty)),
                            new P("범위 안 적이 받는 치유가 절반이 되는 효과는 그대로다.", "Enemies inside still receive only half healing."),
                            prereq(row));
                        Expect(AuraPlan.Row("DeathAura").FearPenalty < 0 && AuraPlan.Row(row.Id).FearPenalty < 0, "the aura values are penalties");
                        break;
                    case "GraveArmor":
                        text = Lines(new P("물리 피해에 대한 피해 감소를 얻는다.", "Gain damage reduction against physical damage."),
                            F("피해 감소 {0}/선: 선 성향이 아닌 무기의 물리 피해를 {0} 줄인다.", "Damage reduction {0}/good: physical damage from weapons that are not good-aligned is reduced by {0}.", SurvivalPlan.GraveArmorDr),
                            new P("레벨에 따라 늘지 않는다.", "It does not grow with level."),
                            prereq(row));
                        break;
                    case "OathShield":
                        text = Lines(new P("가까이 있는 아군이 받는 근접 피해의 일부를 대신 받는 오라를 얻는다.", "Gain an aura that takes part of the melee damage dealt to nearby allies onto yourself."),
                            // 옮기는 조건 = OathShieldComponent.OnEventDidTrigger → OathShieldRule.Applies(명중 굴림 ∧ 무기 ∧ Weapon.Blueprint.IsMelee). 엔진 IsMelee = !IsRanged,
                            //  IsRanged = AttackType Ranged·RangedTouch(BlueprintWeaponType.get_IsRanged IL_0000~0014) → 근접 접촉 주문(TouchType, AttackType Touch)도 옮기고,
                             
                            F("{0}피트 안의 아군이 근접 공격(명중 굴림이 있는 근접 무기 공격 — 근접 접촉 주문 포함)에 맞아 피해를 받을 때마다, 그 피해 가운데 최대 {1}점을 내가 대신 받는다.",
                              "Each time an ally within {0} feet takes damage from a melee hit (an attack roll with a melee weapon, melee touch spells included), up to {1} points of that damage are dealt to you instead.", AuraPlan.Row(row.Id).EdgeRadiusFeet, RevenantRules.Dials.OathShieldAmount),
                            new P("임시 생명점이 막고 남은 실제 피해가 기준이며 횟수 제한은 없다. 원거리 공격(광선 같은 원거리 접촉 주문 포함)과 명중 굴림이 없는 피해(내성 굴림만 하는 주문·범위 효과 등)는 옮기지 않는다.",
                              "It counts the damage left after temporary hit points and has no limit on uses. Ranged attacks (ranged touch spells such as rays included) and damage without an attack roll (spells with only a saving throw, area effects and the like) are not moved."),
                            prereq(row));
                        break;
                    case "SoulHarvest":
                        text = Lines(new P("무기로 적을 죽이면 생명점을 회복한다.", "Heal when you kill an enemy with a weapon."),
                            new P("회복량: 팔라딘 레벨과 같은 생명점. 횟수 제한은 없다.", "Amount: hit points equal to your paladin level. There is no limit on uses."),
                            F("무기 공격의 피해로 생명점이 바닥난 적이 {0}라운드 안에 실제로 죽어야 한다. 접촉 공격과 광선으로 죽인 경우({1} 등)는 제외된다.",
                              "The enemy brought down by your weapon damage must actually die within {0} round(s). Kills by touch attacks and rays (such as {1}) do not count.", RevenantRules.Dials.SoulHarvestWindowRounds, Talent(d, "DeathsTouch")),
                            prereq(row));
                        break;
                    case "LifeDrain":
                        text = Lines(F("{0} 대상을 무기로 명중시키면 음수 레벨을 준다.", "A weapon hit against the target of your {0} bestows negative levels.", smite),
                            F("음수 레벨 {0}, {1}분 지속. 자기 라운드마다 한 번만 발동하고 내성 굴림은 없다.", "{0} negative level(s) for {1} minute(s). It happens only once per round of yours and allows no saving throw.",
                              RevenantRules.Dials.LifeDrainLevels, RevenantRules.Dials.LifeDrainDurationMinutes),
                            prereq(row));
                        break;
                    case "CommandUndead":
                        text = Lines(new P("적 언데드 하나를 지배하는 초자연 능력을 얻는다(본편 「언데드 지배」 주문과 같은 효과).", "Gain a supernatural ability that commands one enemy undead (the effect of the Command Undead spell)."),
                            F("사용: 라운드 전체 행동, 사거리 {0}피트, 하루 {1}번.", "Use: full-round action, range {0} feet, {1} time(s) per day.", closeFeet, RevenantRules.Dials.CommandDaily),
                            F("내성 굴림: 의지 — 성공하면 무효({0}).", "Saving throw: Will negates ({0}).", dc),
                            new P("지속 시간과, 지능이 있는 언데드가 매 라운드 의지 내성 굴림으로 벗어날 수 있다는 점은 그 주문과 같다.", "The duration, and the Will save an intelligent undead makes every round to break free, are those of that spell."),
                            prereq(row));
                        break;
                    case "WailOfTheDead":
                        text = Lines(new P("주변의 적을 모두 겁먹음 상태로 만드는 초자연 능력을 얻는다.", "Gain a supernatural ability that makes all enemies around you frightened."),
                            F("사용: 일반 행동, 나를 중심으로 {0}피트 안의 모든 적, 하루 {1}번.", "Use: standard action, all enemies within {0} feet of you, {1} time(s) per day.", FearPlan.Row(row.Id).RadiusFeet, FearPlan.Row(row.Id).Daily),
                            fearSave, fearLine, prereq(row));
                        break;
                    case "CursedStrike":
                        text = Lines(F("{0} 대상은 치유를 받지 못한다.", "The target of your {0} cannot be healed.", smite), whileSmite, prereq(row));
                        break;
                    case "IronBones":
                        text = Lines(new P("치명타와 암습을 무효로 만들 확률을 얻는다.", "Gain a chance to negate critical hits and sneak attacks."),
                            F("나에게 들어온 치명타나 암습이 {0}% 확률로 무효가 되어 일반 피해만 받는다(요새화 {0}의 효과와 같다).", "A critical hit or sneak attack against you has a {0}% chance to be negated, so you take normal damage only (the effect of fortification {0}).", SurvivalPlan.IronBonesPercent),
                            new P("피해의 일부를 깎는 효과가 아니다. 치명타·암습 자체가 확률로 무효가 된다.", "It does not reduce damage by a share; the critical hit or sneak attack itself is negated by chance."),
                            prereq(row));
                        break;
                    case "DeathBlade":
                        text = Lines(new P("근접 무기 공격이 명중할 때마다 음에너지 피해를 더한다.", "Every melee weapon hit deals extra negative energy damage."),
                            F("추가 피해 {0}. {1} 대상이 아니어도 적용되며, 치명타에 곱해지지 않는다.", "Extra damage {0}. It applies even when the target is not under your {1}, and it is not multiplied on a critical hit.", Dice(RevenantRules.Dials.DeathBladeDice, die), smite),
                            affinity, prereq(row));
                        break;
                    case "EmpoweredChannel":
                        text = Lines(F("{0}의 주사위가 늘어난다.", "Your {0} rolls more dice.", channel),
                            F("주사위 +{0}: 팔라딘 {1}.", "+{0} dice: {1}.", Dice(RevenantRules.Dials.EmpoweredChannelDice, die), Ranges(gate, l => Dice(RevenantRules.ChannelDice(l, true), die))),
                            new P("적을 맞혔을 때 내가 회복하는 양도 굴린 값만큼이므로 같이 늘어난다.", "The amount you heal when it hits an enemy is the rolled value, so it grows as well."),
                            F("{0}의 피해에는 {1}배로 반영된다(+{2}).", "{0} gains {1} times as much (+{2}).", Talent(d, "LastBreath"), RevenantRules.Dials.LastBreathChannelMultiplier,
                              Dice(RevenantRules.Dials.EmpoweredChannelDice * RevenantRules.Dials.LastBreathChannelMultiplier, die)),
                            prereq(row));
                        break;
                    case "VengefulRiposte":
                        text = Lines(new P("근접 공격에 명중당하면 공격자에게 반격한다.", "Strike back at an attacker who hits you in melee."),
                            F("하루 {0}번. 근접 무기 공격에 명중당했을 때 자동으로, 행동을 쓰지 않고 주 손 무기(맨손 포함)로 그 공격자를 한 번 공격한다.",
                              "{0} times per day. When a melee weapon attack hits you, you automatically make one attack against that attacker with your main-hand weapon (or unarmed), using no action.", RevenantRules.Dials.RiposteDaily),
                            new P("주 손 무기가 근접 무기이고 공격자가 그 간격 안에 있어야 한다. 같은 공격에는 한 번만 반격한다.", "Your main-hand weapon must be a melee weapon and the attacker must be within its reach. One riposte per attack."),
                            prereq(row));
                        break;
                    case "MarkOfDeath":
                        text = Lines(F("{0} 대상이 죽으면 {0}이 다른 적에게 옮겨 간다.", "When the target of your {0} dies, the {0} passes to another enemy.", smite),
                            F("죽은 대상에게서 {0}피트 안에 있는, 살아 있는 악 성향 적 가운데 가장 가까운 적이 새 대상이 된다. {1} 사용 횟수는 쓰지 않는다.",
                              "The nearest living evil enemy within {0} feet of the dead target becomes the new target. No use of {1} is spent.", RevenantRules.Dials.MarkRadiusFeet, smite),
                            F("대상이 죽는 순간까지 {0}이 걸려 있어야 한다.", "The {0} must still be on the target when it dies.", smite),
                            prereq(row));
                        break;
                    case "UnyieldingOath":
                        text = Lines(new P("쓰러질 피해를 받아도 한 번 버틴다.", "Stay standing through damage that would drop you."),
                            F("하루 {0}번. 피해를 받아 생명점이 {1} 이하가 될 때 자동으로 발동해 생명점 {2}로 남는다.", "{0} time(s) per day. When damage would bring you to {1} hit points or below, it triggers automatically and you stay at {2} hit point(s).",
                              RevenantRules.Dials.UnyieldingDaily, 0, RevenantRules.Dials.UnyieldingHpLeft),
                            new P("행동을 쓰지 않는다.", "It uses no action."),
                            prereq(row));
                        break;
                    case "LastBreath":
                        Expect(row.Requires == "UnyieldingOath", "last breath needs unyielding oath");
                        text = Lines(F("{0}가 발동하는 순간, 주변의 적에게 음에너지 폭발을 일으킨다.", "The moment {0} triggers, you release a burst of negative energy at the enemies around you.", Talent(d, row.Requires)),
                            F("범위: 나를 중심으로 {0}피트 안의 모든 적. 자동으로 발동하며 행동을 쓰지 않는다.", "Area: all enemies within {0} feet of you. It happens automatically and uses no action.", RevenantRules.Dials.LastBreathRadiusFeet),
                            F("피해: {0} 주사위의 {1}배 — 팔라딘 {2}. {3} 재주가 있으면 {4}.", "Damage: {1} times the dice of your {0}: {2}. With the {3} talent: {4}.",
                              channel, RevenantRules.Dials.LastBreathChannelMultiplier, Ranges(gate, l => Dice(RevenantRules.LastBreathDice(l, false), die)), Talent(d, "EmpoweredChannel"),
                              Steps(gate, l => Dice(RevenantRules.LastBreathDice(l, false), die), l => Dice(RevenantRules.LastBreathDice(l, true), die))),
                            F("내성 굴림: 의지 — 성공하면 피해 절반({0}).", "Saving throw: Will for half damage ({0}).", dc),
                            affinity, prereq(row));
                        break;
                    default:
                        throw new InvalidOperationException("OathboundRevenant: choice text has no description for the talent " + row.Id);
                }
                d.Add(StringsTable.TalentChoiceKey(row.Id), (text.Ko, text.En));
            }

            // ---- 아키타입(직업 선택 화면에서 고르는 항목): 얻는 것 · 잃는 것
            // 본편 특성 이름(한글 패치 · enGB 표기 — blueprints.zip 의 m_DisplayName 을 2026-11-17 에 읽음). 표에 없는 GUID 가 LevelPlan.Removals 에 생기면 던진다.
            var vanilla = new Dictionary<string, P>
            {
                { Guids.Game.LayOnHandsFeature, handsName },
                { Guids.Game.DivineHealth, new P("신성한 체력", "Divine Health") },
                { Guids.Game.AuraOfCourageFeature, new P("용기의 오라", "Aura of Courage") },
                { Guids.Game.SelectionMercy, new P("자비", "Mercy") },
                { Guids.Game.ChannelEnergyPaladinFeature, new P("양에너지 전달", "Channel Positive Energy") },
            };
            Func<string, P> ownName = key => NameOf(d, (key == "UndeadBody" ? "undead" : key.ToLowerInvariant()) + ".name");
            var gained = LevelPlan.Additions.GroupBy(r => r.Key).OrderBy(g => g.Min(r => r.Level)).Select(g => F("{0}({1}레벨)", "{0} ({1})", ownName(g.Key), List(g.Select(r => r.Level), "levels of " + g.Key))).ToList();
            var lost = LevelPlan.Removals.GroupBy(r => r.Guid).Select(g =>
            {
                P name;
                if (!vanilla.TryGetValue(g.Key, out name)) throw new KeyNotFoundException("OathboundRevenant: choice text has no name for the removed vanilla feature " + g.Key);
                return F("{0}({1}레벨)", "{0} ({1})", name, List(g.Select(r => r.Level), "levels of " + g.Key));
            }).ToList();
            var arch = Lines(
                F("맹세를 지키려 죽음에서 돌아온 팔라딘. 언데드이며, {0} 대상을 칠 때마다 맹세가 몸을 되살린다. 주문을 시전하지 않는다.",
                  "A paladin returned from death to keep an oath. Undead; striking the target of a {0} renews the body. Casts no spells.", smite),
                F("얻는 것(괄호 안은 얻는 팔라딘 레벨): {0}.", "Gained (with the paladin levels): {0}.", Join(gained, ", ", ", ")),
                F("잃는 것(괄호 안은 원래 얻는 레벨): {0}, 팔라딘 주문 시전.", "Lost (with their levels): {0}, paladin spellcasting.", Join(lost, ", ", ", ")),
                new P("언데드의 몸은 음에너지로 치유되고 양에너지에 피해를 입으며, 생명점에 건강 대신 매력 수정치를 쓴다. 매력은 능력의 내성 DC도 결정한다.", "The undead body is healed by negative energy, harmed by positive energy, and uses the Charisma modifier instead of Constitution for hit points. Charisma also governs your ability save DCs."),
                F("{0}는 아군에게만 쓸 수 있고, 본편의 신성한 유대(탈것 또는 무기)는 그대로 고른다.", "{0} works on allies only; the divine bond (mount or weapon) is chosen as usual.", handsName));
            d.Add("archetype.desc", (arch.Ko, arch.En));
        }
    }
}
