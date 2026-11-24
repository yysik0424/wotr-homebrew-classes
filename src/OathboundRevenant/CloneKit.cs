using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OathboundRevenant
{
    // [순수] 얕은 복제본의 하위 객체 떼어내기 + 공유 검사 — src\BulwarkMage\CloneKit.cs 복제(계획 Global Constraints 「복제 도구 하나」, 복제 경계 = Bulwark TowerShield.cs:51-62). 게임 형식 없음(modinfo testSources) — 시험 exe 가 일반 C# 객체로 실행한다.
    // 얕은 복제(MemberwiseClone)는 하위 객체(ContextValue·배열·리스트·참조 객체 …)를 원본과 공유한다 → 나중 코드가 복제본의 하위 객체를 고치면 본편이 바뀐다.
    // Detach: 복제본의 참조형 필드마다 「공유해도 되는 것」(keepShared 가 참: 블루프린트·Unity 객체·문자열·형식 등 불변/소유 객체) 밖이면 새 사본으로 바꾼다(재귀, 깊이 maxDepth).
    // Shared: 복제본과 원본에서 같은 경로의 같은 인스턴스(keepShared 밖)를 찾아 경로 목록으로(빈 목록 = 공유 없음). VerifyClones 가 이것이 비어 있지 않으면 빌드를 멈춘다.
    public static class CloneKit
    {
        static readonly MethodInfo Mwc = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        static IEnumerable<FieldInfo> Fields(Type t)
        {
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                foreach (var f in cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!f.IsLiteral) yield return f;
        }

        static bool Mutable(object v, Func<object, bool> keepShared)
        {
            if (v == null || v is string || v is Type || v is Delegate || v is MemberInfo) return false;
            var t = v.GetType();
            if (t.IsPrimitive || t.IsEnum || t == typeof(decimal)) return false;
            return !keepShared(v);
        }

        public static object CopyOf(object v, Func<object, bool> keepShared, int maxDepth)
        {
            if (keepShared == null) throw new ArgumentNullException("keepShared");
            if (!Mutable(v, keepShared)) return v;
            if (maxDepth < 0) throw new InvalidOperationException("OathboundRevenant: CloneKit depth exhausted at " + v.GetType().FullName + " (object graph deeper than expected)");
            var arr = v as Array;
            if (arr != null)
            {
                var c = (Array)arr.Clone();
                for (int i = 0; i < c.Length; i++) c.SetValue(CopyOf(c.GetValue(i), keepShared, maxDepth - 1), i);
                return c;
            }
            var list = v as IList;
            if (list != null && v.GetType().IsGenericType && v.GetType().GetGenericTypeDefinition() == typeof(List<>))
            {
                var c = (IList)Activator.CreateInstance(v.GetType());
                foreach (var x in list) c.Add(CopyOf(x, keepShared, maxDepth - 1));
                return c;
            }
            var dict = v as IDictionary;
            if (dict != null)
            {
                var c = (IDictionary)Activator.CreateInstance(v.GetType());
                foreach (DictionaryEntry e in dict) c[e.Key] = CopyOf(e.Value, keepShared, maxDepth - 1);
                return c;
            }
            if (v.GetType().IsValueType)
            {
                object boxed = System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(v); // 새 상자 사본(값 형식은 복사본) — 필드를 떼어 넣고 그대로 돌려준다
                DetachFields(boxed, keepShared, maxDepth - 1);
                return boxed;
            }
            var copy = Mwc.Invoke(v, null);
            DetachFields(copy, keepShared, maxDepth - 1);
            return copy;
        }

        // o 의 참조형 필드를 사본으로 바꾼다(o 자체는 그대로 — o 는 이미 새 객체여야 한다).
        public static void DetachFields(object o, Func<object, bool> keepShared, int maxDepth)
        {
            if (o == null) throw new ArgumentNullException("o");
            if (keepShared == null) throw new ArgumentNullException("keepShared");
            foreach (var f in Fields(o.GetType()))
            {
                if (f.IsInitOnly) continue;
                var v = f.GetValue(o);
                if (!Mutable(v, keepShared)) continue;
                f.SetValue(o, CopyOf(v, keepShared, maxDepth));
            }
        }

        // 공유 경로(복제본·원본에서 같은 경로의 같은 가변 인스턴스). 경로 = 필드 이름 / [색인].
        public static List<string> Shared(object clone, object source, Func<object, bool> keepShared, int maxDepth)
        {
            if (keepShared == null) throw new ArgumentNullException("keepShared");
            var found = new List<string>();
            Walk(clone, source, "", keepShared, maxDepth, found, new HashSet<object>(new RefEq()));
            return found;
        }

        sealed class RefEq : IEqualityComparer<object>
        {
            public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
            public int GetHashCode(object o) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o); }
        }

        static void Walk(object a, object b, string path, Func<object, bool> keep, int depth, List<string> found, HashSet<object> seen)
        {
            if (a == null || b == null || depth < 0) return;
            if (!a.GetType().IsValueType && !seen.Add(a)) return;
            var arrA = a as Array;
            if (arrA != null)
            {
                var arrB = b as Array;
                if (arrB == null) return;
                for (int i = 0; i < Math.Min(arrA.Length, arrB.Length); i++) Step(arrA.GetValue(i), arrB.GetValue(i), path + "[" + i + "]", keep, depth, found, seen);
                return;
            }
            if (a is IDictionary) return; // 사전은 인스턴스 동일성만 본다(내부 버킷은 구현 세부)
            var la = a as IList;
            if (la != null && !(a is Array))
            {
                var lb = b as IList;
                if (lb == null) return;
                for (int i = 0; i < Math.Min(la.Count, lb.Count); i++) Step(la[i], lb[i], path + "[" + i + "]", keep, depth, found, seen);
                return;
            }
            if (a.GetType() != b.GetType()) return;
            foreach (var f in Fields(a.GetType())) Step(f.GetValue(a), f.GetValue(b), path + "." + f.Name, keep, depth, found, seen);
        }

        static void Step(object va, object vb, string path, Func<object, bool> keep, int depth, List<string> found, HashSet<object> seen)
        {
            if (!Mutable(va, keep)) return;
            if (!va.GetType().IsValueType && ReferenceEquals(va, vb)) { found.Add(path); return; }
            Walk(va, vb, path, keep, depth - 1, found, seen);
        }
    }
    // [순수] 객체 그래프 방문(필드·배열·리스트, stop 이 참인 값은 들어가지 않음, 깊이 maxDepth). 복제 뒤 행동(Element)의 Owner 를 새 블루프린트로 바꾸는 데 쓴다.
    public static class GraphWalk
    {
        public static void Visit(object root, Func<object, bool> stop, int maxDepth, Action<object> visit)
        {
            if (stop == null) throw new ArgumentNullException("stop");
            if (visit == null) throw new ArgumentNullException("visit");
            Walk(root, stop, maxDepth, visit, new HashSet<object>(new RefEq()));
        }

        sealed class RefEq : IEqualityComparer<object>
        {
            public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
            public int GetHashCode(object o) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o); }
        }

        static void Walk(object o, Func<object, bool> stop, int depth, Action<object> visit, HashSet<object> seen)
        {
            if (o == null || o is string || o is Type || o is Delegate || o is MemberInfo) return;
            var t = o.GetType();
            if (t.IsPrimitive || t.IsEnum || t == typeof(decimal) || t.IsValueType) return;
            if (stop(o) || !seen.Add(o)) return;
            if (depth < 0) throw new InvalidOperationException("OathboundRevenant: GraphWalk depth exhausted at " + t.FullName);
            visit(o);
            var arr = o as Array;
            if (arr != null) { foreach (var x in arr) Walk(x, stop, depth - 1, visit, seen); return; }
            var list = o as IList;
            if (list != null) { foreach (var x in list) Walk(x, stop, depth - 1, visit, seen); return; }
            if (o is IDictionary) return;
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                foreach (var f in cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!f.IsLiteral) Walk(f.GetValue(o), stop, depth - 1, visit, seen);
        }
    }
     
    // keepShared = 공유로 남겨도 되는 것(게임 쪽 = Bulwark TowerShield.KeepShared: 블루프린트·Unity 객체·다른 컴포넌트·Element). 소유자 순환(OwnerBlueprint)은 keepShared 가 끊는다.
    // 소유자 재결합은 게임 쪽에서 공용 BlueprintUtil.AddComponents 가 한다(OnEnable 로 OwnerBlueprint 를 새 블루프린트로).
    public static class CloneBoundary
    {
        static readonly MethodInfo Mwc = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        // maxDepth: 호출자가 원본 컴포넌트 모양에 맞춰 고른다 — 단순 컴포넌트 Dials.CloneDepth, 행동 트리(ActionList·ConditionsChecker)를 담은 컴포넌트 Dials.ActionCloneDepth.
        // 지점마다 넘긴 깊이 ≥ blueprints.zip 원본 실측 + 여유 는 시험 `exec: copy component depths` 가 잠근다(2026-11-15 게임 로그: 깊이 6 으로 LayOnHandsOthers 행동 트리 복제 → depth exhausted).
        public static T CopyComponent<T>(T src, Func<object, bool> keepShared, int maxDepth) where T : class
        {
            if (src == null) throw new ArgumentNullException("src");
            if (keepShared == null) throw new ArgumentNullException("keepShared");
            if (maxDepth < 0) throw new ArgumentOutOfRangeException("maxDepth", maxDepth, "OathboundRevenant: CopyComponent depth must be >= 0");
            var copy = Mwc.Invoke(src, null) as T;
            if (copy == null || ReferenceEquals(copy, src) || copy.GetType() != src.GetType()) throw new InvalidOperationException("OathboundRevenant: MemberwiseClone of " + src.GetType().Name + " failed");
            CloneKit.DetachFields(copy, keepShared, maxDepth);
            return copy;
        }
    }
}