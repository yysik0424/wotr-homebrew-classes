using System;
using System.Linq;
using Kingmaker.Blueprints;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
    // [게임] ForeignComponents.Strip 에 넣을 컴포넌트 모습을 블루프린트에서 읽는다(규칙이 있는 형식만 그 규칙의 참조 필드를 읽는다).
    //  null 배열은 빈 배열로 보지 않고 throw(C1). null 컴포넌트는 TypeFullName = null 로 넘겨 Strip 이 던진다.
    //  규칙 필드가 형식에 없거나 블루프린트 참조가 아니면 throw — 다른 모드 판이 바뀐 것이므로 조용히 「참조 없음」으로 두지 않는다.
    internal static class ForeignComponentViews
    {
        internal static ComponentView[] Of(BlueprintScriptableObject bp)
        {
            if (bp == null) throw new ArgumentNullException("bp");
            var comps = bp.ComponentsArray;
            if (comps == null) throw new InvalidOperationException("WeaponSpiritShaman: " + bp.name + " ComponentsArray is null");
            string owner = bp.AssetGuid.ToString();
            return comps.Select(c =>
            {
                if (c == null) return new ComponentView(null, null, null);
                var t = c.GetType();
                string field = ForeignComponents.RefFieldFor(owner, t.FullName);
                if (field == null) return new ComponentView(t.FullName, t.Name, null);
                var f = FindField(t, field);
                if (f == null) throw new MissingFieldException("WeaponSpiritShaman: " + t.FullName + "." + field + " not found (foreign component rule field; the other mod changed)");
                var v = f.GetValue(c);
                if (v == null) return new ComponentView(t.FullName, t.Name, null);
                var r = v as BlueprintReferenceBase;
                if (r == null) throw new InvalidOperationException("WeaponSpiritShaman: " + t.FullName + "." + field + " is a " + v.GetType().Name + ", not a blueprint reference");
                return new ComponentView(t.FullName, t.Name, r.Guid == BlueprintGuid.Empty ? null : r.Guid.ToString());
            }).ToArray();
        }
    }
}
