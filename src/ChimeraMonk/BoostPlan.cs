using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    //  Replace = 비누적 기술자: 한 단계 위 값을 담은 강화 버프(최댓값이 이긴다) · Add = 누적 기술자: 한 단계분 차이값.
    public enum BoostShape { Replace, Add }

    public static class BoostPlan
    {
        public static BoostShape For(bool descriptorStacks) { return descriptorStacks ? BoostShape.Add : BoostShape.Replace; }
    }

    // [순수] AC 합산 모형(계획 T6 `exec: scales stack arithmetic`): 누적 기술자는 모두 더하고, 비누적 기술자는 같은 기술자끼리 최댓값(엔진 ModifiableValue 의 기술자별 최대 규칙).
    public sealed class AcMod
    {
        public readonly string Descriptor;
        public readonly int Value;
        public readonly bool Stacks;
        public AcMod(string descriptor, int value, bool stacks) { Descriptor = descriptor; Value = value; Stacks = stacks; }
    }

    public static class AcModel
    {
        public static int Total(IEnumerable<AcMod> mods)
        {
            if (mods == null) throw new ArgumentNullException("mods");
            var list = mods.ToList();
            if (list.Any(m => m == null || string.IsNullOrEmpty(m.Descriptor))) throw new ArgumentException("ChimeraMonk: AC model modifier without a descriptor", "mods");
            if (list.GroupBy(m => m.Descriptor).Any(g => g.Select(m => m.Stacks).Distinct().Count() > 1)) throw new ArgumentException("ChimeraMonk: AC model descriptor with mixed stacking flags", "mods");
            return list.Where(m => m.Stacks).Sum(m => m.Value) + list.Where(m => !m.Stacks).GroupBy(m => m.Descriptor).Sum(g => g.Max(m => m.Value));
        }
    }
}
