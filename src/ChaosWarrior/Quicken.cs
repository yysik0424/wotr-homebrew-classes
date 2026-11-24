using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
    // 분노 중 신속 시전 권리 버프 6개(개편 설계 §12-2 「블러드레이저와 똑같이」 [소유자] · 계획 2026-11-16 Task O8). 모양 = 순수 QuickenSpec, 상한 = ChaosRules.QuickenCaps.
    // 부여는 여기가 아니라 변이 추첨 행동(MutationCore.GrantQuickenRight → GameMutationWorld.ApplyChild)이 분노의 자식으로 한다 — 억제 뒤 재활성(Resumed)에 다시 생기지 않게(정찰 §5.2).
    // 이 빌더는 블루프린트만 만들고 되읽는다(VerifyNow — 필드가 사양과 다르면 던진다).
    //
    // 계획 Task O8 Step 0 (a) — K10 「Once 로 지워진 권리 버프가 분노의 m_StoredFacts 에 참조로 남은 채 분노가 끝날 때」(Assembly-CSharp IL 직독 2026-11-17, md5 앞 12 748b3a143b60;
    //   덤프 %TEMP%\cw-o8\a.txt·b.txt·c.txt·d.txt·e.txt): 갈림 A(무해)로 판정 — 그래서 권리 버프를 분노의 자식으로 저장한다(계획 설계 그대로).
    //   ① 시전 때: AutoMetamagic.OnEventAboutToTrigger(RuleCastSpell) @0018~002C → UnitHelper.RemoveFact(UnitEntityData, EntityFact) @0000~000C → RemoveFact(UnitDescriptor, EntityFact) @0004~0011
    //      → EntityFactsManager.Remove(fact, true) @0013~004A: m_Facts.Remove 가 참이므로 캐시에서 빼고 → OnFactWillDetach(BuffCollection @001F: Buff.OnRemove) → Detach(@004E: Manager = null) → Dispose.
    //      분노의 m_StoredFacts 에는 이 인스턴스 참조가 그대로 남는다(Buff.StoreFact @0038~003F 는 List.Add 뿐, 지우는 곳이 없다).
    //   ② 분노가 끝날 때: Buff.OnRemove @0047~0095 가 저장 칸마다 UnitHelper.RemoveFact(Owner, f) → EntityFactsManager.Remove(f, true):
    //      @0000~0003 f 는 null 아님 · @0004~000A get_Activating = 필드 읽기(@0000~0006, 떨어진 사실에서도 예외 없음, 활성화 도중이 아니므로 false)
    //      · @0013~001A DelegatePrepareFactForDetach @0000~002C(FirstSuitableDelegate @0000~0049 = 형식 검사 IsSuitableFact @0000~0009 뿐; BuffCollection.PrepareFactForDetach @0000~001E 는
    //        RemoveByRank 문맥이고 순위 > 1 일 때만 RemoveRank — 권리 버프는 Replace · 순위 1 이라 그대로 돌려준다; 위임 전체가 try/catch 로 감싸여 있다)
    //      · @001F~002B m_Facts.Remove(f) 가 false(이미 빠졌다) → @004F 로 건너뛴다: 캐시 제거 · OnFactWillDetach · Detach · Dispose 를 하나도 부르지 않는다(로그 줄도 없다 —
    //        EntityFact.Detach @0008~0022 의 「isn't attached」 오류 줄은 이 경로에서 닿지 않는다). → 예외 없는 무동작. 이어서 Buff.OnRemove @008A~0090 이 목록을 비운다.
    //   게임 안 확인 = S13 consumed-then-rage-off-no-errors(오류 장부 0 · 고아 정리 0) + S0 log-capture-zero(모드 이름이 든 오류 줄 0). 게임 실행 전까지 IL 판독에 기댄다.
    // Step 0 (b) AutoMetamagic 필드(IL 2026-11-17): m_AllowedAbilities(비공개 중첩 열거 AllowedType: SpellOrSpellLike −1 · SpellOnly 0 · …) · Metamagic · Abilities(List) · Descriptor · Once · MaxSpellLevel
    //   · School · CheckSpellbook · m_Spellbook(비공개) · m_IncludeClasses · m_ExcludeClasses(비공개 배열). new AutoMetamagic 은 Abilities · 두 배열이 null 이다(주문 격투가 함정 문서 2026-11-08) —
    //   ShouldApplyTo @00B2·@01E3·@0211 이 LinqExtensions.Empty 로 읽고, get_IncludeClasses @0001~000B 는 null 을 그대로 돌려준다 → 빈 컬렉션을 명시적으로 넣고 되읽는다.
    // Step 0 (d) 슬롯 소모: 준비 칸 소모는 시전 명령의 AbilityData.Spend @001E~0023(SpendFromSpellbook)이고 AutoMetamagic 은 능력치 계산에 메타매직만 더한다 — 칸은 평소대로 1개(추정 — 게임 안 S13 first-spell-quickened-slot-minus-1 이 값으로 본다).
    public static class Quicken
    {
        static readonly List<BlueprintBuff> rights = new List<BlueprintBuff>();



        public static bool Built { get { return rights.Count > 0 && rights.Count == QuickenSpec.Rights.Count; } }

        public static void Build()
        {
            if (rights.Count != 0) throw new InvalidOperationException("ChaosWarrior: Quicken.Build() called twice");
            if (Rage.Buff == null) throw new InvalidOperationException("ChaosWarrior: Quicken.Build() before Rage.Build() (the rights are children of the rage buff)");
            QuickenSpec.Validate();
            var book = Get<BlueprintSpellbook>(QuickenSpec.SpellbookGuid);
            if (book.SpellList == null) throw new InvalidOperationException("ChaosWarrior: spellbook " + QuickenSpec.SpellbookGuid + " has no spell list (AutoMetamagic.CheckSpellbook would throw on every spell)");
            // 독립 검토 R4-F1: 상한마다 주문 목록(살아 있는 목록 — 우리 빌드 앞에 다른 모드가 더한 주문 포함). 판독 불가 · 빈 목록이면 SpellListEntries / AbilitiesFor 가 던진다.
            if (!QuickenSpec.AbilitiesFromSpellList) throw new InvalidOperationException("ChaosWarrior: QuickenSpec.AbilitiesFromSpellList is false but no other ability list is defined (R4-F1: an empty list lets variant spells skip the cap)");
            var entries = SpellListEntries(book.SpellList);
            foreach (var row in QuickenSpec.Rights)
            {
                var b = BuildKit.Buff("ChaosWarriorQuickenRight" + row.MaxSpellLevel, row.BuffGuid, QuickenSpec.NameKey, QuickenSpec.DescKey(row.MaxSpellLevel), false, QuickenSpec.Stacking);
                var am = new AutoMetamagic
                {
                    Metamagic = BuildKit.E<Metamagic>(QuickenSpec.Metamagic),
                    Once = QuickenSpec.Once,
                    MaxSpellLevel = row.MaxSpellLevel,
                    CheckSpellbook = QuickenSpec.CheckSpellbook,
                };
                SetField(am, "m_AllowedAbilities", AllowedValue(QuickenSpec.AllowedAbilities));
                SetField(am, "m_Spellbook", Ref<BlueprintSpellbookReference>(QuickenSpec.SpellbookGuid));
                // 대상 무관(개편 설계 §12-2) · 상한 표는 지킨다(R4-F1): 레벨 ≤ 상한인 워프리스트 주문. 대상으로 거르지 않는다.
                var abilities = new List<BlueprintAbilityReference>();
                foreach (var g in QuickenSpec.AbilitiesFor(row.MaxSpellLevel, entries)) abilities.Add(Ref<BlueprintAbilityReference>(g));
                am.Abilities = abilities;
                ModContext.Log("ChaosWarrior: quicken right cap " + row.MaxSpellLevel + ": " + abilities.Count + " warpriest spell(s) of level <= " + row.MaxSpellLevel + " (of " + entries.Count + " in the live list)");
                SetField(am, "m_IncludeClasses", new BlueprintCharacterClassReference[0]);
                SetField(am, "m_ExcludeClasses", new BlueprintCharacterClassReference[0]);
                AddComponents(b, am);
                rights.Add(b);
            }
            VerifyNow();
        }

        // 살아 있는 주문 목록 → (레벨, GUID) 칸. 엔진 BlueprintAbility.IsInSpellList @0000~0049 가 읽는 바로 그 칸(SpellsByLevel[i].SpellsRefs)이다. 목록·레벨 칸·주문 칸·참조가 null 이거나 GUID 가 비면 던진다(그 칸을 건너뛰어 목록을 조용히 줄이지 않는다). 검사는 QuickenSpec.ValidateSpellList 가 이어서 한다.
        internal static List<SpellListEntry> SpellListEntries(BlueprintSpellList list)
        {
            if (list == null) throw new ArgumentNullException("list");
            var levels = list.SpellsByLevel;
            if (levels == null) throw new InvalidOperationException("ChaosWarrior: spell list " + list.name + " has no SpellsByLevel");
            var r = new List<SpellListEntry>();
            foreach (var lv in levels)
            {
                if (lv == null) throw new InvalidOperationException("ChaosWarrior: spell list " + list.name + " has a null level row");
                var refs = lv.SpellsRefs;
                if (refs == null) throw new InvalidOperationException("ChaosWarrior: spell list " + list.name + " level " + lv.SpellLevel + " has no spell references");
                foreach (var s in refs)
                {
                    if (s == null || s.IsEmpty()) throw new InvalidOperationException("ChaosWarrior: spell list " + list.name + " level " + lv.SpellLevel + " has an empty spell reference");
                    r.Add(new SpellListEntry(lv.SpellLevel, s.Guid.ToString()));
                }
            }
            QuickenSpec.ValidateSpellList(r);
            return r;
        }

        // 비공개 중첩 열거 AutoMetamagic/AllowedType 의 이름 → 값(필드 형식에서 찾는다). 이름이 없으면 던진다(정수 리터럴로 넣지 않는다).
        static object AllowedValue(string name)
        {
            var t = BuildReflect.FieldType(typeof(AutoMetamagic), "m_AllowedAbilities");
            if (!t.IsEnum || !Enum.IsDefined(t, name)) throw new ArgumentException("ChaosWarrior: " + t.FullName + " has no member '" + name + "'");
            return Enum.Parse(t, name);
        }

        // 빌드 되읽기(계획 Task O8 Interfaces · R4-F1): 버프 수 · GUID 순서 · 겹 방식 · 보임 · 부품 = AutoMetamagic 정확히 1개 · 필드 전부 == QuickenSpec · MaxSpellLevel == 그 행
        //   · Abilities == 살아 있는 목록의 「레벨 ≤ 상한」(비지 않음) · 두 클래스 배열 non-null·길이 0.
        // 다르면 한 줄씩 모아 던진다(BuildPlan 이 빌드 실패로 기록 — 조용히 넘어가지 않는다).
        public static void VerifyNow()
        {
            var p = new List<string>();
            var spec = QuickenSpec.Rights;
            if (rights.Count != spec.Count) p.Add(rights.Count + " right buffs built, spec has " + spec.Count);
            var stacking = BuildKit.E<StackingType>(QuickenSpec.Stacking);
            var metamagic = BuildKit.E<Metamagic>(QuickenSpec.Metamagic);
            var allowed = AllowedValue(QuickenSpec.AllowedAbilities);
            for (int i = 0; i < Math.Min(rights.Count, spec.Count); i++)
            {
                var b = rights[i];
                var row = spec[i];
                string at = "right " + i + " (" + row + ")";
                if (b.AssetGuid.ToString() != row.BuffGuid) p.Add(at + ": GUID " + b.AssetGuid);
                if (b.Stacking != stacking) p.Add(at + ": stacking " + b.Stacking + " != " + stacking);
                if (b.IsHiddenInUI) p.Add(at + ": hidden (a right is a visible buff)");
                if (b.FxOnRemove == null || b.FxOnStart == null) p.Add(at + ": null FX link (Buff.OnRemove would throw — BuildKit.Buff defect 2026-11-16)");
                if (BuildKit.GetF(b, "m_DisplayName") == null || BuildKit.GetF(b, "m_Description") == null) p.Add(at + ": null display name / description");
                var comps = b.ComponentsArray ?? new BlueprintComponent[0];
                if (comps.Length != 1 || !(comps[0] is AutoMetamagic)) { p.Add(at + ": components [" + string.Join(", ", comps.Select(c => c == null ? "null" : c.GetType().Name).ToArray()) + "] != [AutoMetamagic]"); continue; }
                var am = (AutoMetamagic)comps[0];
                if (am.Metamagic != metamagic) p.Add(at + ": Metamagic " + am.Metamagic);
                if (am.Once != QuickenSpec.Once) p.Add(at + ": Once " + am.Once);
                if (am.MaxSpellLevel != row.MaxSpellLevel || am.MaxSpellLevel <= ChaosRules.Dials.QuickenEngineNoCap) p.Add(at + ": MaxSpellLevel " + am.MaxSpellLevel);
                if (am.CheckSpellbook != QuickenSpec.CheckSpellbook) p.Add(at + ": CheckSpellbook " + am.CheckSpellbook);
                if (!Equals(BuildKit.GetF(am, "m_AllowedAbilities"), allowed)) p.Add(at + ": m_AllowedAbilities " + BuildKit.GetF(am, "m_AllowedAbilities"));
                var bookRef = BuildKit.GetF(am, "m_Spellbook") as BlueprintSpellbookReference;
                if (bookRef == null || bookRef.Guid.ToString() != QuickenSpec.SpellbookGuid || am.Spellbook == null || am.Spellbook.SpellList == null) p.Add(at + ": m_Spellbook " + (bookRef == null ? "null" : bookRef.Guid.ToString()) + " (or it does not resolve to a spellbook with a spell list)");
                // R4-F1: Abilities == 살아 있는 목록에서 다시 계산한 「레벨 ≤ 상한」 GUID(순서까지). 비었거나 null 칸이 있으면 어긋남(빈 목록 = 변형 주문이 상한을 건너뛴다).
                if (am.Abilities == null || am.Abilities.Count == 0 || am.Abilities.Any(x => x == null)) p.Add(at + ": Abilities " + (am.Abilities == null ? "null" : am.Abilities.Count + " entries") + " (spec: the warpriest spells of level <= " + row.MaxSpellLevel + ", none null)");
                else if (bookRef != null && am.Spellbook != null && am.Spellbook.SpellList != null)
                {
                    var want = QuickenSpec.AbilitiesFor(row.MaxSpellLevel, SpellListEntries(am.Spellbook.SpellList));
                    var got = am.Abilities.Select(x => x.Guid.ToString()).ToArray();
                    if (!got.SequenceEqual(want)) p.Add(at + ": Abilities " + got.Length + " entries != the " + want.Length + " warpriest spells of level <= " + row.MaxSpellLevel + " re-read from the live list");
                }
                var inc = BuildKit.GetF(am, "m_IncludeClasses") as BlueprintCharacterClassReference[];
                var exc = BuildKit.GetF(am, "m_ExcludeClasses") as BlueprintCharacterClassReference[];
                if (inc == null || inc.Length != 0) p.Add(at + ": m_IncludeClasses " + (inc == null ? "null" : inc.Length + " entries"));
                if (exc == null || exc.Length != 0) p.Add(at + ": m_ExcludeClasses " + (exc == null ? "null" : exc.Length + " entries"));
                if (am.Descriptor.Value != 0) p.Add(at + ": Descriptor " + am.Descriptor.Value);
                if (am.School != SpellSchool.None) p.Add(at + ": School " + am.School);
            }
            if (p.Count > 0) throw new InvalidOperationException("ChaosWarrior: quicken right buffs read-back failed: " + string.Join(" | ", p.ToArray()));
        }
    }
}
