using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
     
    //   펫 직업 = AnimalCompanionClass 26b10d43 값 복사: 새 블루프린트 · 새 컴포넌트(PrerequisiteIsPet 복사) · m_Progression → 우리 진행표 · m_Archetypes = [](동료 아키타입 없음, 설계 §8).
    //   펫 진행표 = AnimalCompanionBaseProgression f54159e8 값 복사: LevelEntries = ProgressionEdit.RemoveExactlyOnce(그때의 라이브 원본, SpiritRules.RemovedPetFeatures)의 새 항목,
    //              m_Classes = [새 ClassWithLevel{우리 직업}] (양방향 연결, F06). 첫 프레임에 ProgressionSync 가 라이브 원본으로 다시 맞춘다(F07 — TTT 포스트픽스 뒤).
    //   양방향 되읽기: class.Progression == ourProg && ourProg.m_Classes = [ourClass] 정확히 1개.
    public static class PetClass
    {
        public static BlueprintCharacterClass Class;
        public static BlueprintProgression Progression;
        public static BlueprintFeature SlotLock;
        public static BlueprintFeature Proficiency;

        public static void Build()
        {
            Isolation.TakeSnapshot(); // 본편 원본 값·참조를 복사 전에 읽어 둔다(계획 T3 「Build 전 스냅샷」)
            if (Class != null || Progression != null) throw new InvalidOperationException("WeaponSpiritShaman: PetClass.Build() called twice");
            var srcClass = Get<BlueprintCharacterClass>(Guids.Game.AnimalCompanionClass);
            var srcProg = Get<BlueprintProgression>(Guids.Game.AnimalCompanionBaseProgression);
            // 원본 계약(C2): 직업 ↔ 진행표가 서로를 가리키고, 직업 컴포넌트는 (알려진 다른 모드 것을 뺀 뒤) PrerequisiteIsPet 하나(jbp 직독 — 시험 `jbp: progression classes`).
            if (!ReferenceEquals(srcClass.Progression, srcProg)) throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionClass.Progression is not AnimalCompanionBaseProgression");
            var srcClasses = (BlueprintProgression.ClassWithLevel[])GetF(srcProg, "m_Classes");
            if (srcClasses == null || srcClasses.Length != 1 || !ReferenceEquals(srcClasses[0].Class, srcClass))
                throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionBaseProgression.m_Classes is not exactly [AnimalCompanionClass]");
            // 다른 모드가 Build 전에 붙인 것(핫픽스 2026-11-14): ExpandedContent·MAKING FRIENDS 의 PrerequisiteNoFeature 2개는 ForeignComponents.Rules 의
            // (원본, 형식, 참조 GUID) 규칙으로만 알아보고 복사본에서 뺀다(이유는 규칙 Reason). 모르는 것·형식만 같고 참조가 다른 것은 Strip 이 던진다.
            var srcComps = srcClass.ComponentsArray;
            if (srcComps == null) throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionClass components are null");
            var foreign = ForeignComponents.Strip("AnimalCompanionClass", Guids.Game.AnimalCompanionClass, ForeignComponentViews.Of(srcClass));
            var isPet = srcComps[ForeignComponents.PetClassIsPetIndex(foreign)];
            if (isPet.GetType() != typeof(PrerequisiteIsPet)) throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionClass kept component is " + isPet.GetType().AssemblyQualifiedName + ", not the game's PrerequisiteIsPet");
            ModContext.Log(foreign.Report());
            // 위 Drop 의 이유(「영병은 그 특성을 갖지 않는다」)를 런타임에 확인할 참조 블루프린트 해결 상태를 지금 1회 고정한다(SpiritPetFactGuard.Check 가 Route 에서 쓴다).
            SpiritPetFactGuard.ResolveAtBuild();

            var edit = ProgressionEdit.RemoveExactlyOnce(ToEntries(srcProg.LevelEntries), SpiritRules.RemovedPetFeatures);
            int additional = srcClasses[0].AdditionalLevel;
            Progression = Create<BlueprintProgression>("WeaponSpiritShamanPetProgression", Guids.PetProgression, p =>
            {
                CloneKit.CopyInto(srcProg, p);
                p.ComponentsArray = new BlueprintComponent[0];
                p.LevelEntries = ToLevelEntries(edit.Kept);
                SetField(p, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(Guids.PetClass), AdditionalLevel = additional } });
            });
            Class = Create<BlueprintCharacterClass>("WeaponSpiritShamanPetClass", Guids.PetClass, c =>
            {
                CloneKit.CopyInto(srcClass, c);
                c.ComponentsArray = new BlueprintComponent[0];
                SetField(c, "m_Progression", Ref<BlueprintProgressionReference>(Guids.PetProgression));
                SetField(c, "m_Archetypes", new BlueprintArchetypeReference[0]);
            });
            AddComponents(Class, CloneKit.Copy((PrerequisiteIsPet)isPet));

            // 양방향 되읽기(F06): 게임이 읽는 경로(Progression 속성·ClassWithLevel.Class)로.
            var ourClasses = (BlueprintProgression.ClassWithLevel[])GetF(Progression, "m_Classes");
            if (!ReferenceEquals(Class.Progression, Progression) || ourClasses == null || ourClasses.Length != 1 || !ReferenceEquals(ourClasses[0].Class, Class))
                throw new InvalidOperationException("WeaponSpiritShaman: pet class <-> pet progression readback failed (class.Progression=" + (Class.Progression == null ? "null" : Class.Progression.name) + ")");
            if (Class.Archetypes.Count() != 0) throw new InvalidOperationException("WeaponSpiritShaman: pet class has archetypes after build");
            VerifyRemoved(Progression);
            ModContext.Log("WSS pet progression built from live source: removed " + string.Join(" ", edit.RemovedAt.Select(r => r.ToString()).ToArray()));

            // 비무기 잠금 5칸(설계 §4.4) — 무기 칸(MainHand·OffHand·Weapon1~8)은 잠그지 않는다.
            SlotLock = Create<BlueprintFeature>("WeaponSpiritShamanSlotLockFeature", Guids.SlotLockFeature, f => Describe(f, "SlotLock.Name", "SlotLock.Description"));
            // LockEquipmentSlot.SlotType 은 비공개 중첩 열거형 → 필드 형식으로 이름을 값으로 바꾼다(없는 이름이면 Enum.Parse 가 던진다).
            var slotTypeField = FindField(typeof(LockEquipmentSlot), "m_SlotType");
            if (slotTypeField == null || !slotTypeField.FieldType.IsEnum) throw new MissingFieldException("WeaponSpiritShaman: LockEquipmentSlot.m_SlotType (enum) not found");
            // 명시 배열(LINQ 아님): 시험 `wiring: AddComponents arguments are new instances` 가 원소 출처(newobj)를 IL 로 본다.
            var locks = new BlueprintComponent[SpiritRules.SpiritLockedSlots.Length];
            for (int i = 0; i < locks.Length; i++)
            {
                var l = new LockEquipmentSlot();
                SetField(l, "m_SlotType", Enum.Parse(slotTypeField.FieldType, SpiritRules.SpiritLockedSlots[i]));
                locks[i] = l;
            }
            AddComponents(SlotLock, locks);
            var locked = SlotLock.ComponentsArray.OfType<LockEquipmentSlot>().Select(l => GetF(l, "m_SlotType").ToString()).ToArray();
            if (!locked.SequenceEqual(SpiritRules.SpiritLockedSlots)) throw new InvalidOperationException("WeaponSpiritShaman: slot lock readback [" + string.Join(",", locked) + "]");
        }

        // 영병 숙련(설계 §4.4 전 무기 + §14 Q4 경·중갑): 무기 범주 = SpiritBlueprints 의 범주 루프 결과(C4 — 선택지와 같은 59).
        public static void BuildProficiency(WeaponCategory[] categories)
        {
            if (categories == null || categories.Length == 0) throw new ArgumentException("WeaponSpiritShaman: BuildProficiency: no categories");
            if (Proficiency != null) throw new InvalidOperationException("WeaponSpiritShaman: BuildProficiency called twice");
            if (categories.Distinct().Count() != categories.Length) throw new ArgumentException("WeaponSpiritShaman: BuildProficiency: duplicate categories");
            Proficiency = Create<BlueprintFeature>("WeaponSpiritShamanProficiencyFeature", Guids.ProficiencyFeature, f => Describe(f, "Proficiency.Name", "Proficiency.Description"));
            var armor = SpiritRules.SpiritArmorProficiencies.Select(n => (ArmorProficiencyGroup)Enum.Parse(typeof(ArmorProficiencyGroup), n)).ToArray();
            AddComponents(Proficiency, new AddProficiencies { WeaponProficiencies = categories.ToArray(), ArmorProficiencies = armor });
            var back = Proficiency.ComponentsArray.OfType<AddProficiencies>().Single();
            if (!back.WeaponProficiencies.SequenceEqual(categories) || !back.ArmorProficiencies.SequenceEqual(armor)) throw new InvalidOperationException("WeaponSpiritShaman: proficiency readback failed");
        }

        // ProgressionSync(첫 프레임)가 부른다: 라이브 원본에서 다시 계산한 항목으로 우리 진행표만 교체(원본은 읽기만).
        internal static void ReplaceEntries(Entry[] kept)
        {
            if (Progression == null) throw new InvalidOperationException("WeaponSpiritShaman: ReplaceEntries before PetClass.Build");
            Progression.LevelEntries = ToLevelEntries(kept);
            VerifyRemoved(Progression);
        }

        internal static Entry[] ToEntries(LevelEntry[] entries)
        {
            if (entries == null) throw new InvalidOperationException("WeaponSpiritShaman: progression LevelEntries is null");
            return entries.Select(e =>
            {
                if (e == null) throw new InvalidOperationException("WeaponSpiritShaman: null LevelEntry in progression");
                return new Entry(e.Level, e.Features.Select(f =>
                {
                    if (f == null) throw new InvalidOperationException("WeaponSpiritShaman: unresolved feature at level " + e.Level);
                    return f.AssetGuid.ToString();
                }).ToArray());
            }).ToArray();
        }

        // 항목마다 새 LevelEntry + 새 특성 참조 목록(SetFeatures 가 새 List 를 만든다 — 계획 T3 「새 LevelEntry 마다 새 특성 참조 배열」).
        static LevelEntry[] ToLevelEntries(Entry[] kept)
        {
            return kept.Select(k =>
            {
                var le = new LevelEntry { Level = k.Level };
                le.SetFeatures(k.Features.Select(g => Get<BlueprintFeatureBase>(g)).ToList());
                if (le.Features.Count != k.Features.Length) throw new InvalidOperationException("WeaponSpiritShaman: level entry readback failed at level " + k.Level);
                return le;
            }).ToArray();
        }

        static void VerifyRemoved(BlueprintProgression p)
        {
            var left = p.LevelEntries.SelectMany(e => e.Features).Where(f => SpiritRules.RemovedPetFeatures.Contains(f.AssetGuid.ToString())).Select(f => f.name).ToArray();
            if (left.Length > 0) throw new InvalidOperationException("WeaponSpiritShaman: pet progression still has " + string.Join(",", left));
        }

        static void Describe(BlueprintFeature f, string nameKey, string descriptionKey)
        {
            SetField(f, "m_DisplayName", Strings.L(nameKey));
            SetField(f, "m_Description", Strings.L(descriptionKey));
            SetField(f, "m_DescriptionShort", Strings.L(descriptionKey));
            f.HideInUI = true;
            f.IsClassFeature = true;
            f.Ranks = 1;
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
