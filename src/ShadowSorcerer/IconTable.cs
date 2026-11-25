using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowSorcerer
{
    // [순수] 아이콘 원장(2026-11-15 결함: 메아리 특성·분신 표시 3·보너스 주문 9·하루 −1 표시 특성이 m_Icon 없이 만들어졌다 — 예외가 없어 사람이 보기 전까지 몰랐다).
    // 중장법사(BulwarkMage 84d70ed) IconTable 과 같은 구조. 게임 형식 없음(modinfo testSources). 단일 원장(C4): 게임 쪽 Icons.Apply 가 이 표대로 본편 원본의 m_Icon 을 넣고,
    //   (1) 원본이 없거나 아이콘이 null 이면 던진다(BuildFailed), (2) 우리가 만든 블루프린트 전부가 Targets 또는 NoIcon 에 정확히 한 번 있어야 한다,
    //   (3) NoIcon 의 이유가 거짓이면 던진다, (4) 보이는 사실에 아이콘이 없으면 던진다.
    // 보너스 주문 특성 i 의 원본 = 그 특성이 가르치는 주문 ShadowRules.BonusSpells[i] — Problems 가 같은 값인지 대조한다(복제 금지, C4).
    // 시험 `icons:` 가 같은 표를 Guids 상수 전부와 대조하고, 원본 jbp(blueprints.zip)의 AssetId·m_Icon 을 직독한다. 아이콘만 바꾼다 — 게임 동작 값은 건드리지 않는다.
    public sealed class IconSource
    {
        public readonly string Key, Guid, VanillaName, JbpPath, Reason;
        public IconSource(string key, string guid, string vanillaName, string jbpPath, string reason) { Key = key; Guid = guid; VanillaName = vanillaName; JbpPath = jbpPath; Reason = reason; }
    }

    public static class IconTable
    {
        // 보너스 주문 원본(주문 레벨 1~9 순서 = ShadowRules.BonusSpells 순서). 이름·경로 = blueprints.zip 2026-11-15 직독.
        static readonly (string name, string path)[] BonusSpellJbp =
        {
            ("Vanish", "Spells/Level1/Vanish.jbp"), ("MirrorImage", "Spells/Level2/MirrorImage.jbp"), ("Displacement", "Spells/Level3/Displacement.jbp"),
            ("ShadowConjuration", "Spells/Level4/ShadowConjuration.jbp"), ("ShadowEvocation", "Spells/Level5/ShadowEvocation.jbp"), ("PhantasmalPutrefaction", "Spells/Level6/PhantasmalPutrefaction.jbp"),
            ("ShadowConjurationGreater", "Spells/Level7/ShadowConjurationGreater.jbp"), ("ShadowEvocationGreater", "Spells/Level8/ShadowEvocationGreater.jbp"), ("Shades", "Spells/Level9/Shades.jbp"),
        };

        // 본편 아이콘 원본(유일한 원장). 모두 m_Icon 이 비어 있지 않음(시험 icons: jbp 가 다시 확인).
        public static readonly IReadOnlyList<IconSource> Sources = BuildSources();

        static IReadOnlyList<IconSource> BuildSources()
        {
            var l = new List<IconSource>
            {
                new IconSource("Echo", "237427308e48c3341b3d532b9d3a001f", "ShadowEvocation", "Spells/Level5/ShadowEvocation.jbp", "그림자 메아리 — 그림자가 주문을 되풀이(그림자 방출)"),
                new IconSource("Doubles", "3e4ab69ada402d145a5e0ad3ad4b8564", "MirrorImage", "Spells/Level2/MirrorImage.jbp", "분신 표시 3 — 분신(거울 분신)"),
                new IconSource("Slots", Guids.Game.BloodlineArcaneProgression, "BloodlineArcaneProgression", "Classes/Sorcerer/ArcaneBloodline/BloodlineArcaneProgression.jbp", "하루 주문 칸 −1 표시 — 소서러 비전 혈통(주문 칸) 그림"),
            };
            if (BonusSpellJbp.Length != ShadowRules.BonusSpells.Count) throw new InvalidOperationException("ShadowSorcerer: IconTable bonus spell paths " + BonusSpellJbp.Length + " != ShadowRules.BonusSpells " + ShadowRules.BonusSpells.Count);
            for (int i = 0; i < BonusSpellJbp.Length; i++)
                l.Add(new IconSource("Bonus" + (i + 1), ShadowRules.BonusSpells[i], BonusSpellJbp[i].name, BonusSpellJbp[i].path, "보너스 주문 " + (i + 1) + "레벨 — 가르치는 주문 " + BonusSpellJbp[i].name + " 그 자체"));
            return l;
        }

        // 우리 블루프린트 GUID → 아이콘 원본 키.
        public static readonly IReadOnlyDictionary<string, string> Targets = BuildTargets();

        public const string ReasonArchetype = "archetype";   // 아키타입: 본편 아키타입 213개 중 212개가 m_Icon null(blueprints.zip 2026-11-15 전수) — Icons.Apply 가 형식을 확인
        public const string ReasonHiddenShell = "hidden-shell"; // 시각 전용 그림자 유닛의 껍데기 특성: HideInUI 이고 그 유닛은 대상 불가라 어떤 UI 에도 안 나온다 — Icons.Apply 가 숨김을 확인
        public static readonly IReadOnlyDictionary<string, string> NoIcon = new Dictionary<string, string> { [Guids.Archetype] = ReasonArchetype, [Guids.ShadowDoubleShell] = ReasonHiddenShell };

        static Dictionary<string, string> BuildTargets()
        {
            var t = new Dictionary<string, string>();
            Action<string, string> add = (g, k) => { if (t.ContainsKey(g)) throw new InvalidOperationException("ShadowSorcerer: IconTable target listed twice: " + g); t.Add(g, k); };
            add(Guids.EchoFeature, "Echo");
            add(Guids.Doubles1, "Doubles"); add(Guids.Doubles2, "Doubles"); add(Guids.Doubles3, "Doubles");
            add(Guids.SlotsFeature, "Slots");
            add(Guids.EchoBuff, "Echo");
            add(Guids.EchoToggle, "Echo");
            for (int i = 0; i < Guids.BonusFeatures.Length; i++) add(Guids.BonusFeatures[i], "Bonus" + (i + 1));
            return t;
        }

        public static IconSource SourceOf(string target)
        {
            string key;
            if (!Targets.TryGetValue(target, out key)) throw new KeyNotFoundException("ShadowSorcerer: no icon source for " + target);
            var hit = Sources.Where(s => s.Key == key).ToArray();
            if (hit.Length != 1) throw new KeyNotFoundException("ShadowSorcerer: icon source key '" + key + "' is in Sources " + hit.Length + " time(s)");
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
            foreach (var s in Sources)
            {
                Guid parsed;
                if (s.Guid == null || !Guid.TryParseExact(s.Guid, "N", out parsed) || s.Guid != s.Guid.ToLowerInvariant()) p.Add("source " + s.Key + " GUID '" + s.Guid + "' is not 32 lowercase hex");
                if (ownSet.Contains(s.Guid)) p.Add("source " + s.Key + " is one of our own blueprints (needs a vanilla source)");
                if (string.IsNullOrEmpty(s.Reason) || string.IsNullOrEmpty(s.JbpPath) || string.IsNullOrEmpty(s.VanillaName)) p.Add("source " + s.Key + " has no reason / path / name");
                if (!Targets.Values.Contains(s.Key)) p.Add("source " + s.Key + " is unused");
            }
            foreach (var kv in Targets) if (!Sources.Any(s => s.Key == kv.Value)) p.Add("target " + kv.Key + " -> unknown source key " + kv.Value);
            // 보너스 주문 특성의 원본 = BonusSpellPlan 이 그 특성에 넣는 주문(C4 — 표와 계획이 어긋나면 실패).
            foreach (var r in BonusSpellPlan.Rows)
            {
                string key;
                if (!Targets.TryGetValue(r.FeatureGuid, out key)) { p.Add("bonus feature " + r.FeatureGuid + " has no icon target"); continue; }
                var src = Sources.FirstOrDefault(s => s.Key == key);
                if (src == null || src.Guid != r.SpellGuid) p.Add("bonus feature " + r.FeatureGuid + " icon source " + (src == null ? "none" : src.Guid) + " != its spell " + r.SpellGuid);
            }
            foreach (var g in Targets.Keys.Intersect(NoIcon.Keys)) p.Add(g + " is both an icon target and no-icon");
            foreach (var g in Targets.Keys.Concat(NoIcon.Keys)) if (!ownSet.Contains(g)) p.Add(g + " is in IconTable but not one of our blueprints");
            foreach (var g in ownSet) if (!Targets.ContainsKey(g) && !NoIcon.ContainsKey(g)) p.Add(g + " (ours) has neither an icon source nor a no-icon reason");
            foreach (var kv in NoIcon) if (kv.Value != ReasonArchetype && kv.Value != ReasonHiddenShell) p.Add(kv.Key + " no-icon reason '" + kv.Value + "' unknown");
            return p;
        }
    }
}
