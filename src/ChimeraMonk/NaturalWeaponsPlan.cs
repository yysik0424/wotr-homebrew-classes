using System;
using System.Collections.Generic;
using System.Linq;

namespace ChimeraMonk
{
     
    // 원본 = 본편 1d6 자연 무기(주사위는 ChimeraLimbComponent 가 사다리로 바꾼다), 무기 형식은 본편 자연 형식 그대로(IsNatural = true → FCT 패치 9곳 적용, 설계 §4).
    public sealed class WeaponRow
    {
        public readonly string Key, Mutation, Guid, SourceGuid, TypeGuid;
        public WeaponRow(string key, string mutation, string guid, string source, string type) { Key = key; Mutation = mutation; Guid = guid; SourceGuid = source; TypeGuid = type; }
    }

    public static class NaturalWeaponsPlan
    {
        public const string KeyArm = "Arm";

        public static IReadOnlyList<WeaponRow> Rows()
        {
            return new[]
            {
                new WeaponRow("Tail", "Tail", Guids.WeaponTail, Guids.Game.Tail1d6, Guids.Game.TailType),
                new WeaponRow("Bite", "Bite", Guids.WeaponBite, Guids.Game.Bite1d6, Guids.Game.BiteType),
                new WeaponRow("Claw", "Claw", Guids.WeaponClaw, Guids.Game.Claw1d6, Guids.Game.ClawType),
                new WeaponRow("Horn", "Horn", Guids.WeaponHorn, Guids.Game.Gore1d6, Guids.Game.GoreType),          // 소유자 결정 2026-11-16 2번(정찰 5-1 1순위 Gore1d6)
                new WeaponRow(KeyArm, "ExtraArm", Guids.WeaponArm, Guids.Game.Slam1d6, Guids.Game.SlamType),       // 판정 D3 Slam
            };
        }

        // 폐지된 변이의 무기(소유자 결정 2026-11-17: 추가 꼬리 삭제). 복제 블루프린트만 같은 GUID 로 남기고 **어느 특성에도 붙이지 않는다**(AddAdditionalLimb 없음) — Mutation 칸 = 폐지 변이 Id(ChimeraRules.RetiredMutations).
        //  남기는 이유 = Guids.Retired 머리말(켜진 채 저장된 세이브의 추가 사지 칸에 이 무기가 남아 있을 수 있다). 사다리 필터(WeaponGuids)에는 넣지 않는다: 우리가 붙이는 사지가 아니다.
        public static IReadOnlyList<WeaponRow> RetiredRows()
        {
            return new[]
            {
                new WeaponRow("ExtraTail", "ExtraTail", Guids.Retired.WeaponExtraTail, Guids.Game.Tail1d6, Guids.Game.TailType),
            };
        }

        // 표 무결성(C2) — 빈 목록 = 일치. NaturalWeapons.Build 가 첫머리에서 부르고 비어 있지 않으면 던진다. 시험 `retired: plan` 이 같은 함수를 실행한다.
        //  붙이는 행의 변이 == 선택지의 사지 변이(순서까지) · 폐지 행의 변이 == 폐지 변이 · 키와 GUID 는 두 표를 합쳐 겹치지 않는다.
        public static List<string> Problems()
        {
            var p = new List<string>();
            var rows = Rows();
            var retired = RetiredRows();
            var limbs = ChimeraRules.Mutations.Where(m => m.Kind == MutationKind.Limb).Select(m => m.Id).ToArray();
            if (!rows.Select(r => r.Mutation).SequenceEqual(limbs)) p.Add("weapon rows [" + string.Join(",", rows.Select(r => r.Mutation)) + "] != limb mutations [" + string.Join(",", limbs) + "]");
            foreach (var r in retired) if (!ChimeraRules.RetiredMutations.Contains(r.Mutation)) p.Add("retired weapon row " + r.Key + " names " + r.Mutation + ", which is not a retired mutation");
            var all = rows.Concat(retired).ToArray();
            if (all.Select(r => r.Key).Distinct().Count() != all.Length) p.Add("duplicate weapon key");
            if (all.Select(r => r.Guid).Distinct().Count() != all.Length) p.Add("duplicate weapon guid");
            return p;
        }

        // 사다리 필터 원장(별도 상수 없음 — Rows 의 우리 GUID 열, C4). 폐지 무기는 없다.
        public static IReadOnlyList<string> WeaponGuids { get { return Rows().Select(r => r.Guid).ToArray(); } }

        // 우리가 만드는 자연 무기 블루프린트 전부(붙이는 것 + 폐지) — 아이콘 분류(IconTable.ItemIconTargets)용.
        public static IReadOnlyList<string> AllWeaponGuids { get { return Rows().Concat(RetiredRows()).Select(r => r.Guid).ToArray(); } }

        public static string ArmGuid { get { return Rows().Single(r => r.Key == KeyArm).Guid; } }
    }
}
