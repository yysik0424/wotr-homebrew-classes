using System;
using System.Collections.Generic;
using System.Linq;

namespace BlastMagus
{
     
    // 게임 형식 없음 — 콘솔 시험이 컴파일해 실행한다(modinfo.json testSources). 원소 → 자식 토글·버프 GUID·이름 키의 단일 원장(C4):
    // Energize.Build(자식 생성·부모 변형 목록), BlastBlueprints(원소 특성의 AddFacts), BlastContract.CheckOurFeature(우리 특성 계약)가 전부 이 표에서 읽는다.
    // GUID 출처 = Guids.cs 2026-11-15 발급분(uuid4). 행 순서 = Element enum 순서(부모 변형 목록의 순서).
    public sealed class EnergizeRow
    {
        public readonly Element Id;
        public readonly string ToggleGuid, BuffGuid, NameKey;
        public EnergizeRow(Element id, string toggleGuid, string buffGuid, string nameKey) { Id = id; ToggleGuid = toggleGuid; BuffGuid = buffGuid; NameKey = nameKey; }
    }

    // 자식 토글이 붙을 때의 기본 켜짐 판정(EnergizeDefaultChoice 가 따른다). 예전 단일 토글의 동작(원소 1개 = 기본 켜짐, 2개 이상 = 판정 불가 → 주입 없음)과 같게:
    // 붙은 뒤 유닛의 자식 토글이 정확히 1개 = 그것을 켠다, 2개 이상 = 전부 끈다(조용히 한 원소를 고르지 않는다 — 플레이어가 하나를 고른다).
    public enum EnergizeDefault { TurnOnThis, TurnAllOff }

    public static class EnergizeTable
    {
        static readonly EnergizeRow[] rows =
        {
            new EnergizeRow(Element.Fire, Guids.EnergizeToggleFire, Guids.EnergizeBuffFire, "EnergizeFire.Name"),
            new EnergizeRow(Element.Cold, Guids.EnergizeToggleCold, Guids.EnergizeBuffCold, "EnergizeCold.Name"),
            new EnergizeRow(Element.Electric, Guids.EnergizeToggleElectric, Guids.EnergizeBuffElectric, "EnergizeElectric.Name"),
            new EnergizeRow(Element.Air, Guids.EnergizeToggleAir, Guids.EnergizeBuffAir, "EnergizeAir.Name"),
            new EnergizeRow(Element.Earth, Guids.EnergizeToggleEarth, Guids.EnergizeBuffEarth, "EnergizeEarth.Name"),
            new EnergizeRow(Element.Water, Guids.EnergizeToggleWater, Guids.EnergizeBuffWater, "EnergizeWater.Name"),
            new EnergizeRow(Element.Telekinetic, Guids.EnergizeToggleTelekinetic, Guids.EnergizeBuffTelekinetic, "EnergizeTelekinetic.Name"),
            new EnergizeRow(Element.Gravity, Guids.EnergizeToggleGravity, Guids.EnergizeBuffGravity, "EnergizeGravity.Name"),
            new EnergizeRow(Element.Wood, Guids.EnergizeToggleWood, Guids.EnergizeBuffWood, "EnergizeWood.Name"),
            new EnergizeRow(Element.Negative, Guids.EnergizeToggleNegative, Guids.EnergizeBuffNegative, "EnergizeNegative.Name"),
            new EnergizeRow(Element.Positive, Guids.EnergizeTogglePositive, Guids.EnergizeBuffPositive, "EnergizePositive.Name"),
        };

        // 정적 검사 실패 = 형식 초기화 실패(TypeInitializationException) — ElementTable 과 같은 규칙. 빌드 전체가 시끄럽게 멈춘다.
        static EnergizeTable() { Validate(rows); }

        public static EnergizeRow[] All { get { return rows.ToArray(); } }

        public static EnergizeRow Of(Element e)
        {
            foreach (var r in rows) if (r.Id == e) return r;
            throw new ArgumentOutOfRangeException("e", e, "BlastMagus: no energize row for element " + e);
        }

        // 표 규칙: Element 값마다 정확히 1행(enum 순서), GUID 형식 32자 소문자 16진, 토글·버프 GUID 22개 전부 서로 다름, 원소 표(ElementTable)의 GUID 와 겹치지 않음, 이름 키 비어 있지 않음.
        public static void Validate(IEnumerable<EnergizeRow> rowSet)
        {
            if (rowSet == null) throw new ArgumentNullException("rowSet");
            var list = rowSet.ToList();
            var bad = new List<string>();
            if (list.Any(r => r == null)) bad.Add("null row");
            var ok = list.Where(r => r != null).ToList();
            var values = (Element[])Enum.GetValues(typeof(Element));
            if (!ok.Select(r => r.Id).SequenceEqual(values)) bad.Add("row ids [" + string.Join(",", ok.Select(r => r.Id)) + "] != Element enum order [" + string.Join(",", values) + "]");
            var guids = ok.SelectMany(r => new[] { r.ToggleGuid, r.BuffGuid }).ToList();
            foreach (var g in guids) if (!IsGuidN(g)) bad.Add("GUID '" + g + "' is not a 32-char lowercase hex GUID");
            foreach (var g in guids.Where(x => x != null).GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1)) bad.Add("GUID " + g.Key + " used " + g.Count() + " times");
            var elementGuids = new HashSet<string>(ElementTable.All.SelectMany(r => ElementTable.GuidFields.Select(f => ElementTable.GuidOf(r, f))), StringComparer.Ordinal);
            foreach (var g in guids.Where(x => x != null && elementGuids.Contains(x))) bad.Add("GUID " + g + " is also an element table GUID");
            foreach (var r in ok.Where(r => string.IsNullOrEmpty(r.NameKey))) bad.Add("row " + r.Id + " has no name key");
            if (bad.Count > 0) throw new ArgumentException("BlastMagus: energize table invalid: " + string.Join("; ", bad), "rowSet");
        }

        // 자식 토글이 붙은 뒤(그것을 포함한) 유닛의 자식 토글 수 → 기본 동작. 1 미만은 배선 결함(붙은 자기 자신조차 세지 못했다) → 던진다.
        public static EnergizeDefault OnGrant(int ownedChildren)
        {
            if (ownedChildren < 1) throw new ArgumentOutOfRangeException("ownedChildren", ownedChildren, "BlastMagus: energize child granted but the unit owns " + ownedChildren + " energize child toggles (the granted one was not counted)");
            return ownedChildren == 1 ? EnergizeDefault.TurnOnThis : EnergizeDefault.TurnAllOff;
        }

        // GUID 형식 = BlueprintGuid.ToString() "N"(ElementTable.IsGuidN 과 같은 근거: CorePlans.cs ArcanaFilter).
        const int GuidLength = 32;
        static bool IsGuidN(string g)
        {
            if (g == null || g.Length != GuidLength) return false; // 형식 판정 결과(결측 아님): 호출자가 위반으로 보고한다
            foreach (var c in g) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
    }
}
