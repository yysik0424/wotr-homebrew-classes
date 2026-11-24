using System;
using System.Collections.Generic;

namespace ShadowSorcerer
{
     
    //   피해: 메아리 = 엔진 값 × EchoDamagePercent(50%). 엔진 값은 불신 성공 시 이미 그림자 계수(50%)가 곱해져 있다 → 실패 50% / 성공 25%.
    //   피해 아닌 효과: 메아리 컨텍스트는 확률을 굴리지 않는다(checkChance = false) → 엔진 TryAffectByShadow 가 불신 성공 시 「영향 없음」을 돌려준다.
    // 판정은 3상태다. 판정 불가(Unknown)는 「메아리 아님」으로 흡수하지 않고 던진다 — 호출자(H7·H8)는 Unknown 을 먼저 보고하고 엔진 값을 그대로 둔다.
    public enum EchoMark { NotEcho, Echo, Unknown }

    // [순수] 메아리 표식 항목 판독. 표식 = 엔진 불신 목록(MechanicsContext.m_ShadowDisbelieveData) 안의 「대상이 빈 항목 + 상태 ChanceChecked 단독」 하나.
    // 엔진은 이 모양을 만들지 못한다(항목은 항상 실제 대상으로 만들고, ChanceChecked 는 Disbelieved 뒤에만 선다 — 시험 exec: echo mark: engine cannot produce the sentinel).
    public static class EchoMarkRule
    {
        // 엔진 상태 플래그 거울: ShadowDisbelieveData.State { Disbelieved = 1, ChanceChecked = 2, AffectedByChance = 4 }(byte).
        // 출처 Assembly-CSharp get_IsDisbelieved IL_0006 ldc.i4.1 · get_IsChanceChecked IL_0006 ldc.i4.2 · set_IsAffectedByChance IL_0018 ldc.i4.4(dnlib 2026-11-16). 시험이 enum 상수와 대조한다. 설계 다이얼이 아니다.
        public static readonly byte StateDisbelieved = 1;
        public static readonly byte StateChanceChecked = 2;
        public static readonly byte StateAffectedByChance = 4;

        // 표식 항목의 상태 = ChanceChecked 단독. 정찰 §2-3: 엔진 경로로는 Disbelieved 없이 ChanceChecked 가 설 수 없다(TryAffectByShadow IL_00DC~0122).
        public static readonly byte SentinelState = StateChanceChecked;

        public static byte State(bool disbelieved, bool chanceChecked, bool affectedByChance)
        {
            return (byte)((disbelieved ? StateDisbelieved : 0) | (chanceChecked ? StateChanceChecked : 0) | (affectedByChance ? StateAffectedByChance : 0));
        }

        // entries == null = 엔진 목록이 없다(그림자 판정을 한 번도 하지 않았고 표식도 없다) → NotEcho.
        // 빈-대상 항목이 없다 → NotEcho · 표식 모양 하나 → Echo · 빈-대상 항목이 표식 모양이 아니거나 둘 이상 → Unknown.
        public static EchoMark Classify(IReadOnlyList<(string unitId, byte state)> entries)
        {
            if (entries == null) return EchoMark.NotEcho;
            var mark = EchoMark.NotEcho;
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.unitId)) mark = ClassifyEmptyTarget(mark, e.state);
            }
            return mark;
        }

        internal static EchoMark ClassifyEmptyTarget(EchoMark previous, byte state)
        {
            if (previous == EchoMark.NotEcho && state == SentinelState) return EchoMark.Echo;
            return EchoMark.Unknown;
        }
    }

    public static class EchoStrengthRule
    {
        // H7: 엔진이 낸 피해(그림자 계수 적용 뒤)에 메아리 배율을 곱한다. 메아리가 아니면 그대로.
        public static int Damage(int engineDamage, EchoMark mark) { return DamageCore(engineDamage, mark, ShadowRules.EchoDamagePercent); }

        // 올림은 엔진과 같다(ShadowSpellLogicExtension.TryApplyShadowDamageFactor IL_0000~002F): damage < 1 이면 그대로, 아니면 max(1, ceil(damage × percent / 100)).
        public static int DamageCore(int engineDamage, EchoMark mark, int percent)
        {
            if (percent < 1 || percent > 100) throw new ArgumentOutOfRangeException("percent", percent, "ShadowSorcerer: echo damage percent must be 1..100");
            switch (mark)
            {
                case EchoMark.NotEcho: return engineDamage;
                case EchoMark.Echo:
                    if (engineDamage < 1) return engineDamage;
                    long scaled = ((long)engineDamage * percent + 99) / 100;
                    return scaled < 1 ? 1 : (int)scaled;
                case EchoMark.Unknown: throw new InvalidOperationException("ShadowSorcerer: echo mark unknown - damage is neither echo nor not-echo");
                default: throw new ArgumentOutOfRangeException("mark", mark, "ShadowSorcerer: undefined echo mark");
            }
        }

        // H8: 엔진 TryAffectByShadow 의 checkChance. 메아리면 거짓(불신 성공 = 영향 없음, 확률 굴림 없음), 아니면 엔진 값 그대로.
        public static bool CheckChance(bool engineCheckChance, EchoMark mark)
        {
            switch (mark)
            {
                case EchoMark.NotEcho: return engineCheckChance;
                case EchoMark.Echo: return false;
                case EchoMark.Unknown: throw new InvalidOperationException("ShadowSorcerer: echo mark unknown - chance check is neither echo nor not-echo");
                default: throw new ArgumentOutOfRangeException("mark", mark, "ShadowSorcerer: undefined echo mark");
            }
        }
    }

     
    // 엔진은 ContextActionKill · ContextActionDispelMagic 에 그림자 판정을 걸지 않는다 → H9·H10 이 메아리 컨텍스트에서 그 대상이 불신에 성공했을 때만 원래 동작을 건너뛴다.
    // 불신 상태는 「성공 / 실패 / 아직 굴리지 않음」 3상태 + 「유닛 대상이 아님(지점·장판 해제 — 무변경)」 + 「읽을 수 없음」. 읽을 수 없음을 성공·실패로 읽지 않는다.
    public enum EchoDisbelief { Failed, Passed, NotRolled, NoUnitTarget, Unreadable }

    public enum EchoSkipAction { Keep, Skip, KeepAndReport, RollFirst }

    public static class EchoSkipRule
    {
        // 메아리 표식 × 불신 상태 → 원래 동작을 유지 / 건너뜀 / 유지 + 보고 / 먼저 엔진에 굴리게 함.
        // 메아리가 아니면 불신 상태를 보지 않고 유지한다(본편·다른 직업·다른 모드의 즉사·해제).
        public static EchoSkipAction Decide(EchoMark mark, EchoDisbelief state)
        {
            switch (mark)
            {
                case EchoMark.NotEcho: return EchoSkipAction.Keep;
                case EchoMark.Unknown: return EchoSkipAction.KeepAndReport;
                case EchoMark.Echo:
                    switch (state)
                    {
                        case EchoDisbelief.Failed: return EchoSkipAction.Keep;
                        case EchoDisbelief.Passed: return EchoSkipAction.Skip;
                        case EchoDisbelief.NotRolled: return EchoSkipAction.RollFirst;
                        case EchoDisbelief.NoUnitTarget: return EchoSkipAction.Keep;
                        case EchoDisbelief.Unreadable: return EchoSkipAction.KeepAndReport;
                        default: throw new ArgumentOutOfRangeException("state", state, "ShadowSorcerer: undefined echo disbelief state");
                    }
                default: throw new ArgumentOutOfRangeException("mark", mark, "ShadowSorcerer: undefined echo mark");
            }
        }

        // 엔진 불신 목록에서 그 대상의 항목을 읽은 결과: 항목 없음(null) = 아직 굴리지 않음, 있으면 IsDisbelieved.
        public static EchoDisbelief FromEntry(bool? disbelieved)
        {
            if (!disbelieved.HasValue) return EchoDisbelief.NotRolled;
            return disbelieved.Value ? EchoDisbelief.Passed : EchoDisbelief.Failed;
        }

        // 엔진에 굴리게 한 뒤(TryAffectByShadow(target, checkChance: false)): 참 = 영향 받음 = 불신 실패, 거짓 = 불신 성공(IL_0127~013C).
        // 굴린 뒤에는 RollFirst 가 다시 나올 수 없다 — 나오면 계약 위반으로 던진다.
        public static EchoSkipAction AfterRoll(EchoMark mark, bool affectedByShadow)
        {
            var a = Decide(mark, affectedByShadow ? EchoDisbelief.Failed : EchoDisbelief.Passed);
            if (a == EchoSkipAction.RollFirst) throw new InvalidOperationException("ShadowSorcerer: echo skip rule asked for a second disbelief roll");
            return a;
        }
    }
}
