using System;
using System.Collections.ObjectModel;

namespace PhantomBlade
{
    // 게임 타입 의존 없음. 스탠드 수치 규칙의 단일 원장 (설계: docs/2026-11-07-phantom-blade-design.md)
    public static class StandRules
    {
        // 설계 §4.4(2레벨: 발현 토글 + 흡수 풀) / §4.5 교환표 2행
        public const int StandLevel = 2;
        // 러시가 생기는 레벨 = RushTable 첫 행(설계 §13.1, §4.5 교환표 3행). 상수로 두는 이유: IL 시험이 StandOwner.Level 의 최소 레벨 인자를 정수 리터럴로 본다.
        // 표와 어긋나면 시험(rushtable: first row level == StandRules.RushLevel)이 빨강.
        public const int RushLevel = 3;
        // 설계 §4.3 「스탠드 전력 공격」(15레벨) / §4.5 교환표 15행
        public const int FullAttackLevel = 15;
         
        // 스탠드가 자기 근접 접촉 굴림(BAB + 매력 vs 접촉 AC)을 한다. 그 아래는 6b 규칙(첫 N 명중에 얹힘). 판정 = PlanStrike(ownerHit), 실행 = StandTouchStrike.
        public const int TouchLevel = 10;
        public const int RangedGuardLevel = 6;     // 설계 §4.4: 6레벨 원거리 회피 +2
        public const int RangedGuard2Level = 14;   // 설계 §4.4: 14레벨 원거리 회피 +4
        // (소유자 결정 2026-11-16, 설계 §14: 10레벨 「재충전」(전투 종료 시 풀 회복)은 폐지 — RefillLevel 상수 삭제. 풀은 휴식·레벨업 때만 가득 찬다.)
        public const int Absorb75Level = 18;       // 설계 §4.4: 18레벨 흡수 75%
        // 소유자 결정 2026-11-16: 전투 뒤 회복을 없애는 대신 3배 (설계 §14). 풀 최대치 = 레벨 × max(1, 매력 수정치) × 이 값.
        // 읽는 곳: PoolMax(아래), StandBlueprints.BuildProperties(PoolProperty 의 곱셈 시작값), 문구 시험(CheckStringNumbers), 점검 S2·S5·S9 리터럴 대조.
        public const int PoolMultiplier = 3;
         
        public const int ArmorMasteryRemovedLevel = 19;

        // 전사 레벨 범위(본편 클래스 최대 20 — PbTestUnit.Create 도 1..20 만 받는다). 밖이면 「모르는 레벨」이라 던진다.
        public const int MinFighterLevel = 1;
        public const int MaxFighterLevel = 20;

         
        // 20레벨에서 끝난다(본편 몽크 맨손 표 L20 행, 전사 진행표 20). 그보다 높은 전사 레벨은 20 행을 쓴다.
        public const int TableMaxLevel = MaxFighterLevel;
        // 엔진 최대 캐릭터 레벨: 레전드(LegendaryHero)면 40, 아니면 20 — UnitProgressionData.get_MaxCharacterLevel IL 0010~0021(dnlib 2026-11-09).
        // 전사 클래스 레벨은 정상 진행으로는 20 을 넘지 못한다(BlueprintCharacterClass.MeetsPrerequisites IL 0010~001C, 치트 IgnorePrerequisites 만 예외).
        public const int MaxLegendLevel = 40;

        // 전사 레벨 → 표 레벨. 1..20 그대로, 21..MaxLegendLevel → TableMaxLevel, 그 밖은 모르는 레벨이라 던진다. 게임 쪽은 StandOwner.Level 한 곳에서만 부른다
        // (러시·귀신참·흡수·원거리 방어가 모두 그 결과를 쓴다). 20 을 넘었으면 StandOwner 가 유닛마다 한 번 알린다.
        public static int TableLevel(int fighterLevel)
        {
            Require(fighterLevel >= MinFighterLevel && fighterLevel <= MaxLegendLevel, "fighterLevel", fighterLevel);
            return Math.Min(fighterLevel, TableMaxLevel);
        }

        // 러시 표 한 행: 이 레벨부터 라운드당 타수(= 본체의 그 라운드 첫 Strikes 번 근접 명중이 각각 스탠드 1타).
        public struct RushRow
        {
            public readonly int Level, Strikes;
            public RushRow(int level, int strikes) { Level = level; Strikes = strikes; }
        }

        // 주사위 표 한 행: FromLevel 부터 다음 행 전까지 주먹 피해 Count d Size.
        public struct RushDiceRow
        {
            public readonly int FromLevel, Count, Size;
            public RushDiceRow(int fromLevel, int count, int size) { FromLevel = fromLevel; Count = count; Size = size; }
        }

         
        // 레벨 = 갑옷 훈련 자리 3·7·11·15 + 비어 있던 갑옷 숙달 자리 19 — Archetype 레벨표(StandBlueprints.AddRushFeatures)와 ArmorTrainingLayout.Levels 가 이 표를 읽는다.
        public static readonly ReadOnlyCollection<RushRow> RushTable = new ReadOnlyCollection<RushRow>(new[]
        {
            new RushRow(3, 1),
            new RushRow(7, 2),
            new RushRow(11, 3),
            new RushRow(15, 4),
            new RushRow(19, 5),
        });

         
        // 본편 근거(blueprints.zip 2026-11-09 직독): Classes/Monk/MonkProgression.jbp (8a91753b978e3b34b9425419179aafd6) LevelEntries 의
        //   L1 MonkUnarmedStrike(c3fbeb2f…) → Weapons/Items/Standard/NaturalWeapon/Unarmed1d6.jbp (덮어쓰기 없음 → Weapons/Types/Special/Unarmed.jbp m_BaseDamage 1d6)
        //   L4 MonkUnarmedStrikeLevel4(8267a069…) → Unarmed1d8 · L8 …Level8(f790a36b…) → Unarmed1d10 · L12 …Level12(b3889f44…) → Unarmed2d6
        //   L16 …Level16(078636a2…) → Unarmed2d8 · L20 …Level20(df38e56f…) → Unarmed2d10 (각 특성의 EmptyHandWeaponOverride.m_Weapon, 무기 m_Size Medium).
         
        public static readonly ReadOnlyCollection<RushDiceRow> RushDiceByLevel = new ReadOnlyCollection<RushDiceRow>(new[]
        {
            new RushDiceRow(1, 1, 6),
            new RushDiceRow(4, 1, 8),
            new RushDiceRow(8, 1, 10),
            new RushDiceRow(12, 2, 6),
            new RushDiceRow(16, 2, 8),
            new RushDiceRow(20, 2, 10),
        });

         
        public const int NormalStrikesPerHit = 1;
         
        public const int FullAttackStrikesPerHit = 2;

        static void Require(bool ok, string name, int value)
        {
            if (!ok) throw new ArgumentOutOfRangeException(name, value, name + " out of range");
        }

        static void RequireKnownLevel(int level) { Require(level >= MinFighterLevel && level <= MaxFighterLevel, "level", level); }

        // 그 레벨의 러시 표 행. 3 상태: 행 = 러시 있음 / null = 레벨은 알고 3 미만이라 러시 없음(정당) / 던짐 = 모르는 레벨(1..20 밖).
        public static RushRow? RowFor(int level)
        {
            RequireKnownLevel(level);
            RushRow? hit = null;
            foreach (var r in RushTable) if (r.Level <= level) hit = r;
            return hit;
        }

        // 그 레벨의 주먹 주사위(1..20 전부 정의됨 — 본편 표가 1레벨부터 있다). 모르는 레벨이면 던진다.
        public static RushDiceRow DiceFor(int level)
        {
            RequireKnownLevel(level);
            RushDiceRow? hit = null;
            foreach (var d in RushDiceByLevel) if (d.FromLevel <= level) hit = d;
            if (!hit.HasValue) throw new InvalidOperationException("PhantomBlade: RushDiceByLevel has no row at or below level " + level);
            return hit.Value;
        }

        // 러시 주먹 주사위 수·면(타격 시점의 현재 전사 레벨). 러시가 없는 레벨(< RushLevel)에서 부르면 호출자 결함 — 던진다.
        public static int RushDiceCount(int level) { RequireRush(level); return DiceFor(level).Count; }
        public static int RushDieSize(int level) { RequireRush(level); return DiceFor(level).Size; }

        // 라운드당 러시 타수(RushTable). 러시가 없는 레벨(< RushLevel)·모르는 레벨이면 던진다.
        public static int RushStrikes(int level)
        {
            RequireRush(level);
            var row = RowFor(level);
            if (!row.HasValue) throw new InvalidOperationException("PhantomBlade: RushTable has no row at level " + level + " although level >= RushLevel " + RushLevel + " (table and constant disagree)");
            return row.Value.Strikes;
        }

        static void RequireRush(int level) { RequireKnownLevel(level); Require(level >= RushLevel, "level", level); }

        // 이번 명중이 스탠드 타격을 일으키는가(StandRushState.TryConsume 이 부른다). usedThisRound = 이번 라운드에 이미 일으킨 명중 수(전력 공격 라운드 것은 세지 않는다).
        // 전력 공격 라운드: 라운드 제한이 풀린다(설계 §4.3) — 그 버프는 FullAttackLevel 특성에서만 오므로 그 아래 레벨이면 던진다.
        public static bool MayStrike(int usedThisRound, int level, bool fullAttackRound)
        {
            Require(usedThisRound >= 0, "usedThisRound", usedThisRound);
            int n = RushStrikes(level); // 레벨 검증(러시 없는·모르는 레벨이면 던짐)
            if (fullAttackRound)
            {
                Require(level >= FullAttackLevel, "level", level);
                return true;
            }
            return usedThisRound < n;
        }

        // 이 레벨의 러시가 스탠드 자기 접촉 굴림인가(TouchLevel 이상). 러시가 없는 레벨(< RushLevel)·모르는 레벨이면 던진다 — 「얹힘」으로 조용히 떨어지지 않는다.
        public static bool IsTouchLevel(int level)
        {
            RequireRush(level);
            return level >= TouchLevel;
        }

        // 본체 공격 하나의 러시 계획(PlanStrike 의 결과). 허용이 아니면 타수·주사위는 0, Touch 는 false. UsedAfter·FirstUsedAtAfter = 저장할 새 라운드 상태.
        // Touch = 이 타격들이 스탠드 자기 근접 접촉 굴림(StandTouchStrike)인가 — 아니면 6b 의 얹힘 타격(RuleAttackWithWeapon).
        public struct RushPlan
        {
            public readonly bool Allowed, Touch;
            public readonly int Strikes, DiceCount, DieSize, UsedAfter;
            public readonly TimeSpan FirstUsedAtAfter;
            public RushPlan(bool allowed, int strikes, int diceCount, int dieSize, int usedAfter, TimeSpan firstUsedAtAfter, bool touch)
            {
                Allowed = allowed; Strikes = strikes; DiceCount = diceCount; DieSize = dieSize; UsedAfter = usedAfter; FirstUsedAtAfter = firstUsedAtAfter; Touch = touch;
                if (touch && !allowed) throw new InvalidOperationException("PhantomBlade: RushPlan marked touch but not allowed");
            }
        }

        // 한 명중의 러시 결정 + 라운드 상태 전이(순수, fix 1 — 리뷰 I1·I2·M1). StandRushState.TryConsume 이 게임 값을 넣어 부르고, RunAction 이 결과로 친다.
        //   used / firstUsedAt = 이번 라운드에 친 명중 수 / 그 라운드 「첫」 소모의 게임 시각, now = 지금 게임 시각, roundLength = 엔진 1 라운드(6 초),
        //   turnBased = 턴제 전투 중(시간 경계 없음 — 턴 시작 이벤트만, fix round 2 R2), level = 현재 전사 레벨(주사위도 이 레벨), fullAttackRound = 전력 공격 버프.
         
        // ① 쓴 수 > 0, 턴제 아님, 이벤트 대기 아님, now − firstUsedAt ≥ 1 라운드 → 새 라운드(쓴 수 0). 기준은 첫 소모(마지막 소모 아님 — 설계 §13.1).
         
        //    t=4 시퀀스 → t=6.1 에 시퀀스 도중 열림). 시퀀스 중에는 표준 쿨다운 > 0 이라 이벤트가 반드시 오므로 그동안 시간 경계를 닫는다.
        // ② 전력 공격: 늘 허용, FullAttackStrikesPerHit 타, 세지 않음. 평상: MayStrike(쓴 수 < N)면 1타, 쓴 수 +1, 0→1 이면 첫 소모 시각 = now.
        // ③ 주사위 = DiceFor(level). 기록이 깨졌으면(시각 역행·음수·라운드 길이 ≤ 0) 던진다.
        // ④ Task 6d(귀신 접촉): ownerHit = 이번 본체 공격이 명중했는가(트리거는 빗나감에도 온다 — OnlyHit=false). TouchLevel 미만에서 빗나감 = 거부·세지 않음(6b 「첫 N 명중」 그대로).
        //    TouchLevel 이상 = 명중·빗나감 같은 규칙(그 라운드 첫 N 「공격」), 허용이면 Touch = true. 레벨·전력 공격 검증은 빗나감에도 먼저 한다(조용히 건너뛰지 않음).
        public static RushPlan PlanStrike(int used, TimeSpan firstUsedAt, TimeSpan now, TimeSpan roundLength, bool turnBased, bool roundEventPending, int level, bool fullAttackRound, bool ownerHit)
        {
            Require(used >= 0, "used", used);
            if (turnBased && roundEventPending) throw new InvalidOperationException("PhantomBlade: roundEventPending is a real-time notion but turnBased is set (RoundEventPending returns false in turn-based combat)");
            if (roundLength <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("roundLength", roundLength, "roundLength must be positive");
            if (used > 0 && now < firstUsedAt) throw new InvalidOperationException("PhantomBlade: rush round state is broken: game time " + now + " is before the round's first use " + firstUsedAt);
            bool touch = IsTouchLevel(level); // 레벨 검증(러시 없는·모르는 레벨이면 던짐)
            if (fullAttackRound) Require(level >= FullAttackLevel, "level", level);
            int u = used;
            TimeSpan first = firstUsedAt;
            if (u > 0 && !turnBased && !roundEventPending && now - first >= roundLength) u = 0;
            // 정당한 거부: 10 미만 러시는 명중에만 얹힌다(설계 §13.1·§13.3) — 빗나감은 라운드 타수를 쓰지 않는다.
            if (!touch && !ownerHit) return new RushPlan(false, 0, 0, 0, u, first, false);
            if (!MayStrike(u, level, fullAttackRound)) return new RushPlan(false, 0, 0, 0, u, first, false);
            if (!fullAttackRound)
            {
                if (u == 0) first = now;
                u++;
            }
            var dice = DiceFor(level);
            return new RushPlan(true, StrikesPerHit(fullAttackRound), dice.Count, dice.Size, u, first, touch);
        }

         
        // 엔진 Kingmaker.RuleSystem.Rules.AttackResult 의 거울(이름·값 = Assembly-CSharp SHA-256 2CB7160B… dnlib 2026-11-09). 시험이 엔진 상수와 대조한다(C4).
        public enum RollResult { Unknown = 0, Hit = 1, CriticalHit = 2, Miss = 3, DodgeAC = 4, ArmorAC = 5, ShieldAC = 6, MirrorImage = 7, Concealment = 8, Parried = 9 }

        // 굴림 뒤 판정: true = 명중(피해), false = 정당한 빗나감(피해 없음, 타격 루프 계속), 그 밖 = 던짐.
        // RuleAttackRoll.OnTrigger 에서 AC 규칙(ACRule)을 만들지 않는 정상 경로 세 가지(IL, 같은 DLL):
        //   ① AutoHit(0085~0099: 자기·우호 대상 접촉 등) → Result = Hit. AC 는 급소 공격이 있을 때만 만든다(00CB~00FE) → AC 없는 명중 정당.
        //   ② AutoMiss(0151~015B) → Result = Miss, AC 없음.
        //   ③ 은폐·실패 확률 실패(0166~0172 → 0511~0513) → Result = Concealment, AC 없음(종전 코드는 여기서 던져 전력 공격 2타째를 잃었다).
        // 그 밖에서 AC 가 없으면 결측(비정상) → 던진다. AC 가 있으면 접촉 AC 여야 한다(아니면 던진다 — 종전 단언 유지).
        // isHit = 엔진 RuleAttackRoll.IsHit(get_IsHit IL 0000~0013: ¬AutoMiss ∧ Result 가 명중류). Result 와 어긋나면 기록이 깨진 것 → 던진다.
        public static bool TouchRollHit(RollResult result, bool isHit, bool autoHit, bool autoMiss, bool hasAcRule, bool acIsTouch)
        {
            switch (result)
            {
                case RollResult.Hit: case RollResult.CriticalHit: case RollResult.Miss: case RollResult.DodgeAC: case RollResult.ArmorAC:
                case RollResult.ShieldAC: case RollResult.MirrorImage: case RollResult.Concealment: case RollResult.Parried: break;
                case RollResult.Unknown: throw new InvalidOperationException("PhantomBlade: stand touch roll has no result (Unknown) after it triggered");
                default: throw new InvalidOperationException("PhantomBlade: stand touch roll has an unknown result value " + (int)result);
            }
            bool resultHit = result == RollResult.Hit || result == RollResult.CriticalHit;
            if (isHit != (resultHit && !autoMiss)) throw new InvalidOperationException("PhantomBlade: stand touch roll IsHit " + isHit + " disagrees with Result " + result + " (AutoMiss " + autoMiss + ")");
            if (hasAcRule)
            {
                if (!acIsTouch) throw new InvalidOperationException("PhantomBlade: stand touch roll was resolved against a non-touch AC (result " + result + ")");
                return isHit;
            }
            // AC 없음: 엔진의 정당한 세 경로만 통과(엔진 판정 순서 그대로 AutoHit → AutoMiss → 은폐).
            if (autoHit)
            {
                if (!isHit) throw new InvalidOperationException("PhantomBlade: stand touch roll is AutoHit but not a hit (result " + result + ")");
                return true;
            }
            if (autoMiss)
            {
                if (result != RollResult.Miss) throw new InvalidOperationException("PhantomBlade: stand touch roll is AutoMiss but its result is " + result + " (engine sets Miss)");
                return false;
            }
            if (result == RollResult.Concealment) return false; // 정당한 빗나감: 은폐·실패 확률(AC 를 계산하기 전에 끝남)
            throw new InvalidOperationException("PhantomBlade: stand touch roll has no ACRule although it was neither AutoHit, AutoMiss nor Concealment (result " + result + ")");
        }

         
        // 엔진 RuleCalculateAttackBonusWithoutTarget.OnTrigger(IL 003E~0054)는 ContextData<AttackBonusStatReplacement>.Current 가 있으면 비교 없이 그 능력치를 쓴다 —
        // 개시자·무기 필터가 없다. 그래서 굴림 전체에 문맥을 열면 같은 굴림 안의 남의 계산(적 받아넘기기 ParryData, RuleAttackRoll.OnTrigger IL 0547)도 바뀌었다(I3),
        // 귀신참은 엔진이 Living Grimoire 면 지능으로 연다(<Deliver>d__36 IL 0447~0453, I4). 범위 = 그 굴림 「자신」의 계산 = 같은 개시자 ∧ 같은 무기 인스턴스.
        public enum ChaScope { None, StandTouch, GhostSlash }

        // inRoll = 규칙 스택에 바깥 RuleAttackRoll 이 있음(없음 = 행동 막대 미리보기 등 굴림 밖 계산 — 정당한 None). standTouchRoll = 그 굴림이 스탠드 접촉 굴림(MarkStandTouch),
        // ghostSlashRoll = 그 굴림의 원인 능력이 귀신참, sameInitiator / sameWeapon = 이 계산의 개시자·무기가 그 굴림의 것과 같음(참조 비교).
        public static ChaScope ChaScopeFor(bool inRoll, bool standTouchRoll, bool ghostSlashRoll, bool sameInitiator, bool sameWeapon)
        {
            if (!inRoll)
            {
                if (standTouchRoll || ghostSlashRoll || sameInitiator || sameWeapon) throw new InvalidOperationException("PhantomBlade: ChaScopeFor got roll facts without an enclosing roll");
                return ChaScope.None;
            }
            if (standTouchRoll && ghostSlashRoll) throw new InvalidOperationException("PhantomBlade: a roll is marked both stand touch and ghost slash");
            if (!standTouchRoll && !ghostSlashRoll) return ChaScope.None;  // 정당: 주인의 다른 굴림(본체 무기 공격 등)
            if (sameWeapon && !sameInitiator) throw new InvalidOperationException("PhantomBlade: an attack bonus with the stand/ghost-slash weapon instance but another initiator (broken record)");
            if (!sameInitiator || !sameWeapon) return ChaScope.None;      // 정당: 그 굴림 안의 남의 계산(받아넘기기 등) — I3 의 핵심
            return standTouchRoll ? ChaScope.StandTouch : ChaScope.GhostSlash;
        }

        // 규칙 스택(밖 → 안 순서: Rulebook.TriggerEvent IL 003E PushEvent = List.Add)에서 가장 안쪽(마지막) T. 없으면 null — 정당(굴림 밖 계산).
        // 스택이 없으면 처리기 안이 아니다 → 던진다. 가장 바깥을 고르면 굴림 안 굴림에서 남의 굴림을 본다(전체 검토 m2, 변이 FA-1).
        public static T InnermostOf<T>(System.Collections.IEnumerable stack) where T : class
        {
            if (stack == null) throw new ArgumentNullException("stack");
            T found = null;
            foreach (var e in stack)
            {
                var r = e as T;
                if (r != null) found = r;
            }
            return found;
        }

        // 굴림 뒤 되읽기. attackBonusComputed = roll.AttackBonusRule != null(AutoHit·AutoMiss·은폐 경로는 계산하지 않음 — RuleAttackRoll.OnTrigger IL 01E0 이 그 뒤).
        // 계산했으면 범위가 정확히 한 번 열리고 닫혀 최종 능력치가 매력이어야 하고, 안 했으면 한 번도 열리지 않아야 한다. 그 밖 = 던짐(처리기가 안 돌았거나 문맥 누수).
        public static void RequireChaApplied(bool attackBonusComputed, int opened, int closed, bool finalStatIsCharisma)
        {
            if (!attackBonusComputed)
            {
                if (opened != 0 || closed != 0) throw new InvalidOperationException("PhantomBlade: Charisma scope opened " + opened + " / closed " + closed + " time(s) for a roll that computed no attack bonus");
                return;
            }
            if (opened != 1 || closed != 1) throw new InvalidOperationException("PhantomBlade: Charisma scope opened " + opened + " / closed " + closed + " time(s) for one attack bonus (expected 1/1 — StandAttackStatScope missing on StandBuff, or a leak)");
            if (!finalStatIsCharisma) throw new InvalidOperationException("PhantomBlade: the stand/ghost-slash attack bonus was not computed with Charisma");
        }

         
        //   이전 표준 쿨다운 > 0 을 잡고(IL 0049~0054) 감산 뒤 ≤ 0 이면 OnNewRound + IUnitNewCombatRoundHandler(IL 00F3~011E). 이 컨트롤러는 전투 중 유닛만 틱
        //   (BaseUnitCombatController.ShouldTickOnUnit = IsInCombat). 따라서 실시간 ∧ 전투 중 ∧ 표준 쿨다운 > 0 이면 이벤트가 온다. 턴제는 턴 시작이 경계라 false.
        //   쿨다운이 유한·비음수가 아니면 읽기가 깨진 것 → 던진다(「쿨다운 없음」으로 조용히 읽지 않음).
        public static bool RoundEventPending(bool turnBased, bool ownerInCombat, double standardActionCooldown)
        {
            if (double.IsNaN(standardActionCooldown) || double.IsInfinity(standardActionCooldown) || standardActionCooldown < 0)
                throw new ArgumentOutOfRangeException("standardActionCooldown", standardActionCooldown, "PhantomBlade: standard action cooldown must be finite and >= 0");
            return !turnBased && ownerInCombat && standardActionCooldown > 0;
        }

        // 명중 하나(TouchLevel 이상은 공격 하나)가 일으키는 타수: 평상 NormalStrikesPerHit, 전력 공격 FullAttackStrikesPerHit(Task 6d: 10레벨 이상은 「공격마다」 — 전력 공격은 15레벨이라 늘 접촉 모드).
        public static int StrikesPerHit(bool fullAttackRound) { return fullAttackRound ? FullAttackStrikesPerHit : NormalStrikesPerHit; }

        // 명중 하나가 일으킬 수 있는 최대 타수(령 동작의 같은 프레임 상한 SpiritRules.MaxSwingsPerBurst 가 읽는다).
        public static int MaxStrikesPerHit { get { return Math.Max(NormalStrikesPerHit, FullAttackStrikesPerHit); } }

        // 설계 §4.4: 흡수 비율 — 2~17: 50%, 18+: 75%
        public static int AbsorbPercent(int level) { Require(level >= StandLevel, "level", level); return level >= Absorb75Level ? 75 : 50; }

        // 설계 §4.4: 흡수량 = min(풀, 받은 피해 × 비율 / 100) 내림
        public static int AbsorbAmount(int incoming, int percent, int pool)
        {
            Require(incoming >= 0, "incoming", incoming);
            Require(percent >= 0, "percent", percent);
            Require(pool >= 0, "pool", pool);
            return Math.Min(pool, (int)((long)incoming * percent / 100));
        }

        // max(1, 매력 수정치) — 게임 쪽 ChaMin1Property(StandBlueprints.BuildProperties)의 거울. 풀 최대치·전력 공격 횟수·귀신참 횟수(GhostSlash.UsesPerDay)가 모두 이 하나를 쓴다(C4).
        public static int ChaMin1(int chaMod) { return Math.Max(1, chaMod); }





        // 설계 §4.4/§9: 원거리 AC 보너스 — <6: 0, 6~13: +2, 14+: +4
        public static int RangedAcBonus(int level) { return level >= RangedGuard2Level ? 4 : (level >= RangedGuardLevel ? 2 : 0); }

        // 설계 §4.3: 풀어택 사용 횟수 = max(1, 매력 수정치)
        public static int FullAttackUses(int chaMod) { return ChaMin1(chaMod); }
    }
}
