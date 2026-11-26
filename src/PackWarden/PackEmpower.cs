using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
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
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static PackWarden.BlueprintUtil;

namespace PackWarden
{
    // 「무리 강화」(소유자 결정 2026-11-16). 규칙·상수·문구 = EmpowerRules(순수). 여기는 블루프린트 조립과 얇은 실행 배선.
    //
    // 버프 적용 방식: 버프를 직접 걸지 않고 **원 주문의 액션 사슬(AbilityEffectRunAction.Actions)을 그대로** 무리 구성원마다 돌린다
    // (접촉 전달형은 전달 능력의 사슬). 본편 ContextActionOnContextCaster.RunAction @0061~006D 와 같은 방식(MechanicsContext.GetDataScope(대상) → ActionList.Run()).
    // 그래서 Magic Fang Greater 의 버프 5종 분기, Barkskin 랭크, Death Ward 의 조건부 버프, 지속 시간이 복제 없이 본편과 같다.
    // 사슬이 읽는 랭크·공유값(ContextRankConfig·ContextCalculateSharedValue)만 새 인스턴스로 복사해 우리 능력에 단다(EmpowerRules.ClonedChainComponentTypes).
    // 시전자 레벨 = 헌터 레벨: 본편 ContextCalculateAbilityParamsBasedOnClass(클래스 레벨로 능력 수치를 계산).
    public static class PackEmpower
    {
        public static BlueprintFeatureSelection Selection;
        // 후보 키 → 되읽기 계획(원 주문의 사슬·랭크 설정·공유값 — 전부 **원본 블루프린트의 객체**. 기대값은 여기서 읽어 계산한다).
        sealed class Plan
        {
            public ActionList Actions;
            public ContextRankConfig[] Ranks;
            public ContextCalculateSharedValue[] Shared;
            public BlueprintBuff[] Primary; // 시전자 사실에 달린 덤 버프(Death Ward 의 Ward Master's Amulet)는 뺀 것
        }
        public sealed class ExpectedBuff { public BlueprintBuff Buff; public long DurationSeconds; }
        sealed class StatExpectation { public AddContextStatBonus Component; public long Value; }
        static readonly Dictionary<string, Plan> plans = new Dictionary<string, Plan>();
        // 꺼진 후보(키 → 사유): 버프에 허용 목록 밖 컴포넌트가 있다 등. 블루프린트는 그대로 만든다(저장 호환) — 선택지에서 빠지고 시전이 막힌다.
        static readonly Dictionary<string, string> disabled = new Dictionary<string, string>();
         
        static readonly Dictionary<string, EmpowerRules.DisabledCause> disabledCause = new Dictionary<string, EmpowerRules.DisabledCause>();
        public static int UndeterminedCount;
        static readonly Dictionary<ContextRankConfig, EmpowerRules.RankSpec> specCache = new Dictionary<ContextRankConfig, EmpowerRules.RankSpec>();
        static BlueprintCharacterClass hunterClass;
        static readonly HashSet<string> reported = new HashSet<string>();
        public static int NotAppliedCount, DiceProblemCount, TargetsUnreadableCount, ValueProblemCount;
        public static string LastProblem;
        // 순수 식(EmpowerRules.RankValue)을 엔진과 대조하는 구간: 클래스 레벨 1~20(본편 클래스 레벨 상한 20 — 헌터 레벨 = 시전자 레벨).
        const int CrossCheckMaxLevel = 20;
        const System.Reflection.BindingFlags AnyInstance = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        public static void Build()
        {
            if (Selection != null) throw new InvalidOperationException("PackWarden: PackEmpower.Build() ran twice");
            if (Archetype.Bp == null) throw new InvalidOperationException("PackWarden: Archetype.Build() must run before PackEmpower.Build()");
            if (AuxUnits.NatureFeature == null) throw new InvalidOperationException("PackWarden: AuxUnits.Build() must run before PackEmpower.Build()");
            var hunterList = Get<BlueprintSpellList>(EmpowerRules.HunterSpellList);
            hunterClass = Get<BlueprintCharacterClass>(Guids.Game.HunterClass);
            // 순수 표(EmpowerRules.RoundsPerRate · SecondsPerRound)를 엔진의 환산과 대조한다: 복제한 상수가 엔진과 어긋나면 로드에서 던진다.
            foreach (DurationRate rate in Enum.GetValues(typeof(DurationRate)))
            {
                double engine = rate.ToRounds().Seconds.TotalSeconds;
                long mine = EmpowerRules.ExpectedDurationSeconds(1, rate.ToString());
                if (engine != mine) throw new InvalidOperationException("PackWarden: duration rate " + rate + " is " + engine + " s in the engine, " + mine + " s in EmpowerRules");
            }
            var features = new List<BlueprintFeatureReference>();
            foreach (var c in EmpowerRules.Candidates)
            {
                var feature = BuildCandidate(c, hunterList);
                // 꺼진 후보는 선택지에 넣지 않는다(블루프린트는 남는다 — 이미 고른 캐릭터의 저장이 깨지지 않게).
                if (!disabled.ContainsKey(c.Key)) features.Add(Ref<BlueprintFeatureReference>(feature.AssetGuid.ToString()));
            }
            // 꺼진 후보 때문에 어느 선택 레벨에 고를 것이 없어지면 레벨업이 막힌다 → 그때는 등록 자체를 실패시킨다(시끄럽게).
            if (!EmpowerRules.SelectionUsable(disabled.Keys))
                throw new InvalidOperationException("PackWarden: too many pack empowerment candidates are disabled (" + string.Join(", ", disabled.Keys.ToArray()) + "); a selection level would have nothing to pick");

            // 선택: 본편 헌터의 여러 레벨 선택(Tandem Techniques — 4·7·10… 레벨에 같은 선택이 들어간다)의 UI 관련 값을 따른다. 전제조건은 무시하지 않는다(레벨 범위 = 전제조건).
            var template = Get<BlueprintFeatureBase>(Guids.Game.TandemTechniques) as BlueprintFeatureSelection;
            if (template == null) throw new InvalidOperationException("PackWarden: TandemTechniques is not a BlueprintFeatureSelection (selection template unknown)");
            var icon = Get<BlueprintUnitFact>(Guids.Game.HunterAnimalFocusFeature).Icon;
            if (icon == null) throw new InvalidOperationException("PackWarden: HunterAnimalFocusFeature has no icon");
            Selection = Create<BlueprintFeatureSelection>("PackWardenEmpowerSelection", EmpowerRules.SelectionGuid, s =>
            {
                s.Group = FeatureGroup.None;
                s.Group2 = FeatureGroup.None;
                s.Mode = template.Mode;
                s.Obligatory = template.Obligatory;
                s.IgnorePrerequisites = false;
                s.ExceptWhiteListed = false;
                s.ShowThisSelection = template.ShowThisSelection;
                s.IsClassFeature = true;
                s.Ranks = template.Ranks;
                s.HideInUI = template.HideInUI;
                s.HideInCharacterSheetAndLevelUp = template.HideInCharacterSheetAndLevelUp;
                s.HideNotAvailibleInUI = template.HideNotAvailibleInUI;
                s.Groups = new FeatureGroup[0];
                SetField(s, "m_DisplayName", Strings.L(EmpowerRules.SelectionNameKey()));
                SetField(s, "m_Description", Strings.L(EmpowerRules.SelectionDescriptionKey()));
                SetField(s, "m_DescriptionShort", Strings.L(EmpowerRules.SelectionDescriptionKey()));
                SetField(s, "m_Icon", icon);
                SetField(s, "m_AllFeatures", features.ToArray());
                SetField(s, "m_Features", features.ToArray());
            });
            if (Selection.AllFeatures.Count() != features.Count || Selection.AllFeatures.Any(f => f == null))
                throw new InvalidOperationException("PackWarden: empower selection readback has " + Selection.AllFeatures.Count() + " features, expected " + features.Count);
            foreach (int level in EmpowerRules.SelectionLevels) Archetype.AddAt(level, Selection);
        }

        // 후보 하나를 검사한 결과. Run = 검사 도중 확인된 사슬의 AbilityEffectRunAction(확인 전에 실패했으면 null — 그 후보는 꺼지고 그 능력은 사슬 없이 만들어진다).
        sealed class Analysis { public AbilityEffectRunAction Run; public Plan Plan; public string Reason; }

        // 원 주문·사슬 조회: 없거나 형식이 다르면 외부 데이터 거절(BlueprintUtil.Get 의 두 실패 — BlueprintNotFoundException · 형식 InvalidCastException)로 바꾼다.
        // 그 밖의 예외(GUID 파싱 — 우리 표의 문자열, 엔진 캐시 내부)는 그대로 올라가 내부 실패로 분류된다.
        static BlueprintAbility LookupAbility(string guid, string role)
        {
            try { return Get<BlueprintAbility>(guid); }
            catch (InvalidCastException e) { throw new EmpowerRules.CandidateDataException("the " + role + " blueprint is not an ability: " + e.Message); }
            catch (BlueprintNotFoundException e) { throw new EmpowerRules.CandidateDataException("the " + role + " blueprint is missing: " + e.Message); }
        }

        static BlueprintFeature BuildCandidate(EmpowerRules.Candidate c, BlueprintSpellList hunterList)
        {
             
            // 실패하면 직업 등록을 멈추지 않고 그 후보만 끈다. 저장 호환용 블루프린트(GUID 고정 — 이미 고른 캐릭터의 저장)는 경계 밖에서 **원 주문 없이도** 만든다(아래 — 아이콘·연출은 있으면 쓴다).
            // 원인 종류(EmpowerRules.ClassifyCandidateFailure): 예상 가능한 외부 데이터 거절(CandidateDataException · 걷기 상한) = DataUnsupported,
            // 그 밖의 예외(이 모드 코드의 NRE 등) = InternalFailure — 스택 전체를 오류 줄에 남기고 사용자 문구가 원인을 다른 모드로 단정하지 않는다.
            // 엔진 API 자체가 다른 경우(MissingMemberException — 필드·메서드를 찾지 못함)는 모든 후보에 공통이라 그대로 던진다(직업 등록 전체 실패).
            // 후보가 너무 많이 꺼지면 Build 의 SelectionUsable 이 등록을 멈춘다(선택 화면에 고를 것이 없어지면 레벨업이 막힌다).
            var a = new Analysis();
            BlueprintAbility spell = null, chain = null;
            UnityEngine.Sprite icon = null;
            EmpowerRules.DisabledCause cause = EmpowerRules.DisabledCause.Unknown;
            string internalTrace = null;
            try
            {
                spell = LookupAbility(c.SpellGuid, "spell");
                chain = LookupAbility(c.ChainGuid, "effect chain");
                Analyze(c, hunterList, spell, chain, a);
                if (a.Reason != null) cause = EmpowerRules.DisabledCause.DataUnsupported; // 허용 목록·계약·단계 검사가 사유로 돌려준 것 = 외부 데이터 모양
                 
                icon = spell.Icon;
                if (icon == null)
                {
                    a.Plan = null;
                    a.Reason = (a.Reason == null ? "" : a.Reason + "; ") + "the spell " + spell.name + " has no icon (the candidate is not shown with a substitute icon)";
                    if (cause == EmpowerRules.DisabledCause.Unknown) cause = EmpowerRules.DisabledCause.DataUnsupported;
                }
            }
            catch (MissingMemberException) { throw; }
            catch (Exception e)
            {
                a.Plan = null;
                cause = EmpowerRules.ClassifyCandidateFailure(e);
                a.Reason = (cause == EmpowerRules.DisabledCause.InternalFailure ? "INTERNAL ERROR (unexpected exception in this mod's analysis — not attributed to another mod): " : "it could not be analysed: ")
                    + e.GetType().Name + ": " + e.Message;
                if (cause == EmpowerRules.DisabledCause.InternalFailure) internalTrace = e.ToString();
            }
            if (a.Reason == null && a.Plan == null) throw new InvalidOperationException("PackWarden: " + c.Key + " analysis ended without a plan or a reason");
            if (a.Reason != null)
            {
                if (cause == EmpowerRules.DisabledCause.Unknown) throw new InvalidOperationException("PackWarden: " + c.Key + " was disabled without a classified cause");
                disabled[c.Key] = a.Reason;
                disabledCause[c.Key] = cause;
                Main.Entry.Logger.Error(EmpowerRules.DisabledLogHead + c.Key + ": " + a.Reason + " (the candidate is left out of the selection and cannot be cast)"
                    + (internalTrace == null ? "" : "\n" + internalTrace));
            }
            else plans[c.Key] = a.Plan;
            var run = a.Run;

            string key = EmpowerRules.KeyOf(c);
            // 저장 호환용 블루프린트: 꺼진 후보는 아이콘이 없을 수 있다(원 주문 조회 실패 · 아이콘 결측) — 그때는 아이콘 없이 만든다(시전 제한이 막고 선택지에서 빠진다).
            if (icon == null && a.Reason == null) throw new InvalidOperationException("PackWarden: " + c.Key + " is enabled without an icon");

            // 자원: 능력마다 따로 하루 UsesPerDay 회.
            var resource = Create<BlueprintAbilityResource>("PackWardenEmpower" + c.Key + "Resource", c.ResourceGuid, r =>
            {
                r.LocalizedName = Strings.L(EmpowerRules.NameKey(c));
                r.LocalizedDescription = Strings.L(EmpowerRules.DescriptionKey(c));
                SetField(r, "m_Icon", icon);
                // BlueprintAbilityResource.Amount 는 비공개 중첩 구조체라 리플렉션으로 만든다(기본값 = 레벨·능력치 증가 없음) → 고정 UsesPerDay 회.
                object amount = Activator.CreateInstance(AmountField().FieldType);
                SetField(amount, "BaseValue", EmpowerRules.UsesPerDay);
                AmountField().SetValue(r, amount);
            });
            object maxAmount = AmountField().GetValue(resource);
            const System.Reflection.BindingFlags AnyInstance = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var fBase = maxAmount.GetType().GetField("BaseValue", AnyInstance);
            var fByLevel = maxAmount.GetType().GetField("IncreasedByLevel", AnyInstance);
            var fByStat = maxAmount.GetType().GetField("IncreasedByStat", AnyInstance);
            if (fBase == null || fByLevel == null || fByStat == null) throw new MissingFieldException("PackWarden: BlueprintAbilityResource.Amount fields not found");
            if ((int)fBase.GetValue(maxAmount) != EmpowerRules.UsesPerDay || (bool)fByLevel.GetValue(maxAmount) || (bool)fByStat.GetValue(maxAmount))
                throw new InvalidOperationException("PackWarden: " + resource.name + " max amount readback is not a flat " + EmpowerRules.UsesPerDay);

            var ability = Create<BlueprintAbility>("PackWardenEmpower" + c.Key + "Ability", c.AbilityGuid, a =>
            {
                PackCall.Names(a, key, icon);
                a.Type = (AbilityType)Enum.Parse(typeof(AbilityType), EmpowerRules.AbilityTypeName);
                a.ActionType = (UnitCommand.CommandType)Enum.Parse(typeof(UnitCommand.CommandType), EmpowerRules.ActionTypeName);
                a.Range = AbilityRange.Personal;
                a.CanTargetSelf = true;
                a.CanTargetFriends = false;
                a.CanTargetEnemies = false;
                a.CanTargetPoint = false;
                a.EffectOnAlly = AbilityEffectOnUnit.Helpful;
                a.EffectOnEnemy = AbilityEffectOnUnit.None;
                // 주문 저항·연출·지속/내성 표기는 원 주문을 따른다. 원 주문을 찾지 못한 꺼진 후보(spell == null)는 엔진 기본값으로 둔다 — 시전 제한이 막아 쓰이지 않는다.
                // 단 지속/내성 칸은 엔진 기본값이 null 이라(툴팁 "<null>", 빌드 끝 RequireOwnLocalizedStrings 가 거부) 「문구 없음」(칸 숨김)을 먼저 넣는다 — 원 주문이 있으면 아래에서 그 값으로 바꾼다(2026-11-17).
                a.LocalizedDuration = NoText();
                a.LocalizedSavingThrow = NoText();
                if (spell == null) return;
                a.SpellResistance = spell.SpellResistance;
                a.Animation = spell.Animation;
                a.AnimationStyle = spell.AnimationStyle;
                a.HasFastAnimation = spell.HasFastAnimation;
                a.LocalizedDuration = spell.LocalizedDuration;
                a.LocalizedSavingThrow = spell.LocalizedSavingThrow;
                a.ResourceAssetIds = spell.ResourceAssetIds != null ? spell.ResourceAssetIds.ToArray() : new string[0];
            });
            if (spell == null && a.Reason == null) throw new InvalidOperationException("PackWarden: " + c.Key + " is enabled without its spell");
            if (ability.ActionType.ToString() != EmpowerRules.ActionTypeName || ability.Type.ToString() != EmpowerRules.AbilityTypeName || ability.IsFullRoundAction)
                throw new InvalidOperationException("PackWarden: " + ability.name + " readback is " + ability.Type + " / " + ability.ActionType + ", expected " + EmpowerRules.AbilityTypeName + " / " + EmpowerRules.ActionTypeName);

            var comps = new List<BlueprintComponent>();
            // 사슬이 읽는 랭크·공유값: 새 인스턴스로 복사(공유 인스턴스의 OwnerBlueprint 를 건드리지 않는다). 능력 단계의 주문 서술자는 복사하지 않는다(EmpowerRules 주석 — 기승 중 하차 방지).
             
            if (a.Reason == null)
                foreach (var x in chain.ComponentsArray ?? new BlueprintComponent[0])
                {
                    if (x == null) throw new InvalidOperationException("PackWarden: null component on " + chain.name); // 검사(Readings)가 이미 거른 모양 — 여기 오면 내부 모순
                    if (EmpowerRules.ClonedChainComponentTypes.Contains(x.GetType().Name)) comps.Add(CloneComponent(x));
                }
            var calc = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Wisdom };
            SetField(calc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.HunterClass));
            comps.Add(calc);
            var logic = new AbilityResourceLogic { Amount = 1 };
            SetField(logic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(c.ResourceGuid));
            SetField(logic, "m_IsSpendResource", true);
            comps.Add(logic);
             
            foreach (var kind in EmpowerRules.RestrictionOrder) comps.Add(new PackEmpowerCasterRestriction { Key = c.Key, Kind = kind });
            // 사슬을 확인하기 전에 꺼진 후보(run == null)도 능력 블루프린트는 만든다(저장 호환). 그 능력은 시전 제한에 막히고, 액션도 꺼진 후보를 먼저 거른다(PackEmpowerOnPack.RunAction) —
            // 그래서 아래의 사슬 없음(Actions = null)·내성 Unknown 은 실행되지 않는 자리 표시다(거르기 전에 Actions 를 쓰는 곳이 없다).
            var onPack = new PackEmpowerOnPack { name = "$PackEmpowerOnPack$" + c.AbilityGuid, Key = c.Key, Actions = run == null ? null : run.Actions };
            comps.Add(new AbilityEffectRunAction { SavingThrowType = run == null ? SavingThrowType.Unknown : run.SavingThrowType, Actions = new ActionList { Actions = new GameAction[] { onPack } } });
            AddComponents(ability, comps.ToArray());
            if (!ReferenceEquals(logic.RequiredResource, resource) || !logic.IsSpendResource) throw new InvalidOperationException("PackWarden: " + ability.name + " resource logic readback failed");
            if (ability.GetComponent<SpellDescriptorComponent>() != null) throw new InvalidOperationException("PackWarden: " + ability.name + " must not carry a spell descriptor (a Polymorph descriptor would dismount the rider)");

            // 선택지 특성 = 여러 줄 설명(레벨업 선택 화면의 상세 창이 읽는다), 능력·자원 = 짧은 설명. 이름은 같다(EmpowerRules 의 키 분리 주석, 2026-11-17).
            var feature = PackCall.Feature("PackWardenEmpower" + c.Key + "Feature", c.FeatureGuid, EmpowerRules.NameKey(c), EmpowerRules.ChoiceDescriptionKey(c), icon);
            var addFacts = new AddFacts();
            SetField(addFacts, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(c.AbilityGuid) });
            var addRes = new AddAbilityResources { RestoreAmount = true };
            SetField(addRes, "m_Resource", Ref<BlueprintAbilityResourceReference>(c.ResourceGuid));
            var prereq = new PrerequisiteClassLevel { Level = EmpowerRules.MinHunterLevelFor(c.HunterSpellLevel) };
            SetField(prereq, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.HunterClass));
            AddComponents(feature, addFacts, addRes, prereq);
            if (!addFacts.Facts.Contains(ability) || !ReferenceEquals(addRes.Resource, resource) || prereq.CharacterClass == null || feature.Ranks != 1)
                throw new InvalidOperationException("PackWarden: " + feature.name + " readback failed (ability / resource / prerequisite / single rank)");
            return feature;
        }

        // 후보 하나의 검사. 데이터 모양 문제는 던지거나(호출부의 후보별 경계가 사유로 바꾼다) a.Reason 에 싣는다. 통과하면 a.Plan.
        // 걷기 예산(검토 2026-11-17 지적 4): 사슬 액션(CollectBuffs · TierProblem · WalkExpected) · 버프 사이 재귀(BuffProblems) · 버프 부품 안 객체(ReachableOf)를 **한 예산**으로 센다.
        static void Analyze(EmpowerRules.Candidate c, BlueprintSpellList hunterList, BlueprintAbility spell, BlueprintAbility chain, Analysis a)
        {
            var budget = new EmpowerRules.WalkBudget();
            // 헌터 주문 목록 기준 레벨이 표와 같아야 한다(목록에 없거나 레벨이 다르면 표가 낡았다).
            var listed = spell.GetComponents<SpellListComponent>().Where(x => ReferenceEquals(x.SpellList, hunterList)).ToArray();
            if (listed.Length != 1 || listed[0].SpellLevel != c.HunterSpellLevel)
                throw new EmpowerRules.CandidateDataException("PackWarden: " + spell.name + " is " + (listed.Length == 0 ? "not in the hunter spell list" : "hunter spell level " + string.Join("/", listed.Select(x => x.SpellLevel.ToString()).ToArray())) + ", table says level " + c.HunterSpellLevel);
            // 사슬 능력: 접촉 전달형이면 전달 능력, 아니면 주문 자신.
            var sticky = spell.GetComponent<AbilityEffectStickyTouch>();
            if (sticky != null ? !ReferenceEquals(sticky.TouchDeliveryAbility, chain) : !ReferenceEquals(chain, spell))
                throw new EmpowerRules.CandidateDataException("PackWarden: " + spell.name + " effect chain is not " + chain.name + " (touch delivery = " + (sticky == null ? "none" : sticky.TouchDeliveryAbility == null ? "null" : sticky.TouchDeliveryAbility.name) + ")");
            var runs = chain.GetComponents<AbilityEffectRunAction>().ToArray();
            if (runs.Length != 1 || runs[0].Actions == null || !runs[0].Actions.HasActions)
                throw new EmpowerRules.CandidateDataException("PackWarden: " + chain.name + " has " + runs.Length + " AbilityEffectRunAction with actions, expected 1");
            a.Run = runs[0];
            // 대상 제한 컴포넌트가 있는 주문은 보조가 걸러질 수 있다 → 우회하지 않고 그 후보를 끈다(지금 10개에는 없다 — 시험이 게임 데이터로 확인).
            foreach (var bp in new[] { spell, chain })
                if (bp.GetComponents<IAbilityTargetRestriction>().Any())
                    throw new EmpowerRules.CandidateDataException("PackWarden: " + bp.name + " has a target restriction component; pack members might not qualify");
            // 사슬의 버프를 모은다: 모르는 액션 형식은 건너뛰지 않고 실패.
            var primary = new List<BlueprintBuff>();
            var extra = new List<BlueprintBuff>();
            CollectBuffs(runs[0].Actions, false, primary, extra, chain.name, budget, 0);
            if (primary.Count == 0) throw new EmpowerRules.CandidateDataException("PackWarden: " + chain.name + " applies no unconditional buff (nothing to read back)");
            // 허용 목록(소유자 지시 2026-11-16 마무리): 버프의 컴포넌트 형식이 EmpowerRules 원장에 전부 있어야 한다. 하나라도 없으면 — 게임 패치·다른 모드가 그 버프를 바꿨다 —
            // 그 후보만 끈다. 직업 전체를 못 쓰게 하지 않는 이유: 버프 하나가 바뀐 것으로 이미 이 직업인 캐릭터의 저장까지 깨는 것은 과하다. 꺼진 후보는 오류 줄 + 선택지 제외 + 시전 불가(사유 표시)로 드러난다.
            // 버프의 컴포넌트가 액션 목록으로 거는 버프(중첩 — 예: Natural Rhythm 의 AddInitiatorAttackRollTrigger → NaturalRythmBuffStackable)까지 재귀로 같은 목록으로 본다.
            var problems = new List<string>();
            var seenBuffs = new List<BlueprintBuff>();
            var acceptedForeign = new List<string>();
            foreach (var b in primary.Concat(extra)) BuffProblems(b, b.name, seenBuffs, problems, c.Key, acceptedForeign, budget, 0);
            // 알려진 외부 부품(EmpowerRules.KnownForeignBuffComponents)을 받아들였으면 조용히 넘기지 않고 부품마다 한 줄 남긴다(그 후보가 다른 사유로 꺼져도 남긴다).
            foreach (var line in acceptedForeign) Main.Entry.Logger.Log(line);
            if (problems.Count > 0) { a.Reason = string.Join("; ", problems.ToArray()); return; }
            var plan = new Plan
            {
                Actions = runs[0].Actions,
                Ranks = chain.GetComponents<ContextRankConfig>().ToArray(),
                Shared = chain.GetComponents<ContextCalculateSharedValue>().ToArray(),
                Primary = primary.ToArray(),
            };
            a.Reason = TierProblem(plan.Actions, budget, 0);
            if (a.Reason != null) return;
            // 전 구간(헌터 레벨 1~20 → 시전자 레벨)에서 기대값을 한 번 계산해 본다: 모르는 모양(진행 방식·지속 형식·조건)은 여기서 던진다 — 전투 중에 처음 알게 되지 않게.
            // 같은 자리에서 필수 효과 계약(EmpowerRules.EffectContracts — 설명문 사본과 같은 원장)과 설명문의 지속을 실제 사슬·버프와 대조한다(검토 2026-11-17 지적 1).
            var contract = new List<string>();
            var chainReadings = Readings(chain);
            for (int hl = 1; hl <= CrossCheckMaxLevel; hl++)
            {
                int cl = EmpowerRules.CasterLevelFor(hl);
                var expect = ExpectedBuffs(plan, cl, c.Key, budget);
                if (expect.Count == 0) throw new EmpowerRules.CandidateDataException("PackWarden: " + c.Key + " applies no buff at caster level " + cl);
                contract.AddRange(EmpowerRules.ContractProblems(c.Key, EmpowerRules.ContractTarget.Chain, chain.name, chainReadings, hl));
                foreach (var e in expect)
                {
                    if (!primary.Contains(e.Buff)) throw new InvalidOperationException("PackWarden: " + c.Key + " expected buff " + e.Buff.name + " is not among the collected buffs");
                    ExpectedStats(e.Buff, cl, c.Key);
                    contract.AddRange(EmpowerRules.ContractProblems(c.Key, EmpowerRules.ContractTarget.ExpectedBuff, e.Buff.name, Readings(e.Buff), hl));
                    string d = EmpowerRules.DurationContractProblem(c, hl, e.Buff.name, e.DurationSeconds);
                    if (d != null) contract.Add(d);
                }
                if (contract.Count > 0) break; // 가장 낮은 어긋난 레벨의 문제만 싣는다(상수 어긋남이 레벨마다 20번 반복되지 않게) — 어차피 그 후보는 꺼진다
            }
            if (contract.Count > 0) { a.Reason = EmpowerRules.ContractMismatchHead + string.Join("; ", contract.Distinct().ToArray()); return; }
            a.Plan = plan;
        }

        // 계약 대조용 부품 읽기: 엔진 어셈블리의 부품만(다른 모드 부품은 허용 목록·알려진 외부 부품 원장의 몫). 값은 필드 경로를 리플렉션으로 따라가 EmpowerRules.CanonicalText 로.
        // 경로의 필드가 없거나 값이 null 이면 던진다(계약 대조가 그 필드를 「읽지 못함」 문제로 싣는다 — 기본값을 만들지 않는다).
        static List<EmpowerRules.ComponentReading> Readings(BlueprintScriptableObject bp)
        {
            var engine = typeof(BlueprintComponent).Assembly;
            var list = new List<EmpowerRules.ComponentReading>();
            foreach (var comp in bp.ComponentsArray ?? new BlueprintComponent[0])
            {
                if (comp == null) throw new EmpowerRules.CandidateDataException("PackWarden: null component on " + bp.name);
                if (comp.GetType().Assembly != engine) continue;
                var x = comp;
                list.Add(new EmpowerRules.ComponentReading(comp.GetType().Name, path => EmpowerRules.CanonicalText(ReadPath(x, path)), path => ReadPresence(x, path)));
            }
            return list;
        }

         
        // 중간 필드가 null 이거나 필드가 없거나 모르는 형식이면 던진다(계약 대조가 「읽지 못함」 문제로 싣는다 — 「없음」 으로 넘기지 않는다).
        static string ReadPresence(object root, string path)
        {
            int dot = path.LastIndexOf('.');
            object owner = dot < 0 ? root : ReadPath(root, path.Substring(0, dot));
            string last = dot < 0 ? path : path.Substring(dot + 1);
            var f = FindField(owner.GetType(), last);
            if (f == null) throw new InvalidOperationException("PackWarden: " + owner.GetType().FullName + " has no field '" + last + "' (contract path " + path + ")");
            object v = f.GetValue(owner);
            if (v == null) return EmpowerRules.PresenceNone;
            var r = v as BlueprintReferenceBase;
            if (r != null) return r.Guid == BlueprintGuid.Empty ? EmpowerRules.PresenceNone : "reference !bp_" + r.Guid;
            var calc = v as Kingmaker.EntitySystem.Properties.PropertyCalculator;
            if (calc != null)
            {
                if (calc.Getters == null) throw new InvalidOperationException("PackWarden: " + root.GetType().Name + "." + path + ".Getters is null (the engine's Empty check would throw)");
                return calc.Getters.Length == 0 ? EmpowerRules.PresenceNone : calc.Getters.Length + " property getter(s)";
            }
            throw new NotSupportedException("PackWarden: " + root.GetType().Name + "." + path + " is a " + v.GetType().FullName + " (presence is defined for blueprint references and property calculators only)");
        }

        static object ReadPath(object root, string path)
        {
            object o = root;
            foreach (var part in path.Split('.'))
            {
                if (o == null) throw new InvalidOperationException("PackWarden: " + root.GetType().Name + "." + path + " is null before '" + part + "'");
                var f = FindField(o.GetType(), part);
                if (f == null) throw new InvalidOperationException("PackWarden: " + o.GetType().FullName + " has no field '" + part + "' (contract path " + path + ")");
                o = f.GetValue(o);
            }
            if (o == null) throw new InvalidOperationException("PackWarden: " + root.GetType().Name + "." + path + " is null");
            return o;
        }

        static System.Reflection.FieldInfo AmountField()
        {
            var f = typeof(BlueprintAbilityResource).GetField("m_MaxAmount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f == null) throw new MissingFieldException("PackWarden: BlueprintAbilityResource.m_MaxAmount not found");
            return f;
        }

        static BlueprintComponent CloneComponent(BlueprintComponent src)
        {
            var m = typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (m == null) throw new MissingMethodException("PackWarden: object.MemberwiseClone not found");
            return (BlueprintComponent)m.Invoke(src, null);
        }

        // 사슬에서 버프를 모은다. 아는 액션 = 버프 부여·조건 분기. 조건에 「시전자가 사실 보유」가 있으면 그 가지의 버프는 덤(extra).
        // depth = 사슬 뿌리에서의 깊이, budget = 후보 하나의 걷기 예산(넘으면 EmpowerRules.WalkLimitException — 자기 자신을 품은 조건 사슬도 깊이 상한에서 멈춘다).
        static void CollectBuffs(ActionList list, bool underCasterFact, List<BlueprintBuff> primary, List<BlueprintBuff> extra, string what, EmpowerRules.WalkBudget budget, int depth)
        {
            if (list == null || list.Actions == null) return;
            budget.Step(depth, "an action list of " + what);
            foreach (var a in list.Actions)
            {
                budget.Step(depth + 1, "an action of " + what);
                if (a == null) throw new EmpowerRules.CandidateDataException("PackWarden: null action in " + what);
                var apply = a as ContextActionApplyBuff;
                var cond = a as Conditional;
                if (apply != null)
                {
                    if (apply.Buff == null) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " applies an unresolved buff");
                    (underCasterFact ? extra : primary).Add(apply.Buff);
                }
                else if (cond != null)
                {
                    bool casterFact = underCasterFact || (cond.ConditionsChecker != null && cond.ConditionsChecker.Conditions != null && cond.ConditionsChecker.Conditions.Any(x => x is ContextConditionCasterHasFact));
                    CollectBuffs(cond.IfTrue, casterFact, primary, extra, what, budget, depth + 2);
                    CollectBuffs(cond.IfFalse, casterFact, primary, extra, what, budget, depth + 2);
                }
                else throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " has an action of unknown kind " + a.GetType().Name + " (the pack empowerment only understands buff application and conditionals)");
            }
        }

        // 허용 목록 검사(재귀): 그 버프의 컴포넌트 형식, 컴포넌트 안의 액션 형식, 그 액션(ContextActionApplyBuff)이 거는 버프. 어긋남은 problems 에 사유로 모은다(그 후보를 끈다).
        // path = 사슬에서 그 버프까지의 경로(오류 줄용). seen = 이미 본 버프(참조 동일성 — 순환·중복 방지).
        // candidateKey · acceptedForeign = 알려진 외부 부품을 받아들였을 때 남길 정보 줄(EmpowerRules.KnownForeignAcceptedLine)을 모으는 곳 — 호출부(BuildCandidate)가 모드 로그에 쓴다.
        // depth = 버프가 중첩된 깊이(사슬이 직접 거는 버프 0). 부품 안 걷기는 depth + 1 에서 시작해 같은 budget 을 쓴다(깊이·방문 수가 버프 사이에서 이어진다).
        static void BuffProblems(BlueprintBuff b, string path, List<BlueprintBuff> seen, List<string> problems, string candidateKey, List<string> acceptedForeign, EmpowerRules.WalkBudget budget, int depth)
        {
            if (b == null) throw new ArgumentNullException("b");
            foreach (var s in seen) if (ReferenceEquals(s, b)) return;
            seen.Add(b);
            budget.Step(depth, "buff " + path);
            var comps0 = b.ComponentsArray ?? new BlueprintComponent[0];
            if (comps0.Any(x => x == null)) throw new EmpowerRules.CandidateDataException("PackWarden: null component on buff " + b.name);
            // 컴포넌트 안 어디에 있든(필드 이름·깊이 가정 없음) 액션을 모은다. 다른 블루프린트·참조·Unity 객체로는 넘어가지 않는다 — 버프 부여만 아래에서 따로 따라간다.
            var engine = typeof(BlueprintComponent).Assembly;
            var actions = new List<GameAction>();
            var ledgerNames = new List<string>(); // 엔진 허용 목록으로 판정할 형식 이름: 받아들인 알려진 외부 부품만 뺀다(받아들이지 못한 외부 부품은 남는다 → 허용 목록 밖 사유로도 실린다)
            foreach (var comp in comps0)
            {
                var type = comp.GetType();
                // 원장은 형식의 이름만 본다 → 이름만 같은 다른 어셈블리의 컴포넌트(다른 모드의 형식)는 원장이 가리키는 엔진 형식이 아니다: 통과시키지 않고 사유로.
                bool fromEngine = type.Assembly == engine;
                bool foreignAccepted = false;
                if (!fromEngine)
                {
                    // 엔진 것이 아닌 부품은 「알려진 외부 부품 원장」 의 행(이 버프 · 형식 전체 이름 · 어셈블리 이름)이 있고 필드 값까지 그 행과 같을 때만 받아들인다. 그 밖은 전부 사유.
                    string assemblyName = type.Assembly.GetName().Name;
                    var rule = EmpowerRules.KnownForeignRuleFor(b.AssetGuid.ToString(), type.FullName, assemblyName);
                    if (rule == null)
                        problems.Add("buff " + path + " has component " + type.FullName + " from assembly " + assemblyName + " (the allowlist names the engine's own component types, and EmpowerRules.KnownForeignBuffComponents has no row for this type on this buff)");
                    else
                    {
                        List<string> why;
                        // 필드를 읽지 못하면(그 모드의 다른 판 — 필드 이름·형식이 다름) 「같다」 로 넘기지 않고 사유로.
                        try { why = EmpowerRules.KnownForeignSettingProblems(rule, ForeignSuppressViewOf(comp, rule)); }
                        catch (Exception e) { why = new List<string> { "its fields could not be read: " + e.GetType().Name + ": " + e.Message }; }
                        if (why.Count == 0) { foreignAccepted = true; acceptedForeign.Add(EmpowerRules.KnownForeignAcceptedLine(candidateKey, path, rule)); }
                        else problems.Add("buff " + path + " has known foreign component " + type.FullName + " (" + rule.Mod + " " + rule.PatchMethod + ") that does not match its ledger row: " + string.Join("; ", why.ToArray()));
                    }
                }
                if (!foreignAccepted) ledgerNames.Add(type.Name);
                // 이미 꺼질 사유가 확정된 컴포넌트(엔진 것이 아니고 받아들이지도 못함 — 바로 위 / 허용 목록 밖 — 아래 BuffShapeProblems 가 싣는다)는 걷지 않는다: 그 후보는 어차피 꺼지고, 모르는 컴포넌트일수록 깊은 런타임 객체를 쥘 수 있다.
                // 받아들인 알려진 외부 부품은 엔진 부품과 똑같이 걷는다(안에 액션이 있으면 아래 중첩 액션 원장으로 판정된다 — EmpowerRules 의 원장 주석 「걷기」).
                if (EmpowerRules.BuffComponentAlreadyRejected(fromEngine, foreignAccepted, type.Name)) continue;
                // 걷다가 던지면(읽을 수 없는 객체 · 걷기 상한 EmpowerRules.ReachMaxDepth / ReachMaxVisited 초과) 그 컴포넌트 안은 「본 것」 이 아니다: 통과시키지 않고 사유로(그 후보를 끈다).
                // 걷기 상한(WalkLimitException)은 여기서 삼키지 않는다: 예산은 후보 전체가 같이 쓰므로, 넘었으면 그 뒤의 어떤 걷기도 믿을 수 없다 → 후보별 경계까지 올려 그 후보를 끈다.
                try { actions.AddRange(EmpowerRules.ReachableOf<GameAction>(comp, o => o is SimpleBlueprint || o is BlueprintReferenceBase || o is UnityEngine.Object, budget, depth + 1)); }
                catch (EmpowerRules.WalkLimitException) { throw; }
                catch (Exception e) { problems.Add("buff " + path + " component " + type.Name + " could not be inspected for actions: " + e.GetType().Name + ": " + e.Message); }
            }
            // 원장 판정(컴포넌트 형식·액션 형식)은 순수 함수가 한다 — 돌아온 사유를 그대로 싣는다.
            problems.AddRange(EmpowerRules.BuffShapeProblems(path, ledgerNames, actions.Select(x => x.GetType().Name)));
            foreach (var a in actions)
            {
                var apply = a as ContextActionApplyBuff;
                if (apply == null)
                {
                    // 원장에 없는 이름은 위에서 이미 사유로 넣었다. 이름은 원장에 있는데 엔진의 ContextActionApplyBuff 가 아니면(이름만 같은 형식) 그것도 사유.
                    if (EmpowerRules.UnlistedNestedBuffActions(new[] { a.GetType().Name }).Length == 0) problems.Add("buff " + path + " runs action " + a.GetType().FullName + ", which is not the engine's ContextActionApplyBuff");
                    continue;
                }
                if (apply.Buff == null) { problems.Add("buff " + path + " applies an unresolved buff"); continue; }
                BuffProblems(apply.Buff, path + " -> " + apply.Buff.name, seen, problems, candidateKey, acceptedForeign, budget, depth + 1);
            }
        }

        // [게임] 알려진 외부 부품(TabletopTweaks SuppressBuffsTTT)의 필드 값을 리플렉션으로 읽는다 — 그 모드를 참조하지 않으므로 형식을 직접 쓸 수 없다.
        // 필드가 없거나 형식이 다르면(그 모드의 다른 판) 던진다: 기본값으로 채우지 않는다(호출부가 그 후보를 끄는 사유로 바꾼다). 이 읽기는 원장 행의 형식 하나만 안다.
        static EmpowerRules.ForeignSuppressView ForeignSuppressViewOf(BlueprintComponent comp, EmpowerRules.KnownForeignBuffComponent rule)
        {
            if (rule.TypeFullName != EmpowerRules.TttSuppressBuffsType)
                throw new InvalidOperationException("PackWarden: no field reader for known foreign component type " + rule.TypeFullName + " (only " + EmpowerRules.TttSuppressBuffsType + " is implemented)");
            var t = comp.GetType();
            Func<string, object> read = name =>
            {
                var f = FindField(t, name);
                if (f == null) throw new MissingFieldException("PackWarden: " + t.FullName + "." + name + " not found (the other mod changed)");
                return f.GetValue(comp);
            };
            var refs = read(EmpowerRules.TttSuppressBuffsField) as BlueprintBuffReference[];
            if (refs == null) throw new InvalidOperationException("PackWarden: " + t.FullName + "." + EmpowerRules.TttSuppressBuffsField + " is not a BlueprintBuffReference[]");
            var schools = read(EmpowerRules.TttSuppressSchoolsField) as SpellSchool[];
            if (schools == null) throw new InvalidOperationException("PackWarden: " + t.FullName + "." + EmpowerRules.TttSuppressSchoolsField + " is not a SpellSchool[]");
            object descriptor = read(EmpowerRules.TttSuppressDescriptorField);
            if (!(descriptor is SpellDescriptorWrapper)) throw new InvalidOperationException("PackWarden: " + t.FullName + "." + EmpowerRules.TttSuppressDescriptorField + " is not a SpellDescriptorWrapper");
            object continuous = read(EmpowerRules.TttSuppressContinuousField);
            if (!(continuous is bool)) throw new InvalidOperationException("PackWarden: " + t.FullName + "." + EmpowerRules.TttSuppressContinuousField + " is not a bool");
            var guids = new string[refs.Length];
            for (int i = 0; i < refs.Length; i++)
            {
                if (refs[i] == null) throw new InvalidOperationException("PackWarden: " + t.FullName + "." + EmpowerRules.TttSuppressBuffsField + "[" + i + "] is null");
                guids[i] = refs[i].Guid.ToString();
            }
            return new EmpowerRules.ForeignSuppressView(guids, schools.Length, (long)(SpellDescriptor)(SpellDescriptorWrapper)descriptor, (bool)continuous);
        }

        // 꺼진 후보면 사유, 아니면 null. 모르는 키는 던진다.
        public static string DisabledReason(string key)
        {
            string why;
            if (key != null && disabled.TryGetValue(key, out why)) return why;
            if (key == null || !plans.ContainsKey(key)) throw new InvalidOperationException("PackWarden: '" + key + "' is not a built pack empowerment");
            return null;
        }

        // 꺼진 후보의 원인 종류. 켜진 후보·모르는 키는 던진다(호출부가 DisabledReason 으로 먼저 가른다).
        public static EmpowerRules.DisabledCause DisabledCauseOf(string key)
        {
            EmpowerRules.DisabledCause cause;
            if (key != null && disabledCause.TryGetValue(key, out cause)) return cause;
            throw new InvalidOperationException("PackWarden: '" + key + "' is not a disabled pack empowerment (no disabled cause)");
        }

        static object GetF(ContextRankConfig c, string name)
        {
            var f = typeof(ContextRankConfig).GetField(name, AnyInstance);
            if (f == null) throw new MissingFieldException("PackWarden: ContextRankConfig." + name + " not found");
            return f.GetValue(c);
        }

        // 원본 랭크 설정의 필드 → 순수 표현. 처음 읽을 때 순수 식을 엔진의 ApplyProgression·ApplyMinMax 와 1~20 구간에서 대조한다(식 복제의 어긋남 = 로드 실패).
        static EmpowerRules.RankSpec SpecOf(ContextRankConfig c, string what)
        {
            if (c == null) throw new ArgumentNullException("c");
            EmpowerRules.RankSpec s;
            if (specCache.TryGetValue(c, out s)) return s;
            string baseType = GetF(c, "m_BaseValueType").ToString();
            if (baseType != "CasterLevel") throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " rank is based on " + baseType + " (only CasterLevel is modelled)");
            if ((bool)GetF(c, "m_AffectedByIntensifiedMetamagic")) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " rank is affected by Intensified metamagic (not modelled)");
            s = new EmpowerRules.RankSpec(GetF(c, "m_Progression").ToString(), (int)GetF(c, "m_StartLevel"), (int)GetF(c, "m_StepLevel"), (bool)GetF(c, "m_UseMin"), (int)GetF(c, "m_Min"), (bool)GetF(c, "m_UseMax"), (int)GetF(c, "m_Max"));
            for (int v = 1; v <= CrossCheckMaxLevel; v++) CrossCheckRank(c, s, v, what);
            specCache[c] = s;
            return s;
        }

        // 순수 식(EmpowerRules.RankValue) ↔ 엔진(ApplyMinMax(ApplyProgression(v))) 한 점 대조. 어긋나면 던진다.
        static void CrossCheckRank(ContextRankConfig c, EmpowerRules.RankSpec s, int v, string what)
        {
            var progression = typeof(ContextRankConfig).GetMethod("ApplyProgression", AnyInstance);
            var minMax = typeof(ContextRankConfig).GetMethod("ApplyMinMax", AnyInstance);
            if (progression == null || minMax == null) throw new MissingMethodException("PackWarden: ContextRankConfig.ApplyProgression / ApplyMinMax not found");
            int engine = (int)minMax.Invoke(c, new object[] { (int)progression.Invoke(c, new object[] { v }), false, null });
            int mine = EmpowerRules.RankValue(s, v);
            if (engine != mine) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " rank " + s + " at " + v + " is " + engine + " in the engine, " + mine + " in EmpowerRules");
        }

        // 그 랭크 종류의 값. 설정이 없을 때: 능력의 사슬이면 엔진 기본 = 시전자 레벨(AbilityExecutionContext 생성자가 Ranks 를 Params.CasterLevel 로 채운다), 버프면 근거가 없으니 던진다.
        static int RankOf(IEnumerable<ContextRankConfig> configs, AbilityRankType type, int casterLevel, bool defaultIsCasterLevel, string what)
        {
            ContextRankConfig found = null;
            int n = 0;
            foreach (var c in configs) if (c != null && c.Type == type) { found = c; n++; }
            if (n > 1) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " has " + n + " rank configs of type " + type);
            if (n == 0)
            {
                if (!defaultIsCasterLevel) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " has no rank config of type " + type);
                return casterLevel;
            }
            var spec = SpecOf(found, what);
            // 사전 대조 구간(1~CrossCheckMaxLevel) 밖의 시전자 레벨(클래스 레벨 상한을 올리는 모드 등): 가정하지 않고 그 점을 그 자리에서 엔진과 대조한다(어긋나면 던짐 → 되읽기가 Unreadable 로 보고).
            if (casterLevel < 1 || casterLevel > CrossCheckMaxLevel) CrossCheckRank(found, spec, casterLevel, what);
            return EmpowerRules.RankValue(spec, casterLevel);
        }

        static int SharedOf(Plan p, AbilitySharedValue which, int casterLevel, string what)
        {
            var src = p.Shared.Where(x => x != null && x.ValueType == which).ToArray();
            if (src.Length != 1) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " has " + src.Length + " shared value sources of type " + which);
            var v = src[0].Value;
            if (v == null || v.DiceType != DiceType.Zero || v.BonusValue == null || v.BonusValue.ValueType != ContextValueType.Rank)
                throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " shared value " + which + " is not a plain rank (dice or non-rank bonus are not modelled)");
            return EmpowerRules.SharedValue(RankOf(p.Ranks, v.BonusValue.ValueRank, casterLevel, true, what), src[0].Modifier);
        }

        // 그 시전자 레벨에서 원 주문의 사슬이 걸었어야 하는 버프와 예정 지속(초). 원본 사슬 객체를 걸으며 EmpowerRules 순수 함수로 계산한다. 모르는 모양은 던진다.
        // budget = 조립 때는 그 후보의 검사 예산, 시전 뒤 되읽기(ExpectationFor)에서는 호출마다 새 예산(사슬은 조립 때 이미 상한 안이 확인됐다 — 그래도 상한을 건다).
        static List<ExpectedBuff> ExpectedBuffs(Plan p, int casterLevel, string what, EmpowerRules.WalkBudget budget)
        {
            if (budget == null) throw new ArgumentNullException("budget");
            var into = new List<ExpectedBuff>();
            WalkExpected(p, p.Actions, casterLevel, into, what, budget, 0);
            return into;
        }

        static void WalkExpected(Plan p, ActionList list, int casterLevel, List<ExpectedBuff> into, string what, EmpowerRules.WalkBudget budget, int depth)
        {
            if (list == null || list.Actions == null) return;
            budget.Step(depth, "an action list of " + what);
            foreach (var a in list.Actions)
            {
                budget.Step(depth + 1, "an action of " + what);
                var apply = a as ContextActionApplyBuff;
                var cond = a as Conditional;
                if (apply != null)
                {
                    if (apply.ToCaster || apply.UseDurationSeconds || apply.SameDuration) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " applies " + apply.Buff.name + " to the caster / with a seconds or inherited duration (not modelled)");
                    long seconds;
                    if (apply.Permanent) seconds = EmpowerRules.PermanentDuration;
                    else
                    {
                        var d = apply.DurationValue;
                        if (d == null || d.DiceType != DiceType.Zero || d.BonusValue == null || d.BonusValue.ValueType != ContextValueType.Rank)
                            throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " duration of " + apply.Buff.name + " is not a plain rank (dice or non-rank bonus are not modelled)");
                        seconds = EmpowerRules.ExpectedDurationSeconds(RankOf(p.Ranks, d.BonusValue.ValueRank, casterLevel, true, what), d.Rate.ToString());
                    }
                    into.Add(new ExpectedBuff { Buff = apply.Buff, DurationSeconds = seconds });
                }
                else if (cond != null)
                {
                    var conds = cond.ConditionsChecker == null ? null : cond.ConditionsChecker.Conditions;
                    if (conds == null || conds.Length != 1) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " has a conditional with " + (conds == null ? 0 : conds.Length) + " conditions (exactly one is modelled)");
                    if (conds[0] is ContextConditionCasterHasFact) continue; // 시전자 사실에 달린 덤 버프: 기대에 넣지 않는다
                    var higher = conds[0] as ContextConditionSharedValueHigher;
                    if (higher == null || higher.Not) throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " has a condition of kind " + conds[0].GetType().Name + (higher != null ? " (negated)" : "") + " (not modelled)");
                    bool pass = EmpowerRules.SharedValueConditionPasses(SharedOf(p, higher.SharedValue, casterLevel, what), higher.HigherOrEqual, higher.Inverted);
                    WalkExpected(p, pass ? cond.IfTrue : cond.IfFalse, casterLevel, into, what, budget, depth + 2);
                }
                else throw new EmpowerRules.CandidateDataException("PackWarden: " + what + " has an action of unknown kind " + (a == null ? "null" : a.GetType().Name));
            }
        }

        // 공유값으로 버프를 고르는 가지에서, 그 가지가 거는 버프의 무기 강화 수치가 가지의 문턱과 같아야 한다(Magic Fang, Greater: 단계 N → +N 버프).
        // 휴리스틱 범위: 「조건 = 공유값 >= N, 참 가지의 첫 액션 = EquipmentWeaponTypeEnhancement 를 가진 버프 부여」 모양에만 적용. 어긋나면 사유 문자열(그 후보를 끈다).
        static string TierProblem(ActionList list, EmpowerRules.WalkBudget budget, int depth)
        {
            if (list == null || list.Actions == null) return null;
            budget.Step(depth, "an action list (tier check)");
            foreach (var a in list.Actions)
            {
                budget.Step(depth + 1, "an action (tier check)");
                var cond = a as Conditional;
                if (cond == null) continue;
                var conds = cond.ConditionsChecker == null ? null : cond.ConditionsChecker.Conditions;
                var higher = conds != null && conds.Length == 1 ? conds[0] as ContextConditionSharedValueHigher : null;
                if (higher != null && !higher.Inverted && cond.IfTrue != null && cond.IfTrue.Actions != null && cond.IfTrue.Actions.Length > 0)
                {
                    var apply = cond.IfTrue.Actions[0] as ContextActionApplyBuff;
                    var enh = apply == null || apply.Buff == null ? null : apply.Buff.GetComponent<EquipmentWeaponTypeEnhancement>();
                    if (enh != null && enh.Enhancement != higher.HigherOrEqual)
                        return "buff " + apply.Buff.name + " grants enhancement +" + enh.Enhancement + " on the branch for tier " + higher.HigherOrEqual;
                }
                string inner = TierProblem(cond.IfTrue, budget, depth + 2) ?? TierProblem(cond.IfFalse, budget, depth + 2);
                if (inner != null) return inner;
            }
            return null;
        }

        // 그 버프가 시전자 레벨에 따라 주는 능력치 보너스(AddContextStatBonus 중 값이 랭크인 것). 랭크 설정은 **버프 자신의 것**(원본 블루프린트)에서 읽는다.
        static List<StatExpectation> ExpectedStats(BlueprintBuff buff, int casterLevel, string what)
        {
            var list = new List<StatExpectation>();
            foreach (var c in buff.GetComponents<AddContextStatBonus>())
            {
                if (c.Value == null) throw new EmpowerRules.CandidateDataException("PackWarden: " + buff.name + " AddContextStatBonus has no value");
                if (c.Value.ValueType == ContextValueType.Simple) continue; // 고정값: 시전자 레벨과 무관
                if (c.Value.ValueType != ContextValueType.Rank) throw new EmpowerRules.CandidateDataException("PackWarden: " + buff.name + " stat bonus value of kind " + c.Value.ValueType + " is not modelled");
                if (list.Any(x => x.Component.Stat == c.Stat && x.Component.Descriptor == c.Descriptor)) throw new EmpowerRules.CandidateDataException("PackWarden: " + buff.name + " has two rank-based bonuses to " + c.Stat + " / " + c.Descriptor + " (cannot be told apart on the stat)");
                long v = (long)RankOf(buff.GetComponents<ContextRankConfig>(), c.Value.ValueRank, casterLevel, false, what + " buff " + buff.name) * c.Multiplier;
                if (c.HasMinimal) v = Math.Max(v, c.Minimal);
                list.Add(new StatExpectation { Component = c, Value = v });
            }
            return list;
        }

        // 구성원에게 실제로 붙은 버프의 값 ↔ 원 주문 식의 기대값. 여기는 입력만 모은다 — 합산·판정·문구는 EmpowerRules(SumOwnModifiers · ValueProblems; 시험이 값으로 실행한다).
        // 어긋남·읽을 수 없음은 (구성원, 능력, 버프, 항목, 결과)별 1회 오류 보고 — 일치로 넘기지 않는다. 무리 주인이 건 버프에만 부른다(ReadbackMember — 남의 버프는 대조하지 않는다).
        //   지속: Buff.PlannedDuration = ContextActionApplyBuff.CalculateDuration 이 랭크로 계산해 Buff 생성자에 넘긴 값(생성자 IL @001E 가 유일한 설정 지점). 턴제의 턴 시작 시각 보정과 무관.
        //         그래서 **부여 단계·부여 뒤에 종료 시각만 바꾸는 효과는 보이지 않는다**: RuleCanApplyBuff.Duration(OnTrigger @016D~01AE 스토리 모드 상한, 구독 처리기) →
        //         BuffCollection.PrepareFactForAttach @006C~0281 이 SetEndTime 만 부른다 · EnduringSpells.HandleBuffDidAdded @00C4 SetEndTime · Buff.IncreaseDuration 등.
        //         보이는 것 = 계산 입력이 다른 경우(시전자 레벨 보너스, 복사한 랭크 설정의 어긋남, 지속 값의 Extend 메타매직).
        //   능력치: 그 버프가 출처인 수정치(같은 능력치·같은 종류)의 합.
        static void CheckValues(UnitEntityData leader, string key, UnitEntityData member, Buff buff, ExpectedBuff expected, int casterLevel, int hunterLevel)
        {
            string who = "ability=" + key + " member=" + Who(member) + " buff=" + buff.Blueprint.name;
            string tail = " hunterLevel=" + hunterLevel + " expectedCasterLevel=" + casterLevel + " (leader " + Who(leader) + ")";
            string subjectBase = Who(member) + "|" + buff.Blueprint.AssetGuid + "|";
            var checks = new List<EmpowerRules.ValueCheck>();

            long? actualTicks = null;
            string unreadable = null;
            try
            {
                var planned = buff.PlannedDuration;
                actualTicks = planned.HasValue ? planned.Value.Ticks : EmpowerRules.PermanentDuration;
            }
            catch (Exception e) { unreadable = e.GetType().Name + ": " + e.Message; }
            checks.Add(new EmpowerRules.ValueCheck("duration", EmpowerRules.DurationTicks(expected.DurationSeconds), actualTicks, unreadable, true));

            foreach (var se in ExpectedStats(buff.Blueprint, casterLevel, key))
            {
                long? actual = null;
                unreadable = null;
                try
                {
                    var stat = member.Stats.GetStat(se.Component.Stat);
                    if (stat == null) unreadable = "the unit has no stat " + se.Component.Stat;
                    else
                    {
                        var rows = new List<EmpowerRules.ModifierRow>();
                        foreach (var mod in stat.Modifiers) rows.Add(new EmpowerRules.ModifierRow(mod.Source, mod.ModDescriptor.ToString(), mod.ModValue));
                        actual = EmpowerRules.SumOwnModifiers(rows, buff, se.Component.Descriptor.ToString());
                    }
                }
                catch (Exception e) { unreadable = e.GetType().Name + ": " + e.Message; }
                checks.Add(new EmpowerRules.ValueCheck(se.Component.Stat + "/" + se.Component.Descriptor, se.Value, actual, unreadable, false));
            }
            // 판정 분기는 여기 없다: ValueProblems 가 돌려준 줄(어긋남·읽을 수 없음)을 그대로 보고한다.
            foreach (var p in EmpowerRules.ValueProblems(checks, who, tail)) ReportOnce(p.Message, ref ValueProblemCount, EmpowerRules.OnceKey("value", key, subjectBase + p.What, p.Result.ToString(), null));
        }

        // 대상 = 살아 있고 게임 안에 있는 주 동물 동료 + 살아 있는 보조. 사거리 제한 없음. readable=false = 읽는 도중 실패(「대상 없음」과 다르다).
        public static List<UnitEntityData> Members(UnitEntityData leader, out bool readable)
        {
            var list = new List<UnitEntityData>();
            readable = false;
            if (leader == null) throw new ArgumentNullException("leader");
            try
            {
                var pet = leader.GetPet(PetType.AnimalCompanion);
                if (pet != null && UnitPartPack.IsLive(pet)) list.Add(pet);
                var part = leader.Get<UnitPartPack>(); // 없음 = 소집한 적 없음 = 보조 0(정상)
                if (part != null)
                    for (int i = 0; i < PackRules.AuxSlotCount; i++)
                    {
                        UnitEntityData aux;
                        switch (part.SlotAt(i, out aux))
                        {
                            case PackRules.SlotState.Live: list.Add(aux); break;
                            case PackRules.SlotState.Empty:
                            case PackRules.SlotState.Gone: break; // 빈 자리 · 확인된 죽음/파괴/게임 밖 = 대상 아님(정상)
                            case PackRules.SlotState.Unresolved:
                                // ID 는 있는데 유닛이 해석되지 않는다(검토 2026-11-17 지적 2): 그 보조가 대상인지 알 수 없다 → 목록이 완전하다고 하지 않는다(판정 불가 — 아래 catch 가 보고).
                                throw new InvalidOperationException("PackWarden: pack slot " + i + " holds auxiliary id '" + part.Auxiliaries[i].UniqueId + "' that does not resolve to a unit (the target list would be incomplete)");
                            default: throw new InvalidOperationException("PackWarden: UnitPartPack.SlotAt returned an unknown state for slot " + i);
                        }
                    }
                readable = true;
            }
            catch (Exception e)
            {
                list.Clear();
                ReportOnce(EmpowerRules.TargetsUnreadableLogHead + "leader=" + Who(leader) + ": " + e.GetType().Name + ": " + e.Message, ref TargetsUnreadableCount,
                    EmpowerRules.OnceKey("targets", "(any ability)", Who(leader), EmpowerRules.TargetState.Unreadable.ToString(), e.GetType().Name));
            }
            return list;
        }

        // 오류 줄용 식별 문구. 이름·ID 를 읽다 던지면 그 사실을 문구로 돌려준다(보고 경로가 다시 던지지 않게).
        // 보고 문구·1회 키에서 유닛의 이름·ID 는 이 도우미로만 읽는다(시험이 IL 로 잠근다 — CharacterName 직접 접근은 여기 한 곳).
        internal static string Who(UnitEntityData u)
        {
            if (u == null) return "<null unit>";
            try { return u.CharacterName + " (" + u.UniqueId + ")"; }
            catch (Exception e) { return "<unit unreadable: " + e.GetType().Name + ">"; }
        }

        internal static BlueprintBuff RoutineMarker(string key)
        {
            Plan plan;
            return key != null && plans.TryGetValue(key, out plan) ? plan.Primary.FirstOrDefault() : null;
        }

        internal static bool RoutinePresent(UnitEntityData leader, string key)
        {
            bool readable;
            var members = Members(leader, out readable);
            if (!readable || members.Count == 0) return false;
            var expected = ExpectationFor(leader, key).Buffs;
            return expected.Count != 0 && members.All(member => expected.All(e => member.Buffs.GetBuff(e.Buff) != null));
        }

        sealed class Expectation { public int HunterLevel, CasterLevel; public List<ExpectedBuff> Buffs; public BlueprintBuff[] Primary; }

        // 기대값의 입력 = 지도자의 헌터 레벨(능력의 컨텍스트에서 읽지 않는다 — 우리 능력의 시전자 레벨 계산이 틀려도 드러나게).
        // 기대 버프가 0개로 나와도 여기서 고르지 않는다: 구성원마다 EmpowerRules.ClassifyBuffReadback(0, …) 이 판정 불가(Unreadable)로 분류해 보고한다(시험이 값으로 실행).
        static Expectation ExpectationFor(UnitEntityData leader, string key)
        {
            if (leader == null) throw new ArgumentNullException("leader");
            Plan plan;
            if (key == null || !plans.TryGetValue(key, out plan)) throw new InvalidOperationException("PackWarden: no readback plan for pack empowerment " + key);
            int hunterLevel = leader.Descriptor.Progression.GetClassLevel(hunterClass);
            int casterLevel = EmpowerRules.CasterLevelFor(hunterLevel);
            return new Expectation { HunterLevel = hunterLevel, CasterLevel = casterLevel, Buffs = ExpectedBuffs(plan, casterLevel, key, new EmpowerRules.WalkBudget()), Primary = plan.Primary };
        }

         
        // (참조 · 영구 여부 · 종료 시각 · 랭크 — 없으면 빈 목록). 블루프린트당 GetBuff 하나(첫 인스턴스)만 보면 Stack 의 새 인스턴스·Prolong 의 연장을 놓친다.
        // Readable = 거짓이면 시전 전 상태를 못 읽었다(Why) — 시전 뒤 찾은 버프가 이번 것인지 판정 불가.
        internal sealed class BuffSnapshot { public bool Readable; public string Why; public readonly Dictionary<BlueprintBuff, List<EmpowerRules.BuffState>> Before = new Dictionary<BlueprintBuff, List<EmpowerRules.BuffState>>(); }

        // 그 블루프린트의 모든 인스턴스(엔진 GetBuff 와 같은 비교 — BuffCollection.<GetBuff>b__0 @0000~000E 참조 동일성)와 각 관측. 영구면 종료 시각을 읽지 않는다(Buff.get_IsPermanent @0001~0034).
        static List<EmpowerRules.BuffState> StatesOf(BuffCollection buffs, BlueprintBuff bp, List<Buff> instances)
        {
            if (buffs == null) throw new ArgumentNullException("buffs");
            if (bp == null) throw new ArgumentNullException("bp");
            var states = new List<EmpowerRules.BuffState>();
            foreach (var b in buffs)
            {
                if (b == null) throw new InvalidOperationException("PackWarden: null buff in the member's buff collection");
                if (!ReferenceEquals(b.Blueprint, bp)) continue;
                bool permanent = b.IsPermanent;
                states.Add(new EmpowerRules.BuffState(b, permanent, permanent ? 0L : b.EndTime.Ticks, b.GetRank()));
                if (instances != null) instances.Add(b);
            }
            return states;
        }

        // 시전 직전에 부른다(PackEmpowerOnPack.RunAction). 던지지 않는다: 구성원마다 못 읽으면 그 구성원의 스냅숏을 Readable = 거짓으로 남긴다(되읽기가 판정 불가로 보고).
        internal static Dictionary<UnitEntityData, BuffSnapshot> Snapshot(string key, List<UnitEntityData> members)
        {
            if (members == null) throw new ArgumentNullException("members");
            var all = new Dictionary<UnitEntityData, BuffSnapshot>();
            Plan plan = null;
            bool planned = key != null && plans.TryGetValue(key, out plan);
            foreach (var m in members)
            {
                var s = new BuffSnapshot();
                try
                {
                    if (!planned) throw new InvalidOperationException("PackWarden: no readback plan for pack empowerment " + key);
                    var buffs = m.Buffs;
                    if (buffs == null) throw new InvalidOperationException("PackWarden: the member has no buff collection");
                    foreach (var bp in plan.Primary) s.Before[bp] = StatesOf(buffs, bp, null);
                    s.Readable = true;
                }
                catch (Exception e) { s.Before.Clear(); s.Readable = false; s.Why = e.GetType().Name + ": " + e.Message; }
                all[m] = s;
            }
            return all;
        }

         
        // 인스턴스마다 시전자(무리 주인 / 남 / 읽을 수 없음)를 보고, 입증된 우리 **새** 인스턴스에만 수치 대조를 한다(연장된 옛 인스턴스의 지속은 그때 계산된 값이다). 수치 대조가 던져도 판정은 계속한다.
        // 결과: 붙음 = 문제 없음 · 판정 불가(Undetermined) = UNDETERMINED 줄(실패 아님) · 그 밖 = NOT APPLIED 줄.
        static void ReadbackMember(UnitEntityData leader, string key, UnitEntityData m, Expectation x, BuffSnapshot before)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (x.Buffs == null) throw new InvalidOperationException("PackWarden: " + key + " readback without an expected buff list");
            var proofs = new List<EmpowerRules.BuffProof>();
            var detail = new List<string>(); // 기대 버프마다 입증 결과와 근거(오류 줄용)
            var buffs = m.Buffs;
            if (buffs != null)
            {
                foreach (var expected in x.Buffs)
                {
                    var e = expected;
                    var instances = new List<Buff>();
                    var after = StatesOf(buffs, e.Buff, instances);
                    var casters = new List<EmpowerRules.BuffCaster>();
                    var casterTexts = new List<string>();
                    foreach (var b in instances)
                    {
                        object caster = null;
                        bool casterReadable = false;
                        string casterText;
                        try
                        {
                            var ctx = b.Context;
                            if (ctx == null) casterText = "the buff has no context";
                            else
                            {
                                var u = ctx.MaybeCaster;
                                caster = u;
                                casterReadable = true;
                                casterText = u == null ? "the buff's caster is gone" : Who(u);
                            }
                        }
                        catch (Exception ex) { casterText = ex.GetType().Name + ": " + ex.Message; }
                        casters.Add(EmpowerRules.ClassifyBuffCaster(casterReadable, caster, leader));
                        casterTexts.Add(casterText);
                    }
                    List<EmpowerRules.BuffState> was = null;
                    bool beforeReadable = before != null && before.Readable && before.Before.TryGetValue(e.Buff, out was);
                    string stacking = e.Buff.Stacking.ToString();
                    int ourNew;
                    var proof = EmpowerRules.ProveApplication(beforeReadable, was, after, casters, EmpowerRules.StackingKindOf(stacking), out ourNew);
                    proofs.Add(proof);
                    var who = new List<string>();
                    for (int i = 0; i < casters.Count; i++) who.Add(casters[i] + " (" + casterTexts[i] + ")");
                    detail.Add(e.Buff.name + " proof=" + proof + " stacking=" + stacking + " instances before=" + (beforeReadable ? was.Count.ToString() : "?") + " after=" + after.Count
                        + (casters.Count > 0 ? " casters=[" + string.Join(", ", who.ToArray()) + "]" : "")
                        + (beforeReadable ? "" : " (before: " + (before == null ? "no snapshot of this member" : before.Readable ? "the snapshot has no entry for this buff" : before.Why) + ")"));
                    if (ourNew < 0) continue; // 우리 새 인스턴스가 없다: 수치 대조 없음(남의 것·옛 인스턴스·연장의 지속은 이번 기대값과 비교할 수 없다)
                    var nb = instances[ourNew];
                    EmpowerRules.Guarded(() => CheckValues(leader, key, m, nb, e, x.CasterLevel, x.HunterLevel),
                            ex => ReportOnce(EmpowerRules.ValueLogHead + "what=values result=" + EmpowerRules.ValueReadback.Unreadable + " ability=" + key + " member=" + Who(m) + " buff=" + e.Buff.name + " (leader " + Who(leader) + "): " + ex,
                                ref ValueProblemCount, EmpowerRules.OnceKey("valuethrow", key, Who(m) + "|" + e.Buff.AssetGuid, EmpowerRules.ValueReadback.Unreadable.ToString(), ex.GetType().Name)));
                }
            }
            // 판정은 순수 함수가 한다: 기대 0개 = 판정 불가(붙음으로 읽지 않는다), 버프를 못 읽음 = 판정 불가.
            var r = buffs == null ? EmpowerRules.BuffReadback.Unreadable : EmpowerRules.ClassifyMemberProof(proofs);
            if (r == EmpowerRules.BuffReadback.Present) return;
            string why = string.Join("; ", detail.ToArray());
            // 같은 능력의 다른 단계 버프가 붙어 있으면(예: 단계가 다른 Magic Fang, Greater) 「종류가 다르다」 를 알 수 있게 적는다.
            string others = buffs == null ? "unreadable" : string.Join(", ", x.Primary.Where(bp => buffs.GetBuff(bp) != null).Select(bp => bp.name).ToArray());
            string head = "ability=" + key + " member=" + Who(m) + " result=" + r + " expected [" + string.Join(", ", x.Buffs.Select(e => e.Buff.name).ToArray()) + "] at hunter level " + x.HunterLevel + " has [" + others + "]";
            if (r == EmpowerRules.BuffReadback.Undetermined)
            {
                // 실패가 아니다: 기존 인스턴스를 고치는 중첩(Prolong 등)에서 관측된 변화가 없거나, 변화가 남의 인스턴스에만 있다 — 거절과 「바꿀 것이 없었음」 을 가를 수 없다.
                ReportOnce(EmpowerRules.UndeterminedLogHead + head + " cause=this casting's application could neither be observed nor ruled out (not reported as a failure): " + why + " (leader " + Who(leader) + ")",
                    ref UndeterminedCount, EmpowerRules.OnceKey("undetermined", key, Who(m), r + ":" + why, null));
                return;
            }
            if (!EmpowerRules.IsReadbackProblem(r)) throw new InvalidOperationException("PackWarden: member verdict " + r + " is neither present, undetermined nor a problem");
            // 원인: 알 수 있는 것은 「구성원이 죽었다」 · 「남의 버프만 남았다」 · 「우리 옛 인스턴스만 남았다(새 인스턴스를 만드는 중첩인데 새 것이 없다 = 거절)」 뿐이다. 그 밖의 버프 면역·엔진의 거절은 가를 수 없다.
            string cause = x.Buffs.Count == 0 ? "no buff is expected at caster level " + x.CasterLevel + " (applied and not applied cannot be told apart)"
                : buffs == null ? "the member's buff collection could not be read"
                : r == EmpowerRules.BuffReadback.ForeignCaster ? "another caster's buff is still on the member and no instance of ours appeared or changed, so ours was not applied (the engine refuses a buff before its stacking step): " + why
                : r == EmpowerRules.BuffReadback.NotRefreshed ? "the buff is present but it is the same unchanged instance as before this casting, and its stacking always makes a new instance when the buff is not refused, so this casting was refused: " + why
                : r == EmpowerRules.BuffReadback.Unreadable ? "this casting's application cannot be judged (the buffs before this casting, or the caster of a changed buff, could not be read): " + why
                : m.State != null && m.State.IsDead ? "member died" : "unknown (not applied or removed at once; immunity and engine rejection cannot be told apart): " + why;
            // 1회 키에 결과와 원인을 넣는다: Unreadable 뒤의 Absent, member died 뒤의 unknown 은 다른 문제다.
            ReportOnce(EmpowerRules.NotAppliedLogHead + head + " cause=" + cause + " (leader " + Who(leader) + ")" + EmpowerRules.ChainExceptionHint, ref NotAppliedCount, EmpowerRules.OnceKey("buff", key, Who(m), r + ":" + cause, null));
        }

        // 시전 직전 상태에서 그 구성원의 항목(없으면 null — 판정 불가로 읽힌다). snapshot 자체가 없으면 던진다(호출부 버그).
        static BuffSnapshot SnapshotOf(Dictionary<UnitEntityData, BuffSnapshot> snapshot, UnitEntityData m)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            BuffSnapshot s;
            return snapshot.TryGetValue(m, out s) ? s : null;
        }

        // 강화가 걸린 보조마다 소환 때와 같은 주사위 대조(AuxUnits.VerifyAuxDice). 어긋나면 보고만 한다(이미 전투 중 — 소환 취소 아님).
        static void RecheckMemberDice(string key, UnitEntityData m)
        {
            if (!AuxUnits.IsAuxBlueprint(m.Blueprint)) return;
            string problem = AuxUnits.RecheckDice(m);
            if (problem != null)
                ReportOnce(EmpowerRules.DiceLogHead + "after " + key + " on " + Who(m) + ": " + problem, ref DiceProblemCount, EmpowerRules.OnceKey("dice", key, Who(m), problem, null)); // 결과 = 문제 문구 자체(상태 이름이 따로 없다)
        }

        // 시전 처리 끝: 구성원마다 기대 버프 보유를 되읽고(3상태), 붙은 버프의 수치를 원 주문 식의 기대값과 대조하고, 보조는 주사위 대조를 한 번 더 돌린다.
        // 문제는 (종류, 능력, 구성원, 결과·원인, 예외 형식)별 1회 오류 보고(EmpowerRules.OnceKey) — 게임 진행은 막지 않는다. 반복은 세기만 하고, 시전마다 요약 1줄(Summarize)이 남는다.
        // 단계마다 EmpowerRules.Guarded 로 감싼다: 이 메서드는 엔진 ActionList.Run 아래에서 돌고, 거기서 던진 예외는 게임 로그로만 간다(모드 로그에 줄이 없으면 「오류 없음」 으로 읽힌다).
        // 한 단계(기대값 계산 / 한 구성원의 되읽기 / 한 보조의 주사위 재대조)가 던지면 그 단계만 Unreadable 로 보고하고 나머지는 계속한다. 직접 호출은 Guarded 와 보고뿐(시험이 IL 로 잠근다).
        // snapshot = 시전 직전 상태(Snapshot). 구성원의 항목이 없으면 그 구성원의 찾은 버프는 「이번 시전의 것인지 판정 불가」 다(SnapshotOf 가 null → ReadbackMember 가 Unreadable 로).
        internal static void Readback(UnitEntityData leader, string key, List<UnitEntityData> members, Dictionary<UnitEntityData, BuffSnapshot> snapshot)
        {
            if (members == null) throw new ArgumentNullException("members");
            string unreadable = " result=" + EmpowerRules.ValueReadback.Unreadable + " ability=" + key;
            Expectation x = null;
            EmpowerRules.Guarded(() => x = ExpectationFor(leader, key),
                e => ReportOnce(EmpowerRules.ValueLogHead + "what=expectation" + unreadable + " (no member's buffs or values were compared; leader " + Who(leader) + "): " + e, ref ValueProblemCount,
                    EmpowerRules.OnceKey("expect", key, Who(leader), EmpowerRules.ValueReadback.Unreadable.ToString(), e.GetType().Name)));
            foreach (var member in members)
            {
                var m = member;
                if (x != null)
                    EmpowerRules.Guarded(() => ReadbackMember(leader, key, m, x, SnapshotOf(snapshot, m)),
                        e => ReportOnce(EmpowerRules.ValueLogHead + "what=readback" + unreadable + " member=" + Who(m) + " (leader " + Who(leader) + "): " + e, ref ValueProblemCount,
                            EmpowerRules.OnceKey("readback", key, Who(m), EmpowerRules.ValueReadback.Unreadable.ToString(), e.GetType().Name)));
                EmpowerRules.Guarded(() => RecheckMemberDice(key, m),
                    e => ReportOnce(EmpowerRules.DiceLogHead + "after " + key + " on " + Who(m) + ": UNREADABLE " + e, ref DiceProblemCount,
                        EmpowerRules.OnceKey("dicethrow", key, Who(m), EmpowerRules.ValueReadback.Unreadable.ToString(), e.GetType().Name)));
            }
        }

        // onceKey = EmpowerRules.OnceKey(…) — 결과·예외 형식까지 든 키(시험이 호출부 수를 IL 로 맞춘다). 같은 키의 반복은 세기만 한다(줄 없음) → 시전 요약(Summarize)이 그 수를 보여 준다.
        static void ReportOnce(string message, ref int counter, string onceKey)
        {
            counter++;
            LastProblem = message;
            if (reported.Add(onceKey)) Main.Entry.Logger.Error(message);
        }

        // ReportOnce 가 센 문제의 총수(억제된 반복 포함).
        internal static int ProblemTotal() { return checked(NotAppliedCount + DiceProblemCount + TargetsUnreadableCount + ValueProblemCount); }

        // 시전 요약: 그 시전(before = 시작 때의 ProblemTotal) 동안 센 문제가 있으면 1줄. 판정·문구는 EmpowerRules.SummaryLine(0 이면 null, 음수면 던짐).
        internal static void Summarize(string key, int before)
        {
            string line = EmpowerRules.SummaryLine(key, ProblemTotal() - before);
            if (line != null) Main.Entry.Logger.Error(line);
        }
    }

    // 원 주문의 액션 사슬을 무리 구성원마다 돌리는 액션(능력의 대상은 지도자 자신). 본편 ContextActionOnContextCaster 와 같은 범위 전환.
    public class PackEmpowerOnPack : ContextAction
    {
        public string Key;
        public ActionList Actions;

        public BlueprintBuff RoutineBuff() { return PackEmpower.RoutineMarker(Key); }
        public bool RoutineIsLong() { return EmpowerRules.DurationRateOf(EmpowerRules.Candidates.First(c => c.Key == Key)) != "Rounds"; }
        public bool RoutinePresent(UnitEntityData leader) { return PackEmpower.RoutinePresent(leader, Key); }

        public override string GetCaption() { return "Pack empowerment: run the spell's actions on every pack member"; }

        public override void RunAction()
        {
            // 엔진 ActionList.Run(IL @0013~0057)은 여기서 나간 예외를 전부 잡아 게임 로그 채널로만 보낸다 → 모드 로그에 먼저 남기고 다시 던진다(삼키지 않는다).
            try
            {
                int before = PackEmpower.ProblemTotal(); // 시전 요약의 기준(이 시전에서 센 문제 수 = 끝의 총수 − 이 값)
                var leader = Context.MaybeCaster;
                if (leader == null) throw new InvalidOperationException("PackWarden: pack empowerment " + Key + " ran without a caster");
                // 꺼진 후보를 Actions 보다 먼저 거른다: 사슬을 확인하기 전에 꺼진 후보는 Actions 가 없다(PackEmpower.BuildCandidate — 검토 2026-11-17 지적 5).
                string why = PackEmpower.DisabledReason(Key);
                if (why != null)
                {
                    // 시전 제한이 막았어야 한다. 그래도 왔으면 걸지 않고 남긴다.
                    Main.Entry.Logger.Error(EmpowerRules.DisabledLogHead + Key + " was cast anyway; nothing is applied: " + why);
                    return;
                }
                if (Actions == null) throw new InvalidOperationException("PackWarden: pack empowerment " + Key + " has no action list");
                bool readable;
                var members = PackEmpower.Members(leader, out readable);
                switch (EmpowerRules.ClassifyTargets(readable ? (int?)members.Count : null))
                {
                    case EmpowerRules.TargetState.HasTargets: break;
                    case EmpowerRules.TargetState.Unreadable:
                        // Members 가 이미 오류로 보고했다(같은 문제의 반복이면 줄 없이 세기만 했다 → 요약 줄이 남는다).
                        PackEmpower.Summarize(Key, before);
                        return;
                    case EmpowerRules.TargetState.NoTargets:
                        // 시전 제한(PackEmpowerCasterRestriction)을 통과한 뒤 구성원이 사라졌다: 횟수는 이미 쓰였다 — 조용히 넘기지 않고 남긴다.
                        Main.Entry.Logger.Error("PackWarden: pack empowerment " + Key + " resolved with no pack member (leader " + PackEmpower.Who(leader) + ")");
                        return;
                    default: throw new InvalidOperationException("PackWarden: ClassifyTargets returned an unknown state");
                }
                // 시전 직전의 버프 인스턴스(되읽기가 「이번 시전이 붙였는가」 를 가른다 — 검토 2026-11-17 지적 3). 던지지 않는다(못 읽은 구성원은 판정 불가로 남는다).
                var snapshot = PackEmpower.Snapshot(Key, members);
                foreach (var m in members)
                {
                    try
                    {
                        using (Context.GetDataScope(m)) Actions.Run();
                    }
                    catch (Exception e)
                    {
                        // 이 catch 가 닿는 범위: 범위 전환(GetDataScope)과 그 Dispose 가 던진 예외뿐이다. 사슬 액션(GameAction.RunAction)이 던진 예외는
                        // 엔진 ActionList.Run 이 안에서 삼켜 게임 로그(GameLogFull.txt)로만 보내므로 여기 오지 않는다(IL @0013~0057 — 시험이 잠근다).
                        // 그 경우에도 되읽기가 그 구성원을 「안 붙음」 으로 보고한다. 여기서는 한 구성원의 범위 전환 실패가 나머지 구성원을 막지 않게 한다.
                        // 문구의 이름은 Who 로 읽는다: 이름을 읽다 던지면 이 catch 밖으로 나가 나머지 구성원이 끊긴다.
                        Main.Entry.Logger.Error("PackWarden: pack empowerment " + Key + " failed on " + PackEmpower.Who(m) + ": " + e);
                    }
                }
                PackEmpower.Readback(leader, Key, members, snapshot);
                PackEmpower.Summarize(Key, before);
            }
            catch (Exception e)
            {
                Main.Entry.Logger.Error(EmpowerRules.FailedLogHead + Key + ": " + e);
                throw;
            }
        }
    }

     
    //   Enabled = 꺼진 후보 · TargetsReadable = 구성원 판정 불가(PackEmpower.Members 가 오류 보고) · HasTargets = 구성원 없음(횟수를 쓰지 않는다).
    // 엔진 AbilityData.GetUnavailableReason @006C~009C 는 처음 막는 부품의 문구를 쓴다. 문구는 부품의 Kind(와 꺼진 후보의 원인 종류 — 조립 때 정해진 후보 상태)로만 정해지고,
    // 인스턴스를 모든 시전자가 공유하므로 시전자별 상태(「마지막 결과」)를 두지 않는다. 판정 = 순수 EmpowerRules.RestrictionBlocks.
    public class PackEmpowerCasterRestriction : BlueprintComponent, IAbilityCasterRestriction
    {
        public string Key;
        public EmpowerRules.RestrictionKind Kind;

        public bool IsCasterRestrictionPassed(UnitEntityData caster)
        {
            bool disabled = PackEmpower.DisabledReason(Key) != null;
            var targets = EmpowerRules.TargetState.Unknown;
            if (!disabled && Kind != EmpowerRules.RestrictionKind.Enabled)
            {
                bool readable;
                var members = PackEmpower.Members(caster, out readable);
                targets = EmpowerRules.ClassifyTargets(readable ? (int?)members.Count : null);
            }
            return !EmpowerRules.RestrictionBlocks(Kind, disabled, targets);
        }

        public string GetAbilityCasterRestrictionUIText()
        {
            switch (Kind)
            {
                case EmpowerRules.RestrictionKind.Enabled: return Strings.Resolve("PackWarden." + EmpowerRules.DisabledTextKey(PackEmpower.DisabledCauseOf(Key)));
                case EmpowerRules.RestrictionKind.TargetsReadable: return Strings.Resolve("PackWarden." + EmpowerRules.TargetsUnreadableKey);
                case EmpowerRules.RestrictionKind.HasTargets: return Strings.Resolve("PackWarden." + EmpowerRules.NoTargetsKey);
                default: throw new InvalidOperationException("PackWarden: pack empowerment restriction " + Key + " has kind " + Kind);
            }
        }
    }
}
