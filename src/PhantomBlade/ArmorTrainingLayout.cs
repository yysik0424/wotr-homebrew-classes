using System;
using System.Collections.Generic;
using System.Linq;

namespace PhantomBlade
{
    // 게임 타입 의존 없음(콘솔 시험이 컴파일한다). 귀검사가 전사 진행표에서 지우는 「갑옷 훈련 계열」을 실행 중 배치에서 고른다.
    // 핫픽스 1 (2026-11-08): 설계(§4.1 교환표 「ArmorTraining 제거 L3·7·11·15」)는 본편 배치를 가정했으나, TabletopTweaks-Base 2.6.18 의
     
    //   TabletopTweaks.Base.Bugfixes.Classes.Fighter/BlueprintsCache_Init_Patch::<PatchBase>g__EnableAdvancedArmorTraining|2_4
    //   - ArmorTraining 이 있는 모든 LevelEntry 에 ArmorTrainingSpeedFeature 추가 (b__20 Where → b__21 ForEach, IL 00A3~00C6)
    //   - 그중 Level > 3 인 항목: ArmorTrainingSelection 추가 + ArmorTraining 제거 (b__2_22 ldc.i4.3/cgt → b__23 → b__24, IL 00CB~0112)
    // 그래서 아는 배치는 둘뿐이다. 그 밖(둘 다 있음·둘 다 없음·섞임·중복)은 전부 던진다 — 조용히 일부만 지우지 않는다.
    public static class ArmorTrainingLayout
    {
        public const string Vanilla = "vanilla";
        public const string TabletopTweaks = "TabletopTweaks";
        // 게임 로그(UMM)에 남는 줄의 머리. README 귀검사 절 「다른 모드 영향」이 같은 문자열을 적는다(시험이 대조).
        public const string LogPrefix = "armor training layout: ";

         
        // Task 6b(2026-11-09): 리터럴을 두지 않고 러시 표(StandRules.RushTable)에서 갑옷 숙달 자리(19)를 뺀 레벨로 만든다 — 러시 특성 하나가 갑옷 훈련 하나를 대신한다.
        // 표와 실제 진행표가 어긋나면 Classify 가 그 레벨에서 「nothing to remove」로 던진다(조용히 넘어가지 않음). 시험: atlayout Levels == 3,7,11,15.
        public static readonly int[] Levels = StandRules.RushTable.Select(r => r.Level).Where(l => l != StandRules.ArmorMasteryRemovedLevel).ToArray();
        // TTT 가 ArmorTraining 을 선택지로 바꾸는 레벨 경계: Level > 3 (위 b__2_22 의 ldc.i4.3; cgt). 3레벨은 ArmorTraining 이 남는다.
        public const int TttSelectionAboveLevel = 3;

        static readonly string[] Family = { Guids.Game.ArmorTraining, Guids.Game.TttArmorTrainingSelection, Guids.Game.TttArmorTrainingSpeedFeature };

        public sealed class Result
        {
            public string Layout;
            // 레벨 → 그 레벨에서 지울 블루프린트 GUID(32자 소문자 hex, 순서 고정: 단계 특성 먼저, 속도 특성 다음).
            public Dictionary<int, string[]> Remove = new Dictionary<int, string[]>();
        }





        // live: 레벨 → 실행 중 FighterProgression 그 레벨의 특성 GUID 목록(항목이 없으면 null). Levels 의 모든 레벨이 있어야 한다.
        public static Result Classify(IDictionary<int, IList<string>> live)
        {
            if (live == null) throw new ArgumentNullException("live");
            var r = new Result();
            var perLevel = new List<string>();
            foreach (int lv in Levels)
            {
                IList<string> g;
                if (!live.TryGetValue(lv, out g))
                    throw new InvalidOperationException("PhantomBlade: armor training layout: level " + lv + " was not read from FighterProgression (caller bug)");
                if (g == null)
                    throw new InvalidOperationException("PhantomBlade: FighterProgression has no level " + lv + " entry (nothing to remove)");
                foreach (var f in Family)
                {
                    int n = g.Count(x => x == f);
                    if (n > 1) throw new InvalidOperationException("PhantomBlade: FighterProgression level " + lv + " has " + f + " " + n + " times (unknown armor training layout)");
                }
                bool at = g.Contains(Guids.Game.ArmorTraining), sel = g.Contains(Guids.Game.TttArmorTrainingSelection), spd = g.Contains(Guids.Game.TttArmorTrainingSpeedFeature);
                string where = " in FighterProgression level " + lv + " [" + string.Join(", ", g.ToArray()) + "]";
                if (at && sel) throw new InvalidOperationException("PhantomBlade: both ArmorTraining and TabletopTweaks ArmorTrainingSelection" + where + " (unknown armor training layout)");
                if (!at && !sel) throw new InvalidOperationException("PhantomBlade: neither ArmorTraining nor TabletopTweaks ArmorTrainingSelection is" + where + " (nothing to remove)");

                string layout;
                string[] remove;
                if (at && !spd) { layout = Vanilla; remove = new[] { Guids.Game.ArmorTraining }; }
                else if (at) // ArmorTraining + 속도 특성: TTT 는 3레벨에서만 이렇게 둔다
                {
                    if (lv > TttSelectionAboveLevel) throw new InvalidOperationException("PhantomBlade: ArmorTraining + TabletopTweaks ArmorTrainingSpeedFeature above level " + TttSelectionAboveLevel + where + " (unknown armor training layout)");
                    layout = TabletopTweaks; remove = new[] { Guids.Game.ArmorTraining, Guids.Game.TttArmorTrainingSpeedFeature };
                }
                else // 선택지: TTT 는 3레벨 초과에서만, 언제나 속도 특성과 함께 둔다
                {
                    if (!spd) throw new InvalidOperationException("PhantomBlade: TabletopTweaks ArmorTrainingSelection without ArmorTrainingSpeedFeature" + where + " (unknown armor training layout)");
                    if (lv <= TttSelectionAboveLevel) throw new InvalidOperationException("PhantomBlade: TabletopTweaks ArmorTrainingSelection at level " + lv + " <= " + TttSelectionAboveLevel + where + " (unknown armor training layout)");
                    layout = TabletopTweaks; remove = new[] { Guids.Game.TttArmorTrainingSelection, Guids.Game.TttArmorTrainingSpeedFeature };
                }
                perLevel.Add(lv + "=" + layout);
                if (r.Layout == null) r.Layout = layout;
                r.Remove[lv] = remove;
            }
            if (perLevel.Select(x => x.Substring(x.IndexOf('=') + 1)).Distinct().Count() != 1)
                throw new InvalidOperationException("PhantomBlade: mixed armor training layout across levels (" + string.Join(", ", perLevel.ToArray()) + ")");
            return r;
        }

        // 로그 한 줄: "armor training layout: TabletopTweaks (L3: <guid>+<guid>; L7: …)".
        public static string Describe(Result r)
        {
            if (r == null) throw new ArgumentNullException("r");
            return LogPrefix + r.Layout + " (" + string.Join("; ", r.Remove.OrderBy(kv => kv.Key).Select(kv => "L" + kv.Key + ": " + string.Join("+", kv.Value)).ToArray()) + ")";
        }
    }
}
