using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace PhantomBlade
{
     
     
    // 소유자 결정 2026-11-16(설계 §14): ① 10레벨 재충전 폐지(Refill10 은 효과 없는 숨김 특성으로만 남는다) ② 풀 최대치 × StandRules.PoolMultiplier ③ 귀신참 = 전용 하루 횟수 GhostSlashResource.
    public static class StandBlueprints
    {
        // 주먹 6종 = 본편 몽크 맨손 피해표(중형) 단계(StandRules.RushDiceByLevel, Task 6b). 1d6·1d8·2d6 은 옛 GUID 그대로, 1d10·2d8·2d10 은 2026-11-09 발급.
        public static BlueprintItemWeapon Fist1d6, Fist1d8, Fist1d10, Fist2d6, Fist2d8, Fist2d10;
        public static BlueprintAbilityResource PoolResource, FullAttackResource;
        // 귀신참 전용 하루 횟수(2026-11-16). 전력 공격 자원과 같은 방식(MakeResource + Resources + Increase(…, ChaMin1Property))이되 별개 블루프린트 → 횟수를 따로 센다.
        public static BlueprintAbilityResource GhostSlashResource;
        public static BlueprintUnitProperty PoolProperty, ChaMin1Property;
        public static BlueprintBuff StandBuff, FullAttackBuff;
        public static BlueprintActivatableAbility StandToggle;
        public static BlueprintAbility FullAttackAbility;
        public static BlueprintFeature StandFeature, Rush3, Rush7, Rush11, Rush15, Rush19, RangedGuard6, RangedGuard14, Refill10, Absorb18, FullAttackFeature;
        // 귀신참(설계 §13.2, Task 6c): 능력·투사체 수 속성·5/10/15/20레벨 특성.
        public static BlueprintAbility GhostSlashAbility;
        public static BlueprintUnitProperty GhostSlashProjectilesProperty;
        public static BlueprintFeature GhostSlash5, GhostSlash10, GhostSlash15, GhostSlash20;

        public static void Build()
        {
            if (Archetype.Bp == null) throw new InvalidOperationException("PhantomBlade: StandBlueprints.Build() before Archetype.Build() (ClassLevelGetter needs the archetype)");
            if (StandBuff != null) throw new InvalidOperationException("PhantomBlade: StandBlueprints.Build() called twice");
            var fighter = Get<BlueprintCharacterClass>(GameGuids.FighterClass);

             
            // 아래 FxOnStart 대조는 FX 를 쓰기 위해서가 아니라 「이 GUID 가 정찰한 그 사마귀 버프다」를 확인하는 정체 검사로 남긴다
            // (게임 갱신으로 GUID 가 다른 버프를 가리키면 엉뚱한 아이콘을 조용히 빌리게 되므로 — C5).
            var mantis = Get<BlueprintBuff>(Guids.Game.MantisAspectBuff);
            if (mantis.FxOnStart == null || mantis.FxOnStart.AssetId != Guids.Game.FxMantisAspect)
                throw new InvalidOperationException("PhantomBlade: MantisAspectBuff.FxOnStart is " + (mantis.FxOnStart == null ? "null" : mantis.FxOnStart.AssetId) + ", expected " + Guids.Game.FxMantisAspect);
            Sprite standIcon = mantis.Icon;
            if (standIcon == null) throw new InvalidOperationException("PhantomBlade: MantisAspectBuff has no icon");
            Sprite guardIcon = Get<BlueprintBuff>(Guids.Game.FighterCoverBuff).Icon;
            if (guardIcon == null) throw new InvalidOperationException("PhantomBlade: FighterCoverBuff has no icon");

            BuildWeapons();
            BuildProperties(fighter);

            // --- 자원(설계 §4.3·4.4): BaseValue 0, 최대치는 특성의 IncreaseResourceAmountBySharedValue 가 속성값을 더한다(정찰 §5). 휴식 회복은 게임 기본. ---
            // (2026-11-16: 풀을 채우는 것은 이 「게임 기본」 휴식과 레벨업뿐이다 — 전투 종료 재충전 폐지. 귀신참 전용 횟수 자원은 BuildGhostSlash 가 만든다.)
            PoolResource = MakeResource("PhantomBladePoolResource", Guids.StandPoolResource, "Pool.Name", "Pool.Description", standIcon);
            FullAttackResource = MakeResource("PhantomBladeFullAttackResource", Guids.FullAttackResource, "FullAttack.Name", "FullAttack.Description", standIcon);

            // --- 스탠드 버프·토글(설계 §4.2) ---
            StandBuff = Create<BlueprintBuff>("PhantomBladeStandBuff", Guids.StandBuff, b =>
            {
                Names(b, "Stand.Name", "Stand.Description", standIcon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                 
                b.FxOnStart = null; EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — FxOnStart 가 없어도 화면 있는 유닛이면 지울 때 Load 해서 NRE). 줄 수 유지(InGameAudit 줄 인용)
            });
            if (StandBuff.FxOnStart != null) throw new InvalidOperationException("PhantomBlade: StandBuff.FxOnStart readback failed: expected null (mantis FX removed), got " + StandBuff.FxOnStart.AssetId);

            StandToggle = Create<BlueprintActivatableAbility>("PhantomBladeStandToggle", Guids.StandToggleAbility, t =>
            {
                Names(t, "StandToggle.Name", "StandToggle.Description", standIcon);
                SetField(t, "m_Buff", Ref<BlueprintBuffReference>(Guids.StandBuff));
                t.Group = ActivatableAbilityGroup.None;
                t.ActivationType = AbilityActivationType.Immediately;
                t.DeactivateImmediately = true;
                t.DoNotTurnOffOnRest = true;
                t.IsOnByDefault = true;
            });
            if (StandToggle.Buff != StandBuff) throw new InvalidOperationException("PhantomBlade: StandToggle.Buff readback failed");

             
             
            var hasteRow = FxTable.Get(FxTable.FullAttackHasteKey);
            if (hasteRow.TargetGuid != Guids.FullAttackBuff || hasteRow.TargetSlot != FxSlot.BuffFxOnStart) throw new InvalidOperationException("PhantomBlade: FxTable " + hasteRow.Key + " does not target FullAttackBuff.FxOnStart");
            var hasteLink = Fx.Link(hasteRow);
            FullAttackBuff = Create<BlueprintBuff>("PhantomBladeFullAttackBuff", Guids.FullAttackBuff, b =>
            {
                Names(b, "FullAttack.Name", "FullAttack.Description", standIcon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                b.FxOnStart = hasteLink;
                // 본편 HasteBuff.jbp 의 FxOnRemove = null 은 읽을 때 빈 링크가 된다. 코드로 null 을 넣으면 엔진 Buff.OnRemove 가 NRE(2026-11-16 결함) — 빈 링크.
                b.FxOnRemove = new PrefabLink();
                b.ResourceAssetIds = FxTable.PreloadIds(Guids.FullAttackBuff);
            });
            if (FullAttackBuff.FxOnStart == null || FullAttackBuff.FxOnStart.AssetId != hasteRow.ExpectedAssetId || FullAttackBuff.FxOnRemove == null || !string.IsNullOrEmpty(FullAttackBuff.FxOnRemove.AssetId)
                || FullAttackBuff.ResourceAssetIds == null || !FullAttackBuff.ResourceAssetIds.SequenceEqual(FxTable.PreloadIds(Guids.FullAttackBuff)))
                throw new InvalidOperationException("PhantomBlade: FullAttackBuff FX readback failed (FxOnStart " + (FullAttackBuff.FxOnStart == null ? "null" : FullAttackBuff.FxOnStart.AssetId) + ", expected " + hasteRow.ExpectedAssetId + "; preload " + (FullAttackBuff.ResourceAssetIds == null ? "null" : string.Join(",", FullAttackBuff.ResourceAssetIds)) + ")");
            var applyBuff = new ContextActionApplyBuff
            {
                // 1라운드: Rate=Rounds, 주사위 0개 + 보너스 1
                DurationValue = new ContextDurationValue
                {
                    Rate = DurationRate.Rounds,
                    DiceType = DiceType.Zero,
                    DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 1 },
                },
                ToCaster = true,
            };
            SetField(applyBuff, "m_Buff", Ref<BlueprintBuffReference>(Guids.FullAttackBuff));
            if (applyBuff.Buff != FullAttackBuff) throw new InvalidOperationException("PhantomBlade: ContextActionApplyBuff.Buff readback failed");
            FullAttackAbility = Create<BlueprintAbility>("PhantomBladeFullAttackAbility", Guids.FullAttackAbility, ab =>
            {
                Names(ab, "FullAttack.Name", "FullAttack.Description", standIcon);
                ab.Type = AbilityType.Special;
                ab.Range = AbilityRange.Personal;
                ab.ActionType = UnitCommand.CommandType.Swift;
                ab.CanTargetSelf = true;
                ab.LocalizedDuration = Strings.L("FullAttack.Duration");
                ab.LocalizedSavingThrow = Strings.L("SavingThrow.None");
                ab.ResourceAssetIds = FxTable.PreloadIds(Guids.FullAttackAbility); // 본편 Haste.jbp 처럼 거는 버프의 FX 를 미리 읽는다
            });
            if (FullAttackAbility.ResourceAssetIds == null || !FullAttackAbility.ResourceAssetIds.SequenceEqual(FxTable.PreloadIds(Guids.FullAttackAbility)))
                throw new InvalidOperationException("PhantomBlade: FullAttackAbility ResourceAssetIds readback failed");
            // 원장 전부가 적용됐는지(C4): 지금 적용하는 행은 FullAttackHaste 하나 — 표에 행·미리 읽기·FX 금지 항목이 더 생기면 여기서 던진다(조용히 안 붙은 FX 금지).
            var unapplied = FxTable.Rows.Where(r => r.Key != FxTable.FullAttackHasteKey).Select(r => r.Key)
                .Concat(FxTable.Preload.Keys.Where(g => g != Guids.FullAttackBuff && g != Guids.FullAttackAbility))
                .Concat(FxTable.NoFx.Where(g => g != Guids.StandBuff)).ToArray();
            if (unapplied.Length != 0) throw new InvalidOperationException("PhantomBlade: FxTable entries not applied by StandBlueprints: " + string.Join(", ", unapplied));
            if (StandBuff.FxOnStart != null) throw new InvalidOperationException("PhantomBlade: StandBuff is ruled FX-free (FxTable.NoFx) but has FxOnStart " + StandBuff.FxOnStart.AssetId);
            // 액션 요소의 Owner 는 JSON 로드 때만 채워진다(BlueprintScriptableObject.OnEnable 은 m_AllElements 만 돈다, IL 확인) → 직접 단다.
            applyBuff.name = "$ContextActionApplyBuff$" + Guids.FullAttackAbility + "#0";
            applyBuff.Owner = FullAttackAbility;
            var resLogic = new AbilityResourceLogic { Amount = 1 };
            SetField(resLogic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.FullAttackResource));
            SetField(resLogic, "m_IsSpendResource", true);
            AddComponents(FullAttackAbility, resLogic, new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { applyBuff } } });
            if (resLogic.RequiredResource != FullAttackResource || !resLogic.IsSpendResource) throw new InvalidOperationException("PhantomBlade: AbilityResourceLogic readback failed");

            // --- 특성(설계 §4.3·4.4) ---
            StandFeature = MakeFeature("PhantomBladeStandFeature", Guids.StandFeature, "Stand.Name", "Stand.Description", standIcon);
            AddComponents(StandFeature,
                Facts(StandToggle),
                Resources(PoolResource),
                Increase(PoolResource, PoolProperty));

             
            // 19레벨 = 비어 있던 갑옷 숙달 자리(설계 §4.5 19행) → Rush19(5타). 레벨표에 다는 곳은 AddRushFeatures(StandRules.RushTable 한 곳).
            Rush3 = MakeFeature("PhantomBladeRush3", Guids.RushFeature3, "Rush3.Name", "Rush3.Description", standIcon);
            Rush7 = MakeFeature("PhantomBladeRush7", Guids.RushFeature7, "Rush7.Name", "Rush7.Description", standIcon);
            Rush11 = MakeFeature("PhantomBladeRush11", Guids.RushFeature11, "Rush11.Name", "Rush11.Description", standIcon);
            Rush15 = MakeFeature("PhantomBladeRush15", Guids.RushFeature15, "Rush15.Name", "Rush15.Description", standIcon);
            Rush19 = MakeFeature("PhantomBladeRush19", Guids.RushFeature19, "Rush19.Name", "Rush19.Description", standIcon);
            Absorb18 = MakeFeature("PhantomBladeAbsorb18", Guids.Absorb18Feature, "Absorb18.Name", "Absorb18.Description", standIcon);
            Absorb18.HideInUI = true;
            Absorb18.HideInCharacterSheetAndLevelUp = true;
            // 폐지된 10레벨 「재충전」(소유자 결정 2026-11-16, 설계 §14 ①). 블루프린트는 같은 GUID 로 남긴다 — 이미 이 특성을 가진 캐릭터의 세이브가 사실로 참조하고,
            //   엔진은 블루프린트 없는 사실을 「saves-support」 실행 인자가 있을 때만 지운다(EntityFactsManager.PostLoad IL 0044~0088: 없으면 null 블루프린트로 EntityFact.PostLoad).
            //   컴포넌트 0(효과 없음), 진행표에 넣지 않음, UI 숨김. 옛 세이브의 StandRefill 런타임 컴포넌트는 로드 때 엔진이 경고 1줄과 함께 버린다
            //   (EntityFact.PostLoadComponents IL 006A~008B 「can't find source component」 → Dispose). 형식 StandRefill 은 세이브에 이름이 남지 않아 지워도 된다
            //   (런타임 형식 = 엔진의 UnitFactComponentDelegate<EmptyComponentData>.ComponentRuntime, 키 = 컴포넌트 이름 문자열).
            Refill10 = MakeFeature("PhantomBladeRefill10", Guids.RefillFeature10, "Refill.Name", "Refill.Description", standIcon);
            Refill10.HideInUI = true;
            Refill10.HideInCharacterSheetAndLevelUp = true;
            if (!Refill10.HideInUI || !Refill10.HideInCharacterSheetAndLevelUp || Refill10.ComponentsArray.Length != 0)
                throw new InvalidOperationException("PhantomBlade: retired Refill10 readback failed (must be hidden and have no components; has " + Refill10.ComponentsArray.Length + ")");

             
            // 이 두 특성은 레벨업 화면의 설명용이다(컴포넌트 없음).
            RangedGuard6 = MakeFeature("PhantomBladeRangedGuard6", Guids.RangedGuard6, "RangedGuard.Name", "RangedGuard.Description", guardIcon);
            RangedGuard14 = MakeFeature("PhantomBladeRangedGuard14", Guids.RangedGuard14, "RangedGuard.Name", "RangedGuard.Description", guardIcon);

            // 흡수·원거리 방어는 스탠드 버프에(토글을 끄면 함께 꺼진다). 받는 피해의 25%를 흡수한다. Absorb18은 이전 저장에서만 참조한다.
            // (전투 종료 재충전 컴포넌트 StandRefill 은 2026-11-16 폐지 — 풀을 채우는 것은 엔진의 휴식·레벨업뿐이다.)
            // StandRushState: 러시 라운드당 N 타 상태(F1, Task 6b 카운터). 상태는 유닛 것 — 토글을 꺼도 지우지 않는다(fix round 1 ①).
            AddComponents(StandBuff, new StandAbsorb(), new StandRangedGuard(), new StandRushState());
            if (StandBuff.ComponentsArray.OfType<StandAbsorb>().Count() != 1 || StandBuff.ComponentsArray.OfType<StandRangedGuard>().Count() != 1 || StandBuff.ComponentsArray.OfType<StandRushState>().Count() != 1)
                throw new InvalidOperationException("PhantomBlade: StandAbsorb/StandRangedGuard/StandRushState readback failed");
             
            AddComponents(StandBuff, new StandSpiritLifecycle());
            if (StandBuff.ComponentsArray.OfType<StandSpiritLifecycle>().Count() != 1) throw new InvalidOperationException("PhantomBlade: StandSpiritLifecycle readback failed");
             
            AddComponents(StandBuff, new StandSpiritAnimator());
            if (StandBuff.ComponentsArray.OfType<StandSpiritAnimator>().Count() != 1) throw new InvalidOperationException("PhantomBlade: StandSpiritAnimator readback failed");
             
            AddComponents(StandBuff, new StandAttackStatScope());
            if (StandBuff.ComponentsArray.OfType<StandAttackStatScope>().Count() != 1) throw new InvalidOperationException("PhantomBlade: StandAttackStatScope readback failed");

             
            AddComponents(StandBuff, new GhostSlashProjectileVeil());
            if (StandBuff.ComponentsArray.OfType<GhostSlashProjectileVeil>().Count() != 1) throw new InvalidOperationException("PhantomBlade: GhostSlashProjectileVeil readback failed");

            FullAttackFeature = MakeFeature("PhantomBladeFullAttackFeature", Guids.FullAttackFeature, "FullAttack.Name", "FullAttack.Description", standIcon);
            AddComponents(FullAttackFeature,
                Facts(FullAttackAbility),
                Resources(FullAttackResource),
                Increase(FullAttackResource, ChaMin1Property));

            BuildRushTriggers();
            BuildGhostSlash(standIcon);

            // --- 레벨표(설계 §4.5). 레벨은 StandRules 상수·러시 표(규칙 함수와 같은 원장). ---
            Archetype.AddAt(StandRules.StandLevel, StandFeature);
            AddRushFeatures(new[] { Rush3, Rush7, Rush11, Rush15, Rush19 });
            Archetype.AddAt(StandRules.RangedGuardLevel, RangedGuard6);
            // (10레벨 재충전 특성은 2026-11-16 부터 진행표에 없다 — 위 폐지 주석.)
            Archetype.AddAt(StandRules.RangedGuard2Level, RangedGuard14);
            Archetype.AddAt(StandRules.FullAttackLevel, FullAttackFeature);
            AddGhostSlashFeatures(new[] { GhostSlash5, GhostSlash10, GhostSlash15, GhostSlash20 });
        }

         
        // 능력 = 본편 작열 광선(Spells/Level2/ScorchingRay.jbp cdb106d5…)의 다발 광선 방식을 그대로 따른다:
        //   AbilityDeliverProjectile(Type Simple, NeedAttackRoll, m_Weapon = RayItem(RangedTouch), UseMaxProjectilesCount + 랭크 Default) — 투사체마다 따로 원거리 접촉 굴림,
        //   전부 고른 한 대상에게(작열 광선과 같음 — 같은 대상에 겹쳐 쏜다). 투사체 수 = 랭크 = GhostSlashProjectilesProperty(게터 → GhostSlash.Projectiles(레벨)).
        // 명중 능력치 = 매력, 민첩과 무관: ReplaceAttackRollBonusStat + AttackRollBonusStat = Charisma → 엔진이 굴림 동안 ContextData<AttackBonusStatReplacement> 를 열고
        //   (<Deliver>d__36 IL 0457~046D), RuleCalculateAttackBonusWithoutTarget.OnTrigger 가 비교 없이 그 능력치로 바꾼다(IL 003E~0054). 공격 보너스 = BAB(IL 001A~0038) + 매력 수정치 + 기타.
         
        // 피해 = 러시 주사위(StandRules.RushDiceByLevel, 시전 시 레벨) + 매력 수정치(ContextValue CasterProperty StatBonusCharisma — 본편 선례 Equipment/DLC6BonusItems/RobeOfMagmaAreaEffect.jbp 3f0d2399…), 역장.
        // 치명타 = 본편 광선 규칙: ContextActionDealDamage(IgnoreCritical=false)가 굴림의 확정 치명타에 무기(RayType: 20/×2) 배수를 쓴다(GetDamageInfo IL 0069~00C4). 추가 배수 없음.
        // 비용(소유자 결정 2026-11-16, 설계 §14 ③) = 전용 하루 횟수 GhostSlashResource 1회(GhostSlash.UsesPerCast): AbilityResourceLogic(CostIsCustom) + GhostSlashCost(선례 MagusSpellRecall 1bd76e00…).
        //   흡수 풀은 건드리지 않는다(종전: 풀에서 ⌈레벨/2⌉). 횟수 = max(1, 매력 수정치) — 전력 공격과 같은 구성(MakeResource + Resources + Increase(…, ChaMin1Property)), 자원은 별개.
        //   횟수 0 이면 IsAbilityRestrictionPassed = HasEnoughResource 가 false → 시전 불가(엔진 공개 경로).
        // 스탠드 필요: AbilityCasterHasFacts[StandBuff](FullAttackAbility 와 같은 방식). 사거리 = Close(엔진 30 ft, BlueprintAbility.GetRange IL 0065).
        // 러시와 분리: 광선 경로는 RuleAttackWithWeapon 없이 RuleAttackRoll 만 만든다(<Deliver>d__36 IL 0500~0538; RuleAttackWithWeapon 은 IsArcherBlast =
        //   능력에 AbilityKineticist 컴포넌트가 있을 때만 — 이 능력엔 없다) → 러시 트리거(AddInitiatorAttackWithWeaponTrigger)·령 주먹 동작(RuleAttackWithWeapon 구독)을 지나지 않는다.
        //   2026-11-15: 대신 시전 자체(RuleCastSpell 성공)에 령이 한 번 휘두른다(StandSpiritAnimator RuleCastSpell 처리기, SpiritRules.CastSwingFor) — 광선 명중 수와 무관.
        static void BuildGhostSlash(Sprite icon)
        {
            if (StandBuff == null || ChaMin1Property == null) throw new InvalidOperationException("PhantomBlade: BuildGhostSlash before StandBuff/ChaMin1Property exist");
            if (GhostSlashAbility != null || GhostSlashResource != null) throw new InvalidOperationException("PhantomBlade: BuildGhostSlash called twice");
            // 전용 하루 횟수 자원(전력 공격 자원과 같은 MakeResource — BaseValue 0, 최대치는 특성의 Increase 가 ChaMin1Property 를 더한다).
            GhostSlashResource = MakeResource("PhantomBladeGhostSlashResource", Guids.GhostSlashResource, "GhostSlashUses.Name", "GhostSlashUses.Description", icon);
            var ray = Get<BlueprintItemWeapon>(Guids.Game.RayItem);
            if (ray.Type == null || ray.Type.AttackType != AttackType.RangedTouch) throw new InvalidOperationException("PhantomBlade: RayItem type is " + (ray.Type == null ? "null" : ray.Type.AttackType.ToString()) + ", expected RangedTouch");
             
            //   별도 명중 FX(ProjectileHit.HitFx — 엔진 HitPlayer 가 View 와 따로 만든다)가 없는 블루프린트면 숨긴 뒤 아무것도 안 남는다: 빌드에서 던진다.
            var projectile = Get<BlueprintProjectile>(Guids.Game.RayOfEnfeeblementProjectile);
            if (projectile.ProjectileHit == null || projectile.ProjectileHit.HitFx == null || string.IsNullOrEmpty(projectile.ProjectileHit.HitFx.AssetId))
                throw new InvalidOperationException("PhantomBlade: ghost slash projectile " + projectile.name + " has no HitFx (its flight is hidden, so nothing would be shown on a hit)");
            string[] assets = ProjectileAssetIds(projectile);

            // 투사체 수 = 속성(합: BaseValue 0 + 게터 하나).
            GhostSlashProjectilesProperty = Create<BlueprintUnitProperty>("PhantomBladeGhostSlashProjectilesProperty", Guids.GhostSlashProjectilesProperty, p =>
            {
                p.BaseValue = 0;
                p.OperationOnComponents = BlueprintUnitProperty.MathOperation.Sum;
            });
            AddComponents(GhostSlashProjectilesProperty, new GhostSlashProjectilesGetter());
            if (GhostSlashProjectilesProperty.ComponentsArray.Length != 1 || !(GhostSlashProjectilesProperty.ComponentsArray[0] is GhostSlashProjectilesGetter))
                throw new InvalidOperationException("PhantomBlade: GhostSlashProjectilesProperty readback: expected exactly one GhostSlashProjectilesGetter");

            GhostSlashAbility = Create<BlueprintAbility>("PhantomBladeGhostSlashAbility", Guids.GhostSlashAbility, ab =>
            {
                Names(ab, "GhostSlash.Name", "GhostSlash.Description", icon);
                ab.Type = AbilityType.Supernatural;  
                ab.Range = AbilityRange.Close;       
                ab.ActionType = UnitCommand.CommandType.Standard;
                ab.CanTargetEnemies = true;
                ab.CanTargetFriends = false;
                ab.CanTargetSelf = false;
                ab.CanTargetPoint = false;          // 광선은 지점에 못 쏜다(<Deliver>d__36 IL 03B0 "Can't do ranged touch to point")
                ab.SpellResistance = false;
                ab.ShouldTurnToTarget = true;
                ab.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                ab.EffectOnAlly = AbilityEffectOnUnit.None;
                ab.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;            // 작열 광선과 같음
                ab.LocalizedDuration = Strings.L("GhostSlash.Duration");
                ab.LocalizedSavingThrow = Strings.L("SavingThrow.None");
                ab.ResourceAssetIds = assets;
            });

            var deliver = new AbilityDeliverProjectile
            {
                Type = AbilityProjectileType.Simple,
                NeedAttackRoll = true,
                ReplaceAttackRollBonusStat = true,
                AttackRollBonusStat = StatType.Charisma,
                UseMaxProjectilesCount = true,
                MaxProjectilesCountRank = AbilityRankType.Default,
                DelayBetweenProjectiles = ScorchingRayDelaySeconds,
            };
            SetField(deliver, "m_Projectiles", Enumerable.Range(0, GhostSlash.MaxProjectiles).Select(_ => Ref<BlueprintProjectileReference>(Guids.Game.RayOfEnfeeblementProjectile)).ToArray());
            SetField(deliver, "m_Weapon", Ref<BlueprintItemWeaponReference>(Guids.Game.RayItem));

            var rank = new ContextRankConfig();
            SetField(rank, "m_Type", AbilityRankType.Default);
            SetField(rank, "m_BaseValueType", ContextRankBaseValueType.CustomProperty);
            SetField(rank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(Guids.GhostSlashProjectilesProperty));
            SetField(rank, "m_Progression", ContextRankProgression.AsIs);
            SetField(rank, "m_UseMin", false);
            SetField(rank, "m_UseMax", false);
            // 배열은 본편 JSON 의 [] 와 같게 빈 배열(null 아님). 이름마다 따로 쓴다(리플렉션 원장 시험이 SetField 리터럴을 읽는다).
            SetField(rank, "m_ItemsTypes", EmptyArrayOf(rank, "m_ItemsTypes"));
            SetField(rank, "m_ArmorTypes", EmptyArrayOf(rank, "m_ArmorTypes"));
            SetField(rank, "m_FeatureList", EmptyArrayOf(rank, "m_FeatureList"));
            SetField(rank, "m_CustomProgression", EmptyArrayOf(rank, "m_CustomProgression"));
            SetField(rank, "m_AdditionalArchetypes", EmptyArrayOf(rank, "m_AdditionalArchetypes"));
            SetField(rank, "m_Class", EmptyArrayOf(rank, "m_Class"));
            SetField(rank, "m_CustomPropertyList", EmptyArrayOf(rank, "m_CustomPropertyList"));

            // 피해 변형: StandRules.RushDiceByLevel 행마다 하나(러시 주먹과 같은 원장, C4).
            var rows = StandRules.RushDiceByLevel;
            var variants = rows.Select(r => GhostSlashDamage(r)).ToArray();
            var pick = new ContextActionGhostSlashDamage { ByDiceRow = variants.Select(v => new ActionList { Actions = new GameAction[] { v } }).ToArray() };
            // 작열 광선과 같이: 여러 발 중 정밀 피해(급소 공격 등)는 한 번만(ContextActionDisableBonusForDamage — ScorchingRay.jbp 의 같은 액션 값 그대로).
            var disable = new ContextActionDisableBonusForDamage { DisableSneak = true, DisableAdditionalDamage = true, DisableFavoredEnemyDamage = true, DisableAdditionalDice = false };
            var runAction = new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { pick, disable } } };
            Own(pick, GhostSlashAbility, 0);
            Own(disable, GhostSlashAbility, 1);
            for (int i = 0; i < variants.Length; i++) Own(variants[i], GhostSlashAbility, 2 + i);

            var resLogic = new AbilityResourceLogic { Amount = 1, CostIsCustom = true, ResourceCostIncreasingFacts = new List<BlueprintUnitFactReference>(), ResourceCostDecreasingFacts = new List<BlueprintUnitFactReference>() };
            SetField(resLogic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.GhostSlashResource));
            SetField(resLogic, "m_IsSpendResource", true);
            var needStand = new AbilityCasterHasFacts { NeedsAll = true };
            SetField(needStand, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.StandBuff) });

            AddComponents(GhostSlashAbility, deliver, rank, runAction, resLogic, new GhostSlashCost(), needStand);

            // --- 되읽기(시끄럽게 실패) ---
            var dp = GhostSlashAbility.ComponentsArray.OfType<AbilityDeliverProjectile>().ToArray();
            if (dp.Length != 1 || !dp[0].NeedAttackRoll || !dp[0].ReplaceAttackRollBonusStat || dp[0].AttackRollBonusStat != StatType.Charisma || !dp[0].UseMaxProjectilesCount
                || dp[0].MaxProjectilesCountRank != AbilityRankType.Default || dp[0].Type != AbilityProjectileType.Simple || dp[0].Weapon != ray)
                throw new InvalidOperationException("PhantomBlade: GhostSlash AbilityDeliverProjectile readback failed (one component, attack roll, Charisma, rank count, Simple, RayItem)");
            var projs = dp[0].Projectiles;
            if (projs.Length != GhostSlash.MaxProjectiles || projs.Any(x => x != projectile)) throw new InvalidOperationException("PhantomBlade: GhostSlash m_Projectiles readback is not " + GhostSlash.MaxProjectiles + " × RayOfEnfeeblement00");
            var rk = GhostSlashAbility.ComponentsArray.OfType<ContextRankConfig>().ToArray();
            if (rk.Length != 1 || (AbilityRankType)GetF(rk[0], "m_Type") != AbilityRankType.Default || (ContextRankBaseValueType)GetF(rk[0], "m_BaseValueType") != ContextRankBaseValueType.CustomProperty
                || (ContextRankProgression)GetF(rk[0], "m_Progression") != ContextRankProgression.AsIs || (bool)GetF(rk[0], "m_UseMax") || (bool)GetF(rk[0], "m_UseMin")
                || ((BlueprintUnitPropertyReference)GetF(rk[0], "m_CustomProperty")).Get() != GhostSlashProjectilesProperty)
                throw new InvalidOperationException("PhantomBlade: GhostSlash ContextRankConfig readback failed (Default rank = CustomProperty GhostSlashProjectilesProperty, AsIs, no min/max)");
            var rl = GhostSlashAbility.ComponentsArray.OfType<AbilityResourceLogic>().ToArray();
            var calc = GhostSlashAbility.ComponentsArray.OfType<IAbilityResourceCostCalculator>().ToArray();
            if (rl.Length != 1 || rl[0].RequiredResource != GhostSlashResource || !rl[0].IsSpendResource || !rl[0].CostIsCustom || calc.Length != 1 || !(calc[0] is GhostSlashCost))
                throw new InvalidOperationException("PhantomBlade: GhostSlash AbilityResourceLogic readback failed (one logic spending GhostSlashResource with CostIsCustom, one GhostSlashCost)");
            // 세 자원은 서로 다른 블루프린트여야 한다(풀을 깎지 않고, 전력 공격 횟수와 따로 센다 — 설계 §14 ③).
            if (PoolResource == null || FullAttackResource == null || ReferenceEquals(GhostSlashResource, PoolResource) || ReferenceEquals(GhostSlashResource, FullAttackResource) || ReferenceEquals(rl[0].RequiredResource, PoolResource))
                throw new InvalidOperationException("PhantomBlade: GhostSlashResource must be a resource of its own (not the absorb pool, not the full-attack uses)");
            var hf = GhostSlashAbility.ComponentsArray.OfType<AbilityCasterHasFacts>().ToArray();
            if (hf.Length != 1 || hf[0].Facts.Count() != 1 || hf[0].Facts.First() != StandBuff || !hf[0].NeedsAll) throw new InvalidOperationException("PhantomBlade: GhostSlash AbilityCasterHasFacts(StandBuff) readback failed");
            var ra = GhostSlashAbility.ComponentsArray.OfType<AbilityEffectRunAction>().ToArray();
            if (ra.Length != 1 || ra[0].Actions.Actions.Length != 2 || ra[0].Actions.Actions[0] != pick || ra[0].Actions.Actions[1] != disable || pick.ByDiceRow.Length != rows.Count)
                throw new InvalidOperationException("PhantomBlade: GhostSlash AbilityEffectRunAction readback failed");
            for (int i = 0; i < rows.Count; i++)
            {
                var d = pick.ByDiceRow[i].Actions.Length == 1 ? pick.ByDiceRow[i].Actions[0] as ContextActionDealDamage : null;
                if (d == null || (int)d.Value.DiceType != rows[i].Size || d.Value.DiceCountValue.Value != rows[i].Count || d.DamageType.Type != DamageType.Force || d.IgnoreCritical
                    || d.Value.BonusValue.ValueType != ContextValueType.CasterProperty || d.Value.BonusValue.Property != UnitProperty.StatBonusCharisma)
                    throw new InvalidOperationException("PhantomBlade: GhostSlash damage variant " + i + " readback is not " + rows[i].Count + "d" + rows[i].Size + " + Charisma force (crits on)");
            }

            // --- 특성 사슬: 5레벨 특성이 능력을 주고, 10·15·20레벨 특성은 투사체 수 단계의 설명용(수는 레벨에서 — GhostSlashProjectilesGetter) ---
            GhostSlash5 = MakeFeature("PhantomBladeGhostSlash5", Guids.GhostSlashFeature5, "GhostSlash5.Name", "GhostSlash5.Description", icon);
            GhostSlash10 = MakeFeature("PhantomBladeGhostSlash10", Guids.GhostSlashFeature10, "GhostSlash10.Name", "GhostSlash10.Description", icon);
            GhostSlash15 = MakeFeature("PhantomBladeGhostSlash15", Guids.GhostSlashFeature15, "GhostSlash15.Name", "GhostSlash15.Description", icon);
            GhostSlash20 = MakeFeature("PhantomBladeGhostSlash20", Guids.GhostSlashFeature20, "GhostSlash20.Name", "GhostSlash20.Description", icon);
            if (GhostSlash5.name != "PhantomBladeGhostSlash" + GhostSlash.FirstLevel) throw new InvalidOperationException("PhantomBlade: the ability-granting ghost slash feature " + GhostSlash5.name + " is not the GhostSlash.FirstLevel feature");
            // 5레벨 특성 = 능력 + 전용 횟수 자원. 전력 공격 특성(FullAttackFeature)과 같은 세 도우미: Facts · Resources(처음 얻을 때·레벨업 때 가득) · Increase(최대치 += ChaMin1Property).
            //   최대치는 질의 때마다 계산된다(BlueprintAbilityResource.GetMaxAmount → IResourceAmountBonusHandler) → 매력 수정치가 바뀌면 따라 바뀐다(전력 공격과 같음).
            AddComponents(GhostSlash5,
                Facts(GhostSlashAbility),
                Resources(GhostSlashResource),
                Increase(GhostSlashResource, ChaMin1Property));
            if (GhostSlash5.ComponentsArray.OfType<AddAbilityResources>().Count(c => c.Resource == GhostSlashResource) != 1
                || GhostSlash5.ComponentsArray.OfType<IncreaseResourceAmountBySharedValue>().Count(c => c.Resource == GhostSlashResource) != 1)
                throw new InvalidOperationException("PhantomBlade: GhostSlash5 resource components readback failed (one AddAbilityResources + one IncreaseResourceAmountBySharedValue for GhostSlashResource)");
        }

        // 작열 광선 jbp 의 DelayBetweenProjectiles(Spells/Level2/ScorchingRay.jbp 「0.2」) — 연사 연출 간격(초). 규칙 영향 없음.
        const float ScorchingRayDelaySeconds = 0.2f;

        // 투사체 한 발의 피해(본편 ContextActionDealDamage). 주사위 = 표 행, 보너스 = 시전자 매력 수정치, 역장, 치명타 허용.
        // 플래그는 작열 광선(ScorchingRay.jbp)의 ContextActionDealDamage 값 그대로(추가 피해·선호 적 피해 허용, 무기 피해 수정치 없음, 치명타 허용).
        static ContextActionDealDamage GhostSlashDamage(StandRules.RushDiceRow row)
        {
            if (!Enum.IsDefined(typeof(DiceType), row.Size)) throw new InvalidOperationException("PhantomBlade: d" + row.Size + " is not a DiceType");
            var d = new ContextActionDealDamage
            {
                DamageType = new DamageTypeDescription { Type = DamageType.Force },
                Duration = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = SimpleValue(0), BonusValue = SimpleValue(0) },
                Value = new ContextDiceValue
                {
                    DiceType = (DiceType)row.Size,
                    DiceCountValue = SimpleValue(row.Count),
                    BonusValue = new ContextValue { ValueType = ContextValueType.CasterProperty, Property = UnitProperty.StatBonusCharisma },
                },
                IgnoreCritical = false,
                AddAdditionalDamage = true,
                AddFavoredEnemyDamage = true,
                UseWeaponDamageModifiers = false,
            };
            SetField(d, "m_Type", EnumOf(d, "m_Type", "Damage"));
            return d;
        }

        static ContextValue SimpleValue(int v) { return new ContextValue { ValueType = ContextValueType.Simple, Value = v }; }

        // 귀신참 특성을 GhostSlash.ProjectilesByLevel 의 레벨에 단다(C4: 레벨 리터럴 없음). 이름 "PhantomBladeGhostSlash<레벨>" 이 행과 다르면 던진다.
        // 5·10·15·20 은 본편 전사 특기·무기 훈련·무기 숙달 자리이고 그 특성들은 그대로 둔다(설계 §4.1 유지 목록) — 귀신참은 더해질 뿐이다(제거 없음).
        static void AddGhostSlashFeatures(BlueprintFeature[] features)
        {
            var rows = GhostSlash.ProjectilesByLevel;
            if (features.Length != rows.Count) throw new InvalidOperationException("PhantomBlade: " + features.Length + " ghost slash features for " + rows.Count + " GhostSlash.ProjectilesByLevel rows");
            for (int i = 0; i < rows.Count; i++)
            {
                var f = features[i];
                if (f == null) throw new InvalidOperationException("PhantomBlade: ghost slash feature " + i + " is null (BuildGhostSlash not run)");
                if (f.name != "PhantomBladeGhostSlash" + rows[i].Level) throw new InvalidOperationException("PhantomBlade: ghost slash feature " + f.name + " does not match table row level " + rows[i].Level);
                Archetype.AddAt(rows[i].Level, f);
            }
        }

        // 투사체가 쓰는 자원(본편 BlueprintAbility.ResourceAssetIds 처럼 미리 읽기 목록). 본편 값을 옮겨 적지 않고 투사체 블루프린트에서 읽는다(C4).
        // View 는 필수(없으면 투사체가 안 보인다 — 던짐). 시전·명중·빗나감 FX 는 투사체마다 있을 수도 없을 수도 있다(정당한 결측 — 미리 읽을 것이 없을 뿐).
        static string[] ProjectileAssetIds(BlueprintProjectile p)
        {
            if (p.View == null || string.IsNullOrEmpty(p.View.AssetId)) throw new InvalidOperationException("PhantomBlade: projectile " + p.name + " has no View asset");
            var ids = new List<string> { p.View.AssetId };
            var links = new List<WeakResourceLink> { p.CastFx };
            if (p.ProjectileHit != null) links.AddRange(new WeakResourceLink[] { p.ProjectileHit.HitFx, p.ProjectileHit.HitSnapFx, p.ProjectileHit.MissFx, p.ProjectileHit.MissDecalFx });
            foreach (var l in links) if (l != null && !string.IsNullOrEmpty(l.AssetId)) ids.Add(l.AssetId);
            return ids.Distinct().ToArray();
        }

        static object EmptyArrayOf(object o, string field)
        {
            var f = FindField(o.GetType(), field);
            if (f == null) throw new MissingFieldException("PhantomBlade: " + o.GetType().Name + "." + field + " not found");
            if (!f.FieldType.IsArray) throw new InvalidOperationException("PhantomBlade: " + o.GetType().Name + "." + field + " is not an array");
            return Array.CreateInstance(f.FieldType.GetElementType(), 0);
        }

        // 러시 특성을 StandRules.RushTable 의 레벨에 단다(Task 6b, C4: 레벨 리터럴 없음). 특성 i 는 표 행 i 이고, 이름 "PhantomBladeRush<레벨>" 이 그 행 레벨과
        // 다르면(표를 바꾸고 특성을 안 바꿈) 던진다 — 조용히 엉뚱한 레벨에 붙이지 않는다.
        static void AddRushFeatures(BlueprintFeature[] features)
        {
            var rows = StandRules.RushTable;
            if (features.Length != rows.Count) throw new InvalidOperationException("PhantomBlade: " + features.Length + " rush features for " + rows.Count + " RushTable rows");
            for (int i = 0; i < rows.Count; i++)
            {
                var f = features[i];
                if (f == null) throw new InvalidOperationException("PhantomBlade: rush feature " + i + " is null (MakeFeature not run)");
                if (f.name != "PhantomBladeRush" + rows[i].Level) throw new InvalidOperationException("PhantomBlade: rush feature " + f.name + " does not match RushTable row level " + rows[i].Level);
                Archetype.AddAt(rows[i].Level, f);
            }
        }

         
        // - StandBuff: 모든 근접 공격(Task 6d 부터 빗나감 포함, OnlyOnFirstHit=false) → Conditional[시전자가 Rush3 보유 AND 전력 공격 버프 미보유] → 타격.
         
        //   첫 공격이 빗나가면 이전 HadHit 가 남아 러시를 잃고(IsSuitable IL 0054~006A 가 031D~0334 초기화 전에 반환), 기회공격 뒤 새 시퀀스에서 두 번 친다.
        //   Rush3 조건: 설계 §4.3 성장표 — 러시는 3레벨부터. 스탠드 버프는 2레벨(토글, §4.4)부터 붙으므로 조건이 없으면 2레벨의 첫 명중마다 타격 액션이
        //   StandOwner.Level(…, RushLevel) / StandRules.MayStrike 로 던진다. 그래서 Rush3 미보유면 타격하지 않는다(설계상 2레벨엔 러시가 없다).
        //   전력 공격 조건: 전력 공격 라운드엔 아래 FullAttackBuff 트리거가 첫 명중도 처리하므로 여기서 또 치면 한 명중에 타격이 두 벌이 된다.
        // - FullAttackBuff: 같은 트리거이되 OnlyOnFirstHit=false(모든 근접 명중, 설계 §4.3 「스탠드 전력 공격」) → Conditional[시전자가 StandBuff 보유] → 타격.
        //   설계 §4.2: 토글을 끄면 스탠드 효과는 없다. 라운드 도중 토글을 꺼도 타격이 멈추도록 액션 쪽에서 막는다.
        //   FullAttackAbility 에는 AbilityCasterHasFacts[StandBuff] 를 달아 스탠드 없이는 쓸 수 없게 한다(사용 횟수 낭비 방지).
        //   AbilityCasterHasFacts.IsCasterRestrictionPassed IL: 사실 하나라도 없으면(NeedsAll) false, 목록이 비면 true → 되읽기로 1개 확인.
        // - 둘 다 NotExtraAttack=true: 타격 규칙은 ExtraAttack=true 라서 이 트리거를 다시 부르지 못한다(IsSuitable IL 03BB, 재귀 차단).
        // - ActionsOnInitiator=false: 액션 대상 = 공격받은 유닛(RunActions IL 000B~0018). ContextActionStandStrike 가 그것을 Target 으로 읽는다.
        // - Task 6d(귀신 접촉, 설계 §13.3): 둘 다 OnlyHit=false — 10레벨부터는 빗나간 공격도 스탠드 접촉 굴림을 부른다. 엔진 근거:
        //   근접(투사체 없음) 공격은 명중 여부와 무관하게 해결 규칙을 만들어 발동하고(RuleAttackWithWeapon.LaunchProjectiles(ctx) IL 001D~005C),
        //   DidTrigger 가 해결 규칙마다 RunActions 를 부르며(IL 0043~008C), 명중 거름은 OnlyHit 일 때만이다(IsSuitable IL 0054~006A).
        //   RunActions 는 그 굴림을 ContextAttackData 로 열어 둔다(IL 0021~0037) → 타격 액션이 명중 여부를 읽고, 「10 미만 = 명중에만」은 순수 StandRules.PlanStrike 가 정한다.
        //   OnMiss·TriggerBeforeAttack 은 건드리지 않는다(거짓 — 굴림 뒤에 돈다). 기회 공격: 걸러내지 않는다(종전 러시와 같은 정책 — 그 라운드 타수를 쓴다).
        static void BuildRushTriggers()
        {
            if (StandBuff == null || FullAttackBuff == null || FullAttackAbility == null || Rush3 == null) throw new InvalidOperationException("PhantomBlade: BuildRushTriggers before StandBuff/FullAttackBuff/FullAttackAbility/Rush3 exist");

            var hasRush = new ContextConditionCasterHasFact { Not = false };
            SetField(hasRush, "m_Fact", Ref<BlueprintUnitFactReference>(Guids.RushFeature3));
            var noFullAttack = new ContextConditionCasterHasFact { Not = true };
            SetField(noFullAttack, "m_Fact", Ref<BlueprintUnitFactReference>(Guids.FullAttackBuff));
            if (hasRush.Fact != Rush3 || hasRush.Not) throw new InvalidOperationException("PhantomBlade: ContextConditionCasterHasFact(Rush3) readback failed");
            if (noFullAttack.Fact != FullAttackBuff || !noFullAttack.Not) throw new InvalidOperationException("PhantomBlade: ContextConditionCasterHasFact(not FullAttackBuff) readback failed");

            var firstHitStrike = new ContextActionStandStrike { StrikesOverride = 0 };
            var gate = new Conditional
            {
                ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { hasRush, noFullAttack } },
                IfTrue = new ActionList { Actions = new GameAction[] { firstHitStrike } },
                IfFalse = new ActionList { Actions = new GameAction[0] }, // 본편 블루프린트는 두 목록을 늘 갖는다(null 금지)
            };
            var standTrigger = new AddInitiatorAttackWithWeaponTrigger
            {
                OnlyHit = false, // Task 6d: 빗나감도 액션에 온다(10 이상 접촉 굴림). 10 미만 빗나감은 PlanStrike 가 거부.
                CheckWeaponRangeType = true,
                RangeType = WeaponRangeType.Melee,
                OnlyOnFirstHit = false, // 라운드 제한은 StandRushState(F1)
                NotExtraAttack = true,
                ActionsOnInitiator = false,
                Action = new ActionList { Actions = new GameAction[] { gate } },
            };

            var hasStand = new ContextConditionCasterHasFact { Not = false };
            SetField(hasStand, "m_Fact", Ref<BlueprintUnitFactReference>(Guids.StandBuff));
            if (hasStand.Fact != StandBuff || hasStand.Not) throw new InvalidOperationException("PhantomBlade: ContextConditionCasterHasFact(StandBuff) readback failed");
            var everyHitStrike = new ContextActionStandStrike { StrikesOverride = 0 };
            var fullGate = new Conditional
            {
                ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { hasStand } },
                IfTrue = new ActionList { Actions = new GameAction[] { everyHitStrike } },
                IfFalse = new ActionList { Actions = new GameAction[0] },
            };
            var fullTrigger = new AddInitiatorAttackWithWeaponTrigger
            {
                OnlyHit = false, // Task 6d: 전력 공격(15레벨 ≥ TouchLevel)은 공격마다(빗나가도) 접촉 2타.
                CheckWeaponRangeType = true,
                RangeType = WeaponRangeType.Melee,
                OnlyOnFirstHit = false,
                NotExtraAttack = true,
                ActionsOnInitiator = false,
                Action = new ActionList { Actions = new GameAction[] { fullGate } },
            };

            var needStand = new AbilityCasterHasFacts { NeedsAll = true };
            SetField(needStand, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.StandBuff) });
            if (needStand.Facts.Count() != 1 || needStand.Facts.First() != StandBuff) throw new InvalidOperationException("PhantomBlade: AbilityCasterHasFacts(StandBuff) readback failed");

             
            Own(gate, StandBuff, 0);
            Own(hasRush, StandBuff, 1);
            Own(noFullAttack, StandBuff, 2);
            Own(firstHitStrike, StandBuff, 3);
            Own(fullGate, FullAttackBuff, 0);
            Own(hasStand, FullAttackBuff, 1);
            Own(everyHitStrike, FullAttackBuff, 2);

            AddComponents(StandBuff, standTrigger);
            AddComponents(FullAttackBuff, fullTrigger);
            AddComponents(FullAttackAbility, needStand);
            if (FullAttackAbility.ComponentsArray.OfType<AbilityCasterHasFacts>().Count() != 1) throw new InvalidOperationException("PhantomBlade: FullAttackAbility caster restriction readback failed");

            // 되읽기: 컴포넌트가 실제로 버프에 달렸고 재귀 차단·첫 명중 플래그·액션 연결이 의도대로인지.
            var st = StandBuff.ComponentsArray.OfType<AddInitiatorAttackWithWeaponTrigger>().ToArray();
            var ft = FullAttackBuff.ComponentsArray.OfType<AddInitiatorAttackWithWeaponTrigger>().ToArray();
            if (st.Length != 1 || ft.Length != 1) throw new InvalidOperationException("PhantomBlade: expected one rush trigger per buff (StandBuff " + st.Length + ", FullAttackBuff " + ft.Length + ")");
            if (!st[0].NotExtraAttack || !ft[0].NotExtraAttack) throw new InvalidOperationException("PhantomBlade: rush trigger NotExtraAttack readback is false (recursion)");
            if (st[0].OnlyOnFirstHit || ft[0].OnlyOnFirstHit) throw new InvalidOperationException("PhantomBlade: rush trigger OnlyOnFirstHit readback (both must be false; the round limit is StandRushState)");
            if (st[0].OnlyHit || ft[0].OnlyHit || st[0].OnMiss || ft[0].OnMiss || st[0].TriggerBeforeAttack || ft[0].TriggerBeforeAttack)
                throw new InvalidOperationException("PhantomBlade: rush trigger OnlyHit/OnMiss/TriggerBeforeAttack readback (all must be false; misses reach the action after the roll — Task 6d)");
            if (st[0].Action.Actions.Length != 1 || st[0].Action.Actions[0] != gate || gate.IfTrue.Actions.Length != 1 || gate.IfTrue.Actions[0] != firstHitStrike || gate.IfFalse == null || gate.IfFalse.Actions == null)
                throw new InvalidOperationException("PhantomBlade: StandBuff rush trigger action readback failed");
            if (ft[0].Action.Actions.Length != 1 || ft[0].Action.Actions[0] != fullGate || fullGate.IfTrue.Actions.Length != 1 || fullGate.IfTrue.Actions[0] != everyHitStrike || fullGate.IfFalse == null || fullGate.IfFalse.Actions == null) throw new InvalidOperationException("PhantomBlade: FullAttackBuff rush trigger action readback failed");
        }

        static void Own(Element e, BlueprintScriptableObject owner, int index)
        {
            e.name = "$" + e.GetType().Name + "$" + owner.AssetGuid + "#e" + index;
            e.Owner = owner;
            if (!ReferenceEquals(e.Owner, owner)) throw new InvalidOperationException("PhantomBlade: Element.Owner readback failed for " + e.name);
        }

        // 스탠드 주먹 무기 목록 — 단일 원장(C4). FistFor·IsStandFist·점검 기록기(Checks/StrikeLog)·령 동작(StandSpiritAnimator)이 모두 이것을 읽는다.
        // 순서 = StandRules.RushDiceByLevel 행 순서(BuildWeapons 가 대조). Build 전에 읽으면 던진다(null 이 섞인 목록을 돌려주지 않음).
        public static BlueprintItemWeapon[] StandFists
        {
            get
            {
                var fists = new[] { Fist1d6, Fist1d8, Fist1d10, Fist2d6, Fist2d8, Fist2d10 };
                if (fists.Any(w => w == null)) throw new InvalidOperationException("PhantomBlade: StandFists read before StandBlueprints.Build()");
                return fists;
            }
        }

        // 이 무기 블루프린트가 스탠드 주먹인가(참조 비교). null 은 판정할 수 없다 — 던진다.
        public static bool IsStandFist(BlueprintItemWeapon weapon)
        {
            if (weapon == null) throw new ArgumentNullException("weapon");
            return StandFists.Any(w => ReferenceEquals(w, weapon));
        }

        // 그 주사위의 러시 주먹 무기(만들어진 블루프린트의 실제 주사위로 찾는다). 맞는 것이 없으면 실패.
        // fix 1: 주사위는 호출자가 순수 계획(StandRules.PlanStrike)에서 받아 넘긴다 — 여기서는 단일 주먹 목록에서 그 주사위의 무기를 찾기만 한다.
        public static BlueprintItemWeapon FistFor(int n, int d)
        {
            var fists = StandFists;
            var match = fists.Where(w => w.BaseDamage.Rolls == n && (int)w.BaseDamage.Dice == d).ToArray();
            if (match.Length != 1) throw new InvalidOperationException("PhantomBlade: FistFor(" + n + "d" + d + ") found " + match.Length + " fist weapons");
            return match[0];
        }

        static void BuildWeapons()
        {
            Get<BlueprintWeaponType>(Guids.Game.ClawType); // 존재 확인(없으면 Get 이 던진다)
            var template = Get<BlueprintItemWeapon>(Guids.Game.ForceWispTouchWeapon);
            // 정찰 §3: 템플릿 ForceWispTouchWeapon 의 m_Icon 은 null(blueprints.zip 2026-11-07). 그래서 아이콘은 설정하지 않고
            // BlueprintItemWeapon.Icon 의 형식 폴백(ClawType.Icon, IL 확인)을 쓴다 — MakeFist 에서 null 이 아님을 확인.
            var visual = GetF(template, "m_VisualParameters");
            if (visual == null) throw new InvalidOperationException("PhantomBlade: ForceWispTouchWeapon.m_VisualParameters is null");
            Fist1d6 = MakeFist("PhantomBladeFist1d6", Guids.StandFist1d6, 1, 6, visual);
            Fist1d8 = MakeFist("PhantomBladeFist1d8", Guids.StandFist1d8, 1, 8, visual);
            Fist1d10 = MakeFist("PhantomBladeFist1d10", Guids.StandFist1d10, 1, 10, visual);
            Fist2d6 = MakeFist("PhantomBladeFist2d6", Guids.StandFist2d6, 2, 6, visual);
            Fist2d8 = MakeFist("PhantomBladeFist2d8", Guids.StandFist2d8, 2, 8, visual);
            Fist2d10 = MakeFist("PhantomBladeFist2d10", Guids.StandFist2d10, 2, 10, visual);
            // 단일 원장 대조(C4): 주먹 목록의 주사위 순서 == StandRules.RushDiceByLevel 행 순서. 표에 단계를 더하거나 바꾸고 주먹을 안 고치면 빌드가 실패한다.
            var table = StandRules.RushDiceByLevel;
            var fists = StandFists;
            string want = string.Join(",", table.Select(r => r.Count + "d" + r.Size).ToArray());
            string got = string.Join(",", fists.Select(w => w.BaseDamage.Rolls + "d" + (int)w.BaseDamage.Dice).ToArray());
            if (want != got) throw new InvalidOperationException("PhantomBlade: stand fists [" + got + "] do not match StandRules.RushDiceByLevel [" + want + "]");
        }

        // 주사위는 인자로 받고, 이름("…<n>d<d>")과 어긋나면 실패한다(이름이 거짓말이 되지 않게). 표와의 대조는 BuildWeapons 가 목록 전체로 한다.
        static BlueprintItemWeapon MakeFist(string name, string guid, int n, int d, object visual)
        {
            if (!name.EndsWith(n + "d" + d)) throw new InvalidOperationException("PhantomBlade: fist " + name + " is not named for " + n + "d" + d);
            if (!Enum.IsDefined(typeof(DiceType), d)) throw new InvalidOperationException("PhantomBlade: d" + d + " is not a DiceType");
            var fistName = Strings.L("Fist.Name");
            var fistDescription = Strings.L("Fist.Description");
            var w = Create<BlueprintItemWeapon>(name, guid, x =>
            {
                SetField(x, "m_Type", Ref<BlueprintWeaponTypeReference>(Guids.Game.ClawType));
                SetField(x, "m_Size", Size.Medium); // 템플릿 ForceWispTouchWeapon 과 같음(zip). 생성자 기본값도 Medium(.ctor IL: m_Size=4)이지만 명시해 되읽는다.
                SetField(x, "m_Enchantments", new BlueprintWeaponEnchantmentReference[0]); // get_IsMagic/IsMasterwork 가 null 검사 없이 ldlen(IL) → 빈 배열
                SetField(x, "m_OverrideDamageDice", true);
                SetField(x, "m_DamageDice", new DiceFormula(n, (DiceType)d));
                SetField(x, "m_OverrideDamageType", true);
                SetField(x, "m_DamageType", new DamageTypeDescription { Type = DamageType.Force });
                SetField(x, "m_DisplayNameText", fistName);
                SetField(x, "m_DescriptionText", fistDescription);
                // 2026-11-17(툴팁 `<null>`·예외 결함): 나머지 BlueprintItem 문구 칸 3개 = 빈 키(본편 자연 무기 관례 — 템플릿 ForceWispTouchWeapon · Claw1d6 · StoneFistMedium 의 jbp 모두 빈 키, 시험 tooltip: jbp: 가 대조).
                //   null 이면 get_FlavorText 는 "<null>", get_NonIdentifiedName/Description 은 null 에 IsSet() 을 불러 NRE(@0006). 빈 키면 IsSet 거짓 → 무기 형식의 기본 미감정 이름·설명(본편과 같음).
                SetField(x, "m_FlavorText", NoText());
                SetField(x, "m_NonIdentifiedNameText", NoText());
                SetField(x, "m_NonIdentifiedDescriptionText", NoText());
                SetField(x, "m_VisualParameters", visual);
            });
            // 최종 리뷰 M4: 이름·설명·시각 파라미터 되읽기(SetField 가 다른 필드에 썼거나 Create 가 덮었으면 실패).
            if (!ReferenceEquals(GetF(w, "m_DisplayNameText"), fistName) || !ReferenceEquals(GetF(w, "m_DescriptionText"), fistDescription) || !ReferenceEquals(GetF(w, "m_VisualParameters"), visual))
                throw new InvalidOperationException("PhantomBlade: " + name + " m_DisplayNameText / m_DescriptionText / m_VisualParameters readback failed");
            if (w.Type == null || w.Type.AssetGuid != BlueprintGuid.Parse(Guids.Game.ClawType)) throw new InvalidOperationException("PhantomBlade: " + name + ".Type readback is not ClawType");
            if (w.BaseDamage.Rolls != n || (int)w.BaseDamage.Dice != d) throw new InvalidOperationException("PhantomBlade: " + name + ".BaseDamage readback " + w.BaseDamage.Rolls + "d" + (int)w.BaseDamage.Dice);
            if (w.DamageType == null || w.DamageType.Type != DamageType.Force) throw new InvalidOperationException("PhantomBlade: " + name + ".DamageType readback is not Force");
            if (w.Size != Size.Medium) throw new InvalidOperationException("PhantomBlade: " + name + ".Size readback is not Medium");
            var ench = GetF(w, "m_Enchantments") as BlueprintWeaponEnchantmentReference[];
            if (ench == null || ench.Length != 0 || w.IsMagic || w.IsMasterwork) throw new InvalidOperationException("PhantomBlade: " + name + ".m_Enchantments readback is not an empty array");
            if (w.Icon == null) throw new InvalidOperationException("PhantomBlade: " + name + ".Icon is null (ClawType fallback missing)");
            return w;
        }

        static void BuildProperties(BlueprintCharacterClass fighter)
        {
            // ChaMin1Property = max(1, 매력 수정치). PropertyValueGetter.GetValue 가 게터마다 Settings 를 적용한다(IL 확인) → 게터에 Min=1.
            ChaMin1Property = Create<BlueprintUnitProperty>("PhantomBladeChaMin1Property", Guids.ChaMin1Property, p =>
            {
                p.BaseValue = 0; // Sum: BaseValue + Σ게터
                p.OperationOnComponents = BlueprintUnitProperty.MathOperation.Sum;
            });
            AddComponents(ChaMin1Property, new SimplePropertyGetter { Property = UnitProperty.StatBonusCharisma, Settings = MinOne() });

            // PoolProperty = PoolMultiplier × 귀검사 전사 레벨 × ChaMin1Property (설계 §4.4 + §14 ② = StandRules.PoolMax). GetInt IL: Multiply 는 BaseValue 에서 시작해 곱한다
            //   → 곱셈 시작값 BaseValue 가 곧 배수다(종전 1). 배수는 StandRules.PoolMultiplier 한 곳(소유자 결정 2026-11-16: 전투 뒤 회복을 없애는 대신 3배).
            PoolProperty = Create<BlueprintUnitProperty>("PhantomBladePoolProperty", Guids.StandPoolProperty, p =>
            {
                p.BaseValue = StandRules.PoolMultiplier;
                p.OperationOnComponents = BlueprintUnitProperty.MathOperation.Multiply;
            });
            if (PoolProperty.BaseValue != StandRules.PoolMultiplier || PoolProperty.OperationOnComponents != BlueprintUnitProperty.MathOperation.Multiply)
                throw new InvalidOperationException("PhantomBlade: PoolProperty readback failed (BaseValue " + PoolProperty.BaseValue + ", expected the multiplier " + StandRules.PoolMultiplier + ", Multiply)");
            var level = new ClassLevelGetter();
            SetField(level, "m_Class", Ref<BlueprintCharacterClassReference>(GameGuids.FighterClass));
            SetField(level, "m_Archetype", Ref<BlueprintArchetypeReference>(Guids.Archetype));
            var cha = new CustomPropertyGetter();
            SetField(cha, "m_Property", Ref<BlueprintUnitPropertyReference>(Guids.ChaMin1Property));
            AddComponents(PoolProperty, level, cha);
            if (level.Class != fighter || level.Archetype != Archetype.Bp) throw new InvalidOperationException("PhantomBlade: ClassLevelGetter readback failed");
            if (cha.Property != ChaMin1Property) throw new InvalidOperationException("PhantomBlade: CustomPropertyGetter readback failed");
        }

        static PropertySettings MinOne()
        {
            // PropertySettings.LimitType 는 비공개 중첩 열거형(dnlib) → 필드 형식에서 이름으로 값을 얻는다(없는 이름이면 Enum.Parse 가 던진다).
            var s = new PropertySettings();
            SetField(s, "m_LimitType", EnumOf(s, "m_LimitType", "Min"));
            SetField(s, "m_Min", 1);
            // Apply 공개 메서드로 되읽기: 음수는 1 로, 양수는 그대로(최대 제한 없음).
            if (s.Apply(-3) != 1 || s.Apply(0) != 1 || s.Apply(4) != 4 || s.Apply(1000) != 1000) throw new InvalidOperationException("PhantomBlade: PropertySettings Min=1 readback failed");
            return s;
        }

        static BlueprintAbilityResource MakeResource(string name, string guid, string nameKey, string descKey, Sprite icon)
        {
            var res = Create<BlueprintAbilityResource>(name, guid, r =>
            {
                r.LocalizedName = Strings.L(nameKey);
                r.LocalizedDescription = Strings.L(descKey);
                SetField(r, "m_Icon", icon);
                if (!ReferenceEquals(GetF(r, "m_Icon"), icon)) throw new InvalidOperationException("PhantomBlade: " + name + ".m_Icon readback failed");
                // Amount 는 비공개 중첩 구조체(dnlib): 상자에 담아 BaseValue 0 과 빈 배열(본편 JSON 의 [] 와 같게)을 채운 뒤 통째로 넣는다.
                object amount = GetF(r, "m_MaxAmount"); // 새 블루프린트의 기본값을 상자에 담은 복사본
                SetField(amount, "BaseValue", 0);
                SetField(amount, "m_Class", new BlueprintCharacterClassReference[0]);
                SetField(amount, "m_Archetypes", new BlueprintArchetypeReference[0]);
                SetField(amount, "m_ClassDiv", new BlueprintCharacterClassReference[0]);
                SetField(amount, "m_ArchetypesDiv", new BlueprintArchetypeReference[0]);
                SetField(r, "m_MaxAmount", amount);
            });
            var back = GetF(res, "m_MaxAmount");
            if ((int)GetF(back, "BaseValue") != 0 || (bool)GetF(back, "IncreasedByLevel") || (bool)GetF(back, "IncreasedByStat"))
                throw new InvalidOperationException("PhantomBlade: " + name + ".m_MaxAmount readback is not a plain BaseValue 0");
            // 최종 리뷰 M4: 네 배열이 빈 배열(null 아님)로 들어갔는지(본편 JSON 의 [] 와 같게).
            foreach (var arr in new[] { "m_Class", "m_Archetypes", "m_ClassDiv", "m_ArchetypesDiv" })
            {
                var a = GetF(back, arr) as Array;
                if (a == null || a.Length != 0) throw new InvalidOperationException("PhantomBlade: " + name + ".m_MaxAmount." + arr + " readback is " + (a == null ? "null" : "length " + a.Length) + ", expected an empty array");
            }
            return res;
        }

        static BlueprintFeature MakeFeature(string name, string guid, string nameKey, string descKey, Sprite icon)
        {
            return Create<BlueprintFeature>(name, guid, f =>
            {
                Names(f, nameKey, descKey, icon);
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
        }

        static void Names(BlueprintUnitFact f, string nameKey, string descKey, Sprite icon)
        {
            if (icon == null) throw new ArgumentNullException("icon");
            var displayName = Strings.L(nameKey);
            var description = Strings.L(descKey);
            var descriptionShort = Strings.L(descKey);
            SetField(f, "m_DisplayName", displayName);
            SetField(f, "m_Description", description);
            SetField(f, "m_DescriptionShort", descriptionShort);
            SetField(f, "m_Icon", icon);
            // 최종 리뷰 M4: 네 필드 되읽기(같은 객체가 들어갔는지).
            if (!ReferenceEquals(GetF(f, "m_DisplayName"), displayName) || !ReferenceEquals(GetF(f, "m_Description"), description)
                || !ReferenceEquals(GetF(f, "m_DescriptionShort"), descriptionShort) || !ReferenceEquals(GetF(f, "m_Icon"), icon))
                throw new InvalidOperationException("PhantomBlade: " + f.name + " name/description/icon readback failed");
        }

        static AddFacts Facts(BlueprintUnitFact fact)
        {
            var c = new AddFacts();
            SetField(c, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(fact.AssetGuid.ToString()) });
            if (c.Facts.Count() != 1 || c.Facts.First() != fact) throw new InvalidOperationException("PhantomBlade: AddFacts readback failed for " + fact.name);
            return c;
        }

        // 자원은 AddAbilityResources 가 있어야 유닛에 생긴다(OnActivate → Owner.Resources.Add(res, RestoreAmount), IL 확인).
        // RestoreAmount 만으로는 처음 얻을 때 0 이다: EntityFact.Activate 가 컴포넌트 OnActivate 를 먼저 다 돌고 TurnOn(구독)은 그 뒤라,
         
        // RestoreOnLevelUp: UnitProgressionData.ReapplyFeaturesOnLevelUp 끝에서 RaiseEvent(IUnitReapplyFeaturesOnLevelUpHandler) →
        // Resources.Restore(res) = 최대치로(IL). 이 함수는 LevelUpController.ApplyLevelUpActions 의 모든 Apply 뒤, AddClassLevels.LevelUp 에서도 불린다.
        // 부작용: 레벨업마다 풀·전력 공격 횟수·귀신참 횟수가 가득 찬다(설명 문구에 적음). 세 자원이 이 도우미 하나를 쓴다 → 레벨업 회복 여부가 서로 같다(설계 §14 ③).
        static AddAbilityResources Resources(BlueprintAbilityResource res)
        {
            var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
            SetField(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(res.AssetGuid.ToString()));
            if (c.Resource != res) throw new InvalidOperationException("PhantomBlade: AddAbilityResources readback failed for " + res.name);
            return c;
        }

        static IncreaseResourceAmountBySharedValue Increase(BlueprintAbilityResource res, BlueprintUnitProperty prop)
        {
            var v = new ContextValue { ValueType = ContextValueType.CasterCustomProperty };
            SetField(v, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(prop.AssetGuid.ToString()));
            var c = new IncreaseResourceAmountBySharedValue { Value = v, Decrease = false };
            SetField(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(res.AssetGuid.ToString()));
            if (c.Resource != res || v.CustomProperty != prop) throw new InvalidOperationException("PhantomBlade: IncreaseResourceAmountBySharedValue readback failed for " + res.name);
            return c;
        }

        static object EnumOf(object o, string field, string member)
        {
            var f = FindField(o.GetType(), field);
            if (f == null) throw new MissingFieldException("PhantomBlade: " + o.GetType().Name + "." + field + " not found");
            if (!f.FieldType.IsEnum) throw new InvalidOperationException("PhantomBlade: " + o.GetType().Name + "." + field + " is not an enum");
            return Enum.Parse(f.FieldType, member);
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("PhantomBlade: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
