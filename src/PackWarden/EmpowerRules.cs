using System;
using System.Collections.Generic;

namespace PackWarden
{
    // 「무리 강화」(소유자 결정 2026-11-16) 의 규칙·상수·플레이어 문구. 게임 형식 의존 없음 — 시험 exe 가 직접 실행한다.
    //   헌터 4·10·16레벨에 무리 강화 능력을 1개씩 고른다(중복 불가). 고를 수 있는 범위 = 4레벨: 헌터 주문 1~2레벨, 10레벨: 3레벨까지, 16레벨: 5레벨까지.
    //   고른 능력마다 하루 2회, 주문 유사 능력, 이동 행동. 원래 단일 대상 주문의 효과를 주 동물 + 살아 있는 보조 전체에 건다(시전자 레벨 = 헌터 레벨).
    public static class EmpowerRules
    {
        // 소유자 결정 2026-11-16: 선택을 받는 헌터 레벨.
        public static readonly int[] SelectionLevels = { 4, 10, 16 };
        // 소유자 결정 2026-11-16: 그 선택에서 고를 수 있는 헌터 주문 레벨 상한(SelectionLevels 와 같은 순서).
        static readonly int[] MaxSpellLevelAtSelection = { 2, 3, 5 };
        // 소유자 결정 2026-11-16: 고른 능력마다 하루 사용 횟수.
        public const int UsesPerDay = 2;
        // 소유자 결정 2026-11-16(정정): 무리 강화 능력 10개 전부 이동 행동. 값 = Kingmaker UnitCommand.CommandType 의 이름
        // (본편 선례 Classes/Ranger/HuntersBondAbility.jbp · Units/Abilities/Mounts/MountTargetAbility.jbp "ActionType": "Move").
        public const string ActionTypeName = "Move";
        // 능력 형식 표기 = 주문 유사 능력(Kingmaker AbilityType 의 이름).
        public const string AbilityTypeName = "SpellLike";

        public sealed class Candidate
        {
            public readonly string Key;              // 블루프린트 이름·문자열 키에 쓰는 식별자(영문)
            public readonly string SpellGuid;        // 원 주문(헌터 주문 목록에 있는 것)
            public readonly int HunterSpellLevel;    // 헌터 주문 목록 기준 레벨(Classes/Hunter/HunterSpelllist.jbp)
            public readonly string ChainGuid;        // 효과 사슬을 가진 능력: 원 주문 자신, 접촉 전달형이면 전달 능력(AbilityEffectStickyTouch.m_TouchDeliveryAbility)
            public readonly string AbilityGuid, FeatureGuid, ResourceGuid; // 우리가 만드는 블루프린트(발급 2026-11-16, 영구 고정)
            // NameEn = 본편 영문 주문명. NameKo = 게임 한글 패치(WrathKoreanMod Resources\translation.json, 2026-11-17 대조)의 그 주문 이름 —
            // 본편 데이터에는 한글 주문명이 없어 사본을 둔다(패치가 표기를 바꾸면 이 표를 맞춘다; 2026-11-17 에 5개를 패치 표기로 고침).
            public readonly string NameKo, NameEn;
            public Candidate(string key, string spellGuid, int level, string chainGuid, string abilityGuid, string featureGuid, string resourceGuid, string nameKo, string nameEn)
            { Key = key; SpellGuid = spellGuid; HunterSpellLevel = level; ChainGuid = chainGuid; AbilityGuid = abilityGuid; FeatureGuid = featureGuid; ResourceGuid = resourceGuid; NameKo = nameKo; NameEn = nameEn; }
        }

        public const string HunterSpellList = "d090b791bfe381740b98ed4ff909b1cf"; // Classes/Hunter/HunterSpelllist.jbp
        public const string SelectionGuid = "282d3a77877c4101884d1d72edf681f6";

        // 소유자 결정 2026-11-16: 후보 10개(Animal Growth 는 보조의 소형 고정을 깨므로 제외). 주문 GUID·레벨·전달 능력 = blueprints.zip 2026-11-16 직독(시험이 대조).
         
        public static readonly Candidate[] Candidates =
        {
            new Candidate("MagicFang", "403cf599412299a4f9d5d925c7b9fb33", 1, "403cf599412299a4f9d5d925c7b9fb33", "5447526b70614b2db1270cbd5e34bec0", "7b2863ea0e334a20a882b1625863ac98", "6ef61fbe8ada465995fd163d59633a0b", "마법 송곳니", "Magic Fang"),
            new Candidate("BullsStrength", "4c3d08935262b6544ae97599b3a9556d", 2, "4c3d08935262b6544ae97599b3a9556d", "9154fa6d38734d3d8e6ea66e629932ff", "cd9656461f9e49f186642dfe2903b6c0", "7aa088304b0c479aae7607638117a6ac", "황소의 힘", "Bull's Strength"),
            new Candidate("CatsGrace", "de7a025d48ad5da4991e7d3c682cf69d", 2, "de7a025d48ad5da4991e7d3c682cf69d", "ef4275415d194f1885827f3643936573", "04bd579091d340698dbb8281244f8c42", "6ec554daa0784f09a56e6290a7efb4d8", "고양이의 우아함", "Cat's Grace"),
            new Candidate("BearsEndurance", "a900628aea19aa74aad0ece0e65d091a", 2, "a900628aea19aa74aad0ece0e65d091a", "5b3e7918efc242d2a76c1eb0fef895d7", "13658bfecc4c461988c7e7ecc7ddac3c", "7350dbbf53d04db89a8c3b5b80da491b", "곰의 지구력", "Bear's Endurance"),
            new Candidate("Barkskin", "5b77d7cc65b8ab74688e74a37fc2f553", 2, "5b77d7cc65b8ab74688e74a37fc2f553", "69ccfd073f754c7588b01c100061a148", "aec75e06ff534729bff4ae30a0ad5d70", "78eac929872044e1aa1f7c9071db6040", "나무껍질", "Barkskin"),
            new Candidate("NaturalRhythm", "dde924776bd7ad247928b5efbfacfbdd", 2, "dde924776bd7ad247928b5efbfacfbdd", "de85e9959d2243428434c7dd741965f2", "944cda4b2a76461db6cc1c6ef2ddfdc5", "ddabd7d2c4cb431ebe596835256644e0", "자연의 리듬", "Natural Rhythm"),
            new Candidate("MagicFangGreater", "f1100650705a69c4384d3edd88ba0f52", 3, "f1100650705a69c4384d3edd88ba0f52", "c15a9d50a0be4f5e8644c735755585a5", "9dc01c7ba014419c8775f5726735c4b5", "618844b4359a48e2bfce344e81848e0b", "마법 송곳니, 상급", "Magic Fang, Greater"),
            // 접촉 전달형: 사슬은 전달 능력(Spells/Level4/FreedomOfMovement.jbp · DeathWard.jbp)에 있다.
            new Candidate("FreedomOfMovement", "0087fc2d64b6095478bc7b8d7d512caf", 4, "4c349361d720e844e846ad8c19959b1e", "ca378d14203045388d08facdbeee756f", "56c2675695854d3ca2562bad0a09f071", "64ce61e25f86488695b0c2631d88e59d", "이동의 자유", "Freedom of Movement"),
            new Candidate("BlessingOfTheSalamander", "9256a86aec14ad14e9497f6b60e26f3f", 4, "9256a86aec14ad14e9497f6b60e26f3f", "cb66f53b288b43b28c30db6d9c741403", "2fcc6bca860b4035beec074764510a42", "173872ffc0954e89aef83e23f80dfefe", "샐러맨더의 축복", "Blessing of the Salamander"),
            new Candidate("DeathWard", "e9cc9378fd6841f48ad59384e79e9953", 5, "0413915f355a38146bc6ad40cdf27b3f", "3de371736c114402abba8f9b25f8f468", "2ffe3c9043cf42aeaecfd7e931f21663", "7ec566e52ced4e82b4b81da5cc9f077d", "죽음 방호", "Death Ward"),
        };

        static EmpowerRules()
        {
            if (SelectionLevels.Length != MaxSpellLevelAtSelection.Length)
                throw new InvalidOperationException("PackWarden: empower selection tables differ in length");
            for (int i = 1; i < SelectionLevels.Length; i++)
                if (SelectionLevels[i] <= SelectionLevels[i - 1] || MaxSpellLevelAtSelection[i] <= MaxSpellLevelAtSelection[i - 1])
                    throw new InvalidOperationException("PackWarden: empower selection tables must be ascending");
            CheckTextPremises();
        }

        // 헌터 주문 레벨 → 그 능력을 고를 수 있는 최소 헌터 레벨(전제조건). 범위 밖 주문 레벨(1 미만·표의 상한 초과)은 던진다.
        public static int MinHunterLevelFor(int hunterSpellLevel)
        {
            int top = MaxSpellLevelAtSelection[MaxSpellLevelAtSelection.Length - 1];
            if (hunterSpellLevel < 1 || hunterSpellLevel > top)
                throw new ArgumentOutOfRangeException("hunterSpellLevel", hunterSpellLevel, "PackWarden: hunter spell level must be 1.." + top);
            for (int i = 0; i < SelectionLevels.Length; i++)
                if (hunterSpellLevel <= MaxSpellLevelAtSelection[i]) return SelectionLevels[i];
            throw new InvalidOperationException("PackWarden: no selection level for spell level " + hunterSpellLevel);
        }





        // 원 주문의 사슬 능력에서 우리 능력으로 **복사하는** 컴포넌트 형식(이름). 그 밖은 복사하지 않는다.
        //   ContextRankConfig · ContextCalculateSharedValue = 사슬의 액션이 읽는 랭크·공유값(지속 시간, Magic Fang Greater 의 버프 분기).
        // 능력 단계의 SpellDescriptorComponent 는 일부러 복사하지 않는다: 본편 MountedBuff(Units/Abilities/Mounts/MountedBuff.jbp)는 기수가 변신 서술자 능력을 쓰면
        // 자신을 제거한다(= 하차 → 무리 해산). Blessing of the Salamander 주문 능력에 그 서술자가 있다. 버프 자체의 서술자는 버프 블루프린트에 그대로 남는다.
        public static readonly string[] ClonedChainComponentTypes = { "ContextRankConfig", "ContextCalculateSharedValue" };

        // ---- 대상(주 동물 + 살아 있는 보조). 사거리 제한 없음. ----
        public enum TargetState { Unknown = 0, HasTargets, NoTargets, Unreadable }
        // memberCount = 살아 있는 무리 구성원 수, null = 읽을 수 없음. 「대상 없음」(0)과 「읽을 수 없음」(null)은 다른 상태다.
        public static TargetState ClassifyTargets(int? memberCount)
        {
            if (memberCount == null) return TargetState.Unreadable;
            if (memberCount.Value < 0) throw new ArgumentOutOfRangeException("memberCount", memberCount, "PackWarden: member count cannot be negative");
            return memberCount.Value == 0 ? TargetState.NoTargets : TargetState.HasTargets;
        }

        // ---- 시전 뒤 되읽기(구성원마다 기대 버프 보유) ----
        // ForeignCaster = 기대 버프가 있기는 한데 다른 시전자의 것이다(우리 것은 적용되지 않았다) — ClassifyMemberReadback 만 낸다.
        // NotRefreshed = 기대 버프는 우리 것이 있는데 **시전 전과 같은 인스턴스**다 — 이번 시전이 붙였거나 갱신했다고 입증할 수 없다(검토 2026-11-17 지적 3). ClassifyMemberReadback 만 낸다.
         
        public enum BuffReadback { Unknown = 0, Present, Absent, Unreadable, ForeignCaster, NotRefreshed, Undetermined }
        // expectedCount = 이 능력이 그 시전자 레벨에서 걸었어야 하는 버프 수, presentCount = 그중 구성원이 실제로 가진 수. null = 읽을 수 없음.
        // 기대 버프 0개는 「붙음」 이 아니다: 붙음 / 안 붙음을 가를 근거가 없으므로 판정 불가(Unreadable)로 보고된다. 전부 있어야 Present.
        public static BuffReadback ClassifyBuffReadback(int? expectedCount, int? presentCount)
        {
            if (expectedCount == null || presentCount == null) return BuffReadback.Unreadable;
            if (expectedCount.Value < 0) throw new ArgumentOutOfRangeException("expectedCount", expectedCount, "PackWarden: expected buff count cannot be negative");
            if (presentCount.Value < 0 || presentCount.Value > expectedCount.Value) throw new ArgumentOutOfRangeException("presentCount", presentCount, "PackWarden: present buff count must be 0.." + expectedCount.Value);
            if (expectedCount.Value == 0) return BuffReadback.Unreadable;
            return presentCount.Value == expectedCount.Value ? BuffReadback.Present : BuffReadback.Absent;
        }

        public static bool IsReadbackProblem(BuffReadback r)
        {
            switch (r)
            {
                case BuffReadback.Present:
                case BuffReadback.Undetermined: return false; // 판정 불가는 실패가 아니다 — 따로 UNDETERMINED 줄(UndeterminedLogHead)로 보고한다
                case BuffReadback.Absent:
                case BuffReadback.ForeignCaster:
                case BuffReadback.NotRefreshed:
                case BuffReadback.Unreadable: return true;
                default: throw new ArgumentOutOfRangeException("r", r, "PackWarden: unclassified buff readback");
            }
        }

        // ---- 붙어 있는 버프의 시전자(독립 검토 2차 2026-11-16 지적 4) ----
        // 되읽기는 구성원의 버프를 블루프린트로 찾는다(BuffCollection.GetBuff — 시전자를 가리지 않는다). 엔진 BuffCollection.PrepareFactForAttach 는
        // 규칙 거절(RuleCanApplyBuff.CanApply 거짓, IL @0061~006A → null)을 **중첩 처리(Stacking) 앞에서** 돌려준다 → 다른 시전자가 먼저 건 같은 버프가 있으면,
        // 우리 것이 거절돼도 그 버프가 남아 「붙음」 으로 읽힌다. 거절되지 않으면 Stacking = Replace(@014A~0158: 기존 버프 제거 뒤 새 버프 부착)라 남는 버프의 시전자는 우리(무리 주인)다.
        // 후보가 직접 거는 버프가 전부 Replace 이고 부모 컨텍스트를 물려받는다는 것(IgnoreParentContext 거짓)은 시험이 게임 데이터로 잠근다. Assembly-CSharp 2026-11-16 직독.
        public enum BuffCaster { Unknown = 0, Ours, Foreign, Unreadable }
        // readable = 그 버프의 컨텍스트·시전자를 읽었는가. caster = 읽은 시전자(null = 시전자를 알 수 없음 — 「우리 것」 으로도 「남의 것」 으로도 읽지 않는다). 비교는 참조 동일성.
        public static BuffCaster ClassifyBuffCaster(bool readable, object caster, object leader)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            if (!readable || caster == null) return BuffCaster.Unreadable;
            return ReferenceEquals(caster, leader) ? BuffCaster.Ours : BuffCaster.Foreign;
        }
        // 수치 대조는 우리가 건 버프에만 한다: 남의 버프·시전자를 모르는 버프의 지속을 우리 기대값과 비교하면 거짓 불일치가 난다(그 구성원은 ForeignCaster / Unreadable 로 따로 보고된다).
        public static bool ShouldCheckValues(BuffCaster c)
        {
            switch (c)
            {
                case BuffCaster.Ours: return true;
                case BuffCaster.Foreign:
                case BuffCaster.Unreadable: return false;
                default: throw new ArgumentOutOfRangeException("c", c, "PackWarden: unclassified buff caster");
            }
        }
         
        // 엔진 BuffCollection.PrepareFactForAttach(Assembly-CSharp 2026-11-17 직독):
        //   거절(RuleCanApplyBuff.CanApply 거짓 @0061~006A) → null — 기존 버프를 **그대로 둔다**(중첩 처리 앞).
        //   Stacking = Stack(3) @00FE~0105 beq @0270 → 기존을 보지 않고 **항상 새 인스턴스**. 그 밖은 GetBuff(@010C — 같은 블루프린트의 첫 인스턴스)가 없으면 새 인스턴스(@0115 → @0270).
        //   있으면 switch @0124: Replace(0) @014A~0158 기존 제거 → 새 인스턴스 · Prolong(1) @015D~01A8 기존의 종료가 새 종료보다 이르면 SetEndTime(새 종료), 새 지속이 영구면 MakePermanent → **같은 인스턴스** ·
        //   Ignore(2) @01A9 그대로 같은 인스턴스 · Poison(4) @01AC~023B 반 지속 IncreaseDuration 또는 MakePermanent → 같은 인스턴스 · Summ(5) @023C~025E 지속 IncreaseDuration 또는 MakePermanent → 같은 인스턴스 ·
        //   Rank(6) @025F~0269 AddRank(1) → 같은 인스턴스 · 그 밖 @026A ArgumentOutOfRangeException.
        // 그래서 시전 직전 그 블루프린트의 **모든 인스턴스**(참조 · 영구 여부 · 종료 시각 · 랭크)를 기록하고(PackEmpower.Snapshot), 시전 뒤 모든 인스턴스와 대조한다:
        //   새 인스턴스(시전 전에 없던 참조) 또는 같은 인스턴스의 연장(영구가 됨 · 종료 시각이 늦어짐 · 랭크가 오름) = 이번 시전의 부여가 **관측됐다**.
        //   관측된 변화가 없으면: 새 인스턴스를 만드는 중첩(Replace · Stack)에서는 거절이다(거절되지 않았으면 새 인스턴스가 있었다). 기존을 고치는 중첩(Prolong · Ignore · Poison · Summ · Rank)에서는
        //   거절과 「바꿀 것이 없었음」(Prolong 의 기존 종료가 더 늦음 · Ignore)이 같은 모습이다 → 판정 불가(UNDETERMINED — 실패로 보고하지 않는다).
        // 종료 시각 연장(Enduring Spells 의 부여 뒤 SetEndTime 등)은 새 인스턴스에도 일어나지만 「새 인스턴스」 판정을 바꾸지 않는다(참조로 가른다).
        public enum StackingKind { Unknown = 0, NewInstance, InPlace }
        public static StackingKind StackingKindOf(string stacking)
        {
            switch (stacking)
            {
                case "Replace": case "Stack": return StackingKind.NewInstance;
                case "Prolong": case "Ignore": case "Poison": case "Summ": case "Rank": return StackingKind.InPlace;
                default: throw new ArgumentOutOfRangeException("stacking", stacking, "PackWarden: unknown Stacking (the engine throws on it too — PrepareFactForAttach @026A)");
            }
        }
        // 버프 인스턴스 하나의 관측(시전 전·뒤 공통). Instance = 참조 비교용(Equals 는 쓰지 않는다), Permanent 면 EndTicks 는 보지 않는다.
        public sealed class BuffState
        {
            public readonly object Instance; public readonly bool Permanent; public readonly long EndTicks; public readonly int Rank;
            public BuffState(object instance, bool permanent, long endTicks, int rank)
            {
                if (instance == null) throw new ArgumentNullException("instance");
                if (rank < 0) throw new ArgumentOutOfRangeException("rank", rank, "PackWarden: buff rank cannot be negative");
                Instance = instance; Permanent = permanent; EndTicks = endTicks; Rank = rank;
            }
        }
        public enum InstanceChange { Unknown = 0, New, Extended, Unchanged }
        // 시전 뒤 인스턴스 하나 ↔ 시전 전 기록. 시전 전에 없던 참조 = New, 같은 참조가 영구가 됨 · 종료가 늦어짐 · 랭크가 오름 = Extended, 그 밖 = Unchanged.
        public static InstanceChange ClassifyInstanceChange(IList<BuffState> before, BuffState after)
        {
            if (before == null) throw new ArgumentNullException("before");
            if (after == null) throw new ArgumentNullException("after");
            BuffState was = null;
            foreach (var b in before)
            {
                if (b == null) throw new ArgumentException("PackWarden: null entry in the before-casting buff states", "before");
                if (ReferenceEquals(b.Instance, after.Instance)) { if (was != null) throw new ArgumentException("PackWarden: the same buff instance is recorded twice before the casting", "before"); was = b; }
            }
            if (was == null) return InstanceChange.New;
            if (after.Permanent && !was.Permanent) return InstanceChange.Extended;
            if (!after.Permanent && !was.Permanent && after.EndTicks > was.EndTicks) return InstanceChange.Extended;
            if (after.Rank > was.Rank) return InstanceChange.Extended;
            return InstanceChange.Unchanged;
        }
        // 기대 버프 하나(블루프린트 하나)의 입증 결과.
        //   AppliedOurs = 무리 주인의 인스턴스에서 이번 시전의 변화가 관측됐다 · AppliedForeign = 변화는 남의 인스턴스에만(Prolong 등이 남의 버프를 늘렸다 — 수치는 그 시전자의 것) ·
        //   RefusedForeignPresent / RefusedOursPresent = 새 인스턴스를 만드는 중첩인데 변화가 없다(거절) — 남은 것이 남의 것 / 우리 옛 것 ·
        //   NoObservableChange = 기존을 고치는 중첩인데 변화가 없다(판정 불가) · Absent = 그 블루프린트의 인스턴스가 없다 · Unreadable = 시전 전 상태나 관련 인스턴스의 시전자를 읽지 못함.
        public enum BuffProof { Unknown = 0, AppliedOurs, AppliedForeign, RefusedForeignPresent, RefusedOursPresent, NoObservableChange, Absent, Unreadable }
        // beforeReadable = 시전 전 상태를 읽었는가. after · afterCasters = 시전 뒤 그 블루프린트의 인스턴스와 각각의 시전자(같은 길이). ourNewIndex = 수치 대조 대상(우리 새 인스턴스, 없으면 −1).
        public static BuffProof ProveApplication(bool beforeReadable, IList<BuffState> before, IList<BuffState> after, IList<BuffCaster> afterCasters, StackingKind stacking, out int ourNewIndex)
        {
            ourNewIndex = -1;
            if (after == null) throw new ArgumentNullException("after");
            if (afterCasters == null) throw new ArgumentNullException("afterCasters");
            if (after.Count != afterCasters.Count) throw new ArgumentException("PackWarden: " + after.Count + " buff instances but " + afterCasters.Count + " caster verdicts", "afterCasters");
            if (stacking != StackingKind.NewInstance && stacking != StackingKind.InPlace) throw new ArgumentOutOfRangeException("stacking", stacking, "PackWarden: unclassified stacking kind");
            foreach (var c in afterCasters) if (c != BuffCaster.Ours && c != BuffCaster.Foreign && c != BuffCaster.Unreadable) throw new ArgumentOutOfRangeException("afterCasters", c, "PackWarden: unclassified buff caster");
            if (!beforeReadable) return BuffProof.Unreadable;
            if (before == null) throw new ArgumentNullException("before", "PackWarden: a readable snapshot must carry its states");
            if (after.Count == 0) return BuffProof.Absent;
            bool oursChanged = false, foreignChanged = false, unknownChanged = false, oursSame = false, foreignSame = false, unknownSame = false;
            for (int i = 0; i < after.Count; i++)
            {
                var ch = ClassifyInstanceChange(before, after[i]);
                bool changed = ch == InstanceChange.New || ch == InstanceChange.Extended;
                switch (afterCasters[i])
                {
                    case BuffCaster.Ours:
                        if (changed) { oursChanged = true; if (ch == InstanceChange.New && ourNewIndex < 0) ourNewIndex = i; } else oursSame = true;
                        break;
                    case BuffCaster.Foreign: if (changed) foreignChanged = true; else foreignSame = true; break;
                    default: if (changed) unknownChanged = true; else unknownSame = true; break;
                }
            }
            if (oursChanged) return BuffProof.AppliedOurs;
            if (unknownChanged) { ourNewIndex = -1; return BuffProof.Unreadable; } // 변화가 있는데 누구 것인지 모른다
            if (foreignChanged) return BuffProof.AppliedForeign;
            if (unknownSame) return BuffProof.Unreadable;
            if (stacking == StackingKind.InPlace) return BuffProof.NoObservableChange;
            if (foreignSame) return BuffProof.RefusedForeignPresent;
            if (oursSame) return BuffProof.RefusedOursPresent;
            throw new InvalidOperationException("PackWarden: ProveApplication reached no verdict"); // after 가 비지 않았으므로 위 갈래 중 하나다
        }
        // 한 구성원의 최종 판정 = 기대 버프마다의 입증(같은 순서). 기대 버프 0개는 판정 불가(붙음으로 읽지 않는다).
        // 우선순위(예전 판정과 같은 순서에 판정 불가 갈래를 더함): Unreadable > ForeignCaster(남의 버프만 남고 우리 것은 거절) > Absent > NotRefreshed(우리 옛 인스턴스만 남음) > Undetermined > Present.
        public static BuffReadback ClassifyMemberProof(IList<BuffProof> proofs)
        {
            if (proofs == null) throw new ArgumentNullException("proofs");
            if (proofs.Count == 0) return BuffReadback.Unreadable;
            bool unreadable = false, foreign = false, absent = false, stale = false, undetermined = false;
            foreach (var p in proofs)
            {
                switch (p)
                {
                    case BuffProof.AppliedOurs: break;
                    case BuffProof.AppliedForeign:
                    case BuffProof.NoObservableChange: undetermined = true; break;
                    case BuffProof.RefusedForeignPresent: foreign = true; break;
                    case BuffProof.RefusedOursPresent: stale = true; break;
                    case BuffProof.Absent: absent = true; break;
                    case BuffProof.Unreadable: unreadable = true; break;
                    default: throw new ArgumentOutOfRangeException("proofs", p, "PackWarden: unclassified buff proof");
                }
            }
            if (unreadable) return BuffReadback.Unreadable;
            if (foreign) return BuffReadback.ForeignCaster;
            if (absent) return BuffReadback.Absent;
            if (stale) return BuffReadback.NotRefreshed;
            if (undetermined) return BuffReadback.Undetermined;
            return BuffReadback.Present;
        }
        // 시전자만 본 판정(부여 입증 이전 단계 — 게임 쪽은 ClassifyMemberProof 만 부른다, 시험이 IL 로 잠근다).
        public static BuffReadback ClassifyMemberReadback(BuffReadback presence, IEnumerable<BuffCaster> presentBuffCasters)
        {
            if (presentBuffCasters == null) throw new ArgumentNullException("presentBuffCasters");
            if (presence == BuffReadback.Unknown || presence == BuffReadback.ForeignCaster || presence == BuffReadback.NotRefreshed || presence == BuffReadback.Undetermined) throw new ArgumentOutOfRangeException("presence", presence, "PackWarden: presence must be Present, Absent or Unreadable");
            bool foreign = false, unreadable = false;
            foreach (var c in presentBuffCasters)
            {
                switch (c)
                {
                    case BuffCaster.Ours: break;
                    case BuffCaster.Foreign: foreign = true; break;
                    case BuffCaster.Unreadable: unreadable = true; break;
                    default: throw new ArgumentOutOfRangeException("presentBuffCasters", c, "PackWarden: unclassified buff caster");
                }
            }
            if (presence == BuffReadback.Unreadable || unreadable) return BuffReadback.Unreadable;
            return foreign ? BuffReadback.ForeignCaster : presence;
        }

        // ---- 1회 보고 키 · 시전 요약(독립 검토 2차 2026-11-16 지적 2) ----
        // 「같은 문제는 1회」 의 「같은 문제」 = 종류 · 능력 · 대상 · **결과(상태 이름·원인)** · **예외 형식**. 결과·예외 형식이 다르면 다른 문제다(예: Unreadable 뒤의 Mismatch, member died 뒤의 unknown).
        // kind·result 는 호출부의 상수·열거 이름이라 없으면 던진다. ability·subject 는 런타임 값이라 null 을 표식으로 바꿔 싣는다(보고 경로에서 던지지 않는다). exceptionType = 예외 보고가 아니면 null.
        public const char OnceKeySeparator = '\u0001';
        public static string OnceKey(string kind, string ability, string subject, string result, string exceptionType)
        {
            if (string.IsNullOrEmpty(kind)) throw new ArgumentException("PackWarden: once-key without a kind", "kind");
            if (string.IsNullOrEmpty(result)) throw new ArgumentException("PackWarden: once-key without a result", "result");
            return kind + OnceKeySeparator + (ability ?? "<null ability>") + OnceKeySeparator + (subject ?? "<null subject>") + OnceKeySeparator + result + OnceKeySeparator + (exceptionType ?? "");
        }
        // 시전마다 요약 1줄: 그 시전에서 센 문제 수가 0 보다 크면(새로 찍힌 줄이 있든, 전부 1회 정책으로 억제됐든) 남긴다 — 억제된 반복이 있었음을 알 수 있게. 0 이면 줄 없음(null).
        public const string SummaryLogHead = "PackWarden: pack empowerment summary ";
        public static string SummaryLine(string ability, int problems)
        {
            if (problems < 0) throw new ArgumentOutOfRangeException("problems", problems, "PackWarden: problem count cannot be negative");
            if (problems == 0) return null;
            return SummaryLogHead + "ability=" + (ability ?? "<null ability>") + " problems=" + problems.ToString(System.Globalization.CultureInfo.InvariantCulture) + " (first occurrences are logged above; repeats are counted only)";
        }
        // NOT APPLIED 줄 끝에 붙는 안내: 사슬 액션이 던진 예외는 엔진 ActionList.Run 이 삼켜 게임 로그로만 보낸다(Guarded 주석의 IL 출처) — 모드 로그에는 원문이 없다.
        public const string ChainExceptionHint = " (if one of the spell's own actions threw, the exception text is in the game's GameLogFull.txt, not in this mod log)";

        // ---- 시전 뒤 수치 대조(소유자 지시 2026-11-16 마무리) ----
        // 되읽기가 버프 「보유」만 보면, 복사한 랭크 설정이 원 주문과 다른 값을 내도 드러나지 않는다. 그래서 원 주문 블루프린트의 랭크 설정 **필드**를 읽어
        // 아래 순수 함수로 기대값을 계산하고(리터럴 복제 없음), 구성원에게 실제로 붙은 버프의 값과 대조한다.
        // 식 자체(진행 방식의 뜻)는 엔진 IL 을 옮긴 복제다 → PackEmpower.Build 가 로드 때마다 엔진의 ApplyProgression/ApplyMinMax·ToRounds 와 1~20 전 구간 대조한다(어긋나면 던짐).
        public sealed class RankSpec
        {
            public readonly string Progression; public readonly int Start, Step; public readonly bool UseMin; public readonly int Min; public readonly bool UseMax; public readonly int Max;
            public RankSpec(string progression, int start, int step, bool useMin, int min, bool useMax, int max)
            { Progression = progression; Start = start; Step = step; UseMin = useMin; Min = min; UseMax = useMax; Max = max; }
            public override string ToString() { return Progression + "(start " + Start + ", step " + Step + (UseMin ? ", min " + Min : "") + (UseMax ? ", max " + Max : "") + ")"; }
        }
        // 지금 후보 10종이 쓰는 진행 방식 둘만 옮겼다(Kingmaker ContextRankProgression 의 이름). 그 밖은 계산하지 않고 던진다.
        public const string ProgressionAsIs = "AsIs", ProgressionStartPlusDivStep = "StartPlusDivStep";
        // 출처: ContextRankConfig.ApplyProgression IL @004E(AsIs: 값 그대로) · @005C~0073(StartPlusDivStep: 1 + max((값 − 시작) / 단계, 0), 정수 나눗셈)
        //       ContextRankConfig.ApplyMinMax IL @0000~004B(m_UseMin → max(값, m_Min), m_UseMax → min(값, m_Max)). Assembly-CSharp 2026-11-16 직독.
        public static int RankValue(RankSpec s, int baseValue)
        {
            if (s == null) throw new ArgumentNullException("s");
            int v;
            switch (s.Progression)
            {
                case ProgressionAsIs: v = baseValue; break;
                case ProgressionStartPlusDivStep:
                    if (s.Step == 0) throw new CandidateDataException("PackWarden: rank progression StartPlusDivStep with step 0");
                    v = 1 + Math.Max((baseValue - s.Start) / s.Step, 0);
                    break;
                default: throw new CandidateDataException("PackWarden: rank progression '" + s.Progression + "' is not modelled (only AsIs and StartPlusDivStep)");
            }
            if (s.UseMin) v = Math.Max(v, s.Min);
            if (s.UseMax) v = Math.Min(v, s.Max);
            return v;
        }
        // 시전자 레벨 = max(1, 헌터 레벨). 출처: ContextCalculateAbilityParamsBasedOnClass.Calculate @0087~0096(ReplaceCasterLevel = 클래스 레벨) →
        // RuleCalculateAbilityParams.OnTrigger @009B~009D(Math.Max(1, …)). 그 뒤에 더해지는 시전자 레벨 보너스(m_BonusCasterLevel)는 옮기지 않았다 — 있으면 대조가 어긋나 보고된다.
        public static int CasterLevelFor(int hunterLevel)
        {
            if (hunterLevel < 0) throw new ArgumentOutOfRangeException("hunterLevel", hunterLevel, "PackWarden: hunter level cannot be negative");
            return Math.Max(1, hunterLevel);
        }
        // 출처: Kingmaker.Utility.Rounds.get_Seconds IL @0007(값 × 6초).
        public const int SecondsPerRound = 6;
        // 출처: DurationRateExtension.ToRounds IL(Rounds 1 · Minutes 10 · TenMinutes 100 · Hours 600 · Days 14400). 이름 = Kingmaker DurationRate.
        public static int RoundsPerRate(string rateName)
        {
            switch (rateName)
            {
                case "Rounds": return 1;
                case "Minutes": return 10;
                case "TenMinutes": return 100;
                case "Hours": return 600;
                case "Days": return 14400;
                default: throw new CandidateDataException("PackWarden: duration rate '" + rateName + "' is not modelled");
            }
        }
        // 버프의 예정 지속(초) = 랭크 × 단위당 라운드 × 6. 출처: ContextDurationValue.Calculate(주사위 Zero·보너스 = 랭크) → Rounds.Seconds.
        public static long ExpectedDurationSeconds(int rank, string rateName)
        {
            if (rank < 0) throw new ArgumentOutOfRangeException("rank", rank, "PackWarden: rank cannot be negative");
            return checked((long)rank * RoundsPerRate(rateName) * SecondsPerRound);
        }
        // 「지속 없음(영구)」 을 나타내는 값(초 단위 기대·실제 비교에서 유한한 값과 섞이지 않게).
        public const long PermanentDuration = -1;
        // 공유값 = (int)(주사위 값 × Modifier). 출처: ContextCalculateSharedValue.Calculate IL(Value.Calculate × Modifier → 정수).
        public static int SharedValue(int bonus, double modifier) { return (int)(bonus * modifier); }
        // 공유값 조건. 출처: ContextConditionSharedValueHigher.CheckCondition IL @0013~0033(Inverted 아니면 값 >= 문턱, Inverted 면 값 <= 문턱).
        public static bool SharedValueConditionPasses(int value, int higherOrEqual, bool inverted) { return inverted ? !(value > higherOrEqual) : !(value < higherOrEqual); }

        public enum ValueReadback { Unknown = 0, Match, Mismatch, Unreadable }
        // expected·actual = null 이면 읽을 수 없음. 읽을 수 없는 것을 「일치」로 넘기지 않는다.
        public static ValueReadback ClassifyValue(long? expected, long? actual)
        {
            if (expected == null || actual == null) return ValueReadback.Unreadable;
            return expected.Value == actual.Value ? ValueReadback.Match : ValueReadback.Mismatch;
        }
        // 지속(틱) 대조의 상대 허용 오차. 근거: ToyBox BagOfPatches.Multipliers::GetNewBuffDuration IL @0046 conv.r4 가 틱을 float(유효 24비트, 상대 반올림 오차 ≤ 2^-24 ≈ 6e-8)로 바꿨다가
        // @0052 conv.i8 로 되돌린다 — 배수 1 이어도 57600s → 57599.9967232s. 1e-6 = 그 반올림 오차의 약 17배 여유. 배수가 1 이 아닌 차이(예: ×2)는 이보다 훨씬 커서 그대로 어긋남이다.
        public const double DurationRelativeTolerance = 1e-6;
        // 지속 대조: 기대·실제가 모두 양수일 때만 |기대 − 실제| ≤ 허용 오차 × 기대 를 일치로 본다. 기대 0 · 영구(PermanentDuration) · 음수가 끼면 정확 일치만(허용 오차 없음). null = 읽을 수 없음.
        public static ValueReadback ClassifyDuration(long? expected, long? actual)
        {
            if (expected == null || actual == null) return ValueReadback.Unreadable;
            long e = expected.Value, a = actual.Value;
            if (e <= 0 || a <= 0) return e == a ? ValueReadback.Match : ValueReadback.Mismatch;
            double diff = Math.Abs((double)e - (double)a);
            return diff <= DurationRelativeTolerance * (double)e ? ValueReadback.Match : ValueReadback.Mismatch;
        }
        public static bool IsValueProblem(ValueReadback r)
        {
            switch (r)
            {
                case ValueReadback.Match: return false;
                case ValueReadback.Mismatch:
                case ValueReadback.Unreadable: return true;
                default: throw new ArgumentOutOfRangeException("r", r, "PackWarden: unclassified value readback");
            }
        }
        public const string ValueLogHead = "PackWarden: pack empowerment VALUE CHECK ";

        // ---- 수치 대조의 판정·보고 문구(순수 — 시험이 값으로 실행한다). PackEmpower.CheckValues 는 입력을 모아 넘기고, 돌아온 줄을 그대로 보고만 한다(판정 분기를 게임 쪽에 두지 않는다). ----
        // 능력치 수정치 한 줄: 출처 객체(그 수정치를 건 사실 — 없으면 null), 수정치 종류(Kingmaker ModifierDescriptor 의 이름), 값.
        public sealed class ModifierRow
        {
            public readonly object Source; public readonly string Descriptor; public readonly int Value;
            public ModifierRow(object source, string descriptor, int value) { Source = source; Descriptor = descriptor; Value = value; }
        }
        // 「출처가 own(같은 객체 — 참조 동일성)이고 종류가 같은」 수정치 값의 합. 해당 줄이 없으면 0(= 수정치가 안 붙었다 — 기대값과 다르면 ValueProblems 가 어긋남으로 보고한다).
        public static long SumOwnModifiers(IEnumerable<ModifierRow> rows, object own, string descriptor)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (own == null) throw new ArgumentNullException("own");
            if (string.IsNullOrEmpty(descriptor)) throw new ArgumentException("PackWarden: modifier descriptor name is missing", "descriptor");
            long sum = 0;
            foreach (var r in rows)
            {
                if (r == null) throw new ArgumentNullException("rows", "PackWarden: null modifier row");
                if (r.Descriptor == null) throw new ArgumentNullException("rows", "PackWarden: modifier row without a descriptor name");
                if (ReferenceEquals(r.Source, own) && r.Descriptor == descriptor) sum = checked(sum + r.Value);
            }
            return sum;
        }
        // 대조 항목 하나. Actual = null 이면 읽을 수 없음(UnreadableWhy 필수), 아니면 UnreadableWhy 는 null 이어야 한다. IsDuration = 값이 틱(지속)인가 — 문구가 다르고, 판정은 ClassifyDuration(작은 상대 허용 오차)으로 한다.
        public sealed class ValueCheck
        {
            public readonly string What; public readonly long? Expected, Actual; public readonly string UnreadableWhy; public readonly bool IsDuration;
            public ValueCheck(string what, long? expected, long? actual, string unreadableWhy, bool isDuration)
            {
                if (string.IsNullOrEmpty(what)) throw new ArgumentException("PackWarden: value check without a name", "what");
                if ((actual == null) != (unreadableWhy != null)) throw new ArgumentException("PackWarden: value check '" + what + "' must have either an actual value or the reason it is unreadable (exactly one)", "unreadableWhy");
                What = what; Expected = expected; Actual = actual; UnreadableWhy = unreadableWhy; IsDuration = isDuration;
            }
        }
        public sealed class ValueProblem
        {
            public readonly string What; public readonly ValueReadback Result; public readonly string Message;
            public ValueProblem(string what, ValueReadback result, string message) { What = what; Result = result; Message = message; }
        }
        // 보고할 항목만(어긋남·읽을 수 없음) 완성된 오류 줄과 함께 돌려준다. 일치는 넣지 않는다. who·tail = 줄에 붙는 식별 문구(구성원·버프 / 레벨).
        public static List<ValueProblem> ValueProblems(IEnumerable<ValueCheck> checks, string who, string tail)
        {
            if (checks == null) throw new ArgumentNullException("checks");
            if (who == null) throw new ArgumentNullException("who");
            if (tail == null) throw new ArgumentNullException("tail");
            var problems = new List<ValueProblem>();
            foreach (var c in checks)
            {
                if (c == null) throw new ArgumentNullException("checks", "PackWarden: null value check");
                var r = c.IsDuration ? ClassifyDuration(c.Expected, c.Actual) : ClassifyValue(c.Expected, c.Actual);
                if (IsValueProblem(r))
                    problems.Add(new ValueProblem(c.What, r, ValueLogHead + "what=" + c.What + " result=" + r + " " + who + " expected=" + ValueText(c.Expected, c.IsDuration) + " actual=" + (c.UnreadableWhy ?? ValueText(c.Actual, c.IsDuration)) + tail));
            }
            return problems;
        }
        // 지속(틱)은 「permanent」 또는 초, 그 밖은 수 그대로. null = 읽을 수 없음.
        public static string ValueText(long? value, bool isDuration)
        {
            if (value == null) return "unreadable";
            if (!isDuration) return value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return value.Value == PermanentDuration ? "permanent" : (value.Value / (double)TimeSpan.TicksPerSecond).ToString(System.Globalization.CultureInfo.InvariantCulture) + "s";
        }
        // 예정 지속(초) → 틱. 영구는 영구 그대로.
        public static long DurationTicks(long seconds)
        {
            if (seconds == PermanentDuration) return PermanentDuration;
            if (seconds < 0) throw new ArgumentOutOfRangeException("seconds", seconds, "PackWarden: duration cannot be negative");
            return checked(seconds * TimeSpan.TicksPerSecond);
        }

        // 본문을 돌리고, 던지면 onError 로 넘긴 뒤 삼킨다(거짓 반환). 되읽기의 한 단계(한 구성원의 대조·주사위 재대조)가 던져도 다음 단계가 돌게 하는 용도.
        // 엔진 ActionList.Run(IL @0013~0057, catch System.Exception)은 GameAction.RunAction 의 예외를 전부 잡아 게임 로그 채널(PFLog.Actions)로만 보낸다 —
        // 여기서 모드 로그로 보고하지 않으면 검사자에게 보이지 않는다. onError 가 던지는 것은 삼키지 않는다(보고 자체의 실패를 숨기지 않는다).
        public static bool Guarded(Action body, Action<Exception> onError)
        {
            if (body == null) throw new ArgumentNullException("body");
            if (onError == null) throw new ArgumentNullException("onError");
            try { body(); return true; }
            catch (Exception e) { onError(e); return false; }
        }

        // ---- 버프 컴포넌트 허용 목록(소유자 지시 2026-11-16 마무리: 거부 목록을 뒤집음) ----
        // 후보 10종이 거는 버프(Death Ward 의 덤 버프 포함)와, **그 버프의 컴포넌트가 액션 목록으로 거는 버프(중첩, 재귀)** 에 **지금 실제로 있는** 컴포넌트 형식 전부.
        // blueprints.zip 2026-11-16 직독 — 시험이 게임 데이터의 현재 집합과 양방향 대조한다(중첩 포함).
        // 여기 없는 형식이 버프에 하나라도 있으면(게임 패치·다른 모드가 그 버프를 바꿈) 그 후보를 끈다: 몸 크기·무기 크기·변신을 바꾸는 새 컴포넌트를 이름을 몰라도 막는다.
        // 중첩에서 온 항목: RemoveBuffIfCasterIsMissing = Spells/Level2/NaturalRythmBuffStackable.jbp(Natural Rhythm 버프의 AddInitiatorAttackRollTrigger.Action 이 거는 버프)의 유일한 컴포넌트.
        // 알려진 한계: 10종 합집합이다(후보별 목록이 아님) · 형식의 **이름만** 비교한다(네임스페이스·어셈블리·필드 값은 보지 않음) · 블루프린트 조립 때 1회 검사한다(그 뒤의 변경은 보지 못함).
        // 이 목록은 엔진 어셈블리의 형식만 받는다. 다른 모드가 자기 어셈블리의 부품을 붙인 경우는 아래 「알려진 외부 부품 원장」(KnownForeignBuffComponents)의 행과 전부 일치할 때만 받아들인다.
        public static readonly string[] AllowedBuffComponentTypes =
        {
            "AddConditionImmunity", "AddContextStatBonus", "AddCumulativeDamageBonus", "AddDamageResistanceEnergy", "AddEffectFastHealing", "AddEnergyImmunity", "AddImmunityToEnergyDrain", "AddInitiatorAttackRollTrigger",
            "AddStatBonus", "BuffDescriptorImmunity", "ContextRankConfig", "EquipmentWeaponTypeEnhancement", "ManeuverImmunity", "PolymorphBonuses", "RemoveBuffIfCasterIsMissing", "SavingThrowBonusAgainstDescriptor", "SpecificBuffImmunity",
            "SpellDescriptorComponent", "SpellImmunityToSpellDescriptor",
        };
        // 그 버프들의 컴포넌트 안(액션 목록)에 **지금 실제로 있는** 액션 형식 전부. 출처: Spells/Level2/NaturalRythmBuff.jbp 의 AddInitiatorAttackRollTrigger.Action = ContextActionApplyBuff 1개
        // (blueprints.zip 2026-11-16 직독 — 나머지 버프의 컴포넌트에는 액션이 없다; 시험이 양방향 대조). ContextActionApplyBuff 가 거는 버프는 같은 허용 목록으로 재귀 검사한다(PackEmpower.BuffProblems).
        // 여기 없는 액션 형식(조건 분기 포함)은 무엇을 하는지 모르므로 건너뛰지 않고 그 후보를 끈다.
        public static readonly string[] AllowedNestedBuffActionTypes = { "ContextActionApplyBuff" };
        // 원장에 없는 형식 이름(중복 없이, 처음 나온 순서). 빈 배열 = 전부 허용.
        public static string[] UnlistedBuffComponents(IEnumerable<string> typeNames) { return Unlisted(AllowedBuffComponentTypes, typeNames); }
        public static string[] UnlistedNestedBuffActions(IEnumerable<string> typeNames) { return Unlisted(AllowedNestedBuffActionTypes, typeNames); }
        // 버프 하나의 모양 사유(없으면 빈 목록): 원장에 없는 컴포넌트 형식, 원장에 없는 (컴포넌트 안의) 액션 형식. path = 오류 줄에 적을 그 버프까지의 경로.
        // 판정을 여기 둔다(순수 — 시험이 값으로 실행): PackEmpower.BuffProblems 는 형식 이름을 모아 넘기고 돌아온 사유를 그대로 싣는다.
        public static List<string> BuffShapeProblems(string path, IEnumerable<string> componentTypes, IEnumerable<string> nestedActionTypes)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("PackWarden: buff path is missing", "path");
            var problems = new List<string>();
            var unlisted = UnlistedBuffComponents(componentTypes);
            if (unlisted.Length > 0) problems.Add("buff " + path + " has component type(s) not on the allowlist: " + string.Join(", ", unlisted));
            var unknown = UnlistedNestedBuffActions(nestedActionTypes);
            if (unknown.Length > 0) problems.Add("buff " + path + " runs action type(s) the pack empowerment does not understand: " + string.Join(", ", unknown));
            return problems;
        }
        static string[] Unlisted(string[] ledger, IEnumerable<string> typeNames)
        {
            if (typeNames == null) throw new ArgumentNullException("typeNames");
            var bad = new List<string>();
            foreach (var n in typeNames)
            {
                if (n == null) throw new ArgumentNullException("typeNames", "PackWarden: null type name");
                if (Array.IndexOf(ledger, n) < 0 && !bad.Contains(n)) bad.Add(n);
            }
            return bad.ToArray();
        }
        // ---- 알려진 외부 부품 원장(소유자 결정 2026-11-16: TabletopTweaks 의 그 부품을 「알려진 외부 부품」 으로 받아들여 죽음 방호(Death Ward)를 쓸 수 있게 한다) ----
        // 허용 목록(AllowedBuffComponentTypes)은 엔진 어셈블리의 형식만 받는다. 다른 모드가 후보 버프에 붙이는 **그 모드 어셈블리의** 부품은 이 원장의 행과
        // (붙어 있는 버프의 GUID · 형식 전체 이름 · 어셈블리 이름 · 필드 값) 이 **전부** 일치할 때만 받아들이고, 받아들였다는 사실을 모드 로그에 한 줄 남긴다(KnownForeignAcceptedLine).
        // 하나라도 다르면(다른 버프에 붙음 · 이름만 같은 다른 어셈블리의 형식 · 필드 값이 다름 · 원장에 없는 부품) 지금까지처럼 그 후보를 끈다.
        //
        // 행 1 — 출처: TabletopTweaks-Base 2.6.18 `TabletopTweaks.Base.Bugfixes.Abilities.Spells/BlueprintsCache_Init_Patch::PatchDeathWard` + TabletopTweaks-Core 0.7.16 (설치본 DLL dnlib 직독 2026-11-16).
        //   · 패치: 설정 Fixes.json Spells["DeathWard"](설치본 Enabled = true, 설명 "Now suppresses existing negative levels.") 가 꺼져 있지 않으면
        //     DeathWardBuff(b0253e57…) 와 DeathWardBuff_IsFromSpell(a04d666d… — 컷신 버프, 후보 사슬이 걸지 않는다) 에 AddComponent<SuppressBuffsTTT> 를 **덧붙인다**
        //     (Continuous = true, m_Buffs = [NegativeLevelsBuff b02b6b92…]; Schools · Descriptor 는 생성자 기본값 = 빈 배열 · 0). 지우는 부품은 없고, 그 밖에는 두 버프의 설명문만 바꾼다.
        //     주문 블루프린트(DeathWardCast · DeathWard)는 이 패치가 건드리지 않는다.
        //   · 부품: `TabletopTweaks.Core.NewComponents.OwlcatReplacements.SuppressBuffsTTT`(어셈블리 TabletopTweaks-Core, 기반 형식 UnitFactComponentDelegate).
        //     인스턴스 필드는 4개뿐이다 — m_Buffs(BlueprintBuffReference[]) · Schools(SpellSchool[]) · Descriptor(SpellDescriptorWrapper) · Continuous(bool). 액션 목록·ContextValue·버프를 거는 경로가 없다.
        //     OnActivate: Continuous 면 UnitPartBuffSupressTTT.AddContinuousEntry → 소유자에게 붙어 있는(뒤에 붙는 것은 Buff.OnAttach 후크) 버프 중
        //     「주문 서술자가 Descriptor 와 겹침 ∨ 학파가 Schools 에 있음 ∨ 블루프린트가 m_Buffs 에 있음」 인 것의 IsSuppressed 를 켜고 Deactivate 한다. OnDeactivate 에서 되돌린다(Activate).
        //     PatchDeathWard 의 값에서는 「이미 받은 음수 레벨의 불이익」 버프(NegativeLevelsBuff) 하나만 억제된다 — 엔진 SpellDescriptorOperations.HasAnyFlag 는 (a & b) > 0 이라 Descriptor 0 은 아무것도 고르지 않는다.
        //     몸 크기 · 무기 크기 · 변신 · 유닛 교체와 무관하다(허용 목록의 목적 = 보조 동물의 소형 고정을 깨는 효과를 막는 것).
        //   · **같은 형식이라도 필드 값이 다르면 다른 버프를 억제한다**(예: Descriptor 에 Polymorph, Schools 에 Transmutation) → 형식만이 아니라 필드 값까지 이 행과 같아야 받아들인다(KnownForeignSettingProblems).
        //   · 걷기(액션 탐색): 받아들인 외부 부품도 엔진 부품과 똑같이 걷는다. 위 필드 4개에서는 액션이 나오지 않지만(시험이 설치본 DLL 의 필드 목록을 지문으로 대조),
        //     설치본이 지문과 다른 판이어서 액션을 쥐고 있으면 그 액션이 중첩 액션 원장으로 판정된다 — 「안에 액션이 없다」 를 실행 중에는 가정하지 않는다.
        //   시험(tests/PackRulesTests.cs CheckKnownForeignBuffComponents)이 이 행을 설치된 TabletopTweaks DLL 과 대조한다(설치돼 있지 않으면 통과가 아니라 SKIP).
        public const string TttSuppressBuffsType = "TabletopTweaks.Core.NewComponents.OwlcatReplacements.SuppressBuffsTTT";
        public const string TttCoreAssembly = "TabletopTweaks-Core";
        // SuppressBuffsTTT 의 필드 이름(게임 쪽 어댑터 PackEmpower.ForeignSuppressViewOf 가 리플렉션으로 읽는다 — 없으면 던지고 그 후보가 꺼진다).
        public const string TttSuppressBuffsField = "m_Buffs", TttSuppressSchoolsField = "Schools", TttSuppressDescriptorField = "Descriptor", TttSuppressContinuousField = "Continuous";
        public const string TttDeathWardPatchType = "TabletopTweaks.Base.Bugfixes.Abilities.Spells/BlueprintsCache_Init_Patch";
        public const string TttDeathWardPatchMethod = "PatchDeathWard";
        public const string TttDeathWardSettingKey = "DeathWard";
        public const string DeathWardBuffGuid = "b0253e57a75b621428c1b89de5a937d1";      // Spells/Level4/DeathWardBuff.jbp (blueprints.zip 2026-11-16 직독 — 시험이 대조)
        public const string NegativeLevelsBuffGuid = "b02b6b9221241394db720ca004ea9194"; // Root/SystemMechanics/Conditions/NegativeLevelsBuff.jbp (같음)
        public sealed class KnownForeignBuffComponent
        {
            // Mod · VerifiedVersion · PatchMethod = 출처(로그 줄에 적는다). BuffGuid = 이 부품을 받아들이는 유일한 버프. SuppressedBuffGuids = 그 부품의 m_Buffs 가 정확히 이 집합이어야 한다.
            public readonly string Mod, VerifiedVersion, PatchMethod, TypeFullName, AssemblyName, BuffGuid, BuffName;
            public readonly string[] SuppressedBuffGuids, SuppressedBuffNames;
            public KnownForeignBuffComponent(string mod, string verifiedVersion, string patchMethod, string typeFullName, string assemblyName, string buffGuid, string buffName, string[] suppressedBuffGuids, string[] suppressedBuffNames)
            {
                foreach (var x in new[] { mod, verifiedVersion, patchMethod, typeFullName, assemblyName, buffGuid, buffName })
                    if (string.IsNullOrEmpty(x)) throw new ArgumentException("PackWarden: known foreign component rule has an empty field");
                if (suppressedBuffGuids == null || suppressedBuffNames == null || suppressedBuffGuids.Length == 0 || suppressedBuffGuids.Length != suppressedBuffNames.Length)
                    throw new ArgumentException("PackWarden: known foreign component rule needs the same non-zero number of suppressed buff GUIDs and names");
                if (!IsGuid32(buffGuid)) throw new ArgumentException("PackWarden: known foreign component rule GUID '" + buffGuid + "' is not 32 lowercase hex");
                foreach (var g in suppressedBuffGuids)
                    if (!IsGuid32(g)) throw new ArgumentException("PackWarden: known foreign component rule GUID '" + g + "' is not 32 lowercase hex");
                Mod = mod; VerifiedVersion = verifiedVersion; PatchMethod = patchMethod; TypeFullName = typeFullName; AssemblyName = assemblyName; BuffGuid = buffGuid; BuffName = buffName;
                SuppressedBuffGuids = (string[])suppressedBuffGuids.Clone(); SuppressedBuffNames = (string[])suppressedBuffNames.Clone();
            }
        }
        static bool IsGuid32(string g)
        {
            if (g == null || g.Length != 32) return false;
            foreach (char ch in g) if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'))) return false;
            return true;
        }
        public static readonly KnownForeignBuffComponent[] KnownForeignBuffComponents =
        {
            new KnownForeignBuffComponent("TabletopTweaks-Base", "2.6.18", TttDeathWardPatchMethod, TttSuppressBuffsType, TttCoreAssembly, DeathWardBuffGuid, "DeathWardBuff",
                new[] { NegativeLevelsBuffGuid }, new[] { "NegativeLevelsBuff" }),
        };
        // 게임 쪽 어댑터가 읽은 SuppressBuffsTTT 한 개의 필드 값. 읽지 못한 값은 여기 들어오지 않는다(어댑터가 던지고, 호출부가 그 후보를 끄는 사유로 바꾼다).
        public sealed class ForeignSuppressView
        {
            public readonly string[] BuffGuids; public readonly int SchoolCount; public readonly long Descriptor; public readonly bool Continuous;
            public ForeignSuppressView(string[] buffGuids, int schoolCount, long descriptor, bool continuous)
            {
                if (buffGuids == null) throw new ArgumentNullException("buffGuids");
                foreach (var g in buffGuids) if (g == null) throw new ArgumentNullException("buffGuids", "PackWarden: null suppressed-buff GUID");
                if (schoolCount < 0) throw new ArgumentOutOfRangeException("schoolCount", schoolCount, "PackWarden: school count cannot be negative");
                BuffGuids = (string[])buffGuids.Clone(); SchoolCount = schoolCount; Descriptor = descriptor; Continuous = continuous;
            }
        }
        // 그 버프(GUID)에 붙은 그 형식(전체 이름) · 그 어셈블리의 부품에 해당하는 원장 행. 없으면 null = 모르는 외부 부품(호출부가 그 후보를 끈다).
        // 셋 다 대소문자까지 정확히 같아야 한다: 다른 버프에 붙은 같은 부품, 이름만 같은 다른 어셈블리의 형식, 짧은 이름만 같은 형식은 행이 없다.
        public static KnownForeignBuffComponent KnownForeignRuleFor(string buffGuid, string typeFullName, string assemblyName)
        {
            if (string.IsNullOrEmpty(buffGuid)) throw new ArgumentException("PackWarden: buff GUID is missing", "buffGuid");
            if (string.IsNullOrEmpty(typeFullName)) throw new ArgumentException("PackWarden: component type name is missing", "typeFullName");
            if (string.IsNullOrEmpty(assemblyName)) throw new ArgumentException("PackWarden: component assembly name is missing", "assemblyName");
            KnownForeignBuffComponent found = null;
            foreach (var r in KnownForeignBuffComponents)
            {
                if (!string.Equals(r.BuffGuid, buffGuid, StringComparison.Ordinal) || !string.Equals(r.TypeFullName, typeFullName, StringComparison.Ordinal) || !string.Equals(r.AssemblyName, assemblyName, StringComparison.Ordinal)) continue;
                if (found != null) throw new InvalidOperationException("PackWarden: EmpowerRules.KnownForeignBuffComponents has two rows for " + typeFullName + " on buff " + buffGuid);
                found = r;
            }
            return found;
        }
        // 원장 행에 해당하는 부품의 필드 값이 그 행과 같은가. 빈 목록 = 받아들인다. 사유가 하나라도 있으면 받아들이지 않는다(호출부가 그 후보를 끈다).
        public static List<string> KnownForeignSettingProblems(KnownForeignBuffComponent rule, ForeignSuppressView view)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            if (view == null) throw new ArgumentNullException("view");
            var problems = new List<string>();
            // 순서 없는 비교(중복 포함 — 같은 버프가 두 번 들어 있으면 다른 값이다).
            var want = (string[])rule.SuppressedBuffGuids.Clone();
            var got = (string[])view.BuffGuids.Clone();
            Array.Sort(want, StringComparer.Ordinal);
            Array.Sort(got, StringComparer.Ordinal);
            bool same = want.Length == got.Length;
            for (int i = 0; same && i < want.Length; i++) same = string.Equals(want[i], got[i], StringComparison.Ordinal);
            if (!same)
                problems.Add(TttSuppressBuffsField + " is [" + string.Join(", ", view.BuffGuids) + "], the ledger row allows exactly [" + string.Join(", ", rule.SuppressedBuffGuids) + "]");
            if (view.SchoolCount != 0) problems.Add(TttSuppressSchoolsField + " has " + view.SchoolCount + " spell school(s), the ledger row allows none");
            if (view.Descriptor != 0) problems.Add(TttSuppressDescriptorField + " is " + view.Descriptor.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", the ledger row allows 0 (no spell descriptor)");
            if (!view.Continuous) problems.Add(TttSuppressContinuousField + " is false, the ledger row allows true only");
            return problems;
        }
        public const string KnownForeignLogHead = "PackWarden: pack empowerment ACCEPTED known foreign component ";
        // 받아들였을 때 모드 로그에 남기는 정보 줄(조용히 받아들이지 않는다). path = 사슬에서 그 버프까지의 경로.
        public static string KnownForeignAcceptedLine(string candidateKey, string path, KnownForeignBuffComponent rule)
        {
            if (string.IsNullOrEmpty(candidateKey)) throw new ArgumentException("PackWarden: candidate key is missing", "candidateKey");
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("PackWarden: buff path is missing", "path");
            if (rule == null) throw new ArgumentNullException("rule");
            var suppressed = new List<string>();
            for (int i = 0; i < rule.SuppressedBuffGuids.Length; i++) suppressed.Add(rule.SuppressedBuffNames[i] + " " + rule.SuppressedBuffGuids[i]);
            return KnownForeignLogHead + "candidate=" + candidateKey + " buff=" + path + " component=" + rule.TypeFullName + " assembly=" + rule.AssemblyName
                + " (row verified against " + rule.Mod + " " + rule.VerifiedVersion + " " + rule.PatchMethod + "; it suppresses only " + string.Join(", ", suppressed.ToArray()) + "; ledger EmpowerRules.KnownForeignBuffComponents)";
        }

        // 객체 그래프를 인스턴스 필드(비공개·상속 포함)로 걸어 T 인 객체를 처음 만난 순서로 모은다 — 버프 컴포넌트 안 어디에 있든 액션을 찾기 위한 것(필드 이름·깊이를 가정하지 않는다).
        //   stopAt(o) = 참이면 그 객체는 모으지도 내려가지도 않는다(다른 블루프린트·블루프린트 참조·Unity 객체). T 인 객체는 어느 어셈블리 것이든 모은다.
        //   배열·System 컬렉션은 원소를(사전은 키·값을) 건다. 그 밖의 객체는 **어느 어셈블리 것이든** 필드로 내려간다(엔진·다른 모드 형식을 가리지 않는다 — 못 들어가서 놓치는 객체가 없게).
        //   내려가지 않는 것(잎): 기본형·열거형·문자열·대리자·리플렉션 객체, 그리고 제네릭이 아닌 System 형식(Guid·TimeSpan·decimal 같은 단순 값). 같은 객체는 한 번만 방문한다(순환 방지).
        //   상한(독립 검토 2차 2026-11-16 지적 1): 깊이 ReachMaxDepth · 방문 객체 수 ReachMaxVisited 를 넘으면 **던진다**(「다 본 것」 으로 돌려주지 않는다 — 호출부가 그 후보를 끄는 사유로 바꾼다).
        //   상한이 없으면 다른 모드의 컴포넌트가 쥔 깊은 런타임 객체 사슬에서 재귀가 StackOverflowException(잡을 수 없음)으로 게임을 로드 중에 죽인다.
        public static List<T> ReachableOf<T>(object root, Func<object, bool> stopAt) where T : class
        {
            return ReachableOf<T>(root, stopAt, new WalkBudget(), 0);
        }
        // 예산을 같이 쓰는 걷기(검토 2026-11-17 지적 4): 후보 하나의 사슬 액션 · 버프 · 버프 부품 안 객체를 **한 예산**(같은 방문 집합 · 깊이 · 방문 수)으로 센다.
        // baseDepth = 이 뿌리의 깊이(버프가 중첩된 만큼 더해진다 — 깊이는 누적이라 재귀 프레임 수의 상한이 그대로 ReachMaxDepth 다).
        public static List<T> ReachableOf<T>(object root, Func<object, bool> stopAt, WalkBudget budget, int baseDepth) where T : class
        {
            if (root == null) throw new ArgumentNullException("root");
            if (stopAt == null) throw new ArgumentNullException("stopAt");
            if (budget == null) throw new ArgumentNullException("budget");
            if (baseDepth < 0) throw new ArgumentOutOfRangeException("baseDepth", baseDepth, "PackWarden: walk depth cannot be negative");
            var found = new List<T>();
            Reach(root, stopAt, found, budget, baseDepth);
            return found;
        }
        // 걷기 상한을 넘었다는 예외(「다 본 것」 이 아니다). 호출부(PackEmpower.BuildCandidate 의 후보별 경계)가 그 후보를 끄는 사유로 바꾼다.
        public sealed class WalkLimitException : InvalidOperationException
        {
            public WalkLimitException(string message) : base(message) { }
        }
        // 후보 하나를 검사하는 동안 같이 쓰는 걷기 예산(검토 2026-11-17 지적 4 — 그 전에는 ReachableOf 만 상한이 있었고 부품마다 새로 시작했으며,
        // 사슬의 CollectBuffs · TierProblem · WalkExpected 와 버프 사이 재귀 BuffProblems 에는 상한이 없었다). 상수는 위 ReachMaxDepth · ReachMaxVisited 를 그대로 쓴다.
        // 깊이는 재귀 한 단계 = 1 이라 재귀 프레임 수가 ReachMaxDepth(64) 를 넘지 않는다: 사슬 걷기(CollectBuffs 등)와 버프 걷기(BuffProblems → Reach)는 서로 안에서 돌지 않으므로
        // 한 스레드의 최대 중첩은 그중 하나의 64 단계 + 호출부 몇 단계다(1 단계 = 작은 스택 프레임 1개 — 기본 1MB 스택에서 멀다). 그래서 재귀 형태를 유지했다.
        public sealed class WalkBudget
        {
            public int Visited { get; private set; }
            internal readonly HashSet<object> Seen = new HashSet<object>(new SameObject());
            // 한 객체(액션 목록·액션·버프·부품 안 객체)에 들어갈 때 부른다. at = 오류 문구에 적을 위치. 상한을 넘으면 WalkLimitException.
            public void Step(int depth, string at)
            {
                if (depth < 0) throw new ArgumentOutOfRangeException("depth", depth, "PackWarden: walk depth cannot be negative");
                if (depth > ReachMaxDepth) throw new WalkLimitException("PackWarden: object graph walk went deeper than EmpowerRules.ReachMaxDepth (" + ReachMaxDepth + ") at " + at + "; the graph was not fully inspected");
                if (++Visited > ReachMaxVisited) throw new WalkLimitException("PackWarden: object graph walk visited more than EmpowerRules.ReachMaxVisited (" + ReachMaxVisited + ") objects at " + at + "; the graph was not fully inspected");
            }
        }
        // 걷기 상한. 근거(독립 검토 2차 2026-11-16 의 형식 그래프 대조): 본편 허용 형식 19종(AllowedBuffComponentTypes) + ContextActionApplyBuff 에서 인스턴스 필드로 도달하는 형식은 32개,
        // 컴포넌트 하나에서 방문하는 객체는 10개 안팎이다. 깊이 64 = 그 32개 형식이 전부 한 줄로 이어지고 사이마다 컬렉션이 한 겹씩 끼어도 닿는 값(2 × 32),
        // 방문 4096 = 관측치(10 안팎)의 약 400배. 둘 다 재귀 1단계가 스택 프레임 1개라 스택 한계(1MB)에서 멀다. 본편 버프가 이 값을 넘으면 그 후보가 꺼지고 DISABLED 줄에 이 상수 이름이 나온다.
        // 2026-11-17(검토 지적 4): 같은 두 상수를 **후보 하나 전체**(사슬 액션 + 버프 + 부품 안 객체)의 공유 예산(WalkBudget)으로 쓴다. 후보 하나의 JSON 객체·배열 수 어림은
        // 최대 82(Magic Fang, Greater — 사슬 + 버프 5종; blueprints.zip 2026-11-17) — 4096 의 약 1/50. 시험(CheckReview1117)이 그 어림 × 4 ≤ 4096 을 단언한다.
        public const int ReachMaxDepth = 64;
        public const int ReachMaxVisited = 4096;
        // 참조 동일성 비교자(Equals 재정의에 속지 않게).
        sealed class SameObject : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object a, object b) { return ReferenceEquals(a, b); }
            int IEqualityComparer<object>.GetHashCode(object o) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o); }
        }
        // depth = 뿌리에서 이 객체까지의 단계 수(뿌리 0). visitedCount = 지금까지 들어간 객체 수(잎·멈춤 객체·재방문은 세지 않고, 값 형식은 센다).
        static void Reach<T>(object o, Func<object, bool> stopAt, List<T> found, WalkBudget budget, int depth) where T : class
        {
            if (o == null) return;
            var t = o.GetType();
            if (t.IsPrimitive || t.IsEnum || o is string || o is Delegate || o is System.Reflection.MemberInfo || o is System.Reflection.Pointer) return;
            if (stopAt(o)) return;
            if (!t.IsValueType && !budget.Seen.Add(o)) return;
            budget.Step(depth, t.FullName); // 깊이·방문 수 상한(넘으면 WalkLimitException)
            var hit = o as T;
            if (hit != null) found.Add(hit);
            bool system = t.Namespace == "System" || (t.Namespace != null && t.Namespace.StartsWith("System.", StringComparison.Ordinal));
            if (t.IsArray || system)
            {
                // 배열·System 컬렉션: 원소를 건다(사전은 키·값 둘 다). 컬렉션이 아닌 System 형식은 제네릭(Tuple·KeyValuePair 등 — 다른 객체를 담는다)만 아래에서 필드로 내려간다.
                var dict = o as System.Collections.IDictionary;
                var seq = o as System.Collections.IEnumerable;
                if (dict != null) { foreach (System.Collections.DictionaryEntry e in dict) { Reach(e.Key, stopAt, found, budget, depth + 1); Reach(e.Value, stopAt, found, budget, depth + 1); } return; }
                if (seq != null) { foreach (var x in seq) Reach(x, stopAt, found, budget, depth + 1); return; }
                if (!t.IsGenericType) return;
            }
            for (var ty = t; ty != null && ty != typeof(object); ty = ty.BaseType)
                foreach (var f in ty.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                    Reach(f.GetValue(o), stopAt, found, budget, depth + 1);
        }
        // 그 컴포넌트는 이미 후보를 끄는 사유가 확정됐는가(= 안을 걸을 필요가 없다): 엔진 어셈블리의 형식이 아니거나(BuffProblems 가 사유로 싣는다) 형식 이름이 허용 목록 밖(BuffShapeProblems 가 사유로 싣는다).
        // 걷지 않아도 놓치는 사유는 없다 — 그 후보는 어차피 꺼진다. 걷지 않는 이유: 모르는(다른 모드의) 컴포넌트일수록 깊은 런타임 객체를 쥐고 있을 수 있다.
        // knownForeignAccepted = 알려진 외부 부품 원장으로 받아들인 부품(엔진 것이 아니고 허용 목록에도 없지만 꺼질 사유가 아니다) → 걷는다. 엔진 것이면서 외부 부품일 수는 없다(던진다).
        public static bool BuffComponentAlreadyRejected(bool fromEngineAssembly, bool knownForeignAccepted, string typeName)
        {
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (fromEngineAssembly && knownForeignAccepted) throw new ArgumentException("PackWarden: component " + typeName + " cannot be both an engine type and an accepted known foreign component", "knownForeignAccepted");
            if (knownForeignAccepted) return false;
            return !fromEngineAssembly || Array.IndexOf(AllowedBuffComponentTypes, typeName) < 0;
        }
        // 꺼진 후보가 있어도 선택이 성립하는가: i 번째 선택 레벨에서 고를 수 있는(전제조건 통과 + 켜진) 후보가 i+1 개 이상이어야 그 레벨에서 새로 하나를 고를 수 있다.
        // 거짓이면 레벨업 선택 화면에 고를 것이 없다 → PackEmpower.Build 가 던진다(그때는 직업 등록 자체가 실패하는 편이 낫다).
        public static bool SelectionUsable(ICollection<string> disabledKeys)
        {
            if (disabledKeys == null) throw new ArgumentNullException("disabledKeys");
            foreach (var k in disabledKeys)
                if (Array.FindIndex(Candidates, c => c.Key == k) < 0) throw new ArgumentException("PackWarden: '" + k + "' is not a pack empowerment candidate", "disabledKeys");
            for (int i = 0; i < SelectionLevels.Length; i++)
            {
                int n = 0;
                foreach (var c in Candidates)
                    if (!disabledKeys.Contains(c.Key) && MinHunterLevelFor(c.HunterSpellLevel) <= SelectionLevels[i]) n++;
                if (n < i + 1) return false;
            }
            return true;
        }
        public const string DisabledLogHead = "PackWarden: pack empowerment DISABLED ";
        public const string DisabledKey = "Empower.Disabled";
         
        public const string DisabledKo = "이 주문의 게임 데이터가 이 모드가 아는 모양과 달라(게임 패치나 다른 모드일 수 있다) 이 무리 강화를 쓸 수 없다(모드 로그 참조)";
        public const string DisabledEn = "This spell's game data is not in a shape this mod knows (a game patch or another mod may have changed it), so this pack empowerment is disabled (see the mod log)";

         
        // DataUnsupported = 외부 데이터가 이 모드가 아는 모양이 아니다(예상 가능한 거절: CandidateDataException · 걷기 상한 · 계약/허용 목록 위반 · 원 주문/사슬/아이콘 결측).
        // InternalFailure = 이 모드 코드의 예상 밖 예외(NRE 등) — 원인을 다른 모드로 단정하지 않는다(사용자 문구가 다르고, 오류 로그에 스택 전체를 남긴다).
        public enum DisabledCause { Unknown = 0, DataUnsupported, InternalFailure }
        // 예상 가능한 외부 데이터 거절. 후보 검사(PackEmpower.Analyze 와 그 도우미)가 「데이터 모양이 지원 밖」 일 때만 던진다 — 그 밖의 예외는 내부 실패로 분류된다.
        public sealed class CandidateDataException : Exception
        {
            public CandidateDataException(string message) : base(message) { }
        }
        // 예외 → 원인 종류. MissingMemberException(엔진 API 자체가 다름 — 모든 후보 공통)은 여기 오기 전에 호출부가 다시 던진다 → 넘기면 던진다(호출부 버그).
        // 걷기 상한(WalkLimitException) = 데이터가 너무 크다(또는 자기 자신을 품었다)는 예상 가능한 거절.
        public static DisabledCause ClassifyCandidateFailure(Exception e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (e is MissingMemberException) throw new ArgumentException("PackWarden: an engine API mismatch must stop the build, not disable one candidate", "e", e);
            return e is CandidateDataException || e is WalkLimitException ? DisabledCause.DataUnsupported : DisabledCause.InternalFailure;
        }
        public const string DisabledInternalKey = "Empower.DisabledInternal";
        public const string DisabledInternalKo = "이 모드 내부 오류로 이 무리 강화를 쓸 수 없다(모드 로그의 오류 줄 참조 — 모드 결함일 수 있다)";
        public const string DisabledInternalEn = "This pack empowerment is disabled by an internal error of this mod (see the error line in the mod log; it may be a bug in this mod)";
        // 원인 종류 → 꺼진 후보의 시전 제한 문구 키(Strings 표).
        public static string DisabledTextKey(DisabledCause cause)
        {
            switch (cause)
            {
                case DisabledCause.DataUnsupported: return DisabledKey;
                case DisabledCause.InternalFailure: return DisabledInternalKey;
                default: throw new ArgumentOutOfRangeException("cause", cause, "PackWarden: unclassified disabled cause");
            }
        }

         
        // 엔진 AbilityData.GetUnavailableReason @006C~009C 는 CasterRestrictions 를 순서대로 IsCasterRestrictionPassed(caster) 로 보고 **처음 거짓인 부품의** GetAbilityCasterRestrictionUIText() 를 보여 준다
        // (문구 메서드에는 시전자 인자가 없다). 그래서 문구를 바꾸려고 공유 부품에 「마지막 결과」 를 저장하지 않고, 사유마다 부품을 하나씩 단다 — 부품의 문구는 그 부품의 종류로 고정이다.
        // 순서 = Enabled(꺼진 후보) → TargetsReadable(구성원 판정 불가) → HasTargets(구성원 없음). 모든 부품의 종합(get_CanBeCastByCaster @008D — 하나라도 거짓이면 불가)은 예전 한 부품과 같다.
        public enum RestrictionKind { Unknown = 0, Enabled, TargetsReadable, HasTargets }
        public static readonly RestrictionKind[] RestrictionOrder = { RestrictionKind.Enabled, RestrictionKind.TargetsReadable, RestrictionKind.HasTargets };
        // 그 종류의 부품이 막는가. disabled = 꺼진 후보, targets = 대상 판정(꺼진 후보면 읽지 않아 Unknown 이어도 된다).
        // HasTargets 는 「대상 있음」 이 아니면 전부 막는다(Unreadable 포함) — 같은 프레임 안에서 앞 부품과 판독이 갈려도 시전이 새지 않게(문구만 「없음」 으로 남는 쪽이 시끄럽다).
        public static bool RestrictionBlocks(RestrictionKind kind, bool disabled, TargetState targets)
        {
            switch (kind)
            {
                case RestrictionKind.Enabled: return disabled;
                case RestrictionKind.TargetsReadable:
                    if (disabled) return false; // 꺼진 후보는 Enabled 부품이 막는다
                    if (targets == TargetState.Unknown) throw new ArgumentOutOfRangeException("targets", targets, "PackWarden: an enabled candidate's targets must be classified");
                    return targets == TargetState.Unreadable;
                case RestrictionKind.HasTargets:
                    if (disabled) return false;
                    if (targets == TargetState.Unknown) throw new ArgumentOutOfRangeException("targets", targets, "PackWarden: an enabled candidate's targets must be classified");
                    return targets != TargetState.HasTargets;
                default: throw new ArgumentOutOfRangeException("kind", kind, "PackWarden: unclassified restriction kind");
            }
        }
        public const string TargetsUnreadableKey = "Empower.TargetsUnreadable";
        public const string TargetsUnreadableKo = "무리 구성원을 판정할 수 없다(모드 로그 참조)";
        public const string TargetsUnreadableEn = "The pack members cannot be determined (see the mod log)";

        public const string NotAppliedLogHead = "PackWarden: pack empowerment NOT APPLIED ";
        // 이번 시전의 부여를 입증할 수도 거절로 확정할 수도 없을 때의 머리(BuffReadback.Undetermined — 실패 아님, 문제 수에 넣지 않는다).
        public const string UndeterminedLogHead = "PackWarden: pack empowerment UNDETERMINED ";
        // 액션(PackEmpowerOnPack.RunAction) 자체가 던졌을 때의 머리: 엔진이 그 예외를 게임 로그로만 보내므로 모드 로그에 이 줄을 먼저 남긴다.
        public const string FailedLogHead = "PackWarden: pack empowerment FAILED ";
        public const string DiceLogHead = "PackWarden: pack empowerment dice check ";
        public const string TargetsUnreadableLogHead = "PackWarden: pack empowerment targets UNREADABLE ";

        // ---- 플레이어 문구(한·영). 키 = Strings 표의 키(접두사 없이). ----
        // 키 분리(2026-11-17): 선택지 특성 = ChoiceDescription(레벨업 선택 화면의 상세 창이 읽는 m_Description — 여러 줄), 능력·자원 = Description(짧은 툴팁). 이름은 셋이 같이 쓴다.
        public const string NameSuffix = ".Name", DescriptionSuffix = ".Description", ChoiceDescriptionSuffix = ".ChoiceDescription";
        public static string KeyOf(Candidate c) { if (c == null) throw new ArgumentNullException("c"); return "Empower." + c.Key; }
        // 전체 키는 여기서 만든다(Strings 의 정적 생성자에 ".Name" 리터럴이 생기지 않게 — 시험이 그 리터럴 뒤의 모양을 본다).
        public static string NameKey(Candidate c) { return KeyOf(c) + NameSuffix; }
        public static string DescriptionKey(Candidate c) { return KeyOf(c) + DescriptionSuffix; }
        public static string ChoiceDescriptionKey(Candidate c) { return KeyOf(c) + ChoiceDescriptionSuffix; }
        public static string SelectionNameKey() { return SelectionKey + NameSuffix; }
        public static string SelectionDescriptionKey() { return SelectionKey + DescriptionSuffix; }
        public static string NameKo(Candidate c) { if (c == null) throw new ArgumentNullException("c"); return "무리 강화: " + c.NameKo; }
        public static string NameEn(Candidate c) { if (c == null) throw new ArgumentNullException("c"); return "Pack Empowerment: " + c.NameEn; }

        // ---- 본편 값 사본(선택지 설명문용, 소유자 피드백 2026-11-17 「선택 화면 설명이 한 줄뿐」) ----
        // 무리 강화는 원 주문의 사슬을 그대로 돌리므로 실제 값은 본편 블루프린트가 정한다. 이 파일은 게임 형식이 없어 실행 중에 블루프린트를 읽지 못한다 →
        // 설명문에 적는 값은 아래 사본에서 나오고, 시험(tests\PackRulesTests.cs CheckChoiceText)이 사본을 blueprints.zip · Assembly-CSharp 와 대조한다(어긋나면 실패).
        // 레벨에 따라 달라지는 값과 지속은 시전 뒤 수치 대조(PackEmpower.ExpectedBuffs · ExpectedStats)와 **같은 순수 함수**(RankValue · SharedValue · CasterLevelFor · ExpectedDurationSeconds)로 계산한다.
        // 알려진 한계: 다른 모드가 실행 중에 그 주문·버프의 값을 바꾸면 설명문은 따라가지 않는다(README 「알려진 한계」 ⑦). 그래서 다른 모드가 바꾸는 것으로 확인된 내용은 설명문에 넣지 않았다
        // (FreedomOfMovementUndescribedConditions, Death Ward 의 「이미 받은 음수 레벨」 문장 — TabletopTweaks-Base 2.6.18 PatchFreedomOfMovement · PatchDeathWard).
        // 출처는 전부 blueprints.zip 2026-11-17 직독(경로는 줄마다).
        public const int MagicFangEnhancement = 1;       // Spells/Level1/MagicFangBuff.jbp EquipmentWeaponTypeEnhancement.Enhancement (AllNaturalAndUnarmed = true)
        public const int AbilityEnhancementBonus = 4;    // Spells/Level2/BullsStrengthBuff · CatsGraceBuff · BearsEnduranceBuff.jbp AddStatBonus.Value (Descriptor = Enhancement, Stat = Strength · Dexterity · Constitution)
        // Spells/Level2/BarkskinBuff.jbp ContextRankConfig(Default, CasterLevel): 1 + 시전자 레벨 ÷ 3, 상한 5 → AddContextStatBonus(AC, NaturalArmorEnhancement, Multiplier 1).
        public static readonly RankSpec BarkskinArmorRank = new RankSpec(ProgressionStartPlusDivStep, 0, 3, false, 0, true, 5);
        // Assembly-CSharp Kingmaker.UnitLogic.FactLogic.AddCumulativeDamageBonus.OnEventDidTrigger IL @003F~0046(Bonus = Bonus + 1; 명중할 때마다) — Spells/Level2/NaturalRythmBuff.jbp 의 부품(OnlyNaturalAttacks = true). 2026-11-17 직독.
        public const int NaturalRhythmDamageStep = 1;
        public const int NaturalRhythmResetValue = 0;    // 같은 메서드 IL @004C~0053 · OnEventAboutToTrigger @0050~0057 · @007E~0085(Bonus = 0: 빗나감 · 첫 공격 · 대상 변경 · 전력 공격이 아님)
        // Spells/Level3/MagicFangGreater.jbp ContextRankConfig(StatBonus, CasterLevel): 1 + (시전자 레벨 − 4) ÷ 4, 1~5 → ContextCalculateSharedValue(StatBonus, Modifier 1.0) → 단계 N 가지가 +N 버프(MagicFangGreaterBuffN)를 건다.
        public static readonly RankSpec GreaterFangTierRank = new RankSpec(ProgressionStartPlusDivStep, 4, 4, true, 1, true, 5);
        public const double GreaterFangTierModifier = 1.0;
        // Spells/Level5/BlessingOfTheSalamanderBuff.jbp AddEffectFastHealing.Heal · AddDamageResistanceEnergy(Fire).Value · AddStatBonus(AdditionalCMD, Competence).Value
        public const int SalamanderFastHealing = 5, SalamanderFireResistance = 20, SalamanderCmdBonus = 2;
        public const int DeathWardSaveBonus = 4;         // Spells/Level4/DeathWardBuff.jbp SavingThrowBonusAgainstDescriptor.Value (ModifierDescriptor = Morale, SpellDescriptor = Death)
        // Spells/Level4/FreedomOfMovementBuff.jbp AddConditionImmunity.Condition 중 설명문에 적는 것(이름 = Kingmaker UnitCondition). 설명문의 낱말 순서와 같다.
        public static readonly string[] FreedomOfMovementConditions = { "Paralyzed", "Slowed", "Entangled", "DifficultTerrain", "CantMove" };
        // 같은 버프에 있지만 설명문에 적지 않는 것: TabletopTweaks-Base 2.6.18 PatchFreedomOfMovement(RemoveStaggerImmunity)가 이 면역을 지운다 — 설치 여부에 따라 달라지는 내용은 적지 않는다.
        public static readonly string[] FreedomOfMovementUndescribedConditions = { "Staggered" };
        // 지속: 후보 10종의 사슬은 전부 ContextActionApplyBuff.DurationValue = 랭크(Default) × 단위이고, Default 랭크는 시전자 레벨 그대로다
        // (ContextRankConfig(Default, CasterLevel, AsIs) 또는 설정 없음 = 엔진 기본 시전자 레벨 — PackEmpower.RankOf 의 주석). 단위 이름 = Kingmaker DurationRate.
        public static readonly RankSpec DurationRank = new RankSpec(ProgressionAsIs, 0, 0, false, 0, false, 0);
        static readonly KeyValuePair<string, string>[] DurationRates =
        {
            new KeyValuePair<string, string>("MagicFang", "Minutes"), new KeyValuePair<string, string>("BullsStrength", "Minutes"), new KeyValuePair<string, string>("CatsGrace", "Minutes"),
            new KeyValuePair<string, string>("BearsEndurance", "Minutes"), new KeyValuePair<string, string>("Barkskin", "TenMinutes"), new KeyValuePair<string, string>("NaturalRhythm", "Rounds"),
            new KeyValuePair<string, string>("MagicFangGreater", "Hours"), new KeyValuePair<string, string>("FreedomOfMovement", "TenMinutes"), new KeyValuePair<string, string>("BlessingOfTheSalamander", "Rounds"),
            new KeyValuePair<string, string>("DeathWard", "Minutes"),
        };
        // 사본에 없는 후보는 던진다(기본 단위로 넘기지 않는다).
        public static string DurationRateOf(Candidate c)
        {
            if (c == null) throw new ArgumentNullException("c");
            foreach (var r in DurationRates) if (r.Key == c.Key) return r.Value;
            throw new InvalidOperationException("PackWarden: no duration rate copy for pack empowerment " + c.Key);
        }
        // 설명문의 값 — 전부 시전 뒤 수치 대조와 같은 순수 함수를 지난다.
        public static long TextDurationSeconds(Candidate c, int hunterLevel) { return ExpectedDurationSeconds(RankValue(DurationRank, CasterLevelFor(hunterLevel)), DurationRateOf(c)); }
        public static int BarkskinArmorAt(int hunterLevel) { return RankValue(BarkskinArmorRank, CasterLevelFor(hunterLevel)); }
        public static int GreaterFangEnhancementAt(int hunterLevel) { return SharedValue(RankValue(GreaterFangTierRank, CasterLevelFor(hunterLevel)), GreaterFangTierModifier); }

        // ---- 필수 효과 계약(검토 2026-11-17 지적 1 — 「설명문의 본편 값 사본이 실제 블루프린트와 조용히 다를 수 있다」 를 닫는다) ----
        // 설명문(EffectKo/En)이 적는 효과마다 「그 효과를 만드는 부품과 필드 값」 을 아래 원장에 적는다. 기대값은 **위의 설명문 사본 상수에서 그대로** 온다(같은 원장 — 설명과 계약이 따로 놀 수 없다).
        // 블루프린트 조립 때(PackEmpower.BuildCandidate) 후보마다 실제 사슬·버프의 부품을 읽어 이 계약과 대조하고(ContractProblems — 순수, 시험이 값으로 실행),
        // 하나라도 어긋나면(부품이 없음 · 같은 열쇠의 부품이 둘 이상 · 필드 값이 다름 · 필드를 읽지 못함) 그 후보를 끈다(사유: 설명과 실제가 다름).
        // 엔진 BlueprintScriptableObject.get_ComponentsArray IL @0002~0013 은 null 배열을 빈 배열로 바꾼다 → 부품이 없으면 「찾지 못함」 으로 실패한다(빈 목록을 통과로 읽지 않는다).
        // 오프라인 시험(tests\PackRulesTests.cs CheckChoiceText ③)이 같은 원장을 blueprints.zip 에 돌려 문제 0 을 단언하고, 필드 경로가 엔진 형식에 있는지 dnlib 로 대조한다.
        // 알려진 한계: 「필요한 효과가 있고 값이 같다」 만 본다 — 원장에 없는 부품이 새로 붙는 것은 허용 목록(AllowedBuffComponentTypes)의 몫이다(같은 형식이면 열쇠 중복으로 잡힌다).
        //
        // 주문 서술자 값(Kingmaker.Blueprints.Classes.Spells.SpellDescriptor 의 상수 — Assembly-CSharp 2026-11-17 직독, 시험이 dnlib 로 대조한다).
        public const long SpellDescriptorParalysis = 33554432L;                // 1 << 25
        public const long SpellDescriptorDeath = 536870912L;                   // 1 << 29
        public const long SpellDescriptorMovementImpairing = 562949953421312L; // 1 << 49
        public const long SpellDescriptorChannelNegativeHarm = 72057594037927936L; // 1 << 56
        public enum ContractTarget { Unknown = 0, Chain, ExpectedBuff }
         
        //   참조(BlueprintReferenceBase): 필드가 null 이거나 guid 가 비었다(엔진 BlueprintReferenceBase.get_Guid @0001 = deserializedGuid; 비지 않은 guid 는 해석되든 안 되든 「있음」 — 조립 뒤에 생기는 블루프린트일 수 있다).
        //   조건 계산기(PropertyCalculator): null 이거나 Getters 가 0개(엔진 RestrictionCalculator.IsPassedInternal @0001~0024: null·Empty 면 통과, PropertyCalculator.get_Empty @0001~000B = Getters.Length < 1).
        public const string PresenceNone = "none";
        // BlueprintComponent.m_Flags: 정의된 비트는 Disabled = 1 하나다(Assembly-CSharp BlueprintComponent/Flags — 시험이 dnlib 로 대조). 그래서 「정확히 0」 = 「Disabled 꺼짐」.
        // Disabled 인 부품은 사실(버프)이 붙을 때 런타임 부품이 만들어지지 않는다(EntityFact.Setup @005D~0062 get_Disabled → 건너뜀) — 계약 값이 맞아도 효과가 없다.
        // 이 검사가 닿는 형식 = IRuntimeEntityFactComponentProvider 를 구현한 부품(EntityFact.Setup @0035 isinst). ContextRankConfig · ContextCalculateSharedValue 는 아니다(get_Disabled 호출자 전수: EntityFact 3곳 + 검증기 — 2026-11-17 dnlib).
        public const int ComponentFlagsNone = 0;
        public const string ComponentFlagsPath = "m_Flags";
        // 필드 하나의 기대: Path = 부품에서 필드 이름을 '.' 로 이은 경로(직렬화 이름 = C# 필드 이름), 기대값 = 헌터 레벨의 함수(대부분 상수), FlagMask ≠ 0 이면 「그 비트를 전부 가짐」,
        // ExpectNone = 「없음」 판독(PresenceNone — 위 주석)으로 비교.
        public sealed class ContractField
        {
            public readonly string Path; public readonly long FlagMask; public readonly bool ExpectNone; readonly Func<int, object> expected;
            ContractField(string path, Func<int, object> expected, long flagMask, bool expectNone)
            {
                if (string.IsNullOrEmpty(path)) throw new ArgumentException("PackWarden: contract field without a path", "path");
                if (expected == null && flagMask == 0 && !expectNone) throw new ArgumentException("PackWarden: contract field " + path + " has no expectation", "expected");
                Path = path; this.expected = expected; FlagMask = flagMask; ExpectNone = expectNone;
            }
            public static ContractField Eq(string path, object value) { if (value == null) throw new ArgumentNullException("value"); return new ContractField(path, hl => value, 0, false); }
            public static ContractField EqAt(string path, Func<int, object> valueAtHunterLevel) { if (valueAtHunterLevel == null) throw new ArgumentNullException("valueAtHunterLevel"); return new ContractField(path, valueAtHunterLevel, 0, false); }
            public static ContractField HasFlags(string path, long mask) { if (mask == 0) throw new ArgumentOutOfRangeException("mask", mask, "PackWarden: an empty flag mask checks nothing"); return new ContractField(path, null, mask, false); }
            public static ContractField None(string path) { return new ContractField(path, null, 0, true); }
            public string ExpectedText(int hunterLevel) { return ExpectNone ? PresenceNone : FlagMask != 0 ? "all bits of " + FlagMask.ToString(System.Globalization.CultureInfo.InvariantCulture) : CanonicalText(expected(hunterLevel)); }
            // 이 필드의 판독: 「없음」 기대면 ReadPresence, 아니면 Read(둘 다 못 읽으면 던진다 — 호출부가 「읽지 못함」 문제로 싣는다).
            public string ReadFrom(ComponentReading c) { if (c == null) throw new ArgumentNullException("c"); return ExpectNone ? c.ReadPresence(Path) : c.Read(Path); }
            public bool Matches(string actual, int hunterLevel)
            {
                if (actual == null) throw new ArgumentNullException("actual");
                if (ExpectNone) return actual == PresenceNone;
                if (FlagMask == 0) return actual == CanonicalText(expected(hunterLevel));
                long v;
                return long.TryParse(actual, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out v) && (v & FlagMask) == FlagMask;
            }
        }
        // 계약 한 행: 그 후보의 대상(사슬 능력 / 그 레벨에서 사슬이 거는 기대 버프 각각)에 형식 이름이 ComponentType 이고 Keys 가 전부 맞는 부품이 **정확히 하나** 있고, 그 부품의 Values 가 전부 맞아야 한다.
        public sealed class EffectContract
        {
            public readonly string Candidate, ComponentType; public readonly ContractTarget Target; public readonly ContractField[] Keys, Values;
            public EffectContract(string candidate, ContractTarget target, string componentType, ContractField[] keys, ContractField[] values)
            {
                if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(componentType)) throw new ArgumentException("PackWarden: effect contract without a candidate or component type");
                if (target != ContractTarget.Chain && target != ContractTarget.ExpectedBuff) throw new ArgumentOutOfRangeException("target", target, "PackWarden: effect contract target");
                if (keys == null || values == null) throw new ArgumentNullException("keys");
                Candidate = candidate; Target = target; ComponentType = componentType; Keys = (ContractField[])keys.Clone(); Values = (ContractField[])values.Clone();
            }
            public string Describe(int hunterLevel)
            {
                var k = new List<string>();
                foreach (var f in Keys) k.Add(f.Path + "=" + f.ExpectedText(hunterLevel));
                return ComponentType + (k.Count == 0 ? "" : "{" + string.Join(", ", k.ToArray()) + "}");
            }
        }
        static ContractField[] F(params ContractField[] f) { return f; }
        static ContractField Eq(string path, object value) { return ContractField.Eq(path, value); }
        static ContractField None(string path) { return ContractField.None(path); }
         
        //   EquipmentWeaponTypeEnhancement.IsStackable: get_Descriptor @0001~000D(거짓 = Enhancement(9) 종류 — 다른 강화와 겹치지 않음, 참 = UntypedStackable(25)). Spells/Level1/MagicFangBuff.jbp · Level3/MagicFangGreaterBuff1~5.jbp = false.
        public const bool FangEnhancementIsStackable = false;
        //   AddDamageResistanceEnergy.HealOnDamage: AddDamageResistanceEnergyRuntime.OnEventDidTrigger @000D(참이면 막은 피해로 치유 — 설명에 없는 효과). Spells/Level5/BlessingOfTheSalamanderBuff.jbp = false.
        public const bool SalamanderHealOnDamage = false;
        //   SavingThrowBonusAgainstDescriptor.OnlyPositiveValue: OnEventAboutToTrigger @0059~0068(참이면 「계산값 > Value」 일 때만 — 랭크 1 에서 4 > 4 거짓 → +4 가 붙지 않는다). Spells/Level4/DeathWardBuff.jbp = false.
        public const bool DeathWardOnlyPositiveValue = false;
        //   ContextCalculateSharedValue.Value(주사위 값): Calculate @0001~0015 = Value.Calculate × Modifier. Spells/Level3/MagicFangGreater.jbp Value = { DiceType Zero, BonusValue = Rank(StatBonus) }.
        public const string GreaterFangSharedDice = "Zero", GreaterFangSharedBonusType = "Rank", GreaterFangSharedBonusRank = "StatBonus";
        // 랭크 설정 사본(RankSpec) → 계약 필드(설명문의 랭크와 같은 사본에서 — 시험의 sameRank 와 같은 필드: 최소·최대는 쓰일 때만 값).
        static EffectContract RankContract(string candidate, ContractTarget target, string rankType, RankSpec s)
        {
            var v = new List<ContractField> { Eq("m_BaseValueType", "CasterLevel"), Eq("m_Progression", s.Progression), Eq("m_StartLevel", s.Start), Eq("m_StepLevel", s.Step), Eq("m_UseMin", s.UseMin), Eq("m_UseMax", s.UseMax) };
            if (s.UseMin) v.Add(Eq("m_Min", s.Min));
            if (s.UseMax) v.Add(Eq("m_Max", s.Max));
            return new EffectContract(candidate, target, "ContextRankConfig", F(Eq("m_Type", rankType)), v.ToArray());
        }
        // 계약 부품 중 런타임 부품이 아닌 형식(IRuntimeEntityFactComponentProvider 미구현 — m_Flags 가 효과에 닿지 않는다, ComponentFlagsNone 주석). 시험이 dnlib 로 대조한다.
        // (EffectContracts 보다 먼저 선언 — 정적 필드 초기화는 글 순서다.)
        static readonly string[] NonRuntimeContractComponentTypes = { "ContextRankConfig", "ContextCalculateSharedValue" };
         
        // Freedom of Movement 7(상태 면역 5 + 붙잡기 + 서술자 면역) · Salamander 3 · Death Ward 4 = 24. 행을 더하거나 빼면 이 수와 시험의 표를 같이 고친다(BuildEffectContracts 가 어긋나면 던진다).
        public const int EffectContractRowCount = 24;
        public static readonly EffectContract[] EffectContracts = BuildEffectContracts();
        static EffectContract[] BuildEffectContracts()
        {
            var B = ContractTarget.ExpectedBuff; var C = ContractTarget.Chain;
            var rows = new List<EffectContract>();
            // 마법 송곳니: 자연 무기·비무장 전부에 +MagicFangEnhancement.
            rows.Add(new EffectContract("MagicFang", B, "EquipmentWeaponTypeEnhancement", F(), F(Eq("Enhancement", MagicFangEnhancement), Eq("AllNaturalAndUnarmed", true), Eq("IsStackable", FangEnhancementIsStackable))));
            // 능력치 3종: 그 능력치에 +AbilityEnhancementBonus 강화(기본 공격 보너스 배율 없음). 열쇠 = 능력치 — 같은 능력치에 둘째 AddStatBonus 가 붙으면 모호(실패).
            foreach (var kv in new[] { new[] { "BullsStrength", "Strength" }, new[] { "CatsGrace", "Dexterity" }, new[] { "BearsEndurance", "Constitution" } })
                rows.Add(new EffectContract(kv[0], B, "AddStatBonus", F(Eq("Stat", kv[1])), F(Eq("Descriptor", "Enhancement"), Eq("Value", AbilityEnhancementBonus), Eq("ScaleByBasicAttackBonus", false))));
            // 나무껍질: 방어도에 랭크(Default) × 1 의 자연 방어 강화, 랭크 = BarkskinArmorRank.
            rows.Add(new EffectContract("Barkskin", B, "AddContextStatBonus", F(Eq("Stat", "AC")), F(Eq("Descriptor", "NaturalArmorEnhancement"), Eq("Multiplier", 1), Eq("HasMinimal", false), Eq("Value.ValueType", "Rank"), Eq("Value.ValueRank", "Default"),
                None("Restrictions.Property")))); // 조건: AddContextStatBonus.OnActivate @0001~0031 · OnTurnOn @0001~0031 — Restrictions.IsPassed 거짓이면 보너스를 붙이지 않는다
            rows.Add(RankContract("Barkskin", B, "Default", BarkskinArmorRank));
            // 자연의 리듬: 누적 피해 부품(자연 공격만). 증가·초기화 값은 엔진 코드(시험이 IL 로 대조).
            rows.Add(new EffectContract("NaturalRhythm", B, "AddCumulativeDamageBonus", F(), F(Eq("OnlyNaturalAttacks", true))));
            // 마법 송곳니, 상급: 그 레벨에 사슬이 거는 버프의 강화 = GreaterFangEnhancementAt(헌터 레벨), 단계 랭크·공유값 배수 = 사본.
            rows.Add(new EffectContract("MagicFangGreater", B, "EquipmentWeaponTypeEnhancement", F(), F(ContractField.EqAt("Enhancement", hl => GreaterFangEnhancementAt(hl)), Eq("AllNaturalAndUnarmed", true), Eq("IsStackable", FangEnhancementIsStackable))));
            rows.Add(RankContract("MagicFangGreater", C, "StatBonus", GreaterFangTierRank));
            rows.Add(new EffectContract("MagicFangGreater", C, "ContextCalculateSharedValue", F(Eq("ValueType", "StatBonus")), F(Eq("Modifier", GreaterFangTierModifier),
                Eq("Value.DiceType", GreaterFangSharedDice), Eq("Value.BonusValue.ValueType", GreaterFangSharedBonusType), Eq("Value.BonusValue.ValueRank", GreaterFangSharedBonusRank))));
            // 이동의 자유: 설명에 적은 상태 면역(열쇠 = 상태), 붙잡기 면역, 이동 방해·마비 서술자 면역(조건 없음).
            foreach (var cond in FreedomOfMovementConditions) rows.Add(new EffectContract("FreedomOfMovement", B, "AddConditionImmunity", F(Eq("Condition", cond)), F()));
            rows.Add(new EffectContract("FreedomOfMovement", B, "ManeuverImmunity", F(Eq("Type", "Grapple")), F()));
            rows.Add(new EffectContract("FreedomOfMovement", B, "BuffDescriptorImmunity", F(), F(ContractField.HasFlags("Descriptor.m_IntValue", SpellDescriptorMovementImpairing | SpellDescriptorParalysis), Eq("CheckFact", false),
                None("m_IgnoreFeature")))); // IsImmune @0043~006C: 시전자가 m_IgnoreFeature 를 가지면 면역이 아니다(m_FactToCheck 는 CheckFact 참일 때만 — @006E~0073)
            // 샐러맨더의 축복: 빠른 치유 · 화염 저항(고정값, 풀 없음) · 전법 방어 능숙 보너스.
            rows.Add(new EffectContract("BlessingOfTheSalamander", B, "AddEffectFastHealing", F(), F(Eq("Heal", SalamanderFastHealing), Eq("Bonus.ValueType", "Simple"), Eq("Bonus.Value", 0))));
            rows.Add(new EffectContract("BlessingOfTheSalamander", B, "AddDamageResistanceEnergy", F(Eq("Type", "Fire")), F(Eq("Value.ValueType", "Simple"), Eq("Value.Value", SalamanderFireResistance), Eq("UsePool", false), Eq("UseValueMultiplier", false),
                Eq("HealOnDamage", SalamanderHealOnDamage))));
            rows.Add(new EffectContract("BlessingOfTheSalamander", B, "AddStatBonus", F(Eq("Stat", "AdditionalCMD")), F(Eq("Descriptor", "Competence"), Eq("Value", SalamanderCmdBonus), Eq("ScaleByBasicAttackBonus", false))));
            // 죽음 방호: 죽음 효과 내성 +DeathWardSaveBonus 사기, 음에너지 면역, 원기 소진 면역, 해로운 음에너지 전달 면역.
            rows.Add(new EffectContract("DeathWard", B, "SavingThrowBonusAgainstDescriptor", F(), F(Eq("Value", DeathWardSaveBonus), Eq("ModifierDescriptor", "Morale"), Eq("SpellDescriptor.m_IntValue", SpellDescriptorDeath), Eq("Bonus.ValueType", "Simple"), Eq("Bonus.Value", 0),
                Eq("OnlyPositiveValue", DeathWardOnlyPositiveValue), None("m_DisablingFeature")))); // OnEventAboutToTrigger @0071~0087: 소유자가 m_DisablingFeature 를 가지면 보너스 없음
            rows.Add(new EffectContract("DeathWard", B, "AddEnergyImmunity", F(Eq("Type", "NegativeEnergy")), F()));
            rows.Add(new EffectContract("DeathWard", B, "AddImmunityToEnergyDrain", F(), F()));
            rows.Add(new EffectContract("DeathWard", B, "SpellImmunityToSpellDescriptor", F(Eq("Descriptor.m_IntValue", SpellDescriptorChannelNegativeHarm)),
                F(None("m_CasterIgnoreImmunityFact")))); // OnTurnOn @001F~0025: 이 사실을 가진 시전자에게는 면역이 무시된다
            // 모든 런타임 부품 행(IRuntimeEntityFactComponentProvider 형식 = 버프 쪽에서 ContextRankConfig 를 뺀 것)에 m_Flags = 0(Disabled 꺼짐) — ComponentFlagsNone 주석.
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Target != B || Array.IndexOf(NonRuntimeContractComponentTypes, r.ComponentType) >= 0) continue;
                var v = new List<ContractField>(r.Values) { Eq(ComponentFlagsPath, ComponentFlagsNone) };
                rows[i] = new EffectContract(r.Candidate, r.Target, r.ComponentType, r.Keys, v.ToArray());
            }
            if (rows.Count != EffectContractRowCount) throw new InvalidOperationException("PackWarden: the effect contract has " + rows.Count + " rows, EffectContractRowCount says " + EffectContractRowCount);
            // 후보마다 계약이 있어야 한다(설명문에 효과 줄이 있으면 그것을 만드는 부품도 있어야 한다 — 행이 없으면 대조 없이 통과하므로 던진다).
            foreach (var c in Candidates)
                if (!rows.Exists(r => r.Candidate == c.Key)) throw new InvalidOperationException("PackWarden: pack empowerment " + c.Key + " has no effect contract (its description would go unchecked)");
            foreach (var r in rows)
                if (Array.FindIndex(Candidates, c => c.Key == r.Candidate) < 0) throw new InvalidOperationException("PackWarden: effect contract for unknown candidate " + r.Candidate);
            return rows.ToArray();
        }
        // 대조용 부품 한 개: 형식 이름(짧은 이름 — 엔진 어셈블리의 부품만 넘긴다)과 경로 → 정규화한 값 문자열을 돌려주는 읽기(못 읽으면 던진다 — 기본값을 만들지 않는다).
        // readPresence = 「없음」 판독(경로 → PresenceNone 또는 있는 것의 설명). 경로의 마지막 필드는 null 이어도 된다(그것이 「없음」), 중간이 끊기거나 필드가 없으면 던진다.
        public sealed class ComponentReading
        {
            public readonly string TypeName; readonly Func<string, string> read, readPresence;
            public ComponentReading(string typeName, Func<string, string> read, Func<string, string> readPresence)
            {
                if (string.IsNullOrEmpty(typeName)) throw new ArgumentException("PackWarden: component reading without a type name", "typeName");
                if (read == null) throw new ArgumentNullException("read");
                if (readPresence == null) throw new ArgumentNullException("readPresence");
                TypeName = typeName; this.read = read; this.readPresence = readPresence;
            }
            public string Read(string path)
            {
                string v = read(path);
                if (v == null) throw new InvalidOperationException("PackWarden: " + TypeName + "." + path + " read as null");
                return v;
            }
            public string ReadPresence(string path)
            {
                string v = readPresence(path);
                if (string.IsNullOrEmpty(v)) throw new InvalidOperationException("PackWarden: " + TypeName + "." + path + " presence read as empty");
                return v;
            }
        }
        // 값 → 계약 비교용 문자열(게임 쪽 리플렉션 값과 시험의 JSON 값이 같은 문자열이 되게): bool = true/false, 정수 = 십진, 실수 = 왕복 표기, 열거 = 이름, 문자열 = 그대로. 그 밖은 던진다.
        public static string CanonicalText(object v)
        {
            if (v == null) throw new ArgumentNullException("v", "PackWarden: a contract value is missing");
            if (v is bool) return (bool)v ? "true" : "false";
            if (v is string) return (string)v;
            if (v is Enum) return v.ToString();
            if (v is double || v is float || v is decimal) return Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            if (v is int || v is long || v is short || v is byte || v is sbyte || v is uint || v is ushort) return Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (v is ulong) return ((ulong)v).ToString(System.Globalization.CultureInfo.InvariantCulture);
            throw new NotSupportedException("PackWarden: contract value of type " + v.GetType().FullName + " has no canonical text");
        }
        public static int ContractRowCount(string candidateKey, ContractTarget target)
        {
            int n = 0;
            foreach (var r in EffectContracts) if (r.Candidate == candidateKey && r.Target == target) n++;
            return n;
        }
        // 그 후보 · 그 대상의 계약을 부품 목록과 대조한다(빈 목록 = 문제 없음). targetName = 문구에 적을 블루프린트 이름, hunterLevel = 레벨 함수 기대값의 입력.
        public static List<string> ContractProblems(string candidateKey, ContractTarget target, string targetName, IList<ComponentReading> components, int hunterLevel)
        {
            if (string.IsNullOrEmpty(candidateKey)) throw new ArgumentException("PackWarden: candidate key is missing", "candidateKey");
            if (string.IsNullOrEmpty(targetName)) throw new ArgumentException("PackWarden: contract target name is missing", "targetName");
            if (components == null) throw new ArgumentNullException("components");
            if (target != ContractTarget.Chain && target != ContractTarget.ExpectedBuff) throw new ArgumentOutOfRangeException("target", target, "PackWarden: effect contract target");
            if (Array.FindIndex(Candidates, c => c.Key == candidateKey) < 0) throw new ArgumentException("PackWarden: '" + candidateKey + "' is not a pack empowerment candidate", "candidateKey");
            var problems = new List<string>();
            string where = targetName + " (hunter level " + hunterLevel + ")";
            foreach (var row in EffectContracts)
            {
                if (row.Candidate != candidateKey || row.Target != target) continue;
                var matching = new List<ComponentReading>();
                bool keyUnreadable = false;
                foreach (var comp in components)
                {
                    if (comp == null) throw new ArgumentNullException("components", "PackWarden: null component reading");
                    if (comp.TypeName != row.ComponentType) continue;
                    bool ok = true;
                    foreach (var key in row.Keys)
                    {
                        string v;
                        try { v = key.ReadFrom(comp); }
                        catch (Exception e) { problems.Add(where + ": " + row.ComponentType + "." + key.Path + " could not be read: " + e.GetType().Name + ": " + e.Message); keyUnreadable = true; ok = false; break; }
                        if (!key.Matches(v, hunterLevel)) { ok = false; break; }
                    }
                    if (ok) matching.Add(comp);
                }
                if (keyUnreadable) continue; // 열쇠를 못 읽은 부품이 있으면 「몇 개가 맞는가」 를 셀 수 없다 — 위에서 이미 문제로 실었다
                if (matching.Count == 0) { problems.Add(where + ": no " + row.Describe(hunterLevel) + " (the description states this effect)"); continue; }
                if (matching.Count > 1) { problems.Add(where + ": " + matching.Count + " components match " + row.Describe(hunterLevel) + " (exactly one is expected; the description cannot tell which applies)"); continue; }
                foreach (var f in row.Values)
                {
                    string v;
                    try { v = f.ReadFrom(matching[0]); }
                    catch (Exception e) { problems.Add(where + ": " + row.Describe(hunterLevel) + "." + f.Path + " could not be read: " + e.GetType().Name + ": " + e.Message); continue; }
                    if (!f.Matches(v, hunterLevel)) problems.Add(where + ": " + row.Describe(hunterLevel) + "." + f.Path + " is " + v + ", the description says " + f.ExpectedText(hunterLevel));
                }
            }
            return problems;
        }
        // 지속: 그 레벨에 사슬이 실제로 거는 버프의 예정 지속(초) ↔ 설명문의 지속(TextDurationSeconds — DurationRateOf 사본). 같으면 null.
        public static string DurationContractProblem(Candidate c, int hunterLevel, string buffName, long actualSeconds)
        {
            if (c == null) throw new ArgumentNullException("c");
            if (string.IsNullOrEmpty(buffName)) throw new ArgumentException("PackWarden: buff name is missing", "buffName");
            long want = TextDurationSeconds(c, hunterLevel);
            return want == actualSeconds ? null : buffName + " (hunter level " + hunterLevel + "): duration is " + (actualSeconds == PermanentDuration ? "permanent" : actualSeconds + " s") + ", the description says " + want + " s";
        }
        public const string ContractMismatchHead = "the description and the game data differ: ";

        // 같은 값이 이어지는 헌터 레벨 구간(from..to, 둘 다 포함). 설명문의 「a~b레벨 +n」 은 이 구간에서 나온다.
        public sealed class LevelBand
        {
            public readonly int From, To, Value;
            public LevelBand(int from, int to, int value) { From = from; To = to; Value = value; }
        }
        public static List<LevelBand> LevelBands(Func<int, int> valueAt, int from, int to)
        {
            if (valueAt == null) throw new ArgumentNullException("valueAt");
            if (from < 1 || to < from) throw new ArgumentOutOfRangeException("to", to, "PackWarden: level range must be 1 <= from <= to (from " + from + ")");
            var bands = new List<LevelBand>();
            int start = from, value = valueAt(from);
            for (int lv = from + 1; lv <= to; lv++)
            {
                int v = valueAt(lv);
                if (v == value) continue;
                bands.Add(new LevelBand(start, lv - 1, value));
                start = lv; value = v;
            }
            bands.Add(new LevelBand(start, to, value));
            return bands;
        }
        static string BandsKo(List<LevelBand> bands)
        {
            var parts = new List<string>();
            foreach (var b in bands) parts.Add((parts.Count == 0 ? "헌터 " : "") + (b.From == b.To ? b.From.ToString() : b.From + "~" + b.To) + "레벨 +" + b.Value);
            return string.Join(", ", parts.ToArray());
        }
        static string BandsEn(List<LevelBand> bands)
        {
            var parts = new List<string>();
            foreach (var b in bands) parts.Add("+" + b.Value + " at " + (parts.Count == 0 ? "hunter " : "") + (b.From == b.To ? "level " + b.From : "levels " + b.From + "-" + b.To));
            return string.Join(", ", parts.ToArray());
        }
        // 지속 표기 단위(초, 한글 단위, 영어 단수형). 「레벨당 10분」 단위(TenMinutes)는 분으로 적는다. 모르는 단위는 던진다.
        static int DisplayUnit(string rate, out string ko, out string en)
        {
            switch (rate)
            {
                case "Rounds": ko = "라운드"; en = "round"; return SecondsPerRound;
                case "Minutes":
                case "TenMinutes": ko = "분"; en = "minute"; return RoundsPerRate("Minutes") * SecondsPerRound;
                case "Hours": ko = "시간"; en = "hour"; return RoundsPerRate("Hours") * SecondsPerRound;
                default: throw new NotSupportedException("PackWarden: no display unit for duration rate '" + rate + "'");
            }
        }
        static string DurationText(long seconds, string rate, bool korean)
        {
            string ko, en;
            int unit = DisplayUnit(rate, out ko, out en);
            if (seconds <= 0 || seconds % unit != 0) throw new InvalidOperationException("PackWarden: duration " + seconds + " s is not a whole positive number of " + en + "s");
            long n = seconds / unit;
            return korean ? n + ko : n + " " + en + (n == 1 ? "" : "s");
        }
        // 행동·능력 형식의 표기(한글 패치 표기 「이동 행동」·「주문 유사 능력」). 상수(ActionTypeName · AbilityTypeName)가 바뀌면 문구를 고치도록 모르는 값은 던진다.
        static string ActionText(string actionTypeName, bool korean)
        {
            if (actionTypeName == "Move") return korean ? "이동 행동" : "move action";
            throw new NotSupportedException("PackWarden: no player text for action type '" + actionTypeName + "'");
        }
        static string AbilityTypeText(string abilityTypeName, bool korean)
        {
            if (abilityTypeName == "SpellLike") return korean ? "주문 유사 능력" : "spell-like ability";
            throw new NotSupportedException("PackWarden: no player text for ability type '" + abilityTypeName + "'");
        }
        static int MinLevel(Candidate c) { if (c == null) throw new ArgumentNullException("c"); return MinHunterLevelFor(c.HunterSpellLevel); }

        // ---- 설명문의 줄(한·영 같은 순서·같은 수치). 선택지 = 6줄, 능력 툴팁 = 앞의 3줄 + 사용 1줄. ----
        static string WhatKo(Candidate c) { return "주 동물 동료와 살아 있는 보조 동물 전체에 「" + c.NameKo + "」(" + c.NameEn + ") 주문의 효과를 한 번에 건다."; }
        static string WhatEn(Candidate c) { return "Applies the effect of the " + c.NameEn + " spell to the main animal companion and every living auxiliary animal at once."; }
        // 효과 줄: 후보마다 다르다. 수치는 위 사본·순수 함수에서만 온다(틀에 숫자를 직접 적지 않는다). 사본이 없는 후보는 던진다.
        public static string EffectKo(Candidate c)
        {
            int from = MinLevel(c), to = PackRules.MaxHunterLevel;
            const string fang = "자연 무기와 비무장 타격의 명중 굴림과 피해 굴림에 ";
            const string noStack = " 강화 보너스. 같은 능력치의 다른 강화 보너스와는 중첩되지 않는다.";
            switch (c.Key)
            {
                case "MagicFang": return "효과: " + fang + "+" + MagicFangEnhancement + " 강화 보너스.";
                case "BullsStrength": return "효과: 힘에 +" + AbilityEnhancementBonus + noStack;
                case "CatsGrace": return "효과: 민첩에 +" + AbilityEnhancementBonus + noStack;
                case "BearsEndurance": return "효과: 건강에 +" + AbilityEnhancementBonus + noStack;
                case "Barkskin": return "효과: 방어도에 자연 방어 강화 보너스. " + BandsKo(LevelBands(BarkskinArmorAt, from, to)) + ". 원래의 자연 방어와는 중첩되고, 다른 자연 방어 강화 보너스와는 중첩되지 않는다.";
                case "NaturalRhythm": return "효과: 전력 공격 중에 자연 무기로 같은 대상을 연속으로 명중시킬 때마다, 그 전력 공격의 다음 자연 무기 공격 피해가 +" + NaturalRhythmDamageStep + "씩 커진다. 빗나가거나 대상을 바꾸면 " + NaturalRhythmResetValue + "부터 다시 쌓이고, 기회 공격에는 적용되지 않는다.";
                case "MagicFangGreater": return "효과: " + fang + "강화 보너스. " + BandsKo(LevelBands(GreaterFangEnhancementAt, from, to)) + ".";
                case "FreedomOfMovement": return "효과: " + ConditionsKo() + " 상태에 면역이 되고, 붙잡기 전법이 통하지 않으며, 이동을 방해하는 효과와 마비 효과를 받지 않는다.";
                case "BlessingOfTheSalamander": return "효과: 빠른 치유 " + SalamanderFastHealing + ", 화염 저항 " + SalamanderFireResistance + ", 전법 방어에 +" + SalamanderCmdBonus + " 능숙 보너스.";
                case "DeathWard": return "효과: 죽음 효과에 대한 내성 굴림에 +" + DeathWardSaveBonus + " 사기 보너스. 음에너지 피해, 원기 소진, 해로운 음에너지 전달에 면역이 된다.";
                default: throw new InvalidOperationException("PackWarden: no effect text for pack empowerment " + c.Key);
            }
        }
        public static string EffectEn(Candidate c)
        {
            int from = MinLevel(c), to = PackRules.MaxHunterLevel;
            const string fang = " on attack and damage rolls with natural weapons and unarmed strikes";
            const string noStack = ". It does not stack with other enhancement bonuses to the same ability score.";
            switch (c.Key)
            {
                case "MagicFang": return "Effect: a +" + MagicFangEnhancement + " enhancement bonus" + fang + ".";
                case "BullsStrength": return "Effect: a +" + AbilityEnhancementBonus + " enhancement bonus to Strength" + noStack;
                case "CatsGrace": return "Effect: a +" + AbilityEnhancementBonus + " enhancement bonus to Dexterity" + noStack;
                case "BearsEndurance": return "Effect: a +" + AbilityEnhancementBonus + " enhancement bonus to Constitution" + noStack;
                case "Barkskin": return "Effect: an enhancement bonus to natural armor (AC): " + BandsEn(LevelBands(BarkskinArmorAt, from, to)) + ". It stacks with the creature's own natural armor, but not with other enhancement bonuses to natural armor.";
                case "NaturalRhythm": return "Effect: during a full attack, each consecutive hit with a natural weapon against the same target adds +" + NaturalRhythmDamageStep + " damage to the next natural weapon attack of that full attack. A miss or a change of target starts the count again from " + NaturalRhythmResetValue + ", and attacks of opportunity are not affected.";
                case "MagicFangGreater": return "Effect: an enhancement bonus" + fang + ": " + BandsEn(LevelBands(GreaterFangEnhancementAt, from, to)) + ".";
                case "FreedomOfMovement": return "Effect: immunity to the " + ConditionsEn() + " conditions, immunity to grapple combat maneuvers, and immunity to movement-impairing and paralysis effects.";
                case "BlessingOfTheSalamander": return "Effect: fast healing " + SalamanderFastHealing + ", fire resistance " + SalamanderFireResistance + " and a +" + SalamanderCmdBonus + " competence bonus to Combat Maneuver Defense.";
                case "DeathWard": return "Effect: a +" + DeathWardSaveBonus + " morale bonus on saving throws against death effects, and immunity to negative energy damage, energy drain and harmful channeled negative energy.";
                default: throw new InvalidOperationException("PackWarden: no effect text for pack empowerment " + c.Key);
            }
        }
        // 이동의 자유가 막는 상태의 표기(한글 패치: Paralyzed 마비 · Slowed 감속 · Entangled 속박 · 험지; CantMove 는 패치에 낱말이 없어 「이동 불가」). 사본의 이름마다 표기가 있어야 한다(없으면 던진다).
        static string ConditionWord(string condition, bool korean)
        {
            switch (condition)
            {
                case "Paralyzed": return korean ? "마비" : "paralyzed";
                case "Slowed": return korean ? "감속" : "slowed";
                case "Entangled": return korean ? "속박" : "entangled";
                case "DifficultTerrain": return korean ? "험지" : "difficult terrain";
                case "CantMove": return korean ? "이동 불가" : "unable-to-move";
                default: throw new NotSupportedException("PackWarden: no player text for condition '" + condition + "'");
            }
        }
        static string ConditionsKo()
        {
            var words = new List<string>();
            foreach (var x in FreedomOfMovementConditions) words.Add(ConditionWord(x, true));
            return string.Join(", ", words.ToArray());
        }
        static string ConditionsEn()
        {
            var words = new List<string>();
            foreach (var x in FreedomOfMovementConditions) words.Add(ConditionWord(x, false));
            if (words.Count < 2) throw new InvalidOperationException("PackWarden: the Freedom of Movement condition list needs at least two entries");
            return string.Join(", ", words.GetRange(0, words.Count - 1).ToArray()) + " and " + words[words.Count - 1];
        }
        // 지속 줄: 「레벨당 단위」 + 고를 수 있는 첫 레벨과 최고 레벨의 값. 레벨에 정비례하지 않는 지속이면 이 문장이 틀리므로 정적 생성자가 전 구간을 대조한다.
        static string DurationKo(Candidate c)
        {
            int from = MinLevel(c), to = PackRules.MaxHunterLevel;
            string rate = DurationRateOf(c);
            return "지속: 헌터 레벨당 " + DurationText(TextDurationSeconds(c, 1), rate, true) + "(" + from + "레벨 " + DurationText(TextDurationSeconds(c, from), rate, true) + ", " + to + "레벨 " + DurationText(TextDurationSeconds(c, to), rate, true) + ").";
        }
        static string DurationEn(Candidate c)
        {
            int from = MinLevel(c), to = PackRules.MaxHunterLevel;
            string rate = DurationRateOf(c);
            return "Duration: " + DurationText(TextDurationSeconds(c, 1), rate, false) + " per hunter level (" + DurationText(TextDurationSeconds(c, from), rate, false) + " at level " + from + ", " + DurationText(TextDurationSeconds(c, to), rate, false) + " at level " + to + ").";
        }
        static string UseKo()
        {
            return "사용: " + ActionText(ActionTypeName, true) + ", 하루 " + UsesPerDay + "회(무리 강화 능력마다 따로 세고 휴식하면 회복된다). " + AbilityTypeText(AbilityTypeName, true) + "이며 시전자 레벨은 헌터 레벨이다.";
        }
        static string UseEn()
        {
            return "Use: a " + ActionText(ActionTypeName, false) + ", " + UsesPerDay + " times per day (each pack empowerment counts its own uses; they return on rest). This is a " + AbilityTypeText(AbilityTypeName, false) + " with caster level equal to hunter level.";
        }
        const string TargetKo = "대상: 거리 제한이 없다. 시전한 뒤에 나타난 보조 동물에게는 걸리지 않고, 무리 구성원이 하나도 없으면 쓸 수 없다.";
        const string TargetEn = "Targets: there is no range limit. Auxiliary animals that appear after the casting are not affected, and it cannot be used when the pack has no member.";
        // 그 후보를 고를 수 있는 선택 레벨(전제조건을 통과하는 선택 레벨 전부).
        static List<string> SelectionLevelsFor(Candidate c)
        {
            var levels = new List<string>();
            foreach (int lv in SelectionLevels) if (lv >= MinLevel(c)) levels.Add(lv.ToString());
            if (levels.Count == 0) throw new InvalidOperationException("PackWarden: pack empowerment " + c.Key + " can be chosen at no selection level");
            return levels;
        }
        static string PrerequisiteKo(Candidate c)
        {
            return "전제조건: 헌터 " + MinLevel(c) + "레벨 이상. 헌터 " + string.Join("·", SelectionLevelsFor(c).ToArray()) + "레벨의 무리 강화 선택에서 고를 수 있다.";
        }
        static string PrerequisiteEn(Candidate c)
        {
            var levels = SelectionLevelsFor(c);
            string list = levels.Count == 1 ? "level " + levels[0] : "levels " + string.Join(", ", levels.GetRange(0, levels.Count - 1).ToArray()) + " and " + levels[levels.Count - 1];
            return "Prerequisite: hunter level " + MinLevel(c) + ". It can be chosen at the pack empowerment selection of hunter " + list + ".";
        }

        // 선택지 특성의 설명(레벨업 선택 화면): 무엇을 거는가 → 효과와 수치 → 지속 → 사용 → 대상 → 전제조건.
        public static string ChoiceDescriptionKo(Candidate c)
        {
            if (c == null) throw new ArgumentNullException("c");
            return string.Join("\n", new[] { WhatKo(c), EffectKo(c), DurationKo(c), UseKo(), TargetKo, PrerequisiteKo(c) });
        }
        public static string ChoiceDescriptionEn(Candidate c)
        {
            if (c == null) throw new ArgumentNullException("c");
            return string.Join("\n", new[] { WhatEn(c), EffectEn(c), DurationEn(c), UseEn(), TargetEn, PrerequisiteEn(c) });
        }
        // 능력·자원의 설명(행동 막대 툴팁): 선택지 설명의 앞 3줄 + 사용 1줄. 고를 때만 필요한 줄(대상 규칙·전제조건)은 뺀다.
        public static string DescriptionKo(Candidate c)
        {
            if (c == null) throw new ArgumentNullException("c");
            return string.Join("\n", new[] { WhatKo(c), EffectKo(c), DurationKo(c), ActionText(ActionTypeName, true) + ", 하루 " + UsesPerDay + "회. " + AbilityTypeText(AbilityTypeName, true) + "." });
        }
        public static string DescriptionEn(Candidate c)
        {
            if (c == null) throw new ArgumentNullException("c");
            return string.Join("\n", new[] { WhatEn(c), EffectEn(c), DurationEn(c), "A " + ActionText(ActionTypeName, false) + ", " + UsesPerDay + " times per day. This is a " + AbilityTypeText(AbilityTypeName, false) + "." });
        }

        public const string SelectionKey = "EmpowerSelection";
        public static string SelectionNameKo() { return "무리 강화"; }
        public static string SelectionNameEn() { return "Pack Empowerment"; }
        // i 번째 선택 레벨에서 새로 열리는 후보(전제조건 = 그 선택 레벨). 비어 있으면 던진다(빈 줄을 만들지 않는다).
        static List<Candidate> OpenedAt(int i)
        {
            var list = new List<Candidate>();
            foreach (var c in Candidates) if (MinLevel(c) == SelectionLevels[i]) list.Add(c);
            if (list.Count == 0) throw new InvalidOperationException("PackWarden: no pack empowerment candidate opens at hunter level " + SelectionLevels[i]);
            return list;
        }
        // i 번째 선택 레벨에서 새로 열리는 헌터 주문 레벨 구간.
        static void SpellLevelsAt(int i, out int lo, out int hi) { lo = i == 0 ? 1 : MaxSpellLevelAtSelection[i - 1] + 1; hi = MaxSpellLevelAtSelection[i]; }
        public static string SelectionDescriptionKo()
        {
            var lines = new List<string>();
            var levels = new List<string>();
            foreach (int lv in SelectionLevels) levels.Add(lv.ToString());
            lines.Add("헌터 " + string.Join("·", levels.ToArray()) + "레벨에 무리 강화 능력을 하나씩 고른다. 같은 것을 두 번 고를 수 없다.");
            for (int i = 0; i < SelectionLevels.Length; i++)
            {
                int lo, hi;
                SpellLevelsAt(i, out lo, out hi);
                var names = new List<string>();
                foreach (var c in OpenedAt(i)) names.Add(c.NameKo);
                lines.Add(SelectionLevels[i] + "레벨부터(헌터 주문 " + (lo == hi ? lo.ToString() : lo + "~" + hi) + "레벨): " + string.Join(" / ", names.ToArray()) + ".");
            }
            lines.Add("사용: 고른 능력마다 따로 하루 " + UsesPerDay + "회, " + ActionText(ActionTypeName, true) + "이며 휴식하면 회복된다. " + AbilityTypeText(AbilityTypeName, true) + "이고 시전자 레벨은 헌터 레벨이다.");
            lines.Add("대상: 주 동물 동료와 살아 있는 보조 동물 전체. 거리 제한이 없고, 시전한 뒤에 나타난 보조 동물에게는 걸리지 않으며, 무리 구성원이 하나도 없으면 쓸 수 없다.");
            lines.Add("주 동물 동료에 탄 채로도 쓸 수 있다. 실제 수치와 지속 시간은 선택지마다 적혀 있다.");
            return string.Join("\n", lines.ToArray());
        }
        public static string SelectionDescriptionEn()
        {
            var lines = new List<string>();
            var levels = new List<string>();
            foreach (int lv in SelectionLevels) levels.Add(lv + "th");
            if (levels.Count < 2) throw new InvalidOperationException("PackWarden: the selection text needs at least two selection levels");
            lines.Add("At hunter levels " + string.Join(", ", levels.GetRange(0, levels.Count - 1).ToArray()) + " and " + levels[levels.Count - 1] + ", choose one pack empowerment. The same one cannot be chosen twice.");
            for (int i = 0; i < SelectionLevels.Length; i++)
            {
                int lo, hi;
                SpellLevelsAt(i, out lo, out hi);
                var names = new List<string>();
                foreach (var c in OpenedAt(i)) names.Add(c.NameEn);
                lines.Add("From level " + SelectionLevels[i] + " (hunter spell " + (lo == hi ? "level " + lo : "levels " + lo + "-" + hi) + "): " + string.Join(" / ", names.ToArray()) + ".");
            }
            lines.Add("Use: each chosen ability has its own " + UsesPerDay + " uses per day, as a " + ActionText(ActionTypeName, false) + "; uses return on rest. Each is a " + AbilityTypeText(AbilityTypeName, false) + " with caster level equal to hunter level.");
            lines.Add("Targets: the main animal companion and every living auxiliary animal. There is no range limit; auxiliary animals that appear after the casting are not affected, and the ability cannot be used when the pack has no member.");
            lines.Add("They can be used while riding the main animal companion. The actual values and durations are listed on each choice.");
            return string.Join("\n", lines.ToArray());
        }
        // 설명문의 전제를 로드 때 검사한다(틀리면 직업 등록이 시끄럽게 실패한다 — 틀린 문장을 내보내지 않는다):
        // 「레벨당 단위」 가 성립하는가(지속이 헌터 레벨에 정비례), 후보마다 효과 줄·지속 단위 사본이 있는가, 선택 레벨마다 열리는 후보가 있는가.
        static void CheckTextPremises()
        {
            foreach (var c in Candidates)
            {
                long perLevel = TextDurationSeconds(c, 1);
                for (int lv = 1; lv <= PackRules.MaxHunterLevel; lv++)
                    if (TextDurationSeconds(c, lv) != perLevel * lv)
                        throw new InvalidOperationException("PackWarden: " + c.Key + " duration is not proportional to hunter level at " + lv + " (the text says 'per hunter level')");
                if (ChoiceDescriptionKo(c).Split('\n').Length != ChoiceDescriptionEn(c).Split('\n').Length || DescriptionKo(c).Split('\n').Length != DescriptionEn(c).Split('\n').Length)
                    throw new InvalidOperationException("PackWarden: " + c.Key + " Korean and English descriptions differ in line count");
            }
            if (SelectionDescriptionKo().Split('\n').Length != SelectionDescriptionEn().Split('\n').Length)
                throw new InvalidOperationException("PackWarden: the selection description differs in line count between Korean and English");
        }
        public const string NoTargetsKey = "Empower.NoTargets";
        public const string NoTargetsKo = "무리 구성원(주 동물 동료나 보조 동물)이 없다";
        public const string NoTargetsEn = "The pack has no member (no main animal companion or auxiliary animal)";
    }
}
