using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace BlastMagus
{
    // 폭발(Blast Strike, 설계 §4.2·§4.9·§11-7, 정찰 §7·§8). 4레벨: 신속 행동 + 아케인 풀 1 → 1라운드 버프 → 다음 근접 제작 무기 명중에 블라스트 피해 규칙 1회(원거리·자연·맨손·무기 그룹 없는 형식 제외).
     
    //   - 능력 모양 = 본편 ArcaneAccuracyAbility(정찰 §8). 비용은 AbilityResourceLogic 이 낸다(모드 코드에 Spend 없음).
    //   - SourceAbility = 숨은 능력. 유닛에 주지 않는다. 피해 규칙의 SourceAbility(전투 로그 이름)와 SR 판정의 근거 능력일 뿐이다.
    //     RuleSpellResistanceCheck.get_HasResistanceRoll @0x0066~0x0071 이 Ability.SpellResistance 를 읽으므로 SourceAbility.SpellResistance = true 여야 SR 이 굴러간다.
    // 레벨표 소유(컨트롤러 판정 1): 이 Build 가 4레벨 항목을 직접 AddAt 한다.
    public static class BlastStrike
    {
        public static BlueprintAbility Ability, SourceAbility;
        public static BlueprintBuff Buff;
        public static BlueprintFeature Feature;
        // 랭크 판독용 본편 KineticBlastFeature(BlastBlueprints 가 홀수 레벨마다 준다). Build 가 채운다.
        internal static BlueprintFeature KineticBlast;
        // SR 판정 CL 의 기준(리뷰 수정 1): 원소별 블라스트 능력 = 원소 모델 해석 결과의 잎(계획 T2). 같은 BlastCasterLevel·음수 레벨·CL 보너스를 거친 엔진 자신의 CL 과 대조한다.
         
        static bool referenceChecked;

        // 정찰 §8: ArcaneAccuracyAbility 의 ContextActionApplyBuff.DurationValue = {Rate=Rounds, BonusValue=1} = 1라운드.
        const int DurationRounds = 1;

        public static void Build()
        {
            if (Archetype.Bp == null) throw new InvalidOperationException("BlastMagus: BlastStrike.Build() before Archetype.Build() (AddAt needs the archetype)");
            if (BlastBlueprints.BlastFeature == null) throw new InvalidOperationException("BlastMagus: BlastStrike.Build() before BlastBlueprints.Build() (BlastFeature, caster-level set)");
            if (Buff != null) throw new InvalidOperationException("BlastMagus: BlastStrike.Build() called twice");
            Sprite icon = BlastBlueprints.BlastFeature.Icon;
            if (icon == null) throw new InvalidOperationException("BlastMagus: BlastFeature has no icon (BlastStrike icon source)");
            KineticBlast = Get<BlueprintFeature>(Guids.Game.KineticBlastFeature);
            CheckPhysicalBlastDr(KineticBlast);
            var pool = Get<BlueprintAbilityResource>(Guids.Game.ArcanePoolResourse);
            var referenceBlasts = BlastBlueprints.Resolved.Select(r => (element: r.Row.Id, ability: r.Ability)).ToArray();
            // 기준 능력은 변형 부모가 아니어야 하고(AbilityExecutionContext ctor @0x0031 HasVariants 면 throw) BlastCasterLevel 대상이어야 한다(KEE 행은 2단계 최종 되읽기 VerifyElements 가 같은 조건을 본다).
            foreach (var rb in referenceBlasts)
                if (rb.ability.HasVariants || !BlastBlueprints.IsBlastAbility(rb.ability))
                    throw new InvalidOperationException("BlastMagus: reference blast " + rb.ability.name + " has variants or is not a caster-level blast");
            if (ElementTable.Vanilla.Length != referenceBlasts.Length || referenceBlasts.Select(rb => rb.element).Distinct().Count() != referenceBlasts.Length)
                throw new InvalidOperationException("BlastMagus: reference blasts cover " + referenceBlasts.Length + " of the " + ElementTable.Vanilla.Length + " stage-1 elements");
            referenceChecked = true;

            // --- 숨은 근거 능력(컨트롤러 판정 3) ---
            SourceAbility = Create<BlueprintAbility>("BlastMagusBlastStrikeSource", Guids.BlastStrikeSourceAbility, ConfigureSource);
            BlastBlueprints.Names(SourceAbility, "BlastStrikeSource.Name", "BlastStrike.Description", icon);
            if (!SourceAbility.Hidden || !SourceAbility.SpellResistance || SourceAbility.Type != AbilityType.Special || SourceAbility.ActionType != UnitCommand.CommandType.Standard || SourceAbility.ComponentsArray.Length != 0)
                throw new InvalidOperationException("BlastMagus: BlastStrikeSource readback failed (Hidden, SpellResistance, Special, Standard, no components)");
            RequireTooltipFields(SourceAbility, "");
            // SR 판정의 시전자 레벨 = 메이거스 레벨(컨트롤러 판정 5): BlastCasterLevel 의 대상 집합에 넣는다(CL 도출은 그 한 곳, C4).
            BlastBlueprints.AddCasterLevelAbility(SourceAbility);

            // --- 버프: 1라운드, Replace, 컴포넌트 = BlastStrikeOnHit 하나 ---
            Buff = Create<BlueprintBuff>("BlastMagusBlastStrikeBuff", Guids.BlastStrikeBuff, b =>
            {
                BlastBlueprints.Names(b, "BlastStrike.Name", "BlastStrike.Description", icon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                // 2026-11-17(GameLogFull: BlastStrikeOnHit+OnHitEffects.Consume → 이 버프 제거 → Buff.OnRemove @00B0 NRE 6건): 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말).
                EmptyFxOnRemove(b);
            });
            var onHit = new BlastStrikeOnHit();
            AddComponents(Buff, onHit);
            if (!Buff.IsClassFeature || Buff.Stacking != StackingType.Replace || Buff.ComponentsArray.Length != 1 || !ReferenceEquals(Buff.ComponentsArray[0], onHit))
                throw new InvalidOperationException("BlastMagus: BlastStrikeBuff readback failed (IsClassFeature, Stacking Replace, exactly one BlastStrikeOnHit component; got " + Buff.ComponentsArray.Length + " components)");

            // --- 신속 능력(정찰 §8 ArcaneAccuracyAbility 모양) ---
            var applyBuff = new ContextActionApplyBuff
            {
                DurationValue = new ContextDurationValue
                {
                    Rate = DurationRate.Rounds,
                    DiceType = DiceType.Zero,
                    DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = DurationRounds },
                },
                AsChild = true,
            };
            SetField(applyBuff, "m_Buff", Ref<BlueprintBuffReference>(Guids.BlastStrikeBuff));
            if (applyBuff.Buff != Buff || !applyBuff.AsChild || applyBuff.DurationValue.Rate != DurationRate.Rounds || applyBuff.DurationValue.BonusValue.Value != DurationRounds)
                throw new InvalidOperationException("BlastMagus: ContextActionApplyBuff readback failed (BlastStrikeBuff, AsChild, " + DurationRounds + " round)");
            Ability = Create<BlueprintAbility>("BlastMagusBlastStrikeAbility", Guids.BlastStrikeAbility, ConfigureAbility);
            BlastBlueprints.Names(Ability, "BlastStrike.Name", "BlastStrike.Description", icon);
            // 액션 요소의 Owner·name 은 JSON 로드 때만 채워진다(PhantomBlade StandBlueprints 주석) → 직접 단다.
            applyBuff.name = "$ContextActionApplyBuff$" + Guids.BlastStrikeAbility + "#0";
            applyBuff.Owner = Ability;
            if (!ReferenceEquals(applyBuff.Owner, Ability)) throw new InvalidOperationException("BlastMagus: ContextActionApplyBuff.Owner readback failed");

            var resLogic = new AbilityResourceLogic { Amount = BlastRules.BlastStrikeCost };
            SetField(resLogic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.Game.ArcanePoolResourse));
            SetField(resLogic, "m_IsSpendResource", true);
            if (resLogic.RequiredResource != pool || !resLogic.IsSpendResource || resLogic.Amount != BlastRules.BlastStrikeCost)
                throw new InvalidOperationException("BlastMagus: AbilityResourceLogic readback failed (ArcanePoolResourse, spend, amount " + BlastRules.BlastStrikeCost + ")");

            var needBlast = new AbilityCasterHasFacts { NeedsAll = true };
            SetField(needBlast, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(BlastBlueprints.BlastFeature.AssetGuid.ToString()) });
            if (needBlast.Facts.Count() != 1 || needBlast.Facts.First() != BlastBlueprints.BlastFeature)
                throw new InvalidOperationException("BlastMagus: AbilityCasterHasFacts(BlastFeature) readback failed");

            // 수정 웨이브 F6(a)(설계 §4.7·§11-7 「로그 오류 + 블라스트 비활성」): 꺼진 유닛은 능력 자체가 회색(풀 소모 전에 막힌다).
            // 엔진 경로: AbilityData.get_CanBeCastByCaster @0x006B~0x0092 가 Blueprint.CasterRestrictions(IAbilityCasterRestriction 컴포넌트)를 돈다.
            var stateGate = new BlastStateRestriction();

            // FX(2026-11-15 승인, FxTable): 본편 ArcaneAccuracyAbility 의 AbilitySpawnFx(칸 그대로 + 대조된 PrefabLink). 원본·칸·값이 표와 다르면 BlastFx 가 던진다.
            var spawnFx = BlastFx.NewBlastStrikeSpawnFx();
            string fxId = FxTable.Get(FxTable.BlastStrikeKey).AssetId;
            Ability.ResourceAssetIds = new[] { fxId };   // 본편 관례: 미리 읽을 FX id(ArcaneAccuracyAbility.ResourceAssetIds = [c388856d…])

            var run = new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { applyBuff } } };
            AddComponents(Ability, resLogic, run, needBlast, stateGate, spawnFx);
            if (spawnFx.PrefabLink == null || spawnFx.PrefabLink.AssetId != fxId || Ability.ResourceAssetIds == null || !Ability.ResourceAssetIds.SequenceEqual(new[] { fxId }))
                throw new InvalidOperationException("BlastMagus: BlastStrikeAbility FX readback failed (AbilitySpawnFx.PrefabLink and ResourceAssetIds == FxTable " + fxId + ")");
            var expectTypes = new[] { typeof(AbilityResourceLogic), typeof(AbilityEffectRunAction), typeof(AbilityCasterHasFacts), typeof(BlastStateRestriction), typeof(AbilitySpawnFx) };
            var got = Ability.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!got.SequenceEqual(expectTypes))
                throw new InvalidOperationException("BlastMagus: BlastStrikeAbility components [" + string.Join(", ", got.Select(t => t.Name)) + "] != [" + string.Join(", ", expectTypes.Select(t => t.Name)) + "]");
            if (Ability.ActionType != UnitCommand.CommandType.Swift || Ability.Range != AbilityRange.Personal || !Ability.CanTargetSelf || Ability.Type != AbilityType.Extraordinary || Ability.SpellResistance || Ability.Hidden)
                throw new InvalidOperationException("BlastMagus: BlastStrikeAbility readback failed (Swift, Personal, CanTargetSelf, Extraordinary, no SR, visible)");
            RequireTooltipFields(Ability, Guids.GameText.DurationOneRound);

            // --- 특성(4레벨) = AddFacts[Ability] ---
            Feature = BlastBlueprints.MakeFeature("BlastMagusBlastStrikeFeature", Guids.BlastStrikeFeature, "BlastStrike.Name", "BlastStrike.Description", icon);
            AddComponents(Feature, BlastBlueprints.Facts(Ability));
            if (Feature.ComponentsArray.Length != 1) throw new InvalidOperationException("BlastMagus: BlastStrikeFeature component readback failed (AddFacts[Ability] only)");

            Archetype.AddAt(BlastRules.BlastStrikeLevel, Feature);
            var levels = Archetype.Bp.AddFeatures.Where(e => e.Features.Contains(Feature)).Select(e => e.Level).ToArray();
            if (levels.Length != 1 || levels[0] != BlastRules.BlastStrikeLevel)
                throw new InvalidOperationException("BlastMagus: BlastStrikeFeature archetype levels [" + string.Join(",", levels) + "], expected [" + BlastRules.BlastStrikeLevel + "]");
        }

         
        // (무기 형식 KineticBlastPhysical, CheckWeaponType) 가 준다. 폭발 묶음은 Weapon = null 이라 그것을 못 받으므로 그 「마법」 분기를 PhysicalMagic 으로 복제한다.
        // 원본 플래그(Add*·적용 조건 NaturalAttacks/NotUnarmed/CheckWeaponType/CheckRange/AgainstFactOwner)가 PhysicalMagic.SourceFlags 와 다르면(본편·다른 모드가 바꿈)
        // 무조건 복제가 조용히 어긋나므로 빌드를 멈춘다(읽기만).
        static void CheckPhysicalBlastDr(BlueprintFeature kbf)
        {
            var physicalType = Get<BlueprintWeaponType>(Guids.Game.KineticBlastPhysical);
            var props = kbf.GetComponents<AddOutgoingPhysicalDamageProperty>().Where(p => p.WeaponType == physicalType).ToArray();
            if (props.Length != 1)
                throw new InvalidOperationException("BlastMagus: PhysicalBlastDr readback: KineticBlastFeature has " + props.Length + " AddOutgoingPhysicalDamageProperty for KineticBlastPhysical, expected exactly 1");
            var dr = props[0];
            // 플래그 원장 = PhysicalMagic.SourceFlags(C4). 이름과 그 필드를 짝지어 그대로(부정 없이) 넘기고 판정은 순수 SourceMismatches 가 한다.
            var actual = new[]
            {
                ("AddMagic", dr.AddMagic), ("AffectAnyPhysicalDamage", dr.AffectAnyPhysicalDamage), ("CheckWeaponType", dr.CheckWeaponType),
                ("AddMaterial", dr.AddMaterial), ("AddForm", dr.AddForm), ("AddAlignment", dr.AddAlignment), ("AddReality", dr.AddReality),
                ("NaturalAttacks", dr.NaturalAttacks), ("NotUnarmed", dr.NotUnarmed), ("CheckRange", dr.CheckRange), ("AgainstFactOwner", dr.AgainstFactOwner),
            };
            var bad = PhysicalMagic.SourceMismatches(actual);
            if (bad.Length > 0)
                throw new InvalidOperationException("BlastMagus: PhysicalBlastDr readback: KineticBlastPhysical property differs from PhysicalMagic.SourceFlags (the unconditional magic copy would diverge): " + string.Join("; ", bad));
        }

        internal static BlueprintAbility ReferenceBlast(Element e)
        {
            if (!referenceChecked) throw new InvalidOperationException("BlastMagus: ReferenceBlast before BlastStrike.Build()");
            return BlastBlueprints.ResolvedOf(e).Ability; // 해석 표에 없는 원소(제공되지 않은 KEE 원소)면 ArgumentOutOfRangeException
        }

        // 컨트롤러 판정 2(정찰 §8 ArcaneAccuracyAbility 값).
        // 2026-11-16 결함 수리(툴팁 지속 시간 `<null>`): 툴팁의 지속·내성 칸은 BlueprintAbility.LocalizedDuration·LocalizedSavingThrow 를 읽는다
        // (TooltipTemplateAbility.Prepare @0x0168·@0x0183). 코드로 만든 능력은 두 필드가 null 이라 "<null>" 이 찍혔다(근거 IL = BlueprintUtil 「LocalizedString 필드」 주석).
        // 값 = 본뜬 본편 ArcaneAccuracyAbility 그대로: 지속 = 공용 문구 「1 round」(Guids.GameText.DurationOneRound, DurationRounds 와 짝), 내성 = 빈 키(칸 숨김).
        static void ConfigureAbility(BlueprintAbility ab)
        {
            ab.Type = AbilityType.Extraordinary;
            ab.Range = AbilityRange.Personal;
            ab.ActionType = UnitCommand.CommandType.Swift;
            ab.CanTargetSelf = true;
            ab.SpellResistance = false;
            ab.LocalizedDuration = GameText(Guids.GameText.DurationOneRound);
            ab.LocalizedSavingThrow = NoText();
        }

        // 컨트롤러 판정 3 + 수정 웨이브 F4: 숨김·Special·SR 적용·표준 행동(시전되지 않으므로 행동 형식은 표시 전용), 효과 없음.
        // Type = Special: 본편 Fire/Cold/ElectricBlastAbility 의 jbp Type 이 Special(시험이 blueprints.zip 으로 대조). BlueprintAbility.get_IsSpell 은
        // Spell/SpellLike 에만 true 라 SpellLike 면 AllSpells 면역(UnitPartSpellResistance.CanApply @0x01EC~0x0207)·PromoteSpellDices 가 폭발에만 반응했다(codex F4).
        // SR 은 Type 을 보지 않는다: RuleSpellResistanceCheck.get_HasResistanceRoll @0x0066~0x0071 = Ability.SpellResistance(본편 블라스트 = Special + SR true).
        static void ConfigureSource(BlueprintAbility ab)
        {
            ab.Hidden = true;
            ab.Type = AbilityType.Special;
            ab.SpellResistance = true;
            ab.ActionType = UnitCommand.CommandType.Standard;
            // 2026-11-16: 지속·내성 = 빈 키(본편 Fire/Cold/ElectricBlastAbility.jbp 의 두 칸 모두 { m_Key "", Shared null } — 시험이 대조). 숨은 능력이지만 null 로 두지 않는다.
            ab.LocalizedDuration = NoText();
            ab.LocalizedSavingThrow = NoText();
        }

        // 빌드 되읽기: 지속 칸의 키 == durationKey("" = 빈 키·칸 숨김), 내성 칸 = 빈 키, 둘 다 null 아님·Shared 없음. 어긋나면 던진다.
        static void RequireTooltipFields(BlueprintAbility ab, string durationKey)
        {
            var d = ab.LocalizedDuration;
            var s = ab.LocalizedSavingThrow;
            if (d == null || s == null || d.Key != durationKey || s.Key != "" || d.Shared != null || s.Shared != null)
                throw new InvalidOperationException("BlastMagus: " + ab.name + " tooltip string readback failed (LocalizedDuration key '" + (d == null ? "null" : d.Key) + "', expected '" + durationKey
                    + "'; LocalizedSavingThrow key '" + (s == null ? "null" : s.Key) + "', expected ''; both non-null without a shared asset)");
        }

        // 툴팁이 읽는 그대로((string)LocalizedString = op_Implicit)의 지속·내성 문구 판정. null = 정상, 아니면 사유. 점검(Scenarios.UseStrikeStep)만 부른다 —
        // 빌드 시점에는 부르지 않는다(로컬라이즈 팩이 아직 없으면 LoadString @0x0000~0x0026 이 "" 를 돌려줘 거짓 실패가 된다; 빌드는 아래 RequireTooltipFields 로 필드를 되읽는다).
        // 본편 대조: 보이는 능력의 지속 문구 == 본편 ArcaneAccuracyAbility 의 지속 문구(현재 언어 팩 그대로), 비어 있지 않고 엔진의 결측 표식이 아님.
        const string NullMarker = "<null>";               // LocalizedString.op_Implicit @0x000F
        const string UnknownKeyMarker = "[unknown key";   // LocalizationPack.GetText @0x00AE



    }

    // BlastStrikeBuff 의 컴포넌트(정찰 §7 선택지 ②). 무기 피해가 들어간 뒤(RuleAttackWithWeaponResolve.OnTrigger @0x000C~0x003B) 별도 피해 규칙을 낸다.
    // 재귀 없음: 폭발 피해는 RuleDealDamage 뿐이라 RuleAttackWithWeaponResolve 가 다시 뜨지 않는다.
    // 이중 발동 차단: BlastState.Pending(소유자)을 발동 첫머리에서 내린다(버프 제거가 지연돼도 전력 공격 두 번째 명중은 통과 못 함).
     
    // 인스턴스 필드 없음(블루프린트 컴포넌트는 모든 유닛이 공유한다 — 호출별 상태는 OnHitEffects 에, R-PF7).
    public class BlastStrikeOnHit : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeaponResolve>
    {
        // 정당한 무동작: 피해는 무기 피해 뒤(DidTrigger)에 낸다.
        public void OnEventAboutToTrigger(RuleAttackWithWeaponResolve evt) { }

        public void OnEventDidTrigger(RuleAttackWithWeaponResolve evt)
        {
            StrikeExecutor.Run(new OnHitEffects(this, evt));
        }

        // 호출 한 번의 게임 쪽 효과. 사실 읽기는 오늘의 가드와 같은 엔진 판독, 효과는 오늘의 SetPending·SR·피해·소모. 분기(언제 무엇을 부를지)는 StrikeExecutor 가 한다.
        // 원소·문맥은 한 호출 안에서 한 번만 계산해 둔다(SR 문맥을 피해 규칙의 Reason 으로 다시 쓴다 — 지금 코드와 같다, R-PF9).
        sealed class OnHitEffects : IStrikeEffects
        {
            readonly BlastStrikeOnHit c;
            readonly RuleAttackWithWeaponResolve evt;
            readonly RuleAttackRoll roll;
            bool elementRead;
            Element element;
            AbilityExecutionContext ctx;

            internal OnHitEffects(BlastStrikeOnHit c, RuleAttackWithWeaponResolve evt)
            {
                if (c == null) throw new ArgumentNullException("c");
                if (evt == null) throw new ArgumentNullException("evt");
                var roll = evt.AttackRoll;
                if (roll == null) throw new InvalidOperationException("BlastMagus: RuleAttackWithWeaponResolve without an AttackRoll");
                this.c = c; this.evt = evt; this.roll = roll;
            }

            UnitEntityData Owner { get { return c.Owner; } }

            public bool IsHit() { return roll.IsHit; } // 빗나감이면 버프 유지(설계 §4.2 — 그 라운드 동안 남는다)

            // 설계 §4.2·§4.4: 근접 제작 무기 명중만(Energize 와 같은 가드). 거짓 = 이 명중은 폭발 대상이 아님(Pending 은 그대로, 다음 근접 명중을 기다린다).
            public bool WeaponOk()
            {
                var weapon = roll.Weapon;
                if (weapon == null) return false; // 설계 §4.2: 무기 명중이 필요
                if (weapon.Blueprint.IsNatural || weapon.Blueprint.IsUnarmed) return false; // 제작 무기만(Energize 와 같다)
                if (!weapon.Blueprint.IsMelee) return false; // 설계 §4.4 근접 한정(codex F5: 활 명중이 폭발을 쓰던 결함)
                // FighterGroup 0 = Stub·Touch·Ray·KineticBlast* 등 무기가 아닌 형식(Energize.cs 의 같은 가드·근거). 블라스트로 블라스트를 터뜨리지 않는다.
                if (weapon.Blueprint.FighterGroup == 0) return false;
                return true;
            }

            public bool Enabled() { return BlastState.IsEnabled(Owner); } // 꺼짐/알 수 없음 — BlastState 가 이미 로그를 남긴다

            public bool Pending() { return BlastState.Pending(Owner); } // 이미 발동한 활성(전력 공격 두 번째 명중); 알 수 없으면 BlastState 가 오류 로그

            // 원소 = *BlastBase 보유(0/2개면 ElementOf 가 던진다), 성질 = 원소 표 행.
            public StrikeElement Element()
            {
                var row = ElementTable.Row(ElementOfOwner());
                return new StrikeElement(row.Kind, row.SpellResistance);
            }

            Element ElementOfOwner()
            {
                if (!elementRead) { element = BlastBlueprints.ElementOf(Owner); elementRead = true; }
                return element;
            }

            public StrikeNumbers Numbers()
            {
                var kbf = BlastStrike.KineticBlast;
                if (kbf == null) throw new InvalidOperationException("BlastMagus: blast strike before BlastStrike.Build() (KineticBlast is null)");
                int rank = Owner.Progression.Features.GetRank(kbf);
                if (rank <= 0) throw new InvalidOperationException("BlastMagus: blast strike on " + Owner.CharacterName + " with KineticBlastFeature rank " + rank);
                 
                bool composite = BlastBlueprints.HasCompositeBlast(Owner, ElementOfOwner());
                int intMod = Owner.Stats.Intelligence.Bonus;
                // 치명타 ×2(설계 §4.4). 본편 GetDamageRule @0x0215~0x0241 과 같은 판독.
                bool crit = roll.IsCriticalConfirmed && !roll.FortificationNegatesCriticalHit && BlastRules.StrikeCanCrit(Owner.Progression.GetClassLevel(BlastBlueprints.MagusClass));
                return new StrikeNumbers(rank, composite, intMod, crit);
            }

            public void ClearPending() { BlastState.SetPending(Owner, false); }

            UnitEntityData Target()
            {
                var target = evt.Target;
                if (target == null) throw new InvalidOperationException("BlastMagus: blast strike hit without a target");
                return target;
            }

            // R-PF9: 문맥은 처음 필요할 때 한 번(SR 원소 = RollSr, SR 없는 원소 = Deal) 만들고 피해 규칙의 Reason 으로 다시 쓴다.
            AbilityExecutionContext Context()
            {
                if (ctx == null) ctx = SpellContext(Target());
                return ctx;
            }

            // 정찰 §7: SR 은 피해 규칙이 아니라 능력 적용 단계(AbilityExecutionProcess.DoApplyEffect @0x00B9~0x00C3)에서 굴린다 → 같은 규칙을 직접 굴린다.
            public SrOutcome RollSr()
            {
                var target = Target();
                var sr = Rulebook.Trigger(new RuleSpellResistanceCheck(Context(), target));
                if (sr.IsSpellResisted)
                {
                    LogResisted(target, sr);
                    return SrOutcome.Resisted;  
                }
                return SrOutcome.Allowed;
            }

            // SourceAbility 로 시전 없이 실행 컨텍스트를 만든다: new AbilityData(bp, caster) → CreateExecutionContext(target, null)
            // (CreateExecutionContext @0x0000~0x0013: params null → CalculateParams() → RuleCalculateAbilityParams → BlastCasterLevel 가 CL 을 메이거스 레벨로).
            // RuleSpellResistanceCheck ctor @0x0031~0x0059 가 Context.SourceAbilityContext.Ability.GetDeliverBlueprint(false) = SourceAbility 를 Ability 로 잡는다.
            AbilityExecutionContext SpellContext(UnitEntityData target)
            {
                var bp = BlastStrike.SourceAbility;
                if (bp == null) throw new InvalidOperationException("BlastMagus: blast strike before BlastStrike.Build() (SourceAbility is null)");
                var reference = BlastStrike.ReferenceBlast(ElementOfOwner()); // 원소 0/2개면 ElementOf 가 던진다
                var data = new AbilityData(bp, Owner.Descriptor);
                var context = data.CreateExecutionContext(target, null);
                // 기대 CL = 같은 유닛의 본편 원소 블라스트가 받는 CL(RuleCalculateAbilityParams.OnTrigger 009B~00AE: Max(1, Replace) + m_BonusCasterLevel —
                // 음수 레벨·CL 보너스 포함). 메이거스 레벨과 직접 비교하면 에너지 흡수 상태에서 매번 던진다(리뷰 Important 1).
                // 기준 컨텍스트 생성은 부작용 없음: AbilityExecutionContext/MechanicsContext ctor 는 필드·방향 설정뿐(dnlib, 보고서 fix-round-1).
                var refCtx = new AbilityData(reference, Owner.Descriptor).CreateExecutionContext(target, null);
                int cl = context.Params.CasterLevel;
                int expected = refCtx.Params.CasterLevel;
                if (cl != expected)
                    throw new InvalidOperationException("BlastMagus: blast strike SR caster level " + cl + " != " + reference.name + " caster level " + expected + " on " + Owner.CharacterName + " (BlastCasterLevel override did not apply to the SR source)");
                return context;
            }

            // 피해(정찰 §7 「직접 RuleDealDamage 재현」, 본편 ContextActionDealDamage.GetDamageRule 기준). 수치·치명타·마법 속성은 계획 값만 따른다.
            public void Deal(StrikePlan plan)
            {
                if (plan == null) throw new ArgumentNullException("plan");
                var target = Target();
                var context = Context();
                var row = ElementTable.Row(ElementOfOwner());
                if (plan.Kind != row.Kind) throw new InvalidOperationException("BlastMagus: blast strike plan kind " + plan.Kind + " != element " + row.Id + " kind " + row.Kind);
                // 설계 §4.3-2: 피해 생성 한 경로 = DamageTypeDescription.CreateDamage(dice, bonus) (엔진 IL_0008 switch(Type): Physical → PhysicalDamage(dice, bonus, Form), Energy → EnergyDamage).
                // 본편 GetDamageRule @0x0031~0x003E 도 Bonus 를 같은 함수로 넘긴다.
                var dmg = ElementModel.NewDamageType(row).CreateDamage(new DiceFormula(plan.Dice, DiceType.D6), plan.Bonus);
                if (dmg.Bonus != plan.Bonus) throw new InvalidOperationException("BlastMagus: blast strike damage Bonus readback " + dmg.Bonus + " != " + plan.Bonus);
                // 치명타 ×2(설계 §4.4). 본편 GetDamageRule @0x0215~0x0241 과 같은 필드. 대상 치명타 면역이면 엔진이 1 로 낮춘다(RuleCalculateDamage.CalculateDamageValue @0x0043).
                bool crit = plan.CritMultiplier != StrikePlanner.NoCritMultiplier;
                if (crit)
                {
                    if (plan.CritMultiplier != BlastRules.BlastStrikeCritMultiplier) throw new InvalidOperationException("BlastMagus: blast strike plan crit multiplier " + plan.CritMultiplier + " is neither " + StrikePlanner.NoCritMultiplier + " nor " + BlastRules.BlastStrikeCritMultiplier);
                    dmg.CriticalModifier = plan.CritMultiplier;
                    if (dmg.CriticalModifier != plan.CritMultiplier) throw new InvalidOperationException("BlastMagus: blast strike CriticalModifier readback failed");
                }
                // 설계 §4.4-1 「폭발 DR」: 물리 폭발에 본편 물리 블라스트의 「마법」 속성(PhysicalMagic = AddOutgoingPhysicalDamageProperty AddMagic 분기). 형식이 다르면 던진다.
                var physical = dmg as PhysicalDamage;
                if (plan.ApplyMagic)
                {
                    if (physical == null) throw new InvalidOperationException("BlastMagus: blast strike plan applies magic but CreateDamage made " + dmg.GetType().Name + " for " + row.Id);
                    var m = PhysicalMagic.Apply(physical.Enchantment, physical.EnchantmentTotal);
                    physical.Enchantment = m.enchantment;
                    physical.EnchantmentTotal = m.enchantmentTotal;
                    if (physical.Enchantment != m.enchantment || physical.EnchantmentTotal != m.enchantmentTotal) throw new InvalidOperationException("BlastMagus: blast strike physical magic readback failed");
                }
                else if (physical != null) throw new InvalidOperationException("BlastMagus: blast strike made PhysicalDamage for " + row.Id + " but the plan does not apply magic");
                 
                // AttackRoll 도 싣지 않는다(수정 웨이브 F3): RulePrepareDamage.OnTrigger @0x0287~0x02D1 이 ParentRule.AttackRoll.PreciseStrike 를 무기 확인 없이 다시 더한다
                // (듀얼리스트 정밀 타격이 폭발에 한 번 더). 본편 비공격 주문(ContextActionDealDamage.GetDamageRule @0x03FA~0x040B)도 null 을 넣는다. 치명타는 위에서 직접 싣는다.
                var rule = new RuleDealDamage(Owner, target, new DamageBundle(dmg));
                rule.SourceAbility = BlastStrike.SourceAbility;
                rule.Reason = context;
                Rulebook.Trigger(rule);
                ModContext.Log("BlastStrike: " + Owner.CharacterName + " -> " + target.CharacterName + " " + plan.Dice + "d6+" + plan.Bonus + " " + (row.Kind == DamageKind.Energy ? row.EnergyName : row.PhysicalForm + " magic")
                    + (crit ? " crit x" + plan.CritMultiplier : "") + " = " + rule.Result);
            }

            void LogResisted(UnitEntityData target, RuleSpellResistanceCheck sr)
            {
                // 전투 로그 줄은 엔진이 낸다(RulebookSpellResistanceCheckLogThread) → 우리는 모드 로그만(리뷰 Minor 1: 중복 제거).
                ModContext.Log("BlastStrike: " + Owner.CharacterName + " -> " + target.CharacterName + " resisted (SR " + sr.SpellResistance + ", penetration " + sr.SpellPenetration + ")");
            }

            // 버프 제거. Buff.Remove @0x0023~0x0030 → EntityFactsManager.Remove: 활성화 중(Activating)이면 지연(RemoveWhenActivationEnded @0x000D), 아니면 즉시 Detach.
            public void Consume()
            {
                var fact = c.Fact;
                var buff = fact as Buff;
                if (buff == null) throw new InvalidOperationException("BlastMagus: BlastStrikeOnHit on a non-buff fact (" + (fact == null ? "null" : fact.GetType().Name) + ")");
                buff.Remove();
                if (buff.IsAttached)
                {
                    if (!buff.Activating) throw new InvalidOperationException("BlastMagus: BlastStrikeBuff still attached after Remove() on " + Owner.CharacterName);
                    ModContext.Log("BlastStrike: buff removal deferred until activation ends (" + Owner.CharacterName + "); Pending is already cleared");
                }
            }
        }

        // Pending 수명: 붙을 때 올리고 떨어질 때 내린다. 세이브 로드에선 IsActive 가 직렬화돼(EntityFact.IsActive [JsonProperty]) Activate 가 다시 안 돌고
        // PostLoad → PostLoadComponents 만 돈다 → OnPostLoad 에서도 올린다(살아 있는 버프 = 아직 안 터진 버프; 터진 버프는 이미 제거됐다).
        protected override void OnActivate() { BlastState.SetPending(Owner, true); }

        protected override void OnDeactivate() { BlastState.SetPending(Owner, false); }

        protected override void OnPostLoad() { BlastState.SetPending(Owner, true); }
    }

    // 수정 웨이브 F6(a): 폭발 능력의 시전자 제한. 꺼진/알 수 없는 유닛(BlastState.IsEnabled false)이면 능력이 회색이 되어 아케인 풀을 쓰지 않는다.
    // 인터페이스 = 엔진 AbilityCasterHasFacts 가 구현하는 IAbilityCasterRestriction(dnlib). 소비자: AbilityData.get_CanBeCastByCaster @0x006B~0x0092,
    // GetUnavailableReason @0x0089~0x0097. UI 문구에는 유닛이 넘어오지 않으므로 개별 이유는 모드 로그(BlastPartGuard)에 있다.
    // 인스턴스 필드 없음(블루프린트 컴포넌트는 모든 유닛이 공유한다).
    public class BlastStateRestriction : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster) { return BlastState.IsEnabled(caster); } // null 이면 IsEnabled 가 던진다

        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "BlastStrike.Disabled"); }
    }
}
