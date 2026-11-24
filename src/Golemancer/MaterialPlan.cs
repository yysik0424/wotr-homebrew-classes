using System;
using System.Linq;

namespace Golemancer
{
    // [순수] 골렘 위 재질 버프·수치 특성의 교정 계획(계획 T2 MaterialPlan, R2-03 — 수치 특성은 모드가 소유·추적·회수). 게임 형식 없음.
    // 버프 키·수치 키의 의미는 StatRecipe.BuffKey / StatKey. 기대와 다른 키는 **전부** 제거(고아 회수), 기대 키가 없으면 추가, 기대 키가 2개 이상이면 손상 → throw.
    public sealed class ReconcilePlan
    {
        public readonly string[] RemoveBuffs, RemoveStats;
        public readonly string AddBuff, AddStat; // null = 이미 있어 추가하지 않음(「정당한 무동작」)
        public ReconcilePlan(string[] removeBuffs, string addBuff, string[] removeStats, string addStat)
        {
            RemoveBuffs = removeBuffs; AddBuff = addBuff; RemoveStats = removeStats; AddStat = addStat;
        }
        public bool IsNoop { get { return RemoveBuffs.Length == 0 && RemoveStats.Length == 0 && AddBuff == null && AddStat == null; } }
    }

    public static class MaterialPlan
    {
        public static ReconcilePlan Reconcile(string[] currentBuffKeys, string[] currentStatKeys, string expectedBuffKey, string expectedStatKey)
        {
            Require(currentBuffKeys, "currentBuffKeys");
            Require(currentStatKeys, "currentStatKeys");
            if (string.IsNullOrEmpty(expectedBuffKey)) throw new ArgumentNullException("expectedBuffKey", "MaterialPlan.Reconcile: expected buff key missing");
            if (string.IsNullOrEmpty(expectedStatKey)) throw new ArgumentNullException("expectedStatKey", "MaterialPlan.Reconcile: expected stat key missing");
            int nb = currentBuffKeys.Count(k => k == expectedBuffKey), ns = currentStatKeys.Count(k => k == expectedStatKey);
            if (nb > 1) throw new ArgumentException("MaterialPlan.Reconcile: expected buff key " + expectedBuffKey + " is present " + nb + " times");
            if (ns > 1) throw new ArgumentException("MaterialPlan.Reconcile: expected stat key " + expectedStatKey + " is present " + ns + " times");
            return new ReconcilePlan(
                currentBuffKeys.Where(k => k != expectedBuffKey).ToArray(), nb == 0 ? expectedBuffKey : null,
                currentStatKeys.Where(k => k != expectedStatKey).ToArray(), ns == 0 ? expectedStatKey : null);
        }

        // 적용 뒤 되읽기 조건: 버프 = 기대 1개뿐, 수치 = 기대 1개뿐.
        public static bool Satisfied(string[] buffKeys, string[] statKeys, string expectedBuffKey, string expectedStatKey)
        {
            Require(buffKeys, "buffKeys");
            Require(statKeys, "statKeys");
            if (string.IsNullOrEmpty(expectedBuffKey) || string.IsNullOrEmpty(expectedStatKey)) throw new ArgumentNullException("expected", "MaterialPlan.Satisfied: expected key missing");
            return buffKeys.Length == 1 && buffKeys[0] == expectedBuffKey && statKeys.Length == 1 && statKeys[0] == expectedStatKey;
        }

        static void Require(string[] keys, string name)
        {
            if (keys == null) throw new ArgumentNullException(name, "MaterialPlan: " + name + " missing (unobserved is not 'none')");
            if (keys.Any(string.IsNullOrEmpty)) throw new ArgumentException("MaterialPlan: " + name + " has an empty key");
        }
    }
}
