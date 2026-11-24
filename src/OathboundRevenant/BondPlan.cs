using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
     
    public sealed class BondRow
    {
        public readonly string Key, CheckedFactGuid, InnerKey;
        public BondRow(string key, string checkedFact, string inner) { Key = key; CheckedFactGuid = checkedFact; InnerKey = inner; }
    }

    public sealed class EnchantRow
    {
        public readonly string Key, Guid, Family;
        public readonly int Cost;
        public readonly bool IsVanilla;   // 본편 인챈트를 그대로 참조(새 인챈트 아님)
        public EnchantRow(string key, string guid, int cost, string family, bool vanilla) { Key = key; Guid = guid; Cost = cost; Family = family; IsVanilla = vanilla; }
    }

    public sealed class NecroticRow
    {
        public readonly string Condition, Energy;
        public readonly int Dice;
        public NecroticRow(string condition, string energy, int dice) { Condition = condition; Energy = energy; Dice = dice; }
    }

    public static class BondPlan
    {
        // 출처: 설계 §8-d (가) 탈것을 고르면 말이 언데드 — 확인 사실 = 탈것 선택이 남기는 PaladinDivineMountProgression(부록 A-n), (나) 무기 = WeaponBondFeature(5레벨 항목).
        public static readonly IReadOnlyList<BondRow> Rows = new[]
        {
            new BondRow("RevenantUndeadMount", Guids.Game.PaladinDivineMountProgression, "RevenantUndeadMountInner"),
            new BondRow("RevenantWeaponBond", Guids.Game.WeaponBondFeature, "RevenantWeaponChoices"),
        };

        // 출처: 설계 §8-d (나) 인챈트 4 + 비용(부록 A-n 계열 표·판정 P16 흡혈 1·P18 공포의 칼날 2). 유령 접촉 = 본편 GhostTouch 그대로.
        public static readonly IReadOnlyList<EnchantRow> Enchants = new[]
        {
            new EnchantRow("Necrotic", Guids.RevenantNecroticEnchant, RevenantRules.Dials.NecroticCost, "vanilla Necrotic cost 1", false),
            new EnchantRow("Vampiric", Guids.RevenantVampiricEnchant, RevenantRules.Dials.VampiricCost, "P16 (KissOfVampire/BloodthirstVampiricTouch cost 1)", false),
            new EnchantRow("GhostTouch", Guids.Game.GhostTouch, RevenantRules.Dials.GhostTouchCost, "vanilla GhostTouch cost 1", true),
            new EnchantRow("DreadBlade", Guids.RevenantDreadBladeEnchant, RevenantRules.Dials.DreadBladeCost, "P18", false),
        };

        // 출처: 설계 §8-d 죽음의 기운(+1d6 음에너지, 친화 대상 신성) + P15(구성체 0 — 본편 Necrotic 조건 그대로).
        public static readonly IReadOnlyList<NecroticRow> NecroticRows = new[]
        {
            new NecroticRow("!NegativeEnergyAffinity,!ConstructType", StrikePlan.Negative, RevenantRules.Dials.DeathBladeDice),
            new NecroticRow("NegativeEnergyAffinity", StrikePlan.Divine, RevenantRules.Dials.DeathBladeDice),
        };

        public static EnchantRow Enchant(string key)
        {
            var r = Enchants.FirstOrDefault(x => x.Key == key);
            if (r == null) throw new ArgumentException("OathboundRevenant: no bond enchant row " + key);
            return r;
        }

        // 선택지 플래그(본편 WeaponBondFlamingChoice 와 같음 — 복제 되읽기 기준, blueprints.zip 2026-11-14).
        public static readonly bool ChoiceDeactivateImmediately = true;
        public static readonly string ChoiceActivationType = "Immediately";
        public static readonly string ChoiceCommand = "Free";
        public static readonly string ChoiceGroup = "DivineWeaponProperty";
        public static readonly bool ChoiceOnByDefault = false;
    }

    public enum MountHpVerdict { Apply, Defer, Missing }

    public static class MountHpRule
    {
        // 판정 P14: 팔라딘 매력 수정치를 말의 매력 「점수」에 더한다(음수 그대로) — HP 는 언데드 매력 규칙(UndeadHealth)이 따라간다.
        public static int ChaScoreBonus(int paladinChaMod) { return paladinChaMod; }

        // 검토 M6: 주인(팔라딘) 결측 처리. 켜질 때(로드 중 포함 — Master 참조가 아직 안 풀렸을 수 있음) = 미룸(다음 라운드에 다시 계산, 오류 아님),
        // 라운드 틱에서도 없으면 = 판정 불가(Missing — 보고). 있으면 적용.
        public static MountHpVerdict OnMaster(bool masterPresent, bool atRoundTick)
        {
            if (masterPresent) return MountHpVerdict.Apply;
            return atRoundTick ? MountHpVerdict.Missing : MountHpVerdict.Defer;
        }




    }

    // 2026-11-17 결함(점검 S16·S17): 5레벨 유대 특성의 AddFeatureIfHasFact 는 켜질 때(OnActivate)와 IOwnerGainLevelHandler 에서만 판정한다.
    // 레벨업 동작 순서(엔진 LevelUpActionPriority: Class 2 → ApplyClass 5 → Features 17, LevelUpController.AddAction IL_0101~011B 우선순위 삽입):
    //   SelectClass(AddClassLevel → 레벨 사건) → ApplyClassMechanics(우리 특성 추가 → OnActivate 판정: 탈것·무기 유대 선택 전이라 거짓) → SelectFeature(유대 선택).
    //   UI(LevelUpController.Commit → ApplyLevelup → ApplyLevelUpActions)와 생성(AddClassLevels.LevelUp) 둘 다 같은 순서 → 다음 레벨·불러오기까지 안쪽 특성이 없다.
    // 두 경로 모두 모든 동작 뒤 UnitProgressionData.ReapplyFeaturesOnLevelUp 을 부르고 그 끝(IL_0104)에서 IUnitReapplyFeaturesOnLevelUpHandler 를 올린다
    //   (ApplyLevelUpActions IL_00D0, AddClassLevels.LevelUp IL_02AE) → 그때 다시 판정한다.
    // 본편 ReapplyOnLevelUp 깃발(CavalierTacticianSupportFeature 선례)은 매 레벨업마다 안쪽 사실을 지웠다 다시 붙여 무기 유대 선택지의 켜짐 상태(기본 꺼짐)를 날리므로 쓰지 않는다
    //   → 확인 사실이 있는데 안쪽 특성이 없을 때만 한 번 다시 적용한다.
    public enum BondRecheckVerdict { Nothing, Reapply }

    public static class BondRecheckRule
    {
        public static BondRecheckVerdict Decide(bool hasCheckedFact, bool hasInnerFeature)
        {
            return hasCheckedFact && !hasInnerFeature ? BondRecheckVerdict.Reapply : BondRecheckVerdict.Nothing;
        }
    }
}
