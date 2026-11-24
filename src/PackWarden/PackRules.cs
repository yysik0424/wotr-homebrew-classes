using System;

namespace PackWarden
{
    // 게임 타입 의존 없음. 수치 규칙의 단일 원장 (설계: docs\2026-11-06-pack-warden-design.md)
    public static class PackRules
    {
        // 설계 4.5: 보조 레벨 = 헌터 레벨 − 4, 최소 1
        public static int AuxLevel(int hunterLevel) { return Math.Max(1, hunterLevel - 4); }

        // ---- 보조 동물 수 (소유자 결정 2026-11-16) ----
        // 헌터 클래스 레벨 범위. 출처: 본편 헌터 = 20레벨 클래스(이 모드의 20레벨 능력 Aspects「두 상 동시 활성」, Checks\TestUnit 의 「20 을 넘으면 실패」와 같은 상한).
        public const int MinHunterLevel = 1, MaxHunterLevel = 20;

        // 소유자 결정 2026-11-16: 헌터 레벨 1~6 → 1마리, 7~12 → 2마리, 13~18 → 3마리, 19~20 → 4마리.
        // i 번째 값 = 보조가 (i+1)마리가 되는 첫 헌터 레벨. 마릿수의 유일한 원천 — AuxCount·AuxCountBands·AuxSlotCount 가 여기서 나온다.
        // (그 전 규칙 = 설계 4.5 「7레벨 이상이면 2, 아니면 1」 은 폐기.) 문자열·README 의 구간 문구는 시험이 AuxCountBands 와 대조한다.
        static readonly int[] AuxCountFirstLevels = { 1, 7, 13, 19 };

        // 소유자 결정 2026-11-16: 자리(슬롯) 수 = 표의 최대 마릿수. const 라서 표 길이에서 직접 못 얻는다 → 정적 생성자가 표와 대조해 어긋나면 던진다.
        // UnitPartPack.Auxiliaries 길이와 같아야 한다(UnitPartPack.Validate).
        public const int AuxSlotCount = 4;

        // 보조 자리 하나의 상태(검토 2026-11-17 지적 2). Empty = UniqueId 없음, Live = 해석된 유닛이 살아 있고 파괴되지 않았고 게임 안,
        // Gone = 해석은 됐는데 죽음·파괴·게임 밖(확인된 「대상 아님」), Unresolved = UniqueId 는 있는데 엔진이 유닛을 돌려주지 않음
        // (UnitReference.get_Value IL @0022~0056: 서비스·프록시·유닛 해석 실패를 예외 없이 null 로 돌려준다) — 「없음」 이 아니라 판정 불가.
        public enum SlotState { Unknown = 0, Empty, Live, Gone, Unresolved }
        // empty = UniqueId 가 비었는가, resolved = Value 가 유닛을 돌려줬는가, live = 그 유닛이 살아 있고 게임 안인가(resolved 일 때만 참일 수 있다). 모순된 입력은 던진다.
        public static SlotState ClassifySlot(bool empty, bool resolved, bool live)
        {
            if (live && !resolved) throw new ArgumentException("PackWarden: a slot cannot be live without a resolved unit", "live");
            if (empty) { if (resolved) throw new ArgumentException("PackWarden: an empty slot cannot resolve to a unit", "resolved"); return SlotState.Empty; }
            if (!resolved) return SlotState.Unresolved;
            return live ? SlotState.Live : SlotState.Gone;
        }

         
        // 규칙 소비자는 Live 만 보고 판정하지 않는다(UnitPartPack.LiveCount 는 불완전하면 던진다). 상태마다 수를 센다(Unknown 보초·모르는 값은 던진다).
        public sealed class LiveTally
        {
            public readonly int Live, Empty, Gone, Unresolved;
            public readonly int[] UnresolvedSlots;
            public bool Complete { get { return Unresolved == 0; } }
            LiveTally(int live, int empty, int gone, int[] unresolvedSlots) { Live = live; Empty = empty; Gone = gone; Unresolved = unresolvedSlots.Length; UnresolvedSlots = unresolvedSlots; }
            public static LiveTally Of(System.Collections.Generic.IList<SlotState> slots)
            {
                if (slots == null) throw new ArgumentNullException("slots");
                if (slots.Count != AuxSlotCount) throw new ArgumentException("PackWarden: a pack tally needs " + AuxSlotCount + " slot states, got " + slots.Count, "slots");
                int live = 0, empty = 0, gone = 0; var unresolved = new System.Collections.Generic.List<int>();
                for (int i = 0; i < slots.Count; i++)
                {
                    switch (slots[i])
                    {
                        case SlotState.Live: live++; break;
                        case SlotState.Empty: empty++; break;
                        case SlotState.Gone: gone++; break;
                        case SlotState.Unresolved: unresolved.Add(i); break;
                        default: throw new ArgumentOutOfRangeException("slots", slots[i], "PackWarden: slot " + i + " is not classified");
                    }
                }
                return new LiveTally(live, empty, gone, unresolved.ToArray());
            }
            // 확정된 살아 있는 수. 판정 불가 자리가 있으면 던진다(하한을 정확한 수로 읽지 않게).
            public int CompleteLiveCount()
            {
                if (!Complete) throw new InvalidOperationException("PackWarden: pack slot(s) " + string.Join(",", Array.ConvertAll(UnresolvedSlots, s => s.ToString())) + " hold an auxiliary id that does not resolve to a unit; the live count is unknown");
                return Live;
            }
        }

        // 2026-11-16 이전 저장의 자리 수(그때의 AuxSlotCount = 2, 커밋 8a54348 PackRules.cs). 옛 저장 이관(MigrateSavedSlots)만 쓴다.
        public const int LegacyAuxSlotCount = 2;

        static PackRules()
        {
            if (AuxCountFirstLevels.Length != AuxSlotCount)
                throw new InvalidOperationException("PackWarden: aux count table has " + AuxCountFirstLevels.Length + " rows but AuxSlotCount = " + AuxSlotCount);
            if (AuxCountFirstLevels[0] != MinHunterLevel)
                throw new InvalidOperationException("PackWarden: aux count table must start at hunter level " + MinHunterLevel + ", starts at " + AuxCountFirstLevels[0]);
            for (int i = 1; i < AuxCountFirstLevels.Length; i++)
                if (AuxCountFirstLevels[i] <= AuxCountFirstLevels[i - 1] || AuxCountFirstLevels[i] > MaxHunterLevel)
                    throw new InvalidOperationException("PackWarden: aux count table row " + i + " (level " + AuxCountFirstLevels[i] + ") is not ascending within " + MinHunterLevel + ".." + MaxHunterLevel);
        }

        // 헌터 레벨 → 보조 동물 수(표). 범위 밖 레벨(0 이하·21 이상)은 판정 불가 → 던진다(가장 가까운 행으로 조용히 맞추지 않는다).
        public static int AuxCount(int hunterLevel)
        {
            if (hunterLevel < MinHunterLevel || hunterLevel > MaxHunterLevel)
                throw new ArgumentOutOfRangeException("hunterLevel", hunterLevel, "PackWarden: hunter level must be " + MinHunterLevel + ".." + MaxHunterLevel);
            int n = 0;
            foreach (int first in AuxCountFirstLevels) if (hunterLevel >= first) n++;
            return n;
        }

        // 표의 구간: 행마다 { 첫 레벨, 끝 레벨, 마릿수 }. 문자열·README 대조 시험용(새 배열을 돌려준다 — 표를 밖에서 못 바꾼다).
        public static int[][] AuxCountBands()
        {
            var rows = new int[AuxCountFirstLevels.Length][];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = new[] { AuxCountFirstLevels[i], i + 1 < AuxCountFirstLevels.Length ? AuxCountFirstLevels[i + 1] - 1 : MaxHunterLevel, i + 1 };
            return rows;
        }

        // 설계 4.5: 7레벨부터 탈것 게이트 사용
        public static bool UsesMountGate(int hunterLevel) { return hunterLevel >= 7; }

        // 설계 4.3: 기본 크기가 중형 이상이어야 탑승 가능 (Kingmaker.Enums.Size 서수를 int 로 전달)
        public static bool IsRideableBaseSize(int sizeOrdinal, int mediumOrdinal) { return sizeOrdinal >= mediumOrdinal; }

        // 설계 4.5: 보조 레벨이 업그레이드 요구 레벨 이상이면 업그레이드 부여
        public static bool AuxGetsUpgrade(int auxLevel, int upgradeLevel) { return auxLevel >= upgradeLevel; }

        // 설계 4.6: 상 보너스 = 주능력치(지혜, 설계 4.1 — 2026-11-07 매력→지혜) 수정치, 음수는 0. 능력치 종류는 호출자(Aspects)가 정한다.
        public static int StatBonus(int statModifier) { return Math.Max(0, statModifier); }

        // 설계 4.6: 임시 HP = 상 보너스(지혜 수정치, 음수는 0) × 헌터 레벨
        public static int ToughTempHp(int statModifier, int hunterLevel) { return StatBonus(statModifier) * hunterLevel; }

        // ---- 재소집 (소유자 결정 2026-11-16: 대기 삭제) ----
        // 그 전 규칙(설계 4.5 「죽으면 그 자리는 게임 시간 1시간 뒤, 휴식하면 초기화」 — RespawnDelay·RespawnReady·UnitPartPack.RespawnReadyAt)은 없앴다.
        // 지금: 죽은 보조는 **그 전투 안에서는** 돌아오지 않고, **다음 전투에서** 바로 다시 나타난다.
        // 「그 전투에서 죽은 수」 는 저장하지 않는 전투 단위 상태다(게임은 전투 중 저장을 막는다 → 저장에 실릴 일이 없다).
        public sealed class CombatLosses
        {
            int fallen;

            // 이번 전투에서 죽은 보조 수(0..AuxSlotCount).
            public int Fallen { get { return fallen; } }

            // 보조 1마리 사망. 자리 수보다 많이 죽을 수는 없다 → 넘으면 사망 사건을 두 번 센 것이므로 던진다.
            public void RecordDeath()
            {
                if (fallen >= AuxSlotCount)
                    throw new InvalidOperationException("PackWarden: more auxiliary deaths recorded in one combat than slots (" + AuxSlotCount + ")");
                fallen++;
            }

            // 전투 종료: 다음 전투에서는 전원이 다시 나타난다.
            public void CombatEnded() { fallen = 0; }
        }

        // 멱등 소집 규칙. target = AuxCount(헌터 레벨), live[i] = 자리 i 에 살아 있는 보조가 있음, fallenThisCombat = 이번 전투에서 죽은 수.
        // 채울 수 = target − fallenThisCombat − (살아 있는 수). 빈 자리를 앞에서부터 골라 그만큼 돌려준다. 두 번 연달아 불러도 두 번째는 빈 배열.
        // 같은 전투에서 죽은 만큼은 다시 채우지 않는다(내렸다 다시 타도). fallenThisCombat = 0(새 전투·옛 저장)이면 target 까지 전부 채운다.
        public static int[] SlotsToFill(int target, bool[] live, int fallenThisCombat)
        {
            if (live == null) throw new ArgumentNullException("live");
            if (live.Length != AuxSlotCount)
                throw new ArgumentException("PackWarden: expected " + AuxSlotCount + " slots, got live=" + live.Length);
            if (target < 0 || target > AuxSlotCount)
                throw new ArgumentOutOfRangeException("target", target, "PackWarden: target must be 0.." + AuxSlotCount);
            // 죽은 수가 목표보다 많다 = 있을 수 없는 상태(목표보다 많이 소집된 적이 없다). 0 으로 내리누르지 않고 던진다.
            if (fallenThisCombat < 0 || fallenThisCombat > target)
                throw new ArgumentOutOfRangeException("fallenThisCombat", fallenThisCombat, "PackWarden: fallen-this-combat must be 0.." + target + " (target)");
            int liveCount = 0;
            foreach (bool b in live) if (b) liveCount++;
            int want = target - fallenThisCombat;
            var result = new System.Collections.Generic.List<int>();
            for (int i = 0; i < AuxSlotCount && liveCount + result.Count < want; i++)
                if (!live[i]) result.Add(i);
            return result.ToArray();
        }

        // 옛 저장 이관: 저장된 자리 목록을 지금 자리 수로 맞춘다. 지금 길이면 그대로 복사, 2026-11-16 이전 길이(LegacyAuxSlotCount)면 뒤를 빈 값(default)으로 채운다.
        // 그 밖의 길이·null 은 손상된 저장 → 던진다(잘라내거나 채워서 조용히 맞추지 않는다).
        public static T[] MigrateSavedSlots<T>(System.Collections.Generic.IList<T> saved)
        {
            if (saved == null) throw new ArgumentNullException("saved");
            if (saved.Count != AuxSlotCount && saved.Count != LegacyAuxSlotCount)
                throw new ArgumentException("PackWarden: saved pack has " + saved.Count + " slots; expected " + AuxSlotCount + " or the pre-2026-11-16 count " + LegacyAuxSlotCount, "saved");
            var result = new T[AuxSlotCount];
            for (int i = 0; i < saved.Count; i++) result[i] = saved[i];
            return result;
        }

        // 설계 4.7: 연구 대상에 대한 보조 동물 공격 보너스 +2
        public const int AuxStudiedTargetAttackBonus = 2;

        // 설계 4.5 해산 규칙 + 과제 8 IL 조사(task-8-report.md). 하차는 전부 private UnitPartRider.Dismount(bool force) 로 모인다.
        // 가장 바깥 호출의 진입로로 원인을 가른다(안쪽 호출 = ClearAndRemoveSelf 가 탑승 능력을 끄며 생기는 MountedBuff → ContextActionDismount 연쇄).
        public enum DismountCause
        {
            Voluntary,    // 탑승 토글(MountTargetSwitch) 끔 → MountedBuff Deactivated → ContextActionDismount → DismountForce()
            NoMountZone,  // Dismount() 무인자: 호출자는 TickDismount·DisableMountRiding.OnTurnOn 둘뿐(dnlib) → TickDismount 밖이면 기승 금지 구역(또는 다른 모드의 하차 핫키)
            Incapacity,   // SaddledUnitController.TickDismount 안: 탈것·기수 기절/사망/엎드림/삼켜짐/구덩이, 적대, 크기 불일치
            Scripted,     // 그 밖의 DismountForce(): 컷신·레벨업·리스펙·로드·펫 숨김 등
        }

        public static DismountCause ClassifyDismount(bool force, bool insideTickDismount, bool insideDismountAction)
        {
            if (insideTickDismount) return DismountCause.Incapacity;
            if (!force) return DismountCause.NoMountZone;
            return insideDismountAction ? DismountCause.Voluntary : DismountCause.Scripted;
        }

        // 실제 하차가 일어난 경우에만 해산 사유가 된다: 호출 전 탑승(wasMounted) ∧ 호출 후 비탑승(!mountedAfter).
        // wasMounted=false 는 "결측"이 아니라 "이미 내린 상태에서 다시 불린 하차"(연쇄 호출)다. mountedAfter=true 는 다른 패치가 원본을 취소한 경우.
        // Voluntary 의 상태 조건: 게임은 기수가 의식을 잃거나 행동 불가(CanAct=false)면 탑승 토글을 스스로 끈다
        // (UnitActivatableAbilitiesController.ShouldBeDisabled, DeactivateIfOwnerUnconscious/Disabled=true) — 그 경로도 ContextActionDismount 로 들어오므로 여기서 걸러 낸다.
        public static bool DismountDismisses(DismountCause cause, bool wasMounted, bool mountedAfter, bool leaderConscious, bool leaderCanAct, bool mountConscious)
        {
            switch (cause)
            {
                case DismountCause.NoMountZone:
                case DismountCause.Voluntary:
                case DismountCause.Incapacity:
                case DismountCause.Scripted:
                    break;
                default: throw new ArgumentOutOfRangeException("cause", cause, "PackWarden: unknown DismountCause");
            }
            if (!wasMounted || mountedAfter) return false;
            switch (cause)
            {
                case DismountCause.NoMountZone: return true;
                case DismountCause.Voluntary: return leaderConscious && leaderCanAct && mountConscious;
                default: return false; // Incapacity·Scripted = 유지
            }
        }

        // 보조 소멸 연출 프리팹 선택(2026-11-09, 수정 1 M1·M2). 크기 서수 = Kingmaker.Enums.Size 상수(Fine 0, Diminutive 1, Tiny 2, Small 3, Medium 4, Large 5,
        // Huge 6, Gargantuan 7, Colossal 8 — dnlib 2026-11-09; tests\PackRulesTests.cs 가 게임 DLL 과 대조). 게임 타입을 안 쓰려고 int 로 받는다.
        // 도달 가능한 보조 크기 = Small(2026-11-16 소유자 결정: 소형 고정). 2026-11-16 이전 = Medium(13종 기본 크기, 말 계열은 Large) · Large(7레벨 업그레이드). 그 밖(축소·거대화 효과 등)은 본편이 모델별로 지정하는 값을
        // 우리가 모르므로 null = 「대응 없음」을 돌려주고 호출자가 WARN 하고 재생하지 않는다(어느 한쪽으로 조용히 대체하지 않음). 서수 범위 밖은 예외.
        public const int SizeOrdinalFine = 0, SizeOrdinalSmall = 3, SizeOrdinalMedium = 4, SizeOrdinalLarge = 5, SizeOrdinalColossal = 8;

        // 소유자 결정 2026-11-16: 혼잡 완화를 위해 보조는 소형 고정(종의 기본 크기와 무관, 자라지 않음). 엔진 크기 변경 사실 ChangeUnitSize(Value) 로 건다(AuxUnits.Build).
        public const int AuxFixedSizeOrdinal = SizeOrdinalSmall;

        // 소유자 결정 2026-11-16: 보조는 적의 기회 공격을 받지 않는다. 본편 Grace 주문 버프(Spells/Level2/GraceBuff.jbp)의 AddCondition 과 같은 조건 이름.
        // 엔진 UnitCombatState.AttackOfOpportunity @01D2 가 대상의 HasCondition(이 값 = 35) 이면 공격하지 않는다(시험이 게임 DLL 에서 대조).
        public const string AuxAoOImmunityCondition = "ImmuneToAttackOfOpportunity";

         
        // 엔진의 실제 피해 경로(dnlib 2026-11-16): RuleCalculateWeaponStats.OnTrigger @03F0 → RuleCalculateWeaponSizeBonus.OnTrigger @0054~0077
        //   = WeaponDamageScaleTable.Scale(기본 주사위, WeaponSize, GetBaseDiceSize()). GetBaseDiceSize = 변신 중이 아니면 중형(@0000 ldc.i4.4).
        //   WeaponSize = Shift(무기 크기, m_SizeShift, Tiny, Colossal)(get_WeaponSize @0011~0013), 무기 크기 = 플레이어 진영 보유자의 State.Size.
        //   Scale 은 (기준 크기, 기본 주사위)가 든 **첫** 행에서 목표 크기 칸을 준다. 같은 크기면 그대로, 행이 없으면 오류 로그만 남기고 기본 주사위 그대로.
        // 재정의 무기의 블루프린트 크기(BlueprintItemWeapon.m_Size)는 이 경로에서 쓰이지 않는다(표시용 ItemEntityWeapon.DamageDice 만 쓴다).
        public const int WeaponDiceBaseSizeOrdinal = SizeOrdinalMedium;

        public enum DiceCheck { Unknown = 0, Match, Mismatch }

        // 엔진 WeaponDamageScaleTable..cctor 의 사본(행 순서 그대로, 칸 = Fine..Colossal, 「1」 = DiceFormula.One = 1d1). 출처: Assembly-CSharp dnlib 2026-11-16.
        // 사본인 까닭: 기대값을 엔진의 같은 함수로 구하면 「엔진이 변환을 못 하는 주사위」 를 일치로 읽게 된다. 시험이 이 표를 게임 DLL 의 .cctor 와 대조한다(어긋나면 실패).
        public static readonly string[] WeaponDamageScaleRows =
        {
            "1d1,1d1,1d1,1d1,1d1,1d2,1d3,1d4,1d6",
            "1d1,1d1,1d1,1d1,1d2,1d3,1d4,1d6,1d8",
            "1d1,1d1,1d1,1d2,1d3,1d4,1d6,1d8,2d6",
            "1d1,1d1,1d2,1d3,1d4,1d6,1d8,2d6,3d6",
            "1d1,1d2,1d3,1d4,1d6,1d8,2d6,3d6,4d6",
            "1d2,1d3,1d4,1d6,1d8,2d6,3d6,4d6,6d6",
            "1d2,1d3,1d4,1d6,2d4,2d6,3d6,4d6,6d6",
            "1d3,1d4,1d6,1d8,1d10,2d8,3d8,4d8,6d8",
            "1d4,1d6,1d8,1d10,1d12,3d6,4d6,6d6,8d6",
            "1d4,1d6,1d8,1d10,2d6,3d6,4d6,6d6,8d6",
            "1d4,1d6,1d10,1d10,2d8,3d8,4d8,6d8,8d8",
            "1d6,1d8,1d10,2d6,3d6,4d6,6d6,8d6,12d6",
            "1d8,1d10,2d6,2d8,2d10,4d8,6d8,8d8,12d8",
            "1d8,1d10,2d6,3d6,4d6,6d6,8d6,12d6,16d6",
            "1d10,1d10,3d6,2d12,3d10,4d10,6d10,8d10,12d10",
            "1d2,1d3,1d4,1d6,1d8,1d10,2d8,3d8,4d8",
            "1d1,1d2,1d3,1d4,1d6,1d8,1d10,2d8,3d8",
        };

        static void RequireSizeOrdinal(string name, int ordinal)
        {
            if (ordinal < SizeOrdinalFine || ordinal > SizeOrdinalColossal)
                throw new ArgumentOutOfRangeException(name, ordinal, "PackWarden: not a Kingmaker.Enums.Size ordinal (" + SizeOrdinalFine + ".." + SizeOrdinalColossal + ")");
        }

        // 엔진 Scale 과 같은 규칙. false = 변환 행 없음(엔진은 기본 주사위를 그대로 쓴다 — 호출자는 이것을 판정 불가로 다룬다).
        public static bool ExpectedScaledDice(int baseRolls, int baseSides, int baseSizeOrdinal, int targetSizeOrdinal, out int rolls, out int sides)
        {
            RequireSizeOrdinal("baseSizeOrdinal", baseSizeOrdinal);
            RequireSizeOrdinal("targetSizeOrdinal", targetSizeOrdinal);
            rolls = baseRolls; sides = baseSides;
            if (baseSizeOrdinal == targetSizeOrdinal) return true;
            string key = baseRolls + "d" + baseSides;
            foreach (var row in WeaponDamageScaleRows)
            {
                var cells = row.Split(',');
                if (cells.Length != SizeOrdinalColossal + 1) throw new InvalidOperationException("PackWarden: WeaponDamageScaleRows row has " + cells.Length + " cells: " + row);
                if (cells[baseSizeOrdinal] != key) continue;
                var parts = cells[targetSizeOrdinal].Split('d');
                rolls = int.Parse(parts[0]); sides = int.Parse(parts[1]);
                return true;
            }
            rolls = 0; sides = 0;
            return false;
        }

        // 엔진 WeaponSizeExtension.Shift(크기, 이동, Tiny, Colossal) 사본(RuleCalculateWeaponSizeBonus.get_WeaponSize 가 쓰는 한계).
        public const int SizeOrdinalTiny = 2;
        public static int ShiftedWeaponSizeOrdinal(int sizeOrdinal, int shift)
        {
            RequireSizeOrdinal("sizeOrdinal", sizeOrdinal);
            int v = sizeOrdinal + shift;
            return v < SizeOrdinalTiny ? SizeOrdinalTiny : v > SizeOrdinalColossal ? SizeOrdinalColossal : v;
        }

        // 소형 고정이 없었을 때의 크기 = 종의 블루프린트 기본 크기 + (그 레벨에서 성장이 주었을 크기 증가). 복원 단계 = 그 크기 − 소형.
        // 종별 숫자는 없다: 호출자가 블루프린트·성장 특성에서 읽은 값을 넣는다. 기본 크기를 못 읽었거나(범위 밖) 소형보다 작은 종·음수 성장·Colossal 초과는
        // 0 단계로 넘기지 않고 던진다(잃은 주사위를 되돌리는 규칙이 정의되지 않은 입력).
        public static int AuxDiceRestoreSteps(int blueprintSizeOrdinal, int growthSizeDelta)
        {
            RequireSizeOrdinal("blueprintSizeOrdinal", blueprintSizeOrdinal);
            if (growthSizeDelta < 0) throw new ArgumentOutOfRangeException("growthSizeDelta", growthSizeDelta, "PackWarden: growth size change must be 0 or more");
            int original = blueprintSizeOrdinal + growthSizeDelta;
            if (original > SizeOrdinalColossal) throw new ArgumentOutOfRangeException("growthSizeDelta", growthSizeDelta, "PackWarden: original size " + original + " is beyond Colossal");
            if (original < AuxFixedSizeOrdinal) throw new ArgumentOutOfRangeException("blueprintSizeOrdinal", blueprintSizeOrdinal, "PackWarden: original size " + original + " is below the fixed auxiliary size " + AuxFixedSizeOrdinal + " (no dice restore is defined)");
            return original - AuxFixedSizeOrdinal;
        }

        // 소환 끝 되읽기: 엔진이 실제로 굴릴 주사위(actual) == 기본 주사위를 원래 크기까지 변환한 값(중형 기준). 못 읽었거나 변환 행이 없으면 Unknown — Match 로 넘기지 않는다.
        public static DiceCheck CheckAuxDice(int? actualRolls, int? actualSides, int? baseRolls, int? baseSides, int? originalSizeOrdinal, out string detail)
        {
            if (actualRolls == null || actualSides == null || baseRolls == null || baseSides == null || originalSizeOrdinal == null)
            {
                detail = "UNKNOWN: dice or size unreadable (actual " + (actualRolls == null || actualSides == null ? "?" : actualRolls + "d" + actualSides)
                    + ", base " + (baseRolls == null || baseSides == null ? "?" : baseRolls + "d" + baseSides) + ", original size " + (originalSizeOrdinal == null ? "?" : originalSizeOrdinal.ToString()) + ")";
                return DiceCheck.Unknown;
            }
            int r, s;
            if (!ExpectedScaledDice(baseRolls.Value, baseSides.Value, WeaponDiceBaseSizeOrdinal, originalSizeOrdinal.Value, out r, out s))
            {
                detail = "UNKNOWN: no engine scale row for base " + baseRolls + "d" + baseSides + " (the engine would leave it unscaled); rolled " + actualRolls + "d" + actualSides;
                return DiceCheck.Unknown;
            }
            bool ok = r == actualRolls.Value && s == actualSides.Value;
            detail = "expected " + r + "d" + s + " (base " + baseRolls + "d" + baseSides + " at size " + originalSizeOrdinal + "), rolled " + actualRolls + "d" + actualSides;
            return ok ? DiceCheck.Match : DiceCheck.Mismatch;
        }

        public static string DissolveFxFor(int sizeOrdinal)
        {
            if (sizeOrdinal < SizeOrdinalFine || sizeOrdinal > SizeOrdinalColossal)
                throw new ArgumentOutOfRangeException("sizeOrdinal", sizeOrdinal, "PackWarden: not a Kingmaker.Enums.Size ordinal (" + SizeOrdinalFine + ".." + SizeOrdinalColossal + ")");
            // 2026-11-16: 보조가 소형 고정이 되어 소형을 표에 넣는다(넣지 않으면 소멸 FX 가 늘 생략된다). 본편에 소형 동물 소환체의 용해 프리팹 선례를 고르지 않고
            // 가장 가까운 중형(표범) 프리팹을 쓴다 — 임의 선택, README 눈 점검.
            if (sizeOrdinal == SizeOrdinalSmall) return Guids.Game.SummonDissolveFxMedium;
            if (sizeOrdinal == SizeOrdinalMedium) return Guids.Game.SummonDissolveFxMedium;
            if (sizeOrdinal == SizeOrdinalLarge) return Guids.Game.SummonDissolveFxLarge;
            return null;
        }

        // 설계 4.5: 기승 금지 구역 진입 = 해산. 탑승 여부와 무관하다(이미 강제 하차해 무리를 유지 중인 지도자도 해산 — 과제 8 수정 1).
        // DisableMountRiding.OnTurnOn 후크가 파티원마다 이 규칙을 쓴다. 7레벨 미만(토글 경로)은 대상이 아니다.
        public static bool NoMountZoneEntryDismisses(bool hasCallFeature, int hunterLevel)
        {
            return hasCallFeature && UsesMountGate(hunterLevel);
        }
    }

    // 「무리의 주 동물 동료」 선택 화면의 설명문(소유자 피드백 2026-11-17 「선택 화면 설명이 한 줄뿐」). 게임 형식 의존 없음 — 시험 exe 가 직접 실행한다.
    // 수치는 전부 규칙 원천(PackRules · Guids.Game 의 표)에서 계산한다: 틀에 숫자를 직접 적지 않는다. 선택지 13종의 문구는 본편 것이라 여기서 만들지 않는다.
    public static class CompanionText
    {
        // Strings 표의 키(접두사 없이). 메서드로 준다(Strings 의 정적 생성자에 ".Description" 리터럴이 생기지 않게 — 시험이 그 리터럴 뒤의 모양을 본다).
        public static string DescriptionKey() { return "CompanionSelection" + ".Description"; }

        // 크기 필터가 빼는 본편 3종의 표기: { 특성 GUID, 한글 패치 표기(WrathKoreanMod translation.json 「동물 동료 — …」 2026-11-17), 영어 표기(enGB) }.
        // Guids.Game.SizeExcludedCompanions 의 행마다 표기가 있어야 한다(없으면 던진다 — 이름 없는 줄을 내보내지 않는다).
        static readonly string[][] ExcludedNames =
        {
            new[] { Guids.Game.AnimalCompanionFeatureLeopard, "표범", "leopard" },
            new[] { Guids.Game.AnimalCompanionFeatureMonitor, "왕도마뱀", "monitor lizard" },
            new[] { Guids.Game.AnimalCompanionFeatureVelociraptor, "벨로시랩터", "velociraptor" },
        };
        // 고를 수는 있지만 본편 규칙으로 탈 수 없는 종(Guids.Game.PackCompanions 의 Name). 근거(blueprints.zip 2026-11-17): Classes/Pets/AnimalCompanionUnitCentipede.jbp 의 m_AddFacts 에
        // Traits/Races/Common/VerminType.jbp(09478937…)가 있고, 본편 Units/Abilities/Mounts/MountTargetAbility.jbp 의 AbilityTargetHasFact(Inverted)가 그 사실을 가진 대상을 거절한다
        // (본편 설명문도 "This animal companion can't be used as a mount."). 헌터 7레벨부터의 소집 조건은 주 동물에 탄 상태(PackCall.ShouldSummonCore)라 그 뒤로는 보조를 부를 수 없다.
        // 시험이 13종 전부를 게임 데이터와 대조한다(이 목록 == 탈 수 없는 종). 코드 동작은 바꾸지 않았다 — 보고 대상(2026-11-17).
        public static readonly string[] UnmountableCompanions = { "AnimalCompanionFeatureCentipede" };
        public const string VerminTypeFact = "09478937695300944a179530664e42ec"; // Traits/Races/Common/VerminType.jbp
        static readonly string[][] UnmountableNames = { new[] { "AnimalCompanionFeatureCentipede", "쉿쉿거리는 지네", "hissing centipede" } };

        static string[] Row(string[][] table, string key)
        {
            foreach (var r in table) if (r[0] == key) return r;
            throw new InvalidOperationException("PackWarden: no player text for companion " + key);
        }
        // 탈것 게이트가 켜지는 첫 헌터 레벨(PackRules.UsesMountGate 에서 찾는다). 전 구간에서 꺼져 있거나 첫 레벨부터 켜져 있으면 「토글 → 기승」 문장이 틀리므로 던진다.
        public static int FirstMountGateLevel()
        {
            for (int lv = PackRules.MinHunterLevel; lv <= PackRules.MaxHunterLevel; lv++)
                if (PackRules.UsesMountGate(lv))
                {
                    if (lv == PackRules.MinHunterLevel) throw new InvalidOperationException("PackWarden: the mount gate is on from the first hunter level (the text describes a toggle period before it)");
                    for (int later = lv; later <= PackRules.MaxHunterLevel; later++)
                        if (!PackRules.UsesMountGate(later)) throw new InvalidOperationException("PackWarden: the mount gate turns off again at hunter level " + later);
                    return lv;
                }
            throw new InvalidOperationException("PackWarden: the mount gate never turns on within hunter levels " + PackRules.MinHunterLevel + ".." + PackRules.MaxHunterLevel);
        }
        // 보조 레벨 = max(최소, 헌터 레벨 − 차). 차·최소는 PackRules.AuxLevel 에서 얻고, 전 구간에서 그 식이 성립하는지 대조한다(아니면 던진다).
        public static void AuxLevelRule(out int offset, out int minimum)
        {
            offset = PackRules.MaxHunterLevel - PackRules.AuxLevel(PackRules.MaxHunterLevel);
            minimum = PackRules.AuxLevel(PackRules.MinHunterLevel);
            for (int lv = PackRules.MinHunterLevel; lv <= PackRules.MaxHunterLevel; lv++)
                if (PackRules.AuxLevel(lv) != Math.Max(minimum, lv - offset))
                    throw new InvalidOperationException("PackWarden: auxiliary level at hunter level " + lv + " is not max(" + minimum + ", level - " + offset + ")");
        }
        // 크기 표기(한글 패치: Small 소형 · Medium 중형). 보조의 고정 크기가 소형이 아니게 되면 문구를 고쳐야 하므로 던진다. 선택 필터 = 기본 크기 중형 이상(Archetype.Build · PackRules.IsRideableBaseSize).
        static void SizeWords(out string fixedKo, out string fixedEn, out string filterKo, out string filterEn)
        {
            if (PackRules.AuxFixedSizeOrdinal != PackRules.SizeOrdinalSmall) throw new NotSupportedException("PackWarden: no player text for auxiliary size ordinal " + PackRules.AuxFixedSizeOrdinal);
            fixedKo = "소형"; fixedEn = "Small"; filterKo = "중형"; filterEn = "Medium";
        }

        public static string DescriptionKo()
        {
            int gate = FirstMountGateLevel(), offset, minimum;
            AuxLevelRule(out offset, out minimum);
            string smallKo, smallEn, mediumKo, mediumEn;
            SizeWords(out smallKo, out smallEn, out mediumKo, out mediumEn);
            var excluded = new System.Collections.Generic.List<string>();
            foreach (var g in Guids.Game.SizeExcludedCompanions) excluded.Add(Row(ExcludedNames, g)[1]);
            var bands = new System.Collections.Generic.List<string>();
            foreach (var r in PackRules.AuxCountBands()) bands.Add(r[0] + "~" + r[1] + "레벨 " + r[2] + "마리");
            var lines = new System.Collections.Generic.List<string>
            {
                "무리의 주축이 될 주 동물 동료를 고른다. 보조 동물은 이 동물과 같은 종으로 나타난다.",
                "고를 수 있는 동물: 기본 크기가 " + mediumKo + " 이상인 본편 동물 동료 " + Guids.Game.PackCompanions.Length + "종.",
                "고를 수 없는 동물: " + string.Join(", ", excluded.ToArray()) + "(기본 크기가 " + smallKo + "이라 제외). 다른 모드가 추가한 동물 동료도 고를 수 없다.",
                "보조 동물의 수: 헌터 " + string.Join(", ", bands.ToArray()) + ". 레벨은 헌터 레벨 − " + offset + "(최소 " + minimum + ")이다.",
                "보조 동물은 전투 중에만 나타나고, " + smallKo + "으로 나타나 자라지 않는다.",
                "소집 조건: 헌터 " + PackRules.MinHunterLevel + "~" + (gate - 1) + "레벨에는 무리 소집 토글을 켜 두면, " + gate + "레벨부터는 전투 중 이 동물에 타고 있으면 보조 동물이 나타난다(탄 채 전투에 들어가도, 전투 중에 타도 된다).",
            };
            foreach (var name in UnmountableCompanions)
                lines.Add("주의: " + Row(UnmountableNames, name)[1] + "는 본편에서 탈 수 없는 동물이라, 헌터 " + gate + "레벨부터는 보조 동물을 부를 수 없다.");
            return string.Join("\n", lines.ToArray());
        }
        public static string DescriptionEn()
        {
            int gate = FirstMountGateLevel(), offset, minimum;
            AuxLevelRule(out offset, out minimum);
            string smallKo, smallEn, mediumKo, mediumEn;
            SizeWords(out smallKo, out smallEn, out mediumKo, out mediumEn);
            var excluded = new System.Collections.Generic.List<string>();
            foreach (var g in Guids.Game.SizeExcludedCompanions) excluded.Add(Row(ExcludedNames, g)[2]);
            if (excluded.Count < 2) throw new InvalidOperationException("PackWarden: the excluded companion sentence needs at least two entries");
            var bands = new System.Collections.Generic.List<string>();
            foreach (var r in PackRules.AuxCountBands()) bands.Add(r[2] + " at " + (bands.Count == 0 ? "hunter " : "") + "levels " + r[0] + "-" + r[1]);
            if (bands.Count < 2) throw new InvalidOperationException("PackWarden: the auxiliary count sentence needs at least two bands");
            var lines = new System.Collections.Generic.List<string>
            {
                "Choose the main animal companion that anchors the pack. The auxiliary animals appear as the same species.",
                "Available: the " + Guids.Game.PackCompanions.Length + " base-game animal companions whose base size is " + mediumEn + " or larger.",
                "Not available: " + string.Join(", ", excluded.GetRange(0, excluded.Count - 1).ToArray()) + " and " + excluded[excluded.Count - 1] + " (their base size is " + smallEn + "). Animal companions added by other mods are not available either.",
                "Number of auxiliary animals: " + string.Join(", ", bands.GetRange(0, bands.Count - 1).ToArray()) + " and " + bands[bands.Count - 1] + ". Their level is hunter level − " + offset + " (minimum " + minimum + ").",
                "Auxiliary animals appear only during combat, at " + smallEn + " size, and do not grow.",
                "Call condition: at hunter levels " + PackRules.MinHunterLevel + "-" + (gate - 1) + " the Call the Pack toggle must be on; from level " + gate + " they appear while the hunter rides this animal in combat (entering combat mounted or mounting during combat both work).",
            };
            foreach (var name in UnmountableCompanions)
                lines.Add("Warning: the " + Row(UnmountableNames, name)[2] + " cannot be ridden in the base game, so from hunter level " + gate + " it cannot call auxiliary animals.");
            return string.Join("\n", lines.ToArray());
        }
        // 로드 때 한 번 만들어 본다: 표기 누락·규칙 전제 위반이면 여기서 던진다(Strings 의 정적 생성자가 이 형식을 처음 건드린다).
        static CompanionText()
        {
            foreach (var name in UnmountableCompanions)
            {
                bool listed = false;
                foreach (var r in Guids.Game.PackCompanions) if (r.Name == name) listed = true;
                if (!listed) throw new InvalidOperationException("PackWarden: " + name + " is named as unmountable but is not a pack companion");
            }
            if (DescriptionKo().Split('\n').Length != DescriptionEn().Split('\n').Length)
                throw new InvalidOperationException("PackWarden: the companion selection description differs in line count between Korean and English");
        }
    }
}
