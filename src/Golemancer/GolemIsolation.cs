using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 깊은 복사 격리 단언(게임 쪽 수집기 — 판정은 순수 CloneGraph, 선례 src\WeaponSpiritShaman\Isolation.cs). 계획 T3 「GolemUnit.Build(): … + GolemIsolation.VerifyAfterBuild()」.
    //   원본 그래프 = CR6_GolemWood(블루프린트·컴포넌트·Body·Visual·프리팹·이름) + ConstructClass + ConstructProgression + 도발 선례 3(영역·영역 버프·효과 버프).
    //   우리 그래프 = 펫 직업·펫 진행표·골렘 유닛·도발 4. 허용 공유 = CloneGraph.AllowedSharedKinds(unit.visual — 읽기만).
    //   능력 복제본(T4)은 대상 아님 — T4 AbilityClone.VerifyShare 별도 정책.
    // 순서: PetClass.Build 맨 앞에서 TakeSnapshot → (복사·생성) → GolemUnit.Build 끝에서 VerifyAfterBuild. 점검 S15 가 Check()/SnapshotDiffNow() 로 다시 부른다.
    public static class GolemIsolation
    {
        static bool taken;
        static readonly List<KeyValuePair<string, object>> before = new List<KeyValuePair<string, object>>();

        public static CloneCheck LastCheck { get; private set; }

        public static void TakeSnapshot()
        {
            if (taken) throw new InvalidOperationException("Golemancer: GolemIsolation.TakeSnapshot called twice");
            taken = true;
            before.AddRange(SourceSnapshot());
        }

        // Build 끝: 참조 분리(CloneGraph.Verify) + 본편 값 되읽기(SnapshotDiff 0). 어느 하나라도 어긋나면 던진다 → BuildFailed(등록 안 됨).
        public static void VerifyAfterBuild()
        {
            if (!taken) throw new InvalidOperationException("Golemancer: GolemIsolation.VerifyAfterBuild without snapshot");
            LastCheck = CloneGraph.Verify(Original(), Ours());
            SnapshotGuard.ThrowIfChanged(CloneGraph.SnapshotDiff(before, SourceSnapshot()), "Build");
        }





        // 점검 S15 vanilla-untouched: Build 전 스냅샷 대비 지금(제외 키 없음 — 이 원본들을 Build 뒤 바꾸는 설치 모드는 알려진 바 없다. 바뀌면 FAIL 로 드러난다).
        public static string[] SnapshotDiffNow()
        {
            if (!taken) throw new InvalidOperationException("Golemancer: GolemIsolation snapshot not taken");
            return CloneGraph.SnapshotDiff(before, SourceSnapshot());
        }

        // ---------------- 스냅샷 ----------------
        static List<KeyValuePair<string, object>> SourceSnapshot()
        {
            var l = new List<KeyValuePair<string, object>>();
            var unit = Get<BlueprintUnit>(Guids.Game.GolemWood);
            var cls = Get<BlueprintCharacterClass>(Guids.Game.ConstructClass);
            var prog = Get<BlueprintProgression>(Guids.Game.ConstructProgression);
            l.Add(KV("unit.components", unit.ComponentsArray));
            for (int i = 0; i < unit.ComponentsArray.Length; i++) l.Add(KV("unit.component." + i, unit.ComponentsArray[i]));
            var acl = unit.GetComponent<AddClassLevels>();
            if (acl == null) throw new InvalidOperationException("Golemancer: CR6_GolemWood has no AddClassLevels");
            l.Add(KV("unit.acl.class", GuidOf(GetF(acl, "m_CharacterClass"))));
            l.Add(KV("unit.acl.levels", acl.Levels));
            l.Add(KV("unit.addfacts", GetF(unit, "m_AddFacts")));
            l.Add(KV("unit.addfacts.text", string.Join(",", GolemUnit.FactGuids(unit))));
            l.Add(KV("unit.body", unit.Body));
            l.Add(KV("unit.body.limbs", GetF(unit.Body, "m_AdditionalLimbs")));
            l.Add(KV("unit.body.limbs.count", GolemUnit.LimbCount(unit.Body)));
            l.Add(KV("unit.stats", unit.Strength + "/" + unit.Dexterity + "/" + unit.Constitution + "/" + unit.Intelligence + "/" + unit.Wisdom + "/" + unit.Charisma));
            l.Add(KV("unit.size", unit.Size.ToString()));
            l.Add(KV("unit.faction", GuidOf(GetF(unit, "m_Faction"))));
            l.Add(KV("unit.brain", GuidOf(GetF(unit, "m_Brain"))));
            l.Add(KV("unit.visual", unit.Visual));
            l.Add(KV("unit.prefab", unit.Prefab));
            l.Add(KV("unit.name", unit.LocalizedName));
            l.Add(KV("class.components", cls.ComponentsArray));
            l.Add(KV("class.progression", GuidOf(GetF(cls, "m_Progression"))));
            l.Add(KV("class.archetypes", GetF(cls, "m_Archetypes")));
            l.Add(KV("prog.classes", GetF(prog, "m_Classes")));
            l.Add(KV("prog.entries", prog.LevelEntries));
            l.Add(KV("prog.entries.text", EntriesText(prog)));
            foreach (var g in new[] { Guids.Game.TauntPrecedentArea, Guids.Game.TauntPrecedentAreaBuff, Guids.Game.TauntPrecedentBuff })
            {
                var bp = Get<BlueprintScriptableObject>(g);
                l.Add(KV("taunt." + g + ".components", bp.ComponentsArray));
                for (int i = 0; i < bp.ComponentsArray.Length; i++) l.Add(KV("taunt." + g + ".component." + i, bp.ComponentsArray[i]));
            }
            var area = Get<BlueprintAbilityAreaEffect>(Guids.Game.TauntPrecedentArea);
            l.Add(KV("taunt.area.size", area.Size.Value));
            l.Add(KV("taunt.area.fx", area.Fx));
            // T4: 특기 능력 복제 원본 4(AbilityClone 얕은 복제 — 공유 컴포넌트·DC 직업이 Build 뒤에도 그대로인지) + 슬램 원형 Slam2d6(값 복사 원본).
            foreach (var g in MaterialCatalog.Materials.SelectMany(m => m.SpecialAbilities).Where(a => a.Kind == AbilityKind.CloneRepointDc).Select(a => a.Guid).Distinct())
            {
                var bp = Get<BlueprintScriptableObject>(g);
                l.Add(KV("special." + g + ".components", bp.ComponentsArray));
                for (int i = 0; i < bp.ComponentsArray.Length; i++)
                {
                    var c = bp.ComponentsArray[i];
                    l.Add(KV("special." + g + ".component." + i, c));
                    l.Add(KV("special." + g + ".component." + i + ".owner", c.OwnerBlueprint));
                    var dc = c as Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass;
                    if (dc != null) l.Add(KV("special." + g + ".dc.class", GuidOf(GetF(dc, "m_CharacterClass"))));
                }
            }
            var slam = Get<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(Guids.Game.Slam2d6);
            l.Add(KV("slam2d6.visual", slam.VisualParameters));
            l.Add(KV("slam2d6.dice", slam.BaseDamage.ToString() + "/" + slam.IsDamageDiceOverridden));
            l.Add(KV("slam2d6.components", slam.ComponentsArray));
            return l;
        }

        static KeyValuePair<string, object> KV(string k, object v) { return new KeyValuePair<string, object>(k, v); }

        static string GuidOf(object reference)
        {
            var r = reference as BlueprintReferenceBase;
            if (reference == null) return "null";
            if (r == null) throw new InvalidOperationException("Golemancer: expected a blueprint reference, got " + reference.GetType().Name);
            return r.Guid.ToString();
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
            AddUnit(l, Get<BlueprintUnit>(Guids.Game.GolemWood), "base:CR6_GolemWood");
            AddBlueprint(l, Get<BlueprintCharacterClass>(Guids.Game.ConstructClass), "base:ConstructClass");
            AddProgression(l, Get<BlueprintProgression>(Guids.Game.ConstructProgression), "base:ConstructProgression");
            AddBlueprint(l, Get<BlueprintScriptableObject>(Guids.Game.TauntPrecedentArea), "base:TauntPrecedentArea");
            AddBlueprint(l, Get<BlueprintScriptableObject>(Guids.Game.TauntPrecedentAreaBuff), "base:TauntPrecedentAreaBuff");
            AddBlueprint(l, Get<BlueprintScriptableObject>(Guids.Game.TauntPrecedentBuff), "base:TauntPrecedentBuff");
            return l;
        }

        static List<GraphNode> Ours()
        {
            if (PetClass.Class == null || PetClass.Progression == null) throw new InvalidOperationException("Golemancer: GolemIsolation: PetClass not built");
            if (GolemUnit.Unit == null) throw new InvalidOperationException("Golemancer: GolemIsolation: GolemUnit not built");
            if (Taunt.Area == null || Taunt.AreaBuff == null || Taunt.EffectBuff == null || Taunt.MarkerBuff == null) throw new InvalidOperationException("Golemancer: GolemIsolation: Taunt not built");
            var l = new List<GraphNode>();
            AddBlueprint(l, PetClass.Class, "ours:PetClass");
            AddProgression(l, PetClass.Progression, "ours:PetProgression");
            AddUnit(l, GolemUnit.Unit, "ours:GolemUnit");
            AddBlueprint(l, Taunt.Area, "ours:TauntArea");
            AddBlueprint(l, Taunt.AreaBuff, "ours:TauntAreaBuff");
            AddBlueprint(l, Taunt.EffectBuff, "ours:TauntBuff");
            AddBlueprint(l, Taunt.MarkerBuff, "ours:TauntMarkerBuff");
            return l;
        }

        static void AddBlueprint(List<GraphNode> l, BlueprintScriptableObject bp, string owner)
        {
            if (bp == null) throw new InvalidOperationException("Golemancer: GolemIsolation: null blueprint for " + owner);
            Add(l, bp, "Blueprint", owner, true);
            var comps = bp.ComponentsArray;
            if (comps == null) throw new InvalidOperationException("Golemancer: GolemIsolation: " + owner + " ComponentsArray is null");
            Add(l, comps, "ComponentArray", owner, false);
            foreach (var c in comps)
            {
                if (c == null) throw new InvalidOperationException("Golemancer: GolemIsolation: null component on " + owner);
                Add(l, c, "Component:" + c.GetType().Name, owner, true);
            }
        }

        static void AddProgression(List<GraphNode> l, BlueprintProgression p, string owner)
        {
            AddBlueprint(l, p, owner);
            if (p.LevelEntries == null) throw new InvalidOperationException("Golemancer: GolemIsolation: " + owner + " LevelEntries is null");
            foreach (var e in p.LevelEntries) Add(l, e, "LevelEntry", owner, true);
            var cs = (BlueprintProgression.ClassWithLevel[])GetF(p, "m_Classes");
            if (cs == null) throw new InvalidOperationException("Golemancer: GolemIsolation: " + owner + " m_Classes is null");
            foreach (var c in cs) Add(l, c, "ClassWithLevel", owner, false);
        }

        static void AddUnit(List<GraphNode> l, BlueprintUnit u, string owner)
        {
            AddBlueprint(l, u, owner);
            if (u.Body == null) throw new InvalidOperationException("Golemancer: GolemIsolation: " + owner + " Body is null");
            Add(l, u.Body, "unit.body", owner, true);
            if (u.Visual == null) throw new InvalidOperationException("Golemancer: GolemIsolation: " + owner + " Visual is null");
            Add(l, u.Visual, "unit.visual", owner, false); // 허용 공유(읽기만) — 내려가지 않는다
            if (u.Prefab == null) throw new InvalidOperationException("Golemancer: GolemIsolation: " + owner + " Prefab is null");
            Add(l, u.Prefab, "unit.prefab", owner, false);
            if (u.LocalizedName != null) Add(l, u.LocalizedName, "unit.name", owner, false);
        }

        static void Add(List<GraphNode> l, object o, string kind, string owner, bool descend)
        {
            l.Add(new GraphNode(o, kind, owner));
            if (descend) l.AddRange(CloneGraph.ContainerNodes(o, kind, owner));
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("Golemancer: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
