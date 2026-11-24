using System;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using static WotrClassMod.Common.BlueprintUtil;

namespace BulwarkMage
{
     
     
    // 외부 모드(2026-11-14 dnlib, 보고서 task-3 Step 0-F): TTT-Base EnableAdvancedArmorTraining·PrestigePlus PatchArmorTraining 은 FighterClass 진행표·아키타입만 고치고
    // ArmorTraining 블루프린트 자체는 쓰지 않는다 → 우리 위저드 아키타입은 영향 없음(TTT 켜짐에서도 7·11·15 가 선택지가 되지 않고 ArmorTraining 랭크가 오른다).
    // ArmorTraining 블루프린트 값은 런타임 되읽기 행이 잠근다(ForeignTolerance).
    public static class ArmorTrainingGrant
    {
        public static BlueprintFeature Feature;

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("BulwarkMage: ArmorTrainingGrant.Build() called twice");
            var levels = BulwarkRules.Levels.ArmorTrainingLevels;
            if (levels == null || levels.Count == 0 || levels.Distinct().Count() != levels.Count) throw new InvalidOperationException("BulwarkMage: armor training levels are empty or repeat a level");
            var at = Get<BlueprintFeature>(Guids.Game.ArmorTraining);
            // 랭크 상한이 줄 횟수보다 작으면 마지막 부여가 조용히 무시된다 → 빌드 실패로 드러낸다.
            if (at.Ranks < levels.Count) throw new InvalidOperationException("BulwarkMage: ArmorTraining has Ranks " + at.Ranks + " < " + levels.Count + " grants (another mod changed it?)");
            foreach (int l in levels) Archetype.AddAt(l, at);
            Feature = at;
        }
    }
}
