using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
    public enum LimbAction { None, Modify, ReportNoLevel, ReportBadLevel }

    public struct LimbDecision
    {
        public readonly LimbAction Action;
        public readonly Dice Dice;
        public LimbDecision(LimbAction a, Dice d) { Action = a; Dice = d; }
    }

    // [순수] 사지 주사위 판정(계획 T5 `exec: limb filter representative`). 우리 사지 무기가 아니면 None(정당한 무동작 — 맨손·다른 무기),
    // 우리 사지인데 몽크 레벨 < 1 이면 ReportNoLevel(오류 — 이 컴포넌트는 아키타입 특성에만 붙는다, C1), 아니면 Modify(사다리 주사위).
    public static class LimbDiceRule
    {
        public static LimbDecision Decide(string weaponGuid, int monkLevel, bool mutagen, IReadOnlyCollection<string> ours)
        {
            if (ours == null) throw new ArgumentNullException("ours");
            if (ours.Count == 0) throw new ArgumentException("ChimeraMonk: limb dice rule with an empty weapon list", "ours");
            if (weaponGuid == null || !ours.Contains(weaponGuid)) return new LimbDecision(LimbAction.None, default(Dice));
            if (monkLevel < ChimeraRules.Levels.MinLevel) return new LimbDecision(LimbAction.ReportNoLevel, default(Dice));
            if (monkLevel > ChimeraRules.Levels.MaxLevel) return new LimbDecision(LimbAction.ReportBadLevel, default(Dice)); // 사다리 표 밖(조용히 20 으로 깎지 않음 — 독립 검토 M-1)
            return new LimbDecision(LimbAction.Modify, ChimeraRules.LimbDice(monkLevel, mutagen));
        }

        // 추가 팔 명중 보너스(설계 §7 추가 팔 강화): 추가 팔 무기이고 변이약 중이면 Dials.ArmMutagenAttack, 아니면 0.
        public static int ArmAttackBonus(string weaponGuid, bool mutagen) { return weaponGuid == NaturalWeaponsPlan.ArmGuid && mutagen ? ChimeraRules.Dials.ArmMutagenAttack : 0; }
    }
}
