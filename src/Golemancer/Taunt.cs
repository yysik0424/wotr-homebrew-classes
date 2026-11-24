using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Utility;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 연성 도발 오라(설계 §4.4 [컨트롤러], §12 정정 8, 정찰 §13 — 본편 KnightOfTheWallDefensiveChallenge 3단 + 우리 표식 버프). 블루프린트는 모두 신규 Create(원본은 값 비교용).
    //   영역 GolemancerTauntArea{Size = Dials.TauntRadiusFeet, Cylinder, AggroEnemies=false, AffectEnemies=true, m_TargetType Enemy, Fx = Guids.Game.Fx.TauntArea}
    //        + AbilityAreaEffectBuff{m_Buff = 효과 버프, Condition = [ContextConditionIsEnemy], CheckConditionEveryRound=false}
    //   영역 버프 GolemancerTauntAreaBuff{HiddenInUi, AddAreaEffect{영역}} — 도발 주체(골렘 또는 합체 본체)에 런타임 부착
    //   효과 버프 GolemancerTauntBuff{AttackBonusAgainstFactOwner{m_CheckedFact = 표식, AttackBonus = −TauntAttackPenalty}, AddAttackBonus(TauntAttackPenalty)}
    //        → 표식 주체 대상 합 0, 그 밖의 대상 합 TauntAttackPenalty(−2). 구성 예 src\PackWarden\AuxUnits.cs:101-112.
    //   표식 버프 GolemancerTauntMarkerBuff{HiddenInUi} — 도발 주체에 영역 버프와 함께 부착.
    // 결정 ⑤: 부착은 런타임(Attach/Detach)뿐이다 — 유닛 블루프린트 사실로 붙이지 않는다. 기대 원천 = TauntPlan.Expected(호출자가 계산해 넘김, 여기서 다시 계산하지 않는다).
    public static class Taunt
    {
        public static BlueprintAbilityAreaEffect Area;
        public static BlueprintBuff AreaBuff;
        public static BlueprintBuff EffectBuff;
        public static BlueprintBuff MarkerBuff;

        public static void Build()
        {
            if (Area != null) throw new InvalidOperationException("Golemancer: Taunt.Build() called twice");
            // 아이콘은 Icons.Apply(IconTable) 가 넣는다 — 예전에 복사하던 본편 선례 버프 d064c2b4 는 m_Icon = null 이라 도발 버프가 빈칸이었다(2026-11-15).
            // 2026-11-17(GameLogFull: Taunt.Detach → 도발 버프 제거 → Buff.OnRemove @00B0 NRE 10건): 세 버프 모두 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말).
            MarkerBuff = Create<BlueprintBuff>("GolemancerTauntMarkerBuff", Guids.TauntMarkerBuff, b => Hidden(EmptyFxOnRemove(b)));
            EffectBuff = Create<BlueprintBuff>("GolemancerTauntBuff", Guids.TauntBuff, b =>
            {
                SetField(b, "m_DisplayName", Strings.L("TauntBuff.Name"));
                SetField(b, "m_Description", Strings.L("TauntBuff.Description"));
                SetField(b, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                b.Stacking = StackingType.Replace;
                EmptyFxOnRemove(b);
            });
            var against = new AttackBonusAgainstFactOwner { AttackBonus = -GolemRules.Dials.TauntAttackPenalty, Bonus = new ContextValue(), Descriptor = ModifierDescriptor.None };
            SetField(against, "m_CheckedFact", Ref<BlueprintUnitFactReference>(Guids.TauntMarkerBuff));
            AddComponents(EffectBuff, against, new AddAttackBonus { Bonus = GolemRules.Dials.TauntAttackPenalty });

            Area = Create<BlueprintAbilityAreaEffect>("GolemancerTauntArea", Guids.TauntArea, a =>
            {
                a.Size = new Feet(GolemRules.Dials.TauntRadiusFeet);
                a.Shape = AreaEffectShape.Cylinder;
                a.AggroEnemies = false;
                a.AffectEnemies = true;
                SetField(a, "m_TargetType", EnemyTargetType());
                a.Fx = new PrefabLink { AssetId = Guids.Game.Fx.TauntArea };
            });
            var areaBuffComp = new AbilityAreaEffectBuff
            {
                Condition = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { new ContextConditionIsEnemy() } },
                CheckConditionEveryRound = false,
            };
            SetField(areaBuffComp, "m_Buff", Ref<BlueprintBuffReference>(Guids.TauntBuff));
            AddComponents(Area, areaBuffComp);

            AreaBuff = Create<BlueprintBuff>("GolemancerTauntAreaBuff", Guids.TauntAreaBuff, b => Hidden(EmptyFxOnRemove(b)));
            var add = new AddAreaEffect();
            SetField(add, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(Guids.TauntArea));
            AddComponents(AreaBuff, add);

            // 되읽기(게임이 읽는 경로).
            if (!ReferenceEquals(against.CheckedFact, MarkerBuff) || against.AttackBonus != -GolemRules.Dials.TauntAttackPenalty) throw new InvalidOperationException("Golemancer: taunt AttackBonusAgainstFactOwner readback failed");
            var eff = EffectBuff.ComponentsArray;
            if (eff.Length != 2 || !ReferenceEquals(eff[0], against) || !(eff[1] is AddAttackBonus) || ((AddAttackBonus)eff[1]).Bonus != GolemRules.Dials.TauntAttackPenalty) throw new InvalidOperationException("Golemancer: taunt effect buff components readback failed");
            if (Area.Size.Value != GolemRules.Dials.TauntRadiusFeet || Area.Shape != AreaEffectShape.Cylinder || Area.AggroEnemies || !Area.AffectEnemies
                || !Equals(FindField(typeof(BlueprintAbilityAreaEffect), "m_TargetType").GetValue(Area), EnemyTargetType()) || Area.Fx == null || Area.Fx.AssetId != Guids.Game.Fx.TauntArea)
                throw new InvalidOperationException("Golemancer: taunt area readback failed");
            if (Area.ComponentsArray.Length != 1 || !ReferenceEquals(areaBuffComp.Buff, EffectBuff) || areaBuffComp.Condition.Conditions.Length != 1 || !(areaBuffComp.Condition.Conditions[0] is ContextConditionIsEnemy))
                throw new InvalidOperationException("Golemancer: taunt area buff component readback failed");
            if (AreaBuff.ComponentsArray.Length != 1 || !ReferenceEquals(add.AreaEffect, Area) || !AreaBuff.IsHiddenInUI || !MarkerBuff.IsHiddenInUI || MarkerBuff.ComponentsArray.Length != 0)
                throw new InvalidOperationException("Golemancer: taunt area buff / marker readback failed");
        }

        // BlueprintAbilityAreaEffect.TargetType·BlueprintBuff.Flags 는 비공개 중첩 열거형 → 필드 형식으로 이름을 값으로 바꾼다(없는 이름이면 Enum.Parse 가 던진다, 선례 WSS PetClass.cs LockEquipmentSlot).
        static object EnemyTargetType() { return EnumOfField(typeof(BlueprintAbilityAreaEffect), "m_TargetType", "Enemy"); }

        static object EnumOfField(Type owner, string field, string name)
        {
            var f = FindField(owner, field);
            if (f == null || !f.FieldType.IsEnum) throw new MissingFieldException("Golemancer: " + owner.Name + "." + field + " (enum) not found");
            return Enum.Parse(f.FieldType, name);
        }

        static void Hidden(BlueprintBuff b)
        {
            SetField(b, "m_Flags", EnumOfField(typeof(BlueprintBuff), "m_Flags", "HiddenInUi"));
            b.Stacking = StackingType.Replace;
            NoNames(b);
        }

        // 2026-11-16: 이름·설명 문자열이 없는 블루프린트(UI 숨김)라도 필드를 null 로 두지 않는다 — null LocalizedString 은 읽는 곳마다 "<null>" 이 된다
        // (LocalizedString.op_Implicit @0x000C~0x000F). 「문구 없음」을 명시한다(BlueprintUtil.NoText = 본편 jbp 의 빈 키). BuildAll 끝의 RequireOwnLocalizedStrings 가 되읽는다.
        // 숨은 버프(도발 표식·영역 버프) · 숨은 수치 특성(Materials 펫·합체 수치) · 펫 진행표(PetClass) 가 쓴다.
        internal static void NoNames(Kingmaker.Blueprints.Facts.BlueprintUnitFact b)
        {
            SetField(b, "m_DisplayName", NoText());
            SetField(b, "m_Description", NoText());
            SetField(b, "m_DescriptionShort", NoText());
        }

        static void RequireBuilt()
        {
            if (Area == null || AreaBuff == null || EffectBuff == null || MarkerBuff == null) throw new InvalidOperationException("Golemancer: Taunt.Build() has not run");
        }

        // 주체에 붙은 도발(영역 버프·표식)의 개수.
        public static int AreaBuffCount(UnitEntityData u) { RequireBuilt(); return Count(u, AreaBuff); }
        public static int MarkerCount(UnitEntityData u) { RequireBuilt(); return Count(u, MarkerBuff); }

        static int Count(UnitEntityData u, BlueprintBuff bp)
        {
            if (u == null) throw new ArgumentNullException("u");
            return u.Buffs.Enumerable.Count(b => ReferenceEquals(b.Blueprint, bp));
        }

        // 주체 1 개의 도발 상태: 영역 버프·표식이 둘 다 정확히 1 이면 붙음, 둘 다 0 이면 없음, 그 밖(반쪽·중복)은 판정 불가 → 던진다.
        public static bool IsAttached(UnitEntityData u)
        {
            int a = AreaBuffCount(u), m = MarkerCount(u);
            if (a == 1 && m == 1) return true;
            if (a == 0 && m == 0) return false;
            throw new InvalidOperationException("Golemancer: taunt on " + u.CharacterName + " is broken (area buffs " + a + ", markers " + m + ")");
        }

        public static void Attach(UnitEntityData u)
        {
            if (IsAttached(u)) throw new InvalidOperationException("Golemancer: Taunt.Attach on " + u.CharacterName + " which already has the taunt");
            // 주체 자신을 시전자로(영역의 「적」 = 주체의 적). 기간 없음(영구) — 떼는 것은 Detach 뿐.
            var a = u.Buffs.AddBuff(AreaBuff, u, null, null);
            var m = u.Buffs.AddBuff(MarkerBuff, u, null, null);
            if (a == null || m == null || !IsAttached(u)) throw new InvalidOperationException("Golemancer: Taunt.Attach readback failed on " + u.CharacterName);
        }

        public static void Detach(UnitEntityData u)
        {
            if (!IsAttached(u)) throw new InvalidOperationException("Golemancer: Taunt.Detach on " + u.CharacterName + " which has no taunt");
            u.Buffs.RemoveFact(AreaBuff);
            u.Buffs.RemoveFact(MarkerBuff);
            if (IsAttached(u)) throw new InvalidOperationException("Golemancer: Taunt.Detach readback failed on " + u.CharacterName);
        }

        // 원천 = 주인 본체와 주인의 골렘. 골렘이 관측되지 않으면(펫 칸 비었거나 우리 골렘 아님) 골렘 쪽 원천 없음.
        public static TauntSource SourceOf(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            var pet = master.GetPet(PetType.AnimalCompanion);
            var golem = pet != null && GolemMaster.IsOurGolem(pet) ? pet : null;
            return SourceOn(master, golem);
        }

        static TauntSource SourceOn(UnitEntityData master, UnitEntityData golem)
        {
            bool m = IsAttached(master);
            bool g = golem != null && IsAttached(golem);
            return m && g ? TauntSource.Both : m ? TauntSource.Master : g ? TauntSource.Golem : TauntSource.None;
        }

        // 기대 = 인자(Apply 면 코어가 적용 뒤 상태 Ready 로 계산한 값, 비적용 호출은 호출자의 TauntPlan.Expected). Both → TauntPlan.Fix 로 교정 + WARN.
        // 되읽기: 교정 뒤 원천 == 기대, 아니면 던진다(Apply 트랜잭션이면 ApplyFailed).
        public static void Reconcile(UnitEntityData master, UnitEntityData golem, TauntSource expected)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (golem == null) throw new ArgumentNullException("golem", "Golemancer: Taunt.Reconcile needs the golem (GolemMaster.GolemOf)");
            var current = SourceOn(master, golem);
            if (current == TauntSource.Both) WotrClassMod.Common.ModContext.Log("WARN Golemancer: taunt on both " + master.CharacterName + " and the golem — fixing to " + expected);
            var fix = TauntPlan.Fix(current, expected);
            foreach (var d in fix.Detach) Detach(d == TauntSource.Golem ? golem : master);
            if (fix.Attach.HasValue) Attach(fix.Attach.Value == TauntSource.Golem ? golem : master);
            var after = SourceOn(master, golem);
            if (after != expected) throw new InvalidOperationException("Golemancer: taunt source after reconcile is " + after + ", expected " + expected);
        }
    }
}
