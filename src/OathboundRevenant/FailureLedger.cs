using System;
using WotrClassMod.Common;

namespace OathboundRevenant
{
    // [순수] 실패 전파 장부(계획 Global Constraints 「실패 전파」 #13). 문맥 키(시전 문맥 객체 등) → 처음 실패한 사이트.
    // 약한 키(WeakReference) — 문맥이 수집되면 기록도 사라진다(누수 없음, exec: failure ledger).
    // 같은 문맥의 뒤 부품은 Failed(key) != null 이면 추가 오류 없이 멈춘다(ChannelHealRule.StopSilently 등).
    public static class FailureLedger
    {
        sealed class Box { public string Site; }
        static readonly WeakKeyTable<object, Box> table = new WeakKeyTable<object, Box>();

        // 처음 실패한 사이트만 남긴다(두 번째 Mark 는 무시 — 원인은 첫 실패).
        public static void Mark(object key, string site)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (string.IsNullOrEmpty(site)) throw new ArgumentException("OathboundRevenant: FailureLedger.Mark without a site", "site");
            Box b;
            if (table.TryGetValue(key, out b)) return;
            table.Add(key, new Box { Site = site });
        }

        public static string Failed(object key)
        {
            if (key == null) throw new ArgumentNullException("key");
            Box b;
            return table.TryGetValue(key, out b) ? b.Site : null;
        }
    }
}
