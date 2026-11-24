using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
    public static class StringsTable
    {
        // 한/영 표. 키는 접두 없이 쓴다(접두 "<모드 id>." 는 WotrClassMod.Common.Strings 가 ModContext.StringPrefix 로 붙인다).
        // Main.Load 가 Strings.Configure(Table, Guids.Game.ShamanClass) 로 넘긴다. 내용 출처 = docs\2026-11-08-weapon-spirit-shaman-design.md §1·§4.1~§4.7·§14(Q1 다이얼).
         
        // 선택 화면 설명("SpiritSelection.Description") · 강신 특성 II~IV 설명("PossessionFeature*.Description")은 리터럴이 아니다 — 아래 ChoiceText 가 규칙 상수에서 만들어 넣는다(C4: 문구 속 수치 = 규칙 상수).
        // 스피릿 웨폰 선택지 59개의 설명(ChoiceText.Key(범주))은 이 표에 처음부터 있지 않다 — 게임 안에서 SpiritBlueprints 가 시작 무기 블루프린트에서 읽은 값으로 PutChoice 한다(실제 무기 값 = 표시 값).
        internal static readonly Dictionary<string, (string ko, string en)> Table = Build();

        static Dictionary<string, (string ko, string en)> Build()
        {
            var d = new Dictionary<string, (string ko, string en)>
            {
                ["Archetype.Name"] = ("스피릿 웨폰 샤먼", "Weapon-Spirit Shaman"),
                ["Archetype.Description"] = (
                    "스피릿 웨폰 샤먼은 영이 깃든 무기를 부리는 주술사다. 1레벨에 무기 범주 하나를 고르면 그 무기를 든 떠다니는 스피릿 웨폰이 곁에서 스스로 싸우고, 스피릿 웨폰은 주술사 레벨이 오르면 그 범주의 무기 전문화·상급 무기 중점화·상급 무기 전문화를 얻는다. 스피릿 웨폰의 예비 무기 세트에 쥐여 둔 마법 무기의 마력은 스피릿 웨폰이 지금 휘두르는 무기에 비친다(주술사 레벨이 오를수록 더 많이). 5·10·15·20레벨에는 하루 몇 번 강신하여 스피릿 웨폰 무기에 비친 힘을 잠시 자신의 무기로 받아 든다. 18레벨에는 하루 한 번 스피릿 배러지로 스피릿 웨폰이 예비 근접 무기마다 한 번씩 적 하나를 공격하게 한다. 대신 영 동물, 방랑 영, 방랑 주술을 잃는다. 영과 주술, 주문은 그대로다.",
                    "The Weapon-Spirit Shaman commands a spirit dwelling in a weapon. At level 1 you choose a weapon category and a floating spirit weapon wielding that weapon fights at your side; as your shaman level rises it gains Weapon Specialization, Greater Weapon Focus and Greater Weapon Specialization with that category. The magic of weapons placed in the spirit weapon's spare weapon sets is mirrored onto the weapon it is currently wielding (more of it as your shaman level rises). At levels 5, 10, 15 and 20 you can, a few times per day, invite the spirit to possess you and briefly take the mirrored power into your own weapon. At level 18 you can, once per day, call a Spirit Barrage: the weapon spirit attacks one enemy once with each spare melee weapon. In exchange you lose the spirit animal, wandering spirit and wandering hex. Your spirit, hexes and spells are unchanged."),
                ["Archetype.DescriptionShort"] = (
                    "스피릿 웨폰이 곁에서 싸우고, 예비 세트 마법 무기의 마력을 휘두르는 무기에 비추며, 강신으로 그 힘을 받아 든다. 영 동물과 방랑 영·주술을 잃는다.",
                    "A spirit weapon fights beside you, mirrors the magic of weapons in its spare sets onto the weapon it wields, and lends that power to you through possession. Loses the spirit animal, wandering spirit and wandering hex."),
                 
                ["Spirit.Name"] = ("스피릿 웨폰", "Weapon Spirit"),
                ["SpiritSelection.Name"] = ("스피릿 웨폰", "Weapon Spirit"),
                // 성장 특기 단계 숨김 특성(소유자 결정 2026-11-18) — 주술사 쪽·펫 쪽 둘 다 이 한 쌍. HideInUI 라 화면에는 붙여 준 본편 특기(무기 전문화 등)가 보인다.
                ["SpiritGrowthFeat.Name"] = ("스피릿 웨폰 성장 특기", "Weapon Spirit Feat Growth"),
                ["SpiritGrowthFeat.Description"] = ("스피릿 웨폰이 고른 무기 범주로 주술사 레벨에 따라 무기 전문화·상급 무기 중점화·상급 무기 전문화를 얻는다(전제 조건 무시).",
                    "The weapon spirit gains Weapon Specialization, Greater Weapon Focus and Greater Weapon Specialization with the chosen category as the shaman level rises, ignoring prerequisites."),
                ["SlotLock.Name"] = ("스피릿 웨폰 장비 제한", "Weapon Spirit Equipment Limits"),
                ["SlotLock.Description"] = ("스피릿 웨폰은 장화·반지 2개·안경·셔츠 칸을 쓰지 않는다. 무기 칸은 모두 열려 있다.", "The weapon spirit cannot use the boots, ring, glasses and shirt slots. All weapon slots stay open."),
                 
                ["Mirror.Name"] = ("스피릿 웨폰 흡수", "Spirit Mirror"),
                ["Mirror.Description"] = (
                    "스피릿 웨폰의 예비 무기 세트(활성 아닌 세트 3개, 최대 6칸)에 둔 무기의 마력이 스피릿 웨폰이 지금 든 무기에 비친다.\n비치는 수 B = 주술사 레벨 1~3: 1, 4~7: 2, 8~11: 3, 12~15: 4, 16~19: 5, 20: 6.\n강화는 예비 무기 중 가장 높은 것 하나만, 최대 B(5 이하)이며 든 무기 자기 강화보다 높을 때만 비친다.\n특수 속성은 세트 번호·칸 순서대로 최대 B개, 같은 속성은 한 번, 든 무기에 이미 있는 것은 건너뛴다. 유령 접촉은 늘 비치고 개수에 세지 않는다.\n무기 규칙을 바꾸는 속성과 적중 시 발동하는 효과가 비친다. 발동 효과는 한 번에 하나만(세트 번호·칸 순서로 첫 번째) 비치고 특수 속성 1칸을 쓰며, 든 무기의 종류·사거리가 맞지 않아 발동하지 않는 것은 건너뛴다. 비친 발동 효과의 시전자는 스피릿 웨폰이다. 소지자나 시전자 상태에 기대는 효과와 착용자에게 주는 장비 효과는 스피릿 웨폰에 비치지 않는다(강신하면 주술사가 받는다). 임시 강화는 예비 세트에서 사라진다.\n두 손이 비면 맨손에 비친다. 비무기만 들었다면 비치지 않는다. 모드 로그에 비친 효과와 제외 사유가 남는다.",
                    "The magic of weapons in the weapon spirit's spare sets (three inactive sets, up to six slots) is mirrored onto the weapon it wields.\nMirror count B by shaman level: 1-3: 1, 4-7: 2, 8-11: 3, 12-15: 4, 16-19: 5, 20: 6.\nEnhancement: only the highest spare one, at most B (and 5), and only when above the wielded weapon's own.\nSpecial properties: up to B in set and slot order, each once, skipping those already on the wielded weapon. Ghost touch is always mirrored and does not count.\nWeapon-rule properties and on-hit triggered effects are mirrored: at most one triggered effect (first in order, one slot, the spirit is its caster), skipped if it cannot fire on the wielded weapon. Wielder-dependent effects and equipment effects for the wearer are not (possession gives them to the shaman). Temporary enhancements vanish in spare sets.\nEmpty hands can carry the mirror. Holding only nonweapon items prevents it. The log records mirrored and skipped effects."),
                ["Proficiency.Name"] = ("스피릿 웨폰 숙련", "Weapon Spirit Proficiencies"),
                 
                // "Possession.Description" = 강신 I 특성(5레벨) · 강신 능력 · 강신 자원이 같이 쓴다. "Possession2/3.Description" 은 강신 II·III **버프**의 짧은 문구다 —
                //  강신 II·III **특성**(레벨표·레벨업 화면)의 설명은 ChoiceText 가 만드는 "PossessionFeature2/3.Description" 을 쓴다(2026-11-17 키 분리: 특성 설명을 길게 써도 버프 툴팁은 짧게 남는다).
                ["Possession.Name"] = ("강신", "Spirit Possession"),
                ["Possession.Description"] = (
                    "신속 행동으로 스피릿 웨폰을 몸에 받아들여, 스피릿 웨폰 예비 세트 무기의 힘(유령 접촉 포함)을 잠시 자신의 주무기(없으면 맨손)로 받아 든다. 비침과 같은 규칙(개수 B·강화 하나·발동 효과 하나)을 따르되, 소지자 상태에 기대는 효과와 착용자에게 주는 장비 효과도 받는다(일부 장비 효과는 제외). 발동 효과는 자신의 무기 종류·사거리에 맞아야 한다.\n강화는 겹치지 않고, 이미 같은 속성이 있으면 건너뛰며, 스피릿 웨폰 무기 자기 마법 부여는 받지 않는다. 무기를 바꾸면 받은 힘은 사라진다.\n스피릿 웨폰이 살아 있고 보이며 30피트 안에 있을 때만 쓸 수 있다.\n하루 횟수와 지속 — 5레벨: 하루 1회, 3라운드 · 10레벨: 하루 2회, 5라운드 · 15레벨: 하루 3회, 7라운드 · 20레벨: 하루 4회, 10라운드. 휴식하면 회복한다.\n강신 중에도 스피릿 웨폰은 계속 싸운다. 무엇을 받았고 무엇을 왜 건너뛰었는지는 모드 로그에 한 줄로 남는다. 주 손에 무기 아닌 물건을 든 상태에서는 마법 부여를 받지 않는다.",
                    "As a swift action, borrow the power of the spirit’s spare weapons (including ghost touch) for your primary weapon or empty hand. The mirror's rules apply (count B, one enhancement, one triggered effect), and you also receive effects that depend on the wielder and equipment effects granted to the wearer (a few equipment effects excepted). A triggered effect must suit your weapon's type and range.\nEnhancements do not stack, properties your weapon already has are skipped, and the spirit weapon's own enchantments are not copied. If your primary hand holds a nonweapon item, it receives no enchantments. Changing weapons ends the borrowed power.\nUsable only while the weapon spirit is alive, visible and within 30 feet.\nUses and duration: level 5: 1/day, 3 rounds; level 10: 2/day, 5 rounds; level 15: 3/day, 7 rounds; level 20: 4/day, 10 rounds. Uses return after resting.\nThe spirit keeps fighting. The log records borrowed and skipped effects."),
                ["Possession.Unavailable"] = ("강신하려면 스피릿 웨폰이 살아 있고 보이며 30피트 안에 있고, 남은 횟수가 있어야 한다.", "Spirit possession needs the weapon spirit alive, visible and within 30 feet, and a use left."),
                ["Possession1.Name"] = ("강신 I", "Spirit Possession I"),
                ["Possession1.Description"] = (
                    "스피릿 웨폰 예비 세트 무기의 힘(유령 접촉 포함)을 주무기(없으면 맨손)에 받아 들었다. 무기를 바꾸면 사라진다.",
                    "The power of the weapon spirit's spare weapons (ghost touch included) rests on your primary weapon (or empty hand). Changing weapons ends it."),
                ["Possession2.Name"] = ("강신 II", "Spirit Possession II"),
                ["Possession2.Description"] = (
                    "강신 I 에 더해 비행(Airborne: 지면 효과·넘어뜨리기 면역)을 얻는다.",
                    "In addition to Spirit Possession I, you fly (Airborne: immune to ground effects and trip)."),
                ["Possession3.Name"] = ("강신 III", "Spirit Possession III"),
                ["Possession3.Description"] = (
                    "강신 II 에 더해 영의 주술: 주술 DC +2.",
                    "In addition to Spirit Possession II, spirit hexes: hex DC +2."),
                // 강신 IV(20레벨) — 2026-11-18 소유자 결정으로 융합을 뺐다(강신 IV 는 하루 횟수·지속만 는다). 특성 설명은 ChoiceText 가 만드는 "Possession4.Description".
                // 강신 IV 특성(20레벨)의 설명 "Possession4.Description" 은 ChoiceText 가 만든다(특성만 쓰는 키).
                ["Possession4.Name"] = ("강신 IV", "Spirit Possession IV"),
                ["Fusion.Name"] = ("융합", "Spirit Fusion"),
                // 2026-11-18 융합 제거(소유자 결정): 융합 능력·버프는 세이브 호환용으로만 남는다(얻는 경로 없음, 시전 불가 — Fusion.cs RetiredFusionRestriction).
                ["Fusion.Description"] = ("융합은 제거되었다. 이 능력은 쓸 수 없고, 옛 저장을 불러오면 떼어진다.", "Spirit Fusion has been removed. This ability cannot be used and is removed when an old save is loaded."),
                ["Fusion.Unavailable"] = ("융합은 제거되었다.", "Spirit Fusion has been removed."),
                // 스피릿 배러지 / Spirit Barrage(소유자 결정 2026-11-16, 설계 §16). 숫자(30피트·최대 6회·최대 6칸·하루 1회)는 시험 `strings: barrage numbers` 가 SpiritRules.Dials·SpiritBarrageRules 와 대조한다.
                ["SpiritBarrage.Name"] = ("스피릿 배러지", "Spirit Barrage"),
                ["SpiritBarrage.Description"] = (
                    "신속 행동으로 적 하나를 가리키면, 스피릿 웨폰이 예비 무기 세트(활성 아닌 세트 3개, 최대 6칸)에 둔 근접 무기마다 한 번씩 그 적을 공격한다(최대 6회).\n공격은 무기마다 따로 굴리며 모두 스피릿 웨폰의 최고 공격 보너스를 쓴다. 피해는 그 예비 무기 자신의 주사위·강화·특수 속성으로 정해지고, 흡수로 빌려준 마력은 얹히지 않는다.\n원거리 무기·방패·무기가 아닌 것·빈 칸은 참여하지 않으며, 대상에 닿지 않는 무기도 빠진다. 스피릿 웨폰의 평소 공격과 행동은 그대로다.\n스피릿 웨폰이 살아 있고 보이며 30피트 안에 있고, 예비 칸에 근접 무기가 하나 이상 있으며, 대상이 그 무기의 근접 간격 안에 있어야 한다.\n하루 1회 쓸 수 있고 휴식하면 회복한다. 강신 중에도 쓸 수 있고, 융합 중에는 쓸 수 없다.\n공격한 무기와 빠진 칸의 사유는 전투 기록과 모드 로그에 한 줄로 남는다.",
                    "As a swift action you point at one enemy, and the weapon spirit attacks it once with each melee weapon placed in its spare weapon sets (the three inactive sets, up to 6 slots; at most 6 attacks).\nEach attack is rolled separately, all at the weapon spirit's highest attack bonus. Damage comes from that spare weapon's own dice, enhancement and special properties; power lent through the mirror is not added.\nRanged weapons, shields, non-weapons and empty slots do not take part, and a weapon that cannot reach the target is left out. The weapon spirit's normal attacks and actions are unaffected.\nThe weapon spirit must be alive, visible and within 30 feet, at least one spare slot must hold a melee weapon, and the target must be within that weapon's melee reach.\nUsable 1/day; the use returns after resting. Usable during a possession, not during a fusion.\nOne line in the combat log and the mod log lists the weapons that attacked and why any slot was left out."),
                ["SpiritBarrage.Unavailable"] = ("스피릿 배러지를 쓰려면 스피릿 웨폰이 살아 있고 보이며 30피트 안에 있고, 예비 칸에 근접 무기가 하나 이상 있으며, 남은 횟수가 있어야 한다.", "Spirit Barrage needs the weapon spirit alive, visible and within 30 feet, at least one melee weapon in a spare slot, and a use left."),
                ["SpiritBarrage.OutOfReach"] = ("대상이 스피릿 웨폰 예비 무기의 근접 간격 밖에 있다.", "The target is outside the melee reach of the weapon spirit's spare weapons."),
                // 전투 기록 한 줄(SpiritBarrageRules.Summary): {0} 굴린 공격 수 · {1} 명중 수 · {2} 빠진 칸 수 · {3} 사유별 개수.
                ["SpiritBarrage.LogLine"] = ("스피릿 배러지: 스피릿 웨폰의 예비 무기가 {0}회 공격했다(명중 {1}). 빠진 칸 {2}: {3}.", "Spirit Barrage: the weapon spirit's spare weapons attacked {0} time(s) ({1} hit). Slots left out {2}: {3}."),
                ["SpiritBarrage.None"] = ("없음", "none"),
                ["SpiritBarrageSkip.Empty"] = ("빈 칸", "empty slot"),
                ["SpiritBarrageSkip.Ranged"] = ("원거리 무기", "ranged weapon"),
                ["SpiritBarrageSkip.Shield"] = ("방패", "shield"),
                ["SpiritBarrageSkip.NotWeapon"] = ("무기가 아님", "not a weapon"),
                ["SpiritBarrageSkip.VolatileEnchant"] = ("임시 마법 부여가 걸린 무기", "weapon carrying a temporary enchantment"),
                ["SpiritBarrageSkip.OverLimit"] = ("공격 수 상한 초과", "over the attack limit"),
                ["SpiritBarrageSkip.OutOfReach"] = ("간격 밖", "out of reach"),
                ["SpiritBarrageSkip.ResidualOnEquip"] = ("장착 흔적이 남는 무기", "weapon that leaves a trace when wielded"),
                ["Proficiency.Description"] = ("스피릿 웨폰은 스피릿 웨폰 선택지의 모든 무기 범주와 경갑·중갑에 숙련되어 있다(방패 제외).", "The weapon spirit is proficient with every weapon category of the spirit selection and with light and medium armor (no shields)."),
            };
            ChoiceText.AddTo(d);
            return d;
        }

        // 빌드 때(SpiritBlueprints.BuildFeature) 한 범주의 설명(ChoiceText.Describe 결과 — 수치 설명 또는 판정 불가 설명)을 표에 넣고 그 키를 돌려준다.
        // changed = 이미 있던 문구와 달라졌는가(첫 등록이면 false — 호출자는 true 면 던진다).
        internal static string PutChoice(ChoiceOutcome o, out bool changed)
        {
            if (o == null) throw new ArgumentNullException("o", "WeaponSpiritShaman: PutChoice needs a computed choice text");
            (string ko, string en) old;
            changed = Table.TryGetValue(o.Key, out old) && (old.ko != o.Ko || old.en != o.En);
            Table[o.Key] = (o.Ko, o.En);
            return o.Key;
        }

        // 첫 프레임 갱신(SpiritBlueprints.RefreshChoiceTexts): 전부 계산된 설명을 한 번에 반영한다(외부 검토 2026-11-17 지적 7 — 원자적).
        // 검사(null · 같은 키 두 번 · 빌드 때 등록되지 않은 키)를 전부 먼저 하고, 하나라도 어긋나면 표를 건드리지 않고 던진다. 반영 단계는 있는 키의 값 대입뿐이다.
        // 돌려주는 값 = 문구가 바뀐 범주(입력 순서).
        internal static string[] ApplyChoices(IList<ChoiceOutcome> all)
        {
            if (all == null || all.Count == 0) throw new ArgumentException("WeaponSpiritShaman: ApplyChoices needs the computed choice texts");
            var seen = new HashSet<string>();
            var changed = new List<string>();
            foreach (var o in all)
            {
                if (o == null) throw new ArgumentNullException("all", "WeaponSpiritShaman: ApplyChoices got a missing choice text (nothing applied)");
                if (!seen.Add(o.Key)) throw new ArgumentException("WeaponSpiritShaman: ApplyChoices got " + o.Key + " twice (nothing applied)");
                (string ko, string en) old;
                if (!Table.TryGetValue(o.Key, out old)) throw new InvalidOperationException("WeaponSpiritShaman: ApplyChoices: " + o.Key + " was not registered at build (nothing applied)");
                if (old.ko != o.Ko || old.en != o.En) changed.Add(o.Category);
            }
            foreach (var o in all) Table[o.Key] = (o.Ko, o.En);
            return changed.ToArray();
        }
    }

    // [순수] ChoiceText.Describe 의 결과 하나: 범주 · 표 키 · 문구(ko/en) · 판정 불가 사유(null = 무기 수치를 적은 정상 설명; 아니면 그 사유, 영문 — 모드 로그 오류 줄에 쓴다).
    internal sealed class ChoiceOutcome
    {
        public readonly string Category, Key, Ko, En, Undetermined;
        internal ChoiceOutcome(string category, string key, string ko, string en, string undetermined)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en) || undetermined == string.Empty)
                throw new ArgumentException("WeaponSpiritShaman: choice outcome with an empty part (" + category + ")");
            Category = category; Key = key; Ko = ko; En = en; Undetermined = undetermined;
        }
    }

    // [순수] 엔진 접근자에서 읽은 시작 무기 한 자루의 날값(설명이 적을 수 있는지 판정하기 전). 게임 쪽 SpiritBlueprints.ChoiceOf 가 채운다.
    // Size · AttackType = 엔진 열거 이름 그대로(BlueprintItemWeapon.Size / AttackType 의 ToString). 나머지 = ChoiceWeapon 과 같은 뜻.
    // 이 값이 설명 생성기가 모르는 조합이면(ChoiceText.Describe) 그 선택지만 「판정 불가」 설명이 된다 — 직업 등록은 계속된다(지적 7).
    public sealed class ChoiceRaw
    {
        public string Size, AttackType;
        public int Rolls, Sides, CritEdge, CritMultiplier, ReachFeet;
        public bool TwoHanded, Light, Double;
    }

    // [순수] 선택지 설명에 적는 시작 무기 한 자루의 값. 게임 쪽은 SpiritBlueprints.ChoiceOf(BlueprintItemWeapon) 가 그 무기 블루프린트의 접근자에서 읽어 채우고,
    // 시험은 blueprints.zip 의 같은 블루프린트에서 채운다. 필드 뜻 = 엔진 접근자 이름 그대로(BlueprintItemWeapon.BaseDamage · CriticalRollEdge · CriticalModifier · AttackType · AttackRange · IsTwoHanded · IsLight · Double).
    public sealed class ChoiceWeapon
    {
        public int Rolls, Sides;      // 피해 주사위 = Rolls d Sides
        public int CritEdge;          // 치명타가 나는 가장 낮은 d20 눈(19 → 19–20)
        public int CritMultiplier;    // 치명타 배수
        public int ReachFeet;         // 엔진이 쓰는 간격·사거리(피트) = BlueprintItemWeapon.AttackRange.Value(근접은 원값 − 4, 최소 2 — BlueprintWeaponType.get_AttackRange IL)
        public bool Ranged, TwoHanded, Light, Double;
    }

    // [순수] 레벨업·캐릭터 생성 선택 화면의 설명문(상세 창이 읽는 BlueprintUnitFact.m_Description — 엔진 TooltipTemplateLevelUpFeature.Prepare → FeatureInformation.get_Description). 줄바꿈 = "\n".
    // 수치 잠금(C4): 아래 문구 틀(리터럴)에는 숫자를 쓰지 않는다 — 수치는 전부 {n} 자리에 규칙 상수(SpiritRules · SpiritRules.Dials · SpiritCategories · AbsorbPlanner · SpiritBarrageRules)나
    //  무기 값(ChoiceWeapon)을 끼워 넣는다. 시험 `strings: choice text` 가 (1) 이 형식의 문자열 리터럴에 {n} 밖의 숫자가 없음 (2) 만들어진 문구의 수치 == 손 전사 기대값
    //  (3) ko/en 줄 수·수치 집합 일치 (4) 59개 선택지 설명이 서로 다름 (5) 선택지 설명의 무기 수치 == blueprints.zip 의 그 무기 값 을 잠근다.
    // 틀에 넘긴 인자를 틀이 쓰지 않거나 모자라면 던진다(조용히 빠진 수치 없음). 게임 용어 = 한글 패치(WrathKoreanMod translation.json) 표기: 무기 중점화 · 향상된 치명타 · 명품 · 자연 방어 · 쓰러짐 ·
    //  경장 갑옷 · 평장 갑옷 · 생명점 · 신속 행동 · 한손 무기 · 양손 무기 · 가벼운 무기 · 이중 무기. 영어 = Pathfinder 표준 표기.
    static class ChoiceText
    {
        // 한/영 한 쌍. 인자로 넘기면 그 언어 쪽이 들어간다.
        sealed class P
        {
            public readonly string Ko, En;
            public P(string ko, string en)
            {
                if (string.IsNullOrEmpty(ko) || string.IsNullOrEmpty(en)) throw new ArgumentException("WeaponSpiritShaman: choice text pair with an empty side");
                Ko = ko; En = en;
            }
        }

        static int Min { get { return SpiritRules.MinShamanLevel; } }
        static int Max { get { return SpiritRules.MaxShamanLevel; } }

        static P F(string ko, string en, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args", "WeaponSpiritShaman: choice text argument " + i + " is null (" + en + ")");
                string slot = "{" + i + "}";
                if (!ko.Contains(slot) || !en.Contains(slot)) throw new ArgumentException("WeaponSpiritShaman: choice text template does not use argument " + i + " in both languages (" + en + ")");
            }
            return new P(string.Format(ko, args.Select(a => a is P ? (object)((P)a).Ko : a).ToArray()), string.Format(en, args.Select(a => a is P ? (object)((P)a).En : a).ToArray()));
        }

        // 줄 묶음(null = 그 줄 없음). 한 줄도 없으면 던진다.
        static P Lines(params P[] lines)
        {
            var l = lines.Where(x => x != null).ToArray();
            if (l.Length == 0) throw new ArgumentException("WeaponSpiritShaman: choice text without lines");
            return new P(string.Join("\n", l.Select(x => x.Ko)), string.Join("\n", l.Select(x => x.En)));
        }

        static string Slash(IEnumerable<int> values) { return string.Join("/", values); }

        // ---------------- 범주 이름(한/영) ----------------
        // 선택지 제목은 본편 무기 유형 이름(BlueprintWeaponType.TypeName — 게임 언어 팩이 정한다)이고, 설명 본문에 적는 이름은 이 표다.
        // 왜 표인가: 블루프린트 생성 시점에는 한글 패치가 영어 팩을 덮어쓰기 전일 수 있어(Common\Strings.cs 「언어 판정」 주석) TypeName 을 읽어 굳히면 한국어 화면에 영어 이름이 박힐 수 있다.
        // 출처: ko = 한글 패치 WrathKoreanMod Resources\translation.json, en = 본편 enGB.json 의 그 유형 m_TypeNameText 키 문구(2026-11-17 직독).
        //  SawtoothSabre 의 en 은 Pathfinder 표준 표기다(이 설치본의 enGB.json 은 그 키에 한글 문구가 들어 있다). 시험 `strings: choice names` 가 두 파일과 대조한다(어긋나면 FAIL).
        static readonly Dictionary<string, P> Names = new Dictionary<string, P>
        {
            { "Bardiche", new P("바디시", "Bardiche") },
            { "BastardSword", new P("바스타드 소드", "Bastard Sword") },
            { "Battleaxe", new P("전투도끼", "Battleaxe") },
            { "Club", new P("몽둥이", "Club") },
            { "Dagger", new P("단검", "Dagger") },
            { "Dart", new P("다트", "Dart") },
            { "DoubleAxe", new P("오크 양날도끼", "Orc Double Axe") },
            { "DoubleSword", new P("쌍날검", "Two-Bladed Sword") },
            { "DuelingSword", new P("결투용 검", "Dueling Sword") },
            { "DwarvenWaraxe", new P("드워프 전투도끼", "Dwarven Waraxe") },
            { "EarthBreaker", new P("지진망치", "Earth Breaker") },
            { "ElvenCurvedBlade", new P("엘프 곡도", "Elven Curve Blade") },
            { "Estoc", new P("에스톡", "Estoc") },
            { "Falcata", new P("팔카타", "Falcata") },
            { "Falchion", new P("펄션", "Falchion") },
            { "Fauchard", new P("포샤드", "Fauchard") },
            { "Flail", new P("도리깨", "Flail") },
            { "Glaive", new P("글레이브", "Glaive") },
            { "Greataxe", new P("대형도끼", "Greataxe") },
            { "Greatclub", new P("대형 몽둥이", "Greatclub") },
            { "Greatsword", new P("대검", "Greatsword") },
            { "Handaxe", new P("손도끼", "Handaxe") },
            { "HeavyCrossbow", new P("대형 쇠뇌", "Heavy Crossbow") },
            { "HeavyFlail", new P("대형 도리깨", "Heavy Flail") },
            { "HeavyMace", new P("중형 철퇴", "Heavy Mace") },
            { "HeavyPick", new P("중형 곡괭이", "Heavy Pick") },
            { "HookedHammer", new P("노움 갈고리망치", "Gnome Hooked Hammer") },
            { "Javelin", new P("투창", "Javelin") },
            { "Kama", new P("카마", "Kama") },
            { "Kukri", new P("쿠크리", "Kukri") },
            { "LightCrossbow", new P("소형 쇠뇌", "Light Crossbow") },
            { "LightHammer", new P("소형 망치", "Light Hammer") },
            { "LightMace", new P("소형 철퇴", "Light Mace") },
            { "LightPick", new P("소형 곡괭이", "Light Pick") },
            { "Longbow", new P("장궁", "Longbow") },
            { "Longspear", new P("장창", "Longspear") },
            { "Longsword", new P("장검", "Longsword") },
            { "Nunchaku", new P("쌍절곤", "Nunchaku") },
            { "PunchingDagger", new P("주먹칼", "Punching Dagger") },
            { "Quarterstaff", new P("육척봉", "Quarterstaff") },
            { "Rapier", new P("레이피어", "Rapier") },
            { "Sai", new P("필가차", "Sai") },
            { "SawtoothSabre", new P("톱날 세이버", "Sawtooth Sabre") },
            { "Scimitar", new P("시미터", "Scimitar") },
            { "Scythe", new P("대낫", "Scythe") },
            { "Shortbow", new P("단궁", "Shortbow") },
            { "Shortspear", new P("단창", "Shortspear") },
            { "Shortsword", new P("소검", "Shortsword") },
            { "Sickle", new P("낫", "Sickle") },
            { "SlingStaff", new P("슬링 스태프", "Sling Staff") },
            { "Spear", new P("창", "Spear") },
            { "SpikedHeavyShield", new P("가시 중형 방패", "Spiked Heavy Shield") },
            { "SpikedLightShield", new P("가시 소형 방패", "Spiked Light Shield") },
            { "Starknife", new P("스타나이프", "Starknife") },
            { "ThrowingAxe", new P("투척 도끼", "Throwing Axe") },
            { "Tongi", new P("통기", "Tongi") },
            { "Trident", new P("삼지창", "Trident") },
            { "Urgrosh", new P("드워프 우르그로쉬", "Dwarven Urgrosh") },
            { "Warhammer", new P("전투망치", "Warhammer") },
        };

        // 시험용 읽기 창(범주 → 한/영 이름). 표에 없는 범주는 던진다.
        internal static (string ko, string en) NameOf(string category)
        {
            P p;
            if (category == null || !Names.TryGetValue(category, out p)) throw new KeyNotFoundException("WeaponSpiritShaman: choice text has no name for weapon category '" + category + "'");
            return (p.Ko, p.En);
        }

        internal static IEnumerable<string> NamedCategories { get { return Names.Keys; } }

        // 선택지 설명의 문자열 키(범주마다 하나). 표 키 모양 리터럴("A.B")을 쓰지 않는다 — 시험 `strings: every key literal` 은 처음부터 표에 있는 키만 본다.
        internal static string Key(string category)
        {
            if (string.IsNullOrEmpty(category)) throw new ArgumentException("WeaponSpiritShaman: choice text key needs a category");
            return "SpiritFeature." + category + ".Description";
        }

        // ---------------- 선택지(범주 하나) ----------------
        // d20 의 가장 높은 눈 = 치명타 범위의 끝(범위는 「CritEdge–이 값」). 출처: 엔진 명중 굴림 = d20(RuleAttackRoll), 치명타 위협 = 굴림 >= CriticalRollEdge.
        const int TopRoll = 20;

        static string Dice(ChoiceWeapon w) { return w.Rolls + "d" + w.Sides; }

        static string Crit(ChoiceWeapon w) { return (w.CritEdge == TopRoll ? w.CritEdge.ToString() : w.CritEdge + "–" + TopRoll) + "/×" + w.CritMultiplier; }

        // 설명이 수치를 적을 수 있는 무기 값의 하한(Require 와 Describe 가 같은 목록 NumberIssues 를 쓴다 — C4).
        //  MinRolls = 1: 주사위 0개는 피해 주사위가 아니다(DiceFormula.Rolls).
        //  MinSides = 2: 엔진 DiceType 은 Zero=0 · One=1 · D2=2 …(Assembly-CSharp Kingmaker.RuleSystem.DiceType, 2026-11-17 dnlib 직독) — Zero·One 은 굴리는 주사위가 아니다.
        //  LowestCritEdge = 2: d20 의 1 은 늘 빗나간다(Pathfinder 핵심 규칙 「자연 1」) — 1 이하 눈은 치명타 범위로 적지 않는다. 상한은 TopRoll.
        //  MinMultiplier = 2: 엔진 DamageCriticalModifierType 의 가장 작은 값이 X2=2 다(같은 직독 · 시험 `strings: choice text: engine DiceType …`).
        //  MinReachFeet = 2: 엔진 GameConsts.MinWeaponRange = 2(시험 `strings: choice text: engine GameConsts.MinWeaponRange = 2 feet`) — 엔진 접근자는 이보다 작은 값을 돌려주지 않는다.
        const int MinRolls = 1, MinSides = 2, LowestCritEdge = 2, MinMultiplier = 2, MinReachFeet = 2;
        // 설명이 적는 공격 종류. 엔진 AttackType = Melee · Touch · Ranged · RangedTouch(같은 직독) — Touch · RangedTouch 는 설명 생성기가 모른다(판정 불가).
        internal const string MeleeName = "Melee", RangedName = "Ranged";

        // 수치 목록이 설명이 적을 수 있는 무기 값인가. 빈 목록 = 적을 수 있다. 사유는 ko/en 한 쌍씩(판정 불가 문구와 로그에 그대로 쓴다).
        static List<P> NumberIssues(int rolls, int sides, int critEdge, int multiplier, int reachFeet, bool twoHanded, bool light)
        {
            var l = new List<P>();
            if (rolls < MinRolls || sides < MinSides) l.Add(F("피해 주사위 {0}d{1}", "damage dice {0}d{1}", rolls, sides));
            if (critEdge < LowestCritEdge || critEdge > TopRoll) l.Add(F("치명타 눈 {0}", "critical edge {0}", critEdge));
            if (multiplier < MinMultiplier) l.Add(F("치명타 배수 ×{0}", "critical multiplier ×{0}", multiplier));
            if (reachFeet < MinReachFeet) l.Add(F("간격·사거리 {0}피트", "reach or range {0} feet", reachFeet));
            if (twoHanded && light) l.Add(new P("양손 무기이면서 가벼운 무기", "both two-handed and light"));
            return l;
        }

        // 엔진 날값 한 자루의 판정 불가 사유(빈 목록 = 적을 수 있다). 크기·공격 종류를 못 읽었으면(null) 「모름」 — 사유가 아니라 예외다(C1).
        static List<P> RawIssues(string category, string which, ChoiceRaw w)
        {
            if (w == null) throw new ArgumentNullException("w", "WeaponSpiritShaman: choice text for " + category + ": " + which + " weapon values are missing");
            if (w.Size == null || w.AttackType == null) throw new ArgumentException("WeaponSpiritShaman: choice text for " + category + ": " + which + " weapon size / attack type was not read");
            var l = new List<P>();
            if (w.Size != SpiritRules.SpiritSizeName) l.Add(F("크기 {0}(설명은 {1} 무기의 주사위만 적는다)", "size {0} (only {1} weapon dice are described)", w.Size, SizeName(SpiritRules.SpiritSizeName)));
            if (w.AttackType != MeleeName && w.AttackType != RangedName) l.Add(F("공격 종류 {0}(근접·원거리만 적는다)", "attack type {0} (only melee and ranged are described)", w.AttackType));
            l.AddRange(NumberIssues(w.Rolls, w.Sides, w.CritEdge, w.CritMultiplier, w.ReachFeet, w.TwoHanded, w.Light));
            return l;
        }

        static void Require(string category, string which, ChoiceWeapon w)
        {
            if (w == null) throw new ArgumentNullException("w", "WeaponSpiritShaman: choice text for " + category + ": " + which + " weapon values are missing");
            var issues = NumberIssues(w.Rolls, w.Sides, w.CritEdge, w.CritMultiplier, w.ReachFeet, w.TwoHanded, w.Light);
            if (issues.Count > 0) throw new ArgumentException("WeaponSpiritShaman: choice text for " + category + ": " + which + " weapon values are not a describable weapon (" + string.Join("; ", issues.Select(x => x.En)) + ")");
        }

        // 선택지 한 범주(게임 쪽 입구): 엔진 날값 → 수치 설명, 또는 그 선택지만 「판정 불가」 설명(사유 포함) — 직업 등록은 계속된다(외부 검토 2026-11-17 지적 7).
        // rowDouble = 범주 표의 이중 무기 여부(두 번째 머리 값은 이때만 준다 — 어긋나면 던진다: 우리 빌드의 불변식). 범주 이름이 표에 없으면 던진다(NameOf).
        // 판정 불가는 조용한 생략이 아니다: 문구가 「판정 불가」와 사유를 적고, 결과의 Undetermined 를 호출자가 모드 로그 오류 줄로 쓴다.
        internal static ChoiceOutcome Describe(string category, bool rowDouble, ChoiceRaw main, ChoiceRaw second)
        {
            var name = NameOf(category);
            var n = new P(name.ko, name.en);
            if (rowDouble != (second != null)) throw new ArgumentException("WeaponSpiritShaman: choice text for " + category + ": the category table says double=" + rowDouble + " but second head values are " + (second == null ? "missing" : "given"));
            var issues = RawIssues(category, "starting", main);
            if (main.Double != rowDouble) issues.Add(F("이중 무기 표시 {0}(범주 표는 {1})", "double weapon flag {0} (the category table says {1})", main.Double, rowDouble));
            if (second != null) issues.AddRange(RawIssues(category, "second head", second).Select(p => F("두 번째 머리 {0}", "second head {0}", p)));
            if (issues.Count == 0)
            {
                var text = Choice(category, Weapon(main), second == null ? null : Weapon(second));
                return new ChoiceOutcome(category, Key(category), text.ko, text.en, null);
            }
            var reasons = new P(string.Join(", ", issues.Select(x => x.Ko)), string.Join("; ", issues.Select(x => x.En)));
            var u = Lines(
                Wields(n),
                F("시작 무기 수치: 판정 불가 — 이 게임에서 읽은 시작 무기 값을 이 설명이 나타낼 수 없어 수치를 적지 않는다({0}). 모드 로그에 오류 줄이 남는다.",
                    "Starting weapon numbers: undetermined. The starting weapon values read in this game cannot be expressed by this description, so no numbers are given ({0}). The mod log has an error line.", reasons),
                FocusLine(n),
                AutoFeatLine(n),
                main.AttackType == RangedName ? BarrageNote() : null,
                PrerequisiteLine());
            return new ChoiceOutcome(category, Key(category), u.Ko, u.En, reasons.En);
        }

        // 판정을 통과한 날값 → 생성기 값(Ranged 는 공격 종류가 Ranged 일 때만 — Describe 가 Melee/Ranged 밖을 이미 걸렀다).
        static ChoiceWeapon Weapon(ChoiceRaw w)
        {
            return new ChoiceWeapon { Rolls = w.Rolls, Sides = w.Sides, CritEdge = w.CritEdge, CritMultiplier = w.CritMultiplier, ReachFeet = w.ReachFeet, Ranged = w.AttackType == RangedName, TwoHanded = w.TwoHanded, Light = w.Light, Double = w.Double };
        }

        // 수치 설명과 판정 불가 설명이 같이 쓰는 줄(무기 값에 기대지 않는 규칙 사실).
        static P Wields(P n) { return F("스피릿 웨폰이 명품 {0} 하나를 들고 곁에서 스스로 싸운다.", "A weapon spirit wielding a masterwork {0} fights at your side on its own.", n); }
        // 스피릿 웨폰 성장 특기(소유자 결정 2026-11-18): 레벨은 SpiritRules.SpiritGrowthFeats 에서(문구에 숫자 리터럴 없음). 단계 모양(spec·gwf·gws 순서)이 바뀌면 문구가 거짓이 된다 → 던진다.
        static P FocusLine(P n)
        {
            var t = SpiritRules.SpiritGrowthFeats;
            if (t.Length != 3 || t[0].FeatGuid != Guids.Game.WeaponSpecialization || t[1].FeatGuid != Guids.Game.GreaterWeaponFocus || t[2].FeatGuid != Guids.Game.GreaterWeaponSpecialization)
                throw new InvalidOperationException("WeaponSpiritShaman: choice text names Weapon Specialization / Greater Weapon Focus / Greater Weapon Specialization — SpiritRules.SpiritGrowthFeats changed");
            return F("스피릿 웨폰은 주술사 {1}레벨에 무기 전문화({0}), {2}레벨에 상급 무기 중점화({0}), {3}레벨에 상급 무기 전문화({0})를 얻는다(전제 조건 무시). 주술사 자신은 이 무기의 숙련도 특기도 얻지 않는다.",
                "The weapon spirit gains Weapon Specialization ({0}) at shaman level {1}, Greater Weapon Focus ({0}) at {2} and Greater Weapon Specialization ({0}) at {3}, ignoring prerequisites. The shaman gains neither proficiency nor feats with this weapon.",
                n, t[0].ShamanLevel, t[1].ShamanLevel, t[2].ShamanLevel);
        }
        static P AutoFeatLine(P n)
        {
            return F("스피릿 웨폰의 자동 특기 목록에 있는 무기 중점화와 향상된 치명타는 {0} 대상이다. 스피릿 웨폰이 다른 범주의 무기로 바꿔 쥐어도 그 대상은 바뀌지 않는다.",
                "The Weapon Focus and Improved Critical on the weapon spirit's automatic feat list target {0}. They keep that target even if the weapon spirit wields a weapon of another category.", n);
        }
        static P BarrageNote() { return new P("스피릿 배러지에는 예비 칸의 근접 무기만 참여한다 — 원거리 무기는 빠진다.", "Spirit Barrage uses only melee weapons in the spare slots; ranged weapons take no part."); }
        static P PrerequisiteLine() { return new P("전제 조건: 동물 동료가 없어야 한다.", "Prerequisite: no animal companion."); }

        // 범주 하나의 설명. main = 시작 무기, second = 이중 무기의 두 번째 머리(이중 무기가 아니면 null — 어긋나면 던진다).
        internal static (string ko, string en) Choice(string category, ChoiceWeapon main, ChoiceWeapon second)
        {
            var name = NameOf(category);
            var n = new P(name.ko, name.en);
            Require(category, "starting", main);
            if (main.Double != (second != null)) throw new ArgumentException("WeaponSpiritShaman: choice text for " + category + ": Double=" + main.Double + " but second head values are " + (second == null ? "missing" : "given"));
            if (second != null) Require(category, "second head", second);

            P kind = main.Ranged ? new P("원거리", "ranged") : new P("근접", "melee");
            P hands = main.Double ? new P("이중 무기", "double weapon") : main.TwoHanded ? new P("양손 무기", "two-handed weapon") : main.Light ? new P("가벼운 무기", "light weapon") : new P("한손 무기", "one-handed weapon");
            P reach = main.Ranged ? new P("사거리", "range") : new P("근접 간격", "melee reach");
            var text = Lines(
                Wields(n),
                F("시작 무기: {0} · {1} · 피해 {2} · 치명타 {3} · {4} {5}피트.", "Starting weapon: {0}; {1}; damage {2}; critical {3}; {4} {5} feet.", kind, hands, Dice(main), Crit(main), reach, main.ReachFeet),
                second == null ? null : F("두 번째 머리: 피해 {0} · 치명타 {1}.", "Second head: damage {0}; critical {1}.", Dice(second), Crit(second)),
                FocusLine(n),
                AutoFeatLine(n),
                !main.Ranged ? null : BarrageNote(),
                PrerequisiteLine());
            return (text.Ko, text.En);
        }

        // ---------------- 선택 화면 · 강신 특성 II~IV ----------------
        static P SizeName(string size)
        {
            if (size == "Medium") return new P("중형", "Medium");
            throw new ArgumentException("WeaponSpiritShaman: choice text has no wording for spirit size '" + size + "'");
        }

        static P ActionName(DialAction a)
        {
            if (a == DialAction.Swift) return new P("신속 행동", "swift action");
            throw new ArgumentException("WeaponSpiritShaman: choice text has no wording for action " + a);
        }

        // 스피릿 웨폰이 못 쓰는 장비 칸(SpiritRules.SpiritLockedSlots)의 문구. 모르는 칸 이름이면 던진다.
        static P LockedSlots()
        {
            var ko = new List<string>();
            var en = new List<string>();
            var slots = SpiritRules.SpiritLockedSlots;
            int rings = slots.Count(s => s.StartsWith("Ring", StringComparison.Ordinal));
            bool ringDone = false;
            foreach (var s in slots)
            {
                if (s == "Boots") { ko.Add("신발"); en.Add("boots"); }
                else if (s == "Glasses") { ko.Add("안경"); en.Add("glasses"); }
                else if (s == "Shirt") { ko.Add("상의"); en.Add("shirt"); }
                else if (s.StartsWith("Ring", StringComparison.Ordinal))
                {
                    if (ringDone) continue;
                    ringDone = true;
                    ko.Add("반지 " + rings + "개"); en.Add(rings + " ring");
                }
                else throw new ArgumentException("WeaponSpiritShaman: choice text has no wording for locked slot '" + s + "'");
            }
            if (ko.Count == 0) throw new InvalidOperationException("WeaponSpiritShaman: choice text: SpiritRules.SpiritLockedSlots is empty");
            return new P(string.Join("·", ko), string.Join(", ", en));
        }

        static void Put(Dictionary<string, (string ko, string en)> d, string key, P text) { d.Add(key, (text.Ko, text.En)); }

        internal static void AddTo(Dictionary<string, (string ko, string en)> d)
        {
            if (d == null) throw new ArgumentNullException("d");
            // 문구가 기대는 규칙의 모양(어긋나면 문구가 거짓이 된다 → 던진다).
            if (!SpiritRules.RemovedPetFeatures.SequenceEqual(new[] { Guids.Game.AnimalCompanionSlotFeature, Guids.Game.AnimalType, Guids.Game.Multiattack }))
                throw new InvalidOperationException("WeaponSpiritShaman: choice text says the spirit gains no animal type and no Multiattack — SpiritRules.RemovedPetFeatures changed");
            if (!SpiritRules.SpiritUnitComponentTypes.Contains("AddResurrectOnRest"))
                throw new InvalidOperationException("WeaponSpiritShaman: choice text says the spirit returns after a rest — the unit no longer copies AddResurrectOnRest");
            if (!SpiritRules.SpiritArmorProficiencies.SequenceEqual(new[] { "Light", "Medium" }))
                throw new InvalidOperationException("WeaponSpiritShaman: choice text says light and medium armor — SpiritRules.SpiritArmorProficiencies changed");
            var stats = SpiritRules.SpiritBaseStats;
            if (stats.Length != StatLabels.Length) throw new InvalidOperationException("WeaponSpiritShaman: choice text labels " + StatLabels.Length + " base stats, SpiritRules.SpiritBaseStats has " + stats.Length);
            P statLine = new P(string.Join(" · ", StatLabels.Select((l, i) => l.Ko + " " + stats[i])), string.Join(", ", StatLabels.Select((l, i) => l.En + " " + stats[i])));
            var tiers = SpiritRules.Dials.PossessionTiers;
            if (tiers.Length != StageNames.Length) throw new InvalidOperationException("WeaponSpiritShaman: choice text names " + StageNames.Length + " possession stages, Dials has " + tiers.Length);
            var last = tiers[tiers.Length - 1];
            P action = ActionName(SpiritRules.Dials.PossessionAction);
            int feet = SpiritRules.Dials.PossessionRangeFeet;
            // 선택지 수 = 이름 표의 범주 수. 범주 표(SpiritCategories.Rows)는 여기서 읽지 않는다(범주 루프는 SpiritBlueprints.Build 하나 — 시험 `wiring: one loop builds all`).
            //  이름 표 == 범주 표는 시험 `strings: choice names` 가 잠그고, 게임 안에서는 범주마다 NameOf 가 없는 이름에 던진다.
            int categories = Names.Count;

            // 전제 조건(동물 동료 없음)은 선택지마다 적는다 — 선택 화면 자체에는 전제 조건이 없다.
            Put(d, "SpiritSelection.Description", Lines(
                F("주술사 {0}레벨에 설치된 무기 범주 중 하나를 고른다(최대 {1}개). 나중에 범주를 바꾸는 능력은 없다.",
                    "At shaman level {0}, choose one of the available weapon categories (up to {1}) for good.", SpiritRules.SpiritSelectionLevel, categories),
                F("범주가 정하는 것: 스피릿 웨폰의 명품 시작 무기, 무기 중점화·향상된 치명타·무기 전문화({0})·상급 무기 중점화({1})·상급 무기 전문화({2})의 대상.",
                    "Sets the spirit's masterwork weapon and the category of its Weapon Focus, Improved Critical, Weapon Specialization ({0}), Greater Weapon Focus ({1}) and Greater Weapon Specialization ({2}).",
                    SpiritRules.SpiritGrowthFeats[0].ShamanLevel, SpiritRules.SpiritGrowthFeats[1].ShamanLevel, SpiritRules.SpiritGrowthFeats[2].ShamanLevel),
                new P("스피릿 웨폰은 모든 범주의 무기와 경장·평장 갑옷에 숙련되어 무기를 바꿔 쥘 수 있다. 주술사는 그 무기의 숙련을 얻지 않는다.",
                    "The spirit can wield any category (light and medium armor too); you gain no proficiency."),
                F("스피릿 웨폰: {0}, 이동 속도 {1}피트, {2}, 자연 방어 +{3}. 떠다니며 쓰러짐에 면역이다. 무기 세트 {4}개를 쓰고 {5} 칸은 못 쓴다.",
                    "Spirit: {0}, speed {1} feet, {2}, natural armor +{3}. It floats, is immune to prone, and uses all {4} weapon sets but not the {5} slots.",
                    SizeName(SpiritRules.SpiritSizeName), SpiritRules.SpiritSpeed, statLine, SpiritRules.Dials.SpiritNaturalArmor, AbsorbPlanner.SetCount, LockedSlots()),
                F("주술사 {0}~{1}레벨 매 레벨 동물 동료처럼 자라고(동물 유형·다중공격 없음), 쓰러지면 휴식 뒤 되살아난다.",
                    "It grows like an animal companion at shaman levels {0}-{1} (no animal type or Multiattack); if it falls, it returns after a rest.", Min, Max),
                F("흡수: 예비 무기 세트 무기의 마력이 든 무기에 비친다({0}레벨 {1}개, {2}레벨 {3}개까지).",
                    "Mirror: magic of weapons in its spare sets shows on its weapon ({1} at level {0}, {3} at level {2}).", Min, SpiritRules.Budget(Min), Max, SpiritRules.Budget(Max)),
                F("강신({0}레벨): {1}으로 그 힘을 자신의 무기로 받는다. 하루 {2}회, {3}라운드.",
                    "Possession (levels {0}): as a {1}, that power moves to your weapon; {2} per day, {3} rounds.",
                    Slash(tiers.Select(t => t.Level)), action, Slash(tiers.Select(t => t.UsesPerDay)), Slash(tiers.Select(t => t.Rounds))),
                F("스피릿 배러지({0}레벨): 하루 {1}회, 예비 칸 근접 무기마다 한 번씩 적 하나를 공격한다(최대 {2}회).",
                    "Spirit Barrage (level {0}): {1}/day, each spare melee weapon attacks one enemy once (up to {2}).",
                    SpiritRules.Dials.SpiritBarrageLevel, SpiritRules.Dials.SpiritBarrageUsesPerDay, SpiritRules.Dials.SpiritBarrageMaxAttacks)));

            // 강신 특성 II · III(레벨표·레벨업 화면): 버프 문구("Possession2/3.Description")와 키를 나눈다.
            P usable = F("발동: {0}. 스피릿 웨폰이 살아 있고 보이며 {1}피트 안에 있어야 한다.", "Activation: {0}. The weapon spirit must be alive, visible and within {1} feet.", action, feet);
            Put(d, "PossessionFeature2.Description", Lines(
                F("{0} 에 더해, 강신하는 동안 비행한다(지면 효과와 넘어뜨리기에 면역).", "In addition to {0}, you fly while possessed (immune to ground effects and trip).", StageNames[0]),
                StageLine(tiers[1]),
                usable));
            Put(d, "PossessionFeature3.Description", Lines(
                F("{0} 에 더해, 강신하는 동안 주술 DC +{1}.", "In addition to {0}, hex DC +{1} while possessed.", StageNames[1], SpiritRules.Dials.PossessionHexDcBonus),
                StageLine(tiers[2]),
                usable));
            // 강신 IV 특성(20레벨, 2026-11-18 융합 제거 — 소유자 결정): 효과는 강신 III 그대로, 하루 횟수·지속이 표 마지막 단계로 는다(PossessionRules.EffectBuffIndex).
            Put(d, "Possession4.Description", Lines(
                F("{0} 의 효과(비행·주술 DC +{1})를 그대로 받고, 하루 횟수와 지속이 늘어난다.", "You keep the effects of {0} (flight, hex DC +{1}); uses per day and duration increase.", StageNames[2], SpiritRules.Dials.PossessionHexDcBonus),
                StageLine(last),
                usable));
        }

        // 능력치 이름. 순서 = SpiritRules.SpiritBaseStats(힘·민첩·건강·지능·지혜·매력).
        static readonly P[] StatLabels = { new P("힘", "Str"), new P("민첩", "Dex"), new P("건강", "Con"), new P("지능", "Int"), new P("지혜", "Wis"), new P("매력", "Cha") };

        // 강신 단계 이름(로마 숫자 — 문구 틀에 아라비아 숫자를 쓰지 않는다). 순서 = SpiritRules.Dials 의 강신 표.
        static readonly P[] StageNames = { new P("강신 I", "Spirit Possession I"), new P("강신 II", "Spirit Possession II"), new P("강신 III", "Spirit Possession III"), new P("강신 IV", "Spirit Possession IV") };

        static P StageLine(PossessionTier t)
        {
            return F("주술사 {0}레벨부터 강신은 하루 {1}회, {2}라운드 지속한다.", "From shaman level {0}, possession is usable {1}/day and lasts {2} rounds.", t.Level, t.UsesPerDay, t.Rounds);
        }
    }
}
