using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
    // [순수] 재계산 1회 = UMM 로그 1줄(설계 §14 Q6 「고정 설명 + 로그」, 계획 T4 「로그 1줄/재계산」).
    // 형식: WSS mirror <펫> L=<n> B=<b> target=<Primary|Secondary|EmptyHand(no weapon held)|NonWeapon(not mirrored)> +<강화> [특수 이름…] fixed[…] rejected[<이름:사유>…]
    //       ledger(gone=,engine=,removed=,orphan=) added=<n>
    // target 칸은 계획 줄에 없던 것을 더했다: 설계 §4.4 「활성 세트에 무기가 없으면 … 로그(조용히 넘기지 않음)」.
    public static class MirrorLog
    {
        public const string EmptyHandNote = "(no weapon held)";
        public const string NonWeaponNote = "(not mirrored)";

        // nameOf: 계획에 원천 이름이 없는 GUID(고유 속성 Fixed)의 이름 — 게임 쪽은 블루프린트 이름, 모르는 GUID 는 던진다.
        public static string Line(string pet, PlanInput input, RunReport rep, Func<string, string> nameOf)
        {
            if (string.IsNullOrEmpty(pet)) throw new ArgumentException("MirrorLog: pet name is empty");
            if (input == null) throw new ArgumentNullException("input");
            if (rep == null) throw new ArgumentNullException("rep");
            if (rep.Plan == null) throw new ArgumentException("MirrorLog: run report has no plan");
            if (nameOf == null) throw new ArgumentNullException("nameOf");
            if (input.Sources == null) throw new ArgumentException("MirrorLog: input has no sources array");
            var p = rep.Plan;
            var names = new Dictionary<string, string>();
            foreach (var s in input.Sources)
                foreach (var e in s.Enchantments)
                {
                    if (string.IsNullOrEmpty(e.Shape.Name)) throw new ArgumentException("MirrorLog: source enchantment " + e.Shape.Guid + " has no name (the extractor fills it from the blueprint)");
                    if (!names.ContainsKey(e.Shape.Guid)) names[e.Shape.Guid] = e.Shape.Name;
                }
            Func<string, string> special = g =>
            {
                string n;
                if (!names.TryGetValue(g, out n)) throw new InvalidOperationException("MirrorLog: special " + g + " is not among the plan input's source enchantments");
                return n;
            };
            string target = p.TargetKind + (p.TargetKind == TargetKind.EmptyHand ? EmptyHandNote : p.TargetKind == TargetKind.NonWeapon ? NonWeaponNote : "");
            return "WSS mirror " + pet + " L=" + input.ShamanLevel + " B=" + p.Budget + " target=" + target + " +" + p.EnhancementValue
                + " [" + string.Join(", ", p.Specials.Select(special)) + "]"
                + " fixed[" + string.Join(", ", p.Fixed.Select(nameOf)) + "]"
                + " rejected[" + string.Join(", ", p.Rejected.Select(r => r.Name + ":" + r.Reason)) + "]"
                + " ledger(gone=" + rep.Gone + ",engine=" + rep.EngineRemoved + ",removed=" + rep.Removed + ",orphan=" + rep.Orphans + ") added=" + rep.Added;
        }
    }
}
