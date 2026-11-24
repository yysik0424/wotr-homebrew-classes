using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spellfist
{
    // 저수지 신속 시전, 경로 A(설계 §4.5·§11-6, 정찰 §5-3 A). Harmony 없음.
    // 토글(즉시·휴식 유지·기본 꺼짐) → 버프 Buff(QuickenController) → 숨은 캡 버프 Caps[n-1] = AutoMetamagic{Quicken, MaxSpellLevel = n} 중 n = QuickenCap(잔량) 하나만 유지.
    // AutoMetamagic.ShouldApplyTo(정찰 §5-2, IL 직독 2026-11-08): MaxSpellLevel @0x0166~0x0185 (AbilityData.SpellLevel > n 이면 미적용), CheckSpellbook @0x0195 (참이면 m_Spellbook.SpellList 소속 검사 —
    // 우리 책 SpellList 는 빈 목록이라 false 여야 한다), Abilities 빈 목록 @0x00B2 = 전부, Descriptor 0 @0x00EC = 전부, School 0 @0x0133 = 전부, m_AllowedAbilities 기본 SpellOnly(0) @IsSuitableAbility 0x0089~0x00D8 = 주문만.
     
    public static class Quicken
    {
        public static BlueprintActivatableAbility Toggle { get; private set; }
        public static BlueprintBuff Buff { get; private set; }
        public static BlueprintBuff[] Caps { get; private set; } // Caps[n-1] = 최대 n 레벨 주문 신속
        public static BlueprintFeature Feature { get; private set; }
        internal static BlueprintAbilityResource Resource { get; private set; } // 저수지 자원(Guids.Game.ArcanistReservoirResource)

        // 설계 §5 레벨표: 신속 시전 1레벨.
        const int FeatureLevel = 1;

        // BlueprintBuff/Flags 와 AutoMetamagic/AllowedType 은 private 중첩 열거라(dnlib 2026-11-08, Assembly-CSharp) 정수 값으로 다룬다.
        // tests\SpellfistTests.cs CheckTask6Dll 이 Assembly-CSharp 의 리터럴과 대조한다(값이 바뀌면 시험이 빨개진다).
        const int BuffFlagHiddenInUi = 2; // BlueprintBuff/Flags.HiddenInUi = 2 (get_IsHiddenInUI IL_0001 ldc.i4.2)
        const int AllowedSpellOnly = 0;   // AutoMetamagic/AllowedType.SpellOnly = 0 (필드 기본값; IsSuitableAbility 0x00D6~0x00DB: Type == Spell 만)

         
        static readonly string[] CapGuids =
        {
            Guids.QuickenCap1, Guids.QuickenCap2, Guids.QuickenCap3, Guids.QuickenCap4, Guids.QuickenCap5,
            Guids.QuickenCap6, Guids.QuickenCap7, Guids.QuickenCap8, Guids.QuickenCap9,
        };

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Spellfist: Quicken.Build() called twice");
            if (Archetype.Bp == null || Spellbook.Bp == null || Reservoir.Feature == null)
                throw new InvalidOperationException("Spellfist: Quicken.Build() before Archetype/Spellbook/Reservoir.Build()");
            var resource = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(Guids.Game.ArcanistReservoirResource);
            if (resource == null) throw new InvalidOperationException("Spellfist: arcanist reservoir resource " + Guids.Game.ArcanistReservoirResource + " not in cache");
            // 아이콘 = 본편 신속 비전(QuickenedArcanaAbility, 신속 선례 — 정찰 §5-2)의 것.
            var icon = Get<BlueprintAbility>(Guids.Game.QuickenedArcanaAbility).Icon;
            if (icon == null) throw new InvalidOperationException("Spellfist: QuickenedArcanaAbility " + Guids.Game.QuickenedArcanaAbility + " has no icon");

            if (CapGuids.Length != SpellfistRules.MaxSpellLevel)
                throw new InvalidOperationException("Spellfist: Quicken has " + CapGuids.Length + " cap GUIDs but SpellfistRules.MaxSpellLevel = " + SpellfistRules.MaxSpellLevel);
            var caps = new BlueprintBuff[SpellfistRules.MaxSpellLevel];
            for (int n = 1; n <= SpellfistRules.MaxSpellLevel; n++) caps[n - 1] = BuildCap(n, CapGuids[n - 1], icon);
            for (int n = 1; n <= caps.Length; n++)
            {
                var am = caps[n - 1].ComponentsArray.OfType<AutoMetamagic>().Single();
                if (am.MaxSpellLevel != n || caps[n - 1].AssetGuid != BlueprintGuid.Parse(CapGuids[n - 1]))
                    throw new InvalidOperationException("Spellfist: Quicken cap MaxSpellLevel/GUID readback failed: cap " + n + " has MaxSpellLevel " + am.MaxSpellLevel + ", GUID " + caps[n - 1].AssetGuid);
            }
            if (caps.Distinct().Count() != SpellfistRules.MaxSpellLevel)
                throw new InvalidOperationException("Spellfist: Quicken caps are not " + SpellfistRules.MaxSpellLevel + " distinct buffs");

            // --- 토글 버프: 컴포넌트 = QuickenController 하나. ---
            var buff = Create<BlueprintBuff>("SpellfistQuickenBuff", Guids.QuickenBuff, b =>
            {
                Names(b, icon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — 토글을 끌 때 Buff.OnRemove @00B0 NRE)
            });
            var controller = new QuickenController();
            AddComponents(buff, controller);
            if (!buff.IsClassFeature || buff.Stacking != StackingType.Replace || buff.ComponentsArray.Length != 1 || !ReferenceEquals(buff.ComponentsArray[0], controller))
                throw new InvalidOperationException("Spellfist: QuickenBuff readback failed (IsClassFeature, Stacking Replace, exactly one QuickenController; got " + buff.ComponentsArray.Length + " components)");

             
            var toggle = Create<BlueprintActivatableAbility>("SpellfistQuickenToggle", Guids.QuickenToggle, t =>
            {
                Names(t, icon);
                SetField(t, "m_Buff", Ref<BlueprintBuffReference>(Guids.QuickenBuff));
                t.Group = ActivatableAbilityGroup.None;
                t.ActivationType = AbilityActivationType.Immediately;
                t.DeactivateImmediately = true;
                t.DoNotTurnOffOnRest = true;
                t.IsOnByDefault = false;
            });
            if (toggle.Buff != buff) throw new InvalidOperationException("Spellfist: QuickenToggle.m_Buff readback is not QuickenBuff");
            if (toggle.ActivationType != AbilityActivationType.Immediately || !toggle.DeactivateImmediately || !toggle.DoNotTurnOffOnRest || toggle.IsOnByDefault || toggle.Group != ActivatableAbilityGroup.None)
                throw new InvalidOperationException("Spellfist: QuickenToggle readback failed (expected Immediately, DeactivateImmediately, DoNotTurnOffOnRest, not IsOnByDefault, Group None; got "
                    + toggle.ActivationType + ", " + toggle.DeactivateImmediately + ", " + toggle.DoNotTurnOffOnRest + ", " + toggle.IsOnByDefault + ", " + toggle.Group + ")");

            // --- 특성(L1) = AddFacts[Toggle] ---
            var feature = Create<BlueprintFeature>("SpellfistQuickenFeature", Guids.QuickenFeature, f => Names(f, icon));
            var facts = new AddFacts();
            SetField(facts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.QuickenToggle) });
            if (facts.Facts.Count() != 1 || facts.Facts.First() != toggle) throw new InvalidOperationException("Spellfist: QuickenFeature AddFacts[Toggle] readback failed");
            AddComponents(feature, facts);
            if (feature.ComponentsArray.Length != 1 || !ReferenceEquals(feature.ComponentsArray[0], facts))
                throw new InvalidOperationException("Spellfist: QuickenFeature components readback failed (expected exactly AddFacts[Toggle])");

            Resource = resource;
            Caps = caps;
            Buff = buff;
            Toggle = toggle;
            Feature = feature;
            Archetype.AddAt(FeatureLevel, Feature);
            if (Archetype.CountAt(Archetype.Bp.AddFeatures, FeatureLevel, Feature) != 1)
                throw new InvalidOperationException("Spellfist: Quicken feature count at archetype level 1 readback is not 1");
        }

        // 숨은 캡 버프 하나: AutoMetamagic{Quicken, MaxSpellLevel = n, Once = false, CheckSpellbook = false} + HideFeatureInInspect.
        static BlueprintBuff BuildCap(int n, string guid, UnityEngine.Sprite icon)
        {
            var b = Create<BlueprintBuff>("SpellfistQuickenCap" + n, guid, x =>
            {
                Names(x, icon);
                x.IsClassFeature = true;
                x.Stacking = StackingType.Replace;
                SetField(x, "m_Flags", Enum.ToObject(Util.GetF(x, "m_Flags").GetType(), BuffFlagHiddenInUi));
                EmptyFxOnRemove(x); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말)
            });
            // 핫픽스 3: AutoMetamagic..ctor 는 어떤 필드도 채우지 않는다(Assembly-CSharp ctor IL_0001 기반 ctor 만) → 세 컬렉션을 명시적으로 빈 것으로 둔다.
            // jbp 에서 읽힌 컴포넌트는 직렬화가 빈 배열을 주지만, new 로 만든 우리 것은 null 이고 get_IncludeClasses/ExcludeClasses 는 그때 null 을 돌려준다(IL_0007~000B).
            var am = new AutoMetamagic { Metamagic = Metamagic.Quicken, MaxSpellLevel = n, Once = false, CheckSpellbook = false, Abilities = new List<BlueprintAbilityReference>() };
            SetField(am, "m_IncludeClasses", new BlueprintCharacterClassReference[0]);
            SetField(am, "m_ExcludeClasses", new BlueprintCharacterClassReference[0]);
            AddComponents(b, am, new HideFeatureInInspect());
            if (!b.IsHiddenInUI || !b.HiddenInInspector)
                throw new InvalidOperationException("Spellfist: " + b.name + " hidden flags readback failed (IsHiddenInUI " + b.IsHiddenInUI + ", HiddenInInspector " + b.HiddenInInspector + ")");
            if (Convert.ToInt32(Util.GetF(b, "m_Flags")) != BuffFlagHiddenInUi)
                throw new InvalidOperationException("Spellfist: " + b.name + " m_Flags is " + Util.GetF(b, "m_Flags") + ", expected only HiddenInUi");
            // 범위 필터 전부 「제한 없음」이어야 한다(정찰 §5-2): 아니면 조용히 일부 주문만 신속화된다.
            var allowed = Util.GetF(am, "m_AllowedAbilities");
            if (Convert.ToInt32(allowed) != AllowedSpellOnly)
                throw new InvalidOperationException("Spellfist: " + b.name + " AutoMetamagic.m_AllowedAbilities is " + allowed + ", expected SpellOnly");
            // 컬렉션 필터 되읽기: null 은 「비었다」가 아니라 「설정 안 됨」 → throw. 게터(IncludeClasses/ExcludeClasses)는 null 을 돌려줄 수 있어 쓰지 않고 필드를 직접 읽는다.
            if (am.Abilities == null) throw new InvalidOperationException("Spellfist: " + b.name + " AutoMetamagic.Abilities is null (must be an explicit empty list)");
            if (am.Abilities.Count != 0) throw new InvalidOperationException("Spellfist: " + b.name + " AutoMetamagic.Abilities has " + am.Abilities.Count + " entries, expected an empty list (no ability filter)");
            var inc = Util.GetF(am, "m_IncludeClasses") as BlueprintCharacterClassReference[];
            if (inc == null) throw new InvalidOperationException("Spellfist: " + b.name + " AutoMetamagic.m_IncludeClasses is null (must be an explicit empty array) or not BlueprintCharacterClassReference[] (" + (Util.GetF(am, "m_IncludeClasses") == null ? "null" : Util.GetF(am, "m_IncludeClasses").GetType().Name) + ")");
            if (inc.Length != 0) throw new InvalidOperationException("Spellfist: " + b.name + " AutoMetamagic.m_IncludeClasses has " + inc.Length + " entries, expected an empty array (no class filter)");
            var exc = Util.GetF(am, "m_ExcludeClasses") as BlueprintCharacterClassReference[];
            if (exc == null) throw new InvalidOperationException("Spellfist: " + b.name + " AutoMetamagic.m_ExcludeClasses is null (must be an explicit empty array) or not BlueprintCharacterClassReference[] (" + (Util.GetF(am, "m_ExcludeClasses") == null ? "null" : Util.GetF(am, "m_ExcludeClasses").GetType().Name) + ")");
            if (exc.Length != 0) throw new InvalidOperationException("Spellfist: " + b.name + " AutoMetamagic.m_ExcludeClasses has " + exc.Length + " entries, expected an empty array (no class filter)");
            if (am.Metamagic != Metamagic.Quicken || am.MaxSpellLevel != n || am.Once || am.CheckSpellbook
                || am.Descriptor.Value != 0 || am.School != Kingmaker.Blueprints.Classes.Spells.SpellSchool.None)
                throw new InvalidOperationException("Spellfist: " + b.name + " AutoMetamagic cap " + n + " MaxSpellLevel/Metamagic/Once/CheckSpellbook/Descriptor/School readback failed");
            var types = b.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!types.SequenceEqual(new[] { typeof(AutoMetamagic), typeof(HideFeatureInInspect) }) || !ReferenceEquals(b.ComponentsArray[0], am))
                throw new InvalidOperationException("Spellfist: " + b.name + " components [" + string.Join(", ", types.Select(t => t.Name)) + "] != [AutoMetamagic, HideFeatureInInspect]");
            return b;
        }

        static void Names(BlueprintUnitFact f, UnityEngine.Sprite icon)
        {
            Spellbook.Names(f, "Quicken.Name", "Quicken.Description");
            SetField(f, "m_Icon", icon);
            if (!ReferenceEquals(Util.GetF(f, "m_Icon"), icon)) throw new InvalidOperationException("Spellfist: " + f.name + " icon readback failed");
        }
    }

    // QuickenBuff 의 컴포넌트. 인스턴스 필드 없음(블루프린트 컴포넌트는 모든 유닛이 공유) — 시전별 표식은 규칙 인스턴스 키의 정적 ConditionalWeakTable.
    // 「실제로 신속 행동을 썼는가」(정찰 §7 미확인 항목) — dnlib 직독 2026-11-08 결론:
    //   UnitUseAbility..ctor(AbilityData, TargetWrapper) @IL_0002 가 명령 종류 = AbilityData.RuntimeActionType 을 명령 생성 때 고정하고(UnitCommand.Type),
    //   UnitUseAbility.OnAction @IL_01DA~01E6 이 RuleCastSpell.Context.CommandType = this.Type 을 넣은 뒤 @IL_01F9 Rulebook.Trigger 한다.
    //   → AboutToTrigger 시점의 Context.CommandType 이 그 시전이 실제로 쓴 행동이다(RuntimeActionType 재계산은 쿨다운 뒤라 Standard 로 바뀔 수 있어 쓰지 않는다).
    //   AbilityExecutionContext 생성자는 CommandType 을 넣지 않으므로(기본 Free = 0) 명령 밖의 시전(접촉 전달·ContextActionCastSpell 등)은 Swift 가 아니다 → 차감 없음.
    //   두 번째 신속 주문은 get_RuntimeActionType @0x0013~0x0063 이 Standard 로 내려 명령 종류가 Standard → 차감 없음.
    public class QuickenController : UnitFactComponentDelegate, IUnitAbilityResourceHandler, IUnitCombatHandler, IInitiatorRulebookHandler<RuleCastSpell>
    {
        // Type = 이 시전의 명령 종류, CapAtCast = 시전 시점 보유한 우리 캡 버프의 n(없으면 0).
        sealed class Marker { public UnitCommand.CommandType Type; public int CapAtCast; }

        static readonly ConditionalWeakTable<RuleCastSpell, Marker> markers = new ConditionalWeakTable<RuleCastSpell, Marker>();

        protected override void OnActivate() { Sync(Owner); }
        protected override void OnPostLoad() { Sync(Owner); }
        protected override void OnDeactivate() { CancelPendingSwift(Owner, "deactivate"); RemoveAllCaps(Owner); }
        // EntityFactComponentDelegate`2 의 가상 OnTurnOn/OnTurnOff(dnlib 2026-11-08): 사실이 꺼지면 캡도 걷고, 다시 켜지면 맞춘다.
        protected override void OnTurnOn() { Sync(Owner); }
        protected override void OnTurnOff() { CancelPendingSwift(Owner, "turn off"); RemoveAllCaps(Owner); }

        // 우리 캡(n)이 이 주문 레벨을 신속화할 수 있었는가. 캡 0 = 우리 신속 없음, 캡 < 레벨 = 다른 원천(막대·MetamagicOnNextSpell 등).
         
        static bool CapCovers(int cap, int spellLevel) { return cap != 0 && cap >= spellLevel; }

         
        // 예외는 LogError 로 남긴다(삼키지 않고 기록 — 취소 못 한 명령은 저수지를 우회할 수 있다는 뜻).
        static void CancelPendingSwift(UnitEntityData owner, string why)
        {
            try { InterruptPendingSwift(owner, why); }
            catch (Exception e)
            {
                ModContext.LogError("Spellfist: quicken toggle " + why + " — cancelling pending swift casts failed (" + (owner == null ? "?" : owner.CharacterName) + "); caps are still removed, a pending reservoir swift cast may bypass the reservoir: " + e);
            }
        }

         
        // 토글 ON 때 만든 Swift 명령이 OFF 뒤에 OnAction 하면 신속은 남고 이 컴포넌트(과금)는 사라져 저수지 0 소모가 된다.
        // UnitCommands.InterruptAll(Func) 은 실행 칸(m_Commands)과 대기열(m_Queue)을 모두 돌며 술어가 참인 명령에 Interrupt(true) 한다(IL_0004~0036, IL_0038~007D, dnlib 2026-11-08).
        // 이미 OnAction 을 지난 명령(IsActed)은 그때 토글이 켜져 있었으므로 과금이 끝났다 — 건드리지 않는다.
         
        // 한계(README): 끄기 전에 이미 캡이 내려갔으면(자원 감소·전투 이탈) 우리가 만든 명령도 캡 밖으로 보여 남는다 — F2 잔여 경로와 같은 뿌리.
        static void InterruptPendingSwift(UnitEntityData owner, string why)
        {
            if (owner == null) throw new InvalidOperationException("Spellfist: QuickenController has no owner");
            if (Spellbook.Bp == null) throw new InvalidOperationException("Spellfist: QuickenController active but Spellbook.Build() did not run");
            int heldCap = HeldCap(owner);
            var cmds = owner.Commands;
            if (cmds == null)
            {
                // 판정 불가: 취소할 명령 컬렉션이 없다. 던지지 않고(호출자가 캡을 걷어야 한다) 오류로 남긴다.
                ModContext.LogError("Spellfist: " + owner.CharacterName + " has no UnitCommands — cannot cancel pending swift casts on quicken toggle " + why + " (held cap " + heldCap + "); a pending reservoir swift cast may bypass the reservoir");
                return;
            }
            var cancelled = new List<string>();
            cmds.InterruptAll(c =>
            {
                var u = c as UnitUseAbility;
                if (u == null || u.Type != UnitCommand.CommandType.Swift || u.IsActed || u.IsFinished) return false;
                var a = u.Ability;
                if (a == null)
                {
                    // 판정 불가: 어떤 주문인지 모르는 신속 명령. 취소하지 않되 조용히 넘기지 않는다.
                    ModContext.LogError("Spellfist: pending swift UnitUseAbility with null Ability on " + owner.CharacterName + " at quicken toggle " + why + " — not cancelled, reservoir may be bypassed");
                    return false;
                }
                if (a.Spellbook == null || a.Spellbook.Blueprint != Spellbook.Bp) return false; // 우리 주문서 밖(OnEventDidTrigger 와 같은 범위)
                if (a.Blueprint.ActionType == UnitCommand.CommandType.Swift) return false;      // 원래 신속 주문
                if (a.MetamagicData != null && a.MetamagicData.Has(Metamagic.Quicken)) return false; // 특기로 준비한 신속
                if (!CapCovers(heldCap, a.SpellLevel)) return false; // 우리 캡 밖 = 외부 신속 원천(막대 등) — 남긴다
                cancelled.Add(a.Name);
                return true;
            });
            foreach (var n in cancelled)
                ModContext.Log("Spellfist: quicken toggle " + why + " — cancelled pending swift cast of " + n + " (" + owner.CharacterName + ", cap " + heldCap + "): reservoir quicken toggle turned off");
        }

        // 캡은 전투 중에만(Fix round 1 R2). IUnitCombatHandler 는 IGlobalSubscriber(dnlib) — 모든 유닛의 진입·이탈이 온다.
        // UnitEntityData.JoinCombat @IL_005A / LeaveCombat @IL_0023 이 CombatState 를 바꾼 뒤 이벤트를 낸다(@IL_0072 / @IL_00B9) → 여기서 IsInCombat 은 이미 새 값.
        public void HandleUnitJoinCombat(UnitEntityData unit) { if (ReferenceEquals(unit, Owner)) Sync(Owner); }
        public void HandleUnitLeaveCombat(UnitEntityData unit) { if (ReferenceEquals(unit, Owner)) Sync(Owner); }

        // 전역 구독자(IUnitAbilityResourceHandler : IGlobalSubscriber, dnlib) — 모든 유닛의 자원 변동이 온다. 우리 유닛·저수지만.
        public void HandleAbilityResourceChange(UnitEntityData unit, UnitAbilityResource resource, int oldAmount)
        {
            if (!ReferenceEquals(unit, Owner)) return; // 다른 유닛(정상)
            if (resource == null) throw new InvalidOperationException("Spellfist: QuickenController got a resource change with null resource");
            if (Quicken.Resource == null) throw new InvalidOperationException("Spellfist: QuickenController active but Quicken.Build() did not run");
            if (resource.Blueprint != Quicken.Resource) return; // 다른 자원(정상)
            Sync(Owner);
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
            if (!IsOurBook(evt)) return;
            var ctx = evt.Context;
            if (ctx == null) return; // 표식 없음 → DidTrigger 가 LogError(판정 불가)
            Marker existing;
            if (markers.TryGetValue(evt, out existing)) throw new InvalidOperationException("Spellfist: QuickenController saw RuleCastSpell AboutToTrigger twice for the same rule");
            markers.Add(evt, new Marker { Type = ctx.CommandType, CapAtCast = HeldCap(Owner) });
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            Marker mark;
            bool marked = markers.TryGetValue(evt, out mark);
            if (marked) markers.Remove(evt);
            if (!evt.Success) return; // 실패한 시전은 차감하지 않는다(설계 §4.5 「성공할 때마다」)
            var spell = evt.Spell;
            if (spell == null) throw new InvalidOperationException("Spellfist: QuickenController got RuleCastSpell.Spell == null");
            var book = spell.Spellbook;
            if (Spellbook.Bp == null) throw new InvalidOperationException("Spellfist: QuickenController active but Spellbook.Build() did not run");
            if (book == null || book.Blueprint != Spellbook.Bp) return; // 우리 주문서 밖(멀티클래스 한계는 README)
            // 원래 신속(메타마법 특기로 준비/변환된 Quicken) 주문은 저수지 신속이 아니다.
            if (spell.MetamagicData != null && spell.MetamagicData.Has(Metamagic.Quicken)) return;
            if (!marked)
            {
                ModContext.LogError("Spellfist: QuickenController has no swift-action marker for " + spell.Name + " (" + (Owner == null ? "?" : Owner.CharacterName) + ") — action type unknown, reservoir not spent");
                return;
            }
            if (mark.Type != UnitCommand.CommandType.Swift) return; // 보통 행동으로 시전(캡 밖·두 번째 신속 강등·명령 밖 시전)
            // 원래 신속 행동 주문(블루프린트 ActionType Swift)은 우리 신속화의 몫이 아니다.
            if (spell.Blueprint.ActionType == UnitCommand.CommandType.Swift) return;
            // 우리 캡이 줄 수 있었던 신속만 차감(Fix round 1 R1). 캡이 없거나(0) 주문 레벨보다 낮으면 신속은 다른 원천
            // (UnitPartSpecialMetamagic.GetMetamagic 의 MetamagicOnNextSpell·막대, UnitPartAbilityModifiers.HasActionOverride) — 차감하지 않는다.
            if (!CapCovers(mark.CapAtCast, spell.SpellLevel))
            {
                ModContext.Log("Spellfist: INFO quicken of " + spell.Name + " (level " + spell.SpellLevel + ") came from another quicken source (our cap at cast " + mark.CapAtCast + ") — reservoir not spent");
                return;
            }
            int cost = SpellfistRules.QuickenCost(spell.SpellLevel);
            var res = RequireResource(Owner);
            int have = Owner.Resources.GetResourceAmount(res);
            // Spend 는 모자라면 PFLog 오류 후 남은 만큼만 깎는다(Spend IL_004F~00A8) → 캡이 있었는데 모자라면 상태 모순이므로 던진다.
            if (have < cost) throw new InvalidOperationException("Spellfist: QuickenController reservoir " + have + " < quicken cost " + cost + " for " + spell.Name + " (cap buff disagreed with pool)");
            Owner.Resources.Spend(res, cost);
            Sync(Owner);
        }

        bool IsOurBook(RuleCastSpell evt)
        {
            if (!ReferenceEquals(evt.Initiator, Owner)) throw new InvalidOperationException("Spellfist: QuickenController got RuleCastSpell for another initiator");
            if (Spellbook.Bp == null) throw new InvalidOperationException("Spellfist: QuickenController active but Spellbook.Build() did not run");
            var book = evt.Spell == null ? null : evt.Spell.Spellbook;
            return book != null && book.Blueprint == Spellbook.Bp;
        }

        static BlueprintAbilityResource RequireResource(UnitEntityData owner)
        {
            if (owner == null) throw new InvalidOperationException("Spellfist: QuickenController has no owner");
            var res = Quicken.Resource;
            if (res == null || Quicken.Caps == null) throw new InvalidOperationException("Spellfist: QuickenController active but Quicken.Build() did not run");
            // GetResourceAmount 는 자원이 없으면 0 을 돌려준다(IL_0009~000C) → 「풀 0」과 구별되지 않으므로 먼저 확인한다.
            if (!owner.Resources.ContainsResource(res)) throw new InvalidOperationException("Spellfist: " + owner.CharacterName + " has no reservoir resource " + res.AssetGuid + " (Reservoir feature missing)");
            return res;
        }

        // 캡 버프를 Caps[QuickenCap(잔량) - 1] 하나만(잔량 0 이면 없음) 남긴다. 이미 맞으면 아무것도 바꾸지 않는다(자원 변동마다 버프 재부여 금지).
        internal static void Sync(UnitEntityData owner)
        {
            var res = RequireResource(owner);
            // 캡은 전투 중에만(Fix round 1 R2). UnitEntityData.IsInCombat 은 CombatState 가 null 이면 false(get_IsInCombat IL_0006~0015) →
            // 그때는 캡 없음 = 신속 없음·차감 없음 쪽으로 기운다(무료 신속은 생기지 않는다). 전투 진입 이벤트가 다시 Sync 한다.
            int cap = owner.IsInCombat ? SpellfistRules.QuickenCap(owner.Resources.GetResourceAmount(res)) : 0;
            var caps = Quicken.Caps;
            for (int n = 1; n <= caps.Length; n++)
            {
                var have = owner.Buffs.GetBuff(caps[n - 1]);
                if (n == cap && have == null)
                {
                    if (owner.Buffs.AddBuff(caps[n - 1], owner, null, null) == null)
                        throw new InvalidOperationException("Spellfist: AddBuff " + caps[n - 1].name + " returned null for " + owner.CharacterName);
                }
                else if (n != cap && have != null) have.Remove();
            }
            var present = Enumerable.Range(1, caps.Length).Where(n => owner.Buffs.GetBuff(caps[n - 1]) != null).ToArray();
            if (!(cap == 0 ? present.Length == 0 : present.Length == 1 && present[0] == cap))
                throw new InvalidOperationException("Spellfist: quicken cap buffs after sync [" + string.Join(",", present) + "], expected " + (cap == 0 ? "none" : cap.ToString()) + " for " + owner.CharacterName);
        }

        // 지금 보유한 우리 캡 버프의 n(없으면 0). 둘 이상이면 Sync 불변식 위반 → throw.
        static int HeldCap(UnitEntityData owner)
        {
            if (owner == null) throw new InvalidOperationException("Spellfist: QuickenController has no owner");
            if (Quicken.Caps == null) throw new InvalidOperationException("Spellfist: QuickenController active but Quicken.Build() did not run");
            var held = Enumerable.Range(1, Quicken.Caps.Length).Where(n => owner.Buffs.GetBuff(Quicken.Caps[n - 1]) != null).ToArray();
            if (held.Length > 1) throw new InvalidOperationException("Spellfist: more than one quicken cap buff on " + owner.CharacterName + " [" + string.Join(",", held) + "]");
            return held.Length == 0 ? 0 : held[0];
        }

        static void RemoveAllCaps(UnitEntityData owner)
        {
            if (owner == null) throw new InvalidOperationException("Spellfist: QuickenController has no owner");
            if (Quicken.Caps == null) throw new InvalidOperationException("Spellfist: QuickenController active but Quicken.Build() did not run");
            foreach (var bp in Quicken.Caps)
            {
                var b = owner.Buffs.GetBuff(bp);
                if (b != null) b.Remove();
            }
            if (Quicken.Caps.Any(bp => owner.Buffs.GetBuff(bp) != null))
                throw new InvalidOperationException("Spellfist: quicken cap buffs after sync-off still present on " + owner.CharacterName);
        }
    }
}
