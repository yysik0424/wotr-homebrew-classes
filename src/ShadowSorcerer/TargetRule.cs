namespace ShadowSorcerer
{
     
    public sealed class TargetFacts
    {
        public bool IsPoint;            // 래퍼가 지점(고정 지점 포함)
        public bool? UnitPresent;       // 유닛 래퍼의 유닛이 있음
        public bool? IsDead;
        public bool? CanTarget;         // AbilityData.CanTarget(래퍼)
        public bool? IsEnemyOfCaster;   // 시전 시점에만 본다(아군 대상 메아리 없음 — S6 ally-target-no-echo)
    }

    public static class TargetReasons { public const string TargetDead = "TargetDead", TargetInvalid = "TargetInvalid", AllyTarget = "AllyTarget"; }

     
    public enum FreezeKind { KeepWrapper, PointWrapper, KeepWrapperFrozenCenter, Unknown }

    public struct FreezeFacts
    {
        public bool IsAoe;
        // (계획의 CanTargetPoint 는 넣지 않았다 — 앵커가 이미 그것을 담는다: AbilityData.TargetAnchor. 읽지 않는 입력을 두지 않는다.)
        public int Anchor;             // AbilityTargetAnchor: Owner 0, Unit 1, Point 2 (Assembly-CSharp enum 순서 — 시험 exec: freeze anchor enum 이 dnlib 로 대조)
        public bool UsesTargetsAround;  // AbilityTargetsAround 컴포넌트(H6 FrozenCenterPatch 가 중심을 바꿀 수 있는 유일한 형식)
        public bool IsUnitWrapper;
    }

    // H6 의 순수 판정: 메아리 태그(있는가·고정 중심이 필요한가·중심 값)로 범위 선택 중심을 무엇으로 쓸지. 좌표는 float 3개로 받는다(게임 형식 없음).
    public enum CenterChoice { KeepLive, UseFrozen, MissingFrozen }

    public static class FrozenCenterRule
    {
        public static CenterChoice Choose(bool tagPresent, bool needsFrozen, bool hasFrozen)
        {
            if (!tagPresent || !needsFrozen) return CenterChoice.KeepLive;
            return hasFrozen ? CenterChoice.UseFrozen : CenterChoice.MissingFrozen;
        }

        // 쓸 중심 값: UseFrozen 이면 고정 중심, 아니면 지금 중심(H6 은 이 결과로만 인자를 만든다).
        public static T Pick<T>(CenterChoice c, T live, T frozen) { return c == CenterChoice.UseFrozen ? frozen : live; }
    }

    public static class TargetRule
    {
        public const int AnchorOwner = 0, AnchorUnit = 1, AnchorPoint = 2;

        // 쏠 때마다: 지점 → CanTarget 만. 유닛 → 없음·죽음·무효 Skip.
        public static GateResult AtFire(TargetFacts t)
        {
            if (t == null) return GateResult.Unknown("no target facts");
            if (!t.IsPoint)
            {
                if (!t.UnitPresent.HasValue) return GateResult.Unknown("target unit presence unread");
                if (!t.UnitPresent.Value) return GateResult.Skip(TargetReasons.TargetInvalid);
                if (!t.IsDead.HasValue) return GateResult.Unknown("target death unread");
                if (t.IsDead.Value) return GateResult.Skip(TargetReasons.TargetDead);
            }
            if (!t.CanTarget.HasValue) return GateResult.Unknown("CanTarget unread");
            if (!t.CanTarget.Value) return GateResult.Skip(TargetReasons.TargetInvalid);
            return new GateResult(Decision.Echo, null, 0);
        }

        // 시전 시점(관문 9): AtFire + 유닛이면 시전자의 적이어야 한다.
        public static GateResult AtCast(TargetFacts t)
        {
            var r = AtFire(t);
            if (r.Decision != Decision.Echo || t.IsPoint) return r;
            if (!t.IsEnemyOfCaster.HasValue) return GateResult.Unknown("target hostility unread");
            return t.IsEnemyOfCaster.Value ? r : GateResult.Skip(TargetReasons.AllyTarget);
        }

        public static FreezeKind FreezeAtEnqueue(FreezeFacts f)
        {
            if (!f.IsAoe) return FreezeKind.KeepWrapper;
            if (f.Anchor == AnchorPoint) return FreezeKind.PointWrapper; // 유닛 래퍼였어도 그 시점 좌표의 새 지점 래퍼(TargetWrapper.get_Point 는 유닛을 따라간다)
            if (f.Anchor == AnchorOwner || f.Anchor == AnchorUnit)
                return f.UsesTargetsAround ? FreezeKind.KeepWrapperFrozenCenter : FreezeKind.Unknown; // Owner 앵커는 지점 래퍼를 거부한다(CanTarget IL_014B~0169)
            return FreezeKind.Unknown;
        }
    }
}
