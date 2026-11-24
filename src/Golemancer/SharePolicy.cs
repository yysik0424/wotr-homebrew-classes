using System;
using System.Collections.Generic;
using System.Linq;

namespace Golemancer
{
     
    // 게임 쪽(AbilityClone.VerifyShare)이 복제본·원본을 관측해 ShareObservation 을 채우고, 위반이 1개라도 있으면 빌드 실패(BuildFailed)로 던진다.
    //   ① 배열 길이 같음, 교체 1개(DC 형식) 외 전부 원본과 같은 인스턴스
    //   ② 공유 컴포넌트 형식 이름 전부 ∈ 허용 목록(MaterialCatalog SharedComponentKinds, 정찰 §20) — 허용 목록에 있는데 공유되지 않은 형식도 위반(낡은 허용 목록)
    //   ③ 공유 컴포넌트·중첩 원소의 OwnerBlueprint/Owner 가 Build 전 스냅샷과 같음
    //   ④ 원본 배열·원본 DC 직업 무변경
    //   ⑤ 교체 컴포넌트의 OwnerBlueprint == 복제본, 직업 == 펫 직업, 엔진 경로(GetComponent<ContextAbilityParamsCalculator>)가 찾는 계산기 == 교체 컴포넌트
    public sealed class ShareObservation
    {
        public readonly string Name;
        public readonly int SourceLength, CloneLength, ReplacedIndex;
        public readonly bool[] SameAsSource;     // 복제 배열 색인마다 ReferenceEquals(clone[i], src[i])
        public readonly string[] CloneKinds;     // 복제 배열 색인마다 컴포넌트 형식 이름
        public readonly string[] OwnerChanges;   // 스냅샷과 소유자가 다른 노드 설명(공유 컴포넌트·중첩 원소)
        public readonly bool SourceArrayUnchanged, SourceDcClassUnchanged, ReplacedOwnerIsClone, ReplacedClassIsPet, EngineCalculatorIsReplaced;

        public ShareObservation(string name, int sourceLength, int cloneLength, int replacedIndex, bool[] sameAsSource, string[] cloneKinds, string[] ownerChanges,
            bool sourceArrayUnchanged, bool sourceDcClassUnchanged, bool replacedOwnerIsClone, bool replacedClassIsPet, bool engineCalculatorIsReplaced)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("ShareObservation: empty name");
            if (sameAsSource == null || cloneKinds == null || ownerChanges == null) throw new ArgumentNullException("ShareObservation " + name + ": per-index observations missing (unobserved is not 'none')");
            if (sameAsSource.Length != cloneLength || cloneKinds.Length != cloneLength) throw new ArgumentException("ShareObservation " + name + ": per-index arrays (" + sameAsSource.Length + "/" + cloneKinds.Length + ") != clone length " + cloneLength);
            if (cloneKinds.Any(string.IsNullOrEmpty)) throw new ArgumentException("ShareObservation " + name + ": empty component kind");
            Name = name; SourceLength = sourceLength; CloneLength = cloneLength; ReplacedIndex = replacedIndex;
            SameAsSource = (bool[])sameAsSource.Clone(); CloneKinds = (string[])cloneKinds.Clone(); OwnerChanges = (string[])ownerChanges.Clone();
            SourceArrayUnchanged = sourceArrayUnchanged; SourceDcClassUnchanged = sourceDcClassUnchanged; ReplacedOwnerIsClone = replacedOwnerIsClone; ReplacedClassIsPet = replacedClassIsPet; EngineCalculatorIsReplaced = engineCalculatorIsReplaced;
        }
    }

    public static class SharePolicy
    {
        // 위반 목록(빈 배열 = 통과). 허용 목록이 null·빈 배열이면 판정 불가 → throw(「공유 0개 허용」이라는 조용한 기본값 금지 — 원장 판정 T2→T4 (a)).
        public static string[] Violations(ShareObservation o, string dcKind, string[] allowedKinds)
        {
            return Violations(o, dcKind, allowedKinds, null, null);
        }

        // 점검용(2026-11-17 동굴 점검 S11·S16): 다른 모드가 복제본의 공유 컴포넌트를 자기 사본으로 갈아 끼운 것을 「우리 위반」 과 갈라 센다.
        // foreignCopiedBy = 복제 배열 색인마다 「그 칸을 바꾼 것으로 식별한 모드」(null = 식별 못 함 / 바뀌지 않음). null 배열 = 다른 모드 분리 없음(빌드 검사 — 엄격).
        // 식별된 칸: 위반 대신 foreignNotes 에 한 줄(INFO), 그 형식은 「공유 관측」 대신 「다른 모드가 사본으로 바꿈」 으로 허용 목록을 채운다(낡은 허용 목록 판정에서 빠지지 않게).
        // 식별 못 한 칸은 그대로 「is not the source instance」 위반(FAIL 유지). 모순(바뀌지 않은 칸·교체 칸에 식별 표시)은 던진다.
        public static string[] Violations(ShareObservation o, string dcKind, string[] allowedKinds, string[] foreignCopiedBy, List<string> foreignNotes, int ownedAreaIndex = -1)
        {
            if (o == null) throw new ArgumentNullException("o");
            if (foreignCopiedBy != null)
            {
                if (foreignCopiedBy.Length != o.CloneLength) throw new ArgumentException("SharePolicy " + o.Name + ": foreign-copy marks (" + foreignCopiedBy.Length + ") != clone length " + o.CloneLength);
                if (foreignNotes == null) throw new ArgumentNullException("foreignNotes", "SharePolicy " + o.Name + ": foreign copies are separated but there is no list to report them in");
                for (int i = 0; i < foreignCopiedBy.Length; i++)
                {
                    if (foreignCopiedBy[i] == null) continue;
                    if (foreignCopiedBy[i].Length == 0) throw new ArgumentException("SharePolicy " + o.Name + ": empty foreign mod name at component " + i);
                    if (i == o.ReplacedIndex || o.SameAsSource[i]) throw new ArgumentException("SharePolicy " + o.Name + ": component " + i + " is marked as copied by " + foreignCopiedBy[i] + " but it is " + (i == o.ReplacedIndex ? "our replaced DC component" : "still the source instance") + " (contradictory)");
                }
            }
            if (string.IsNullOrEmpty(dcKind)) throw new ArgumentException("SharePolicy: DC component kind missing");
            if (allowedKinds == null || allowedKinds.Length == 0) throw new ArgumentException("SharePolicy " + o.Name + ": allowed shared kinds are null/empty (T4 Step 0 must fill MaterialCatalog SharedComponentKinds)");
            if (allowedKinds.Any(string.IsNullOrEmpty) || allowedKinds.Contains(dcKind)) throw new ArgumentException("SharePolicy " + o.Name + ": allowed kinds contain an empty name or the DC kind itself");
            var v = new List<string>();
            // ①
            if (o.CloneLength != o.SourceLength) v.Add(o.Name + ": component count " + o.CloneLength + " != source " + o.SourceLength);
            bool idxOk = o.ReplacedIndex >= 0 && o.ReplacedIndex < o.CloneLength;
            if (!idxOk) v.Add(o.Name + ": replaced index " + o.ReplacedIndex + " outside 0.." + (o.CloneLength - 1));
            else
            {
                if (o.SameAsSource[o.ReplacedIndex]) v.Add(o.Name + ": component " + o.ReplacedIndex + " (" + o.CloneKinds[o.ReplacedIndex] + ") was not replaced");
                if (o.CloneKinds[o.ReplacedIndex] != dcKind) v.Add(o.Name + ": replaced component is " + o.CloneKinds[o.ReplacedIndex] + ", expected " + dcKind);
            }
            var shared = new SortedSet<string>(StringComparer.Ordinal);
            var foreignKinds = new SortedSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < o.CloneLength; i++)
            {
                if (i == o.ReplacedIndex) continue;
                if (i == ownedAreaIndex)
                {
                    if (o.SameAsSource[i] || o.CloneKinds[i] != "AddAreaEffect") v.Add(o.Name + ": incorrect owned area component at " + i);
                    shared.Add(o.CloneKinds[i]);
                    continue;
                }
                if (o.SameAsSource[i]) { shared.Add(o.CloneKinds[i]); continue; }
                string by = foreignCopiedBy == null ? null : foreignCopiedBy[i];
                if (by == null) { v.Add(o.Name + ": component " + i + " (" + o.CloneKinds[i] + ") is not the source instance"); continue; }
                foreignKinds.Add(o.CloneKinds[i]);
                foreignNotes.Add(o.Name + ": component " + i + " (" + o.CloneKinds[i] + ") was replaced by its own copy by " + by + " (another mod's change, counted separately)");
            }
            // ②
            foreach (var k in shared.Concat(foreignKinds).Distinct()) if (!allowedKinds.Contains(k)) v.Add(o.Name + ": shared kind " + k + " is not allowed");
            foreach (var k in allowedKinds) if (!shared.Contains(k) && !foreignKinds.Contains(k)) v.Add(o.Name + ": allowed kind " + k + " was not observed shared (stale allowance)");
            // ③
            foreach (var c in o.OwnerChanges) v.Add(o.Name + ": owner changed since the Build snapshot: " + c);
            // ④
            if (!o.SourceArrayUnchanged) v.Add(o.Name + ": source components array changed");
            if (!o.SourceDcClassUnchanged) v.Add(o.Name + ": source DC class changed");
            // ⑤
            if (!o.ReplacedOwnerIsClone) v.Add(o.Name + ": replaced component's OwnerBlueprint is not the clone");
            if (!o.ReplacedClassIsPet) v.Add(o.Name + ": replaced component's class is not the pet class");
            if (!o.EngineCalculatorIsReplaced) v.Add(o.Name + ": the engine's GetComponent<ContextAbilityParamsCalculator> finds another calculator than the replaced one");
            return v.ToArray();
        }
    }
}
