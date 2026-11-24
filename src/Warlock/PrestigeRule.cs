using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock
{
    public sealed class PrestigeItem
    {
        public readonly string ItemGuid; // 본편 「위치 주문서」 선택지 항목(BlueprintFeatureReplaceSpellbook)
        public readonly string SelectionGuid; // 그 항목을 담는 프리스티지 주문서 선택지
        public readonly bool ObligatoryExpected; // 선택지 Obligatory(true = 고를 항목 0개면 레벨업 완료 불가 — 정찰 §6)
        public readonly int VanillaComponentCount; // 본편 항목 ComponentsArray 길이(우리 PrerequisiteNoArchetype 추가 전) = VanillaComponents.Length
        public readonly PrestigeComponent[] VanillaComponents; // 본편 컴포넌트 기대열(형식 + 참조, 순서 그대로 — 정찰 §16-T6 jbp)
        public PrestigeItem(string itemGuid, string selectionGuid, bool obligatoryExpected, int vanillaComponentCount, PrestigeComponent[] vanillaComponents)
        {
            if (vanillaComponents == null || vanillaComponents.Length != vanillaComponentCount)
                throw new ArgumentException("PrestigeItem " + itemGuid + ": vanilla component list length " + (vanillaComponents == null ? "null" : vanillaComponents.Length.ToString()) + " != count " + vanillaComponentCount);
            ItemGuid = itemGuid; SelectionGuid = selectionGuid; ObligatoryExpected = obligatoryExpected; VanillaComponentCount = vanillaComponentCount; VanillaComponents = vanillaComponents;
        }
    }

    // 컴포넌트 한 개의 모습(게임 쪽 PrestigeBlock.View 가 만든다). TypeFullName == null 은 null 컴포넌트.
    // ClassGuid·ArchetypeGuid 는 PrerequisiteNoArchetype 일 때만 채운다(참조가 비었으면 null) — 다른 형식은 늘 null.
    public sealed class PrestigeComponent
    {
        public readonly string TypeFullName, ClassGuid, ArchetypeGuid;
        public PrestigeComponent(string typeFullName, string classGuid, string archetypeGuid) { TypeFullName = typeFullName; ClassGuid = classGuid; ArchetypeGuid = archetypeGuid; }
        public bool SameAs(PrestigeComponent o) { return o != null && o.TypeFullName == TypeFullName && o.ClassGuid == ClassGuid && o.ArchetypeGuid == ArchetypeGuid; }
        public override string ToString()
        {
            if (TypeFullName == null) return "null";
            string n = TypeFullName.Substring(TypeFullName.LastIndexOf('.') + 1);
            return ClassGuid == null && ArchetypeGuid == null ? n : n + "{" + (ClassGuid ?? "null") + "," + (ArchetypeGuid ?? "null") + "}";
        }
    }

    // 다른 모드가 본편 항목에 붙이는 것으로 확인된 PrerequisiteNoArchetype(정찰 §16-T6 설치 모드 대조). 처분 = 그대로 둔다(본편 항목은 공유 — 우리는 뒤에 하나 더할 뿐).
    public sealed class PrestigeForeignRule
    {
        public readonly string ClassGuid, ArchetypeGuid, ArchetypeName, Mod, Source, Reason;
        public PrestigeForeignRule(string classGuid, string archetypeGuid, string archetypeName, string mod, string source, string reason)
        {
            foreach (var kv in new[] { Tuple.Create("classGuid", classGuid), Tuple.Create("archetypeGuid", archetypeGuid), Tuple.Create("archetypeName", archetypeName), Tuple.Create("mod", mod), Tuple.Create("source", source), Tuple.Create("reason", reason) })
                if (string.IsNullOrEmpty(kv.Item2)) throw new ArgumentException("PrestigeForeignRule: " + kv.Item1 + " is empty");
            ClassGuid = classGuid; ArchetypeGuid = archetypeGuid; ArchetypeName = archetypeName; Mod = mod; Source = source; Reason = reason;
        }
    }

    public enum PrestigeSlot { Vanilla, KnownForeign, Ours }

    // 항목 10: 다른 모드 항목 처분(순수 판정) · 게임 쪽 상태(NotRun = 첫 프레임 전, Failed = 시도했으나 던짐 — S14 가 FAIL 로 읽는다).
    public enum ForeignItemAction { SkipAbsent, Block }
    public enum ForeignBlockState { NotRun, Absent, Blocked, Failed }

    public sealed class PrestigePlan
    {
        public string ItemGuid;
        public PrestigeComponent[] All;
        public PrestigeSlot[] Slots;
        public PrestigeForeignRule[] ForeignBy; // Slots 와 같은 길이, KnownForeign 자리만 채움
        public int OursCount { get { return Slots.Count(s => s == PrestigeSlot.Ours); } }
        public int VanillaCount { get { return Slots.Count(s => s == PrestigeSlot.Vanilla); } }
        public int ForeignCount { get { return Slots.Count(s => s == PrestigeSlot.KnownForeign); } }
        public string Describe() { return "[" + string.Join(", ", All.Select(c => c.ToString()).ToArray()) + "]"; }
        public string Report()
        {
            var f = new List<string>(); // 반복문(닫힘 람다 없음): 본편 쓰기 스캐너가 this 를 받는 대리자 생성을 쓰기로 보지 않게
            for (int i = 0; i < All.Length; i++) if (Slots[i] == PrestigeSlot.KnownForeign) f.Add("#" + i + " " + ForeignBy[i].ArchetypeName + " from " + ForeignBy[i].Mod);
            return "Warlock prestige item " + ItemGuid + ": vanilla " + VanillaCount + ", ours " + OursCount + ", known foreign " + ForeignCount + (f.Count == 0 ? "" : " [" + string.Join("; ", f.ToArray()) + "]") + " " + Describe();
        }
    }

     
    public static class PrestigeRule
    {
        public const string PrerequisiteClassSpellLevelType = "Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassSpellLevel";
        public const string PrerequisiteNoArchetypeType = "Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoArchetype";
        public const string AddFactsType = "Kingmaker.UnitLogic.FactLogic.AddFacts";

        // 본편 기대열(정찰 §16-T6 표: 5 항목 = 3개, WinterWitchWitch = 뒤에 AddFacts).
        static PrestigeComponent[] Vanilla(bool addFacts)
        {
            var l = new List<PrestigeComponent>
            {
                new PrestigeComponent(PrerequisiteClassSpellLevelType, null, null),
                new PrestigeComponent(PrerequisiteNoArchetypeType, Guids.Game.WitchClass, Guids.Game.AccursedWitchArchetype),
                new PrestigeComponent(PrerequisiteNoArchetypeType, Guids.Game.WitchClass, Guids.Game.LeyLineGuardianWitchArchetype),
            };
            if (addFacts) l.Add(new PrestigeComponent(AddFactsType, null, null));
            return l.ToArray();
        }

        // 출처: 정찰 §6 표(Obligatory) + §16-T1·§16-T6 jbp 되읽기(컴포넌트 3·3·3·3·3·4, 담는 선택지) — 시험 `jbp: prestige items vanilla components`·`jbp: prestige selections obligatory` 가 잠근다.
        public static readonly PrestigeItem[] Items =
        {
            new PrestigeItem(Guids.Game.ArcaneTricksterWitch, Guids.Game.ArcaneTricksterSpellbookSelection, true, 3, Vanilla(false)),
            new PrestigeItem(Guids.Game.EldritchKnightWitch, Guids.Game.EldritchKnightSpellbookSelection, false, 3, Vanilla(false)),
            new PrestigeItem(Guids.Game.HellknightSigniferWitch, Guids.Game.HellknightSigniferSpellbook, false, 3, Vanilla(false)),
            new PrestigeItem(Guids.Game.LoremasterWitch, Guids.Game.LoremasterSpellbookSelection, false, 3, Vanilla(false)),
            new PrestigeItem(Guids.Game.MysticTheurgeWitch, Guids.Game.MysticTheurgeArcaneSpellbookSelection, true, 3, Vanilla(false)),
            new PrestigeItem(Guids.Game.WinterWitchWitch, Guids.Game.WinterWitchSpellbookSelection, false, 4, Vanilla(true)),
        };

        // CharacterOptionsPlus 1.4.0 의 「Winter Witch」 위치 아키타입: CharacterOptionsPlus.dll Archetypes.WinterWitch::CreateSpellbook @0067 등 ldstr "23f2281a-c005-4193-b545-849b669b4ea5"(정찰 §16-T6 1).
        public const string CopWinterWitchArchetype = "23f2281ac0054193b545849b669b4ea5";

        // 알려진 다른 모드 것(6 항목 공통). 새 모드가 다른 것을 붙이면 Classify 가 던진다(조용히 세지 않는다).
        public static readonly PrestigeForeignRule[] ForeignRules =
        {
            new PrestigeForeignRule(Guids.Game.WitchClass, CopWinterWitchArchetype, "CharacterOptionsPlus WinterWitch archetype", "CharacterOptionsPlus 1.4.0",
                "CharacterOptionsPlus.Archetypes.WinterWitch::CreateSpellbook @0062-0233 -> Util.Common::ConfigureArchetypeSpellbookReplacement @003E AddPrerequisiteNoArchetype(archetype, WitchClass); GameLogFull 2026-11-14 4735-4743 WinterWitch.ConfigureEnabled",
                "COP blocks its own Winter Witch archetype from the vanilla witch item (it adds a separate item for that archetype); it does not touch warlocks, so the item stays blocked for us by our own component"),
        };

         
        // 다른 모드 블루프린트 쓰기 = 허용 목록의 이름 붙은 대상 정확히 1개(이 GUID). 블루프린트가 있을 때만 막고, 없으면 INFO 1줄로 건너뛴다.
        // 출처: 정찰 §16-T6 2 + 2026-11-14 dnlib 재확인 — ExpandedContent 0.13.69 StargazerClass b__337: AddComponent PrerequisiteClassSpellLevel{WitchClass, 3}(b__1287) · PrerequisiteNoArchetype{WitchClass, AccursedWitchArchetype c5f6e53e…}(b__1288)
        // · PrerequisiteNoArchetype{WitchClass, LeyLineGuardianWitchArchetype ee92a981…}(b__1289) 순서, m_Spellbook = WitchSpellbook dd04f923… @00A3-00AD(필드 값 = AddStargazerClass @0190·@54EF·@54FF·@571F 의 ldstr GUID)
        // = 본편 5 항목과 같은 기대열 Vanilla(false). 그 모드가 바뀌면 Classify 가 던진다(조용히 세지 않는다).
        // 막지 않으면(핫픽스 1, 2026-11-14 IL): 엔진 SelectFeature.Apply @010A-013A → ApplySpellbook.Apply @0075 DemandSpellbook(WitchSpellbook) 이 워락에게 별도의 새 위치 주문서를 만든다(자기 주문서는 진행 안 됨).
        // 같은 모드의 StargazerWitchAccursed(b__338) 는 PrerequisiteArchetypeLevel{Witch, AccursedWitchArchetype, 1}(b__1291) 이라 워락 해당 없음(막지 않는다).
        public const string EcStargazerWitch = "11c419181f534ad2831342eaf97630d2";
        // 그 항목의 형식: ExpandedContent.dll StargazerClass::AddStargazerClass @5C40 Helpers.CreateBlueprint<BlueprintFeatureReplaceSpellbook>("StargazerWitch", …)(2026-11-14 dnlib).
        public const string FeatureReplaceSpellbookType = "Kingmaker.Blueprints.Classes.BlueprintFeatureReplaceSpellbook";
        // 그 항목의 기대(선택지 GUID 는 읽지 않는다 — null: 담는 선택지·Obligatory 는 그 모드 소유라 S14 의 선택지 분류에 넣지 않는다).
        public static readonly PrestigeItem EcStargazerItem = new PrestigeItem(EcStargazerWitch, null, false, 3, Vanilla(false));

        // [순수] 관측 → 처분. observedType = 그 GUID 로 캐시에서 읽은 블루프린트의 형식 FullName, 캐시에 없으면 null(그 모드 미설치·그 기능 꺼짐 = 정상 상태 → 건너뜀).
        // 다른 형식이면 GUID 충돌이거나 그 모드가 바뀐 것 — 판정 불가라 던진다(있음/없음으로 읽지 않는다).
        public static ForeignItemAction ForeignItemDecide(string observedType)
        {
            if (observedType == null) return ForeignItemAction.SkipAbsent;
            if (observedType == FeatureReplaceSpellbookType) return ForeignItemAction.Block;
            throw new InvalidOperationException("Warlock: blueprint " + EcStargazerWitch + " is " + (observedType.Length == 0 ? "(empty type name)" : observedType) + ", expected " + FeatureReplaceSpellbookType + " (ExpandedContent StargazerWitch) — cannot judge");
        }

        public static string ForeignAbsentLine()
        {
            return "INFO Warlock: ExpandedContent StargazerWitch " + EcStargazerWitch + " is not in the blueprint cache at the first frame after BlueprintsCache.Init (ExpandedContent absent or Stargazer off) — nothing to block, skipped";
        }

        public static string ForeignBlockedLine(PrestigePlan plan)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (plan.ItemGuid != EcStargazerWitch || plan.OursCount != 0) throw new InvalidOperationException("Warlock: ForeignBlockedLine needs the pre-append plan of " + EcStargazerWitch + " (got " + plan.ItemGuid + ", ours " + plan.OursCount + ")");
            return "INFO Warlock: ExpandedContent StargazerWitch " + EcStargazerWitch + " blocked for warlocks — our PrerequisiteNoArchetype{WitchClass, Warlock} appended last (owner ruling 2026-11-14 item 10); before: " + plan.Report();
        }

         
        public static string ComponentName(string itemGuid, int oldLength)
        {
            if (string.IsNullOrEmpty(itemGuid)) throw new ArgumentException("PrestigeRule.ComponentName: empty item GUID");
            if (oldLength < 0) throw new ArgumentOutOfRangeException("oldLength");
            return "$PrerequisiteNoArchetype$" + itemGuid + "#" + oldLength;
        }





        // 분류: 우리 것(우리 아키타입) → 알려진 다른 모드 것 → 나머지(본편). 나머지 == 본편 기대열(순서 포함)이 아니면 던진다.
        // 판정 불가는 통과가 아니다: null 목록·null 컴포넌트 · 참조가 빈 PrerequisiteNoArchetype · 우리 것인데 클래스가 위치가 아님 · 같은 규칙 두 번 → throw.
        public static PrestigePlan Classify(PrestigeItem item, IList<PrestigeComponent> comps, string ourArchetypeGuid)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (comps == null) throw new ArgumentNullException("comps", "PrestigeRule.Classify: " + item.ItemGuid + " components are null");
            if (string.IsNullOrEmpty(ourArchetypeGuid)) throw new ArgumentException("PrestigeRule.Classify: empty archetype GUID");
            var all = comps.ToArray();
            string listing = "[" + string.Join(", ", all.Select(c => c == null ? "null" : c.ToString()).ToArray()) + "]";
            var slots = new PrestigeSlot[all.Length];
            var by = new PrestigeForeignRule[all.Length];
            var used = new List<PrestigeForeignRule>();
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null || c.TypeFullName == null) throw new InvalidOperationException("Warlock: prestige item " + item.ItemGuid + " has a null component at #" + i + " " + listing);
                if (c.TypeFullName == PrerequisiteNoArchetypeType)
                {
                    if (c.ClassGuid == null || c.ArchetypeGuid == null) throw new InvalidOperationException("Warlock: prestige item " + item.ItemGuid + " component #" + i + " PrerequisiteNoArchetype has an empty class/archetype reference (cannot judge) " + listing);
                    if (c.ArchetypeGuid == ourArchetypeGuid)
                    {
                        if (c.ClassGuid != Guids.Game.WitchClass) throw new InvalidOperationException("Warlock: prestige item " + item.ItemGuid + " component #" + i + " names our archetype with class " + c.ClassGuid + ", expected WitchClass " + listing);
                        slots[i] = PrestigeSlot.Ours; continue;
                    }
                    var rule = ForeignRules.Where(r => r.ClassGuid == c.ClassGuid && r.ArchetypeGuid == c.ArchetypeGuid).ToArray();
                    if (rule.Length == 1)
                    {
                        if (used.Contains(rule[0])) throw new InvalidOperationException("Warlock: prestige item " + item.ItemGuid + " has " + rule[0].ArchetypeName + " (" + rule[0].Mod + ") more than once (each known mod adds it once) " + listing);
                        used.Add(rule[0]); slots[i] = PrestigeSlot.KnownForeign; by[i] = rule[0]; continue;
                    }
                }
                slots[i] = PrestigeSlot.Vanilla;
            }
            var kept = Enumerable.Range(0, all.Length).Where(i => slots[i] == PrestigeSlot.Vanilla).Select(i => all[i]).ToArray();
            if (kept.Length != item.VanillaComponents.Length || !kept.Zip(item.VanillaComponents, (k, v) => k.SameAs(v)).All(x => x))
                throw new InvalidOperationException("Warlock: prestige item " + item.ItemGuid + " components " + listing + " minus ours and known foreign = [" + string.Join(", ", kept.Select(k => k.ToString()).ToArray())
                    + "] != vanilla [" + string.Join(", ", item.VanillaComponents.Select(v => v.ToString()).ToArray()) + "] (another mod changed the item — add a known rule after reading its code)");
            return new PrestigePlan { ItemGuid = item.ItemGuid, All = all, Slots = slots, ForeignBy = by };
        }

        // 추가 전 검사: 항목의 주문서가 본편 WitchSpellbook 이 아니면(다른 모드가 바꿨거나 다른 항목) 던진다 · 우리 전제가 이미 있으면(두 번 적용) 던진다.
        public static void Verify(string itemSpellbookGuid, int ourPrereqCount)
        {
            if (itemSpellbookGuid != Guids.Game.WitchSpellbook)
                throw new InvalidOperationException("Warlock: prestige item spellbook is " + (itemSpellbookGuid ?? "null") + ", expected WitchSpellbook " + Guids.Game.WitchSpellbook);
            if (ourPrereqCount != 0)
                throw new InvalidOperationException("Warlock: prestige item already has " + ourPrereqCount + " of our PrerequisiteNoArchetype (applied twice?)");
        }

        // ③ 새 배열 = 원본 원소 그대로 + 맨 뒤 하나(원본 배열은 바꾸지 않는다).
        public static T[] Appended<T>(T[] old, T add) where T : class
        {
            if (old == null) throw new ArgumentNullException("old");
            if (add == null) throw new ArgumentNullException("add");
            if (old.Any(x => ReferenceEquals(x, add))) throw new InvalidOperationException("PrestigeRule.Appended: the component is already in the array");
            var r = new T[old.Length + 1];
            Array.Copy(old, r, old.Length);
            r[old.Length] = add;
            return r;
        }

        // ④ 되읽기: 길이 old+1, 앞 old 원소 ReferenceEquals, 마지막 == add. 아니면 던진다.
        public static void VerifyAppended<T>(T[] old, T[] back, T add) where T : class
        {
            if (old == null || back == null || add == null) throw new ArgumentNullException(old == null ? "old" : back == null ? "back" : "add");
            if (back.Length != old.Length + 1) throw new InvalidOperationException("Warlock: prestige readback length " + back.Length + ", expected " + (old.Length + 1));
            for (int i = 0; i < old.Length; i++)
                if (!ReferenceEquals(back[i], old[i])) throw new InvalidOperationException("Warlock: prestige readback element #" + i + " is not the original component");
            if (!ReferenceEquals(back[old.Length], add)) throw new InvalidOperationException("Warlock: prestige readback last element is not our PrerequisiteNoArchetype");
        }
    }
}
