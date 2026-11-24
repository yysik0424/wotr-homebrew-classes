using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 골렘 펫 직업·진행표(계획 T3 「깊은 복사 계약」 표 1·2행, 설계 §4.4, 정찰 §4.2·§4.3, 선례 src\WeaponSpiritShaman\PetClass.cs).
    //   펫 직업 = ConstructClass fd66bdea 값 복사(CloneKit.CopyInto): 새 블루프린트 · ComponentsArray = [] · m_Progression → 우리 진행표 · m_Archetypes = [].
    //            BAB·내성 진행 참조는 원본과 공유(읽기만). 원본 컴포넌트는 0개여야 한다(jbp — 시험 `jbp: construct class`).
    //   펫 진행표 = 신규 Create: m_Classes = [새 ClassWithLevel{우리 직업}], LevelEntries = [L1: ConstructType](특기 선택 없음 — 정찰 §4.2 「선택지를 두지 않는 것이 안전」).
    //            성장(자연 방어·DR·힘·슬램)은 수치 특성(T4 StatRecipe) — 결정 ②: 진행표는 틀만.
    //   양방향 되읽기(WSS PetClass.cs:60-63): class.Progression == ourProg && ourProg.m_Classes = [ourClass] 정확히 1개.
    public static class PetClass
    {
        public static BlueprintCharacterClass Class;
        public static BlueprintProgression Progression;

        public static void Build()
        {
            if (Class != null || Progression != null) throw new InvalidOperationException("Golemancer: PetClass.Build() called twice");
            GolemIsolation.TakeSnapshot(); // 본편 원본 값·참조를 복사 전에 읽어 둔다(WSS PetClass.cs:29 과 같은 자리)
            var src = Get<BlueprintCharacterClass>(Guids.Game.ConstructClass);
            var constructType = Get<BlueprintFeature>(Guids.Game.ConstructType);
            // 원본 계약(C2): 컴포넌트 0개, 진행표 = ConstructProgression, 아키타입 없음.
            if (src.ComponentsArray == null || src.ComponentsArray.Length != 0) throw new InvalidOperationException("Golemancer: ConstructClass components are not [] (got " + (src.ComponentsArray == null ? "null" : src.ComponentsArray.Length.ToString()) + ")");
            if (src.Progression == null || src.Progression.AssetGuid.ToString() != Guids.Game.ConstructProgression) throw new InvalidOperationException("Golemancer: ConstructClass.Progression is not ConstructProgression");
            if (src.Archetypes.Count() != 0) throw new InvalidOperationException("Golemancer: ConstructClass has archetypes");

            Progression = Create<BlueprintProgression>("GolemancerPetProgression", Guids.PetProgression, p =>
            {
                p.IsClassFeature = true;
                p.Ranks = 1;
                Taunt.NoNames(p); // 2026-11-16: 펫 진행표는 화면에 이름이 없다(본편 ConstructProgression 의 세 칸도 빈 키 — 시험 tooltip: 이 대조) — 문구 없음을 명시(null 금지)
                SetField(p, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(Guids.PetClass), AdditionalLevel = 0 } });
                var l1 = new LevelEntry { Level = 1 };
                l1.SetFeatures(new[] { (BlueprintFeatureBase)constructType }.ToList());
                p.LevelEntries = new[] { l1 };
            });
            Class = Create<BlueprintCharacterClass>("GolemancerPetClass", Guids.PetClass, c =>
            {
                CloneKit.CopyInto(src, c);
                c.ComponentsArray = new BlueprintComponent[0];
                SetField(c, "m_Progression", Ref<BlueprintProgressionReference>(Guids.PetProgression));
                SetField(c, "m_Archetypes", new BlueprintArchetypeReference[0]);
            });

            // 양방향 되읽기(게임이 읽는 경로).
            var classes = (BlueprintProgression.ClassWithLevel[])FindField(typeof(BlueprintProgression), "m_Classes").GetValue(Progression);
            if (!ReferenceEquals(Class.Progression, Progression) || classes == null || classes.Length != 1 || !ReferenceEquals(classes[0].Class, Class))
                throw new InvalidOperationException("Golemancer: pet class <-> pet progression readback failed");
            if (Class.Archetypes.Count() != 0 || Class.ComponentsArray.Length != 0) throw new InvalidOperationException("Golemancer: pet class has archetypes/components after build");
            if (!ReferenceEquals(Class.BaseAttackBonus, src.BaseAttackBonus)) throw new InvalidOperationException("Golemancer: pet class BAB readback differs from ConstructClass");
            if (Progression.LevelEntries.Length != 1 || Progression.LevelEntries[0].Level != 1 || Progression.LevelEntries[0].Features.Count != 1 || !ReferenceEquals(Progression.LevelEntries[0].Features[0], constructType))
                throw new InvalidOperationException("Golemancer: pet progression is not exactly [L1: ConstructType]");
        }
    }
}
