using System;
using System.Collections.Generic;

namespace ChaosWarrior
{
     
    // 결함: 우리 FX 버프의 FxOnRemove 가 null 이라 엔진 Buff.OnRemove 가 NRE 를 내며 분노가 묶은 변이·껍질 버프를 다 지우지 못했다 → 분노 없이 영구 버프로 세이브에 남음.
    // 고아 = 우리 변이 표식 또는 원소 껍질 버프인데, 지금 그 유닛에 붙어 있는 우리 분노 버프 어느 것도 그 인스턴스를 저장(StoreFact)하고 있지 않은 것.
    // 게임 쪽(OrphanMutationSweep)은 값을 옮기기만 하고 판단은 여기서 한다(시험 exe 가 직접 실행).
    public struct BuffSlot
    {
        public readonly string Guid;          // 블루프린트 GUID. null = 블루프린트 없음(판정 불가)
        public readonly bool StoredByLiveRage; // 붙어 있는 우리 분노 버프의 m_StoredFacts 에 이 인스턴스가 있음
        public BuffSlot(string guid, bool storedByLiveRage) { Guid = guid; StoredByLiveRage = storedByLiveRage; }
    }

    public static class OrphanMutationRule
    {
        public static bool IsOurs(string guid)
        {
            if (guid == null) throw new ArgumentNullException("guid");
            if (MutationSpecs.IsMarker(guid)) return true;
            foreach (var s in MutationSpecs.ShellBuffIds) if (s == guid) return true;
            // 2026-11-16: 분노 중 자연 무기 단계 버프도 분노의 자식이어야 한다. 본편 ContextActionApplyBuff.AsChild 는 부모 버프 문맥이 없으면 기록 없이 안 묶는다(RunAction @00E7~00FC)
            // → 분노 없이 남은 단계 버프는 같은 정리 대상(지역 진입·불러오기 뒤와 분노 종료 직후에 지우고 기록 — OrphanMutationSweep).
            if (RageStrikeSpec.IsStrikeBuff(guid)) return true;
            // 개편 O8: 신속 시전 권리 버프도 분노의 자식(변이 추첨 행동이 ApplyChild 로 부여) — 분노 없이 남은 권리는 분노 밖 신속 시전을 주므로 같은 정리 대상.
            if (QuickenSpec.IsRight(guid)) return true;
            return false;
        }

        // 정리 로그에 쓰는 종류 이름(2026-11-16 독립 검토 F): 단계 버프를 지우면서 「변이·FxOnRemove 결함」으로 찍으면 원인을 오도한다. README 가 이 문자열로 로그를 찾는다(시험 `exec: orphan mutation rule` 이 README 와 대조).
        public static readonly string LogRemovedPrefix = "removed orphan ";
        public static readonly string KindMutation = "mutation buff";
        public static readonly string KindStrike = "rage strike tier buff";
        public static readonly string KindQuicken = "quicken right buff";

        // 우리 것이 아닌 GUID 는 종류가 없다 → 던진다(조용히 「변이」로 부르지 않는다).
        public static string KindOf(string guid)
        {
            if (guid == null) throw new ArgumentNullException("guid");
            if (RageStrikeSpec.IsStrikeBuff(guid)) return KindStrike;
            if (QuickenSpec.IsRight(guid)) return KindQuicken;
            if (IsOurs(guid)) return KindMutation;
            throw new ArgumentException("ChaosWarrior: buff " + guid + " is not a Chaos Warrior rage child (no orphan kind)", "guid");
        }

        // 지운 버프 한 개의 정보 줄(모드 접두 뒤). 종류마다 원인 설명이 다르다.
        public static string RemovedLine(string guid, string buffName, string unitName, int totalRemoved)
        {
            string kind = KindOf(guid);
            string why = kind == KindStrike
                ? "no live Chaos Warrior rage holds it; a tier buff must end with its rage — it was never tied to one or outlived it"
                : kind == KindQuicken
                ? "no live Chaos Warrior rage holds it; a quicken right must end with its rage"
                : "no live Chaos Warrior rage holds it; a rage ended without removing this child — first seen as the 2026-11-16 FxOnRemove defect";
            return LogRemovedPrefix + kind + " " + buffName + " from " + unitName + " (" + why + "). Total removed this session: " + totalRemoved;
        }





        // 지울 인덱스. 블루프린트 없는 버프(Guid null)는 우리 것인지 알 수 없으므로 건너뛰고 unknown 으로 센다(조용히 지우지도, 아니라고 하지도 않는다).
        public static List<int> Orphans(IList<BuffSlot> buffs, out int unknown)
        {
            if (buffs == null) throw new ArgumentNullException("buffs");
            unknown = 0;
            var r = new List<int>();
            for (int i = 0; i < buffs.Count; i++)
            {
                var b = buffs[i];
                if (b.Guid == null) { unknown++; continue; }
                if (IsOurs(b.Guid) && !b.StoredByLiveRage) r.Add(i);
            }
            return r;
        }
    }
}
