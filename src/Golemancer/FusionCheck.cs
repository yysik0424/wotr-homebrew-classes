using System;

namespace Golemancer
{
    // [순수] 합체 시작 판정(계획 T2 FusionCheck — 시작 전용; 토글 제한은 IgnoreIfStarted = true, R2-05. 유지 중 종료는 융합 기계 사건으로만). 게임 형식 없음.
    // 순서: NotReady(life ∉ {Ready, Stale}) → Broken → GolemMissing(golemOk == false) → Unknown(golemOk == null) → SavePending → MasterPolymorphed(남의 변신 중) / Unknown(변신 판정 불가)
     
    //   FeatureMissing(Fix round 2 nm4): 합체 특성(흡수기) 없이 토글만 있는 상태 — FusionCheckInputs.Decide 가 None 대신 낸다(이 판정기는 특성을 모른다).
    // Gate_* = WSS FusionBlock(FusionGate.cs:44) 값을 1:1 투영(None → None).
    public enum GolemFusionBlock { None, NotReady, Broken, GolemMissing, SavePending, Unknown, Gate_Possession, Gate_PossessionActive, Gate_NotIdle, Gate_Unknown, MasterPolymorphed, FeatureMissing }

    public static class FusionCheck
    {
        public static GolemFusionBlock Decide(LifecycleState life, bool broken, bool? golemOk, bool? savePending, bool? masterPolymorphedByOther, FusionBlock gate)
        {
            if (!Enum.IsDefined(typeof(LifecycleState), life)) throw new ArgumentException("FusionCheck.Decide: undefined lifecycle state " + (int)life);
            var projected = Project(gate); // 정의되지 않은 gate 는 여기서 throw
            if (life != LifecycleState.Ready && life != LifecycleState.Stale) return GolemFusionBlock.NotReady;
            if (broken) return GolemFusionBlock.Broken;
            if (golemOk == false) return GolemFusionBlock.GolemMissing;
            if (golemOk == null) return GolemFusionBlock.Unknown;
            if (savePending == null) return GolemFusionBlock.Unknown;  
            if (savePending.Value) return GolemFusionBlock.SavePending;
            if (masterPolymorphedByOther == true) return GolemFusionBlock.MasterPolymorphed;
            if (masterPolymorphedByOther == null) return GolemFusionBlock.Unknown;
            return projected;
        }

        public static GolemFusionBlock Project(FusionBlock gate)
        {
            switch (gate)
            {
                case FusionBlock.None: return GolemFusionBlock.None;
                case FusionBlock.Possession: return GolemFusionBlock.Gate_Possession;
                case FusionBlock.PossessionActive: return GolemFusionBlock.Gate_PossessionActive;
                case FusionBlock.NotIdle: return GolemFusionBlock.Gate_NotIdle;
                case FusionBlock.Unknown: return GolemFusionBlock.Gate_Unknown;
            }
            throw new ArgumentException("FusionCheck.Project: undefined FusionBlock " + (int)gate);
        }
    }
}
