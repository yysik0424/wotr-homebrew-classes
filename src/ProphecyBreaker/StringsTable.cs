using System.Collections.Generic;
namespace ProphecyBreaker
{
    internal static class StringsTable
    {
        internal static readonly Dictionary<string,(string ko,string en)> Table = new Dictionary<string,(string ko,string en)> {
            {"Archetype.name", ("예언 파괴자","Prophecy Breaker")},
            {"Archetype.desc", ("자신에게 내려진 예언을 거부한 오라클입니다. 신비와 계시를 포기하고 지능으로 신성 주문을 시전하며, 비전 주문을 직접 연구합니다. 오라클의 저주와 치유 또는 가해 선택을 유지합니다. 기본 공격 보너스는 오라클 레벨의 절반(내림)입니다. 연구 주문의 기본 내성 DC는 10 + 주문 단계 + 지능 수정치이며, 주문 집중과 주문 고양도 적용됩니다.","This oracle rejects a foretold fate. In exchange for mysteries and revelations, you cast divine spells with Intelligence and study arcane magic. You retain your curse and choice of cure or inflict spells. Your base attack bonus equals half your oracle level, rounded down. Research spells use a base save DC of 10 + spell level + Intelligence modifier, including the usual adjustments for Spell Focus and Heighten Spell.")},
            {"Divine", ("예언 파괴자의 신성 주문서","Prophecy Breaker’s Divine Spellbook")},
            {"Research", ("비전 연구서","Arcane Research Book")},
            {"Research.desc", ("오라클 레벨마다 비전 주문 2종을 선택하며 두루마리도 필사할 수 있습니다. 1~9단계 각각 최대 4종을 기입하고 2종을 준비합니다. 연구서에서 열린 단계마다 신성 주문의 하루 시전 횟수 2회를 비전에 배정합니다. 그 단계의 준비 주문은 지능에 관계없이 이 2회를 공유합니다. 휴식하면 준비 변경이 적용됩니다. 기입란이 찼다면 기존 한 종을 골라 교체할 수 있습니다. 시전자 레벨은 오라클 레벨이며 기본 내성 난이도는 10 + 주문 단계 + 지능 수정치입니다.","Choose two arcane spells per oracle level or copy scrolls. At each spell tier from 1 to 9 you can inscribe four spells and prepare two. Each unlocked research tier takes two daily casts from the same divine tier. Its prepared arcane spells share those two casts regardless of Intelligence. Rest to ready your preparations. When a tier is full, select one inscription to replace. Caster level equals oracle level. Base save DC equals 10 + spell level + your Intelligence modifier.")},
            {"Replace", ("{0}단계에 {1} 기입 · 교체할 주문 선택","Inscribe {1} at tier {0} · Choose a spell to replace")},
            {"Cancel", ("취소","Cancel")}
        };
    }
}
