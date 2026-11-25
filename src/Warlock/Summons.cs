using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Parts;
using Rounds = Kingmaker.Utility.Rounds;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Warlock
{
     
    public sealed class SummonRecord
    {
        public readonly UnitEntityData Unit, Caster;
        public readonly string UniqueId;
        public readonly SummonPhase Phase;
        public SummonRecord(UnitEntityData unit, string uniqueId, UnitEntityData caster, SummonPhase phase) { Unit = unit; UniqueId = uniqueId; Caster = caster; Phase = phase; }
    }

    // T3: 워락 풀 + 과열 소환 부모 6 + 같은 시전자 소환물 관측(C4 ⑦ 단일 원장). T5: 변형 54(SummonTable.Cells) · 대사자 표식 버프 · 최고 단계.
    public static class Summons
    {
        public static BlueprintSummonPool Pool;
        public static BlueprintBuff CapstoneBuff; // L20 대사자 표식(영구 IX 소환물, 전투 종료 처리기가 그 소환물을 소멸시킨다)
        public static BlueprintBuff IxAugmentBuff; // 항목 9: 두 번째 소환 강화(SummonTable.Cell.IxAugment 칸만)
        public static BlueprintBuff ServantBuff; // 항목 4b: 직접 조종 불가 표식(54칸 전부 — WarlockNoDirectControl)
        static readonly Dictionary<Group, BlueprintAbility> parents = new Dictionary<Group, BlueprintAbility>();
        static readonly Dictionary<string, BlueprintAbility> variants = new Dictionary<string, BlueprintAbility>();
        public static bool ParentsBuilt { get; private set; }
        public static bool Built { get; private set; }
        static BlueprintBuff summonedBuff, dismemberedBuff;
        // Invalid 단계(SummonedUnitBuff·DismemberedUnitBuff 모순 또는 둘 다 없음) ERROR 1회 키 = UniqueId(진단 전용 런타임 상태, 저장 안 함).
        // 로드(세대 = PactLog.Generation) 범위 — PactLog/HeatLog 와 같은 OnceSet. 같은 세이브를 다시 불러오면 다시 나온다(최종 검토 m-4).
        static readonly OnceSet invalidLogged = new OnceSet();

        public static void Build()
        {
            if (Pool != null || ParentsBuilt || Built) throw new InvalidOperationException("Warlock: Summons.Build() called twice");
            SummonTable.Verify(); // 표 불변식(54칸·대체 15·템플릿 12·DLC5 1) — 어긋나면 빌드 실패
            var available = SummonTable.Available(id => ResourcesLibrary.TryGetBlueprint<BlueprintUnit>(id) != null);
            if (available.Length != SummonTable.Cells.Length) ModContext.Log("워락: DLC 5 심연 III 소환을 건너뜁니다. / Warlock: DLC 5 Abyss III summon omitted.");
            Pool = Create<BlueprintSummonPool>("WarlockSummonPool", Guids.SummonPool, p =>
            {
                p.Limit = 0;                     // 미사용 — ContextActionSpawnMonster 의 Limit 은 「조용한 거부」라 쓰지 않는다(정찰 §3-3, 계획 판정 「Limit 불채택」)
                p.DoNotRemoveDeadUnits = false;  // 죽은 유닛은 SummonPoolsManager.HandleUnitDeath 가 뺀다(PackWarden AuxUnits.cs:60-64 와 같음)
            });
            if (Pool.Limit != 0 || Pool.DoNotRemoveDeadUnits) throw new InvalidOperationException("Warlock: WarlockSummonPool readback failed");
            summonedBuff = Get<BlueprintBuff>(Guids.Game.SummonedUnitBuff);
            dismemberedBuff = Get<BlueprintBuff>(Guids.Game.DismemberedUnitBuff);
            var capIcon = KitBuild.Icon(Guids.Game.SummonMonsterIXSingle);
            CapstoneBuff = Create<BlueprintBuff>("WarlockCapstoneSummonBuff", Guids.CapstoneSummonBuff, b =>
            {
                KitBuild.Names(b, KitSpec.GrandSummonerKey, capIcon);
                b.IsClassFeature = true;
                EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — Buff.OnRemove @00B0 NRE)
            });
            if (CapstoneBuff.ComponentsArray == null || CapstoneBuff.ComponentsArray.Length != 0) throw new InvalidOperationException("Warlock: WarlockCapstoneSummonBuff must have 0 components");
            BuildIxAugment();
            BuildServant();
            var icon = KitBuild.Icon(Guids.Game.SummonMonsterISingle);
            foreach (var g in PatronGroups.AllGroups)
            {
                var ab = Create<BlueprintAbility>("WarlockSummon" + g, KitRule.SummonParent(g), a =>
                {
                    KitBuild.Names(a, KitSpec.ParentKey(g), icon);
                    a.Type = AbilityType.Supernatural;
                    a.Range = AbilityRange.Close;
                    a.ActionType = UnitCommand.CommandType.Swift; // 설계 §4.6 「행동 신속」
                    a.CanTargetSelf = true;
                    a.CanTargetPoint = true;
                    a.CanTargetEnemies = false;
                    a.CanTargetFriends = false;
                    a.SpellResistance = false;
                    // 2026-11-16(툴팁 `<null>` 결함): 지속 = 본편 공용 문구 「1 round/level」, 내성 = 빈 키(칸 숨김). 본편 변형 부모 SummonMonsterII~IXBase.jbp 와 같다 — 시험 `tooltip:` 이 대조.
                    a.LocalizedDuration = GameText(Guids.GameText.DurationOneRoundPerLevel);
                    a.LocalizedSavingThrow = NoText();
                });
                KitBuild.RequireTooltipFields(ab, Guids.GameText.DurationOneRoundPerLevel);
                parents.Add(g, ab);
                var refs = new List<BlueprintAbilityReference>();
                var tiers = available.Where(c => c.Group == g).Select(c => c.Tier).ToArray();
                foreach (int tier in tiers)
                {
                    var v = BuildVariant(g, tier, ab);
                    variants.Add(DerivedGuids.Summon(g, tier), v);
                    refs.Add(Ref<BlueprintAbilityReference>(DerivedGuids.Summon(g, tier)));
                }
                var av = new AbilityVariants();
                SetField(av, "m_Variants", refs.ToArray());
                AddComponents(ab, av);
                // 양방향 되읽기: 부모 → 변형 9(단계 순), 변형 → 부모.
                var back = av.Variants.ToArray();
                if (ab.ComponentsArray.Length != 1 || ab.GetComponent<AbilityVariants>() != av || back.Length != tiers.Length
                    || tiers.Where((tier, i) => back[i] == null || back[i].AssetGuid.ToString() != DerivedGuids.Summon(g, tier) || !ReferenceEquals(back[i].Parent, ab)).Any())
                    throw new InvalidOperationException("Warlock: summon parent " + ab.name + " readback failed (one AbilityVariants with the available tier variants in order, each Parent == parent)");
            }
            ParentsBuilt = true;
            Built = true;
        }

         
        // 본편 AugmentSummoningBuff 를 두 번 씌우면 쌓이지 않는다(2026-11-14 dnlib): 그 버프 Stacking = Replace(jbp) → BuffCollection.PrepareFactForAttach @0124 switch 0 → @014A 옛 버프 제거 후 새 것 1개.
        // 같은 +4 를 Enhancement 서술자로 주는 별도 버프도 0 이다(EngineCodes.ModifierUntypedStackable 주석 — 같은 서술자 최대값만). 그래서 별도 버프 + UntypedStackable 서술자로 힘·건강 +4 를 더한다.
        static void BuildIxAugment()
        {
            var icon = KitBuild.Icon(Guids.Game.SummonMonsterIXSingle);
            IxAugmentBuff = Create<BlueprintBuff>("WarlockIxAugmentBuff", Guids.IxAugmentBuff, b =>
            {
                KitBuild.Names(b, KitSpec.IxBoostKey, icon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말)
            });
            var str = new AddStatBonus { Stat = StatType.Strength, Descriptor = (ModifierDescriptor)EngineCodes.ModifierUntypedStackable, Value = WarlockRules.Dials.IxAugmentBonus };
            // 2026-11-17(소유자 결정 「언데드는 매력 +4」): 건강 몫은 유닛마다 정한다 — 건강이 비활성(언데드)이면 엔진이 건강 수정치를 버리므로 매력으로(WarlockIxHealthBonus·SummonRule.IxHealthStat).
            var health = new WarlockIxHealthBonus { Descriptor = (ModifierDescriptor)EngineCodes.ModifierUntypedStackable, Value = WarlockRules.Dials.IxAugmentBonus };
            str.name = "$AddStatBonus$" + Guids.IxAugmentBuff + "#0";
            health.name = "$WarlockIxHealthBonus$" + Guids.IxAugmentBuff + "#1";
            AddComponents(IxAugmentBuff, str, health);
            var got = IxAugmentBuff.ComponentsArray.OfType<AddStatBonus>().ToArray();
            var gotHealth = IxAugmentBuff.ComponentsArray.OfType<WarlockIxHealthBonus>().ToArray();
            if (IxAugmentBuff.ComponentsArray.Length != 2 || got.Length != 1 || got[0].Stat != StatType.Strength || got[0].Value != WarlockRules.Dials.IxAugmentBonus || got[0].Descriptor.ToString() != "UntypedStackable" || got[0].ScaleByBasicAttackBonus
                || gotHealth.Length != 1 || gotHealth[0].Value != WarlockRules.Dials.IxAugmentBonus || gotHealth[0].Descriptor.ToString() != "UntypedStackable")
                throw new InvalidOperationException("Warlock: WarlockIxAugmentBuff readback failed (want AddStatBonus Strength and WarlockIxHealthBonus (Constitution, or Charisma if Constitution is disabled), UntypedStackable +" + WarlockRules.Dials.IxAugmentBonus + ")");
        }

         
        // 본편 길(2026-11-14 dnlib): get_IsDirectlyControllable @000B-0018 — PreventDirectControl 이 서 있으면 다른 판정 전에 false. 같은 깃발을 본편 ChangeFaction(m_AllowDirectControl false)이
        // OnTurnOn @01A5-01B8 Retain / OnTurnOff @0000-0013 Release 로 쓴다(그 컴포넌트의 진영·그룹 바꾸기는 하지 않는다 — 아군·같은 그룹 그대로). 깃발은 저장되지 않고(필드에 JsonProperty 없음,
        // JSON 생성자 @005B-0060 이 새로 만든다) 로드 뒤 사실이 다시 켜질 때 OnTurnOn 이 다시 잡는다 — ChangeFaction 과 같은 전제. 소환 액션의 IsDirectlyControllable 필드는 엔진이 읽지 않는다
        // (ContextActionSpawnMonster.RunAction 에 그 ldfld 없음 — 그 false 는 효과 없음, 이 버프가 실제 경로).
        static void BuildServant()
        {
            var icon = KitBuild.Icon(Guids.Game.SummonMonsterISingle);
            ServantBuff = Create<BlueprintBuff>("WarlockServantBuff", Guids.ServantBuff, b =>
            {
                KitBuild.Names(b, KitSpec.ServantKey, icon);
                b.IsClassFeature = true;
                b.Stacking = StackingType.Replace;
                EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말)
            });
            var c = new WarlockNoDirectControl { name = "$WarlockNoDirectControl$" + Guids.ServantBuff + "#0" };
            AddComponents(ServantBuff, c);
            if (ServantBuff.ComponentsArray.Length != 1 || !ReferenceEquals(ServantBuff.ComponentsArray[0], c))
                throw new InvalidOperationException("Warlock: WarlockServantBuff readback failed (want exactly one WarlockNoDirectControl)");
        }

        // 변형 1개(계획 T5 「Summons.Build」): 칸 = SummonTable.Of(g, tier) 하나(C4 ⑤).
        static BlueprintAbility BuildVariant(Group g, int tier, BlueprintAbility parent)
        {
            var cell = SummonTable.Of(g, tier);
            string guid = DerivedGuids.Summon(g, tier);
            var icon = KitBuild.Icon(SummonTable.IconAbility(tier));
            var ab = Create<BlueprintAbility>("WarlockSummon" + g + tier, guid, a =>
            {
                KitBuild.Names(a, KitSpec.VariantKey(g, tier), icon);
                a.Type = AbilityType.Supernatural;
                a.Range = AbilityRange.Close;
                a.ActionType = (UnitCommand.CommandType)EngineCodes.ActionSwift;
                a.CanTargetPoint = true;
                a.CanTargetSelf = true;
                a.CanTargetEnemies = false;
                a.CanTargetFriends = false;
                a.SpellResistance = false;
                a.Animation = (UnitAnimationActionCastSpell.CastAnimationStyle)EngineCodes.AnimationOmni;
                a.Parent = parent;
                // 2026-11-16(툴팁 `<null>` 결함): 지속 = 본편 공용 문구 「1 round/level」(본편 SummonMonsterI~IXSingle.jbp 의 LocalizedDuration, 기본 지속 = 위치 레벨 라운드와 짝),
                // 내성 = 빈 키(칸 숨김, 본편과 같음). L16 지속 2배·L20 대사자(IX 전투 종료까지)는 문구에 반영하지 않는다(기본 지속만 적는다 — 보고의 「판정 필요」 항목).
                a.LocalizedDuration = GameText(Guids.GameText.DurationOneRoundPerLevel);
                a.LocalizedSavingThrow = NoText();
            });
            KitBuild.RequireTooltipFields(ab, Guids.GameText.DurationOneRoundPerLevel);
            int k = 0;
            Func<string> nm = () => "$WarlockSummon$" + guid + "#" + (k++);
            var after = new List<GameAction> { ApplyPermanent(SummonTable.FxBuff(tier), ab, nm()), ApplyPermanent(ServantBuff.AssetGuid.ToString(), ab, nm()) }; // 항목 4b: 조종 불가 = 54칸 전부(조건 없음)
            if (cell.TemplateBuffGuid != null) after.Add(ApplyPermanent(cell.TemplateBuffGuid, ab, nm()));
            if (cell.IxAugment) after.Add(ApplyPermanent(IxAugmentBuff.AssetGuid.ToString(), ab, nm())); // 항목 9 — 표의 3칸만(버프는 BuildIxAugment 가 먼저 만든다)
            var spawn = new ContextActionSpawnMonster
            {
                DurationValue = new ContextDurationValue { Rate = (DurationRate)EngineCodes.DurationRateRounds, DiceType = DiceType.Zero, DiceCountValue = Simple(0), BonusValue = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default } },
                CountValue = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = Simple(0), BonusValue = Simple(1) },
                LevelValue = Simple(0),
                DoNotLinkToCaster = false,
                IsDirectlyControllable = false, // 엔진이 읽지 않는 필드(위 BuildServant 주석) — 조종 불가는 ServantBuff
                UseLimitFromSummonPool = false, // Limit = 조용한 거부(정찰 §3-3) — 동시 1마리는 비우기 액션
                AfterSpawn = new ActionList { Actions = after.ToArray() },
            };
            SetField(spawn, "m_Blueprint", Ref<BlueprintUnitReference>(cell.UnitGuid));
            SetField(spawn, "m_SummonPool", Ref<BlueprintSummonPoolReference>(Guids.SummonPool));
            Own(spawn, ab, nm());
            var spend = new ContextActionWarlockSpendHeat { Tier = tier }; Own(spend, ab, nm());
            var clear = new ContextActionWarlockClearOwnSummons(); Own(clear, ab, nm());
            var confirm = new ContextActionWarlockConfirmSummon(); Own(confirm, ab, nm()); // 리뷰 T5 m-4: 소환 실패 = 과열 복원 + 승인 닫기 + ERROR
            var approved = new ConditionWarlockApproved(); Own(approved, ab, nm());
            var gate = new Conditional
            {
                ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { approved } },
                IfTrue = new ActionList { Actions = new GameAction[] { clear, spawn, confirm } },
                IfFalse = new ActionList { Actions = new GameAction[0] },
            };
            Own(gate, ab, nm());
            var run = new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { spend, gate } } };
            var desc = new SpellDescriptorComponent { Descriptor = new SpellDescriptorWrapper((SpellDescriptor)EngineCodes.SpellDescriptorSummoning) };
            var cost = new WarlockHeatCost { Tier = tier };
            var marker = new WarlockSummonMarker { Group = g, Tier = tier };
            var rank = KitBuild.RankOf(KitSpec.SummonRoundsRank);
            AddComponents(ab, desc, cost, marker, run, rank);

            // 되읽기(조용한 기본값 금지): 컴포넌트 순서 · 유닛·풀·템플릿 == 칸 · 연출 버프 · 대상·행동 · 부모.
            var expect = new[] { typeof(SpellDescriptorComponent), typeof(WarlockHeatCost), typeof(WarlockSummonMarker), typeof(AbilityEffectRunAction), typeof(Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig) };
            var got = ab.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!got.SequenceEqual(expect)) throw new InvalidOperationException("Warlock: " + ab.name + " components [" + string.Join(", ", got.Select(x => x.Name)) + "]");
            var buffs = spawn.AfterSpawn.Actions.Cast<ContextActionApplyBuff>().Select(x => x.Buff == null ? null : x.Buff.AssetGuid.ToString()).ToArray();
            var wantList = new List<string> { SummonTable.FxBuff(tier), ServantBuff.AssetGuid.ToString() };
            if (cell.TemplateBuffGuid != null) wantList.Add(cell.TemplateBuffGuid);
            if (cell.IxAugment) wantList.Add(IxAugmentBuff.AssetGuid.ToString());
            var wantBuffs = wantList.ToArray();
            if (spawn.Blueprint == null || spawn.Blueprint.AssetGuid.ToString() != cell.UnitGuid || spawn.SummonPool != Pool || !buffs.SequenceEqual(wantBuffs) || spawn.UseLimitFromSummonPool)
                throw new InvalidOperationException("Warlock: " + ab.name + " spawn readback failed (unit/pool/after-spawn buffs != SummonTable " + g + "/" + tier + ")");
            var t = gate.IfTrue.Actions;
            if (t.Length != 3 || !ReferenceEquals(t[0], clear) || !ReferenceEquals(t[1], spawn) || !ReferenceEquals(t[2], confirm)) // 리뷰 T5 m-1: {Clear, Spawn, Confirm} 순서(Spawn 뒤 Clear 면 새 소환물을 지운다)
                throw new InvalidOperationException("Warlock: " + ab.name + " gate IfTrue order is not {Clear, Spawn, Confirm}");
            if (((SpellDescriptor)desc.Descriptor).ToString() != "Summoning" || cost.Tier != tier || marker.Tier != tier || marker.Group != g)
                throw new InvalidOperationException("Warlock: " + ab.name + " descriptor/cost/marker readback failed");
            if (ab.ActionType.ToString() != "Swift" || ab.Type != AbilityType.Supernatural || ab.CanTargetEnemies || ab.CanTargetFriends || !ab.CanTargetSelf || !ab.CanTargetPoint || ab.SpellResistance || !ReferenceEquals(ab.Parent, parent) || ab.Animation.ToString() != "Omni")
                throw new InvalidOperationException("Warlock: " + ab.name + " targeting/action/parent readback failed");
            return ab;
        }

        static ContextValue Simple(int v) { return new ContextValue { ValueType = ContextValueType.Simple, Value = v }; }

        // 액션 요소의 Owner·name 은 JSON 로드 때만 채워진다(Blasts.BuildRay·BM BlastStrike.cs:106-109 선례) → 직접 단다.
        static void Own(Element e, BlueprintAbility owner, string name) { e.name = name; e.Owner = owner; }

        static ContextActionApplyBuff ApplyPermanent(string buffGuid, BlueprintAbility owner, string name)
        {
            var a = new ContextActionApplyBuff
            {
                Permanent = true,
                IsNotDispelable = true, // 본편 SM 단일 능력 AfterSpawn 과 같다(정찰 §16-T5)
                DurationValue = new ContextDurationValue { Rate = (DurationRate)EngineCodes.DurationRateRounds, DiceType = DiceType.Zero, DiceCountValue = Simple(0), BonusValue = Simple(0) },
            };
            SetField(a, "m_Buff", Ref<BlueprintBuffReference>(buffGuid));
            Own(a, owner, name);
            if (a.Buff == null || a.Buff.AssetGuid.ToString() != buffGuid) throw new InvalidOperationException("Warlock: after-spawn buff " + buffGuid + " does not resolve");
            return a;
        }





        public static int HighestAvailableTier(Group group, int maximum)
        {
            if (!Built) throw new InvalidOperationException("Warlock: summons are not ready");
            for (int tier = maximum; tier > 0; tier--)
                if (variants.ContainsKey(DerivedGuids.Summon(group, tier))) return tier;
            throw new InvalidOperationException("Warlock: no available summon at or below tier " + maximum + " for " + group);
        }

        public static BlueprintAbility Variant(Group g, int tier)
        {
            BlueprintAbility a;
            if (!Built || !variants.TryGetValue(DerivedGuids.Summon(g, tier), out a)) throw new InvalidOperationException("Warlock: Summons.Variant(" + g + ", " + tier + ") before Summons.Build() or out of range");
            return a;
        }

        // 표식(변형 블루프린트의 WarlockSummonMarker): 0개 = 우리 변형 아님(null — 확정된 「아님」), 2개 이상·컴포넌트 배열 없음 = 던진다.
        public static WarlockSummonMarker MarkerOf(BlueprintAbility a)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (a.ComponentsArray == null) throw new InvalidOperationException("Warlock: " + a.name + " has a null ComponentsArray (cannot judge summon marker)");
            var m = a.ComponentsArray.OfType<WarlockSummonMarker>().ToArray();
            if (m.Length > 1) throw new InvalidOperationException("Warlock: " + a.name + " carries " + m.Length + " WarlockSummonMarker components");
            return m.Length == 1 ? m[0] : null;
        }

        // 최고 소환 단계(계획 T5 「Summons.MaxTier」): 위치 ClassData 의 주문서 → 유닛 주문서 → MaxSpellLevel → SummonRule.MaxTier(null/0 = 판정 불가).
        // 블루프린트를 인자로 넘기는 GetClassData/GetSpellbook 대신 참조 비교로 거른다(정찰 §16-T5 4). 같은 직업·주문서 2개 이상 = 손상 → 던진다.
        public static int? MaxTier(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            var witch = Get<BlueprintCharacterClass>(Guids.Game.WitchClass);
            var cds = u.Progression.Classes.Where(c => ReferenceEquals(c.CharacterClass, witch)).ToArray();
            if (cds.Length > 1) throw new InvalidOperationException("Warlock: " + u.CharacterName + " has " + cds.Length + " witch class data entries");
            if (cds.Length == 0 || cds[0].Spellbook == null) return SummonRule.MaxTier(null); // 판정 불가(위치 레벨·주문서 없음)
            var book = cds[0].Spellbook;
            var sbs = u.Descriptor.Spellbooks.Where(s => ReferenceEquals(s.Blueprint, book)).ToArray();
            if (sbs.Length > 1) throw new InvalidOperationException("Warlock: " + u.CharacterName + " has " + sbs.Length + " spellbooks " + book.name);
            return SummonRule.MaxTier(sbs.Length == 0 ? (int?)null : sbs[0].MaxSpellLevel);
        }

        // 위치 직업 레벨(멀티클래스면 위치 몫만 — 계획 Review Focus 5). 참조 비교로 거른다(위 MaxTier 와 같은 이유). 같은 직업 기록 2개 이상 = 손상 → 던진다, 0 = 0(SummonRule 이 레벨 범위 밖으로 던진다).
        public static int WitchLevel(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("u");
            var witch = Get<BlueprintCharacterClass>(Guids.Game.WitchClass);
            var cds = u.Progression.Classes.Where(c => ReferenceEquals(c.CharacterClass, witch)).ToArray();
            if (cds.Length > 1) throw new InvalidOperationException("Warlock: " + u.CharacterName + " has " + cds.Length + " witch class data entries");
            return cds.Length == 0 ? 0 : cds[0].Level;
        }

        static void RequireBuilt()
        {
            if (Pool == null || summonedBuff == null || dismemberedBuff == null) throw new InvalidOperationException("Warlock: Summons used before Summons.Build()");
            var root = Game.Instance.BlueprintRoot.SystemMechanics;
            if (!ReferenceEquals(root.SummonedUnitBuff, summonedBuff) || !ReferenceEquals(root.DismemberedUnitBuff, dismemberedBuff))
                throw new InvalidOperationException("Warlock: Guids.Game.SummonedUnitBuff/DismemberedUnitBuff do not match BlueprintRoot.SystemMechanics (PackWarden AuxUnits.cs:203-204 check)");
        }

         
        // 단계 = SummonRule.Phase(버프 보유, 소멸 중 버프 보유, 파괴, 풀 안 = true). Invalid 는 ERROR 1회 후 기록은 돌려준다(호출자가 Phase 로 거른다).
        public static SummonRecord[] OwnSummons(UnitEntityData caster)
        {
            if (caster == null) throw new ArgumentNullException("caster");
            RequireBuilt();
            var pool = Game.Instance.SummonPools.GetPool(Pool);
            if (pool == null) throw new InvalidOperationException("Warlock: SummonPoolsManager.GetPool(WarlockSummonPool) returned null");
            var list = new List<SummonRecord>();
            foreach (var u in pool.Units.ToList())
            {
                if (u == null) throw new InvalidOperationException("Warlock: WarlockSummonPool holds a null unit");
                var part = u.Get<UnitPartSummonedMonster>();
                var summoner = part == null ? null : part.Summoner;
                if (summoner == null) throw new InvalidOperationException("Warlock: summon pool unit " + u.CharacterName + " (" + u.UniqueId + ") has no summoner source (UnitPartSummonedMonster.Summoner is null)");
                if (summoner != caster) continue; // 정당한 무동작: 다른 시전자의 소환물(파티에 워락이 둘일 때)
                var phase = SummonRule.Phase(u.HasFact(summonedBuff), u.HasFact(dismemberedBuff), u.Destroyed, true);
                if (phase == SummonPhase.Invalid && invalidLogged.Add(PactLog.Generation(), u.UniqueId))
                    ModContext.LogError("Warlock: summon " + u.CharacterName + " (" + u.UniqueId + ") of " + caster.CharacterName + " is in the pool with an invalid buff state (SummonedUnitBuff " + u.HasFact(summonedBuff) + ", DismemberedUnitBuff " + u.HasFact(dismemberedBuff) + ")");
                list.Add(new SummonRecord(u, u.UniqueId, summoner, phase));
            }
            return list.ToArray();
        }

        // 비우기·대사자 종료가 쓰는 것: Active 만.
        public static SummonRecord[] OwnActive(UnitEntityData caster) { return OwnSummons(caster).Where(r => r.Phase == SummonPhase.Active).ToArray(); }

        // 소환물 소멸 = SummonedUnitBuff 제거(본편 ContextActionClearSummonPool @0000~0060 과 같은 UnitHelper.RemoveFact → OnRemoved → Dismembered 사슬). 되읽기.
        public static void Dismiss(UnitEntityData summon)
        {
            if (summon == null) throw new ArgumentNullException("summon");
            RequireBuilt();
            // SummonedUnitBuff.OnRemoved uses the same timed removal marker when the unit has no dismember prefab.
            // Apply it first only in that case, so the engine keeps its normal visual path for other summons.
            if (!summon.Destroyed && !summon.HasFact(dismemberedBuff))
            {
                var prefab = summon.Blueprint.Visual?.BloodPuddleFx?.Load();
                if (prefab == null || prefab.GetComponent<Kingmaker.Visual.Particles.DismemberUnitFX>() == null)
                {
                    var delay = TurnBased.Controllers.CombatController.IsInTurnBasedCombat()
                        ? TimeSpan.Zero : new Kingmaker.Utility.Rounds(1).Seconds;
                    summon.AddBuff(dismemberedBuff, summon, delay);
                    if (!summon.HasFact(dismemberedBuff)) throw new InvalidOperationException("Warlock: summon dismissal marker was not applied to " + summon.UniqueId);
                }
            }
            summon.RemoveFact(summonedBuff);
            if (summon.HasFact(summonedBuff)) throw new InvalidOperationException("Warlock: SummonedUnitBuff still on " + summon.CharacterName + " (" + summon.UniqueId + ") after RemoveFact");
        }

        // 막 소환된 유닛의 SummonedUnitBuff(정확히 1 — 아니면 던진다). RuleSummonUnit.OnTrigger @00D9 가 붙인다.
        public static Buff SummonBuffOf(UnitEntityData summon)
        {
            if (summon == null) throw new ArgumentNullException("summon");
            RequireBuilt();
            var bs = summon.Buffs.Enumerable.Where(b => ReferenceEquals(b.Blueprint, summonedBuff)).ToArray();
            if (bs.Length != 1) throw new InvalidOperationException("Warlock: summoned " + summon.CharacterName + " carries " + bs.Length + " SummonedUnitBuff(s), expected 1");
            return bs[0];
        }




    }

    // 소환 변형 표식(설계 §4.6). 빌드 때 정해지는 설정값(유닛 상태 아님).
    public class WarlockSummonMarker : BlueprintComponent
    {
        public Group Group;
        public int Tier;
    }

    // 항목 9 IX 강화의 「건강」 몫(소유자 결정 2026-11-17: 언데드는 매력 +4). 받는 능력치 = 순수 SummonRule.IxHealthStat(건강 활성?, 체력 기준) — 켜질 때 그 유닛에서 판정.
    // 본편 AddStatBonus.OnTurnOn @0011~008D(GetStat(Stat).AddModifierUnique(값, Runtime, 서술자))·OnTurnOff @0011~001C(RemoveModifiersFrom(Runtime))와 같은 수정치 경로.
    // 판정 불가면 SummonRule 이 던진다(엔진이 컴포넌트 예외를 기록한다 — 조용히 건강에 넣지 않는다). 끌 때는 두 능력치 모두에서 이 컴포넌트 출처 수정치를 지운다.
    public class WarlockIxHealthBonus : UnitFactComponentDelegate
    {
        public int Value;
        public ModifierDescriptor Descriptor;

        protected override void OnTurnOn()
        {
            var stats = Owner.Stats;
            var con = stats.Constitution;
            string want = SummonRule.IxHealthStat(con == null ? (bool?)null : con.Enabled, stats.HitPoints == null ? null : stats.HitPoints.BaseStat.ToString());
            var target = want == SummonRule.IxHealthStatCharisma ? (ModifiableValue)stats.Charisma : con;
            if (target == null) throw new InvalidOperationException("Warlock: IX boost target stat " + want + " is missing on " + Owner.CharacterName);
            target.AddModifierUnique(Value, Runtime, Descriptor);
        }

        protected override void OnTurnOff()
        {
            var stats = Owner.Stats;
            if (stats.Constitution != null) stats.Constitution.RemoveModifiersFrom(Runtime);
            if (stats.Charisma != null) stats.Charisma.RemoveModifiersFrom(Runtime);
        }
    }

    // 항목 4b: 소환물 직접 조종 불가(본편 ChangeFaction 의 PreventDirectControl Retain/Release 부분만 — 진영·그룹은 건드리지 않는다). 필드 0(상태는 엔진 깃발의 셈).
    public class WarlockNoDirectControl : UnitFactComponentDelegate
    {
        protected override void OnTurnOn() { Owner.PreventDirectControl.Retain(); }
        protected override void OnTurnOff() { Owner.PreventDirectControl.Release(); }
    }

    // 사용 제한(계획 T5 「WarlockHeatCost」, 정찰 §3-2): HeatCostRule.Decide(랭크, 단계, 최고 단계) == Ok 만 true. Unknown = false + ERROR 1회(유닛마다).
    // UI 가 매 프레임 부를 수 있다 → 예외는 잡아서 세고(HeatLog.Failed) ERROR 는 유닛마다 1회, 결과 false(시전 불가 — 조용한 허용 아님).
    public class WarlockHeatCost : BlueprintComponent, IAbilityRestriction
    {
        public int Tier;

        public bool IsAbilityRestrictionPassed(AbilityData ability)
        {
            UnitEntityData caster = null;
            try
            {
                if (ability == null) throw new ArgumentNullException("ability");
                caster = ability.Caster == null ? null : ability.Caster.Unit;
                if (caster == null) throw new InvalidOperationException("Warlock: heat cost restriction without a caster");
                var verdict = HeatCostRule.Decide(WarlockHeat.Rank(caster), Tier, Summons.MaxTier(caster));
                if (verdict == HeatCostVerdict.Unknown)
                    HeatLog.Emit(true, caster.UniqueId + "|RestrictionUnknown", "Warlock: " + caster.CharacterName + " heat summon restriction cannot judge the max tier (spellbook / max spell level unknown) — summons blocked");
                return HeatCostRule.Allows(verdict);
            }
            catch (Exception ex) { HeatLog.Failed("heat cost restriction", caster, ex, (caster == null ? "-" : caster.UniqueId) + "|RestrictionFailed"); return false; }
        }

        public string GetAbilityRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "Summon.Restriction"); }
    }

    // 과열 차감(계획 T5, R2-F03·F04 — 수동·자동 공통 액션 하나, C4 ⑥). 판정 = SpendRule.Plan. ActionList.Run 이 액션 예외를 삼키고 다음 액션(게이트)을 돌리므로(정찰 §16-T5)
    // 첫머리에서 LastDecision 을 지운다 — 이 액션이 어디서 실패해도 게이트는 승인을 못 본다. 예외는 잡아서 센다(HeatLog.Failed).
    public class ContextActionWarlockSpendHeat : ContextAction
    {
        public int Tier;

        public override string GetCaption() { return "Warlock: spend heat " + Tier; }

        public override void RunAction()
        {
            UnitEntityData caster = null;
            try
            {
                caster = Context.MaybeCaster;
                if (caster == null) throw new InvalidOperationException("Warlock: heat spend without a caster (Context.MaybeCaster is null)");
                var part = caster.Get<UnitPartWarlockAuto>();
                if (part != null) part.LastDecision = null;
                var ctx = Context.SourceAbilityContext;
                var ability = ctx == null ? null : ctx.Ability;
                var obs = new SpendObs
                {
                    HasAbilityContext = ability != null,
                    AbilityIsReserved = part != null && part.Reserved != null && ReferenceEquals(ability, part.Reserved),
                    AbilityIsObsolete = part != null && (AutoSummon.IsRetired(ability, part.Reserved) || (ability != null && part.Obsolete.Any(o => ReferenceEquals(o, ability)))),
                    PartExists = part != null,
                };
                var plan = SpendRule.Plan(obs, WarlockHeat.Rank(caster), Tier, caster.UniqueId);
                if (plan.Kind == SpendKind.MatchingReservation)
                {
                    if (!part.Owes(ability)) throw new InvalidOperationException("Warlock: matching reservation has no prepaid heat");
                }
                WarlockHeat.Write(caster, plan.NewRank);
                if (plan.ConsumeReservation) part.Reserved = null;
                if (plan.DropObsolete) part.Obsolete.RemoveAll(o => ReferenceEquals(o, ability));
                if (plan.OpenApproval) { part.ApprovedOpen = ability; part.ApprovedAt = HeatClock.Now(); }
                part.LastDecision = new SpendDecision(Context, ability, plan.Approved, plan.Kind, Tier);
                if (plan.Info != null) ModContext.Log("INFO " + plan.Info);
            }
            catch (Exception ex) { HeatLog.Failed("heat spend", caster, ex); }
        }
    }

    // 게이트 조건(계획 T5 「ConditionWarlockApproved」): 같은 실행 문맥 + 같은 능력 참조 + 승인. 판정 불가(시전자·부품·문맥 없음)는 false + 셈(예외를 엔진 로그로 넘기지 않는다).
    public class ConditionWarlockApproved : ContextCondition
    {
        protected override string GetConditionCaption() { return "Warlock: heat spend approved this execution"; }

        // 실패 계약 = 순수 SpendRule.GuardedApproval(던지면 false + 1번 셈 — 리뷰 T5 I-2, 시험 `exec: gate guard`).
        protected override bool CheckCondition()
        {
            UnitEntityData caster = null;
            return SpendRule.GuardedApproval(() =>
            {
                caster = Context == null ? null : Context.MaybeCaster;
                if (caster == null) throw new InvalidOperationException("Warlock: approval gate without a caster");
                var part = caster.Get<UnitPartWarlockAuto>();
                if (part == null) throw new InvalidOperationException("Warlock: approval gate without UnitPartWarlockAuto (the spend action did not run)");
                var ctx = Context.SourceAbilityContext;
                if (ctx == null || ctx.Ability == null) throw new InvalidOperationException("Warlock: approval gate without an ability context");
                var d = part.LastDecision;
                return d != null && ReferenceEquals(d.Context, Context) && ReferenceEquals(d.Ability, ctx.Ability) && d.Approved;
            }, ex => HeatLog.Failed("approval gate", caster, ex));
        }
    }

    // 소환 확인(리뷰 T5 m-4, 게이트 IfTrue 의 마지막): 판정 = SpendRule.AfterSpawn. 소환 사건이 승인 기록을 닫지 않았으면(소환 실패 — ActionList 가 예외를 삼킨다)
    // 쓴 과열을 되돌리고 승인 기록을 닫고 ERROR 1줄(다음 명중의 ApprovedTimedOut 대신). 예외는 잡아서 센다.
    public class ContextActionWarlockConfirmSummon : ContextAction
    {
        public override string GetCaption() { return "Warlock: confirm heat summon"; }

        public override void RunAction()
        {
            UnitEntityData caster = null;
            try
            {
                caster = Context.MaybeCaster;
                if (caster == null) throw new InvalidOperationException("Warlock: confirm summon without a caster");
                var part = caster.Get<UnitPartWarlockAuto>();
                if (part == null) throw new InvalidOperationException("Warlock: confirm summon without UnitPartWarlockAuto");
                var ctx = Context.SourceAbilityContext;
                var ability = ctx == null ? null : ctx.Ability;
                if (ability == null) throw new InvalidOperationException("Warlock: confirm summon without an ability context");
                var d = part.LastDecision;
                bool bound = d != null && ReferenceEquals(d.Context, Context) && ReferenceEquals(d.Ability, ability);
                bool isThis = ReferenceEquals(part.ApprovedOpen, ability);
                // 관측만 평평하게(리뷰 T6 I-1): 판정·적용 값은 전부 순수 SpendRule.Confirm 의 계획에서 온다(시험 `exec: confirm plan` · `wiring: confirm summon applies SpendRule.Confirm`).
                var obs = new ConfirmObs { DecisionBound = bound, ApprovalOpenIsThis = isThis, ApprovalOpenIsOther = part.ApprovedOpen != null && !isThis, Unit = caster.UniqueId };
                if (bound) { obs.Kind = d.Kind; obs.Tier = d.Tier; }
                obs.RankNow = WarlockHeat.Rank(caster);
                var plan = SpendRule.Confirm(obs);
                if (!plan.Failed) return; // 정당한 무동작: 소환됨(소환 사건이 승인 기록을 닫았다)
                if (d.Kind == SpendKind.MatchingReservation)
                {
                    if (!part.Owes(ability)) return;
                    part.Refund();
                }
                else WarlockHeat.Write(caster, plan.NewRank);
                if (plan.CloseApproval) part.ApprovedOpen = null;
                HeatLog.Emit(plan.ErrorIsError, null, plan.Error);
            }
            catch (Exception ex) { HeatLog.Failed("confirm summon", caster, ex); }
        }
    }

    // 같은 시전자 Active 소환물 비우기(계획 T5 ⑥ — 다른 시전자의 소환물은 그대로). Dismissing 은 이미 소멸 중(정당한 무동작).
    public class ContextActionWarlockClearOwnSummons : ContextAction
    {
        public override string GetCaption() { return "Warlock: clear own summons"; }

        public override void RunAction()
        {
            UnitEntityData caster = null;
            try
            {
                caster = Context.MaybeCaster;
                if (caster == null) throw new InvalidOperationException("Warlock: clear own summons without a caster");
                foreach (var r in Summons.OwnActive(caster)) Summons.Dismiss(r.Unit);
            }
            catch (Exception ex) { HeatLog.Failed("clear own summons", caster, ex); }
        }
    }

    // 소환 직후 처리(계획 T5 「WarlockSummonComponent」): 승인 기록 닫기 + 지속 조정(SummonRule.Shape) · 전투 종료 시 대사자 표식 소환물 소멸.
    public class WarlockSummonComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleSummonUnit>, IUnitCombatHandler, IGlobalSubscriber
    {
        public void OnEventAboutToTrigger(RuleSummonUnit evt) { } // 정당한 무동작: 조정은 소환 버프가 붙은 뒤(DidTrigger)

        public void OnEventDidTrigger(RuleSummonUnit evt)
        {
            try
            {
                var ctx = evt.Reason == null || evt.Reason.Context == null ? null : evt.Reason.Context.SourceAbilityContext;
                if (ctx == null || ctx.Ability == null) return; // 정당한 무동작: 능력 밖 소환(우리 변형은 늘 능력 문맥 안 — RuleCastSpell/수동 시전)
                var marker = Summons.MarkerOf(ctx.Ability.Blueprint);
                if (marker == null) return; // 정당한 무동작: 우리 변형이 아닌 소환(본편 SM 등)
                var part = Owner.Get<UnitPartWarlockAuto>();
                if (evt.SummonedUnit == null) throw new InvalidOperationException("Warlock: RuleSummonUnit without SummonedUnit after trigger");
                if (part != null) part.Spawned(ctx.Ability);
                if (part != null && SpendRule.ClosesApproval(part.ApprovedOpen, ctx.Ability)) part.ApprovedOpen = null;
                else HeatLog.Emit(true, null, "Warlock: " + Owner.CharacterName + " summon of tier " + marker.Tier + " without an open approval record (approved execution missing)");
                int level = Summons.WitchLevel(Owner);
                var shape = SummonRule.Shape(level, marker.Tier, Owner.HasFact(LevelFeatures.Marker(Guids.Extend16Feature)), Owner.HasFact(LevelFeatures.Marker(Guids.Capstone20Feature)));
                var buff = Summons.SummonBuffOf(evt.SummonedUnit);
                switch (shape.Kind)
                {
                    case SummonShapeKind.None: break; // 정당한 무동작: 액션의 기본 지속 = 위치 레벨 라운드
                    case SummonShapeKind.ExtendRounds:
                        {
                            var add = new Rounds(shape.ExtraRounds).Seconds;
                            var before = buff.EndTime;
                            buff.IncreaseDuration(add);
                            if (buff.IsPermanent || buff.EndTime != before + add) throw new InvalidOperationException("Warlock: IncreaseDuration readback (end " + before + " + " + add + " != " + buff.EndTime + ")");
                            break;
                        }
                    case SummonShapeKind.Permanent:
                        buff.MakePermanent();
                        if (!buff.IsPermanent) throw new InvalidOperationException("Warlock: MakePermanent readback failed");
                        if (evt.SummonedUnit.Descriptor.AddBuff(Summons.CapstoneBuff, Owner, null, null) == null) throw new InvalidOperationException("Warlock: AddBuff(WarlockCapstoneSummonBuff) returned null");
                        break;
                    default: throw new ArgumentOutOfRangeException("shape", shape.Kind, "Warlock: unknown summon shape");
                }
            }
            catch (Exception ex) { HeatLog.Failed("summon shaper", Owner, ex); }
        }

        public void HandleUnitJoinCombat(UnitEntityData unit) { } // 정당한 무동작

        // 대사자(L20): 전투 종료 = 표식 소환물 소멸(설계 §4.6 「전투 종료까지」).
        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            try
            {
                if (!HeatRule.IsOwnerEvent(unit, Owner)) return; // 정당한 무동작: 다른 유닛의 전투 이탈(전역 구독)
                foreach (var r in Summons.OwnActive(Owner)) if (r.Unit.HasFact(Summons.CapstoneBuff)) Summons.Dismiss(r.Unit);
            }
            catch (Exception ex) { HeatLog.Failed("capstone combat end", Owner, ex); }
        }
    }
}
