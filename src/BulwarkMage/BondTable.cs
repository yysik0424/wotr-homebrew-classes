using System;
using System.Collections.Generic;
using System.Linq;

namespace BulwarkMage
{
     
    //   강화 = 레벨 5·10·15·20(BulwarkRules.Levels.BondEnhancementLevels)마다 특성 하나 → 그 특성의 버프가 본편 ArmorEnhancementBonus{n} 을 방패 방어구 구성요소에 붙인다(층 n = +n).
    //   층이 함께 있어도 강화 종류가 같아 엔진은 큰 값 하나를 쓴다(GameHelper.GetItemEnhancementBonus @0054~0097 = ArmorEnhancementBonus.EnhancementValue 의 최댓값) — Effective.
    //   단 층마다 그 마법부여의 AdvanceArmorStats(ACP +1)는 따로 더해진다(AdvanceArmorStats.OnEventAboutToTrigger @0011~0016) — AcpShiftFromBond(보고 미결 항목, 큰 값 규칙이 ACP 에는 없다).
    //   A2b(2026-11-14): 층 n 버프의 자체 컴포넌트 BondOffHandEnchant 가 우리 복사본 BondEnchant{n} 을 「지금 보조손의 타워실드」 하나에만 붙인다(배치 판정 = BondPlacement).
    public static class BondTable
    {
        // 출처: blueprints.zip ArmorEnhancementBonus1~4.jbp(EnhancementValue 1~4) = 본편 MagicalVestmentShieldBuff 2e8446f8… m_Enchantments 의 앞 4개(jbp 2026-11-14). 순서 = 층 1..4.
        public static readonly IReadOnlyList<string> EnhancementGuids = new[]
        {
            Guids.Game.ArmorEnhancementBonus1, Guids.Game.ArmorEnhancementBonus2, Guids.Game.ArmorEnhancementBonus3, Guids.Game.ArmorEnhancementBonus4,
        };

        // A2b(2026-11-14): 방패에 실제로 붙는 것은 위 본편 마법부여의 값 복사본인 우리 BondEnchant{n}(Guids) — 「우리 것」을 블루프린트로 알아본다(BondPlacement).
        public static readonly IReadOnlyList<string> OurEnchantmentGuids = new[] { Guids.BondEnchant1, Guids.BondEnchant2, Guids.BondEnchant3, Guids.BondEnchant4 };

        // 층 n(1..4)의 본편 원본 마법부여 GUID(복사 출처). 범위 밖 = 던짐(층 수와 레벨 표가 어긋나면 빌드가 시끄럽게 멈춘다).
        public static string EnhancementGuid(int n) { return Pick(EnhancementGuids, n); }

        // 층 n(1..4)의 우리 마법부여 GUID(방패에 붙는 것).
        public static string OurEnchantmentGuid(int n) { return Pick(OurEnchantmentGuids, n); }

        static string Pick(IReadOnlyList<string> list, int n)
        {
            if (list.Count != BulwarkRules.Levels.BondEnhancementLevels.Count) throw new InvalidOperationException("BulwarkMage: bond enchantment list (" + list.Count + ") and BondEnhancementLevels (" + BulwarkRules.Levels.BondEnhancementLevels.Count + ") differ in length");
            if (n < 1 || n > list.Count) throw new ArgumentOutOfRangeException("n", n, "BulwarkMage: bond enhancement layer must be 1.." + list.Count);
            return list[n - 1];
        }

        static void RequireLevel(int level)
        {
            if (level < BulwarkRules.Levels.MinLevel || level > BulwarkRules.Levels.MaxLevel) throw new ArgumentOutOfRangeException("level", level, "BulwarkMage: level must be " + BulwarkRules.Levels.MinLevel + ".." + BulwarkRules.Levels.MaxLevel);
        }

        // 그 레벨까지 받은 강화 층 수(= 방패에 붙는 우리 마법부여 수, 가장 큰 층 번호).
        public static int StepsAt(int level)
        {
            RequireLevel(level);
            return BulwarkRules.Levels.BondEnhancementLevels.Count(l => l <= level);
        }

        // 방패의 실제 강화 = 큰 값 하나(설계 C5 비중첩): max(방패 자체 강화, 우리 층 수).
        public static int Effective(int ownEnhancement, int level)
        {
            if (ownEnhancement < 0) throw new ArgumentOutOfRangeException("ownEnhancement", ownEnhancement, "BulwarkMage: an item enhancement cannot be negative");
            return Math.Max(ownEnhancement, StepsAt(level));
        }

        // 층들이 더하는 ACP 이동(층마다 AdvanceArmorStats.ArmorCheckPenaltyShift).
        public static int AcpShiftFromBond(int level)
        {
            return StepsAt(level) * BulwarkRules.Dials.BondAcpShiftPerEnchantment;
        }

    }
}
