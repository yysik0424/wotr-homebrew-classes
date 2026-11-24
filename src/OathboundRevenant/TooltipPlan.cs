using System;
using System.Collections.Generic;
using System.Linq;

namespace OathboundRevenant
{
    // [순수] 능력 툴팁의 지속 시간·내성 굴림 칸 원장(2026-11-16 결함 수리: 코드로 만든 BlueprintAbility 의 LocalizedDuration·LocalizedSavingThrow 가 null 이면
    // 툴팁에 `<null>` 이 찍힌다 — 엔진 근거는 src\Common\BlueprintUtil.cs 「LocalizedString 필드」 주석). 게임 형식 없음(modinfo testSources).
    // 규칙: 본뜬 본편 능력과 같은 값. 새 문자열을 만들지 않는다 — 빈 키(칸 숨김) 또는 본편 공용 문구 키(Guids.GameText)만.
    // 이 모드가 만드는 BlueprintAbility 는 전부 여기에 한 줄씩 있어야 한다(Tooltips.Require 가 빌드에서, 시험 `tooltip:` 이 오프라인에서 대조).
    public sealed class TooltipRow
    {
        public readonly string Key;        // 능력 이름(ResourcePlan·FxTable 의 능력 키와 같은 철자)
        public readonly string Guid;       // 우리 능력 GUID(Guids)
        public readonly string Origin;     // TooltipPlan.Made = 두 칸을 이 표대로 넣는다 / TooltipPlan.Copied = 본편 능력의 필드를 통째로 복사(값은 본편 것, null 아님만 검사)
        public readonly string Duration;   // Made 만: 지속 칸의 팩 키, TooltipPlan.Hidden = 빈 키(칸 숨김)
        public readonly string Save;       // Made 만: 내성 칸의 팩 키, TooltipPlan.Hidden = 빈 키(칸 숨김)
        public readonly string ModelJbp;   // 값의 근거가 된 본편 능력(blueprints.zip 경로) — 시험이 그 능력의 두 칸과 대조한다
        public TooltipRow(string key, string guid, string origin, string duration, string save, string modelJbp)
        { Key = key; Guid = guid; Origin = origin; Duration = duration; Save = save; ModelJbp = modelJbp; }
    }

    public static class TooltipPlan
    {
        public const string Hidden = "";   // 빈 키 = 본편 jbp 의 { "m_Key": "", "Shared": null } = 툴팁이 칸을 숨긴다
        public const string Made = "Made";
        public const string Copied = "Copied";

        // 근거(blueprints.zip 직독 2026-11-16):
        //   음에너지 채널·마지막 숨(채널과 같은 폭발) — 본편 채널(팔라딘 ChannelEnergyPaladinHarm·성직자 ChannelNegativeEnergy)은 두 칸 모두 빈 키.
        //   죽음의 안수 — 본뜬 LayOnHandsOthers 는 두 칸 모두 빈 키.
        //   공포의 응시·사자의 외침 — 본편 FrightfulMoan(초자연 공포, 의지 무효): 지속 빈 키, 내성 = 공용 문구 「Will negates」.
        //   망자의 손·언데드 지배 — 본편 능력의 필드 복사(Hands.CopyAbilityFields): 본편 값이 그대로 온다.
        public static readonly IReadOnlyList<TooltipRow> Rows = new[]
        {
            new TooltipRow("RevenantLayOnHands", Guids.RevenantLayOnHands, Copied, null, null, "Classes/Paladin/LayOnHands/LayOnHandsOthers.jbp"),
            new TooltipRow("NegativeChannel", Guids.ChannelAbility, Made, Hidden, Hidden, "Classes/Paladin/ChannelEnergyPaladin/ChannelEnergyPaladinHarm.jbp"),
            new TooltipRow("DeathsTouch", Guids.DeathsTouchAbility, Made, Hidden, Hidden, "Classes/Paladin/LayOnHands/LayOnHandsOthers.jbp"),
            new TooltipRow("TerribleGaze", Guids.GazeAbility, Made, Hidden, Guids.GameText.SaveWillNegates, "Units/NPC/Commoners/Ghost/GhostBrain/FrightfulMoan.jbp"),
            new TooltipRow("WailOfTheDead", Guids.WailAbility, Made, Hidden, Guids.GameText.SaveWillNegates, "Units/NPC/Commoners/Ghost/GhostBrain/FrightfulMoan.jbp"),
            new TooltipRow("CommandUndead", Guids.CommandUndeadAbility, Copied, null, null, "Spells/Level2/CommandUndead.jbp"),
            new TooltipRow("LastBreath", Guids.LastBreathAbility, Made, Hidden, Hidden, "Classes/Cleric/ChannelNegativeEnergy.jbp"),
        };

        // 없는 GUID 는 던진다(「표에 없는 능력 = 칸 없음」으로 넘기지 않는다).
        public static TooltipRow Row(string guid)
        {
            if (guid == null) throw new ArgumentNullException("guid");
            var r = Rows.Where(x => x.Guid == guid).ToList();
            if (r.Count != 1) throw new ArgumentException("OathboundRevenant: TooltipPlan has " + r.Count + " rows for ability " + guid + " (every BlueprintAbility this mod makes needs exactly one)");
            return r[0];
        }

        // 표 무결성 + 「우리가 만든 능력 GUID 집합 == 표」. 문제 목록(비면 정상). abilityGuids 가 비면 그것도 문제다(검사할 것이 없어서 통과하지 않는다).
        public static List<string> Problems(IEnumerable<string> abilityGuids)
        {
            if (abilityGuids == null) throw new ArgumentNullException("abilityGuids");
            var built = abilityGuids.ToList();
            var p = new List<string>();
            if (built.Count == 0) p.Add("no built abilities to check");
            foreach (var g in Rows.GroupBy(r => r.Guid).Where(g => g.Count() > 1)) p.Add(g.Key + ": listed " + g.Count() + " times");
            foreach (var r in Rows)
            {
                if (string.IsNullOrEmpty(r.Key) || string.IsNullOrEmpty(r.Guid) || string.IsNullOrEmpty(r.ModelJbp)) p.Add(r.Guid + ": key / guid / model missing");
                if (r.Origin == Made) { if (r.Duration == null || r.Save == null) p.Add(r.Key + ": a made ability needs both texts (TooltipPlan.Hidden for a hidden row)"); }
                else if (r.Origin == Copied) { if (r.Duration != null || r.Save != null) p.Add(r.Key + ": a copied ability takes its texts from the vanilla source, the table must not name any"); }
                else p.Add(r.Key + ": unknown origin '" + r.Origin + "'");
                if (!built.Contains(r.Guid)) p.Add(r.Key + " " + r.Guid + ": in the table but not built");
            }
            foreach (var g in built.Where(g => Rows.All(r => r.Guid != g))) p.Add(g + ": built ability without a TooltipPlan row");
            return p;
        }
    }
}
