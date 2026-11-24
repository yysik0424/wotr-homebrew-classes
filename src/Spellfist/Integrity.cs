using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spellfist
{
    // 남의 블루프린트 무변조 기록(설계 §6-9, 검수 S9). 우리 빌드가 쓰는 본편·브롤러 모드 블루프린트의 「배열 참조」를 빌드 맨 앞에서 적어 두고,
    // 점검(S9)이 지금 참조와 같은지 본다 — 우리 코드가 배열을 새로 만들어 끼우면(복제 대신 원본 수정) 참조가 바뀌어 드러난다.
    // 기록 시점 = Archetype.Build 의 첫 호출(BuildAll 의 첫 빌드 단계 — 그 뒤의 모든 우리 쓰기보다 먼저). BuildAll 의 호출 순서는 시험이 고정하므로 여기서 부른다.
    // 한계(README): 배열 「안」의 원소를 제자리에서 바꾸는 변조는 참조 비교로 안 보인다. 익스플로잇 원본은 Exploits.Build 9) 가 원소 수준(복제 GUID 참조·클래스·매력 개수)까지 따로 본다.
    // 브롤러 m_Archetypes 는 우리 Register 가 일부러 바꾸므로(새 배열) 표에 없다.
    public static class Integrity
    {
        public sealed class Entry
        {
            public string Label;
            public object Captured;
            public Func<object> Read;
        }

        static readonly List<Entry> entries = new List<Entry>();
        static bool captured;





        // 빌드 맨 앞에서 한 번. 대상이 캐시에 없으면 Get/Archetype.Mod 가 던진다(빌드가 어차피 그 블루프린트를 읽는다 → BuildFailed 로 드러남).
        internal static void Capture()
        {
            if (captured) throw new InvalidOperationException("Spellfist: Integrity.Capture() called twice");
            Add("ArcanistSpellbook.ComponentsArray", Get<BlueprintSpellbook>(Guids.Game.ArcanistSpellbook), b => b.ComponentsArray);
            var sel = Get<BlueprintFeatureSelection>(Guids.Game.ArcanistExploitSelection);
            Add("ArcanistExploitSelection.ComponentsArray", sel, b => b.ComponentsArray);
            Add("ArcanistExploitSelection.m_AllFeatures", sel, b => Util.GetF(b, "m_AllFeatures"));
            foreach (var r in ExploitGuids.Chain)
            {
                var bp = ResourcesLibrary.TryGetBlueprint<BlueprintScriptableObject>(r.source);
                if (bp == null) throw new InvalidOperationException("Spellfist: Integrity: exploit source " + r.exploit + " " + r.kind + " " + r.source + " not in cache");
                Add("exploit source " + bp.name + ".ComponentsArray", bp, b => b.ComponentsArray);
            }
            Add("BrawlerProgression.LevelEntries", Archetype.Mod<BlueprintProgression>(Guids.Game.BrawlerProgression), b => b.LevelEntries);
            foreach (var g in SpellfistRules.BrawlerModArchetypes)
            {
                var a = Archetype.Mod<BlueprintArchetype>(g);
                Add("brawler archetype " + a.name + ".ComponentsArray", a, b => b.ComponentsArray);
                Add("brawler archetype " + a.name + ".AddFeatures", a, b => b.AddFeatures);
                Add("brawler archetype " + a.name + ".RemoveFeatures", a, b => b.RemoveFeatures);
            }
            Add("WizardSpellList.SpellsByLevel", Get<BlueprintSpellList>(Guids.Game.WizardSpellList), b => b.SpellsByLevel);
            captured = true;
        }

        static void Add<T>(string label, T bp, Func<T, object> read) where T : class
        {
            if (bp == null) throw new InvalidOperationException("Spellfist: Integrity: " + label + " owner is null");
            // null 도 그대로 기록한다: 제자리에서 바꿀 원소가 없으므로 「null → 새 배열」 교체만 가능하고 그것은 참조 비교가 잡는다.
            // 여기서 던지면 점검과 무관한 일반 플레이에서 빌드가 실패한다(이 기록은 늘 돈다) — 점검 S9 가 null 로 기록된 항목을 FAIL 로 드러낸다(판정 불가 = 통과 아님).
            entries.Add(new Entry { Label = label, Captured = read(bp), Read = () => read(bp) });
        }




    }
}
