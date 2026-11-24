using System;

namespace PhantomBlade
{
    // 게임 타입 의존 없음(콘솔 시험이 컴파일한다 — modinfo.json testSources). 령(스탠드 시각 유닛) 규칙의 단일 원장.
     
    // 결측 = Tri.Unknown(C1). Unknown 은 어떤 판정도 통과하지 못하고 던진다.
    public static class SpiritRules
    {
        public enum Tri { Yes, No, Unknown }
        public enum SpiritAction { Spawn, Keep, Despawn, None }
        // 령 반투명 FX 선택(Dials.SpiritTranslucentFx). None = 끔(핫픽스), Stealth00 = 은신 파티원과 같은 stealth00.fx.
        public enum TranslucentFx { None, Stealth00 }

        // 반투명 FX 를 켜는가. 정의되지 않은 값은 던진다(C1: 모르는 값이 「끔」으로 지나가지 않게).
        public static bool TranslucentFxEnabled(TranslucentFx fx)
        {
            switch (fx)
            {
                case TranslucentFx.None: return false;
                case TranslucentFx.Stealth00: return true;
                default: throw new InvalidOperationException("SpiritRules: unknown TranslucentFx value " + (int)fx);
            }
        }

        // 령 유닛 m_AddFacts 의 GUID 열(순서 포함). 껍데기 → ShadowFX → (켜졌으면) 반투명 FX 버프. 단일 원장: SpiritBlueprints.BuildClone 이 이 열로 사실을 만들고
        //   같은 열로 되읽기를 대조한다. 순서 = FX 생성 순서(같은 재질 효과 종류가 겹치면 엔진 ColorTintAnimationController.Update IL 0006~0057 이 목록의
        //   마지막 진행 중 애니메이션 하나만 쓴다 — 어느 쪽이 이기는지는 눈 점검).
        public static string[] SpiritFactGuids(TranslucentFx fx, string shellGuid, string shadowFxGuid, string translucentBuffGuid)
        {
            if (string.IsNullOrEmpty(shellGuid)) throw new ArgumentException("SpiritRules.SpiritFactGuids: shellGuid is empty");
            if (string.IsNullOrEmpty(shadowFxGuid)) throw new ArgumentException("SpiritRules.SpiritFactGuids: shadowFxGuid is empty");
            if (!TranslucentFxEnabled(fx)) return new[] { shellGuid, shadowFxGuid };
            if (string.IsNullOrEmpty(translucentBuffGuid)) throw new ArgumentException("SpiritRules.SpiritFactGuids: translucent FX is on but translucentBuffGuid is empty");
            if (translucentBuffGuid == shellGuid || translucentBuffGuid == shadowFxGuid) throw new ArgumentException("SpiritRules.SpiritFactGuids: translucentBuffGuid duplicates another fact");
            return new[] { shellGuid, shadowFxGuid, translucentBuffGuid };
        }

        // 켜진 반투명 FX 의 프리팹 AssetId. 꺼진 값(None)에 묻는 것은 호출자 오류 → 던진다(null 을 돌려주지 않는다).
        public static string TranslucentFxAssetId(TranslucentFx fx)
        {
            if (!TranslucentFxEnabled(fx)) throw new InvalidOperationException("SpiritRules: TranslucentFxAssetId asked for " + fx + " (FX off)");
            return Dials.StealthFxAssetId; // Stealth00 뿐(TranslucentFxEnabled 가 다른 값을 거른다)
        }

         
        public static class Dials
        {
            // 모델 = 그림자 악마 CR7_ShadowDemonStandard (Units/Monsters/Demon Shadow/CR7_ShadowDemonStandard.jbp, 프리팹 4ca4dc13…, Size Medium — blueprints.zip 직독 2026-11-16).
             
            public const string ModelUnitGuid = "d4b8db4dd6e88754086a5bab18974422";
             
            public const string ShadowFxBuffGuid = "8caafe9dc0ff21041b36ad225569d164";
             
            public const string GhostFxBuffGuid = "20f79fea035330b479fc899fa201d232";
             
            public const string MantisFxAssetGuid = Guids.Game.FxMantisAspect;
             
            public const bool HideWhenOwnerStealthed = true;
            // 크기 = 모델 원래 크기(그림자 악마 jbp Size = Medium, 2026-11-16 직독). 바꾸지 않고 복제 크기가 이것인지 단언만 한다(게임 Size 열거 이름). 이전 = 전령 Large(2026-11-09 판정)
            public const string ExpectedSizeName = "Medium";
            // 휘두름 동작 = 특수 공격 Claw. 그림자 악마 동작 세트에는 MainHandAttack 이 없고 SpecialAttack Bite·Claw 만 있다(정찰 §0-A·§1.3, animset.py 번들 직독).
            //   게임 열거 UnitAnimationSpecialAttackType 의 이름(Claw = 2). 애니메이터가 Enum.Parse 로 읽고, 모델에 그 동작이 없으면 던진다.
            public const string SpiritSwingSpecialAttack = "Claw";
             
            //   이 접두사로 시작하는 렌더러의 재질에만 디졸브 값을 더 세게 덮어쓴다(SpiritGhostLook). 하나도 못 찾으면 오류 보고(불투명 하체가 눈에 드러남).
            public const string LowerBodyRendererPrefix = "shadow_demon_planes";
             
            public const double LowerBodyDissolveLevel = 0.8;
             
             
            //   뒤 0.3(본체가 큰 령 앞쪽에 조금 남게 — 범위 최대) · 위 0.0(「떠 있음」이 어색하다는 지적이라 띄우지 않음). 근거가 「보기」라 임시값.
            //   0 이어도 깨지는 곳 없음: FollowTarget·Dials.ValidateValues 는 0 이상 허용, 따라가기(SpiritFollower.LateUpdate)·점검(S12 follows)에 거리 분기 없음.
            public const double SpiritBehindMeters = 0.3;   // 임시값 — 눈 점검 E2로 확정
            public const double SpiritUpMeters = 0.0;       // 임시값 — 눈 점검 E2로 확정
            public const double FollowToleranceMeters = 0.25; // 임시값 — 눈 점검 E2로 확정 (점검 허용 오차)
            // 점검 대기 상한(초). 정찰 §4 0레벨 위험 행: 생성 2초 뒤 생명 상태 단언
            public const double RespawnGraceSeconds = 2.0;
             
             
            public const double AttackFacingHoldSeconds = 1.0;

            // ---- 귀신참 돌진 연출(소유자 결정 2026-11-16, 눈 점검으로 조정 — 설계 §14.6). 규칙(명중·피해·사거리·횟수)과 무관, 령의 위치·동작만. ----
            // 돌진 시간(초): 출발점 → 대상 앞까지 직선 보간. 소유자 지정 초기값 0.15.
            public const double DashSeconds = 0.15;
            // 대상과의 간격 = 대상 반지름(UnitEntityData.Corpulence) + 이 여유(m). 초기값 = 기존 겹침 위치 다이얼(주인 뒤 SpiritBehindMeters)과 같은 값 — 소유자 지시 「일관되게」.
            public const double DashMarginMeters = SpiritBehindMeters;
            // 휘두름 1회분 머무는 시간(초). 머묾 = 발 수 × 이 값. 임의값(출처 없음) — 엔진에 UnitAnimationActionSpecialAttack.GetDuration(handle) 이 있으나
            //   값의 뜻·읽는 시점을 확인하지 못해 쓰지 않았다(보고 항목). 너무 짧으면 동작 도중 복귀하고, 길면 대상 앞에 서 있는 시간이 는다 — 눈 점검으로 조정.
            public const double DashSwingSeconds = 0.5;
            // 상한 여유(초): 돌진 + 머묾 을 이만큼 넘겨도 끝나지 않으면 강제 복귀 + 보고(휘두름이 계속 재생되지 못하는 경우). 값 = 이 모드의 대기 상한 RespawnGraceSeconds.
            public const double DashTimeoutSlackSeconds = RespawnGraceSeconds;
            // (높이 = 기존 SpiritUpMeters 재사용: 대상 발밑 높이 + SpiritUpMeters.)
             
            //   false 한 줄이면 꺼진다(핫픽스): 투사체가 본편 쇠약 광선 모습 그대로 보인다. 설계 §14.7.
            public const bool HideGhostSlashProjectileFlight = true;
            // 「이 모드가 끈 렌더러」 장부의 상한(개). 정상이면 View 가 풀로 돌아갈 때 비워져 0 근처다(귀신참 1회 = 최대 4 투사체 × View 당 렌더러 몇 개).
            //   임의값(출처 없음 — View 프리팹의 렌더러 수를 번들에서 세지 않았다): 4발 × 렌더러 16개 × 동시 시전 4번까지 담는 크기. 넘으면 기록할 수 없으므로
            //   숨기지 않고 보고한다(기록 없이 끄면 가드가 「모드가 끈 것」과 「프리팹이 원래 꺼 둔 것」을 구분하지 못한다).
            public const int VeilLedgerCapacity = 256;

            // ---- 귀신참 돌진 잔상·타격 효과(소유자 결정 2026-11-16 밤: ① 「령이 지나간 자리에 잔상」 ② 「베는 순간에 타격 효과」 — 설계 §14.8). ----
            //   돌진 시간·머무는 시간(DashSeconds · DashSwingSeconds)은 그대로다(소유자가 고르지 않음). 아래 수치는 전부 작성자 다이얼(출처 없음 — 눈 점검 E5 로 조정).
            //   FX 는 GhostSlashFxTable 의 열쇠로 고른다: 「다른 걸로」 = 아래 두 줄의 열쇠만 바꾼다(대안 열쇠는 같은 표에 이름 붙은 상수로 있다).
            // 잔상 켜기. false 한 줄이면 꺼진다(핫픽스).
            public const bool DashTrailEnabled = true;
            // 잔상 FX. 대안: GhostSlashFxTable.TrailGhostFade(유령이 흩어짐) · GhostSlashFxTable.TrailDimensionDoor(차원문 사라짐).
            public const string DashTrailFxKey = GhostSlashFxTable.TrailBlackCloud;
            // 잔상 간격(m): 경로 길이 ÷ 이 값(내림)개를 경로 위에 고르게 놓는다. 경로가 이 값보다 짧으면 잔상 없음(붙어서 쓰면 지나간 자리가 없다).
            public const double DashTrailSpacingMeters = 1.5;
            // 한 구간(가는 길 / 오는 길)의 잔상 최대 개수. 귀신참 사거리 30 ft(약 9 m)에서 간격 1.5 m 면 최대 5~6개가 나오므로 이 값이 상한이 된다.
            //   (1순위 FX 는 하나가 3.5초 남고 소리가 있다 — 너무 많으면 화면·소리가 겹친다.)
            public const int DashTrailMaxPoints = 3;
            // 오는 길(순간 복귀)에도 잔상을 남길지. 복귀는 한 프레임이라 오는 길 잔상은 그 프레임에 한꺼번에 뜬다.
            public const bool DashTrailOnReturn = true;
            // 타격 효과 켜기. false 한 줄이면 꺼진다(핫픽스).
            public const bool DashHitFxEnabled = true;
            // 타격 FX. 대안: GhostSlashFxTable.HitForceMajor(역장 큰 명중) · GhostSlashFxTable.HitDevilClaw(악마 발톱) · GhostSlashFxTable.HitNegative(음에너지 명중).
            public const string DashHitFxKey = GhostSlashFxTable.HitForceStandard;

             
            //   은신한 파티원과 같은 FX 를 FX 전용 버프(컴포넌트 0, UI 숨김)로 령에 붙인다. 끄기 = None 1줄(핫픽스용). ShadowFX 는 그대로 둔다(2026-11-09 판정).
             
            //   그 색조가 ShadowFX 의 검은 색조를 덮는다(ColorTintAnimationController.Update IL 0006~0057: 진행 중 색조 중 목록의 마지막 하나만 — 나중에 붙는 반투명 버프가 이긴다).
            //   림만 남길 수 없다(프리팹 하나에 색조·림·외곽선이 묶여 있음). 코드는 다이얼 뒤에 남긴다(Stealth00 1줄로 되살림).
            public const TranslucentFx SpiritTranslucentFx = TranslucentFx.None;
            // stealth00.fx — blueprints.zip Root/BlueprintRoot.jbp StealthEffectPrefab.guid = 이 값, Bundles/locationlist.json → "stealth00.fx" (2026-11-14 직독).
            //   엔진은 은신 유닛에 UnitEntityView.UpdateStealthMarks IL 004D~007A 로 이 프리팹을 붙인다(FxHelper.SpawnFxOnUnit).
            public const string StealthFxAssetId = "37a016af46941134ab8e45e5696f64b5";
            // PrefabLink 출처 = 본편 버프 InvisibilityBuff_Stealth_Cutscene(Spells/CutsceneSpells/…jbp)의 FxOnStart(AssetId = StealthFxAssetId, blueprints.zip 2026-11-14 직독).
            //   이 버프 자체는 쓰지 않는다(BuffInvisibility·AddFactContextActions 규칙 효과). 빌드가 이 버프의 FxOnStart 를 읽어 위 상수와 대조한다.
            public const string StealthFxPrecedentBuffGuid = "5d2476e10f6447cb8fb5b6380fe59daf";

             
            // 기전: 본편 디졸브 장치 그대로. 런타임에 DissolveSettings 하나를 령의 StandardMaterialController.DissolveController.Animations 에 넣는다
            //   (본편 선례 = BloodyFaceController.Init IL 0080~017C(new DissolveSettings → Animations.Add), EntityFader.OnEnabled IL 005F~011F(같은 방식, 질감은
            //   BlueprintRoot.Prefabs.FogOfWarDissolveSettings 에서 복사)). 엔진이 매 프레임 굴린다(StandardMaterialController.DoUpdate IL 0048~0054 →
            //   DissolveAnimationController.Update → UpdateAnimation → ApplyAnimationToMaterial 이 _Dissolve·_DissolveWidth·_DissolveMap_ST 를 씀). Harmony 0.
            // 끄기 = Off 1줄(핫픽스). 정의 밖 값은 GhostDissolveEnabled 가 던진다.
            public const GhostDissolve SpiritGhostDissolve = GhostDissolve.FogOfWarNoise;
             
            //   (2026-11-15, 0.45 → 0.55 → 0.65 「더 흐릿하게」) — 눈 점검 E1 로 다시 확인.
            public const double GhostDissolveLevel = 0.65;
            public const double GhostDissolveOwnerMin = 0.4;  
            public const double GhostDissolveOwnerMax = 0.7;  
            // 테두리 폭 배율. 엔진 폭 = 디졸브 값 × 이 배율(UpdateAnimation IL 0148~0152 — DissolveWidthOverLifetime 곡선은 이 경로에서 안 읽힘). 1.0 = ShadowFX 프리팹
            //   (Bundles\ghostformdark00_cycle_fromcutout.fx, 반복 DissolveSetup 「Fresnel_start」 Delay 2.5)의 DissolveWidthScale(UnityPy 직독 2026-11-14) → 폭 = GhostDissolveLevel(현재 0.65).
            public const double GhostDissolveWidthScale = 1.0;
            // 흐름 속도(질감 UV/초). ShadowFX 프리팹 AdditionalAlbedoSetup.OffsetSpeed(-0.1, -0.05)(같은 번들 직독) — 같은 유령 FX 안에서 흐르는 질감의 본편 값.
            //   둘 다 0 이 아니어야 흐른다: UpdateAnimation IL 0173~018D 가 x == 0 또는 y == 0 이면 오프셋을 더하지 않는다(조용히 멈춤 → Validate 가 던진다).
            public const double GhostDissolveOffsetSpeedX = -0.1;
            public const double GhostDissolveOffsetSpeedY = -0.05;
            // 한 주기 길이(초). 곡선이 상수라 모습에는 영향 없고, 0 이하이면 엔진이 즉시 끝냄(UpdateAnimation IL 0044~006D → 목록에서 빠짐). 2.5 = ShadowFX 반복 DissolveSetup.Lifetime.
            public const double GhostDissolveLifetimeSeconds = 2.5;
            // 질감 반복. ShadowFX 두 DissolveSetup 의 TilingScale (1, 1) · TilingOverride = true(재질 배율을 곱하지 않음, ApplyAnimationToMaterial IL 005A~0075).
            public const double GhostDissolveTilingScale = 1.0;
            public const bool GhostDissolveTilingOverride = true;
            // 테두리 색 = 검정 불투명(ShadowFX 두 DissolveSetup 의 ColorOverLifetime 첫 키 (0,0,0,1)), HDR 배율 0.0(같은 두 설정의 HdrColorScale) — 검은 그림자 외형 유지.
            public const double GhostDissolveEdgeR = 0.0, GhostDissolveEdgeG = 0.0, GhostDissolveEdgeB = 0.0, GhostDissolveEdgeA = 1.0;
            public const double GhostDissolveHdrColorScale = 0.0;
            // 구멍을 실제로 뚫는다(RenderType TransparentCutout·렌더 큐 2450 — SwitchAnimationForMaterial IL 0018~0036). 안개 디졸브(BlueprintRoot.jbp FogOfWarDissolveSettings.DissolveCutout = true)와 같음.
            public const bool GhostDissolveCutout = true;
            // 테두리 발광 끔(EntityFader.OnEnabled IL 00FF~0101 DissolveEmission = false 와 같음 — 검은 테두리가 빛나지 않게).
            public const bool GhostDissolveEmission = false;
            // 실시간 시계(EntityFader.OnEnabled IL 0111~0113 UseUnscaledTime = true 와 같음 — 일시정지·시간 배율과 무관하게 흐름).
            public const bool GhostDissolveUseUnscaledTime = true;
            // 층(엔진은 진행 중·지연 아닌 것 중 Layer 최대 하나만 그린다, 같으면 목록 뒤쪽 — DissolveAnimationController.Update IL 0082~00A3).
            //   ShadowFX 프리팹 두 DissolveSetup.Layer = 0(번들 직독) → 이것보다 커야 우리 것이 보인다. 본편 페이더(숨김·등장 페이드) = 10000(EntityFader.OnEnabled IL 0107)
            //   → 이것보다 작아야 페이드가 우리 것을 덮는다. 피 묻은 얼굴 = -9999(BloodyFaceController.Init IL 0153). 값 = ShadowFX 층 + 1(범위 안 아무 값이나 같은 결과).
            public const int ShadowFxDissolveLayer = 0;
            public const int EntityFaderDissolveLayer = 10000;
            public const int BloodyFaceDissolveLayer = -9999;
            public const int GhostDissolveLayer = ShadowFxDissolveLayer + 1;

             
            // 「주인이 움직이는 중」 문턱(m/s). 0 = 조금이라도 움직이면. 출처: 복사는 같은 float 를 그대로 씀(문턱이 필요 없음),
            //   엔진 TickOnUnit IL 01B9~01E1 도 문턱 없이 MovementAgent.Speed × 계수를 동작 속도로 쓴다.
            public const double OwnerMovingThresholdMps = 0.0;
            // 엔진 동작 속도 0 을 「오류」로 부르기까지의 유예(초, 실시간). 엔진 애니메이션 틱은 묶음 단위(TickProlongationEntityPool)라 한 프레임 0 은 정상일 수 있고,
             
            public const double EngineSpeedGraceSeconds = RespawnGraceSeconds;

             
            public static void Validate()
            {
                ValidateValues(SpiritBehindMeters, SpiritUpMeters, FollowToleranceMeters, RespawnGraceSeconds, ModelUnitGuid, ShadowFxBuffGuid, GhostFxBuffGuid);
                RequireDial(AttackFacingHoldSeconds, "AttackFacingHoldSeconds");
                RequireDial(DashMarginMeters, "DashMarginMeters");
                RequireDial(DashTimeoutSlackSeconds, "DashTimeoutSlackSeconds");
                // 0 이면 보간이 0 으로 나누고(돌진), 머묾이 0 초가 되어 휘두름 없이 복귀한다 → 양수만.
                if (double.IsNaN(DashSeconds) || double.IsInfinity(DashSeconds) || DashSeconds <= 0) throw new InvalidOperationException("SpiritRules.Dials.DashSeconds must be finite and > 0 (got " + DashSeconds + ")");
                if (double.IsNaN(DashSwingSeconds) || double.IsInfinity(DashSwingSeconds) || DashSwingSeconds <= 0) throw new InvalidOperationException("SpiritRules.Dials.DashSwingSeconds must be finite and > 0 (got " + DashSwingSeconds + ")");
                // 잔상·타격 다이얼: 간격은 양수(0 이면 개수가 0 으로 나누기), 개수 상한은 0 이상, 열쇠는 표에 있고 용도가 맞아야 한다(Require 가 던진다).
                if (double.IsNaN(DashTrailSpacingMeters) || double.IsInfinity(DashTrailSpacingMeters) || DashTrailSpacingMeters <= 0) throw new InvalidOperationException("SpiritRules.Dials.DashTrailSpacingMeters must be finite and > 0 (got " + DashTrailSpacingMeters + ")");
                if (DashTrailMaxPoints < 0) throw new InvalidOperationException("SpiritRules.Dials.DashTrailMaxPoints must be >= 0 (got " + DashTrailMaxPoints + ")");
                GhostSlashFxTable.Require(DashTrailFxKey, GhostSlashFxUse.Trail);
                GhostSlashFxTable.Require(DashHitFxKey, GhostSlashFxUse.Hit);
                if (string.IsNullOrEmpty(SpiritSwingSpecialAttack)) throw new InvalidOperationException("SpiritRules.Dials.SpiritSwingSpecialAttack is empty");
                if (string.IsNullOrEmpty(LowerBodyRendererPrefix)) throw new InvalidOperationException("SpiritRules.Dials.LowerBodyRendererPrefix is empty");
                if (double.IsNaN(LowerBodyDissolveLevel) || LowerBodyDissolveLevel <= GhostDissolveLevel || LowerBodyDissolveLevel > 1.0)
                    throw new InvalidOperationException("SpiritRules.Dials.LowerBodyDissolveLevel " + LowerBodyDissolveLevel + " must be in (GhostDissolveLevel " + GhostDissolveLevel + ", 1]");
                ValidateGhostDissolve(SpiritGhostDissolve, GhostDissolveLevel, GhostDissolveWidthScale, GhostDissolveOffsetSpeedX, GhostDissolveOffsetSpeedY, GhostDissolveLifetimeSeconds, GhostDissolveTilingScale, GhostDissolveLayer);
                RequireDial(OwnerMovingThresholdMps, "OwnerMovingThresholdMps");
                RequireDial(EngineSpeedGraceSeconds, "EngineSpeedGraceSeconds");
                if (EngineSpeedGraceSeconds <= 0) throw new InvalidOperationException("SpiritRules.Dials.EngineSpeedGraceSeconds must be > 0");
                TranslucentFxEnabled(SpiritTranslucentFx); // 정의되지 않은 열거값이면 던진다
                RequireGuid(StealthFxAssetId, "StealthFxAssetId");
                RequireGuid(StealthFxPrecedentBuffGuid, "StealthFxPrecedentBuffGuid");
                if (StealthFxPrecedentBuffGuid == GhostFxBuffGuid || StealthFxPrecedentBuffGuid == ShadowFxBuffGuid || StealthFxPrecedentBuffGuid == ModelUnitGuid)
                    throw new InvalidOperationException("SpiritRules.Dials: StealthFxPrecedentBuffGuid collides with another spirit dial GUID");
            }

            public static void ValidateValues(double behind, double up, double tol, double grace, string model, string shadowFx, string ghostFx)
            {
                RequireDial(behind, "SpiritBehindMeters");
                RequireDial(up, "SpiritUpMeters");
                RequireDial(tol, "FollowToleranceMeters");
                RequireDial(grace, "RespawnGraceSeconds");
                RequireGuid(model, "ModelUnitGuid");
                RequireGuid(shadowFx, "ShadowFxBuffGuid");
                RequireGuid(ghostFx, "GhostFxBuffGuid");
                if (shadowFx == ghostFx) throw new InvalidOperationException("SpiritRules.Dials: ShadowFxBuffGuid equals GhostFxBuffGuid (the blue GhostFx is forbidden on the spirit)");
                if (model == shadowFx || model == ghostFx) throw new InvalidOperationException("SpiritRules.Dials: ModelUnitGuid collides with an FX buff GUID");
            }

            static void RequireDial(double v, string name)
            {
                if (double.IsNaN(v) || double.IsInfinity(v) || v < 0) throw new InvalidOperationException("SpiritRules.Dials." + name + " must be finite and >= 0 (got " + v + ")");
            }

            static void RequireGuid(string g, string name)
            {
                if (string.IsNullOrEmpty(g) || g.Length != 32) throw new InvalidOperationException("SpiritRules.Dials." + name + " must be a 32-char GUID (got '" + g + "')");
                foreach (char c in g)
                    if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) throw new InvalidOperationException("SpiritRules.Dials." + name + " must be lowercase hex (got '" + g + "')");
            }

            // 유령 디졸브 다이얼 검사(인자로 받아 시험이 틀린 값을 실행한다). 꺼짐(Off)이면 나머지 값은 쓰이지 않으니 보지 않는다(정당).
            //   각 검사 = 엔진이 「조용히」 다르게 굴던 경우: 정도가 판정 범위 밖, 흐름 성분 0(멈춤), 수명 0 이하(즉시 끝나 빠짐), 층이 ShadowFX 이하(가려짐)·페이더 이상(숨김 페이드를 덮음).
            public static void ValidateGhostDissolve(GhostDissolve mode, double level, double widthScale, double offsetX, double offsetY, double lifetime, double tiling, int layer)
            {
                if (!GhostDissolveEnabled(mode)) return;
                foreach (var kv in new[] { Tuple.Create(level, "GhostDissolveLevel"), Tuple.Create(widthScale, "GhostDissolveWidthScale"), Tuple.Create(offsetX, "GhostDissolveOffsetSpeedX"),
                    Tuple.Create(offsetY, "GhostDissolveOffsetSpeedY"), Tuple.Create(lifetime, "GhostDissolveLifetimeSeconds"), Tuple.Create(tiling, "GhostDissolveTilingScale") })
                    if (double.IsNaN(kv.Item1) || double.IsInfinity(kv.Item1)) throw new InvalidOperationException("SpiritRules.Dials." + kv.Item2 + " must be finite (got " + kv.Item1 + ")");
                if (level < GhostDissolveOwnerMin || level > GhostDissolveOwnerMax)
                    throw new InvalidOperationException("SpiritRules.Dials.GhostDissolveLevel " + level + " is outside the owner-ruled range " + GhostDissolveOwnerMin + "–" + GhostDissolveOwnerMax);
                if (widthScale < 0) throw new InvalidOperationException("SpiritRules.Dials.GhostDissolveWidthScale must be >= 0 (got " + widthScale + ")");
                if (offsetX == 0 || offsetY == 0)
                    throw new InvalidOperationException("SpiritRules.Dials.GhostDissolveOffsetSpeed (" + offsetX + ", " + offsetY + ") has a zero component — the engine then never moves the noise (UpdateAnimation IL 0173~018D)");
                if (lifetime <= 0) throw new InvalidOperationException("SpiritRules.Dials.GhostDissolveLifetimeSeconds must be > 0 (got " + lifetime + ") — the engine finishes and drops it at once (UpdateAnimation IL 0044~006D)");
                if (tiling <= 0) throw new InvalidOperationException("SpiritRules.Dials.GhostDissolveTilingScale must be > 0 (got " + tiling + ")");
                if (layer <= ShadowFxDissolveLayer || layer >= EntityFaderDissolveLayer)
                    throw new InvalidOperationException("SpiritRules.Dials.GhostDissolveLayer " + layer + " must be > ShadowFX " + ShadowFxDissolveLayer + " and < EntityFader " + EntityFaderDissolveLayer);
            }
        }

        // ---- 유령 디졸브(판정 1) 순수 부분 ----
        public enum GhostDissolve { Off, FogOfWarNoise }

        // 정의되지 않은 값은 던진다(C1: 모르는 값이 「끔」으로 지나가지 않게).
        public static bool GhostDissolveEnabled(GhostDissolve d)
        {
            switch (d)
            {
                case GhostDissolve.Off: return false;
                case GhostDissolve.FogOfWarNoise: return true;
                default: throw new InvalidOperationException("SpiritRules: unknown GhostDissolve value " + (int)d);
            }
        }

        // 엔진이 재질에 쓸 값(점검의 기대값): _Dissolve = 곡선값 × FadeOut(1, DissolveSettings..ctor IL 0095) = 정도(상수 곡선),
        //   _DissolveWidth = 곡선값 × DissolveWidthScale(UpdateAnimation IL 0148~0152).
        public static double GhostMaterialDissolve(double level) { RequireFinite(level, "level"); return level; }
        public static double GhostMaterialDissolveWidth(double level, double widthScale)
        {
            RequireFinite(level, "level");
            RequireFiniteNonNeg(widthScale, "widthScale");
            return level * widthScale;
        }

        // ---- 하체 흐림 불일치 진단(2026-11-17 K11: UMM 로그 「lower-body fade failed」 31회·성공 0, 사유 = 판 렌더러 재질이 디졸브 컨트롤러 재질 목록에 없음) ----
        // 엔진 사실(Assembly-CSharp dnlib 2026-11-17, StandardMaterialController):
        //   ① TryUpdateMaterialsCache IL_0042~0044 GetSharedMaterials → IL_00B3~00CC 「SMC_」 복제 → IL_012E~0134 set_sharedMaterials: 복제본을 렌더러 sharedMaterials 에 되써 넣는다
        //      → 캐시가 끝난 렌더러라면 sharedMaterials 와 컨트롤러 목록은 같은 객체다(공유 재질 대 인스턴스 재질 불일치는 엔진 규칙상 원인이 아니다; 이 모드는 .materials 를 부르지 않는다).
        //   ② IL_00F9~0108: 셰이더가 「Owlcat/Lit」(CacheMaterialSettings IL_0023 ldstr) 인 재질만 m_MaterialsOwlcatLit(= get_Materials)에 들어간다
        //      → 다른 셰이더의 판 재질은 엔진 디졸브(ApplyAnimationToMaterial IL_0001 get_Materials)가 아예 다루지 않는다.
        //   ③ 렌더러 목록 m_Renderers 는 ReinitMaterials(UnitEntityView.DoLateUpdate IL_007E) 또는 PrepareForAnimation IL_000A(비활성 제외)가 채우고, m_RenderersUpdated 가 참이면 다음 PrepareForAnimation 에서야 다시 캐시한다.
        // 셋 중 무엇인지 오프라인으로 확정할 수 없다(판 재질의 셰이더는 번들 안) → 기능은 그대로 두고, 실패 보고에 판정 + 두 재질 목록을 싣는다(다음 실행이 원인을 보여 준다).
        public const string EngineOwlcatLitShaderName = "Owlcat/Lit"; // StandardMaterialController.CacheMaterialSettings IL_0023 ldstr
        public const string EngineClonePrefix = "SMC_";               // StandardMaterialController.TryUpdateMaterialsCache IL_00A7·IL_00BB ldstr
        public enum LowerBodyMismatch { NotOwlcatLit, RendererNotInCache, CachePending, Unexplained, Unknown }

        // lowerShaders = 판 렌더러 sharedMaterials 의 셰이더 이름(null = 셰이더 판독 불가). rendererInCache = 판 렌더러가 전부 m_Renderers 에 있나. cachePending = m_RenderersUpdated.
        // 판정 순서 = 엔진 순서: 셰이더(②)가 Owlcat/Lit 가 아니면 캐시 상태와 무관하게 영영 안 들어간다 → 그다음 렌더러 목록(③) → 그다음 재캐시 대기(③).
        // 입력이 결측(Unknown/null)이면 그 단계에서 Unknown — 「설명 안 됨」으로 접지 않는다(C1).
        public static LowerBodyMismatch ClassifyLowerBodyMismatch(string[] lowerShaders, Tri rendererInCache, Tri cachePending)
        {
            if (lowerShaders == null || lowerShaders.Length == 0) throw new ArgumentException("SpiritRules.ClassifyLowerBodyMismatch: no lower-body materials to classify");
            bool anyLit = false, anyUnknown = false;
            foreach (var s in lowerShaders)
            {
                if (s == null) anyUnknown = true;
                else if (s == EngineOwlcatLitShaderName) anyLit = true;
            }
            if (!anyLit) return anyUnknown ? LowerBodyMismatch.Unknown : LowerBodyMismatch.NotOwlcatLit;
            if (rendererInCache == Tri.Unknown) return LowerBodyMismatch.Unknown;
            if (rendererInCache == Tri.No) return LowerBodyMismatch.RendererNotInCache;
            if (cachePending == Tri.Unknown) return LowerBodyMismatch.Unknown;
            if (cachePending == Tri.Yes) return LowerBodyMismatch.CachePending;
            return LowerBodyMismatch.Unexplained;
        }

        // DissolveAnimationController.Update IL 0015~00A3 의 선택 규칙(독립 재현 — 시험이 층 선택을 실행해 본다): 끝나지 않고 지연되지 않은 것 중
        //   Layer 가 가장 큰 하나, 같으면 목록 뒤쪽. 없으면 -1. layers·delayed 길이가 다르면 던진다.
        public static int WinningDissolve(int[] layers, bool[] delayed)
        {
            if (layers == null || delayed == null || layers.Length != delayed.Length) throw new ArgumentException("SpiritRules.WinningDissolve: layers/delayed missing or of different length");
            int best = -1;
            for (int i = 0; i < layers.Length; i++)
            {
                if (delayed[i]) continue;
                int bestLayer = best < 0 ? int.MinValue : layers[best];
                if (layers[i] < bestLayer) continue;
                best = i;
            }
            return best;
        }

        // ---- 걷기 동작 복사(판정 2) 순수 부분 — 엔진 결과로 판정(검토 I2, 2026-11-14) ----
        // 매 LateUpdate, 새로 쓰기 「전에」 지난 프레임에 쓴 값이 엔진에서 어떻게 쓰였는지 본다:
        //   agentSpeedBefore = 엔진 TickOnUnit 이 읽은 령 m_Speed(2026-11-17: 그 틱 후처리가 적은 값 — SpiritWalkSpeedPatch. 쓰기는 같은 틱 전처리),
        //   engineAnimSpeed = 엔진이 TickOnUnit IL 01E1/0248 에서 쓴 UnitAnimationManager.Speed(= 실제 걷기 동작 속도).
        // 결과(정상): OwnerIdle(지난 프레임 주인 정지) · Copied(엔진 동작 속도 > 0) · NoPreviousWrite(지난 프레임에 안 씀) ·
         
        //   SpiritBusyAnimating(령 자기 공격 동작이 이동을 막음, 짧게만 정상) · Pending(엔진이 아직 0 — 틱 지연, 유예 안).
        // 결과(오류): AgentReset(m_Speed 가 우리가 쓴 값이 아님 — 누가 Stop/TickMovement 로 덮었거나 복사가 안 됨) ·
        //   SpiritCannotMove(CanMove 거짓, 자기 동작 때문이 아님 → TickOnUnit IL 01A7~01B7 이 0) ·
        //   EngineSawZero(실시간인데 엔진 동작 속도 0 이 유예 graceSeconds 이상 계속 — 자기 동작 중이어도 이 이상은 가리지 않는다, 검토 m3).
        // now·zeroSince = 게임 시각(초, 일시정지 중 멈춤 — 재검토 n1). zeroSince = 엔진 0 연속 시작 시각(-∞ = 연속 없음). 세 Tri 는 먼저 모두 확인(Unknown 이면 이름과 함께 던짐).
        public enum WalkCopy { OwnerIdle, Copied, NoPreviousWrite, TurnBasedSuppressed, SpiritBusyAnimating, Pending, AgentReset, SpiritCannotMove, EngineSawZero }

        public static (WalkCopy result, double zeroSince) WalkEngineStep(double lastWritten, double agentSpeedBefore, double engineAnimSpeed, Tri turnBased, Tri spiritCanMove, Tri spiritAnimPreventsMovement,
            double now, double zeroSince, double graceSeconds)
        {
            bool tb = Known(turnBased, "turnBased");
            bool canMove = Known(spiritCanMove, "spiritCanMove");
            bool busy = Known(spiritAnimPreventsMovement, "spiritAnimPreventsMovement");
            RequireFinite(now, "now");
            RequireFiniteNonNeg(agentSpeedBefore, "agentSpeedBefore");
            RequireFiniteNonNeg(engineAnimSpeed, "engineAnimSpeed");
            RequireFinite(graceSeconds, "graceSeconds");
            if (graceSeconds <= 0) throw new ArgumentOutOfRangeException("graceSeconds", graceSeconds, "SpiritRules: graceSeconds must be > 0");
            if (double.IsNaN(zeroSince) || double.IsPositiveInfinity(zeroSince)) throw new ArgumentException("SpiritRules: zeroSince must be finite or -infinity (got " + zeroSince + ")", "zeroSince");
            const double none = double.NegativeInfinity;
            if (double.IsNaN(lastWritten)) return (WalkCopy.NoPreviousWrite, none);
            RequireFiniteNonNeg(lastWritten, "lastWritten");
            if (lastWritten <= Dials.OwnerMovingThresholdMps) return (WalkCopy.OwnerIdle, none);
            if (agentSpeedBefore != lastWritten) return (WalkCopy.AgentReset, none);
            if (engineAnimSpeed > 0) return (WalkCopy.Copied, none);
            if (tb) return (WalkCopy.TurnBasedSuppressed, none);
            if (!canMove && !busy) return (WalkCopy.SpiritCannotMove, none);
            double since = double.IsNegativeInfinity(zeroSince) ? now : zeroSince;
            if (now - since >= graceSeconds) return (WalkCopy.EngineSawZero, since);
            return (busy ? WalkCopy.SpiritBusyAnimating : WalkCopy.Pending, since);
        }

        public static bool WalkCopyIsError(WalkCopy r)
        {
            switch (r)
            {
                case WalkCopy.OwnerIdle: case WalkCopy.Copied: case WalkCopy.NoPreviousWrite: case WalkCopy.TurnBasedSuppressed: case WalkCopy.SpiritBusyAnimating: case WalkCopy.Pending: return false;
                case WalkCopy.AgentReset: case WalkCopy.SpiritCannotMove: case WalkCopy.EngineSawZero: return true;
                default: throw new InvalidOperationException("SpiritRules: unknown WalkCopy value " + (int)r);
            }
        }

        // 버스트당 령 휘두름 상한 = 본체 명중 하나가 일으킬 수 있는 최대 스탠드 타수(StandRules.MaxStrikesPerHit). 리터럴 금지.
        // 버스트 = 같은 프레임 = 명중 하나의 주먹 묶음(StandStrike.cs for 루프). Task 6b 부터 라운드 타수(최대 5)는 명중 5번에 나뉘어 1타씩 오고,
        // 한 명중의 묶음은 평상 1타·전력 공격 FullAttackStrikesPerHit 타다 → 라운드 타수가 아니라 명중당 최대 타수가 이 상한이다(시험이 IL·값으로 잠근다).
        public static int MaxSwingsPerBurst { get { return StandRules.MaxStrikesPerHit; } }

        static bool Known(Tri t, string name)
        {
            if (t == Tri.Unknown) throw new InvalidOperationException("SpiritRules: input '" + name + "' is Unknown (missing evidence must not pass as No)");
            if (t != Tri.Yes && t != Tri.No) throw new InvalidOperationException("SpiritRules: input '" + name + "' has invalid value " + (int)t);
            return t == Tri.Yes;
        }

         
        // 다섯 입력을 전부 먼저 확인한다(단락 평가로 Unknown 이 숨지 않게).
        public static SpiritAction Decide(Tri standOn, Tri ownerConscious, Tri ownerInGame, Tri areaLoaded, Tri ownerHiddenByEngine, bool spiritExists)
        {
            bool s = Known(standOn, "standOn");
            bool c = Known(ownerConscious, "ownerConscious");
            bool g = Known(ownerInGame, "ownerInGame");
            bool a = Known(areaLoaded, "areaLoaded");
            bool h = Known(ownerHiddenByEngine, "ownerHiddenByEngine");
            bool should = s && c && g && a && !h;
            if (should) return spiritExists ? SpiritAction.Keep : SpiritAction.Spawn;
            return spiritExists ? SpiritAction.Despawn : SpiritAction.None;
        }

         
        public static bool ShouldShow(Tri ownerStealthed, Tri ownerInvisible)
        {
            bool st = Known(ownerStealthed, "ownerStealthed");
            bool inv = Known(ownerInvisible, "ownerInvisible");
            return !(Dials.HideWhenOwnerStealthed && (st || inv));
        }

         

        // 본체가 화면에 있음 = 본체 뷰 활성(UnitEntityData.IsViewActive: GetViewActive(true) IL 0000~002B — IsInGame 이고 잠들지 않음)
        //   ∧ 게임 안(EntityDataBase.IsInGame). 컷신·대화에서 본체가 숨거나 꺼지는 잔여 경우(정찰 §4 「숨김 연출·컷신」 행): 령을 지우지 않고 숨긴다.
        //   둘 다 먼저 확인한다(단락 평가로 Unknown 이 숨지 않게).
        public static bool OwnerPresent(Tri ownerViewActive, Tri ownerInGame)
        {
            bool va = Known(ownerViewActive, "ownerViewActive");
            bool ig = Known(ownerInGame, "ownerInGame");
            return va && ig;
        }

        public enum VisibilityChange { None, Hide, Show }

        // 숨김 전이. show = 은신 규칙(ShouldShow) ∧ 본체 존재(OwnerPresent). hiddenByUs = 따라가기가 령의 Hidden 깃발을 붙잡고 있음.
        // 바뀔 때만 Hide(붙잡기 1회)/Show(놓기 1회) — 매 프레임 Retain 이 쌓이거나 로그가 반복되지 않는다(본편 UnitPartInPit.SetHidden IL 0000~0049 과 같은 걸쇠 방식).
        public static VisibilityChange VisibilityStep(bool stealthRuleShows, bool ownerPresent, bool hiddenByUs)
        {
            bool show = stealthRuleShows && ownerPresent;
            if (show) return hiddenByUs ? VisibilityChange.Show : VisibilityChange.None;
            return hiddenByUs ? VisibilityChange.None : VisibilityChange.Hide;
        }

        // 공격 방향 유지 중인가: now < holdUntil. holdUntil = -∞ 는 「한 번도 설정 안 함」(정당한 초기값). NaN·+∞ 유지, 비유한 now 는 던진다.
        public static bool FacingHeld(double now, double holdUntil)
        {
            RequireFinite(now, "now");
            if (double.IsNaN(holdUntil) || double.IsPositiveInfinity(holdUntil)) throw new ArgumentException("SpiritRules: holdUntil must be finite or -infinity (got " + holdUntil + ")", "holdUntil");
            return now < holdUntil;
        }

        public enum FollowerStep { Run, Stop, RemoveOrphan }

        // 따라가기 유효성: 령이 사라지는 중이면 멈춤(지울 것 없음 — 엔진이 이미 지움). 령은 살아 있는데 주인이 폐기·파괴됐으면 고아 제거
         
        public static FollowerStep FollowerCheck(bool spiritGone, bool ownerGone)
        {
            if (spiritGone) return FollowerStep.Stop;
            return ownerGone ? FollowerStep.RemoveOrphan : FollowerStep.Run;
        }

        static void RequireFinite(double v, string name)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) throw new ArgumentException("SpiritRules: " + name + " must be finite (got " + v + ")", name);
        }

        static void RequireFiniteNonNeg(double v, string name)
        {
            RequireFinite(v, name);
            if (v < 0) throw new ArgumentOutOfRangeException(name, v, "SpiritRules: " + name + " must be >= 0");
        }

         
        public static (double x, double y, double z) FollowTarget(double ox, double oy, double oz, double orientationDeg, double behindM, double upM)
        {
            RequireFinite(ox, "ox");
            RequireFinite(oy, "oy");
            RequireFinite(oz, "oz");
            RequireFinite(orientationDeg, "orientationDeg");
            RequireFiniteNonNeg(behindM, "behindM");
            RequireFiniteNonNeg(upM, "upM");
            double rad = orientationDeg * Math.PI / 180.0;
            double fx = Math.Sin(rad), fz = Math.Cos(rad);
            return (ox - fx * behindM, oy + upM, oz - fz * behindM);
        }





        // 버스트의 주먹 명중 수 -> 령 휘두름 수(상한 MaxSwingsPerBurst).
        public static int SwingsFor(int fistHitsInBurst)
        {
            if (fistHitsInBurst < 0) throw new ArgumentOutOfRangeException("fistHitsInBurst", fistHitsInBurst, "SpiritRules: fistHitsInBurst must be >= 0");
            return Math.Min(fistHitsInBurst, MaxSwingsPerBurst);
        }









         
        // 명중 여부는 휘두름을 정하지 않지만, 굴림 결측(null)은 여전히 깨진 규칙이다 → 던진다(「빗나감」으로 조용히 읽지 않음).
        public static bool SpiritSwingsOnFist(bool isStandFistBlueprint, bool? attackHit)
        {
            if (!attackHit.HasValue) throw new InvalidOperationException("SpiritRules: attackHit is missing (no attack roll) — cannot decide a spirit swing");
            return isStandFistBlueprint;
        }

        // 독립 접촉 굴림(Task 6d): 무기 공격 안의 굴림은 무기 처리기가 이미 휘두른다 → 두 번 휘두르지 않는다.
        public static bool SpiritSwingsOnTouch(bool isStandFistBlueprint, bool partOfWeaponAttack, bool? attackHit)
        {
            if (!attackHit.HasValue) throw new InvalidOperationException("SpiritRules: attackHit is missing (no attack roll result) — cannot decide a spirit swing");
            return isStandFistBlueprint && !partOfWeaponAttack;
        }

         
        //   None = 우리 능력이 아님(주인의 다른 주문·능력) 또는 시전 실패(주문 실패·집중 실패 — 엔진이 SpawnInterruptFx, UnitUseAbility.OnAction IL 0228~0231).
        //   FaceTarget = 귀신참(적 단일 대상, CanTargetPoint/Self = false — StandBlueprints.BuildGhostSlash) → 대상 쪽을 보고 휘두름.
        //   InPlace = 전력 공격(Personal, 대상 = 자신) → 제자리에서 휘두름(볼 곳 없음).
        // 모순 입력(두 능력 동시·귀신참 대상이 유닛 아님/자신·전력 공격 대상이 자신 아님)은 블루프린트와 어긋난 것 → 던진다(조용히 None 금지).
        public enum CastSwing { None, FaceTarget, InPlace }
        public static CastSwing CastSwingFor(bool isGhostSlash, bool isFullAttack, bool castSucceeded, bool targetIsUnit, bool targetIsOwner)
        {
            if (isGhostSlash && isFullAttack) throw new InvalidOperationException("SpiritRules.CastSwingFor: one ability cannot be both ghost slash and full attack");
            if (!isGhostSlash && !isFullAttack) return CastSwing.None;
            if (!castSucceeded) return CastSwing.None;
            if (isGhostSlash)
            {
                if (!targetIsUnit || targetIsOwner) throw new InvalidOperationException("SpiritRules.CastSwingFor: ghost slash cast at " + (targetIsUnit ? "its own caster" : "a point") + " — the ability only targets enemy units");
                return CastSwing.FaceTarget;
            }
            if (!targetIsUnit || !targetIsOwner) throw new InvalidOperationException("SpiritRules.CastSwingFor: full attack is Personal, but its target is " + (targetIsUnit ? "another unit" : "a point"));
            return CastSwing.InPlace;
        }

         

        // 버스트 기록의 「아직 없음」 표식. Unity Time.frameCount 는 0 이상이라 겹치지 않는다.
        public const int NoFrame = -1;

        // 러시 한 번의 주먹은 ContextActionStandStrike.RunAction 의 for 루프(StandStrike.cs) 안에서 같은 프레임에 모두 규칙이 돈다 → 같은 프레임 명중 = 한 버스트.
        // (lastFrame, hitsInLastFrame) = 지금까지 기록, nowFrame = 이번 명중의 프레임. 돌려주는 값 = 새 기록 + 이번 명중에 휘두를지.
        // 휘두름 = 이번 명중으로 SwingsFor(버스트 명중 수) 가 1 늘었을 때(상한 MaxSwingsPerBurst 에서 파생 — 리터럴 없음).
        // 시간 역행·음수·「프레임 없는데 명중 수 있음」·「프레임 있는데 명중 0」은 기록이 깨진 것 → 던진다.
        public static (int frame, int hits, bool play) BurstStep(int lastFrame, int hitsInLastFrame, int nowFrame)
        {
            if (nowFrame < 0) throw new ArgumentOutOfRangeException("nowFrame", nowFrame, "SpiritRules.BurstStep: frame must be >= 0");
            if (lastFrame < NoFrame) throw new ArgumentOutOfRangeException("lastFrame", lastFrame, "SpiritRules.BurstStep: lastFrame must be >= NoFrame");
            if (hitsInLastFrame < 0) throw new ArgumentOutOfRangeException("hitsInLastFrame", hitsInLastFrame, "SpiritRules.BurstStep: hits must be >= 0");
            if (lastFrame == NoFrame && hitsInLastFrame != 0) throw new InvalidOperationException("SpiritRules.BurstStep: " + hitsInLastFrame + " hit(s) recorded without a frame");
            if (lastFrame != NoFrame && hitsInLastFrame == 0) throw new InvalidOperationException("SpiritRules.BurstStep: frame " + lastFrame + " recorded with 0 hits");
            if (nowFrame < lastFrame) throw new InvalidOperationException("SpiritRules.BurstStep: frame went back (" + lastFrame + " -> " + nowFrame + ")");
            int hits = nowFrame == lastFrame ? hitsInLastFrame + 1 : 1;
            bool play = SwingsFor(hits) > SwingsFor(hits - 1);
            return (nowFrame, hits, play);
        }

        // 공격 방향 유지 끝(Time.unscaledTime 기준) = 지금 + 다이얼(SpiritFollower.HoldFacingUntil 에 넘긴다).
        public static double FacingHoldEnd(double now)
        {
            RequireFinite(now, "now");
            return now + Dials.AttackFacingHoldSeconds;
        }

        // ---- 귀신참 돌진(소유자 결정 2026-11-16, 설계 §14.6): 귀신참을 쓰면 령이 대상 앞까지 아주 빠르게 가서 발 수만큼 휘두르고 주인 뒤로 순간 복귀한다. ----
        // 연출만이다(규칙 없음). 게임 쪽(SpiritFollower.DashPlace · StandSpiritAnimator.TryStartDash)은 값을 읽어 넘기고 결과를 대입할 뿐 — 상태 전이·위치 계산은 전부 여기(시험이 실행).
        // 상태: Idle(대기 = 종전 따라가기) → Dashing(출발점 → 대상 앞, 직선 보간) → Holding(대상 앞에서 머물며 휘두름) → Idle(복귀는 보간 없이 그 프레임에 따라가기 위치).

        public enum DashPhase { Idle, Dashing, Holding }

        // 시작 판정. Dash = 돌진, SwingInPlace = 제자리 휘두름(2026-11-15 동작 그대로) — 조용한 폴백이 아니라 이 명시적 분기다(게임 쪽이 한 줄 기록).
        //   제자리 사유: 대상이 사라지는 중(파괴·폐기) / 이미 쓰러짐 / 게임 밖(위치가 뜻이 없음). 「게임 안 여부」를 모르면(Unknown) 던진다.
        public enum DashStart { Dash, SwingInPlace }
        public static DashStart DashStartFor(bool targetGone, bool targetDead, Tri targetInGame)
        {
            bool inGame = Known(targetInGame, "targetInGame");
            return targetGone || targetDead || !inGame ? DashStart.SwingInPlace : DashStart.Dash;
        }

        // 돌진 도중 즉시 대기로 돌아가는 사유(명시적 분기, 게임 쪽이 한 줄 기록). 대상이 죽는 것은 사유가 아니다 — 광선이 대상을 쓰러뜨려도 남은 휘두름은
        //   그 자리에서 끝낸다(쓰러진 유닛의 위치는 유효). 주인 사망·스탠드 끔·지역 이동·령 파괴는 령(과 이 상태를 가진 따라가기 컴포넌트)이 지워지므로 여기 오지 않는다.
        public enum DashAbort { None, TargetGone, TargetOutOfGame, OwnerLeftCombat }
        public static DashAbort DashAbortFor(bool targetGone, Tri targetInGame, bool ownerLeftCombat)
        {
            bool inGame = Known(targetInGame, "targetInGame");
            if (targetGone) return DashAbort.TargetGone;
            if (!inGame) return DashAbort.TargetOutOfGame;
            return ownerLeftCombat ? DashAbort.OwnerLeftCombat : DashAbort.None;
        }

        // 휘두름 수 = 귀신참 발 수(GhostSlash.ProjectilesByLevel): 1..MaxProjectiles. 밖이면 던진다.
        public static void RequireDashSwings(int swings)
        {
            if (swings < 1 || swings > GhostSlash.MaxProjectiles) throw new ArgumentOutOfRangeException("swings", swings, "SpiritRules: dash swings must be 1.." + GhostSlash.MaxProjectiles + " (ghost slash projectiles)");
        }

        static void RequirePositive(double v, string name)
        {
            RequireFinite(v, name);
            if (v <= 0) throw new ArgumentOutOfRangeException(name, v, "SpiritRules: " + name + " must be > 0");
        }

        // 머묾 길이 = 발 수 × 휘두름 1회분. 돌진 전체 = 돌진 + 머묾. 상한 = 전체 + 여유.
        public static double DashHoldSeconds(int swings, double swingSeconds) { RequireDashSwings(swings); RequirePositive(swingSeconds, "swingSeconds"); return swings * swingSeconds; }
        public static double DashTotalSeconds(int swings, double dashSeconds, double swingSeconds) { RequirePositive(dashSeconds, "dashSeconds"); return dashSeconds + DashHoldSeconds(swings, swingSeconds); }
        public static double DashCapSeconds(int swings, double dashSeconds, double swingSeconds, double slackSeconds) { RequireFiniteNonNeg(slackSeconds, "slackSeconds"); return DashTotalSeconds(swings, dashSeconds, swingSeconds) + slackSeconds; }

        // 대상 앞 지점: 대상 중심에서 출발점 쪽으로 (대상 반지름 + 여유) 떨어진 곳(수평), 높이 = 대상 높이 + 위 오프셋.
        //   출발점이 이미 그 간격 안이면(주인이 대상에 붙어 있음) 움직이지 않는다 = 출발점 그대로(뒤로 물러나는 「돌진」은 없다).
        public static (double x, double y, double z) DashDestination((double x, double y, double z) start, (double x, double y, double z) target, double targetRadius, double marginM, double upM)
        {
            RequireFinite(start.x, "start.x"); RequireFinite(start.y, "start.y"); RequireFinite(start.z, "start.z");
            RequireFinite(target.x, "target.x"); RequireFinite(target.y, "target.y"); RequireFinite(target.z, "target.z");
            RequireFiniteNonNeg(targetRadius, "targetRadius");
            RequireFiniteNonNeg(marginM, "marginM");
            RequireFiniteNonNeg(upM, "upM");
            double gap = targetRadius + marginM;
            double dx = start.x - target.x, dz = start.z - target.z;
            double dist = Math.Sqrt(dx * dx + dz * dz);
            if (dist <= gap) return start;
            return (target.x + dx / dist * gap, target.y + upM, target.z + dz / dist * gap);
        }

        // 한 프레임의 결과: 령 위치, 바라볼 방향(도), 다음 상태, 이번 프레임에 휘두름 1회를 낼지, 상한 초과로 강제 복귀했는지.
        public struct DashFrame
        {
            public readonly double X, Y, Z, FacingDeg;
            public readonly DashPhase Phase;
            public readonly bool FireSwing, TimedOut;
            public DashFrame(double x, double y, double z, double facingDeg, DashPhase phase, bool fireSwing, bool timedOut) { X = x; Y = y; Z = z; FacingDeg = facingDeg; Phase = phase; FireSwing = fireSwing; TimedOut = timedOut; }
        }

        // 돌진 상태기(순수). elapsed = 돌진 시작 뒤 흐른 시간(초, 일시 정지 제외 — 게임 쪽이 센다), swings = 낼 휘두름 수(발 수), swingsDone = 이미 재생된 수,
        //   start = 돌진을 시작한 순간의 령 위치, follow / ownerFacingDeg = 이번 프레임의 따라가기 위치·주인 방향(복귀 지점), target / targetRadius = 대상 위치·반지름.
        // 순서:
        //   ① Idle → 따라가기 위치 그대로.
        //   ② elapsed > 상한(DashCapSeconds) → 강제 복귀(TimedOut) — 휘두름이 계속 재생되지 못해 swingsDone 이 차지 않는 경우에도 상태가 영구히 남지 않게.
        //   ③ 휘두름을 다 냈고(swingsDone == swings) 머묾 시간도 지났다 → 복귀. 복귀는 보간하지 않는다: 그 프레임의 위치 = 따라가기 위치(소유자 「순간 복귀」).
        //   ④ elapsed < 돌진 시간 → Dashing: 출발점과 대상 앞 사이를 직선 보간(t = elapsed / 돌진 시간).
        //   ⑤ 그 밖 → Holding: 위치 = 대상 앞(정확히). 지금까지 냈어야 할 수 = min(swings, 1 + ⌊(elapsed − 돌진) / 1회분⌋) 보다 적게 냈으면 이번 프레임에 1회(FireSwing).
        // 방향 = 령 위치에서 대상 쪽(수평, FollowTarget 과 같은 규약: 앞 = (sin θ, 0, cos θ)). 대상과 겹쳐 방향이 없으면 주인 방향.
        public static DashFrame DashStep(DashPhase phase, double elapsed, int swings, int swingsDone,
            (double x, double y, double z) start, (double x, double y, double z) follow, double ownerFacingDeg, (double x, double y, double z) target, double targetRadius,
            double dashSeconds, double swingSeconds, double marginM, double upM, double slackSeconds)
        {
            RequireFinite(follow.x, "follow.x"); RequireFinite(follow.y, "follow.y"); RequireFinite(follow.z, "follow.z");
            RequireFinite(ownerFacingDeg, "ownerFacingDeg");
            if (phase == DashPhase.Idle) return new DashFrame(follow.x, follow.y, follow.z, ownerFacingDeg, DashPhase.Idle, false, false);
            if (phase != DashPhase.Dashing && phase != DashPhase.Holding) throw new InvalidOperationException("SpiritRules.DashStep: unknown phase " + (int)phase);
            RequireFiniteNonNeg(elapsed, "elapsed");
            RequireDashSwings(swings);
            if (swingsDone < 0 || swingsDone > swings) throw new ArgumentOutOfRangeException("swingsDone", swingsDone, "SpiritRules.DashStep: swingsDone must be 0.." + swings);
            double total = DashTotalSeconds(swings, dashSeconds, swingSeconds);
            var dest = DashDestination(start, target, targetRadius, marginM, upM);

            if (elapsed > DashCapSeconds(swings, dashSeconds, swingSeconds, slackSeconds))
                return new DashFrame(follow.x, follow.y, follow.z, ownerFacingDeg, DashPhase.Idle, false, true);
            if (swingsDone == swings && elapsed >= total)
                return new DashFrame(follow.x, follow.y, follow.z, ownerFacingDeg, DashPhase.Idle, false, false);

            if (elapsed < dashSeconds)
            {
                if (swingsDone != 0) throw new InvalidOperationException("SpiritRules.DashStep: " + swingsDone + " swing(s) recorded before the dash arrived (elapsed " + elapsed + " < " + dashSeconds + ")");
                double t = elapsed / dashSeconds;
                double x = start.x + (dest.x - start.x) * t, y = start.y + (dest.y - start.y) * t, z = start.z + (dest.z - start.z) * t;
                return new DashFrame(x, y, z, FacingToward(x, z, target.x, target.z, ownerFacingDeg), DashPhase.Dashing, false, false);
            }
            int due = Math.Min(swings, 1 + (int)Math.Floor((elapsed - dashSeconds) / swingSeconds));
            return new DashFrame(dest.x, dest.y, dest.z, FacingToward(dest.x, dest.z, target.x, target.z, ownerFacingDeg), DashPhase.Holding, swingsDone < due, false);
        }

        // ---- 귀신참 돌진 잔상·타격 효과(소유자 결정 2026-11-16 밤, 설계 §14.8). 판정·지점 계산만 여기(순수, 시험이 실행), FX 를 띄우는 것은 GhostSlashFx.cs. ----

        // 한 구간의 잔상 개수 = min(상한, ⌊경로 길이 ÷ 간격⌋). 경로가 간격보다 짧으면 0(정당한 「잔상 없음」 — 지나간 자리가 없다). 아주 긴 경로도 상한을 넘지 않는다.
        public static int DashTrailCount(double distanceM, double spacingM, int maxPoints)
        {
            RequireFiniteNonNeg(distanceM, "distanceM");
            RequirePositive(spacingM, "spacingM");
            if (maxPoints < 0) throw new ArgumentOutOfRangeException("maxPoints", maxPoints, "SpiritRules.DashTrailCount: maxPoints must be >= 0");
            double n = Math.Floor(distanceM / spacingM);
            return n >= maxPoints ? maxPoints : (int)n;
        }

        // 잔상 지점: from → to 직선(높이 포함) 위에 n 개를 고르게(i / (n + 1), i = 1..n). 양 끝점은 넣지 않는다 — 출발점은 주인과 겹친 자리, 도착점은 령이 서 있는 자리다.
        //   순서 = from 에서 가까운 것부터(가는 길은 이 순서로 차례로 띄운다). 좌표가 유한하지 않으면 던진다.
        public static (double x, double y, double z)[] DashTrailPoints((double x, double y, double z) from, (double x, double y, double z) to, double spacingM, int maxPoints)
        {
            RequireFinite(from.x, "from.x"); RequireFinite(from.y, "from.y"); RequireFinite(from.z, "from.z");
            RequireFinite(to.x, "to.x"); RequireFinite(to.y, "to.y"); RequireFinite(to.z, "to.z");
            double dx = to.x - from.x, dy = to.y - from.y, dz = to.z - from.z;
            int n = DashTrailCount(Math.Sqrt(dx * dx + dy * dy + dz * dz), spacingM, maxPoints);
            var pts = new (double x, double y, double z)[n];
            for (int i = 1; i <= n; i++)
            {
                double t = (double)i / (n + 1);
                pts[i - 1] = (from.x + dx * t, from.y + dy * t, from.z + dz * t);
            }
            return pts;
        }

        // 가는 길에서 지금까지 띄웠어야 할 잔상 수: 령이 이미 지나친 지점 수 = 진행률(0..1) ≥ i / (n + 1) 인 i 의 개수. 도착(진행률 ≥ 1)이면 전부.
        public static int DashTrailDue(int points, double progress)
        {
            if (points < 0) throw new ArgumentOutOfRangeException("points", points, "SpiritRules.DashTrailDue: points must be >= 0");
            RequireFiniteNonNeg(progress, "progress");
            if (progress >= 1.0) return points;
            // i ≤ progress × (n + 1). 부동소수 오차로 지점 하나가 한 프레임 늦는 것은 무해하다(도착 프레임에 전부 뜬다).
            return Math.Min(points, (int)Math.Floor(progress * (points + 1)));
        }

        // 잔상을 띄울지. Show 가 아니면 띄우지 않는다(명시적 분기):
        //   Disabled = 다이얼 꺼짐 / SpiritHidden = 령이 숨겨진 상태(주인 은신·투명·본체 없음 — SpiritFollower 의 hiddenByUs). 령을 숨기는 규칙(HideWhenOwnerStealthed)과 같은 이유:
        //   숨은 귀검사의 위치를 FX 가 드러내면 안 된다.
        public enum DashFx { Show, Disabled, SpiritHidden }
        public static DashFx DashTrailFor(bool dialOn, bool spiritHidden)
        {
            if (!dialOn) return DashFx.Disabled;
            return spiritHidden ? DashFx.SpiritHidden : DashFx.Show;
        }

        // 타격 효과를 띄울지(휘두름 1회마다 묻는다). Show 가 아니면 띄우지 않는다(명시적 분기):
        //   Disabled / SpiritHidden = 위와 같음. TargetGone = 대상이 사라지는 중(파괴·폐기), TargetOutOfGame = 게임 밖, TargetDown = 이미 쓰러짐
        //   (광선이 대상을 쓰러뜨린 뒤의 남은 휘두름 — 령은 휘두르지만 쓰러진 대상에 타격 효과는 띄우지 않는다). 「게임 안 여부」를 모르면(Unknown) 던진다.
        public enum DashHitFx { Show, Disabled, SpiritHidden, TargetGone, TargetOutOfGame, TargetDown }
        public static DashHitFx DashHitFxFor(bool dialOn, bool spiritHidden, bool targetGone, Tri targetInGame, bool targetDead)
        {
            bool inGame = Known(targetInGame, "targetInGame");
            if (!dialOn) return DashHitFx.Disabled;
            if (spiritHidden) return DashHitFx.SpiritHidden;
            if (targetGone) return DashHitFx.TargetGone;
            if (!inGame) return DashHitFx.TargetOutOfGame;
            return targetDead ? DashHitFx.TargetDown : DashHitFx.Show;
        }

         
        // 1단계(발사 순간, 굴림이 아직 없음): 이 투사체가 「후보」인가 = 다이얼 켜짐 ∧ 발사자 = 이 귀검사 ∧ 블루프린트 = 귀신참이 쓰는 투사체.
        //   No = 정당한 아님(다이얼 꺼짐·남의 투사체·다른 블루프린트), Candidate = 2단계로, Unknown = 우리 블루프린트인데 발사자를 읽을 수 없음(보고 후 보이는 채로).
        public enum VeilCandidate { No, Candidate, Unknown }
        public static VeilCandidate VeilCandidateFor(bool dialOn, bool blueprintIsGhostSlashProjectile, Tri launcherIsOwner)
        {
            if (!dialOn || !blueprintIsGhostSlashProjectile) return VeilCandidate.No;
            if (launcherIsOwner == Tri.Unknown) return VeilCandidate.Unknown;
            return Known(launcherIsOwner, "launcherIsOwner") ? VeilCandidate.Candidate : VeilCandidate.No;
        }

        // 2단계(같은 프레임, 엔진이 투사체에 명중 굴림을 단 뒤): 그 굴림의 원인 능력이 귀신참인가 — 3 상태.
        //   같은 투사체 블루프린트(쇠약 광선)를 본편 주문도 쓴다 → 「이 귀검사가 쏜 쇠약 광선 투사체」만으로는 귀신참이라 할 수 없다.
        //   GhostSlash = 굴림 있음 ∧ 원인 능력 = 귀신참 → 숨김. Other = 굴림 있음 ∧ 원인 능력이 다른 능력(본편 쇠약 광선 등) → 그대로.
        //   Unknown = 굴림이 없거나(이 블루프린트를 쓰는 능력은 모두 굴린다 — 아직 안 달렸거나 전달이 깨짐) 원인 능력을 읽을 수 없음 → 「아님」이 아니다: 보고 후 보이는 채로.
        public enum ProjectileOrigin { GhostSlash, Other, Unknown }
        public static ProjectileOrigin ProjectileOriginFor(bool hasAttackRoll, bool sourceAbilityKnown, bool sourceIsGhostSlash)
        {
            if (sourceIsGhostSlash && (!hasAttackRoll || !sourceAbilityKnown)) throw new InvalidOperationException("SpiritRules.ProjectileOriginFor: source is ghost slash but the roll or its source ability is missing (broken reading)");
            if (!hasAttackRoll || !sourceAbilityKnown) return ProjectileOrigin.Unknown;
            return sourceIsGhostSlash ? ProjectileOrigin.GhostSlash : ProjectileOrigin.Other;
        }

        // 끈 뒤 되읽기: found = View 아래 렌더러 수, disabledByUs = 켜져 있어서 우리가 끈 수, stillEnabled = 끈 뒤에도 켜져 있는 수.
        //   Hidden = 하나 이상 껐고 남은 것 없음. NoRenderers = 렌더러를 하나도 못 찾음(끌 것이 없다 — 숨기지 못함). StillVisible = 끈 뒤에도 켜진 것이 있음.
        //   AlreadyOff = 렌더러는 있는데 전부 이미 꺼져 있었다(우리가 끈 것 0) — 숨긴 것으로 치지 않는다(복원할 것도 없다). 뒤 셋은 보고 대상.
        public enum VeilReadback { Hidden, NoRenderers, StillVisible, AlreadyOff }
        public static VeilReadback VeilReadbackFor(int found, int disabledByUs, int stillEnabled)
        {
            if (found < 0 || disabledByUs < 0 || stillEnabled < 0 || disabledByUs > found || stillEnabled > found) throw new ArgumentOutOfRangeException("found", found, "SpiritRules.VeilReadbackFor: counts out of range (" + found + ", " + disabledByUs + ", " + stillEnabled + ")");
            if (found == 0) return VeilReadback.NoRenderers;
            if (stillEnabled > 0) return VeilReadback.StillVisible;
            return disabledByUs == 0 ? VeilReadback.AlreadyOff : VeilReadback.Hidden;
        }

         
        //   직전 구현의 두 「조용히 잘못된 값」 후보(풀 반납이 OnDisable 을 부르지 않는다 / 같은 프레임 재사용에서 꺼진 채 남는다)가 실제로 일어나면,
        //   다음에 그 View 가 쓰일 때 본편 쇠약 광선이 오류 없이 안 보인다. 가드 = 쇠약 광선 투사체가 발사될 때마다 그 View 를 훑어 흔적을 되켜고 오류를 남긴다.
        // 「모드가 끈 것」과 「프리팹이 원래 꺼 둔 것」의 구분 = 장부(GhostSlashVeilLedger: 렌더러 인스턴스 ID → 끈 가림 컴포넌트). 끄기 전에 적고, 되켤 때 지운다.
        //   렌더러 하나의 처분: 장부에 없음 → None(꺼져 있어도 절대 켜지 않는다 — 원래 꺼 둔 것일 수 있다). 장부에 있고 그것을 끈 가림 컴포넌트가 아직 숨기는 중 → None(정당하게 숨겨진 것).
        //   장부에 있고 주인이 없는데 꺼져 있음 → ReenableOrphan(흔적: 되켜고 보고). 장부에 있고 주인이 없는데 켜져 있음 → DropStaleEntry(누가 이미 켰다 — 기록만 지운다).
        public enum VeilLeftover { None, ReenableOrphan, DropStaleEntry }
        public static VeilLeftover VeilLeftoverFor(bool rendererEnabled, bool inLedger, bool ownerActive)
        {
            if (!inLedger || ownerActive) return VeilLeftover.None;
            return rendererEnabled ? VeilLeftover.DropStaleEntry : VeilLeftover.ReenableOrphan;
        }

        // 가드 한 번의 결정(투사체당 1회): 한 발사에 처리기가 여럿 불린다(모드 로드 때 등록한 전역 구독자 + 스탠드를 켠 귀검사마다 StandBuff 처리기, 순서는 엔진 몫).
        //   각 처리기는 가드를 먼저 부르고, 가림 컴포넌트를 붙이는 것은 StandBuff 처리기뿐이며 자기 가드 호출 뒤다 → 「이미 검사한 투사체는 Skip」이면
        //   어느 쪽이 먼저 불려도 방금 붙인 가림 컴포넌트를 흔적으로 훑지 않는다(시험이 두 순서를 모형으로 실행).
        public enum VeilGuardStep { Sweep, Skip }
        public static VeilGuardStep VeilGuardStepFor(bool alreadyGuarded) { return alreadyGuarded ? VeilGuardStep.Skip : VeilGuardStep.Sweep; }

        // 「장부에 있음」 = 그 인스턴스 ID 의 기록이 있고 기록된 렌더러가 바로 이 객체다. ID 만 같고 객체가 다르면(인스턴스 ID 재사용) 「없음」 — 켜지 않는다.
        public static bool VeilLedgerHit(bool idFound, bool sameObject)
        {
            if (sameObject && !idFound) throw new InvalidOperationException("SpiritRules.VeilLedgerHit: 'same object' without a ledger entry (broken reading)");
            return idFound && sameObject;
        }

        // 장부에 adding 개를 더 적을 자리가 있는가(상한 = Dials.VeilLedgerCapacity). 자리가 없으면 호출자는 숨기지 않는다(기록 없이 끄면 구분할 수 없다).
        public static bool VeilLedgerHasRoom(int count, int adding, int capacity)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count", count, "SpiritRules.VeilLedgerHasRoom: count must be >= 0");
            if (adding < 0) throw new ArgumentOutOfRangeException("adding", adding, "SpiritRules.VeilLedgerHasRoom: adding must be >= 0");
            if (capacity <= 0) throw new ArgumentOutOfRangeException("capacity", capacity, "SpiritRules.VeilLedgerHasRoom: capacity must be > 0");
            return (long)count + adding <= capacity;
        }

        // 발사된 View 에 이전 사용의 흔적이 있었는가: 복원되지 않은 가림 컴포넌트가 남아 있었거나, 장부의 렌더러를 되켜야 했다.
        public static bool VeilTraceFound(int unrestoredVeils, int reenabledRenderers)
        {
            if (unrestoredVeils < 0) throw new ArgumentOutOfRangeException("unrestoredVeils", unrestoredVeils, "SpiritRules.VeilTraceFound: count must be >= 0");
            if (reenabledRenderers < 0) throw new ArgumentOutOfRangeException("reenabledRenderers", reenabledRenderers, "SpiritRules.VeilTraceFound: count must be >= 0");
            return unrestoredVeils > 0 || reenabledRenderers > 0;
        }

        // (x, z) 에서 (tx, tz) 를 보는 방향(도). 두 점이 같으면 방향이 없다 → fallbackDeg(주인 방향).
        static double FacingToward(double x, double z, double tx, double tz, double fallbackDeg)
        {
            double dx = tx - x, dz = tz - z;
            if (dx == 0 && dz == 0) return fallbackDeg;
            return Math.Atan2(dx, dz) * 180.0 / Math.PI;
        }

        // 주먹이 명중했는데 주인의 령이 없다. 이 처리기는 스탠드 버프가 켜져(구독) 있을 때만 불리고, 공격한 주인은 의식이 있고 게임 안이며
        // 지역이 로드돼 있다 → Decide 의 다섯 입력 중 남는 「령이 없어도 되는」 경우는 엔진이 주인을 숨김(잠금 컷신·전역 지도) 하나뿐.
        // Yes = 정당한 부재(INFO), No = 있어야 할 령이 없음(오류), Unknown = 던짐.
        public static bool SpiritAbsenceExplained(Tri ownerHiddenByEngine)
        {
            return Known(ownerHiddenByEngine, "ownerHiddenByEngine");
        }

         
        // 처리기마다 넘기는 이벤트 상수는 시험이 IL 로 대조한다(주인/남 갈림 처리기는 두 상수).
        public enum LifecycleEvent { Activate, TurnOn, TurnOff, Deactivate, AreaBeginUnloading, AreaDidLoad, OwnerLifeStateChanged, OtherUnitLifeStateChanged, OwnerInGameChanged, OtherEntityInGameChanged, GameModeChanged }
        // Decide = 다섯 입력을 실제 상태에서 읽어 Decide. DecideNoSpawn = Decide 하되 Spawn 은 None 으로(끄는 쪽 이벤트는 만들지 않는다).
        // Remove = 입력 없이 지운다. SweepThenDecide = 표에 없는 령 청소 후 Decide. Ignore = 입력을 읽지 않는다.
        public enum LifecyclePlan { Decide, DecideNoSpawn, Remove, SweepThenDecide, Ignore }
        public enum SpiritEffect { Ensure, Remove, Nothing }

        // 근거(엔진 IL, dnlib 2026-11-09):
        //  - Activate/TurnOn: EntityFact.Activate IL 00AC set_IsActive(true) → 0121 컴포넌트 Activate → EntityFactComponent.Activate 000B OnActivate,
        //    0034 TurnOn → TurnOn 0019 Subscribe(EventBus) → 001F OnTurnOn. 켜짐은 재판정(지역·주인 상태가 아직 안 맞으면 None).
        //  - TurnOff = DecideNoSpawn(제거 아님): 저장할 때 엔진이 엔티티를 껐다 켠다 — SaveManager.SaveRoutine IL 0681 LoadedAreaState.TurnOff,
        //    06AF CrossSceneState.TurnOff(→ EntityDataBase.TurnOff 0030 Facts.TurnOff → 컴포넌트 OnTurnOff), 직렬화 뒤 08FB/090F/0934 TurnOn.
        //    TurnOff 를 제거로 두면 저장마다 령이 지워졌다 다시 생기고, 다시 생기는 090F 시점엔 지역 상태가 아직 꺼져 있다. 버프가 실제로 꺼지는 경로는
        //    Deactivate(EntityFactComponent.Deactivate 0012 TurnOff → 0019 OnDeactivate)라 그쪽이 제거를 맡는다. 끄는 길(저장 직전·엔티티 정리)에서
        //    새로 만들면 안 되므로 Spawn 은 None 으로 바꾼다(NoSpawn). 있는 령은 Keep(저장 뒤에도 그대로) / 조건이 깨졌으면 Despawn.
        //  - AreaBeginUnloading: Game.HandleAreaBeginUnloading IL 0069 RaiseEvent<IAreaHandler> → 지역 상태째 버려지기 전에 제거.
        //  - AreaDidLoad: Game.AfterSceneLoaderFinishedArea IL 015E RaiseEvent<IAreaHandler>(OnAreaDidLoad). 세이브 불러오기도 이 길이다(정찰 §2(f)).
        //  - 생명 상태: UnitLifeController.SetLifeState IL 0079 UpdateLifeState → 014B RaiseLifestateChanged(전역, 모든 유닛) → 주인 것만 재판정.
        //  - 게임 안: EntityDataBase.set_IsInGame IL 000B 값 대입 → 0036 RaiseEvent<IInGameHandler>(전역, 모든 엔티티) → 주인 것만 재판정.
        //  - 게임 모드: CutsceneLock.UpdateModeForCutsceneLock IL 0011~007C 잠금 값에 따라 StartMode(Cutscene) → IGameModeHandler. 모드 변화마다 재판정.
        public static LifecyclePlan EventPlan(LifecycleEvent e)
        {
            switch (e)
            {
                case LifecycleEvent.Activate: return LifecyclePlan.Decide;
                case LifecycleEvent.TurnOn: return LifecyclePlan.Decide;
                case LifecycleEvent.TurnOff: return LifecyclePlan.DecideNoSpawn;
                case LifecycleEvent.Deactivate: return LifecyclePlan.Remove;
                case LifecycleEvent.AreaBeginUnloading: return LifecyclePlan.Remove;
                case LifecycleEvent.AreaDidLoad: return LifecyclePlan.SweepThenDecide;
                case LifecycleEvent.OwnerLifeStateChanged: return LifecyclePlan.Decide;
                case LifecycleEvent.OtherUnitLifeStateChanged: return LifecyclePlan.Ignore;   // 정당한 무동작: 전역 이벤트의 다른 유닛
                case LifecycleEvent.OwnerInGameChanged: return LifecyclePlan.Decide;
                case LifecycleEvent.OtherEntityInGameChanged: return LifecyclePlan.Ignore;    // 정당한 무동작: 전역 이벤트의 다른 엔티티(령 자신 포함)
                case LifecycleEvent.GameModeChanged: return LifecyclePlan.Decide;
                default: throw new InvalidOperationException("SpiritRules.EventPlan: unknown lifecycle event " + (int)e);
            }
        }

        // 제거 계획: 있으면 Despawn, 없으면 None(없는 것을 지우는 것은 정당한 무동작).
        public static SpiritAction RemovalAction(bool spiritExists)
        {
            return spiritExists ? SpiritAction.Despawn : SpiritAction.None;
        }

        // DecideNoSpawn 계획: Spawn 만 None 으로 바꾸고 나머지는 그대로.
        public static SpiritAction NoSpawn(SpiritAction a)
        {
            switch (a)
            {
                case SpiritAction.Spawn: return SpiritAction.None;
                case SpiritAction.Keep:
                case SpiritAction.Despawn:
                case SpiritAction.None: return a;
                default: throw new InvalidOperationException("SpiritRules.NoSpawn: unknown action " + (int)a);
            }
        }

        // 판정 → 실행. Keep 도 Ensure 로 보낸다: Ensure 의 재사용 경로가 「살아 있는 령이 정확히 하나」를 매번 단언한다(이중 생성 검출).
        public static SpiritEffect Effect(SpiritAction a)
        {
            switch (a)
            {
                case SpiritAction.Spawn: return SpiritEffect.Ensure;
                case SpiritAction.Keep: return SpiritEffect.Ensure;
                case SpiritAction.Despawn: return SpiritEffect.Remove;
                case SpiritAction.None: return SpiritEffect.Nothing;
                default: throw new InvalidOperationException("SpiritRules.Effect: unknown action " + (int)a);
            }
        }

        // ---- 수정 1차(검토 I1·I2·M1·M3): 판정 조립·숨김 결합·청소 유지·저장 제외 단언을 순수 함수로. 제품 코드와 시험이 같은 함수를 부른다(C4). ----

        // 다섯 입력. 생성자로만 채운다(기본값 Tri.Yes=0 이 몰래 들어가지 않게 — 구조체 대신 클래스, 필드 readonly).
        public sealed class LifecycleInputs
        {
            public readonly Tri StandOn, OwnerConscious, OwnerInGame, AreaLoaded, OwnerHiddenByEngine;
            public LifecycleInputs(Tri standOn, Tri ownerConscious, Tri ownerInGame, Tri areaLoaded, Tri ownerHiddenByEngine)
            {
                StandOn = standOn; OwnerConscious = ownerConscious; OwnerInGame = ownerInGame; AreaLoaded = areaLoaded; OwnerHiddenByEngine = ownerHiddenByEngine;
            }
        }

        // StandSpiritLifecycle.Handle 의 판정 조립 전부(검토 I1). 순서:
        //   계획 → Ignore 면 아무것도 읽지 않고 Nothing → SweepThenDecide 면 청소 먼저 → 현재 령 읽기(null = 없음) →
        //   Remove 면 RemovalAction, 아니면 입력을 읽어 Decide(+ DecideNoSpawn 이면 NoSpawn) → Effect.
        // 게임 쪽은 위임만 넘긴다(청소 = StandSpirit.DestroyUntracked, 현재 령 = StandSpirit.Get, 입력 = 실제 상태 읽기).
        public static SpiritEffect Resolve(LifecycleEvent e, Action sweep, Func<object> currentSpirit, Func<LifecycleInputs> readInputs, out SpiritAction action)
        {
            if (sweep == null) throw new ArgumentNullException("sweep");
            if (currentSpirit == null) throw new ArgumentNullException("currentSpirit");
            if (readInputs == null) throw new ArgumentNullException("readInputs");
            var plan = EventPlan(e);
            if (plan == LifecyclePlan.Ignore) { action = SpiritAction.None; return SpiritEffect.Nothing; }
            if (plan == LifecyclePlan.SweepThenDecide) sweep();
            bool exists = currentSpirit() != null;
            if (plan == LifecyclePlan.Remove) action = RemovalAction(exists);
            else
            {
                var i = readInputs();
                if (i == null) throw new InvalidOperationException("SpiritRules.Resolve: readInputs returned null");
                action = Decide(i.StandOn, i.OwnerConscious, i.OwnerInGame, i.AreaLoaded, i.OwnerHiddenByEngine, exists);
                if (plan == LifecyclePlan.DecideNoSpawn) action = NoSpawn(action);
            }
            return Effect(action);
        }

        // 엔진이 본체를 숨김 = 조작 잠금 컷신 ∨ 지역 규칙이 파티를 숨김(전역 지도: BlueprintArea.get_HideParty IL 0000~0006 = IsGlobalMap,
        // SleepingUnitsController.ShouldBeSleeping IL 0046~005B 가 동료를 재움). 어느 쪽이든 Unknown 이면 Unknown(Yes 가 있어도 — 결측을 숨기지 않는다).
        public static Tri OwnerHiddenByEngine(Tri cutsceneLocked, Tri areaHidesParty)
        {
            if (cutsceneLocked == Tri.Unknown || areaHidesParty == Tri.Unknown) return Tri.Unknown;
            bool c = Known(cutsceneLocked, "cutsceneLocked");
            bool a = Known(areaHidesParty, "areaHidesParty");
            return c || a ? Tri.Yes : Tri.No;
        }

        // 청소(StandSpirit.DestroyUntracked) 유지 조건: 표에 있고 ∧ 지금 지역에 살아 있고 ∧ 주인이 폐기·파괴되지 않음(검토 M3:
        // EntityDataBase.Dispose 는 사실을 Deactivate/TurnOff 하지 않아 폐기된 주인의 령이 표에 남을 수 있다).
        public static bool KeepSpirit(bool tracked, bool liveInLoadedArea, bool ownerGone)
        {
            return tracked && liveInLoadedArea && !ownerGone;
        }

        // 저장 제외 실행 단언(검토 M1): 엔진 ShouldSerialize(령) 는 false, 대조군 ShouldSerialize(주인) 는 true 여야 한다.
        public static void RequireSaveExcluded(bool spiritSerialized, bool ownerSerialized)
        {
            if (spiritSerialized) throw new InvalidOperationException("engine would SAVE the spirit (ShouldSerialize == true): save exclusion broken");
            if (!ownerSerialized) throw new InvalidOperationException("control failed: ShouldSerialize(owner) == false, the save-exclusion probe cannot tell");
        }

         
        //   AddEntity IL 0061~0080 m_ToCreate.Add(대기열만). 등록은 다음 EntityCreationController.Tick IL 0026 AddEntityData(IL 006F HoldingState, 0087 OnAdded → Game.State.Units).
        //   Tick 은 게임 모드 컨트롤러라(GameModesFactory.Initialize IL 1325~136D, Pause 모드 없음) 일시정지 중엔 미뤄진다 → 시간 제한 단언은 쓰지 않는다.
        // 종전 Ensure 는 SpawnUnit 직후 Units 를 세어(0 개) 던지고 새 령을 지웠다. 또 다음 이벤트가 Tick 전에 오면 HoldingState 가 없어 생성 중인 령을 지우고 다시 만들었다.
        public enum TrackedSpirit { None, Reserved, Creating, Registered, Stale }

        // 주인 기록 하나의 상태. hasRecord = 주인 → 령 기록 있음, hasUnit = 기록에 유닛이 있음(없음 = SpawnUnit 진행 중 예약), going = 파괴·폐기·파괴 표시,
        // holdingStateSet = 유닛 HoldingState 있음(등록됨), holdingIsLoadedMain = 그것이 지금 불러온 지역의 주 상태, createdForLoadedMain = 생성 목표 상태(기록)가 지금 주 상태.
        // 입력끼리 어긋나면(기록 없는데 유닛, HoldingState 없는데 주 상태와 같음 등) 기록이 깨진 것 → 던진다.
        public static TrackedSpirit ClassifyTracked(bool hasRecord, bool hasUnit, bool going, bool holdingStateSet, bool holdingIsLoadedMain, bool createdForLoadedMain)
        {
            if (holdingIsLoadedMain && !holdingStateSet) throw new InvalidOperationException("SpiritRules.ClassifyTracked: holding state equals the loaded main state but is not set");
            if (!hasRecord)
            {
                if (hasUnit || going || holdingStateSet || createdForLoadedMain) throw new InvalidOperationException("SpiritRules.ClassifyTracked: unit facts without an owner record");
                return TrackedSpirit.None;
            }
            if (!hasUnit)
            {
                if (going || holdingStateSet) throw new InvalidOperationException("SpiritRules.ClassifyTracked: unit facts on a reservation without a unit");
                return TrackedSpirit.Reserved;
            }
            if (going) return TrackedSpirit.Stale;
            if (!holdingStateSet) return createdForLoadedMain ? TrackedSpirit.Creating : TrackedSpirit.Stale; // 대기열: 이 지역으로 생성 중 / 옛 지역으로 생성 중
            return holdingIsLoadedMain ? TrackedSpirit.Registered : TrackedSpirit.Stale;
        }

        // StandSpirit.Get: 생성 중도 「있음」(다음 이벤트가 생성 중인 령을 지우지 않게). 예약 중 = 같은 주인의 이벤트가 SpawnUnit 안에서 재진입 → 던진다.
        public static bool PresentForGet(TrackedSpirit s)
        {
            switch (s)
            {
                case TrackedSpirit.Creating: case TrackedSpirit.Registered: return true;
                case TrackedSpirit.None: case TrackedSpirit.Stale: return false;
                case TrackedSpirit.Reserved: throw new InvalidOperationException("SpiritRules: spirit lookup while its creation is in progress (reentrant lifecycle event)");
                default: throw new InvalidOperationException("SpiritRules.PresentForGet: unknown state " + (int)s);
            }
        }

        public enum EnsureStep { Create, ReplaceStale, ReuseCreating, ReuseRegistered }

        // StandSpirit.Ensure: 없음 → 생성, 낡음 → 지우고 생성, 생성 중 → 그대로(등록 전이라 세지 않음), 등록됨 → 그대로 + 「정확히 하나」 단언. 예약 중 = 재진입 → 던진다.
        public static EnsureStep EnsurePlan(TrackedSpirit s)
        {
            switch (s)
            {
                case TrackedSpirit.None: return EnsureStep.Create;
                case TrackedSpirit.Stale: return EnsureStep.ReplaceStale;
                case TrackedSpirit.Creating: return EnsureStep.ReuseCreating;
                case TrackedSpirit.Registered: return EnsureStep.ReuseRegistered;
                case TrackedSpirit.Reserved: throw new InvalidOperationException("SpiritRules: Ensure while the spirit's creation is in progress (reentrant lifecycle event)");
                default: throw new InvalidOperationException("SpiritRules.EnsurePlan: unknown state " + (int)s);
            }
        }

        public enum RegistrationCheck { Wait, AssertSingle, Done }

        // 등록 관찰(SpiritFollower.LateUpdate → StandSpirit.ConfirmRegistration). confirmed = 이 기록에서 이미 「정확히 하나」를 확인함.
        // spiritIsTracked = 따라가기의 령이 주인 기록의 령과 같음. 생성 중 → 대기(시간 제한 없음 — 일시정지), 등록됨 → 한 번 단언, 그 밖 = 추적이 깨짐 → 던진다.
        public static RegistrationCheck RegistrationStep(TrackedSpirit s, bool confirmed, bool spiritIsTracked)
        {
            if (!spiritIsTracked) throw new InvalidOperationException("SpiritRules: the follower's spirit is not the tracked spirit of its owner (state " + s + ")");
            if (confirmed) return RegistrationCheck.Done;
            switch (s)
            {
                case TrackedSpirit.Creating: return RegistrationCheck.Wait;
                case TrackedSpirit.Registered: return RegistrationCheck.AssertSingle;
                case TrackedSpirit.None: case TrackedSpirit.Reserved: case TrackedSpirit.Stale:
                    throw new InvalidOperationException("SpiritRules: spirit is " + s + " before its registration was confirmed");
                default: throw new InvalidOperationException("SpiritRules.RegistrationStep: unknown state " + (int)s);
            }
        }

        // 이 주인에게 묶인 살아 있는 령(Game.State.Units)이 정확히 하나이고 그것이 추적 중인 령인지(이중 생성 검출). 등록이 관찰된 뒤에만 부른다.
        public static void RequireExactlyOne(int liveCount, bool sawTracked, string owner)
        {
            if (liveCount != 1 || !sawTracked) throw new InvalidOperationException("PhantomBlade: owner " + owner + " has " + liveCount + " live spirit unit(s) in the loaded scene (expected exactly 1, tracked one present: " + sawTracked + ")");
        }

        // 결과 적용(검토 I1-r): Ensure → ensure() 1번, Remove → remove() 1번, Nothing → 아무것도 안 부름. 모르는 값·빠진 위임은 던진다.
        public static void Apply(SpiritEffect effect, Action ensure, Action remove)
        {
            if (ensure == null) throw new ArgumentNullException("ensure");
            if (remove == null) throw new ArgumentNullException("remove");
            switch (effect)
            {
                case SpiritEffect.Ensure: ensure(); return;
                case SpiritEffect.Remove: remove(); return;
                case SpiritEffect.Nothing: return;
                default: throw new InvalidOperationException("SpiritRules.Apply: unknown effect " + (int)effect);
            }
        }

         
        // 원인(IL): Player 진영 유닛의 인벤토리 = 파티 공용 인벤토리(UnitDescriptor..ctor IL 00B4~00D8: IsPlayerFaction 이면 Player.EnsureInventory()).
        //   손 무기 삽입 시 Player 진영은 장착 조건을 검사하고(ItemEntity.CanBeEquippedBy IL 0001~0016) 거부되면 UnitBody.TryInsertItem IL 0024·005B~0068 이
        //   그 아이템을 주인 인벤토리(= 파티)에 낱개로 넣는다("can't insert item" 로그 + "item found"). 령 생성마다 모델 손 무기 2개가 쌓였다.
        // 처방: 령 블루프린트의 Body 에서 아이템 블루프린트를 전부 뺀다(TryInsertItem IL 0000~0006: 블루프린트가 없으면 아무것도 만들지 않음) +
        //   생성 직후 령에게 자기 인벤토리(EnsureOwnInventory) + 파티 인벤토리 전후 대조(RequireNoInventoryLeak).

        public enum BodyField { Keep, ClearItem }

        // BlueprintUnit.UnitBody 의 필드별 처리. 표에 없는 이름은 던진다: 게임이 장비 칸을 늘리면 조용히 아이템이 새어 나가지 않고 빌드가 멈춘다(C2).
        // 필드 목록 출처 = Assembly-CSharp Kingmaker.Blueprints.BlueprintUnit/UnitBody 인스턴스 필드 26개(dnlib 2026-11-14) — 시험이 같은 DLL 과 대조한다(C4).
        public static BodyField BodyFieldPlan(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentException("SpiritRules.BodyFieldPlan: empty field name", "fieldName");
            switch (fieldName)
            {
                // 아이템이 아닌 설정값.
                case "DisableHands":
                case "ActiveHandSet":
                // 맨손 무기: UnitBody.SetEmptyHandWeapon IL 0038 이 ItemsEntityFactory.CreateEntity 로 만들 뿐 어떤 인벤토리에도 넣지 않는다(같은 메서드에 ItemsCollection.Add 없음).
                //   손이 비면 WeaponSlot.get_MaybeWeapon IL 0009~001B 가 이것을 돌려준다 = 령 휘두름 동작 스타일의 원천(StandSpiritAnimator.MainHandStyle).
                case "m_EmptyHandWeapon":
                    return BodyField.Keep;
                // 손(주·보조 + 대체 세트 3벌) — 장착이 거부되면 파티 인벤토리로 간다(위 원인).
                case "m_PrimaryHand":
                case "m_SecondaryHand":
                case "m_PrimaryHandAlternative1":
                case "m_SecondaryHandAlternative1":
                case "m_PrimaryHandAlternative2":
                case "m_SecondaryHandAlternative2":
                case "m_PrimaryHandAlternative3":
                case "m_SecondaryHandAlternative3":
                // 추가 사지·장비 칸·빠른 칸 — UnitBody.Initialize IL 0063~0227 이 같은 TryInsertItem 으로 넣는다(같은 거부 경로).
                case "m_AdditionalLimbs":
                case "m_AdditionalSecondaryLimbs":
                case "m_Armor":
                case "m_Shirt":
                case "m_Belt":
                case "m_Head":
                case "m_Glasses":
                case "m_Feet":
                case "m_Gloves":
                case "m_Neck":
                case "m_Ring1":
                case "m_Ring2":
                case "m_Wrist":
                case "m_Shoulders":
                case "m_QuickSlots":
                    return BodyField.ClearItem;
                default:
                    throw new InvalidOperationException("SpiritRules.BodyFieldPlan: unknown BlueprintUnit.UnitBody field '" + fieldName + "' — classify it (Keep / ClearItem) after checking whether UnitBody.Initialize creates an item from it");
            }
        }

        // 령 생성 전후 대조. 하나라도 어긋나면 던진다 → StandSpirit.Ensure 의 catch 가 령을 지우고 원인과 함께 다시 던진다(로그 오류, 조용한 계속 금지).
        //   partyEntries* = 파티 인벤토리 항목 수(ItemsCollection.Items.Count), partyTotal* = 그 수량 합(ItemEntity.Count).
        //   spiritSharesParty = 령의 인벤토리 객체가 파티 인벤토리와 같은가(참조 비교). spiritEntries = 령 자기 인벤토리의 항목 수(장착품 포함 — ItemSlot.InsertItem IL 006A~007B 가 장착품도 주인 인벤토리에 넣는다).
        //   령은 시각 전용이라 아이템을 하나도 갖지 않아야 한다.
        public static void RequireNoInventoryLeak(int partyEntriesBefore, int partyTotalBefore, int partyEntriesAfter, int partyTotalAfter, bool spiritSharesParty, int spiritEntries)
        {
            RequireCount(partyEntriesBefore, "partyEntriesBefore");
            RequireCount(partyTotalBefore, "partyTotalBefore");
            RequireCount(partyEntriesAfter, "partyEntriesAfter");
            RequireCount(partyTotalAfter, "partyTotalAfter");
            RequireCount(spiritEntries, "spiritEntries");
            if (spiritSharesParty) throw new InvalidOperationException("PhantomBlade: spirit inventory is the party inventory (EnsureOwnInventory did not isolate it) — anything the spirit gets or loses would change the party's items");
            if (partyEntriesAfter != partyEntriesBefore || partyTotalAfter != partyTotalBefore)
                throw new InvalidOperationException("PhantomBlade: spirit creation changed the party inventory: entries " + partyEntriesBefore + " -> " + partyEntriesAfter + ", total count " + partyTotalBefore + " -> " + partyTotalAfter);
            if (spiritEntries != 0) throw new InvalidOperationException("PhantomBlade: spirit owns " + spiritEntries + " item(s) after creation, expected 0 (visual-only unit; an item source is still on its blueprint)");
        }

        static void RequireCount(int v, string name)
        {
            if (v < 0) throw new ArgumentOutOfRangeException(name, v, "SpiritRules: " + name + " must be >= 0");
        }
    }

    // [순수] 령 외형(디졸브·걷기 복사) 실패의 계수 장부. 게임 쪽(SpiritVisualErrors)은 출구로 ModContext.LogError 만 붙인다 → 오류 줄은 S11 포착이 센다.
    //   매 프레임 경로라 같은 (where, key) 는 한 번만 오류 줄(Count +1), 그 뒤는 SuppressedRepeats 만 센다(사라지지 않되 도배하지 않음). 보고했으면 true.
    //   (선례: BulwarkMage ErrorLedger.ReportOnce — 같은 계약.)
    public sealed class CountedReporter
    {
        readonly Action<string> sink;
        readonly System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>();
        public int Count { get; private set; }
        public int SuppressedRepeats { get; private set; }

        public CountedReporter(Action<string> sink)
        {
            if (sink == null) throw new ArgumentNullException("sink");
            this.sink = sink;
        }

        public bool ReportOnce(string where, string key, string what)
        {
            if (string.IsNullOrEmpty(where)) throw new ArgumentException("PhantomBlade: ReportOnce without a location", "where");
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("PhantomBlade: ReportOnce without a dedupe key", "key");
            if (!seen.Add(where + "|" + key)) { SuppressedRepeats++; return false; }
            Count++;
            sink("PhantomBlade: ERROR " + where + ": " + what);
            return true;
        }
    }
}
