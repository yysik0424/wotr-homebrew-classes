using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Fx;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using ModContext = WotrClassMod.Common.ModContext;

namespace ShadowSorcerer
{
    // 그림자의 시전 동작·손 FX·손 위치(소유자 결정 2026-11-16 밤 2·5·6, 정찰 §3.4·§7-5). 결정은 순수 ShadowCastRule 이 한다.
    // 엔진 경로(명령 없이 핸들만 — 본편 UnitCommand.StartAnimation IL_0019~0024 · UnitUseAbility.<TriggerAnimation>b__69_0 가 하는 일에서 명령 부분을 뺀 것):
    //   view.AnimationManager.CreateHandle(UnitAnimationType.CastSpell, true) → CastStyle · CastingTime · Spell · AttackTargetDistance → Execute(handle).
    //   규칙·명령을 만들지 않는다(시험이 IL 로 잠금). 그림자는 실제 유닛이라 시전 액션이 읽는 handle.Unit.EntityData 가 있다(UnitAnimationActionCastSpell.OnStart IL_025C~0266).
    // 그림자 모델(BCT_ShadowHalfElfMale, 프리팹 9dc6267b…)에서 확인한 것 — 번들 직독 2026-11-17(UnityPy, 설치본 Bundles\9dc6267b….unit · ghost.animations):
    //   손 뼈대: ParticlesSnapMap 에 Locator_PalmFX_Left / _Right · Locator_TorsoCenterFX · Locator_HeadCenterFX_00 · Locator_GroundFX 가 있다.
    //   시전 동작: 애니메이션 세트 = Ghost_AnimationSet(사람 세트가 아니다) — UnitAnimationActionCastSpell 「Ghost_AnimationSet_CastSpell」, 스타일 0~5(SelfTouch · Touch · Point · Directional · Omni · Self).
    //   Thrown · BreathWeapon 등 그 밖의 스타일은 세트에 없다 → 대체 스타일(Directional 우선)로 재생하고 정보 줄을 남긴다.
    // 게임 밖에서 볼 수 없는 것(다른 모드가 모델·세트를 바꿨는지)은 실행 때 다시 읽는다: 없으면 오류 줄 1회 + 대체(스타일 대체 / 유닛 위치 + 높이 다이얼) — 대체가 쓰였다는 사실이 그 줄에 남는다.
    // 계약: 공개 진입점은 던지지 않는다. 동작·FX 가 실패해도 메아리와 나머지 연출은 그대로 간다.
    public static class ShadowCast
    {
        public static int Played { get; private set; }
        public static int HandFxSpawned { get; private set; }
        public static int BoneLaunches { get; private set; }
        public static int FallbackLaunches { get; private set; }

        public static int StanceDeferred { get; private set; }

        // 등장 때: 본체의 전투 자세를 따라 한다(전투 대기 자세). 던지지 않는다.
        // 드러낸 그 프레임에 뷰의 애니메이션 관리자가 아직 없을 수 있다(2026-11-20 실플레이 1회: 「shadow view has no AnimationManager」, 그 무대는 시전 전에 CombatEnded 로 꺼져 지속 여부 미확인).
        //   그때는 자세를 시전 동작 직전(Play)으로 미룬다. Play 에서도 없으면 Play 의 「animation not played」 오류 줄이 난다 — 조용히 넘어가지 않는다(C1).
        // 돌려주는 값 = 자세를 미뤘는가. 무대(Show)가 자기 자리에 적어 두므로 전역 기록이 없다 — 무대가 사라지면 함께 사라진다.
        public static bool Prepare(UnitEntityData shadow, UnitEntityData caster)
        {
            try
            {
                if ((object)shadow == null) throw new ArgumentNullException("shadow");
                if ((object)caster == null) throw new ArgumentNullException("caster");
                var view = shadow.View;
                if (view == null) throw new InvalidOperationException("shadow unit has no view");
                var am = view.AnimationManager;
                if (am == null)
                {
                    StanceDeferred++;
                    return true;
                }
                am.IsInCombat = caster.IsInCombat;
                return false;
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "cast-prepare-" + e.GetType().Name, "shadow cast: stance not set (" + e.GetType().Name + ": " + e.Message + ")"); return false; }
        }

        // 시전 동작 시작(발사 CastLead 전). 던지지 않는다.
        // stanceOf = 자세를 미뤘던 본체(없으면 null).
        public static void Play(UnitEntityData shadow, AbilityData spell, Vector3 target, UnitEntityData stanceOf)
        {
            try
            {
                if (spell == null || spell.Blueprint == null) throw new ArgumentNullException("spell");
                var am = Manager(shadow);
                if ((object)stanceOf != null)
                {
                    am.IsInCombat = stanceOf.IsInCombat;
                }
                int wanted = (int)spell.Blueprint.Animation;
                var cast = am.ActionSet.OfType<UnitAnimationActionCastSpell>().FirstOrDefault();
                // 재생할 수 있는 스타일 = 시전 액션의 항목 중 시전 클립이 있는 것(없으면 엔진이 「No cast clip」 오류만 남기고 아무것도 재생하지 않는다 — OnStart IL_0105~0146).
                var available = cast == null || cast.Animations == null ? new int[0] : cast.Animations.Where(a => a != null && a.Default != null && a.Default.CastClipWrapper != null).Select(a => (int)a.Style).ToArray();
                int style;
                var pick = ShadowCastRule.Pick(wanted, available, out style);
                string name = ((UnitAnimationActionCastSpell.CastAnimationStyle)wanted).ToString();
                switch (pick)
                {
                    case CastStylePick.Exact: break;
                    case CastStylePick.KnownSubstitute:
                        if (knownSubstitutes.Add(wanted)) ModContext.Log("ShadowSorcerer: shadow cast: style " + name + " is not in the shadow's animation set - playing " + (UnitAnimationActionCastSpell.CastAnimationStyle)style + " instead");
                        break;
                    case CastStylePick.UnexpectedSubstitute:
                        ShadowErrors.ReportOnce(EchoVisuals.Site, "cast-style-missing-" + wanted, "shadow cast: style " + name + " should be in the shadow's animation set but is not (model or animation set changed) - playing " + (UnitAnimationActionCastSpell.CastAnimationStyle)style + " instead");
                        break;
                    case CastStylePick.None:
                        ShadowErrors.ReportOnce(EchoVisuals.Site, "cast-style-none", "shadow cast: the shadow's animation set has no usable cast animation (" + (cast == null ? "no cast action" : "no cast clip") + ") - the shadow stands still while the echo fires");
                        return;
                    default: throw new InvalidOperationException("unknown cast style pick " + pick);
                }
                var handle = am.CreateHandle(UnitAnimationType.CastSpell, true);
                if (handle == null) throw new InvalidOperationException("UnitAnimationManager.CreateHandle(CastSpell) returned null");
                handle.CastStyle = (UnitAnimationActionCastSpell.CastAnimationStyle)style;
                // 준비 동작 없이 시전 클립만 한 번: NoPrecast 면 OnStart 가 시전 클립을 Oneshot 으로 시작하고(IL_01AC~020D) OnUpdate 는 바로 돌아간다(IL_002C~0032).
                // CastingTime 0 도 같은 갈래로 간다(OnStart IL_017F~01A7) — 둘 다 적어 어느 한쪽 해석에 기대지 않는다.
                handle.SpecialCastBehaviour = UnitAnimationActionCastSpell.SpecialBehaviourType.NoPrecast;
                handle.CastingTime = ShadowCastRule.CastingTimeSeconds;
                handle.Spell = spell.Blueprint;
                handle.AttackTargetDistance = (target - shadow.Position).magnitude;
                am.Execute(handle);
                Played++;
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "cast-play-" + e.GetType().Name, "shadow cast: animation not played (" + e.GetType().Name + ": " + e.Message + ") - the shadow stands still while the echo fires"); }
        }

        static readonly HashSet<int> knownSubstitutes = new HashSet<int>();

        // 발사 순간의 손 FX: 본체가 시전할 때와 같은 프리팹(본편 UnitUseAbility.SpawnCastFx IL_005D~0081 — FxRoot.GetCast(SpellSource, CastSource) → FxHelper.SpawnFxOnUnit, 같은 인자).
        // 그 주문 원천에 프리팹이 없으면(null) 낼 것이 없다 — 정당한 무동작. 던지지 않는다.
        public static void HandFx(UnitEntityData shadow, AbilityData spell)
        {
            try
            {
                if (spell == null) throw new ArgumentNullException("spell");
                if (ShadowDoubles.Going(shadow)) return; // 정당한 무동작: 사라지는 그림자
                var view = shadow.View;
                if (view == null) throw new InvalidOperationException("shadow unit has no view");
                var root = BlueprintRoot.Instance;
                if (root == null || root.FxRoot == null) throw new InvalidOperationException("BlueprintRoot.FxRoot is missing");
                var prefab = root.FxRoot.GetCast(spell.SpellSource, CastSource.SingleHand);
                if (prefab == null) return; // 정당한 무동작: 이 원천에는 시전 FX 가 없다(본편도 null 이면 띄우지 않는다)
                FxHelper.SpawnFxOnUnit(prefab, view, null, null, default(Vector3), FxPriority.EventuallyImportant);
                HandFxSpawned++;
            }
            catch (Exception e) { ShadowErrors.ReportOnce(EchoVisuals.Site, "cast-fx-" + e.GetType().Name, "shadow cast: hand FX not spawned (" + e.GetType().Name + ": " + e.Message + ")"); }
        }

        // 투사체가 나갈 자리 = 그림자의 그 뼈대(본편 ProjectileController.CreateView IL_00E7~010E 와 같은 조회: ParticlesSnapMap[SourceBone].Transform.position).
        //   뼈대 이름이 비어 있으면 본편처럼 뷰의 뿌리(발밑). 뼈대가 없으면 오류 줄 1회 + 유닛 위치 + 높이 다이얼(대체가 쓰였다는 사실이 그 줄에 남는다).
        // 호출자(H12)가 예외를 보고한다 — 여기는 값만 낸다.
        public static Vector3 LaunchPoint(UnitEntityData shadow, string sourceBone)
        {
            var view = shadow.View;
            if (view == null) throw new InvalidOperationException("shadow unit has no view");
            if (string.IsNullOrEmpty(sourceBone)) { BoneLaunches++; return view.transform.position; }
            var map = view.ParticlesSnapMap;
            var bone = map == null ? null : map[sourceBone];
            if (bone != null && bone.Transform != null) { BoneLaunches++; return bone.Transform.position; }
            ShadowErrors.ReportOnce(EchoVisuals.Site, "bone-missing-" + sourceBone, "shadow cast: the shadow model has no '" + sourceBone + "' locator" + (map == null ? " (no ParticlesSnapMap)" : "") + " - echo projectiles start "
                + ShadowVisualDials.FallbackLaunchHeightMeters + " m above the shadow's feet instead");
            FallbackLaunches++;
            return shadow.Position + Vector3.up * ShadowVisualDials.FallbackLaunchHeightMeters;
        }

        static UnitAnimationManager Manager(UnitEntityData shadow)
        {
            if ((object)shadow == null) throw new ArgumentNullException("shadow");
            var view = shadow.View;
            if (view == null) throw new InvalidOperationException("shadow unit has no view");
            var am = view.AnimationManager;
            if (am == null) throw new InvalidOperationException("shadow view has no AnimationManager");
            return am;
        }
    }
}
