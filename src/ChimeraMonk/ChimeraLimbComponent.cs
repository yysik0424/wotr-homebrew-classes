using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    // 변이약 중 추가 팔 명중 +1. 판정은 순수 LimbDiceRule(시험 exe 가 실행) — 여기는 엔진 값 읽기·쓰기만.
    // 필터 = NaturalWeaponsPlan.WeaponGuids(우리 사지 무기 전부 — 행에서 파생) — 맨손(몽크 주먹)·다른 무기는 그대로(정당한 무동작). 소유자 몽크 레벨 0 = 오류(ReportOnce, C1).
    // 「변이약 중」 = Mutagen.Buffs(T4 원장 하나, C4) 중 하나라도 걸려 있음. 본문 전체 try/catch → ReportException("limb-dice").
    public class ChimeraLimbComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
        internal static bool MutagenActive(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            if (Mutagen.Buffs.Count == 0) throw new InvalidOperationException("ChimeraMonk: mutagen buffs not built");
            return Mutagen.Buffs.Any(b => u.Buffs.GetBuff(b) != null);
        }

        static DiceFormula ToFormula(Dice d) { return new DiceFormula(d.Count, Fab.EnumOf<DiceType>("D" + d.Sides)); }

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            try
            {
                ChimeraErrors.MaybeFault(ErrorLedger.SiteLimbDice);
                var bp = evt.Weapon == null ? null : evt.Weapon.Blueprint;
                string guid = bp == null ? null : bp.AssetGuid.ToString();
                int level = Owner == null ? 0 : Owner.Progression.GetClassLevel(Get<BlueprintCharacterClass>(Guids.Game.MonkClass));
                bool ours = guid != null && NaturalWeaponsPlan.WeaponGuids.Contains(guid);
                var d = LimbDiceRule.Decide(guid, level, ours && level >= ChimeraRules.Levels.MinLevel && level <= ChimeraRules.Levels.MaxLevel && MutagenActive(Owner), NaturalWeaponsPlan.WeaponGuids);
                switch (d.Action)
                {
                    case LimbAction.None: return;
                    case LimbAction.ReportNoLevel:
                        ChimeraErrors.ReportOnce(ErrorLedger.SiteLimbDice, "no-monk-level", (Owner == null ? "<no owner>" : Owner.CharacterName) + " has a Chimera limb weapon but monk level " + level + " - dice left unchanged");
                        return;
                    case LimbAction.ReportBadLevel:
                        ChimeraErrors.ReportOnce(ErrorLedger.SiteLimbDice, "bad-monk-level", (Owner == null ? "<no owner>" : Owner.CharacterName) + " monk level " + level + " is outside the limb dice ladder - dice left unchanged");
                        return;
                    case LimbAction.Modify:
                        evt.WeaponDamageDice.Modify(ToFormula(d.Dice), Fact);
                        return;
                }
            }
            catch (Exception e)
            {
                ChimeraErrors.ReportException(ErrorLedger.SiteLimbDice, e);
            }
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            try
            {
                ChimeraErrors.MaybeFault(ErrorLedger.SiteLimbDice);
                var bp = evt.Weapon == null ? null : evt.Weapon.Blueprint;
                if (bp == null || bp.AssetGuid.ToString() != NaturalWeaponsPlan.ArmGuid) return;
                int bonus = LimbDiceRule.ArmAttackBonus(bp.AssetGuid.ToString(), MutagenActive(Owner));
                if (bonus != 0) evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
            }
            catch (Exception e)
            {
                ChimeraErrors.ReportException(ErrorLedger.SiteLimbDice, e);
            }
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
    }
}
