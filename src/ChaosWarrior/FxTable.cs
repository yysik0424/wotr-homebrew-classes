using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
     
    // 단일 원장(C4): 게임 쪽 ChaosFx(빌드 시 본편 원본 블루프린트의 FX 칸을 읽어 이 표의 AssetId 와 대조) · 되읽기(Rage.VerifyNow · Mutations.VerifyEffects) · 시험 `fx:`(blueprints.zip 직독)이 모두 이 표를 읽는다.
    // 원본이 없거나 · 이름이 다르거나 · FX 칸이 비었거나 · 칸의 AssetId 가 표와 다르면 빌드가 던진다(BuildFailed) — 엔진은 빈 PrefabLink 를 조용히 건너뛰므로 여기서 시끄럽게 막는다.
    // 칸 의미는 본편 그대로: 원본이 FxOnStart 면 우리 버프의 FxOnStart(버프가 켜져 있는 동안), 원본이 AbilitySpawnFx · 버프 Activated 의 ContextActionSpawnFx 면 우리 버프 Activated 의 ContextActionSpawnFx(켜질 때 1회).
    // FX 만 더한다 — 게임 동작 값(능력치·저항·피해)은 건드리지 않는다.
    public sealed class FxSource
    {
        public readonly string Key, SourceGuid, SourceName, JbpPath, Slot, AssetId, Reason;
        public FxSource(string key, string sourceGuid, string sourceName, string jbpPath, string slot, string assetId, string reason)
        { Key = key; SourceGuid = sourceGuid; SourceName = sourceName; JbpPath = jbpPath; Slot = slot; AssetId = assetId; Reason = reason; }
    }

    public static class FxTable
    {
        // 원본 칸 이름(게임·시험이 같은 문자열로 분기 — 모르는 칸은 던진다).
        public const string SlotAbilitySpawnFx = "AbilitySpawnFx.PrefabLink";                 // BlueprintAbility 의 AbilitySpawnFx 컴포넌트(정확히 1개) PrefabLink
        public const string SlotBuffFxOnStart = "BlueprintBuff.FxOnStart";                    // BlueprintBuff.FxOnStart
        public const string SlotBuffActivatedSpawnFx = "AddFactContextActions.Activated.SpawnFx"; // BlueprintBuff 의 AddFactContextActions.Activated 최상위 ContextActionSpawnFx(전체에서 정확히 1개)

        // 지속(FxOnStart) = 원본 칸이 FxOnStart 일 때만. 나머지 칸 = 1회(ContextActionSpawnFx).
        public static bool IsPersistent(FxSource s)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (s.Slot == SlotBuffFxOnStart) return true;
            if (s.Slot == SlotAbilitySpawnFx || s.Slot == SlotBuffActivatedSpawnFx) return false;
            throw new InvalidOperationException("ChaosWarrior: FX source " + s.Key + " slot '" + s.Slot + "' unknown");
        }

        // 값 = blueprints.zip 직독(2026-11-15, scratchpad fx-recon/fxdump.py · fxacts.py): 원본 AssetId · 경로 · 칸의 PrefabLink.AssetId.
        public static readonly IReadOnlyList<FxSource> Sources = new[]
        {
            new FxSource("RageAura", "da8ce41ac3cd74742b80984ccc3c9613", "StandartRageBuff", "Classes/Barbarian/Rage/StandartRageBuff.jbp", SlotBuffFxOnStart, "53c86872d2be80b48afc218af1b204d7", "분노 — 본편 바바리안 분노 버프의 지속 FX"),
            new FxSource("RageBurst", "da8ce41ac3cd74742b80984ccc3c9613", "StandartRageBuff", "Classes/Barbarian/Rage/StandartRageBuff.jbp", SlotBuffActivatedSpawnFx, "a536a9fb639035140b8771b2e3a21ffd", "분노 — 본편 분노 버프가 켜질 때 Activated[12] 에서 1회 뿌리는 FX"),
            new FxSource("Shaken", "25ec6cb6ab1845c48a95f9c20b034220", "Shaken", "Buffs/Shaken.jbp", SlotBuffFxOnStart, "f00bbb092bd65a4468e72869b99f1d66", "끔찍한 형상 공포 — 본편 Shaken 복제본이 잃었던 FxOnStart 복원"),
            new FxSource("FlameShield", "23c0f0417981608479131d25d4349f7d", "FlameShieldBuff", "Classes/Kineticist/WildTalents/FlameShieldBuff.jbp", SlotBuffFxOnStart, "c8782bace2956d641892f8a6a523bdfa", "원소 껍질 화염 — 화염 방패"),
            new FxSource("IceBody", "a6da7d6a5c9377047a7bd2680912860f", "IceBodyBuff", "Spells/Level7/IceBodyBuff.jbp", SlotBuffFxOnStart, "6997425ac95b2a347a261dbd4139c9a9", "원소 껍질 냉기 — 얼음 몸"),
            new FxSource("EnergyShieldAcid", "24d8752be1d6f734480e7f75e1c6796a", "ArcanistExploitEnergyShieldAcidBuff", "Classes/Arcanist/ArcanistExploits/ArcanistExploitEnergyShieldAcidBuff.jbp", SlotBuffFxOnStart, "e23fec8d2024a8c48a8b4a57693e31a7", "원소 껍질 산 — 비전술사 에너지 방패(산)"),
            new FxSource("EnergyShieldElectricity", "3159ec8a0a53f3c4d9c239c2db96f912", "ArcanistExploitEnergyShieldElectricityBuff", "Classes/Arcanist/ArcanistExploits/ArcanistExploitEnergyShieldElectricityBuff.jbp", SlotBuffFxOnStart, "e23fec8d2024a8c48a8b4a57693e31a7", "원소 껍질 전기 — 비전술사 에너지 방패(전기)"),
            new FxSource("Stoneskin", "37a956d0e7a84ab0bb66baf784767047", "StoneskinBuff", "Spells/Level4/StoneskinBuff.jbp", SlotBuffFxOnStart, "eec87237ed7b61149a952f56da85bbb1", "굳은 살갗 — 돌피부"),
            new FxSource("ThornBody", "3749c8ac1a99a5f488f5782f74807eec", "ThornBodyBuff", "Spells/Level4/ThornBodyBuff.jbp", SlotBuffFxOnStart, "f9c7aa4de523fb742b0c27c3ffc5a202", "가시 피부 — 가시 몸"),
            new FxSource("Bleed", "dc9ed761b7721c64e98fab507e2a7755", "BleedEnchantment1d6Buff", "CreatureAbilities/Bleed/BleedEnchantment1d6Buff.jbp", SlotBuffFxOnStart, "39404d9c32bf5e9408b2d40a6405cec4", "터지는 혈관 — 출혈"),
            new FxSource("ExpeditiousRetreat", "9ea4ec3dc30cd7940a372a4d699032e7", "ExpeditiousRetreatBuff", "Spells/Level1/ExpeditiousRetreatBuff.jbp", SlotBuffFxOnStart, "534da216ed0a6ff4781aa4628ec2513e", "신속 — 신속한 후퇴"),
            new FxSource("ChaosDomain", "96bbd279e0bed0f4fb208a1761f566b5", "ChaosDomainBaseBuff", "Classes/Cleric/Domains/Chaos/ChaosDomainBaseBuff.jbp", SlotBuffFxOnStart, "9425f10c02c4c614f8f2013d4a718756", "혼돈의 피 — 혼돈 영역"),
            new FxSource("PhantasmalReach", "985da6a662924b22aaf02cf059315aa1", "PhantasmalMageReachBuff", "Classes/Arcanist/Archetypes/PhantasmalMage/PhantasmalMageReachBuff.jbp", SlotBuffFxOnStart, "34306172e3b0f33478ac715ca9ac7b4a", "긴 팔다리 — 환영 마법사 사거리"),
            new FxSource("Dazzled", "df6d1025da07524429afbae248845ecc", "DazzledBuff", "Spells/Level0/DazzledBuff.jbp", SlotBuffActivatedSpawnFx, "c388856d0e8855f429a83ccba67944ba", "흐린 눈 — 현혹(본편도 Activated 1회)"),
            new FxSource("Sickened", "4e42460798665fd4cb9173ffa7ada323", "Sickened", "Buffs/Sickened.jbp", SlotBuffFxOnStart, "d3c03d0642effaf4c8f4deb356926870", "노출된 신경 — 구역질"),
            new FxSource("CrushingDespair", "36897b36a4bd63146b1d8509a17fc5ad", "CrushingDespairBuff", "Spells/Level3/CrushingDespairBuff.jbp", SlotBuffActivatedSpawnFx, "9a38d742801be084d89bd34318c600e8", "끓는 정신 — 짓누르는 절망(본편도 Activated 1회)"),
            new FxSource("EnlargeCast", "c60969e7f264e6d4b84a1499fdcf9039", "EnlargePerson", "Spells/Level1/EnlargePerson.jbp", SlotAbilitySpawnFx, "352469f228a3b1f4cb269c7ab0409b8e", "거대화 — 거대화 주문 적용 FX(본편은 시전 시 1회 AbilitySpawnFx)"),
            new FxSource("FrightfulAspectCast", "e788b02f8d21014488067bdd3ba7b325", "FrightfulAspect", "Spells/Level8/FrightfulAspect.jbp", SlotAbilitySpawnFx, "352469f228a3b1f4cb269c7ab0409b8e", "끔찍한 형상 — 무시무시한 모습 시전 FX(본편은 시전자에게 1회 AbilitySpawnFx)"),
            new FxSource("MarilithForm", "f048ee68bc72da447970025667a77b12", "DemonicFormIVMarilithBuff", "Mythic/Demon/DemonSpells/DemonicForm/DemonicFormIVMarilithBuff.jbp", SlotBuffFxOnStart, "352469f228a3b1f4cb269c7ab0409b8e", "추가 팔 — 악마 형상(마릴리스, 팔 여럿) 버프의 FxOnStart"),
        };

        // ---------------- 대상(우리 버프) ----------------
        public const string TargetRage = "Rage";
        public const string TargetHorrorShaken = "HorrorShaken";
        public const string MutationPrefix = "Mut:";
        public static string MutationTarget(string id) { return MutationPrefix + id; }
        public static string ShellTarget(string element) { return MutationSpecs.ShellPrefix + element; }
        // 효과 행 Id(변이 Id 또는 "Shell:<원소>") → 대상. 모르는 Id 는 ForTarget 이 던진다.
        public static string TargetOfRowId(string rowId)
        {
            if (string.IsNullOrEmpty(rowId)) throw new ArgumentException("ChaosWarrior: empty effect row id", "rowId");
            return rowId.StartsWith(MutationSpecs.ShellPrefix, StringComparison.Ordinal) ? rowId : MutationTarget(rowId);
        }

        // 대상 → 원본 키(순서 = ResourceAssetIds 순서). 지속 원본은 대상마다 최대 1개.
        public static readonly IReadOnlyDictionary<string, string[]> TargetFx = new Dictionary<string, string[]>
        {
            [TargetRage] = new[] { "RageAura", "RageBurst" },
            [TargetHorrorShaken] = new[] { "Shaken" },
            ["Shell:Fire"] = new[] { "FlameShield" },
            ["Shell:Cold"] = new[] { "IceBody" },
            ["Shell:Acid"] = new[] { "EnergyShieldAcid" },
            ["Shell:Electricity"] = new[] { "EnergyShieldElectricity" },
            ["Mut:Enlarge"] = new[] { "EnlargeCast" },
            ["Mut:ToughHide"] = new[] { "Stoneskin" },
            ["Mut:Swiftness"] = new[] { "ExpeditiousRetreat" },
            ["Mut:ThornSkin"] = new[] { "ThornBody" },
            ["Mut:ChaosBlood"] = new[] { "ChaosDomain" },
            ["Mut:ExtraArm"] = new[] { "MarilithForm" },
            ["Mut:HorrificForm"] = new[] { "FrightfulAspectCast" },
            ["Mut:LongLimbs"] = new[] { "PhantasmalReach" },
            ["Mut:BlurredEyes"] = new[] { "Dazzled" },
            ["Mut:ExposedNerves"] = new[] { "Sickened" },
            ["Mut:BurstingVeins"] = new[] { "Bleed" },
            ["Mut:SeethingMind"] = new[] { "CrushingDespair" },
        };

        // 개편 2026-11-16 의 새 변이에 FX 를 더하지 않는 이유(한 문구 — 행마다 같은 값).
        public const string NoFxOverhaul = "개편 2026-11-16: FX 승인 목록 밖 — 외형은 비목표(개편 설계 §2)";

         
        public static readonly IReadOnlyDictionary<string, string> TargetNoFx = new Dictionary<string, string>
        {
            ["Mut:Regeneration"] = "승인 목록: FX 없음",
            ["Mut:PainAmplify"] = "승인 목록: FX 없음",
            ["Mut:TwistedLegs"] = "승인 목록: FX 없음",
            ["Mut:ElementalShell"] = "표식만 — 원소 FX 는 함께 붙는 껍질 버프(Shell:*)가 갖는다",
            // 개편 O5(계획 2026-11-16 원장 A 끝 줄): 새 자연 공격 변이 11종 — 「표에 없음」이 「FX 없음」으로 새지 않게 행으로 적는다.
            ["Mut:Fangs"] = NoFxOverhaul,
            ["Mut:Claws"] = NoFxOverhaul,
            ["Mut:Horns"] = NoFxOverhaul,
            ["Mut:Tail"] = NoFxOverhaul,
            ["Mut:Tentacle"] = NoFxOverhaul,
            ["Mut:Stinger"] = NoFxOverhaul,
            ["Mut:Wings"] = NoFxOverhaul,
            ["Mut:Hooves"] = NoFxOverhaul,
            ["Mut:Talons"] = NoFxOverhaul,
            ["Mut:Pincer"] = NoFxOverhaul,
            ["Mut:NeuroStinger"] = NoFxOverhaul,
            // 개편 O6(같은 원장 줄 — 「새 변이 16종의 FX: 없음」): 양면 변이 5종.
            ["Mut:Bloodthirst"] = NoFxOverhaul,
            ["Mut:BloatedMuscle"] = NoFxOverhaul,
            ["Mut:StoneShell"] = NoFxOverhaul,
            ["Mut:RawNerves"] = NoFxOverhaul,
            ["Mut:PainResonance"] = NoFxOverhaul,
        };

        // 빌드가 만드는 대상 전부(분노 · 공포 복제 · 변이 표식 16 · 껍질 4) — 표가 이 집합을 정확히 한 번씩 분류해야 한다.
        public static List<string> ExpectedTargets()
        {
            var l = new List<string> { TargetRage, TargetHorrorShaken };
            l.AddRange(MutationSpecs.Ids.Select(MutationTarget));
            l.AddRange(ChaosRules.Dials.ShellElements.Select(ShellTarget));
            return l;
        }

        public static FxSource Get(string key)
        {
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("ChaosWarrior: FX source key '" + key + "' is in FxTable " + hit.Length + " time(s)");
            return hit[0];
        }

        // 대상의 FX 원본(빈 배열 = FX 없음 — 단 NoFx 에 이유가 있어야 한다. 둘 다 없으면 던진다: 「표에 없음」이 「FX 없음」으로 새지 않게).
        public static FxSource[] ForTarget(string target)
        {
            string[] keys;
            if (target != null && TargetFx.TryGetValue(target, out keys)) return keys.Select(Get).ToArray();
            if (target != null && TargetNoFx.ContainsKey(target)) return new FxSource[0];
            throw new KeyNotFoundException("ChaosWarrior: FX target '" + target + "' is neither in FxTable.TargetFx nor TargetNoFx");
        }

        // 지속 원본(없으면 null). 2개 이상이면 던진다(FxOnStart 칸은 하나).
        public static FxSource PersistentOf(string target)
        {
            var p = ForTarget(target).Where(IsPersistent).ToArray();
            if (p.Length > 1) throw new InvalidOperationException("ChaosWarrior: FX target " + target + " has " + p.Length + " persistent sources (FxOnStart holds one)");
            return p.Length == 0 ? null : p[0];
        }

        public static FxSource[] OneShotsOf(string target) { return ForTarget(target).Where(s => !IsPersistent(s)).ToArray(); }

        // 미리 읽기 목록(본편 관례: StandartRageBuff · Shaken 등이 FX id 를 ResourceAssetIds 에 적는다). 중복 제거 · 표 순서.
        public static string[] ResourceIds(string target) { return ForTarget(target).Select(s => s.AssetId).Distinct().ToArray(); }

        static bool Hex32(string s) { return s != null && s.Length == 32 && s.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'); }

        // 표 무결성(C2) — 빈 목록 = 일치. targets = 빌드가 만드는 대상 전부, own = 우리 블루프린트 GUID(원본이 우리 것이면 안 된다).
        public static List<string> Problems(IEnumerable<string> targets, IEnumerable<string> own)
        {
            var p = new List<string>();
            var ts = (targets ?? new string[0]).ToList();
            var ownSet = new HashSet<string>(own ?? new string[0]);
            if (ts.Count == 0) p.Add("no FX targets");
            if (ts.Distinct().Count() != ts.Count) p.Add("duplicate FX target in the build's target list");
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            if (Sources.Count == 0) p.Add("no FX sources");
            if (Sources.Select(s => s.Key).Distinct().Count() != Sources.Count) p.Add("duplicate FX source key");
            if (Sources.Select(s => s.SourceGuid + "|" + s.Slot).Distinct().Count() != Sources.Count) p.Add("duplicate FX source (guid, slot)");
            foreach (var s in Sources)
            {
                if (!Hex32(s.SourceGuid)) p.Add("FX source " + s.Key + " GUID '" + s.SourceGuid + "' is not 32 lowercase hex");
                if (!Hex32(s.AssetId)) p.Add("FX source " + s.Key + " asset id '" + s.AssetId + "' is not 32 lowercase hex");
                if (s.Slot != SlotAbilitySpawnFx && s.Slot != SlotBuffFxOnStart && s.Slot != SlotBuffActivatedSpawnFx) p.Add("FX source " + s.Key + " slot '" + s.Slot + "' unknown");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.SourceName)) p.Add("FX source " + s.Key + " has no reason / path / name");
                else if (!s.JbpPath.EndsWith("/" + s.SourceName + ".jbp", StringComparison.Ordinal)) p.Add("FX source " + s.Key + " path " + s.JbpPath + " does not end with its name " + s.SourceName);
                if (ownSet.Contains(s.SourceGuid)) p.Add("FX source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (!TargetFx.Values.Any(v => v.Contains(s.Key))) p.Add("FX source " + s.Key + " is unused");
            }
            foreach (var kv in TargetFx)
            {
                if (kv.Value == null || kv.Value.Length == 0) { p.Add("target " + kv.Key + " has an empty FX list (use TargetNoFx with a reason)"); continue; }
                if (kv.Value.Distinct().Count() != kv.Value.Length) p.Add("target " + kv.Key + " lists an FX source twice");
                foreach (var k in kv.Value) if (!Sources.Any(s => s.Key == k)) p.Add("target " + kv.Key + " -> unknown FX key " + k);
                if (kv.Value.Count(k => Sources.Any(s => s.Key == k && s.Slot == SlotBuffFxOnStart)) > 1) p.Add("target " + kv.Key + " has more than one persistent (FxOnStart) source");
            }
            foreach (var t in TargetFx.Keys.Intersect(TargetNoFx.Keys)) p.Add("target " + t + " is both FX and no-FX");
            foreach (var t in TargetFx.Keys.Concat(TargetNoFx.Keys)) if (!ts.Contains(t)) p.Add("FxTable names target " + t + " which the build does not make");
            foreach (var t in ts) if (!TargetFx.ContainsKey(t) && !TargetNoFx.ContainsKey(t)) p.Add("target " + t + " has neither an FX source nor a no-FX reason");
            foreach (var kv in TargetNoFx) if (string.IsNullOrEmpty(kv.Value)) p.Add("target " + kv.Key + " no-FX reason empty");
            return p;
        }
    }
}
