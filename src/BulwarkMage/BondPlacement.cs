using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
     
    // 게임 쪽 BondOffHandEnchant 컴포넌트(강화 층 버프 n 에 하나)가 장비 사건마다 유닛의 손 칸 방패들(쉬는 세트 포함 + 방금 칸을 떠난 방패)을 읽어 이 판정을 부른다.
    // 판정: 대상 = (컴포넌트 활성) ∧ (활성 보조손) ∧ (타워실드) 인 방패 하나. 대상은 우리 마법부여 정확히 1개, 나머지는 0개 — 차이만큼 붙이고 뗀다.
    // 저장 상태 없음: 「우리 것」은 방패에 붙은 마법부여의 블루프린트(BondEnchant{n}, 우리 GUID)로 센다 — 엔진이 아이템 사실로 저장하는 것 그대로.
    // 판정 불가(C1): 활성 보조손 방패의 타워실드 여부를 못 읽으면(Unknown) 그 방패는 대상이 아니고(붙이지 않음) 문제 목록에 남는다 → 호출자가 ERROR 로 센다.
    public enum ShieldTowerKind { Tower, NotTower, Unknown }

    public sealed class ShieldView
    {
        public readonly string Id;          // 호출자의 식별자(시험·로그용)
        public readonly bool ActiveOffHand;  // 지금 손 세트의 보조손 칸에 있는가
        public readonly ShieldTowerKind Tower;
        public readonly int OurCount;        // 그 방패 방어구 구성요소에 붙은 우리 마법부여(이 층) 수
        public ShieldView(string id, bool activeOffHand, ShieldTowerKind tower, int ourCount)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("BulwarkMage: shield view without an id", "id");
            if (ourCount < 0) throw new ArgumentOutOfRangeException("ourCount", ourCount, "BulwarkMage: an enchantment count cannot be negative");
            Id = id; ActiveOffHand = activeOffHand; Tower = tower; OurCount = ourCount;
        }
    }

    public sealed class PlacementPlan
    {
        public readonly IReadOnlyList<KeyValuePair<string, int>> Changes;  // (방패 id, +1 붙임 / −k 뗌), 바뀌지 않는 방패는 없음
        public readonly IReadOnlyList<string> Problems;                   // 판정 불가·중복 등(호출자가 Report)
        public readonly string Target;                                    // 붙어 있어야 할 방패 id(없으면 null)
        public PlacementPlan(List<KeyValuePair<string, int>> changes, List<string> problems, string target) { Changes = changes; Problems = problems; Target = target; }
    }

    public static class BondPlacement
    {
        public static PlacementPlan Decide(bool componentActive, IReadOnlyList<ShieldView> shields)
        {
            if (shields == null) throw new ArgumentNullException("shields");
            if (shields.Select(s => s.Id).Distinct().Count() != shields.Count) throw new ArgumentException("BulwarkMage: duplicate shield id in the placement input", "shields");
            if (shields.Count(s => s.ActiveOffHand) > 1) throw new ArgumentException("BulwarkMage: more than one shield claims the active off hand", "shields");
            var problems = new List<string>();
            string target = null;
            var off = shields.FirstOrDefault(s => s.ActiveOffHand);
            if (componentActive && off != null)
            {
                if (off.Tower == ShieldTowerKind.Tower) target = off.Id;
                else if (off.Tower == ShieldTowerKind.Unknown) problems.Add("cannot read whether the off-hand shield " + off.Id + " is a tower shield - no enhancement placed");
            }
            var changes = new List<KeyValuePair<string, int>>();
            foreach (var s in shields)
            {
                int want = s.Id == target ? 1 : 0;
                if (s.OurCount > 1) problems.Add("shield " + s.Id + " carries " + s.OurCount + " of our enchantments (expected at most 1) - extras removed");
                if (s.OurCount != want) changes.Add(new KeyValuePair<string, int>(s.Id, want - s.OurCount));
            }
            return new PlacementPlan(changes, problems, target);
        }
    }
}
