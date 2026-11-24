using System;
using System.Collections.Generic;
using System.Linq;

namespace Golemancer
{
    // [순수] 아이콘 원장(2026-11-15 결함: 골렘 특성·재질 특성/선택/버프·우리 특성 2·돌 감속 오라·면역 공유·반대 학파 선택·도발 버프·슬램·특기 복제 4 가 m_Icon 없이 만들어져
    //   특성 선택 목록·초상 버프·행동 막대가 빈칸이었다. 도발 버프는 m_Icon = null 인 본편 선례 버프에서 「복사」하고 있었다 — 예외가 없어 사람이 보기 전까지 몰랐다).
    // 게임 형식 없음(modinfo testSources). 단일 원장(C4, 선례 BulwarkMage IconTable 84d70ed): 게임 쪽 Icons.Apply 가 이 표대로 본편 원본의 m_Icon 을 우리 블루프린트에 넣고,
    //   (1) 원본이 없거나 아이콘이 null 이면 던진다(BuildFailed), (2) 우리가 만든 블루프린트 전부가 Targets 또는 NoIcon 에 정확히 한 번 있어야 한다(아니면 던진다),
    //   (3) NoIcon 에 적은 이유(숨김·필드 없음·아키타입·유닛)가 실제와 다르면 던진다, (4) 보이는 사실·항목에 아이콘이 없으면 던진다,
    //   (5) 빌더가 이미 넣은 아이콘(학파 7·폭탄 절반·합체 3·응급 수리 8)이 표의 원본과 다르면 던진다.
    // 시험 `icons:` 가 같은 표를 Guids 상수 전부와 대조하고, 원본 jbp(blueprints.zip)의 AssetId·m_Icon 을 직독한다. 아이콘만 바꾼다 — 게임 동작 값은 건드리지 않는다.
    public sealed class IconSource
    {
        public readonly string Key, Guid, VanillaName, JbpPath, Reason;
        public IconSource(string key, string guid, string vanillaName, string jbpPath, string reason) { Key = key; Guid = guid; VanillaName = vanillaName; JbpPath = jbpPath; Reason = reason; }
    }

    public static class IconTable
    {
        // 본편 아이콘 원본(유일한 원장). 값 = blueprints.zip(2026-11-15 직독) 의 AssetId · 경로, 모두 m_Icon 이 비어 있지 않음(시험 icons: jbp 가 다시 확인).
        public static readonly IReadOnlyList<IconSource> Sources = new[]
        {
            new IconSource("WizardSchools", "656b12e5bec3442db074fe17f7c9fde8", "WizardSchools", "Classes/Wizard/WizardSchools.jbp", "반대 학파 선택 — 위저드 학파 특성(본편 OppositionSchoolSelection 은 m_Icon null)"),
            new IconSource("OppAbjuration", Guids.Game.OppositionSchoolAbjuration, "OppositionSchoolAbjuration", "Classes/Wizard/OppositionSchoolAbjuration.jbp", "학파 특성 — 복사 원본 그대로(OppositionSchools 가 이미 복사, 같은 값이어야 함)"),
            new IconSource("OppConjuration", Guids.Game.OppositionSchoolConjuration, "OppositionSchoolConjuration", "Classes/Wizard/OppositionSchoolConjuration.jbp", "학파 특성 — 복사 원본 그대로"),
            new IconSource("OppDivination", Guids.Game.OppositionSchoolDivination, "OppositionSchoolDivination", "Classes/Wizard/OppositionSchoolDivination.jbp", "학파 특성 — 복사 원본 그대로"),
            new IconSource("OppEnchantment", Guids.Game.OppositionSchoolEnchantment, "OppositionSchoolEnchantment", "Classes/Wizard/OppositionSchoolEnchantment.jbp", "학파 특성 — 복사 원본 그대로"),
            new IconSource("OppEvocation", Guids.Game.OppositionSchoolEvocation, "OppositionSchoolEvocation", "Classes/Wizard/OppositionSchoolEvocation.jbp", "학파 특성 — 복사 원본 그대로"),
            new IconSource("OppIllusion", Guids.Game.OppositionSchoolIllusion, "OppositionSchoolIllusion", "Classes/Wizard/OppositionSchoolIllusion.jbp", "학파 특성 — 복사 원본 그대로"),
            new IconSource("OppNecromancy", Guids.Game.OppositionSchoolNecromancy, "OppositionSchoolNecromancy", "Classes/Wizard/Necromancy/OppositionSchoolNecromancy.jbp", "학파 특성 — 복사 원본 그대로"),
            new IconSource("AlchemistBombsResource", Guids.Game.AlchemistBombsResource, "AlchemistBombsResource", "Classes/Alchemist/Bombs/AlchemistBombsResource.jbp", "폭탄 절반 — 복사 원본 그대로(BombHalf 가 이미 복사)"),
            new IconSource("AnimalCompanionSelection", "90406c575576aee40a34917a1b429254", "AnimalCompanionSelectionBase", "Classes/Pets/AnimalCompanionSelectionBase.jbp", "골렘 펫 특성·펫 직업·펫 진행표 — 동물 동료(펫) 그림"),
            new IconSource("CavalierChallenge", Guids.Game.FusionIconSource, "CavalierChallengeAbility", "Classes/Cavalier/CavalierChallengeAbility.jbp", "합체 토글·제어 버프·특성(복사 원본 그대로) + 도발 효과 버프 — 도전(도발) 그림"),
            new IconSource("CureLightWounds", Guids.Game.RepairIconSource, "CureLightWounds", "Spells/Level1/CureLightWounds.jbp", "응급 수리 특성·능력·변형 6 — 복사 원본 그대로(Repair 가 이미 복사)"),
            new IconSource("MutagenStrength", Guids.Game.OverdriveIconSource, "MutagenStrength", "Classes/Alchemist/Mutagens/MutagenStrength.jbp", "골렘 폭주 특성·능력·변형 54·버프·학파 효과 버프 8 — 약을 주입한다(변이약 그림, Overdrive 가 이미 복사)"),
            new IconSource("Barkskin", "5b77d7cc65b8ab74688e74a37fc2f553", "Barkskin", "Spells/Level2/Barkskin.jbp", "나무 재질 — 나무껍질 피부"),
            new IconSource("Stoneskin", "c66e86905f7606c4eaa5c774f0357b2b", "Stoneskin", "Spells/Level4/Stoneskin.jbp", "돌 재질 — 돌 피부"),
            new IconSource("IronBody", "198fcc43490993f49899ed086fe723c1", "IronBody", "Spells/Level8/IronBody.jbp", "철 재질 — 강철 신체"),
            new IconSource("ElementalBodyFire", "bb6bb6d6d4b27514dae8ec694433dcd3", "ElementalBodyIFire", "Spells/Level4/ElementalBodyIFire.jbp", "황동 재질 — 불 숨결 골렘, 불 원소 신체"),
            new IconSource("PrismaticSpray", "b22fd434bdb60fb4ba1068206402c4cf", "PrismaticSpray", "Spells/Level7/PrismaticSpray.jbp", "황금·황금 상위 재질 — 프리즘 쇄도 골렘"),
            new IconSource("Entangle", "0fd00984a2c0e0a429cf1a911b4ec5ca", "Entangle", "Spells/Level1/Entangle.jbp", "가을 재질 + 가을·여름 얽힘 오라 복제 — 얽힘"),
            new IconSource("SpikeGrowth", "29b0f9026ad05e14789d84e867cc6dff", "SpikeGrowth", "Spells/Level3/SpikeGrowth.jbp", "여름 재질 — 식물 성장(가시)"),
            new IconSource("SmitingAdamantite", "2c448ab4135c7c741b6f0f223901f9fa", "SmitingJudgmentAbilityAdamantite", "Classes/Inquisitor/Judgments/SmitingJudgmentAbilityAdamantite.jbp", "아다만틴 재질 — 아다만틴 심판"),
            new IconSource("Haste", "486eaff58293f6441a5c2759c4872f98", "Haste", "Spells/Level3/Haste.jbp", "미스랄 재질 — 빠른 골렘(이동 +20)"),
            new IconSource("ElementalBodyEarth", "5990d4f32cf8c6241948d2993d09179d", "ElementalBodyIEarth", "Spells/Level4/ElementalBodyIEarth.jbp", "재질 선택 L5·10·15 — 몸의 재질(흙 원소 신체)"),
            new IconSource("SlamGargantuan", "27eee74857c42db499b3a6b20cfa6211", "SlamGargantuan2d6", "Weapons/Items/GargantuanStandard/NaturalWeapon/SlamGargantuan2d6.jbp", "슬램 13 — 본편 슬램 무기(복사 원형 Slam2d6 은 m_Icon null)"),
            new IconSource("ThornBody", "2daf9c5112f16d54ab3cd6904c705c59", "ThornBody", "Spells/Level4/ThornBody.jbp", "나무 파편(복제) — 가시 신체(본편 WoodGolemSplintering 은 m_Icon null)"),
            new IconSource("GrabTiger", "0744543019af47f28c178e89b6cb101b", "GrabTiger", "Classes/Pets/GrabTiger.jbp", "붙잡기(복제) — 펫 붙잡기(본편 GolemAutumnGrabFeature 은 m_Icon null)"),
            new IconSource("Slow", "f492622e473d34747806bdb39356eb89", "Slow", "Spells/Level3/Slow.jbp", "돌 감속 오라 버프 — 감속 주문(오라가 SlowBuff 를 건다)"),
            new IconSource("FieryBody", "08ccad78cac525040919d51963f9ac39", "FieryBody", "Spells/Level9/FieryBody.jbp", "불로 수리 — 화염 신체(정찰 §8 선례 FieryBodyBuff)"),
            new IconSource("UncannyDodge", "3c08d842e802c3e4eb19d15496145709", "UncannyDodge", "Classes/Rogue/UncannyDodge.jbp", "급소 면역 — 기습·급소를 막는 직감 회피"),
            new IconSource("DeathWard", "0413915f355a38146bc6ad40cdf27b3f", "DeathWard", "Spells/Level4/DeathWard.jbp", "면역 공유 특성·버프 3 — 보호 주문"),
        };

        // 재질 id → 아이콘 원본 키(재질 특성·재질 버프가 같은 그림). 재질 집합 = MaterialCatalog.Materials(어긋나면 Problems 가 잡는다).
        public static readonly IReadOnlyDictionary<string, string> MaterialSource = new Dictionary<string, string>
        {
            ["wood"] = "Barkskin", ["stone"] = "Stoneskin", ["iron"] = "IronBody", ["brass"] = "ElementalBodyFire", ["gold"] = "PrismaticSpray",
            ["autumn"] = "Entangle", ["summer"] = "SpikeGrowth", ["adamantine"] = "SmitingAdamantite", ["greatergold"] = "PrismaticSpray", ["mithral"] = "Haste",
        };

        // 학파 특성 GUID → 원본 키(OppositionSchools.Rows 와 같은 짝 — 그 빌더가 이미 vanilla.Icon 을 넣고, Icons.Apply 가 같은 그림인지 확인한다).
        static readonly string[][] SchoolPairs =
        {
            new[] { Guids.SchoolAbjuration, "OppAbjuration" }, new[] { Guids.SchoolConjuration, "OppConjuration" }, new[] { Guids.SchoolDivination, "OppDivination" },
            new[] { Guids.SchoolEnchantment, "OppEnchantment" }, new[] { Guids.SchoolEvocation, "OppEvocation" }, new[] { Guids.SchoolIllusion, "OppIllusion" },
            new[] { Guids.SchoolNecromancy, "OppNecromancy" },
        };

        // 우리 블루프린트 GUID → 아이콘 원본 키.
        public static readonly IReadOnlyDictionary<string, string> Targets = BuildTargets();

        // 아이콘을 받지 않는 우리 블루프린트와 이유. Icons.Apply 가 이유마다 실제를 확인한다(아니면 던진다).
        public const string ReasonHidden = "hidden";
        public const string ReasonNoIconField = "no-icon-field"; // 형식에 m_Icon 필드가 없다(BlueprintUnitProperty·BlueprintAbilityAreaEffect, dnlib 2026-11-15) — Icons.Apply 가 필드 부재를 확인
        public const string ReasonArchetype = "archetype";       // 본편 아키타입도 아이콘이 없다(ChirurgeonArchetype 063440d5… m_Icon null, 시험 icons: jbp) — Icons.Apply 가 형식을 확인
        public const string ReasonUnit = "unit";                 // 유닛은 초상화를 쓴다(본편 CR6_GolemWood b02f074f… m_Icon null, 시험 icons: jbp) — Icons.Apply 가 BlueprintUnit 인지 확인
        public static readonly IReadOnlyDictionary<string, string> NoIcon = BuildNoIcon();

        static Dictionary<string, string> BuildTargets()
        {
            var t = new Dictionary<string, string>();
            Action<string, string> add = (g, k) => { if (t.ContainsKey(g)) throw new InvalidOperationException("Golemancer: IconTable target listed twice: " + g); t.Add(g, k); };
            add(Guids.OppositionSchoolSelection, "WizardSchools");
            foreach (var p in SchoolPairs) add(p[0], p[1]);
            add(Guids.BombHalfFeature, "AlchemistBombsResource");
            add(Guids.PetClass, "AnimalCompanionSelection");
            add(Guids.PetProgression, "AnimalCompanionSelection");
            add(Guids.GolemFeature, "AnimalCompanionSelection");
            add(Guids.CallGolem, "AnimalCompanionSelection");
            add(Guids.DismissGolem, "Barkskin");
            add(Guids.TauntBuff, "CavalierChallenge");
            foreach (var level in GolemRules.Dials.MaterialLevels) add(MaterialCatalog.SelectionGuid(level), "ElementalBodyEarth");
            foreach (var m in MaterialCatalog.Materials) add(MaterialCatalog.FeatureGuid(m), MaterialSourceOf(m.Id)); // 나무 = Guids.MaterialWoodFeature
            foreach (var key in MaterialCatalog.Keys)
            {
                add(MaterialCatalog.BuffGuid(key), MaterialSourceOf(MaterialCatalog.MaterialOfKey(key).Id));
                add(MaterialCatalog.SlamGuid(key), "SlamGargantuan");
            }
            add(Guids.CloneSplintering, "ThornBody");
            add(Guids.CloneAutumnAuraBuff, "Entangle");
            add(Guids.CloneSummerAuraBuff, "Entangle");
            add(Guids.CloneGrabFeature, "GrabTiger");
            add(Guids.StoneSlowAuraBuff, "Slow");
            add(Guids.FireRepairFeature, "FieryBody");
            add(Guids.PrecisionImmunityFeature, "UncannyDodge");
            add(Guids.FusionToggle, "CavalierChallenge");
            add(Guids.FusionControlBuff, "CavalierChallenge");
            add(Guids.FusionFeature, "CavalierChallenge");
            foreach (var g in new[] { Guids.RepairFeature, Guids.RepairAbility, Guids.RepairAbilityL1, Guids.RepairAbilityL2, Guids.RepairAbilityL3, Guids.RepairAbilityL4, Guids.RepairAbilityL5, Guids.RepairAbilityL6 }) add(g, "CureLightWounds");
            foreach (var g in new[] { Guids.SharedImmunityFeature, Guids.SharedImmunityAdamantine, Guids.SharedImmunityGreaterGold, Guids.SharedImmunityMithral }) add(g, "DeathWard");
            foreach (var g in new[] { Guids.OverdriveFeature, Guids.OverdriveAbility, Guids.OverdriveBuff }.Concat(OverdriveRules.AllVariantGuids).Concat(OverdriveRules.AllEffectGuids)) add(g, "MutagenStrength"); // 폭주 특성·능력·버프 + 변형 54 + 학파 효과 버프 8
            return t;
        }

        static string MaterialSourceOf(string id)
        {
            string k;
            if (!MaterialSource.TryGetValue(id, out k)) throw new KeyNotFoundException("Golemancer: IconTable has no icon source for material '" + id + "'");
            return k;
        }

        static Dictionary<string, string> BuildNoIcon()
        {
            var n = new Dictionary<string, string>();
            Action<string, string> add = (g, r) => { if (n.ContainsKey(g)) throw new InvalidOperationException("Golemancer: IconTable no-icon listed twice: " + g); n.Add(g, r); };
            add(Guids.Archetype, ReasonArchetype);
            add(Guids.GolemUnit, ReasonUnit);
            foreach (var g in new[] { Guids.BombHalfProperty, Guids.GolemLevelProperty, Guids.TauntArea, Guids.StoneSlowArea, Guids.SummerEntangleArea }) add(g, ReasonNoIconField);
            foreach (var g in new[] { Guids.TauntAreaBuff, Guids.TauntMarkerBuff }) add(g, ReasonHidden);
            foreach (var g in MaterialCatalog.StatFeatureGuids) add(g, ReasonHidden);                   // 펫·합체 수치 특성 26(HideInUI)
            foreach (var key in MaterialCatalog.Keys) add(MaterialCatalog.FusionFormGuid(key), ReasonHidden); // 합체 형태 13(HiddenInUi — 제어 버프가 보인다)
            foreach (var g in SlamRules.AllLimbGuids) add(g, ReasonHidden);                                     // 추가 슬램 사지 버프 27(HiddenInUi — 2026-11-16)
            return n;
        }

        public static IconSource SourceOf(string target)
        {
            string key;
            if (!Targets.TryGetValue(target, out key)) throw new KeyNotFoundException("Golemancer: no icon source for " + target);
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("Golemancer: icon source key '" + key + "' is in Sources " + hit.Length + " time(s)");
            return hit[0];
        }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리가 만드는 블루프린트 GUID 전체(시험: Guids 상수 전부 · 게임: BlueprintUtil.OwnBlueprints + AbilityClone 복제본).
        public static List<string> Problems(IEnumerable<string> own)
        {
            var p = new List<string>();
            if (own == null) { p.Add("no own GUID set"); return p; }
            var ownSet = new HashSet<string>(own);
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            if (Sources.Select(s => s.Key).Distinct().Count() != Sources.Count) p.Add("duplicate source key");
            if (Sources.Select(s => s.Guid).Distinct().Count() != Sources.Count) p.Add("duplicate source GUID");
            foreach (var s in Sources)
            {
                if (s.Guid == null || s.Guid.Length != 32 || s.Guid.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))) p.Add("source " + s.Key + " GUID '" + s.Guid + "' is not 32 lowercase hex");
                if (ownSet.Contains(s.Guid)) p.Add("source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.VanillaName)) p.Add("source " + s.Key + " has no reason / path / name");
                if (!Targets.Values.Contains(s.Key)) p.Add("source " + s.Key + " is unused");
            }
            foreach (var kv in Targets) if (!Sources.Any(s => s.Key == kv.Value)) p.Add("target " + kv.Key + " -> unknown source key " + kv.Value);
            var mats = MaterialCatalog.Materials.Select(m => m.Id).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (!mats.SequenceEqual(MaterialSource.Keys.OrderBy(x => x, StringComparer.Ordinal))) p.Add("material icon map [" + string.Join(",", MaterialSource.Keys.ToArray()) + "] != catalog materials [" + string.Join(",", mats) + "]");
            foreach (var g in Targets.Keys.Intersect(NoIcon.Keys)) p.Add(g + " is both an icon target and no-icon");
            foreach (var g in Targets.Keys.Concat(NoIcon.Keys)) if (!ownSet.Contains(g)) p.Add(g + " is in IconTable but not one of our blueprints");
            foreach (var g in ownSet) if (!Targets.ContainsKey(g) && !NoIcon.ContainsKey(g)) p.Add(g + " (ours) has neither an icon source nor a no-icon reason");
            foreach (var kv in NoIcon) if (kv.Value != ReasonHidden && kv.Value != ReasonNoIconField && kv.Value != ReasonArchetype && kv.Value != ReasonUnit) p.Add(kv.Key + " no-icon reason '" + kv.Value + "' unknown");
            return p;
        }
    }
}
