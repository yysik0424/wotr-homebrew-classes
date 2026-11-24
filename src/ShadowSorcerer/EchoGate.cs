using System;

namespace ShadowSorcerer
{
     
    public enum Decision { Echo, Skip, Unknown }

    // 명령 출처(H1·H2·H3 표식 결과). None = 표식 없음(직접 Trigger·자동 시전 등).
    public enum OriginKind { Command, TouchWeaponDelivery, None }

    public sealed class GateResult
    {
        public readonly Decision Decision;
        public readonly string Reason;
        public readonly int Count;
        public GateResult(Decision d, string reason, int count) { Decision = d; Reason = reason; Count = count; }
        public static GateResult Skip(string reason) { return new GateResult(Decision.Skip, reason, 0); }
        public static GateResult Unknown(string reason) { return new GateResult(Decision.Unknown, reason, 0); }
        public override string ToString() { return Decision + (Reason == null ? "" : " (" + Reason + ")") + (Decision == Decision.Echo ? " x" + Count : ""); }
    }

    // 사유 이름(사유별 계수 ShadowErrors.CountSkip 의 키 — 점검 S6 skip-reasons-recorded 가 읽는다).
    public static class GateReasons
    {
        public const string Duplicate = "Duplicate", Failed = "Failed", NotCommand = "NotCommand", Item = "Item", NotSpell = "NotSpell", NoSpellbook = "NoSpellbook",
            OtherSpellbook = "OtherSpellbook", StickyCharge = "StickyCharge", NotHostile = "NotHostile", RoundUsed = "RoundUsed";
    }

    // 관찰한 시전의 사실(null = 알 수 없음). 게임 쪽 ShadowEchoComponent 가 채운다.
    public sealed class CastFacts
    {
        public bool? IsDuplicate;
        public bool? Success;
        public OriginKind? Origin;
        public string SpellbookGuid;           // null = 주문책 없음(정당한 상태 — Skip)
        public bool SpellbookKnown;            // 주문책 사실을 읽었는가(false = Unknown)
        public bool? HasSourceItem;
        public bool? IsSpellType;               // AbilityType == Spell
        public bool? IsStickyCastStage;         // 전하 유지 접촉의 충전 단계(AbilityEffectStickyTouch 를 가진 시전)
        public bool? IsStickyDelivery;          // 전달 단계(AbilityData.StickyTouch != null) — 적대 사실은 원래 주문의 것
        public HostileFacts Hostile;
        public TargetFacts Target;
        public int? SorcererLevel;
        public RoundFacts Round;
    }

    public static class EchoGate
    {
        // 생산 진입점: ShadowRules 를 ldsfld 로 읽어 넘긴다.
        public static GateResult Decide(CastFacts f) { return Core(f, Guids.Game.SorcererSpellbook, ShadowRules.RoundSeconds, ShadowRules.EchoCountLevels, ShadowRules.MinLevel, ShadowRules.MaxLevel); }

        // 순서 고정(계획): 1 중복 · 2 실패 · 3 출처 · 4 아이템 · 5 주문 형식 · 6 주문책 · 7 접촉 · 8 적대 · 9 대상 · 10 라운드 · 11 분신 수.
        public static GateResult Core(CastFacts f, string sorcererSpellbook, double roundSeconds, System.Collections.Generic.IReadOnlyList<(int level, int count)> echoLevels, int minLevel, int maxLevel)
        {
            if (f == null) return GateResult.Unknown("no cast facts");
            if (!f.IsDuplicate.HasValue) return GateResult.Unknown("IsDuplicate unread");
            if (f.IsDuplicate.Value) return GateResult.Skip(GateReasons.Duplicate);
            if (!f.Success.HasValue) return GateResult.Unknown("Success unread");
            if (!f.Success.Value) return GateResult.Skip(GateReasons.Failed);
            if (!f.Origin.HasValue) return GateResult.Unknown("origin unread");
            if (f.Origin.Value == OriginKind.None) return GateResult.Skip(GateReasons.NotCommand);
            if (!f.HasSourceItem.HasValue) return GateResult.Unknown("source item unread");
            if (f.HasSourceItem.Value) return GateResult.Skip(GateReasons.Item);
            if (!f.IsSpellType.HasValue) return GateResult.Unknown("ability type unread");
            if (!f.IsSpellType.Value) return GateResult.Skip(GateReasons.NotSpell);
            if (!f.SpellbookKnown) return GateResult.Unknown("spellbook unread");
            if (f.SpellbookGuid == null) return GateResult.Skip(GateReasons.NoSpellbook);
            if (f.SpellbookGuid != sorcererSpellbook) return GateResult.Skip(GateReasons.OtherSpellbook);
            if (!f.IsStickyCastStage.HasValue || !f.IsStickyDelivery.HasValue) return GateResult.Unknown("sticky touch stage unread");
            if (f.IsStickyCastStage.Value && !f.IsStickyDelivery.Value) return GateResult.Skip(GateReasons.StickyCharge);
            var h = HostileRule.Classify(f.Hostile);
            if (h.Decision != Decision.Echo) return h;
            var t = TargetRule.AtCast(f.Target);
            if (t.Decision != Decision.Echo) return t;
            var r = RoundRule.Core(f.Round, roundSeconds);
            if (r.Decision != Decision.Echo) return r;
            if (!f.SorcererLevel.HasValue) return GateResult.Unknown("sorcerer level unread");
            int n = ShadowRules.EchoCountCore(f.SorcererLevel.Value, echoLevels, minLevel, maxLevel);
            if (n <= 0) return GateResult.Unknown("echo count " + n + " at sorcerer level " + f.SorcererLevel.Value);
            return new GateResult(Decision.Echo, null, n);
        }
    }
}
