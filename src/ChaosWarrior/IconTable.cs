using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // [순수] 아이콘 원장(2026-11-15 결함: 분노·변이 16표식·원소 껍질 4·고통·끔찍한 형상 등 보이는 블루프린트 전부가 m_Icon 없이 만들어졌다 —
    // 예외가 없어 사람이 보기 전까지 몰랐다). 중장법사(BulwarkMage 84d70ed) IconTable 과 같은 구조.
    // 게임 형식 없음(modinfo testSources). 단일 원장(C4): 게임 쪽 Icons.Apply 가 이 표대로 본편 원본의 m_Icon 을 우리 블루프린트에 넣고,
    //   (1) 원본이 없거나 아이콘이 null 이면 던진다(BuildFailed), (2) 우리가 빌드 때 만든 블루프린트 전부가 Targets 또는 NoIcon 에 정확히 한 번 있어야 한다(아니면 던진다),
    //   (3) NoIcon 의 「숨김」 이 실제로 숨김이 아니면 던진다, (4) 보이는 사실(특성·버프·능력·토글)에 아이콘이 없으면 던진다,
    //   (5) ItemIconTargets(자연 무기 복제, 개편 O4)는 쓰지 않고 공개 Icon(아이템 m_Icon ?? 무기 형식 Icon)이 null 이면 던진다.
    // 점검 전용 블루프린트(CheckOnly — 점검 모드에서 BuildAll 이 Icons.Apply·등록을 마친 뒤에만 만든다)는 빌드 때 있으면 안 된다 — 있으면 Problems 가 잡는다(일반·점검 모드 모두).
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
            new IconSource("RageFeature", "2479395977cfeeb46b482bc3385f4647", "RageFeature", "Classes/Barbarian/Rage/RageFeature.jbp", "혼돈의 분노 특성·자원 — 바바리안 분노"),
            new IconSource("RageBuff", "da8ce41ac3cd74742b80984ccc3c9613", "StandartRageBuff", "Classes/Barbarian/Rage/StandartRageBuff.jbp", "분노 버프 — 바바리안 분노 버프"),
            new IconSource("RageAbility", "df6a2cce8e3a9bd4592fb1968b83f730", "StandartRageActivateableAbility", "Classes/Barbarian/Rage/StandartRageActivateableAbility.jbp", "분노 토글 — 바바리안 분노 토글"),
            new IconSource("GreaterRage", "ce49c579fe0bcc647a32c96929fae982", "GreaterRageFeature", "Classes/Barbarian/GreaterRageFeature.jbp", "상급 혼돈의 분노 — 상급 분노"),
            new IconSource("MightyRage", "06a7e5b60020ad947aed107d82d1f897", "MightyRage", "Classes/Barbarian/MightyRage.jbp", "최상급 혼돈의 분노 — 최상급 분노"),
            new IconSource("Mutagen", "e16b1291cd254bd8aa9d203df602fcd6", "AlchemistMutagen", "Classes/Alchemist/AlchemistMutagen.jbp", "광기의 변이 — 몸을 바꾸는 변이제"),
            new IconSource("EnlargePerson", "c60969e7f264e6d4b84a1499fdcf9039", "EnlargePerson", "Spells/Level1/EnlargePerson.jbp", "거대화 — 크기 +1·힘 +2"),
            new IconSource("Stoneskin", "c66e86905f7606c4eaa5c774f0357b2b", "Stoneskin", "Spells/Level4/Stoneskin.jbp", "굳은 살갗 — 피해 감소(DR)"),
            new IconSource("FastHealing", "318ce88da1832744c87fd15be5010dc0", "FastHealing", "Classes/Alchemist/Discoveries/GrandDiscovery/FastHealing.jbp", "재생 — 빠른 치유"),
            new IconSource("Haste", "8d20b0a6129bd814eb0146041879f38a", "Haste", "Buffs/Haste.jbp", "신속 — 속도·반사 보너스"),
            new IconSource("ThornBody", "2daf9c5112f16d54ab3cd6904c705c59", "ThornBody", "Spells/Level4/ThornBody.jbp", "가시 피부 — 근접 공격자에게 관통 피해"),
            new IconSource("CloakOfChaos", "9155dbc8268da1c49a7fc4834fa1a4b1", "CloakOfChaos", "Spells/Level8/CloakOfChaos.jbp", "혼돈의 피 — 혼돈 계열 내성 보너스 주문"),
            new IconSource("ResistEnergy", "21ffef7791ce73f468b6fca4d9371e8b", "ResistEnergy", "Spells/Level2/ResistEnergy.jbp", "원소 껍질 표식 — 원소 저항(원소 무작위)"),
            new IconSource("ResistFire", "468877871a8e3ba41813a9697ec4eb4e", "ResistFireBuff", "Spells/Level2/ResistFireBuff.jbp", "원소 껍질: 화염 — 화염 저항 버프"),
            new IconSource("ResistCold", "dfedc0bf1d93f024d85546314c42b56a", "ResistColdBuff", "Spells/Level2/ResistColdBuff.jbp", "원소 껍질: 냉기 — 냉기 저항 버프"),
            new IconSource("ResistAcid", "8d8f20391422c0e41a1650e7a9b7a21f", "ResistAcidBuff", "Spells/Level2/ResistAcidBuff.jbp", "원소 껍질: 산 — 산 저항 버프"),
            new IconSource("ResistElectricity", "17aee23103aee674082ff9891c82ae2f", "ResistElectricityBuff", "Spells/Level2/ResistElectricityBuff.jbp", "원소 껍질: 전기 — 전기 저항 버프"),
            new IconSource("PainfulStrike", "052c82521039f7b4e978207eb533ead5", "ExecutionerPainfulStrike", "Classes/Slayer/Archetypes/Executioner/ExecutionerPainfulStrike.jbp", "고통 증폭 — 고통을 더하는 일격"),
            new IconSource("FeralMutagen", "fd5f7b37ab4301c48a88cc196ee5f0ce", "FeralMutagenFeature", "Classes/Alchemist/Discoveries/FeralMutagen/FeralMutagenFeature.jbp", "추가 팔 — 변이로 생긴 자연 공격"),
            new IconSource("DemonicForm", "66fa2aab59294c44cad02c4dd60f986d", "DemonicFormI", "Mythic/Demon/DemonSpells/DemonicForm/DemonicFormI.jbp", "끔찍한 형상 — 악마 형상"),
            new IconSource("Shaken", "25ec6cb6ab1845c48a95f9c20b034220", "Shaken", "Buffs/Shaken.jbp", "끔찍한 형상의 흔들림 — 부품을 복제한 원본 Shaken 그대로"),
            new IconSource("ReachArcana", "9d6546880b05e9a43bb6cc5e1d374c6a", "ReachArcanaFeature", "Classes/Magus/Arcanas/ReachArcana/ReachArcanaFeature.jbp", "늘어난 사지 — 간격 증가"),
            new IconSource("Entangled", "3a6c5d8520c3b404883276590b086702", "DirtyTrickEntangledBuff", "Root/SystemMechanics/Conditions/DirtyTrickEntangledBuff.jbp", "비틀린 다리 — 발이 묶여 느려짐"),
            new IconSource("Dazzled", "df6d1025da07524429afbae248845ecc", "DazzledBuff", "Spells/Level0/DazzledBuff.jbp", "흐린 눈 — 눈부심(명중 감소)"),
            new IconSource("ExposeVulnerability", "8ce3c4b3c1ad24f4dbb6cb4c72e1ec53", "ExposeVulnerability", "Mythic/MythicAbilities/ExposeVulnerability.jbp", "드러난 신경 — 약점 노출(AC 감소)"),
            new IconSource("Bleed", "75039846c3d85d940aa96c249b97e562", "Bleed1d6Buff", "CreatureAbilities/Bleed/Bleed1d6Buff.jbp", "터지는 핏줄 — 매 라운드 출혈 피해"),
            new IconSource("Confusion", "886c7407dc629dc499b9f1465ff382df", "Confusion", "Buffs/Confusion.jbp", "들끓는 정신 — 혼란(정신 약화)"),
            new IconSource("InflictLightWounds", "e5cb4c4459e437e49a4cd73fde6b9063", "InflictLightWounds", "Spells/Level1/InflictLightWounds.jbp", "고통 버프 — 고통을 주는 주문"),
            new IconSource("Insanity", "2b044152b3620c841badb090e01ed9de", "Insanity", "Spells/Level7/Insanity.jbp", "번지는 광기(10레벨 광역) — 광기 주문"),
            // 개편 O5(계획 2026-11-16 원장 A-2 — blueprints.zip 직독 2026-11-16, m_Icon 비어 있지 않음): 자연 공격 변이 11종의 표식 아이콘
            new IconSource("BiteFeature", "572329fc0af467a43847d39f8c6162ea", "DragonDiscipleBiteFeatureBasic", "Classes/DragonDisciple/DragonDiscipleBiteFeatureBasic.jbp", "송곳니 — 물기 자연 공격"),
            new IconSource("ExtraClaw", "856d2e2a2edc4ce7b21ef931eb77bfee", "BeastTotemLesserRageBuffAdditionalClaw", "Classes/Barbarian/RagePowers/BeastTotemLesserRageBuffAdditionalClaw.jbp", "발톱 — 분노 중 추가 발톱"),
            new IconSource("GoreFeature", "ba3e6688b9c84bb6bae639cd1676c187", "BoarGoreFeature", "Classes/Pets/BoarGoreFeature.jbp", "뿔 — 들이받기 자연 공격"),
            new IconSource("FinesseTail", "ba5616910c9b4545bf9846b7dea024f3", "FinesseTrainingTail", "Classes/Rogue/FinesseTraining/FinesseTrainingTail.jbp", "꼬리 — 꼬리 자연 공격"),
            new IconSource("FinesseTentacle", "59ba33d61ab94872813900e403ccc078", "FinesseTrainingTentacle", "Classes/Rogue/FinesseTraining/FinesseTrainingTentacle.jbp", "촉수 — 촉수 자연 공격"),
            new IconSource("WeaponFocusSting", "b98d08a3b7bbef44eacdf74fe6b29840", "WeaponFocusSting", "Feats/WeaponFocus/WeaponFocusSting.jbp", "독침 — 침 전용 그림이 본편에 없어 무기 집중(침) 그림"),
            new IconSource("FeralWings", "78197196e096c6e4eaed5c62fa108b52", "FeralWings", "Classes/Alchemist/Discoveries/FeralMutagen/FeralWings.jbp", "날개 — 변이로 돋은 날개"),
            new IconSource("FinesseHoof", "738a5f7aab6b4c57997af440e8864e54", "FinesseTrainingHoof", "Classes/Rogue/FinesseTraining/FinesseTrainingHoof.jbp", "굽 — 발굽 자연 공격"),
            new IconSource("FinesseTalon", "f1017d46f56547a3b10dc9b9b5b8da5a", "FinesseTrainingTalon", "Classes/Rogue/FinesseTraining/FinesseTrainingTalon.jbp", "맹금 발톱 — 갈고리발톱 자연 공격"),
            new IconSource("FinesseClaw", "6b2dee734f5db0043b15ed3d0f49d32d", "FinesseTrainingClaw", "Classes/Rogue/FinesseTraining/FinesseTrainingClaw.jbp", "집게 — 집게 전용 그림이 본편에 없어 범주가 같은 발톱 그림"),
            new IconSource("Poison", "ba1ae42c58e228c4da28328ea6b4ae34", "PoisonBuff", "Spells/Level3/PoisonBuff.jbp", "신경독침 — 독(고통을 더하는 침)"),
            // 개편 O6(계획 2026-11-16 원장 A-2 — blueprints.zip 직독 2026-11-16, m_Icon 비어 있지 않음; 시험 icons: jbp 가 다시 확인): 양면 변이 5종의 표식 아이콘
            new IconSource("Bloodlust", "59aca1a1b0eb45bf898d8fb40896552e", "BloodlustBuff", "Classes/Slayer/Archetypes/Bloodseeker/BloodlustBuff.jbp", "피의 갈증 — 피에 굶주린 공격"),
            new IconSource("BullsStrength", "4c3d08935262b6544ae97599b3a9556d", "BullsStrength", "Spells/Level2/BullsStrength.jbp", "비대한 근육 — 힘 증가"),
            new IconSource("Barkskin", "5b77d7cc65b8ab74688e74a37fc2f553", "Barkskin", "Spells/Level2/Barkskin.jbp", "돌 껍질 — 자연 방어"),
            new IconSource("LightningReflexes", "15e7da6645a7f3d41bdad7c8c4b9de1e", "LightningReflexes", "Feats/LightningReflexes.jbp", "과민한 신경 — 반사 내성"),
            new IconSource("Castigate", "ce4c4e52c53473549ae033e2bb44b51a", "Castigate", "Spells/Level2/Castigate.jbp", "고통 공명 — 고통을 주는 질책"),
            // 개편 O8(계획 2026-11-16 원장 A-2 — blueprints.zip 직독 2026-11-17, m_Icon ed900c6a…): 신속 시전 권리 버프 6개
            new IconSource("QuickenSpell", "ef7ece7bb5bb66a41b256976b27f424e", "QuickenSpellFeat", "Feats/Metamagic/QuickenSpellFeat.jbp", "광란의 시전(신속 시전 권리) — 주문 신속화 특기"),
        };

        // 변이 표식(ChaosRules.Mutations 의 Id) → 아이콘 원본 키. Id 집합 = MutationSpecs.Ids(어긋나면 Problems 가 잡는다).
        public static readonly IReadOnlyDictionary<string, string> MutationSource = new Dictionary<string, string>
        {
            ["Enlarge"] = "EnlargePerson", ["ToughHide"] = "Stoneskin", ["Regeneration"] = "FastHealing", ["Swiftness"] = "Haste", ["ThornSkin"] = "ThornBody",
            ["ChaosBlood"] = "CloakOfChaos", ["ElementalShell"] = "ResistEnergy", ["PainAmplify"] = "PainfulStrike", ["ExtraArm"] = "FeralMutagen", ["HorrificForm"] = "DemonicForm",
            ["LongLimbs"] = "ReachArcana", ["TwistedLegs"] = "Entangled", ["BlurredEyes"] = "Dazzled", ["ExposedNerves"] = "ExposeVulnerability", ["BurstingVeins"] = "Bleed", ["SeethingMind"] = "Confusion",
            // 개편 O5(계획 원장 A · A-2): 자연 공격 변이 11
            ["Fangs"] = "BiteFeature", ["Claws"] = "ExtraClaw", ["Horns"] = "GoreFeature", ["Tail"] = "FinesseTail", ["Tentacle"] = "FinesseTentacle", ["Stinger"] = "WeaponFocusSting",
            ["Wings"] = "FeralWings", ["Hooves"] = "FinesseHoof", ["Talons"] = "FinesseTalon", ["Pincer"] = "FinesseClaw", ["NeuroStinger"] = "Poison",
            // 개편 O6(계획 원장 A · A-2): 양면 변이 5
            ["Bloodthirst"] = "Bloodlust", ["BloatedMuscle"] = "BullsStrength", ["StoneShell"] = "Barkskin", ["RawNerves"] = "LightningReflexes", ["PainResonance"] = "Castigate",
        };

        // 원소 껍질(ChaosRules.Dials.ShellElements 순서 = MutationSpecs.ShellBuffIds 순서) → 아이콘 원본 키. 원소 집합이 어긋나면 Problems 가 잡는다.
        public static readonly IReadOnlyDictionary<string, string> ShellSource = new Dictionary<string, string>
        {
            ["Fire"] = "ResistFire", ["Cold"] = "ResistCold", ["Acid"] = "ResistAcid", ["Electricity"] = "ResistElectricity",
        };

        // 우리 블루프린트 GUID → 아이콘 원본 키.
        public static readonly IReadOnlyDictionary<string, string> Targets = BuildTargets();

        // 개편 O4: 자연 무기 복제 12개(NaturalWeaponPlan 의 우리 GUID 열 — 행을 더하면 따라온다, C4). 아이템이라 「보이는 사실」 규칙 대상이 아니다 — 아이콘을 쓰지 않고,
        // 엔진 폴백(BlueprintItemWeapon.get_Icon @0000~0020 = 아이템 m_Icon ?? 무기 형식 Icon)이 실제로 그림을 내는지만 Icons.Apply 가 본다(null 이면 던진다). 키메라 몽크 IconTable 과 같은 규칙.
        public static readonly IReadOnlyList<string> ItemIconTargets = NaturalWeaponPlan.WeaponGuids;

        public const string ReasonHidden = "hidden";
        public const string ReasonNoIconField = "no-icon-field";   // 형식에 m_Icon 필드 자체가 없다(BlueprintAbilityAreaEffect — jbp 638개 전부 m_Icon 키 없음, blueprints.zip 2026-11-15) — Icons.Apply 가 필드 부재를 확인
        public const string ReasonArchetype = "archetype";          // 아키타입: 본편 아키타입도 아이콘이 없다(RageshaperShifterArchetype 41d5b44e… 등 — 시험 icons: jbp 가 m_Icon null 을 확인) — Icons.Apply 가 형식을 확인
        public static readonly IReadOnlyDictionary<string, string> NoIcon = BuildNoIcon();

        // 점검 모드에서만 만드는 블루프린트(Checks/CheckOnlyBlueprints.cs — Blueprints.BuildAll 맨 끝). 빌드(Icons.Apply) 때는 없어야 한다.
        // 2026-11-17: 예전에는 Main.Load 안(CheckRunner.Begin → Scenarios.RegisterAll)에서 만들어 BuildAll 보다 먼저 생겼다 → 점검 모드만 빌드 실패(시험 `checkmode:`).
        public static readonly IReadOnlyList<string> CheckOnly = new[] { Guids.CheckWillMinus50, Guids.CheckWillPlus50, Guids.CheckMindBuffImmune, Guids.CheckMindSpellImmune, Guids.CheckForeignBite };

        static Dictionary<string, string> BuildTargets()
        {
            var t = new Dictionary<string, string>();
            Action<string, string> add = (g, k) => { if (t.ContainsKey(g)) throw new InvalidOperationException("ChaosWarrior: IconTable target listed twice: " + g); t.Add(g, k); };
            add(Guids.RageResource, "RageFeature");
            add(Guids.RageFeature, "RageFeature");
            add(Guids.RageBuff, "RageBuff");
            add(Guids.RageAbility, "RageAbility");
            add(Guids.RageGreaterFeature, "GreaterRage");
            add(Guids.RageMightyFeature, "MightyRage");
            add(Guids.MutationFeature, "Mutagen");
            foreach (var kv in MutationSource) add(MutationSpecs.MarkerOf(kv.Key), kv.Value);
            var elements = ChaosRules.Dials.ShellElements;
            if (elements.Length != MutationSpecs.ShellBuffIds.Count) throw new InvalidOperationException("ChaosWarrior: IconTable: shell elements " + elements.Length + " != shell buffs " + MutationSpecs.ShellBuffIds.Count);
            for (int i = 0; i < elements.Length; i++)
            {
                string key;
                if (!ShellSource.TryGetValue(elements[i], out key)) throw new InvalidOperationException("ChaosWarrior: IconTable has no icon for shell element " + elements[i]);
                add(MutationSpecs.ShellBuffIds[i], key);
            }
            add(Guids.HorrorShakenBuff, "Shaken");
            add(Guids.PainBuff, "InflictLightWounds");
            add(Guids.PainAoeFeature, "Insanity");
            foreach (var r in QuickenSpec.Rights) add(r.BuffGuid, "QuickenSpell");   // 개편 O8: 권리 버프(보이는 버프 — Quicken.Build 의 hidden = false)
            return t;
        }

        static Dictionary<string, string> BuildNoIcon()
        {
            var n = new Dictionary<string, string>();
            Action<string, string> add = (g, r) => { if (n.ContainsKey(g)) throw new InvalidOperationException("ChaosWarrior: IconTable no-icon listed twice: " + g); n.Add(g, r); };
            add(Guids.Archetype, ReasonArchetype);
            add(Guids.HorrorArea, ReasonNoIconField);
            add(Guids.FrenziedCastingSpent, ReasonHidden);
            add(Guids.PainProbeBuff, ReasonHidden);    // Pain.cs: BuildKit.Buff(..., hidden: true) — 내부 판정용
            add(Guids.PainSaveSource, ReasonHidden);   // Pain.cs: BuildKit.Buff(..., hidden: true) — 내성 이유 출처
            add(Guids.PainOverflowBuff, ReasonHidden); // 개편 O7 Pain.cs: BuildKit.Buff(..., hidden: true) — 옛 세이브 전용 초과 버프(R3-F1 재구성 뒤 새로 붙지 않음)
            add(Guids.PainCounterBuff, ReasonHidden);  // R3-F1 재구성 Pain.cs: BuildKit.Buff(..., hidden: true) — 고통 계수(총 겹, 해제 불가)
            foreach (var t in RageStrikeSpec.Tiers) add(t.BuffGuid, ReasonHidden);   // Rage.cs: 분노 중 자연 무기 단계 버프(숨김 — 설명은 rage.desc)
            return n;
        }

        public static IconSource SourceOf(string target)
        {
            string key;
            if (!Targets.TryGetValue(target, out key)) throw new KeyNotFoundException("ChaosWarrior: no icon source for " + target);
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("ChaosWarrior: icon source key '" + key + "' is in Sources " + hit.Length + " time(s)");
            return hit[0];
        }

        // 표 무결성(C2) — 빈 목록 = 일치. own = 빌드 때 우리가 만든 블루프린트 GUID 전체(시험: Guids 상수 − CheckOnly · 게임: BlueprintUtil 이 실제로 만든 것 전부).
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
            var ids = MutationSpecs.Ids.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (!ids.SequenceEqual(MutationSource.Keys.OrderBy(x => x, StringComparer.Ordinal))) p.Add("mutation icon map [" + string.Join(",", MutationSource.Keys.ToArray()) + "] != MutationSpecs.Ids [" + string.Join(",", ids) + "]");
            if (!ChaosRules.Dials.ShellElements.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(ShellSource.Keys.OrderBy(x => x, StringComparer.Ordinal))) p.Add("shell icon map != ChaosRules.Dials.ShellElements");
            foreach (var g in CheckOnly) if (ownSet.Contains(g)) p.Add(g + " is a check-only blueprint but exists at build time");
            // 분류는 정확히 한 곳: 아이콘 대상(Targets) · 아이템 아이콘 대상(ItemIconTargets) · 아이콘 없음(NoIcon). 같은 목록 안의 중복도 센다.
            var count = new Dictionary<string, int>();
            foreach (var g in Targets.Keys.Concat(ItemIconTargets).Concat(NoIcon.Keys)) { int c; count.TryGetValue(g, out c); count[g] = c + 1; }
            foreach (var kv in count) if (kv.Value > 1) p.Add(kv.Key + " is classified " + kv.Value + " times (icon target / item icon target / no-icon)");
            foreach (var g in count.Keys) if (!ownSet.Contains(g)) p.Add(g + " is in IconTable but not one of our blueprints");
            foreach (var g in ownSet) if (!count.ContainsKey(g)) p.Add(g + " (ours) has neither an icon source nor a no-icon reason");
            foreach (var kv in NoIcon) if (kv.Value != ReasonHidden && kv.Value != ReasonNoIconField && kv.Value != ReasonArchetype) p.Add(kv.Key + " no-icon reason '" + kv.Value + "' unknown");
            return p;
        }
    }
}
