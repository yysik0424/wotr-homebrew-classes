using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    //  Removals = 제거 레벨(ChimeraRules.Levels.RemovedBonusFeatLevels) 각각을 그 레벨의 본편 보너스 특기 선택으로 사상.
    //    본편 MonkProgression(blueprints.zip, 시험 `jbp: monk progression bonus feats`): (1,Level1)(2,Level1)(6,Level6)(10,Level10)(14,Level10)(18,Level10).
    //    엔진 BlueprintArchetype.GetRemoveEntry @001D~0023 은 RemoveFeatures 중 Level == level 항목만 → 같은 선택을 공유하는 1·14·18 은 남는다(부록 A-g).
    //    + 소유자 결정 2026-11-16: 명경지수·빠른 이동(RemovedCoreFeatures — 3레벨 MonkFastMovementUnlock, 4레벨 StillMind).
    //  Additions = (레벨, 키). 키 → 블루프린트는 그 키를 만드는 빌더가 Archetype.AddPlanned(key, bp) 로 붙인다. Register 가 모든 행이 붙었는지 되읽는다.
    public static class LevelPlan
    {
        public const string KeyFct = "FCT";
        public const string KeyMutationSelection = "MutationSelection";
        public const string KeyMutagen = "Mutagen"; // T4: 1레벨 키메라 변이약(설계 §6)
        public const string KeyBody = "Body";       // T5: 1레벨 숨김 몸 특성(사다리 주사위 컴포넌트, 계획 T5)

        // 본편 보너스 특기 선택(몽크 레벨 → 그 레벨 항목의 선택). 1..20 밖·보너스 특기가 없는 레벨 = 던짐.
        public static string BonusFeatSelectionAt(int level)
        {
            switch (level)
            {
                case 1: case 2: return Guids.Game.MonkBonusFeatSelectionLevel1;
                case 6: return Guids.Game.MonkBonusFeatSelectionLevel6;
                case 10: case 14: case 18: return Guids.Game.MonkBonusFeatSelectionLevel10;
                default: throw new ArgumentOutOfRangeException("level", level, "ChimeraMonk: MonkProgression has no bonus feat selection at level " + level);
            }
        }

        // 소유자 결정 2026-11-16: 명경지수·빠른 이동 제거(대가로 주는 것 없음). 제거 표는 이 한 곳 — 행 = (본편 블루프린트 이름, GUID, 본편 진행표에서 그 특성이 있는 레벨 전부).
        //  출처 = blueprints.zip Classes/Monk/MonkProgression.jbp LevelEntries(시험 `jbp: monk progression removed core features` 가 실제 zip 과 대조):
        //   빠른 이동 = 3레벨 MonkFastMovementUnlock 하나(속도 증가는 그 안의 MonkFastMovement 가 몽크 레벨로 계산 — 진행표 항목은 이 하나뿐),
        //   명경지수 = 4레벨 StillMind 하나.
        //  Archetype.Build 가 실행 시점의 진행표와 이 표를 대조한다(이름·레벨 집합이 다르면 던짐 — 다른 모드가 진행표를 바꾼 경우 포함).
        public sealed class CoreRemoval
        {
            public readonly string Name, Guid;
            public readonly int[] Levels;
            public CoreRemoval(string name, string guid, int[] levels)
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(guid)) throw new ArgumentException("ChimeraMonk: core removal row needs a blueprint name and a GUID");
                if (levels == null || levels.Length == 0) throw new ArgumentException("ChimeraMonk: core removal " + name + " has no level");
                if (levels.Distinct().Count() != levels.Length || levels.Any(l => l < ChimeraRules.Levels.MinLevel || l > ChimeraRules.Levels.MaxLevel))
                    throw new ArgumentOutOfRangeException("levels", "ChimeraMonk: core removal " + name + " levels {" + string.Join(",", levels) + "} must be distinct and within 1..20");
                Name = name; Guid = guid; Levels = levels.OrderBy(l => l).ToArray();
            }
        }

        public static IReadOnlyList<CoreRemoval> RemovedCoreFeatures
        {
            get
            {
                return new[]
                {
                    new CoreRemoval("MonkFastMovementUnlock", Guids.Game.MonkFastMovementUnlock, ChimeraRules.Levels.FastMovementRemovedLevels),
                    new CoreRemoval("MonkStyleStrike", Guids.Game.MonkStyleStrike, ChimeraRules.Levels.RemovedStyleStrikeLevels),
                    new CoreRemoval("StillMind", Guids.Game.StillMind, ChimeraRules.Levels.StillMindRemovedLevels),
                };
            }
        }

        // 제거 대상이 「주는」 특성 — 본편 진행표의 직접 항목이 아니어야 제거가 완전하다(직접 항목으로도 있으면 Archetype.Build 가 던진다). 점검 S1 은 유닛에 이 특성이 없음을 본다.
        public static IReadOnlyList<(string Name, string Guid)> RemovedGrantedFeatures
        {
            get { return new[] { ("MonkFastMovement", Guids.Game.MonkFastMovement) }; }
        }

        public static IReadOnlyList<(int Level, string Guid)> Removals
        {
            get
            {
                var list = new List<(int Level, string Guid)>();
                foreach (var row in RemovedCoreFeatures) list.AddRange(row.Levels.Select(l => (l, row.Guid)));
                return list;
            }
        }

        public static IReadOnlyList<(int Level, string Key)> Additions
        {
            get
            {
                var list = new List<(int, string)> { (ChimeraRules.Levels.MinLevel, KeyFct), (ChimeraRules.Levels.MinLevel, KeyMutagen), (ChimeraRules.Levels.MinLevel, KeyBody) };
                list.AddRange(ChimeraRules.Levels.MutationLevels.Select(l => (l, KeyMutationSelection)));
                return list;
            }
        }
    }
}
