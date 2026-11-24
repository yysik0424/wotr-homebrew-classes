using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using static WotrClassMod.Common.BlueprintUtil;

namespace WeaponSpiritShaman
{
    // 깊은 복사 격리 단언(게임 쪽 수집기 — 판정은 순수 CloneGraph). 계획 T3: 「Build 직후 단언: 우리 블루프린트의 모든 컴포넌트·LevelEntry·ClassWithLevel·UnitBody·
    // WeaponVisualParameters 인스턴스가 본편 원본 객체 그래프의 어떤 인스턴스와도 참조가 같지 않음 + 본편 원본 값 되읽기가 Build 전 스냅샷과 같음」.
    //   원본 그래프 = 복사 원본 5종(AnimalCompanionClass·AnimalCompanionBaseProgression·AnimalCompanionUnit·WildlandShaman 랭크 진행표·WildlandShaman 동료 선택지)
    //               + 기준 무기 59 + 두 번째 머리 4 (SpiritBlueprints 가 행마다 AddItem 으로 알린다).
    //   노드 = 블루프린트 · 컴포넌트 배열 · 컴포넌트 · (각 노드의 배열/List 필드 값) · LevelEntry · ClassWithLevel · 유닛 Body · Visual(허용 공유, 내려가지 않음) ·
    //          프리팹 링크 · 이름 자산 · 무기 시각 객체.
    // 순서: PetClass.Build 맨 앞에서 TakeSnapshot → (복사) → SpiritBlueprints.Build 끝에서 VerifyAfterBuild. 점검 S22 가 Check()/ItemSnapshotDiff() 로 다시 부른다.
    public static class Isolation
    {
        static bool taken;
        static readonly List<Func<List<KeyValuePair<string, object>>>> readers = new List<Func<List<KeyValuePair<string, object>>>>();
        static readonly List<KeyValuePair<string, object>> before = new List<KeyValuePair<string, object>>();
        static readonly List<BlueprintItemWeapon> baseItems = new List<BlueprintItemWeapon>();

        public static CloneCheck LastCheck { get; private set; }

        // 복사 원본 5종의 값·참조를 Build 전에 읽어 둔다.
        public static void TakeSnapshot()
        {
            if (taken) throw new InvalidOperationException("WeaponSpiritShaman: Isolation.TakeSnapshot called twice");
            taken = true;
            AddReader(SourceSnapshot);
        }

        // 기준 무기(또는 두 번째 머리)를 복사하기 직전에 부른다: 원본 그래프에 넣고 값·참조를 읽어 둔다.
        public static void AddItem(BlueprintItemWeapon baseItem)
        {
            if (!taken) throw new InvalidOperationException("WeaponSpiritShaman: Isolation.AddItem before TakeSnapshot");
            if (baseItem == null) throw new ArgumentNullException("baseItem");
            if (baseItems.Any(b => ReferenceEquals(b, baseItem))) throw new InvalidOperationException("WeaponSpiritShaman: Isolation.AddItem twice for " + baseItem.name);
            baseItems.Add(baseItem);
            AddReader(() => ItemSnapshot(baseItem));
        }

        static void AddReader(Func<List<KeyValuePair<string, object>>> r)
        {
            readers.Add(r);
            before.AddRange(r());
        }

        // Build 끝: 참조 분리(CloneGraph.Verify) + 본편 값 되읽기(SnapshotDiff 0). 어느 하나라도 어긋나면 던진다 → BuildFailed(등록 안 됨).
        public static void VerifyAfterBuild()
        {
            if (!taken || baseItems.Count == 0) throw new InvalidOperationException("WeaponSpiritShaman: Isolation.VerifyAfterBuild without snapshot/base items");
            LastCheck = CloneGraph.Verify(Original(), Ours());
            var diff = CloneGraph.SnapshotDiff(before, After());
            if (diff.Length > 0) throw new InvalidOperationException("WeaponSpiritShaman: base-game values changed during Build: " + string.Join(" | ", diff.Take(10)));
        }





        // Build 뒤에 다른 모드가 합법적으로 바꾸는 본편 값 — 점검 S22(지금 vs Build 전)에서만 뺀다(Build 끝의 VerifyAfterBuild 는 모두 본다).
        //   TabletopTweaks-Base Patch_AnimalCompanion_Classes.UpdateProgressions(BlueprintsCache.Init 포스트픽스, HarmonyPriority(0) = 우리 공용 포스트픽스 뒤)가
        //   AnimalCompanionClass(26b10d43)의 진행표 LevelEntries 를 제자리에서 바꾼다: <UpdateProgressions>g__UpdateLevelEntry|1_1 @0017 stfld LevelEntry::Level,
        //   <UpdateProgressions>b__3 @000C List.Remove(AnimalCompanionStatFeature)(리뷰 T3 I1, TTT-Base dll 직독). 그래서 S22 는 원본 진행표를
        //   m_Classes 참조(prog.classes)와 제거 3개의 존재(vanilla-untouched)로만 본다.
        public static readonly string[] KeysOtherModsMayChangeAfterBuild = { "prog.entries", "prog.entries.text" };

        // 점검 S22 weapon-visual-disjoint: Build 전 스냅샷 대비 지금(위 목록 제외 — 기준 아이템의 VisualParameters.Prototype 참조 포함).
        public static string[] SnapshotDiffNow()
        {
            Func<KeyValuePair<string, object>, bool> keep = kv => !KeysOtherModsMayChangeAfterBuild.Contains(kv.Key);
            return CloneGraph.SnapshotDiff(before.Where(keep).ToList(), After().Where(keep).ToList());
        }

















        static List<KeyValuePair<string, object>> After() { return readers.SelectMany(r => r()).ToList(); }

        // ---------------- 스냅샷 ----------------
        static List<KeyValuePair<string, object>> SourceSnapshot()
        {
            var l = new List<KeyValuePair<string, object>>();
            var unit = Get<BlueprintUnit>(Guids.Game.AnimalCompanionUnit);
            var cls = Get<BlueprintCharacterClass>(Guids.Game.AnimalCompanionClass);
            var prog = Get<BlueprintProgression>(Guids.Game.AnimalCompanionBaseProgression);
            var rank = Get<BlueprintProgression>(Guids.Game.WildlandShamanAnimalCompanionProgression);
            var sel = Get<BlueprintScriptableObject>(Guids.Game.AnimalCompanionSelectionWildlandShaman);
            var acl = unit.GetComponent<AddClassLevels>();
            if (acl == null) throw new InvalidOperationException("WeaponSpiritShaman: AnimalCompanionUnit has no AddClassLevels");
            l.Add(KV("unit.components", unit.ComponentsArray));
            foreach (var c in unit.ComponentsArray) l.Add(KV("unit.component." + c.name, c));
            l.Add(KV("unit.acl.class", GuidOf(GetF(acl, "m_CharacterClass"))));
            l.Add(KV("unit.acl.levels", acl.Levels));
            l.Add(KV("unit.body", unit.Body));
            l.Add(KV("unit.body.primary", GuidOf(GetF(unit.Body, "m_PrimaryHand"))));
            l.Add(KV("unit.stats", unit.Strength + "/" + unit.Dexterity + "/" + unit.Constitution + "/" + unit.Intelligence + "/" + unit.Wisdom + "/" + unit.Charisma));
            l.Add(KV("unit.addfacts", GetF(unit, "m_AddFacts")));
            l.Add(KV("unit.visual", unit.Visual));
            l.Add(KV("unit.prefab", unit.Prefab));
            l.Add(KV("unit.name", unit.LocalizedName));
            l.Add(KV("class.components", cls.ComponentsArray));
            l.Add(KV("class.progression", GuidOf(GetF(cls, "m_Progression"))));
            l.Add(KV("class.archetypes", GetF(cls, "m_Archetypes")));
            l.Add(KV("prog.classes", GetF(prog, "m_Classes")));
            l.Add(KV("prog.classes.text", ClassesText(prog)));
            l.Add(KV("prog.entries", prog.LevelEntries));
            l.Add(KV("prog.entries.text", EntriesText(prog)));
            l.Add(KV("rank.classes", GetF(rank, "m_Classes")));
            l.Add(KV("rank.classes.text", ClassesText(rank)));
            l.Add(KV("rank.entries", rank.LevelEntries));
            l.Add(KV("rank.entries.text", EntriesText(rank)));
            l.Add(KV("selection.components", sel.ComponentsArray));
            return l;
        }

        static List<KeyValuePair<string, object>> ItemSnapshot(BlueprintItemWeapon b)
        {
            string k = "item." + b.AssetGuid + ".";
            var vp = b.VisualParameters;
            return new List<KeyValuePair<string, object>>
            {
                KV(k + "components", b.ComponentsArray),
                KV(k + "enchantments", GetF(b, "m_Enchantments")),
                KV(k + "cost", GetF(b, "m_Cost")),
                KV(k + "second", GuidOf(GetF(b, "m_SecondWeapon"))),
                KV(k + "visual", vp),
                KV(k + "visual.prototype", vp == null ? null : vp.Prototype),
            };
        }

        static KeyValuePair<string, object> KV(string k, object v) { return new KeyValuePair<string, object>(k, v); }

        static string GuidOf(object reference)
        {
            var r = reference as BlueprintReferenceBase;
            if (reference == null) return "null";
            if (r == null) throw new InvalidOperationException("WeaponSpiritShaman: expected a blueprint reference, got " + reference.GetType().Name);
            return r.Guid.ToString();
        }

        static string ClassesText(BlueprintProgression p)
        {
            var cs = (BlueprintProgression.ClassWithLevel[])GetF(p, "m_Classes");
            if (cs == null) return "null";
            return string.Join(",", cs.Select(c => GuidOf(c.m_Class) + "+" + c.AdditionalLevel).ToArray());
        }

        static string EntriesText(BlueprintProgression p)
        {
            if (p.LevelEntries == null) return "null";
            return string.Join(" ", p.LevelEntries.Select(e => e.Level + ":" + string.Join(",", ((IEnumerable)GetF(e, "m_Features")).Cast<BlueprintReferenceBase>().Select(GuidOf).ToArray())).ToArray());
        }

        // ---------------- 그래프 수집 ----------------
        static List<GraphNode> Original()
        {
            var l = new List<GraphNode>();
            AddBlueprint(l, Get<BlueprintCharacterClass>(Guids.Game.AnimalCompanionClass), "base:AnimalCompanionClass");
            AddProgression(l, Get<BlueprintProgression>(Guids.Game.AnimalCompanionBaseProgression), "base:AnimalCompanionBaseProgression");
            AddUnit(l, Get<BlueprintUnit>(Guids.Game.AnimalCompanionUnit), "base:AnimalCompanionUnit");
            AddProgression(l, Get<BlueprintProgression>(Guids.Game.WildlandShamanAnimalCompanionProgression), "base:WildlandShamanAnimalCompanionProgression");
            AddBlueprint(l, Get<BlueprintScriptableObject>(Guids.Game.AnimalCompanionSelectionWildlandShaman), "base:AnimalCompanionSelectionWildlandShaman");
            foreach (var b in baseItems) AddWeapon(l, b, "base:" + b.name);
            return l;
        }

        static List<GraphNode> Ours()
        {
            if (PetClass.Class == null || PetClass.Progression == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: PetClass not built");
            if (SpiritBlueprints.RankProgression == null || SpiritBlueprints.Selection == null || SpiritBlueprints.Entries.Count == 0) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: SpiritBlueprints not built");
            var l = new List<GraphNode>();
            AddBlueprint(l, PetClass.Class, "ours:PetClass");
            AddProgression(l, PetClass.Progression, "ours:PetProgression");
            AddProgression(l, SpiritBlueprints.RankProgression, "ours:RankProgression");
            AddBlueprint(l, SpiritBlueprints.Selection, "ours:Selection");
            foreach (var e in SpiritBlueprints.Entries)
            {
                AddUnit(l, e.Unit, "ours:" + e.Unit.name);
                AddWeapon(l, e.Weapon, "ours:" + e.Weapon.name);
                if (e.Second != null) AddWeapon(l, e.Second, "ours:" + e.Second.name);
            }
            return l;
        }

        static void AddBlueprint(List<GraphNode> l, BlueprintScriptableObject bp, string owner)
        {
            if (bp == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: null blueprint for " + owner);
            Add(l, bp, "Blueprint", owner, true);
            var comps = bp.ComponentsArray;
            if (comps == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: " + owner + " ComponentsArray is null");
            Add(l, comps, "ComponentArray", owner, false);
            foreach (var c in comps)
            {
                if (c == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: null component on " + owner);
                Add(l, c, "Component:" + c.GetType().Name, owner, true);
            }
        }

        static void AddProgression(List<GraphNode> l, BlueprintProgression p, string owner)
        {
            AddBlueprint(l, p, owner);
            if (p.LevelEntries == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: " + owner + " LevelEntries is null");
            foreach (var e in p.LevelEntries) Add(l, e, "LevelEntry", owner, true);
            var cs = (BlueprintProgression.ClassWithLevel[])GetF(p, "m_Classes");
            if (cs == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: " + owner + " m_Classes is null");
            foreach (var c in cs) Add(l, c, "ClassWithLevel", owner, false);
        }

        static void AddUnit(List<GraphNode> l, BlueprintUnit u, string owner)
        {
            AddBlueprint(l, u, owner);
            if (u.Body == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: " + owner + " Body is null");
            Add(l, u.Body, "unit.body", owner, true);
            if (u.Visual == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: " + owner + " Visual is null");
            Add(l, u.Visual, "unit.visual", owner, false); // 허용 공유(읽기만) — 내려가지 않는다
            if (u.Prefab == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: " + owner + " Prefab is null");
            Add(l, u.Prefab, "unit.prefab", owner, false);
            if (u.LocalizedName != null) Add(l, u.LocalizedName, "unit.name", owner, false);
        }

        static void AddWeapon(List<GraphNode> l, BlueprintItemWeapon w, string owner)
        {
            AddBlueprint(l, w, owner);
            if (w.VisualParameters == null) throw new InvalidOperationException("WeaponSpiritShaman: Isolation: " + owner + " VisualParameters is null");
            Add(l, w.VisualParameters, "weapon.visual", owner, true);
        }

        // 노드 하나 + (descend 면) 그 객체의 배열/List 인스턴스 필드 값마다 노드(형식.필드 종류).
        static void Add(List<GraphNode> l, object o, string kind, string owner, bool descend)
        {
            l.Add(new GraphNode(o, kind, owner));
            if (descend) l.AddRange(CloneGraph.ContainerNodes(o, kind, owner));
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("WeaponSpiritShaman: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
