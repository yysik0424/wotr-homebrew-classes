using System;
using System.Collections.Generic;
using System.Linq;

namespace Spellfist
{
    // [순수] 아이콘 원장(2026-11-15 결함: 학파 선택·학파 특성 8·저수지 특성·익스플로잇 선택·11레벨 상급 익스플로잇 표식이 m_Icon 없이 만들어졌다 —
    // 예외가 없어 사람이 보기 전까지 몰랐다). 중장법사(BulwarkMage 84d70ed) IconTable 과 같은 구조.
    // 게임 형식 없음(modinfo testSources). 단일 원장(C4): 게임 쪽 Icons.Apply 가 이 표대로
    //   (1) Targets: 본편 원본의 m_Icon 을 넣는다(원본 없음 · 아이콘 null · 이미 넣은 값(신속·주먹 주문타격)과 다름 → 던진다),
    //   (2) CloneTargets: 익스플로잇 복제 37 = 원본 아이콘을 깊은 복제로 이미 가진다 — 원본의 m_Icon 과 같은 참조여야 한다(둘 다 null 인 숨김 비용 버프 포함, 쓰지 않음),
    //   (3) NoIcon: 이유가 거짓이면(숨김이 아님 · 필드가 있음 · 형식이 다름) 던진다, (4) 우리가 만든 블루프린트 전부가 정확히 한 곳에 있어야 하고 보이는 사실에 아이콘이 없으면 던진다.
    // 시험 `icons:` 가 같은 표를 Guids·ExploitGuids 상수 전부와 대조하고, 원본 jbp(blueprints.zip)의 AssetId·m_Icon 을 직독한다. 아이콘만 바꾼다 — 게임 동작 값은 건드리지 않는다.
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
            new IconSource("ArcaneSchoolPower", "3524a71d57d99bb4b835ad20582cf613", "BloodlineArcaneSchoolPowerSelection", "Classes/Sorcerer/ArcaneBloodline/BloodlineArcaneSchoolPowerSelection.jbp", "학파 선택 — 비전 학파 고르기"),
            new IconSource("SchoolAbjuration", "30f20e6f850519b48aa59e8c0ff66ae9", "SpecializationSchoolAbjuration", "Classes/Wizard/Abjuration/SpecializationSchoolAbjuration.jbp", "학파: 방호 — 위저드 방호 전문"),
            new IconSource("SchoolConjuration", "cee0f7edbd874a042952ee150f878b84", "SpecializationSchoolConjuration", "Classes/Wizard/Conjuration/SpecializationSchoolConjuration.jbp", "학파: 소환 — 위저드 소환 전문"),
            new IconSource("SchoolDivination", "54d21b3221ea82a4d90d5a91b7872f3d", "DivinationSchoolBaseFeature", "Classes/Wizard/Divination/DivinationSchoolBaseFeature.jbp", "학파: 예지 — 위저드 예지 학파"),
            new IconSource("SchoolEnchantment", "9e4c4799735ae9c45964e9113107ef02", "EnchantmentSchoolBaseFeature", "Classes/Wizard/Enchantment/EnchantmentSchoolBaseFeature.jbp", "학파: 환혹 — 위저드 환혹 학파"),
            new IconSource("SchoolEvocation", "c46512b796216b64899f26301241e4e6", "SpecializationSchoolEvocation", "Classes/Wizard/Evocation/SpecializationSchoolEvocation.jbp", "학파: 방출 — 위저드 방출 전문"),
            new IconSource("SchoolIllusion", "9be5e050244352d43a1cb50aad8d548f", "IllusionSchoolBaseFeature", "Classes/Wizard/Illusion/IllusionSchoolBaseFeature.jbp", "학파: 환영 — 위저드 환영 학파"),
            new IconSource("SchoolNecromancy", "927707dce06627d4f880c90b5575125f", "NecromancySchoolBaseFeature", "Classes/Wizard/Necromancy/NecromancySchoolBaseFeature.jbp", "학파: 사령 — 위저드 사령 학파"),
            new IconSource("SchoolTransmutation", "c459c8200e666ef4c990873d3e501b91", "SpecializationSchoolTransmutation", "Classes/Wizard/Transmutation/SpecializationSchoolTransmutation.jbp", "학파: 변환 — 위저드 변환 전문"),
            new IconSource("ArcaneReservoir", "55db1859bd72fd04f9bd3fe1f10e4cbb", "ArcanistArcaneReservoirFeature", "Classes/Arcanist/ArcanistArcaneReservoirFeature.jbp", "저수지 — 아카니스트 비전 저수지"),
            new IconSource("ExploitSelection", Guids.Game.ArcanistExploitSelection, "ArcanistExploitSelection", "Classes/Arcanist/ArcanistExploitSelection.jbp", "익스플로잇 선택 · 11레벨 상급 익스플로잇 표식 — 아카니스트 익스플로잇 선택"),
            new IconSource("QuickenedArcana", Guids.Game.QuickenedArcanaAbility, "QuickenedArcanaAbility", "Classes/Magus/Arcanas/QuickenedArcana/QuickenedArcanaAbility.jbp", "신속(버프·토글·특성) — Quicken 이 이미 복사(같은 값이어야 함)"),
            new IconSource("SpellStrike", Guids.Game.SpellStrikeAbility, "SpellStrikeAbility", "Classes/Magus/SpellStrike/SpellStrikeAbility.jbp", "주먹 주문타격 — FistSpellstrike 가 이미 복사(같은 값이어야 함)"),
        };

        // 우리 블루프린트 GUID → 아이콘 원본 키.
        public static readonly IReadOnlyDictionary<string, string> Targets = BuildTargets();

        // 익스플로잇 복제 → 그 원본(ExploitGuids.Chain). 쓰지 않고, 복제의 m_Icon 이 원본의 m_Icon 과 같은 참조인지만 본다.
        public static readonly IReadOnlyDictionary<string, string> CloneTargets = ExploitGuids.Chain.ToDictionary(r => r.clone, r => r.source);

        public const string ReasonHidden = "hidden";
        public const string ReasonNoIconField = "no-icon-field";   // 형식에 m_Icon 필드 자체가 없다(BlueprintSpellbook 62 · BlueprintSpellList 102 — jbp 전부 m_Icon 키 없음, blueprints.zip 2026-11-15) — Icons.Apply 가 필드 부재를 확인
        public const string ReasonArchetype = "archetype";          // 아키타입: 본편 아키타입 213개 중 212개가 m_Icon null(blueprints.zip 2026-11-15 전수) — Icons.Apply 가 형식을 확인
        public static readonly IReadOnlyDictionary<string, string> NoIcon = BuildNoIcon();

        static Dictionary<string, string> BuildTargets()
        {
            var t = new Dictionary<string, string>();
            Action<string, string> add = (g, k) => { if (t.ContainsKey(g)) throw new InvalidOperationException("Spellfist: IconTable target listed twice: " + g); t.Add(g, k); };
            add(Guids.SchoolSelection, "ArcaneSchoolPower");
            add(Guids.SchoolAbjurationFeature, "SchoolAbjuration"); add(Guids.SchoolConjurationFeature, "SchoolConjuration"); add(Guids.SchoolDivinationFeature, "SchoolDivination");
            add(Guids.SchoolEnchantmentFeature, "SchoolEnchantment"); add(Guids.SchoolEvocationFeature, "SchoolEvocation"); add(Guids.SchoolIllusionFeature, "SchoolIllusion");
            add(Guids.SchoolNecromancyFeature, "SchoolNecromancy"); add(Guids.SchoolTransmutationFeature, "SchoolTransmutation");
            add(Guids.ReservoirFeature, "ArcaneReservoir");
            add(Guids.ExploitSelection, "ExploitSelection"); add(Guids.GreaterExploitsMarker11, "ExploitSelection");
            add(Guids.QuickenBuff, "QuickenedArcana"); add(Guids.QuickenToggle, "QuickenedArcana"); add(Guids.QuickenFeature, "QuickenedArcana");
            add(Guids.FistSpellstrikeFeature, "SpellStrike");
            return t;
        }

        static Dictionary<string, string> BuildNoIcon()
        {
            var n = new Dictionary<string, string>();
            Action<string, string> add = (g, r) => { if (n.ContainsKey(g)) throw new InvalidOperationException("Spellfist: IconTable no-icon listed twice: " + g); n.Add(g, r); };
            add(Guids.Archetype, ReasonArchetype);
            add(Guids.Spellbook, ReasonNoIconField);
            foreach (var g in new[] { Guids.EmptySpellList, Guids.SchoolListAbjuration, Guids.SchoolListConjuration, Guids.SchoolListDivination, Guids.SchoolListEnchantment,
                Guids.SchoolListEvocation, Guids.SchoolListIllusion, Guids.SchoolListNecromancy, Guids.SchoolListTransmutation }) add(g, ReasonNoIconField);
            // 신속 상한 버프 9 — Quicken.BuildCap 이 m_Flags = HiddenInUi 로 만들고 되읽는다(아이콘은 넣어 두지만 보이지 않는다).
            foreach (var g in new[] { Guids.QuickenCap1, Guids.QuickenCap2, Guids.QuickenCap3, Guids.QuickenCap4, Guids.QuickenCap5, Guids.QuickenCap6, Guids.QuickenCap7, Guids.QuickenCap8, Guids.QuickenCap9 }) add(g, ReasonHidden);
            return n;
        }

        public static IconSource SourceOf(string target)
        {
            string key;
            if (!Targets.TryGetValue(target, out key)) throw new KeyNotFoundException("Spellfist: no icon source for " + target);
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("Spellfist: icon source key '" + key + "' is in Sources " + hit.Length + " time(s)");
            return hit[0];
        }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리가 만드는 블루프린트 GUID 전체(시험: Guids + ExploitGuids 상수 · 게임: BlueprintUtil 이 실제로 만든 것 전부).
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
                Guid parsed;
                if (s.Guid == null || !Guid.TryParseExact(s.Guid, "N", out parsed) || s.Guid != s.Guid.ToLowerInvariant()) p.Add("source " + s.Key + " GUID '" + s.Guid + "' is not 32 lowercase hex");
                if (ownSet.Contains(s.Guid)) p.Add("source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.VanillaName)) p.Add("source " + s.Key + " has no reason / path / name");
                if (!Targets.Values.Contains(s.Key)) p.Add("source " + s.Key + " is unused");
            }
            foreach (var kv in Targets) if (!Sources.Any(s => s.Key == kv.Value)) p.Add("target " + kv.Key + " -> unknown source key " + kv.Value);
            foreach (var kv in CloneTargets) if (ownSet.Contains(kv.Value)) p.Add("clone " + kv.Key + " -> source " + kv.Value + " is one of our own blueprints");
            var count = new Dictionary<string, int>();
            foreach (var g in Targets.Keys.Concat(CloneTargets.Keys).Concat(NoIcon.Keys)) { int c; count.TryGetValue(g, out c); count[g] = c + 1; }
            foreach (var kv in count) if (kv.Value > 1) p.Add(kv.Key + " is classified " + kv.Value + " times");
            foreach (var g in count.Keys) if (!ownSet.Contains(g)) p.Add(g + " is in IconTable but not one of our blueprints");
            foreach (var g in ownSet) if (!count.ContainsKey(g)) p.Add(g + " (ours) has neither an icon source nor a no-icon reason");
            foreach (var kv in NoIcon) if (kv.Value != ReasonHidden && kv.Value != ReasonNoIconField && kv.Value != ReasonArchetype) p.Add(kv.Key + " no-icon reason '" + kv.Value + "' unknown");
            return p;
        }
    }
}
