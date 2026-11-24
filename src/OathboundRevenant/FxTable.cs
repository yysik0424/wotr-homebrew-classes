using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
     
    // 단일 원장(C4): 게임 쪽 OathFx(빌드 시 본편 원본의 FX 칸을 읽어 이 표의 AssetId 와 대조) · 되읽기(Strike.VerifyPolarity · OathFx.VerifyAllUsed) · 시험 `fx:`(blueprints.zip 직독)이 모두 이 표를 읽는다.
    // 원본이 없거나 · 이름이 다르거나 · FX 칸이 비었거나 · 칸의 AssetId 가 표와 다르면 빌드가 던진다(BuildFailed) — 엔진은 빈 PrefabLink 를 조용히 건너뛰므로 여기서 시끄럽게 막는다.
    // FX 만 더한다 — 게임 동작 값(피해·내성·회복량)은 건드리지 않는다.
    public sealed class FxSource
    {
        public readonly string Key, SourceGuid, SourceName, JbpPath, Slot, AssetId, Reason;
        public FxSource(string key, string sourceGuid, string sourceName, string jbpPath, string slot, string assetId, string reason)
        { Key = key; SourceGuid = sourceGuid; SourceName = sourceName; JbpPath = jbpPath; Slot = slot; AssetId = assetId; Reason = reason; }
    }

    // 쓰는 자리 1곳: Key(우리 쪽 이름) · 원본 키 · 방식 · 이유.
    public sealed class FxUse
    {
        public readonly string Key, SourceKey, Mode, Reason;
        public FxUse(string key, string sourceKey, string mode, string reason) { Key = key; SourceKey = sourceKey; Mode = mode; Reason = reason; }
    }

    public static class FxTable
    {
        // 원본 칸 이름(게임·시험이 같은 문자열로 분기 — 모르는 칸은 던진다).
        public const string SlotAbilitySpawnFx = "AbilitySpawnFx.PrefabLink";            // BlueprintAbility 의 AbilitySpawnFx 컴포넌트(정확히 1개) PrefabLink
        public const string SlotBuffFxOnStart = "BlueprintBuff.FxOnStart";               // BlueprintBuff.FxOnStart
        public const string SlotAreaFx = "BlueprintAbilityAreaEffect.Fx";                 // BlueprintAbilityAreaEffect.Fx
        public const string SlotNestedSpawnFx = "Components.Actions.ContextActionSpawnFx"; // 컴포넌트 행동 트리 안의 ContextActionSpawnFx 전부(1개 이상, 모두 같은 AssetId)

        // 쓰는 방식.
        public const string ModeAbilitySpawnFx = "AbilitySpawnFx";   // 우리 능력에 AbilitySpawnFx 컴포넌트(본편 원본 컴포넌트의 Time·Anchor 등 그대로, 새 PrefabLink) — 원본 칸이 AbilitySpawnFx 여야 한다
        public const string ModeAreaFx = "AreaFx";                   // 우리 영역 BlueprintAbilityAreaEffect.Fx — 원본 칸이 영역 Fx 여야 한다
        public const string ModeSpawnAction = "ContextActionSpawnFx"; // 행동 목록 안 ContextActionSpawnFx(문맥 대상 위 1회 — 대상/시전자는 놓는 자리가 정한다)
        public const string ModeCSharp = "FxHelper.SpawnFxOnUnit";    // 자체 C# 부품이 FxHelper.SpawnFxOnUnit 으로 직접(PackWarden AuxUnits.cs:266 선례)

        // 값 = blueprints.zip 직독(2026-11-15, scratchpad fx-recon/fxdump.py · fxacts.py · fxwhere.py). 24개 FX id 모두 Bundles/locationlist.json m_Guids 에 있음(같은 날 확인).
        public static readonly IReadOnlyList<FxSource> Sources = new[]
        {
            new FxSource("ChannelNegative", "89df18039ef22174b81052e2e419c728", "ChannelNegativeEnergy", "Classes/Cleric/ChannelNegativeEnergy.jbp", SlotAbilitySpawnFx, "f1325dbc42578b94b8e3a87c973ebdde", "음에너지 채널(성직자) — 시전자 기준 30 ft 폭발(Time OnApplyEffect, Anchor Caster)"),
            new FxSource("NegEnergyArea", "c4696e02f7384305bcce231201f70ff4", "DLC3_NegativeEnergyModAreaEffect", "Buffs/DLC3/DLC3_MODS/DLC3_NegativeEnergyMod/DLC3_NegativeEnergyModAreaEffect.jbp", SlotAreaFx, "b56b39f94af1bb04da24ba4206cc9140", "음에너지 영역 — 대안(미배선) WightBossCommanderAuraAreaEffect 14ba91a2… Fx 3a022865…"),
            new FxSource("CauseFear", "bd81a3931aa285a4f9844585b5d97e51", "CauseFear", "Spells/Level1/CauseFear.jbp", SlotAbilitySpawnFx, "cbfe312cb8e63e240a859efaad8e467c", "공포 유발 — 대상 위(Time OnApplyEffect, Anchor SelectedTarget)"),
            new FxSource("VampireDrain", "f934e1faf8e987243a62648d0c8032ff", "VampireEnergyDrain", "CreatureAbilities/Templates/Vampire/VampireEnergyDrain.jbp", SlotNestedSpawnFx, "c4b83c8890a517b4abb235c9cc8e908f", "흡혈귀 에너지 흡수 — 본편은 명중 트리거 안 ContextActionSpawnFx 2곳(같은 FX)으로 대상 위"),
            new FxSource("SmiteEvil", "7bb9eb2042e67bf489ccd1374423cdec", "SmiteEvilAbility", "Classes/Paladin/SmiteEvil/SmiteEvilAbility.jbp", SlotAbilitySpawnFx, "6e799a804a9ce4044a70eba38890cf5a", "악 응징 — 대상 위 단일 FX(HolySmite 영역 폭발은 피함)"),
            new FxSource("LayOnHandsSelf", "8d6073201e5395d458b8251386d72df1", "LayOnHandsSelf", "Classes/Paladin/LayOnHands/LayOnHandsSelf.jbp", SlotAbilitySpawnFx, "224fb8fd952ec4d45b6d3436a77663d9", "안수(자신) — 회복 FX"),
            new FxSource("VampiricTouch", "c4bd071ce69682f46aa780cc7118add9", "VampiricTouchBuff", "Spells/Level3/VampiricTouchBuff.jbp", SlotBuffFxOnStart, "afc207ce60f237746a561a2877ec29e3", "흡혈의 손길 버프 — 본편은 지속 FxOnStart 로만 쓴다(여기서는 1회로 씀)"),
        };

        public const string Divine = "Divine:";
        public const string Aura = "Aura:";

        public static readonly IReadOnlyList<FxUse> Uses = new[]
        {
            new FxUse("NegativeChannel", "ChannelNegative", ModeAbilitySpawnFx, "4레벨 음에너지 채널(Channel.cs Ability) + 본편 Animation 복사"),
            new FxUse("TerribleGaze", "CauseFear", ModeAbilitySpawnFx, "공포의 응시(Fear.cs BuildFear) — 대상 위"),
            new FxUse("WailOfTheDead", "CauseFear", ModeAbilitySpawnFx, "사자의 외침(Fear.cs BuildFear) — 자신 대상 능력이라 SelectedTarget = 시전자"),
            new FxUse("LastBreath", "ChannelNegative", ModeAbilitySpawnFx, "마지막 숨(SurvivalBuild.cs) — 채널과 같은 모양(30 ft 적 · 채널 주사위 ×2 · 시전자 중심 폭발)이라 채널 FX"),
            new FxUse(Aura + "DeathAura", "NegEnergyArea", ModeAreaFx, "죽음의 오라 영역(Auras.cs BuildAura)"),
            new FxUse(Aura + "DenseFear", "NegEnergyArea", ModeAreaFx, "짙은 공포 영역(죽음의 오라를 대체 — 동시에 켜지지 않는다)"),
            new FxUse("LifeDrain", "VampireDrain", ModeSpawnAction, "생명 흡수(Strike.cs) — 라운드 1회 안, 대상 위"),
            new FxUse(Divine + "SmiteNegative", "SmiteEvil", ModeSpawnAction, "스마이트 음에너지 신성 갈래(음에너지 친화 대상) — 대상 위"),
            new FxUse(Divine + "DeathBlade", "SmiteEvil", ModeSpawnAction, "죽음의 칼날 신성 갈래 — 대상 위"),
            new FxUse(Divine + "DeathsTouch", "SmiteEvil", ModeSpawnAction, "죽음의 안수 신성 갈래 — 대상 위"),
            new FxUse(Divine + "LastBreath", "SmiteEvil", ModeSpawnAction, "마지막 숨 신성 갈래 — 대상 위"),
            new FxUse("OathRegeneration", "LayOnHandsSelf", ModeSpawnAction, "맹세의 재생(Strike.cs) — ContextActionOnContextCaster 안 = 시전자 위"),
            new FxUse("VampiricBond", "VampiricTouch", ModeSpawnAction, "흡혈 결속 마법 부여(DivineBond.cs) — ActionsOnInitiator = 휘두른 자 위"),
            new FxUse("SoulHarvest", "LayOnHandsSelf", ModeCSharp, "영혼 수확 회복(SurvivalParts.cs Heal) — 소유자 위"),
        };

        // FX 를 더하지 않는 자리와 이유.
        public static readonly IReadOnlyDictionary<string, string> NoFx = new Dictionary<string, string>
        {
            [Aura + "DeathsResolve"] = "아군 인접 오라(공포 면역) — 승인 문구 「죽음의 오라」 계열이 아님, 음에너지 구름을 겹치지 않는다",
            [Aura + "OathShield"] = "아군 인접 오라(피해 분담) — 같은 이유",
        };

        // 빌드가 지나가는 자리 전부(계획 원장에서 파생: 오라 = AuraPlan.Rows, 신성 갈래 = StrikePlan.AllBranches, 공포 = FearPlan.Rows).
        public static List<string> ExpectedUses()
        {
            var l = new List<string> { "NegativeChannel" };
            l.AddRange(FearPlan.Rows.Select(r => r.Key));
            l.Add("LastBreath");
            l.AddRange(AuraPlan.Rows.Select(r => Aura + r.Key));
            l.Add("LifeDrain");
            l.AddRange(StrikePlan.AllBranches.Select(b => Divine + b.Key));
            l.AddRange(new[] { "OathRegeneration", "VampiricBond", "SoulHarvest" });
            return l;
        }

        public static IReadOnlyDictionary<string, FxSource> Resolve(Func<string, bool> sourceExists)
        {
            if (sourceExists == null) throw new ArgumentNullException(nameof(sourceExists));
            var available = new Dictionary<string, FxSource>();
            foreach (var source in Sources)
            {
                if (sourceExists(source.SourceGuid)) available.Add(source.Key, source);
                else if (source.Key != "NegEnergyArea") throw new InvalidOperationException("OathboundRevenant: required FX source missing " + source.SourceGuid);
            }
            return available;
        }

        public static FxSource Get(string key)
        {
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("OathboundRevenant: FX source key '" + key + "' is in FxTable " + hit.Length + " time(s)");
            return hit[0];
        }

        // 자리의 FX(null = FX 없음 — 단 NoFx 에 이유가 있어야 한다. 둘 다 없으면 던진다: 「표에 없음」이 「FX 없음」으로 새지 않게).
        public static FxUse UseOf(string key)
        {
            var hit = Uses.Where(u => u.Key == key).ToArray();
            if (hit.Length == 1) return hit[0];
            if (hit.Length == 0 && key != null && NoFx.ContainsKey(key)) return null;
            throw new KeyNotFoundException("OathboundRevenant: FX use '" + key + "' is in FxTable.Uses " + hit.Length + " time(s) and " + (key != null && NoFx.ContainsKey(key) ? "" : "not ") + "in NoFx");
        }

        // 자리 → 원본, 방식이 기대와 다르면 던진다(능력 자리에 행동 FX 를 잘못 꽂지 않게).
        public static FxSource SourceFor(string key, string mode)
        {
            var u = UseOf(key);
            if (u == null) return null;
            if (u.Mode != mode) throw new InvalidOperationException("OathboundRevenant: FX use " + key + " is mode " + u.Mode + ", caller wants " + mode);
            return Get(u.SourceKey);
        }

        // 능력 ResourceAssetIds 에 적을 FX id(자리 여러 개 → 중복 제거, 자리 순서). FX 없는 자리는 건너뛴다(UseOf 가 NoFx 이유를 요구).
        public static string[] AssetIds(params string[] useKeys)
        {
            if (useKeys == null || useKeys.Length == 0) throw new ArgumentException("OathboundRevenant: FxTable.AssetIds needs at least one use key");
            return useKeys.Select(UseOf).Where(u => u != null).Select(u => Get(u.SourceKey).AssetId).Distinct().ToArray();
        }

        // 32자 소문자 16진(블루프린트 AssetId 모양) — Guid "N" 형식 왕복으로 판정(숫자 리터럴 없이, dial: single block ①).
        static bool Hex32(string s) { Guid g; return s != null && Guid.TryParseExact(s, "N", out g) && g.ToString("N") == s; }

        // 표 무결성(C2) — 빈 목록 = 일치. expected = 빌드가 지나가는 자리 전부, own = 우리 블루프린트 GUID.
        public static List<string> Problems(IEnumerable<string> expected, IEnumerable<string> own)
        {
            var p = new List<string>();
            var ex = (expected ?? new string[0]).ToList();
            var ownSet = new HashSet<string>(own ?? new string[0]);
            if (ex.Count == 0) p.Add("no expected FX uses");
            if (ex.Distinct().Count() != ex.Count) p.Add("duplicate expected FX use");
            if (ownSet.Count == 0) p.Add("own GUID set is empty");
            if (Sources.Count == 0) p.Add("no FX sources");
            if (Sources.Select(s => s.Key).Distinct().Count() != Sources.Count) p.Add("duplicate FX source key");
            if (Uses.Select(u => u.Key).Distinct().Count() != Uses.Count) p.Add("duplicate FX use key");
            foreach (var s in Sources)
            {
                if (!Hex32(s.SourceGuid)) p.Add("FX source " + s.Key + " GUID '" + s.SourceGuid + "' is not 32 lowercase hex");
                if (!Hex32(s.AssetId)) p.Add("FX source " + s.Key + " asset id '" + s.AssetId + "' is not 32 lowercase hex");
                if (s.Slot != SlotAbilitySpawnFx && s.Slot != SlotBuffFxOnStart && s.Slot != SlotAreaFx && s.Slot != SlotNestedSpawnFx) p.Add("FX source " + s.Key + " slot '" + s.Slot + "' unknown");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.SourceName)) p.Add("FX source " + s.Key + " has no reason / path / name");
                else if (!s.JbpPath.EndsWith("/" + s.SourceName + ".jbp", StringComparison.Ordinal)) p.Add("FX source " + s.Key + " path " + s.JbpPath + " does not end with its name " + s.SourceName);
                if (ownSet.Contains(s.SourceGuid)) p.Add("FX source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (!Uses.Any(u => u.SourceKey == s.Key)) p.Add("FX source " + s.Key + " is unused");
            }
            foreach (var u in Uses)
            {
                var hit = Sources.Where(s => s.Key == u.SourceKey).ToArray();
                if (hit.Length != 1) { p.Add("FX use " + u.Key + " -> unknown source " + u.SourceKey); continue; }
                if (u.Mode == ModeAbilitySpawnFx && hit[0].Slot != SlotAbilitySpawnFx) p.Add("FX use " + u.Key + " copies an AbilitySpawnFx but source " + u.SourceKey + " slot is " + hit[0].Slot);
                else if (u.Mode == ModeAreaFx && hit[0].Slot != SlotAreaFx) p.Add("FX use " + u.Key + " sets an area Fx but source " + u.SourceKey + " slot is " + hit[0].Slot);
                else if (u.Mode != ModeAbilitySpawnFx && u.Mode != ModeAreaFx && u.Mode != ModeSpawnAction && u.Mode != ModeCSharp) p.Add("FX use " + u.Key + " mode '" + u.Mode + "' unknown");
                if (string.IsNullOrEmpty(u.Reason)) p.Add("FX use " + u.Key + " has no reason");
            }
            foreach (var k in Uses.Select(u => u.Key).Intersect(NoFx.Keys)) p.Add("FX use " + k + " is both FX and no-FX");
            foreach (var k in Uses.Select(u => u.Key).Concat(NoFx.Keys)) if (!ex.Contains(k)) p.Add("FxTable names use " + k + " which the build does not pass");
            foreach (var k in ex) if (!Uses.Any(u => u.Key == k) && !NoFx.ContainsKey(k)) p.Add("FX use " + k + " has neither a source nor a no-FX reason");
            foreach (var kv in NoFx) if (string.IsNullOrEmpty(kv.Value)) p.Add("FX use " + kv.Key + " no-FX reason empty");
            return p;
        }
    }
}
