using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Feet = Kingmaker.Utility.Feet;
using Rounds = Kingmaker.Utility.Rounds;
using Newtonsoft.Json;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
     
    //   횟수·단계·지속·발동 조건·대상 = PossessionRules, 발동/종료 절차·기록 대조 = PossessionRun(거울과 같은 LedgerReconcile), 소유 = PossessionScope.
    // 여기서는 블루프린트를 만들고, 사건(능력 실행·버프 켜짐/꺼짐·시전 제한 질의)을 코어로 넘기고, 게임 객체를 읽고 쓰는 효과기(GamePossessionEffects)만 구현한다.
    //  - 자원 「강신」: 최대 = 엔진 식 IncreasedByLevelStartPlusDivStep(주술사 레벨) — 매개변수는 PossessionRules.ResourceFormula()(Dials 에서 파생, 시험이 MaxUses 와 같음을 실행 확인).
    //    휴식 회복 = 엔진 Replenish(자원 전부 Restore, RestController.ApplyRest @0012 → UnitHelper.Replenish).
    //  - 능력 「강신」: 행동 = Dials.PossessionAction(신속), 비용 = 자원 1, 시전 제한 = PossessionGate(→ PossessionRules.Observe), 효과 = PossessionAction(→ Apply).
    //  - 효과 버프 I/II/III(지속 = Rounds(단계)): 공통 PossessionBuffComponent(켜짐 = 발동, 꺼짐 = 종료) · II 부터 Airborne 사실 · III 에 주술 DC +2.
    //  - 특성 I/II/III 을 Dials 단계 레벨(5·10·15)에 AddAt. I = 능력 + 자원, II·III = 설명(효과는 단계가 레벨에서 정해진다 — PossessionRules.TierAt).
    // 상태는 주술사 유닛 파트(UnitPartSpiritPossession, 세이브)와 아이템 마법 부여에만 둔다(정적 필드 = 블루프린트 참조뿐).
    public static class Possession
    {
        public static BlueprintAbilityResource Resource;
        public static BlueprintAbility Ability;
        public static BlueprintBuff[] Buffs;       // 효과 버프 I, II, III
        public static BlueprintFeature[] Features; // 특성 I, II, III

        // 효과 버프 정의 = (GUID, 문자열 키, 이 버프에 더하는 효과). 순서 = 단계 I, II, III(설계 §4.7 표의 「추가 효과」 열이 누적: II = I + 비행, III = II + 영의 주술).
        static readonly string[] BuffGuids = { Guids.PossessionBuff1, Guids.PossessionBuff2, Guids.PossessionBuff3 };
        static readonly string[] FeatureGuids = { Guids.PossessionFeature1, Guids.PossessionFeature2, Guids.PossessionFeature3 };

        // 영의 주술 DC 수정치 종류 = 본편 선례 CorsetOfDireWitchcraftBuffWitch(IncreaseSpellDescriptorDC{Hex, +2, UntypedStackable, SpellsOnly false}, jbp 2026-11-09 직독).
        internal const ModifierDescriptor HexModifier = ModifierDescriptor.UntypedStackable;

        public static void Build()
        {
            if (Archetype.Bp == null) throw new InvalidOperationException("WeaponSpiritShaman: Possession.Build() before Archetype.Build() (AddAt needs the archetype)");
            if (Resource != null) throw new InvalidOperationException("WeaponSpiritShaman: Possession.Build() called twice");
            var tiers = PossessionRules.Tiers;
            if (tiers.Length < BuffGuids.Length) throw new InvalidOperationException("WeaponSpiritShaman: Dials.PossessionTiers has " + tiers.Length + " rows, possession I..III need " + BuffGuids.Length);
            var icon = Get<BlueprintAbility>(Guids.Game.ArcaneWeaponSwitchAbility).Icon;
            if (icon == null) throw new InvalidOperationException("WeaponSpiritShaman: ArcaneWeaponSwitchAbility has no icon (possession icon source)");

            Resource = MakeResource(icon, PossessionRules.ResourceFormula());

            // --- 효과 버프 I..III ---
            Buffs = new BlueprintBuff[BuffGuids.Length];
            for (int i = 0; i < BuffGuids.Length; i++)
            {
                string key = "Possession" + (i + 1);
                var b = Create<BlueprintBuff>("WeaponSpiritShamanPossessionBuff" + (i + 1), BuffGuids[i], x =>
                {
                    Names(x, key + ".Name", key + ".Description", icon);
                    x.IsClassFeature = true;
                    x.Stacking = StackingType.Replace;
                    EmptyFxOnRemove(x); // 2026-11-17: 생성 자리에서 빈 FxOnRemove(BlueprintUtil.EmptyFxOnRemove 머리말 — PossessionFx.ApplyBuff 선례와 같은 값)
                });
                // 컴포넌트는 전부 새 인스턴스를 인자 배열에 바로 넣는다(시험 `wiring: AddComponents arguments are new instances`).
                var main = new PossessionBuffComponent();
                if (i == 0) AddComponents(b, main);
                else
                {
                    var fly = new AddFacts(); // II·III: 비행(설계 §4.7 「Airborne 70cffb44… 를 버프 사실로」)
                    SetField(fly, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.Game.Airborne) });
                    if (i == 1) AddComponents(b, main, fly);
                    else AddComponents(b, main, fly, new IncreaseSpellDescriptorDC // III: 영의 주술 DC + Dials(설계 §0-21 Hex = 2^50)
                    {
                        Descriptor = new SpellDescriptorWrapper(SpellDescriptor.Hex),
                        BonusDC = SpiritRules.Dials.PossessionHexDcBonus,
                        ModifierDescriptor = HexModifier,
                        SpellsOnly = false,
                    });
                }
                PossessionFx.ApplyBuff(b); // FX(2026-11-15 승인): 영체 모습 FxOnStart — FxTable 원장, 원본 대조는 PossessionFx.Resolve(어긋나면 던진다)
                VerifyBuff(b, i);
                Buffs[i] = b;
            }

            // --- 능력 ---
            var action = new PossessionAction();
            Ability = Create<BlueprintAbility>("WeaponSpiritShamanPossessionAbility", Guids.PossessionAbility, a =>
            {
                Names(a, "Possession.Name", "Possession.Description", icon);
                a.Type = AbilityType.Supernatural;
                a.Range = AbilityRange.Personal;
                a.ActionType = CommandOf(SpiritRules.Dials.PossessionAction);
                a.CanTargetSelf = true;
                a.SpellResistance = false;
                // 2026-11-16 결함 수리(툴팁 지속·내성 칸 `<null>`): 코드로 만든 능력은 두 필드가 null 이라 "<null>" 이 찍힌다(근거 IL = BlueprintUtil 「LocalizedString 필드」 주석).
                // 지속 = 빈 키(칸 숨김): 강신 지속은 단계별 3·5·7·10 라운드(Dials.PossessionTiers)라 맞는 본편 고정 공용 문구가 없다 — 설명문이 단계별 지속을 적는다(Guids.GameText 주석, 판정 필요).
                // 내성 = 빈 키: 내성 굴림 없음(엔진 선례 본편 ArcaneWeaponSwitchAbility 의 LocalizedSavingThrow 도 빈 키 — 시험이 대조).
                a.LocalizedDuration = NoText();
                a.LocalizedSavingThrow = NoText();
            });
            RequireTooltipFields(Ability, "");
            PossessionFx.ApplyAbility(Ability); // FX 미리 읽기(ResourceAssetIds) — 보이는 FX 는 버프가 낸다
            // 액션 요소의 Owner·name 은 JSON 로드 때만 채워진다(블라스트 메이거스 BlastStrike 와 같음) → 직접 단다.
            action.name = "$PossessionAction$" + Guids.PossessionAbility + "#0";
            action.Owner = Ability;
            var resLogic = new AbilityResourceLogic { Amount = 1 }; // 발동 1회 = 자원 1(설계 §4.7 「하루 횟수」)
            SetField(resLogic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(Guids.PossessionResource));
            SetField(resLogic, "m_IsSpendResource", true);
            AddComponents(Ability, resLogic, new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { action } } }, new PossessionGate());
            var types = Ability.ComponentsArray.Select(c => c.GetType()).ToArray();
            if (!types.SequenceEqual(new[] { typeof(AbilityResourceLogic), typeof(AbilityEffectRunAction), typeof(PossessionGate) }) || resLogic.RequiredResource != Resource || !resLogic.IsSpendResource || resLogic.Amount != 1
                || Ability.ActionType != CommandOf(SpiritRules.Dials.PossessionAction) || Ability.Range != AbilityRange.Personal || !Ability.CanTargetSelf || !ReferenceEquals(action.Owner, Ability)
                || Ability.CasterRestrictions.Length != 1)
                throw new InvalidOperationException("WeaponSpiritShaman: possession ability readback failed (components [" + string.Join(", ", types.Select(t => t.Name)) + "], resource, action type, range, owner, caster restriction)");

            // --- 특성 I..III → Dials 단계 레벨 ---
            Features = new BlueprintFeature[FeatureGuids.Length];
            for (int i = 0; i < FeatureGuids.Length; i++)
            {
                // 이름 = 강신 I·II·III. 설명: I = 강신 능력과 같은 문구, II·III = 특성 전용 문구(StringsTable 「PossessionFeature2/3」 — 2026-11-17 키 분리: 버프 II·III 의 짧은 문구와 따로).
                string nameKey = "Possession" + (i + 1), descKey = i == 0 ? "Possession" : "PossessionFeature" + (i + 1);
                var f = Create<BlueprintFeature>("WeaponSpiritShamanPossessionFeature" + (i + 1), FeatureGuids[i], x =>
                {
                    Names(x, nameKey + ".Name", descKey + ".Description", icon);
                    x.IsClassFeature = true;
                    x.Ranks = 1;
                    x.Groups = new FeatureGroup[0];
                });
                if (i == 0)
                {
                    var res = new AddAbilityResources { RestoreAmount = true };
                    SetField(res, "m_Resource", Ref<BlueprintAbilityResourceReference>(Guids.PossessionResource));
                    var grant = new AddFacts();
                    SetField(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(Guids.PossessionAbility) });
                    AddComponents(f, res, grant);
                    if (res.Resource != Resource || !res.RestoreAmount || f.ComponentsArray.Length != 2) throw new InvalidOperationException("WeaponSpiritShaman: possession feature I readback failed (AddAbilityResources + AddFacts[ability])");
                }
                Features[i] = f;
                Archetype.AddAt(tiers[i].Level, f);
                var levels = Archetype.Bp.AddFeatures.Where(e => e.Features.Contains(f)).Select(e => e.Level).ToArray();
                if (levels.Length != 1 || levels[0] != tiers[i].Level)
                    throw new InvalidOperationException("WeaponSpiritShaman: possession feature " + (i + 1) + " archetype levels [" + string.Join(",", levels) + "], expected [" + tiers[i].Level + "]");
            }
        }

        static BlueprintAbilityResource MakeResource(UnityEngine.Sprite icon, StartPlusDivStep f)
        {
            if (f == null) throw new ArgumentNullException("f");
            var res = Create<BlueprintAbilityResource>("WeaponSpiritShamanPossessionResource", Guids.PossessionResource, r =>
            {
                r.LocalizedName = Strings.L("Possession.Name");
                r.LocalizedDescription = Strings.L("Possession.Description");
                SetField(r, "m_Icon", icon);
                object amount = GetF(r, "m_MaxAmount"); // 새 블루프린트 기본값을 상자에 담은 복사본(Amount 는 비공개 중첩 구조체 — 블라스트 메이거스와 같은 방법)
                SetField(amount, "BaseValue", 0);
                SetField(amount, "IncreasedByLevel", false);
                SetField(amount, "IncreasedByStat", false);
                SetField(amount, "m_Class", new BlueprintCharacterClassReference[0]);
                SetField(amount, "m_Archetypes", new BlueprintArchetypeReference[0]);
                SetField(amount, "IncreasedByLevelStartPlusDivStep", true);
                SetField(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(Guids.Game.ShamanClass) });
                SetField(amount, "m_ArchetypesDiv", new BlueprintArchetypeReference[0]);
                SetField(amount, "StartingLevel", f.StartingLevel);
                SetField(amount, "StartingIncrease", f.StartingIncrease);
                SetField(amount, "LevelStep", f.LevelStep);
                SetField(amount, "PerStepIncrease", f.PerStepIncrease);
                SetField(amount, "MinClassLevelIncrease", 0);
                SetField(amount, "OtherClassesModifier", 0f);
                SetField(r, "m_MaxAmount", amount);
                SetField(r, "m_UseMax", false);
                SetField(r, "m_Max", 0);
            });
            var back = GetF(res, "m_MaxAmount");
            if ((int)GetF(back, "BaseValue") != 0 || (bool)GetF(back, "IncreasedByLevel") || (bool)GetF(back, "IncreasedByStat") || !(bool)GetF(back, "IncreasedByLevelStartPlusDivStep")
                || (int)GetF(back, "StartingLevel") != f.StartingLevel || (int)GetF(back, "StartingIncrease") != f.StartingIncrease || (int)GetF(back, "LevelStep") != f.LevelStep
                || (int)GetF(back, "PerStepIncrease") != f.PerStepIncrease || (int)GetF(back, "MinClassLevelIncrease") != 0 || (float)GetF(back, "OtherClassesModifier") != 0f
                || ((Array)GetF(back, "m_Class")).Length != 0 || ((Array)GetF(back, "m_Archetypes")).Length != 0 || ((Array)GetF(back, "m_ArchetypesDiv")).Length != 0
                || ((BlueprintCharacterClassReference[])GetF(back, "m_ClassDiv")).Select(x => x.Guid.ToString()).SingleOrDefault() != Guids.Game.ShamanClass
                || (bool)GetF(res, "m_UseMax") || (int)GetF(res, "m_Min") != 0)
                throw new InvalidOperationException("WeaponSpiritShaman: possession resource m_MaxAmount readback failed (StartPlusDivStep from PossessionRules.ResourceFormula, ClassDiv = [ShamanClass])");
            return res;
        }

        static void VerifyBuff(BlueprintBuff b, int i)
        {
            var got = b.ComponentsArray.Select(c => c.GetType()).ToList();
            var want = new List<Type> { typeof(PossessionBuffComponent) };
            if (i >= 1) want.Add(typeof(AddFacts));
            if (i >= 2) want.Add(typeof(IncreaseSpellDescriptorDC));
            bool ok = got.SequenceEqual(want) && b.Stacking == StackingType.Replace && b.IsClassFeature;
            var af = b.ComponentsArray.OfType<AddFacts>().ToArray();
            if (af.Length == 1) ok &= af[0].Facts.Count() == 1 && af[0].Facts.First().AssetGuid.ToString() == Guids.Game.Airborne;
            var dc = b.ComponentsArray.OfType<IncreaseSpellDescriptorDC>().ToArray();
            if (dc.Length == 1) ok &= dc[0].Descriptor.Value == SpellDescriptor.Hex && dc[0].BonusDC == SpiritRules.Dials.PossessionHexDcBonus && dc[0].ModifierDescriptor == HexModifier && !dc[0].SpellsOnly;
            if (!ok) throw new InvalidOperationException("WeaponSpiritShaman: possession buff " + (i + 1) + " readback failed (components [" + string.Join(", ", got.Select(t => t.Name)) + "], Replace, Airborne, hex DC)");
        }

        // 판정 불가 원인 기록(키 = UnknownOnce.KeyOf: 유닛·관측·예외 형식·메시지별 1회). 진단 전용 런타임 상태(세이브 아님). 강신·스피릿 배러지 시전 제한이 쓴다
         
        static readonly UnknownOnce unknownLogged = new UnknownOnce();

        internal static void LogUnknown(UnitEntityData u, string what, Exception ex)
        {
            string id = (object)u == null ? "?" : u.UniqueId;
            if (unknownLogged.First(UnknownOnce.KeyOf(id, what, ex)))
                ModContext.LogError("WeaponSpiritShaman: UNKNOWN " + what + " for " + ((object)u == null ? "?" : u.CharacterName) + " — the ability is blocked (" + ex.GetType().Name + ": " + ex.Message + ")");
        }

        internal static void Names(BlueprintUnitFact f, string nameKey, string descKey, UnityEngine.Sprite icon)
        {
            SetField(f, "m_DisplayName", Strings.L(nameKey));
            SetField(f, "m_Description", Strings.L(descKey));
            SetField(f, "m_DescriptionShort", Strings.L(descKey));
            SetField(f, "m_Icon", icon);
        }

        // 빌드 되읽기(강신·융합·스피릿 배러지 능력 공용): 지속 칸의 키 == durationKey("" = 빈 키·칸 숨김), 내성 칸 = 빈 키, 둘 다 null 아님·Shared 없음. 어긋나면 던진다.
        internal static void RequireTooltipFields(BlueprintAbility ab, string durationKey)
        {
            if (ab == null) throw new ArgumentNullException("ab");
            if (durationKey == null) throw new ArgumentNullException("durationKey");
            var d = ab.LocalizedDuration;
            var s = ab.LocalizedSavingThrow;
            if (d == null || s == null || d.Key != durationKey || s.Key != "" || d.Shared != null || s.Shared != null)
                throw new InvalidOperationException("WeaponSpiritShaman: " + ab.name + " tooltip string readback failed (LocalizedDuration key '" + (d == null ? "null" : d.Key) + "', expected '" + durationKey
                    + "'; LocalizedSavingThrow key '" + (s == null ? "null" : s.Key) + "', expected ''; both non-null without a shared asset)");
        }

        // Dials 의 행동 종류 → 게임 행동 형식(SpiritRules.cs DialAction 주석: 게임 형식 대응은 T6). 다른 값 = 대응 없음 → throw.
        internal static UnitCommand.CommandType CommandOf(DialAction a)
        {
            switch (a)
            {
                case DialAction.Swift: return UnitCommand.CommandType.Swift;
                default: throw new ArgumentOutOfRangeException("a", a, "WeaponSpiritShaman: no game command type for possession action " + a);
            }
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            for (var t = o.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(o);
            }
            throw new MissingFieldException("WeaponSpiritShaman: " + o.GetType().Name + "." + name + " not found");
        }

        // ---------------- 실행 ----------------

        // 주술사의 영병(동물 동료 펫). 없으면 null(Observe 의 NoSpirit) — 영병 아닌 동물 동료는 이 아키타입에 올 수 없다(PrerequisitePet NoCompanion, 판정 Q3).
        public static UnitEntityData SpiritOf(UnitEntityData shaman)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            return shaman.GetPet(PetType.AnimalCompanion);
        }

        // 강신 조건 판정. moment = Cast: 시전 제한 질의(PossessionGate — 매 프레임 불릴 수 있다), Effect: 실행 시점 재판정(Apply, 그리고 Fusion.Start 가 Fusion.Check 를 거쳐).
        // 관측만 하고 판정은 PossessionRules.Observe(moment, …) — 실행 시점에는 횟수를 보지 않는다(PossessionRules.CheckMoment 주석, 2026-11-16 수리).
        public static ActivateCheck Check(UnitEntityData shaman, CheckMoment moment)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            if (Resource == null) throw new InvalidOperationException("WeaponSpiritShaman: Possession.Build() has not run");
            if (!shaman.Descriptor.Resources.ContainsResource(Resource)) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " has the possession ability but no possession resource");
            int uses = shaman.Descriptor.Resources.GetResourceAmount(Resource);
            var o = ObserveSpirit(shaman);
            return PossessionRules.Observe(moment, o.Present, o.Alive, o.Hidden, o.Feet, uses);
        }

        // 스피릿 웨폰 관측(있음·살아 있음·숨음·주술사와의 거리). 판정은 하지 않는다 — PossessionRules.Observe 에 그대로 넣는다.
        // 강신(Check)과 스피릿 배러지(SpiritBarrage.ObserveSpirit, 2026-11-16)가 같이 쓴다: 「살아 있고 보이며 30피트 안」 관측은 이 한 곳(시험 `wiring: barrage` 이 DistanceTo 호출처를 고정).
        // 거리는 잴 수 있을 때만(죽었거나 숨었으면 null = 모름 — PossessionRules.CanActivate 가 그 순서로 읽는다).
        internal struct SpiritObservation
        {
            public bool Present, Alive, Hidden;
            public double? Feet;
        }

        internal static SpiritObservation ObserveSpirit(UnitEntityData shaman)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            var pet = SpiritOf(shaman);
            bool present = (object)pet != null, alive = present && !pet.Descriptor.State.IsDead, hidden = present && !pet.IsInGame;
            double? feet = present && alive && !hidden ? shaman.DistanceTo(pet) / Feet.FeetToMetersRatio : (double?)null;
            return new SpiritObservation { Present = present, Alive = alive, Hidden = hidden, Feet = feet };
        }

        // 능력 효과(PossessionAction.RunAction): 단계 = TierAt(주술사 레벨) → 효과 버프 = EffectBuffIndex, 지속 = Rounds(단계).
        public static void Apply(MechanicsContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            var shaman = context.MaybeCaster;
            if ((object)shaman == null) throw new InvalidOperationException("WeaponSpiritShaman: possession ability context has no caster");
            // 실행 시점 재판정(2026-11-16 — 융합·스피릿 배러지와 같은 함수): 횟수는 보지 않는다(엔진이 효과보다 먼저 썼다). 시전과 효과 사이에 스피릿 웨폰 조건이 깨졌으면 얹지 않고 던진다.
            var effectCheck = Check(shaman, CheckMoment.Effect);
            if (!effectCheck.Ok) throw new InvalidOperationException("WeaponSpiritShaman: possession took effect on " + shaman.CharacterName + " while blocked (" + effectCheck.Reason + ") — the spirit condition broke between the cast and the effect");
            if (Buffs == null) throw new InvalidOperationException("WeaponSpiritShaman: Possession.Build() has not run");
            int tier = PossessionRules.TierAt(GameMirrorEffects.ShamanLevelOf(shaman));
            if (tier < 1) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " used possession below the first tier level");
            var buff = Buffs[PossessionRules.EffectBuffIndex(tier, Buffs.Length)];
            int rounds = PossessionRules.Rounds(tier);
            var applied = shaman.Buffs.AddBuff(buff, context, new Rounds(rounds).Seconds);
            if (applied == null) throw new InvalidOperationException("WeaponSpiritShaman: AddBuff(" + buff.name + ") returned null on " + shaman.CharacterName + " (buff refused)");
        }

        // 버프 켜짐 = 발동. 원장은 주술사 파트(부분 실패 시 원장 = 실제 상태: finally Store).
        internal static void BuffActivated(UnitEntityData shaman, Buff buff)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            if (buff == null) throw new ArgumentNullException("buff");
            int rounds = PossessionRules.Rounds(PossessionRules.TierAt(GameMirrorEffects.ShamanLevelOf(shaman)));
            var part = shaman.Ensure<UnitPartSpiritPossession>();
            var fx = new GamePossessionEffects(shaman, buff);
            var ledger = part.ToLedger();
            PossessionReport rep;
            try { rep = PossessionRun.Activate(fx, ledger, rounds); }
            finally { part.Store(ledger); }
            ModContext.Log(PossessionRun.Line(shaman.CharacterName, "start", rep, fx.NameOf));
        }

        // 버프 꺼짐(만료·제거·교체) = 종료: 남은 기록 제거(엔진이 이미 지운 것은 EngineRemoved INFO).
        internal static void BuffDeactivated(UnitEntityData shaman)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            var part = shaman.Ensure<UnitPartSpiritPossession>();
            var fx = new GamePossessionEffects(shaman, null);
            var ledger = part.ToLedger();
            PossessionReport rep;
            try { rep = PossessionRun.End(fx, ledger); }
            finally { part.Store(ledger); }
            ModContext.Log(PossessionRun.Line(shaman.CharacterName, "end", rep, fx.NameOf));
        }




    }

    // 주술사 소유 파트. 저장 = [JsonProperty] Applied 만(거울 파트와 같은 SavedRecord·같은 손상 판정 — SavedRecord.ToLedger/FromLedger).
    public class UnitPartSpiritPossession : UnitPart
    {
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<SavedRecord> Applied;

        public UnitPartSpiritPossession() { Applied = new List<SavedRecord>(); }

        public Ledger ToLedger() { return SavedRecord.ToLedger(Applied, "UnitPartSpiritPossession"); }

        public void Store(Ledger l) { Applied = SavedRecord.FromLedger(l); }
    }

    // 효과 버프 공통 컴포넌트. 블루프린트 컴포넌트 하나를 모든 주술사가 공유하므로 인스턴스 필드를 두지 않는다(상태 = 주술사 파트).
    // 로드: 버프·마법 부여·파트가 함께 저장되므로 OnPostLoad 에서 할 일이 없다(재발동하면 원장이 이중이 된다).
    public class PossessionBuffComponent : UnitBuffComponentDelegate
    {
        protected override void OnActivate() { Possession.BuffActivated(Owner, Buff); }

        protected override void OnDeactivate() { Possession.BuffDeactivated(Owner); }
    }

    // 능력 효과 요소. 인스턴스 필드 없음(블루프린트 요소 1개를 모든 주술사가 공유).
    public class PossessionAction : ContextAction
    {
        public override string GetCaption() { return "WeaponSpiritShaman: spirit possession"; }

        public override void RunAction() { Possession.Apply(Context); }
    }

    // 시전 제한: 영병이 살아 있고 보이며 30피트(Dials) 안, 횟수 남음(PossessionRules.Observe). 엔진 소비자 = AbilityData.get_CanBeCastByCaster·GetUnavailableReason
    // (블라스트 메이거스 BlastStateRestriction 과 같은 인터페이스). UI 문구에는 유닛이 넘어오지 않으므로(IAbilityCasterRestriction.GetAbilityCasterRestrictionUIText()) 조건 전체를 한 문장으로 보인다.
    public class PossessionGate : BlueprintComponent, IAbilityCasterRestriction
    {
         
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
        {
            bool? ok;
            try { ok = Possession.Check(caster, CheckMoment.Cast).Ok; }
            catch (Exception ex) { Possession.LogUnknown(caster, "possession check", ex); ok = null; }
            return PolledQuery.Passes(ok);
        }

        public string GetAbilityCasterRestrictionUIText() { return Strings.Resolve(ModContext.StringPrefix + "Possession.Unavailable"); }
    }

    // IPossessionEffects 의 게임 구현(발동·종료 1회마다 새로 만든다 — 상태 없음).
    //  아이템 찾기 = 주술사 손 칸 8개 + 맨손 무기 + 주술사(파티 공유) 인벤토리의 들리지 않은 아이템(남이 든 아이템은 찾지 않는다 — MirrorScope.Searchable).
    //  우리 인스턴스 = 주술사 손 칸 8개 + 맨손 무기의 마법 부여 중 ParentContext.AssociatedBlueprint 가 강신 효과 버프인 것(PossessionScope.IsOurs).
    public sealed class GamePossessionEffects : IPossessionEffects
    {
        readonly UnitEntityData shaman;
        readonly Buff buff; // Add 에만 필요(부모 문맥). 종료·해체에서는 null.

        public GamePossessionEffects(UnitEntityData shaman, Buff buff)
        {
            if ((object)shaman == null) throw new ArgumentNullException("shaman");
            this.shaman = shaman;
            this.buff = buff;
        }

        UnitBody Body()
        {
            var b = shaman.Body;
            if (b == null || b.HandsEquipmentSets == null || b.HandsEquipmentSets.Count == 0) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " body/weapon sets not available");
            return b;
        }

        List<HandSlot> HandSlots()
        {
            var r = new List<HandSlot>();
            foreach (var set in Body().HandsEquipmentSets)
            {
                if (set == null || set.PrimaryHand == null || set.SecondaryHand == null) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " has a weapon set with a null hand slot");
                r.Add(set.PrimaryHand);
                r.Add(set.SecondaryHand);
            }
            return r;
        }

         
         
        List<KeyValuePair<ItemEntity, ItemHolder>> Candidates()
        {
            var hand = new HashSet<ItemEntity>();
            var all = new List<ItemEntity>();
            var inAll = new HashSet<ItemEntity>();
            foreach (var s in HandSlots()) if (s.HasItem && hand.Add(s.Item)) { all.Add(s.Item); inAll.Add(s.Item); }
            var empty = Body().EmptyHandWeapon;
            var inv = new HashSet<ItemEntity>();
            var invOrder = new List<ItemEntity>();
            if (shaman.Inventory != null) foreach (var it in shaman.Inventory.Items) if (it != null && inv.Add(it)) invOrder.Add(it);
            if (empty != null && inAll.Add(empty)) all.Add(empty);
            foreach (var it in invOrder) if (inAll.Add(it)) all.Add(it);
            var r = new List<KeyValuePair<ItemEntity, ItemHolder>>(all.Count);
            foreach (var it in all)
                r.Add(new KeyValuePair<ItemEntity, ItemHolder>(it, MirrorScope.Classify(hand.Contains(it), empty != null && ReferenceEquals(it, empty), inv.Contains(it), it.HoldingSlot != null)));
            return r;
        }

        List<ItemEntity> OwnItems() { return Candidates().Where(kv => MirrorScope.ListsOurInstances(kv.Value)).Select(kv => kv.Key).ToList(); }

        List<ItemEntity> SearchItems() { return Candidates().Where(kv => MirrorScope.Searchable(kv.Value)).Select(kv => kv.Key).ToList(); }

        ItemEntity Find(string itemId)
        {
            var hit = SearchItems().Where(i => i.UniqueId == itemId).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("WeaponSpiritShaman: two items share UniqueId " + itemId);
            return hit.Length == 1 ? hit[0] : null;
        }

        ItemEntity Require(string itemId)
        {
            var it = Find(itemId);
            if (it == null) throw new InvalidOperationException("WeaponSpiritShaman: possession item " + itemId + " not found on " + shaman.CharacterName);
            return it;
        }

        static ItemEnchantment FactOn(ItemEntity item, string factId)
        {
            var hit = item.Enchantments.Where(e => e.UniqueId == factId).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("WeaponSpiritShaman: two enchantments share UniqueId " + factId + " on " + item.UniqueId);
            return hit.Length == 1 ? hit[0] : null;
        }

        static bool IsOurs(ItemEnchantment e)
        {
            var ctx = e.ParentContext;
            string g = ctx == null || ctx.AssociatedBlueprint == null ? null : ctx.AssociatedBlueprint.AssetGuid.ToString();
            return PossessionScope.IsOurs(g);
        }

        public bool ItemExists(string itemId) { return Find(itemId) != null; }

         
        ItemEntity HeldByOtherItem(string itemId)
        {
            var hit = Candidates().Where(kv => MirrorScope.HeldByOther(kv.Value) && kv.Key.UniqueId == itemId).Select(kv => kv.Key).ToArray();
            if (hit.Length > 1) throw new InvalidOperationException("WeaponSpiritShaman: two items held by other units share UniqueId " + itemId);
            return hit.Length == 1 ? hit[0] : null;
        }

        public HeldByOtherState HeldByOtherFact(string itemId, string factId)
        {
            var it = HeldByOtherItem(itemId);
            if (it == null) return HeldByOtherState.ItemNotFound;
            return FactOn(it, factId) != null ? HeldByOtherState.FactPresent : HeldByOtherState.FactAbsent;
        }

        public void RemoveHeldByOther(string itemId, string factId)
        {
            var item = HeldByOtherItem(itemId);
            if (item == null) throw new InvalidOperationException("WeaponSpiritShaman: item " + itemId + " is not held by another unit");
            var e = FactOn(item, factId);
            if (e == null) throw new InvalidOperationException("WeaponSpiritShaman: enchantment " + factId + " not on " + itemId + " (held by another unit)");
            item.RemoveEnchantment(e);
            if (FactOn(item, factId) != null) throw new InvalidOperationException("WeaponSpiritShaman: RemoveEnchantment left " + factId + " on " + itemId + " (held by another unit)");
        }

        public bool FactExists(string itemId, string factId) { return FactOn(Require(itemId), factId) != null; }

        public void Remove(string itemId, string factId)
        {
            var item = Require(itemId);
            var e = FactOn(item, factId);
            if (e == null) throw new InvalidOperationException("WeaponSpiritShaman: enchantment " + factId + " not on " + itemId);
            item.RemoveEnchantment(e);
            if (FactOn(item, factId) != null) throw new InvalidOperationException("WeaponSpiritShaman: RemoveEnchantment left " + factId + " on " + itemId);
        }

        public IList<OurInstance> ListOurInstances()
        {
            var r = new List<OurInstance>();
            foreach (var item in OwnItems())
                foreach (var e in item.Enchantments)
                    if (IsOurs(e)) r.Add(new OurInstance(item.UniqueId, e.UniqueId, e.Blueprint.AssetGuid.ToString()));
            return r;
        }

        public void Log(string level, string message)
        {
            if (level == "INFO" || level == "WARN") ModContext.Log("WSS " + level + " " + message);
            else ModContext.LogError("WeaponSpiritShaman: " + level + " " + message);
        }

        // 영병의 지금 계획 입력 = 거울과 같은 입력 생성기(GameMirrorEffects.BuildInput), 대상 자기 목록에서 거울 인스턴스만 뺀 것.
        // 영병의 거울 인스턴스가 대상 밖(원천 무기 등)에 있으면 거울 사후 조건이 이미 깨진 상태 → 던진다(그 상태로 복사하지 않는다).
        public PlanInput BuildSpiritInput()
        {
            var pet = Possession.SpiritOf(shaman);
            if ((object)pet == null) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " has no weapon spirit at possession");
            var mfx = GameMirrorEffects.ForPossessionInput(pet);
            var input = mfx.BuildInput();
            var stray = mfx.ListOurInstances().Where(o => o.ItemId != input.Target.ItemId).ToList();
            if (stray.Count != 0) throw new InvalidOperationException("WeaponSpiritShaman: spirit " + pet.CharacterName + " has " + stray.Count + " mirror instance(s) off its target (mirror post-condition broken) — possession refuses to copy");
            return input;
        }

        public PossessionTarget ShamanTarget()
        {
            var b = Body();
            var primary = b.PrimaryHand;
            if (primary == null) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " has no primary hand slot");
            var kind = PossessionRules.ShamanTarget(!primary.HasItem ? HandContent.Empty : primary.Item is ItemEntityWeapon ? HandContent.Weapon : HandContent.NonWeapon);
            ItemEntity target = kind == TargetKind.EmptyHand ? b.EmptyHandWeapon : primary.Item;
            if (target == null) throw new InvalidOperationException("WeaponSpiritShaman: " + shaman.CharacterName + " primary hand is empty and there is no empty-hand weapon");
            return new PossessionTarget { Kind = kind, ItemId = target.UniqueId, OwnGuids = target.Enchantments.Select(e => e.Blueprint.AssetGuid.ToString()).ToArray(), Carrier = GameMirrorEffects.CarrierOf(target, kind) };
        }

        // 판정 Q2: AddEnchantment(bp, 강신 버프 문맥, 지속 라운드) + RemoveOnUnequipItem = true(무기를 바꾸면 엔진이 지운다, OnWillUnequip @00A1~00DA).
        public string Add(string itemId, string enchantmentGuid, int rounds)
        {
            if (buff == null) throw new InvalidOperationException("WeaponSpiritShaman: GamePossessionEffects.Add without the possession buff");
            if (rounds < 1) throw new ArgumentOutOfRangeException("rounds");
            var item = Require(itemId);
            var bp = Get<BlueprintItemEnchantment>(enchantmentGuid);
            var ctx = buff.MaybeContext;
            if (ctx == null) throw new InvalidOperationException("WeaponSpiritShaman: possession buff on " + shaman.CharacterName + " has no context");
            var e = item.AddEnchantment(bp, ctx, new Rounds?(new Rounds(rounds)));
            if (e == null) throw new InvalidOperationException("WeaponSpiritShaman: AddEnchantment returned null for " + bp.name + " on " + item.UniqueId);
            e.RemoveOnUnequipItem = true;
            if (!e.RemoveOnUnequipItem || !IsOurs(e) || FactOn(item, e.UniqueId) == null || string.IsNullOrEmpty(e.UniqueId))
            {
                item.RemoveEnchantment(e);
                throw new InvalidOperationException("WeaponSpiritShaman: possession " + bp.name + " readback failed on " + item.UniqueId + " (RemoveOnUnequip / parent context / presence)");
            }
            return e.UniqueId;
        }

        public string NameOf(string guid) { return Get<BlueprintItemEnchantment>(guid).name; }
    }
}
