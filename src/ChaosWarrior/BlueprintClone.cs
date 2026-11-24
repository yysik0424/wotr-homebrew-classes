// 출처: 워크트리 wotr-class-mod-wt\chimera-monk 의 src\ChimeraMonk\BlueprintClone.cs 를 복사(그 워크트리 HEAD 42423c24 시점, 이 파일의 마지막 변경 커밋 8aa39582) — 네임스페이스와 오류 문구의 모드 이름만 바꿨다(개편 계획 2026-11-16 Task O4 · K12).
 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
     
    //  ① 새 블루프린트 = BlueprintUtil.Create(우리 GUID·이름) 안에서 원본 필드를 CloneKit.CopyOf 로 옮김(가변 하위 객체 분리, 블루프린트·Unity·컴포넌트·Element 공유).
    //  ② 컴포넌트 = CloneBoundary.CopyComponent(최상위 얕은 복제 + DetachFields 깊이 6) → 행동(ActionList·ConditionsChecker 의 Element)은 깊게 복제하고 Owner 를 새 블루프린트로(Bulwark CloneGainedActions 선례).
    //  ③ AddComponents 로 새 소유자에 재결합. ④ CloneKit.Shared(복제, 원본) 가 비어 있지 않으면 던진다.
    internal static class BlueprintClone
    {
        sealed class RefEq : IEqualityComparer<object>
        {
            public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
            public int GetHashCode(object o) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o); }
        }

        internal static bool KeepShared(object v) { return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent || v is Element; }
        static bool KeepSharedExceptElements(object v) { return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent; }

        static IEnumerable<FieldInfo> Fields(Type t)
        {
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                foreach (var f in cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!f.IsLiteral && !f.IsInitOnly) yield return f;
        }

        internal static T Clone<T>(T src, string name, string guid, Func<BlueprintComponent, bool> keepComponent) where T : BlueprintScriptableObject, new()
        {
            if (src == null) throw new ArgumentNullException("src");
            if (src.GetType() != typeof(T)) throw new InvalidOperationException("ChaosWarrior: clone of " + src.name + " as " + typeof(T).Name + " but it is " + src.GetType().Name);
            var clone = Create<T>(name, guid, c =>
            {
                foreach (var f in Fields(typeof(T)))
                {
                    if (f.Name == "AssetGuid" || f.Name == "name" || f.Name == "Components" || f.Name == "m_AllElements") continue;
                    f.SetValue(c, CloneKit.CopyOf(f.GetValue(src), KeepShared, CloneBoundary.DetachDepth));
                }
                c.ComponentsArray = new BlueprintComponent[0];
            });
            var comps = (src.ComponentsArray ?? new BlueprintComponent[0]).Where(x => x != null && keepComponent(x)).Select(x =>
            {
                var cc = (BlueprintComponent)CloneBoundary.CopyComponent(x, KeepShared);
                CloneElements(cc, clone, CloneBoundary.ActionDepth);
                return cc;
            }).ToArray();
            if (comps.Length > 0) AddComponents(clone, comps);
            var shared = CloneKit.Shared(clone, src, KeepShared, CloneBoundary.DetachDepth);
            // 무기: OnEnableWithLibrary 가 Prototype 을 무기 형식 블루프린트 소유 객체로 다시 묶는다(ClonePolicy.WeaponPrototypePath). 그 경로 하나만, 조건이 성립할 때만 허용.
            bool prototypeTypeOwned = false;
            var cw = clone as Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon;
            if (cw != null)
            {
                var sw = (Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon)(object)src;
                prototypeTypeOwned = ClonePolicy.TypeOwnedPrototype(
                    cw.VisualParameters == null ? null : cw.VisualParameters.Prototype, cw.Type == null ? null : cw.Type.VisualParameters,
                    sw.VisualParameters == null ? null : sw.VisualParameters.Prototype, sw.Type == null ? null : sw.Type.VisualParameters);
            }
            shared = ClonePolicy.Unexplained(shared, prototypeTypeOwned);
            if (shared.Count > 0) throw new InvalidOperationException("ChaosWarrior: clone " + name + " shares mutable objects with " + src.name + ": " + string.Join(", ", shared));
            return clone;
        }

        // 컴포넌트의 최상위 필드 중 ActionList·ConditionsChecker 를 새 것으로(행동 깊은 복제 + Owner = 새 블루프린트).
        // maxDepth = 행동 트리 깊이(호출자는 CloneBoundary.ActionDepth — 지점마다 blueprints.zip 실측 + 여유 이상인지 시험 `exec: clone depths` 가 잠근다).
        internal static void CloneElements(object component, SimpleBlueprint owner, int maxDepth)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException("maxDepth", maxDepth, "ChaosWarrior: CloneElements depth must be >= 0");
            foreach (var f in Fields(component.GetType()))
            {
                var v = f.GetValue(component);
                if (v is ActionList || v is ConditionsChecker)
                {
                    var copy = CloneKit.CopyOf(v, KeepSharedExceptElements, maxDepth);
                    SetOwners(copy, owner, maxDepth, new HashSet<object>(new RefEq()));
                    f.SetValue(component, copy);
                }
            }
        }

        static void SetOwners(object o, SimpleBlueprint owner, int depth, HashSet<object> seen)
        {
            if (o == null || depth < 0 || o is string || KeepSharedExceptElements(o) || o.GetType().IsPrimitive || o.GetType().IsEnum || !seen.Add(o)) return;
            var e = o as Element;
            if (e != null) e.Owner = owner;
            var arr = o as IEnumerable;
            if (arr != null && !(o is string)) { foreach (var x in arr) SetOwners(x, owner, depth - 1, seen); return; }
            if (o.GetType().IsValueType) return;
            foreach (var f in Fields(o.GetType())) SetOwners(f.GetValue(o), owner, depth - 1, seen);
        }
    }
}
