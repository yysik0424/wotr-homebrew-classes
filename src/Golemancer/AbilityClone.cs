using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.ResourceLinks;
using UnityEngine;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 특기 능력 DC 직업 재지정 얕은 복제(계획 T4 AbilityClone, 설계 §12 정정 6, 정찰 §6·§20).
    //   Repoint: 원본 MemberwiseClone → AssetGuid·name → ComponentsArray = 원본 배열의 새 복사, 그 안의 ContextCalculateAbilityParamsBasedOnClass 1개만
    //            CloneKit.Copy 새 인스턴스(m_CharacterClass = 펫 직업, OwnerBlueprint = 복제본)로 교체 → AddCachedBlueprint. **OnEnable 금지**(공유 컴포넌트의 OwnerBlueprint·원소 Owner 를 바꾼다).
    //   VerifyShare(R2-08): 순수 SharePolicy.Violations 로 판정 — 위반 1개라도 있으면 던진다(빌드 실패). 소유자 스냅샷은 Repoint 가 복제 전에 찍는다.
    // 공유 컴포넌트는 실행 중 읽기 전용(정찰 §20 stfld 검사) — 다른 모든 본편 유닛도 같은 인스턴스를 공유하는 것과 같은 조건.
    public static class AbilityClone
    {
        static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        sealed class Node
        {
            public readonly string Label; public readonly object Ref; public readonly object Owner;
            public Node(string label, object r, object owner) { Label = label; Ref = r; Owner = owner; }
        }

        sealed class Record
        {
            public string Name;
            public BlueprintScriptableObject Src, Clone;
            public BlueprintComponent[] SrcArray, SrcElems;
            public int ReplacedIndex;
            public ContextCalculateAbilityParamsBasedOnClass SrcDc, Replaced;
            public BlueprintCharacterClass SrcDcClass;
            public List<Node> Owners;
            public int AreaIndex = -1;
            public AddAreaEffect SummerAreaLink;
        }

        static readonly List<Record> records = new List<Record>();
        static readonly Dictionary<BlueprintScriptableObject, BlueprintScriptableObject> sourcesByClone =
            new Dictionary<BlueprintScriptableObject, BlueprintScriptableObject>();




        // 만든 복제본(Create 밖 — BlueprintUtil.OwnBlueprints 에 없다). Icons.Apply 가 우리 블루프린트 목록에 더한다.
        public static IList<BlueprintScriptableObject> Clones { get { return sourcesByClone.Keys.ToList().AsReadOnly(); } }




        public static BlueprintScriptableObject Repoint(string srcGuid, string newGuid, string name)
        {
            if (string.IsNullOrEmpty(srcGuid) || string.IsNullOrEmpty(newGuid) || string.IsNullOrEmpty(name)) throw new ArgumentException("Golemancer: AbilityClone.Repoint needs source guid, new guid and name");
            if (PetClass.Class == null) throw new InvalidOperationException("Golemancer: AbilityClone.Repoint before PetClass.Build()");
            if (records.Any(r => r.Clone.AssetGuid.ToString() == newGuid || r.Src.AssetGuid.ToString() == srcGuid)) throw new InvalidOperationException("Golemancer: AbilityClone.Repoint " + srcGuid + " -> " + newGuid + " twice");
            var g = BlueprintGuid.Parse(newGuid);
            if (ResourcesLibrary.BlueprintsCache.Load(g) != null) throw new InvalidOperationException("Golemancer: blueprint already exists in cache: " + name + " " + newGuid);
            var src = Get<BlueprintScriptableObject>(srcGuid);
            var comps = src.ComponentsArray;
            if (comps == null || comps.Any(c => c == null)) throw new InvalidOperationException("Golemancer: " + src.name + " components missing or null");
            // 원본 계약(C2): 직업 기반 DC 계산기 정확히 1개, 엔진이 찾는 계산기(ContextAbilityParamsCalculator 첫 번째)가 그것.
            var dcIdx = Enumerable.Range(0, comps.Length).Where(i => comps[i] is ContextCalculateAbilityParamsBasedOnClass).ToArray();
            if (dcIdx.Length != 1 || comps.OfType<ContextAbilityParamsCalculator>().Count() != 1) throw new InvalidOperationException("Golemancer: " + src.name + " has " + dcIdx.Length + " class-based DC component(s) and " + comps.OfType<ContextAbilityParamsCalculator>().Count() + " params calculator(s), expected exactly 1 each");
            var srcDc = (ContextCalculateAbilityParamsBasedOnClass)comps[dcIdx[0]];
            if (srcDc.CharacterClass == null) throw new InvalidOperationException("Golemancer: " + src.name + " DC component has no class");

            var rec = new Record
            {
                Name = name, Src = src, SrcArray = comps, SrcElems = comps.ToArray(), ReplacedIndex = dcIdx[0], SrcDc = srcDc, SrcDcClass = srcDc.CharacterClass,
                Owners = OwnerNodes(comps, dcIdx[0]),
            };

            var clone = ShallowCopy(src);
            clone.AssetGuid = g;
            clone.name = name;
            var dc = CloneKit.Copy(srcDc);
            SetField(dc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.PetClass));
            dc.name = "$" + dc.GetType().Name + "$" + newGuid + "#" + dcIdx[0];
            dc.OwnerBlueprint = clone;
            var arr = new BlueprintComponent[comps.Length];
            for (int i = 0; i < comps.Length; i++) arr[i] = i == dcIdx[0] ? dc : comps[i];
            if (srcGuid == Guids.Game.GolemSummerEntangleAuraBuff)
            {
                var originalLink = comps.OfType<AddAreaEffect>().Single();
                rec.AreaIndex = Array.IndexOf(comps, originalLink);
                rec.SummerAreaLink = BuildSummerAreaLink(originalLink, clone);
                arr[rec.AreaIndex] = rec.SummerAreaLink;
            }
            clone.ComponentsArray = arr;
            RegisterClone(src, clone);
            rec.Clone = clone;
            rec.Replaced = dc;
            records.Add(rec);
            return clone;
        }

        // 얕은 복제(MemberwiseClone) — 최상위 객체만 새것, 필드 값(하위 객체)은 원본과 공유. 시험 WriteScan 원천 규칙(T4)이 이 메서드의 결과를 「최상위만 깨끗」으로 본다.
        static BlueprintScriptableObject ShallowCopy(BlueprintScriptableObject src)
        {
            if (MemberwiseCloneMethod == null) throw new MissingMethodException("Golemancer: object.MemberwiseClone not found");
            var copy = MemberwiseCloneMethod.Invoke(src, null) as BlueprintScriptableObject;
            if (copy == null || ReferenceEquals(copy, src) || copy.GetType() != src.GetType()) throw new InvalidOperationException("Golemancer: MemberwiseClone of " + src.name + " failed");
            return copy;
        }

        static AddAreaEffect BuildSummerAreaLink(AddAreaEffect originalLink, BlueprintScriptableObject owner)
        {
            var source = originalLink.AreaEffect;
            if (source == null || source.AssetGuid.ToString() != Guids.Game.GolemSummerAura)
                throw new InvalidOperationException("Golemancer: unexpected native summer entangle area");
            var id = BlueprintGuid.Parse(Guids.SummerEntangleArea);
            if (ResourcesLibrary.BlueprintsCache.Load(id) != null) throw new InvalidOperationException("Golemancer: summer area GUID already registered");
            var area = (BlueprintAbilityAreaEffect)ShallowCopy(source);
            area.AssetGuid = id;
            area.name = "GolemancerSummerEntangleArea";
            area.Shape = AreaEffectShape.Cylinder;
            area.AffectEnemies = true;
            area.Fx = source.Fx ?? new PrefabLink();
            var originals = source.ComponentsArray;
            var components = new BlueprintComponent[originals.Length];
            var actions = originals.OfType<AbilityAreaEffectRunAction>().Single();
            var ownedActions = CloneKit.Copy(actions);
            ownedActions.OwnerBlueprint = area;
            for (int i = 0; i < originals.Length; i++)
                components[i] = ReferenceEquals(originals[i], actions) ? ownedActions : originals[i];
            area.ComponentsArray = components;
            RegisterClone(source, area);
            var link = CloneKit.Copy(originalLink);
            SetField(link, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(Guids.SummerEntangleArea));
            link.OwnerBlueprint = owner;
            return link;
        }

        static void RegisterClone(BlueprintScriptableObject source, BlueprintScriptableObject clone)
        {
            ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(clone.AssetGuid, clone);
            sourcesByClone.Add(clone, source);
        }

        // R2-08 공유 정책 검사. 위반 = 던짐(빌드 중이면 BuildFailed). 점검 S10·S16 은 Violations(…) 로 같은 판정을 다시 부른다.
        public static void VerifyShare(BlueprintScriptableObject clone, BlueprintScriptableObject src, string[] allowedKinds)
        {
            var v = Violations(clone, src, allowedKinds);
            if (v.Length > 0) throw new InvalidOperationException("Golemancer: AbilityClone share policy violated: " + string.Join(" | ", v));
        }

        public static string[] Violations(BlueprintScriptableObject clone, BlueprintScriptableObject src, string[] allowedKinds)
        {
            return Violations(clone, src, allowedKinds, null, null);
        }

        // identifyForeign(복제본 칸, 원본 칸, 복제본) = 그 칸을 사본으로 바꾼 다른 모드 이름(식별 못 하면 null). null = 분리 없음(빌드 검사 — 엄격). 점검(S10·S11·S16)만 넘긴다.
        public static string[] Violations(BlueprintScriptableObject clone, BlueprintScriptableObject src, string[] allowedKinds,
            Func<BlueprintComponent, BlueprintComponent, BlueprintScriptableObject, string> identifyForeign, List<string> foreignNotes)
        {
            if (clone == null) throw new ArgumentNullException("clone");
            if (src == null) throw new ArgumentNullException("src");
            var rec = records.SingleOrDefault(r => ReferenceEquals(r.Clone, clone));
            if (rec == null) throw new InvalidOperationException("Golemancer: " + clone.name + " was not made by AbilityClone.Repoint");
            if (!ReferenceEquals(rec.Src, src)) throw new InvalidOperationException("Golemancer: " + clone.name + " was cloned from " + rec.Src.name + ", not " + src.name);
            var cc = clone.ComponentsArray ?? new BlueprintComponent[0];
            var sc = src.ComponentsArray ?? new BlueprintComponent[0];
            var same = Enumerable.Range(0, cc.Length).Select(i => i < sc.Length && ReferenceEquals(cc[i], sc[i])).ToArray();
            var kinds = cc.Select(c => c == null ? "null" : c.GetType().Name).ToArray();
            var now = OwnerNodes(sc, rec.ReplacedIndex);
            var changes = new List<string>();
            if (now.Count != rec.Owners.Count) changes.Add("node count " + rec.Owners.Count + " -> " + now.Count);
            for (int i = 0; i < Math.Min(now.Count, rec.Owners.Count); i++)
                if (!ReferenceEquals(now[i].Ref, rec.Owners[i].Ref) || !ReferenceEquals(now[i].Owner, rec.Owners[i].Owner)) changes.Add(rec.Owners[i].Label);
            bool idxOk = rec.ReplacedIndex >= 0 && rec.ReplacedIndex < cc.Length;
            var replaced = idxOk ? cc[rec.ReplacedIndex] as ContextCalculateAbilityParamsBasedOnClass : null;
            var o = new ShareObservation(clone.name, sc.Length, cc.Length, rec.ReplacedIndex, same, kinds, changes.ToArray(),
                ReferenceEquals(sc, rec.SrcArray) && sc.SequenceEqual(rec.SrcElems),
                sc.Length > rec.ReplacedIndex && ReferenceEquals(sc[rec.ReplacedIndex], rec.SrcDc) && ReferenceEquals(rec.SrcDc.CharacterClass, rec.SrcDcClass),
                replaced != null && ReferenceEquals(replaced, rec.Replaced) && ReferenceEquals(replaced.OwnerBlueprint, clone),
                replaced != null && ReferenceEquals(replaced.CharacterClass, PetClass.Class),
                ReferenceEquals(clone.GetComponent<ContextAbilityParamsCalculator>(), rec.Replaced));
            if (rec.AreaIndex >= 0)
            {
                var native = src.GetComponent<AddAreaEffect>()?.AreaEffect;
                var owned = rec.SummerAreaLink.AreaEffect;
                if (rec.AreaIndex >= cc.Length || !ReferenceEquals(cc[rec.AreaIndex], rec.SummerAreaLink) || !ReferenceEquals(rec.SummerAreaLink.OwnerBlueprint, clone)
                    || native == null || native.AssetGuid.ToString() != Guids.Game.GolemSummerAura || owned == null || owned.AssetGuid.ToString() != Guids.SummerEntangleArea
                    || owned.Shape != AreaEffectShape.Cylinder || !owned.AffectEnemies || owned.Size.Value != native.Size.Value || owned.Fx == null)
                    return new[] { clone.name + ": invalid summer area link, shape, targeting or radius" };
            }
            string[] foreign = null;
            if (identifyForeign != null)
            {
                foreign = new string[cc.Length];
                for (int i = 0; i < cc.Length; i++)
                    if (i != rec.ReplacedIndex && i != rec.AreaIndex && !same[i] && i < sc.Length) foreign[i] = identifyForeign(cc[i], sc[i], clone);
            }
            return SharePolicy.Violations(o, MaterialCatalog.DcComponentKind, allowedKinds, foreign, foreignNotes, rec.AreaIndex);
        }

        // 점검(S10 ability-clone-share-policy · S16 shared-owner-unchanged-after-use): 만든 복제본 전부를 카탈로그 허용 목록으로 다시 판정.
        public static string[] VerifyAllNow()
        {
            return VerifyAllNow(null, null);
        }

        // 점검용: 다른 모드가 바꾼 칸(identifyForeign 이 식별)은 foreignNotes 로 따로 센다 — SharePolicy.Violations(…, foreignCopiedBy, …) 주석.
        public static string[] VerifyAllNow(Func<BlueprintComponent, BlueprintComponent, BlueprintScriptableObject, string> identifyForeign, List<string> foreignNotes)
        {
            if (records.Count == 0) throw new InvalidOperationException("Golemancer: no AbilityClone records (Materials.Build has not run)");
            var v = new List<string>();
            foreach (var r in records)
            {
                var ab = MaterialCatalog.Materials.SelectMany(m => m.SpecialAbilities).Where(a => a.Guid == r.Src.AssetGuid.ToString()).ToArray();
                if (ab.Length == 0) { v.Add(r.Name + ": source " + r.Src.name + " is not a catalog special ability"); continue; }
                v.AddRange(Violations(r.Clone, r.Src, ab[0].SharedComponentKinds, identifyForeign, foreignNotes));
            }
            return v.ToArray();
        }

        // ---- 공유 컴포넌트·중첩 원소의 소유자 수집(교체 색인 제외) ----
        static List<Node> OwnerNodes(BlueprintComponent[] comps, int skip)
        {
            var l = new List<Node>();
            var seen = new HashSet<object>(new RefEq());
            for (int i = 0; i < comps.Length; i++)
            {
                if (i == skip) continue;
                var c = comps[i];
                if (c == null) throw new InvalidOperationException("Golemancer: null component at " + i);
                l.Add(new Node("component " + i + " " + c.GetType().Name, c, c.OwnerBlueprint));
                Walk(c, "component " + i, l, seen, 0);
            }
            return l;
        }

        // 원소(Element) 트리: 필드를 따라 내려가며 Element 를 만날 때마다 (원소, Owner) 를 기록한다. 블루프린트 참조·Unity 객체·문자열·값 형식은 내려가지 않는다.
        // 출처: 정찰 §20 — 원소 Owner = Element.<Owner>k__BackingField(set_Owner 는 블루프린트 OnEnable 경로만). 순환은 seen(참조 동일성)이 막는다.
        static void Walk(object o, string path, List<Node> l, HashSet<object> seen, int depth)
        {
            foreach (var f in AllFields(o.GetType()))
            {
                var v = f.GetValue(o);
                if (v == null || v is string || v is UnityEngine.Object || v is BlueprintReferenceBase || v is SimpleBlueprint || v is Type || v is Delegate || v is Kingmaker.EntitySystem.EntityDataBase) continue;
                var vt = v.GetType();
                if (vt.IsPrimitive || vt.IsEnum || vt.IsValueType) continue;
                if (v is IEnumerable && !(v is Element))
                {
                    int k = 0;
                    foreach (var e in (IEnumerable)v)
                    {
                        if (e != null && !(e is string) && !e.GetType().IsValueType) Visit(e, path + "." + f.Name + "[" + k + "]", l, seen, depth + 1);
                        k++;
                    }
                    continue;
                }
                Visit(v, path + "." + f.Name, l, seen, depth + 1);
            }
        }

        static void Visit(object v, string path, List<Node> l, HashSet<object> seen, int depth)
        {
            // 런타임 개체(EntityDataBase — 예: AbstractWeaponTrigger.m_OverrideTargetData)는 블루프린트 데이터 그래프가 아니다 → 내려가지 않는다.
            if (v is UnityEngine.Object || v is BlueprintReferenceBase || v is SimpleBlueprint || v is Type || v is Delegate || v is BlueprintComponent || v is Kingmaker.EntitySystem.EntityDataBase) return;
            if (!seen.Add(v)) return;
            var e = v as Element;
            if (e != null) l.Add(new Node(path + " " + e.GetType().Name, e, e.Owner));
            Walk(v, path, l, seen, depth);
        }

        // 컴포넌트 아래 원소 전부(읽기 전용 수집 — Materials 가 본편 StoneGolemSlow 의 행동 값을 읽을 때 쓴다).
        internal static List<Element> ElementsUnder(BlueprintComponent c)
        {
            if (c == null) throw new ArgumentNullException("c");
            var l = new List<Node>();
            Walk(c, c.GetType().Name, l, new HashSet<object>(new RefEq()), 0);
            return l.Select(n => (Element)n.Ref).ToList();
        }

        static IEnumerable<FieldInfo> AllFields(Type t)
        {
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                foreach (var f in cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    yield return f;
        }

        sealed class RefEq : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
            public int GetHashCode(object o) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o); }
        }
    }
}
