using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using static WotrClassMod.Common.BlueprintUtil;

namespace ChimeraMonk
{
     
    // 조용한 통과 금지(C1·C2): 원본 없음(Get 이 던짐) · 이름 다름 · AbilitySpawnFx ≠ 1개 · PrefabLink 빈칸 · id/Time/Anchor/Animation/AnimationStyle 이 표와 다름 — 전부 던진다(Mutagen.Build → BuildFailed).
    // 본편 객체는 읽기만 한다 — 새 AbilitySpawnFx·PrefabLink 를 만들고 값 형식 칸만 옮긴다(본편 인스턴스를 공유하지 않는다).
    public static class MutagenFx
    {
        static BlueprintAbility source;
        static AbilitySpawnFx sourceFx;

        static void Resolve()
        {
            if (source != null) return;
            var a = Get<BlueprintAbility>(FxTable.SourceGuid);
            if (a.name != FxTable.SourceName) throw new InvalidOperationException("ChimeraMonk: FX source " + FxTable.SourceGuid + " is '" + a.name + "', FxTable says '" + FxTable.SourceName + "'");
            var fx = (a.ComponentsArray ?? new Kingmaker.Blueprints.BlueprintComponent[0]).OfType<AbilitySpawnFx>().ToArray();
            if (fx.Length != 1) throw new InvalidOperationException("ChimeraMonk: FX source " + a.name + " has " + fx.Length + " AbilitySpawnFx components, FxTable expects exactly 1");
            string got = fx[0].PrefabLink == null ? null : fx[0].PrefabLink.AssetId;
            if (string.IsNullOrEmpty(got)) throw new InvalidOperationException("ChimeraMonk: FX source " + a.name + " " + FxTable.Slot + " is empty");
            var p = Mismatches(got, fx[0].Time.ToString(), fx[0].Anchor.ToString(), a.Animation.ToString(), a.AnimationStyle.ToString());
            if (p.Count != 0) throw new InvalidOperationException("ChimeraMonk: FX source " + a.name + ": " + string.Join(" | ", p.ToArray()) + " (FxTable says otherwise)");
            source = a; sourceFx = fx[0];
        }

        // 표와 다른 칸 목록(빈 목록 = 일치). 원본 대조와 우리 능력 되읽기가 같은 함수를 쓴다(C4).
        static List<string> Mismatches(string id, string time, string anchor, string animation, string style)
        {
            var d = new List<string>();
            if (id != FxTable.AssetId) d.Add(FxTable.Slot + " = " + (id ?? "null") + " != " + FxTable.AssetId);
            if (time != FxTable.SpawnTime) d.Add("AbilitySpawnFx.Time = " + time + " != " + FxTable.SpawnTime);
            if (anchor != FxTable.SpawnAnchor) d.Add("AbilitySpawnFx.Anchor = " + anchor + " != " + FxTable.SpawnAnchor);
            if (animation != FxTable.Animation) d.Add("Animation = " + animation + " != " + FxTable.Animation);
            if (style != FxTable.AnimationStyle) d.Add("AnimationStyle = " + style + " != " + FxTable.AnimationStyle);
            return d;
        }

        static void RequireTarget(BlueprintAbility a)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (!FxTable.Targets.Contains(a.AssetGuid.ToString())) throw new InvalidOperationException("ChimeraMonk: " + a.name + " is not an FxTable target");
        }

        // 능력 만들 때(Fab.Ability 의 more 안): 동작 = 원본 동작(물약 마시기), 미리 읽기 = [FX id].
        public static void ApplyAnimation(BlueprintAbility a)
        {
            RequireTarget(a);
            Resolve();
            a.Animation = source.Animation;
            a.AnimationStyle = source.AnimationStyle;
            a.HasFastAnimation = source.HasFastAnimation;
            a.ResourceAssetIds = new[] { FxTable.AssetId };
        }

        // 능력 컴포넌트: 원본 AbilitySpawnFx 의 새 사본(PrefabLink 새로, 값 형식 칸 복사).
        public static AbilitySpawnFx SpawnComponent()
        {
            Resolve();
            return Fab.Comp(new AbilitySpawnFx
            {
                PrefabLink = new PrefabLink { AssetId = FxTable.AssetId },
                Time = sourceFx.Time, Anchor = sourceFx.Anchor, WeaponTarget = sourceFx.WeaponTarget, DestroyOnCast = sourceFx.DestroyOnCast, Delay = sourceFx.Delay,
                PositionAnchor = sourceFx.PositionAnchor, OrientationAnchor = sourceFx.OrientationAnchor, OrientationMode = sourceFx.OrientationMode,
            });
        }

        // 되읽기(Readback.MutagenDiff): 표 무결성 + 대상 집합 == 만든 변이약 능력 + 능력마다 AbilitySpawnFx 정확히 1개·값 == 표·미리 읽기 == [id].
        public static List<string> Diff(IList<BlueprintAbility> built)
        {
            var d = new List<string>();
            d.AddRange(FxTable.Problems(OwnBlueprints.Select(b => b.AssetGuid.ToString())).Select(x => "mutagen fx table: " + x));
            var ids = (built ?? new BlueprintAbility[0]).Select(a => a.AssetGuid.ToString()).ToList();
            if (ids.Count == 0 || !new HashSet<string>(ids).SetEquals(FxTable.Targets)) d.Add("mutagen fx: built abilities [" + string.Join(",", ids) + "] != FxTable.Targets");
            foreach (var a in built ?? new BlueprintAbility[0])
            {
                var fx = a.ComponentsArray.OfType<AbilitySpawnFx>().ToArray();
                if (fx.Length != 1) { d.Add(a.name + ": " + fx.Length + " AbilitySpawnFx (want 1)"); continue; }
                d.AddRange(Mismatches(fx[0].PrefabLink == null ? null : fx[0].PrefabLink.AssetId, fx[0].Time.ToString(), fx[0].Anchor.ToString(), a.Animation.ToString(), a.AnimationStyle.ToString()).Select(x => a.name + ": " + x));
                if (a.ResourceAssetIds == null || !a.ResourceAssetIds.SequenceEqual(new[] { FxTable.AssetId })) d.Add(a.name + ": ResourceAssetIds [" + (a.ResourceAssetIds == null ? "null" : string.Join(",", a.ResourceAssetIds)) + "] != [" + FxTable.AssetId + "]");
            }
            return d;
        }
    }
}
