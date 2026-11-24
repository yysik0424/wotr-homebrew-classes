using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.View;
using Kingmaker.Visual.MaterialEffects;
using Kingmaker.Visual.MaterialEffects.Dissolve;
using UnityEngine;
using WotrClassMod.Common;

namespace PhantomBlade
{
    // 령 외형 실패 장부(게임 쪽 출구). 순수 계수부 = SpiritRules.cs 의 CountedReporter. 오류 줄은 ModContext.LogError → S11 포착이 센다.
    public static class SpiritVisualErrors
    {
        public const string SiteGhostDissolve = "spirit-ghost-dissolve";
        public const string SiteWalkCopy = "spirit-walk-copy";
        public const string SiteLowerBody = "spirit-lower-body";       // 2026-11-16 하체 흐림
        public const string SiteNoHighlight = "spirit-no-highlight";   // 2026-11-16 Tab 외곽선 제외
        public const string SiteDash = "spirit-ghost-slash-dash";      // 2026-11-16 귀신참 돌진(상한 초과·판독 실패)
        public const string SiteDashFx = "ghost-slash-dash-fx";        // 2026-11-16 밤 귀신참 잔상·타격 효과(원본 대조 실패·프리팹 없음·자동 소멸 없음·예외)
        public const string SiteProjectileVeil = "ghost-slash-projectile-veil"; // 2026-11-16 귀신참 투사체 숨김(판정 불가·렌더러 없음·예외)
        public const string SiteProjectileVeilLeftover = "ghost-slash-veil-leftover"; // 2026-11-16 가드: 풀에서 다시 쓰이는 View 에 꺼진 렌더러·가림 컴포넌트가 남아 있었다
        static readonly CountedReporter ledger = new CountedReporter(ModContext.LogError);



        public static int SuppressedRepeats { get { return ledger.SuppressedRepeats; } }
        public static bool ReportOnce(string where, string key, string what) { return ledger.ReportOnce(where, key, what); }
    }

     
    // 본편 디졸브 장치를 그대로 쓴다: DissolveSettings 하나를 령 뷰의 StandardMaterialController 디졸브 목록에 넣으면 엔진이 매 프레임 굴린다
    //   (선례 BloodyFaceController.Init IL 0080~017C, EntityFader.OnEnabled IL 005F~011F). 값은 전부 SpiritRules.Dials(한 곳, 출처 주석 그곳).
    // 질감 = BlueprintRoot.Prefabs.FogOfWarDissolveSettings.Texture — 본편 페이더가 모든 유닛 페이드에 쓰는 바로 그 질감(EntityFader.OnEnabled IL 005F~008E).
    // SpiritFollower.LateUpdate 가 매 프레임 Tick 을 부른다: 처음엔 붙이고, 그 뒤엔 목록에 그대로 있는지 본다(엔진이 빼는 경로 = 끝난 것뿐 — Update IL 0032~0080, 반복이라 안 끝남).
    // 실패는 시끄럽게: 무엇이 없는지 SpiritVisualErrors.ReportOnce(오류 줄 1·Count +1) 후 이 령에서는 더 시도하지 않는다 — 령은 불투명하게 남는다(눈에도 드러남).
    //   따라가기는 계속된다(외형 실패가 위치·숨김을 멈추지 않게 따로 잡는다).
    public sealed class SpiritGhostLook
    {
        // 디졸브 컨트롤러는 internal 게터(get_DissolveController, Assembly) 뒤의 private 필드다(dnlib 2026-11-14). GetField 직접 호출 = Exact.
        static readonly FieldInfo DissolveControllerField = typeof(StandardMaterialController).GetField("m_DissolveController", BindingFlags.Instance | BindingFlags.NonPublic);

        readonly UnitEntityView view;
        readonly string who;
        DissolveAnimationController controller;
        DissolveSettings settings;
        bool failed;

        public SpiritGhostLook(UnitEntityView spiritView, string spiritName)
        {
            if (spiritView == null) throw new ArgumentNullException("spiritView");
            view = spiritView;
            who = string.IsNullOrEmpty(spiritName) ? "?" : spiritName;
        }











        public void Tick()
        {
            TickNoHighlight(); // 디졸브와 무관하게 늘 돈다(실패도 따로 센다)
            if (failed) return; // 이미 보고함(한 번)
            if (!SpiritRules.GhostDissolveEnabled(SpiritRules.Dials.SpiritGhostDissolve)) return; // 다이얼 Off = 정당한 무동작
            try
            {
                if (settings == null) Attach();
                else Verify();
            }
            catch (Exception e)
            {
                failed = true;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteGhostDissolve, who + "#" + view.GetInstanceID(), "spirit " + who + " ghost dissolve failed, spirit stays opaque: " + e.Message);
                return;
            }
            TickLowerBody();
        }

         
        // 엔진 디졸브는 컨트롤러의 모든 재질에 같은 값을 쓴다(DissolveAnimationController.ApplyAnimationToMaterial IL 00B4~00D0 SetFloat(_Dissolve/_DissolveWidth),
        //   StandardMaterialController.DoUpdate — Update 단계). 우리는 LateUpdate(SpiritFollower)에서 하체 판 렌더러(Dials.LowerBodyRendererPrefix)의 재질에만 더 센 값을 덮어쓴다.
        // 실패(렌더러·재질·속성 없음)는 한 번 보고하고 그 령에서 더 시도하지 않는다 — 하체가 상체와 같은 흐림으로 남는다(눈에 드러남).
        static readonly PropertyInfo MaterialsProperty = typeof(StandardMaterialController).GetProperty("Materials", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Material[] lowerMats;
        bool lowerFailed;
        public int LowerBodyMaterialCount { get { return lowerMats == null ? 0 : lowerMats.Length; } }
        public bool LowerBodyFailed { get { return lowerFailed; } }

        void TickLowerBody()
        {
            if (lowerFailed) return;
            try
            {
                if (lowerMats == null) lowerMats = FindLowerBodyMaterials();
                float level = (float)SpiritRules.GhostMaterialDissolve(SpiritRules.Dials.LowerBodyDissolveLevel);
                float width = (float)SpiritRules.GhostMaterialDissolveWidth(SpiritRules.Dials.LowerBodyDissolveLevel, SpiritRules.Dials.GhostDissolveWidthScale);
                foreach (var m in lowerMats)
                {
                    if (m == null) throw new InvalidOperationException("a lower-body material was destroyed");
                    m.SetFloat(StandardMaterialController._Dissolve, level);
                    m.SetFloat(StandardMaterialController._DissolveWidth, width);
                }
            }
            catch (Exception e)
            {
                lowerFailed = true;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteLowerBody, who + "#" + view.GetInstanceID(), "spirit " + who + " lower-body fade failed, lower body keeps the upper-body dissolve: " + e.Message);
            }
        }

        Material[] FindLowerBodyMaterials()
        {
            var smc = view.GetComponent<StandardMaterialController>();
            if (smc == null) throw new InvalidOperationException("spirit view has no StandardMaterialController");
            // get_Materials 는 internal(dnlib 2026-11-16) — 정확한 이름 하나로 조회, 없으면 던진다.
            if (MaterialsProperty == null) throw new MissingMemberException("StandardMaterialController.Materials not found");
            var controlled = MaterialsProperty.GetValue(smc, null) as System.Collections.Generic.List<Material>;
            if (controlled == null || controlled.Count == 0) throw new InvalidOperationException("StandardMaterialController.Materials is empty");
            var prefix = SpiritRules.Dials.LowerBodyRendererPrefix;
            var list = new System.Collections.Generic.List<Material>();
            int renderers = 0;
            foreach (var r in view.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || !r.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                renderers++;
                foreach (var m in r.sharedMaterials)
                    if (m != null && controlled.Contains(m) && !list.Contains(m)) list.Add(m);
            }
            if (renderers == 0) throw new InvalidOperationException("no renderer named '" + prefix + "*' on the spirit model");
            if (list.Count == 0) throw new InvalidOperationException(renderers + " '" + prefix + "*' renderer(s) found, but none of their materials is driven by the dissolve controller; " + SafeMismatchDiagnosis(smc, controlled, prefix));
            foreach (var m in list)
                if (!m.HasProperty(StandardMaterialController._Dissolve) || !m.HasProperty(StandardMaterialController._DissolveWidth))
                    throw new InvalidOperationException("lower-body material " + m.name + " has no _Dissolve/_DissolveWidth property");
            ModContext.Log("PhantomBlade: spirit " + who + " lower-body fade on (" + renderers + " renderer(s), " + list.Count + " material(s), level " + SpiritRules.Dials.LowerBodyDissolveLevel + ")");
            return list.ToArray();
        }

        // 2026-11-17 K11 진단(기능 무변경 — 실패 사유 문자열만 길어진다). 판정 = SpiritRules.ClassifyLowerBodyMismatch(엔진 근거는 그 주석).
        // 컨트롤러의 렌더러 목록·재캐시 깃발은 private(dnlib 2026-11-17: List<Renderer> m_Renderers, bool m_RenderersUpdated). GetField 직접 호출 = Exact(원장 ReflectionLookups).
        static readonly FieldInfo RenderersField = typeof(StandardMaterialController).GetField("m_Renderers", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo RenderersUpdatedField = typeof(StandardMaterialController).GetField("m_RenderersUpdated", BindingFlags.Instance | BindingFlags.NonPublic);
        // 사유 줄이 끝없이 길어지지 않게 목록마다 앞 몇 개만(숫자는 진단 출력 길이일 뿐 판정에 안 쓰인다).
        const int DiagnosisListMax = 16;

        static string MatText(Material m)
        {
            if (m == null) return "null";
            return m.name + "#" + m.GetInstanceID() + "[" + (m.shader == null ? "shader?" : m.shader.name) + "]";
        }

        static string ListText(System.Collections.Generic.IEnumerable<Material> ms)
        {
            var all = new System.Collections.Generic.List<string>();
            foreach (var m in ms) all.Add(MatText(m));
            var head = all.Count > DiagnosisListMax ? all.GetRange(0, DiagnosisListMax) : all;
            return "(" + all.Count + ") " + string.Join(", ", head.ToArray()) + (all.Count > DiagnosisListMax ? ", ..." : "");
        }

        // 진단이 던져도 원래 사유(불일치)를 가리지 않는다 — 진단 실패를 사유 뒤에 그대로 적는다(숨기지 않음).
        string SafeMismatchDiagnosis(StandardMaterialController smc, System.Collections.Generic.List<Material> controlled, string prefix)
        {
            try { return MismatchDiagnosis(smc, controlled, prefix); }
            catch (Exception e) { return "diagnosis failed: " + e.GetType().Name + ": " + e.Message; }
        }

        string MismatchDiagnosis(StandardMaterialController smc, System.Collections.Generic.List<Material> controlled, string prefix)
        {
            var cache = RenderersField == null ? null : RenderersField.GetValue(smc) as System.Collections.Generic.List<Renderer>;
            object pendingObj = RenderersUpdatedField == null ? null : RenderersUpdatedField.GetValue(smc);
            var pending = pendingObj is bool ? ((bool)pendingObj ? SpiritRules.Tri.Yes : SpiritRules.Tri.No) : SpiritRules.Tri.Unknown;
            bool allInCache = cache != null;
            var shaders = new System.Collections.Generic.List<string>();
            var lowerText = new System.Collections.Generic.List<string>();
            foreach (var r in view.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || !r.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                bool inCache = cache != null && cache.Contains(r);
                if (!inCache) allInCache = false;
                foreach (var m in r.sharedMaterials) if (m != null) shaders.Add(m.shader == null ? null : m.shader.name);
                lowerText.Add(r.name + "#" + r.GetInstanceID() + "{active " + r.gameObject.activeInHierarchy + ", enabled " + r.enabled + ", inCache " + (cache == null ? "?" : inCache.ToString()) + "} " + ListText(r.sharedMaterials));
            }
            var inCacheTri = cache == null ? SpiritRules.Tri.Unknown : (allInCache ? SpiritRules.Tri.Yes : SpiritRules.Tri.No);
            string verdict = shaders.Count == 0 ? "NoMaterials" : SpiritRules.ClassifyLowerBodyMismatch(shaders.ToArray(), inCacheTri, pending).ToString();
            return "diagnosis " + verdict + " (renderersUpdated " + pending + ", cache renderers " + (cache == null ? "?" : cache.Count.ToString()) + "); lower " + string.Join(" | ", lowerText.ToArray())
                + "; controller " + ListText(controlled);
        }

         
        // 외곽선 그리기는 ObjectRegistry<Highlighter> 만 훑고(HighlighterPass::Setup IL 0030~00AD), 컴포넌트가 꺼지면 등록이 빠진다(RegisteredBehaviour::OnDisable IL 0013~001F).
        // Highlighter 는 령 뷰 GameObject 에 붙는다(UnitEntityView::Awake IL 001D~0028 → MultiHighlighter.OnEnabled IL 0008~0012). 형식은 Owlcat.Runtime.Visual(빌드 미참조) →
        //   이름으로 찾아 UnityEngine.Behaviour.enabled 만 쓴다. 매 프레임 꺼져 있게 유지하고, 다시 켜진 것을 보면 센다. 유예(RespawnGraceSeconds) 안에 못 찾으면 한 번 보고.
        public const string HighlighterTypeName = "Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.Highlighting.Highlighter";
        Behaviour highlighter;
        bool highlightFailed;
        float highlightSearchStart = -1f;
        public int HighlighterReenables { get; private set; }
        public bool HighlighterOff { get { return highlighter != null && !highlighter.enabled; } }

        void TickNoHighlight()
        {
            if (highlightFailed) return;
            try
            {
                if (highlighter == null)
                {
                    if (highlightSearchStart < 0f) highlightSearchStart = Time.unscaledTime;
                    Behaviour found = null;
                    int n = 0;
                    foreach (var b in view.GetComponents<Behaviour>())
                        if (b != null && b.GetType().FullName == HighlighterTypeName) { found = b; n++; }
                    if (n > 1) throw new InvalidOperationException(n + " Highlighter components on the spirit view (expected one)");
                    if (found == null)
                    {
                        // 정당한 대기: MultiHighlighter.OnEnabled 가 아직 안 붙였을 수 있다.
                        if (Time.unscaledTime - highlightSearchStart > SpiritRules.Dials.RespawnGraceSeconds) throw new InvalidOperationException("no " + HighlighterTypeName + " on the spirit view after " + SpiritRules.Dials.RespawnGraceSeconds + " s");
                        return;
                    }
                    highlighter = found;
                    highlighter.enabled = false;
                    ModContext.Log("PhantomBlade: spirit " + who + " outline highlight off (Tab / hover outline excluded)");
                    return;
                }
                if (highlighter.enabled) { highlighter.enabled = false; HighlighterReenables++; }
            }
            catch (Exception e)
            {
                highlightFailed = true;
                SpiritVisualErrors.ReportOnce(SpiritVisualErrors.SiteNoHighlight, who + "#" + view.GetInstanceID(), "spirit " + who + " outline highlight could not be turned off (Tab still outlines it): " + e.Message);
            }
        }

        void Attach()
        {
            var smc = view.GetComponent<StandardMaterialController>();
            if (smc == null) throw new InvalidOperationException("spirit view has no StandardMaterialController (EntityFader.OnEnabled normally ensures one)");
            if (DissolveControllerField == null) throw new MissingFieldException("StandardMaterialController.m_DissolveController not found");
            var dc = DissolveControllerField.GetValue(smc) as DissolveAnimationController;
            if (dc == null) throw new InvalidOperationException("StandardMaterialController.m_DissolveController is null");
            if (dc.Animations == null) throw new InvalidOperationException("DissolveAnimationController.Animations is null");
            var root = Game.Instance == null ? null : Game.Instance.BlueprintRoot;
            if (root == null || root.Prefabs == null) throw new InvalidOperationException("BlueprintRoot.Prefabs is missing (no dissolve noise texture source)");
            var fog = root.Prefabs.FogOfWarDissolveSettings;
            if (fog == null) throw new InvalidOperationException("BlueprintRoot.Prefabs.FogOfWarDissolveSettings is null (no dissolve noise texture source)");
            if (fog.Texture == null) throw new InvalidOperationException("BlueprintRoot.Prefabs.FogOfWarDissolveSettings.Texture is null (no dissolve noise texture)");

            var s = Build(fog.Texture);
            dc.Animations.Add(s);
            // 되읽기(틀리면 던진다 → 위 catch 가 보고).
            int mine = 0;
            foreach (var a in dc.Animations) if (ReferenceEquals(a, s)) mine++;
            if (mine != 1) throw new InvalidOperationException("ghost dissolve is in the dissolve list " + mine + " time(s) after Add, expected 1");
            controller = dc;
            settings = s;
            ModContext.Log("PhantomBlade: spirit " + who + " ghost dissolve on (level " + SpiritRules.Dials.GhostDissolveLevel + ", width " + SpiritRules.GhostMaterialDissolveWidth(SpiritRules.Dials.GhostDissolveLevel, SpiritRules.Dials.GhostDissolveWidthScale)
                + ", flow (" + SpiritRules.Dials.GhostDissolveOffsetSpeedX + ", " + SpiritRules.Dials.GhostDissolveOffsetSpeedY + "), layer " + s.Layer + ", texture " + fog.Texture.name + "; list size " + dc.Animations.Count + ")");
        }

        void Verify()
        {
            if (!controller.Animations.Contains(settings)) throw new InvalidOperationException("ghost dissolve was removed from the spirit's dissolve list");
            if (settings.IsFinished) throw new InvalidOperationException("ghost dissolve reports IsFinished (it must loop forever)");
        }

        static DissolveSettings Build(Texture2D noise)
        {
            var d = SpiritRules.Dials.GhostDissolveLevel;
            float level = (float)SpiritRules.GhostMaterialDissolve(d);
            var edge = new Color((float)SpiritRules.Dials.GhostDissolveEdgeR, (float)SpiritRules.Dials.GhostDissolveEdgeG, (float)SpiritRules.Dials.GhostDissolveEdgeB, (float)SpiritRules.Dials.GhostDissolveEdgeA);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(edge, 0f), new GradientColorKey(edge, 1f) }, new[] { new GradientAlphaKey(edge.a, 0f), new GradientAlphaKey(edge.a, 1f) });
            var s = new DissolveSettings
            {
                Texture = noise,
                TilingScale = Vector2.one * (float)SpiritRules.Dials.GhostDissolveTilingScale,
                TilingOverride = SpiritRules.Dials.GhostDissolveTilingOverride,
                OffsetSpeed = new Vector2((float)SpiritRules.Dials.GhostDissolveOffsetSpeedX, (float)SpiritRules.Dials.GhostDissolveOffsetSpeedY),
                Delay = 0f, // 곧바로 시작(지연 중이면 엔진이 건너뜀 — Update IL 0082~0088). 페이더 선례도 안개 설정의 Delay 0.0
                Lifetime = (float)SpiritRules.Dials.GhostDissolveLifetimeSeconds,
                // 상수 곡선(시간 0·1 에 같은 값) → 정도가 흔들리지 않고, 움직이는 것은 질감 오프셋뿐(「흐르는 구멍」).
                DissolveOverLifetime = new AnimationCurve(new Keyframe(0f, level), new Keyframe(1f, level)),
                // 엔진은 이 곡선을 폭 계산에 안 읽는다(UpdateAnimation IL 0148~0152) — 같은 폭을 적어 두어 읽는 사람이 헷갈리지 않게만.
                DissolveWidthOverLifetime = new AnimationCurve(new Keyframe(0f, level), new Keyframe(1f, level)),
                DissolveWidthScale = (float)SpiritRules.Dials.GhostDissolveWidthScale,
                ColorOverLifetime = gradient,
                HdrColorScale = (float)SpiritRules.Dials.GhostDissolveHdrColorScale,
                LoopAnimation = true, // 끝나지 않게(끝나면 엔진이 목록에서 뺀다 — Update IL 0032~0080)
                DissolveCutout = SpiritRules.Dials.GhostDissolveCutout,
                DissolveEmission = SpiritRules.Dials.GhostDissolveEmission,
                Layer = SpiritRules.Dials.GhostDissolveLayer,
            };
            s.UseUnscaledTime = SpiritRules.Dials.GhostDissolveUseUnscaledTime;
            s.PlayBackwards = false;
            return s;
        }
    }
}
