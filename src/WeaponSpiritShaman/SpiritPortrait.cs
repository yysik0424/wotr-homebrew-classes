using System;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
    // 스피릿 웨폰 초상화(2026-11-17 결함 수리: 파티 바의 펫 띠 초상화가 흰색).
    // 엔진 경로(dnlib 2026-11-17): 펫 띠 = PartyCharacterPetVM.<.ctor>b__9_0 @0052 UnitEntityData.get_Portrait → @0057 PortraitData.get_PetEyePortrait
    //   (= m_PetEyeImage, null 이면 @000A null) → PartyCharacterPetPCView.<BindViewImplementation>b__10_3 @0007 Image.set_sprite(null) → 흰 사각형.
    //   유닛 초상화 = UnitUISettings.GetOriginalPortraitData_Internal @002A GetOriginalBlueprintPortraitForCharacter(m_Portrait ?? @0040 Blueprint.PortraitSafe) → @0035 .Data.
    //   영병 유닛의 m_Portrait 는 본편 AnimatedWeapons(264ed688)였고, 그 jbp 에는 m_PetEyeImage 가 없다(본편 동물 동료 초상화 Pet/Wolf_Portrait 등은 있다).
    // 수리: 본편 블루프린트는 건드리지 않고(읽기만) 새 BlueprintPortrait 를 만든다 = 원본 그림 3장(같은 AssetId 의 새 SpriteLink) + m_PetEyeImage = SpiritRules.SpiritPetEyeSpriteAssetId.
    // 그림 출처만 바뀌는 표시 수리다 — 전투 규칙 값은 건드리지 않는다. 원본이 없거나 모양이 다르면 던진다(→ BuildFailed, SpiritBlueprints 의 다른 원본 검사와 같은 등급).
    public static class SpiritPortrait
    {
        public static BlueprintPortrait Portrait { get; private set; }

        // SpiritBlueprints.Build 가 유닛을 만들기 전에 한 번 부른다(유닛 m_Portrait = Guids.SpiritPortrait 되읽기에 이 블루프린트가 필요).
        public static BlueprintPortrait Build()
        {
            if (Portrait != null) throw new InvalidOperationException("WeaponSpiritShaman: SpiritPortrait.Build() called twice");
            string eye = SpiritRules.SpiritPetEyeSpriteAssetId;
            if (!IsHex32(eye)) throw new InvalidOperationException("WeaponSpiritShaman: SpiritRules.SpiritPetEyeSpriteAssetId '" + eye + "' is not a 32-hex asset id");
            var src = Get<BlueprintPortrait>(SpiritRules.SpiritPortraitBaseGuid);
            var sd = src.Data;
            if (sd == null) throw new InvalidOperationException("WeaponSpiritShaman: portrait source " + src.name + " has no PortraitData");
            if (sd.IsCustom) throw new InvalidOperationException("WeaponSpiritShaman: portrait source " + src.name + " is a custom (file) portrait");
            string small = AssetOf(sd, "m_PortraitImage"), half = AssetOf(sd, "m_HalfLengthImage"), full = AssetOf(sd, "m_FullLengthImage"), srcEye = AssetOf(sd, "m_PetEyeImage");
            // 작은·반신 그림은 엔진이 필수로 표시한다(ValidateNotNull) — 없으면 고칠 원본이 아니다.
            if (small == null || half == null) throw new InvalidOperationException("WeaponSpiritShaman: portrait source " + src.name + " has no small/half image (small " + (small ?? "null") + ", half " + (half ?? "null") + ")");
            // 수리의 전제(원본에 펫 띠 그림 없음)가 바뀌었으면 알린다 — 그래도 다이얼 그림을 쓴다(다이얼 = 단일 출처).
            if (srcEye != null) ModContext.Log("WSS portrait: source " + src.name + " now has its own pet-eye image " + srcEye + "; using the dial " + eye + " anyway");

            var p = Create<BlueprintPortrait>("WeaponSpiritShamanPortrait", Guids.SpiritPortrait, x =>
            {
                var d = new PortraitData();
                SetField(d, "m_PortraitImage", Link(small));
                SetField(d, "m_HalfLengthImage", Link(half));
                SetField(d, "m_FullLengthImage", full == null ? null : Link(full));
                SetField(d, "m_PetEyeImage", Link(eye));
                d.PortraitCategory = sd.PortraitCategory;
                d.IsDefault = sd.IsDefault;
                d.InitiativePortrait = sd.InitiativePortrait;
                x.Data = d;
            });
            // 되읽기(엔진이 읽는 필드): 새 PortraitData · 그림 4개 · 본편 원본 불변.
            var pd = p.Data;
            if (pd == null || ReferenceEquals(pd, sd) || pd.IsCustom) throw new InvalidOperationException("WeaponSpiritShaman: portrait " + p.name + " data readback failed");
            if (AssetOf(pd, "m_PortraitImage") != small || AssetOf(pd, "m_HalfLengthImage") != half || AssetOf(pd, "m_FullLengthImage") != full || AssetOf(pd, "m_PetEyeImage") != eye)
                throw new InvalidOperationException("WeaponSpiritShaman: portrait " + p.name + " sprite links readback failed");
            if (ReferenceEquals(GetF(pd, "m_PortraitImage"), GetF(sd, "m_PortraitImage")) || AssetOf(sd, "m_PetEyeImage") != srcEye || AssetOf(sd, "m_PortraitImage") != small)
                throw new InvalidOperationException("WeaponSpiritShaman: portrait " + p.name + " shares or changed the vanilla source " + src.name);
            Portrait = p;
            ModContext.Log("WSS portrait built: " + p.name + " " + Guids.SpiritPortrait + " = " + src.name + " images (small " + small + ") + pet-eye " + eye);
            return p;
        }

        static SpriteLink Link(string assetId) { return new SpriteLink { AssetId = assetId }; }

        // 초상화의 펫 띠 그림 AssetId(없으면 null). SpiritBlueprints.BuildUnit 의 유닛 되읽기·점검이 쓴다.
        internal static string PetEyeAssetOf(BlueprintPortrait p)
        {
            if (p == null) throw new ArgumentNullException("p");
            if (p.Data == null) throw new InvalidOperationException("WeaponSpiritShaman: portrait " + p.name + " has no PortraitData");
            return AssetOf(p.Data, "m_PetEyeImage");
        }

        // SpriteLink 의 AssetId(없거나 빈 값 = null). 필드가 SpriteLink 가 아니면 던진다.
        static string AssetOf(PortraitData d, string field)
        {
            var v = GetF(d, field);
            if (v == null) return null;
            var l = v as SpriteLink;
            if (l == null) throw new InvalidOperationException("WeaponSpiritShaman: PortraitData." + field + " is " + v.GetType().Name + ", not SpriteLink");
            return string.IsNullOrEmpty(l.AssetId) ? null : l.AssetId;
        }

        static bool IsHex32(string g)
        {
            if (g == null || g.Length != 32) return false;
            foreach (var c in g) if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')) return false;
            return true;
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("WeaponSpiritShaman: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
