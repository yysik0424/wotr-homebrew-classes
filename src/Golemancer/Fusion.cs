using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;
using WotrClassMod.Common;



using static WotrClassMod.Common.BlueprintUtil;
using UnityModManager = UnityModManagerNet.UnityModManager;
using PolymorphTransitionSettings = Kingmaker.Visual.PolymorphTransitionSettings;

namespace Golemancer
{
     
    //   블루프린트: 토글 GolemancerFusionToggle{제한(IgnoreIfStarted), 시작기} · 제어 버프(컴포넌트 0 — 「토글이 시작됐다」는 엔진 표식) ·
    //     형태 버프 13{Polymorph(재질 프리팹·크기·슬램 2·m_Facts = 도발 2), SpellDescriptor Polymorph} · 수치 특성 13(Materials.BuildFusionStats) ·
    //     합체 특성{AddFacts[토글], 흡수기} → Archetype.AddAt(Dials.FusionLevel).
    //   상태: 주인 파트 UnitPartGolemFusion([JsonProperty] State/HiddenByUs/PendingSteps/PetId — WSS 와 같다). 파트는 시작기에서만 만든다(R2-C01).
    //   허용 차이 원장(시험 `copy: fusion adapter diff ledger`): 아래 「원장 ②~⑱」 표식이 그 자리다. 원장 ① = 개명·접두(UnitPartSpiritFusion → UnitPartGolemFusion, GameFusionWorld → GameGolemWorld, 문구 접두 Golemancer).
    public static class Fusion
    {
        public static BlueprintActivatableAbility Toggle;
        public static BlueprintBuff ControlBuff;
        public static BlueprintFeature Feature;
        static readonly Dictionary<string, BlueprintBuff> forms = new Dictionary<string, BlueprintBuff>(); // 버프 키 → 형태 버프

        public static IEnumerable<BlueprintBuff> AllForms { get { RequireBuilt(); return forms.Values; } }
        public static BlueprintBuff FormOf(string buffKey) { RequireBuilt(); BlueprintBuff b; if (!forms.TryGetValue(buffKey, out b)) throw new KeyNotFoundException("Golemancer: no fusion form for key " + buffKey); return b; }
        public static bool IsForm(BlueprintScriptableObject bp) { RequireBuilt(); return bp is BlueprintBuff && forms.Values.Contains((BlueprintBuff)bp); }
        public static string KeyOfForm(BlueprintBuff bp)
        {
            RequireBuilt();
            string key = null;
            int matches = 0;
            foreach (var entry in forms)
            {
                if (!ReferenceEquals(entry.Value, bp)) continue;
                key = entry.Key;
                matches++;
            }
            if (matches != 1) throw new InvalidOperationException("Golemancer: " + (bp == null ? "null" : bp.name) + " is not one of our fusion forms");
            return key;
        }

        static void RequireBuilt() { if (Toggle == null || ControlBuff == null || Feature == null || forms.Count == 0) throw new InvalidOperationException("Golemancer: Fusion.Build() has not run"); }

        // ================= 블루프린트 =================
        public static void Build()
        {
            if (Toggle != null) throw new InvalidOperationException("Golemancer: Fusion.Build() called twice");
            if (Materials.Selections == null || Taunt.AreaBuff == null || GolemMaster.Feature == null) throw new InvalidOperationException("Golemancer: Fusion.Build() before Materials/Taunt/GolemMaster");
            FusionDamageLedger.RequireEngineMembers(); // 확정 원장의 리플렉션 대상(훅 목록·상한 설정자)이 없으면 빌드 실패(시작 때 시끄럽게 — 흡수가 조용히 꺼지지 않게)
            // 아이콘 원천 = 본편 CavalierChallengeAbility(m_Icon 있음 — 시험 `jbp: fusion icon source has an icon`). 옛 원천(도발 선례 버프 d064c2b4…)은
            // 본편에서 m_Icon = null 이라 설치 빌드의 BuildAll 이 매번 여기서 실패했다(2026-11-14 Player.log). 새 원천도 없으면 이름 붙여 실패한다.
            var icon = Get<BlueprintAbility>(Guids.Game.FusionIconSource).Icon;
            if (icon == null) throw new InvalidOperationException("Golemancer: fusion icon source CavalierChallengeAbility (" + Guids.Game.FusionIconSource + ") has no icon (game update or another mod cleared it)");

            Materials.BuildFusionStats(); // 1-6 수치 13(기존 FusionStats* GUID)

            // 1-3 제어 버프: 컴포넌트 0(FusionController·흡수기·Polymorph 없음 — R1·I7). StayOnDeath·서술자·지속 없음.
            ControlBuff = Create<BlueprintBuff>("GolemancerFusionBuff", Guids.FusionControlBuff, b =>
            {
                SetField(b, "m_DisplayName", Strings.L("FusionBuff.Name"));
                SetField(b, "m_Description", Strings.L("FusionBuff.Description"));
                SetField(b, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(b, "m_Icon", icon);
                b.Stacking = StackingType.Replace;
                EmptyFxOnRemove(b); // 2026-11-17: 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — Buff.OnRemove @00B0 NRE)
            });
            if (ControlBuff.ComponentsArray.Length != 0 || ControlBuff.GetComponent<Polymorph>() != null || ControlBuff.StayOnDeath)
                throw new InvalidOperationException("Golemancer: fusion control buff readback failed (components " + ControlBuff.ComponentsArray.Length + ", must be 0 and no Polymorph)");

            // 1-5 형태 13. 변신 FX = 본편 원소 신체의 외부 전환 에셋(FusionFxTable — 없거나 비면 여기서 던진다).
            var transition = ElementalTransition();
            foreach (var key in MaterialCatalog.Keys) forms.Add(key, BuildForm(key, transition));

            // 1-1·1-2·1-4 토글.
            Toggle = Create<BlueprintActivatableAbility>("GolemancerFusionToggle", Guids.FusionToggle, a =>
            {
                SetField(a, "m_DisplayName", Strings.L("FusionToggle.Name"));
                SetField(a, "m_Description", Strings.L("FusionToggle.Description"));
                SetField(a, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(a, "m_Icon", icon);
                a.ActivationType = AbilityActivationType.WithUnitCommand;
                SetField(a, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Swift);
                a.DeactivateImmediately = true;
                a.Group = ActivatableAbilityGroup.None;
                a.IsOnByDefault = false;
                a.DoNotTurnOffOnRest = false;
                // R6 + 판정 (b): 기절·마비·빈사·의식 없음·전투 종료에도 유지.
                a.DeactivateIfCombatEnded = false;
                a.DeactivateIfOwnerDisabled = false;
                a.DeactivateIfOwnerUnconscious = false;
                a.OnlyInCombat = false;
                a.DeactivateAfterFirstRound = false;
            });
            SetField(Toggle, "m_Buff", Ref<BlueprintBuffReference>(Guids.FusionControlBuff));
            AddComponents(Toggle, new GolemancerFusionRestriction { IgnoreIfStarted = true }, new GolemancerFusionStarter());
            var tt = Toggle.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!tt.SequenceEqual(new[] { typeof(GolemancerFusionRestriction), typeof(GolemancerFusionStarter) }) || !Toggle.GetComponent<GolemancerFusionRestriction>().IgnoreIfStarted
                || Toggle.ActivationType != AbilityActivationType.WithUnitCommand || Toggle.ActivateWithUnitCommandType != UnitCommand.CommandType.Swift || !Toggle.DeactivateImmediately
                || Toggle.Group != ActivatableAbilityGroup.None || Toggle.IsOnByDefault || Toggle.DoNotTurnOffOnRest || !ReferenceEquals(Toggle.Buff, ControlBuff)
                || Toggle.DeactivateIfCombatEnded || Toggle.DeactivateIfOwnerDisabled || Toggle.DeactivateIfOwnerUnconscious || Toggle.OnlyInCombat || Toggle.DeactivateAfterFirstRound || Toggle.ActivateOnCombatStarts)
                throw new InvalidOperationException("Golemancer: fusion toggle readback failed (components [" + string.Join(", ", tt.Select(t => t.Name).ToArray()) + "], flags)");

            // 1-7 특성: AddFacts[토글] + 흡수기, 랭크 1, 재적용 false, 10레벨.
            Feature = Create<BlueprintFeature>("GolemancerFusionFeature", Guids.FusionFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("FusionFeature.Name"));
                SetField(f, "m_Description", Strings.L("FusionFeature.Description"));
                SetField(f, "m_DescriptionShort", Strings.L("FusionFeature.Description"));
                SetField(f, "m_Icon", icon);
                f.IsClassFeature = true;
                f.Ranks = 1;
                f.ReapplyOnLevelUp = false;
            });
            var grant = new AddFacts();
            SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.FusionToggle) });
            AddComponents(Feature, grant, new GolemancerFusionAbsorber());
            var ft = Feature.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!ft.SequenceEqual(new[] { typeof(AddFacts), typeof(GolemancerFusionAbsorber) }) || grant.Facts.Count() != 1 || !ReferenceEquals(grant.Facts.First(), Toggle) || Feature.Ranks != 1 || Feature.ReapplyOnLevelUp
                || Feature.ComponentsArray.OfType<Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite>().Any())
                throw new InvalidOperationException("Golemancer: fusion feature readback failed (components [" + string.Join(", ", ft.Select(t => t.Name).ToArray()) + "], Ranks, ReapplyOnLevelUp)");
            int level = GolemRules.Dials.FusionLevel;
            Archetype.AddAt(level, Feature);
            var levels = Archetype.Bp.AddFeatures.Where(e => e.Features.Contains(Feature)).Select(e => e.Level).ToArray();
            if (levels.Length != 1 || levels[0] != level) throw new InvalidOperationException("Golemancer: fusion feature archetype levels [" + string.Join(",", levels) + "], expected [" + level + "]");
        }

        // 합체 변신 FX 원천(FusionFxTable): 본편 흙 원소 신체 IV 버프의 Polymorph.m_TransitionExternal 을 읽어 확인한다. 본편 객체에는 쓰지 않는다(참조만 공유).
        //   원천 없음·형식 다름 = Get 이 던진다 · 이름 다름·Polymorph ≠ 1개·외부 에셋 없음(또는 파괴됨)·Enter/Exit 설정 null·PrefabFX AssetId 빈칸 = 여기서 던진다.
        //   PrefabFX 프리팹 자체는 불러 보지 않는다(빌드 시점 로드 없음 — AssetId 존재까지만 확인, 게임 안 모습은 미확인).
        //   재질 버프 13(Materials.BuildBuff, 2026-11-15 승인)도 같은 에셋을 쓴다 — 사용처 = FusionFxTable.Users.
        internal static PolymorphTransitionSettings ElementalTransition()
        {
            var src = Get<BlueprintBuff>(FusionFxTable.SourceGuid);
            if (src.name != FusionFxTable.SourceName) throw new InvalidOperationException("Golemancer: fusion FX source " + FusionFxTable.SourceGuid + " is '" + src.name + "', expected " + FusionFxTable.SourceName);
            var polys = src.ComponentsArray.OfType<Polymorph>().ToArray();
            if (polys.Length != 1) throw new InvalidOperationException("Golemancer: fusion FX source " + src.name + " has " + polys.Length + " Polymorph component(s), expected 1");
            var f = FindField(typeof(Polymorph), FusionFxTable.TransitionField);
            if (f == null || f.FieldType != typeof(PolymorphTransitionSettings)) throw new InvalidOperationException("Golemancer: Polymorph." + FusionFxTable.TransitionField + " missing or not PolymorphTransitionSettings (game update?)");
            var ext = f.GetValue(polys[0]) as PolymorphTransitionSettings;
            if (!ext) throw new InvalidOperationException("Golemancer: fusion FX source " + src.name + " has no " + FusionFxTable.TransitionField + " (expected asset " + FusionFxTable.ExternalAssetGuid + ") — no elemental transformation FX");
            if (ext.EnterTransition == null || ext.ExitTransition == null) throw new InvalidOperationException("Golemancer: elemental transition asset '" + ext.name + "' has a null " + (ext.EnterTransition == null ? "EnterTransition" : "ExitTransition"));
            var ids = new[] { ext.EnterTransition.OldPrefabFX, ext.EnterTransition.NewPrefabFX, ext.ExitTransition.OldPrefabFX, ext.ExitTransition.NewPrefabFX }.Select(l => l == null ? null : l.AssetId).ToArray(); // RequiredInside 순서
            var problems = FusionFxTable.InsideProblems(ids);
            if (problems.Count != 0) throw new InvalidOperationException("Golemancer: elemental transition asset '" + ext.name + "' (from " + src.name + "): " + string.Join(" | ", problems.ToArray()));
            return ext;
        }

        static BlueprintBuff BuildForm(string key, PolymorphTransitionSettings transition)
        {
            if (!transition) throw new ArgumentNullException("transition", "Golemancer: fusion form " + key + " needs the elemental transition asset");
            var m = MaterialCatalog.MaterialOfKey(key);
            var slam = Materials.SlamOf(key);
            var b = Create<BlueprintBuff>("GolemancerFusionForm_" + key.Replace("@", "_"), MaterialCatalog.FusionFormGuid(key), x =>
            {
                SetField(x, "m_DisplayName", Strings.L("FusionBuff.Name"));
                SetField(x, "m_Description", Strings.L("FusionBuff.Description"));
                SetField(x, "m_DescriptionShort", NoText()); // 2026-11-16: 짧은 설명 없음 = 본편 jbp 의 빈 키(null 이면 "<null>" 이 찍힌다)
                SetField(x, "m_Flags", Enum.Parse(FindField(typeof(BlueprintBuff), "m_Flags").FieldType, "HiddenInUi"));
                x.Stacking = StackingType.Replace;
                // 2026-11-17(GameLogFull: GolemFusionSequence.RemoveAll → GameGolemWorld.RemoveForms → 형태 버프 제거 → Buff.OnRemove @00B0 NRE 16건): 빈 FxOnRemove.
                EmptyFxOnRemove(x);
            });
            var poly = new Polymorph { Size = (Size)Enum.Parse(typeof(Size), m.Size), StrengthBonus = 0, DexterityBonus = 0, ConstitutionBonus = 0, NaturalArmor = 0 }; // 수치는 합체 수치 특성(R2-03)
            SetField(poly, "m_Prefab", new UnitViewLink { AssetId = MaterialCatalog.PrefabOf(m) });
            SetField(poly, "m_MainHand", Ref<BlueprintItemWeaponReference>(MaterialCatalog.SlamGuid(key)));
            SetField(poly, "m_OffHand", Ref<BlueprintItemWeaponReference>(MaterialCatalog.SlamGuid(key))); // 슬램 2회(설계 §4.7)
            SetField(poly, "m_AdditionalLimbs", new BlueprintItemWeaponReference[0]);
            SetField(poly, "m_SecondaryAdditionalLimbs", new BlueprintItemWeaponReference[0]);
            SetField(poly, "m_KeepSlots", false);
            // T7: 20레벨 형태 키면 재질별 면역 공유 버프 1개를 더한다(Capstone.FormExtraFactGuids — 순수 CapstoneRules.FormGetsShared).
            var formFacts = new[] { Guids.TauntAreaBuff, Guids.TauntMarkerBuff }.Concat(Capstone.FormExtraFactGuids(key)).ToArray();
            SetField(poly, "m_Facts", formFacts.Select(g => Ref<BlueprintUnitFactReference>(g)).ToArray());
            SetField(poly, "m_EnterTransition", new Polymorph.VisualTransitionSettings()); // null 이면 StartTransition @0046 NRE(T4 D12)
            SetField(poly, "m_ExitTransition", new Polymorph.VisualTransitionSettings());
            SetField(poly, FusionFxTable.TransitionField, transition); // 엔진은 이것이 있으면 인라인 전환(위 두 줄) 대신 이것을 쓴다(FusionFxTable 머리말)
            AddComponents(b, poly, new SpellDescriptorComponent { Descriptor = new SpellDescriptorWrapper(SpellDescriptor.Polymorph) });
            var back = b.GetComponent<Polymorph>();
            if (!ReferenceEquals(FindField(typeof(Polymorph), FusionFxTable.TransitionField).GetValue(back), transition))
                throw new InvalidOperationException("Golemancer: fusion form " + b.name + " transition FX readback failed");
            var facts = ((BlueprintUnitFactReference[])FindField(typeof(Polymorph), "m_Facts").GetValue(back)).Select(r => r.Guid.ToString()).ToArray();
            var desc = b.GetComponent<SpellDescriptorComponent>();
            if (b.ComponentsArray.Length != 2 || back.Size.ToString() != m.Size || ((UnitViewLink)FindField(typeof(Polymorph), "m_Prefab").GetValue(back)).AssetId != MaterialCatalog.PrefabOf(m)
                || !ReferenceEquals(back.MainHand, slam) || !ReferenceEquals(back.OffHand, slam) || !facts.SequenceEqual(formFacts)
                || desc == null || desc.Descriptor.Value != SpellDescriptor.Polymorph || b.StayOnDeath)
                throw new InvalidOperationException("Golemancer: fusion form " + b.name + " readback failed");
            return b;
        }

        // ================= 진단 =================
        static readonly UnknownOnce unknownLogged = new UnknownOnce();

        internal static void LogUnknown(UnitEntityData u, string what, Exception ex)
        {
            string id = (object)u == null ? "?" : u.UniqueId;
            var mp = (object)u == null ? null : u.Get<UnitPartGolemMaster>();
            if (mp != null) { mp.Error(u, "UNKNOWN " + what + " — fusion blocked (" + ex.GetType().Name + ": " + ex.Message + ")"); return; } // 점검 기록 + 같은 내용 1회 로그
            if (unknownLogged.First(UnknownOnce.KeyOf(id, what, ex)))
                ModContext.LogError("Golemancer: UNKNOWN " + what + " for " + ((object)u == null ? "?" : u.CharacterName) + " — fusion blocked (" + ex.GetType().Name + ": " + ex.Message + ")");
        }

        static readonly RepeatCounter errorLogged = new RepeatCounter();

        // 같은 원인의 ERROR 는 1회(매 틱 반복이 로그를 덮지 않게). 점검 기록(파트 Trace)이 있으면 매번 센다.
        internal static void Error(UnitEntityData u, string what)
        {
            var mp = (object)u == null ? null : u.Get<UnitPartGolemMaster>();
            if (mp != null) mp.Error(u, "fusion: " + what);
            else
            {
                int n = errorLogged.Hit(((object)u == null ? "?" : u.UniqueId) + "|" + what);
                if (RepeatLog.ShouldLog(n)) ModContext.LogError("Golemancer: fusion: " + RepeatLog.Text(what, n) + " (" + ((object)u == null ? "?" : u.CharacterName) + ")");
            }
        }

        internal static void Warn(UnitEntityData u, string what) { var mp = u.Get<UnitPartGolemMaster>(); if (mp != null) mp.Warn("fusion: " + what); else ModContext.Log("WARN Golemancer: fusion: " + what + " (" + u.CharacterName + ")"); }
        internal static void Info(UnitEntityData u, string what) { var mp = u.Get<UnitPartGolemMaster>(); if (mp != null) mp.Info("fusion: " + what); else ModContext.Log("Golemancer: fusion: " + what + " (" + u.CharacterName + ")"); }

        // ================= 관측(§2-4) =================
        public static bool IsPreview(UnitEntityData u)
        {
            if ((object)u == null) throw new ArgumentNullException("u");
            return !string.IsNullOrEmpty(u.PreviewOf.Id); // setter: UnitHelper.CopyInternal @0041 · Respec @009F · UnitPartLocustClonePets(§2-1)
        }

        public static ActivatableAbility ToggleOf(UnitEntityData master)
        {
            var t = TogglesOf(master);
            if (t.Count != 1) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has " + t.Count + " fusion toggle fact(s), expected exactly 1");
            return t[0];
        }

        public static List<ActivatableAbility> TogglesOf(UnitEntityData master) { RequireBuilt(); return master.ActivatableAbilities.Enumerable.Where(IsOurToggle).ToList(); }

        // 토글이 정확히 1개면 그것, 아니면 null(0 = 특성·토글 소실, 2+ = 중복 — 관측이 행 12 로 분류한다).
        public static ActivatableAbility SingleToggleOrNull(UnitEntityData master) { var t = TogglesOf(master); return t.Count == 1 ? t[0] : null; }

        public static List<Buff> ControlsOf(UnitEntityData u) { RequireBuilt(); return u.Buffs.Enumerable.Where(IsOurControl).ToList(); }
        public static List<Buff> FormsOn(UnitEntityData u) { RequireBuilt(); return u.Buffs.Enumerable.Where(IsOurForm).ToList(); }

        public static List<EntityFact> FusionStatFacts(UnitEntityData u)
        {
            return Materials.OwnedStatFacts(u).Where(IsFusionStat).ToList();
        }

        // 기대 형태 키 = 골렘에 적용된 (재질, 레벨) → 버프 키(I5). 적용 기록이 없으면 판정 불가 → throw.
        public static string ExpectedKeyOf(UnitEntityData master)
        {
            var lp = GolemMaster.PartOf(master);
            if (string.IsNullOrEmpty(lp.AppliedKey) || lp.AppliedLevel < GolemRules.MinLevel) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has no applied golem material (AppliedKey " + (lp.AppliedKey ?? "null") + ", level " + lp.AppliedLevel + ")");
            return StatRecipe.BuffKey(MaterialCatalog.Get(lp.AppliedKey), lp.AppliedLevel);
        }

        struct BuffObservation
        {
            public int Count;
            public Buff Single;
            public bool Active, Suppressed;
        }

        static BuffObservation ReadBuffs(List<Buff> raw, Func<Buff, bool> match)
        {
            if (raw == null) throw new ArgumentNullException("source");
            var result = new BuffObservation();
            foreach (var buff in raw)
            {
                if (!match(buff)) continue;
                result.Count++;
                result.Single = result.Count == 1 ? buff : null;
                result.Active = (result.Count == 1 || result.Active) && buff.IsActive && buff.IsTurnedOn;
                result.Suppressed = result.Suppressed || buff.IsSuppressed;
            }
            return result;
        }

        static ActivatableAbility ReadToggle(FusionObs o, List<ActivatableAbility> raw)
        {
            if (raw == null) throw new ArgumentNullException("source");
            ActivatableAbility single = null;
            o.ToggleCount = 0;
            foreach (var ability in raw)
            {
                if (!IsOurToggle(ability)) continue;
                o.ToggleCount++;
                single = o.ToggleCount == 1 ? ability : null;
                o.IsStarted = o.IsStarted || ability.IsStarted;
            }
            o.IsOn = single != null && single.IsOn;
            return single;
        }

        static bool PendingCommand(UnitCommand command, ActivatableAbility toggle)
        {
            if (command == null || toggle == null) return false;
            var activate = command as UnitActivateAbility;
            return StartPhase.IsPendingActivate(activate != null, activate != null && ReferenceEquals(activate.Ability, toggle), command.IsFinished, command.IsActed);
        }

        static bool HasPendingCommand(UnitCommands commands, ActivatableAbility toggle)
        {
            var raw = commands.Raw;
            if (raw == null) throw new ArgumentNullException("source");
            foreach (var command in raw) if (PendingCommand(command, toggle)) return true;
            var queue = commands.Queue;
            if (queue == null) throw new ArgumentNullException("source");
            foreach (var command in queue) if (PendingCommand(command, toggle)) return true;
            return false;
        }

        static void ReadStatFacts(FusionObs o, List<EntityFact> facts)
        {
            if (facts == null) throw new ArgumentNullException("source");
            o.StatCount = 0;
            foreach (var fact in facts)
            {
                if (!Materials.IsOwnedStatFact(fact)) continue;
                var stat = MaterialCatalog.StatKeyOfFeature(fact.Blueprint.AssetGuid.ToString());
                if (stat.Value)
                {
                    o.StatCount++;
                    o.StatKey = o.StatCount == 1 ? stat.Key : null;
                }
                else o.StatHasPetSet = true;
            }
        }

        public static FusionObs Observe(UnitEntityData master)
        {
            if ((object)master == null) throw new ArgumentNullException("master");
            RequireBuilt();
            if (IsPreview(master)) return new FusionObs { IsPreview = true };
            var o = new FusionObs();
            var part = master.Get<UnitPartGolemFusion>(); // Ensure 하지 않는다(R2-C01 — 파트는 시작기에서만)
            o.HasPart = part != null;
            var rawBuffs = master.Buffs.Enumerable;
            var controls = ReadBuffs(rawBuffs, IsOurControl);
            if (part != null)
            {
                o.State = part.Runtime.Gate.State; // 손상 → throw
                o.SwapPending = part.Flags.SwapPending;
                o.SelfFormOp = part.Flags.SelfFormOp.Active;
                o.FormLostPending = part.Flags.FormLostPending;
                o.StopRequestedIsControl = part.Flags.StopRequestedIs(controls.Single);
            }
            else o.State = FusionState.Idle;
            o.HasFeature = master.Descriptor.HasFact(Feature);
            var toggle = ReadToggle(o, master.ActivatableAbilities.Enumerable);
            var applied = toggle == null ? null : toggle.AppliedBuff;
            o.AppliedIsNull = applied == null;
            o.AppliedIsControl = applied != null && (controls.Count == 1 ? ReferenceEquals(controls.Single, applied)
                : controls.Count > 1 && IsOurControl(applied) && rawBuffs.Contains(applied));
            o.PendingActivateCommand = HasPendingCommand(master.Commands, toggle);
            o.ControlCount = controls.Count;
            o.ControlActive = controls.Active;
            o.ControlSuppressed = controls.Suppressed;
            var form = ReadBuffs(rawBuffs, IsOurForm);
            o.FormCount = form.Count;
            o.FormActive = form.Active;
            o.FormSuppressed = form.Suppressed;
            o.FormKey = form.Count == 1 ? KeyOfForm(form.Single.Blueprint) : null;
            var poly = master.GetActivePolymorph();
            o.ActivePolymorphPresent = poly.Component != null;
            o.ActivePolymorphIsOurs = poly.Component != null && IsForm(poly.Component.OwnerBlueprint);
            o.BodyPolymorphed = master.Body.IsPolymorphed;
            ReadStatFacts(o, master.Facts.List);
            if (o.HasPart && o.State == FusionState.Active)
            {
                o.GolemHidden = !part.Pet().IsInGame; // 골렘 미관측 → throw
                o.Taunt = Golemancer.Taunt.SourceOf(master); // 반쪽 → throw
                o.ExpectedKey = ExpectedKeyOf(master);   // AppliedKey null → throw
            }
            return o;
        }

        // ================= 판정 Fusion.Check(§6-1, 원장 ⑮) =================
        public static GolemFusionBlock Check(UnitEntityData master, CheckPhase phase)
        {
            if ((object)master == null || Toggle == null) { LogUnknown(master, "fusion: check (no master or Fusion.Build() not run)", new InvalidOperationException("precondition")); return GolemFusionBlock.Unknown; }
            if (IsPreview(master)) throw new InvalidOperationException("Golemancer: Fusion.Check on a preview unit (the restriction must answer false first)");
            UnitPartGolemMaster lp;
            FusionObs obs;
            try { lp = GolemMaster.PartOf(master); obs = Observe(master); }
            catch (Exception ex) { LogUnknown(master, "fusion: observation", ex); return GolemFusionBlock.Unknown; }

            string key;
            try { key = GolemMaster.ObservedMaterialKey(master); }
            catch (Exception ex) { LogUnknown(master, "fusion: material key", ex); return GolemFusionBlock.Unknown; }
            if (key == null) { LogUnknown(master, "fusion: material key", new InvalidOperationException("observed material key is null (0 or 2+ material features)")); return GolemFusionBlock.Unknown; }

            bool? golemOk;
            try
            {
                var pet = master.GetPet(PetType.AnimalCompanion);
                golemOk = GolemPresence.IsPresentForAbility(master, lp, pet) && lp.GolemId != null && pet.UniqueId == lp.GolemId && GolemMaster.IsOurGolem(pet) && !pet.State.IsDead;
            }
            catch (Exception ex) { LogUnknown(master, "fusion: golem", ex); golemOk = null; }

            bool? savePending = SavePending();
            if (!savePending.HasValue) LogUnknown(master, "fusion: save state", new InvalidOperationException("Game.Instance / SaveManager missing"));
            bool? gateCanStart;
            try { var part = master.Get<UnitPartGolemFusion>(); gateCanStart = part == null ? (bool?)null : part.Runtime.Gate.CanStart; }
            catch (Exception ex) { LogUnknown(master, "fusion: part state", ex); gateCanStart = null; obs = null; }
            if (obs == null) return GolemFusionBlock.Unknown;
            try { return FusionCheckInputs.Decide(obs, phase, lp.Life.State, lp.Broken, golemOk, savePending, gateCanStart); }
            catch (Exception ex) { LogUnknown(master, "fusion: check", ex); return GolemFusionBlock.Unknown; }
        }

         
        public static bool? SavePending()
        {
            var g = Game.Instance;
            if (g == null || g.SaveManager == null) return null;
            return g.SaveManager.CurrentState != SaveManager.State.None;
        }

        // ================= 본체 형태·수치 교정(§3-3, 판정 (a), 원장 ⑬ — GolemMaster.CurrentFusion·Effector.Materials/Readback 확장과 짝) =================
        public static void ReconcileMaster(UnitEntityData master, string buffKey)
        {
            if ((object)master == null) throw new ArgumentNullException("master");
            if (string.IsNullOrEmpty(buffKey)) throw new ArgumentNullException("buffKey");
            var part = master.Get<UnitPartGolemFusion>();
            if (part == null) throw new InvalidOperationException("Golemancer: ReconcileMaster without a fusion part on " + master.CharacterName);
            var owned = Materials.OwnedStatFacts(master);
            var fusionStats = FusionStatFacts(master);
            if (owned.Count != fusionStats.Count) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " carries golem (pet) stat features");
            var fs = FormsOn(master);
            var plan = MaterialPlan.Reconcile(fs.Select(b => KeyOfForm(b.Blueprint)).ToArray(), fusionStats.Select(f => MaterialCatalog.StatKeyOfFeature(f.Blueprint.AssetGuid.ToString()).Key).ToArray(), buffKey, buffKey);
            var world = part.World;
            part.WithSelfFormOp(() =>
            {
                if (plan.RemoveBuffs.Length > 0 || plan.AddBuff != null)
                {
                    foreach (var b in fs) if (plan.RemoveBuffs.Contains(KeyOfForm(b.Blueprint))) { GameGolemWorld.RequireSettled(b); b.Remove(); }
                    if (plan.AddBuff != null) world.AddForm(buffKey);
                    world.ReadbackForm(buffKey);
                }
                foreach (var f in FusionStatFacts(master)) Materials.RemoveStat(master, f);
                Materials.AddStat(master, Materials.StatSet.Fusion, buffKey);
            });
            // 추가 슬램 사지(소유자 결정 2026-11-16): 합체 중 본체 = max(합체 손 슬램 2, 표 값) − 2 — 평소에는 0, 폭주 중에만 1·2(SlamRules.MasterExtraLimbs).
            SlamLimbs.Reconcile(master, SlamLimbs.WantedForMaster(master, SlamLimbs.AlchemistLevel(master), Overdrive.IsActive(master)));
            Info(master, "master form/stat reconciled to " + buffKey + " (" + (plan.AddBuff != null ? "form changed" : "form kept") + ")");
        }

        // Effector.Readback 확장(Active): 형태 키·수치 1·formOk·크기·슬램·도발 원천.
        public static void ReadbackMaster(UnitEntityData master, string buffKey)
        {
            var o = Observe(master);
            o.ExpectedKey = buffKey; // Apply 도중이라 적용 기록(AppliedKey·Level)은 아직 이전 값이다 — 기대 키는 이번 Apply 의 키
            if (o.FormKey != buffKey || o.StatCount != 1 || o.StatKey != buffKey || o.StatHasPetSet || !FusionInvariant.FormOk(o))
                throw new InvalidOperationException("Golemancer: fused master readback " + o + ", expected form/stat " + buffKey);
            var part = master.Get<UnitPartGolemFusion>();
            part.World.ReadbackForm(buffKey);
        }

        // ================= Tick 불변식(§3-4, 원장 ⑭) =================
        // 구독 확인 일정(LedgerVerifySchedule — 매 프레임 리플렉션 금지, 시험 `perf:`) · 틱 유닛 목록 버퍼(매 프레임 ToList 대신 재사용 — Player.AllCharacters 는 내부 목록이라
        // 틱 도중 UpdateCharacterLists 가 다시 채울 수 있어 직접 순회하지 않고 스냅숏을 뜬다. 재진입하면 바깥 foreach 가 「Collection was modified」로 시끄럽게 실패한다).
        static readonly LedgerVerifySchedule ledgerVerify = new LedgerVerifySchedule();
        static readonly FusionAbsentFacts<EntityFact> absentFacts = new FusionAbsentFacts<EntityFact>();
        static readonly List<UnitEntityData> tickUnits = new List<UnitEntityData>();

        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            if (Feature == null || ModContext.BuildFailed) return; // 정당한 무동작: 빌드 안 됨
            var game = Game.Instance;
            if (game == null || game.Player == null || game.CurrentlyLoadedArea == null || game.State == null) { absentFacts.SetRoster(null, 0); ledgerVerify.NotInGame(); return; } // 정당한 무동작: 지역 없음(다음 게임 틱에 구독 재확인)
            if (LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess) { absentFacts.SetRoster(null, 0); ledgerVerify.NotInGame(); return; } // 정당한 무동작: 로딩 중(불러오기·지역 이동 뒤 재확인)
            // 확정 원장: 정해진 시점의 구독 확인 + 수명 위반 기록 정산(Fix round 1 C01 — 기록은 Tick 경계를 넘기지 못한다).
            var area = game.LoadedAreaState;
            try { VerifyLedgerIfDue(area); FusionDamageLedger.Sweep(); }
            catch (Exception ex) { ModContext.LogError("Golemancer: fusion damage ledger tick threw " + ex); }
            tickUnits.Clear();
            tickUnits.AddRange(game.Player.AllCharacters);
            try
            {
                absentFacts.SetRoster(area, tickUnits.Count);
                for (int slot = 0; slot < tickUnits.Count; slot++)
                {
                    var u = tickUnits[slot];
                    if ((object)u == null || u.Destroyed || IsPreview(u)) continue; // 정당한 무동작: 대상 아님
                    try
                    {
                        if (!IsFusionTickTarget(u, slot)) continue;
                        u.Get<UnitPartGolemFusion>()?.FinishRemovedBuff();
                        TickUnit(u);
                        u.Get<UnitPartGolemOverdrive>()?.FinishFormSync();
                    }
                    catch (Exception ex) { LogUnknown(u, "fusion: tick", ex); }
                }
            }
            finally { tickUnits.Clear(); } // 유닛 참조를 프레임 사이에 붙잡지 않는다
        }

        // 구독 확인 관문: 일정이 Due 일 때만 EnsureSubscribed(없으면 다시 걸고 ERROR, 다시 걸어도 안 되면 throw). 확인을 마친 뒤에만 Verified —
        // 실패하면 예외가 Tick 의 ERROR 로 가고 일정은 Due 로 남아 다음 틱에 다시 확인한다(예전 매 틱 동작과 같은 실패 모양).
        internal static void VerifyLedgerIfDue(object areaState)
        {
            if (!ledgerVerify.Due(areaState)) return; // 정당한 무동작: 이 지역 상태에서 이미 확인했다(사이 구간 해지 경로 없음 — LedgerVerifySchedule 주석)
            FusionDamageLedger.EnsureSubscribed();
            ledgerVerify.Verified(areaState);
        }

        // Tick 대상(Fix round 1 I03): 특성이 없어도 합체 파트나 산출물(토글·제어·형태·수치)이 남아 있으면 본다 — 「특성 없음 = 건너뜀」은 합체 중 특성 소실을 숨긴다.
        // 파트가 없고 직전 정밀 검사에서 융합 흔적이 없었던 유닛은 같은 지역·같은 팩트 목록 버전에서만 건너뛴다.
        // HasFusionFacts는 변경 직후와 구형 저장의 첫 검사에서 토글·제어·형태·수치를 모두 검사한다.
        internal static bool IsFusionTickTarget(UnitEntityData u, int slot)
        {
            if (u.Get<UnitPartGolemFusion>() != null) return true;
            var facts = u.Facts.List;
            int version;
            if (absentFacts.KnownAbsent(slot, facts, out version)) return false;
            if (HasFusionFacts(u)) return true;
            absentFacts.Remember(slot, facts, version);
            return false;
        }

        static bool HasFusionFacts(UnitEntityData u)
        {
            return FusionTick.IsTarget(u.Descriptor.HasFact(Feature), false, CountTogglesOf(u), CountControlsOf(u), CountFormsOn(u), CountFusionStatFacts(u));
        }

        static readonly Func<ActivatableAbility, bool> IsOurToggle = a => ReferenceEquals(a.Blueprint, Toggle);
        static readonly Func<Buff, bool> IsOurControl = b => ReferenceEquals(b.Blueprint, ControlBuff);
        static readonly Func<Buff, bool> IsOurForm = b => forms.ContainsValue(b.Blueprint);   // 예전 forms.Values.Contains(x) = ICollection.Contains = ContainsValue(같은 비교자)
        static readonly Func<EntityFact, bool> IsFusionStat = f => MaterialCatalog.StatKeyOfFeature(f.Blueprint.AssetGuid.ToString()).Value;

        internal static int CountTogglesOf(UnitEntityData master) { RequireBuilt(); return FusionTick.CountMatching(master.ActivatableAbilities.Enumerable, IsOurToggle); }
        internal static int CountControlsOf(UnitEntityData u) { RequireBuilt(); return FusionTick.CountMatching(u.Buffs.Enumerable, IsOurControl); }
        internal static int CountFormsOn(UnitEntityData u) { RequireBuilt(); return FusionTick.CountMatching(u.Buffs.Enumerable, IsOurForm); }
        internal static int CountFusionStatFacts(UnitEntityData u)
        {
            if (u == null) throw new ArgumentNullException("unit"); // 예전 OwnedStatFacts 와 같은 첫머리
            return FusionTick.CountFusionStats(u.Facts.List, Materials.IsOwnedStatFact, IsFusionStat);
        }

        internal static TickOutcome TickUnit(UnitEntityData u)
        {
            return FusionTick.Run(Observe(u), new GameTickWorld(u)); // 관측이 먼저, 판정·동작 순서는 순수 FusionTick
        }

        // 고아 정리(§2-3): 제어·형태 Remove(안전 규칙), 수치 회수, 토글 끔(시작돼 있으면 Stop — m3), 되읽기.
        internal static void CleanupOrphan(UnitEntityData u)
        {
            var part = u.Get<UnitPartGolemFusion>();
            Action a = () =>
            {
                foreach (var b in FormsOn(u)) { GameGolemWorld.RequireSettled(b); b.Remove(); }
                foreach (var b in ControlsOf(u)) { GameGolemWorld.RequireSettled(b); b.Remove(); }
                foreach (var f in FusionStatFacts(u)) Materials.RemoveStat(u, f);
            };
            if (part != null) part.WithSelfFormOp(a); else a();
            TurnTogglesOff(u);
            var ts = TogglesOf(u);
            int on = ts.Count(t => t.IsOn), started = ts.Count(t => t.IsStarted);
            if (!FusionTick.CleanupDone(FormsOn(u).Count, ControlsOf(u).Count, FusionStatFacts(u).Count, on, started))
                throw new InvalidOperationException("Golemancer: orphan cleanup readback failed on " + u.CharacterName + " (forms " + FormsOn(u).Count + ", controls " + ControlsOf(u).Count + ", stats " + FusionStatFacts(u).Count + ", toggles on " + on + " started " + started + ")");
        }

        // 토글 끔: 켜져 있으면 IsOn=false(OnDidTurnOff → Stop), 이미 꺼졌는데 시작돼 있으면(행 10) TurnOffImmediately(@004C SetIsOn(false) → @0053 Stop).
        internal static void TurnTogglesOff(UnitEntityData u)
        {
            foreach (var t in TogglesOf(u))
            {
                if (t.IsOn) t.IsOn = false;
                if (t.IsStarted) t.TurnOffImmediately();
            }
        }

        // ================= 저장 전 분리(§5) =================
        public static void EndAllBeforeSave()
        {
            if (Feature == null || ModContext.BuildFailed) return; // 정당한 무동작: 빌드 안 됨
            var game = Game.Instance;
            if (game == null || game.Player == null) return; // 정당한 무동작: 게임 상태 없음(메인 메뉴 저장 없음)
            foreach (var u in game.Player.AllCharacters.ToList())
            {
                if ((object)u == null || u.Destroyed || IsPreview(u)) continue; // 정당한 무동작: 대상 아님
                var part = u.Get<UnitPartGolemFusion>();
                if (part == null) continue; // 정당한 무동작: 합체한 적 없음
                try
                {
                    var s = part.Runtime.Gate.State;
                    if (s == FusionState.Idle || s == FusionState.AwaitRestore) continue; // 정당한 무동작: 저장해도 되는 상태
                    part.DeliverPreSave();
                    var after = part.Runtime.Gate.State;
                    if (after != FusionState.Idle) Error(u, "pre-save end left the fusion " + after + " (pending " + part.Runtime.Gate.PendingSteps + ") — saved as is, retried on load");
                }
                catch (Exception ex) { Error(u, "pre-save end threw " + ex.GetType().Name + ": " + ex.Message); }
            }
        }





        // 개체의 엔진 파트 목록 전부(EntityPartsManager.m_Parts — 비공개, 게터 get_Parts 도 private). 2026-11-17 결함 수리:
        // GetAll<EntityPart>() 는 UpdateCache<TPart> @0014~001B 가 TPart == EntityPart 면 즉시 return 해 캐시 칸을 만들지 않고, GetAll @0017 LinqExtensions.Get(기본 null) → null 을 돌려준다
        // (GetAll(pred) 이터레이터 d__14 @0025 도 같은 GetAll 을 부른다). 그래서 OnPreSave 의 「저장 중 파트 집합 불변」 단언이 합체 중 저장 등에서 ArgumentNullException 으로 죽을 수 있었다.
        // EntityPartsManager.PreSave @0001 이 바로 이 m_Parts 를 원본 열거한다 → 불변을 단언할 대상도 m_Parts 다. 못 읽으면(필드 없음·형식 다름·null) 던진다.
        internal static List<EntityPart> EnginePartsOf(EntityDataBase entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            if (entity.Parts == null) throw new InvalidOperationException("Golemancer: " + entity + " has no EntityPartsManager");
            var f = PartsListField();
            var list = f.GetValue(entity.Parts) as List<EntityPart>;
            if (list == null) throw new InvalidOperationException("Golemancer: EntityPartsManager.m_Parts of " + entity + " is null");
            return list.ToList(); // 스냅숏(비교하는 동안 원본이 바뀌어도 그때의 집합을 본다)
        }

        // EntityPartsManager.m_Parts(형식 List<EntityPart>, dnlib 2026-11-17 .ctor @0001~0006). 빌드 때 FusionDamageLedger.RequireEngineMembers 가 한 번 확인한다.
        internal static System.Reflection.FieldInfo PartsListField()
        {
            var f = typeof(EntityPartsManager).GetField("m_Parts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f == null) throw new MissingFieldException("Golemancer: EntityPartsManager.m_Parts not found (game update?)");
            if (f.FieldType != typeof(List<EntityPart>)) throw new InvalidOperationException("Golemancer: EntityPartsManager.m_Parts is " + f.FieldType + ", expected List<EntityPart>");
            return f;
        }
    }

    // ================= 파트(§2-2) — WSS UnitPartSpiritFusion 복제 + 원장 =================
    public class UnitPartGolemFusion : UnitPart, IInGameHandler, IUnitBuffHandler, IUnitLifeStateChanged, IAreaHandler, IPolymorphDeactivatedHandler, IActivatableAbilityWillStopHandler, IGlobalSubscriber
    {
        [JsonProperty] public int State;
        [JsonProperty] public bool HiddenByUs;
        [JsonProperty] public int PendingSteps;
        [JsonProperty] public string PetId;

        // 런타임 전용(저장 안 함 — 로드 때 비어 있다: 저장 전 분리).
        // 교체 대기(3-3) · 형태 변신 해제 기록(R2-I01) · WillStop 제어(R2-I03) · 우리 형태 조작 깊이 — 판정·기록은 순수 FusionRuntimeFlags(Fix round 1, 오프라인 실행 시험).
        [JsonIgnore] readonly FusionRuntimeFlags flags = new FusionRuntimeFlags();
        [JsonIgnore] int stopPendingSeen;           // 진단(INFO) — StopPending 관측 수

        [JsonIgnore] FusionRuntime runtime;
        [JsonIgnore] FusionEventKind? pendingBuffEnd;
        [JsonIgnore] string corrupt;
        [JsonIgnore] GameGolemWorld world;
        [JsonIgnore] int runDepth;
        [JsonIgnore] FusionState runEntry;
        [JsonIgnore] bool endedInRun;               // 시작 실패 정리(StartFailureEnd)가 같은 실행 안에서 Idle 로 되돌렸다

        [JsonIgnore] public FusionRuntimeFlags Flags { get { return flags; } }
        [JsonIgnore] public bool SwapPending { get { return flags.SwapPending; } }
        [JsonIgnore] public bool FormLostPending { get { return flags.FormLostPending; } }
        [JsonIgnore] public Buff StopRequestedControl { get { return flags.StopRequested as Buff; } }
        [JsonIgnore] public int StopPendingSeen { get { return stopPendingSeen; } }
        [JsonIgnore] internal GameGolemWorld World { get { var r = Runtime; return world; } }

        bool Preview { get { return Fusion.IsPreview(Owner); } }

        // 지금 상태: 런타임이 있으면 관문 상태(실행 중에는 저장 필드가 아직 갱신 전이다), 없으면 저장값. 순수 읽기(관측 아님).
        internal FusionState CurState { get { return PartState.Current(runtime == null ? (FusionState?)null : runtime.Gate.State, State); } }

        public FusionRuntime Runtime
        {
            get
            {
                if (runtime != null) return runtime;
                if (corrupt != null) throw new InvalidOperationException("Golemancer: fusion part on " + Owner.CharacterName + " could not be restored: " + corrupt);
                world = new GameGolemWorld(this);
                try { runtime = FusionRuntime.Restore(world, (FusionState)State, HiddenByUs, (FusionStep)PendingSteps); }
                catch (Exception ex) { corrupt = ex.Message; ModContext.LogError("Golemancer: fusion part restore failed on " + Owner.CharacterName + ": " + ex.Message); throw; }
                return runtime;
            }
        }

        void Store()
        {
            if (runtime == null) return; // 정당한 무동작: 런타임이 만들어지지 않았으면 필드는 저장값 그대로다.
            State = (int)runtime.Gate.State;
            HiddenByUs = runtime.Gate.HiddenByUs;
            PendingSteps = (int)runtime.Gate.PendingSteps;
        }

        // 사건 처리 공통(WSS Run + 원장 ⑧): 가장 바깥 실행이 Idle 이 아니던 상태에서 Idle 로 끝나면 런타임 필드를 비우고 종료 뒤 도발을 맞춘다.
        void Run(string what, Action a)
        {
            if (runDepth == 0) { runEntry = runtime == null ? (FusionState)State : runtime.Gate.State; endedInRun = false; }
            runDepth++;
            try { a(); }
            catch (Exception ex) { ModContext.LogError("Golemancer: fusion " + what + " on " + Owner.CharacterName + " threw: " + ex); throw; }
            finally
            {
                Store();
                runDepth--;
                if (runDepth == 0 && runtime != null && (runEntry != FusionState.Idle || endedInRun) && runtime.Gate.State == FusionState.Idle) AfterEnd(what);
            }
        }

        // 원장 ⑧(R4): ToIdle 뒤 Feed(Poll) → FusionTauntAfterEnd → Taunt.Reconcile. 실패는 ERROR(원래 예외를 가리지 않게 여기서 삼키지 않고 기록만 — finally 안이다).
        void AfterEnd(string what)
        {
            ClearRuntime();
            try
            {
                GolemMaster.Feed(Owner, LifeEventKind.Poll);
                var obs = GolemMaster.Observe(Owner);
                var lp = GolemMaster.PartOf(Owner);
                var expected = FusionTauntAfterEnd.Expected(obs.GolemLife, lp.Life.State);
                var golem = GolemMaster.GolemOf(Owner, obs.GolemId);
                Golemancer.Taunt.Reconcile(Owner, golem, expected);
                Fusion.Info(Owner, "fusion ended (" + what + ") -> Idle, taunt " + expected);
            }
            catch (Exception ex) { Fusion.Error(Owner, "after-end taunt reconcile (" + what + ") threw " + ex.GetType().Name + ": " + ex.Message); }
        }

        internal void ClearRuntime() { pendingBuffEnd = null; flags.Clear(); }

        // Removing other buffs inside IUnitBuffHandler unsubscribes their components while EventBus is enumerating.
        internal void FinishRemovedBuff()
        {
            if (!pendingBuffEnd.HasValue) return;
            var reason = pendingBuffEnd.Value;
            pendingBuffEnd = null;
            if (CurState == FusionState.Active)
                DeliverEnd(reason);
        }
        internal void ConsumeFormLost() { flags.ConsumeFormLost(); }
        internal void CountStopPending(string info) { stopPendingSeen++; if (stopPendingSeen == 1 || DevFlags.ChecksActive) Fusion.Info(Owner, info); }

        internal void WithSelfFormOp(Action a)
        {
            using (flags.SelfFormOp.Enter()) a();
        }

        // 원장 ④: 시작기만 부른다(파트 생성도 시작기에서만).
        internal void BeginWith(UnitEntityData golem, MechanicsContext context)
        {
            Run("start", () =>
            {
                var gate = Runtime.Gate;
                if (!gate.CanStart) throw new InvalidOperationException("Golemancer: fusion Start while " + gate.State + (gate.LoadPending ? " (load pending)" : ""));
                PetId = golem.UniqueId;
                world.StartContext = context;
                try { gate.Start(); }
                catch (Exception) { if (gate.State == FusionState.Idle) endedInRun = true; throw; } // 정리 뒤 Idle → 종료 뒤 도발을 되돌린다(Begin 이 골렘 도발을 떼었다)
                finally { world.StartContext = null; }
            });
        }

        // 원장 ③: OnPreSave = 검출만(PreSaveDetector). 종료는 FusionPreSavePatch(SaveRoutine 앞)가 한다. 강신 전제 삭제.
        protected override void OnPreSave()
        {
            if (Preview) return; // 정당한 무동작: 미리보기
            Run("pre-save", () =>
            {
                var before = Fusion.EnginePartsOf(Owner).Cast<object>().ToList();
                var verdict = PreSaveDetector.Decide(Runtime.Gate.State);
                if (verdict == PreSaveVerdict.ErrorKeep) Fusion.Error(Owner, "pre-save detector: fusion still " + Runtime.Gate.State + " at EntityPart.PreSave (save path outside SaveRoutine — respec / area unload / serialize) — kept");
                var after = Fusion.EnginePartsOf(Owner).Cast<object>().ToList();
                if (!FusionGate.SameIdentitySet(before, after)) throw new InvalidOperationException("Golemancer: fusion pre-save changed the master's part set " + before.Count + " -> " + after.Count);
            });
        }

        internal void DeliverPreSave() { Run("pre-save end", () => Runtime.Gate.PreSave()); }

        protected override void OnPostLoad()
        {
            base.OnPostLoad();
            if (Preview) return; // 정당한 무동작: 미리보기(복사 원본의 저장값을 들고 있다)
            Run("post-load", () => { var r = Runtime; });
        }

        public void OnAreaDidLoad() { if (Preview) return; Run("area-did-load", () => Runtime.Gate.AreaDidLoad()); } // 미리보기 = 정당한 무동작

        public void OnAreaBeginUnloading() { if (Preview) return; Run("area-unloading", () => Runtime.Gate.Deliver(FusionEvent.Of(FusionEventKind.AreaUnloading))); } // 미리보기 = 정당한 무동작

        public void HandleObjectInGameChanged(EntityDataBase entity)
        {
            // 정당한 무동작: 우리가 숨긴 골렘이 없으면(Idle) 외부 해제란 없다 — 전역 사건이라 모든 개체가 지나간다.
            if (!HiddenByUs || PetId == null || Preview) return;
            var u = entity as UnitEntityData;
            if ((object)u == null || u.UniqueId != PetId || !u.IsInGame) return; // 정당한 무동작: 다른 개체이거나 숨겨지는 쪽 변화
            Run("external-unhide", () => Runtime.Gate.Deliver(FusionEvent.Of(FusionEventKind.ExternalUnhide)));
        }

        // 원장 ⑫: 제어 → ControlRemoval, 형태 → FormRemoval. 콜백 안에서 몸의 변신 해제를 단언하지 않는다(Detach 뒤에 풀린다).
        public void HandleBuffDidRemoved(Buff buff)
        {
            if (buff.Owner == null || !ReferenceEquals(buff.Owner.Unit, Owner) || Preview) return; // 정당한 무동작: 다른 유닛(전역 사건)·미리보기. buff.Owner 는 UnitDescriptor(UnitFact.get_Owner @0001-000C) — 참조 비교는 .Unit 으로
            if (ReferenceEquals(buff.Blueprint, Fusion.ControlBuff))
            {
                var state = CurState;
                var t = Fusion.SingleToggleOrNull(Owner); // 특성·토글 소실(I03) = null → 아래 분류가 End(시작 없음·적용 없음)
                if (t == null && state == FusionState.Active) Fusion.Error(Owner, "control buff removed while fused but the fusion toggle fact is gone (" + Fusion.TogglesOf(Owner).Count + " toggle facts) — ending the fusion");
                var kind = ControlRemoval.Classify(state, t != null && t.IsStarted, t == null || t.AppliedBuff == null, t != null && ReferenceEquals(t.AppliedBuff, buff), flags.StopRequestedIs(buff));
                if (flags.OnControlRemoved(kind))
                {
                    if (!pendingBuffEnd.HasValue) pendingBuffEnd = FusionEventKind.DurationOver;
                    return;
                }
                if (kind == ControlRemovalKind.Swap) Fusion.Info(Owner, "control buff swap (level-up ReapplyBuff) — fusion continues");
                return; // Swap = 계속, Ignore = 정당한 무동작(우리 종료 중·Idle)
            }
            if (Fusion.IsForm(buff.Blueprint))
            {
                var kind = FormRemoval.Classify(CurState, flags.SelfFormOp.Active, Owner.Descriptor.State.IsDead);
                if (kind == FormRemovalKind.Ignore) return; // 정당한 무동작: 우리 교체·종료 또는 Active 아님
                if (kind == FormRemovalKind.DurationOver) Fusion.Warn(Owner, "fusion form removed from outside (" + buff.Blueprint.name + ") — ending the fusion");
                if (kind == FormRemovalKind.ShamanDead || !pendingBuffEnd.HasValue)
                    pendingBuffEnd = kind == FormRemovalKind.ShamanDead ? FusionEventKind.ShamanDead : FusionEventKind.DurationOver;
            }
            // 정당한 무동작: 다른 버프
        }

        public void HandleBuffDidAdded(Buff buff)
        {
            if (Preview) return; // 정당한 무동작: 미리보기
            if (flags.OnControlAdded(buff.Owner != null && ReferenceEquals(buff.Owner.Unit, Owner) && ReferenceEquals(buff.Blueprint, Fusion.ControlBuff)))
                Fusion.Info(Owner, "control buff swap completed (new control attached)");
            // 정당한 무동작: 교체가 아닌 추가
        }

        // 원장 ⑰(R2-I01): 기록만 — 삭제·종료·관측 금지(바깥 Deactivate 중, Polymorph.OnDeactivate @00F2).
        public void OnPolymorphDeactivated(UnitEntityData unit, Polymorph polymorph)
        {
            if (polymorph == null) return; // 정당한 무동작: 엔진이 넘긴 변신 없음
            flags.OnPolymorphDeactivated(ReferenceEquals(unit, Owner), Fusion.IsForm(polymorph.OwnerBlueprint), CurState); // 기록 판정은 순수(주인·우리 형태·우리 조작 아님·Active)
        }

        // 원장 ⑱(R2-I03): 기록만. EndTime 은 판정에 쓰지 않는다.
        public void HandleActivatableAbilityWillStop(ActivatableAbility ability)
        {
            if (ability == null) return; // 정당한 무동작
            bool ours = ReferenceEquals(ability.Blueprint, Fusion.Toggle) && ReferenceEquals(ability.Owner.Unit, Owner);
            var state = CurState;
            if (flags.OnWillStop(ours, state, ability.AppliedBuff)) Fusion.Info(Owner, "manual stop requested — control removal follows on the next buff tick");
            else if (ours && state == FusionState.Active) Fusion.Error(Owner, "WillStop while Active but the toggle's AppliedBuff is null");
        }

        public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        {
            // 정당한 무동작: 다른 유닛, 또는 죽음 아닌 변화(기절·마비·빈사·의식 없음은 종료 아님 — R6·판정 (b)).
            if (!ReferenceEquals(unit, Owner) || Preview || !Owner.Descriptor.State.IsDead) return;
            Run("master-dead", () => Runtime.Gate.Deliver(FusionEvent.Of(FusionEventKind.ShamanDead)));
        }

        internal void DeliverEnd(FusionEventKind k) { Run("end " + k, () => Runtime.Gate.Deliver(FusionEvent.Of(k))); }

        internal AbsorbCalc AbsorbCalc(int incoming)
        {
            AbsorbCalc r = null;
            Run("absorb-calc", () => { r = Runtime.Calc(incoming); });
            return r;
        }

        internal void AbsorbDeal(AbsorbCalc c) { Run("absorb-deal", () => Runtime.Deal(c)); }

        // Rebase(Fix round 1 m7): 계산 뒤 골렘 HP 가 바뀐 정당한 경우 — 지금 HP 로 나눈 몫만 골렘에 쓰고, Active 면 코어에 같은 Hit 를 넣는다(넘치면 PetDowned).
        internal void AbsorbDealRebased(int petHpNow, int share)
        {
            Run("absorb-deal-rebased", () =>
            {
                if (share > 0) world.DamagePet(share);
                if (Runtime.Gate.AbsorbAllowed) Runtime.Gate.Deliver(FusionEvent.Hit(petHpNow, share));
                else world.Log("WARN", "fusion left Active between damage calculation and damage (" + Runtime.Gate.State + ") — " + share + " applied to the golem HP only");
            });
        }

        // 원장 ②: 해석된 골렘 = PetId 와 같은 주인의 골렘. 못 찾으면 throw — 단계가 StepFailed 로 남는다.
        internal UnitEntityData Pet()
        {
            var pet = Owner.GetPet(PetType.AnimalCompanion);
            FusionPetIdentity.Require(PetId, pet == null ? null : pet.UniqueId, pet != null && GolemMaster.IsOurGolem(pet));
            return GolemMaster.GolemOf(Owner, PetId);
        }
    }

    // ================= IFusionWorld 게임 구현 + 원시 동작(원장 ⑤·⑥·⑦) =================
    public sealed class GameGolemWorld : IFusionWorld, IGolemFusionPrims
    {
        readonly UnitPartGolemFusion part;
        internal MechanicsContext StartContext; // Begin 동안만(제어 버프 문맥)

        public GameGolemWorld(UnitPartGolemFusion part)
        {
            if (part == null) throw new ArgumentNullException("part");
            this.part = part;
        }

        UnitEntityData Master { get { return part.Owner; } }

        // 안전 경계(§2-5): 지울 사실이 활성화·비활성화 중이면 지우지 않고 던진다(→ StepFailed → AwaitRestore ERROR).
        internal static void RequireSettled(EntityFact f)
        {
            if (f.Activating || f.Deactivating) throw new InvalidOperationException("Golemancer: refusing to remove " + f.Blueprint.name + " while it is " + (f.Activating ? "activating" : "deactivating"));
        }

        // 원장 ⑤
        public void AddFusionBuff()
        {
            if (StartContext == null) throw new InvalidOperationException("Golemancer: AddFusionBuff outside the start handler");
            GolemFusionSequence.Begin(this);
        }

        // 원장 ⑥
        public int RemoveFusionBuffs()
        {
            int n = 0;
            part.WithSelfFormOp(() => { n = GolemFusionSequence.RemoveAll(this); });
            return n;
        }

        public int FusionBuffCount() { return Fusion.FormsOn(Master).Count + Fusion.ControlsOf(Master).Count + Fusion.FusionStatFacts(Master).Count; }

        // 원장 ⑦: 골렘술사에는 강신 기록이 없다.
        public int PossessionRecordCount() { return 0; }
        public void EndPossessionRecords() { throw new InvalidOperationException("Golemancer: EndPossessionRecords called — the golemancer has no possession records"); }

        public void SetPetHidden(bool hidden)
        {
            var pp = part.Pet().Get<UnitPartPet>();
            if (pp == null) throw new InvalidOperationException("Golemancer: golem has no UnitPartPet");
            pp.SetHidden(hidden);
        }

        public bool PetInGame() { return part.Pet().IsInGame; }
        public int PetHpLeft() { return part.Pet().Descriptor.HPLeft; }

        public void DamagePet(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException("amount");
            var d = part.Pet().Descriptor;
            int want = d.Damage + amount;
            d.Damage = want;
            if (d.Damage != want) throw new InvalidOperationException("Golemancer: golem damage readback " + d.Damage + " != " + want);
        }

        public void SettleLifeState() { UnitLifeController.ForceTickOnUnit(part.Pet()); }
        public bool PetDead() { return part.Pet().Descriptor.State.IsDead; }
        public string PetLifeState() { return part.Pet().Descriptor.State.LifeState.ToString(); }

        public void Log(string level, string message)
        {
            if (level == "INFO") Fusion.Info(Master, message);
            else if (level == "WARN") Fusion.Warn(Master, message);
            else Fusion.Error(Master, level + " " + message);
        }

        // ---- 원시 동작(GolemFusionSequence 가 순서를 정한다) ----
        public void AssertControlIsApplied()
        {
            var t = Fusion.ToggleOf(Master);
            var c = Fusion.ControlsOf(Master);
            if (c.Count != 1 || !ReferenceEquals(c[0], t.AppliedBuff)) throw new InvalidOperationException("Golemancer: fusion start: control buffs " + c.Count + ", applied " + (t.AppliedBuff == null ? "null" : "other") + " (expected exactly the applied control)");
        }

        public void DetachGolemTaunt() { Golemancer.Taunt.Reconcile(Master, part.Pet(), TauntSource.None); }

        public string ReadbackGolemKey()
        {
            string key = Fusion.ExpectedKeyOf(Master);
            var golemKeys = Materials.OurBuffs(part.Pet()).Select(Materials.KeyOfBuff).ToArray();
            if (golemKeys.Length != 1 || golemKeys[0] != key) throw new InvalidOperationException("Golemancer: golem material buffs [" + string.Join(",", golemKeys) + "], expected exactly " + key);
            return key;
        }

        public void AddForm(string key)
        {
            var b = Master.Descriptor.AddBuff(Fusion.FormOf(key), Master, null);
            if (b == null) throw new InvalidOperationException("Golemancer: AddBuff(fusion form " + key + ") returned null on " + Master.CharacterName);
            // The buff-added event can precede the polymorph replacing the hands and additional limbs.
            if (Overdrive.IsActive(Master)) Overdrive.Sync(Master, true, "fusion body attached");
        }

        // 형태 되읽기: formOk(관측) + 크기 == 재질 크기, 주·보조 손 == 그 키의 슬램, 도발 원천 == 본체. 이미 남의 변신 중이면 Polymorph.OnActivate 가 로그만 남기고 반환하므로 여기서 잡힌다.
        public void ReadbackForm(string key)
        {
            var fs = Fusion.FormsOn(Master);
            var poly = Master.GetActivePolymorph();
            var m = MaterialCatalog.MaterialOfKey(key);
            var slam = Materials.SlamOf(key);
            var main = Master.Body.PrimaryHand.MaybeWeapon;
            var off = Master.Body.SecondaryHand.MaybeWeapon;
            var src = Golemancer.Taunt.SourceOf(Master);
            if (fs.Count != 1 || Fusion.KeyOfForm(fs[0].Blueprint) != key || !fs[0].IsActive || fs[0].IsSuppressed || poly.Component == null || !ReferenceEquals(poly.Component.OwnerBlueprint, fs[0].Blueprint) || !Master.Body.IsPolymorphed
                || Master.State.Size.ToString() != m.Size || main == null || off == null || !ReferenceEquals(main.Blueprint, slam) || !ReferenceEquals(off.Blueprint, slam) || src != TauntSource.Master)
                throw new InvalidOperationException("Golemancer: fusion form readback on " + Master.CharacterName + ": forms " + fs.Count + ", polymorph " + (poly.Component == null ? "none" : poly.Component.OwnerBlueprint.name)
                    + ", size " + Master.State.Size + " (" + m.Size + "), hands " + (main == null ? "-" : main.Blueprint.name) + "/" + (off == null ? "-" : off.Blueprint.name) + " (" + slam.name + "), taunt " + src);
        }

        public void AddStat(string key) { Materials.AddStat(Master, Materials.StatSet.Fusion, key); }

        public void ClearRuntime() { part.ClearRuntime(); }

        public int RemoveForms()
        {
            var list = Fusion.FormsOn(Master);
            foreach (var b in list) { RequireSettled(b); b.Remove(); }
            if (list.Count > 0 && Overdrive.IsActive(Master))
                Overdrive.Sync(Master, true, "fusion body detached");
            return list.Count;
        }

        public int RemoveControls()
        {
            var list = Fusion.ControlsOf(Master);
            foreach (var b in list) { RequireSettled(b); b.Remove(); }
            return list.Count;
        }

        public int RemoveStats()
        {
            var owned = Materials.OwnedStatFacts(Master);
            var list = Fusion.FusionStatFacts(Master);
            if (owned.Count != list.Count) throw new InvalidOperationException("Golemancer: " + Master.CharacterName + " carries golem (pet) stat features");
            foreach (var f in list) { RequireSettled(f); Materials.RemoveStat(Master, f); }
            return list.Count;
        }
    }

    // ================= 토글 제한(§1-2, 원장 ⑨ — WSS FusionAction·FusionRestriction(능력 시전 제한) 대신) =================
    // IgnoreIfStarted = true: 시작 뒤 get_IsAvailableByRestrictions @005A/@0067 가 이 제한을 건너뛴다(OnNewRound·OnAttack 이 Active 를 끄지 않는다). 시작 전에는 평가된다(TryStart @0011).
    public class GolemancerFusionRestriction : ActivatableAbilityRestriction
    {
        static readonly UnknownOnce once = new UnknownOnce();

        protected override bool IsAvailable()
        {
            var u = Owner;
            try
            {
                if (Fusion.IsPreview(u)) return false; // 정당한 무동작: 미리보기 유닛
                return Fusion.Check(u, CheckPhase.Steady) == GolemFusionBlock.None;
            }
            catch (Exception ex)
            {
                if (once.First(UnknownOnce.KeyOf((object)u == null ? "?" : u.UniqueId, "fusion restriction", ex))) ModContext.LogError("Golemancer: UNKNOWN fusion restriction for " + ((object)u == null ? "?" : u.CharacterName) + " — blocked (" + ex.GetType().Name + ": " + ex.Message + ")");
                return false;
            }
        }
    }

    // ================= 시작기(§1-4, 원장 ⑪) — 토글의 컴포넌트, 인스턴스 필드 없음 =================
    public class GolemancerFusionStarter : UnitFactComponentDelegate, IActivatableAbilityStartHandler, IGlobalSubscriber
    {
        public void HandleActivatableAbilityStart(ActivatableAbility a)
        {
            if (a == null || !ReferenceEquals(a.Blueprint, Fusion.Toggle) || !ReferenceEquals(a.Owner.Unit, Owner)) return; // 정당한 무동작: 다른 토글·다른 유닛(전역 사건)
            var master = Owner;
            try { Start(master, a); }
            catch (Exception ex)
            {
                Fusion.Error(master, "start handler threw " + ex.GetType().Name + ": " + ex.Message);
                ModContext.LogError("Golemancer: fusion start handler on " + master.CharacterName + " threw: " + ex);
                TurnOff(master, a);
            }
        }

        static void TurnOff(UnitEntityData master, ActivatableAbility a)
        {
            var part = master.Get<UnitPartGolemFusion>();
            if (part != null && part.Runtime.Gate.State != FusionState.Idle) return; // 코어가 종료 중이거나 합체 중 — 그쪽 경로가 토글을 끈다
            if (a.AppliedBuff != null) a.AppliedBuff.Remove(); // 동기 → 엔진 HandleBuffDidRemoved 가 토글을 끈다
            if (a.IsOn) a.IsOn = false;
        }

        static void Start(UnitEntityData master, ActivatableAbility a)
        {
            if (Fusion.IsPreview(master)) { Fusion.Error(master, "start event on a preview unit (previews get no buffs)"); return; }
            FusionStart.Run(Fusion.Observe(master), new GameStartWorld(master, a, TurnOff));
        }
    }

    // 시작기 원시 동작(순서·판정은 순수 FusionStart).
    sealed class GameStartWorld : IFusionStartWorld
    {
        readonly UnitEntityData master; readonly ActivatableAbility a; readonly Action<UnitEntityData, ActivatableAbility> turnOff;
        UnitPartGolemFusion part;
        public GameStartWorld(UnitEntityData master, ActivatableAbility a, Action<UnitEntityData, ActivatableAbility> turnOff) { this.master = master; this.a = a; this.turnOff = turnOff; }
        public void EnsurePart() { part = master.Ensure<UnitPartGolemFusion>(); } // 파트 생성은 여기 한 곳뿐(R2-C01)
        public GolemFusionBlock CheckStarting() { return Fusion.Check(master, CheckPhase.Starting); }
        public void Begin()
        {
            if (part == null) throw new InvalidOperationException("Golemancer: start Begin before EnsurePart");
            part.BeginWith(GolemMaster.GolemOf(master, GolemMaster.PartOf(master).GolemId), a.AppliedBuff.Context);
        }
        public void SetIsOnFalse() { a.IsOn = false; }
        public void RemoveAppliedControl() { if (a.AppliedBuff == null) throw new InvalidOperationException("Golemancer: start rejected but the toggle has no applied control"); a.AppliedBuff.Remove(); }
        public void TurnOff() { turnOff(master, a); }
        public void Info(string m) { Fusion.Info(master, m); }
        public void Warn(string m) { Fusion.Warn(master, m); }
        public void Error(string m) { Fusion.Error(master, m); }
    }

    // Tick 원시 동작(순서·판정은 순수 FusionTick).
    sealed class GameTickWorld : IFusionTickWorld
    {
        readonly UnitEntityData u;
        public GameTickWorld(UnitEntityData u) { this.u = u; }
        UnitPartGolemFusion Part { get { var p = u.Get<UnitPartGolemFusion>(); if (p == null) throw new InvalidOperationException("Golemancer: tick action needs the fusion part on " + u.CharacterName); return p; } }
        public GolemFusionBlock CheckSteady() { return Fusion.Check(u, CheckPhase.Steady); }
        public void TurnToggleOff() { Fusion.TurnTogglesOff(u); }
        public void CleanupOrphan() { Fusion.CleanupOrphan(u); }
        public void ConsumeFormLost() { Part.ConsumeFormLost(); }
        public void DeliverEnd() { Part.DeliverEnd(FusionEventKind.DurationOver); }
        public void CountStopPending() { var c = Fusion.ControlsOf(u); Part.CountStopPending("StopPending (control EndTime " + (c.Count == 1 ? c[0].EndTime.ToString() : "?") + ", INFO only — not a verdict input)"); }
        public void Warn(string m) { Fusion.Warn(u, m); }
        public void Error(string m) { Fusion.Error(u, m); }
    }

    // ================= 흡수기(§1-8, 원장 ⑯) — 합체 특성의 컴포넌트: 관문만 =================
     
    // Fix round 2(NI1, 설계 이탈 — 보고서 기재): 상한도 여기서 걸지 않는다. 이 처리기는 대상 단계(@004D)라 개시자 처리기(@0073)의 남의 상한이 뒤에 와서
    //   우리 몫과 구별되지 않았다. 여기서는 관문을 통과한 규칙의 기록만 열고, 상한은 지속 전역 원장의 DidTrigger 훅(모든 구독자 뒤)이 건다.
    // 엔진은 규칙 처리기의 예외를 삼킨다(RulebookSubscribersList.OnEventDidTrigger @002B-0047) → 던지지 않고 ERROR(모드 로그 = S6 no-mod-errors).
    public class GolemancerFusionAbsorber : UnitFactComponentDelegate, ITargetRulebookHandler<RuleCalculateDamage>
    {
        static readonly UnknownOnce warnOnce = new UnknownOnce();

        void Err(string what) { Fusion.Error(Owner, "absorber: " + what); }

        public void OnEventAboutToTrigger(RuleCalculateDamage evt) { } // 정당한 무동작: 감산 전이라 읽을 것이 없다(StandAbsorb 와 같음)

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            try { Gate(evt); }
            catch (Exception ex) { Err("gate threw " + ex.GetType().Name + ": " + ex.Message + " (no record opened — the master takes the full damage)"); ModContext.LogError("Golemancer: absorber gate on " + Owner.CharacterName + " threw: " + ex); }
        }

        void Gate(RuleCalculateDamage evt)
        {
            if (evt.Target != Owner) throw new InvalidOperationException("Golemancer: absorber got RuleCalculateDamage for another target");
            var parent = evt.ParentRule;
            if (parent == null) throw new InvalidOperationException("Golemancer: absorber got RuleCalculateDamage without ParentRule");
            if (parent.IsFake) return; // 정당한 무동작: 미리보기(StandAbsorb H1)
            if (parent.IgnoreDamageReduction) return; // 정당한 무동작: 엔진이 상한을 버린다(@01AE-01BC, StandAbsorb 와 같음)

             
            var master = Owner;
            bool preview = Fusion.IsPreview(master);
            var facts = master.Facts.List.Where(f => ReferenceEquals(f.Blueprint, Fusion.Feature)).ToList();
            FusionObs o = null;
            if (!preview)
            {
                try { o = Fusion.Observe(master); }
                catch (Exception ex) { Fusion.LogUnknown(master, "fusion: absorber observation", ex); }
            }
            var d = AbsorberGateObs.Decide(preview, facts.Count, facts.Count > 0 && ReferenceEquals(Fact, facts[0]), o);
            if (d.DuplicateError) Err(facts.Count + " fusion feature facts — only the first absorbs");
            var part = master.Get<UnitPartGolemFusion>();
            switch (d.Verdict)
            {
                case AbsorberVerdict.Skip: return; // 정당한 무동작: 미리보기·합체 아님(정상 빈 상태)
                case AbsorberVerdict.ErrorObservationFailed: return; // LogUnknown 이 이미 남겼다 — 흡수 안 함
                case AbsorberVerdict.ErrorOrphan: Fusion.LogUnknown(master, "fusion: absorber sees orphan artefacts", new InvalidOperationException(o.ToString())); return; // Tick 이 정리
                case AbsorberVerdict.ErrorNotDesignated: Err("absorber fact is not the designated (first) fusion feature fact — not absorbing"); return;
                case AbsorberVerdict.SkipWarnFormLost:
                    if (warnOnce.First(UnknownOnce.KeyOf(master.UniqueId, "form lost absorb", new InvalidOperationException(part == null ? "-" : part.PetId ?? "-")))) Fusion.Warn(master, "absorb skipped: fusion form suppressed/lost — the fusion ends on the next tick");
                    return;
                case AbsorberVerdict.PassToGate: break;
                default: throw new InvalidOperationException("Golemancer: undefined absorber verdict " + d);
            }
            if (part == null) throw new InvalidOperationException("Golemancer: absorber passed the gate without a fusion part");

             
            if (!FusionDamageLedger.OpenPending(parent, master, part))
            {
                if (warnOnce.First(UnknownOnce.KeyOf(master.UniqueId, "repeat calculation", new InvalidOperationException(parent.GetHashCode().ToString())))) Fusion.Warn(master, "repeat damage calculation of the same rule — the cap stays on the damage source, the open record is kept");
            }
        }
    }

    // ================= 확정 원장(지속 전역 규칙 처리기 + DidTrigger 훅) =================
    // 판정·순서·대상 선택은 순수 FusionDamageCore(FusionRules.cs). 여기는 원시 동작과 배선만:
    //   RuleCalculateDamage 훅 → OnCalculateHook(상한) · RuleDealDamage 전역 DidTrigger(@001C) → OnDidTrigger(정산, 본체 피해는 이미 @025A) · RuleDealDamage 훅 → OnDealHook(정산한 상한 걷기).
    // 구독은 기록을 열 때와 Fusion.Tick 의 정해진 시점(모드 적재·불러오기·지역 이동 뒤 첫 틱 — Fusion.VerifyLedgerIfDue·LedgerVerifySchedule)에 확인한다(EnsureSubscribed — 전역 구독과 훅 등록 둘 다, 없으면 다시 걸고 ERROR). Tick 경계에 열린 기록 = 위반(Sweep).
    public static class FusionDamageLedger
    {
        static readonly FusionDamageCore<RuleDealDamage> core = new FusionDamageCore<RuleDealDamage>();
        static readonly Handler handler = new Handler();
        static bool everSubscribed;




        internal static bool IsOpen(RuleDealDamage r) { return core.IsOpen(r); }
        internal static ChunkMark MarkOf(BaseDamage source) { return core.MarkOf(source); }
        internal static bool OpenPending(RuleDealDamage r, UnitEntityData master, UnitPartGolemFusion part) { return core.OpenPending(r, master, part, part.PetId, GameCoreWorld.Instance); }

        internal static void EnsureSubscribed()
        {
            bool g = RulebookRegistry.IsGloballySubscribed(handler), h = HookRegistered(); // 규칙 등록부 직독(EventBus.IsGloballySubscribed 는 규칙 처리기에 항상 거짓 — RulebookRegistry.cs)
            if (g && h) return;
            if (everSubscribed) ModContext.LogError("Golemancer: fusion damage ledger lost its subscription (global " + g + ", hook " + h + ") — subscribing again");
            if (g || h) EventBus.Unsubscribe(handler); // 반쪽 등록을 지우고 둘 다 다시(Register 는 중복 훅을 PFLog 오류로 거부한다 @0010-0032)
            EventBus.Subscribe(handler); // EventBus.Subscribe @0044 → RulebookEventBus.Subscribe @0061-0067 → Hooks.Register(IRulebookEventDidTriggerHook)
            everSubscribed = true;
            if (!RulebookRegistry.IsGloballySubscribed(handler) || !HookRegistered()) throw new InvalidOperationException("Golemancer: fusion damage ledger subscription did not register (global " + RulebookRegistry.IsGloballySubscribed(handler) + ", hook " + HookRegistered() + ")");
        }

        // 빌드 때 1회: 리플렉션 대상이 이 게임 판에 있는가(게임 업데이트로 사라지면 throw → BuildAll 실패 → FAIL build/blueprints, 합체 없음).
        internal static void RequireEngineMembers()
        {
            HookRegistered();          // RulebookEventBus.Hooks · RulebookEventHooksManager.m_DidTriggerHooks
            GameCoreWorld.MaxSetter(); // BaseDamage.MaximumValue 비공개 설정자
            Fusion.PartsListField();   // EntityPartsManager.m_Parts(OnPreSave 파트 집합 단언)
        }

        // 훅 등록 관측: RulebookEventBus.Hooks(비공개 정적) → RulebookEventHooksManager.m_DidTriggerHooks(비공개). 못 찾으면 throw(조용히 「등록됨」으로 보지 않는다).
        static bool HookRegistered()
        {
            var hf = typeof(RulebookEventBus).GetField("Hooks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (hf == null) throw new MissingFieldException("Golemancer: RulebookEventBus.Hooks not found");
            var mgr = hf.GetValue(null);
            if (mgr == null) throw new InvalidOperationException("Golemancer: RulebookEventBus.Hooks is null");
            var lf = mgr.GetType().GetField("m_DidTriggerHooks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lf == null) throw new MissingFieldException("Golemancer: RulebookEventHooksManager.m_DidTriggerHooks not found");
            var list = lf.GetValue(mgr) as System.Collections.IList;
            if (list == null) throw new InvalidOperationException("Golemancer: m_DidTriggerHooks is not a list");
            return list.Contains(handler);
        }

        // Tick 경계: 열린 기록 = 수명 위반(그 규칙의 DidTrigger 가 오지 않았다). 지금 관측으로 정산하고 ERROR, 상한을 걷는다.
        internal static void Sweep()
        {
            if (core.OpenCount == 0) return; // 정당한 무동작: 열린 기록 없음(빈 정산은 아무것도 하지 않는다 — 매 틱 목록 2개 할당을 피한다)
            foreach (var res in core.Sweep(r => r.Target, r => () => ReadResult(r), r => r.DifficultyModifier, r => r.MinHPAfterDamage, GameCoreWorld.Instance))
            {
                // 배포 구성에서도 반복은 남긴다(정산은 core.Sweep 을 끝까지 도는 것이다) — 빠지는 것은 점검 전용 로그 줄뿐.



            }
        }

        // 덩어리 관측: 원천·v = max(0, 감산 전 − 감소)·FinalValue·원천 상한(DamageValue.get_FinalValue @0002-0036 의 입력 그대로).
        static IList<ChunkNow> Chunks(List<DamageValue> list)
        {
            if (list == null) return null; // 순수 쪽이 「목록 없음」을 관련 규칙이면 관측 불가로, 무관하면 무동작으로 판정한다
            return list.Select(v => new ChunkNow { Source = v.Source, Value = Math.Max(0, v.ValueWithoutReduction - v.Reduction), Final = v.FinalValue, Max = v.Source == null ? null : v.Source.MaximumValue }).ToList();
        }
        static IList<ChunkNow> ReadResult(RuleDealDamage r) { return Chunks(r.ResultList); }
        static IList<ChunkNow> ReadCalc(RuleCalculateDamage c) { return Chunks(c.CalculatedDamage); }

        sealed class Handler : IGlobalRulebookHandler<RuleDealDamage>, IRulebookEventDidTriggerHook // IGlobalSubscriber 표식은 칸을 만들지 않는다(InterfaceFinder.GetSubscriberInterfaces) — 2026-11-15 제거
        {
            public void OnEventAboutToTrigger(RuleDealDamage evt) { } // 정당한 무동작: 규칙 시작 전

            public void OnEventDidTrigger(RuleDealDamage evt)
            {
                try
                {
                    var res = core.OnDidTrigger(evt, evt.IsFake, evt.Target, () => ReadResult(evt), evt.DifficultyModifier, evt.MinHPAfterDamage, GameCoreWorld.Instance);



                }
                catch (Exception ex) { Fusion.Error(evt.Target, "absorb settle threw " + ex.GetType().Name + ": " + ex.Message); ModContext.LogError("Golemancer: absorb settle threw: " + ex); }
            }

            // 모든 규칙의 DidTrigger 뒤(RulebookEventHooksManager.OnAfterEventDidTrigger, 엔진이 예외를 PFLog 로 삼킨다 @0024-0036 → 여기서 잡아 모드 ERROR).
            public void OnAfterEventDidTrigger(Kingmaker.RuleSystem.RulebookEvent e)
            {
                var calc = e as RuleCalculateDamage;
                if (calc != null)
                {
                    try { if (calc.ParentRule != null) core.OnCalculateHook(calc.ParentRule, calc.Target, () => ReadCalc(calc), GameCoreWorld.Instance); }
                    catch (Exception ex) { Fusion.Error(calc.Target, "cap hook threw " + ex.GetType().Name + ": " + ex.Message + " (an open record is settled from the observed damage)"); ModContext.LogError("Golemancer: cap hook threw: " + ex); }
                    return;
                }
                var deal = e as RuleDealDamage;
                if (deal == null) return; // 정당한 무동작: 피해 규칙이 아니다
                try { core.OnDealHook(deal.Target, () => ReadResult(deal), GameCoreWorld.Instance); }
                catch (Exception ex) { Fusion.Error(deal.Target, "cap lift hook threw " + ex.GetType().Name + ": " + ex.Message); ModContext.LogError("Golemancer: cap lift hook threw: " + ex); }
            }
        }
    }

    // 확정 원시 동작(게임). 규칙 객체를 들지 않는다 — 보상 대상은 순수 코어가 넘긴 유닛(기록의 주인)뿐이다(opus N3g).
    sealed class GameCoreWorld : IDamageCoreWorld
    {
        internal static readonly GameCoreWorld Instance = new GameCoreWorld();
        GameCoreWorld() { }

        static UnitEntityData U(object u) { var x = u as UnitEntityData; if ((object)x == null) throw new InvalidCastException("Golemancer: damage core expected a unit, got " + (u == null ? "null" : u.GetType().Name)); return x; }
        static UnitPartGolemFusion P(object p) { var x = p as UnitPartGolemFusion; if (x == null) throw new InvalidCastException("Golemancer: damage core expected a fusion part"); return x; }
        static BaseDamage S(object s) { var x = s as BaseDamage; if (x == null) throw new InvalidCastException("Golemancer: damage core expected a damage source"); return x; }

        public void EnsureSubscribed() { FusionDamageLedger.EnsureSubscribed(); }
        public AbsorbCalc AbsorbCalc(object part, int incoming) { return P(part).AbsorbCalc(incoming); } // 골렘 HP 를 읽는다(신원 불일치 → throw)
        public void SetMaximum(object source, int cap) { S(source).SetMaximum(cap); }
        public int? ReadMaximum(object source) { return S(source).MaximumValue; }

        // 상한 걷기: BaseDamage.MaximumValue 의 비공개 설정자(@0000-0007, 자동 속성). 못 찾으면 throw.
        public void RestoreMaximum(object source, int? value) { MaxSetter().Invoke(S(source), new object[] { value }); }

        internal static System.Reflection.MethodInfo MaxSetter()
        {
            var pi = typeof(BaseDamage).GetProperty("MaximumValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var set = pi == null ? null : pi.GetSetMethod(true);
            if (set == null) throw new MissingMethodException("Golemancer: BaseDamage.MaximumValue setter not found");
            return set;
        }

        public object CurrentPartOf(object master) { return U(master).Get<UnitPartGolemFusion>(); }
        public string ResolvePetId(object part) { return P(part).Pet().UniqueId; }
        public int PetHp(object part) { return P(part).Pet().Descriptor.HPLeft; }
        public int PetDamage(object part) { return P(part).Pet().Descriptor.Damage; }
        public void CommitCore(DamageRecord r) { P(r.Part).AbsorbDeal(r.Calc); }
        public void CommitRebased(DamageRecord r, int petHpNow, int share) { P(r.Part).AbsorbDealRebased(petHpNow, share); }
        public ICompensationPrims PrimsOf(object unit) { return new UnitCompensationPrims(U(unit)); }
        public void Info(object unit, string m) { Fusion.Info(U(unit), m); }
        public void Warn(object unit, string m) { Fusion.Warn(U(unit), m); }
        public void Error(object unit, string m) { Fusion.Error(U(unit), m); }
    }

    // 보상 쓰기 원시 동작(한 줄씩). 순서·양·관측은 순수 CompensationRun.
    sealed class UnitCompensationPrims : ICompensationPrims
    {
        readonly UnitEntityData m;
        public UnitCompensationPrims(UnitEntityData m) { if ((object)m == null) throw new ArgumentNullException("m"); this.m = m; }
        public int TempHp() { return m.Stats.TemporaryHitPoints.ModifiedValue; }
        public int ConsumeTemp(int amount) { return m.Stats.TemporaryHitPoints.HandleDamage(amount); } // 엔진 @0254 와 같은 호출, 끝에 UpdateValue(@0093)
        public int HpLeft() { return m.HPLeft; }
        public int Damage() { return m.Descriptor.Damage; }
        public void SetDamage(int value) { m.Descriptor.Damage = value; }
        public void LifeTick() { UnitLifeController.ForceTickOnUnit(m); }
    }

    // ================= 저장 전 분리 패치(§5, 원장 ⑩) — 정확한 서명 SaveManager.SaveRoutine(SaveInfo, bool) =================
    [HarmonyPatch(typeof(SaveManager), "SaveRoutine", new[] { typeof(SaveInfo), typeof(bool) })]
    static class FusionPreSavePatch
    {
        static void Prefix()
        {
            try { Fusion.EndAllBeforeSave(); }
            catch (Exception ex) { ModContext.LogError("Golemancer: FusionPreSavePatch threw " + ex); } // 저장 경로로 예외를 내지 않는다
        }
    }
}
