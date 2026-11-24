using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
    // [순수] 중장법사의 본체 표(게임 형식 없음, modinfo testSources). 시험 `exec: body table vs transcribed copy` 가 시험 파일의
    // 독립 전사본(설계 §5.4 교환표·정찰 §8 에서 손으로 옮김)과 칸 단위로, `exec: removed vs jbp` 가 WizardProgression jbp 와 대조한다.
    public static class BodyTable
    {
         
         
        // 보너스 특기 레벨은 BulwarkRules.Levels.RemovedBonusFeatLevels 에서 파생한다(같은 값을 두 곳에 두지 않는다 — C4).
        public static readonly IReadOnlyList<(int level, string guid)> Removed = BuildRemoved();

        static (int level, string guid)[] BuildRemoved()
        {
            var l1 = new List<(int, string)>
            {
                (1, Guids.Game.ArcaneBondSelection),
                (1, Guids.Game.SpecialistSchoolSelection),
                (1, Guids.Game.ScribingScrolls),
            };
            var feats = BulwarkRules.Levels.RemovedBonusFeatLevels;
            if (feats == null || feats.Count == 0) throw new InvalidOperationException("BulwarkMage: BulwarkRules.Levels.RemovedBonusFeatLevels is empty");
            return l1.Concat(feats.Select(l => (l, Guids.Game.WizardFeatSelection))).ToArray();
        }

        // 중장 숙련(AddProficiencies.ArmorProficiencies)과 갑옷·방패 ASF 0(ArcaneArmorProficiency.Armor)이 받는 갑옷 숙련군 이름(엔진 열거형
        // Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup 의 이름 — 시험이 Assembly-CSharp 상수 1..7 과 대조한다).
        // 출처: 설계 §5.3(1)(2) 「경·중·중갑 + 버클러·경방패·중방패·타워실드」, 정찰 §4(열거값 입증). 바딩(8~10)은 넣지 않는다(설계 §5.3(2) 경계).
        public static readonly IReadOnlyList<string> ArmorProficiencyGroups = new[] { "Light", "Medium", "Heavy", "Buckler", "LightShield", "HeavyShield", "TowerShield" };

        // T3 — 얻는 것 중 본편 타워실드 스페셜리스트 기능을 그대로 참조하는 6종 = (레벨, 본편 GUID) 10쌍(설계 §4·§5.2, TowerShieldSpecialistArchetype a599da9a… AddFeatures 직독).
        // 타워실드 훈련(3·7·11·15)은 복제본(TowerShield.Training)이라 여기 없다. 레벨은 BulwarkRules.Levels 에서 파생한다(C4). 시험이 독립 전사본·TSS 아키타입 jbp 와 대조한다.
        public static readonly IReadOnlyList<(int level, string guid)> TowerShieldGrants = BuildTowerShieldGrants();

        static (int level, string guid)[] BuildTowerShieldGrants()
        {
            var L = BulwarkRules.Levels.BurstBarrierLevels;
            if (L == null || L.Count == 0) throw new InvalidOperationException("BulwarkMage: BulwarkRules.Levels.BurstBarrierLevels is empty");
            var list = L.Select(l => (l, Guids.Game.BurstBarrier)).ToList();
            list.Add((BulwarkRules.Levels.NegatePenaltyLevel, Guids.Game.TowerShieldNegatePenalty));
            list.Add((BulwarkRules.Levels.TouchShieldLevel, Guids.Game.TowerShieldTouchShield));
            list.Add((BulwarkRules.Levels.TotalCoverLevel, Guids.Game.TowerShieldTotalCover));
            list.Add((BulwarkRules.Levels.EvasionLevel, Guids.Game.TowerShieldEvasion));
            list.Add((BulwarkRules.Levels.ImprovedEvasionLevel, Guids.Game.TowerShieldImprovedEvasion));
            return list.ToArray();
        }
    }
}
