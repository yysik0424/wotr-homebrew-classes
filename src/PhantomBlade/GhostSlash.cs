using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PhantomBlade
{
    // 귀신참(Ghost Slash) 수치 규칙의 단일 원장(게임 타입 없음 — 콘솔 시험이 컴파일한다, modinfo testSources). 설계 §13.2, brief-T6c.md.
    // 게임 쪽(GhostSlashProjectilesGetter·GhostSlashCost·ContextActionGhostSlashDamage)은 StandOwner.Level 을 읽어 여기 함수에 넘기기만 한다(산술 없음 — 시험이 IL 로 고정).
    // 레벨은 3 상태: 표 행 = 귀신참 있음 / null = 레벨은 알고 5 미만이라 없음(정당) / 던짐 = 모르는 레벨(전사 1..20 밖).
    public static class GhostSlash
    {
        // 표 한 행: 이 레벨부터 시전 한 번에 나가는 투사체 수.
        public struct Row
        {
            public readonly int Level, Projectiles;
            public Row(int level, int projectiles) { Level = level; Projectiles = projectiles; }
        }

         
        // Archetype 레벨표(StandBlueprints.AddGhostSlashFeatures)가 이 표의 레벨에 특성을 단다.
        public static readonly ReadOnlyCollection<Row> ProjectilesByLevel = new ReadOnlyCollection<Row>(new[]
        {
            new Row(5, 1),
            new Row(10, 2),
            new Row(15, 3),
            new Row(20, 4),
        });

        // 귀신참이 생기는 레벨 = 표 첫 행(시험 `ghostslash: FirstLevel == 5 == first table row level` 이 대조). 상수인 이유: IL 시험이 StandOwner.Level 의 최소 레벨 인자를 리터럴로 본다.
        public const int FirstLevel = 5;

        // 시전 한 번의 비용 = 전용 하루 횟수 1회. 소유자 결정 2026-11-16(설계 §14 ③): 귀신참은 흡수 풀을 쓰지 않는다 —
        // 종전 「⌈레벨 / 2⌉ 흡수 풀 점수」(CostDivisor = 2, brief-T6c.md)는 폐지. 1 = 「하루 N 회」의 한 회(15레벨 스탠드 전력 공격의 AbilityResourceLogic.Amount 와 같은 뜻).
        public const int UsesPerCast = 1;

        static void Require(bool ok, string name, int value)
        {
            if (!ok) throw new ArgumentOutOfRangeException(name, value, name + " out of range");
        }

        static void RequireKnownLevel(int level) { Require(level >= StandRules.MinFighterLevel && level <= StandRules.MaxFighterLevel, "level", level); }

        static void RequireGhostSlash(int level) { RequireKnownLevel(level); Require(level >= FirstLevel, "level", level); }

        // 그 레벨의 표 행(3 상태 — 위 주석).
        public static Row? RowFor(int level)
        {
            RequireKnownLevel(level);
            Row? hit = null;
            foreach (var r in ProjectilesByLevel) if (r.Level <= level) hit = r;
            return hit;
        }

        // 투사체 수. 귀신참이 없는 레벨(< FirstLevel)·모르는 레벨이면 던진다(호출자 결함 — 능력은 5레벨 특성에서만 온다).
        public static int Projectiles(int level)
        {
            RequireGhostSlash(level);
            var row = RowFor(level);
            if (!row.HasValue) throw new InvalidOperationException("PhantomBlade: GhostSlash.ProjectilesByLevel has no row at level " + level + " although level >= FirstLevel " + FirstLevel + " (table and constant disagree)");
            return row.Value.Projectiles;
        }

        // 표 최대 투사체 수(AbilityDeliverProjectile.m_Projectiles 길이 — 엔진은 랭크만큼 Take 한다).
        public static int MaxProjectiles { get { return ProjectilesByLevel.Max(r => r.Projectiles); } }

        // 시전 비용(귀신참 전용 횟수) = UsesPerCast, 레벨과 무관. 레벨 인자는 가드다: 귀신참이 없는 레벨(< FirstLevel)·모르는 레벨이면 던진다(비용 1 로 조용히 답하지 않음).
        public static int Cost(int level)
        {
            RequireGhostSlash(level);
            return UsesPerCast;
        }

        // 하루 횟수(휴식으로 회복) = max(1, 매력 수정치) — 전력 공격(StandRules.FullAttackUses)과 같은 식(StandRules.ChaMin1)이되 따로 센다(자원 블루프린트 별개).
        public static int UsesPerDay(int chaMod) { return StandRules.ChaMin1(chaMod); }

        // 투사체 피해 주사위 = 러시 주먹 주사위(StandRules.DiceFor — 본편 몽크 맨손 중형, 설계 §13.1·§13.2)의 행 번호(StandRules.RushDiceByLevel 안).
        public static int DiceRowIndex(int level)
        {
            RequireGhostSlash(level);
            var d = StandRules.DiceFor(level);
            var rows = StandRules.RushDiceByLevel;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].FromLevel == d.FromLevel && rows[i].Count == d.Count && rows[i].Size == d.Size) return i;
            throw new InvalidOperationException("PhantomBlade: StandRules.DiceFor(" + level + ") is not a row of RushDiceByLevel");
        }

        // 하루(휴식 한 번) 시전 수 = 하루 횟수 ÷ 시전당 비용. 2026-11-16 부터 흡수 풀과 무관하다(종전: 풀 최대치 ÷ ⌈레벨/2⌉). 귀신참이 없는 레벨이면 Cost 가 던진다.
        public static int CastsPerDay(int level, int chaMod) { return UsesPerDay(chaMod) / Cost(level); }
    }
}
