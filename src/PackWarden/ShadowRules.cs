using System;
using System.Collections.Generic;

namespace PackWarden
{
    // 그림자 무리(Shadow Pack)의 순수 판정 코어 — 게임 형식 없음(시험 exe 가 함께 컴파일한다, tests\run-tests.ps1).
     
    // 처리기(T3)·점검(T3~T4)·시험이 같은 판정을 이 한 곳에서 읽는다(C4).
    public static class ShadowRules
    {
        // ---- 닫힌 판정 enum(R3-03): 0 = Unknown 보초. 어떤 판정 함수도 Unknown 을 반환하지 않는다(시험이 진리표 전 행으로 단언). ----
        // 기본값으로 남은(아무도 쓰지 않은) 칸이 정상 값처럼 읽히지 않게 한다 — 감사는 Enum.IsDefined ∧ != Unknown 을 요구한다.
         
         
        //   SlotUnknown 과 같은 계약: 그 휘두름의 따라치기를 건너뛰고 처리기가 던져 FailCount 로 센다(스냅샷은 미완성 — 감사가 (UNKNOWN) 으로 FAIL).
        public enum ShadowVerdict { Unknown = 0, Shadow, NotPet, NoPack, Recursive, AttackOfOpportunity, Ranged, SlotUnknown, PackUnreadable }
        public enum StrikeVerdict { Unknown = 0, Strike, TargetDead, AuxCannotAct, OutOfRange }
        public enum SlotStatus { Unknown = 0, Live, Empty, NotLive }

        // 무기를 읽지 않는 관문(R2-01): 소유 → 무리 → 재귀(보조 규칙·Extra) → 기회 공격(D3). Shadow = 「무기 단계로 진행」.
        // 엔진 AoO 규칙엔 WeaponSlot 이 없으므로(UnitAttackOfOpportunity.OnAction @002F~0047) 무기 결측 검사는 이 관문 뒤에만 한다.
        public static ShadowVerdict Gate(bool initiatorIsPet, bool packHasLive, bool extraAttack, bool isAttackOfOpportunity)
        {
            if (!initiatorIsPet) return ShadowVerdict.NotPet;
            if (!packHasLive) return ShadowVerdict.NoPack;
            if (extraAttack) return ShadowVerdict.Recursive;
            if (isAttackOfOpportunity) return ShadowVerdict.AttackOfOpportunity;
            return ShadowVerdict.Shadow;
        }

         
        // Shadow = 「Gate 로 진행(packHasLive = 확정된 살아 있는 수 > 0)」.
        public static ShadowVerdict PackReadGate(bool hasPackPart, bool tallyComplete)
        {
            if (!hasPackPart) return ShadowVerdict.Shadow;
            return tallyComplete ? ShadowVerdict.Shadow : ShadowVerdict.PackUnreadable;
        }

        // 관문 통과 뒤의 무기 단계: 원거리 무기는 무동작(허용된 약한 경로 ③, 지도자당 경고는 처리기 몫).
        public static ShadowVerdict WeaponGate(bool weaponIsRanged)
        {
            return weaponIsRanged ? ShadowVerdict.Ranged : ShadowVerdict.Shadow;
        }

         
        // 엔진에서 RuleAttackWithWeapon.WeaponSlot 을 채우는 곳은 UnitAttack.TriggerAttackRule @00B3 하나뿐(dnlib callers 전수, T3 검토 I-1).
        // 슬롯 없는 메인 근접 공격의 무기 출처(dnlib 2026-11-14, Assembly-CSharp SHA256 2CB7160B…8953):
        //   Cleave  AbilityCustomCleave/<Deliver>d__5 @009A GetThreatHandMelee(caster,false) → @01E9 slot.get_Weapon → @01EF ctor (첫 타·추가 타 모두 같은 슬롯 객체)
        //   Vital Strike AbilityCustomVitalStrike/<Deliver>d__7 @004E UnitEntityData.GetFirstWeapon(주손/보조손/추가 팔다리의 MaybeWeapon) → @00F6 ctor
        //   ContextActionMeleeAttack.RunAttackRule @0003 slot.get_Weapon(슬롯 = RunAction @002C GetThreatHandMelee 또는 @00BF AttackInfo.Hand)
        //   UnitCustomFreeAttackBase.OnAction @001F m_NextAttackInfo.get_Weapon → RunAttackRule @0004 ctor
        //   AbilityCustomMeleeAttack/<Deliver>d__2 @00C1 slot.get_Weapon → @00C8 ctor
        //   ContextActionAttackWithWeapon.RunAction @0027 new ItemEntityWeapon(m_WeaponRef 블루프린트) → @002D ctor (어느 슬롯의 객체도 아님 → 블루프린트로만 맞을 수 있음)
        // 그래서 인스턴스(같은 객체)로 먼저 찾고, 없을 때만 같은 블루프린트를 본다. 둘 이상이면 모호(판정 불가), 없으면 찾지 못함(판정 불가).
        // 입력 = 펫 몸의 무기 있는 슬롯(HasWeapon 으로 거른 순서: 주손, 보조손, 추가 팔다리) 마다 (같은 객체인가, 같은 블루프린트인가).
        public enum SlotMatch { Unknown = 0, RuleSlot, ByInstance, ByBlueprint, NotFound, Ambiguous }
        public struct SlotPick { public SlotMatch Match; public int Index; } // Index = 입력 목록 안 위치, RuleSlot·판정 불가면 −1
        public static SlotPick ResolveMainSlot(bool ruleHasSlot, IList<bool> sameInstance, IList<bool> sameBlueprint)
        {
            if (ruleHasSlot) return new SlotPick { Match = SlotMatch.RuleSlot, Index = -1 };
            if (sameInstance == null) throw new ArgumentNullException("sameInstance");
            if (sameBlueprint == null) throw new ArgumentNullException("sameBlueprint");
            if (sameInstance.Count != sameBlueprint.Count) throw new ArgumentException("PackWarden: ResolveMainSlot lists differ in length (" + sameInstance.Count + " vs " + sameBlueprint.Count + ")");
            int inst = -1, nInst = 0, bp = -1, nBp = 0;
            for (int i = 0; i < sameInstance.Count; i++)
            {
                if (sameInstance[i] && !sameBlueprint[i]) throw new ArgumentException("PackWarden: ResolveMainSlot slot " + i + " holds the same weapon object with a different blueprint (contradictory)");
                if (sameInstance[i]) { nInst++; inst = i; }
                if (sameBlueprint[i]) { nBp++; bp = i; }
            }
            if (nInst == 1) return new SlotPick { Match = SlotMatch.ByInstance, Index = inst };
            if (nInst > 1) return new SlotPick { Match = SlotMatch.Ambiguous, Index = -1 };
            if (nBp == 1) return new SlotPick { Match = SlotMatch.ByBlueprint, Index = bp };
            if (nBp > 1) return new SlotPick { Match = SlotMatch.Ambiguous, Index = -1 };
            return new SlotPick { Match = SlotMatch.NotFound, Index = -1 };
        }

        // 슬롯이 정해졌는가. 판정 불가(NotFound·Ambiguous) = 거짓, 보초·미등록 값은 던진다.
        public static bool SlotResolved(SlotMatch m)
        {
            switch (m)
            {
                case SlotMatch.RuleSlot: case SlotMatch.ByInstance: case SlotMatch.ByBlueprint: return true;
                case SlotMatch.NotFound: case SlotMatch.Ambiguous: return false;
                default: throw new ArgumentOutOfRangeException("m", m, "PackWarden: unknown SlotMatch");
            }
        }

         
        // slotUnknown = 1회 로그 규칙을 따르는 판정 불가 실패인가(두 종류 공통).
        // firstForKey 는 슬롯 판정 불가일 때만 의미가 있다 — 아닌데 참이면 호출자 결함(던짐).
        public static bool StrikeFailureLogged(bool slotUnknown, bool firstForKey)
        {
            if (!slotUnknown && firstForKey) throw new ArgumentException("PackWarden: StrikeFailureLogged(slotUnknown=false, firstForKey=true) is contradictory");
            return !slotUnknown || firstForKey;
        }
        public const string SlotUnknownHead = "UNKNOWN main weapon slot: ";
         
        public const string PackUnreadableHead = "UNDETERMINED pack (unresolved auxiliary slot) for ";

        // 대상 생존(T3 검토 R06): 죽음 표시 또는 HP ≤ 0 이면 죽음(HP 0 = 사망 처리 직전 같은 프레임).
        public static bool TargetAlive(bool isDead, int hpBefore) { return !(isDead || hpBefore <= 0); }

        // 메인 슬롯의 역할·색인(T3 검토 R11): 주손 → (Primary, 0), 보조손 → (Secondary, 0), 추가 팔다리 k → (Limb, k). 어느 것도 아니면(몸에 없음)·주손이면서 보조손 → throw.
        public struct MainLimbRef { public LimbRole Role; public int Index; }
        public static MainLimbRef MainLimb(bool isPrimary, bool isSecondary, int limbIndex)
        {
            if (isPrimary && isSecondary) throw new ArgumentException("PackWarden: main slot is both primary and secondary hand (contradiction)");
            if (isPrimary) return new MainLimbRef { Role = LimbRole.Primary, Index = 0 };
            if (isSecondary) return new MainLimbRef { Role = LimbRole.Secondary, Index = 0 };
            if (limbIndex < 0) throw new InvalidOperationException("PackWarden: main weapon slot is not on the main companion's body");
            return new MainLimbRef { Role = LimbRole.Limb, Index = limbIndex };
        }





        // 진리표 전용 합성. 처리기는 Gate 와 WeaponGate 를 따로, 그 순서로 부른다(무기 접근이 관문 뒤에 오도록).
        public static ShadowVerdict ShouldShadow(bool initiatorIsPet, bool packHasLive, bool extraAttack, bool isAttackOfOpportunity, bool weaponIsRanged)
        {
            var g = Gate(initiatorIsPet, packHasLive, extraAttack, isAttackOfOpportunity);
            return g != ShadowVerdict.Shadow ? g : WeaponGate(weaponIsRanged);
        }

        // 보조 1마리의 이 타 판정(설계 D2·SP-06): 죽은 대상 → 행동 불가 → 사정거리 밖 → 타격.
        public static StrikeVerdict StrikeDecision(bool targetAlive, bool auxCanAct, bool inRange)
        {
            if (!targetAlive) return StrikeVerdict.TargetDead;
            if (!auxCanAct) return StrikeVerdict.AuxCannotAct;
            if (!inRange) return StrikeVerdict.OutOfRange;
            return StrikeVerdict.Strike;
        }

        // 사정거리(경계 포함). 엔진 값이 NaN·음수면 결측·오류이므로 판정하지 않고 던진다.
        public static bool InRange(float distance, float approachRadius)
        {
            if (float.IsNaN(distance) || distance < 0) throw new ArgumentOutOfRangeException("distance", distance, "PackWarden: distance must be a non-negative number");
            if (float.IsNaN(approachRadius) || approachRadius < 0) throw new ArgumentOutOfRangeException("approachRadius", approachRadius, "PackWarden: approach radius must be a non-negative number");
            return distance <= approachRadius;
        }

        // ---- 팔다리 대응(설계 D5 (다)): 무기 형식 GUID 대응, 없으면 주손, 경고 없음 ----
        public enum LimbRole { Primary, Secondary, Limb }

        // 무기가 있는 슬롯 하나(어댑터가 HasWeapon 으로 거른 뒤 압축한 목록의 원소). Index = 역할 안 색인(주손·보조손은 0, 팔다리는 AdditionalLimbs 색인).
        public sealed class LimbDesc
        {
            public readonly LimbRole Role;
            public readonly int Index;
            public readonly string WeaponTypeGuid;
            public LimbDesc(LimbRole role, int index, string weaponTypeGuid)
            {
                if (weaponTypeGuid == null) throw new ArgumentNullException("weaponTypeGuid");
                if (index < 0) throw new ArgumentOutOfRangeException("index", index, "PackWarden: limb index must be >= 0");
                if (role != LimbRole.Primary && role != LimbRole.Secondary && role != LimbRole.Limb) throw new ArgumentOutOfRangeException("role", role, "PackWarden: unknown LimbRole");
                Role = role; Index = index; WeaponTypeGuid = weaponTypeGuid;
            }
        }

        // 반환 = aux 안의 위치. ① aux 비었거나 aux[0] 이 주손이 아님 → throw ② 메인 형식 null → throw
        // ③ 같은 (역할, 색인) ∧ 같은 형식 → 그것 ④ 순서상 첫 「같은 형식」 ⑤ 없으면 0(주손).
        public static int MapLimb(LimbRole mainRole, int mainIndex, string mainWeaponTypeGuid, IList<LimbDesc> aux)
        {
            if (aux == null) throw new ArgumentNullException("aux");
            if (aux.Count == 0) throw new ArgumentException("PackWarden: auxiliary has no weapon slot (the compressed list is empty)", "aux");
            for (int i = 0; i < aux.Count; i++)
                if (aux[i] == null) throw new ArgumentException("PackWarden: auxiliary limb list has a null entry at " + i, "aux");
            if (aux[0].Role != LimbRole.Primary) throw new ArgumentException("PackWarden: auxiliary limb list must start with the primary hand (got " + aux[0].Role + ")", "aux");
            if (mainWeaponTypeGuid == null) throw new ArgumentNullException("mainWeaponTypeGuid");
            if (mainIndex < 0) throw new ArgumentOutOfRangeException("mainIndex", mainIndex, "PackWarden: limb index must be >= 0");
            for (int i = 0; i < aux.Count; i++)
                if (aux[i].Role == mainRole && aux[i].Index == mainIndex && aux[i].WeaponTypeGuid == mainWeaponTypeGuid) return i;
            for (int i = 0; i < aux.Count; i++)
                if (aux[i].WeaponTypeGuid == mainWeaponTypeGuid) return i;
            return 0;
        }

        // 설계 D1 (라)·SP-01: 메인 규칙의 AttackBonusPenalty 를 그대로 복사 = 같은 유효 페널티. 엔진 값에 도메인을 씌우지 않는다(항등).
        public static int ShadowPenalty(int mainAttackBonusPenalty) { return mainAttackBonusPenalty; }

        // 반복 공격 페널티 간격: UnitAttack/<EnumerateAttacks>d__89.MoveNext @0070 FlawlessAttacks → @0077 ldc.i4.5 / @007A ldc.i4.4 (dnlib 2026-11-14, SHA256 2CB7160B…8953).
        public const int IterativeStepNormal = 5, IterativeStepFlawless = 4;




        // 그림자 규칙에 넣는 플래그(설계 B3 — 리터럴은 여기 1곳). Extra = 재귀 차단 표식, AoO 아님.
        // 복사 = 메인의 전력 공격·첫 공격·공격 번호·공격 수(R3-05 가 기록·감사까지 전달), 복사 안 함 = 자동 명중·자동 치명 위협·돌격·찢기.
        public struct ShadowRuleSpec
        {
            public bool ExtraAttack, IsAttackOfOpportunity;
            public bool CopyIsFullAttack, CopyIsFirstAttack, CopyAttackNumber, CopyAttacksCount;
            public bool CopyAutoHit, CopyAutoCriticalThreat, CopyIsCharge, CopyIsRend;
        }
        public static readonly ShadowRuleSpec Spec = new ShadowRuleSpec
        {
            ExtraAttack = true, IsAttackOfOpportunity = false,
            CopyIsFullAttack = true, CopyIsFirstAttack = true, CopyAttackNumber = true, CopyAttacksCount = true,
            CopyAutoHit = false, CopyAutoCriticalThreat = false, CopyIsCharge = false, CopyIsRend = false,
        };








        // 처리 중인 메인 규칙의 스택(R2-03 중첩 보존). Pop 은 맨 위가 expected 와 같은 객체일 때만 — 짝이 깨지면 던지고 스택은 그대로.
        public sealed class ActiveContext<T> where T : class
        {
            readonly List<T> stack = new List<T>();
            public void Push(T item)
            {
                if (item == null) throw new ArgumentNullException("item");
                stack.Add(item);
            }
            public void Pop(T expected)
            {
                if (stack.Count == 0) throw new InvalidOperationException("PackWarden: ActiveContext.Pop on an empty stack");
                if (!ReferenceEquals(stack[stack.Count - 1], expected)) throw new InvalidOperationException("PackWarden: ActiveContext.Pop pairing broken (top is not the expected item)");
                stack.RemoveAt(stack.Count - 1);
            }
            // 비었으면 null = 「처리 중인 메인 없음」 — 호출자가 판정한다.
            public T Current { get { return stack.Count == 0 ? null : stack[stack.Count - 1]; } }
            public int Depth { get { return stack.Count; } }
        }

        // ---- 모드(설계 D6): 원하는 상태 = 펫 상태 하나 ----
        public enum PackMode { Shadow, Independent }

        // 펫이 있고 의식이 있으면 그림자, 아니면 독립. 펫 없음 ∧ 의식 있음 은 모순 입력(호출자 결함)이라 던진다.
        public static PackMode ModeFor(bool petExists, bool petConscious)
        {
            if (!petExists && petConscious) throw new ArgumentException("PackWarden: ModeFor(petExists=false, petConscious=true) is contradictory");
            return petExists && petConscious ? PackMode.Shadow : PackMode.Independent;
        }

         
        // ShadowPack.DecideState 가 게임 값만 모아 이것 하나를 부른다(판정은 여기 한 곳).
        //   exists         = 지도자.GetPet(AnimalCompanion) != null (엔진 GetPet = UnitPartPetMaster.m_Pets 에서 형식이 맞는 첫 항목 — UnitEntityData.GetPet @000E~0027)
        //   inWorld        = 그 펫이 !Destroyed
        //   hidden         = 그 펫이 !IsInGame (HideAllPets.TryHidePet @0076 UnitPartPet.SetHidden(true) → SetHidden @002E set_IsInGame(false) · @0082 set_IsInGame(false);
        //                    파티 밖 동료 지도자: UnitEntityData.OnIsInGameChanged @006B~00BB 가 펫 IsInGame 을 주인 값으로 맞춘다)
        //   removedByEvent = 지금 처리 중인 HandleRemovePet 이 이 지도자의 동물 동료를 알렸다(펫이 목록에 있으면 그 펫과 같은 유닛, 없으면 사건 유닛의
        //                    UnitPartPet 이 Type = AnimalCompanion ∧ Master = 지도자 — RemovePet 은 @0043 발행 뒤 @006C 에야 UnitPartPet.Clear). 사건 밖에서는 false.
        //   dead = State.IsDead, conscious = State.IsConscious
        // 결과:
        //   목록에 없음 ∧ 제거 사건      → Dismiss        (UnitPartPetMaster.RemovePet: @001E m_Pets.Remove 뒤 @0043 발행 = 영구 제거)
        //   목록에 없음 ∧ 사건 아님      → Independent    (펫 없음 = 메인 부재, 설계 D6 — 사건 없는 떼어짐 EtudeBracketDetachPet → SetPetAsEx 포함)
        //   목록에 있음 ∧ 파괴됨          → Independent
        //   목록에 있음 ∧ 숨김            → HiddenWithMain (생사·의식·사건과 무관: 메인이 세상 밖이면 보조도 세상 밖)
        //   목록·세상에 있음 ∧ 제거 사건 → Independent + unknown (IL 상 없는 순서 — 호출자가 FailCount 계약으로 센다)
        //   그 밖                         → 죽음·기절 = Independent(D6), 의식 있음 = Shadow
        // 모순 입력(목록에 없는데 펫 사실이 참, 죽었는데 의식 있음)은 호출자 결함이라 던진다.
        public enum PackState { Unknown = 0, Shadow, Independent, HiddenWithMain, Dismiss }
        public const string RemovedPetStillListed = "UNKNOWN: the pet named by HandleRemovePet is still returned by GetPet and is in the world";
        public static PackState StateForMainPet(bool exists, bool inWorld, bool hidden, bool removedByEvent, bool dead, bool conscious, out string unknown)
        {
            unknown = null;
            if (!exists && (inWorld || hidden || dead || conscious)) throw new ArgumentException("PackWarden: StateForMainPet(exists=false) with a pet fact set is contradictory");
            if (dead && conscious) throw new ArgumentException("PackWarden: StateForMainPet(dead=true, conscious=true) is contradictory");
            if (!exists) return removedByEvent ? PackState.Dismiss : PackState.Independent;
            if (!inWorld) return PackState.Independent;
            if (hidden) return PackState.HiddenWithMain;
            if (removedByEvent)
            {
                unknown = RemovedPetStillListed + " (engine order differs from UnitPartPetMaster.RemovePet @001E/@0043 and HideAllPets.TryHidePet @0082/@00CF) — treated as absent";
                return PackState.Independent;
            }
            return ModeFor(!dead, !dead && conscious) == PackMode.Shadow ? PackState.Shadow : PackState.Independent;
        }

        // 상태 → 두뇌 모드. 두뇌를 맞추는 상태는 Shadow·Independent 뿐 — 숨김·해산에 두뇌를 고르는 것은 호출자 결함(던짐).
        public static PackMode BrainModeFor(PackState s)
        {
            switch (s)
            {
                case PackState.Shadow: return PackMode.Shadow;
                case PackState.Independent: return PackMode.Independent;
                default: throw new ArgumentOutOfRangeException("s", s, "PackWarden: no brain mode for pack state " + s);
            }
        }

        // 상태 → 보조가 게임 안(IsInGame)에 있어야 하는가. 해산은 보이기 판정 대상이 아니다(던짐).
        public static bool AuxVisibleFor(PackState s)
        {
            switch (s)
            {
                case PackState.Shadow: case PackState.Independent: return true;
                case PackState.HiddenWithMain: return false;
                default: throw new ArgumentOutOfRangeException("s", s, "PackWarden: no visibility for pack state " + s);
            }
        }

        // 새 보조를 소환해도 되는가: 메인이 숨은 동안은 소환하지 않는다(소환 직후 숨길 보조를 만들지 않음). 해산은 사건 밖에서 나올 수 없다(던짐).
        public static bool SummonAllowed(PackState s)
        {
            switch (s)
            {
                case PackState.Shadow: case PackState.Independent: return true;
                case PackState.HiddenWithMain: return false;
                default: throw new ArgumentOutOfRangeException("s", s, "PackWarden: SummonAllowed has no answer for pack state " + s);
            }
        }

        // 보이기 맞춤 순서: inGame[i] = 살아 있는 보조 i 의 지금 IsInGame. 결과 = 바꿔야 할 보조 색인(순서대로). 이미 맞으면 무동작.
        public static List<int> VisibilitySteps(IList<bool> inGame, bool wantVisible)
        {
            if (inGame == null) throw new ArgumentNullException("inGame");
            var r = new List<int>();
            for (int i = 0; i < inGame.Count; i++) if (inGame[i] != wantVisible) r.Add(i);
            return r;
        }













        // 재검토 m-2: Reconcile 의 실행 순서. matching[i] = 살아 있는 보조 i 가 원하는 모드와 이미 맞는가. 결과 = 맞지 않는 보조 색인(순서대로) 다음,
        // 판정 불가면 마지막에 ReconcileThrowStep 하나 — 「보조를 먼저 맞춘 뒤 던진다」 계약(호출자 ShadowPack.ReconcileCore 가 이 목록을 그대로 실행).
        public const int ReconcileThrowStep = -1;
        public static List<int> ReconcileSteps(IList<bool> matching, bool unknown)
        {
            if (matching == null) throw new ArgumentNullException("matching");
            var steps = new List<int>();
            for (int i = 0; i < matching.Count; i++)
                if (!matching[i]) steps.Add(i);
            if (unknown) steps.Add(ReconcileThrowStep);
            return steps;
        }

        // 모드 전환 때 중단할 명령인가(설계 D4: AoO·플레이어 명령 보존 — AI 가 낸 명령만 중단). ApplyMode 의 InterruptAll 술어가 이 결과를 그대로 쓴다(T4 검토 M-2/V03).
        public static bool InterruptsOnSwitch(bool isAiCommand) { return isAiCommand; }

        // AI 추종 명령이 「낡은 대상」인가(Observe 의 AiFollowWrongTarget, T4 검토 M-1/V26). 3상태(C1).
        // 2026-11-18 결함 수리(소유자 결정): 교전 추종(PackWardenAiFollowEngaged)이 낸 「주 동물이 교전 중인 적」 쪽 명령은 유효하다 — 그 전엔 펫 아닌 대상을
        // 전부 낡음으로 세어 Reconcile 이 ApplyMode 로 그 명령을 끊었다(Player-prev.log 15958·15962 「늑대 -> Shadow … interrupted=1」).
        // targetIsEngagedEnemy = 대상이 주 동물이 지금 교전 중인 적인가(TargetIsEngagedEnemy). 펫을 알고 대상이 펫이 아닐 때만 쓰고, 그 밖은 null(계산 안 함)을 넘긴다.
        // 그 자리의 null = 교전 판정 불가 → Unknown(Fresh·Stale 어느 쪽으로도 넘기지 않는다 — 호출자가 장부에 남기고 CountsAsWrongTarget 로 센다).
        public enum FollowStale { Unknown = 0, Fresh, Stale }
        public static FollowStale FollowTargetStale(bool petKnown, bool targetIsPet, bool? targetIsEngagedEnemy)
        {
            if (!petKnown && targetIsPet) throw new ArgumentException("PackWarden: FollowTargetStale(petKnown=false, targetIsPet=true) is contradictory");
            if (targetIsPet)
            {
                if (targetIsEngagedEnemy == true) throw new ArgumentException("PackWarden: FollowTargetStale: the pet itself cannot be the enemy it is engaged with");
                return FollowStale.Fresh;
            }
            if (!petKnown)
            {
                // 교전 판정은 펫이 있어야 한다(ShadowBrains.Engagement 은 펫 없음 = Unknown) → 펫 없이 답이 왔으면 호출자 결함.
                if (targetIsEngagedEnemy != null) throw new ArgumentException("PackWarden: FollowTargetStale: no pet but an engagement answer (" + targetIsEngagedEnemy.Value + ") was given");
                return FollowStale.Stale;
            }
            if (targetIsEngagedEnemy == null) return FollowStale.Unknown;
            return targetIsEngagedEnemy.Value ? FollowStale.Fresh : FollowStale.Stale;
        }

        // Observe 의 AiFollowWrongTarget 에 셀 것인가. 판정 불가 = 기존 동작(낡음으로 세어 재적용 — 2026-11-18 이전과 같은 결과)을 유지하되 호출자가 장부(FollowUnknown)에 남긴다
        // → 점검 ShadowHealth 가 판정 불가 증가 0 을 단언하므로 조용히 지나가지 않는다.
        public static bool CountsAsWrongTarget(FollowStale v)
        {
            switch (v)
            {
                case FollowStale.Fresh: return false;
                case FollowStale.Stale: return true;
                case FollowStale.Unknown: return true;
                default: throw new ArgumentOutOfRangeException("v", v, "PackWarden: CountsAsWrongTarget has no answer for " + v);
            }
        }

        // 교전 판정(ClassifyEngagement — 추종 행동 PackWardenAiFollowEngaged.IsTargetValid 와 같은 ShadowBrains.Engagement) → FollowTargetStale 의 셋째 인자.
        // sameAsEngagedTarget = 명령 대상이 그 판정의 교전 대상과 같은 유닛인가(교전 대상이 없으면 호출자가 false 를 넘긴다). 교전 대상의 적대·생존·게임 안은 Engagement(Usable)가 이미 확인했다.
        public static bool? TargetIsEngagedEnemy(EngageVerdict v, bool sameAsEngagedTarget)
        {
            switch (v)
            {
                case EngageVerdict.Engaged: return sameAsEngagedTarget;
                case EngageVerdict.NotEngaged:
                case EngageVerdict.Unknown:
                    if (sameAsEngagedTarget) throw new ArgumentException("PackWarden: TargetIsEngagedEnemy: verdict " + v + " has no engaged target to be the same as");
                    return v == EngageVerdict.NotEngaged ? (bool?)false : null;
                default: throw new ArgumentOutOfRangeException("v", v, "PackWarden: TargetIsEngagedEnemy has no answer for " + v);
            }
        }

        // 키마다 첫 1회만 참(로그 1회 규칙 — 메인 슬롯 판정 불가 오류 줄 「(펫, 원천)별 1회」, T4 검토 M-5/V17). 빈 키는 던진다.
        public sealed class FirstSeen
        {
            readonly HashSet<string> seen = new HashSet<string>();
            public bool First(string key)
            {
                if (string.IsNullOrEmpty(key)) throw new ArgumentException("PackWarden: FirstSeen needs a key", "key");
                return seen.Add(key);
            }
        }

        public static string BrainGuidFor(PackMode mode)
        {
            switch (mode)
            {
                case PackMode.Shadow: return Guids.ShadowBrain;
                case PackMode.Independent: return Guids.Game.CharacterBrain;
                default: throw new ArgumentOutOfRangeException("mode", mode, "PackWarden: unknown PackMode");
            }
        }

        // 모드별 두뇌 행동 GUID 집합(정렬, 매번 새 배열). Independent = 본편 Character_Brain.m_Actions(blueprints.zip 2026-11-14 — 시험이 대조).
        public static string[] ExpectedActionGuids(PackMode mode)
        {
            string[] r;
            switch (mode)
            {
                case PackMode.Shadow: r = new[] { Guids.FollowPetAiAction, Guids.FollowEngagedAiAction }; break; // 2026-11-16: 교전 대상 추종 추가
                case PackMode.Independent: r = new[] { Guids.Game.ManualAttackAiAction, Guids.Game.AttackAiAction }; break;
                default: throw new ArgumentOutOfRangeException("mode", mode, "PackWarden: unknown PackMode");
            }
            Array.Sort(r, StringComparer.Ordinal);
            return r;
        }

        // 모드별 BlueprintAiAttack 행동 수: 그림자 = 0(공격 행동 0, 설계 §4.2), 독립 = Character_Brain 의 2.
        public static int ExpectedAttackActions(PackMode mode)
        {
            switch (mode)
            {
                case PackMode.Shadow: return 0;
                case PackMode.Independent: return 2;
                default: throw new ArgumentOutOfRangeException("mode", mode, "PackWarden: unknown PackMode");
            }
        }

        // ---- 추종 상수 ----
        public const int FollowApproachFeet = 5;          // 설계 §8: 동물 동료 BiteType m_AttackRange 5 ft
        public const float FollowSlackMeters = 1.0f;      // 설계 §6-9: 추종 점검 여유 1 m
        public const float FollowBaseScore = 1.0f;        // 본편 Brain/Actions/FollowEnemyAiAction.jbp BaseScore 1.0

        // ---- 교전 대상 추종(소유자 결정 2026-11-16: 주 동물이 싸우는 동안 보조는 그 대상 곁으로 간다) ----
        // 두 번째 추종 행동의 BaseScore. 본편 Brain/Actions/ManualAttackAiAction.jbp BaseScore 10(Guids.Game.ManualAttackAiAction 주석)과 같은 값 —
        // 엔진 점수 = BaseScore × 대상 가중·경로·고려사항(AiAction.ScoreTarget·AiBrainController.CalculateTargetsScore)이라 1 대 10 으로 크게 벌려 펫 추종을 이기게 한다.
        // 교전 행동에 유효 대상이 없으면(교전 아님·판정 불가·엔진이 도달 불가로 거른 대상) 펫 추종(1)만 남는다.
        public const float EngageFollowBaseScore = 10f;
        // 주 동물의 최근 근접 휘두름을 교전으로 보는 시간(게임 초). 다이얼: PF 1라운드 = 6초. 소유자 지시 「최근 N 초(다이얼)」.
        public const double EngageRecentSwingSeconds = 6.0;
        // 교전 후보 순서(우선순위). 0 = 펫의 현재 공격 명령 대상(UnitCommands.Attack.Target), 1 = 기승 중이면 기수(지도자)의 현재 공격 명령 대상,
        // 2 = 펫이 최근 EngageRecentSwingSeconds 안에 근접 휘두른 대상(PackShadowStrikes 가 메인 휘두름마다 기록).
        public const int EngageCandidateCount = 3;

        public enum EngageVerdict { Unknown = 0, Engaged, NotEngaged }

        public struct EngagePick
        {
            public readonly EngageVerdict Verdict;
            public readonly int Index; // Engaged·Unknown = 결정한 후보 번호, NotEngaged = -1
            public EngagePick(EngageVerdict v, int index) { Verdict = v; Index = index; }
        }

        // 후보마다 3상태: true = 쓸 수 있는 적 대상 있음, false = 없음(명령 없음·대상 사망·적 아님·기승 아님·오래됨), null = 읽을 수 없음(판정 불가).
        // 우선순위대로 보고 처음 true 면 교전, true 보다 먼저 null 을 만나면 판정 불가(「없음」으로 건너뛰지 않는다), 전부 false 면 교전 아님.
        public static EngagePick ClassifyEngagement(IList<bool?> candidates)
        {
            if (candidates == null) throw new ArgumentNullException("candidates");
            if (candidates.Count != EngageCandidateCount)
                throw new ArgumentException("PackWarden: expected " + EngageCandidateCount + " engagement candidates, got " + candidates.Count, "candidates");
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == null) return new EngagePick(EngageVerdict.Unknown, i);
                if (candidates[i].Value) return new EngagePick(EngageVerdict.Engaged, i);
            }
            return new EngagePick(EngageVerdict.NotEngaged, -1);
        }

        // ---- 교전 추종 관측(2026-11-16 가드): 「교전 중인데 보조가 대상 쪽으로 가지 않는다」 를 드러낸다 ----
        // 직전까지는 「점수 10 대 1 이면 교전 행동이 이긴다」·「적 목록에 펫의 대상이 있다」 가 틀려도 보조가 펫 곁에 머물 뿐 흔적이 없었다.
        // 지도자의 새 라운드마다(PackEngageWatcher) 살아 있는 보조를 하나씩 관측한다. 관측 1회의 판정:
        //   Satisfied = 교전 대상을 향한 명령이 있거나 이미 그 대상의 근접 사거리 안(도착해 명령이 끝난 상태).
        //   LegitCannotMove = 보조가 움직일 수 없다(기절·속박 등) · LegitUnreachable = 엔진이 그 대상을 도달 불가로 걸렀다(CombatAiData.UnreachableUnits) — 정상, 세지 않는다.
        //   MissNotInEnemyList = 대상이 보조 그룹의 적 기억(UnitGroupMemory.Enemies — AI 적 목록의 원천)에 없다.
        //   MissLostOnScore = 대상은 유효한데 보조가 다른 대상의 명령을 받았다(교전 행동이 점수에서 졌다고 본다 — 추론이다: 다른 원인을 가를 정보가 없다).
        //   MissCauseUnknown = 대상은 유효한데 명령이 아예 없다(원인 불명 — 그래도 보고한다).
        //   MissUnreadable = 판정에 필요한 값을 읽지 못했다. 못 읽은 것을 정상(Legit*)·만족으로 넘기지 않는다.
        public enum EngageObservation { Unknown = 0, Satisfied, LegitUnreachable, LegitCannotMove, MissNotInEnemyList, MissLostOnScore, MissCauseUnknown, MissUnreadable }

        public static EngageObservation ClassifyEngageObservation(bool? commandTargetsEngaged, bool? inRange, bool? canMove, bool? filteredUnreachable, bool? inEnemyList, bool? hasOtherCommand)
        {
            if (commandTargetsEngaged == true) return EngageObservation.Satisfied;
            if (commandTargetsEngaged == null) return EngageObservation.MissUnreadable;
            if (inRange == true) return EngageObservation.Satisfied;
            if (inRange == null) return EngageObservation.MissUnreadable;
            if (canMove == null) return EngageObservation.MissUnreadable;
            if (!canMove.Value) return EngageObservation.LegitCannotMove;
            if (filteredUnreachable == null) return EngageObservation.MissUnreadable;
            if (filteredUnreachable.Value) return EngageObservation.LegitUnreachable;
            if (inEnemyList == null) return EngageObservation.MissUnreadable;
            if (!inEnemyList.Value) return EngageObservation.MissNotInEnemyList;
            if (hasOtherCommand == null) return EngageObservation.MissUnreadable;
            return hasOtherCommand.Value ? EngageObservation.MissLostOnScore : EngageObservation.MissCauseUnknown;
        }

        public static bool IsEngageMiss(EngageObservation o)
        {
            switch (o)
            {
                case EngageObservation.Satisfied:
                case EngageObservation.LegitUnreachable:
                case EngageObservation.LegitCannotMove:
                    return false;
                case EngageObservation.MissNotInEnemyList:
                case EngageObservation.MissLostOnScore:
                case EngageObservation.MissCauseUnknown:
                case EngageObservation.MissUnreadable:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException("o", o, "PackWarden: unclassified engage observation");
            }
        }

        // 연속 놓침 몇 회에 보고하는가. **임의값**(출처 없음): 관측 주기 = 지도자의 새 라운드(약 6초)라 2회 = 약 한 라운드를 통째로 놓친 뒤.
        // 1 이면 방금 교전이 시작돼 아직 AI 가 결정하지 않은 보조를 오류로 셀 수 있어 2 로 뒀다. 게임 안 확인 뒤 조정 대상.
        public const int EngageMissObservationLimit = 2;
        public const string EngageMissLogHead = "PackWarden: engaged follow MISSED ";

        // 보조마다 연속 놓침을 세고, 다이얼에 이르면 그 보조에 대해 1회만 보고하라고 알린다. 정상 관측(만족·정상 사유)은 연속을 끊는다.
        public sealed class EngageMissTracker
        {
            readonly Dictionary<string, int> streak = new Dictionary<string, int>();
            readonly HashSet<string> reported = new HashSet<string>();

            public int ReportCount { get { return reported.Count; } }

            // true = 지금 보고하라(이 보조의 첫 보고).
            public bool Observe(string auxId, EngageObservation o)
            {
                if (string.IsNullOrEmpty(auxId)) throw new ArgumentException("PackWarden: EngageMissTracker needs an auxiliary id", "auxId");
                if (!IsEngageMiss(o)) { streak.Remove(auxId); return false; } // Unknown 보초는 IsEngageMiss 가 던진다
                int n;
                streak.TryGetValue(auxId, out n);
                n++;
                streak[auxId] = n;
                if (n < EngageMissObservationLimit) return false;
                return reported.Add(auxId);
            }

            // 교전이 끝났거나 보조가 사라졌다: 연속을 지운다(보고 이력은 남긴다 — 같은 보조를 다시 보고하지 않는다).
            public void Clear(string auxId)
            {
                if (string.IsNullOrEmpty(auxId)) throw new ArgumentException("PackWarden: EngageMissTracker needs an auxiliary id", "auxId");
                streak.Remove(auxId);
            }

            public int StreakOf(string auxId) { int n; return streak.TryGetValue(auxId, out n) ? n : 0; }
        }

        // 후보 2(최근 휘두름). swingAt = 기록된 게임 시각(초), null = 기록 없음 → false. now 가 없으면(시계를 못 읽음) null.
        // now < swingAt(시계가 뒤로 감 — 불러오기 등)은 판정 불가 null. 창 안이고 대상이 쓸 만해야 true.
        public static bool? RecentSwingCandidate(double? now, double? swingAt, bool targetUsable)
        {
            if (swingAt == null) return false;
            if (now == null) return null;
            if (double.IsNaN(now.Value) || double.IsNaN(swingAt.Value)) return null;
            double age = now.Value - swingAt.Value;
            if (age < 0) return null;
            return age <= EngageRecentSwingSeconds && targetUsable;
        }

        // 추종 점검 거리 = 접근 거리 + 두 유닛 몸 반경 + 여유. 음수·NaN 은 결측·오류라 던진다.
        public static float FollowCheckDistance(float approachMeters, float leaderCorpulence, float followerCorpulence)
        {
            if (float.IsNaN(approachMeters) || approachMeters < 0) throw new ArgumentOutOfRangeException("approachMeters", approachMeters, "PackWarden: must be a non-negative number");
            if (float.IsNaN(leaderCorpulence) || leaderCorpulence < 0) throw new ArgumentOutOfRangeException("leaderCorpulence", leaderCorpulence, "PackWarden: must be a non-negative number");
            if (float.IsNaN(followerCorpulence) || followerCorpulence < 0) throw new ArgumentOutOfRangeException("followerCorpulence", followerCorpulence, "PackWarden: must be a non-negative number");
            return approachMeters + leaderCorpulence + followerCorpulence + FollowSlackMeters;
        }

         
        public const int ExpectedUpgradedBodies = 2;

        // ---- 추종 대상 판정(T2, R3-04): 추종 AI 행동(PackWardenAiFollowPet.IsTargetValid)의 분기 전체를 순수 함수로 ----
        // 상태 3개 + 판정 불가 2종: 대상 = 이 보조 소환자의 동물 동료면 Accept, 다른 유닛이면 Reject, 소환자·펫을 알 수 없으면 UNKNOWN(판정 불가).
        // UNKNOWN 은 Reject 가 아니다 — 호출자는 장부에 세고 로그를 남긴다(AI 루프 안이라 throw 대신; 계획 C1 ④). 0 = Unknown 보초(반환하지 않음).
        public enum FollowTarget { Unknown = 0, Accept, Reject, UnknownNoSummoner, UnknownNoPet }

        public static FollowTarget ClassifyFollowTarget(bool summonerMissing, bool petMissing, bool targetIsPet)
        {
            if (summonerMissing) return FollowTarget.UnknownNoSummoner;
            if (petMissing) return FollowTarget.UnknownNoPet;
            return targetIsPet ? FollowTarget.Accept : FollowTarget.Reject;
        }

        // ---- 공격 AI 행동 판별(T2 W21 판정 2026-11-14): 본편 BlueprintAiAttack 은 internal 이라 `is` 를 못 쓴다 → 형식 사슬의 전체 이름으로 판별. ----
        // 게임 쪽(ShadowBrains.IsAiAttack)은 GetType()→BaseType 사슬의 FullName 목록만 만들어 이 함수에 넘긴다(판정은 여기 한 곳, 시험 exe 가 실행).
        // 출처: dnlib 2026-11-14 Assembly-CSharp(SHA256 2CB7160B…8953) Kingmaker.AI.Blueprints.BlueprintAiAttack IsPublic=false, 기반 BlueprintAiAction.
        public const string AiAttackTypeName = "Kingmaker.AI.Blueprints.BlueprintAiAttack";
        public enum AiActionKind { Unknown = 0, Attack, NotAttack }

        // 사슬(자기 형식부터 위로) 안에 AiAttackTypeName 이 있으면 Attack, 없으면 NotAttack. 사슬이 null·비었거나 원소가 null·빈 문자열이면 Unknown(판정 불가 — 호출자가 던진다).
        public static AiActionKind ClassifyAiActionTypeChain(IList<string> chainFullNames)
        {
            if (chainFullNames == null || chainFullNames.Count == 0) return AiActionKind.Unknown;
            for (int i = 0; i < chainFullNames.Count; i++)
                if (string.IsNullOrEmpty(chainFullNames[i])) return AiActionKind.Unknown;
            for (int i = 0; i < chainFullNames.Count; i++)
                if (chainFullNames[i] == AiAttackTypeName) return AiActionKind.Attack;
            return AiActionKind.NotAttack;
        }





        // 판정 불가 장부: 매 기록마다 Count++(먼저), 그다음 (유닛, 사유) 쌍의 첫 발생에만 로그 1줄(AI 가 틱마다 같은 결측을 볼 때 로그 폭주 방지).
        // 점검(Scenarios.ShadowHealth)이 Count 의 실행 시작 대비 증가량을 0 으로 단언한다. 잘못된 인자는 세지 않고 던진다.
        public sealed class UnknownLedger
        {
            readonly string logHead;
            readonly HashSet<string> logged = new HashSet<string>();
            int count;
            string last;

            public UnknownLedger(string logHead)
            {
                if (string.IsNullOrEmpty(logHead)) throw new ArgumentException("PackWarden: UnknownLedger needs a log head", "logHead");
                this.logHead = logHead;
            }








            // 「무엇보다 먼저 센다」(T1·T2 독립 검토 Minor-4, 2026-11-14): 어떤 인자든 Count++ 가 첫 줄. 유닛 ID 가 비면 던지지 않고
            // 사유에 "unit id missing" 을 붙여 같은 장부에 남긴다(AI 루프 안에서 ID 가 빈 판정 불가가 세지지 않고 사라지지 않게).
            // 사유 없음·로그 출구 없음은 호출자 결함이라 센 뒤 던진다.
            public const string UnitIdMissing = "unit id missing";
            public const string NoUnitId = "(none)";

            public void Record(string unitId, string reason, Action<string> log)
            {
                count++;
                if (string.IsNullOrEmpty(reason)) throw new ArgumentException("PackWarden: UnknownLedger.Record needs a reason", "reason");
                if (log == null) throw new ArgumentNullException("log");
                if (string.IsNullOrEmpty(unitId)) { unitId = NoUnitId; reason = reason + "; " + UnitIdMissing; }
                last = reason + " unit=" + unitId;
                if (logged.Add(unitId + "\u0001" + reason)) log(logHead + "unit=" + unitId + " reason=" + reason);
            }
        }

        // ---- 추종 행동 도달 계수(T1·T2 독립 검토 Important-2): IsTargetValid 가 판정마다 센다. 점검 S3 aux-brain 이 전투 진입 뒤 Accepted ≥ 1 을 단언한다
        // (AI 가 우리 행동을 펫을 후보로 실제로 평가했다는 증거). Unknown·미등록 판정은 셀 칸이 없으므로 던진다. ----
        public sealed class FollowTally
        {
            int accepted, rejected, unknown;









            public void Record(FollowTarget f)
            {
                switch (f)
                {
                    case FollowTarget.Accept: accepted++; return;
                    case FollowTarget.Reject: rejected++; return;
                    case FollowTarget.UnknownNoSummoner:
                    case FollowTarget.UnknownNoPet: unknown++; return;
                    default: throw new ArgumentOutOfRangeException("f", f, "PackWarden: FollowTally cannot count " + f);
                }
            }
        }

        // ---- 그림자 건강 판정 전체(T1·T2 독립 검토 Important-1): 기준선·현재 값·마지막 사유 → FAIL 사유 또는 null. 점검 ShadowHealth 는 값만 모아 이것 하나를 부른다. ----
        // 로그 머리말(한 곳 — ShadowBrains.FollowUnknownLogHead·PackShadowStrikes.FailLogHead 가 이 값을 쓴다, C4).
        public const string FollowUnknownLogHead = "PackWarden: follow target UNKNOWN ";
        public const string StrikeFailLogHead = "PackWarden: PackShadowStrikes failed for ";

         
        // 감시자·소환 전환 실패 로그 머리말(ShadowPack.CountFailure 가 쓴다 — FailCount 계약은 T3 과 같다: 증가 → 로그 → 재throw).
        public const string ModeFailLogHead = "PackWarden: ShadowPack failed in ";

        // 보조의 현재 상태가 모드의 불변식을 만족하는가(R2-02 — GUID 하나가 아니라 두뇌·행동 신원·명령까지). 목록은 다중집합으로 비교(중복도 다름).
        // 결측(null 목록·null/빈 GUID·음수 개수)·미등록 모드는 판정하지 않고 던진다(C2).
        public static bool ModeMatches(string brainGuid, IList<string> actionGuids, IList<string> availableGuids, int attackActions, int aiAttackCmds, int aiFollowWrongTarget, PackMode mode)
        {
            if (string.IsNullOrEmpty(brainGuid)) throw new ArgumentException("PackWarden: ModeMatches needs the brain GUID (missing brain = cannot judge)", "brainGuid");
            if (actionGuids == null) throw new ArgumentNullException("actionGuids");
            if (availableGuids == null) throw new ArgumentNullException("availableGuids");
            if (attackActions < 0) throw new ArgumentOutOfRangeException("attackActions", attackActions, "PackWarden: count must be >= 0");
            if (aiAttackCmds < 0) throw new ArgumentOutOfRangeException("aiAttackCmds", aiAttackCmds, "PackWarden: count must be >= 0");
            if (aiFollowWrongTarget < 0) throw new ArgumentOutOfRangeException("aiFollowWrongTarget", aiFollowWrongTarget, "PackWarden: count must be >= 0");
            string wantBrain = BrainGuidFor(mode); // 미등록 모드는 여기서 던진다
            var want = ExpectedActionGuids(mode);
            bool r = string.Equals(brainGuid, wantBrain, StringComparison.Ordinal)
                & SameMultiset(actionGuids, want, "actionGuids")
                & SameMultiset(availableGuids, want, "availableGuids")
                & attackActions == ExpectedAttackActions(mode)
                & aiFollowWrongTarget == 0;
            return r && (mode != PackMode.Shadow || aiAttackCmds == 0);
        }

        static bool SameMultiset(IList<string> got, string[] sortedWant, string what)
        {
            var g = new List<string>(got.Count);
            foreach (var s in got)
            {
                if (string.IsNullOrEmpty(s)) throw new ArgumentException("PackWarden: " + what + " has a null/empty GUID", what);
                g.Add(s);
            }
            g.Sort(StringComparer.Ordinal);
            if (g.Count != sortedWant.Length) return false;
            for (int i = 0; i < g.Count; i++) if (!string.Equals(g[i], sortedWant[i], StringComparison.Ordinal)) return false;
            return true;
        }

        // 명령 1개의 관측(R3-02). IsAi = AiAction != null · IsAttack = is UnitAttack · Interrupted = Result == ResultType.Interrupt · InQueue = 대기열 원소.
        public struct CmdObs { public bool IsAi, IsAttack, IsFinished, Interrupted, InQueue; }













        // ApplyMode ④: 두뇌 교체·목록 복원·AI 명령 중단 직후의 되읽기. 불변식 불일치 또는 AI 명령(공격·추종) 잔류 = 전환 실패 사유. null = 성공.
        public static string SwitchReadbackProblem(bool matches, int aiAttackCmds, int aiFollowCmds)
        {
            if (aiAttackCmds < 0) throw new ArgumentOutOfRangeException("aiAttackCmds", aiAttackCmds, "PackWarden: count must be >= 0");
            if (aiFollowCmds < 0) throw new ArgumentOutOfRangeException("aiFollowCmds", aiFollowCmds, "PackWarden: count must be >= 0");
            if (!matches) return "readback does not match the mode invariant";
            if (aiAttackCmds + aiFollowCmds != 0) return aiAttackCmds + " AI attack and " + aiFollowCmds + " AI follow command(s) survived the interrupt";
            return null;
        }





        // ---- 복제 원본 두뇌 전제(T1·T2 독립 검토 R17): 13종 원본 펫 m_Brain 은 본편 Character_Brain 이어야 한다(설계 D6 — 독립 모드 두뇌와 같음).
        // null = 전제 성립, 아니면 문제 문자열. GUID 형식 = BlueprintGuid.ToString()(소문자 32자 hex, Guids 와 같음) — 다른 표기는 「다름」으로 본다. ----
        public static string SourceBrainProblem(string brainGuid)
        {
            if (string.IsNullOrEmpty(brainGuid)) return "source brain missing";
            return string.Equals(brainGuid, Guids.Game.CharacterBrain, StringComparison.Ordinal) ? null : "source brain " + brainGuid + " is not Character_Brain " + Guids.Game.CharacterBrain;
        }
    }
}
