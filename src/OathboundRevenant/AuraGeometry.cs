using System;

namespace OathboundRevenant
{
    // [순수] 오라 영역 기하(계획 Global Constraints 「오라 크기」 #8, 부록 A-f). 엔진 판정 = ScriptZoneCylinder.Contains(point, r) 「SqrDistance2D(중심, 점) ≤ (Radius + r)²」,
    // r = 대상 유닛 체구(Corpulence). 설계 반경 R 은 가장자리–가장자리 ⇒ 중심 거리 ≤ R + 시전자 체구 + 대상 체구 ⇔ Size = R + 시전자 체구.
    // 입력이 NaN·음수면 판정 불가 → 던진다(C1). Medium 체구 값은 RevenantRules.Dials 에 두고 T6 Step 0 판독으로 확정한다.
    public static class AuraGeometry
    {
        static void RequireFinite(float v, string name)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || v < 0) throw new ArgumentOutOfRangeException(name, v, "OathboundRevenant: aura geometry input " + name + " must be a finite non-negative number (NaN = not yet determined)");
        }

        // 엔진 Contains 식: 중심 거리 ≤ Size + 대상 체구.
        public static bool Inside(float centerDistFt, float sizeFt, float targetCorpulenceFt)
        {
            RequireFinite(centerDistFt, "centerDistFt"); RequireFinite(sizeFt, "sizeFt"); RequireFinite(targetCorpulenceFt, "targetCorpulenceFt");
            return centerDistFt <= sizeFt + targetCorpulenceFt;
        }

        // 설계 가장자리 반경 → 영역 Size.
        public static float SizeFor(int edgeRadiusFt, float casterCorpulenceFt)
        {
            if (edgeRadiusFt < 0) throw new ArgumentOutOfRangeException("edgeRadiusFt", edgeRadiusFt, "OathboundRevenant: edge radius must be >= 0");
            RequireFinite(casterCorpulenceFt, "casterCorpulenceFt");
            return edgeRadiusFt + casterCorpulenceFt;
        }

        // 가장자리 거리 → 중심 거리.
        public static float CenterDistance(float edgeFt, float casterCorpFt, float targetCorpFt)
        {
            RequireFinite(edgeFt, "edgeFt"); RequireFinite(casterCorpFt, "casterCorpFt"); RequireFinite(targetCorpFt, "targetCorpFt");
            return edgeFt + casterCorpFt + targetCorpFt;
        }
    }
}
