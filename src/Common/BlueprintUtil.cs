using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;

namespace WotrClassMod.Common
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
                if (loadedField == null) throw new MissingFieldException(ModContext.Id + ": BlueprintsCache.m_LoadedBlueprints not found");
            }
            var dict = loadedField.GetValue(ResourcesLibrary.BlueprintsCache) as IDictionary;
            if (dict == null) throw new InvalidOperationException(ModContext.Id + ": BlueprintsCache.m_LoadedBlueprints is not an IDictionary");
            return dict.Contains(guid);
        }

        public static T Create<T>(string name, string guid, Action<T> init) where T : SimpleBlueprint, new()
        {
            var g = BlueprintGuid.Parse(guid);
            if (IsCached(g)) throw new InvalidOperationException(ModContext.Id + ": blueprint already exists in cache: " + name + " " + guid);
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

        // Create 로 만든 블루프린트(만든 순서). 모드가 빌드 끝에 「우리가 만든 것 전부」를 검사할 때 쓴다(예: 아이콘 빠진 보이는 블루프린트를 빌드 실패로).
        static readonly System.Collections.Generic.List<SimpleBlueprint> ownBlueprints = new System.Collections.Generic.List<SimpleBlueprint>();
        public static System.Collections.Generic.IReadOnlyList<SimpleBlueprint> OwnBlueprints { get { return ownBlueprints; } }

        // ---- LocalizedString 필드는 null 이면 안 된다 (2026-11-16 결함: 블라스트 메이거스 「폭발」 툴팁의 지속 시간이 `<null>`) ----
        // 엔진 근거(Assembly-CSharp.dll dnlib 직독 2026-11-16):
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
            if (s.Key != "") throw new InvalidOperationException(ModContext.Id + ": new LocalizedString().Key is '" + s.Key + "', expected the empty key (engine ctor changed?)");
            return s;
        }

        // 본편 문자열 키(로컬라이즈 팩의 키 = 소문자 8-4-4-4-12 GUID)를 그대로 가리키는 LocalizedString. 본편 공용 문구(SharedStringAsset)의 stringkey 를 넣으면
        // 같은 문구가 나온다(LocalizedString.GetActualKey @0x0007~0x0045: Shared 가 있으면 그 String.m_Key, 없으면 자기 m_Key — 팩 조회 키는 같다).
        static readonly System.Text.RegularExpressions.Regex GameTextKey = new System.Text.RegularExpressions.Regex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");
        public static Kingmaker.Localization.LocalizedString GameText(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!GameTextKey.IsMatch(key)) throw new ArgumentException(ModContext.Id + ": GameText key '" + key + "' is not a localization pack key (lowercase 8-4-4-4-12 GUID)", "key");
            var s = new Kingmaker.Localization.LocalizedString { Key = key };
            if (s.Key != key) throw new InvalidOperationException(ModContext.Id + ": LocalizedString.Key readback failed for " + key);
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
                throw new InvalidOperationException(ModContext.Id + ": blueprint " + bp.name + " " + bp.AssetGuid + " has null LocalizedString field(s) [" + string.Join(", ", missing.ToArray())
                    + "] (the UI would print \"<null>\"; set a string, or BlueprintUtil.NoText() for an intentionally empty one)");
        }

        // Create 로 만든 모든 블루프린트에 RequireLocalizedStrings. 모드의 BuildAll 이 끝에서(아키타입 등록 전에) 부른다. 돌려주는 값 = 검사한 개수.
        // 하나도 없으면 던진다(빌드 전에 불린 배선 결함 — 「검사할 것이 없어서 통과」를 만들지 않는다).
        public static int RequireOwnLocalizedStrings()
        {
            if (ownBlueprints.Count == 0) throw new InvalidOperationException(ModContext.Id + ": RequireOwnLocalizedStrings before any BlueprintUtil.Create (nothing to check)");
            foreach (var bp in ownBlueprints) RequireLocalizedStrings(bp);
            return ownBlueprints.Count;
        }

        // ---- 버프의 FxOnRemove 는 null 이면 안 된다 (2026-11-17 결함: 우리 버프를 지울 때마다 Buff.OnRemove NRE) ----
        // 엔진 근거(Assembly-CSharp.dll dnlib 직독 2026-11-17):
        //   - Buff.TrySpawnParticleEffect @00AE~00BF: 유닛에 화면(View)이 있으면 FxOnStart 유무와 무관하게 m_ParticleEffectOwner 를 채운다(FxOnStart 는 @0093 dup/brtrue 로 null 검사).
        //   - Buff.OnRemove @0095~00B7: m_ParticleEffectOwner 가 있고 IsEnabled 이면 Blueprint.FxOnRemove.Load(false,false) 를 null 검사 없이 부른다 → null 이면 NRE.
        //     그 NRE 로 @010E~012E CallComponents<IBuffRemoved>(우리 부품의 「버프 제거」 처리)와 그 뒤 제거 사건이 생략된다.
        //   - BlueprintBuff 가 선언한 참조 칸 중 엔진이 null 검사 없이 읽는 것은 FxOnRemove 하나다(FxOnStart = 검사함, ResourceAssetIds = ResourcesPreload.PreloadResources @001A EmptyIfNull).
        //   - 본편 jbp 의 빈 FxOnRemove 는 읽을 때 빈 링크가 된다. 코드로 만든 버프는 생성자가 이 칸을 채우지 않는다 → 직접 넣어야 한다.
        // 버프를 만들거나 복제한 자리마다 부른다: null 이면 빈 링크(원본을 복사한 버프는 원본 값 그대로 — null 일 때만 채운다). 돌려주는 값 = 같은 버프.
        public static Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff EmptyFxOnRemove(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff b)
        {
            if (b == null) throw new ArgumentNullException("b");
            if (b.FxOnRemove == null) b.FxOnRemove = new Kingmaker.ResourceLinks.PrefabLink();
            return b;
        }

        // Create 로 만든 모든 버프의 FxOnRemove 가 null 이 아니어야 한다(위 엔진 근거). RequireOwnLocalizedStrings 와 같은 자리(빌드 끝, 아키타입 등록 전)에서 부른다.
        // 하나라도 null 이면 던진다(이름 전부). Create 가 하나도 없었으면 던진다(빌드 전에 불린 배선 결함). requireAnyBuff = 이 모드가 빌드에서 버프를 만든다(0 개면 던진다 — 「검사할 것이 없어서 통과」 금지).
        // 돌려주는 값 = 검사한 버프 수.
        public static int RequireOwnBuffFx(bool requireAnyBuff)
        {
            if (ownBlueprints.Count == 0) throw new InvalidOperationException(ModContext.Id + ": RequireOwnBuffFx before any BlueprintUtil.Create (nothing to check)");
            var buffs = ownBlueprints.OfType<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>().ToList();
            if (requireAnyBuff && buffs.Count == 0) throw new InvalidOperationException(ModContext.Id + ": RequireOwnBuffFx found no buffs we created, but this mod builds buffs (called before the buff builders?)");
            var bad = buffs.Where(b => b.FxOnRemove == null).Select(b => b.name + " " + b.AssetGuid).ToList();
            if (bad.Count != 0) throw new InvalidOperationException(ModContext.Id + ": buff(s) with a null FxOnRemove (engine Buff.OnRemove @00B0 would throw on removal; use BlueprintUtil.EmptyFxOnRemove): " + string.Join(", ", bad.ToArray()));
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

        public static T Get<T>(string guid) where T : SimpleBlueprint
        {
            var raw = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(guid));
            if (raw == null) throw new InvalidOperationException(ModContext.Id + ": blueprint not found " + guid + " (expected " + typeof(T).Name + ")");
            var typed = raw as T;
            if (typed == null) throw new InvalidCastException(ModContext.Id + ": blueprint " + guid + " is " + raw.GetType().Name + ", expected " + typeof(T).Name);
            return typed;
        }

        public static void AddComponents(BlueprintScriptableObject bp, params BlueprintComponent[] cs)
        {
            if (bp == null) throw new ArgumentNullException("bp");
            if (!ownGuids.Contains(bp.AssetGuid)) throw new InvalidOperationException(ModContext.Id + ": AddComponents on a blueprint not made by BlueprintUtil.Create: " + bp.name + " " + bp.AssetGuid);
            var old = bp.ComponentsArray ?? new BlueprintComponent[0];
            int start = old.Length;
            for (int i = 0; i < cs.Length; i++)
            {
                if (cs[i] == null) throw new ArgumentNullException("cs[" + i + "]");
                if (old.Contains(cs[i])) throw new InvalidOperationException(ModContext.Id + ": component instance already on " + bp.name);
                cs[i].name = "$" + cs[i].GetType().Name + "$" + bp.AssetGuid + "#" + (start + i);
            }
            bp.ComponentsArray = old.Concat(cs).ToArray();
            bp.OnEnable(); // 새 컴포넌트에도 OwnerBlueprint 를 단다(Create 주석 참조). 대상 배열의 인스턴스는 전부 우리 것이다.
            foreach (var c in cs)
                if (!ReferenceEquals(c.OwnerBlueprint, bp)) throw new InvalidOperationException(ModContext.Id + ": OwnerBlueprint not set on " + c.name);
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
            if (f == null) throw new MissingFieldException(ModContext.Id + ": " + o.GetType().Name + "." + name + " not found");
            f.SetValue(o, value);
        }

        static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo AddFactsField = typeof(BlueprintUnit).GetField("m_AddFacts", BindingFlags.NonPublic | BindingFlags.Instance);

        public static BlueprintUnit CloneUnit(BlueprintUnit src, string name, string guid)
        {
            if (src == null) throw new ArgumentNullException("src");
            if (MemberwiseCloneMethod == null) throw new MissingMethodException(ModContext.Id + ": object.MemberwiseClone not found");
            if (AddFactsField == null) throw new MissingFieldException(ModContext.Id + ": BlueprintUnit.m_AddFacts not found");
            var g = BlueprintGuid.Parse(guid);
            if (IsCached(g)) throw new InvalidOperationException(ModContext.Id + ": blueprint already exists in cache: " + name + " " + guid);
            var clone = (BlueprintUnit)MemberwiseCloneMethod.Invoke(src, null);
            clone.AssetGuid = g;
            clone.name = name;
            clone.ComponentsArray = (src.ComponentsArray ?? new BlueprintComponent[0]).ToArray();
            var facts = (BlueprintUnitFactReference[])AddFactsField.GetValue(src) ?? new BlueprintUnitFactReference[0];
            AddFactsField.SetValue(clone, facts.ToArray());
            ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(g, clone);
            return clone;
        }
    }
}
