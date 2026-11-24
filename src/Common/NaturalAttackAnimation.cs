using Kingmaker.UnitLogic.Commands;
using Kingmaker.Visual.Animation.Kingmaker;

namespace WotrClassMod.Common
{
    public static class NaturalAttackAnimation
    {
        // GetAction is the same rig lookup used by CreateSpecialAttackHandle, without allocating an animation handle.
        static readonly UnitAnimationSpecialAttackType[] fallback = {
            UnitAnimationSpecialAttackType.Slam, UnitAnimationSpecialAttackType.Claw, UnitAnimationSpecialAttackType.Bite
        };

        public static void Resolve(AttackHandInfo hand, ref UnitAnimationSpecialAttackType requested, ref int index)
        {
            var rig = hand.Hand?.Owner?.Unit?.View?.AnimationManager;
            if (rig == null || rig.GetAction(requested) != null) return;
            foreach (var motion in fallback)
            {
                if (motion == requested || rig.GetAction(motion) == null) continue;
                requested = motion;
                index = 0;
                return;
            }
        }
    }
}
