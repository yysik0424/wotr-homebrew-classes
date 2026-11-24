using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
     
    // TTT-Base 가 동물 동료 진행표 레벨을 랭크 표로 재배치하므로(정찰 r1 §2.3 보충) 레벨이 아니라 GUID 로 찾는다.
    // 각 GUID 가 전체 항목에서 0회 또는 2회 이상이면 throw. 나머지 레벨·항목 순서·특성 순서 보존, 새 배열(입력 불변).
    public sealed class Entry
    {
        public int Level;
        public string[] Features;
        public Entry() { }
        public Entry(int level, params string[] features) { Level = level; Features = features; }
    }

    public sealed class Removed
    {
        public readonly string Guid;
        public readonly int Level;
        public Removed(string guid, int level) { Guid = guid; Level = level; }
        public override string ToString() { return Guid + "@L" + Level; }
    }

    public sealed class EditResult
    {
        public Entry[] Kept;
        public Removed[] RemovedAt; // guids 인자 순서
    }

    public static class ProgressionEdit
    {
        public static EditResult RemoveExactlyOnce(Entry[] entries, string[] guids)
        {
            if (entries == null) throw new ArgumentNullException("entries");
            if (guids == null || guids.Length == 0) throw new ArgumentException("ProgressionEdit: no guids to remove");
            if (guids.Any(string.IsNullOrEmpty) || guids.Distinct().Count() != guids.Length) throw new ArgumentException("ProgressionEdit: guids must be distinct and non-empty");
            foreach (var e in entries)
                if (e == null || e.Features == null) throw new ArgumentException("ProgressionEdit: null entry or feature list");

            var removed = new List<Removed>();
            foreach (var g in guids)
            {
                var hits = entries.SelectMany(e => e.Features.Where(f => f == g).Select(f => e.Level)).ToList();
                if (hits.Count != 1) throw new InvalidOperationException("ProgressionEdit: " + g + " found " + hits.Count + " time(s), expected exactly 1 (levels " + string.Join(",", hits) + ")");
                removed.Add(new Removed(g, hits[0]));
            }
            var drop = new HashSet<string>(guids);
            var kept = entries.Select(e => new Entry { Level = e.Level, Features = e.Features.Where(f => !drop.Contains(f)).ToArray() }).ToArray();
            return new EditResult { Kept = kept, RemovedAt = removed.ToArray() };
        }
    }
}
