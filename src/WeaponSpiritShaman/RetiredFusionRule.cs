using System;

namespace WeaponSpiritShaman
{
    // [순수] 융합 제거(소유자 결정 2026-11-18) 뒤 옛 세이브 정리 판정(게임 형식 없음 — modinfo testSources). 관측은 RetiredFusionAudit 가 지역 로드마다 넘긴다.
    //  - 융합 능력 사실: 그 블루프린트(Guids.FusionAbility)는 우리 특성 IV 의 AddFacts 만 붙였다 — 다른 출처가 없으므로 출처 확정 → 떼고 INFO 1줄.
    //  - 융합 버프: 저장 직전 종료 규칙(옛 UnitPartSpiritFusion.OnPreSave)상 세이브에 없어야 한다 → 있으면 떼고 오류 1줄.
    //  - 옛 융합 파트의 저장값이 Idle 이 아님(State ≠ 0 · PendingSteps ≠ 0 · HiddenByUs): 같은 규칙상 없어야 한다 → 값을 Idle 로 되돌리고 오류 1줄,
    //    HiddenByUs 면 스피릿 웨폰 숨김을 푼다(PetId 가 없으면 풀 대상을 모름 → 오류 사유에 적는다).
    public static class RetiredFusionRule
    {
        public const int IdleState = 0; // 옛 FusionState.Idle 의 저장값(enum 첫 값 — 옛 FusionLifecycle.cs `enum FusionState { Idle, Active, Ending, AwaitRestore }`)

        public sealed class Plan
        {
            public bool RemoveAbilities, RemoveBuffs, ResetPart, Unhide;
            public string Error; // null = 오류 없음
            public bool Nothing { get { return !RemoveAbilities && !RemoveBuffs && !ResetPart && !Unhide && Error == null; } }
        }

        public static Plan Decide(int abilityFacts, int fusionBuffs, bool hasPart, int savedState, int pendingSteps, bool hiddenByUs, bool hasPetId)
        {
            if (abilityFacts < 0 || fusionBuffs < 0 || pendingSteps < 0) throw new ArgumentOutOfRangeException("RetiredFusionRule: negative count");
            if (!hasPart && (savedState != IdleState || pendingSteps != 0 || hiddenByUs || hasPetId)) throw new ArgumentException("RetiredFusionRule: part values given without a part");
            var p = new Plan { RemoveAbilities = abilityFacts > 0, RemoveBuffs = fusionBuffs > 0 };
            string err = null;
            if (fusionBuffs > 0) err = "fusion buff x" + fusionBuffs + " present after fusion removal (the pre-save end should have removed it) — removed";
            if (hasPart && (savedState != IdleState || pendingSteps != 0 || hiddenByUs))
            {
                p.ResetPart = true;
                p.Unhide = hiddenByUs && hasPetId;
                string s = "saved fusion state " + savedState + " pending " + pendingSteps + " hiddenByUs " + hiddenByUs + " after fusion removal — reset to idle"
                    + (hiddenByUs ? (hasPetId ? ", weapon spirit unhidden" : ", but no PetId: the hidden weapon spirit cannot be found") : "");
                err = err == null ? s : err + "; " + s;
            }
            p.Error = err;
            return p;
        }
    }
}
