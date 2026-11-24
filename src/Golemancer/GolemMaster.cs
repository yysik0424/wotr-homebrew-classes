using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Newtonsoft.Json;
using WotrClassMod.Common;



using static WotrClassMod.Common.BlueprintUtil;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace Golemancer
{
    // 골렘 펫 어댑터(계획 T3 Interfaces — GolemMaster·UnitPartGolemMaster·GolemMasterComponent). 판정은 전부 순수 코어(T2):
    //   GolemLifecycle(Handle·Execute·DecideRebind) · BreakRules · TauntPlan · CompanionConflict. 게임 쪽은 그 순간을 관측해 코어에 넣고 돌아온 Cmd 를 실행한다.
    // 원시 사건 → 생명주기: OnActivate→Activated · HandleAddPet→AddPet · HandleUnitAfterLevelUp→PetLevelUp/MasterLevelUp · OnAreaDidLoad→AreaDidLoad ·
    //   HandleUnitDestroyed→GolemDestroyed(+WARN) · HandleRestFinished(성공)→RestSucceeded · Tick→Poll. 사망 = BreakState 만(설계 §4.7).
    // 모든 관측 Handle 뒤 DecideRebind(원장 판정 T2 fix 1 — Detached 막다른 길 방지), ApplyOk 직후 한 번 더 관측(진행 중 Apply 동안 Wait 로 답한 AddPet 회수).
    public static class GolemMaster
    {
        public static BlueprintFeature Feature;             // GolemancerGolemFeature(AddPet + 「동물 동료 없음」 전제 + GolemMasterComponent)
        public static BlueprintFeature WoodMaterialFeature; // 나무 재질 특성(GUID 는 T3 발급) — 컴포넌트는 Materials.Build 가 단다(GolemMaterialComponent)
        public static BlueprintUnitProperty LevelProperty;  // GolemancerLevelProperty = 연금술사 레벨(HD = 연금술사 레벨, 설계 §4.4 [컨트롤러])

        public static void Build()
        {
            if (Feature != null) throw new InvalidOperationException("Golemancer: GolemMaster.Build() called twice");
            if (GolemUnit.Unit == null || PetClass.Class == null || Taunt.Area == null) throw new InvalidOperationException("Golemancer: GolemMaster.Build() before PetClass/Taunt/GolemUnit");
            var alchemist = Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass);

            // 레벨 속성(선례 src\PhantomBlade\StandBlueprints.cs:349-351): BaseValue 0 + Σ게터(ClassLevelGetter{연금술사}).
            LevelProperty = Create<BlueprintUnitProperty>("GolemancerLevelProperty", Guids.GolemLevelProperty, p =>
            {
                p.BaseValue = 0;
                p.OperationOnComponents = BlueprintUnitProperty.MathOperation.Sum;
            });
            var level = new ClassLevelGetter();
            SetField(level, "m_Class", Ref<BlueprintCharacterClassReference>(Guids.Game.AlchemistClass));
            AddComponents(LevelProperty, level);
            if (!ReferenceEquals(level.Class, alchemist)) throw new InvalidOperationException("Golemancer: GolemancerLevelProperty ClassLevelGetter readback failed");

            Feature = Create<BlueprintFeature>("GolemancerGolemFeature", Guids.GolemFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("GolemFeature.Name"));
                SetField(f, "m_Description", Strings.L("GolemFeature.Description"));
                SetField(f, "m_DescriptionShort", Strings.L("GolemFeature.Description"));
                f.IsClassFeature = true;
                f.Ranks = 1;
            });
            // AddPet(정찰 §4.1 Triceratops 선례, 설정 방식 WSS SpiritBlueprints.cs:287-290): 레벨 = ContextValue(연금술사 레벨) → TTT·ExpandedContent 패치 영향 밖.
            var addPet = new AddPet { Type = PetType.AnimalCompanion, ProgressionType = PetProgressionType.AnimalCompanion };
            SetField(addPet, "m_Pet", Ref<BlueprintUnitReference>(Guids.GolemUnit));
            SetField(addPet, "m_UseContextValueLevel", true);
            var levelValue = new ContextValue { ValueType = ContextValueType.CasterCustomProperty };
            SetField(levelValue, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(Guids.GolemLevelProperty));
            SetField(addPet, "m_LevelContextValue", levelValue);
            SetField(addPet, "m_ForceAutoLevelup", true);
            // m_UpgradeFeature 는 두지 않는다(생성자 기본 = 없음) — TryLevelUpPet 은 UpgradeFeature == null 이면 건너뛴다(@0174~0179, WSS SpiritBlueprints.cs 와 같음). 되읽기가 null 을 단언한다.
            SetField(addPet, "m_DestroyPetOnDeactivate", true);
            var prereq = new PrerequisitePet { Type = PetType.AnimalCompanion, NoCompanion = true, Group = Prerequisite.GroupType.All };
            AddComponents(Feature, addPet, prereq, new GolemMasterComponent());
            GolemPresence.Grant(Feature);
            var ap = Feature.GetComponent<AddPet>();
            if (ap == null || !ReferenceEquals(ap.Pet, GolemUnit.Unit) || ap.UpgradeFeature != null || ap.Type != PetType.AnimalCompanion || ap.ProgressionType != PetProgressionType.AnimalCompanion
                || !(bool)FindField(typeof(AddPet), "m_UseContextValueLevel").GetValue(ap) || !(bool)FindField(typeof(AddPet), "m_ForceAutoLevelup").GetValue(ap) || !(bool)FindField(typeof(AddPet), "m_DestroyPetOnDeactivate").GetValue(ap))
                throw new InvalidOperationException("Golemancer: GolemancerGolemFeature AddPet readback failed");
            var lv = (ContextValue)FindField(typeof(AddPet), "m_LevelContextValue").GetValue(ap);
            if (lv == null || lv.ValueType != ContextValueType.CasterCustomProperty || !ReferenceEquals(lv.CustomProperty, LevelProperty)) throw new InvalidOperationException("Golemancer: AddPet level ContextValue readback failed");
            if (Feature.ComponentsArray.OfType<GolemMasterComponent>().Count() != 1 || Feature.ComponentsArray.OfType<PrerequisitePet>().Count() != 1) throw new InvalidOperationException("Golemancer: GolemancerGolemFeature components readback failed");

            WoodMaterialFeature = Create<BlueprintFeature>("GolemancerMaterialWood", Guids.MaterialWoodFeature, f =>
            {
                SetField(f, "m_DisplayName", Strings.L("MaterialWood.Name"));
                SetField(f, "m_Description", Strings.L("MaterialWood.Description"));
                SetField(f, "m_DescriptionShort", Strings.L("MaterialWood.Summary")); // 2026-11-17: 짧은 설명 = 한 줄 요약(재질 버프 툴팁과 같은 키)
                f.IsClassFeature = true;
                f.Ranks = 1;
            });

            Archetype.AddAt(1, Feature);
            Archetype.AddAt(1, WoodMaterialFeature);
        }

        // ---------------- 관측 ----------------

        public static bool IsOurGolem(UnitEntityData pet)
        {
            if (pet == null) throw new ArgumentNullException("pet");
            if (GolemUnit.Unit == null) throw new InvalidOperationException("Golemancer: GolemUnit.Build() has not run");
            return ReferenceEquals(pet.Blueprint, GolemUnit.Unit);
        }

        // 주인의 결속 골렘. 펫 칸 비었음·우리 블루프린트 아님·UniqueId 불일치 = throw(설계 F08 신원 검사 — 같은 블루프린트의 다른 펫을 쓰지 않는다).
        public static UnitEntityData GolemOf(UnitEntityData master, string expectedId)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (string.IsNullOrEmpty(expectedId)) throw new ArgumentException("Golemancer: GolemOf needs the bound golem id");
            var pet = master.GetPet(PetType.AnimalCompanion);
            if (pet == null) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has no animal companion (golem " + expectedId + " expected)");
            if (!IsOurGolem(pet)) throw new InvalidOperationException("Golemancer: " + master.CharacterName + "'s companion " + pet.CharacterName + " is not our golem");
            if (pet.UniqueId != expectedId) throw new InvalidOperationException("Golemancer: " + master.CharacterName + "'s golem is " + pet.UniqueId + ", bound id is " + expectedId);
            return pet;
        }

        // 관측 재질 키 = 주인의 현재 재질 id(Materials.CurrentOfOrNull — 재질 특성 0·2개면 null = 코어 「material key unknown」 Wait, 계획 T2 Obs.MaterialKey).
        public static string ObservedMaterialKey(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            var m = Materials.CurrentOfOrNull(master);
            return m == null ? null : m.Id;
        }

        // Apply ② 재질 단계(원장 판정 T3→T4 이음매): Materials.Reconcile(LegalLevels 검사 → 버프 교정 → 수치 사실 전부 회수 뒤 1개). 반환 = 적용한 재질 키.
        public static string MaterialsStep(UnitEntityData master, UnitEntityData golem, int level, string key)
        {
            if (master == null || golem == null) throw new ArgumentNullException(master == null ? "master" : "golem");
            return Materials.Reconcile(master, golem, level, key);
        }

         
        public static bool ObserveConflict(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            var pets = new List<string>();
            var selections = new List<string>();
            foreach (var f in master.Facts.List)
            {
                f.CallComponents<AddPet>(a => { if (a.Type == PetType.AnimalCompanion) pets.Add(a.Pet == null ? "" : a.Pet.AssetGuid.ToString()); });
                var s = f.Blueprint as BlueprintFeatureSelection;
                if (s != null && (s.Group == FeatureGroup.AnimalCompanion || s.Group2 == FeatureGroup.AnimalCompanion)) selections.Add(s.AssetGuid.ToString());
            }
            return CompanionConflict.Decide(pets.ToArray(), Guids.GolemUnit, selections.ToArray());
        }

        // 그 순간의 관측. 펫 레벨 = 우리 펫 직업 레벨, 주인 레벨 = 연금술사 레벨. 펫이 없으면 GolemId = null(코어가 레벨보다 먼저 본다 — 그때의 레벨 0 은 판정에 쓰이지 않는다).
        // 합체 상태 = T3 은 Idle 고정(원장 판정 T2→T3 「T3 feeds fusion = Idle constant」; T5 가 실제 값).
        public static Obs Observe(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (PetClass.Class == null) throw new InvalidOperationException("Golemancer: PetClass.Build() has not run");
            var pet = master.GetPet(PetType.AnimalCompanion);
            int masterLevel = master.Progression.GetClassLevel(Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass));
            if (pet == null) return new Obs(null, false, 0, masterLevel, ObservedMaterialKey(master), ObserveConflict(master), CurrentFusion(master), PetLife.None);
            var part = master.Get<UnitPartGolemMaster>();
            bool stowed = part != null && part.Called == false && part.GolemId == pet.UniqueId;
            return new Obs(pet.UniqueId, IsOurGolem(pet), pet.Progression.GetClassLevel(PetClass.Class), masterLevel, ObservedMaterialKey(master), ObserveConflict(master), CurrentFusion(master),
                pet.State.IsDead ? PetLife.Dead : stowed ? PetLife.Stowed : PetLife.Alive);
        }

         
        // 파트 없음 → 합체 특성이 없으면 Idle(10레벨 미만), 있으면 StartPhase.Classify(사건 밖) 가 NoFusion/PendingStart/RejectedStart 면 Idle, 고아면 throw.
        public static FusionState CurrentFusion(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (Fusion.Feature == null) throw new InvalidOperationException("Golemancer: CurrentFusion before Fusion.Build()");
            if (Fusion.IsPreview(master)) return FusionState.Idle;
            var part = master.Get<UnitPartGolemFusion>();
            if (part != null) return part.Runtime.Gate.State;
            if (!master.Descriptor.HasFact(Fusion.Feature)) return FusionState.Idle;
            var ph = StartPhase.Classify(Fusion.Observe(master), false);
            if (ph.Phase == FusionPhase.NoFusion || ph.Phase == FusionPhase.PendingStart || ph.Phase == FusionPhase.RejectedStart) return FusionState.Idle;
            throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has fusion artefacts without a fusion part (" + ph + ")");
        }

        // 옛 골렘 파괴 판정(원장 판정 Step 0 ④·T3 fix I3): 두 풀의 (UniqueId, Destroyed) 를 순수 판정 GolemLifecycle.OldGolemDestroyedIn 에 넘기기만 한다.
        public static bool OldGolemDestroyed(string oldId)
        {
            if (string.IsNullOrEmpty(oldId)) throw new ArgumentException("Golemancer: OldGolemDestroyed needs the old golem id");
            var state = Game.Instance.State;
            return GolemLifecycle.OldGolemDestroyedIn(Pool(state.Units), Pool(state.ExtraUnits), oldId);
        }

        static IEnumerable<KeyValuePair<string, bool>> Pool(IEnumerable<UnitEntityData> units)
        {
            if (units == null) throw new ArgumentNullException("units");
            foreach (var u in units) yield return new KeyValuePair<string, bool>(u.UniqueId, u.Destroyed);
        }

        public static UnitPartGolemMaster PartOf(UnitEntityData master)
        {
            if (master == null) throw new ArgumentNullException("master");
            var part = master.Get<UnitPartGolemMaster>();
            if (part == null) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has no UnitPartGolemMaster (golem feature never activated)");
            return part;
        }

        // ---------------- 생명주기 공급 ----------------

        // 관측 사건 1개: 관측 → Life.Handle → Cmd 실행 → DecideRebind.
        public static void Feed(UnitEntityData master, LifeEventKind kind)
        {
            var part = PartOf(master);
            var obs = Observe(master);
            var from = part.Life.State;
            var cmds = part.Life.Handle(LifeEvent.Of(kind), obs);
            part.Transition(from, kind.ToString());
            Run(master, part, cmds);
            AfterObservation(master, part, obs);
            if (part.Called != true && obs.GolemIsOurs && obs.GolemId == part.GolemId)
                GolemPresence.Reconcile(master, part);
        }

        // 원장 판정(R3-01 + T2 검토): 관측 Handle 뒤마다. 펫이 관측될 때만(코어 계약 — newId null 이면 throw).
        static void AfterObservation(UnitEntityData master, UnitPartGolemMaster part, Obs obs)
        {
            if (obs.GolemId == null) return; // 정당한 무동작: 펫 미관측 = 재결속 대상 없음
            // 옛 골렘 생사는 Detached 에서만 판정에 들어간다(그 밖은 DecideRebind 가 NotApplicable) — 매 틱 유닛 목록을 훑지 않으려 Detached 에서만 본다.
            // 같은 ID·우리 것 아님은 DecideRebind 가 생사와 무관하게 Ignore — 그 경우 유닛 목록을 훑지 않는다(Detached 에 같은 펫이 남아 매 틱 Poll 되는 비용).
            bool oldDestroyed = part.Life.State == LifecycleState.Detached && obs.GolemIsOurs && obs.GolemId != part.Life.BoundId && OldGolemDestroyed(part.Life.BoundId);
            var d = GolemLifecycle.DecideRebind(part.Life.State, part.Life.BoundId, obs.GolemId, obs.GolemIsOurs, oldDestroyed);
            if (d == RebindDecision.Rebind)
            {
                string old = part.Life.BoundId;
                var cmds = part.Life.Handle(LifeEvent.Rebind(obs.GolemId, true), null);
                if (part.Life.State != LifecycleState.BoundPending || part.Life.BoundId != obs.GolemId) throw new InvalidOperationException("Golemancer: rebind to " + obs.GolemId + " left the lifecycle in " + part.Life.State + " bound " + part.Life.BoundId);
                part.GolemId = obs.GolemId;
                part.AppliedLevel = 0;
                part.AppliedKey = null;
                part.Transition(LifecycleState.Detached, "Rebind");
                part.Info("골렘 교체 rebind " + old + " -> " + obs.GolemId);
                Run(master, part, cmds);
            }
            else if (d == RebindDecision.ErrorOldAlive)
            {
                Run(master, part, part.Life.Handle(LifeEvent.Rebind(obs.GolemId, false), null)); // 코어 → Ignore + ERROR(신원 불일치, R3-04)
            }
            // 정당한 무동작: Ignore / NotApplicable
        }

        // 명령 실행(배열 순서 = 코어가 정한 순서: Bind 가 Apply 앞).
        static void Run(UnitEntityData master, UnitPartGolemMaster part, Cmd[] cmds)
        {
            if (cmds == null) throw new ArgumentNullException("cmds");
            foreach (var c in cmds)
            {
                if (c.Kind == CmdKind.Bind)
                {
                    // Bind 는 Unbound 에서만 나온다 → 저장 ID 가 비어 있어야 한다. ID 를 먼저 쓴다(선례 WSS Fusion.cs:218, R2-01).
                    if (part.GolemId != null) throw new InvalidOperationException("Golemancer: Bind(" + c.Id + ") while GolemId is already " + part.GolemId);
                    part.GolemId = c.Id;
                    part.Called = false;
                    part.AppliedLevel = 0;
                    part.AppliedKey = null;
                    part.Info("bind GolemId=" + c.Id);
                }
                else if (c.Kind == CmdKind.Apply)
                {
                    if (part.GolemId != c.Id) throw new InvalidOperationException("Golemancer: " + c + " for a golem other than the saved GolemId " + (part.GolemId ?? "null"));
                    part.Info("apply " + c);
                    var from = part.Life.State;
                    var result = Apply(master, part, c);
                    part.Transition(from, "Apply");
                    bool ok = part.Life.State == LifecycleState.Ready && part.Life.Applied != null && part.Life.Applied.Id == c.Id && part.Life.Applied.Level == c.Level && part.Life.Applied.Key == c.Key;
                    if (ok)
                    {
                        part.AppliedLevel = c.Level;
                        part.AppliedKey = c.Key;
                        part.Info("apply-ok " + c.Id + " L" + c.Level + " " + c.Key);
                    }
                    Run(master, part, result);
                    if (ok) Feed(master, LifeEventKind.Poll); // ApplyOk 직후 재평가(진행 중 Apply 동안 Wait 로 답한 관측 회수). Ready 의 Poll 은 Apply 를 내지 않는다 — 재귀는 한 번으로 끝난다.
                }
                else if (c.Kind == CmdKind.Report)
                {
                    if (c.Severity == ReportLevel.Warn) part.Warn(c.Text);
                    else part.Error(master, c.Text);
                }
                else if (c.Kind == CmdKind.Wait) part.CountWait(c.Reason);
                else if (c.Kind != CmdKind.Ignore) throw new InvalidOperationException("Golemancer: undefined command " + c);
                // 정당한 무동작: Ignore
            }
        }

        // Apply 트랜잭션(R3-02): 코어 Execute 가 GolemOf → Materials → Taunt(기대 = 명령 인자) → Readback → ApplyOk/ApplyFailed 순서를 보장한다.
        // 최초 생성·같은 ID 로드·Rebind 세 진입 모두 이 한 경로.
        public static Cmd[] Apply(UnitEntityData master, UnitPartGolemMaster part, Cmd apply)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (part == null) throw new ArgumentNullException("part");
            return part.Life.Execute(apply, new Effector(master));
        }

        sealed class Effector : IApplyEffector<UnitEntityData>
        {
            readonly UnitEntityData master;
            public Effector(UnitEntityData master) { this.master = master; }
            public UnitEntityData GolemOf(string id) { return GolemMaster.GolemOf(master, id); }
            public void Materials(UnitEntityData golem, int level, string key)
            {
                var applied = MaterialsStep(master, golem, level, key);
                if (applied != key) throw new InvalidOperationException("Golemancer: material step applied " + applied + ", expected " + key);
                 
                if (CurrentFusion(master) == FusionState.Active) Fusion.ReconcileMaster(master, StatRecipe.BuffKey(MaterialCatalog.Get(key), level));
            }
            public void Taunt(UnitEntityData golem, TauntSource expected) { Golemancer.Taunt.Reconcile(master, golem, expected); }
            public void Readback(UnitEntityData golem, int level, string key, TauntSource expected)
            {
                int petLevel = golem.Progression.GetClassLevel(PetClass.Class);
                if (petLevel != level) throw new InvalidOperationException("Golemancer: readback golem level " + petLevel + ", applied " + level);
                var observed = ObservedMaterialKey(master);
                if (observed != key) throw new InvalidOperationException("Golemancer: readback material key " + observed + ", applied " + key);
                var src = Golemancer.Taunt.SourceOf(master);
                if (src != expected) throw new InvalidOperationException("Golemancer: readback taunt source " + src + ", expected " + expected);
                // T5: 합체 중이면 본체 형태 키·수치 1·formOk·크기·슬램·도발 원천을 되읽는다(실패 = ApplyFailed).
                if (CurrentFusion(master) == FusionState.Active) Fusion.ReadbackMaster(master, StatRecipe.BuffKey(MaterialCatalog.Get(key), level));
            }
        }

        // ---------------- 부서짐·파괴·휴식 ----------------

        internal static void OnGolemDied(UnitEntityData master, UnitEntityData unit)
        {
            var part = PartOf(master);
            if (part.GolemId == null || unit.UniqueId != part.GolemId) return; // 정당한 무동작: 이 주인의 골렘이 아니다
            part.Broken = BreakRules.Next(part.Break, BreakEvent.GolemDied) == BreakState.Broken;
            part.Info("golem " + unit.UniqueId + " died -> Broken=" + part.Broken);
            Overdrive.CheckEnd(master, "golem died"); // 소유자 결정 2026-11-16: 골렘이 부서지면 폭주가 즉시 끝난다
        }

        // 외부 파괴(R3-04): WARN 만 — 신원 변화는 다음 관측(규칙 ①)과 DecideRebind 가 다룬다.
        internal static void OnGolemDestroyed(UnitEntityData master, UnitEntityData unit)
        {
            var part = PartOf(master);
            if (part.GolemId == null || unit.UniqueId != part.GolemId) return; // 정당한 무동작: 이 주인의 골렘이 아니다
            part.Broken = BreakRules.Next(part.Break, BreakEvent.GolemDestroyed) == BreakState.Broken;
            var cmds = part.Life.Handle(LifeEvent.Destroyed(unit.UniqueId), null);
            if (cmds.Length != 1 || cmds[0].Kind != CmdKind.Ignore) throw new InvalidOperationException("Golemancer: GolemDestroyed returned " + string.Join(",", cmds.Select(c => c.ToString()).ToArray()) + " (core contract: Ignore)");
            part.Warn("골렘 외부 파괴 golem " + unit.UniqueId + " destroyed outside the mod (lifecycle " + part.Life.State + ")");
            Overdrive.CheckEnd(master, "golem destroyed");
        }

        internal static void OnRestInterrupted(UnitEntityData master)
        {
            var part = PartOf(master);
            part.Broken = BreakRules.Next(part.Break, BreakEvent.RestInterrupted) == BreakState.Broken;
            part.Info("rest interrupted -> Broken=" + part.Broken);
        }

        // 성공한 휴식(F07, 설계 §4.7 「휴식 시 전부 수리」, 결정 ③ 처리기 단독): (죽었으면 사망 관측 → 되살림) → 피해 0 → Broken 해제 → 생명주기 RestSucceeded → 되읽기.
        // 도발 복구 = 생명주기 Apply(T3 fix I1: 사망 관측으로 Stale → 되살아난 뒤 RestSucceeded 경계에서 Apply 가 도발을 다시 붙인다 — 비적용 Reconcile 없음).
        public static void OnRestSucceeded(UnitEntityData master)
        {
            var part = PartOf(master);
            var state = part.Life.State;
            var pet = master.GetPet(PetType.AnimalCompanion);
            // T3 fix I2: 결속 골렘이 펫 칸에 없으면(미결속·외부 파괴 뒤·다른 펫·충돌) 수리할 대상이 없다 — GolemOf 로 던지지 않고 그 관측을 생명주기에 그대로 넣는다
            // (외부 파괴 뒤 Ready 면 코어 규칙 ④ Ready → Detached WARN, R3-04 「외부 파괴 경로 ERROR 0」).
            if (part.GolemId == null || state == LifecycleState.Detached || state == LifecycleState.Conflict || pet == null || pet.UniqueId != part.GolemId)
            {
                if (state == LifecycleState.Detached) part.Warn("rest: bound golem " + part.Life.BoundId + " is detached — nothing to repair");
                part.Broken = BreakRules.Next(part.Break, BreakEvent.RestSucceeded) == BreakState.Broken;
                Feed(master, LifeEventKind.RestSucceeded);
                return;
            }
            var golem = GolemOf(master, part.GolemId);
            string id = golem.UniqueId;
            if (golem.State.IsDead)
            {
                // 되살리기 전에 사망을 관측시킨다(Ready → Stale, ⑦) — 사망 사건은 생명주기에 들어가지 않으므로(설계 §4.7) 여기서 한 번 본다.
                Feed(master, LifeEventKind.Poll);
                golem.Descriptor.Resurrect(null, 1f, true);  
                if (golem.State.IsDead || golem.UniqueId != id) throw new InvalidOperationException("Golemancer: golem " + id + " still dead (or replaced) after Resurrect");
            }
            golem.Descriptor.Damage = 0;
            part.Broken = BreakRules.Next(part.Break, BreakEvent.RestSucceeded) == BreakState.Broken;
            Feed(master, LifeEventKind.RestSucceeded);
            if (golem.HPLeft != golem.MaxHP) throw new InvalidOperationException("Golemancer: golem HP " + golem.HPLeft + "/" + golem.MaxHP + " after rest repair");
            part.Info("rest repaired golem " + id + " HP " + golem.HPLeft + "/" + golem.MaxHP + " Broken=" + part.Broken + " lifecycle " + part.Life.State);
        }

        // ---------------- 틱 ----------------

        // OnUpdate(Main): 파티 유닛 중 Life ∈ {Unbound, BoundPending, Stale} 에 Poll(다음 프레임 = 안정 경계). Detached 는 펫이 관측될 때만 Poll(DecideRebind 기회).
        // Ready·Conflict 는 비용 0. 게임 상태가 없거나 로딩 중이면 정당한 무동작.
        // 틱 유닛 목록 버퍼(2026-11-15 성능 수리 — 매 프레임 ToList 대신 재사용). Player.Party 는 내부 목록이고 Feed(Apply → 펫 생성 등)가 UpdateCharacterLists 로 다시 채울 수 있어
        // 직접 순회하지 않고 스냅숏을 뜬다(ToList 와 같은 의미). 재진입하면 바깥 foreach 가 「Collection was modified」로 시끄럽게 실패한다.
        static readonly List<UnitEntityData> tickUnits = new List<UnitEntityData>();

        public static void Tick(UnityModManager.ModEntry entry, float dt)
        {
            if (Feature == null || ModContext.BuildFailed) return; // 정당한 무동작: 빌드 안 됨
            var game = Game.Instance;
            if (game == null || game.Player == null || game.CurrentlyLoadedArea == null || game.State == null) return; // 정당한 무동작: 지역 없음
            if (LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess) return; // 정당한 무동작: 로딩 중
            tickUnits.Clear();
            tickUnits.AddRange(game.Player.Party);
            try
            {
                foreach (var u in tickUnits)
                {
                    if (u == null || u.Destroyed) continue;
                    var part = u.Get<UnitPartGolemMaster>();
                    if (part == null || !u.Descriptor.HasFact(Feature)) continue; // 정당한 무동작: 골렘술사 아님
                    var life = part.Life;
                    if (part.Called == false && (life.State == LifecycleState.BoundPending || life.State == LifecycleState.Stale)) continue;
                    bool poll = life.NeedsPoll || (life.State == LifecycleState.Detached && u.GetPet(PetType.AnimalCompanion) != null);
                    if (!poll)
                    {
                        if (!part.Called.HasValue && part.GolemId != null && life.State == LifecycleState.Ready)
                            GolemPresence.Reconcile(u, part);
                        continue;
                    }
                    try { Feed(u, LifeEventKind.Poll); }
                    catch (Exception ex) { part.Error(u, "tick poll threw " + ex.GetType().Name + ": " + ex.Message); }
                }
            }
            finally { tickUnits.Clear(); } // 유닛 참조를 프레임 사이에 붙잡지 않는다
        }
    }

    // 주인 소유 파트. 저장 = [JsonProperty] 4필드만(계획 T3 Interfaces). 생명주기·로그 기록은 런타임 전용(저장 안 함 — 로드 때 GolemId 로 Restore).
    public class UnitPartGolemMaster : UnitPart
    {
        [JsonProperty] public bool Broken;
        [JsonProperty] public string GolemId;
        // Null identifies a save made before manual call/dismiss was introduced.
        [JsonProperty] public bool? Called;
        [JsonProperty] public int AppliedLevel;
        [JsonProperty] public string AppliedKey;

        [JsonIgnore] GolemLifecycle life;
        [JsonIgnore] readonly RepeatCounter errors = new RepeatCounter();




        // 저장 ID 없음 → Unbound, 있음 → BoundPending(R3-01 코어 규칙).
        [JsonIgnore]
        public GolemLifecycle Life
        {
            get
            {
                if (life == null) life = GolemLifecycle.Restore(GolemId);
                return life;
            }
        }

        [JsonIgnore] public BreakState Break { get { return Broken ? BreakState.Broken : BreakState.Intact; } }




         
        void Record(string line) { }


        internal void Info(string text) { ModContext.Log("Golemancer: " + Owner.CharacterName + ": " + text); Record("INFO " + text); }
        internal void Warn(string text) { ModContext.Log("WARN Golemancer: " + Owner.CharacterName + ": " + text); Record("WARN " + text); }

        // ERROR 는 매번 센다(Fix round 2 nm3). 로그는 같은 내용의 1·2·4·8… 번째에 횟수와 함께(매 틱 재시도가 로그를 덮지 않되, 반복이 조용하지 않게). 기록은 매번.
        internal void Error(UnitEntityData master, string text)
        {
            Record("ERROR " + text);
            int n = errors.Hit(master.UniqueId + "|" + text);
            if (RepeatLog.ShouldLog(n)) ModContext.LogError("Golemancer: " + master.CharacterName + ": " + RepeatLog.Text(text, n));
        }

        // 점검용: 이 주인의 ERROR 누적 횟수(같은 문구 합산 아님 — 전부).
        [JsonIgnore] public int ErrorTotal { get { return errors.Total; } }

        internal void CountWait(string reason)
        {



            // 배포 구성: 점검 러너가 없으니 대기 사유 집계도 없다(위 개발 구성 본문의 「점검 밖이면 무동작」과 같은 결과).
        }

        internal void Transition(LifecycleState from, string what)
        {
            var to = Life.State;
            if (from != to) Info("state " + from + " -> " + to + " (" + what + ")");
        }
    }

    // 골렘 특성에 붙는다(주인 소유, 전역 구독 — 선례 src\PackWarden\PackCall.cs:272·AuxUnits.cs:263). 사건마다 GolemMaster 가 관측·코어 호출을 맡는다.
    // 블루프린트 컴포넌트는 모든 주인이 공유한다 → 인스턴스 필드 없음(상태는 주인 파트).
    public class GolemMasterComponent : UnitFactComponentDelegate, IPetHandler, IUnitHandler, IRestFinishedHandler, IUnitLevelUpHandler, IAreaHandler, IInGameHandler, IGlobalSubscriber
    {
        protected override void OnActivate()
        {
            Owner.Ensure<UnitPartGolemMaster>();
            GolemMaster.Feed(Owner, LifeEventKind.Activated);
        }

        protected override void OnDeactivate()
        {
            // AddPet releases its companion when this fact is removed. A new grant must bind its own golem.
            if (!IsReapplying && !Fusion.IsPreview(Owner)) Owner.Remove<UnitPartGolemMaster>();
        }

        public void HandleAddPet(UnitEntityData master, UnitEntityData pet)
        {
            if (!ReferenceEquals(master, Owner)) return; // 정당한 무동작: 다른 주인의 펫
            GolemMaster.Feed(Owner, LifeEventKind.AddPet);
        }

        public void HandleRemovePet(UnitEntityData master, UnitEntityData pet) { } // 정당한 무동작: 펫 이탈은 다음 관측(규칙 ①·④)이 다룬다

        public void HandleUnitBeforeLevelUp(UnitEntityData unit) { } // 정당한 무동작: 레벨업 완료(After)만 경계

         
        public void HandleUnitAfterLevelUp(UnitEntityData unit, LevelUpController controller)
        {
            if (unit == null) return; // 정당한 무동작: 대상 없음
            if (ReferenceEquals(unit, Owner)) { GolemMaster.Feed(Owner, LifeEventKind.MasterLevelUp); return; }
            var part = GolemMaster.PartOf(Owner);
            if (ReferenceEquals(Owner.GetPet(PetType.AnimalCompanion), unit) || (part.GolemId != null && unit.UniqueId == part.GolemId))
                GolemMaster.Feed(Owner, LifeEventKind.PetLevelUp);
        }

        public void OnAreaBeginUnloading() { } // 정당한 무동작: 로드 완료(OnAreaDidLoad)만 경계

        public void OnAreaDidLoad() { GolemMaster.Feed(Owner, LifeEventKind.AreaDidLoad); }

        public void HandleObjectInGameChanged(EntityDataBase entity)
        {
            if (!entity.IsInGame) return;
            var pet = entity as UnitEntityData;
            if ((object)pet == null || !ReferenceEquals(pet.Blueprint, GolemUnit.Unit) || !ReferenceEquals(pet.Master, Owner)) return;
            var part = Owner.Get<UnitPartGolemMaster>();
            if (part != null && part.Called == false && part.GolemId == pet.UniqueId)
                GolemPresence.Reconcile(Owner, part);
        }

        public void HandleUnitDeath(UnitEntityData unit)
        {
            if (unit == null) return; // 정당한 무동작
            GolemMaster.OnGolemDied(Owner, unit);
        }

        public void HandleUnitDestroyed(UnitEntityData unit)
        {
            if (unit == null) return; // 정당한 무동작
            GolemMaster.OnGolemDestroyed(Owner, unit);
        }

        public void HandleUnitSpawned(UnitEntityData unit) { } // 정당한 무동작: IUnitHandler 가 IUnitSpawnHandler 를 상속 — 골렘 규칙과 무관

        public void HandleRestFinished(RestStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (status.RestSucceeded) GolemMaster.OnRestSucceeded(Owner);
            else GolemMaster.OnRestInterrupted(Owner);
        }
    }
}
