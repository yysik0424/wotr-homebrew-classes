using System;
using HarmonyLib;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Blueprints.Classes;
using static WotrClassMod.Common.BlueprintUtil;
using Kingmaker.UnitLogic;
using Kingmaker;
using Newtonsoft.Json;

namespace ShadowSorcerer
{
    // This part belongs only to actual shadow sorcerers. Saving the marker prevents
    // later equipment or bonus-slot changes from reapplying the balance migration.
    public class UnitPartShadowSlotMigration : UnitPart
    {
        [JsonProperty] public bool Applied;
    }

     
    // [HarmonyBefore("0ToyBox0")]: ToyBox 의 Spellbook_GetSpellsPerDay_Patch(배율 Postfix, ToyBox.BagOfPatches.Multipliers/SpellbookPatches — Step 0-F)가 우리 뒤에 돌아
    //   우리 조정 결과에 배율을 곱한다(ToyBox 기본 배율 1 이면 영향 없음). PrestigePlus MiracleSpellNumber 는 다른 주문책(Book/Book2)만 바꾼다.
    // 판단은 순수 SpellsPerDayRule.Decide. 능력치·표는 엔진과 같은 경로로 읽는다(아래 IL 위치). 실패 = ReportOnce(뜨거운 경로) + 엔진 값 그대로.
    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetSpellsPerDay))]
    [HarmonyBefore("0ToyBox0")]
    static class SpellsPerDayPatch
    {
        // 엔진 상수 거울: 능력치 문턱 = 10 + 주문 레벨. 출처 Spellbook.GetSpellsPerDay IL_0098 ldc.i4.s 10(dnlib 2026-11-14). 설계 다이얼이 아니다.
        static readonly int AbilityGateBase = 10;

        [ThreadStatic] static Spellbook legacyBook;

        internal static void MarkNewCaster(UnitDescriptor owner)
        {
            if (owner != null && owner.Unit.PreviewOf.IsEmpty && Game.HasInstance && !Game.Instance.IsLoadingSave)
                owner.Unit.Ensure<UnitPartShadowSlotMigration>().Applied = true;
        }

        // Area loading has restored equipment, stats and facts. Ordinary area switches
        // and later saves leave the remaining slots alone once this migration has run.
        internal static void ClampLoadedSlots(UnitDescriptor owner)
        {
            try
            {
                if (owner == null || Archetype.Bp == null || !owner.Progression.IsArchetype(Archetype.Bp)) return;
                var state = owner.Unit.Get<UnitPartShadowSlotMigration>();
                if (state != null && state.Applied) return;
                var book = owner.GetSpellbook(Get<BlueprintCharacterClass>(Guids.Game.SorcererClass));
                if (book == null || book.Blueprint == null || book.Blueprint.AssetGuid.ToString() != Guids.Game.SorcererSpellbook) return;
                ReconcileRemaining(book);
                owner.Unit.Ensure<UnitPartShadowSlotMigration>().Applied = true;
            }
            catch (Exception e) { ShadowErrors.ReportException(ErrorLedger.SiteSpellsPerDay, e); }
        }

        internal static void ReconcileRemaining(Spellbook book)
        {
            var stat = book.Owner.Stats == null ? null : book.Owner.Stats.GetStat(book.Blueprint.CastingAttribute) as ModifiableValueAttributeStat;
            if (stat == null || stat.ModifiedValue < stat.CalculatePermanentValueWithoutTempBuffs()) return;
            for (int level = ShadowRules.SpellsPerDayMinLevel; level <= ShadowRules.SpellsPerDayMaxLevel; level++)
            {
                int saved = book.GetSpontaneousSlots(level);
                if (saved <= 0) continue;
                int current = book.GetSpellsPerDay(level);
                if (saved <= current) continue;
                var facts = Facts(book, level, current);
                if (facts.AbilityTooLow != false || facts.TableValue.GetValueOrDefault() < 2) continue;

                // Evaluate the old rule in the same patch chain, including other mods'
                // slot multipliers. A balance update cannot explain a value above that cap.
                var previousBook = legacyBook;
                int previous;
                try { legacyBook = book; previous = book.GetSpellsPerDay(level); }
                finally { legacyBook = previousBook; }
                if (SpellsPerDayRule.IsLegacySurplus(saved, current, previous))
                    book.RestoreSpontaneousSlots(level, 0);
            }
        }

        static void Postfix(Spellbook __instance, int __0, ref int __result)
        {
            try
            {
                ShadowErrors.MaybeFault(ErrorLedger.SiteSpellsPerDay);
                if (__instance == null || __instance.Blueprint == null) return;                                       // 정당한 무동작: 다른 경로(빈 주문책)
                if (__instance.Blueprint.AssetGuid.ToString() != Guids.Game.SorcererSpellbook) return;                // 정당한 무동작: 소서러 주문책이 아니다
                var owner = __instance.Owner;
                if (owner == null || Archetype.Bp == null || !owner.Progression.IsArchetype(Archetype.Bp)) return;    // 정당한 무동작: 우리 아키타입이 아니다
                var facts = Facts(__instance, __0, __result);
                var d = ReferenceEquals(__instance, legacyBook)
                    ? SpellsPerDayRule.Core(facts, -1, ShadowRules.SpellsPerDayMinLevel, ShadowRules.SpellsPerDayMaxLevel)
                    : SpellsPerDayRule.Decide(facts);
                __result = d.Value;
                if (d.Unknown != SlotsUnknown.None)
                    ShadowErrors.ReportOnce(ErrorLedger.SiteSpellsPerDay, owner.Unit.UniqueId + "|" + __0 + "|" + d.Unknown, "spells per day level " + __0 + " for " + owner.CharacterName + ": " + d);
            }
            catch (Exception e)
            {
                // 엔진 값 그대로(들어온 __result) + 보고. 주입 예외는 주입분으로 센다(S17 inject-spells-per-day).
                ShadowErrors.ReportException(ErrorLedger.SiteSpellsPerDay, e);
            }
        }

        // 엔진 경로 그대로:
        //   능력치: Owner.Stats.GetStat(Blueprint.CastingAttribute) as ModifiableValueAttributeStat — IL_0059~0079. null 이면 엔진은 0(IL_0082~0086) → 능력치 부족과 같다.
        //   부족 판정: Mathf.Min(ModifiedValue, CalculatePermanentValueWithoutTempBuffs()) < 10 + spellLevel — IL_0087~009C(독립 신화 주문책은 이 검사를 건너뛴다; 소서러 주문책은 Normal/Mythic).
        //   표: Blueprint.SpellsPerDay.GetCount(CasterLevel, spellLevel) — IL_00A8~00BA.
        static SlotFacts Facts(Spellbook sb, int spellLevel, int engine)
        {
            var f = new SlotFacts { SpellLevel = spellLevel, EngineResult = engine };
            var stats = sb.Owner.Stats;
            if (stats == null) return f; // AbilityTooLow = null → Unknown
            var stat = stats.GetStat(sb.Blueprint.CastingAttribute) as ModifiableValueAttributeStat;
            if (sb.IsStandaloneMythic) f.AbilityTooLow = false;
            else if (stat == null) f.AbilityTooLow = true;
            else f.AbilityTooLow = Math.Min(stat.ModifiedValue, stat.CalculatePermanentValueWithoutTempBuffs()) < AbilityGateBase + spellLevel;
            var table = sb.Blueprint.SpellsPerDay;
            if (table != null) f.TableValue = table.GetCount(sb.CasterLevel, spellLevel);
            return f;
        }
    }
}
