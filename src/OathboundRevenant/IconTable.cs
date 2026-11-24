using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
    // [순수] 아이콘 원장(2026-11-15 결함: 레벨표 특성 껍데기·재주 선택·재주 20·채널·공포·오라 효과·하루 자원·유대 특성 등이 m_Icon 없이 만들어졌고,
    // 안수 복제·언데드 지배 복제·유대 선택지의 본편 아이콘 복사에는 null 검사가 없었다 — 예외가 없어 사람이 보기 전까지 몰랐다). 중장법사(BulwarkMage 84d70ed) IconTable 과 같은 구조.
    // 게임 형식 없음(modinfo testSources). 단일 원장(C4): 게임 쪽 Icons.Apply 가 이 표대로 본편 원본의 m_Icon 을 우리 블루프린트에 넣고,
    //   (1) 원본이 없거나 아이콘이 null 이면 · 이미 들어 있는 값(복제 복사)과 다르면 던진다(BuildFailed), (2) 우리가 만든 블루프린트 전부가 Targets 또는 NoIcon 에 정확히 한 번 있어야 한다,
    //   (3) NoIcon 의 이유가 거짓이면(숨김이 아님 · 형식이 다름) 던진다, (4) 보이는 사실(특성·선택·버프·능력·토글)에 아이콘이 없으면 던진다.
    // 유대 선택지 4개는 불꽃 선택지를 통째로 복사하면서 불꽃 아이콘까지 따라왔다 — DivineBond 가 그 아이콘만 비우고 이 표가 주제에 맞는 그림을 넣는다(2026-11-15).
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
            new IconSource("AnimateDead", "4b76d32feb089ad4499c3a1ce8e1ac27", "AnimateDead", "Spells/Level4/AnimateDead.jbp", "언데드 몸 — 되살아난 망자"),
            new IconSource("Regeneration", "613a8f55cd2e4570a45cbac3b0feb912", "RegenerationFeature", "Classes/Slayer/Archetypes/Bloodseeker/NocturnalSelections/RegenerationFeature.jbp", "맹세의 재생 — 재생"),
            new IconSource("SmiteEvil", Guids.Game.SmiteEvilFeature, "SmiteEvilFeature", "Classes/Paladin/SmiteEvil/SmiteEvilFeature.jbp", "스마이트: 음에너지 — 팔라딘 스마이트"),
            new IconSource("SmiteAdditional", Guids.Game.SmiteEvilAdditionalUse, "SmiteEvilAdditionalUse", "Classes/Paladin/SmiteEvil/SmiteEvilAdditionalUse.jbp", "죽음의 표식 — 스마이트가 옮겨 감"),
            new IconSource("LayOnHandsFeature", Guids.Game.LayOnHandsFeature, "LayOnHandsFeature", "Classes/Paladin/LayOnHands/LayOnHandsFeature.jbp", "망자의 손 — 안수 특성"),
            new IconSource("LayOnHandsOthers", Guids.Game.LayOnHandsOthers, "LayOnHandsOthers", "Classes/Paladin/LayOnHands/LayOnHandsOthers.jbp", "아군 안수 복제 — 복제 원본 그대로(CopyAbilityFields 가 이미 복사)"),
            new IconSource("Shaken", Guids.Game.Shaken, "Shaken", "Buffs/Shaken.jbp", "묘지의 공포 — 스마이트 대상이 흔들림"),
            new IconSource("CircleOfDeath", "a89dcbbab8f40e44e920cc60636097cf", "CircleOfDeath", "Spells/Level6/CircleOfDeath.jbp", "죽음의 오라(특성·효과) — 죽음의 원"),
            new IconSource("ChannelNegative", Guids.Game.ChannelNegativeEnergy, "ChannelNegativeEnergy", "Classes/Cleric/ChannelNegativeEnergy.jbp", "음에너지 채널(특성·능력) — 클레릭 음에너지 채널"),
            new IconSource("Enervation", "f34fb78eaaec141469079af124bcfa0f", "Enervation", "Spells/Level4/Enervation.jbp", "죽음 기사 재주 선택 — 생명을 빼앗는 음에너지"),
            new IconSource("UndeadMount", Guids.Game.UndeadMountFeature, "UndeadMountFeature", "Mythic/Lich/LichUniqueAbilities/UndeadMountFeature.jbp", "망자의 군마 — 언데드 탈것"),
            new IconSource("WeaponBond", Guids.Game.WeaponBondFeature, "WeaponBondFeature", "Classes/Paladin/WeaponBond/WeaponBondFeature.jbp", "망자의 무기 유대 — 무기 유대"),
            new IconSource("Slow", "f492622e473d34747806bdb39356eb89", "Slow", "Spells/Level3/Slow.jbp", "추적의 맹세 — 대상을 늦춤"),
            new IconSource("Slowed", "488e53ede2802ff4da9372c6a494fb66", "Slowed", "Buffs/Slowed.jbp", "추적 감속 버프 — 감속 상태"),
            new IconSource("Eyebite", "3167d30dd3c622c46b0c0cb242061642", "Eyebite", "Spells/Level6/Eyebite.jbp", "공포의 응시(재주·능력·자원) — 노려보는 눈"),
            new IconSource("InflictLightWounds", Guids.Game.InflictLightWounds, "InflictLightWounds", "Spells/Level1/InflictLightWounds.jbp", "죽음의 손길(재주·능력) — 상처 입히기"),
            new IconSource("Blink", "045351f1421ee3f449a9143db701d192", "Blink", "Spells/Level3/Blink.jbp", "유령 걸음 — 반쯤 유령이 되어 지나감"),
            new IconSource("AuraOfResolve", Guids.Game.AuraOfResolveFeature, "AuraOfResolveFeature", "Classes/Paladin/AuraOfResolve/AuraOfResolveFeature.jbp", "죽음의 결의(재주·오라 효과) — 결의의 오라"),
            new IconSource("Fear", "d2aeac47450c76347aebbc02e4f463e0", "Fear", "Spells/Level4/Fear.jbp", "짙은 공포(재주·오라 효과) — 공포"),
            new IconSource("Stoneskin", "c66e86905f7606c4eaa5c774f0357b2b", "Stoneskin", "Spells/Level4/Stoneskin.jbp", "무덤 갑옷 — 피해 감소"),
            new IconSource("ShieldOfFaith", "183d5bb91dea3a1489a6db6c9cb64445", "ShieldOfFaith", "Spells/Level1/ShieldOfFaith.jbp", "맹세의 방패(재주·오라 효과) — 신앙의 방패"),
            new IconSource("Soulreaver", "b4afacd337dac4a40a769a567c038ab7", "Soulreaver", "Spells/Level8/Soulreaver.jbp", "영혼 수확 — 영혼을 거둠"),
            new IconSource("VampiricTouch", "6cbb040023868574b992677885390f92", "VampiricTouch", "Spells/Level3/VampiricTouch.jbp", "생명 흡수 · 유대: 흡혈 — 흡혈의 손길"),
            new IconSource("CommandUndead", Guids.Game.CommandUndead, "CommandUndead", "Spells/Level2/CommandUndead.jbp", "언데드 지배(재주·자원·복제 능력 — 복제가 이미 복사)"),
            new IconSource("WailOfBanshee", "b24583190f36a8442b212e45226c54fc", "WailOfBanshee", "Spells/Level9/WailOfBanshee.jbp", "사자의 외침(재주·능력·자원) — 밴시의 울부짖음"),
            new IconSource("BestowCurse", "989ab5c44240907489aba0a8568d0603", "BestowCurse", "Spells/Level3/BestowCurse.jbp", "저주의 일격 · 치유 불가 버프 — 저주"),
            new IconSource("IronBody", "198fcc43490993f49899ed086fe723c1", "IronBody", "Spells/Level8/IronBody.jbp", "강철 뼈 — 강철 몸"),
            new IconSource("SlayLiving", "a6e59e74cba46a44093babf6aec250fc", "SlayLiving", "Spells/Level5/SlayLiving.jbp", "죽음의 칼날 — 산 자를 베는 음에너지"),
            new IconSource("ExtraChannel", "cd9f19775bd9d3343a31a065e93f0c47", "ExtraChannel", "Feats/ExtraChannel.jbp", "강화된 채널 — 추가 채널"),
            new IconSource("CraneRiposte", "59eb2a5507975244c893402d582bf77b", "CraneStyleRiposteFeat", "Feats/CraneStyle/CraneStyleRiposteFeat.jbp", "복수의 반격(재주·자원) — 반격"),
            new IconSource("Diehard", "86669ce8759f9d7478565db69b8c19ad", "Diehard", "Feats/Diehard.jbp", "꺾이지 않는 맹세(재주·자원) — 불굴"),
            new IconSource("LastStand", "d25326011c45a5942bc12f5d4401d4ae", "LastStand", "Mythic/MythicAbilities/LastStand/LastStand.jbp", "마지막 숨 — 최후의 저항"),
            new IconSource("UnholyChoice", "561803a819460f34ea1fe079edabecce", "ArcaneWeaponUnholyChoice", "Classes/Magus/ArcanePool/ArcaneWeaponUnholyChoice.jbp", "유대: 괴사 — 음에너지 무기 강화"),
            new IconSource("GhostTouchChoice", "688d42200cbb2334c8e27191c123d18f", "ArcaneWeaponGhostTouchChoice", "Classes/Magus/ArcanePool/ArcaneWeaponGhostTouchChoice.jbp", "유대: 유령 접촉 — 같은 무기 강화"),
            new IconSource("CauseFear", "bd81a3931aa285a4f9844585b5d97e51", "CauseFear", "Spells/Level1/CauseFear.jbp", "유대: 공포의 칼날 — 명중 시 흔들림"),
        };

        // 재주 Id → 아이콘 원본 키(Targets 보다 먼저 초기화돼야 한다 — 정적 초기화 순서). Id 집합 = TalentPlan.Rows 의 Id(어긋나면 Problems 가 잡는다).
        public static readonly IReadOnlyDictionary<string, string> TalentSource = new Dictionary<string, string>
        {
            ["PursuitOath"] = "Slow", ["TerribleGaze"] = "Eyebite", ["DeathsTouch"] = "InflictLightWounds", ["GhostStride"] = "Blink", ["DeathsResolve"] = "AuraOfResolve",
            ["DenseFear"] = "Fear", ["GraveArmor"] = "Stoneskin", ["OathShield"] = "ShieldOfFaith", ["SoulHarvest"] = "Soulreaver", ["LifeDrain"] = "VampiricTouch",
            ["CommandUndead"] = "CommandUndead", ["WailOfTheDead"] = "WailOfBanshee", ["CursedStrike"] = "BestowCurse", ["IronBones"] = "IronBody", ["DeathBlade"] = "SlayLiving",
            ["EmpoweredChannel"] = "ExtraChannel", ["VengefulRiposte"] = "CraneRiposte", ["MarkOfDeath"] = "SmiteAdditional", ["UnyieldingOath"] = "Diehard", ["LastBreath"] = "LastStand",
        };

        // 우리 블루프린트 GUID → 아이콘 원본 키.
        public static readonly IReadOnlyDictionary<string, string> Targets = BuildTargets();

        public const string ReasonHidden = "hidden";
        public const string ReasonNoIconField = "no-icon-field";   // 형식에 m_Icon 필드 자체가 없다(BlueprintAbilityAreaEffect 638 · BlueprintWeaponEnchantment 725 · BlueprintUnitProperty 295 — jbp 전부 m_Icon 키 없음, blueprints.zip 2026-11-15) — Icons.Apply 가 필드 부재를 확인
        public const string ReasonArchetype = "archetype";          // 아키타입: 본편 아키타입 213개 중 212개가 m_Icon null(blueprints.zip 2026-11-15 전수) — Icons.Apply 가 형식을 확인
        public static readonly IReadOnlyDictionary<string, string> NoIcon = BuildNoIcon();

        static Dictionary<string, string> BuildTargets()
        {
            var t = new Dictionary<string, string>();
            Action<string, string> add = (g, k) => { if (t.ContainsKey(g)) throw new InvalidOperationException("OathboundRevenant: IconTable target listed twice: " + g); t.Add(g, k); };
            // 레벨표 특성(껍데기) · 언데드 몸
            add(Guids.UndeadBodyFeature, "AnimateDead"); add(Guids.OathRegeneration, "Regeneration"); add(Guids.SmiteNegative, "SmiteEvil"); add(Guids.RevenantHands, "LayOnHandsFeature");
            add(Guids.GraveyardDread, "Shaken"); add(Guids.DeathAura, "CircleOfDeath"); add(Guids.NegativeChannel, "ChannelNegative"); add(Guids.TalentSelection, "Enervation");
            add(Guids.RevenantUndeadMount, "UndeadMount"); add(Guids.RevenantWeaponBond, "WeaponBond");
            foreach (var kv in TalentSource) add(Guids.Talent(kv.Key), kv.Value);
            // 복제·능력·버프·오라 효과·자원
            add(Guids.RevenantLayOnHands, "LayOnHandsOthers");
            add(Guids.PursuitSlowBuff, "Slowed"); add(Guids.CursedNoHealBuff, "BestowCurse");
            add(Guids.ChannelAbility, "ChannelNegative"); add(Guids.DeathsTouchAbility, "InflictLightWounds");
            add(Guids.DeathAuraEffect, "CircleOfDeath"); add(Guids.DenseFearEffect, "Fear"); add(Guids.ResolveEffect, "AuraOfResolve"); add(Guids.OathShieldEffect, "ShieldOfFaith");
            add(Guids.GazeAbility, "Eyebite"); add(Guids.GazeResource, "Eyebite"); add(Guids.WailAbility, "WailOfBanshee"); add(Guids.WailResource, "WailOfBanshee");
            add(Guids.CommandUndeadAbility, "CommandUndead"); add(Guids.CommandResource, "CommandUndead"); add(Guids.RiposteResource, "CraneRiposte"); add(Guids.UnyieldingResource, "Diehard");
            // 유대 선택지(DivineBond 가 복사된 불꽃 아이콘을 비운다)
            add(Guids.BondNecroticChoice, "UnholyChoice"); add(Guids.BondVampiricChoice, "VampiricTouch"); add(Guids.BondGhostTouchChoice, "GhostTouchChoice"); add(Guids.BondDreadBladeChoice, "CauseFear");
            return t;
        }

        static Dictionary<string, string> BuildNoIcon()
        {
            var n = new Dictionary<string, string>();
            Action<string, string> add = (g, r) => { if (n.ContainsKey(g)) throw new InvalidOperationException("OathboundRevenant: IconTable no-icon listed twice: " + g); n.Add(g, r); };
            add(Guids.Archetype, ReasonArchetype);
            add(Guids.RevenantHandsMaximize, ReasonHidden);   // Hands.cs: MaximizeFeature.HideInUI = true
            foreach (var g in new[] { Guids.SmiteMarkBuff, Guids.OathRegenSpent, Guids.LifeDrainSpent }) add(g, ReasonHidden);   // Strike.cs: NewBuff(..., hidden: true)
            foreach (var g in new[] { Guids.DeathAuraBuff, Guids.DenseFearBuff, Guids.ResolveBuff, Guids.OathShieldBuff }) add(g, ReasonHidden);   // Auras.cs: 오라 버프 NewBuff(..., true)
            foreach (var g in new[] { Guids.DeathAuraArea, Guids.DenseFearArea, Guids.ResolveArea, Guids.OathShieldArea, Guids.ChannelDiceProperty }) add(g, ReasonNoIconField);
            add(Guids.LastBreathAbility, ReasonHidden);       // SurvivalBuild.cs: a.Hidden = true
            foreach (var g in new[] { Guids.RevenantMountHp, Guids.RevenantUndeadMountInner, Guids.RevenantWeaponChoices }) add(g, ReasonHidden);   // DivineBond.cs: NewFeature(..., true)
            foreach (var g in new[] { Guids.BondNecroticBuff, Guids.BondVampiricBuff, Guids.BondGhostTouchBuff, Guids.BondDreadBladeBuff }) add(g, ReasonHidden);   // 불꽃 버프 m_Flags(HiddenInUi) 복사
            foreach (var g in new[] { Guids.RevenantNecroticEnchant, Guids.RevenantVampiricEnchant, Guids.RevenantDreadBladeEnchant }) add(g, ReasonNoIconField);
            return n;
        }

        public static IconSource SourceOf(string target)
        {
            string key;
            if (!Targets.TryGetValue(target, out key)) throw new KeyNotFoundException("OathboundRevenant: no icon source for " + target);
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("OathboundRevenant: icon source key '" + key + "' is in Sources " + hit.Length + " time(s)");
            return hit[0];
        }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 우리가 만드는 블루프린트 GUID 전체(시험: Guids 상수 · 게임: BlueprintUtil 이 실제로 만든 것 전부).
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
                Guid parsed; if (s.Guid == null || !Guid.TryParseExact(s.Guid, "N", out parsed) || s.Guid != s.Guid.ToLowerInvariant()) p.Add("source " + s.Key + " GUID '" + s.Guid + "' is not 32 lowercase hex");
                if (ownSet.Contains(s.Guid)) p.Add("source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.VanillaName)) p.Add("source " + s.Key + " has no reason / path / name");
                if (!Targets.Values.Contains(s.Key)) p.Add("source " + s.Key + " is unused");
            }
            foreach (var kv in Targets) if (!Sources.Any(s => s.Key == kv.Value)) p.Add("target " + kv.Key + " -> unknown source key " + kv.Value);
            var ids = TalentPlan.Rows.Select(r => r.Id).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (!ids.SequenceEqual(TalentSource.Keys.OrderBy(x => x, StringComparer.Ordinal))) p.Add("talent icon map [" + string.Join(",", TalentSource.Keys.ToArray()) + "] != TalentPlan ids [" + string.Join(",", ids) + "]");
            foreach (var g in Targets.Keys.Intersect(NoIcon.Keys)) p.Add(g + " is both an icon target and no-icon");
            foreach (var g in Targets.Keys.Concat(NoIcon.Keys)) if (!ownSet.Contains(g)) p.Add(g + " is in IconTable but not one of our blueprints");
            foreach (var g in ownSet) if (!Targets.ContainsKey(g) && !NoIcon.ContainsKey(g)) p.Add(g + " (ours) has neither an icon source nor a no-icon reason");
            foreach (var kv in NoIcon) if (kv.Value != ReasonHidden && kv.Value != ReasonNoIconField && kv.Value != ReasonArchetype) p.Add(kv.Key + " no-icon reason '" + kv.Value + "' unknown");
            return p;
        }
    }
}
