using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosWarrior
{
     
    //   SingleAnchorComponent = 기준(StandartRageBuff)을 단일 필드로 가진 컴포넌트를 **복제한 새 컴포넌트 1개 추가**(필드 = 우리 분노, 원본 불변 — 본편 Cloak 이 분노 종류마다 하나씩 두는 모양).
    //   ListAnchor = 기준이 든 목록 끝에 우리 분노 1칸.
    // 표는 T8 Step 0 의 zip 판독으로 확정(원장 T8). CrystalMind 의 RemoveBuff 는 행동 목록 모양으로 처리(독립 검토 m3). BadTemper 의 조건 행동은 제외(README).
    // ActionListAnchor(독립 검토 m3): 컴포넌트의 ActionList 안에서 기준을 m_Buff 로 가진 ContextActionRemoveBuff 를 복제해 1개 추가(Crystal Mind 가 분노를 해제하는 모양).
    public enum AliasShape { SingleAnchorComponent, ListAnchor, ActionListAnchor }

    public struct AliasRow
    {
        public readonly string ConsumerGuid, Name, ComponentType, Field, AnchorGuid;
        public readonly AliasShape Shape;
        public AliasRow(string consumerGuid, string name, string componentType, AliasShape shape, string field, string anchorGuid)
        { ConsumerGuid = consumerGuid; Name = name; ComponentType = componentType; Shape = shape; Field = field; AnchorGuid = anchorGuid; }
        public override string ToString() { return Name + "/" + ComponentType + "." + Field; }
    }

    public static class RageAliasTable
    {
        // 기준 분노 버프: 바바리안 StandartRageBuff(Classes/Barbarian/Rage/StandartRageBuff.jbp).
        public const string Anchor = "da8ce41ac3cd74742b80984ccc3c9613";

        public const string RagingBrutality = "4048cff852c8413d85e86d68be7e02a6";
        public static readonly IReadOnlyList<AliasRow> Rows = new[]
        {
            new AliasRow("9db109eee03524f4aa89881c4539a14d", "CloakOfBloodScentFeature", "BuffExtraEffects", AliasShape.SingleAnchorComponent, "m_CheckedBuff", Anchor),
            new AliasRow("421eec56d618765499c8b9e6f8e153a1", "CallToViolenceBuff", "BuffExtraEffects", AliasShape.SingleAnchorComponent, "m_CheckedBuff", Anchor),
            new AliasRow("29e2f51e6dd7427099b015de88718990", "ManglingFrenzyFeature", "BuffExtraEffects", AliasShape.SingleAnchorComponent, "m_CheckedBuff", Anchor),
            new AliasRow("4a83b67d4f24bba4a8d881340185bff0", "DemonicResentmentFeature", "BuffExtraEffects", AliasShape.SingleAnchorComponent, "m_CheckedBuff", Anchor),
            new AliasRow("f3f239c4c08920a4e842bc69d7173d63", "BadTemperFeature", "BuffExtraEffects", AliasShape.SingleAnchorComponent, "m_CheckedBuff", Anchor),
            new AliasRow(RagingBrutality, "RagingBrutalityAbility", "AbilityTargetHasFact", AliasShape.ListAnchor, "m_CheckedFacts", Anchor),
            new AliasRow("d7204fb8ac8a2c943bc81ec70eff6356", "InspireTranquilityEffectBuff", "SuppressBuffs", AliasShape.ListAnchor, "m_Buffs", Anchor),
            new AliasRow("949459a61d064e848f7e86e2a80e336d", "InspireTranquilityEffectBuffMythic", "SuppressBuffs", AliasShape.ListAnchor, "m_Buffs", Anchor),
            new AliasRow("4733d8dd549ff544395f1684ec73c392", "CrystalMind", "AbilityEffectRunAction", AliasShape.ActionListAnchor, "Actions", Anchor),
        };

        public const string NamePrefix = "$ChaosWarriorRageAlias$";

         
        public static string NameFor(string consumerGuid, int index)
        {
            if (string.IsNullOrEmpty(consumerGuid)) throw new ArgumentException("ChaosWarrior: NameFor without a consumer GUID");
            if (index < 0) throw new ArgumentOutOfRangeException("index", index, "ChaosWarrior: alias index must be >= 0");
            return NamePrefix + consumerGuid + "#" + index;
        }
    }

    // [순수] 변환 계획. 컴포넌트 모형 = (이름, 그 필드의 GUID 또는 목록). 게임 쪽 RageAlias.Build 가 이 결과대로 적용·되읽는다.
    public static class AliasPlan
    {
        // 단일 기준: 기준을 가진 컴포넌트가 정확히 1개, 우리 분노를 가진 것 0개, 새 이름이 기존 이름과 겹치지 않음 → (복제할 색인, 새 이름).
        public static Tuple<int, string> Single(IReadOnlyList<Tuple<string, string>> components, string anchor, string ours, string consumerGuid, int index)
        {
            if (components == null) throw new ArgumentNullException("components");
            var hits = Enumerable.Range(0, components.Count).Where(i => components[i].Item2 == anchor).ToArray();
            if (hits.Length != 1) throw new InvalidOperationException("ChaosWarrior: rage alias " + consumerGuid + " has " + hits.Length + " anchor component(s), expected 1");
            if (components.Any(c => c.Item2 == ours)) throw new InvalidOperationException("ChaosWarrior: rage alias " + consumerGuid + " already refers to our rage");
            string name = RageAliasTable.NameFor(consumerGuid, index);
            if (components.Any(c => c.Item1 == name)) throw new InvalidOperationException("ChaosWarrior: rage alias name " + name + " collides with an existing component");
            return Tuple.Create(hits[0], name);
        }

        // 분노를 지우는 복제 행동은 「버프를 통째로」 지워야 한다(독립 검토 R1-I3, 2026-11-17 — Assembly-CSharp IL 직독, md5 앞 12 748b3a143b60):
        //   ContextActionRemoveBuff.RunAction @007D~0082 는 RemoveRank 가 참이면 RemoveByRank 문맥을 열고, 그 안에서 BuffCollection.PrepareFactForDetach @000A~001C 는
        //   순위 > 1 인 버프의 순위만 1 내리고 제거를 건너뛴다. 분노 버프(순위 1)는 지워지지만 Buff.OnRemove 가 지우려는 순위형 자식(변이 표식 · 원소 껍질 — 순위 = 뽑힌 횟수)은 순위 n−1 로 남는다.
        // 그래서 복제한 행동의 RemoveRank 가 참이면 던진다(본편 값은 false — 시험 `jbp: alias rows exist`. 다른 모드가 바꾸면 여기서 빌드가 실패한다 — 조용히 변이가 남는 것보다 낫다).
        public static void RequireWholeRemoval(bool removeRank, string consumerGuid, string actionName)
        {
            if (string.IsNullOrEmpty(consumerGuid)) throw new ArgumentException("ChaosWarrior: RequireWholeRemoval without a consumer GUID");
            if (removeRank) throw new InvalidOperationException("ChaosWarrior: rage alias " + consumerGuid + " removes our rage with RemoveRank = true (action " + actionName + ") — ranked mutation markers would keep rank n-1 after the rage ends");
        }

        // 목록 기준: 기준이 든 목록이 정확히 1개(컴포넌트 단위), 우리 분노 없음 → 그 목록 + 우리.
        public static IReadOnlyList<string> List(IReadOnlyList<string> list, string anchor, string ours, string consumerGuid)
        {
            if (list == null) throw new ArgumentNullException("list");
            if (!list.Contains(anchor)) throw new InvalidOperationException("ChaosWarrior: rage alias " + consumerGuid + " list has no anchor");
            if (list.Contains(ours)) throw new InvalidOperationException("ChaosWarrior: rage alias " + consumerGuid + " list already has our rage");
            return list.Concat(new[] { ours }).ToArray();
        }
    }
}
