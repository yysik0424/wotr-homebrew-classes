using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace OathboundRevenant
{
     
    // 조용한 통과 금지(C1·C2): 원본 없음(Get 이 던짐) · 이름 다름 · 칸 부품 개수가 기대와 다름 · 칸이 비었음 · AssetId 가 표와 다름 · 방식 불일치 — 전부 던진다(해당 Build → BuildFailed).
    // 빌드가 지나가야 할 자리(FxTable.ExpectedUses)를 하나라도 빠뜨리면 VerifyAllUsed 가 던진다(「FX 를 안 붙임」이 조용히 남지 않게).
    // 본편 객체는 읽기만 한다 — 새 PrefabLink · 새 AbilitySpawnFx 를 만든다. 선례 src\BulwarkMage\RebukeFx.cs, PackWarden AuxUnits.cs:254-274(FxHelper 직접 호출).
    public static class OathFx
    {
        static readonly Dictionary<string, string> resolved = new Dictionary<string, string>();
        static readonly HashSet<string> seen = new HashSet<string>();
        static readonly Dictionary<string, PrefabLink> csharpLinks = new Dictionary<string, PrefabLink>();
        static readonly List<string> warnings = new List<string>();   // 연출 실패(뷰 없음·프리팹 로드 실패) — 회복 자체는 막지 않는다. 숫자 정적 필드 금지(dial) → 목록 길이로 센다.

        public static int WarningCount { get { return warnings.Count; } }
        public static string LastWarning { get { return warnings.Count == 0 ? null : warnings[warnings.Count - 1]; } }

        static readonly IReadOnlyDictionary<string, FxSource> available =
            FxTable.Resolve(id => ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(id)) != null);

        static OathFx()
        {
            var own = typeof(Guids).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()).ToList();
            var p = FxTable.Problems(FxTable.ExpectedUses(), own);
            if (p.Count != 0) throw new InvalidOperationException("OathboundRevenant: FxTable invalid: " + string.Join(" | ", p.ToArray()));
        }

        static bool KeepShared(object v) { return v is SimpleBlueprint || v is UnityEngine.Object || v is BlueprintComponent; }

        static AbilitySpawnFx SourceSpawn(FxSource s)
        {
            var a = Get<BlueprintAbility>(s.SourceGuid);
            var fx = (a.ComponentsArray ?? new BlueprintComponent[0]).OfType<AbilitySpawnFx>().ToArray();
            if (fx.Length != 1) throw new InvalidOperationException("OathboundRevenant: FX source " + s.Key + " " + s.SourceGuid + " (" + a.name + ") has " + fx.Length + " AbilitySpawnFx components, FxTable expects exactly 1");
            return fx[0];
        }

        public static string Resolve(FxSource s)
        {
            if (s == null) throw new ArgumentNullException("s");
            string id;
            if (resolved.TryGetValue(s.Key, out id)) return id;
            string got, name;
            if (s.Slot == FxTable.SlotAbilitySpawnFx)
            {
                var a = Get<BlueprintAbility>(s.SourceGuid);
                name = a.name;
                var fx = SourceSpawn(s);
                got = fx.PrefabLink == null ? null : fx.PrefabLink.AssetId;
            }
            else if (s.Slot == FxTable.SlotBuffFxOnStart)
            {
                var b = Get<BlueprintBuff>(s.SourceGuid);
                name = b.name;
                got = b.FxOnStart == null ? null : b.FxOnStart.AssetId;
            }
            else if (s.Slot == FxTable.SlotAreaFx)
            {
                var ae = Get<BlueprintAbilityAreaEffect>(s.SourceGuid);
                name = ae.name;
                got = ae.Fx == null ? null : ae.Fx.AssetId;
            }
            else if (s.Slot == FxTable.SlotNestedSpawnFx)
            {
                var bp = Get<BlueprintScriptableObject>(s.SourceGuid);
                name = bp.name;
                var spawns = new List<ContextActionSpawnFx>();
                foreach (var c in bp.ComponentsArray ?? new BlueprintComponent[0])
                    GraphWalk.Visit(c, v => KeepShared(v) && !ReferenceEquals(v, c), RevenantRules.Dials.ActionCloneDepth, v => { var sp = v as ContextActionSpawnFx; if (sp != null) spawns.Add(sp); });
                var ids = spawns.Select(x => x.PrefabLink == null ? null : x.PrefabLink.AssetId).Distinct().ToList();
                if (spawns.Count == 0 || ids.Count != 1) throw new InvalidOperationException("OathboundRevenant: FX source " + s.Key + " (" + name + ") has " + spawns.Count + " nested ContextActionSpawnFx with " + ids.Count + " distinct ids, FxTable expects >= 1 sharing one id");
                got = ids[0];
            }
            else throw new InvalidOperationException("OathboundRevenant: FX source " + s.Key + " has unknown slot '" + s.Slot + "'");
            if (name != s.SourceName) throw new InvalidOperationException("OathboundRevenant: FX source " + s.Key + " " + s.SourceGuid + " is '" + name + "', FxTable says '" + s.SourceName + "'");
            if (string.IsNullOrEmpty(got)) throw new InvalidOperationException("OathboundRevenant: FX source " + s.Key + " (" + name + ") " + s.Slot + " is empty");
            if (got != s.AssetId) throw new InvalidOperationException("OathboundRevenant: FX source " + s.Key + " (" + name + ") " + s.Slot + " = " + got + ", FxTable says " + s.AssetId);
            resolved.Add(s.Key, got);
            return got;
        }

        // FX 가 반드시 있어야 하는 자리(능력·행동·C#) — NoFx 자리면 던진다(그 자리는 FX 없음으로 분류되면 안 된다).
        static FxSource Required(string useKey, string mode)
        {
            var s = FxTable.SourceFor(useKey, mode);
            if (s == null) throw new InvalidOperationException("OathboundRevenant: FX use " + useKey + " (" + mode + ") is listed as no-FX, but this place always carries FX");
            seen.Add(useKey);
            return s;
        }

        // 능력 AbilitySpawnFx: 본편 원본 컴포넌트의 시점·기준점을 그대로, 새 PrefabLink.
        public static AbilitySpawnFx AbilityFx(string useKey)
        {
            var s = Required(useKey, FxTable.ModeAbilitySpawnFx);
            var src = SourceSpawn(s);
            return Parts.Comp(new AbilitySpawnFx
            {
                PrefabLink = new PrefabLink { AssetId = Resolve(s) },
                Time = src.Time, Anchor = src.Anchor, WeaponTarget = src.WeaponTarget, DestroyOnCast = src.DestroyOnCast, Delay = src.Delay,
                PositionAnchor = src.PositionAnchor, OrientationAnchor = src.OrientationAnchor, OrientationMode = src.OrientationMode,
            });
        }

        // 본편 원본 능력의 시전 동작(Animation)을 우리 능력에 복사(채널 = Omni, 시험 fx: jbp 가 값 고정).
        public static void CopyAnimation(BlueprintAbility a, string useKey)
        {
            if (a == null) throw new ArgumentNullException("a");
            var u = FxTable.UseOf(useKey);
            if (u == null || u.Mode != FxTable.ModeAbilitySpawnFx) throw new InvalidOperationException("OathboundRevenant: CopyAnimation needs an AbilitySpawnFx use, got " + useKey);
            a.Animation = Get<BlueprintAbility>(FxTable.Get(u.SourceKey).SourceGuid).Animation;
        }

        // AreaEffectView.SpawnFxs calls Fx.Load even when the aura has no visual.
        public static PrefabLink AreaFx(string useKey)
        {
            var u = FxTable.UseOf(useKey);
            seen.Add(useKey);
            if (u == null) return new PrefabLink();
            var s = FxTable.SourceFor(useKey, FxTable.ModeAreaFx);
            if (!available.ContainsKey(s.Key)) return new PrefabLink();
            return new PrefabLink { AssetId = Resolve(s) };
        }

        // 행동 목록 안 1회 FX(문맥 대상 위 — ContextActionSpawnFx.RunAction IL_0031~0067 Target.Unit.View).
        public static ContextActionSpawnFx SpawnAction(string useKey)
        {
            var s = Required(useKey, FxTable.ModeSpawnAction);
            return Parts.Act(new ContextActionSpawnFx { PrefabLink = new PrefabLink { AssetId = Resolve(s) } });
        }

        // 되읽기: 그 자리의 행동이 표의 SpawnFx 인가(아니면 던진다).
        public static void VerifySpawn(GameAction a, string useKey)
        {
            var s = FxTable.SourceFor(useKey, FxTable.ModeSpawnAction);
            var sp = a as ContextActionSpawnFx;
            string got = sp == null ? (a == null ? "null" : a.GetType().Name) : sp.PrefabLink == null ? "null link" : sp.PrefabLink.AssetId;
            if (s == null || sp == null || got != s.AssetId) throw new InvalidOperationException("OathboundRevenant: FX use " + useKey + " action is " + got + ", FxTable says " + (s == null ? "no FX" : "ContextActionSpawnFx " + s.AssetId));
        }

        // 능력 ResourceAssetIds 에 자리들의 FX id 를 더한다(본편 관례: ChannelNegativeEnergy · CauseFear 등이 FX id 를 적는다).
        public static void SetResourceIds(BlueprintAbility a, params string[] useKeys)
        {
            if (a == null) throw new ArgumentNullException("a");
            foreach (var k in useKeys ?? new string[0]) { var u = FxTable.UseOf(k); if (u != null) Resolve(FxTable.Get(u.SourceKey)); }
            a.ResourceAssetIds = (a.ResourceAssetIds ?? new string[0]).Concat(FxTable.AssetIds(useKeys)).Distinct().ToArray();
        }

        // C# 부품용 링크를 빌드 때 준비(원본 대조 포함). 실행 때 SpawnOnUnit 이 쓴다.
        public static void PrepareCSharp(string useKey)
        {
            var s = Required(useKey, FxTable.ModeCSharp);
            if (csharpLinks.ContainsKey(useKey)) throw new InvalidOperationException("OathboundRevenant: FX use " + useKey + " prepared twice");
            csharpLinks.Add(useKey, new PrefabLink { AssetId = Resolve(s) });
        }

        // 소유자 위 1회 FX(본편 ContextActionSpawnFx 와 같은 호출: DisableFx 존중 대신 뷰 확인, 우선순위 EventuallyImportant = IL_0066 ldc.i4.1).
        // 준비 안 됨 = 던진다(호출자 Guard 가 계수). 뷰 없음 · 프리팹 로드 실패 · FX 시스템 예외 = 경고 1줄 + WarningCount(연출은 회복을 막지 않는다 — PackWarden 선례).
        public static void SpawnOnUnit(string useKey, UnitEntityData unit)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            PrefabLink link;
            if (!csharpLinks.TryGetValue(useKey, out link)) throw new InvalidOperationException("OathboundRevenant: FX use " + useKey + " was not prepared at build (OathFx.PrepareCSharp)");
            var view = unit.View;
            if (view == null) { Warn(useKey + " fx skipped: " + unit + " has no view"); return; }
            try
            {
                var prefab = link.Load(false, false);
                if (prefab == null) { Warn(useKey + " fx skipped: prefab " + link.AssetId + " did not load"); return; }
                var handle = FxHelper.SpawnFxOnUnit(prefab, view, unit.IsPlayerFaction, null, Vector3.zero, FxPriority.EventuallyImportant);
                if (handle == null) Warn(useKey + " fx skipped: FxHelper.SpawnFxOnUnit returned null for " + unit);
            }
            catch (Exception e) { Warn(useKey + " fx skipped: " + e.GetType().Name + " from the FX system for " + unit + ": " + e.Message); }
        }

        static void Warn(string msg)
        {
            warnings.Add(msg);
            ModContext.Entry.Logger.Warning("OathboundRevenant: " + msg);
        }

        // 빌드 끝(마지막 FX 빌더 DivineBond.Build): 지나가야 할 자리를 모두 지났는가. 빠진 자리 = 던진다.
        public static void VerifyAllUsed()
        {
            var missing = FxTable.ExpectedUses().Where(k => !seen.Contains(k)).ToList();
            if (missing.Count != 0) throw new InvalidOperationException("OathboundRevenant: FX uses never applied by the build: " + string.Join(", ", missing));
        }
    }
}
