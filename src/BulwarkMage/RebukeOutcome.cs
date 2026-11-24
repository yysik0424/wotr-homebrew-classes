using System;

namespace BulwarkMage
{
     
    // 사용 처리는 바뀌지 않는다 — 이 판정은 효과 행동이 끝난 뒤 관측한 값으로만 내리고, 자원·준비됨·표지에는 손대지 않는다(하루 −1·라운드 사용 1 은 효과 앞에서 이미 끝).
    // 게임 형식 없음(modinfo testSources) — 시험 exe 가 실행한다. 관측값은 게임 쪽 RebukeOutcomeTracker 가 엔진 규칙에서 채운다:
    //   피해 = 그 발동 창 안의 시전자 → 공격자 RuleDealDamage 수와 Result 합(엔진이 감소·면역·저항을 적용한 뒤의 실제 피해, RuleDealDamage.get_Result)
    //   버프 = 공격자의 효과 버프(Staggered·SlowBuff·DazeBuff) 인스턴스가 새로 생기거나 끝 시각(Buff.EndTime)이 늘었는가 — 내성 성공·면역이면 둘 다 없다
    //   밀기 = 공격자의 UnitPartForceMove 덩어리 수가 늘었는가(ContextActionPush.RunAction @00D4~00F2 → UnitPartForceMove.Push → ForceMove @004D 가 m_Chunks 에 Add; 녹색 NPC 등 엔진이 거르면 늘지 않는다)
    //   해제 = 그 시전의 RuleCastSpell(시작 여부 = Result 프로세스) · 그 시전 동안의 RuleDispelMagic 수·Success 수 · 능력 실행 끝(IAbilityExecutionProcessHandler.HandleExecutionProcessEnd)
    // SR: 우리 효과 중 주문 저항을 보는 것은 없다 — 피해·버프·밀기는 주문 문맥이 아니고, E7 의 본편 DispelMagicTarget 은 SpellResistance = false(jbp). 그래서 SR 사유는 없다.
    // 판정 불가(Unknown) = 관측이 모순(피해 효과인데 피해 규칙 0 등) — 「효과 없음」으로 새지 않고 호출자가 ERROR 로 센다(C1).
    public enum RebukeOutcomeKind { Effect, NoEffect, Pending, Unknown }

    // 불발·효과 없음 사유(전투 기록 문구 키 = "dud." + 소문자 이름 — 시험 `strings:` 가 키 존재를 잠근다). 값 순서는 바꾸지 않는다(AttackerDown = 0 은 T5 부터).
    //   AttackerDown = 공격자 쓰러짐(T5, 설계 §R4.1) · Untargetable = 해제 반격의 대상 불가(A4 사전 차단) · 나머지 = A3 효과 없음.
    public enum DudReason { AttackerDown, NoDamage, SavedOrImmune, NoDamageSaved, NotPushed, NothingToDispel, DispelFailed, DispelNotCast, Untargetable }

    public sealed class RebukeObservation
    {
        public int DamageRules;          // 창 안 시전자 → 공격자 RuleDealDamage 수
        public int DamageDealt;          // 그 규칙들의 Result 합
        public bool BuffApplied;         // 효과 버프 새 인스턴스 또는 끝 시각 연장
        public bool? PushStarted;        // 밀기 덩어리 증가(관측 못 하면 null)
        public bool CastRuleSeen;        // E7: 그 시전의 RuleCastSpell 을 봤는가
        public bool CastStarted;         // E7: 그 RuleCastSpell 이 실행 프로세스를 만들었는가(Result != null)
        public bool DispelFinished;      // E7: 그 실행이 끝났는가(HandleExecutionProcessEnd)
        public int DispelRules;          // E7: 그 실행 동안 시전자 → 공격자 RuleDispelMagic 수
        public int DispelSuccesses;      // E7: 그 가운데 Success
    }

    public struct RebukeVerdict
    {
        public readonly RebukeOutcomeKind Kind;
        public readonly DudReason? Reason;   // NoEffect 일 때만 값
        public readonly string Why;          // Unknown 일 때 설명
        public RebukeVerdict(RebukeOutcomeKind kind, DudReason? reason, string why) { Kind = kind; Reason = reason; Why = why; }
        public override string ToString() { return Kind + (Reason == null ? "" : " (" + Reason + ")") + (Why == null ? "" : " (" + Why + ")"); }
    }

    public static class RebukeOutcome
    {
        static RebukeVerdict Effect() { return new RebukeVerdict(RebukeOutcomeKind.Effect, null, null); }
        static RebukeVerdict None(DudReason reason) { return new RebukeVerdict(RebukeOutcomeKind.NoEffect, reason, null); }
        static RebukeVerdict Unknown(string why) { return new RebukeVerdict(RebukeOutcomeKind.Unknown, null, why); }

        // 효과의 모양은 RebukeTable 의 칸에서 읽는다(효과 ID 를 늘어놓지 않는다): 피해(DiceCount) · 적 버프(BuffGuid) · 밀기(PushFeet) · 시전(CastSpellGuid).
        public static RebukeVerdict Decide(string effectId, RebukeObservation o)
        {
            if (o == null) throw new ArgumentNullException("o");
            var s = RebukeTable.Get(effectId);
            if (o.DamageRules < 0 || o.DamageDealt < 0 || o.DispelRules < 0 || o.DispelSuccesses < 0 || o.DispelSuccesses > o.DispelRules)
                throw new ArgumentException("BulwarkMage: rebuke " + effectId + " observation has impossible counts (damage rules " + o.DamageRules + ", dealt " + o.DamageDealt + ", dispels " + o.DispelSuccesses + "/" + o.DispelRules + ")", "o");
            bool damage = s.DiceCount != null, buff = s.BuffGuid != null, push = s.PushFeet != null, cast = s.CastSpellGuid != null;
            if (cast)
            {
                if (damage || buff || push) throw new InvalidOperationException("BulwarkMage: rebuke " + effectId + " mixes a spell cast with other effects - no outcome rule");
                if (!o.CastRuleSeen) return Unknown("no RuleCastSpell was observed for the cast");
                if (!o.CastStarted) return None(DudReason.DispelNotCast);
                if (!o.DispelFinished) return new RebukeVerdict(RebukeOutcomeKind.Pending, null, null);
                if (o.DispelSuccesses > 0) return Effect();
                return None(o.DispelRules == 0 ? DudReason.NothingToDispel : DudReason.DispelFailed);
            }
            if (push)
            {
                if (damage || buff) throw new InvalidOperationException("BulwarkMage: rebuke " + effectId + " mixes a push with other effects - no outcome rule");
                if (o.PushStarted == null) return Unknown("the push could not be observed (no forced-move reading)");
                return o.PushStarted.Value ? Effect() : None(DudReason.NotPushed);
            }
            if (damage)
            {
                if (o.DamageRules == 0) return Unknown("a damage rebuke raised no damage rule");
                if (o.DamageDealt > 0 || (buff && o.BuffApplied)) return Effect();
                return None(buff ? DudReason.NoDamageSaved : DudReason.NoDamage);
            }
            if (buff) return o.BuffApplied ? Effect() : None(DudReason.SavedOrImmune);
            throw new InvalidOperationException("BulwarkMage: rebuke " + effectId + " has no outcome rule");
        }
    }
}
