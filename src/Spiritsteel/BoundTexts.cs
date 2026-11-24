using System.Collections.Generic;
using Kingmaker.Blueprints.Items.Weapons;
using WeaponSpiritShaman;

namespace Spiritsteel
{
    internal static class BoundTexts
    {
        const string Korean = "부족에게 받은 이 무기와 결속하고 해당 범주에 숙련됩니다. 무기는 1·5·9·13·17레벨에 +1부터 +5까지 강화됩니다. 2레벨부터 짝수 레벨마다 이 한 자루에 무기 피트 하나를 새깁니다.";
        const string English = "You bond with an ancestral weapon and gain proficiency in its category. Its enhancement bonus increases from +1 to +5 at levels 1, 5, 9, 13 and 17. Starting at level 2, inscribe a weapon feat on this weapon at each even level.";
        static Dictionary<string, (string ko, string en)> texts;

        internal static void Add(Dictionary<string, (string ko, string en)> table)
        {
            texts = table;
            table.Add("bound.name", ("결속 무기", "Bound Weapon"));
            table.Add("bound.desc", (Korean, English));
            foreach (var row in SpiritCategories.Rows)
                table.Add(Key(row.Category), (Korean, English));
        }

        internal static string Key(string category) { return "bound." + category + ".desc"; }

        static string Stats(BlueprintItemWeapon w, bool ko)
        {
            string critical = (w.CriticalRollEdge < 20 ? w.CriticalRollEdge + "–20" : "20") + "/×" + (int)w.CriticalModifier;
            string damage = w.BaseDamage.Rolls + "d" + (int)w.BaseDamage.Dice;
            return ko ? "피해 " + damage + " · 치명타 " + critical + " · 사거리 " + w.AttackRange.Value + "피트"
                      : "Damage " + damage + " · critical " + critical + " · range " + w.AttackRange.Value + " ft.";
        }

        internal static void Describe(string category, BlueprintItemWeapon head, BlueprintItemWeapon second)
        {
            string ko = Stats(head, true), en = Stats(head, false);
            if (second != null)
            {
                ko += "\n두 번째 머리 — " + Stats(second, true);
                en += "\nSecond head — " + Stats(second, false);
            }
            texts[Key(category)] = (ko + "\n\n" + Korean, en + "\n\n" + English);
        }
    }
}
