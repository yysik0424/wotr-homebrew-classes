using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
     
    public sealed class LevelRow
    {
        public readonly int Level;
        public readonly string Key;      // 제거 = Guids.Game 상수 이름 / 추가 = 우리 특성 키
        public readonly string Guid;
        public readonly string FilledBy; // 추가 행만: 그 특성 본문을 채우는 Task(T1·T3 = 이미 완성, 나머지는 Archetype.Build 가 껍데기로 만든다)
        public readonly bool IsSelection;
        public LevelRow(int level, string key, string guid, string filledBy = null, bool isSelection = false) { Level = level; Key = key; Guid = guid; FilledBy = filledBy; IsSelection = isSelection; }
    }

    public static class LevelPlan
    {
        // 출처: 설계 §5 제거 열 + §3(자기 안수 비활성) — 2 안수 특성 · 3 신성한 건강·용기의 오라·자비 · 4 양에너지 방출 · 6·9·12·15·18 자비 = 10행.
        public static readonly IReadOnlyList<LevelRow> Removals = BuildRemovals();
        // 출처: 설계 §5·§6 추가 열 — 1 언데드 몸·맹세의 재생·스마이트 음에너지 · 2 망자의 손 · 3 묘지의 공포·죽음의 오라 · 4 음에너지 채널 · 20 안수 극대화 · 5 유대 언데드화 2(§8-d) · 3..18 재주 선택 ×6 = 16행.
        public static readonly IReadOnlyList<LevelRow> Additions = BuildAdditions();

        // 이 빌드에서 본문이 구현된 Task(빌드 되읽기: 추가 행의 FilledBy 가 여기 있으면 「채워짐」, 없으면 「껍데기」여야 한다 — 어긋나면 Register 가 던진다).
        public static readonly IReadOnlyList<string> ImplementedTasks = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8" };

        static IReadOnlyList<LevelRow> BuildRemovals()
        {
            var l = new List<LevelRow>
            {
                new LevelRow(RevenantRules.Levels.HandsReplaceLevel, "LayOnHandsFeature", Guids.Game.LayOnHandsFeature),
                new LevelRow(RevenantRules.Levels.MercyRemovedLevels[0], "DivineHealth", Guids.Game.DivineHealth),
                new LevelRow(RevenantRules.Levels.MercyRemovedLevels[0], "AuraOfCourageFeature", Guids.Game.AuraOfCourageFeature),
            };
            foreach (int lv in RevenantRules.Levels.MercyRemovedLevels) l.Add(new LevelRow(lv, "SelectionMercy", Guids.Game.SelectionMercy));
            l.Add(new LevelRow(RevenantRules.Levels.ChannelLevel, "ChannelEnergyPaladinFeature", Guids.Game.ChannelEnergyPaladinFeature));
            return l;
        }

        static IReadOnlyList<LevelRow> BuildAdditions()
        {
            var lv = RevenantRules.Levels.MinLevel;
            var l = new List<LevelRow>
            {
                new LevelRow(RevenantRules.Levels.UndeadBodyLevel, "UndeadBody", Guids.UndeadBodyFeature, "T1"),
                new LevelRow(lv, "OathRegeneration", Guids.OathRegeneration, "T4"),
                new LevelRow(lv, "SmiteNegative", Guids.SmiteNegative, "T4"),
                new LevelRow(RevenantRules.Levels.HandsReplaceLevel, "RevenantHands", Guids.RevenantHands, "T3"),
                new LevelRow(RevenantRules.Levels.TalentLevels[0], "GraveyardDread", Guids.GraveyardDread, "T4"),
                new LevelRow(RevenantRules.Levels.TalentLevels[0], "DeathAura", Guids.DeathAura, "T6"),
                new LevelRow(RevenantRules.Levels.ChannelLevel, "NegativeChannel", Guids.NegativeChannel, "T5"),
                new LevelRow(RevenantRules.Levels.HandsMaximizeLevel, "RevenantHandsMaximize", Guids.RevenantHandsMaximize, "T3"),
                // T8(설계 §8-d): 5레벨 신성한 유대 언데드화 2행.
                new LevelRow(RevenantRules.Levels.DivineBondLevel, "RevenantUndeadMount", Guids.RevenantUndeadMount, "T8"),
                new LevelRow(RevenantRules.Levels.DivineBondLevel, "RevenantWeaponBond", Guids.RevenantWeaponBond, "T8"),
            };
            foreach (int t in RevenantRules.Levels.TalentLevels) l.Add(new LevelRow(t, "TalentSelection", Guids.TalentSelection, "T3", true));
            return l;
        }

        // 추가 행의 서로 다른 키(같은 재주 선택이 6레벨에 나온다).
        public static IEnumerable<LevelRow> DistinctAdditionKeys() { return Additions.GroupBy(r => r.Key).Select(g => g.First()); }

        public static bool IsImplemented(LevelRow row)
        {
            if (row == null || row.FilledBy == null) throw new ArgumentException("OathboundRevenant: LevelPlan row without FilledBy");
            return ImplementedTasks.Contains(row.FilledBy);
        }
    }
}
