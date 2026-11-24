using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using static WotrClassMod.Common.BlueprintUtil;

namespace Golemancer
{
    // 준비한 추출물 슬롯의 관측·소모(공용 — 「응급 수리」 Repair.cs 와 「골렘 폭주」 Overdrive.cs 가 같은 경로를 쓴다, 소유자 결정 2026-11-16 「복제하지 말고 공용화」).
    // 2026-11-16: Repair.cs 의 SlotsOf·View·CountSpendable·소모 블록을 그대로 옮기고 학파 조건(school — null = 학파 무관 = 응급 수리)을 더했다. 판정(쓸 수 있는 칸·고를 칸)은 순수 RepairRules.
    // 엔진 경로(Assembly-CSharp 748b3a14): 슬롯 = Spellbook.GetMemorizedSpellSlots(int) @0001~0008 · 소모 = SpellSlot.Spend @0002(Available=false, 반대 학파면 연결 칸도 @0010~0033) ·
    //   순서 = Spellbook.SpendInternal @010A~0155(뒤에서 앞으로) · 학파 = BlueprintAbility.School(@0001~001A: SpellComponent 없으면 None(0)).
    public sealed class SpentExtract
    {
        public readonly int Level;
        public readonly string SchoolName, BlueprintName; // SchoolName = 엔진 SpellSchool 이름("None" 포함) — 해석은 순수 OverdriveRules.SchoolOf
        public readonly int Before, After;                // 그 레벨(·학파)의 쓸 수 있는 추출물 수(소모 전·후, 되읽기 = −1)
        public SpentExtract(int level, string schoolName, string blueprintName, int before, int after) { Level = level; SchoolName = schoolName; BlueprintName = blueprintName; Before = before; After = after; }
        public override string ToString() { return "L" + Level + " " + BlueprintName + " (" + SchoolName + "), slots " + Before + " -> " + After; }
    }

    public static class ExtractSlots
    {
        static BlueprintCharacterClass Alchemist { get { return Get<BlueprintCharacterClass>(Guids.Game.AlchemistClass); } }

        public static List<SpellSlot> SlotsOf(UnitEntityData master, int slotLevel)
        {
            if (master == null) throw new ArgumentNullException("master");
            RepairRules.RequireSlotLevel(slotLevel);
            var sb = master.Descriptor.GetSpellbook(Alchemist);
            if (sb == null) throw new InvalidOperationException("Golemancer: " + master.CharacterName + " has no alchemist spellbook");
            var raw = sb.GetMemorizedSpellSlots(slotLevel);
            if (raw == null) throw new InvalidOperationException("Golemancer: alchemist spellbook has no slot list for level " + slotLevel);
            return raw.ToList();
        }

        // 칸의 관측. 학파 = 그 칸 추출물 블루프린트의 School 이름(추출물이 없으면 null).
        internal static SlotView View(SpellSlot s)
        {
            var shell = s.SpellShell;
            return new SlotView(s.Available, shell != null, s.IsMainSlot, shell == null || shell.Blueprint == null ? null : shell.Blueprint.School.ToString());
        }

        // school == null = 학파 무관.
        public static int CountSpendable(UnitEntityData master, int slotLevel, string school = null) { return RepairRules.CountSpendable(SlotsOf(master, slotLevel).Select(View).ToList(), school); }

        // 그 레벨(·학파)의 추출물 1개 소모: 고를 칸 = RepairRules.PickSlot(엔진과 같은 순서 + 학파 조건), 소모 전에 그 추출물의 학파·이름을 읽고, 되읽기 = 쓸 수 있는 수가 정확히 −1.
        // 쓸 칸이 없으면 던진다(호출자는 판정으로 먼저 막는다 — 여기 오면 판정과 실행이 어긋난 것).
        public static SpentExtract SpendOne(UnitEntityData master, int slotLevel, string what, string school = null)
        {
            string schoolName, name;
            var slot = Pick(master, slotLevel, what, school, out schoolName, out name);
            int before = CountSpendable(master, slotLevel, school);
            slot.Spend();
            int after = CountSpendable(master, slotLevel, school);
            if (after != before - 1) throw new InvalidOperationException("Golemancer: " + what + " slot spend readback " + before + " -> " + after + " (expected -1)");
            return new SpentExtract(slotLevel, schoolName, name, before, after);
        }

        // 다음에 소모될 추출물(소모하지 않는다): SpendOne 이 고를 것과 같은 칸. Before == After == 지금 쓸 수 있는 수.
        public static SpentExtract PeekNext(UnitEntityData master, int slotLevel, string what, string school = null)
        {
            string schoolName, name;
            Pick(master, slotLevel, what, school, out schoolName, out name);
            int n = CountSpendable(master, slotLevel, school);
            return new SpentExtract(slotLevel, schoolName, name, n, n);
        }

        static SpellSlot Pick(UnitEntityData master, int slotLevel, string what, string school, out string schoolName, out string name)
        {
            if (string.IsNullOrEmpty(what)) throw new ArgumentException("Golemancer: ExtractSlots needs the name of what is being paid for");
            var slots = SlotsOf(master, slotLevel);
            int idx = RepairRules.PickSlot(slots.Select(View).ToList(), school);
            if (idx < 0) throw new InvalidOperationException("Golemancer: " + what + " decided None but no spendable L" + slotLevel + (school == null ? "" : " " + school) + " slot");
            var shell = slots[idx].SpellShell;
            if (shell == null || shell.Blueprint == null) throw new InvalidOperationException("Golemancer: " + what + " picked an L" + slotLevel + " slot without an extract");
            schoolName = shell.Blueprint.School.ToString();
            name = shell.Blueprint.name;
            return slots[idx];
        }
    }
}
