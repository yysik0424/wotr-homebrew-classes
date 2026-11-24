using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
    // [순수] 아이콘 원장(2026-11-15 결함: 변이 선택·변이 16·브레스 원소 4·부분 변환 형태 10·변이약 8·브레스 자원 2·독 버프가 m_Icon 없이 만들어졌고,
    // 브레스 능력 8개의 붉은 브레스 아이콘 복사에는 null 검사가 없었다 — 예외가 없어 사람이 보기 전까지 몰랐다). 중장법사(BulwarkMage 84d70ed) IconTable 과 같은 구조.
    // 게임 형식 없음(modinfo testSources). 단일 원장(C4): 게임 쪽 Icons.Apply 가 이 표대로
    //   (1) Targets: 본편 원본의 m_Icon 을 넣는다(원본 없음 · 아이콘 null · 이미 들어 있는 값(복제·복사)과 다름 → 던진다),
    //   (2) ItemIconTargets: 자연 무기 복제 전부(NaturalWeaponsPlan 행) = 공개 Icon(아이템 m_Icon ?? 유형 Icon, BlueprintItemWeapon.get_Icon IL — PhantomBlade task-3 보고)이 null 이면 던진다(쓰지 않음),
    //   (3) NoIcon: 「숨김」 이 실제로 숨김이 아니면 · 형식이 이유와 다르면 던진다, (4) 우리가 만든 블루프린트 전부가 정확히 한 곳에 있어야 하고 보이는 사실에 아이콘이 없으면 던진다.
    // 시험 `icons:` 가 같은 표를 Guids 상수(중첩 클래스 포함, Game 제외) 전부와 대조하고, 원본 jbp(blueprints.zip)의 AssetId·m_Icon 을 직독한다. 아이콘만 바꾼다 — 게임 동작 값은 건드리지 않는다.
    public sealed class IconSource
    {
        public readonly string Key, Guid, VanillaName, JbpPath, Reason;
        public IconSource(string key, string guid, string vanillaName, string jbpPath, string reason) { Key = key; Guid = guid; VanillaName = vanillaName; JbpPath = jbpPath; Reason = reason; }
    }

    public static class IconTable
    {
        // 부분 변환 형태 키(= AspectPlan.Forms 의 Key, Guids.AspectForm/AspectAbility/AspectBuff 필드 이름). 어긋나면 Problems 가 잡는다.
        public static readonly IReadOnlyList<string> AspectKeys = new[] { "Bear", "Boar", "Dinosaur", "Elephant", "Horse", "Lizard", "Spider", "Tiger", "Wolf", "Wolverine" };

        // 본편 아이콘 원본(유일한 원장). 값 = blueprints.zip(2026-11-15 직독) 의 AssetId · 경로, 모두 m_Icon 이 비어 있지 않음(시험 icons: jbp 가 다시 확인).
        public static readonly IReadOnlyList<IconSource> Sources = BuildSources();

        static IReadOnlyList<IconSource> BuildSources()
        {
            var l = new List<IconSource>
            {
                new IconSource("FeralMutagen", "fd5f7b37ab4301c48a88cc196ee5f0ce", "FeralMutagenFeature", "Classes/Alchemist/Discoveries/FeralMutagen/FeralMutagenFeature.jbp", "키메라 변이 선택 — 몸을 짐승으로 바꾸는 변이"),
                new IconSource("MagicalTail", "c78a6efed8b2a8e4aacc0f3fb6908d93", "MagicalTailArchetypeFeature1", "Classes/Sorcerer/Archetypes/NineTailedHeir/MagicalTailArchetypeFeature/MagicalTailArchetypeFeature1.jbp", "꼬리 — 꼬리"),
                new IconSource("DragonBite", "572329fc0af467a43847d39f8c6162ea", "DragonDiscipleBiteFeatureBasic", "Classes/DragonDisciple/DragonDiscipleBiteFeatureBasic.jbp", "물기 — 자연 물기 공격"),
                new IconSource("ShifterClaws", "512f845e29514539b1943b74633dba24", "ShifterClawsFeatureLevel1", "Classes/Shifter/ShifterClaws/ShifterClawsFeatureLevel1.jbp", "발톱 — 셰이프시프터 발톱"),
                new IconSource("FiendTotemGore", "76437492f801f054ba536473ad2fde79", "FiendTotemLesserFeature", "Classes/Barbarian/RagePowers/FiendTotem/FiendTotemLesserFeature.jbp", "뿔 — 뿔로 들이받기를 주는 분노 능력(정찰 2026-11-16 5-2 1순위)"),
                new IconSource("PoisonSpell", Guids.Game.PoisonSpell, "Poison", "Spells/Level3/Poison.jbp", "독 변이 — 독"),
                new IconSource("PoisonBuff", Guids.Game.PoisonBuff, "PoisonBuff", "Spells/Level3/PoisonBuff.jbp", "키메라 독 버프 — 독 버프"),
                new IconSource("DragonNaturalArmor", "aa4f9fd22a07ddb49982500deaed88f9", "DragonDiscipleNaturalArmor", "Classes/DragonDisciple/DragonDiscipleNaturalArmor.jbp", "비늘 — 용 비늘 자연 방어"),
                new IconSource("ShifterAspectSelection", "121829d239124685b430f5263031bf83", "ShifterAspectSelectionFeature", "Classes/Shifter/ShifterAspectSelectionFeature.jbp", "부분 변환(중첩 선택) — 셰이프시프터 형상 선택"),
                new IconSource("WingsMutagen", Guids.Game.BuffWingsMutagen, "BuffWingsMutagen", "Classes/Basic/Wings/BuffWingsMutagen.jbp", "날개 — 변이제 날개"),
                new IconSource("DragonBreath", "0aadb51129cb0c147b5d2464c0db10b3", "DragonDiscipleBreathFeature", "Classes/DragonDisciple/DragonDiscipleBreathFeature.jbp", "브레스(중첩 선택) — 용의 숨결"),
                new IconSource("TwoWeaponFighting", "ac8aaf29054f5b74eb18f2af950e752d", "TwoWeaponFighting", "Feats/TwoWeaponFighting.jbp", "추가 팔 — 한 손 더 휘두르는 공격"),
                new IconSource("FastMovement", "d294a5dddd0120046aae7d4eb6cbc4fc", "FastMovement", "Classes/Barbarian/FastMovement.jbp", "추가 다리 — 빠른 이동"),
                new IconSource("KeenSenses", "9c747d24f6321f744aa1bb4bd343880d", "KeenSenses", "Traits/Races/Common/KeenSenses.jbp", "추가 눈 — 예민한 감각(시야)"),
                new IconSource("BlindFight", Guids.Game.BlindFight, "BlindFight", "Feats/BlindFight.jbp", "추가 귀 — 맹목 전투(이 변이가 주는 본편 특기, Guids.Game T6 주석)"),
                new IconSource("DragonBlindSense", "bb19b581f5a47b44abfe00164f1fcade", "DragonDiscipleBlindSense", "Classes/DragonDisciple/DragonDiscipleBlindSense.jbp", "추가 감각 — 맹감"),
                new IconSource("Toughness", "d09b20029e9abfe4480b356c92095623", "Toughness", "Feats/Toughness.jbp", "추가 심장 — 강인함(생명력)"),
                new IconSource("Endurance", Guids.Game.Endurance, "Endurance", "Feats/Endurance.jbp", "추가 폐 — 지구력"),
                new IconSource("RedBreath", Guids.Game.ScaledFistRedBreath, "ScaledFistRedBreathWeaponAbility", "Classes/Monk/Archetypes/ScaledFist/DraconicAbilities/Breaths/ScaledFistRedBreathWeaponAbility.jbp", "브레스: 화염 · 브레스 자원 2 · 브레스 능력 8(Breath.MakeAbility 가 이미 복사 — 같은 값이어야 함)"),
                new IconSource("WhiteBreath", "5a2accb17ffde8b4dafb8c8f7e00b711", "ScaledFistWhiteBreathWeaponAbility", "Classes/Monk/Archetypes/ScaledFist/DraconicAbilities/Breaths/ScaledFistWhiteBreathWeaponAbility.jbp", "브레스: 냉기 — 백룡(냉기) 숨결"),
                new IconSource("BlackBreath", "45fe18c8dcd11e54e8499646b9389029", "ScaledFistBlackBreathWeaponAbility", "Classes/Monk/Archetypes/ScaledFist/DraconicAbilities/Breaths/ScaledFistBlackBreathWeaponAbility.jbp", "브레스: 산 — 흑룡(산) 숨결"),
                new IconSource("BlueBreath", "9bfed63e0b9bfa1478c521f0527fb772", "ScaledFistBlueBreathWeaponAbility", "Classes/Monk/Archetypes/ScaledFist/DraconicAbilities/Breaths/ScaledFistBlueBreathWeaponAbility.jbp", "브레스: 전기 — 청룡(전기) 숨결"),
                new IconSource("AlchemistMutagen", "e16b1291cd254bd8aa9d203df602fcd6", "AlchemistMutagen", "Classes/Alchemist/AlchemistMutagen.jbp", "키메라 변이약 특성·자원 — 연금술사 변이제"),
                new IconSource("MutagenStrength", "7af42862f58edcc4cb825862ff2a0d10", "MutagenStrength", "Classes/Alchemist/Mutagens/MutagenStrength.jbp", "변이약(힘) 능력 — 힘 변이제"),
                new IconSource("MutagenStrengthBuff", "b84abc3531ed5674284ef0ba4aafcd3b", "MutagenStrengthBuff", "Classes/Alchemist/Mutagens/MutagenStrengthBuff.jbp", "변이약(힘) 버프 — 힘 변이제 버프"),
                new IconSource("MutagenDexterity", "b11d314d60f7a38498d1ed6933fe1638", "MutagenDexterity", "Classes/Alchemist/Mutagens/MutagenDexterity.jbp", "변이약(민첩) 능력 — 민첩 변이제"),
                new IconSource("MutagenDexterityBuff", "f2be3d538b5d75c409289d35399723c4", "MutagenDexterityBuff", "Classes/Alchemist/Mutagens/MutagenDexterityBuff.jbp", "변이약(민첩) 버프 — 민첩 변이제 버프"),
                new IconSource("CognatogenWisdom", "84a9092b8430a1344a3c8b002cc68e7f", "CognatogenWisdom", "Classes/Alchemist/Discoveries/Cognatogens/CognatogenWisdom.jbp", "변이약(지혜) 능력 — 지혜 인지제(본편 지혜 변이제 없음)"),
                new IconSource("CognatogenWisdomBuff", "6871149a90e278f479aa171ee8bb563e", "CognatogenWisdomBuff", "Classes/Alchemist/Discoveries/Cognatogens/CognatogenWisdomBuff.jbp", "변이약(지혜) 버프 — 지혜 인지제 버프"),
            };
            // 부분 변환: 형태 선택지 = 본편 형태 특성 · 능력 복제 = 본편 능력 · 버프 복제 = 본편 버프(복제가 이미 그 아이콘을 가진다 — 같은 값이어야 함).
            foreach (var k in AspectKeys)
            {
                string feat = k == "Wolverine" ? "Classes/Shifter/ShifterMajorForms/Wolverine/ShifterWolverineProgression.jbp" : "Classes/Shifter/ShifterAspects/ShifterAspect" + k + ".jbp";
                l.Add(new IconSource("Aspect" + k, GameGuid("Aspect" + k), k == "Wolverine" ? "ShifterWolverineProgression" : "ShifterAspect" + k, feat, "부분 변환: " + k + " — 본편 셰이프시프터 형상 그 자체"));
                l.Add(new IconSource("Aspect" + k + "Ability", GameGuid("Aspect" + k + "Ability"), "ShifterAspect" + k + "Ability", "Classes/Shifter/ShifterAspects/ShifterAspect" + k + "Ability.jbp", k + " 토글 복제 — 복제 원본 그대로"));
                l.Add(new IconSource("Aspect" + k + "Buff", GameGuid("Aspect" + k + "Buff"), "ShifterAspect" + k + "Buff", "Classes/Shifter/ShifterAspects/ShifterAspect" + k + "Buff.jbp", k + " 버프 복제 — 복제 원본 그대로"));
            }
            return l;
        }

        static string GameGuid(string name)
        {
            var f = typeof(Guids.Game).GetField(name);
            if (f == null || !f.IsLiteral) throw new InvalidOperationException("ChimeraMonk: IconTable: Guids.Game." + name + " not found");
            return (string)f.GetRawConstantValue();
        }

        static string OurGuid(Type t, string name)
        {
            var f = t.GetField(name);
            if (f == null || !f.IsLiteral) throw new InvalidOperationException("ChimeraMonk: IconTable: " + t.Name + "." + name + " not found");
            return (string)f.GetRawConstantValue();
        }

        // 우리 블루프린트 GUID → 아이콘 원본 키.
        public static readonly IReadOnlyDictionary<string, string> Targets = BuildTargets();

        // 자연 무기 복제 전부(폐지 무기 포함) — 엔진 폴백 아이콘만 확인(쓰지 않음). 목록 = NaturalWeaponsPlan 의 우리 GUID 열(C4 — 무기 행을 더하면 따라온다).
        public static readonly IReadOnlyList<string> ItemIconTargets = NaturalWeaponsPlan.AllWeaponGuids;

        public const string ReasonHidden = "hidden";
        public const string ReasonArchetype = "archetype";   // 아키타입: 본편 아키타입 213개 중 212개가 m_Icon null(blueprints.zip 2026-11-15 전수) — Icons.Apply 가 형식을 확인
        public static readonly IReadOnlyDictionary<string, string> NoIcon = BuildNoIcon();

        static Dictionary<string, string> BuildTargets()
        {
            var t = new Dictionary<string, string>();
            Action<string, string> add = (g, k) => { if (t.ContainsKey(g)) throw new InvalidOperationException("ChimeraMonk: IconTable target listed twice: " + g); t.Add(g, k); };
            add(Guids.MutationSelection, "FeralMutagen");
            add(Guids.Mutation.Tail, "MagicalTail");
            add(Guids.Mutation.Bite, "DragonBite"); add(Guids.Mutation.Claw, "ShifterClaws"); add(Guids.Mutation.Horn, "FiendTotemGore"); add(Guids.Mutation.Venom, "PoisonSpell");
            add(Guids.Mutation.Scales, "DragonNaturalArmor"); add(Guids.Mutation.Wings, "WingsMutagen");
            add(Guids.Mutation.Breath, "DragonBreath"); add(Guids.Mutation.ExtraArm, "TwoWeaponFighting"); add(Guids.Mutation.ExtraLegs, "FastMovement");
            add(Guids.Mutation.ExtraEyes, "KeenSenses"); add(Guids.Mutation.ExtraEars, "BlindFight"); add(Guids.Mutation.ExtraSenses, "DragonBlindSense");
            add(Guids.Mutation.ExtraHeart, "Toughness"); add(Guids.Mutation.ExtraLung, "Endurance");
            add(Guids.BreathElement.Fire, "RedBreath"); add(Guids.BreathElement.Cold, "WhiteBreath"); add(Guids.BreathElement.Acid, "BlackBreath"); add(Guids.BreathElement.Electricity, "BlueBreath");
            add(Guids.MutagenResource, "AlchemistMutagen"); add(Guids.MutagenFeature, "AlchemistMutagen");
            add(Guids.MutagenAbilityStrength, "MutagenStrength"); add(Guids.MutagenBuffStrength, "MutagenStrengthBuff");
            add(Guids.MutagenAbilityDexterity, "MutagenDexterity"); add(Guids.MutagenBuffDexterity, "MutagenDexterityBuff");
            add(Guids.MutagenAbilityWisdom, "CognatogenWisdom"); add(Guids.MutagenBuffWisdom, "CognatogenWisdomBuff");
            add(Guids.BreathResource, "RedBreath"); add(Guids.BreathSurgeResource, "RedBreath");
            foreach (var e in new[] { "Fire", "Cold", "Acid", "Electricity" })
            {
                add(OurGuid(typeof(Guids.BreathAbility), e), "RedBreath");
                add(OurGuid(typeof(Guids.BreathSurgeAbility), e), "RedBreath");
            }
            add(Guids.VenomBuff, "PoisonBuff");
            // 부분 변환 단계(I·II·III — ChimeraRules.AspectStages): 단계의 중첩 선택 = 셰이프시프터 형상 선택 아이콘, 단계마다의 형태 선택지 = 그 형태의 본편 아이콘.
            foreach (var stage in ChimeraRules.AspectStages)
            {
                add(MutationsPlan.GuidOf(stage.Id), "ShifterAspectSelection");
                foreach (var o in MutationsPlan.AspectStageForms(stage.Number)) add(o.Guid, "Aspect" + o.Key);
            }
            foreach (var k in AspectKeys)
            {
                add(OurGuid(typeof(Guids.AspectAbility), k), "Aspect" + k + "Ability");
                add(OurGuid(typeof(Guids.AspectBuff), k), "Aspect" + k + "Buff");
            }
            return t;
        }

        static Dictionary<string, string> BuildNoIcon()
        {
            var n = new Dictionary<string, string>();
            Action<string, string> add = (g, r) => { if (n.ContainsKey(g)) throw new InvalidOperationException("ChimeraMonk: IconTable no-icon listed twice: " + g); n.Add(g, r); };
            add(Guids.Archetype, ReasonArchetype);
            add(Guids.BodyFeature, ReasonHidden);               // NaturalWeapons.cs: Fab.Feature(..., hidden: true)
            foreach (var r in MutationsPlan.RetiredRows()) add(r.Guid, ReasonHidden);   // Mutations.BuildRetired: Fab.Feature(..., hidden: true) — 폐지된 변이(2026-11-17 추가 꼬리)
            foreach (var f in typeof(Guids.Boost).GetFields()) add((string)f.GetRawConstantValue(), ReasonHidden);        // BodyMutations.cs: Fab.Buff(..., hidden: true)
            foreach (var f in typeof(Guids.AspectBoost).GetFields()) add((string)f.GetRawConstantValue(), ReasonHidden);  // Aspects.cs: Fab.Buff(..., hidden: true)
            add(Guids.VenomSpent, ReasonHidden);                // Venom.cs: Fab.Buff(..., hidden: true)
            add(Guids.AspectWolfScentUpgrade, ReasonHidden);    // 복제 원본 ShifterAspectWolfScentUpgrade 33684fea HideInUI true(blueprints.zip 2026-11-15)
            return n;
        }

        public static IconSource SourceOf(string target)
        {
            string key;
            if (!Targets.TryGetValue(target, out key)) throw new KeyNotFoundException("ChimeraMonk: no icon source for " + target);
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("ChimeraMonk: icon source key '" + key + "' is in Sources " + hit.Length + " time(s)");
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
                if (s.Guid == null || s.Guid.Length != 32 || s.Guid.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))) p.Add("source " + s.Key + " GUID '" + s.Guid + "' is not 32 lowercase hex");
                if (ownSet.Contains(s.Guid)) p.Add("source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.VanillaName)) p.Add("source " + s.Key + " has no reason / path / name");
                if (!Targets.Values.Contains(s.Key)) p.Add("source " + s.Key + " is unused");
            }
            foreach (var kv in Targets) if (!Sources.Any(s => s.Key == kv.Value)) p.Add("target " + kv.Key + " -> unknown source key " + kv.Value);
            if (!AspectPlan.Forms.Select(f => f.Key).SequenceEqual(AspectKeys)) p.Add("aspect keys [" + string.Join(",", AspectKeys.ToArray()) + "] != AspectPlan.Forms");
            var count = new Dictionary<string, int>();
            foreach (var g in Targets.Keys.Concat(ItemIconTargets).Concat(NoIcon.Keys)) { int c; count.TryGetValue(g, out c); count[g] = c + 1; }
            foreach (var kv in count) if (kv.Value > 1) p.Add(kv.Key + " is classified " + kv.Value + " times");
            foreach (var g in count.Keys) if (!ownSet.Contains(g)) p.Add(g + " is in IconTable but not one of our blueprints");
            foreach (var g in ownSet) if (!count.ContainsKey(g)) p.Add(g + " (ours) has neither an icon source nor a no-icon reason");
            foreach (var kv in NoIcon) if (kv.Value != ReasonHidden && kv.Value != ReasonArchetype) p.Add(kv.Key + " no-icon reason '" + kv.Value + "' unknown");
            return p;
        }
    }
}
