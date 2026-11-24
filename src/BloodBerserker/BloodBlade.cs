using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.View.Equipment;
using UnityEngine;
using UnityEngine.Rendering;
using ModContext = WotrClassMod.Common.ModContext;

namespace BloodBerserker
{
    public class BloodBladeVisual : UnitFactComponentDelegate, IUnitBuffHandler, IUnitViewAttachedHandler
    {
        protected override void OnTurnOn() { Set(BloodrageRequired.Active(Owner)); }
        protected override void OnTurnOff() { Set(false); }
        public void HandleUnitViewAttached() { OnTurnOn(); }
        public void HandleBuffDidAdded(Buff buff) { if (buff.Owner == Owner && buff.Blueprint == Blueprints.Rage) Set(true); }
        public void HandleBuffDidRemoved(Buff buff) { if (buff.Owner == Owner && buff.Blueprint == Blueprints.Rage) Set(false); }

        void Set(bool active)
        {
            var view = Owner.View;
            if (view == null) return;
            var effect = view.GetComponent<BloodBladeView>();
            if (active && effect == null) effect = view.gameObject.AddComponent<BloodBladeView>();
            if (effect != null) { effect.Owner = Owner; effect.enabled = active; }
        }
    }

    public class BloodBladeView : MonoBehaviour
    {
        internal UnitEntityData Owner;
        readonly BladeHand primary = new BladeHand(), secondary = new BladeHand();



        Material film;

        void LateUpdate()
        {
            if (Owner?.View == null || Owner.View.gameObject != gameObject) { enabled = false; return; }
            var hands = Owner.View.HandsEquipment;
            if (hands == null || !hands.Sets.TryGetValue(Owner.Body.CurrentHandsEquipmentSet, out var set) ||
                hands.ForceHideWeapons) return;
            if (film == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null || !shader.isSupported)
                {
                    ModContext.LogError("Blood blade transparent shader unavailable.");
                    enabled = false;
                    return;
                }
                film = new Material(shader);
            }
            primary.Draw(set.MainHand, film);
            secondary.Draw(set.OffHand, film);
        }

        void OnDisable()
        {
            primary.Clear();
            secondary.Clear();
            if (film != null) Destroy(film);
            film = null;
        }
    }

    internal sealed class BladeHand
    {
        GameObject model;
        ItemEntityWeapon item;
        Renderer[] sources;
        Mesh shell;




        Renderer Source(bool visibleOnly)
        {
            if (sources == null) return null;
            foreach (var renderer in sources)
                if (renderer != null && (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                    && renderer.enabled && !renderer.forceRenderingOff && renderer.gameObject.activeInHierarchy
                    && (!visibleOnly || renderer.isVisible)) return renderer;
            return null;
        }

        internal void Draw(UnitViewHandSlotData slot, Material film)
        {
            var weapon = slot?.VisibleItem as ItemEntityWeapon;
            bool held = slot != null && slot.IsInHand && weapon != null && slot.Slot.MaybeWeapon == weapon;
            var next = held ? slot.VisualModel : null;
            if (next != model || (held && weapon != item) || (next == null && shell != null))
            {
                Clear();
                model = next;
                item = held ? weapon : null;
                if (model != null && BloodWeapon.Eligible(weapon) && BladeGeometry.Edged(weapon.Blueprint.DamageType.Physical.Form))
                {
                    // The native hand model and sheath are separate objects. Only the drawn model supplies locators.
                    sources = model.GetComponentsInChildren<Renderer>(true);
                    shell = BladeGeometry.Build(model.transform);
                    if (shell == null || sources.Length == 0)
                        ModContext.Log("Blood blade visual unavailable on weapon model " + model.name);
                }
            }
            if (shell == null) return;
            var source = Source(true) ?? Source(false);
            if (source != null)
            {
                Graphics.DrawMesh(shell, model.transform.localToWorldMatrix, film, source.gameObject.layer,
                    null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);



            }
        }

        internal void Clear()
        {



            if (shell != null) UnityEngine.Object.Destroy(shell);
            shell = null;
            sources = null;
            model = null;
            item = null;
        }
    }

    internal static class BladeGeometry
    {
        // Base game weapons.prefabs uses the unsuffixed primary trail, plus optional surface points.
        // Offset locators belong to the animated swing trail and are outside the physical blade.
        const string BaseName = "Locator_WeaponTrail00_Start", TipName = "Locator_WeaponTrail00_End";
        const string Surface = "Locator_WeaponSurface_";
        // Art settings for a translucent blood film, in weapon-local metres. Taper both ends.
        const int Sides = 8;
        const float RadiusRatio = .05f, MinimumRadius = .009f, MaximumRadius = .038f;
        static readonly Color32 Blood = new Color32(185, 9, 31, 100);

        internal static bool Edged(PhysicalDamageForm form)
        {
            return (form & (PhysicalDamageForm.Slashing | PhysicalDamageForm.Piercing)) != 0;
        }

        internal static Mesh Build(Transform root)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            foreach (var start in root.GetComponentsInChildren<Transform>(true))
            {
                if (start.name != BaseName || start.parent == null) continue;
                var tip = start.parent.Find(TipName);
                if (tip == null) continue;
                var surface = new List<Vector3>();
                foreach (Transform point in start.parent)
                    if (point.name.StartsWith(Surface, StringComparison.Ordinal))
                        surface.Add(root.InverseTransformPoint(point.position));
                AddBlade(root.InverseTransformPoint(start.position), root.InverseTransformPoint(tip.position),
                    surface, vertices, indices);
            }
            if (vertices.Count == 0) return null;
            var mesh = new Mesh { name = "Blood Berserker blade film" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            var colors = new Color32[vertices.Count];
            for (int i = 0; i < colors.Length; ++i) colors[i] = Blood;
            mesh.colors32 = colors;
            mesh.RecalculateBounds();
            return mesh;
        }

        internal static void AddBlade(Vector3 start, Vector3 tip, List<Vector3> surface,
            List<Vector3> vertices, List<int> indices)
        {
            var axis = tip - start;
            float length = axis.magnitude;
            if (length <= Mathf.Epsilon) return;
            axis /= length;
            float radius = Mathf.Clamp(length * RadiusRatio, MinimumRadius, MaximumRadius);
            float Depth(Vector3 p) => Vector3.Dot(p - start, axis);
            surface.Sort((a, b) => Depth(a).CompareTo(Depth(b)));
            var path = new List<Vector3> { start };
            // Ignore grip/guard samples and group points across the same cross-section.
            for (int i = 0; i < surface.Count;)
            {
                float depth = Depth(surface[i]);
                var center = surface[i++];
                int count = 1;
                while (i < surface.Count && Depth(surface[i]) - depth < radius)
                { center += surface[i++]; ++count; }
                center /= count;
                if (depth > radius && Depth(center) < length - radius) path.Add(center);
            }
            path.Add(tip);
            var normal = Vector3.Cross(axis, Mathf.Abs(axis.y) < .9f ? Vector3.up : Vector3.right).normalized;
            int first = vertices.Count;
            for (int i = 0; i < path.Count; ++i)
            {
                var tangent = (path[Math.Min(i + 1, path.Count - 1)] - path[Math.Max(i - 1, 0)]).normalized;
                var side = Vector3.Cross(tangent, normal).normalized;
                normal = Vector3.Cross(side, tangent).normalized;
                float t = Depth(path[i]) / length;
                float width = i == path.Count - 1 ? 0f : radius * Mathf.Lerp(1f, .35f, t);
                for (int j = 0; j < Sides; ++j)
                {
                    float angle = j * (2f * Mathf.PI / Sides);
                    vertices.Add(path[i] + width * (Mathf.Cos(angle) * normal + Mathf.Sin(angle) * side));
                    if (i == 0) continue;
                    int a = first + (i - 1) * Sides + j;
                    int b = first + (i - 1) * Sides + (j + 1) % Sides;
                    indices.Add(a); indices.Add(b); indices.Add(a + Sides);
                    if (i < path.Count - 1) { indices.Add(b); indices.Add(b + Sides); indices.Add(a + Sides); }
                }
            }
        }
    }
}
