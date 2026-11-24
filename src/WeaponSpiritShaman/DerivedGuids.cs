using System;
using System.Security.Cryptography;
using System.Text;

namespace WeaponSpiritShaman
{
    // [순수] 범주별 파생 GUID(계획 T2 「DerivedGuids」, Global Constraints). GUID = MD5(이름공간 + "|" + kind + "|" + category) 의 32자 소문자 hex.
    // 이름공간 문자열은 Guids.Derive.Namespace 한 곳 — 커밋 뒤 변경 금지(세이브 호환). 시험 `exec: derived guids` 가 고정 3개 스냅샷으로 잠근다.
    public static class DerivedGuids
    {
        // kind: weapon(시작 무기) · weapon2(이중 무기 두 번째 머리) · unit(영병 유닛) · feature(영병 특성). 59×3 + 이중 4.
        // 성장 특기(소유자 결정 2026-11-18): grant-<단계>(주술사 쪽 숨김 특성, AddFeatureToPet) · petfeat-<단계>(펫 쪽 숨김 특성, AddParametrizedFeatures) — 단계 = SpiritRules.SpiritGrowthFeats 의 Key. 59×6.
        // 시험 `feat tiers:` 가 단계마다 두 종류가 이 목록에 있음을 확인한다(목록은 리터럴 — For 가 모르는 종류를 거부하는 원장이다).
        public static readonly string[] Kinds = { "weapon", "weapon2", "unit", "feature", "grant-spec", "grant-gwf", "grant-gws", "petfeat-spec", "petfeat-gwf", "petfeat-gws" };

        public static string For(string kind, string category)
        {
            if (string.IsNullOrEmpty(kind)) throw new ArgumentException("DerivedGuids: kind is empty");
            if (string.IsNullOrEmpty(category)) throw new ArgumentException("DerivedGuids: category is empty");
            if (Array.IndexOf(Kinds, kind) < 0) throw new ArgumentException("DerivedGuids: unknown kind '" + kind + "'");
            if (category.IndexOf('|') >= 0) throw new ArgumentException("DerivedGuids: category contains the separator");
            using (var md5 = MD5.Create())
            {
                var h = md5.ComputeHash(Encoding.UTF8.GetBytes(Guids.Derive.Namespace + "|" + kind + "|" + category));
                var sb = new StringBuilder(32);
                foreach (var b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
