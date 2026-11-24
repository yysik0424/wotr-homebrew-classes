using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock
{
     
    public enum BlastKind { Single, Double }

    // 설계 §4.4 Range=Close · §5 L6 Medium(연속 사격은 L10 이라 늘 Medium).
    public enum BlastRange { Close, Medium }

    // 그룹 가호 종류(설계 §4.8): 저항 5/10/20 · 면역 · 별 = 편향 AC 1/2/3 · 별 4단 = 편향 AC 3 + 주문 저항 10 + 위치 레벨.
    public enum WardKind { Resist, Immune, Deflection, DeflectionAndSr }

    public sealed class RaySpec
    {
        public readonly Group Group;
        public readonly BlastRange Range;
        public readonly BlastKind Kind;
        public readonly string Guid, ProjectileGuid, IconGuid, Key;
        public readonly int Projectiles;
        public readonly float Delay;
        public readonly BlastElement Element;
        public RaySpec(Group g, BlastRange range, BlastKind kind, string guid, string projectile, int projectiles, float delay, string icon, string key)
        {
            Group = g; Range = range; Kind = kind; Guid = guid; ProjectileGuid = projectile; Projectiles = projectiles; Delay = delay; IconGuid = icon; Key = key; Element = PatronGroups.ElementOf(g);
        }
    }

    public sealed class WardSpec
    {
        public readonly Group Group;
        public readonly int Tier;
        public readonly string Guid, Key;
        public readonly WardKind Kind;
        public readonly BlastElement Element;
        public readonly int Value;       // Resist = 저항값, Deflection/DeflectionAndSr = 편향 AC, Immune = 0(값 없음)
        public readonly int SrStepLevel; // DeflectionAndSr 만: ContextRankConfig BonusValue 의 StepLevel(= 위치 레벨 + 이 값). 나머지 0
        public readonly bool ReapplyOnLevelUp;
        public WardSpec(Group g, int tier, WardKind kind, int value, int srStep, bool reapply)
        {
            Group = g; Tier = tier; Kind = kind; Value = value; SrStepLevel = srStep; ReapplyOnLevelUp = reapply;
            Guid = DerivedGuids.Ward(g, tier); Key = "Ward" + g + tier; Element = PatronGroups.ElementOf(g);
        }
    }

    // [순수] 키트 빌더가 쓰는 엔진 열거 값(게임 형식 없이 시험이 실행할 수 있게 int). 출처 = Assembly-CSharp 열거 리터럴(dnlib 2026-11-14, 정찰 §16-T3b);
    // 시험 `jbp: engine enum codes` 가 같은 DLL 의 리터럴과 이름으로 대조하고, 게임 쪽 빌드 되읽기가 (열거)값.ToString() == 이름 을 다시 확인한다(리뷰 T3 I-1~I-4).
    public static class EngineCodes
    {
        // Kingmaker.Enums.Damage.DamageEnergyType — 이름 = BlastElement 이름(Force 는 DamageType.Force 라 여기 없음).
        public static int Energy(BlastElement e)
        {
            switch (e)
            {
                case BlastElement.Fire: return 0;
                case BlastElement.Cold: return 1;
                case BlastElement.Electricity: return 3;
                case BlastElement.Acid: return 4;
                case BlastElement.NegativeEnergy: return 5;
                default: throw new ArgumentOutOfRangeException("e", e, "Warlock: " + e + " is not an energy type (Force is DamageType.Force)");
            }
        }

        // Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange — 이름 = BlastRange 이름(Close 1, Medium 2).
        public static int Range(BlastRange r)
        {
            switch (r)
            {
                case BlastRange.Close: return 1;
                case BlastRange.Medium: return 2;
                default: throw new ArgumentOutOfRangeException("r", r, "Warlock: unknown blast range");
            }
        }

        public const int ActionStandard = 1;          // UnitCommand.CommandType.Standard(설계 §4.4 「표준 행동」)
        public const int RankBaseClassLevel = 1;      // ContextRankBaseValueType.ClassLevel
        public const int RankBaseStatBonus = 3;       // ContextRankBaseValueType.StatBonus
        public const int RankBaseCharacterLevel = 5;  // ContextRankBaseValueType.CharacterLevel(쓰지 않는다 — 멀티클래스 반증 시험의 대조값)
        public const int ProgressionDiv2 = 1;         // ContextRankProgression.Div2
        public const int ProgressionStartPlusDivStep = 3; // ContextRankProgression.StartPlusDivStep
        public const int ProgressionBonusValue = 8;   // ContextRankProgression.BonusValue
        public const int RankTypeDefault = 0, RankTypeDamageDice = 1, RankTypeDamageBonus = 2; // AbilityRankType
        // T5 소환 변형(정찰 §16-T5 dnlib 리터럴, 시험 `jbp: engine enum codes` 가 이름으로 대조).
        public const int ActionSwift = 2;                     // UnitCommand.CommandType.Swift(설계 §4.6 「행동 신속」)
        public const int ProgressionMultiplyByModifier = 6;   // ContextRankProgression.MultiplyByModifier(ApplyProgression @008E~0096 = 값 × m_StepLevel)
        public const int DurationRateRounds = 0;              // DurationRate.Rounds
        public const int SpellDescriptorSummoning = 512;      // SpellDescriptor.Summoning(AugmentSummoning OnSpawnBuff 의 판정 비트 — 정찰 §4)
        public const int AnimationOmni = 4;                   // UnitAnimationActionCastSpell.CastAnimationStyle.Omni(계획 T5 「Animation = Omni」)
        // 항목 9(2026-11-14 dnlib): ModifierDescriptor.UntypedStackable = 25 — ModifierDescriptorHelper..cctor @001D 가 DefaultStackingDescriptors 에 넣는다(쌓임).
        // 본편 소환 강화의 Enhancement(9)는 그 집합에 없어 같은 서술자 중 최대값만 남는다(ModifiableValue.ApplyModifiersFiltered @00AF~00FC) → 같은 +4 를 Enhancement 로 다시 주면 0 이 된다.
        public const int ModifierUntypedStackable = 25;

        // 순위 설정 코드 → 엔진 열거 이름(KitBuild.RankOf 되읽기의 기대값 — 한 곳). 시험 `exec: rank spec names` 가 KitSpec 의 모든 RankSpec 과
        // 위의 RankBase*/Progression* 상수 전부를 Assembly-CSharp 열거 이름과 대조한다. 모르는 코드 = 던진다(「?」 자리표시를 돌려주면 되읽기가 엉뚱한 곳에서 터진다 — 2026-11-15 게임 로그).
        public static string RankBaseName(int code)
        {
            switch (code)
            {
                case RankBaseClassLevel: return "ClassLevel";
                case RankBaseStatBonus: return "StatBonus";
                case RankBaseCharacterLevel: return "CharacterLevel";
                default: throw new ArgumentOutOfRangeException("code", code, "Warlock: unknown ContextRankBaseValueType code (add it to EngineCodes.RankBaseName)");
            }
        }

        public static string ProgressionName(int code)
        {
            switch (code)
            {
                case ProgressionDiv2: return "Div2";
                case ProgressionStartPlusDivStep: return "StartPlusDivStep";
                case ProgressionMultiplyByModifier: return "MultiplyByModifier";
                case ProgressionBonusValue: return "BonusValue";
                default: throw new ArgumentOutOfRangeException("code", code, "Warlock: unknown ContextRankProgression code (add it to EngineCodes.ProgressionName)");
            }
        }
    }

    // [순수] ContextRankConfig 명세(설계 §4.4 「ContextRankConfig{ClassLevel, m_Class=[WitchClass], StartPlusDivStep, StartLevel 1, StepLevel 2}」·「매력 ÷ 2」, §4.8 「SR = 10 + 위치 레벨」).
    // StatBonus 기반(매력 ÷ 2)은 직업 목록을 읽지 않으므로 WitchClass = false(엔진 GetBaseValue 의 StatBonus 분기 — 정찰 §16-T3b).
    public sealed class RankSpec
    {
        public readonly string Name;
        public readonly int Type, Base, Progression, Start, Step;
        public readonly bool StatCharisma, WitchClass;
        public RankSpec(string name, int type, int baseType, int progression, int start, int step, bool statCharisma, bool witchClass)
        {
            Name = name; Type = type; Base = baseType; Progression = progression; Start = start; Step = step; StatCharisma = statCharisma; WitchClass = witchClass;
        }

        // 엔진 계산 전사(ContextRankConfig.GetBaseValue ClassLevel = Σ m_Class 레벨, CharacterLevel = 캐릭터 레벨, StatBonus = 능력치 수정치 → ApplyProgression @005C~0073 / @0050 / @009D~00A5).
        // witchLevel = 위치 레벨, characterLevel = 전체 캐릭터 레벨(멀티클래스면 더 큼), statMod = 매력 수정치. 모르는 기반·진행 = 던진다.
        public int Evaluate(int witchLevel, int characterLevel, int statMod)
        {
            if (witchLevel < 0 || characterLevel < witchLevel) throw new ArgumentOutOfRangeException("characterLevel", characterLevel, "Warlock: character level must be >= witch level >= 0");
            int v;
            if (Base == EngineCodes.RankBaseClassLevel) { if (!WitchClass) throw new InvalidOperationException("Warlock: ClassLevel rank " + Name + " without a class list counts nothing"); v = witchLevel; }
            else if (Base == EngineCodes.RankBaseCharacterLevel) v = characterLevel;
            else if (Base == EngineCodes.RankBaseStatBonus) v = statMod;
            else throw new InvalidOperationException("Warlock: rank " + Name + " has an unknown base " + Base);
            if (Progression == EngineCodes.ProgressionStartPlusDivStep) return 1 + Math.Max(0, (v - Start) / Step);
            if (Progression == EngineCodes.ProgressionDiv2) return v / 2;
            if (Progression == EngineCodes.ProgressionBonusValue) return v + Step;
            if (Progression == EngineCodes.ProgressionMultiplyByModifier) return v * Step;
            throw new InvalidOperationException("Warlock: rank " + Name + " has an unknown progression " + Progression);
        }
    }

    public sealed class MarkerSpec
    {
        public readonly string Guid, Key, FactGuid; // FactGuid = AddFacts 로 줄 본편 사실(L4 AugmentSummoning 만), 나머지 null = 컴포넌트 0 마커
        public readonly int Level;
        public MarkerSpec(string guid, int level, string key, string fact) { Guid = guid; Level = level; Key = key; FactGuid = fact; }
    }

    // [순수] 키트 블루프린트 명세(그룹 특성 6·블라스트 18·가호 24·마커 10·소환 부모 6). 게임 쪽 빌더(Groups/Blasts/LevelFeatures/Summons.Build)는 이 표만 읽어 만든다(C4).
    // 값의 출처: 투사체 = 설계 §4.4 표 · 줄기 수/간격 = 설계 §4.5(Dials.DoubleDelaySeconds) · 가호 = 설계 §4.8(Dials.WardValues/StarAc/StarSrBase) ·
    // 레벨 = WarlockRules.Levels · 아이콘 = 작성자 결정(정찰 §16-T3 「계획에 없던 사실」 4 — 설계가 정하지 않음).
    public static class KitSpec
    {
        public const int RaysPerSingle = 1; // 설계 §4.4 블라스트 1줄기
        public const int RaysPerTwin = 2; // 설계 §4.5 연속 사격 「광선 2줄기」

        // 설계 §4.4 「투사체」 줄(그룹 → 본편 투사체).
        public static string ProjectileOf(Group g)
        {
            switch (g)
            {
                case Group.Hell: return Warlock.Guids.Game.FireCommonProjectile00;
                case Group.Abyss: return Warlock.Guids.Game.AcidCommonProjectile00;
                case Group.Fey: return Warlock.Guids.Game.ColdCommonProjectile00;
                case Group.Storm: return Warlock.Guids.Game.ElectroCommonProjectile00;
                case Group.Death: return Warlock.Guids.Game.NegativeCommonProjectile00;
                case Group.Star: return Warlock.Guids.Game.MagicMissile00;
                default: throw new ArgumentOutOfRangeException("g", g, "Warlock: unknown group");
            }
        }

        // 아이콘 원천 능력(작성자 결정 — 정찰 §16-T3: 여섯 능력 모두 jbp m_Icon 있음, 시험 `jbp: group icons exist`).
        public static string IconOf(Group g)
        {
            switch (g)
            {
                case Group.Hell: return Warlock.Guids.Game.ScorchingRay;
                case Group.Abyss: return Warlock.Guids.Game.AcidSplash;
                case Group.Fey: return Warlock.Guids.Game.RayOfFrost;
                case Group.Storm: return Warlock.Guids.Game.Jolt;
                case Group.Death: return Warlock.Guids.Game.Enervation;
                case Group.Star: return Warlock.Guids.Game.MagicMissile;
                default: throw new ArgumentOutOfRangeException("g", g, "Warlock: unknown group");
            }
        }

        // 순위 설정 3종(리뷰 T3 I-4): 블라스트 주사위 · 블라스트 보너스 · 별 4단 SR.
        public static readonly RankSpec DiceRank = new RankSpec("BlastDice", EngineCodes.RankTypeDamageDice, EngineCodes.RankBaseClassLevel, EngineCodes.ProgressionStartPlusDivStep, WarlockRules.Dials.BlastStartLevel, WarlockRules.Dials.BlastStepLevel, false, true);
        public static readonly RankSpec BonusRank = new RankSpec("BlastBonus", EngineCodes.RankTypeDamageBonus, EngineCodes.RankBaseStatBonus, EngineCodes.ProgressionDiv2, 0, 0, true, false);
        public static readonly RankSpec StarSpellResistanceRank = new RankSpec("StarSr", EngineCodes.RankTypeDefault, EngineCodes.RankBaseClassLevel, EngineCodes.ProgressionBonusValue, 0, WarlockRules.Dials.StarSrBase, false, true);

        // T5 소환 지속(설계 §4.6 「지속 = 위치 레벨 라운드」): ContextRankConfig{Default, ClassLevel, [WitchClass], MultiplyByModifier, StepLevel = SummonRoundsPerLevel}(계획 T5).
        public static readonly RankSpec SummonRoundsRank = new RankSpec("SummonRounds", EngineCodes.RankTypeDefault, EngineCodes.RankBaseClassLevel, EngineCodes.ProgressionMultiplyByModifier, 0, WarlockRules.Dials.SummonRoundsPerLevel, false, true);

        // T5 소환 변형 54 + 대사자 표식 버프의 문자열 키(이름 + 설명). AllKeys(T3 키트 65)와 따로 — 시험 `strings: summon keys`.
        public static string VariantKey(Group g, int tier) { return "Summon" + g + tier; }
        public const string GrandSummonerKey = "CapstoneSummon";
        public const string IxBoostKey = "IxSummonBoost"; // 항목 9 두 번째 소환 강화 버프
        public const string ServantKey = "PatronServant"; // 항목 4b 조종 불가 표식 버프(54칸 전부)
        public static string[] SummonKeys()
        {
            var keys = new List<string>();
            foreach (var g in PatronGroups.AllGroups) for (int t = 1; t <= WarlockRules.Dials.SummonTiers; t++) keys.Add(VariantKey(g, t));
            keys.Add(GrandSummonerKey);
            keys.Add(IxBoostKey);
            keys.Add(ServantKey);
            return keys.ToArray();
        }

        public static string GroupKey(Group g) { return "Group" + g; }
        public static string ParentKey(Group g) { return "SummonParent" + g; }
        public const string PactKey = "Pact";

        static RaySpec[] rays;
        static WardSpec[] wards;
        static MarkerSpec[] markers;

        // 18 = 그룹 6 × {Close 블라스트, Medium 블라스트, Medium 연속 사격}. 순서 = 그룹 순 → Close, Medium, Double.
        public static RaySpec[] Rays()
        {
            if (rays != null) return rays;
            var list = new List<RaySpec>();
            foreach (var g in PatronGroups.AllGroups)
            {
                list.Add(new RaySpec(g, BlastRange.Close, BlastKind.Single, DerivedGuids.Blast(g, false), ProjectileOf(g), RaysPerSingle, 0f, IconOf(g), "Blast" + g + "Close"));
                list.Add(new RaySpec(g, BlastRange.Medium, BlastKind.Single, DerivedGuids.Blast(g, true), ProjectileOf(g), RaysPerSingle, 0f, IconOf(g), "Blast" + g + "Medium"));
                list.Add(new RaySpec(g, BlastRange.Medium, BlastKind.Double, DerivedGuids.Double(g), ProjectileOf(g), RaysPerTwin, WarlockRules.Dials.DoubleDelaySeconds, IconOf(g), "Double" + g));
            }
            return rays = list.ToArray();
        }

         
        public static WardSpec[] AllWards()
        {
            if (wards != null) return wards;
            int tiers = WarlockRules.Levels.WardLevels.Length;
            if (tiers != 4 || WarlockRules.Dials.WardValues.Length != tiers - 1 || WarlockRules.Dials.StarAc.Length != tiers - 1)
                throw new InvalidOperationException("Warlock: ward table needs 4 tiers with 3 resistance / AC values (WardLevels " + tiers + ", WardValues " + WarlockRules.Dials.WardValues.Length + ", StarAc " + WarlockRules.Dials.StarAc.Length + ")");
            var list = new List<WardSpec>();
            foreach (var g in PatronGroups.AllGroups)
                for (int t = 1; t <= tiers; t++)
                {
                    if (g == Group.Star)
                        list.Add(t < tiers ? new WardSpec(g, t, WardKind.Deflection, WarlockRules.Dials.StarAc[t - 1], 0, false)
                                           : new WardSpec(g, t, WardKind.DeflectionAndSr, WarlockRules.Dials.StarAc[tiers - 2], WarlockRules.Dials.StarSrBase, true));
                    else
                        list.Add(t < tiers ? new WardSpec(g, t, WardKind.Resist, WarlockRules.Dials.WardValues[t - 1], 0, false)
                                           : new WardSpec(g, t, WardKind.Immune, 0, 0, false));
                }
            return wards = list.ToArray();
        }

        // 10 마커 = 레벨표 자리(WarlockRules.Levels 가 원장 — 숫자를 여기 다시 쓰지 않는다). 규칙은 마커 사실 보유를 읽는다(계획 Global Constraints).
        public static MarkerSpec[] Markers()
        {
            if (markers != null) return markers;
            var L = WarlockRules.Levels.WardLevels;
            if (L.Length != 4) throw new InvalidOperationException("Warlock: marker table needs 4 ward levels (got " + L.Length + ")");
            return markers = new[]
            {
                new MarkerSpec(Warlock.Guids.Ward1Feature, L[0], "MarkerWard1", null),
                new MarkerSpec(Warlock.Guids.Augment4Feature, WarlockRules.Levels.AugmentLevel, "MarkerAugment", Warlock.Guids.Game.AugmentSummoning),
                new MarkerSpec(Warlock.Guids.Reach6Feature, WarlockRules.Levels.ReachLevel, "MarkerReach", null),
                new MarkerSpec(Warlock.Guids.Ward2Feature, L[1], "MarkerWard2", null),
                new MarkerSpec(Warlock.Guids.Double10Feature, WarlockRules.Levels.DoubleLevel, "MarkerDouble", null),
                new MarkerSpec(Warlock.Guids.HeatAccel12Feature, WarlockRules.Levels.HeatAccelLevel, "MarkerHeatAccel", null),
                new MarkerSpec(Warlock.Guids.Ward3Feature, L[2], "MarkerWard3", null),
                new MarkerSpec(Warlock.Guids.Extend16Feature, WarlockRules.Levels.ExtendLevel, "MarkerExtend", null),
                new MarkerSpec(Warlock.Guids.Ward4Feature, L[3], "MarkerWard4", null),
                new MarkerSpec(Warlock.Guids.Capstone20Feature, WarlockRules.Levels.CapstoneLevel, "MarkerCapstone", null),
            };
        }

        // 가호 마커 GUID(단계 순 1..4) — KitMarkers.WardTier 계산용(게임 쪽 Kit.Markers).
        public static readonly string[] TierMarkerGuids = { Warlock.Guids.Ward1Feature, Warlock.Guids.Ward2Feature, Warlock.Guids.Ward3Feature, Warlock.Guids.Ward4Feature };

        // 보유한 가호 마커(단계 순 bool 4개) → WardTier = 보유한 최고 단계(0 = 없음). 낮은 단계 없이 높은 단계만 있으면 레벨표 결함 → 던진다(조용히 높은 쪽을 믿지 않는다).
        public static int WardTierOf(bool[] held)
        {
            if (held == null || held.Length != TierMarkerGuids.Length) throw new ArgumentException("Warlock: WardTierOf needs " + TierMarkerGuids.Length + " flags");
            int tier = 0;
            for (int i = 0; i < held.Length; i++)
            {
                if (held[i]) { if (tier != i) throw new InvalidOperationException("Warlock: ward marker " + (i + 1) + " held without marker " + (tier + 1) + " (level table defect)"); tier = i + 1; }
            }
            return tier;
        }

        // 문자열 키(이름 + 설명) 전부 — 시험 `strings: every ability/feature has name+description keys` 의 원장.
        public static string[] AllKeys()
        {
            var keys = new List<string> { PactKey };
            foreach (var g in PatronGroups.AllGroups) { keys.Add(GroupKey(g)); keys.Add(ParentKey(g)); }
            keys.AddRange(Rays().Select(r => r.Key));
            keys.AddRange(AllWards().Select(w => w.Key));
            keys.AddRange(Markers().Select(m => m.Key));
            return keys.ToArray();
        }
    }
}
