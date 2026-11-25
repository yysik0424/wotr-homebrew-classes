using System;
using Kingmaker.RuleSystem;
using WotrClassMod.Common;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Mechanics;
using UnityEngine;

namespace ShadowSorcerer
{
     
    public sealed class EchoTag
    {
        public readonly bool NeedsFrozenCenter;
        public readonly Vector3? FrozenCenter;
        public readonly int Index;
        public EchoTag(bool needsFrozenCenter, Vector3? frozenCenter, int index) { NeedsFrozenCenter = needsFrozenCenter; FrozenCenter = frozenCenter; Index = index; }
    }

    // 출처는 규칙 자체에, 메아리 표지는 규칙·컨텍스트의 약한 키에 둔다. 표지에는 엔진 객체 참조가 없다.
     
    public static class EchoRegistry
    {
        static readonly RulebookEvent.CustomDataKey originKey = new RulebookEvent.CustomDataKey("ShadowSorcerer.Origin");
        internal static readonly RulebookEvent.CustomDataKey OriginalAbilityKey = new RulebookEvent.CustomDataKey("ShadowSorcerer.OriginalAbility");
        static readonly WeakKeyTable<object, EchoTag> tags = new WeakKeyTable<object, EchoTag>();

        // 명령 범위·생성자 claim 상태기계(순수 OriginClaimRule) — 게임 메인 스레드 하나.
        public static readonly OriginClaimRule Claims = new OriginClaimRule();

        public static void MarkOrigin(RuleCastSpell rule, OriginKind kind)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            OriginKind prior;
            if (rule.TryGetCustomData(originKey, out prior)) throw new InvalidOperationException("ShadowSorcerer: RuleCastSpell origin marked twice");
            rule.SetCustomData(originKey, kind);
        }

        public static OriginKind OriginOf(RuleCastSpell rule)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            OriginKind prior;
            return rule.TryGetCustomData(originKey, out prior) ? prior : OriginKind.None;
        }

        public static void Mark(object ruleOrContext, EchoTag tag)
        {
            if (ruleOrContext == null) throw new ArgumentNullException("ruleOrContext");
            if (tag == null) throw new ArgumentNullException("tag");
            EchoTag old;
            if (tags.TryGetValue(ruleOrContext, out old)) throw new InvalidOperationException("ShadowSorcerer: echo tag set twice on " + ruleOrContext.GetType().Name);
            tags.Add(ruleOrContext, tag);
        }

        public static bool IsEcho(RuleCastSpell rule) { EchoTag t; return rule != null && tags.TryGetValue(rule, out t); }
        public static bool IsEcho(MechanicsContext ctx) { EchoTag t; return ctx != null && tags.TryGetValue(ctx, out t); }
        public static EchoTag TagOf(MechanicsContext ctx) { EchoTag t; return ctx != null && tags.TryGetValue(ctx, out t) ? t : null; }
        public static bool InConstruction { get { return EchoConstructionScope.Depth > 0; } }




    }

     
    public static class EchoConstructionScope
    {
        [ThreadStatic] static int depth;
        public static int Depth { get { return depth; } }

        public struct Handle : IDisposable
        {
            bool open;
            internal Handle(bool o) { open = o; }
            public void Dispose()
            {
                if (!open) return;
                open = false;
                if (depth <= 0) throw new InvalidOperationException("ShadowSorcerer: EchoConstructionScope closed more times than opened");
                depth--;
            }
        }

        public static Handle Enter() { depth++; return new Handle(true); }
    }
}
