using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
    // [순수] BlueprintClone.Clone 의 공유 검사 예외(게임 형식 없음 — modinfo testSources, 시험 exe 가 일반 객체로 실행).
    // 복제본은 BlueprintUtil.Create/AddComponents 안에서 bp.OnEnable() 을 거친다(src\Common\BlueprintUtil.cs:36,76).
    // OnEnable/OnEnableWithLibrary 재정의가 필드를 쓰면 CopyOf 로 떼어 낸 하위 객체가 다시 원본과 같은 인스턴스로 묶일 수 있다.
    // 시험 `copy: clone OnEnable writes` 가 Assembly-CSharp IL 에서 Clone<T> 의 T(모드 dll 에서 수집)마다 기반 형식 사슬의 쓰기를 모아 이 목록과 양방향 대조한다.
    public static class ClonePolicy
    {
        // 「선언 형식::메서드|쓰는 대상(필드 또는 setter)」 → 왜 공유가 생기지 않는가(또는 어느 허용 경로로 다루는가).
        public static readonly Tuple<string, string>[] OnEnableWrites =
        {
            // BlueprintScriptableObject.OnEnable: 컴포넌트·Element 의 소유자를 this 로(dnlib 2026-11-15). 복제본의 컴포넌트·Element 는 CopyComponent/CloneElements 사본이라 원본 객체를 바꾸지 않는다.
            Tuple.Create("Kingmaker.Blueprints.BlueprintScriptableObject::OnEnable|Kingmaker.Blueprints.BlueprintComponent::set_OwnerBlueprint", "owner rebind on our component copies"),
            Tuple.Create("Kingmaker.Blueprints.BlueprintScriptableObject::OnEnable|Kingmaker.ElementsSystem.Element::set_Owner", "owner rebind on our element copies"),
            // BlueprintItem.OnEnable: m_CachedEnchantments = null(IL ldnull; stfld, dnlib 2026-11-15). null 은 공유 인스턴스가 아니다.
            Tuple.Create("Kingmaker.Blueprints.Items.BlueprintItem::OnEnable|Kingmaker.Blueprints.Items.BlueprintItem::m_CachedEnchantments", "cache reset to null"),
            // BlueprintItemWeapon.OnEnableWithLibrary: m_VisualParameters = VisualParameters ?? new — 복제본 자신의(CopyOf 로 뗀) 객체 또는 새 객체.
            Tuple.Create("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon::OnEnableWithLibrary|Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentHand::m_VisualParameters", "own copy or new object"),
            // 같은 메서드: m_VisualParameters.Prototype = Type.VisualParameters → 공유 생김. WeaponPrototypePath + TypeOwnedPrototype 로만 허용.
            Tuple.Create("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon::OnEnableWithLibrary|Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters::set_Prototype", "allowed path " + WeaponPrototypePath),
        };

        // 허용 공유 경로(CloneKit.Shared 의 경로 표기). BlueprintItemWeapon.OnEnableWithLibrary(IL 2026-11-15):
        //   if (Type != null) { m_VisualParameters = VisualParameters ?? new WeaponVisualParameters(); m_VisualParameters.Prototype = Type.VisualParameters; }
        // → 복제본의 Prototype 은 무기 형식 블루프린트(Type)가 소유한 객체를 가리킨다(원본도 같다). 블루프린트 소유 객체의 정당한 공유.
        public const string WeaponPrototypePath = ".m_VisualParameters.<Prototype>k__BackingField";

        // 허용 조건: 복제본 Prototype == 복제본 Type.VisualParameters 이고, 원본에서도 같은 관계. 하나라도 깨지면 거짓(→ 공유 검사가 던진다).
        public static bool TypeOwnedPrototype(object clonePrototype, object cloneTypeVisual, object srcPrototype, object srcTypeVisual)
        {
            return clonePrototype != null && cloneTypeVisual != null && srcTypeVisual != null
                && ReferenceEquals(clonePrototype, cloneTypeVisual) && ReferenceEquals(srcPrototype, srcTypeVisual);
        }

        // 공유 경로 중 설명되지 않는 것(빈 목록 = 통과). 허용 경로는 조건이 참일 때 그 경로 하나만 빠진다.
        public static List<string> Unexplained(IEnumerable<string> shared, bool weaponPrototypeTypeOwned)
        {
            if (shared == null) throw new ArgumentNullException("shared");
            return shared.Where(p => !(weaponPrototypeTypeOwned && p == WeaponPrototypePath)).ToList();
        }
    }
}
