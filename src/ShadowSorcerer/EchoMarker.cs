using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.UnitLogic.Mechanics;
using WotrClassMod.Common;

namespace ShadowSorcerer
{
     
    // EchoRegistry 의 약한 표는 메아리 규칙과 최상위 컨텍스트에만 붙고 저장되지 않는다. 피해·버프 규칙의 Reason.Context 는 흔히 자식 컨텍스트(버프·장판)이고
    // 불러오면 약한 표가 사라지므로, 세기 판정은 엔진이 그림자 계수와 같은 경로로 복사(MechanicsContext..ctor IL_0205~0246, 항목마다 Clone)·저장([JsonProperty])하는
    // 불신 목록 m_ShadowDisbelieveData 안의 표식 항목 하나로 한다. 판독·모양 판단은 순수 EchoMarkRule.
    public static class EchoMarker
    {
        // 기반 형식까지 올라가는 필드 조회(BlueprintUtil.FindField). 이름 리터럴은 ReflectionLookups 원장에 있다.
        static FieldInfo GetField(Type t, string name) { return BlueprintUtil.FindField(t, name); }

        static readonly FieldInfo listField = FindListField();

        static FieldInfo FindListField()
        {
            var mc = typeof(MechanicsContext);
            var f = GetField(mc, "m_ShadowDisbelieveData");
            if (f == null) throw new MissingFieldException("ShadowSorcerer: MechanicsContext.m_ShadowDisbelieveData not found");
            if (f.FieldType != typeof(List<ShadowDisbelieveData>)) throw new InvalidOperationException("ShadowSorcerer: MechanicsContext.m_ShadowDisbelieveData is " + f.FieldType + ", expected List<ShadowDisbelieveData>");
            return f;
        }

        // 3상태 판독. 컨텍스트가 없으면 던진다(없는 것을 「메아리 아님」으로 읽지 않는다 — 호출자가 먼저 거른다).
        public static EchoMark Read(MechanicsContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException("ctx", "ShadowSorcerer: echo mark read without a context");
            var list = (List<ShadowDisbelieveData>)listField.GetValue(ctx);
            var mark = EchoMark.NotEcho;
            if (list == null) return mark;
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                if (d == null) throw new InvalidOperationException("ShadowSorcerer: null entry in the shadow disbelief list");
                if (string.IsNullOrEmpty(d.Unit.Id))
                    mark = EchoMarkRule.ClassifyEmptyTarget(mark, EchoMarkRule.State(d.IsDisbelieved, d.IsChanceChecked, d.IsAffectedByChance));
            }
            return mark;
        }

        // 그 대상의 불신 항목(H9·H10 용): 없으면 null(아직 굴리지 않음), 있으면 IsDisbelieved. 조회는 엔진과 같은 연산(EntityRef == 유닛 — TryAffectByShadow 조회 람다 IL_0001~000C).
        // 표식 항목(빈 대상)은 null 이 아닌 유닛과 같지 않으므로 걸리지 않는다. 유닛이 없으면 던진다(호출자가 먼저 거른다).
        public static bool? DisbeliefOf(MechanicsContext ctx, Kingmaker.EntitySystem.Entities.UnitEntityData unit)
        {
            if (ctx == null) throw new ArgumentNullException("ctx", "ShadowSorcerer: disbelief read without a context");
            if (unit == null) throw new ArgumentNullException("unit", "ShadowSorcerer: disbelief read without a unit");
            var list = (List<ShadowDisbelieveData>)listField.GetValue(ctx);
            if (list == null) return null;
            foreach (var d in list)
            {
                if (d == null) throw new InvalidOperationException("ShadowSorcerer: null entry in the shadow disbelief list");
                if (d.Unit == unit) return d.IsDisbelieved;
            }
            return null;
        }

        // 메아리 컨텍스트에 표식 항목을 넣는다(EchoQueue.ApplyShadowFactor 한 곳에서만 부른다). 이미 표식이 있거나 판정 불가면 던진다. 넣은 뒤 되읽는다.
        public static void Mark(MechanicsContext ctx)
        {
            var before = Read(ctx);
            if (before != EchoMark.NotEcho) throw new InvalidOperationException("ShadowSorcerer: echo mark on a context that already reads " + before);
            var list = (List<ShadowDisbelieveData>)listField.GetValue(ctx);
            if (list == null)
            {
                list = new List<ShadowDisbelieveData>();
                listField.SetValue(ctx, list);
            }
            list.Add(new ShadowDisbelieveData { IsChanceChecked = true }); // 대상 없음(빈 EntityRef) + ChanceChecked 단독 = EchoMarkRule.SentinelState
            var after = Read(ctx);
            if (after != EchoMark.Echo) throw new InvalidOperationException("ShadowSorcerer: echo mark readback " + after + ", expected Echo");
        }
    }
}
