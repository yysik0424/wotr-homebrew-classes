using System;
using Kingmaker.BundlesLoading;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using static WotrClassMod.Common.BlueprintUtil;

namespace PhantomBlade
{
    // FxTable(순수 원장) 한 행 → 새 PrefabLink. 본편 원본 블루프린트를 읽어 대조한다 — 지어낸 링크 금지(SpiritBlueprints.BuildTranslucentFx 선례).
    // 시끄럽게 실패(C1·C2): 원본 없음(Get 이 던짐) · 원본 슬롯 null/빈 값 · 값이 표의 ExpectedAssetId 와 다름 · 번들 위치 목록에 없음 → 던진다(빌드 실패).
    // 본편 객체에는 쓰지 않는다(읽기만). 돌려주는 링크는 원본과 다른 인스턴스.
    public static class Fx
    {
        public static PrefabLink Link(FxRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            string got;
            switch (row.SourceSlot)
            {
                case FxSlot.BuffFxOnStart:
                    {
                        var src = Get<BlueprintBuff>(row.SourceGuid);
                        if (src.name != row.SourceName) throw new InvalidOperationException("PhantomBlade: FX source " + row.Key + " " + row.SourceGuid + " is '" + src.name + "', FxTable says '" + row.SourceName + "'");
                        got = src.FxOnStart == null ? null : src.FxOnStart.AssetId;
                        break;
                    }
                default: throw new InvalidOperationException("PhantomBlade: FX source slot " + row.SourceSlot + " (" + row.Key + ") has no reader");
            }
            if (string.IsNullOrEmpty(got)) throw new InvalidOperationException("PhantomBlade: FX source " + row.SourceName + " (" + row.SourceGuid + ") " + row.SourceSlot + " is empty; FxTable " + row.Key + " expects " + row.ExpectedAssetId);
            if (got != row.ExpectedAssetId) throw new InvalidOperationException("PhantomBlade: FX source " + row.SourceName + " " + row.SourceSlot + " is " + got + ", FxTable " + row.Key + " expects " + row.ExpectedAssetId);
            var bundles = BundlesLoadService.Instance;
            if (bundles == null) throw new InvalidOperationException("PhantomBlade: BundlesLoadService.Instance is null at blueprint build; cannot verify FX prefab " + got + " (" + row.Key + ")");
            if (!bundles.HasLocation(got)) throw new InvalidOperationException("PhantomBlade: FX prefab " + got + " (" + row.Key + ") is not in the bundle location list");
            return new PrefabLink { AssetId = got };
        }
    }
}
