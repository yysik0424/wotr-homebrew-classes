using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;

namespace PackWarden
{
    public static class BlueprintUtil
    {
        static FieldInfo loadedField;

        // 캐시에 이미 올라간(= 로드됐거나 추가된) 블루프린트인지. Load 는 번들에서 읽어오므로 중복 가드에는 쓰지 않는다.
        static bool IsCached(BlueprintGuid guid)
        {
            if (loadedField == null)
            {
                loadedField = typeof(BlueprintsCache).GetField("m_LoadedBlueprints", BindingFlags.NonPublic | BindingFlags.Instance);
                if (loadedField == null) throw new MissingFieldException("PackWarden: BlueprintsCache.m_LoadedBlueprints not found");
            }
            var dict = loadedField.GetValue(ResourcesLibrary.BlueprintsCache) as IDictionary;
            if (dict == null) throw new InvalidOperationException("PackWarden: BlueprintsCache.m_LoadedBlueprints is not an IDictionary");
            return dict.Contains(guid);
        }

        public static T Create<T>(string name, string guid, Action<T> init) where T : SimpleBlueprint, new()
        {
            var g = BlueprintGuid.Parse(guid);
            if (IsCached(g)) throw new InvalidOperationException("PackWarden: blueprint already exists in cache: " + name + " " + guid);
            var bp = new T { AssetGuid = g, name = name };
            if (init != null) init(bp);
            // 번들에서 읽은 블루프린트는 BlueprintsCache.Load 가 OnEnable 을 부른다(dnlib 2026-11-07). 우리 것은 Load 를 거치지 않으므로 직접 부른다.
            // BlueprintScriptableObject.OnEnable = 컴포넌트마다 OwnerBlueprint=this + Element.Owner 설정 + OnEnableWithLibrary(빈 메서드).
            // 우리가 쓰는 형식(Feature·Selection·Archetype·Buff·ActivatableAbility·SummonPool)에는 재정의가 없다(dnlib 전수 검색 2026-11-07).
            bp.OnEnable();
            ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(g, bp);
            ownGuids.Add(g);
            ownBlueprints.Add(bp);
            return bp;
        }

        // Create 로 만든 블루프린트(만든 순서). RequireOwnLocalizedStrings 가 돈다.
        static readonly System.Collections.Generic.List<SimpleBlueprint> ownBlueprints = new System.Collections.Generic.List<SimpleBlueprint>();

        // ---- LocalizedString 필드는 null 이면 안 된다 (2026-11-16 결함: 블라스트 메이거스 「폭발」 툴팁의 지속 시간이 `<null>`; 2026-11-17 다른 직업들에서도 발견) ----
        // 엔진 근거(Assembly-CSharp.dll dnlib 직독 2026-11-16, 공용 src\Common\BlueprintUtil.cs 의 같은 주석을 옮김):
        //   - LocalizedString.op_Implicit @0x000C~0x000F: 인스턴스가 null 이면 문자열 "<null>" 을 돌려준다(예외 아님 → 화면에 그대로 찍힌다).
        //   - LocalizedString..ctor @0x0001~0x0006: m_Key = "". LoadString @0x0003~0x001C: 실제 키가 "" 이면 "" 를 돌려준다(팩 조회 없음).
        //   - 툴팁(TooltipTemplateAbility.Prepare @0x0168·@0x0183 → AddDuration @0x0001~0x000D, AddSavingThrow → AddTitleDescriptionBlock @0x0000~0x0008)은
        //     string.IsNullOrEmpty 인 칸만 숨긴다. 그래서 null 참조 = "<null>" 표시, 빈 키 인스턴스 = 칸 숨김.
        //   - 본편 jbp 는 문구 없는 칸을 { "m_Key": "", "Shared": null } 로 적는다(JSON 로드가 인스턴스를 만든다). 코드로 만든 블루프린트는 생성자가
        //     LocalizedString 필드를 채우지 않는다(예: BlueprintAbility..ctor 에 LocalizedDuration·LocalizedSavingThrow 대입 없음) → 직접 넣어야 한다.

        // 「문구 없음」(본편 jbp 의 빈 키와 같은 것). 칸을 일부러 비울 때 명시적으로 부른다 — null 로 두는 것과 다르다.
        public static Kingmaker.Localization.LocalizedString NoText()
        {
            var s = new Kingmaker.Localization.LocalizedString();
            if (s.Key != "") throw new InvalidOperationException("PackWarden: new LocalizedString().Key is '" + s.Key + "', expected the empty key (engine ctor changed?)");
            return s;
        }

        // 블루프린트 형식(기반 형식 포함)에 선언된 LocalizedString 인스턴스 필드 중 null 인 것이 있으면 던진다. 형식으로 찾으므로 이름 원장이 필요 없고,
        // 새 블루프린트 형식을 만들어도 그 형식의 문자열 필드가 자동으로 대상이 된다. 컴포넌트·액션 안의 LocalizedString 은 보지 않는다(블루프린트 자신의 필드만).
        public static void RequireLocalizedStrings(SimpleBlueprint bp)
        {
            if (bp == null) throw new ArgumentNullException("bp");
            var missing = new System.Collections.Generic.List<string>();
            for (var t = bp.GetType(); t != null; t = t.BaseType)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (f.FieldType == typeof(Kingmaker.Localization.LocalizedString) && f.GetValue(bp) == null)
                        missing.Add(t.Name + "." + f.Name);
            if (missing.Count > 0)
                throw new InvalidOperationException("PackWarden: blueprint " + bp.name + " " + bp.AssetGuid + " has null LocalizedString field(s) [" + string.Join(", ", missing.ToArray())
                    + "] (the UI would print \"<null>\"; set a string, or BlueprintUtil.NoText() for an intentionally empty one)");
        }

        // Create 로 만든 모든 블루프린트에 RequireLocalizedStrings. BuildAll 이 끝에서(아키타입 등록 전에) 부른다. 돌려주는 값 = 검사한 개수.
        // 하나도 없으면 던진다(빌드 전에 불린 배선 결함 — 「검사할 것이 없어서 통과」를 만들지 않는다).
        // 얕은 복제(CloneFeature·CloneUnit)는 Create 를 거치지 않으므로 여기 없다 — 그 둘은 만들 때 RequireLocalizedStrings 로 바로 검사한다.
        public static int RequireOwnLocalizedStrings()
        {
            if (ownBlueprints.Count == 0) throw new InvalidOperationException("PackWarden: RequireOwnLocalizedStrings before any BlueprintUtil.Create (nothing to check)");
            foreach (var bp in ownBlueprints) RequireLocalizedStrings(bp);
            return ownBlueprints.Count;
        }

        // ---- 버프의 FxOnRemove 는 null 이면 안 된다 (2026-11-17, 다른 직업 모드에서 확인된 결함 — 우리 버프를 지울 때마다 Buff.OnRemove NRE) ----
        // 엔진 근거(Assembly-CSharp.dll dnlib 직독 2026-11-17): Buff.TrySpawnParticleEffect @00AE~00BF 가 화면 있는 유닛이면 m_ParticleEffectOwner 를 채우고,
        // Buff.OnRemove @0095~00B7 이 그때 Blueprint.FxOnRemove.Load 를 null 검사 없이 부른다 → null 이면 NRE(@010E~012E IBuffRemoved 처리 생략).
        // FxOnStart 는 @0093 dup/brtrue 로 null 검사, ResourceAssetIds 는 ResourcesPreload.PreloadResources @001A EmptyIfNull. 코드로 만든 버프는 생성자가 이 칸을 채우지 않는다.
        // 이 모드의 버프 자리(Aspects.Buff · PackCall.Build)는 빈 링크를 넣는다(시험 `buff fx:` 가 자리 전수를 잠근다). 이 함수는 빌드 끝 전수 되읽기:
        // RequireOwnLocalizedStrings 와 같은 자리(등록 전)에서 부른다. 하나라도 null 이면 이름 전부로 던진다. Create 가 없었으면 던진다.
        // requireAnyBuff = 이 모드가 빌드에서 버프를 만든다(0 개면 던진다 — 「검사할 것이 없어서 통과」 금지). 돌려주는 값 = 검사한 버프 수.
        public static int RequireOwnBuffFx(bool requireAnyBuff)
        {
            if (ownBlueprints.Count == 0) throw new InvalidOperationException("PackWarden: RequireOwnBuffFx before any BlueprintUtil.Create (nothing to check)");
            var buffs = ownBlueprints.OfType<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>().ToList();
            if (requireAnyBuff && buffs.Count == 0) throw new InvalidOperationException("PackWarden: RequireOwnBuffFx found no buffs we created, but this mod builds buffs (called before the buff builders?)");
            var bad = buffs.Where(b => b.FxOnRemove == null).Select(b => b.name + " " + b.AssetGuid).ToList();
            if (bad.Count != 0) throw new InvalidOperationException("PackWarden: buff(s) with a null FxOnRemove (engine Buff.OnRemove @00B0 would throw on removal): " + string.Join(", ", bad.ToArray()));
            return buffs.Count;
        }

        // Create 로 만든 블루프린트 GUID. AddComponents 는 이 집합 밖(본편·복제 유닛)의 블루프린트를 거부한다:
        // 복제 유닛의 컴포넌트는 원본과 공유 인스턴스라 OnEnable 로 OwnerBlueprint 를 바꾸면 본편 객체가 바뀐다.
        static readonly System.Collections.Generic.HashSet<BlueprintGuid> ownGuids = new System.Collections.Generic.HashSet<BlueprintGuid>();




        public static TRef Ref<TRef>(string guid) where TRef : BlueprintReferenceBase, new()
        {
            // deserializedGuid 는 protected 라 직접 못 쓴다. 공개 메서드 ReadGuidFromGuid 가 그 필드를 채운다.
            var r = new TRef();
            r.ReadGuidFromGuid(BlueprintGuid.Parse(guid));
            return r;
        }

        // 블루프린트가 캐시에 없음(Get 의 결측). InvalidOperationException 의 하위 형식이라 기존 호출부의 동작은 같다 — 후보 조회(PackEmpower.LookupAbility)만 이 형식으로 외부 데이터 거절을 가른다.
        public sealed class BlueprintNotFoundException : InvalidOperationException
        {
            public BlueprintNotFoundException(string message) : base(message) { }
        }

        public static T Get<T>(string guid) where T : SimpleBlueprint
        {
            var raw = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(guid));
            if (raw == null) throw new BlueprintNotFoundException("PackWarden: blueprint not found " + guid + " (expected " + typeof(T).Name + ")");
            var typed = raw as T;
            if (typed == null) throw new InvalidCastException("PackWarden: blueprint " + guid + " is " + raw.GetType().Name + ", expected " + typeof(T).Name);
            return typed;
        }

        public static void AddComponents(BlueprintScriptableObject bp, params BlueprintComponent[] cs)
        {
            if (bp == null) throw new ArgumentNullException("bp");
            if (!ownGuids.Contains(bp.AssetGuid)) throw new InvalidOperationException("PackWarden: AddComponents on a blueprint not made by BlueprintUtil.Create: " + bp.name + " " + bp.AssetGuid);
            var old = bp.ComponentsArray ?? new BlueprintComponent[0];
            int start = old.Length;
            for (int i = 0; i < cs.Length; i++)
            {
                if (cs[i] == null) throw new ArgumentNullException("cs[" + i + "]");
                if (old.Contains(cs[i])) throw new InvalidOperationException("PackWarden: component instance already on " + bp.name);
                cs[i].name = "$" + cs[i].GetType().Name + "$" + bp.AssetGuid + "#" + (start + i);
            }
            bp.ComponentsArray = old.Concat(cs).ToArray();
            bp.OnEnable(); // 새 컴포넌트에도 OwnerBlueprint 를 단다(Create 주석 참조). 대상 배열의 인스턴스는 전부 우리 것이다.
            foreach (var c in cs)
                if (!ReferenceEquals(c.OwnerBlueprint, bp)) throw new InvalidOperationException("PackWarden: OwnerBlueprint not set on " + c.name);
        }

        // 비공개 직렬화 필드 조회: 파생 형식에서 시작해 기반 형식으로 올라간다. .NET GetField 는 기반 형식의 private 필드를
        // 돌려주지 않는다(2026-11-07: m_DisplayName 은 BlueprintUnitFact 의 private). 없으면 null — 호출자가 실패시킨다.
        internal static FieldInfo FindField(Type t, string name)
        {
            if (t == null) throw new ArgumentNullException("t");
            for (var cur = t; cur != null; cur = cur.BaseType)
            {
                var f = cur.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            return null;
        }

        // 비공개 직렬화 필드 쓰기. 필드가 없으면(게임 버전 차이) 조용히 넘어가지 않고 실패.
        public static void SetField(object o, string name, object value)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("PackWarden: " + o.GetType().Name + "." + name + " not found");
            f.SetValue(o, value);
        }

        static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo AddFactsField = typeof(BlueprintUnit).GetField("m_AddFacts", BindingFlags.NonPublic | BindingFlags.Instance);

        // 특성 얕은 복제(2026-11-16, 보조 성장 사본): 컴포넌트 배열만 새 배열(keep 이 참인 원본 인스턴스, 같은 순서). OnEnable 을 부르지 않는다 —
        // 공유 컴포넌트의 OwnerBlueprint 가 본편 특성에서 바뀌면 안 된다(CloneUnit 과 같은 규칙). ownGuids 에 넣지 않으므로 AddComponents 가 이 사본을 거부한다.
        public static Kingmaker.Blueprints.Classes.BlueprintFeature CloneFeature(Kingmaker.Blueprints.Classes.BlueprintFeature src, string name, string guid, Func<BlueprintComponent, bool> keep)
        {
            if (src == null) throw new ArgumentNullException("src");
            if (keep == null) throw new ArgumentNullException("keep");
            if (MemberwiseCloneMethod == null) throw new MissingMethodException("PackWarden: object.MemberwiseClone not found");
            var g = BlueprintGuid.Parse(guid);
            if (IsCached(g)) throw new InvalidOperationException("PackWarden: blueprint already exists in cache: " + name + " " + guid);
            var clone = (Kingmaker.Blueprints.Classes.BlueprintFeature)MemberwiseCloneMethod.Invoke(src, null);
            clone.AssetGuid = g;
            clone.name = name;
            clone.ComponentsArray = (src.ComponentsArray ?? new BlueprintComponent[0]).Where(keep).ToArray();
            RequireLocalizedStrings(clone); // 2026-11-17: 문자열 칸은 원본 인스턴스를 공유한다 — 원본이 null 이면 사본도 null(캐시에 올리기 전에 던진다)
            ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(g, clone);
            return clone;
        }

        public static BlueprintUnit CloneUnit(BlueprintUnit src, string name, string guid)
        {
            if (src == null) throw new ArgumentNullException("src");
            if (MemberwiseCloneMethod == null) throw new MissingMethodException("PackWarden: object.MemberwiseClone not found");
            if (AddFactsField == null) throw new MissingFieldException("PackWarden: BlueprintUnit.m_AddFacts not found");
            var g = BlueprintGuid.Parse(guid);
            if (IsCached(g)) throw new InvalidOperationException("PackWarden: blueprint already exists in cache: " + name + " " + guid);
            var clone = (BlueprintUnit)MemberwiseCloneMethod.Invoke(src, null);
            clone.AssetGuid = g;
            clone.name = name;
            clone.ComponentsArray = (src.ComponentsArray ?? new BlueprintComponent[0]).ToArray();
            var facts = (BlueprintUnitFactReference[])AddFactsField.GetValue(src) ?? new BlueprintUnitFactReference[0];
            AddFactsField.SetValue(clone, facts.ToArray());
            RequireLocalizedStrings(clone); // 2026-11-17: CloneFeature 와 같은 이유(문자열 칸 공유)
            ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(g, clone);
            return clone;
        }
    }
}
