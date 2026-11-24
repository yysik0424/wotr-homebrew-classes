// 출처: 워크트리 wotr-class-mod-wt\chimera-monk 의 src\ChimeraMonk\CloneKit.cs 를 복사(그 워크트리 HEAD 42423c24 시점, 이 파일의 마지막 변경 커밋 8aa39582) — 네임스페이스와 오류 문구의 모드 이름만 바꿨다(개편 계획 2026-11-16 Task O4 · K12).
 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChaosWarrior
{
     
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
            if (maxDepth < 0) throw new InvalidOperationException("ChaosWarrior: CloneKit depth exhausted at " + v.GetType().FullName + " (object graph deeper than expected)");
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
                object boxed = v; // 상자 사본 — 필드를 떼어 넣고 그대로 돌려준다
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
     
    //   최상위 컴포넌트만 얕은 복제(MemberwiseClone) → DetachFields(keepShared, 깊이 6)로 배열·리스트·참조 래퍼 같은 가변 하위 객체만 분리.
    //   keepShared(블루프린트·Unity 객체·다른 컴포넌트·Element)는 공유 — 그래서 component → OwnerBlueprint → Components → component 순환을 따라 내려가지 않는다.
    //   소유자 재결합은 게임 쪽 BlueprintUtil.AddComponents(BlueprintUtil.cs:63-78, OnEnable 이 OwnerBlueprint 를 새 블루프린트로)가 한다.
    public static class CloneBoundary
    {
        public const int DetachDepth = 6; // TowerShield.Copy 와 같은 깊이(ContextRankConfig 배열 → 참조 객체 → 필드 여유, 넘으면 CloneKit 이 던진다)
        // 행동(ActionList·ConditionsChecker) 깊은 복제(BlueprintClone.CloneElements) 깊이. 출처: blueprints.zip 실측 최대 7(ScaledFistRedBreath AbilityEffectRunAction →
        // ContextActionDealDamage.Duration.DiceCountValue.m_CustomProperty 참조 → BlueprintGuid → Guid, 2026-11-15, 시험 `exec: clone depths`) + 여유 1(시험 CloneDepthMargin) = 8. 넘으면 CloneKit 이 던진다.
        public static readonly int ActionDepth = 8;

        static readonly System.Reflection.MethodInfo Mwc = typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static object CopyComponent(object src, Func<object, bool> keepShared)
        {
            if (src == null) throw new ArgumentNullException("src");
            if (keepShared == null) throw new ArgumentNullException("keepShared");
            var copy = Mwc.Invoke(src, null);
            if (copy == null || ReferenceEquals(copy, src) || copy.GetType() != src.GetType()) throw new InvalidOperationException("ChaosWarrior: MemberwiseClone of " + src.GetType().Name + " failed");
            CloneKit.DetachFields(copy, keepShared, DetachDepth);
            return copy;
        }
    }
}
