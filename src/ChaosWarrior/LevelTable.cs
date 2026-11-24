using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
    // [순수] 레벨 표(설계 §5·§10-b + 판정 2026-11-14): 워프리스트 진행표에서 지우는 16행 + 남아야 하는 행. 레벨은 ChaosRules.Levels 에서 파생한다.
    public struct LevelRow
    {
        public readonly int Level;
        public readonly string Guid, Name;
        public LevelRow(int level, string guid, string name) { Level = level; Guid = guid; Name = name; }
        public override string ToString() { return Level + ":" + Name; }
    }

    public static class LevelTable
    {
        static LevelRow R(int level, string guid, string name) { return new LevelRow(level, guid, name); }

        public static readonly IReadOnlyList<LevelRow> Removals = BuildRemovals();

        static LevelRow[] BuildRemovals()
        {
            var l = new List<LevelRow>
            {
                // 판정 2026-11-14: 축복은 신 영역 안 1개 — 두 번째 선택 칸만 제거(첫 칸 BlessingSelection 이 축복 자원을 갖는다).
                R(ChaosRules.Levels.BlessingLevel, Guids.Game.SecondBlessingSelection, "SecondBlessingSelection"),
                R(ChaosRules.Levels.FervorBaseLevel, Guids.Game.WarpriestFervorBase, "WarpriestFervorBase"),
                R(ChaosRules.Levels.FervorBaseLevel, Guids.Game.WarpriestFervorHealDamageSelection, "WarpriestFervorHealDamageSelection"),
                R(ChaosRules.Levels.FervorBaseLevel, Guids.Game.WarpriestFervorQuickenCast, "WarpriestFervorQuickenCast"),
                R(ChaosRules.Levels.ChannelLevel, Guids.Game.WarpriestChannelEnergySelection, "WarpriestChannelEnergySelection"),
            };
            foreach (int lv in ChaosRules.Levels.FervorRankLevels) l.Add(R(lv, Guids.Game.WarpriestFervorHealDamageRank, "WarpriestFervorHealDamageRank"));
            var armor = new[]
            {
                Tuple.Create(Guids.Game.SacredArmorFeature, "SacredArmorFeature"), Tuple.Create(Guids.Game.SacredArmorEnchantPlus2, "SacredArmorEnchantPlus2"),
                Tuple.Create(Guids.Game.SacredArmorEnchantPlus3, "SacredArmorEnchantPlus3"), Tuple.Create(Guids.Game.SacredArmorEnchantPlus4, "SacredArmorEnchantPlus4"),
                Tuple.Create(Guids.Game.SacredArmorEnchantPlus5, "SacredArmorEnchantPlus5"),
            };
            if (armor.Length != ChaosRules.Levels.SacredArmorLevels.Length) throw new InvalidOperationException("ChaosWarrior: sacred armor rows " + armor.Length + " != SacredArmorLevels " + ChaosRules.Levels.SacredArmorLevels.Length);
            for (int i = 0; i < armor.Length; i++) l.Add(R(ChaosRules.Levels.SacredArmorLevels[i], armor[i].Item1, armor[i].Item2));
            return l.ToArray();
        }

        // 유지(설계 §5 「유지」 줄): 첫 축복 칸 · 보너스 특기 3·6·9·12·15·18 · 성스러운 무기 4·8·12·16·20 · 20레벨 AspectOfWar. 시험·S1 이 읽는다(생산 빌더는 쓰지 않는다).
        public static readonly IReadOnlyList<LevelRow> Kept = new[]
        {
            R(1, Guids.Game.BlessingSelection, "BlessingSelection"),
            R(3, Guids.Game.WarpriestFeatSelection, "WarpriestFeatSelection"), R(6, Guids.Game.WarpriestFeatSelection, "WarpriestFeatSelection"),
            R(9, Guids.Game.WarpriestFeatSelection, "WarpriestFeatSelection"), R(12, Guids.Game.WarpriestFeatSelection, "WarpriestFeatSelection"),
            R(15, Guids.Game.WarpriestFeatSelection, "WarpriestFeatSelection"), R(18, Guids.Game.WarpriestFeatSelection, "WarpriestFeatSelection"),
            R(4, Guids.Game.SacredWeaponEnchantFeature, "SacredWeaponEnchantFeature"), R(8, Guids.Game.SacredWeaponEnchantPlus2, "SacredWeaponEnchantPlus2"),
            R(12, Guids.Game.SacredWeaponEnchantPlus3, "SacredWeaponEnchantPlus3"), R(16, Guids.Game.SacredWeaponEnchantPlus4, "SacredWeaponEnchantPlus4"),
            R(20, Guids.Game.SacredWeaponEnchantPlus5, "SacredWeaponEnchantPlus5"), R(20, Guids.Game.WarpriestAspectOfWar, "WarpriestAspectOfWar"),
        };

        // 엔진 BlueprintArchetype.GetRemoveEntry 규칙 = 같은 레벨·같은 GUID 만 지운다. 제거 행마다 진행표의 그 레벨에서 첫 일치 하나를 지운다.
        // 제거 행이 진행표 그 레벨에 없으면 InvalidOperationException(다른 모드가 진행표를 바꿨거나 표가 틀림 — 조용히 넘기지 않는다). 반환 = 남는 항목.
        public static IReadOnlyList<LevelRow> Apply(IReadOnlyList<LevelRow> progression, IReadOnlyList<LevelRow> removals)
        {
            if (progression == null) throw new ArgumentNullException("progression");
            if (removals == null) throw new ArgumentNullException("removals");
            var left = progression.ToList();
            foreach (var r in removals)
            {
                if (string.IsNullOrEmpty(r.Guid)) throw new ArgumentException("ChaosWarrior: removal row without a GUID (" + r + ")", "removals");
                int at = left.FindIndex(p => p.Level == r.Level && p.Guid == r.Guid);
                if (at < 0) throw new InvalidOperationException("ChaosWarrior: removal " + r + " (" + r.Guid + ") is not in the progression at level " + r.Level);
                left.RemoveAt(at);
            }
            return left;
        }
    }
}
