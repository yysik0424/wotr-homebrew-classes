using Kingmaker.EntitySystem.Entities;
using Kingmaker.Blueprints;
using Kingmaker.View;
using Kingmaker.Controllers.Projectiles;
using HarmonyLib;
using static WotrClassMod.Common.BlueprintUtil;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;
using WotrClassMod.Common;

namespace BloodBerserker
{
    // Class-owned effect. Its endpoints follow the actors until native forced movement stops.
    public class BloodTether : MonoBehaviour
    {
        UnitEntityData caster, foe, mover;
        Transform hand, torso;
        LineRenderer line;
        Material material;

        internal static BlueprintProjectile Shot;

        internal static void BuildProjectile(BlueprintProjectile source)
        {
            if (source.Trajectory == null || source.View == null || string.IsNullOrEmpty(source.View.AssetId))
                throw new System.InvalidOperationException("Missing native chain trajectory or view.");
            Shot = Create<BlueprintProjectile>("BloodBerserkerChainProjectile", Guids.BloodProjectile, p => {
                p.Speed = source.Speed; p.MinTime = source.MinTime;
                p.View = source.View; p.CastFx = source.CastFx; p.CastEffectDuration = source.CastEffectDuration;
                p.LifetimeParticlesAfterHit = source.LifetimeParticlesAfterHit;
                p.ProjectileHit = source.ProjectileHit; p.DamageHit = source.DamageHit;
                p.SourceBone = source.SourceBone; p.TargetBone = source.TargetBone; p.TargetBoneOnCrit = source.TargetBoneOnCrit;
                p.SourceBoneOffsetAtTarget = source.SourceBoneOffsetAtTarget; p.UseSourceBoneScale = source.UseSourceBoneScale;
                p.SourceBoneCorpulenceOffset = source.SourceBoneCorpulenceOffset;
                p.TargetBoneOffsetMultiplier = source.TargetBoneOffsetMultiplier; p.FallsOnMiss = source.FallsOnMiss;
                p.MissMinRadius = source.MissMinRadius; p.MissMaxRadius = source.MissMaxRadius;
                p.MissRaycastDistance = source.MissRaycastDistance; p.AddRagdollImpulse = source.AddRagdollImpulse;
                p.FollowTerrain = source.FollowTerrain; p.StuckArrowPrefab = source.StuckArrowPrefab;
                p.DeflectedArrowPrefab = source.DeflectedArrowPrefab;
                SetField(p, "m_Trajectory", Ref<BlueprintProjectileTrajectoryReference>(source.Trajectory.AssetGuid.ToString()));
            });
        }

        internal static void Attach(UnitEntityData source, UnitEntityData target, UnitEntityData moving)
        {
            if (source.View == null || target.View == null || moving.Get<UnitPartForceMove>() == null) return;
            var fx = new GameObject("BloodBerserkerTether");
            fx.transform.SetParent(source.View.transform, false);
            var tether = fx.AddComponent<BloodTether>();
            tether.caster = source;
            tether.foe = target;
            tether.mover = moving;
            tether.hand = Find(source.View.transform, "Locator_PalmFX_Right");
            tether.torso = Find(target.View.transform, "Locator_TorsoCenterFX");
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) { ModContext.LogError("Blood tether shader unavailable."); Destroy(fx); return; }
            tether.material = new Material(shader);
            tether.line = fx.AddComponent<LineRenderer>();
            tether.line.sharedMaterial = tether.material;
            tether.line.useWorldSpace = true;
            tether.line.positionCount = 2;
            tether.line.startWidth = tether.line.endWidth = 0.045f;
            tether.line.startColor = new Color(.43f, .012f, .035f);
            tether.line.endColor = new Color(.93f, .035f, .105f);
            tether.Redraw();
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var bone in root.GetComponentsInChildren<Transform>(true))
                if (bone.name == name) return bone;
            return null;
        }
        void Redraw()
        {
            line.SetPosition(0, hand != null ? hand.position : caster.Position + Vector3.up * caster.Corpulence);
            line.SetPosition(1, torso != null ? torso.position : foe.Position + Vector3.up * foe.Corpulence);
        }
        void LateUpdate()
        {
            if (line == null || caster?.View == null || foe?.View == null || mover.Get<UnitPartForceMove>() == null)
            { Destroy(gameObject); return; }
            Redraw();
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }

    [HarmonyPatch(typeof(RayView), nameof(RayView.Initialize))]
    internal static class BloodRayView
    {
        static void Postfix(RayView __instance, Projectile __0)
        {
            if (__0.Blueprint != BloodTether.Shot || BloodTether.Shot == null) return;
            foreach (var line in __instance.GetComponentsInChildren<LineRenderer>(true))
            {
                var tint = line.GetComponent<BloodRayTint>() ?? line.gameObject.AddComponent<BloodRayTint>();
                tint.Apply(line);
            }
        }
    }

    public class BloodRayTint : MonoBehaviour
    {
        LineRenderer line;
        MaterialPropertyBlock original;
        Gradient gradient;

        internal void Apply(LineRenderer renderer)
        {
            if (line != null) return;
            line = renderer;
            gradient = line.colorGradient;
            original = new MaterialPropertyBlock();
            line.GetPropertyBlock(original);
            var red = new Color(.78f, .025f, .07f);
            var changed = new MaterialPropertyBlock();
            line.GetPropertyBlock(changed);
            foreach (var property in new[] { "_Color", "_TintColor", "_BaseColor", "_EmissionColor" })
                if (line.sharedMaterial != null && line.sharedMaterial.HasProperty(property)) changed.SetColor(property, red);
            line.SetPropertyBlock(changed);
            line.startColor = red;
            line.endColor = red;
        }

        void OnDisable()
        {
            if (line == null) return;
            line.SetPropertyBlock(original);
            line.colorGradient = gradient;
            line = null;
        }
    }
}
