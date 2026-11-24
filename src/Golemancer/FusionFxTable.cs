using System;
using System.Collections.Generic;
using System.Linq;

namespace Golemancer
{
    // [순수] 합체 변신 FX 원장(2026-11-15 사용자 판정 「골렘술사 합체 변신은 원소정령 변신을 사용하자」).
    //   결함: 합체 형태 버프 13 의 Polymorph 전환(m_EnterTransition/m_ExitTransition)이 빈 VisualTransitionSettings 라 변신 FX 가 없었다 — 예외가 없어 아무도 몰랐다.
    //   본편 원소 신체(ElementalBody I~IV × 4원소 = 16 버프, blueprints.zip 2026-11-15 직독)는 인라인 전환이 비어 있고(Old/NewPrefabFX 없음), FxOnStart/FxOnRemove 도 null,
    //   변신 FX 는 전부 외부 전환 에셋 m_TransitionExternal(Unity ScriptableObject PolymorphTransitionSettings, guid 5687f680…, fileid 11400000)에 있다.
    //   엔진: Polymorph.TryReplaceView @0188 / RestoreView @0110 이 get_HasExternalTransition(= m_TransitionExternal 이 살아 있음)이면 외부 에셋의 EnterTransition/ExitTransition 을 쓰고
    //   인라인 m_EnterTransition/m_ExitTransition 은 읽지 않는다(Assembly-CSharp dnlib 2026-11-15).
    // 원천 선택: 흙 원소 신체 IV(ElementalBodyIVEarthBuff) — 골렘은 흙·돌로 빚은 구조물이라 흙 원소가 가장 가깝다. 16 버프 모두 같은 외부 에셋을 쓴다(시험 fusionfx: jbp 가 확인)
    //   — 즉 원소를 바꿔도 보이는 FX 는 같다. 이 에셋은 용 형태(Bloodrager Form of the Dragon) 등 본편 변신 버프 139 개가 공유한다(원소 전용 FX 는 아님).
    // 조용한 통과 금지(C1·C2): 게임 쪽 Fusion.ElementalTransition 이 원천 버프·Polymorph 1개·외부 에셋·Enter/Exit 설정·각 PrefabFX AssetId 를 확인하고 하나라도 없으면 던진다(BuildFailed).
    public sealed class FusionFxRow
    {
        public readonly string Slot, Reason;
        public FusionFxRow(string slot, string reason) { Slot = slot; Reason = reason; }
    }

    public static class FusionFxTable
    {
        // 원천(유일한 원장). 값 = blueprints.zip 2026-11-15 직독(시험 fusionfx: jbp 가 다시 확인).
        public const string SourceKey = "ElementalBodyIVEarth";
        public const string SourceGuid = "3c7c12df25d21b344b7cbe12a60038d8";
        public const string SourceName = "ElementalBodyIVEarthBuff";
        public const string SourceJbp = "Spells/Level7/ElementalBodyIVEarthBuff.jbp";
        public const string ExternalAssetGuid = "5687f6801ca6ea848a8d48298d892a8e"; // 원천 Polymorph.m_TransitionExternal.guid
        public const string ExternalAssetFileId = "11400000";                     // 같은 참조의 fileid(ScriptableObject 주 객체). 문자열 = 수치 다이얼 단일 블록 시험 밖(조율값이 아니라 식별자)
        public const string ElementalBodyNamePrefix = "ElementalBody"; // 원소 신체 16 버프 파일 이름 접두(…Buff.jbp, Spells/Level4~7) — 시험이 전부 같은 외부 에셋인지 센다

        // 우리 합체 형태 버프에 넣는 칸. 값의 출처 = 원천 버프 Polymorph 의 같은 칸(참조 공유 — 본편 객체에는 쓰지 않는다).
        public const string TransitionField = "m_TransitionExternal";
        public static readonly IReadOnlyList<FusionFxRow> Copied = new[]
        {
            new FusionFxRow(TransitionField, "원소 신체 변신 FX 는 전부 이 외부 전환 에셋에 있다 — 엔진이 이것이 있으면 이것만 쓴다(TryReplaceView @0188, RestoreView @0110)"),
        };

         
        public static readonly IReadOnlyList<FusionFxRow> Users = new[]
        {
            new FusionFxRow("Fusion::BuildForm", "합체 형태 버프 13(주인) — 합체 시작/해제"),
            new FusionFxRow("Materials::BuildBuff","재질 버프 13(골렘) — 재질·단계(키 @T) 교체, 사실 빠짐 재적용 때 Reconcile 이 버프를 바꾸면 재생(로드·생성은 즉시 교체라 없음)"),
        };

        // 외부 에셋 안에서 비어 있으면 안 되는 칸(빌드 때 게임 쪽이 실제 객체로 확인, 비면 던진다).
        public static readonly IReadOnlyList<FusionFxRow> RequiredInside = new[]
        {
            new FusionFxRow("EnterTransition/OldPrefabFX", "합체 시작 — 사라지는 원래 모습 쪽 FX"),
            new FusionFxRow("EnterTransition/NewPrefabFX", "합체 시작 — 나타나는 골렘 모습 쪽 FX"),
            new FusionFxRow("ExitTransition/OldPrefabFX", "합체 해제 — 사라지는 골렘 모습 쪽 FX"),
            new FusionFxRow("ExitTransition/NewPrefabFX", "합체 해제 — 돌아오는 원래 모습 쪽 FX"),
        };

        // 원천에 없어서 우리도 넣지 않는 칸(원천 jbp 에서 비어 있음을 시험이 확인 — 「복사 안 함」이 거짓이면 빨개진다).
        public static readonly IReadOnlyList<FusionFxRow> AbsentInSource = new[]
        {
            new FusionFxRow("FxOnStart", "원천 버프 FxOnStart = null"),
            new FusionFxRow("FxOnRemove", "원천 버프 FxOnRemove = null"),
            new FusionFxRow("m_EnterTransition.PrefabFX", "원천 인라인 전환 FX 없음(외부 에셋이 있으면 엔진이 읽지도 않는다)"),
            new FusionFxRow("m_ExitTransition.PrefabFX", "원천 인라인 전환 FX 없음"),
        };

        // 외부 에셋에서 읽은 4칸(RequiredInside 순서)의 AssetId 를 받아 문제 목록을 낸다. 빈 목록 = 통과. null 배열·개수 불일치도 문제다(판정 불가 ≠ 통과).
        public static List<string> InsideProblems(string[] assetIds)
        {
            var p = new List<string>();
            if (assetIds == null) { p.Add("no asset ids (null)"); return p; }
            if (assetIds.Length != RequiredInside.Count) { p.Add("expected " + RequiredInside.Count + " asset ids, got " + assetIds.Length); return p; }
            for (int i = 0; i < assetIds.Length; i++)
                if (string.IsNullOrEmpty(assetIds[i])) p.Add(RequiredInside[i].Slot + " is empty (" + RequiredInside[i].Reason + ")");
            return p;
        }
    }
}
