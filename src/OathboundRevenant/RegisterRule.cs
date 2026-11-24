using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
     
    // Archetype.Register 가 PaladinClass.m_Archetypes 원본 배열로 Merge 를 불러 새 배열을 얻어 대입하고, 되읽은 배열을 VerifyReadback 에 넣는다.
    public static class RegisterRule
    {
        // 원본 원소(순서·참조 그대로) + ours 를 마지막에 둔 새 배열. 원본 배열은 건드리지 않는다.
        // ours 가 이미 있으면(같은 참조 또는 같은 키) 던진다 — 두 번 등록·다른 경로의 선등록을 조용히 넘기지 않는다.
        // 원본이 null 이거나 null 원소가 있으면 직업이 깨진 것 → 빈 목록으로 덮거나 건너뛰지 않고 던진다(C1).
        public static T[] Merge<T>(T[] original, T ours, Func<T, string> key) where T : class
        {
            if (original == null) throw new ArgumentNullException("original", "RegisterRule.Merge: original archetype array is null (broken class?)");
            if (ours == null) throw new ArgumentNullException("ours");
            if (key == null) throw new ArgumentNullException("key");
            string ourKey = key(ours);
            if (string.IsNullOrEmpty(ourKey)) throw new ArgumentException("RegisterRule.Merge: our key is empty", "ours");
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] == null) throw new InvalidOperationException("RegisterRule.Merge: original element " + i + " is null (broken class?)");
                if (ReferenceEquals(original[i], ours)) throw new InvalidOperationException("RegisterRule.Merge: ours is already element " + i + " (registered twice?)");
                if (key(original[i]) == ourKey) throw new InvalidOperationException("RegisterRule.Merge: element " + i + " already has our key " + ourKey + " (registered twice?)");
            }
            var merged = new T[original.Length + 1];
            Array.Copy(original, merged, original.Length);
            merged[original.Length] = ours;
            return merged;
        }

        // 점검 S22(2026-11-15, 사용자 판정 「우리 변경만 확인」): 등록 직전 배열(before) 대비 지금 배열(now) = before 원소 순서·참조 그대로 + 맨 끝 ours 1개.
        // 본편 원소 수는 보지 않는다(다른 모드가 등록 전에 더한 원소는 before 에 들어 있다 — ExpandedContent DraconicShaman). 문제 목록, 빈 목록 = 일치.
        // before·now·ours 가 없으면 「일치」가 아니라 문제(C1). ours 뒤에 무엇이든 더 있으면 문제(엄격 판정 — 우리 뒤에 더한 모드가 생기면 점검이 시끄럽게 실패한다).
        public static List<string> OnlyOursAppended<T>(T[] before, T[] now, T ours) where T : class
        {
            var p = new List<string>();
            if (before == null) { p.Add("no pre-registration snapshot"); return p; }
            if (now == null) { p.Add("current array is null"); return p; }
            if (ours == null) { p.Add("our element is null"); return p; }
            if (now.Length != before.Length + 1) p.Add("length " + now.Length + ", expected snapshot " + before.Length + " + ours");
            for (int i = 0; i < before.Length; i++)
                if (i >= now.Length || !ReferenceEquals(now[i], before[i])) p.Add("element " + i + " is not the pre-registration element (removed, replaced or reordered)");
            if (now.Length != before.Length + 1 || !ReferenceEquals(now[before.Length], ours)) p.Add("ours is not the single element after the snapshot");
            int seen = now.Count(x => ReferenceEquals(x, ours));
            if (seen != 1) p.Add("ours appears " + seen + " time(s)");
            return p;
        }
        // 대입 직후 되읽기 단언: 새 배열(원본과 다른 객체), 길이 = 원본 + 1, 앞부분 = 원본 원소 순서·참조 그대로, 마지막 = ours. 어긋나면 던진다.
        public static void VerifyReadback<T>(T[] original, T[] back, T ours) where T : class
        {
            if (original == null) throw new ArgumentNullException("original");
            if (ours == null) throw new ArgumentNullException("ours");
            if (back == null) throw new InvalidOperationException("RegisterRule.VerifyReadback: readback array is null");
            if (ReferenceEquals(back, original)) throw new InvalidOperationException("RegisterRule.VerifyReadback: readback is the original array (assignment did not happen)");
            if (back.Length != original.Length + 1)
                throw new InvalidOperationException("RegisterRule.VerifyReadback: readback length " + back.Length + ", expected " + original.Length + " + 1");
            for (int i = 0; i < original.Length; i++)
                if (!ReferenceEquals(back[i], original[i]))
                    throw new InvalidOperationException("RegisterRule.VerifyReadback: element " + i + " is not the original element (order or reference changed)");
            if (!ReferenceEquals(back[original.Length], ours)) throw new InvalidOperationException("RegisterRule.VerifyReadback: last element is not ours");
        }
    }
}
