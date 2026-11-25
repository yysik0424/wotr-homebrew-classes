using System;
using System.Collections.Generic;

namespace ShadowSorcerer
{
     
    //   Open(kind, data) / Close() = 범위 스택(H1 UnitUseAbility.OnAction · H3 TouchSpellsController.OnEventDidTrigger 의 Prefix/Finalizer).
    //   EnterCtor(data) = RuleCastSpell 생성자 진입(H2 Prefix). 맨 위 범위의 데이터와 같고 그 범위에서 아직 아무도 claim 하지 않았으면 토큰.
    //   CompleteCtor(token, instance) = 생성자 끝(H2 Postfix). 토큰이 있을 때만 표식(mark).
    // 진입 순서로 claim 하므로 CalculateParams(생성자 IL_0023 → CreateExecutionContext → CalculateParams) 안의 처리기가 같은 데이터로
     
    public sealed class ClaimToken
    {
        public readonly OriginKind Kind;
        internal readonly object OriginalAbility;
        internal ClaimToken(OriginKind kind, object originalAbility) { Kind = kind; OriginalAbility = originalAbility; }
    }

    public sealed class OriginClaimRule
    {
        sealed class Scope { public OriginKind Kind; public object Data, OriginalAbility; public bool Claimed; }
        readonly List<Scope> stack = new List<Scope>();








        public void Open(OriginKind kind, object data, object originalAbility = null)
        {
            if (kind == OriginKind.None) throw new ArgumentException("ShadowSorcerer: OriginClaimRule.Open with OriginKind.None");
            if (data == null) throw new ArgumentNullException("data", "ShadowSorcerer: OriginClaimRule.Open without ability data");
            stack.Add(new Scope { Kind = kind, Data = data, OriginalAbility = originalAbility });
        }

        public void Close()
        {
            if (stack.Count == 0) throw new InvalidOperationException("ShadowSorcerer: OriginClaimRule.Close with no open scope");
            stack.RemoveAt(stack.Count - 1);
        }

        // H1·H3 Finalizer 의 순수 판정: Prefix 가 범위를 열었을 때만(opened) 닫는다. 닫았으면 true.
        public bool CloseIfOpened(bool opened)
        {
            if (!opened) return false;
            Close();
            return true;
        }

        public ClaimToken EnterCtor(object data)
        {
            if (stack.Count == 0 || data == null) return null;
            var top = stack[stack.Count - 1];
            if (top.Claimed || !ReferenceEquals(top.Data, data)) return null;
            top.Claimed = true;
            return new ClaimToken(top.Kind, top.OriginalAbility);
        }

        public bool CompleteCtor(ClaimToken token, object instance, Action<object, OriginKind> mark)
        {
            if (mark == null) throw new ArgumentNullException("mark");
            if (token == null) return false;
            if (instance == null) throw new ArgumentNullException("instance", "ShadowSorcerer: OriginClaimRule.CompleteCtor with a token but no instance");
            mark(instance, token.Kind);
            return true;
        }
    }
}
