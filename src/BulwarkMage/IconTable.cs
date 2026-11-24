using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
    // [순수] 아이콘 원장(2026-11-15 결함: 반격 블루프린트 23개 등 m_Icon 을 아무도 안 넣어서 선택 목록·행동 막대·초상 버프가 빈칸이었다 — 예외가 없어 사람이 보기 전까지 몰랐다).
    // 게임 형식 없음(modinfo testSources). 단일 원장(C4): 게임 쪽 Icons.Apply 가 이 표대로 본편 원본의 m_Icon 을 우리 블루프린트에 넣고,
    //   (1) 원본이 없거나 아이콘이 null 이면 던진다(BuildFailed), (2) 우리가 만든 블루프린트 전부가 Rows 또는 NoIcon 에 정확히 한 번 있어야 한다(아니면 던진다),
    //   (3) NoIcon 에 적은 「숨김」이 실제로 숨김이 아니면 던진다, (4) 보이는 사실(특성·선택·버프·능력·토글)에 아이콘이 없으면 던진다.
    // 시험 `icons:` 가 같은 표를 Guids 상수 전부와 대조하고, 원본 jbp(blueprints.zip)의 AssetId·m_Icon 을 직독한다.
    // 아이콘만 바꾼다 — 게임 동작 값은 건드리지 않는다.
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
            new IconSource("ShieldBash", "121811173a614534e8720d7550aae253", "ShieldBashFeature", "Feats/ShieldBash/ShieldBashFeature.jbp", "방패 반격 공용(자원·1레벨 특성·배우기 선택) — 방패로 되치는 그림"),
            new IconSource("MagicMissile", "4ac47ddb9fa1eaf43a1b6809980cfbd2", "MagicMissile", "Spells/Level1/MagicMissile.jbp", "E1 역장 반격 — 대표 역장 주문"),
            new IconSource("ShockingGrasp", "ab395d2335d3f384e99dddee8562978f", "ShockingGraspCast", "Spells/Level1/ShockingGraspCast.jbp", "E2 전격 반격 — 근접 전기 주문"),
            new IconSource("RayOfFrost", "9af2ab69df6538f4793b2f9c3cc85603", "RayOfFrost", "Spells/Level0/RayOfFrost.jbp", "E3 서리 반격 — 냉기 주문"),
            new IconSource("BullRush", "7ab6f70c996fe9b4597b8332f0a3af5f", "BullRushAction", "Feats/Maneuvers/BullRush/BullRushAction.jbp", "E4 밀쳐내기 — 밀어내기 전투 기동"),
            new IconSource("Slow", "f492622e473d34747806bdb39356eb89", "Slow", "Spells/Level3/Slow.jbp", "E5 감속 반격 — 감속 주문(효과 버프 SlowBuff 의 주문)"),
            new IconSource("Daze", "55f14bc84d7c85446b07a1b5dd6b2b4c", "Daze", "Spells/Level0/Daze.jbp", "E6 현혹 반격 — 멍해짐 주문(효과 버프 DazeBuff 의 주문)"),
            new IconSource("DispelMagicTarget", "143775c49ae6b7446b805d3b2e702298", "DispelMagicTarget", "Spells/Level3/DispelMagicTarget.jbp", "E7 해제 반격 — 실제로 시전하는 주문 그 자체"),
            new IconSource("ItemBondFeature", "2fb5e65bd57caa943b45ee32d825e9b9", "ItemBondFeature", "Classes/Wizard/ArcaneBond/ItemBond/ItemBondFeature.jbp", "유대 방패 특성·자원 — 위저드 물건 유대"),
            new IconSource("ItemBondAbility", "e5dcf71e02e08fc448d9745653845df1", "ItemBondAbility", "Classes/Wizard/ArcaneBond/ItemBond/ItemBondAbility.jbp", "유대 방패 회복 능력 — 복사 원본 그대로(BondedShield 가 이미 복사, 같은 값이어야 함)"),
            new IconSource("TowerShieldTraining", "d2dbc71b45efa8749bd9c1924534b2bb", "TowerShieldTraining", "Classes/Fighter/Archetypes/TowerShieldSpecialist/TowerShieldTraining.jbp", "타워실드 훈련 복제 — 복제 원본 그대로(TowerShield 가 이미 복사)"),
            new IconSource("TowerShieldTrainingBuff", "e85647881f1d4f849799ae8b108802fa", "TowerShieldTrainingBuff", "Classes/Fighter/Archetypes/TowerShieldSpecialist/TowerShieldTrainingBuff.jbp", "타워실드 훈련 버프 복제 — 복제 원본 그대로(TowerShield 가 이미 복사)"),
            new IconSource("CombatCasting", "06964d468fde1dc4aa71a92ea04d930d", "CombatCasting", "Feats/CombatCasting.jbp", "철벽 집중 — 집중 판정 특기"),
            new IconSource("QuarterstaffProficiency", "aed4f88b52ae0fb468895f90da854ad4", "QuarterstaffProficiency", "Feats/Proficiencies/QuarterstaffProficiency.jbp", "한손 지팡이 — 쿼터스태프 숙련"),
            new IconSource("HeavyArmorProficiency", "1b0f68188dcc435429fb87a022239681", "HeavyArmorProficiency", "Classes/Basic/Proficiencies/HeavyArmorProficiency.jbp", "중장 숙련(갑옷·방패 숙련) — 중갑 숙련"),
        };

        // 반격 효과 → 아이콘 원본 키(배움·토글·토글 버프 셋이 같은 그림). 효과 집합 = RebukeTable.Effects(어긋나면 Problems 가 잡는다).
        public static readonly IReadOnlyDictionary<string, string> RebukeEffectSource = new Dictionary<string, string>
        {
            ["E1"] = "MagicMissile", ["E2"] = "ShockingGrasp", ["E3"] = "RayOfFrost", ["E4"] = "BullRush", ["E5"] = "Slow", ["E6"] = "Daze", ["E7"] = "DispelMagicTarget",
        };

        // 반격 역할 중 아이콘을 받는 것(보이는 것 + 자원). 나머지 역할(Gate·Ready·Spent)은 숨김 — NoIcon.
        static readonly RebukeRole[] RebukeIconRoles = { RebukeRole.Learn, RebukeRole.Toggle, RebukeRole.ToggleBuff };

        // 우리 블루프린트 GUID → 아이콘 원본 키.
        public static readonly IReadOnlyDictionary<string, string> Targets = BuildTargets();

        // 아이콘을 받지 않는 우리 블루프린트와 이유. 「숨김」이라고 적은 것은 Icons.Apply 가 실제 숨김인지 확인한다(아니면 던진다).
        public const string ReasonHidden = "hidden";
        public const string ReasonNoIconField = "no-icon-field";   // 형식에 m_Icon 필드 자체가 없다(BlueprintItemEnchantment 계열, dnlib 2026-11-15) — Icons.Apply 가 필드 부재를 확인
        public const string ReasonArchetype = "archetype";          // 아키타입: 본편 아키타입도 아이콘이 없다(ArmoredBattlemageArchetype 67ec8dca… m_Icon null, blueprints.zip 2026-11-15) — Icons.Apply 가 형식을 확인
        public static readonly IReadOnlyDictionary<string, string> NoIcon = BuildNoIcon();

        static Dictionary<string, string> BuildTargets()
        {
            var t = new Dictionary<string, string>();
            Action<string, string> add = (g, k) => { if (t.ContainsKey(g)) throw new InvalidOperationException("BulwarkMage: IconTable target listed twice: " + g); t.Add(g, k); };
            add(Guids.RebukeResource, "ShieldBash");
            add(Guids.RebukeBaseFeature, "ShieldBash");
            add(Guids.RebukeSelection, "ShieldBash");
            foreach (var kv in RebukeEffectSource)
                foreach (var role in RebukeIconRoles) add(RebukePlan.GuidOf(role, kv.Key), kv.Value);
            add(Guids.BondResource, "ItemBondFeature");
            add(Guids.BondFeature, "ItemBondFeature");
            add(Guids.BondAbility, "ItemBondAbility");
            add(Guids.TowerShieldTraining, "TowerShieldTraining");
            add(Guids.TowerShieldTrainingBuff, "TowerShieldTrainingBuff");
            add(Guids.IronConcentrationFeature, "CombatCasting");
            add(Guids.OneHandStaffFeature, "QuarterstaffProficiency");
            add(Guids.ProficiencyFeature, "HeavyArmorProficiency");
            return t;
        }

        static Dictionary<string, string> BuildNoIcon()
        {
            var n = new Dictionary<string, string>();
            Action<string, string> add = (g, r) => { if (n.ContainsKey(g)) throw new InvalidOperationException("BulwarkMage: IconTable no-icon listed twice: " + g); n.Add(g, r); };
            add(Guids.Archetype, ReasonArchetype);
            add(Guids.RebukeSpent, ReasonHidden);
            foreach (var e in RebukeEffectSource.Keys)
            {
                add(RebukePlan.GuidOf(RebukeRole.Gate, e), ReasonHidden);
                add(RebukePlan.GuidOf(RebukeRole.Ready, e), ReasonHidden);
            }
            add(Guids.BondAbilityFact, ReasonHidden);
            foreach (var g in new[] { Guids.BondEnhance1, Guids.BondEnhance2, Guids.BondEnhance3, Guids.BondEnhance4, Guids.BondEnhanceBuff1, Guids.BondEnhanceBuff2, Guids.BondEnhanceBuff3, Guids.BondEnhanceBuff4 }) add(g, ReasonHidden);
            foreach (var g in new[] { Guids.BondEnchant1, Guids.BondEnchant2, Guids.BondEnchant3, Guids.BondEnchant4 }) add(g, ReasonNoIconField);   // 마법부여(BlueprintItemEnchantment)에는 m_Icon 이 없다
            return n;
        }

        public static IconSource SourceOf(string target)
        {
            string key;
            if (!Targets.TryGetValue(target, out key)) throw new KeyNotFoundException("BulwarkMage: no icon source for " + target);
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("BulwarkMage: icon source key '" + key + "' is in Sources " + hit.Length + " time(s)");
            return hit[0];
        }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리가 만드는 블루프린트 GUID 전체(시험: Guids 상수 전부 · 게임: BlueprintUtil 이 실제로 만든 것 전부).
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
            var effects = RebukeTable.Effects.Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (!effects.SequenceEqual(RebukeEffectSource.Keys.OrderBy(x => x, StringComparer.Ordinal))) p.Add("rebuke effect icon map [" + string.Join(",", RebukeEffectSource.Keys.ToArray()) + "] != RebukeTable effects [" + string.Join(",", effects) + "]");
            foreach (var g in Targets.Keys.Intersect(NoIcon.Keys)) p.Add(g + " is both an icon target and no-icon");
            foreach (var g in Targets.Keys.Concat(NoIcon.Keys)) if (!ownSet.Contains(g)) p.Add(g + " is in IconTable but not one of our blueprints");
            foreach (var g in ownSet) if (!Targets.ContainsKey(g) && !NoIcon.ContainsKey(g)) p.Add(g + " (ours) has neither an icon source nor a no-icon reason");
            foreach (var kv in NoIcon) if (kv.Value != ReasonHidden && kv.Value != ReasonNoIconField && kv.Value != ReasonArchetype) p.Add(kv.Key + " no-icon reason '" + kv.Value + "' unknown");
            return p;
        }
    }
}
