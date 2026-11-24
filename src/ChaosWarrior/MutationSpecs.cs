using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
     
     
    public static class MutationSpecs
    {
        public const string ElementalShellId = "ElementalShell";
        public const string PainAmplifyId = "PainAmplify";
        // 개편 O6: 고통 공명(양면 — 고통 겹 추가 쪽은 효과 행이 Core, 고통 핵심이 이 표식의 순위를 읽는다: O7 계획 Interfaces).
        public const string PainResonanceId = "PainResonance";
        // 개편 O7: 신경독침 — 고통 핵심이 이 변이의 우리 복제 무기 GUID(NaturalWeaponPlan.GuidOf)로 명중 무기를 식별한다.
        public const string NeuroStingerId = "NeuroStinger";

        static readonly Dictionary<string, string> markers = new Dictionary<string, string>
        {
            ["Enlarge"] = Guids.MarkEnlarge, ["ToughHide"] = Guids.MarkToughHide, ["Regeneration"] = Guids.MarkRegeneration, ["Swiftness"] = Guids.MarkSwiftness,
            ["ThornSkin"] = Guids.MarkThornSkin, ["ChaosBlood"] = Guids.MarkChaosBlood, ["ElementalShell"] = Guids.MarkElementalShell, ["PainAmplify"] = Guids.MarkPainAmplify,
            ["ExtraArm"] = Guids.MarkExtraArm, ["HorrificForm"] = Guids.MarkHorrificForm, ["LongLimbs"] = Guids.MarkLongLimbs, ["TwistedLegs"] = Guids.MarkTwistedLegs,
            ["BlurredEyes"] = Guids.MarkBlurredEyes, ["ExposedNerves"] = Guids.MarkExposedNerves, ["BurstingVeins"] = Guids.MarkBurstingVeins, ["SeethingMind"] = Guids.MarkSeethingMind,
            // 개편 O5: 자연 공격 변이 11(계획 원장 A #16~26)
            ["Fangs"] = Guids.MarkFangs, ["Claws"] = Guids.MarkClaws, ["Horns"] = Guids.MarkHorns, ["Tail"] = Guids.MarkTail, ["Tentacle"] = Guids.MarkTentacle, ["Stinger"] = Guids.MarkStinger,
            ["Wings"] = Guids.MarkWings, ["Hooves"] = Guids.MarkHooves, ["Talons"] = Guids.MarkTalons, ["Pincer"] = Guids.MarkPincer, ["NeuroStinger"] = Guids.MarkNeuroStinger,
            // 개편 O6: 양면 변이 5(계획 원장 A #27~31)
            ["Bloodthirst"] = Guids.MarkBloodthirst, ["BloatedMuscle"] = Guids.MarkBloatedMuscle, ["StoneShell"] = Guids.MarkStoneShell, ["RawNerves"] = Guids.MarkRawNerves, ["PainResonance"] = Guids.MarkPainResonance,
        };

        public static IReadOnlyList<string> Ids { get { return ChaosRules.Mutations.Select(m => m.Id).ToArray(); } }

        public static string MarkerOf(string id)
        {
            string g;
            if (id == null || !markers.TryGetValue(id, out g)) throw new ArgumentException("ChaosWarrior: unknown mutation id '" + id + "'", "id");
            return g;
        }

        public static string IdOfMarker(string markerGuid)
        {
            var hit = markers.Where(kv => kv.Value == markerGuid).Select(kv => kv.Key).ToArray();
            return hit.Length == 1 ? hit[0] : null;
        }

        public static bool IsMarker(string guid) { return markers.ContainsValue(guid); }

        // 원소 껍질 효과 버프(ChaosRules.Dials.ShellElements 와 같은 순서 — 시험 `exec: required buffs` 가 길이·순서를 고정).
        public static readonly IReadOnlyList<string> ShellBuffIds = new[] { Guids.ShellFire, Guids.ShellCold, Guids.ShellAcid, Guids.ShellElectricity };

        // ---------------- 개편 O1: 겹 방식 · 다시 뽑기 행(계획 2026-11-16 원장 A — 그 표의 「겹」「다시 뽑기」 열 전사) ----------------
        // Rank = 순위형(버프 1개, Stacking = Rank, 순위 = 뽑힌 횟수) · Instances = 인스턴스형(Stacking = Stack, 인스턴스 수 = 뽑힌 횟수). 정찰 2026-11-16 §1.2·§1.4.
        public enum StackMode { Rank, Instances }
        // Never = 항상 후보 · OncePerRage = 이 분노에서 이미 뽑혔으면 후보에서 뺀다 · LimbCap = 그 범주의 실제 사지 수가 엔진 상한에 찼으면 뺀다(개편 설계 §12-1). 시전자가 변신 중이면 LimbCap 행은 전부 뺀다(MutationEligibility).
        public enum RedrawRule { Never, OncePerRage, LimbCap }

        public struct StackRow
        {
            public readonly string Id;
            public readonly StackMode Mode;
            public readonly RedrawRule Redraw;
            // LimbCap 일 때만: 엔진 WeaponCategory 이름. 그 밖엔 null.
            public readonly string LimbCategory;
            public StackRow(string id, StackMode mode, RedrawRule redraw, string limbCategory) { Id = id; Mode = mode; Redraw = redraw; LimbCategory = limbCategory; }
        }

        static StackRow Stack(string id, StackMode mode, RedrawRule redraw, string limbCategory = null) { return new StackRow(id, mode, redraw, limbCategory); }

        // 출처: 계획 2026-11-16 「단일 원장 A」 #0~31(#27~31 = 개편 O6 양면 5종: 전부 순위형 · 다시 뽑기 없음) · #16~26(개편 O5 자연 공격 11종: 전부 인스턴스형 · 상한(범주) — 범주는 원장 B 의 무기 형식 범주, 집게는 Claw) · #0~15(개편 설계 §12-1: 굳은 살갗·끔찍한 형상·늘어난 사지 = 1회 / §12-4: 자연 공격·재생 = 인스턴스형, 나머지 순위형). 순서 = ChaosRules.Mutations.
        public static readonly IReadOnlyList<StackRow> StackRows = new[]
        {
            Stack("Enlarge", StackMode.Rank, RedrawRule.Never), Stack("ToughHide", StackMode.Rank, RedrawRule.OncePerRage), Stack("Regeneration", StackMode.Instances, RedrawRule.Never),
            Stack("Swiftness", StackMode.Rank, RedrawRule.Never), Stack("ThornSkin", StackMode.Rank, RedrawRule.Never), Stack("ChaosBlood", StackMode.Rank, RedrawRule.Never),
            Stack("ElementalShell", StackMode.Rank, RedrawRule.Never), Stack("PainAmplify", StackMode.Rank, RedrawRule.Never), Stack("ExtraArm", StackMode.Instances, RedrawRule.LimbCap, "Slam"),
            Stack("HorrificForm", StackMode.Rank, RedrawRule.OncePerRage), Stack("LongLimbs", StackMode.Rank, RedrawRule.OncePerRage), Stack("TwistedLegs", StackMode.Rank, RedrawRule.Never),
            Stack("BlurredEyes", StackMode.Rank, RedrawRule.Never), Stack("ExposedNerves", StackMode.Rank, RedrawRule.Never), Stack("BurstingVeins", StackMode.Rank, RedrawRule.Never),
            Stack("SeethingMind", StackMode.Rank, RedrawRule.Never),
            Stack("Fangs", StackMode.Instances, RedrawRule.LimbCap, "Bite"), Stack("Claws", StackMode.Instances, RedrawRule.LimbCap, "Claw"), Stack("Horns", StackMode.Instances, RedrawRule.LimbCap, "Gore"), Stack("Tail", StackMode.Instances, RedrawRule.LimbCap, "Tail"),
            Stack("Tentacle", StackMode.Instances, RedrawRule.LimbCap, "Tentacle"), Stack("Stinger", StackMode.Instances, RedrawRule.LimbCap, "Sting"), Stack("Wings", StackMode.Instances, RedrawRule.LimbCap, "Wing"), Stack("Hooves", StackMode.Instances, RedrawRule.LimbCap, "Hoof"),
            Stack("Talons", StackMode.Instances, RedrawRule.LimbCap, "Talon"), Stack("Pincer", StackMode.Instances, RedrawRule.LimbCap, "Claw"), Stack("NeuroStinger", StackMode.Instances, RedrawRule.LimbCap, "Sting"),
            Stack("Bloodthirst", StackMode.Rank, RedrawRule.Never), Stack("BloatedMuscle", StackMode.Rank, RedrawRule.Never), Stack("StoneShell", StackMode.Rank, RedrawRule.Never),
            Stack("RawNerves", StackMode.Rank, RedrawRule.Never), Stack("PainResonance", StackMode.Rank, RedrawRule.Never),
        };

        public static StackRow StackOf(string id)
        {
            var hit = StackRows.Where(r => r.Id == id).ToArray();
            if (hit.Length != 1) throw new ArgumentException("ChaosWarrior: mutation id '" + id + "' has " + hit.Length + " stack rows (need exactly 1)", "id");
            return hit[0];
        }

        // 엔진 StackingType 이름(정찰 §1.2: Rank 6 · Stack 3). 표식 빌더가 E<StackingType>(이 이름) 으로 옮긴다(O3).
        public const string StackingRank = "Rank";
        public const string StackingStack = "Stack";

        public static string StackingName(string id)
        {
            var mode = StackOf(id).Mode;
            if (mode == StackMode.Rank) return StackingRank;
            if (mode == StackMode.Instances) return StackingStack;
            throw new InvalidOperationException("ChaosWarrior: stack mode " + mode + " of '" + id + "' has no engine StackingType name");
        }

        // 표식 블루프린트의 Ranks: 순위형·Never → MaxDraws(엔진 AddRank 는 Ranks 를 넘으면 기록 없이 무동작 — 정찰 §1.2) · 순위형·OncePerRage → 1 · 인스턴스형 → 1(엔진 기본).
        public static int RanksOf(string id)
        {
            var row = StackOf(id);
            if (row.Mode == StackMode.Instances) return 1; // 인스턴스마다 순위 1(엔진 Buff 의 첫 순위, 정찰 §1.2)
            if (row.Mode != StackMode.Rank) throw new InvalidOperationException("ChaosWarrior: stack mode " + row.Mode + " of '" + id + "' has no Ranks rule");
            if (row.Redraw == RedrawRule.OncePerRage) return 1; // 한 분노에 한 번만 뽑힌다 → 순위는 1 을 넘지 않는다
            if (row.Redraw == RedrawRule.Never) return ChaosRules.MaxDraws;
            throw new InvalidOperationException("ChaosWarrior: rank-mode mutation '" + id + "' has redraw rule " + row.Redraw + " (no Ranks rule — a limb cap needs instance mode)");
        }

        // 개편 O3: 원소 껍질 버프 4종의 겹 방식(계획 2026-11-16 원장 A #6 「껍질 버프(4종, R, Ranks = MaxDraws)」) — 같은 원소가 다시 굴려지면 순위가 올라 저항이 더해진다(AddDamageResistanceEnergy = 값 × 순위).
        // 원소 껍질 변이는 Never 라 한 분노에 MaxDraws 번까지 뽑히고, 전부 같은 원소일 수 있다 → Ranks = MaxDraws.
        // static readonly(const 는 소비처 IL 에 접혀 「빌더가 사양을 읽는다」를 시험이 볼 수 없다 — ChaosRules 머리말과 같은 이유).
        public static readonly string ShellStackingName = StackingRank;
        public static int ShellRanks { get { return ChaosRules.MaxDraws; } }

        // 정적 검사(빌드 시작 때 Mutations.Build 가 부른다). 어긋나면 던진다.
        public static void Validate()
        {
            ChaosRules.ValidateKinds();
            Validate(StackRows);
            ValidateScales();
        }

        // ① Id 집합 == ChaosRules.Mutations(순서 포함) ② Never 행 ≥ 1(항상 뽑을 수 있는 변이 — 후보 0 방지, 개편 설계 §12-1) ③ LimbCap ⇔ LimbCategory 있음
        // ④ LimbCap 행 집합 == 자연 공격(Kind) 행 집합이고 그 행은 전부 인스턴스형(사지는 사실 하나에 하나 — 정찰 §1.2) ⑤ 행마다 후보 규칙이 돈다(DryRunCandidates).
        internal static void Validate(IReadOnlyList<StackRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            var ids = rows.Select(r => r.Id).ToArray();
            var want = ChaosRules.Mutations.Select(m => m.Id).ToArray();
            if (!ids.SequenceEqual(want)) throw new InvalidOperationException("ChaosWarrior: stack rows [" + string.Join(",", ids) + "] != mutation table [" + string.Join(",", want) + "] (same ids, same order)");
            if (!rows.Any(r => r.Redraw == RedrawRule.Never)) throw new InvalidOperationException("ChaosWarrior: no stack row is always drawable (RedrawRule.Never) — a draw could have no candidate");
            foreach (var r in rows)
            {
                bool cap = r.Redraw == RedrawRule.LimbCap;
                if (cap && string.IsNullOrEmpty(r.LimbCategory)) throw new InvalidOperationException("ChaosWarrior: stack row '" + r.Id + "' is LimbCap without a limb category");
                if (!cap && r.LimbCategory != null) throw new InvalidOperationException("ChaosWarrior: stack row '" + r.Id + "' has limb category '" + r.LimbCategory + "' but redraw rule " + r.Redraw);
                if (cap && r.Mode != StackMode.Instances) throw new InvalidOperationException("ChaosWarrior: LimbCap stack row '" + r.Id + "' must be instance mode (one limb per fact)");
            }
            var capIds = rows.Where(r => r.Redraw == RedrawRule.LimbCap).Select(r => r.Id).ToArray();
            var natural = ChaosRules.Mutations.Where(m => m.Kind == ChaosRules.MutationKind.NaturalAttack).Select(m => m.Id).ToArray();
            if (!capIds.SequenceEqual(natural)) throw new InvalidOperationException("ChaosWarrior: LimbCap rows [" + string.Join(",", capIds) + "] != natural-attack mutations [" + string.Join(",", natural) + "]");
            DryRunCandidates(rows);
        }

        // ⑤ 후보 판정의 마른 실행(독립 검토 R1-M2, 2026-11-17): 행마다 후보 규칙이 실제로 돈다는 것을 빌드 때 확인한다 — 모르는 다시 뽑기 규칙 · 범주 없는 LimbCap 행은 분노를 켤 때가 아니라 여기서 던진다.
        // 상태 = 아무것도 안 뽑힘 · 변신 아님 · 사지 0 · 상한 없음(실제 상한은 실행 때 엔진에서 읽는다 — 이 실행은 규칙의 유무만 본다). 이 상태에서는 모든 행이 후보여야 한다(아니면 영영 뽑히지 않는 변이다).
        // 사지 범주 이름이 엔진 WeaponCategory 인지는 게임 쪽 빌드가 본다(NaturalWeapons.VerifyNow: 복제 무기의 엔진 범주 == 원장 범주, NaturalWeaponPlan.Validate: 원장 범주 == 이 표의 범주).
        internal static void DryRunCandidates(IReadOnlyList<StackRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            var empty = new DrawState(rows.Count) { Polymorphed = false, LimbsNow = category => 0, LimbCap = category => ChaosRules.Dials.LimbCapNone };
            var eligible = MutationEligibility.Eligible(rows, empty);   // 규칙이 없거나 판정할 수 없는 행이 있으면 여기서 던진다
            if (eligible.Count != rows.Count)
                throw new InvalidOperationException("ChaosWarrior: stack rows [" + string.Join(",", Enumerable.Range(0, rows.Count).Where(i => !eligible.Contains(i)).Select(i => rows[i].Id)) + "] are not drawable even with nothing drawn, no limbs and no cap");
        }

         
        // Component 이름은 엔진 부품 이름(또는 None·NewRoundDamage), Stat·Descriptor 는 엔진 열거 이름, Value = 워프리스트 레벨 → 값(ChaosRules 에서), Extra = 부품별 보충(원소·무기·피해 형식).
        // 레벨에 따라 변하는 값(ScaledByFive)은 빌더가 ContextRankConfig(ProgressionRows(Value)) 로 옮긴다 — Scaled = true.
        // 개편 O2: 그 행의 효과가 「뽑힌 횟수」에 어떻게 비례하는가(계획 2026-11-16 Task O2 Interfaces, 엔진 근거 = 정찰 2026-11-16 §1.2).
        //   PerRank        엔진 부품이 값 × 순위(Fact.GetRank)를 스스로 곱한다
        //   PerRankContext 우리 빌더가 값을 ContextValue{CasterBuffRank} 로 넣어 순위를 곱한다(가시 피부 주사위 개수 · 터지는 핏줄 피해)
        //   PerInstance    부품이 순위를 보지 않는다 — 인스턴스마다 한 번(인스턴스형 변이)
        //   Once           순위·인스턴스와 무관하게 한 번(여러 번 뽑혀도 효과가 늘지 않는다)
        //   Core           부품 없음 — 핵심(MutationCore·PainCore)이 순위를 읽는다
        public enum RowScale { PerRank, PerRankContext, PerInstance, Once, Core }

        public struct EffectRow
        {
            public readonly string Id, Component, Stat, Descriptor, Extra;
            public readonly Func<int, int> Value;
            public readonly bool Scaled;
            public readonly RowScale Scale;
            public EffectRow(string id, string component, string stat, string descriptor, Func<int, int> value, string extra, bool scaled, RowScale scale)
            { Id = id; Component = component; Stat = stat; Descriptor = descriptor; Value = value; Extra = extra; Scaled = scaled; Scale = scale; }
        }

        public const string ShellPrefix = "Shell:";

        // Scale 은 기본값이 없다 — 새 행이 「곱해지는지」를 적지 않고 지나가지 못한다.
        static EffectRow Row(string id, string component, RowScale scale, string stat, string descriptor, Func<int, int> value, string extra = null, bool scaled = false)
        {
            return new EffectRow(id, component, stat, descriptor, value, extra, scaled, scale);
        }

        public static readonly IReadOnlyList<EffectRow> EffectRows = BuildRows();

        static EffectRow[] BuildRows()
        {
            var l = new List<EffectRow>
            {
                // 거대화: 크기 +1(ChangeUnitSize Delta, EnlargePersonBuff 선례) · 힘 Size(M2 기본값)
                Row("Enlarge", "ChangeUnitSize", RowScale.Once, null, null, L => ChaosRules.Dials.EnlargeSizeDelta, "Delta"),
                Row("Enlarge", "AddStatBonus", RowScale.PerRank, "Strength", "Size", L => ChaosRules.Dials.EnlargeStr),
                Row("ToughHide", "AddDamageResistancePhysical", RowScale.Once, null, null, L => ChaosRules.ScaledByFive(ChaosRules.Dials.ToughHideBase, L), null, true),
                Row("Regeneration", "AddEffectFastHealing", RowScale.PerInstance, null, null, L => ChaosRules.ScaledByFive(ChaosRules.Dials.RegenBase, L), null, true),
                Row("Swiftness", "AddStatBonus", RowScale.PerRank, "Speed", "UntypedStackable", L => ChaosRules.Dials.SwiftSpeedFeet),
                Row("Swiftness", "AddStatBonus", RowScale.PerRank, "SaveReflex", "UntypedStackable", L => ChaosRules.Dials.SwiftReflex),
                // 가시 피부: 근접 명중한 공격자에게 물리 관통(M1 기본값) (ThornsDiceCount × 표식 순위) d ThornsDiceSides — 개편 설계 §5 「반사 피해 주사위 n개」
                Row("ThornSkin", "AddTargetAttackWithWeaponTrigger", RowScale.PerRankContext, null, null, L => ChaosRules.Dials.ThornsDiceCount, "Piercing"),
                Row("ChaosBlood", "BuffAllSavesBonus", RowScale.PerRank, null, "UntypedStackable", L => ChaosRules.Dials.ChaosBloodSaves),
                Row("ElementalShell", "None", RowScale.Core, null, null, L => 0),
                Row("PainAmplify", "None", RowScale.Core, null, null, L => 0),
                // 추가 팔: 우리 복제 슬램(개편 O4 — 본편 Slam1d6 을 직접 쓰면 다른 출처의 슬램 사지와 구분되지 않는다, 정찰 §2.2). NaturalWeaponPlan.Validate 가 이 칸 == 무기 원장의 GUID 를 검사한다.
                Row("ExtraArm", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponSlam),
                Row("HorrificForm", "AddAreaEffect", RowScale.Once, null, null, L => ChaosRules.Dials.HorrorRadiusFeet, Guids.HorrorArea),
                Row("LongLimbs", "AddStatBonus", RowScale.PerRank, "Reach", "UntypedStackable", L => ChaosRules.Dials.LongLimbsReachFeet),
                Row("TwistedLegs", "AddStatBonus", RowScale.PerRank, "Speed", "Penalty", L => ChaosRules.Dials.TwistedLegsSpeedFeet),
                Row("BlurredEyes", "AddStatBonus", RowScale.PerRank, "AdditionalAttackBonus", "Penalty", L => ChaosRules.Dials.BlurredEyesAttack),
                Row("ExposedNerves", "AddStatBonus", RowScale.PerRank, "AC", "Penalty", L => ChaosRules.Dials.ExposedNervesAc),
                // 터지는 핏줄: 매 라운드 직접 피해 (BurstingVeinsDamage × 표식 순위) — 개편 설계 §5 「라운드당 피해 × n」
                Row("BurstingVeins", "NewRoundDamage", RowScale.PerRankContext, null, null, L => ChaosRules.Dials.BurstingVeinsDamage, "Direct"),
                Row("SeethingMind", "AddStatBonus", RowScale.PerRank, "SaveWill", "Penalty", L => ChaosRules.Dials.SeethingMindWill),
                // 개편 O5: 자연 공격 변이 11종 — 표식 인스턴스마다 우리 복제 무기 사지 하나(계획 원장 A #16~26 · 원장 B). 독·비행 같은 부가 효과 없음(개편 설계 §2 비목표).
                Row("Fangs", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponFangs),
                Row("Claws", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponClaws),
                Row("Horns", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponHorns),
                Row("Tail", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponTail),
                Row("Tentacle", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponTentacle),
                Row("Stinger", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponStinger),
                Row("Wings", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponWings),
                Row("Hooves", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponHooves),
                Row("Talons", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponTalons),
                Row("Pincer", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponPincer),
                Row("NeuroStinger", "AddAdditionalLimb", RowScale.PerInstance, null, null, L => 0, Guids.WeaponNeuroStinger),
                // 개편 O6: 양면 변이 5종(계획 원장 A #27~31 · 원장 C · 정찰 §6.2) — 이로운 쪽 먼저, 해로운 쪽 뒤. 전부 순위형 표식이라 엔진 부품이 값 × 순위를 낸다(n번 뽑히면 양쪽 다 n배).
                // 이로운 쪽 기술자는 다른 출처와 합산되는 종류(분노 Rage · 거대화 Size 와 「최대 하나」가 되지 않게 — 개편 설계 §12-4), 해로운 쪽은 Penalty.
                // 피의 갈증: 근접 피해 — WeaponAttackTypeDamageBonus{Type = Extra(엔진 WeaponRangeType 이름)}. 기술자 칸은 엔진이 코드에 고정한 UntypedStackable(OnEventAboutToTrigger @004F — Descriptor 필드는 읽지 않는다)을 적는다.
                Row("Bloodthirst", "WeaponAttackTypeDamageBonus", RowScale.PerRank, null, "UntypedStackable", L => ChaosRules.Dials.BloodthirstDamage, "Melee"),
                Row("Bloodthirst", "AddStatBonus", RowScale.PerRank, "AC", "Penalty", L => ChaosRules.Dials.BloodthirstAc),
                Row("BloatedMuscle", "AddStatBonus", RowScale.PerRank, "Strength", "UntypedStackable", L => ChaosRules.Dials.BloatedMuscleStr),
                Row("BloatedMuscle", "AddStatBonus", RowScale.PerRank, "Dexterity", "Penalty", L => ChaosRules.Dials.BloatedMuscleDex),
                // 돌 껍질: 자연 방어 = NaturalArmor(합산 — 계획 「판정 대기」 P2 기본값)
                Row("StoneShell", "AddStatBonus", RowScale.PerRank, "AC", "NaturalArmor", L => ChaosRules.Dials.StoneShellNaturalArmor),
                Row("StoneShell", "AddStatBonus", RowScale.PerRank, "Speed", "Penalty", L => ChaosRules.Dials.StoneShellSpeedFeet),
                // 과민한 신경: 회피 AC = Dodge(계획 「판정 대기」 P3 기본값 — 엔진 기본 동작대로 무방비 상태에서 사라진다)
                Row("RawNerves", "AddStatBonus", RowScale.PerRank, "SaveReflex", "UntypedStackable", L => ChaosRules.Dials.RawNervesReflex),
                Row("RawNerves", "AddStatBonus", RowScale.PerRank, "AC", "Dodge", L => ChaosRules.Dials.RawNervesDodgeAc),
                Row("RawNerves", "AddStatBonus", RowScale.PerRank, "SaveWill", "Penalty", L => ChaosRules.Dials.RawNervesWill),
                // 고통 공명: 고통 겹 추가는 부품 없음(Core — O7 의 고통 핵심이 표식 순위를 읽는다) · 자신의 모든 내성 −1 × 순위
                Row(PainResonanceId, "None", RowScale.Core, null, null, L => 0),
                Row(PainResonanceId, "BuffAllSavesBonus", RowScale.PerRank, null, "Penalty", L => ChaosRules.Dials.PainResonanceSaves),
            };
            foreach (var e in ChaosRules.Dials.ShellElements)
                l.Add(Row(ShellPrefix + e, "AddDamageResistanceEnergy", RowScale.PerRank, null, null, L => ChaosRules.Dials.ElementResist, e));
            return l.ToArray();
        }

        public static IReadOnlyList<EffectRow> RowsFor(string id) { return EffectRows.Where(r => r.Id == id).ToArray(); }

        // ---------------- 개편 O2: 효과 행의 겹 대응 검사 ----------------
        // 부품 이름 → 그 부품이 실제로 하는 것(행의 Scale 열이 이것과 다르면 표가 엔진과 어긋난 것이다 — 던진다). 표에 없는 부품도 던진다.
        // 출처: 정찰 2026-11-16 §1.2(엔진 IL) — AddStatBonus.OnTurnOn @0016~0028 · BuffAllSavesBonus.OnTurnOn @0007~0017 · AddDamageResistanceEnergy.CalculateValue @0000~003B = 값 × 순위,
        //       AddDamageResistanceBase.CalculateValue @0000~0016(물리 DR: 순위 무시) · UnitPartSizeModifier.UpdateSize(크기 변경은 누적 안 됨) · 오라는 사실 하나에 하나,
        //       AddEffectFastHealing.OnNewRound @002C~004A · AddAdditionalLimb.OnTurnOn @0006~0029(순위 무시, 인스턴스마다 한 번).
        //       가시 피부·터지는 핏줄 = 계획 Task O2 갈림 A(K1: ContextValue.Calculate @019B~022E 가 문맥의 연관 블루프린트 = 표식 버프의 순위 × Value 를 낸다 — Mutations.CvBuffRank 주석).
        //       개편 O6: WeaponAttackTypeDamageBonus.OnEventAboutToTrigger @0000~0051 = Value × AttackBonus × Fact.GetRank()(Step 0 재판독 2026-11-17 — 시험 `exec: two-sided engine`).
        static readonly Dictionary<string, RowScale> componentScales = new Dictionary<string, RowScale>
        {
            ["AddStatBonus"] = RowScale.PerRank, ["BuffAllSavesBonus"] = RowScale.PerRank, ["AddDamageResistanceEnergy"] = RowScale.PerRank, ["WeaponAttackTypeDamageBonus"] = RowScale.PerRank,
            ["AddTargetAttackWithWeaponTrigger"] = RowScale.PerRankContext, ["NewRoundDamage"] = RowScale.PerRankContext,
            ["AddEffectFastHealing"] = RowScale.PerInstance, ["AddAdditionalLimb"] = RowScale.PerInstance,
            ["ChangeUnitSize"] = RowScale.Once, ["AddAreaEffect"] = RowScale.Once, ["AddDamageResistancePhysical"] = RowScale.Once,
            ["None"] = RowScale.Core,
        };

        public static RowScale ComponentScale(string component)
        {
            RowScale s;
            if (component == null || !componentScales.TryGetValue(component, out s)) throw new InvalidOperationException("ChaosWarrior: effect component '" + component + "' has no scale entry (MutationSpecs.componentScales) — say how it reacts to a second draw before using it");
            return s;
        }

        // 겹칠 수 있는 변이인데도 「한 번만」이 설계인 행(이름 붙인 예외).
        // 출처: 개편 설계 §5 「거대화: 크기는 한 단계까지만, 두 번째부터는 힘 +2만」.
        public static readonly IReadOnlyList<Tuple<string, string>> DesignOnceRows = new[] { Tuple.Create("Enlarge", "ChangeUnitSize") };

        // 정적 검사(Validate 가 부른다 → 빌드 시작 때): 생산 표.
        public static void ValidateScales() { ValidateScales(EffectRows, StackRows, DesignOnceRows); }

        // ⓪ 행의 Scale == 그 부품이 실제로 하는 것(ComponentScale) ① 인스턴스형 변이의 행은 전부 PerInstance ② 순위형 변이의 행은 PerRank·PerRankContext·Once·Core
        // ③ Once 행은 (그 변이가 OncePerRage) 이거나 예외 목록에 있어야 한다 — 순위를 무시하는 부품이 겹칠 수 있는 변이에 달리면 「두 번 뽑혔는데 한 번만 적용」이다(개편 설계 반목표 1).
        // 원소 껍질 행(Shell:<원소>)은 원소 껍질 변이의 겹 방식을 따른다. 예외 목록의 행이 표에 없으면(낡은 예외) 던진다.
        internal static void ValidateScales(IReadOnlyList<EffectRow> rows, IReadOnlyList<StackRow> stacks, IReadOnlyList<Tuple<string, string>> onceExceptions)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (stacks == null) throw new ArgumentNullException("stacks");
            if (onceExceptions == null) throw new ArgumentNullException("onceExceptions");
            foreach (var r in rows)
            {
                string owner = r.Id;
                if (r.Id != null && r.Id.StartsWith(ShellPrefix, StringComparison.Ordinal))
                {
                    string element = r.Id.Substring(ShellPrefix.Length);
                    if (!ChaosRules.Dials.ShellElements.Contains(element)) throw new InvalidOperationException("ChaosWarrior: effect row '" + r.Id + "' names an unknown shell element");
                    owner = ElementalShellId;
                }
                var hit = stacks.Where(x => x.Id == owner).ToArray();
                if (hit.Length != 1) throw new InvalidOperationException("ChaosWarrior: effect row '" + r.Id + "' (" + r.Component + ") has " + hit.Length + " stack rows for '" + owner + "' (need exactly 1)");
                var stack = hit[0];
                string where = "effect row " + r.Id + "/" + r.Component;
                var engine = ComponentScale(r.Component);
                if (r.Scale != engine) throw new InvalidOperationException("ChaosWarrior: " + where + " says scale " + r.Scale + " but that component is " + engine);
                if (stack.Mode == StackMode.Instances)
                {
                    if (r.Scale != RowScale.PerInstance) throw new InvalidOperationException("ChaosWarrior: " + where + " is " + r.Scale + " on an instance-mode mutation (every row must be PerInstance)");
                }
                else if (stack.Mode == StackMode.Rank)
                {
                    if (r.Scale == RowScale.PerInstance) throw new InvalidOperationException("ChaosWarrior: " + where + " is PerInstance on a rank-mode mutation (a second draw would raise the rank but not the effect)");
                    if (r.Scale == RowScale.Once && stack.Redraw != RedrawRule.OncePerRage && !onceExceptions.Any(x => x.Item1 == owner && x.Item2 == r.Component))
                        throw new InvalidOperationException("ChaosWarrior: " + where + " ignores the rank but '" + owner + "' can be drawn again (redraw rule " + stack.Redraw + ") and is not in DesignOnceRows — drawn twice, applied once");
                }
                else throw new InvalidOperationException("ChaosWarrior: " + where + " has stack mode " + stack.Mode + " (no scale rule)");
            }
            foreach (var x in onceExceptions)
                if (!rows.Any(r => r.Id == x.Item1 && r.Component == x.Item2 && r.Scale == RowScale.Once))
                    throw new InvalidOperationException("ChaosWarrior: DesignOnceRows names " + x.Item1 + "/" + x.Item2 + " but no Once effect row matches (stale exception)");
        }
        public static IReadOnlyList<string> RequiredBuffs(string id, int elementIdx)
        {
            var l = new List<string> { MarkerOf(id) };
            if (id == ElementalShellId)
            {
                if (ShellBuffIds.Count != ChaosRules.Dials.ShellElements.Length) throw new InvalidOperationException("ChaosWarrior: shell buffs " + ShellBuffIds.Count + " != ShellElements " + ChaosRules.Dials.ShellElements.Length);
                if (elementIdx < 0 || elementIdx >= ShellBuffIds.Count) throw new ArgumentOutOfRangeException("elementIdx", elementIdx, "ChaosWarrior: shell element index out of range");
                l.Add(ShellBuffIds[elementIdx]);
            }
            return l;
        }
    }
}
