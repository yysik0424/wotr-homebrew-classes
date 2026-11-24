using System;
using System.Linq;

namespace WeaponSpiritShaman
{
    // [순수] 영병 세트 스냅샷 비교(계획 T2 「SetChangeDetector」) — 로그·단언용.
    // SlotItemIds = 8칸: 세트 i 의 주무기 = [2i], 보조 = [2i+1]. 빈 칸 = null. 대상 = 활성 세트 주 → 없으면 보조 → 없으면 맨손(null).
    public sealed class SetSnapshot
    {
        public int Active;
        public string[] SlotItemIds;
    }

    public sealed class SetChange
    {
        public bool ActiveChanged, SourcesChanged, TargetChanged;
        public override string ToString() { return "active=" + ActiveChanged + " sources=" + SourcesChanged + " target=" + TargetChanged; }
    }

    public static class SetChangeDetector
    {
        public const int Slots = AbsorbPlanner.SetCount * 2;

        static void Validate(SetSnapshot s, string name)
        {
            if (s == null) throw new ArgumentNullException(name);
            if (s.SlotItemIds == null || s.SlotItemIds.Length != Slots) throw new ArgumentException("SetChangeDetector: " + name + " must have exactly " + Slots + " slot ids (got " + (s.SlotItemIds == null ? "null" : s.SlotItemIds.Length.ToString()) + ")");
            if (s.Active < 0 || s.Active >= AbsorbPlanner.SetCount) throw new ArgumentOutOfRangeException(name, "SetChangeDetector: active set " + s.Active + " out of range");
        }

        public static string TargetOf(SetSnapshot s)
        {
            Validate(s, "s");
            return s.SlotItemIds[2 * s.Active] ?? s.SlotItemIds[2 * s.Active + 1];
        }

        // 원천 = 활성 아닌 세트의 비어 있지 않은 칸 (칸 번호, id) 열 — 빈 칸은 원천이 아니다.
        static string[] SourcesOf(SetSnapshot s)
        {
            return Enumerable.Range(0, Slots).Where(i => i / 2 != s.Active && s.SlotItemIds[i] != null).Select(i => i + "=" + s.SlotItemIds[i]).ToArray();
        }

        public static SetChange Compare(SetSnapshot prev, SetSnapshot cur)
        {
            Validate(prev, "prev");
            Validate(cur, "cur");
            return new SetChange
            {
                ActiveChanged = prev.Active != cur.Active,
                SourcesChanged = !SourcesOf(prev).SequenceEqual(SourcesOf(cur)),
                TargetChanged = TargetOf(prev) != TargetOf(cur),
            };
        }
    }
}
