using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
    // [순수] 아이콘 원장(2026-11-15 결함: 흡수 사실 SpiritMirror·랭크 진행표·펫 진행표가 m_Icon 없이 보이는 특성이었고, 선택지·영병 특성의 본편 아이콘 복사에는 null 검사가 없었다 —
    // 예외가 없어 사람이 보기 전까지 몰랐다). 중장법사(BulwarkMage 84d70ed) IconTable 과 같은 구조.
    // 게임 형식 없음(modinfo testSources). 단일 원장(C4): 게임 쪽 Icons.Apply 가 이 표대로
    //   (1) Targets: 본편 원본의 m_Icon 을 우리 블루프린트에 넣는다(원본 없음 · 아이콘 null · 이미 넣은 값과 다름 → 던진다),
    //   (2) TypeIconTargets: 영병 특성 59개 = 그 범주 무기 유형(BlueprintWeaponType)의 아이콘(BuildFeature 가 이미 복사 — 같은 값이어야 하고 null 이면 던진다),
    //   (3) ItemIconTargets: 시작 무기·두 번째 머리 = 아이템 m_Icon 이 비면 엔진이 Type.Icon 을 쓴다(BlueprintItemWeapon.get_Icon, IL — PhantomBlade task-3 보고) → 공개 Icon 이 null 이면 던진다(쓰지 않음),
    //   (4) NoIcon: 「숨김」 이라고 적은 것이 실제 숨김이 아니면 · 이유와 형식이 다르면 던진다,
    //   (5) 우리가 만든 블루프린트 전부가 위 넷 중 정확히 한 곳에 있어야 하고, 마지막에 보이는 사실(특성·선택·버프·능력)에 아이콘이 없으면 던진다.
    // 시험 `icons:` 가 같은 표를 Guids 상수 + 파생 GUID 전부와 대조하고, 원본 jbp(blueprints.zip)의 AssetId·m_Icon 을 직독한다. 아이콘만 바꾼다 — 게임 동작 값은 건드리지 않는다.
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
            new IconSource("ArcaneWeaponSwitch", Guids.Game.ArcaneWeaponSwitchAbility, "ArcaneWeaponSwitchAbility", "Classes/Magus/ArcanePool/ArcaneWeaponSwitchAbility.jbp", "강신 I~IV·융합·스피릿 배러지(자원·능력·버프·특성) — 무기에 힘을 깃들이는 그림. Possession·Fusion·SpiritBarrage 이 이미 복사(같은 값이어야 함)"),
            new IconSource("WildlandCompanionSelection", Guids.Game.AnimalCompanionSelectionWildlandShaman, "AnimalCompanionSelectionWildlandShaman", "Classes/Shaman/Archetypes/WildlandShaman/AnimalCompanionSelectionWildlandShaman.jbp", "영병 선택지(이미 복사 원본) · 랭크 진행표 · 펫 진행표 — 동료(펫) 계열 그림. 진행표 복사 원본 둘은 m_Icon 이 null(직독 2026-11-15)"),
            new IconSource("PaladinWeaponBond", "1c7cdc1605554954f838d85bbdd22d90", "WeaponBondFeature", "Classes/Paladin/WeaponBond/WeaponBondFeature.jbp", "흡수 사실(영병이 주인 무기의 마법 부여를 받아들임) — 무기와 맺은 유대(마법 부여 공유) 그림"),
        };

        // 우리 블루프린트 GUID → 아이콘 원본 키.
        public static readonly IReadOnlyDictionary<string, string> Targets = BuildTargets();

        // 범주 행은 인자로 받는다: SpiritCategories.Rows 를 읽는 곳은 SpiritBlueprints.Build 한 곳뿐이어야 한다(시험 `wiring: one loop builds all`).
        // 게임 = Icons.Apply 가 SpiritBlueprints.Entries 의 Row(실제로 만든 행) · 시험 = SpiritCategories.Rows.
        // 영병 특성(파생 GUID feature) → 그 범주 무기 유형 GUID(행의 TypeGuid). 시험이 유형 jbp 59개의 m_Icon 을 직독한다.
        public static Dictionary<string, string> TypeIconTargets(IEnumerable<CategoryRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            return rows.ToDictionary(r => DerivedGuids.For("feature", r.Category), r => r.TypeGuid);
        }

        public const string ReasonHidden = "hidden";
        public const string ReasonArchetype = "archetype";   // 아키타입: 본편 아키타입도 아이콘이 없다(SpiritHunterArchetype 39b2fd50… m_Icon null, blueprints.zip 2026-11-15) — Icons.Apply 가 형식을 확인
        public const string ReasonPetClass = "pet-class";    // 펫 직업: 복제 원본 AnimalCompanionClass 26b10d43… 도 m_Icon null(blueprints.zip 2026-11-15), 사실(UnitFact)이 아니다 — Icons.Apply 가 형식을 확인
        public const string ReasonUnit = "unit";             // 영병 유닛: UI 는 초상화(m_Portrait)를 쓴다, 복제 원본 AnimalCompanionUnit 67c08507… 도 m_Icon null(blueprints.zip 2026-11-15) — Icons.Apply 가 형식을 확인
        public const string ReasonPortrait = "portrait";     // 스피릿 웨폰 초상화(BlueprintPortrait, 2026-11-17): 사실이 아니고 m_Icon 필드가 없다(그림 = PortraitData 스프라이트) — Icons.Apply 가 형식을 확인
        public static readonly IReadOnlyDictionary<string, string> NoIcon = BuildNoIcon();

        // 범주별 no-icon(영병 유닛) — 고정 NoIcon 과 합쳐 쓴다.
        public static Dictionary<string, string> UnitNoIcon(IEnumerable<CategoryRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            return rows.ToDictionary(r => DerivedGuids.For("unit", r.Category), r => ReasonUnit);
        }

        // 성장 특기 단계 특성(소유자 결정 2026-11-18) — 주술사 쪽 grant · 펫 쪽 petfeat, 범주 x 단계마다 둘. 둘 다 숨김(SpiritBlueprints.BuildTierFeatures: HideInUI = true).
        // 화면에 보이는 것은 그 컴포넌트가 붙이는 본편 특기(무기 전문화 등) 자신이다 — 본편 아이콘 그대로.
        public static Dictionary<string, string> TierNoIcon(IEnumerable<CategoryRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            var t = new Dictionary<string, string>();
            foreach (var r in rows)
                foreach (var tier in SpiritRules.SpiritGrowthFeats)
                {
                    t.Add(DerivedGuids.For(tier.GrantKind, r.Category), ReasonHidden);
                    t.Add(DerivedGuids.For(tier.PetKind, r.Category), ReasonHidden);
                }
            return t;
        }

        static Dictionary<string, string> BuildTargets()
        {
            var t = new Dictionary<string, string>();
            Action<string, string> add = (g, k) => { if (t.ContainsKey(g)) throw new InvalidOperationException("WeaponSpiritShaman: IconTable target listed twice: " + g); t.Add(g, k); };
            foreach (var g in new[] { Guids.PossessionResource, Guids.PossessionAbility, Guids.PossessionBuff1, Guids.PossessionBuff2, Guids.PossessionBuff3,
                Guids.PossessionFeature1, Guids.PossessionFeature2, Guids.PossessionFeature3, Guids.FusionBuff, Guids.FusionAbility, Guids.PossessionFeature4,
                Guids.SpiritBarrageResource, Guids.SpiritBarrageAbility, Guids.SpiritBarrageFeature }) add(g, "ArcaneWeaponSwitch"); // 스피릿 배러지(2026-11-16)도 같은 그림 — SpiritBarrage.Build 가 강신 능력 아이콘을 복사한다
            add(Guids.SpiritSelection, "WildlandCompanionSelection");
            add(Guids.RankProgression, "WildlandCompanionSelection");
            add(Guids.PetProgression, "WildlandCompanionSelection");
            add(Guids.SpiritMirrorFeature, "PaladinWeaponBond");
            return t;
        }

        // 시작 무기·두 번째 머리(파생 GUID weapon·weapon2) → 아이콘 = 엔진 폴백(아이템 m_Icon ?? 유형 Icon). 값 = 그 무기의 유형 GUID.
        public static Dictionary<string, string> ItemIconTargets(IEnumerable<CategoryRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            var t = new Dictionary<string, string>();
            foreach (var r in rows)
            {
                t.Add(DerivedGuids.For("weapon", r.Category), r.TypeGuid);
                // 두 번째 머리 = 그 머리 아이템 자신의 유형(첫 머리와 다를 수 있음 — CategoryRow.SecondTypeGuid). 없으면 던진다(조용히 첫 머리 유형으로 대체하지 않음).
                if (r.Double)
                {
                    if (string.IsNullOrEmpty(r.SecondTypeGuid)) throw new InvalidOperationException("IconTable: double row " + r.Category + " has no SecondTypeGuid");
                    t.Add(DerivedGuids.For("weapon2", r.Category), r.SecondTypeGuid);
                }
            }
            return t;
        }

        static Dictionary<string, string> BuildNoIcon()
        {
            var n = new Dictionary<string, string>();
            Action<string, string> add = (g, r) => { if (n.ContainsKey(g)) throw new InvalidOperationException("WeaponSpiritShaman: IconTable no-icon listed twice: " + g); n.Add(g, r); };
            add(Guids.Archetype, ReasonArchetype);
            add(Guids.PetClass, ReasonPetClass);
            add(Guids.SlotLockFeature, ReasonHidden);      // PetClass.Describe: HideInUI = true
            add(Guids.ProficiencyFeature, ReasonHidden);   // PetClass.Describe: HideInUI = true
            add(Guids.SpiritPortrait, ReasonPortrait);     // SpiritPortrait.Build — 초상화 블루프린트
            return n;
        }

        public static IconSource SourceOf(string target)
        {
            string key;
            if (!Targets.TryGetValue(target, out key)) throw new KeyNotFoundException("WeaponSpiritShaman: no icon source for " + target);
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("WeaponSpiritShaman: icon source key '" + key + "' is in Sources " + hit.Length + " time(s)");
            return hit[0];
        }

        static bool IsHex32(string g) { return g != null && g.Length == 32 && g.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리가 만드는 블루프린트 GUID 전체(시험: Guids 상수 + 파생 GUID · 게임: BlueprintUtil 이 실제로 만든 것 전부), rows = 범주 행(위 주석).
        public static List<string> Problems(IEnumerable<string> own, IEnumerable<CategoryRow> rows)
        {
            var p = new List<string>();
            if (own == null) { p.Add("no own GUID set"); return p; }
            if (rows == null) { p.Add("no category rows"); return p; }
            var rowList = rows.ToList();
            if (rowList.Count == 0) p.Add("category rows are empty");
            var typeT = TypeIconTargets(rowList); var itemT = ItemIconTargets(rowList); var unitN = UnitNoIcon(rowList); var tierN = TierNoIcon(rowList);
            var ownSet = new HashSet<string>(own);
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            if (Sources.Select(s => s.Key).Distinct().Count() != Sources.Count) p.Add("duplicate source key");
            if (Sources.Select(s => s.Guid).Distinct().Count() != Sources.Count) p.Add("duplicate source GUID");
            foreach (var s in Sources)
            {
                if (!IsHex32(s.Guid)) p.Add("source " + s.Key + " GUID '" + s.Guid + "' is not 32 lowercase hex");
                if (ownSet.Contains(s.Guid)) p.Add("source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.VanillaName)) p.Add("source " + s.Key + " has no reason / path / name");
                if (!Targets.Values.Contains(s.Key)) p.Add("source " + s.Key + " is unused");
            }
            foreach (var kv in Targets) if (!Sources.Any(s => s.Key == kv.Value)) p.Add("target " + kv.Key + " -> unknown source key " + kv.Value);
            foreach (var kv in typeT.Concat(itemT)) if (!IsHex32(kv.Value) || ownSet.Contains(kv.Value)) p.Add(kv.Key + " -> weapon type '" + kv.Value + "' is not a vanilla 32-hex GUID");
            var lists = new IEnumerable<string>[] { Targets.Keys, typeT.Keys, itemT.Keys, NoIcon.Keys, unitN.Keys, tierN.Keys };
            var count = new Dictionary<string, int>();
            foreach (var l in lists) foreach (var g in l) { int c; count.TryGetValue(g, out c); count[g] = c + 1; }
            foreach (var kv in count) if (kv.Value > 1) p.Add(kv.Key + " is classified " + kv.Value + " times");
            foreach (var g in count.Keys) if (!ownSet.Contains(g)) p.Add(g + " is in IconTable but not one of our blueprints");
            foreach (var g in ownSet) if (!count.ContainsKey(g)) p.Add(g + " (ours) has neither an icon source nor a no-icon reason");
            foreach (var kv in NoIcon.Concat(unitN).Concat(tierN)) if (kv.Value != ReasonHidden && kv.Value != ReasonArchetype && kv.Value != ReasonPetClass && kv.Value != ReasonUnit && kv.Value != ReasonPortrait) p.Add(kv.Key + " no-icon reason '" + kv.Value + "' unknown");
            return p;
        }
    }
}
