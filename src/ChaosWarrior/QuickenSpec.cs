using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // 신속 시전 권리 버프 하나(개편 계획 2026-11-16 Task O8): 이 버프의 AutoMetamagic.MaxSpellLevel 과 블루프린트 GUID.
    public struct QuickenRight
    {
        public readonly int MaxSpellLevel;
        public readonly string BuffGuid;
        public QuickenRight(int maxSpellLevel, string buffGuid) { MaxSpellLevel = maxSpellLevel; BuffGuid = buffGuid; }
        public override string ToString() { return "cap " + MaxSpellLevel + " " + BuffGuid; }
    }

    // 워프리스트 주문 목록의 한 칸(독립 검토 R4-F1 수리): 그 목록에서의 주문 레벨(SpellLevelList.SpellLevel)과 주문 블루프린트 GUID.
    public struct SpellListEntry
    {
        public readonly int Level;
        public readonly string Guid;
        public SpellListEntry(int level, string guid) { Level = level; Guid = guid; }
        public override string ToString() { return "L" + Level + " " + Guid; }
    }

    // [순수] 분노 중 신속 시전 권리 버프의 모양(개편 설계 §12-2 「블러드레이저와 똑같이」 [소유자] · 계획 Task O8 Interfaces). 게임 형식 없음(modinfo testSources).
    // 본편 선례 = BloodragerGreaterBloodrageBuff 711192a3…(blueprints.zip Classes/Bloodrager/BloodragerRage/ 직독 2026-11-17):
    //   AutoMetamagic{m_AllowedAbilities SpellOnly, Metamagic Quicken, Abilities [], Descriptor 0, Once true, MaxSpellLevel 2, School None, CheckSpellbook true, m_Spellbook 블러드레이저 주문서, m_IncludeClasses [], m_ExcludeClasses []}.
    //   우리 권리 버프는 상한(MaxSpellLevel = ChaosRules.QuickenCaps)과 주문서(워프리스트)만 다르다 — 시험 `jbp: bloodrager precedent` 가 이 표의 값과 zip 을 대조한다.
    // 엔진 동작(Assembly-CSharp IL 직독 2026-11-17, md5 앞 12 748b3a143b60 — 정찰 §5.1 재확인):
    //   AutoMetamagic.OnEventAboutToTrigger(RuleCalculateAbilityParams) @0000~0049: ShouldApplyTo 가 참이면 메타매직을 더하고 그 주문을 기억, 아니면 기억을 지운다.
    //   OnEventAboutToTrigger(RuleCastSpell) @0000~0031: 기억한 주문이 시전되면 Once 일 때 이 부품이 달린 사실(권리 버프)을 UnitHelper.RemoveFact 로 지운다 → 분노마다 주문 1개.
    //   ShouldApplyTo @0160~018F: MaxSpellLevel > 0 일 때만 AbilityData.SpellLevel(= 주문서 레벨 + 메타매직 비용 — 슬롯 레벨) > MaxSpellLevel 이면 제외. 0 = 상한 없음(그래서 0 행은 던진다).
    //     @018F~01C1: CheckSpellbook 이면 주문이 m_Spellbook.SpellList 에 들어 있어야 한다(어느 주문서로 시전했는지는 보지 않는다). @00A7~00DF: Abilities 가 비어 있으면 주문을 가리지 않는다.
    //   독립 검토 R4-F1(2026-11-17, IL 재판독 덤프 %TEMP%\cw-r4\am.txt·lam.txt): 변형(AbilityVariants 자식)·변환(ConvertedFrom) 주문은 상한을 건너뛴다 —
    //     ShouldApplyTo @0020~006A 가 loc2 = data.ConvertedFrom?.Blueprint ?? ability.Parent ?? ability 를 구하고, @006B~0086 에서 loc2 != ability 면 ShouldApplyTo(c, loc2, null) 이 참일 때 곧장 참.
    //     그 재귀에는 data 가 없어 @016E~0172 가 SpellLevel 대신 0 을 쓴다 → 부모가 목록에 있기만 하면 상한과 무관하게 통과(본편 블러드레이저도 같다). 재귀가 거짓이면 자식 자신(data 있음)으로 다시 판정한다.
    //     막는 길(소유자 결정 2026-11-17 「본편처럼 대상 무관 + 상한 표는 지킨다」): Abilities 를 비우지 않고 「살아 있는 워프리스트 목록에서 레벨 ≤ 그 권리의 상한인 주문」으로 채운다.
    //     목록 대조 람다 <>c__DisplayClass21_0.<ShouldApplyTo>b__0 @0000~000C = BlueprintReference.Is(ability) — 정확 일치라 재귀의 부모가 목록에 없으면(상한 초과 부모) @00A7~00DA 에서 거짓.
    //     그때 자식 자신의 판정도 자식이 목록에 없어 거짓(워프리스트 목록에 변형 자식이 직접 든 칸 0 — zip 직독 2026-11-17). 결과 = 변형 주문은 부모 레벨로 상한을 본다(대상 필터는 넣지 않는다).
    //     남는 한계(README): 부모 레벨이 상한 안이면 메타매직으로 칸 레벨을 올려도 통과(재귀가 data 를 버린다) · 빌드 뒤 다른 모드가 워프리스트 목록에 더한 주문은 우리 목록에 없다(그 주문은 신속이 안 된다).
    //   AbilityData.GetDefaultActionType @009B~00A6: HasMetamagic(Quicken)(자동 메타매직 UnitPartSpecialMetamagic 포함 — HasMetamagic @0026~003E) → Swift.
    //   AbilityData.get_RuntimeActionType @0013~0063: Swift 인데 이미 신속 행동 대기(쿨다운)가 있고 표준 행동이 남았으면 Standard 로 나간다 — 이때도 시전되면 Once 가 권리를 지운다.
    public static class QuickenSpec
    {
        // 출처: 본편 BloodragerGreaterBloodrageBuff AutoMetamagic.Metamagic "Quicken"(blueprints.zip 직독 2026-11-17) — 엔진 Metamagic 이름
        public static readonly string Metamagic = "Quicken";
        // 출처: 본편 BloodragerGreaterBloodrageBuff AutoMetamagic.Once true — 시전 1회 뒤 권리 버프를 지운다(개편 설계 §12-2 「분노마다 1회」)
        public static readonly bool Once = true;
        // 출처: 본편 BloodragerGreaterBloodrageBuff AutoMetamagic.m_AllowedAbilities "SpellOnly" — 엔진 AutoMetamagic/AllowedType 이름(IsSuitableAbility @00D6~00DB: 능력 종류 Spell 만)
        public static readonly string AllowedAbilities = "SpellOnly";
        // 출처: 본편 BloodragerGreaterBloodrageBuff AutoMetamagic.CheckSpellbook true — 개편 설계 §12-2 「워프리스트 주문」(주문 목록 소속)
        public static readonly bool CheckSpellbook = true;
        // 출처: 개편 설계 §12-2 「주문서 한정」 = 워프리스트 주문서(blueprints.zip Classes/Warpriest/WarpriestSpellbook.jbp, m_SpellList = WarpriestSpelllist)
        public static readonly string SpellbookGuid = Guids.Game.WarpriestSpellbook;
        // 출처: 독립 검토 R4-F1 소유자 결정 2026-11-17 「본편처럼 대상 무관 + 상한 표는 지킨다」 — 본편 BloodragerGreaterBloodrageBuff 는 Abilities [] 지만(변형 주문이 상한을 건너뛴다 — 머리말),
        //   우리는 살아 있는 워프리스트 목록에서 레벨 ≤ 상한인 주문을 넣는다(AbilitiesFor). 대상 필터는 넣지 않는다(개편 설계 §12-2 「대상 무관」). null · 빈 목록 금지(빈 목록 = 상한 없는 변형 통과).
        public static readonly bool AbilitiesFromSpellList = true;
        // 출처: 계획 2026-11-16 Task O8 Interfaces 「BuildKit.Buff(…, hidden: false, RageSpec.Stacking)」 — 분노 버프와 같은 Replace(권리는 분노마다 하나, 겹치지 않는다)
        public static readonly string Stacking = RageSpec.Stacking;
        // 출처: 계획 2026-11-16 원장 D 「quicken.name」 · 「quicken.desc.N (N = 1..6)」(N = 그 버프의 상한)
        public static readonly string NameKey = "quicken.name";
        public static readonly string DescKeyPrefix = "quicken.desc.";

        // 권리 버프 GUID — 오름차순 상한 순서(Guids.QuickenRight1 = 상한이 가장 낮은 버프). 길이가 표의 서로 다른 상한 수와 다르면 형식 초기화가 던진다.
        static readonly string[] RightGuids = { Guids.QuickenRight1, Guids.QuickenRight2, Guids.QuickenRight3, Guids.QuickenRight4, Guids.QuickenRight5, Guids.QuickenRight6 };

        // 권리 버프 6행 = ChaosRules.QuickenCaps 의 서로 다른 상한(오름차순)과 1:1. 상한 숫자는 표에서 나온다(여기에 숫자를 적지 않는다 — `dial: core literals`).
        public static readonly IReadOnlyList<QuickenRight> Rights = BuildRights(ChaosRules.QuickenCaps, RightGuids);

        internal static QuickenRight[] BuildRights(IReadOnlyList<ChaosRules.QuickenBand> caps, IReadOnlyList<string> guids)
        {
            if (caps == null) throw new ArgumentNullException("caps");
            if (guids == null) throw new ArgumentNullException("guids");
            var distinct = caps.Select(b => b.MaxSpellLevel).Distinct().OrderBy(x => x).ToArray();
            if (distinct.Length != guids.Count)
                throw new InvalidOperationException("ChaosWarrior: quicken cap table has " + distinct.Length + " distinct caps [" + string.Join(",", distinct) + "] but there are " + guids.Count + " right buff GUIDs");
            return distinct.Select((cap, i) => new QuickenRight(cap, guids[i])).ToArray();
        }

        public static string DescKey(int maxSpellLevel) { return DescKeyPrefix + maxSpellLevel; }

        // 그 워프리스트 레벨에 줄 권리 버프(ChaosRules.QuickenCapFor 의 상한을 가진 행). 레벨 밖 · 표 오류 · 행이 정확히 하나가 아니면 던진다.
        public static string RightFor(int level)
        {
            int cap = ChaosRules.QuickenCapFor(level);
            var hit = Rights.Where(r => r.MaxSpellLevel == cap).ToArray();
            if (hit.Length != 1) throw new InvalidOperationException("ChaosWarrior: quicken cap " + cap + " (level " + level + ") has " + hit.Length + " right buff row(s)");
            return hit[0].BuffGuid;
        }

        public static bool IsRight(string guid)
        {
            if (guid == null) throw new ArgumentNullException("guid");
            return Rights.Any(r => r.BuffGuid == guid);
        }

        // 그 권리 버프의 상한. 권리 버프가 아니면 던진다(조용히 0 아님).
        public static int CapOf(string guid)
        {
            var hit = Rights.Where(r => r.BuffGuid == guid).ToArray();
            if (hit.Length != 1) throw new ArgumentException("ChaosWarrior: " + guid + " is not a quicken right buff (" + hit.Length + " rows)", "guid");
            return hit[0].MaxSpellLevel;
        }

        // 빌드 시작 때(Quicken.Build) 정적 검사. 어기면 던진다.
        public static void Validate() { ValidateRows(Rights, ChaosRules.QuickenCaps); }

        // 상한 cap 의 권리 버프 AutoMetamagic.Abilities 에 넣을 주문(독립 검토 R4-F1): 목록에서 Level ≤ cap 인 칸의 GUID, 목록 순서 그대로.
        //   Level 0(오리즌)도 넣는다 — 엔진 상한 비교 @0175~0185 가 0 ≤ cap 을 통과시키는 것과 같은 범위(빈 목록 시절에도 신속이 됐다).
        //   변형 자식은 따로 넣지 않는다 — 목록에 있는 부모가 들어가면 엔진 재귀가 그 부모로 통과시킨다(머리말). 목록·결과가 비면 던진다(빈 Abilities = 「가리지 않음」으로 조용히 열린다).
        public static string[] AbilitiesFor(int cap, IReadOnlyList<SpellListEntry> list)
        {
            if (cap <= ChaosRules.Dials.QuickenEngineNoCap) throw new ArgumentOutOfRangeException("cap", cap, "ChaosWarrior: quicken cap must be above " + ChaosRules.Dials.QuickenEngineNoCap + " (engine 'every spell level')");
            ValidateSpellList(list);
            var r = list.Where(e => e.Level <= cap).Select(e => e.Guid).ToArray();
            if (r.Length == 0) throw new InvalidOperationException("ChaosWarrior: the warpriest spell list has no spell of level <= " + cap + " (" + list.Count + " entries) — an empty Abilities list would quicken every spell");
            return r;
        }

        // 목록 판독 결과의 요구(함수 첫머리에서 — 어기면 던진다): 1칸 이상 · GUID 비지 않음 · 레벨 ≥ 0 · 같은 GUID 가 두 레벨에 없음
        //   (두 레벨에 있으면 어느 레벨로 상한을 볼지 정할 수 없다 — 본편 목록에는 없다, zip 직독 2026-11-17: 145칸 · 서로 다른 145개).
        public static void ValidateSpellList(IReadOnlyList<SpellListEntry> list)
        {
            if (list == null) throw new ArgumentNullException("list");
            if (list.Count == 0) throw new InvalidOperationException("ChaosWarrior: the warpriest spell list is empty (cannot build the quicken right lists)");
            var bad = list.Where(e => string.IsNullOrEmpty(e.Guid) || e.Level < 0).ToArray();
            if (bad.Length > 0) throw new InvalidOperationException("ChaosWarrior: unreadable warpriest spell list entries [" + string.Join(", ", bad.Select(e => e.ToString()).ToArray()) + "]");
            var twice = list.GroupBy(e => e.Guid).Where(g => g.Select(e => e.Level).Distinct().Count() > 1).Select(g => g.Key + " at levels " + string.Join("/", g.Select(e => e.Level).Distinct())).ToArray();
            if (twice.Length > 0) throw new InvalidOperationException("ChaosWarrior: spell(s) listed at more than one warpriest spell level [" + string.Join("; ", twice) + "] — cannot tell which level the quicken cap should use");
        }

        // 표(ChaosRules.ValidateQuickenCaps) · 모든 상한 > QuickenEngineNoCap(엔진에서 0 = 「모든 레벨」 — 0 이 들어가면 상한이 조용히 사라진다) · 상한·GUID 서로 다름 · GUID null 없음 · 표의 상한 집합 == 행의 상한 집합.
        internal static void ValidateRows(IReadOnlyList<QuickenRight> rows, IReadOnlyList<ChaosRules.QuickenBand> caps)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (caps == null) throw new ArgumentNullException("caps");
            if (rows.Count == 0) throw new InvalidOperationException("ChaosWarrior: no quicken right rows");
            ChaosRules.ValidateQuickenCaps(caps);
            foreach (var r in rows)
            {
                if (r.MaxSpellLevel <= ChaosRules.Dials.QuickenEngineNoCap) throw new InvalidOperationException("ChaosWarrior: quicken right " + r + " has MaxSpellLevel " + r.MaxSpellLevel + " (the engine reads <= " + ChaosRules.Dials.QuickenEngineNoCap + " as 'every spell level')");
                if (string.IsNullOrEmpty(r.BuffGuid)) throw new InvalidOperationException("ChaosWarrior: quicken right with cap " + r.MaxSpellLevel + " has no buff GUID");
            }
            if (rows.Select(r => r.MaxSpellLevel).Distinct().Count() != rows.Count) throw new InvalidOperationException("ChaosWarrior: quicken right rows repeat a cap [" + string.Join(", ", rows.Select(r => r.ToString()).ToArray()) + "]");
            if (rows.Select(r => r.BuffGuid).Distinct().Count() != rows.Count) throw new InvalidOperationException("ChaosWarrior: quicken right rows repeat a buff GUID");
            var tableCaps = new HashSet<int>(caps.Select(b => b.MaxSpellLevel));
            if (!tableCaps.SetEquals(rows.Select(r => r.MaxSpellLevel)))
                throw new InvalidOperationException("ChaosWarrior: quicken cap table caps [" + string.Join(",", tableCaps.OrderBy(x => x)) + "] != right buff caps [" + string.Join(",", rows.Select(r => r.MaxSpellLevel)) + "]");
        }
    }
}
