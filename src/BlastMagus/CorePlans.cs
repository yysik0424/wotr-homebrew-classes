using System;
using System.Collections.Generic;

namespace BlastMagus
{
     
    // T1 = 비전 거르기(ArcanaFilter), T3 = 폭발 판단(StrikePlanner·StrikeExecutor·PhysicalMagic). 게임 쪽(PoolArcana·BlastStrikeOnHit)은 이 결과를 그대로 따른다 — 판단 논리의 증거는 시험의 exec: 항목이다.

    // 설계 §4.6-2 (R2): 무기 강화 능력에만 의미가 있는 비전 4종. 블라스트 풀에는 무기 강화 능력(ArcaneWeaponSwitchAbility)이 없으므로(§4.6-1)
    // 고르면 효과 0 인 헛선택이 된다(정찰 11-08 §7(b)) → 우리 비전 선택지에서 뺀다. 시험이 이 집합을 설계의 4 GUID 리터럴·blueprints.zip 과 대조한다.
    public static class ArcanaPlan
    {
        // 매번 새 배열(호출자가 원장을 바꾸지 못하게).
        public static string[] Excluded
        {
            get { return new[] { Guids.Game.BaneBladeFeature, Guids.Game.DevotedBladeFeature, Guids.Game.EnduringBladeFeature, Guids.Game.GhostBladeFeature }; }
        }
    }

    public static class ArcanaFilter
    {
        // GUID 문자열 형식 = BlueprintGuid.ToString() = System.Guid "N"(32자 소문자 16진; Assembly-CSharp BlueprintGuid::ToString IL_001B ldstr "N",
        // 빈 GUID 는 "" — IL_0026). Guids.cs 상수도 같은 형식. 형식이 다르면(대문자·하이픈) 같은 GUID 가 조용히 「다름」이 되므로 던진다.
        const int GuidLength = 32;

        // vanilla 에서 excluded 를 뺀 목록(vanilla 순서 보존)과, excluded 중 vanilla 에 없던 것(excluded 순서).
        // 중복 입력은 vanilla·excluded 어느 쪽이든 던진다(같은 비전이 두 번 = 다른 모드가 목록을 망가뜨린 것 — 조용히 하나로 합치지 않는다).
        public static (string[] kept, string[] missingExcluded) Filter(string[] vanilla, string[] excluded)
        {
            if (vanilla == null) throw new ArgumentNullException("vanilla");
            if (excluded == null) throw new ArgumentNullException("excluded");
            CheckList(vanilla, "vanilla");
            CheckList(excluded, "excluded");

            var kept = new List<string>();
            foreach (var v in vanilla)
            {
                bool hit = false;
                foreach (var x in excluded)
                    if (v == x) { hit = true; break; }
                if (!hit) kept.Add(v);
            }
            var missing = new List<string>();
            foreach (var x in excluded)
            {
                bool found = false;
                foreach (var v in vanilla)
                    if (v == x) { found = true; break; }
                if (!found) missing.Add(x);
            }
            // 자기 검사: 빠진 수 == 본편에 있던 제외 대상 수.
            if (vanilla.Length - kept.Count != excluded.Length - missing.Count)
                throw new InvalidOperationException("BlastMagus: ArcanaFilter removed " + (vanilla.Length - kept.Count) + " but " + (excluded.Length - missing.Count) + " excluded entries are in vanilla");
            return (kept.ToArray(), missing.ToArray());
        }

        static void CheckList(string[] list, string what)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < list.Length; i++)
            {
                var g = list[i];
                if (g == null) throw new ArgumentException("BlastMagus: ArcanaFilter " + what + "[" + i + "] is null", what);
                if (!IsGuidN(g)) throw new ArgumentException("BlastMagus: ArcanaFilter " + what + "[" + i + "] = '" + g + "' is not a 32-char lowercase hex GUID", what);
                if (!seen.Add(g)) throw new ArgumentException("BlastMagus: ArcanaFilter " + what + " lists " + g + " twice", what);
            }
        }

        static bool IsGuidN(string g)
        {
            if (g.Length != GuidLength) return false; // 형식 판정 결과(결측 아님): 호출자가 던진다
            foreach (var c in g)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
    }

     
    // 게임 쪽 BlastStrikeOnHit 은 IStrikeEffects 구현(사실 읽기·SR 굴림·피해·소모)만 제공하고 분기는 하지 않는다. 분기·순서·수치는 여기서 정한다.

     
    public enum SrOutcome { NotRolled, Allowed, Resisted }

    // 원소의 폭발 관련 성질 = 원소 표 행의 Kind·SpellResistance(설계 §4.4-1 「폭발 SR」 행: 원소 모델 SR 열).
    public struct StrikeElement
    {
        public readonly DamageKind Kind;
        public readonly bool SpellResistance;
        public StrikeElement(DamageKind kind, bool spellResistance) { Kind = kind; SpellResistance = spellResistance; }
    }

    // 피해 수치 입력(계획 T3 StrikeNumbers). 진행·비저항일 때만 읽는다(Resolve 가 지연 호출 — R-PF8: 빗나감·저항에 랭크 판독·예외가 새로 생기지 않게).
    public struct StrikeNumbers
    {
        public readonly int Rank, IntMod;
        public readonly bool HasComposite, CritConfirmed;
        public StrikeNumbers(int rank, bool hasComposite, int intMod, bool critConfirmed) { Rank = rank; HasComposite = hasComposite; IntMod = intMod; CritConfirmed = critConfirmed; }
    }

    // Gate 입력(계획 T3 StrikeInput: IsHit, WeaponOk, Enabled, Pending, Kind·SpellResistance). R-PF8: 오늘의 판정 순서를 지키려고 값이 아니라 읽기 함수로 받는다 —
    // Gate 가 IsHit → WeaponOk → Enabled → Pending → Element 순서로 읽고, 앞이 거짓이면 뒤를 읽지 않는다(빗나감에 IsEnabled 로그·ElementOf 예외가 새로 생기지 않는다).
    public sealed class StrikeInput
    {
        public readonly Func<bool> IsHit, WeaponOk, Enabled, Pending;
        public readonly Func<StrikeElement> Element;
        public StrikeInput(Func<bool> isHit, Func<bool> weaponOk, Func<bool> enabled, Func<bool> pending, Func<StrikeElement> element)
        {
            if (isHit == null) throw new ArgumentNullException("isHit");
            if (weaponOk == null) throw new ArgumentNullException("weaponOk");
            if (enabled == null) throw new ArgumentNullException("enabled");
            if (pending == null) throw new ArgumentNullException("pending");
            if (element == null) throw new ArgumentNullException("element");
            IsHit = isHit; WeaponOk = weaponOk; Enabled = enabled; Pending = pending; Element = element;
        }
    }

    // Gate 결과(계획 T3 Gate { Proceed, ClearPending, RollSr }). Resolve 가 피해 식을 고르려고 Kind 도 싣는다 — 진행하지 않는 Gate 의 Kind 는 판정 불가라 읽으면 던진다.
    public sealed class StrikeGate
    {
        public readonly bool Proceed, ClearPending, RollSr;
        readonly DamageKind kind;
        StrikeGate(bool proceed, bool clearPending, bool rollSr, DamageKind kind) { Proceed = proceed; ClearPending = clearPending; RollSr = rollSr; this.kind = kind; }
        internal static StrikeGate Stop() { return new StrikeGate(false, false, false, default(DamageKind)); }
        internal static StrikeGate Go(StrikeElement e) { return new StrikeGate(true, true, e.SpellResistance, e.Kind); }
        public DamageKind Kind
        {
            get
            {
                if (!Proceed) throw new InvalidOperationException("BlastMagus: StrikeGate.Kind read on a gate that does not proceed (element was never read)");
                return kind;
            }
        }
    }

    // Resolve 결과(계획 T3 StrikePlan { Fire, Consume, Dice, Bonus, CritMultiplier, ApplyMagic } + Kind). 쏘지 않는 계획의 수치는 없다 — 읽으면 던진다.
    public sealed class StrikePlan
    {
        public readonly bool Fire, Consume;
        readonly DamageKind kind;
        readonly int dice, bonus, critMultiplier;
        readonly bool applyMagic;
        StrikePlan(bool fire, bool consume, DamageKind kind, int dice, int bonus, int critMultiplier, bool applyMagic)
        { Fire = fire; Consume = consume; this.kind = kind; this.dice = dice; this.bonus = bonus; this.critMultiplier = critMultiplier; this.applyMagic = applyMagic; }
        internal static StrikePlan Resisted() { return new StrikePlan(false, true, default(DamageKind), 0, 0, 0, false); }
        internal static StrikePlan Fired(DamageKind kind, int dice, int bonus, int crit, bool magic) { return new StrikePlan(true, true, kind, dice, bonus, crit, magic); }
        void Need(string what) { if (!Fire) throw new InvalidOperationException("BlastMagus: StrikePlan." + what + " read on a plan that does not fire"); }
        public DamageKind Kind { get { Need("Kind"); return kind; } }
        public int Dice { get { Need("Dice"); return dice; } }
        public int Bonus { get { Need("Bonus"); return bonus; } }
        public int CritMultiplier { get { Need("CritMultiplier"); return critMultiplier; } }
        public bool ApplyMagic { get { Need("ApplyMagic"); return applyMagic; } }
    }

    public static class StrikePlanner
    {
        // 치명타가 아닐 때의 배수 = 1: 엔진 CriticalModifier 를 비워 둔다(지금 BlastStrike.Deal 의 `if (crit)` 와 같다). 치명타 = BlastRules.BlastStrikeCritMultiplier(설계 §4.2).
        public const int NoCritMultiplier = 1;

        // 설계 §4.9 ①: 빗나감·무기 불가·꺼짐·Pending 없음 → 진행 안 함(Pending 유지). 진행이면 Pending 해제, SR 원소면 SR 굴림.
        public static StrikeGate Gate(StrikeInput input)
        {
            if (input == null) throw new ArgumentNullException("input");
            if (!input.IsHit()) return StrikeGate.Stop();    // legitimate no-op: 빗나감 — 버프는 그 라운드 동안 남는다(설계 §4.2)
            if (!input.WeaponOk()) return StrikeGate.Stop(); // legitimate no-op: 근접 제작 무기 아님(설계 §4.2·§4.4)
            if (!input.Enabled()) return StrikeGate.Stop();  // legitimate no-op: 블라스트 꺼짐/알 수 없음(BlastState 가 이미 로그)
            if (!input.Pending()) return StrikeGate.Stop();  // legitimate no-op: 이번 활성은 이미 발동(전력 공격 두 번째 명중)
            var e = input.Element();
            if (!Enum.IsDefined(typeof(DamageKind), e.Kind)) throw new ArgumentOutOfRangeException("input", e.Kind, "BlastMagus: StrikePlanner.Gate element kind undefined");
            return StrikeGate.Go(e);
        }

        // 설계 §4.9 ③: SR 결과를 받아 피해 발행 여부·수치. Resisted → 피해 0·소모(본편 DoApplyEffect IL_00B9~00CA 가 저항이면 효과 적용을 통째로 건너뛴다).
        // numbers 는 쏠 때만 부른다(저항 경로에 랭크 판독 예외가 새로 생기지 않게 — 지금 코드도 저항이면 Deal 에 들어가지 않는다).
        public static StrikePlan Resolve(StrikeGate gate, SrOutcome sr, Func<StrikeNumbers> numbers)
        {
            if (gate == null) throw new ArgumentNullException("gate");
            if (numbers == null) throw new ArgumentNullException("numbers");
            if (!gate.Proceed) throw new InvalidOperationException("BlastMagus: StrikePlanner.Resolve on a gate that does not proceed");
            if (!Enum.IsDefined(typeof(SrOutcome), sr)) throw new ArgumentOutOfRangeException("sr", sr, "BlastMagus: SrOutcome undefined");
            if (gate.RollSr && sr == SrOutcome.NotRolled) throw new InvalidOperationException("BlastMagus: SR element but SR outcome NotRolled (not rolled is not 'allowed')");
            if (!gate.RollSr && sr != SrOutcome.NotRolled) throw new InvalidOperationException("BlastMagus: element without SR but SR outcome " + sr);
            if (sr == SrOutcome.Resisted) return StrikePlan.Resisted();
            var n = numbers();
            var kind = gate.Kind;
            int dice = BlastRules.StrikeDice(n.Rank, n.HasComposite);
            int bonus = BlastRules.StrikeBonus(kind, dice, n.IntMod);
            int crit = n.CritConfirmed ? BlastRules.BlastStrikeCritMultiplier : NoCritMultiplier;
            // 설계 §4.4-1 「폭발 DR」: 물리면 본편 물리 블라스트의 「마법」 속성을 직접 붙인다(PhysicalMagic).
            return StrikePlan.Fired(kind, dice, bonus, crit, kind == DamageKind.Physical);
        }
    }

    // 게임 쪽 효과(BlastStrikeOnHit 이 호출마다 새로 만드는 구현). 사실 읽기 6개 + 효과 4개. 분기는 하지 않는다.
    public interface IStrikeEffects
    {
        bool IsHit();
        bool WeaponOk();
        bool Enabled();
        bool Pending();
        StrikeElement Element();
        StrikeNumbers Numbers();
        void ClearPending();
        SrOutcome RollSr();
        void Deal(StrikePlan plan);
        void Consume();
    }

    public static class StrikeExecutor
    {
         
        public static void Run(IStrikeEffects fx)
        {
            if (fx == null) throw new ArgumentNullException("fx");
            var gate = StrikePlanner.Gate(new StrikeInput(fx.IsHit, fx.WeaponOk, fx.Enabled, fx.Pending, fx.Element));
            if (!gate.Proceed) return; // legitimate no-op: 진행하지 않는 명중(빗나감·무기·꺼짐·이미 발동) — Pending 은 그대로
            if (gate.ClearPending) fx.ClearPending();
            var sr = gate.RollSr ? fx.RollSr() : SrOutcome.NotRolled;
            var plan = StrikePlanner.Resolve(gate, sr, fx.Numbers);
            if (plan.Fire) fx.Deal(plan);
            if (plan.Consume) fx.Consume();
        }
    }

    // 엔진 AddOutgoingPhysicalDamageProperty.ApplyProperties 의 AddMagic 분기(Assembly-CSharp IL, dnlib 2026-11-08, T3 보고서에 오프셋별 대조):
    //   IL_0005 ldfld AddMagic / IL_000A brfalse.s IL_002A → IL_000D get_Enchantment / IL_0012 ldc.i4.1 / IL_0013 bge.s IL_002A(1 이상이면 아무것도 안 함)
    //   → IL_0016 ldc.i4.1 / IL_0017 set_Enchantment(1) → IL_001E get_EnchantmentTotal / IL_0023 ldc.i4.1 / IL_0024 add / IL_0025 set_EnchantmentTotal(+1).
    public static class PhysicalMagic
    {
        // IL_0012·IL_0016·IL_0023 의 상수 1.
        const int MagicEnchantment = 1;

        // 복제의 전제 = 본편 원본 컴포넌트(KineticBlastFeature 의 KineticBlastPhysical 용 AddOutgoingPhysicalDamageProperty)가 「무조건 마법」이라는 것.
        // 원장 = 이 목록 하나(C4): 게임 Build 되읽기(BlastStrike.CheckPhysicalBlastDr)와 jbp 시험이 모두 SourceMismatches 로 대조한다.
        // 값 출처 = KineticBlastFeature.jbp 직독(2026-11-08) + 엔진 OnEventAboutToTrigger 적용 조건 IL_0035~0113(NaturalAttacks·NotUnarmed·CheckWeaponType·CheckRange·AgainstFactOwner)
        // 과 ApplyProperties 의 Add* 분기(IL_0005~010B). 하나라도 다르면 본편 마법이 조건부/다른 속성이 되어 우리 무조건 복제가 조용히 어긋난다 → 빌드 실패.
        public static (string name, bool value)[] SourceFlags
        {
            get
            {
                return new[]
                {
                    ("AddMagic", true), ("AffectAnyPhysicalDamage", true), ("CheckWeaponType", true),
                    ("AddMaterial", false), ("AddForm", false), ("AddAlignment", false), ("AddReality", false),
                    ("NaturalAttacks", false), ("NotUnarmed", false), ("CheckRange", false), ("AgainstFactOwner", false),
                };
            }
        }

        // 실제 플래그(이름, 값) 목록 vs SourceFlags: 값이 다른 것·빠진 것·남는 것·중복을 전부 문장으로. 빈 배열 = 일치.
        public static string[] SourceMismatches((string name, bool value)[] actual)
        {
            if (actual == null) throw new ArgumentNullException("actual");
            var bad = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var a in actual)
            {
                if (a.name == null) { bad.Add("null flag name"); continue; }
                if (!seen.Add(a.name)) bad.Add(a.name + " listed twice");
            }
            foreach (var e in SourceFlags)
            {
                int n = 0;
                foreach (var a in actual)
                    if (a.name == e.name) { n++; if (a.value != e.value) bad.Add(e.name + " = " + a.value + ", expected " + e.value); }
                if (n == 0) bad.Add(e.name + " missing");
            }
            foreach (var a in actual)
            {
                bool known = false;
                foreach (var e in SourceFlags) if (e.name == a.name) known = true;
                if (a.name != null && !known) bad.Add(a.name + " is not a source flag");
            }
            return bad.ToArray();
        }

        public static (int enchantment, int enchantmentTotal) Apply(int enchantment, int enchantmentTotal)
        {
            if (enchantment >= MagicEnchantment) return (enchantment, enchantmentTotal); // IL_0013 bge.s: 이미 마법 이상이면 그대로
            return (MagicEnchantment, enchantmentTotal + MagicEnchantment);
        }
    }

    // 원소 판정(설계 §4.8 「원소 판별」 — 폭발 경로 BlastBlueprints.ElementOf 가 쓴다). 2026-11-15: 에너지 주입은 원소마다 자식 토글·버프로 바뀌어(Energize.cs)
    // 원소를 판별하지 않는다(예전 JudgeElement·ElementJudge 제거). 상태 3개 중 「판정 불가」를 명시 값으로 둔다(C1): 원소 수 != 1 → Undetermined.
    // Undetermined 의 Element 를 읽으면 던진다(조용히 하나를 고르지 않는다). ElementOf 는 판정 불가면 던진다(명중 시에만 — 핫패스 아님).
    public enum ElementVerdictState { Determined, Undetermined }

    public sealed class ElementVerdict
    {
        // 판정됨 = 정확히 1개(설계 §4.8: 원소 하나 = 블라스트 하나).
        const int RequiredCount = 1;

        public readonly ElementVerdictState State;
        readonly Element[] have;
        ElementVerdict(ElementVerdictState state, Element[] have) { State = state; this.have = have; }

        public bool IsDetermined { get { return State == ElementVerdictState.Determined; } }

        // 보유 원소(판별표 순서) 사본.
        public Element[] Have { get { return (Element[])have.Clone(); } }

        public Element Element
        {
            get
            {
                if (State != ElementVerdictState.Determined)
                    throw new InvalidOperationException("BlastMagus: ElementVerdict.Element read on an undetermined verdict (" + have.Length + " element blast bases: " + string.Join(",", have) + ")");
                return have[0];
            }
        }

        public static ElementVerdict Of(Element[] have)
        {
            if (have == null) throw new ArgumentNullException("have");
            var seen = new HashSet<Element>();
            foreach (var e in have)
            {
                if (!Enum.IsDefined(typeof(Element), e)) throw new ArgumentOutOfRangeException("have", e, "BlastMagus: ElementVerdict.Of undefined element");
                if (!seen.Add(e)) throw new ArgumentException("BlastMagus: ElementVerdict.Of duplicate element " + e + " (the base table lists each element once)", "have");
            }
            var copy = (Element[])have.Clone();
            return new ElementVerdict(copy.Length == RequiredCount ? ElementVerdictState.Determined : ElementVerdictState.Undetermined, copy);
        }
    }
}
