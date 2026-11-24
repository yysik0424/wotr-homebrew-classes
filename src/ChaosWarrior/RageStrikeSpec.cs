using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // [순수] 분노 중 자연 무기 공격의 무기 취급 — 소유자 결정 2026-11-16: 몽크 기 일격 구조(3·7·10), 10레벨은 혼돈·악, 16레벨 단계 없음.
    //   혼돈의 분노가 켜져 있는 동안 자연 무기 공격은 워프리스트 레벨 3 부터 「마법」, 7 부터 「냉철·은」, 10 부터 「혼돈·악 성향」 무기로 취급된다(누적).
    //   강화 보너스(명중·피해 +N) 없음 · 아다만틴·선·질서 없음 · DR x/— 는 대상 아님(무시 계열 부품 금지) · 제작 무기(주 손·보조 손)·맨손(몽크 주먹 포함)에는 붙지 않는다.
    //   성향 속성은 캐릭터의 실제 성향과 무관하게 붙는다(MyAlignment 끔 — 고정값 혼돈·악).
    // 단계 표는 이 파일의 TierRows(속성) + ChaosRules.Levels.RageStrikeLevels(레벨 숫자 — 수치 원장 규약상 숫자는 ChaosRules 에만) 한 벌이고, Tiers 가 둘을 묶는다(길이가 다르면 던진다).
    // Rage 빌더·Rage.VerifyNow 되읽기·시험·설명문 대조가 전부 Tiers 에서 파생된다(C3·C4).
    // 구조: 단계마다 숨김 버프 하나(본편 AddOutgoingPhysicalDamageProperty 1개). 분노 버프의 Activated 가 「워프리스트 레벨 ≥ 단계 레벨」(본편 ContextConditionCharacterClass)일 때
    //   그 버프를 분노의 자식으로 영구 부여한다(본편 Conditional + ContextActionApplyBuff{Permanent, AsChild} — 선례 StandartRageBuff da8ce41a…, zip). 분노가 끝나면 함께 풀린다(Buff.OnRemove @0047~0090).
    // 수용한 귀결(소유자 결정 2026-11-16 — README 「알려진 한계/주의」와 설계 문서 §12 에 같은 내용):
    //   (1) 「마법」은 강화 합계(EnchantmentTotal)를 +1 올린다(ApplyProperties @0004~0025: Enchantment < 1 일 때만 Enchantment = 1, EnchantmentTotal += 1).
    //       강화 0 이면서 합계가 정확히 4 인 자연 무기는 합계 5 가 되어 선·질서 DR 까지 뚫고(CheckBypassedByAlignment: 합계 ≥ 5), 합계 5 면 6 이 되어 에픽 판정(IsEpic: 합계 > 5)도 켜진다.
    //   (2) 속성은 DR 판정 밖에서도 읽힌다: 무형 대상에 대한 물리 피해 감쇄(AddIncorporealDamageDivisor · GhostCriticalAndPrecisionImmunity), 마법·냉철·은·혼돈·악 무기로 억제되는 재생(AddEffectRegeneration).
    //   (3) 「악」: DR/악(주로 천상 계열 적)을 뚫고 악 성향 무기로 억제되는 재생을 끈다 — 캐릭터가 혼돈 중립이어도 붙는다.
    // 전부 static readonly(const 는 소비처 IL 에 접힌다). 엔진 열거값은 이름 문자열 — 빌더가 BuildKit.FlagsOrNone 으로 옮긴다(없는 이름이면 던진다).
    // 엔진 근거(IL 직독 2026-11-16, 시험 `strike:` 가 잠근다): AddOutgoingPhysicalDamageProperty.OnEventAboutToTrigger(RulePrepareDamage) · ApplyProperties,
    //   AddDamageResistancePhysical.Hit · CheckBypassedByMaterial · CheckBypassedByAlignment, ContextConditionCharacterClass.CheckCondition, ContextActionApplyBuff.RunAction.
    public struct StrikeTier
    {
        public readonly int Level;            // 이 워프리스트 레벨부터(ChaosRules.Levels.RageStrikeLevels)
        public readonly string Id, BuffGuid;  // 단계 이름(블루프린트 이름 접미) · 그 단계의 숨김 버프
        public readonly bool Magic;
        public readonly string[] Materials, Alignments;   // 엔진 PhysicalDamageMaterial · DamageAlignment 이름. 없으면 빈 배열(null 아님)
        public StrikeTier(int level, string id, string buffGuid, bool magic, string[] materials, string[] alignments)
        {
            Level = level; Id = id; BuffGuid = buffGuid; Magic = magic; Materials = materials; Alignments = alignments;
        }
        public bool HasMaterials { get { return Materials.Length != 0; } }
        public bool HasAlignments { get { return Alignments.Length != 0; } }
        public override string ToString() { return "L" + Level + ":" + Id; }
    }

    public static class RageStrikeSpec
    {
        // 출처: 본편 부품 Kingmaker.UnitLogic.FactLogic.AddOutgoingPhysicalDamageProperty(버프 선례 BeastTamerInspireFerocity10EffectBuff f36ed941…, ParagonShape 3ac26dec…)
        public static readonly string Component = "AddOutgoingPhysicalDamageProperty";
        // 출처: 소유자 결정 2026-11-16 「자연 무기에만」 — OnEventAboutToTrigger @0035~0056: 켜면 (무기 == null ? 거짓 : Type.IsNatural). 제작 무기·무기 없는 피해(주문·가시 피부 반사)는 여기서 걸러진다
        public static readonly bool NaturalAttacks = true;
        // 출처: 소유자 결정 2026-11-16 「맨손 공격에는 붙지 않는다」 — 본편 Unarmed fcca8e6b… · EmptyHand a8b38f6b… 는 m_IsNatural true 이자 m_IsUnarmed true(zip) → @0057~0083 !IsUnarmed 가 필요
        public static readonly bool NotUnarmed = true;
        // 출처: OnEventAboutToTrigger @0124~017E — 끄면 무기 피해 청크(DamageBundle.WeaponDamage)만, 켜면 묶음의 모든 물리 청크. 자연 무기의 무기 피해에만 붙이므로 끈다
        public static readonly bool AffectAnyPhysicalDamage = false;
        // 출처: 소유자 결정 2026-11-16 「10레벨은 혼돈·악」 — 고정 성향. ApplyProperties @005D~00EB: 켜면 소유자의 실제 성향 성분을 읽고 Alignment 필드를 무시한다 → 끈다(성향과 무관하게 혼돈·악)
        public static readonly bool MyAlignment = false;
        // 출처: 본편 StandartRageBuff da8ce41a…(zip) Activated 의 Conditional → ContextActionApplyBuff{Permanent true, AsChild true, ToCaster false} — 분노의 자식으로 영구, 대상 = 분노 소유자
        public static readonly bool GatePermanent = true;
        // 출처: 같은 선례 AsChild true — ContextActionApplyBuff.RunAction @00DF~0117: 부모 버프(Buff.Data)가 같은 소유자면 StoreFact → 분노 종료 때 함께 제거
        public static readonly bool GateAsChild = true;
        // 출처: 같은 선례 ToCaster false — GetBuffTarget @0000~0013: 대상(= Activated 문맥의 분노 소유자)에게 건다
        public static readonly bool GateToCaster = false;
        // 출처: ContextConditionCharacterClass.CheckCondition @0000~001B — 켜면 문맥 시전자(분노를 켠 본인)의 클래스 레벨을 본다(변이 추첨 어댑터가 쓰는 MaybeCaster 와 같은 대상)
        public static readonly bool GateCheckCaster = true;
        // 출처: [작성자] 해제 주문이 단계 버프만 지워 분노는 남고 속성만 조용히 사라지는 경로를 막는다(RunAction @00CB~00DA IsNotDispelable |= 값)
        public static readonly bool GateNotDispelable = true;

        // 속성 행(레벨 숫자 없음 — 순서 = ChaosRules.Levels.RageStrikeLevels 의 순서).
        sealed class Row
        {
            public readonly string Id, BuffGuid; public readonly bool Magic; public readonly string[] Materials, Alignments;
            public Row(string id, string buffGuid, bool magic, string[] materials, string[] alignments) { Id = id; BuffGuid = buffGuid; Magic = magic; Materials = materials; Alignments = alignments; }
        }

        static readonly string[] None = new string[0];

        // 출처: 소유자 결정 2026-11-16: 몽크 기 일격 구조(3·7·10), 10레벨은 혼돈·악, 16레벨 단계 없음 — 마법 → 냉철·은 → 혼돈·악. 아다만틴·선·질서는 주지 않는다
        static readonly Row[] TierRows =
        {
            new Row("Magic", Guids.RageStrikeMagicBuff, true, None, None),
            new Row("ColdIronSilver", Guids.RageStrikeMaterialBuff, false, new[] { "ColdIron", "Silver" }, None),
            new Row("ChaoticEvil", Guids.RageStrikeAlignmentBuff, false, None, new[] { "Chaotic", "Evil" }),
        };

        // 단계 표(레벨 → 그 레벨에 추가되는 속성). 빌더·되읽기·시험·설명문 대조의 단일 출처.
        public static readonly IReadOnlyList<StrikeTier> Tiers = BuildTiers();

        static StrikeTier[] BuildTiers()
        {
            var levels = ChaosRules.Levels.RageStrikeLevels;
            if (levels == null || levels.Length != TierRows.Length)
                throw new InvalidOperationException("ChaosWarrior: RageStrikeLevels has " + (levels == null ? "no" : levels.Length.ToString()) + " levels but RageStrikeSpec has " + TierRows.Length + " tier rows");
            var l = new List<StrikeTier>();
            for (int i = 0; i < TierRows.Length; i++) l.Add(new StrikeTier(levels[i], TierRows[i].Id, TierRows[i].BuffGuid, TierRows[i].Magic, TierRows[i].Materials, TierRows[i].Alignments));
            return l.ToArray();
        }

        // 조용한 변형 금지: 표의 모양이 어긋나면 던진다. 빌더가 부품을 만들기 전에 부른다.
        public static void Validate()
        {
            if (!NaturalAttacks) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec.NaturalAttacks must be true (without it the properties reach manufactured weapons and NotUnarmed is ignored by the engine)");
            if (!NotUnarmed) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec.NotUnarmed must be true (vanilla unarmed weapon types are IsNatural)");
            if (AffectAnyPhysicalDamage) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec.AffectAnyPhysicalDamage must be false (weapon damage chunk only)");
            if (MyAlignment) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec.MyAlignment must be false (the engine would ignore the fixed alignment list and read the owner's alignment)");
            if (!GatePermanent || !GateAsChild || GateToCaster || !GateCheckCaster)
                throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec gate flags must be Permanent + AsChild + target the rage owner + check the caster's class level (otherwise the tier buff outlives the rage or lands on the wrong unit)");
            if (Tiers.Count == 0) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec.Tiers is empty (owner ruling 2026-11-16 needs the tier table)");
            int prev = ChaosRules.Levels.MinLevel - 1;
            foreach (var t in Tiers)
            {
                ChaosRules.RequireLevel(t.Level);
                if (t.Level <= prev) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec tier " + t + " is not above the previous tier level " + prev + " (levels must ascend strictly)");
                prev = t.Level;
                if (string.IsNullOrEmpty(t.Id) || string.IsNullOrEmpty(t.BuffGuid)) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec tier at level " + t.Level + " has no id or buff GUID");
                if (t.Materials == null || t.Alignments == null) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec tier " + t + " has a null name list (use an empty array for none)");
                RequireNames(t.Materials, t + " Materials");
                RequireNames(t.Alignments, t + " Alignments");
                if (!t.Magic && !t.HasMaterials && !t.HasAlignments) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec tier " + t + " adds nothing");
            }
            if (Tiers.Select(t => t.Id).Distinct().Count() != Tiers.Count || Tiers.Select(t => t.BuffGuid).Distinct().Count() != Tiers.Count)
                throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec tiers repeat an id or a buff GUID");
            // 같은 속성을 두 단계가 주면 표가 틀린 것이다(엔진은 무해하게 겹치지만 — 마법 두 번째는 Enchantment >= 1 이라 건너뛴다 — 단계 표의 뜻이 흐려진다).
            var all = Properties(ChaosRules.Levels.MaxLevel, false);
            if (all.Distinct().Count() != all.Count) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec tiers grant the same property twice [" + string.Join(", ", all.ToArray()) + "]");
        }

        static void RequireNames(string[] names, string what)
        {
            if (names.Any(string.IsNullOrEmpty) || names.Distinct().Count() != names.Length) throw new InvalidOperationException("ChaosWarrior: RageStrikeSpec " + what + " has a blank or repeated name");
        }





        // 그 레벨에서 자연 무기가 갖는 속성 이름("Magic" + 재질 + 성향, 단계 순서 그대로).
        public static List<string> PropertiesAt(int level) { return Properties(level, true); }

        static List<string> Properties(int level, bool requireLevel)
        {
            if (requireLevel) ChaosRules.RequireLevel(level);
            var l = new List<string>();
            foreach (var t in Tiers.Where(x => x.Level <= level))
            {
                if (t.Magic) l.Add("Magic");
                l.AddRange(t.Materials);
                l.AddRange(t.Alignments);
            }
            return l;
        }

        // 부품 한 개의 되읽기 줄. 이름 목록은 서수 정렬 + '+', 없으면 '-'(없는 것과 있는 것이 같은 줄이 되지 않는다). 사양 쪽(Expected)과 실제 부품 쪽(Rage.DescribeStrike)이 같은 함수를 쓴다.
        public static string Line(bool natural, bool notUnarmed, bool any, bool magic, IEnumerable<string> materials, IEnumerable<string> alignments,
            bool myAlignment, bool form, bool reality, bool weaponType, bool range, bool againstFact)
        {
            return Component + "|natural=" + natural + "|notUnarmed=" + notUnarmed + "|any=" + any + "|magic=" + magic + "|material=" + Names(materials, "materials") + "|alignment=" + Names(alignments, "alignments")
                + "|my=" + myAlignment + "|form=" + form + "|reality=" + reality + "|weaponType=" + weaponType + "|range=" + range + "|againstFact=" + againstFact;
        }

        static string Names(IEnumerable<string> names, string what)
        {
            if (names == null) throw new ArgumentNullException(what);
            var l = names.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            return l.Length == 0 ? "-" : string.Join("+", l);
        }

        // 그 단계의 버프에 있어야 하는 부품 줄(버프마다 정확히 1개). 형태·실재·무기 종류·거리·대상 사실 조건은 쓰지 않는다.
        public static string Expected(StrikeTier t)
        {
            return Line(NaturalAttacks, NotUnarmed, AffectAnyPhysicalDamage, t.Magic, t.Materials, t.Alignments, MyAlignment, false, false, false, false, false);
        }

        // 분노 버프 Activated 의 단계 게이트 한 줄(조건 + 부여). 사양 쪽(ExpectedGate)과 실제 행동 쪽(Rage.DescribeGate)이 같은 함수를 쓴다.
        public static string GateLine(string classGuid, int minLevel, bool checkCaster, bool not, string buffGuid, bool permanent, bool asChild, bool toCaster, bool notDispelable, int ifFalseActions)
        {
            if (string.IsNullOrEmpty(classGuid)) throw new ArgumentException("ChaosWarrior: strike gate line without a class GUID", "classGuid");
            if (string.IsNullOrEmpty(buffGuid)) throw new ArgumentException("ChaosWarrior: strike gate line without a buff GUID", "buffGuid");
            return "gate|class=" + classGuid + "|min=" + minLevel + "|caster=" + checkCaster + "|not=" + not + "|buff=" + buffGuid + "|permanent=" + permanent + "|child=" + asChild + "|toCaster=" + toCaster
                + "|notDispelable=" + notDispelable + "|else=" + ifFalseActions;
        }

        public static string ExpectedGate(StrikeTier t)
        {
            return GateLine(Guids.Game.WarpriestClass, t.Level, GateCheckCaster, false, t.BuffGuid, GatePermanent, GateAsChild, GateToCaster, GateNotDispelable, 0);
        }

        public static bool IsStrikeBuff(string guid)
        {
            if (guid == null) throw new ArgumentNullException("guid");
            return Tiers.Any(t => t.BuffGuid == guid);
        }
    }
}
