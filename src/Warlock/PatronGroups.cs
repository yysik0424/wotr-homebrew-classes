using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock
{
     
    // 게임 쪽(T3 WarlockPactComponent.Run)은 보유 후견인 진행표 GUID·보유 그룹·기록(첫 후견인, 그룹)을 평평하게 넣고 결정만 적용한다.
    public enum Group { Hell, Abyss, Fey, Storm, Death, Star }

    public enum BlastElement { Fire, Acid, Cold, Electricity, NegativeEnergy, Force }

    // 결정 종류. Grant·Keep·RecoverFromRecord 만 그룹을 갖는다(나머지는 변경 없음 — 게임 쪽 로그 등급은 계획 Global Constraints C1 문단).
    public enum PactKind { NoPatron, Grant, Keep, RecoverFromRecord, Ambiguous, Unmapped, RecordMissing, GroupMismatch, FirstPatronMissing, RecordStale }

    public enum NoteKind { SecondSameGroup, SecondOtherGroup, SecondUnmapped }

    // 추가 후견인 하나(결정의 첫 후견인 외). 같은 그룹 = INFO, 다른 그룹·미매핑(어둠의 계약 포함) = WARN(게임 쪽).
    public sealed class Note
    {
        public readonly NoteKind Kind;
        public readonly string Guid;
        public Note(NoteKind kind, string guid) { Kind = kind; Guid = guid; }
    }

    public sealed class PactDecision
    {
        public readonly PactKind Kind;
        public readonly Group? Group; // Grant·Keep·RecoverFromRecord 에서만 값이 있다
        public readonly string[] Guids; // Grant·Keep·Recover = [첫 후견인] · Ambiguous = 매핑된 후견인들 · 그 밖 = 보유 후견인 전부(진단용)
        public readonly Note[] Notes; // Grant·Keep·Recover 공통: 첫 후견인 외 보유 후견인마다 1개(그 밖의 종류는 빈 배열)
        public PactDecision(PactKind kind, Group? group, string[] guids, Note[] notes) { Kind = kind; Group = group; Guids = guids; Notes = notes; }
    }

    public static class PatronGroups
    {
        public static readonly Group[] AllGroups = { Group.Hell, Group.Abyss, Group.Fey, Group.Storm, Group.Death, Group.Star };

        // 설계 §4.3 「그룹 = 블라스트 속성」.
        public static BlastElement ElementOf(Group g)
        {
            switch (g)
            {
                case Group.Hell: return BlastElement.Fire;
                case Group.Abyss: return BlastElement.Acid;
                case Group.Fey: return BlastElement.Cold;
                case Group.Storm: return BlastElement.Electricity;
                case Group.Death: return BlastElement.NegativeEnergy;
                case Group.Star: return BlastElement.Force;
                default: throw new ArgumentOutOfRangeException("g", g, "Warlock: unknown group");
            }
        }

         
        // 독립 앵커 = Checks\ScenarioPlan.PatronNames(시험 `exec: patron map` 이 (GUID, 그룹) 집합 일치를 단언).
        public static readonly Tuple<string, Group>[] Map =
        {
            Tuple.Create(Warlock.Guids.Game.WitchDeceptionPatronProgression, Group.Hell),
            Tuple.Create(Warlock.Guids.Game.WitchDevotionPatronProgression, Group.Hell),
            Tuple.Create(Warlock.Guids.Game.WitchInsanityPatronProgression, Group.Abyss),
            Tuple.Create(Warlock.Guids.Game.WitchStrengthPatronProgression, Group.Abyss),
            Tuple.Create(Warlock.Guids.Game.WitchWinterPatronProgression, Group.Fey),
            Tuple.Create(Warlock.Guids.Game.WitchTransformationhPatronProgression, Group.Fey),
            Tuple.Create(Warlock.Guids.Game.WitchElementsPatronProgression, Group.Storm),
            Tuple.Create(Warlock.Guids.Game.WitchAgilityPatronProgression, Group.Storm),
            Tuple.Create(Warlock.Guids.Game.WitchEndurancePatronProgression, Group.Storm),
            Tuple.Create(Warlock.Guids.Game.WitchShadowPatronProgression, Group.Death),
            Tuple.Create(Warlock.Guids.Game.WitchAncestorsPatronProgression, Group.Death),
            Tuple.Create(Warlock.Guids.Game.WitchHealingPatronProgression, Group.Star),
            Tuple.Create(Warlock.Guids.Game.WitchMercyPatronProgression, Group.Star),
        };

        // 어둠의 계약(다크 시스터 L1 AddFeatures) — 매핑 안 함(설계 §4.3 「도달 불가」 행). TTT 「두 번째 후견인」으로만 올 수 있다(정찰 §7) → SecondUnmapped.
        public const string DarkPact = Warlock.Guids.Game.WitchDarkPactPatronProgression;

        // null = 매핑표에 없음(어둠의 계약·다른 모드 후견인). 입력 null 은 던진다(없는 것과 미매핑을 섞지 않는다).
        public static Group? GroupOf(string patronGuid)
        {
            if (patronGuid == null) throw new ArgumentNullException("patronGuid");
            foreach (var t in Map) if (t.Item1 == patronGuid) return t.Item2;
            return null;
        }

        public static string[] Unmapped(string[] candidates)
        {
            if (candidates == null) throw new ArgumentNullException("candidates");
            return candidates.Where(c => GroupOf(c) == null).ToArray();
        }

        // 첫 후견인(base) 외 보유 후견인마다 Note 1개.
        static Note[] NotesBesides(string[] owned, string first, Group group)
        {
            var notes = new List<Note>();
            foreach (var p in owned)
            {
                if (p == first) continue;
                var g = GroupOf(p);
                notes.Add(new Note(g == null ? NoteKind.SecondUnmapped : g.Value == group ? NoteKind.SecondSameGroup : NoteKind.SecondOtherGroup, p));
            }
            return notes.ToArray();
        }

        static PactDecision Fail(PactKind kind, string[] guids) { return new PactDecision(kind, null, guids, new Note[0]); }

        // 판정 순서(계획 T1 Interfaces, progress.md 사전 점검 T1 행):
        //   그룹 2개 이상 → throw(불변식 위반 — 그룹 특성은 하나만 부여된다)
        //   그룹 1개: 기록 쌍 결측 → RecordMissing · 기록 그룹 ≠ 보유 그룹(또는 기록 후견인의 현재 그룹 ≠ 기록 그룹) → GroupMismatch ·
        //            기록 후견인 미보유 → FirstPatronMissing · 아니면 Keep + Notes
        //   그룹 0개: 기록이 있으면(첫 후견인·그룹 어느 쪽이든) 유효할 때 RecoverFromRecord, 아니면 RecordStale ·
        //            기록 없음: 후견인 0 → NoPatron · 매핑 1 → Grant + Notes · 매핑 2+ → Ambiguous · 매핑 0 → Unmapped
        public static PactDecision Decide(string[] ownedPatrons, Group[] ownedGroups, string recordedFirst, Group? recordedGroup)
        {
            if (ownedPatrons == null) throw new ArgumentNullException("ownedPatrons");
            if (ownedGroups == null) throw new ArgumentNullException("ownedGroups");
            if (ownedPatrons.Any(p => p == null)) throw new ArgumentException("Warlock: ownedPatrons has a null entry", "ownedPatrons");
            if (ownedPatrons.Distinct().Count() != ownedPatrons.Length) throw new ArgumentException("Warlock: ownedPatrons has duplicates (a fact set cannot)", "ownedPatrons");
            if (ownedGroups.Length >= 2) throw new InvalidOperationException("Warlock: unit owns " + ownedGroups.Length + " warlock group features (invariant: at most one)");
            var owned = ownedPatrons.ToArray();

            if (ownedGroups.Length == 1)
            {
                var held = ownedGroups[0];
                if (recordedFirst == null || recordedGroup == null) return Fail(PactKind.RecordMissing, owned);
                if (recordedGroup.Value != held || GroupOf(recordedFirst) != recordedGroup) return Fail(PactKind.GroupMismatch, owned);
                if (!owned.Contains(recordedFirst)) return Fail(PactKind.FirstPatronMissing, owned);
                return new PactDecision(PactKind.Keep, held, new[] { recordedFirst }, NotesBesides(owned, recordedFirst, held));
            }

            if (recordedFirst != null || recordedGroup != null)
            {
                if (recordedFirst != null && recordedGroup != null && owned.Contains(recordedFirst) && GroupOf(recordedFirst) == recordedGroup)
                    return new PactDecision(PactKind.RecoverFromRecord, recordedGroup.Value, new[] { recordedFirst }, NotesBesides(owned, recordedFirst, recordedGroup.Value));
                return Fail(PactKind.RecordStale, owned);
            }

            if (owned.Length == 0) return Fail(PactKind.NoPatron, owned);
            var mapped = owned.Where(p => GroupOf(p) != null).ToArray();
            if (mapped.Length == 1)
            {
                var g = GroupOf(mapped[0]).Value;
                return new PactDecision(PactKind.Grant, g, new[] { mapped[0] }, NotesBesides(owned, mapped[0], g));
            }
            if (mapped.Length >= 2) return Fail(PactKind.Ambiguous, mapped);
            return Fail(PactKind.Unmapped, owned);
        }
    }
}
