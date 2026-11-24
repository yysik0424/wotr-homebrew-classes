using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponSpiritShaman
{
     
    // 게임 쪽(T3)은 BlueprintWeaponType·BlueprintItemWeapon 을, 시험은 blueprints.zip jbp 를 같은 행으로 만든다:
    //   TypeRow: Category = 유형의 Category, Path = "Weapons/Types/" 아래 상대 경로(확장자 없음), IsNatural = m_IsNatural,
    //            HasModel = m_VisualParameters.m_WeaponModel.AssetId 가 비어 있지 않음.
    //   ItemRow: Name = 블루프린트 이름, TypeGuid = m_Type, Double = Double, SecondGuid = m_SecondWeapon(없으면 null), HasEnchantments = m_Enchantments 가 비어 있지 않음.
    public sealed class TypeRow
    {
        public string Category, TypeGuid, Path;
        public bool IsNatural, HasModel;
    }

    public sealed class ItemRow
    {
        public string Guid, Name, TypeGuid, SecondGuid;
        public bool Double, HasEnchantments;
    }

    public sealed class CategoryChoice
    {
        public CategoryRow[] Rows;               // 범주 이름순
        public string[] ExcludedNatural;         // 유형이 전부 자연 무기인 범주
        public string[] ExcludedNoModel;         // 자연 전용은 아니지만 「비자연 + 모델 있음」 유형이 없는 범주
    }

    public static class CategoryPlan
    {
        public const string StandardPrefix = "Standard";   // 본편 기준 아이템 이름 규칙(StandardLongsword 등, jbp 전수 — 정찰 r1 §4.4)
        public const string SpecialPathPrefix = "Special/"; // 특수 유형 폴더(횃불·사자 발톱 등) — 기준 유형 후보에서 뒤로

        public static CategoryChoice Choose(TypeRow[] types, ItemRow[] items)
        {
            if (types == null) throw new ArgumentNullException("types");
            if (items == null) throw new ArgumentNullException("items");
            foreach (var t in types)
                if (t == null || string.IsNullOrEmpty(t.Category) || string.IsNullOrEmpty(t.TypeGuid) || string.IsNullOrEmpty(t.Path)) throw new ArgumentException("CategoryPlan: incomplete type row");
            foreach (var i in items)
                if (i == null || string.IsNullOrEmpty(i.Guid) || string.IsNullOrEmpty(i.Name) || i.TypeGuid == null) throw new ArgumentException("CategoryPlan: incomplete item row");
            if (types.Select(t => t.TypeGuid).Distinct().Count() != types.Length) throw new ArgumentException("CategoryPlan: duplicate type guid");
            if (items.Select(i => i.Guid).Distinct().Count() != items.Length) throw new ArgumentException("CategoryPlan: duplicate item guid");
            var itemById = items.ToDictionary(i => i.Guid);

            var rows = new List<CategoryRow>();
            var natural = new List<string>();
            var noModel = new List<string>();
            foreach (var g in types.GroupBy(t => t.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                if (g.All(t => t.IsNatural)) { natural.Add(g.Key); continue; }
                var cand = g.Where(t => !t.IsNatural && t.HasModel).OrderBy(t => t.Path, StringComparer.Ordinal).ToList();
                if (cand.Count == 0) { noModel.Add(g.Key); continue; }
                // 기준 유형 = 경로 이름 == 범주 우선, 없으면 Special/ 아닌 첫 유형(경로 사전순).
                var same = cand.Where(t => LastSegment(t.Path) == g.Key).ToList();
                if (same.Count > 1) throw new InvalidOperationException("CategoryPlan: " + g.Key + " has " + same.Count + " types named like the category");
                var baseType = same.Count == 1 ? same[0] : cand.FirstOrDefault(t => !t.Path.StartsWith(SpecialPathPrefix, StringComparison.Ordinal));
                if (baseType == null) throw new InvalidOperationException("CategoryPlan: " + g.Key + " has only Special/ types");

                // 기준 아이템 = 그 유형의 마법 부여 없는 Standard* — Double 이 있으면 그것(F09), 없으면 예외표, 둘 다 없으면 throw. 한 단계에 2개 이상이면 모호 → throw.
                var std = items.Where(i => i.TypeGuid == baseType.TypeGuid && i.Name.StartsWith(StandardPrefix, StringComparison.Ordinal) && !i.HasEnchantments).ToList();
                var dbl = std.Where(i => i.Double).ToList();
                ItemRow pick;
                BaseItemSource src;
                string over;
                if (dbl.Count > 1) throw new InvalidOperationException("CategoryPlan: " + g.Key + " has " + dbl.Count + " double Standard items (" + string.Join(",", dbl.Select(i => i.Name)) + ")");
                if (dbl.Count == 1) { pick = dbl[0]; src = BaseItemSource.StandardDouble; }
                else if (std.Count > 1) throw new InvalidOperationException("CategoryPlan: " + g.Key + " has " + std.Count + " Standard items (" + string.Join(",", std.Select(i => i.Name)) + ")");
                else if (std.Count == 1) { pick = std[0]; src = BaseItemSource.Standard; }
                else if (SpiritRules.CategoryBaseItemOverrides.TryGetValue(g.Key, out over))
                {
                    if (!itemById.TryGetValue(over, out pick)) throw new InvalidOperationException("CategoryPlan: override item " + over + " for " + g.Key + " not found");
                    if (pick.TypeGuid != baseType.TypeGuid) throw new InvalidOperationException("CategoryPlan: override item " + pick.Name + " is not of base type " + baseType.Path);
                    src = BaseItemSource.Override;
                }
                else throw new InvalidOperationException("CategoryPlan: " + g.Key + " has no Standard item for " + baseType.Path + " and no override");
                if (pick.Double && string.IsNullOrEmpty(pick.SecondGuid)) throw new InvalidOperationException("CategoryPlan: double item " + pick.Name + " has no second head");
                if (pick.Double && !itemById.ContainsKey(pick.SecondGuid)) throw new InvalidOperationException("CategoryPlan: second head " + pick.SecondGuid + " of " + pick.Name + " not found");
                string secondType = pick.Double ? itemById[pick.SecondGuid].TypeGuid : null;
                if (pick.Double && string.IsNullOrEmpty(secondType)) throw new InvalidOperationException("CategoryPlan: second head " + pick.SecondGuid + " of " + pick.Name + " has no m_Type");
                rows.Add(new CategoryRow
                {
                    Category = g.Key, TypeGuid = baseType.TypeGuid, TypePath = baseType.Path, ItemGuid = pick.Guid, ItemName = pick.Name,
                    Double = pick.Double, SecondGuid = pick.Double ? pick.SecondGuid : null, SecondTypeGuid = secondType, Source = src,
                });
            }
            return new CategoryChoice { Rows = rows.ToArray(), ExcludedNatural = natural.ToArray(), ExcludedNoModel = noModel.ToArray() };
        }

        static string LastSegment(string path)
        {
            int k = path.LastIndexOf('/');
            return k < 0 ? path : path.Substring(k + 1);
        }
    }
}
