using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using WotrClassMod.Common;
using static WotrClassMod.Common.BlueprintUtil;

namespace ShadowSorcerer
{
    // 시각 전용 그림자 유닛의 블루프린트(소유자 결정 2026-11-16 밤 1: 본편 그림자 생물 Shadow 형상, 귀검사 령과 같은 방식).
     
    // 두 단계:
    //   BuildShell()  — 껍데기 특성(우리 블루프린트, 실패하면 빌드 실패: GUID 충돌 같은 프로그램 오류뿐).
    //   TryBuildUnit() — 본편 Shadow 사본. 본편 모양이 알려진 배치가 아니면(다른 모드가 바꿈) 만들지 않고 오류 줄 1개 — 메아리 자체는 연출 없이 그대로 동작한다.
    public static class ShadowUnitBlueprint
    {
        public static BlueprintFeature Shell;
        public static BlueprintUnit Unit;   // null = 그림자 연출 불가(사유는 빌드 때 오류 줄로 남는다)

        const string UnitName = "ShadowSorcererDoubleUnit";

        // 껍데기 특성(귀검사 정찰 §2(a)(b)): 클릭·대상 불가(Untargetable) · AI 정지·기회 공격 없음(MarkPassive) · 전투 참여 판정 제외(IsIgnoredByCombat)
        //   + 격리 감시선(ShadowTripwireComponent — 이 유닛이 내성 굴림·피해 규칙의 당사자가 되면 한 번 보고, 읽기만 한다).
        // 주의: 대상 불가는 「지정」만 막는다. 원뿔·선·연쇄의 대상 수집은 이 깃발을 보지 않는다 — 그 격리는 숨긴 동안 유닛을 게임 밖에 두는 것(ShadowDoubles 머리 주석)이 한다.
        public static void BuildShell()
        {
            if (Shell != null) throw new InvalidOperationException("ShadowSorcerer: ShadowUnitBlueprint.BuildShell() called twice");
            Shell = Create<BlueprintFeature>("ShadowSorcererDoubleShellFeature", Guids.ShadowDoubleShell, f =>
            {
                f.HideInUI = true;
                f.Ranks = 1;
            });
            // 숨김 특성이라 어디에도 나오지 않지만 LocalizedString 은 null 로 두지 않는다(null 이면 화면에 「<null>」 — 빈 키 인스턴스).
            SetField(Shell, "m_DisplayName", new LocalizedString());
            SetField(Shell, "m_Description", new LocalizedString());
            SetField(Shell, "m_DescriptionShort", new LocalizedString());
            var ignored = new AddMechanicsFeature();
            SetField(ignored, "m_Feature", AddMechanicsFeature.MechanicsFeatureType.IsIgnoredByCombat);
            if (ignored.Feature != AddMechanicsFeature.MechanicsFeatureType.IsIgnoredByCombat) throw new InvalidOperationException("ShadowSorcerer: AddMechanicsFeature.m_Feature readback failed (" + ignored.Feature + ")");
            AddComponents(Shell, new Untargetable(), new MarkPassive(), ignored, new ShadowTripwireComponent());
            var comps = Shell.ComponentsArray;
            if (comps == null || comps.Length != 4 || !(comps[0] is Untargetable) || !(comps[1] is MarkPassive) || !ReferenceEquals(comps[2], ignored) || !(comps[3] is ShadowTripwireComponent) || !Shell.HideInUI)
                throw new InvalidOperationException("ShadowSorcerer: shadow shell feature readback failed");
        }

        // 던지지 않는다. 실패 = 오류 줄 + Unit == null(그림자 연출 없음).
        public static void TryBuildUnit()
        {
            try
            {
                if (Unit != null) throw new InvalidOperationException("ShadowUnitBlueprint.TryBuildUnit() called twice");
                if (Shell == null) throw new InvalidOperationException("ShadowUnitBlueprint.TryBuildUnit() before BuildShell()");
                Unit = BuildClone();
            }
            catch (Exception e)
            {
                Unit = null;
                ShadowErrors.Report(EchoVisuals.Site, "shadow unit blueprint not built (" + e.GetType().Name + ": " + e.Message + ") - echoes keep working without the shadow visual");
            }
        }

        static BlueprintUnit BuildClone()
        {
            var src = Get<BlueprintUnit>(Guids.Game.ShadowUnit);
            var shadowFx = Get<BlueprintBuff>(ShadowDoublePlan.ShadowFxBuffGuid);
            var player = Get<BlueprintFaction>(Guids.Game.PlayerFaction);

            // ---- 원본 모양 가드: 본편 배치만 받는다(알려진 모드 배치 없음 — 2026-11-17 설치본 Log.txt 의 Patched/Added 줄에 이 유닛·ShadowFX·사실 5개 없음). 그 밖 = 실패. ----
            if (src.Size.ToString() != ShadowDoublePlan.SourceSizeName) throw new InvalidOperationException("source " + src.name + " Size is " + src.Size + ", expected " + ShadowDoublePlan.SourceSizeName);
            if (src.Prefab == null || src.Prefab.AssetId != ShadowDoublePlan.SourcePrefabAssetId) throw new InvalidOperationException("source " + src.name + " prefab is " + (src.Prefab == null ? "null" : src.Prefab.AssetId) + ", expected " + ShadowDoublePlan.SourcePrefabAssetId + " (another mod changed the model)");
            var srcComps = src.ComponentsArray;
            if (srcComps == null) throw new InvalidOperationException("source " + src.name + " has null ComponentsArray");
            var srcCompsCopy = srcComps.ToArray();
            var srcFacts = (BlueprintUnitFactReference[])GetF(src, "m_AddFacts");
            if (srcFacts == null) throw new InvalidOperationException("source " + src.name + " has null m_AddFacts");
            var srcFactsCopy = srcFacts.ToArray();
            if (!src.AddFacts.Any(f => f != null && f.AssetGuid == shadowFx.AssetGuid)) throw new InvalidOperationException("source " + src.name + " no longer carries ShadowFX (the dark look comes from that buff)");
            if (shadowFx.FxOnStart == null || string.IsNullOrEmpty(shadowFx.FxOnStart.AssetId)) throw new InvalidOperationException("ShadowFX has no FxOnStart prefab");
            var srcFactionRef = GetF(src, "m_Faction");
            var srcBrainRef = GetF(src, "m_Brain");
            var srcBody = src.Body;
            if (srcBody == null) throw new InvalidOperationException("source " + src.name + " has no Body");
            var bodyFields = BodyFields();
            var srcBodyValues = bodyFields.Select(f => f.GetValue(srcBody)).ToArray();
            var srcStart = GetF(src, "m_StartingInventory");

            var keep = new List<BlueprintComponent>();
            foreach (var c in srcComps)
            {
                if (c == null) throw new InvalidOperationException("source " + src.name + " has a null component");
                var plan = ShadowDoublePlan.Component(c.GetType().FullName);
                if (plan == ShadowDoublePlan.ComponentPlan.Keep) keep.Add(c);
                else if (plan != ShadowDoublePlan.ComponentPlan.Drop)
                    throw new InvalidOperationException("source " + src.name + " has unclassified component " + c.GetType().FullName + " (" + c.name + ") - not the vanilla layout; classify it in ShadowDoublePlan.Component after checking what it does");
            }

            // ---- 사본: 바꾸는 것은 ComponentsArray · m_AddFacts · m_Faction · m_Brain · Body · m_StartingInventory 뿐. 원본은 그대로여야 한다(아래 단언). ----
            var clone = CloneUnit(src, UnitName, Guids.Visual.ShadowDoubleUnit);
            clone.ComponentsArray = keep.ToArray();
            SetField(clone, "m_AddFacts", new[] { Ref<BlueprintUnitFactReference>(Guids.ShadowDoubleShell), Ref<BlueprintUnitFactReference>(ShadowDoublePlan.ShadowFxBuffGuid) });
            SetField(clone, "m_Faction", Ref<BlueprintFactionReference>(Guids.Game.PlayerFaction));
            SetField(clone, "m_Brain", null); // 두뇌 없음: UnitBrain 이 null 기본 두뇌를 받으면 행동 목록이 빈 채로 끝난다(귀검사 정찰 — SetBrain IL 0013~001A)
            var body = (BlueprintUnit.UnitBody)MemberwiseCloneMethod.Invoke(srcBody, null);
            foreach (var f in bodyFields)
            {
                if (ShadowDoublePlan.Body(f.Name) != ShadowDoublePlan.BodyField.ClearItem) continue; // Keep = 원본 값 그대로(정당)
                if (f.FieldType.IsArray)
                {
                    var old = (Array)f.GetValue(body);
                    bool limbs = f.Name == "m_AdditionalLimbs" || f.Name == "m_AdditionalSecondaryLimbs";
                    f.SetValue(body, Array.CreateInstance(f.FieldType.GetElementType(), limbs || old == null ? 0 : old.Length));
                }
                else f.SetValue(body, null);
            }
            clone.Body = body;
            SetField(clone, "m_StartingInventory", new BlueprintItemReference[0]);

            // ---- 되읽기 ----
            var facts = clone.AddFacts.ToArray();
            if (facts.Length != 2 || !ReferenceEquals(facts[0], Shell) || !ReferenceEquals(facts[1], shadowFx)) throw new InvalidOperationException(UnitName + " facts readback is [" + string.Join(",", facts.Select(f => f == null ? "null" : f.name)) + "], expected [shell, ShadowFX]");
            var cc = clone.ComponentsArray;
            if (ReferenceEquals(cc, srcComps) || cc.Length != keep.Count || cc.Any(c => ShadowDoublePlan.Component(c.GetType().FullName) != ShadowDoublePlan.ComponentPlan.Keep)) throw new InvalidOperationException(UnitName + " components readback failed");
            if (clone.Faction != player) throw new InvalidOperationException(UnitName + " faction readback is " + (clone.Faction == null ? "null" : clone.Faction.name) + ", expected Player");
            if (clone.DefaultBrain != null) throw new InvalidOperationException(UnitName + " brain readback is " + clone.DefaultBrain.name + ", expected none");
            if (ReferenceEquals(clone.Body, srcBody) || clone.Body == null) throw new InvalidOperationException(UnitName + " Body is the source Body or null");
            foreach (var f in bodyFields)
            {
                var v = f.GetValue(clone.Body);
                bool clear = ShadowDoublePlan.Body(f.Name) == ShadowDoublePlan.BodyField.ClearItem;
                bool hasItem = v is Array a ? a.Cast<object>().Any(x => x != null) : v != null;
                if (clear && hasItem) throw new InvalidOperationException(UnitName + " Body." + f.Name + " still references an item after the strip");
                if (!clear && !Equals(v, f.GetValue(srcBody))) throw new InvalidOperationException(UnitName + " Body." + f.Name + " (kept field) differs from the source");
            }
            var cloneStart = GetF(clone, "m_StartingInventory") as BlueprintItemReference[];
            if (cloneStart == null || cloneStart.Length != 0 || ReferenceEquals(cloneStart, srcStart)) throw new InvalidOperationException(UnitName + " m_StartingInventory readback is not a fresh empty array");
            if (clone.AssetGuid.ToString() != Guids.Visual.ShadowDoubleUnit || clone.name != UnitName) throw new InvalidOperationException(UnitName + " identity readback failed");
            if (clone.Prefab == null || clone.Prefab.AssetId != ShadowDoublePlan.SourcePrefabAssetId) throw new InvalidOperationException(UnitName + " prefab readback failed");
            // 2026-11-17: 문구 칸 null 금지(사본은 본편 Shadow 의 인스턴스를 그대로 가리킨다 — 본편 jbp 는 세 칸 모두 빈 키, 시험 tooltip: 이 대조). null 이면 사본을 만들지 않는다(위 TryBuildUnit 오류 줄).
            RequireLocalizedStrings(clone);

            // ---- 원본 불변 ----
            if (!ReferenceEquals(src.ComponentsArray, srcComps) || !srcComps.SequenceEqual(srcCompsCopy)) throw new InvalidOperationException("source " + src.name + " ComponentsArray was modified");
            var srcFactsAfter = (BlueprintUnitFactReference[])GetF(src, "m_AddFacts");
            if (!ReferenceEquals(srcFactsAfter, srcFacts) || !srcFacts.SequenceEqual(srcFactsCopy)) throw new InvalidOperationException("source " + src.name + " m_AddFacts was modified");
            if (!ReferenceEquals(GetF(src, "m_Faction"), srcFactionRef) || !ReferenceEquals(GetF(src, "m_Brain"), srcBrainRef)) throw new InvalidOperationException("source " + src.name + " faction/brain was modified");
            if (!ReferenceEquals(src.Body, srcBody) || bodyFields.Where((f, i) => !ReferenceEquals(f.GetValue(srcBody), srcBodyValues[i]) && !Equals(f.GetValue(srcBody), srcBodyValues[i])).Any()) throw new InvalidOperationException("source " + src.name + " Body was modified");
            if (!ReferenceEquals(GetF(src, "m_StartingInventory"), srcStart)) throw new InvalidOperationException("source " + src.name + " m_StartingInventory was modified");
            return clone;
        }

        static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        // BlueprintUnit.UnitBody 의 인스턴스 필드 전부. 모든 이름은 ShadowDoublePlan.Body 가 분류해야 한다(모르는 이름이면 그 함수가 던진다).
        static FieldInfo[] BodyFields()
        {
            if (MemberwiseCloneMethod == null) throw new MissingMethodException("object.MemberwiseClone not found");
            var fs = typeof(BlueprintUnit.UnitBody).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fs.Length == 0) throw new InvalidOperationException("BlueprintUnit.UnitBody has no instance fields (cannot strip items)");
            foreach (var f in fs) ShadowDoublePlan.Body(f.Name);
            if (!fs.Any(f => ShadowDoublePlan.Body(f.Name) == ShadowDoublePlan.BodyField.ClearItem)) throw new InvalidOperationException("BlueprintUnit.UnitBody has no item field to clear (field names changed?)");
            return fs;
        }

        static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException(o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
