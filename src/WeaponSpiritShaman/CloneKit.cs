using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace WeaponSpiritShaman
{
     
    // 값 복사 = 새 인스턴스 + 모든 인스턴스 필드(기반 형식 포함) 복사. 규칙:
    //   - CloneRules.SkipFields 는 복사하지 않는다(정체·원소 등록부·컴포넌트 배열·원형 연결·캐시 — 표의 이유 참조). 생성자 기본값이 남는다.
    //   - 이름에 「cache」가 든 필드가 표에 없으면 던진다(모르는 캐시를 조용히 복사하지 않는다 — 시험 `clone rules:` 가 dll 로도 확인).
    //   - 배열·List<T> 값은 「새 컨테이너 + 같은 원소」로 복사한다(컨테이너는 절대 공유하지 않는다). 원소 객체를 바꿔야 하는 곳은 호출자가 새 원소로 교체한다.
    //   - SimpleBlueprint.m_AllElements 가 비어 있지 않은 원본은 던진다(원소 복사는 지원하지 않는다 — 공유하면 OnEnable 이 본편 원소의 Owner 를 바꾼다).
    // 결과가 본편 그래프와 참조가 겹치지 않는지는 Isolation.VerifyAfterBuild(CloneGraph.Verify)와 점검 S22 가 실제 객체로 단언한다.
    public static class CloneKit
    {
        const BindingFlags DeclaredInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        const string ElementsOwnerType = "Kingmaker.Blueprints.SimpleBlueprint";
        const string ElementsField = "m_AllElements";

        // 새 T 를 만들어 src 를 값 복사해 돌려준다. src 의 실제 형식은 T 와 같아야 한다(파생 형식의 필드를 잃지 않게).
        // 시험 WriteScan 은 이 메서드의 반환값을 「우리 새 인스턴스」로 본다(원천 규칙) — 그 근거는 IL 시험 `wiring: CloneKit.Copy returns the Activator.CreateInstance result`.
        public static T Copy<T>(T src) where T : class, new()
        {
            if (src == null) throw new ArgumentNullException("src", "WeaponSpiritShaman: CloneKit.Copy<" + typeof(T).Name + "> source is null");
            if (src.GetType() != typeof(T)) throw new InvalidOperationException("WeaponSpiritShaman: CloneKit.Copy<" + typeof(T).Name + "> got a " + src.GetType().FullName + " (exact type required)");
            var dst = new T();
            CopyInto(src, dst);
            return dst;
        }

        // dst(호출자가 방금 만든 우리 객체)에 src 의 필드를 값 복사한다. 두 객체의 실제 형식이 같아야 한다.
        public static void CopyInto(object src, object dst)
        {
            if (src == null) throw new ArgumentNullException("src");
            if (dst == null) throw new ArgumentNullException("dst");
            if (ReferenceEquals(src, dst)) throw new InvalidOperationException("WeaponSpiritShaman: CloneKit.CopyInto source and destination are the same object");
            // 필드 목록은 우리 새 객체(dst)의 형식에서 얻는다(두 형식은 같아야 한다) — 원본 객체에서 얻은 리플렉션 핸들로 쓰지 않는다.
            var type = dst.GetType();
            if (src.GetType() != type) throw new InvalidOperationException("WeaponSpiritShaman: CloneKit.CopyInto " + src.GetType().FullName + " -> " + type.FullName + " (types differ)");
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                string decl = t.FullName.Replace('+', '/');
                foreach (var f in CloneGraph.FieldsInDeclarationOrder(t, DeclaredInstance))  
                {
                    bool skip = CloneRules.Skip(decl, f.Name);
                    if (!skip && f.Name.IndexOf(CloneRules.CacheMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                        throw new InvalidOperationException("WeaponSpiritShaman: CloneKit: " + decl + "." + f.Name + " looks like a cache and is not in CloneRules.SkipFields (refusing to copy it silently)");
                    if (decl == ElementsOwnerType && f.Name == ElementsField)
                    {
                        var elements = f.GetValue(src) as ICollection;
                        if (elements != null && elements.Count > 0)
                            throw new InvalidOperationException("WeaponSpiritShaman: CloneKit: source " + type.Name + " has " + elements.Count + " element(s) in m_AllElements (element copy is not supported)");
                    }
                    if (skip) continue;
                    f.SetValue(dst, FreshContainer(f.GetValue(src)));
                }
            }
        }

        // 배열·List<T> 는 새 컨테이너(같은 원소), 그 밖의 값은 그대로.
        static object FreshContainer(object v)
        {
            if (v == null || v is string) return v;
            var a = v as Array;
            if (a != null) return a.Clone();
            var vt = v.GetType();
            if (vt.IsGenericType && vt.GetGenericTypeDefinition() == typeof(List<>)) return Activator.CreateInstance(vt, v);
            return v;
        }
    }
}
