using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace Warlock
{
     
    // 하나를 맨 뒤에 붙인다. 다른 모드가 붙인 것은 순수 PrestigeRule.Classify 가 알려진 규칙으로만 받아들이고, 모르는 것은 던진다(정찰 §16-T6).
    // 항목 10(2026-11-14): 다른 모드 항목 1개(ExpandedContent StargazerWitch)도 같은 BlockOne 으로 — 있을 때만(ApplyForeignItems).
    // 본편 블루프린트·컴포넌트에 OnEnable 을 부르지 않는다(시험 `wiring: Append never calls OnEnable`).
    public static class PrestigeBlock
    {
        static bool applyCalled;
        static readonly Dictionary<string, PrerequisiteNoArchetype> appended = new Dictionary<string, PrerequisiteNoArchetype>();
        // 6 항목 전부 되읽기까지 끝났을 때만 true(점검 러너 BlueprintsBuilt 가 읽는다).
        public static bool Applied { get; private set; }





        public static void Apply()
        {
            if (applyCalled) throw new InvalidOperationException("Warlock: PrestigeBlock.Apply() called twice");
            applyCalled = true;
            if (Archetype.Bp == null) throw new InvalidOperationException("Warlock: PrestigeBlock.Apply() before Archetype.Build()");
            foreach (var it in PrestigeRule.Items)
            {
                var plan = BlockOne(it, Get<BlueprintFeatureReplaceSpellbook>(it.ItemGuid));
                if (plan.ForeignCount > 0) ModContext.Log(plan.Report());
            }
            if (appended.Count != PrestigeRule.Items.Length) throw new InvalidOperationException("Warlock: prestige block appended to " + appended.Count + " item(s), expected " + PrestigeRule.Items.Length);
            Applied = true;
        }

        // 항목 하나 막기(본편 6 항목과 항목 10 의 ExpandedContent StargazerWitch 가 같은 절차 — C4 한 곳).
        // ① 분류(알려진 다른 모드 것 · 기대열) → 주문서 되읽기 + 우리 것 0개 ② 새 전제 해석 확인 ③ ④ Append. 되돌린 계획을 돌려준다.
        internal static PrestigePlan BlockOne(PrestigeItem it, BlueprintFeatureReplaceSpellbook item)
        {
            if (it == null) throw new ArgumentNullException("it");
            if (item == null) throw new ArgumentNullException("item", "Warlock: prestige item " + it.ItemGuid + " blueprint is null");
            if (Archetype.Bp == null) throw new InvalidOperationException("Warlock: PrestigeBlock.BlockOne() before Archetype.Build()");
            if (item.AssetGuid.ToString() != it.ItemGuid) throw new InvalidOperationException("Warlock: prestige blueprint " + item.AssetGuid + " passed for item " + it.ItemGuid);
            if (appended.ContainsKey(it.ItemGuid)) throw new InvalidOperationException("Warlock: prestige item " + it.ItemGuid + " already blocked (called twice)");
            var witch = Get<BlueprintCharacterClass>(Guids.Game.WitchClass);
            var plan = PrestigeRule.Classify(it, View(item.ComponentsArray), Guids.Archetype);
            var sb = item.Spellbook;
            PrestigeRule.Verify(sb == null ? null : sb.AssetGuid.ToString(), plan.OursCount);
            // ② 전제: Group = All(AND 묶음 — 엔진 MeetsPrerequisites @0081). 기존 PrerequisiteNoArchetype 도 같은 묶음이어야 한다.
            var p = new PrerequisiteNoArchetype();
            SetField(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Guids.Game.WitchClass));
            SetField(p, "m_Archetype", Ref<BlueprintArchetypeReference>(Guids.Archetype));
            p.Group = Prerequisite.GroupType.All;
            // 해석 안 되는 참조는 Contains(null)=false → 전제가 조용히 항상 통과한다. 지금 해석해 확인(Archetype.Build 와 같은 이유).
            if (!ReferenceEquals(p.CharacterClass, witch) || !ReferenceEquals(p.Archetype, Archetype.Bp))
                throw new InvalidOperationException("Warlock: PrerequisiteNoArchetype{WitchClass, Warlock} readback failed for " + it.ItemGuid);
            var otherGroups = item.ComponentsArray.OfType<PrerequisiteNoArchetype>().Where(x => x.Group != p.Group).ToArray();
            if (otherGroups.Length > 0) throw new InvalidOperationException("Warlock: prestige item " + it.ItemGuid + " has PrerequisiteNoArchetype with Group " + otherGroups[0].Group + ", ours is All");
            // ③ ④
            Append(item, p);
            appended[it.ItemGuid] = p;
            return plan;
        }

        // ③ 새 배열 1회 대입(원본 + 우리 하나) · OwnerBlueprint 공개 setter(정찰 §16-T6) · 이름 · ④ 되읽기. 본편 쓰기 허용 목록의 2번(시험 exec: base-blueprint writes).
        internal static void Append(BlueprintFeatureReplaceSpellbook item, PrerequisiteNoArchetype p)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (p == null) throw new ArgumentNullException("p");
            var old = item.ComponentsArray;
            p.name = PrestigeRule.ComponentName(item.AssetGuid.ToString(), old.Length);
            p.OwnerBlueprint = item;
            item.ComponentsArray = PrestigeRule.Appended<BlueprintComponent>(old, p);
            PrestigeRule.VerifyAppended<BlueprintComponent>(old, item.ComponentsArray, p);
            if (!ReferenceEquals(p.OwnerBlueprint, item)) throw new InvalidOperationException("Warlock: prestige block OwnerBlueprint readback failed for " + item.name);
        }

         
        // 블루프린트가 있을 때만(없음 = 그 모드 미설치·기능 꺼짐 = INFO 1줄, 건너뜀). 다른 모드 블루프린트 쓰기 = 쓰기 허용 목록의 이름 붙은 대상 1개(시험 `exec: base-blueprint writes`).
        // L20Sync.Tick 이 모든 BlueprintsCache.Init 포스트픽스가 끝난 첫 프레임에 한 번 부른다(그 모드가 우리 BuildAll 뒤에 만들어도 보인다 — 레벨업 UI 보다 앞).
        // BlueprintsCache.Load 는 모르는 GUID 에 로그 없이 null 을 준다(@001F TryGetValue false → @0109 ldnull, 2026-11-14 dnlib).
        public static ForeignBlockState StargazerState { get; private set; }

        internal static void ApplyForeignItems()
        {
            if (StargazerState != ForeignBlockState.NotRun) throw new InvalidOperationException("Warlock: PrestigeBlock.ApplyForeignItems() called twice (state " + StargazerState + ")");
            StargazerState = ForeignBlockState.Failed; // 아래가 끝까지 가야 Absent/Blocked — 던지면 Failed 로 남는다(S14 가 FAIL 로 읽는다)
            if (!Applied) throw new InvalidOperationException("Warlock: foreign prestige block before the 6 vanilla items were blocked (PrestigeBlock.Applied false)");
            var raw = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(PrestigeRule.EcStargazerWitch));
            if (PrestigeRule.ForeignItemDecide(raw == null ? null : raw.GetType().FullName) == ForeignItemAction.SkipAbsent)
            {
                StargazerState = ForeignBlockState.Absent;
                ModContext.Log(PrestigeRule.ForeignAbsentLine());
                return;
            }
            var plan = BlockOne(PrestigeRule.EcStargazerItem, (BlueprintFeatureReplaceSpellbook)raw);
            StargazerState = ForeignBlockState.Blocked;
            ModContext.Log(PrestigeRule.ForeignBlockedLine(plan));
        }

        // 게임 컴포넌트 → 순수 모습. PrerequisiteNoArchetype 만 참조 GUID 를 싣는다(직렬화된 참조 — 해석 여부와 무관).
        internal static PrestigeComponent[] View(BlueprintComponent[] comps)
        {
            if (comps == null) throw new ArgumentNullException("comps");
            return comps.Select(c =>
            {
                if (c == null) return null;
                var pna = c as PrerequisiteNoArchetype;
                if (pna == null || c.GetType() != typeof(PrerequisiteNoArchetype)) return new PrestigeComponent(c.GetType().FullName, null, null);
                return new PrestigeComponent(c.GetType().FullName, RefGuid(GetF(pna, "m_CharacterClass") as BlueprintReferenceBase), RefGuid(GetF(pna, "m_Archetype") as BlueprintReferenceBase));
            }).ToArray();
        }

        static string RefGuid(BlueprintReferenceBase r) { return r == null || r.IsEmpty() ? null : r.Guid.ToString(); }

        static object GetF(object o, string name)
        {
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("Warlock: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
