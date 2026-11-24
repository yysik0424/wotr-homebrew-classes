using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.Actions;
using WotrClassMod.Common;
using static ChaosWarrior.BuildKit;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChaosWarrior
{
     
    // 판단(기준 찾기·이름·중복)은 순수 AliasPlan, 여기는 적용과 되읽기. 하나라도 어긋나면 던진다(BuildFailed).
    public static class RageAlias
    {
        public static int Patched { get; private set; }
        static bool built;
        public static bool Complete { get; private set; }

        public static void Build()
        {
            if (built) throw new InvalidOperationException("ChaosWarrior: RageAlias.Build() called twice");
            built = true;
            if (Rage.Buff == null) throw new InvalidOperationException("ChaosWarrior: RageAlias.Build before Rage.Build");
            string ours = Guids.RageBuff;
            var perConsumer = new Dictionary<string, int>();
            var available = RageAliasTable.Rows.Where(row => row.ConsumerGuid != RageAliasTable.RagingBrutality ||
                ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(row.ConsumerGuid)) != null).ToArray();
            foreach (var row in available)
            {
                var bp = Get<BlueprintScriptableObject>(row.ConsumerGuid);
                if (bp.name != row.Name) throw new InvalidOperationException("ChaosWarrior: rage alias " + row.ConsumerGuid + " is " + bp.name + ", expected " + row.Name);
                var comps = (bp.ComponentsArray ?? new BlueprintComponent[0]).Where(c => c.GetType().Name == row.ComponentType).ToList();
                if (row.Shape == AliasShape.SingleAnchorComponent)
                {
                    int index;
                    perConsumer.TryGetValue(row.ConsumerGuid, out index);
                    // 모형 = 소비자의 모든 컴포넌트(이름, 그 형식이면 필드 GUID / 아니면 null) — 이름 충돌은 전체에서, 기준은 그 형식에서.
                    var all = bp.ComponentsArray.Select(c => Tuple.Create(c.name, comps.Contains(c) ? RefGuidOf(GetF(c, row.Field)) : null)).ToList();
                    var plan = AliasPlan.Single(all, row.AnchorGuid, ours, row.ConsumerGuid, index);
                    var source = bp.ComponentsArray[plan.Item1];
                    var clone = BuildReflect.CloneComponent(source);
                    SetField(clone, row.Field, Ref<BlueprintBuffReference>(ours));
                    VanillaComponentAppend.Append(bp, clone, plan.Item2);
                    perConsumer[row.ConsumerGuid] = index + 1;
                    // 되읽기: 그 이름의 컴포넌트 정확히 1개, 필드 = 우리, 원본 필드 = 기준 그대로.
                    var back = bp.ComponentsArray.Where(c => c.name == plan.Item2).ToArray();
                    if (back.Length != 1 || RefGuidOf(GetF(back[0], row.Field)) != ours || RefGuidOf(GetF(source, row.Field)) != row.AnchorGuid)
                        throw new InvalidOperationException("ChaosWarrior: rage alias read-back failed for " + row);
                }
                else if (row.Shape == AliasShape.ListAnchor)
                {
                    var holders = comps.Where(c => RefGuids(GetF(c, row.Field)).Contains(row.AnchorGuid)).ToList();
                    if (holders.Count != 1) throw new InvalidOperationException("ChaosWarrior: rage alias " + row + " has " + holders.Count + " anchor list(s), expected 1");
                    var oldList = RefGuids(GetF(holders[0], row.Field));
                    var newList = AliasPlan.List(oldList, row.AnchorGuid, ours, row.ConsumerGuid);
                    var arrType = BuildReflect.FieldType(holders[0].GetType(), row.Field);
                    var elemType = arrType.GetElementType();
                    var arr = Array.CreateInstance(elemType, newList.Count);
                    for (int i = 0; i < newList.Count; i++)
                    {
                        var r = (BlueprintReferenceBase)Activator.CreateInstance(elemType);
                        r.ReadGuidFromGuid(BlueprintGuid.Parse(newList[i]));
                        arr.SetValue(r, i);
                    }
                    SetField(holders[0], row.Field, arr);
                    var back = RefGuids(GetF(holders[0], row.Field));
                    if (!back.SequenceEqual(newList)) throw new InvalidOperationException("ChaosWarrior: rage alias list read-back failed for " + row);
                }
                else
                {
                    // 행동 목록: 기준 RemoveBuff 행동을 가진 컴포넌트 정확히 1개 → 그 행동을 복제(m_Buff = 우리, 새 고유 이름)해 목록 끝에.
                    var holders = comps.Where(c => RemoveBuffModel((ActionList)GetF(c, row.Field)).Any(x => x.Item2 == row.AnchorGuid)).ToList();
                    if (holders.Count != 1) throw new InvalidOperationException("ChaosWarrior: rage alias " + row + " has " + holders.Count + " anchor action list(s), expected 1");
                    var al = (ActionList)GetF(holders[0], row.Field);
                    if (row.ConsumerGuid == "4733d8dd549ff544395f1684ec73c392") RepairCrystalMindVisual(al);
                    int index;
                    perConsumer.TryGetValue(row.ConsumerGuid, out index);
                    var plan = AliasPlan.Single(RemoveBuffModel(al), row.AnchorGuid, ours, row.ConsumerGuid, index);
                    var source = al.Actions[plan.Item1];
                    var clone = (GameAction)BuildReflect.CloneObject(source);
                    SetField(clone, "m_Buff", Ref<BlueprintBuffReference>(ours));
                    clone.name = plan.Item2;
                    // 독립 검토 R1-I3: 복제는 RemoveRank 도 옮긴다 — 참이면 분노가 지워질 때 순위형 변이 표식이 순위만 내려가 남는다(AliasPlan.RequireWholeRemoval). 캐스트 실패(RemoveBuff 가 아님)는 던진다.
                    AliasPlan.RequireWholeRemoval(((ContextActionRemoveBuff)clone).RemoveRank, row.ConsumerGuid, clone.name);
                    al.Actions = al.Actions.Concat(new[] { clone }).ToArray();
                    perConsumer[row.ConsumerGuid] = index + 1;
                    var back = RemoveBuffModel(al);
                    if (back.Count(x => x.Item2 == ours) != 1 || back.Count(x => x.Item2 == row.AnchorGuid) != 1 || !al.Actions.Any(a => a.name == plan.Item2))
                        throw new InvalidOperationException("ChaosWarrior: rage alias action read-back failed for " + row);
                }
                Patched++;
            }
            if (Patched != available.Length) throw new InvalidOperationException("ChaosWarrior: rage alias patched " + Patched + " of " + available.Length);
            Complete = true;
        }

        // 행동 목록 모형: (행동 이름, RemoveBuff 이면 그 m_Buff GUID / 아니면 null).
        // internal(2026-11-15): 점검 S10 alias-readback 이 같은 판정을 쓴다(판정은 한 곳 — 가시성만 바꿈, 동작 그대로).
        internal static List<Tuple<string, string>> RemoveBuffModel(ActionList al)
        {
            if (al == null || al.Actions == null) return new List<Tuple<string, string>>();
            return al.Actions.Select(a => Tuple.Create(a == null ? null : a.name, a is ContextActionRemoveBuff ? RefGuidOf(GetF(a, "m_Buff")) : null)).ToList();
        }

        static void RepairCrystalMindVisual(ActionList list)
        {
            const string absentFx = "f3b0a43a302081749940c2cb675195ed";
            int index = Array.FindIndex(list.Actions, a => (a as ContextActionSpawnFx)?.PrefabLink?.AssetId == absentFx);
            if (index < 0 || Kingmaker.BundlesLoading.BundlesLoadService.Instance.HasLocation(absentFx)) return;
            // Remove Fear uses the native abjuration burst on the selected target.
            var donor = Get<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>("55a037e514c0ee14a8e3ed14b47061de");
            var link = donor.GetComponent<Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx>()?.PrefabLink;
            if (string.IsNullOrEmpty(link?.AssetId) || !Kingmaker.BundlesLoading.BundlesLoadService.Instance.HasLocation(link.AssetId))
            {
                ModContext.Entry.Logger.Warning("ChaosWarrior: CrystalMind target FX unavailable in both the original and RemoveFear.");
                return;
            }
            var replacement = (ContextActionSpawnFx)BuildReflect.CloneObject(list.Actions[index]);
            replacement.PrefabLink = link;
            var actions = (GameAction[])list.Actions.Clone();
            actions[index] = replacement;
            list.Actions = actions;
        }

        static string RefGuidOf(object r)
        {
            var b = r as BlueprintReferenceBase;
            return b == null || b.IsEmpty() ? null : b.Guid.ToString();
        }

        static List<string> RefGuids(object arr)
        {
            var a = arr as Array;
            if (a == null) return new List<string>();
            return a.Cast<object>().Select(RefGuidOf).ToList();
        }
    }

    // 본편 블루프린트에 컴포넌트 1개 추가(공용 BlueprintUtil.AddComponents 는 본편을 거부한다 — BlueprintUtil.cs L63~66). 호출처는 RageAlias.Build 하나(시험 `exec: base-blueprint writes`).
    // 공용 함수 L67~78 과 같은 절차에서 소유 확인만 뺐다: null·이름 충돌 → 던짐, 이름 지정, 새 배열 대입, OnEnable 후 OwnerBlueprint 되읽기.
    internal static class VanillaComponentAppend
    {
        internal static void Append(BlueprintScriptableObject bp, BlueprintComponent c, string name)
        {
            if (bp == null) throw new ArgumentNullException("bp");
            if (c == null) throw new ArgumentNullException("c");
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("ChaosWarrior: component name is empty");
            var old = bp.ComponentsArray ?? new BlueprintComponent[0];
            if (old.Any(x => x.name == name)) throw new InvalidOperationException("ChaosWarrior: component name " + name + " already on " + bp.name);
            if (old.Contains(c)) throw new InvalidOperationException("ChaosWarrior: component instance already on " + bp.name);
            c.name = name;
            bp.ComponentsArray = old.Concat(new[] { c }).ToArray();
            bp.OnEnable();
            if (!ReferenceEquals(c.OwnerBlueprint, bp)) throw new InvalidOperationException("ChaosWarrior: OwnerBlueprint not set on " + name);
        }
    }
}
